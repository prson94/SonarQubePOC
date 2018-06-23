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
                var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 4).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif
                companies.ForEach(c =>
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);

                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        try
                        {
                            company.Execute("delete ResponsibilityTypeRelationItem where RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
                            company.Execute("delete ResponsibilityTypeRelationTypeItem where RuleID not in (select ID from ResponsibilityTypeRelationRule)", commandTimeout: 7200);
                        }
                        catch (Exception dex)
                        {
                            CoreFunction.AITrackException(functionName, dex, c.CompanyID);
                            log.WriteLine($"Company [{c.CompanyID}]: [{dex.GetFullExceptionData()}]");
                            CoreFunction.AIFlush();
                        }

                        var items = company.Query<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule").ToList();

                        if (items.Count > 0)
                        {
                            var errorList = string.Empty;

                            items.ForEach(i => {
                                try
                                {
                                    i.SetDefinitionFromRaw();

                                    if (i.ApplyToType)
                                    {
                                        var typeResults = company.GetProcessedResponsibilityRuleTypeResults(i).ToList();
                                        company.SaveResponsibilityRuleTypeResults(typeResults, true, i.ID);
                                        typeResults = null;
                                    }
                                    else
                                    {
                                        var itemResults = company.GetProcessedResponsibilityRuleResults(i).ToList();
                                        company.SaveResponsibilityRuleResults(itemResults, true, i.ID);
                                        itemResults = null;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorList += $"Company [{c.CompanyID}] for Object [{i.Object} {i.ObjectID}]: [{ex.GetFullExceptionData()}]; ";
                                }
                            });

                            // Build cache.SecurityProcessor
                            company.Execute("exec cache.SecurityProcessor 4, 1, 0", commandTimeout: 1200);
                            log.WriteLine("Re-built cache.AssetResponsibility for company {0}.", c.CompanyID);

                            if (!string.IsNullOrEmpty(errorList))
                            {
                                CoreFunction.AITrackException(functionName, new ApplicationException($"The following errors occurred: {errorList}"), c.CompanyID);
                                //log.Error(errorList);
                                CoreFunction.AIFlush();
                            }
                        }
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
