using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.entities.Views;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

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

        public ObjectStatisticTileModel GetObjectStatistics(SystemObjects type, int id)
        {
            var model = new ObjectStatisticTileModel { Items = new List<ObjectStatisticTileItemModel>() };

            var list = Database.Connection.Query<RawObjectStatistic>("[tile].[GetObjectStatistics] @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).ToList();

            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

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
                        model.Items.Add(new ObjectStatisticTileItemModel { Count = i.Value.GetValueOrDefault(), Name = pluralize.Pluralize(i.Name ?? ""), TypeID = i.TypeID });
                        break;
                }
            });

            return model;
        }

        #endregion
    }

    public static partial class ConnectionExtensions
    {
        public static MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(this SqlConnection cnn, Guid assetTypeUid, DateTime? effectiveDate)
        {
            if (!effectiveDate.HasValue)
                effectiveDate = DateTime.UtcNow.Date;

            //declare @effectiveDate date = '10/2/2018', @assetTypeUid uniqueidentifier = '8371C4C6-E17E-4620-BA8B-AE0301966E0E';
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
					C.ValueJson
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

            foreach (var i in results)
            {
                if (!string.IsNullOrEmpty(i.ConditionsJson))
                {
                    i.Conditions = JsonConvert.DeserializeObject<List<MetricConditionHierarchyModel>>(i.ConditionsJson);
                    i.Conditions.ForEach(c =>
                    {
                        if (!string.IsNullOrEmpty(c.ValueJson))
                        {
                            c.Values = JsonConvert.DeserializeObject<List<string>>(c.ValueJson);
                        }
                    });
                }

                if (i.ParentUid.HasValue)
                {
                    var p = model.SingleOrDefault(o => o.Uid == i.ParentUid.Value);
                    if (p != null)
                    {
                        if (p.Metrics == null)
                            p.Metrics = new List<MetricAssetTypeHierarchyModel>();

                        p.Metrics.Add(i);
                    }
                }
                else
                {
                    model.Add(i);
                }
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

drop table if exists #tbl
create table #tbl ([Uid] uniqueidentifier, Name nvarchar(250), Description nvarchar(max), ParentUid uniqueidentifier, IsGroup bit, Weight decimal(5,3), EffectiveDate date)

insert into #tbl 
	select	A.[Uid],
			A.Name,
			A.Description,
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

select	h.[Uid],
		h.ParentUid,
		h.[Level],
		h.Name,
		h.Description,
	    h.IsGroup,
		h.Weight,
		coalesce(M.[Value], 0) as Value
from	h
		outer apply (
			select	I.EffectiveDate,
					I.[Value]
			from	metrics.ScoreItem I
					inner join (
						select	max(EffectiveDate) as EffectiveDate
						from	metrics.ScoreItem I
						where	AssetUid = @assetUid
								and MetricAssetUid = h.[Uid]
								and EffectiveDate <= @effectiveDate
					) MI on MI.EffectiveDate = I.EffectiveDate
			where	AssetUid = @assetUid
					and MetricAssetUid = h.[Uid]
		) M 
where	metrics.AssetMeetsConditions(h.[Uid], h.EffectiveDate, @assetUid) = 1";

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            var results = cnn.Query<MetricAssetHierarchyModel>(sql, new { assetUid, effectiveDate = effectiveDate.Value }).ToList();

            var model = new MetricAssetHierarchyModels();

            foreach (var i in results)
            {
            //    if (i.ParentUid.HasValue)
            //    {
            //        var p = model.SingleOrDefault(o => o.Uid == i.ParentUid.Value);
            //        if (p != null)
            //        {
            //            if (p.Metrics == null)
            //                p.Metrics = new List<MetricAssetHierarchyModel>();

            //            p.Metrics.Add(i);
            //        }
            //    }
            //    else
            //    {
                    model.Add(i);
            //    }
            }

            return model;
        }

        public static List<BulkMetricTemporaryTableModel> BulkMetricsImport(this SqlConnection cnn, BulkMetricsImport model)
        {
            List<BulkMetricTemporaryTableModel> results = null;

            var table = new System.Data.DataTable();

            table.Columns.Add("AssetUid", typeof(Guid));
            table.Columns.Add("MetricAssetUid", typeof(Guid));
            table.Columns.Add("EffectiveDate", typeof(DateTime));
            table.Columns.Add("Result", typeof(bool));
            table.Columns.Add("IsValidAsset", typeof(bool));
            table.Columns.Add("IsValidMetric", typeof(bool));
            table.Columns.Add("IsValidMetricDate", typeof(bool));
            table.Columns.Add("IsSuccess", typeof(bool));
            table.Columns.Add("ErrorMessage", typeof(string));

            #region Generate data sets

            foreach (var item in model)
            {
                var row = table.NewRow();
                row["AssetUid"] = item.AssetUid;
                row["MetricAssetUid"] = item.MetricAssetUid;
                row["EffectiveDate"] = item.EffectiveDate ?? DateTime.UtcNow.Date;
                row["Result"] = item.Result;
                row["IsValidAsset"] = false;
                row["IsValidMetric"] = false;
                row["IsValidMetricDate"] = false;
                row["IsSuccess"] = true;
                table.Rows.Add(row);
            }

            #endregion

            #region

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            using (var trans = cnn.BeginTransaction())
            {
                try
                {
                    cnn.Execute("DROP TABLE IF EXISTS #MetricsTable", transaction: trans);

                    #region Bulk Copy

                    cnn.Execute(@"
    create table #MetricsTable (
	    AssetUid uniqueidentifier not null,
	    MetricAssetUid uniqueidentifier not null,
	    EffectiveDate date not null,
	    [Result] [bit] not null,

        IsValidAsset bit not null,
        IsValidMetric bit not null,
        IsValidMetricDate bit not null,
        IsSuccess bit not null,
        ErrorMessage nvarchar(2500) null
    )", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempMetricsTable_AssetUid ON #MetricsTable ( AssetUid ASC )", transaction: trans);
                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempMetricsTable_MetricAssetUid ON #MetricsTable ( MetricAssetUid ASC )", transaction: trans);
                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempMetricsTable_IsSuccess ON #MetricsTable ( IsSuccess ASC )", transaction: trans);

                    var bulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "#MetricsTable",
                        BulkCopyTimeout = 3600
                    };

                    bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                    bulkCopy.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
                    bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                    bulkCopy.ColumnMappings.Add("Result", "Result");
                    bulkCopy.ColumnMappings.Add("IsValidAsset", "IsValidAsset");
                    bulkCopy.ColumnMappings.Add("IsValidMetric", "IsValidMetric");
                    bulkCopy.ColumnMappings.Add("IsValidMetricDate", "IsValidMetricDate");
                    bulkCopy.ColumnMappings.Add("IsSuccess", "IsSuccess");

                    bulkCopy.WriteToServer(table);

                    #endregion

                    #region Resolve Asset

                    cnn.Execute(@"
update  T
set     T.IsValidAsset = IIF(S.ID is not null, 1, 0)
from    #MetricsTable T
		left join Asset S on S.[uid] = T.AssetUid", transaction: trans);

                    #endregion

                    #region Resolve Metric

                    cnn.Execute(@"
update  T
set     T.IsValidMetric = IIF(S.[Uid] is not null, 1, 0)
from    #MetricsTable T
		left join metrics.[Asset] S on S.[Uid] = T.MetricAssetUid and S.[State] = 1", transaction: trans);

                    #endregion

                    #region Resolve Metric Group/Item Effective Date

                    cnn.Execute(@"
update  T
set     T.IsValidMetricDate = IIF(M_M.EffectiveDate is not null, 1, 0)
from    #MetricsTable T
        left join metrics.[Asset] A on A.[Uid] = T.MetricAssetUid and A.[State] = 1
        outer apply (
                    select  max(EffectiveDate) as EffectiveDate
                    from    metrics.AssetVersion
                    where   [Uid] = A.[Uid]
                            and EffectiveDate <= T.[EffectiveDate]
                    ) M_M", transaction: trans);

                    #endregion

                    #region Determine if Error

                    cnn.Execute(@"
update  #MetricsTable
set     IsSuccess = case 
                    when IsValidAsset = 0 then 0
                    when IsValidMetric = 0 then 0
                    when IsValidMetricDate = 0 then 0
                    else 1
                  end,
        ErrorMessage = '';

update  #MetricsTable
set     ErrorMessage = 'Invalid asset specified; '
where   IsValidAsset = 0;

update  #MetricsTable
set     ErrorMessage = 'Invalid metric specified; '
where   IsValidMetric = 0;

update  #MetricsTable
set     ErrorMessage = 'Invalid metric specified for the date provided; '
where   IsValidMetricDate = 0;

update  #MetricsTable
set     ErrorMessage = null
where   ErrorMessage = '' 
        and IsSuccess = 1;
", transaction: trans);

                    #endregion

                    #region Load valid items into staging table

                    cnn.Execute(@"
merge into  [metrics].StagingScoreItem T
using       (
            select      *
            from        #MetricsTable
            where       IsSuccess = 1
            ) S
on          (
                S.AssetUid = T.AssetUid and 
                S.MetricAssetUid = T.MetricAssetUid and 
                S.EffectiveDate = T.EffectiveDate
            )
when matched then
    update set
            T.Result = S.Result,
            T.Archived = 0
when not matched by target then
    insert  (AssetUid, MetricAssetUid, EffectiveDate, Result, Processing, Archived)
    values  (S.AssetUid, S.MetricAssetUid, S.EffectiveDate, S.Result, 0, 0);", transaction: trans);

                    #endregion

                    #endregion

                    results = cnn.Query<BulkMetricTemporaryTableModel>("select * from #MetricsTable", transaction: trans).ToList();
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }

            cnn.Close();

            return results;
        }
    }
}
