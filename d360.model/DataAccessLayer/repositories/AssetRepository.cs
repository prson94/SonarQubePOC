using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json;
using d360.core;
using d360.core.queue;
using d360.extensions;
using System.Net;
using d360.core.helpers;
using d360.core.resources;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.core.entities.Process;
using AngleSharp.Text;

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

        /// <summary>
        /// Common code for creating batch calls.
        /// </summary>
        /// <param name="executionInfo"></param>
        /// <param name="execution"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected async Task<ApiExecutionInfo> CreateApiBatchJob(ApiExecutionInfo executionInfo, ApiExecution execution, object data)
        {
            // Save to storage container.
            StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(data));

            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;
            CompanyContext.Add(execution);

            // Save to queue.
            if (!await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo))
            {
                throw new Exception(AZURE_QUEUE_INSERTION_FAILURE_MESSAGE);
            }

            return executionInfo;
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
                    else
                        throw new ArgumentException("Invalid value for parameter [useastransformation]", useAsTransformationString);
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
                    else
                        throw new ArgumentException("Invalid value for parameter [hierarchical]", hierarchicalString);
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
                    else
                        throw new ArgumentException("Invalid value for parameter [autoDisplayDescription]", autoDisplayDescriptionString);
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
                    else
                        throw new ArgumentException("Invalid value for parameter [canOwnFusion]", canOwnFusionString);
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "autodisplayparent"))
                {
                    bool autoDisplayParent;
                    var autoDisplayParentString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "autodisplayparent").Value;
                    if (Boolean.TryParse(autoDisplayParentString, out autoDisplayParent))
                    {

                        condition += " and A.AutoDisplayParent=@autoDisplayParent ";
                        dbArgs.Add("autoDisplayParent", autoDisplayParent);
                    }
                    else
                        throw new ArgumentException("Invalid value for parameter [autoDisplayParent]", autoDisplayParentString);
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
                                    ,A.AutoDisplayParent
                                    ,A.FlowObjectType
                                    ,P.[Path]
                                    ,AT.IconBackColor as BackColor
                                    ,AT.Icon as Icon
                                    ,AT.IconForeColor as ForeColor
                        FROM        AssetType A
                                    {optionalJoin}
                                    cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
                                    left join [dbo].[AssetTypeStyle] AT on (A.ID = AT.ID)
                        where       A.[State] = 1 and A.ObjectID != 0
                        {condition}
                        order by    P.[Path]
                        ";

            // If you change the order of the select columns please pay attention to the dapper multimap split on parameter where it is splitting out the icon class.

            return await CompanyContext.QueryAsync<AssetTypeApiViewModel, IconStyleInsert, AssetTypeApiViewModel>(sql, param: dbArgs, map: (a, i) => { a.IconStyle = i; return a; }, splitOn: "Path,BackColor", timeout: ApiTimeout);
        }

        //UseAsAdmin is used to override permissions from reading an access. It is used by Process Designer Export
        public async Task<AssetsApiViewModel> GetAssets(AssetType assetType, IEnumerable<KeyValuePair<string, string>> queryParams, bool useAsAdmin = false)
        {
            var assetTypeID = 0;
            var includeRelationships = false;
            var fusionAttributeWithParent = false;
            var includeSegments = false;
            var includePermissionDetails = false;
            bool includeOnlyListableFields = false;
            string populatePremissionAssetTableSQL = " ";
            string permissionDetailSQL = " ";
            string includePermissionFields = " ";
            bool listColorsAsJSON = false;
            bool includeColor = true;
            var includeTotal = true;
            bool isHierachyItem = false;
            string hierarchyParentUidCol = "";
            string hierarchyParentUidSelect = "";
                        
            if (assetType == null)
                throw new Exception("Invalid assetType specified");

            if (useAsAdmin && !queryParams.ToList().Any(k => k.Key.ToLower() == "_assetuid"))
            {
                throw new ArgumentException("UseAsAdmin parameter can be used only with _assetUid specified!");
            }

            assetTypeID = assetType.ID;

            List<string> hiddenFieldTypes = new List<string>() { "ComplexRelationLookup", "", "OwnershipLookup", "RefListRelationship" };
            var allFieldTypes = CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetTypeID).ToList();
            var fieldTypes = allFieldTypes.Where(f => !hiddenFieldTypes.Contains(f.Type)).ToList();

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_predicateuid"))
                includeRelationships = true;

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_onlylistablefields"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_onlylistablefields").Value, out includeOnlyListableFields);
                if (includeOnlyListableFields)
                {
                    fieldTypes = fieldTypes.Where(x => x.IsListable == true).ToList();
                }
            }

            var includeFieldsList = new List<string>();
            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includefields"))
            {
                try
                {
                    var includeFieldsString = queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includefields").Value;
                    includeFieldsList = includeFieldsString
                        .Split(',')
                        .Select(s => s.ToLower().Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
                catch
                {
                    throw new ArgumentException("Could not parse value of _includeFields");
                }


                //validate param values
                includeFieldsList.ForEach(f =>
                {
                    if (!allFieldTypes.Any(x => x.Name.ToLower() == f))
                    {
                        throw new ArgumentException($"Invalid value {f} in _includeFields parameter, field with this name not found.");
                    }
                });

                if (includeFieldsList.Any())
                {
                    fieldTypes = fieldTypes
                        .Where(x => includeFieldsList.Contains(x.Name.ToLower()))
                        .ToList();
                }

            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_listcolorsasjson"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_listcolorsasjson").Value, out listColorsAsJSON);
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includetotal").Value, out includeTotal);
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includecolor"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includecolor").Value, out includeColor);
            }

            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();
            List<string> whereStatements = new List<string>();
            List<string> pagingSql = new List<string>();

            var dbArgs = new DynamicParameters();
            var model = new AssetsApiViewModel();

            fieldJoins.Add("inner join AssetType T on T.ID = A.AssetTypeID");

            dbArgs.Add("@assetTypeID", assetTypeID);
            whereStatements.Add("A.AssetTypeID = @assetTypeID");

            dbArgs.Add("@userId", CompanyContext.CurrentResourceID);
            dbArgs.Add("@isAdmin", CompanyContext.CurrentResourceIsAdmin);

            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, "A.[Object]", "A.[ObjectId]", listColorsAsJSON);
            List<string> countJoins = new List<string>(fieldJoins);

            if (includeRelationships)
            {
                var subjectAlias = "B";
                var objectAlias = "A";
                string relatedAssetUIDString = "";
                Guid relatedAssetUID;

                var predicateUID = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_predicateuid").Value;
                var intersectJoin = "";
                var IntersectTypeIDField = "";
                var reverseIntersectJoin = "";
                var relatedAssetSql = " 1=1 ";
                bool includeBoth = false;
                var addtop1hint = "";


                if (queryParams.ToList().Any(q => q.Key.ToLower() == "_objectuid"))
                {
                    relatedAssetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_objectuid").Value;
                    if (Guid.TryParse(relatedAssetUIDString, out relatedAssetUID))
                    {
                        dbArgs.Add("@relatedAssetUid", relatedAssetUID);
                        relatedAssetSql = $"{objectAlias}.[UID] = @relatedAssetUid";
                    }
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and I.SubjectID = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and abs(I.ObjectID) = {subjectAlias}.ObjectID";

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
                    IntersectTypeIDField = ", I.IntersectTypeID ";
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and I.SubjectID = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and abs(I.ObjectID) = {subjectAlias}.ObjectID";
                    reverseIntersectJoin = $"I.[Subject] = {subjectAlias}.[Object] and abs(I.SubjectID) = {subjectAlias}.ObjectID and I.[Object] = {objectAlias}.[Object] and I.ObjectID = {objectAlias}.ObjectID";
                }

                var innerSql = $@"
                            select 
                                B.[UID] as AssetUid 
                                ,BD.DisplayValue
                                ,TB.[Name] as TypeName
                                ,@predicateUid as PredicateUid
                                {IntersectTypeIDField}
                            from Asset B
                            inner join AssetType TB on TB.ID = B.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(B.ID) BD
                            inner join [Intersect] I on {intersectJoin}";

                if (includeBoth == false)
                {
                    addtop1hint = " top 1 ";

                    innerSql = innerSql + $@"
                    where { relatedAssetSql }
                    and exists (select 1 from IntersectType IT 
	                inner join [Predicate] P on P.ID = IT.PredicateID 
	                where IT.ID = I.IntersectTypeID and P.[UID] = @predicateUid)";
                }

                var innerCountSql = $@"
						select {addtop1hint} B.ID as Relationships  from Asset B
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
                                B.[UID] as AssetUid 
                                ,BD.DisplayValue
                                ,TB.[Name] as TypeName
                                ,@predicateUid as PredicateUid
                                {IntersectTypeIDField}
                            from Asset B
                            inner join AssetType TB on TB.ID = B.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(B.ID) BD
                            inner join [Intersect] I on {reverseIntersectJoin}";

                    var reverseInnerCountSql = $@"
						select B.ID as Relationships from Asset B
						inner join AssetType TB on TB.ID = B.AssetTypeID
						where {relatedAssetSql}
						and exists (select 1 from [Intersect] I
							inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid
							where {reverseIntersectJoin})";

                    innerSql = $@"select AssetUid
                            ,DisplayValue 
                            ,TypeName 
                            ,PredicateUid
                    from (
                        {innerSql}
                        union all
                        {reverseInnerSql}
                        ) RI
                        where exists (select 1 from IntersectType IT 
						inner join [Predicate] P on P.ID = IT.PredicateID 
						where IT.ID = RI.IntersectTypeID and P.[UID] = @predicateUid)";

                    innerCountSql = $@"
                        select top 1 * from (
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

            var restrictions = (await CompanyContext.QueryAsync<UserGetAPIRestrictionModel>(@"select
                    case when exists(
                    select AssetID from dbo.UserAssetPermissions(@userId,@assetTypeID) where ((PermissionsBitMask & 1)) = 0)
                     then 1
                     else 0
                    end as HasAssetRestriction,
                    case when exists(
                    select 1 from AssetTypesUserCantRead(@userId) u where u.AssetTypeID = @assetTypeID)
                     then 1
                     else 0
                    end as HasAssetTypeRestriction,
                    case when exists(
                    select AssetID from dbo.UserAssetPermissions(@userId,@assetTypeID))
                     then 1
                     else 0
                    end as HasAssetPermission
                    ", new { userId = CompanyContext.CurrentResourceID, assetTypeID }
                    , ApiTimeout))
                    .FirstOrDefault();


            if ((restrictions.HasAssetRestriction && !useAsAdmin) || restrictions.HasAssetPermission)
            {
                populatePremissionAssetTableSQL = @"
                                            drop table if exists #PermissiondAssets;
                                            create table #PermissiondAssets(
	                                            AssetId int,
	                                            AssetTypeID bigint,
	                                            PermissionsBitMask int
                                            )

                                            create index cix_permissionAssetId on #PermissiondAssets(Assetid);

                                            insert into #PermissiondAssets
                                            select AssetID,AssetTypeID,PermissionsBitMask from dbo.UserAssetPermissions(@userId,@assetTypeId); ";

                if (restrictions.HasAssetRestriction && !useAsAdmin)
                {
                    whereStatements.Add($"not exists (select AssetID from #PermissiondAssets where AssetID = A.ID and ((PermissionsBitMask & 1)) = 0)");
                }
            }

            if (!CompanyContext.CurrentResourceIsAdmin && !useAsAdmin)
            {
                if (restrictions.HasAssetTypeRestriction)
                    whereStatements.Add($"not exists (select 1 from AssetTypesUserCantRead(@userId) u where u.AssetTypeID = A.AssetTypeID)");
            }
            getQueryParamsSql(model, assetType, fieldTypes, dbArgs, whereStatements, pagingSql, queryParams);

            if (assetType.Class == AssetTypeClass.FusionAttribute)
            {
                if ((await CompanyContext.Database.Connection.QueryFirstOrDefaultAsync<int>("select ISNULL(parentId,0) from fusionattributetype where id = @id", new { id = assetType.ObjectID }, commandTimeout: ApiTimeout)) > 0)
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
                    throw new ArgumentException("Invalid asset Uid in parameters!");

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

                if (!CompanyContext.TypeHasParent((SystemObjects)(Enum.Parse(typeof(SystemObjects), assetType.Object, true)), assetType.ObjectID))
                {
                    includeParent = false;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_filter"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                if (!string.IsNullOrEmpty(value))
                {
                    //Temp vars for filter expression parsing
                    //Filter expression parser uses sql definitions from getFieldSql() method
                    var tempArgs = new DynamicParameters();
                    List<string> tempJoins = new List<string>();
                    List<string> tempFieldColumns = new List<string>();
                    getFieldSql(allFieldTypes, tempArgs, tempJoins, tempFieldColumns);

                    var filterExpressionParser = new FilterExpressionParser(CompanyContext, FilterExpressionParseType.CustomFields, includeParent);
                    filterExpressionParser.LoadFieldTypes(allFieldTypes, tempFieldColumns);
                    Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                    List<int> filteredFields = new List<int>();
                    whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams, out filteredFields) + ")");

                    if (includeOnlyListableFields || includeFieldsList.Any())
                    {
                        tempArgs = new DynamicParameters();
                        tempJoins.Clear();
                        tempFieldColumns.Clear();
                        getFieldSql(allFieldTypes.Where(x => filteredFields.Contains(x.ID) && !fieldTypes.Any(f => f.ID == x.ID)).ToList(), tempArgs, tempJoins, tempFieldColumns);
                        fieldColumns.AddRange(tempFieldColumns);
                        fieldJoins.AddRange(tempJoins);
                        countJoins.AddRange(tempJoins);
                        dbArgs.AddDynamicParams(tempArgs);
                    }

                    foreach (var item in sqlParams)
                    {
                        dbArgs.Add(item.Key, item.Value);
                    }
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_relationfilter"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_relationfilter").Value;
                if (!string.IsNullOrEmpty(value))
                {
                    var filterExpressionParser = new FilterExpressionParser(CompanyContext, FilterExpressionParseType.Relationships);
                    Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                    List<int> filteredFields = new List<int>();
                    whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams, out filteredFields) + ")");

                    foreach (var item in sqlParams)
                    {
                        dbArgs.Add(item.Key, item.Value);
                    }
                }
            }

            if (restrictions.HasAssetPermission)
            {
                permissionDetailSQL = @"	outer apply (
                 select top 1 * from
				 (select PermissionsBitMask from #PermissiondAssets 
					where AssetID = A.ID 
				union all 	
					select PermissionsBitMask from #PermissiondAssets
					where AssetID = 0 and AssetTypeID = A.AssetTypeID)t
				   )Permission(mask)";

                includePermissionFields = @",(SELECT case 
					   when permission.mask is null then 1
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
            }
            else
            {
                includePermissionFields = @",(SELECT 1 'ReadAsset',
					                                 @isAdmin as 'ModifyAsset', 
					                                 @isAdmin as 'DeleteAsset'
					 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
					 ) as Permissions";
            }


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
                        if (ft.Type == DataType.Tag.ToString())
                        {
                            string simpleFilterTagSql = @"exists (select top 1 AT.TagId from AssetTag AT
						                                inner join Tag T on AT.TagId = T.Id
						                                where AT.AssetID = A.ID and T.Value like @simpleFilter)";

                            simpleFilters.Add(simpleFilterTagSql);
                        }
                        else if (ft.Type == DataType.Path.ToString())
                        {
                            simpleFilters.Add($"Node.DisplayPath like @simpleFilter");
                        }
                        else if (ft.Type == DataType.Lookup.ToString() && ft.AllowAllValue)
                        {
                            string ftformatted = (listColorsAsJSON && CompanyContext.LookupFieldHasColorItem(ft)) ? $@"JSON_VALUE(F{ft.ID}.FormattedValue, '$[0].name')" : $@"F{ft.ID}.FormattedValue";
                            simpleFilters.Add($"(select case when F{ft.ID}.[Value] = '0' then @F{ft.ID}_AllValue else {ftformatted} end as value) like @simpleFilter");
                        }
                        else if (ft.Type == DataType.Lookup.ToString() && listColorsAsJSON && CompanyContext.LookupFieldHasColorItem(ft))
                        {
                            simpleFilters.Add($"JSON_VALUE(F{ft.ID}.FormattedValue, '$[0].name') like @simpleFilter");
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
                        simpleFilters.Add($"JSON_VALUE((select top 1 * from dbo.GetAssetColorJsonByColor(A.Color)), '$.Name') like @simpleFilter");
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
                    var ownershipSQL = $@"EXISTS(
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid in @ownerUids 
                                                and 
                                                a.ID=rd.AssetID 
                                            UNION
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid in @ownerUids 
                                                and 
                                                rd.ApplyToType = 1 
                                                and 
                                                rd.AssetID = 0 
                                                and 
                                                rd.AssetTypeId=a.AssetTypeId
                                            )";
                    whereStatements.Add(ownershipSQL);
                }
            }

            var whereSql = "";
            if (whereStatements.Any())
                whereSql = $"where {string.Join(" and ", whereStatements)}";

            var fieldsSql = "";
            if (fieldColumns.Any())
                fieldsSql = $",\n {string.Join(",\n", fieldColumns)}";


            var countSql = $@"
                {populatePremissionAssetTableSQL}
                select  count(*)
                from    Asset A 
                        left join graph.AssetNodeDisplayPath Node on Node.Uid = a.uid 
                {(assetType.Object == "FusionAttributeType" ? " inner join FusionAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {(fusionAttributeWithParent ? " inner join Asset ATP on ATP.ObjectID = FA.ParentID and ATP.[Object] = 'FusionAttribute'" : "")}
                {(assetType.Object == "FusionQueryAttributeType" ? " inner join FusionQueryAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {string.Join("\n", countJoins)}
                {(includeParent ? parentApplySQL : "")}
                {whereSql}";

            var hierachy = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_ishierachyitem").Value;
            bool.TryParse(hierachy, out isHierachyItem);

            if (isHierachyItem)
            {
                hierarchyParentUidCol = " ,Parent.uid as ParentUid";
                hierarchyParentUidSelect = $@" outer apply (
					select	PA.uid 
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
							inner join Asset PA on PA.Object = I.Subject and PA.ObjectID =I.SubjectID
					) Parent ";
            }

            var sql = $@"
                {populatePremissionAssetTableSQL}
                select
                    A.ID as AssetId,
                    A.[UID] as [AssetUid],
                    A.AssetTypeId,
                    T.[UID] as AssetTypeUid,
                    A.UpdatedOn,
                    A.CreatedOn,
                    {(includeParent ? parentFieldSQL : "")}
                    {(assetType.Class == AssetTypeClass.Reference ? "A.Code, A.Icon," : "")}
                    {(includeColor ? "ACJ.ColorJson as Color," : "")}
                    {(includeSegments ? "Node.Segments," : "")}
                    KP.KeyPath as [Path]
                    {(assetType.Object == "FusionAttributeType" ? " , FA.SourceID, FA.Name, FA.TextPath" : "")} 
                    {(fusionAttributeWithParent ? " , ATP.uid as ParentUid" : "")}
                    {fieldsSql}
                    {(includePermissionDetails ? includePermissionFields : "")}
                    {hierarchyParentUidCol}
                from Asset A
                {(assetType.Object == "FusionAttributeType" ? " inner join FusionAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {(fusionAttributeWithParent ? " inner join Asset ATP on ATP.ObjectID = FA.ParentID and ATP.[Object] = 'FusionAttribute'" : "")}
                {(assetType.Object == "FusionQueryAttributeType" ? " inner join FusionQueryAttribute FA on FA.ID = A.ObjectID and FA.Deleted = 0" : "")} 
                {string.Join("\n", fieldJoins)}
                left join graph.AssetNodeDisplayPath Node on Node.ID = a.ID 
                left join graph.AssetNodeKeyPath KP on KP.ID = a.ID 
                {(includeColor ? "cross apply dbo.GetAssetColorJsonByColor(A.Color) ACJ" : "")}
                {(includePermissionDetails ? permissionDetailSQL : "")}
                {hierarchyParentUidSelect}
                {(includeParent ? parentApplySQL : "")}
                {whereSql}
                {string.Join("\n", pagingSql)}
            ";

            if (includeTotal)
            {
                model.total = await CompanyContext.QueryFirstOrDefaultAsync<int>(countSql, dbArgs, ApiTimeout);                
            }

            var results = await CompanyContext.QueryAsync(sql, dbArgs, ApiTimeout);

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
            
            return model;
        }

        public async Task<AssetPathResults> GetAssetPaths(AssetType assetType, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();

            dbArgs.Add("@assetTypeId", assetType.ID);

            int pageSize = 5000;
            int pageNum = 0;
            bool includeTotal = true;

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagesize"))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value, out int res))
                {
                    pageSize = res;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagenum"))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value, out int res))
                {
                    pageNum = res - 1;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includetotal"))
            {
                if (bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includetotal").Value, out bool res))
                {
                    includeTotal = res;
                }
            }


            dbArgs.Add("@assetTypeUid", assetType.uid);
            dbArgs.Add("@pageNum", pageNum);
            dbArgs.Add("@pageSize", pageSize);

            var countSql = $@"select count(*) 
                from	graph.AssetNodeKeyPath P
		                inner join Asset A on A.ID = P.ID
		                inner join AssetType T on T.id = A.AssetTypeID
                where T.uid = @assetTypeUid";

            var sql = $@"
                select	P.[uid],
		                P.[keypath] as [path]  
                from	graph.AssetNodeKeyPath P
		                inner join Asset A on A.ID = P.ID
		                inner join AssetType T on T.id = A.AssetTypeID
                where T.uid = @assetTypeUid
                order by A.ID
                OFFSET @pageSize*@pageNum ROWS FETCH NEXT @pageSize ROWS ONLY";

            int? total = null;
            if (includeTotal)
            {
                total = (await CompanyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout)).FirstOrDefault();
            }

            var results = await CompanyContext.QueryAsync<AssetPathResult>(sql, dbArgs, ApiTimeout);

            return new AssetPathResults
            {
                items = results,
                total = total
            };
        }

        public async Task<SLDocument> GetAssetsExcel(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var assetType = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == uid);
            var results = await GetAssets(assetType, queryParams);
            
            var fields = new List<FieldType>();

            bool includeAssetUrl = true;
            bool includeParent = false;
            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includeparent"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includeparent").Value;
                bool.TryParse(value, out includeParent);
            }
            var hierarchy = CompanyContext.IntersectTypes
                .FirstOrDefault(x => x.Object == assetType.Object && x.ObjectID == assetType.ObjectID && x.Predicate.Type == PredicateType.InterTypeHierarchy);

            if (hierarchy == null)
            {
                includeParent = false;
            }
            var typesToAvoid = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.OwnershipLookup.ToString()
            };

            //add default fields
            if (assetType.Class == AssetTypeClass.Reference)
            {
                fields.Add(new FieldType { Type = "string", Name = "Code", FriendlyName = "Code" });
                fields.Add(new FieldType { Type = "string", Name = "Color", FriendlyName = "Color" });
                includeAssetUrl = false;
            }

            if (includeParent)
            {
                var columnName = "Parent";
                if (assetType.Class == AssetTypeClass.Reference && hierarchy != null)
                {
                    var parent = CompanyContext.AssetTypes.FirstOrDefault(x => x.Object == hierarchy.Subject && x.ObjectID == hierarchy.SubjectID);
                    if (parent != null)
                        columnName = parent.Name;
                }

                fields.Add(new FieldType { Type = "string", Name = "ParentDisplayName", FriendlyName = columnName });
            }

            fields.AddRange(CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetType.ID).OrderBy(x => x.ColumnOrder).ThenBy(x => x.FriendlyName).ToList());

            fields.Add(new FieldType { Type = "string", Name = "AssetUid", FriendlyName = "Asset UID" });
            fields.Add(new FieldType { Type = "number", Name = "AssetId", FriendlyName = "Asset ID" });

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
            document.SetCellValue(3, 2, (int)results.total);


            document.SelectWorksheet(assetSheetName);

            int index = 1;

            foreach (var field in fields)
            {
                if (typesToAvoid.Contains(field.Type))
                    continue;
                document.SetCellValue(1, index++, (string)field.FriendlyName);
            }
            if (includeAssetUrl)
                document.SetCellValue(1, index++, "Url");


            if (rowData == null || rowData.Count == 0)
            {
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

                        if (field.Name == "Color")
                        {
                            string val = extractColorNameFromJSON((string)rowValues[field.Name]);
                            setCellValueFromField(document, rowNumber, index, field, val);
                        }
                        else
                        {
                            var val = rowValues[field.Name];
                            setCellValueFromField(document, rowNumber, index, field, val);
                        }

                    }

                    index++;
                }

                if (includeAssetUrl)
                    document.SetCellValue(rowNumber, index, $"asset/{rowValues["AssetUid"]}");
            }


            SetExcelColumnWidths(document, fields);
            #endregion

            return document;
        }

        public async Task<SLDocument> GetHierarchyExcel(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams, bool stripHtml = false)
        {
            IQueryable<dynamic> levels = null;
            IEnumerable<dynamic> results = null;
            List<dynamic> assetUids = null;
            IEnumerable<dynamic> allResults = null;
            AssetType assetType = null;
            var fields = new List<FieldType>();
            var tempFields = new List<FieldType>();
            var fieldsToRemove = new List<FieldType>();

            string filter = "";
            if (queryParams.Any(p => p.Key.Trim().ToLower() == "_simplefilter"))
                filter = queryParams.ToList().First(k => k.Key.ToLower() == "_simplefilter").Value.ToString();

            string orderValue = "";
            int orderID = 0;
            if (queryParams.Any(p => p.Key.Trim().ToLower() == "_order"))
            {
                orderValue = queryParams.ToList().First(k => k.Key.ToLower() == "_order").Value.ToString();
                if (orderValue != "undefined")
                {
                    orderValue = orderValue.Split(new[] { "Field" }, StringSplitOptions.None)[1];
                    orderID = int.Parse(orderValue);
                }
            }

            List<KeyValuePair<string, string>> queryParamsWithOrder = new List<KeyValuePair<string, string>>();
            if (orderID != 0)
            {
                var orderName = CompanyContext.FieldTypes.Where(f => f.ID == orderID).FirstOrDefault().FriendlyName;
                queryParamsWithOrder.AddRange(queryParams);
                queryParamsWithOrder.RemoveAll(x => x.Key == "_order");
                queryParamsWithOrder.Add(new KeyValuePair<string, string>("_order", orderName));
            }
            else
            {
                queryParamsWithOrder.AddRange(queryParams);
            }

            var typesToAvoid = new List<string>() {
                DataType.OwnershipLookup.ToString(),
                DataType.ComplexRelationLookup.ToString()
            };

            assetType = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == uid);
            var id = assetType.ObjectID;
            var data = await GetAssets(assetType, queryParamsWithOrder);
            fields.AddRange(CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetType.ID).OrderBy(x => x.ColumnOrder).ThenBy(x => x.FriendlyName).ToList());
            results = data.items;
            if (assetType.Class == AssetTypeClass.Policy)
            {
                levels = GetPolicyTypeLevels(id);
            }

            if (assetType.Class == AssetTypeClass.Model)
            {
                levels = GetTaxonomyTypeLevels(id);
            }
            List<KeyValuePair<string, string>> qp = new List<KeyValuePair<string, string>>();

            if (!String.IsNullOrEmpty(filter))
            {
                assetUids = results.Select(x => x.AssetUid).ToList();
                var allFamily = GetAllFamilyForAssetUid(assetUids).Distinct().ToList();

                if (allFamily.Count > 0)
                {
                    var par = queryParamsWithOrder.Where(k => k.Key.ToLower() != "_simplefilter");
                    qp.AddRange(par);
                    qp.Add(new KeyValuePair<string, string>("_assetUid", string.Join(",", allFamily)));
                    var fammilyAssets = await GetAssets(assetType, qp);
                    allResults = results.Union(fammilyAssets.items);
                }
                else
                {
                    allResults = results;
                }
            }
            else
            {
                allResults = results;
            }

            var document = new SLDocument();
            const string assetSheetName = "Assets";
            const string apiSheetName = "Api Info";
            int maxDepth = 1;

            #region Populate Excel Document

            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, assetSheetName);

            document.AddWorksheet(apiSheetName);
            document.SelectWorksheet(apiSheetName);

            document.SetCellValue(1, 1, "pageSize");
            document.SetCellValue(1, 2, 1);
            document.SetCellValue(2, 1, "pageNum");
            document.SetCellValue(2, 2, 1);
            document.SetCellValue(3, 1, "total");
            document.SetCellValue(3, 2, allResults.Select(m => m.AssetUid).Distinct().Count());


            document.SelectWorksheet(assetSheetName);

            foreach (var row in allResults)
            {
                int depth = CheckDepth(allResults, row.AssetUid);
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
            }

            int index = 1;
            for (int i = 1; i < maxDepth + 1; i++)
            {
                foreach (var field in fields)
                {
                    if (field.IsPartOfKey)
                    {
                        fieldsToRemove.Add(field);

                        string levelName = "Level " + i.ToString();
                        foreach (var level in levels)
                        {
                            if ((int)level.Level == i)
                            {
                                levelName = level.Name;
                            }
                        }
                        tempFields.Add(new FieldType { Type = "string", Name = $"{(string)field.Name}", FriendlyName = $"{levelName} {(string)field.FriendlyName}", ID = field.ID, IsPartOfKey = field.IsPartOfKey });

                    }
                }
            }
            tempFields.AddRange(fields);
            for (int i = 1; i < maxDepth + 1; i++)
            {
                string levelName = "Level " + i.ToString();
                foreach (var level in levels)
                {
                    if ((int)level.Level == i)
                    {
                        levelName = level.Name;
                    }
                }
                tempFields.Add(new FieldType { Type = "string", Name = $"AssetUid", FriendlyName = $"{levelName} UID", IsPartOfKey = true });
            }
            tempFields.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "URL" });
            fields = tempFields;
            foreach (var field in fieldsToRemove)
                fields.Remove(field);


            foreach (var field in fields)
            {
                if (typesToAvoid.Contains(field.Type))
                    continue;
                document.SetCellValue(1, index, (string)field.FriendlyName);
                index++;
            }


            int rowNumber = 1;
            List<Guid> used = new List<Guid>();
            foreach (var row in allResults.Where(x => x.ParentUid == null).ToList())
            {
                if (used.Contains(row.AssetUid))
                {
                    continue;
                }
                rowNumber++;
                (int, List<Guid>) tuple = AddRow(allResults, document, fields, rowNumber, row, maxDepth, used);
                (rowNumber, used) = tuple;
            }

            #endregion
            SetExcelColumnWidths(document, fields);
            return document;
        }

        private (int, List<Guid>) AddRow(IEnumerable<dynamic> policies, SLDocument document, List<FieldType> fields, int rowNumber, dynamic row, int maxDepth, List<Guid> used)
        {
            if (!used.Contains(row.AssetUid))
            {
                var rowValues = (row as IDictionary<string, object>);
                int itemDepth = CheckDepth(policies, row.AssetUid);
                int index = 1;
                int level = 1;
                var typesToAvoid = new List<string>() {
                DataType.OwnershipLookup.ToString(),
                DataType.ComplexRelationLookup.ToString()
            };
                var keyFields = fields.Where(x => x.IsPartOfKey && x.ID > 0).GroupBy(x => x.ID).Select(x => x.First()).ToList();
                for (int currentLevel = 1; currentLevel <= maxDepth; currentLevel++)
                {
                    if (currentLevel == itemDepth)
                    {
                        foreach (var field in keyFields)
                        {
                            if (rowValues.ContainsKey(field.Name))
                            {
                                var val = rowValues[field.Name];
                                setCellValueFromField(document, rowNumber, index, field, val);
                            }
                            else if (rowValues.ContainsKey("Field" + field.ID))
                            {
                                var val = rowValues["Field" + field.ID];
                                setCellValueFromField(document, rowNumber, index, field, val);
                            }
                            index++;
                        }
                    }
                    else
                    {
                        var parent = policies.FirstOrDefault(x => x.AssetUid == row.ParentUid);
                        while (parent != null && CheckDepth(policies, parent.AssetUid) != level)
                        {
                            parent = policies.FirstOrDefault(x => x.AssetUid == parent.ParentUid);
                        }
                        if (parent != null)
                        {
                            foreach (var field in keyFields)
                            {
                                var parentRowValue = (parent as IDictionary<string, object>);
                                if (parentRowValue.ContainsKey(field.Name))
                                {
                                    var val = parentRowValue[field.Name];
                                    setCellValueFromField(document, rowNumber, index, field, val);
                                }
                                else if (parentRowValue.ContainsKey("Field" + field.ID))
                                {
                                    var val = parentRowValue["Field" + field.ID];
                                    setCellValueFromField(document, rowNumber, index, field, val);
                                }
                                index++;
                            }
                        }
                        else
                        {
                            index += keyFields.Count;
                        }
                    }
                    level++;
                }
                var uidCounterLevel = 1;
                List<object> writtenUids = new List<object>();
                foreach (var field in fields.Where(x => !keyFields.Select(y => y.ID).Contains(x.ID)))
                {
                    if (typesToAvoid.Contains(field.Type))
                        continue;
                    if (field.Name == "AssetUid")
                    {
                        var parent = policies.FirstOrDefault(x => x.AssetUid == row.ParentUid);
                        while (parent != null && CheckDepth(policies, parent.AssetUid) != uidCounterLevel)
                        {
                            parent = policies.FirstOrDefault(x => x.AssetUid == parent.ParentUid);
                        }
                        if (parent != null && parent.AssetUid != row.AssetUid)
                        {
                            var parentRowValue = (parent as IDictionary<string, object>);
                            var val = parentRowValue[field.Name];
                            if (!writtenUids.Contains(val))
                                setCellValueFromField(document, rowNumber, index, field, val);
                            writtenUids.Add(val);
                        }
                        else
                        {
                            var val = rowValues[field.Name];
                            if (!writtenUids.Contains(val))
                                setCellValueFromField(document, rowNumber, index, field, val);
                            writtenUids.Add(val);
                        }
                        uidCounterLevel++;
                    }
                    else if (rowValues.ContainsKey(field.Name))
                    {
                        var val = rowValues[field.Name];
                        setCellValueFromField(document, rowNumber, index, field, val);
                    }
                    else if (rowValues.ContainsKey("Field" + field.ID))
                    {
                        var val = rowValues["Field" + field.ID];
                        setCellValueFromField(document, rowNumber, index, field, val);
                    }
                    else if (field.Name == "Url")
                    {
                        var val = "asset/" + row.AssetUid;
                        setCellValueFromField(document, rowNumber, index, field, val);
                    }
                    used.Add(row.AssetUid);
                    index++;
                }
                //check kids
                var children = policies.Where(x => x.ParentUid == row.AssetUid);
                foreach (var child in children)
                {
                    rowNumber++;
                    (int, List<Guid>) tuple = AddRow(policies, document, fields, rowNumber, child, maxDepth, used);
                    (rowNumber, used) = tuple;
                }
            }
            else
            {
                rowNumber--;
            }
            return (rowNumber, used);
        }

        private int CheckDepth(IEnumerable<dynamic> tree, Guid itemID, int level = 1)
        {
            var item = tree.Where(x => x.AssetUid == itemID).FirstOrDefault();
            if (item != null && item.ParentUid != null)
            {
                level = CheckDepth(tree, item.ParentUid, ++level);
            }
            return level;
        }

        private IQueryable<dynamic> GetPolicyTypeLevels(int id)
        {
            return CompanyContext.Query<dynamic>($@"Select AT.ObjectId as PolicyTypeID,ATL.Level,ATL.Name,ATL.Description
                                            From AssetTypeLevel ATL
                                            inner join AssetType AT on AT.Id = ATL.AssetTypeID
                                            WHERE  [object]='PolicyType' and ObjectId=@ObjectId
                                            order by Level", new { ObjectId = id }, ApiTimeout).AsQueryable();
        }

        private IQueryable<dynamic> GetTaxonomyTypeLevels(int id)
        {
            return CompanyContext.Query<dynamic>(@"Select AT.ObjectId as TaxonomyTypeID,ATL.Level,ATL.Name,ATL.Description
                                            From AssetTypeLevel ATL
                                            inner join AssetType AT on AT.Id = ATL.AssetTypeID
                                            WHERE  [object]='TaxonomyType' and ObjectId=@ObjectId
                                            order by Level", new { ObjectId = id }, ApiTimeout).AsQueryable();
        }

        private List<Guid> GetAllFamilyForAssetUid(List<dynamic> uids)
        {
            var sql = $@"drop table if exists #family
                create table #family(
                 AssetUid uniqueidentifier
                )
                --GET ALL CHILDREN
                ;with family_cte as (
                select a1.uid,ADV.DisplayValue
                from graph.assetnode an
                inner join graph.AssetEdge edge1 on edge1.$from_id = an.$node_id and edge1.PredicateType = 4
                inner join graph.AssetNode rel1 on rel1.$node_id = edge1.$to_id
                inner join asset a1 on a1.uid = rel1.Uid
                cross apply GetAssetDisplayValueById(a1.ID)ADV
                where an.Uid in @assetUid
                union all
                select a1.uid, ADV.DisplayValue
                from family_cte fam, graph.assetnode an
                inner join graph.AssetEdge edge1 on edge1.$from_id = an.$node_id and edge1.PredicateType = 4
                inner join graph.AssetNode rel1 on rel1.$node_id = edge1.$to_id
                inner join asset a1 on a1.uid = rel1.Uid
                cross apply GetAssetDisplayValueById(a1.ID)ADV
                where an.Uid = fam.uid)
                insert into #family 
                select 
                uid as AssetUid from family_cte
                --GET ALL PARENT
                ;with family_cte as (
                select a2.uid,ADV.DisplayValue
                from graph.assetnode an
                inner join graph.AssetEdge edge2 on edge2.$to_id = an.$node_id and edge2.PredicateType = 4
                inner join graph.AssetNode rel2 on rel2.$node_id = edge2.$from_id
                inner join asset a2 on a2.uid = rel2.Uid
                cross apply GetAssetDisplayValueById(a2.ID)ADV
                where an.Uid in @assetUid
                union all
                select a2.uid, ADV.DisplayValue
                from family_cte fam, graph.assetnode an
                inner join graph.AssetEdge edge2 on edge2.$to_id = an.$node_id and edge2.PredicateType = 4
                inner join graph.AssetNode rel2 on rel2.$node_id = edge2.$from_id
                inner join asset a2 on a2.uid = rel2.Uid
                cross apply GetAssetDisplayValueById(a2.ID)ADV
                where an.Uid = fam.uid)
                insert into #family 
                select 
                uid as AssetUid from family_cte
                select * from #family";

            return CompanyContext.Query<Guid>(sql, new { assetUid = uids }, ApiTimeout).AsList();
        }


        private string extractColorNameFromJSON(string jsonString)
        {
            if (!string.IsNullOrEmpty(jsonString))
            {
                var colorObj = JObject.Parse(jsonString);
                return (string)colorObj["Name"] ?? "";
            }
            return "";
        }

        public async Task<AssetsByPathApiViewModel> GetAssetsByPath(AssetsByPathApiRequestModel model)
        {
            var dbArgs = new DynamicParameters();
            var returnModel = new AssetsByPathApiViewModel();

            var prefilterSql = "";

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

            dbArgs.Add("@phrase", "%" + model.searchPhrase.CleanForSql() + "%");

            if (!string.IsNullOrEmpty(prefilterSql))
            {
                prefilterSql = $"and N.AssetTypeID in ({prefilterSql})";
            }

            var countSql = $@"
select	count(1)
from	graph.AssetNode N
        inner join graph.AssetNodeDisplayPath P on P.ID = N.ID
where	P.DisplayPath like @phrase {prefilterSql}
";

            var sql = $@"
select	N.Uid,
		N.AssetTypeUid,
        T.Name as AssetTypeName,
        coalesce(S.Icon, 'fa-book') as AssetTypeIcon, 
		N.Segments as SegmentsXml
from	graph.AssetNode N
        inner join graph.AssetNodeDisplayPath P on P.ID = N.ID
        inner join AssetType T on T.ID = N.AssetTypeID
        left join AssetTypeStyle S on S.ID = T.ID
where	P.DisplayPath like @phrase {prefilterSql}
order by P.DisplayPath asc
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

            var count = await CompanyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
            var total = count.First();
            var results = await CompanyContext.QueryAsync<AssetsByPathItemApiViewModel>(sql, dbArgs, ApiTimeout);

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
                i.Type,
                i.Name
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
                        CanOwnFusion = model.CanOwnFusion ?? false,
                        Parent = parentAssetType,
                        AutoDisplayParent = model.AutoDisplayParent
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
                        orgAssetType.AutoDisplayDescription = model.AutoDisplayDescription;
                        orgAssetType.Notes = model.Notes;
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
                case AssetTypeClass.Diagram:
                    #region
                    var diagram = new AssetType
                    {
                        uid = uid,
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description,
                        Object = SystemObjects.TaskType.ToString(),
                        State = State.Active,
                        UpdatedBy = resourceId,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = resourceId,
                        CreatedOn = DateTime.UtcNow,
                        Hierarchical = true,
                        Class = model.Class,
                        AutoDisplayDescription = model.AutoDisplayDescription,
                        UseAsTransformation = model.UseAsTransformation,
                        CanOwnFusion = model.CanOwnFusion ?? false,
                        Parent = parentAssetType,
                        AutoDisplayParent = model.AutoDisplayParent,
                        FlowObjectType = model.FlowObjectType
                    };
                    CompanyContext.Add(diagram);
                    parentType = SystemObjects.TaskType;
                    model.ObjectID = diagram.ObjectID;
                    model.Object = SystemObjects.TaskType.ToString();

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

            bool shouldRemoveOldRelationshipType = (model.Class == AssetTypeClass.Reference || model.ParentUid == Guid.Empty);

            if (!string.IsNullOrEmpty(model?.Name ?? null))
                model.Name = model.Name.Trim();

            switch (model.Class)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.Policy:
                case AssetTypeClass.Reference:
                case AssetTypeClass.Model:
                case AssetTypeClass.TechnicalAsset:
                case AssetTypeClass.Diagram:
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
                    assetType.DisplayFormat = model.DisplayFormat ?? assetType.DisplayFormat;
                    assetType.Description = model.Description;
                    assetType.HierarchyMaximumDepth = (model.Hierarchy != null) ? model.Hierarchy.MaximumDepth : 1;
                    assetType.AutoDisplayDescription = model.AutoDisplayDescription;
                    assetType.AutoDisplayParent = model.AutoDisplayParent;
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

                    if (model.Class == AssetTypeClass.Diagram)
                    {
                        assetType.FlowObjectType = model.FlowObjectType;
                    }

                    #endregion
                    break;
                case AssetTypeClass.Organization:
                    #region

                    var org = CompanyContext.GetById<OrganizationType>(model.ObjectID);
                    if (org == null) return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Organization.ToString()}", $"Invalid {AssetTypeClass.Organization.ToString()} provided. {AssetTypeErrors.CheckRequest}");
                    org.Name = model.Name;
                    org.Description = model.Description;
                    org.DisplayFormat = model.DisplayFormat ?? assetType.DisplayFormat;
                    CompanyContext.Update(org);

                    //also update asset type record
                    assetType.Name = model.Name;
                    assetType.Description = model.Description;
                    assetType.DisplayFormat = model.DisplayFormat ?? assetType.DisplayFormat;
                    assetType.AutoDisplayDescription = model.AutoDisplayDescription;
                    assetType.Notes = model.Notes ?? assetType.Notes;

                    #endregion
                    break;
                case AssetTypeClass.Rule:
                    #region

                    var r = CompanyContext.GetById<RuleType>(model.ObjectID);
                    if (r == null) return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Rule.ToString()}", $"Not valid {AssetTypeClass.Rule.ToString()} provided. {AssetTypeErrors.CheckRequest}");
                    r.Name = model.Name;
                    r.DisplayFormat = model.DisplayFormat ?? assetType.DisplayFormat;
                    r.Description = model.Description;
                    CompanyContext.Update(r);

                    assetType.Name = model.Name;
                    assetType.DisplayFormat = model.DisplayFormat ?? assetType.DisplayFormat;
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
            try
            {
                CompanyContext.Update(assetType);
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }

            return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.OK, "", "");
        }

        public List<DatabaseBulkAssetResult> DeleteAsset(AssetDeletes assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.RemoveAssets(execution, assetType, assets, sendWorkflowEvents: sendWorkflowEvents, sendGraphEvents: false);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);

                CompanyContext.SendApiGraphEvent(new ApiExecutionInfo
                {
                    ExecutionID = execution.ExecutionID,
                    Action = ApiExecutionAction.DeleteAssets,
                    CompanyID = CompanyContext.CurrentCompanyID
                });

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

            return await CreateApiBatchJob(executionInfo, execution, assetTypes);
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

            return await CreateApiBatchJob(executionInfo, execution, assets);
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

            return await CreateApiBatchJob(executionInfo, execution, assets);
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

            return await CreateApiBatchJob(executionInfo, execution, assets);
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
        public AssetType GetArtifactTypeByID(int artifactTypeId)
        {
            return CompanyContext.Filter<AssetType>(i => i.Object.Equals("ArtifactType") && i.ObjectID == artifactTypeId).SingleOrDefault();
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
            var executions = await CompanyContext.QueryAsync<dynamic>(sql, ApiTimeout);
            var count = await CompanyContext.QueryAsync<int>(countSQL, ApiTimeout);

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
                    COALESCE(StatusColor.FormattedValue, f.FormattedValue, ft.DefaultFormattedValue) as Status,
	                KP.KeyPath as Path
                from Asset A
                inner join AssetType AT on AT.ID = A.AssetTypeID and AT.UID = @typeUid
                left join FieldType ft on AT.Object = ft.Object and AT.ObjectID = ft.ObjectID and ft.FriendlyName like 'status'
                left Join Field f on f.FieldTypeID = ft.ID and f.AssetID = A.ID
                left join graph.AssetNode Node on Node.Uid = a.uid and Node.AssetTypeUid = AT.[UID]
                left join graph.AssetNodeKeyPath KP on KP.ID = Node.ID
				outer apply(
                                select FormattedValue = 
                                (SELECT F.FormattedValue as name,
                                COALESCE(JSON_VALUE(ACJF.ColorJSON,'$.Value'), 'transparent') as color FOR JSON PATH) 
								FROM Asset ACF    
								cross apply dbo.GetAssetColorJsonByColor(ACF.Color) ACJF
								WHERE ACF.Object = ft.LookupObjectType and ACF.ObjectID = TRY_PARSE(F.Value as int)
                            )StatusColor(FormattedValue)
                WHERE A.ID = @id
            ";
            var res = new
            {
                AssetDetail = await CompanyContext.QueryFirstOrDefaultAsync<dynamic>(sql, dbArgs, ApiTimeout),
                Scores = await GetAssetScores(asset.uid)
            };
            return res;
        }

        public async Task<string[][]> GetAssetPath(Guid assetUid)
        {
            var dbArgs = new DynamicParameters();
            dbArgs.Add("@assetUid", assetUid.ToString());

            var sql = $@"SELECT	Segments FROM graph.AssetNode WHERE Uid = @assetUid";
            string segment = await CompanyContext.QueryFirstOrDefaultAsync<string>(sql, dbArgs, ApiTimeout);
            return GetPathFromSegments(segment);
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

            return await CompanyContext.QueryFirstOrDefaultAsync<dynamic>(sql, dbArgs, ApiTimeout);
        }

        private async Task<IEnumerable<dynamic>> GetAssetScores(Guid AssetUid)
        {
            var scoreSQL = @"
select	*
from	(
		select  S.AssetUid,
				S.EffectiveDate,
				S.EndDate,
				S.RunDate,
				case 
					when AL.ScoreType = 1 then 'Governance'
					when AL.ScoreType = 2 then 'DataQuality'
				end as ScoreType,
				ROW_NUMBER() OVER(PARTITION BY AL.ScoreType ORDER BY S.EffectiveDate DESC) as RowNum,
				S.Value, 
				AL.LowerThreshold, 
				AL.UpperThreshold 
		from    metrics.Score S
				inner join Asset A on A.Uid = S.AssetUid and S.AssetUid = @assetUid and S.EffectiveDate <= @date and S.EndDate is null 
				inner join metrics.Allocation AL on AL.Uid = S.AllocationUid
		) O
where	O.RowNum = 1";

            return await CompanyContext.QueryAsync<dynamic>(scoreSQL, new { assetUid = AssetUid, date = DateTime.UtcNow }, ApiTimeout);
        }

        
        public async Task<AssetsCountModel> GetAssetsCounts()
        {
            var results = new AssetsCountModel();

            var includedAssetClasses = new List<AssetTypeClass>() {
                AssetTypeClass.BusinessAsset,
                AssetTypeClass.Diagram,
                AssetTypeClass.Fusion,
                AssetTypeClass.FusionAttribute,
                AssetTypeClass.FusionQuery,
                AssetTypeClass.Group,
                AssetTypeClass.Model,
                AssetTypeClass.Organization,
                AssetTypeClass.Policy,
                AssetTypeClass.Reference,
                AssetTypeClass.Rule,
                AssetTypeClass.TechnicalAsset,
                AssetTypeClass.User
            };
                        
            //total asset count
            results.totalNumberOfAssets = await CompanyContext.QueryFirstOrDefaultAsync<int>("select count(1) from asset a inner join assettype att on a.assetTypeId = att.id where att.class in @includedClassTypes", new { includedClassTypes = includedAssetClasses });

            results.countsByAssetClass = new List<AssetClassCountModel>();

            var allCounts = await CompanyContext.QueryAsync<dynamic>("select class as [assetTypeClass], count(1) as [cnt] from asset a inner join assettype att on a.assetTypeId = att.id group by att.class");

            foreach (var assetType in includedAssetClasses)
            {
                var info = allCounts.FirstOrDefault(x => x.assetTypeClass == (int)assetType);

                results.countsByAssetClass.Add(new AssetClassCountModel()
                {
                    @class = assetType.ToString(),
                    numberOfAssets = info == null ? 0 : info.cnt
                });
            }

            return results;
        }

        public async Task<IEnumerable<AssetTypeCountModel>> GetAssetTypeCounts(int[] filterClasses)
        {

            string assetPermissionWhere = @" and ID NOT IN (select AssetId 
                        from dbo.UserAssetPermissions(@resourceId,AT.Id) where ((PermissionsBitMask & 1)) = 0
                        )";

            string assetTypePermissionWhere = @" and AT.ID not in (select AssetTypeID
                    from dbo.AssetTypesUserCantRead(@ResourceID))";

            if (CompanyContext.CurrentResourceIsAdmin)
            {
                assetTypePermissionWhere = "";
            }

            var countsSQL = $@"select AT.uid, 
	                        ATParent.uid as parentUid,
	                        case at.class
	                         when 1 then 'Business Asset'
	                         when 8 then 'Technical Asset'
	                         when 2 then 'Model'
	                         when 6 then 'Policy'
	                         when 7 then 'Rule'
                             when 15 then 'Diagram'
	                        end as class,
	                        at.name,
	                        at.description,
	                        Assets.count as count
                         from AssetType AT
						 outer apply (select ATParent.uid from IntersectType IT
							inner join [Predicate] P on P.ID = it.PredicateID and P.Type in (3,4)
							inner join [AssetType] ATParent on ATParent.Object = IT.Subject AND ATParent.ObjectID = IT.SubjectID
						 where it.ObjectID = AT.ObjectID and it.Object = at.Object
						 )ATParent
                         outer apply (select count(*) from Asset where AssetTypeID = AT.ID {assetPermissionWhere})Assets(count)
                        where
                         at.Class in @filterClasses
                         {assetTypePermissionWhere}
                    order by at.name";
            return await CompanyContext.QueryAsync<AssetTypeCountModel>(countsSQL, new { ResourceId = CompanyContext.CurrentResourceID, filterClasses }, ApiTimeout);
        }

        public async Task<dynamic> GetAssetTypeObjectAndObjectId(Guid uid)
        {
            return await CompanyContext.QueryAsync<dynamic>("select Object, ObjectID, Id as AssetTypeID from assettype where uid = @uid", new { uid }, ApiTimeout);
        }

        public async Task<dynamic> GetExecutionStatusModel(Guid executionUid, bool includeResults = true)
        {
            ApiExecution dbExecutionItem = GetExecutionItemByUid(executionUid);

            if (dbExecutionItem == null)
            {
                throw new ArgumentException("Execution unique identifier not found.");
            }

            var info = new ApiExecutionInfo { CompanyID = CompanyContext.CurrentCompanyID, ExecutionID = executionUid };

            List<DatabaseBulkAssetResult> results = null;
            bool finished = (dbExecutionItem.Processed + dbExecutionItem.Error) == dbExecutionItem.Total;

            if (includeResults && finished)
            {
                try
                {
                    results = await StorageProvider.DeserializeJsonObjectFromBlobAsync<List<DatabaseBulkAssetResult>>(info.StorageFolder, info.ResponseFileName);
                }
                catch
                {
                }
            }

            var f = string.IsNullOrEmpty(dbExecutionItem.Fields) ? "{}" : dbExecutionItem.Fields;

            return new
            {
                Total = dbExecutionItem.Total,
                Processed = dbExecutionItem.Processed,
                Error = dbExecutionItem.Error,
                Fields = Newtonsoft.Json.Linq.JObject.Parse(f),
                StartedOn = dbExecutionItem.StartedOn,
                CompletedOn = dbExecutionItem.CompletedOn,
                Results = results
            };
        }

        public List<DatabaseBulkAssetTypeResult> DeleteSingleAssetType(AssetTypeDeletes assetTypes, AssetType assetType, ApiExecution execution)
        {
            if (assetTypes.Count > 1)
                throw new ArgumentException("Maximum number of asset types for this method is 1.");

            CompanyContext.Add(execution);
            List<DatabaseBulkAssetTypeResult> results = null;
            try
            {
                var deletes = new AssetTypeDeletes();
                results = CompanyContext.RemoveAssetTypes(execution, assetTypes, 28800); //dbExecutionTimeout = 8 hours
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

        public List<ValidationError> ValidateAssetUpsertModel(List<UpsertModel> model)
        {
            List<ValidationError> errors = new List<ValidationError>();
            foreach (var item in model)
            {
                var assetType = GetAssetTypeByUID(item.AssetTypeUid);
                if (assetType == null)
                {
                    errors.Add(new ValidationError() { Error = "Asset Type not found.", AssetTypeUid = item.AssetTypeUid });
                }

                foreach (var asset in item.Assets)
                {
                    bool success = true;
                    string error = "";
                    var fieldTypes = CompanyContext.FieldTypes.Where(x => x.AssetTypeID == assetType.ID).ToList();
                    CompanyContext.ValidateFields(assetType.Object,
                        assetType.ObjectID,
                        true,
                        fieldTypes,
                        fieldTypes.Where(x => x.IsRequired == true || x.IsPartOfKey).Select(x => x.Name).ToList(),
                        asset.Fields,
                        Guid.Empty, 0,
                        null,
                        out success,
                        out error,
                        true,
                        true
                        );
                    if (!success)
                        errors.Add(new ValidationError() { AssetName = asset.Fields["Name"], Error = error.Trim().Trim('.'), AssetTypeUid = item.AssetTypeUid, AssetUid = asset.ExternalKey ?? Guid.Empty });

                }

            }
            return errors;
        }


        public async Task<dynamic> GetAssetSingle(Guid assetUid)
        {

            var asset = GetAssetByUID(assetUid);

            if (asset == null)
                return null;

            var canRead = CompanyContext.HasAssetPermission(asset.ID, Permission.ReadAsset);

            if (!canRead)
                return null;


            var assetType = CompanyContext.Filter<AssetType>(a => a.ID == asset.AssetTypeID).FirstOrDefault();
            var fieldTypes = CompanyContext.Filter<FieldType>(f => f.AssetTypeID == asset.AssetTypeID).ToList();
            var fieldJoins = new List<string>();
            var fieldColumns = new List<string>();
            DynamicParameters dbArgs = new DynamicParameters();
            dbArgs.Add("@assetUid", assetUid);

            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);


            var sql = $@"
select  A.ID as AssetId,
        A.[uid] as AssetUid,
        A.AssetTypeId,
        T.[uid] as AssetTypeUid,
        P.[uid] as ParentAssetUid,
        P.DisplayValue as ParentDisplayName,
        A.CreatedOn,
        A.UpdatedOn,
        ACJ.ColorJson as Color,
        {(assetType.Class == AssetTypeClass.Reference ? "A.Code, A.Icon," : "")}
        {(assetType.Class == AssetTypeClass.Rule ? "R.Threshold," : "")}
        KP.KeyPath as [Path] {(fieldColumns.Any() ? "," : "")}
        {string.Join(",\n", fieldColumns)}
from    Asset A
        inner join AssetType T on T.ID = A.AssetTypeID
        left join graph.AssetNodeDisplayPath Node on Node.ID = a.ID 
        left join graph.AssetNodeKeyPath KP on KP.ID = a.ID 
        {(assetType.Class == AssetTypeClass.Rule ? "inner join [Rule] R on R.ID = A.ObjectID" : "")}
        cross apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
        outer apply (
            select  T.[uid]
            from    graph.AssetNode S,
                    graph.AssetEdge E,
                    graph.assetNode T
            where   match (T-(E)->S)
                    and E.PredicateType in (3,4)
                    and S.[uid] = A.[uid]
        ) Parent
        left join AssetDetail P on P.uid = Parent.uid
        {string.Join("\n", fieldJoins)}
where   A.[uid] = @assetUid";


            return (await CompanyContext.QueryAsync<dynamic>(sql, dbArgs, ApiTimeout)).FirstOrDefault();
        }

        public async Task<List<IndexFieldDisplay>> GetAssetSearchFields(Guid assetUid)
        {
            var asset = GetAssetByUID(assetUid);

            if (asset == null)
                return null;

            var canRead = CompanyContext.HasAssetPermission(asset.ID, Permission.ReadAsset);

            if (!canRead)
                return null;

            var assetType = CompanyContext.Filter<AssetType>(a => a.ID == asset.AssetTypeID).FirstOrDefault();
            //Get fieldtypes to display on search result card
            var fieldTypes = CompanyContext.Filter<FieldType>(f => f.AssetTypeID == asset.AssetTypeID && f.SearchAddToResult).ToList();

            if (!fieldTypes.Any())
                return null;

            var fieldJoins = new List<string>();
            var fieldColumns = new List<string>();
            DynamicParameters dbArgs = new DynamicParameters();
            dbArgs.Add("@assetUid", assetUid);

            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

            var sql = $@"
select  
        {string.Join(","+Environment.NewLine, fieldColumns)}
from    Asset A
        inner join AssetType T on T.ID = A.AssetTypeID
        outer apply (
            select  T.[uid]
            from    graph.AssetNode S,
                    graph.AssetEdge E,
                    graph.assetNode T
            where   match (T-(E)->S)
                    and E.PredicateType in (3,4)
                    and S.[uid] = A.[uid]
        ) Parent
        left join AssetDetail P on P.uid = Parent.uid
        {string.Join(Environment.NewLine, fieldJoins)}
where   A.[uid] = @assetUid";

            var data = await CompanyContext.QueryFirstOrDefaultAsync<dynamic>(sql, dbArgs) as IDictionary<string, object>;

            fieldTypes.Sort((x, y) =>
                {
                    //Order field types by SearchDisplayOrder(if available), ColumnOrder, FriendlyName
                    int result = Nullable.Compare(x.SearchDisplayOrder, y.SearchDisplayOrder);
                    if (result == 0)
                        result = x.ColumnOrder.CompareTo(y.ColumnOrder);
                    if (result == 0)
                        result = x.FriendlyName.CompareTo(y.FriendlyName);
                    return result;
                });

            return fieldTypes.Select(f => new IndexFieldDisplay(){
               Name = f.Name,
               Type = f.Type,
               Label = f.FriendlyName,
               Prefix = f.SearchPrefix,
               Suffix = f.SearchSuffix,
               Value = data[f.Name]?.ToString() ?? "",
               Empty = (data[f.Name] is null && f.ShowIfEmpty)
            }).Where(f => !string.IsNullOrEmpty(f.Value) || f.Empty).ToList();
        }

        public async Task PopulateSheetForAssetTypeAndAssets(SLDocument document, AssetType assetType, List<Guid> assetUids)
        {
            var fields = new List<FieldType>();

            var qp = new List<KeyValuePair<string, string>>();
            qp.Add(new KeyValuePair<string, string>("_assetUid", string.Join(",", assetUids.Select(x => x.ToString()))));
            qp.Add(new KeyValuePair<string, string>("includeParent", "true"));
            var results = await GetAssets(assetType, qp);

            var hierarchy = CompanyContext.IntersectTypes
                .FirstOrDefault(x => x.Object == assetType.Object && x.ObjectID == assetType.ObjectID && x.Predicate.Type == PredicateType.InterTypeHierarchy);

            bool includeParent = true;
            if (hierarchy == null)
            {
                includeParent = false;
            }
            var typesToAvoid = new List<string>() {
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.OwnershipLookup.ToString()
            };



            if (includeParent)
            {
                fields.Add(new FieldType { Type = "string", Name = "ParentDisplayName", FriendlyName = "Parent" });
            }

            fields.AddRange(CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetType.ID).OrderBy(x => x.ColumnOrder).ThenBy(x => x.FriendlyName).ToList());

            fields.Add(new FieldType { Type = "string", Name = "AssetUid", FriendlyName = "Asset UID" });
            fields.Add(new FieldType { Type = "number", Name = "AssetId", FriendlyName = "Asset ID" });


            int index = 1;

            foreach (var field in fields)
            {
                if (typesToAvoid.Contains(field.Type))
                    continue;
                document.SetCellValue(1, index++, (string)field.FriendlyName);
            }

            document.SetCellValue(1, index++, "Url");
            var rowData = results.items.ToList().OrderBy(x => x.StepNo).ThenBy(x => x.Name).ToList();

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

                        if (field.Name == "Color")
                        {
                            string val = extractColorNameFromJSON((string)rowValues[field.Name]);
                            setCellValueFromField(document, rowNumber, index, field, val);
                        }
                        else
                        {
                            var val = rowValues[field.Name];
                            setCellValueFromField(document, rowNumber, index, field, val);
                        }

                    }

                    index++;
                }
                document.SetCellValue(rowNumber, index, $"asset/{rowValues["AssetUid"]}");
            }
            SetExcelColumnWidths(document, fields);
        }

        public async Task<List<AssetTypeExportTemplate>> GetExportTemplates(Guid assetTypeUid = default(Guid), Guid exportTemplateUID = default(Guid))
        {
            List<AssetTypeExportTemplate> templateList = new List<AssetTypeExportTemplate>();

            string whereSQL = "";                      

            if (exportTemplateUID != null && exportTemplateUID != Guid.Empty)
            {
                whereSQL = $"where ATET.uid = '{exportTemplateUID}'";
                                
            }
            if (assetTypeUid != null && assetTypeUid != Guid.Empty)
            {
                whereSQL = $"where AT.Uid = '{assetTypeUid}'";
            }            
           
            if((!string.IsNullOrWhiteSpace(whereSQL)) || (string.IsNullOrWhiteSpace(whereSQL) && CompanyContext.CurrentResourceIsAdmin))
            {             
                string exportTemplateSQL = $@"select 
                                                ATET.ID, 
                                                ATET.uid, 
                                                ATET.AssetTypeID, 
                                                AT.uid as AssetTypeUID, 
                                                ATET.Name, 
                                                ATET.Description,
                                                ATET.ExportViewType,
                                                ATET.IncludeUrl,
                                                ATET.IncludeParent,
                                                ATET.TemplateFile,
                                                ATET.CreatedBy,
	                                            ATET.CreatedOn,
	                                            ATET.UpdatedBy,
	                                            ATET.UpdatedOn,
                                                ATET.UsageNotes,
                                                CASE WHEN ATET.templatefile IS NULL THEN 0 ELSE 1 END as HasTemplateFile
                                            from 
                                                AssetTypeExportTemplate ATET 
                                                left join AssetType AT ON ATET.AssetTypeID = AT.ID 
                                            {whereSQL}
                                            order by ATET.Name, ATET.ID";

                templateList = (await CompanyContext.QueryAsync<AssetTypeExportTemplate>(exportTemplateSQL, timeout: ApiTimeout)).ToList();

                foreach (var template in templateList)
                {
                    string templateFieldTypesSQL = $@"select FT.Name from AssetTypeExportTemplateField ATETF inner join FieldType FT on ATETF.FieldTypeId = FT.ID where ATETF.TemplateId = @templateId order by [Order] asc";

                    template.IncludeFieldTypes = (await CompanyContext.QueryAsync<string>(templateFieldTypesSQL, new { templateId = template.ID }, timeout: ApiTimeout)).ToArray();
                }
            }
            
            return templateList;
        }
    }
}
