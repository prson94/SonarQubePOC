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

namespace d360.web.Controllers.V2
{
    [ApiExplorerSettings(IgnoreApi = true)]
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
        }

        #endregion

        #region Executions
        /// <summary>
        /// GETs all execution records, including the results for the execution.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of all execution statuses.", typeof(APIExecutionAPIModelResult)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by Giud.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),

        ]
        public async Task<IHttpActionResult> GetExecutions()
        {

            var queryParams = Request.GetQueryNameValuePairs();
            var executions = await AssetRepository.GetExecutionItems(queryParams);
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
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <param name="executionUid">The execution's unique identifier to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{executionUid:Guid}/status"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of assets.", typeof(ApiExecutionStatusModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your status was not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionUid)
        {
            var prefix = "Assets.GetExecutionStatus => ";
            var errorMessage = "";

            try
            {
                ApiExecution dbExecutionItem = AssetRepository.GetExecutionItemByUid(executionUid);

                if (dbExecutionItem == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution unique identifier not found."));
                }

                var info = new ApiExecutionInfo { CompanyID = Company.CurrentCompanyID, ExecutionID = executionUid };

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    var resultsJson = Storage.GetFileContentsAsString(info.StorageFolder, info.ResponseFileName);
                    results = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(resultsJson);
                }
                catch
                {
                }
                var f = string.IsNullOrEmpty(dbExecutionItem.Fields) ? "{}" : dbExecutionItem.Fields;
                var statusModel = new ApiExecutionStatusModel
                {
                    CompletedOn = dbExecutionItem.CompletedOn,
                    Error = dbExecutionItem.Error,
                    Fields = Newtonsoft.Json.Linq.JObject.Parse(f),
                    Processed = dbExecutionItem.Processed,
                    StartedOn = dbExecutionItem.StartedOn,
                    Total = dbExecutionItem.Total,
                    Results = results
                };

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            statusModel
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "ExecutionUid", executionUid.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #endregion
    }
}