using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

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

        ICompanyContext company;
        public HealthController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository) : base(community, company, settingsRepository)
        {
            this.Community = community;
            this.company = company;
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
                if (this.company.Connection.State != System.Data.ConnectionState.Open)
                {
                    this.company.Connection.Open();
                    this.company.Connection.Close();
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
    }
}
