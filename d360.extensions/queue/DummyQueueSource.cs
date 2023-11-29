using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.extensions.queue
{
	public class DummyQueueSource : IQueueSource
	{
		public Task CreateFilteredTopicMessageAsync(string topicName, IFilteredServiceBusMessage e)
		{
			return Task.CompletedTask;
		}

		public bool CreateMessage<T>(string queueName, T item)
		{
			return true;
		}

		public Task<bool> CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null)
		{
			return Task.FromResult(true);
		}

		public bool CreateMessages<T>(string queueName, List<T> items)
		{
			return true;
		}

		public Task<bool> CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
		{
			return Task.FromResult(true);
		}

		public Task CreateScheduledTopicMessageAsync(EventInfo e, DateTimeOffset delay)
		{
			return Task.CompletedTask;
		}

		public void CreateTopicMessage(EventInfo e)
		{
			
		}

		public void CreateTopicMessage(string topicName, EventInfo e)
		{
			
		}

		public void CreateTopicMessage<T>(string topicName, T e)
		{
			
		}

		public Task CreateTopicMessageAsync(EventInfo e)
		{
			return Task.CompletedTask;
		}

		public Task CreateTopicMessageAsync(string topicName, EventInfo e)
		{
			return Task.CompletedTask;
		}

		public Task CreateTopicMessageAsync<T>(string topicName, T e)
		{
			return Task.CompletedTask;
		}

		public void CreateTopicMessages(List<EventInfo> events)
		{
			
		}

		public void CreateTopicMessages(string topicName, List<EventInfo> events)
		{
			
		}

		public void CreateTopicMessages<T>(string topicName, List<T> events, DateTime? scheduledEnqueueTime = null)
		{
			
		}

		public Task CreateTopicMessagesAsync(List<EventInfo> events)
		{
			return Task.CompletedTask;
		}

		public Task CreateTopicMessagesAsync(string topicName, List<EventInfo> events)
		{
			return Task.CompletedTask;
		}

		public Task CreateTopicMessagesAsync<T>(string topicName, List<T> events)
		{
			return Task.CompletedTask;
		}

		public string GetMessageIdFromEventInfo(EventInfo eventInfo)
		{
			return string.Empty;
		}
	}
}
