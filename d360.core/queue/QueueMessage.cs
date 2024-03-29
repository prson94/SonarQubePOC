namespace d360.core.queue
{
	public class QueueMessage<T>
	{
		public int CompanyId { get; set; }
		public string CompanyPrefix { get; set; }
		public T Payload { get; set; }
	}
}
