using d360.core;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using System.Linq;
using d360.extensions.search;

namespace igx.functions.consumption
{
    public class IndexMaintainance
    {
        const string functionName = "IndexMaintainance";
        private CoreFunction CoreFunction;

        const string timerSettings = "0 0 17 * * 6";

        [FunctionName("IndexMaintainance")]
        public async Task Run([TimerTrigger(timerSettings)] TimerInfo myTimer, ExecutionContext context, TextWriter log)
        {
            var config = new ConfigurationBuilder()
                   .SetBasePath(context.FunctionAppDirectory)
                   .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .Build();

            CoreFunction = new CoreFunction(config);

            try
            {
                foreach (var server in CoreFunction.GetSearchServersByCurrentSlot())
                {
                    CoreFunction.AITrackTrace(functionName, $"Performing index maintainance for server {server}");
                    var companies = CoreFunction.GetCompaniesBySearchServer(server);

                    var indexCompanies = ElasticSearchSource.GetCompanyByIndices(server);
                    var serverCompanies = companies.Where(c => c.SearchServer == server).Select(c => c.CompanyID);

                    var removeIndices = indexCompanies.Except(serverCompanies);
                    var missingIndices = serverCompanies.Except(indexCompanies);

                    if (removeIndices.Any())
                    {
                        foreach (var companyId in removeIndices)
                        {
                            CoreFunction.AITrackTrace(functionName, $"Removing index for company {companyId} on server {server}");
                            ElasticSearchSource.DeleteIndexIfExists(server, companyId);
                        }
                    }

                    if(missingIndices.Any())
                    {
                        foreach(var companyId in missingIndices)
                        {
                            CoreFunction.AITrackTrace(functionName, $"Company {companyId} does not have an index on server {server}");
                        }
                    }
                }
                CoreFunction.AITrackTrace(functionName, $"Index maintainance done");
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
            }

        }
    }
}
