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
            Route(""),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of all execution statuses.", typeof(APIExecutionAPIModelResult)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by CompletedOn.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),

        ]
        public async Task<IHttpActionResult> GetExecutions()
        {

            var queryParams = Request.GetQueryNameValuePairs();

            var executions = await AssetRepository.GetExecutionItems(queryParams);
            if(executions.StatusCode != HttpStatusCode.OK)
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