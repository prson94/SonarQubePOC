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
        const string timerSettings = "0 */3 * * * *";
        //const string timerSettings = "*/5 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    CoreFunction.AITrackEvent(functionName, "Begin Processing Company", null, c.CompanyID);

                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);

                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        var items = company.Query<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule").ToList();

                        CoreFunction.AITrackEvent(functionName, $"Got {items.Count} rules", null, c.CompanyID);
                        CoreFunction.AIFlush();

                        if (items.Count > 0)
                        {
                            CoreFunction.AITrackEvent(functionName, $"Item count is greater than 0", null, c.CompanyID);
                            CoreFunction.AIFlush();

                            var errorList = string.Empty;


                            items.ForEach(i => {
                                try
                                {
                                    //CoreFunction.AITrackEvent(functionName, $"Deserializing item definition from raw [{i.Definition}]", null, c.CompanyID);
                                    //CoreFunction.AIFlush();

                                    i.SetDefinitionFromRaw();

                                    //CoreFunction.AITrackEvent(functionName, $"Parsed raw definition", null, c.CompanyID);
                                    //CoreFunction.AIFlush();

                                    if (i.ApplyToType)
                                    {
                                        company.GetProcessedResponsibilityRuleTypeResults(i);
                                    }
                                    else
                                    {
                                        company.GetProcessedResponsibilityRuleResults(i);
                                    }

                                    //CoreFunction.AITrackEvent(functionName, $"Parsed end results", null, c.CompanyID);
                                    //CoreFunction.AIFlush();
                                }
                                catch (Exception ex)
                                {
                                    errorList += $"Company [{c.CompanyID}] for Object [{i.Object} {i.ObjectID}]: [{ex.GetFullExceptionData()}]; ";
                                }
                            });

                            CoreFunction.AITrackEvent(functionName, $"After foreach item", null, c.CompanyID);
                            CoreFunction.AIFlush();

                            if (!string.IsNullOrEmpty(errorList))
                            {
                                CoreFunction.AITrackException(functionName, new ApplicationException($"The following errors occurred: {errorList}"), c.CompanyID);
                                //log.Error(errorList);
                                CoreFunction.AIFlush();
                            }
                        }
                        else
                        {
                            CoreFunction.AITrackEvent(functionName, $"Item count is 0", null, c.CompanyID);
                            CoreFunction.AIFlush();
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        //log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                        CoreFunction.AIFlush();
                    }

                    CoreFunction.AITrackEvent(functionName, "End Processing Company", null, c.CompanyID);
                    CoreFunction.AIFlush();
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                //log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }
    }
}
