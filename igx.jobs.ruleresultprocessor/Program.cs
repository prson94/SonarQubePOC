using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.IO;
using System.Linq;

namespace igx.jobs.ruleresultprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class RuleResultProcessor
    {
        const string functionName = "DataQuality_ProcessRuleResults";
        const string timerSettings = "0 */5 * * * *";
        //  const string timerSettings = "*/10 * * * * *";

        private static int _defaultQueryCommandTimeout = 180;

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(x => x.CompanyID == 4).ToList();
#endif

                companies.ForEach(c =>
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        #region Resolving rule result qualifiers

                        company.Execute(@"
update	Q
set		Q.ResolvedObject	= coalesce(R_F.Object, R_A.Object, R_R.Object, R_T.Object),
	Q.ResolvedObjectID	= coalesce(R_F.ObjectID, R_A.ObjectID, R_R.ObjectID, R_T.ObjectID),
	Q.UpdatedOn = coalesce(Q.UpdatedOn, getutcdate()),
	Q.UpdatedBy = 0
from	RuleResultQualifier Q
inner join RuleResultQualifierType QT on QT.ID = Q.RuleResultQualifierTypeID and QT.ResolutionObject is not null and Q.ResolvedObject is null
outer apply (
			select	top 1
					O.ObjectType as Object,
					O.ObjectID
			from	Field O
					inner join FieldType T on T.ID = O.FieldTypeID and T.ID = QT.ResolutionFieldTypeID and T.Object = QT.ResolutionObject and T.ObjectID = QT.ResolutionObjectID and O.FormattedValue = Q.Value
			) R_F
outer apply (
			select	top 1
					'Artifact' as Object,
					O.ObjectID as ObjectID
			from	AssetDetail O
					where O.[Object] = 'Artifact' and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'ArtifactType' and O.TypeID = QT.ResolutionObjectID and O.DisplayValue = Q.Value
			) R_A
outer apply (
			select	top 1
					'ReferenceItem' as Object,
					O.ObjectID as ObjectID
			from	AssetDetail O
                    inner join Asset A on
                    O.ID = A.ID
					where O.[Object] = 'ReferenceItem' and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'ReferenceItemType' and O.TypeID = QT.ResolutionObjectID and (O.DisplayValue = Q.Value OR A.Code = Q.Value)
			) R_R
outer apply (
			select	top 1
					'Taxonomy' as Object,
					O.ObjectID as ObjectID
			from	AssetDetail O
					where O.[Object] = 'Taxonomy' and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'TaxonomyType' and O.TypeID = QT.ResolutionObjectID and O.DisplayValue = Q.Value
			) R_T
where coalesce(Q.UpdatedOn, dateadd(minute, -10, getutcdate())) >= dateadd(minute, -10, getutcdate())"
, commandTimeout: _defaultQueryCommandTimeout);

                        #endregion

                        #region Resolving rule result qualifiers

                        company.Execute(@"
        select	RuleResultID,
			    RuleResultQualifierTypeID,
			    ResolvedObject as Object,
			    ResolvedObjectID as ObjectID,
			    D.Type as ObjectType,
			    D.TypeID as ObjectTypeID
	    into	#tbl
	    from	RuleResultQualifier Q
			    inner join AssetDetail D on D.Object = Q.ResolvedObject and D.ObjectID = Q.ResolvedObjectID
	    where	ResolvedObject is not null 
			    and EventNotificationSent = 0

	    insert into [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
            select	'EventTopicNotification', 
				    '<fields><ChangeType>RuleResult</ChangeType><ObjectType>' + ObjectType + '</ObjectType><ObjectTypeID>' + cast(ObjectTypeID as varchar) + '</ObjectTypeID><RuleResultID>' + cast(RuleResultID as varchar) + '</RuleResultID></fields>',
				    Object, 
				    ObjectID
		    from	#tbl T

	    update	T
	    set		T.EventNotificationSent = 1
	    from	RuleResultQualifier T
			    inner join #tbl S on S.RuleResultID = T.RuleResultID and S.RuleResultQualifierTypeID = T.RuleResultQualifierTypeID", commandTimeout: _defaultQueryCommandTimeout);

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        //log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                //log.Error($"EXCEPTION OCCURED: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }
    }
}
