using d360.core;
using d360.core.entities.Metric;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions.queue;
using d360.model;
using Dapper;
using igx.jobs.scoreprocessor.ChangeTypes;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
	public static class ScoringQueueProcessor
    {
        const string FUNCTION_NAME = "Scoring_QueueProcessor";

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
					case ScoreQueueChangeType.RescoreRequest:
						if (scoreInfo.UseUpdatedScoringEngine)
						{
							var payload = JsonConvert.DeserializeObject<AssetRescoreRequestModel>(scoreInfo.Payload.ToString());
							sql = "exec metrics.GenerateScore @AssetUid, @EffectiveDate, @scoreType";
							companyConnectionString = getCompanyConnectionString(scoreInfo.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								await companyConnection.ExecuteAsync(sql, new { payload.AssetUid, EffectiveDate = payload.EffectiveDate.Date, ScoreType = (int)payload.ScoreType });
							}
						}
						break;
					case ScoreQueueChangeType.AssetMeasures:
						process = new AssetMeasuresProcess();
						break;
					case ScoreQueueChangeType.CheckTypeDependencyRemoved:
						process = new CheckTypeDependencyRemovedProcess();
						break;
					case ScoreQueueChangeType.MeasureChanged:
						if (scoreInfo.UseUpdatedScoringEngine)
						{
							var payload = JsonConvert.DeserializeObject<MeasureChangedModel>(scoreInfo.Payload.ToString());
							sql = getMeasureChangedSql();
							companyConnectionString = getCompanyConnectionString(scoreInfo.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								await companyConnection.ExecuteAsync(sql, new { versionUid = payload.MetricAssetVersionUid });
							}
						}
						else
						{
							process = new MeasureChangedProcess();
						}
						break;
					case ScoreQueueChangeType.MeasureRemoved:
						if (scoreInfo.UseUpdatedScoringEngine)
						{
							var payload = JsonConvert.DeserializeObject<MeasureRemovedModel>(scoreInfo.Payload.ToString());
							sql = getMeasureChangedSql();
							companyConnectionString = getCompanyConnectionString(scoreInfo.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								await companyConnection.ExecuteAsync(sql, new { versionUid = payload.MetricAssetVersionUid });
							}
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
				handleInvalidMeasureError(ex, scoreInfo);
            }
            catch (ScoresCurrentlyProcessingException ex)
            {
				await handleScoreProcessingError(ex, scoreInfo);
            }
            catch (Exception ex)
            {
				await handleError(ex, scoreInfo);
            }
        }

		static string getCompanyConnectionString(int companyID)
		{
			string communityConnectionString = "";
#if DEBUG
			communityConnectionString = ConfigurationManager.AppSettings["CommunityContext"];
#else
			communityConnectionString = Environment.GetEnvironmentVariable("CommunityContext");
#endif
			string connectionString = "";

			using (var cnn = new SqlConnection(communityConnectionString))
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

		static string getMeasureChangedSql()
		{
			return @"
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
	select @currentAssetUid = AssetUid from #ids where RowId = @current
	exec metrics.GenerateScore @currentAssetUid, @effectiveDate, @scoreType
end";
		}

		static async Task handleError(Exception ex, ScoreQueueInfo scoreInfo)
		{
			var props = new Dictionary<string, string>() {
						{ "ExecutionUid", scoreInfo.ExecutionUid.ToString() },
						{ "ChangeType", scoreInfo.ChangeType.ToString() }
					};

			bool warningOnly = false;
			if (scoreInfo.ChangeType == ScoreQueueChangeType.RollupPathChanged)
			{
				if (ex is SqlException && ex.Message.Contains("deadlock"))
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
			}
		}

		static void handleInvalidMeasureError(InvalidScoreMeasure ex, ScoreQueueInfo scoreInfo)
		{
			var props = new Dictionary<string, string>() {
						{ "ExecutionUid", scoreInfo.ExecutionUid.ToString() },
						{ "ChangeType", scoreInfo.ChangeType.ToString() }
					};

			CoreFunction.AITrackException(FUNCTION_NAME, ex, scoreInfo.CompanyID, props);
		}

		static async Task handleScoreProcessingError(ScoresCurrentlyProcessingException ex, ScoreQueueInfo scoreInfo)
		{
			var queue = new AzureQueueSource();
			await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, 0, 30));
		}
	}
}
