using d360.core;
using d360.extensions.info;
using d360.extensions.mail;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
        const string functionName = "ResponsibilityRules_ProcessScheduled";
        const string timerSettings = "0 */3 * * * *";

        public static async Task Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                // increase the default dapper timeout from 30 to 90 seconds
                Dapper.SqlMapper.Settings.CommandTimeout = 90;

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(i => i.CompanyID == 2).ToList();
#endif

                foreach (var c in companies)
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
                            });

                        CoreFunction.AITrackEvent(functionName, "ResponsibilityRuleProcessor Job Starting", new Dictionary<string, string> { { "CompanyID", c.CompanyID.ToString() } });

                        try
                        {
                            company.ClearInvalidRelationRuleResults();
                        }
                        catch (Exception dex)
                        {
                            CoreFunction.AITrackException(functionName, dex, c.CompanyID);
                            log.WriteLine($"Company [{c.CompanyID}]: [{dex.GetFullExceptionData()}]");
                            CoreFunction.AIFlush();
                        }

                        try
                        {
                            await company.ProcessResponsibilityRelationRules();
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                            CoreFunction.AIFlush();
                        }

                        CoreFunction.AITrackEvent(functionName, "ResponsibilityRuleProcessor Job Completed", new Dictionary<string, string> { { "CompanyID", c.CompanyID.ToString() } });
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                        CoreFunction.AIFlush();
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
            }
            CoreFunction.AIFlush();
        }
    }
}
