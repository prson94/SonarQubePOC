using d360.core.queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using repositories.azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
{
	public class SecurityPolicyProcessor : BaseWebJob
	{
		const string FUNCTION_NAME = "SecurityPolicyProcessor";

		public SecurityPolicyProcessor(IConfiguration config, ICommunity community) : base(community, config)
		{
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([QueueTrigger(constants.Queue.SecurityPolicy, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
		{
			var info = JsonConvert.DeserializeObject<SecurityPolicyQueueMessage>(myQueueItem);

			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", info.CompanyID }
			};

			using (log.BeginScope(logProperties))
			{
				try
				{
					var connectionString = Community.GetConnectionStringForTenant(info.CompanyID);
					var dapperProvider = new DapperConnectionProvider
					{
						ReadOnlyConnectionString = $"{connectionString}ApplicationIntent=ReadOnly",
						ReadWriteConnectionString = $"{connectionString}ApplicationIntent=ReadWrite"
					};

					ISecurity security = new Security(dapperProvider);

					List<Guid> assetUidsToRescore;

					Scoring scoring = new Scoring(dapperProvider);

					if (info.IsDeleteAction && info.PolicyUid.HasValue)
					{
						var response = await scoring.ReadAssetUidsAssociatedToPolicyAsync(info.PolicyUid.Value);
						if (response.IsSuccess)
						{
							assetUidsToRescore = response.Data;
						}
						await security.RemovePolicyAsync(info.PolicyUid.Value, false);
					}
					else 
					{
						if (info.AssetUid.HasValue)
						{
							await security.RunPolicyAsync(info.AssetUid);
							//assetUidsToRescore = new List<Guid> { info.AssetUid.Value };
						}
						else if (info.ExecutionUid.HasValue)
						{
							await security.RunPolicyAsync(null, info.ExecutionUid);
						}
						else if (info.PolicyUid.HasValue)
						{
							await security.RunPolicyAsync(null, null, info.PolicyUid);
							var response = await scoring.ReadAssetUidsAssociatedToPolicyAsync(info.PolicyUid.Value);
							if (response.IsSuccess)
							{
								assetUidsToRescore = response.Data;
							}
						}
						else
						{
							await security.RunPolicyAsync();
						}
					}
					//if (assetUidsToRescore.Count > 0)
					//{ 
					
					//}
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Error while processing security policy.");
				}
			}
		}

	}
}
