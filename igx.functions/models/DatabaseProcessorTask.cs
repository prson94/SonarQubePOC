using d360.core.entities;
using d360.core.queue;

namespace igx.functions.consumption.models
{
	public class DatabaseProcessorTask : IFilteredServiceBusMessage
	{
		public DatabaseProcessorTask(CompanyWithDatabaseServerSettings company)
		{
			Company = company;
		}
		public CompanyWithDatabaseServerSettings Company { get; set; }
		public string EventType { get; set; } = "DatabaseTask";
	}
}
