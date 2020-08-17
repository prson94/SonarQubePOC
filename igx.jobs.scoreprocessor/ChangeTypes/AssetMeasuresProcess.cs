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
            var json = Storage.GetFileContentsAsString(Info.StorageFolder, Info.StorageFile);
            var assetMeasures = JsonConvert.DeserializeObject<List<AssetMeasureModel>>(json);

            var finalScoresToAdd = new List<Score>();

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
                            Result bit not null,
                            MetricAssetVersionUid uniqueidentifier null,
                            AllocationUid uniqueidentifier null,
                            AssetTypeId int null
                        )", transaction: trans);

                    using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans) { BatchSize = 500, DestinationTableName = "#AssetAllocations", BulkCopyTimeout = 3600 })
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
        Si.EffectiveDate,
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
        I.MetricAssetUid,
        Al.AssetUid,
        Al.EffectiveDate
from    (
			select		AllocationUid, AssetUid, EffectiveDate, AssetTypeId
			from		#AssetAllocations		
			group by	AllocationUid, AssetUid, EffectiveDate, AssetTypeId
		) Al
        inner join metrics.Score S on S.AllocationUid = Al.AllocationUid and S.AssetUid = Al.AssetUid and S.EffectiveDate = Al.EffectiveDate
        inner join metrics.ScoreItemLink L on L.ScoreUid = S.Uid
        inner join metrics.ScoreItem I on I.Uid = L.ScoreItemUid", transaction: trans, commandTimeout: 900);
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
			order by C.Position asc
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
                var uniqueAssetCombinations = models.Select(i => new { i.AssetTypeId, i.AllocationUid, i.AssetUid, i.EffectiveDate }).Distinct().ToList();
                uniqueAssetCombinations.ForEach(assetEffectiveDate =>
                {
                    Score assetScoreResult = null;
                    var assetScoreItemResults = new List<ScoreItem>();

                    var allMeasures = allocations.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var providedMeasureResults = models.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.AssetUid == assetEffectiveDate.AssetUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var assetFields = company.Query<FieldDetail>("select F.* from FieldDetail F inner join Asset A on A.ID = F.AssetID and A.Uid = @AssetUid", new { assetEffectiveDate.AssetUid }).ToList();

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
                                    AssetUid = assetEffectiveDate.AssetUid,
                                    MetricAssetUid = measure.MetricAssetUid,
                                    AssetVersionUid = measure.MetricAssetVersionUid,
                                    EffectiveDate = assetEffectiveDate.EffectiveDate,
                                    RunDate = DateTime.UtcNow,
                                    UpdatedOn = DateTime.UtcNow,
                                    ConditionUid = conditionValidator.SelectedConditionUid,
                                    Uid = Guid.NewGuid()
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
                                            var columnSql = "select R.Uid, R.PassFraction ";
                                            var tableSql = "from ";
                                            var whereSql = "";
                                            var matchQuery = "";
                                            measure.RollupPath.SegmentLinks.ForEach(s =>
                                            {
                                                matchQuery += $"N{s.StartAssetTypeID}-(E{s.IntersectTypeID})->";

                                                tableSql += $"graph.AssetNode N{s.StartAssetTypeID}, ";
                                                tableSql += $"graph.AssetEdge E{s.IntersectTypeID}, ";

                                                whereSql += $"and N{s.StartAssetTypeID}.AssetTypeID = {s.StartAssetTypeID} and E{s.IntersectTypeID}.IntersectTypeID = {s.IntersectTypeID} ";

                                                if (s.PredicateType == PredicateType.Evaluation)    // This is the last relationship in the chain.
                                                {
                                                    matchQuery += $"N{s.EndAssetTypeID}-(RE)-R";
                                                    tableSql += $"graph.AssetNode N{s.EndAssetTypeID}, ";
                                                    tableSql += $"dbo.AssetResultEdge RE, ";
                                                    tableSql += $"dbo.AssetResult R ";

                                                    whereSql += $"and E{s.IntersectTypeID}.PredicateType = {(int)s.PredicateType} ";
                                                }
                                            });
                                            whereSql += $"and N{assetEffectiveDate.AssetTypeId}.Uid = @AssetUid";

                                            var sql = $"{columnSql} {tableSql} where match({matchQuery}) {whereSql}";
                                            var rollupPathResultsRequest = await company.QueryAsync<RollupPathRuleResult>(sql, new { assetEffectiveDate.AssetUid });
                                            var rollupPathResults = rollupPathResultsRequest.ToList();

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
                                            else { 
                                                //TODO: Figure out how to send the adjustment value along to code below, without having it be overwritten later.
                                                // For example, pass an adjustment ratio.
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

                                assetScoreItemResults.Add(scoreItem);
                            }
                        }
                    });

                    // Perform final score calculations for this asset/effective date combination.
                    if (assetScoreItemResults.Count > 0)
                    {
                        var previousScoreItems = allPreviousScoreItems.Where(p => p.AssetUid == assetEffectiveDate.AssetUid).ToList();

                        allMeasures.ForEach(aM =>
                        {
                            // If no current results sent in for existing data load, then we need to carry forward the previous score items to create a complete score.
                            if (!assetScoreItemResults.Any(nI => nI.AssetVersionUid == aM.MetricAssetVersionUid))
                            {
                                // We need to add a previous result for the missing measure, or create a new one as a failure.
                                var conditionValidator = CheckMeasureConditions(assetFields, fieldTypes, aM);
                                if (conditionValidator.ConditionMet)
                                {
                                    // Look up to see if there is an existing score item for this measure, and use that value.
                                    var previousScoreItem = previousScoreItems.FirstOrDefault(e => e.MetricAssetUid == aM.MetricAssetUid);

                                    var scoreItem = new ScoreItem
                                    {
                                        RawMeasureWeight = conditionValidator.SelectedWeight,
                                        EffectiveDate = assetEffectiveDate.EffectiveDate,
                                        RunDate = DateTime.UtcNow,
                                        AssetUid = assetEffectiveDate.AssetUid,
                                        AssetVersionUid = aM.MetricAssetVersionUid,
                                        MetricAssetUid = aM.MetricAssetUid,
                                        ConditionUid = conditionValidator.SelectedConditionUid,
                                        UpdatedOn = DateTime.UtcNow,
                                        Value = (previousScoreItem != null) ? previousScoreItem.Value : false
                                    };

                                    var matchingScoreItem = matchingScoreItems.FirstOrDefault(s =>
                                        s.AssetUid == assetEffectiveDate.AssetUid &&
                                        s.EffectiveDate == assetEffectiveDate.EffectiveDate &&
                                        s.MetricAssetUid == scoreItem.MetricAssetUid);

                                    scoreItem.Uid = (matchingScoreItem != null) ? matchingScoreItem.ScoreItemUid : Guid.NewGuid();

                                    if (matchingScoreItem == null && previousScoreItem != null)
                                    {
                                        if (previousScoreItem.EffectiveDate == scoreItem.EffectiveDate)
                                        {
                                            scoreItem.Uid = previousScoreItem.ScoreItemUid;
                                        }
                                    }

                                    assetScoreItemResults.Add(scoreItem);
                                }
                            }
                        });

                        var score = AdjustScoreItemWeights(allMeasures, assetScoreItemResults);
                        assetScoreResult = new Score
                        {
                            EffectiveDate = assetEffectiveDate.EffectiveDate,
                            ScoreType = ScoreType.Governance,
                            AllocationUid = assetEffectiveDate.AllocationUid,
                            AssetUid = assetEffectiveDate.AssetUid,
                            RunDate = DateTime.UtcNow,
                            Value = score,
                            Items = assetScoreItemResults
                        };
                        // If there is a macthing score in the system, update the Uid 
                        var matchingScore = matchingScores.FirstOrDefault(s => s.AllocationUid == assetEffectiveDate.AllocationUid && s.AssetUid == assetEffectiveDate.AssetUid && s.EffectiveDate == assetEffectiveDate.EffectiveDate);
                        assetScoreResult.Uid = (matchingScore != null) ? matchingScore.ScoreUid : Guid.NewGuid();
                        finalScoresToAdd.Add(assetScoreResult);
                    }
                });

                //Now add scores via a transaction.
                if (finalScoresToAdd.Count > 0)
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
                            scores.Columns.Add("ScoreType", typeof(int));
                            scores.Columns.Add("AllocationUid", typeof(Guid));

                            var scoreItems = new DataTable();
                            scoreItems.Columns.Add("Uid", typeof(Guid));
                            scoreItems.Columns.Add("ScoreUid", typeof(Guid));
                            scoreItems.Columns.Add("AssetUid", typeof(Guid));
                            scoreItems.Columns.Add("MetricAssetUid", typeof(Guid));
                            scoreItems.Columns.Add("EffectiveDate", typeof(DateTime));
                            scoreItems.Columns.Add("UpdatedOn", typeof(DateTime));
                            scoreItems.Columns.Add("Value", typeof(bool));
                            scoreItems.Columns.Add("AdjustedWeight", typeof(decimal));
                            scoreItems.Columns.Add("RunDate", typeof(DateTime));
                            scoreItems.Columns.Add("EndDate", typeof(DateTime));
                            scoreItems.Columns.Add("AssetVersionUid", typeof(Guid));
                            scoreItems.Columns.Add("Evidence", typeof(string));
                            scoreItems.Columns.Add("ConditionUid", typeof(Guid));
                            scoreItems.Columns.Add("AdjustedMaxWeight", typeof(decimal));

                            foreach (var s in finalScoresToAdd)
                            {
                                var scoreRow = scores.NewRow();
                                scoreRow["Uid"] = s.Uid;
                                scoreRow["AssetUid"] = s.AssetUid;
                                scoreRow["EffectiveDate"] = s.EffectiveDate.Date;
                                scoreRow["Value"] = s.Value;
                                scoreRow["RunDate"] = s.RunDate;
                                scoreRow["ScoreType"] = (int)s.ScoreType;
                                scoreRow["AllocationUid"] = s.AllocationUid;
                                scores.Rows.Add(scoreRow);

                                foreach (var si in s.Items)
                                {
                                    var scoreItemRow = scoreItems.NewRow();
                                    scoreItemRow["Uid"] = si.Uid;
                                    scoreItemRow["ScoreUid"] = s.Uid;
                                    scoreItemRow["AssetUid"] = si.AssetUid;
                                    scoreItemRow["MetricAssetUid"] = si.MetricAssetUid;
                                    scoreItemRow["EffectiveDate"] = si.EffectiveDate;
                                    scoreItemRow["UpdatedOn"] = si.UpdatedOn;
                                    scoreItemRow["Value"] = si.Value;
                                    scoreItemRow["AdjustedWeight"] = si.AdjustedWeight;
                                    scoreItemRow["RunDate"] = si.RunDate;
                                    if (si.EndDate.HasValue)
                                        scoreItemRow["EndDate"] = si.EndDate;
                                    scoreItemRow["AssetVersionUid"] = si.AssetVersionUid;
                                    scoreItemRow["Evidence"] = si.Evidence ?? "{}";
                                    if (si.ConditionUid.HasValue)
                                        scoreItemRow["ConditionUid"] = si.ConditionUid;
                                    scoreItemRow["AdjustedMaxWeight"] = si.AdjustedMaxWeight;
                                    scoreItems.Rows.Add(scoreItemRow);
                                }
                            }

                            await company.ExecuteAsync(
                                @"create table #Scores (
                                    Uid uniqueidentifier not null,
                                    AssetUid uniqueidentifier not null,
                                    EffectiveDate date not null,
                                    Value decimal(5,3) null,
                                    RunDate datetime not null,
                                    EndDate date null,
                                    ScoreType int not null,
                                    AllocationUid uniqueidentifier null
                                );
                                create table #ScoreItems (
	                                Uid uniqueidentifier NOT NULL,
                                    ScoreUid uniqueidentifier NOT NULL,
	                                AssetUid uniqueidentifier NOT NULL,
	                                MetricAssetUid uniqueidentifier NOT NULL,
	                                EffectiveDate date NOT NULL,
	                                UpdatedOn datetime NOT NULL,
	                                Value bit NOT NULL,
	                                AdjustedWeight decimal(5, 3) NULL,
	                                RunDate datetime NULL,
	                                EndDate date NULL,
	                                AssetVersionUid uniqueidentifier NULL,
	                                Evidence nvarchar(max) NULL,
	                                ConditionUid uniqueidentifier NULL,
	                                AdjustedMaxWeight decimal(5, 3) NULL
                                );", transaction: trans);

                            var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                            {
                                BatchSize = scores.Rows.Count,
                                DestinationTableName = "#Scores",
                                BulkCopyTimeout = 3600
                            };

                            bulkCopy.ColumnMappings.Add("Uid", "Uid");
                            bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                            bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                            bulkCopy.ColumnMappings.Add("Value", "Value");
                            bulkCopy.ColumnMappings.Add("RunDate", "RunDate");
                            bulkCopy.ColumnMappings.Add("EndDate", "EndDate");
                            bulkCopy.ColumnMappings.Add("ScoreType", "ScoreType");
                            bulkCopy.ColumnMappings.Add("AllocationUid", "AllocationUid");

                            await bulkCopy.WriteToServerAsync(scores);

                            bulkCopy = null;

                            bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                            {
                                BatchSize = scoreItems.Rows.Count,
                                DestinationTableName = "#ScoreItems",
                                BulkCopyTimeout = 3600
                            };

                            bulkCopy.ColumnMappings.Add("Uid", "Uid");
                            bulkCopy.ColumnMappings.Add("ScoreUid", "ScoreUid");
                            bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                            bulkCopy.ColumnMappings.Add("MetricAssetUid", "MetricAssetUid");
                            bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                            bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");
                            bulkCopy.ColumnMappings.Add("Value", "Value");
                            bulkCopy.ColumnMappings.Add("AdjustedWeight", "AdjustedWeight");
                            bulkCopy.ColumnMappings.Add("RunDate", "RunDate");
                            bulkCopy.ColumnMappings.Add("EndDate", "EndDate");
                            bulkCopy.ColumnMappings.Add("AssetVersionUid", "AssetVersionUid");
                            bulkCopy.ColumnMappings.Add("Evidence", "Evidence");
                            bulkCopy.ColumnMappings.Add("ConditionUid", "ConditionUid");
                            bulkCopy.ColumnMappings.Add("AdjustedMaxWeight", "AdjustedMaxWeight");

                            await bulkCopy.WriteToServerAsync(scoreItems);

                            bulkCopy = null;

                            // Final check to match up existing score items.
                            await company.ExecuteAsync("update T " +
                                "set T.Uid = S.Uid " +
                                "from #ScoreItems T " +
                                "inner join metrics.ScoreItem S on S.AssetUid = T.AssetUid and S.MetricAssetUid = T.MetricAssetUid and S.EffectiveDate = T.EffectiveDate", transaction: trans);


                            // End-date earlier scores and score items.
                            await company.ExecuteAsync("update T " +
                                "set T.EndDate = DATEADD(d, -1, S.EffectiveDate) " +
                                "from metrics.Score T " +
                                "inner join #Scores S on S.AllocationUid = T.AllocationUid and S.AssetUid = T.AssetUid and S.EffectiveDate > T.EffectiveDate and T.EndDate is null", transaction: trans);
                            await company.ExecuteAsync("update T " +
                                "set T.EndDate = DATEADD(d, -1, S.EffectiveDate) " +
                                "from metrics.ScoreItem T " +
                                "inner join #ScoreItems S on S.MetricAssetUid = T.MetricAssetUid and S.AssetUid = T.AssetUid and S.EffectiveDate > T.EffectiveDate and T.EndDate is null and S.Uid <> T.Uid", transaction: trans);

                            // End-date new scores and score items IF the effective date is not the latest effective date.
                            await company.ExecuteAsync("update T " +
                                "set T.EndDate = DATEADD(d, -1, S.EffectiveDate) " +
                                "from #Scores T " +
                                "cross apply (select min(EffectiveDate) as EffectiveDate from metrics.Score where AllocationUid = T.AllocationUid and AssetUid = T.AssetUid and EffectiveDate > T.EffectiveDate) MinS " +
                                "inner join metrics.Score S on S.AllocationUid = T.AllocationUid and S.AssetUid = T.AssetUid and S.EffectiveDate = MinS.EffectiveDate", transaction: trans);
                            await company.ExecuteAsync("update T " +
                                "set T.EndDate = DATEADD(d, -1, S.EffectiveDate) " +
                                "from #ScoreItems T " +
                                "cross apply (select min(EffectiveDate) as EffectiveDate from metrics.ScoreItem where MetricAssetUid = T.MetricAssetUid and AssetUid = T.AssetUid and EffectiveDate > T.EffectiveDate) MinS " +
                                "inner join metrics.ScoreItem S on S.MetricAssetUid = T.MetricAssetUid and S.AssetUid = T.AssetUid and S.EffectiveDate = MinS.EffectiveDate and S.Uid <> T.Uid", transaction: trans);

                            // Merge scores.
                            await company.ExecuteAsync("merge metrics.Score as T " +
                                "using #Scores as S " +
                                "on (S.Uid = T.Uid) " +
                                "when matched then " +
                                "update set " +
                                "T.RunDate = S.RunDate, T.EndDate = S.EndDate, T.Value = S.Value " +
                                "when not matched then " +
                                "insert (Uid, AllocationUid, AssetUid, EffectiveDate, Value, RunDate, EndDate, ScoreType) " +
                                "values (S.Uid, S.AllocationUid, S.AssetUid, S.EffectiveDate, S.Value, S.RunDate, S.EndDate, S.ScoreType);", transaction: trans);

                            // Merge score items.
                            await company.ExecuteAsync("merge metrics.ScoreItem as T " +
                                "using #ScoreItems as S " +
                                //"on (S.AssetUid = T.AssetUid and S.MetricAssetUid = T.MetricAssetUid and S.EffectiveDate = T.EffectiveDate) " +
                                "on (S.Uid = T.Uid) " +
                                "when matched then " +
                                "update set " +
                                "T.RunDate = S.RunDate, T.EndDate = S.EndDate, T.UpdatedOn = S.UpdatedOn, " +
                                "T.AssetVersionUid = S.AssetVersionUid, T.Value = S.Value, T.Evidence = S.Evidence, " +
                                "T.ConditionUid = S.ConditionUid, T.AdjustedWeight = S.AdjustedWeight, T.AdjustedMaxWeight = S.AdjustedMaxWeight " +
                                "when not matched then " +
                                "insert (AssetUid, MetricAssetUid, EffectiveDate, UpdatedOn, Value, AdjustedWeight, RunDate, EndDate, Uid, AssetVersionUid, Evidence, ConditionUid, AdjustedMaxWeight) " +
                                "values (S.AssetUid, S.MetricAssetUid, S.EffectiveDate, S.UpdatedOn, S.Value, S.AdjustedWeight, S.RunDate, S.EndDate, S.Uid, S.AssetVersionUid, S.Evidence, S.ConditionUid, S.AdjustedMaxWeight);", transaction: trans);

                            // Merge score Item Links.
                            await company.ExecuteAsync("merge metrics.ScoreItemLink as T " +
                                "using #ScoreItems as S " +
                                "on (S.ScoreUid = T.ScoreUid and T.ScoreItemUid = S.Uid) " +
                                "when not matched then " +
                                "insert (ScoreUid, ScoreItemUid) " +
                                "values (S.ScoreUid, S.Uid);", transaction: trans);

                            trans.Commit();

                            Db.SendScoreEventWithPayload(Guid.NewGuid(), ScoreQueueChangeType.WorkflowCheck, finalScoresToAdd.Select(i => i.Uid).ToList());
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
