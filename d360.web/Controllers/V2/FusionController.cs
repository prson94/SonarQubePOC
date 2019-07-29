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
using d360.web.Models;
using d360.model.DataAccessLayer;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling fusion specific activities.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/fusion"),
        Authorize
    ]
    public class FusionController : BaseV2ApiController
    {
        static string APIMACHINENAME = "API-GENERATED";
        private IFusionRepository FusionRepository;
        public FusionController(ICommunityContext community, ICompanyContext company, IFusionRepository fusionRepository) : base(community, company)
        {
            this.FusionRepository = fusionRepository;
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



        /// <summary>
        /// Removes a fusion configuration based on the specific fusion Uid. This endpoint is meant for deleting one fusion configuration at a time.
        /// </summary>
        /// <remarks>
        /// <strong>&#9888; Read before calling this endpoint</strong><br/>
        /// Calling this endpoint with parameter Cascade set to true irrevocably deletes a Fusion configuration and all related data, which includes attributes, fields and relationships
        /// <br/>Fusion rules must be deleted manually from the UI before calling this endpoint.
        /// </remarks>
        /// <param name="assetUid">The unique identifier of the fusion configuration.</param>
        /// <param name="cascade">Cascade delete to all related data (true/false). Setting cascade to false will not cascade the delete to other items and attempt to just delete the specified fusion configuration.  If the specified fusion configuration has data and you try to delete with cascade set to false you will get an error.  The default value for cascade is FALSE.  If cascade is set to true, the fusion configuration and all related data will be removed.  This includes all fusion attribute data in this fusion configuration all field data for those field attributes, all relations that include fusion attributes in the configuration to be deleted.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("batch/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkFusionAsync(string assetUid, bool cascade = false)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to remove assets of this type."));

            var prefix = "Assets.DeleteBulkAssetsAsync => ";
            var errorMessage = "";

            Guid fusionGuid = Guid.Parse(assetUid);

            try
            {
                Asset fusion = FusionRepository.GetFusionByUID(fusionGuid);

                if (fusion == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Fusion configuration with Uid {fusionGuid} could not be found."));

                if (FusionRepository.HasFusionRules(fusion.ObjectID))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Not found", $"Fusion configuration have rules. Delete them manually before calling this endpoint!"));
                }

                var execution = getApiExecution(1, new ApiExecutionFields_DeleteAssets { AssetTypeUid = fusion.AssetType.uid});

                var executionInfo = await FusionRepository.BulkDeleteFusionConfiguration(fusionGuid, cascade, execution);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetUid.ToString() },
                    { "AssetCount", "1" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }
    }
}
