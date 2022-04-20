using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core;
using d360.core.entities;
using d360.core.entities.Graph;
using d360.core.enums;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;

using Microsoft.Web.Http;

using Newtonsoft.Json;

using Resources;
using static Dapper.SqlMapper;
using d360.core.exceptions;

using Swashbuckle.Swagger.Annotations;

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
		private readonly IGraphFilterRepository GraphFilterRepository;

		public BrowserController(ICoreComponentSet set, IGraphFilterRepository graphFilterRepository) : base(set)
		{
			GraphFilterRepository = graphFilterRepository;
		}

        private HttpResponseMessage buildAssetBrowserResponseModel(GridReader reader, bool readReveal, bool checkDataLimit = true)
        {
            var model = new AssetBrowserResponseModel
            {
                nodes = reader.Read<AssetBrowserNode>().ToList(),
                links = reader.Read<AssetBrowserLink>().ToList(),
                hierarchy = reader.Read<AssetBrowserHeirarchy>().ToList(),
                reveals = readReveal ? reader.Read<AssetBrowserRevealNode>().ToList() : null,
				dataLimitReached = checkDataLimit ? reader.Read<bool>().First() : false
			};

            return Request.CreateResponse(HttpStatusCode.OK, model);
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
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetInitialLineage(AssetBrowserLineageInitialModel model)
        {
            try
            {
                var showResources = SettingsRepository.GetSettingValue<bool>(Setting.ShowResources);

                var assets = new List<AssetBrowserApiHopAssetRequestModel> { new AssetBrowserApiHopAssetRequestModel { Uid = model.uid } };

                var sql = @"exec graph.AssetBrowser_Lineage 
@ancestry, @descendancy, @direction, 
@assets, @resourceId, @currentHop, @hopCount, @intersects,
@includeOwnershipBadges, @includeRelationBadges, null, @isAdmin";
                var reader = await Company.QueryMultipleAsync(
                    sql,
                    new
                    {
                        ancestry = (int)model.ancestry,
                        descendancy = (int)model.descendancy,
                        direction = "A",
                        assets = assets.AsTableValuedParameter("dbo.UidTable", new List<string> { "Uid" }),
                        resourceId = Company.CurrentResourceID,
                        currentHop = 0,
                        hopCount = model.hopCount,
                        intersects = new List<long>().AsTableValuedParameter("dbo.Ids", new List<string> { "Id" }),
                        includeOwnershipBadges = showResources,
                        includeRelationBadges = true,
                        isAdmin = Company.CurrentResourceIsAdmin
                    },
                    timeout: 60
                );

                return buildAssetBrowserResponseModel(reader, true);
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
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetHopLineage(AssetBrowserLineageHopModel hopModel)
        {
            try
            {
                var showResources = SettingsRepository.GetSettingValue<bool>(Setting.ShowResources);

				if (hopModel.assets == null)
				{
					hopModel.assets = new List<AssetBrowserApiHopAssetRequestModel>();
				}
				if (hopModel.preloadedIntersects == null)
				{
					hopModel.preloadedIntersects = new List<long>();
				}

				var sql = @"exec graph.AssetBrowser_Lineage 
@ancestry, @descendancy, @direction, 
@assets, @resourceId, @currentHop, @hopCount, @intersects,
@includeOwnershipBadges, @includeRelationBadges, @hierarchyKey, @isAdmin";
                var reader = await Company.QueryMultipleAsync(
                    sql,
                    new
                    {
                        ancestry = (int)hopModel.ancestry,
                        descendancy = (int)hopModel.descendancy,
                        direction = (hopModel.direction == AssetBrowserApiHopDirection.Backward) ? "B" : "F",
                        assets = hopModel.assets.AsTableValuedParameter("dbo.UidTable", new List<string> { "Uid" }),
                        resourceId = Company.CurrentResourceID,
                        currentHop = hopModel.currentHop, 
                        hopCount = 1,
                        intersects = hopModel.preloadedIntersects.AsTableValuedParameter("dbo.Ids", new List<string> { "Id" }),
                        includeOwnershipBadges = showResources,
                        includeRelationBadges = true,
                        hierarchyKey = hopModel.hierarchyKey,
                        isAdmin = Company.CurrentResourceIsAdmin
                    },
                    timeout: 60
                );

                return buildAssetBrowserResponseModel(reader, true);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }


        [
            Route("impact/initial"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetInitialImpact(AssetBrowserImpactInitialModel request)
        {
            try
            {
                var showResources = SettingsRepository.GetSettingValue<bool>(Setting.ShowResources);

                var sql = "exec graph.AssetBrowser_Impact @assets, @resourceId, @hopCount, @intersects, @includeOwnershipBadges, @includeRelationshipBadges, @direction, null, null, @isAdmin";
                var assets = new List<AssetBrowserApiHopAssetRequestModel> { new AssetBrowserApiHopAssetRequestModel { Uid = request.uid } };
                var reader = await Company.QueryMultipleAsync(
                    sql,
                    new
                    {
                        assets = assets.AsTableValuedParameter("dbo.UidTable", new List<string> { "Uid" }),
                        resourceId = Company.CurrentResourceID,
                        hopCount = request.hopCount,
                        intersects = new List<long>().AsTableValuedParameter("dbo.Ids", new List<string> { "Id" }),
                        includeOwnershipBadges = showResources,
                        includeRelationshipBadges = true,
                        direction = "A",
                        isAdmin = Company.CurrentResourceIsAdmin
                    },
                    timeout: 60
                );

                return buildAssetBrowserResponseModel(reader, false, false);
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
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetHopImpact(AssetBrowserImpactHopModel hopModel)
        {
            try
            {
                var showResources = SettingsRepository.GetSettingValue<bool>(Setting.ShowResources);
				if (hopModel.assets == null)
				{
					hopModel.assets = new List<AssetBrowserApiHopAssetRequestModel>();
				}
				if (hopModel.intersects == null) 
				{
					hopModel.intersects = new List<long>();
				}
                var sql = "exec graph.AssetBrowser_Impact @assets, @resourceId, @hopCount, @intersects, @includeOwnershipBadges, @includeRelationshipBadges, @direction, @hierarchyKey, @predicateUid, @isAdmin";
                var reader = await Company.QueryMultipleAsync(
                    sql,
                    new
                    {
                        assets = hopModel.assets.AsTableValuedParameter("dbo.UidTable", new List<string> { "Uid" }),
                        resourceId = Company.CurrentResourceID,
                        hopCount = 1,
                        intersects = hopModel.intersects.AsTableValuedParameter("dbo.Ids", new List<string> { "Id" }),
                        includeOwnershipBadges = showResources,
                        includeRelationshipBadges = true,
                        direction = hopModel.direction == AssetBrowserApiHopDirection.Backward ? "B" : "F",
                        hopModel.hierarchyKey,
                        hopModel.predicateUid,
                        isAdmin = Company.CurrentResourceIsAdmin
                    },
                    timeout: 60
                );

                return buildAssetBrowserResponseModel(reader, false, false);
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
            Route("ownership/hop"),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserOwnersModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetOwnerHop(AssetBrowserApiOwnerHopRequestModel criteria)
        {
            try
            {
                var showResources = SettingsRepository.GetSettingValue<bool>(Setting.ShowResources);
                if (!showResources)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Environment Conflict", "Your environment does not allow retrieval of owners in Asset Browser.");
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
order by R.ResourceName", new { assetUids = criteria.assets.Select(i => i.Uid).ToList(), criteria.responsibilityTypeId });

                foreach (var o in owners)
                {
                    o.key = $"{criteria.hierarchyKey}_{criteria.responsibilityTypeId}|{o.resourceId}";
                    if (!distinctOwners.Any(d => d.key == o.key))
                    {
                        distinctOwners.Add(o);
                    }
                }

                var ownerRelations = (
                                     from o in owners
                                     join a in criteria.assets on o.assetUid equals a.Uid
                                     select new AssetBrowserOwnerRelationModel
                                     {
                                         assetKey = a.Key,
                                         assetUid = a.Uid,
                                         backColor = o.backColor,
                                         foreColor = o.foreColor,
                                         ownerKey = o.key,
                                         ownerUid = o.resourceUid
                                     }).ToList();

                return Request.CreateResponse(
                    HttpStatusCode.OK, 
                    new AssetBrowserOwnersModel 
                    { 
                        owners = distinctOwners, 
                        ownerRelations = ownerRelations 
                    }
                );
            }
            catch (GenericException)
            {
                throw;
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
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
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> GetUserAssetBrowserFilters()
		{
			try
			{
				var fil = GraphFilterRepository.GetGraphFiltersByUser(Company.CurrentResourceID);

				return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, fil))).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

				SendException(ex, new Dictionary<string, string>() {
					{ "Endpoint Method", "BrowserController.GetUserAssetBrowserFilters" },
				});

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
				{
					return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));
				}
				else
				{
					return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = ApiMessages.SaveFailedMessage, Success = false, Uid = Guid.Empty }));
				}

			}
			catch (Exception ex)
			{
				string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
				{
					return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, ApiMessages.FilterNotFound));
				}

				if (orig.OwnedBy != Company.CurrentResourceID)
				{
					return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, ApiMessages.FilterNotOwned));
				}

				orig.Name = model.Name;
				orig.IsPublic = model.IsPublic;
				orig.Settings = model.Settings;

				GraphFilterRepository.UpdateGraphFilter(orig);

				return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, orig));

			}
			catch (Exception ex)
			{
				string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
				{
					return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, ApiMessages.FilterNotFound));
				}

				if (model.OwnedBy != Company.CurrentResourceID)
				{
					return ResponseMessage(Request.CreateResponse(HttpStatusCode.Unauthorized, ApiMessages.FilterNotOwned));
				}

				GraphFilterRepository.DeleteGraphFilter(model);

				return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = ApiMessages.FilterRemove, Success = true, Uid = Guid.Empty }));

			}
			catch (Exception ex)
			{
				string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
			{
				return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidAssetUid));
			}

			var asset = (await Company.QueryAsync<Asset>("select * from Asset where uid = @uid", new { uid })).FirstOrDefault();

			if (asset == null)
			{
				return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.AssetNotFound, uid.ToString())));
			}

			var assetType = (await Company.QueryAsync<AssetType>("select * from AssetType where id = @assetTypeID", new { asset.AssetTypeID })).FirstOrDefault();

			if (assetType == null)
			{
				return ResponseMessage(Request.CreateResponse(HttpStatusCode.NotFound, ApiMessages.AssetTypeNotFoundForAsset));
			}


			var items = new List<dynamic>();
			int? initial = (int)AssetBrowserDiagramType.Lineage;

			var includeImpact = SettingsRepository.GetSettingValue<bool>(Setting.ShowImpactSidebar);
			var includeLineage = SettingsRepository.GetSettingValue<bool>(Setting.ShowLineageSidebar) && assetType.Class != AssetTypeClass.ReferenceItemType;
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
					value = (int)AssetBrowserDiagramType.Lineage
				});
			}

			var canEdit = Company.HasAssetPermission(asset.ID, Permission.EditAsset);

			if (anyDiagramRelationTypes)
			{
				if (anyProcessDiagram || canEdit)
				{
					items.Add(new
					{
						label = "Process Diagram",
						value = (int)AssetBrowserDiagramType.Process,
						canEdit
					});
				}
			}

			if (includeImpact)
			{
				items.Add(new
				{
					label = "Impact Diagram",
					value = (int)AssetBrowserDiagramType.Impact
				});
			}

			var diagramTypes = new List<AssetTypeClass>
			{
				AssetTypeClass.BusinessAsset,
				AssetTypeClass.Model,
				AssetTypeClass.Policy,
				AssetTypeClass.Rule
			};

			if (diagramTypes.Contains(assetType.Class))
			{
				if (anyDiagramRelationTypes && (anyProcessDiagram || canEdit))
				{
					initial = (int)AssetBrowserDiagramType.Process;

				}
				else if (includeImpact)
				{
					initial = (int)AssetBrowserDiagramType.Impact;
				}
			}
			else if (assetType.Class == AssetTypeClass.TechnicalAsset)
			{
				if (includeLineage)
				{
					initial = (int)AssetBrowserDiagramType.Lineage;
				}
			}

			return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { initial, items }));
		}
	}
}
