using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Diagnostics;
using Dapper;

namespace d360.jobs.queue.ProcessFollowChildren
{
    class Program : FunctionsBase
    {

        static string followUpdateSql = @"select * from queue.FollowUpdate where MachineAssigned IS NULL";

        static string taxonomyParentsSql = @"with t as
                                            (
	                                            select t1.* from taxonomy t1 where t1.id = @id
	                                            union all
	                                            select t2.* from t
	                                            join taxonomy t2 on t2.id = t.parentid
                                            )
                                            select c.id from t 
                                            inner join FollowWithChildren c on c.objectid = t.id and c.objecttype = 'Taxonomy' and c.FollowTypeID = 3";

        static void Main(string[] args)
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 4).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();

                    var domainPrefix = domainPrefixes.First(i => i.Key == companyID).Value;
                    var queueItems = companyConnection.Query<dynamic>(followUpdateSql).ToList();

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].FollowUpdate set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            switch((string)q.ObjectType)
                            {
                                case "Taxonomy":
                                    var processItems = companyConnection.Query<int>(taxonomyParentsSql,new { id = (int)q.ObjectID });

                                    foreach(var item in processItems)
                                    {
                                        companyConnection.Execute("SetChildrenByFollowID @id", new { id = (int)item});
                                    }
                                    
                                    break;
                            }

                            //cleanup orphaned FollowChild records

                            companyConnection.Execute("delete from followchild where not exists(select * from follow f where f.followtypeid = 3 and f.objecttype = parentobjecttype and f.objectid = parentobjectid)", null, null, 500);

                            companyConnection.Execute("delete [queue].FollowUpdate where ID = @queueID", new { queueID = q.ID }, null, 500);

                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            companyConnection.Execute(@"update [queue].FollowUpdate set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
                        }
                    });

                    companyConnection.Close();
                    companyConnection.Dispose();
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                Trace.TraceError(msg);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
