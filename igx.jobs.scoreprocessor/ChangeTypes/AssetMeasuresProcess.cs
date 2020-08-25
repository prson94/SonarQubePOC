using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
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
    public class AssetMeasuresProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var assetMeasures = await Storage.DeserializeJsonObjectFromBlobAsync<List<AssetMeasureModel>>(Info.StorageFolder, Info.StorageFile);

            var scoresToAdd = new List<Score>();
            var scoresItemsToAdd = new List<ScoreItem>();
            var scoreItemLinksToAdd = new List<ScoreItemLink>();

            var Db = GetCompanyContext();
            using (var company = GetEnvironmentConnection())
            {
                // Load assets to a temporary table to get the list of asset types with all associated measures for the specific effective date.
                var assets = new DataTable();
                assets.Columns.Add("AssetUid", typeof(Guid));
                assets.Columns.Add("MetricAssetUid", typeof(Guid));
                assets.Columns.Add("MetricAssetVersionUid", typeof(Guid));
                assets.Columns.Add("EffectiveDate", typeof(DateTime));
                assets.Columns.Add("Result", typeof(bool));

                var rawAssetMeasures = from a in assetMeasures
                             from m in a.Measures
                             select new 
                             {
                                 a.AssetUid,
                                 a.EffectiveDate,
                                 m.MetricAssetUid,
                                 m.MetricAssetVersionUid,
                                 m.Result
                             };
                foreach (var model in rawAssetMeasures)
                {
                    var assetRow = assets.NewRow();
                    assetRow["AssetUid"] = model.AssetUid;
                    assetRow["MetricAssetUid"] = model.MetricAssetUid;
                    if (model.MetricAssetVersionUid.HasValue) assetRow["MetricAssetVersionUid"] = model.MetricAssetVersionUid;
                    assetRow["EffectiveDate"] = model.EffectiveDate.Date;
                    if(model.Result.HasValue) assetRow["Result"] = model.Result;
                    assets.Rows.Add(assetRow);
                }

                List<AllocationDataModel> allocations = null;
                List<FieldType> fieldTypes = null;
                List<AssetAllocationPreviousResult> allPreviousScoreItems = null;
                List<MatchingScoreModel> matchingScores = null;
                List<MatchingScoreItemModel> matchingScoreItems = null;
                List<ExternalMeasureResultsCreatedModel> models = null;

                if (company.State != ConnectionState.Open)
                    company.Open();

                using (var trans = company.BeginTransaction())
                {
                    #region Populate models with relevant details

                    await company.ExecuteAsync(@"create table #AssetAllocations (
                            AssetUid uniqueidentifier not null,
                            EffectiveDate date not null,
                            MetricAssetUid uniqueidentifier not null,
                            Result bit null,
                            MetricAssetVersionUid uniqueidentifier null,
                            AllocationUid uniqueidentifier null,
                            AssetTypeId int null
                        )", transaction: trans);

                    using (var bulkCopy = CreateBulkCopy(company, trans, "#AssetAllocations"))
                    {
                        bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                        bulkCopy.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
                        bulkCopy.ColumnMappings.Add("MetricAssetVersionUid", "MetricAssetVersionUid");
                        bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                        bulkCopy.ColumnMappings.Add("Result", "Result");

                        await bulkCopy.WriteToServerAsync(assets);
                    }

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
                        "inner join metrics.AssetVersion S on S.AssetUid = T.MetricAssetUid and T.MetricAssetVersionUid is null " +
                        "and ( (T.EffectiveDate between S.EffectiveDate and S.EffectiveEndDate) or (T.EffectiveDate >= S.EffectiveDate and S.EffectiveEndDate is null) )",
                        transaction: trans
                        );

                    #endregion

                    var supportingDataRequest = await company.QueryMultipleAsync(@"
select * from #AssetAllocations;

select * from FieldType where AssetTypeID in (select AssetTypeID from #AssetAllocations group by AssetTypeID);

select  Al.AllocationUid,
        Al.AssetUid,
		V.AssetUid as MetricAssetUid,
		L.*,
        Si.AssetVersionUid as MetricAssetVersionUid,
		Si.ConditionUid,
        S.EffectiveDate,
		Si.Value,
		Si.AdjustedWeight,
		Si.AdjustedMaxWeight
from    (
			select		AllocationUid, AssetUid, EffectiveDate, AssetTypeId
			from		#AssetAllocations		
			group by	AllocationUid, AssetUid, EffectiveDate, AssetTypeId
		) Al
        inner join metrics.Score S on S.AllocationUid = Al.AllocationUid 
            and S.AssetUid = Al.AssetUid 
            and ( (Al.EffectiveDate between S.EffectiveDate and S.EndDate) or (Al.EffectiveDate >= S.EffectiveDate and S.EndDate is null) ) 
        inner join metrics.ScoreItemLink L on L.ScoreUid = S.Uid
        inner join metrics.ScoreItem Si on Si.Uid = L.ScoreItemUid
		inner join metrics.AssetVersion V on V.Uid = Si.AssetVersionUid;

select  S.Uid as ScoreUid,
        Al.AllocationUid,
        Al.AssetUid,
        Al.EffectiveDate
from    (
			select		AllocationUid, AssetUid, EffectiveDate, AssetTypeId
			from		#AssetAllocations		
			group by	AllocationUid, AssetUid, EffectiveDate, AssetTypeId
		) Al
        inner join metrics.Score S on S.AllocationUid = Al.AllocationUid and S.AssetUid = Al.AssetUid and S.EffectiveDate = Al.EffectiveDate;

select  distinct 
        L.ScoreItemUid,
        V.AssetUid as MetricAssetUid,
        Al.AssetUid,
        Al.EffectiveDate
from    (
			select		AllocationUid, AssetUid, EffectiveDate, AssetTypeId
			from		#AssetAllocations		
			group by	AllocationUid, AssetUid, EffectiveDate, AssetTypeId
		) Al
        inner join metrics.Score S on S.AllocationUid = Al.AllocationUid and S.AssetUid = Al.AssetUid and S.EffectiveDate = Al.EffectiveDate
        inner join metrics.ScoreItemLink L on L.ScoreUid = S.Uid
        inner join metrics.ScoreItem I on I.Uid = L.ScoreItemUid
        inner join metrics.AssetVersion V on V.Uid = I.AssetVersionUid", transaction: trans, commandTimeout: 900);
                    models = supportingDataRequest.Read<ExternalMeasureResultsCreatedModel>().ToList();
                    fieldTypes = supportingDataRequest.Read<FieldType>().ToList();
                    allPreviousScoreItems = supportingDataRequest.Read<AssetAllocationPreviousResult>().ToList();
                    matchingScores = supportingDataRequest.Read<MatchingScoreModel>().ToList();
                    matchingScoreItems = supportingDataRequest.Read<MatchingScoreItemModel>().ToList();

                    // Get the full list of relevant measures based on the allocations and effective dates.
                    var allocationRequest = await company.QueryAsync<AllocationDataModel>(@"
select	Al.AllocationUid,
		Mal.ScoreType,
        Mal.CalculationMethod,
        Mal.IsThresholdBased,
        Al.EffectiveDate,
		V.AssetUid as MetricAssetUid,
		A.ParentUid as MetricParentAssetUid,
		A.IsGroup,
		V.Uid as MetricAssetVersionUid,
		V.[Weight],
		V.Threshold,
		V.MatchConditionsOnly,
		V.[Definition],
        V.EffectiveEndDate,
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
		) as ConditionsJson,
		(
			select	C.Uid as AssetVersionRollupPathUid,
					C.FilterMatchType,
					(
					select	I.IntersectTypeID,
                            P.[Type] as PredicateType,
							I.StartPosition,
                            S.AssetTypeID as StartAssetTypeID,
                            SA.Class as StartClass,
                            I.EndPosition,
                            E.AssetTypeID as EndAssetTypeID,
                            EA.Class as EndClass
					from	[metrics].[RollupPathLink] I
                            inner join [metrics].[RollupPathSegment] S on S.RollupPathUid = I.RollupPathUid and S.Position = I.StartPosition
                            inner join AssetType SA on SA.ID = S.AssetTypeID
                            inner join [metrics].[RollupPathSegment] E on E.RollupPathUid = I.RollupPathUid and E.Position = I.EndPosition
                            inner join AssetType EA on EA.ID = E.AssetTypeID
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID
					where	I.RollupPathUid = C.RollupPathUid
                    order by I.StartPosition, I.EndPosition
					for json path
					) as SegmentLinks,
					(
					select	I.Uid as AssetVersionRollupPathFilterUid,
							I.AssetTypeID,
							I.FieldTypeID,
							I.Operator,
							(
							select	V.[Value]
							from	[metrics].[AssetVersionRollupPathFilterValue] V
							where	V.AssetVersionRollupPathFilterUid = I.Uid
							for json path
							) as [Values]
					from	[metrics].[AssetVersionRollupPathFilter] I
					where	AssetVersionRollupPathUid = C.Uid
					for json path
					) as Filters
			from	[metrics].[AssetVersionRollupPath] C
                    inner join [metrics].[RollupPath] RP on RP.Uid = C.RollupPathUid and RP.State = 1 and C.AssetVersionUid = V.Uid
			for json path, WITHOUT_ARRAY_WRAPPER
		) as RollupPathJson
from	metrics.Asset A
		inner join metrics.AssetVersion V on V.AssetUid = A.Uid
        inner join metrics.Allocation Mal on Mal.Uid = A.AllocationUid
		inner join (
			select		AllocationUid, EffectiveDate
			from		#AssetAllocations		
			group by	AllocationUid, EffectiveDate
		) Al on Al.AllocationUid = A.AllocationUid and ( (Al.EffectiveDate between V.EffectiveDate and V.EffectiveEndDate) or (Al.EffectiveDate >= V.EffectiveDate and V.EffectiveEndDate is null) )", transaction: trans);
                    allocations = allocationRequest.ToList();

                    trans.Commit();
                }

                var scoreResults = new List<Score>();
                var scoreItemResults = new List<ScoreItem>();
                var uniqueAssetCombinations = models
                    .Where(i => i.AllocationUid.HasValue)
                    .Select(i => new { 
                        AllocationUid = i.AllocationUid.Value, 
                        i.AssetTypeId, 
                        i.AssetUid, 
                        i.EffectiveDate 
                    })
                    .Distinct()
                    .ToList();
                uniqueAssetCombinations.ForEach(assetEffectiveDate =>
                {
                    // The local lists below keep track of score items and links to add for a specific score (asset / effective date / allocation combination).
                    var assetScoreItems = new List<ScoreItem>();
                    var assetScoreItemLinks = new List<ScoreItemLink>();

                    var allMeasures = allocations.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var providedMeasureResults = models.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.AssetUid == assetEffectiveDate.AssetUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var assetFields = company.Query<FieldDetail>("select F.* from FieldDetail F inner join Asset A on A.ID = F.AssetID and A.Uid = @AssetUid", new { assetEffectiveDate.AssetUid }).ToList();
                    var previousScoreItems = allPreviousScoreItems.Where(p => p.AssetUid == assetEffectiveDate.AssetUid).ToList();
                    
                    providedMeasureResults.ForEach(async n =>
                    {
                        var measure = allMeasures.FirstOrDefault(i => i.MetricAssetVersionUid == n.MetricAssetVersionUid);
                        if (measure != null)
                        {
                            var conditionValidator = CheckMeasureConditions(assetFields, fieldTypes, measure);

                            if (conditionValidator.ConditionMet) // Then we should be creating this score result (a conditon was met, or does not need to be met except to override weight)
                            {
                                var scoreItem = new ScoreItem
                                {
                                    RawMeasureWeight = conditionValidator.SelectedWeight, // this is the measure/condition weight, which will need to be re-adjusted at the end.
                                    MetricAssetUid = measure.MetricAssetUid,
                                    AssetVersionUid = measure.MetricAssetVersionUid,
                                    RunDate = DateTime.UtcNow,
                                    UpdatedOn = DateTime.UtcNow,
                                    ConditionUid = conditionValidator.SelectedConditionUid
                                };

                                // Now perform analysis based on measure type and check type.
                                switch (measure.ScoreType)
                                {
                                    case ScoreType.DataQuality:
                                        #region
                                        var dqDefinition = JsonConvert.DeserializeObject<DataQualityMeasureDefinition>(measure.Definition);
                                        // Do something with rollups here.
                                        if (measure.RollupPath == null)
                                        {
                                            scoreItem.Value = false;
                                            scoreItem.Evidence = "{ IsError: true, ErrorMessage: \"Rollup Path is invalid. An asset type or relationship type may have been removed.\" }";
                                        }
                                        else
                                        {
                                            var columnSql = "select ROW_NUMBER() over (partition by RE2.$from_id, RE1.$from_id order by R.UpdatedOn desc) as RowNumber, R.Uid, R.PassFraction ";
                                            var tableSql = "from ";
                                            var whereSql = "";
                                            var matchQuery = "";
                                            measure.RollupPath.SegmentLinks.ForEach(s =>
                                            {
                                                if (s.PredicateType == PredicateType.Evaluation)    // This is the last relationship in the chain.
                                                {
                                                    matchQuery += $"<-(E{s.IntersectTypeID})-N{s.EndAssetTypeID} AND  N{s.StartAssetTypeID}-(RE2)->R<-(RE1)-N{s.EndAssetTypeID}";
                                                    tableSql += $"graph.AssetEdge E{s.IntersectTypeID}, graph.AssetNode N{s.EndAssetTypeID}, dbo.AssetResultEdge RE1, dbo.AssetResultEdge RE2, dbo.AssetResult R ";
                                                    whereSql += $" and N{s.EndAssetTypeID}.AssetTypeID = {s.EndAssetTypeID} and E{s.IntersectTypeID}.IntersectTypeID = {s.IntersectTypeID} and E{s.IntersectTypeID}.PredicateType = {(int)s.PredicateType} ";
                                                }
                                                else 
                                                {
                                                    matchQuery = matchQuery.Replace($"N{s.StartAssetTypeID}", "") +
                                                                 $"N{s.StartAssetTypeID}-(E{s.IntersectTypeID})->N{s.EndAssetTypeID}";

                                                    tableSql = tableSql.Replace($"graph.AssetNode N{s.StartAssetTypeID}, ", "") +
                                                               $"graph.AssetNode N{s.StartAssetTypeID}, graph.AssetEdge E{s.IntersectTypeID}, graph.AssetNode N{s.EndAssetTypeID}, ";

                                                    whereSql = whereSql.Replace($"and N{s.StartAssetTypeID}.AssetTypeID = {s.StartAssetTypeID}", "") +
                                                               $"and N{s.StartAssetTypeID}.AssetTypeID = {s.StartAssetTypeID} and E{s.IntersectTypeID}.IntersectTypeID = {s.IntersectTypeID} and N{s.EndAssetTypeID}.AssetTypeID = {s.EndAssetTypeID}";
                                                }
                                            });
                                            whereSql += $"and N{assetEffectiveDate.AssetTypeId}.Uid = @AssetUid";
                                            if (measure.EffectiveEndDate.HasValue)
                                            {
                                                whereSql += $"and R.EffectiveDate <= @EffectiveEndDate";
                                            }

                                            var sql = $"select	Uid, PassFraction from({ columnSql} {tableSql} where match({matchQuery}) {whereSql}) O where RowNumber = 1";
                                            var rollupPathResults = company.Query<RollupPathRuleResult>(sql, new { assetEffectiveDate.AssetUid, measure.EffectiveEndDate }).ToList();

                                            if (rollupPathResults.Count > 0)
                                            {
                                                float resultOperationValue = 0;
                                                switch (dqDefinition.ResultOperation)
                                                {
                                                    case MeasureResultOperation.Average:
                                                        resultOperationValue = rollupPathResults.Select(r => r.PassFraction).Average();
                                                        break;
                                                    case MeasureResultOperation.Max:
                                                        resultOperationValue = rollupPathResults.Select(r => r.PassFraction).Max();
                                                        break;
                                                    case MeasureResultOperation.Minimum:
                                                        resultOperationValue = rollupPathResults.Select(r => r.PassFraction).Min();
                                                        break;
                                                }

                                                if (measure.IsThresholdBased)
                                                {
                                                    scoreItem.Value = (measure.Threshold <= resultOperationValue);
                                                }
                                                else
                                                {
                                                    // This will be used when adjusting max and actual weights.
                                                    scoreItem.OverrideAdjustmentPercentage = resultOperationValue;
                                                }
                                            }
                                            else
                                            {
                                                scoreItem.Value = true;
                                            }

                                            scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = false, RuleResults = rollupPathResults.Select(r => r.Uid) });
                                        }

                                        break;
                                        #endregion
                                    case ScoreType.Governance:
                                        var gDefinition = JsonConvert.DeserializeObject<GovernanceMeasureDefinition>(measure.Definition);
                                        switch (gDefinition.Check)
                                        {
                                            case GovernanceMeasureCheck.External:
                                                scoreItem.Value = n.Result;
                                                break;
                                            case GovernanceMeasureCheck.Field:
                                                //assetFields.FirstOrDefault(f => f.FieldTypeID == gDefinition.TypeUid)
                                                //scoreItem.Value = n.Result;
                                                break;
                                            case GovernanceMeasureCheck.Ownership:
                                                //scoreItem.Value = n.Result;
                                                break;
                                            case GovernanceMeasureCheck.Predicate:
                                                //scoreItem.Value = n.Result;
                                                break;
                                            case GovernanceMeasureCheck.Relationship:
                                                //scoreItem.Value = n.Result;
                                                break;
                                        }
                                        break;
                                }

                                // Check to see if we have an existing scoreItem for this recalculated measure result.
                                var previousScoreItem = previousScoreItems.FirstOrDefault(e => e.MetricAssetVersionUid == measure.MetricAssetVersionUid);
                                scoreItem.Uid = (previousScoreItem != null) ? previousScoreItem.ScoreItemUid : Guid.NewGuid();

                                assetScoreItems.Add(scoreItem);
                                assetScoreItemLinks.Add(new ScoreItemLink { ScoreItemUid = scoreItem.Uid });
                            }
                        }
                    });

                    // Perform final score calculations for this asset/effective date combination. If no data for asset/effective date, then do not even bother to recalculate anything for it.
                    if (assetScoreItems.Count > 0)
                    {
                        allMeasures.ForEach(aM =>
                        {
                            // If no current results sent in for existing data load, then we need to carry forward the previous score items to create a complete score.
                            if (!assetScoreItems.Any(nI => nI.AssetVersionUid == aM.MetricAssetVersionUid))
                            {
                                // We need to add a previous result for the missing measure, or create a new one as a failure.
                                var conditionValidator = CheckMeasureConditions(assetFields, fieldTypes, aM);
                                if (conditionValidator.ConditionMet)
                                {
                                    // Look up to see if there is an existing score item for this measure, and use that value.
                                    var previousScoreItem = previousScoreItems.FirstOrDefault(e => e.MetricAssetUid == aM.MetricAssetUid);

                                    var scoreItem = new ScoreItem
                                    {
                                        MetricAssetUid = aM.MetricAssetUid,
                                        RawMeasureWeight = conditionValidator.SelectedWeight,
                                        RunDate = DateTime.UtcNow,
                                        AssetVersionUid = aM.MetricAssetVersionUid,
                                        ConditionUid = conditionValidator.SelectedConditionUid,
                                        UpdatedOn = DateTime.UtcNow,
                                        Value = (previousScoreItem != null) ? previousScoreItem.Value : false,
                                        Uid = (previousScoreItem != null) ? previousScoreItem.ScoreItemUid : Guid.NewGuid()
                                    };
                                    assetScoreItems.Add(scoreItem);
                                    assetScoreItemLinks.Add(new ScoreItemLink { ScoreItemUid = scoreItem.Uid });
                                }
                            }
                        });

                        var score = AdjustScoreItemWeights(allMeasures, assetScoreItems);
                        Score assetScore = new Score
                        {
                            EffectiveDate = assetEffectiveDate.EffectiveDate,
                            AllocationUid = assetEffectiveDate.AllocationUid,
                            AssetUid = assetEffectiveDate.AssetUid,
                            RunDate = DateTime.UtcNow,
                            Value = score
                        };
                        // If there is a macthing score in the system, update the Uid 
                        var matchingScore = matchingScores.FirstOrDefault(s => s.AllocationUid == assetEffectiveDate.AllocationUid && s.AssetUid == assetEffectiveDate.AssetUid && s.EffectiveDate == assetEffectiveDate.EffectiveDate);
                        assetScore.Uid = (matchingScore != null) ? matchingScore.ScoreUid : Guid.NewGuid();
                        //finalScoresToAdd.Add(assetScoreResult);
                        
                        // Update the links with the chosen score Uid.
                        assetScoreItemLinks.ForEach(l => {
                            l.ScoreUid = assetScore.Uid;
                        });


                        var assetScoreGroupUids = allMeasures.Where(i => i.IsGroup).Select(i => new { i.MetricAssetUid, i.MetricAssetVersionUid }).ToList();
                        assetScoreGroupUids.ForEach(g =>
                        {
                            if (assetScoreItems.Any(i => i.MetricAssetUid == g.MetricAssetUid))
                            {
                                // See if there are any child measures that we have. 
                                // If not, we need to remove this measure group as it is not relevant and we should not create an entry for it.
                                if (
                                    !(
                                    from am in allMeasures
                                    join si in assetScoreItems on am.MetricAssetUid equals si.MetricAssetUid
                                    where am.MetricParentAssetUid == g.MetricAssetUid
                                    select si
                                    ).Any()
                                    )
                                {
                                    assetScoreItems.RemoveAll(si => si.MetricAssetUid == g.MetricAssetUid);
                                }
                            }
                        });

                        // Now add to master collection which will be sent to database.
                        scoreItemLinksToAdd.AddRange(assetScoreItemLinks);
                        scoresItemsToAdd.AddRange(assetScoreItems);
                        scoresToAdd.Add(assetScore);
                    }
                });

                //Now add scores via a transaction.
                if (scoresToAdd.Count > 0)
                {
                    using (var trans = company.BeginTransaction())
                    {
                        try
                        {
                            var scores = new DataTable();
                            scores.Columns.Add("Uid", typeof(Guid));
                            scores.Columns.Add("AssetUid", typeof(Guid));
                            scores.Columns.Add("EffectiveDate", typeof(DateTime));
                            scores.Columns.Add("Value", typeof(decimal));
                            scores.Columns.Add("RunDate", typeof(DateTime));
                            scores.Columns.Add("EndDate", typeof(DateTime));
                            scores.Columns.Add("AllocationUid", typeof(Guid));

                            var scoreItems = new DataTable();
                            scoreItems.Columns.Add("Uid", typeof(Guid));
                            scoreItems.Columns.Add("UpdatedOn", typeof(DateTime));
                            scoreItems.Columns.Add("Value", typeof(bool));
                            scoreItems.Columns.Add("AdjustedWeight", typeof(decimal));
                            scoreItems.Columns.Add("RunDate", typeof(DateTime));
                            scoreItems.Columns.Add("AssetVersionUid", typeof(Guid));
                            scoreItems.Columns.Add("Evidence", typeof(string));
                            scoreItems.Columns.Add("ConditionUid", typeof(Guid));
                            scoreItems.Columns.Add("AdjustedMaxWeight", typeof(decimal));

                            var scoreItemLinks = new DataTable();
                            scoreItemLinks.Columns.Add("ScoreUid", typeof(Guid));
                            scoreItemLinks.Columns.Add("ScoreItemUid", typeof(Guid));

                            scoresToAdd.ForEach(s =>
                            {
                                var scoreRow = scores.NewRow();
                                scoreRow["Uid"] = s.Uid;
                                scoreRow["AssetUid"] = s.AssetUid;
                                scoreRow["EffectiveDate"] = s.EffectiveDate.Date;
                                scoreRow["Value"] = s.Value;
                                scoreRow["RunDate"] = s.RunDate;
                                scoreRow["AllocationUid"] = s.AllocationUid;
                                scores.Rows.Add(scoreRow);
                            });

                            scoresItemsToAdd.ForEach(s =>
                            {
                                var scoreItemRow = scoreItems.NewRow();
                                scoreItemRow["Uid"] = s.Uid;
                                scoreItemRow["UpdatedOn"] = s.UpdatedOn;
                                scoreItemRow["Value"] = s.Value;
                                if (s.AdjustedWeight.HasValue)
                                    scoreItemRow["AdjustedWeight"] = s.AdjustedWeight.Value;
                                scoreItemRow["RunDate"] = s.RunDate;
                                scoreItemRow["AssetVersionUid"] = s.AssetVersionUid;
                                scoreItemRow["Evidence"] = s.Evidence ?? "{}";
                                if (s.ConditionUid.HasValue)
                                    scoreItemRow["ConditionUid"] = s.ConditionUid;
                                if (s.AdjustedMaxWeight.HasValue)
                                    scoreItemRow["AdjustedMaxWeight"] = s.AdjustedMaxWeight.Value;
                                scoreItems.Rows.Add(scoreItemRow);
                            });

                            scoreItemLinksToAdd.ForEach(s =>
                            {
                                var scoreRow = scoreItemLinks.NewRow();
                                scoreRow["ScoreUid"] = s.ScoreUid;
                                scoreRow["ScoreItemUid"] = s.ScoreItemUid;
                                scoreItemLinks.Rows.Add(scoreRow);
                            });

                            await company.ExecuteAsync(
                               @"create table #Scores (
                                    Uid uniqueidentifier not null,
                                    AssetUid uniqueidentifier not null,
                                    EffectiveDate date not null,
                                    Value decimal(5,3) null,
                                    RunDate datetime not null,
                                    EndDate date null,
                                    AllocationUid uniqueidentifier null
                                );
                                create table #ScoreItems (
	                                Uid uniqueidentifier NOT NULL,
	                                UpdatedOn datetime NOT NULL,
	                                Value bit NOT NULL,
	                                AdjustedWeight decimal(5, 3) NULL,
	                                RunDate datetime NULL,
	                                AssetVersionUid uniqueidentifier NULL,
	                                Evidence nvarchar(max) NULL,
	                                ConditionUid uniqueidentifier NULL,
	                                AdjustedMaxWeight decimal(5, 3) NULL
                                );
                                create table #ScoreItemLinks (
                                    ScoreUid uniqueidentifier NOT NULL,
	                                ScoreItemUid uniqueidentifier NOT NULL
                                );", transaction: trans);

                            using (var bulkCopy = CreateBulkCopy(company, trans, "#Scores"))
                            { 
                                bulkCopy.ColumnMappings.Add("Uid", "Uid");
                                bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                                bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                                bulkCopy.ColumnMappings.Add("Value", "Value");
                                bulkCopy.ColumnMappings.Add("RunDate", "RunDate");
                                bulkCopy.ColumnMappings.Add("EndDate", "EndDate");
                                bulkCopy.ColumnMappings.Add("AllocationUid", "AllocationUid");

                                await bulkCopy.WriteToServerAsync(scores);                           
                            }

                            using (var bulkCopy = CreateBulkCopy(company, trans, "#ScoreItems"))
                            {
                                bulkCopy.ColumnMappings.Add("Uid", "Uid");
                                bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");
                                bulkCopy.ColumnMappings.Add("Value", "Value");
                                bulkCopy.ColumnMappings.Add("AdjustedWeight", "AdjustedWeight");
                                bulkCopy.ColumnMappings.Add("RunDate", "RunDate");
                                bulkCopy.ColumnMappings.Add("AssetVersionUid", "AssetVersionUid");
                                bulkCopy.ColumnMappings.Add("Evidence", "Evidence");
                                bulkCopy.ColumnMappings.Add("ConditionUid", "ConditionUid");
                                bulkCopy.ColumnMappings.Add("AdjustedMaxWeight", "AdjustedMaxWeight");

                                await bulkCopy.WriteToServerAsync(scoreItems);
                            }

                            using (var bulkCopy = CreateBulkCopy(company, trans, "#ScoreItemLinks"))
                            {
                                bulkCopy.ColumnMappings.Add("ScoreUid", "ScoreUid");
                                bulkCopy.ColumnMappings.Add("ScoreItemUid", "ScoreItemUid");

                                await bulkCopy.WriteToServerAsync(scoreItemLinks);
                            }

                            // End-date earlier scores and score items.
                            await company.ExecuteAsync("update T " +
                                "set T.EndDate = DATEADD(d, -1, S.EffectiveDate) " +
                                "from metrics.Score T " +
                                "inner join #Scores S on S.AllocationUid = T.AllocationUid and S.AssetUid = T.AssetUid and S.EffectiveDate > T.EffectiveDate and T.EndDate is null", transaction: trans);

                            // End-date new scores and score items IF the effective date is not the latest effective date.
                            await company.ExecuteAsync("update T " +
                                "set T.EndDate = DATEADD(d, -1, S.EffectiveDate) " +
                                "from #Scores T " +
                                "cross apply (select min(EffectiveDate) as EffectiveDate from metrics.Score where AllocationUid = T.AllocationUid and AssetUid = T.AssetUid and EffectiveDate > T.EffectiveDate) MinS " +
                                "inner join metrics.Score S on S.AllocationUid = T.AllocationUid and S.AssetUid = T.AssetUid and S.EffectiveDate = MinS.EffectiveDate", transaction: trans);

                            // Merge scores.
                            await company.ExecuteAsync(
                                "merge metrics.Score as T " +
                                "using #Scores as S " +
                                "on (S.Uid = T.Uid) " +
                                "when matched then " +
                                "update set " +
                                "T.RunDate = S.RunDate, T.EndDate = S.EndDate, T.Value = S.Value " +
                                "when not matched then " +
                                "insert (Uid, AllocationUid, AssetUid, EffectiveDate, Value, RunDate, EndDate) " +
                                "values (S.Uid, S.AllocationUid, S.AssetUid, S.EffectiveDate, S.Value, S.RunDate, S.EndDate);", transaction: trans);

                            // Merge score items.
                            await company.ExecuteAsync(
                                "merge metrics.ScoreItem as T " +
                                "using #ScoreItems as S " +
                                "on (S.Uid = T.Uid) " +
                                "when matched then " +
                                "update set " +
                                "T.RunDate = S.RunDate, T.UpdatedOn = S.UpdatedOn, " +
                                "T.AssetVersionUid = S.AssetVersionUid, T.Value = S.Value, T.Evidence = S.Evidence, " +
                                "T.ConditionUid = S.ConditionUid, T.AdjustedWeight = S.AdjustedWeight, T.AdjustedMaxWeight = S.AdjustedMaxWeight " +
                                "when not matched then " +
                                "insert (UpdatedOn, Value, AdjustedWeight, RunDate, Uid, AssetVersionUid, Evidence, ConditionUid, AdjustedMaxWeight) " +
                                "values (S.UpdatedOn, S.Value, S.AdjustedWeight, S.RunDate, S.Uid, S.AssetVersionUid, S.Evidence, S.ConditionUid, S.AdjustedMaxWeight);", transaction: trans);

                            // Merge score Item Links.
                            await company.ExecuteAsync(
                                "merge metrics.ScoreItemLink as T " +
                                "using #ScoreItemLinks as S " +
                                "on (S.ScoreUid = T.ScoreUid and T.ScoreItemUid = S.ScoreItemUid) " +
                                "when not matched then " +
                                "insert (ScoreUid, ScoreItemUid) " +
                                "values (S.ScoreUid, S.ScoreItemUid);", transaction: trans);

                            trans.Commit();

                            Db.SendScoreEventWithPayload(Guid.NewGuid(), ScoreQueueChangeType.WorkflowCheck, scoresToAdd.Select(i => i.Uid).ToList());
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            throw ex;
                        }
                    }
                }
            }
        }
    }
}
