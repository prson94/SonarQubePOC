using Azure.Messaging.ServiceBus;
using Azure.Storage.Queues;
using d360.core.enums.Workflow;
using d360.core.queue;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.events
{
	public class AzureQueueSource : IQueueSource
    {
		public string QueuesConnectionString { get; set; } = "";
		public string EventServiceBusConnectionString { get; set; } = "";
		public string EventBusTopicName { get; set; } = "";

		// Clients are thread safe and designed to be used with DI or singleton patterns.
		private static ServiceBusClient SERVICE_BUS_CLIENT;
        private static ConcurrentDictionary<string, ServiceBusSender> SERVICE_BUS_SENDERS;

		private QueueServiceClient cloudClient
        {
            get
            {
                return new QueueServiceClient(QueuesConnectionString);
            }
        }

        private ServiceBusMessage GetFilteredServiceBusMessage(IFilteredServiceBusMessage o)
        {
            var bm = GetServiceBusMessageFromObject(o);

            if (!string.IsNullOrEmpty(o.EventType))
            {
                bm.ApplicationProperties.Add("EventType", o.EventType);
            }

            return bm;
        }

        private ServiceBusMessage GetServiceBusMessageFromObject(object o)
        {
            var eString = JsonConvert.SerializeObject(o);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new ServiceBusMessage(new BinaryData(eBytes))
            {
                MessageId = Guid.NewGuid().ToString()
            };

            return bm;
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
            var queue = cloudClient.GetQueueClient(queueName);
			var response = queue.SendMessage(encodeMessage(item));
            if (string.IsNullOrEmpty(response.Value.PopReceipt))
            {
                throw new ApplicationException("Queue message has no population receipt and appears to not have been added properly.");
            }
            
			return true;
        }

        public async Task<bool> CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null)
        {
            try
            {
				var queue = cloudClient.GetQueueClient(queueName);
				var response = await queue.SendMessageAsync(JsonConvert.SerializeObject(item), initialVisibilityDelay);
                if (string.IsNullOrEmpty(response.Value.PopReceipt))
                {
                    throw new Exception("Queue message has no population receipt and appears to not have been added properly.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occurred trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
                return false;
            }
            return true;
        }

        public bool CreateMessages<T>(string queueName, List<T> items)
        {
            try
            {
				var queue = cloudClient.GetQueueClient(queueName);
				items.ForEach(item =>
                {
					queue.SendMessage(JsonConvert.SerializeObject(item));
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occurred trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
                return false;
            }
            return true;
        }

        public async Task<bool> CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
        {
            try
            {
				var queue = cloudClient.GetQueueClient(queueName);
				await Task.Run(() =>
                {
					items.ForEach(item =>
					{
						queue.SendMessage(JsonConvert.SerializeObject(item));
					});
				});
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occurred trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
                return false;
            }
            return true;
        }

        public void CreateTopicMessage(EventInfo e)
        {
            var bm = GetServiceBusMessageFromObject(e);

            var sender = CreateServiceBusSender(EventBusTopicName);
            sender.SendMessageAsync(bm).Wait();
        }

        public void CreateTopicMessage(string topicName, EventInfo e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            sender.SendMessageAsync(bm).Wait();
        }

        public async Task CreateTopicMessageAsync(EventInfo e)
        {
            await CreateTopicMessageAsync(EventBusTopicName, e);
        }

        public async Task CreateTopicMessageAsync(string topicName, EventInfo e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            await sender.SendMessageAsync(bm);
        }

        public async Task CreateScheduledTopicMessageAsync(EventInfo e, DateTimeOffset delay)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(EventBusTopicName);
            await sender.ScheduleMessageAsync(bm, delay);
        }

        public void CreateTopicMessages(List<EventInfo> events)
        {
            CreateTopicMessages(EventBusTopicName, events);
        }

        public void CreateTopicMessages(string topicName, List<EventInfo> events)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var e in events)
            {
                var msg = GetServiceBusMessageFromObject(e);
                messages.Enqueue(msg);
                msg.MessageId = GetMessageIdFromEventInfo(e);

                if (e.Action == ChangeType.Add || e.Action == ChangeType.Update) //delay the processing if add or edit so update has chance to process
                {
                    msg.ScheduledEnqueueTime = DateTime.UtcNow.AddSeconds(15);
                }
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = sender.CreateMessageBatchAsync().Result)
                {

                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    sender.SendMessagesAsync(batch).Wait();
                }
            }
        }

        private ServiceBusSender CreateServiceBusSender(string topicName)
        {
            if (SERVICE_BUS_CLIENT == null)
            {
                SERVICE_BUS_CLIENT = new ServiceBusClient(EventServiceBusConnectionString);
            }

            if (SERVICE_BUS_SENDERS == null)
            {
                SERVICE_BUS_SENDERS = new ConcurrentDictionary<string, ServiceBusSender>();
            }

            if (!SERVICE_BUS_SENDERS.ContainsKey(topicName))
            {
                SERVICE_BUS_SENDERS.TryAdd(topicName, SERVICE_BUS_CLIENT.CreateSender(topicName));
            }

            return SERVICE_BUS_SENDERS[topicName];
        }

        public async Task CreateTopicMessagesAsync(List<EventInfo> events)
        {
            await CreateTopicMessagesAsync(EventBusTopicName, events);
        }

        public async Task CreateTopicMessagesAsync(string topicName, List<EventInfo> events)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var @event in events)
            {
                messages.Enqueue(GetServiceBusMessageFromObject(@event));
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = await sender.CreateMessageBatchAsync())
                {

                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    await sender.SendMessagesAsync(batch);
                }
            }
        }

        public void CreateTopicMessage<T>(string topicName, T e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            sender.SendMessageAsync(bm).Wait();
        }

        public async Task CreateTopicMessageAsync<T>(string topicName, T e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            await sender.SendMessageAsync(bm);
        }

        public async Task CreateFilteredTopicMessageAsync(string topicName, IFilteredServiceBusMessage e)
        {
            var bm = GetFilteredServiceBusMessage(e);
            var sender = CreateServiceBusSender(topicName);
            await sender.SendMessageAsync(bm).ConfigureAwait(false);
        }

        public void CreateTopicMessages<T>(string topicName, List<T> events, DateTime? scheduledEnqueueTime = null)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var @event in events)
            {
                var msg = GetServiceBusMessageFromObject(@event);
                if (scheduledEnqueueTime.HasValue)
                {
                    msg.ScheduledEnqueueTime = scheduledEnqueueTime.Value;
                }
                messages.Enqueue(msg);
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = sender.CreateMessageBatchAsync().Result)
                {

                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    sender.SendMessagesAsync(batch).Wait();
                }
            }
        }

        public async Task CreateTopicMessagesAsync<T>(string topicName, List<T> events)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var @event in events)
            {
                messages.Enqueue(GetServiceBusMessageFromObject(@event));
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = await sender.CreateMessageBatchAsync())
                {

                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    await sender.SendMessagesAsync(batch);
                }
            }
        }
    }
}
