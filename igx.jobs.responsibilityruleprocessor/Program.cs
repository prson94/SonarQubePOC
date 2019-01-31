using d360.core;
using d360.core.entities;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace igx.jobs.responsibilityruleprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class ResponsibilityRuleProcessor
    {
        const string functionName = "ResponsibilityRules_ProcessScheduled";
#if DEBUG
        const string timerSettings = "*/2 * * * * *";
#else
        const string timerSettings = "0 */3 * * * *";
#endif
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {

#if DEBUG
                var companies = new List<CompanyWithDatabaseServerSettings>();
                using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                    companies = cnn.Query<CompanyWithDatabaseServerSettings>(@"
                        select  c.ID as CompanyID, 
                                c.Status, 
                                ds.Server, 
                                ds.Username, 
                                ds.Password, 
                                ds.FusionQueue, 
                                ds.SearchServer, 
                                ds.EventTopic, 
                                ds.IsDevelopment,
                                c.EnvironmentLevel,
                                CDS.UrlPrefix
                        from    company c 
                                inner join databaseserver ds on c.databaseserverid = ds.id and c.ID = 1065
                                inner join CompanyDomainSetting CDS on CDS.CompanyID = c.ID and CDS.IsPrimary = 1").ToList();
                }
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif
                //companies.ForEach(c =>
                companies.AsParallel().ForAll(c =>
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);

                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

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
                            company.ProcessResponsibilityRelationRules();
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
                });
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
