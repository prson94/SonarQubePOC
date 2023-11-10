using d360.core;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace igx.jobs.workflowdigestprocessor
{
	class Program
	{
		static async Task Main()
		{
			var builder = CoreFunction.JobHostConfigBuilder();
			builder.ConfigureWebJobs(c =>
			{
				c.AddAzureStorageCoreServices()
				.AddAzureStorage()
				.AddTimers();
			});

			builder.ConfigureServices(services =>
			{
				services.AddSingleton<LaunchDarkly.Sdk.Server.LdClient>(x =>
				{
					return ActivatorUtilities.CreateInstance<LaunchDarkly.Sdk.Server.LdClient>(x, Config.GetValue<string>("LaunchDarklySdkKey"));
				});
			});

			using (var host = builder.Build())
			{
				await host.RunAsync();
			}
		}
	}

	public class WorkflowDigestProcessor
	{
		readonly LaunchDarkly.Sdk.Server.LdClient LdClient;
		const string FUNCTION_NAME = "Workflow_DigestProcessor";

#if DEBUG
		const string TIMER_SETTINGS = "*/10 * * * * *";
#else
        const string TIMER_SETTINGS = "0 0 5 * * *"; // every day at 5am
#endif

		public WorkflowDigestProcessor(LaunchDarkly.Sdk.Server.LdClient ldc)
		{
			this.LdClient = ldc;
		}

		public async Task Run([TimerTrigger(TIMER_SETTINGS)] TimerInfo myTimer, ILogger log)   
		{
			try
			{
				var companies = CoreFunction.GetCompaniesByCurrentSlot();

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
							// Create EF connection
							var company = JobDbContextCreator.CreateCompanyContext(
								new UriSecurityContextProvider
								{
									CompanyID = c.CompanyID,
									CompanyPrefix = c.UrlPrefix,
									ResourceID = 0,
									IsAdministrator = true
								},
								new MandrillMailProvider
								{
									ApiKey = ConfigurationManager.AppSettings[constants.MAIL_API_KEY],
									SubAccount = ConfigurationManager.AppSettings[constants.MAIL_SUB_ACCOUNT]
								},
								new AzureQueueSource(),
								new DummyCachingProvider(),
								constants.COMMUNITY_DATABASE_CONNECTION);


							company.FeatureFlags_TEMP_ASSIGNMENTS = LdClient.BoolVariation(FeatureFlags.TEMP_ASSIGNMENTS, company.GetSdkFeatureFlagUser(), false);

							await company.SendDigestEmails(c.EnvironmentLevel);
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
			finally 
			{
				CoreFunction.AIFlush();
			}
		}
	}
}