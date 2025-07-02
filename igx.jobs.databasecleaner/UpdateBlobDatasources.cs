using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using repositories;
using repositories.azure;
using System.Threading.Tasks;

namespace igx.jobs.databasecleaner
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class UpdateBlobDatasources : BaseWebJob
	{
		const string FUNCTION_NAME = "UpdateBlobDatasources";
		const string TIMER_SETTINGS = "0 0 */6 * * *";

		public UpdateBlobDatasources(IConfiguration config, ICommunity community) : base(community, config) { }

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)] TimerInfo myTimer, ILogger log)
		{
			await LoopThroughTenantsAsync(log, FUNCTION_NAME, async c => {
				IWorkspaces workspace = new Workspaces(
					new DapperConnectionProvider { ReadOnlyConnectionString = $"{c.GetConnectionString(true)}", ReadWriteConnectionString = c.GetConnectionString() }
					);

				var config = c.GetBlobConfigurationModel();
				await workspace.UpsertBlobDataSourcesAsync(config);
			});
		}
	}
}