using d360.core.queue;
using d360.extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace igx.jobs.workflowsubscriber
{
	public class WorkflowQueueListener : WorkflowBaseJob
	{
		const string FUNCTION_NAME = "Workflow_Queue_Listener";

		public WorkflowQueueListener(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config, cache, mail, queue) { }

		[FunctionName(FUNCTION_NAME)]
		public async Task Listen([QueueTrigger(constants.Queue.Workflow, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
		{
			var info = JsonConvert.DeserializeObject<EventInfo>(myQueueItem);
			await ProcessMessage(FUNCTION_NAME, info, log);
		}
	}
}
