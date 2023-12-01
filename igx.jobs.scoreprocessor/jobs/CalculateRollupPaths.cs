using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
	public class CalculateRollupPaths: BaseWebJob
    {
        const string FUNCTION_NAME = "Scoring_CalculateRollupPaths_Timer";
        const string TIMER_SETTINGS = "0 */30 * * * *"; //every 30 minutes

		public CalculateRollupPaths(IConfiguration config): base(config)
		{
				
		}

		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            try
            {
                var companies = GetCompaniesByCurrentSlot();

                foreach(var c in companies)
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
								await company.ExecuteAsync("exec metrics.CalculateRollups", commandTimeout: 600);
							}                          
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error processing rollup paths for environment.");          
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
					log.LogCritical(ex, "Critical error in web job."); 
				}        
            }
        }
    }
}
