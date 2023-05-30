using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.exceptions;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using Microsoft.Web.Http;

using Resources;

using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/workflow"),
        Authorize
    ]
    public class WorkflowController : BaseV2ApiController
    {
        #region DI

        private readonly IWorkflowRepository workflowRepository;
        private readonly IWorkflowApiModelValidator validator;

        public WorkflowController(ICoreComponentSet set, IWorkflowRepository workflowRepository, IWorkflowApiModelValidator validator) : base(set)
        {
            this.workflowRepository = workflowRepository;
            this.validator = validator;
        }

        #endregion

        /// <summary>
        /// Retrieves workflow types for the given asset type unique identifier / action type unique identifier/ relationship type unique identifier .
        /// </summary>
        /// <remarks>
        /// If using Uid parametes, you may provide only one of the following: ActionTypeUid,AssetTypeUid,RelationshipTypeUid
        /// </remarks>
        /// <param name="ChangeType">ChangeType</param>
        /// <param name="State">State</param>
        /// <returns>Returns list of workflow types and An HTTP status code </returns>
        [
            HttpGet,
            Route("types"),
            SwaggerParameter("ActionTypeUid", "Action Type unique identifier", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AssetTypeUid", "Asset Type unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("RelationshipTypeUid", "Relationship unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowTypeApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Action Type / Asset Type / Relationship Type  not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetWorkflowTypeAsync(ChangeType? ChangeType = null, State? State = null)
        {
            var queryParams = Request.GetQueryNameValuePairs();

            if (!validator.IsValidGuidCountForWorkflowGetTypeModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.MoreThanOneUidPassed);
            }

            if (!validator.IsValidGuidForWorkflowGetTypeModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.InvalidTypeWorkflowVersionRequest);
            }

            if (!validator.IsValidAssetType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeNotFound, GetUidFromQueryParams(queryParams, "AssetTypeUid")));
            }

            if (!validator.IsValidActionType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.ActionTypeUidIsNotValid, GetUidFromQueryParams(queryParams, "ActionTypeUid")));
            }

            if (!validator.IsValidRelationshipType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.RelationShipTypeUidNotFound, GetUidFromQueryParams(queryParams, "RelationshipTypeUid")));
            }

            var workflowtypes = await workflowRepository.GetWorkflowTypes(queryParams);

			return Ok(workflowtypes);
        }

        /// <summary>
        /// Retrieves workflow versions for the given asset type unique identifier / action type unique identifier/ relationship type unique identifier / workflow type  unique identifier.
        /// </summary>
        /// <remarks>
        /// If using Uid parametes, you may provide only one of the following: ActionTypeUid,AssetTypeUid,RelationshipTypeUid,WorkflowTypeUid
        /// </remarks>
        /// <param name="State">State</param>
        /// <returns>Returns list of workflow versions and An HTTP status code </returns>
        [
            HttpGet,
            Route("versions"),
            SwaggerParameter("ActionTypeUid", "Action Type unique identifier", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AssetTypeUid", "Asset Type unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("RelationshipTypeUid", "Relationship unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("WorkflowTypeUid", "Workflow Type unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. Options are UpdatedOn, CreatedOn, State, and VersionNumber. By default the results are ordered by VersionNumber.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowVersionsApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Action Type / Asset Type / Relationship Type / Workfflow Type  not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid/WorkflowTypeUid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetWorkflowVersionAsync(State? State = null)
        {
            var queryParams = Request.GetQueryNameValuePairs();
            var isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
				throw new ArgumentException(isValid);
            }

            if (!validator.IsValidGuidCountForWorkflowGetVersionModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.MoreThanOneTypeUidPassedInclWorkflow);
            }

            if (!validator.IsValidOrderByFieldForWorkflowGetVersionModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.InvalidParameterWorkflowVersion);
            }

            if (!validator.IsValidGuidForWorkflowGetVersionModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.InvalidTypeWorkflowVersionRequestIncWF);
            }

            if (!validator.IsValidAssetType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeNotFound, GetUidFromQueryParams(queryParams, "AssetTypeUid")));
            }

            if (!validator.IsValidActionType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeNotFound, GetUidFromQueryParams(queryParams, "ActionTypeUid")));
            }

            if (!validator.IsValidRelationshipType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.RelationShipTypeUidNotFound, GetUidFromQueryParams(queryParams, "RelationshipTypeUid")));
            }

            if (!validator.IsValidWorkflowType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(WorkflowApiMessages.WorkflowtypeUIDNotFound, GetUidFromQueryParams(queryParams, "WorkflowTypeUid")));
            }

            var workflowVersions = await workflowRepository.GetWorkflowVersions(queryParams);

			return Ok(workflowVersions);
        }

        /// <summary>
        /// Retrieves workflow versions  steps for the given workflow version unique identifier .
        /// </summary>
        /// <param name="workflowVersionUid"> workflow version unique identifier</param>
        /// <returns>Returns list of workflow version steps and An HTTP status code</returns>
        [
            HttpGet,
            Route("versions/{workflowVersionUid}/steps"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowVersionStepsApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Workflow Version  not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow version steps is invalid, possibly due to an incorrectly formatted  workflow version unique identifier.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetWorkflowVersionStepsAsync(Guid workflowVersionUid)
        {
            if (!validator.IsValidWorkflowVersion(workflowVersionUid))
            {
				throw new NotFoundBusinessLayerException(string.Format(WorkflowApiMessages.WorkflowVersionUIDNotFound, workflowVersionUid.ToString()));
            }

            var workflowVersionSteps = await workflowRepository.GetWorkflowVersionSteps(workflowVersionUid);
            
			return Ok(workflowVersionSteps);
        }

        private Guid GetUidFromQueryParams(IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName)
        {
            Guid uid = Guid.Empty;

            if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName.ToLower()))
            {
                var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName.ToLower()).Value;
               
                if (!Guid.TryParse(uidString, out uid))
                {
                    uid = Guid.Empty;
                }
            }

            return uid;
        }

        /// <summary>
        /// Get a list of steps and their relevant information for a specific workflow instance contained within the system.
        /// </summary>
        /// <param name="workflowUid">workflow instance unique identifier</param>
        /// <returns></returns>
        [
            HttpGet, 
            Route("{workflowUid}/steps"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowInstanceApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Workflow Instance  not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow instance is invalid, possibly due to an incorrectly formatted  workflow instance unique identifier.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetWorkflowInstances(Guid workflowUid)
        {
            if (!validator.IsValidWorkflowInstance(workflowUid))
            {
				throw new NotFoundBusinessLayerException(string.Format(WorkflowApiMessages.WorkflowUIDNotFound, workflowUid.ToString()));
            }

            var workflowInstances = await workflowRepository.GetWorkflowInstances(workflowUid);

			return Ok(workflowInstances);
        }

        /// <summary>
        /// Get a list of workflows contained within the system.
        /// </summary>
        /// <param name="Active">Active: is the workflow in an active (non-completed) state; Default is Active
        /// </param>
        /// <returns>Returns list of workflows and a HTTP status code</returns>
        [
            HttpGet,
            Route(""),
            SwaggerParameter("WorkflowTypeUid", "The unique identifier for the workflow type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("VersionUid", "The unique identifier for the workflow version.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("ActionUid", "Action unique identifier that the workflow is registered to.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AssetUid", "Asset unique identifier that the workflow is registered to.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("RelationshipUid", "Relationship unique identifier that the workflow is registered to.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending.The acceptable fields are StartedOn and CompletedOn.By default the results are ordered by StartedOn.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowsApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Action / Asset / Relationship / Workfflow Type / Workflow Version  not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionUid / AssetUid / RelationshipUid / WorkflowTypeUid / VersionUid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetWorkflowsAsync(WorkflowApiState? Active = null)
        {
            var queryParams = Request.GetQueryNameValuePairs();
            var isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
				throw new ArgumentException(isValid);
            }

            if (!validator.IsValidGuidCountForGetWorkflowModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.MoreThanOneUidPassed);
            }

            if (!validator.IsValidOrderByFieldForGetWorkflowModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.InvalidOrderParameterPassed);
            }

            if (!validator.IsValidDirectionForWorkflowGetModel(queryParams))
			{
				throw new ArgumentException(ApiMessages.InvalidDirection);
            }

            if (!validator.IsValidGuidForGetWorkflowModel(queryParams))
            {
				throw new ArgumentException(WorkflowApiMessages.InvalidUidWorkflowVersionRequest);
            }

            if (!validator.IsValidAsset(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetNotFound, GetUidFromQueryParams(queryParams, "AssetUid")));
            }

            if (!validator.IsValidAction(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.ActionUidNotFound, GetUidFromQueryParams(queryParams, "ActionUid")));
            }

            if (!validator.IsValidRelationship(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(RelationshipsApiMessages.RelationShipUidNotFound, GetUidFromQueryParams(queryParams, "RelationshipTypeUid")));
            }

            if (!validator.IsValidWorkflowType(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(WorkflowApiMessages.WorkflowtypeUIDNotFound, GetUidFromQueryParams(queryParams, "WorkflowTypeUid")));
            }

            if (!validator.IsValidWorkflowVersion(queryParams))
            {
				throw new NotFoundBusinessLayerException(string.Format(WorkflowApiMessages.WorkflowVersionUIDNotFound, GetUidFromQueryParams(queryParams, "versionUid")));
            }

            var workflows = await workflowRepository.GetWorkflows(queryParams);

			return Ok(workflows);
        }

        [
            HttpGet,
            Route("type/{uid}/id"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(int))
        ]
        public IHttpActionResult GetWorkflowtypeId(Guid uid)
        {
            var result = workflowRepository.GetWorkflowTypeByUID(uid);

            return Ok(result.ID);
        }

        [
            HttpGet,
            Route("{uid}/legacyData"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(int))
        ]
        public IHttpActionResult GetWorkflowId(Guid uid)
        {
            var result = workflowRepository.GetWorkflowItemByUID(uid);

            if (result == null)
            {
				throw new NotFoundBusinessLayerException(WorkflowApiMessages.WorkflowInstanceNotFound);
            }

			return Ok(result.ID);
        }

        [
            HttpGet,
            Route("reassignment/objects/{id:int}"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(IEnumerable<WorkflowReassignmentAssetApiModel>))
        ]
        public async Task<IHttpActionResult> GetWorkflowReassignmentAssets(int id, string query, CancellationToken cancellationToken)
        {
            var result = Company.WorkflowItems.FirstOrDefault(i => i.ID == id);

            if (result == null)
            {
				throw new NotFoundBusinessLayerException(WorkflowApiMessages.WorkflowInstanceNotFound);
            }

			return Ok(await workflowRepository.GetWorkflowReassignmentAssets(id, query, cancellationToken: cancellationToken));
        }

		[
			HttpGet,
			Route("assignments"),			
			SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_order", "The name of the field to order results by, ascending. Options are UpdatedOn, CreatedOn. By default the results are ordered by UpdatedOn.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of an assignment. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_initiatorUid", "Return assignments Filter by provided initiator Uid", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_workflowItemUid", "Return assignments Filter by provided initiator Uid", DataType = "string", ParameterType = "query", Required = false),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowAssignmentApiModel)),
			SwaggerResponse(HttpStatusCode.NotFound, "Action Type / Asset Type / Relationship Type / Workfflow Type  not found based on Uid provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid/WorkflowTypeUid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetWorkflowAssignments()
		{
			var prefix = "Workflow.GetWorkflowAssignments => ";
			var queryParams = Request.GetQueryNameValuePairs();
			try
			{
				var isValid = isPageSizeAndNumValid(queryParams);

				if (!string.IsNullOrEmpty(isValid))
				{
					throw new ArgumentException(isValid);
				}

				if (!validator.IsValidDirectionForWorkflowGetModel(queryParams))
				{
					throw new ArgumentException(ApiMessages.InvalidDirection);
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "_initiatorUid"))
				{
					string initiatorUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_initiatorUid").Value;
					if (!Guid.TryParse(initiatorUidString, out Guid initiatorUid))
					{
						throw new ArgumentException(string.Format(WorkflowApiMessages.InvalidGuid, initiatorUidString, "_initiatorUid"));
					}
					else
					{
						var initiator = Company.GlobalReportingResources.Where(u=>u.Uid==initiatorUid);

						if (initiator == null)
						{
							throw new ArgumentException(string.Format(WorkflowApiMessages.InvalidGuid, initiatorUidString, "_initiatorUid"));
						}
					}
				}				

				var response = await workflowRepository.GetWorkflowAssignmentList(queryParams).ConfigureAwait(false);

				return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
			}			
			catch(GenericException gex)
			{
				return await Task.FromResult(errorMessageResponse(gex.StatusCode, gex.StatusMessage, gex.StatusDescription)).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
				SendException(ex, new Dictionary<string, string> {
					{ "Endpoint Method", prefix }
				});

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, errorMessage)).ConfigureAwait(false);
			}

		}
    }
}
