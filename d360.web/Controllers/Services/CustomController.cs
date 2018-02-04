using d360.core.entities;
using d360.model;
using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using d360.core.exceptions;
using d360.core.enums;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling custom API configurations.
    /// </summary>
    [RoutePrefix("services/custom"), Authorize]
    public class CustomController : BaseApiController
    {
        #region DI

        public CustomController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        /// <summary>
        /// Sends back data based on a custom route.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="version"></param>
        /// <param name="entityFormat"></param>
        /// <returns></returns>
        [HttpGet, Route("{service}/{endpoint}/{version}/{*entityFormat}")]
        public HttpResponseMessage GetDataBasedOnRoute(string service, string endpoint, string version, string entityFormat)
        {
            var queryParams = Request.GetQueryNameValuePairs();

            var assets = Company.Filter<AssetApiModel>(i => i.AssetTypeID == 1, 
                i => i.Fields);

            if (queryParams.Any(i => i.Key == "_sort"))
            {
                var sort = queryParams.SingleOrDefault(i => i.Key == "_sort");
                var arrSort = sort.Value.Split(',').ToList();
                foreach (var sRaw in arrSort)
                {
                    var s = sRaw;
                    var sAsc = true;
                    if (s.StartsWith("-"))
                    {
                        sAsc = false;
                        s = s.Replace("-", "");
                    }

                    assets = sAsc ? assets.OrderBy(i => i.Fields.Single(f => f.Name == s)).AsQueryable() :
                                    assets.OrderByDescending(i => i.Fields.Single(f => f.Name == s)).AsQueryable();
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, assets);

            //return Request.CreateResponse<string>($"Service: {service}, Endpoint: {endpoint}, Version: {version}, Entity: {entityFormat}");
        }

    }
}