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
using d360.web.Models;
using d360.core;
using Resources;

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
            SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;            
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

        internal bool HideData3SixtyUsers()
        {
            var hideData3SixtyUsers = false;
            var settings = Community.GetCompanySettings();
            if (settings.Any(i => i.Key == "HideData3SixtyUsers"))
            {
                hideData3SixtyUsers = bool.Parse(settings["HideData3SixtyUsers"]);
            }
            return hideData3SixtyUsers;
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

        [Route("responsibilities/{type}/{id:int}")]
        internal IQueryable<dynamic> GetResponsibilities(SystemObjects type, int id)
        {
            return Company.Query<dynamic>(
                QueryConstants.ResponsibilityList,
                new { ObjectType = type.ToString(), ObjectID = id }
            ).AsQueryable();
        }

        internal void SendException(Exception ex, IDictionary<string, string> properties, IDictionary<string, double> metrics = null)
        {
            var telemetry = new TelemetryClient();
            properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackException(ex, properties, metrics);
            telemetry = null;
        }

        #region Private Methods

        internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFieldName = true)
        {
            columns = "";
            joins = "";

            var fieldTypeRelationTypeString = type;
            switch (type)
            { 
                case "Rule":
                default:
                    fieldTypeRelationTypeString += "Type";
                    break;
            }
            var fields = Company.Filter<FieldTypeWithRelation>(i => i.Object == fieldTypeRelationTypeString && i.ObjectID == typeID && i.IsListable).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");
                if (!useFieldName)
                {
                    var fieldName = $"Field{f.ID}";
                    if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";
                    columns += $"{name}_T.FormattedValue as [{fieldName}], ";
                    joins += $@" left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {f.ID} 
left join FieldType {name}_TT on {name}_TT.ID = {name}_T.FieldTypeID and {name}_TT.IsListable = 1";
                }
                else
                {
                    if (includeIdColumn) columns += string.Format("{0}_T.Value as [{0}ID], ", name);
                    columns += string.Format("{0}_T.FormattedValue as [{0}], ", name);
                    joins += $@" left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {f.ID} 
left join FieldType {name}_TT on {name}_TT.ID = {name}_T.FieldTypeID and {name}_TT.IsListable = 1";
                }
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

        internal List<FieldValidationModel> checkAndAddValidation(string fieldType, string friendlyName, bool required, string pattern, int? minLength, int? maxLength, string validationMessage = "")
        {
            var models = new List<FieldValidationModel>();

            #region Validation

            if (fieldType != "Lookup")
            {
                if (string.IsNullOrEmpty(validationMessage))
                {
                    switch (fieldType)
                    {
                        case "Number":
                            validationMessage = string.Format(Validation.Pattern_Tokenized, friendlyName, "must be a whole number");
                            break;
                        case "Decimal":
                            validationMessage = string.Format(Validation.Pattern_Tokenized, friendlyName, "must be a decimal number");
                            break;
                    }
                }

                // Required validation
                if (required)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.Required_Tokenized, friendlyName), rule = "required" });
                }

                // Pattern validation
                if (!string.IsNullOrEmpty(pattern))
                {
                    models.Add(new FieldValidationModel { action = "blur", message = validationMessage, regex = pattern });
                }

                // Min/Max next precedent
                if (maxLength.HasValue && minLength.HasValue)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.Length_Tokenized, friendlyName, minLength.Value, maxLength.Value), rule = string.Format("length={0},{1}", minLength.Value, maxLength.Value) });
                }
                // Min next precedent
                else if (minLength.HasValue)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.MaxLength_Tokenized, friendlyName, minLength.Value), rule = string.Format("minLength={0}", minLength.Value) });
                }
                // Max next precedent
                else if (maxLength.HasValue)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.MinLength_Tokenized, friendlyName, maxLength.Value), rule = string.Format("maxLength={0}", maxLength.Value) });
                }
            }

            #endregion

            return models.Count > 0 ? models : null;
        }

        internal IQueryable<GlobalReportingResource> GetCompanyResources()
        {
            var hideData3SixtyUsers = HideData3SixtyUsers();
            var query = Company.Table<GlobalReportingResource>();
            return ((HideData3SixtyUsers()) ? query.Where(i => !i.Email.Contains("data3sixty.com")) : query);
        }

        internal bool HideData3SixtyUsers()
        {
            var hideData3SixtyUsers = false;
            var settings = Community.GetCompanySettings();
            if (settings.Any(i => i.Key == "HideData3SixtyUsers"))
            {
                hideData3SixtyUsers = bool.Parse(settings["HideData3SixtyUsers"]);
            }
            return hideData3SixtyUsers;
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

        internal List<EditableField> loadDynamicFields(List<EditableField> list, List<FieldTypeWithRelation> fields, int startRow = 10)
        {
            var row = startRow;

            fields.ForEach(f =>
            {
                if (f.Type != DataType.Attribute.ToString() && f.Type != DataType.FilteredLookup.ToString() && f.Type != DataType.RelationLookup.ToString() && f.Type != DataType.ComplexRelationLookup.ToString())
                {
                    var patternMessage = "";

                    if (string.IsNullOrEmpty(f.ValidationDescription))
                    {
                        switch (f.Type)
                        {
                            case "Number":
                                patternMessage = "must be a whole number";
                                break;
                            case "Decimal":
                                patternMessage = "must be a decimal number";
                                break;
                        }
                    }
                    else
                    {
                        patternMessage = f.ValidationDescription;
                    }


                    var fld = new EditableField
                    {
                        Row = row,
                        Column = 1,
                        FieldName = f.Name,
                        Name = f.FriendlyName,
                        FieldType = f.Type.ToString(),
                        FieldDescription = f.FormDescription,
                        Validations = checkAndAddValidation(f.Type.ToString(), f.FriendlyName, f.IsRequired, f.Pattern, f.MinimumLength, f.MaximumLength, patternMessage)
                    };

                    if (f.Type == DataType.FusionLookup.ToString())
                    {
                        //need to render drop down of all fusion attributes that have the same type as the current
                        var IDs = Company.Filter<FieldTypeFusionLookupDefinition>(x => x.FieldTypeID == f.ID).Select(i => i.SourceFusionAttributeTypeID).Distinct().ToList();

                        if (!f.IsRequired)
                            fld.Items.Add(new SelectListItem { Text = "", Value = "" });

                        fld.Items.AddRange(
                            Company.Filter<FusionAttribute>(x => IDs.Contains(x.FusionAttributeTypeID), i => i.FusionAttributeType)
                            .Select(i => new { i.ID, i.TextPath, Type = i.FusionAttributeType.Name })
                            .ToList()
                            .Select(i =>
                                new SelectListItem
                                {
                                    Group = new SelectListGroup { Name = i.Type },
                                    Text = i.TextPath,
                                    Value = i.ID.ToString()
                                })
                        );
                    }

                    if (!string.IsNullOrEmpty(f.LookupObjectType))
                    {
                        fld.FieldType = DataType.Lookup.ToString();
                        try
                        {
                            //if (f.LookupObjectType == "Predicate")
                            //{
                            //    fld.Items = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == f.ID && o.LookupObjectType == f.LookupObjectType && o.LookupObjectID == f.LookupObjectID.Value)
                            //        .OrderBy(o => o.Text)
                            //        .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                            //        .ToList();
                            //}
                            //else
                            //{
                                fld.Items = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == f.ID && o.LookupObjectType == f.LookupObjectType && o.LookupObjectID == f.LookupObjectID.Value)
                                    .OrderBy(o => o.Text)
                                    .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                    .ToList();
                            //}
                            if (!f.IsRequired) fld.Items.Insert(0, new SelectListItem { Text = "Choose...", Value = "" });
                        }
                        catch
                        {
                            fld.Items.Add(new SelectListItem { Text = "No valid lookup found", Value = "" });
                        }
                    }
                    fld.Required = (f.MinimumLength > 0 || f.Length > 0);
                    /* Boolean, Date, DateTime, Decimal, Integer, String */
                    list.Add(fld);
                }
                row++;
            });

            return list;
        }

        internal List<EditableField> loadDynamicFields(List<EditableField> list, List<FieldTypeWithRelation> fieldTypes, List<FieldWithRelation> fields, int startRow = 10, bool decode = false)
        {
            var row = startRow;

            fieldTypes.ForEach(ft =>
            {
                if (ft.Type != DataType.FilteredLookup.ToString() && ft.Type != DataType.RelationLookup.ToString() && ft.Type != DataType.Attribute.ToString() && ft.Type != DataType.ComplexRelationLookup.ToString())
                {
                    var f = fields.SingleOrDefault(i => i.FieldTypeID == ft.ID);

                    var patternMessage = "";

                    if (string.IsNullOrEmpty(ft.ValidationDescription))
                    {
                        switch (ft.Type)
                        {
                            case "Number":
                                patternMessage = "must be a whole number";
                                break;
                            case "Decimal":
                                patternMessage = "must be a decimal number";
                                break;
                        }
                    }
                    else
                    {
                        patternMessage = ft.ValidationDescription;
                    }

                    var fld = new EditableField
                    {
                        Row = row,
                        Column = 1,
                        FieldName = ft.Name,
                        Name = ft.FriendlyName,
                        FieldType = ft.Type.ToString(),
                        FieldDescription = ft.FormDescription,
                        Validations = checkAndAddValidation(ft.Type.ToString(), ft.FriendlyName, ft.IsRequired, ft.Pattern, ft.MinimumLength, ft.MaximumLength, patternMessage)
                    };

                    if (ft.Type == DataType.FusionLookup.ToString())
                    {
                        var IDs = Company.Filter<FieldTypeFusionLookupDefinition>(x => x.FieldTypeID == ft.ID).Select(i => i.SourceFusionAttributeTypeID).Distinct().ToList();

                        if (!ft.IsRequired)
                            fld.Items.Add(new SelectListItem { Text = "", Value = "" });

                        fld.Items.AddRange(
                            Company.Filter<FusionAttribute>(x => IDs.Contains(x.FusionAttributeTypeID), i => i.FusionAttributeType)
                                .Select(i => new { i.ID, i.TextPath, Type = i.FusionAttributeType.Name })
                                .ToList()
                                .Select(i =>
                                    new SelectListItem
                                    {
                                        Group = new SelectListGroup { Name = i.Type },
                                        Text = i.TextPath,
                                        Value = i.ID.ToString()
                                    })
                                .OrderBy(x => x.Text)
                        );
                    }

                    if (!string.IsNullOrEmpty(ft.LookupObjectType))
                    {
                        //fld.FieldType = DataType.Lookup.ToString();
                        try
                        {
                            fld.Items = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == ft.ID && o.LookupObjectType == ft.LookupObjectType && o.LookupObjectID == ft.LookupObjectID.Value)
                                .OrderBy(o => o.Text)
                                .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                .ToList();
                            if (!ft.IsRequired) fld.Items.Insert(0, new SelectListItem { Text = "Choose...", Value = "" });
                        }
                        catch
                        {
                            fld.Items.Add(new SelectListItem { Text = "No valid lookup found", Value = "" });
                        }
                    }
                    fld.Required = (ft.MinimumLength > 0 || ft.Length > 0);
                    /* Boolean, Date, DateTime, Decimal, Integer, String */
                    if (f != null) fld.Value = decode ? Server.HtmlDecode(f.Value) : f.Value;
                    list.Add(fld);

                    row++;
                }
            });

            return list;
        }

        internal Dictionary<string, object> SerializeDynamicObject(ExpandoObject obj)
        {
            var result = new Dictionary<string, object>();
            var dictionary = obj as IDictionary<string, object>;
            foreach (var item in dictionary)
                result.Add(item.Key, item.Value);
            return result;
        }
        
        internal void SendException(Exception ex, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (properties == null) properties = new Dictionary<string, string>();
            var telemetry = new TelemetryClient();
            properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackException(ex, properties, metrics);
            telemetry = null;
        }

        #region Private Methods

         internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFriendlyName = false)
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

            var fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();

            foreach (var f in fields)
            {
                var name = $"Field{f.ID}";//f.Name.Replace("'", "''").Replace("--", "");
                var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");
                if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";
                columns += $"{name}_T.FormattedValue as [{(useFriendlyName ? friendlyName : name)}], ";
                joins += $@" left join FieldWithRelation {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {f.ID} 
left join FieldType {name}_TT on {name}_TT.ID = {name}_T.FieldTypeID and {name}_TT.IsListable = 1";
            }

            fields = null;
        }

        internal string addDynamicFieldSimpleFilter(string[] fixedColumns, string type, int typeID, string filterExp, Dapper.DynamicParameters dbArgs)
        {            
            if (string.IsNullOrEmpty(filterExp)) return "";

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

            //loop through visible fields for this item 
            var fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();

            StringBuilder sb = new StringBuilder();

            foreach (var column in fixedColumns)
            {
                if (sb.Length != 0) sb.Append(" or ");

                sb.Append($"({column} like @simpleFilter)");
            }
            
            foreach (var field in fields)
            {
                if (sb.Length != 0) sb.Append(" or ");

                var name = $"Field{field.ID}_T.FormattedValue";
                
                sb.Append($"({name} like @simpleFilter)");
            }
            

            // add value to db args
            dbArgs.Add("simpleFilter", $"%{filterExp}%");

            return $"({sb.ToString()})";
        }

        internal List<FieldType> getDynamicFieldJoinStatements(int typeID, string type, List<string> filterFields, out string joins, out string filterjoins, out string columns, out string filtercolumns, bool includeIdColumn = true, bool useFriendlyName = false)
        {
            columns = "";
            joins = "";

            filtercolumns = "";
            filterjoins = "";

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

            var fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();

            foreach (var f in fields)
            {
                var name = $"Field{f.ID}"; //f.Name.Replace("'", "''").Replace("--", "");
                var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");

                if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";

                var thisColumn = $", {name}_T.FormattedValue as [{(useFriendlyName ? friendlyName : name)}]";
                var thisJoin = $@" left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {f.ID} 
left join FieldType {name}_TT on {name}_TT.ID = {name}_T.FieldTypeID and {name}_TT.IsListable = 1";

                columns += thisColumn;
                joins += thisJoin;

                if (filterFields.Contains(name))
                {
                    filtercolumns += thisColumn;
                    filterjoins += thisJoin;
                }
            }
            return fields;
            //fields = null;
        }

        internal string getFilteringConditionBind(string field, string condition, int filterNumber, Dapper.DynamicParameters dbParams, string value, string prefix, bool skipFieldValidation = false)
        {
            var bind = $"{prefix}{filterNumber}val";

            if (!skipFieldValidation)
            {
                if (!isValidFieldName(field)) return string.Empty; // sql injection check on field name
            }
            
            switch (condition)
            {
                case "CONTAINS":
                    dbParams.Add(bind, $"%{value}%");
                    return $"{field} LIKE @{bind}";
                case "DOES_NOT_CONTAIN":
                    dbParams.Add(bind, $"%{value}%");
                    return $"{field} NOT LIKE @{bind}";                    
                case "EQUAL":
                    dbParams.Add(bind, $"{value}");
                    return $"{field} = @{bind}";                    
                case "NOT_EQUAL":
                    dbParams.Add(bind, $"{value}");
                    return $"{field} <> @{bind}";                    
                case "STARTS_WITH":
                    dbParams.Add(bind, $"{value}%");
                    return $"{field} LIKE @{bind}";                    
                case "ENDS_WITH":
                    dbParams.Add(bind, $"%{value}");
                    return $"{field} LIKE @{bind}";     
                //greater / less than cause issues with dates when casting...               
                /*case "GREATER_THAN":
                    dbParams.Add(bind, $"{value}");                    
                    return $"CAST({field} as numeric) > CAST(@{bind} as numeric)";
                case "GREATER_THAN_OR_EQUAL":
                    dbParams.Add(bind, $"{value}");
                    return $"CAST({field} as numeric) >= CAST(@{bind} as numeric)";                    
                case "LESS_THAN":
                    dbParams.Add(bind, $"{value}");
                    return $"CAST({field} as numeric) < CAST(@{bind} as numeric)";                    
                case "LESS_THAN_OR_EQUAL":
                    dbParams.Add(bind, $"{value}");
                    return $"CAST({field} as numeric) <= CAST(@{bind} as numeric)";                    */
                case "NULL":
                    return field + " is null";
                case "NOT_NULL":
                    return field + " is not null";
                case "EMPTY":
                    return field + " = ''";
                case "NOT_EMPTY":
                    return field + " <> ''";
                default:
                    dbParams.Add(bind, $"{value}");
                    return $"{field} = @{bind}";
            }
        }
        
        internal bool isValidFieldName(string field)
        {
            var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z][a-zA-Z0-9._-]+$");
            return nameRegex.IsMatch(field);
        }

        internal string applyRelationFilteringExists(string sql, System.Web.HttpRequestBase Request, Dapper.DynamicParameters dbParams)
        {
            var query = Request.Params;
            int filterscount = 0;            

            if (int.TryParse(query["relfilterscount"], out filterscount) && filterscount > 0)
            {
                StringBuilder sb = new StringBuilder();
                for (var i = 0; i < filterscount; i++)
                {                    
                    var fFieldId = int.Parse(query["relfilterdatafield" + i]);
                    var fCondition = query["relfiltercondition" + i];
                    var fValue = query["relfiltervalue" + i];

                    var filtersql = getFilteringConditionBind("relField.FormattedValue", fCondition, i, dbParams,fValue,"relflt",true);

                    if (string.IsNullOrEmpty(filtersql)) continue;

                    var existsql = @" and exists (select  B.sourceobjectid
                                from(
                                        select  IntersectID as ID,
                                                SourceObjectID
                                        from Relationship
                                        where SourceObjectType = 'Artifact'
                                                and SourceObjectID = A.id
                                        ) B left join FieldWithRelation relField on (relField.ObjectType = 'Intersect' and relField.ObjectID = B.ID and relField.FieldTypeID = {0})
                                        where " + filtersql + ")";

                    existsql = string.Format(existsql, fFieldId);
                                        
                    sb.Append(existsql);
                }

                return sql + sb.ToString();                
            }
            return sql;            
        }
        
        internal string applyHiddenFilteringSuffix(System.Web.HttpRequestBase Request, Dapper.DynamicParameters dbParams)
        {
            var query = Request.Params;

            int filterscount = 0;
            var filters = "";

            if (int.TryParse(query["hidfilterscount"], out filterscount))
            {
                for (int i = 0; i < filterscount; i++)
                {
                    var filter = "";
                    var fieldID = 0;
                    var fField = query["hidfilterdatafield" + i];
                    var fCondition = query["hidfiltercondition" + i];
                    var fValue = query["hidfiltervalue" + i];

                    if (!int.TryParse(fField, out fieldID)) continue;


                    if (string.IsNullOrEmpty(filters))
                        filter = $" inner join field hidft on (A.ID = hidft.objectID and hidft.ObjectType = 'Artifact') where ";
                    else
                        filter = " and ";

                    filter += getFilteringConditionBind("hidft.FormattedValue", fCondition, i, dbParams, fValue, "hidflt",true);

                    filter += $" and hidft.fieldtypeid={fieldID}";

                    if (!string.IsNullOrEmpty(filter))
                    {                        
                        filters += filter;
                    }
                }
            }

            return filters;
        }

        internal string applyFilteringSuffixBind(string sql, System.Web.HttpRequestBase Request, Dapper.DynamicParameters dbParams, bool applyHiddenFilters = false)
        {
            var query = Request.Params;

            int filterscount = 0;
            var filters = applyHiddenFilters ? applyHiddenFilteringSuffix(Request, dbParams) : string.Empty;

            if (int.TryParse(query["filterscount"], out filterscount))
            {
                for (int i = 0; i < filterscount; i++)
                {
                    var filter = "";
                    var fField = query["filterdatafield" + i];
                    var fCondition = query["filtercondition" + i];
                    var fValue = query["filtervalue" + i];

                    if (fValue.EndsWith(".000")) fValue = fValue.Replace(".000", "");

                    filter = getFilteringConditionBind(fField, fCondition, i, dbParams, fValue, "");// "flt");
                    
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

            //check querystring
            if(string.IsNullOrEmpty(RelationshipObjectIDs))
            {
                RelationshipIncludeType = query.AllKeys.Any(i => i == "RelationshipIncludeType") ? query["RelationshipIncludeType"] : "";
                RelationshipObjectType = query.AllKeys.Any(i => i == "RelationshipObjectType") ? query["RelationshipObjectType"] : "";
                RelationshipObjectIDs = query.AllKeys.Any(i => i == "RelationshipObjectIDs") ? Server.UrlDecode(query["RelationshipObjectIDs"]) : "";
            }

            if (!string.IsNullOrEmpty(RelationshipObjectIDs))
            {
                var IDs = RelationshipObjectIDs.Split(',').ToList();
                if (RelationshipIncludeType == "All")
                {                    
                    IDs.ForEach(ID =>
                    {
                        dbParams.Add("relTypeAdvFlt", RelationshipObjectType); // use bind variable to avoid sql injection

                        int idInt = 0;

                        if(int.TryParse(ID,out idInt)) //convert to integer to avoid sql injection
                            filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + "A.ID in (select SourceObjectID from cache.Relationships where SourceObject = 'Artifact' and TargetObject = @relTypeAdvFlt and TargetObjectID = " + idInt + ")";
                    });
                }
                else
                {
                    var idList = "";
                    IDs.ForEach(ID =>
                    {
                        int idInt = 0;

                        if (int.TryParse(ID, out idInt)) //convert to integer to avoid sql injection
                            idList += (string.IsNullOrEmpty(idList) ? "" : ", ") + idInt;
                    });

                    dbParams.Add("relTypeAdvFlt", RelationshipObjectType); // use bind variable to avoid sql injection

                    filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + "A.ID in (select SourceObjectID from cache.Relationships where SourceObject = 'Artifact' and TargetObject = @relTypeAdvFlt and TargetObjectID in (" + idList + "))";
                }
            }


            var AttributeType = Request.Form.AllKeys.Any(i => i == "AttributeType") ? Request["AttributeType"] : "";
            var AttributeSearchValue = Request.Form.AllKeys.Any(i => i == "AttributeSearchValue") ? Server.UrlDecode(Request["AttributeSearchValue"]) : "";

            //check querystring
            if (string.IsNullOrEmpty(AttributeType) && string.IsNullOrEmpty(AttributeSearchValue))
            {
                AttributeType = query.AllKeys.Any(i => i == "AttributeType") ? query["AttributeType"] : "";
                AttributeSearchValue = query.AllKeys.Any(i => i == "AttributeSearchValue") ? Server.UrlDecode(query["AttributeSearchValue"]) : "";
            }

            if (!string.IsNullOrEmpty(AttributeType) && !string.IsNullOrEmpty(AttributeSearchValue))
            {
                int attributeTypeID;
                if (int.TryParse(AttributeType, out attributeTypeID))
                {
                    dbParams.Add("attrTypeAdvFlt", "%" + AttributeSearchValue + "%"); // use bind variable to avoid sql injection

                    filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + @"A.ID in (
                    select ObjectID
                    from AttributeDetail
                    where ObjectType = 'Artifact' and AttributeTypeID = " + attributeTypeID + @" and FormattedValue like @attrTypeAdvFlt
                    union
                    select  R.SourceObjectID
                    from    cache.Relationships R
                            inner join AttributeDetail A on A.ObjectType = 'Intersect' and A.ObjectID = R.IntersectID and R.SourceType = 'ArtifactType' and R.SourceTypeID = @id and A.FormattedValue like @attrTypeAdvFlt
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

            sortOrder = (sortOrder ?? string.Empty).ToLower();

            //validate inputs            
            if ((!string.IsNullOrEmpty(sortOrder)) && sortOrder != "asc" && sortOrder != "desc")
            {
                throw new Exception("Invalid sort order specified");
            }
                        
            // make sure its a valid field name
            if (!isValidFieldName(sortDataField))
            {
                return sql;
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