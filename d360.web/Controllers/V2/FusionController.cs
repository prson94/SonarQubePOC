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
using Resources;

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
        private IFusionRepository FusionRepository;
        public FusionController(ICommunityContext community, ICompanyContext company, IFusionRepository fusionRepository) : base(community, company)
        {
            this.FusionRepository = fusionRepository;
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
        /// <param name="cascade">Cascade delete to all related data (true/false). Setting cascade to false will not cascade the delete to other items and attempt to just delete the specified fusion configuration.  If the specified fusion configuration has data and you try to delete with cascade set to false, you will get an error.  The default value for cascade is FALSE.  If cascade is set to true, the fusion configuration and all related data will be removed.  This includes all fusion attribute data in this fusion configuration, all field data for those field attributes, and all relations that include fusion attributes in the configuration to be deleted.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("batch/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkFusionAsync(string assetUid, bool cascade = false)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, "You are not allowed to remove assets of this type."));

            var prefix = "Assets.DeleteBulkAssetsAsync => ";
            var errorMessage = "";

            Guid fusionGuid = Guid.Parse(assetUid);

            try
            {
                Asset fusion = FusionRepository.GetFusionByUID(fusionGuid);

                if (fusion == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Fusion configuration with Uid {fusionGuid} could not be found."));


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
