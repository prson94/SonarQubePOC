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
        /// Retrieves workflow types for the given asset type unique identifier / action type unique identifier/ relationship unique identifier .
        /// </summary>
        /// <param name="ActionTypeUid">Action Type unique identifier</param>
        /// <param name="AssetTypeUid">Asset Type unique identifier</param>
        /// <param name="RelationshipTypeUid">Relationship unique identifier</param>
        /// <param name="ChangeType">ChangeType</param>
        /// <param name="State">State</param>
        /// <returns>Returns list of workflow types and An HTTP status code </returns>
        [
        HttpGet,
        Route("types"),
        SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
        SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowTypeApiViewModel)),
        SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetWorkflowTypeAsync(Guid? ActionTypeUid=null,Guid? AssetTypeUid=null,Guid? RelationshipTypeUid=null, ChangeType? ChangeType = null, State? State = null)
        {
            var prefix = "Workflow.GetWorkflowTypeAsync => ";
            var errorMessage = "";

            try
            {
                List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();

                if (ActionTypeUid.HasValue)
                    queryParams.Add(new KeyValuePair<string, string>("ActionTypeUid", ActionTypeUid.ToString()));

                if (AssetTypeUid.HasValue)
                    queryParams.Add(new KeyValuePair<string, string>("AssetTypeUid", AssetTypeUid.ToString()));

                if (RelationshipTypeUid.HasValue)
                    queryParams.Add(new KeyValuePair<string, string>("RelationshipTypeUid", RelationshipTypeUid.ToString()));

                if (ChangeType.HasValue)
                    queryParams.Add(new KeyValuePair<string, string>("ChangeType", ChangeType.ToString()));

                if (State.HasValue)
                    queryParams.Add(new KeyValuePair<string, string>("State", State.ToString()));



                if (!validator.ValidateWorkflowGetTypeModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request, either ActionTypeUid OR AssetTypeUid OR RelationshipTypeUid"));
                
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
        /// Retrieves workflow versions for the given asset type unique identifier / action type unique identifier/ relationship unique identifier .
        /// </summary>
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
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow type is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid.", typeof(ErrorResponse)),
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request, either  ActionTypeUid OR AssetTypeUid OR RelationshipTypeUid"));

                if (!validator.IsValidGuidForWorkflowGetVersionModel(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Your request to retrieve this workflow version is invalid, possibly due to an incorrectly formatted identifier ActionTypeUid/AssetTypeUid/RelationshipTypeUid/WorkflowTypeUid"));


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
        SwaggerResponse(HttpStatusCode.OK, "", typeof(WorkflowVersionSteps)),
        SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this workflow version steps is invalid, possibly due to an incorrectly formatted  workflow version unique identifier.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))]
       
        public async Task<IHttpActionResult> GetWorkflowVersionStepsAsync(Guid workflowVersionUid)
        {
            var prefix = "Workflow.GetWorkflowVersionStepsAsync => ";
            var errorMessage = "";
            try {
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
    }
}
