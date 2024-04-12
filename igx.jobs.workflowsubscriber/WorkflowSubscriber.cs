using Azure.Messaging.ServiceBus;
using d360.core.queue;
using d360.extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.workflowsubscriber
{
	public class WorkflowSubscriber : WorkflowBaseJob
	{
		const string FUNCTION_NAME = "Workflow_Topic_Subscriber";
		public WorkflowSubscriber(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config, cache, mail, queue) { }

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([ServiceBusTrigger("%EventBusTopicName%", "Workflow", Connection = "EventServiceBus")] ServiceBusReceivedMessage brokeredMessage, ILogger log)
		{
			var messageString = Encoding.UTF8.GetString(brokeredMessage.Body.ToArray());
			var info = JsonConvert.DeserializeObject<EventInfo>(messageString);
			await ProcessMessage(FUNCTION_NAME, info, log);
		}

	}
}
