using d360.core;
using d360.core.entities;
using d360.model;
using d360.web.Models;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Resources;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;

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
            var fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationTypeString && i.ObjectID == typeID && i.IsListable).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");
                if (!useFieldName)
                {
                    var fieldName = $"Field{f.ID}";
                    if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";

                    columns += $@"case 
    when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
    when {name}_T.Value is not null then {name}_T.FormattedValue 
    when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
    else '' 
end as [{fieldName}], ";

//                    joins += $@" left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {f.ID} 
//left join FieldType {name}_TT on {name}_TT.ID = {name}_T.FieldTypeID and {name}_TT.IsListable = 1";

                    joins += $@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID}
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {name}_TT.ID ";
                }
                else
                {
                    if (includeIdColumn) columns += string.Format("{0}_T.Value as [{0}ID], ", name);
                    columns += $@"case 
    when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
    when {name}_T.Value is not null then {name}_T.FormattedValue 
    when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
    else '' 
end as [{name}], ";
                    joins += $@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{type}' and {name}_TT.ObjectID = {typeID} 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {name}_TT.ID ";

//                    joins += $@" left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {f.ID} 
//left join FieldType {name}_TT on {name}_TT.ID = {name}_T.FieldTypeID and {name}_TT.IsListable = 1";
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

        internal JsonNetResult jsonNetException(Exception ex)
        {
            return new JsonNetResult
            {
                Data = new { type = "error", title = "Error Occurred!", message = ex.GetFullExceptionData() },
                Formatting = Newtonsoft.Json.Formatting.None
            };
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

        internal List<EditableField> loadDynamicFields(List<EditableField> list, List<FieldType> fields, int startRow = 10)
        {
            var row = startRow;

            fields.ForEach(f =>
            {
                if (f.IsEditable)
                {
                    #region Is Editable

                    if (f.Type != DataType.Attribute.ToString() && f.Type != DataType.FilteredLookup.ToString() && f.Type != DataType.ComplexRelationLookup.ToString())
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
                            Validations = checkAndAddValidation(f.Type.ToString(), f.FriendlyName, f.IsRequired, f.Pattern, f.MinimumLength, f.MaximumLength, patternMessage),
                            Category = f.Category
                        };

                        if (!string.IsNullOrEmpty(f.DefaultValue))
                        {
                            fld.Value = f.DefaultValue;
                        }

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
                                fld.Items = new List<SelectListItem>();

                                if (!f.IsRequired) fld.Items.Add(new SelectListItem { Text = "Choose...", Value = "" });
                                if (f.AllowAllValue) fld.Items.Add(new SelectListItem { Text = f.AllowAllLabel, Value = "0" });

                                fld.Items.AddRange(
                                    Company.Filter<FieldLookupValue>(o => o.FieldTypeID == f.ID && o.LookupObjectType == f.LookupObjectType && o.LookupObjectID == f.LookupObjectID.Value)
                                    .OrderBy(o => o.Text)
                                    .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                    .ToList()
                                );
                                //}
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

                    #endregion Is Editable
                }
                row++;
            });

            return list;
        }

        internal List<EditableField> loadDynamicFields(List<EditableField> list, List<FieldType> fieldTypes, List<FieldWithRelation> fields, int startRow = 10, bool decode = false)
        {
            var row = startRow;

            fieldTypes.ForEach(ft =>
            {
                if (ft.IsEditable)
                {
                    #region Is Editable

                    if (ft.Type != DataType.FilteredLookup.ToString() && ft.Type != DataType.Attribute.ToString() && ft.Type != DataType.ComplexRelationLookup.ToString())
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
                            Validations = checkAndAddValidation(ft.Type.ToString(), ft.FriendlyName, ft.IsRequired, ft.Pattern, ft.MinimumLength, ft.MaximumLength, patternMessage),
                            Category = ft.Category
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
                                fld.Items = new List<SelectListItem>();

                                if (!ft.IsRequired)
                                    fld.Items.Add(new SelectListItem { Text = "Choose...", Value = "" });

                                if (ft.AllowAllValue)
                                    fld.Items.Add(new SelectListItem { Text = ft.AllowAllLabel, Value = "0" });

                                fld.Items.AddRange(
                                    Company.Filter<FieldLookupValue>(o => o.FieldTypeID == ft.ID && o.LookupObjectType == ft.LookupObjectType && o.LookupObjectID == ft.LookupObjectID.Value)
                                        .OrderBy(o => o.Text)
                                        .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                        .ToList()
                                );
                            }
                            catch
                            {
                                fld.Items.Add(new SelectListItem { Text = "No valid lookup found", Value = "" });
                            }
                        }
                        fld.Required = (ft.MinimumLength > 0 || ft.Length > 0);
                        /* Boolean, Date, DateTime, Decimal, Integer, String */
                        if (f != null) fld.Value = decode ? Server.HtmlDecode(f.Value) : f.Value;
                        if (f == null && !string.IsNullOrEmpty(ft.DefaultValue))
                        {
                            fld.Value = ft.DefaultValue;
                        }
                        list.Add(fld);

                        row++;
                    }

                    #endregion Is Editable
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

        #region Dynamic Query Processing

        public class DynamicPagedResults
        {
            public int total { get; set; }
            public IEnumerable<dynamic> results { get; set; }
        }

        internal string addOwnershipJoinCriteria(string joins, string ownerUsers, string ownerGroups)
        {
            int index = 0;
            if (!string.IsNullOrEmpty(ownerUsers))
            {
                foreach (var user in ownerUsers.Split(','))
                {
                    var ids = user.Split('|');
                    if (ids.Length == 2)
                    {
                        joins += $" inner join responsibilitydetail RD{index} on (RD{index}.ObjectID = A.ID and RD{index}.Visible = 1 and RD{index}.ObjectType = 'Artifact' and RD{index}.ResponsibleObjectType = 'resource' and RD{index}.ResponsibleObjectID = {int.Parse(ids[1])} and RD{index}.ResponsibilityTypeID = {int.Parse(ids[0])} )";
                        index++;
                    }
                }
            }

            if (!string.IsNullOrEmpty(ownerGroups))
            {
                foreach (var group in ownerGroups.Split(','))
                {
                    var ids = group.Split('|');
                    if (ids.Length == 2)
                    {
                        joins += $" inner join responsibilitydetail RD{index} on (RD{index}.ObjectID = A.ID and RD{index}.Visible = 1 and RD{index}.ObjectType = 'Artifact' and RD{index}.ResponsibleObjectType = 'group' and RD{index}.ResponsibleObjectID = {int.Parse(ids[1])} and RD{index}.ResponsibilityTypeID = {int.Parse(ids[0])})";
                        index++;
                    }
                }
            }

            return joins;
        }

        internal List<FieldType> getFieldTypesByObjectType(string objectType, int objectTypeID, bool listableOnly)
        {
            return (listableOnly) ?
                Company.Filter<FieldType>(i => i.Object == objectType && i.ObjectID == objectTypeID && i.IsListable).OrderBy(i => i.SortOrder).ToList() :
                Company.Filter<FieldType>(i => i.Object == objectType && i.ObjectID == objectTypeID).OrderBy(i => i.SortOrder).ToList();
        }

        internal DynamicPagedResults processDynamicResults(
            string sql,
            HttpRequestBase Request,
            string objectType, int objectTypeID,
            bool listableOnly, string sortDataField, string sortOrder, int pagenum, int pagesize,
            string[] staticFields,
            string filter = "", string ownerUsers = "", string ownerGroups = "",
            string sortDefaultField = "Name", string sortDefaultDirection = "asc",
            Dictionary<string, object> extraParams = null,
            bool applyHiddenFilters = false, bool includeIdColumn = true, bool useFriendlyName = false)
        {
            var requestParams = Request.Params;
            var dbArgs = new Dapper.DynamicParameters();
            var obj = objectType.Replace("Type", "");

            var fields = getFieldTypesByObjectType(objectType, objectTypeID, listableOnly);

            dbArgs.Add("id", objectTypeID);

            if (extraParams != null)
            {
                foreach (var k in extraParams.Keys)
                {
                    dbArgs.Add(k, extraParams[k]);
                }
            }

            #region Field Joins

            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(objectTypeID, obj, out joins, out columns, includeIdColumn, useFriendlyName, listableOnly, fields);
            sql = string.Format(sql, columns, joins);

            #endregion

            // Ownership Joins
            joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups);

            // If simple filter specified add that criteria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                sql = $"{sql} and {addDynamicFieldSimpleFilter(new string[] { "A.Name", "A.Status", "T.Name", "P.TextPath" }, obj, objectTypeID, filter, dbArgs)}";
            }

            var querySql = $@"select * from ({sql}) A";
            var countSql = $@"select count(1) from ({sql}) A";

            #region Relation filtering

            var filters = applyRelationFilteringExistsRawSuffix(Request, dbArgs, fields);

            countSql += filters;
            querySql += filters;

            #endregion

            filters += applyFilteringSuffixBindRaw(Request, dbArgs, true, fields);  // Filtering

            countSql += filters;
            querySql += filters;

            querySql = applySortSuffix(querySql, sortDataField, sortOrder, isNumericString:isSortColumnNumber(sortDataField, fields));         // Sorting
            querySql = applyPagingSuffix(querySql, pagenum, pagesize);              // Paging

            countSql += " OPTION (RECOMPILE)";
            querySql += " OPTION (RECOMPILE)";

            int total = Company.Query<int>(countSql, dbArgs).First();
            var query = Company.Query<dynamic>(querySql, dbArgs);

            return new DynamicPagedResults { results = query, total = total };
        }

        protected bool isSortColumnNumber(string sortDataField, List<FieldType> fields)
        {
            if (string.IsNullOrEmpty(sortDataField)) return false;

            var field = fields.Where(x => string.Compare($"Field{x.ID}", sortDataField, true) == 0).FirstOrDefault();

            if (field == null) return false;

            return field.Type == "Number";
        }

        #endregion

        #region Private Methods

        internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFriendlyName = false, bool listableOnly = true, List<FieldType> fields = null)
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

            if (fields == null)
            {
                if(listableOnly)
                    fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();
                else
                    fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID).OrderBy(i => i.SortOrder).ToList();
            }

            foreach (var f in fields)
            {
                var name = $"Field{f.ID}";//f.Name.Replace("'", "''").Replace("--", "");
                var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");
                if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";
                columns += $@"case 
    when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
    when {name}_T.Value is not null then {name}_T.FormattedValue 
    when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
    else '' 
end as [{(useFriendlyName ? friendlyName : name)}], ";
                joins += $@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{fieldTypeRelationType}' and {name}_TT.ObjectID = {typeID} 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {name}_TT.ID ";
            }

            fields = null;
        }

        internal string addDynamicFieldSimpleFilter(string[] fixedColumns, string type, int typeID, string filterExp, Dapper.DynamicParameters dbArgs, List<FieldType> fields = null)
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
            if (fields == null)
            {
                fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();
            }

            StringBuilder sb = new StringBuilder();

            foreach (var column in fixedColumns)
            {
                if (sb.Length != 0) sb.Append(" or ");

                sb.Append($"({column} like @simpleFilter + '%')");
            }
            
            foreach (var field in fields)
            {
                if (sb.Length != 0) sb.Append(" or ");

                var name = $"Field{field.ID}_T.FormattedValue";
                
                sb.Append($"({name} like @simpleFilter + '%')");
            }

            var val = new Dapper.DbString { Value = filterExp.Replace('*','%').Replace('?','_'), Length = 200};

            dbArgs.Add("simpleFilter", val);

            return $"({sb.ToString()})";
        }

        internal List<FieldType> getDynamicFieldJoinStatements(int typeID, string type, List<string> filterFields, out string joins, out string filterjoins, out string columns, out string filtercolumns, bool includeIdColumn = true, bool useFriendlyName = false, List<FieldType> fields = null)
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

            if (fields == null)
            {
                fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.SortOrder).ToList();
            }

            foreach (var f in fields)
            {
                var name = $"Field{f.ID}"; //f.Name.Replace("'", "''").Replace("--", "");
                var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");

                if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";

                //var thisColumn = $", coalesce({name}_T.FormattedValue, {name}_TT.DefaultFormattedValue) as [{(useFriendlyName ? friendlyName : name)}]";
                var thisColumn = $@", case 
    when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
    when {name}_T.Value is not null then {name}_T.FormattedValue 
    when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
    else '' 
end as [{(useFriendlyName ? friendlyName : name)}]";

                var thisJoin = $@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{fieldTypeRelationType}' and {name}_TT.ObjectID = {typeID} and {name}_TT.IsListable = 1 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = A.ID and {name}_T.FieldTypeID = {name}_TT.ID ";

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

        internal string getFilteringConditionBind(string field, string condition, int filterNumber, Dapper.DynamicParameters dbParams, string value, string prefix, bool skipFieldValidation = false, FieldType ft = null)
        {
            var bind = $"{prefix}{filterNumber}val";
            var allItemsBind = "";
            var allValueBind = "";
            if (!skipFieldValidation)
            {
                if (!isValidFieldName(field)) return string.Empty; // sql injection check on field name
            }

            if (ft != null)
            {
                if (ft.AllowAllValue)
                {
                    allItemsBind = $"{prefix}{filterNumber}val_all";
                    allValueBind = $"{ft.AllowAllLabel.Replace("'", "''")}";
                }
            }

            var querySyntax = "";
            switch (condition)
            {
                case "CONTAINS":
                    var val = (value ?? "").Replace('*', '%').Replace('?', '_');
                    dbParams.Add(bind, $"%{val}%");
                    querySyntax = $"{field} LIKE @{bind}";
                    break;
                case "DOES_NOT_CONTAIN":
                    dbParams.Add(bind, $"%{value}%");
                    querySyntax = $"{field} NOT LIKE @{bind}";
                    break;
                case "EQUAL":
                    dbParams.Add(bind, $"{value}");
                    querySyntax = $"{field} = @{bind}";
                    break;
                case "NOT_EQUAL":
                    dbParams.Add(bind, $"{value}");
                    querySyntax = $"{field} <> @{bind}";
                    break;
                case "STARTS_WITH":
                    dbParams.Add(bind, $"{value}%");
                    querySyntax = $"{field} LIKE @{bind}";
                    break;
                case "ENDS_WITH":
                    dbParams.Add(bind, $"%{value}");
                    querySyntax = $"{field} LIKE @{bind}";
                    break;
                case "IN":                    
                    dbParams.Add(bind, value.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries));
                    querySyntax = $"{field} IN @{bind}";
                    break;
                //greater / less than cause issues with dates when casting...               
                /*case "GREATER_THAN":
                    dbParams.Add(bind, $"{value}");                    
                    querySyntax =  $"CAST({field} as numeric) > CAST(@{bind} as numeric)";
                    break;
                case "GREATER_THAN_OR_EQUAL":
                    dbParams.Add(bind, $"{value}");
                    querySyntax =  $"CAST({field} as numeric) >= CAST(@{bind} as numeric)";  
                    break;                  
                case "LESS_THAN":
                    dbParams.Add(bind, $"{value}");
                    querySyntax =  $"CAST({field} as numeric) < CAST(@{bind} as numeric)";  
                    break;                  
                case "LESS_THAN_OR_EQUAL":
                    dbParams.Add(bind, $"{value}");
                    querySyntax =  $"CAST({field} as numeric) <= CAST(@{bind} as numeric)"; 
                    break;                   */
                case "NULL":
                    querySyntax = $"{field} is null";
                    break;
                case "NOT_NULL":
                    querySyntax = $"{field} is not null";
                    break;
                case "EMPTY":
                    querySyntax = $"{field} = ''";
                    break;
                case "NOT_EMPTY":
                    querySyntax = $"{field} <> ''";
                    break;
                default:
                    dbParams.Add(bind, $"{value}");
                    querySyntax = $"{field} = @{bind}";
                    break;
            }

            if (!string.IsNullOrEmpty(allItemsBind) && !string.IsNullOrEmpty(allValueBind))
            {
                dbParams.Add(allItemsBind, $"{allValueBind}");
                querySyntax = $"({querySyntax} or {field} = @{allItemsBind})";
            }

            return querySyntax;
        }
        
        internal bool isValidFieldName(string field)
        {
            var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z][a-zA-Z0-9._-]+$");
            return nameRegex.IsMatch(field);
        }

        internal bool isValidUserProfileName(string name)
        {
            return !(name.Contains('>') || name.Contains('<') || name.Contains('"') || name.Contains('/') || name.Contains('\\'));
        }

        internal string applyRelationFilteringExists(string sql, HttpRequestBase Request, Dapper.DynamicParameters dbParams, List<FieldType> fields = null)
        {
            return sql + applyRelationFilteringExistsRawSuffix(Request, dbParams, fields);                
        }

        internal string applyRelationFilteringExistsRawSuffix(HttpRequestBase Request, Dapper.DynamicParameters dbParams, List<FieldType> fields = null)
        {
            var query = Request.Params;
            int filterscount = 0;

            var sb = new StringBuilder();

            if (int.TryParse(query["relfilterscount"], out filterscount) && filterscount > 0)
            {    
                for (var i = 0; i < filterscount; i++)
                {
                    var fFieldId = int.Parse(query["relfilterdatafield" + i]);
                    var fCondition = query["relfiltercondition" + i];
                    var fValue = query["relfiltervalue" + i];

                    FieldType filterFieldType = null;
                    if (fields != null)
                    {
                        filterFieldType = fields.FirstOrDefault(f => f.ID == fFieldId);
                    }

                    var filtersql = getFilteringConditionBind("relField.FormattedValue", fCondition, i, dbParams, fValue, "relflt", true, ft: filterFieldType);

                    if (string.IsNullOrEmpty(filtersql)) continue;

                    var existsql = @" and exists (select  B.sourceobjectid
                                from(
                                        select  IntersectID as ID,
                                                SourceObjectID
                                        from Relationship
                                        where SourceObjectType = 'Artifact'
                                                and SourceObjectID = A.ID
                                        ) B left join Field relField on (relField.ObjectType = 'Intersect' and relField.ObjectID = B.ID and relField.FieldTypeID = {0})
                                        where " + filtersql + ")";

                    existsql = string.Format(existsql, fFieldId);

                    sb.Append(existsql);
                }
            }

            return sb.ToString();
        }

        internal string applyHiddenFilteringSuffix(HttpRequestBase Request, Dapper.DynamicParameters dbParams)
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

        internal string applyFilteringSuffixBind(string sql, HttpRequestBase Request, Dapper.DynamicParameters dbParams, bool applyHiddenFilters = false, List<FieldType> fields = null)
        {
            return sql + applyFilteringSuffixBindRaw(Request, dbParams, applyHiddenFilters, fields);
        }

        internal string applyFilteringSuffixBindRaw(HttpRequestBase Request, Dapper.DynamicParameters dbParams, bool applyHiddenFilters = false, List<FieldType> fields = null)
        {
            var query = Request.Params;

            #region Field Filters

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

                    int fieldTypeID = 0;
                    FieldType filterFieldType = null;
                    if (fields != null)
                    {
                        if (fField.StartsWith("Field"))
                        {
                            string fieldTypeIDRaw = fField.Replace("Field", "");
                            if (int.TryParse(fieldTypeIDRaw, out fieldTypeID))
                            {
                                filterFieldType = fields.FirstOrDefault(f => f.ID == fieldTypeID);
                            }
                        }
                    }
                    filter = getFilteringConditionBind(fField, fCondition, i, dbParams, fValue, "", ft: filterFieldType);// "flt");

                    if (!string.IsNullOrEmpty(filter))
                    {
                        filters += (string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ";
                        filters += filter;
                    }
                }
            }

            #endregion

            #region Relationship Filters

            int relcount = 0;

            if (int.TryParse(query["relcount"], out relcount))
            {
                for (int i = 0; i < relcount; i++)
                {
                    var qs_includetype = $"rel_includetype_{i}";
                    var qs_object = $"rel_object_{i}";
                    var qs_objectids = $"rel_objectids_{i}";
                    var qs_typeid = $"rel_typeid_{i}";

                    //check form
                    var RelationshipIncludeType = Request.Form.AllKeys.Any(k => k == qs_includetype) ? Request[qs_includetype] : "";
                    var RelationshipObjectType = Request.Form.AllKeys.Any(k => k == qs_object) ? Request[qs_object] : "";
                    var RelationshipObjectIDs = Request.Form.AllKeys.Any(k => k == qs_objectids) ? Server.UrlDecode(Request[qs_objectids]) : "";
                    var RelationshipIntersectTypeID = Request.Form.AllKeys.Any(k => k == qs_typeid) ? Server.UrlDecode(Request[qs_typeid]) : "";

                    //check querystring
                    if (string.IsNullOrEmpty(RelationshipObjectIDs))
                    {
                        RelationshipIncludeType = query.AllKeys.Any(k => k == qs_includetype) ? query[qs_includetype] : "";
                        RelationshipObjectType = query.AllKeys.Any(k => k == qs_object) ? query[qs_object] : "";
                        RelationshipObjectIDs = query.AllKeys.Any(k => k == qs_objectids) ? Server.UrlDecode(query[qs_objectids]) : "";
                        RelationshipIntersectTypeID = query.AllKeys.Any(k => k == qs_typeid) ? Server.UrlDecode(query[qs_typeid]) : "";
                    }

                    if (!string.IsNullOrEmpty(RelationshipObjectIDs))
                    {
                        var IDs = RelationshipObjectIDs.Split(',').ToList();
                        var idList = "";
                        IDs.ForEach(ID =>
                        {
                            int idInt = 0;

                            if (int.TryParse(ID, out idInt)) //convert to integer to avoid sql injection
                                idList += (string.IsNullOrEmpty(idList) ? "" : ", ") + idInt;
                        });

                        dbParams.Add("relTypeAdvFlt", RelationshipObjectType); // use bind variable to avoid sql injection

                        if (RelationshipObjectType.ToUpper() == "MAP")
                        {
                            var subSql = $@"select a.ID from [Intersect] i
                                    inner join intersecttype it on (i.intersecttypeid = it.id)
                                    inner join[intersect] i_2 on(i_2.subject = 'Map' and i_2.subjectid = i.subjectid and i.subject = 'Map')
                                    inner join artifact a on(a.id = i_2.objectid and a.artifacttypeid = @id)
                                where i.intersecttypeid = {int.Parse(RelationshipIntersectTypeID)} and i.objectid in ({idList})";

                            filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + $"A.ID in ({subSql})";
                        }
                        else
                        {
                            if (RelationshipIncludeType == "Any")
                            {
                                filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + $@"A.ID in (
    select SubjectID from [Intersect] where Subject = 'Artifact' and Object = @relTypeAdvFlt and ObjectID in ({idList}) and IntersectTypeID = {int.Parse(RelationshipIntersectTypeID)} 
    union 
    select ObjectID from [Intersect] where Object = 'Artifact' and Subject = @relTypeAdvFlt and SubjectID in ({idList}) and IntersectTypeID = {int.Parse(RelationshipIntersectTypeID)} 
    )";
                            }
                            else
                            {
                                IDs.ForEach(ID =>
                                {
                                    int idInt = 0;
                                    if (int.TryParse(ID, out idInt)) //convert to integer to avoid sql injection
                                    {
                                        filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ");
                                        filters += $@"A.ID in (
    select SubjectID from [Intersect] where Subject = 'Artifact' and Object = @relTypeAdvFlt and ObjectID = {idInt} and IntersectTypeID = {int.Parse(RelationshipIntersectTypeID)} 
    union 
    select ObjectID from [Intersect] where Object = 'Artifact' and Subject = @relTypeAdvFlt and SubjectID = {idInt} and IntersectTypeID = {int.Parse(RelationshipIntersectTypeID)} 
    )";
                                    }
                                });
                            }
                        }
                    }
                }
            }

            #endregion

            #region Attribute Filters

            int attcount = 0;

            if (int.TryParse(query["attcount"], out attcount))
            {
                for (int i = 0; i < attcount; i++)
                {
                    var qs_value = $"att_value_{i}";
                    var qs_typeid = $"att_typeid_{i}";

                    var AttributeType = Request.Form.AllKeys.Any(k => k == qs_typeid) ? Request[qs_typeid] : "";
                    var AttributeSearchValue = Request.Form.AllKeys.Any(k => k == qs_value) ? Server.UrlDecode(Request[qs_value]) : "";

                    //check querystring
                    if (string.IsNullOrEmpty(AttributeType) || string.IsNullOrEmpty(AttributeSearchValue))
                    {
                        AttributeType = query.AllKeys.Any(k => k == qs_typeid) ? query[qs_typeid] : "";
                        AttributeSearchValue = query.AllKeys.Any(k => k == qs_value) ? Server.UrlDecode(query[qs_value]) : "";
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
                }
            }

            #endregion

            return filters;
        }


        internal string applySortSuffix(string sql, string sortDataField, string sortOrder, string sortDefaultField = "Name", string sortDefaultDirection = "asc", bool isNumericString = false)
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
                //return sql;
                throw new Exception("Invalid sort field specified");
            }

            if(isNumericString)
                sql += " ORDER BY CAST(+ [" + sortDataField + "] AS int)" + sortOrder;
            else
                sql += " ORDER BY [" + sortDataField + "] " + sortOrder;

            return sql;
        }

        internal string applyPagingSuffix(string sql, int pagenum = 0, int pagesize = 20)
        {
            sql += string.Format(" OFFSET({0}) ROWS FETCH NEXT ({1}) ROWS ONLY", pagenum * pagesize, pagesize);

            return sql;
        }

        protected void ResetResourcePassword(int resourceId, string firstName, string email, string fullName)
        {
            var generatedPassword = System.Web.Security.Membership.GeneratePassword(10, 3);

            Community.ChangePassword(resourceId, "", generatedPassword);

            var templateValues = new Dictionary<string, string>();

            string strUrl = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, "/");

            templateValues["firstname"] = firstName;
            templateValues["password"] = generatedPassword;
            templateValues["request_url"] = strUrl;

            //email user 
            extensions.mail.TemplateMessage.SendMessage("Data3Sixty Password Reset", email, fullName, templateValues, "forms-password-reset");
        }

        #endregion
    }
}