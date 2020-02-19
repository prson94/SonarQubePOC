using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.entities.Scoring;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.helpers;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.Linq;

namespace d360.model
{
    partial class CompanyContext : BaseContext
    {
        #region DbSets

        public DbSet<MetricAsset> MetricAssets { get; set; }

        public DbSet<MetricAssetVersion> MetricAssetVersions { get; set; }

        public DbSet<MetricAssetVersionCondition> MetricAssetVersionConditions { get; set; }

        #endregion

        #region Engine Methods

        public List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution, ScoreType scoreType = ScoreType.Governance, bool useAllocation = false)
        {

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            //Set effective date for any results that do not have a date set.
            model.ForEach(m =>
            {
                if (!m.EffectiveDate.HasValue)
                {
                    m.EffectiveDate = DateTime.UtcNow.Date;
                }
            });

            var dupes = model
                .GroupBy(i => new { i.AssetUid, i.MetricAssetUid, i.EffectiveDate })
                .Where(i => i.Count() > 1)
                .Any();

            if (dupes)
            {
                Add(execution);

                var message = "The request contains duplicate combinations of AssetUid, MetricAssetUid, and EffectiveDate. You must send in unique combinations for those three fields.";
                execution.Error = 1;
                execution.Processed = 0;
                execution.CompletedOn = DateTime.UtcNow;
                execution.ErrorMessage = message;

                Update(execution);
                throw new GenericException(
                    System.Net.HttpStatusCode.BadRequest,
                    "Duplicate items found in request",
                    message);
            }
            else
            {

                Add(execution);

                var table = new DataTable();

                table.Columns.Add("ExecutionID", typeof(Guid));
                table.Columns.Add("ItemNumber", typeof(int));
                table.Columns.Add("AssetUid", typeof(Guid));
                table.Columns.Add("MetricAssetUid", typeof(Guid));
                table.Columns.Add("EffectiveDate", typeof(DateTime));
                table.Columns.Add("Result", typeof(bool));

                #region Generate data sets

                int itemNumber = 1;
                foreach (var item in model)
                {
                    var row = table.NewRow();
                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = itemNumber;
                    row["AssetUid"] = item.AssetUid;
                    row["MetricAssetUid"] = item.MetricAssetUid;
                    row["EffectiveDate"] = item.EffectiveDate ?? DateTime.UtcNow.Date;
                    row["Result"] = item.Result;

                    table.Rows.Add(row);

                    itemNumber++;
                }

                #endregion

                if (Connection.State != ConnectionState.Open)
                    Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                #region Bulk Copy

                var bulkCopy = new SqlBulkCopy(Connection)
                {
                    BatchSize = table.Rows.Count,
                    DestinationTableName = "[api].[ExecutionMetric]",
                    BulkCopyTimeout = 3600
                };

                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                bulkCopy.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
                bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                bulkCopy.ColumnMappings.Add("Result", "Result");

                bulkCopy.WriteToServer(table);

                bulkCopy = null;

                #endregion

                #region Validation

                // Resolve Asset
                Connection.Execute(@"update T set T.IsValidAsset = IIF(S.ID is not null, 1, 0) from api.ExecutionMetric T left join Asset S on S.[uid] = T.AssetUid where T.ExecutionID = @ExecutionID", new { execution.ExecutionID });

                // Resolve Metric
                Connection.Execute(@"update T set T.IsValidMetric = IIF(S.[Uid] is not null, 1, 0) from api.ExecutionMetric T left join metrics.[Asset] S on S.[Uid] = T.MetricAssetUid and S.[State] = 1 where T.ExecutionID = @ExecutionID", new { execution.ExecutionID });

                // Resolve Metric Group/Item Effective Date
                Connection.Execute(@"update T set T.IsValidMetricDate = IIF(M_M.EffectiveDate is not null, 1, 0) from api.ExecutionMetric T 
                left join metrics.[Asset] A on A.[Uid] = T.MetricAssetUid and A.[State] = 1
                outer apply (
                            select max(EffectiveDate) as EffectiveDate from metrics.AssetVersion where [Uid] = A.[Uid] and EffectiveDate <= T.[EffectiveDate]
                            ) M_M
                where T.ExecutionID = @ExecutionID", new { execution.ExecutionID });


                if (useAllocation)
                {
                    Connection.Execute(@"
                    update  M
                    set     M.IsValidAllocation = 0,
                            M.Message = coalesce(Message + '; ', '') + 'This asset does not have this score type allocated; '
                    from    api.ExecutionMetric M
                            left join AssetWithType A on A.uid = M.assetUid
                            left join metrics.Allocation L on L.ScoreType = @scoreType and L.AssetTypeUid = A.AssetTypeUid
                    where   L.Uid is null and M.ExecutionID = @executionID
                ", new { execution.ExecutionID, scoreType });


                    Connection.Execute(@"
                    update  M
                    set     M.IsValidAllocation = 0,
                            M.Message = coalesce(Message + '; ', '') + 'This asset does not have this score type allocated for internal scores; '
                    from    api.ExecutionMetric M
                            left join AssetWithType A on A.uid = M.assetUid
                            left join metrics.Allocation L on L.ScoreType = @scoreType and L.AssetTypeUid = A.AssetTypeUid and L.IsExternallyCalculated = 0
                    where   L.Uid is null and M.ExecutionID = @executionID
                ", new { execution.ExecutionID, scoreType });

                }


                // Log errors
                Connection.Execute(@"
    update  api.ExecutionMetric
    set     Success = case 
                        when IsValidAsset = 0 then 0
                        when IsValidMetric = 0 then 0
                        when IsValidMetricDate = 0 then 0
                        when IsValidAllocation = 0 then 0
                        else 1
                      end 
    where   ExecutionID = @ExecutionID;

    update  api.ExecutionMetric
    set     Message = coalesce(Message + '; ', '') + 'Invalid asset specified; '
    where   ExecutionID = @ExecutionID 
            and IsValidAsset = 0;

    update  api.ExecutionMetric
    set     Message = coalesce(Message + '; ', '') + 'Invalid metric specified; '
    where   ExecutionID = @ExecutionID 
            and IsValidMetric = 0;

    update  api.ExecutionMetric
    set     Message = coalesce(Message + '; ', '') + 'Invalid metric specified for the date provided; '
    where   ExecutionID = @ExecutionID 
            and IsValidMetricDate = 0;

    update  api.ExecutionMetric
    set     Success = 0,
            Message = coalesce(Message + '; ', '') + 'Effective date cannot be in the future; '
    where   ExecutionID = @ExecutionID and EffectiveDate > getutcdate();

    update api.ExecutionMetric set Message = null where ExecutionID = @ExecutionID and Success = 1;", new { execution.ExecutionID });


                #endregion

                List<BulkMetricTemporaryTableModel> results = new List<BulkMetricTemporaryTableModel>();

                int loopSize = 100;
                int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total) / loopSize);
                int beginItemNumber = 1;
                int endItemNumber = loopSize;

                for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                {
                    bool runCompleted = false;
                    int retryCount = 0;

                    while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                    {
                        using (var trans = Connection.BeginTransaction())
                        {
                            try
                            {
                                #region Load valid items into staging table

                                Connection.Execute($@"
merge into  [metrics].StagingScoreItem T
using       (
            select      *
            from        api.ExecutionMetric
            where       ExecutionID = @ExecutionID 
                        and ItemNumber between {beginItemNumber} and {endItemNumber}
                        and Success = 1
            ) S
on          (
                S.AssetUid = T.AssetUid and 
                S.MetricAssetUid = T.MetricAssetUid and 
                S.EffectiveDate = T.EffectiveDate
            )
when matched and T.Result <> S.Result then
    update set
            T.Result = S.Result,
            T.Archived = 0
when not matched by target then
    insert  (AssetUid, MetricAssetUid, EffectiveDate, Result, Processing, Archived, ScoreType)
    values  (S.AssetUid, S.MetricAssetUid, S.EffectiveDate, S.Result, 0, 0, @scoreType);",
                                new { execution.ExecutionID, scoreType },
                                transaction: trans);

                                #endregion

                                results.AddRange(
                                    Connection.Query<BulkMetricTemporaryTableModel>(
                                    $"select AssetUid, MetricAssetUid, EffectiveDate, Result, Success as IsSuccess, Message as ErrorMessage, IsValidAsset, IsValidMetric, IsValidMetricDate from api.ExecutionMetric where ExecutionID = @ExecutionID and ItemNumber between {beginItemNumber} and {endItemNumber}",
                                    new { execution.ExecutionID },
                                    transaction: trans)
                                );

                                trans.Commit();

                                runCompleted = true;
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();

                                retryCount++;

                                if (retryCount > API_V2_RETRY_LIMIT)
                                {
                                    LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionMetric", ex.GetFullExceptionData(false), 3600);
                                }
                            }
                        }
                    }

                    beginItemNumber += loopSize;
                    endItemNumber += loopSize;
                }

                try
                {
                    execution.Error = results.Count(i => !i.IsSuccess);
                    execution.Processed = results.Count(i => i.IsSuccess);
                    execution.CompletedOn = DateTime.UtcNow;
                    Update(execution);

                    // Cleanup
                    Connection.Execute($"delete api.ExecutionMetric where ExecutionID = @ExecutionID", new { execution.ExecutionID });
                }
                catch (Exception ex)
                {
                    execution.ErrorMessage = ex.GetFullExceptionData(false);
                    execution.CompletedOn = DateTime.UtcNow;
                    Update(execution);
                }

                Connection.Close();

                return results;
            }
        }

        public List<ExternalScoreResultsApiResultsModel> BulkExternalResultsImport(List<ExternalScoreResultsApiPostModel> model, ApiExecution execution, ScoreType scoreType)
        {
            SetApiExecutionProcessingStartTime(execution.ExecutionID);


            Add(execution);


            #region Generate Data Sets

            var metricTable = new DataTable();
            var measureTable = new DataTable();

            metricTable.Columns.Add("ExecutionID", typeof(Guid));
            metricTable.Columns.Add("ItemNumber", typeof(int));
            metricTable.Columns.Add("AssetUid", typeof(Guid));
            metricTable.Columns.Add("MetricAssetUid", typeof(Guid));
            metricTable.Columns.Add("EffectiveDate", typeof(DateTime));
            metricTable.Columns.Add("Result", typeof(bool));
            metricTable.Columns.Add("Value", typeof(decimal));
            metricTable.Columns.Add("RunDate", typeof(DateTime));


            measureTable.Columns.Add("ExecutionID", typeof(Guid));
            measureTable.Columns.Add("ItemNumber", typeof(int));
            measureTable.Columns.Add("MeasureUid", typeof(Guid));
            measureTable.Columns.Add("Passed", typeof(bool));

            int itemNumber = 1;
            foreach (var item in model)
            {
                var row = metricTable.NewRow();
                row["ExecutionID"] = execution.ExecutionID;
                row["ItemNumber"] = itemNumber;
                row["AssetUid"] = item.assetUid;
                row["MetricAssetUid"] = Guid.Empty;
                row["EffectiveDate"] = item.effectiveDate;
                row["Result"] = false;
                row["Value"] = item.score;
                row["RunDate"] = item.runDate;


                metricTable.Rows.Add(row);

                if (item.measures != null && item.measures.Any())
                {
                    foreach (var measure in item.measures)
                    {
                        var measureRow = measureTable.NewRow();
                        measureRow["ExecutionID"] = execution.ExecutionID;
                        measureRow["ItemNumber"] = itemNumber;
                        measureRow["MeasureUid"] = measure.measureUid;
                        measureRow["Passed"] = measure.passed;

                        measureTable.Rows.Add(measureRow);
                    }
                }

                itemNumber++;
            }

            #endregion

            if (Connection.State != ConnectionState.Open)
                Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

            #region Bulk Copy

            var bulkCopy = new SqlBulkCopy(Connection)
            {
                BatchSize = metricTable.Rows.Count,
                DestinationTableName = "[api].[ExecutionMetric]",
                BulkCopyTimeout = 3600
            };

            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
            bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
            bulkCopy.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
            bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
            bulkCopy.ColumnMappings.Add("Result", "Result");
            bulkCopy.ColumnMappings.Add("RunDate", "RunDate");
            bulkCopy.ColumnMappings.Add("Value", "Value");


            bulkCopy.WriteToServer(metricTable);

            bulkCopy = new SqlBulkCopy(Connection)
            {
                BatchSize = measureTable.Rows.Count,
                DestinationTableName = "[api].[ExecutionMetricMeasure]"
            };

            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
            bulkCopy.ColumnMappings.Add("MeasureUid", "MeasureUid");
            bulkCopy.ColumnMappings.Add("Passed", "Passed");

            bulkCopy.WriteToServer(measureTable);

            #endregion


            #region Validation

            //resolve allocation
            Connection.Execute(@"update T set T.IsValidAsset = case when S.uid is null then 0 else 1 end 
from api.ExecutionMetric T 
left join Asset S on S.[uid] = T.AssetUid 
where T.ExecutionID = @executionID", new { execution.ExecutionID, scoreType = (int)scoreType });

            //validate effective date
            Connection.Execute(@"update T set T.IsValidMetricDate = 0, T.Message = coalesce(T.Message + '; ', '') + 'Effective date cannot be in the future; '
from api.ExecutionMetric T 
where T.ExecutionID = @executionID and T.EffectiveDate > getutcdate()", new { execution.ExecutionID });


            //resolve allocation
            Connection.Execute(@"update T set T.IsValidAllocation = case when A.uid is null then 0 else 1 end 
from api.ExecutionMetric T 
left join Asset S on S.[uid] = T.AssetUid 
left join AssetType AT on AT.ID = S.AssetTypeId
left join metrics.Allocation A on A.AssetTypeUid = AT.[uid] 
where T.ExecutionID = @executionID and A.ScoreType = @scoreType and A.IsExternallyCalculated = 1 and T.IsValidAsset = 1", new { execution.ExecutionID, scoreType = (int)scoreType });

            //resolve measures
            Connection.Execute(@"update T set T.Success = 0, T.Message = coalesce(T.Message + '; ', '') + 'All measures must be provided for this metric; '
from api.ExecutionMetric T
left join Asset S on S.[uid] = T.AssetUid 
left join AssetType AT on AT.ID = S.AssetTypeId
inner join metrics.Asset A on A.assetTypeUid = AT.uid and A.[State] = 1 and scoreType = @scoreType
where T.ExecutionID = @executionID 
and T.IsValidAsset = 1
and A.uid not in (select measureuid from api.ExecutionMetricMeasure where ExecutionID = @executionID and ItemNumber = T.ItemNumber)
", new { execution.ExecutionID, scoreType = (int)scoreType });

            Connection.Execute(@"update T set T.Success = 0, T.Message = coalesce(T.Message + '; ', '') + 'Provided measures do not match allocation measures; '
from api.ExecutionMetric T
left join Asset S on S.[uid] = T.AssetUid 
left join AssetType AT on AT.ID = S.AssetTypeId
inner join api.ExecutionMetricMeasure M on M.ExecutionID = @executionID and M.ItemNUmber = T.ItemNumber
where T.ExecutionID = @executionID 
and T.IsValidAsset = 1
and M.MeasureUid not in (select uid from metrics.Asset where AssetTypeUid = At.uid and [State] = 1 and scoreType = @scoreType)

", new { execution.ExecutionID, scoreType = (int)scoreType });

            //validate score
            Connection.Execute(@"update T set T.Success = 0, T.Message = coalesce(T.Message + '; ', '') + 'Score must be between 0 and 1; '
from api.ExecutionMetric T 
where T.ExecutionID = @executionID and T.[Value] is null or T.[Value] < 0 or T.[Value] > 1", new { execution.ExecutionID });


            //update success status
            Connection.Execute(@"update  api.ExecutionMetric
                set     Success = case 
                                    when IsValidAsset = 0 then 0
                                    when IsValidMetric = 0 then 0
                                    when IsValidMetricDate = 0 then 0
                                    else 1
                                  end 
                where   ExecutionID = @ExecutionID and success is null;

                update  api.ExecutionMetric
                set     Message = coalesce(Message + '; ', '') + 'Invalid asset specified; '
                where   ExecutionID = @ExecutionID 
                        and IsValidAsset = 0;

                update  api.ExecutionMetric
                set     Message = coalesce(Message + '; ', '') + 'Invalid metric specified; '
                where   ExecutionID = @ExecutionID 
                        and IsValidMetric = 0;

                update  api.ExecutionMetric
                set     Message = coalesce(Message + '; ', '') + 'Invalid metric specified for the date provided; '
                where   ExecutionID = @ExecutionID 
                        and IsValidMetricDate = 0;

                update  api.ExecutionMetric
                set     Success = 1
                where   ExecutionID = @ExecutionID
                        and Success is null;

                ", new { execution.ExecutionID });


            #endregion


            #region Load Data

            int loopSize = 100;
            int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total) / loopSize);
            int beginItemNumber = 1;
            int endItemNumber = loopSize;
            var results = new List<ExternalScoreResultsApiResultsModel>();

            for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
            {
                bool runCompleted = false;
                int retryCount = 0;

                while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                {
                    using (var trans = Connection.BeginTransaction())
                    {
                        try
                        {
                            #region Load valid items into table

                            Connection.Execute($@"
                            merge into  [metrics].Score T
                            using       (
                                        select      *
                                        from        api.ExecutionMetric
                                        where       ExecutionID = @ExecutionID 
                                                    and ItemNumber between {beginItemNumber} and {endItemNumber}
                                                    and Success = 1
                                        ) S
                            on          (
                                            S.AssetUid = T.AssetUid and 
                                            T.ScoreType = @scoreType and 
                                            S.EffectiveDate = T.EffectiveDate
                                        )
                            when matched and T.Value <> S.Value then
                                update set
                                        T.Value = S.Value,
                                        T.RunDate = S.RunDate
                            when not matched by target then
                                insert  (AssetUid, EffectiveDate, RunDate, Value, EndDate, ScoreType)
                                values  (S.AssetUid, S.EffectiveDate, S.RunDate, S.Value, null, @scoreType);",
                                            new { execution.ExecutionID, scoreType = (int)scoreType },
                                            transaction: trans);

                            Connection.Execute($@"
                            merge into  [metrics].ScoreItem T
                            using       (
                                        select      E.AssetUid, E.MetricAssetUid, E.EffectiveDate, M.Passed, M.MeasureUid, E.RunDate
                                        from        api.ExecutionMetric E
                                        inner join api.ExecutionMetricMeasure M on M.ExecutionID = @executionID and M.ItemNumber = E.ItemNumber
                                        where       E.ExecutionID = @ExecutionID 
                                                    and E.ItemNumber between {beginItemNumber} and {endItemNumber}
                                                    and E.Success = 1
                                        ) S
                            on          (
                                            S.AssetUid = T.AssetUid and 
                                            S.MeasureUid = T.MetricAssetUid and 
                                            S.EffectiveDate = T.EffectiveDate
                                        )
                            when matched then
                                update set
                                        T.[Value] = S.Passed,
                                        T.RunDate = S.RunDate,
                                        T.UpdatedOn = getutcdate()
                            when not matched by target then
                                insert  (AssetUid, MetricAssetUid, EffectiveDate, [Value], RunDate, AdjustedWeight, UpdatedOn)
                                values  (S.AssetUid, S.MeasureUid, S.EffectiveDate, S.Passed, S.RunDate, NULL, getutcdate());",
                            new { execution.ExecutionID, scoreType = (int)scoreType },
                            transaction: trans);


                            results.AddRange(
                                 Connection.Query<ExternalScoreResultsApiResultsModel>(
                                 $"select AssetUid, EffectiveDate, Success as IsSuccess, Message as ErrorMessage  from api.ExecutionMetric where ExecutionID = @ExecutionID and ItemNumber between {beginItemNumber} and {endItemNumber}",
                                 new { execution.ExecutionID },
                                 transaction: trans)
 );

                            #endregion

                            trans.Commit();

                            runCompleted = true;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();

                            retryCount++;

                            if (retryCount > API_V2_RETRY_LIMIT)
                            {
                                LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionMetric", ex.GetFullExceptionData(false), 3600);
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
                //update EndDate of previous scores
                Connection.Execute(@"
		        update  M
		        set     M.EndDate = R.EffectiveDate
		        from    [metrics].[Score] M
                        inner join api.ExecutionMetric E on E.ExecutionId = @executionID and M.AssetUid = E.AssetUid and E.Success = 1
		                cross apply (
			                select top 1 EffectiveDate from metrics.Score R
			                where R.EffectiveDate <> M.EffectiveDate and R.AssetUid = M.AssetUid
			                and R.EffectiveDate > M.EffectiveDate
			                order by EffectiveDate asc
		                ) R
                where   M.EndDate is null and M.ScoreType = @scoreType

		        update  M
		        set     M.EndDate = R.EffectiveDate
		        from    [metrics].[ScoreItem] M
                        inner join api.ExecutionMetric E on E.ExecutionId = @executionID and E.AssetUid = M.AssetUid and E.Success = 1
                        inner join api.ExecutionMetricMeasure S on S.ExecutionId = @executionID and S.MeasureUid = M.MetricAssetUid
		                cross apply (
			                select top 1 EffectiveDate from metrics.ScoreItem R
			                where R.EffectiveDate <> M.EffectiveDate and R.AssetUid = M.AssetUid and R.MetricAssetUid = M.MetricAssetUid
			                and R.EffectiveDate > M.EffectiveDate
			                order by EffectiveDate asc
		                ) R
                where   M.EndDate is null", new { execution.ExecutionID, scoreType = (int)scoreType });


                execution.Error = results.Count(i => !i.IsSuccess);
                execution.Processed = results.Count(i => i.IsSuccess);
                execution.CompletedOn = DateTime.UtcNow;
                Update(execution);

                // Cleanup
                Connection.Execute($"delete api.ExecutionMetricMeasure where ExecutionID = @ExecutionID", new { execution.ExecutionID });
                Connection.Execute($"delete api.ExecutionMetric where ExecutionID = @ExecutionID", new { execution.ExecutionID });
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                Update(execution);
            }

            Connection.Close();

            return results;

        }

        public ObjectStatisticTileModel GetObjectStatistics(SystemObjects type, int id)
        {
            var model = new ObjectStatisticTileModel { Items = new List<ObjectStatisticTileItemModel>() };

            var list = Database.Connection.Query<RawObjectStatistic>("[tile].[GetObjectStatistics] @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).ToList();

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
                        var name = "";

                        if (PluralCultureHelper.IsNeutralCultureEnglish())
                        {
                            var namePluralizationInstance = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                            name = namePluralizationInstance.Pluralize(i.Name ?? "");
                        }

                        model.Items.Add(new ObjectStatisticTileItemModel { Count = i.Value.GetValueOrDefault(), Name = name, TypeID = i.TypeID });
                        break;
                }
            });

            return model;
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
            foreach (var c in results.Where(o => o.ParentUid == i.Uid))
            {
                BuildMetricHierarchy(results, model, i, c);
            }

            if (p != null)
            {
                if (p.Metrics == null)
                    p.Metrics = new List<MetricAssetTypeHierarchyModel>();

                p.Metrics.Add(i);
            }
            else
            {
                model.Add(i);
            }
        }
    }

}
