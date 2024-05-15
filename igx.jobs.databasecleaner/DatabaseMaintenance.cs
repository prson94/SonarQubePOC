using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.databasecleaner
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class DatabaseMaintenance : BaseWebJob
	{
		const string FUNCTION_NAME = "DatabaseMaintenance";
        const string TIMER_SETTINGS = "0 0 * * * *";
#if DEBUG
		const bool RUN_ON_STARTUP = true;
#else
		const bool RUN_ON_STARTUP = false;
#endif

		public DatabaseMaintenance(IConfiguration config) : base(config) { }

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = RUN_ON_STARTUP)] TimerInfo myTimer, ILogger log)
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
							bool allowOffHourRun = false;

							string timezoneId = "";

							switch (c.Region)
							{
								case "AustraliaEast":
									timezoneId = "AUS Eastern Standard Time";
									break;
								case "SouthEastAsia":
									timezoneId = "Singapore Standard Time";
									break;
								case "FranceCentral":
									timezoneId = "W. Europe Standard Time";
									break;
								case "UKSouth":
									timezoneId = "GMT Standard Time";
									break;
								case "EastUS":
									timezoneId = "Eastern Standard Time";
									break;
								case "CentralUS":
									timezoneId = "Central Standard Time";
									break;
								default:
									timezoneId = "Eastern Standard Time";
									break;
							}
							
							TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
							var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone).TimeOfDay.Hours;
							allowOffHourRun = (now <= 6 || now >= 19);

							if (allowOffHourRun)
							{ 
								using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
								{
									company.Open();
									var metrics = await company.QuerySingleAsync<dynamic>(@"
SELECT	top 1
		avg_data_io_percent AS DataPct,
		avg_cpu_percent as CpuPct
FROM	sys.dm_db_resource_stats
order by end_time desc");

									decimal cpuPct = (decimal)metrics.CpuPct;
									decimal dataPct = (decimal)metrics.DataPct;

									if (cpuPct <= 50 && dataPct < 50)
									{ 
										// Re-organize indexes for database.
										await company.ExecuteAsync("exec AzureSQLMaintenance @From = 50, @To = 100", commandTimeout: 7200);
									}

									if (cpuPct <= 90 && dataPct < 75)
									{
										// Update database statistics.
										await company.ExecuteAsync("sp_updatestats", commandTimeout: 1400);
									}
								}							
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
