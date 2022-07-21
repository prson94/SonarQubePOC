using d360.core.queue;
using System;

public enum BatchApiEventAction
{
	Started,
	Completed
}

public class BatchApiEvent : IFilteredServiceBusMessage
{
	public BatchApiEventAction Action { get; set; }
	public Guid ExecutionID { get; set; }
	public int CompanyID { get; set; }
	public string CompanyDomainPrefix { get; set; }
	public string EventType { get; set; } = "BatchApiEvent";
}