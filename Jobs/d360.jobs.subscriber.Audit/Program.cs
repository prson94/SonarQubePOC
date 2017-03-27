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
    class Program: FunctionsBase
    {
        static void ProcessTopicMessage([ServiceBusTrigger("Events", "Audit", AccessRights.Listen)] BrokeredMessage message)
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

        static void Main()
        {
            var config = new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION);
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
