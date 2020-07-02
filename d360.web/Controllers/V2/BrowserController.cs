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


        private List<T> parseArrayCount<T>(string json)
        {
            return JsonConvert.DeserializeObject<List<T>>(json ?? "[]");
        }

        private void recurse(AssetBrowserAssetsModel model, List<HopNodeResult> hierarchies, AssetBrowserAssetModel current, int multiplier)
        {
            foreach (var h in hierarchies.Where(h => h.parentKey == current.key && h.key != current.key))
            {
                var child = new AssetBrowserAssetModel
                {
                    key = h.key,
                    parentKey = h.parentKey,
                    assetUid = h.assetUid,
                    assetTypeId = h.assetTypeID,
                    assetTypeUid = h.assetTypeUid,
                    backAmount = ((multiplier <= 4) ? multiplier : 4) * .2,
                    backColor = h.back,
                    foreAmount = 0,
                    foreColor = h.fore,
                    icon = h.icon,
                    @class = h.@class,
                    displayValue = h.displayValue,
                    reveal = h.reveal,
                    actionCount = h.actionCount,
                    ownerCounts = parseArrayCount<AssetBrowserOwnerCountModel>(h.ownerCounts),
                    relationCounts = parseArrayCount<AssetBrowserAssetRelationCountModel>(h.relationCounts),
                    useAsTransformation = h.useAsTransformation,
                    hasAssetReadAccess = h.hasAssetReadAccess,
                    isSubjectInTransformation = h.isSubjectInTransformation
                };
                //child.ownerCounts.ForEach(o => o.Users = JsonConvert.DeserializeObject<List<int>>(o.UsersList));

                recurse(model, hierarchies, child, multiplier + 1);

                if (current.items == null)
                {
                    current.items = new List<AssetBrowserAssetModel>();
                }

                if (!current.items.Any(c => c.key == child.key))
                {
                    current.items.Add(child);
                }
                //model.assets.Add(child);
            }
        }

        private AssetBrowserAssetsModel buildResponseModel(List<HopNodeResult> hierarchies, List<HopLinkResult> relationships, int multiplier)
        {
            var model = new AssetBrowserAssetsModel();

            foreach (var h in hierarchies.Where(i => string.IsNullOrEmpty(i.parentKey)))
            {
                var current = new AssetBrowserAssetModel
                {
                    focal = h.isFocal,
                    key = h.key,
                    assetUid = h.assetUid,
                    assetTypeId = h.assetTypeID,
                    assetTypeUid = h.assetTypeUid,
                    backAmount = ((multiplier <= 4) ? multiplier : 4) * .2,
                    backColor = h.back,
                    foreAmount = 0,
                    foreColor = h.fore,
                    icon = h.icon,
                    @class = h.@class,
                    displayValue = h.displayValue,
                    reveal = h.reveal,
                    actionCount = h.actionCount,
                    ownerCounts = parseArrayCount<AssetBrowserOwnerCountModel>(h.ownerCounts),
                    relationCounts = parseArrayCount<AssetBrowserAssetRelationCountModel>(h.relationCounts),
                    useAsTransformation = h.useAsTransformation,
                    hasAssetReadAccess = h.hasAssetReadAccess,
                    isSubjectInTransformation = h.isSubjectInTransformation
                };
                //current.ownerCounts.ForEach(o => o.Users = JsonConvert.DeserializeObject<List<int>>(o.UsersList));
                recurse(model, hierarchies, current, multiplier + 1);

                if (!model.assets.Any(r => r.key == current.key))
                {
                    model.assets.Add(current);
                }
            }

            model.assetRelations = relationships.Select(r => new AssetBrowserAssetRelationModel
            {
                backColor = "",
                foreColor = "",
                icon = "",
                intersectUid = r.uid,
                objectUid = Guid.NewGuid(),
                objectKey = r.objectKey,
                predicate = r.predicate,
                predicateId = r.predicateId,
                predicateUid = r.predicateUid,
                predicateType = r.predicateType,
                subjectUid = Guid.NewGuid(),
                subjectKey = r.subjectKey
            }).ToList();

            return model;
        }

        private async Task<HopModel> getHop(
            bool initial,
            AssetBrowserDiagramType diagramType,
            AssetBrowserApiHopType hopType,
            List<AssetBrowserApiHopAssetRequestModel> assets,
            List<AssetBrowserApiHopIgnoreRequestModel> ignoredRelations,
            int hopCount,
            AssetBrowserApiHopDirection direction,
            Guid? predicateUid,
            bool leafOnly
            )
        {
            var hopModel = new HopModel();

            // Check to see if keys are populated on incoming assets. If not, populate with auto-generated salt.
            assets.ForEach(a =>
            {
                if (string.IsNullOrEmpty(a.Key))
                {
                    a.Key = "";
                }
            });

            var reader = await Company.QueryMultipleAsync(
                @"exec graph.GetHop @assets, @initial, @hopCount, @diagramType, @hopType, @resourceId, @isAdmin, @ignoredRelations, @direction, @predicateUid, @leafOnly",
                new
                {
                    assets = assets.AsTableValuedParameter("dbo.AssetBrowserImpactTable", new List<string>() { "Key", "Uid" }),
                    initial,
                    hopCount,
                    diagramType = (int)diagramType,
                    hopType = (int)hopType,
                    resourceId = Company.CurrentResourceID,
                    isAdmin = Company.CurrentResourceIsAdmin,

                    ignoredRelations = ignoredRelations.AsTableValuedParameter("dbo.UidTable", new List<string>() { "Uid" }),
                    direction = (direction == AssetBrowserApiHopDirection.Backward) ? "B" : "F",
                    predicateUid,
                    leafOnly
                }, timeout: 60);

            hopModel.nodes = reader.Read<HopNodeResult>().ToList();
            hopModel.links = reader.Read<HopLinkResult>().ToList();

            return hopModel;
        }

        /// <summary>
        /// Retrieves lineage relationships for the specified set of assets.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="criteria">
        /// An object containing:
        /// 1. Assets: A set of asset you want to retrieve lineage for. 
        /// 2. IsReveal: A true/false value indicating whether this call is from clicking a Reveal button, or is from an initial call to get starting lineage.
        /// 3. Direction: An enumeration value (Backward, Both, Forward) indicating the direction you want to traverse when getting relationships. Backward is upstream, Forward is downstream.
        /// 4. Hops: The number of hops, or traversals, you want to pull. The more hops, the slower the API response.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route(""),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(AssetBrowserApiHopRequestModel), typeof(GetAssetLineagePostModelExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserAssetsModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetHop(AssetBrowserApiHopRequestModel criteria)
        {
            try
            {
                var model = await getHop(criteria.Initial, AssetBrowserDiagramType.Lineage, criteria.HopType, criteria.Assets, criteria.RelationsToIgnore, criteria.Hops, criteria.Direction, criteria.PredicateUid, criteria.LeafOnly);
                return Request.CreateResponse(HttpStatusCode.OK, buildResponseModel(model.nodes, model.links, 0));
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
            Route("owners"),
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
        /// Retrieves relationships for the specified set of assets for use in an impact diagram.
        /// </summary>
        /// <remarks>
        /// While this endpoint is used primarily by the Govern Asset Browser tool, external callers may find some data within this endpoint useful.
        /// </remarks>
        /// <param name="criteria">
        /// An object containing:
        /// 1. Assets: A set of asset you want to retrieve lineage for. 
        /// 2. IsReveal: A true/false value indicating whether this call is from clicking a Reveal button, or is from an initial call to get starting lineage.
        /// 3. Direction: An enumeration value (Backward, Both, Forward) indicating the direction you want to traverse when getting relationships. Backward is upstream, Forward is downstream.
        /// 4. Hops: The number of hops, or traversals, you want to pull. The more hops, the slower the API response.
        /// </param>
        /// <returns>An object containing lineage results, as well as an HTTP status code and message.</returns>
        [
            Route("impact"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(AssetBrowserApiHopRequestModel), typeof(GetAssetLineagePostModelExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserAssetsModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetImpactHop(AssetBrowserApiHopRequestModel criteria)
        {
            try
            {
                var model = await getHop(criteria.Initial, AssetBrowserDiagramType.Impact, criteria.HopType, criteria.Assets, criteria.RelationsToIgnore, criteria.Hops, criteria.Direction, criteria.PredicateUid, criteria.LeafOnly);
                return Request.CreateResponse(HttpStatusCode.OK, buildResponseModel(model.nodes, model.links, 0));
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
								ADV.DisplayValue as name,
                                COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), '{{emptycolor}}') as color
								FROM field fi 
								cross apply STRING_SPLIT(fi.Value, ',') SPFfi
								inner join Asset AC on AC.Object = F.LookupObjectType and AC.ObjectID = try_cast(SPFfi.value as int)
								cross apply dbo.GetAssetColorJsonById(AC.Id) ACJ
                                cross apply GetAssetDisplayValueByID(AC.ID) ADV
								 where FieldTypeID = F.ID and fi.AssetID = V.AssetID and F.[Type] = 'Lookup'
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

            if (assetType.Class == AssetTypeClass.BusinessAsset || assetType.Class == AssetTypeClass.Model || assetType.Class == AssetTypeClass.Policy)
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
