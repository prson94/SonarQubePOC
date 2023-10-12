using d360.core;
using d360.core.entities.Metric;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions.queue;
using d360.model;
using Dapper;
using igx.jobs.scoreprocessor.ChangeTypes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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

		public async static Task Run([QueueTrigger("%ScoringQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, ILogger log)
        {
			var scoreInfo = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);
			var logEvent = new EventId(scoreInfo.CompanyID, $"Score Processor: {scoreInfo.ChangeType}");

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
								await companyConnection.ExecuteAsync(sql, new { payload.AssetUid, EffectiveDate = payload.EffectiveDate.Date, ScoreType = (int)payload.ScoreType }, commandTimeout: 600);
							}
						}
						break;
					case ScoreQueueChangeType.PatchCatalogExecution:
						if (scoreInfo.UseUpdatedScoringEngine)
						{
							sql = getPatchExecutionSql();
							companyConnectionString = getCompanyConnectionString(scoreInfo.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								await companyConnection.ExecuteAsync(sql, new { scoreInfo.ExecutionUid }, commandTimeout: 18000);
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
								await companyConnection.ExecuteAsync(sql, new { versionUid = payload.MetricAssetVersionUid }, commandTimeout: 18000);
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
								await companyConnection.ExecuteAsync(sql, new { versionUid = payload.MetricAssetVersionUid }, commandTimeout: 18000);
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
						if (scoreInfo.UseUpdatedScoringEngine)
						{ 
							var payload = JsonConvert.DeserializeObject<RuleAssetRemovedModel>(scoreInfo.Payload.ToString());
							sql = getRuleRemovedSql();
							companyConnectionString = getCompanyConnectionString(scoreInfo.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								await companyConnection.ExecuteAsync(sql, new { assetUid = payload.AssetUid }, commandTimeout: 18000);
							}
						}
						else
						{
							process = new RuleAssetRemovedProcess();
						}
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
                log.LogError(logEvent, $"No score execution record found. Company: {scoreInfo.CompanyID}; Execution: {scoreInfo.ExecutionUid}.");
            }
            catch (InvalidScoreMeasure ex)
            {
				handleInvalidMeasureError(ex, scoreInfo, log, logEvent);
            }
            catch (ScoresCurrentlyProcessingException ex)
            {
				await handleScoreProcessingError(ex, scoreInfo);
            }
            catch (Exception ex)
            {
				await handleError(ex, scoreInfo, log, logEvent);
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

		static string getCommonTempTable()
		{
			return "create table #ids (RowId int identity, AssetUid uniqueidentifier);";
		}

		static string getCommonLookupSql()
		{
			return @"
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

		static string getMeasureChangedSql()
		{
			return $@"
declare @effectiveDate date = getutcdate(),
		@scoreType int;

{getCommonTempTable()}

select	@scoreType = al.ScoreType
from	metrics.Asset a
		inner join metrics.AssetVersion v on v.AssetUid = a.Uid and v.Uid = @versionUid
		inner join metrics.Allocation al on al.Uid = a.AllocationUid;

insert into #ids
	select	s.AssetUid
	from	metrics.ScoreItem i
			inner join metrics.ScoreItemLink l on l.ScoreItemUid = i.Uid and i.AssetVersionUid = @versionUid
			inner join metrics.Score s on s.Uid = l.ScoreUid;

{getCommonLookupSql()}";
		}

		static string getPatchExecutionSql()
		{
			return $@"
declare @effectiveDate date = getutcdate(),
		@scoreType int = 1; 

{getCommonTempTable()}

insert into #ids (AssetUid)
	select	i.[Uid]
	from	api.ExecutionCatalogItem i
			inner join api.Execution e on e.Id = i.ExecutionId and e.ExecutionID = @ExecutionUid and i.[Type] = 'A' and i.Success = 1
			inner join AssetType t on t.ID = i.TypeId
			inner join metrics.Allocation al on al.AssetTypeUid = t.Uid and al.ScoreType = @scoreType; 

{getCommonLookupSql()}";
		}

		static string getRuleRemovedSql()
		{
			return $@"
declare @effectiveDate date = getutcdate(),
		@scoreType int = 2;

{getCommonTempTable()}

insert into #ids
	select	distinct 
			S.AssetUid
	from	metrics.ScoreItem I
			inner join metrics.AssetVersion V on V.Uid = I.AssetVersionUid
			inner join metrics.Asset A on A.Uid = V.AssetUid
			inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 2
			cross apply openjson(I.Evidence) Ev
			cross apply openjson(Ev.value) Rp 
			cross apply openjson(Rp.value)  with (Uid nvarchar(max) '$.Uid') as P
			inner join metrics.ScoreItemLink SIL on SIL.ScoreItemUid = I.Uid
			inner join metrics.Score S on S.Uid = SIL.ScoreUid
	where	Evidence <> '{"{}"}' 
			and Evidence is not null
			and ISNUMERIC(Ev.[key]) = 1
			and Rp.[key] = 'RollupPath' 
			and P.Uid = @assetUid;

{getCommonLookupSql()}";
		}

		static async Task handleError(Exception ex, ScoreQueueInfo scoreInfo, ILogger log, EventId logEvent)
		{
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
				log.LogWarning(logEvent, ex, ex.Message);

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
				log.LogError(logEvent, ex, ex.Message);
			}


			var execUpdater = new ExecutionUpdater { Info = scoreInfo };
			var closedExecution = await execUpdater.UpdateAsync(ex);

			if (!closedExecution && shouldRequeue)
			{
				var queue = new AzureQueueSource();
				await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, minuteDelay, 0));
			}
		}

		static void handleInvalidMeasureError(InvalidScoreMeasure ex, ScoreQueueInfo scoreInfo, ILogger log, EventId logEvent)
		{
			log.LogError(logEvent, ex, ex.Message);
		}

		static async Task handleScoreProcessingError(ScoresCurrentlyProcessingException ex, ScoreQueueInfo scoreInfo)
		{
			var queue = new AzureQueueSource();
			await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, 0, 30));
		}
	}
}
