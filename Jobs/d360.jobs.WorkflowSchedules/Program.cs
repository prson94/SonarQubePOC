using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace d360.jobs.WorkflowSchedules
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
                    Console.WriteLine("Starting to check for scheduled workflows that require instantiation for company id: {0}", companyID);
                    
                    try {                        
                        #region Create EF connection

                        var sec = new UriSecurityContextProvider()
                        {
                            CompanyID = companyID,
                            ResourceID = 0,
                            CompanyPrefix = "demo.dev",
                            IsAdministrator = true
                        };
                        var cache = new DummyCachingProvider();
                        var queue = new AzureQueueSource();
                        var community = new CommunityContext(cache, queue, sec);
                        var company = new CompanyContext(community, cache, queue, sec, true);
                                                
                        #endregion
                        //load all workflows of type schedule

                        var scheduledWorkflows = company.WorkflowEventRegistrations.Where(x => x.ChangeType == core.enums.Workflow.ChangeType.Schedule && x.Type.State == core.enums.State.Active && x.Type.PublishedVersionID != null).Include(x=>x.Type);

                        foreach (var registration in scheduledWorkflows)
                        {
                            // if the registration applies fire of the workflow and break if not go to the next one.
                            if (company.ExecuteScheduledWorkflow(registration))
                            {
                                return;
                            }
                        }                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("AN EXCEPTION OCCURED WHILE RUNNING D360.JOBS.WORKFLOWSCHEDULES FOR COMPANY: [{0}] MESSAGE: [{1}]",companyID, ex.Message);
                        mex.Add(ex);
                    }

                    Console.WriteLine("Completed checking for scheduled workflows for company id: {0}", companyID);

                });

            }
            catch (Exception ex)
            {
                Console.WriteLine("AN EXCEPTION OCCURED WHILE RUNNING D360.JOBS.WORKFLOWSCHEDULES DETAILS:" + ex.Message);
                mex.Add(ex);
            }

            if (mex.Count > 0) throw new AggregateException(mex);
        }                        
    }
}
