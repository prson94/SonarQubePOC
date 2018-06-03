using Microsoft.Azure.WebJobs;
using System;
using System.IO;

namespace igx.jobs.dataqualitychecker
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

    public static class DataQualityChecker
    {
        const string functionName = "DatabaseMaintenance_QualityChecker";
        const string timerSettings = "0 */30 * * * *";
        //const string timerSettings = "*/10 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                //CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    try
                    {
                        //                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);

                        //                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        //                        var items = company.Query<TextPathModel>(@"
                        //select  od.[Object],
                        //		od.[ObjectID],
                        //		od.[TextPath],
                        //		utility.GetBreadcrumbStringWrapper(od.[object], od.[objectid], '/') as CorrectTextPath
                        //from    cache.objectdetails od
                        //where   od.[textpath] != utility.GetBreadcrumbStringWrapper(od.[object], od.[objectid], '/') and
                        //		od.[object] in ('Taxonomy', 'Policy')").ToList();

                        //                        if (items.Count > 0)
                        //                        {
                        //                            CoreFunction.AITrackEvent(
                        //                                functionName,
                        //                                "Invalid Paths Found", 
                        //                                new System.Collections.Generic.Dictionary<string, string>() { {
                        //                                        "Message",
                        //                                        $"Found {items.Count} item(s) with invalid text paths."
                        //                                    }
                        //                                },
                        //                                c.CompanyID);
                        //                        }

                        //                        var errorList = string.Empty;
                        //                        items.ForEach(i => {
                        //                            try
                        //                            {
                        //                                company.Execute($"update [{i.Object}] set TextPath = @tp where ID = @id", new { tp = i.CorrectTextPath, id = i.ObjectID });
                        //                            }
                        //                            catch (Exception ex)
                        //                            {
                        //                                errorList += $"Company [{c.CompanyID}] for Object [{i.Object} {i.ObjectID}]: [{ex.GetFullExceptionData()}]; ";
                        //                            }
                        //                        });

                        //                        if (!string.IsNullOrEmpty(errorList))
                        //                        {
                        //                            CoreFunction.AITrackException(functionName, new ApplicationException($"The following TextPath update errors occurred: {errorList}"), c.CompanyID);
                        //                            log.Error(errorList);
                        //                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        //log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });

                //CoreFunction.AITrackJobCompletedNoErrors(functionName);
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
