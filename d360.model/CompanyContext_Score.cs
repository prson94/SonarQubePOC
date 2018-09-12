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
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<MetricCondition> MetricConditions { get; set; }

        public DbSet<MetricConditionValue> MetricConditionValues { get; set; }

        public DbSet<MetricGroup> MetricGroups { get; set; }

        public DbSet<MetricItem> MetricItems { get; set; }

        public DbSet<MetricMap> MetricMaps { get; set; }

        public DbSet<MetricMapResult> MetricMapResults { get; set; }

        public DbSet<MetricScore> MetricScores { get; set; }

        #endregion

        #region Engine Methods

        public IEnumerable<dynamic> GetStatisticTypeRollupCheckOptions()
        {
            var sql = @"select * from (
			select	'ArtifactType|' + cast(ID as varchar(15)) as ID, 'Artifacts :: ' + Name as Name from ArtifactType
			) O order by Name";
            return Query<dynamic>(sql).ToList();
        }

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
        public static MetricGroupHierarchyModels GetMetricDefinitionHierarchyByAssetType(this SqlConnection cnn, Guid assetTypeUid, DateTime? effectiveDate)
        {
            if (!effectiveDate.HasValue)
                effectiveDate = DateTime.UtcNow;

            var sql = @"
with GH as (
	select	G.ID,
			G.ParentID,
			G.[Uid] as MetricGroupUid,
			G.Name as MetricGroupName,
			G.[Weight],
			0 as [Level],
			(
			select	I.[Uid] as MetricItemUid,
					I.Name as MetricItemName,
					M.[Weight],
					(
					select	F.Name as FieldName,
							AndOr,
							Operator,
							Value
					from	[metrics].[Condition] C
							inner join  FieldType F on F.ID = C.FieldTypeID
					where	MapID = M.ID
					for json path
					)as Conditions
			from	metrics.Item I
					cross apply (
						select	min(IM.ID) as ID
						from	metrics.Map IM
								inner join AssetType IA on IA.[uid] = @assetTypeUid and IA.ID = IM.AssetTypeID
						where	IM.ItemID = I.ID
								and IM.GroupID = G.ID
								and IM.EffectiveDate <= @effectiveDate
								and IM.[State] in (1)
					) MM
					inner join metrics.Map M on M.ID = MM.ID	
			for json path		
			) as RawItems
	from	metrics.[Group] G 
			cross apply (
				select	min(IM.ID) as ID
				from	metrics.Map IM
						inner join AssetType IA on IA.[uid] = @assetTypeUid and IA.ID = IM.AssetTypeID
				where	IM.GroupID = G.ID
						and IM.EffectiveDate <= @effectiveDate
						and IM.[State] in (1)
			) OM
	union all
	select	P.ID,
			P.ParentID,
			P.[Uid] as MetricGroupUid,
			P.Name as MetricGroupName,
			P.[Weight],
			C.[Level]-1 as [Level],
			null as RawItems
	from	metrics.[Group] P 
			inner join GH C on C.ParentID = P.ID
)

select	ID,
		ParentID,
		MetricGroupUid,
		MetricGroupName,
		[Weight],
		RawItems,
		min([Level]) as [Level]
from	GH
group by ID,
		ParentID,
		MetricGroupUid,
		MetricGroupName,
		[Weight],
		RawItems
order by min([Level]) asc";

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            var results = cnn.Query<MetricGroupHierarchyModel>(sql, new { assetTypeUid, effectiveDate = effectiveDate.Value }).ToList();

            var model = new MetricGroupHierarchyModels();

            foreach (var i in results)
            {
                if (i.RawItems != "[]" && !string.IsNullOrEmpty(i.RawItems))
                {
                    i.Items = JsonConvert.DeserializeObject<List<MetricItemHierarchyModel>>(i.RawItems);
                }

                if (i.ParentID.HasValue)
                {
                    var p = model.Single(o => o.ID == i.ParentID.Value);
                    if (p.Groups == null)
                        p.Groups = new List<MetricGroupHierarchyModel>();

                    p.Groups.Add(i);
                }
                else
                {
                    model.Add(i);
                }
            }

            return model;
        }

        public static List<BulkMetricTemporaryTableModel> BulkMetricsImport(this SqlConnection cnn, BulkMetricsImport model)
        {
            List<BulkMetricTemporaryTableModel> results = null;

            var table = new System.Data.DataTable();

            table.Columns.Add("AssetUid", typeof(Guid));
            table.Columns.Add("MetricGroupUid", typeof(Guid));
            table.Columns.Add("MetricItemUid", typeof(Guid));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("Result", typeof(bool));
            table.Columns.Add("IsValidAsset", typeof(bool));
            table.Columns.Add("IsValidMetricGroup", typeof(bool));
            table.Columns.Add("IsValidMetricItem", typeof(bool));
            table.Columns.Add("IsValidMetricDate", typeof(bool));
            table.Columns.Add("IsSuccess", typeof(bool));

            #region Generate data sets

            foreach (var item in model)
            {
                var row = table.NewRow();
                row["AssetUid"] = item.AssetUid;
                row["MetricGroupUid"] = item.MetricGroupUid;
                row["MetricItemUid"] = item.MetricItemUid;
                row["Date"] = item.Date ?? DateTime.UtcNow.Date;
                row["Result"] = item.Result;
                row["IsValidAsset"] = false;
                row["IsValidMetricGroup"] = false;
                row["IsValidMetricItem"] = false;
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
	    MetricGroupUid uniqueidentifier not null,
	    MetricItemUid uniqueidentifier not null,
	    [Date] date not null,
	    [Result] [bit] not null,

        IsValidAsset bit not null,
        IsValidMetricGroup bit not null,
        IsValidMetricItem bit not null,
        IsValidMetricDate bit not null,
        IsSuccess bit not null,
        ErrorMessage nvarchar(2500) null
    )", transaction: trans);

                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempMetricsTable_AssetUid ON #MetricsTable ( AssetUid ASC )", transaction: trans);
                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempMetricsTable_MetricGroupUid ON #MetricsTable ( MetricGroupUid ASC )", transaction: trans);
                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempMetricsTable_MetricItemUid ON #MetricsTable ( MetricItemUid ASC )", transaction: trans);
                    cnn.Execute(@"CREATE NONCLUSTERED INDEX IX_TempMetricsTable_IsSuccess ON #MetricsTable ( IsSuccess ASC )", transaction: trans);

                    var bulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "#MetricsTable",
                        BulkCopyTimeout = 3600
                    };

                    bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                    bulkCopy.ColumnMappings.Add("MetricGroupUid", "MetricGroupUid");
                    bulkCopy.ColumnMappings.Add("MetricItemUid", "MetricItemUid");
                    bulkCopy.ColumnMappings.Add("Date", "Date");
                    bulkCopy.ColumnMappings.Add("Result", "Result");
                    bulkCopy.ColumnMappings.Add("IsValidAsset", "IsValidAsset");
                    bulkCopy.ColumnMappings.Add("IsValidMetricGroup", "IsValidMetricGroup");
                    bulkCopy.ColumnMappings.Add("IsValidMetricItem", "IsValidMetricItem");
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

                    #region Resolve Metric Group

                    cnn.Execute(@"
update  T
set     T.IsValidMetricGroup = IIF(S.ID is not null, 1, 0)
from    #MetricsTable T
		left join metrics.[Group] S on S.[uid] = T.MetricGroupUid", transaction: trans);

                    #endregion

                    #region Resolve Metric Item

                    cnn.Execute(@"
update  T
set     T.IsValidMetricItem = IIF(S.ID is not null, 1, 0)
from    #MetricsTable T
		left join metrics.[Item] S on S.[uid] = T.MetricItemUid", transaction: trans);

                    #endregion

                    #region Resolve Metric Group/Item Effective Date

                    cnn.Execute(@"
update  T
set     T.IsValidMetricDate = IIF(M_M.ID is not null, 1, 0)
from    #MetricsTable T
        left join metrics.[Group] M_G on M_G.[uid] = T.MetricGroupUid		
        left join metrics.[Item] M_I on M_I.[uid] = T.MetricItemUid
        outer apply (
                    select  min(ID) as ID,
                            min(EffectiveDate) as EffectiveDate
                    from    metrics.Map 
                    where   GroupID = M_G.ID 
                            and ItemID = M_I.ID 
                            and EffectiveDate <= T.[Date]
                            and [State] not in (2,3)
                    ) M_M", transaction: trans);

                    #endregion

                    #region Determine if Error

                    cnn.Execute(@"
update  #MetricsTable
set     IsSuccess = case 
                    when IsValidAsset = 0 then 0
                    when IsValidMetricGroup = 0 then 0
                    when IsValidMetricItem = 0 then 0
                    when IsValidMetricDate = 0 then 0
                    else 1
                  end,
        ErrorMessage = '';

update  #MetricsTable
set     ErrorMessage = 'Invalid asset specified; '
where   IsValidAsset = 0;

update  #MetricsTable
set     ErrorMessage = 'Invalid metric group specified; '
where   IsValidMetricGroup = 0;

update  #MetricsTable
set     ErrorMessage = 'Invalid metric item specified; '
where   IsValidMetricItem = 0;

update  #MetricsTable
set     ErrorMessage = 'Invalid metric group/item specified for the date provided; '
where   IsValidMetricDate = 0;

update  #MetricsTable
set     ErrorMessage = null
where   ErrorMessage = '' 
        and IsSuccess = 1;
", transaction: trans);

                    #endregion

                    #region Load valid items into staging table

                    cnn.Execute(@"
merge into  [metrics].StagingItem T
using       (
            select      *
            from        #MetricsTable
            where       IsSuccess = 1
            ) S
on          (
                S.AssetUid = T.AssetUid and 
                S.MetricGroupUid = T.MetricGroupUid and 
                S.MetricItemUid = T.MetricItemUid and
                S.[Date] = T.EffectiveDate
            )
when matched then
    update set
            T.Result = S.Result
when not matched by target then
    insert  (AssetUid, MetricGroupUid, MetricItemUid, EffectiveDate, Result, Processing, Archived)
    values  (S.AssetUid, S.MetricGroupUid, S.MetricItemUid, S.[Date], S.Result, 0, 0);", transaction: trans);

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
