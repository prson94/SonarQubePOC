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
using static d360.core.entities.Resource;
using SpreadsheetLight;
using System.IO;
using System.Web.Http.Results;
using d360.core;
using DocumentFormat.OpenXml.Packaging;
using System.Text;

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
        public ExecutionsController(ICoreComponentSet set, IAssetRepository repository, IStorageProvider storage)
            : base(set)
        {
            Storage = storage;
            AssetRepository = repository;
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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of all execution statuses.", typeof(APIExecutionAPIModelResult)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by CompletedOn.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_status", "Filter by execution status. Allowed values are Pending, Running, Completed", DataType = "string", ParameterType = "query", Required = false, Enum = typeof(ExecutionInternalStatus)),
        ]
        public async Task<IHttpActionResult> GetExecutions()
        {
            var queryParams = Request.GetQueryNameValuePairs();

            string isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
            }

            if (queryParams.Any(x => x.Key == "_status"))
            {
                string status = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_status").Value;
                if (!Enum.IsDefined(typeof(ExecutionInternalStatus), status))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ExecutionStatusInvalid)).ConfigureAwait(false);
                }
            }

            var executions = await AssetRepository.GetExecutionItems(queryParams);
            if (executions.StatusCode != HttpStatusCode.OK)
            {
                return await Task.FromResult(errorMessageResponse(executions.StatusCode, ApiMessages.InvalidRequest, executions.Message)).ConfigureAwait(false);
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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json"),
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
                }

                var response = new ConfirmResponse();
                var execution = Company.ApiExecutions.FirstOrDefault(x => x.ExecutionID == executionID);

                if (execution == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.ExecutionUIDNotExist, executionID.ToString()))).ConfigureAwait(false);
                }

                if (execution.State == core.enums.State.Deleted)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.ExecutionUIDCancelled, executionID.ToString()))).ConfigureAwait(false);
                }

                if (execution.CompletedOn != null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.ExecutionUIDFinishedCanNotCancel, executionID.ToString()))).ConfigureAwait(false);
                }

                if (execution.ProcessingStartedOn != null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.ExecutionUIDStartedCanNotCancel, executionID.ToString()))).ConfigureAwait(false);
                }

                if (!execution.Route.Contains("batch"))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.ExecutionUIDNotBatchJobCanNotCancel, executionID.ToString()))).ConfigureAwait(false);
                }

                execution.State = core.enums.State.Deleted;
                execution.CompletedOn = DateTime.UtcNow;
                execution.ErrorMessage = ApiMessages.ExecutionCancelByUser;

                bool isDone = Company.Update(execution);

                response.message = string.Format(ApiMessages.ExecutionCancel, executionID.ToString());
                if (isDone)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response))).ConfigureAwait(false);
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InvalidRequest, ApiMessages.ExecutionWrongWhenCancel)).ConfigureAwait(false);
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json"),
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ExecutionUIDNotFound)).ConfigureAwait(false);
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
            catch (ArgumentException)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ExecutionUIDNotFound)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "ExecutionID", executionID.ToString() },
                    { "ExecutionUid", executionID.ToString() }, //left to prevent a breaking change
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
        ///
        /// 
        /// 
        /// ###Configuration###
        /// Configuration value can be any valid JSON object in format of an Array. Object should be places within Array brackets [].
        ///
        /// Configuration value example as KeyValue pairs
        /// ```
        ///[
        ///   {"Framework Version" :"5.4"},
        ///   {"JDBC Harvester Version":"2.5"},
        ///   { "Analyze Version":"3.6.8"},
        ///   { "Flow Name":"MYGRAPH"},
        ///   { "Metadata Source":"MYCRM"}  
        ///]
        /// ```
        ///
        /// Configuration value example as nested object 
        /// ```
        ///[
        ///   {"Framework Version" :
        ///     {
        ///         "JDBC Harvester Version": { 
        ///             "Analyze Version":"3.6.8"
        ///         }
        ///     }
        ///   }
        ///]
        ///```
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
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.ErrorInvalidDatasetMessage)).ConfigureAwait(false);
            }

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
            }
            if (model?.Status == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.StatusRequied)).ConfigureAwait(false);
            }
            if (model.Component?.Length > 250)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.ComponentMaxSize250)).ConfigureAwait(false);
            }


            if (!Enum.IsDefined(typeof(ExecutionExternalStatus), model.Status))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.StatusInvalid));
            }

            try
            {
                ApiExecutionExternalViewModel result = AssetRepository.AddConnectorStatus(model);

                return ResponseMessage(Request.CreateResponse<ApiExecutionExternalViewModel>(HttpStatusCode.OK, result));
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorConnectorStatus, e.Message);
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
            SwaggerParameter("_order", "The name of the field to order results by. By default the results are ordered by CreatedOn desc, then ExternalID if the dates are the same", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered descending.", DataType = "string", ParameterType = "query", Required = false),
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
            DateTime SqlDateTimeMin = (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue;

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
            }

            string isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
            }


            #region "Filter Condition"
            if (queryParams.Any(x => x.Key == "status"))
            {
                status = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "status").Value;
                if (!Enum.IsDefined(typeof(ExecutionExternalStatus), status))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.StatusInvalid)).ConfigureAwait(false);
                }
            }

            if (queryParams.Any(q => q.Key == "_startDate"))
            {
                DateTime _tempstartDate;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_startDate").Value, out _tempstartDate))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.InvalidStartDate);
                }
                _startDate = _tempstartDate;

                if (_startDate == DateTime.MinValue || DateTime.Compare((DateTime)_startDate, SqlDateTimeMin) < 0)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.InvalidStartDate);
                }
            }

            if (queryParams.Any(q => q.Key == "_endDate"))
            {
                DateTime _tempendDate;
                if (!DateTime.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_endDate").Value, out _tempendDate))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.InvalidEndDate);
                }
                _endDate = _tempendDate;

                if (_endDate == DateTime.MaxValue || DateTime.Compare((DateTime)_endDate, SqlDateTimeMin) < 0)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.InvalidEndDate);
                }

                if (_startDate != null && DateTime.Compare((DateTime)_endDate, (DateTime)_startDate) < 0)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.StartEndDateValidation);
                }
            }

            if (queryParams.Any(q => q.Key == "externalId"))
            {
                Guid tempexternalId;
                if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "externalId").Value, out tempexternalId))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(ApiMessages.InvalidExternalID, queryParams.ToList().FirstOrDefault(q => q.Key == "externalId").Value.ToString()))).ConfigureAwait(false);
                }

                externalId = tempexternalId;

                long results = Company.Query<int>(@"select count(1) 
                                                   from [api].[ExecutionExternal] 
                                                   where externalid = @externalId", new { externalId }).FirstOrDefault();


                if (results <= 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ApiMessages.ConnectorStatusNotFound, externalId.ToString()))).ConfigureAwait(false);
                }
            }

            if (queryParams.Any(q => q.Key.ToLower() == "component"))
            {
                component = queryParams.FirstOrDefault(x => x.Key.ToLower() == "component").Value;
                if (string.IsNullOrEmpty(component))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.ComponentNotValid);
                }
                else if (component.Length > 250)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.ComponentMaxSize250);
                }

            }

            #endregion

            try
            {
                var executions = await AssetRepository.GetConnectorStatusItems(queryParams, _startDate, _endDate, externalId, component, status);
                if (executions.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(executions.StatusCode, ApiMessages.InvalidRequest, executions.Message)).ConfigureAwait(false);
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
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }
        #endregion

        /// <summary>
        /// Retrieves bulk load info.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("bulkload"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Gets bulk load info.", typeof(APIExecutionBulkLoadModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within fields.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by. Options are DateStarted, DateCompleted, Action, AssetTypeName, RequestedByName. Default is DateStarted desc", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered descending.", DataType = "string", ParameterType = "query", Required = false),
        ]

        public async Task<IHttpActionResult> GetLoads()
        {
            ResourceApiViewModel model = new ResourceApiViewModel();
            var queryParams = Request.GetQueryNameValuePairs();
            int _pageSize = 200;
            int _pageNum = 1;
            string _direction = "desc";
            string orderBySql = "";
            string offsetSql = "";
            string whereSql = " ";
            string filterValue = "";
            string countSql = "select count(1) from";
            string defaultSelect = "select	* from";

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
            }

            string isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
            }

            #region "Filter Condition"
            if (queryParams.Any(x => x.Key == "_pageSize"))
            {
                int _temppageSize;
                if (!int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_pageSize").Value, out _temppageSize))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, Validation.InvalidPageSize);
                }
                _pageSize = _temppageSize;
            }

            if (queryParams.Any(x => x.Key == "_pageNum"))
            {
                int _temppageNum;
                if (!int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_pageNum").Value, out _temppageNum))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, Validation.InvalidPageNum);
                }
                _pageNum = _temppageNum;
            }
            if (queryParams.Any(x => x.Key == "_direction"))
            {
                var allowedDirections = new[] { "asc", "desc" };
                var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value;
                if (!allowedDirections.Contains(order.Trim().ToLower()))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ActionApiMessages.InvalidDirectionPassed);
                }
                _direction = allowedDirections.Contains(order.Trim().ToLower()) ? order : "desc";
            }

            if (!queryParams.Any(p => p.Key == "_order"))
            {
                orderBySql = $" order by DateStarted {_direction} ";
            }
            else
            {

                var orderByCol = queryParams.FirstOrDefault(p => p.Key == "_order").Value;
                string[] validOrderByFields = { "datestarted", "datecompleted", "action", "assettypename",
                                                "requestedbyname" };
                if (!validOrderByFields.Contains(orderByCol.ToLower()))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ActionApiMessages.InvalidOrder);
                }
                orderBySql = $" order by {orderByCol} {_direction} ";
            }
            if (queryParams.Any(x => x.Key == "_simpleFilter"))
            {
                filterValue = queryParams.FirstOrDefault(p => p.Key == "_simpleFilter").Value.ToString();
                if (filterValue != "")
                {
                    filterValue = '%' + filterValue + '%';
                    whereSql = @"where (X.[Action] like @filterValue or X.DateCompleted like @filterValue or X.[RequestedByName] like @filterValue
                        or X.AssetTypeName like @filterValue or X.ErrorMessage like @filterValue or X.ErrorMessage like @filterValue
                        or x.Success like @filterValue or X.Error like @filterValue or X.Total like @filterValue) ";
                }

            }
            #endregion


            offsetSql = $" offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only ";
            string LoadDetailBaseSql = @"(select	
            case L.[Action]
			when 'M' then 'Users/Groups'
            when 'P' then 'Promotion'
			when 'R' then 'Relation'
			when 'U' then 'Unrelation'
            when 'L' then 'Lineage'
            when 'O' then 'Responsibilities'
            when 'T' then 'Lineage : Technical'
            when 'S' then 'Synonyms'
			when 'W' then 'Promotion (via Propose Workflow)'
		end as [Action],
        case when L.Action in ('P','R','U') and L.[File] is null then
            case when (select count(*) from LoadItem where LoadID = L.ID) = (select count(*) from LoadItem where LoadID = L.ID and Status = 0) then
                L.DateCompleted
            when (L.PutExecutionId is not null and EE.CompletedOn is null) or (L.PostExecutionId is not null and EA.CompletedOn is null) then
                null
            when coalesce(EE.CompletedOn, '1/1/1900') > coalesce(EA.CompletedOn, '1/1/1900') then
                EE.CompletedOn
            else
                EA.CompletedOn      
            end
        else 
            L.DateCompleted 
        end as DateCompleted,
        case 
			when L.[Action] = 'M' and L.ObjectID = 0 then 'Group Membership'
			when L.[Action] = 'M' and L.ObjectID = 1 then 'Users'
            when L.[Action] in ('P','R','U') then coalesce(C_D.[Name], '[Deleted]')  
			else coalesce(C_D.[Name], 'Default') 
		end as AssetTypeName,
        C_D.[uid] as AssetTypeUid,
        L.DateStarted as DateStarted,
        coalesce(EA.ErrorMessage, '' ) + iif(EA.ErrorMessage is null, '', '; ') + coalesce(EE.ErrorMessage, '' ) as ErrorMessage,
        S.C as Success,
        E.C as Error,
		T.C as Total,
        R.FirstName + ' ' + R.LastName as RequestedByName,
        R.uid as RequestedByUid,
        L.uid as LoadUid
from	[Load] L
        left join api.Execution EE on EE.ExecutionId = L.PutExecutionID
        left join api.Execution EA on EA.ExecutionId = L.PostExecutionID
		left join (
			select [Name],[uid], [Object] ,ObjectID from AssetType
			union all
			select ITN.[Name] as [Name], [uid] as uid, 'IntersectType' as [Object], ID as ObjectID from IntersectType IT
			cross apply dbo.GetIntersectTypeNames(IT.ID) ITN

		) C_D on C_D.[Object] = L.[Object] and C_D.ObjectID = L.ObjectID 
		left join reporting.Global_Resource R on R.ResourceID = L.UpdatedBy       
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T ) X ";

            try
            {
                var sql = defaultSelect + LoadDetailBaseSql + whereSql + orderBySql + offsetSql;
                var totalSql = countSql + LoadDetailBaseSql;
                var total = Company.Query<int>(totalSql).FirstOrDefault();
                var results = Company.Query<LoadDetailV2>(sql, new { filterValue });
                model.pageNum = _pageNum;
                model.pageSize = _pageSize;
                model.items = results;
                model.total = total;

                return await Task.FromResult<IHttpActionResult>(
                            ResponseMessage(
                                Request.CreateResponse(
                                    HttpStatusCode.OK, model
                                )
                            )
                        );
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Retrieves bulk load items details for a given load unique identifier.
        /// </summary>
        /// /// <param name="loadUid">The unique identifier of the load which details are returned for.</param>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("bulkload/{loadUid:Guid}/items"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Gets bulk load items details.", typeof(APIExecutionBulkLoadItemDetailsModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by. Options are RowIndex, Status, StatusMessage. Default is RowIndex desc", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered descending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the fields.", DataType = "string", ParameterType = "query", Required = false),
        ]

        public async Task<IHttpActionResult> GetLoadItemDetails(Guid loadUid)
        {
            ResourceApiViewModel model = new ResourceApiViewModel();
            var queryParams = Request.GetQueryNameValuePairs();
            var sql = "";
            var sqlColumns = "";
            var sqlTables = "";
            int _pageSize = 200;
            int _pageNum = 1;
            string _direction = "desc";
            string orderBySql = "";
            string offsetSql = "";
            string whereSql = "";
            string filterValue = "";
            List<string> v2ApiActions = new List<string> { "P", "R", "U" };
            string countSql = "select	count(1) ";

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
            }

            string isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
            }

            var load = Company.Filter<Load>(i => i.uid == loadUid).FirstOrDefault();

            if (load == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.InvalidLoadUid, ApiMessages.InvalidLoadUid)).ConfigureAwait(false);
            }
            var useExecutionTable = false;

            if (v2ApiActions.Contains(load.Action) && (load.PutExecutionID.HasValue || load.PostExecutionID.HasValue))
            {
                useExecutionTable = true;
            }

            var columns = Company.Filter<LoadColumn>(i => i.LoadID == load.ID).OrderBy(i => i.ColumnIndex).ToList();

            #region "Filter Condition"
            if (queryParams.Any(x => x.Key == "_pageSize"))
            {
                int _temppageSize;
                if (!int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_pageSize").Value, out _temppageSize))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, Validation.InvalidPageSize);
                }
                _pageSize = _temppageSize;
            }

            if (queryParams.Any(x => x.Key == "_pageNum"))
            {
                int _temppageNum;
                if (!int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "_pageNum").Value, out _temppageNum))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, Validation.InvalidPageNum);
                }
                _pageNum = _temppageNum;
            }
            if (queryParams.Any(x => x.Key == "_direction"))
            {
                var allowedDirections = new[] { "asc", "desc" };
                var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value;
                if (!allowedDirections.Contains(order.Trim().ToLower()))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ActionApiMessages.InvalidDirectionPassed);
                }
                _direction = allowedDirections.Contains(order.Trim().ToLower()) ? order : "desc";
            }

            if (!queryParams.Any(p => p.Key == "_order"))
            {
                orderBySql = $" order by RowIndex {_direction} ";
            }
            else
            {

                var orderByCol = queryParams.FirstOrDefault(p => p.Key == "_order").Value;
                string[] validOrderByFields = { "rowindex","column1", "column2", "column3", "column4",
                                                "column5","column6", "column7", "column8", "column9",
                                                "column10", "column11", "column12", "column13", "column14",
                                                "column15","column16", "column17", "column18", "column19",
                                                "column20", "status","statusmessage" };
                if (!validOrderByFields.Contains(orderByCol.ToLower()))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ActionApiMessages.InvalidOrder);
                }
                orderBySql = $" order by {orderByCol} {_direction} ";
            }

            if (queryParams.Any(x => x.Key == "_simpleFilter"))
            {
                filterValue = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_simplefilter").Value.ToString();
                if (filterValue != "")
                {
                    string columnSql = "";
                    filterValue = '%' + filterValue + '%';
                    whereSql = " where (RowIndex like @filterValue or [Status] like @filterValue or StatusMessage like @filterValue";
                    if (columns.Count > 0)
                    {
                        for (int x = 1; x <= columns.Count; x++)
                        {
                            columnSql += " or Column" + x + " like @filterValue ";
                        }
                        whereSql += columnSql + ")";
                    }
                    else
                    {
                        whereSql += whereSql + ")";
                    }
                }
            }
            #endregion

            offsetSql = $" offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only ";

            if (useExecutionTable)
            {
                switch (load.Action)
                {
                    case "P":
                        AssetType assetType = Company.Filter<AssetType>(a => a.uid == load.AssetTypeUid).FirstOrDefault();
                        core.SystemObjects type = core.SystemObjects.Load;
                        if (Enum.TryParse(assetType.Object, out core.SystemObjects pObject))
                        {
                            type = pObject;
                        }
                        AssetType parentAssetType = assetType == null ? null : Company.GetParentType(assetType.ID, type);

                        sqlColumns = $"select I.RowIndex as RowIndex\n";
                        sqlTables = @"
                            from (
		                        select ExecutionId, ItemNumber, ExecutionItemUid, ParentAssetID, Message, Success from api.ExecutionAsset where ExecutionId = {0}
		                        union all
		                        select ExecutionID, ItemNumber, ExecutionItemUid, null as ParentAssetID, Message, cast(0 as bit) as Success from api.ExecutionAssetError where ExecutionId = {0}
	                         ) EA
                             left join LoadItem I on I.LoadID = @id and I.ExecutionItemUid = EA.ExecutionItemUid";
                        columns.ForEach(c =>
                        {
                            var i = c.ColumnIndex;
                            if (parentAssetType != null && c.Name == parentAssetType.Name)
                            {
                                sqlColumns += $",EF{i}.DisplayValue + ' [' + cast(EF{i}.[uid] as varchar(50)) + ']' as Column{i}\n";
                                sqlTables += $" left join AssetDetail EF{i} on EF{i}.ID = EA.ParentAssetID\n";
                            }
                            else
                            {
                                sqlColumns += $",coalesce(EF{i}.FieldValue,C{i}.[Value]) as Column{i}\n";
                                sqlTables += $" left join LoadItemColumn C{i} on C{i}.LoadID = I.LoadID and C{i}.RowIndex = I.RowIndex and C{i}.ColumnIndex = {i}\n";
                                sqlTables += $" left join api.ExecutionField EF{i} on EF{i}.ItemNumber = EA.ItemNumber and EF{i}.ExecutionID = EA.ExecutionID and EF{i}.FieldName = '{c.Name}'\n";
                            }

                        });
                        sqlColumns += $", case EA.Success when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status]\n";
                        sqlColumns += ", case when EA.Message is null and EA.Success = 1 then '{0}' else  EA.Message end as StatusMessage\n";

                        sql = $"select * from ({string.Format(sqlColumns, "Item successfully updated.")} {string.Format(sqlTables, "@putExecutionID")} where EA.ExecutionID = @putExecutionID\n";
                        sql += $"union all\n";
                        sql += $"{string.Format(sqlColumns, "Item successfully added.")} {string.Format(sqlTables, "@postExecutionID")} where EA.ExecutionID = @postExecutionID) R " + whereSql + orderBySql + offsetSql;

                        countSql += $" from ({string.Format(sqlColumns, "Item successfully updated.")} {string.Format(sqlTables, "@putExecutionID")} where EA.ExecutionID = @putExecutionID\n";
                        countSql += $"union all\n";
                        countSql += $"{string.Format(sqlColumns, "Item successfully added.")} {string.Format(sqlTables, "@postExecutionID")} where EA.ExecutionID = @postExecutionID) R " + whereSql;

                        break;
                    case "R":
                        sqlColumns = $"select	* from(select I.RowIndex as RowIndex\n";
                        countSql += $" from(select I.RowIndex as RowIndex\n";
                        sqlTables = @"from LoadItem I
                                      left join api.ExecutionRelationship EA on I.ExecutionItemUid = EA.ExecutionItemUid and EA.ExecutionID = @postExecutionID
                                      left join api.ExecutionRelationshipError ER on ER.ExecutionItemUid = I.ExecutionItemUid and ER.ExecutionID = @postExecutionID
                                      left join api.Execution E on E.ExecutionID = @postExecutionID ";
                        columns.ForEach(c =>
                        {
                            var i = c.ColumnIndex;
                            sqlColumns += $",coalesce(EF{i}.FieldValue,C{i}.[Value]) as Column{i}\n";
                            countSql += $",coalesce(EF{i}.FieldValue,C{i}.[Value]) as Column{i}\n";
                            sqlTables += $" left join LoadItemColumn C{i} on C{i}.LoadID = I.LoadID and C{i}.RowIndex = I.RowIndex and C{i}.ColumnIndex = {i}\n";
                            sqlTables += $" left join api.ExecutionField EF{i} on EF{i}.ItemNumber = EA.ItemNumber and EF{i}.ExecutionID = EA.ExecutionID and EF{i}.FieldName = '{c.Name}'\n";

                        });
                        sqlColumns += $", case coalesce(EA.Success,I.Status) when 1 then 'Complete' when 0 then 'Failed' else case when E.CompletedOn is null then 'Queued' else 'Failed' end end as [Status]\n";
                        sqlColumns += ", case when coalesce(EA.Message, ER.Message, I.StatusMessage) is null and EA.Success = 1 then case when EA.IsNew = 1 then 'Item successfully added.' else 'Item successfully updated.' end else coalesce(EA.Message, ER.Message, I.StatusMessage) end as StatusMessage\n";

                        countSql += $", case coalesce(EA.Success,I.Status) when 1 then 'Complete' when 0 then 'Failed' else case when E.CompletedOn is null then 'Queued' else 'Failed' end end as [Status]\n";
                        countSql += ", case when coalesce(EA.Message, ER.Message, I.StatusMessage) is null and EA.Success = 1 then case when EA.IsNew = 1 then 'Item successfully added.' else 'Item successfully updated.' end else coalesce(EA.Message, ER.Message, I.StatusMessage) end as StatusMessage\n";

                        sql = $"{sqlColumns} {sqlTables} where I.LoadID = @id) X " + whereSql + orderBySql + offsetSql;
                        countSql += $" {sqlTables} where I.LoadID = @id) X " + whereSql;

                        break;
                    case "U":
                        countSql += " from(select I.RowIndex as RowIndex\n";
                        sqlColumns = $"select	* from(select I.RowIndex as RowIndex\n";
                        sqlTables = @"from LoadItem I
                                        left join api.ExecutionDeletedRelationship EA on I.ExecutionItemUid = EA.ExecutionItemUid and EA.ExecutionID = @postExecutionID";
                        columns.ForEach(c =>
                        {
                            var i = c.ColumnIndex;
                            sqlColumns += $",C{i}.[Value] as Column{i}\n";
                            countSql += $",C{i}.[Value] as Column{i}\n";
                            sqlTables += $" left join LoadItemColumn C{i} on C{i}.LoadID = I.LoadID and C{i}.RowIndex = I.RowIndex and C{i}.ColumnIndex = {i}\n";

                        });
                        sqlColumns += $", case coalesce(EA.Success,I.Status) when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status]\n";
                        sqlColumns += ", case when coalesce(EA.Message, I.StatusMessage) is null and EA.Success = 1 then 'Relationship successfully removed.' else  coalesce(EA.Message, I.StatusMessage) end as StatusMessage\n";

                        countSql += $", case coalesce(EA.Success,I.Status) when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status]\n";
                        countSql += ", case when coalesce(EA.Message, I.StatusMessage) is null and EA.Success = 1 then 'Relationship successfully removed.' else  coalesce(EA.Message, I.StatusMessage) end as StatusMessage\n";

                        sql = $"{sqlColumns} {sqlTables} where I.LoadID = @id) X " + whereSql + orderBySql + offsetSql;
                        countSql += $" {sqlTables} where I.LoadID = @id) X " + whereSql;
                        break;
                }


                var results = Company.Query<dynamic>(sql, new { id = load.ID, putExecutionID = load.PutExecutionID, postExecutionID = load.PostExecutionID, filterValue });
                var total = Company.Query<int>(countSql, new { id = load.ID, putExecutionID = load.PutExecutionID, postExecutionID = load.PostExecutionID, filterValue }).FirstOrDefault();
                model.pageNum = _pageNum;
                model.pageSize = _pageSize;
                model.items = results;
                model.total = total;
                return await Task.FromResult<IHttpActionResult>(
                        ResponseMessage(
                            Request.CreateResponse(
                                HttpStatusCode.OK, model
                            )
                        )
                    );
            }
            else
            {
                countSql += "from(select I.RowIndex as RowIndex";
                sqlColumns = "select	* from(select I.RowIndex as RowIndex";
                sqlTables = "from LoadItem I";
                columns.ForEach(c =>
                {
                    countSql += string.Format(", C{0}.Value as Column{0}", c.ColumnIndex);
                    sqlColumns += string.Format(", C{0}.Value as Column{0}", c.ColumnIndex);
                    sqlTables += string.Format(" left join LoadItemColumn C{0} on C{0}.LoadID = I.LoadID and C{0}.RowIndex = I.RowIndex and C{0}.ColumnIndex = {0}", c.ColumnIndex);
                });
                sqlColumns += ", case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage as StatusMessage";
                countSql += ", case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage as StatusMessage";

                sql += sqlColumns + " " + sqlTables + " where I.LoadID = @id) X " + whereSql + orderBySql + offsetSql;
                var count = countSql + " " + sqlTables + " where I.LoadID = @id) X " + whereSql;

                var results = Company.Query<dynamic>(sql, new { id = load.ID, filterValue });
                var total = Company.Query<int>(count, new { id = load.ID, filterValue }).FirstOrDefault();
                model.pageNum = _pageNum;
                model.pageSize = _pageSize;
                model.items = results;
                model.total = total;
                return await Task.FromResult<IHttpActionResult>(
                        ResponseMessage(
                            Request.CreateResponse(
                                HttpStatusCode.OK, model
                            )
                        )
                    );
            }
        }

        /// <summary>
        /// Retrieves bulk load info for a given load unique identifier.
        /// </summary>
        /// <param name="loadUid">The unique identifier of the load which details are returned for.</param>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("bulkload/{loadUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Gets bulk load info.", typeof(SingleLoadDetail)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]

        public async Task<IHttpActionResult> GetLoads(Guid loadUid)
        {
            List<string> v2ApiActions = new List<string> { "P", "R", "U" };
            string countSql = "";

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
            }

            var load = Company.Filter<Load>(i => i.uid == loadUid).FirstOrDefault();
            var useExecutionTable = false;

            if (v2ApiActions.Contains(load.Action) && (load.PutExecutionID.HasValue || load.PostExecutionID.HasValue))
            {
                useExecutionTable = true;
            }

            if (useExecutionTable)
            {
                switch (load.Action)
                {
                    case "P":
                        countSql = @"
		                    cross apply (
				                    select count(*) as C from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success = 1
			                    ) S
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success = 0
				                    union all
				                    select count(*) as I from api.ExecutionAssetError where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				                    ) R
			                    ) E
		                    cross apply (
				                    select count(*) as C from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID) and Success is null
			                    ) I
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionAsset where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				                    union all
				                    select count(*) as I from api.ExecutionAssetError where ExecutionID in (L.PostExecutionID, L.PutExecutionID)
				                    ) R
			                    ) T";
                        break;
                    case "R":
                        countSql = @"		
                            cross apply (
				                    select count(*) as C from api.ExecutionRelationship where ExecutionID = L.PostExecutionID and Success = 1
			                    ) S
		                    cross apply (
			                    select sum(I) as C from (
				                    select Error as I from api.Execution where ExecutionID = L.PostExecutionID
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) E
		                    cross apply (
				                select case when CompletedOn is null then (Total - Processed) else 0 end as C from api.Execution where ExecutionID = L.PostExecutionID
			                    ) I
		                    cross apply (
			                    select sum(I) as C from (
				                    select Total as I from api.Execution where ExecutionID = L.PostExecutionID
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) T";
                        break;
                    case "U":
                        countSql = @"
                            cross apply (
				                    select count(*) as C from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID and Success = 1
			                    ) S
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID and Success = 0
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) E
		                    cross apply (
				                    select count(*) as C from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID and Success is null
			                    ) I
		                    cross apply (
			                    select sum(I) as C from (
				                    select count(*) as I from api.ExecutionDeletedRelationship where ExecutionID = L.PostExecutionID
                                    union all
                                    select count(*) as I from LoadItem where LoadID = L.ID and Status = 0
				                    ) R
			                    ) T";
                        break;
                }
            }
            else
            {
                countSql = @"
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
                    cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T";
            }

            string LoadDetailBaseSql = @"select	* from(select	
            case L.[Action]
			when 'M' then 'Users/Groups'
            when 'P' then 'Promotion'
			when 'R' then 'Relation'
			when 'U' then 'Unrelation'
            when 'L' then 'Lineage'
            when 'O' then 'Responsibilities'
            when 'T' then 'Lineage : Technical'
            when 'S' then 'Synonyms'
			when 'W' then 'Promotion (via Propose Workflow)'
		end as [Action],
        case 
			when L.[Action] = 'M' and L.ObjectID = 0 then 'Group Membership'
			when L.[Action] = 'M' and L.ObjectID = 1 then 'Users'
            when L.[Action] in ('P','R','U') then coalesce(C_D.[Name], '[Deleted]')  
			else coalesce(C_D.[Name], 'Default') 
		end as AssetTypeName,
        C_D.[uid] as AssetTypeUid,
        S.C as Success,
        E.C as Error,
        I.C as Incomplete,
		T.C as Total,
        case coalesce(M.Success,LI.[Status],ER.Success,EDR.Success,EAA.Success) when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status],
        R.FirstName + ' ' + R.LastName as RequestedByName,
        R.uid as RequestedByUid
from	[Load] L
        left join api.Execution EE on EE.ExecutionId = L.PutExecutionID
        left join api.Execution EA on EA.ExecutionId = L.PostExecutionID
		left join (
			select [Name],[uid], [Object] ,ObjectID from AssetType
			union all
			select ITN.[Name] as [Name], [uid] as uid, 'IntersectType' as [Object], ID as ObjectID from IntersectType IT
			cross apply dbo.GetIntersectTypeNames(IT.ID) ITN
		) C_D on C_D.[Object] = L.[Object] and C_D.ObjectID = L.ObjectID 
		left join reporting.Global_Resource R on R.ResourceID = L.UpdatedBy 
        left join (select top(1) Success, ExecutionID from api.ExecutionAsset) M on M.ExecutionID in (L.PostExecutionID, L.PutExecutionID) 
        left join(select top(1) Success,ExecutionID from api.ExecutionAsset) EAA on  EAA.ExecutionID = L.PostExecutionID
        cross apply (select top 1 Status,ExecutionItemUid from LoadItem where LoadID = L.ID) LI 
        left join api.ExecutionRelationship ER on LI.[ExecutionItemUid] = ER.ExecutionItemUid and ER.ExecutionID = L.PostExecutionID
        left join api.ExecutionDeletedRelationship EDR on LI.[ExecutionItemUid] = EDR.ExecutionItemUid and EDR.ExecutionID = L.PostExecutionID
         "
        + countSql + " where L.uid = @loadUid ) X ";

            try
            {
                var results = Company.Query<SingleLoadDetail>(LoadDetailBaseSql, new { loadUid }).ToList();

                if (load != null && load.DateCompleted.HasValue && load.DateStarted.HasValue)
                {
                    var minutes = Math.Round((load.DateCompleted.Value - load.DateStarted.Value).TotalMinutes);

                    var minutesMessage = (minutes == 0 ? "less than a minute" : minutes + " minute(s)");

                    if (results.Count > 0)
                    {
                        results[0].ElapsedTime = minutesMessage;
                    }
                }

                return await Task.FromResult<IHttpActionResult>(
                            ResponseMessage(
                                Request.CreateResponse(
                                    HttpStatusCode.OK, results
                                )
                            )
                        );
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Creates a new Bulk load.
        /// </summary>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="intersectTypeUid">The unique identifier of the intersect type.</param>
        /// <param name="type">The bulkload type of the load you are creating. Default is Promotion.</param>
        /// <param name="notes">Add notes to the load.</param>
        /// <returns></returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("bulkload"),
            SwaggerConsumes("application/octet-stream"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Creates a new Bulk load.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("file", "File to be uploaded", DataType = "file", ParameterType = "formData", Required = true),

        ]

        public async Task<IHttpActionResult> AddLoad(Guid? assetTypeUid = null, Guid? intersectTypeUid = null, BulkLoadType type = BulkLoadType.Promotion, string notes = "")
        {
            try
            {
                var assetType = Company.AssetTypes.Where(r => r.uid == assetTypeUid).FirstOrDefault();
                var intersectType = Company.IntersectTypes.Where(i => i.uid == intersectTypeUid).FirstOrDefault();

                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                if (type == BulkLoadType.Promotion)
                {
                    if (assetTypeUid == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidAssetTypeUidProvided)).ConfigureAwait(false);
                    }
                }

                if (type == BulkLoadType.Relation || type == BulkLoadType.Unrelation)
                {
                    if (intersectTypeUid == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidIntersectTypeUidProvided)).ConfigureAwait(false);
                    }
                }
		
                var response = new ConfirmResponse();
                var c = await Request.Content.ReadAsMultipartAsync();
                var file = c.Contents.Where(x => x.Headers?.ContentDisposition?.Parameters.Any(param => param?.Value.Contains("file") == true) == true).FirstOrDefault();

                if (file == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.RequiredFieldError, "file"))).ConfigureAwait(false);
                }

                byte[] bytes = await file.ReadAsByteArrayAsync();
                string extension = Path.GetExtension(file.Headers.ContentDisposition.FileName.ToString().Replace("\"", ""));

                Load load = null;
                var errorMessages = new List<string>();
                SLDocument xls;
                string objectType = "";
                int objectID = 0;
                using (var stream = new MemoryStream(bytes))
                {
                    if (extension == ".xlsx")
                    {
                        if (type == BulkLoadType.Users)
                        {
                            objectType = "Membership";
                            objectID = 1;
                        }
                        else if (type == BulkLoadType.Groups)
                        {
                            objectType = "Membership";
                            objectID = 0;
                        }
                        else if (type == BulkLoadType.Responsibilities)
                        {
                            objectType = "ArtifactType";
                            objectID = 0;
                        }
                        else if (intersectType != null)
                        {
                            objectType = "IntersectType";
                            objectID = intersectType.ID;
                        }
                        else
                        {
                            objectType = assetType.Object;
                            objectID = assetType.ObjectID;
                        }

                        load = new Load
                        {
                            File = stream.ToArray(),
                            Action = type.GetDisplayName(),
                            Extension = extension,
                            Notes = notes,
                            Object = objectType,
                            ObjectID = objectID,
                            DateStarted = DateTime.UtcNow,
                            UpdatedBy = Company.CurrentResourceID,
                            AssetTypeUid = assetTypeUid,
                            IntersectTypeUid = intersectTypeUid
                        };

                        xls = new SLDocument(stream);

                        var fieldTypeNames = Company.GetLoadColumns(load.Action, load.Object, load.ObjectID, false);

                        var stats = xls.GetWorksheetStatistics();
                        int columnCount = 0;

                        for (int i = 1; i <= stats.NumberOfColumns; i++)
                        {
                            var testValue = xls.GetCellValueAsString(1, i);
                            if (string.IsNullOrEmpty(testValue))
                            {
                                errorMessages.Add($"Invalid column header in column {i}. ");
                            }
                            else
                            {
                                columnCount++;
                            }
                        }

                        if (errorMessages.Count == 0)
                        {
                            // Spreadsheet should not have more columns than the type has, but it can have less.
                            // Spreadsheet should only contain columns that the type has.
                            if (columnCount <= fieldTypeNames.Count)
                            {
                                load.LoadColumns = new List<LoadColumn>();

                                #region Loop through spreadsheet columns and make sure type has that column defined.
                                for (var i = stats.StartColumnIndex; i <= stats.EndColumnIndex; i++)
                                {
                                    var columnName = (xls.GetCellValueAsString(1, i) ?? string.Empty).Trim();

                                    if (string.IsNullOrEmpty(columnName))
                                    {
                                        continue;
                                    }

                                    if (!fieldTypeNames.Any(x => x.Name == columnName))
                                    {
                                        errorMessages.Add($"Unexpected column found [{columnName}]. ");
                                    }
                                    else
                                    {

                                        if (load.Action == "P" && load.LoadColumns.Any(l => l.Name == columnName))
                                        {
                                            errorMessages.Add($"Duplicate column found [{columnName}]. ");
                                        }
                                        else
                                        {
                                            load.LoadColumns.Add(new LoadColumn { ColumnIndex = i, Name = columnName });
                                        }
                                    }
                                }
                                #endregion

                                Func<int, bool> allColumnRowsHaveValue = delegate (int columnIndex)
                                {
                                    bool returnValue = true;

                                    if (columnIndex >= 0)
                                    {
                                        returnValue = stats.EndRowIndex > stats.StartRowIndex; // If there is only header row, this fails.

                                        for (var i = stats.StartRowIndex + 1; i <= stats.EndRowIndex; i++)
                                        {
                                            if (returnValue) // Continue checking ONLY if we are still set to TRUE.
                                            {
                                                var rowValue = (xls.GetCellValueAsString(i, columnIndex) ?? string.Empty).Trim();

                                                if (string.IsNullOrEmpty(rowValue))
                                                {
                                                    returnValue = false;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        returnValue = false;
                                    }

                                    return returnValue;
                                };

                                // This is where we do our key check, split by ordered Level.
                                var levelFields = (
                                                  from fl in fieldTypeNames
                                                  select new LevelField
                                                  {
                                                      Level = fl.Level,
                                                      Name = fl.Name,
                                                      PartOfKey = fl.PartOfKey,
                                                      Required = fl.Required,
                                                      ColumnIndex = load.LoadColumns.Any(lc => lc.Name == fl.Name) ? load.LoadColumns.First(lc => lc.Name == fl.Name).ColumnIndex : -1
                                                  }
                                                  ).OrderBy(i => i.Level).ToList();

                                // Determine which key/required columns are fully loaded.
                                foreach (var lf in levelFields.Where(f => f.PartOfKey || f.Required))
                                {
                                    lf.DataLoaded = allColumnRowsHaveValue(lf.ColumnIndex);
                                }

                                var requiredLevels = levelFields
                                    .Select(i => new LoadLevelStatus
                                    {
                                        Level = i.Level,
                                        Required = false
                                    })
                                    .Distinct(new LoadLevelStatusComparer())
                                    .OrderByDescending(l => l.Level)
                                    .ToList();

                                // Determine which levels are required.
                                requiredLevels.ForEach(l =>
                                {
                                    l.DataLoaded = !levelFields.Any(f => f.Level == l.Level && (f.PartOfKey || f.Required) && !f.DataLoaded);

                                    if (l.Level == 1)
                                    {
                                        // Level 1 is always required.
                                        l.Required = true;
                                    }
                                    else
                                    {
                                        if (requiredLevels.Any(p => p.Level == l.Level + 1 && p.DataLoaded))
                                        {
                                            l.Required = true; // Since level below CURRENT is data-populated, then CURRENT is required.
                                        }
                                        else
                                        {
                                            l.Required = l.DataLoaded;
                                        }
                                    }
                                });

                                List<string> invalidKeyFields = new List<string>();
                                List<string> invalidRequiredFields = new List<string>();

                                // Log missing required column messages.
                                requiredLevels.ForEach(l =>
                                {
                                    if (l.Required)
                                    {
                                        // Log any missing key field errors.
                                        errorMessages.AddRange(
                                            levelFields
                                            .Where(f => f.Level == l.Level && f.PartOfKey && f.Required && f.ColumnIndex == -1)
                                            .Select(f => $"Key column not provided [{f.Name}]. ")
                                        );

                                        // Log any missing required, non-key field errors.
                                        errorMessages.AddRange(
                                            levelFields
                                            .Where(f => f.Level == l.Level && f.Required && !f.PartOfKey && f.ColumnIndex == -1)
                                            .Select(f => $"Required column not provided [{f.Name}]. ")
                                        );

                                        // Get any key columns that do not have data populated for this level.
                                        invalidKeyFields.AddRange(
                                            levelFields.Where(lf => lf.Level == l.Level && lf.PartOfKey && lf.Required && lf.ColumnIndex > -1 && !lf.DataLoaded).Select(lf => lf.Name)
                                        );

                                        // Get any required, non-key columns that do not have data populated for this level.
                                        invalidRequiredFields.AddRange(
                                            levelFields.Where(lf => lf.Level == l.Level && lf.Required && !lf.PartOfKey && lf.ColumnIndex > -1 && !lf.DataLoaded).Select(lf => lf.Name)
                                        );
                                    }
                                });

                                if (invalidKeyFields.Count > 0)
                                {
                                    errorMessages.Add($"One or more values not populated for Key column{(invalidKeyFields.Count > 1 ? "s" : "")} [{string.Join(", ", invalidKeyFields)}]. ");
                                }
                                if (invalidRequiredFields.Count > 0)
                                {
                                    errorMessages.Add($"One or more values not populated for Required column{(invalidRequiredFields.Count > 1 ? "s" : "")} [{string.Join(", ", invalidRequiredFields)}]. ");
                                }
                            }
                            else
                            {
                                errorMessages.Add("The number of columns in the spreadsheet exceeds the number of defined fields for this load type. ");
                            }
                        }
                    }
                    else
                    {
                        errorMessages.Add("Incorrect file type. ");
                    }
                }

                if (errorMessages.Count == 0)
                {
                    load.File = null;
                    Company.Add<Load>(load);
                    await Storage.CreateFolder($"{constants.COMPANY_BULK_LOAD_FOLDER}");
                    await Storage.CreateFile($"{constants.COMPANY_BULK_LOAD_FOLDER}", $"{Company.CurrentCompanyID}/load_{load.ID}.{load.Extension}", new MemoryStream(bytes));
                    Company.Enqueue(Config.GetValue<string>("BulkLoadQueue"), new BulkLoadInfo { CompanyID = Company.CurrentCompanyID, LoadID = load.ID, To = QueueAction.BulkLoad });
                    response.message = FormControllerApiMessage.FileUploadedAndQueueProcessing;
                    return await Task.FromResult<IHttpActionResult>(
                            ResponseMessage(
                                Request.CreateResponse(
                                    HttpStatusCode.OK, response
                                )
                            )
                        );
                }
                else
                {
                    StringBuilder error = new StringBuilder();
                    foreach (var i in errorMessages)
                    {
                        error.Append(i);
                    }
                    string err = error.ToString();
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, err)).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }
    }
}
