using d360.core.entities.Workflow;
using d360.extensions;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using System.Web.Http.Description;
using System.Threading;
using Resources;

namespace d360.web.Controllers.V2
{

    [
    ApiVersion("2.0"),
    RoutePrefix("api/v{version:apiVersion}/workflow"),
    Authorize
]
    public class WorkflowController  : BaseV2ApiController
    {
        #region DI

        IWorkflowRepository workflowRepository;
        IWorkflowApiModelValidator validator;

        public WorkflowController(CoreComponentSet set, IWorkflowRepository workflowRepository, IWorkflowApiModelValidator validator): base(set)
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
            var prefix = "Workflow.GetWorkflowTypeAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();


                if (!validator.IsValidGuidCountForWorkflowGetTypeModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.MoreThanOneUidPassed)).ConfigureAwait(false);
                }

                if (!validator.IsValidGuidForWorkflowGetTypeModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.InvalidTypeWorkflowVersionRequest)).ConfigureAwait(false);
                }

                if (!this.validator.IsValidAssetType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, this.GetUidFromQueryParams(queryParams, "AssetTypeUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidActionType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.ActionTypeUidIsNotValid, this.GetUidFromQueryParams(queryParams, "ActionTypeUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidRelationshipType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.RelationShipTypeUidNotFound, this.GetUidFromQueryParams(queryParams, "RelationshipTypeUid")))).ConfigureAwait(false);
                }


                var workflowtypes = await this.workflowRepository.GetWorkflowTypes(queryParams);
                 return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowtypes))).ConfigureAwait(false);


            } catch (Exception ex) {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage)).ConfigureAwait(false);
            }


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
            var prefix = "Workflow.GetWorkflowVersionAsync => ";
            var errorMessage = "";
            try
            {
                

                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , isValid)).ConfigureAwait(false);
                }


                if (!validator.IsValidGuidCountForWorkflowGetVersionModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.MoreThanOneTypeUidPassedInclWorkflow)).ConfigureAwait(false);
                }

                if (!validator.IsValidOrderByFieldForWorkflowGetVersionModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.InvalidParameterWorkflowVersion)).ConfigureAwait(false);
                }

                if (!validator.IsValidGuidForWorkflowGetVersionModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.InvalidTypeWorkflowVersionRequestIncWF)).ConfigureAwait(false);
                }

                if (!this.validator.IsValidAssetType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, this.GetUidFromQueryParams(queryParams, "AssetTypeUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidActionType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, this.GetUidFromQueryParams(queryParams, "ActionTypeUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidRelationshipType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.RelationShipTypeUidNotFound, this.GetUidFromQueryParams(queryParams, "RelationshipTypeUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidWorkflowType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(WorkflowApiMessages.WorkflowtypeUIDNotFound, this.GetUidFromQueryParams(queryParams, "WorkflowTypeUid")))).ConfigureAwait(false);
                }

                var workflowVersions = await this.workflowRepository.GetWorkflowVersions(queryParams);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowVersions))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Retrieves workflow versions  steps for the given workflow version unique identifier .
        /// </summary>
        /// <param name="workflowVersionUid"> workflow version unique identifier</param>
        /// <returns>Returns list of workflow version steps and An HTTP status code</returns>
        [HttpGet,Route("versions/{workflowVersionUid}/steps"),
        SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
        SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowVersionStepsApiViewModel)),
        SwaggerResponse(HttpStatusCode.NotFound, "Workflow Version  not found based on Uid provided.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow version steps is invalid, possibly due to an incorrectly formatted  workflow version unique identifier.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))]
       
        public async Task<IHttpActionResult> GetWorkflowVersionStepsAsync(Guid workflowVersionUid)
        {
            var prefix = "Workflow.GetWorkflowVersionStepsAsync => ";
            var errorMessage = "";
            try {

                if (!this.validator.IsValidWorkflowVersion(workflowVersionUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(WorkflowApiMessages.WorkflowVersionUIDNotFound, workflowVersionUid.ToString()))).ConfigureAwait(false);
                }


                var workflowVersionSteps = await this.workflowRepository.GetWorkflowVersionSteps(workflowVersionUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowVersionSteps))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage)).ConfigureAwait(false);
            }
        }

        private Guid GetUidFromQueryParams(IEnumerable<KeyValuePair<string, string>> queryParams,string parameterName)
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
        [HttpGet,Route("{workflowUid}/steps"),
        SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
        SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowInstanceApiViewModel)),
        SwaggerResponse(HttpStatusCode.NotFound, "Workflow Instance  not found based on Uid provided.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow instance is invalid, possibly due to an incorrectly formatted  workflow instance unique identifier.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))]

        public async Task<IHttpActionResult> GetWorkflowInstances(Guid workflowUid)
        {
            var prefix = "Workflow.GetWorkflowInstances => ";
            var errorMessage = "";
            try
            {
                if (!this.validator.IsValidWorkflowInstance(workflowUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(WorkflowApiMessages.WorkflowUIDNotFound, workflowUid.ToString()))).ConfigureAwait(false);
                }

                var workflowInstances = await this.workflowRepository.GetWorkflowInstances(workflowUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowInstances))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage)).ConfigureAwait(false);
            }
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
            var prefix = "Workflow.GetWorkflowsAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , isValid)).ConfigureAwait(false);
                }


                if (!validator.IsValidGuidCountForGetWorkflowModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.MoreThanOneUidPassed)).ConfigureAwait(false);
                }

                if (!validator.IsValidOrderByFieldForGetWorkflowModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.InvalidOrderParameterPassed)).ConfigureAwait(false);
                }

                if (!validator.IsValidDirectionForWorkflowGetModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.InvalidDirection)).ConfigureAwait(false);
                }

                if (!validator.IsValidGuidForGetWorkflowModel(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , WorkflowApiMessages.InvalidUidWorkflowVersionRequest)).ConfigureAwait(false);
                }

                if (!this.validator.IsValidAsset(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetNotFound, this.GetUidFromQueryParams(queryParams, "AssetUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidAction(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.ActionUidNotFound, this.GetUidFromQueryParams(queryParams, "ActionUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidRelationship(queryParams)) 
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(RelationshipsApiMessages.RelationShipUidNotFound, this.GetUidFromQueryParams(queryParams, "RelationshipTypeUid")))).ConfigureAwait(false); 
                }

                if (!this.validator.IsValidWorkflowType(queryParams)) 
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(WorkflowApiMessages.WorkflowtypeUIDNotFound, this.GetUidFromQueryParams(queryParams, "WorkflowTypeUid")))).ConfigureAwait(false); 
                }

                if (!this.validator.IsValidWorkflowVersion(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(WorkflowApiMessages.WorkflowVersionUIDNotFound, this.GetUidFromQueryParams(queryParams, "versionUid")))).ConfigureAwait(false);
                }



                var workflows = await this.workflowRepository.GetWorkflows(queryParams);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflows))).ConfigureAwait(false);


            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage)).ConfigureAwait(false);
            }

        }

        [
            HttpGet,
            Route("type/{uid}/id"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(int))
        ]
        public IHttpActionResult GetWorkflowtypeId(Guid uid)
        {
            var prefix = "Workflow.GetWorkflowtypeId => ";
            var errorMessage = "";
            try {
               var result = this.workflowRepository.GetWorkflowTypeByUID(uid);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result.ID));
            } catch (Exception ex) {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });
                return this.errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage);
            }
            
        }

        [
            HttpGet,
            Route("{uid}/legacyData"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(int))
        ]
        public IHttpActionResult GetWorkflowId(Guid uid)
        {
            var prefix = "Workflow.GetWorkflowId => ";
            var errorMessage = "";
            try
            {
                var result = this.workflowRepository.GetWorkflowItemByUID(uid);

                if(result == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.WorkflowInstanceNotFound));
                
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result.ID));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });
                return this.errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage);
            }

        }

        
        [
            HttpGet,
            Route("reassignment/objects/{id:int}"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(IEnumerable<WorkflowReassignmentAssetApiModel>))
        ]
        public async Task<IHttpActionResult> GetWorkflowReassignmentAssets(int id, string query, CancellationToken cancellationToken)
        {
            var prefix = "Workflow.GetWorkflowReassignmentAssets => ";
            var errorMessage = "";
            try
            {

                var result = Company.WorkflowItems.FirstOrDefault(i => i.ID == id);

                if (result == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.WorkflowInstanceNotFound));

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, await workflowRepository.GetWorkflowReassignmentAssets(id, query, cancellationToken: cancellationToken)));
            }
            catch (Exception ex)
            {
               errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });
                return this.errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage);
            }

        }
    }
}
