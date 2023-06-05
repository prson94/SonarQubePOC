using d360.core.enums;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace igx.functions.consumption
{
    internal class DisplayValueChecker
    {
        const string functionName = "DisplayValueChecker";
        private CoreFunction CoreFunction;

#if DEBUG
        const string timerSettings = "*/10 * * * * *";
#else
        const string timerSettings = "0 0 */6 * * *"; // every 6 hours
#endif

        [FunctionName("DisplayValueChecker")]
        public async Task Run([TimerTrigger(timerSettings)] TimerInfo myTimer, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                   .SetBasePath(context.FunctionAppDirectory)
                   .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .Build();

            CoreFunction = new CoreFunction(config);

            try
            {
                CoreFunction.AITrackJobStart(functionName);

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(x => x.CompanyID == 2).ToList();
#endif
                foreach (var c in companies)
                {
                    try
                    {
						var securityContext = new UriSecurityContextProvider
						{
							CompanyID = c.CompanyID,
							CompanyPrefix = c.UrlPrefix,
							ResourceID = 0,
							IsAdministrator = true,
						};
						var mailProvider = new MandrillMailProvider
						{
							ApiKey = config.GetValue<string>("MandrillApiKey"),
							SubAccount = config.GetValue<string>("MandrillSubAccount")
						};

						using (
							var companyContext = JobDbContextCreator.CreateCompanyContext(
								securityContextProvider: securityContext,
								mailProvider: mailProvider,
								queueSource: new AzureQueueSource(config),
								cachingProvider: new DummyCachingProvider(),
								connectionString: CoreFunction.GetConnectionString("CommunityContext")))
						{ 
							var rs = await companyContext.UpdateRebuildJobStatus(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Active, config.GetValue("V2EnvironmentJobRebuildTimeoutInHours", 18));
							if (rs.StatusCode == System.Net.HttpStatusCode.OK)
							{
								try
								{
									companyContext.Connection.Execute("CheckDisplayValues", commandTimeout: 600, commandType: System.Data.CommandType.StoredProcedure);
								}
								catch (Exception ex)
								{
									CoreFunction.AITrackException(functionName, ex, c.CompanyID);
								}
								finally
								{
									await companyContext.UpdateRebuildJobStatus(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Inactive, config.GetValue("V2EnvironmentJobRebuildTimeoutInHours", 18));
								}
							}						
						}
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                    }
                }

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            CoreFunction.AIFlush();
        }
    }
}
