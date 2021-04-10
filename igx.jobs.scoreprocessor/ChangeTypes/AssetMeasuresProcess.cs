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
        #region Sql Constants

        const string ALLOCATION_SQL = @"
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
		) Al on Al.AllocationUid = A.AllocationUid and ( (Al.EffectiveDate between V.EffectiveDate and V.EffectiveEndDate) or (Al.EffectiveDate >= V.EffectiveDate and V.EffectiveEndDate is null) )";

        const string SUPPORTING_DATA_SQL = @"
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
                S.EffectiveDate,
                S.EndDate,
                Si.AssetVersionUid as MetricAssetVersionUid,
		        Si.ConditionUid,
		        Si.Value,
		        Si.AdjustedWeight,
		        Si.AdjustedMaxWeight,
                Si.DecimalValue,
                Si.Evidence,
                Si.OtherConditions,
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
where   O.RowNum = 1;";

        #endregion

        ApiExecution executionRecord;
        AssetVersionCheckObjectTypes assetVersionCheckObjectTypes;
        List<Score> scoresToAdd;
        List<ScoreItem> scoresItemsToAdd;
        List<ScoreItemLink> scoreItemLinksToAdd;
        List<ScoreItemLink> scoreItemLinksToDelete;

        public AssetMeasuresProcess()
        {
            // Store the Governance check values and whether they are valid, so we do not have to check individual measure validity more than once.
            assetVersionCheckObjectTypes = new AssetVersionCheckObjectTypes();

            resetLists();
        }

        public async Task Run()
        {
            var assetMeasures = await Storage.DeserializeJsonObjectFromBlobAsync<List<AssetMeasureModel>>(Info.StorageFolder, Info.StorageFile);

            if(assetMeasures == null)
            {
                throw new ArgumentNullException("assetMeasures","Cannot load score file from storage");
            }

            var Db = GetCompanyContext();
            using (var company = GetEnvironmentConnection())
            {
                executionRecord = company.Query<ApiExecution>("select * from api.Execution where ExecutionID = @id", new { id = Info.ExecutionUid }).SingleOrDefault();

                if (executionRecord == null && assetMeasures.Count > 10)
                {
                    executionRecord = new ApiExecution
                    {
                        ExecutionID = Info.ExecutionUid,
                        StartedOn = Info.StartedOn,
                        ResourceID = Info.ResourceID ?? 0,
                        Method = "SCORE",
                        State = State.Unknown,
                        Total = assetMeasures.Count
                    };
                    Db.Add(executionRecord);
                }

                if (executionRecord != null)
                {
                    if (executionRecord.Total != assetMeasures.Count)
                    {
                        executionRecord.Total = assetMeasures.Count;
                        updateExecution(company, executionRecord, false);
                    }

                    // This means that the original execution came in via one of the external measure/score endpoints.
                    // We need to check whether any other execution is running.

                    // Wait a moment in case there are multiple queue messages
                    //Thread.Sleep(new Random().Next(2000, 7000));

                    var currentlyRunningExecutions = company.Query<bool>(@"
select  cast(iif(count(1) > 0, 1, 0) as bit) 
from    api.Execution
where   ExecutionID <> @id 
        and [Method] = 'SCORE'
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
                    updateExecution(company, executionRecord, false);
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

                    var supportingDataRequest = await company.QueryMultipleAsync(SUPPORTING_DATA_SQL, transaction: trans, commandTimeout: 900);
                    models = supportingDataRequest.Read<ExternalMeasureResultsCreatedModel>().ToList();
                    fieldTypes = supportingDataRequest.Read<FieldType>().ToList();
                    fields = supportingDataRequest.Read<AssetMeasuresProcessField>().ToList();
                    allPreviousScoreItems = supportingDataRequest.Read<AssetAllocationPreviousResult>().ToList();
                    matchingScores = supportingDataRequest.Read<MatchingScoreModel>().ToList();

                    // Get the full list of relevant measures based on the allocations and effective dates.
                    var allocationRequest = await company.QueryAsync<AllocationDataModel>(ALLOCATION_SQL, transaction: trans);
                    allocations = allocationRequest.ToList();

                    trans.Commit();
                }
                
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

                var scoreItems = new List<StagingScoreItem>();
                var scoreCount = 0;
                uniqueAssetCombinations.ForEach(async assetEffectiveDate =>
                {
                    scoreCount++;

                    // The local lists below keep track of score items and links to add for a specific score (asset / effective date / allocation combination).
                    var assetScoreItems = new List<StagingScoreItem>();
                    //var ignoredAssetScoreItems = new List<ScoreItem>();
                    //var assetScoreItems = new List<ScoreItem>();
                    //var assetScoreItemLinks = new List<ScoreItemLink>();
                    //var assetScoreItemLinksToDelete = new List<ScoreItemLink>();

                    var allMeasures = allocations.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var incomingMeasureResults = models.Where(i => i.AllocationUid == assetEffectiveDate.AllocationUid && i.AssetUid == assetEffectiveDate.AssetUid && i.EffectiveDate == assetEffectiveDate.EffectiveDate).ToList();
                    var assetFields = fields.Where(f => f.Assetuid == assetEffectiveDate.AssetUid).ToList();
                    var previousScoreItems = allPreviousScoreItems.Where(p => p.AssetUid == assetEffectiveDate.AssetUid && p.EffectiveDate.Date <= assetEffectiveDate.EffectiveDate.Date).ToList();

                    // Add default raw items to represent the measure groups that may be present.
                    incomingMeasureResults.AddRange(allMeasures.Where(am => am.IsGroup).Select(am => new ExternalMeasureResultsCreatedModel
                    {
                        AllocationUid = am.AllocationUid,
                        AssetTypeId = assetEffectiveDate.AssetTypeId,
                        AssetUid = assetEffectiveDate.AssetUid,
                        MetricAssetUid = am.MetricAssetUid,
                        MetricAssetVersionUid = am.MetricAssetVersionUid,
                        EffectiveDate = assetEffectiveDate.EffectiveDate,
                        Result = false
                    }));

                    allMeasures.ForEach(allMeasure =>
                    {
                        bool measureDeleted = false;
                        var conditionValidator = CheckMeasureConditions(assetFields, fieldTypes, allMeasure, true);
                        var previousScoreItem = previousScoreItems.Where(p => p.MetricAssetUid == allMeasure.MetricAssetUid).OrderByDescending(p => p.EffectiveDate).FirstOrDefault();

                        if (conditionValidator.ConditionMet)
                        {
                            var incomingMeasureResult = incomingMeasureResults.FirstOrDefault(p => p.MetricAssetUid == allMeasure.MetricAssetUid);

                            if (assetVersionCheckObjectTypes.ShouldContinueAnalysis(allMeasure.MetricAssetVersionUid))
                            {
                                string definitionJson = allMeasure.Definition;
                                if (string.IsNullOrEmpty(definitionJson))
                                {
                                    definitionJson = "{}";
                                }
                                var definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(definitionJson);

                                var scoreItem = new StagingScoreItem//ScoreItem
                                {
                                    AllocationUid = assetEffectiveDate.AllocationUid,
                                    AssetUid = assetEffectiveDate.AssetUid,
                                    EffectiveDate = assetEffectiveDate.EffectiveDate,
                                    RawWeight = conditionValidator.SelectedWeight, // this is the measure/condition weight, which will need to be re-adjusted at the end.
                                    MeasureUid = allMeasure.MetricAssetUid,
                                    MeasureVersionUid = allMeasure.MetricAssetVersionUid,
                                    ConditionUid = conditionValidator.SelectedConditionUid,
                                    OtherConditions = JsonConvert.SerializeObject(conditionValidator.ExtraneousConditions)
                                };

                                if (incomingMeasureResult != null)
                                {   // Now perform analysis based on score type.
                                    switch (allMeasure.ScoreType)
                                    {
                                        case ScoreType.DataQuality:
                                            #region
                                            var dqDefinition = definition.DataQuality;
                                            // Do something with rollups here.
                                            if (allMeasure.RollupPath == null || dqDefinition == null)
                                            {
                                                var error = "";
                                                error += (allMeasure.RollupPath == null) ? "Rollup Path is invalid. An asset type or relationship type may have been removed. " : "";
                                                error += (dqDefinition == null) ? "Measure definition is invalid. Please check and re-save the measure definition, and try again." : "";
                                                scoreItem.Value = false;
                                                scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = error });
                                            }
                                            else
                                            {
                                                if (allMeasure.RollupPath.SegmentLinks == null)
                                                {
                                                    scoreItem.Value = false;
                                                    scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = "Rollup Path Segments do not exist. An asset type or relationship type may have been removed." });
                                                }
                                                else
                                                {
                                                    var dqQueryDetail = dqMeasureQueryLibrary.FirstOrDefault(dq => dq.AssetVersionRollupPathUid == allMeasure.RollupPath.AssetVersionRollupPathUid);
                                                    if (dqQueryDetail == null)
                                                    {
                                                        dqQueryDetail = Db.BuildDataQualityMeasureQueryModel(MetricDataQualityQueryType.MeasureResults_For_Calculation, allMeasure.RollupPath.AssetVersionRollupPathUid);
                                                        dqMeasureQueryLibrary.Add(dqQueryDetail); // Add to library for future reference.
                                                    }

                                                    try
                                                    {
                                                        var rollupPathResults = Db.GetDataQualityMeasureQueryResultModels(dqQueryDetail, incomingMeasureResult.AssetUid, assetEffectiveDate.EffectiveDate);

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

                                                            if (allMeasure.IsThresholdBased)
                                                            {
                                                                scoreItem.DecimalValue = resultOperationValue;
                                                                scoreItem.Value = (allMeasure.Threshold <= resultOperationValue);
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
                                                            conditionValidator.ConditionMet = false; // GOV-13324 - Since no rules are linked via path, then this is not really a qualifying measure.
                                                        }

                                                        var evidence = rollupPathResults.Select(rp => new DataQualityEvidenceModel
                                                        {
                                                            ErrorMessage = null,
                                                            IsError = false,
                                                            ResultResultUids = rp.StructuredResults.Select(r => r.Uid).ToList(),
                                                            RollupPath = rp.StructuredPath
                                                        });

                                                        scoreItem.Evidence = JsonConvert.SerializeObject(evidence);

                                                        // GOV-13324 - Keep track of this so we do not add a default below.
                                                        if (!conditionValidator.ConditionMet)
                                                        {
                                                            scoreItem.Action = 'D';
                                                            //ignoredAssetScoreItems.Add(scoreItem);
                                                        }
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
                                                    scoreItem.Value = incomingMeasureResult.Result;
                                                    break;
                                                case MetricGovernanceCheckType.Field:
                                                    if (gDefinition.Field != null)
                                                    {
                                                        var assetFieldType = fieldTypes.FirstOrDefault(i => i.Name == gDefinition.Field.FieldTypeName);
                                                        string dataType = (assetFieldType != null) ? assetFieldType.Type : "Text";
                                                        bool allowMultipleValues = (assetFieldType != null) ? assetFieldType.AllowMultipleValues : false;

                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(gDefinition, allMeasure.MetricAssetVersionUid, (assetFieldType != null));

                                                        var assetFieldForFieldCheck = assetFields.FirstOrDefault(f => f.FieldTypeName == gDefinition.Field.FieldTypeName);

                                                        scoreItem.Value = gDefinition.Field.Operator.TestTwoValues(dataType, allowMultipleValues, gDefinition.Field.Values, ((assetFieldForFieldCheck == null) ? null : assetFieldForFieldCheck.Values));
                                                    }
                                                    break;
                                                case MetricGovernanceCheckType.Owner:
                                                    if (gDefinition.Owner != null)
                                                    {
                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(gDefinition, allMeasure.MetricAssetVersionUid, null);

                                                        string trueValue = (gDefinition.Owner.Operator == Operator.Populated) ? "1" : "0";
                                                        string falseValue = (gDefinition.Owner.Operator == Operator.Populated) ? "0" : "1";
                                                        scoreItem.Value = company.Query<bool>(
                                                            $"select cast(iif(count(1) > 0, {trueValue}, {falseValue}) as bit) " +
                                                            "from ResponsibilityDetail R " +
                                                            "inner join ResponsibilityType T on T.ID = R.ResponsibilityTypeID and T.Uid = @ResponsibilityTypeUid " +
                                                            "where exists ( select 1 from Asset where Uid = @AssetUid and ( (ID = R.AssetID and R.AssetID <> 0) or (AssetTypeID = R.AssetTypeID and R.AssetID = 0) ) )",
                                                            new { gDefinition.Owner.ResponsibilityTypeUid, incomingMeasureResult.AssetUid }, commandTimeout: 90
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
                                                        assetVersionCheckObjectTypeAction(gDefinition, allMeasure.MetricAssetVersionUid, null);

                                                        var predicateExistenceSql = "select cast(iif(sum(bit1) > 0, 1, 0) as bit) from (" +
                                                            "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where PredicateUid = @PredicateUid and SubjectUid = @AssetUid  " +
                                                            "union all " +
                                                            "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where PredicateUid = @PredicateUid and ObjectUid = @AssetUid " +
                                                            ") a";

                                                        switch (gDefinition.Predicate.Operator)
                                                        {
                                                            case Operator.Populated:
                                                                scoreItem.Value = company.Query<bool>(predicateExistenceSql, new { gDefinition.Predicate.PredicateUid, incomingMeasureResult.AssetUid }, commandTimeout: 90).Single();
                                                                break;
                                                            case Operator.NotPopulated:
                                                                scoreItem.Value = !company.Query<bool>(predicateExistenceSql, new { gDefinition.Predicate.PredicateUid, incomingMeasureResult.AssetUid }, commandTimeout: 90).Single();
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
                                                        assetVersionCheckObjectTypeAction(gDefinition, allMeasure.MetricAssetVersionUid, null);

                                                        var operatorSql = "";
                                                        var bitSql = "";
                                                        object parameters = null;

                                                        if (gDefinition.Relation.Values == null)
                                                        {
                                                            gDefinition.Relation.Values = new List<string>();
                                                        }
                                                        if (gDefinition.Relation.Values.Count == 0)
                                                        {
                                                            gDefinition.Relation.Values.Add(Guid.Empty.ToString());
                                                        }

                                                        switch (gDefinition.Relation.Operator)
                                                        {
                                                            case Operator.Equals:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, incomingMeasureResult.AssetUid, ValueUid = Guid.Parse(gDefinition.Relation.Values[0]) };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @AssetUid and ObjectUid = @ValueUid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @ValueUid and ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.In:
                                                                parameters = new
                                                                {
                                                                    gDefinition.Relation.IntersectTypeUid,
                                                                    incomingMeasureResult.AssetUid,
                                                                    Uids = gDefinition.Relation.Values.Select(u => new { Uid = Guid.Parse(u) }).AsTableValuedParameter("dbo.UidTable", new List<string>() { "Uid" })
                                                                };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = @AssetUid and I.ObjectUid = U.Uid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = U.Uid and I.ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.NotEquals:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, incomingMeasureResult.AssetUid, ValueUid = Guid.Parse(gDefinition.Relation.Values[0]) };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @AssetUid and ObjectUid = @ValueUid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @ValueUid and ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.NotIn:
                                                                parameters = new
                                                                {
                                                                    gDefinition.Relation.IntersectTypeUid,
                                                                    incomingMeasureResult.AssetUid,
                                                                    Uids = gDefinition.Relation.Values.Select(u => new { Uid = Guid.Parse(u) }).AsTableValuedParameter("dbo.UidTable", new List<string>() { "Uid" })
                                                                };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = @AssetUid and I.ObjectUid = U.Uid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = U.Uid and I.ObjectUid = @AssetUid ";
                                                                break;
                                                            case Operator.NotPopulated:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, incomingMeasureResult.AssetUid };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @AssetUid  " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and ObjectUid = @AssetUid ";
                                                                break;
                                                            default: // case Operator.Populated:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, incomingMeasureResult.AssetUid };
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
                                        default:
                                            scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = "Unknown score type." });
                                            scoreItem.Value = false;
                                            break;
                                    }

                                    Guid scoreItemUid = Guid.NewGuid();
                                    if (previousScoreItem != null)
                                    {
                                        if (previousScoreItem.Value == scoreItem.Value && previousScoreItem.AdjustedWeight == scoreItem.AdjustedWeight)
                                        {   // Since value is the same, just link the existing score item to score.
                                            scoreItemUid = previousScoreItem.ScoreItemUid;
                                        }
                                        else
                                        {
                                            if (previousScoreItem.EffectiveDate.Date == assetEffectiveDate.EffectiveDate.Date) 
                                            {
                                                if (previousScoreItem.UsedInOtherScores)
                                                {   // The score item is used in an earlier score, so we need to create a new score item, AND detach this score from the now old score item.
                                                    //assetScoreItemLinksToDelete.Add(new ScoreItemLink { ScoreItemUid = previousScoreItem.ScoreItemUid });
                                                    scoreItem.Action = 'D';
                                                }
                                                else
                                                {   // Not used in any other score, so we are OK to update the value on this score item.
                                                    scoreItemUid = previousScoreItem.ScoreItemUid;
                                                }
                                            }
                                        }
                                    }
                                    scoreItem.ScoreItemUid = scoreItemUid;

                                    assetScoreItems.Add(scoreItem);
                                    //assetScoreItemLinks.Add(new ScoreItemLink { ScoreItemUid = scoreItem.Uid });
                                }
                                else
                                {   // No current results sent in for existing data load, so we need to carry forward the previous score items to create a complete score.
                                    if (definition.Governance != null)
                                    {
                                        if (definition.Governance.Check == MetricGovernanceCheckType.Field)
                                        {
                                            if (!fieldTypes.Any(f => f.Name == definition.Governance.Field.FieldTypeName))
                                            {
                                                measureDeleted = true;
                                            }
                                        }
                                    }

                                    if (previousScoreItem != null)
                                    {
                                        // Look up to see if there is an existing score item for this measure, and use that value.
                                        scoreItem.ScoreItemUid = Guid.NewGuid();

                                        // If same measure version, then use existing Guid.
                                        if (previousScoreItem.MetricAssetVersionUid == allMeasure.MetricAssetVersionUid)
                                        {
                                            scoreItem.ScoreItemUid = previousScoreItem.ScoreItemUid;
                                        }
                                        scoreItem.Value = previousScoreItem.Value;
                                        scoreItem.DecimalValue = previousScoreItem.DecimalValue;
                                        scoreItem.Evidence = previousScoreItem.Evidence;
                                        scoreItem.ConditionUid = previousScoreItem.ConditionUid;
                                        scoreItem.OtherConditions = previousScoreItem.OtherConditions;

                                        assetScoreItems.Add(scoreItem);
                                        //assetScoreItemLinks.Add(new ScoreItemLink { ScoreItemUid = scoreItem.Uid });
                                    }
                                }
                            }
                            else
                            {
                                measureDeleted = true;
                            }
                        }
                        else 
                        {
                            measureDeleted = true;
                        }

                        if (measureDeleted && previousScoreItem != null && previousScoreItem.EffectiveDate == assetEffectiveDate.EffectiveDate)
                        {   // Remove from existing score.
                            scoreItemLinksToDelete.Add(new ScoreItemLink { ScoreItemUid = previousScoreItem.ScoreItemUid, ScoreUid = previousScoreItem.ScoreUid });

                            // Now see if we should delete the group, if no other children are present for it.
                            if (allMeasure.MetricParentAssetUid.HasValue)
                            {
                                var groupScoreItem = previousScoreItems.FirstOrDefault(i => i.MetricAssetUid == allMeasure.MetricParentAssetUid);
                                if (groupScoreItem != null)
                                {
                                    if (!(from a in assetScoreItems
                                          join all in allMeasures on a.MeasureUid equals all.MetricAssetUid
                                          where all.MetricParentAssetUid == allMeasure.MetricParentAssetUid
                                          where all.MetricAssetUid != previousScoreItem.MetricAssetUid
                                          select all.MetricAssetUid).Any())
                                    {
                                        
                                        //scoreItemLinksToDelete.Add(new ScoreItemLink { ScoreItemUid = groupScoreItem.ScoreItemUid, ScoreUid = previousScoreItem.ScoreUid });
                                    }
                                }
                            }
                        }
                    });

                    // Perform final score calculations for this asset/effective date combination. If no data for asset/effective date, then do not even bother to recalculate anything for it.
                    //if (scoreItems.Count > 0)
                    //{
                    //    assetScoreItems.RemoveAll(s => ignoredAssetScoreItems.Any(d => d.AssetVersionUid == s.AssetVersionUid)); 

                        var score = AdjustScoreItemWeights(allMeasures, assetScoreItems);

                    //    var matchingScore = matchingScores.FirstOrDefault(s => s.AllocationUid == assetEffectiveDate.AllocationUid && s.AssetUid == assetEffectiveDate.AssetUid);

                    //    // Helps to determine if we should create a new score record.
                    //    var scoreItemHash = string.Join(";", assetScoreItems.OrderBy(i => i.AssetVersionUid).Select(i => $"{i.AssetVersionUid}:{String.Format("{0:#,0.000}", i.AdjustedWeight ?? 0)}"));
                    //    scoreItemHash = scoreItemHash.GetSha1HashString();

                    //    Score assetScore = new Score
                    //    {
                    //        EffectiveDate = assetEffectiveDate.EffectiveDate,
                    //        AllocationUid = assetEffectiveDate.AllocationUid,
                    //        AssetUid = assetEffectiveDate.AssetUid,
                    //        RunDate = DateTime.UtcNow,
                    //        Value = score,
                    //        VersionValueHash = scoreItemHash
                    //    };

                    //    // If there is a matching score in the system, update the Uid 
                    //    var scoreUid = Guid.NewGuid();
                    //    if (matchingScore != null)
                    //    {
                    //        if (matchingScore.EffectiveDate == assetEffectiveDate.EffectiveDate)
                    //        {
                    //            scoreUid = matchingScore.ScoreUid;
                    //        }
                    //        else
                    //        {
                    //            // This condition is for cases where you need to check historical (pre-migration scores that do not yet have a proper hash).
                    //            if (string.IsNullOrEmpty(matchingScore.VersionValueHash))
                    //            {
                    //                var matchingScoreItemHash = string.Join(";", previousScoreItems.OrderBy(i => i.MetricAssetVersionUid).Select(i => $"{i.MetricAssetVersionUid}:{String.Format("{0:#,0.000}", i.AdjustedWeight)}"));
                    //                matchingScoreItemHash = matchingScoreItemHash.GetSha1HashString();
                    //                matchingScore.VersionValueHash = matchingScoreItemHash;
                    //            }

                    //            if (assetEffectiveDate.EffectiveDate > matchingScore.EffectiveDate && assetScore.VersionValueHash == matchingScore.VersionValueHash)
                    //            {
                    //                scoreUid = matchingScore.ScoreUid;
                    //            }
                    //        }
                    //    }
                    //    assetScore.Uid = scoreUid;

                    //    // Update the links with the chosen score Uid.
                    //    assetScoreItemLinks.ForEach(l => {
                    //        l.ScoreUid = assetScore.Uid;
                    //    });

                    //    assetScoreItemLinksToDelete.ForEach(l => {
                    //        l.ScoreUid = assetScore.Uid;
                    //    });

                    //    // Empty group deletion.
                    //    var uidsToRemove = getEmptyMeasureGroups(allMeasures, assetScoreItems);
                    //    if (uidsToRemove.Count > 0)
                    //    {
                    //        assetScoreItemLinks.RemoveAll(l => uidsToRemove.Contains(l.ScoreItemUid));
                    //        assetScoreItems.RemoveAll(si => uidsToRemove.Contains(si.Uid));
                    //    }

                    //    // Now add to master collection which will be sent to database.
                    //    scoreItemLinksToAdd.AddRange(assetScoreItemLinks);
                    //    scoreItemLinksToDelete.AddRange(assetScoreItemLinksToDelete);
                    //    scoresItemsToAdd.AddRange(assetScoreItems.Where(n => !scoresItemsToAdd.Any(e => e.Uid == n.Uid)));
                    //    if (scoreItemLinksToAdd.Count > 0 || scoreItemLinksToDelete.Count > 0 || scoresItemsToAdd.Count > 0)
                    //    {
                    //        scoresToAdd.Add(assetScore);
                    //    }
                    //}
                    //Add to main list.
                    scoreItems.AddRange(assetScoreItems);

                    // Now add remaining scores via a transaction.
                    if (scoreCount % 250 == 0)//(scoresToAdd.Count % 250 == 0)
                    {
                        addScoresToEnvironmentDatabase(scoreItems);
                        scoreItems.Clear();
                        //await Db.SendContinuingScoreEventWithPayload(
                        //    ScoreQueueChangeType.WorkflowCheck,
                        //    scoresToAdd.Select(i => new ScoreCreatedModel { AllocationUid = i.AllocationUid, AssetUid = i.AssetUid, EffectiveDate = i.EffectiveDate }).ToList(),
                        //    Info.ExecutionUid,
                        //    Info.StartedOn
                        //    );
                        //resetLists();
                    }
                });

                // Now add remaining scores via a transaction.
                if (scoreItems.Count > 0)
                {
                    addScoresToEnvironmentDatabase(scoreItems);
                    //await Db.SendContinuingScoreEventWithPayload(
                    //    ScoreQueueChangeType.WorkflowCheck,
                    //    scoresToAdd.Select(i => new ScoreCreatedModel { AllocationUid = i.AllocationUid, AssetUid = i.AssetUid, EffectiveDate = i.EffectiveDate }).ToList(),
                    //    Info.ExecutionUid,
                    //    Info.StartedOn
                    //    );
                    //resetLists();
                    scoreItems.Clear();
                }

                updateExecution(company, executionRecord, true);
            }
        }

        bool addScoresToEnvironmentDatabase(List<StagingScoreItem> items) 
        {
            bool success = false;
            using (var company = GetEnvironmentConnection())
            {
                if (company.State != ConnectionState.Open)
                    company.Open();

                using (var trans = company.BeginTransaction())
                {
                    try
                    {
                        var itemsTable = new DataTable();
                        itemsTable.Columns.Add("AllocationUid", typeof(Guid));
                        itemsTable.Columns.Add("AssetUid", typeof(Guid));
                        itemsTable.Columns.Add("MeasureUid", typeof(Guid));
                        itemsTable.Columns.Add("MeasureVersionUid", typeof(Guid));
                        itemsTable.Columns.Add("EffectiveDate", typeof(DateTime));

                        itemsTable.Columns.Add("Value", typeof(bool));
                        itemsTable.Columns.Add("DecimalValue", typeof(decimal));
                        itemsTable.Columns.Add("RawWeight", typeof(decimal));
                        itemsTable.Columns.Add("ConditionUid", typeof(Guid));
                        itemsTable.Columns.Add("Evidence", typeof(string));
                        itemsTable.Columns.Add("OtherConditions", typeof(string));

                        items.ForEach(s =>
                        {
                            var itemRow = itemsTable.NewRow();
                            itemRow["AllocationUid"] = s.AllocationUid;
                            itemRow["AssetUid"] = s.AssetUid;
                            itemRow["MeasureUid"] = s.MeasureUid;
                            itemRow["MeasureVersionUid"] = s.MeasureVersionUid;
                            itemRow["EffectiveDate"] = s.EffectiveDate;
                            itemRow["Value"] = s.Value;
                            if (s.DecimalValue.HasValue)
                            {
                                itemRow["DecimalValue"] = s.DecimalValue;
                            }
                            if (s.RawWeight.HasValue)
                            {
                                itemRow["RawWeight"] = s.RawWeight;
                            }
                            if (s.ConditionUid.HasValue)
                            {
                                itemRow["ConditionUid"] = s.ConditionUid;
                            }
                            itemRow["Evidence"] = s.Evidence ?? "{}";
                            itemRow["OtherConditions"] = s.OtherConditions ?? "[]";

                            itemsTable.Rows.Add(itemRow);
                        });

                        company.Execute(@"
CREATE TABLE #StagingScoreItem (
	AllocationUid uniqueidentifier NOT NULL,
    AssetUid uniqueidentifier NOT NULL,
	MeasureUid uniqueidentifier NOT NULL,
	MeasureVersionUid uniqueidentifier NOT NULL,
	EffectiveDate datetime NOT NULL,
	[Value] bit NULL,
	DecimalValue float NULL,
	RawWeight decimal(8, 6) NULL,
	ConditionUid uniqueidentifier NULL,
	Evidence nvarchar(max) NOT NULL,
	OtherConditions nvarchar(max) NOT NULL
)", transaction: trans);

                        using (var bulkCopy = CreateBulkCopy(company, trans, "#StagingScoreItem"))
                        {
                            bulkCopy.ColumnMappings.Add("AllocationUid", "AllocationUid");
                            bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                            bulkCopy.ColumnMappings.Add("MeasureUid", "MeasureUid");
                            bulkCopy.ColumnMappings.Add("MeasureVersionUid", "MeasureVersionUid");
                            bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                            bulkCopy.ColumnMappings.Add("Value", "Value");
                            bulkCopy.ColumnMappings.Add("DecimalValue", "DecimalValue");
                            bulkCopy.ColumnMappings.Add("RawWeight", "RawWeight");
                            bulkCopy.ColumnMappings.Add("ConditionUid", "ConditionUid");
                            bulkCopy.ColumnMappings.Add("Evidence", "Evidence");
                            bulkCopy.ColumnMappings.Add("OtherConditions", "OtherConditions");

                            bulkCopy.WriteToServer(itemsTable);
                        }

                        company.Execute(@"
merge [metrics].[StagingScoreItem] as T
using #StagingScoreItem as S
on (S.AssetUid = T.AssetUid and S.MeasureUid = T.MeasureUid and S.EffectiveDate = T.EffectiveDate)
when matched then 
update set 
    T.MeasureVersionUid = S.MeasureVersionUid,
	T.[Value] = S.[Value],
	T.DecimalValue = S.DecimalValue,
	T.RawWeight = S.RawWeight,
	T.ConditionUid = S.ConditionUid,
	T.Evidence = S.Evidence,
	T.OtherConditions = S.OtherConditions,
    T.AllocationUid = S.AllocationUid
when not matched then
    insert (
        AllocationUid, AssetUid, MeasureUid, MeasureVersionUid, 
        EffectiveDate, [Value], DecimalValue, RawWeight, 
        ConditionUid, Evidence, OtherConditions
    ) values (
        S.AllocationUid, S.AssetUid, S.MeasureUid, S.MeasureVersionUid, 
        S.EffectiveDate, S.[Value], S.DecimalValue, S.RawWeight, 
        S.ConditionUid, S.Evidence, S.OtherConditions
    );", transaction: trans);

                        // Call the new procedure here.

                        trans.Commit();

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        if (executionRecord != null)
                        {
                            updateExecution(company, executionRecord, false, ex);
                        }
                        throw ex;
                    }
                }

                if (success && executionRecord != null)
                {
                    executionRecord.Processed += scoresToAdd.Count;
                    updateExecution(company, executionRecord);
                }

                company.Close();
            }
            return success;
        }

        void resetLists()
        {
            scoresToAdd = new List<Score>();
            scoresItemsToAdd = new List<ScoreItem>();
            scoreItemLinksToAdd = new List<ScoreItemLink>();
            scoreItemLinksToDelete = new List<ScoreItemLink>();
        }
    }
}
