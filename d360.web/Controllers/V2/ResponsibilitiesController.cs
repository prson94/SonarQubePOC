using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using d360.web.Utilities;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using Microsoft.Web.Http;
using repositories;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;

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
		private readonly ISecurity Security;
		public ResponsibilitiesController(ICoreComponentSet set, ISecurity security) : base(set)
		{
			Security = security;
		}

		[HttpGet, Route("types"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IHttpActionResult> GetResponsibilityTypesAsync()
		{
			var roles = (await Security.ReadRolesAsync())
				.Data
				.Select(o => new ResponsibilityTypeViewModel
				{
					Description = o.Description,
					Name = o.Name,
					uid = o.Uid,
					UpdatedOn = o.UpdatedOn
				}).OrderBy(o => o.Name);

			return Ok(roles);
		}

		[HttpGet, Route("type/{uid}"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IHttpActionResult> GetResponsibilityTypeAsync(Guid uid)
		{
			if (uid == null || uid == Guid.Empty)
			{
				return errorMessageArgumentResponse(Error.InvalidResponsibilityUid);
			}

			var role = (await Security.ReadRolesAsync())
				.Data
				.Where(o => o.Uid == uid)
				.Select(o => new ResponsibilityTypeViewModel
				{
					Description = o.Description,
					Name = o.Name,
					uid = o.Uid,
					UpdatedOn = o.UpdatedOn
				})
				.FirstOrDefault();
			
			if (role == null)
			{
				return errorMessageNotFoundResponse(string.Format(Error.ResponsibilityTypeUidNotExist, uid.ToString()));
			}

			return Ok(new { data = role });
		}

		/// <summary>
		/// Get a list of all claims that are available for assignment.
		/// </summary>
		/// <returns>Returns a list of claims for assignment</returns>
		[
			HttpGet,
			Route("claims"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of claims for assignment.", typeof(ICollection<ClaimsViewModel>))
		]
		public IHttpActionResult GetClaimsAsync()
		{
			var permissions = Permission.ReadResponsibilities.GetList();
			var claims = permissions.Select(x => new ClaimsViewModel()
			{
				ID = (int)x.ID,
				Name = x.Name,
				Category = x.Category,
				Description = x.Description
			}).ToList();

			return Ok(claims);
		}

		[HttpGet, Route("types/{assetTypeUid:guid}"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IHttpActionResult> GetResponsibilityTypesByAssetTypeAsync(Guid assetTypeUid)
		{
			return await GetResponsibilityTypesAsync();
		}

		[HttpGet, Route("types/{responsibilityTypeUid:Guid}/allocations"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetResponsibilityTypeAllocationsAsync(Guid responsibilityTypeUid)
		{
			return Ok(new List<ResponsibilityTypeAllocationViewModel>());
		}

		[HttpGet, Route("typesbyasset/{assetTypeUid:Guid}/allocations"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetResponsibilityTypeAllocationsByAssetAsync(Guid assetTypeUid)
		{
			return Ok(new List<ResponsibilityTypeAllocationViewModel>());
		}

		[HttpPost, Route("types/{uid:Guid}/allocations"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult PostResponsibilityTypeAllocations(Guid uid, IEnumerable<ResponsibilityTypeAllocationInsertModel> model)
		{
			return Ok(new List<ResponsibilityTypeAllocationResponseModel>());
		}

		[HttpPut, Route("types/{uid:Guid}/allocations"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult PutResponsibilityTypeAllocations(Guid uid, IEnumerable<ResponsibilityTypeAllocationInsertModel> model)
		{
			return Ok(new List<ResponsibilityTypeAllocationResponseModel>());
		}

		[HttpDelete, Route("types/{uid:Guid}/allocations"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult DeleteResponsibilityTypeAllocationsAsync(Guid uid, ResponsibilityTypeAllocationDeleteModel model)
		{
			return Ok(new List<ResponsibilityTypeAllocationResponseModel>());
		}

		[HttpGet, Route("types/{responsibilityTypeUid:Guid}/ownershiprules"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetResponsibilityRulesForTypeAsync(Guid responsibilityTypeUid)
		{
			return Ok(new List<ResponsibilityTypeRuleViewModel>());
		}

		[HttpGet, Route("rules/{responsibilityTypeRuleUid:Guid}/stats"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetResponsibilityRulesStats(Guid responsibilityTypeRuleUid)
		{
			return Ok(new ResponsibilityTypeRuleStatsViewModel { AssignedAssets = 0, AssignedUsers = 0 });
		}

		[HttpGet, Route("assignments"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetResponsibilities()
		{
			return Ok(new AssetResponsibilitiesApiModel());
		}

		[HttpPost, Route("types"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult InsertResponsibilityTypes(List<ResponsibilityTypeInsertModel> responsibilityTypes)
		{
			return Ok(new List<ResponsibilityTypeUpsertResult>());
		}

		[HttpGet, Route("assignments/{assetUid}"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult GetOwnershipOfAsset(string assetUid)
		{
			return Ok(new List<OwnershipApiModel>());
		}

		[HttpGet, Route("hasassignments/{assetUid}"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IHttpActionResult> GetAssetHasOwnership(string assetUid)
		{
			return Ok(true);
		}

		[HttpPut, Route("types"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult UpdateResponsibilityTypes(List<ResponsibilityTypeUpsertModel> responsibilityTypes)
		{
			return Ok(new List<ResponsibilityTypeUpsertResult>());
		}

		[HttpDelete, Route("types"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult DeleteResponsibilityTypes(ResponsibilityTypeDeleteModel responsibilityTypes)
		{
			return Ok(new ResponsibilityTypeDeleteResult { Success = false, Message = "Not supported", Uid = Guid.Empty });
		}

		[HttpPut, Route("{assetUid:guid}/{responsibilityUid:guid}"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult UpdateResponsibilitiesOverride(Guid assetUid, Guid responsibilityUid, [FromBody] ResponsibilityOverridePutModel model)
		{
			return Ok(new ConfirmResponse { title = Error.Success, message = "Endpoint no longer supported." });
		}

		[HttpPost, Route("{assetUid:guid}/{responsibilityUid:guid}"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult AddResponsibilitiesOverride(Guid assetUid, Guid responsibilityUid, [FromBody] ResponsibilityOverridePostModel model, bool passedResponsibilityCheck = false)
		{
			return Ok(new ConfirmResponse { title = Error.Success, message = "Endpoint no longer supported" });
		}

		[HttpPost, Route("batch"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult BulkAddResponsibilitiesOverride(List<BulkResponsibilityOverridePostModel> models)
		{
			return Ok();
		}

		[HttpDelete, Route("{assetUid:guid}/{responsibilityUid:guid}"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult DeleteResponsibilitiesOverride()
		{
			return Ok(new ConfirmResponse { title = Error.Success, message = "Endpoint no longer available" });
		}

		[HttpPost, Route("types/{responsibilityTypeUid:guid}/ownershiprules"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult PostResponsibilityRules(Guid responsibilityTypeUid, [FromBody] List<ResponsibilityRuleUpsertModel> responsibilityRules)
		{
			return Ok(new List<ResponsibilityRuleUpsertResponseModel>());
		}

		[HttpPut, Route("types/{responsibilityTypeUid:guid}/ownershiprules"), RequireAdminPermissions, Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult PutResponsibilityRules(Guid responsibilityTypeUid, [FromBody] List<ResponsibilityRuleUpsertModel> responsibilityRules)
		{
			return Ok(new List<ResponsibilityRuleUpsertResponseModel>());
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
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Authorization has been denied for this request.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetResponsibilityTypeBreakdown([FromUri] Guid? responsibilityTypeUid = null)
		{
			var response = await Security.ReadAssetCountsByRoleAsync(responsibilityTypeUid);
			return sendRepositoryOkResponse(response);
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
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Authorization has been denied for this request.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetResponsibilityTypeBreakdownByResource(Guid resourceUid, [FromUri] Guid? responsibilityTypeUid = null)
		{
			var response = await Security.ReadAssetCountsByResourceAndRoleAsync(resourceUid, responsibilityTypeUid);
			return sendRepositoryOkResponse(response);
		}

		[HttpDelete, RequireAdminPermissions, Route("overrides/{uid:guid}"), Obsolete, ApiExplorerSettings(IgnoreApi = true)]
		public IHttpActionResult DeleteOverrideByGroupOrResource()
		{
			return Ok();
		}
	}
}
