using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.security;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using d360.web.Utilities;
using Microsoft.Web.Http;
using repositories;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

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
		public async Task<IHttpActionResult> CreatePolicyAsync(CreateRule model)
		{
			var result = await Security.CreatePolicyAsync(model);
			return sendRepositoryResponse(result);
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
		public async Task<IHttpActionResult> CreatePolicyOverrideAsync(CreateRuleOverride model)
		{
			var result = await Security.CreatePolicyOverrideAsync(model);
			return sendRepositoryResponse(result);
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
		public async Task<IHttpActionResult> CreateRoleAsync(CreateRole model)
		{
			if (model == null)
			{
				return errorMessageArgumentResponse(ApiMessages.JSONValidMessage);
			}

			var result = await Security.CreateRoleAsync(model);
			return sendRepositoryResponse(result);
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
			var result = await Security.RemovePolicyAsync(uid);
			return sendRepositoryResponse(result);
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
			var result = await Security.RemovePolicyOverrideAsync(uid);
			return sendRepositoryResponse(result);
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
			SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ResponsibilityTypeDeleteResult)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to update responsibility types.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Responsibility with UID 'provided uid' does not exist.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> DeleteRoleAsync(Guid uid)
		{
			if (uid == Guid.Empty)
			{
				return errorMessageArgumentResponse(ResponsibilityApiMessages.InvalidResponsibilityUid);
			}

			var result = await Security.RemoveRoleAsync(uid);
			return sendRepositoryResponse(result);
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
			SwaggerResponse(HttpStatusCode.Forbidden, "Forbidden")
		]
		public async Task<IHttpActionResult> ReadPoliciesAsync()
		{
			var result = await Security.ReadPoliciesAsync();
			return Ok(result.Data);
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
			return Ok(result.Data);
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
		public async Task<IHttpActionResult> UpdatePolicyAsync(Guid uid, ReadRule model)
		{
			var result = await Security.UpdatePolicyAsync(uid, model);
			return sendRepositoryResponse(result);
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
		public async Task<IHttpActionResult> UpdatePolicyOverrideAsync(Guid uid, CreateRuleOverride model)
		{
			var result = await Security.UpdatePolicyOverrideAsync(uid, model);
			return sendRepositoryResponse(result);
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
				return errorMessageArgumentResponse(ApiMessages.JSONValidMessage);
			}

			var result = await Security.UpdateRoleAsync(uid, model);
			return sendRepositoryResponse(result);
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
