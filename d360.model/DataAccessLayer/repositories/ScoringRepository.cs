using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers.filters;

using Dapper;
using LaunchDarkly.Sdk.Server;

namespace d360.model.DataAccessLayer
{
	public class ScoringRepository : BaseRepository, IScoringRepository
	{
		internal LdClient Ld;
		internal IQueueSource QueueSource;

		public ScoringRepository(ICompanyContext companyContext, LdClient ld, IQueueSource queueSource)
			: base(companyContext)
		{
			Ld = ld;
			QueueSource = queueSource;
		}

		public List<AssetTypeClass> AllowedClassesForScoreType()
		{
			return new List<AssetTypeClass>
				{
					AssetTypeClass.BusinessAsset,
					AssetTypeClass.TechnicalAsset,
					AssetTypeClass.Model,
					AssetTypeClass.Policy,
					AssetTypeClass.Rule
				};
		}

		public List<AllocationApiGetModel> GetAllocations(IEnumerable<KeyValuePair<string, string>> queryParams, out string error, AssetTypeClass? Class = null)
		{
			error = string.Empty;
			List<string> whereStatements = new List<string>();
			string orderBy = "P.[Path]";
			string orderDirection = "asc";

			var dbArgs = new DynamicParameters();

			Dictionary<string, string> fieldMapping = new Dictionary<string, string>
			{
				{ "assetclassname","AT.class" },
				{ "assettypepath","P.[Path]"},
				{ "scoretype", "AL.scoreType" },
				{ "state", "AL.[state]"},
				{ "isexternallycalculated", "AL.isExternallyCalculated"},
				{ "lowerthreshold","AL.lowerThreshold" },
				{ "upperthreshold","AL.upperThreshold"}
			};

			if (Class.HasValue)
			{
				whereStatements.Add("AT.class = @Class");
				dbArgs.Add("@Class", Class);
			}

			foreach (var kp in queryParams)
			{
				switch (kp.Key.ToLower())
				{
					case "allocationuid":
						Guid allocationUid = Guid.Empty;
						Guid.TryParse(kp.Value, out allocationUid);

						if (allocationUid == Guid.Empty)
						{
							error = "Invalid Allocation UID specified.";
							return null;
						}

						whereStatements.Add("AL.Uid = @allocationUid");
						dbArgs.Add("@allocationUid", allocationUid);
						break;
					case "assetuid":
						Guid assetUid = Guid.Empty;
						Guid.TryParse(kp.Value, out assetUid);

						if (assetUid == Guid.Empty)
						{
							error = "Invalid Asset UID specified.";
							return null;
						}

						whereStatements.Add("AL.Uid in (select distinct AllocationUid from metrics.score where AssetUid = @assetUid)");
						dbArgs.Add("@assetUid", assetUid);
						break;

					case "assettypeuid":
						Guid assetTypeUid = Guid.Empty;
						Guid.TryParse(kp.Value, out assetTypeUid);

						if (assetTypeUid == Guid.Empty)
						{
							error = "Invalid Asset Type UID specified.";
							return null;
						}

						whereStatements.Add("AL.assettypeuid = @assettypeuid");
						dbArgs.Add("@assettypeuid", assetTypeUid);
						break;

					case "_state":
						State stateValue;
						Enum.TryParse(kp.Value, true, out stateValue);

						if ((stateValue != State.Active && stateValue != State.Deleted) || string.IsNullOrEmpty(kp.Value))
						{
							error = "Invalid state value specified.";
							return null;
						}

						whereStatements.Add("AL.[state] = @state");
						dbArgs.Add("@state", stateValue);
						break;

					case "assetclassname":
						var classList = AssetTypeClass.Generic.GetAsList();
						var filteredClasses = classList.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
						whereStatements.Add("AT.class in @filteredClasses");
						dbArgs.Add("@filteredClasses", filteredClasses);
						break;

					case "assettypepath":
						whereStatements.Add("P.[Path] like @pathname");
						dbArgs.Add("@pathname", "%" + kp.Value.Trim() + "%");
						break;

					case "scoretype":
						var sc = kp.Value.Trim();
						var scoretypeInfos = ScoreType.DataQuality.GetAsList();
						var filteredScoreTypes = scoretypeInfos.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
						whereStatements.Add("AL.scoreType in @filteredScoreTypesGlobal");
						dbArgs.Add("@filteredScoreTypesGlobal", filteredScoreTypes);
						break;

					case "isexternallycalculated":
						bool? isExtern = null;
						
						if ("external".Contains(kp.Value.ToLower()))
						{
							isExtern = true;
						}
						
						if ("internal".Contains(kp.Value.ToLower()))
						{
							isExtern = false;
						}
						
						if ("ternal".Contains(kp.Value.ToLower()))
						{
							isExtern = null;
						}

						if (isExtern.HasValue)
						{
							whereStatements.Add("AL.IsExternallyCalculated = @isExt");
							dbArgs.Add("@isExt", isExtern);
						}
						break;

					case "global":
						List<string> globalFilters = new List<string>();
						var classListGlobal = AssetTypeClass.Generic.GetAsList();
						var filteredClassesGlobal = classListGlobal.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
						globalFilters.Add("AT.class in @filteredClassesGlobal");
						dbArgs.Add("@filteredClassesGlobal", filteredClassesGlobal);

						globalFilters.Add("P.[Path] like @pathnameGlobal");
						dbArgs.Add("@pathnameGlobal", "%" + kp.Value.Trim() + "%");

						var scGlobal = kp.Value.Trim();
						var scoretypeInfosGlobal = ScoreType.DataQuality.GetAsList();
						var filteredScoreTypesGlobal = scoretypeInfosGlobal.Where(x => x.Name.ToLower().Contains(kp.Value.Trim().ToLower())).Select(x => x.ID);
						globalFilters.Add("AL.scoreType in @filteredScoreTypesGlobal");
						dbArgs.Add("@filteredScoreTypesGlobal", filteredScoreTypesGlobal);

						bool? isExt = null;

						if ("external".Contains(kp.Value.ToLower()))
						{
							isExt = true;
						}

						if ("internal".Contains(kp.Value.ToLower()))
						{
							isExt = false;
						}

						if ("ternal".Contains(kp.Value.ToLower()))
						{
							isExt = null;
						}

						if (isExt.HasValue)
						{
							globalFilters.Add("AL.IsExternallyCalculated = @isExt");
							dbArgs.Add("@isExt", isExt);
						}
						else
						{
							globalFilters.Add("AL.IsExternallyCalculated IS NOT NULL");
						}

						whereStatements.Add($"({string.Join(" or ", globalFilters)})");
						break;
					case "_direction":
						string val = kp.Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
						
						if (!(new[] { "asc", "desc" }.Contains(val)))
						{
							error = "Invalid _direction specified. Allowed values are 'asc' and 'desc'.";
							return null;
						}
						
						orderDirection = val;
						break;
					case "_order":
						string order = kp.Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
						if (!fieldMapping.ContainsKey(order))
						{
							error = "Invalid _order specified.";
							return null;
						}
						orderBy = fieldMapping[order];
						break;
					default: break;
				}
			}

			//Defaults
			if (dbArgs.ParameterNames.Contains("@state"))
			{
				whereStatements.Add("AL.[state] = @state");
				dbArgs.Add("@state", State.Active);
			}

			string sqlWhere = whereStatements.Count > 0 ? " where " + string.Join(" and ", whereStatements) : "";
			string sqlOrderClause = $"order by {orderBy} {orderDirection}";

			var sql = $@"select 
							AL.uid,
							AT.class as assetClassName,
							AL.assettypeuid,
							P.[Path] as assetTypePath,
							AL.scoreType,
							AL.[state],
							AL.isExternallyCalculated,
							AL.lowerThreshold,
							AL.upperThreshold,
							case 
								when Measures.F > 0 then 1
								else 0
							end as hasMeasure,
							case 
								when DisabledMeasures.F > 0 then 1
								else 0
							end as hasDisabledMeasure,
							case 
								when Fields.F > 0 then 1
								else 0
							end as hasField
						from metrics.Allocation AL
							inner join AssetType AT on AT.uid = AL.assettypeuid                                    
							cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P
							cross apply (select count(*) from metrics.Asset where State = 1 and AllocationUid = AL.Uid) Measures(F)
							cross apply (select count(*) from metrics.Asset where State <> 1 and AllocationUid = AL.Uid) DisabledMeasures(F)
							cross apply (select count(*) from FieldType where AssetTypeID = AT.ID and [Type] = 'Score' and ScoreType = AL.ScoreType) Fields(F)
						{sqlWhere}
						{sqlOrderClause}
						";

			List<AllocationApiGetModel> allocations = CompanyContext.Query<AllocationApiGetModel>(sql, dbArgs, ApiTimeout).ToList();
			return allocations;
		}

		public AllocationApiGetModel PostAllocation(AllocationApiUpsertModel model, ref MetricAllocation alloc)
		{
			if (alloc != null)
			{
				alloc.State = State.Active;
				alloc.UpdatedBy = CompanyContext.CurrentResourceID;
				alloc.IsExternallyCalculated = model.isExternallyCalculated;
				alloc.UpdatedOn = DateTime.UtcNow;
				alloc.LowerThreshold = model.lowerThreshold.Value;
				alloc.UpperThreshold = model.upperThreshold.Value;
				CompanyContext.SaveChanges();
			}
			else
			{
				alloc = new MetricAllocation
				{
					AssetTypeUid = model.assetTypeUid,
					ScoreType = model.scoreType
				};
				alloc.CreatedBy = alloc.UpdatedBy = CompanyContext.CurrentResourceID;
				alloc.CreatedOn = alloc.UpdatedOn = DateTime.UtcNow;
				alloc.IsExternallyCalculated = model.isExternallyCalculated;
				alloc.LowerThreshold = model.lowerThreshold.Value;
				alloc.UpperThreshold = model.upperThreshold.Value;
				CompanyContext.MetricAllocations.Add(alloc);
				CompanyContext.SaveChanges();
			}

			#region Execution Log

			string logSql = $@"
declare @executionUid uniqueidentifier = newid(),
		@id int,
		@d datetime = getutcdate();
insert into api.Execution (ExecutionID, ResourceID, Total, Processed, [Error], StartedOn, ProcessingStartedOn, CompletedOn, [State], [Action])
values (@executionUid, @CurrentResourceID, 1, 1, 0, @d, @d, @d, 4, @action)

select @id = Id from api.Execution where ExecutionID = @executionUid;

insert into api.ExecutionLog (ExecutionId, [Payload])
	select	@id,
			(select Id,
					@CalculationMethod as CalculationMethod,
					@ScoreType as ScoreType,
					iif(IsExternallyCalculated = 1, 'true', 'false') as IsExternallyCalculated,
					LowerThreshold,
					UpperThreshold,
					cast(1 as bit) as IsNew
			for json path
			) as Payload
	from	metrics.Allocation
	where	Uid = @Uid;

select @id";
			
			var executionId = CompanyContext.Query<int>(logSql, 
				new { 
					alloc.Uid, 
					CompanyContext.CurrentResourceID, 
					action = (int)ApiExecutionAction.PostScoreAllocation, 
					CalculationMethod = alloc.CalculationMethod.ToString(),
					ScoreType = alloc.ScoreType.ToString()
				}).Single();

			QueueSource.CreateMessage(Config.GetValue<string>("AssetGraphQueue"), new PostExecutionQueueMessage { 
				Action = PostExecutionQueueMessageAction.History, 
				CompanyID = CompanyContext.CurrentCompanyID, 
				ExecutionId = executionId
			});

			#endregion


			var dbArgs = new DynamicParameters();
			dbArgs.Add("@uid", alloc.Uid);

			var sql = $@"select 
							AL.uid,
							AT.class as assetClassName,
							AL.assettypeuid,
							P.[Path] as assetTypePath,
							AL.scoreType,
							AL.[state],
							AL.isExternallyCalculated,
							AL.lowerThreshold,
							AL.upperThreshold
						from metrics.Allocation AL
							inner join AssetType AT on AT.uid = AL.assettypeuid                                    
							cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P
						where AL.uid = @uid";

			AllocationApiGetModel allocation = CompanyContext.Query<AllocationApiGetModel>(sql, dbArgs).FirstOrDefault();
			
			return allocation;
		}

		public AllocationApiGetModel UpdateAllocation(AllocationApiUpsertModel model, MetricAllocation alloc)
		{
			alloc.AssetTypeUid = model.assetTypeUid;
			alloc.ScoreType = model.scoreType;
			alloc.UpdatedBy = CompanyContext.CurrentResourceID;
			alloc.IsExternallyCalculated = model.isExternallyCalculated;
			alloc.LowerThreshold = model.lowerThreshold.Value;
			alloc.UpperThreshold = model.upperThreshold.Value;
			alloc.UpdatedOn = DateTime.UtcNow;
			CompanyContext.SaveChanges();

			#region Execution Log

			string logSql = $@"
declare @executionUid uniqueidentifier = newid(),
		@id int,
		@d datetime = getutcdate();
insert into api.Execution (ExecutionID, ResourceID, Total, Processed, [Error], StartedOn, ProcessingStartedOn, CompletedOn, [State], [Action])
values (@executionUid, @CurrentResourceID, 1, 1, 0, @d, @d, @d, 4, @action)

select @id = Id from api.Execution where ExecutionID = @executionUid;

insert into api.ExecutionLog (ExecutionId, [Payload])
	select	@id,
			(select Id,
					@CalculationMethod as CalculationMethod,
					@ScoreType as ScoreType,
					iif(IsExternallyCalculated = 1, 'true', 'false') as IsExternallyCalculated,
					LowerThreshold,
					UpperThreshold,
					cast(0 as bit) as IsNew
			for json path
			) as Payload
	from	metrics.Allocation
	where	Uid = @Uid;

select @id";

			var executionId = CompanyContext.Query<int>(logSql,
				new
				{
					alloc.Uid,
					CompanyContext.CurrentResourceID,
					action = (int)ApiExecutionAction.PutScoreAllocation,
					CalculationMethod = alloc.CalculationMethod.ToString(),
					ScoreType = alloc.ScoreType.ToString()
				}).Single();

			QueueSource.CreateMessage(Config.GetValue<string>("AssetGraphQueue"), new PostExecutionQueueMessage
			{
				Action = PostExecutionQueueMessageAction.History,
				CompanyID = CompanyContext.CurrentCompanyID,
				ExecutionId = executionId
			});

			#endregion

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@uid", alloc.Uid);

			var sql = $@"select 
							AL.uid,
							AT.class as assetClassName,
							AL.assettypeuid,
							P.[Path] as assetTypePath,
							AL.scoreType,
							AL.[state],
							AL.isExternallyCalculated,
							AL.lowerThreshold,
							AL.upperThreshold
						from metrics.Allocation AL
							inner join AssetType AT on AT.uid = AL.assettypeuid                                    
							cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P
						where AL.uid = @uid";

			AllocationApiGetModel allocation = CompanyContext.Query<AllocationApiGetModel>(sql, dbArgs).FirstOrDefault();

			return allocation;
		}

		public void DeleteAllocation(MetricAllocation alloc)
		{
			alloc.UpdatedBy = CompanyContext.CurrentResourceID;
			alloc.UpdatedOn = DateTime.UtcNow;
			alloc.State = State.Deleted;
			CompanyContext.SaveChanges();

			#region Execution Log

			string logSql = $@"
declare @executionUid uniqueidentifier = newid(),
		@id int,
		@d datetime = getutcdate();
insert into api.Execution (ExecutionID, ResourceID, Total, Processed, [Error], StartedOn, ProcessingStartedOn, CompletedOn, [State], [Action])
values (@executionUid, @CurrentResourceID, 1, 1, 0, @d, @d, @d, 4, @action)

select @id = Id from api.Execution where ExecutionID = @executionUid;

insert into api.ExecutionLog (ExecutionId, [Payload])
	select	@id,
			(select Id
			for json path
			) as Payload
	from	metrics.Allocation
	where	Uid = @Uid;

select @id";

			var executionId = CompanyContext.Query<int>(logSql,
				new
				{
					alloc.Uid,
					CompanyContext.CurrentResourceID,
					action = (int)ApiExecutionAction.DeleteScoreAllocation
				}).Single();

			QueueSource.CreateMessage(Config.GetValue<string>("AssetGraphQueue"), new PostExecutionQueueMessage
			{
				Action = PostExecutionQueueMessageAction.History,
				CompanyID = CompanyContext.CurrentCompanyID,
				ExecutionId = executionId
			});

			#endregion
		}

		public bool HasActiveMeasures(MetricAllocation alloc)
		{
			return CompanyContext.MetricAssets.Any(x => x.State == State.Active && x.AllocationUid == alloc.Uid);
		}

		public bool DoesAllocationExist(Guid allocationUid, AllocationApiUpsertModel model)
		{
			return CompanyContext.MetricAllocations.Any(x => x.Uid != allocationUid && x.AssetTypeUid == model.assetTypeUid && x.ScoreType == model.scoreType);
		}

		public MetricAllocation GetAllocationByUid(Guid allocationUid)
		{
			return CompanyContext.GetByUid<MetricAllocation>(allocationUid);
		}

		public MetricAllocation GetAllocationByModel(AllocationApiUpsertModel model)
		{
			return CompanyContext.MetricAllocations.FirstOrDefault(x => x.AssetTypeUid == model.assetTypeUid && x.ScoreType == model.scoreType);
		}

		public async Task<List<AllocationApiGetUnallocatedAssetTypeModel>> GetUnallocatedAssetTypes(ScoreType scoreType)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@scoreType", (int)scoreType);

			dbArgs.Add("@supportedAssetClasses", AllowedClassesForScoreType().Select(x => (int)x));

			var sql = $@"select 
							att.[uid] as assetTypeUid,
							atp.Path as assetTypePath,
							att.Class as assetTypeClass
						from
							[dbo].[assettype] att
							cross apply [dbo].[GetAssetTypeTextPathById](att.id,'/') atp
						where 
							att.class in @supportedAssetClasses
								and
							not exists (select 1 from [metrics].Allocation a where a.[state] = 1 and a.assettypeuid = att.[uid] and a.scoretype = @scoreType)";

			return (await CompanyContext.QueryAsync<AllocationApiGetUnallocatedAssetTypeModel>(sql, dbArgs, ApiTimeout)).ToList();

		}

		public List<ExternalScoreResultApiResponseModel> PostExternalResults(MetricAllocation allocation, List<ExternalScoreResultApiRequestModel> model, ApiExecution execution)
		{
			return CompanyContext.BulkExternalResultsImport(model, execution, allocation);
		}

		public List<ExternalScoreResultApiResponseModel> PostExternalResults(ScoreType scoreType, List<ExternalScoreResultApiRequestModel> model, ApiExecution execution)
		{
			return CompanyContext.BulkExternalResultsImport(model, execution, scoreType);
		}

		private List<InternalScoreResultApiResponseModel> postScoreResults(List<InternalScoreResultApiRequestModel> results)
		{
			List<InternalScoreResultApiResponseModel> list = new();

			var now = DateTime.UtcNow.Date;
			var models = results.Select(o => new ExternalMeasureResult { AssetUid = o.assetUid, AssetVersionUid = o.metricAssetUid, EffectiveDate = o.effectiveDate ?? now, Value = o.result }).ToList();
			results = null;

			CompanyContext.ExternalMeasureResults.AddRange(models);
			CompanyContext.SaveChanges();

			var minId = models.Select(o => o.Id).Min();
			var maxId = models.Select(o => o.Id).Max();

			models = CompanyContext.Query<ExternalMeasureResult>(@"
update	t
set		t.AssetUid = s.Uid,
		t.AssetVersionUid = m.Uid 
from	metrics.ExternalMeasureResult t
		outer apply (
			select	a.Uid,
					aa.Uid as AssetTypeUid
			from	Asset a
					inner join AssetType aa on aa.Id = a.AssetTypeId and a.Uid = t.AssetUid
		) s
		outer apply (
			select	v.Uid
			from	metrics.AssetVersion v
					cross apply openjson(v.Definition) with (
						[Check] varchar(25) '$.Governance.Check'
					) vd
					inner join metrics.Asset a on a.Uid = v.AssetUid and a.Uid = t.AssetVersionUid and v.EffectiveDate <= t.EffectiveDate and (v.EffectiveEndDate is null or v.EffectiveEndDate >= t.EffectiveDate)
					inner join metrics.Allocation al on al.Uid = a.AllocationUid and al.ScoreType = 1 and al.AssetTypeUid = s.AssetTypeUid
			where	vd.[Check] = 'External'
		) m
where	t.Id between @minId and @maxId;

delete	metrics.ExternalMeasureResult
where	Id between @minId and @maxId
		and (AssetUid is null or AssetVersionUid is null);

select	*
from	metrics.ExternalMeasureResult
where	Id between @minId and @maxId;
", new { minId, maxId }).ToList();

			var uniques = models.Select(o => new { o.AssetUid, o.EffectiveDate }).Distinct();
			foreach (var unique in uniques)
			{
				var info = new ScoreQueueInfo
				{
					CompanyID = CompanyContext.CurrentCompanyID,
					ResourceID = CompanyContext.CurrentResourceID,
					ChangeType = ScoreQueueChangeType.RescoreRequest,
					UseUpdatedScoringEngine = true,
					Payload = new AssetRescoreRequestModel { AssetUid = unique.AssetUid, EffectiveDate = unique.EffectiveDate, ScoreType = ScoreType.Governance },
					StartedOn = DateTime.UtcNow
				};
				QueueSource.CreateMessage(Config.GetValue<string>("ScoringQueue"), info);
			}

			list = models.Select(o => new InternalScoreResultApiResponseModel { AssetUid = o.AssetUid, EffectiveDate = o.EffectiveDate, IsSuccess = true, Result = o.Value }).ToList();
			models = null;

			return list;
		}

		public List<InternalScoreResultApiResponseModel> PostScoreResults(MetricAllocation allocation, ApiExecution execution, List<InternalScoreResultApiRequestModel> results)
		{
			var isUpdatedScoring = Ld.BoolVariation(FeatureFlags.TEMP_SCORE_ENGINE_UPDATE, CompanyContext.GetSdkFeatureFlagUser(), false);
			if (isUpdatedScoring)
			{
				return postScoreResults(results);
			}
			else
			{
				return CompanyContext.BulkMetricsImport(results, execution, allocation);
			}
		}

		public List<InternalScoreResultApiResponseModel> PostScoreResults(ScoreType scoreType, ApiExecution execution, List<InternalScoreResultApiRequestModel> results)
		{
			var isUpdatedScoring = Ld.BoolVariation(FeatureFlags.TEMP_SCORE_ENGINE_UPDATE, CompanyContext.GetSdkFeatureFlagUser(), false);
			if (isUpdatedScoring)
			{
				return postScoreResults(results);
			}
			else 
			{
				return CompanyContext.BulkMetricsImport(results, execution, scoreType);
			}
		}

		public async Task<DataQualityScoreItemEvidenceViewModel> GetEvidenceForDataQualityScoreItem(Guid scoreItemUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var evidenceModel = new DataQualityScoreItemEvidenceViewModel { };

			var dbArgs = new DynamicParameters();
			List<string> whereStatements = new List<string>();
			var queryFieldOptions = new List<DefaultFilter>
			{
				new DefaultFilter("ResultUid", "AR.Uid", SqlFieldType.Guid),
				new DefaultFilter("OwningAssetUid", "OA.Uid", SqlFieldType.Guid),
				new DefaultFilter("OwningAssetPath", "OAN.KeyPath", SqlFieldType.Text),
				new DefaultFilter("OwningAssetTypePath", "OANTP.Path", SqlFieldType.Text),
				new DefaultFilter("OwningAssetDisplayPath", "OAN.DisplayPath", SqlFieldType.Text),

				new DefaultFilter("EvaluatedAssetUid", "EA.Uid", SqlFieldType.Guid),
				new DefaultFilter("EvaluatedAssetPath", "EAN.KeyPath", SqlFieldType.Text),
				new DefaultFilter("EvaluatedAssetTypePath", "EANTP.Path", SqlFieldType.Text),
				new DefaultFilter("EvaluatedAssetDisplayPath", "EAN.DisplayPath", SqlFieldType.Text),

				new DefaultFilter("EffectiveDate", "AR.EffectiveDate", SqlFieldType.DateTime),
				new DefaultFilter("RunDate", "AR.RunDate", SqlFieldType.DateTime),
				new DefaultFilter("TotalCount", "AR.TotalCount", SqlFieldType.Number),
				new DefaultFilter("PassFraction", "AR.PassFraction", SqlFieldType.Number),
				new DefaultFilter("PassCount", "AR.PassCount", SqlFieldType.Number),
				new DefaultFilter("FailCount", "AR.FailCount", SqlFieldType.Number),
			};

			CompanyContext.ParseAdvancedFilterQueryParameter(queryParams, queryFieldOptions, out DynamicParameters advFilterArgs, out List<string> advFilterStatements);
			if (advFilterArgs != null && advFilterStatements != null)
			{
				dbArgs.AddDynamicParams(advFilterArgs);
				whereStatements.AddRange(advFilterStatements);
			}

			var simpleWhere = "";

			CompanyContext.ParseSimpleFilterQueryParameter(queryParams, queryFieldOptions, out DynamicParameters simpleFilterArgs, out List<string> simpleFilterStatements);
			if (simpleFilterArgs.ParameterNames.Count() != 0 && simpleFilterStatements.Count != 0)
			{
				dbArgs.AddDynamicParams(simpleFilterArgs);

				simpleWhere = " and ( " + string.Join(" or ", simpleFilterStatements) + ") ";
			}

			//Add the default query items
			dbArgs.Add("@scoreItemUid", scoreItemUid);
			dbArgs.Add("@userId", CompanyContext.CurrentResourceID);
			whereStatements.Insert(0, "I.Uid = @scoreItemUid");

			var orderColumn = CompanyContext.ParseOrderColumn(queryParams, queryFieldOptions, "OAN.DisplayPath");
			var orderDirection = CompanyContext.ParseOrderDirection(queryParams, "desc");
			var orderBySql = $" order by {orderColumn} {orderDirection} ";

			int pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
			int pageSize = CompanyContext.ParsePageSize(queryParams);
			string offset = CompanyContext.ParsePageOffsetSql(pageNum, pageSize);

			evidenceModel.pageNum = pageNum;
			evidenceModel.pageSize = pageSize;

			var tables = $@"
							from	metrics.ScoreItem I
									cross apply openjson(I.Evidence) E
									cross apply (
										select	count(1) as PathItemCount
										from	openjson(E.value, N'$.RollupPath')
									) Pc
									inner join Asset OA on OA.Uid = cast(JSON_VALUE(E.value, N'$.RollupPath['+cast(Pc.PathItemCount-1 as varchar)+'].Uid') as uniqueidentifier)
									inner join AssetPath OAN on OAN.ID = OA.ID
									cross apply GetAssetTypeTextPathById(OA.AssetTypeID, ' > ') OANTP

									inner join Asset EA on EA.Uid = cast(JSON_VALUE(E.value, N'$.RollupPath['+cast(Pc.PathItemCount-2 as varchar)+'].Uid') as uniqueidentifier)
									inner join AssetPath EAN on EAN.ID = EA.ID 
									cross apply GetAssetTypeTextPathById(EA.AssetTypeID, ' > ') EANTP 

									outer apply openjson(E.value, '$.ResultResultUids') R
									left join AssetResult AR on AR.Uid = R.value
							where   {string.Join(" and ", whereStatements)} {simpleWhere} and JSON_VALUE(I.Evidence, N'$.IsError') is null ";

			var sql = $@"
						declare @exists bit = 0,
								@visible bit = 0,
								@isDq bit = 0,
								@total int = 0

						select	@exists = cast(iif(count(1) > 0, 1, 0) as bit)
						from	metrics.ScoreItem I
						where	I.Uid = @scoreItemUid

						select	@visible = cast(iif(count(1) > 0, 1, 0) as bit)
						from	metrics.ScoreItem I
								inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid
								inner join metrics.Score S on S.Uid = L.ScoreUid
								inner join Asset A on A.Uid = S.AssetUid
						where	I.Uid = @scoreItemUid

						select	@isDq = cast(iif(count(1) > 0, 1, 0) as bit)
						from	metrics.ScoreItem I
								inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid
								inner join metrics.Score S on S.Uid = L.ScoreUid
								inner join metrics.Allocation A on A.Uid = S.AllocationUid and A.ScoreType = 2
						where	I.Uid = @scoreItemUid;

						select	@total = count(1) {tables};

						select @exists;
						select @visible;
						select @isDq;
						select @total;

						if @exists = 1 and @visible = 1
						begin
							select	(
										select 	ERP.PathAssetUid as [Uid],
												P.DisplayPath as AssetPath,
												TP.Path as AssetTypePath,
												case ERP.PathPosition
													when 1 then null 
													when MP.Position then PR.Inverse
													else PR.Name 
												end as [Predicate],
												ERP.PathPosition as [Position]
										from	openjson(E.value, '$.RollupPath') with (
													[PathAssetUid] uniqueidentifier '$.Uid',
													[PathPosition] int '$.Position'
												) ERP
												inner join Asset A on A.Uid = ERP.PathAssetUid 
												inner join AssetPath P on P.ID = A.ID 
												cross apply GetAssetTypeTextPathById(A.AssetTypeID, ' > ') TP
												inner join metrics.AssetVersion V on V.Uid = I.AssetVersionUid
												inner join metrics.AssetVersionRollupPath VR on VR.AssetVersionUid = V.Uid
												inner join metrics.RollupPathLink PL on PL.RollupPathUid = VR.RollupPathUid and ( (ERP.PathPosition = 1 and ERP.PathPosition = PL.StartPosition) or (ERP.PathPosition > 1 and ERP.PathPosition = PL.EndPosition) )
												inner join IntersectType IT on IT.ID = PL.IntersectTypeID
												inner join [Predicate] as PR on PR.ID = IT.PredicateID
												cross apply (
													select	max(P) as [Position]
													from	openjson(E.value, '$.RollupPath') with ([P] int '$.Position')
												) MP
										order by ERP.PathPosition
										for json path
									) as RollupPathJson,
									AR.Uid as ResultUid,
									OA.Uid as OwningAssetUid, 
									OAN.KeyPath as OwningAssetPath,
									OANTP.Path as OwningAssetTypePath,
									OAN.DisplayPath as OwningAssetDisplayPath,
									EA.Uid as EvaluatedAssetUid, 
									EAN.KeyPath as EvaluatedAssetPath,
									EANTP.Path as EvaluatedAssetTypePath,
									EAN.DisplayPath as EvaluatedAssetDisplayPath,
									AR.EffectiveDate,
									AR.RunDate,
									AR.TotalCount,
									coalesce(AR.PassFraction, 0) as PassFraction,
									AR.PassCount,
									AR.FailCount
							 {tables} {orderBySql} {offset} 
						end";

			// I've setup 120 seconds timeout as a hot fix for GOV-18554. 
			// It will be revisited in GOV-18444
			var evidenceModelRequest = await CompanyContext.QueryMultipleAsync(sql, dbArgs, 120);

			var scoreItemExists = evidenceModelRequest.Read<bool>().Single();
			var canReadAsset = evidenceModelRequest.Read<bool>().Single();
			var isDq = evidenceModelRequest.Read<bool>().Single();
			evidenceModel.total = evidenceModelRequest.Read<int>().Single();
			evidenceModel.items = (scoreItemExists && canReadAsset && isDq) ?
				evidenceModelRequest.Read<DataQualityScoreItemEvidenceItemViewModel>().ToList() :
				null;

			if (!scoreItemExists)
			{
				throw new StatusCodeException(System.Net.HttpStatusCode.NotFound);
			}

			if (!isDq)
			{
				throw new StatusCodeException(System.Net.HttpStatusCode.Conflict);
			}
			
			if (!canReadAsset)
			{
				throw new StatusCodeException(System.Net.HttpStatusCode.Forbidden);
			}

			return evidenceModel;
		}

		public ScoreExecution GetExecutionById(Guid uid)
		{
			return CompanyContext.Filter<ScoreExecution>(i => i.Uid == uid).SingleOrDefault();
		}

		public IQueryable<ScoreExecution> GetExecutions(int pageSize, int pageNumber)
		{
			if (pageNumber > 0)
			{
				pageNumber -= 1;
			}
			else
			{
				pageNumber = 0;
			}

			if (pageSize > 200 || pageSize < 0)
			{
				pageSize = 200;
			}

			return CompanyContext.ScoreExecutions.OrderByDescending(i => i.StartedOn).Skip(pageSize * pageNumber).Take(pageSize);
		}

		public List<ScoreExecutionItemViewModel> GetExecutionItems(
			long executionId,
			int pageSize,
			int pageNumber,
			ScoreQueueChangeType? changeType = null)
		{
			if (pageNumber > 0)
			{
				pageNumber -= 1;
			}
			else
			{
				pageNumber = 0;
			}

			if (pageSize > 200 || pageSize < 0)
			{
				pageSize = 200;
			}

			var items = CompanyContext.Table<ScoreExecutionItem>();

			if (changeType.HasValue)
			{
				items = items.Where(i => i.ExecutionID == executionId && i.ChangeType == changeType.Value);
			}
			else
			{
				items = items.Where(i => i.ExecutionID == executionId);
			}

			items = items.OrderByDescending(i => i.ChangeType).ThenBy(i => i.RowNumber);
			items.Skip(pageSize * pageNumber).Take(pageSize);

			List<ScoreExecutionItemViewModel> models = new List<ScoreExecutionItemViewModel>();

			foreach (var item in items)
			{
				var model = new ScoreExecutionItemViewModel
				{
					ChangeType = item.ChangeType,
					Message = item.Message,
					RowNumber = item.RowNumber,
					State = item.State
				};
				switch (item.ChangeType)
				{
					case ScoreQueueChangeType.AssetMeasures:
						model.Payload = item.GetPayload<AssetMeasureModel>();
						break;
					case ScoreQueueChangeType.CheckTypeDependencyRemoved:
						model.Payload = item.GetPayload<CheckTypeDependencyRemovedModel>();
						break;
					case ScoreQueueChangeType.MeasureChanged:
						model.Payload = item.GetPayload<MeasureChangedModel>();
						break;
					case ScoreQueueChangeType.MeasureRemoved:
						model.Payload = item.GetPayload<MeasureRemovedModel>();
						break;
					case ScoreQueueChangeType.RollupPathChanged:
						model.Payload = item.GetPayload<RollupPathChangedModel>();
						break;
					case ScoreQueueChangeType.RuleAssetRemoved:
						model.Payload = item.GetPayload<RuleAssetRemovedModel>();
						break;
					case ScoreQueueChangeType.WorkflowCheck:
						model.Payload = item.GetPayload<ScoreCreatedModel>();
						break;
					default:
						model.Payload = "{}";
						break;
				}
				models.Add(model);
			}

			return models;
		}
	}
}
