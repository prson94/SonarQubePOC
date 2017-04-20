using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using d360.core;
using d360.utils.company;

namespace d360.jobs.ProcessRuleResults
{
    class Program: FunctionsBase
    {
        private static int _defaultQueryCommandTimeout = 180;

        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings();

           
              companies.ForEach(company =>
              {
                  try
                  {
                      if (company.IsDevelopment)
                      {
                          using (var context = GetCompanyConnection(company.CompanyID))
                          {
                              context.OpenWithRetry(RetryPolicy.DefaultFixed);

                              #region Resolving rule result qualifiers

                              Console.WriteLine("BEGIN: Resolving rule result qualifiers [company id: {0}]", company.CompanyID);

                              context.Execute(@"
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
							    inner join ArtifactType T on T.ID = O.ArtifactTypeID and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'ArtifactType' and T.ID = QT.ResolutionObjectID and O.TextPath = Q.Value
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
							    inner join TaxonomyType T on T.ID = O.TaxonomyTypeID and QT.ResolutionFieldTypeID = 0 and QT.ResolutionFieldTypeName = 'Name' and QT.ResolutionObject = 'TaxonomyType' and T.ID = QT.ResolutionObjectID and O.TextPath = Q.Value
					    ) R_T");

                              Console.WriteLine("END: Resolving rule result qualifiers [company id: {0}]", company.CompanyID);

                              #endregion

                              #region Resolving rule result qualifiers

                              Console.WriteLine("BEGIN: Send resolved rule results to event notification [company id: {0}]", company.CompanyID);

                              context.Execute(@"
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



                              Console.WriteLine("END: Send resolved rule results to event notification [company id: {0}]", company.CompanyID);

                              #endregion
                          }
                      }
                  }
                  catch (Exception ex)
                  {
                      Console.WriteLine(ex.GetFullExceptionData());
                  }
              });
            }
            catch (Exception ex)
            {
                mex.Add(ex);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
