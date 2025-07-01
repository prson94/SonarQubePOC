using Azure.Storage.Blobs;
using Azure.Storage.Queues;

string connectionString = "UseDevelopmentStorage=true";

List<string> containers = ["api-execution", "bulk-loads", "resoruces", "themes"];
var containerClient = new BlobServiceClient(connectionString);
containers.ForEach(containerName => 
{
	containerClient.GetBlobContainerClient(containerName).CreateIfNotExists();
});

List<string> queues = [
	"asset-type-change", "bulk-load", "display-value", "execution", "notification", "parse-owner-rule", "post-execution", 
	"score", "search", "security-policy", "workflow"
];
var queueClient = new QueueServiceClient(connectionString);
queues.ForEach(queueName => 
{
	queueClient.GetQueueClient(queueName).CreateIfNotExists();
});