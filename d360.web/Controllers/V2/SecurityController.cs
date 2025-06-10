using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.core.security;
using d360.extensions;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using repositories;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
	/// <summary>
	/// This service houses all endpoints handling glossary-related data such as artifacts and models.
	/// </summary>
	[
		ApiVersion("2.0"),
		RoutePrefix("api/v{version:apiVersion}/security"),
		Authorize, StringEnum
	]
	public class SecurityController : BaseV2ApiController
	{
		private readonly ISecurity Security;

		public SecurityController(ICoreComponentSet set, ISecurity security)
			: base(set)
		{
			Security = security;
		}


		/// <summary>
		/// Adds a list of ownership rules for the specified responsibility type.
		/// </summary>
		/// 
		/// <remarks>
		///###Rules###
		/// Conditions can be specified as Field condition (filter by field and its value), Relation condition (filter by relationship) and Assignee (filter by Resource or Group)
		/// <table>
		/// <tr><td>**Object**</td><td>**Description**</td><td>**Validation**</td></tr>
		/// <tr><td>When</td><td>List of conditions which filter assets to which rule applies to</td><td>Can be empty - applies to all asset within asset type</td></tr>
		/// <tr><td>Then</td><td>List of conditions which specify to which Resource or Group rule applies to</td><td>Cannot be empty</td></tr>
		///</table>
		/// <br/>
		/// <table>
		/// <tr><td>**Object**</td><td>**Field**</td><td>**Description**</td><td>**Validation**</td></tr>
		/// <tr><td>Field</td><td>ApiName</td><td>API Name of the field</td><td>Must be a valid field Name for given Asset Type</td></tr>
		/// <tr><td>Field</td><td>Value</td><td>Field value for comparison. Only assets that match this value will be considered as a part of rule.</td><td>Must NOT be empty</td></tr>
		/// <tr><td>Relation</td><td>IntersectTypeUid</td><td>Relationship Type Uid</td><td>Must be valid relationship type for given Asset Type</td></tr>
		/// <tr><td>Relation</td><td>AssetUid</td><td>UID of matching Asset</td><td>Must be valid asset for Relationship Type specified on subject or object side.</td></tr>
		/// <tr><td>Assignee</td><td>Uid</td><td>UID of Resource or Group</td><td>Type must match to AssigneeTypeUid.</td></tr>
		/// <tr><td>Then</td><td>AssigneeTypeUid</td><td>UID of ResourceType or GroupType</td><td>Must be valid UID</td></tr>
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
			Route("policies"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create the responsibility rule", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.OK, "A list of responsibility rules uid, including any error / success messages.", typeof(List<ResponsibilityRuleUpsertResponseModel>)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Responsibility Type not found based on Uid provided.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> CreatePolicyAsync(CreateSecurityPolicy model)
		{
			if(string.IsNullOrWhiteSpace(model?.Name))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidName);
			}

			var policyExists = await Security.DoesPolicyExists(model.Name?.Trim());
			if (policyExists)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, Error.DuplicateItem);
			}
			var result = await Security.CreatePolicyAsync(model);
			if (result.IsSuccess)
			{
				RecalculateSecurityPolicy(new SecurityPolicyArgs { PolicyUid = result.Data.Uid });
			}
			return sendRepositoryCreatedResponse(result);
		}

		/// <summary>
		/// Adds role override to asset for a given Resource Uid list.
		/// </summary>
		/// <param name="model">An object containing list of Resource/Group Uids and description (context).</param>
		/// <returns>An HTTP status code and message.</returns>
		[
			HttpPost,
			MapToApiVersion("2.0"),
			Route("policy-overrides"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility override.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> CreatePolicyOverrideAsync(CreateSecurityPolicyOverride model)
		{
			var result = await Security.CreateOverrideAsync(model);
			return sendRepositoryCreatedResponse(result);
		}

		/// <summary>
		/// Allows administrators to create one or more roles within this workspace.
		/// </summary>
		/// <param name="models">The list of responsibility types for insertion.</param>
		/// <returns>An HTTP status code and message.</returns>
		[
			HttpPost,
			RequireAdminPermissions,
			MapToApiVersion("2.0"),
			Route("roles"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(ReadRole)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to add roles.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> CreateRoleAsync([FromBody]CreateRole model)
		{
			if (model == null)
			{
				return errorMessageArgumentResponse(Error.JSONValidMessage);
			}

			var result = await Security.CreateRoleAsync(model);
			return sendRepositoryCreatedResponse(result);
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
			Route("policies/{uid:Guid}"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to delete the responsibility rule", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.OK, "A list of responsibility rules uid, including any error / success messages.", typeof(ICollection<ResponsibilityRuleDeleteResponse>)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Responsibility Type not found based on Uid provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Authorization has been denied for this request.", typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> DeletePolicyAsync(Guid uid)
		{
			var result = await Security.RemovePolicyAsync(uid, true);
			if (result.IsSuccess)
			{
				RecalculateSecurityPolicy(new SecurityPolicyArgs { PolicyUid = uid, IsDeleteAction = true });
			}
			return sendRepositoryOkResponse(result);
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
			Route("policy-overrides/{uid:guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerRequestExample(typeof(ResponsibilityOverrideDeleteModel), typeof(ResponsibilitiesDeleteExample)),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility override.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> DeletePolicyOverrideAsync(Guid uid)
		{
			var result = await Security.RemoveOverrideAsync(uid);
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// Deletes a role.
		/// </summary>
		/// <returns>An HTTP status code and message.</returns>
		[
			HttpDelete,
			RequireAdminPermissions,
			MapToApiVersion("2.0"),
			Route("roles/{uid:Guid}"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(RoleDeleteResult)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to update responsibility types.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Responsibility with UID 'provided uid' does not exist.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> DeleteRoleAsync(Guid uid)
		{
			if (uid == Guid.Empty)
			{
				return errorMessageArgumentResponse(Error.InvalidResponsibilityUid);
			}

			var result = await Security.RemoveRoleAsync(uid);
			return sendRepositoryOkResponse(result);
		}


		/// <summary>
		/// Retrieves a list of all owners for a given asset.
		/// </summary>
		/// <returns>A list of owners.</returns>
		[
			HttpGet,
			Route("{assetUid:Guid}/owners"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of owners.", typeof(IEnumerable<ReadRole>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Forbidden"),
		]
		public async Task<IHttpActionResult> ReadOwnersByAssetAsync(Guid assetUid)
		{
			// TODO: Put permission check in here.
			var result = await Security.ReadVisibleOwnersByAssetAsync(assetUid);
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// BFF endpoint that retrieves a list of all options for owner overrides (groups and users)
		/// </summary>
		/// <returns>A list of owner options.</returns>
		[
			HttpGet,
			Route("owner-options"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of owners.", typeof(IEnumerable<dynamic>)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> ReadOwnerOptionsAsync(Guid assetUid)
		{
			var hide = await GetCachedSettingValueById<bool>(Setting.HideData3SixtyUsers);
			var result = await Security.ReadGroupsAndUsersAsSecurityAsync(assetUid, !hide);
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// Retrieves a list of all policies.
		/// </summary>
		/// <returns>A list of roles.</returns>
		[
			HttpGet,
			Route("policies"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of roles.", typeof(IEnumerable<ReadRole>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Forbidden"), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> ReadPoliciesAsync()
		{
			var result = await Security.ReadPoliciesAsync();
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// BFF-style endpoint that is used internally and hidden from Swagger.
		/// </summary>
		/// <returns>An object with all lookups required for the Security Policy Editor.</returns>
		[ HttpGet, Route("policy-editor/options"), ApiExplorerSettings(IgnoreApi = true), RequireAdminPermissions]
		public async Task<IHttpActionResult> ReadPolicyEditOptionsAsync()
		{
			var result = await Security.ReadPolicyEditOptionsAsync();
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// BFF-style endpoint that is used internally and hidden from Swagger.
		/// </summary>
		/// <returns>An object with all lookups required for the Security Policy Editor after selecting an asset type. Items like fields and intersect types.</returns>
		[HttpGet, Route("policy-editor/options/asset-type/{assetTypeUid:Guid}"), ApiExplorerSettings(IgnoreApi = true), RequireAdminPermissions]
		public async Task<IHttpActionResult> ReadPolicyEditAssetTypeOptionsAsync(Guid assetTypeUid)
		{
			var result = await Security.ReadPolicyEditAssetTypeOptionsAsync(assetTypeUid);
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// BFF-style endpoint that is used internally and hidden from Swagger.
		/// </summary>
		/// <returns>An object with all groups for the Security Policy Editor after selecting an asset type.</returns>
		[HttpGet, Route("policy-editor/options/group"), ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IHttpActionResult> ReadPolicyEditGroupOptionsAsync()
		{
			var result = await Security.ReadPolicyEditGroupOptionsAsync();
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// BFF-style endpoint that is used internally and hidden from Swagger.
		/// </summary>
		/// <returns>An object with all users for the Security Policy Editor after selecting an asset type.</returns>
		[HttpGet, Route("policy-editor/options/user"), ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IHttpActionResult> ReadPolicyEditUserOptionsAsync()
		{
			var result = await Security.ReadPolicyEditUserOptionsAsync();
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// BFF-style endpoint that is used internally and hidden from Swagger.
		/// </summary>
		/// <returns>An object with all lookup options for the Security Policy Editor after selecting an asset type.</returns>
		[HttpGet, Route("policy-editor/options/{assetTypeUid:Guid}/{fieldName}/field-lookup"), ApiExplorerSettings(IgnoreApi = true), RequireAdminPermissions]
		public async Task<IHttpActionResult> ReadPolicyEditFieldLookupOptionsAsync(Guid assetTypeUid, string fieldName)
		{
			var result = await Security.ReadPolicyEditFieldLookupOptionsAsync(assetTypeUid, fieldName);
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// BFF-style endpoint that is used internally and hidden from Swagger.
		/// </summary>
		/// <returns>An object with all related asset options for the Security Policy Editor after selecting an asset type.</returns>
		[HttpGet, Route("policy-editor/options/{intersectTypeUid:Guid}/{assetTypeUid:Guid}/relation-lookup"), ApiExplorerSettings(IgnoreApi = true), RequireAdminPermissions]
		public async Task<IHttpActionResult> ReadPolicyEditRelationLookupOptionsAsync(Guid intersectTypeUid, Guid assetTypeUid)
		{
			var result = await Security.ReadPolicyEditRelationLookupOptionsAsync(intersectTypeUid, assetTypeUid);
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// Retrieves a list of all roles.
		/// </summary>
		/// <returns>A list of roles.</returns>
		[
			HttpGet,
			Route("roles"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of roles.", typeof(IEnumerable<ReadRole>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Forbidden")
		]
		public async Task<IHttpActionResult> ReadRolesAsync()
		{
			var result = await Security.ReadRolesAsync();
			return sendRepositoryOkResponse(result);
		}


		/// <summary>
		/// Edits a list of ownership rules for the specified responsibility type..
		/// </summary>
		/// <remarks>
		///###Rules###
		/// Conditions can be specified as Field condition (filter by field and its value), Relation condition (filter by relationship) and Assignee (filter by Resource or Group)
		/// <table>
		/// <tr><td>**Object**</td><td>**Description**</td><td>**Validation**</td></tr>
		/// <tr><td>When</td><td>List of conditions which filter assets to which rule applies to</td><td>Can be empty - applies to all asset within asset type</td></tr>
		/// <tr><td>Then</td><td>List of conditions which specify to which Resrouce or Group rule applies to</td><td>Cannot be empty</td></tr>
		///</table>
		/// <br/>
		/// <table>
		/// <tr><td>**Object**</td><td>**Field**</td><td>**Description**</td><td>**Validation**</td></tr>
		/// <tr><td>Field</td><td>ApiName</td><td>API Name of the field</td><td>Must be a valid field Name for given Asset Type</td></tr>
		/// <tr><td>Field</td><td>Value</td><td>Field value for comparison. Only assets that match this value will be considered as a part of rule.</td><td>Must NOT be empty</td></tr>
		/// <tr><td>Relation</td><td>IntersectTypeUid</td><td>Relationship Type Uid</td><td>Must be valid relationship type for given Asset Type</td></tr>
		/// <tr><td>Relation</td><td>AssetUid</td><td>UID of matching Asset</td><td>Must be valid asset for Relationship Type specified on subject or object side.</td></tr>
		/// <tr><td>Assignee</td><td>Uid</td><td>UID of Resource or Group</td><td>Type must match to AssigneeTypeUid.</td></tr>
		/// <tr><td>Then</td><td>AssigneeTypeUid</td><td>UID of ResourceType or GroupType</td><td>Must be valid UID</td></tr>
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
			Route("policies/{uid:Guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to update the responsibility rule", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.OK, "A list of responsibility rules uid, including any error / success messages.", typeof(List<ResponsibilityRuleUpsertResponseModel>)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Responsibility Type not found based on Uid provided.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> UpdatePolicyAsync(Guid uid, ReadSecurityPolicy model)
		{
			if(string.IsNullOrWhiteSpace(model?.Name))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidName);
			}
			var result = await Security.UpdatePolicyAsync(uid, model);
			if (result.IsSuccess)
			{
				RecalculateSecurityPolicy(new SecurityPolicyArgs { PolicyUid = uid });
			}
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// Adds responsibility override to asset for a given Resource Uid list.
		/// </summary>
		/// <param name="assetUid">Uid of an Asset.</param>
		/// <param name="roleUid">Uid of Responsibility type.</param>
		/// <param name="model">An object containing list of Resource/Group Uids and description (context).</param>
		/// <returns>An HTTP status code and message.</returns>
		[
			HttpPut,
			MapToApiVersion("2.0"),
			Route("policy-overrides/{uid:guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility override.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> UpdatePolicyOverrideAsync(Guid uid, UpdateSecurityPolicyOverride model)
		{
			var result = await Security.UpdateOverrideAsync(uid, model);
			return sendRepositoryOkResponse(result);
		}

		/// <summary>
		/// Updates a role.
		/// </summary>
		/// <returns>An HTTP status code and message.</returns>
		[
			HttpPut,
			RequireAdminPermissions,
			MapToApiVersion("2.0"),
			Route("roles/{uid:Guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the PUT request.", typeof(ReadRole)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update responsibility types.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> UpdateRoleAsync(Guid uid, CreateRole model)
		{
			if (model == null)
			{
				return errorMessageArgumentResponse(Error.JSONValidMessage);
			}

			var result = await Security.UpdateRoleAsync(uid, model);
			return sendRepositoryOkResponse(result);
		}


		/// <summary>
		/// Test a responsibility rule definition to see which assets it will apply to.
		/// </summary>
		/// 
		/// <remarks>
		///###Rules###
		/// Conditions can be specified as Field condition (filter by field and its value), Relation condition (filter by relationship) and Assignee (filter by Resource or Group)
		/// <table>
		/// <tr><td>**Object**</td><td>**Description**</td><td>**Validation**</td></tr>
		/// <tr><td>When</td><td>List of conditions which filter assets to which rule applies to</td><td>Can be empty - applies to all asset within asset type</td></tr>
		/// <tr><td>Then</td><td>List of conditions which specify to which Resrouce or Group rule applies to</td><td>Cannot be empty</td></tr>
		///</table>
		/// <br/>
		/// <table>
		/// <tr><td>**Object**</td><td>**Field**</td><td>**Description**</td><td>**Validation**</td></tr>
		/// <tr><td>Field</td><td>ApiName</td><td>API Name of the field</td><td>Must be a valid field Name for given Asset Type</td></tr>
		/// <tr><td>Field</td><td>Value</td><td>Field value for comparison. Only assets that match this value will be considered as a part of rule.</td><td>Must NOT be empty</td></tr>
		/// <tr><td>Relation</td><td>IntersectTypeUid</td><td>Relationship Type Uid</td><td>Must be valid relationship type for given Asset Type</td></tr>
		/// <tr><td>Relation</td><td>AssetUid</td><td>UID of matching Asset</td><td>Must be valid asset for Relationship Type specified on subject or object side.</td></tr>
		/// <tr><td>Assignee</td><td>Uid</td><td>UID of Resource or Group</td><td>Type must match to AssigneeTypeUid.</td></tr>
		/// <tr><td>Then</td><td>AssigneeTypeUid</td><td>UID of ResourceType or GroupType</td><td>Must be valid UID</td></tr>
		/// </table>
		/// <br/>
		/// **Notes:** 
		/// * Only administrators can use this endpoint.
		/// 
		/// </remarks>
		/// <param name="testType">The type of test to perform. Valid values are 'when' and 'then'</param>
		/// <param name="responsibilityRule">A responsibility rule definition to test.</param>
		/// <returns>An HTTP status code and message.</returns>
		//[
		//	HttpPost,
		//	RequireAdminPermissions,
		//	Route("test/{testType}"),
		//	SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
		//	SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
		//	SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
		//	SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
		//	SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is false meaning the total count is excluded.", DataType = "boolean", ParameterType = "query", Required = false),
		//	SwaggerParameter("_simpleFilter", SIMPLE_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
		//	SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		//	SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create the responsibility rule", typeof(ErrorResponse)),
		//	SwaggerResponse(HttpStatusCode.OK, "A list of assets which are applicable to the rule definition.", typeof(ResponsibilityRuleTestResponseModel)),
		//	SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		//]
		//public async Task<IHttpActionResult> TestResponsibilityRules(string testType, [FromBody] ResponsibilityRuleUpsertModel responsibilityRule)
		//{
		//	var allowedTests = new[] { "when", "then" };

		//	if (!allowedTests.Contains(testType.ToLower()))
		//	{
		//		throw new ArgumentException(ResponsibilityApiMessages.InvalidTestType);
		//	}

		//	var hideD3SUsers = await GetCachedSettingValueById<bool>(Setting.HideData3SixtyUsers);
		//	var queryParams = Request.GetQueryNameValuePairs();
		//	var includeThen = testType.ToLower() == "then";

		//	var pageValid = isPageSizeAndNumValid(queryParams);

		//	if (!string.IsNullOrEmpty(pageValid))
		//	{
		//		throw new ArgumentException(pageValid);
		//	}

		//	var allowedValues = new[] { "asc", "desc" };
		//	var direction = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value ?? "asc";

		//	if (!allowedValues.Contains(direction.Trim().ToLower()))
		//	{
		//		throw new ArgumentException(ApiMessages.InvalidDirection);
		//	}

		//	var results = await ResponsibilityRepository.GetResponsibilityRuleTestResults(responsibilityRule, hideD3SUsers, includeThen, queryParams, testType);

		//	if (!results.Success)
		//	{
		//		throw new ArgumentException(results.Message);
		//	}

		//	return Ok(results);
		//}
	}
}
