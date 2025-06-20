using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace monolith.Processor.Tasks;

public class BulkLoader
{
    private readonly ILogger<BulkLoader> Logger;

    public BulkLoader(ILogger<BulkLoader> logger)
    {
		Logger = logger;
    }

    [Function(nameof(BulkLoader))]
    public void Run([QueueTrigger(constants.Queue.Execution, Connection = constants.Setting.Storage)] QueueMessage message)
    {
		//Logger.LogMetric("QueueMessageCount", 1, new Dictionary<string, object> { { "QueueName", "executions" } });
		//var meter = new Meter("OTel.Govern.QueueMessageCount");
		//Counter<long> queueCounter = meter.CreateCounter<long>("ExecutionMessages");
		//queueCounter.Add(1);
		//Azure.Data.Tables.TableClient tableClient = new Azure.Data.Tables.TableClient(message.MessageText, constants.Table.Execution, new Azure.Data.Tables.TableClientOptions { Retry = { MaxRetries = 3 } });

		Logger.LogInformation("C# Queue trigger function processed: {messageText}", message.MessageText);
    }
}