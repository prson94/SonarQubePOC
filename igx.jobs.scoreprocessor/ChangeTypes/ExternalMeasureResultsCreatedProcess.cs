using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using d360.utils.company;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    internal class AssetAllocationPreviousResult
    {
        public Guid AssetUid { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public Guid AllocationUid { get; set; }
        public int AssetTypeId { get; set; }
        public DateTime? PreviousEffectiveDate { get; set; }
        public bool? PreviousResult { get; set; }
    }

    internal class AllocationDataModel
    {
        public Guid AllocationUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid MetricParentAssetUid { get; set; }
        public bool IsGroup { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public float Weight { get; set; }
        public float Threshold { get; set; }
        public bool MatchConditionsOnly { get; set; }
        public string Definition { get; set; }
        public string ConditionsJson { get; set; }
        public List<AllocationDataModelCondition> Conditions { get { return JsonConvert.DeserializeObject<List<AllocationDataModelCondition>>(ConditionsJson ?? "[]"); } }
    }
    internal class AllocationDataModelCondition
    {
        public Guid ConditionUid { get; set; }
        public MetricMatchType MatchType { get; set; }
        public int Position { get; set; }
        public float Weight { get; set; }
        public float Threshold { get; set; }
        public List<AllocationDataModelConditionItem> Items { get; set; }
    }

    internal class AllocationDataModelConditionItem
    {
        public Guid ItemUid { get; set; }
        public MetricConditionType ConditionType { get; set; }
        public int? ConditionFieldTypeID { get; set; }
        public int? ConditionIntersectTypeID { get; set; }
        public string Operator { get; set; }
        public List<AllocationDataModelConditionItemValue> Values { get; set; }
    }

    internal class AllocationDataModelConditionItemValue
    {
        public string Value { get; set; }
    }

    public class ExternalMeasureResultsCreatedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var json = Storage.GetFileContentsAsString(Info.StorageFolder, Info.StorageFile);
            var models = JsonConvert.DeserializeObject<List<ExternalMeasureResultsCreatedModel>>(json);

            using (var company = GetEnvironmentConnection())
            {
                // Load assets to a temporary table to get the list of asset types with all associated measures for the specific effective date.
                var assets = new DataTable();
                assets.Columns.Add("AssetUid", typeof(Guid));
                assets.Columns.Add("MetricAssetUid", typeof(Guid));
                assets.Columns.Add("EffectiveDate", typeof(DateTime));
                assets.Columns.Add("Result", typeof(bool));

                foreach (var model in models)
                {
                    var assetRow = assets.NewRow();
                    assetRow["AssetUid"] = model.AssetUid;
                    assetRow["MetricAssetUid"] = model.MetricAssetUid;
                    assetRow["EffectiveDate"] = model.EffectiveDate.Date;
                    assetRow["Result"] = model.Result;
                    assets.Rows.Add(assetRow);
                }

                List<AllocationDataModel> allocations = null;
                List<FieldType> fieldTypes = null;
                List<AssetAllocationPreviousResult> existingScoreItems = null;

                if (company.State != ConnectionState.Open)
                    company.Open();

                using (var trans = company.BeginTransaction())
                {
                    // Load all the measures to get the appropriate version, with the full set of conditions that we should check.

                    #region Populate models with relevant details

                    await company.ExecuteAsync(
                        "create table #AssetAllocations (" +
                            "AssetUid uniqueidentifier not null, " +
                            "EffectiveDate date not null, " +
                            "MetricAssetUid uniqueidentifier not null, " +
                            "Result bit not null, " +
                            "MetricAssetVersionUid uniqueidentifier null, " +
                            "AllocationUid uniqueidentifier null, " +
                            "AssetTypeId int null, " +
                            "PreviousEffectiveDate date null, " +
                            "PreviousResult bit null " +
                        ")", 
                        transaction: trans
                        );

                    var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = assets.Rows.Count,
                        DestinationTableName = "#AssetAllocations",
                        BulkCopyTimeout = 3600
                    };

                    bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                    bulkCopy.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
                    bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                    bulkCopy.ColumnMappings.Add("Result", "Result");

                    await bulkCopy.WriteToServerAsync(assets);
                    bulkCopy = null;

                    // Figure out which allocations we are dealing with.
                    await company.ExecuteAsync(
                        "update T " +
                        "set T.AllocationUid = S.AllocationUid, " +
                        "T.AssetTypeId = A.ID " +
                        "from #AssetAllocations T " +
                        "inner join metrics.Asset S on S.Uid = T.MetricAssetUid " +
                        "inner join metrics.Allocation Al on Al.Uid = S.AllocationUid " +
                        "inner join AssetType A on A.Uid = Al.AssetTypeUid", 
                        transaction: trans
                        );
                    
                    // Figure out which measure versions we are dealing with.
                    await company.ExecuteAsync(
                        "update T " +
                        "set T.MetricAssetVersionUid = S.Uid " +
                        "from #AssetAllocations T " +
                        "inner join metrics.AssetVersion S on S.AssetUid = T.MetricAssetUid " +
                        "and ( (T.EffectiveDate between S.EffectiveDate and S.EffectiveEndDate) or (T.EffectiveDate >= S.EffectiveDate and S.EffectiveEndDate is null) )",
                        transaction: trans
                        );

                    #endregion

                    var filledModels = await company.QueryAsync<ExternalMeasureResultsCreatedModel>("select * from #AssetAllocations", transaction: trans);
                    models = filledModels.ToList();

                    var existingScoreItemsRequest = await company.QueryAsync<AssetAllocationPreviousResult>(@"
select	Al.AssetUid,
		V.AssetUid as MetricAssetUid,
		V.Uid as MetricAssetVersionUid,
		Al.AllocationUid,
		Al.AssetTypeId,
		P.EffectiveDate as PreviousEffectiveDate,
		P.Value as PreviousResult
from	metrics.Asset A
		inner join metrics.AssetVersion V on V.AssetUid = A.Uid
		inner join (
			select		AllocationUid, AssetUid, EffectiveDate, AssetTypeId
			from		#AssetAllocations		
			group by	AllocationUid, AssetUid, EffectiveDate, AssetTypeId
		) Al on Al.AllocationUid = A.AllocationUid and ( (Al.EffectiveDate between V.EffectiveDate and V.EffectiveEndDate) or (Al.EffectiveDate >= V.EffectiveDate and V.EffectiveEndDate is null) ) 
		outer apply (
			select		MetricAssetUid,
						AssetUid,
						max(EffectiveDate) as EffectiveDate
			from		metrics.ScoreItem
			where		AssetUid = Al.AssetUid
						and MetricAssetUid = A.Uid
						and EffectiveDate <= Al.EffectiveDate
			group by	MetricAssetUid, AssetUid
		) M
		left join metrics.ScoreItem P on P.MetricAssetUid = M.MetricAssetUid and P.AssetUid = M.AssetUid and P.EffectiveDate = M.EffectiveDate", transaction: trans);
                    existingScoreItems = existingScoreItemsRequest.ToList();

                    // Get the full list of relevant measures based on the allocations and effective dates.
                    var allocationRequest = await company.QueryAsync<AllocationDataModel>(@"
select	Al.AllocationUid,
		Al.EffectiveDate,
		V.AssetUid as MetricAssetUid,
		A.ParentUid as MetricParentAssetUid,
		A.IsGroup,
		V.Uid as MetricAssetVersionUid,
		V.[Weight],
		V.Threshold,
		V.MatchConditionsOnly,
		V.[Definition],
		(
			select	C.Uid as ConditionUid,
					C.MatchType,
					C.Position,
					C.Threshold,
					C.Weight,
					(
					select	I.Uid as ItemUid,
							I.ConditionType,
							I.ConditionFieldTypeID,
							I.ConditionIntersectTypeID,
							I.Operator,
							(
							select	V.[Value]
							from	[metrics].[AssetVersionConditionItemValue] V
							where	V.Uid = I.Uid
							for json path
							) as [Values]
					from	[metrics].[AssetVersionConditionItem] I
					where	AssetVersionConditionUid = C.Uid
					for json path
					) as Items
			from	[metrics].[AssetVersionCondition] C
			where	C.AssetVersionUid = V.Uid
			order by C.Position asc
			for json path
		) as ConditionsJson
from	metrics.Asset A
		inner join metrics.AssetVersion V on V.AssetUid = A.Uid
		inner join (
			select		AllocationUid, EffectiveDate
			from		#AssetAllocations		
			group by	AllocationUid, EffectiveDate
		) Al on Al.AllocationUid = A.AllocationUid and ( (Al.EffectiveDate between V.EffectiveDate and V.EffectiveEndDate) or (Al.EffectiveDate >= V.EffectiveDate and V.EffectiveEndDate is null) )", transaction: trans);
                    allocations = allocationRequest.ToList();

                    var fieldTypesRequest = await company.QueryAsync<FieldType>(
                        "select * from FieldType where AssetTypeID in (select AssetTypeID from #AssetAllocations group by AssetTypeID)", transaction: trans);
                    fieldTypes = fieldTypesRequest.ToList();

                    trans.Commit();
                }

                var scoreResults = new List<Score>();
                var scoreItemResults = new List<ScoreItem>();
                var uniqueAssetCombinations = models.Select(i => new { i.AllocationUid, i.AssetUid, i.EffectiveDate }).Distinct().ToList();
                uniqueAssetCombinations.ForEach(assetEffectiveDate =>
                {
                    Score assetScoreResult = null;
                    var assetScoreItemResults = new List<ScoreItem>();

                    var allMeasures = allocations.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var providedMeasureResults = models.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.AssetUid == assetEffectiveDate.AssetUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var assetFields = company.Query<FieldDetail>("select F.* from FieldDetail F inner join Asset A on A.ID = F.AssetID and A.Uid = @AssetUid", new { assetEffectiveDate.AssetUid }).ToList();

                    providedMeasureResults.ForEach(n =>
                    {
                        var measure = allMeasures.FirstOrDefault(i => i.MetricAssetVersionUid == n.MetricAssetVersionUid);
                        if (measure != null)
                        {
                            var conditionMet = (measure.Conditions.Count == 0);
                            int? positionForMetCondition = null;
                            
                            if (measure.Conditions.Count > 0)
                            {
                                measure.Conditions.ForEach(c => {

                                    if (!positionForMetCondition.HasValue)
                                    {
                                        int conditionsMetCount = 0;

                                        c.Items.ForEach(i =>
                                        {
                                            var assetField = assetFields.SingleOrDefault(f => f.FieldTypeID == i.ConditionFieldTypeID);
                                            var fieldType = fieldTypes.SingleOrDefault(f => f.ID == i.ConditionFieldTypeID);
                                            if (assetField != null && fieldType != null)
                                            {
                                                if (fieldType.Type == DataType.Lookup.ToString() && fieldType.AllowMultipleValues)
                                                {
                                                    var fieldValues = assetField.Value.Split(',');
                                                    var conditionValues = i.Values.Select(o => o.Value).ToList();
                                                    if (i.ConditionType == MetricConditionType.And)
                                                    {
                                                        if (i.Operator == "neq")
                                                        {
                                                            int conditionValueCountMet = conditionValues.Count;
                                                            conditionValues.ForEach(cv =>
                                                            {
                                                                if (!fieldValues.Any(fv => fv == cv))
                                                                {
                                                                    conditionValueCountMet--;
                                                                }
                                                            });
                                                            if (conditionValueCountMet == 0) // No condition values met by field, which for "neq" is a good thing.
                                                            {
                                                                conditionsMetCount++;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            int conditionValueCountMet = 0;
                                                            conditionValues.ForEach(cv =>
                                                            {
                                                                if (fieldValues.Any(fv => fv == cv))
                                                                {
                                                                    conditionValueCountMet++;
                                                                }
                                                            });
                                                            if (conditionValueCountMet == conditionValues.Count) // All condition values met by field.
                                                            {
                                                                conditionsMetCount++;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (i.Operator == "neq")
                                                        {
                                                            // This NEQ logic is the same as above. Let's just get the logic right first before optimizing.
                                                            int conditionValueCountMet = conditionValues.Count;
                                                            conditionValues.ForEach(cv =>
                                                            {
                                                                if (!fieldValues.Any(fv => fv == cv))
                                                                {
                                                                    conditionValueCountMet--;
                                                                }
                                                            });
                                                            if (conditionValueCountMet == 0) // No condition values met by field, which for "neq" is a good thing.
                                                            {
                                                                conditionsMetCount++;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (conditionValues.Intersect(fieldValues).Any())
                                                            {
                                                                conditionsMetCount++;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    switch (i.Operator)
                                                    {
                                                        case "eq":
                                                            if (assetField.Value == i.Values[0].Value)
                                                            {
                                                                conditionsMetCount++;
                                                            }
                                                            break;
                                                        case "neq":
                                                            if (assetField.Value != i.Values[0].Value)
                                                            {
                                                                conditionsMetCount++;
                                                            }
                                                            break;
                                                        case "gt":
                                                        case "gte":
                                                        case "lt":
                                                        case "lte":
                                                            dynamic conditionValue;
                                                            dynamic fieldValue;
                                                            if (fieldType.Type == DataType.Boolean.ToString())
                                                            {
                                                                conditionValue = bool.Parse(i.Values[0].Value);
                                                                fieldValue = bool.Parse(assetField.Value);
                                                            }
                                                            else if (fieldType.Type == DataType.Date.ToString() || fieldType.Type == DataType.DateTime.ToString())
                                                            {
                                                                conditionValue = DateTime.Parse(i.Values[0].Value);
                                                                fieldValue = DateTime.Parse(assetField.Value);
                                                            }
                                                            else if (fieldType.Type == DataType.Decimal.ToString())
                                                            {
                                                                conditionValue = decimal.Parse(i.Values[0].Value);
                                                                fieldValue = decimal.Parse(assetField.Value);
                                                            }
                                                            else if (fieldType.Type == DataType.Number.ToString())
                                                            {
                                                                conditionValue = int.Parse(i.Values[0].Value);
                                                                fieldValue = int.Parse(assetField.Value);
                                                            }
                                                            else
                                                            {
                                                                conditionValue = i.Values[0].Value;
                                                                fieldValue = assetField.Value;
                                                            }

                                                            switch (i.Operator)
                                                            {
                                                                case "gt":
                                                                    if (fieldValue > conditionValue)
                                                                    {
                                                                        conditionsMetCount++;
                                                                    }
                                                                    break;
                                                                case "gte":
                                                                    if (fieldValue >= conditionValue)
                                                                    {
                                                                        conditionsMetCount++;
                                                                    }
                                                                    break;
                                                                case "lt":
                                                                    if (fieldValue < conditionValue)
                                                                    {
                                                                        conditionsMetCount++;
                                                                    }
                                                                    break;
                                                                case "lte":
                                                                    if (fieldValue <= conditionValue)
                                                                    {
                                                                        conditionsMetCount++;
                                                                    }
                                                                    break;
                                                            }
                                                            break;
                                                    }
                                                }
                                            }
                                        });

                                        if (c.MatchType == MetricMatchType.All)
                                        {
                                            conditionMet = (conditionsMetCount == c.Items.Count);
                                        }
                                        else
                                        {
                                            conditionMet = (conditionsMetCount > 0);
                                        }

                                        if (conditionMet)
                                        {
                                            positionForMetCondition = c.Position;
                                        }
                                    }

                                });
                            }

                            float? weight = null;
                            if (conditionMet)
                            {
                                if (positionForMetCondition.HasValue)
                                {
                                    weight = measure.Conditions.First(i => i.Position == positionForMetCondition.Value).Weight;
                                    if (weight == 0)
                                    {
                                        weight = null;
                                    }
                                }
                                
                                // Set the measure weight as the default.
                                if (!weight.HasValue)
                                {
                                    weight = measure.Weight;
                                }
                            }
                            else
                            {
                                if (!measure.MatchConditionsOnly)
                                {
                                    weight = measure.Weight;
                                }
                            }

                            if (weight.HasValue) // Then we should be creating this score result (a conditon was met, or does not need to be met except to override weight)
                            {
                                //TODO: Do we look up and adjust a prior score item here as well?
                                var scoreItem = new ScoreItem {
                                    AdjustedWeight = weight, // this is the measure/condition weight, which will need to be re-adjusted at the end.
                                    AssetUid = assetEffectiveDate.AssetUid,
                                    MetricAssetUid = measure.MetricAssetUid,
                                    AssetVersionUid = measure.MetricAssetVersionUid,
                                    EffectiveDate = assetEffectiveDate.EffectiveDate,
                                    RunDate = DateTime.UtcNow,
                                    UpdatedOn = DateTime.UtcNow,
                                    BooleanResult = n.Result
                                };
                                assetScoreItemResults.Add(scoreItem);
                            }
                        }
                    });

                    // Perform final score calculations for this asset/effective date combination.
                    if (assetScoreItemResults.Count > 0)
                    {
                        var missingMeasures = existingScoreItems.Where(p => p.AssetUid == assetEffectiveDate.AssetUid).ToList();
                        assetScoreResult = new Score();
                        // assetScoreItemResults
                    }
                });
            }
        }
    }
}
