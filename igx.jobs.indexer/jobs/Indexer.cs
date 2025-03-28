using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace igx.jobs.indexer
{
	public class Indexer
	{        
		[FunctionName("Stub")]
		public void RunViaQueue([QueueTrigger(constants.Queue.Search, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
        {
			// do nothing
		}
    }
}
