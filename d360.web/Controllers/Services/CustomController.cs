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
using System.Xml.Linq;
using System.Xml;
using System.Reflection;
using Newtonsoft.Json;

namespace d360.web.Controllers.Services
{
    internal class CustomApiSortField
    {
        public string FieldName { get; set; }
        public bool IsAscending { get; set; }
    }

    public class JsonResultLinkModel
    {
        internal const string ALTE = "alternate";
        internal const string CANO = "canonical";
        internal const string NEXT = "next";
        internal const string PREV = "previous";

        public string @ref { get; set; }
        public string href { get; set; }
    }

    public class JsonResultsModel
    {
        public int total { get; set; }
        public IEnumerable<dynamic> items { get; set; }
        public List<JsonResultLinkModel> _links { get; set; }
    }

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
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet, Route("{service}/{endpoint}/{version}/{entityFormat}/{key}")]
        public HttpResponseMessage GetSingletonBasedOnRoute(string service, string endpoint, string version, string entityFormat, string key)
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
                         where u.UriType == ApiUriType.Singleton
                         select new
                         {
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

            #region Begin: Singleton endpoint processing

            #region Base SQL statements

            var sql = @"
select  D.[key] as ID 
        {0} 
from    AssetApiModel A
        cross apply utility.GetAssetBusinessKey(A.ID) D 
        {1} 
where   A.AssetTypeID = @id
        and D.[key] = @key";

            #endregion

            var columnSql = "";
            var fieldSql = "";
            foreach (var f in config)
            {
                var fID = f.FieldType.ID;
                var fieldName = f.FieldType.Name;

                #region Process field name overrides, and determine name to use.

                if (asJson && !string.IsNullOrEmpty(f.JsonFieldNameOverride))
                {
                    fieldName = f.JsonFieldNameOverride.Trim();
                }
                else if (!string.IsNullOrEmpty(f.XmlFieldNameOverride))
                {
                    fieldName = f.XmlFieldNameOverride.Trim();
                }

                // One last check, set the default field name to be the Api Name of the FieldType, but only if the field name is empty.
                if (string.IsNullOrEmpty(fieldName))
                {
                    fieldName = f.FieldType.Name;
                }

                #endregion

                columnSql += $", F{fID}.FormattedValue as [{fieldName}]";
                fieldSql += $" left join Field F{fID} on F{fID}.AssetID = A.ID and F{fID}.FieldTypeID = {f.FieldType.ID}";

            }

            // Now, format the SQL to get the items.
            sql = string.Format(sql, columnSql, fieldSql);

            // Get the actual results from DB.
            var asset = Company.Query<dynamic>(sql, new { id = config.First().AssetType.ID, key }).FirstOrDefault();

            //Determine whether it is JSON or XML to send back to caller, and format appropriately.
            if (asJson)
            {
                return Request.CreateResponse(HttpStatusCode.OK, asset as object, "application/json");
            }
            else
            {
                XElement xAsset = DynamicHelper.ConvertToXml(asset, "item");
                xAsset.Add(new XAttribute("id", asset.ID));

                //var CollectionWrapper = new XElement(
                //    "CollectionWrapper",
                //    new XElement("total", count),
                //    xLinks,
                //    xItems
                //);

                //XNamespace ns = "http://www.lmtom.london/schema/endpoints/Lloyds/RiskCode/v1";
                //CollectionWrapper.Add(new XAttribute(XNamespace.Xmlns + "", ns));

                return Request.CreateResponse(HttpStatusCode.OK, xAsset, "application/xml");
            }

            #endregion End: Collection endpoint processing

            return Request.CreateResponse<string>($"Service: {service}, Endpoint: {endpoint}, Version: {version}, Entity: {entityFormat}, Key: {key}");
        }

        /// <summary>
        /// Sends back data based on a custom route.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="version"></param>
        /// <param name="entityFormat"></param>
        /// <returns></returns>
        [HttpGet, Route("{service}/{endpoint}/{version}/{*entityFormat}")]
        public HttpResponseMessage GetCollectionBasedOnRoute(string service, string endpoint, string version, string entityFormat)
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
                         where u.UriType == ApiUriType.Collection
                         select new
                         {
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

            #region Begin: Collection endpoint processing

            #region Base SQL statements

            var countSql = @"
select  count(1)
from    AssetApiModel A
    {0} 
where   A.AssetTypeID = @id";

            var sql = @"
select  D.[Key] as ID
    {0}
from    AssetApiModel A
        cross apply utility.GetAssetBusinessKey(A.ID) D 
    {1} 
where   A.AssetTypeID = @id
    {2}";

            #endregion

            #region Page Size Processing

            var rawPageSize = string.Empty;
            int pageSize = 200;
            if (queryParams.Any(i => i.Key == "_pageSize"))
            {
                var queryPageSize = queryParams.SingleOrDefault(i => i.Key == "_pageSize");
                if (!string.IsNullOrEmpty(queryPageSize.Value))
                {
                    if (!int.TryParse(queryPageSize.Value, out pageSize))
                    {
                        pageSize = 200;
                    }
                }
            }

            #endregion

            #region Page Number Processing

            var rawPageNumber = string.Empty;
            int pageNumber = 1;
            if (queryParams.Any(i => i.Key == "_pageNum"))
            {
                var queryPageNumber = queryParams.SingleOrDefault(i => i.Key == "_pageNum");
                if (!string.IsNullOrEmpty(queryPageNumber.Value))
                {
                    if (!int.TryParse(queryPageNumber.Value, out pageNumber))
                    {
                        pageNumber = 1;
                    }
                }
            }
            var currentPageNumber = pageNumber; //Nees to stay in this location as it records the unchanged current page, that will be used in later page number query string links.

            #endregion

            #region Order Processing

            List<CustomApiSortField> arrSort = null;
            if (queryParams.Any(i => i.Key == "_order"))
            {
                var sort = queryParams.SingleOrDefault(i => i.Key == "_order");
                var rawSortFields = sort.Value.Split(',').ToList();
                arrSort = rawSortFields.Select(i => new CustomApiSortField
                {
                    FieldName = i.TrimStart('-').Trim(),//i.StartsWith("-") ? i.TrimStart('-') i.Replace("-", "").Trim() : i.Trim(),
                    IsAscending = !i.StartsWith("-")
                }).ToList();
            }

            #endregion

            #region Field Selection Processing

            List<string> arrSelect = null;
            if (queryParams.Any(i => i.Key == "_select"))
            {
                var select = queryParams.SingleOrDefault(i => i.Key == "_select");
                arrSelect = select.Value.Split(',').ToList();
            }

            #endregion

            var columnSql = "";
            var fieldSql = "";
            var orderSql = "";
            var defaultOrderBySql = " order by D.[key]";
            var defaultOrderBySqlSet = false;
            foreach (var f in config)
            {
                var fID = f.FieldType.ID;
                var fieldName = f.FieldType.Name;

                #region Process field name overrides, and determine name to use.

                if (asJson && !string.IsNullOrEmpty(f.JsonFieldNameOverride))
                {
                    fieldName = f.JsonFieldNameOverride.Trim();
                }
                else if (!string.IsNullOrEmpty(f.XmlFieldNameOverride))
                {
                    fieldName = f.XmlFieldNameOverride.Trim();
                }

                // One last check, set the default field name to be the Api Name of the FieldType, but only if the field name is empty.
                if (string.IsNullOrEmpty(fieldName))
                {
                    fieldName = f.FieldType.Name;
                }

                #endregion

                var includeColumn = false;
                var includeJoin = false;

                if (f.AllowSelect)
                {
                    includeColumn = true;
                    includeJoin = true;

                    if (arrSelect != null)
                    {

                        if (!arrSelect.Contains(fieldName))
                        {
                            includeColumn = false;
                            includeJoin = false;
                        }
                    }
                }

                if (arrSort != null && !includeJoin) //Only process this if the JOIN is not going to be loaded already.
                {
                    includeJoin = arrSort.Any(s => s.FieldName == fieldName) && f.AllowSort;
                }

                if (includeColumn)
                    columnSql += $", F{fID}.FormattedValue as [{fieldName}]";

                if (includeJoin)
                    fieldSql += $" left join Field F{fID} on F{fID}.AssetID = A.ID and F{fID}.FieldTypeID = {f.FieldType.ID}";

                //Now process the order by string.
                if (f.AllowSort && arrSort != null && includeJoin)
                {
                    var sRaw = arrSort.FirstOrDefault(s => s.FieldName == fieldName);
                    if (sRaw != null)
                    {
                        orderSql += ((string.IsNullOrEmpty(orderSql)) ? " order by " : ", ") + $"F{f.FieldType.ID}.FormattedValue";
                        orderSql += sRaw.IsAscending ? " asc" : " desc";
                        arrSort.Remove(sRaw);
                    }
                }

                if (!defaultOrderBySqlSet && includeJoin)
                {
                    defaultOrderBySql = $"order by F{f.FieldType.ID}.FormattedValue";
                    defaultOrderBySqlSet = true;
                }
            }

            #region VALIDATION: Has user included any sort fields that are not valid on this endpoint? If so, throw error.
            if (arrSort != null)
            {
                if (arrSort.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "You have invalid fields in your _order query parameter.");
                }
            }
            #endregion

            // Get total number of items to send back to caller in response message.
            countSql = string.Format(countSql, fieldSql);
            var count = Company.Query<int>(countSql, new { id = config.First().AssetType.ID }).Single();

            // Now, format the SQL to get the items.
            sql = string.Format(sql, columnSql, fieldSql, orderSql);

            if (!sql.Contains("order by"))
            {
                sql += defaultOrderBySql;
            }

            // Page number is 0-based.
            if (pageNumber > 0)
                pageNumber -= 1;

            //Set paging in t-sql statement.
            sql += $" OFFSET({pageNumber * pageSize}) ROWS FETCH NEXT ({pageSize}) ROWS ONLY";

            // Get the actual results from DB.
            var assets = Company.Query<dynamic>(sql, new { id = config.First().AssetType.ID });

            #region Calculate the page links

            var uri = Request.RequestUri;
            var qs = uri.ParseQueryString();

            var canoUri = uri.PathAndQuery;
            var nextUri = uri.PathAndQuery;
            var prevUri = uri.PathAndQuery;

            nextUri = (nextUri.Contains("_pageNum=")) ?
                nextUri.Replace($"_pageNum={currentPageNumber}", $"_pageNum={currentPageNumber + 1}") :
                nextUri + $"&_pageNum={currentPageNumber + 1}";

            prevUri = prevUri.Contains("_pageNum=") ?
                prevUri.Replace($"_pageNum={currentPageNumber}", $"_pageNum={currentPageNumber - 1}") :
                prevUri + $"&_pageNum={currentPageNumber - 1}";

            var showPrevLink = (currentPageNumber > 1);
            var showNextLink = (count > (currentPageNumber * pageSize));

            #endregion

            //Determine whether it is JSON or XML to send back to caller, and format appropriately.
            if (asJson)
            {
                var json = new JsonResultsModel { total = count, items = assets, _links = new List<JsonResultLinkModel>() };

                json._links.Add(new JsonResultLinkModel { href = canoUri, @ref = JsonResultLinkModel.CANO });
                if (showNextLink)
                    json._links.Add(new JsonResultLinkModel { href = nextUri, @ref = JsonResultLinkModel.NEXT });
                if (showPrevLink)
                    json._links.Add(new JsonResultLinkModel { href = prevUri, @ref = JsonResultLinkModel.PREV });

                return Request.CreateResponse(HttpStatusCode.OK, json, "application/json");
            }
            else
            {
                var xItems = new XElement("items");

                foreach (var a in assets)
                {
                    var xNode = DynamicHelper.ConvertToXml(a, "item");
                    (xNode as XElement).Add(new XAttribute("id", a.ID));
                    xItems.Add(xNode);
                }

                var xLinks = new XElement("links");

                xLinks.Add(new XElement("link", new XElement("rel", JsonResultLinkModel.CANO), new XElement("href", canoUri)));
                if (showNextLink)
                    xLinks.Add(new XElement("link", new XElement("rel", JsonResultLinkModel.NEXT), new XElement("href", nextUri)));
                if (showPrevLink)
                    xLinks.Add(new XElement("link", new XElement("rel", JsonResultLinkModel.PREV), new XElement("href", prevUri)));

                var CollectionWrapper = new XElement(
                    "CollectionWrapper",
                    new XElement("total", count),
                    xLinks,
                    xItems
                );

                //XNamespace ns = "http://www.lmtom.london/schema/endpoints/Lloyds/RiskCode/v1";
                //CollectionWrapper.Add(new XAttribute(XNamespace.Xmlns + "", ns));

                return Request.CreateResponse(HttpStatusCode.OK, CollectionWrapper, "application/xml");
            }

            #endregion End: Collection endpoint processing

            // Temporary catch-all
            return Request.CreateResponse<string>($"Service: {service}, Endpoint: {endpoint}, Version: {version}, Entity: {entityFormat}");
        }

    }
}