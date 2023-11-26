using d360.core.queue;
using d360.extensions;
using d360.extensions.search;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.functions.consumption
{
	public class IndexMaintainance: BaseFunction
    {
		readonly ElasticSearchSource Search;
		readonly IQueueSource Queue;

		public IndexMaintainance(IConfiguration config, IQueueSource queue, ElasticSearchSource search) : base(config)
		{
			Queue = queue;
			Search = search;
		}

		[FunctionName("IndexMaintainance")]
        public async Task Run([TimerTrigger("0 0 17 * * 6")] TimerInfo myTimer, ILogger log)
        {
			foreach (var server in GetSearchServersByCurrentSlot())
            {
				var logProperties = new Dictionary<string, object> {
					{ "Function", "IndexMaintainance" },
					{ "SearchServer", server}
				};

				using (log.BeginScope(logProperties))
				{ 
					try
					{
						/* One search server may host indexes for different environment levels.
						 * serverCompanies are all companies regardless of environment level hosted on a search server
						 * slotCompanies are all companies of the current environment level hosted on the active search server
						 */
						var indexCompanies = ElasticSearchSource.GetCompanyByIndices(server);
						var serverCompanies = GetCompaniesBySearchServer(server).Select(c => c.CompanyID);
						var slotCompanies = GetCompaniesByCurrentSlot().Where(c => c.SearchServer == server);

						var removeIndices = indexCompanies.Except(serverCompanies);
						var missingIndices = slotCompanies.Select(c => c.CompanyID).Except(indexCompanies);
						var checkIndicies = slotCompanies.Select(c => c.CompanyID).Except(missingIndices);

						var reindex = new List<int>();

						foreach (var companyId in removeIndices)
						{
							ElasticSearchSource.DeleteIndexIfExists(server, companyId);
						}

						foreach (var companyId in missingIndices)
						{
							log.LogWarning($"Company {companyId} does not have an index on server {server}.");
							reindex.Add(companyId);
						}

						foreach (var companyId in checkIndicies)
						{
							if(!ElasticSearchSource.IndexHasLatestFeatures(server, companyId))
							{
								log.LogWarning($"Company {companyId} index on server {server} does not have latest features");
								reindex.Add(companyId);
							}
							else
							{
								var c = slotCompanies.First(s => s.CompanyID == companyId);
								using (var companyConn = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
								{
									companyConn.Open();
									//Get field/limit values from index and db

									var sql = @"select count(1) from FieldType where AssetTypeID is not null and [Type] not in @types";
									var indexableFields = companyConn.Query<int>(sql, new { types = SearchIndexer.ExcludedFieldTypes.ToList() }).Single();

									sql = "select case " +
										  "	WHEN a.dist > 30000 THEN 30000 " +
										  "	WHEN a.total > 30000 THEN a.dist " +
										  "	ELSE a.total " +
										  "END " +
										  "FROM (" +
										  "		select floor(count(1) * 2.4) AS total, floor(count(distinct [Name]) * 3.6) AS dist " +
										  "		from FieldType " +
										  "		where AssetTypeID is not null" +
										  ") a;";
									var suggestedLimit = companyConn.Query<int>(sql).First();
									var limit = ElasticSearchSource.GetIndexTotalFieldsLimit(server, companyId);
									var fields = ElasticSearchSource.GetIndexFieldMappingCount(server, companyId);

									var limitDelta = Math.Abs(limit - suggestedLimit);

									if (suggestedLimit > 1000 && (10 * limitDelta) > suggestedLimit)
									{
										log.LogWarning($"Company {companyId} index on server {server} has a limit delta over 10%");
										reindex.Add(companyId);
									}
									else if (limit > 1000 && (2 * suggestedLimit) < limit)
									{
										log.LogWarning($"Company {companyId} index on server {server} has a limit more than twice the suggested");
										reindex.Add(companyId);
									}
									else if (Math.Max(indexableFields, 400) * 2 < Math.Max(fields, 800))
									{
										log.LogWarning($"Company {companyId} index on server {server} has more than double fields in mapping");
										reindex.Add(companyId);
									}
								}
							}
						}

						foreach(var companyId in reindex)
						{
							var searchQueueName = Config["SearchIndexQueue"];
							Queue.CreateMessage(searchQueueName, new ReindexModel { CompanyID = companyId });
						}
					}
					catch (Exception ex)
					{
						log.LogError(ex, $"Indexer refresh error.");
					}				
				}
			}
		}
	}
}
