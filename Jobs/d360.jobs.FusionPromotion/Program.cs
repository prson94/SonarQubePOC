using Microsoft.Azure.WebJobs;
using d360.core;
using System.Collections.Generic;
using System;
using System.Linq;
using Dapper;

namespace d360.jobs.FusionPromotion
{
    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));
            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 4).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();

                    try
                    {
                        Console.WriteLine("Company: {0}. Executing FusionPromotion procedure", companyID);

                        bool writeStatus = true;
                        var task = companyConnection.ExecuteAsync("EXEC fusion.Rules", null, null, 12600);
                        //var task = companyConnection.ExecuteAsync("EXEC utility.PromoteFusionAttributes", null, null, 10800);
                        task.ContinueWith(t =>
                        {
                            if (t.IsCompleted)
                                Console.WriteLine("Fusion promotion completed for Company {0}", companyID);
                            if (t.IsFaulted)
                                Console.WriteLine("Fusion promotion failed for Company {0}", companyID);
                            if (t.Exception != null)
                            {
                                if (t.Exception.InnerExceptions != null)
                                {
                                    mex.AddRange(t.Exception.InnerExceptions);
                                }
                            }
                            writeStatus = false;
                        });

                        while (writeStatus)
                        {
                            Console.WriteLine(".");
                            System.Threading.Thread.Sleep(15000);
                        }

                        Console.WriteLine("Company: {0}. Finished executing FusionPromotion procedure", companyID);
                    }
                    catch (Exception ex)
                    {
                        mex.Add(ex);
                    }

                    companyConnection.Close();
                    companyConnection.Dispose();
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                Console.WriteLine(msg);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
