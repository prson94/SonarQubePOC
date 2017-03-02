using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using d360.core;
using Microsoft.ServiceBus.Messaging;
using d360.core.queue;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;

namespace d360.jobs.subscriber.Workflow
{
    public class Program: FunctionsBase
    {
        public static void ProcessTopicMessage([ServiceBusTrigger("Events", "Workflow", AccessRights.Listen)] BrokeredMessage message)
        {
            try
            {
                var info = message.GetBody<EventInfo>();
                
                #region Create EF connection

                //var sec = new UriSecurityContextProvider()
                //{
                //    CompanyID = info.CompanyID,
                //    ResourceID = info.ResourceID,
                //    CompanyPrefix = info.DomainPrefix,
                //    IsAdministrator = true
                //};
                //var cache = new DummyCachingProvider();
                //var queue = new AzureQueueSource();
                //var community = new CommunityContext(cache, queue, sec);
                //var company = new CompanyContext(community, cache, queue, sec, true);

                var workflow = new WorkflowContext(GetCompanyConnectionString(info.CompanyID), info.CompanyID, info.ResourceID);

                #endregion

                var sObject = info.ObjectType.ToString();
                var registration = workflow.WorkflowEventRegistrations.FirstOrDefault(i => i.ChangeType == info.Action && i.Object == sObject && i.ObjectID == info.ObjectTypeID);

                if (registration != null)
                {
                    var workflowItem = workflow.CreateWorkflowItem(registration.TypeID, info.Object.ToString(), info.ObjectID);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.GetFullExceptionData());
            }
        }

        static void Main()
        {
            var config = new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION);
            config.UseServiceBus();
            var host = new JobHost(config);
            host.RunAndBlock();


//            var mex = new List<Exception>();

//            try
//            {
//                var companies = GetActiveCompanyIDs();

//#if DEBUG                       
//                companies = GetActiveCompanyIDs().Where(i => i == 4).ToList();
//#endif
           
//              companies.ForEach(companyID =>
//              {
//                  try
//                  {
//                      using (var context = GetCompanyConnection(companyID))
//                      {
//                          Console.WriteLine($"Getting objects with invalid text paths [company id: {companyID}]");

//                          context.OpenWithRetry(RetryPolicy.DefaultFixed);
//                          var items = context.Query<dynamic>("").ToList();

//                          Console.WriteLine($"Found {items.Count} item(s) with invalid text paths [company id: {companyID}]");

//                          items.ForEach(i => {
//                              try
//                              {
//                                  context.Execute($"update {i.Object} set TextPath = @tp where ID = @id", new { tp = i.CorrectTextPath, id = i.ObjectID });
//                              }
//                              catch (Exception ex)
//                              {
//                                  Console.WriteLine(ex.GetFullExceptionData());
//                              }
//                          });
//                      }
//                  }
//                  catch (Exception ex)
//                  {
//                      Console.WriteLine(ex.GetFullExceptionData());
//                  }

//              });
//            }
//            catch (Exception ex)
//            {
//                mex.Add(ex);
//            }

//            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
