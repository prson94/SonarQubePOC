using d360.core.entities;
using d360.core.queue;

namespace igx.jobs.apiexecutionprocessor
{
	public class TaskMessage : IFilteredServiceBusMessage
	{
		public TaskMessage(CompanyWithDatabaseServerSettings company)
		{
			Company = company;
		}
		public CompanyWithDatabaseServerSettings Company { get; set; }
		public string EventType { get; set; } = "DatabaseTask";
	}
}
