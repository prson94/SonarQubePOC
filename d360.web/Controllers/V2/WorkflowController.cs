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

        public WorkflowController(ICommunityContext community, ICompanyContext company, 
            IWorkflowRepository workflowRepository, IWorkflowApiModelValidator validator)
            : base(community, company)
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
    SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
   ]
       
        public async Task<IHttpActionResult> GetWorkflowTypeAsync(ChangeType? ChangeType = null, State? State = null)
        {
            var prefix = "Workflow.GetWorkflowTypeAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

               
                if (!validator.IsValidGuidCountForWorkflowGetTypeModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "More than one uid is passed in the request, either  ActionTypeUid OR AssetTypeUid OR RelationshipTypeUid"));

                if (!validator.IsValidGuidForWorkflowGetTypeModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Your request to retrieve this workflow version is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid"));

                if (!this.validator.IsValidAssetType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {this.GetUidFromQueryParams(queryParams, "AssetTypeUid")} could not be found."));

                if (!this.validator.IsValidActionType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Action Type with Uid {this.GetUidFromQueryParams(queryParams, "ActionTypeUid")} could not be found."));

                if (!this.validator.IsValidRelationshipType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Relationship Type with Uid {this.GetUidFromQueryParams(queryParams, "RelationshipTypeUid")} could not be found."));


                var workflowtypes = await this.workflowRepository.GetWorkflowTypes(queryParams);
                 return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowtypes)));


            } catch (Exception ex) {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
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
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by VersionNumber.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowVersionsApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Action Type / Asset Type / Relationship Type / Workfflow Type  not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid/WorkflowTypeUid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ]
        public async Task<IHttpActionResult> GetWorkflowVersionAsync(State? State = null)
        {
            var prefix = "Workflow.GetWorkflowVersionAsync => ";
            var errorMessage = "";
            try
            {
                

                var queryParams = Request.GetQueryNameValuePairs();


                if (!validator.IsValidGuidCountForWorkflowGetVersionModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "More than one uid is passed in the request , either  ActionTypeUid OR AssetTypeUid OR RelationshipTypeUid or WorkflowTypeUid"));

                if (!validator.IsValidOrderByFieldForWorkflowGetVersionModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid order passed in the request. Valid values are: VersionNumber, State, CreatedOn, and UpdatedOn"));

                if (!validator.IsValidGuidForWorkflowGetVersionModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Your request to retrieve this workflow version is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid/WorkflowTypeUid"));

                if (!this.validator.IsValidAssetType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {this.GetUidFromQueryParams(queryParams, "AssetTypeUid")} could not be found."));

                if (!this.validator.IsValidActionType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Action Type with Uid {this.GetUidFromQueryParams(queryParams, "ActionTypeUid")} could not be found."));

                if (!this.validator.IsValidRelationshipType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Relationship Type with Uid {this.GetUidFromQueryParams(queryParams, "RelationshipTypeUid")} could not be found."));

                if (!this.validator.IsValidWorkflowType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Workflow Type with Uid {this.GetUidFromQueryParams(queryParams, "WorkflowTypeUid")} could not be found."));

                var workflowVersions = await this.workflowRepository.GetWorkflowVersions(queryParams);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowVersions)));

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
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
        SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))]
       
        public async Task<IHttpActionResult> GetWorkflowVersionStepsAsync(Guid workflowVersionUid)
        {
            var prefix = "Workflow.GetWorkflowVersionStepsAsync => ";
            var errorMessage = "";
            try {

                if (!this.validator.IsValidWorkflowVersion(workflowVersionUid))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Workflow Version with Uid {workflowVersionUid.ToString()} could not be found."));


                var workflowVersionSteps = await this.workflowRepository.GetWorkflowVersionSteps(workflowVersionUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowVersionSteps)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        private Guid GetUidFromQueryParams(IEnumerable<KeyValuePair<string, string>> queryParams,string parameterName)
        {
            Guid uid = Guid.Empty;
            
            if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName.ToLower()))
            {
                var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName.ToLower()).Value;
                if (!Guid.TryParse(uidString, out uid))
                    uid = Guid.Empty;
     
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
        SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))]

        public async Task<IHttpActionResult> GetWorkflowInstances(Guid workflowUid)
        {
            var prefix = "Workflow.GetWorkflowInstances => ";
            var errorMessage = "";
            try
            {
                if (!this.validator.IsValidWorkflowInstance(workflowUid))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Workflow  Uid {workflowUid.ToString()} could not be found."));

                var workflowInstances = await this.workflowRepository.GetWorkflowInstances(workflowUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflowInstances)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Get a list of workflows contained within the system.
        /// </summary>
        /// <param name="State">Active: is the workflow in an activate (non-completed) state.</param>
        /// <returns>Returns list of workflows and a HTTP status code</returns>
        [
        HttpGet,
            Route(""),
            SwaggerParameter("WorkflowTypeUid", "the unique identifier for the workflow type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("VersionUid", "the unique identifier for the workflow version.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("ActionUid", "issue that the workflow is registered to", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AssetUid", "asset that the workflow is registered to", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("RelationshipUid", "relationship that the workflow is registered to", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by StartedOn.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowsApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Action / Asset / Relationship / Workfflow Type / Workflow Version  not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid/WorkflowTypeUid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ]
        public async Task<IHttpActionResult> GetWorkflowsAsync(WorkflowApiState? State = null)
        {
            var prefix = "Workflow.GetWorkflowsAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();


                if (!validator.IsValidGuidCountForGetWorkflowModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "More than one uid is passed in the request , either  ActionUid OR AssetUid OR RelationshipUid"));

                if (!validator.IsValidOrderByFieldForGetWorkflowModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid order passed in the request. Valid values are: StartedOn and CompletedOn"));

                if (!validator.IsValidGuidForGetWorkflowModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Your request to retrieve this workflow version is invalid, possibly due to an incorrectly formatted identifier ActionUid / AssetUid / RelationshipUid / WorkflowTypeUid / WorkflowVerionUid"));

                if (!this.validator.IsValidAsset(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {this.GetUidFromQueryParams(queryParams, "AssetUid")} could not be found."));

                if (!this.validator.IsValidAction(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Action with Uid {this.GetUidFromQueryParams(queryParams, "ActionUid")} could not be found."));

                if (!this.validator.IsValidRelationship(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Relationship  with Uid {this.GetUidFromQueryParams(queryParams, "RelationshipTypeUid")} could not be found."));

                if (!this.validator.IsValidWorkflowType(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Workflow Type with Uid {this.GetUidFromQueryParams(queryParams, "WorkflowTypeUid")} could not be found."));

                if (!this.validator.IsValidWorkflowVersion(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Workflow Version with Uid {this.GetUidFromQueryParams(queryParams, "versionUid")} could not be found."));



                var workflows = await this.workflowRepository.GetWorkflows(queryParams);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, workflows)));


            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }

    }
}
