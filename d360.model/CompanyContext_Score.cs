using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.helpers;
using d360.core.queue;
using Dapper;
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

        public DbSet<MetricAllocation> MetricAllocations { get; set; }

        public DbSet<MetricAsset> MetricAssets { get; set; }

        public DbSet<MetricAssetVersion> MetricAssetVersions { get; set; }

        public DbSet<MetricAssetVersionCondition> MetricAssetVersionConditions { get; set; }

        public DbSet<MetricAssetVersionConditionItem> MetricAssetVersionConditionItems { get; set; }

        public DbSet<MetricAssetVersionConditionItemValue> MetricAssetVersionConditionItemValues { get; set; }

        public DbSet<Score> Scores { get; set; }

        #endregion

        #region Engine Methods

        public List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution, ScoreType scoreType = ScoreType.Governance, bool useAllocation = false)
        {
            Add(execution);
            SetApiExecutionProcessingStartTime(execution.ExecutionID);
            
            // Set effective date for any results that do not have a date set.
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
                var table = new DataTable();

                table.Columns.Add("AssetUid", typeof(Guid));
                table.Columns.Add("MetricAssetUid", typeof(Guid));
                table.Columns.Add("EffectiveDate", typeof(DateTime));
                table.Columns.Add("Result", typeof(bool));

                #region Generate data sets

                foreach (var item in model)
                {
                    var row = table.NewRow();
                    row["AssetUid"] = item.AssetUid;
                    row["MetricAssetUid"] = item.MetricAssetUid;
                    row["EffectiveDate"] = item.EffectiveDate ?? DateTime.UtcNow.Date;
                    row["Result"] = item.Result;

                    table.Rows.Add(row);
                }

                #endregion

                if (Connection.State != ConnectionState.Open)
                    Connection.Open();

                var trans = Connection.BeginTransaction();
                List<BulkMetricTemporaryTableModel> results = null;

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

    IsValidAllocation bit NULL,
	IsValidAsset bit NULL,
	IsValidMeasure bit NULL,
	IsValidEffectiveDate bit NULL,
	
    Success bit NULL,
	[Message] nvarchar(2500) NULL,

	PRIMARY KEY ( RowNumber ASC )
);

CREATE NONCLUSTERED INDEX [IX_TempInternalMeasures_AssetUid] ON #InternalMeasures ( [AssetUid] ASC );
CREATE NONCLUSTERED INDEX [IX_TempInternalMeasures_MetricAssetUid] ON #InternalMeasures ( [MetricAssetUid] ASC, EffectiveDate DESC );
CREATE NONCLUSTERED INDEX [IX_TempInternalMeasures_Success] ON #InternalMeasures ( [Success] ASC )", transaction: trans);

                    using (var bulk = Connection.CreateBulkCopy("#InternalMeasures", trans: trans))
                    {
                        bulk.ColumnMappings.Add("AssetUid", "AssetUid");
                        bulk.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
                        bulk.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                        bulk.ColumnMappings.Add("Result", "Result");

                        bulk.WriteToServer(table);
                    }

                    #region Validation

                    // Resolve Allocation
                    Connection.Execute(@"
update  M
set     M.IsValidAllocation = 0,
        M.Message = coalesce(Message, '') + 'This asset does not have this score type allocated; '
from    #InternalMeasures M
        left join AssetWithType A on A.uid = M.assetUid
        left join metrics.Allocation L on L.ScoreType = @scoreType and L.AssetTypeUid = A.AssetTypeUid
where   L.Uid is null;

update  M
set     M.IsValidAllocation = 0,
        M.Message = coalesce(Message, '') + 'This asset does not have this score type allocated for internal scores; '
from    #InternalMeasures M
        inner join AssetWithType A on A.uid = M.assetUid
        inner join metrics.Allocation L on L.ScoreType = @scoreType and L.AssetTypeUid = A.AssetTypeUid and L.IsExternallyCalculated = 1",
            new { scoreType = (int)scoreType }, transaction: trans);

                    // Resolve Asset
                    Connection.Execute(@"
update  T 
set     T.IsValidAsset = IIF(S.ID is not null, 1, 0) 
from    #InternalMeasures T 
        left join Asset S on S.[uid] = T.AssetUid", transaction: trans);

                    // Resolve Measure
                    Connection.Execute(@"
update  T 
set     T.IsValidMeasure = IIF(S.[Uid] is not null, 1, 0) 
from    #InternalMeasures T 
        left join metrics.[Asset] S on S.[Uid] = T.MetricAssetUid and S.[State] = 1", transaction: trans);

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
    set     Message = coalesce(Message, '') + 'Invalid measure specified for the date provided; '
    where   IsValidEffectiveDate = 0;

    update  #InternalMeasures
    set     Success = 0,
            Message = coalesce(Message, '') + 'Effective date cannot be in the future; '
    where   EffectiveDate > getutcdate();

    update #InternalMeasures set Message = null where Success = 1;", new { execution.ExecutionID }, transaction: trans);


                    #endregion

                    results = Connection.Query<BulkMetricTemporaryTableModel>(
                        $"select AssetUid, MetricAssetUid, EffectiveDate, Result, Success as IsSuccess, Message as ErrorMessage from #InternalMeasures",
                        new { execution.ExecutionID },
                        commandTimeout: 1200, transaction: trans
                    ).ToList();

                    trans.Commit();

                    execution.Error = results.Count(i => !i.IsSuccess);
                    execution.Processed = results.Count(i => i.IsSuccess);
                    execution.CompletedOn = DateTime.UtcNow;
                    Update(execution);

                    var queueResults = results.Where(r => r.IsSuccess).Select(r => new ExternalMeasureResultsCreatedModel
                    {
                        AssetUid = r.AssetUid,
                        EffectiveDate = r.EffectiveDate,
                        MetricAssetUid = r.MetricAssetUid,
                        Result = r.Result
                    }).ToList();

                    if (queueResults.Count > 0)
                    {
                        SendScoreEventWithPayload(execution.ExecutionID, ScoreQueueChangeType.ExternalMeasureResultsCreated, queueResults);
                    }
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    execution.ErrorMessage = ex.GetFullExceptionData(false);
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

        public List<ExternalScoreResultsApiResultsModel> BulkExternalResultsImport(List<ExternalScoreResultsApiPostModel> model, ApiExecution execution, ScoreType scoreType)
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

            var scoreTable = new DataTable();
            var measureTable = new DataTable();

            scoreTable.Columns.Add("ExecutionID", typeof(Guid));
            scoreTable.Columns.Add("ItemNumber", typeof(int));
            scoreTable.Columns.Add("AssetUid", typeof(Guid));
            scoreTable.Columns.Add("EffectiveDate", typeof(DateTime));
            scoreTable.Columns.Add("ScoreType", typeof(int));
            scoreTable.Columns.Add("Score", typeof(decimal));
            scoreTable.Columns.Add("RunDate", typeof(DateTime));
            scoreTable.Columns["RunDate"].AllowDBNull = true;

            measureTable.Columns.Add("ExecutionID", typeof(Guid));
            measureTable.Columns.Add("ItemNumber", typeof(int));
            measureTable.Columns.Add("MetricAssetUid", typeof(Guid));
            measureTable.Columns.Add("Passed", typeof(bool));

            int itemNumber = 1;
            foreach (var item in model)
            {
                var row = scoreTable.NewRow();
                row["ExecutionID"] = execution.ExecutionID;
                row["ItemNumber"] = itemNumber;
                row["AssetUid"] = item.assetUid;
                row["EffectiveDate"] = item.effectiveDate;
                row["ScoreType"] = (int)scoreType;
                row["Score"] = item.score;
                if (item.runDate.HasValue)
                    row["RunDate"] = item.runDate;
                else
                    row["RunDate"] = DBNull.Value;

                scoreTable.Rows.Add(row);

                if (item.measures != null && item.measures.Any())
                {
                    foreach (var measure in item.measures)
                    {
                        var measureRow = measureTable.NewRow();
                        
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
                Connection.Open();
            
            #region Bulk Copy

            using (var bulk = Connection.CreateBulkCopy("api.ExecutionScore"))
            {
                bulk.ColumnMappings.Add("ExecutionID", "ExecutionID");
                bulk.ColumnMappings.Add("ItemNumber", "ItemNumber");
                bulk.ColumnMappings.Add("AssetUid", "AssetUid");
                bulk.ColumnMappings.Add("ScoreType", "ScoreType");
                bulk.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                bulk.ColumnMappings.Add("RunDate", "RunDate");
                bulk.ColumnMappings.Add("Score", "Score");

                bulk.WriteToServer(scoreTable);
            }

            using (var bulk = Connection.CreateBulkCopy("api.ExecutionMeasure"))
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
where   T.ExecutionID = @ExecutionID"
            , new { execution.ExecutionID, scoreType = (int)scoreType }
            , commandTimeout: timeout);

            // Validate date ranges
            Connection.Execute(@"
update  T 
set     T.Success = 0, 
        T.Message = coalesce(T.Message, '') + 'Effective date cannot be in the future; '
from    api.ExecutionScore T 
where   T.ExecutionID = @executionID and T.EffectiveDate > getutcdate()", new { execution.ExecutionID }, commandTimeout: timeout);

            Connection.Execute(@"
update  T 
set     T.Success = 0, 
        T.Message = coalesce(T.Message, '') + 'Run date cannot be in the future; '
from    api.ExecutionScore T 
where   T.ExecutionID = @executionID and T.RunDate > getutcdate()", new { execution.ExecutionID }, commandTimeout: timeout);

            Connection.Execute(@"
update  T 
set     T.Success = 0, 
        T.Message = coalesce(T.Message, '') + 'Run date must be provided; '
from    api.ExecutionScore T 
where   T.ExecutionID = @executionID and T.RunDate is null", new { execution.ExecutionID }, commandTimeout: timeout);

            // Resolve measures
            Connection.Execute(@"
update  T 
set     T.Success = 0, 
        T.Message = coalesce(T.Message, '') + 'All measures must be provided for this metric; '
from    api.ExecutionScore T
        inner join metrics.Asset Ma on Ma.AllocationUid = T.AllocationUid and Ma.State = 1 and Ma.IsGroup = 0
        left join api.ExecutionMeasure Em on Em.ExecutionID = T.ExecutionID and Em.ItemNumber = T.ItemNumber and Em.MetricAssetUid = Ma.Uid
where   Em.ItemNumber is null", new { execution.ExecutionID, scoreType = (int)scoreType }, commandTimeout: timeout);

            // Validate score value
            Connection.Execute(@"
update  api.ExecutionScore 
set     Success = 0, 
        Message = coalesce(Message, '') + 'Score must be between 0 and 1; '
where   ExecutionID = @executionID 
        and ( [Score] is null or [Score] < 0 or [Score] > 1 )", new { execution.ExecutionID }, commandTimeout: timeout);

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
                                                    inner join api.ExecutionScore M on M.ExecutionID = E.ExecutionID and E.ExecutionID = @executionID and M.ItemNumber = E.ItemNumber
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
                                                    inner join api.ExecutionScore M on M.ExecutionID = E.ExecutionID and E.ExecutionID = @executionID and M.ItemNumber = E.ItemNumber
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


                            Connection.Execute($@"
		                    update  M
		                    set     M.EndDate = dateadd(d, -1, R.EffectiveDate)
		                    from    [metrics].[Score] M
                                    inner join api.ExecutionScore E on  E.ExecutionId = @executionID 
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
                            new { execution.ExecutionID, scoreType = (int)scoreType }, transaction: trans, commandTimeout: timeout);

                            var batchResults = Connection.Query<ExternalScoreResultsApiResultsModel>( $@"
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
                            trans.Rollback();

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
                // Send to ScoreEngine.
                var scores = results.Where(i => i.IsSuccess).Select(i => new ScoreCreatedModel { AllocationUid = i.AllocationUid, AssetUid = i.AssetUid, EffectiveDate = i.EffectiveDate }).ToList();
                if (scores.Count > 0)
                {
                    SendScoreEventWithPayload(execution.ExecutionID, ScoreQueueChangeType.ExternalScoresCreated, scores);
                }

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

            var list = Database.Connection.Query<RawObjectStatistic>("[dbo].[GetObjectStatistics] @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).ToList();

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

        public void SendScoreEventWithPayload<T>(Guid executionUid, ScoreQueueChangeType changeType, T item)
        {
            var info = new ScoreQueueInfo
            {
                CompanyID = CurrentCompanyID,
                ChangeType = changeType,
                ExecutionUid = executionUid,
                Location = ScoreQueueExecutionDataLocation.File
            };
            Storage.CreateFile(info.StorageFolder, info.StorageFile, JsonConvert.SerializeObject(item));
            QueueSource.CreateMessage(Config.GetValue<string>("ScoringQueue"), info);
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
