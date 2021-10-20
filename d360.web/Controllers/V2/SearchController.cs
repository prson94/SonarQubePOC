using d360.core.entities;
using d360.extensions;
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

        public SearchController(CoreComponentSet set, ISearchSource searchSource) : base(set)
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE)
        ]
        public IQueryable<IndexResult> GetSearchResults(string phrase)
        {
            if (!string.IsNullOrEmpty(phrase))
            {
                var result = SearchSource.GetSearchResults(Company.CurrentCompanyID, Company.CurrentResourceID, phrase, 200, 0);
                result.Results.ForEach(i => {
                    i.AbsoluteUrl = string.Format($"https://{Community.GetPrimaryUrlPrefix()}.data3sixty.com/{i.Url}");
                });
                return result.Results.AsQueryable();
            }
            return null;            
        }
    }
}
