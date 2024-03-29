using Azure.Storage.Queues;
using d360.core.exceptions;
using d360.core.queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.events
{
	public class AzureQueueSource : IQueueSource
    {
		public string StorageConnectionString { get; set; } = "";

		private QueueServiceClient cloudClient
        {
            get
            {
                return new QueueServiceClient(StorageConnectionString);
            }
        }

		QueueClient getQueue(string name)
		{
			var queue = cloudClient.GetQueueClient(name);
			queue.CreateIfNotExists();
			return queue;
		}

        public string GetMessageIdFromEventInfo(EventInfo eventInfo)
        {
            if (eventInfo == null)
            {
                throw new ArgumentNullException("eventInfo");
            }

            string messageId = $"C{eventInfo.CompanyID}_A{eventInfo.Action}_W{eventInfo.WorkflowItemID}_S{eventInfo.VersionStepTransitionID}_I{eventInfo.ItemStepID}";

            if (eventInfo.Object != null)
            {
                messageId += $"_O{eventInfo.Object.Object}|{eventInfo.Object.ObjectID}";
            }

            return messageId;
        }

		string encodeMessage<T>(T item)
		{
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item)));
			return encoded;
		}

        public bool CreateMessage<T>(string queueName, T item)
        {
            var queue = getQueue(queueName);
			var response = queue.SendMessage(encodeMessage(item));
            if (string.IsNullOrEmpty(response.Value.PopReceipt))
            {
                throw new InfrastructureException("Queue message has no population receipt and appears to not have been added properly.", "StorageQueue");
            }
			return true;
        }

        public async Task<bool> CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null)
        {
			var queue = getQueue(queueName);
			var response = await queue.SendMessageAsync(encodeMessage(item), initialVisibilityDelay);
            if (string.IsNullOrEmpty(response.Value.PopReceipt))
            {
				throw new InfrastructureException("Queue message has no population receipt and appears to not have been added properly.", "StorageQueue");
			}
            return true;
        }

        public bool CreateMessages<T>(string queueName, List<T> items)
        {
			var queue = getQueue(queueName);
			items.ForEach(item =>
            {
				queue.SendMessage(encodeMessage(item));
            });
            return true;
        }

        public async Task<bool> CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
        {
			var queue = getQueue(queueName);
			while (items.Count > 0)
			{
				var item = items[0];
				await queue.SendMessageAsync(encodeMessage(item));
				items.RemoveAt(0);
			}
            return true;
        }
    }
}
