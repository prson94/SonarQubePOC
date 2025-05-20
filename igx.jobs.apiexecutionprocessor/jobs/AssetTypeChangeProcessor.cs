using d360.core.queue;
using Dapper;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using repositories.azure;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
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
							(bool isFieldCounterType, int? CounterInitialIndex) = await catalog.IsFieldCounterType(info.FieldTypeId);
							if (isFieldCounterType)
							{
								var assetIds = await catalog.GetAssetsByFieldType(info.AssetTypeId);
								if (assetIds.Any())
								{
									await catalog.InsertAssetWithCounter(
										CounterInitialIndex ?? default,
										info.AssetTypeId,
										info.FieldTypeId ?? default,
										assetIds);
								}
							}
							break;
						case AssetTypeChangeAction.FieldRemoval:
							//var fieldType = await catalog.ReadFieldByAssetTypeAsync(info.AssetTypeId, info.FieldTypeId);
							// NOTE: Ensure that you change the save logic so that we do not incur any cost to remove the field at DELETION time.
							//		The field should be logically deleted on DELETION.
							break;
						default:
							Console.WriteLine(info.Action);
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


		private async Task<int> GetCounterFieldValue(DapperConnectionProvider dapper, int? fieldTypeId)
		{
			try
			{
				using (var connection = (SqlConnection)dapper.Connect())
				{
					var sql = @"
							Select fc.Value from dbo.FieldCounterValue fc
							Join dbo.FieldType ft
							ON  fc.FieldTypeId = ft.ID
							Where ft.[Name] = 'ProcessCounter' AND fc.FieldTypeId = @fieldTypeId
							  ";

					return await connection.QueryFirstOrDefaultAsync<int>(sql, new {  fieldTypeId });
				}
			}
			catch (Exception ex)
			{

				return -1;
			}
		}

		private async Task<IEnumerable<int>> GetAssetsByFieldType(DapperConnectionProvider dapper, int? assetTypeId)
		{
			try
			{
				using (var connection = (SqlConnection)dapper.Connect())
				{
					var sql = @"
							  SELECT a.ID from dbo.Asset a
							  WHERE a.AssetTypeID = @assetTypeId
							  ";

					return await connection.QueryAsync<int>(sql, new { assetTypeId });
				}
			}
			catch (Exception)
			{

				throw;
			}
		}
	}
}
