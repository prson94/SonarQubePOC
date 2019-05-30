using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
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

        public IssuesController(ICommunityContext community, ICompanyContext company, IIssueRepository repository)
            : base(community, company)
        {
            this.issueRepository = repository;
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
        public async Task<HttpResponseMessage> Get()
        {
            var issueTypes = await issueRepository.GetIssueTypes();

            return Request.CreateResponse(issueTypes);
        }
    }
}