using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling issue management in Govern.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/issues"),
        Authorize
    ]
    public class IssuesController : BaseV2ApiController
    {
        IIssueRepository issueRepository;
        IAssetRepository assetRepository;

        public IssuesController(ICommunityContext community, ICompanyContext company, IIssueRepository repository, IAssetRepository assetRepository)
            : base(community, company)
        {
            this.issueRepository = repository;
            this.assetRepository = assetRepository;
        }

        /// <summary>
        /// Returns all issue types that are defined in Govern.  
        /// 
        /// </summary>
        /// <returns>A list of issue types</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A full list of issue types.", typeof(List<IssueTypeApiModel>)),
        ]
        public async Task<HttpResponseMessage> GetIssueTypes()
        {
            var issueTypes = await issueRepository.GetIssueTypes();

            return Request.CreateResponse(issueTypes);
        }

        [HttpGet,
            Route("types/{AssetTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(IssueTypeApiModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type with Uid {uid} not found."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
            ]
        public async Task<IHttpActionResult> GetAllocationByAssetTypeAsync(Guid AssetTypeUid)
        {
            var prefix = "Issues.GetAllocationByAssetTypeAsync => ";
            var errorMessage = "";

            try
            {
                AssetType assetType = this.assetRepository.GetAssetTypeByUID(AssetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {AssetTypeUid} could not be found."));

                var allocations=  await this.issueRepository.GetAllocationByAssetType(AssetTypeUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations)));
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