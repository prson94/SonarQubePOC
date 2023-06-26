using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.helpers;
using d360.core.queue;
using d360.core.resources;

using Dapper;

using Newtonsoft.Json;

namespace d360.model
{
	public partial interface ICompanyContext : IBaseContext
	{
		#region DbSets

		DbSet<MetricAllocation> MetricAllocations { get; set; }

		DbSet<MetricAsset> MetricAssets { get; set; }

		DbSet<MetricAssetVersion> MetricAssetVersions { get; set; }

		DbSet<MetricAssetVersionCondition> MetricAssetVersionConditions { get; set; }

		DbSet<MetricAssetVersionConditionItem> MetricAssetVersionConditionItems { get; set; }

		DbSet<MetricAssetVersionConditionItemValue> MetricAssetVersionConditionItemValues { get; set; }

		DbSet<MetricAssetVersionRollupPath> MetricAssetVersionRollupPaths { get; set; }

		DbSet<MetricAssetVersionRollupPathFilter> MetricAssetVersionRollupPathFilters { get; set; }

		DbSet<MetricAssetVersionRollupPathFilterValue> MetricAssetVersionRollupPathFilterValues { get; set; }

		DbSet<MetricRollupPath> MetricRollupPaths { get; set; }

		DbSet<MetricRollupPathLink> MetricRollupPathLinks { get; set; }

		DbSet<MetricRollupPathSegment> MetricRollupPathSegments { get; set; }

		DbSet<Score> Scores { get; set; }

		DbSet<ScoreExecution> ScoreExecutions { get; set; }

		DbSet<ScoreExecutionItem> ScoreExecutionItems { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the SQL statement to execute for data quality measures, depending on the type of query needed.
		/// </summary>
		/// <param name="queryType">
		/// 1 => Impacted Assets/Effective Dates by ResultUids.
		/// 2 => Impacted Asset/Effective Dates By Provided Uid.
		/// 3 => Get Measure Results For Calculation.
		/// </param>
		DataQualityMeasureQueryModel BuildDataQualityMeasureQueryModel(MetricDataQualityQueryType queryType, Guid assetVersionRollupPathUid);

		List<ExternalScoreResultApiResponseModel> BulkExternalResultsImport(List<ExternalScoreResultApiRequestModel> model, ApiExecution execution, MetricAllocation allocation);
		
		List<ExternalScoreResultApiResponseModel> BulkExternalResultsImport(List<ExternalScoreResultApiRequestModel> model, ApiExecution execution, ScoreType scoreType);
		
		List<InternalScoreResultApiResponseModel> BulkMetricsImport(List<InternalScoreResultApiRequestModel> model, ApiExecution execution, MetricAllocation allocation);
		
		List<InternalScoreResultApiResponseModel> BulkMetricsImport(List<InternalScoreResultApiRequestModel> model, ApiExecution execution, ScoreType scoreType = ScoreType.Governance);

		/// <summary>
		/// A Score Engine method that is called when a dependent object such as an Intersect Type, Responsibility Type or Field Type is removed from Govern, and a notification is sent to the Score Engine to determine what needs to be recalculated.
		/// </summary>
		void CreateCheckDependencyRemovedNotificationExecution(List<Guid> versionUids);

		/// <summary>
		/// A Score Engine method that is called when a dependent object such as an Intersect Type, Responsibility Type or Field Type is removed from Govern, and score recalculations will take place.
		/// </summary>
		void CreateCheckDependencyRemovedResultExecution(List<Guid> versionUids);

		/// <summary>
		/// A Score Engine method that is called when a workflow check should occur when externally calculated scores are added to Govern.
		/// </summary>
		void CreateExternalScoreWorkflowCheckExecution(Guid apiExecutionUid);

		/// <summary>
		/// A Score Engine method that is called when assets are added to Govern.
		/// </summary>
		void CreateImportAssetsExecution(Guid apiExecutionUid, Guid assetTypeUid);

		/// <summary>
		/// A Score Engine method that is called when relationships are removed from Govern.
		/// </summary>
		void CreateDeleteRelationshipsExecution(Guid apiExecutionUid, int intersectTypeId);

		/// <summary>
		/// A Score Engine method that is called when relationships are added to Govern.
		/// </summary>
		void CreateImportRelationshipsExecution(Guid apiExecutionUid, int intersectTypeId, int timeout);

		/// <summary>
		/// A Score Engine method that is called when a measure is updated in Govern, and a notification is sent to the Score Engine to determine what needs to be recalculated.
		/// </summary>
		Guid CreateMeasureChangedNotificationExecution(MetricAssetVersion version, DateTime effectiveDate, Guid? triggeredByMeasureUid = null);

		/// <summary>
		/// A Score Engine method that is called when a measure is updated in Govern, resulting in a new version.
		/// </summary>
		void CreateMeasureChangedResultExecution(List<AssetMeasureModel> list, Guid? apiExecutionUid = null);

		/// <summary>
		/// A Score Engine method that is called when a measure is removed from Govern, and a notification is sent to the Score Engine to determine what needs to be recalculated.
		/// </summary>
		void CreateMeasureRemovedNotificationExecution(MetricAssetVersion version);

		/// <summary>
		/// A Score Engine method that is called when a measure is removed from Govern.
		/// </summary>
		void CreateMeasureRemovedResultExecution(Guid metricAssetVersionUid);

		/// <summary>
		/// A Score Engine method that is called when a parent asset is (un)assigned a child.
		/// </summary>
		void CreateParentAssetGovernanceRescoreExecution(Guid apiExecutionUid);

		/// <summary>
		/// A Score Engine method that is called when an asset type or intersect type are added or removed from Govern.
		/// </summary>
		void CreateRollupPathChangedExecution(int? intersectTypeId = null, int? assetTypeId = null, Guid? triggeredByApiExecutionUid = null);

		/// <summary>
		/// A Score Engine method that is called when one or more rule results are removed from Govern, and result in a score recalculation.
		/// </summary>
		void CreateRuleResultsRemovedExecution(Guid assetUid);

		/// <summary>
		/// A Score Engine method that is called when one or more rules are removed from Govern, and result in a score recalculation.
		/// </summary>
		void CreateRulesRemovedExecution(Guid apiExecutionUid, List<Guid> assetUids);

		/// <summary>
		/// A Score Engine method that is called when one or more rules are removed from Govern, and result in a score recalculation.
		/// </summary>
		void CreateRulesRemovedExecution(Guid apiExecutionUid, int assetTypeId);

		/// <summary>
		/// A Score Engine method that is called when a workflow check occurs after scores are processed.
		/// </summary>
		void CreateWorkflowCheckExecution(ScoreExecution execution, ScoreQueueChangeType previousChangeType);

		/// <summary>
		/// A Score Engine method that is called from the Workflow system when an asset field is updated.
		/// </summary>
		void CreateWorkflowItemFieldUpdateExecution(AssetType assetType, Asset asset);

		List<DataQualityDeleteResponseModel> DeleteAssetResults(List<DataQualityDeleteModel> request, ApiExecution execution, int timeout = 3600);
		
		List<AssetMeasureModel> GetAssetMeasuresFromRuleResults(List<Guid> ruleResultUids);
		
		decimal? GetAssetScore(long assetId, ScoreType type);

		/// <summary>
		/// Used where BuildDataQualityMeasureQueryModel uses QueryType = 2
		/// </summary>
		List<AssetMeasureModel> GetDataQualityAssetEffectiveDateResultModels(DataQualityMeasureQueryModel query, Guid allocationUid, Guid metricAssetUid, Guid metricAssetVersionUid, DateTime measureEffectiveDate);

		/// <summary>
		/// Used where BuildDataQualityMeasureQueryModel uses QueryType = 3
		/// </summary>
		List<DataQualityMeasureQueryResultModel> GetDataQualityMeasureQueryResultModels(DataQualityMeasureQueryModel query, Guid assetUid, DateTime? maxDate);

		/// <summary>
		/// Gets impacted asset/measures that require rescoring based on this responsibility type allocation.
		/// </summary>
		/// <param name="assetType">The asset type.</param>
		/// <param name="responsibility">The responsibility type.</param>
		/// <returns>A list of AssetMeasureModel items to send to the scoring engine.</returns>
		List<AssetMeasureModel> GetMeasureModelsBasedOnResponsibilityAllocation(AssetType assetType, ResponsibilityType responsibility);

		ObjectStatisticTileModel GetObjectStatistics(string type, int id);
		
		decimal? GetPreviousAssetScore(long assetId, ScoreType type);
		
		List<DataQualityResponseModel> UpsertAssetResults(List<IDataQualityUpsert> request, ApiExecution execution, int timeout = 3600, bool sendWorkflowEvents = true);

		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		#region DbSets

		public DbSet<MetricAllocation> MetricAllocations { get; set; }

		public DbSet<MetricAsset> MetricAssets { get; set; }

		public DbSet<MetricAssetVersion> MetricAssetVersions { get; set; }

		public DbSet<MetricAssetVersionCondition> MetricAssetVersionConditions { get; set; }

		public DbSet<MetricAssetVersionConditionItem> MetricAssetVersionConditionItems { get; set; }

		public DbSet<MetricAssetVersionConditionItemValue> MetricAssetVersionConditionItemValues { get; set; }

		public DbSet<MetricAssetVersionRollupPath> MetricAssetVersionRollupPaths { get; set; }

		public DbSet<MetricAssetVersionRollupPathFilter> MetricAssetVersionRollupPathFilters { get; set; }

		public DbSet<MetricAssetVersionRollupPathFilterValue> MetricAssetVersionRollupPathFilterValues { get; set; }

		public DbSet<MetricRollupPath> MetricRollupPaths { get; set; }

		public DbSet<MetricRollupPathLink> MetricRollupPathLinks { get; set; }

		public DbSet<MetricRollupPathSegment> MetricRollupPathSegments { get; set; }

		public DbSet<Score> Scores { get; set; }

		public DbSet<ScoreExecution> ScoreExecutions { get; set; }

		public DbSet<ScoreExecutionItem> ScoreExecutionItems { get; set; }

		#endregion

		#region Utility
		
		private ScoreExecution createScoreExecution(Guid? triggeredByApiExecutionUid = null, Guid? triggeredMeasureUid = null)
		{
			ScoreExecution execution = new ScoreExecution
			{
				Uid = Guid.NewGuid(),
				StartedOn = DateTime.UtcNow,
				PercentComplete = 0,
				TriggeredByExecutionUid = triggeredByApiExecutionUid,
				TriggeredByMeasureUid = triggeredMeasureUid
			};

			Add(execution);

			return execution;
		}	
		
		private List<ExternalScoreResultApiResponseModel> BulkExternalResultsImport(List<ExternalScoreResultApiRequestModel> model, ApiExecution execution, bool isSpecificAllocation)
		{
			//Set effective date for any results that do not have a date set.
			model.ForEach(m =>
			{
				if (!m.effectiveDate.HasValue)
				{
					m.effectiveDate = DateTime.UtcNow.Date;
				}
			});

			Add(execution);

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			#region Generate Data Sets

			DataTable scoreTable = new DataTable();
			DataTable measureTable = new DataTable();

			scoreTable.Columns.Add("ExecutionID", typeof(Guid));
			scoreTable.Columns.Add("ItemNumber", typeof(int));
			scoreTable.Columns.Add("AssetUid", typeof(Guid));
			scoreTable.Columns.Add("EffectiveDate", typeof(DateTime));
			scoreTable.Columns.Add("AllocationUid", typeof(Guid));
			scoreTable.Columns["AllocationUid"].AllowDBNull = true;
			scoreTable.Columns.Add("ScoreType", typeof(int));
			scoreTable.Columns["ScoreType"].AllowDBNull = true;
			scoreTable.Columns.Add("Score", typeof(decimal));
			scoreTable.Columns.Add("RunDate", typeof(DateTime));
			scoreTable.Columns["RunDate"].AllowDBNull = true;

			measureTable.Columns.Add("ExecutionID", typeof(Guid));
			measureTable.Columns.Add("ItemNumber", typeof(int));
			measureTable.Columns.Add("MetricAssetUid", typeof(Guid));
			measureTable.Columns.Add("Passed", typeof(bool));

			int itemNumber = 1;
			foreach (ExternalScoreResultApiRequestModel item in model)
			{
				DataRow row = scoreTable.NewRow();

				row["ExecutionID"] = execution.ExecutionID;
				row["ItemNumber"] = itemNumber;
				row["AssetUid"] = item.assetUid;
				row["EffectiveDate"] = item.effectiveDate;

				if (item.scoreType.HasValue)
				{
					row["ScoreType"] = (int)item.scoreType.Value;
				}
				else
				{
					row["ScoreType"] = DBNull.Value;
				}

				if (item.allocationUid.HasValue)
				{
					row["AllocationUid"] = item.allocationUid.Value;
				}
				else
				{
					row["AllocationUid"] = DBNull.Value;
				}

				row["Score"] = item.score;

				if (item.runDate.HasValue)
				{
					row["RunDate"] = item.runDate;
				}
				else
				{
					row["RunDate"] = DBNull.Value;
				}

				scoreTable.Rows.Add(row);

				if (item.measures != null && item.measures.Any())
				{
					foreach (ExternalScoreResultMeasureModel measure in item.measures)
					{
						DataRow measureRow = measureTable.NewRow();

						measureRow["ExecutionID"] = execution.ExecutionID;
						measureRow["ItemNumber"] = itemNumber;
						measureRow["MetricAssetUid"] = measure.measureUid;
						measureRow["Passed"] = measure.passed;

						measureTable.Rows.Add(measureRow);
					}
				}

				itemNumber++;
			}

			#endregion

			if (Connection.State != ConnectionState.Open)
			{
				Connection.Open();
			}

			#region Bulk Copy

			using (SqlBulkCopy bulk = Connection.CreateBulkCopy("api.ExecutionScore"))
			{
				bulk.ColumnMappings.Add("ExecutionID", "ExecutionID");
				bulk.ColumnMappings.Add("ItemNumber", "ItemNumber");
				bulk.ColumnMappings.Add("AssetUid", "AssetUid");
				bulk.ColumnMappings.Add("AllocationUid", "AllocationUid");
				bulk.ColumnMappings.Add("ScoreType", "ScoreType");
				bulk.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
				bulk.ColumnMappings.Add("RunDate", "RunDate");
				bulk.ColumnMappings.Add("Score", "Score");

				bulk.WriteToServer(scoreTable);
			}

			using (SqlBulkCopy bulk = Connection.CreateBulkCopy("api.ExecutionMeasure"))
			{
				bulk.ColumnMappings.Add("ExecutionID", "ExecutionID");
				bulk.ColumnMappings.Add("ItemNumber", "ItemNumber");
				bulk.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
				bulk.ColumnMappings.Add("Passed", "Passed");

				bulk.WriteToServer(measureTable);
			}

			#endregion

			#region Validation

			// Resolve Uids and key objects.
			if (isSpecificAllocation)
			{
				Connection.Execute(@"
									update  T 
									set     T.IsValidAllocation = iif(Al.Uid is null, 0, 1),
											T.IsValidAsset = iif(A.Uid is null, 0, 1),
											T.AllocationUid = Al.Uid,
											T.ScoreUid = iif(S.Uid is null, newid(), S.Uid)
									from    api.ExecutionScore T 
											left join dbo.Asset A on A.Uid = T.AssetUid
											left join dbo.AssetType Ast on Ast.ID = A.AssetTypeID
											left join metrics.Allocation Al on Al.AssetTypeUid = Ast.Uid and Al.Uid = T.AllocationUid and Al.IsExternallyCalculated = 1 and Al.OverrideName is null 
											left join metrics.Score S on S.AllocationUid = Al.Uid and S.AssetUid = T.AssetUid and S.EffectiveDate = T.EffectiveDate
									where   T.ExecutionID = @ExecutionID
											and T.AllocationUid is not null", new { execution.ExecutionID }, commandTimeout: timeout);
			}
			else
			{
				Connection.Execute(@"
									update  T 
									set     T.IsValidAllocation = iif(Al.Uid is null, 0, 1),
											T.IsValidAsset = iif(A.Uid is null, 0, 1),
											T.AllocationUid = Al.Uid,
											T.ScoreUid = iif(S.Uid is null, newid(), S.Uid)
									from    api.ExecutionScore T 
											left join dbo.Asset A on A.Uid = T.AssetUid
											left join dbo.AssetType Ast on Ast.ID = A.AssetTypeID
											left join metrics.Allocation Al on Al.AssetTypeUid = Ast.Uid and Al.ScoreType = T.ScoreType and (Al.OverrideName is null or Al.OverrideName = '') and Al.IsExternallyCalculated = 1
											left join metrics.Score S on S.AllocationUid = Al.Uid and S.AssetUid = T.AssetUid and S.EffectiveDate = T.EffectiveDate
									where   T.ExecutionID = @ExecutionID
											and T.AllocationUid is null", new { execution.ExecutionID }, commandTimeout: timeout);
			}

			Connection.Execute(@"
								update  T 
								set     T.IsValidMetric = iif(A.Uid is null, 0, 1), 
										T.IsValidVersion = iif(VUid.Uid is null, 0, 1), 
										T.MetricAssetVersionUid = VUid.Uid,
										T.ScoreUid = S.ScoreUid,
										T.ScoreItemUid = iif(Si.Uid is null, newid(), Si.Uid)
								from    api.ExecutionMeasure T 
										inner join api.ExecutionScore S on S.ExecutionID = @executionID and S.ItemNumber = T.ItemNumber
										left join metrics.[Asset] A on A.[Uid] = T.MetricAssetUid and A.[State] = 1 and S.AllocationUid = A.AllocationUid
										outer apply (
													select max(EffectiveDate) as EffectiveDate from metrics.AssetVersion where AssetUid = A.[Uid] and EffectiveDate <= S.[EffectiveDate] and [State] = 1
													) VEff
										outer apply (
													select  Uid 
													from    metrics.AssetVersion 
													where   AssetUid = A.[Uid] 
															and EffectiveDate = VEff.EffectiveDate
													) VUid
										left join metrics.ScoreItemLink Sil on Sil.ScoreUid = S.ScoreUid
										left join metrics.ScoreItem Si on Si.Uid = Sil.ScoreItemUid and Si.AssetVersionUid = VUid.Uid
								where   T.ExecutionID = @ExecutionID", new { execution.ExecutionID }, commandTimeout: timeout);

			// Validate date ranges
			Connection.Execute(@"
								update  T 
								set     T.Success = 0, 
										T.Message = coalesce(T.Message, '') + 'Effective date cannot be in the future; '
								from    api.ExecutionScore T 
								where   T.ExecutionID = @ExecutionID and T.EffectiveDate > getutcdate()", new { execution.ExecutionID }, commandTimeout: timeout);

											Connection.Execute(@"
								update  T 
								set     T.Success = 0, 
										T.Message = coalesce(T.Message, '') + 'Run date cannot be in the future; '
								from    api.ExecutionScore T 
								where   T.ExecutionID = @ExecutionID and T.RunDate > getutcdate()", new { execution.ExecutionID }, commandTimeout: timeout);

											Connection.Execute(@"
								update  T 
								set     T.Success = 0, 
										T.Message = coalesce(T.Message, '') + 'Run date must be provided; '
								from    api.ExecutionScore T 
								where   T.ExecutionID = @ExecutionID and T.RunDate is null", new { execution.ExecutionID }, commandTimeout: timeout);

			// Resolve measures
			Connection.Execute(@"
								update  T 
								set     T.Success = 0, 
										T.Message = coalesce(T.Message, '') + 'All measures must be provided for this metric; '
								from    api.ExecutionScore T
										inner join metrics.Asset Ma on Ma.AllocationUid = T.AllocationUid and Ma.State = 1 and Ma.IsGroup = 0
										left join api.ExecutionMeasure Em on Em.ExecutionID = T.ExecutionID and Em.ItemNumber = T.ItemNumber and Em.MetricAssetUid = Ma.Uid
								where   T.ExecutionID = @ExecutionID and Em.ItemNumber is null", new { execution.ExecutionID }, commandTimeout: timeout);

			// Validate score value
			Connection.Execute(@"
								update  api.ExecutionScore 
								set     Success = 0, 
										Message = coalesce(Message, '') + 'Score must be between 0 and 1; '
								where   ExecutionID = @ExecutionID and ( [Score] is null or [Score] < 0 or [Score] > 1 )", new { execution.ExecutionID }, commandTimeout: timeout);

			// Update success status
			Connection.Execute(@"
								update  api.ExecutionScore
								set     Success = 0,
										Message = coalesce(Message, '') + 'Invalid asset specified; '
								where   ExecutionID = @ExecutionID 
										and IsValidAsset = 0;

								update  api.ExecutionScore
								set     Success = 0,
										Message = coalesce(Message, '') + 'This asset does not have this score type allocated for external scores; '
								where   ExecutionID = @ExecutionID 
										and IsValidAllocation = 0;

								update  T
								set     T.Success = 0,
										T.Message = coalesce(Message, '') + 'Invalid metric specified; '
								from    api.ExecutionScore T
										inner join api.ExecutionMeasure S on S.ExecutionID = T.ExecutionID and T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber and S.IsValidMetric = 0;

								update  T
								set     T.Success = 0,
										T.Message = coalesce(Message, '') + 'Invalid effective date specified; '
								from    api.ExecutionScore T
										inner join api.ExecutionMeasure S on S.ExecutionID = T.ExecutionID and T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber and S.IsValidVersion = 0;

								update  api.ExecutionScore
								set     Success = 1
								where   ExecutionID = @ExecutionID 
										and success is null;", new { execution.ExecutionID }, commandTimeout: timeout);

			#endregion

			#region Load Data

			int loopSize = 100;
			int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total) / loopSize);
			int beginItemNumber = 1;
			int endItemNumber = loopSize;
			List<ExternalScoreResultApiResponseModel> results = new List<ExternalScoreResultApiResponseModel>();

			for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
			{
				bool runCompleted = false;
				int retryCount = 0;

				while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
				{
					using (SqlTransaction trans = Connection.BeginTransaction())
					{
						try
						{
							#region Load valid items into table

							Connection.Execute($@"
							merge into  [metrics].Score T
							using       (
										select  *
										from    api.ExecutionScore
										where   ExecutionID = @ExecutionID 
												and ItemNumber between {beginItemNumber} and {endItemNumber}
												and Success = 1
										) S
							on          (S.ScoreUid = T.Uid)
							when    matched and T.Value <> S.Score then
							update  set
									T.Value = S.Score,
									T.RunDate = S.RunDate
							when    not matched by target then
							insert  (Uid, AssetUid, EffectiveDate, Value, RunDate, AllocationUid)
							values  (S.ScoreUid, S.AssetUid, S.EffectiveDate, S.Score, S.RunDate, S.AllocationUid);

							merge into  [metrics].ScoreItem T
							using       (
										select      E.ScoreItemUid, E.Passed, M.RunDate, E.MetricAssetVersionUid 
										from        api.ExecutionMeasure E
													inner join api.ExecutionScore M on M.ExecutionID = E.ExecutionID and E.ExecutionID = @ExecutionID and M.ItemNumber = E.ItemNumber
										where       E.ItemNumber between {beginItemNumber} and {endItemNumber}
													and M.Success = 1
										) S
							on          (S.ScoreItemUid = T.Uid)
							when matched then
								update set
										T.[Value] = S.Passed,
										T.RunDate = S.RunDate,
										T.UpdatedOn = getutcdate()
							when not matched by target then
								insert  (Uid, AssetVersionUid, [Value], RunDate, UpdatedOn)
								values  (S.ScoreItemUid, S.MetricAssetVersionUid, S.Passed, S.RunDate, getutcdate());

							merge into  [metrics].ScoreItemLink T
							using       (
										select      E.ScoreUid, E.ScoreItemUid
										from        api.ExecutionMeasure E
													inner join api.ExecutionScore M on M.ExecutionID = E.ExecutionID and E.ExecutionID = @ExecutionID and M.ItemNumber = E.ItemNumber
										where       E.ItemNumber between {beginItemNumber} and {endItemNumber}
													and M.Success = 1
										) S
							on          (S.ScoreUid = T.ScoreUid and S.ScoreItemUid = T.ScoreItemUid)
							when not matched by target then
								insert  (ScoreUid, ScoreItemUid)
								values  (S.ScoreUid, S.ScoreItemUid);"
								, new { execution.ExecutionID }
								, transaction: trans
								, commandTimeout: timeout);

							// End-date new scores and score items IF the effective date is not the latest effective date.
							Connection.Execute($@"
							update  M
							set     M.EndDate = dateadd(d, -1, R.EffectiveDate)
							from    [metrics].[Score] M
									inner join api.ExecutionScore E on  E.ExecutionId = @ExecutionID 
																		and E.Success = 1 
																		and E.ItemNumber between {beginItemNumber} and {endItemNumber}
																		and E.ScoreUid = M.Uid 
									cross apply (
												select      min(EffectiveDate) as EffectiveDate 
												from        metrics.Score
												where       AssetUid = M.AssetUid
															and EffectiveDate > M.EffectiveDate 
															and AllocationUid = M.AllocationUid
									) R
							where   M.EndDate is null",
							new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

							// End-date earlier scores and score items.
							Connection.Execute($@"
												update  T 
												set     T.EndDate = DATEADD(d, -1, M.EffectiveDate) 
												from    metrics.Score T 
														inner join api.ExecutionScore S on S.AllocationUid = T.AllocationUid and S.AssetUid = T.AssetUid and S.EffectiveDate > T.EffectiveDate and T.EndDate is null 
																							and S.ExecutionId = @ExecutionID and S.ItemNumber between {beginItemNumber} and {endItemNumber}
														cross apply (
																	select      min(EffectiveDate) as EffectiveDate 
																	from        metrics.Score
																	where       AssetUid = T.AssetUid
																				and EffectiveDate > T.EffectiveDate 
																				and AllocationUid = T.AllocationUid
														) M",
							new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

							List<ExternalScoreResultApiResponseModel> batchResults = 
								Connection.Query<ExternalScoreResultApiResponseModel>($@"
																						select  E.ScoreUid, 
																								E.AllocationUid,
																								E.AssetUid, 
																								E.EffectiveDate, 
																								E.Success as IsSuccess, 
																								E.RunDate, 
																								E.Score, 
																								E.[Message] as ErrorMessage, 
																								M.[Value] as measuresJson
																						from    api.ExecutionScore E
																								outer apply (
																											select  (
																													select  MetricAssetUid as MeasureUid, 
																															Passed 
																													from    api.ExecutionMeasure
																													where   ExecutionID = E.ExecutionID 
																															and ItemNumber = E.ItemNumber
																													for json path
																													) as [value]
																											) M 
																						where   E.ExecutionID = @ExecutionID 
																								and E.ItemNumber between {beginItemNumber} and {endItemNumber}",
																								new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout).ToList();

							results.AddRange(batchResults);

							#endregion

							trans.Commit();

							runCompleted = true;
						}
						catch (Exception ex)
						{
							try
							{
								if (trans != null)
								{
									trans.Rollback();
								}
							}
							catch
							{
							}

							retryCount++;

							if (retryCount > API_V2_RETRY_LIMIT)
							{
								LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionScore", ex.GetFullExceptionData(false), timeout);
							}
						}
					}
				}

				beginItemNumber += loopSize;
				endItemNumber += loopSize;
			}

			#endregion

			try
			{
				CreateExternalScoreWorkflowCheckExecution(execution.ExecutionID);

				execution.Error = results.Count(i => !i.IsSuccess);
				execution.Processed = results.Count(i => i.IsSuccess);
				execution.CompletedOn = DateTime.UtcNow;

				Update(execution);

				// Cleanup
				Connection.Execute($"delete api.ExecutionMeasure where ExecutionID = @ExecutionID", new { execution.ExecutionID }, commandTimeout: timeout);
				Connection.Execute($"delete api.ExecutionScore where ExecutionID = @ExecutionID", new { execution.ExecutionID }, commandTimeout: timeout);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				Update(execution);
			}

			Connection.Close();

			return results;
		}
		
		private List<InternalScoreResultApiResponseModel> BulkMetricsImport(List<InternalScoreResultApiRequestModel> model, ApiExecution execution, bool isSpecificAllocation)
		{
			Add(execution);
			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			// Set effective date for any results that do not have a date set.
			model.ForEach(m =>
			{
				if (!m.effectiveDate.HasValue)
				{
					m.effectiveDate = DateTime.UtcNow.Date;
				}
			});

			bool dupes = model
				.GroupBy(i => new { i.assetUid, i.metricAssetUid, i.effectiveDate })
				.Where(i => i.Count() > 1)
				.Any();

			if (dupes)
			{
				string message = OthersError.DuplicateCombination;
				execution.Error = 1;
				execution.Processed = 0;
				execution.CompletedOn = DateTime.UtcNow;
				execution.ErrorMessage = message;

				Update(execution);

				throw new GenericException(
					System.Net.HttpStatusCode.BadRequest,
					AssetTypeErrors.DuplicateItem,
					message);
			}
			else
			{
				DataTable table = new DataTable();

				table.Columns.Add("AssetUid", typeof(Guid));
				table.Columns.Add("MetricAssetUid", typeof(Guid));
				table.Columns.Add("AllocationUid", typeof(Guid));
				table.Columns["AllocationUid"].AllowDBNull = true;
				table.Columns.Add("ScoreType", typeof(int));
				table.Columns["ScoreType"].AllowDBNull = true;
				table.Columns.Add("EffectiveDate", typeof(DateTime));
				table.Columns.Add("Result", typeof(bool));

				#region Generate data sets

				foreach (InternalScoreResultApiRequestModel item in model)
				{
					DataRow row = table.NewRow();
					row["AssetUid"] = item.assetUid;
					row["MetricAssetUid"] = item.metricAssetUid;

					if (item.scoreType.HasValue)
					{
						row["ScoreType"] = (int)item.scoreType.Value;
					}
					else
					{
						row["ScoreType"] = DBNull.Value;
					}

					if (item.allocationUid.HasValue)
					{
						row["AllocationUid"] = item.allocationUid.Value;
					}
					else
					{
						row["AllocationUid"] = DBNull.Value;
					}

					row["EffectiveDate"] = item.effectiveDate.Value;
					row["Result"] = item.result;

					table.Rows.Add(row);
				}

				#endregion

				if (Connection.State != ConnectionState.Open)
				{
					Connection.Open();
				}

				SqlTransaction trans = Connection.BeginTransaction();
				List<InternalScoreResultApiResponseModel> results = null;

				try
				{
					Connection.Execute(@"
										DROP TABLE IF EXISTS #InternalMeasures;

										CREATE TABLE #InternalMeasures (
											RowNumber int identity, 
											AssetUid uniqueidentifier NOT NULL,
											MetricAssetUid uniqueidentifier NOT NULL,
											EffectiveDate date NOT NULL,
											Result bit NOT NULL,

											ScoreType int null,
											AllocationUid uniqueidentifier null,

											IsValidAllocation bit NULL,
											IsValidAsset bit NULL,
											IsValidMeasure bit NULL,
											IsValidCheck bit null,
											IsValidEffectiveDate bit NULL,
	
											Success bit NULL,
											[Message] nvarchar(2500) NULL,

											PRIMARY KEY ( RowNumber ASC )
										);

										CREATE NONCLUSTERED INDEX [IX_TempInternalMeasures_AssetUid] ON #InternalMeasures ( [AssetUid] ASC );
										CREATE NONCLUSTERED INDEX [IX_TempInternalMeasures_MetricAssetUid] ON #InternalMeasures ( [MetricAssetUid] ASC, EffectiveDate DESC );
										CREATE NONCLUSTERED INDEX [IX_TempInternalMeasures_Success] ON #InternalMeasures ( [Success] ASC )", transaction: trans);

					using (SqlBulkCopy bulk = Connection.CreateBulkCopy("#InternalMeasures", trans: trans))
					{
						bulk.ColumnMappings.Add("AssetUid", "AssetUid");
						bulk.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
						bulk.ColumnMappings.Add("ScoreType", "ScoreType");
						bulk.ColumnMappings.Add("AllocationUid", "AllocationUid");
						bulk.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
						bulk.ColumnMappings.Add("Result", "Result");

						bulk.WriteToServer(table);
					}

					#region Validation

					// Resolve Allocation if scoreType is used.
					if (!isSpecificAllocation)
					{
						Connection.Execute(@"
											update  M
											set     M.AllocationUid = L.Uid 
											from    #InternalMeasures M
													inner join AssetWithType A on A.uid = M.AssetUid
													inner join metrics.Allocation L on L.ScoreType = M.ScoreType and L.AssetTypeUid = A.AssetTypeUid and L.OverrideName is null;", transaction: trans);
					}

					Connection.Execute(@"
										update  #InternalMeasures 
										set     IsValidAllocation = 0, 
												Message = coalesce(Message, '') + 'This asset does not have this score type allocated; ' 
										where   AllocationUid is null; 

										update  M
										set     M.IsValidAllocation = 0,
												M.Message = coalesce(Message, '') + 'This asset does not have this score type allocated for internal scores; '
										from    #InternalMeasures M
												inner join metrics.Allocation L on L.Uid = M.AllocationUid and L.IsExternallyCalculated = 1; 

										update  #InternalMeasures 
										set     IsValidAllocation = 1 
										where   AllocationUid is not null 
												and IsValidAllocation is null; 

										update  M
										set     M.IsValidAsset = IIF(A.ID is not null, 1, 0) 
										from    #InternalMeasures M
												inner join metrics.Allocation L on L.Uid = M.AllocationUid and M.IsValidAllocation = 1
												left join AssetWithType A on A.uid = M.AssetUid and A.AssetTypeUid = L.AssetTypeUid;", transaction: trans);

															// Resolve Measure
															Connection.Execute(@"
										update  T 
										set     T.IsValidMeasure = IIF(S.[Uid] is not null, 1, 0) 
										from    #InternalMeasures T 
												left join metrics.[Asset] S on S.AllocationUid = T.AllocationUid and T.IsValidAllocation = 1 and S.[Uid] = T.MetricAssetUid and S.[State] = 1", transaction: trans);

															// Resolve Measure Check
															Connection.Execute(@"
										update  T 
										set     T.IsValidCheck = IIF(V.[Uid] is not null, 1, 0) 
										from    #InternalMeasures T 
												left join metrics.[Asset] S on S.[Uid] = T.MetricAssetUid and S.[State] = 1
												outer apply (
															select max(EffectiveDate) as EffectiveDate from metrics.AssetVersion where [AssetUid] = S.[Uid] and EffectiveDate <= T.[EffectiveDate]
															) M_M
												left join metrics.AssetVersion V on V.AssetUid = S.Uid and V.EffectiveDate = M_M.EffectiveDate and JSON_VALUE(V.Definition, '$.Governance.Check') = 'External'", transaction: trans);

															// Resolve Metric Group/Item Effective Date
															Connection.Execute(@"
										update  T 
										set     T.IsValidEffectiveDate = IIF(M_M.EffectiveDate is not null, 1, 0) 
										from    #InternalMeasures T 
												left join metrics.[Asset] A on A.[Uid] = T.MetricAssetUid and A.[State] = 1
												outer apply (
															select max(EffectiveDate) as EffectiveDate from metrics.AssetVersion where [AssetUid] = A.[Uid] and EffectiveDate <= T.[EffectiveDate]
															) M_M", transaction: trans);

																// Log errors
					Connection.Execute(@"
										update  #InternalMeasures
										set     Success = case 
															when IsValidAllocation = 0 then 0
															when IsValidAsset = 0 then 0
															when IsValidMeasure = 0 then 0
															when IsValidCheck = 0 then 0
															when IsValidEffectiveDate = 0 then 0
															else 1
															end;

										update  #InternalMeasures
										set     Message = coalesce(Message, '') + 'Invalid asset specified; '
										where   IsValidAsset = 0;

										update  #InternalMeasures
										set     Message = coalesce(Message, '') + 'Invalid measure specified; '
										where   IsValidMeasure = 0;

										update  #InternalMeasures
										set     Message = coalesce(Message, '') + 'Measure does not have a Test Type of External; '
										where   IsValidCheck = 0 
												and IsValidEffectiveDate = 1 
												and EffectiveDate <= getutcdate();

										update  #InternalMeasures
										set     Message = coalesce(Message, '') + 'Invalid measure specified for the date provided; '
										where   IsValidEffectiveDate = 0;

										update  #InternalMeasures
										set     Success = 0,
												Message = coalesce(Message, '') + 'Effective date cannot be in the future; '
										where   EffectiveDate > getutcdate();

										update #InternalMeasures set Message = null where Success = 1;", new { execution.ExecutionID }, transaction: trans);

					#endregion

					// Send score recalculation notifications.
					Guid scoreExecutionUid = Guid.NewGuid();
					string sql = @"
								set nocount on;
								declare @ef date = cast(getutcdate() as date),
										@scoreExecutionId bigint = 0,
										@successfulRowCount int = 0;

								select @successfulRowCount = count(1) from #InternalMeasures where Success = 1;

								set nocount off;
								if @successfulRowCount > 0
								begin
									insert into metrics.Execution (Uid, TriggeredByExecutionUid, StartedOn, PercentComplete, Failures, Processing)
									values (@scoreExecutionUid, @ExecutionID, getutcdate(), 0, 0, 0);

									select @scoreExecutionId = ID from metrics.Execution where Uid = @scoreExecutionUid;

									insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
										select	@scoreExecutionId as ExecutionID,
												@changeType as ChangeType,
												ROW_NUMBER() OVER(ORDER BY M.AssetUid, M.EffectiveDate) as RowNumber,
												(
												select	M.AssetUid,
														M.EffectiveDate,
														(
														select	IM.AllocationUid,
																IM.MetricAssetUid,
																V.Uid as MetricAssetVersionUid,
																IM.Result
														from	#InternalMeasures IM
																inner join metrics.AssetVersion V on V.AssetUid = IM.MetricAssetUid  and ( (IM.EffectiveDate between V.EffectiveDate and V.EffectiveEndDate) or (IM.EffectiveDate >= V.EffectiveDate and V.EffectiveEndDate is null) )
														where	IM.AssetUid = M.AssetUid
																and IM.EffectiveDate = M.EffectiveDate 
																for json path
														) as Measures
												for json path, WITHOUT_ARRAY_WRAPPER
												) as Payload,
												0 as [State]
										from		#InternalMeasures M
										where		Success = 1
										group by	AssetUid, EffectiveDate;
								end;";

					ScoreQueueChangeType changeType = ScoreQueueChangeType.AssetMeasures;
					int rowsImpacted = Connection.Execute(sql, new { scoreExecutionUid, execution.ExecutionID, changeType = (int)changeType }, commandTimeout: 1200, transaction: trans);

					Connection.Execute(@"
										update  T 
										set     T.Processed = P.[Count], 
												T.Error = E.[Count], 
												T.ProcessingStartedOn = null, 
												T.CompletedOn = getutcdate() 
										from    api.Execution T 
												cross apply (
													select count(1) as [Count] from #InternalMeasures where Success = 1
												) P 
												cross apply (
													select count(1) as [Count] from #InternalMeasures where Success = 0
												) E 
										where   T.ExecutionID = @ExecutionID", new { execution.ExecutionID }, transaction: trans);

					results = Connection.Query<InternalScoreResultApiResponseModel>(
						$"select AssetUid, MetricAssetUid, EffectiveDate, Result, Success as IsSuccess, Message as ErrorMessage from #InternalMeasures",
						new { execution.ExecutionID },
						commandTimeout: 1200, transaction: trans
					).ToList();

					trans.Commit();

					if (rowsImpacted > 0)
					{
						ScoreQueueInfo info = new ScoreQueueInfo
						{
							CompanyID = CurrentCompanyID,
							ResourceID = CurrentResourceID,
							ChangeType = ScoreQueueChangeType.AssetMeasures,
							ExecutionUid = scoreExecutionUid,
							StartedOn = execution.StartedOn
						};
						QueueSource.CreateMessage(Config.GetValue<string>("ScoringQueue"), info);
					}
				}
				catch (Exception ex)
				{
					try
					{
						if (trans != null)
						{
							trans.Rollback();
						}
					}
					catch
					{
					}

					string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = message;
					execution.CompletedOn = DateTime.UtcNow;

					Update(execution);
				}
				finally
				{
					Connection.Close();
				}

				return results;
			}
		}

		private void endEmptyExecution(SqlConnection cnn, long executionId)
		{
			try
			{
				cnn.OpenIfClosed().Wait();
				cnn.Execute("update metrics.Execution set PercentComplete = 1, CompletedOn = getutcdate(), UpdatedOn = getutcdate() where ID = @executionId", new { executionId });
			}
			catch
			{
				// Do nothing.
			}
		}
		
		private bool isScoreAllocationPresentForIntersectType(int id)
		{
			bool present = Query<bool>(@"
										select	cast(iif(count(1)>0,1,0) as bit) 
										from	IntersectType T
												inner join AssetType A on A.ID = T.SubjectAssetTypeID or A.ID = T.ObjectAssetTypeID
												inner join metrics.Allocation L on L.AssetTypeUid = A.Uid and L.ScoreType = 1
										where   T.ID = @id", new { id }).Single();

			return present;
		}	
		
		private void sendScoreQueueMessage(ScoreExecution execution, ScoreQueueChangeType changeType = ScoreQueueChangeType.AssetMeasures)
		{
			ScoreQueueInfo info = new ScoreQueueInfo
			{
				CompanyID = CurrentCompanyID,
				ResourceID = CurrentResourceID,
				ChangeType = changeType,
				ExecutionUid = execution.Uid,
				StartedOn = execution.StartedOn
			};

			QueueSource.CreateMessage(Config.GetValue<string>("ScoringQueue"), info);
		}

		#endregion

		#region Methods
		
		public DataQualityMeasureQueryModel BuildDataQualityMeasureQueryModel(MetricDataQualityQueryType queryType, Guid assetVersionRollupPathUid)
		{
			DataQualityMeasureQueryModel dqQueryDetail = new DataQualityMeasureQueryModel
			{
				AssetVersionRollupPathUid = assetVersionRollupPathUid
			};

			Connection.OpenIfClosed().Wait();

			SqlMapper.GridReader dqQueryDetails = Connection.QueryMultiple(
				"metrics.BuildDataQualityMeasureQuery @queryType, @assetVersionRollupPathUid",
				new { queryType = (int)queryType, assetVersionRollupPathUid }
				);
			IEnumerable<string> resultSqlQueryStatements = dqQueryDetails.Read<string>();
			dqQueryDetail.FilterMatchType = dqQueryDetails.Read<MetricMatchType>().Single();
			IEnumerable<DataQualityMeasureQueryFilterModel> resultFilters = dqQueryDetails.Read<DataQualityMeasureQueryFilterModel>();

			dqQueryDetail.Sql = string.Join("", resultSqlQueryStatements);
			dqQueryDetail.Filters = resultFilters.ToList();

			string filterSql = "";
			if (dqQueryDetail.Filters.Count > 0)
			{

				dqQueryDetail.Filters.ForEach(f =>
				{
					string listFieldQuery = $" in (select FD.AssetID from FieldDetail FD inner join FieldLookupValue LV on LV.FieldTypeID = FD.FieldTypeID and LV.Value = FD.Value and FD.AssetTypeID = {f.AssetTypeID} and FD.FieldTypeID = {f.FieldTypeID} and ";
					string nonListFieldQuery = $" in (select AssetID from FieldDetail where AssetTypeID = {f.AssetTypeID} and FieldTypeID = {f.FieldTypeID} and ";

					f.WhereQuery += ((f.Type == "Lookup") ? listFieldQuery : nonListFieldQuery);
					string queryColumn = ((f.Type == "Lookup") ? "LV.AssetUid" : "FormattedValue");

					string paramName = $"@P{f.AssetTypeID}_{f.FieldTypeID}";
					string dbTypeToCastTo = "";
					switch (f.Type)
					{
						case "Date":
						case "DateTime":
							dbTypeToCastTo = "datetime";
							DateTime dt;
							if (DateTime.TryParse(f.Value, out dt))
							{
								f.Parameter = new SqlParameter(paramName, dt);
							}
							break;
						case "Decimal":
							dbTypeToCastTo = "decimal";
							decimal dc;
							if (decimal.TryParse(f.Value, out dc))
							{
								f.Parameter = new SqlParameter(paramName, dc);
							}
							break;
						case "Number":
							dbTypeToCastTo = "bigint";
							long lg;
							if (long.TryParse(f.Value, out lg))
							{
								f.Parameter = new SqlParameter(paramName, lg);
							}
							break;
						default:
							if (!string.IsNullOrEmpty(f.Value))
							{
								f.Parameter = new SqlParameter(paramName, f.Value);
							}
							break;
					}

					switch (f.Operator)
					{
						case Operator.After:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) > {paramName}";
							break;
						case Operator.Before:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) < {paramName}";
							break;
						case Operator.Contains:
							queryColumn = $"{queryColumn} like '%' + {paramName} + '%'";
							break;
						case Operator.EndsWith:
							queryColumn = $"{queryColumn} like '%' + {paramName}";
							break;
						case Operator.Equals:
							queryColumn = (string.IsNullOrEmpty(dbTypeToCastTo)) ?
								$"{queryColumn} = {paramName}" :
								$"try_cast({queryColumn} as {dbTypeToCastTo}) = {paramName}";
							break;
						case Operator.GreaterThan:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) > {paramName}";
							break;
						case Operator.GreaterThanOrEquals:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) >= {paramName}";
							break;
						case Operator.IsFalse:
							queryColumn = $"coalesce(try_cast({queryColumn} as bit), 1) = 0";
							break;
						case Operator.IsTrue:
							queryColumn = $"coalesce(try_cast({queryColumn} as bit), 0) = 1";
							break;
						case Operator.LessThan:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) < {paramName}";
							break;
						case Operator.LessThanOrEquals:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) <= {paramName}";
							break;
						case Operator.NotContains:
							queryColumn = $"{queryColumn} not like '%' + {paramName} + '%'";
							break;
						case Operator.NotEquals:
							queryColumn = (string.IsNullOrEmpty(dbTypeToCastTo)) ?
								$"{queryColumn} <> {paramName}" :
								$"try_cast({queryColumn} as {dbTypeToCastTo}) <> {paramName}";
							break;
						case Operator.NotPopulated:
							queryColumn = $"{queryColumn} is null";
							break;
						case Operator.OnOrAfter:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) >= {paramName}";
							break;
						case Operator.OnOrBefore:
							queryColumn = $"try_cast({queryColumn} as {dbTypeToCastTo}) <= {paramName}";
							break;
						case Operator.Populated:
							queryColumn = $"{queryColumn} is not null";
							break;
						case Operator.StartsWith:
							queryColumn = $"{queryColumn} like {paramName} + '%'";
							break;
						default: //does the same thing as Equals
							queryColumn = (string.IsNullOrEmpty(dbTypeToCastTo)) ?
								$"{queryColumn} = {paramName}" :
								$"try_cast({queryColumn} as {dbTypeToCastTo}) = {paramName}";
							break;

					}

					f.WhereQuery += queryColumn + ")";
				});

				filterSql = " and (" + string.Join(
					dqQueryDetail.FilterMatchType == MetricMatchType.Any ? " or " : " and ",
					dqQueryDetail.Filters.Select(f => f.WhereQuery)
					) + ") ";
			}

			dqQueryDetail.Sql = dqQueryDetail.Sql.Replace("{{FILTERS}}", filterSql);

			return dqQueryDetail;
		}
		
		public List<ExternalScoreResultApiResponseModel> BulkExternalResultsImport(List<ExternalScoreResultApiRequestModel> model, ApiExecution execution, MetricAllocation allocation)
		{
			model.ForEach(m =>
			{
				m.allocationUid = allocation.Uid;
			});

			return BulkExternalResultsImport(model, execution, true);
		}

		public List<ExternalScoreResultApiResponseModel> BulkExternalResultsImport(List<ExternalScoreResultApiRequestModel> model, ApiExecution execution, ScoreType scoreType)
		{
			model.ForEach(m =>
			{
				m.scoreType = scoreType;
			});

			return BulkExternalResultsImport(model, execution, false);
		}

		public List<InternalScoreResultApiResponseModel> BulkMetricsImport(List<InternalScoreResultApiRequestModel> model, ApiExecution execution, MetricAllocation allocation)
		{
			model.ForEach(m =>
			{
				m.allocationUid = allocation.Uid;
			});

			return BulkMetricsImport(model, execution, true);
		}

		public List<InternalScoreResultApiResponseModel> BulkMetricsImport(List<InternalScoreResultApiRequestModel> model, ApiExecution execution, ScoreType scoreType = ScoreType.Governance)
		{
			model.ForEach(m =>
			{
				m.scoreType = scoreType;
			});

			return BulkMetricsImport(model, execution, false);
		}
		
		public void CreateCheckDependencyRemovedNotificationExecution(List<Guid> versionUids)
		{
			ScoreExecution execution = createScoreExecution();

			ScoreExecutionItem executionItem = new ScoreExecutionItem
			{
				ChangeType = ScoreQueueChangeType.CheckTypeDependencyRemoved,
				ExecutionID = execution.ID,
				State = ScoreExecutionItemState.NotProcessed,
				RowNumber = 1,
				Payload = JsonConvert.SerializeObject(new CheckTypeDependencyRemovedModel { VersionUids = versionUids })
			};
			Add(executionItem);

			sendScoreQueueMessage(execution, ScoreQueueChangeType.CheckTypeDependencyRemoved);
		}

		public void CreateCheckDependencyRemovedResultExecution(List<Guid> versionUids)
		{
			string sql = @"
							create table #results (AssetUid uniqueidentifier, AllocationUid  uniqueidentifier, MetricAssetUid uniqueidentifier, MetricAssetVersionUid uniqueidentifier, Result bit)
							insert into #results
								select	distinct
										S.AssetUid,
										A2.AllocationUid,
										A2.Uid as MetricAssetUid,
										V2.Uid as MetricAssetVersionUid,
										I2.Value as Result
								from	metrics.AssetVersion V
										inner join metrics.ScoreItem I on  I.AssetVersionUid = V.Uid
										inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid 
										inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
										inner join metrics.ScoreItemLink L2 on L2.ScoreUid = S.Uid and L2.ScoreItemUid <> I.Uid
										inner join metrics.ScoreItem I2 on I2.Uid = L2.ScoreItemUid
										inner join metrics.AssetVersion V2 on V2.Uid = I2.AssetVersionUid and V2.State = 1
										inner join metrics.Asset A2 on A2.State = 1 and A2.Uid = V2.AssetUid and A2.IsGroup = 0
								where   V.Uid in @versionUids;

							CREATE INDEX IX_TempResults ON #results ( AssetUid );

							declare @ef date = cast(getutcdate() as date);

							insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
								select  *
								from    (
										select	@ID as ExecutionID,
												@changeType as ChangeType,
												ROW_NUMBER() over (order by AssetUid asc) as RowNumber,
												(
												select	A.AssetUid,
														@ef as EffectiveDate,
														(
														select	AllocationUid,
																MetricAssetUid,
																MetricAssetVersionUid,
																Result
														from	#results
														where	AssetUid = A.AssetUid
														for json path
														) as Measures
												for json path, WITHOUT_ARRAY_WRAPPER
												) as Payload,
												0 as [State]
										from	#results A
										group by A.AssetUid
										) J 
								where   J.Payload like '%Measures%';";

			ScoreExecution execution = createScoreExecution();

			Connection.OpenIfClosed().Wait();

			int rowsImpacted = Connection.Execute(
				sql,
				new
				{
					execution.ID,
					versionUids,
					changeType = (int)ScoreQueueChangeType.AssetMeasures
				});

			if (rowsImpacted > 0)
			{
				sendScoreQueueMessage(execution);
			}
			else
			{
				endEmptyExecution(Connection, execution.ID);
			}
		}

		public void CreateExternalScoreWorkflowCheckExecution(Guid apiExecutionUid)
		{
			string sql = @"
						insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
							select  @ID as ExecutionID, 
									@changeType as ChangeType,
									E.ItemNumber as RowNumber,
									(
									select	E.AllocationUid,
											E.AssetUid, 
											E.EffectiveDate
									for json path, WITHOUT_ARRAY_WRAPPER
									) as Payload,
									0 as [State]
							from    api.ExecutionScore E
							where   E.ExecutionID = @apiExecutionUid ";

			ScoreExecution execution = createScoreExecution();

			Connection.OpenIfClosed().Wait();

			int rowsImpacted = Connection.Execute(
				sql,
				new
				{
					execution.ID,
					apiExecutionUid,
					changeType = (int)ScoreQueueChangeType.WorkflowCheck
				});

			if (rowsImpacted > 0)
			{
				sendScoreQueueMessage(execution, ScoreQueueChangeType.WorkflowCheck);
			}
			else
			{
				endEmptyExecution(Connection, execution.ID);
			}
		}

		public void CreateImportAssetsExecution(Guid apiExecutionUid, Guid assetTypeUid)
		{
			string sql = @"
						declare @ef date = cast(getutcdate() as date); 
						insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
							select  * 
							from    (
									select	@ID as ExecutionID,
											@changeType as ChangeType,
											EA.ItemNumber as RowNumber,
											(
											select	EA.Uid as AssetUid, 
													@ef as EffectiveDate,
													(
													select	A.Uid as AllocationUid,
															M.Uid as MetricAssetUid,
															V.Uid as MetricAssetVersionUid,
															cast(0 as bit) as Result
													from	metrics.Allocation A 
															inner join metrics.Asset M on M.AllocationUid = A.Uid and A.AssetTypeUid = @assetTypeUid and M.State = 1 and A.ScoreType = 1 and A.IsExternallyCalculated = 0 and M.IsGroup = 0
															cross apply (
																select	Uid
																from	metrics.AssetVersion 
																where	AssetUid = M.Uid
																		and EffectiveDate <= getutcdate()
																		and EffectiveEndDate is null
																		and JSON_VALUE(Definition, '$.Governance.Check') <> 'External'
																		and Definition <> '{}'
															) V
													for json path
													) as Measures
											for json path, WITHOUT_ARRAY_WRAPPER
											) as Payload,
											0 as [State]
									from	api.ExecutionAsset EA 
									where	EA.ExecutionID = @apiExecutionUid
											and EA.Success = 1
									) J 
							where   J.Payload like '%Measures%';";

			ScoreExecution execution = createScoreExecution(apiExecutionUid);

			int rowsImpacted = Connection.Execute(
				sql,
				new
				{
					apiExecutionUid,
					execution.ID,
					changeType = (int)ScoreQueueChangeType.AssetMeasures,
					assetTypeUid
				});

			if (rowsImpacted > 0)
			{
				sendScoreQueueMessage(execution);
			}
			else
			{
				endEmptyExecution(Connection, execution.ID);
			}
		}

		public void CreateDeleteRelationshipsExecution(Guid apiExecutionUid, int intersectTypeId)
		{
			if (isScoreAllocationPresentForIntersectType(intersectTypeId))
			{
				string sql = @"
							declare @ef date = cast(getutcdate() as date); 
							insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
								select  *
								from    (
										select	@ID as ExecutionID,
												@changeType as ChangeType,
												ROW_NUMBER() OVER(order by A.ID) as RowNumber,
												(
												select	A.Uid as AssetUid, 
														@ef as EffectiveDate,
														(
														select	SAL.Uid as AllocationUid,
																SA.Uid as MetricAssetUid,
																V.Uid as MetricAssetVersionUid,
																cast(0 as bit) as Result
														from	metrics.Allocation SAL
																inner join metrics.Asset SA on SA.AllocationUid = SAL.Uid and SA.State = 1 and SA.IsGroup = 0
																cross apply (
																	select	Uid
																	from	metrics.AssetVersion 
																	where	AssetUid = SA.Uid
																			and EffectiveDate <= getutcdate()
																			and EffectiveEndDate is null
																			and (
																			(JSON_VALUE(Definition, '$.Governance.Check') = 'Relation' and JSON_VALUE(Definition, '$.Governance.Relation.IntersectTypeUid') = T.Uid)
																			or 
																			(JSON_VALUE(Definition, '$.Governance.Check') = 'Predicate' and JSON_VALUE(Definition, '$.Governance.Predicate.PredicateUid') = P.Uid)                        
																			)
																			and Definition is not null 
																			and Definition <> 'null' 
																			and Definition <> '{}'
																) V
														where	SAL.AssetTypeUid = ST.Uid and SAL.ScoreType = 1
																for json path
														) as Measures
												for json path, WITHOUT_ARRAY_WRAPPER
												) as Payload,
												0 as [State]
										from	api.ExecutionDeletedRelationship ER
												inner join api.Execution Ex on Ex.ExecutionID = ER.ExecutionID
												cross apply openjson(Ex.Fields) with (IntersectTypeUid uniqueidentifier '$.IntersectTypeUid') DF
												inner join IntersectType T on T.Uid = DF.IntersectTypeUid
												inner join Predicate P on P.ID = T.PredicateID 
												inner join Asset A on (A.ID = ER.SubjectID or A.ID = ER.ObjectID)
													and ER.ExecutionID = @apiExecutionUid 
													and ER.Success = 1
												inner join AssetType ST on ST.ID = A.AssetTypeID
										) J 
								where   J.Payload like '%Measures%';";

				ScoreExecution execution = createScoreExecution(apiExecutionUid);

				Connection.OpenIfClosed().Wait();

				int rowsImpacted = Connection.Execute(
					sql,
					new
					{
						apiExecutionUid,
						execution.ID,
						changeType = (int)ScoreQueueChangeType.AssetMeasures
					});

				if (rowsImpacted > 0)
				{
					sendScoreQueueMessage(execution);
				}
				else
				{
					endEmptyExecution(Connection, execution.ID);
				}
			}
		}

		public void CreateImportRelationshipsExecution(Guid apiExecutionUid, int intersectTypeId, int timeout)
		{
			if (isScoreAllocationPresentForIntersectType(intersectTypeId))
			{
				string sql = @"
							declare @ef date = cast(getutcdate() as date); 
							insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
								select  *
								from    (
										select	@ID as ExecutionID,
												@changeType as ChangeType,
												ROW_NUMBER() OVER(order by S.ID) as RowNumber,
												(
												select	S.Uid as AssetUid, 
														@ef as EffectiveDate,
														(
														select	SAL.Uid as AllocationUid,
																SA.Uid as MetricAssetUid,
																V.Uid as MetricAssetVersionUid,
																cast(0 as bit) as Result
														from	metrics.Allocation SAL
																inner join metrics.Asset SA on SA.AllocationUid = SAL.Uid and SA.State = 1 and SA.IsGroup = 0
																cross apply (
																	select	Uid
																	from	metrics.AssetVersion 
																	where	AssetUid = SA.Uid
																			and EffectiveDate <= getutcdate()
																			and EffectiveEndDate is null
																			and (
																			(JSON_VALUE(Definition, '$.Governance.Check') = 'Relation' and JSON_VALUE(Definition, '$.Governance.Relation.IntersectTypeUid') = T.Uid)
																			or 
																			(JSON_VALUE(Definition, '$.Governance.Check') = 'Predicate' and JSON_VALUE(Definition, '$.Governance.Predicate.PredicateUid') = P.Uid)                        
																			)
																			and Definition is not null 
																			and Definition <> 'null' 
																			and Definition <> '{}'
																) V
														where	SAL.AssetTypeUid = ST.Uid and SAL.ScoreType = 1
																for json path
														) as Measures
												for json path, WITHOUT_ARRAY_WRAPPER
												) as Payload,
												0 as [State]
										from	(
													SELECT a.id, a.Uid, R.IntersectTypeID, A.AssetTypeID
													FROM api.ExecutionRelationship ER
													inner join [Intersect] R on R.ID = ER.IntersectID 
														and ER.ExecutionID = @apiExecutionUid 
														and ER.Success = 1
													inner join Asset A on A.ID = R.SubjectAssetId
													UNION ALL
													SELECT a.id, a.Uid, R.IntersectTypeID, A.AssetTypeID
													FROM api.ExecutionRelationship ER
													inner join [Intersect] R on R.ID = ER.IntersectID 
														and ER.ExecutionID = @apiExecutionUid 
														and ER.Success = 1
													inner join Asset A on A.ID = R.ObjectAssetId
												) S
												inner join IntersectType T on T.ID = S.IntersectTypeID 
												inner join [Predicate] P on P.ID = T.PredicateID 
												inner join AssetType ST on ST.ID = S.AssetTypeID
										) J 
								 where J.Payload like '%Measures%';";

				ScoreExecution execution = createScoreExecution(apiExecutionUid);

				Connection.OpenIfClosed().Wait();

				int rowsImpacted = Connection.Execute(
					sql,
					new
					{
						apiExecutionUid,
						execution.ID,
						changeType = (int)ScoreQueueChangeType.AssetMeasures
					}, commandTimeout: timeout);

				if (rowsImpacted > 0)
				{
					sendScoreQueueMessage(execution);
				}
				else
				{
					endEmptyExecution(Connection, execution.ID);
				}
			}
		}

		public Guid CreateMeasureChangedNotificationExecution(MetricAssetVersion version, DateTime effectiveDate, Guid? triggeredByMeasureUid = null)
		{
			ScoreExecution execution = createScoreExecution(triggeredMeasureUid: triggeredByMeasureUid);

			ScoreExecutionItem executionItem = new ScoreExecutionItem
			{
				ChangeType = ScoreQueueChangeType.MeasureChanged,
				ExecutionID = execution.ID,
				State = ScoreExecutionItemState.NotProcessed,
				RowNumber = 1,
				Payload = JsonConvert.SerializeObject(
					new MeasureChangedModel
					{
						EffectiveDate = effectiveDate,
						MetricAssetUid = version.AssetUid,
						MetricAssetVersionUid = version.Uid
					}
				)
			};

			Add(executionItem);

			sendScoreQueueMessage(execution, ScoreQueueChangeType.MeasureChanged);

			return execution.Uid;
		}

		public void CreateMeasureChangedResultExecution(List<AssetMeasureModel> list, Guid? apiExecutionUid = null)
		{
			if (list.Count > 0)
			{
				List<ScoreExecutionItem> scoreExecutionItems = new List<ScoreExecutionItem>();
				List<DateTime> effectiveDates = list.Select(o => o.EffectiveDate).Distinct().OrderBy(o => o).ToList();
				effectiveDates.ForEach(ed =>
				{
					ScoreExecution execution = createScoreExecution(apiExecutionUid);

					DataTable itemsTable = new DataTable();
					itemsTable.Columns.Add("ExecutionID", typeof(long));
					itemsTable.Columns.Add("ChangeType", typeof(int));
					itemsTable.Columns.Add("RowNumber", typeof(int));
					itemsTable.Columns.Add("Payload", typeof(string));

					scoreExecutionItems.Clear();
					List<AssetMeasureModel> assetMeasuresSubset = list.Where(m => m.EffectiveDate == ed).ToList();
					for (int i = 0; i < assetMeasuresSubset.Count; i++)
					{
						DataRow itemRow = itemsTable.NewRow();
						itemRow["ExecutionID"] = execution.ID;
						itemRow["ChangeType"] = (int)ScoreQueueChangeType.AssetMeasures;
						itemRow["RowNumber"] = i + 1;
						itemRow["Payload"] = JsonConvert.SerializeObject(assetMeasuresSubset[i]);
						itemsTable.Rows.Add(itemRow);
					}

					Connection.OpenIfClosed().Wait();

					Connection.Execute(@"
										drop table if exists #ExecutionItem;
										create table #ExecutionItem (
											ExecutionID bigint not null,
											ChangeType int not null,
											RowNumber int not null,
											Payload nvarchar(max) not null
										);
										alter table #ExecutionItem add primary key ( ExecutionID DESC, ChangeType DESC, RowNumber ASC );");

					using (SqlTransaction trans = Connection.BeginTransaction())
					{

						using (SqlBulkCopy bulkCopy = Connection.CreateBulkCopy("#ExecutionItem", trans: trans))
						{
							bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
							bulkCopy.ColumnMappings.Add("ChangeType", "ChangeType");
							bulkCopy.ColumnMappings.Add("RowNumber", "RowNumber");
							bulkCopy.ColumnMappings.Add("Payload", "Payload");

							bulkCopy.WriteToServer(itemsTable);
						}

						Connection.Execute(@"
											merge [metrics].[ExecutionItem] as T
											using #ExecutionItem as S
											on (S.ExecutionID = T.ExecutionID and S.ChangeType = T.ChangeType and S.RowNumber = T.RowNumber)
											when matched then 
											update set 
												T.Payload = S.Payload,
												T.State = 0
											when not matched then
												insert (ExecutionID, ChangeType, RowNumber, State, Payload) 
												values (S.ExecutionID, S.ChangeType, S.RowNumber, 0, S.Payload);

											truncate table #ExecutionItem;", transaction: trans, commandTimeout: timeout);

						trans.Commit();
					}

					sendScoreQueueMessage(execution);
				});
			}
		}

		public void CreateMeasureRemovedNotificationExecution(MetricAssetVersion version)
		{
			ScoreExecution execution = createScoreExecution();

			ScoreExecutionItem executionItem = new ScoreExecutionItem
			{
				ChangeType = ScoreQueueChangeType.MeasureRemoved,
				ExecutionID = execution.ID,
				State = ScoreExecutionItemState.NotProcessed,
				RowNumber = 1,
				Payload = JsonConvert.SerializeObject(
					new MeasureRemovedModel
					{
						EffectiveEndDate = version.EffectiveEndDate.Value,
						MetricAssetUid = version.AssetUid,
						MetricAssetVersionUid = version.Uid
					}
				)
			};

			Add(executionItem);

			sendScoreQueueMessage(execution, ScoreQueueChangeType.MeasureRemoved);
		}

		public void CreateMeasureRemovedResultExecution(Guid metricAssetVersionUid)
		{
			ScoreExecution execution = createScoreExecution();
			int changeType = (int)ScoreQueueChangeType.AssetMeasures;

			Connection.OpenIfClosed().Wait();

			string sql = "declare @ef date = cast(getutcdate() as date), @numberOfResults int = 0;";

			// Build list of results before deleting score links and other score data.
			sql += @"
					create table #results (AssetUid uniqueidentifier, AllocationUid  uniqueidentifier, MetricAssetUid uniqueidentifier, MetricAssetVersionUid uniqueidentifier, Result bit);

					insert into #results
						select	distinct
								S.AssetUid
								,A.AllocationUid
								,A.Uid as MetricAssetUid
								,V.Uid as MetricAssetVersionUid
								,coalesce(SI.Result, 0) as Result
						from	metrics.ScoreItem I
								inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @metricAssetVersionUid
								inner join metrics.Score S on S.Uid = L.ScoreUid
								inner join metrics.AssetVersion LV on LV.Uid = I.AssetVersionUid 
								inner join metrics.Asset LA on LA.Uid = LV.AssetUid
								inner join metrics.Asset A on A.AllocationUid = LA.AllocationUid and A.Uid <> LA.Uid and A.IsGroup = 0 and (A.ParentUid <> LV.AssetUid or A.ParentUid is null)
								cross apply (
									select	max(EffectiveDate) as EffectiveDate
									from	metrics.AssetVersion
									where	AssetUid = A.Uid
											and [State] = 1 
											and EffectiveDate <= @ef 
											and (EffectiveEndDate > @ef or EffectiveEndDate is null)
								) MV
								inner join metrics.AssetVersion V on A.Uid = V.AssetUid and V.State = 1 and V.EffectiveDate = MV.EffectiveDate
								outer apply (
									select	II.Value as Result
									from	metrics.ScoreItemLink IL
											inner join metrics.ScoreItem II on IL.ScoreItemUid = II.Uid and IL.ScoreUid = S.Uid and II.AssetVersionUid = V.Uid
								) SI;

					CREATE INDEX IX_TempResults ON #results ( AssetUid );";

			// Delete asset scores where this measure is the only one that was present and was created today (a one-measure score).
			// Also delete score items linked to these scores that we will be deleting, but are NOT linked to any other (i.e. earlier) scores.
			sql += @"
					create table #Scores (Uid uniqueidentifier, EffectiveDate date, EndDate date, OtherMeasuresCount int);

					insert into #Scores
						select	distinct
								L.ScoreUid,
								S.EffectiveDate,
								S.EndDate,
								C.[Count] as OtherMeasuresCount
						from	metrics.ScoreItem I
								inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @metricAssetVersionUid
								inner join metrics.Score S on S.Uid = L.ScoreUid and S.EffectiveDate = @ef and S.EndDate is null
								cross apply (
									select	count(1) as [Count]
									from	metrics.ScoreItemLink IL
											inner join metrics.ScoreItem II on II.Uid = IL.ScoreItemUid and IL.ScoreUid = S.Uid and IL.ScoreItemUid <> L.ScoreItemUid 
											inner join metrics.AssetVersion IV on IV.Uid = II.AssetVersionUid and IV.State = 1
											inner join metrics.Asset IA on IA.State = 1 and IA.Uid = IV.AssetUid and IA.ParentUid is null
								) C;

					-- Delete the link between this measure version and today's score. 
					delete	L 
					from	metrics.ScoreItemLink L 
							inner join metrics.Score S on S.Uid = L.ScoreUid and S.EffectiveDate = @ef and S.EndDate is null 
							inner join metrics.ScoreItem I on I.Uid = L.ScoreItemUid and I.AssetVersionUid = @metricAssetVersionUid; 

					-- End-date active scores that are prior to current UTC.
					update  T 
					set     T.EndDate = dateadd(dd, -1, @ef) ,
							T.[Log] = T.[Log] + 'End-dated by Score Execution ' + cast(@ExecutionId as varchar(50)) + '; '
					from	metrics.Score T 
							inner join #Scores S on S.Uid = T.Uid and S.EffectiveDate < @ef; 

					-- Delete any staging data tied to this measure version / effective date
					delete	metrics.StagingScoreItem where MeasureVersionUid = @metricAssetVersionUid and EffectiveDate = @ef; 

					-- Delete asset scores where this measure is the only one that was present and was created today (a one-measure score).
					delete  T 
					from	metrics.Score T 
							inner join #Scores S on S.Uid = T.Uid and S.OtherMeasuresCount = 0 and S.EffectiveDate = @ef;";

			// Now insert into execution result table
			sql += @"
					insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
					select  *
					from    (
							select	@ID as ExecutionID,
									@changeType as ChangeType,
									ROW_NUMBER() over (order by AssetUid asc) as RowNumber,
									(
									select	A.AssetUid,
											@ef as EffectiveDate,
											(
											select	AllocationUid,
													MetricAssetUid,
													MetricAssetVersionUid,
													Result
											from	#results
											where	AssetUid = A.AssetUid
											for json path
											) as Measures
									for json path, WITHOUT_ARRAY_WRAPPER
									) as Payload,
									0 as [State]
							from	#results A
							group by A.AssetUid
							) J 
					where   J.Payload like '%Measures%';";

			Connection.OpenIfClosed().Wait();

			int rowsAffected = Connection.Execute(sql, new { ExecutionId = execution.Uid, execution.ID, metricAssetVersionUid, changeType }, commandTimeout: 1200);

			if (rowsAffected > 0)
			{
				sendScoreQueueMessage(execution);
			}
			else
			{
				endEmptyExecution(Connection, execution.ID);
			}
		}

		public void CreateParentAssetGovernanceRescoreExecution(Guid apiExecutionUid)
		{
			string sql = @"
						declare @ef date = cast(getutcdate() as date); 
						declare @assetTypeUid uniqueidentifier;

						select  @assetTypeUid = T.Uid
						from    api.ExecutionItemDependentChange E
								inner join Asset A on A.Uid = JSON_VALUE(Payload, '$.ParentAssetUid')
								inner join AssetType T on T.ID = A.AssetTypeID
						where   ExecutionID = @apiExecutionUid
								and DependentChangeType = 1 --parent change

						insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
							select  distinct 
									* 
							from    (
									select	@ID as ExecutionID,
											@changeType as ChangeType,
											EA.ItemNumber,
											(
											select	EA.AssetUid, 
													@ef as EffectiveDate,
													(
													select	A.Uid as AllocationUid,
															M.Uid as MetricAssetUid,
															V.Uid as MetricAssetVersionUid,
															cast(0 as bit) as Result
													from	metrics.Allocation A 
															inner join metrics.Asset M on M.AllocationUid = A.Uid and A.AssetTypeUid = @assetTypeUid and M.State = 1 and A.ScoreType = 1 and A.IsExternallyCalculated = 0 and M.IsGroup = 0
															cross apply (
																select	Uid
																from	metrics.AssetVersion 
																where	AssetUid = M.Uid
																		and EffectiveDate <= getutcdate()
																		and EffectiveEndDate is null
																		and (
																			(
																			JSON_VALUE(Definition, '$.Governance.Check') = 'Relation'
																			and JSON_VALUE(Definition, '$.Governance.Relation.IntersectTypeUid') in (select T.Uid from IntersectType T inner join Predicate P on P.ID = T.PredicateID and P.[Type] in (3,4))
																			) or (
																			JSON_VALUE(Definition, '$.Governance.Check') = 'Predicate'
																			and JSON_VALUE(Definition, '$.Governance.Predicate.PredicateUid') in (select Uid from Predicate where [Type] in (3,4))
																			)
																		)
																		and Definition <> '{}'
															) V
													for json path
													) as Measures
											for json path, WITHOUT_ARRAY_WRAPPER
											) as Payload,
											0 as [State]
									from	(
											select  ROW_NUMBER() OVER(order by JSON_VALUE(Payload, '$.ParentAssetUid')) ItemNumber,
													JSON_VALUE(Payload, '$.ParentAssetUid') as AssetUid 
											from    api.ExecutionItemDependentChange
											where   ExecutionID = @apiExecutionUid
													and DependentChangeType = 1 --parent change
											) EA
									) J 
							where   J.Payload like '%Measures%';";

			ScoreExecution execution = createScoreExecution(apiExecutionUid);

			int rowsImpacted = Connection.Execute(sql, new { apiExecutionUid, execution.ID, changeType = (int)ScoreQueueChangeType.AssetMeasures });

			if (rowsImpacted > 0)
			{
				sendScoreQueueMessage(execution);
				Connection.Execute(
					"delete api.ExecutionItemDependentChange where ExecutionID = @apiExecutionUid and DependentChangeType = 1",
					new { apiExecutionUid }
					);
			}
			else
			{
				endEmptyExecution(Connection, execution.ID);
			}
		}

		public void CreateRollupPathChangedExecution(int? intersectTypeId = null, int? assetTypeId = null, Guid? triggeredByApiExecutionUid = null)
		{
			int count = Query<int>(@"
									select	count(1) as [Count] 
									from	metrics.Execution E
											inner join metrics.ExecutionItem I on I.ExecutionID = E.ID 
												and I.ChangeType = 5 
												and E.StartedOn > dateadd(day, -1, getutcdate()) 
												and E.CompletedOn is null 
												and (E.Failures = 0 
												and E.ErrorMessage is null)").Single();

			if (count == 0)
			{
				ScoreExecution execution = createScoreExecution(triggeredByApiExecutionUid);

				ScoreExecutionItem executionItem = new ScoreExecutionItem
				{
					ExecutionID = execution.ID,
					ChangeType = ScoreQueueChangeType.RollupPathChanged,
					RowNumber = 1,
					State = ScoreExecutionItemState.NotProcessed,
					Payload = JsonConvert.SerializeObject(new RollupPathChangedModel { IntersectTypeId = intersectTypeId, AssetTypeId = assetTypeId })
				};
				Add(executionItem);

				sendScoreQueueMessage(execution, ScoreQueueChangeType.RollupPathChanged);
			}
		}

		public void CreateRuleResultsRemovedExecution(Guid assetUid)
		{
			string sql = @"
						create table #results (AssetUid uniqueidentifier, AllocationUid  uniqueidentifier, MetricAssetUid uniqueidentifier, MetricAssetVersionUid uniqueidentifier, EffectiveDate date, Result bit)
						insert into #results
							select	S.AssetUid,
									A.AllocationUid,
									V.AssetUid as MetricAssetUid,
									I.AssetVersionUid as MetricAssetVersionUid,
									S.EffectiveDate,
									0 as Result
							from	metrics.ScoreItem I
									inner join metrics.AssetVersion V on V.Uid = I.AssetVersionUid
									inner join metrics.Asset A on A.Uid = V.AssetUid
									inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 2
									cross apply openjson(I.Evidence) Ev
									cross apply openjson(Ev.value) Rp 
									cross apply openjson(Rp.value)  with (Uid nvarchar(max) '$.Uid') as P
									inner join metrics.ScoreItemLink SIL on SIL.ScoreItemUid = I.Uid
									inner join metrics.Score S on S.Uid = SIL.ScoreUid
							where	Evidence <> '{}' 
									and Evidence is not null
									and ISNUMERIC(Ev.[key]) = 1
									and Rp.[key] = 'RollupPath' 
									and P.Uid = @assetUid;

						CREATE INDEX IX_TempResults ON #results ( AssetUid );

						insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
							select  * 
							from    (
									select	@ID as ExecutionID,
											@changeType as ChangeType,
											ROW_NUMBER() over (order by AssetUid asc) as RowNumber,
											(
											select	A.AssetUid,
													A.EffectiveDate,
													(
													select	AllocationUid,
															MetricAssetUid,
															MetricAssetVersionUid,
															Result
													from	#results
													where	AssetUid = A.AssetUid
															and EffectiveDate = A.EffectiveDate
													for json path
													) as Measures
											for json path, WITHOUT_ARRAY_WRAPPER
											) as Payload,
											0 as [State]
									from	#results A
									group by A.AssetUid, A.EffectiveDate
									) J 
							where   J.Payload like '%Measures%';";

			ScoreExecution execution = createScoreExecution();

			Connection.OpenIfClosed().Wait();

			int rowsImpacted = Connection.Execute(
				sql,
				new
				{
					execution.ID,
					assetUid,
					changeType = (int)ScoreQueueChangeType.AssetMeasures
				});

			if (rowsImpacted > 0)
			{
				sendScoreQueueMessage(execution);
			}
			else
			{
				endEmptyExecution(Connection, execution.ID);
			}
		}

		public void CreateRulesRemovedExecution(Guid apiExecutionUid, List<Guid> assetUids)
		{
			assetUids.ForEach(uid =>
			{
				ScoreExecution execution = createScoreExecution(apiExecutionUid);

				ScoreExecutionItem executionItem = new ScoreExecutionItem
				{
					ChangeType = ScoreQueueChangeType.RuleAssetRemoved,
					ExecutionID = execution.ID,
					State = ScoreExecutionItemState.NotProcessed,
					RowNumber = 1,
					Payload = JsonConvert.SerializeObject(new RuleAssetRemovedModel { AssetUid = uid })
				};
				Add(executionItem);

				sendScoreQueueMessage(execution, ScoreQueueChangeType.RuleAssetRemoved);
			});
		}

		public void CreateRulesRemovedExecution(Guid apiExecutionUid, int assetTypeId)
		{
			bool anyActiveMeasureVersions = Query<bool>(@"
														select cast(iif(count(1) > 0, 1, 0) as bit) as [Any] 
														from metrics.RollupPathSegment Se 
														inner join metrics.AssetVersionRollupPath Ar on Ar.RollupPathUid = Se.RollupPathUid and Se.AssetTypeID = @assetTypeId 
														inner join metrics.AssetVersion Ve on Ve.Uid = Ar.AssetVersionUid and Ve.EffectiveEndDate is null", new { assetTypeId }).First();

			if (anyActiveMeasureVersions)
			{
				List<Guid> assetUids = Query<Guid>("select uid from api.ExecutionDeletedAsset where ExecutionID = @apiExecutionUid and Success = 1", new { apiExecutionUid }).ToList();
				CreateRulesRemovedExecution(apiExecutionUid, assetUids);
			}
		}

		public void CreateWorkflowCheckExecution(ScoreExecution previousExecution, ScoreQueueChangeType previousChangeType)
		{
			ScoreExecution newExecution = createScoreExecution();

			string sql = @"
						insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
							select	@executionID,
									@changeType,
									RowNumber,
									(
										select	json_value(Measures, '$[0].AllocationUid') as AllocationUid,
												AssetUid,
												EffectiveDate
										from	openjson(I.Payload) with (AssetUid uniqueidentifier '$.AssetUid', EffectiveDate date '$.EffectiveDate', Measures nvarchar(max) '$.Measures' as json)
										for json path, WITHOUT_ARRAY_WRAPPER
									) as Payload,
									0 as [State]
							from	metrics.ExecutionItem I
							where	ExecutionID = @previousID 
									and ChangeType = @previousChangeType
									and [State] = 1";

			Connection.OpenIfClosed().Wait();

			int rowsImpacted = Connection.Execute(
				sql,
				new
				{
					previousID = previousExecution.ID,
					previousChangeType = (int)previousChangeType,
					executionID = newExecution.ID,
					changeType = (int)ScoreQueueChangeType.WorkflowCheck,
				});

			if (rowsImpacted > 0)
			{
				sendScoreQueueMessage(newExecution, ScoreQueueChangeType.WorkflowCheck);
			}
			else
			{
				endEmptyExecution(Connection, newExecution.ID);
			}
		}

		public void CreateWorkflowItemFieldUpdateExecution(AssetType assetType, Asset asset)
		{
			if (assetType != null && asset != null && Any<MetricAllocation>(i => i.AssetTypeUid == assetType.uid && i.ScoreType == ScoreType.Governance && !i.IsExternallyCalculated))
			{
				string sql = @"
							declare @ef date = cast(getutcdate() as date)

							insert into metrics.ExecutionItem (ExecutionID, ChangeType, RowNumber, Payload, [State])
								select  * 
								from    ( 
										select	@ID as ExecutionID,
												@changeType as ChangeType,
												1 as RowNumber,
												(
												select	@AssetUid as AssetUid, 
														@ef as EffectiveDate,
														(
														select	A.Uid as AllocationUid,
																M.Uid as MetricAssetUid,
																V.Uid as MetricAssetVersionUid,
																cast(0 as bit) as Result
														from	metrics.Allocation A 
																inner join metrics.Asset M on M.AllocationUid = A.Uid and A.AssetTypeUid = @AssetTypeUid and M.State = 1 and A.ScoreType = 1 and A.IsExternallyCalculated = 0 and M.IsGroup = 0
																cross apply (
																	select	Uid
																	from	metrics.AssetVersion 
																	where	AssetUid = M.Uid
																			and EffectiveDate <= getutcdate()
																			and EffectiveEndDate is null
																			and JSON_VALUE(Definition, '$.Governance.Check') <> 'External'
																			and Definition <> '{}'
																) V
														for json path
														) as Measures
												for json path, WITHOUT_ARRAY_WRAPPER
												) as Payload,
												0 as [State]
										from	Asset EA 
										where	EA.Uid = @AssetUid
										) J 
								where   J.Payload like '%Measures%'";

				ScoreExecution execution = createScoreExecution();

				Connection.OpenIfClosed().Wait();

				int rowsImpacted = Connection.Execute(
					sql,
					new
					{
						execution.ID,
						AssetUid = asset.uid,
						AssetTypeUid = assetType.uid,
						changeType = (int)ScoreQueueChangeType.AssetMeasures
					});

				if (rowsImpacted > 0)
				{
					sendScoreQueueMessage(execution);
				}
				else
				{
					endEmptyExecution(Connection, execution.ID);
				}
			}
		}
		
		public List<DataQualityDeleteResponseModel> DeleteAssetResults(List<DataQualityDeleteModel> import, ApiExecution execution, int timeout = 3600)
		{
			List<DataQualityDeleteResponseModel> results = new List<DataQualityDeleteResponseModel>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var dupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			if (dupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DataQualityDeleteResponseModel { ExecutionItemUid = i.ExecutionItemUid.Value, Message = execution.ErrorMessage, Success = false }));
			}
			else
			{
				try
				{
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeleteAssetResult");

					if (currentLocation.HighestItemNumberProcessed > 0)
					{
						results.AddRange(
							Query<DataQualityDeleteResponseModel>(
								$"select ExecutionItemUid, Success, Message from api.ExecutionDeleteAssetResult where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
								new { execution.ExecutionID },
								timeout
							)
						);
					}

					#region Build data tables.

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));
					table.Columns.Add("Uid", typeof(Guid));
					table.Columns.Add("EvaluatedAssetUid", typeof(Guid));
					table.Columns.Add("OwningAssetUid", typeof(Guid));
					table.Columns.Add("EffectiveDateStart", typeof(string));
					table.Columns.Add("EffectiveDateEnd", typeof(string));
					table.Columns.Add("RunDateStart", typeof(string));
					table.Columns.Add("RunDateEnd", typeof(string));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));

					#endregion

					#region Generate data sets

					for (int i = 1; i <= import.Count; i++)
					{
						if (i > currentLocation.HighestItemNumber)
						{
							DataQualityDeleteModel model = import[i - 1];
							List<string> messages = new List<string>();
							DataRow row = table.NewRow();
							DateTime effectiveDateStart = new DateTime();
							DateTime runDateStart = new DateTime();

							row["ExecutionID"] = execution.ExecutionID;
							row["ExecutionItemUid"] = model.ExecutionItemUid ?? Guid.NewGuid();
							row["ItemNumber"] = i;

							if (model.Uid.HasValue)
							{
								row["Uid"] = model.Uid.Value;
							}
							else
							{
								row["Uid"] = DBNull.Value;
							}

							if (model.OwningAssetUid.HasValue)
							{
								row["OwningAssetUid"] = model.OwningAssetUid.Value;
							}
							else
							{
								row["OwningAssetUid"] = DBNull.Value;
							}

							if (model.EvaluatedAssetUid.HasValue)
							{
								row["EvaluatedAssetUid"] = model.EvaluatedAssetUid.Value;
							}
							else
							{
								row["EvaluatedAssetUid"] = DBNull.Value;
							}

							if (model.EffectiveDateStart != null)
							{
								row["EffectiveDateStart"] = model.EffectiveDateStart;

								if (!DateTime.TryParseExact(model.EffectiveDateStart,
														"yyyy-MM-dd",
														System.Globalization.CultureInfo.InvariantCulture,
														System.Globalization.DateTimeStyles.None,
														out effectiveDateStart))
								{
									row["Message"] = string.Format(DataQualityErrors.InvalidFormatError, "EffectiveDateStart", "yyyy-MM-dd");
									row["Success"] = 0;
								}
							}

							if (model.EffectiveDateEnd != null)
							{
								row["EffectiveDateEnd"] = model.EffectiveDateEnd;

								if (!DateTime.TryParseExact(model.EffectiveDateEnd,
														"yyyy-MM-dd",
														System.Globalization.CultureInfo.InvariantCulture,
														System.Globalization.DateTimeStyles.None,
														out DateTime effectiveDateEnd))
								{
									row["Message"] = string.Format(DataQualityErrors.InvalidFormatError, "EffectiveDateEnd", "yyyy-MM-dd");
									row["Success"] = 0;
								}
								else if (model.EffectiveDateStart != null && effectiveDateStart > effectiveDateEnd)
								{
									messages.Add(string.Format(DataQualityErrors.GreaterThanError, "EffectiveDateStart", "EffectiveDateEnd"));
									row["Success"] = 0;
								}
							}

							if (model.RunDateStart != null)
							{
								row["RunDateStart"] = model.RunDateStart;

								if (!DateTime.TryParseExact(model.RunDateStart,
														"yyyy-MM-dd HH:mm:ss",
														System.Globalization.CultureInfo.InvariantCulture,
														System.Globalization.DateTimeStyles.None,
														out runDateStart))
								{
									row["Message"] = string.Format(DataQualityErrors.InvalidFormatError, "RunDateStart", "yyyy-MM-dd HH:mm:ss");
									row["Success"] = 0;
								}
							}

							if (model.RunDateEnd != null)
							{
								row["RunDateEnd"] = model.RunDateEnd;

								if (!DateTime.TryParseExact(model.RunDateEnd,
														"yyyy-MM-dd HH:mm:ss",
														System.Globalization.CultureInfo.InvariantCulture,
														System.Globalization.DateTimeStyles.None,
														out DateTime runDateEnd))
								{
									row["Message"] = string.Format(DataQualityErrors.InvalidFormatError, "RunDateEnd", "yyyy-MM-dd HH:mm:ss");
									row["Success"] = 0;
								}
								else if (model.RunDateStart != null && runDateStart > runDateEnd)
								{
									messages.Add(string.Format(DataQualityErrors.GreaterThanError, "RunDateStart", "RunDateEnd"));
									row["Success"] = 0;
								}
							}
							if ((!model.Uid.HasValue || model.Uid.Value == Guid.Empty) && (!model.OwningAssetUid.HasValue || model.OwningAssetUid.Value == Guid.Empty) && (!model.EvaluatedAssetUid.HasValue || model.EvaluatedAssetUid.Value == Guid.Empty))
							{
								messages.Add("At least one of the following MUST be provided: Uid, OwningAssetUid, EvaluatedAssetUid.");
								row["Success"] = 0;
							}

							row["Message"] = string.Join(";", messages.ToArray());


							table.Rows.Add(row);
						}
					}

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

					#region Bulk Copy

					using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
					{

						bulkCopy.BatchSize = SqlBulkBatchSize;
						bulkCopy.DestinationTableName = "api.ExecutionDeleteAssetResult";
						bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
						bulkCopy.ColumnMappings.Add("Uid", "Uid");
						bulkCopy.ColumnMappings.Add("OwningAssetUid", "OwningAssetUid");
						bulkCopy.ColumnMappings.Add("EvaluatedAssetUid", "EvaluatedAssetUid");
						bulkCopy.ColumnMappings.Add("EffectiveDateStart", "EffectiveDateStart");
						bulkCopy.ColumnMappings.Add("EffectiveDateEnd", "EffectiveDateEnd");
						bulkCopy.ColumnMappings.Add("RunDateStart", "RunDateStart");
						bulkCopy.ColumnMappings.Add("RunDateEnd", "RunDateEnd");
						bulkCopy.ColumnMappings.Add("Message", "Message");
						bulkCopy.ColumnMappings.Add("Success", "Success");

						bulkCopy.WriteToServer(table);
					}

					#endregion

					#region Log data errors 

					string checkSQL = $@"
								--check user permissions
								declare @IsAdministrator bit = 0
								select	@IsAdministrator = IsAdministrator
								from	reporting.Global_Resource
								where	ResourceID = @ResourceID;

								if @IsAdministrator = 0
								begin
									drop table if exists #temppremissiondel;
									
									select DAR.ExecutionID, DAR.ItemNumber
									into #temppremissiondel
									from    api.ExecutionDeleteAssetResult DAR
									inner join api.Execution E on E.ExecutionID = DAR.ExecutionID and E.ExecutionID=@ExecutionID
									left join AssetResult AR on DAR.Uid = AR.uid 
									left join Asset A on AR.OwningAssetUid = A.Uid									
									outer apply dbo.UserAssetPermissions(E.ResourceID, A.AssetTypeID) P 
									Where 
									P.PermissionsBitMask is null
									or 
									(
										P.AssetTypeID = A.AssetTypeID 
										and 
										(
											P.AssetID <> A.ID 
											and
											P.AssetID <> 0
										) 
										and 
										P.PermissionsBitMask & @p <> @p
									)
									group by DAR.ExecutionID, DAR.ItemNumber

									create clustered index IX_temppremissiondel on #temppremissiondel(ExecutionID,ItemNumber)

									update	DAR
									set		DAR.Success = 0,
											DAR.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to delete this result.'
									from    api.ExecutionDeleteAssetResult DAR                                                
									inner join api.Execution E on E.ExecutionID = DAR.ExecutionID and E.ExecutionID=@ExecutionID
									inner join #temppremissiondel S on S.ExecutionID = DAR.ExecutionID and S.ItemNumber = DAR.ItemNumber;
								end
																		
								-- check Owning Asset Uid
								update DAR
								set		Success = 0,
										[Message] = coalesce([Message] + '; ', '') + 'Invalid OwningAssetUid value'
								from api.[ExecutionDeleteAssetResult] DAR
									inner join api.Execution AE on AE.ExecutionID = DAR.ExecutionID
									left join asset a on a.uid = DAR.OwningAssetUid
									left Join assettype at on at.id = a.AssetTypeID
								where 
									DAR.ExecutionID = @ExecutionID 		
									and 
									DAR.OwningAssetUid is not null
									AND
									DAR.OwningAssetUid <> '00000000-0000-0000-0000-000000000000'
									AND
									(			                                			                             
										a.ID is null
										or
										(a.ID is not null and at.Class <> {(int)AssetTypeClass.Rule})
										or
										A.State = {(int)State.InActive}
									)

								-- check Evaluated Asset Uid
								update DAR
								set		Success = 0,
										[Message] = coalesce([Message] + '; ', '') + 'Invalid EvaluatedAssetUid value'
								from api.[ExecutionDeleteAssetResult] DAR
									inner join api.Execution AE on AE.ExecutionID = DAR.ExecutionID
									left join asset a on a.uid = DAR.EvaluatedAssetUid
									left Join assettype at on at.id = a.AssetTypeID
								where 		                               
									DAR.ExecutionID = @ExecutionID 		
									And
									DAR.EvaluatedAssetUid is not null
									AND
									DAR.EvaluatedAssetUid <> '00000000-0000-0000-0000-000000000000'
									and 
									(
										a.ID is null -- no match
										or
										(a.ID is not null and at.Class not in ({(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.BusinessAsset}))-- match but wrong asset type
										or
										A.State = {(int)State.InActive} -- inactive state
									)                                      

								";

					Connection.Execute(checkSQL, new { ResourceID = CurrentResourceID, execution.ExecutionID, p = Permission.DeleteAsset }, commandTimeout: timeout);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = import.Count();

					results = new List<DataQualityDeleteResponseModel>();
					results.AddRange(import.Select(i => new DataQualityDeleteResponseModel { Message = msg, Success = false }));
				}

				if (generalChecksCompleted)
				{
					List<ExecutionDeletedAssetResult> executionDeleteAssetResults = Query<ExecutionDeletedAssetResult>(
						$"select * from api.ExecutionDeleteAssetResult where ExecutionID = @ExecutionID and ItemNumber > {currentLocation.HighestItemNumberProcessed} order by ItemNumber asc", execution, timeout
						).ToList();

					List<AssetMeasureModel> assetMeasures = new List<AssetMeasureModel>();

					executionDeleteAssetResults.ForEach(dr =>
					{
						if (!dr.Success.HasValue)
						{
							List<string> wheres = new List<string>();

							if (dr.Uid != Guid.Empty)
							{
								wheres.Add("AR.Uid = @Uid");
							}
							else
							{
								if (dr.OwningAssetUid != Guid.Empty)
								{
									wheres.Add($@"AR.OwningAssetUid = @OwningAssetUid");
								}

								if (dr.EvaluatedAssetUid != Guid.Empty)
								{
									wheres.Add($@"AR.Uid in (select AR.Uid from AssetResult AR inner join Asset A on AR.EvaluatedAssetUid = A.Uid and AR.EvaluatedAssetUid = @EvaluatedAssetUid)");
								}

								if (dr.EffectiveDateStart.HasValue)
								{
									wheres.Add("AR.EffectiveDate >= @EffectiveDateStart");
								}

								if (dr.EffectiveDateEnd.HasValue)
								{
									wheres.Add("AR.EffectiveDate <= @EffectiveDateEnd");
								}

								if (dr.RunDateStart.HasValue)
								{
									wheres.Add("AR.RunDate >= @RunDateStart");
								}

								if (dr.RunDateEnd.HasValue)
								{
									wheres.Add("AR.RunDate <= @RunDateEnd");
								}
							}

							string ruleResultWhereClause = "from AssetResult AR ";

							if (wheres.Count > 0)
							{
								ruleResultWhereClause += "where " + string.Join(" and ", wheres);
							}

							string deleteAssetResultSQL = $@"
															create table #uids ([uid] uniqueidentifier);
															CREATE NONCLUSTERED INDEX IX_Tempuids_Uid ON #uids ( Uid ASC );
	
															insert into #uids (Uid)
																select  distinct AR.Uid {ruleResultWhereClause};															

															delete  T
															from    AssetResult T
																	inner join #uids S on S.Uid = T.Uid;

															update  T 
															set     T.Success = iif(C.[Count] = 0, 1, 0) 
															from    api.ExecutionDeleteAssetResult T 
																	inner join #uids S on 1=1
																	cross apply (
																		select  count(1) as [Count]
																		from    AssetResult
																		where   Uid = S.Uid
																	) C
															where   T.Success is null 
																	and T.ExecutionID = @ExecutionID 
																	and T.ItemNumber = @ItemNumber";

							// Find out which items we need to update scores for. Do this first!
							List<Guid> ruleResultUids = Query<Guid>($@"select distinct AR.Uid {ruleResultWhereClause}", dr, timeout).ToList();

							if (ruleResultUids.Count > 0)
							{
								assetMeasures.AddRange(GetAssetMeasuresFromRuleResults(ruleResultUids));
							}

							// Now perform the delete.
							try
							{
								Connection.Execute(deleteAssetResultSQL, dr, commandTimeout: timeout);
								dr.Success = true;
								dr.Message = "Successfully removed results in this range.";
							}
							catch (Exception ex)
							{
								dr.Success = false;
								dr.Message = ex.GetFullExceptionData(false);
							}

							results.Add(new DataQualityDeleteResponseModel { ExecutionItemUid = dr.ExecutionItemUid, Message = dr.Message, Success = dr.Success ?? false });
						}
					});

					// Now that results are deleted, send the score events to re-process scores for impacted assets.
					if (assetMeasures.Count > 0)
					{
						List<AssetMeasureModel> newAssetMeasures = new List<AssetMeasureModel>();

						// Filter through and see if there are any duplicates based on the possible dupe logic above.
						while (assetMeasures.Count > 0)
						{
							AssetMeasureModel existingAssetMeasure = assetMeasures.First();

							AssetMeasureModel newAssetMeasure = newAssetMeasures.FirstOrDefault(n => n.AssetUid == existingAssetMeasure.AssetUid && n.EffectiveDate == existingAssetMeasure.EffectiveDate);
							if (newAssetMeasure != null)
							{
								existingAssetMeasure.Measures.ForEach(e =>
								{
									if (!newAssetMeasure.Measures.Any(n => n.MetricAssetVersionUid == e.MetricAssetVersionUid))
									{
										newAssetMeasure.Measures.Add(e);
									}
								});
							}
							else
							{
								newAssetMeasures.Add(existingAssetMeasure.CloneThis());
							}

							assetMeasures.RemoveAt(0);
						}

						// Send to queue.
						CreateMeasureChangedResultExecution(newAssetMeasures, execution.ExecutionID);
					}
				}
			}

			return results;
		}		
		
		public List<AssetMeasureModel> GetAssetMeasuresFromRuleResults(List<Guid> ruleResultUids)
		{
			DataTable ruleResults = new DataTable();
			ruleResults.Columns.Add("RuleResultUid", typeof(Guid));

			foreach (Guid r in ruleResultUids.Distinct())
			{
				DataRow dr = ruleResults.NewRow();
				dr["RuleResultUid"] = r;
				ruleResults.Rows.Add(dr);
			}

			if (Database.Connection.State != ConnectionState.Open)
			{
				Connection.Open();
			}

			List<RuleResultChangedRawModel> rawMeasures;
			using (SqlTransaction trans = Connection.BeginTransaction())
			{
				Connection.Execute(@"create table #RuleResults (
					RuleResultUid uniqueidentifier not null,
					PRIMARY KEY NONCLUSTERED (RuleResultUid)
				)", transaction: trans);

				using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection, SqlBulkCopyOptions.Default, trans))
				{
					bulkCopy.BatchSize = 500;
					bulkCopy.DestinationTableName = "#RuleResults";
					bulkCopy.BulkCopyTimeout = 3600;

					bulkCopy.ColumnMappings.Add("RuleResultUid", "RuleResultUid");

					bulkCopy.WriteToServer(ruleResults);
				}

				rawMeasures = Connection
								.Query<RuleResultChangedRawModel>($@"
															create table #Items (
																AllocationUid uniqueidentifier, 
																MetricAssetUid uniqueidentifier, 
																MetricAssetVersionUid uniqueidentifier, 
																AssetUid uniqueidentifier, 
																EffectiveDate date
															);

															select	distinct
																	cast(Re.EffectiveDate as date) as EffectiveDate,
																	Oat.Uid as RuleAssetTypeUid,
																	Oa.AssetTypeId as RuleAssetTypeId,
																	Oa.Uid as RuleAssetUid,
																	Eat.Uid as EvaluatedAssetTypeUid,
																	Ea.AssetTypeId as EvaluatedAssetTypeId,
																	Ea.Uid as EvaluatedAssetUid,
																	I.IntersectTypeID,
																	I.ID as IntersectID
															into	#Results
															from	AssetResult Re
																	inner join #RuleResults Rr on Rr.RuleResultUid = Re.[Uid]
																	Inner Join Asset Ea on Ea.[Uid] = Re.EvaluatedAssetUid
																	inner join AssetType Eat on Ea.AssetTypeId = Eat.ID
																	inner join Asset Oa on Oa.[Uid] = Re.OwningAssetUid
																	inner join AssetType Oat on Oa.AssetTypeId = Oat.ID
																	inner join [Intersect] I on I.SubjectAssetID = OA.ID and I.ObjectAssetID = EA.ID
																	inner join IntersectType it on i.intersectTypeid = it.id 
																	inner join [Predicate] p on p.id = it.predicateid and p.[Type] = {(int)PredicateType.Evaluation}

															select	R.IntersectID,
																	R.EvaluatedAssetUid,
																	R.EvaluatedAssetTypeUid,
																	R.RuleAssetUid,
																	R.RuleAssetTypeUid,
																	L.RollupPathUid,
																	L.StartPosition as Position,
																	Ma.AllocationUid,
																	Mal.AssetTypeUid as AllocationAssetTypeUid,
																	Mv.Uid as MetricAssetVersionUid,
																	Mv.AssetUid as MetricAssetUid,
																	R.EffectiveDate
															into	#Raw
															from	#Results R
																	inner join metrics.RollupPathLink L on L.IntersectTypeID = R.IntersectTypeID
																	inner join metrics.AssetVersionRollupPath Mr on Mr.RollupPathUid = L.RollupPathUid
																	inner join metrics.AssetVersion Mv on Mv.Uid = Mr.AssetVersionUid
																		and (
																			(Mv.EffectiveDate <= R.EffectiveDate and Mv.EffectiveEndDate >= R.EffectiveDate)
																			or (Mv.EffectiveDate <= R.EffectiveDate and Mv.EffectiveEndDate is null)
																		)
																	inner join metrics.Asset Ma on Ma.Uid = Mv.AssetUid
																	inner join metrics.Allocation Mal on Mal.Uid = Ma.AllocationUid;

															with cte as (
																select	EvaluatedAssetUid as AssetUid,
																		EffectiveDate,
																		RollupPathUid,
																		Position,
																		AllocationUid,
																		MetricAssetUid,
																		MetricAssetVersionUid,
																		AllocationAssetTypeUid,
																		EvaluatedAssetTypeUid as AssetTypeUid
																from	#Raw
																where	EvaluatedAssetTypeUid <> AllocationAssetTypeUid
																		and RuleAssetTypeUid <> AllocationAssetTypeUid
																union all
																select	S.Uid as AssetUid,
																		cte.EffectiveDate,
																		L.RollupPathUid,
																		L.StartPosition as Position,
																		cte.AllocationUid,
																		cte.MetricAssetUid,
																		cte.MetricAssetVersionUid,
																		cte.AllocationAssetTypeUid,
																		ST.Uid as AssetTypeUid
																from	cte
																		inner join [metrics].[RollupPathLink] L on L.RollupPathUid = cte.RollupPathUid and L.EndPosition = cte.Position and L.StartPosition < cte.Position
																		inner join [Intersect] I on I.IntersectTypeID = L.IntersectTypeID
																		inner join Asset S on S.ID = I.SubjectAssetID 
																		inner join Asset O on O.ID = I.ObjectAssetID and O.Uid = cte.AssetUid
																		inner join AssetType ST on ST.ID = S.AssetTypeID
															)

															-- Start Path Asset Scoring
															insert into #Items
																select	AllocationUid, 
																		MetricAssetUid,
																		MetricAssetVersionUid,
																		AssetUid,
																		EffectiveDate
																from	cte 
																where	Position = 1;

															-- Rule Asset Scoring
															insert into #Items
																select	distinct
																		AllocationUid,
																		MetricAssetUid,
																		MetricAssetVersionUid,
																		RuleAssetUid as AssetUid,
																		EffectiveDate
																from	#Raw
																where	RuleAssetTypeUid = AllocationAssetTypeUid;

															-- Evaluated Asset Scoring
															insert into #Items
																select	distinct
																		AllocationUid,
																		MetricAssetUid,
																		MetricAssetVersionUid,
																		EvaluatedAssetUid as AssetUid,
																		EffectiveDate
																from	#Raw
																where	EvaluatedAssetTypeUid = AllocationAssetTypeUid;

															select * from #Items", transaction: trans, commandTimeout: timeout).ToList();
			}

			List<AssetMeasureModel> structuredMeasures = rawMeasures
				.GroupBy(m => new { m.AssetUid, m.EffectiveDate })
				.Select(m => new AssetMeasureModel
				{
					AssetUid = m.Key.AssetUid,
					EffectiveDate = m.Key.EffectiveDate,
					Measures = m.Select(o => new AssetMeasureChildModel
					{
						AllocationUid = o.AllocationUid,
						MetricAssetUid = o.MetricAssetUid,
						MetricAssetVersionUid = o.MetricAssetVersionUid
					}).ToList()
				}).ToList();

			return structuredMeasures;
		}

		public decimal? GetAssetScore(long assetId, ScoreType type)
		{
			string sql = $@"
							select      top 1
										cast(S.Value * 100 as decimal(18,1)) as 'Score'                            
							from        Asset A                            
										inner join metrics.Score S on S.AssetUid = A.[uid] and S.EffectiveDate <= getutcdate()
										inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = @type and (Al.OverrideName is null or Al.OverrideName = '')
							where       A.ID = @assetId 
							order by    S.EffectiveDate desc";

			return Query<decimal?>(sql, new { assetId, type = (int)type }).FirstOrDefault();
		}

		public List<AssetMeasureModel> GetDataQualityAssetEffectiveDateResultModels(DataQualityMeasureQueryModel query, Guid allocationUid, Guid metricAssetUid, Guid metricAssetVersionUid, DateTime measureEffectiveDate)
		{
			DynamicParameters args = new DynamicParameters();
			args.Add("@AssetVersionEffectiveDate", measureEffectiveDate, DbType.Date);
			foreach (SqlParameter p in query.Filters.Where(p => p.Parameter != null).Select(p => p.Parameter))
			{
				args.Add(p.ParameterName, p.Value, p.DbType);
			}

			Connection.OpenIfClosed().Wait();

			List<AssetMeasureModel> list = Connection.Query<AssetMeasureModel>(query.Sql, args, commandTimeout: 600)
				.ToList()
				.Select(o => new AssetMeasureModel
				{
					AssetUid = o.AssetUid,
					EffectiveDate = o.EffectiveDate,
					Measures = new List<AssetMeasureChildModel> {
						new AssetMeasureChildModel {
							AllocationUid = allocationUid,
							MetricAssetUid = metricAssetUid,
							MetricAssetVersionUid = metricAssetVersionUid,
							Result = false
						}
					}
				})
				.ToList();

			return list;
		}
		
		public List<DataQualityMeasureQueryResultModel> GetDataQualityMeasureQueryResultModels(DataQualityMeasureQueryModel query, Guid assetUid, DateTime? maxDate)
		{
			DynamicParameters args = new DynamicParameters();
			args.Add("@AssetUid", assetUid, DbType.Guid);
			args.Add("@MaximumEffectiveDate", maxDate ?? DateTime.UtcNow, DbType.Date);
			foreach (SqlParameter p in query.Filters.Where(p => p.Parameter != null).Select(p => p.Parameter))
			{
				args.Add(p.ParameterName, p.Value, p.DbType);
			}

			Connection.OpenIfClosed().Wait();

			List<DataQualityMeasureQueryResultModel> list = Connection.Query<DataQualityMeasureQueryResultModel>(query.Sql, args, commandTimeout: 600).ToList();

			return list;
		}	
		
		public List<Guid> GetImpactedMeasureVersionsBy(MetricGovernanceCheckType check, int typeId)
		{
			string sql = "";
			switch (check)
			{
				case MetricGovernanceCheckType.Field:
					sql = @"
							select	V.Uid
							from	metrics.AssetVersion V
									inner join metrics.Asset A on A.Uid = V.AssetUid and V.Definition is not null 
									inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 1
									inner join AssetType T on T.Uid = Al.AssetTypeUid
									inner join FieldType FT on FT.Name = JSON_VALUE(V.Definition, '$.Governance.Field.FieldTypeName') and FT.AssetTypeID = T.ID and FT.ID = @typeId";
					break;
				case MetricGovernanceCheckType.Owner:
					sql = @"
							select	V.Uid
							from	metrics.AssetVersion V
									inner join metrics.Asset A on A.Uid = V.AssetUid and V.Definition is not null 
									inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 1
									inner join AssetType T on T.Uid = Al.AssetTypeUid
									inner join ResponsibilityTypeRelation RA on RA.ObjectType = T.Object and RA.ObjectID = T.ObjectID
									inner join ResponsibilityType RT on RT.Uid = JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') and RT.ID = RA.ResponsibilityTypeID and RT.ID = @typeId";
					break;
				case MetricGovernanceCheckType.Predicate:
					sql = @"
							select	V.Uid
							from	metrics.AssetVersion V
									inner join metrics.Asset A on A.Uid = V.AssetUid and V.Definition is not null 
									inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 1
									inner join AssetType T on T.Uid = Al.AssetTypeUid
									inner join IntersectType IA on ( IA.SubjectAssetTypeID = T.ID or IA.ObjectAssetTypeID = T.ID ) 
									inner join [Predicate] P on P.Uid = JSON_VALUE(V.Definition, '$.Governance.Predicate.PredicateUid') and P.ID = IA.PredicateID and P.ID = @typeId";
					break;
				case MetricGovernanceCheckType.Relation:
					sql = @"
							select	V.Uid
							from	metrics.AssetVersion V
									inner join metrics.Asset A on A.Uid = V.AssetUid and V.Definition is not null 
									inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 1
									inner join AssetType T on T.Uid = Al.AssetTypeUid
									inner join IntersectType IA on ( IA.SubjectAssetTypeID = T.ID or IA.ObjectAssetTypeID = T.ID ) 
										and IA.Uid = JSON_VALUE(V.Definition, '$.Governance.Relation.IntersectTypeUid') and IA.ID = @typeId";
					break;
			}
			List<Guid> list = null;

			if (!string.IsNullOrEmpty(sql))
			{
				list = Query<Guid>(sql, new { typeId }).ToList();
			}

			return list;
		}		
		
		public List<AssetMeasureModel> GetMeasureModelsBasedOnResponsibilityAllocation(AssetType assetType, ResponsibilityType responsibility)
		{
			DateTime today = DateTime.UtcNow.Date;
			IEnumerable<ResponsibilityAssetMeasureProcessedResult> measureResults = 
				Query<ResponsibilityAssetMeasureProcessedResult>(@"
																select  A.Uid as AssetUid, 
																		M.AllocationUid,
																		M.Uid as MetricAssetUid,
																		V.Uid as MetricAssetVersionUid
																from    ResponsibilityDetail O 
																		inner join Asset A on ((A.ID = O.AssetID) or O.AssetID = 0 and O.AssetTypeID = A.AssetTypeID) and O.ResponsibilityTypeID = @ResponsibilityTypeID
																		inner join AssetType T on T.ID = A.AssetTypeID and T.ID = @ID
																		inner join metrics.Allocation Al on Al.AssetTypeUid = T.Uid and Al.ScoreType = 1 and Al.IsExternallyCalculated = 0 
																		inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
																		inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
																			and ( 
																				(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
																				(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
																				) 
																			and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
																			and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = @ResponsibilityTypeUid
																			and V.Definition <> '{}' 
																group by A.Uid, M.AllocationUid, M.Uid, V.Uid", new { assetType.ID, ResponsibilityTypeUid = responsibility.UID, ResponsibilityTypeID = responsibility.ID, today });

			return measureResults.GroupBy(m => new { m.AssetUid })
				.Select(m => new AssetMeasureModel
				{
					AssetUid = m.Key.AssetUid,
					EffectiveDate = today,
					Measures = m.Select(o => new AssetMeasureChildModel
					{
						AllocationUid = o.AllocationUid,
						MetricAssetUid = o.MetricAssetUid,
						MetricAssetVersionUid = o.MetricAssetVersionUid
					}).Distinct().ToList()
				}).ToList();
		}		
		
		public ObjectStatisticTileModel GetObjectStatistics(string type, int id)
		{
			ObjectStatisticTileModel model = new ObjectStatisticTileModel { Items = new List<ObjectStatisticTileItemModel>() };

			List<RawObjectStatistic> list = Database.Connection.Query<RawObjectStatistic>("[dbo].[GetObjectStatistics] @type, @id", new { type = new DbString { Value = type.ToString(), IsAnsi = true }, id }).ToList();

			list.ForEach(i =>
			{
				switch (i.Group)
				{
					case "Comments":
						model.CommentCount = i.Value.GetValueOrDefault();
						model.CommentLast = i.MostRecent;
						break;
					case "Followers":
						model.FollowerCount = i.Value.GetValueOrDefault();
						break;
					case "Score":
						model.Score = i.Value;
						model.ScoreLast = i.MostRecent;
						break;
					case "Issues":
						model.IssueCount = i.Value.GetValueOrDefault();
						model.IssueLast = i.MostRecent;
						break;
					default:
						string name = "";

						if (PluralCultureHelper.IsNeutralCultureEnglish())
						{
#if RUNNING_ON_STANDARD
							name = PluralizeService.Core.PluralizationProvider.Pluralize(i.Name ?? "");
#endif

#if RUNNING_ON_NET48
							var namePluralizationInstance = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
							name = namePluralizationInstance.Pluralize(i.Name ?? "");
#endif
						}

						model.Items.Add(new ObjectStatisticTileItemModel { Count = i.Value.GetValueOrDefault(), Name = name, TypeID = i.TypeID });
						break;
				}
			});

			return model;
		}

		public decimal? GetPreviousAssetScore(long assetId, ScoreType type)
		{
			string sql = $@"
							select      top 1
										cast(S.Value * 100 as decimal(18,1)) as 'Score'                            
							from        Asset A                            
										inner join metrics.Score S on S.AssetUid = A.[uid] and S.EffectiveDate <= getutcdate()
										inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = @type and (Al.OverrideName is null or Al.OverrideName = '')
										cross apply (
											select top 1 EffectiveDate from Asset AP
											inner join metrics.Score SA on SA.AssetUid = AP.[uid] and SA.EffectiveDate <= getutcdate()
											inner join metrics.Allocation ALP on ALP.Uid = SA.AllocationUid and ALP.ScoreType = @type and (ALP.OverrideName is null or ALP.OverrideName = '')
											where AP.ID = @assetId
										) P
							where       A.ID = @assetId and S.EffectiveDate < P.EffectiveDate
							order by    S.EffectiveDate desc";

			return Query<decimal?>(sql, new { assetId, type = (int)type }).FirstOrDefault();
		}

		public List<DataQualityResponseModel> UpsertAssetResults(List<IDataQualityUpsert> import, ApiExecution execution, int timeout = 3600, bool sendWorkflowEvents = true)
		{
			List<DataQualityResponseModel> results = new List<DataQualityResponseModel>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var dupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			if (dupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DataQualityResponseModel { ExecutionItemUid = i.ExecutionItemUid.Value, Message = execution.ErrorMessage, Success = false }));
			}
			else
			{
				try
				{
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAssetResult");

					if (currentLocation.HighestItemNumberProcessed > 0)
					{
						results.AddRange(
							Query<DataQualityResponseModel>(
								$"select ItemNumber, Uid, ExecutionItemUid, Success, Message from api.ExecutionAssetResult where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
								new { execution.ExecutionID }
							)
						);
					}

					#region Build data tables.

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));
					table.Columns.Add("EvaluatedAssetUid", typeof(Guid));
					table.Columns.Add("OwningAssetUid", typeof(Guid));
					table.Columns.Add("Uid", typeof(Guid));
					table.Columns.Add("EffectiveDate", typeof(string));
					table.Columns.Add("RunDate", typeof(string));
					table.Columns.Add("PassCount", typeof(long));
					table.Columns.Add("FailCount", typeof(long));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));

					#endregion

					#region Generate data sets

					for (int i = 1; i <= import.Count; i++)
					{
						if (i > currentLocation.HighestItemNumber)
						{
							IDataQualityUpsert model = import[i - 1];

							DataRow row = table.NewRow();

							row["ExecutionID"] = execution.ExecutionID;
							row["ExecutionItemUid"] = model.ExecutionItemUid ?? Guid.NewGuid();
							row["ItemNumber"] = i;

							if (model.RunDate != null)
							{
								row["RunDate"] = model.RunDate;

								if (!DateTime.TryParseExact(model.RunDate,
														"yyyy-MM-dd HH:mm:ss",
														System.Globalization.CultureInfo.InvariantCulture,
														System.Globalization.DateTimeStyles.None,
														out DateTime rundate))
								{
									row["Message"] = string.Format(DataQualityErrors.InvalidFormatError, "RunDate", "yyyy-MM-dd HH:mm:ss");
									row["Success"] = 0;
								}
								else
								{
									if (rundate > DateTime.Now)
									{
										row["Message"] = string.Format(DataQualityErrors.GreaterThanTodayError, "RunDate");
										row["Success"] = 0;
									}
									else if (rundate == DateTime.MinValue)
									{
										row["Message"] = string.Format(DataQualityErrors.GenericInvalidFieldValueError, model.RunDate, "RunDate");
										row["Success"] = 0;
									}
								}
							}

							if (model is DataQualityInsertModel dataQualityInsertModel)
							{
								row["OwningAssetUid"] = dataQualityInsertModel.OwningAssetUid;

								if (dataQualityInsertModel.EffectiveDate != null)
								{
									row["EffectiveDate"] = dataQualityInsertModel.EffectiveDate;

									if (!DateTime.TryParseExact(dataQualityInsertModel.EffectiveDate,
															"yyyy-MM-dd",
															System.Globalization.CultureInfo.InvariantCulture,
															System.Globalization.DateTimeStyles.None,
															out DateTime effectiveDate))
									{
										row["Message"] = string.Format(DataQualityErrors.InvalidFormatError, "EffectiveDate", "yyyy-MM-dd");
										row["Success"] = 0;
									}
									else if (effectiveDate == DateTime.MinValue)
									{
										row["Message"] = string.Format(DataQualityErrors.GenericInvalidFieldValueError, dataQualityInsertModel.EffectiveDate, "EffectiveDate");
										row["Success"] = 0;
									}
									else if (effectiveDate > DateTime.Now)
									{
										row["Message"] = string.Format(DataQualityErrors.GreaterThanTodayError, "EffectiveDate");
										row["Success"] = 0;
									}
								}
								else
								{
									row["Message"] = string.Format(DataQualityErrors.RequiredFieldError, "EffectiveDate");
									row["Success"] = 0;
								}

								if (model.RunDate == null)
								{
									row["Message"] = string.Format(DataQualityErrors.RequiredFieldError, "RunDate");
									row["Success"] = 0;
								}

								if (!model.PassCount.HasValue)
								{
									row["Message"] = string.Format(DataQualityErrors.RequiredFieldError, "PassCount");
									row["Success"] = 0;
								}

								if (!model.FailCount.HasValue)
								{
									row["Message"] = string.Format(DataQualityErrors.RequiredFieldError, "FailCount");
									row["Success"] = 0;
								}
							}

							if (model is DataQualityUpdateModel dataQualityUpdateModel)
							{
								row["Uid"] = dataQualityUpdateModel.Uid;

								if (!model.EvaluatedAssetUid.HasValue && model.RunDate == null && !model.PassCount.HasValue && !model.FailCount.HasValue)
								{
									row["Message"] = DataQualityErrors.InvalidUpdateError;
									row["Success"] = 0;
								}
							}

							if (model.EvaluatedAssetUid.HasValue)
							{
								row["EvaluatedAssetUid"] = model.EvaluatedAssetUid.Value;
							}
							else
							{
								row["EvaluatedAssetUid"] = DBNull.Value;
							}
							if (model.PassCount.HasValue)
							{
								row["PassCount"] = model.PassCount.Value;
							}
							else
							{
								row["PassCount"] = DBNull.Value;
							}

							if (model.FailCount.HasValue)
							{
								row["FailCount"] = model.FailCount.Value;
							}
							else
							{
								row["FailCount"] = DBNull.Value;
							}

							if (model.PassCount.HasValue && (model.PassCount < 0 || model.PassCount > 9223372036854775807))
							{
								row["Message"] = string.Format(DataQualityErrors.ValueBetweenError, "PassCount", 0, 9223372036854775807);
								row["Success"] = 0;
							}

							if (model.FailCount.HasValue && (model.FailCount < 0 || model.FailCount > 9223372036854775807))
							{
								row["Message"] = string.Format(DataQualityErrors.ValueBetweenError, "FailCount", 0, 9223372036854775807);
								row["Success"] = 0;
							}

							if (model.PassCount.HasValue && model.FailCount.HasValue)
							{
								ulong total = (ulong)model.PassCount.Value + (ulong)model.FailCount.Value;

								if (total > 9223372036854775807)
								{
									row["Message"] = string.Format(DataQualityErrors.GreaterThanError, "PassCount + FailCount", "9223372036854775807", 0);
									row["Success"] = 0;
								}

							}

							table.Rows.Add(row);
						}
					}

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

					#region Bulk Copy

					using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
					{

						bulkCopy.BatchSize = SqlBulkBatchSize;
						bulkCopy.DestinationTableName = "api.ExecutionAssetResult";
						bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
						bulkCopy.ColumnMappings.Add("OwningAssetUid", "OwningAssetUid");
						bulkCopy.ColumnMappings.Add("EvaluatedAssetUid", "EvaluatedAssetUid");
						bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
						bulkCopy.ColumnMappings.Add("RunDate", "RunDate");
						bulkCopy.ColumnMappings.Add("PassCount", "PassCount");
						bulkCopy.ColumnMappings.Add("FailCount", "FailCount");
						bulkCopy.ColumnMappings.Add("Message", "Message");
						bulkCopy.ColumnMappings.Add("Success", "Success");
						bulkCopy.ColumnMappings.Add("Uid", "Uid");

						bulkCopy.WriteToServer(table);
					}

					#endregion

					#region Log data errors

					string checkSQL = $@"
								--check user permissions
								declare @IsAdministrator bit = 0
								select	@IsAdministrator = IsAdministrator
								from	reporting.Global_Resource
								where	ResourceID = @ResourceID

								if @IsAdministrator = 0
								begin
									-- check on insert
									drop table if exists #temppremissionpost;

									select EAR.ExecutionID, EAR.ItemNumber
									into #temppremissionpost
									from    api.ExecutionAssetResult EAR
											inner join api.Execution E on E.ExecutionID = EAR.ExecutionID 
																			and E.ExecutionID = @executionID and EAR.Success is null and UPPER(E.Method)='POST'
											inner join 
											Asset A on (EAR.OwningAssetUid = A.uid) 
											and EAR.OwningAssetUid is not null												
											outer apply dbo.UserAssetPermissions(E.ResourceID, A.AssetTypeID) P 
											Where 
											P.PermissionsBitMask is null
											or 
											(
												P.AssetTypeID = A.AssetTypeID 
												and 
												(
													P.AssetID <> A.ID 
													and
													P.AssetID <> 0
												) 
												and 
												P.PermissionsBitMask & @p <> @p
											)
									group by EAR.ExecutionID, EAR.ItemNumber;

									create clustered index IX_temppremissionpost on #temppremissionpost(ExecutionID,ItemNumber)

									update	EAR
									set		EAR.Success = 0,
											EAR.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to create this result.'
									from    api.ExecutionAssetResult EAR
											inner join api.Execution E on E.ExecutionID = EAR.ExecutionID 
																			and E.ExecutionID = @executionID and EAR.Success is null and UPPER(E.Method)='POST'
											inner join #temppremissionpost S on S.ExecutionID = EAR.ExecutionID and S.ItemNumber = EAR.ItemNumber;
										
									-- Check on update
									drop table if exists #temppremissionput;

									select EAR.ExecutionID, EAR.ItemNumber
									into #temppremissionput
									from  api.ExecutionAssetResult EAR                                                
									inner join api.Execution E on E.ExecutionID = EAR.ExecutionID and E.ExecutionID=@ExecutionID and EAR.Success is null and UPPER(E.Method)='PUT'
									inner join AssetResult AR on AR.uid =EAR.Uid
									inner join Asset A on A.uid = AR.OwningAssetUid
									outer apply dbo.UserAssetPermissions(E.ResourceID, A.AssetTypeID) P 
									Where 
									P.PermissionsBitMask is null
									or 
									(
										P.AssetTypeID = A.AssetTypeID 
										and 
										(
											P.AssetID <> A.ID 
											and
											P.AssetID <> 0
										) 
										and 
										P.PermissionsBitMask & @p <> @p
									)
									group by EAR.ExecutionID, EAR.ItemNumber;

									create clustered index IX_temppremissionput on #temppremissionput(ExecutionID,ItemNumber)

									update	EAR
									set		EAR.Success = 0,
											EAR.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to update this result.'
									from    api.ExecutionAssetResult EAR                                                
									inner join api.Execution E on E.ExecutionID = EAR.ExecutionID and E.ExecutionID=@ExecutionID and EAR.Success is null and UPPER(E.Method)='PUT'
									inner join #temppremissionput S on S.ExecutionID = EAR.ExecutionID and S.ItemNumber = EAR.ItemNumber;
								end

								-- check Uid on Put
								update EAR
								set		Success = 0,
										[Message] = coalesce([Message] + '; ', '') + 'Invalid Rule Result UID value'
								from api.[ExecutionAssetResult] EAR
									inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID
									left join AssetResult AR on AR.Uid = EAR.Uid
								where 
									AE.Method = 'PUT'
									and EAR.ExecutionID = @ExecutionID 		
									and 
									(EAR.Uid is null or EAR.Uid = '00000000-0000-0000-0000-000000000000' or AR.Uid is null)                                        

								-- check Owning Asset Uid
								update EAR
								set		Success = 0,
										[Message] = coalesce([Message] + '; ', '') + 'Invalid OwningAssetUid value'
								from api.[ExecutionAssetResult] EAR
									inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID
									left join asset a on a.uid = EAR.OwningAssetUid
									left Join assettype at on at.id = a.AssetTypeID
								where 
									AE.Method = 'POST'
									AND
									EAR.ExecutionID = @ExecutionID 		
									and 
									(
										(EAR.OwningAssetUid is null or EAR.OwningAssetUid = '00000000-0000-0000-0000-000000000000')
										or
										(EAR.OwningAssetUid is not null and a.ID is null)
										or
										(EAR.OwningAssetUid is not null and a.ID is not null and at.Class <> {(int)AssetTypeClass.Rule})
										or
										A.State = {(int)State.InActive}
									)

								-- check Evaluated Asset Uid
								update EAR
								set		Success = 0,
										[Message] = coalesce([Message] + '; ', '') + 'Invalid EvaluatedAssetUid value'
								from api.[ExecutionAssetResult] EAR
									inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID
									left join asset a on a.uid = EAR.EvaluatedAssetUid
									left Join assettype at on at.id = a.AssetTypeID
								where 		                               
									EAR.ExecutionID = @ExecutionID 		
									And
									EAR.EvaluatedAssetUid is not null
									and 
									(
										a.ID is null -- no match
										or
										(a.ID is not null and at.Class not in ({(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.BusinessAsset}))-- match but wrong asset type
										or
										A.State = {(int)State.InActive} -- inactive state
									)	                                    

								-- check PassCount/FailCount on Put
								update EAR
								set		Success = 0,
										[Message] = coalesce([Message] + '; ', '') + '{string.Format(DataQualityErrors.GreaterThanError, "PassCount + FailCount", "9223372036854775807", 0)}'
								from api.[ExecutionAssetResult] EAR
									inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID 
									left join AssetResult AR on AR.Uid = EAR.Uid
								where 
									AE.Method = 'PUT'
									and EAR.ExecutionID = @ExecutionID
									and success is null
									and (
									CASE
										WHEN EAR.FailCount is not null and EAR.PassCount is null and (9223372036854775807 - AR.PassCount - EAR.FailCount)<0 THEN 1
										WHEN EAR.PassCount is not null and EAR.FailCount is null and (9223372036854775807 - AR.FailCount - EAR.PassCount)<0 THEN 1
										WHEN EAR.PassCount is not null and EAR.FailCount is not null and (9223372036854775807 - EAR.Passcount - EAR.FailCount)<0 THEN 1
										ELSE 0
									END)=1";

					Connection.Execute(checkSQL, new { ResourceID = CurrentResourceID, execution.ExecutionID, p = Permission.ModifyAsset }, commandTimeout: timeout);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = import.Count();

					results = new List<DataQualityResponseModel>();
					results.AddRange(import.Select(i => new DataQualityResponseModel { Message = msg, Success = false }));
				}

				if (generalChecksCompleted)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

					string assetResultSQL = $@"create table #ObjectMergeTableAssetResult (Uid uniqueidentifier, ItemNumber int, [Operation] varchar(10));
											CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableAssetResult ON #ObjectMergeTableAssetResult ( ItemNumber ASC );

											Merge into AssetResult AR
											using (
													select  ItemNumber, 
															UID,
															OwningAssetUid,
															EvaluatedAssetUid,															
															EffectiveDate,
															RunDate,
															PassCount,
															FailCount			                                                    
													from    api.[ExecutionAssetResult]
													where   ExecutionID = @ExecutionID
															and Success is null
															and ItemNumber between @beginItemNumber and @endItemNumber
													) S
											ON S.UID = AR.UID
											WHEN NOT MATCHED THEN
											INSERT ([Uid]
													,[OwningAssetUid]
													,[EvaluatedAssetUid]
														,[EffectiveDate]
														,[RunDate]
														,[PassCount]
														,[FailCount]
														,[CreatedOn]
														,[CreatedBy]
														,[UpdatedOn]
														,[UpdatedBy])
													VALUES
														(NEWID()
														,S.[OwningAssetUid]
														,S.[EvaluatedAssetUid]
														,S.EffectiveDate
														,S.RunDate
														,S.PassCount
														,S.FailCount
														,@requestDate
														,@userId
														,@requestDate
														,@userId)	                                                
											WHEN MATCHED THEN
												UPDATE 
												SET RunDate = (case when S.RunDate is null then AR.RunDate else S.RunDate end),
												PassCount = (case when S.PassCount is null then AR.PassCount else S.PassCount end),
												FailCount = (case when S.FailCount is null then AR.FailCount else S.FailCount end),												
												EvaluatedAssetUid = (case when S.EvaluatedAssetUid is null then AR.EvaluatedAssetUid else S.EvaluatedAssetUid end),
												UpdatedOn = @requestDate,
												UpdatedBy = @userId
											output inserted.Uid, S.ItemNumber, $action into #ObjectMergeTableAssetResult;

												--Update Exection record with new Uid
												Update EAR
												set Uid = MTR.Uid
												from 
													api.ExecutionAssetResult EAR 
													inner join 
													#ObjectMergeTableAssetResult MTR on EAR.ItemNumber=MTR.ItemNumber and EAR.ExecutionID=@ExecutionID                                                                                                         
													

												Update EAR
												set EAR.success = 1 
												FROM 
												api.ExecutionAssetResult EAR
												inner join 
												#ObjectMergeTableAssetResult MTR on MTR.Uid = EAR.Uid and EAR.ExecutionID = @ExecutionID";

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;
						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								try
								{
									Connection.Execute(assetResultSQL, new { execution.ExecutionID, beginItemNumber, endItemNumber, userId = CurrentResourceID, requestDate = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);
									trans.Commit();
									runCompleted = true;
								}
								catch (Exception ex)
								{
									try
									{
										if (trans != null)
										{
											trans.Rollback();
										}
									}
									catch
									{
										// Continue through loops, do not kill entire process.
									}

									retryCount++;

									if (retryCount > API_V2_RETRY_LIMIT)
									{
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAssetResult", ex.GetFullExceptionData(false), timeout);
									}
								}
							}
						}

						results.AddRange(
								Query<DataQualityResponseModel>(
									$"select ItemNumber, Uid, ExecutionItemUid, Success, Message from api.ExecutionAssetResult where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
									new { execution.ExecutionID, beginItemNumber, endItemNumber }
								)
							);

						beginItemNumber += loopSize;
						endItemNumber += loopSize;
					}
				}
			}

			completeApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionAssetResult");

			#region Scoring

			List<Guid> ruleResultUids = results.Where(i => i.Success).Select(i => i.Uid.Value).ToList();
			if (ruleResultUids.Count > 0)
			{
				List<AssetMeasureModel> assetMeasures = GetAssetMeasuresFromRuleResults(ruleResultUids);
				CreateMeasureChangedResultExecution(assetMeasures);
			}

			#endregion Scoring

			return results;
		}	
		
		#endregion
	}

	internal class MetricHierarchyBuilder
	{
		public void BuildMetricHierarchy(List<MetricAssetTypeHierarchyModel> results, MetricAssetTypeHierarchyModels model, MetricAssetTypeHierarchyModel p, MetricAssetTypeHierarchyModel i)
		{
			if (!string.IsNullOrEmpty(i.ConditionsJson))
			{
				i.Conditions = JsonConvert.DeserializeObject<List<MetricConditionHierarchyModel>>(i.ConditionsJson);
			}

			// Recurse.
			foreach (MetricAssetTypeHierarchyModel c in results.Where(o => o.ParentUid == i.Uid))
			{
				BuildMetricHierarchy(results, model, i, c);
			}

			if (p != null)
			{
				if (p.Metrics == null)
				{
					p.Metrics = new List<MetricAssetTypeHierarchyModel>();
				}

				p.Metrics.Add(i);
			}
			else
			{
				model.Add(i);
			}
		}
	}
}
