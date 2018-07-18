using d360.core;
using d360.core.enums;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;

namespace d360.web.Controllers.Services
{
    #region Classes for Custom Controller Logic Only

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

    public class JsonVersionModel
    {
        public string APIVersionNumber { get; set; }
        public string ImplementationVersion { get; set; }
    }

    internal interface IFilterModel
    {
        string FieldName { get; set; }
        bool Negated { get; set; }
    }
    internal class BaseFilterModel
    {
        public string FieldName { get; set; }
        public bool Negated { get; set; }
    }
    internal class SingleValueFilterModel : BaseFilterModel, IFilterModel
    {
        public string Value { get; set; }
    }
    internal class MultiValueFilterModel : BaseFilterModel, IFilterModel
    {
        public List<string> Values { get; set; }
    }
    /// <summary>
    /// Should support the following filter functions:
    /// 
    /// range(adatetime,incl,bdatetime,excl)
    /// range(anumber,incl,bnumber,incl), 
    /// adatetime..bdatetime
    /// anumber..bnumber
    /// ...adatetime
    /// ...anumber
    /// adatetime...
    /// anumber...
    /// </summary>
    internal class RangeValueFilterModel : BaseFilterModel, IFilterModel
    {
        public string StartValue { get; set; }
        public bool StartInclusive { get; set; }
        public string EndValue { get; set; }
        public bool EndInclusive { get; set; }
    }
    internal enum SearchFilterType
    {
        Contains,
        Match,
        Prefix,
        Suffix
    }
    /// <summary>
    /// Should support the following filter functions:
    /// 
    /// match_casesens(astring)
    /// match_casesens(astring,bstring,cstring)
    /// match(astring)
    /// match(astring,bstring,cstring)
    /// contains_casesens(astring)
    /// contains_casesens(astring,bstring,cstring)
    /// contains(astring)
    /// contains(astring,bstring,cstring)
    /// prefix_casesens(astring)
    /// prefix_casesens(astring,bstring,cstring)
    /// prefix(astring)
    /// prefix(astring,bstring,cstring)
    /// suffix_casesens(astring)
    /// suffix_casesens(astring,bstring,cstring)
    /// suffix(astring)
    /// suffix(astring,bstring,cstring)
    /// </summary>
    internal class SearchFilterModel : BaseFilterModel, IFilterModel
    {
        public SearchFilterType Type { get; set; }

        public bool CaseSensitive { get; set; }

        public List<string> Values { get; set; }
    }

    [DataContract(Namespace = "http://www.api.londonmarketgroup.co.uk/schema/2017/07/error", Name ="Error")]
    public class HttpCustomApiError
    {
        public HttpCustomApiError(string message, HttpStatusCode code)
        {
            Code = (int)code;
            Message = message;
        }

        [JsonProperty(Order = 1)]
        [DataMember(Order=1)]
        public string Message { get; set; }
        [JsonProperty(Order = 2)]
        [DataMember(Order =2)]
        public int Code { get; set; }        
    }


    #endregion

    /// <summary>
    /// This service houses all endpoints handling custom API configurations.
    /// </summary>
    [RoutePrefix("services/custom"), Authorize]
    public class CustomController : BaseApiController
    {
        
        #region DI

        public CustomController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
            
        }

        #endregion

        
        #region Error Handling Helper

        private HttpResponseMessage CreateCustomApiError(HttpStatusCode status, string message)
        {            
            HttpCustomApiError err = new HttpCustomApiError(message, status);

            var acceptHeaders = Request.Headers.Accept;
            var asJson = !acceptHeaders.Any(i => i.MediaType == "application/xml");
            return Request.CreateResponse<HttpCustomApiError>(status, err, asJson ? "application/json": "application/xml");
        }

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
            
            try
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
                                 ServiceID = s.ID,
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
                    return CreateCustomApiError(HttpStatusCode.NotFound, "Endpoint not found.");

                var acceptHeaders = Request.Headers.Accept;

                var asJson = !acceptHeaders.Any(i => i.MediaType == "application/xml");

                #region Begin: Singleton endpoint processing

                #region Base SQL statements

                var sql = @"
    select  D.[key] as id 
            {0} 
    from    AssetApiModel A
            cross apply utility.GetAssetBusinessKey(A.ID) D 
            {1} 
    where   A.AssetTypeID = @id
            and D.[key] = @key";

                #endregion

                var columnSql = "";
                var fieldSql = "";

                // special case for reference item lists
                // add the code field with same value as id
                var assetTypeId = config.First().AssetType.ID;
                var assetType = Company.AssetTypes.FirstOrDefault(x => x.ID == assetTypeId);

                // gov=4840 lloyds changed there mind they dont want this after all
                /*if (assetType != null && assetType.Object == "ReferenceItemType")
                {
                    columnSql += ", D.[Key] as [Code]";
                }*/

                
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

                    switch ((f.FieldType.Type ?? "").ToUpper()) 
                    {
                        case "DATE":                            
                            columnSql += $", convert(varchar, cast(F{ fID}.FormattedValue as date), 120) as [{fieldName}]";
                            break;
                        default:
                            columnSql += $", F{fID}.FormattedValue as [{fieldName}]";
                            break;
                    }
                    
                    fieldSql += $" left join Field F{fID} on F{fID}.AssetID = A.ID and F{fID}.FieldTypeID = {f.FieldType.ID}";

                }

                // Now, format the SQL to get the items.
                sql = string.Format(sql, columnSql, fieldSql);

                // Get the actual results from DB.
                var asset = Company.Query<dynamic>(sql, new { id = config.First().AssetType.ID, key }).FirstOrDefault();

                if (asset == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Item not found.");

                var canoUri = Request.RequestUri.PathAndQuery;

                //Determine whether it is JSON or XML to send back to caller, and format appropriately.
                if (asJson)
                {
                    //var json = new JsonResultsModel { total = count, items = new List<dynamic>asset, _links = new List<JsonResultLinkModel>() };
                    //json._links.Add(new JsonResultLinkModel { href = canoUri, @ref = JsonResultLinkModel.CANO });
                    //return Request.CreateResponse(HttpStatusCode.OK, json, "application/json");

                    ((IDictionary<string, Object>)asset).Add("_links", new List<JsonResultLinkModel> { new JsonResultLinkModel { href = canoUri, @ref = JsonResultLinkModel.CANO } });

                    return Request.CreateResponse(HttpStatusCode.OK, asset as object, "application/json");
                }
                else
                {
                    var serviceID = config.First().ServiceID;
                    var namespaces = Company.ApiNamespaces.Where(i => i.ServiceID == serviceID).ToDictionary(k => k.Node, v => v.Namespace);

                    XElement xAsset = DynamicHelper.ConvertToXml(asset, "item", namespaces);
                    xAsset.Add(new XAttribute("id", asset.id));

                    XElement xLinks = DynamicHelper.GetXElement("Links", namespaces, xAsset); 
                    XElement link = DynamicHelper.GetXElement("link", namespaces, xLinks);

                    link.Add(new XAttribute("rel", JsonResultLinkModel.CANO), new XAttribute("href", canoUri));

                    xLinks.Add(link);
                    xAsset.Add(xLinks);
                    
                   /* var CollectionWrapper = new XElement(
                        "CollectionWrapper",
                        xLinks,
                        xAsset
                    );*/

                    //XNamespace ns = "http://www.lmtom.london/schema/endpoints/Lloyds/RiskCode/v1";
                    //CollectionWrapper.Add(new XAttribute(XNamespace.Xmlns + "", ns));

                    //responseMessage = Request.CreateResponse(HttpStatusCode.OK, CollectionWrapper, "application/xml");
                    return Request.CreateResponse(HttpStatusCode.OK, xAsset, "application/xml");
                }

                #endregion End: Collection endpoint processing

                //return Request.CreateResponse<string>($"Service: {service}, Endpoint: {endpoint}, Version: {version}, Entity: {entityFormat}, Key: {key}");
            }
            catch (Exception r)
            {
                SendException(r, new Dictionary<string, string>());

                return CreateCustomApiError(HttpStatusCode.InternalServerError, "A server error occured. Please try your request again at a later time");
            }
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
            
            try
            {                
                if (Request.RequestUri.ToString().Length > 16000)
                    return CreateCustomApiError(HttpStatusCode.NotFound, "Request URI must not exceed 16,000 characters.");

                var queryParams = Request.GetQueryNameValuePairs();

                #region config

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
                                 ServiceID = s.ID,
                                 s.MaximumCacheAge,
                                 en.AssetType,
                                 en.FieldTypes,
                                 f.AllowFilter,
                                 f.AllowSelect,
                                 f.AllowSort,
                                 f.JsonFieldNameOverride,
                                 f.XmlFieldNameOverride,
                                 f.FieldType,
                                 EntityUri = u
                             }).ToList();

                #endregion

                if (config.Count <= 0)
                    return CreateCustomApiError(HttpStatusCode.NotFound, "Endpoint not found.");

                var maxAge = config[0].MaximumCacheAge;

                var acceptHeaders = Request.Headers.Accept;

                var asJson = !acceptHeaders.Any(i => i.MediaType == "application/xml");

                #region Begin: Collection endpoint processing

                var dbArgs = new DynamicParameters();

                #region Base SQL statements

                var countSql = @"
    select  count(1)
    from    AssetApiModel A
            {0} 
    where   A.AssetTypeID = @id";

                var sql = @"
    select  D.[Key] as id
            {0}
    from    AssetApiModel A
            inner join Asset O on O.ID = A.ID
            cross apply utility.GetAssetBusinessKey(A.ID) D 
            {1} 
    where   A.AssetTypeID = @id
            {2}
            {3}";

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

                        if (pageSize > 200)
                            return CreateCustomApiError(HttpStatusCode.BadRequest, "_pageSize parameter has a maximum supported value of 200.");
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

                    if(pageNumber < 1)
                        return CreateCustomApiError(HttpStatusCode.BadRequest, "_pageNum parameter must be greater than 0.");
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
                int arrSelectValidFieldCount = 0;
                if (queryParams.Any(i => i.Key == "_select"))
                {
                    var select = queryParams.SingleOrDefault(i => i.Key == "_select");
                    arrSelect = select.Value.Split(',').ToList();
                }
                                
                #endregion

                #region Field Filter Processing

                    var filterErrors = new List<string>();

                var filters = new List<IFilterModel>();
                foreach (var qp in queryParams.Where(i => i.Key != "_pageNum" && i.Key != "_pageSize" && i.Key != "_order" && i.Key != "_select"))
                {
                    var fieldToFilter = qp.Key;
                    var fieldValueToFilterBy = qp.Value;

                    // 1. check to see if the value is negated (has a ! at the first character of the filter query string value. This negates all comma-delimited values.)
                    var isNegated = fieldValueToFilterBy.StartsWith("!");
                    if (isNegated) fieldValueToFilterBy = fieldValueToFilterBy.Remove(0, 1); //Now, remove this c=! so it does not interfere with further processing.

                    // 2. Check for functions in the value. This requires special processing, based on the function name passed.
                    var continueChecking = true;

                    if (continueChecking)
                    {
                        // range(adatetime,incl,bdatetime,excl)     range(anumber,incl,bnumber,incl), 
                        // adatetime..bdatetime                     anumber..bnumber
                        // ...adatetime                             ...anumber
                        // adatetime...                             anumber...
                        if (fieldValueToFilterBy.StartsWith("range(") || fieldValueToFilterBy.Contains("..") || fieldValueToFilterBy.StartsWith("...") || fieldValueToFilterBy.EndsWith("..."))
                        {
                            var filter = new RangeValueFilterModel { Negated = isNegated, FieldName = fieldToFilter };

                            if (fieldValueToFilterBy.StartsWith("range("))
                            {
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("range(", "").Replace(")", "");
                                var rangeValues = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                                if (rangeValues.Count == 4)
                                {
                                    filter.StartValue = rangeValues[0];
                                    filter.StartInclusive = rangeValues[1].Equals("incl");
                                    filter.EndValue = rangeValues[2];
                                    filter.EndInclusive = rangeValues[3].Equals("incl");
                                }
                                else
                                {
                                    filterErrors.Add($"{filter.FieldName} filter must have exactly four comma-delimited values within the range function.");
                                }
                            }
                            else if (fieldValueToFilterBy.Contains(".."))
                            {
                                var rangeValues = fieldValueToFilterBy.Split(new string[1]{ ".." }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToList();
                                if (rangeValues.Count == 2)
                                {
                                    filter.StartValue = rangeValues[0];
                                    filter.StartInclusive = true;
                                    filter.EndValue = rangeValues[1];
                                    filter.EndInclusive = true;
                                }
                                else
                                {
                                    filterErrors.Add($"{filter.FieldName} filter must have exactly two values when using the dot-range function.");
                                }
                            }
                            else if (fieldValueToFilterBy.StartsWith("..."))
                            {
                                fieldValueToFilterBy = fieldValueToFilterBy.Remove(0, 3).Trim();
                                filter.StartValue = null;
                                filter.StartInclusive = true;
                                filter.EndValue = fieldValueToFilterBy;
                                filter.EndInclusive = true;
                            }
                            else if (fieldValueToFilterBy.EndsWith("..."))
                            {
                                fieldValueToFilterBy = fieldValueToFilterBy.Remove(fieldValueToFilterBy.Length-1-3, 3).Trim();
                                filter.StartValue = fieldValueToFilterBy;
                                filter.StartInclusive = true;
                                filter.EndValue = null;
                                filter.EndInclusive = true;
                            }

                            if (!string.IsNullOrEmpty(filter.StartValue) || !string.IsNullOrEmpty(filter.EndValue))
                            {
                                filters.Add(filter);
                            }

                            continueChecking = false;
                        }
                    }

                    if (continueChecking)
                    {
                        // contains_casesens(astring)                  contains_casesens(astring,bstring,cstring)
                        // contains(astring)                           contains(astring,bstring,cstring)
                        if (fieldValueToFilterBy.StartsWith("contains(") || fieldValueToFilterBy.StartsWith("contains_casesens("))
                        {
                            var filter = new SearchFilterModel { Negated = isNegated, FieldName = fieldToFilter, Type = SearchFilterType.Contains };

                            if (fieldValueToFilterBy.StartsWith("contains("))
                            {
                                filter.CaseSensitive = false;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("contains(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }
                            else if (fieldValueToFilterBy.StartsWith("contains_casesens("))
                            {
                                filter.CaseSensitive = true;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("contains_casesens(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }

                            filters.Add(filter);
                            continueChecking = false;
                        }
                    }

                    if (continueChecking)
                    {
                        // match_casesens(astring)                  match_casesens(astring,bstring,cstring)
                        // match(astring)                           match(astring,bstring,cstring)
                        if (fieldValueToFilterBy.StartsWith("match(") || fieldValueToFilterBy.StartsWith("match_casesens("))
                        {
                            var filter = new SearchFilterModel { Negated = isNegated, FieldName = fieldToFilter, Type = SearchFilterType.Match };

                            if (fieldValueToFilterBy.StartsWith("match("))
                            {
                                filter.CaseSensitive = false;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("match(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }
                            else if (fieldValueToFilterBy.StartsWith("match_casesens("))
                            {
                                filter.CaseSensitive = true;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("match_casesens(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }

                            filters.Add(filter);
                            continueChecking = false;
                        }
                    }

                    if (continueChecking)
                    {
                        // prefix_casesens(astring)                  prefix_casesens(astring,bstring,cstring)
                        // prefix(astring)                           prefix(astring,bstring,cstring)
                        if (fieldValueToFilterBy.StartsWith("prefix(") || fieldValueToFilterBy.StartsWith("prefix_casesens("))
                        {
                            var filter = new SearchFilterModel { Negated = isNegated, FieldName = fieldToFilter, Type = SearchFilterType.Prefix };

                            if (fieldValueToFilterBy.StartsWith("prefix("))
                            {
                                filter.CaseSensitive = false;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("prefix(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }
                            else if (fieldValueToFilterBy.StartsWith("prefix_casesens("))
                            {
                                filter.CaseSensitive = true;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("prefix_casesens(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }

                            filters.Add(filter);
                            continueChecking = false;
                        }
                    }

                    if (continueChecking)
                    {
                        // suffix_casesens(astring)                  suffix_casesens(astring,bstring,cstring)
                        // suffix(astring)                           suffix(astring,bstring,cstring)
                        if (fieldValueToFilterBy.StartsWith("suffix(") || fieldValueToFilterBy.StartsWith("suffix_casesens("))
                        {
                            var filter = new SearchFilterModel { Negated = isNegated, FieldName = fieldToFilter, Type = SearchFilterType.Suffix };

                            if (fieldValueToFilterBy.StartsWith("suffix("))
                            {                                
                                filter.CaseSensitive = false;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("suffix(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }
                            else if (fieldValueToFilterBy.StartsWith("suffix_casesens("))
                            {
                                filter.CaseSensitive = true;
                                fieldValueToFilterBy = fieldValueToFilterBy.Replace("suffix_casesens(", "").ReplaceLast(")", "");
                                filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                            }

                            filters.Add(filter);
                            continueChecking = false;
                        }
                    }

                    if (continueChecking)
                    {
                        // astring          astring,bstring,cstring
                        if (fieldValueToFilterBy.Contains(","))
                        {
                            var filter = new MultiValueFilterModel { Negated = isNegated, FieldName = fieldToFilter, Values = fieldValueToFilterBy.Split(',').ToList() };
                            filters.Add(filter);
                        }
                        else
                        {
                            var filter = new SingleValueFilterModel { Negated = isNegated, FieldName = fieldToFilter, Value = fieldValueToFilterBy };
                            filters.Add(filter);
                        }

                        continueChecking = false;
                    }
                }

                #endregion

                if (filterErrors.Count > 0)
                {
                    //There are errors parsing the filters. Return an error HTTP status to the caller.                    
                    return CreateCustomApiError(HttpStatusCode.BadRequest, $"Filter expressions contained the following errors: {string.Join("; ", filterErrors)}.");
                }

                var columnSql = "";
                var fieldSql = "";
                var additionalWhereSql = "";
                var orderSql = "";
                var defaultOrderBySql = " order by D.[key]";
                var defaultOrderBySqlSet = false;

                // special case for reference item lists
                // add the code field with same value as id
                var assetTypeId = config.First().AssetType.ID;
                var assetType = Company.AssetTypes.FirstOrDefault(x => x.ID == assetTypeId);

                // gov-4840 lloyds doesnt want this after all
               /* if (assetType != null && assetType.Object == "ReferenceItemType")
                {
                    columnSql += ", D.[Key] as [Code]";
                }*/
                

                foreach (var f in config)
                {
                    var fID = f.FieldType.ID;
                    var fieldName = f.FieldType.Name;
                    var fieldDataType = f.FieldType.Type;
                    System.Data.DbType fieldDbType;
                    // Determine SQL Server data type.
                    switch (fieldDataType)
                    {
                        case "Boolean":
                            fieldDataType = "bit";
                            fieldDbType = System.Data.DbType.Byte;
                            break;
                        case "Date":
                            fieldDataType = "date";
                            fieldDbType = System.Data.DbType.Date;
                            break;
                        case "DateTime":
                            fieldDataType = "datetime";
                            fieldDbType = System.Data.DbType.DateTime;
                            break;
                        case "Number":
                            fieldDataType = "int";
                            fieldDbType = System.Data.DbType.Int32;
                            break;
                        case "Decimal":
                            fieldDataType = "decimal(18, 4)";
                            fieldDbType = System.Data.DbType.Decimal;
                            break;
                        default:
                            fieldDataType = "nvarchar";
                            fieldDbType = System.Data.DbType.String;
                            break;
                    }

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
                            else
                            {
                                //validate if select fields have been specified if this field is a select field or not so we can later compare 
                                // the count of valid select fields to the number specified
                                arrSelectValidFieldCount++;
                            }
                        }
                    }

                    if (arrSort != null && !includeJoin) //Only process this if the JOIN is not going to be loaded already.
                    {
                        includeJoin = arrSort.Any(s => s.FieldName == fieldName) && f.AllowSort;
                    }

                    // Determine if casting the formatted value is required.
                    var formattedValueColumnSql = $"F{fID}.FormattedValue";

                    if (fieldDataType == "date")
                    {
                        formattedValueColumnSql = $"convert(varchar, cast(F{fID}.FormattedValue as date), 120)";
                    }
                    else if (fieldDataType != "nvarchar")
                    {
                        formattedValueColumnSql = $"cast(F{fID}.FormattedValue as {fieldDataType})";
                    }

                
                    if (includeColumn)
                        columnSql += $", {formattedValueColumnSql} as [{fieldName}]";

                    if (includeJoin)
                    {
                        var filter = filters.FirstOrDefault(i => i.FieldName == fieldName);
                        if (filter != null)
                        {
                            fieldSql += $" inner join Field F{fID} on F{fID}.AssetID = A.ID and F{fID}.FieldTypeID = {f.FieldType.ID}";
                            var fieldFilterSql = "";
                            var @operator = filter.Negated ? "<>" : "=";
                            var conjunction = (filter.Negated ? " and " : " or ");
                            if (filter is SingleValueFilterModel)
                            {
                                var singleValueFilter = filter as SingleValueFilterModel;

                                fieldFilterSql += $"({formattedValueColumnSql} {@operator} @{filter.FieldName})";

                                // TRUE / FALSE DATA TYPE HANDLING
                                if(fieldDbType == System.Data.DbType.Byte)
                                {
                                    if((singleValueFilter.Value ?? "").ToUpper() == "TRUE")
                                        dbArgs.Add($"@{filter.FieldName}", 1, fieldDbType);
                                    else if((singleValueFilter.Value ?? "").ToUpper() == "FALSE")
                                        dbArgs.Add($"@{filter.FieldName}", 0, fieldDbType);
                                    else
                                        dbArgs.Add($"@{filter.FieldName}", singleValueFilter.Value, fieldDbType);
                                }
                                else
                                    dbArgs.Add($"@{filter.FieldName}", singleValueFilter.Value, fieldDbType);
                            }
                            else if (filter is MultiValueFilterModel)
                            {
                                var multiValueFilter = filter as MultiValueFilterModel;
                                var loopNumber = 1;
                                foreach (var v in multiValueFilter.Values)
                                {
                                    if (!string.IsNullOrEmpty(fieldFilterSql)) fieldFilterSql += conjunction;

                                    fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}{loopNumber}";
                                    dbArgs.Add($"@{filter.FieldName}{loopNumber}", v, fieldDbType);
                                    loopNumber++;
                                }
                                fieldFilterSql = $"({fieldFilterSql})";
                            }
                            else if (filter is RangeValueFilterModel)
                            {
                                var rangeFilter = filter as RangeValueFilterModel;

                                if (!string.IsNullOrEmpty(rangeFilter.StartValue) && string.IsNullOrEmpty(rangeFilter.EndValue))
                                {
                                    // Start Value only.
                                    @operator = (rangeFilter.Negated ? " < " : " > ") + (rangeFilter.StartInclusive ? "=" : "");
                                    fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}Start";
                                    dbArgs.Add($"@{filter.FieldName}Start", rangeFilter.StartValue, fieldDbType);
                                }
                                else if (!string.IsNullOrEmpty(rangeFilter.StartValue) && !string.IsNullOrEmpty(rangeFilter.EndValue))
                                {
                                    // Start Value and End Value.
                                    if (rangeFilter.StartInclusive && rangeFilter.EndInclusive)
                                    {
                                        @operator = rangeFilter.Negated ? " not between " : " between ";
                                        fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}Start and @{filter.FieldName}End";
                                    }
                                    else
                                    {
                                        if (rangeFilter.Negated)
                                        {
                                            var startOperator = rangeFilter.StartInclusive ? "<=" : "<";
                                            var endOperator = rangeFilter.EndInclusive ? ">=" : ">";
                                            fieldFilterSql += $"{formattedValueColumnSql} {startOperator} @{filter.FieldName}Start and {formattedValueColumnSql} {endOperator} @{filter.FieldName}End";
                                        }
                                        else
                                        {
                                            var startOperator = rangeFilter.StartInclusive ? ">=" : ">";
                                            var endOperator = rangeFilter.EndInclusive ? "<=" : "<";
                                            fieldFilterSql += $"{formattedValueColumnSql} {startOperator} @{filter.FieldName}Start and {formattedValueColumnSql} {endOperator} @{filter.FieldName}End";
                                        }
                                    }
                                    dbArgs.Add($"@{filter.FieldName}Start", rangeFilter.StartValue, fieldDbType);
                                    dbArgs.Add($"@{filter.FieldName}End", rangeFilter.EndValue, fieldDbType);
                                }
                                else if (string.IsNullOrEmpty(rangeFilter.StartValue) && !string.IsNullOrEmpty(rangeFilter.EndValue))
                                {
                                    // End Value only.
                                    @operator = (rangeFilter.Negated ? " > " : " < ") + (rangeFilter.EndInclusive ? "=" : "");
                                    fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}End";
                                    dbArgs.Add($"@{filter.FieldName}End", rangeFilter.EndValue, fieldDbType);
                                }

                                fieldFilterSql = $"({fieldFilterSql})";
                            }
                            else if (filter is SearchFilterModel)
                            {
                                var searchFilter = filter as SearchFilterModel;
                                var loopNumber = 1;
                                foreach (var v in searchFilter.Values)
                                {
                                    if (!string.IsNullOrEmpty(fieldFilterSql)) fieldFilterSql += conjunction;
                                    var likeFormat = "";
                                    switch (searchFilter.Type)
                                    {
                                        case SearchFilterType.Contains:
                                            @operator = filter.Negated ? "not like" : "like";
                                            fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}{loopNumber}";
                                            likeFormat = "%{0}%";
                                            break;
                                        case SearchFilterType.Prefix:
                                            @operator = filter.Negated ? "not like" : "like";
                                            fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}{loopNumber}";
                                            likeFormat = "{0}%";
                                            break;
                                        case SearchFilterType.Suffix:
                                            @operator = filter.Negated ? "not like" : "like";
                                            fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}{loopNumber}";
                                            likeFormat = "%{0}";
                                            break;
                                        default: //Match
                                            fieldFilterSql += $"{formattedValueColumnSql} {@operator} @{filter.FieldName}{loopNumber}";
                                            likeFormat = "{0}";
                                            break;
                                    }
                                    if (searchFilter.CaseSensitive)
                                    {
                                        fieldFilterSql += " Collate SQL_Latin1_General_CP1_CS_AS";
                                    }
                                    dbArgs.Add($"@{filter.FieldName}{loopNumber}", string.Format(likeFormat, v), fieldDbType);
                                    loopNumber++;
                                }

                                fieldFilterSql = $"({fieldFilterSql})";
                            }

                            if (!string.IsNullOrEmpty(fieldFilterSql))
                            {
                                fieldSql += $" and {fieldFilterSql}";
                            }

                            filters.Remove(filter);
                        }
                        else
                        {
                            fieldSql += $" left join Field F{fID} on F{fID}.AssetID = A.ID and F{fID}.FieldTypeID = {f.FieldType.ID}";
                        }
                    }

                    //Now process the order by string.
                    if (f.AllowSort && arrSort != null && includeJoin)
                    {
                        var sRaw = arrSort.FirstOrDefault(s => s.FieldName == fieldName);
                        if (sRaw != null)
                        {
                            orderSql += ((string.IsNullOrEmpty(orderSql)) ? " order by " : ", ") + $"{formattedValueColumnSql}";
                            orderSql += sRaw.IsAscending ? " asc" : " desc";
                            arrSort.Remove(sRaw);
                        }
                    }

                    if (!defaultOrderBySqlSet && includeJoin)
                    {
                        defaultOrderBySql = $"order by {formattedValueColumnSql}";
                        defaultOrderBySqlSet = true;
                    }
                }

                #region Last_modified Field Filter Check

                if (filters.Count > 0)
                {
                    var filter = filters.FirstOrDefault(i => i.FieldName == "last_modified");
                    if (filter != null)
                    {
                        var @operator = filter.Negated ? "<>" : "=";

                        if (filter is SingleValueFilterModel)
                        {
                            var singleValueFilter = filter as SingleValueFilterModel;
                            additionalWhereSql += $"O.UpdatedOn {@operator} @{filter.FieldName}) ";
                            dbArgs.Add($"@{filter.FieldName}", singleValueFilter.Value, System.Data.DbType.DateTime);
                        }
                        else if (filter is MultiValueFilterModel)
                        {
                            var multiValueFilter = filter as MultiValueFilterModel;
                            additionalWhereSql += "(";
                            var loopNumber = 1;
                            var dateFieldString = "";
                            foreach (var v in multiValueFilter.Values)
                            {
                                if (!string.IsNullOrEmpty(dateFieldString)) dateFieldString += " OR ";
                                dateFieldString += $"O.UpdatedOn {@operator} @{filter.FieldName}{loopNumber}";
                                dbArgs.Add($"@{filter.FieldName}{loopNumber}", v, System.Data.DbType.DateTime);
                                loopNumber++;
                            }
                            additionalWhereSql += dateFieldString;
                            additionalWhereSql += ") ";
                        }
                        else if (filter is RangeValueFilterModel)
                        {
                            var rangeFilter = filter as RangeValueFilterModel;

                            if (rangeFilter.StartValue == ".") rangeFilter.StartValue = string.Empty;
                            if (rangeFilter.EndValue == ".") rangeFilter.EndValue = string.Empty;

                            if (!string.IsNullOrEmpty(rangeFilter.StartValue) && string.IsNullOrEmpty(rangeFilter.EndValue))
                            {
                                // Start Value only.
                                @operator = (rangeFilter.Negated ? " < " : " > ") + (rangeFilter.StartInclusive ? "=" : "");
                                additionalWhereSql += $"O.UpdatedOn {@operator} @{filter.FieldName}Start";
                                dbArgs.Add($"@{filter.FieldName}Start", rangeFilter.StartValue, System.Data.DbType.DateTime);
                            }
                            else if (!string.IsNullOrEmpty(rangeFilter.StartValue) && !string.IsNullOrEmpty(rangeFilter.EndValue))
                            {
                                // Start Value and End Value.
                                if (rangeFilter.StartInclusive && rangeFilter.EndInclusive)
                                {
                                    @operator = rangeFilter.Negated ? " not between " : " between ";
                                    additionalWhereSql += $"O.UpdatedOn {@operator} @{filter.FieldName}Start and @{filter.FieldName}End";
                                }
                                else
                                {
                                    if (rangeFilter.Negated)
                                    {
                                        var startOperator = rangeFilter.StartInclusive ? "<=" : "<";
                                        var endOperator = rangeFilter.EndInclusive ? ">=" : ">";
                                        additionalWhereSql += $"O.UpdatedOn {startOperator} @{filter.FieldName}Start and O.UpdatedOn {endOperator} @{filter.FieldName}End";
                                    }
                                    else
                                    {
                                        var startOperator = rangeFilter.StartInclusive ? ">=" : ">";
                                        var endOperator = rangeFilter.EndInclusive ? "<=" : "<";
                                        additionalWhereSql += $"O.UpdatedOn {startOperator} '@{filter.FieldName}Start and O.UpdatedOn {endOperator} @{filter.FieldName}End";
                                    }
                                }
                                dbArgs.Add($"@{filter.FieldName}Start", rangeFilter.StartValue, System.Data.DbType.DateTime);
                                dbArgs.Add($"@{filter.FieldName}End", rangeFilter.EndValue, System.Data.DbType.DateTime);
                            }
                            else if (string.IsNullOrEmpty(rangeFilter.StartValue) && !string.IsNullOrEmpty(rangeFilter.EndValue))
                            {
                                // End Value only.
                                @operator = (rangeFilter.Negated ? " > " : " < ") + (rangeFilter.EndInclusive ? "=" : "");
                                additionalWhereSql += $"O.UpdatedOn {@operator} @{filter.FieldName}End";
                                dbArgs.Add($"@{filter.FieldName}End", rangeFilter.EndValue, System.Data.DbType.DateTime);
                            }
                        }
                        else if (filter is SearchFilterModel)
                        {
                            return CreateCustomApiError(HttpStatusCode.BadRequest, $"Search filters are invalid in your filter query parameter: last_modified.");
                        }

                        if (!string.IsNullOrEmpty(additionalWhereSql))
                            additionalWhereSql = " and " + additionalWhereSql;

                        filters.Remove(filter);
                    }
                }

                #endregion

                #region Last_modified order check

                if (arrSort != null)
                {
                    if (arrSort.Count > 0)
                    {

                        var sRaw = arrSort.FirstOrDefault(s => s.FieldName == "last_modified");
                        if (sRaw != null)
                        {
                            orderSql += ((string.IsNullOrEmpty(orderSql)) ? " order by " : ", ") + "O.UpdatedOn";
                            orderSql += sRaw.IsAscending ? " asc" : " desc";
                            arrSort.Remove(sRaw);
                        }
                    }
                }

                #endregion

                #region VALIDATION: Has user included any sort fields that are not valid on this endpoint? If so, throw error.
                if (arrSort != null)
                {
                    if (arrSort.Count > 0)
                    {                        
                        return CreateCustomApiError(HttpStatusCode.BadRequest, "You have invalid fields in your _order query parameter.");
                    }
                }
                #endregion

                #region VALIDATION: Has user included any filter fields that are not valid on this endpoint? If so, throw error.
                if (filters != null)
                {
                    if (filters.Count > 0)
                    {
                        var badFilterFieldNames = string.Join(", ", filters.Select(i => i.FieldName));

                        return CreateCustomApiError(HttpStatusCode.BadRequest, $"You have invalid fields in your filter query parameters: {badFilterFieldNames}.");
                    }
                }
                #endregion

                #region VALIDATION: Has the user included any select fields that are not valid on this endpoint? If so, throw an error.


                if(arrSelect != null && arrSelect.Count > 0)
                {
                    if(arrSelect.Count != arrSelectValidFieldCount)
                    {
                        var badSelectFieldNames = string.Join(", ", arrSelect);

                        return CreateCustomApiError(HttpStatusCode.BadRequest, $"You have invalid fields in your select query parameters: {badSelectFieldNames}.");
                    }
                }

                #endregion

                //Add final dynamic parameter.
                dbArgs.Add("@id", config.First().AssetType.ID, System.Data.DbType.Int32);

                // Get total number of items to send back to caller in response message.
                countSql = string.Format(countSql, fieldSql);
                var count = Company.Query<int>(countSql, dbArgs).Single();

                // Now, format the SQL to get the items.
                sql = string.Format(sql, columnSql, fieldSql, additionalWhereSql, orderSql);

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
                var assets = Company.Query<dynamic>(sql, dbArgs); //new { id = config.First().AssetType.ID }

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

                var showPrevLink = (currentPageNumber > 1) && (count > ((currentPageNumber-1) * pageSize));
                var showNextLink = (count > (currentPageNumber * pageSize));

                #endregion

                HttpResponseMessage responseMessage = null;

                //Determine whether it is JSON or XML to send back to caller, and format appropriately.
                if (asJson)
                {
                    var json = new JsonResultsModel { total = count, items = assets, _links = new List<JsonResultLinkModel>() };

                    json._links.Add(new JsonResultLinkModel { href = canoUri, @ref = JsonResultLinkModel.CANO });
                    if (showNextLink)
                        json._links.Add(new JsonResultLinkModel { href = nextUri, @ref = JsonResultLinkModel.NEXT });
                    if (showPrevLink)
                        json._links.Add(new JsonResultLinkModel { href = prevUri, @ref = JsonResultLinkModel.PREV });

                    responseMessage = Request.CreateResponse(HttpStatusCode.OK, json, "application/json");
                }
                else
                {
                    int serviceID = config.First().ServiceID;
                    var namespaces = Company.ApiNamespaces.Where(i => i.ServiceID == serviceID).ToDictionary(k => k.Node, v => v.Namespace);


                    var CollectionWrapper = DynamicHelper.GetXElement(
                        "CollectionWrapper",
                        namespaces);

                    var xItems = DynamicHelper.GetXElement("Items", namespaces, CollectionWrapper);

                    foreach (var a in assets)
                    {
                        var xNode = DynamicHelper.ConvertToXml(a, "item", namespaces, xItems);
                        (xNode as XElement).Add(new XAttribute("id", a.id));
                        xItems.Add(xNode);
                    }

                    var xLinks = DynamicHelper.GetXElement("Links", namespaces, CollectionWrapper);

                    var link = DynamicHelper.GetXElement("link", namespaces, xLinks);
                    link.Add(new XAttribute("rel", JsonResultLinkModel.CANO), new XAttribute("href", canoUri));
                    xLinks.Add(link);
                        
                    if (showNextLink)
                    {
                        link = DynamicHelper.GetXElement("link", namespaces, xLinks);
                        link.Add(new XAttribute("rel", JsonResultLinkModel.NEXT), new XAttribute("href", nextUri));
                        xLinks.Add(link);
                    }
                    if (showPrevLink)
                    {
                        link = DynamicHelper.GetXElement("link", namespaces, xLinks);
                        link.Add(new XAttribute("rel", JsonResultLinkModel.PREV), new XAttribute("href", prevUri));
                        xLinks.Add(link);
                    }

                    CollectionWrapper.Add(
                        DynamicHelper.GetXElement("Total", namespaces, CollectionWrapper, count),
                        xLinks,
                        xItems
                    );

                    responseMessage = Request.CreateResponse(HttpStatusCode.OK, CollectionWrapper, "application/xml");
                }

                responseMessage.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, maxAge) };

                return responseMessage;

                #endregion End: Collection endpoint processing
            }
            catch (Exception r)
            {
                SendException(r, new Dictionary<string,string>());

                return CreateCustomApiError(HttpStatusCode.InternalServerError, "A server error occured. Please try your request again at a later time");
            }
        }


        #region Version Endpoints

        [HttpGet, Route("{service}/{endpoint}/{version}/version")]        
        public HttpResponseMessage GetEndpointVersion(string service, string endpoint, string version)
        {
            

            try
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
                             select new
                             {
                                 ServiceID = s.ID,
                                 MajorVersion = v.MajorVersion,
                                 MinorVersion = v.MinorVersion,
                                 MaximumCacheAge = s.MaximumCacheAge
                             }).FirstOrDefault();

                if (config == null)
                {                    
                    return CreateCustomApiError(HttpStatusCode.NotFound, "Endpoint not found.");
                }

                var acceptHeaders = Request.Headers.Accept;

                var asJson = !acceptHeaders.Any(i => i.MediaType == "application/xml");

                HttpResponseMessage responseMessage = null;

                var apiVersion = $"{config.MajorVersion}.{config.MinorVersion}";
                var governVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                //Determine whether it is JSON or XML to send back to caller, and format appropriately.
                if (asJson)
                {
                    var json = new JsonVersionModel { APIVersionNumber = apiVersion, ImplementationVersion = governVersion };
                    
                    responseMessage = Request.CreateResponse(HttpStatusCode.OK, json , "application/json");
                }
                else
                {
                    //XNamespace ns = "http://www.api.londonmarketgroup.co.uk/schema/2017/07/version";

                    var serviceID = config.ServiceID;
                    var namespaces = Company.ApiNamespaces.Where(i => i.ServiceID == serviceID).ToDictionary(k => k.Node, v => v.Namespace);

                    XElement versionDoc = DynamicHelper.GetXElement("Version", namespaces);
                    versionDoc.Add(
                        DynamicHelper.GetXElement("APIVersionNumber", namespaces, versionDoc, apiVersion),
                        DynamicHelper.GetXElement("ImplementationVersion", namespaces, versionDoc, governVersion)
                        );
                    
                    responseMessage = Request.CreateResponse(HttpStatusCode.OK, versionDoc, "application/xml");
                }

                responseMessage.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { MaxAge = new TimeSpan(0, 0, 0, config.MaximumCacheAge) };

                return responseMessage;
            }
            catch (Exception)
            {                
                return CreateCustomApiError(HttpStatusCode.InternalServerError, "A server error occured. Please try your request again at a later time");
            }
        }
        #endregion

        #region Health Endpoints

        [AllowAnonymous, HttpGet, Route("{service}/{endpoint}/{version}/health")]
        public HttpResponseMessage GetEndpointHealth(string service, string endpoint, string version)
        {
            

            try
            {
                //test the database connection
                Company.Database.Connection.Open();

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
                             select new
                             {
                                 MajorVersion = v.MajorVersion,
                                 MinorVersion = v.MinorVersion,
                                 MaximumCacheAge = s.MaximumCacheAge
                             }).FirstOrDefault();

                if (config == null)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "Endpoint not found."); 
                }


            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "The underlying data source is not reachable.");
            }
            finally
            {
                Company.Database.Connection.Close();
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        #endregion

    }
}