using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using System.Text;
using d360.extensions.mail;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace igx.jobs.bulkloadprocessor
{
	public class BulkLoadTagProcessor
	{
		const string FUNCTION_NAME = "BulkLoadTag_Process";
		const string BulkMethodName = "BULK";

		public async Task Run([ServiceBusTrigger("%EventBusTopicName%", "BatchApiEvent")] Message brokeredMessage, ILogger log)
		{
			string messageString;
			BatchApiEvent info;

			log.LogDebug($"BulkLoadTagProcessor triggered with message: {brokeredMessage.MessageId}");

			try
			{
				messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
				info = JsonConvert.DeserializeObject<BatchApiEvent>(messageString);
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error when parsing message.");
				return;
			}

			if (info.Action != BatchApiEventAction.Completed)
			{
				return;
			}

			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", info.CompanyID },
				{ "UrlPrefix", info.CompanyDomainPrefix },
				{ "ExecutionId", info.ExecutionID },
				{ "ExecutionAction", info.Action.ToString() }
			};

			using (log.BeginScope(logProperties))
			{
				try
				{
					var sec = new UriSecurityContextProvider
					{
						CompanyID = info.CompanyID,
						ResourceID = 0,
						CompanyPrefix = info.CompanyDomainPrefix,
						IsAdministrator = true
					};
					var cache = new DummyCachingProvider();
					var mail = new DummyMailProvider();
					var queue = new AzureQueueSource();
					var community = new CommunityContext(cache, queue, sec);

					var company = new CompanyContext(community, cache, queue, mail, sec, true);
					var tagRepository = new TagRepository(company, community);


					var execution = company.ApiExecutions.FirstOrDefault(e => e.ExecutionID == info.ExecutionID);
					if (execution != null && (execution.Action == d360.core.queue.ApiExecutionAction.PostAssets || execution.Action == d360.core.queue.ApiExecutionAction.PutAssets))
					{
						var load = company.Loads.FirstOrDefault(l => l.PutExecutionID == info.ExecutionID || l.PostExecutionID == info.ExecutionID);

						if (load != null)
						{
							var intersectTypeId = load.IntersectTypeUid != null ? company.IntersectTypes.Where(i => i.uid == load.IntersectTypeUid).FirstOrDefault().ID : -1;

							var assetTypeId = load.AssetTypeUid != null ? company.AssetTypes.Where(i => i.uid == load.AssetTypeUid).FirstOrDefault().ID : -1;

							var tagField = company.FieldTypes.FirstOrDefault(f => ((assetTypeId >= 0 && f.AssetTypeID == assetTypeId) || (assetTypeId < 0 && f.IntersectTypeID == intersectTypeId)) && f.Type == "Tag");

							if (tagField != null)
							{
								var loadHasTagField = company.LoadColumns.Any(l => l.LoadID == load.ID && l.Name == tagField.Name);

								if (loadHasTagField)
								{
									log.LogTrace($"Processing execution {execution.ExecutionID} for load {load.ID}");
									var bulkTags = await company.GetBulkTagAssetsAsync(load.ID, execution.ExecutionID);
									if (bulkTags.Any())
									{
										await tagRepository.BulkTagAssets(bulkTags, load.UpdatedBy ?? 0);
									}
								}

							}
						}
					}
				}
				catch (Exception ex)
				{
					log.LogCritical(ex, "Critical error in Bulk Tag Processor");
					return;
				}
			}

		}
	}
}
