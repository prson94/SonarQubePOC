using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json;
using d360.core;
using d360.core.queue;
using d360.extensions;
using System.Net;
using System.Text.RegularExpressions;
using d360.core.helpers;
using d360.core.resources;

namespace d360.model.DataAccessLayer
{
    public class AssetRepository : IAssetRepository
    {
        internal ICompanyContext CompanyContext;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;
        internal ICommunityContext Community;
        public AssetRepository(ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider, ICommunityContext community)
        {
            this.CompanyContext = companyContext;
            this.QueueSource = queueSource;
            this.StorageProvider = storageProvider;
            this.Community = community;
        }
        public Asset GetAssetByUID(Guid assetUid)
        {
            return CompanyContext.Filter<Asset>(i => i.uid == assetUid, i => i.AssetType).SingleOrDefault();
        }
        public List<AssetTypeClassInfo> GetAssetTypeList()
        {
            return AssetTypeClass.BusinessAsset.GetAsList();
        }
        public async Task<IEnumerable<AssetTypeApiViewModel>> GetAssetType(AssetTypeClass? Class, Guid? fusionTypeUid)
        {
            var dbArgs = new DynamicParameters();
            string condition = string.Empty;
            string optionalJoin = string.Empty;
            if (Class.HasValue)
            {
                if (fusionTypeUid.HasValue && fusionTypeUid.Value != Guid.Empty && (Class == AssetTypeClass.FusionAttribute || Class == AssetTypeClass.FusionQuery))
                {
                    if(Class == AssetTypeClass.FusionAttribute)
                    {
                        optionalJoin = @"inner join FusionAttributeType FAT on A.[Object] = 'FusionAttributeType' and A.Objectid = FAT.ID 
                                          inner join AssetType ATTFusionType on ATTFusionType.[Object] = 'FusionType' and ATTFusionType.ObjectID = FAT.FusionTypeID";
                        dbArgs.Add("@fusionTypeUid", fusionTypeUid);
                        condition += " and ATTFusionType.uid = @fusionTypeUid";
                    }

                    if(Class == AssetTypeClass.FusionQuery)
                    {
                        optionalJoin = @"inner join FusionQueryAttributeType FQAT ON A.Object = 'FusionQueryAttributeType' AND FQAT.ID = a.ObjectID
                                         inner join Fusion F on F.ID = FQAT.FusionID
                                         inner join AssetType ATQFusionType on ATQFusionType.[Object] = 'FusionType' and ATQFusionType.ObjectID = F.FusionTypeID";

                        dbArgs.Add("@fusionTypeUid", fusionTypeUid);
                        condition += " and ATQFusionType.uid = @fusionTypeUid";
                    }
                }
                else
                {
                    var Id = (int)Class;
                    dbArgs.Add("@Id", Id.ToString());
                    condition = "and A.[Class]=@Id";
                }
            }
            else if (fusionTypeUid.HasValue && fusionTypeUid.Value != Guid.Empty)
            {


                optionalJoin += @"left join FusionAttributeType FAT on A.[Object] = 'FusionAttributeType' and A.Objectid = FAT.ID 
                                  left join AssetType ATTFusionType on ATTFusionType.[Object] = 'FusionType' and ATTFusionType.ObjectID = FAT.FusionTypeID 
                                  left join FusionQueryAttributeType FQAT ON A.Object = 'FusionQueryAttributeType' AND FQAT.ID = a.ObjectID
                                  left join Fusion F on F.ID = FQAT.FusionID
                                  left join AssetType ATQFusionType on ATQFusionType.[Object] = 'FusionType' and ATQFusionType.ObjectID = F.FusionTypeID ";

                dbArgs.Add("@class1", (int)AssetTypeClass.FusionAttribute);
                dbArgs.Add("@class2", (int)AssetTypeClass.FusionQuery);
                dbArgs.Add("@fusionTypeUid", fusionTypeUid);

                condition = string.Format("and (A.[Class] = @class1 OR A.[Class] = @class2) AND (ATQFusionType.uid = @fusionTypeUid or ATTFusionType.uid = @fusionTypeUid)");

            }

            var sql = $@"
                        SELECT      A.[Name]
                                    ,A.[Description]
                                    ,A.[Class] as ClassID
                                    ,A.[Notes]
                                    ,A.[uid],
                                    P.[Path]
                        FROM        AssetType A
                                    {optionalJoin}
                                    cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
                        where       A.[State] = 1
                        {condition}
                        order by    P.[Path]
                        ";
            var assetTypes = await CompanyContext.QueryAsync<AssetTypeApiViewModel>(sql, dbArgs);
            return assetTypes;
        }
        public async Task<AssetsApiViewModel> GetAssets(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var assetTypeID = 0;
            var includeRelationships = false;
            var fusionAttributeWithParent = false;

            var assetType = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == uid);
            if (assetType == null)
                throw new Exception("not found");

            assetTypeID = assetType.ID;

            var fieldTypes = CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetTypeID).ToList();

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_predicateuid"))
                includeRelationships = true;

            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();
            List<string> whereStatements = new List<string>();
            List<string> pagingSql = new List<string>();

            var dbArgs = new DynamicParameters();
            var model = new AssetsApiViewModel();

            dbArgs.Add("@uid", uid.ToString());
            fieldJoins.Add("inner join AssetType T on T.ID = A.AssetTypeID and T.UID = @uid");

            List<string> countJoins = new List<string>(fieldJoins);

            if (includeRelationships)
            {
                var subjectAlias = "B";
                var objectAlias = "A";
                string relatedAssetUIDString = "";
                Guid relatedAssetUID;

                var predicateUID = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_predicateuid").Value;
                var intersectJoin = "";
                var reverseIntersectJoin = "";
                var relatedAssetSql = "";
                bool includeBoth = false;


                if (queryParams.ToList().Any(q => q.Key.ToLower() == "_objectuid"))
                {
                    relatedAssetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_objectuid").Value;
                    if (Guid.TryParse(relatedAssetUIDString, out relatedAssetUID))
                    {
                        dbArgs.Add("@relatedAssetUid", relatedAssetUID);
                        relatedAssetSql = $"where {subjectAlias}.[UID] = @relatedAssetUid";
                    }
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and abs(I.SubjectID) = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and I.ObjectID = {subjectAlias}.ObjectID";

                }
                else if (queryParams.ToList().Any(q => q.Key.ToLower() == "_subjectuid"))
                {
                    relatedAssetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_subjectuid").Value;
                    if (Guid.TryParse(relatedAssetUIDString, out relatedAssetUID))
                    {
                        dbArgs.Add("@relatedAssetUid", relatedAssetUID);
                        relatedAssetSql = $"where {subjectAlias}.[UID] = @relatedAssetUid";
                    }
                    intersectJoin = $"I.[Subject] = {subjectAlias}.[Object] and abs(I.SubjectID) = {subjectAlias}.ObjectID and I.[Object] = {objectAlias}.[Object] and I.ObjectID = {objectAlias}.ObjectID";
                }
                else
                {
                    //subject and object not specified
                    includeBoth = true;
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and abs(I.SubjectID) = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and I.ObjectID = {subjectAlias}.ObjectID";
                    reverseIntersectJoin = $"I.[Subject] = {subjectAlias}.[Object] and abs(I.SubjectID) = {subjectAlias}.ObjectID and I.[Object] = {objectAlias}.[Object] and I.ObjectID = {objectAlias}.ObjectID";
                }

                var innerSql = $@"
                            select 
                                B.[UID] as AssetUid, 
                                BD.DisplayValue,
                                TB.[Name] as TypeName,
                                P.[UID] as PredicateUid
                            from Asset B
                            inner join AssetType TB on TB.ID = B.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(B.ID) BD
                            inner join [Intersect] I on {intersectJoin}
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid
                            {relatedAssetSql}";

                if (includeBoth)
                {
                    var reverseInnerSql = $@"
                            select 
                                B.[UID] as AssetUid, 
                                BD.DisplayValue,
                                TB.[Name] as TypeName,
                                P.[UID] as PredicateUid
                            from Asset B
                            inner join AssetType TB on TB.ID = B.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(B.ID) BD
                            inner join [Intersect] I on {reverseIntersectJoin}
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid";

                    innerSql = $@"select * from (
                        {innerSql}
                        union all
                        {reverseInnerSql}) RI";
                }

                var joinSql = $@"
                    cross apply (
                        select (
                            {innerSql}
                            for json path
                        ) as Relationships
                    ) R";


                fieldColumns.Add("R.Relationships");
                dbArgs.Add("@predicateUid", predicateUID);

                fieldJoins.Add(joinSql);
            }

            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

            if (includeRelationships)
                whereStatements.Add("R.Relationships is not null");

            if (!CompanyContext.CurrentResourceIsAdmin)
            {
                whereStatements.Add($"A.ID not in ({CompanyContext.GetNoReadSqlStatement()})");
                whereStatements.Add($"A.AssetTypeID not in ({CompanyContext.GetAssetTypeNoReadSqlStatement()})");
            }

            getQueryParamsSql(model, assetType, fieldTypes, dbArgs, whereStatements, pagingSql, queryParams);

            if(assetType.Class == AssetTypeClass.FusionAttribute)
            {
                if ((await CompanyContext.Database.Connection.QueryFirstOrDefaultAsync<int>("select ISNULL(parentId,0) from fusionattributetype where id = @id", new { id = assetType.ObjectID })) > 0)
                    fusionAttributeWithParent = true;                
            }

            var whereSql = "";
            if (whereStatements.Any())
                whereSql = $"where {string.Join(" and ", whereStatements)}";

            var fieldsSql = "";
            if (fieldColumns.Any())
                fieldsSql = $",\n {string.Join(",\n", fieldColumns)}";


            var countSql = $@"
                select
                    count(*)
                from Asset A
                {(assetType.Object == "FusionAttributeType" ? " inner join FusionAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {(fusionAttributeWithParent ? " inner join Asset ATP on ATP.ObjectID = FA.ParentID and ATP.[Object] = 'FusionAttribute'" : "")}
                {(assetType.Object == "FusionQueryAttributeType" ? " inner join FusionQueryAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {string.Join("\n", string.IsNullOrWhiteSpace(whereSql) ? countJoins : fieldJoins)}
                {whereSql}";

            var sql = $@"
                select
                    A.ID as AssetId,
                    A.[UID] as [AssetUid],
                    A.AssetTypeId,
                    T.[UID] as AssetTypeUid,
                    A.UpdatedOn,
                    A.CreatedOn,
                    A.Code
                    {(assetType.Object == "FusionAttributeType" ? " , FA.SourceID, FA.Name, FA.TextPath" : "")} 
                    {(fusionAttributeWithParent ? " , ATP.uid as ParentUid": "")}
                    {fieldsSql}
                from Asset A
                {(assetType.Object == "FusionAttributeType" ? " inner join FusionAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {(fusionAttributeWithParent ? " inner join Asset ATP on ATP.ObjectID = FA.ParentID and ATP.[Object] = 'FusionAttribute'" : "")}
                {(assetType.Object == "FusionQueryAttributeType" ? " inner join FusionQueryAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {string.Join("\n", fieldJoins)}
                {whereSql}
                {string.Join("\n", pagingSql)}
            ";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs);
            var count = countResults.First();

            var results = await CompanyContext.QueryAsync<dynamic>(sql, dbArgs);

            if (includeRelationships)
            {
                foreach (var result in results)
                {
                    result.Relationships = JsonConvert.DeserializeObject(result.Relationships);
                }
            }

            model.items = results;
            model.total = count;

            return model;
        }
        public dynamic GetFieldTypes(Guid assetTypeUid)
        {
            var assetTypeID = 0;
            assetTypeID = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == assetTypeUid)?.ID ?? 0;
            //Use same output format as FieldsController._FieldTypesByObject to preserve compatability
            return CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetTypeID).Select(i => new
            {
                i.FriendlyName,
                i.Category,
                i.DisplayDescription,
                i.FormDescription,
                i.ID,
                i.IsListable,
                i.IsRequired,
                i.ColumnOrder,
                i.SortOrder,
                ObjectType = i.Object,
                i.ObjectID,
                i.Type
            }).ToList();
        }
        public List<DatabaseBulkAssetResult> PostAssets(List<AssetInsert> assets, AssetType assetType, ApiExecution execution, bool fieldJsonPropertyLoadLimitToTopLevel = true, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.ImportAssets(execution, assetType, assets, true, fieldJsonPropertyLoadLimitToTopLevel: fieldJsonPropertyLoadLimitToTopLevel, sendWorkflowEvents: sendWorkflowEvents, lookupFieldsPassedByValue: lookupFieldsPassedByValue);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }

            return results;
        }
        public Tuple<HttpStatusCode, string, string> AddAssetType(AssetTypeInsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate, int resourceId, out string nameFriendlyName, out bool isNamePartOfKey)
        {
            var parentType = SystemObjects.ArtifactType;
            nameFriendlyName = "Name";
            isNamePartOfKey = true;

            if (!string.IsNullOrEmpty(model?.Name ?? null))
                model.Name = model.Name.Trim();

            switch (model.Class)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.TechnicalAsset:
                    #region
                    var a = new AssetType
                    {
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description,
                        Object = SystemObjects.ArtifactType.ToString(),
                        State = State.Active,
                        UpdatedBy = resourceId,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = resourceId,
                        CreatedOn = DateTime.UtcNow,
                        Hierarchical = true,
                        Class = model.Class,
                        AutoDisplayDescription = model.AutoDisplayDescription,
                        UseAsTransformation = model.UseAsTransformation,
                        CanOwnFusion = model.CanOwnFusion ?? false
                    };
                    CompanyContext.Add(a);
                    parentType = SystemObjects.ArtifactType;
                    model.ObjectID = a.ObjectID;
                    model.Object = SystemObjects.ArtifactType.ToString();

                    #endregion
                    break;
                case AssetTypeClass.Organization:
                    #region
                    var org = new OrganizationType
                    {
                        Name = model.Name,
                        Description = model.Description,
                        DisplayFormat = model.DisplayFormat
                    };
                    var existing = CompanyContext.Filter<OrganizationType>(o => o.Name == org.Name && o.State == State.Active).FirstOrDefault();
                    if (existing != null)
                        return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, "Wrong Name", AssetTypeErrors.ExistingOrganizationType);
                    CompanyContext.Add(org);
                    parentType = SystemObjects.OrganizationType;
                    model.ObjectID = org.ID;
                    model.Object = SystemObjects.OrganizationType.ToString();
                    #endregion
                    break;
                case AssetTypeClass.Policy:
                    #region                    
                    var p = new AssetType
                    {
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description,
                        HierarchyMaximumDepth = model.Hierarchy.MaximumDepth,
                        Object = SystemObjects.PolicyType.ToString(),
                        State = State.Active,
                        UpdatedBy = resourceId,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = resourceId,
                        CreatedOn = DateTime.UtcNow,
                        Hierarchical = true,
                        UseAsTransformation = model.UseAsTransformation,
                        Class = AssetTypeClass.Policy
                    };
                    CompanyContext.Add(p);
                    parentType = SystemObjects.PolicyType;
                    model.ObjectID = p.ObjectID;
                    model.Object = SystemObjects.PolicyType.ToString();
                    #endregion
                    break;
                case AssetTypeClass.Model:
                    #region
                    var t = new AssetType
                    {
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description,
                        HierarchyMaximumDepth = model.Hierarchy.MaximumDepth,
                        Object = SystemObjects.TaxonomyType.ToString(),
                        State = State.Active,
                        UpdatedBy = resourceId,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = resourceId,
                        CreatedOn = DateTime.UtcNow,
                        Hierarchical = true,
                        UseAsTransformation = model.UseAsTransformation,
                        Class = AssetTypeClass.Model
                    };

                    if (t.HierarchyMaximumDepth <= 0 || t.HierarchyMaximumDepth > 10)
                        return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, "Invalid Maximum Depth", AssetTypeErrors.InvalidModelDepth);


                    CompanyContext.Add(t);

                    for (int i = 1; i <= t.HierarchyMaximumDepth; i++)
                    {
                        CompanyContext.Set<AssetTypeLevel>().Add(new AssetTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), AssetTypeID = t.ID });
                    }
                    CompanyContext.SaveChanges();

                    parentType = SystemObjects.TaxonomyType;
                    model.ObjectID = t.ObjectID;
                    model.Object = SystemObjects.TaxonomyType.ToString();
                    #endregion
                    break;
                case AssetTypeClass.Reference:
                    #region
                    var rt = new AssetType
                    {
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description,
                        Notes = model.Notes,
                        Object = SystemObjects.ReferenceItemType.ToString(),
                        State = State.Active,
                        UpdatedBy = resourceId,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = resourceId,
                        CreatedOn = DateTime.UtcNow,
                        UseAsTransformation = model.UseAsTransformation,
                        Class = AssetTypeClass.Reference
                    };
                    isNamePartOfKey = false;
                    nameFriendlyName = "Long Description";
                    CompanyContext.Add(rt);
                    parentType = SystemObjects.ReferenceItemType;
                    model.ObjectID = rt.ObjectID;
                    model.Object = SystemObjects.ReferenceItemType.ToString();
                    #endregion
                    break;
                case AssetTypeClass.Rule:
                    #region
                    var r = new RuleType
                    {
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description
                    };
                    CompanyContext.Add(r);
                    parentType = SystemObjects.Rule;
                    model.ObjectID = r.ID;
                    model.Object = SystemObjects.RuleType.ToString();
                    #endregion
                    break;
            }

            if (predicate != null)
            {
                var intersectType = new IntersectType
                {
                    Subject = parentType.ToString(),
                    SubjectID = (parentAssetType != null) ? parentAssetType.ObjectID : model.ObjectID,
                    SubjectCardinality = Cardinality.One,
                    Object = model.Object,
                    ObjectID = model.ObjectID,
                    ObjectCardinality = Cardinality.Many,
                    PredicateID = predicate.ID
                };
                CompanyContext.Add(intersectType);
            }

            return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", "");
        }
        public List<DatabaseBulkAssetResult> PutAssets(List<AssetUpdate> assets, AssetType assetType, ApiExecution execution, bool fieldJsonPropertyLoadLimitToTopLevel = true, bool sendWorkflowEvents = true,bool lookupFieldsPassedByValue = false)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.ImportAssets(execution, assetType, assets, false, fieldJsonPropertyLoadLimitToTopLevel: fieldJsonPropertyLoadLimitToTopLevel, sendWorkflowEvents:sendWorkflowEvents, lookupFieldsPassedByValue:lookupFieldsPassedByValue);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }

            return results;
        }
        public Tuple<HttpStatusCode, string, string> UpdateAssetType(AssetTypeInsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate)
        {
            List<AssetTypeClass> predicateClass = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference };

            bool shouldRemoveOldRelationshipType = false;
            bool shouldRemoveExistingParentChildRelationshipType = false;

            if (!string.IsNullOrEmpty(model?.Name ?? null))
                model.Name = model.Name.Trim();

            switch (model.Class)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.Policy:
                case AssetTypeClass.Reference:
                case AssetTypeClass.Model:
                case AssetTypeClass.TechnicalAsset:
                    if (assetType == null) return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, $"Wrong {model.Class.ToString()}", $"Invalid {model.Class.ToString()} provided. {AssetTypeErrors.CheckRequest}");

                    assetType.Name = model.Name;
                    assetType.DisplayFormat = model.DisplayFormat;
                    assetType.Description = model.Description;
                    assetType.HierarchyMaximumDepth = (model.Hierarchy != null) ? model.Hierarchy.MaximumDepth : 1;
                    assetType.AutoDisplayDescription = model.AutoDisplayDescription;
                    if (model.Class == AssetTypeClass.BusinessAsset || model.Class == AssetTypeClass.TechnicalAsset)
                    {
                        assetType.UseAsTransformation = model.UseAsTransformation;
                        assetType.CanOwnFusion = model.CanOwnFusion ?? false;
                    }
                    else
                    {
                        assetType.UseAsTransformation = false;
                        assetType.CanOwnFusion = false;
                    }
                    assetType.Class = model.Class;
                    assetType.Notes = model.Notes;

                    if (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy)
                    {
                        if (assetType.HierarchyMaximumDepth <= 0 || assetType.HierarchyMaximumDepth > 10)
                            return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, "Invalid Maximum Depth", AssetTypeErrors.InvalidModelDepth);

                        for (int i = 1; i <= assetType.HierarchyMaximumDepth; i++)
                        {
                            var level = assetType.AssetTypeLevels.SingleOrDefault(l => l.Level == i);
                            if (level == null)
                            {
                                CompanyContext.Set<AssetTypeLevel>().Add(new AssetTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), AssetTypeID = assetType.ID });
                            }
                        }
                        CompanyContext.Delete<AssetTypeLevel>(l => l.Level > assetType.HierarchyMaximumDepth);
                    }

                    CompanyContext.Update(assetType);

                    if (model.Class == AssetTypeClass.Reference)
                    {
                        shouldRemoveOldRelationshipType = true;
                        shouldRemoveExistingParentChildRelationshipType = true;
                    }

                    break;
                case AssetTypeClass.Organization:
                    var org = CompanyContext.GetById<OrganizationType>(model.ObjectID);
                    if (org == null) return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Organization.ToString()}", $"Invalid {AssetTypeClass.Organization.ToString()} provided. {AssetTypeErrors.CheckRequest}");
                    org.Name = model.Name;
                    org.Description = model.Description;
                    org.DisplayFormat = model.DisplayFormat;
                    CompanyContext.Update(org);

                    break;
                case AssetTypeClass.Rule:
                    #region
                    var r = CompanyContext.GetById<RuleType>(model.ObjectID);
                    if (r == null) return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Rule.ToString()}", $"Not valid {AssetTypeClass.Rule.ToString()} provided. {AssetTypeErrors.CheckRequest}");
                    r.Name = model.Name;
                    r.DisplayFormat = model.DisplayFormat;
                    r.Description = model.Description;
                    CompanyContext.Update(r);
                    #endregion
                    break;
            }

            var parentType = SystemObjectHelper.GetSystemObjects(model.Class).ToString();

            if (predicateClass.Contains(model.Class) && (parentAssetType != null || predicate != null))
            {
                var parentPredicateType = PredicateType.InterTypeHierarchy;

                if (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy)
                {
                    parentPredicateType = PredicateType.IntraTypeHierarchy;
                }

                IntersectType intersectType = null;

                if (shouldRemoveExistingParentChildRelationshipType)
                {
                    intersectType = CompanyContext.Filter<IntersectType>(i =>
                        i.Subject == parentType &&
                        i.Object == model.Object &&
                        i.ObjectID == model.ObjectID &&
                        i.Predicate.Type == parentPredicateType
                    ).SingleOrDefault();
                }
                else
                {
                    int subjectId = parentAssetType != null ? parentAssetType.ObjectID : model.ObjectID;
                    intersectType = CompanyContext.Filter<IntersectType>(i =>
                        i.Subject == parentType &&
                        i.SubjectID == subjectId &&
                        i.Object == model.Object &&
                        i.ObjectID == model.ObjectID &&
                        i.Predicate.Type == parentPredicateType
                    ).SingleOrDefault();
                }

                if (predicate != null)
                {
                    if (intersectType != null)
                    {
                        if (intersectType.PredicateID != predicate.ID)
                        {
                            intersectType.PredicateID = predicate.ID;
                            CompanyContext.Update(intersectType);
                        }

                        var parentID = (parentAssetType != null ? parentAssetType.ObjectID : model.ObjectID);

                        if (intersectType.SubjectID != parentID)
                        {
                            intersectType.SubjectID = parentID;
                            CompanyContext.Update(intersectType);
                        }
                    }
                    else
                    {
                        intersectType = new IntersectType
                        {
                            IsSystem = true,
                            Subject = parentType,
                            SubjectID = parentAssetType != null ? parentAssetType.ObjectID : model.ObjectID,
                            Object = model.Object,
                            ObjectID = model.ObjectID,
                            PredicateID = predicate.ID
                        };
                        CompanyContext.Add(intersectType);
                    }
                }
            }
            else if (shouldRemoveOldRelationshipType)
            {
                var parentPredicateType = PredicateType.InterTypeHierarchy;

                var intersectType = CompanyContext.Filter<IntersectType>(i =>
                    i.Object == model.Object &&
                    i.ObjectID == model.ObjectID &&
                    i.Predicate.Type == parentPredicateType
                ).FirstOrDefault();

                if (intersectType != null)
                {
                    CompanyContext.Delete(SystemObjects.IntersectType, intersectType.ID);
                }
            }

            return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", "");
        }

        public List<DatabaseBulkAssetResult> DeleteAsset(AssetDeletes assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.RemoveAssets(execution, assetType, assets, sendWorkflowEvents:sendWorkflowEvents);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }

            return results;
        }

        public async Task<ApiExecutionInfo> DeleteBulkAssetTypes(AssetTypeDeletes assetTypes, ApiExecution execution)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = CompanyContext.CurrentResourceID,
                Action = ApiExecutionAction.DeleteAssetTypes
            };

            // Save to storage container.
            StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assetTypes));

            // Save to queue.
            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;
            CompanyContext.Add(execution);
            return executionInfo;
        }

        public async Task<ApiExecutionInfo> BulkDeleteAssets(Guid assetTypeUid, AssetDeletes assets, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = CompanyContext.CurrentResourceID,
                Action = ApiExecutionAction.DeleteAssets,
                SendWorkflowEvents = sendWorkflowEvents
            };

            // Save to storage container.
            StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

            // Save to queue.
            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;
            CompanyContext.Add(execution);
            return executionInfo;
        }

        public async Task<ApiExecutionInfo> PutBulkAssets(Guid assetTypeUid, List<AssetUpdate> assets, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = CompanyContext.CurrentResourceID,
                Action = ApiExecutionAction.PutAssets,
                SendWorkflowEvents = sendWorkflowEvents
            };

            // Save to storage container.
            //Storage.CreateFolder(executionInfo.StorageFolder);
            StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

            // Save to queue.
            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);


            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;
            CompanyContext.Add(execution);
            return executionInfo;
        }

        public async Task<ApiExecutionInfo> PostBulkAssets(List<AssetInsert> assets, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = CompanyContext.CurrentResourceID,
                Action = ApiExecutionAction.PostAssets,
                SendWorkflowEvents = sendWorkflowEvents
            };

            // Save to storage container.
            StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

            // Save to queue.
            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;

            CompanyContext.Add(execution);
            return executionInfo;
        }

        public Predicate GetPredicateByUID(Guid predicateGuid)
        {
            return CompanyContext.Filter<Predicate>(x => x.UID == predicateGuid).SingleOrDefault();
        }

        public AssetType GetAssetTypeByUID(Guid assetTypeUid)
        {
            return CompanyContext.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();
        }

        public AssetType GetAssetTypeByModel(AssetTypeInsert model)
        {
            return CompanyContext.Filter<AssetType>(x => x.ObjectID == model.ObjectID && x.Object == model.Object).SingleOrDefault();
        }

        public ApiExecution GetExecutionItemByUid(Guid executionUid)
        {
            return CompanyContext.Filter<ApiExecution>(i => i.ExecutionID == executionUid).SingleOrDefault();
        }

        public void UpsertObjectStyle(string type, int id, string foreColor, string backColor, string objectName = "Tx")
        {
            var style = CompanyContext.GetObjectStyle(type, id);
            bool add = (style == null);

            string iconText = "Tx";

            var words = objectName.Trim().Split(' ');
            if (words.Length > 1 && words[1].Length > 0)
            {
                iconText = words[0][0].ToString().ToUpper() + words[1][0].ToString().ToLower();
            }
            else
            {
                iconText = objectName[0].ToString().ToUpper() + objectName[1].ToString().ToLower();
            }

            if (add)
            {
                style = new ObjectStyle
                {
                    ObjectType = type,
                    ObjectID = id,
                    IconBackColor = backColor,
                    IconForeColor = foreColor,
                    IconText = iconText
                };
                CompanyContext.Add<ObjectStyle>(style);
            }
            else
            {
                style.IconBackColor = backColor;
                style.IconForeColor = foreColor;
                style.IconText = iconText;
                CompanyContext.Update<ObjectStyle>(style);
            }


        }

        public bool DoesAssetExists(Guid uid)
        {
            return CompanyContext.Any<Asset>(i => i.uid == uid);
        }

        public bool IsReachedTransformationLimit(AssetTypeInsert model)
        {
            bool reached = false;
            if (model.Class == AssetTypeClass.BusinessAsset && model.UseAsTransformation == true)
            {
                var useAsTransformationLimit = Community.GetCompanySettingByKey<int>("UseAsTransformationLimit");
                var totalUseAsTransform = CompanyContext.Filter<AssetType>(i => i.UseAsTransformation == true).Count();
                if (totalUseAsTransform > useAsTransformationLimit)
                    reached = true;
            }
            return reached;
        }
        
        #region Private
        private void getFieldSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> fieldJoins, List<string> fieldColumns)
        {
            fieldTypes.ForEach(f =>
            {
                var defaultVal = f.DefaultFormattedValue;
                var joinPrefix = "left";
                var tableAlias = $"F{f.ID}";
                var columnName = f.Name;
                var valueColumn = "FormattedValue";
                var fieldDataType = getFieldDataType(f);

                FieldTypeDefinition_JsonElement jsonElementDefinition = null;

                if (f.Type == "JsonElement")
                {
                    jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(f.Definition);
                }

                if (f.Type == "Link")
                    valueColumn = "Value";

                if (f.Type == "FieldFromRelationship")
                {
                    if (!f.LookupObjectFieldTypeID.HasValue || !f.LookupObjectID.HasValue)
                        return;

                    var relatedField = CompanyContext.GetById<FieldType>((int)f.LookupObjectFieldTypeID);
                    if (relatedField == null)
                        return;

                }

                if (f.IsRequired && string.IsNullOrEmpty(f.DefaultValue))
                {
                    joinPrefix = "left";
                    if (!string.IsNullOrEmpty(fieldDataType))
                    {
                        if (fieldDataType == "bit")
                            fieldColumns.Add($"try_cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                        else
                            fieldColumns.Add($"try_cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                    }
                    else
                        fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
                }
                else
                {
                    if (!string.IsNullOrEmpty(f.DefaultValue))
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                        {
                            if (fieldDataType == "bit")
                                fieldColumns.Add($"coalesce(try_cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                            else
                                fieldColumns.Add($"coalesce(try_cast({tableAlias}.{valueColumn} as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                        }
                        else
                            fieldColumns.Add($"coalesce({tableAlias}.{valueColumn}, @defaultValue{tableAlias}) as [{columnName}]");

                        dbArgs.Add($"@defaultValue{tableAlias}", defaultVal);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                        {
                            if (fieldDataType == "bit")
                                fieldColumns.Add($"try_cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                            else
                                fieldColumns.Add($"try_cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                        }
                        else if (f.Type == "JsonElement")
                        {
                            if (jsonElementDefinition.DataType == "decimal")
                            {
                                jsonElementDefinition.DataType = "float";
                            }
                            fieldColumns.Add($"try_cast(FJP{f.ID}.[Value] as {jsonElementDefinition.DataType}) as [{columnName}]");
                        }
                        else
                        {
                            fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
                        }
                    }
                }

                if (f.Type == "FieldFromRelationship")
                {
                    fieldJoins.Add($@"outer apply (
                        select top 1 
                            F.[Value], 
                            F.FormattedValue 
                        from [Intersect] I
                        inner join Asset R on R.[Object] = I.[Object] and R.ObjectID = I.ObjectID
                        inner join Field F on F.FieldTypeID = {f.LookupObjectFieldTypeID} and F.AssetID = R.ID
                        where I.[Subject] = A.Object and I.SubjectID = A.ObjectID and I.IntersectTypeID = {f.LookupObjectID}
                    ) {tableAlias}");
                }
                else if (f.Type == "JsonElement")
                {
                    fieldJoins.Add($@"
                        {joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {jsonElementDefinition.FieldTypeID} and {tableAlias}.[ObjectType] = A.[Object] and {tableAlias}.[ObjectID] = A.[ObjectID]
                        {joinPrefix} join FieldJsonProperty FJP{f.ID} on FJP{f.ID}.FieldID = {tableAlias}.ID and FJP{f.ID}.[Path] = @jsonPath{f.ID}
                    ");
                    dbArgs.Add($"@jsonPath{f.ID}", jsonElementDefinition.Path);
                }
                else
                {
                    fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = A.[Object] and {tableAlias}.[ObjectID] = A.[ObjectID]");
                }
            });
        }
        private void getQueryParamsSql(AssetsApiViewModel model, AssetType assetType, List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> whereStatements, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (queryParams != null)
            {

                var orderBySql = "";
                var offsetSql = "";
                var pageNum = -1;
                var pageSize = -1;

                //add base sort if none is specified
                if (!queryParams.Any(p => p.Key == "_order"))
                {
                    orderBySql = "order by A.ID";
                }

                queryParams
                    .ToList()
                    .ForEach(q =>
                    {
                        var key = q.Key.ToLower();

                        if (key.StartsWith("_"))
                        {
                            if (key == "_order")
                            {
                                if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "name")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.Name";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "sourceid")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.SourceID";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "textpath")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.TextPath";
                                }
                                else if (assetType.Object == "ReferenceItemType" && q.Value.ToLower() == "code")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "RI.Code";
                                }
                                else
                                {
                                    var field = fieldTypes.FirstOrDefault(f => f.Name.ToLower() == q.Value.ToLower());
                                    var valueColumn = "FormattedValue";
                                    var fieldDataType = getFieldDataType(field);
                                    if (field.Type == "Link") valueColumn = "Value";

                                    if (field == null)
                                    {
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "A.ID";
                                        return;
                                    }

                                    if (!string.IsNullOrEmpty(fieldDataType))
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"cast(F{field.ID}.{valueColumn} as {fieldDataType})";
                                    else
                                    {
                                        if (field.Type == "JsonElement")
                                        {
                                            FieldTypeDefinition_JsonElement jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(field.Definition);

                                            if (jsonElementDefinition.DataType == "decimal")
                                            {
                                                jsonElementDefinition.DataType = "float";
                                            }

                                            fieldDataType = jsonElementDefinition.DataType;

                                            orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"try_cast(FJP{field.ID}.Value as {fieldDataType})";
                                        }
                                        else
                                        {
                                            orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"F{field.ID}.{valueColumn}";
                                        }
                                    }
                                }
                            }
                            else if (key == "_pagenum")
                            {
                                if (int.TryParse(q.Value, out pageNum))
                                {
                                    if (pageNum < 1) pageNum = 1;
                                }
                            }
                            else if (key == "_pagesize")
                            {
                                if (int.TryParse(q.Value, out pageSize))
                                {
                                    if (pageSize < 1) pageSize = 1;
                                }
                            }
                        }
                        else
                        {
                            if (assetType.Object == "FusionAttributeType" && key == "name")
                            {
                                whereStatements.Add($"FA.[Name] = @faName");
                                dbArgs.Add($"@faName", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "sourceid")
                            {
                                whereStatements.Add($"FA.[SourceID] = @sourceID");
                                dbArgs.Add($"@sourceID", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "textpath")
                            {
                                whereStatements.Add($"FA.[TextPath] = @textpath");
                                dbArgs.Add($"@textpath", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "parentuid")
                            {
                                if ((CompanyContext.Database.Connection.QueryFirstOrDefault<int>("select ISNULL(parentId,0) from fusionattributetype where id = @id", new { id = assetType.ObjectID })) > 0)
                                {
                                    whereStatements.Add($"ATP.[uid] = @parentuid");
                                    dbArgs.Add($"@parentuid", q.Value);
                                }
                                
                            }
                            else if (assetType.Object == "ReferenceItemType" && key == "code")
                            {
                                whereStatements.Add($"RI.[Code] = @code");
                                dbArgs.Add($"@code", q.Value);
                            }
                            else
                            {
                                var field = fieldTypes.Find(f => f.Name.ToLower() == key);

                                if (field != null)
                                {
                                    if (field.Type == "JsonElement")
                                    {
                                        whereStatements.Add($"FJP{field.ID}.Value = @field{field.ID}");
                                        dbArgs.Add($"@field{field.ID}", q.Value);
                                    }
                                    else
                                    {
                                        whereStatements.Add($"F{field.ID}.FormattedValue = @field{field.ID}");
                                        dbArgs.Add($"@field{field.ID}", q.Value);
                                    }
                                }
                            }
                        }
                    });

                pagingSql.Add(orderBySql);

                if (pageSize > 0 || pageNum > 0)
                {
                    if (pageSize < 1) pageSize = 1;
                    if (pageNum < 1) pageNum = 1;

                    model.pageSize = pageSize;
                    model.pageNum = pageNum;

                    offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                    pagingSql.Add(offsetSql);
                }

            }
        }
        private string getFieldDataType(FieldType field)
        {
            switch (field.Type)
            {
                case "Date":
                case "DateTime":
                    return "datetime";
                case "Number":
                    return "bigint";
                case "Decimal":
                    return "float";
                case "Boolean":
                    return "bit";
                default:
                    return "";
            }
        }


        #endregion

    }
}
