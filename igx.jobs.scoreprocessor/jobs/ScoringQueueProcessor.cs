using AngleSharp.Common;
using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
	public class ScoringQueueProcessor : BaseWebJob
	{
        const string FUNCTION_NAME = "Scoring_QueueProcessor";

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;

		public ScoringQueueProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		public async Task Run([QueueTrigger("%ScoringQueue%", Connection = "QueuesConnectionString")] string myQueueItem, ILogger log)
		{
			var info = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);
			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", info.CompanyID },
				{ "ChangeType", info.ChangeType.ToString() }
			};

			using (log.BeginScope(logProperties))
			{
				try
				{
					string sql = "";
					string companyConnectionString = "";
					List<WorkflowScoredAsset> updatedAssets;

					switch (info.ChangeType)
					{
						case ScoreQueueChangeType.RescoreRequest:
							var rescorePayload = JsonConvert.DeserializeObject<AssetRescoreRequestModel>(info.Payload.ToString());
							sql = getAssetRescoreSql();
							companyConnectionString = GetCompanyConnectionString(info.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								var response = await companyConnection.QueryAsync<WorkflowScoredAsset>(sql, new { 
									rescorePayload.AssetUid, 
									effectiveDate = rescorePayload.EffectiveDate.Date, 
									scoreType = (int)rescorePayload.ScoreType 
								}, commandTimeout: 600);
								updatedAssets = response.ToList();
								
								processWorkflowCalls(info.CompanyID, info.ResourceID ?? 0, "", rescorePayload.ScoreType, updatedAssets, log);
							}
							break;
						case ScoreQueueChangeType.PatchCatalogExecution:
							sql = getPatchExecutionSql();
							companyConnectionString = GetCompanyConnectionString(info.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								var response = await companyConnection.QueryAsync<WorkflowScoredAsset>(sql, new { 
									info.ExecutionId
								}, commandTimeout: 18000);
								updatedAssets = response.ToList();
								
								processWorkflowCalls(info.CompanyID, info.ResourceID ?? 0, "", ScoreType.Governance, updatedAssets, log);
							}
							break;
						case ScoreQueueChangeType.MeasureChanged:
							if (info.Payload != null)
							{ 
								var measureChangedPayload = JsonConvert.DeserializeObject<MeasureChangedModel>(info.Payload.ToString());
								sql = getMeasureChangedSql();
								companyConnectionString = GetCompanyConnectionString(info.CompanyID);
								using (var companyConnection = new SqlConnection(companyConnectionString))
								{
									await companyConnection.OpenIfClosed();
									var response = await companyConnection.QueryAsync<WorkflowScoredAsset>(sql, new { 
										versionUid = measureChangedPayload.MetricAssetVersionUid 
									}, commandTimeout: 18000);
									updatedAssets = response.ToList();
								
									//Get the score type for this deleted measure, which will be sent to workflow.
									var scoreType = await companyConnection.QuerySingleAsync<ScoreType>(SCORE_TYPE_SQL, new { measureChangedPayload.MetricAssetVersionUid });
									processWorkflowCalls(info.CompanyID, info.ResourceID ?? 0, "", scoreType, updatedAssets, log);
								}							
							}
							break;
						case ScoreQueueChangeType.MeasureRemoved:
							if (info.Payload != null)
							{
								var measureRemovedPayload = JsonConvert.DeserializeObject<MeasureRemovedModel>(info.Payload.ToString());
								sql = getMeasureChangedSql();
								companyConnectionString = GetCompanyConnectionString(info.CompanyID);
								using (var companyConnection = new SqlConnection(companyConnectionString))
								{
									await companyConnection.OpenIfClosed();
									var response = await companyConnection.QueryAsync<WorkflowScoredAsset>(sql, new
									{
										versionUid = measureRemovedPayload.MetricAssetVersionUid
									}, commandTimeout: 18000);
									updatedAssets = response.ToList();

									//Get the score type for this deleted measure, which will be sent to workflow.
									var scoreType = await companyConnection.QuerySingleAsync<ScoreType>(SCORE_TYPE_SQL, new { measureRemovedPayload.MetricAssetVersionUid });
									processWorkflowCalls(info.CompanyID, info.ResourceID ?? 0, "", scoreType, updatedAssets, log);
								}
							}
							break;
						case ScoreQueueChangeType.RollupPathChanged:
							sql = "exec metrics.CalculateRollups";
							companyConnectionString = GetCompanyConnectionString(info.CompanyID);
							using (var companyConnection = new SqlConnection(companyConnectionString))
							{
								await companyConnection.OpenIfClosed();
								await companyConnection.ExecuteAsync(sql, commandTimeout: 600);
							}
							break;
						case ScoreQueueChangeType.RuleAssetRemoved:
							if (info.Payload != null)
							{
								var ruleRemovedPayload = JsonConvert.DeserializeObject<RuleAssetRemovedModel>(info.Payload.ToString());
								sql = getRuleRemovedSql();
								companyConnectionString = GetCompanyConnectionString(info.CompanyID);
								using (var companyConnection = new SqlConnection(companyConnectionString))
								{
									await companyConnection.OpenIfClosed();
									var response = await companyConnection.QueryAsync<WorkflowScoredAsset>(sql, new
									{
										assetUid = ruleRemovedPayload.AssetUid
									}, commandTimeout: 18000);
									updatedAssets = response.ToList();
									processWorkflowCalls(info.CompanyID, info.ResourceID ?? 0, "", ScoreType.DataQuality, updatedAssets, log);
								}
							}
							break;
						default:
							// no action found.
							break;
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
		}

		const string SCORE_TYPE_SQL = "select al.ScoreType " +
									  "from metrics.Asset a " +
									  "inner join metrics.AssetVersion v on v.AssetUid = a.Uid and v.Uid = @MetricAssetVersionUid " +
									  "inner join metrics.Allocation al on al.Uid = a.AllocationUid ";
		string COMMON_TEMP_TABLE_SQL = $@"create table #ids (RowId int identity, AssetUid uniqueidentifier, Score decimal(8,6));
										create clustered index cdx_ids on #ids (RowId);";
		const string COMMON_LOOP_SQL = @"
declare @current int = 1,
		@max int,
		@currentAssetUid uniqueidentifier,
		@responseScore decimal(8,6);

select @max = max(RowId) from #ids
while @current <= @max
begin
	select @currentAssetUid = AssetUid from #ids where RowId = @current

	exec metrics.GenerateScore @currentAssetUid, @effectiveDate, @scoreType, @responseScore
	update #ids set Score = @responseScore where RowId = @current
	set @responseScore = null

	set @current = @current + 1
end";
		const string COMMON_WORKFLOW_ASSET_SQL = "select t.Object as ObjectType, t.ObjectID as ObjectTypeID, a.Object, a.ObjectID " +
												 "from #ids i " +
												 "inner join Asset a on a.Uid = i.AssetUid and i.Score is not null " +
												 "inner join AssetType t on t.ID = a.AssetTypeID " +
												 "inner join workflow.EventRegistration W on W.Object = t.Object and W.ObjectID = t.ObjectID and W.ChangeType = 5;";

		string getAssetRescoreSql()
		{
			return $@"
{COMMON_TEMP_TABLE_SQL}
insert into #ids (AssetUid) values (@AssetUid);
{COMMON_LOOP_SQL}
{COMMON_WORKFLOW_ASSET_SQL}";
		}

		string getMeasureChangedSql()
		{
			return $@"
declare @effectiveDate date = getutcdate(),
		@scoreType int,
		@conditionsJson nvarchar(max),
		@matchConditionsOnly bit,
		@assetTypeId int;

{COMMON_TEMP_TABLE_SQL}

select	@scoreType = al.ScoreType,
		@matchConditionsOnly = v.MatchConditionsOnly,
		@conditionsJson = metrics.BuildConditionJson(v.Uid),
		@assetTypeId = t.ID
from	metrics.Asset a
		inner join metrics.AssetVersion v on v.AssetUid = a.Uid and v.Uid = @versionUid
		inner join metrics.Allocation al on al.Uid = a.AllocationUid
		inner join AssetType t on t.Uid = al.AssetTypeUid;

insert into #ids (AssetUid) 
	select	a.Uid
	from	Asset a
			cross apply metrics.ConditionsFiltering(a.Id, @conditionsJson, @matchConditionsOnly) cm
	where	a.AssetTypeID = @assetTypeId 
			and cm.[Include] = 1;

{COMMON_LOOP_SQL}
{COMMON_WORKFLOW_ASSET_SQL}";
		}

		string getPatchExecutionSql()
		{
			return $@"
declare @effectiveDate date = getutcdate(),
		@scoreType int = 1; 

{COMMON_TEMP_TABLE_SQL}

insert into #ids (AssetUid)
	select	i.[Uid]
	from	api.ExecutionCatalogItem i
			inner join AssetType t on t.ID = i.TypeId and i.ExecutionId = @ExecutionId and i.[Type] = 'A' and i.Success = 1
			inner join metrics.Allocation al on al.AssetTypeUid = t.Uid and al.ScoreType = @scoreType; 

{COMMON_LOOP_SQL}
{COMMON_WORKFLOW_ASSET_SQL}";
		}

		string getRuleRemovedSql()
		{
			return $@"
declare @effectiveDate date = getutcdate(),
		@scoreType int = 2;

{COMMON_TEMP_TABLE_SQL}

insert into #ids (AssetUid) 
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

{COMMON_LOOP_SQL}
{COMMON_WORKFLOW_ASSET_SQL}";
		}

		void processWorkflowCalls(int companyId, int resourceId, string companyDomainPrefix, ScoreType scoreType, List<WorkflowScoredAsset> updatedAssets, ILogger log)
		{
			var context = new UriSecurityContextProvider
			{
				CompanyID = companyId,
				ResourceID = resourceId,
				CompanyPrefix = companyDomainPrefix,
				IsAdministrator = false
			};
			var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
			var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true)
			{
				ApiExecutionQueue = Configuration["ApiExecutionQueue"],
				AssetGraphQueue = Configuration["AssetGraphQueue"],
				BulkLoadQueue = Configuration["BulkLoadQueue"],
				DisplayValueQueue = Configuration["DisplayValueQueue"],
				EventBusTopicName = Configuration["EventBusTopicName"],
				ScoringQueue = Configuration["ScoringQueue"],
				SearchIndexQueue = Configuration["SearchIndexQueue"]
			};

			var assetGroups = updatedAssets.GroupBy(a => new { a.ObjectType, a.ObjectTypeID }).ToList();

			assetGroups.ForEach(ag =>
			{
				company.SendWorkflowEvents(ag.Key.ObjectType, ag.Key.ObjectTypeID, ag.ToList(), scoreType: scoreType);
			});
		}

		async Task handleScoreProcessingError(ScoreQueueInfo scoreInfo)
		{
			await Queue.CreateMessageAsync(Configuration["ScoringQueue"], scoreInfo, new TimeSpan(0, 0, 30));
		}
	}
}
