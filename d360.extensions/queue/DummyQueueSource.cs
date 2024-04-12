using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.extensions.queue
{
	public class DummyQueueSource : IQueueSource
	{
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

		public string GetMessageIdFromEventInfo(EventInfo eventInfo)
		{
			return "";
		}
	}
}
