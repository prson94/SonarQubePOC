using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.web.Models;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Resources;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
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

    public class BaseApiController : System.Web.Http.ApiController
    {
        internal ICompanyContext Company;
        internal ICommunityContext Community;

        internal List<string> CalculatedFieldTypes = new List<string>() { DataType.Attribute.ToString(), DataType.ComplexRelationLookup.ToString(), DataType.DataTableSelect.ToString(), DataType.File.ToString(), DataType.FilteredLookup.ToString(), DataType.OwnershipLookup.ToString() };

        internal const int MAX_SYNCHRONOUS_API_ITEM_COUNT = 250;

        public BaseApiController(ICommunityContext community, ICompanyContext company)
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
        
        #region Error Handling Helper

        [System.Runtime.Serialization.DataContract(Name = "Error")]
        public class GenericHttpError
        {
            public string Message { get; set; }
            public HttpStatusCode Code { get; set; }
        }

        internal HttpResponseMessage ReturnApiError(HttpStatusCode status, string message)
        {
            var acceptHeaders = Request.Headers.Accept;
            var asJson = !acceptHeaders.Any(i => i.MediaType == "application/xml");
            return Request.CreateResponse<GenericHttpError>(status, new GenericHttpError { Code = status, Message = message }, asJson ? "application/json" : "application/xml");
        }

        #endregion

        internal void SendException(Exception ex, IDictionary<string, string> properties, IDictionary<string, double> metrics = null)
        {
            var telemetry = new TelemetryClient();
            if (!properties.ContainsKey("CompanyID")) properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackException(ex, properties, metrics);
            telemetry = null;
        }

        internal System.Web.Http.IHttpActionResult errorMessageResponse(HttpStatusCode status, string title, string message)
        {            
            return ResponseMessage(
                Request.CreateResponse(
                    status,
                    new ErrorResponse { title = title, message = message }
                )
            );
        }

        internal System.Web.Http.IHttpActionResult successMessageResponse(HttpStatusCode status, string title, string message)
        {
            return ResponseMessage(
                Request.CreateResponse(
                    status,
                    new ConfirmResponse { title = title, message = message }
                )
            );
        }

        internal void SendEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (properties == null) properties = new Dictionary<string, string>();
            var telemetry = new TelemetryClient();
            if(!properties.ContainsKey("CompanyID")) properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackEvent(eventName, properties, metrics);
            telemetry = null;
        }

        #region Private Methods

        internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFieldName = true, bool checkForListable = true, bool checkForKeyColumn = false, string coreTableIdJoinColumn = "A.ID", string nameColumnOverride = "", bool enableRelationFields = true)
        {
            Company.getDynamicFieldJoinStatements(typeID, type, out joins, out columns, includeIdColumn, useFieldName, checkForListable, null, coreTableIdJoinColumn,false, enableRelationFields);            
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

        internal bool isValidFieldName(string field)
        {
            var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z][a-zA-Z0-9._-]+$");
            return nameRegex.IsMatch(field);
        }

        internal string applySortSuffix(string sql, System.Net.Http.HttpRequestMessage Request, string sortDefaultField = "Name", string sortOrder = "asc", string sortFieldType = "string")
        {
            string sortDataField = "";
            
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

            // make sure its a valid field name
            if (!isValidFieldName(sortDataField))
            {
                throw new Exception("Invalid sort field specified");
            }

            if ((sortFieldType ?? "").ToUpper() == "NUMBER")
                sql += " ORDER BY TRY_CAST(+ [" + sortDataField + "] AS bigint)" + sortOrder;
            else if ((sortFieldType ?? "").ToUpper() == "DATE")
                sql += " ORDER BY TRY_CAST(+ [" + sortDataField + "] AS date)" + sortOrder;
            else if ((sortFieldType ?? "").ToUpper() == "DATETIME")
                sql += " ORDER BY TRY_CAST(+ [" + sortDataField + "] AS datetime)" + sortOrder;
            else
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

        internal ApiExecution getApiExecution(int total = 0, object fields = null, int error = 0, int processed = 0)
        {

            var execution = new ApiExecution
            {
                ExecutionID = Guid.NewGuid(),
                StartedOn = DateTime.UtcNow,
                Route = Request?.RequestUri?.LocalPath,
                Method =  Request?.Method?.Method,
                ResourceID = Company.CurrentCompanyID,
                Total = total,
                Fields = fields == null ? "" : JsonConvert.SerializeObject(fields),
                Error = error,
                Processed = processed
            };

            return execution;
        }
        #endregion
    }
        
    public class BaseController: Controller
    {
        internal ICompanyContext Company;
        internal ICommunityContext Community;

        internal List<string> limitedFieldTypes = new List<string> {
            DataType.Attribute.ToString(),
            DataType.FilteredLookup.ToString(),
            DataType.ComplexRelationLookup.ToString(),
            DataType.FieldFromRelationship.ToString(),
            DataType.DataTableSelect.ToString(),
            DataType.OwnershipLookup.ToString(),
            DataType.RefListRelationship.ToString()
        };

        public BaseController(ICommunityContext community, ICompanyContext company)
        {
            Community = community;
            Company = company;
        }

        #region Json Message Handling

        internal JsonNetResult jsonNetException(Exception ex, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return new JsonNetResult { Data = new { type = "error", title = title, message = ex.GetFullExceptionData() }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        internal JsonResult jsonException(Exception ex, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return Json(new { type = "error", title = title, message = ex.GetFullExceptionData() }, JsonRequestBehavior.AllowGet);
        }

        internal JsonResult jsonException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return Json(new { type = "error", title = title, message = message }, JsonRequestBehavior.AllowGet);
        }

        internal JsonNetResult jsonNetException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return new JsonNetResult
            {
                Data = new { type = "error", title = title, message = message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        internal JsonResult jsonSuccess(string message, string id, string action, HttpStatusCode statusCode, dynamic customdata = null)
        {
            Response.StatusCode = (int)statusCode;
            Response.StatusDescription = message.Replace("\n", "  ");
            return Json(new { type = "confirm", title = "Success!", action = action, message = message.Replace("\n", "  "), id = id, custom = customdata }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        internal string GetNoReadSqlStatement(string identifier = null)
        {
            return $"select AssetID from ResponsibilityDetail where ((PermissionsBitMask & {(int)Permission.ReadAsset}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? Company.CurrentResourceID.ToString() : identifier)}";
        }

        internal string GetAssetTypeNoReadSqlStatement(string identifier = null)
        {
            return $"select AssetTypeID from ResponsibilityDetail where AssetID = 0 and ((PermissionsBitMask & {(int)Permission.ReadAsset}) = 0) and ResourceID = {(string.IsNullOrEmpty(identifier) ? Company.CurrentResourceID.ToString() : identifier)}";
        }

        internal List<FieldValidationModel> checkAndAddValidation(string fieldType, string friendlyName, bool required, string pattern, decimal? minLength, decimal? maxLength, string validationMessage = "", decimal? Increment = null, int? Precision = null)
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
                    models.Add(new FieldValidationModel { message = string.Format(Validation.Required_Tokenized, friendlyName), rule = "required" });
                }

                // Pattern validation
                if (!string.IsNullOrEmpty(pattern))
                {
                    models.Add(new FieldValidationModel { message = validationMessage, regex = pattern });
                }
                //Increment validation 
                if (Increment.HasValue)
                {
                    models.Add(new FieldValidationModel { message = validationMessage, rule = string.Format("increment={0}", Increment.Value) });
                }
                //Precision for decimals
                if (Precision.HasValue)
                {
                    models.Add(new FieldValidationModel { message = validationMessage, rule = string.Format("precision={0}", Precision.Value) });
                }

                // Min/Max next precedent
                if (maxLength.HasValue && minLength.HasValue)
                {
                    models.Add(new FieldValidationModel { message = string.Format(Validation.Length_Tokenized, friendlyName, minLength.Value, maxLength.Value), rule = string.Format("length={0},{1}", minLength.Value, maxLength.Value) });
                }
                // Min next precedent
                else if (minLength.HasValue)
                {
                    models.Add(new FieldValidationModel { message = string.Format(Validation.MaxLength_Tokenized, friendlyName, minLength.Value), rule = string.Format("minLength={0}", minLength.Value) });
                }
                // Max next precedent
                else if (maxLength.HasValue)
                {
                    models.Add(new FieldValidationModel { message = string.Format(Validation.MinLength_Tokenized, friendlyName, maxLength.Value), rule = string.Format("maxLength={0}", maxLength.Value) });
                }
            }

            #endregion

            return models.Count > 0 ? models : null;
        }

        internal IQueryable<GlobalReportingResource> GetCompanyResources()
        {
            var hideData3SixtyUsers = HideData3SixtyUsers();
            var query = Company.Table<GlobalReportingResource>();
            return (hideData3SixtyUsers ? query.Where(i => !i.Email.Contains("data3sixty.com")) : query);
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

        internal bool ShowAllUsersAPIKey()
        {
            var showAllUsersAPIKey = false;
            var settings = Community.GetCompanySettings();
            if (settings.Any(i => i.Key == "ShowAllUsersAPIKey"))
            {
                showAllUsersAPIKey = bool.Parse(settings["ShowAllUsersAPIKey"]);
            }
            return showAllUsersAPIKey;
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

                    if (!limitedFieldTypes.Contains(f.Type))
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
                            Validations = checkAndAddValidation(f.Type.ToString(), f.FriendlyName, f.IsRequired, f.Pattern, f.MinimumLength, f.MaximumLength, patternMessage, f.Increment, f.Precision),
                            Category = f.Category,
                            FieldTypeID = f.ID
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

                        if (f.Type == DataType.Lookup.ToString() && !string.IsNullOrEmpty(f.LookupObjectType))
                        {
                            fld.FieldType = DataType.Lookup.ToString();
                            try
                            {
                                fld.MultiSelect = f.AllowMultipleValues;
                                fld.ParentFieldTypeID = f.ParentFieldTypeID;

                                fld.Items = new List<SelectListItem>();

                                if (f.ParentFieldTypeID > 0)
                                {
                                    var parent = Company.FieldTypes.Where(x => x.ID == f.ParentFieldTypeID).FirstOrDefault();

                                    if (parent != null) fld.ParentFieldTypeName = parent.FriendlyName;
                                }
                                else if (f.FilterFieldTypeID > 0 || f.FilterPredicateID > 0)
                                {
                                    if(f.FilterFieldTypeID > 0)
                                    {
                                        fld.DelayedLoadType = "FieldFilter";
                                        //Field filter works similar to ParentFieldType, so we'll overload those parameters
                                        var filterParent = Company.FieldTypes.Where(x => x.ID == f.FilterFieldTypeID).FirstOrDefault();
                                        if (filterParent != null)
                                        {
                                            fld.ParentFieldTypeID = f.FilterFieldTypeID;
                                            fld.ParentFieldTypeName = filterParent.FriendlyName;
                                        }
                                    }
                                    else
                                    {
                                        fld.DelayedLoadType = "Predicate";
                                    }
                                }
                                else
                                {
                                    if (!f.IsRequired && !f.AllowMultipleValues)
                                        fld.Items.Add(new SelectListItem { Text = "Choose...", Value = "" });

                                    if (f.AllowAllValue)
                                        fld.Items.Add(new SelectListItem { Text = f.AllowAllLabel, Value = "0" });

                                    bool hideData3SixtyUsers = HideData3SixtyUsers();
                                    var columns = $@"
                                        V.FieldTypeID,
                                        V.LookupObjectType,
                                        V.LookupObjectID,
                                        V.Value,
                                        V.Text";

                                    var hideData3SixtyUsersCondition = $@" and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com'";

                                    var resourceJoin = $@"
                                        inner join reporting.Global_resource R on R.ResourceID = V.Value and R.State <> 3 {(hideData3SixtyUsers ? hideData3SixtyUsersCondition : "")}
                                        ";

                                    var itemSql = $@"select {columns} 
                                        from FieldLookupValue V
                                        {(f.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId
                                        ";

                                    var countSql = $@"select count(*)
                                        from FieldLookupValue V
                                        {(f.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId
                                        ";

                                    if (f.AllowMultipleValues)
                                    {
                                        var items = Company.Query<FieldLookupValue>(itemSql, new { fieldTypeId = f.ID, lookupObjectType = f.LookupObjectType, lookupObjectId = f.LookupObjectID })
                                            .OrderBy(o => o.Text)
                                            .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                            .ToList();

                                        fld.Items.AddRange(items);
                                    }
                                    else
                                    {
                                        int maxItems = int.Parse(Community.GetCompanySettings()["MaxDropdownItems"]);
                                        int count = Company.Query<int>(countSql, new { fieldTypeId = f.ID, lookupObjectType = f.LookupObjectType, lookupObjectId = f.LookupObjectID }).FirstOrDefault();

                                        if (count > maxItems)
                                        {
                                            fld.UseTypeahead = true;
                                            if (!string.IsNullOrWhiteSpace(f.DefaultValue) && int.TryParse(f.DefaultValue, out int selectedVal))
                                            {
                                                fld.Value = f.DefaultValue;
                                                fld.Items.AddRange(
                                                 Company.Filter<FieldLookupValue>(o => o.FieldTypeID == f.ID && o.LookupObjectType == f.LookupObjectType && o.LookupObjectID == f.LookupObjectID.Value && o.Value == selectedVal)
                                                .OrderBy(o => o.Text)
                                                .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = true })
                                                .ToList()
                                                    );
                                            }
                                        }
                                        else
                                        {
                                            var items = Company.Query<FieldLookupValue>(itemSql, new { fieldTypeId = f.ID, lookupObjectType = f.LookupObjectType, lookupObjectId = f.LookupObjectID })
                                                .OrderBy(o => o.Text)
                                                .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                                .ToList();

                                            fld.Items.AddRange(items);
                                        }
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                fld.Items.Add(new SelectListItem { Text = "Error while rendering lookup field type.", Value = "" });
                                SendException(ex);
                            }
                        }

                        if (f.Type == DataType.Relationship.ToString() && !string.IsNullOrEmpty(f.LookupObjectType))
                        {
                            var intersectType = Company.GetById<IntersectType>(f.LookupObjectID.Value);
                            if (intersectType != null)
                            {
                                bool isSubject = (intersectType.Subject == f.Object && intersectType.SubjectID == f.ObjectID);
                                
                                
                                var cardinality = isSubject ? intersectType.ObjectCardinality : intersectType.SubjectCardinality;

                                if (cardinality != Cardinality.Many)
                                    fld.MultiSelect = false;
                                else
                                    fld.MultiSelect = true;

                                var result = Company.GetRelationshipFieldItems(f.ID);
                                fld.Value = JsonConvert.SerializeObject(((List<dynamic>)result["Selection"]).Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = i.Selected == 1 ? true : false }).ToArray());
                                fld.RecordCount = (int)result["Count"];
                            }
                        }

                        if (f.Type == DataType.Lookup.ToString())  // lookups dont set min / length properties
                            fld.Required = (f.MinimumLength > 0 || f.Length > 0 || f.IsRequired);
                        else
                        {
                            if (!new[] { "Number", "Decimal",  "Text" }.Contains(f.Type))
                            {
                                fld.Required = (f.MinimumLength > 0 || f.Length > 0);
                            }
                        }

                        list.Add(fld);
                    }

                    #endregion Is Editable
                }
                row++;
            });

            return list;
        }

        internal List<EditableField> loadDynamicFields(string @object, int objectID, List<EditableField> list, List<FieldType> fieldTypes, List<FieldWithRelation> fields, int startRow = 10, bool decode = false)
        {
            var row = startRow;

            fieldTypes.ForEach(ft =>
            {
                if (ft.IsEditable)
                {
                    #region Is Editable

                    if (!limitedFieldTypes.Contains(ft.Type))
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
                            Validations = checkAndAddValidation(ft.Type.ToString(), ft.FriendlyName, ft.IsRequired, ft.Pattern, ft.MinimumLength, ft.MaximumLength, patternMessage, ft.Increment, ft.Precision),
                            Category = ft.Category,
                            FieldTypeID = ft.ID
                        };

                        #region FusionLookup

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

                        #endregion FusionLookup

                        #region Lookup

                        if (ft.Type == DataType.Lookup.ToString() && !string.IsNullOrEmpty(ft.LookupObjectType))
                        {
                            try
                            {
                                fld.Items = new List<SelectListItem>();

                                fld.ParentFieldTypeID = ft.ParentFieldTypeID;
                                fld.MultiSelect = ft.AllowMultipleValues;


                                if (ft.ParentFieldTypeID > 0)
                                {
                                    var parent = Company.FieldTypes.Where(x => x.ID == ft.ParentFieldTypeID).FirstOrDefault();

                                    if (parent != null) fld.ParentFieldTypeName = parent.FriendlyName;

                                    if (ft.AllowMultipleValues)
                                    {
                                        if (f != null && !string.IsNullOrWhiteSpace(f.Value))
                                            fld.Value = f.Value;
                                        else if (!string.IsNullOrWhiteSpace(ft.DefaultValue))
                                            fld.Value = ft.DefaultValue;
                                    }
                                }
                                else if (ft.FilterFieldTypeID > 0 || ft.FilterPredicateID > 0)
                                {
                                    if (ft.FilterFieldTypeID > 0)
                                    {
                                        fld.DelayedLoadType = "FieldFilter";
                                        //Field filter works similar to ParentFieldType, so we'll overload those parameters
                                        var filterParent = Company.FieldTypes.Where(x => x.ID == ft.FilterFieldTypeID).FirstOrDefault();
                                        if (filterParent != null)
                                        {
                                            fld.ParentFieldTypeID = ft.FilterFieldTypeID;
                                            fld.ParentFieldTypeName = filterParent.FriendlyName;
                                        }
                                    }
                                    else
                                    {
                                        fld.DelayedLoadType = "Predicate";
                                    }
                                    if (ft.AllowMultipleValues && f != null && !string.IsNullOrWhiteSpace(f.Value))
                                        fld.Value = f.Value;
                                }
                                else
                                {
                                    if (!ft.IsRequired)
                                        fld.Items.Add(new SelectListItem { Text = "Choose...", Value = "" });

                                    if (ft.AllowAllValue)
                                        fld.Items.Add(new SelectListItem { Text = ft.AllowAllLabel, Value = "0" });

                                    var items = new List<SelectListItem>();
                                    bool hideData3SixtyUsers = HideData3SixtyUsers();

                                    var columns = $@"
                                        V.FieldTypeID,
                                        V.LookupObjectType,
                                        V.LookupObjectID,
                                        V.Value,
                                        V.Text";

                                    var hideData3SixtyUsersCondition = $@" and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com'";

                                    var resourceJoin = $@"
                                        inner join reporting.Global_resource R on R.ResourceID = V.Value and R.State <> 3 {(hideData3SixtyUsers ? hideData3SixtyUsersCondition : "")}
                                        ";

                                    var itemSql = $@"select {columns} 
                                        from FieldLookupValue V
                                        {(ft.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId
                                        ";

                                    var countSql = $@"select count(*)
                                        from FieldLookupValue V
                                        {(ft.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId
                                        ";

                                    if (ft.AllowMultipleValues)
                                    {
                                        items = Company.Query<FieldLookupValue>(itemSql, new { fieldTypeId = ft.ID, lookupObjectType = ft.LookupObjectType, lookupObjectId = ft.LookupObjectID.Value})
                                            .OrderBy(o => o.Text)
                                            .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                            .ToList();

                                        var selected = new List<string>();
                                        // selected items need to go into multiplevalues array
                                        if (f != null && !string.IsNullOrWhiteSpace(f.Value))
                                            selected = f.Value.Split(',').ToList();
                                        else if (!string.IsNullOrWhiteSpace(ft.DefaultValue))
                                            selected = ft.DefaultValue.Split(',').ToList();

                                        if (ft.AllowAllValue && selected.Contains("0"))
                                        {
                                            var all = fld.Items.Where(x => x.Value == "0").FirstOrDefault();
                                            all.Selected = true;
                                        }

                                        foreach (var item in items)
                                        {
                                            if (selected.Contains(item.Value)) item.Selected = true;
                                        }
                                    }
                                    else
                                    {
                                        int maxItems = int.Parse(Community.GetCompanySettings()["MaxDropdownItems"]);
                                        int count = Company.Query<int>(countSql, new { fieldTypeId = ft.ID, lookupObjectType = ft.LookupObjectType, lookupObjectId = ft.LookupObjectID }).FirstOrDefault();
                                        
                                        string selectedValue = null;
                                        if (f != null && !string.IsNullOrWhiteSpace(f.Value))
                                            selectedValue = f.Value;
                                        else if (!string.IsNullOrWhiteSpace(ft.DefaultValue))
                                            selectedValue = ft.DefaultValue;

                                        List<SelectListItem> selected = null;

                                        if (count > maxItems)
                                        {
                                            fld.UseTypeahead = true;
                                            if (!string.IsNullOrWhiteSpace(selectedValue) && selectedValue != null && int.TryParse(selectedValue, out var selectedValueInt))
                                            {
                                                selected = Company.FieldLookupValues.Where(i => i.FieldTypeID == ft.ID && i.LookupObjectType == ft.LookupObjectType && i.LookupObjectID == ft.LookupObjectID && i.Value == selectedValueInt)
                                                .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = true })
                                                .ToList();
                                            }
                                            items = selected;
                                        }
                                        else
                                        {
                                            fld.UseTypeahead = false;

                                            items = Company.Query<FieldLookupValue>(itemSql, new { fieldTypeId = ft.ID, lookupObjectType = ft.LookupObjectType, lookupObjectId = ft.LookupObjectID.Value })
                                                .OrderBy(o => o.Text)
                                                .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                                .ToList();
                                        }

                                    }

                                    if (items != null) // missing null check causes exception if items is null GOV-6041
                                    {
                                        fld.Items.AddRange(
                                            items
                                        );
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                fld.Items.Add(new SelectListItem { Text = "Error while rendering lookup field type.", Value = "" });

                                SendException(ex);
                            }
                        }

                        #endregion Lookup

                        #region Relationship

                        if (ft.Type == DataType.Relationship.ToString() && !string.IsNullOrEmpty(ft.LookupObjectType))
                        {
                            var intersectType = Company.GetById<IntersectType>(ft.LookupObjectID.Value);
                            if (intersectType != null)
                            {
                                bool isSubject = (intersectType.Subject == ft.Object && intersectType.SubjectID == ft.ObjectID);
                                var cardinality = isSubject ? intersectType.ObjectCardinality : intersectType.SubjectCardinality;

                                if (cardinality != Cardinality.Many)
                                    fld.MultiSelect = false;
                                else
                                    fld.MultiSelect = true;

                                var result = Company.GetRelationshipFieldItems(ft.ID, @object, objectID);

                                fld.Value = JsonConvert.SerializeObject(((List<dynamic>)result["Selection"]).Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = i.Selected == 1 ? true : false }).ToArray());
                                fld.RecordCount = (int)result["Count"];
                            }
                        }

                        #endregion Relationship

                        if (ft.Type == DataType.Lookup.ToString())
                            fld.Required = (ft.MinimumLength > 0 || ft.Length > 0 || ft.IsRequired);
                        else
                            fld.Required = (ft.MinimumLength > 0 || ft.Length > 0);



                        if (!ft.AllowMultipleValues)
                        {
                            if (f != null) fld.Value = decode ? Server.HtmlDecode(f.Value) : f.Value;
                            if (f == null && !string.IsNullOrEmpty(ft.DefaultValue))
                            {
                                fld.Value = ft.DefaultValue;
                            }
                        }

                        list.Add(fld);

                        row++;
                    }

                    #endregion Is Editable
                }
            });

            return list;
        }

        protected string sortColumnType(string sortDataField, List<FieldType> fields)
        {
            if (string.IsNullOrEmpty(sortDataField)) return "";

            var field = fields.Where(x => string.Compare($"Field{x.ID}", sortDataField, true) == 0).FirstOrDefault();

            if (field == null) return "";

            return field.Type;
        }

        internal void processFormDynamicRelationshipFields(SystemObjects ot, int otid, SystemObjects o, int oid, ICollection<FieldType> fieldTypes, FormCollection form)
        {
            foreach (var ft in fieldTypes)
            {
                if (ft.Type == DataType.Relationship.ToString() && ft.IsEditable)
                {
                    var value = form[ft.Name];
                    List<int> items = new List<int>();
                        var intersectType = Company.GetById<IntersectType>(ft.LookupObjectID.Value);
                        if (intersectType != null)
                        {
                            var isSubject = (intersectType.Subject == ot.ToString() && intersectType.SubjectID == otid);
                            if(!string.IsNullOrEmpty(value))
                                items =  value.Trim(' ', ',').Split(',').Select<string, int>(int.Parse).ToList();
                            //delete any intersects for this object not in the list
                            List<Intersect> intersects = null;
                            if (isSubject)
                            {
                                intersects = Company.Filter<Intersect>(i => i.IntersectTypeID == intersectType.ID && i.Subject == o.ToString() && i.SubjectID == oid).ToList();
                                foreach (var intersect in intersects)
                                {
                                    //check if the object is in the value list if not delete the intersect
                                    if(!items.Contains(intersect.ObjectID))
                                    {
                                        Company.Delete<Intersect>(intersect);
                                    }
                                }
                            }
                            else
                            {
                                intersects = Company.Filter<Intersect>(i => i.IntersectTypeID == intersectType.ID && i.Object == o.ToString() && i.ObjectID == oid).ToList();
                                foreach (var intersect in intersects)
                                {
                                    //check if the object is in the value list if not delete the intersect
                                    if (!items.Contains(intersect.SubjectID))
                                    {
                                        Company.Delete<Intersect>(intersect);
                                    }
                                }
                            }

                        if (!string.IsNullOrEmpty(value))
                        {

                            //add / update the rest

                            foreach (var val in items)
                            {
                                var obj = "";
                                var sub = "";
                                var objID = 0;
                                var subID = 0;

                                Intersect intersect = null;

                                if (isSubject)
                                {
                                    sub = o.ToString();
                                    subID = oid;
                                    obj = intersectType.Object;
                                    obj = (obj == "ReferenceItemType" && intersectType.ObjectID == 0) ? obj : obj.Replace("Type", "");
                                    objID = val;

                                    intersect = Company.Filter<Intersect>(i => i.IntersectTypeID == intersectType.ID && i.Subject == sub && i.SubjectID == subID && i.ObjectID == val).FirstOrDefault();
                                }
                                else
                                {
                                    obj = o.ToString();
                                    objID = oid;
                                    sub = intersectType.Subject;
                                    sub = (sub == "ReferenceItemType" && intersectType.SubjectID == 0) ? sub : sub.Replace("Type", "");
                                    subID = val;

                                    intersect = Company.Filter<Intersect>(i => i.IntersectTypeID == intersectType.ID && i.Object == obj && i.ObjectID == objID && i.SubjectID == val).FirstOrDefault();
                                }

                                if (intersect != null)
                                {
                                    if (isSubject)
                                    {
                                        intersect.Object = obj;
                                        intersect.ObjectID = objID;
                                    }
                                    else
                                    {
                                        intersect.Subject = sub;
                                        intersect.SubjectID = subID;
                                    }
                                    Company.Update(intersect);
                                }
                                else
                                {
                                    intersect = new Intersect { IntersectTypeID = intersectType.ID, Object = obj, ObjectID = objID, Subject = sub, SubjectID = subID };
                                    Company.Add(intersect);
                                }
                            }
                        }
                    }
                }
            }
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
            if (!properties.ContainsKey("CompanyID")) properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackException(ex, properties, metrics);
            telemetry = null;
        }

        internal void SendEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (properties == null) properties = new Dictionary<string, string>();
            var telemetry = new TelemetryClient();
            if(!properties.ContainsKey("CompanyID")) properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackEvent(eventName, properties, metrics);
            telemetry = null;
        }

        #region Dynamic Query Processing

        public class DynamicPagedResults
        {
            public int total { get; set; }
            public IEnumerable<dynamic> results { get; set; }
        }

        internal string addOwnershipJoinCriteria(string joins, string ownerUsers, string ownerGroups, string idColumn = "A.ID")
        {
            int index = 0;
            if (!string.IsNullOrEmpty(ownerUsers))
            {
                foreach (var user in ownerUsers.Split(','))
                {
                    var ids = user.Split('|');
                    if (ids.Length == 2)
                    {
                        joins += $" inner join ResponsibilityDetail RD{index} on (RD{index}.AssetID = {idColumn} and RD{index}.SecurityAsset = 'R' and RD{index}.SecurityAssetID = {int.Parse(ids[1])} and RD{index}.ResponsibilityTypeID = {int.Parse(ids[0])} )";
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
                        joins += $" inner join ResponsibilityDetail RD{index} on (RD{index}.AssetID = {idColumn} and RD{index}.SecurityAsset = 'G' and RD{index}.SecurityAssetID = {int.Parse(ids[1])} and RD{index}.ResponsibilityTypeID = {int.Parse(ids[0])})";
                        index++;
                    }
                }
            }

            return joins;
        }

        internal List<FieldType> getFieldTypesByObjectType(string objectType, int objectTypeID, bool listableOnly)
        {
            return (listableOnly) ?
                Company.Filter<FieldType>(i => i.Object == objectType && i.ObjectID == objectTypeID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList() :
                Company.Filter<FieldType>(i => i.Object == objectType && i.ObjectID == objectTypeID).OrderBy(i => i.ColumnOrder).ToList();
        }

        internal DynamicPagedResults processDynamicResults(
            string sql,
            HttpRequestBase Request,
            string objectType, int objectTypeID,
            bool listableOnly, string sortDataField, string sortOrder, int pagenum, int pagesize,
            string[] staticFields,
            string filter = "", string ownerUsers = "", string ownerGroups = "",
            string sortDefaultField = "DisplayValue", string sortDefaultDirection = "asc",
            Dictionary<string, object> extraParams = null,
            bool includeIdColumn = true, bool useFriendlyName = false, bool fetchPermissions = false, string idColumn = "A.ID", string innerIdColumn = "A.ID")
        {            
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

            var joins = "";
            var columns = "";

            // Field Joins
            getDynamicFieldJoinStatements(objectTypeID, obj, out joins, out columns, includeIdColumn, useFriendlyName, listableOnly, fields, innerIdColumn);

            // Ownership Joins
            joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups, innerIdColumn);

            if (fetchPermissions)
            {
                if (Company.CurrentResourceIsAdmin)
                {
                    columns += "1 P_CanEdit, 1 P_CanDelete,";
                }
                else
                {
                    columns += "IIF(S_E.AssetID is null, 0, 1) as P_CanEdit, IIF(S_D.AssetID is null, 0, 1) as P_CanDelete, ";
                    joins += $@"
outer apply (select top 1 AssetID from ResponsibilityDetail where AssetID = A.ID and ResourceID = {Company.CurrentResourceID} and (PermissionsBitMask & {(int)Permission.ModifyAsset}) = {(int)Permission.ModifyAsset}) S_E 
outer apply (select top 1 AssetID from ResponsibilityDetail where AssetID = A.ID and ResourceID = {Company.CurrentResourceID} and (PermissionsBitMask & {(int)Permission.DeleteAsset}) = {(int)Permission.DeleteAsset}) S_D ";
                }
            }

            sql = string.Format(sql, columns, joins);

            // If simple filter specified add that criteria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                sql = $"{sql} and {addDynamicFieldSimpleFilter(staticFields, obj, objectTypeID, filter, dbArgs, fields)}";                
            }

            var querySql = $@"select * from ({sql}) A";
            var countSql = $@"select count(1) from ({sql}) A";

            #region Relation filtering

            var filters = applyRelationFilteringExistsRawSuffix(Request, dbArgs, fields, idColumn);

            countSql += filters;
            querySql += filters;

            #endregion

            filters += applyFilteringSuffixBindRaw(Request, dbArgs, true, fields, idColumn);  // Filtering

            countSql += filters;
            querySql += filters;

            #region Sorting

            if (string.IsNullOrEmpty(sortDataField))
            {
                var sortSql = "";
                
                foreach (var field in fields.Where(i => i.SortOrder > 0).OrderBy(i => i.SortOrder))
                {
                    var columnName = useFriendlyName ? field.FriendlyName.Replace("[", "").Replace("]", "") : $"Field{field.ID}";
                    switch (field.Type)
                    {
                        case "Number":
                            sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST(+ [{columnName}] AS bigint)";
                            break;
                        case "Date":
                            sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST(+ [{columnName}] AS date)";
                            break;
                        default:
                            sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"[{columnName}]";
                            break;
                    }
                }

                if (string.IsNullOrEmpty(sortSql))
                {
                    sortSql = sortDefaultField;
                }

                querySql += " ORDER BY " + sortSql;
            }
            else
            {
                //The user sorted by something else, other than the default SortOrder settings on the FieldTypes.
                querySql = applySortSuffix(querySql, sortDataField, sortOrder, sortDefaultField, sortDefaultDirection, sortFieldType: sortColumnType(sortDataField, fields));         // Sorting
            }

            #endregion

            querySql = applyPagingSuffix(querySql, pagenum, pagesize);              // Paging
                        
            int total = Company.Query<int>(countSql, dbArgs).First();
            var query = Company.Query<dynamic>(querySql, dbArgs);

            return new DynamicPagedResults { results = query, total = total };
        }

        #endregion

        #region Private Methods


        internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFriendlyName = false, bool listableOnly = true, List<FieldType> fields = null, string idColumn = "A.ID")
        {
            Company.getDynamicFieldJoinStatements(typeID, type, out joins, out columns, includeIdColumn, useFriendlyName, listableOnly, fields, idColumn);
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
                fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList();
            }

            StringBuilder sb = new StringBuilder();

            foreach (var column in fixedColumns)
            {
                if (sb.Length != 0) sb.Append(" or ");

                sb.Append($"({column} like @simpleFilter + '%')");
            }

            var relationFieldInfos = Company.getRelationFieldData(fieldTypeRelationType, typeID, fields);

            foreach (var field in fields)
            {
                if (sb.Length != 0) sb.Append(" or ");

                if (field.Type == DataType.Relationship.ToString())
                {
                    var relationFieldInfo = relationFieldInfos.FirstOrDefault(i => i.FieldTypeID == field.ID);
                    var columnName = "DisplayValue";
                    if (relationFieldInfo != null)
                    {
                        if (relationFieldInfo.Object == SystemObjects.ReferenceItemType.ToString())
                        {
                            columnName = "Name";
                        }
                    }

                        sb.Append($"(Field{field.ID}_OT.{columnName} like @simpleFilter + '%')");
                }
                else if (field.Type == DataType.FieldFromRelationship.ToString())
                {
                    var columnName = "FormattedValue";
                    sb.Append($"(Field{field.ID}_OT.{columnName} like @simpleFilter + '%')");
                }
                else
                {
                    if (field.Name.ToLower() == "highproductrisk" && filterExp.ToLower() == "yes")
                    {
                        //do nothing, LMTOM-specific. Yes means Yes AND No
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(field.DefaultFormattedValue))
                            sb.Append($"(coalesce(Field{field.ID}_T.FormattedValue, '{field.DefaultFormattedValue}') like @simpleFilter + '%')");
                        else
                            sb.Append($"(Field{field.ID}_T.FormattedValue like @simpleFilter + '%')");
                    }
                    
                }
            }

            var val = new Dapper.DbString { Value = filterExp.Replace('*','%').Replace('?','_'), Length = 200};

            dbArgs.Add("simpleFilter", val);

            return $"({sb.ToString()})";
        }

        internal List<FieldType> getDynamicFieldJoinStatements(int typeID, string type, List<string> filterFields, out string joins, out string filterjoins, out string columns, out string filtercolumns, bool includeIdColumn = true, bool useFriendlyName = false, List<FieldType> fields = null, bool showSubsetColumns = false, List<int> subsetColumns = null, string idColumn = "A.ID")
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
                fields = Company.Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList();
            }

            foreach (var f in fields)
            {
                var name = $"Field{f.ID}";
                var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");
                var thisColumn = $@", case 
    when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
    when {name}_T.Value is not null then {name}_T.FormattedValue 
    when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
    else '' 
end as [{(useFriendlyName ? friendlyName : name)}]";

                if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";
                columns += thisColumn;

                var thisJoin = $@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{fieldTypeRelationType}' and {name}_TT.ObjectID = {typeID} and {name}_TT.IsListable = 1 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = {idColumn} and {name}_T.FieldTypeID = {name}_TT.ID ";
                
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

                if (ft.AllowMultipleValues)
                {
                    if (condition == "IN")
                        condition = "IN_MULTI";
                    else
                        condition = "CONTAINS";
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
                case "IN_MULTI":
                    var vals = value.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries);
                    int index = 0;
                    querySyntax = "(";
                    foreach (var part in vals)
                    {
                        if (index != 0) querySyntax += " or ";
                        var bind_sub = $"{bind}{index++}";
                        dbParams.Add(bind_sub, $"%{part}%");                        
                        querySyntax += $"{field} LIKE @{bind_sub}";
                    }
                    querySyntax += ")";

                    break;
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

            if (field.ToLower() == "field50102" && value.ToLower() == "yes") //field50102 = highproductrisk
            {
                //do nothing, this is LMTOM-specific. Need to deal with this some other way. No means No, Yes means YES and NO.
                querySyntax = "";
            }
            else
            {
                if (!string.IsNullOrEmpty(allItemsBind) && !string.IsNullOrEmpty(allValueBind))
                {
                    dbParams.Add(allItemsBind, $"{allValueBind}");
                    querySyntax = $"({querySyntax} or {field} = @{allItemsBind})";
                }
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

        internal string applyRelationFilteringExists(string sql, HttpRequestBase Request, Dapper.DynamicParameters dbParams, List<FieldType> fields = null, string idColumn = "A.ID")
        {
            return sql + applyRelationFilteringExistsRawSuffix(Request, dbParams, fields, idColumn);
        }

        internal string applyRelationFilteringExistsRawSuffix(HttpRequestBase Request, Dapper.DynamicParameters dbParams, List<FieldType> fields = null, string idColumn = "A.ID")
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
                                                and SourceObjectID = {idColumn}
                                        ) B left join Field relField on (relField.ObjectType = 'Intersect' and relField.ObjectID = B.ID and relField.FieldTypeID = {0})
                                        where " + filtersql + ")";

                    existsql = string.Format(existsql, fFieldId);

                    sb.Append(existsql);
                }
            }

            return sb.ToString();
        }

        internal string applyHiddenFilteringSuffix(HttpRequestBase Request, Dapper.DynamicParameters dbParams, string idColumn = "A.ID", List<FieldType> fields = null)
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

                    var tableId = $"hidft{i}";

                    var fieldType = fields.Where(x => x.ID == fieldID).SingleOrDefault();
                    if (fieldType != null && fieldType.AllowMultipleValues)
                        filter= applyMulitSelectFilteringSuffix(dbParams, fValue, tableId, i, fieldType, idColumn);
                    else
                        filter = $" inner join field {tableId} on ({idColumn} = {tableId}.objectID and {tableId}.ObjectType = 'Artifact'  and {tableId}.fieldtypeid={fieldID} and {getFilteringConditionBind(tableId +".FormattedValue", fCondition, i, dbParams, fValue, tableId, true)} )  ";
                    
                    if (!string.IsNullOrEmpty(filter))
                    {                        
                        filters += filter;
                    }
                }
            }

            return filters;
        }

        private string applyMulitSelectFilteringSuffix(Dapper.DynamicParameters dbParams, string value, string prefix, int filterNumber,FieldType fieldType, string idColumn = "A.ID")
        {
            value = value.Replace("!~!", ",");

            if (fieldType.AllowAllValue)
                value += ",0";
            var bind = $"{prefix}{filterNumber}val";
            dbParams.Add(bind, $"{value}");

            var filter = $@"			inner join ( 
			select F.objectID, F.ObjectType, F.FieldTypeID, dd.value as Value, F.[Value] as Val from Field F with (NOLOCK) 
			cross apply string_split(F.Value,',') dd 
			where F.FieldTypeID = {fieldType.ID} 
			and exists (SELECT value  
			FROM STRING_SPLIT(@{bind}, ',')  WHERE RTRIM(value)=dd.value) 
			)  {prefix}  on   {prefix}.objectID={idColumn} and {prefix}.ObjectType = 'Artifact' ";

            return filter;
        }

        internal string applyFilteringSuffixBind(string sql, HttpRequestBase Request, Dapper.DynamicParameters dbParams, bool applyHiddenFilters = false, List<FieldType> fields = null)
        {
            return sql + applyFilteringSuffixBindRaw(Request, dbParams, applyHiddenFilters, fields);
        }

        internal List<UiRequestFilterValue> GetFilterValuesFromRequest(HttpRequestBase Request, bool applyHiddenFilters = false)
        {
            var query = Request.Params;
            var filters = new List<UiRequestFilterValue>();

            int relfilterscount = 0;

            #region Hidden Filters

            if (applyHiddenFilters)
            {
                if (int.TryParse(query["hidfilterscount"], out relfilterscount))
                {
                    for (int i = 0; i < relfilterscount; i++)
                    {
                        var fField = query["hidfilterdatafield" + i];
                        var fCondition = query["hidfiltercondition" + i];
                        var fValue = query["hidfiltervalue" + i];

                        if (!string.IsNullOrEmpty(fValue))
                        {
                            filters.Add(new UiRequestFieldFilterValue
                            {
                                IsUnlistedFilterField = true,
                                Condition = fCondition,
                                FieldName = fField,
                                RawValue = fValue
                            });
                        }
                    }
                }

                relfilterscount = 0; // Reset
            }

            #endregion

            #region Field Filters

            if (int.TryParse(query["filterscount"], out relfilterscount))
            {
                for (int i = 0; i < relfilterscount; i++)
                {
                    var fField = query["filterdatafield" + i];
                    var fCondition = query["filtercondition" + i];
                    var fValue = query["filtervalue" + i];

                    if (fValue.EndsWith(".000")) fValue = fValue.Replace(".000", "");

                    if (!string.IsNullOrEmpty(fValue))
                    {
                        filters.Add(new UiRequestFieldFilterValue
                        {
                            Condition = fCondition,
                            FieldName = fField,
                            RawValue = fValue
                        });
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
                        var rawIDs = RelationshipObjectIDs.Split(',').ToList();
                        var IDs = new List<int>();
                        rawIDs.ForEach(ID =>
                        {
                            int idInt = 0;

                            if (int.TryParse(ID, out idInt)) //convert to integer to avoid sql injection
                                IDs.Add(idInt);
                        });

                        filters.Add(new UiRequestRelationshipFilterValue
                        {
                            IntersectTypeID = int.Parse(RelationshipIntersectTypeID),
                            TargetObjectIDs = IDs,
                            Operator = (RelationshipIncludeType == "Any") ? "OR" : "AND",
                            TargetObject = RelationshipObjectType
                        });
                    }
                }
            }

            if (int.TryParse(query["relfilterscount"], out  relfilterscount) && relfilterscount > 0)
            {
                for (var i = 0; i < relfilterscount; i++)
                {

                    if (int.TryParse(query["relfilterdatafield" + i], out int fieldTypeId))
                    {
                        filters.Add(new UiRequestRelationshipFieldFilterValue
                        {
                            FieldTypeID = fieldTypeId,
                            Condition = query.AllKeys.Any(k => k == $"relfiltercondition{i}") ? query[$"relfiltercondition{i}"] : "",
                            Value = query.AllKeys.Any(k => k == $"relfiltervalue{i}")  ? query[$"relfiltervalue{i}"] : ""
                        });
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
                            filters.Add(new UiRequestAttributeFilterValue
                            {
                                AttributeTypeID = attributeTypeID,
                                RawValue = AttributeSearchValue
                            });
                        }
                    }
                }
            }

            #endregion

            #region Ownership Filters

            string ownerUsers = query["ownerUsers"];
            string ownerGroups = query["ownerGroups"];

            var userFilterEnabled = !string.IsNullOrEmpty(ownerUsers);
            var groupFilterEnabled = !string.IsNullOrEmpty(ownerGroups);

            if (userFilterEnabled || groupFilterEnabled)
            {
                var ownershipFilter = new UiRequestOwnershipFilterValue();

                if (groupFilterEnabled)
                {
                    foreach (var group in ownerGroups.Split(','))
                    {
                        var ids = group.Split('|');
                        if (ids.Length == 2)
                        {
                            ownershipFilter.Items.Add(new UiRequestOwnershipFilterItem {
                                FilterType = UiRequestOwnershipFilterType.Group,
                                ResponsibilityTypeID = int.Parse(ids[0]),
                                SecurityAssetID = int.Parse(ids[1])
                            });
                        }
                    }
                }

                if (userFilterEnabled)
                {
                    foreach (var user in ownerUsers.Split(','))
                    {
                        var ids = user.Split('|');
                        if (ids.Length == 2)
                        {
                            ownershipFilter.Items.Add(new UiRequestOwnershipFilterItem {
                                FilterType = UiRequestOwnershipFilterType.User,
                                ResponsibilityTypeID = int.Parse(ids[0]),
                                SecurityAssetID = int.Parse(ids[1])
                            });
                        }
                    }
                }

                if (ownershipFilter.Items.Count > 0)
                {
                    filters.Add(ownershipFilter);
                }
            }

            #endregion

            return filters;
        }

        internal string applyFilteringSuffixBindRaw(HttpRequestBase Request, Dapper.DynamicParameters dbParams, bool applyHiddenFilters = false, List<FieldType> fields = null, string idColumn = "A.ID")
        {
            var query = Request.Params;

            #region Field Filters

            int filterscount = 0;
            var filters = applyHiddenFilters ? applyHiddenFilteringSuffix(Request, dbParams, idColumn, fields) : string.Empty;
            var whereFilter = string.Empty;
            if (int.TryParse(query["filterscount"], out filterscount))
            {
                for (int i = 0; i < filterscount; i++)
                {
                    var filter = "";
                    var fField = query["filterdatafield" + i];
                    var fCondition = query["filtercondition" + i];
                    var fValue = query["filtervalue" + i];
                    var tableId = $"fieldft{i}";

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
                    if(filterFieldType.AllowMultipleValues)
                        filters +=  applyMulitSelectFilteringSuffix(dbParams, fValue, tableId, i, filterFieldType, idColumn);
                   else
                        filter = getFilteringConditionBind(fField, fCondition, i, dbParams, fValue, "", ft: filterFieldType);// "flt");

                    if (!string.IsNullOrEmpty(filter))
                    {
                        whereFilter += (i == 0) ? " WHERE " : " AND ";
                        whereFilter += filter;
                    }
                }
            }
             filters += whereFilter;
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

                            filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + $"{idColumn} in ({subSql})";
                        }
                        else
                        {
                            if (RelationshipIncludeType == "Any")
                            {
                                filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + $@"{idColumn} in (
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
                                        filters += $@"{idColumn} in (
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

                            filters += ((string.IsNullOrEmpty(filters)) ? " WHERE " : " AND ") + @"{idColumn} in (
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

        internal string applySortSuffix(string sql, string sortDataField, string sortOrder, string sortDefaultField = "Name", string sortDefaultDirection = "asc", string sortFieldType = "string")
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
                throw new Exception("Invalid sort field specified");
            }

            if ((sortFieldType ?? "").ToUpper() == "NUMBER")
                sql += " ORDER BY CAST(+ [" + sortDataField + "] AS bigint)" + sortOrder;
            else if ((sortFieldType ?? "").ToUpper() == "DATE")
                sql += " ORDER BY TRY_CAST(+ [" + sortDataField + "] AS date)" + sortOrder;
            else if ((sortFieldType ?? "").ToUpper() == "DATETIME")
                sql += " ORDER BY TRY_CAST(+ [" + sortDataField + "] AS datetime)" + sortOrder;
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

        protected bool IsSingleSignOn()
        {
            var c = Community.GetById<Company>(Company.CurrentCompanyID, i => i.CompanyDomainSettings);

            foreach (var companySetting in c.CompanyDomainSettings)
            {
                if (Company.CurrentCompanyDomain == companySetting.UrlPrefix)
                {
                    return !(companySetting.AuthenticationType == AuthenticationType.Forms);
                    
                }
            }

            return false;
        }
    }
}