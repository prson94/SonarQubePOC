using d360.core;
using d360.core.entities;
using d360.core.queue;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Linq;

namespace d360.jobs.subscriber.Audit
{
    public class Program: FunctionsBase
    {
        static void Main()
        {
            JobHostConfiguration config = new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION);
            config.UseServiceBus();
            config.NameResolver = new TopicNameResolver();
            var host = new JobHost(config);
            host.RunAndBlock();
        }

        public static void ProcessTopicMessage([ServiceBusTrigger("%EventBusTopicName%", "Audit", AccessRights.Listen)] BrokeredMessage message)
        {
            try
            {
                var info = message.GetBody<EventInfo>();

                if (info.Object.Object.IsAuditEnabled())
                {
                    var cnn = GetCompanyConnection(info.CompanyID);
                    cnn.Query<Field>("select * from Field where ObjectType = @type and ObjectID = @id", new { type = new Dapper.DbString { IsAnsi = true, IsFixedLength = true, Length = 50, Value = info.Object.Object.ToString() }, id = info.Object.ObjectID }).ToList();

                    switch (info.Object.Object)
                    {
                        case SystemObjects.Artifact:
                            break;
                        case SystemObjects.ArtifactType:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.GetFullExceptionData());
            }
        }
    }
}
