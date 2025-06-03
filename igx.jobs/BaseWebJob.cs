using d360.core.entities;
using d360.core.enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs
{
	public class BaseWebJob
	{
		public IConfiguration Configuration { get; private set; }
		
		public ICommunity Community;

		public BaseWebJob(ICommunity community, IConfiguration configuration)
		{
			Community = community;
			Configuration = configuration;
		}

		public EnvironmentLevel GetEnvironmentLevelCurrentSlot()
		{
			var environment = Configuration["Environment"];
			EnvironmentLevel lvl;

			switch (environment)
			{
				case "NIGHTLY":
					lvl = EnvironmentLevel.Nightly;
					break;
				case "CLIENTDEV":
					lvl = EnvironmentLevel.Development;
					break;
				case "UAT":
					lvl = EnvironmentLevel.UAT;
					break;
				case "PROD":
					lvl = EnvironmentLevel.Production;
					break;
				default:
					lvl = EnvironmentLevel.Nightly;
					break;
			}

			return lvl;
		}

		public async Task LoopThroughTenantsAsync(ILogger log, string functionName, Func<CompanyWithDatabaseServerSettings, Task> action)
		{
			var slot = GetEnvironmentLevelCurrentSlot();
			var tenants = await Community.ReadTenantConnectionSettingsByCurrentSlotAsync(slot);
			foreach (var c in tenants.OrderBy(t => t.Priority))
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", functionName },
					{ "CompanyID", c.CompanyID },
					{ "UrlPrefix", c.UrlPrefix }
				};
				using (log.BeginScope(logProperties))
				{
					try
					{
						await action(c);
					}
					catch (Exception ex)
					{
						log.LogCritical(ex, "Web job failed.");
					}
				}
			}
		}
	}
}
