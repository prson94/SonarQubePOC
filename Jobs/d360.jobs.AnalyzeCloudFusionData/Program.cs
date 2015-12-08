using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace d360.jobs.AnalyzeCloudFusionData
{
    class Program : FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));
            var mex = new List<Exception>();
            
            try
            {
                var companies = GetActiveCompanyIDs();
#if DEBUG
                companies = GetActiveCompanyIDs().Where(i => i == 4).ToList();
#endif
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(3).ForAll(companyID =>
                {
                    Console.WriteLine("Starting to analyze cloud fusion data for company id: {0}", companyID);
                    
                    try {
                        EagleMCCloudFusionAnalyzer.Analyze(companyID);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("AN EXCEPTION OCCURED WHILE RUNNING D360.JOBS.ANALYZECLOUDFUSIONDATA FOR COMPANY: [{0}] MESSAGE: [{1}]",companyID, ex.Message);
                        mex.Add(ex);
                    }

                    Console.WriteLine("Completed to analyzing cloud fusion data for company id: {0}", companyID);

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine("AN EXCEPTION OCCURED WHILE RUNNING D360.JOBS.ANALYZECLOUDFUSIONDATA DETAILS:" + ex.Message);
                mex.Add(ex);
            }

            if (mex.Count > 0) throw new AggregateException(mex);
        }                        
    }
}
