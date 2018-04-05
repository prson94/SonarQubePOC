using d360.core.enums;
using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;

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

            #endregion

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

            #region Field Filter Processing

            var filterErrors = new List<string>();

            var filters = new List<IFilterModel>();
            foreach (var qp in queryParams.Where(i => i.Key != "_pageNum" && i.Key != "_pageSize" && i.Key != "_order" && i.Key != "_select"))
            {
                var fieldToFilter = qp.Key;
                var fieldValueToFilterBy = qp.Value;

                // 1. check to see if the value is negated (has a ! at the first character of the filter query string value. This negates all comma-delimited values.)
                var isNegated = fieldValueToFilterBy.StartsWith("!");
                if (isNegated) fieldValueToFilterBy.Remove(0); //Now, remove this c=! so it does not interfere with further processing.

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
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("contains(", "").Replace(")", "");
                            filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                        }
                        else if (fieldValueToFilterBy.StartsWith("contains_casesens("))
                        {
                            filter.CaseSensitive = true;
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("contains_casesens(", "").Replace(")", "");
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
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("match(", "").Replace(")", "");
                            filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                        }
                        else if (fieldValueToFilterBy.StartsWith("match_casesens("))
                        {
                            filter.CaseSensitive = true;
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("match_casesens(", "").Replace(")", "");
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
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("prefix(", "").Replace(")", "");
                            filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                        }
                        else if (fieldValueToFilterBy.StartsWith("prefix_casesens("))
                        {
                            filter.CaseSensitive = true;
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("prefix_casesens(", "").Replace(")", "");
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
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("suffix(", "").Replace(")", "");
                            filter.Values = fieldValueToFilterBy.Split(',').Select(i => i.Trim()).ToList();
                        }
                        else if (fieldValueToFilterBy.StartsWith("suffix_casesens("))
                        {
                            filter.CaseSensitive = true;
                            fieldValueToFilterBy = fieldValueToFilterBy.Replace("suffix_casesens(", "").Replace(")", "");
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

                // NOTE: If implementations cannot correctly implement either exact matching or case insensitive matching then the implementation MUST return a 501 error code.
            }

            #endregion

            if (filterErrors.Count > 0)
            {
                //There are errors parsing the filters. Return an error HTTP status to the caller.
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Filter expressions contained the following errors: {string.Join("; ", filterErrors)}.");
            }

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
                {
                    var filter = filters.FirstOrDefault(i => i.FieldName == fieldName);
                    if (filter != null)
                    {
                        fieldSql += $" inner join Field F{fID} on F{fID}.AssetID = A.ID and F{fID}.FieldTypeID = {f.FieldType.ID}";
                        var fieldFilterSql = "";
                        var @operator = filter.Negated ? "<>" : "=";
                        if (filter is SingleValueFilterModel)
                        {
                            var singleValueFilter = filter as SingleValueFilterModel;

                            fieldFilterSql += $"(F{fID}.FormattedValue {@operator} '{singleValueFilter.Value}')";
                        }
                        else if (filter is MultiValueFilterModel)
                        {
                            var multiValueFilter = filter as MultiValueFilterModel;
                            foreach (var v in multiValueFilter.Values)
                            {
                                if (!string.IsNullOrEmpty(fieldFilterSql)) fieldFilterSql += " or ";
                                fieldFilterSql += $"F{fID}.FormattedValue {@operator} '{v}'";
                            }
                            fieldFilterSql = $"({fieldFilterSql})";
                        }

                        if (!string.IsNullOrEmpty(fieldFilterSql))
                        {
                            fieldSql += $" and {fieldFilterSql}";
                        }
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
        }

    }
}