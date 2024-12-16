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

		DbSet<ExternalMeasureResult> ExternalMeasureResults { get; set; }

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

		#endregion

		#region Methods

		List<ExternalScoreResultApiResponseModel> BulkExternalResultsImport(List<ExternalScoreResultApiRequestModel> model, ApiExecution execution, MetricAllocation allocation);
		
		List<ExternalScoreResultApiResponseModel> BulkExternalResultsImport(List<ExternalScoreResultApiRequestModel> model, ApiExecution execution, ScoreType scoreType);
		
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
		/// A Score Engine method that is called when relationships are added to Govern.
		/// </summary>
		void CreateImportRelationshipsExecution(Guid apiExecutionUid, int intersectTypeId, int timeout);

		/// <summary>
		/// A Score Engine method that is called when a measure is updated in Govern, and a notification is sent to the Score Engine to determine what needs to be recalculated.
		/// </summary>
		void CreateMeasureChangedNotificationExecution(MetricAssetVersion version, DateTime effectiveDate, Guid? triggeredByMeasureUid = null);

		/// <summary>
		/// A Score Engine method that is called when a parent asset is (un)assigned a child.
		/// </summary>
		void CreateParentAssetGovernanceRescoreExecution(Guid apiExecutionUid);

		/// <summary>
		/// This function takes a list of assets (using their Uids) and a type of score and submits a request to the Scoring Engine to reprocess the score for the current date.
		/// </summary>
		/// <param name="assets">A list of asset Uids to submit to the queue for rescoring.</param>
		/// <param name="scoreType">The type of score to recalculate.</param>
		void CreateRescoreRequests(List<Guid> assets, ScoreType scoreType);

		/// <summary>
		/// This function takes a list of re-processed responsibility rules (using their IDs), gathers up the assets, and submits a request to the Scoring Engine to reprocess the score for the current date.
		/// </summary>
		/// <param name="ruleIds">List of responsibility rules, absed on their Id</param>
		void CreateRescoreRequestsBasedOnResponsibilityRulesRun(List<int> ruleIds);

		/// <summary>
		/// A Score Engine method that is called when an asset type or intersect type are added or removed from Govern.
		/// </summary>
		void CreateRollupPathChangedExecution(int? intersectTypeId = null, int? assetTypeId = null, Guid? triggeredByApiExecutionUid = null);

		List<DataQualityDeleteResponseModel> DeleteAssetResults(List<DataQualityDeleteModel> request, ApiExecution execution, int timeout = 3600);
		
		List<Guid> GetImpactedAssetsFromRuleResults(List<Guid> ruleResultUids);
		
		decimal? GetAssetScore(long assetId, ScoreType type);

		/// <summary>
		/// Gets impacted asset/measures that require rescoring based on this responsibility type allocation.
		/// </summary>
		/// <param name="assetType">The asset type.</param>
		/// <param name="responsibility">The responsibility type.</param>
		/// <returns>A list of AssetMeasureModel items to send to the scoring engine.</returns>
		List<Guid> GetScoreImpactedAssetsBasedOnResponsibilityAllocation(AssetType assetType, ResponsibilityType responsibility);

		ObjectStatisticTileModel GetObjectStatistics(string type, int id);
		
		decimal? GetPreviousAssetScore(long assetId, ScoreType type);
		
		List<DataQualityResponseModel> UpsertAssetResults(List<IDataQualityUpsert> request, ApiExecution execution, int timeout = 3600, bool sendWorkflowEvents = true);

		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		#region DbSets

		public DbSet<ExternalMeasureResult> ExternalMeasureResults { get; set; }

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

		#endregion

		#region Utility
		
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
				UpdateExecutionWithErrorFromException(execution, ex);
			}

			Connection.Close();

			return results;
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
		
		private void sendScoreQueueMessage(ScoreQueueChangeType changeType, int? executionId = null, object payload = null)
		{
			ScoreQueueInfo info = new ScoreQueueInfo
			{
				CompanyID = SecurityContext.CompanyID,
				ResourceID = SecurityContext.ResourceID,
				ChangeType = changeType,
				ExecutionId = executionId,
				StartedOn = DateTime.UtcNow,
				Payload = (payload != null ? JsonConvert.SerializeObject(payload) : "{}")
			};
			QueueSource.CreateMessage(constants.Queue.Score, info);
		}

		#endregion

		#region Methods
		
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

		public void CreateCheckDependencyRemovedResultExecution(List<Guid> versionUids)
		{
			string sql = @"
select	distinct
		S.AssetUid
from	metrics.AssetVersion V
		inner join metrics.ScoreItem I on  I.AssetVersionUid = V.Uid
		inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid 
		inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
		inner join metrics.ScoreItemLink L2 on L2.ScoreUid = S.Uid and L2.ScoreItemUid <> I.Uid
		inner join metrics.ScoreItem I2 on I2.Uid = L2.ScoreItemUid
		inner join metrics.AssetVersion V2 on V2.Uid = I2.AssetVersionUid and V2.State = 1
		inner join metrics.Asset A2 on A2.State = 1 and A2.Uid = V2.AssetUid and A2.IsGroup = 0
where   V.Uid in @versionUids";

			Connection.OpenIfClosed().Wait();
			var results = Connection.Query<Guid>(sql, new { versionUids }).ToList();
			if (results.Count > 0)
			{
				CreateRescoreRequests(results, ScoreType.Governance);
			}
		}

		public void CreateExternalScoreWorkflowCheckExecution(Guid apiExecutionUid)
		{
			string sql = @"
	select  t.Object as ObjectType,
			t.ObjectID as ObjectTypeID
			a.Object,
			a.ObjectID
	from    api.ExecutionScore s
			inner join Asset a on a.uid = s.AssetUid and s.ExecutionID = @apiExecutionUid and s.Success = 1
			inner join AssetType t on t.Id = a.AssetTypeId
	where	exists (select 1 from workflow.EventRegistration where AssetTypeID = t.Id and ChangeType = 5)";

			var items = Query<WorkflowScoredAsset>(sql, new { apiExecutionUid }).ToList();
			if (items.Count > 0)
			{
				var groupedItems = items.GroupBy(i => new { i.ObjectType, i.ObjectTypeID }).ToList();
				groupedItems.ForEach(g => {
					SendWorkflowEvents(g.Key.ObjectType, g.Key.ObjectTypeID, g.ToList(), core.enums.Workflow.ChangeType.ScoreUpdate);
				});
			}
		}

		public void CreateImportAssetsExecution(Guid apiExecutionUid, Guid assetTypeUid)
		{
			if (Any<MetricAllocation>(i => i.AssetTypeUid == assetTypeUid && i.ScoreType == ScoreType.Governance && !i.IsExternallyCalculated))
			{
				string sql = @"select Uid from api.ExecutionAsset where ExecutionID = @apiExecutionUid and Success = 1";
				var assetUids = Query<Guid>(sql, new { apiExecutionUid }).ToList();
				CreateRescoreRequests(assetUids, ScoreType.Governance);
			}
		}

		public void CreateImportRelationshipsExecution(Guid apiExecutionUid, int intersectTypeId, int timeout)
		{
			if (isScoreAllocationPresentForIntersectType(intersectTypeId))
			{
				string sql = @"
select	S.Uid as AssetUid
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
where	exists (
		select	1
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
		)";
				Connection.OpenIfClosed().Wait();
				var results = Connection.Query<Guid>(sql, new { apiExecutionUid }, commandTimeout: timeout).ToList();
				if (results.Count > 0)
				{
					CreateRescoreRequests(results, ScoreType.Governance);
				}
			}
		}

		public void CreateMeasureChangedNotificationExecution(MetricAssetVersion version, DateTime effectiveDate, Guid? triggeredByMeasureUid = null)
		{
			var payload = new MeasureChangedModel
			{
				EffectiveDate = effectiveDate,
				MetricAssetUid = version.AssetUid,
				MetricAssetVersionUid = version.Uid
			};

			sendScoreQueueMessage(ScoreQueueChangeType.MeasureChanged, null, payload);
		}

		public void CreateParentAssetGovernanceRescoreExecution(Guid apiExecutionUid)
		{
			string sql = @"
declare @assetTypeUid uniqueidentifier;

select  top 1 
		@assetTypeUid = T.Uid
from    api.ExecutionItemDependentChange E
		inner join Asset A on A.Uid = JSON_VALUE(Payload, '$.ParentAssetUid')
		inner join AssetType T on T.ID = A.AssetTypeID
where   ExecutionID = @apiExecutionUid
		and DependentChangeType = 1 -- parent change

select	cast(EA.AssetUid as uniqueidentifier)
from	(
		select  ROW_NUMBER() OVER(order by JSON_VALUE(Payload, '$.ParentAssetUid')) ItemNumber,
				JSON_VALUE(Payload, '$.ParentAssetUid') as AssetUid 
		from    api.ExecutionItemDependentChange
		where   ExecutionID = @apiExecutionUid
				and DependentChangeType = 1 --parent change
		) EA
where	exists(
			select	1
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
		)";
			var assetUids = Query<Guid>(sql, new { apiExecutionUid }).ToList();
			assetUids.ForEach(assetUid =>
			{
				var info = new ScoreQueueInfo
				{
					CompanyID = SecurityContext.CompanyID,
					ResourceID =  SecurityContext.ResourceID,
					ChangeType = ScoreQueueChangeType.RescoreRequest,
					Payload = new AssetRescoreRequestModel { AssetUid = assetUid, ScoreType = ScoreType.Governance, EffectiveDate = DateTime.UtcNow.Date },
					StartedOn = DateTime.UtcNow
				};
				QueueSource.CreateMessage(constants.Queue.Score, info);
			});
		}
		
		public void CreateRescoreRequests(List<Guid> assets, ScoreType scoreType)
		{
			assets.ForEach(assetUid =>
			{
				var info = new ScoreQueueInfo
				{
					CompanyID = SecurityContext.CompanyID,
					ResourceID =  SecurityContext.ResourceID,
					ChangeType = ScoreQueueChangeType.RescoreRequest,
					Payload = new AssetRescoreRequestModel { AssetUid = assetUid, ScoreType = scoreType, EffectiveDate = DateTime.UtcNow.Date },
					StartedOn = DateTime.UtcNow
				};
				QueueSource.CreateMessage(constants.Queue.Score, info);
			});
		}

		public void CreateRescoreRequestsBasedOnResponsibilityRulesRun(List<int> ruleIds)
		{
			DateTime today = DateTime.UtcNow.Date;
			var assets = Query<Guid>(@"
declare		@relevantAssetTypes table (Id int)
insert into @relevantAssetTypes
	select		t.Id
	from		ResponsibilityTypeRelationRule p
				inner join ResponsibilityType r on r.ID = p.ResponsibilityTypeID and p.ID in (select ObjectID from @ruleIds)
				inner join AssetType t on t.Object = p.Object and t.ObjectID = p.ObjectID
				inner join metrics.Allocation al on al.AssetTypeUid = t.Uid and al.ScoreType = 1
				inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
				inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
					and ( 
						(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
						(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
						) 
					and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
					and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = r.Uid
					and V.Definition <> '{}'
	group by	t.Id;

select		a.Uid
from		ResponsibilityTypeRelationRule p
			inner join ResponsibilityRuleResultAsset ra on ra.RuleID = p.ID and ra.AssetTypeID = 0 -- specific to asset
			inner join Asset a on a.ID = ra.AssetID and a.AssetTypeID in (select Id from @relevantAssetTypes)
group by	a.Uid
union
select		a.Uid
from		ResponsibilityTypeRelationRule p
			inner join ResponsibilityRuleResultAsset ra on ra.RuleID = p.ID and ra.AssetID = 0 and ra.AssetTypeID in (select Id from @relevantAssetTypes) -- specific to asset type
			inner join Asset a on a.ID = ra.AssetTypeID
group by	a.Uid",
				new
				{
					today,
					ruleIds = ruleIds.AsTableValuedParameter("dbo.IDTable")
				}
			).ToList();
			CreateRescoreRequests(assets, ScoreType.Governance);
		}

		public void CreateRollupPathChangedExecution(int? intersectTypeId = null, int? assetTypeId = null, Guid? triggeredByApiExecutionUid = null)
		{
			sendScoreQueueMessage(ScoreQueueChangeType.RollupPathChanged);
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
				execution.ErrorMessage = message.Substring(0, Math.Min(ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
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
									row["Message"] = string.Format(Error.InvalidFormatError, "EffectiveDateStart", "yyyy-MM-dd");
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
									row["Message"] = string.Format(Error.InvalidFormatError, "EffectiveDateEnd", "yyyy-MM-dd");
									row["Success"] = 0;
								}
								else if (model.EffectiveDateStart != null && effectiveDateStart > effectiveDateEnd)
								{
									messages.Add(string.Format(Error.GreaterThanError, "EffectiveDateStart", "EffectiveDateEnd"));
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
									row["Message"] = string.Format(Error.InvalidFormatError, "RunDateStart", "yyyy-MM-dd HH:mm:ss");
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
									row["Message"] = string.Format(Error.InvalidFormatError, "RunDateEnd", "yyyy-MM-dd HH:mm:ss");
									row["Success"] = 0;
								}
								else if (model.RunDateStart != null && runDateStart > runDateEnd)
								{
									messages.Add(string.Format(Error.GreaterThanError, "RunDateStart", "RunDateEnd"));
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

					Connection.Execute(checkSQL, new { ResourceID =  SecurityContext.ResourceID, execution.ExecutionID, p = Permission.DeleteAsset }, commandTimeout: timeout);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, ERROR_MESSAGE_CHARACTER_LIMIT);
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

					var scoreImpactedAssets = new List<Guid>();

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
								scoreImpactedAssets.AddRange(GetImpactedAssetsFromRuleResults(ruleResultUids));
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
					if (scoreImpactedAssets.Count > 0)
					{
						CreateRescoreRequests(scoreImpactedAssets, ScoreType.DataQuality);
					}
				}
			}

			return results;
		}		
		
		public List<Guid> GetImpactedAssetsFromRuleResults(List<Guid> ruleResultUids)
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

			List<Guid> impactedAssets;
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

				impactedAssets = Connection.Query<Guid>($@"
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

select distinct AssetUid from #Items", transaction: trans, commandTimeout: timeout).ToList();
			}

			return impactedAssets;
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
		
		public List<Guid> GetScoreImpactedAssetsBasedOnResponsibilityAllocation(AssetType assetType, ResponsibilityType responsibility)
		{
			DateTime today = DateTime.UtcNow.Date;
			var impactedAssets = Query<Guid>(@"
select	distinct 
		A.Uid
from    ResponsibilityDetailByAssetTypeID (@ID) O 
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
group by A.Uid, M.AllocationUid, M.Uid, V.Uid", 
new { assetType.ID, ResponsibilityTypeUid = responsibility.UID, ResponsibilityTypeID = responsibility.ID, today });

			return impactedAssets.ToList();
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
							select     cast(S.Value * 100 as decimal(18,1)) as 'Score'                            
							from        Asset A                            
										inner join metrics.Score S on S.AssetUid = A.[uid] and S.EffectiveDate <= getutcdate()
										inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = @type and (Al.OverrideName is null or Al.OverrideName = '')
							where       A.ID = @assetId
							order by    S.EffectiveDate desc
							OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY";

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
				execution.ErrorMessage = message.Substring(0, Math.Min(ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
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
									row["Message"] = string.Format(Error.InvalidFormatError, "RunDate", "yyyy-MM-dd HH:mm:ss");
									row["Success"] = 0;
								}
								else
								{
									if (rundate > DateTime.Now)
									{
										row["Message"] = string.Format(Error.GreaterThanTodayError, "RunDate");
										row["Success"] = 0;
									}
									else if (rundate == DateTime.MinValue)
									{
										row["Message"] = string.Format(Error.GenericInvalidFieldValueError, model.RunDate, "RunDate");
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
										row["Message"] = string.Format(Error.InvalidFormatError, "EffectiveDate", "yyyy-MM-dd");
										row["Success"] = 0;
									}
									else if (effectiveDate == DateTime.MinValue)
									{
										row["Message"] = string.Format(Error.GenericInvalidFieldValueError, dataQualityInsertModel.EffectiveDate, "EffectiveDate");
										row["Success"] = 0;
									}
									else if (effectiveDate > DateTime.Now)
									{
										row["Message"] = string.Format(Error.GreaterThanTodayError, "EffectiveDate");
										row["Success"] = 0;
									}
								}
								else
								{
									row["Message"] = string.Format(Error.RequiredFieldError, "EffectiveDate");
									row["Success"] = 0;
								}

								if (model.RunDate == null)
								{
									row["Message"] = string.Format(Error.RequiredFieldError, "RunDate");
									row["Success"] = 0;
								}

								if (!model.PassCount.HasValue)
								{
									row["Message"] = string.Format(Error.RequiredFieldError, "PassCount");
									row["Success"] = 0;
								}

								if (!model.FailCount.HasValue)
								{
									row["Message"] = string.Format(Error.RequiredFieldError, "FailCount");
									row["Success"] = 0;
								}
							}

							if (model is DataQualityUpdateModel dataQualityUpdateModel)
							{
								row["Uid"] = dataQualityUpdateModel.Uid;

								if (!model.EvaluatedAssetUid.HasValue && model.RunDate == null && !model.PassCount.HasValue && !model.FailCount.HasValue)
								{
									row["Message"] = Error.InvalidUpdateError;
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
								row["Message"] = string.Format(Error.ValueBetweenError, "PassCount", 0, 9223372036854775807);
								row["Success"] = 0;
							}

							if (model.FailCount.HasValue && (model.FailCount < 0 || model.FailCount > 9223372036854775807))
							{
								row["Message"] = string.Format(Error.ValueBetweenError, "FailCount", 0, 9223372036854775807);
								row["Success"] = 0;
							}

							if (model.PassCount.HasValue && model.FailCount.HasValue)
							{
								ulong total = (ulong)model.PassCount.Value + (ulong)model.FailCount.Value;

								if (total > 9223372036854775807)
								{
									row["Message"] = string.Format(Error.GreaterThanError, "PassCount + FailCount", "9223372036854775807", 0);
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
										[Message] = coalesce([Message] + '; ', '') + '{string.Format(Error.GreaterThanError, "PassCount + FailCount", "9223372036854775807", 0)}'
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
									END)=1

								-- check Duplicate Record
									drop table if exists #tempduplicate;

									select OwningAssetUid,
											EffectiveDate,
											EvaluatedAssetUid,
											RunDate
									into #tempduplicate
									from api.[ExecutionAssetResult] EAR
									where success is null
									group by OwningAssetUid,
											EffectiveDate,
											EvaluatedAssetUid,
											RunDate
									having count(1) > 1;

									create clustered index cx_tempduplicate on #tempduplicate(OwningAssetUid,EvaluatedAssetUid,EffectiveDate,RunDate)

									update EAR
									set		Success = 0,
											[Message] = coalesce([Message] + '; ', '') + 'Duplicate data quality result for an asset/Rule in payload.'
									from api.[ExecutionAssetResult] EAR
									inner join #tempduplicate AR on AR.OwningAssetUid = EAR.OwningAssetUid
													and AR.EffectiveDate = EAR.EffectiveDate
													and AR.EvaluatedAssetUid = EAR.EvaluatedAssetUid
													and AR.RunDate = EAR.RunDate
									where EAR.ExecutionID = @ExecutionID
										and EAR.success is null;

									update EAR
									set		Success = 0,
											[Message] = coalesce([Message] + '; ', '') + 'Duplicate data quality result for an asset/Rule already in records.'
									from api.[ExecutionAssetResult] EAR
										inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID
										inner join AssetResult AR on AR.OwningAssetUid = EAR.OwningAssetUid
													and AR.EffectiveDate = EAR.EffectiveDate
													and AR.EvaluatedAssetUid = EAR.EvaluatedAssetUid
													and AR.RunDate = EAR.RunDate
									where 
										AE.Method = 'POST'
										and EAR.ExecutionID = @ExecutionID
										and success is null;
";

					Connection.Execute(checkSQL, new { ResourceID =  SecurityContext.ResourceID, execution.ExecutionID, p = Permission.ModifyAsset }, commandTimeout: timeout);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, ERROR_MESSAGE_CHARACTER_LIMIT);
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
									Connection.Execute(assetResultSQL, new { execution.ExecutionID, beginItemNumber, endItemNumber, userId =  SecurityContext.ResourceID, requestDate = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);
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
