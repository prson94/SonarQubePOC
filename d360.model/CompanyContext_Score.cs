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
	public partial class CompanyContext : BaseContext
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

		#region Bulk Import Methods

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

		#endregion Bulk Import Methods

		public ObjectStatisticTileModel GetObjectStatistics(SystemObjects type, int id)
		{
			ObjectStatisticTileModel model = new ObjectStatisticTileModel { Items = new List<ObjectStatisticTileItemModel>() };

			List<RawObjectStatistic> list = Database.Connection.Query<RawObjectStatistic>("[dbo].[GetObjectStatistics] @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).ToList();

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

		#region Score Engine Methods

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

			Connection.OpenIfClosed().Wait();

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
													inner join Asset A on (A.Object = R.Subject and A.ObjectID = R.SubjectID)
													UNION ALL
													SELECT a.id, a.Uid, R.IntersectTypeID, A.AssetTypeID
													FROM api.ExecutionRelationship ER
													inner join [Intersect] R on R.ID = ER.IntersectID 
														and ER.ExecutionID = @apiExecutionUid 
														and ER.Success = 1
													inner join Asset A on (A.Object = R.Object and A.ObjectID = R.ObjectID)
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

			Connection.OpenIfClosed().Wait();

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

		#region helpers

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

		private bool isScoreAllocationPresentForIntersectType(int id)
		{
			bool present = Query<bool>(@"
										select	cast(iif(count(1)>0,1,0) as bit) 
										from	IntersectType T
											inner join AssetType A on (A.Object = T.Subject and A.ObjectID = T.SubjectID) or (A.Object = T.Object and A.ObjectID = T.ObjectID)
											inner join metrics.Allocation L on L.AssetTypeUid = A.Uid and L.ScoreType = 1
										where   T.ID = @id", new { id }).Single();

			return present;
		}

		#endregion helpers

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
									inner join IntersectType IA on ( (IA.Subject = T.Object and IA.SubjectID = A.ObjectID) or (IA.Object = T.Object and IA.ObjectID = A.ObjectID) ) 
									inner join [Predicate] P on P.Uid = JSON_VALUE(V.Definition, '$.Governance.Predicate.PredicateUid') and P.ID = IA.PredicateID and P.ID = @typeId";
					break;
				case MetricGovernanceCheckType.Relation:
					sql = @"
							select	V.Uid
							from	metrics.AssetVersion V
									inner join metrics.Asset A on A.Uid = V.AssetUid and V.Definition is not null 
									inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 1
									inner join AssetType T on T.Uid = Al.AssetTypeUid
									inner join IntersectType IA on ( (IA.Subject = T.Object and IA.SubjectID = T.ObjectID) or (IA.Object = T.Object and IA.ObjectID = T.ObjectID) ) 
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

		#endregion Score Engine Methods
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
