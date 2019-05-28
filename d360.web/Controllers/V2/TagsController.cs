using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
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
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/tags"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class TagsController : BaseV2ApiController
    {        
        ITagRepository tagRepository;

        public TagsController(ICommunityContext community, ICompanyContext company, ITagRepository repository)
            : base(community, company)
        {            
            this.tagRepository = repository;
        }

        /// <summary>
        /// Returns all tags that are defined in Govern.  
        /// 
        /// </summary>
        /// <param name="Uid">The uid of a specific tag.</param>        
        /// <returns>A list of tags</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("uid", "The uid of a specific tag to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(List<TagApiModelWrapper>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied")            
        ]
        public async Task<HttpResponseMessage> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            var queryParams = Request.GetQueryNameValuePairs();

            var assetCrossReferences = await tagRepository.GetTags(queryParams);

            return Request.CreateResponse(assetCrossReferences);
        }
    }
}
