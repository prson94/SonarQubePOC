using d360.core;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.workflowdigestprocessor
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

    public static class WorkflowDigestProcessor
    {
        const string functionName = "Workflow_DigestProcessor";
        

#if DEBUG
        const string timerSettings = "*/10 * * * * *";
#else
        const string timerSettings = "0 0 5 * * *"; // every day at 5am
#endif


        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                foreach (var c in companies)
                {
                    try
                    {
                        // Create EF connection
                        var company = JobDbContextCreator.CreateCompanyContext(
                            new UriSecurityContextProvider
                            {
                                CompanyID = c.CompanyID,
                                CompanyPrefix = c.UrlPrefix,
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

                        await company.SendDigestEmails(c.EnvironmentLevel);
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.Message}]");
                    }
                }
                
                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.Message}");
            }

            CoreFunction.AIFlush();
        }
    }
}