using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.graph;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace d360.jobs.subscriber.Graph
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    public class Program : FunctionsBase
    {
        static void Main()
        {
            JobHostConfiguration config = new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION);
            config.UseServiceBus();
            config.NameResolver = new TopicNameResolver();
            var host = new JobHost(config);
            host.RunAndBlock();
        }

        public static async Task ProcessTopicMessage([ServiceBusTrigger("%EventBusTopicName%", "Graph", AccessRights.Listen)] BrokeredMessage message)
        {
            try
            {
                var info = message.GetBody<EventInfo>();

                //only care about intersects added if it is an intersect we need to add
                // the vertices and edges to the graph.

                if (info.Object == null || info.Object.Object != SystemObjects.Intersect) return;

                #region Create EF connection

                var sec = new UriSecurityContextProvider()
                {
                    CompanyID = info.CompanyID,
                    ResourceID = info.ResourceID,
                    CompanyPrefix = info.DomainPrefix,
                    IsAdministrator = true
                };
                var cache = new DummyCachingProvider();
                var queue = new AzureQueueSource();
                var community = new CommunityContext(cache, queue, sec);
                var company = new CompanyContext(community, cache, queue, sec, true);

                #endregion
                // run a query that gets intersect info

                
                var graphDatabase = new CosmosGraphProvider();

                if (info.Action == core.enums.Workflow.ChangeType.Add)
                {
                    var sql =
                        @"select
	                    [utility].[GetObjectName] (I.Subject, I.SubjectID) as SubjectName
	                    , I.Subject
	                    , I.SubjectID
	                    , [utility].[GetObjectName] (I.Object, I.ObjectID) as ObjectName
	                    , I.Object
	                    , I.ObjectID
	                    , p.name as Predicate
                    from
	                    [intersect] i
	                    left outer join intersecttype it on (i.intersecttypeid = it.id)
	                    left outer join [predicate] p on (it.predicateid = p.id)
                    where i.id = @intersectId";
                                        
                    var res = company.Query<dynamic>(sql, new { intersectId = info.Object.ObjectID }).FirstOrDefault();

                    if(res == null)
                    {
                        Console.WriteLine($"INVALID RELATIONSHIP ID SPECIFIED, CANNOT LOAD RELATION DATA");

                        return;
                    }

                    var subjectId = $"{res.Subject}|{res.SubjectID}";
                    var objectId = $"{res.Object}|{res.ObjectID}";

                    await graphDatabase.AddVertex(info.CompanyID, subjectId, res.Subject, new Dictionary<string, string> { { "name", res.SubjectName } });
                    await graphDatabase.AddVertex(info.CompanyID, objectId, res.Object, new Dictionary<string, string> { { "name", res.ObjectName } });

                    //add connection between the two vertices
                    await graphDatabase.AddEdge(info.CompanyID, subjectId, objectId, res.Predicate, new Dictionary<string, string> { { "intersectId", info.Object.ObjectID.ToString() } });
                }
                else if(info.Action == core.enums.Workflow.ChangeType.Delete)
                {
                    //delete edges related to the deleted intersect id need to find way to do this by predicate cause we are droping all edges now...
                    await graphDatabase.DeleteEdge(info.CompanyID, "intersectId",info.Object.ObjectID.ToString());
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.GetFullExceptionData());
            }
        }
    }
}
