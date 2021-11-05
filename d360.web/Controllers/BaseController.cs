using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.helpers;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.utils.excel;
using d360.web.Models;
using Dapper;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

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
            {
                throw new ArgumentNullException("context");
            }

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
        internal ISettingsRepository SettingsRepository;

        internal List<string> CalculatedFieldTypes = DataType.Text.GetComputedFields();

        internal const int MAX_SYNCHRONOUS_API_ITEM_COUNT = 250;

        #region Validation constants

        internal const string NOT_AUTHORIZED_MESSAGE = "You are not authorized to perform this action.";
        internal const string CONFLICT_MESSAGE = "Encountered a data conflict between your request and Govern.";
        internal const string NOT_FOUND_GENERIC_MESSAGE = "The item you are looking for cannot be located.";
        internal const string BAD_REQUEST_GENERIC_MESSAGE = "Error while processing request.";
        internal const string INTERNAL_ERROR_MESSAGE = "An unknown error occurred while processing this request.";
        internal const string UNKNOWN_ERROR_MESSAGE = "An unknown error occurred.";

        #endregion

        #region Parameter Description Constants

        internal const string SIMPLE_FILTER_DESCRIPTION = "The text or phrase you want to find within the data set. Filtering is done using 'Starts with' logic.";
        internal const string ADVANCED_FILTER_DESCRIPTION = "The filter expression used to filter assets by all listable and non-listable fields. Asterisk (*) symbol can be used as a wild card character to match any character.";
        internal const string PAGE_SIZE_DESCRIPTION = "The number of results to return per page. The default value is 200. Maximum is 250.";
        internal const string PAGE_NUMBER_DESCRIPTION = "The page number to return results for.";

        #endregion

        public BaseApiController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository)
        {
            Community = community;
            Company = company;
            SettingsRepository = settingsRepository;
        }

        protected internal bool HideData3SixtyUsers()
        {
            return SettingsRepository.GetSettingValue<bool>(Setting.HideData3SixtyUsers);
        }

        protected internal IQueryable<Resource> GetCompanyResources()
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

        protected internal HttpResponseMessage ReturnApiError(HttpStatusCode status, string message)
        {
            var acceptHeaders = Request.Headers.Accept;
            var asJson = !acceptHeaders.Any(i => i.MediaType == "application/xml");
            return Request.CreateResponse<GenericHttpError>(status, new GenericHttpError { Code = status, Message = message }, asJson ? "application/json" : "application/xml");
        }

        public class StatusCodeErrorMessage 
        {
            public HttpStatusCode Status { get; set; }
            public string ErrorMessage { get; set; }
        }

        #endregion

        protected internal IHttpActionResult DetermineUnhandledException(Exception ex, string errorHeading, List<StatusCodeErrorMessage> errorMessages, Dictionary<string, string> methodProperties)
        {
            if (errorMessages == null)
            {
                errorMessages = new List<StatusCodeErrorMessage>();
            }

            if (ex is ConflictException && errorMessages.Any(e => e.Status == HttpStatusCode.Conflict))
            {
                return errorMessageResponse((ex as ConflictException).StatusCode, errorHeading, errorMessages.First(e => e.Status == HttpStatusCode.Conflict).ErrorMessage);
            }
            else if (ex is NotFoundException && errorMessages.Any(e => e.Status == (ex as NotFoundException).StatusCode))
            {
                return errorMessageResponse((ex as NotFoundException).StatusCode, errorHeading, errorMessages.First(e => e.Status == (ex as NotFoundException).StatusCode).ErrorMessage);
            }
            else if (ex is StatusCodeException && errorMessages.Any(e => e.Status == (ex as StatusCodeException).StatusCode))
            {
                return errorMessageResponse((ex as StatusCodeException).StatusCode, errorHeading, errorMessages.First(e => e.Status == (ex as StatusCodeException).StatusCode).ErrorMessage);
            }
            else if (ex is GenericException)
            {
                return errorMessageResponse((ex as GenericException).StatusCode, errorHeading, (ex as GenericException).StatusDescription);
            }
            else
            { 
                if (ex.Message.ToLower().Contains("invalid filter expression"))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, errorHeading, $"{ApiMessages.InvalidFilterExpressionUsed}{ex.Message.Replace(ApiMessages.InvalidFilterExpression, "")}");
                }
                else if (ex.Message.ToLower().Contains("conversion failed when converting from"))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, errorHeading, ApiMessages.InvalidFilterExpressionUsedMessage);
                }
                else
                {
                    SendException(ex, methodProperties);
                    return errorMessageResponse(HttpStatusCode.InternalServerError, errorHeading, ApiMessages.UnknownErrorInvestigatingMessage);
                }
            }
        }

        protected internal void SendException(Exception ex, IDictionary<string, string> properties, IDictionary<string, double> metrics = null)
        {
            var telemetry = new TelemetryClient();
            if (!properties.ContainsKey("CompanyID"))
            {
                properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            }
            telemetry.TrackException(ex, properties, metrics);
        }

        protected internal System.Web.Http.IHttpActionResult errorMessageResponse(HttpStatusCode status, string title, string message)
        {
            return ResponseMessage(
                Request.CreateResponse(
                    status,
                    new ErrorResponse { title = title, message = message }
                )
            );
        }

        protected internal System.Web.Http.IHttpActionResult successMessageResponse(HttpStatusCode status, string title, string message)
        {
            return ResponseMessage(
                Request.CreateResponse(
                    status,
                    new ConfirmResponse { title = title, message = message }
                )
            );
        }

        protected internal void SendEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }
            var telemetry = new TelemetryClient();
            if (!properties.ContainsKey("CompanyID"))
            {
                properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            }
            telemetry.TrackEvent(eventName, properties, metrics);
        }

        protected internal HttpResponseMessage createFileResponseMessage(HttpStatusCode status, string fileName, byte[] content)
        {
            var response = Request.CreateResponse(status);
            response.Content = new ByteArrayContent(content);
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = fileName;
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            return response;

        }


        #region Private Methods

        protected internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFieldName = true, bool checkForListable = true, bool checkForKeyColumn = false, string coreTableIdJoinColumn = "A.ID", string nameColumnOverride = "", bool enableRelationFields = true)
        {
            Company.getDynamicFieldJoinStatements(typeID, type, out joins, out columns, includeIdColumn, useFieldName, checkForListable, null, coreTableIdJoinColumn, false, enableRelationFields, checkForKeyColumn);
        }

        protected internal string applyFilteringSuffix(string sql, System.Net.Http.HttpRequestMessage Request)
        {
            var query = Request.GetQueryStrings();

            int filterscount = 0;
            var filters = new StringBuilder();

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
                                default:
                                    fFormat = "";
                                    break;
                            }

                            filter = string.Format(fFormat, fField, fValue.Replace("--", "").Replace("'", "''"));   //SQL Injection check

                            if (!string.IsNullOrEmpty(filter))
                            {
                                filters.Append(filters.Length > 0 ? " WHERE " : " AND ");
                                filters.Append(filter);
                            }
                        }
                    }

                    sql += filters;
                }
            }


            return sql;
        }

        protected internal bool isValidFieldName(string field)
        {
            var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z][a-zA-Z0-9._-]+$");
            return nameRegex.IsMatch(field);
        }

        protected internal string applySortSuffix(string sql, System.Net.Http.HttpRequestMessage Request, string sortDefaultField = "Name", string sortOrder = "asc", string sortFieldType = "string")
        {
            string sortDataField = "";
            string _sortOrder = sortOrder;
            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append(sql);
            var query = Request.GetQueryStrings();

            if (query.ContainsKey("sortDataField"))
            {
                sortDataField = query["sortDataField"];
            }
            if (query.ContainsKey("sortOrder"))
            {
                _sortOrder = query["sortOrder"];
            }



            if (string.IsNullOrEmpty(sortDataField))
            {
                sortDataField = sortDefaultField;
            }

            // make sure its a valid field name
            if (!isValidFieldName(sortDataField))
            {
                throw new ArgumentException(ApiMessages.InvalidSortField);
            }

            if ((sortFieldType ?? "").ToUpperInvariant() == "NUMBER")
            {
                sqlBuilder.Append($" ORDER BY TRY_CAST(+ [{sortDataField}] AS bigint) {_sortOrder}");
            }
            else if ((sortFieldType ?? "").ToUpperInvariant() == "DATE")
            {
                sqlBuilder.Append($" ORDER BY TRY_CAST(+ [{sortDataField}] AS date) {_sortOrder}");
            }
            else if ((sortFieldType ?? "").ToUpperInvariant() == "DATETIME")
            {
                sqlBuilder.Append($" ORDER BY TRY_CAST(+ [{sortDataField}] AS datetime) {_sortOrder}");
            }
            else
            {
                sqlBuilder.Append(" ORDER BY [" + sortDataField + "] " + _sortOrder);
            }


            return sqlBuilder.ToString();
        }

        protected internal string applyPagingSuffix(string sql, System.Net.Http.HttpRequestMessage Request)
        {
            int pagenum = 0;
            int pagesize = 20;

            var query = Request.GetQueryStrings();

            if (query.ContainsKey("pagenum"))
            {
                pagenum = int.Parse(query["pagenum"]);
            }
            if (query.ContainsKey("pagesize"))
            {
                pagesize = int.Parse(query["pagesize"]);
            }

            sql += string.Format(" OFFSET({0}) ROWS FETCH NEXT ({1}) ROWS ONLY", pagenum * pagesize, pagesize);

            return sql;
        }

        protected internal ApiExecution getApiExecution(int total = 0, object fields = null, int error = 0, int processed = 0)
        {

            var execution = new ApiExecution
            {
                ExecutionID = Guid.NewGuid(),
                StartedOn = DateTime.UtcNow,
                Route = Request?.RequestUri?.LocalPath,
                Method = Request?.Method?.Method,
                ResourceID = Company.CurrentResourceID,
                Total = total,
                Fields = fields == null ? "" : JsonConvert.SerializeObject(fields),
                Error = error,
                Processed = processed
            };

            return execution;
        }
        #endregion
    }

    public class BaseController : Controller
    {
        internal ICompanyContext Company;
        internal ICommunityContext Community;
        internal IMailProvider Mail;
        internal ISettingsRepository SettingsRepository;

        internal List<string> limitedFieldTypes = new List<string> {
            DataType.Path.ToString(),
            DataType.ComplexRelationLookup.ToString(),
            DataType.FieldFromRelationship.ToString(),
            DataType.DataTableSelect.ToString(),
            DataType.OwnershipLookup.ToString(),
            DataType.RefListRelationship.ToString(),
            DataType.JsonElement.ToString(),
            DataType.Tag.ToString(),
            DataType.JSON.ToString(),
            DataType.Score.ToString(),
            DataType.Counter.ToString()
        };

        public BaseController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository)
        {
            Community = community;
            Company = company;
            SettingsRepository = settingsRepository;
        }

        #region Validation constants

        internal const string UNKNOWN_ERROR_MESSAGE = "An unknown error occurred.";

        #endregion

        #region Json Message Handling

        internal JsonNetResult jsonNetException(Exception ex, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return new JsonNetResult { Data = new { type = "error", title, message = ex.GetFullExceptionData() }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        internal JsonResult jsonException(Exception ex, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return Json(new { type = "error", title, message = ex.GetFullExceptionData() }, JsonRequestBehavior.AllowGet);
        }

        internal JsonResult jsonException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return Json(new { type = "error", title, message }, JsonRequestBehavior.AllowGet);
        }

        internal JsonNetResult jsonNetException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return new JsonNetResult
            {
                Data = new { type = "error", title, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        internal JsonNetResult jsonNetException(Exception ex)
        {
            return new JsonNetResult
            {
                Data = new { type = "error", title = "Error Occurred!", message = ex.GetFullExceptionData() },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        internal JsonResult jsonSuccess(string message, string id, string action, HttpStatusCode statusCode, dynamic customdata = null)
        {
            Response.StatusCode = (int)statusCode;
            Response.StatusDescription = message.Replace("\n", "  ");
            return Json(new { type = "confirm", title = "Success!", action, message = message.Replace("\n", "  "), id, custom = customdata }, JsonRequestBehavior.AllowGet);
        }

        internal JsonNetResult jsonNetResult(dynamic data)
        {
            return new JsonNetResult { Data = data, Formatting = Formatting.None };
        }

        /// <summary>
        /// Used to override default JSON return type for MVC controllers overrides maxJsonLength which doesnt get picked up from web.config for mvc endpoints
        /// do not remove or JSON responses will be limited to default (102400 bytes)
        /// </summary>        
        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = Int32.MaxValue
            };
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
                    if(fieldType == "Number")
                    {
                        validationMessage = string.Format(Validation.Pattern_Tokenized, friendlyName, "must be a whole number");
                    }
                    if(fieldType == "Decimal")
                    {
                        validationMessage = string.Format(Validation.Pattern_Tokenized, friendlyName, "must be a decimal number");
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
            return SettingsRepository.GetSettingValue<bool>(Setting.HideData3SixtyUsers);
        }

        internal bool ShowAllUsersAPIKey()
        {
            return SettingsRepository.GetSettingValue<bool>(Setting.ShowAllUsersAPIKey);
        }

        internal List<EditableField> loadDynamicFields(List<EditableField> list, List<FieldType> fields, int startRow = 10, bool useDefaultCategory = true)
        {
            var row = startRow;
            const string defaultCategoryName = "General";

            fields.ForEach(f =>
            {
                var categoryName = f.Category;
                if (useDefaultCategory && string.IsNullOrWhiteSpace(categoryName))
                {
                    categoryName = defaultCategoryName;
                }

                if (f.IsEditable && f.Type != "Tag")
                {
                    #region Is Editable

                    if (!limitedFieldTypes.Contains(f.Type))
                    {
                        var patternMessage = "";

                        if (string.IsNullOrEmpty(f.ValidationDescription))
                        {
                            if (f.Type == "Number")
                            {
                                patternMessage = "must be a whole number";
                            }
                            if (f.Type == "Decimal")
                            {
                                patternMessage = "must be a decimal number";
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
                            Category = categoryName,
                            FieldTypeID = f.ID,
                            IsPartOfKey = f.IsPartOfKey
                        };

                        if (!string.IsNullOrEmpty(f.DefaultValue))
                        {
                            fld.Value = f.DefaultValue;
                        }

                        if (f.Type == DataType.Lookup.ToString() && !string.IsNullOrEmpty(f.LookupObjectType))
                        {
                            fld.FieldType = DataType.Lookup.ToString();
                            try
                            {
                                fld.MultiSelect = f.AllowMultipleValues;
                                fld.ParentFieldTypeID = f.ParentFieldTypeID;
                                var lookupType = f.LookupObjectType == "ReferenceItem" ? "ReferenceItemType" : f.LookupObjectType;
                                fld.UseColorControl = Company.Assets.Any(x => x.Color != null && x.AssetType.Object == lookupType && f.LookupObjectID == x.AssetType.ObjectID);

                                fld.Items = new List<SelectListItem>();

                                if (f.ParentFieldTypeID > 0)
                                {
                                    var parent = Company.FieldTypes.Where(x => x.ID == f.ParentFieldTypeID).FirstOrDefault();

                                    if (parent != null)
                                    {
                                        fld.ParentFieldTypeName = parent.FriendlyName;
                                    }
                                }
                                else if (f.FilterFieldTypeID > 0 || f.FilterPredicateID > 0)
                                {
                                    if (f.FilterFieldTypeID > 0)
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
                                    {
                                        fld.Items.Add(new SelectListItem { Text = "Choose...", Value = "" });
                                    }

                                    if (f.AllowAllValue)
                                    {
                                        fld.Items.Add(new SelectListItem { Text = f.AllowAllLabel, Value = "0" });
                                    }

                                    bool hideData3SixtyUsers = HideData3SixtyUsers();
                                    var columns = $@"
                                        V.FieldTypeID,
                                        V.LookupObjectType,
                                        V.LookupObjectID,
                                        V.Value,
                                        {(fld.UseColorControl ? "colorJson.FV AS Text" : "V.Text")}";

                                    var hideData3SixtyUsersCondition = $@" and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com'";

                                    var resourceJoin = $@"
                                        inner join reporting.Global_resource R on R.ResourceID = V.Value and R.State <> 3 {(hideData3SixtyUsers ? hideData3SixtyUsersCondition : "")}
                                        ";
                                    var colorjoin = $@"
                                        outer apply(SELECT FV = (SELECT V.Text as name, COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color 
                                                    from Asset A 
                                                    outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
													where A.Object = v.LookupObjectType and A.ObjectID = V.Value FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) 
                                        )colorJSON 
                                        ";
                                    var itemSql = $@"select {columns} 
                                        from FieldLookupValue V
                                        {(f.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        {(fld.UseColorControl ? colorjoin : "")}
                                        where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId
                                        ";

                                    var countSql = $@"select count(*)
                                        from FieldLookupValue V
                                        {(f.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        {(fld.UseColorControl ? colorjoin : "")}
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
                                        int maxItems = SettingsRepository.GetSettingValue<int>(Setting.MaxDropdownItems);
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
                            catch (Exception ex)
                            {
                                fld.Items.Add(new SelectListItem { Text = "Error while rendering lookup field type.", Value = "" });
                                SendException(ex);
                            }
                        }

                        if (f.Type == DataType.Relationship.ToString() && !string.IsNullOrEmpty(f.LookupObjectType))
                        {
                            var sql = @"select
                                            [ID],
                                            [Subject],
                                            [SubjectID],
                                            [SubjectCardinality],
                                            [Object],
                                            [ObjectID],
                                            [ObjectCardinality],
                                            [PredicateID] from [dbo].[intersecttype] where ID = @ID";
                            var intersectType = Company.Database.Connection.QueryFirstOrDefault<IntersectType>(sql, new { ID = f.LookupObjectID.Value });
                            if (intersectType != null)
                            {
                                bool isSubject = (intersectType.Subject == f.Object && intersectType.SubjectID == f.ObjectID);


                                var cardinality = isSubject ? intersectType.ObjectCardinality : intersectType.SubjectCardinality;

                                if (cardinality != Cardinality.Many)
                                {
                                    fld.MultiSelect = false;
                                }
                                else
                                {
                                    fld.MultiSelect = true;
                                }

                                Predicate predicate = null;
                                if (intersectType.PredicateID.HasValue)
                                {
                                    predicate = Company.GetById<Predicate>((int)intersectType.PredicateID);
                                    if (predicate != null && predicate.Type.AsInfoModel().SingleRelationshipByFunctionalType)
                                    {
                                        fld.IsSemantic = true;
                                    }
                                }
                            }
                        }

                        if (f.Type == DataType.Lookup.ToString())  // lookups dont set min / length properties
                        {
                            fld.Required = (f.MinimumLength > 0 || f.Length > 0 || f.IsRequired);
                        }
                        else
                        {
                            if (!new[] { "Number", "Decimal", "Text" }.Contains(f.Type))
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

        internal List<EditableField> loadDynamicFields(string @object, int objectID, List<EditableField> list, List<FieldType> fieldTypes, List<FieldWithRelation> fields, int startRow = 10, bool decode = false, bool useDefaultCategory = true)
        {
            var row = startRow;
            const string defaultCategoryName = "General";

            fieldTypes.ForEach(ft =>
            {
                var categoryName = ft.Category;
                if (useDefaultCategory && string.IsNullOrWhiteSpace(categoryName))
                {
                    categoryName = defaultCategoryName;
                }

                if (ft.IsEditable && ft.Type != "Tag")
                {
                    #region Is Editable

                    if (!limitedFieldTypes.Contains(ft.Type))
                    {
                        var f = fields.SingleOrDefault(i => i.FieldTypeID == ft.ID);

                        var patternMessage = "";

                        if (string.IsNullOrEmpty(ft.ValidationDescription))
                        {
                            if (ft.Type == "Number")
                            {
                                patternMessage = "must be a whole number";
                            }
                            if (ft.Type == "Decimal")
                            {
                                patternMessage = "must be a decimal number";
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
                            Category = categoryName,
                            FieldTypeID = ft.ID,
                            IsPartOfKey = ft.IsPartOfKey
                        };

                        #region Lookup

                        if (ft.Type == DataType.Lookup.ToString() && !string.IsNullOrEmpty(ft.LookupObjectType))
                        {
                            try
                            {
                                fld.Items = new List<SelectListItem>();
                                fld.ParentFieldTypeID = ft.ParentFieldTypeID;
                                fld.MultiSelect = ft.AllowMultipleValues;
                                var lookupType = ft.LookupObjectType == "ReferenceItem" ? "ReferenceItemType" : ft.LookupObjectType;
                                fld.UseColorControl = Company.Assets.Any(x => x.Color != null && x.AssetType.Object == lookupType && ft.LookupObjectID == x.AssetType.ObjectID);

                                if (ft.ParentFieldTypeID > 0)
                                {
                                    var parent = Company.FieldTypes.Where(x => x.ID == ft.ParentFieldTypeID).FirstOrDefault();

                                    if (parent != null) fld.ParentFieldTypeName = parent.FriendlyName;

                                    if (ft.AllowMultipleValues)
                                    {
                                        if (f != null && !string.IsNullOrWhiteSpace(f.Value))
                                        {
                                            fld.Value = f.Value;
                                        }
                                        else if (!string.IsNullOrWhiteSpace(ft.DefaultValue))
                                        {
                                            fld.Value = ft.DefaultValue;
                                        }
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
                                    {
                                        fld.Value = f.Value;
                                    }
                                }
                                else
                                {
                                    if (!ft.IsRequired && !ft.AllowMultipleValues)
                                    {
                                        fld.Items.Add(new SelectListItem { Text = "Choose...", Value = "" });
                                    }

                                    if (ft.AllowAllValue)
                                    {
                                        fld.Items.Add(new SelectListItem { Text = ft.AllowAllLabel, Value = "0" });
                                    }

                                    List<SelectListItem> items;
                                    bool hideData3SixtyUsers = HideData3SixtyUsers();

                                    var columns = $@"
                                        V.FieldTypeID,
                                        V.LookupObjectType,
                                        V.LookupObjectID,
                                        V.Value,
                                        {(fld.UseColorControl ? "colorJson.FV AS Text" : "V.Text")}";

                                    var hideData3SixtyUsersCondition = $@" and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com'";

                                    var resourceJoin = $@"
                                        inner join reporting.Global_resource R on R.ResourceID = V.Value and R.State <> 3 {(hideData3SixtyUsers ? hideData3SixtyUsersCondition : "")}
                                        ";

                                    var colorjoin = $@"
                                        outer apply(SELECT FV = (SELECT V.Text as name, COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color 
                                                    from Asset A 
                                                    outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
													where A.Object = v.LookupObjectType and A.ObjectID = V.Value FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) 
                                        )colorJSON 
                                        ";

                                    var itemSql = $@"select {columns} 
                                        from FieldLookupValue V
                                        {(ft.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        {(fld.UseColorControl ? colorjoin : "")}
                                        where V.FieldTypeID = @fieldTypeId
                                        ";

                                    var countSql = $@"select count(*)
                                        from FieldLookupValue V
                                        {(ft.LookupObjectType == "Resource" ? resourceJoin : "")}
                                        {(fld.UseColorControl ? colorjoin : "")}
                                        where V.FieldTypeID = @fieldTypeId
                                        ";

                                    if (ft.AllowMultipleValues)
                                    {
                                        items = Company.Query<FieldLookupValue>(itemSql, new { fieldTypeId = ft.ID })
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
                                            if (selected.Contains(item.Value))
                                            {
                                                item.Selected = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        int maxItems = SettingsRepository.GetSettingValue<int>(Setting.MaxDropdownItems);
                                        int count = Company.Query<int>(countSql, new { fieldTypeId = ft.ID }).FirstOrDefault();

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
                                                selected = Company.FieldLookupValues.Where(i => i.FieldTypeID == ft.ID && i.Value == selectedValueInt)
                                                .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = true })
                                                .ToList();
                                            }
                                            items = selected;
                                        }
                                        else
                                        {
                                            fld.UseTypeahead = false;

                                            items = Company.Query<FieldLookupValue>(itemSql, new { fieldTypeId = ft.ID })
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
                            catch (Exception ex)
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

                                Predicate predicate = null;
                                if (intersectType.PredicateID.HasValue)
                                {
                                    predicate = Company.GetById<Predicate>((int)intersectType.PredicateID);
                                    if (predicate != null && predicate.Type.AsInfoModel().SingleRelationshipByFunctionalType)
                                    {
                                        fld.IsSemantic = true;
                                    }
                                }
                            }
                        }

                        #endregion Relationship

                        if (ft.Type == DataType.Lookup.ToString())
                        {
                            fld.Required = (ft.MinimumLength > 0 || ft.Length > 0 || ft.IsRequired);
                        }
                        else
                            if (!new[] { "Number", "Decimal", "Text" }.Contains(ft.Type))
                        {
                            fld.Required = (ft.MinimumLength > 0 || ft.Length > 0);
                        }



                        if (!ft.AllowMultipleValues)
                        {
                            if (f != null)
                            {
                                if (!string.IsNullOrEmpty(f.Value))
                                {
                                    fld.Value = decode ? Server.HtmlDecode(f.Value) : f.Value;
                                }
                                else
                                {
                                    fld.Value = decode ? Server.HtmlDecode(f.FormattedValue) : f.FormattedValue;
                                }
                            }
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

        internal void SendException(Exception ex, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (properties == null) properties = new Dictionary<string, string>();
            var telemetry = new TelemetryClient();
            if (!properties.ContainsKey("CompanyID")) properties.Add("CompanyID", Company.CurrentCompanyID.ToString());
            telemetry.TrackException(ex, properties, metrics);
            telemetry = null;
        }

        #region Dynamic Query Processing

        internal List<FieldType> getFieldTypesByObjectType(string objectType, int objectTypeID, bool listableOnly)
        {
            return (listableOnly) ?
                Company.Filter<FieldType>(i => i.Object == objectType && i.ObjectID == objectTypeID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList() :
                Company.Filter<FieldType>(i => i.Object == objectType && i.ObjectID == objectTypeID).OrderBy(i => i.ColumnOrder).ToList();
        }

        #endregion

        #region Private Methods


        internal void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFriendlyName = false, bool listableOnly = true, List<FieldType> fields = null, string idColumn = "A.ID")
        {
            Company.getDynamicFieldJoinStatements(typeID, type, out joins, out columns, includeIdColumn, useFriendlyName, listableOnly, fields, idColumn);
        }
        
        internal List<FieldType> getDynamicFieldJoinStatements(int typeID, string type, List<string> filterFields, out string joins, out string filterjoins, out string columns, out string filtercolumns, DynamicParameters dbArgs, bool includeIdColumn = true, bool useFriendlyName = false, List<FieldType> fields = null, bool showSubsetColumns = false, List<int> subsetColumns = null, string idColumn = "A.ID")
        {
            var columnBuilder = new StringBuilder();
            var joinBuilder = new StringBuilder();
            var filterJoinBuilder = new StringBuilder();
            var filterColumnBuilder = new StringBuilder();
            var fieldJoinBuilder = new StringBuilder();

            columns = "";
            joins = "";

            filterjoins = "";
            filtercolumns = "";

            var fieldTypeRelationType = $"{type}Type";
            if (fields == null)
            {
                fields = Company.Filter<FieldType>(i =>
                        i.Object == fieldTypeRelationType &&
                        i.ObjectID == typeID &&
                        i.IsListable
                    ).OrderBy(i => i.ColumnOrder).ToList();
            }

            var dtJsonElement = DataType.JsonElement.ToString();

            foreach (var f in fields)
            {
                var ftID = f.ID;

                FieldTypeDefinition_JsonElement jsonElementDefinition = null;
                if (f.Type == dtJsonElement)
                {
                    jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(f.Definition);
                    ftID = jsonElementDefinition.FieldTypeID;
                }

                var name = $"Field{f.ID}";
                var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");
                string thisColumn;
                fieldJoinBuilder.Clear();
                string dataType;

                if (f.Type == dtJsonElement)
                {
                    fieldJoinBuilder.Append($" left join Field {name} on {name}.ObjectType = '{type}' and {name}.ObjectID = {idColumn} and {name}.FieldTypeID = {ftID} ");
                    fieldJoinBuilder.Append($" left join FieldJsonProperty {name}_FJP on {name}_FJP.FieldID = {name}.ID and {name}_FJP.[Path] = @jsonPath{f.ID} ");

                    dbArgs.Add($"@jsonPath{f.ID}", jsonElementDefinition.Path);

                    dataType = jsonElementDefinition.DataType;

                    thisColumn = $", try_cast({name}_FJP.[Value] as {dataType}) as [{(useFriendlyName ? friendlyName : name)}]";
                    columnBuilder.Append(thisColumn);
                }
                else
                {
                    fieldJoinBuilder.Append($@" left join FieldDetail {name} on {name}.Object = '{type}' and {name}.ObjectID = {idColumn} and {name}.FieldTypeID = {f.ID} ");

                    dataType = f.Type;

                    switch (dataType)
                    {
                        case "decimal":
                            dataType = "float";
                            break;
                        default:
                            dataType = "nvarchar(max)";
                            break;
                    }

                    thisColumn = $@", try_cast({name}.FormattedValue as {dataType}) as [{(useFriendlyName ? friendlyName : name)}]";
                    columnBuilder.Append(thisColumn);
                    if (includeIdColumn)
                    {
                        columnBuilder.Append($"{name}.Value as [{name}ID], ");
                    }
                }

                joinBuilder.Append(fieldJoinBuilder.ToString());

                if (filterFields.Contains(name))
                {
                    filterColumnBuilder.Append(thisColumn);
                    filterJoinBuilder.Append(fieldJoinBuilder);
                }
            }
            columns = columnBuilder.ToString();
            joins = joinBuilder.ToString();
            filterjoins = filterJoinBuilder.ToString();
            filtercolumns = filterColumnBuilder.ToString();
            return fields;
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

        internal string applyHiddenFilteringSuffix(HttpRequestBase Request, Dapper.DynamicParameters dbParams, string idColumn = "A.ID", List<FieldType> fields = null, bool v2ApiFilterValues = false)
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
                    {
                        filter = applyMulitSelectFilteringSuffix(dbParams, fValue, tableId, i, fieldType, idColumn, v2ApiFilterValues);
                    }
                    else
                    {
                        filter = $" inner join field {tableId} on ({idColumn} = {tableId}.objectID and {tableId}.ObjectType = 'Artifact'  and {tableId}.fieldtypeid={fieldID} and {getFilteringConditionBind(tableId + ".FormattedValue", fCondition, i, dbParams, fValue, tableId, true)} )  ";
                    }

                    if (!string.IsNullOrEmpty(filter))
                    {
                        filters += filter;
                    }
                }
            }

            return filters;
        }

        private string applyMulitSelectFilteringSuffix(Dapper.DynamicParameters dbParams, string value, string prefix, int filterNumber, FieldType fieldType, string idColumn = "A.ID", bool v2ApiFilterValues = false)
        {
            value = value.Replace("!~!", ",");

            if (v2ApiFilterValues)
            {
                var resolveValueSQL = @"select string_agg(FLV.Value,',') from 
                        STRING_SPLIT (@input,',') S  
                        inner join 
                        FieldLookupValue FLV
                        ON fieldtypeid = @ftId AND Text = TRIM(S.value)";

                value = Company.Query<string>(resolveValueSQL, new { input = value, ftId = fieldType.ID }).FirstOrDefault();

                if (fieldType.AllowAllValue)
                {
                    value += "," + fieldType.AllowAllLabel;
                }
            }
            else
            {
                if (fieldType.AllowAllValue)
                    value += ",0";
            }

            var bind = $"{prefix}{filterNumber}val";
            dbParams.Add(bind, $"{value}");

            var filter = $@"			inner join ( 
			select F.objectID, F.ObjectType, F.FieldTypeID, dd.value as Value, F.[Value] as Val from Field F with (NOLOCK) 
			cross apply string_split(F.Value,',') dd 
			where F.FieldTypeID = {fieldType.ID} 
			and exists (SELECT value  
			FROM STRING_SPLIT(@{bind}, ',')  WHERE RTRIM(value)=dd.value) 
			)  {prefix}  on   {prefix}.objectID={idColumn} ";

            return filter;
        }

        internal string applyFilteringSuffixBind(string sql, HttpRequestBase Request, Dapper.DynamicParameters dbParams, bool applyHiddenFilters = false, List<FieldType> fields = null, bool fromArtifact = false, bool v2ApiFilterValues = false)
        {
            return sql + applyFilteringSuffixBindRaw(Request, dbParams, applyHiddenFilters, fields, fromArtifact: fromArtifact, v2ApiFilterValues: v2ApiFilterValues);
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
                            ownershipFilter.Items.Add(new UiRequestOwnershipFilterItem
                            {
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
                            ownershipFilter.Items.Add(new UiRequestOwnershipFilterItem
                            {
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

        internal string applyFilteringSuffixBindRaw(HttpRequestBase Request, Dapper.DynamicParameters dbParams, bool applyHiddenFilters = false, List<FieldType> fields = null, string idColumn = "A.ID", bool fromArtifact = false, bool v2ApiFilterValues = false)
        {
            var query = Request.Params;

            #region Field Filters

            int filterscount = 0;
            var filters = applyHiddenFilters ? applyHiddenFilteringSuffix(Request, dbParams, idColumn, fields, v2ApiFilterValues) : string.Empty;
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
                    if (fromArtifact && filterFieldType != null && filterFieldType.AllowMultipleValues)
                        filters += applyMulitSelectFilteringSuffix(dbParams, fValue, tableId, i, filterFieldType, idColumn, v2ApiFilterValues);
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

                    if (v2ApiFilterValues)
                    {
                        RelationshipObjectIDs = Company.Query<string>(@"select string_agg(A.ObjectID,',')
                                from string_split(@objectUids,',')S
                                inner join Asset A on A.Uid = S.value", new { objectUids = RelationshipObjectIDs }).FirstOrDefault();
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
                            var subSql = $@"select a.ObjectID from [Intersect] i
                                    inner join intersecttype it on (i.intersecttypeid = it.id)
                                    inner join[intersect] i_2 on(i_2.subject = 'Map' and i_2.subjectid = i.subjectid and i.subject = 'Map')
                                    inner join asset a on a.object = 'Artifact' and a.objectid = = i_2.objectid
                                    inner join assettype t on t.id = a.assettypeid and t.object = 'ArtifactType' and t.objectid = @id
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
                throw new ArgumentNullException(ApiMessages.InvalidSortOrder);
            }

            // make sure its a valid field name
            if (!isValidFieldName(sortDataField))
            {
                throw new ArgumentNullException(ApiMessages.InvalidSortField);
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
            Mail.SendMessage("Data360 Password Reset", email, fullName, templateValues, "forms-password-reset");
        }

        #endregion

        protected async Task<bool> IsSingleSignOn()
        {
            var authModel = await Community.QueryFirstOrDefaultAsync<AuthenticationType>("select AuthenticationType from CompanyDomainSetting where CompanyID = @id and UrlPrefix = @prefix", new { id = Company.CurrentCompanyID, prefix = Company.CurrentCompanyDomain });

            return !(authModel == AuthenticationType.Forms);
        }

        protected FileContentResult ExcelDocumentAsFile(ExcelDocument document)
        {
            using (var stream = new MemoryStream())
            {
                using (var slDocument = document.ToSLDocument())
                {
                    slDocument.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.ms-excel", $"{document.Name.GetSafeFilename()}.xlsx");
                }
            }
        }
    }
}
