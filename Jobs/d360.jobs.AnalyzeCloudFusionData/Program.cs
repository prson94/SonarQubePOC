using Microsoft.Azure.WebJobs;
using System;
using System.Linq;

namespace d360.jobs.AnalyzeCloudFusionData
{
    class Program : FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));

            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 4).ToList();
#if DEBUG
                companies = GetActiveCompanyIDs().Where(i => i == 4).ToList();
#endif
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(3).ForAll(companyID =>
                {
                    // run EagleMC cloud fusion analysis
                    EagleMCCloudFusionAnalyzer.Analyze(companyID);                    
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("AN EXCEPTION OCCURED WHILE RUNNING D360.JOBS.ANALYZECLOUDFUSIONDATA FOR DETAILS:" + ex.Message);
            }
        }
        
                        
    }
}
