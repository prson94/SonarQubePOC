using d360.extensions;
using d360.model;
using d360.utils.company;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class DatabaseTaskConverter : BaseTaskWebJob
	{
		const string FUNCTION_NAME = "DatabaseTaskConverter";
		readonly IQueueSource Queue;

		public DatabaseTaskConverter(IConfiguration config, IQueueSource queue) : base(config)
		{
			Queue = queue;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task RunScheduler([TimerTrigger("*/10 * * * * *", RunOnStartup = true)] TimerInfo myTimer, ILogger log)
		{
			var companies = GetCompaniesByCurrentSlot();
			companies.ForEach(async company =>
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME },
					{ "CompanyID", company.CompanyID },
					{ "UrlPrefix", company.UrlPrefix }
				};

				using (log.BeginScope(logProperties))
				{
					try
					{
						using (var outerCompanyConnection = new SqlConnection(CompanyConnectionUtils.GetConnectionString(company.CompanyID, company.Server, company.Username, company.Password)))
						{
							await outerCompanyConnection.OpenIfClosed();
							//if (HasWork(outerCompanyConnection))
							//{
								//await Queue.CreateFilteredTopicMessageAsync(Configuration["EventBusTopicName"], new TaskMessage(company));
							//}
						}
					}
					catch (Exception ex)
					{
						log.LogError(ex, "Task Processor Failed for company.");
					}
				}
			});
		}
	}
}
