using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.web.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// Search everything in Data3Sixty.
    /// </summary>
    [RoutePrefix("services/search"), Authorize]
    public class SearchController : BaseApiController
    {
        #region DI

        ISearchSource SearchSource;

        public SearchController(CommunityContext community, CompanyContext company, ISearchSource searchSource)
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
                var result = SearchSource.GetSearchResults(Company.CurrentCompanyID, Company.CurrentResourceID, phrase);
                result.Results.ForEach(i => {
                    i.AbsoluteUrl = string.Format("https://{0}.data3sixty.com/{1}", "", c.CompanyDomainSettings.First(d => d.IsPrimary).UrlPrefix, i.Url);
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
