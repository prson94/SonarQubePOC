using d360.model.DataAccessLayer;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.web.Filters;
using Newtonsoft.Json;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service to verify the Govern environment is healthy.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/health")
        
    ]
    public class HealthController : BaseV2ApiController
    {
        private IApplicationHealthDapperRepository ApplicationHealthDapperRepository { get; }

        public HealthController(CoreComponentSet set, IApplicationHealthDapperRepository applicationHealthDapperRepository): base(set)
        {
            ApplicationHealthDapperRepository = applicationHealthDapperRepository;
        }

        /// <summary>
        /// Get the health of the system
        /// </summary>
        /// <returns>An HTTP status code</returns>
        [HttpGet,
            Route(""),
             SwaggerResponse(HttpStatusCode.OK, "API call was successful and connect to the database"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "API call was not successful and cannot connect to the database"),
           
            ]
        public async Task<IHttpActionResult> GetHealth()
        {
            var prefix = "Health.GetHealth => ";
            var errorMessage = "";
            try
            {
                if (Company.Connection.State != System.Data.ConnectionState.Open)
                {
                    Company.Connection.Open();
                    Company.Connection.Close();
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.InternalServerError))).ConfigureAwait(false);
            }
        }

        [HttpGet]
        [Route("details")]
        [SwaggerProduces("application/json")]
        [SwaggerResponse(HttpStatusCode.OK, "Application health details", typeof(HealthDetailsResponse))]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "API call was not successful.", typeof(ErrorResponse))]
        [SwaggerResponse(HttpStatusCode.Forbidden, "Forbidden user is not an administrator.", typeof(ErrorResponse))]
        [SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse))]
        [Authorize]
        [RequireAdminPermissions]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> GetDetailsAsync()
        {
            ValidateParameters();

            var entity = await ApplicationHealthDapperRepository.GetDetailsAsync();

            var result = new HealthDetailsResponse
            {
                ApiExecutionPendingCount = entity.ApiExecutionPendingCount,
                QueueTaskCount = entity.QueueTaskCount,
                WorkflowItemPendingCount = entity.WorkflowItemPendingCount
            };

            return Ok(result);
        }

        #region Request / Response models

        public sealed class HealthDetailsResponse
        {
            [JsonProperty("queueTaskCount")]
            public int QueueTaskCount { get; set; }

            [JsonProperty("pendingApiCalls")]
            public int ApiExecutionPendingCount { get; set; }

            [JsonProperty("pendingWorkflowInstances")]
            public int WorkflowItemPendingCount { get; set; }
        }

        #endregion
    }
}
