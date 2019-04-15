using d360.core.entities;
using d360.extensions;
using d360.model;
using Microsoft.Web.Http;
using System.Linq;
using System.Web.Http;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// Search everything in Data3Sixty.
    /// </summary>
    [ApiVersion("1.0"), RoutePrefix("services/search"), Authorize]
    public class SearchController : BaseApiController
    {
        #region DI

        ISearchSource SearchSource;

        public SearchController(ICommunityContext community, ICompanyContext company, ISearchSource searchSource)
            : base(community, company)
        {
            SearchSource = searchSource;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phrase"></param>
        /// <returns></returns>
        [Route("")]
        public IQueryable<IndexResult> GetSearchResults(string phrase)
        {
            if (!string.IsNullOrEmpty(phrase))
            {
                var c = Community.GetById<Company>(Company.CurrentCompanyID, i => i.CompanyDomainSettings);
                var result = SearchSource.GetSearchResults(Company.CurrentCompanyID, Company.CurrentResourceID, phrase,200,0);
                result.Results.ForEach(i => {
                    i.AbsoluteUrl = string.Format("https://{0}.data3sixty.com/{1}", c.CompanyDomainSettings.First(d => d.IsPrimary).UrlPrefix, i.Url);
                });                
                return result.Results.AsQueryable();
            }
            else 
            {
                return null;
            }
        }
    }
}
