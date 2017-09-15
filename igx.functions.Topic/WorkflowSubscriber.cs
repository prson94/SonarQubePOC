using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System.Configuration;

namespace igx.functions.Topic
{
    public class TopicNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }
    }

    public static class WorkflowSubscriber
    {
        const string functionName = "WorkflowSubscriber";
        
        [FunctionName(functionName)]
        public static void Run([ServiceBusTrigger("%EventBusTopicName%", "Workflow", AccessRights.Manage)]string mySbMsg, TraceWriter log)
        {
            log.Info($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
        }
    }
}
