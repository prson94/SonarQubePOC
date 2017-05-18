using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
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
                var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings();
#if DEBUG || NIGHTLY
                companies = companies.Where(i => i.CompanyID == 4).ToList();
#endif

#if CLIENTDEV
                companies = companies.Where(i => i.IsDevelopment && i.CompanyID != 4).ToList();
#endif

#if PROD
                companies = companies.Where(i => !i.IsDevelopment).ToList();
#endif

                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(3).ForAll(c =>
                {
                    Console.WriteLine("Starting to check for scheduled workflows that require instantiation for company id: {0}", c.CompanyID);
                    
                    try {                        
                        #region Create EF connection

                        var sec = new UriSecurityContextProvider()
                        {
                            CompanyID = c.CompanyID,
                            ResourceID = 0,
                            CompanyPrefix = c.UrlPrefix,
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
                            if (company.ExecuteScheduledWorkflow(registration).Result)
                            {
                                break;
                            }
                        }

                        Console.WriteLine("Executing timer transitions for company id: {0}", c.CompanyID);
                        //evaluate any timer transitions and see if they need to be moved along    
                        var res = company.ExecuteTimerSteps();                  
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("AN EXCEPTION OCCURED WHILE RUNNING D360.JOBS.WORKFLOWSCHEDULES FOR COMPANY: [{0}] MESSAGE: [{1}]",c.CompanyID, ex.Message);
                        mex.Add(ex);
                    }

                    Console.WriteLine("Completed checking for scheduled workflows for company id: {0}", c.CompanyID);

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
