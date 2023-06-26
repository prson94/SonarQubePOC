using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core;
using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.exceptions;
using d360.core.resources;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.utils.excel;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using Microsoft.Web.Http;
using Newtonsoft.Json;

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
			Route("reassignmentByUid/objects/{workflowItemUid:Guid}"),
			ApiExplorerSettings(IgnoreApi = true),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(IEnumerable<WorkflowReassignmentAssetApiModel>))
		]
		public async Task<IHttpActionResult> GetWorkflowReassignmentAssetsByUid(Guid workflowItemUid, string query, CancellationToken cancellationToken)
		{
			var result = Company.WorkflowItems.FirstOrDefault(i => i.UID == workflowItemUid);

			if (result == null)
			{
				throw new NotFoundBusinessLayerException(WorkflowApiMessages.WorkflowInstanceNotFound);
			}

			return Ok(await workflowRepository.GetWorkflowReassignmentAssets(result.ID, query, cancellationToken: cancellationToken));
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
			SwaggerParameter("_actionsOnly", "If true only assignments where the workflow is triggered by an action are returned. Default value is false", DataType = "boolean", ParameterType = "query", Required = false),
			SwaggerConsumes("application/json"), 
			SwaggerProduces("application/json", "application/octet-stream"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowAssignmentApiModel)),
			SwaggerResponse(HttpStatusCode.NotFound, "Initiator not found based on initiatorUid provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve the workflow assignments is invalid, possibly due to an incorrectly formatted identifier/parameter.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetWorkflowAssignments()
		{
			var prefix = "Workflow.GetWorkflowAssignments => ";
			var queryParams = Request.GetQueryNameValuePairs();
			try
			{
				var isValid = isPageSizeAndNumValid(queryParams);
				var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;

				if (!string.IsNullOrEmpty(isValid))
				{
					return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, isValid));
				}

				if (!validator.IsValidDirectionForWorkflowGetModel(queryParams))
				{
					return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest,ApiMessages.InvalidDirection));
				}
					
				if (queryParams.ToList().Any(q => q.Key.ToLower() == "_initiatoruid"))
				{
					string initiatorUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_initiatoruid").Value;
					if (!Guid.TryParse(initiatorUidString, out Guid initiatorUid))
					{
						return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(WorkflowApiMessages.InvalidGuid, initiatorUidString, "_initiatorUid")));
					}
					else
					{
						var initiator = Company.GlobalReportingResources.Where(u=>u.Uid==initiatorUid).FirstOrDefault();

						if (initiator == null)
						{
							return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.InvalidInitiatorUid));							
						}
					}
				}				

				var response = await workflowRepository.GetWorkflowAssignmentList(queryParams).ConfigureAwait(false);

				if (isStreamResponse)
				{
					ExcelDocument document = CreateResponseDocumentForAssignmentsExport(response, queryParams);				
					var stream = new MemoryStream();
					var sldoc = document.ToSLDocument();
					sldoc.SelectWorksheet(ExcelExports.WorkflowAssignments_Assignments);
					sldoc.SaveAs(stream);
					byte[] bytes = stream.ToArray();

					return ResponseMessage(createFileResponseMessage(HttpStatusCode.OK, $"{string.Format(document.Name.GetSafeFilename(), DateTime.Now.ToString("ddd MMM dd yyyy"))}.xlsx", bytes));
				}
			
				return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
			}
			catch (ArgumentException aex)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, aex.Message)).ConfigureAwait(false);
			}
			catch (GenericException gex)
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

		/// <summary>
		/// Get the details for a specific workflow item.
		/// </summary>
		/// <param name="workflowItemUid">Workflow item instance unique identifier</param>
		/// <returns></returns>
		[
			HttpGet,
			Route("item/{workflowItemUid:Guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowItemDetails)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve the workflow item details failed.", typeof(WorkflowItemDetails))
		]
		public async Task<IHttpActionResult> GetWorkflowItemDetails (Guid workflowItemUid)
		{
			var workflowItem = Company.WorkflowItems.Where(wi => wi.UID == workflowItemUid).FirstOrDefault();

			if (workflowItem == null)
			{
				return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(WorkflowApiMessages.WorkflowItemUidNotFound, workflowItemUid.ToString())));
			}

			return Ok(await workflowRepository.GetWorkflowItemDetails(workflowItemUid));
		}

		[
			HttpGet,
			Route("assignmentsByVersion"),
			SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_order", "The name of the field to order results by, ascending. Options are workflowName, version and outstanding. By default the results are ordered by UpdatedOn.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of an assignment. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),						
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowInstanceDetailsByVersionAPIModel)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve the workflow assignments is invalid, possibly due to an incorrectly formatted identifier/parameter.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetWorkflowInstanceDetailsByVersion()
		{
			var prefix = "Workflow.GetWorkflowInstanceByVersion => ";
			var queryParams = Request.GetQueryNameValuePairs();
			try
			{
				var isValid = isPageSizeAndNumValid(queryParams);

				if (!string.IsNullOrEmpty(isValid))
				{
					return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, isValid));
				}

				if (!validator.IsValidDirectionForWorkflowGetModel(queryParams))
				{
					return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidDirection));
				}

				var response = await workflowRepository.GetWorkflowInstanceDetailsByVersion(queryParams).ConfigureAwait(false);				

				return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
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
		
		/// <summary>
		/// Get the possible Assignees for which an open workflow instance exists.
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("possibleAssignees"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "List of Resource Uids with associated resource name"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetPossibleAssignees()
		{			
			return Ok(await workflowRepository.GetPossibleAssignees());
		}

		/// <summary>
		/// Get the possible Initiators for which a workflow instance exists.
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("possibleInitiators"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "List of Resource Uids with associated resource name"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetPossibleInitiators()
		{
			return Ok(await workflowRepository.GetPossibleInitiators());
		}

		/// <summary>
		/// Get the asset types for which a workflow exists.
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("relevantAssetTypes"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "List of Asset type Uids with associated type name"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetRelevantAssetTypes()
		{
			return Ok(await workflowRepository.GetRelevantAssetTypes());
		}

		/// <summary>
		/// Create the Excel document for export
		/// </summary>
		/// <returns>A spreadsheet populated with a list of Assignments/Requests</returns>
		private ExcelDocument CreateResponseDocumentForAssignmentsExport(WorkflowAssignmentApiModel assignments, IEnumerable<KeyValuePair<string, string>> queryParams)
		{			

			var exportName = ExcelExports.WorkflowAssignments_Assignments;
			var hasSingleActionFilter = false;
			var isRequestExport = false;
			Guid actionTypeUid = new Guid();
			var actionTypeName = "";
			List<core.entities.FieldType> fieldTypes = new List<core.entities.FieldType>();
			var filterValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
			var initiatorUid = "";

			if (queryParams.ToList().Any(q => q.Key.ToLower() == "_initiatoruid"))
			{
				initiatorUid = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_initiatoruid").Value;
				
				if(queryParams.ToList().Any(q => q.Key.ToLower() == "_actionsonly") && queryParams.FirstOrDefault(x => x.Key.ToLower() == "_actionsonly").Value.ToLower() == "true")
				{
					exportName = ExcelExports.WorkflowAssignments_Requests;
					isRequestExport = true;
				}				
			}

			if (!string.IsNullOrEmpty(filterValue) && Regex.Matches(filterValue, "actionTypeUid", RegexOptions.IgnoreCase).Count == 1 && filterValue.Substring(filterValue.IndexOf("actionTypeUid") + 13).TrimStart().StartsWith("eq"))
			{
				var actionTypeValue = filterValue.Substring(filterValue.IndexOf("actionTypeUid")).Substring(filterValue.IndexOf("'")+1);
				hasSingleActionFilter = true;

				if (!string.IsNullOrEmpty(actionTypeValue))
				{
					actionTypeValue = actionTypeValue.Substring(0, actionTypeValue.IndexOf("'"));

					if (Guid.TryParse(actionTypeValue, out actionTypeUid))
					{
						var issueType = Company.IssueTypes.Where(i => i.uid == actionTypeUid).SingleOrDefault();
						
						if (issueType == null)
						{
							throw new ArgumentException(Workflows.InvalidActionTypeUid);
						}

						actionTypeName = issueType.Name;			
						exportName = $"{issueType.Name} {exportName}";	
						fieldTypes = Company.FieldTypes.Where(ft => ft.IssueTypeID == issueType.ID).ToList();
					}
					else
					{
						throw new ArgumentException(Workflows.InvalidActionTypeUid);						
					}
				}
			}

			var headers = new List<ExcelRow>();
			var assignmentSheetRows = new List<ExcelRow>();

			var headerRow = new ExcelRow
									{
										ExcelExports.WorkflowMonitor_WorkflowName,
										ExcelExports.WorkflowAssignments_AssociatedWith,
									};

			if (!hasSingleActionFilter)
			{
				headerRow.Add(ExcelExports.WorkflowMonitor_Type);
				headerRow.Add(ExcelExports.WorkflowMonitor_TypeName);
			}

			if (!isRequestExport)
			{
				headerRow.Add(ExcelExports.WorkflowMonitor_Initiator);
			}
				
			headerRow.Add(ExcelExports.WorkflowAssignments_Initiated);
			headerRow.Add(ExcelExports.WorkflowAssignments_Assignees);
			headerRow.Add(ExcelExports.WorkflowMonitor_Completed);
			headerRow.Add(ExcelExports.WorkflowMonitor_Status);
														
			foreach(var fieldtype in fieldTypes)
			{
				headerRow.Add(fieldtype.FriendlyName);
			}

			headerRow.Add(ExcelExports.WorkflowMonitor_WorkflowInstanceUID);
			headerRow.Add(ExcelExports.WorkflowMonitor_Url);

			headers.Add(headerRow);			

			foreach(var item in assignments.items)
			{
				var row = new ExcelRow();

				row.Add(item.workflowName);
				row.Add(item.assetDisplayValue);
				if (!hasSingleActionFilter)
				{
					row.Add(item.initiatingObjectType);
					row.Add(item.initiatingObjectTypeName);
				}

				if (!isRequestExport)
				{
					row.Add(item.initiator);
				}

				row.Add(item.StartedOn.ToString());

				var assigneesStr = "";
				if (!string.IsNullOrWhiteSpace(item.assigneesJson))
				{
					List<WorkflowAssignee> assignees = JsonConvert.DeserializeObject<List<WorkflowAssignee>>(item.assigneesJson);
					assigneesStr = assignees.Count > 0 ? string.Join("|", assignees.Select(a => a.Name).ToArray()) : "";
				}
				row.Add(assigneesStr);
				row.Add(item.CompletedOn?.ToString());
				row.Add(item.Status);

				var itemRow = (IDictionary<string, object>)item;
				foreach (var fieldtype in fieldTypes)
				{					
					var fieldValue = itemRow[fieldtype.Name]?.ToString();
					row.Add(fieldValue);
				}

				row.Add(item.workflowItemUid.ToString());
				row.Add($"/workflow/details/{item.workflowItemUid}");
				assignmentSheetRows.Add(row);
			}

			var exportSheetRows = new List<ExcelRow> {
				new ExcelRow { ExcelExports.WorkflowAssignments_ExportDate, DateTime.Now.ToString("mm/dd/yyyy hh:mm:ss")},
				new ExcelRow { ExcelExports.Common_PageSize, assignments.pageSize.ToString()},
				new ExcelRow { ExcelExports.Common_PageNum, assignments.pageNum.ToString()},
				new ExcelRow { ExcelExports.Common_Total, assignments.total.ToString()}
			};

			if (isRequestExport)
			{
				exportSheetRows.Add(new ExcelRow { ExcelExports.WorkflowMonitor_Initiator, assignments.items[0].initiator });
				exportSheetRows.Add(new ExcelRow { ExcelExports.WorkflowAssignments_InitiatorUid, initiatorUid });
			}

			if (hasSingleActionFilter)
			{
				exportSheetRows.Add(new ExcelRow { ExcelExports.WorkflowAssignments_ActionTypeName, actionTypeName });
				exportSheetRows.Add(new ExcelRow { ExcelExports.WorkflowAssignments_ActionTypeUID, actionTypeUid.ToString() });
			}

			var document = new ExcelDocument(string.Format(ExcelExports.Common_ExportName, exportName, DateTime.Now.ToString("ddd MMM dd yyyy")))
			{
				new ExcelSheet(ExcelExports.WorkflowAssignments_Assignments)
				{
					HeaderRows = headers,

					ValueRows = assignmentSheetRows,
				},

				new ExcelSheet(ExcelExports.WorkflowAssignments_ExportInfoTab)
				{
					ValueRows = exportSheetRows
				}
			};

			return document;
		}
	}
}
