using d360.core.entities;
using d360.core.entities.Metric;
using d360.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.enums;
using d360.model;
using System.Net;
using d360.core;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using d360.core.entities.Scoring;
using System.Data;

namespace d360.model.DataAccessLayer
{
    public class MetricsRepository : IMetricsRepository
    {
        internal ICompanyContext Company;
        internal IQueueSource QueueSource;

        public MetricsRepository(ICompanyContext context, IQueueSource queueSource)
        {
            this.Company = context;
            this.QueueSource = queueSource;
        }

        public void DeleteMetric(MetricAsset model)
        {

            var currentAssetVersion = model.Versions.OrderBy(x => x.EffectiveDate).FirstOrDefault();
            currentAssetVersion.State = State.Deleted;
            currentAssetVersion.EffectiveEndDate = DateTime.Now.Date;
            model.State = State.Deleted;
            model.UpdatedOn = DateTime.Now;
            var children = Company.MetricAssets.Where(x => x.ParentUid != null && x.ParentUid == model.Uid).ToList();

            if (children.Count > 0)
            {
                children.ForEach(c => c.State = State.Deleted);
            }

            Company.SaveChanges();
        }

        public MetricAsset GetMetricByUid(Guid uid)
        {
            return Company.Filter<MetricAsset>(i => i.Uid == uid, i => i.Children).SingleOrDefault();
        }

        public MetricAsset GetActiveMetric(Guid uid)
        {
            return Company.Filter<MetricAsset>(i => i.Uid == uid && i.State == State.Active).SingleOrDefault();
        }

        public WorkHttpStatus AddOrUpdateMetrics(MetricAssetViewModel model, out bool isNew)
        {
            MetricAsset metricAsset = null;
            AssetType targetAssetType = null;
            isNew = true;

            if (model.Uid != null && model.Uid != Guid.Empty)
            {
                Guid assetTypeId = Company.MetricAssets.FirstOrDefault(x => x.Uid == model.Uid).AssetTypeUid;
                targetAssetType = Company.AssetTypes.FirstOrDefault(x => x.uid == assetTypeId);
            }

            if (model.AssetTypeUid != null && model.AssetTypeUid != Guid.Empty)
            {
                targetAssetType = Company.AssetTypes.FirstOrDefault(x => x.uid == model.AssetTypeUid);
            }


            var existingResultCount = 0;
            var childMetricCount = 0;


            List<string> validTypes = new List<string>() { "Boolean", "Decimal", "Date", "Lookup", "Number", "Text" };

            foreach (var condition in model.Conditions)
            {
                var fieldType = new FieldType();
                if (condition.FieldTypeID.HasValue)
                {
                    fieldType = Company.FieldTypes.FirstOrDefault(x => x.ID == condition.FieldTypeID);
                }
                else
                {
                    fieldType = Company.FieldTypes.FirstOrDefault(x => x.Name.ToLower() == condition.FieldName.Trim().ToLower() && x.Object == targetAssetType.Object && x.ObjectID == targetAssetType.ObjectID);

                    if (fieldType == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "Invalid FieldType for this asset!");
                    }
                    condition.FieldTypeID = fieldType.ID;
                }

                if (fieldType == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "FieldType does not exist!");
                }

                if (targetAssetType.Object != fieldType.Object || targetAssetType.ObjectID != fieldType.ObjectID)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "Invalid FieldType for this asset!");
                }

                if (!validTypes.Contains(fieldType.Type))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"FieldType cannot be type of '{fieldType.Type}'!");
                }
                bool tempBool;
                decimal tempDecimal;
                DateTime tempDate;
                int tempInt;

                switch (fieldType.Type)
                {
                    case "Boolean":
                        if (!bool.TryParse(condition.Values, out tempBool))
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{(string.IsNullOrEmpty(condition.FieldName) ? condition.FieldTypeID.Value.ToString() : condition.FieldName)}' does not contain valid '{fieldType.Type}' value!");
                        break;
                    case "Decimal":
                        if (!decimal.TryParse(condition.Values, out tempDecimal))
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{(string.IsNullOrEmpty(condition.FieldName) ? condition.FieldTypeID.Value.ToString() : condition.FieldName)}' does not contain valid '{fieldType.Type}' value!");
                        break;
                    case "Date":
                        if (!DateTime.TryParse(condition.Values, out tempDate))
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{(string.IsNullOrEmpty(condition.FieldName) ? condition.FieldTypeID.Value.ToString() : condition.FieldName)}' does not contain valid '{fieldType.Type}' value!");
                        condition.Values = tempDate.ToShortDateString();
                        break;
                    case "Number":
                        if (!int.TryParse(condition.Values, out tempInt))
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{(string.IsNullOrEmpty(condition.FieldName) ? condition.FieldTypeID.Value.ToString() : condition.FieldName)}' does not contain valid '{fieldType.Type}' value!");
                        break;
                    default:
                        break;
                }

            }

            int metricExistsCount = 0;
            metricExistsCount = (model.ParentUid.HasValue) ?
                Company.Query<int>($"select count(1) from metrics.Asset where lower(Name) = @n and [State] = 1 and ParentUid = @p and AssetTypeUid = @assetTypeUid and uid <> @uid", new { n = model.Name.Trim().ToLower(), p = model.ParentUid.Value, assetTypeUid = targetAssetType.uid, uid = model.Uid }).Single() :
                Company.Query<int>($"select count(1) from metrics.Asset where lower(Name) = @n and [State] = 1 and ParentUid is null and AssetTypeUid = @assetTypeUid and uid <> @uid", new { n = model.Name.Trim().ToLower(), assetTypeUid = targetAssetType.uid, uid = model.Uid }).Single();

            if (metricExistsCount > 0)
            {
                return new WorkHttpStatus(
                    HttpStatusCode.BadRequest,
                    "Error adding metric",
                    (model.ParentUid.HasValue) ?
                    "You may not add a metric with the same name under the same grouping." :
                    $"Measure with name '{model.Name}' already exists.");
            }


            if (model.Uid != Guid.Empty)
            {
                isNew = false;

                existingResultCount = Company.Query<int>("select count(1) from metrics.ScoreItem where MetricAssetUid = @Uid", new { model.Uid }).Single();

                childMetricCount = Company.Query<int>("select count(1) from metrics.Asset where ParentUid = @Uid and State=1", new { model.Uid }).Single();

                metricAsset = Company.Filter<MetricAsset>(i => i.Uid == model.Uid).SingleOrDefault();
                if (metricAsset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Error updating metric", "Metric not found.");
                }

                metricAsset.Description = model.Description;
                metricAsset.Name = model.Name.Trim();
                metricAsset.ScoreType = model.ScoreType.Value;
                metricAsset.UpdatedBy = Company.CurrentResourceID;
                metricAsset.UpdatedOn = DateTime.Now;
                // If results, then you cannot change. 
                if (existingResultCount > 0 && model.IsGroup)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"You may not convert this metric to a grouping as there are results already associated with it.");
                }
                // If has child metrics, you cannot change.
                if (childMetricCount > 0 && !model.IsGroup)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"You may not convert this grouping to a metric as there are child metrics already associated with it.");
                }

                // If made it past above, then we can save the grouping change.
                metricAsset.IsGroup = model.IsGroup;
            }
            else
            {
                metricAsset = new MetricAsset
                {
                    Uid = Guid.NewGuid(),
                    AssetTypeUid = model.AssetTypeUid,
                    Description = model.Description,
                    IsGroup = model.IsGroup,
                    Name = model.Name.Trim(),
                    State = State.Active,
                    ScoreType = model.ScoreType.Value
                };

                if (model.AssetTypeUid == Guid.Empty)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Error updating metric", "Asset type not found or is empty.");
                }

                if (model.ParentUid != Guid.Empty && model.ParentUid.HasValue)
                {
                    var parentMetricAsset = Company.Filter<MetricAsset>(i => i.Uid == model.ParentUid).SingleOrDefault();
                    if (parentMetricAsset == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Error updating metric", "Parent metric not found.");
                    }
                    else
                    {
                        if (parentMetricAsset.AssetTypeUid != metricAsset.AssetTypeUid)
                        {
                            return new WorkHttpStatus(HttpStatusCode.NotFound, "Error updating metric", "Parent metric must belong to the same asset type.");
                        }
                    }
                    metricAsset.ParentUid = model.ParentUid;
                }
                Company.MetricAssets.Add(metricAsset);
            }

            var effectiveDate = model.EffectiveDate == DateTime.MinValue ? DateTime.UtcNow : model.EffectiveDate;

            var maxEffectiveDate = Company.Query<DateTime?>("select max(EffectiveDate) from metrics.AssetVersion where [Uid] = @Uid", new { model.Uid }).SingleOrDefault();

            if (maxEffectiveDate.HasValue)
            {
                if (maxEffectiveDate.Value > effectiveDate.Date)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"You may not backdate the effective date for this metric. You must provide date more recent than {maxEffectiveDate.Value.ToShortDateString()}");
                }
            }

            var metricAssetVersion = Company.Filter<MetricAssetVersion>(i => i.Uid == model.Uid && i.EffectiveDate == effectiveDate, v => v.Conditions).SingleOrDefault();

            string newConditionHash = string.Join("|", model.Conditions.Select(c => string.Join(";", c.FieldTypeID, c.Operator, c.Values)));
            newConditionHash = newConditionHash.GetD3sHashString();
            if (metricAssetVersion == null)
            {
                //Set the default to a = And.
                if (string.IsNullOrEmpty(model.ConditionAndOr))
                {
                    model.ConditionAndOr = "a";
                }

                metricAssetVersion = new MetricAssetVersion
                {
                    Uid = metricAsset.Uid,
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    ConditionAndOr = model.ConditionAndOr,
                    EffectiveDate = effectiveDate,
                    Weight = model.Weight,
                    State = metricAsset.State,
                    EffectiveEndDate = null
                };

                if (model.Conditions.Count > 0)
                {
                    if (metricAssetVersion.Conditions == null)
                        metricAssetVersion.Conditions = new List<MetricAssetVersionCondition>();

                    var usedFieldTypeIDs = new List<int>();

                    model.Conditions.ForEach(c =>
                    {
                        // You can only add one of a specific field type ID.
                        if (!usedFieldTypeIDs.Contains(c.FieldTypeID.Value))
                        {
                            metricAssetVersion.Conditions.Add(new MetricAssetVersionCondition { FieldTypeID = c.FieldTypeID.Value, Operator = c.Operator, ValueJson = c.Values });
                            usedFieldTypeIDs.Add(c.FieldTypeID.Value);
                        }
                    });
                }

                Company.MetricAssetVersions.Add(metricAssetVersion);
            }
            else
            {
                // Only validate if there any existing results for this metric. If not, do not worry about it.
                if (existingResultCount > 0)
                {
                    if (metricAssetVersion.Weight != model.Weight)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "You may not alter the weight of this metric without also altering its effective date.");
                    }

                    if (metricAssetVersion.ConditionAndOr != model.ConditionAndOr)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "You may not alter the condition type of this metric without also altering its effective date.");
                    }

                    //Set the default to a = And.
                    if (string.IsNullOrEmpty(model.ConditionAndOr))
                    {
                        model.ConditionAndOr = "a";
                    }

                    string existingConditionHash = string.Join("|", metricAssetVersion.Conditions.Select(c => string.Join(";", c.FieldTypeID, c.Operator, c.ValueJson)));
                    existingConditionHash = existingConditionHash.GetD3sHashString();
                    if (newConditionHash != existingConditionHash)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "You may not alter the conditions of this metric without also altering its effective date.");
                    }
                }

                // Set the properties.
                metricAssetVersion.ConditionAndOr = model.ConditionAndOr;
                metricAssetVersion.Weight = model.Weight;

                #region Deal with processing the conditions.
                if (model.Conditions.Count > 0)
                {
                    if (metricAssetVersion.Conditions == null)
                        metricAssetVersion.Conditions = new List<MetricAssetVersionCondition>();

                    // Handle UPDATEs and ADDs.
                    model.Conditions.ForEach(c =>
                    {
                        var ec = metricAssetVersion.Conditions.SingleOrDefault(i => i.FieldTypeID == c.FieldTypeID);
                        if (ec != null)
                        {
                            // Update the values.
                            ec.Operator = c.Operator;
                            ec.ValueJson = c.Values;
                        }
                        else
                        {
                            // Add to collection.
                            metricAssetVersion.Conditions.Add(new MetricAssetVersionCondition { FieldTypeID = c.FieldTypeID.Value, Operator = c.Operator, ValueJson = c.Values });
                        }
                    });

                    // Handle DELETEs.
                    var fieldTypeIdsToDelete = new List<int>();
                    foreach (var c in metricAssetVersion.Conditions)
                    {
                        if (!model.Conditions.Any(i => i.FieldTypeID == c.FieldTypeID))
                        {
                            fieldTypeIdsToDelete.Add(c.FieldTypeID);
                        }
                    }
                    foreach (var id in fieldTypeIdsToDelete)
                    {
                        var dc = metricAssetVersion.Conditions.Single(i => i.FieldTypeID == id);
                        metricAssetVersion.Conditions.Remove(dc);
                    }
                }
                else
                {
                    metricAssetVersion.Conditions = null;
                }
                #endregion Deal with processing the conditions.
            }

            var operatorErrorMessage = "";
            var operators = new List<string>() { "eq", "neq", "lt", "lte", "gt", "gte" };
            model.Conditions.ForEach(c =>
            {
                if (!operators.Contains(c.Operator))
                {
                    operatorErrorMessage += $"Invalid operator used: {c.Operator}; ";
                }
            });

            if (!string.IsNullOrEmpty(operatorErrorMessage))
            {
                operatorErrorMessage += $"Only the operators ({string.Join(", ", operators)}) may be used.";
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", operatorErrorMessage);
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        public MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(Guid assetTypeUid, DateTime? effectiveDate)
        {
            SqlConnection cnn = Company.Database.Connection as SqlConnection;
            if (!effectiveDate.HasValue)
                effectiveDate = DateTime.UtcNow.Date;

            var sql = @"
                    drop table if exists #tbl
                    create table #tbl ([Uid] uniqueidentifier, Name nvarchar(250), ParentUid uniqueidentifier, IsGroup bit, Weight decimal(5,3), EffectiveDate date, Description nvarchar(500))
                    
                    insert into #tbl 
                    	select	A.[Uid],
                    			A.Name,
                    			A.ParentUid,
                    			A.IsGroup,
                    			V.Weight,
                    			V.EffectiveDate,
                    			A.Description

                    	from	metrics.AssetVersion V
                    			inner join (
                    					select		IA.[Uid],
                    								max(IV.EffectiveDate) as EffectiveDate
                    					from		metrics.AssetVersion IV
                    								inner join metrics.Asset IA on IA.[Uid] = IV.[Uid] 
                    															and IA.AssetTypeUid = @assetTypeUid 
                    															and IV.EffectiveDate <= @effectiveDate 
                    															and ((IA.State = 1 and EffectiveEndDate is null) or (IA.State = 3 and EffectiveEndDate >= @effectiveDate))
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
                            EffectiveDate,
                            Description,
                    		(
                    			select	F.FriendlyName as FieldName,
                    					C.Operator,
                    					(case WHEN F.Type = 'Lookup' THEN FL.Text ELSE C.ValueJson END) as [Value]
                    			from	[metrics].[AssetVersionCondition] C
                    					inner join FieldType F on F.ID = C.FieldTypeID
                                        inner join FieldLookupValue FL on 
				                            FL.FieldTypeID = F.ID 
				                            and F.LookupObjectType = FL.LookupObjectType 
				                            and F.LookupObjectID = FL.LookupObjectID 
                                            and [Value] = C.ValueJson
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

        public MetricAssetHierarchyModels GetMetricHierarchyByAsset(Guid assetUid, DateTime? effectiveDate, ScoreType type)
        {
            SqlConnection cnn = Company.Database.Connection as SqlConnection;

            if (!effectiveDate.HasValue)
                effectiveDate = DateTime.UtcNow.Date;

            string sql = $@"declare @assetTypeUid uniqueidentifier;
                    select	@assetTypeUid = T.[Uid]
                    from	dbo.Asset A
                    		inner join AssetType T on T.ID = A.AssetTypeID and A.[Uid] = @assetUid;

                    declare @lastScoredDate date = (select top 1 RunDate from metrics.score where AssetUid = @assetUid and ScoreType = @scoreType order by RunDate desc)

                    if @effectiveDate > @lastScoredDate
                    begin
	                    set @effectiveDate = @lastScoredDate
                    end

                    select 
                        ma.[Uid], 
                        ParentUid,
                        null,
                        ma.Name,
                        ma.Description,
                        ma.IsGroup,
                        AV.EffectiveDate, 
                        COALESCE((SELECT top 1 AV1.EffectiveDate FROM metrics.AssetVersion AV1
							                            WHERE AV1.Uid = ma.Uid 
							                            and AV1.EffectiveDate > @effectiveDate
	                        order by EffectiveDate), AV.EffectiveEndDate) 
                         as [EndDate], 
                         COALESCE(I.AdjustedWeight, AV.[Weight]) as [Weight],
                         I.Value,
                         ma.ScoreType
                        from metrics.asset ma 
		                        inner join metrics.AssetVersion AV 
		                        on AV.Uid = ma.uid
								and AV.EffectiveDate = (select max(av1.EffectiveDate) from metrics.assetVersion AV1 where ma.Uid = AV1.Uid and AV1.EffectiveDate <= @effectiveDate)
								AND (AV.EffectiveEndDate is null or AV.EffectiveEndDate >= @EffectiveDate)
		                        left join metrics.scoreitem I on ma.Uid = I.MetricAssetUid AND I.AssetUid = @assetUid 
                        where  
						ma.ScoreType = @scoreType 
						and ma.AssetTypeUid = @AssetTypeUid 
						and (
								(
									(endDate >= dateadd(day, 1,@effectiveDate) 
									and I.EffectiveDate <= @effectiveDate) or ma.IsGroup = 1)
								or 
								(endDate is null and I.EffectiveDate <= @effectiveDate)
							)";



            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            var results = cnn.Query<MetricAssetHierarchyModel>(sql, new { assetUid, effectiveDate = effectiveDate.Value, scoreType = (int)type }).ToList();

            var model = new MetricAssetHierarchyModels();

            foreach (var i in results)
            {
                model.Add(i);
            }

            return model;
        }

        public List<int> GetScoreTypesForAsset(Guid assetUid)
        {
            var sql = $@"select distinct ma.scoretype from metrics.Allocation  ma
                            inner join assettype att on ma.AssetTypeUid = att.[uid]
                            inner join asset a on att.id = a.AssetTypeID
							inner join metrics.score ms on ms.AssetUid = a.uid and ms.ScoreType = ma.ScoreType
                        where 
                            a.[uid] = '{assetUid.ToString()}' 
							and ma.[state] = 1
							and EndDate is null";
            return Company.Query<int>(sql).ToList();
        }

        public List<string> GetMetricStructureFragments(Guid assetTypeUid, ScoreType scoreTypeFilter)
        {
            return Company.Query<string>($@"
                    select	A.Uid,
                    		A.ParentUid,
                    		A.AssetTypeUid,
                    		A.IsGroup,
                    		A.Name,
                    		A.Description,
                    		V.EffectiveDate,
                    		V.Weight,
                    		V.ConditionAndOr,
                    		(
                    			select	FieldTypeID,
                    					Operator,
                    					[ValueJson] as [Values]
                    			from	metrics.AssetVersionCondition
                    			where	Uid = V.Uid and EffectiveDate = V.EffectiveDate
                    			for		json path
                    
                    		) as Conditions
                    from	metrics.Asset A
                    		cross apply (
                    			select	max(EffectiveDate) as EffectiveDate
                    			from	metrics.AssetVersion
                    			where	Uid = A.Uid
                    		) MV
                    		inner join metrics.AssetVersion V on V.Uid = A.Uid and V.EffectiveDate = MV.EffectiveDate and A.[State] = 1
                    where	A.AssetTypeUid = '{assetTypeUid.ToString()}' and A.ScoreType = {(int)scoreTypeFilter}
                    for		json path").ToList();
        }

        public List<string> GetMetricFieldFragments(Guid assetTypeUid)
        {
            return Company.Query<string>($@"
                            select	F.ID,
                            		F.FriendlyName as Name,
                            		F.Type,
                            		(
                            			select	Value,
                            					Text
                            			from	FieldLookupValue
                            			where	FieldTypeID = F.ID
                            			for		json path
                            
                            		) as [Values]
                            from	AssetType A
                            		inner join FieldType F on F.AssetTypeID = A.ID and A.[uid] = '{assetTypeUid.ToString()}' and F.Type in ('Boolean', 'Decimal', 'Date', 'Lookup', 'Number', 'Text')
                            for		json path").ToList();
        }

        public List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution)
        {
            return Company.BulkMetricsImport(model, execution);
        }

        public (MetricScoreApiModel, string) GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var filterAsset = new Asset();
            int? filterFieldTypeId = null;

            var result = new MetricScoreApiModel();
            var parameters = new DynamicParameters();

            List<string> whereClauses = new List<string>();
            List<string> scoreFilters = new List<string>();
            List<string> fieldFilters = new List<string>();
            whereClauses.Add("AT.uid = @assetTypeUid");
            parameters.Add("@assetTypeUid", at.uid);

            var dateStart = DateTime.MinValue;
            var dateEnd = DateTime.MinValue;
            int customFieldsCounter = 0;
            foreach (var param in queryParams)
            {
                switch (param.Key.ToLower())
                {
                    case "_pagesize":
                        int pageSize = 0;
                        if (!int.TryParse(param.Value, out pageSize) || pageSize <= 0)
                            return (null, "Invalid '_pagesize' parameter value");
                        result.pageSize = pageSize;
                        break;
                    case "_pagenum":
                        int pageNum = 0;
                        if (!int.TryParse(param.Value, out pageNum) || pageNum <= 0)
                            return (null, "Invalid 'pageNum' parameter value");
                        result.pageNum = pageNum;
                        break;
                    case "_effectivedatestart":
                        DateTime.TryParse(param.Value, out dateStart);
                        if (dateStart == DateTime.MinValue)
                            return (null, "Invalid '_effectiveDateStart' parameter value");
                        parameters.Add("@dateStart", dateStart);
                        scoreFilters.Add("MS.EffectiveDate >= @dateStart");
                        break;
                    case "_effectivedateend":
                        DateTime.TryParse(param.Value, out dateEnd);
                        if (dateEnd == DateTime.MinValue)
                            return (null, "Invalid '_effectiveDateEnd' parameter value");
                        parameters.Add("@dateEnd", dateEnd);
                        scoreFilters.Add("MS.EffectiveDate <= @dateEnd");
                        break;
                    case "_assetuid":
                        Guid assetUid = Guid.Empty;
                        if (!Guid.TryParse(param.Value, out assetUid))
                            return (null, "Invalid '_assetUid' parameter value");

                        var assetTypeId = Company.Assets.Where(x => x.uid == assetUid).FirstOrDefault()?.AssetTypeID;
                        if (assetTypeId != at.ID)
                            return (null, "Asset of given asset type Uid does not exists");

                        parameters.Add("@assetUid", assetUid);
                        if (parameters.ParameterNames.Any(x => x.ToLower() == "customfield"))
                            return (null, "'_assetUid' AND 'customfield' are exclusive filters and may not be combined.");

                        whereClauses.Add("A.Uid = @assetUid");
                        break;
                    default:
                        customFieldsCounter++;
                        var fieldName = param.Key;

                        filterFieldTypeId = Company.FieldTypes.Where(x => x.AssetTypeID == at.ID && x.Name.ToLower() == param.Key.ToLower()).FirstOrDefault()?.ID;
                        if (filterFieldTypeId == null)
                            return (null, $"Invalid custom field parameter. Field type with name '{param.Key}' does not exists");

                        if (parameters.ParameterNames.Any(x => x.ToLower() == "assetuid"))
                            return (null, "'_assetUid' AND 'customfield' are exclusive filters and may not be combined.");

                        fieldFilters.Add($"inner join Field F{customFieldsCounter} on F{customFieldsCounter}.FieldTypeID = @ftId{customFieldsCounter} and F{customFieldsCounter}.AssetID = A.ID and F{customFieldsCounter}.FormattedValue = @ftValue{customFieldsCounter}");
                        parameters.Add("@ftId" + customFieldsCounter, filterFieldTypeId);
                        parameters.Add("@ftValue" + customFieldsCounter, param.Value);

                        break;
                }
            }


            bool takeOnlyLastScore = false;

            if (dateEnd < dateStart && (dateStart != DateTime.MinValue && dateEnd != DateTime.MinValue))
            {
                return (null, "Effective start date should be before effective end date parameter");
            }
            if (dateStart == DateTime.MinValue && dateEnd == DateTime.MinValue)
                takeOnlyLastScore = true;

            parameters.Add("@pageSize", result.pageSize);
            parameters.Add("@pageNum", result.pageNum);


            if (!Company.CurrentResourceIsAdmin)
            {
                whereClauses.Add($"A.ID not in ({Company.GetNoReadSqlStatement()})");
            }

            string whereSQl = whereClauses.Count == 0 ? "" : "where " + string.Join(" AND ", whereClauses);
            string scoreWhereSQl = scoreFilters.Count == 0 ? "" : " and " + string.Join(" AND ", scoreFilters);

            var countSql = $@"select
                         count(distinct a.uid)
                         from metrics.Score MS
                            inner join Asset A on A.uid = MS.AssetUid
                            inner join AssetType AT on A.AssetTypeID = AT.ID
                            {string.Join(" ", fieldFilters)}
                            {whereSQl}";

            result.total = Company.Query<int>(countSql, parameters).FirstOrDefault();

            var sql = $@"select LOWER(A.uid) as AssetUid,
	                    (select {(takeOnlyLastScore ? "top 1" : "")} MS.EffectiveDate, MS.Value as Score 
		                     from metrics.Score MS
								where AssetUid = a.uid 
                                {scoreWhereSQl}
								order by MS.EffectiveDate desc
		                     for json path) as Scores
		                from metrics.Score MS
		                    inner join Asset A on A.uid = MS.AssetUid
		                    inner join AssetType AT on A.AssetTypeID = AT.ID
                            {string.Join(" ", fieldFilters)}
                        {whereSQl}
                        group by a.uid
	                    order by a.uid
	                    offset ((@pageNum-1)*@pageSize) rows fetch next @pageSize rows only
                    for json path
                    ";

            var itemsJson = string.Join("", Company.Query<string>(sql, parameters).ToList());

            result.items = JsonConvert.DeserializeObject<List<MetricAssetScoreModel>>(itemsJson);
            if (result.items == null) result.items = new List<MetricAssetScoreModel>();
            return (result, "");
        }

        public ScoreTypeAllocation GetAllocationByMetricModel(MetricAssetViewModel model)
        {
            return Company.ScoreTypeAllocations.FirstOrDefault(x => x.AssetTypeUid == model.AssetTypeUid && x.ScoreType == model.ScoreType);
        }


        public List<DataQualityResponseModel> InsertDataQualityResult(List<DataQualityInsertModel> request, ApiExecution execution)
        {
            Company.Add(execution);

            List<DataQualityResponseModel> results = null;
            try
            {
                List<IDataQualityUpsert> upsert = new List<IDataQualityUpsert>();
                upsert.AddRange(request);
                results = Company.UpsertAssetResults(upsert, execution);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }
        public DataQualityGetResultModel GetDataQualityResults(Guid owningAssetUid, Guid? evaluatedAssetUid = null, int pageSize = 250, int pageNum = 1, string sort = null, string direction = "asc", DateTime? effectiveDateStart = null, DateTime? effectiveDateEnd = null)
        {
            var result = new DataQualityGetResultModel();
            var parameters = new DynamicParameters();
            string orderBy;
            string effectiveSQL = "";
            string evaluatedAssetSQL;


            if (effectiveDateStart.HasValue)
            {
                effectiveSQL = $@"and EffectiveDate >= @effectiveStartDate";
                parameters.Add("@effectiveStartDate", effectiveDateStart.Value);
            }
            if (effectiveDateEnd.HasValue)
            {
                effectiveSQL = $@"{effectiveSQL} and EffectiveDate <= @effectiveEndDate";
                parameters.Add("@effectiveEndDate", effectiveDateEnd.Value);
            }

            string owningAssetSQL = $@"(
	                                select 
		                                AR.Uid resultUid, AR.Passcount, AR.FailCount, AR.EffectiveDate, AR.RunDate, AR.PassFraction, AN.Uid owningAssetUid
                                    from 
		                                AssetResult AR, assetResultedge ARE, graph.AssetNode AN					
	                                where 
		                                Match (AN -(ARE)-> AR)
		                                and 
                                        AN.Uid = @owningAssetUid
		                                and 
		                                ARE.Class = {(int)ResultRelationClass.Owns}
                                        {effectiveSQL}
	                                ) DQR";

            string sortPhrase = "EffectiveDate";

            if (!string.IsNullOrWhiteSpace(sort))
            {                                
                sortPhrase = sort;                
            }
            
            orderBy = $"Order by {sortPhrase} {direction ?? ""}";

            if (evaluatedAssetUid != null)
            {
                evaluatedAssetSQL = $@"inner Join 
	                            (		
		                            select 
			                            AR.Uid resultUid, AN.Uid evaluatedAssetUid, AN.Path EvaluatedAssetPath, 
                                        AP.Path EvaluatedAssetTypePath, case when AN.class = {(int)AssetTypeClass.BusinessAsset} then '{AssetTypeClass.BusinessAsset.GetDisplayName()}' when AN.class = {(int)AssetTypeClass.TechnicalAsset} then '{AssetTypeClass.TechnicalAsset.GetDisplayName()}' else '' end EvaluatedAssetClass
		                            from 			                            
                                        AssetResult AR
				                        inner join 
				                        assetResultedge ARE on AR.$node_id = ARE.$to_id and ARE.Class = {(int)ResultRelationClass.EvaluatedBy}		
				                        inner join 
				                        graph.AssetNode AN on AN.$node_id = ARE.$from_id and AN.Uid = @evaluatedAssetUid				                        
				                        inner join
				                        AssetType AT on AT.Uid = AN.AssetTypeUid
				                        cross apply dbo.GetAssetTypeTextPathById(AT.id,'/') AP				                            
	                            ) DQA on DQA.resultUid=DQR.resultUid";

            }
            else
            {
                evaluatedAssetSQL = $@"left Join
	                            (		
		                            select 
			                            AR.Uid resultUid, AN.Uid evaluatedAssetUid, AN.Path EvaluatedAssetPath, 
                                        AP.Path EvaluatedAssetTypePath, case when AN.class = {(int)AssetTypeClass.BusinessAsset} then '{AssetTypeClass.BusinessAsset.GetDisplayName()}' when AN.class = {(int)AssetTypeClass.TechnicalAsset} then '{AssetTypeClass.TechnicalAsset.GetDisplayName()}' else '' end EvaluatedAssetClass
		                            from 
			                            AssetResult AR
				                        inner join 
				                        assetResultedge ARE on AR.$node_id = ARE.$to_id and ARE.Class = {(int)ResultRelationClass.EvaluatedBy}		
				                        inner join 
				                        graph.AssetNode AN on AN.$node_id = ARE.$from_id				                        
				                        inner join
				                        AssetType AT on AT.Uid = AN.AssetTypeUid
				                        cross apply dbo.GetAssetTypeTextPathById(AT.id,'/') AP				
		                            where 
				                        AR.UID in ( 
							                        select 
								                        AR1.Uid
							                        from 
								                        AssetResult AR1, assetResultedge ARE1, graph.AssetNode AN1 
							                        where 
								                        Match (AN1 -(ARE1)-> AR1) 
								                        and 
								                        AN1.Uid = @owningAssetUid 
								                        and 
								                        ARE1.Class = {(int)ResultRelationClass.Owns}
                                                        {effectiveSQL}
                                                        )
	                            ) DQA on DQA.resultUid=DQR.resultUid";
            }
            var countSql = $@"select 
	                            Count(distinct DQR.resultUid)
                            from 
	                            {owningAssetSQL}
	                            {evaluatedAssetSQL}";

            var dataQualityResultSql = $@"select 
	                        distinct DQR.resultUid as ResultUid, DQR.OwningAssetUid as OwningAssetUid, DQA.evaluatedAssetUid as EvaluatedAssetUid, DQA.EvaluatedAssetPath as EvaluatedAssetPath, DQA.EvaluatedAssetTypePath as EvaluatedAssetTypePath, DQA.EvaluatedAssetClass as EvaluatedAssetClass, DQR.EffectiveDate as EffectiveDate, DQR.RunDate as RunDate, DQR.Passcount as Passcount, DQR.FailCount as FailCount, DQR.PassFraction as PassFraction, P.Passed as Passed
                        from 
	                        {owningAssetSQL}
	                        {evaluatedAssetSQL}
	                        cross apply 
	                        CalculatePassedPropertyForAssetResult(DQR.resultUid) P
	                        {orderBy}
	                        offset ((@pageNum-1)*@pageSize) rows fetch next @pageSize rows only";

            result.pageNum = pageNum;
            result.pageSize = pageSize;

            parameters.Add("@evaluatedAssetUid", evaluatedAssetUid);
            parameters.Add("@owningAssetUid", owningAssetUid);
            parameters.Add("@pageNum", result.pageNum);
            parameters.Add("@pageSize", result.pageSize);

            result.total = Company.Query<int>(countSql, parameters).FirstOrDefault();

            result.items = Company.Query<DataQualityGetResultItem>(dataQualityResultSql, parameters).ToList();
            if (result.items == null) result.items = new List<DataQualityGetResultItem>();
            return result;
        }

        public List<DataQualityAssetResultModel> GetAssetResultDetailsByUid(Guid value)
        {
            var result = new List<DataQualityAssetResultModel>();
            var parameters = new DynamicParameters();
            parameters.Add("@Uid", value);

            string assetResultSQL = $@"select 
	                                    AR.Uid as ResultUid, ARE.[Class] as Class, AN.UID as AssetUid, AR.EffectiveDate as EffectiveDate, AR.RunDate as RunDate
                                    from 
	                                    AssetResult AR, assetResultedge ARE, graph.AssetNode AN					
                                    where 
	                                    Match (AN -(ARE)-> AR)
	                                    and 
	                                    AR.Uid = @Uid";

            result = Company.Query<DataQualityAssetResultModel>(assetResultSQL, parameters).ToList();

            return result;

        }

        public List<DataQualityDeleteResponseModel> DeleteDataQualityResult(List<DataQualityDeleteModel> request, ApiExecution execution)
        {
            Company.Add(execution);

            List<DataQualityDeleteResponseModel> results = null;
            try
            {
                results = Company.DeleteAssetResults(request, execution);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }

        public List<DataQualityResponseModel> UpdateDataQualityResult(List<DataQualityUpdateModel> request, ApiExecution execution)
        {
            Company.Add(execution);

            List<DataQualityResponseModel> results = null;
            try
            {
                List<IDataQualityUpsert> upsert = new List<IDataQualityUpsert>();
                upsert.AddRange(request);
                results = Company.UpsertAssetResults(upsert, execution);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);
            }

            return results;
        }
    }
}
