using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.model.DataAccessLayer;
using d360.model.DataAccessLayer.repositories;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using d360.web.Utilities;
using Microsoft.Web.Http;
using Resources;
using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/responsibilities"),
        Authorize
    ]
    public class ResponsibilitiesController : BaseV2ApiController
    {
        private readonly IAssetRepository AssetRepository;
        private readonly IResponsibilityRepository ResponsibilityRepository;
        private readonly IResourceRepository ResourceRepository;
        private readonly IAssetService AssetService;

        public ResponsibilitiesController(ICoreComponentSet set,
            IAssetRepository assetRepository,
            IResponsibilityRepository responsibilityRepository,
            IResourceRepository resourceRepository,
            IAssetService assetService
            )
            : base(set)
        {
	        ResponsibilityRepository = responsibilityRepository;
            ResourceRepository = resourceRepository;
            AssetRepository = assetRepository;
            AssetService = assetService;
        }

		/// <summary>
		/// Retrieves a list of all responsibility types.
		/// </summary>
		/// <returns>Returns a list of responsibility types.</returns>
		[
			HttpGet,
			RequireAdminPermissions,
			Route("types"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of responsibility types.", typeof(List<ResponsibilityTypeViewModel>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
		]
		public async Task<IHttpActionResult> GetResponsibilityTypesAsync()
		{
			var responsibilityTypes = await ResponsibilityRepository.GetResponsibilityTypes();

			return Ok(responsibilityTypes);
		}

		/// <summary>
		/// Retrieves a responsibility type.
		/// </summary>
		/// <returns>Returns a responsibility type.</returns>
		[
            HttpGet,
			RequireAdminPermissions,
			Route("type/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A responsibility type.", typeof(List<ResponsibilityTypeViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied"),
			ApiExplorerSettings(IgnoreApi = true)
		]
        public async Task<IHttpActionResult> GetResponsibilityTypeAsync(Guid uid)
        {
           dynamic responsibilityTypes = await ResponsibilityRepository.GetResponsibilityType(uid);

           return Ok(new { data = responsibilityTypes });
        }

		/// <summary>
		/// Get a list of all claims that are available for assignment.
		/// </summary>
		/// <returns>Returns a list of claims for assignment</returns>
		[
			HttpGet,
			Route("claims"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of claims for assignment.", typeof(ICollection<ClaimsViewModel>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetClaimsAsync()
		{
			var claims = await ResponsibilityRepository.GetClaimsAsync();

			return Ok(claims);
		}

		/// <summary>
		/// Retrieves a list of responsibility types that are applicable for the specified AssetTypeUid.
		/// </summary>
		/// <param name="assetTypeUid">The unique identifier of the asset type.</param>
		/// <returns>Returns a list of responsibility types.</returns>
		[
            HttpGet,
            Route("types/{assetTypeUid:guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of responsibility types.", typeof(List<ResponsibilityTypeViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Asset Type based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<IHttpActionResult> GetResponsibilityTypesByAssetTypeAsync(Guid assetTypeUid)
        {
            var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

            if (assetType == null)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));
            }

            if (!Company.HasAssetTypePermission(assetType.Object, assetType.ID, Permission.ReadAsset))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            IEnumerable<ResponsibilityTypeViewModel> responsibilityTypes = await ResponsibilityRepository.GetResponsibilityTypesByAssetUid(assetTypeUid);
            return Ok(responsibilityTypes);
        }

        /// <summary>
        /// Retrieves a list of all allocations for the specified responsibility type.
        /// </summary>
        /// <param name="responsibilityTypeUid">The unique identifier of the responsibility type to get allocations for.</param>
        /// <returns>Returns a list of asset types a responsibility rule is allocated to.</returns>
        [
            HttpGet,
			RequireAdminPermissions,
            Route("types/{responsibilityTypeUid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type allocations for the given responsibility type uid.", typeof(List<ResponsibilityTypeAllocationViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<IHttpActionResult> GetResponsibilityTypeAllocationsAsync(Guid responsibilityTypeUid)
        {
            IEnumerable<ResponsibilityTypeAllocationViewModel> responsibilityTypeAllocations = await ResponsibilityRepository.GetResponsibilityTypeAllocations(responsibilityTypeUid);
            
			return Ok(responsibilityTypeAllocations);
        }

        /// <summary>
        /// Retrieves a list of all allocations for the specified asset type.
        /// </summary>
        /// <param name="assetTypeUid">The unique identifier of the asset type to get allocations for.</param>
        /// <returns>Returns a list of responsibility types and allocations for an asset type.</returns>
        [
            HttpGet,
			RequireAdminPermissions,
			Route("typesbyasset/{assetTypeUid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type allocations for the given responsibility type uid.", typeof(List<ResponsibilityTypeAllocationViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<IHttpActionResult> GetResponsibilityTypeAllocationsByAssetAsync(Guid assetTypeUid)
		{
			IEnumerable<ResponsibilityTypeAllocationViewModel> responsibilityTypeAllocations = await ResponsibilityRepository.GetResponsibilityTypeAllocationsByAsset(assetTypeUid);
			
			return Ok(responsibilityTypeAllocations);
        }

        /// <summary>
        /// Adds a list of all allocations for the specified asset.
        /// </summary>
        /// <param name="uid">The Uid of the responsibility type.</param>
        /// <param name="model">A list of assetType Uids and permissions to add allocations for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
			RequireAdminPermissions,
            Route("types/{uid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(ResponsibilityTypeAllocationInsertModel), typeof(ResponsibilityTypeAllocationExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeAllocationResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add responsibility type allocations.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostResponsibilityTypeAllocations(Guid uid, IEnumerable<ResponsibilityTypeAllocationInsertModel> model)
        {
			List<ResponsibilityTypeAllocationResponseModel> results = new List<ResponsibilityTypeAllocationResponseModel>();

			//valdiate the responsibilitytype uid passed in
			ResponsibilityType responsibility = Company.Filter<ResponsibilityType>(x => x.UID == uid).FirstOrDefault();

			if (responsibility == null)
			{
				throw new ArgumentException(ResponsibilityApiMessages.InvalidResponsibilityUid);
			}

			foreach (var allocation in model)
			{
				AssetType assetType = Company.Filter<AssetType>(x => x.uid == allocation.AssetTypeUid).FirstOrDefault();

				if (assetType == null)
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = ActionApiMessages.InvalidAssetTypeUid,
						Success = false
					});
					continue;
				}

				List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>()
					{
						AssetTypeClass.BusinessAsset,
						AssetTypeClass.TechnicalAsset,
						AssetTypeClass.Model,
						AssetTypeClass.Rule,
						AssetTypeClass.Policy,
						AssetTypeClass.Reference
					};

				if (!allowedClasses.Contains(assetType.Class))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = string.Format(ResponsibilityApiMessages.InvalidAssetTypeClass, assetType.Class.ToString()),
						Success = false
					});

					continue;
				}

				var validValues = Permission.DeleteAsset.GetList().Select(x => x.Value);
				if (allocation.Permissions.Any(x => !validValues.Contains(x)))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = string.Format(ResponsibilityApiMessages.InvalidPermissionProvided, string.Join(",", allocation.Permissions.Where(x => !validValues.Contains(x)).ToArray())),
						Success = false
					});

					continue;
				}

				if (Company.ResponsibilityTypeRelations.Any(x => x.ObjectType == assetType.Object && x.ObjectID == assetType.ObjectID && x.ResponsibilityTypeID == responsibility.ID))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = ActionApiMessages.UniqueAllocation,
						Success = false
					});

					continue;
				}

				results.Add(ResponsibilityRepository.AddAllocation(responsibility, assetType, allocation.Permissions));
			}

			return Ok(results);
		}

        /// <summary>
        /// Edits a list of all allocations for the specified asset.
        /// </summary>
        /// <param name="uid">The Uid of the responsibility type.</param>
        /// <param name="model">A list of assetType Uids and permissions to edit allocations.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
			RequireAdminPermissions,
            Route("types/{uid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(ResponsibilityTypeAllocationInsertModel), typeof(ResponsibilityTypeAllocationExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeAllocationResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to edit responsibility type allocations.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PutResponsibilityTypeAllocations(Guid uid, IEnumerable<ResponsibilityTypeAllocationInsertModel> model)
        {
			List<ResponsibilityTypeAllocationResponseModel> results = new List<ResponsibilityTypeAllocationResponseModel>();

			ResponsibilityType responsibility = Company.Filter<ResponsibilityType>(x => x.UID == uid).FirstOrDefault();
			if (responsibility == null)
			{
				throw new ArgumentException(ResponsibilityApiMessages.InvalidResponsibilityUid);
			}

			foreach (var allocation in model)
			{
				AssetType assetType = Company.Filter<AssetType>(x => x.uid == allocation.AssetTypeUid).FirstOrDefault();
				if (assetType == null)
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = ActionApiMessages.InvalidAssetTypeUid,
						Success = false
					});

					continue;
				}

				List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>()
					{
						AssetTypeClass.BusinessAsset,
						AssetTypeClass.TechnicalAsset,
						AssetTypeClass.Model,
						AssetTypeClass.Rule,
						AssetTypeClass.Policy,
						AssetTypeClass.ReferenceItemType
					};
				if (!allowedClasses.Contains(assetType.Class))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = string.Format(ResponsibilityApiMessages.InvalidAssetTypeClass, assetType.Class.ToString()),
						Success = false
					});

					continue;
				}

				var validValues = Permission.DeleteAsset.GetList().Select(x => x.Value);
				if (allocation.Permissions.Any(x => !validValues.Contains(x)))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = string.Format(ResponsibilityApiMessages.InvalidPermissionProvided, string.Join(",", allocation.Permissions.Where(x => !validValues.Contains(x)).ToArray())),
						Success = false
					});

					continue;
				}

				if (!Company.ResponsibilityTypeRelations.Any(x => x.ObjectType == assetType.Object && x.ObjectID == assetType.ObjectID && x.ResponsibilityTypeID == responsibility.ID))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = ResponsibilityApiMessages.AllocationNotFound,
						Success = false
					});

					continue;
				}

				results.Add(ResponsibilityRepository.EditAllocation(responsibility, assetType, allocation.Permissions));
			}

			return Ok(results);
		}

        /// <summary>
        /// Deletes a list of all allocations for the specified asset.
        /// </summary>
        /// <param name="uid">The Uid of the responsibility type.</param>
        /// <param name="model">A list of assetType Uids to delete allocations for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
			RequireAdminPermissions,
            Route("types/{uid:Guid}/allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeAllocationResponseModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete responsibility type allocations.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteResponsibilityTypeAllocationsAsync(Guid uid, ResponsibilityTypeAllocationDeleteModel model)
        {
			List<ResponsibilityTypeAllocationResponseModel> results = new List<ResponsibilityTypeAllocationResponseModel>();

			ResponsibilityType responsibility = Company.Filter<ResponsibilityType>(x => x.UID == uid).FirstOrDefault();
			if (responsibility == null)
			{
				throw new ArgumentException(ResponsibilityApiMessages.InvalidResponsibilityUid);
			}

			foreach (var allocation in model.Items)
			{
				AssetType assetType = Company.Filter<AssetType>(x => x.uid == allocation.AssetTypeUid).FirstOrDefault();
				if (assetType == null)
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = ActionApiMessages.InvalidAssetTypeUid,
						Success = false
					});

					continue;
				}

				List<AssetTypeClass> allowedClasses = new List<AssetTypeClass>()
					{
						AssetTypeClass.BusinessAsset,
						AssetTypeClass.TechnicalAsset,
						AssetTypeClass.Model,
						AssetTypeClass.Rule,
						AssetTypeClass.Policy,
						AssetTypeClass.ReferenceItemType
					};

				if (!allowedClasses.Contains(assetType.Class))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = string.Format(ResponsibilityApiMessages.InvalidAssetTypeClass, assetType.Class.ToString()),
						Success = false
					});

					continue;
				}

				if (!Company.ResponsibilityTypeRelations.Any(x => x.ObjectType == assetType.Object && x.ObjectID == assetType.ObjectID && x.ResponsibilityTypeID == responsibility.ID))
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = ResponsibilityApiMessages.AllocationNotFound,
						Success = false
					});

					continue;
				}

				string ownershipLookupMessage = ResponsibilityRepository.GetResponsibilityTypeUsedInOwnershipLookupMessage(responsibility, assetType);

				if (ownershipLookupMessage != "")
				{
					results.Add(new ResponsibilityTypeAllocationResponseModel()
					{
						AssetTypeUid = allocation.AssetTypeUid,
						Message = ownershipLookupMessage,
						Success = false
					});

					continue;
				}

				results.Add(await ResponsibilityRepository.DeleteAllocation(responsibility, assetType, model.Cascade));
			}

			return Ok(results);
		}

        /// <summary>
        /// Retrieves a list of responsibility type ownership rules for the specified responsibility type.
        /// </summary>
        /// <param name="responsibilityTypeUid">The unique identifier of the responsibility type to get responsibility type ownership rules for.</param>
        /// <returns>Returns a list of responsibility type ownership rules.</returns>
        [
            HttpGet,
			RequireAdminPermissions,
			Route("types/{responsibilityTypeUid:Guid}/ownershiprules"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of responsibility type ownership rules for the given responsibility type uid.", typeof(List<ResponsibilityTypeRuleViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<IHttpActionResult> GetResponsibilityRulesForTypeAsync(Guid responsibilityTypeUid)
        {
            IEnumerable<ResponsibilityTypeRuleViewModel> responsibilityTypeRules = await ResponsibilityRepository.GetResponsibilityRules(responsibilityTypeUid);
            
			return Ok(responsibilityTypeRules);
        }

        /// <summary>
        /// Retrieves a list of responsibility type ownership rules for the specified responsibility type.  Rules applied to groups and organizations are enumerated to the actual count of users contained therein.  Rules applying to a type are enumerated down to the count of assets within the given type.
        /// </summary>
        /// <param name="responsibilityTypeRuleUid">The unique identifier of the responsibility type ownership rule to get stats for.</param>
        /// <returns>Returns a stats for the specified responsibility type ownership rules.</returns>
        [
            HttpGet,
			RequireAdminPermissions,
			Route("rules/{responsibilityTypeRuleUid:Guid}/stats"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Ownership rule statistics for the given responsibility type rule uid.", typeof(ResponsibilityTypeRuleStatsViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")
        ]
        public async Task<IHttpActionResult> GetResponsibilityRulesStats(Guid responsibilityTypeRuleUid)
        {
            ResponsibilityTypeRuleStatsViewModel responsibilityTypeRuleStats = await ResponsibilityRepository.GetResponsibilityRuleStats(responsibilityTypeRuleUid);
            
			return Ok(responsibilityTypeRuleStats);
        }

        /// <summary>
        /// Retrieves a list of assets with ownership based on the provided parameters.  Assets and ownership results reflect the users permissions to see the assets and the ownership details for them.  If a user doesnt have access to see an asset then they will not be able to see the asset or its ownership.  If a user does have access to see an asset but doesn't have access to see the assets ownership, the asset will be returned without any ownership details.  No filters applied will return all items which have at least one owner.  Only assets with ownership are returned by this API.  By default 5 assets are returned at a time the max page size is 250 assets.  Please keep in mind that assets with lots of owners will impact response time / size.
        /// </summary>   
        /// <permission>Admin or Ownership read required</permission>
        /// <returns>Returns a list of assets and there corresponding ownership information.</returns>
        [
            HttpGet,
            Route("assignments"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Ownership rule statistics for the given responsibility type rule uid.", typeof(AssetResponsibilityItemModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 assets per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "The Uid of a asset to return ownership for. If specified the results will include ownership of this asset.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetTypeUid", "The Uid of a asset type to return ownership for. If specified the results will include ownership of this asset type only.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_responsibilityTypeUid", "The Uid of a responsibility type to return ownership for. If specified the results will include ownership of assets that include this responsibility type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assigneeUid", "The Uid of an assignee to return ownership for. If specified the results will include assets for which the specified user is an owner.  In order to use this filter you must specify in addition the _assetTypeUid or _assetUid filter as well.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetResponsibilities()
        {
			var queryParams = Request.GetQueryNameValuePairs();

			Guid responsibilityUidFilter = Guid.Empty;
			Guid assigneeUidFilter = Guid.Empty;
			Guid assetUidFilter = Guid.Empty;
			Guid assetTypeUidFilter = Guid.Empty;
			string pageSize = "5";
			string pageNum = "1";
			var timeout = 300;

			foreach (var q in queryParams.ToList())
			{
				var key = q.Key.ToLower();

				if (key.StartsWith("_"))
				{
					switch (key)
					{
						case "_pagesize":
							pageSize = q.Value;
							break;
						case "_pagenum":
							pageNum = q.Value;
							break;
						case "_responsibilitytypeuid":
							if (!Guid.TryParse(q.Value, out responsibilityUidFilter) || responsibilityUidFilter == Guid.Empty)
							{
								throw new ArgumentException(string.Format(Messages.Error_Parameter_InvalidUidValue, ResponsibilityApiMessages._responsibilitytypeuid));
							}
							break;
						case "_assigneeuid":
							if (!Guid.TryParse(q.Value, out assigneeUidFilter) || assigneeUidFilter == Guid.Empty)
							{
								throw new ArgumentException(string.Format(Messages.Error_Parameter_InvalidUidValue, ResponsibilityApiMessages._assigneeuid));
							}
							break;
						case "_assettypeuid":
							if (!Guid.TryParse(q.Value, out assetTypeUidFilter) || assetTypeUidFilter == Guid.Empty)
							{
								throw new ArgumentException(string.Format(Messages.Error_Parameter_InvalidUidValue, ResponsibilityApiMessages._assettypeuid));
							}
							break;
						case "_assetuid":
							if (!Guid.TryParse(q.Value, out assetUidFilter) || assetUidFilter == Guid.Empty)
							{
								throw new ArgumentException(string.Format(Messages.Error_Parameter_InvalidUidValue, ResponsibilityApiMessages._assetuid));
							}
							break;
						case "_timeout":
							if (int.TryParse(q.Value, out timeout))
							{
								if (timeout < 1)
								{
									timeout = 30; // min timeout
								}
							}
							break;
					}
				}
			}

			Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
			string isValid = isPageSizeAndNumValid(pageParams);

			if (!string.IsNullOrEmpty(isValid))
			{
				throw new ArgumentException(isValid);
			}

			//validation dont allow assigneeuid filter across entire universe
			if (assigneeUidFilter != Guid.Empty && assetTypeUidFilter == Guid.Empty && assetUidFilter == Guid.Empty)
			{
				throw new ArgumentException(ResponsibilityApiMessages.assigneeUidFilterValidation);
			}

			int.TryParse(pageSize, out int _pageSize);
			int.TryParse(pageNum, out int _pageNum);

			AssetResponsibilitiesApiModel res = await ResponsibilityRepository.GetResponsibilities(queryParams, responsibilityUidFilter, assigneeUidFilter, assetUidFilter, assetTypeUidFilter, _pageSize, _pageNum, timeout);

			return Ok(res);
		}

        /// <summary>
        /// Inserts responsibility types of a given responsibility types list.
        /// </summary>
        /// <param name="responsibilityTypes">The list of responsibility types for insertion.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
			RequireAdminPermissions,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ResponsibilityTypeUpsertResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add responsibility types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult InsertResponsibilityTypes(List<ResponsibilityTypeInsertModel> responsibilityTypes)
        {
			if (responsibilityTypes == null)
			{
				responsibilityTypes = readRequestJsonContent<List<ResponsibilityTypeInsertModel>>(Request, true).Result;
			}

			if (responsibilityTypes == null)
			{
				throw new ArgumentException(ApiMessages.JSONValidMessage);
			}

			if (responsibilityTypes.Count == 0)
			{
				throw new ArgumentException(RelationshipsApiMessages.PredicateRequired);
			}

			if (responsibilityTypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
			{
				throw new ArgumentException(string.Format(RelationshipsApiMessages.PredicateLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT));
			}

			foreach (var type in responsibilityTypes)
			{
				if (type.Name?.Trim().Length > 250)
				{
					throw new ArgumentException(ActionApiMessages.NameMaxLength250Char);
				}

				if (type.Description?.Trim().Length > 4000)
				{
					throw new ArgumentException(string.Format(MetricsApiMessages.DescriptionLengthValidation, type.Description?.Trim().Length));
				}
			}

			var existingUids = Company.Query<Guid>("select uid from responsibilitytype where uid in @uids", new { uids = responsibilityTypes.Where(x => x.Uid.HasValue).Select(x => x.Uid) }).ToList();

			if (existingUids.Any())
			{
				string errorMessage = string.Format(ResponsibilityApiMessages.ResponsibilityUidNonUnique, string.Join(", ", existingUids.Select(i => i.ToString())));

				throw new ArgumentException(errorMessage);
			}

			var execution = getApiExecution(responsibilityTypes.Count);

			var upserts = new List<ResponsibilityTypeUpsertModel>();
			upserts = responsibilityTypes.ConvertAll(x => new ResponsibilityTypeUpsertModel()
			{
				Name = x.Name,
				Description = x.Description,
				Uid = x.Uid,
				IsNew = true
			});

			List<ResponsibilityTypeUpsertResult> results = ResponsibilityRepository.UpsertResponsibilityTypes(upserts, execution);

			return Ok(results);
		}

        /// <summary>
        /// Retrieves all ownership records for the provided asset uid.
        /// </summary>   
        /// <returns>Returns all ownership records for the current asset.</returns>
        [
            HttpGet,
            Route("assignments/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "All ownership records for the current asset.", typeof(OwnershipApiModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Asset Uid item doesn't exist or is not a valid type for ownership."),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetOwnershipOfAsset(string assetUid)
        {
			Guid assetId;
			if (!Guid.TryParse(assetUid, out assetId))
			{
				throw new ArgumentException(ActionApiMessages.InvalidAssetUid);
			}

            if (!Company.Assets.Any(x => x.uid == assetId))
            {
				throw new NotFoundBusinessLayerException();
            }

            var res = await ResponsibilityRepository.GetOwnership(assetId);

			return Ok(res);
        }

        /// <summary>
        /// Checks whether or not the asset with the given uid has any ownership records.
        /// </summary>   
        /// <returns>Returns true or false.</returns>
        [
            HttpGet,
            Route("hasassignments/{assetUid}"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A boolean representing whether or not the asset has ownership records.", typeof(bool)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Asset Uid item doesn't exist or is not a valid type for ownership."),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetHasOwnership(string assetUid)
        {
			Guid assetId;
			if (!Guid.TryParse(assetUid, out assetId))
			{
				throw new ArgumentException(ActionApiMessages.InvalidAssetUid);
			}

			if (!Company.Assets.Any(x => x.uid == assetId))
			{
				throw new NotFoundBusinessLayerException();
			}

			var res = await ResponsibilityRepository.HasOwnership(assetId);

			return Ok(res);
		}

        /// <summary>
        /// Updates responsibility types of a given responsibility types list.
        /// </summary>
        /// <param name="responsibilityTypes">The list of responsibility types for update.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
			RequireAdminPermissions,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the PUT request.", typeof(List<ResponsibilityTypeUpsertResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpdateResponsibilityTypes(List<ResponsibilityTypeUpsertModel> responsibilityTypes)
        {
			if (responsibilityTypes == null)
			{
				responsibilityTypes = await readRequestJsonContent<List<ResponsibilityTypeUpsertModel>>(Request, true);
			}

			if (responsibilityTypes == null)
			{
				throw new ArgumentException(ApiMessages.JSONValidMessage);
			}

			if (responsibilityTypes.Count == 0)
			{
				throw new ArgumentException(RelationshipsApiMessages.PredicateRequired);
			}

			if (responsibilityTypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
			{
				throw new ArgumentException(string.Format(RelationshipsApiMessages.PredicateLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT));
			}

			foreach (var type in responsibilityTypes)
			{
				if (type.Name?.Trim().Length > 250)
				{
					throw new ArgumentException(ActionApiMessages.NameMaxLength250Char);
				}

				if (type.Description?.Trim().Length > 4000)
				{
					throw new ArgumentException(string.Format(MetricsApiMessages.DescriptionLengthValidation, type.Description?.Trim().Length));
				}
			}

			var execution = getApiExecution(responsibilityTypes.Count);

			List<ResponsibilityTypeUpsertResult> results = ResponsibilityRepository.UpsertResponsibilityTypes(responsibilityTypes, execution);
			return Ok(results);
		}

        /// <summary>
        /// Deletes responsibility type.
        /// </summary>
        /// <param name="responsibilityTypes">Responsibility type Uid for deletion.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
			RequireAdminPermissions,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ResponsibilityTypeDeleteResult)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteResponsibilityTypes(ResponsibilityTypeDeleteModel responsibilityTypes)
        {
			if (responsibilityTypes == null)
			{
				throw new ArgumentException(ApiMessages.JSONValidMessage);
			}

			ResponsibilityTypeDeleteResult results = ResponsibilityRepository.DeleteResponsibilityTypes(responsibilityTypes);

			return Ok(results);
		}

        /// <summary>
        /// Adds responsibility override to asset for a given Resource Uid list.
        /// </summary>
        /// <param name="assetUid">Uid of an Asset.</param>
        /// <param name="responsibilityUid">Uid of Responsibility type.</param>
        /// <param name="model">An object containing list of Resource/Group Uids and description (context).</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("{assetUid:guid}/{responsibilityUid:guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility override.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult AddResponsibilitiesOverride(Guid assetUid, Guid responsibilityUid, [FromBody] ResponsibilityOverridePostModel model)
        {
			var asset = AssetRepository.GetAssetByUID(assetUid);

			if (asset == null)
			{
				throw new ArgumentException(string.Format(ActionApiMessages.AssetNotFound, assetUid.ToString()));
			}

			var responsibility = ResponsibilityRepository.GetResponsibilityTypeByUID(responsibilityUid);

			if (responsibility == null)
			{
				throw new ArgumentException(string.Format(ResponsibilityApiMessages.ResponsibilityUidNotExist, responsibilityUid.ToString()));
			}

			if (!Company.HasAssetPermission(asset.ID, Permission.AddResponsibilities))
			{
				throw new UnauthorizedBusinessLayerException(ApiMessages.EndpointNotAuthorizedMessage);
			}

			bool isValidResponsibilityForAsset = ResponsibilityRepository.IsValidResponsibilityForAsset(responsibilityUid, assetUid);

			if (!isValidResponsibilityForAsset)
			{
				throw new ArgumentException(ResponsibilityApiMessages.ReposibilityTypeNotValidForAsset);
			}

			if (model.ResourceUid.Count == 0)
			{
				throw new ArgumentException(ResponsibilityApiMessages.ResourceUidNotEmpty);
			}

			if (model.ResourceUid.Any(x => x == Guid.Empty))
			{
				throw new ArgumentException(ResponsibilityApiMessages.ResourceUidInvalid);
			}

			var securityAssets = ResponsibilityRepository.GetSecurityAssetModelsForResources(model.ResourceUid, asset.uid, responsibility.UID).ToList();

			if (securityAssets.Any(x => string.IsNullOrEmpty(x.SecurityAsset)))
			{
				var badAsset = securityAssets.First(x => string.IsNullOrEmpty(x.SecurityAsset));
				throw new ArgumentException(string.Format(ResponsibilityApiMessages.InvalidResourceGroupUid, badAsset.uid.ToString()));
			}

			if (securityAssets.Any(x => x.Exists == true))
			{
				var badAsset = securityAssets.First(x => x.Exists == true);
				throw new ArgumentException(string.Format(ResponsibilityApiMessages.ReponsibilityOverrideExists, badAsset.uid.ToString()));
			}

			foreach (var uid in model.ResourceUid)
			{
				var sas = securityAssets.FirstOrDefault(x => x.uid == uid);
				if (sas == null)
				{
					throw new ArgumentException(string.Format(ResponsibilityApiMessages.ResourceGroupUidNotExists, uid.ToString()));
				}
			}

			ResponsibilityRepository.InsertResponsibilityOverrides(responsibility, asset, securityAssets, model.Description);

			return Ok(new ConfirmResponse { title = ApiMessages.Success, message = ResponsibilityApiMessages.ResponsibilitySuccessAddMessage });
		}

        /// <summary>
        /// Allows Bulk addition of responsibility overrides to assets for given Resource Uids.
        /// </summary>
        /// <param name="overrides">List of responsibility overrides.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
			RequireAdminPermissions,
            MapToApiVersion("2.0"),
            Route("batch"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> BulkAddResponsibilitiesOverride(List<BulkResponsibilityOverridePostModel> models)
        {
			var execution = getApiExecution(models.Count);
			ApiExecutionInfo executionInfo = await ResponsibilityRepository.PostBatchResponsibilityOverride(models, execution);

			var result = new ApiExecutionRecievedResponse
			{
				ExecutionID = executionInfo.ExecutionID,
				Message = ApiMessages.ExecutionIDStatus,
				Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/executions/{executionInfo.ExecutionID}"
			};

			return Ok(result);
		}

        /// <summary>
        /// Deletes responsibility overrides from asset for a given Resource Uid list.
        /// </summary>
        /// <param name="assetUid">Uid of an Asset.</param>
        /// <param name="responsibilityUid">Uid of Responsibility type.</param>
        /// <param name="resourceUids">An object which contains list of Resource/Group Uids.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{assetUid:guid}/{responsibilityUid:guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(ResponsibilityOverrideDeleteModel), typeof(ResponsibilitiesDeleteExample)),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility override.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteResponsibilitiesOverride(Guid assetUid, Guid responsibilityUid, [FromBody] List<ResponsibilityOverrideDeleteModel> resourceUids)
        {
			var asset = AssetRepository.GetAssetByUID(assetUid);

			if (asset == null)
			{
				throw new ArgumentException(string.Format(ActionApiMessages.AssetNotFound, assetUid.ToString()));
			}

			var responsibility = ResponsibilityRepository.GetResponsibilityTypeByUID(responsibilityUid);

			if (responsibility == null)
			{
				throw new ArgumentException(string.Format(ResponsibilityApiMessages.ResponsibilityUidNotExist, responsibilityUid.ToString()));
			}

			if (!Company.HasAssetPermission(asset.ID, Permission.DeleteResponsibilities))
			{
				throw new UnauthorizedBusinessLayerException(ApiMessages.EndpointNotAuthorizedMessage);
			}

			bool isValidResponsibilityForAsset = ResponsibilityRepository.IsValidResponsibilityForAsset(responsibilityUid, assetUid);

			if (!isValidResponsibilityForAsset)
			{
				throw new ArgumentException(ResponsibilityApiMessages.ReposibilityTypeNotValidForAsset);
			}

			if (resourceUids.Count == 0)
			{
				throw new ArgumentException(ResponsibilityApiMessages.ResourceUidNotEmpty);
			}

			if (resourceUids.Any(x => x.ResourceUid == Guid.Empty))
			{
				throw new ArgumentException(ResponsibilityApiMessages.ResourceUidInvalid);
			}

			var securityAssets = ResponsibilityRepository.GetSecurityAssetModelsForResources(resourceUids.Select(x => x.ResourceUid).ToList(), asset.uid, responsibility.UID).ToList();

			if (securityAssets.Any(x => string.IsNullOrEmpty(x.SecurityAsset)))
			{
				var badAsset = securityAssets.First(x => string.IsNullOrEmpty(x.SecurityAsset));
				throw new ArgumentException(string.Format(ResponsibilityApiMessages.InvalidResourceGroupUid, badAsset.uid.ToString()));
			}

			if (securityAssets.Any(x => x.Exists != true))
			{
				var badAsset = securityAssets.First(x => x.Exists != true);
				throw new ArgumentException(string.Format(ResponsibilityApiMessages.ReponsibilityOverrideExists, badAsset.uid.ToString()));
			}

			foreach (var uid in resourceUids.Select(x => x.ResourceUid).ToList())
			{
				var sas = securityAssets.FirstOrDefault(x => x.uid == uid);
				if (sas == null)
				{
					throw new ArgumentException(string.Format(ResponsibilityApiMessages.ResourceGroupUidNotExists, uid.ToString()));
				}
			}

			ResponsibilityRepository.DeleteResponsibilityOverrides(responsibility, asset, securityAssets);

			return Ok(new ConfirmResponse { title = ApiMessages.Success, message = ResponsibilityApiMessages.ResponsibilitySuccessDeleteMessage });
		}

        /// <summary>
        /// Adds a list of ownership rules for the specified responsibility type.
        /// </summary>
        /// 
        /// <remarks>
        ///###Rules###
        /// Conditions can be specified as Field condition (filter by field and its value), Relation condition (filter by relationship) and Assignee (filter by Resource, Group or Organization)
        /// <table>
        /// <tr><td>**Object**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>When</td><td>List of conditions which filter assets to which rule applies to</td><td>Can be empty - applies to all asset within asset type</td></tr>
        /// <tr><td>Then</td><td>List of conditions which specify to which Resrouce, Group or Organization rule applies to</td><td>Cannot be empty</td></tr>
        ///</table>
        /// <br/>
        /// <table>
        /// <tr><td>**Object**</td><td>**Field**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>Field</td><td>ApiName</td><td>API Name of the field</td><td>Must be a valid field Name for given Asset Type</td></tr>
        /// <tr><td>Field</td><td>Value</td><td>Field value for comparison. Only assets that match this value will be considered as a part of rule.</td><td>Must NOT be empty</td></tr>
        /// <tr><td>Relation</td><td>IntersectTypeUid</td><td>Relationship Type Uid</td><td>Must be valid relationship type for given Asset Type</td></tr>
        /// <tr><td>Relation</td><td>AssetUid</td><td>UID of matching Asset</td><td>Must be valid asset for Relationship Type specified on subject or object side.</td></tr>
        /// <tr><td>Assignee</td><td>Uid</td><td>UID of Resource, Group or Organization</td><td>Type must match to AssigneeTypeUid.</td></tr>
        /// <tr><td>Then</td><td>AssigneeTypeUid</td><td>UID of ResourceType, GroupType or OrganizationType</td><td>Must be valid UID</td></tr>
        /// </table>
        /// <br/>
        /// **Notes:** 
        /// * Only administrators can use this endpoint.
        /// 
        /// </remarks>
        /// 
        /// <param name="responsibilityTypeUid">Responsibility Type UID.</param>
        /// <param name="responsibilityRules">A list of responsibility rules you want to add.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
			RequireAdminPermissions,
            Route("types/{responsibilityTypeUid:guid}/ownershiprules"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create the responsibility rule", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of responsibility rules uid, including any error / success messages.", typeof(List<ResponsibilityRuleUpsertResponseModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Responsibility Type not found based on Uid provided.", typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> PostResponsibilityRules(Guid responsibilityTypeUid, [FromBody] List<ResponsibilityRuleUpsertModel> responsibilityRules)
        {
			var responsibility = ResponsibilityRepository.GetResponsibilityTypeByUID(responsibilityTypeUid);

			if (responsibility == null)
			{
				throw new NotFoundBusinessLayerException(ResponsibilityApiMessages.InvalidResponsibilityUid);
			}

			var existingUids = Company.Query<Guid>("select uid from ResponsibilityTypeRelationRule where uid in @uids", new { uids = responsibilityRules.Where(x => x.Uid.HasValue).Select(x => x.Uid) }).ToList();

			if (existingUids.Any())
			{
				string errorMessage = string.Format(ResponsibilityApiMessages.DuplicateResponsibilityRule, string.Join(", ", existingUids.Select(i => i.ToString())));
				throw new ArgumentException(errorMessage);
			}

			var execution = getApiExecution(responsibilityRules.Count);

			var results = await ResponsibilityRepository.UpsertResponsibilityRules(responsibilityTypeUid, responsibilityRules, execution);

			return Ok(results);
		}

        /// <summary>
        /// Edits a list of ownership rules for the specified responsibility type..
        /// </summary>
        /// <remarks>
        ///###Rules###
        /// Conditions can be specified as Field condition (filter by field and its value), Relation condition (filter by relationship) and Assignee (filter by Resource, Group or Organization)
        /// <table>
        /// <tr><td>**Object**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>When</td><td>List of conditions which filter assets to which rule applies to</td><td>Can be empty - applies to all asset within asset type</td></tr>
        /// <tr><td>Then</td><td>List of conditions which specify to which Resrouce, Group or Organization rule applies to</td><td>Cannot be empty</td></tr>
        ///</table>
        /// <br/>
        /// <table>
        /// <tr><td>**Object**</td><td>**Field**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>Field</td><td>ApiName</td><td>API Name of the field</td><td>Must be a valid field Name for given Asset Type</td></tr>
        /// <tr><td>Field</td><td>Value</td><td>Field value for comparison. Only assets that match this value will be considered as a part of rule.</td><td>Must NOT be empty</td></tr>
        /// <tr><td>Relation</td><td>IntersectTypeUid</td><td>Relationship Type Uid</td><td>Must be valid relationship type for given Asset Type</td></tr>
        /// <tr><td>Relation</td><td>AssetUid</td><td>UID of matching Asset</td><td>Must be valid asset for Relationship Type specified on subject or object side.</td></tr>
        /// <tr><td>Assignee</td><td>Uid</td><td>UID of Resource, Group or Organization</td><td>Type must match to AssigneeTypeUid.</td></tr>
        /// <tr><td>Then</td><td>AssigneeTypeUid</td><td>UID of ResourceType, GroupType or OrganizationType</td><td>Must be valid UID</td></tr>
        /// </table>
        /// <br/>
        /// **Notes:** 
        /// * Only administrators can use this endpoint.
        /// 
        /// </remarks>
        /// <param name="responsibilityTypeUid">Responsibility Type UID.</param>
        /// <param name="responsibilityRules">A list of responsibility rules you want to update.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
			RequireAdminPermissions,
            Route("types/{responsibilityTypeUid:guid}/ownershiprules"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update the responsibility rule", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of responsibility rules uid, including any error / success messages.", typeof(List<ResponsibilityRuleUpsertResponseModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Responsibility Type not found based on Uid provided.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutResponsibilityRules(Guid responsibilityTypeUid, [FromBody] List<ResponsibilityRuleUpsertModel> responsibilityRules)
        {
			var responsibility = ResponsibilityRepository.GetResponsibilityTypeByUID(responsibilityTypeUid);

			if (responsibility == null)
			{
				throw new NotFoundBusinessLayerException(ResponsibilityApiMessages.InvalidResponsibilityUid);
			}

			var execution = getApiExecution(responsibilityRules.Count);
			var results = await ResponsibilityRepository.UpsertResponsibilityRules(responsibilityTypeUid, responsibilityRules, execution);

			return Ok(results);
		}

		/// <summary>
		/// Deletes a list of ownership rules for the specified responsibility type..
		/// </summary>
		/// <param name="responsibilityTypeUid">Responsibility Type UID.</param>
		/// <param name="responsibilityRulesDeletes">A list of responsibility rules you want to delete.</param>
		/// <returns>An HTTP status code and message.</returns>
		[
			HttpDelete,
			RequireAdminPermissions,
			Route("types/{responsibilityTypeUid:guid}/ownershiprules"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to delete the responsibility rule", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.OK, "A list of responsibility rules uid, including any error / success messages.", typeof(ICollection<ResponsibilityRuleDeleteResponse>)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Responsibility Type not found based on Uid provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Authorization has been denied for this request.", typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> DeleteResponsibilityRules(Guid responsibilityTypeUid, [FromBody] IReadOnlyList<ResponsibilityRuleDeleteModel> responsibilityRulesDeletes)
		{
			ValidateParameters();

			var responsibility = ResponsibilityRepository.GetResponsibilityTypeByUID(responsibilityTypeUid);
			Condition.Require(responsibility, NotFoundBusinessLayerException.Create<ResponsibilityType>);

			var rulesForDeletion = responsibilityRulesDeletes.Select(x => x.Uid).ToList();
			var result = await ResponsibilityRepository.DeleteResponsibilityRulesAsync(responsibilityTypeUid, rulesForDeletion);

			return Ok(result);
		}

		/// <summary>
		/// Gets the breakdown of responsibilities
		/// </summary>
		/// <param name="responsibilityTypeUid">Responsibility Type UID</param>
		/// <returns>An Array of responsibility type breakdowns.</returns>
		[
			HttpGet,
			Route("breakdown"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "An Array of responsibility type breakdowns.", typeof(ICollection<ResponsibilityBreakdownResponse>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Authorization has been denied for this request.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetResponsibilityTypeBreakdown([FromUri] Guid? responsibilityTypeUid = null)
		{
			ValidateParameters();

			if (responsibilityTypeUid != null)
			{
				var responsibilityType = ResponsibilityRepository.GetResponsibilityTypeByUID(responsibilityTypeUid.Value);
				Condition.Require(responsibilityType, NotFoundBusinessLayerException.Create<ResponsibilityType>);
			}

			var result = await ResponsibilityRepository.GetTypeBreakdownAsync(responsibilityTypeUid);

			return Ok(result);
		}

		/// <summary>
		/// Gets the breakdown of responsibilities
		/// </summary>
		/// <param name="resourceUid">Resource UID</param>
		/// <param name="responsibilityTypeUid">Responsibility Type UID</param>
		/// <returns>An Array of responsibility type breakdowns.</returns>
		[
			HttpGet,
			Route("breakdown/{resourceUid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "An array of responsibilities per asset type.", typeof(ICollection<ResponsibilityGetBreakdownByResourceModel>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Authorization has been denied for this request.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetResponsibilityTypeBreakdownByResource(Guid resourceUid, [FromUri] Guid? responsibilityTypeUid = null)
		{
			ValidateParameters();

			var resource = await ResourceRepository.GetByUidAsync(resourceUid);
			Condition.Require(resource, NotFoundBusinessLayerException.Create<GlobalReportingResource>);

			if (responsibilityTypeUid != null)
			{
				var responsibilityType = ResponsibilityRepository.GetResponsibilityTypeByUID(responsibilityTypeUid.Value);
				Condition.Require(responsibilityType, NotFoundBusinessLayerException.Create<ResponsibilityType>);
			}

			var entities = await ResponsibilityRepository.GetTypeBreakdownByResourceAsync(resourceUid, responsibilityTypeUid);

			ResponsibilityGetBreakdownByResourceModel Convert(ResponsibilityBreakdownByResourceAggregate aggregate)
			{
				var model = new ResponsibilityGetBreakdownByResourceModel
				{
					Name = AssetService.GetAssetName(aggregate.AssetType),
					Class = aggregate.AssetType.Class.ToString(),
					AssetTypeUid = aggregate.AssetType.uid,
					AssetCount = aggregate.AssetCount
				};

				return model;
			}

			ICollection<ResponsibilityGetBreakdownByResourceModel> result = entities.Select(Convert).ToList();

			return Ok(result);
		}

        /// <summary>
        /// Test a responsibility rule definition to see which assets it will apply to.
        /// </summary>
        /// 
        /// <remarks>
        ///###Rules###
        /// Conditions can be specified as Field condition (filter by field and its value), Relation condition (filter by relationship) and Assignee (filter by Resource, Group or Organization)
        /// <table>
        /// <tr><td>**Object**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>When</td><td>List of conditions which filter assets to which rule applies to</td><td>Can be empty - applies to all asset within asset type</td></tr>
        /// <tr><td>Then</td><td>List of conditions which specify to which Resrouce, Group or Organization rule applies to</td><td>Cannot be empty</td></tr>
        ///</table>
        /// <br/>
        /// <table>
        /// <tr><td>**Object**</td><td>**Field**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>Field</td><td>ApiName</td><td>API Name of the field</td><td>Must be a valid field Name for given Asset Type</td></tr>
        /// <tr><td>Field</td><td>Value</td><td>Field value for comparison. Only assets that match this value will be considered as a part of rule.</td><td>Must NOT be empty</td></tr>
        /// <tr><td>Relation</td><td>IntersectTypeUid</td><td>Relationship Type Uid</td><td>Must be valid relationship type for given Asset Type</td></tr>
        /// <tr><td>Relation</td><td>AssetUid</td><td>UID of matching Asset</td><td>Must be valid asset for Relationship Type specified on subject or object side.</td></tr>
        /// <tr><td>Assignee</td><td>Uid</td><td>UID of Resource, Group or Organization</td><td>Type must match to AssigneeTypeUid.</td></tr>
        /// <tr><td>Then</td><td>AssigneeTypeUid</td><td>UID of ResourceType, GroupType or OrganizationType</td><td>Must be valid UID</td></tr>
        /// </table>
        /// <br/>
        /// **Notes:** 
        /// * Only administrators can use this endpoint.
        /// 
        /// </remarks>
        /// <param name="testType">The type of test to perform. Valid values are 'when' and 'then'</param>
        /// <param name="responsibilityRule">A responsibility rule definition to test.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
			RequireAdminPermissions,
			Route("test/{testType}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is false meaning the total count is excluded.", DataType = "boolean", ParameterType = "query", Required = false),
			SwaggerParameter("_simpleFilter", SIMPLE_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create the responsibility rule", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of assets which are applicable to the rule definition.", typeof(ResponsibilityRuleTestResponseModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> TestResponsibilityRules(string testType, [FromBody] ResponsibilityRuleUpsertModel responsibilityRule)
        {
			var allowedTests = new[] { "when", "then" };

			if (!allowedTests.Contains(testType.ToLower()))
			{
				throw new ArgumentException(ResponsibilityApiMessages.InvalidTestType);
			}

			var hideD3SUsers = SettingsRepository.GetSettingValue<bool>(Setting.HideData3SixtyUsers);
			var queryParams = Request.GetQueryNameValuePairs();
			var includeThen = testType.ToLower() == "then";

			var pageValid = isPageSizeAndNumValid(queryParams);

			if (!string.IsNullOrEmpty(pageValid))
			{
				throw new ArgumentException(pageValid);
			}

			var allowedValues = new[] { "asc", "desc" };
			var direction = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value ?? "asc";

			if (!allowedValues.Contains(direction.Trim().ToLower()))
			{
				throw new ArgumentException(ApiMessages.InvalidDirection);
			}

			var results = await ResponsibilityRepository.GetResponsibilityRuleTestResults(responsibilityRule, hideD3SUsers, includeThen, queryParams, testType);

			if (!results.Success)
			{
				throw new ArgumentException(results.Message);
			}

			return Ok(results);
		}

        /// <summary>
        /// Deletes responsibility overrides from asset for a given group or resource uid.
        /// </summary>
        /// <param name="uid">Uid of Group or Resource.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            RequireAdminPermissions,
            Route("overrides/{uid:guid}"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the request.", typeof(OkResult)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility override.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Forbidden user is not an administrator.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteResponsibilitiesOverrideByGroupOrResourceAsync(
            [FromUri] Guid uid
        )
        {
            ValidateParameters();

            string[] allowedObjects = { "Group", "Resource" };
            var asset = AssetRepository.GetAssetByUID(uid);
            
            if (asset == null || allowedObjects.Contains(asset.Object) == false)
            {
                throw new ArgumentException("Invalid resource or group uid.");
            }

            await ResponsibilityRepository.DeleteResponsibilityOverridesByGroupOrResourceAsync(uid);

            return Ok();
        }

        /// <summary>
        /// Deletes responsibility overrides from asset for a given Resource Type Uid.
        /// </summary>
        /// <param name="responsibilityTypeUid">Uid of an Asset Type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            RequireAdminPermissions,
            Route("overrides/byType/{responsibilityTypeUid:guid}"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the request.", typeof(OkResult)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility override.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Forbidden user is not an administrator.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteResponsibilitiesOverrideByTypeAsync([FromUri] Guid responsibilityTypeUid)
        {
            ValidateParameters();

            var type = await this.ResponsibilityRepository.GetResponsibilityType(responsibilityTypeUid);
            if (type == null)
            {
                throw new ArgumentException("Invalid ResponsibilityType uid.");
            }

            await ResponsibilityRepository.DeleteResponsibilityOverridesByTypeAsync(responsibilityTypeUid);

            return Ok();
        }
    }
}
