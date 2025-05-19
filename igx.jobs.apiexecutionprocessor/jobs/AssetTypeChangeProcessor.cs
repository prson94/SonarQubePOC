using d360.core.queue;
using DocumentFormat.OpenXml.Office.CustomUI;
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
	public class AssetTypeChangeProcessor : BaseWebJob
	{
		const string FUNCTION_NAME = "AssetTypeChangeProcessor";

		public AssetTypeChangeProcessor(IConfiguration config, ICommunity community) : base(community, config)
		{
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([QueueTrigger(constants.Queue.AssetTypeChange, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
		{
			var info = JsonConvert.DeserializeObject<AssetTypeChangeMessage>(myQueueItem);

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

					ICatalog catalog = new Catalog(dapperProvider);

					switch (info.Action)
					{
						case AssetTypeChangeAction.Removal:
							
							break;
						case AssetTypeChangeAction.FieldAddition:
							//var fieldType = await catalog.ReadFieldByAssetTypeAsync(info.AssetTypeId, info.FieldTypeId);

							// Check if this new field is a Counter. If so, you need to populate all existing assets with a new counter value.
							// Create a method on the ICatalog repository that calls SQL to populate the counter value on all existing assets, then resets the counter to a new currentIndex.
							// NOTE: Ensure that you change the save logic so that we do not incur any cost to add the field at CREATION time.

							// If the field has a default value (non-counter) then we should populate all eixsting assets with the default value.
							// Create a method on the ICatalog repository that calls SQL to populate the default value on all existing assets.
							break;
						case AssetTypeChangeAction.FieldRemoval:
							//var fieldType = await catalog.ReadFieldByAssetTypeAsync(info.AssetTypeId, info.FieldTypeId);
							// NOTE: Ensure that you change the save logic so that we do not incur any cost to remove the field at DELETION time.
							//		The field should be logically deleted on DELETION.
							break;
					}
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Error while processing asset type change.");
					throw;
				}
			}
		}

	}
}
