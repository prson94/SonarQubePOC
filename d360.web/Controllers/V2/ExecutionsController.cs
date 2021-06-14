using Microsoft.Web.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using d360.model;
using d360.web.Filters;
using Swashbuckle.Swagger.Annotations;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using d360.core.entities;
using d360.web.Models;
using d360.core.queue;
using d360.extensions;
using Newtonsoft.Json;
using Resources;
using d360.core.enums;

namespace d360.web.Controllers.V2
{
    [
    ApiVersion("2.0"),
    RoutePrefix("api/v{version:apiVersion}/executions"), Authorize
    ]
    public class ExecutionsController : BaseV2ApiController
    {
        #region DI

        IAssetRepository AssetRepository;        
        IStorageProvider Storage;
        public ExecutionsController(ICommunityContext community, ICompanyContext company, IAssetRepository repository, IStorageProvider storage)
            : base(community, company)
        {
            Storage = storage;
            AssetRepository = repository;
            Company = company;
        }

        #endregion

        #region Executions
        /// <summary>
        /// GETs all execution records, including the results for the execution.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route(""),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of all execution statuses.", typeof(APIExecutionAPIModelResult)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by CompletedOn.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),

        ]
        public async Task<IHttpActionResult> GetExecutions()
        {
            
            var queryParams = Request.GetQueryNameValuePairs();

            string isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid));
            }

            var executions = await AssetRepository.GetExecutionItems(queryParams);
            if (executions.StatusCode != HttpStatusCode.OK)
            {
                return await Task.FromResult(errorMessageResponse(executions.StatusCode, "Invalid request", executions.Message));
            }
            return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            executions
                        )
                    )
                );
        }

        /// <summary>
        /// Cancel an API Execution by Execution UID
        /// </summary>
        /// <returns></returns>
        [
            HttpDelete,
            Route("{executionID:Guid}"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A success message.", typeof(ConfirmResponse)),

        ]
        public async Task<IHttpActionResult> CancelExecution(Guid executionID)
        {
            var prefix = "Executions.DeleteExecution => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));
                }

                var response = new ConfirmResponse();
                var execution = Company.ApiExecutions.FirstOrDefault(x => x.ExecutionID == executionID);

                if (execution == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"Execution with UID {executionID} does not exist."));
                }

                if (execution.State == core.enums.State.Deleted)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"Execution with UID {executionID} has been already canceled."));
                }

                if (execution.CompletedOn != null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"Execution with UID {executionID} has finished and cannot be canceled."));
                }

                if (execution.ProcessingStartedOn != null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"Execution with UID {executionID} has started and cannot be canceled."));
                }

                if (!execution.Route.Contains("batch"))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"Execution with UID {executionID} is not a batch job and cannot be canceled."));
                }

                execution.State = core.enums.State.Deleted;
                execution.CompletedOn = DateTime.UtcNow;
                execution.ErrorMessage = "Execution job was canceled by user.";

                bool isDone = Company.Update(execution);

                response.message = $"Execution with UID {executionID} has been cancelled successfully.";
                if (isDone)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Invalid request", $"Something went wrong while canceling Execution."));
                }

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "ExecutionID", executionID.ToString() },
                    { "ExecutionUid", executionID.ToString() }, //left to prevent a breaking change
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }

        /// <summary>
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <param name="executionID">The execution's unique identifier to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{executionID:Guid}"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerParameter("summaryOnly", "When true the results are omitted from the response. The default value is false.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of assets.", typeof(ApiExecutionStatusModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your status was not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionID)
        {

            var prefix = "Executions.GetExecutionStatus => ";
            var errorMessage = "";
            var summaryOnly = false;
            var queryParams = Request.GetQueryNameValuePairs();

            try
            {
                if (queryParams.ToList().Any(x => x.Key.ToLower() == "summaryonly"))
                {
                    bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "summaryonly").Value, out summaryOnly);
                }

                var res = await AssetRepository.GetExecutionStatusModel(executionID, !summaryOnly);
                if (res == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution unique identifier not found."));
                }
                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            res as object
                        )
                    )
                );
            }
            catch(ArgumentException)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution unique identifier not found."));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "ExecutionID", executionID.ToString() },
                    { "ExecutionUid", executionID.ToString() }, //left to prevent a breaking change
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Added analyze connector status and needed immediate result.
        /// </summary>        

        /// <remarks>
        /// Status Possible values are:  
        /// - START
        /// - COMPLETE_SUCCESS
        /// - COMPLETE_FAILURE
        /// - INFORMATION
        /// </remarks>

        /// <param name="model">The status of connector to be add.</param>
        /// <returns>The required values of status of connector.</returns>
        [
            HttpPost,
            Route("external"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The status of connector was added, returns the required values of the added connector status.", typeof(ApiExecutionExternalViewModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostConnectorStatus(ApiExecutionExternalRequestModel model)
        {
            if (model == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", "You have submitted an invalid or empty request please check your request and try again.")).ConfigureAwait(false);
            }

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
            }
            if (model?.Status == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", "Status parameter value is required.")).ConfigureAwait(false);
            }
            if (model.Component?.Length > 250)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", "Component cannot exceed 250 characters.")).ConfigureAwait(false);
            }

  
            if (!Enum.IsDefined(typeof(ExecutionExternalStatus),model.Status))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid value for Status. Allowed values are: START, COMPLETE_SUCCESS, COMPLETE_FAILURE, INFORMATION"));
            }

            try
            {
                ApiExecutionExternalViewModel result = AssetRepository.AddConnectorStatus(model);

                return ResponseMessage(Request.CreateResponse<ApiExecutionExternalViewModel>(HttpStatusCode.OK, result));
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Error while add status of a connector endpoint", e.Message);
            }

        }

        /// <summary>
        /// GETs connector status record.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("external"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your externalid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of connector status.", typeof(APIExecutionExternalAPIModelResult)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by CreatedOn desc, then ExternalID if the dates are the same", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_startDate", "Start date to get data for limit result on createdon column.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_endDate", "End date to get data for limit result on createdon column.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("externalId", "Filter by external ID.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("component", "Filter by component.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("status", "Filter by component status. Allowed values are START, COMPLETE_SUCCESS, COMPLETE_FAILURE, INFORMATION", DataType = "string", ParameterType = "query", Required = false),

        ]
        public async Task<IHttpActionResult> GetConnectorStatus()
        {

            var queryParams = Request.GetQueryNameValuePairs();
            DateTime? _startDate = null;
            DateTime? _endDate = null;
            Guid? externalId = null;
            string status = null;
            string component = null;

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
            }

            string isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid));
            }


            #region "Filter Condition"
            if (queryParams.Any(x => x.Key == "status"))
            {
                status = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "status").Value;
                if (!Enum.IsDefined(typeof(ExecutionExternalStatus), status))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid value for Status. Allowed values are: START, COMPLETE_SUCCESS, COMPLETE_FAILURE, INFORMATION")).ConfigureAwait(false);
                }
            }

            if (queryParams.Any(q => q.Key == "_startDate"))
            {
                DateTime _tempstartDate;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_startDate").Value, out _tempstartDate))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"Start date is not valid.");
                }
                _startDate = _tempstartDate;

                if (_startDate == DateTime.MinValue)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"Start date is not valid.");
                }
            }

            if (queryParams.Any(q => q.Key == "_endDate"))
            {
                DateTime _tempendDate;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_endDate").Value, out _tempendDate))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"End date is not valid.");
                }
                _endDate = _tempendDate;

                if (_endDate == DateTime.MaxValue)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"End date is not valid.");
                }

                if (_startDate != null && DateTime.Compare((DateTime)_endDate, (DateTime)_startDate) < 0)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"End date must be later than start date.");
                }
            }

            if (queryParams.Any(q => q.Key == "externalId"))
            {
                Guid tempexternalId;
                if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "externalId").Value, out tempexternalId))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Uid", $"externalId {queryParams.ToList().FirstOrDefault(q => q.Key == "externalId").Value} is not a valid Uid")).ConfigureAwait(false);
                }

                externalId = tempexternalId;

                long results = Company.Query<int>(@"select count(1) 
                                                   from [api].[ExecutionExternal] 
                                                   where externalid = @externalId", new { externalId }).FirstOrDefault();


                if (results <= 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Connetor status with externalId {externalId} could not be found.")).ConfigureAwait(false);
                }
            }

            if (queryParams.Any(q => q.Key.ToLower() == "component"))
            {
                component = queryParams.FirstOrDefault(x => x.Key.ToLower() == "component").Value;
                if (string.IsNullOrEmpty(component))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"component is not valid.");
                }
                else if (component.Length > 250)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Parameter", $"Component cannot exceed 250 characters.");
                }

            }

            #endregion


            var executions = await AssetRepository.GetConnectorStatusItems(queryParams, _startDate, _endDate, externalId, component, status);
            if (executions.StatusCode != HttpStatusCode.OK)
            {
                return await Task.FromResult(errorMessageResponse(executions.StatusCode, "Invalid request", executions.Message));
            }
            return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            executions
                        )
                    )
                );
        }
        #endregion
    }
}