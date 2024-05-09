using d360.core.entities;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace igx.jobs.workflowdigestprocessor
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class WorkflowDigestProcessor: BaseWebJob
	{
		const string FUNCTION_NAME = "Workflow_DigestProcessor";

#if DEBUG
		const string TIMER_SETTINGS = "*/10 * * * * *";
#else
        const string TIMER_SETTINGS = "0 0 5 * * *"; // every day at 5am
#endif

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;		

		public WorkflowDigestProcessor(ICachingProvider cache, IConfiguration config, IMailProvider mail, IQueueSource queue): base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[Singleton(Mode = SingletonMode.Function)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS)] TimerInfo myTimer, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext executionContext)   
		{
			try
			{
				//add random delay so instances run at an offset to each other.
				var rand = new Random();
				Thread.Sleep(rand.Next(30) * 1000);

				var companies = GetCompaniesByCurrentSlot();
				foreach (var c in companies)
				{
					var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME },
						{ "CompanyID", c.CompanyID },
						{ "UrlPrefix", c.UrlPrefix }
					};

					using (log.BeginScope(logProperties))
					{
						try
						{
							var context = new UriSecurityContextProvider {
								CompanyID = c.CompanyID,
								CompanyPrefix = c.UrlPrefix,
								ResourceID = 0,
								IsAdministrator = true
							};
							using (var community = new CommunityContext(ConnString, Cache, Queue, context))
							{
								using (var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true))
								{
									CompanyDigestExecution lastExecution = community.CompanyDigestExecution.Where(x => x.CompanyID == c.CompanyID).OrderByDescending(x => x.LastExecuted).FirstOrDefault();

									//Check if digest was already sent today
									if (lastExecution != null && lastExecution?.LastExecuted?.DayOfWeek == DateTime.UtcNow.DayOfWeek)
									{
										continue;
									}

									if (lastExecution == null)
									{
										lastExecution = new CompanyDigestExecution
										{
											CompanyID = c.CompanyID,
											InstanceID = executionContext.InvocationId,
											LastExecuted = DateTime.UtcNow,
										};

										community.Add(lastExecution);
									}
									else
									{
										lastExecution.LastExecuted = DateTime.UtcNow;
										lastExecution.InstanceID = executionContext.InvocationId;
										community.Update(lastExecution);
									}

									await company.SendDigestEmails(c.EnvironmentLevel);
								}
							}
							
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error while processing workflow digests for this environment.");
						}
					}
				}
			}
			catch (Exception ex)
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME }
				};

				using (log.BeginScope(logProperties))
				{
					log.LogCritical(ex, "Critical error while running this web job.");
				}
			}
		}
	}
}