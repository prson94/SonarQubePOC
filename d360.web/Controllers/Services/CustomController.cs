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

            var config = (
                         from s in Company.ApiServices
                         from e in s.Endpoints
                         from v in e.Versions
                         from en in v.Entities
                         from u in en.Uris
                         from f in en.FieldTypes
                         where s.UriPrefix == service
                         where e.UriPrefix == endpoint
                         where v.UriPrefix == version
                         where u.Format == entityFormat
                         select new {
                             ServiceName = s.Name,
                             en.AssetType,
                             en.FieldTypes,
                             f.AllowFilter,
                             f.AllowSelect,
                             f.AllowSort,
                             f.JsonFieldNameOverride,
                             f.XmlFieldNameOverride,
                             f.FieldType,
                             EntityUri = u
                         });

            if (config.Count() <= 0)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Endpoint not found.");

            var acceptHeaders = Request.Headers.Accept;

            var asJson = !acceptHeaders.Any(i => i.MediaType == "application/xml");

            var sql = @"
select  A.ID 
        {0}
from    AssetApiModel A
        {1} 
where   A.AssetTypeID = @id
        {2}
";

            var columnSql = "";
            var fieldSql = "";
            foreach (var f in config)
            {
                
                var fID = f.FieldType.ID;
                if (f.AllowSelect)
                {
                    var fieldName = f.FieldType.Name;
                    if (asJson && !string.IsNullOrEmpty(f.JsonFieldNameOverride))
                    {
                        fieldName = f.JsonFieldNameOverride.Trim();
                    }
                    else if (!string.IsNullOrEmpty(f.XmlFieldNameOverride))
                    {
                        fieldName = f.XmlFieldNameOverride.Trim();
                    }

                    // One last check.
                    if (string.IsNullOrEmpty(fieldName))
                    {
                        fieldName = f.FieldType.Name;
                    }

                    columnSql += $", F{fID}.FormattedValue as [{fieldName}]";
                    fieldSql += $" left join Field F{fID} on F{fID}.AssetID = A.ID and F{fID}.FieldTypeID = {f.FieldType.ID}";
                }
            }

            var orderSql = "";
            if (queryParams.Any(i => i.Key == "_order"))
            {
                var sort = queryParams.SingleOrDefault(i => i.Key == "_order");
                var arrSort = sort.Value.Split(',').ToList();
                foreach (var sRaw in arrSort)
                {
                    var s = sRaw;
                    var sAsc = true;
                    if (s.StartsWith("-"))
                    {
                        sAsc = false;
                        s = s.Replace("-", "").Trim();
                    }

                    var f = config.SingleOrDefault(i => i.JsonFieldNameOverride == s || i.XmlFieldNameOverride == s);

                    if (f != null)
                    {
                        if (f.AllowSort)
                        {
                            orderSql += ((string.IsNullOrEmpty(orderSql)) ? " order by " : ", ") + $"F{f.FieldType.ID}.FormattedValue";
                            orderSql += sAsc ? " asc" : " desc";
                        }
                    }
                }
            }

            sql = string.Format(sql, columnSql, fieldSql, orderSql);

            var assets = Company.Query<dynamic>(sql, new { id = config.First().AssetType.ID });

            if (asJson)
            {
                return Request.CreateResponse(HttpStatusCode.OK, assets, "application/json");
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, assets, "application/xml");
            }

            //return Request.CreateResponse<string>($"Service: {service}, Endpoint: {endpoint}, Version: {version}, Entity: {entityFormat}");
        }

    }
}