using d360.core.entities;
using d360.model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using Microsoft.ApplicationInsights;
using d360.web.Models.Attributes;

namespace System.Net.Http
{
    /// <summary>
    /// Extends the HttpRequestMessage collection
    /// </summary>
    public static class HttpRequestMessageExtensions
    {

        /// <summary>
        /// Returns a dictionary of QueryStrings that's easier to work with 
        /// than GetQueryNameValuePairs KevValuePairs collection.
        /// 
        /// If you need to pull a few single values use GetQueryString instead.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetQueryStrings(this HttpRequestMessage request)
        {
            return request.GetQueryNameValuePairs()
                          .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns an individual querystring value
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetQueryString(this HttpRequestMessage request, string key)
        {
            // IEnumerable<KeyValuePair<string,string>> - right!
            var queryStrings = request.GetQueryNameValuePairs();
            if (queryStrings == null)
                return null;

            var match = queryStrings.FirstOrDefault(kv => string.Compare(kv.Key, key, true) == 0);
            if (string.IsNullOrEmpty(match.Value))
                return null;

            return match.Value;
        }

        /// <summary>
        /// Returns an individual HTTP Header value
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetHeader(this HttpRequestMessage request, string key)
        {
            IEnumerable<string> keys = null;
            if (!request.Headers.TryGetValues(key, out keys))
                return null;

            return keys.First();
        }

        /// <summary>
        /// Retrieves an individual cookie from the cookies collection
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cookieName"></param>
        /// <returns></returns>
        public static string GetCookie(this HttpRequestMessage request, string cookieName)
        {
            System.Net.Http.Headers.CookieHeaderValue cookie = request.Headers.GetCookies(cookieName).FirstOrDefault();
            if (cookie != null)
                return cookie[cookieName].Value;

            return null;
        }

    }
}

namespace d360.web.Controllers
{
    public class JsonNetResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public object Data { get; set; }

        public JsonSerializerSettings SerializerSettings { get; set; }
        public Formatting Formatting { get; set; }

        public JsonNetResult()
        {
            SerializerSettings = new JsonSerializerSettings();
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            HttpResponseBase response = context.HttpContext.Response;

            response.ContentType = !string.IsNullOrEmpty(ContentType)
              ? ContentType
              : "application/json";

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;

            if (Data != null)
            {
                JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = Formatting };

                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);

                writer.Flush();
            }
        }
    }

    //[ModifiedSinceHeaderAttribute]
    public class BaseApiController : System.Web.Http.ApiController
    {
        internal CompanyContext Company;
        internal CommunityContext Community;

        public BaseApiController(CommunityContext community, CompanyContext company)
        {
            Community = community;
            Company = company;
        }

        internal IQueryable<Resource> GetCompanyResources()
        {
            return (
                   from cr in Community.Table<CompanyResource>()
                   join r in Community.Table<Resource>() on cr.ResourceID equals r.ID
                   where cr.CompanyID == Company.CurrentCompanyID
                   select r
                );
        }

        internal void SendException(Exception ex, IDictionary<string, string> properties, IDictionary<string, double> metrics = null)
        {
            var telemetry = new TelemetryClient();
            properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackException(ex, properties, metrics);
            telemetry = null;
        }

        #region Private Methods

        internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true)
        {
            columns = "";
            joins = "";

            var fieldTypeRelationTypeString = type;
            switch (type)
            { 
                case "Rule":
                case "Policy":
                    break;
                default:
                    fieldTypeRelationTypeString += "Type";
                    break;
            }
            var fields = Company.Filter<FieldTypeWithRelation>(i => i.Object == fieldTypeRelationTypeString && i.ObjectID == typeID && i.IsListable).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");
                if (includeIdColumn) columns += string.Format("{0}_T.Value as [{0}ID], ", name);
                columns += string.Format("{0}_T.FormattedValue as [{0}], ", name);
                joins += string.Format(" left join FieldWithRelation {0}_T on {0}_T.ObjectType = '{2}' and {0}_T.ObjectID = A.ID and {0}_T.FieldTypeID = {1} and {0}_T.IsListable = 1", name, f.ID, type);
            }

            fields = null;
        }

        internal string applyFilteringSuffix(string sql, System.Net.Http.HttpRequestMessage Request)
        {
            var query = Request.GetQueryStrings();

            int filterscount = 0;
            var filters = string.Empty;

            if (query.ContainsKey("filterscount"))
            { 
                if (int.TryParse(query["filterscount"], out filterscount))
                {
                    var filteredFields = new List<string>();    //Keeps track of the filters we have set so far.
                    for (int i = 0; i < filterscount; i++)
                    {
                        if (query.ContainsKey("filterdatafield" + i))
                        {
                            var fField = query["filterdatafield" + i];
                            filteredFields.Add(fField);                        
                        }
                    }

                    for (int i = 0; i < filterscount; i++)
                    {
                        if (query.ContainsKey("filterdatafield" + i) && query.ContainsKey("filtercondition" + i) && query.ContainsKey("filtervalue" + i))
                        {
                            var filter = "";
                            var fField = query["filterdatafield" + i];
                            var fCondition = query["filtercondition" + i];
                            var fValue = query["filtervalue" + i];
                            var fFormat = "";

                            switch (fCondition)
                            {
                                case "CONTAINS":
                                    fFormat = "[{0}] LIKE '%{1}%'";
                                    break;
                                case "DOES_NOT_CONTAIN":
                                    fFormat = "[{0}] NOT LIKE '%{1}%'";
                                    break;
                                case "EQUAL":
                                    fFormat = "[{0}] = '{1}'";
                                    break;
                                case "NOT_EQUAL":
                                    fFormat = "[{0}] <> '{1}'";
                                    break;
                                case "STARTS_WITH":
                                    fFormat = "[{0}] LIKE '{1}%'";
                                    break;
                                case "ENDS_WITH":
                                    fFormat = "[{0}] LIKE '%{1}'";
                                    break;
                            }

                            filter = string.Format(fFormat, fField, fValue.Replace("--", "").Replace("'", "''"));   //SQL Injection check

                            if (!string.IsNullOrEmpty(filter))
                            {
                                filters += (string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ";
                                filters += filter;
                            }
                        }
                    }

                    sql += filters;
                }            
            }


            return sql;
        }

        internal string applySortSuffix(string sql, System.Net.Http.HttpRequestMessage Request, string sortDefaultField = "Name")
        {
            string sortDataField = "";
            string sortOrder = "asc";

            var query = Request.GetQueryStrings();

            if (query.ContainsKey("sortDataField")) {
                sortDataField = query["sortDataField"];
            }
            if (query.ContainsKey("sortOrder"))
            {
                sortOrder = query["sortOrder"];
            }

            if (string.IsNullOrEmpty(sortDataField))
                sortDataField = sortDefaultField;

            sql += " ORDER BY [" + sortDataField + "] " + sortOrder;

            return sql;
        }

        internal string applyPagingSuffix(string sql, System.Net.Http.HttpRequestMessage Request)
        {
            int pagenum = 0;
            int pagesize = 20;

            var query = Request.GetQueryStrings();

            if (query.ContainsKey("pagenum")) {
                pagenum = int.Parse(query["pagenum"]);
            }
            if (query.ContainsKey("pagesize"))
            {
                pagesize = int.Parse(query["pagesize"]);
            }

            sql += string.Format(" OFFSET({0}) ROWS FETCH NEXT ({1}) ROWS ONLY", pagenum * pagesize, pagesize);

            return sql;
        }

        #endregion
    }

    //[ModifiedSinceHeaderAttribute]
    public class BaseController: Controller
    {
        internal CompanyContext Company;
        internal CommunityContext Community;

        public BaseController(CommunityContext community, CompanyContext company)
        {
            Community = community;
            Company = company;
        }

        internal IQueryable<Resource> GetCompanyResources()
        {
            return (
                   from cr in Community.CompanyResources
                   join r in Community.Resources on cr.ResourceID equals r.ID
                   where cr.CompanyID == Company.CurrentCompanyID
                   select r
                );
        }

        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = Int32.MaxValue
            };
        }

        internal Dictionary<string, object> SerializeDynamicObject(ExpandoObject obj)
        {
            var result = new Dictionary<string, object>();
            var dictionary = obj as IDictionary<string, object>;
            foreach (var item in dictionary)
                result.Add(item.Key, item.Value);
            return result;
        }

        //protected override void OnException(ExceptionContext filterContext)
        //{
        //    RedirectToAction("Index", "Error", new { error = filterContext.Exception });
        //    base.OnException(filterContext);
        //}

        internal void SendException(Exception ex, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (properties == null) properties = new Dictionary<string, string>();
            var telemetry = new TelemetryClient();
            properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackException(ex, properties, metrics);
            telemetry = null;
        }

        #region Private Methods

        internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true)
        {
            columns = "";
            joins = "";

            var fieldTypeRelationType = type;
            switch (type)
            { 
                case "Rule":
                    type = "Event";
                    break;
                default:
                    fieldTypeRelationType += "Type";
                    break;
            }

            var fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");
                if (includeIdColumn) columns += string.Format("{0}_T.Value as [{0}ID], ", name);
                columns += string.Format("{0}_T.FormattedValue as [{0}], ", name);
                joins += string.Format(" left join FieldWithRelation {0}_T on {0}_T.ObjectType = '{2}' and {0}_T.ObjectID = A.ID and {0}_T.FieldTypeID = {1} and {0}_T.IsListable = 1", name, f.ID, type);
            }

            fields = null;
        }

        internal string applyFilteringSuffix(string sql, System.Web.HttpRequestBase Request)
        {
            var query = Request.Params; 

            int filterscount = 0;
            var filters = "";

            if (int.TryParse(query["filterscount"], out filterscount))
            {
                var filteredFields = new List<string>();    //Keeps track of the filters we have set so far.
                for (int i = 0; i < filterscount; i++)
                {
                    var fField = query["filterdatafield" + i];
                    filteredFields.Add(fField);
                }

                for (int i = 0; i < filterscount; i++)
                {
                    var filter = "";
                    var fField = query["filterdatafield" + i];
                    var fCondition = query["filtercondition" + i];
                    var fValue = query["filtervalue" + i];
                    var fFormat = "";

                    switch (fCondition)
                    {
                        case "CONTAINS":
                            fFormat = "[{0}] LIKE '%{1}%'";
                            break;
                        case "DOES_NOT_CONTAIN":
                            fFormat = "[{0}] NOT LIKE '%{1}%'";
                            break;
                        case "EQUAL":
                            fFormat = "[{0}] = '{1}'";
                            break;
                        case "NOT_EQUAL":
                            fFormat = "[{0}] <> '{1}'";
                            break;
                        case "STARTS_WITH":
                            fFormat = "[{0}] LIKE '{1}%'";
                            break;
                        case "ENDS_WITH":
                            fFormat = "[{0}] LIKE '%{1}'";
                            break;
                    }

                    filter = string.Format(fFormat, fField, fValue.Replace("--", "").Replace("'", "''"));   //SQL Injection check

                    if (!string.IsNullOrEmpty(filter))
                    {
                        filters += (string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ";
                        filters += filter;
                    }
                }
            }

            var RelationshipIncludeType = Request.Form.AllKeys.Any(i => i == "RelationshipIncludeType") ? Request["RelationshipIncludeType"] : "";
            var RelationshipObjectType = Request.Form.AllKeys.Any(i => i == "RelationshipObjectType") ? Request["RelationshipObjectType"] : "";
            var RelationshipObjectIDs = Request.Form.AllKeys.Any(i => i == "RelationshipObjectIDs") ? Server.UrlDecode(Request["RelationshipObjectIDs"]) : "";

            if (!string.IsNullOrEmpty(RelationshipObjectIDs))
            {
                var IDs = RelationshipObjectIDs.Split(',').ToList();
                if (RelationshipIncludeType == "All")
                {
                    IDs.ForEach(ID =>
                    {
                        filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + "A.ID in (select SourceObjectID from cache.Relationships where SourceObject = 'Artifact' and TargetObject = '" + RelationshipObjectType + "' and TargetObjectID = " + ID + ")";
                    });
                }
                else
                {
                    var idList = "";
                    IDs.ForEach(ID =>
                    {
                        idList += (string.IsNullOrEmpty(idList) ? "" : ", ") + ID;
                    });
                    filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + "A.ID in (select SourceObjectID from cache.Relationships where SourceObject = 'Artifact' and TargetObject = '" + RelationshipObjectType + "' and TargetObjectID in (" + idList + "))";
                }
            }


            var AttributeType = Request.Form.AllKeys.Any(i => i == "AttributeType") ? Request["AttributeType"] : "";
            var AttributeSearchValue = Request.Form.AllKeys.Any(i => i == "AttributeSearchValue") ? Server.UrlDecode(Request["AttributeSearchValue"]) : "";

            if (!string.IsNullOrEmpty(AttributeType) && !string.IsNullOrEmpty(AttributeSearchValue))
            {
                int attributeTypeID;
                if (int.TryParse(AttributeType, out attributeTypeID))
                {
                    filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + @"A.ID in (
                    select ObjectID
                    from AttributeDetail
                    where ObjectType = 'Artifact' and AttributeTypeID = " + attributeTypeID + @" and FormattedValue like '%" + AttributeSearchValue.Replace("'", "''").Replace("--", "") + @"%'
                    union
                    select  R.SourceObjectID
                    from    cache.Relationships R
                            inner join AttributeDetail A on A.ObjectType = 'Intersect' and A.ObjectID = R.IntersectID and R.SourceType = 'ArtifactType' and R.SourceTypeID = @id and A.FormattedValue like '%" + AttributeSearchValue.Replace("'", "''").Replace("--", "") + @"%'
					)";
                }
            }

            sql += filters;

            return sql;
        }

        internal string applySortSuffix(string sql, string sortDataField, string sortOrder, string sortDefaultField = "Name", string sortDefaultDirection = "asc")
        {
            if (string.IsNullOrEmpty(sortDataField))
            {
                sortDataField = sortDefaultField;
                sortOrder = sortDefaultDirection;
            }

            sql += " ORDER BY [" + sortDataField + "] " + sortOrder;

            return sql;
        }

        internal string applyPagingSuffix(string sql, int pagenum = 0, int pagesize = 20)
        {
            sql += string.Format(" OFFSET({0}) ROWS FETCH NEXT ({1}) ROWS ONLY", pagenum * pagesize, pagesize);

            return sql;
        }

        #endregion
    }
}