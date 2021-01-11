using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.model;
using Dapper;
using igx.jobs.scoreprocessor.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
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
            var scoreItemLinksToDelete = new List<ScoreItemLink>();

            var Db = GetCompanyContext();
            using (var company = GetEnvironmentConnection())
            {
                var executionRecord = company.Query<ApiExecution>("select * from api.Execution where ExecutionID = @id", new { id = Info.ExecutionUid }).SingleOrDefault();

                if (executionRecord == null && assetMeasures.Count > 10)
                {
                    executionRecord = new ApiExecution
                    {
                        ExecutionID = Info.ExecutionUid,
                        StartedOn = Info.StartedOn,
                        ResourceID = Info.ResourceID ?? 0,
                        Method = "SCORE",
                        State = State.Unknown,
                        Route = "ScoreEngine",
                        Total = assetMeasures.Count
                    };
                    Db.Add(executionRecord);
                }

                if (executionRecord != null)
                {
                    // This means that the original execution came in via one of the external measure/score endpoints.
                    // We need to check whether any other execution is running.

                    // Wait a moment in case there are multiple queue messages
                    Thread.Sleep(new Random().Next(3000, 7000));

                    var currentlyRunningExecutions = company.Query<bool>(@"
select  cast(iif(count(1) > 0, 1, 0) as bit) 
from    api.Execution 
where   ExecutionID <> @id 
        and [Route] = 'ScoreEngine'
        and MarkedForProcessing = 1 
        and (
    (Total <= 1000 and ProcessingStartedOn > dateadd(mi, -10, getutcdate())) OR
    (Total > 1000 and Total <= 10000 and ProcessingStartedOn > dateadd(mi, -30, getutcdate())) OR
    (Total > 10000 and ProcessingStartedOn > dateadd(hh, -3, getutcdate()))  )", new { id = Info.ExecutionUid }).Single();
                    if (currentlyRunningExecutions)
                    {
                        throw new ScoresCurrentlyProcessingException();
                    }

                    executionRecord.MarkedForProcessing = true;
                    executionRecord.ProcessingStartedOn = DateTime.UtcNow;
                    company.Execute("update api.Execution set MarkedForProcessing = 1, ProcessingStartedOn = @dt where ExecutionID = @id", new { dt = executionRecord.ProcessingStartedOn, id = executionRecord.ExecutionID });
                }

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
                List<AssetMeasuresProcessField> fields = null;
                List<AssetAllocationPreviousResult> allPreviousScoreItems = null;
                List<MatchingScoreModel> matchingScores = null;
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

select	A.Uid as AssetUid,
		FT.ID as FieldTypeID,
        FT.Name as FieldTypeName,
		COALESCE (V.LookupValues, F.Value, F.FormattedValue, FT.DefaultValue) as [Values] 
from	Asset A 
        inner join (
			select		AssetUid
			from		#AssetAllocations		
			group by	AssetUid
		) Al on Al.AssetUid = A.Uid
		inner join FieldType FT ON FT.AssetTypeID = A.AssetTypeID 
		left join Field F ON F.FieldTypeID = FT.ID AND F.ObjectType = A.Object AND F.ObjectID = A.ObjectID
		outer apply (
			select	string_agg(lower(cast(LA.Uid as nvarchar(50))), ',') as LookupValues
			from	STRING_SPLIT(COALESCE(F.Value, FT.DefaultValue),',') MV
					inner join Asset LA on LA.ObjectID = MV.value
					inner join AssetType LAT on LAT.Object = FT.LookupObjectType+'Type' and LAT.ObjectID = FT.LookupObjectID and LAT.ID = LA.AssetTypeID
			where	FT.Type = 'Lookup'
		) V
where	(F.FormattedValue IS NOT NULL 
		OR FT.DefaultValue IS NOT NULL 
		OR FT.ShowIfEmpty = 1)
		and FT.Type not in ('JSON','Path','Relationship','FieldFromRelationship','ComplexRelationLookup', 'OwnershipLookup', 'RefListRelationship','Tag','Score');

select  *
from    (
        select  Al.AllocationUid,
                Al.AssetUid,
		        V.AssetUid as MetricAssetUid,
                ROW_NUMBER() OVER(PARTITION BY Al.AssetUid, Al.EffectiveDate, Si.AssetVersionUid ORDER BY S.EffectiveDate DESC) as RowNum,
		        L.*,
                Si.AssetVersionUid as MetricAssetVersionUid,
		        Si.ConditionUid,
                S.EffectiveDate,
                S.EndDate,
		        Si.Value,
		        Si.AdjustedWeight,
		        Si.AdjustedMaxWeight,
                iif(U.UseCount > 0, cast(1 as bit), cast(0 as bit)) as UsedInOtherScores
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
		        inner join metrics.AssetVersion V on V.Uid = Si.AssetVersionUid 
                cross apply (
                    select  count(1) as UseCount
                    from    metrics.ScoreItemLink
                    where   ScoreUid <> S.Uid
                            and ScoreItemUid = Si.Uid
                ) U
        ) O
where   O.RowNum = 1;

select  *
from    (
select  S.Uid as ScoreUid,
        Al.AllocationUid,
        Al.AssetUid,
        ROW_NUMBER() OVER(PARTITION BY Al.AllocationUid, Al.AssetUid ORDER BY S.EffectiveDate DESC) as RowNum,        
        S.EffectiveDate,
        S.VersionValueHash
from    (
			select		AllocationUid, AssetUid, EffectiveDate, AssetTypeId
			from		#AssetAllocations		
			group by	AllocationUid, AssetUid, EffectiveDate, AssetTypeId
		) Al
        inner join metrics.Score S on S.AllocationUid = Al.AllocationUid and S.AssetUid = Al.AssetUid and S.EffectiveDate <= Al.EffectiveDate
        ) O
where   O.RowNum = 1;", transaction: trans, commandTimeout: 900);
                    models = supportingDataRequest.Read<ExternalMeasureResultsCreatedModel>().ToList();
                    fieldTypes = supportingDataRequest.Read<FieldType>().ToList();
                    fields = supportingDataRequest.Read<AssetMeasuresProcessField>().ToList();
                    allPreviousScoreItems = supportingDataRequest.Read<AssetAllocationPreviousResult>().ToList();
                    matchingScores = supportingDataRequest.Read<MatchingScoreModel>().ToList();

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
        V.UpdateFrequency,
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
                            select  [Value] 
                            from    metrics.AssetVersionConditionItemValue 
                            where   Uid = I.Uid
                            for json path
                            ) as ValueItems
                            --JSON_QUERY((SELECT CONCAT('[""',STRING_AGG([Value], '"",""'),'""]') FROM metrics.AssetVersionConditionItemValue where Uid = I.Uid)) as [Values]
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
		inner join metrics.AssetVersion V on V.AssetUid = A.Uid and V.[State] = 1
        inner join metrics.Allocation Mal on Mal.Uid = A.AllocationUid
		inner join (
			select		AllocationUid, EffectiveDate
			from		#AssetAllocations		
			group by	AllocationUid, EffectiveDate
		) Al on Al.AllocationUid = A.AllocationUid and ( (Al.EffectiveDate between V.EffectiveDate and V.EffectiveEndDate) or (Al.EffectiveDate >= V.EffectiveDate and V.EffectiveEndDate is null) )", transaction: trans);
                    allocations = allocationRequest.ToList();

                    trans.Commit();
                }
                
                // Store the Governance check values and whetehr they are valid, so we do not have to check individual measure validity more than once.
                var assetVersionCheckObjectTypes = new AssetVersionCheckObjectTypes();

                Action<MetricAssetDefinitionGovernanceViewModel, Guid, bool?> assetVersionCheckObjectTypeAction = (MetricAssetDefinitionGovernanceViewModel governance, Guid metricAssetVersionUid, bool? overrideBoolValue) => {
                    if (assetVersionCheckObjectTypes.OkToAddToList(metricAssetVersionUid))
                    {
                        var check = MetricGovernanceCheckType.External;

                        var assetVersionCheckObjectType = new AssetVersionCheckObjectType
                        {
                            AssetVersionUid = metricAssetVersionUid,
                            Valid = true
                        };

                        if (governance != null)
                        {
                            check = governance.Check;
                        }

                        if (overrideBoolValue.HasValue)
                        {
                            assetVersionCheckObjectType.Valid = overrideBoolValue.Value;
                        }
                        else 
                        {
                            var tbl = "";
                            switch (check)
                            {
                                case MetricGovernanceCheckType.Owner:
                                    tbl = "[ResponsibilityType]";
                                    assetVersionCheckObjectType.TypeUid = governance.Owner.ResponsibilityTypeUid;
                                    break;
                                case MetricGovernanceCheckType.Predicate:
                                    tbl = "[Predicate]";
                                    assetVersionCheckObjectType.TypeUid = governance.Predicate.PredicateUid;
                                    break;
                                case MetricGovernanceCheckType.Relation:
                                    tbl = "IntersectType";
                                    assetVersionCheckObjectType.TypeUid = governance.Relation.IntersectTypeUid;
                                    break;
                            }
                            if (!string.IsNullOrEmpty(tbl))
                            {
                                var exists = company.Query<bool>($"select cast(iif(count(1) > 0, 1, 0) as bit) from {tbl} where Uid = @TypeUid", new { assetVersionCheckObjectType.TypeUid }).Single();
                                assetVersionCheckObjectType.Valid = exists;
                            }
                        }
                    
                        assetVersionCheckObjectTypes.Add(assetVersionCheckObjectType);                    
                    }
                };

                var dqMeasureQueryLibrary = new List<DataQualityMeasureQueryModel>();
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
                    var assetScoreItemLinksToDelete = new List<ScoreItemLink>();

                    var allMeasures = allocations.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var providedMeasureResults = models.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.AssetUid == assetEffectiveDate.AssetUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var assetFields = fields.Where(f => f.Assetuid == assetEffectiveDate.AssetUid).ToList(); //company.Query<FieldDetail>("select F.* from FieldDetail F inner join Asset A on A.ID = F.AssetID and A.Uid = @AssetUid", new { assetEffectiveDate.AssetUid }).ToList();
                    var previousScoreItems = allPreviousScoreItems.Where(p => p.AssetUid == assetEffectiveDate.AssetUid && p.EffectiveDate.Date <= assetEffectiveDate.EffectiveDate.Date).ToList();

                    providedMeasureResults.ForEach(n =>
                    {
                        var measure = allMeasures.FirstOrDefault(i => i.MetricAssetVersionUid == n.MetricAssetVersionUid);
                        if (measure != null)
                        {
                            var conditionValidator = CheckMeasureConditions(assetFields, fieldTypes, measure);

                            if (conditionValidator.ConditionMet) // Then we should be creating this score result (a conditon was met, or does not need to be met except to override weight)
                            {
                                if (assetVersionCheckObjectTypes.ShouldContinueAnalysis(measure.MetricAssetVersionUid))
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

                                    var definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(measure.Definition ?? "{}");

                                    // Now perform analysis based on measure type and check type.
                                    switch (measure.ScoreType)
                                    {
                                        case ScoreType.DataQuality:
                                            #region
                                            var dqDefinition = definition.DataQuality;
                                            // Do something with rollups here.
                                            if (measure.RollupPath == null || dqDefinition == null)
                                            {
                                                var error = "";
                                                error += (measure.RollupPath == null) ? "Rollup Path is invalid. An asset type or relationship type may have been removed. " : "";
                                                error += (dqDefinition == null) ? "Measure definition is invalid. Please check and re-save the measure definition, and try again." : "";
                                                scoreItem.Value = false;
                                                scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = error });
                                            }
                                            else
                                            {
                                                if (measure.RollupPath.SegmentLinks == null)
                                                {
                                                    scoreItem.Value = false;
                                                    scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = "Rollup Path Segments do not exist. An asset type or relationship type may have been removed." });
                                                }
                                                else
                                                {
                                                    var dqQueryDetail = dqMeasureQueryLibrary.FirstOrDefault(dq => dq.AssetVersionRollupPathUid == measure.RollupPath.AssetVersionRollupPathUid);
                                                    if (dqQueryDetail == null)
                                                    {
                                                        dqQueryDetail = BuildDataQualityMeasureQueryModel(measure.AllocationUid, measure.RollupPath.AssetVersionRollupPathUid);
                                                        dqMeasureQueryLibrary.Add(dqQueryDetail); // Add to library for future reference.
                                                    }

                                                    try
                                                    {
                                                        DateTime minimumDate = measure.EffectiveDate;
                                                        switch (measure.UpdateFrequency)
                                                        {
                                                            case MetricUpdateFrequency.Annually:
                                                                minimumDate = minimumDate.AddYears(-1);
                                                                break;
                                                            case MetricUpdateFrequency.Daily:
                                                                minimumDate = minimumDate.AddDays(-1);
                                                                break;
                                                            case MetricUpdateFrequency.Hourly:
                                                                minimumDate = minimumDate.AddHours(-1);
                                                                break;
                                                            case MetricUpdateFrequency.Monthly:
                                                                minimumDate = minimumDate.AddMonths(-1);
                                                                break;
                                                            case MetricUpdateFrequency.Quarterly:
                                                                minimumDate = minimumDate.AddMonths(-3);
                                                                break;
                                                            case MetricUpdateFrequency.None:
                                                            case MetricUpdateFrequency.Weekly:
                                                                minimumDate = minimumDate.AddDays(-7);
                                                                break;
                                                        }
                                                        var rollupPathResults = GetDataQualityMeasureQueryResultModels(dqQueryDetail, n.AssetUid, minimumDate, measure.EffectiveEndDate);

                                                        if (rollupPathResults.Count > 0)
                                                        {
                                                            rollupPathResults.ForEach(o =>
                                                            {
                                                                if (o.StructuredResults.Count > 0)
                                                                {
                                                                    // We should only be getting one result back for each row anyway. This is just in case.
                                                                    o.ResultScoreValue = o.StructuredResults.Select(r => r.PassFraction).Average();
                                                                }
                                                                else 
                                                                {
                                                                    o.ResultScoreValue = 0;
                                                                }
                                                            });

                                                            float resultOperationValue = 0;
                                                            switch (dqDefinition.ResultOperation)
                                                            {
                                                                case MetricRuleResultOperation.Average:
                                                                    resultOperationValue = rollupPathResults.Select(r => r.ResultScoreValue).Average();
                                                                    break;
                                                                case MetricRuleResultOperation.Maximum:
                                                                    resultOperationValue = rollupPathResults.Select(r => r.ResultScoreValue).Max();
                                                                    break;
                                                                case MetricRuleResultOperation.Minimum:
                                                                    resultOperationValue = rollupPathResults.Select(r => r.ResultScoreValue).Min();
                                                                    break;
                                                            }

                                                            if (measure.IsThresholdBased)
                                                            {
                                                                scoreItem.DecimalValue = resultOperationValue;
                                                                scoreItem.Value = (measure.Threshold <= resultOperationValue);
                                                            }
                                                            else
                                                            {
                                                                // This will be used when adjusting max and actual weights.
                                                                scoreItem.DecimalValue = resultOperationValue;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            scoreItem.Value = true;
                                                        }

                                                        var evidence = rollupPathResults.Select(rp => new DataQualityEvidenceModel
                                                        {
                                                            ErrorMessage = null,
                                                            IsError = false,
                                                            ResultResultUids = rp.StructuredResults.Select(r => r.Uid).ToList(),
                                                            RollupPath = rp.StructuredPath
                                                        });

                                                        scoreItem.Evidence = JsonConvert.SerializeObject(evidence);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        scoreItem.Value = false;
                                                        scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = ex.GetFullExceptionData(false) });
                                                    }
                                                }

                                            }

                                            break;
                                        #endregion
                                        case ScoreType.Governance:
                                            #region
                                            var gDefinition = definition.Governance;
                                            if (gDefinition == null)
                                            {
                                                gDefinition = new MetricAssetDefinitionGovernanceViewModel
                                                {
                                                    Check = MetricGovernanceCheckType.External,
                                                    External = new MetricAssetDefinitionGovernanceExternalViewModel { UpdateFrequency = MetricUpdateFrequency.None }
                                                };
                                            }
                                            switch (gDefinition.Check)
                                            {
                                                case MetricGovernanceCheckType.External:
                                                    scoreItem.Value = n.Result;
                                                    break;
                                                case MetricGovernanceCheckType.Field:
                                                    if (gDefinition.Field != null)
                                                    {
                                                        var assetFieldType = fieldTypes.FirstOrDefault(i => i.Name == gDefinition.Field.FieldTypeName);
                                                        string dataType = (assetFieldType != null) ? assetFieldType.Type : "Text";
                                                        bool allowMultipleValues = (assetFieldType != null) ? assetFieldType.AllowMultipleValues : false;

                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(gDefinition, measure.MetricAssetVersionUid, (assetFieldType != null));

                                                        var assetFieldForFieldCheck = assetFields.FirstOrDefault(f => f.FieldTypeName == gDefinition.Field.FieldTypeName);

                                                        scoreItem.Value = gDefinition.Field.Operator.TestTwoValues(dataType, allowMultipleValues, gDefinition.Field.Values, ((assetFieldForFieldCheck == null) ? null : assetFieldForFieldCheck.Values));
                                                    }
                                                    break;
                                                case MetricGovernanceCheckType.Owner:
                                                    if (gDefinition.Owner != null)
                                                    {
                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(gDefinition, measure.MetricAssetVersionUid, null);

                                                        string trueValue = (gDefinition.Owner.Operator == Operator.Populated) ? "1" : "0";
                                                        string falseValue = (gDefinition.Owner.Operator == Operator.Populated) ? "0" : "1";
                                                        scoreItem.Value = company.Query<bool>(
                                                            $"select cast(iif(count(1) > 0, {trueValue}, {falseValue}) as bit) " +
                                                            "from ResponsibilityDetail R " +
                                                            "inner join ResponsibilityType T on T.ID = R.ResponsibilityTypeID and T.Uid = @ResponsibilityTypeUid " +
                                                            "where exists ( select 1 from Asset where Uid = @AssetUid and ( (ID = R.AssetID and R.AssetID <> 0) or (AssetTypeID = R.AssetTypeID and R.AssetID = 0) ) )",
                                                            new { gDefinition.Owner.ResponsibilityTypeUid, n.AssetUid }, commandTimeout: 90
                                                            ).Single();
                                                    }
                                                    else
                                                    {
                                                        scoreItem.Value = false;
                                                    }
                                                    break;
                                                case MetricGovernanceCheckType.Predicate:
                                                    if (gDefinition.Predicate != null)
                                                    {
                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(gDefinition, measure.MetricAssetVersionUid, null);

                                                        var predicateExistenceSql = "select cast(iif(sum(bit1) > 0, 1, 0) as bit) from (" +
                                                            "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where PredicateUid = @PredicateUid and SubjectUid = @AssetUid  " +
                                                            "union all " +
                                                            "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where PredicateUid = @PredicateUid and ObjectUid = @AssetUid " +
                                                            ") a";

                                                        switch (gDefinition.Predicate.Operator)
                                                        {
                                                            case Operator.Populated:
                                                                scoreItem.Value = company.Query<bool>(predicateExistenceSql, new { gDefinition.Predicate.PredicateUid, n.AssetUid }, commandTimeout: 90).Single();
                                                                break;
                                                            case Operator.NotPopulated:
                                                                scoreItem.Value = !company.Query<bool>(predicateExistenceSql, new { gDefinition.Predicate.PredicateUid, n.AssetUid }, commandTimeout: 90).Single();
                                                                break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        scoreItem.Value = false;
                                                    }
                                                    break;
                                                case MetricGovernanceCheckType.Relation:
                                                    if (gDefinition.Relation != null)
                                                    {
                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(gDefinition, measure.MetricAssetVersionUid, null);

                                                        var operatorSql = "";
                                                        var bitSql = "";
                                                        object parameters = null;

                                                        switch (gDefinition.Relation.Operator)
                                                        {
                                                            case Operator.Equals:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, n.AssetUid, ValueUid = Guid.Parse(gDefinition.Relation.Values[0]) };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql = 
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @AssetUid and ObjectUid = @ValueUid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @ValueUid and ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.In:
                                                                parameters = new {
                                                                    gDefinition.Relation.IntersectTypeUid,
                                                                    n.AssetUid,
                                                                    Uids = gDefinition.Relation.Values.Select(u => new { Uid = Guid.Parse(u) }).AsTableValuedParameter("dbo.UidTable", new List<string>() { "Uid" })
                                                                };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = @AssetUid and I.ObjectUid = U.Uid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = U.Uid and I.ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.NotEquals:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, n.AssetUid, ValueUid = Guid.Parse(gDefinition.Relation.Values[0]) };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @AssetUid and ObjectUid = @ValueUid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @ValueUid and ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.NotIn:
                                                                parameters = new {
                                                                    gDefinition.Relation.IntersectTypeUid,
                                                                    n.AssetUid,
                                                                    Uids = gDefinition.Relation.Values.Select(u => new { Uid = Guid.Parse(u) }).AsTableValuedParameter("dbo.UidTable", new List<string>() { "Uid" })
                                                                };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = @AssetUid and I.ObjectUid = U.Uid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = U.Uid and I.ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.NotPopulated:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, n.AssetUid };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @AssetUid  " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and ObjectUid = @AssetUid ";
                                                                break;
                                                            default: // case Operator.Populated:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, n.AssetUid };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @AssetUid  " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and ObjectUid = @AssetUid ";
                                                                break;
                                                        }
                                                        var relationSql = $"select cast({bitSql} as bit) from ({operatorSql}) a";

                                                        scoreItem.Value = company.Query<bool>(relationSql, parameters, commandTimeout: 90).Single();
                                                    }
                                                    else
                                                    {
                                                        scoreItem.Value = false;
                                                    }
                                                    break;
                                                default:
                                                    scoreItem.Value = false;
                                                    break;
                                            }
                                            break;
                                            #endregion
                                    }

                                    if (assetVersionCheckObjectTypes.ShouldContinueAnalysis(measure.MetricAssetVersionUid))
                                    { 
                                        // Check to see if we have an existing scoreItem for this recalculated measure result.
                                        var previousScoreItem = previousScoreItems.FirstOrDefault(e => e.MetricAssetVersionUid == measure.MetricAssetVersionUid);
                                        Guid scoreItemUid = Guid.NewGuid();
                                        if (previousScoreItem != null)
                                        {
                                            if (previousScoreItem.Value == scoreItem.Value && previousScoreItem.AdjustedWeight == scoreItem.AdjustedWeight)
                                            {
                                                // Since value is the same, just link the existing score item to score.
                                                scoreItemUid = previousScoreItem.ScoreItemUid;
                                            }
                                            else
                                            {
                                                if (previousScoreItem.EffectiveDate.Date == assetEffectiveDate.EffectiveDate.Date)
                                                {
                                                    // The value for an existing effective date is the now different.
                                                    if (previousScoreItem.UsedInOtherScores)
                                                    {
                                                        // The score item is used in an earlier score, so we need to create a new score item, AND detach this score from the now old score item.
                                                        assetScoreItemLinksToDelete.Add(new ScoreItemLink { ScoreItemUid = previousScoreItem.ScoreItemUid });
                                                    }
                                                    else
                                                    {
                                                        // Not used in any other score, so we are OK to update the value on this score item.
                                                        scoreItemUid = previousScoreItem.ScoreItemUid;
                                                    }
                                                }
                                            }
                                        }
                                        scoreItem.Uid = scoreItemUid;

                                        assetScoreItems.Add(scoreItem);
                                        assetScoreItemLinks.Add(new ScoreItemLink { ScoreItemUid = scoreItem.Uid });                                    
                                    }
                                }
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
                                if (assetVersionCheckObjectTypes.OkToAddToList(aM.MetricAssetVersionUid))
                                {
                                    // Check the measure validity.
                                    if (aM.ScoreType == ScoreType.Governance)
                                    {
                                        var definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(aM.Definition ?? "{}");
                                        bool? overrideValue = null;
                                        if (definition.Governance != null)
                                        {
                                            if (definition.Governance.Check == MetricGovernanceCheckType.Field)
                                            {
                                                overrideValue = fieldTypes.Any(f => f.Name == definition.Governance.Field.FieldTypeName);
                                            }
                                        }
                                        assetVersionCheckObjectTypeAction(definition.Governance, aM.MetricAssetVersionUid, overrideValue);
                                    }
                                }

                                if (assetVersionCheckObjectTypes.ShouldContinueAnalysis(aM.MetricAssetVersionUid))
                                { 
                                    // We need to add a previous result for the missing measure, or create a new one as a failure.
                                    var conditionValidator = CheckMeasureConditions(assetFields, fieldTypes, aM);
                                    if (conditionValidator.ConditionMet)
                                    {
                                        // Look up to see if there is an existing score item for this measure, and use that value.
                                        var previousScoreItem = previousScoreItems.FirstOrDefault(e => e.MetricAssetUid == aM.MetricAssetUid);
                                        var scoreItemUid = Guid.Empty;//.NewGuid();
                                        bool scoreItemValue = false;
                                        if (previousScoreItem != null) 
                                        {
                                            // If a different measure version, then automatically generate a new guid.
                                            if (previousScoreItem.MetricAssetVersionUid != aM.MetricAssetVersionUid)
                                            {
                                                scoreItemUid = Guid.NewGuid();
                                            }
                                            scoreItemValue = previousScoreItem.Value;
                                        }

                                        var scoreItem = new ScoreItem
                                        {
                                            MetricAssetUid = aM.MetricAssetUid,
                                            RawMeasureWeight = conditionValidator.SelectedWeight,
                                            RunDate = DateTime.UtcNow,
                                            AssetVersionUid = aM.MetricAssetVersionUid,
                                            ConditionUid = conditionValidator.SelectedConditionUid,
                                            UpdatedOn = DateTime.UtcNow,
                                            Value = scoreItemValue,
                                            Uid = scoreItemUid
                                        };
                                        
                                        assetScoreItems.Add(scoreItem);
                                        
                                        if (scoreItemUid != Guid.Empty) 
                                        {
                                            assetScoreItemLinks.Add(new ScoreItemLink { ScoreItemUid = scoreItem.Uid });
                                        }
                                    }                                
                                }
                            }
                        });

                        var score = AdjustScoreItemWeights(allMeasures, assetScoreItems);

                        var matchingScore = matchingScores.FirstOrDefault(s => s.AllocationUid == assetEffectiveDate.AllocationUid && s.AssetUid == assetEffectiveDate.AssetUid);

                        // Now figure out whether to switch out the Uids for any score items that are currently marked as empty guid.
                        assetScoreItems.ForEach(scoreItem =>
                        {
                            if (scoreItem.Uid == Guid.Empty)
                            {
                                var previousScoreItem = previousScoreItems.FirstOrDefault(e => e.MetricAssetUid == scoreItem.MetricAssetUid);
                                var scoreItemUid = Guid.NewGuid();
                                if (previousScoreItem != null)
                                {
                                    if ((previousScoreItem.AdjustedWeight == scoreItem.AdjustedWeight) && (previousScoreItem.AdjustedMaxWeight == scoreItem.AdjustedMaxWeight))
                                    {
                                        scoreItem.Uid = previousScoreItem.ScoreItemUid;
                                    }
                                    else 
                                    {
                                        // Check to see if this is for the same effective date as the most recent existing score.
                                        scoreItem.Uid = (matchingScore != null && matchingScore.EffectiveDate == assetEffectiveDate.EffectiveDate) 
                                            ? previousScoreItem.ScoreItemUid 
                                            : Guid.NewGuid();
                                    }
                                }
                                else 
                                {
                                    scoreItem.Uid = Guid.NewGuid();
                                }
                                assetScoreItemLinks.Add(new ScoreItemLink { ScoreItemUid = scoreItem.Uid });
                            }
                        });

                        // Helps to determine if we should create a new score record.
                        var scoreItemHash = string.Join(";", assetScoreItems.OrderBy(i => i.AssetVersionUid).Select(i => $"{i.AssetVersionUid}:{String.Format("{0:#,0.000}", i.AdjustedWeight ?? 0)}"));
                        scoreItemHash = scoreItemHash.GetSha1HashString();

                        Score assetScore = new Score
                        {
                            EffectiveDate = assetEffectiveDate.EffectiveDate,
                            AllocationUid = assetEffectiveDate.AllocationUid,
                            AssetUid = assetEffectiveDate.AssetUid,
                            RunDate = DateTime.UtcNow,
                            Value = score,
                            VersionValueHash = scoreItemHash
                        };

                        // If there is a matching score in the system, update the Uid 
                        var scoreUid = Guid.NewGuid();
                        if (matchingScore != null)
                        {
                            if (matchingScore.EffectiveDate == assetEffectiveDate.EffectiveDate)
                            {
                                scoreUid = matchingScore.ScoreUid;
                            }
                            else
                            {
                                // This condition is for cases where ou need to check historical (pre-migration scores that do not yet have a proper hash).
                                if (string.IsNullOrEmpty(matchingScore.VersionValueHash))
                                {
                                    var matchingScoreItemHash = string.Join(";", previousScoreItems.OrderBy(i => i.MetricAssetVersionUid).Select(i => $"{i.MetricAssetVersionUid}:{String.Format("{0:#,0.000}", i.AdjustedWeight)}"));
                                    matchingScoreItemHash = matchingScoreItemHash.GetSha1HashString();
                                    matchingScore.VersionValueHash = matchingScoreItemHash;
                                }

                                if (assetEffectiveDate.EffectiveDate > matchingScore.EffectiveDate && assetScore.VersionValueHash == matchingScore.VersionValueHash)
                                {
                                    scoreUid = matchingScore.ScoreUid;
                                }
                            }
                        }
                        assetScore.Uid = scoreUid;
                        
                        // Update the links with the chosen score Uid.
                        assetScoreItemLinks.ForEach(l => {
                            l.ScoreUid = assetScore.Uid;
                        });

                        assetScoreItemLinksToDelete.ForEach(l => {
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
                                    var uidsToRemove = assetScoreItems.Where(si => si.MetricAssetUid == g.MetricAssetUid).Select(i => i.Uid).ToList();
                                    assetScoreItemLinks.RemoveAll(l => uidsToRemove.Contains(l.ScoreItemUid));
                                    assetScoreItems.RemoveAll(si => uidsToRemove.Contains(si.Uid));
                                }
                            }
                        });

                        // Now add to master collection which will be sent to database.
                        scoreItemLinksToAdd.AddRange(assetScoreItemLinks);
                        scoreItemLinksToDelete.AddRange(assetScoreItemLinksToDelete);
                        scoresItemsToAdd.AddRange(assetScoreItems.Where(n => !scoresItemsToAdd.Any(e => e.Uid == n.Uid)));
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
                            scores.Columns.Add("VersionValueHash", typeof(string));

                            var scoreItems = new DataTable();
                            scoreItems.Columns.Add("Uid", typeof(Guid));
                            scoreItems.Columns.Add("UpdatedOn", typeof(DateTime));
                            scoreItems.Columns.Add("Value", typeof(bool));
                            scoreItems.Columns.Add("AdjustedWeight", typeof(decimal));
                            scoreItems.Columns.Add("RunDate", typeof(DateTime));
                            scoreItems.Columns.Add("AssetVersionUid", typeof(Guid));
                            scoreItems.Columns.Add("Evidence", typeof(string));
                            scoreItems.Columns.Add("ConditionUid", typeof(Guid));
                            scoreItems.Columns.Add("DecimalValue", typeof(float));
                            scoreItems.Columns.Add("AdjustedMaxWeight", typeof(decimal));

                            var scoreItemLinks = new DataTable();
                            scoreItemLinks.Columns.Add("ScoreUid", typeof(Guid));
                            scoreItemLinks.Columns.Add("ScoreItemUid", typeof(Guid));

                            var deleteScoreItemLinks = new DataTable();
                            deleteScoreItemLinks.Columns.Add("ScoreUid", typeof(Guid));
                            deleteScoreItemLinks.Columns.Add("ScoreItemUid", typeof(Guid));

                            var deleteScoreItemVersionLinks = new DataTable();
                            deleteScoreItemVersionLinks.Columns.Add("AssetVersionUid", typeof(Guid));

                            scoresToAdd.ForEach(s =>
                            {
                                var scoreRow = scores.NewRow();
                                scoreRow["Uid"] = s.Uid;
                                scoreRow["AssetUid"] = s.AssetUid;
                                scoreRow["EffectiveDate"] = s.EffectiveDate.Date;
                                scoreRow["Value"] = s.Value;
                                scoreRow["RunDate"] = s.RunDate;
                                scoreRow["AllocationUid"] = s.AllocationUid;
                                scoreRow["VersionValueHash"] = s.VersionValueHash;
                                scores.Rows.Add(scoreRow);
                            });

                            scoresItemsToAdd.ForEach(s =>
                            {
                                var scoreItemRow = scoreItems.NewRow();
                                scoreItemRow["Uid"] = s.Uid;
                                scoreItemRow["UpdatedOn"] = s.UpdatedOn;
                                scoreItemRow["Value"] = s.Value;
                                if (s.DecimalValue.HasValue)
                                    scoreItemRow["DecimalValue"] = s.DecimalValue;
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

                            scoreItemLinksToDelete.ForEach(s =>
                            {
                                var scoreRow = deleteScoreItemLinks.NewRow();
                                scoreRow["ScoreUid"] = s.ScoreUid;
                                scoreRow["ScoreItemUid"] = s.ScoreItemUid;
                                deleteScoreItemLinks.Rows.Add(scoreRow);
                            });

                            assetVersionCheckObjectTypes.ForEach(s =>
                            {
                                if (!s.Valid)
                                {
                                    var row = deleteScoreItemVersionLinks.NewRow();
                                    row["AssetVersionUid"] = s.AssetVersionUid;
                                    deleteScoreItemVersionLinks.Rows.Add(row);
                                }
                            });

                            await company.ExecuteAsync(
                               @"create table #Scores (
                                    Uid uniqueidentifier not null,
                                    AssetUid uniqueidentifier not null,
                                    EffectiveDate date not null,
                                    Value decimal(5,3) null,
                                    RunDate datetime not null,
                                    EndDate date null,
                                    AllocationUid uniqueidentifier null,
                                    VersionValueHash varchar(50) null
                                );
                                create table #ScoreUidSynchronization (
                                    GivenUid uniqueidentifier not null,
                                    ActualUid uniqueidentifier not null
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
	                                AdjustedMaxWeight decimal(5, 3) NULL,
                                    DecimalValue float NULL
                                );
                                create table #ScoreItemLinks (
                                    ScoreUid uniqueidentifier NOT NULL,
	                                ScoreItemUid uniqueidentifier NOT NULL
                                );
                                create table #ScoreItemLinksToDelete (
                                    ScoreUid uniqueidentifier NOT NULL,
	                                ScoreItemUid uniqueidentifier NOT NULL
                                );
                                create table #ScoreItemVersionLinksToDelete (
                                    AssetVersionUid uniqueidentifier NOT NULL
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
                                bulkCopy.ColumnMappings.Add("VersionValueHash", "VersionValueHash");

                                await bulkCopy.WriteToServerAsync(scores);
                            }

                            using (var bulkCopy = CreateBulkCopy(company, trans, "#ScoreItems"))
                            {
                                bulkCopy.ColumnMappings.Add("Uid", "Uid");
                                bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");
                                bulkCopy.ColumnMappings.Add("Value", "Value");
                                bulkCopy.ColumnMappings.Add("DecimalValue", "DecimalValue");
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

                            if (scoreItemLinksToDelete.Count > 0)
                            {
                                using (var bulkCopy = CreateBulkCopy(company, trans, "#ScoreItemLinksToDelete"))
                                {
                                    bulkCopy.ColumnMappings.Add("ScoreUid", "ScoreUid");
                                    bulkCopy.ColumnMappings.Add("ScoreItemUid", "ScoreItemUid");

                                    await bulkCopy.WriteToServerAsync(deleteScoreItemLinks);
                                }
                            }

                            if (assetVersionCheckObjectTypes.Count > 0)
                            {
                                using (var bulkCopy = CreateBulkCopy(company, trans, "#ScoreItemVersionLinksToDelete"))
                                {
                                    bulkCopy.ColumnMappings.Add("AssetVersionUid", "AssetVersionUid");

                                    await bulkCopy.WriteToServerAsync(deleteScoreItemVersionLinks);
                                }
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
                                "using (select Si.* from #Scores Si inner join metrics.Allocation A on A.Uid = Si.AllocationUid) as S " +
                                "on ((S.AllocationUid = T.AllocationUid and T.AssetUid = S.AssetUid and T.EffectiveDate = S.EffectiveDate) OR (T.Uid = S.Uid)) " +
                                "when matched then " +
                                "update set " +
                                "T.RunDate = S.RunDate, T.EndDate = S.EndDate, T.Value = S.Value, T.VersionValueHash = S.VersionValueHash " +
                                "when not matched then " +
                                "insert (Uid, AllocationUid, AssetUid, EffectiveDate, Value, RunDate, EndDate, VersionValueHash) " +
                                "values (S.Uid, S.AllocationUid, S.AssetUid, S.EffectiveDate, S.Value, S.RunDate, S.EndDate, S.VersionValueHash)" +
                                "output S.Uid, inserted.Uid into #ScoreUidSynchronization;", transaction: trans);

                            // Synchronize score Uids with temp table we are about to merge into Link and Item table.
                            await company.ExecuteAsync(@"
update T 
set T.ScoreUid = S.ActualUid 
from #ScoreItemLinks T 
inner join #ScoreUidSynchronization S on S.GivenUid = T.ScoreUid;

update T 
set T.ScoreUid = S.ActualUid 
from #ScoreItemLinksToDelete T 
inner join #ScoreUidSynchronization S on S.GivenUid = T.ScoreUid;

update T 
set T.Uid = S.ActualUid 
from #Scores T 
inner join #ScoreUidSynchronization S on S.GivenUid = T.Uid;

delete  I 
from    #ScoreItems I
        inner join #ScoreItemLinks L on L.ScoreItemUid = I.Uid
        inner join #Scores S on S.Uid = L.ScoreUid
        left join #ScoreUidSynchronization N on N.GivenUid = S.Uid
where   N.ActualUid is null;

delete  L 
from    #ScoreItemLinks L
        inner join #Scores S on S.Uid = L.ScoreUid
        left join #ScoreUidSynchronization N on N.GivenUid = S.Uid
where   N.ActualUid is null;", transaction: trans);

                            // Merge score items.
                            await company.ExecuteAsync(
                                "merge metrics.ScoreItem as T " +
                                "using #ScoreItems as S " +
                                "on (S.Uid = T.Uid) " +
                                "when matched then " +
                                "update set " +
                                "T.RunDate = S.RunDate, T.UpdatedOn = S.UpdatedOn, " +
                                "T.AssetVersionUid = S.AssetVersionUid, T.Value = S.Value, T.DecimalValue = S.DecimalValue, T.Evidence = S.Evidence, " +
                                "T.ConditionUid = S.ConditionUid, T.AdjustedWeight = S.AdjustedWeight, T.AdjustedMaxWeight = S.AdjustedMaxWeight " +
                                "when not matched then " +
                                "insert (UpdatedOn, Value, DecimalValue, AdjustedWeight, RunDate, Uid, AssetVersionUid, Evidence, ConditionUid, AdjustedMaxWeight) " +
                                "values (S.UpdatedOn, S.Value, S.DecimalValue, S.AdjustedWeight, S.RunDate, S.Uid, S.AssetVersionUid, S.Evidence, S.ConditionUid, S.AdjustedMaxWeight);", transaction: trans);

                            // Merge score Item Links.
                            await company.ExecuteAsync(
                                "merge metrics.ScoreItemLink as T " +
                                "using (select distinct ScoreUid, ScoreItemUid from #ScoreItemLinks) as S " +
                                "on (S.ScoreUid = T.ScoreUid and T.ScoreItemUid = S.ScoreItemUid) " +
                                "when not matched then " +
                                "insert (ScoreUid, ScoreItemUid) " +
                                "values (S.ScoreUid, S.ScoreItemUid);", transaction: trans);

                            if (scoreItemLinksToDelete.Count > 0)
                            {
                                // Delete now invalid score Item Links.
                                await company.ExecuteAsync(
                                    "delete T " +
                                    "from metrics.ScoreItemLink T " +
                                    "inner join #ScoreItemLinksToDelete S on (S.ScoreUid = T.ScoreUid and T.ScoreItemUid = S.ScoreItemUid);", transaction: trans);
                            }

                            if (deleteScoreItemVersionLinks.Rows.Count > 0)
                            {
                                // Delete now invalid score Item Links.
                                await company.ExecuteAsync(
                                    "delete T " +
                                    "from metrics.ScoreItemLink T " +
                                    "inner join #Scores S on S.Uid = T.ScoreUid " +
                                    "inner join metrics.ScoreItem I on I.Uid = T.ScoreItemUid " +
                                    "inner join #ScoreItemVersionLinksToDelete D on D.AssetVersionUid = I.AssetVersionUid;", transaction: trans);
                            }

                            // Clean out potentially old references to the same asset versions on a single score.
                            await company.ExecuteAsync(
                                @"
delete	T
from	metrics.ScoreItemLink T
		inner join	(
					select	*
					from	(
							select	L.*,
									ROW_NUMBER() OVER(PARTITION BY S.Uid, I.AssetVersionUid ORDER BY I.UpdatedOn desc) as RowNum
							from	#Scores S
									inner join metrics.ScoreItemLink L on L.ScoreUid = S.Uid
									inner join metrics.ScoreItem I on I.Uid = L.ScoreItemUid
							) O
					where	O.RowNum > 1
					) S on (S.ScoreUid = T.ScoreUid and S.ScoreItemUid = T.ScoreItemUid);", transaction: trans);

                            trans.Commit();

                            Db.SendScoreEventWithPayload(
                                Info.ExecutionUid,
                                ScoreQueueChangeType.WorkflowCheck,
                                scoresToAdd.Select(i => new ScoreCreatedModel { AllocationUid = i.AllocationUid, AssetUid = i.AssetUid, EffectiveDate = i.EffectiveDate }).ToList(),
                                Info.StartedOn
                                );
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            updateExecution(company, executionRecord, false);
                            throw ex;
                        }
                    }
                }

                updateExecution(company, executionRecord, true);
            }
        }

        void updateExecution(SqlConnection Db, ApiExecution executionRecord, bool completed) 
        {
            try
            {
                // Reset on failure so it does not interfere with any other executing thread.
                if (executionRecord != null)
                {
                    executionRecord.MarkedForProcessing = false;
                    executionRecord.ProcessingStartedOn = null;
                    if (completed)
                    {
                        executionRecord.CompletedOn = DateTime.UtcNow;
                        executionRecord.State = State.InActive;
                    }
                    Db.Execute("update api.Execution set MarkedForProcessing = 0, ProcessingStartedOn = null, CompletedOn = @dt, [State] = 4 where ExecutionID = @id", new { dt = executionRecord.CompletedOn, id = executionRecord.ExecutionID });
                }
            }
            catch
            {
                //do nothing.
            }
        }
    }
}
