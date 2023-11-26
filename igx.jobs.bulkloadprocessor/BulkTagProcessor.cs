using Azure.Messaging.ServiceBus;
using d360.extensions;
using d360.extensions.info;
using d360.featureflags;
using d360.model;
using d360.model.DataAccessLayer;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.bulkloadprocessor
{
	public class BulkLoadTagProcessor: BaseWebJob
	{
		const string FUNCTION_NAME = "BulkLoadTag_Process";

		ICachingProvider Cache;
		IMailProvider Mail;
		IQueueSource Queue;
		IFeatureFlagService FeatureFlags;

		public BulkLoadTagProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue, IFeatureFlagService ff) : base(config)
		{
			Cache = cache;
			FeatureFlags = ff;
			Mail = mail;
			Queue = queue;
		}

		public async Task Run([ServiceBusTrigger("%EventBusTopicName%", "BatchApiEvent")] ServiceBusReceivedMessage brokeredMessage, ILogger log)
		{
			string messageString;
			BatchApiEvent info;

			log.LogDebug($"BulkLoadTagProcessor triggered with message: {brokeredMessage.MessageId}");

			try
			{
				messageString = Encoding.UTF8.GetString(brokeredMessage.Body.ToArray());
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
					var context = new UriSecurityContextProvider
					{
						CompanyID = info.CompanyID,
						ResourceID = 0,
						CompanyPrefix = info.CompanyDomainPrefix,
						IsAdministrator = true
					};
					var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
					var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true);
					var tagRepository = new TagRepository(company, FeatureFlags);

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
