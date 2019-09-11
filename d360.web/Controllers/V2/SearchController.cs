using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling search in Govern.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/search"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class SearchController : BaseV2ApiController
    {
        ISearchSource SearchSource;

        public SearchController(ICommunityContext community, ICompanyContext company, ISearchSource searchSource) : base(community, company)
        {
            SearchSource = searchSource;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phrase">Search term</param>
        /// <returns></returns>
        [
            HttpGet,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of matching search items.", typeof(IQueryable<IndexResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.")
        ]
        public IQueryable<IndexResult> GetSearchResults(string phrase)
        {
            if (!string.IsNullOrEmpty(phrase))
            {
                var c = Community.GetById<Company>(Company.CurrentCompanyID, i => i.CompanyDomainSettings);
                var result = SearchSource.GetSearchResults(Company.CurrentCompanyID, Company.CurrentResourceID, phrase, 200, 0);
                result.Results.ForEach(i => {
                    i.AbsoluteUrl = string.Format("https://{0}.data3sixty.com/{1}", c.CompanyDomainSettings.First(d => d.IsPrimary).UrlPrefix, i.Url);
                });
                return result.Results.AsQueryable();
            }
            return null;            
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("rebuildIndex")]
        public HttpResponseMessage RebuildIndex()
        {
            if (!Company.CurrentResourceIsAdmin) return ReturnApiError(HttpStatusCode.Unauthorized, "User not authorized to perfom this action");

            Company.RebuildIndexRequest();

            return Request.CreateResponse(HttpStatusCode.Created, new { type = "confirm", title = "Success!", action = "add", message = "Rebuild request received and accepted.", id = "" });
        }
    }
}
