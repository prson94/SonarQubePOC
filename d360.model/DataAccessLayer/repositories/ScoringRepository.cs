using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers.filters;

using Dapper;

namespace d360.model.DataAccessLayer
{
	public class ScoringRepository : BaseRepository, IScoringRepository
	{
		private readonly ICompanyContext companyContext;

		public ScoringRepository(ICompanyContext companyContext)
			: base(companyContext)
		{
			this.companyContext = companyContext;
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

			List<AllocationApiGetModel> allocations = companyContext.Query<AllocationApiGetModel>(sql, dbArgs, ApiTimeout).ToList();
			return allocations;
		}

		public AllocationApiGetModel PostAllocation(AllocationApiUpsertModel model, ref MetricAllocation alloc)
		{
			if (alloc != null)
			{
				alloc.State = State.Active;
				alloc.UpdatedBy = companyContext.CurrentResourceID;
				alloc.IsExternallyCalculated = model.isExternallyCalculated;
				alloc.UpdatedOn = DateTime.UtcNow;
				alloc.LowerThreshold = model.lowerThreshold.Value;
				alloc.UpperThreshold = model.upperThreshold.Value;
				companyContext.SaveChanges();
			}
			else
			{
				alloc = new MetricAllocation
				{
					AssetTypeUid = model.assetTypeUid,
					ScoreType = model.scoreType
				};
				alloc.CreatedBy = alloc.UpdatedBy = companyContext.CurrentResourceID;
				alloc.CreatedOn = alloc.UpdatedOn = DateTime.UtcNow;
				alloc.IsExternallyCalculated = model.isExternallyCalculated;
				alloc.LowerThreshold = model.lowerThreshold.Value;
				alloc.UpperThreshold = model.upperThreshold.Value;
				companyContext.MetricAllocations.Add(alloc);
				companyContext.SaveChanges();
			}

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

			AllocationApiGetModel allocation = companyContext.Query<AllocationApiGetModel>(sql, dbArgs).FirstOrDefault();
			
			return allocation;
		}

		public AllocationApiGetModel UpdateAllocation(AllocationApiUpsertModel model, MetricAllocation alloc)
		{
			alloc.AssetTypeUid = model.assetTypeUid;
			alloc.ScoreType = model.scoreType;
			alloc.UpdatedBy = companyContext.CurrentResourceID;
			alloc.IsExternallyCalculated = model.isExternallyCalculated;
			alloc.LowerThreshold = model.lowerThreshold.Value;
			alloc.UpperThreshold = model.upperThreshold.Value;
			alloc.UpdatedOn = DateTime.UtcNow;
			companyContext.SaveChanges();

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

			AllocationApiGetModel allocation = companyContext.Query<AllocationApiGetModel>(sql, dbArgs).FirstOrDefault();

			return allocation;
		}

		public void DeleteAllocation(MetricAllocation alloc)
		{
			alloc.UpdatedBy = companyContext.CurrentResourceID;
			alloc.UpdatedOn = DateTime.UtcNow;
			alloc.State = State.Deleted;
			companyContext.SaveChanges();
		}

		public bool HasActiveMeasures(MetricAllocation alloc)
		{
			return companyContext.MetricAssets.Any(x => x.State == State.Active && x.AllocationUid == alloc.Uid);
		}

		public bool DoesAllocationExist(Guid allocationUid, AllocationApiUpsertModel model)
		{
			return companyContext.MetricAllocations.Any(x => x.Uid != allocationUid && x.AssetTypeUid == model.assetTypeUid && x.ScoreType == model.scoreType);
		}

		public MetricAllocation GetAllocationByUid(Guid allocationUid)
		{
			return companyContext.GetByUid<MetricAllocation>(allocationUid);
		}

		public MetricAllocation GetAllocationByModel(AllocationApiUpsertModel model)
		{
			return companyContext.MetricAllocations.FirstOrDefault(x => x.AssetTypeUid == model.assetTypeUid && x.ScoreType == model.scoreType);
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

			return (await companyContext.QueryAsync<AllocationApiGetUnallocatedAssetTypeModel>(sql, dbArgs, ApiTimeout)).ToList();

		}

		public List<ExternalScoreResultApiResponseModel> PostExternalResults(MetricAllocation allocation, List<ExternalScoreResultApiRequestModel> model, ApiExecution execution)
		{
			return companyContext.BulkExternalResultsImport(model, execution, allocation);
		}

		public List<ExternalScoreResultApiResponseModel> PostExternalResults(ScoreType scoreType, List<ExternalScoreResultApiRequestModel> model, ApiExecution execution)
		{
			return companyContext.BulkExternalResultsImport(model, execution, scoreType);
		}

		public List<InternalScoreResultApiResponseModel> PostScoreResults(MetricAllocation allocation, ApiExecution execution, List<InternalScoreResultApiRequestModel> results)
		{
			return companyContext.BulkMetricsImport(results, execution, allocation);
		}

		public List<InternalScoreResultApiResponseModel> PostScoreResults(ScoreType scoreType, ApiExecution execution, List<InternalScoreResultApiRequestModel> results)
		{
			return companyContext.BulkMetricsImport(results, execution, scoreType);
		}

		public async Task<DataQualityScoreItemEvidenceViewModel> GetEvidenceForDataQualityScoreItem(Guid scoreItemUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var evidenceModel = new DataQualityScoreItemEvidenceViewModel { };

			var dbArgs = new DynamicParameters();
			List<string> whereStatements = new List<string>();
			var queryFieldOptions = new List<DefaultFilter>
			{
				new DefaultFilter("ResultUid", "AR.Uid", SqlFieldType.Guid),
				new DefaultFilter("OwningAssetUid", "OAN.Uid", SqlFieldType.Guid),
				new DefaultFilter("OwningAssetPath", "OANKP.KeyPath", SqlFieldType.Text),
				new DefaultFilter("OwningAssetTypePath", "OANTP.Path", SqlFieldType.Text),
				new DefaultFilter("OwningAssetDisplayPath", "OANDP.DisplayPath", SqlFieldType.Text),

				new DefaultFilter("EvaluatedAssetUid", "EAN.Uid", SqlFieldType.Guid),
				new DefaultFilter("EvaluatedAssetPath", "EANKP.KeyPath", SqlFieldType.Text),
				new DefaultFilter("EvaluatedAssetTypePath", "EANTP.Path", SqlFieldType.Text),
				new DefaultFilter("EvaluatedAssetDisplayPath", "EANDP.DisplayPath", SqlFieldType.Text),

				new DefaultFilter("EffectiveDate", "AR.EffectiveDate", SqlFieldType.DateTime),
				new DefaultFilter("RunDate", "AR.RunDate", SqlFieldType.DateTime),
				new DefaultFilter("TotalCount", "AR.TotalCount", SqlFieldType.Number),
				new DefaultFilter("PassFraction", "AR.PassFraction", SqlFieldType.Number),
				new DefaultFilter("PassCount", "AR.PassCount", SqlFieldType.Number),
				new DefaultFilter("FailCount", "AR.FailCount", SqlFieldType.Number),
			};

			companyContext.ParseAdvancedFilterQueryParameter(queryParams, queryFieldOptions, out DynamicParameters advFilterArgs, out List<string> advFilterStatements);
			if (advFilterArgs != null && advFilterStatements != null)
			{
				dbArgs.AddDynamicParams(advFilterArgs);
				whereStatements.AddRange(advFilterStatements);
			}

			var simpleWhere = "";

			companyContext.ParseSimpleFilterQueryParameter(queryParams, queryFieldOptions, out DynamicParameters simpleFilterArgs, out List<string> simpleFilterStatements);
			if (simpleFilterArgs.ParameterNames.Count() != 0 && simpleFilterStatements.Count != 0)
			{
				dbArgs.AddDynamicParams(simpleFilterArgs);

				simpleWhere = " and ( " + string.Join(" or ", simpleFilterStatements) + ") ";
			}

			//Add the default query items
			dbArgs.Add("@scoreItemUid", scoreItemUid);
			dbArgs.Add("@userId", companyContext.CurrentResourceID);
			whereStatements.Insert(0, "I.Uid = @scoreItemUid");

			var orderColumn = companyContext.ParseOrderColumn(queryParams, queryFieldOptions, "OANDP.DisplayPath");
			var orderDirection = companyContext.ParseOrderDirection(queryParams, "desc");
			var orderBySql = $" order by {orderColumn} {orderDirection} ";

			int pageNum = companyContext.ParsePageNumber(queryParams, 1);
			int pageSize = companyContext.ParsePageSize(queryParams);
			string offset = companyContext.ParsePageOffsetSql(pageNum, pageSize);

			evidenceModel.pageNum = pageNum;
			evidenceModel.pageSize = pageSize;

			var tables = $@"
							from	metrics.ScoreItem I
									cross apply openjson(I.Evidence) E
									cross apply (
										select	count(1) as PathItemCount
										from	openjson(E.value, N'$.RollupPath')
									) Pc

									inner join graph.AssetNode OAN on OAN.Uid = cast(JSON_VALUE(E.value, N'$.RollupPath['+cast(Pc.PathItemCount-1 as varchar)+'].Uid') as uniqueidentifier)
									inner join graph.AssetNodeKeyPath OANKP on OANKP.Uid = OAN.Uid
									inner join graph.AssetNodeDisplayPath OANDP on OANDP.Uid = OAN.Uid
									cross apply GetAssetTypeTextPathById(OAN.AssetTypeID, ' > ') OANTP

									inner join graph.AssetNode EAN on EAN.Uid = cast(JSON_VALUE(E.value, N'$.RollupPath['+cast(Pc.PathItemCount-2 as varchar)+'].Uid') as uniqueidentifier)
									inner join graph.AssetNodeKeyPath EANKP on EANKP.Uid = EAN.Uid
									inner join graph.AssetNodeDisplayPath EANDP on EANDP.Uid = EAN.Uid
									cross apply GetAssetTypeTextPathById(EAN.AssetTypeID, ' > ') EANTP 

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
												inner join graph.AssetNodeDisplayPath P on P.Uid = ERP.PathAssetUid
												cross apply GetAssetTypeTextPathById(P.AssetTypeID, ' > ') TP
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
									OAN.Uid as OwningAssetUid, 
									OANKP.KeyPath as OwningAssetPath,
									OANTP.Path as OwningAssetTypePath,
									OANDP.DisplayPath as OwningAssetDisplayPath,
									EAN.Uid as EvaluatedAssetUid, 
									EANKP.KeyPath as EvaluatedAssetPath,
									EANTP.Path as EvaluatedAssetTypePath,
									EANDP.DisplayPath as EvaluatedAssetDisplayPath,
									AR.EffectiveDate,
									AR.RunDate,
									AR.TotalCount,
									coalesce(AR.PassFraction, 0) as PassFraction,
									AR.PassCount,
									AR.FailCount
							 {tables} {orderBySql} {offset} 
						end";

			var evidenceModelRequest = await companyContext.QueryMultipleAsync(sql, dbArgs);
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
			return companyContext.Filter<ScoreExecution>(i => i.Uid == uid).SingleOrDefault();
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

			return companyContext.ScoreExecutions.OrderByDescending(i => i.StartedOn).Skip(pageSize * pageNumber).Take(pageSize);
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

			var items = companyContext.Table<ScoreExecutionItem>();

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
