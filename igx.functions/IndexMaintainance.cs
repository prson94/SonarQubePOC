using d360.core;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using System.Linq;
using d360.extensions.search;
using d360.utils.company;
using System.Collections.Generic;
using d360.extensions.queue;
using d360.core.queue;

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
			var queue = new AzureQueueSource(config);

			foreach (var server in CoreFunction.GetSearchServersByCurrentSlot())
            {

				try
				{

					CoreFunction.AITrackTrace(functionName, $"Performing index maintainance for server {server}");

					/* One search server may host indexes for different environment levels.
					 * serverCompanies are all companies regardless of environment level hosted on a search server
					 * slotCompanies are all companies of the current environment level hosted on the active search server
					 */
					var indexCompanies = ElasticSearchSource.GetCompanyByIndices(server);
					var serverCompanies = CoreFunction.GetCompaniesBySearchServer(server).Select(c => c.CompanyID);
					var slotCompanies = CoreFunction.GetCompaniesByCurrentSlot().Where(c => c.SearchServer == server);


					var removeIndices = indexCompanies.Except(serverCompanies);
					var missingIndices = slotCompanies.Select(c => c.CompanyID).Except(indexCompanies);
					var checkIndicies = slotCompanies.Select(c => c.CompanyID).Except(missingIndices);

					var reindex = new List<int>();

					foreach (var companyId in removeIndices)
					{
						CoreFunction.AITrackTrace(functionName, $"Removing index for company {companyId} on server {server}");
						ElasticSearchSource.DeleteIndexIfExists(server, companyId);
					}

					foreach (var companyId in missingIndices)
					{
						CoreFunction.AITrackTrace(functionName, $"Company {companyId} does not have an index on server {server}");
						reindex.Add(companyId);
					}

					foreach (var companyId in checkIndicies)
					{
						if(!ElasticSearchSource.IndexHasLatestFeatures(server, companyId))
						{
							CoreFunction.AITrackTrace(functionName, $"Company {companyId} index on server {server} does not have latest features");
							reindex.Add(companyId);
						}
						else
						{
							var c = slotCompanies.First(s => s.CompanyID == companyId);
							using (var companyConn = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
							{
								companyConn.Open();
								//Get field/limit values from index and db
								var indexableFields = ElasticSearchSource.CountIndexableFieldTypes(companyConn);
								var suggestedLimit = ElasticSearchSource.SuggestIndexLimit(companyConn);
								var limit = ElasticSearchSource.GetIndexTotalFieldsLimit(server, companyId);
								var fields = ElasticSearchSource.GetIndexFieldMappingCount(server, companyId);

								var limitDelta = Math.Abs(limit - suggestedLimit);

								if (suggestedLimit > 1000 && (10 * limitDelta) > suggestedLimit)
								{
									CoreFunction.AITrackTrace(functionName, $"Company {companyId} index on server {server} has a limit delta over 10%");
									reindex.Add(companyId);
								}
								else if (limit > 1000 && (2 * suggestedLimit) < limit)
								{
									CoreFunction.AITrackTrace(functionName, $"Company {companyId} index on server {server} has a limit more than twice the suggested");
									reindex.Add(companyId);
								}
								else if (Math.Max(indexableFields, 400) * 2 < Math.Max(fields, 800))
								{
									CoreFunction.AITrackTrace(functionName, $"Company {companyId} index on server {server} has more than double fields in mapping");
									reindex.Add(companyId);
								}
							}
						}
					}

					foreach(var companyId in reindex)
					{
                        ReindexModel model = new ReindexModel { CompanyID = companyId };
						queue.CreateMessage(config["SearchIndexQueue"], model);
					}

					CoreFunction.AITrackTrace(functionName, $"Index maintainance done");
				}
				catch (Exception ex)
				{
					CoreFunction.AITrackException(functionName, ex);
					log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
				}
			}
			CoreFunction.AITrackTrace(functionName, $"Index maintainance completely done");
		}
	}
}
