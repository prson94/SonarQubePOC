using d360.core;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.IO;

namespace igx.jobs
{
    public static class RuleResultProcessor
    {
        const string functionName = "DataQuality_ProcessRuleResults";
        const string timerSettings = "0 */5 * * * *";
        //const string timerSettings = "*/10 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

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
		    Q.ResolvedObjectID	= coalesce(R_F.ObjectID, R_A.ObjectID, R_R.ObjectID, R_T.ObjectID)
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
							    O.ID as ObjectID
					    from	Artifact O
							    inner join ArtifactType T on T.ID = O.ArtifactTypeID and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'ArtifactType' and T.ID = QT.ResolutionObjectID and O.DisplayValue = Q.Value
					    ) R_A
		    outer apply (
					    select	top 1
							    'ReferenceItem' as Object,
							    O.ID as ObjectID
					    from	ReferenceItem O
							    inner join ReferenceItemType T on T.ID = O.ReferenceItemTypeID and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'ReferenceItemType' and T.ID = QT.ResolutionObjectID and (O.DisplayValue = Q.Value OR O.Code = Q.Value)
					    ) R_R
		    outer apply (
					    select	top 1
							    'Taxonomy' as Object,
							    O.ID as ObjectID
					    from	Taxonomy O
							    inner join TaxonomyType T on T.ID = O.TaxonomyTypeID and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'TaxonomyType' and T.ID = QT.ResolutionObjectID and O.DisplayValue = Q.Value
					    ) R_T");

                        #endregion

                        #region Resolving rule result qualifiers

                        company.Execute(@"
	    select	RuleResultID,
			    RuleResultQualifierTypeID,
			    ResolvedObject as Object,
			    ResolvedObjectID as ObjectID,
			    D.ObjectType,
			    D.ObjectTypeID
	    into	#tbl
	    from	RuleResultQualifier Q
			    inner join cache.ObjectDetails D on D.Object = Q.ResolvedObject and D.ObjectID = Q.ResolvedObjectID
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
			    inner join #tbl S on S.RuleResultID = T.RuleResultID and S.RuleResultQualifierTypeID = T.RuleResultQualifierTypeID");

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
