using d360.core;
using d360.core.entities.Metric;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.model;
using d360.utils.company;
using Dapper;
using igx.jobs.scoreprocessor.ChangeTypes;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
    public static class ScoringQueueProcessor
    {
        const string FUNCTION_NAME = "Scoring_QueueProcessor";
		
		static string GetCompanyConnectionString(int companyID)
		{
			string connectionString = "";

			using (var cnn = new SqlConnection(Environment.GetEnvironmentVariable("CommunityContext")))
			{
				if (cnn.State != System.Data.ConnectionState.Open)
				{
					cnn.Open();
				}

				var company = cnn.Query<dynamic>(
					@"select  ds.Server, ds.Username, ds.Password from company c inner join databaseserver ds on c.databaseserverid = ds.id and c.Id = @companyID",
					new { companyID }
				).FirstOrDefault();

				if (company != null)
				{
					connectionString = CompanyConnectionStringHelper.ConnectionString(companyID, company.Server, company.Username, company.Password);
				}
			}

			return connectionString;
		}

		public async static Task Run([QueueTrigger("%ScoringQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
			var scoreInfo = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);

			try
			{
				string sql = "";
				string companyConnectionString = "";

				IScoreProcess process = null;

				switch (scoreInfo.ChangeType)
				{
					case ScoreQueueChangeType.AssetMeasures:
						process = new AssetMeasuresProcess();
						break;
					case ScoreQueueChangeType.CheckTypeDependencyRemoved:
						process = new CheckTypeDependencyRemovedProcess();
						break;
					case ScoreQueueChangeType.MeasureChanged:
						process = new MeasureChangedProcess();
						break;
					case ScoreQueueChangeType.MeasureRemoved:
						if (scoreInfo.UseUpdatedScoringEngine)
						{
							var payload = scoreInfo.Payload as MeasureRemovedModel;
							sql = @"
declare @effectiveDate date = getutcdate(),
		@scoreType int

select	@scoreType = al.ScoreType
from	metrics.Asset a
		inner join metrics.AssetVersion v on v.AssetUid = a.Uid and v.Uid = @versionUid
		inner join metrics.Allocation al on al.Uid = a.AllocationUid
create table #ids (RowId int identity, AssetUid uniqueidentifier)

insert into #ids
	select	s.AssetUid
	from	metrics.ScoreItem i
			inner join metrics.ScoreItemLink l on l.ScoreItemUid = i.Uid and i.AssetVersionUid = @versionUid
			inner join metrics.Score s on s.Uid = l.ScoreUid

declare @current int = 1,
		@max int,
		@currentAssetUid uniqueidentifier;

select @max = max(RowId) from #ids
while @current <= @max
begin
	select @currentAssetUid = from #ids where RowId = @current
	exec metrics.GenerateScore @currentAssetUid, @effectiveDate, @scoreType
end";
							companyConnectionString = GetCompanyConnectionString(scoreInfo.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								await companyConnection.ExecuteAsync(sql, new { versionUid = payload.MetricAssetVersionUid });
							}
							//Execute the SQL.
						}
						else 
						{
							process = new MeasureRemovedProcess();
						}
						break;
					case ScoreQueueChangeType.RollupPathChanged:
						process = new RollupPathChangedProcess();
						break;
					case ScoreQueueChangeType.RuleAssetRemoved:
						process = new RuleAssetRemovedProcess();
						break;
					case ScoreQueueChangeType.WorkflowCheck:
						process = new WorkflowCheckProcess();
						break;
				}		

				if (process != null)
				{
					process.Info = scoreInfo;
					await process.Run();
				}
            }
            catch (ArgumentNullException)
            {
                log.WriteLine($"No score execution record found. Company: {scoreInfo.CompanyID}; Execution: {scoreInfo.ExecutionUid}.");
            }
            catch (InvalidScoreMeasure ex)
            {
                var props = new Dictionary<string, string>() {
                        { "ExecutionUid", scoreInfo.ExecutionUid.ToString() },
                        { "ChangeType", scoreInfo.ChangeType.ToString() }
                    };

                CoreFunction.AITrackException(FUNCTION_NAME, ex, scoreInfo.CompanyID, props);
            }
            catch (ScoresCurrentlyProcessingException)
            {
                var queue = new AzureQueueSource();
                await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, 0, 30));
                queue = null;
            }
            catch (Exception ex)
            {
                var props = new Dictionary<string, string>() {
                        { "ExecutionUid", scoreInfo.ExecutionUid.ToString() },
                        { "ChangeType", scoreInfo.ChangeType.ToString() }
                    };

                bool warningOnly = false;
                if (scoreInfo.ChangeType == ScoreQueueChangeType.RollupPathChanged)
                {
                    if (ex is System.Data.SqlClient.SqlException && ex.Message.Contains("deadlock"))
                    {
                        warningOnly = true;
                    }
                }

                int minuteDelay = 5;
                bool shouldRequeue = true;
                if (warningOnly)
                {
                    CoreFunction.AITrackEvent(FUNCTION_NAME, "Rollup Deadlock", props, scoreInfo.CompanyID);
                    
                    // Only requeue 1 out of 3 times as there are rapid changes that will put many messages on the queue.
                    Random random = new Random();
                    int ans = random.Next(1, 12);
                    if (ans % 3 > 0)
                    {
                        shouldRequeue = false;
                    }

                    minuteDelay = random.Next(15, 45);
                }
                else
                {
                    CoreFunction.AITrackException(FUNCTION_NAME, ex, scoreInfo.CompanyID, props);
                }
                

                var execUpdater = new ExecutionUpdater { Info = scoreInfo };
                var closedExecution = await execUpdater.UpdateAsync(ex);

                if (!closedExecution && shouldRequeue)
                {
                    var queue = new AzureQueueSource();
                    await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, minuteDelay, 0));
                    queue = null;
                }
            }
        }
    }
}
