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
            model.State = State.Deleted;
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
                var fieldType = Company.FieldTypes.FirstOrDefault(x => x.ID == condition.FieldTypeID);
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
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{condition.FieldTypeID}' does not contain valid '{fieldType.Type}' value!");
                        break;
                    case "Decimal":
                        if (!decimal.TryParse(condition.Values, out tempDecimal))
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{condition.FieldTypeID}' does not contain valid '{fieldType.Type}' value!");
                        break;
                    case "Date":
                        if (!DateTime.TryParse(condition.Values, out tempDate))
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{condition.FieldTypeID}' does not contain valid '{fieldType.Type}' value!");
                        break;
                    case "Number":
                        if (!int.TryParse(condition.Values, out tempInt))
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{condition.FieldTypeID}' does not contain valid '{fieldType.Type}' value!");
                        break;
                    default:
                        break;
                }

            }


            if (model.Uid != Guid.Empty)
            {
                isNew = false;

                existingResultCount = Company.Query<int>("select count(1) from metrics.ScoreItem where MetricAssetUid = @Uid", new { model.Uid }).Single();

                childMetricCount = Company.Query<int>("select count(1) from metrics.Asset where ParentUid = @Uid", new { model.Uid }).Single();

                metricAsset = Company.Filter<MetricAsset>(i => i.Uid == model.Uid).SingleOrDefault();
                if (metricAsset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Error updating metric", "Metric not found.");
                }

                metricAsset.Description = model.Description;
                metricAsset.Name = model.Name.Trim();

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
                    State = State.Active
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
                int metricExistsCount = 0;
                metricExistsCount = (model.ParentUid.HasValue) ?
                    Company.Query<int>($"select count(1) from metrics.Asset where lower(Name) = @n and ParentUid = @p", new { n = model.Name.Trim().ToLower(), p = model.ParentUid.Value }).Single() :
                    Company.Query<int>($"select count(1) from metrics.Asset where lower(Name) = @n and ParentUid is null", new { n = model.Name.Trim().ToLower() }).Single();

                if (metricExistsCount > 0)
                {
                    return new WorkHttpStatus(
                        HttpStatusCode.BadRequest,
                        "Error adding metric",
                        (model.ParentUid.HasValue) ?
                        "You may not add a metric with the same name under the same grouping." :
                        "You may not add a metric with the same name at the root of the hierarchy.");
                }

                Company.MetricAssets.Add(metricAsset);
            }

            var cleanDate = model.EffectiveDate.Date;
            var metricAssetVersion = Company.Filter<MetricAssetVersion>(i => i.Uid == model.Uid && i.EffectiveDate == cleanDate, v => v.Conditions).SingleOrDefault();

            string newConditionHash = string.Join("|", model.Conditions.Select(c => string.Join(";", c.FieldTypeID, c.Operator, c.Values)));
            newConditionHash = newConditionHash.GetD3sHashString();
            if (metricAssetVersion == null)
            {
                var maxEffectiveDate = Company.Query<DateTime?>("select max(EffectiveDate) from metrics.AssetVersion where [Uid] = @Uid", new { model.Uid }).SingleOrDefault();

                if (maxEffectiveDate.HasValue)
                {
                    if (maxEffectiveDate.Value > cleanDate)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"You may not backdate the effective date for this metric. You must provide date more recent than {maxEffectiveDate.Value.ToShortDateString()}");
                    }
                }

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
                    EffectiveDate = model.EffectiveDate,
                    Weight = model.Weight
                };

                if (model.Conditions.Count > 0)
                {
                    if (metricAssetVersion.Conditions == null)
                        metricAssetVersion.Conditions = new List<MetricAssetVersionCondition>();

                    var usedFieldTypeIDs = new List<int>();

                    model.Conditions.ForEach(c =>
                    {
                        // You can only add one of a specific field type ID.
                        if (!usedFieldTypeIDs.Contains(c.FieldTypeID))
                        {
                            metricAssetVersion.Conditions.Add(new MetricAssetVersionCondition { FieldTypeID = c.FieldTypeID, Operator = c.Operator, ValueJson = c.Values });
                            usedFieldTypeIDs.Add(c.FieldTypeID);
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
                            metricAssetVersion.Conditions.Add(new MetricAssetVersionCondition { FieldTypeID = c.FieldTypeID, Operator = c.Operator, ValueJson = c.Values });
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

            return new WorkHttpStatus(HttpStatusCode.OK,"","");
        }

        public MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(Guid assetTypeUid, DateTime? effectiveDate)
        {
            SqlConnection cnn = Company.Database.Connection as SqlConnection;
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

        public MetricAssetHierarchyModels GetMetricHierarchyByAsset(Guid assetUid, DateTime? effectiveDate)
        {
            SqlConnection cnn = Company.Database.Connection as SqlConnection;

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

        public List<string> GetMetricStructureFragments(Guid assetTypeUid)
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
                    where	A.AssetTypeUid = '{assetTypeUid.ToString()}'
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

        public MetricScoreApiModel GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string,string>> queryParams)
        {
            var result = new MetricScoreApiModel();
            List<SqlParameter> sqlParameters = new List<SqlParameter>();


            sqlParameters.Add(new SqlParameter("@pageSize", result.pageSize));
            sqlParameters.Add(new SqlParameter("@pageNum", result.pageNum));

            var baseSql = $@"
                    ;WITH Scores_CTE AS (
                    select 
	                     A.uid
	                     from metrics.Score MS
		                    inner join Asset A on A.uid = MS.AssetUid
		                    inner join AssetType AT on A.AssetTypeID = AT.ID
                    )
                    select 
                    @pageSize,
                    @pageNum,
                    (select count(*) from Scores_CTE) as total,
                    (select 
	                    LOWER(uid) as AssetUid,
	                    (select EffectiveDate, Value as Score 
		                     from metrics.Score 
		                     where AssetUid = uid 
		                     order by EffectiveDate desc
		                     for json path) as Scores
		                     from Scores_CTE
	                    group by uid
	                    order by uid
	                    offset (@pageNum-1) rows fetch next @pageSize rows only
                    for json path) as items
                    ";


            result = Company.Query<MetricScoreApiModel>(baseSql, sqlParameters).FirstOrDefault();

            return result;
        }

    }
}
