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
using System.Drawing;
using System.Threading;
using d360.model.helpers.filters;

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

        public Asset GetAssetByObjectId(string obj, int objId)
        {
            return CompanyContext.Filter<Asset>(i => i.Object == obj && i.ObjectID == objId).SingleOrDefault();
        }

        public Asset GetAssetByUID(Guid assetUid)
        {
            return CompanyContext.Filter<Asset>(i => i.uid == assetUid, i => i.AssetType).SingleOrDefault();
        }

        public List<AssetTypeClassInfo> GetAssetTypeList()
        {
            return AssetTypeClass.BusinessAsset.GetAsList();
        }

        public async Task<IEnumerable<AssetTypeApiViewModel>> GetAssetType(IEnumerable<KeyValuePair<string, string>> queryParams, AssetTypeClass? Class, Guid? assetTypeUid)
        {
            var dbArgs = new DynamicParameters();
            string condition = string.Empty;
            string optionalJoin = string.Empty;
            string permissionsJoin = string.Empty;

            if (Class.HasValue)
            {
                var Id = (int)Class;
                dbArgs.Add("@Id", Id.ToString());
                condition = " and A.[Class]=@Id";
            }
            var levelsSql = "";
            List<string> whereStatements = new List<string>();
            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "useastransformation"))
                {
                    bool useAsTransformation;
                    var useAsTransformationString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "useastransformation").Value;
                    if (bool.TryParse(useAsTransformationString, out useAsTransformation))
                    {

                        condition += " and A.UseAsTransformation=@useAsTransformation ";
                        dbArgs.Add("useAsTransformation", useAsTransformation);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value for parameter [useastransformation]", useAsTransformationString);
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "hierarchical"))
                {
                    bool hierarchical;
                    var hierarchicalString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "hierarchical").Value;
                    if (bool.TryParse(hierarchicalString, out hierarchical))
                    {

                        condition += " and A.Hierarchical=@hierarchical ";
                        dbArgs.Add("hierarchical", hierarchical);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value for parameter [hierarchical]", hierarchicalString);
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "autodisplaydescription"))
                {
                    bool autoDisplayDescription;
                    var autoDisplayDescriptionString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "autodisplaydescription").Value;
                    if (bool.TryParse(autoDisplayDescriptionString, out autoDisplayDescription))
                    {

                        condition += " and A.AutoDisplayDescription=@autodisplaydescription ";
                        dbArgs.Add("autoDisplayDescription", autoDisplayDescription);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value for parameter [autoDisplayDescription]", autoDisplayDescriptionString);
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "autodisplayparent"))
                {
                    bool autoDisplayParent;
                    var autoDisplayParentString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "autodisplayparent").Value;
                    if (bool.TryParse(autoDisplayParentString, out autoDisplayParent))
                    {

                        condition += " and A.AutoDisplayParent=@autoDisplayParent ";
                        dbArgs.Add("autoDisplayParent", autoDisplayParent);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value for parameter [autoDisplayParent]", autoDisplayParentString);
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "obj") && queryParams.ToList().Any(q => q.Key.ToLower() == "objid"))
                {
                    var obj = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "obj").Value;
                    var objId = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "objid").Value;
                    SystemObjects ot;
                    if (Enum.TryParse(obj, out ot))
                    {
                        condition += " and A.Object=@obj ";
                        dbArgs.Add("obj", obj);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value for parameter [obj]", obj);
                    }
                    int otid;
                    if (int.TryParse(objId, out otid))
                    {
                        condition += " and A.ObjectID=@objId ";
                        dbArgs.Add("objId", otid);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value for parameter [objId]", objId);
                    }
                }


                if (queryParams.ToList().Any(q => q.Key.ToLower() == "includelevels"))
                {
                    bool includeLevels;
                    var includeLevelsString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "includelevels").Value;
                    if (bool.TryParse(includeLevelsString, out includeLevels))
                    {
                        levelsSql = @",(select Level, Name, Description from AssetTypeLevel where AssetTypeID = A.ID order by Level for json path) as LevelsJson";
                    }
                    else
                    {
                        throw new ArgumentException("Invalid value for parameter [includeLevels]", includeLevelsString);
                    }
                }

            }

            if (!CompanyContext.CurrentResourceIsAdmin)
            {
                permissionsJoin = $"outer apply (select case when ua.PermissionsBitMask & {(int)Permission.ReadAsset} = 0 then 0 else 1 end as hasRead from UserAssetPermissions(@userId,a.id) ua where ua.AssetTypeID = a.id and ua.AssetID = 0) UserP";
                condition += " and (UserP.hasRead is null or UserP.hasRead != 0)";
                dbArgs.Add("@userId", CompanyContext.CurrentResourceID);
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
                                    ,0 as 'CanOwnFusion'
                                    ,A.AutoDisplayParent
                                    ,A.FlowObjectType
                                    ,A.CanEditParent
                                    {levelsSql} 
                                    ,P.[Path]
                                    ,AT.IconBackColor as BackColor
                                    ,AT.Icon as Icon
                                    ,AT.IconForeColor as ForeColor
                        FROM        AssetType A
                                    {optionalJoin}
                                    cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
                                    left join [dbo].[AssetTypeStyle] AT on (A.ID = AT.ID)
                                    {permissionsJoin}
                        where       A.[State] = 1 and A.ObjectID != 0
                        {condition}
                        order by    P.[Path]
                        ";

            // If you change the order of the select columns please pay attention to the dapper multimap split on parameter where it is splitting out the icon class.

            return await CompanyContext.QueryAsync<AssetTypeApiViewModel, IconStyleInsert, AssetTypeApiViewModel>(sql, param: dbArgs, map: (a, i) => { a.IconStyle = i; return a; }, splitOn: "Path,BackColor", timeout: ApiTimeout);
        }

        //UseAsAdmin is used to override permissions from reading an access. It is used by Process Designer Export
        public async Task<AssetsApiViewModel> GetAssets(AssetType assetType, IEnumerable<KeyValuePair<string, string>> queryParams, bool useAsAdmin = false, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null)
            {
                cancellationToken = CancellationToken.None;
            }

            var assetTypeID = 0;
            Guid? parentUid = null;
            bool parentUidPopulated = false;
            var includeRelationships = false;
            var includeSegments = false;
            var includePermissionDetails = false;
            bool includeOnlyListableFields = false;
            string populatePremissionAssetTableSQL = " ";
            string populateOwnershipLookupTableSQL = " ";
            string selectOwnershipSQL = "";

            string permissionDetailSQL = " ";
            string includePermissionFields = " ";
            bool listColorsAsJSON = false;
            bool includeColor = true;
            var includeTotal = true;
            bool isHierachyItem = false;
            bool hasAssetPathField = false;
            string hierarchyParentUidCol = "";
            string hierarchyParentUidSelect = "";
            bool includeCreatedByModifiedBy = false;
            bool includeOwnershipLookup = false;
            bool simpleFilterOwnershipOnResource = false;
            bool simpleFilterOwnershipOnSecurityAsset = false;
            bool isForTreeGrid = false;
            bool useTempTableForResults = false;
            string profilingCheckSql = "";
            string profilingCheckFields = "";
            bool includeProfilingCheck = false;

            Dictionary<string, string> ownershipPropertiesMapping = new Dictionary<string, string>();

            if (assetType == null)
                throw new Exception("Invalid assetType specified");

            if (useAsAdmin && !queryParams.ToList().Any(k => k.Key.ToLower() == "_assetuid"))
            {
                throw new ArgumentException("UseAsAdmin parameter can be used only with _assetUid specified!");
            }

            assetTypeID = assetType.ID;

            List<string> hiddenFieldTypes = new List<string>() { "ComplexRelationLookup", "", "RefListRelationship" };
            var allFieldTypes = CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetTypeID).AsNoTracking().ToList();
            var fieldTypes = allFieldTypes.Where(f => !hiddenFieldTypes.Contains(f.Type)).ToList();

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_predicateuid"))
            {
                includeRelationships = true;
            }

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

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includecreatedmodifiedby"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includecreatedmodifiedby").Value, out includeCreatedByModifiedBy);
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includeownershiplookup"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includeownershiplookup").Value, out includeOwnershipLookup);
            }

            if (queryParams.Any(x => x.Key.ToLower() == "isfortreegrid"))
            {
                bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "isfortreegrid").Value, out isForTreeGrid);
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includeprofilingcheck"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includeprofilingcheck").Value, out includeProfilingCheck);
            }

            //check for asset path fields now after include fields have been filtered
            if (fieldTypes.Any(x => x.Type == "Path"))
            {
                hasAssetPathField = true;
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

            //Don't get field sql for OwnershipLookup fields, as that will return the definition rather than the json we want
            //The sql for OwnershipLookup fields will be added below at the includeOwnershipLookup conditional
            getFieldSql(fieldTypes.Where(f => f.Type != "OwnershipLookup").ToList(), dbArgs, fieldJoins, fieldColumns, "A.[Object]", "A.[ObjectId]", listColorsAsJSON);
            List<string> countJoins = new List<string>(fieldJoins);

            if (includeProfilingCheck)
            {
                profilingCheckSql = $"cross apply (select case when exists (select 1 from AssetDataProfile where AssetID = A.ID) then cast(1 as bit) else cast(0 as bit) end as HasProfiling) Profiling";
                profilingCheckFields = $"Profiling.HasProfiling as HasProfiling,";
            }

            if (includeRelationships)
            {
                var subjectAlias = "B";
                var objectAlias = "A";
                var ATsubjectAlias = "TB";
                var ATobjectAlias = "T";
                string relatedAssetUIDString = "";
                Guid relatedAssetUID;

                var predicateUID = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_predicateuid").Value;
                var intersectJoin = "";
                var intersecTypeJoin = "";
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
                    intersecTypeJoin = $"IT.[Subject] = {ATobjectAlias}.[Object] and IT.SubjectID = {ATobjectAlias}.ObjectID and IT.[Object] = {ATsubjectAlias}.[Object] and abs(IT.ObjectID) = {ATsubjectAlias}.ObjectID";
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
                    intersecTypeJoin = $"IT.[Subject] = {ATsubjectAlias}.[Object] and abs(IT.SubjectID) = {ATsubjectAlias}.ObjectID and IT.[Object] = {ATobjectAlias}.[Object] and IT.ObjectID = {ATobjectAlias}.ObjectID";
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
	                where IT.ID = I.IntersectTypeID and P.[UID] = @predicateUid
                    and {intersecTypeJoin})";
                }

                var innerCountSql = $@"
						select {addtop1hint} B.ID as Relationships  from Asset B
						inner join AssetType TB on TB.ID = B.AssetTypeID
						where {relatedAssetSql}
						and exists (select 1 from [Intersect] I
							inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid
							where {intersectJoin})";

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
            {
                whereStatements.Add("R.Relationships is not null");
            }

            //Add read permission check for admin and non-admin users as in GetAssets procedure
            var restrictions = (await CompanyContext.QueryAsync<UserGetAPIRestrictionModel>(@"select
                    case when exists(
                    select AssetID from dbo.UserAssetPermissions(@userId,@assetTypeID) where ((PermissionsBitMask & @p)) = 0)
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
                    ", new { userId = CompanyContext.CurrentResourceID, assetTypeID, p = (int)Permission.ReadAsset }
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
                    whereStatements.Add($"not exists (select AssetID from #PermissiondAssets where AssetID = A.ID and ((PermissionsBitMask & {(int)Permission.ReadAsset})) = 0)");
                }
            }

            var ownershipFieldTypes = fieldTypes.Where(f => f.Type == "OwnershipLookup").ToList();
            if (ownershipFieldTypes.Any(f => f.SortOrder > 0))
            {
                includeOwnershipLookup = true;
            }
            if (includeOwnershipLookup && ownershipFieldTypes.Any())
            {
                populateOwnershipLookupTableSQL = @"
                    declare @id int = (select top 1 id from assettype where id = @assettypeid)

                    drop table if exists #OwnershipLookupAssets;
                    create table #OwnershipLookupAssets (
						AssetID bigint,
                        ResponsibilityTypeID int,
                        ResponsibilityTypeName nvarchar(250),
                        ResourceName nvarchar(501),
                        SecurityAsset char(1),
                        SecurityAssetName nvarchar(501),
                        Context nvarchar(max),
                        ResourceId int,
                        ResourceUid uniqueidentifier,
                        SecurityAssetId int,
                        SecurityAssetUid uniqueidentifier
					);
					insert into #OwnershipLookupAssets
                        SELECT [AssetID]
                              ,[ResponsibilityTypeID]
                              ,[ResponsibilityTypeName]
                              ,[ResourceName]
                              ,[SecurityAsset]
                              ,[SecurityAssetName]
                              ,[Context]
                              ,[ResourceId]
                              ,[ResourceUid]
                              ,[SecurityAssetId]
                              ,[SecurityAssetUid]
                        FROM [dbo].[ResponsibilityDetail] rd
                        where rd.assetid <> 0 and IsVisible = 1 and rd.[AssetTypeID] = @id
                        union all
                        select a.[ID] as AssetID
                             ,rd.[ResponsibilityTypeID]
                             ,rd.[ResponsibilityTypeName]
                             ,rd.[ResourceName]
                             ,rd.[SecurityAsset]
                             ,rd.[SecurityAssetName]
                             ,rd.[Context]
                             ,rd.[ResourceId]
                             ,rd.[ResourceUid]
                             ,rd.[SecurityAssetId]
                             ,rd.[SecurityAssetUid]
                        from ResponsibilityDetail rd
                        inner join asset a on rd.assettypeid = a.assettypeid
                        where rd.assetid = 0 and IsVisible = 1 and rd.assettypeid = @id
                        union all
                        select a.[ID] as AssetID
                             ,rd.[ResponsibilityTypeID]
                             ,rd.[ResponsibilityTypeName]
                             ,rd.[ResourceName]
                             ,rd.[SecurityAsset]
                             ,rd.[SecurityAssetName]
                             ,rd.[Context]
                             ,rd.[ResourceId]
                             ,rd.[ResourceUid]
                             ,rd.[SecurityAssetId]
                             ,rd.[SecurityAssetUid]
                        from ResponsibilityDetail rd
                        inner join asset a on rd.assetid = a.id
                        where rd.AssetTypeID = 0 and IsVisible = 1 and a.AssetTypeID = @id;

                    create index cix_OwnershipLookupAssetId on #OwnershipLookupAssets (AssetId);
                    ";

                List<string> ownershipJoins = new List<string>();
                List<string> ownershipColumns = new List<string>();
                List<string> groupColumns = new List<string>();

                ownershipFieldTypes.ForEach(f =>
                {
                    FieldTypeLookup lookup = CompanyContext.FieldTypeLookups.Where(ftl => ftl.FieldTypeID == f.ID).FirstOrDefault();
                    var definition = (dynamic)JsonConvert.DeserializeObject(lookup.Definition);
                    string responsibilityIdCondition = "";
                    bool includeResponsibilityNames = true;
                    if (definition.ResponsibilityType != null && definition.ResponsibilityType > 0)
                    {
                        responsibilityIdCondition = $" and ola{f.ID}.ResponsibilityTypeID = {definition.ResponsibilityType}";
                        includeResponsibilityNames = false;
                    }
                    string innerOwnershipQuery = "";
                    if ((bool)definition.ExpandGroupMembership)
                    {
                        innerOwnershipQuery = $@"select ResponsibilityTypeName, ResourceName, ResourceUid, ResourceItemUrl from #OwnershipLookupAssets ola{f.ID}
                            cross apply (select  concat('resource/', cast(ResourceID as varchar)) as ResourceItemUrl) ola{f.ID}x
			                where ola{f.ID}.assetid = a.id {responsibilityIdCondition}
			                group by ResponsibilityTypeName, ResourceName, ResourceUid, ResourceItemUrl";
                        simpleFilterOwnershipOnResource = true;
                    }
                    else
                    {
                        innerOwnershipQuery = $@"select ResponsibilityTypeName, SecurityAssetName as ResourceName, SecurityAssetUid as ResourceUid, ResourceItemUrl  from #OwnershipLookupAssets ola{f.ID}
                            cross apply (select  concat(case SecurityAsset when 'R' then '/resource/' else '/group/' end, cast(SecurityAssetID as varchar)) as ResourceItemUrl) ola{f.ID}x
                			where ola{f.ID}.assetid = a.id {responsibilityIdCondition}
                            group by ResponsibilityTypeName, SecurityAssetName, SecurityAssetUid, ResourceItemUrl";
                        simpleFilterOwnershipOnSecurityAsset = true;
                    }
                    string responsibilityNameSelect = includeResponsibilityNames ? "string_agg(ResponsibilityTypeName,', ')" : "''";
                    string ownershipQuery = $@"
                        outer apply(
                            select FormattedValue = (
		                        select ResourceName, {responsibilityNameSelect} AS ResponsibilityTypes, LOWER(ResourceUid) AS ResourceUid, ResourceItemUrl
		                        from ( {innerOwnershipQuery} ) Responsibilites{f.ID}
                                group by ResourceName, ResourceUid, ResourceItemUrl
                                order by ResourceName
	                         FOR JSON PATH)
                        ) F{f.ID} (FormattedValue) ";

                    ownershipColumns.Add($"F{f.ID}.FormattedValue as [{f.Name}]");
                    groupColumns.Add($"F{f.ID}.FormattedValue");
                    fieldJoins.Add(ownershipQuery);
                    ownershipJoins.Add(ownershipQuery);
                    ownershipPropertiesMapping.Add(f.Name, "F{f.ID}.FormattedValue");
                });

                selectOwnershipSQL = $@"
                select {(string.Join(", ", ownershipColumns))} ,
                string_agg(cast(a.id as nvarchar(max)), ',') as Assets
                from asset a
                {(string.Join("\n ", ownershipJoins))}
                where A.AssetTypeID = @assetTypeID
				group by {(string.Join(", ", groupColumns))}";

                if (!isForTreeGrid)
                {
                    useTempTableForResults = true;

                    selectOwnershipSQL = $@"
                    select {(string.Join(", ", ownershipColumns))} ,
                    string_agg(cast(a.id as nvarchar(max)), ',') as Assets
                    from asset a
                    {(string.Join("\n ", ownershipJoins))}
                    where A.AssetTypeID = @assetTypeID 
                    and a.id in (select assetid from #results)
				    group by {(string.Join(", ", groupColumns))}";
                }
            }

            if (!CompanyContext.CurrentResourceIsAdmin && !useAsAdmin)
            {
                if (restrictions.HasAssetTypeRestriction)
                    whereStatements.Add($"not exists (select 1 from AssetTypesUserCantRead(@userId) u where u.AssetTypeID = A.AssetTypeID)");
            }
            getQueryParamsSql(model, assetType, fieldTypes, dbArgs, whereStatements, pagingSql, queryParams);

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
            bool includeParentInCount = false;
            bool includeAssetPathInCount = false;
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

                var functionalPredicateType = PredicateType.InterTypeHierarchy;
                if (assetType.Object == "PolicyType" || assetType.Object == "TaxonomyType")
                {
                    functionalPredicateType = PredicateType.IntraTypeHierarchy;
                }
                if (!CompanyContext.TypeHasParent((SystemObjects)(Enum.Parse(typeof(SystemObjects), assetType.Object, true)), assetType.ObjectID, functionalPredicateType))
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
                    List<Tuple<int, string>> originalTypeMappings = new List<Tuple<int, string>>();

                    //handle field types in case "Field from relationship"
                    foreach (var ft in allFieldTypes.Where(x => x.LookupObjectFieldTypeID > 0))
                    {
                        var origFieldType = CompanyContext.FieldTypes.FirstOrDefault(x => x.ID == ft.LookupObjectFieldTypeID);
                        if (origFieldType != null)
                        {
                            originalTypeMappings.Add(new Tuple<int, string>(ft.ID, ft.Type));
                            ft.Type = origFieldType.Type;
                        }
                    }
                    getFieldSql(allFieldTypes, tempArgs, tempJoins, tempFieldColumns);

                    var filterDataProvider = new FilterDataProvider(CompanyContext);
                    var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.CustomFields, includeParent);
                    filterExpressionParser.LoadFieldTypes(allFieldTypes, tempFieldColumns);
                    Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                    List<int> filteredFields = new List<int>();
                    whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams, out filteredFields) + ")");

                    // advanced filter contains a filter on the parent.  Technically this parent join should be an Inner join because the parent MUST be one of the values and not null
                    if (value.Contains("ParentDisplayName") || value.Contains("ParentUid"))
                    {
                        includeParentInCount = true;
                    }

                    // check if the advanced filter contains a filter by asset path
                    foreach (var fieldTypeId in filteredFields)
                    {
                        var fieldType = allFieldTypes.FirstOrDefault(x => x.ID == fieldTypeId);

                        if (fieldType != null && fieldType.Type == "Path")
                        {
                            includeSegments = true;
                            includeAssetPathInCount = true;
                        }
                    }

                    if (includeOnlyListableFields || includeFieldsList.Any())
                    {
                        if (originalTypeMappings.Count > 0)
                        {
                            foreach (var item in originalTypeMappings)
                            {
                                var ft = allFieldTypes.FirstOrDefault(x => x.ID == item.Item1);
                                ft.Type = item.Item2;
                            }
                        }

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
                    var filterDataProvider = new FilterDataProvider(CompanyContext);
                    var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.Relationships);
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
					   when permission.mask is not null and permission.mask & 8 = 8 then 1
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
                    //There may be multiple OwnershipLookup fields, but they all look to the same table for filtering, so that will be dealt with below
                    foreach (var ft in fieldTypes.Where(x => x.IsListable == true && x.Type != DataType.OwnershipLookup.ToString()))
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
                            includeAssetPathInCount = true; // asset path field and simple filter must include asset path in join for count
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
                        else if (ft.Type == DataType.Counter.ToString())
                        {
                            simpleFilters.Add($"('{ft.CounterPrefix}' + CAST(F{ft.ID}.FormattedValue as nvarchar(max))) like @simpleFilter");
                        }
                        else
                        {
                            simpleFilters.Add($"F{ft.ID}.FormattedValue like @simpleFilter");
                        }
                    }
                    if (includeOwnershipLookup && ownershipFieldTypes.Any())
                    {
                        List<string> ownershipSimpleFilterFields = new List<string>();
                        ownershipSimpleFilterFields.Add("ResponsibilityTypeName");

                        if (simpleFilterOwnershipOnResource)
                        {
                            ownershipSimpleFilterFields.Add("ResourceName");
                        }
                        if (simpleFilterOwnershipOnSecurityAsset)
                        {
                            ownershipSimpleFilterFields.Add("SecurityAssetName");
                        }
                        string simpleFilterOwnership = $@"exists (select 1 from #OwnershipLookupAssets ola where ola.assetid = a.id
                                                    and ({string.Join(" or ", ownershipSimpleFilterFields.Select(f => $"{f} like @simpleFilter"))}))";
                        simpleFilters.Add(simpleFilterOwnership);
                    }

                    //do not use simple filter on parent value if response is used for tree grid, otherwise all child items will be matched incorrectly
                    if (includeParent && !isForTreeGrid)
                    {
                        simpleFilters.Add($"Parent.DisplayValue like @simpleFilter");
                        includeParentInCount = true; // simple filter AND the asset has a parent which posibly impacts the count
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
                                                and
                                                rd.isVisible = 1
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
                                                and
                                                rd.isVisible = 1
                                            )";
                    whereStatements.Add(ownershipSQL);
                }
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_notownedby"))
            {
                List<Guid> notOwnerUids = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_notownedby")
                    .Value.Split(',').Select(x =>
                    {
                        var guid = Guid.Empty;
                        Guid.TryParse(x, out guid);
                        return guid;
                    }).ToList();

                if (notOwnerUids.Any(x => x == Guid.Empty))
                    throw new Exception("Invalid Owner Uid in parameters!");

                if (notOwnerUids.Count > 0)
                {
                    dbArgs.Add("notOwnerUids", notOwnerUids);
                    var ownershipSQL = $@"NOT EXISTS(
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid in @notOwnerUids 
                                                and 
                                                a.ID=rd.AssetID 
                                                and
                                                rd.isVisible = 1
                                            UNION
                                            SELECT 1 
                                            FROM 
                                                [dbo].[ResponsibilityDetail] rd 
                                            WHERE 
                                                rd.SecurityAssetUid in @notOwnerUids 
                                                and 
                                                rd.ApplyToType = 1 
                                                and 
                                                rd.AssetID = 0 
                                                and 
                                                rd.AssetTypeId=a.AssetTypeId
                                                and
                                                rd.isVisible = 1
                                            )";
                    whereStatements.Add(ownershipSQL);
                }
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_parentuid"))
            {
                parentUidPopulated = true;
                var parentUidString = queryParams.FirstOrDefault(k => k.Key.ToLower() == "_parentuid").Value;
                if (parentUidString != "null")
                {
                    Guid pUid;
                    if (Guid.TryParse(parentUidString, out pUid))
                    {
                        parentUid = pUid;
                        if (!CompanyContext.Any<Asset>(i => i.uid == pUid))
                        {
                            throw new ArgumentException($"_parentUid with value {pUid} does not correspond to a valid asset!");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("_parentUid parameter must be a valid Guid, be set to null, or not be present!");
                    }
                }
            }

            var hierachy = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_ishierachyitem").Value;
            bool.TryParse(hierachy, out isHierachyItem);

            if (isHierachyItem || parentUidPopulated)
            {
                if (isHierachyItem)
                {
                    hierarchyParentUidCol = " ,HParent.uid as ParentUid";
                }

                string predicateType = "0";
                switch (assetType.Class)
                {
                    case AssetTypeClass.Model:
                    case AssetTypeClass.Policy:
                        predicateType = "4";
                        break;
                    default:
                        predicateType = "3";
                        break;
                }
                hierarchyParentUidSelect = $@" {(parentUid.HasValue ? "cross" : "outer")} apply (
					select	PA.uid 
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = {predicateType}
							inner join Asset PA on PA.Object = I.Subject and PA.ObjectID =I.SubjectID
					) HParent ";

                if (parentUidPopulated)
                {
                    if (parentUid.HasValue)
                    {
                        dbArgs.Add("parentUid", parentUid.Value);
                        whereStatements.Add("HParent.Uid = @parentUid");
                    }
                    else
                    {
                        whereStatements.Add("HParent.Uid is null");
                    }
                }

            }

            var whereSql = "";
            if (whereStatements.Any())
                whereSql = $"where {string.Join(" and ", whereStatements)}";

            var fieldsSql = "";
            if (fieldColumns.Any())
                fieldsSql = $",\n {string.Join(",\n", fieldColumns)}";

            bool hasKeyPathCountFiltering = whereSql.ToLowerInvariant().Contains("kp.keypath");

            var countSql = $@"
                select  count(*)
                from    Asset A 
                {(includeAssetPathInCount ? " left join graph.AssetNodeDisplayPath Node on Node.id = a.id" : "")} 
                {(isForTreeGrid ? "cross apply dbo.GetAssetLevelById(A.Id)LVL" : "")}
                {string.Join("\n", countJoins)}
                {(hasKeyPathCountFiltering ? "left join graph.AssetNodeKeyPath KP on KP.ID = a.ID" : "")} 
                {hierarchyParentUidSelect}
                {(includeParentInCount ? parentApplySQL : "")}
                {whereSql}";

            var sql = $@"
                {(useTempTableForResults ? "drop table if exists #results;" : "")}
                select 
                    {(useTempTableForResults ? $"row_number() over ({pagingSql.First()}) as _rowid," : "")}
                    A.ID as AssetId,
                    A.[UID] as [AssetUid],
                    A.AssetTypeId,
                    T.[UID] as AssetTypeUid,
                    {(includeCreatedByModifiedBy ? "UA.uid as UpdatedByUid," : "")}
                    A.UpdatedOn,
                    {(includeCreatedByModifiedBy ? "CA.uid as CreatedByUid," : "")}                    
                    A.CreatedOn,
                    {(isForTreeGrid ? "LVL.Level as 'Level'," : "")}
                    {(includeParent ? parentFieldSQL : "")}
                    {(assetType.Class == AssetTypeClass.Reference ? "A.Code, A.Icon," : "")}
                    {(includeColor ? "ACJ.ColorJson as Color," : "")}
                    {(includeProfilingCheck ? profilingCheckFields : "")}
                    {(includeSegments ? "Node.Segments," : "")}
                    KP.KeyPath as [Path]
                    {fieldsSql}
                    {(includePermissionDetails ? includePermissionFields : "")} 
                    {hierarchyParentUidCol}
                {(useTempTableForResults ? " into #results " : "")}
                from Asset A
                left join Asset CA on CA.ObjectID  = A.CreatedBy and CA.Object = 'Resource'
				left join Asset UA on UA.ObjectID  = A.UpdatedBy and UA.Object = 'Resource'
                {string.Join("\n", fieldJoins)}
                {(includeSegments || hasAssetPathField || whereSql.Contains("Node.") ? " left join graph.AssetNodeDisplayPath Node on Node.ID = a.ID" : "")} 
                left join graph.AssetNodeKeyPath KP on KP.ID = a.ID 
                {(isForTreeGrid ? "cross apply dbo.GetAssetLevelById(A.Id)LVL" : "")}
                {(includeColor ? "cross apply dbo.GetAssetColorJsonByColor(A.Color) ACJ" : "")}
                {(includePermissionDetails ? permissionDetailSQL : "")}
                {(includeProfilingCheck ? profilingCheckSql : "")}
                {hierarchyParentUidSelect}
                {(includeParent ? parentApplySQL : "")}
                {whereSql}
                {string.Join("\n", pagingSql)}
                {(useTempTableForResults ? "select * from #results order by _rowid " : "")}
            ";

            if (!includeTotal)
            {
                countSql = "";
                model.total = null;
            }

            var getAllQuery = $"{populatePremissionAssetTableSQL} {populateOwnershipLookupTableSQL} {countSql} {sql} OPTION(RECOMPILE)";

            if (!string.IsNullOrEmpty(selectOwnershipSQL))
            {
                getAllQuery += selectOwnershipSQL;
            }

            var gridReader = await CompanyContext.Database.Connection.QueryMultipleAsync(
                  new CommandDefinition(getAllQuery,
                  cancellationToken: cancellationToken.Value,
                  parameters: dbArgs,
                  commandTimeout: ApiTimeout
                ));

            if (includeTotal)
            {
                model.total = gridReader.Read<int>().FirstOrDefault();
            }
            var results = gridReader.Read<dynamic>().ToList();


            List<Tuple<List<int>, dynamic>> ownershipData = new List<Tuple<List<int>, dynamic>>();
            if (!string.IsNullOrEmpty(selectOwnershipSQL))
            {
                var dyOwnData = gridReader.Read<dynamic>().ToList();
                foreach (var ow in dyOwnData)
                {
                    var data = (IDictionary<string, object>)ow;
                    var assetIds = data["Assets"].ToString().Split(',').Select(x => int.Parse(x.Trim())).ToList();
                    ownershipData.Add(new Tuple<List<int>, dynamic>(assetIds, ow));
                }
            }
            //Loop results once for if any applicable conversions
            if (useTempTableForResults || includeRelationships || includePermissionDetails || includeSegments || (includeOwnershipLookup && ownershipFieldTypes.Any()))
            {
                foreach (var result in results)
                {
                    if (useTempTableForResults)
                    {
                        IDictionary<string, object> res = result;
                        result.Remove("_rowid");
                    }

                    if (includeRelationships)
                    {
                        result.Relationships = JsonConvert.DeserializeObject(result.Relationships);
                    }

                    if (includePermissionDetails)
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

                    if (includeSegments)
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

                    if (includeOwnershipLookup && ownershipFieldTypes.Any())
                    {
                        var data = (IDictionary<string, object>)result;
                        var oData = ownershipData.FirstOrDefault(x => x.Item1.Contains(int.Parse(data["AssetId"].ToString())))
                            .Item2;
                        ownershipFieldTypes.ForEach(ft =>
                        {
                            var ownership = (IDictionary<string, object>)oData;

                            var val = (string)ownership[ft.Name];
                            if (!string.IsNullOrEmpty(val))
                            {
                                data[ft.Name] = JsonConvert.DeserializeObject(val);
                            }
                        });
                    }
                }
            }

            //if we want to include hierarchy items (parents) for tree grid
            //used in tree grids we want to find all parents from our assets that are included in results
            if (isForTreeGrid)
            {
                if (queryParams.Any(x => x.Key.ToLower() == "_simplefilter" || x.Key.ToLower() == "_filter"))
                {
                    List<Guid> assetUids = new List<Guid>();
                    foreach (var item in results)
                    {
                        var data = (IDictionary<string, object>)item;
                        assetUids.Add(Guid.Parse(data["AssetUid"].ToString()));
                    }

                    var allParents = GetAllParentsAssetUid(assetUids).Distinct().Where(x => !assetUids.Contains(x)).ToList();

                    if (allParents.Count > 0)
                    {
                        var par = queryParams.Where(k => k.Key.ToLower() != "_simplefilter" && k.Key.ToLower() != "_filter" && k.Key.ToLower() != "isfortreegrid").ToList();
                        par.Add(new KeyValuePair<string, string>("_assetUid", string.Join(",", allParents)));
                        var fammilyAssets = await GetAssets(assetType, par);
                        results = results.Union(fammilyAssets.items).ToList().ToList();

                        //filtered tree grid results need to be sorted in memory too
                        string orderBy = "";
                        string direction = "ASC";

                        if (queryParams.ToList().Any(x => x.Key.ToLower() == "_order"))
                        {
                            orderBy = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value;
                        }

                        if (queryParams.ToList().Any(x => x.Key.ToLower() == "_direction"))
                        {
                            direction = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value;
                        }

                        if (string.IsNullOrEmpty(orderBy))
                        {
                            orderBy = fieldTypes.Where(x => x.IsListable)
                                .OrderByDescending(x => x.SortOrder)
                                .ThenBy(x => x.ID).FirstOrDefault().Name;
                        }
                        try
                        {
                            results = results.OrderBy(x => ((IDictionary<string, object>)x)[orderBy]).ToList();
                        }
                        catch (ArgumentException ex)
                        {
                            //If dynamic object for property orderBy does not implement IComparable (i.e. JObject,JArray), use string comparison
                            results.Sort((x, y) =>
                            {
                                var value1 = ((IDictionary<string, object>)x)[orderBy].ToString();
                                var value2 = ((IDictionary<string, object>)y)[orderBy].ToString();
                                return value1.CompareTo(value2);
                            });
                        }

                        if (direction == "DESC")
                        {
                            results.Reverse();
                        }
                    }
                }
            }

            model.items = results;

            return model;
        }

        public async Task<AssetPathResults> GetAssetPaths(AssetType assetType, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();

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

            dbArgs.Add("@assetTypeId", assetType.ID);
            dbArgs.Add("@pageNum", pageNum);
            dbArgs.Add("@pageSize", pageSize);
            dbArgs.Add("@offset", (pageSize * pageNum));

            var sql = $@"
                select	P.[uid],
		                P.[keypath] as [path]  
                from	graph.AssetNodeKeyPath P		                
                where P.assetTypeId = @assetTypeId
                order by P.ID
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            int? total = null;
            if (includeTotal)
            {
                var countSql = $@"select count(1) 
                from	graph.AssetNodeKeyPath P		                
                where P.assetTypeId = @assetTypeId";

                total = await CompanyContext.QueryFirstOrDefaultAsync<int>(countSql, dbArgs, ApiTimeout);
            }

            var results = await CompanyContext.QueryAsync<AssetPathResult>(sql, dbArgs, ApiTimeout);

            return new AssetPathResults
            {
                items = results,
                total = total
            };
        }

        public async Task<SLDocument> GetAssetsExcel(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams, bool isChildItem = false)
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
                DataType.DataTableSelect.ToString()
            };

            //add default fields
            if (assetType.Class == AssetTypeClass.Reference)
            {
                fields.Add(new FieldType { Type = "string", Name = "Code", FriendlyName = "Code" });
                fields.Add(new FieldType { Type = "string", Name = "Color", FriendlyName = "Color" });
                includeAssetUrl = false;
            }

            string ParentAssetTypeUidHeading = "Parent";

            if (includeParent)
            {
                var columnName = "Parent";
                if ((assetType.Class == AssetTypeClass.Reference && hierarchy != null) || isChildItem)
                {
                    var parent = CompanyContext.AssetTypes.FirstOrDefault(x => x.Object == hierarchy.Subject && x.ObjectID == hierarchy.SubjectID);
                    if (parent != null)
                    {
                        columnName = isChildItem ? "Parent " + parent.Name + " Name" : parent.Name;
                        ParentAssetTypeUidHeading = parent.Name + " UID";
                    }

                }

                fields.Add(new FieldType { Type = "string", Name = "ParentDisplayName", FriendlyName = columnName });
            }

            if (!isChildItem)
            {
                fields.AddRange(CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetType.ID).OrderBy(x => x.ColumnOrder).ThenBy(x => x.FriendlyName).ToList());

                fields.Add(new FieldType { Type = "string", Name = "AssetUid", FriendlyName = "Asset UID" });
                fields.Add(new FieldType { Type = "number", Name = "AssetId", FriendlyName = "Asset ID" });
            }
            else
            {
                fields.Add(new FieldType { Type = "string", Name = "Name", FriendlyName = assetType.Name + " Name" });

                fields.AddRange(CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetType.ID && f.Name.ToLower() != "name").OrderBy(x => x.ColumnOrder).ThenBy(x => x.FriendlyName).ToList());

                fields.Add(new FieldType { Type = "string", Name = "ParentAssetUid", FriendlyName = ParentAssetTypeUidHeading });
                fields.Add(new FieldType { Type = "string", Name = "AssetUid", FriendlyName = assetType.Name + " UID" });
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
            if (results.total.HasValue)
            {
                document.SetCellValue(3, 1, "total");
                document.SetCellValue(3, 2, (int)results.total);
            }


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

        private List<Guid> GetAllParentsAssetUid(List<Guid> uids)
        {
            var sql = $@"drop table if exists #family
                create table #family(
                 AssetUid uniqueidentifier
                )
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

            List<Guid> parentUids = new List<Guid>();
            var pages = uids.Count() / 2000;
            for (int i = 0; i <= pages; i++)
            {
                parentUids.AddRange(CompanyContext.Query<Guid>(sql, new { assetUid = uids.Skip(i * 2000).Take(2000) }, ApiTimeout).AsList());
            }

            return parentUids;
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

        public List<DatabaseBulkAssetResult> PostAssets(List<AssetInsert> assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, bool useTempTablesForField = false)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.ImportAssets(execution, assetType, assets, true, sendWorkflowEvents: sendWorkflowEvents, lookupFieldsPassedByValue: lookupFieldsPassedByValue, useTempTablesForField: useTempTablesForField);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);

                // Quick sync of graph.
                try
                {
                    CompanyContext.SynchronizeExecutionAssetsWithGraph(execution.ExecutionID);
                }
                catch
                {
                    // Do nothing, as graph topic will eventually synch.
                }
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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

            if (!model.CanEditParent.HasValue)
            {
                model.CanEditParent = true;
            }

            AssetType at = null;

            switch (model.Class)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.TechnicalAsset:
                case AssetTypeClass.Rule:
                    #region
                    parentType = (model.Class == AssetTypeClass.Rule) ? SystemObjects.RuleType : SystemObjects.ArtifactType;

                    at = new AssetType
                    {
                        uid = uid,
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description,
                        Object = parentType.ToString(),
                        State = State.Active,
                        UpdatedBy = resourceId,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = resourceId,
                        CreatedOn = DateTime.UtcNow,
                        Hierarchical = false,
                        Class = model.Class,
                        AutoDisplayDescription = model.AutoDisplayDescription,
                        UseAsTransformation = model.UseAsTransformation,                        
                        Parent = parentAssetType,
                        AutoDisplayParent = model.AutoDisplayParent,
                        CanEditParent = model.CanEditParent
                    };
                    CompanyContext.Add(at);

                    model.ObjectID = at.ObjectID;
                    model.Object = at.Object;

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
                    at = CompanyContext.Filter<AssetType>(i => i.Object == model.Object && i.ObjectID == model.ObjectID).SingleOrDefault();
                    if (at != null)
                    {
                        at.AutoDisplayDescription = model.AutoDisplayDescription;
                        at.Notes = model.Notes;
                        at.uid = uid;
                        CompanyContext.Update(at);
                    }
                    #endregion
                    break;
                case AssetTypeClass.Model:
                case AssetTypeClass.Policy:
                    #region

                    var objectType = model.Class == AssetTypeClass.Model ? SystemObjects.TaxonomyType : SystemObjects.PolicyType;
                    var errorMessage = model.Class == AssetTypeClass.Model ? AssetTypeErrors.InvalidModelDepth : AssetTypeErrors.InvalidPolicyDepth;

                    at = new AssetType
                    {
                        uid = uid,
                        Name = model.Name,
                        DisplayFormat = model.DisplayFormat,
                        Description = model.Description,
                        HierarchyMaximumDepth = model.Hierarchy.MaximumDepth,
                        Object = objectType.ToString(),
                        State = State.Active,
                        UpdatedBy = resourceId,
                        UpdatedOn = DateTime.UtcNow,
                        CreatedBy = resourceId,
                        CreatedOn = DateTime.UtcNow,
                        Hierarchical = true,
                        UseAsTransformation = model.UseAsTransformation,
                        Class = model.Class,
                        CanEditParent = model.CanEditParent
                    };

                    if (at.HierarchyMaximumDepth <= 0 || at.HierarchyMaximumDepth > 10)
                        return new Tuple<HttpStatusCode, string, string>(HttpStatusCode.BadRequest, "Invalid Maximum Depth", errorMessage);

                    CompanyContext.Add(at);

                    for (int i = 1; i <= at.HierarchyMaximumDepth; i++)
                    {
                        CompanyContext.Set<AssetTypeLevel>().Add(new AssetTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), AssetTypeID = at.ID });
                    }
                    CompanyContext.SaveChanges();

                    parentType = objectType;
                    model.ObjectID = at.ObjectID;
                    model.Object = objectType.ToString();
                    #endregion
                    break;
                case AssetTypeClass.Reference:
                    #region
                    at = new AssetType
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
                        Class = AssetTypeClass.Reference,
                        CanEditParent = model.CanEditParent
                    };
                    isNamePartOfKey = false;
                    nameFriendlyName = "Long Description";
                    CompanyContext.Add(at);
                    parentType = SystemObjects.ReferenceItemType;
                    model.ObjectID = at.ObjectID;
                    model.Object = SystemObjects.ReferenceItemType.ToString();
                    #endregion
                    break;
                case AssetTypeClass.Diagram:
                    #region
                    at = new AssetType
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
                        Parent = parentAssetType,
                        AutoDisplayParent = model.AutoDisplayParent,
                        FlowObjectType = model.FlowObjectType,
                        CanEditParent = model.CanEditParent
                    };
                    CompanyContext.Add(at);
                    parentType = SystemObjects.TaskType;
                    model.ObjectID = at.ObjectID;
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

        public List<DatabaseBulkAssetResult> PutAssets(List<AssetUpdate> assets, AssetType assetType, ApiExecution execution, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, bool useTempTablesForField = false)
        {
            CompanyContext.Add(execution);

            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = CompanyContext.ImportAssets(execution, assetType, assets, false, sendWorkflowEvents: sendWorkflowEvents, lookupFieldsPassedByValue: lookupFieldsPassedByValue, useTempTablesForField: useTempTablesForField);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);

                // Quick sync of graph.
                try
                {
                    CompanyContext.SynchronizeExecutionAssetsWithGraph(execution.ExecutionID);
                }
                catch
                {
                    // Do nothing, as graph topic will eventually synch.
                }
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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

            if (!model.CanEditParent.HasValue)
            {
                model.CanEditParent = true;
            }

            switch (model.Class)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.Diagram:
                case AssetTypeClass.Model:
                case AssetTypeClass.Policy:
                case AssetTypeClass.Reference:
                case AssetTypeClass.Rule:
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
                    assetType.DisplayFormat = model.DisplayFormat ?? assetType.DisplayFormat;
                    assetType.Description = model.Description;
                    assetType.HierarchyMaximumDepth = (model.Hierarchy != null) ? model.Hierarchy.MaximumDepth : 1;
                    assetType.AutoDisplayDescription = model.AutoDisplayDescription;
                    assetType.AutoDisplayParent = model.AutoDisplayParent;
                    if (model.Class == AssetTypeClass.BusinessAsset || model.Class == AssetTypeClass.TechnicalAsset)
                    {
                        assetType.UseAsTransformation = model.UseAsTransformation;                        
                    }
                    else
                    {
                        assetType.UseAsTransformation = false;                        
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
                        CompanyContext.Delete<AssetTypeLevel>(l => l.Level > assetType.HierarchyMaximumDepth && l.AssetTypeID == assetType.ID);
                    }

                    if (model.Class == AssetTypeClass.Diagram)
                    {
                        assetType.FlowObjectType = model.FlowObjectType;
                    }

                    assetType.CanEditParent = model.CanEditParent;

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
                    assetType.CanEditParent = model.CanEditParent;

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
                results = CompanyContext.RemoveAssets(execution, assetType, assets, sendWorkflowEvents: sendWorkflowEvents);

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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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

            return await CreateApiBatchJob(executionInfo, execution, assetTypes, StorageProvider, QueueSource).ConfigureAwait(false);
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

            return await CreateApiBatchJob(executionInfo, execution, assets, StorageProvider, QueueSource).ConfigureAwait(false);
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

            return await CreateApiBatchJob(executionInfo, execution, assets, StorageProvider, QueueSource).ConfigureAwait(false);
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

            return await CreateApiBatchJob(executionInfo, execution, assets, StorageProvider, QueueSource).ConfigureAwait(false);
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
	                          ,coalesce(ERR.[Message],ex.errormessage) as ErrorMessage
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
                    Fields = JsonConvert.DeserializeObject<dynamic>(f),
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

        public async Task<APIExecutionExternalAPIModelResult> GetConnectorStatusItems(IEnumerable<KeyValuePair<string, string>> queryParams, DateTime? _startDate, DateTime? _endDate, Guid? externalId, string component, string status)
        {
            string orderDirection = "desc";
            var includeTotal = true;
            string orderBySql = "";
            string offsetSql = "";
            var parameters = new DynamicParameters();
            string whereResultCteSql = " ";
            string StrWhereAnd = "";

            if (_startDate.HasValue)
            {
                StrWhereAnd = string.IsNullOrEmpty(StrWhereAnd) ? " where " : " and ";
                whereResultCteSql = StrWhereAnd + " Createdon >= @_startDate";
                parameters.Add("@_startDate", _startDate.Value);
            }

            if (_endDate.HasValue)
            {
                StrWhereAnd = string.IsNullOrEmpty(StrWhereAnd) ? " where " : " and ";

                whereResultCteSql += StrWhereAnd + " Createdon <= @_endDate";
                parameters.Add("@_endDate", _endDate.Value);
            }

            if (externalId.HasValue)
            {
                StrWhereAnd = string.IsNullOrEmpty(StrWhereAnd) ? " where " : " and ";

                whereResultCteSql += StrWhereAnd + " externalId = @externalId";
                parameters.Add("@externalId", externalId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                StrWhereAnd = string.IsNullOrEmpty(StrWhereAnd) ? " where " : " and ";

                whereResultCteSql += StrWhereAnd + " status = @status";
                parameters.Add("@status", status);
            }

            if (!string.IsNullOrEmpty(component))
            {
                StrWhereAnd = string.IsNullOrEmpty(StrWhereAnd) ? " where " : " and ";

                whereResultCteSql += StrWhereAnd + " component = @component";
                parameters.Add("@component", component);
            }

            if (queryParams.Any(x => x.Key == "_direction"))
            {
                string[] allowedDirections = new string[] { "asc", "desc" };
                var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value;
                if (!allowedDirections.Contains(order.Trim().ToLower()))
                {
                    return new APIExecutionExternalAPIModelResult
                    {
                        Message = "Invalid order direction passed in the request",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }
                orderDirection = allowedDirections.Contains(order.Trim().ToLower()) ? order : "desc";
            }

            if (!queryParams.Any(p => p.Key == "_order"))
            {
                orderBySql = $" order by [createdOn] desc, externalid asc";
            }
            else
            {

                var orderByCol = queryParams.FirstOrDefault(p => p.Key == "_order").Value;
                string[] validOrderByFields = { "status", "externalid", "detail", "component", "createdon" };
                if (!validOrderByFields.Contains(orderByCol.ToLower()))
                    return new APIExecutionExternalAPIModelResult
                    {
                        Message = "Invalid order passed in the request",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                orderBySql = $" order by {orderByCol} {orderDirection} ";
            }

            int pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
            int pageSize = CompanyContext.ParsePageSize(queryParams);
            string offset = CompanyContext.ParsePageOffsetSql(pageNum, pageSize);

            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;

                offsetSql = $" offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only ";
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includetotal").Value, out includeTotal);
            }

            var sql = $@"
                        SELECT ee.Status
                              ,ee.externalid
	                          ,ee.detail
                              ,ee.component
                              ,ee.createdOn
							  ,ee.Configuration as 'ConfigurationJSON'
                          FROM [api].[ExecutionExternal] ee
                          {whereResultCteSql}
                          {orderBySql}
                          {offsetSql}
                        ";
            var countSQL = $@"
                        SELECT count(1)
                          FROM [api].[ExecutionExternal] ee
                          {whereResultCteSql}
                        ";
            var executions = await CompanyContext.QueryAsync<APIExecutionExternalAPIModel>(sql, parameters, ApiTimeout);

            foreach (var ex in executions)
            {
                if (!string.IsNullOrEmpty(ex.ConfigurationJSON))
                {
                    ex.Configuration = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(ex.ConfigurationJSON);
                }
            }

            int? count = null;
            if (includeTotal)
            {
                var countresult = await CompanyContext.QueryAsync<int>(countSQL, parameters, ApiTimeout);
                count = countresult.FirstOrDefault();
            }

            var resultsModel = new APIExecutionExternalAPIModelResult
            {
                items = executions,
                total = count,
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

            if (add)
            {
                style = new AssetTypeStyle
                {
                    ID = assetTypeId,
                    IconBackColor = backColor,
                    IconForeColor = foreColor,
                    IconText = IconHelper.GetIconText(objectName),
                    Icon = icon
                };
                CompanyContext.Add(style);
            }
            else
            {
                style.IconBackColor = backColor;
                style.IconForeColor = foreColor;
                style.IconText = IconHelper.GetIconText(objectName);
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
                var useAsTransformationLimit = CompanyContext.GetSettingValue<int>(Setting.UseAsTransformationLimit);
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
	                KP.KeyPath as Path,
                    ADV.DisplayValue,
                    AT.Name as TypeName,
                    A.Object,
                    A.ObjectId,
                    A.Id
                from Asset A
                inner join AssetType AT on AT.ID = A.AssetTypeID and AT.UID = @typeUid
                left join FieldType ft on AT.Object = ft.Object and AT.ObjectID = ft.ObjectID and ft.FriendlyName like 'status'
                left Join Field f on f.FieldTypeID = ft.ID and f.AssetID = A.ID
                left join graph.AssetNode Node on Node.Uid = a.uid and Node.AssetTypeUid = AT.[UID]
                left join graph.AssetNodeKeyPath KP on KP.ID = Node.ID
                left join AssetDisplayValue ADV on ADV.AssetID = A.ID
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

        public async Task<Dictionary<Guid, List<PathComponent>>> GetAssetPathComponents(IEnumerable<Guid> assetUids)
        {
            Dictionary<Guid, List<PathComponent>> paths = new Dictionary<Guid, List<PathComponent>>();

            var sql = @"SELECT an.Uid, an.Segments
                FROM  graph.AssetNode an
                inner join @uids U on U.Uid = an.Uid";

            var nodes = await CompanyContext.QueryAsync<(Guid Uid, string Segments)>(sql, new
            {
                uids = assetUids.Distinct().AsTableValuedParameter(
                        "dbo.UidTable",
                        new List<string>() { "Uid" })
            });

            foreach (var node in nodes)
            {
                List<PathComponent> returnlist = new List<PathComponent>();

                if (!string.IsNullOrWhiteSpace(node.Segments) && node.Segments.IndexOf('<') >= 0)
                {
                    XElement segmentXML = XElement.Parse(node.Segments);
                    List<XElement> segmentList = segmentXML
                        .Descendants("segment")
                        .OrderBy(s => { int.TryParse(s.Attribute("level")?.Value, out int l); return l; })
                        .ThenBy(s => { int.TryParse(s.Attribute("position")?.Value, out int p); return p; })
                        .ToList();
                    int currentlevel = 1;
                    int level = 0;
                    int position = 0;
                    int assetTypeId = -1;
                    List<string> elementPath = new List<string>();

                    foreach (XElement element in segmentList)
                    {
                        if (int.TryParse(element.Attribute("level")?.Value, out level))
                        {
                            if (int.TryParse(element.Attribute("position")?.Value, out position))
                            {
                                if (level != currentlevel)
                                {
                                    returnlist.Add(new PathComponent
                                    {
                                        Key = elementPath.ToArray(),
                                        AssetType = CompanyContext.Filter<AssetType>(i => i.ID == assetTypeId).SingleOrDefault()?.Name
                                    });
                                    currentlevel = level;
                                    elementPath = new List<string>();
                                }
                                elementPath.Add(element.Value);
                                int.TryParse(element.Attribute("assetTypeId")?.Value, out assetTypeId);
                            }
                        }
                    }
                    //capture the last element path
                    if (elementPath.Any())
                    {
                        returnlist.Add(new PathComponent
                        {
                            Key = elementPath.ToArray(),
                            AssetType = CompanyContext.Filter<AssetType>(i => i.ID == assetTypeId).SingleOrDefault()?.Name
                        });
                    }
                }

                paths.Add(node.Uid, returnlist);
            }
            return paths;
        }

        public async Task<List<PathComponent>> GetAssetPath(Guid assetUid)
        {
            var paths = await GetAssetPathComponents(new List<Guid>() { assetUid });
            if (paths.ContainsKey(assetUid))
                return paths[assetUid];
            else
                return new List<PathComponent>();
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
				inner join Asset A on A.Uid = S.AssetUid and S.AssetUid = @assetUid and S.EffectiveDate <= @date 
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

        public async Task<AssetCountsModel> GetAssetCountOfAssetTypeUid(Guid assetTypeUid)
        {
            string assetPermissionWhere = @" and not exists (select 1 
                        from #TempAssetpremission u where u.AssetId = a.id)";

            string assetTypePermissionWhere = @" and not exists (select 1
                    from dbo.AssetTypesUserCantRead(@ResourceID) atp where atp.AssetTypeID = @AssetTypeID)";

            if (CompanyContext.CurrentResourceIsAdmin)
            {
                assetTypePermissionWhere = "";
            }

            var countsSQL = $@"
                                declare @AssetTypeID int = (select id from assettype where uid = @assetTypeUid)
                                drop table if exists #TempAssetpremission;

                                select distinct up.assetid
                                into #TempAssetpremission
                                from dbo.userassetpermissions(@resourceId, @AssetTypeID) up
                                where ((up.permissionsbitmask & @p)) = 0
                                {assetTypePermissionWhere}
                                
                                IF EXISTS(SELECT 1 FROM #TempAssetpremission)
                                    begin
                                        create index idx_TempAssetpremission on #TempAssetpremission(assetid)

                                        select count(1) [Count]
                                        from Asset a
                                        where a.AssetTypeID = @AssetTypeID
                                        {assetPermissionWhere}
                                        {assetTypePermissionWhere}
                                    end
                                else
                                    begin
                                        select count(1) [Count]
                                        from Asset a
                                        where a.AssetTypeID = @AssetTypeID
                                        {assetTypePermissionWhere}
                                    end";
            int count = 0;

            count = (await CompanyContext.QueryAsync<int>(countsSQL, new { ResourceId = CompanyContext.CurrentResourceID, p = (int)Permission.ReadAsset, assetTypeUid }, ApiTimeout)).FirstOrDefault();

            return new AssetCountsModel { count = count };
        }


        public async Task<IEnumerable<AssetTypeCountModel>> GetAssetTypeCounts(int[] filterClasses, IEnumerable<KeyValuePair<string, string>> queryParams, Guid? assetTypeUid = null)
        {
            bool isATUidPassed = false;
            bool isReturnCount = true;
            string strCountstmt = " ";

            string assetTypePermissionWhere = @" and not exists (select 1
                    from dbo.AssetTypesUserCantRead(@ResourceID) atp where atp.AssetTypeID = att.ID)";

            if (CompanyContext.CurrentResourceIsAdmin)
            {
                assetTypePermissionWhere = "";
            }

            if (assetTypeUid.HasValue)
            {
                assetTypePermissionWhere += " and att.uid = @assetTypeUid";
                isATUidPassed = true;
            }

            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "returncount"))
                {
                    bool returncount;
                    var returncountString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "returncount").Value;
                    if (bool.TryParse(returncountString, out returncount))
                    {
                        isReturnCount = returncount;
                    }
                    else
                    {
                        throw new ArgumentException(AssetTypeErrors.InvalidValueReturnCount, returncountString);
                    }
                }
            }

            if (isReturnCount)
            {
                strCountstmt = $@"
                            drop table if exists #TempAssetpremission;
                            drop table if exists #TempAssetCount;

                            CREATE TABLE #TempAssetCount(ASSETTYPEID BIGINT,RecordCount BIGINT);
                            CREATE index idx_TempAssetCount on #TempAssetCount(ASSETTYPEID);

                            select distinct att.id assettypeid, up.assetid
                             into #TempAssetpremission
                            from assettype att
                            cross apply dbo.userassetpermissions(@resourceId, att.id) up
                             where att.class in @filterClasses and((up.permissionsbitmask & @p)) = 0
                            {assetTypePermissionWhere
                                };

                                IF EXISTS(SELECT 1 FROM #TempAssetpremission)
                                                        BEGIN

                                    INSERT INTO #TempAssetCount
                                                            select Att.ID, count(1) RecordCount
                                                             from AssetType Att
                                                            inner join Asset A on Att.ID = A.AssetTypeID
                                                            where att.class in @filterClasses
                                                            {assetTypePermissionWhere
                            }
                            and NOT EXISTS (select 1 from #TempAssetpremission U
                                                            where U.ASSETTYPEID = Att.ID and U.AssetID = A.ID)	
                                                            GROUP BY Att.ID
                                                        END
                                                        ELSE
                                                        BEGIN
                                                            INSERT INTO #TempAssetCount
                                                            select Att.ID , count(1) RecordCount
                                                            from AssetType Att
                                                            inner join Asset A on Att.ID = A.AssetTypeID
                                                            where att.class in @filterClasses
                            { assetTypePermissionWhere}
                            group by Att.ID
                            END ";

            }

            var countsSQL = $@"
                            {(isATUidPassed ? "declare @assetTypeUid uniqueidentifier = (select @assetTypeUidPassed);" : "")}
                            {strCountstmt}                        
                            select att.uid, 
	                        ATParent.uid as parentUid,
	                        case att.class
	                         when 1 then 'Business Asset'
	                         when 8 then 'Technical Asset'
	                         when 2 then 'Model'
	                         when 6 then 'Policy'
	                         when 7 then 'Rule'
                             when 15 then 'Diagram'
	                        end as class,
	                        att.name,
	                        att.description
	                        {(isReturnCount ? ",isnull(Assets.Recordcount,0) as count " : "")}
                         from AssetType att
						 outer apply (select ATParent.uid from IntersectType IT
							inner join [Predicate] P on P.ID = it.PredicateID and P.Type in (3,4)
							inner join [AssetType] ATParent on ATParent.Object = IT.Subject AND ATParent.ObjectID = IT.SubjectID
						 where it.ObjectID = att.ObjectID and it.Object = att.Object
						 )ATParent
                         {(isReturnCount ? " left outer join #TempAssetCount Assets on Assets.ASSETTYPEID = att.ID " : "")}
                        where att.Class in @filterClasses
                         {assetTypePermissionWhere}
                    order by att.name";

            return await CompanyContext.QueryAsync<AssetTypeCountModel>(countsSQL, new { ResourceId = CompanyContext.CurrentResourceID, filterClasses, p = (int)Permission.ReadAsset, assetTypeUidPassed = assetTypeUid }, ApiTimeout);
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
                Fields = JsonConvert.DeserializeObject<dynamic>(f),
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
                results = CompanyContext.RemoveAssetTypes(execution, assetTypes, ApiTimeout, 0); // single endpoint should not retry otherwise timeout will cause 10x attempts to delete.
                                                                                                 // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }

            return results;
        }

        public List<ValidationError> ValidateAssetUpsertModel(List<UpsertModel> model, bool validateFields = true, bool nullifyEmptyFields = false)
        {
            List<ValidationError> errors = new List<ValidationError>();
            foreach (var item in model)
            {
                var assetType = GetAssetTypeByUID(item.AssetTypeUid);
                if (assetType == null)
                {
                    errors.Add(new ValidationError() { Error = "Asset Type not found.", AssetTypeUid = item.AssetTypeUid });
                }

                if (validateFields)
                {
                    foreach (var asset in item.Assets)
                    {
                        bool success = true;
                        string error = "";
                        var fieldTypes = CompanyContext.FieldTypes.Where(x => x.AssetTypeID == assetType.ID).ToList();

                        if (nullifyEmptyFields)
                        {
                            var keys = asset.Fields.Keys.ToList();
                            foreach (string key in keys)
                            {
                                if (string.IsNullOrEmpty(asset.Fields[key]))
                                    asset.Fields[key] = null;
                            }
                        }

                        CompanyContext.ValidateFields(assetType.Object,
                            assetType.ObjectID,
                            true,
                            fieldTypes,
                            fieldTypes.Where(x => (x.IsRequired == true || x.IsPartOfKey) && x.Type != DataType.Counter.ToString()).Select(x => x.Name).ToList(),
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
        KP.KeyPath as [Path] {(fieldColumns.Any() ? "," : "")}
        {string.Join(",\n", fieldColumns)}
from    Asset A
        inner join AssetType T on T.ID = A.AssetTypeID
        left join graph.AssetNodeDisplayPath Node on Node.ID = a.ID 
        left join graph.AssetNodeKeyPath KP on KP.ID = a.ID 
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
        {string.Join("," + Environment.NewLine, fieldColumns)}
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

            return fieldTypes.Select(f => new IndexFieldDisplay()
            {
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

            if ((!string.IsNullOrWhiteSpace(whereSQL)) || (string.IsNullOrWhiteSpace(whereSQL) && CompanyContext.CurrentResourceIsAdmin))
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
                                                CASE WHEN ATET.templatefile IS NULL THEN 0 ELSE 1 END as HasTemplateFile,
                                                (SELECT * from AssetTypeExportTemplateStyle where AssetTypeExportTemplateID = ATET.ID
					                             FOR JSON PATH
					                             ) as AssetTypeExportTemplateStyleJson
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

                    if (template.AssetTypeExportTemplateStyleJson != null)
                    {
                        var styles = JsonConvert.DeserializeObject<ICollection<AssetTypeExportTemplateStyle>>(template.AssetTypeExportTemplateStyleJson ?? "[]");
                        foreach (var style in styles)
                        {
                            style.BgColor = style.BackgroundColor.HasValue ? ColorTranslator.ToHtml(Color.FromArgb(style.BackgroundColor.Value)) : "#FFFFFF";
                            style.TextColor = style.Color.HasValue ? ColorTranslator.ToHtml(Color.FromArgb(style.Color.Value)) : "#000000";
                        }
                        template.AssetTypeExportTemplateStyles = styles;

                        template.AssetTypeExportTemplateStyleJson = null;
                    }
                }
            }

            return templateList;
        }

        public async Task<AssetWatchers> GetAssetWatchers(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            int pageNum = 0;
            int pageSize = 200;
            string orderBy = "name";
            string orderDirection = "asc";
            string offsetSQL = "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            string joinSQL = $@"
                            from FollowDetail F
                            inner join reporting.Global_Resource R on
                            R.ResourceID = F.ResourceID
						    inner join Asset A on F.ObjectID = A.ObjectID and F.ObjectType=A.[Object]
						    where A.[uid]=@assetUid
                            ";

            bool includeTotal = true;

            int? count = 0;

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
                    pageNum = res > 0 ? res - 1 : 0;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includetotal"))
            {
                if (bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includetotal").Value, out bool res))
                {
                    includeTotal = res;
                }
            }

            if (queryParams.Any(q => q.Key == "_order"))
            {
                string[] allowedValues = new string[] { "name", "resourceid" };
                var order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value.Trim().ToLower();
                if (allowedValues.Contains(order))
                {
                    orderBy = order;
                }
            }

            if (queryParams.Any(q => q.Key == "_direction"))
            {
                string[] allowedValues = new string[] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value.Trim().ToLower();

                if (allowedValues.Contains(directionFilter))
                {
                    orderDirection = directionFilter;
                }
            }

            var orderBySQL = $"order by {orderBy} {orderDirection}";

            var dbArgs = new DynamicParameters();
            dbArgs.Add("@assetUid", assetUid);
            dbArgs.Add("@pageSize", pageSize);
            dbArgs.Add("@offset", (pageSize * pageNum));

            var itemsSQL = $@"
                            select R.Uid as resourceUid, R.resourceId, F.FollowerName as 'name'
                            {joinSQL}
                            {orderBySQL}
                            {offsetSQL}
                            ";

            var items = (await CompanyContext.QueryAsync<AssetWatcher>(itemsSQL, dbArgs, timeout: ApiTimeout));

            if (includeTotal)
            {
                var countSQL = $@"
                                SELECT count(*)
                                    {joinSQL}
                                ";
                count = (await CompanyContext.QueryAsync<int>(countSQL, dbArgs, timeout: ApiTimeout)).FirstOrDefault();
            }

            count = includeTotal ? count : null;

            return new AssetWatchers { total = count, items = items };
        }

        public async Task<WatchedAssetTypeDetailModel> GetWatchedAssetDetails(Guid assetTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            int pageNum = 0;
            int pageSize = 200;
            string orderBy = "name";
            string orderDirection = "asc";
            string offsetSQL = "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            string resourceSQL = "";

            bool includeTotal = true;
            var dbArgs = new DynamicParameters();

            int? count = 0;

            if (queryParams.Any(q => q.Key == "resourceUid"))
            {
                if (Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "resourceUid").Value.ToLower(), out Guid resourceUid) && CompanyContext.GlobalReportingResources.Any(u => u.Uid == resourceUid))
                {
                    resourceSQL = $@" and r.uid = @resourceUid";

                    dbArgs.Add("@resourceUid", resourceUid);
                }
            }

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
                    pageNum = res > 0 ? res - 1 : 0;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includetotal"))
            {
                if (bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includetotal").Value, out bool res))
                {
                    includeTotal = res;
                }
            }

            if (queryParams.Any(q => q.Key == "_order"))
            {
                string[] allowedValues = new string[] { "name", "resourceid", "assetdisplayvalue", "governancescore", "dataqualityscore" };
                var order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value.Trim().ToLower();
                if (allowedValues.Contains(order))
                {
                    orderBy = order;
                }
            }

            if (queryParams.Any(q => q.Key == "_direction"))
            {
                string[] allowedValues = new string[] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value.Trim().ToLower();

                if (allowedValues.Contains(directionFilter))
                {
                    orderDirection = directionFilter;
                }
            }

            var orderBySQL = $"order by {orderBy} {orderDirection}";

            dbArgs.Add("@assetTypeUid", assetTypeUid);
            dbArgs.Add("@pageSize", pageSize);
            dbArgs.Add("@offset", (pageSize * pageNum));

            var dataSQL = $@"
                            SELECT
	                            R.uid as resourceUid,
	                            R.ResourceID,	
	                            R.FirstName + ' ' + R.LastName as name,
	                            a.[uid] as assetUid,
	                            AName.DisplayValue as assetDisplayValue,
	                            Governance.Score as governanceScore,
	                            DataQuality.Score as dataQualityScore
                            FROM
	                            Follow f
	                            inner join 
	                            Asset a on a.ObjectID=f.ObjectID and a.[Object]=f.ObjectType and f.FollowTypeID=1
	                            inner join 
	                            AssetType ast on a.AssetTypeID=ast.ID and ast.[uid]=@assetTypeUid
	                            inner join 
	                            reporting.Global_Resource r on r.ResourceId = f.ResourceId {resourceSQL}
	                            cross apply [dbo].[GetAssetDisplayValueById](A.ID) AName
	                            outer apply (select cast(S.Value * 100 as decimal(18,1)) as Score from metrics.Score S
					                            inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = {ScoreType.Governance.ToString("D")} and Al.OverrideName is null
								                            and S.AssetUid = A.[Uid] and S.EffectiveDate <= getutcdate() and (S.EndDate >= getutcdate() or S.EndDate is null)) Governance
	                            outer apply (select cast(S.Value * 100 as decimal(18,1)) as Score from metrics.Score S
					                            inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = {ScoreType.DataQuality.ToString("D")} and Al.OverrideName is null
								                            and S.AssetUid = A.[Uid] and S.EffectiveDate <= getutcdate() and (S.EndDate >= getutcdate() or S.EndDate is null)) DataQuality
                            union
                            select 
                            R.uid as resourceUid,
	                            R.ResourceID,	
	                            R.FirstName + ' ' + R.LastName as name,  	
	                            a.[uid] as assetUid,
	                            AName.DisplayValue as assetDisplayValue,
	                            Governance.Score as governanceScore,
	                            DataQuality.Score as dataQualityScore	
                            FROM
		                            Follow f	
		                            inner join 
		                            AssetType ast on ast.ObjectID=f.ObjectID and ast.[Object]=f.ObjectType and f.FollowTypeID=3 and ast.[uid]=@assetTypeUid
		                            inner join 
		                            Asset a on a.AssetTypeID=ast.ID		
		                            inner join 
	                                reporting.Global_Resource r on r.ResourceId = f.ResourceId {resourceSQL}
		                            cross apply [dbo].[GetAssetDisplayValueById](A.ID) AName
		                            outer apply (select cast(S.Value * 100 as decimal(18,1)) as Score from metrics.Score S
						                            inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = {ScoreType.Governance.ToString("D")} and Al.OverrideName is null
									                            and S.AssetUid = A.[Uid] and S.EffectiveDate <= getutcdate() and (S.EndDate >= getutcdate() or S.EndDate is null)) Governance
		                            outer apply (select cast(S.Value * 100 as decimal(18,1)) as Score from metrics.Score S
						                            inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = {ScoreType.DataQuality.ToString("D")} and Al.OverrideName is null
									                            and S.AssetUid = A.[Uid] and S.EffectiveDate <= getutcdate() and (S.EndDate >= getutcdate() or S.EndDate is null)) DataQuality";

            var itemsSQL = $@"
                            SELECT
                                * 
                            FROM 
                            (
                                {dataSQL}
                            ) items
                            {orderBySQL}
                            {offsetSQL}
                            ";

            var items = (await CompanyContext.QueryAsync<WatchedAssetTypeDetailItemModel>(itemsSQL, dbArgs, timeout: ApiTimeout));

            if (includeTotal)
            {
                var countSQL = $@"
                                SELECT COUNT(*) FROM (
                                    {dataSQL}
                                ) items
                                ";
                count = (await CompanyContext.QueryAsync<int>(countSQL, dbArgs, timeout: ApiTimeout)).FirstOrDefault();
            }

            count = includeTotal ? count : null;

            return new WatchedAssetTypeDetailModel { total = count, items = items };
        }

        public ApiExecutionExternalViewModel AddConnectorStatus(ApiExecutionExternalRequestModel model)
        {
            var result = new ApiExecutionExternalViewModel();

            Guid guid = Guid.Empty;

            if (model?.ExternalId == null || !model.ExternalId.HasValue || model.ExternalId == Guid.Empty)
            {
                guid = Guid.NewGuid();
            }
            else
            {
                guid = (Guid)model.ExternalId;
            }

            result.Status = model.Status;
            result.ExternalId = guid;
            result.Detail = model.Detail;
            result.Component = model.Component;
            result.CreatedOn = DateTime.UtcNow;
            result.Configuration = model.Configuration;

            var ExecutionExternal = new ApiExecutionsExternal
            {
                ExternalId = result.ExternalId,
                Status = result.Status,
                Detail = result.Detail,
                Component = result.Component,
                CreatedOn = (System.DateTime)result.CreatedOn,
                Configuration = model.Configuration != null ? JsonConvert.SerializeObject(model.Configuration) : ""
            };

            //add new issue record

            CompanyContext.Add(ExecutionExternal);

            CompanyContext.SaveChanges();
            return result;
        }

        public IEnumerable<dynamic> GetPossibleOwnersForAssetType(AssetType assetType)
        {
            var sql = $@"
            ; with owners as (select distinct
                    responsibilityTypeId,
		            securityAssetid,
	                '[' + ResponsibilityTypeName + '] - ' + SecurityAssetName as 'Name', 
                    case 
                        when SecurityAsset = 'R' then 'Resource'
						when SecurityAsset = 'O' then 'Organization'
                        when SecurityAsset = 'G' then 'Group'
                        else [Type]
                    end as [Type],
                    SecurityAssetName
                            from ResponsibilityDetail
            where TypeID = @id
                    and[Type] = @Object
                    and IsVisible = 1)
            select Res.SecurityAssetUid as Uid, o.Name, o.Type,o.SecurityAssetName
            from owners o
            cross apply(
            select top 1 * from
            ResponsibilityDetail rd where rd.ResponsibilityTypeID = o.responsibilityTypeId

                                                and rd.SecurityAssetID = o.SecurityAssetID and rd.TypeID = @id and rd.[Type] = @Object
            )Res
            order by o.[Name]
";

            var results = CompanyContext.Query<dynamic>(sql
         , new { id = assetType.ObjectID, assetType.Object }
         , ApiTimeout);
            return results;
        }

    }
}
