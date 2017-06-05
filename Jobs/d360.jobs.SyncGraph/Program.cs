using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.extensions.graph;
using System.Data.SqlClient;
using Dapper;

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

                            var intersects = GetIntersects(context);

                            Console.WriteLine("Starting clear graph [company id: {0}]", companyID);

                            // add them to the graph
                            var graphDatabase = new CosmosGraphProvider();

                            //clear the graph for this company
                            graphDatabase.ClearData(companyID);

                            Console.WriteLine("Starting populate graph with intersects [company id: {0}]", companyID);

                            //graphDatabase.AddVertices<Intersect>
                        }


                    }
                    catch
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
