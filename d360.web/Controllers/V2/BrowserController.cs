using d360.core.entities;
using d360.model;
using Microsoft.Web.Http;
using System;
using System.Web.Http;
using d360.core;
using System.Linq;
using System.Data.SqlClient;
using d360.core.enums;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using d360.web.Filters;
using Swashbuckle.Swagger.Annotations;
using d360.web.Models;
using System.Web.Http.Description;
using System.Security.Cryptography;
using System.Text;
using d360.core.entities.Views;
using d360.model.DataAccessLayer;
using d360.core.entities.Graph;
using DocumentFormat.OpenXml.EMMA;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service supports all asset browser functionality for Lineage version 3.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/browser"),
        Authorize,
        StringEnumController
    ]
    public class BrowserController : BaseV2ApiController
    {
        IGraphFilterRepository GraphFilterRepository;

        public BrowserController(ICommunityContext community, ICompanyContext company, IGraphFilterRepository graphFilterRepository) : base(community, company)
        {
            GraphFilterRepository = graphFilterRepository;
        }

        public class AssetBrowserResponseModel
        {
            public List<AssetBrowserNode> nodes { get; set; }
            public List<AssetBrowserLink> links { get; set; }
            public List<AssetBrowserHeirarchy> hierarchy { get; set; }
            public List<AssetBrowserRevealNode> reveals { get; set; }
        }

        public class AssetBrowserNodeOwnerCount 
        {
            public string key { get; set; }
            public bool expanded { get; set; }
            //[JsonIgnore]
            //public string usersList { get; set; }
            //public List<int> users { get { return JsonConvert.DeserializeObject<List<int>>(usersList ?? "[]"); } }
            public int count { get; set; }
            public int responsibilityTypeId { get; set; }
            public string responsibilityType { get; set; }
        }

        public class AssetBrowserNodeRelationCount
        {
            public string key { get; set; }
            public string predicate { get; set; }
            public int predicateId { get; set; }
            public Guid predicateUid { get; set; }
            public int direction { get; set; }
            public int count { get; set; }
            public bool expanded { get; set; }
        }

        public class AssetBrowserHeirarchy
        {
            public string hierarchyKey { get; set; }
            public int backwardReveal { get; set; }
            public int forwardReveal { get; set; }
            [JsonIgnore]
            public string ownersJson { get; set; }
            public List<AssetBrowserNodeOwnerCount> owners { get { return JsonConvert.DeserializeObject<List<AssetBrowserNodeOwnerCount>>(ownersJson ?? "[]"); } }
            [JsonIgnore]
            public string relationsJson { get; set; }
            public List<AssetBrowserNodeRelationCount> relations { get { return JsonConvert.DeserializeObject<List<AssetBrowserNodeRelationCount>>(relationsJson ?? "[]"); } }
        }

        public class AssetBrowserRevealNode
        {
            public string hierarchyKey { get; set; }
            public string from { get; set; }
            public string to { get; set; }
            public AssetBrowserApiHopDirection direction { get; set; }
        }

        public class AssetBrowserNode
        {
            public string hierarchyKey { get; set; }
            public bool focal { get; set; }
            public bool leaf { get; set; }
            public string key { get; set; }
            public string group { get; set; }
            public Guid? assetUid { get; set; }
            public int assetTypeId { get; set; }
            public Guid assetTypeUid { get; set; }
            public decimal backAmount { get; set; }
            public string back { get; set; }
            public string icon { get; set; }
            public AssetTypeClass @class { get; set; }
            public string text { get; set; }
            
            public int actionCount { get; set; }
            public bool useAsTransformation { get; set; }
            public bool hasAssetReadAccess { get; set; }
            public bool isSubjectInTransformation { get; set; }
        }

        public class AssetBrowserChildLink
        {
            public long id { get; set; }
            public string from { get; set; }
            public string to { get; set; }
        }

        public class AssetBrowserLink
        {
            public string from { get; set; }
            public string to { get; set; }
            public string back { get; set; }
            public int predicateId { get; set; }
            public Guid predicateUid { get; set; }
            public string text { get; set; }
            public int predicateType { get; set; }
            [JsonIgnore]
            public string linksJson { get; set; }
            public List<AssetBrowserChildLink> links { get { return JsonConvert.DeserializeObject<List<AssetBrowserChildLink>>(linksJson ?? "[]"); } }
        }

        public enum AssetBrowserAncestry
        {
            AllAncestors = 1,
            DirectAncestor = 2,
            TypeOnly = 3 //For Impact
        }

        public class AssetBrowserInitialModel
        {
            public AssetBrowserAncestry ancestry { get; set; }
            public Guid uid { get; set; }
            public int hopCount { get; set; }
        }

        public class AssetBrowserImpactInitialModel
        {
            public Guid uid { get; set; }
            public int hopCount { get; set; }
        }

        public class AssetBrowserLineageInitialModel
        {
            public AssetBrowserAncestry ancestry { get; set; }
            public Guid uid { get; set; }
            public int hopCount { get; set; }
        }

        public abstract class AssetBrowserHopModelBase
        {
            public string hierarchyKey { get; set; }
        }
        
        public abstract class AssetBrowserHopModelRelationBase: AssetBrowserHopModelBase
        {
            public List<AssetBrowserApiHopAssetRequestModel> assets { get; set; }
            public List<long> preloadedIntersects { get; set; }
            public AssetBrowserApiHopDirection direction { get; set; }
        }

        public class AssetBrowserLineageHopModel: AssetBrowserHopModelRelationBase
        {
        }

        public class AssetBrowserImpactHopModel : AssetBrowserHopModelRelationBase
        {
            public AssetBrowserAncestry ancestry { get; set; }
            public Guid predicateUid { get; set; }
        }

        public class AssetBrowserOwnershipHopModel: AssetBrowserHopModelBase
        {
            public List<AssetBrowserApiHopAssetRequestModel> assets { get; set; }
            public int responsibilityTypeId { get; set; }
        }


        async Task<HttpResponseMessage> getInitial(AssetBrowserInitialModel postModel)
        {
            try
            {
                var sql = "exec graph.AssetBrowser_Initial @ancestry, @uid, @resourceId, @isAdmin, @hopCount";
                var reader = await Company.QueryMultipleAsync(
                    sql,
                    new
                    {
                        ancestry = (int)postModel.ancestry,
                        postModel.uid,
                        resourceId = Company.CurrentResourceID,
                        isAdmin = Company.CurrentResourceIsAdmin,
                        postModel.hopCount
                    },
                    timeout: 60
                );

                var model = new AssetBrowserResponseModel
                {
                    nodes = reader.Read<AssetBrowserNode>().ToList(),
                    links = reader.Read<AssetBrowserLink>().ToList(),
                    hierarchy = reader.Read<AssetBrowserHeirarchy>().ToList(),
                    reveals = reader.Read<AssetBrowserRevealNode>().ToList()
                };

                if (model.reveals.Count == 1) {
                    if (model.reveals[0].direction == AssetBrowserApiHopDirection.None) {
                        model.reveals = null;
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        /// <summary>
        /// Retrieves lineage relationships for the specified set of assets.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="model">
        /// An object containing:
        /// 1. ancestry: AllAncestors, DirectAncestor, TypeOnly. 
        /// 2. uid: The Uid of the asset you are initially loading lineage for.
        /// 4. hopCount: The number of hops, or traversals, you want to pull. The more hops, the slower the API response.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route("lineage/initial"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserAssetsModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetInitialLineage(AssetBrowserLineageInitialModel model)
        {
            var o = new AssetBrowserInitialModel { ancestry = model.ancestry, hopCount = model.hopCount, uid = model.uid };
            return await getInitial(o);
        }

        [
            Route("impact/initial"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserAssetsModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetInitialImpact(AssetBrowserImpactInitialModel model)
        {
            var o = new AssetBrowserInitialModel { ancestry = AssetBrowserAncestry.TypeOnly, hopCount = model.hopCount, uid = model.uid };
            return await getInitial(o);
        }

        /// <summary>
        /// Retrieves relationships for the specified set of assets for use in an impact diagram.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="hopModel">
        /// An object containing:
        /// 1. assets: A set of asset you want to retrieve lineage for. 
        /// 2. preloadedIntersects: A true/false value indicating whether this call is from clicking a Reveal button, or is from an initial call to get starting lineage.
        /// 3. direction: An enumeration value (Backward, Both, Forward) indicating the direction you want to traverse when getting relationships. Backward is upstream, Forward is downstream.
        /// 4. ancestry: AllAncestors, DirectAncestor, TypeOnly.
        /// 5. predicateuid: The Uid of the predicate you want to pull non-lineage relationships for.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route("impact/hop"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserAssetsModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetHopImpact(AssetBrowserImpactHopModel hopModel)
        {
            try
            {
                var sql = "exec graph.AssetBrowser_ImpactHop @ancestry, @hierarchyKey, @assets, @preloadedIntersects, @predicateUid, @direction, @resourceId, @isAdmin";
                var reader = await Company.QueryMultipleAsync(
                    sql,
                    new
                    {
                        ancestry = (int)hopModel.ancestry,
                        hopModel.hierarchyKey,
                        assets = hopModel.assets.AsTableValuedParameter("dbo.AssetBrowserImpactTable", new List<string>() { "Key", "Uid" }),
                        preloadedIntersects = hopModel.preloadedIntersects.AsTableValuedParameter("dbo.Ids", new List<string>() { "Id" }),
                        hopModel.predicateUid,
                        direction = (hopModel.direction == AssetBrowserApiHopDirection.Backward) ? "B" : "F",
                        resourceId = Company.CurrentResourceID,
                        isAdmin = Company.CurrentResourceIsAdmin
                    },
                    timeout: 60
                );

                var model = new AssetBrowserResponseModel
                {
                    nodes = reader.Read<AssetBrowserNode>().ToList(),
                    links = reader.Read<AssetBrowserLink>().ToList(),
                    hierarchy = reader.Read<AssetBrowserHeirarchy>().ToList(),
                    reveals = null
                };

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        [
            Route("lineage/hop"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserAssetsModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetHopLineage(AssetBrowserLineageHopModel hopModel)
        {
            try
            {
                var sql = "exec graph.AssetBrowser_LineageHop @hierarchyKey, @assets, @preloadedIntersects, @direction, @resourceId, @isAdmin";
                var reader = await Company.QueryMultipleAsync(
                    sql,
                    new
                    {
                        hopModel.hierarchyKey,
                        assets = hopModel.assets.AsTableValuedParameter("dbo.AssetBrowserImpactTable", new List<string>() { "Key", "Uid" }),
                        preloadedIntersects = hopModel.preloadedIntersects.AsTableValuedParameter("dbo.Ids", new List<string>() { "Id" }),
                        direction = (hopModel.direction == AssetBrowserApiHopDirection.Backward) ? "B" : "F", 
                        resourceId = Company.CurrentResourceID,
                        isAdmin = Company.CurrentResourceIsAdmin
                    },
                    timeout: 60
                );

                var model = new AssetBrowserResponseModel
                {
                    nodes = reader.Read<AssetBrowserNode>().ToList(),
                    links = reader.Read<AssetBrowserLink>().ToList(),
                    hierarchy = reader.Read<AssetBrowserHeirarchy>().ToList(),
                    reveals = reader.Read<AssetBrowserRevealNode>().ToList()
                };

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        /// <summary>
        /// Retrieves owners for the specified set of assets.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="criteria">
        /// An object containing:
        /// 1. Assets: A set of asset you want to retrieve lineage for. 
        /// 2. IsReveal: A true/false value indicating whether this call is from clicking a Reveal button, or is from an initial call to get starting lineage.
        /// 3. Hops: A starting point, which will be used to generate unique key values that are used in the Asset Browser UI.
        /// 4. Direction: An enumeration value (Backward, Both, Forward) indicating the direction you want to traverse when getting relationships. Backward is upstream, Forward is downstream.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route("ownership/hop", Order = 1), Route("owners", Order = 2),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserOwnersModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetOwnerHop(AssetBrowserApiOwnerHopRequestModel criteria)
        {
            try
            {
                var model = new HopModel
                {
                    links = new List<HopLinkResult>(),
                    nodes = new List<HopNodeResult>()
                };

                string hashKey(string input)
                {
                    string returnValue = "";

                    var hash = new SHA1Managed().ComputeHash(Encoding.UTF8.GetBytes(input));
                    returnValue = string.Concat(hash.Select(b => b.ToString("x2")));
                    returnValue = returnValue.Substring(2, returnValue.Length - 3);

                    return returnValue;
                }

                var distinctOwners = new List<AssetBrowserOwnerModel>();

                var owners = await Company.QueryAsync<AssetBrowserOwnerModel>(@"
select	distinct
		A.Uid as assetUid,
		R.ResourceName as displayValue, 
		'fa-user' as icon,
		RE.Uid as resourceUid,
        R.resourceId
from	ResponsibilityDetail R
		inner join Asset A on ( (A.ID = R.AssetID) OR (R.AssetID = 0 and A.AssetTypeID = R.AssetTypeID) ) and R.IsVisible = 1
		inner join reporting.Global_Resource RE on RE.ResourceID = R.ResourceID
where	A.Uid in @assetUids
        and R.ResponsibilityTypeID = @ResponsibilityTypeId
order by R.ResourceName", new { assetUids = criteria.Assets.Select(i => i.Uid).ToList(), criteria.ResponsibilityTypeId });

                // Check to see if keys are populated on incoming assets. If not, populate with auto-generated salt.
                foreach (var o in owners)
                {
                    o.key = hashKey($"{criteria.ResponsibilityTypeId}|{o.resourceId}");
                    if (!distinctOwners.Any(d => d.key == o.key))
                    {
                        distinctOwners.Add(o);
                    }
                }

                var ownerRelations = from o in owners
                                     join a in criteria.Assets on o.assetUid equals a.Uid
                                     select new AssetBrowserOwnerRelationModel
                                     {
                                         assetKey = a.Key,
                                         assetUid = a.Uid,
                                         backColor = o.backColor,
                                         foreColor = o.foreColor,
                                         ownerKey = o.key,
                                         ownerUid = o.resourceUid
                                     };

                return Request.CreateResponse(HttpStatusCode.OK, new { owners = distinctOwners, ownerRelations });
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        /// <summary>
        /// Gets detailed field information regarding a specific asset that a user selects from the Asset Browser UI.
        /// </summary>
        /// <param name="uid">The uid of the asset that we are getting field information for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            Route("diagramasset/{uid:Guid}"),
            ApiExplorerSettings(IgnoreApi = true),
            HttpGet,
            MapToApiVersion("2.0"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the GET request.", typeof(AssetBrowserDiagramAsset)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetDiagramAsset(Guid uid)
        {
            try
            {
                var ignoredFields = this.CalculatedFieldTypes;
                ignoredFields.Add(DataType.Tag.ToString());
                ignoredFields.Add(DataType.Relationship.ToString());
                ignoredFields.Add(DataType.FieldFromRelationship.ToString());
                ignoredFields.Add(DataType.Score.ToString());

                var sql = $@"
select	A.TypeName,
		A.Uid,
        A.AssetTypeClass,
		A.DisplayValue,
		P.DisplayPath as [Path],
		dbo.GenerateAssetUrl(A.ID) as Url,
		(
			select  *
            from    (
                    select	F.ColumnOrder,
							F.FriendlyName as Name,
					       COALESCE(fv.value,V.FormattedValue) as Value,
                            '[]' as [Values],
					         (CASE
								WHEN fv.value is not null THEN 'Color'
								ELSE F.Type 
							END) as Type
			        from	utility.FieldValue V
					        inner join FieldType F on F.ID = V.FieldTypeID and F.[Type] not in @ignoredFields
                             outer apply(
							select value = (
								SELECT 
								 CASE
				                    WHEN (F.AllowMultipleValues = 0) THEN COALESCE(fi.FormattedValue, ADV.DisplayValue, AC.Code)
				                    ELSE COALESCE(ADV.DisplayValue, AC.Code)
			                     END as name,
                                JSON_VALUE(ACJ.ColorJSON,'$.Value') as color
								FROM field fi 
								cross apply STRING_SPLIT(fi.Value, ',') SPFfi
								inner join Asset AC on AC.Object = F.LookupObjectType and AC.ObjectID = try_cast(SPFfi.value as int)
								cross apply dbo.GetAssetColorJsonById(AC.Id) ACJ
                                cross apply GetAssetDisplayValueByID(AC.ID) ADV
								 where FieldTypeID = F.ID and fi.AssetID = V.AssetID and F.[Type] = 'Lookup' and F.LookupObjectType = 'ReferenceItem'
								for json path)
							)FV
			        where	AssetID = A.ID
					        and F.IsDisplayable = 1 
                            and (F.ShowIfEmpty = 1 OR (F.ShowIfEmpty = 0 AND V.FormattedValue <> '' and V.FormattedValue IS NOT NULL))
                    union all
                    select	F.ColumnOrder,
							F.FriendlyName as Name,
					        null as Value,
                            TV.[Values],
					        F.Type
			        from	FieldType F 
                            inner join Asset TA on TA.ID = A.ID and F.AssetTypeID = TA.AssetTypeID and F.[Type] = 'Tag'
                            cross apply (
                                select	( 
										select  T.Value,
												'tag' as TooltipType,
												T.ID as TooltipID,
												T.CreatedBy,
												'Preview' as TooltipContext,
												'' as TooltipUrl,
												T.[uid]
										from    AssetTag TJ
												inner join Tag T on T.ID = TJ.TagID and TJ.AssetID = TA.ID
										for json path
										) as [Values]
                            ) TV
                    ) F
			order by F.ColumnOrder
			for json path
		) as Fields,
		(
			select S.AssetUid,
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
                where S.AssetUid = @uid and EndDate is null and EffectiveDate <= getUtcDate()
			for json path
		) as Scores,
		(
			select	distinct
                    ResponsibilityTypeID,
					ResponsibilityTypeName,
					ResourceID,
					ResourceName
			from	ResponsibilityDetail
			where	IsVisible = 1 
					and AssetID = A.ID
			for json path
		) as Owners
from	AssetDetail A
        inner join graph.AssetNodeDisplayPath P on P.ID = A.ID
where	A.Uid = @uid
for json path, WITHOUT_ARRAY_WRAPPER";

                var reader = await Company.QueryAsync<string>(sql, new { uid, ignoredFields }, timeout: 90);
                var json = string.Join("", reader);

                var model = JsonConvert.DeserializeObject<AssetBrowserDiagramAsset>(json);

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        /// <summary>
        /// Retrieves lists of filters to be used in the Asset Browser. Hidden from Swagger as this is an internal API.
        /// </summary>
        /// <returns>Lists of filters for the asset browser.</returns>
        [
            Route("filters"),
            ApiExplorerSettings(IgnoreApi = true),
            HttpGet,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetBrowserFilters()
        {
            try
            {
                #region

                var sql = @"
with H as	(
			select	O.Class,
					O.[uid],
					O.ID as AssetTypeID,
					O.Object,
					O.ObjectID,
					cast(O.Name as nvarchar(2500)) as [Path],
					cast(null as int) as ParentAssetTypeID,
					1 as [Level]
			from	AssetType O
					outer apply (
								select	I.ID 
								from	IntersectType I 
										inner join [Predicate] P on P.ID = I.PredicateID and P.[Type] = 3 and I.Object = O.Object and I.ObjectID = O.ObjectID
								) I
			where	I.ID is null
					and O.Class in (1,7,8)
			union all
			select	O.Class,
					O.[uid],
					O.ID as AssetTypeID,
					O.Object,
					O.ObjectID,
					cast(H.Path + ' > ' + O.Name as nvarchar(2500)) as [Path],
					H.AssetTypeID as ParentAssetTypeID,
					H.[Level]+1 as [Level]
			from	AssetType O
					inner join IntersectType I on I.Object = O.Object and I.ObjectID = O.ObjectID
					inner join H on H.Object = I.Subject and H.ObjectID = I.SubjectID
					inner join [Predicate] P on P.ID = I.PredicateID and P.[Type] = 3
			)

select	*
from	(
		select		[Uid], [Path], AssetTypeID, Class as ClassId, [Level]
		from		H 
		where		[Level] = 1
					or AssetTypeID in (
										select	A.ID
										from	AssetType A
												inner join	(
															select	I.Subject,
																	I.SubjectID
															from	AssetType O
																	inner join IntersectType I on I.Object = O.Object and I.ObjectID = O.ObjectID
																	inner join [Predicate] P on P.ID = I.PredicateID and P.[Type] in (3,4)
																	left join IntersectType SI on SI.Subject = O.Object and SI.SubjectID = O.ObjectID and SI.PredicateID = P.ID
															where	O.Class in (1,7,8)
																	and SI.ID is null
															) S on S.Subject = A.Object and S.SubjectID = A.ObjectID			
									)
		union
		select	O.[Uid],
				cast(O.Name as nvarchar(2500)) as [Path],
				O.ID as AssetTypeID,
				O.Class as ClassId,
				1 as [Level]
		from	AssetType O
		where	O.Class in (2,6)
		) H
order by	ClassId, [Path];

select	Id,
        [Uid],
		[Type] as TypeId,	
		[Name],
		[Inverse]
from	[Predicate]
where	[Type] in (6,7,9,14)
order by [Type], [Name];

select  Id,
        [Uid],
        Name
from    ResponsibilityType
order by Name";

                #endregion

                var reader = await Company.QueryMultipleAsync(sql, timeout: 60);

                var assetTypes = reader.Read<AssetBrowserAssetTypeFilterItem>().ToList();
                var predicates = reader.Read<AssetBrowserPredicateFilterItem>().ToList();
                var responsibilityTypes = reader.Read<AssetBrowserResponsibilityTypeFilterItem>().ToList();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    AssetTypeOptions = assetTypes,
                    PredicateOptions = predicates,
                    ResponsibilityTypeOptions = responsibilityTypes
                });
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }


        /// <summary>
        /// Retrieves lists of filters to be used in the Asset Browser. Hidden from Swagger as this is an internal API.
        /// </summary>
        /// <returns>Lists of filters for the asset browser.</returns>
        [
            Route("filters/me"),
            HttpGet,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetUserAssetBrowserFilters()
        {
            try
            {
                var fil = GraphFilterRepository.GetGraphFiltersByUser(Company.CurrentResourceID);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, fil)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", "BrowserController.GetUserAssetBrowserFilters" },
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Create an asset browser filter
        /// </summary>
        /// <returns>The saved filter</returns>
        [
            HttpPost,
            Route("filters"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Created, "Filter created.", typeof(GraphFilter)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request badly formatted.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "Unknown error.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "No permissions.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> CreateAssetBrowserFilter(GraphFilter model)
        {
            try
            {
                if (GraphFilterRepository.CreateGraphFilter(model))
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
                else
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Save failed", Success = false, Uid = Guid.Empty }));

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Update asset browser filter
        /// </summary>
        /// <param name="uid">The public identifier for the filter.</param>
        /// <param name="model"></param>
        /// <returns>The saved filter</returns>
        [
            HttpPut,
            Route("filters/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.Created, "Filter updated.", typeof(GraphFilter)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request badly formatted.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "Unknown error.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "No permissions.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> UpdateAssetBrowserFilterById(Guid uid, GraphFilter model)
        {
            try
            {
                GraphFilter orig = GraphFilterRepository.GetGraphFilterByUid(uid);

                if (orig == null)
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Filter not found."));

                if (orig.OwnedBy != Company.CurrentResourceID)
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "Filter not owned by user."));

                orig.Name = model.Name;
                orig.IsPublic = model.IsPublic;
                orig.Settings = model.Settings;

                GraphFilterRepository.UpdateGraphFilter(orig);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, orig));

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Allows you to remove a user filter based on its Uid.
        /// </summary>
        /// <param name="uid">The public identifier for the filter.</param>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route("filters/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate that the metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> DeleteAssetBrowserFilterById(Guid uid)
        {
            try
            {
                GraphFilter model = GraphFilterRepository.GetGraphFilterByUid(uid);

                if (model == null)
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "Filter not found."));

                if (model.OwnedBy != Company.CurrentResourceID)
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, "Filter not owned by user."));

                GraphFilterRepository.DeleteGraphFilter(model);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Filter removed.", Success = true, Uid = Guid.Empty }));

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Returns a list of available diagram types for the current user and asset, as well as the default view
        /// </summary>
        /// <param name="uid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("types/{uid:Guid}/me"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of available diagram types.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetDiagramTypes(Guid uid)
        {
            if (uid == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified"));

            var asset = (await Company.QueryAsync<Asset>("select * from Asset where uid = @uid", new { uid })).FirstOrDefault();

            if (asset == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "The asset for this uid could not be found"));

            var assetType = (await Company.QueryAsync<AssetType>("select * from AssetType where id = @assetTypeID", new { asset.AssetTypeID })).FirstOrDefault();

            if (assetType == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, "The asset type for this asset could not be found"));


            var items = new List<dynamic>();
            int? initial = ((int)AssetBrowserDiagramType.Lineage);

            var includeImpact = Community.GetCompanySettingByKey<bool>("ShowImpactSidebar");
            var includeLineage = Community.GetCompanySettingByKey<bool>("ShowLineageSidebar") && assetType.Class != AssetTypeClass.ReferenceItemType;
            var anyDiagramRelationTypes = (await Company.QueryAsync<bool>("select case when count(*) > 0 then 1 else 0 end from IntersectTypeDetail D where D.PredicateType = @predicateType and D.SubjectUid = @uid ", new { assetType.uid, predicateType = (int)PredicateType.Diagram })).SingleOrDefault();
            bool anyProcessDiagram = false;

            if (anyDiagramRelationTypes)
            {
                anyProcessDiagram = (await Company.QueryAsync<bool>(@"select case when count(*) > 0 then 1 else 0 end from 
                Asset A 
                left join dbo.AssetProcessDiagram APD ON APD.AssetID = A.ID
                where A.ID = @assetId and APD.Diagram is not null",
                   new { assetId = asset.ID })).SingleOrDefault();
            }

            if (includeLineage)
            {
                items.Add(new
                {
                    label = "Lineage Diagram",
                    value = ((int)AssetBrowserDiagramType.Lineage)
                }); ;
            }

            var canEdit = Company.HasAssetPermission(asset.ID, Permission.ModifyAsset);

            if (anyDiagramRelationTypes)
            {

                if (anyProcessDiagram || canEdit)
                {
                    items.Add(new
                    {
                        label = "Process Diagram",
                        value = ((int)AssetBrowserDiagramType.Process),
                        canEdit
                    });
                }
            }

            if (includeImpact)
            {
                items.Add(new
                {
                    label = "Impact Diagram",
                    value = ((int)AssetBrowserDiagramType.Impact)
                });
            }

            var diagramTypes = new List<AssetTypeClass>();
            diagramTypes.Add(AssetTypeClass.BusinessAsset);
            diagramTypes.Add(AssetTypeClass.Model);
            diagramTypes.Add(AssetTypeClass.Policy);
            diagramTypes.Add(AssetTypeClass.Rule);

            if (diagramTypes.Contains(assetType.Class))
            {
                if (anyDiagramRelationTypes && (anyProcessDiagram || canEdit))
                {
                    initial = ((int)AssetBrowserDiagramType.Process);

                }
                else if (includeImpact)
                {
                    initial = ((int)AssetBrowserDiagramType.Impact);
                }
            }
            else if (assetType.Class == AssetTypeClass.TechnicalAsset)
            {
                if (includeLineage)
                {
                    initial = ((int)AssetBrowserDiagramType.Lineage);
                }
            }

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { initial, items }));
        }
    }
}
