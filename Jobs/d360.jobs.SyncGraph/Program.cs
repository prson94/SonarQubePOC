using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.extensions.graph;
using System.Data.SqlClient;
using Dapper;
using d360.extensions;

namespace d360.jobs.SyncGraph
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program : FunctionsBase
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();
                //var companies = GetActiveDevelopmentCompanyIDs();

#if DEBUG                       
                companies = GetActiveCompanyIDs().Where(i => i == 4).ToList();
#endif

                companies.ForEach(companyID =>
                {
                    try
                    {
                        // load intersects for this company
                        using (var context = GetCompanyConnection(companyID))
                        {
                            Console.WriteLine("Starting to rebuild graph [company id: {0}]", companyID);
                                                        
                            Console.WriteLine("Starting to load intersects [company id: {0}]", companyID);

                            var sql =
                                    @"select
	                                [utility].[GetObjectName] (I.Subject, I.SubjectID) as SubjectName
	                                , I.Subject
	                                , I.SubjectID
	                                , [utility].[GetObjectName] (I.Object, I.ObjectID) as ObjectName
	                                , I.Object
	                                , I.ObjectID
	                                , p.name as Predicate
                                from
	                                [intersect] i
	                                left outer join intersecttype it on (i.intersecttypeid = it.id)
	                                left outer join [predicate] p on (it.predicateid = p.id)
                                ";

                            var intersects = context.Query<dynamic>(sql);
                            
                            Console.WriteLine("Starting clear graph [company id: {0}]", companyID);

                            // add them to the graph
                            var graphDatabase = new CosmosGraphProvider();

                            //clear the graph for this company
                            var clearTask = graphDatabase.ClearData(companyID);

                            clearTask.Wait();

                            Console.WriteLine("Starting populate graph with intersects [company id: {0}]", companyID);

                            var vertices = new HashSet<VertexModel>();
                            var edges = new HashSet<EdgeModel>();
                            
                            foreach (var item in intersects)
                            {
                                var startId = $"{item.Object}|{item.ObjectID}";
                                var endId = $"{item.Subject}|{item.SubjectID}";

                                vertices.Add(new VertexModel { ID = startId, Label = item.Object, Properties = new Dictionary<string, string> { {"name", item.ObjectName } } } );
                                vertices.Add(new VertexModel { ID = endId, Label = item.Subject, Properties = new Dictionary<string, string> { { "name", item.SubjectName } } });

                                edges.Add(new EdgeModel { RelationshipType = item.Predicate, StartID = startId, EndID = endId, StartLabel = item.Object, EndLabel = item.Subject });
                            }

                            graphDatabase.AddObjects<VertexModel>(companyID, vertices).Wait();
                            
                            graphDatabase.AddObjects<EdgeModel>(companyID, edges).Wait();
                        }
                    }
                    catch(Exception e)
                    {

                    }
                });
            }
            catch
            {

            }
        }

        private static IEnumerable<dynamic> GetIntersects(SqlConnection context)
        {
            return null;
        }
    }
}
