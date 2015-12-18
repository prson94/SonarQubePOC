using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace d360.jobs.CalculateAnalytics
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
                    using (var companyConnection = GetCompanyConnection(companyID))
                    {
                        companyConnection.Open();

                        Console.WriteLine("Calling FindRelations proc to find cross fusion relations for company id: {0}", companyID);

                        companyConnection.Execute("EXEC [fusion].[FindRelationships]", null, null, 600);

                        Console.WriteLine("Finished calling FindRelations proc to find cross fusion relations for company id: {0}", companyID);

                        Console.WriteLine("Calling process unprocessed relationships proc for company id: {0}", companyID);

                        companyConnection.Execute("EXEC [fusion].[ProcessUnprocessedRelations]", null, null, 600);

                        Console.WriteLine("Completed unprocessed relationships proc for company id: {0}", companyID);                        
                    }
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine("AN EXCEPTION OCCURED WHILE RUNNING D360.JOBS.FUSIONRELATIONSHIPPROCESSING DETAILS:" + ex.Message);
                mex.Add(ex);
            }

            if (mex.Count > 0) throw new AggregateException(mex);
        }
    }
}
