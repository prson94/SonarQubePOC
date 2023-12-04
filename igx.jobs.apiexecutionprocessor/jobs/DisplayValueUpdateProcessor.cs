using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using d360.utils.company;
using Dapper;
using DocumentFormat.OpenXml.Math;
using igx.jobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.functions.consumption
{
	public class DisplayValueUpdateProcessor: BaseWebJob
    {
		const string FUNCTION_NAME = "DisplayValueUpdateProcessor";
		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;

		public DisplayValueUpdateProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue): base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[FunctionName(FUNCTION_NAME)]
        public async Task Run([QueueTrigger("%DisplayValueQueue%", Connection = "QueuesConnectionString")] string myQueueItem, ILogger log)
        {
            var updateInfo = JsonConvert.DeserializeObject<DisplayUpdateInfo>(myQueueItem);

			var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME },
					{ "CompanyID", updateInfo.CompanyID },
					{ "RebuildAll", updateInfo.RebuildAll }
				};

			using (log.BeginScope(logProperties))
			{
				try
				{
					var _c = GetCompaniesByCurrentSlot().FirstOrDefault(x => x.CompanyID == updateInfo.CompanyID);
					var context = new UriSecurityContextProvider
					{
						CompanyID = updateInfo.CompanyID,
						CompanyPrefix = _c.UrlPrefix,
						ResourceID = 0,
						IsAdministrator = true,
					};
					var community = new CommunityContext(Cache, Queue, context);
					var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true);

					using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(updateInfo.CompanyID, Configuration["CommunityContext"]))
					{
						await companyConnection.OpenIfClosed();

						var assetTypeID = updateInfo.AssetTypeID;
						if (updateInfo.ObjectTypeID > 0)
						{
							assetTypeID = await companyConnection.QueryFirstOrDefaultAsync<int>($"select id from assettype where [object] = @obj and [objectid] = @objId", new { obj = new DbString { Value = updateInfo.ObjectType, IsFixedLength = true, Length = 20, IsAnsi = true }, objId = updateInfo.ObjectTypeID });
						}

						//if its an asset call the asset update proc
						//if its a asset type call the asset type update proc
						if (updateInfo.AssetID > 0)
						{
							await companyConnection.ExecuteAsync("exec GenerateAssetDisplayValue @assetID, null,-1", new { assetID = updateInfo.AssetID }, null, 2400);
						}
						else if (assetTypeID > 0)
						{
							await companyConnection.ExecuteAsync("exec GenerateAssetTypeDisplayValues @assetTypeID", new { assetTypeID }, null, 2400);
						}
						else if (updateInfo.RebuildAll)
						{
							try
							{
								await companyConnection.ExecuteAsync("exec CheckDisplayValues", commandTimeout: 2400);
							}
							catch (Exception ex)
							{
								log.LogError(ex, "Error on display value update processor.");
							}
							finally
							{
								await company.UpdateRebuildJobStatus(
									CompanyRebuildJobToken.DisplayValues, 
									CompanyRebuildJobStatusState.Inactive, 
									int.Parse(Configuration["V2EnvironmentJobRebuildTimeoutInHours"])
								);
							}
						}
					}
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Error on display value update processor.");
				}
			}
        }
    }
}
