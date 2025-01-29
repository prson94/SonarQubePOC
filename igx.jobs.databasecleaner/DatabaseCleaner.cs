using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using repositories;

namespace igx.jobs.databasecleaner
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class DatabaseCleaner : BaseWebJob
	{
		const string FUNCTION_NAME = "DatabaseCleaner";
        const string TIMER_SETTINGS = "0 0 4 * * *";

		public DatabaseCleaner(IConfiguration config, ICommunity community) : base(community, config) { }

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)] TimerInfo myTimer, ILogger log)
		{
			try
			{
				var slot = GetEnvironmentLevelCurrentSlot();
				var tenants = await Community.ReadTenantConnectionSettingsByCurrentSlotAsync(slot);

				foreach (var c in tenants)
				{
					var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME },
						{ "CompanyID", c.CompanyID },
						{ "UrlPrefix", c.UrlPrefix }
					};

					string overrideValue = await Community.ReadSettingValueAsync<string>(c.CompanyID, Setting.AssetDataProfileLifespan);

					using (log.BeginScope(logProperties))
					{
						try
						{
							using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
							{
								company.Open();
								var settingInfo = Setting.AssetDataProfileLifespan.AsInfoModel();
								settingInfo.Value = (string.IsNullOrEmpty(overrideValue)) ? settingInfo.DefaultValue : overrideValue;

								//remove any old api execution records
								await company.ExecuteAsync("[api].[DeleteExecutionRecords]", commandTimeout: 1800);

								//remove any old data profile records
								await company.ExecuteAsync("[DeleteAssetDataProfileRecords] @dataProfileLifespan", new { dataProfileLifespan = (int)(Convert.ChangeType(settingInfo.Value, typeof(int))) }, commandTimeout: 1800);
							}
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error occured for company.");
						}
						finally
						{
							log.LogInformation("Finished run for company.");
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.LogCritical(ex, "Web job failed.");
			}
		}
	}
}
