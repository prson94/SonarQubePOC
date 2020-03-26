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
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using SpreadsheetLight;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;

namespace d360.model.DataAccessLayer
{
    public class AssetRepository : BaseRepository, IAssetRepository
    {
        internal ICompanyContext CompanyContext;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;
        internal ICommunityContext Community;
        public AssetRepository(ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider, ICommunityContext community)
            : base(companyContext)
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
        public async Task<IEnumerable<AssetTypeApiViewModel>> GetAssetType(IEnumerable<KeyValuePair<string, string>> queryParams, AssetTypeClass? Class, Guid? fusionTypeUid, Guid? assetTypeUid)
        {
            var dbArgs = new DynamicParameters();
            string condition = string.Empty;
            string optionalJoin = string.Empty;
            if (Class.HasValue)
            {
                if (fusionTypeUid.HasValue && fusionTypeUid.Value != Guid.Empty && (Class == AssetTypeClass.FusionAttribute || Class == AssetTypeClass.FusionQuery))
                {
                    if (Class == AssetTypeClass.FusionAttribute)
                    {
                        optionalJoin = @"inner join FusionAttributeType FAT on A.[Object] = 'FusionAttributeType' and A.Objectid = FAT.ID 
                                          inner join AssetType ATTFusionType on ATTFusionType.[Object] = 'FusionType' and ATTFusionType.ObjectID = FAT.FusionTypeID";
                        dbArgs.Add("@fusionTypeUid", fusionTypeUid);
                        condition += " and ATTFusionType.uid = @fusionTypeUid";
                    }

                    if (Class == AssetTypeClass.FusionQuery)
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
                    condition = " and A.[Class]=@Id";
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

                condition = string.Format(" and (A.[Class] = @class1 OR A.[Class] = @class2) AND (ATQFusionType.uid = @fusionTypeUid or ATTFusionType.uid = @fusionTypeUid)");

            }

            List<string> whereStatements = new List<string>();
            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "useastransformation"))
                {
                    bool useAsTransformation;
                    var useAsTransformationString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "useastransformation").Value;
                    if (Boolean.TryParse(useAsTransformationString, out useAsTransformation))
                    {

                        condition += " and A.UseAsTransformation=@useAsTransformation ";
                        dbArgs.Add("useAsTransformation", useAsTransformation);
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "hierarchical"))
                {
                    bool hierarchical;
                    var hierarchicalString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "hierarchical").Value;
                    if (Boolean.TryParse(hierarchicalString, out hierarchical))
                    {

                        condition += " and A.Hierarchical=@hierarchical ";
                        dbArgs.Add("hierarchical", hierarchical);
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "autodisplaydescription"))
                {
                    bool autoDisplayDescription;
                    var autoDisplayDescriptionString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "autodisplaydescription").Value;
                    if (Boolean.TryParse(autoDisplayDescriptionString, out autoDisplayDescription))
                    {

                        condition += " and A.AutoDisplayDescription=@autodisplaydescription ";
                        dbArgs.Add("autoDisplayDescription", autoDisplayDescription);
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "canownfusion"))
                {
                    bool canOwnFusion;
                    var canOwnFusionString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "canownfusion").Value;
                    if (Boolean.TryParse(canOwnFusionString, out canOwnFusion))
                    {

                        condition += " and A.CanOwnFusion=@canownfusion ";
                        dbArgs.Add("canownfusion", canOwnFusion);
                    }
                }

            }

            if (assetTypeUid != null && assetTypeUid.HasValue && assetTypeUid.Value != Guid.Empty)
            {
                condition += " and A.uid=@assetTypeUid ";
                dbArgs.Add("assetTypeUid", assetTypeUid.Value);
            }

            var sql = $@"
                        SELECT     A.[Name]
                                    ,ISNULL(A.[Description],'') as Description
                                    ,A.[Class] as ClassID
                                    ,ISNULL(A.[Notes],'') as Notes
                                    ,A.[uid]
									,A.Hierarchical
									,A.HierarchyMaximumDepth
									,A.DisplayFormat
									,A.AutoDisplayDescription
									,A.UseAsTransformation
                                    ,A.CanOwnFusion
                                    ,P.[Path]
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
            var includeSegments = false;
            var includePermissionDetails = false;

            var assetType = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == uid);
            if (assetType == null)
                throw new Exception("not found");

            assetTypeID = assetType.ID;

            List<string> hiddenFieldTypes = new List<string>() { "ComplexRelationLookup", "", "OwnershipLookup", "RefListRelationship" };
            var fieldTypes = CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetTypeID && !hiddenFieldTypes.Contains(f.Type)).ToList();

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

            dbArgs.Add("@userId", CompanyContext.CurrentResourceID);
            dbArgs.Add("@isAdmin", CompanyContext.CurrentResourceIsAdmin);

            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);
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
                var relatedAssetSql = " 1=1 ";
                bool includeBoth = false;


                if (queryParams.ToList().Any(q => q.Key.ToLower() == "_objectuid"))
                {
                    relatedAssetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_objectuid").Value;
                    if (Guid.TryParse(relatedAssetUIDString, out relatedAssetUID))
                    {
                        dbArgs.Add("@relatedAssetUid", relatedAssetUID);
                        relatedAssetSql = $"{objectAlias}.[UID] = @relatedAssetUid";
                    }
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and abs(I.SubjectID) = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and I.ObjectID = {subjectAlias}.ObjectID";

                }
                else if (queryParams.ToList().Any(q => q.Key.ToLower() == "_subjectuid"))
                {
                    relatedAssetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_subjectuid").Value;
                    if (Guid.TryParse(relatedAssetUIDString, out relatedAssetUID))
                    {
                        dbArgs.Add("@relatedAssetUid", relatedAssetUID);
                        relatedAssetSql = $"{subjectAlias}.[UID] = @relatedAssetUid";
                    }
                    intersectJoin = $"I.[Subject] = {subjectAlias}.[Object] and abs(I.SubjectID) = {subjectAlias}.ObjectID and I.[Object] = {objectAlias}.[Object] and I.ObjectID = {objectAlias}.ObjectID";
                }
                else
                {
                    //subject and object not specified
                    includeBoth = true;
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and I.SubjectID = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and I.ObjectID = {subjectAlias}.ObjectID";
                    reverseIntersectJoin = $"I.[Subject] = {subjectAlias}.[Object] and I.SubjectID = {subjectAlias}.ObjectID and I.[Object] = {objectAlias}.[Object] and I.ObjectID = {objectAlias}.ObjectID";
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
                            where {relatedAssetSql}";

                var innerCountSql = $@"
						select B.ID as Relationships  from Asset B
						inner join AssetType TB on TB.ID = B.AssetTypeID
						where {relatedAssetSql}
						and exists (select 1 from [Intersect] I
							inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid
							where {intersectJoin})    
";

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

                    var reverseInnerCountSql = $@"
						select B.ID as Relationships from Asset B
						inner join AssetType TB on TB.ID = B.AssetTypeID
						where {relatedAssetSql}
						and exists (select 1 from [Intersect] I
							inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid
							where {reverseIntersectJoin})";

                    innerSql = $@"select * from (
                        {innerSql}
                        union all
                        {reverseInnerSql}) RI";

                    innerCountSql = $@"
                        select * from (
                        {innerCountSql}
                        union all
                        {reverseInnerCountSql}) RI
                    ";
                }

                var joinSql = $@"
                    cross apply (
                        select (
                            {innerSql}
                            for json path
                        ) as Relationships
                    ) R";

                var joinCountSql = $@"cross apply ({innerCountSql}) R";

                fieldColumns.Add("R.Relationships");
                dbArgs.Add("@predicateUid", predicateUID);

                fieldJoins.Add(joinSql);
                countJoins.Add(joinCountSql);
            }


            if (includeRelationships)
                whereStatements.Add("R.Relationships is not null");

            //Add read permission check for admin and non-admin users as in GetAssets procedure
            whereStatements.Add($"A.ID not in ({CompanyContext.GetNoReadSqlStatement()})");
            whereStatements.Add($"A.AssetTypeID not in ({CompanyContext.GetAssetTypeNoReadSqlStatement()})");

            getQueryParamsSql(model, assetType, fieldTypes, dbArgs, whereStatements, pagingSql, queryParams);

            if (assetType.Class == AssetTypeClass.FusionAttribute)
            {
                if ((await CompanyContext.Database.Connection.QueryFirstOrDefaultAsync<int>("select ISNULL(parentId,0) from fusionattributetype where id = @id", new { id = assetType.ObjectID })) > 0)
                    fusionAttributeWithParent = true;
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_assetuid"))
            {
                List<Guid> assetUids = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_assetuid")
                    .Value.Split(',').Select(x =>
                    {
                        var guid = Guid.Empty;
                        Guid.TryParse(x, out guid);
                        return guid;
                    }).ToList();

                if (assetUids.Any(x => x == Guid.Empty))
                    throw new Exception("Invalid asset Uid in parameters!");

                if (assetUids.Count > 0)
                {
                    dbArgs.Add("assetUids", assetUids);
                    whereStatements.Add($"A.uid in @assetUids");
                }

            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "includesegments"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "includesegments").Value;
                bool.TryParse(value, out includeSegments);
            }

            bool includeParent = false;
            string parentFieldSQL = @" Parent.uid as ParentAssetUid,
					Parent.DisplayValue as ParentDisplayName,";
            string parentApplySQL = $@"left join [utility].[ArtifactAssetParent] AAP on A.ID = AAP.AssetID 
				left join AssetDetail Parent on Parent.ID = AAP.ParentAssetID";
            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includeparent"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includeparent").Value;
                bool.TryParse(value, out includeParent);

                if (queryParams.ToList().Any(x => x.Key.ToLower() == "usegraphforparent"))
                {
                    var useGraph = queryParams.ToList().FirstOrDefault(x => x.Key.ToLower() == "usegraphforparent").Value;
                    bool useGraphForParent = true;
                    bool.TryParse(useGraph, out useGraphForParent);

                    if (!useGraphForParent)
                    {
                        parentApplySQL = $@"outer apply (
					            select top 1 AD.uid, AD.DisplayValue from [IntersectType] IT
						            inner join [Intersect] I on I.IntersectTypeId = IT.Id and I.Object = A.Object and I.ObjectID = A.ObjectID
						            inner join [Predicate] P on P.ID = IT.PredicateID
						            inner join AssetDetail AD on AD.Object = I.Subject and AD.ObjectID = I.SubjectID
					            where IT.Object = T.Object and IT.ObjectID = T.ObjectID and P.Type = {(int)PredicateType.InterTypeHierarchy}
				            )Parent";
                    }
                }

                var hierarchy = CompanyContext.IntersectTypes
                    .FirstOrDefault(x => x.Object == assetType.Object && x.ObjectID == assetType.ObjectID && x.Predicate.Type == PredicateType.InterTypeHierarchy)?.ID;

                if (hierarchy == null)
                {
                    includeParent = false;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_filter"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                var filterExpressionParser = new FilterExpressionParser(CompanyContext, FilterExpressionParseType.CustomFields, includeParent);
                filterExpressionParser.LoadFieldTypes(fieldTypes, fieldColumns);
                Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams) + ")");

                foreach (var item in sqlParams)
                {
                    dbArgs.Add(item.Key, item.Value);
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_relationfilter"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_relationfilter").Value;
                var filterExpressionParser = new FilterExpressionParser(CompanyContext, FilterExpressionParseType.Relationships);
                Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams) + ")");

                foreach (var item in sqlParams)
                {
                    dbArgs.Add(item.Key, item.Value);
                }
            }

            string permissionDetailSQL = @"	outer apply (
                 select top 1 * from
				 (select PermissionsBitMask from UserAssetPermissions(@userId,A.AssetTypeID) 
					where AssetID = A.ID
				union all 	
					select PermissionsBitMask from UserAssetPermissions(@userId,A.AssetTypeID) 
					where AssetID = 0 and AssetTypeID = A.AssetTypeID)t
				   )Permission(mask)";

            string includePermissionFields = @",(SELECT case 
					   when permission.mask is null then @isAdmin
					   when permission.mask is not null and permission.mask & 1 = 1 then 1
					 else 0
					 end as 'ReadAsset',
					 Case 
					   when permission.mask is null then @isAdmin
					   when permission.mask is not null and permission.mask & 2 = 2 then 1
					 else 0
					 end as 'ModifyAsset', 
					 case 
					   when permission.mask is null then @isAdmin
					   when permission.mask is not null and permission.mask & 4 = 4 then 1
					 else 0
					 end as 'DeleteAsset'
					 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
					 ) as Permissions";

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_loadpermissiondetails"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_loadpermissiondetails").Value;
                bool.TryParse(value, out includePermissionDetails);
            }



            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
            {
                var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();
                if (!string.IsNullOrEmpty(simpleFilter))
                {
                    simpleFilter = CompanyContext.GetEscapedFilterString(simpleFilter);

                    dbArgs.Add("@simpleFilter", simpleFilter);

                    List<string> simpleFilters = new List<string>();
                    foreach (var ft in fieldTypes.Where(x => x.IsListable == true))
                    {
                        if (ft.Type == "Tag")
                        {
                            string simpleFilterTagSql = @"exists (select top 1 AT.TagId from AssetTag AT
						                                inner join Tag T on AT.TagId = T.Id
						                                where AT.AssetID = A.ID and T.Value like @simpleFilter)";

                            simpleFilters.Add(simpleFilterTagSql);
                        }
                        else if (ft.Type == "Lookup" && ft.AllowAllValue)
                        {
                            simpleFilters.Add($"(select case when F{ft.ID}.[Value] = '0' then @F{ft.ID}_AllValue else F{ft.ID}.FormattedValue end as value) like @simpleFilter");
                        }
                        else
                        {
                            simpleFilters.Add($"F{ft.ID}.FormattedValue like @simpleFilter");
                        }
                    }

                    if (includeParent)
                    {
                        simpleFilters.Add($"Parent.DisplayValue like @simpleFilter");
                    }

                    if (assetType.Class == AssetTypeClass.Reference)
                    {
                        simpleFilters.Add($"A.Code like @simpleFilter");
                    }

                    whereStatements.Add($"({string.Join(" or ", simpleFilters)})");
                }
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_ownedby"))
            {
                List<Guid> ownerUids = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_ownedby")
                    .Value.Split(',').Select(x =>
                    {
                        var guid = Guid.Empty;
                        Guid.TryParse(x, out guid);
                        return guid;
                    }).ToList();

                if (ownerUids.Any(x => x == Guid.Empty))
                    throw new Exception("Invalid Owner Uid in parameters!");

                if (ownerUids.Count > 0)
                {
                    dbArgs.Add("ownerUids", ownerUids);
                    whereStatements.Add($@"EXISTS (
                        SELECT 1 FROM [dbo].[ResponsibilityDetail] rd WHERE rd.AssetID = a.ID AND rd.ResourceUid in @ownerUids
                        UNION ALL
                        SELECT 1 FROM [dbo].[ResponsibilityDetail] rd  WHERE rd.AssetID = 0 AND rd.AssetTypeID = a.AssetTypeID AND rd.ResourceUid in @ownerUids
                    )");
                }
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
                {string.Join("\n", countJoins)}
                {(includeParent ? parentApplySQL : "")}
                {whereSql}";



            var sql = $@"
                select
                    A.ID as AssetId,
                    A.[UID] as [AssetUid],
                    A.AssetTypeId,
                    T.[UID] as AssetTypeUid,
                    A.UpdatedOn,
                    A.CreatedOn,
                    {(includeParent ? parentFieldSQL : "")}
                    A.Code,
                    {(includeSegments ? "Node.Segments," : "")}
                    Node.Path --,
                    --Node.Segments --GOV-8967 - temporarily remove segments property due to analyze issue
                    {(assetType.Object == "FusionAttributeType" ? " , FA.SourceID, FA.Name, FA.TextPath" : "")} 
                    {(fusionAttributeWithParent ? " , ATP.uid as ParentUid" : "")}
                    {fieldsSql}
                    {(includePermissionDetails ? includePermissionFields : "")}
                from Asset A
                {(assetType.Object == "FusionAttributeType" ? " inner join FusionAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {(fusionAttributeWithParent ? " inner join Asset ATP on ATP.ObjectID = FA.ParentID and ATP.[Object] = 'FusionAttribute'" : "")}
                {(assetType.Object == "FusionQueryAttributeType" ? " inner join FusionQueryAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {string.Join("\n", fieldJoins)}
                left join graph.AssetNode Node on Node.Uid = a.uid and Node.AssetTypeUid = T.[UID]
                {(includePermissionDetails ? permissionDetailSQL : "")}
                {(includeParent ? parentApplySQL : "")}
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

            if (includePermissionDetails)
            {
                foreach (var result in results)
                {
                    AssetsApiPermissionViewModel permissionObject = JsonConvert.DeserializeObject<AssetsApiPermissionViewModel>(result.Permissions);

                    //Override responsibilities for Admin users (as in GetAssets procedure)
                    if (CompanyContext.CurrentResourceIsAdmin)
                    {
                        permissionObject.ModifyAsset = true;
                        permissionObject.DeleteAsset = true;
                    }

                    result.Permissions = permissionObject;
                }
            }

            if (includeSegments)
            {
                foreach (var result in results)
                {
                    try
                    {
                        var xmlString = result.Segments;
                        if (xmlString != null)
                        {
                            List<AssetsByPathItemSegmentApiViewModel> segments = new List<AssetsByPathItemSegmentApiViewModel>();
                            var xml = XDocument.Parse(xmlString as string);
                            foreach (var segment in xml.Descendants("segment"))
                            {
                                segments.Add(new AssetsByPathItemSegmentApiViewModel()
                                {
                                    Value = segment.Value
                                });
                            }

                            result.Segments = segments;
                        }
                    }
                    catch
                    {
                        result.Segments = null;
                    }
                }
            }

            model.items = results;
            model.total = count;

            return model;
        }
        public async Task<SLDocument> GetAssetsExcel(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var results = await GetAssets(uid, queryParams);
            var assetType = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == uid);
            var fields = CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetType.ID).ToList();

            bool includeParent = false;
            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includeparent"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includeparent").Value;
                bool.TryParse(value, out includeParent);
            }

            var typesToAvoid = new List<string>() {
                DataType.Attribute.ToString(),
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.FilteredLookup.ToString(),
                DataType.OwnershipLookup.ToString()
            };

            //add default fields
            fields.Add(new FieldType { Type = "string", Name = "Code", FriendlyName = "Code" });
            fields.Add(new FieldType { Type = "string", Name = "Path", FriendlyName = "Path" });
            fields.Add(new FieldType { Type = "date", Name = "UpdatedOn", FriendlyName = "Updated On" });
            fields.Add(new FieldType { Type = "date", Name = "CreatedOn", FriendlyName = "Created On" });
            fields.Add(new FieldType { Type = "string", Name = "AssetUid", FriendlyName = "Asset Uid" });
            fields.Add(new FieldType { Type = "number", Name = "AssetId", FriendlyName = "Asset Id" });
            fields.Add(new FieldType { Type = "number", Name = "AssetTypeId", FriendlyName = "Asset Type Id" });
            fields.Add(new FieldType { Type = "string", Name = "AssetTypeUid", FriendlyName = "Asset Type Uid" });

            if (includeParent)
            {
                fields.Add(new FieldType { Type = "string", Name = "ParentAssetUid", FriendlyName = "Parent Asset Uid" });
                fields.Add(new FieldType { Type = "string", Name = "ParentDisplayName", FriendlyName = "Parent Display Name" });
            }


            var rowData = results.items.ToList();

            var document = new SLDocument();
            const string assetSheetName = "Assets";
            const string apiSheetName = "Api Info";


            #region Populate Excel Document

            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, assetSheetName);

            document.AddWorksheet(apiSheetName);
            document.SelectWorksheet(apiSheetName);

            document.SetCellValue(1, 1, "pageSize");
            document.SetCellValue(1, 2, results.pageSize);
            document.SetCellValue(2, 1, "pageNum");
            document.SetCellValue(2, 2, results.pageNum);
            document.SetCellValue(3, 1, "total");
            document.SetCellValue(3, 2, results.total);


            document.SelectWorksheet(assetSheetName);

            int index = 1;

            foreach (var field in fields)
            {
                if (typesToAvoid.Contains(field.Type))
                    continue;
                document.SetCellValue(1, index++, (string)field.FriendlyName);
            }


            if (rowData == null || rowData.Count == 0)
            {
                var s = new MemoryStream();
                document.SaveAs(s);
                return document;
            }


            int rowNumber = 1;
            foreach (var row in rowData)
            {
                index = 1;
                rowNumber++;
                var rowValues = (row as IDictionary<string, object>);

                foreach (var field in fields)
                {
                    if (typesToAvoid.Contains(field.Type))
                        continue;

                    if (rowValues.ContainsKey(field.Name))
                    {
                        var val = rowValues[field.Name];
                        setCellValueFromField(document, rowNumber, index, field, val);
                    }

                    index++;
                }
            }

            #endregion

            return document;
        }
        public async Task<AssetsByPathApiViewModel> GetAssetsByPath(AssetsByPathApiRequestModel model)
        {
            var dbArgs = new DynamicParameters();
            var returnModel = new AssetsByPathApiViewModel();

            var prefilterSql = "";
            var countSql = "";
            var sql = "";

            int i = 1;
            foreach (var filter in model.filters)
            {
                string prefilterRelationshipStatement = string.Empty;
                string prefilterStatement = string.Empty;
                if (filter.AsSideOfRelationship != null)
                {
                    switch (filter.AsSideOfRelationship.Side)
                    {
                        case AssetsByPathItemApiFilterSideOfRelationshipRequestEnum.Object:
                            prefilterRelationshipStatement += "inner join IntersectType I on I.Object = T.Object and I.ObjectID = T.ObjectID";
                            break;
                        case AssetsByPathItemApiFilterSideOfRelationshipRequestEnum.Subject:
                            prefilterRelationshipStatement += "inner join IntersectType I on I.Subject = T.Object and I.SubjectID = T.ObjectID";
                            break;
                    }
                    if (filter.AsSideOfRelationship.PredicateType.HasValue || filter.AsSideOfRelationship.PredicateUid.HasValue)
                    {
                        prefilterRelationshipStatement += $" inner join [Predicate] P on P.ID = I.PredicateID";
                        if (filter.AsSideOfRelationship.PredicateType.HasValue)
                        {
                            prefilterRelationshipStatement += $" and P.[Type] = @pt{i}";
                            dbArgs.Add($"@pt{i}", (int)filter.AsSideOfRelationship.PredicateType.Value);
                        }
                        if (filter.AsSideOfRelationship.PredicateUid.HasValue)
                        {
                            prefilterRelationshipStatement += $" and P.[Uid] = @puid{i}";
                            dbArgs.Add($"@puid{i}", filter.AsSideOfRelationship.PredicateUid.Value);
                        }
                    }
                }
                if (filter.Class.HasValue)
                {
                    prefilterStatement += $"where T.[Class] = @class{i}";
                    dbArgs.Add($"@class{i}", (int)filter.Class.Value);
                }
                if (filter.Uid.HasValue)
                {
                    prefilterStatement += (string.IsNullOrEmpty(prefilterStatement) ? "where" : " and ") +
                        $" T.[Uid] = @uid{i}";
                    dbArgs.Add($"@uid{i}", filter.Uid.Value);
                }
                if (filter.UseAsTransformation.HasValue)
                {
                    prefilterStatement += (string.IsNullOrEmpty(prefilterStatement) ? "where" : " and ") +
                        $" T.[UseAsTransformation] = @uat{i}";
                    dbArgs.Add($"@uat{i}", filter.UseAsTransformation.Value);
                }

                if (!string.IsNullOrEmpty(prefilterStatement))
                {
                    prefilterSql += (string.IsNullOrEmpty(prefilterSql)) ? "" : " union ";
                    prefilterSql += $"select T.ID from AssetType T {prefilterRelationshipStatement} {prefilterStatement}";
                }

                i++;
            }

            dbArgs.Add("@phrase", model.searchPhrase.ToSqlFullTextSearchPhrase());

            if (!string.IsNullOrEmpty(prefilterSql))
            {
                prefilterSql = $"and N.AssetTypeID in ({prefilterSql})";
            }

            countSql = $@"
select	count(1)
from	graph.AssetNode N
where	CONTAINS(N.[Path], @phrase) {prefilterSql}
";

            sql = $@"
select	N.Uid,
		N.AssetTypeUid,
        T.Name as AssetTypeName,
        coalesce(S.Icon, 'fa-book') as AssetTypeIcon, 
		N.Segments as SegmentsXml
from	graph.AssetNode N
        inner join AssetType T on T.ID = N.AssetTypeID
        left join AssetTypeStyle S on S.ID = T.ID
where	CONTAINS(N.[Path], @phrase) {prefilterSql}
order by N.[Path] asc
OFFSET(@pageNum*@pageSize) ROWS FETCH NEXT (@pageSize) ROWS ONLY
";
            if (model.pageNum <= 1)
            {
                model.pageNum = 1;
            }
            if (model.pageSize <= 0 || model.pageSize > 250)
            {
                model.pageSize = 250;
            }

            dbArgs.Add("@pageNum", model.pageNum - 1);
            dbArgs.Add("@pageSize", model.pageSize);

            var count = await CompanyContext.QueryAsync<int>(countSql, dbArgs);
            var total = count.First();
            var results = await CompanyContext.QueryAsync<AssetsByPathItemApiViewModel>(sql, dbArgs);

            returnModel.items = results;
            returnModel.pageNum = model.pageNum;
            returnModel.pageSize = model.pageSize;
            returnModel.total = total;

            return returnModel;
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
        public Tuple<HttpStatusCode, string, string> AddAssetType(AssetTypeUpsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate, int resourceId, out string nameFriendlyName, out bool isNamePartOfKey)
        {
            var parentType = SystemObjects.ArtifactType;
            nameFriendlyName = "Name";
            isNamePartOfKey = true;

            if (!string.IsNullOrEmpty(model?.Name ?? null))
                model.Name = model.Name.Trim();

            Guid uid = Guid.NewGuid();
            if (model.Uid != Guid.Empty)
            {
                uid = model.Uid;
            }

            switch (model.Class)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.TechnicalAsset:
                    #region
                    var a = new AssetType
                    {
                        uid = uid,
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
                    var orgAssetType = CompanyContext.Filter<AssetType>(i => i.Object == model.Object && i.ObjectID == model.ObjectID).SingleOrDefault();
                    if (orgAssetType != null)
                    {
                        orgAssetType.uid = uid;
                        CompanyContext.Update(orgAssetType);
                    }
                    #endregion
                    break;
                case AssetTypeClass.Policy:
                    #region                    
                    var p = new AssetType
                    {
                        uid = uid,
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

                    if (p.HierarchyMaximumDepth <= 0 || p.HierarchyMaximumDepth > 10)
                        return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, "Invalid Maximum Depth", AssetTypeErrors.InvalidPolicyDepth);

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
                        uid = uid,
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
                        uid = uid,
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

                    var ruleAssetType = CompanyContext.Filter<AssetType>(i => i.Object == model.Object && i.ObjectID == model.ObjectID).SingleOrDefault();
                    if (ruleAssetType != null)
                    {
                        ruleAssetType.uid = uid;
                        CompanyContext.Update(ruleAssetType);
                    }

                    #endregion
                    break;
                case AssetTypeClass.FusionAttribute:

                    int? parentId = parentAssetType?.ObjectID;
                    int fusionTypeId = model.FusionID.Value;

                    var fusionAttrType = new FusionAttributeType
                    {
                        Name = model.Name,
                        ParentID = parentId,
                        ScanEnabled = true,
                        FusionTypeID = fusionTypeId
                    };

                    CompanyContext.Add(fusionAttrType);
                    model.ObjectID = fusionAttrType.ID;
                    model.Object = SystemObjects.FusionAttributeType.ToString();

                    var fatAssetType = CompanyContext.Filter<AssetType>(i => i.Object == model.Object && i.ObjectID == model.ObjectID).SingleOrDefault();
                    if (fatAssetType != null)
                    {
                        fatAssetType.Description = model.Description;
                        CompanyContext.Update(fatAssetType);
                    }
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
        public List<DatabaseBulkAssetResult> PutAssets(List<AssetUpdate> assets, AssetType assetType, ApiExecution execution, bool fieldJsonPropertyLoadLimitToTopLevel = true, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.ImportAssets(execution, assetType, assets, false, fieldJsonPropertyLoadLimitToTopLevel: fieldJsonPropertyLoadLimitToTopLevel, sendWorkflowEvents: sendWorkflowEvents, lookupFieldsPassedByValue: lookupFieldsPassedByValue);

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
        public Tuple<HttpStatusCode, string, string> UpdateAssetType(AssetTypeUpsert model, AssetType assetType, AssetType parentAssetType, Predicate predicate)
        {
            List<AssetTypeClass> predicateClass = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference };

            bool shouldRemoveOldRelationshipType = (model.Class == AssetTypeClass.Reference);

            if (!string.IsNullOrEmpty(model?.Name ?? null))
                model.Name = model.Name.Trim();

            switch (model.Class)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.Policy:
                case AssetTypeClass.Reference:
                case AssetTypeClass.Model:
                case AssetTypeClass.TechnicalAsset:
                    #region

                    if (assetType == null)
                    {
                        return new Tuple<HttpStatusCode, string, string>(
                            HttpStatusCode.BadRequest,
                            $"Wrong {model.Class.ToString()}",
                            $"Invalid {model.Class.ToString()} provided. {AssetTypeErrors.CheckRequest}"
                        );
                    }

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

                    #endregion
                    break;
                case AssetTypeClass.Organization:
                    #region

                    var org = CompanyContext.GetById<OrganizationType>(model.ObjectID);
                    if (org == null) return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Organization.ToString()}", $"Invalid {AssetTypeClass.Organization.ToString()} provided. {AssetTypeErrors.CheckRequest}");
                    org.Name = model.Name;
                    org.Description = model.Description;
                    org.DisplayFormat = model.DisplayFormat;
                    CompanyContext.Update(org);

                    #endregion
                    break;
                case AssetTypeClass.Rule:
                    #region

                    var r = CompanyContext.GetById<RuleType>(model.ObjectID);
                    if (r == null) return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Rule.ToString()}", $"Not valid {AssetTypeClass.Rule.ToString()} provided. {AssetTypeErrors.CheckRequest}");
                    r.Name = model.Name;
                    r.DisplayFormat = model.DisplayFormat;
                    r.Description = model.Description;
                    CompanyContext.Update(r);

                    assetType.Name = model.Name;
                    assetType.DisplayFormat = model.DisplayFormat;
                    assetType.Description = model.Description;

                    #endregion
                    break;
                case AssetTypeClass.FusionAttribute:
                    #region

                    var fusionAttributeType = CompanyContext.GetById<FusionAttributeType>(model.ObjectID);
                    if (fusionAttributeType == null)
                    {
                        return new Tuple<HttpStatusCode, string, string>(
                            HttpStatusCode.BadRequest,
                            $"Wrong {AssetTypeClass.FusionAttribute.ToString()}",
                            $"Not valid {AssetTypeClass.FusionAttribute.ToString()} provided. {AssetTypeErrors.CheckRequest}"
                        );
                    }

                    assetType.Description = model.Description;

                    fusionAttributeType.Name = model.Name;
                    CompanyContext.Update(fusionAttributeType);

                    #endregion
                    break;
            }

            var parentType = SystemObjectHelper.GetSystemObjects(model.Class).ToString();
            IntersectType intersectType = null;

            if (predicateClass.Contains(model.Class) && (parentAssetType != null || predicate != null))
            {
                var parentPredicateType = (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy) ?
                    PredicateType.IntraTypeHierarchy :
                    PredicateType.InterTypeHierarchy;

                intersectType = CompanyContext.Filter<IntersectType>(i =>
                    i.Object == model.Object &&
                    i.ObjectID == model.ObjectID &&
                    i.Predicate.Type == parentPredicateType,
                    i => i.Predicate
                ).SingleOrDefault();

                var parentID = (parentAssetType != null ? parentAssetType.ObjectID : model.ObjectID);

                if (intersectType != null)
                {
                    bool relationshipChangeMade = false;
                    var anyExistingRelationships = CompanyContext.Any<Intersect>(i => i.IntersectTypeID == intersectType.ID);

                    if (predicate != null)
                    {
                        if (intersectType.PredicateID != predicate.ID)
                        {
                            intersectType.PredicateID = predicate.ID;
                            relationshipChangeMade = true;
                        }
                    }

                    if (intersectType.SubjectID != parentID)
                    {
                        if (anyExistingRelationships)
                        {
                            return new Tuple<HttpStatusCode, string, string>(
                                HttpStatusCode.Conflict,
                                $"Invalid Parent Selected",
                                $"There are existing parent/child relationships for assets of this type. You may not alter the parent type until these relationships are removed. {AssetTypeErrors.CheckRequest}"
                            );
                        }

                        intersectType.SubjectID = parentID;
                        relationshipChangeMade = true;
                    }

                    if (relationshipChangeMade)
                    {
                        CompanyContext.Update(intersectType);
                    }
                }
                else
                {
                    // We now want to require a parent on an asset type that previously did NOT have any parent.
                    intersectType = new IntersectType
                    {
                        IsSystem = true,
                        Subject = parentType,
                        SubjectID = parentID,
                        Object = model.Object,
                        ObjectID = model.ObjectID,
                        PredicateID = predicate.ID
                    };
                    CompanyContext.Add(intersectType);
                }
            }
            else if (shouldRemoveOldRelationshipType)
            {
                // We are removing the parent completely from this asset type.

                var parentPredicateType = PredicateType.InterTypeHierarchy;

                intersectType = CompanyContext.Filter<IntersectType>(i =>
                    i.Object == model.Object &&
                    i.ObjectID == model.ObjectID &&
                    i.Predicate.Type == parentPredicateType,
                    i => i.Predicate
                ).SingleOrDefault();

                if (intersectType != null)
                {
                    CompanyContext.Delete(SystemObjects.IntersectType, intersectType.ID);
                }
            }

            // If we made it this far, then we can save the asset type.
            CompanyContext.Update(assetType);


            return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", "");
        }

        public List<DatabaseBulkAssetResult> DeleteAsset(AssetDeletes assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.RemoveAssets(execution, assetType, assets, sendWorkflowEvents: sendWorkflowEvents);

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
                ResourceID = execution.ResourceID,
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

        public async Task<ApiExecutionInfo> BulkDeleteAssets(Guid assetTypeUid, AssetDeletes assets, ApiExecution execution, bool clearallassetsfromtype, bool sendWorkflowEvents = true)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.DeleteAssets,
                SendWorkflowEvents = sendWorkflowEvents
            };


            if ((assets == null || assets.Count == 0) && clearallassetsfromtype)
            {
                var assetList = CompanyContext.Assets.Include(at => at.AssetType).Where(xx => xx.AssetType.uid == assetTypeUid).Select(xx => new AssetDelete { Uid = xx.uid, Cascade = true }).ToList<AssetDelete>();
                assets = new AssetDeletes();
                if (assetList != null)
                    assets.AddRange(assetList);
                execution.Total = assets.Count;
            }

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
                ResourceID = execution.ResourceID,
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
                ResourceID = execution.ResourceID,
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

        public AssetType GetAssetTypeByUidAndClass(Guid assetTypeUid, AssetTypeClass @class)
        {
            return CompanyContext.Filter<AssetType>(i => i.uid == assetTypeUid && i.Class == @class).SingleOrDefault();
        }

        public AssetType GetAssetTypeByModel(AssetTypeUpsert model)
        {
            return CompanyContext.Filter<AssetType>(x => x.ObjectID == model.ObjectID && x.Object == model.Object).SingleOrDefault();
        }

        public ApiExecution GetExecutionItemByUid(Guid executionUid)
        {
            return CompanyContext.Filter<ApiExecution>(i => i.ExecutionID == executionUid).SingleOrDefault();
        }

        public async Task<APIExecutionAPIModelResult> GetExecutionItems(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            int pageNum = 1;
            int pageSize = 200;
            string orderDirection = "asc";
            string orderBySql = "";
            string offsetSql = "";
            if (queryParams.Any(x => x.Key == "_direction"))
            {
                string[] allowedDirections = new string[] { "asc", "desc" };
                var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value;
                if (!allowedDirections.Contains(order.Trim().ToLower()))
                {
                    return new APIExecutionAPIModelResult
                    {
                        Message = "Invalid order direction passed in the request",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }
                orderDirection = allowedDirections.Contains(order.Trim().ToLower()) ? order : "asc";
            }

            if (!queryParams.Any(p => p.Key == "_order"))
            {
                orderBySql = $" order by [CompletedOn] {orderDirection} ";
            }
            else
            {

                var orderByCol = queryParams.FirstOrDefault(p => p.Key == "_order").Value;
                string[] validOrderByFields = { "executionid", "resourceuid", "resource", "total",
                                                "processed", "error", "errormessage", "processingstartedon",
                                                "startedon", "completedon", "method", "route", "fields" };
                if (!validOrderByFields.Contains(orderByCol.ToLower()))
                    return new APIExecutionAPIModelResult
                    {
                        Message = "Invalid order passed in the request",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                orderBySql = $" order by {orderByCol} {orderDirection} ";
            }

            if (queryParams.Any(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)))
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)).Value, out pageNum))
                    if (pageNum < 1) pageNum = 1;

            if (queryParams.Any(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)))
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)).Value, out pageSize))
                    if (pageSize < 1) pageSize = 1;

            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;
                if (pageSize > 25000) pageSize = 25000;
                if (pageNum > 10000) pageNum = 10000;


                offsetSql = $" offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only ";
            }


            var sql = $@"
                        SELECT Ex.[ExecutionID]
                              ,GR.[uid] as ResourceUid
	                          ,CONCAT(GR.[FirstName],' ', GR.[LastName]) as [Resource]
                              ,[Total]
                              ,[Processed]
                              ,[Error]
	                          ,ERR.[Message] as ErrorMessage
                              ,[ProcessingStartedOn] 
                              ,[StartedOn] 
                              ,[CompletedOn]
                              ,[Method]
                              ,[Route]
                              ,[Fields]
                          FROM [api].[Execution] Ex
                          INNER JOIN [reporting].[Global_Resource] GR on GR.ResourceID = Ex.ResourceID  
                          LEFT JOIN [api].[ExecutionAssetError] ERR on ERR.[ExecutionID] = Ex.[ExecutionID] 
                          {orderBySql}
                          {offsetSql}
                        ";
            var countSQL = $@"
                        SELECT count(*)
                          FROM [api].[Execution] Ex
                          INNER JOIN [reporting].[Global_Resource] GR on GR.ResourceID = Ex.ResourceID 
                          LEFT JOIN [api].[ExecutionAssetError] ERR on ERR.[ExecutionID] = Ex.[ExecutionID]
                        ";
            var executions = await CompanyContext.QueryAsync<dynamic>(sql);
            var count = await CompanyContext.QueryAsync<int>(countSQL);

            var items = executions.Select(x =>
            {
                var f = string.IsNullOrEmpty(x.Fields) ? "{}" : x.Fields;
                return new APIExecutionAPIModel
                {
                    CompletedOn = x.CompletedOn,
                    Error = x.Error,
                    ErrorMessage = x.ErrorMessage,
                    ExecutionID = x.ExecutionID,
                    Fields = JObject.Parse(f),
                    Method = x.Method,
                    Processed = x.Processed,
                    ProcessingStartedOn = x.ProcessingStartedOn,
                    Resource = x.Resource,
                    ResourceUid = x.ResourceUid,
                    Route = x.Route,
                    StartedOn = x.StartedOn,
                    Total = x.Total
                };
            });
            var resultsModel = new APIExecutionAPIModelResult
            {
                items = items,
                total = count.FirstOrDefault(),
                pageNum = pageNum,
                pageSize = pageSize,
                StatusCode = HttpStatusCode.OK
            };

            return resultsModel;
        }

        public void UpsertAssetStyle(int assetTypeId, string foreColor, string backColor, string icon, string objectName = "Tx")
        {
            var style = CompanyContext.GetAssetTypeStyle(assetTypeId);
            bool add = (style == null);

            string iconText = CompanyContext.GetIconText(objectName);

            if (add)
            {
                style = new AssetTypeStyle
                {
                    ID = assetTypeId,
                    IconBackColor = backColor,
                    IconForeColor = foreColor,
                    IconText = iconText,
                    Icon = icon
                };
                CompanyContext.Add(style);
            }
            else
            {
                style.IconBackColor = backColor;
                style.IconForeColor = foreColor;
                style.IconText = iconText;
                style.Icon = icon;
                CompanyContext.Update(style);
            }


        }

        public bool DoesAssetExists(Guid uid)
        {
            return CompanyContext.Any<Asset>(i => i.uid == uid);
        }

        public bool IsReachedTransformationLimit(AssetTypeUpsert model)
        {
            bool reached = false;
            if ((model.Class == AssetTypeClass.BusinessAsset || model.Class == AssetTypeClass.TechnicalAsset) && model.UseAsTransformation == true)
            {
                var useAsTransformationLimit = Community.GetCompanySettingByKey<int>("UseAsTransformationLimit");
                var totalUseAsTransform = CompanyContext.Filter<AssetType>(i => i.UseAsTransformation == true).Count();
                if (totalUseAsTransform > useAsTransformationLimit)
                    reached = true;
            }
            return reached;
        }

        public Guid GetRuleUIDFromRuleID(int ruleid)
        {
            var asset = CompanyContext.Filter<Asset>(i => i.ObjectID == ruleid && i.Object == "Rule").SingleOrDefault();
            if (asset == null)
                return Guid.Empty;
            return asset.uid;
        }

        public async Task<dynamic> GetAssetDetails(Asset asset)
        {
            var dbArgs = new DynamicParameters();
            dbArgs.Add("@typeUid", asset.AssetType.uid.ToString());
            dbArgs.Add("@assetUid", asset.uid.ToString());
            dbArgs.Add("@id", asset.ID);

            var sql = $@"
                select
	                A.[UID] as [uid],
                    COALESCE(f.FormattedValue, ft.DefaultFormattedValue) as Status,
	                Node.Path 
                from Asset A
                inner join AssetType AT on AT.ID = A.AssetTypeID and AT.UID = @typeUid
                left join FieldType ft on AT.Object = ft.Object and AT.ObjectID = ft.ObjectID and ft.FriendlyName like 'status'
                left Join Field f on f.FieldTypeID = ft.ID and f.AssetID = A.ID
                left join graph.AssetNode Node on Node.Uid = a.uid and Node.AssetTypeUid = AT.[UID]
                WHERE A.ID = @id
            ";
            var res = new
            {
                AssetDetail = await CompanyContext.QueryFirstOrDefaultAsync<dynamic>(sql, dbArgs),
                Scores = await GetAssetScores(asset.uid)
            };
            return res;
        }

        public async Task<dynamic> GetAssetTypeDetails(AssetType type)
        {
            var dbArgs = new DynamicParameters();
            dbArgs.Add("@typeUid", type.uid.ToString());
            var sql = $@"
                    SELECT
                        A.[uid]
                        ,P.[Path]
                    FROM AssetType A
                        cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
                        where       A.[State] = 1 and A.Uid = @typeUid
                    ";

            return await CompanyContext.QueryFirstOrDefaultAsync<dynamic>(sql, dbArgs);
        }

        private async Task<IEnumerable<dynamic>> GetAssetScores(Guid AssetUid)
        {
            var scoreSQL = @"select S.AssetUid,
S.EffectiveDate,
S.EndDate,
S.RunDate,
case 
	when S.ScoreType = 1 then 'Governance'
	when S.ScoreType = 2 then 'DataQuality'
end as ScoreType,
S.Value, 
AL.LowerThreshold, 
AL.UpperThreshold 
from metrics.Score S
inner join Asset A on A.Uid = S.AssetUid
inner join AssetType AT on AT.Id = A.AssetTypeID
inner join metrics.Allocation AL on AT.uid = AL.AssetTypeUid and AL.ScoreType = s.ScoreType
where S.AssetUid = @assetUid and EndDate is null and EffectiveDate < @date";



            return await CompanyContext.QueryAsync<dynamic>(scoreSQL, new { assetUid = AssetUid, date = DateTime.UtcNow });
        }
    }
}
