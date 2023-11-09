using d360.core;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace igx.jobs.responsibilityruleprocessor
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage()
                .AddTimers();
            });

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public static class ResponsibilityRuleProcessor
    {
        const string FUNCTION_NAME = "ResponsibilityRules_ProcessScheduled";
        const string TIMER_SETTINGS = "0 */3 * * * *";

        public static async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)]TimerInfo myTimer, ILogger log)  
        {
            try
            {
                // increase the default dapper timeout from 30 to 90 seconds
                Dapper.SqlMapper.Settings.CommandTimeout = 90;

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

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
							var company = JobDbContextCreator.CreateCompanyContext(
								new UriSecurityContextProvider
								{
									CompanyID = c.CompanyID,
									CompanyPrefix = "",
									ResourceID = 0,
									IsAdministrator = true
								},
								new MandrillMailProvider
								{
									ApiKey = ConfigurationManager.AppSettings[constants.MAIL_API_KEY],
									SubAccount = ConfigurationManager.AppSettings[constants.MAIL_SUB_ACCOUNT]
								},
								new AzureQueueSource(),
								new DummyCachingProvider(),
								constants.COMMUNITY_DATABASE_CONNECTION);

							try
							{
								company.ClearInvalidRelationRuleResults();
							}
							catch (Exception dex)
							{
								log.LogError(dex, "Error while clearing relation rules results.");
							}

							try
							{
								await company.ProcessResponsibilityRelationRules();
							}
							catch (Exception ex)
							{
								log.LogError(ex, "Error while processing responsibility rules.");
							}
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error occurred while processing tasks for this environment.");
						}
					}
                }
            }
            catch (Exception ex)
            {
                log.LogCritical(ex, "Critical exception at root of web job");
            }
        }
    }
}
