using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using d360.model;
using Dapper;
using igx.jobs.scoreprocessor.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
            select	M.AllocationUid, P.EffectiveDate
            from	metrics.ExecutionItem I
		            cross apply openjson(Payload)  with ( EffectiveDate date '$.EffectiveDate', Measures nvarchar(max) '$.Measures' AS JSON ) P 
		            cross apply openjson(P.Measures) with ( AllocationUid uniqueidentifier '$.AllocationUid' ) M 
            where	I.ExecutionID = @ExecutionID
		            and I.ChangeType = 0
            group by M.AllocationUid, P.EffectiveDate
		) Al on Al.AllocationUid = A.AllocationUid and ( (Al.EffectiveDate between V.EffectiveDate and V.EffectiveEndDate) or (Al.EffectiveDate >= V.EffectiveDate and V.EffectiveEndDate is null) )";

        const string SUPPORTING_DATA_SQL = @"
select  * 
from    FieldType 
where   AssetTypeID in (
            select  A.AssetTypeId 
            from    metrics.ExecutionItem I
		            cross apply openjson(Payload) with ( AssetUid uniqueidentifier '$.AssetUid' ) P 
		            inner join Asset A on A.Uid = P.AssetUid
            where	I.ExecutionID = @ExecutionID and I.ChangeType = 0
        );";

        #endregion
        
        AssetVersionCheckObjectTypes assetVersionCheckObjectTypes;

        public AssetMeasuresProcess()
        {
            // Store the Governance check values and whether they are valid, so we do not have to check individual measure validity more than once.
            assetVersionCheckObjectTypes = new AssetVersionCheckObjectTypes();
        }

        public async Task Run()
        {
            var Db = GetCompanyContext();
            using (var company = GetEnvironmentConnection())
            {
                if (company.State != ConnectionState.Open)
                    company.Open();

                ExecutionRecord = getExecution(company);
                checkIfOtherRunningExecutions(company);

                var fieldTypesRequest = await company.QueryAsync<FieldType>(SUPPORTING_DATA_SQL, new { ExecutionID = ExecutionRecord.ID }, commandTimeout: 900);
                var fieldTypes = fieldTypesRequest.ToList();

                // Get the full list of relevant measures based on the allocations and effective dates.
                var allocationRequest = await company.QueryAsync<AllocationDataModel>(ALLOCATION_SQL, new { ExecutionID = ExecutionRecord.ID });
                var allocations = allocationRequest.ToList();

                Action<Object, MetricAssetDefinitionGovernanceViewModel, Guid, bool?> assetVersionCheckObjectTypeAction = (Object locker, MetricAssetDefinitionGovernanceViewModel governance, Guid metricAssetVersionUid, bool? overrideBoolValue) => {
                    if (assetVersionCheckObjectTypes.OkToAddToList(locker, metricAssetVersionUid))
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

                        lock (locker)
                        {
                            assetVersionCheckObjectTypes.Add(assetVersionCheckObjectType);
                        }
                    }
                };

                var dqMeasureQueryLibrary = new List<DataQualityMeasureQueryModel>();

                var scoreItems = new List<StagingScoreItem>();
                var scoreCount = 0;

                var stopwatch = new Stopwatch();
                int loopCount = 1;
                int loopElapsedSeconds = 0;
                var executionItems = getExecutionItems(company, 0);
                while (executionItems.Count > 0)
                {
                    stopwatch.Start();

                    var executionItemSubset = executionItems.Select(exItem => new { Item = exItem.GetPayload<AssetMeasureModel>(), exItem.RowNumber }).ToList();

                    // Get all fields for this set.
                    var setFields = company.Query<AssetMeasuresProcessField>(@"
    select	A.Uid as AssetUid,
	    FT.ID as FieldTypeID,
        FT.Name as FieldTypeName,
	    COALESCE (V.LookupValues, F.Value, F.FormattedValue, FT.DefaultValue) as [Values] 
    from	Asset A 
	    inner join FieldType FT ON FT.AssetTypeID = A.AssetTypeID 
	    left join Field F ON F.FieldTypeID = FT.ID AND F.ObjectType = A.Object AND F.ObjectID = A.ObjectID
	    outer apply (
		    select	string_agg(lower(cast(LA.Uid as nvarchar(max))), ',') as LookupValues
		    from	STRING_SPLIT(COALESCE(F.Value, FT.DefaultValue),',') MV
				    inner join Asset LA on LA.ObjectID = MV.value
				    inner join AssetType LAT on LAT.Object = FT.LookupObjectType+'Type' and LAT.ObjectID = FT.LookupObjectID and LAT.ID = LA.AssetTypeID
		    where	FT.Type = 'Lookup'
	    ) V
    where	(F.FormattedValue IS NOT NULL 
	    OR FT.DefaultValue IS NOT NULL 
	    OR FT.ShowIfEmpty = 1)
	    and FT.Type not in ('JSON','Path','Relationship','FieldFromRelationship','ComplexRelationLookup', 'OwnershipLookup', 'RefListRelationship','Tag','Score')
    and A.Uid in (select Uid from @assets);", new { assets = executionItemSubset.Select(i => new { Uid = i.Item.AssetUid }).Distinct().AsTableValuedParameter("dbo.UidTable", new List<string> { "Uid" }) }).ToList();

                    object setLock = new Object();
                    executionItemSubset.AsParallel().ForAll(executionItem =>
                    {
                        lock (setLock) { 
                            scoreCount++;
                        }

                        var assetUid = executionItem.Item.AssetUid;
                        var effectiveDate = executionItem.Item.EffectiveDate;

                        // The local lists below keep track of score items and links to add for a specific score (asset / effective date / allocation combination).
                        var assetScoreItems = new List<StagingScoreItem>();

                        var results = (
                                        from r in executionItem.Item.Measures
                                        join m in allocations on r.MetricAssetUid equals m.MetricAssetUid
                                        where m.AllocationUid == r.AllocationUid
                                        where m.EffectiveDate == effectiveDate
                                        select new
                                        {
                                            Result = r,
                                            Measure = m
                                        })
                                        .GroupBy(r => r.Result.MetricAssetVersionUid)
                                        .Select(r => r.First())
                                        .ToList();

                        var assetFields = setFields.Where(f => f.Assetuid == assetUid).ToList();

                        object resultsLock = new Object();
                        results.AsParallel().ForAll(r =>
                        {
                            var conditionValidator = CheckMeasureConditions(assetFields, fieldTypes, r.Measure, true);
                        
                            var scoreItem = new StagingScoreItem
                            {
                                AllocationUid = r.Result.AllocationUid,
                                AssetUid = assetUid,
                                EffectiveDate = effectiveDate,
                                RawWeight = conditionValidator.SelectedWeight,
                                MeasureUid = r.Measure.MetricAssetUid,
                                MeasureVersionUid = r.Measure.MetricAssetVersionUid                            
                            };

                            if (conditionValidator.ConditionMet)
                            {
                                scoreItem.ConditionUid = conditionValidator.SelectedConditionUid;
                                scoreItem.OtherConditions = JsonConvert.SerializeObject(conditionValidator.ExtraneousConditions);
                                scoreItem.IsRemoved = false;

                                if (assetVersionCheckObjectTypes.ShouldContinueAnalysis(resultsLock, r.Measure.MetricAssetVersionUid))
                                {
                                    string definitionJson = r.Measure.Definition;
                                    if (string.IsNullOrEmpty(definitionJson))
                                    {
                                        definitionJson = "{}";
                                    }
                                    var definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(definitionJson);

                                    // Now perform analysis based on score type.
                                    switch (r.Measure.ScoreType)
                                    {
                                        case ScoreType.DataQuality:
                                            #region
                                            var dqDefinition = definition.DataQuality;
                                            // Do something with rollups here.
                                            if (r.Measure.RollupPath == null || dqDefinition == null)
                                            {
                                                var error = "";
                                                error += (r.Measure.RollupPath == null) ? "Rollup Path is invalid. An asset type or relationship type may have been removed. " : "";
                                                error += (dqDefinition == null) ? "Measure definition is invalid. Please check and re-save the measure definition, and try again." : "";
                                                scoreItem.Value = false;
                                                scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = error });
                                            }
                                            else
                                            {
                                                if (r.Measure.RollupPath.SegmentLinks == null)
                                                {
                                                    scoreItem.Value = false;
                                                    scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = "Rollup Path Segments do not exist. An asset type or relationship type may have been removed." });
                                                }
                                                else
                                                {
                                                    var iDb = GetCompanyContext();
                                                    DataQualityMeasureQueryModel dqQueryDetail = null;
                                                    lock (resultsLock)
                                                    {
                                                        dqQueryDetail = dqMeasureQueryLibrary.FirstOrDefault(dq => dq.AssetVersionRollupPathUid == r.Measure.RollupPath.AssetVersionRollupPathUid);
                                                        if (dqQueryDetail == null)
                                                        {
                                                            dqQueryDetail = iDb.BuildDataQualityMeasureQueryModel(MetricDataQualityQueryType.MeasureResults_For_Calculation, r.Measure.RollupPath.AssetVersionRollupPathUid);
                                                            dqMeasureQueryLibrary.Add(dqQueryDetail); // Add to library for future reference.
                                                        }
                                                    }

                                                    try
                                                    {
                                                        List<DataQualityMeasureQueryResultModel> rollupPathResults = null;
                                                        
                                                        rollupPathResults = iDb.GetDataQualityMeasureQueryResultModels(dqQueryDetail, assetUid, effectiveDate);

                                                        if (rollupPathResults.Count > 0)
                                                        {
                                                            rollupPathResults.ForEach(o =>
                                                            {
                                                                if (o.StructuredResults.Count > 0)
                                                                {
                                                                    // We should only be getting one result back for each row anyway. This is just in case.
                                                                    o.ResultScoreValue = o.StructuredResults.Select(v => v.PassFraction).Average();
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
                                                                    resultOperationValue = rollupPathResults.Select(v => v.ResultScoreValue).Average();
                                                                    break;
                                                                case MetricRuleResultOperation.Maximum:
                                                                    resultOperationValue = rollupPathResults.Select(v => v.ResultScoreValue).Max();
                                                                    break;
                                                                case MetricRuleResultOperation.Minimum:
                                                                    resultOperationValue = rollupPathResults.Select(v => v.ResultScoreValue).Min();
                                                                    break;
                                                            }

                                                            if (r.Measure.IsThresholdBased)
                                                            {
                                                                scoreItem.DecimalValue = resultOperationValue;
                                                                scoreItem.Value = (r.Measure.Threshold <= resultOperationValue);
                                                            }
                                                            else
                                                            {
                                                                // This will be used when adjusting max and actual weights.
                                                                scoreItem.DecimalValue = resultOperationValue;
                                                            }

                                                            var evidence = rollupPathResults.Select(rp => new DataQualityEvidenceModel
                                                            {
                                                                ErrorMessage = null,
                                                                IsError = false,
                                                                ResultResultUids = rp.StructuredResults.Select(o => o.Uid).ToList(),
                                                                RollupPath = rp.StructuredPath
                                                            });

                                                            scoreItem.Evidence = JsonConvert.SerializeObject(evidence);
                                                        }
                                                        else
                                                        {
                                                            scoreItem.Value = true;
                                                            scoreItem.IsRemoved = true; // GOV-13324 - Since no rules are linked via path, then this is not really a qualifying measure.
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        scoreItem.Value = false;
                                                        scoreItem.Evidence = JsonConvert.SerializeObject(new { IsError = true, ErrorMessage = ex.GetFullExceptionData(false) });
                                                    }

                                                    iDb = null;
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
                                                    scoreItem.Value = r.Result.Result.Value;
                                                    break;
                                                case MetricGovernanceCheckType.Field:
                                                    if (gDefinition.Field != null)
                                                    {
                                                        var assetFieldType = fieldTypes.FirstOrDefault(i => i.Name == gDefinition.Field.FieldTypeName);
                                                        string dataType = (assetFieldType != null) ? assetFieldType.Type : "Text";
                                                        bool allowMultipleValues = (assetFieldType != null) ? assetFieldType.AllowMultipleValues : false;

                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(resultsLock, gDefinition, r.Measure.MetricAssetVersionUid, (assetFieldType != null));

                                                        var assetFieldForFieldCheck = assetFields.FirstOrDefault(f => f.FieldTypeName == gDefinition.Field.FieldTypeName);

                                                        scoreItem.Value = gDefinition.Field.Operator.TestTwoValues(dataType, allowMultipleValues, gDefinition.Field.Values, ((assetFieldForFieldCheck == null) ? null : assetFieldForFieldCheck.Values));
                                                    }
                                                    break;
                                                case MetricGovernanceCheckType.Owner:
                                                    if (gDefinition.Owner != null)
                                                    {
                                                        // Check the measure validity.
                                                        assetVersionCheckObjectTypeAction(resultsLock, gDefinition, r.Measure.MetricAssetVersionUid, null);

                                                        string trueValue = (gDefinition.Owner.Operator == Operator.Populated) ? "1" : "0";
                                                        string falseValue = (gDefinition.Owner.Operator == Operator.Populated) ? "0" : "1";

                                                        scoreItem.Value = calculateAssetMeasureResultFromDb(
                                                        $"select cast(iif(count(1) > 0, {trueValue}, {falseValue}) as bit) " +
                                                        "from ResponsibilityDetail R " +
                                                        "inner join ResponsibilityType T on T.ID = R.ResponsibilityTypeID and T.Uid = @ResponsibilityTypeUid " +
                                                        "where exists ( select 1 from Asset where Uid = @assetUid and ( (ID = R.AssetID and R.AssetID <> 0) or (AssetTypeID = R.AssetTypeID and R.AssetID = 0) ) )",
                                                        new { gDefinition.Owner.ResponsibilityTypeUid, assetUid });
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
                                                        assetVersionCheckObjectTypeAction(resultsLock, gDefinition, r.Measure.MetricAssetVersionUid, null);

                                                        var predicateExistenceSql = "select cast(iif(sum(bit1) > 0, 1, 0) as bit) from (" +
                                                            "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where PredicateUid = @PredicateUid and SubjectUid = @assetUid  " +
                                                            "union all " +
                                                            "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where PredicateUid = @PredicateUid and ObjectUid = @assetUid " +
                                                            ") a";

                                                        switch (gDefinition.Predicate.Operator)
                                                        {
                                                            case Operator.Populated:
                                                                scoreItem.Value = calculateAssetMeasureResultFromDb(predicateExistenceSql, new { gDefinition.Predicate.PredicateUid, assetUid });
                                                                break;
                                                            case Operator.NotPopulated:
                                                                scoreItem.Value = !calculateAssetMeasureResultFromDb(predicateExistenceSql, new { gDefinition.Predicate.PredicateUid, assetUid });
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
                                                        assetVersionCheckObjectTypeAction(resultsLock, gDefinition, r.Measure.MetricAssetVersionUid, null);

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
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, assetUid, ValueUid = Guid.Parse(gDefinition.Relation.Values[0]) };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @assetUid and ObjectUid = @ValueUid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @ValueUid and ObjectUid = @assetUid ";
                                                                break;
                                                            case Operator.In:
                                                                parameters = new
                                                                {
                                                                    gDefinition.Relation.IntersectTypeUid,
                                                                    assetUid,
                                                                    Uids = gDefinition.Relation.Values.Select(u => new { Uid = Guid.Parse(u) }).AsTableValuedParameter("dbo.UidTable", new List<string>() { "Uid" })
                                                                };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = @assetUid and I.ObjectUid = U.Uid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = U.Uid and I.ObjectUid = @assetUid ";
                                                                break;
                                                            case Operator.NotEquals:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, assetUid, ValueUid = Guid.Parse(gDefinition.Relation.Values[0]) };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @assetUid and ObjectUid = @ValueUid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @ValueUid and ObjectUid = @assetUid ";
                                                                break;
                                                            case Operator.NotIn:
                                                                parameters = new
                                                                {
                                                                    gDefinition.Relation.IntersectTypeUid,
                                                                    assetUid,
                                                                    Uids = gDefinition.Relation.Values.Select(u => new { Uid = Guid.Parse(u) }).AsTableValuedParameter("dbo.UidTable", new List<string>() { "Uid" })
                                                                };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = @assetUid and I.ObjectUid = U.Uid " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail I inner join @Uids U on I.IntersectTypeUid = @IntersectTypeUid and I.SubjectUid = U.Uid and I.ObjectUid = @assetUid ";
                                                                break;
                                                            case Operator.NotPopulated:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, assetUid };
                                                                bitSql = "iif(sum(bit1) = 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @assetUid  " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and ObjectUid = @assetUid ";
                                                                break;
                                                            default: // case Operator.Populated:
                                                                parameters = new { gDefinition.Relation.IntersectTypeUid, assetUid };
                                                                bitSql = "iif(sum(bit1) > 0, 1, 0)";
                                                                operatorSql =
                                                                    "select iif(count(1) > 0, 1, 0) bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and SubjectUid = @assetUid  " +
                                                                    "union all " +
                                                                    "select iif(count(1) > 0, 1, 0) as bit1 from IntersectDetail where IntersectTypeUid = @IntersectTypeUid and ObjectUid = @assetUid ";
                                                                break;
                                                        }
                                                        var relationSql = $"select cast({bitSql} as bit) from ({operatorSql}) a";

                                                        scoreItem.Value = calculateAssetMeasureResultFromDb(relationSql, parameters);
                                                        
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
                                }
                                else
                                {
                                    scoreItem.IsRemoved = true;
                                }
                            }
                            else
                            {
                                scoreItem.IsRemoved = true;
                            }

                            lock (resultsLock)
                            {
                                assetScoreItems.Add(scoreItem);
                            }
                            
                        });

                        //Add to main list.
                        lock (setLock)
                        {
                            scoreItems.AddRange(assetScoreItems);
                        }
                    });

                    // Now add scores in this set via a transaction.
                    scoreItems = scoreItems.Distinct(new StagingScoreItemComparer()).ToList();
                    var success = addScoresToEnvironmentDatabase(scoreItems);
                    
                    // Stop the stopwatch and figure out how much time elapsed, taking the average time across all loops so far.
                    stopwatch.Stop();
                    loopElapsedSeconds = (int)(stopwatch.Elapsed.TotalSeconds / loopCount);

                    if (success)
                    {
                        ExecutionRecord.LoopSecondsElapsed = loopElapsedSeconds;
                        ExecutionRecord.UpdatedOn = DateTime.UtcNow;
                        updateExecution(company, ExecutionRecord);
                    }
                    
                    // Update the executionItems we were just working with.
                    company.Execute(
                        "update metrics.ExecutionItem set [State] = 1 where ExecutionID = @ID and RowNumber in (select Id from @rows)", 
                        new { ExecutionRecord.ID, rows = executionItemSubset.Select(i => new { Id = i.RowNumber }).Distinct().AsTableValuedParameter("dbo.Ids", new List<string> { "Id" }) }
                        );
                    executionItems = getExecutionItems(company, 0);
                    scoreItems.Clear();

                    loopCount++;
                }

                updateExecution(company, ExecutionRecord, true);

                Db.CreateWorkflowCheckExecution(ExecutionRecord, ScoreQueueChangeType.AssetMeasures); // Add workflow check.
            }
        }

        bool addScoresToEnvironmentDatabase(List<StagingScoreItem> items) 
        {
            bool success = false;

            // First, remove potential duplicates.
            var duplicateGroupings = items.GroupBy(i => new { i.AssetUid, i.MeasureUid, i.EffectiveDate }).Where(i => i.Count() > 1).Select(i => i.Key).ToList();
            if (duplicateGroupings.Count > 0)
            {
                duplicateGroupings.ForEach(g =>
                {
                    var firstDuplicate = items.Where(i => i.AssetUid == g.AssetUid && i.EffectiveDate == g.EffectiveDate && i.MeasureUid == g.MeasureUid).First();
                    if (firstDuplicate != null)
                    {
                        items.Remove(firstDuplicate);
                    }
                });
            }

            using (var company = GetEnvironmentConnection())
            {
                if (company.State != ConnectionState.Open)
                    company.Open();

                using (var trans = company.BeginTransaction())
                {
                    bool transactionSuccessful = false;

                    try
                    {
                        var itemsTable = new DataTable();
                        itemsTable.Columns.Add("AllocationUid", typeof(Guid));
                        itemsTable.Columns.Add("AssetUid", typeof(Guid));
                        itemsTable.Columns.Add("MeasureUid", typeof(Guid));
                        itemsTable.Columns.Add("MeasureVersionUid", typeof(Guid));
                        itemsTable.Columns.Add("EffectiveDate", typeof(DateTime));

                        itemsTable.Columns.Add("IsRemoved", typeof(bool));

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
                            itemRow["IsRemoved"] = s.IsRemoved;
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
    RowNumber int identity not null,
	AllocationUid uniqueidentifier NOT NULL,
    AssetUid uniqueidentifier NOT NULL,
	MeasureUid uniqueidentifier NOT NULL,
	MeasureVersionUid uniqueidentifier NOT NULL,
	EffectiveDate datetime NOT NULL,
	[Value] bit NULL,
    IsRemoved bit not null,
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
                            bulkCopy.ColumnMappings.Add("IsRemoved", "IsRemoved");
                            bulkCopy.ColumnMappings.Add("Value", "Value");
                            bulkCopy.ColumnMappings.Add("DecimalValue", "DecimalValue");
                            bulkCopy.ColumnMappings.Add("RawWeight", "RawWeight");
                            bulkCopy.ColumnMappings.Add("ConditionUid", "ConditionUid");
                            bulkCopy.ColumnMappings.Add("Evidence", "Evidence");
                            bulkCopy.ColumnMappings.Add("OtherConditions", "OtherConditions");

                            bulkCopy.WriteToServer(itemsTable);
                        }

                        company.Execute(@"
delete  #StagingScoreItem
where   RowNumber in (
        select      min(RowNumber) as RowNumber
        from        #StagingScoreItem
        group by    AssetUid,
                    MeasureUid,
                    EffectiveDate
        having      count(1) > 1
        );", transaction: trans);

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
    T.IsRemoved = S.IsRemoved,
	T.Evidence = S.Evidence,
	T.OtherConditions = S.OtherConditions,
    T.AllocationUid = S.AllocationUid
when not matched then
    insert (
        AllocationUid, AssetUid, MeasureUid, MeasureVersionUid, 
        EffectiveDate, [Value], DecimalValue, RawWeight, 
        ConditionUid, Evidence, OtherConditions, IsRemoved
    ) values (
        S.AllocationUid, S.AssetUid, S.MeasureUid, S.MeasureVersionUid, 
        S.EffectiveDate, S.[Value], S.DecimalValue, S.RawWeight, 
        S.ConditionUid, S.Evidence, S.OtherConditions, S.IsRemoved
    );", transaction: trans);

                        trans.Commit();
                        transactionSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            if (trans != null)
                            {
                                trans.Rollback();
                            }
                        }
                        catch
                        {
                        }
                        if (ExecutionRecord != null)
                        {
                            updateExecution(company, ExecutionRecord, false, ex);
                        }
                    }

                    if (transactionSuccessful)
                    { 
                        try
                        {
                            // Call the new procedure here.
                            var allocationUids = items.Select(item => item.AllocationUid).Distinct().ToList();
                            allocationUids.ForEach(allocationUid =>
                            {
                                company.Execute(
                                    "metrics.ProcessAssetScores @allocationUid, @assets",
                                    new
                                    {
                                        allocationUid,
                                        assets = items
                                                    .Where(item => item.AllocationUid == allocationUid)
                                                    .Select(i => new { i.AssetUid, i.EffectiveDate })
                                                    .Distinct()
                                                    .AsTableValuedParameter("dbo.AssetEffectiveDate", new List<string> { "AssetUid", "EffectiveDate" }),
                                    },
                                    commandTimeout: 600
                                );
                            });

                            success = true;
                        }
                        catch (Exception ex)
                        {
                            if (ExecutionRecord != null)
                            {
                                updateExecution(company, ExecutionRecord, false, ex);
                            }
                        }                    
                    }
                }

                company.Close();
            }
            return success;
        }

        bool calculateAssetMeasureResultFromDb(string sql, object parameters)
        {
            bool result = false;

            using (var company = GetEnvironmentConnection())
            {
                result = company.Query<bool>(sql, parameters, commandTimeout: 90).Single();
            }

            return result;
        }
    }
}
