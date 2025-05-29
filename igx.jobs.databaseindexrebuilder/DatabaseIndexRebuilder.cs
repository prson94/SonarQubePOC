using d360.core;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.databaseindexrebuilder
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class DatabaseIndexRebuilder : BaseWebJob
	{
        const string FUNCTION_NAME = "DatabaseTask_IndexRebuilder";
#if DEBUG
        const string TIMER_SETTINGS = "*/60 * * * * *";
#else
        const string TIMER_SETTINGS = "0 0 0 * * SAT";
#endif

		public DatabaseIndexRebuilder(IConfiguration config, ICommunity community) : base(community, config)
		{

		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS)]TimerInfo myTimer, ILogger log)
        {
            int commandTimeout = int.TryParse(Configuration["IndexRebuilderDBCommandTimeout"], out commandTimeout) ? commandTimeout : 1800;

			await LoopThroughTenantsAsync(log, FUNCTION_NAME, async item => {
				try
				{
					var start = DateTime.Now;
					using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(item.CompanyID, item.Server, item.Username, item.Password))
					{
						await companyConnection.OpenAsync();
						await companyConnection.ExecuteAsync(
							"EXEC [dbo].[AzureSQLMaintenance]", 
							new { 
								Operation = "reindex", 
								MinReorganize = 5, 
								From = 15, 
								To = 100, 
								MinNumberOfPages = 10 
							}, null, commandTimeout);
					}
					TimeSpan end = DateTime.Now - start;
					log.LogInformation($"Completed database index rebuild. Time taken: {end.TotalMinutes}");
				}
				catch (Exception e)
				{
					log.LogError(e, "Error during DB index rebuild.");
				}	
			});
		}
    }
}
