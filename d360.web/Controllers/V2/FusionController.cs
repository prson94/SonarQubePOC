using d360.core.entities;
using d360.model;
using d360.web.Filters;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Dapper;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/fusion"),
        Authorize
    ]
    public class FusionController : BaseV2ApiController
    {
        static string APIMACHINENAME = "API-GENERATED";
        public FusionController(ICommunityContext community, ICompanyContext company) : base(community, company)
        {

        }

        /// <summary>
        /// Creates a new fusion agent status log entry for the specified fusion.  Used by Analyze to simulate Fusion Agent activity.  Only Admininistrators can call this endpoint.
        /// </summary>
        /// <param name="model">The fusion status log model.</param>
        /// <returns>The model of the created fusion status log entry.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("Agent/Status"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied"),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Fusion ID specified is not valid."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error has occured inserting the record.")
        ]
        public async Task<FusionStatusLog> Post(FusionStatusLog model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            
            //validate the model input fusion id needs to be a valid fusion id
            if (!Company.FusionTypeConfigurations.Any(x=>x.ID==model.FusionID))            
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain a valid fusion id."));

            model.ID = Guid.NewGuid();
            model.DateStarted = DateTime.UtcNow;
            model.Success = false;
            model.MachineQueuedOn = APIMACHINENAME; //incase the fusion client is running so this job doesnt get allocated.

            var res = await Company.Database.Connection.ExecuteAsync("insert into fusionstatuslog (id,FusionID,DateStarted,Success,MachineQueuedOn) values(@id,@fusion,@start,@s,@machine)", new { id = model.ID, fusion= model.FusionID, start= model.DateStarted,s=model.Success, machine = model.MachineQueuedOn });

            if (res <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error creating fusionstatuslog record."));
            }

            return model;
        }

        /// <summary>
        /// Updates an existing fusion agent status log entry.  Used by Analyze to simulate Fusion Agent activity.  Only Admininistrators can call this endpoint.  Please provide the status of your call here (success true, failure false) along with any messages you would like for the run.  The ID returned when you created this record should be provided back to this call.
        /// Date completed will be updated to UTC now and the Date started will be ingored and the original value used.  Only records that have not already been marked as completed can be updated.
        /// </summary>
        /// <param name="model">The fusion status log model.</param>
        /// <returns>The model of the updated fusion status log entry.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("Agent/Status"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied"),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Fusion ID specified is not a valid fusion id."),
            SwaggerResponse(HttpStatusCode.NotFound, "Fusion Status Log record cannot be found."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error has occured inserting the record.")
        ]
        public async Task<FusionStatusLog> Put(FusionStatusLog model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //validate the model input fusion id needs to be a valid fusion id
            if (!Company.FusionTypeConfigurations.Any(x => x.ID == model.FusionID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain a valid fusion id."));

            if(!Company.FusionStatusLogs.Any(x => x.ID == model.ID && x.FusionID == model.FusionID))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Model does not contain a valid fusion status log with the matching id and fusion id."));
                        
            model.DateCompleted = DateTime.UtcNow;            
            
            var res = await Company.Database.Connection.ExecuteAsync("update fusionstatuslog set DateCompleted = @end, Success = @s, Message = @msg where ID = @id and datecompleted is null", new { id = model.ID, end = model.DateCompleted, s = model.Success, msg = model.Message });

            if (res <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error updating fusionstatuslog record."));
            }

            // if its a failure add the message to the agent error tables so it shows in the ui

            if (!model.Success)
            {
                await Company.Database.Connection.ExecuteAsync($@"
                        begin
	                        insert into fusion.agenterror values(@id,'{APIMACHINENAME}',GETUTCDATE())

	                        declare @errorID int
	                        select @errorID = (SELECT SCOPE_IDENTITY());

	                        insert into fusion.agenterroritem values(@errorID,getutcdate(),@msg)
                        end
                    ", new { id = model.FusionID, msg = model.Message });
            }

            return model;
        }
    }
}
