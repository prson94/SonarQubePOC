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
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
	public static class ScoringQueueProcessor
    {
        const string FUNCTION_NAME = "Scoring_QueueProcessor";

		public async static Task Run([QueueTrigger("%ScoringQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, ILogger log)
        {
			var info = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);
			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", info.CompanyID },
				{ "ExecutionId", info.ExecutionUid },
				{ "ChangeType", info.ChangeType.ToString() }
			};

			using (log.BeginScope(logProperties))
			{
				try
				{
					string sql = "";
					string companyConnectionString = "";

					IScoreProcess process = null;

					switch (info.ChangeType)
					{
						case ScoreQueueChangeType.RescoreRequest:
							if (info.UseUpdatedScoringEngine)
							{
								var payload = JsonConvert.DeserializeObject<AssetRescoreRequestModel>(info.Payload.ToString());
								sql = "exec metrics.GenerateScore @AssetUid, @EffectiveDate, @scoreType";
								companyConnectionString = getCompanyConnectionString(info.CompanyID);
								using (var companyConnection = new SqlConnection(companyConnectionString))
								{
									await companyConnection.OpenIfClosed();
									await companyConnection.ExecuteAsync(sql, new { payload.AssetUid, EffectiveDate = payload.EffectiveDate.Date, ScoreType = (int)payload.ScoreType }, commandTimeout: 600);
								}
							}
							break;
						case ScoreQueueChangeType.PatchCatalogExecution:
							if (info.UseUpdatedScoringEngine)
							{
								sql = getPatchExecutionSql();
								companyConnectionString = getCompanyConnectionString(info.CompanyID);
								using (var companyConnection = new SqlConnection(companyConnectionString))
								{
									await companyConnection.OpenIfClosed();
									await companyConnection.ExecuteAsync(sql, new { info.ExecutionUid }, commandTimeout: 18000);
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
							if (info.UseUpdatedScoringEngine)
							{
								var payload = JsonConvert.DeserializeObject<MeasureChangedModel>(info.Payload.ToString());
								sql = getMeasureChangedSql();
								companyConnectionString = getCompanyConnectionString(info.CompanyID);
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
							if (info.UseUpdatedScoringEngine)
							{
								var payload = JsonConvert.DeserializeObject<MeasureRemovedModel>(info.Payload.ToString());
								sql = getMeasureChangedSql();
								companyConnectionString = getCompanyConnectionString(info.CompanyID);
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
							if (info.UseUpdatedScoringEngine)
							{ 
								var payload = JsonConvert.DeserializeObject<RuleAssetRemovedModel>(info.Payload.ToString());
								sql = getRuleRemovedSql();
								companyConnectionString = getCompanyConnectionString(info.CompanyID);
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
						default:
							// no action found.
							break;
					}		

					if (process != null)
					{
						process.Info = info;
						await process.Run();
					}
				}
				catch (ArgumentNullException ex)
				{
					log.LogError(ex, $"No score execution record found.");
				}
				catch (InvalidScoreMeasure ex)
				{
					log.LogError(ex, "Attempting to process an invalid score measure.");
				}
				catch (ScoresCurrentlyProcessingException ex)
				{
					await handleScoreProcessingError(info);
				}
				catch (Exception ex)
				{
					log.LogError(ex, ex.Message);
				}
			}

			CoreFunction.AIFlush();
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
	set @current = @current + 1
end";
		}

		static string getMeasureChangedSql()
		{
			return $@"
declare @effectiveDate date = getutcdate(),
		@scoreType int,
		@conditionsJson nvarchar(max),
		@matchConditionsOnly bit,
		@assetTypeId int;

{getCommonTempTable()}

select	@scoreType = al.ScoreType,
		@matchConditionsOnly = v.MatchConditionsOnly,
		@conditionsJson = metrics.BuildConditionJson(v.Uid),
		@assetTypeId = t.ID
from	metrics.Asset a
		inner join metrics.AssetVersion v on v.AssetUid = a.Uid and v.Uid = @versionUid
		inner join metrics.Allocation al on al.Uid = a.AllocationUid
		inner join AssetType t on t.Uid = al.AssetTypeUid;

insert into #ids
	select	a.Uid
	from	Asset a
			cross apply metrics.ConditionsFiltering(a.Id, @conditionsJson, @matchConditionsOnly) cm
	where	a.AssetTypeID = @assetTypeId 
			and cm.[Include] = 1;

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

		static async Task handleError(Exception ex, ScoreQueueInfo scoreInfo, ILogger log)
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
				log.LogWarning(ex, ex.Message);

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
				log.LogError(ex, ex.Message);
			}


			var execUpdater = new ExecutionUpdater { Info = scoreInfo };
			var closedExecution = await execUpdater.UpdateAsync(ex);

			if (!closedExecution && shouldRequeue)
			{
				var queue = new AzureQueueSource();
				await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, minuteDelay, 0));
			}
		}

		static async Task handleScoreProcessingError(ScoreQueueInfo scoreInfo)
		{
			var queue = new AzureQueueSource();
			await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, 0, 30));
		}
	}
}
