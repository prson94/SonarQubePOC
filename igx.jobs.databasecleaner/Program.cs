using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.databasecleaner
{
	class Program
    {
        static async Task Main()
        {
			var builder = new HostBuilder();
			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c => {
					c.AddTimers();
				})
				.ConfigureGovernLogging();

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public class DatabaseCleaner: BaseWebJob
    {
        const string FUNCTION_NAME = "DatabaseMaintenance_Cleaner";

#if DEBUG
        const string TIMER_SETTINGS = "*/1 * * * * *";
#else
        const string TIMER_SETTINGS = "0 0 4 * * *";
#endif

		public DatabaseCleaner(IConfiguration config) : base(config)
		{
				
		}

		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
			try
			{
				var companies = GetCompaniesByCurrentSlot();

				foreach (var c in companies)
				{
					var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME },
						{ "CompanyID", c.CompanyID },
						{ "UrlPrefix", c.UrlPrefix }
					};

					using (log.BeginScope(logProperties))
					{
						try
						{
							using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
							{
								company.Open();
								string overrideValue = company.Query<string>("select Value from Setting where ID = @ID", new { ID = (int)Setting.AssetDataProfileLifespan }).SingleOrDefault();
								var settingInfo = Setting.AssetDataProfileLifespan.AsInfoModel();
								settingInfo.Value = (string.IsNullOrEmpty(overrideValue)) ? settingInfo.DefaultValue : overrideValue;

								//remove any old api execution records
								await company.ExecuteAsync("[api].[DeleteExecutionRecords]", commandTimeout: 1800);

								//remove any old data profile records
								await company.ExecuteAsync("[DeleteAssetDataProfileRecords] @dataProfileLifespan", new { dataProfileLifespan = (int)(Convert.ChangeType(settingInfo.Value, typeof(int))) }, commandTimeout: 1800);

								//remove any old score execution data
								await company.ExecuteAsync("metrics.CleanupExecutions", commandTimeout: 1800);

								//remove any old queue task data
								await company.ExecuteAsync("Queue.DeleteQueueTaskRecords", commandTimeout: 1800);

								//update database statistics
								await company.ExecuteAsync("sp_updatestats", commandTimeout: 1400);
							}
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error occured for company.");
						}
						finally
						{
							log.LogError("Finished run for company.");
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
