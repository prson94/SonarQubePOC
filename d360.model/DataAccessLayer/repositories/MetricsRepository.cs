using d360.core.entities;
using d360.core.entities.Metric;
using d360.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d360.core.enums;
using d360.model;
using System.Net;
using d360.core;
using System.Data.SqlClient;
using Dapper;
using Newtonsoft.Json;
using System.Data;
using d360.model.DataAccessLayer.repositories;
using d360.core.queue;


namespace d360.model.DataAccessLayer
{
    public class MetricsRepository : BaseRepository, IMetricsRepository
    {
        internal ICompanyContext Company;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;
               

        public MetricsRepository(ICompanyContext context, IQueueSource queueSource, IStorageProvider storageProvider) : base(context)
        {
            this.Company = context;
            this.QueueSource = queueSource;
            this.StorageProvider = storageProvider;
        }

        public void DeleteMetric(MetricAsset model)
        {
            var currentAssetVersion = model.Versions.OrderByDescending(x => x.EffectiveDate).FirstOrDefault();
            currentAssetVersion.State = State.Deleted;

            var lastUsedMetric = GetMetricsLastUsedEffectiveDate(currentAssetVersion.Uid);
            if (lastUsedMetric == null)
                currentAssetVersion.EffectiveEndDate = DateTime.Now.Date;
            else
                currentAssetVersion.EffectiveEndDate = lastUsedMetric;

            model.State = State.Deleted;
            model.UpdatedOn = DateTime.Now;
            var children = Company.MetricAssets.Where(x => x.ParentUid != null && x.ParentUid == model.Uid).ToList();

            if (children.Count > 0)
            {
                children.ForEach(c => c.State = State.Deleted);
            }

            var olderVersions = Company.MetricAssetVersions.Where(x => x.Uid == currentAssetVersion.Uid).ToList();
            if (olderVersions.Count > 0)
            {
                olderVersions.ForEach(x => x.State = State.Deleted);
            }

            Company.SaveChanges();
        }

        public MetricAssetViewDetailModel GetMetricViewModelByUid(Guid uid, DateTime? effectiveDate)
        {
            var model = (
                        from a in Company.MetricAssets.Include("Allocation").Include("Versions.Conditions.Items.Values")
                        from v in a.Versions
                        where a.Uid == uid
                        where (
                                (!effectiveDate.HasValue && v.EffectiveEndDate == null) ||
                                (effectiveDate.HasValue && v.EffectiveDate <= effectiveDate.Value && v.EffectiveEndDate >= effectiveDate.Value)
                              )
                        select new MetricAssetViewDetailModel
                        {
                            AllocationUid = a.AllocationUid,
                            ConditionGroups = v.Conditions.Select(g => new MetricAssetVersionConditionViewModel { 
                                ConditionItems = g.Items.Select(i => new MetricAssetVersionConditionItemViewModel {
                                    ConditionFieldTypeID = i.ConditionFieldTypeID,
                                    ConditionIntersectTypeID = i.ConditionIntersectTypeID,
                                    ConditionType = i.ConditionType,
                                    Operator = i.Operator,
                                    Uid = i.Uid,
                                    Values = i.Values.ToList()
                                }).ToList(),
                                MatchType = g.MatchType, 
                                Position = g.Position, 
                                Threshold = g.Threshold, 
                                Uid = g.Uid, 
                                Weight = g.Weight 
                            }).ToList(),
                            Versions = a.Versions.Select(v => new MetricAssetVersionViewModel {
                              ConditionAndOr = v.ConditionAndOr,
                              Description = v.Description,
                              EffectiveDate = v.EffectiveDate,
                              EffectiveEndDate = v.EffectiveEndDate,
                              MatchConditionsOnly = v.MatchConditionsOnly,
                              Name = v.Name,
                              Threshold = v.Threshold,
                              Uid = v.Uid,
                              UpdateFrequency = v.UpdateFrequency,
                              Weight = v.Weight
                            }).ToList(),
                            AssetTypeUid = a.Allocation.AssetTypeUid,
                            MatchConditionsOnly = v.MatchConditionsOnly,
                            Description = v.Description,
                            EffectiveDate = v.EffectiveDate,
                            IsGroup = a.IsGroup,
                            Name = v.Name,
                            ParentUid = a.ParentUid,
                            ScoreType = a.Allocation.ScoreType,
                            Threshold = v.Threshold,
                            Uid = a.Uid,
                            UpdateFrequency = v.UpdateFrequency,
                            Weight = v.Weight
                        }).FirstOrDefault();
            
            return model;
        }

        public MetricAsset GetMetricByUid(Guid uid)
        {
            return Company.GetByUid<MetricAsset>(uid, i => i.Children);
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
                isNew = false;
                metricAsset = Company.GetByUid<MetricAsset>(model.Uid, i => i.Allocation);
                if (metricAsset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Error updating metric", "Metric not found.");
                }
                Guid assetTypeId = metricAsset.Allocation.AssetTypeUid;
                targetAssetType = Company.Filter<AssetType>(x => x.uid == assetTypeId).SingleOrDefault();
            }
            else
            {
                if (model.AllocationUid != null && model.AllocationUid != Guid.Empty)
                {
                    targetAssetType = (
                                      from allocation in Company.MetricAllocations
                                      join assettype in Company.AssetTypes on allocation.AssetTypeUid equals assettype.uid
                                      where allocation.Uid == model.AllocationUid
                                      select assettype
                                      ).SingleOrDefault();
                }
                else
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error adding metric", "You must provide a valid AllocationUid.");
                }
            }

            var existingResultCount = 0;
            var childMetricCount = 0;

            List<string> validTypes = new List<string>() { "Boolean", "Decimal", "Date", "Lookup", "Number", "Text" };
            var operators = new List<string>() { "eq", "neq", "lt", "lte", "gt", "gte" };
            var operatorErrorMessage = "";

            if (!model.IsGroup)
            {
                foreach (var group in model.ConditionGroups)
                {
                    foreach (var condition in group.ConditionItems)
                    {
                        var fieldType = new FieldType();

                        if (condition.ConditionFieldTypeID.HasValue)
                        {
                            fieldType = Company.FieldTypes.FirstOrDefault(x => x.ID == condition.ConditionFieldTypeID.Value);
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

                        if (!operators.Contains(condition.Operator))
                        {
                            operatorErrorMessage += $"Invalid operator used: {condition.Operator}; ";
                        }

                        bool tempBool;
                        decimal tempDecimal;
                        DateTime tempDate;
                        int tempInt;

                        switch (fieldType.Type)
                        {
                            case "Boolean":
                                if (!bool.TryParse(condition.Values[0].Value, out tempBool))
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{fieldType.Name}' does not contain valid '{fieldType.Type}' value!");
                                break;
                            case "Decimal":
                                if (!decimal.TryParse(condition.Values[0].Value, out tempDecimal))
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{fieldType.Name}' does not contain valid '{fieldType.Type}' value!");
                                break;
                            case "Date":
                                if (!DateTime.TryParse(condition.Values[0].Value, out tempDate))
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{fieldType.Name}' does not contain valid '{fieldType.Type}' value!");
                                condition.Values[0].Value = tempDate.ToShortDateString();
                                break;
                            case "Number":
                                if (!int.TryParse(condition.Values[0].Value, out tempInt))
                                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"Field '{fieldType.Name}' does not contain valid '{fieldType.Type}' value!");
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(model.Name) && model.Name.Length > 250)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, $"Error " + ((isNew) ? "adding" : "updating") + " metric", "Name cannot be longer than 250 characters.");
            }

            if (!string.IsNullOrEmpty(operatorErrorMessage))
            {
                operatorErrorMessage += $"Only the operators ({string.Join(", ", operators)}) may be used.";
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", operatorErrorMessage);
            }

            int metricExistsCount = 0;
            var metricCountSql = $@"select count(1) from (
select A.Uid, max(V.EffectiveDate) as EffectiveDate
from metrics.Asset A inner join metrics.AssetVersion V on V.AssetUid = A.Uid and A.State = 1 and A.AllocationUid = @AllocationUid and A.Uid <> @Uid and lower(V.Name) = @n and {(model.ParentUid.HasValue ? "A.ParentUid = @p" : "A.ParentUid is null")} group by A.Uid) O";
            metricExistsCount = Company.Query<int>(metricCountSql, new { n = model.Name.Trim().ToLower(), p = model.ParentUid, model.AllocationUid, model.Uid }).Single();

            if (metricExistsCount > 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error adding metric",
                    (model.ParentUid.HasValue) ?
                    "You may not add a metric with the same name under the same grouping." :
                    $"Measure with name '{model.Name}' already exists.");
            }

            #region Asset

            if (isNew)
            {
                metricAsset = new MetricAsset
                {
                    Uid = Guid.NewGuid(),
                    AllocationUid = model.AllocationUid,
                    IsGroup = model.IsGroup,
                    State = State.Active
                };

                if (model.ParentUid != Guid.Empty && model.ParentUid.HasValue)
                {
                    var parentExists = Company.Any<MetricAsset>(i => i.AllocationUid == model.AllocationUid && i.Uid == model.ParentUid.Value);
                    if (!parentExists)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Error updating metric", "Parent metric not found.");
                    }
                    metricAsset.ParentUid = model.ParentUid;
                }

                Company.Add(metricAsset);
            }
            else 
            {
                existingResultCount = Company.Query<int>("select count(1) from metrics.ScoreItem I inner join metrics.AssetVersion V on V.Uid = I.AssetVersionUid and V.AssetUid = @Uid", new { model.Uid }).Single();
                childMetricCount = Company.Query<int>("select count(1) from metrics.Asset where ParentUid = @Uid and State = 1", new { model.Uid }).Single();

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

            #endregion

            var effectiveDate = model.EffectiveDate == DateTime.MinValue ? DateTime.UtcNow : model.EffectiveDate;

            var maxEffectiveDate = Company.Query<DateTime?>("select max(EffectiveDate) from metrics.AssetVersion where AssetUid = @Uid", new { model.Uid }).SingleOrDefault();

            if (maxEffectiveDate.HasValue)
            {
                if (maxEffectiveDate.Value > effectiveDate.Date)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", $"You may not backdate the effective date for this metric. You must provide date more recent than {maxEffectiveDate.Value.ToShortDateString()}");
                }
            }

            #region Version

            var metricAssetVersion = Company.Filter<MetricAssetVersion>(i => i.AssetUid == model.Uid && i.EffectiveDate == effectiveDate, v => v.Conditions).SingleOrDefault();

            var hashItems = from g in model.ConditionGroups
                            from c in g.ConditionItems
                            from v in c.Values
                            orderby g.Position, c.ConditionFieldTypeID, c.ConditionIntersectTypeID, v.Value
                            select $"{g.MatchType};{g.Position};{g.Weight};{c.ConditionFieldTypeID};{c.ConditionIntersectTypeID};{c.ConditionType};{c.Operator};{v.Value}";
            string newConditionHash = string.Join("|", hashItems);
            newConditionHash = newConditionHash.GetD3sHashString();
            if (metricAssetVersion == null)
            {
                metricAssetVersion = new MetricAssetVersion
                {
                    AssetUid = metricAsset.Uid,
                    Name = model.Name,
                    Description = model.Description,
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    MatchConditionsOnly = model.MatchConditionsOnly,
                    EffectiveDate = effectiveDate,
                    Threshold = model.Threshold,
                    Weight = model.Weight,
                    State = metricAsset.State,
                    EffectiveEndDate = null,
                    Definition = model.ScoreType == ScoreType.Governance ? "{ \"Check\": \"External\"}" : "{}"
                };

                // End-date the now previous version, if any.
                var existingAssetVersions = Company.Filter<MetricAssetVersion>(x => x.AssetUid == metricAsset.Uid && x.EffectiveEndDate == null)
                    .OrderByDescending(x => x.EffectiveDate)
                    .ToList();
                for (var i = 0; i < existingAssetVersions.Count; i++)
                {
                    if (i == 0)
                    {
                        var endDateToUse = (i == 0) ? effectiveDate : existingAssetVersions[i - 1].EffectiveDate;
                        endDateToUse = endDateToUse.AddDays(-1);
                        existingAssetVersions[i].EffectiveEndDate = endDateToUse;
                        Company.Update(existingAssetVersions[i]);
                    }
                }

                Company.Add(metricAssetVersion);
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

                    if (metricAssetVersion.MatchConditionsOnly != model.MatchConditionsOnly)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "You may not alter the condition type of this metric without also altering its effective date.");
                    }

                    var existingHashItems = from g in metricAssetVersion.Conditions
                                            from c in g.Items
                                            from v in c.Values
                                            orderby g.Position, c.ConditionFieldTypeID, c.ConditionIntersectTypeID, v.Value
                                            select $"{g.MatchType};{g.Position};{g.Weight};{c.ConditionFieldTypeID};{c.ConditionIntersectTypeID};{c.ConditionType};{c.Operator};{v.Value}";
                    string existingConditionHash = string.Join("|", existingHashItems);
                    existingConditionHash = newConditionHash.GetD3sHashString();
                    if (newConditionHash != existingConditionHash)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Error updating metric", "You may not alter the conditions of this metric without also altering its effective date.");
                    }
                }

                // Set the properties.
                metricAssetVersion.Name = model.Name;
                metricAssetVersion.Description = model.Description;
                metricAssetVersion.MatchConditionsOnly = model.MatchConditionsOnly;
                metricAssetVersion.Threshold = model.Threshold;
                metricAssetVersion.Weight = model.Weight;
            }

            #endregion

            #region Process conditions for ADDs or UPDATEs

            if (model.IsGroup)
            {
                model.ConditionGroups.Clear();
            }

            if (model.ConditionGroups.Count > 0)
            {
                if (metricAssetVersion.Conditions == null)
                    metricAssetVersion.Conditions = new List<MetricAssetVersionCondition>();

                model.ConditionGroups.ForEach(g =>
                {
                    var usedFieldTypeIDs = new List<int>();
                    var usedIntersectTypeIDs = new List<int>();

                    var cg = metricAssetVersion.Conditions.SingleOrDefault(i => i.Uid == g.Uid);

                    if (g.ConditionItems.Count > 0)
                    {
                        var isNewGroup = (cg == null);
                        if (isNewGroup) 
                        {
                            cg = new MetricAssetVersionCondition();
                        }

                        // Update the group's properties.
                        cg.MatchType = g.MatchType;
                        cg.Position = g.Position;
                        cg.Threshold = g.Threshold;
                        cg.Weight = g.Weight;

                        if (cg.Items == null)
                        {
                            cg.Items = new List<MetricAssetVersionConditionItem>();
                        }

                        g.ConditionItems.ForEach(c =>
                        {
                            var ci = cg.Items.SingleOrDefault(i => (i.Uid != Guid.Empty) && (i.Uid == c.Uid));

                            if (ci == null)
                            {
                                ci = new MetricAssetVersionConditionItem();
                            }

                            Action<MetricAssetVersionConditionItem, List<MetricAssetVersionConditionItemValue>> checkValues = delegate (MetricAssetVersionConditionItem item, List<MetricAssetVersionConditionItemValue> newValues) {
                                if (item.Values != null)
                                {
                                    item.Values.Clear();
                                }

                                newValues.ForEach(nv =>
                                {
                                    if (item.Values == null)
                                    {
                                        item.Values = new List<MetricAssetVersionConditionItemValue>();
                                    }
                                    if (!item.Values.Any(ev => ev.Value == nv.Value) && nv.Value != null)
                                    {
                                        item.Values.Add(nv);
                                    }
                                });
                            };

                            if (c.ConditionFieldTypeID.HasValue)
                            {
                                // Only one of the specific field per condition group.
                                if (!usedFieldTypeIDs.Contains(c.ConditionFieldTypeID.Value))
                                {
                                    ci.ConditionFieldTypeID = c.ConditionFieldTypeID.Value;
                                    ci.ConditionType = c.ConditionType;
                                    ci.Operator = c.Operator;

                                    checkValues(ci, c.Values);
                                    ci.Updated = true;

                                    if (ci.Uid == Guid.Empty || ci.Uid == null) 
                                    {
                                        cg.Items.Add(ci);
                                    }

                                    usedFieldTypeIDs.Add(c.ConditionFieldTypeID.Value);
                                }
                            }
                            else if (c.ConditionIntersectTypeID.HasValue)
                            {
                                // Only one of the specific relationship per condition group.
                                if (!usedIntersectTypeIDs.Contains(c.ConditionFieldTypeID.Value))
                                {
                                    ci.ConditionIntersectTypeID = c.ConditionIntersectTypeID;
                                    ci.ConditionType = c.ConditionType;
                                    ci.Operator = c.Operator;
                                    checkValues(ci, c.Values);
                                    ci.Updated = true;

                                    if (ci.Uid == Guid.Empty || ci.Uid == null)
                                    {
                                        cg.Items.Add(ci);
                                    }

                                    usedIntersectTypeIDs.Add(c.ConditionFieldTypeID.Value);
                                }
                            }
                        });

                        // Now remove the items that were NOT updated during this process.
                        while (cg.Items.Any(i => !i.Updated))
                        {
                            var itemToRemove = cg.Items.First(i => !i.Updated);
                            Company.MetricAssetVersionConditionItems.Remove(itemToRemove);
                            //cg.Items.Remove(itemToRemove);
                        }

                        cg.Updated = true;
                        if (cg.Uid == Guid.Empty || cg.Uid == null)
                        {
                            metricAssetVersion.Conditions.Add(cg);
                        }
                    }
                    else 
                    {
                        if (cg != null)
                        {
                            metricAssetVersion.Conditions.Remove(cg); //This is now an empty group, so remove the group entirely.
                        }
                    }
                });

                // Now remove the groups that were NOT updated during this process.
                while (metricAssetVersion.Conditions.Any(i => !i.Updated))
                {
                    var itemToRemove = metricAssetVersion.Conditions.First(i => !i.Updated);
                    //metricAssetVersion.Conditions.Remove(itemToRemove);
                    Company.MetricAssetVersionConditions.Remove(itemToRemove);
                }
            }
            else 
            {
                if (metricAssetVersion.Conditions != null)
                { 
                     metricAssetVersion.Conditions.ToList().ForEach(g =>
                    {
                        g.Items.ToList().ForEach(i => {
                            i.Values.ToList().ForEach(v => {
                                Company.Entry(v).State = System.Data.Entity.EntityState.Deleted;
                            });
                            Company.Entry(i).State = System.Data.Entity.EntityState.Deleted;
                        });
                        Company.Entry(g).State = System.Data.Entity.EntityState.Deleted;
                    });               
                }
            }

            #endregion

            Company.Update(metricAssetVersion);

            if (isNew)
            {
                Company.SendScoreEventWithPayload(Guid.NewGuid(), ScoreQueueChangeType.MeasureCreated, metricAsset);
            }
            {
                Company.SendScoreEventWithPayload(Guid.NewGuid(), ScoreQueueChangeType.MeasureChanged, metricAsset);
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
                    create table #tbl ([Uid] uniqueidentifier, VersionUid uniqueidentifier, Name nvarchar(250), ParentUid uniqueidentifier, IsGroup bit, Weight decimal(5,3), EffectiveDate date, Description nvarchar(4000))
                    
                    insert into #tbl 
                    	select	A.[Uid],
                    			V.Uid,
                                V.Name,
                    			A.ParentUid,
                    			A.IsGroup,
                    			V.Weight,
                    			V.EffectiveDate,
                    			V.Description

                    	from	metrics.AssetVersion V
                    			inner join (
                    					select		IA.[Uid],
                    								max(IV.EffectiveDate) as EffectiveDate
                    					from		metrics.AssetVersion IV
                    								inner join metrics.Asset IA on IA.[Uid] = IV.AssetUid 
                    															and IV.EffectiveDate <= @effectiveDate 
                    															and ((IA.State = 1 and EffectiveEndDate is null) or (IA.State = 3 and EffectiveEndDate >= @effectiveDate))
                                                    inner join metrics.Allocation Al on Al.Uid = IA.AllocationUid and Al.AssetTypeUid = @assetTypeUid 
                    					group by	IA.[Uid]
                    			) MV on MV.[Uid] = V.AssetUid AND MV.EffectiveDate = V.EffectiveDate
                    			inner join metrics.Asset A on A.[Uid] = V.AssetUid;
                    
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
                    					CI.Operator,
                    					(case WHEN F.Type = 'Lookup' THEN FL.Text ELSE CIV.Value END) as [Value]
                    			from	[metrics].[AssetVersionCondition] C
                                        inner join metrics.AssetVersionConditionItem CI on CI.AssetVersionConditionUid = C.Uid
                                        inner join metrics.AssetVersionConditionItemValue CIV on CIV.Uid = CI.Uid 
                    					inner join FieldType F on F.ID = CI.ConditionFieldTypeID
                                        inner join FieldLookupValue FL on FL.FieldTypeID = F.ID and F.LookupObjectType = FL.LookupObjectType and F.LookupObjectID = FL.LookupObjectID 
                                                                        and FL.[Value] = CIV.Value
                    			where	C.AssetVersionUid = h.VersionUid
                    			for json path
                    		) as ConditionsJson
                    from	h
                    order by [Level] asc";

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.Open();

            var results = cnn.Query<MetricAssetTypeHierarchyModel>(sql, new { assetTypeUid, effectiveDate = effectiveDate.Value }, commandTimeout: ApiTimeout).ToList();
            var model = new MetricAssetTypeHierarchyModels();
            var builder = new MetricHierarchyBuilder();

            foreach (var i in results.Where(i => !i.ParentUid.HasValue))
            {
                builder.BuildMetricHierarchy(results, model, null, i);
            }

            return model;
        }

        public MetricAssetHierarchyModels GetMetricHierarchyByAsset(Guid allocationUid, Guid assetUid, DateTime? effectiveDate)
        {
            SqlConnection cnn = Company.Database.Connection as SqlConnection;

            if (!effectiveDate.HasValue)
                effectiveDate = DateTime.UtcNow.Date;
            else
                effectiveDate = effectiveDate.Value.ToUniversalTime();
            
            string sql = $@"
declare @lastScoredDate date =  (
    select  top 1 
            RunDate 
    from    metrics.Score
    where   AllocationUid = @allocationUid and AssetUid = @assetUid 
    order by    RunDate desc
    )

if @effectiveDate > @lastScoredDate
begin
    set @effectiveDate = @lastScoredDate
end

select	*
from	(
		select  Ma.[Uid], 
				Ma.ParentUid,
				V.Name,
				V.Description,
				Ma.IsGroup,
				V.EffectiveDate, 
				ROW_NUMBER() OVER(PARTITION BY Ma.Uid ORDER BY S.EffectiveDate DESC) as RowNum,
				V.EffectiveEndDate as EndDate,
				SI.AdjustedWeight as [Weight],
				SI.AdjustedMaxWeight,
				SI.Value,
				A.ScoreType
		from    metrics.Score S 
				inner join metrics.Allocation A on A.Uid = S.AllocationUid
                inner join metrics.ScoreItemLink SIL on SIL.ScoreUid = S.Uid 
				inner join metrics.ScoreItem SI on SI.Uid = SIL.ScoreItemUid
				inner join metrics.AssetVersion V on V.Uid = SI.AssetVersionUid
				inner join metrics.Asset Ma on Ma.Uid = V.AssetUid
        where   S.AllocationUid = @allocationUid 
                and S.AssetUid = @assetUid 
				and S.EffectiveDate <= @effectiveDate 
				and (S.EndDate >= @effectiveDate or S.EndDate is null)
		) O 
where	O.RowNum = 1";


            if (cnn.State != ConnectionState.Open)
                cnn.Open();

            var results = cnn.Query<MetricAssetHierarchyModel>(sql, new { allocationUid, assetUid, effectiveDate = effectiveDate.Value }, commandTimeout: ApiTimeout).ToList();

            var model = new MetricAssetHierarchyModels();
            results.ForEach(i => { model.Add(i); });
            return model;
        }

        public List<int> GetScoreTypesForAsset(Guid assetUid)
        {
            var sql = $@"
select  distinct 
        ma.scoretype 
from    metrics.Allocation  ma
		inner join metrics.score ms on ms.AssetUid = @assetUid and ma.Uid = ms.AllocationUid and ma.[state] = 1 and ms.EndDate is null";
            return Company.Query<int>(sql, new { assetUid }, ApiTimeout).ToList();
        }

        public List<string> GetMetricStructureFragments(Guid allocationUid)
        {
            return Company.Query<string>($@"
                    select	A.Uid,
                    		A.ParentUid,
                            A.AllocationUid,
                    		Al.AssetTypeUid,
                    		A.IsGroup,
                    		V.Name,
                    		V.Description,
                    		V.EffectiveDate,
							V.Weight,
                    		V.Threshold,
							V.UpdateFrequency,
							V.MatchConditionsOnly,
							(
                    			select		C.Uid,
											C.Position,
											C.Threshold,
											C.Weight,
											C.MatchType,
											(
												select	CI.Uid,
														CI.ConditionType,
														CI.ConditionFieldTypeID,
														CI.ConditionIntersectTypeID,
														CI.Operator,
														(
															select	[Value]
															from	metrics.AssetVersionConditionItemValue
															where	Uid = CI.Uid
															for json path
														) as [Values]
												from	metrics.AssetVersionConditionItem CI
												where	CI.AssetVersionConditionUid = C.Uid
												for json path
											) as ConditionItems
                    			from		metrics.AssetVersionCondition C
                    			where		C.AssetVersionUid = V.Uid
								order by	C.Position
                    			for		json path
                    		) as ConditionGroups,
                            VC.Count as [VersionCount]
                    from	metrics.Asset A
                    		inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.Uid = @allocationUid
                            cross apply (
                    			select	max(EffectiveDate) as EffectiveDate
                    			from	metrics.AssetVersion
                    			where	AssetUid = A.Uid
                    		) MV
                    		inner join metrics.AssetVersion V on V.AssetUid = A.Uid and V.EffectiveDate = MV.EffectiveDate and A.[State] = 1
                            cross apply (select count(1) as [Count] from metrics.AssetVersion where AssetUid = A.Uid) VC
                    for		json path", new { allocationUid }, ApiTimeout).ToList();
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
                            		inner join FieldType F on F.AssetTypeID = A.ID and A.[uid] = @assetTypeUid and F.Type in ('Boolean', 'Decimal', 'Date', 'Lookup', 'Number', 'Text')
                            for		json path", new { assetTypeUid }, ApiTimeout).ToList();
        }

        public List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution)
        {
            return Company.BulkMetricsImport(model, execution);
        }

        public async Task<IEnumerable<MetricPathOptionViewModel>> GetMetricPathOptionsBy(int assetTypeId, ScoreType scoreType)
        {
            var sql = @"
select  *
from    (
        select	P.Uid,
		        P.State,
		        metrics.CalculateRollupPath(P.Uid) as [Path],
		        (
                    select      A.Uid as AssetTypeUid,
					            A.Name
                    from        [metrics].[RollupPathSegment] SE
                                inner join AssetType A on A.ID = SE.AssetTypeID
                    where       RollupPathUid = P.Uid
                    order by    [Position]
                    for json path
                ) as SegmentsJson
        from    [metrics].[RollupPath] P
        where   P.ScoreType = @scoreType 
                and P.AssetTypeid = @assetTypeId
        ) P
order by P.[Path]";
            return await Company.QueryAsync<MetricPathOptionViewModel>(sql, new { assetTypeId, scoreType = (int)scoreType }, ApiTimeout);
        }

        public (MetricScoreApiModel, string) GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var filterAsset = new Asset();

            var result = new MetricScoreApiModel();
            var parameters = new DynamicParameters();

            List<string> outerFilters = new List<string>();
            List<string> innerFilters = new List<string>();
            List<string> fieldJoins = new List<string>();
            
            var dateStart = DateTime.MinValue;
            var dateEnd = DateTime.MinValue;
            Guid allocationUid = Guid.Empty;
            MetricAllocation allocation = null;

            if (!queryParams.Any(x => x.Key.ToLower() == "_scoretype") && !queryParams.Any(x => x.Key.ToLower() == "_allocationuid"))
            {
                // Look up default Governance score allocation.
                allocation = Company.Filter<MetricAllocation>(a => a.AssetTypeUid == at.uid && a.ScoreType == ScoreType.Governance && string.IsNullOrEmpty(a.OverrideName)).FirstOrDefault();
                if (allocation == null)
                    return (null, $"Allocation for {ScoreType.Governance} score type and asset type does not exist");

                parameters.Add("@allocationUid", allocation.Uid);
                allocation = null;
            }

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
                        innerFilters.Add("IMS.EffectiveDate >= @dateStart");
                        outerFilters.Add("MS.EffectiveDate >= @dateStart");
                        break;
                    case "_effectivedateend":
                        DateTime.TryParse(param.Value, out dateEnd);
                        
                        if (dateEnd == DateTime.MinValue)
                            return (null, "Invalid '_effectiveDateEnd' parameter value");
                        
                        parameters.Add("@dateEnd", dateEnd);
                        innerFilters.Add("IMS.EffectiveDate <= @dateEnd");
                        outerFilters.Add("MS.EffectiveDate <= @dateEnd");
                        break;
                    case "_assetuid":
                        Guid assetUid = Guid.Empty;
                        if (!Guid.TryParse(param.Value, out assetUid))
                            return (null, "Invalid '_assetUid' parameter value");

                        var assetTypeId = Company.Assets.Where(x => x.uid == assetUid).FirstOrDefault()?.AssetTypeID;
                        
                        if (assetTypeId != at.ID)
                            return (null, "Asset of given asset type Uid does not exists");
                        
                        if (queryParams.Any(x => x.Key.ToLower() == "customfield"))
                            return (null, "'_assetUid' AND 'customfield' are exclusive filters and may not be combined.");

                        parameters.Add("@assetUid", assetUid);
                        outerFilters.Add("MS.AssetUid = @assetUid");
                        break;
                    case "_scoretype":
                        ScoreType scoretype;
                        if (!Enum.TryParse(param.Value, out scoretype))
                            return (null, "Invalid '_scoreType' parameter value");

                        if (queryParams.Any(x => x.Key.ToLower() == "_allocationuid"))
                            return (null, "'_allocationUid' AND '_scoreType' are exclusive filters and may not be combined.");

                        allocation = Company.Filter<MetricAllocation>(a => a.AssetTypeUid == at.uid && a.ScoreType == scoretype && string.IsNullOrEmpty(a.OverrideName)).FirstOrDefault();

                        if (allocation == null)
                            return (null, "Allocation for specified score type and asset type does not exist");

                        parameters.Add("@allocationUid", allocation.Uid);
                        allocation = null;
                        break;
                    case "_allocationuid":
                        if (!Guid.TryParse(param.Value, out allocationUid))
                            return (null, "Invalid '_allocationUid' parameter value");

                        allocation = Company.GetByUid<MetricAllocation>(allocationUid);
                        
                        if (allocation == null)
                            return (null, "Allocation does not exist");

                        if (allocation.AssetTypeUid != at.uid)
                            return (null, "Allocation is not associated with the given asset type");

                        if (queryParams.Any(x => x.Key.ToLower() == "_scoretype"))
                            return (null, "'_allocationUid' AND '_scoreType' are exclusive filters and may not be combined.");

                        parameters.Add("@allocationUid", allocationUid);
                        allocation = null;
                        break;

                    default:
                        customFieldsCounter++;
                        var fieldName = param.Key;

                        int? filterFieldTypeId = null;
                        filterFieldTypeId = Company.FieldTypes.Where(x => x.AssetTypeID == at.ID && x.Name.ToLower() == param.Key.ToLower()).FirstOrDefault()?.ID;
                        if (filterFieldTypeId == null)
                            return (null, $"Invalid custom field parameter. Field type with name '{param.Key}' does not exists");

                        if (parameters.ParameterNames.Any(x => x.ToLower() == "_assetuid"))
                            return (null, "'_assetUid' AND 'customfield' are exclusive filters and may not be combined.");

                        fieldJoins.Add($"inner join Field F{customFieldsCounter} on F{customFieldsCounter}.FieldTypeID = @ftId{customFieldsCounter} and F{customFieldsCounter}.AssetID = A.ID and F{customFieldsCounter}.FormattedValue = @ftValue{customFieldsCounter}");
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
                outerFilters.Add($"A.ID not in ({Company.GetNoReadSqlStatement()})");
            }

            string outerWhere = outerFilters.Count == 0 ? "" : " and " + string.Join(" and ", outerFilters);
            string innerWhere = innerFilters.Count == 0 ? "" : " and " + string.Join(" and ", innerFilters);
            string fieldJoinStatement = string.Join(" ", fieldJoins) + "";

            var countSql = $@"
select  count(distinct MS.AssetUid) 
from    metrics.Score MS 
        inner join Asset A on A.Uid = MS.AssetUid 
        {fieldJoinStatement} 
        {outerWhere}";

            result.total = Company.Query<int>(countSql, parameters, ApiTimeout).FirstOrDefault();

            var sql = $@"
select      MS.AssetUid,
            (
            select      {(takeOnlyLastScore ? "top 1" : "")} 
                        IMS.EffectiveDate, 
                        IMS.Value as Score, 
                        Al.ScoreType 
            from        metrics.Score IMS
                        inner join metrics.Allocation Al on Al.Uid = IMS.AllocationUid 
		    where       IMS.AllocationUid = @allocationUid and IMS.AssetUid = MS.AssetUid {innerWhere}
		    order by    IMS.EffectiveDate desc
		    for json path
            ) as Scores 
from        metrics.Score MS
            inner join Asset A on A.Uid = MS.AssetUid 
            {fieldJoinStatement}
where       MS.AllocationUid = @allocationUid {outerWhere}
group by    MS.AssetUid
order by    MS.AssetUid
offset ((@pageNum-1)*@pageSize) rows fetch next @pageSize rows only
for json path";

            var itemsJson = string.Join("", Company.Query<string>(sql, parameters, ApiTimeout).ToList());

            result.items = JsonConvert.DeserializeObject<List<MetricAssetScoreModel>>(itemsJson);
            if (result.items == null) result.items = new List<MetricAssetScoreModel>();
            
            return (result, "");
        }

        public MetricAllocation GetAllocationByMetricModel(MetricAssetViewModel model)
        {
            if (model.AllocationUid == Guid.Empty)
            {
                if (model.AssetTypeUid.HasValue)
                {
                    if (!model.ScoreType.HasValue)
                    {
                        model.ScoreType = ScoreType.Governance;
                    }
                    return Company.Filter<MetricAllocation>(a => a.AssetTypeUid == model.AssetTypeUid.Value && a.ScoreType == model.ScoreType.Value && string.IsNullOrEmpty(a.OverrideName)).FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return Company.GetByUid<MetricAllocation>(model.AllocationUid);
            }
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
        
        public DataQualityGetResultModel GetDataQualityResults(Guid owningAssetUid, Guid? evaluatedAssetUid = null, int pageSize = 250, int pageNum = 1, string sort = null, string direction = "asc", DateTime? effectiveDateStart = null, DateTime? effectiveDateEnd = null, bool includeDuplicateFlag = false)
        {
            var result = new DataQualityGetResultModel();
            var parameters = new DynamicParameters();

            string  cteSql = "",
                    columnSql = "",
                    fromSql = "",
                    whereSql = "",
                    whereCteEvaluatedBySql = "",
                    whereResultCteSql = "",
                    orderSql = "R.EffectiveDate",
                    pagingSql = "offset ((@pageNum-1)*@pageSize) rows fetch next @pageSize rows only";

            if (effectiveDateStart.HasValue)
            {
                whereResultCteSql = " and EffectiveDate >= @effectiveStartDate";
                parameters.Add("@effectiveStartDate", effectiveDateStart.Value);
            }
            if (effectiveDateEnd.HasValue)
            {
                whereResultCteSql += $@" and EffectiveDate <= @effectiveEndDate";
                parameters.Add("@effectiveEndDate", effectiveDateEnd.Value);
            }
            if (evaluatedAssetUid.HasValue)
            {
                whereCteEvaluatedBySql = " and AE.Uid = @evaluatedAssetUid";
                whereSql = (string.IsNullOrEmpty(whereSql) ? "where" : "and") + " E.EvaluatedAssetUid = @evaluatedAssetUid";
            }

            if (!string.IsNullOrWhiteSpace(sort))
            {
                switch (sort.Trim()) 
                {
                    case "EffectiveDate":
                    case "EvaluatedAssetUid":
                    case "FailCount":
                    case "OwningAssetUid":
                    case "PassCount":
                    case "Passed":
                    case "PassFraction":
                    case "ResultUid":
                    case "RunDate":
                    case "TotalCount":
                        orderSql = $"R.{sort}";
                        break;
                    case "EvaluatedAssetClass":
                        orderSql = "E.EvaluatedAssetTypeClass";
                        break;
                    case "EvaluatedAssetDisplayPath":
                        orderSql = "E.EvaluatedAssetDisplayPath";
                        break;
                    case "EvaluatedAssetPath":
                        orderSql = "E.EvaluatedAssetPath";
                        break;
                    case "EvaluatedAssetTypePath":
                        orderSql = "P.[Path]";
                        break;
                }
            }
            orderSql = $"order by {orderSql} {direction??""}";

            columnSql = @" R.ResultUid, R.OwningAssetUid, E.EvaluatedAssetUid, E.EvaluatedAssetPath, E.EvaluatedAssetDisplayPath, E.EvaluatedAssetSegments, P.[Path] as EvaluatedAssetTypePath, E.EvaluatedAssetTypeClass, R.EffectiveDate, R.RunDate, R.PassCount, R.FailCount, R.TotalCount, R.PassFraction, R.Passed";
            if (includeDuplicateFlag)
            {
                columnSql += @", case when ROW_NUMBER() over (partition by R.OwningAssetUid, coalesce(E.EvaluatedAssetUid, newid()), R.EffectiveDate order by R.RunDate desc) = 1 then cast(0 as bit) else cast(1 as bit) end as IsDuplicate";
            }

            cteSql = $@"with 
	E as	(
			select	R.Uid as ResultUid,
					AE.Uid as EvaluatedAssetUid,
					EDP.DisplayPath as EvaluatedAssetDisplayPath,
                    EKP.KeyPath as EvaluatedAssetPath,
					EDP.Segments as EvaluatedAssetSegments,
					AE.AssetTypeID,
					EDP.Class as EvaluatedAssetTypeClass
			from	AssetResult R,
					AssetResultEdge EO,
					graph.AssetNode AO,
					AssetResultEdge EE,
					graph.AssetNode AE,
					[graph].[AssetNodeDisplayPath] EDP,
                    [graph].[AssetNodeKeyPath] EKP
			where	match(AO-(EO)->R)
					and EO.Class = 1 -- Owns
					and match(AE-(EE)->R)
					and EE.Class = 2 -- Evals
					and EDP.ID = AE.ID
					and EKP.ID = AE.ID
					and	AO.Uid = @owningAssetUid {whereCteEvaluatedBySql}
			),
	R as	(
			select	R.Uid as ResultUid,
					A.Uid as OwningAssetUid,
					R.EffectiveDate,
					R.RunDate,
					R.PassCount,
					R.FailCount,
					R.TotalCount,
					RU.Threshold,
					R.PassFraction,
					case 
						when R.PassCount = 0 and R.FailCount = 0 then null 
						when RU.Threshold <= R.PassFraction then cast(1 as bit) --R.Passed 
						else cast(0 as bit)
					end as Passed
			from	AssetResult R,
					AssetResultEdge O,
					graph.AssetNode A,
					Asset AA,
					[Rule] RU
			where	match(A-(O)->R)
					and AA.ID = A.ID
					and RU.ID = AA.ObjectID
					and O.Class = 1 --Owns
					and	A.Uid = @owningAssetUid {whereResultCteSql}
			)";

            fromSql = $" from R left join E on R.ResultUid = E.ResultUid outer apply dbo.GetAssetTypeTextPathById(E.AssetTypeID, ' > ') P";

            result.pageNum = pageNum;
            result.pageSize = pageSize;

            parameters.Add("@evaluatedAssetUid", evaluatedAssetUid);
            parameters.Add("@owningAssetUid", owningAssetUid);
            parameters.Add("@pageNum", result.pageNum);
            parameters.Add("@pageSize", result.pageSize);

            result.total = Company.Query<int>($"{cteSql} select count(1) {fromSql} {whereSql}", parameters, ApiTimeout).FirstOrDefault();
            result.items = Company.Query<DataQualityGetResultItem>($"{cteSql} select {columnSql} {fromSql} {whereSql} {orderSql} {pagingSql}", parameters, ApiTimeout).ToList();
            
            if (result.items == null)
            {
                result.items = new List<DataQualityGetResultItem>();
            }

            return result;
        }

        public List<DataQualityAssetResultModel> GetAssetResultDetailsByUid(Guid value)
        {
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

            return Company.Query<DataQualityAssetResultModel>(assetResultSQL, parameters, ApiTimeout).ToList();
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

        public async Task<ApiExecutionInfo> PostBulkDataQualityResults(List<DataQualityInsertModel> request, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = Company.CurrentCompanyID,
                CompanyDomainPrefix = Company.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.PostDataQualityResults,
                SendWorkflowEvents = sendWorkflowEvents
            };

            // Save to storage container.
            StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(request));
                        

            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;

            Company.Add(execution);

            // Save to queue.
            if (!await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo))
            {
                throw new Exception(AZURE_QUEUE_INSERTION_FAILURE_MESSAGE);
            }

            return executionInfo;
        }

        private DateTime? GetMetricsLastUsedEffectiveDate(Guid uid)
        {
            return Company.Query<DateTime?>(@"
select  max(S.EffectiveDate) as EffectiveDate 
from    metrics.ScoreItem I 
        inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @metricVersionUid 
        inner join metrics.Score S on S.Uid = L.ScoreUid", new { metricVersionUid = uid }, ApiTimeout).FirstOrDefault();
        }

        public List<string> GetMetricVersionHistory(Guid measureUid)
        {
            return Company.Query<string>($@"                    
                    select ROW_NUMBER() over (Order by V.EffectiveDate asc, ISNULL(V.EffectiveEndDate, GETDATE()) asc) as version, 
                            A.Uid as MeasureUid,
                    		V.Name,
                    		V.Description,
                    		V.EffectiveDate,
							V.EffectiveEndDate,
							V.Weight,
							V.Uid as versionuid,
							(
                    			select		C.Uid,
											C.Position,
											C.Threshold,
											C.Weight,
											C.MatchType,
											(
												select	CI.Uid,
														CI.ConditionType,
														CI.ConditionFieldTypeID,
														CI.ConditionIntersectTypeID,
														CI.Operator,
														(
															select	[Value]
															from	metrics.AssetVersionConditionItemValue
															where	Uid = CI.Uid
															for json path
														) as [Values]
												from	metrics.AssetVersionConditionItem CI
												where	CI.AssetVersionConditionUid = C.Uid
												for json path
											) as ConditionItems
                    			from		metrics.AssetVersionCondition C
                    			where		C.AssetVersionUid = V.Uid
								order by	C.Position
                    			for		json path
                    		) as ConditionGroups
                    from	metrics.Asset A                    		
                            cross apply (
                    			select	EffectiveDate as EffectiveDate
                    			from	metrics.AssetVersion
                    			where	AssetUid = A.Uid
                    		) MV
                    		inner join metrics.AssetVersion V on V.AssetUid = A.Uid and V.EffectiveDate = MV.EffectiveDate
					where A.Uid = @measureUid
					Order by version
                    for		json path", new { measureUid }, ApiTimeout).ToList();
        }
    }
}