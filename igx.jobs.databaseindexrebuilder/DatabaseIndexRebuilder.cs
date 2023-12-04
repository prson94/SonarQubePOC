using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace igx.jobs.databaseindexrebuilder
{
	public class DatabaseIndexRebuilder : BaseWebJob
	{
        const string FUNCTION_NAME = "DatabaseTask_IndexRebuilder";
#if DEBUG
        const string TIMER_SETTINGS = "*/60 * * * * *";
#else
        const string TIMER_SETTINGS = "0 0 0 * * SAT";
#endif

		public DatabaseIndexRebuilder(IConfiguration config) : base(config)
		{

		}

		[FunctionName(FUNCTION_NAME)]
		public void Run([TimerTrigger(TIMER_SETTINGS)]TimerInfo myTimer, ILogger log)
        {
            int commandTimeout = int.TryParse(Configuration["IndexRebuilderDBCommandTimeout"], out commandTimeout) ? commandTimeout : 1800;

            try
            {
                var companies = GetCompaniesByCurrentSlot().OrderBy(x => x.Priority);
                foreach (var item in companies)
                {
					var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME },
						{ "CompanyID", item.CompanyID },
						{ "UrlPrefix", item.UrlPrefix }
					};

					using (log.BeginScope(logProperties)) 
					{
						try
						{
							var start = DateTime.Now;
							using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(item.CompanyID))
							{
								companyConnection.Open();
								companyConnection.Execute(
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
					}
                }
            }
            catch (Exception ex)
            {
				var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME }
					};

				using (log.BeginScope(logProperties))
				{
					log.LogCritical(ex, "DatabaseReindexWebJob critical job error.");
				}
			}
		}
    }
}
