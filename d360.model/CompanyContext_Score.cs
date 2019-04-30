using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
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

        public List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution)
        {
            //Set effective date for any results that do not have a date set.
            model.ForEach(m => {
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
                throw new GenericException(
                    System.Net.HttpStatusCode.BadRequest,
                    "Duplicate items found in request", 
                    "The request contains duplicate combinations of AssetUid, MetricAssetUid, and EffectiveDate. You must send in unique combinations for those three fields.");
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

                // Log errors
                Connection.Execute(@"
    update  api.ExecutionMetric
    set     Success = case 
                        when IsValidAsset = 0 then 0
                        when IsValidMetric = 0 then 0
                        when IsValidMetricDate = 0 then 0
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
    insert  (AssetUid, MetricAssetUid, EffectiveDate, Result, Processing, Archived)
    values  (S.AssetUid, S.MetricAssetUid, S.EffectiveDate, S.Result, 0, 0);", 
                                new { execution.ExecutionID }, 
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
                            catch(Exception ex)
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

    public static partial class ConnectionExtensions
    {
        public static MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(this SqlConnection cnn, Guid assetTypeUid, DateTime? effectiveDate)
        {
            if (!effectiveDate.HasValue)
                effectiveDate = DateTime.UtcNow.Date;
                        
            var sql = @"
drop table if exists #tbl
create table #tbl ([Uid] uniqueidentifier, Name nvarchar(250), ParentUid uniqueidentifier, IsGroup bit, Weight decimal(5,3), EffectiveDate date)

insert into #tbl 
	select	A.[Uid],
			A.Name,
			A.ParentUid,
			A.IsGroup,
			V.Weight,
			V.EffectiveDate
	from	metrics.AssetVersion V
			inner join (
					select		IA.[Uid],
								max(IV.EffectiveDate) as EffectiveDate
					from		metrics.AssetVersion IV
								inner join metrics.Asset IA on IA.[Uid] = IV.[Uid] 
															and IA.AssetTypeUid = @assetTypeUid 
															and IV.EffectiveDate <= @effectiveDate 
															and IA.State = 1
					group by	IA.[Uid]
			) MV on MV.[Uid] = V.[Uid] AND MV.EffectiveDate = V.EffectiveDate
			inner join metrics.Asset A on A.[Uid] = V.[Uid];

with h as (
	select	*,
			1 as [Level]
	from	#tbl
	where	ParentUid is null
	union all
	select	A.*,
			h.[Level]+1 as [Level]
	from	#tbl A
			inner join h on h.[Uid] = A.ParentUid
)

select	[Uid],
		ParentUid,
		[Level],
		Name,
		IsGroup,
		Weight,
		(
			select	F.Name as FieldName,
					C.Operator,
					C.ValueJson as [Value]
			from	[metrics].[AssetVersionCondition] C
					inner join FieldType F on F.ID = C.FieldTypeID
			where	[Uid] = h.[Uid]
					and EffectiveDate = h.EffectiveDate
			for json path
		) as ConditionsJson
from	h
order by [Level] asc";

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            var results = cnn.Query<MetricAssetTypeHierarchyModel>(sql, new { assetTypeUid, effectiveDate = effectiveDate.Value }).ToList();
            var model = new MetricAssetTypeHierarchyModels();
            var builder = new MetricHierarchyBuilder();

            foreach (var i in results.Where(i => !i.ParentUid.HasValue))
            {
                builder.BuildMetricHierarchy(results, model, null, i);
            }

            return model;
        }

        public static MetricAssetHierarchyModels GetMetricHierarchyByAsset(this SqlConnection cnn, Guid assetUid, DateTime? effectiveDate)
        {
            if (!effectiveDate.HasValue)
                effectiveDate = DateTime.UtcNow.Date;

            var sql = @"
declare @assetTypeUid uniqueidentifier;
select	@assetTypeUid = T.[Uid]
from	dbo.Asset A
		inner join AssetType T on T.ID = A.AssetTypeID and A.[Uid] = @assetUid;

drop table if exists #groups;
create table #groups (
	[Uid] uniqueidentifier, EffectiveDate date
);

insert into #groups
	select		IA.[Uid],
				max(IV.EffectiveDate) as EffectiveDate
	from		metrics.AssetVersion IV
				inner join metrics.Asset IA on IA.[Uid] = IV.[Uid] 
											and IA.IsGroup = 1
											and IA.AssetTypeUid = @assetTypeUid 
											and IV.EffectiveDate <= @effectiveDate 
											and IA.State = 1
	group by	IA.[Uid];

drop table if exists #tbl
create table #tbl (
	[Uid] uniqueidentifier, ParentUid uniqueidentifier, 
	[Name] nvarchar(250), [Description] nvarchar(max), IsGroup bit, 
	[Weight] decimal(5,3), EffectiveDate date, 
	[Value] bit null, [Applies] bit null, [Level] int null
);

with rh as (
	select	I.MetricAssetUid,
			A.ParentUid,
			A.Name,	
			A.Description,
			A.IsGroup,
			I.AdjustedWeight as [Weight],
			I.EffectiveDate,
			I.[Value]
	from	metrics.ScoreItem I
			inner join metrics.Asset A on A.Uid = I.MetricAssetUid
			inner join (
				select	max(EffectiveDate) as EffectiveDate
				from	metrics.ScoreItem I
				where	AssetUid = @assetUid
						and MetricAssetUid = I.MetricAssetUid
						and EffectiveDate <= @effectiveDate
			) MI on MI.EffectiveDate = I.EffectiveDate
	where	AssetUid = @assetUid
	union all
	select	A.[Uid],
			A.ParentUid,
			A.Name,
			A.Description,
			A.IsGroup,
			V.Weight,
			V.EffectiveDate,
			NULL as Value
	from	metrics.AssetVersion V
			inner join #groups MV on MV.[Uid] = V.[Uid] AND MV.EffectiveDate = V.EffectiveDate
			inner join metrics.Asset A on A.[Uid] = V.[Uid]
			inner join rh C on C.ParentUid = A.Uid
	)

insert into #tbl 
	select *, NULL, NULL from rh;

with h as (
	select	*,
			1 as Lvl
	from	#tbl
	where	ParentUid is null
	union all
	select	A.*,
			h.Lvl+1 as Lvl
	from	#tbl A
			inner join h on h.[Uid] = A.ParentUid
)

update	T
set		T.[Level] = S.Lvl
from	#tbl T
		inner join h S on S.Uid = T.Uid;

select	distinct
		Uid, ParentUid, [Level], Name, Description, IsGroup, Weight, Value
from	#tbl 
order by [Level], Name";

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            var results = cnn.Query<MetricAssetHierarchyModel>(sql, new { assetUid, effectiveDate = effectiveDate.Value }).ToList();

            var model = new MetricAssetHierarchyModels();

            foreach (var i in results)
            {
                model.Add(i);
            }

            return model;
        }
    }
}
