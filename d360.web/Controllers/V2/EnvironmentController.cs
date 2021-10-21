using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Linq;
using static d360.model.CommunityContext;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling environment and settings in Govern.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/environment"),
        Authorize
    ]
    public class EnvironmentController : BaseV2ApiController
    {
        IStorageProvider _storage;
        IAssetRepository _assetRepository;

        public EnvironmentController(CoreComponentSet set, IStorageProvider storage, IAssetRepository assetRepository)
            : base(set)
        {
            _storage = storage;
            _assetRepository = assetRepository;

        }

        [HttpGet, AjaxValidateAntiForgeryToken, Route("rebuilds"), ApiExplorerSettings(IgnoreApi = true)]
        public async Task<HttpResponseMessage> GetRebuilds()
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
                }

                var currentStatusList = await Community.GetRebuildJobStatuses();
                var listToReturn = CompanyRebuildJobStatusApiModel.GetDefaultList();
                currentStatusList.ForEach(i =>
                {
                    listToReturn.Single(j => j.JobToken == i.JobToken).SetCurrentJobStatusProperties(i);
                });

                return Request.CreateResponse(HttpStatusCode.OK, listToReturn);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("rebuilds"), ApiExplorerSettings(IgnoreApi = true)]
        public async Task<HttpResponseMessage> Rebuild(CompanyRebuildJobRequest model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
                }

                if (model == null)
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);
                }

                var readyToActivate = await Community.UpdateRebuildJobStatus(model.Job, CompanyRebuildJobStatusState.Active);
                if (readyToActivate.StatusCode == HttpStatusCode.OK)
                {
                    switch (model.Job)
                    {
                        case CompanyRebuildJobToken.AssetGraph:
                            Company.RebuildAssetGraphRequest();
                            break;
                        case CompanyRebuildJobToken.DisplayValues:
                            Company.RebuildDisplayValuesRequest();
                            break;
                        case CompanyRebuildJobToken.SearchIndex:
                            Company.RebuildIndexRequest();
                            break;
                    }

                    return Request.CreateResponse(HttpStatusCode.Created, new { type = "confirm", title = "Success!", action = "add", message = "Rebuild request received and accepted.", id = "" });
                }
                else
                {
                    return ReturnApiError(readyToActivate.StatusCode, readyToActivate.Error);
                }
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet, Route("styles"), ApiExplorerSettings(IgnoreApi = true)]
        public HttpResponseMessage StyleCustomizations()
        {
            var css = "";

            //only admins can access this route
            if (!Company.CurrentResourceIsAdmin)
            {
                return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
            }


            //go to azure storage for this company try to get the custom css
            try
            {
                css = _storage.GetFileContentsAsString(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css");
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.Message);
            }

            return Request.CreateResponse(HttpStatusCode.OK, css);
        }

        [HttpPut, Route("styles"), ApiExplorerSettings(IgnoreApi = true)]
        public async Task<HttpResponseMessage> UpdateStyleCustomizations(UpdateCss UpdateCss)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);

            //delete the old css file
            try
            {
                await _storage.DeleteFile(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css");
            }
            catch { }

            try
            {
                if (!string.IsNullOrWhiteSpace(UpdateCss.css))
                {
                    SettingsRepository.UpsertSetting(Setting.CustomCSSLocation, $"{constants.COMPANY_STYLES_URL}{Company.CurrentCompanyID}.css");
                    await _storage.CreateFile(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css", UpdateCss.css, "text/css", false);
                }
                else
                {
                    SettingsRepository.DeleteSetting(Setting.CustomCSSLocation);
                }
            }
            catch { }

            return Request.CreateResponse(HttpStatusCode.OK, ApiMessages.StyleUpdated);
        }


        /// <summary>
        /// Retrieves a list of company settings.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("settings"),
            SwaggerParameter("_settingId", "Optional parameter to filter by setting ID.", DataType = "integer", ParameterType = "query", Required = false),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage Settings()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
            }

            var queryParams = Request.GetQueryNameValuePairs();
            var _settingId = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_settingid").Value;
            int? settingId = null;
            if (!string.IsNullOrEmpty(_settingId))
            {
                if (!int.TryParse(_settingId, out int val) || val <= 0)
                    return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.SettingIDNotValid);
                else
                    settingId = val;
            }

            try
            {
                var settings = SettingsRepository.GetSettings();
                if (settingId.HasValue)
                {
                    settings = settings.Where(s => (int)s.ID == settingId.Value).ToList();
                }
                
                if (settingId.HasValue && settings.Count() == 0)
                {
                    return ReturnApiError(HttpStatusCode.NotFound, ApiMessages.SettingIDNotFound);
                }

                var response = settings.Select(s => new CompanySettingApiModel(s, s.Value));

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.Message);
            }

        }

        /// <summary>
        /// Update a setting. If the setting value is null, it will be set to the default value.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("settings"), ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage UpdateSetting(CompanySettingApiUpdateModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
            }

            if (model == null)
                return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);

            try
            {
                var setting = Setting.ActionMessage.GetAsList().SingleOrDefault(s => (int)s.ID == model.SettingID);

                if (setting == null)
                    return ReturnApiError(HttpStatusCode.NotFound, ApiMessages.SettingIDNotFound);

                if (setting.Locked)
                    return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.SettingLocked);

                if (!model.HasExactlyOneValue)
                    return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.SettingValueProvided);

                bool clearSetting = false;
                string value = "";

                string valueErrorMessage = ApiMessages.DataTypeValueNotMatched;
                switch (setting.Type)
                {
                    case SettingType.Number:
                        if (model.NumberSetting == null)
                        {
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        }

                        if (model.NumberSetting.Value == null)
                        {
                            clearSetting = true;
                        }
                        else
                        {
                            if (int.TryParse(model.NumberSetting.Value, out int val))
                            {
                                value = val.ToString();
                            }
                            else
                            {
                                return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.InvalidNumber);
                            }
                        }
                        break;
                    case SettingType.Boolean:
                        if (model.BooleanSetting == null)
                        {
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        }

                        if (model.BooleanSetting.Value == null)
                        {
                            clearSetting = true;
                        }
                        else
                        {
                            if (bool.TryParse(model.BooleanSetting.Value, out bool val))
                            {
                                value = (val.ToString() ?? "").ToLower();
                            }
                            else
                            {
                                return ReturnApiError(HttpStatusCode.BadRequest,ApiMessages.InvalidBoolean);
                            }
                        }

                        break;
                    case SettingType.IPAddress:
                        if (model.IpAddressSetting == null)
                        {
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        }

                        if (model.IpAddressSetting.Value == null || model.IpAddressSetting.Value.Count == 0)
                        {
                            clearSetting = true;
                        }

                        if (model.IpAddressSetting.Value?.Any() ?? false)
                        {
                            value = "<ips />";
                            var xml = new XElement("ips");
                            foreach (var ip in model.IpAddressSetting.Value)
                            {
                                if (string.IsNullOrEmpty(ip.Name) || string.IsNullOrEmpty(ip.Start) || string.IsNullOrEmpty(ip.End))
                                    return ReturnApiError(HttpStatusCode.BadRequest,ApiMessages.MissingIPAddressValue);
                                if (!IPAddress.TryParse(ip.Start, out IPAddress _))
                                    return ReturnApiError(HttpStatusCode.BadRequest, string.Format(ApiMessages.StartIPAddressNotValid, ip.Start));
                                if (!IPAddress.TryParse(ip.End, out IPAddress _))
                                    return ReturnApiError(HttpStatusCode.BadRequest, string.Format(ApiMessages.EndIPAddressNotValid, ip.End));

                                xml.Add(new XElement("ip",
                                    new XElement("name", ip.Name),
                                    new XElement("start", ip.Start),
                                    new XElement("end", ip.End)
                                ));

                            }

                            value = xml.ToString();
                        }
                        break;

                    case SettingType.Guid:
                        if (model.GuidSetting == null)
                        {
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        }

                        if (model.GuidSetting.Value == null)
                        {
                            clearSetting = true;
                        }

                        value = model.GuidSetting.Value.ToString();

                        break;
                    default:
                        if (model.StringSetting == null)
                        {
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        }

                        if (model.StringSetting.Value == null)
                        {
                            clearSetting = true;
                        }

                        value = model.StringSetting.Value;

                        break;
                }

                // Sanitize allowed CORS origins
                if (setting.ID == Setting.AllowedOrigins && !string.IsNullOrEmpty(value))
                {
                    value = string.Join(",", value
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
                        .ToList());
                }

                if (clearSetting)
                {
                    SettingsRepository.DeleteSetting(setting.ID);
                }
                else
                {
                    SettingsRepository.UpsertSetting(setting.ID, value);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.Message);
            }

        }

        /// <summary>
        /// Retrieves a list of operators that can be used as values for the Operator property on certain endpoints within the Scoring and Metrics API.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("operators"),
            SwaggerConsumes("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Gets a list of operators.", typeof(List<OperatorInfo>))
        ]
        public async Task<IHttpActionResult> GetOperators(bool isForAdvancedFilters = false)
        {
            var response = Operator.Equals.GetAsList().OrderBy(x=> x.SortOrder);
            if (isForAdvancedFilters)
            {
                response = Operator.Equals.GetAsListForAdvancedFilters().OrderBy(x => x.SortOrder);
            }
            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

        }

        /// <summary>
        /// Retrieves usage information for assets and asset types a user or users has viewed.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("usage"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this information is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerConsumes("application/json"), 
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by eventDate.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to include the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_startDate", "Start date for events to return", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_endDate", "End date for events to return", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_resourceUid", "Filter by the provided resource uid", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "Filter by the provided asset Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetTypeUid", "Filter by the provided asset type uid", DataType = "string", ParameterType = "query", Required = false),

        ]
        public async Task<IHttpActionResult> GetUsageDetails()
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, "Forbidden your not an admin.")).ConfigureAwait(false);


                var queryParams = Request.GetQueryNameValuePairs();
                string isValid = isPageSizeAndNumValid(queryParams);
                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
                }


                var dbArgs = new DynamicParameters();

                var orderBySql = "";
                var offsetSql = "";
                var orderDirection = "";
                var pageNum = -1;
                var pageSize = 200;
                var whereClause = "";
                bool includeTotal = true;
                
                List<string> whereClauseItems = new List<string>();

                string[] columns = 
                { 
                    "action", 
                    "user agent", 
                    "host", 
                    "browser language", 
                    "timestamp" , 
                    "assettypename", 
                    "assettypeuid", 
                    "assetuid", 
                    "assetdisplayvalue", 
                    "class", 
                    "assetTypeuid2" 
                };

                #region handle queryparams
                if (queryParams.Any(x => x.Key == "_direction"))
                {
                    string[] allowedDirections = { "asc", "desc" };
                    var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value;
                    if (!allowedDirections.Contains(order.Trim().ToLower()))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString(),ApiMessages.InvalidDirection)).ConfigureAwait(false);
                    }

                    orderDirection = allowedDirections.Contains(order.Trim().ToLower()) ? order : "asc";
                }

                if (!queryParams.Any(p => p.Key == "_order"))
                {
                    orderBySql = $"order by Timestamp {orderDirection}";
                }

                string errorMessage = null;
                HttpStatusCode code = HttpStatusCode.OK;

                queryParams.ToList().ForEach(q =>
                {
                    var key = q.Key.ToLower();

                    if (key.StartsWith("_"))
                    {
                        if (key == "_order")
                        {
                            if (columns.Contains(q.Value.ToLower()))
                            {
                                orderBySql = $"order by {q.Value} {orderDirection}";
                            }
                            else
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage =ApiMessages.Invalid_Order;
                            }
                        }
                        else if (key == "_pagenum")
                        {
                            if (int.TryParse(q.Value, out pageNum))
                            {
                                if (pageNum < 1) { pageNum = 1; }
                            }
                        }
                        else if (key == "_pagesize")
                        {
                            if (int.TryParse(q.Value, out pageSize))
                            {
                                if (pageSize < 1) { pageSize = 1; }
                            }
                        }
                        else if(key == "_includetotal")
                        {
                            if(!bool.TryParse(q.Value, out includeTotal))
                            {
                                includeTotal = true;
                            }

                        }
                        else if (key == "_startdate")
                        {
                            DateTime startDate = DateTime.MinValue;
                            if (!DateTime.TryParse(q.Value, out startDate))
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = ApiMessages.InvalidStartDate;
                            }
                            else
                            {
                                dbArgs.Add("startDate", startDate);
                                whereClauseItems.Add("stat.Timestamp >= @startDate");
                            }
                        }
                        else if (key == "_enddate")
                        {

                            DateTime endDate = DateTime.MaxValue;
                            if (!DateTime.TryParse(q.Value, out endDate))
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = ApiMessages.InvalidEndDate;
                            }
                            else
                            {
                                dbArgs.Add("endDate", endDate);
                                whereClauseItems.Add("stat.Timestamp <= @endDate");
                            }
                        }
                        else if(key == "_resourceuid")
                        {
                            Guid ruid = Guid.Empty;
                            if (Guid.TryParse(q.Value,out ruid))
                            {
                                if(Company.GlobalReportingResources.Any(x => x.Uid == ruid) && ruid != Guid.Empty)
                                {
                                    whereClauseItems.Add("gr.uid = @resourceUid");
                                    dbArgs.Add("resourceUid", ruid);
                                }
                                else
                                {
                                    code = HttpStatusCode.BadRequest;
                                    errorMessage = ApiMessages.InvalidResourceuid;
                                }
                            }
                            else
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = ApiMessages.InvalidResourceuid;
                            }
                        }
                        else if (key == "_assetuid")
                        {
                            Guid auid = Guid.Empty;
                            if (Guid.TryParse(q.Value, out auid))
                            {
                                if(Company.Assets.Any(x => x.uid == auid) && auid != Guid.Empty) 
                                {
                                    whereClauseItems.Add("a.uid = @assetuid");
                                    dbArgs.Add("assetuid", auid);
                                }
                                else
                                {
                                    code = HttpStatusCode.BadRequest;
                                    errorMessage = string.Format(ApiMessages.InvalidAssetUid, q.Value);
                                }
                            }
                            else
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = string.Format(ApiMessages.InvalidAssetUid, q.Value);
                            }
                        }
                        else if (key == "_assettypeuid")
                        {
                            Guid atuid = Guid.Empty;
                            if (Guid.TryParse(q.Value, out atuid))
                            {
                                if(Company.AssetTypes.Any(x => x.uid == atuid) && atuid != Guid.Empty)
                                {
                                    whereClauseItems.Add("(att.uid = @assettypeuid or att2.uid = @assettypeuid )");
                                    dbArgs.Add("assettypeuid", atuid);
                                }
                                else
                                {
                                    code = HttpStatusCode.BadRequest;
                                    errorMessage = string.Format(ActionApiMessages.AssetTypeNotFound, q.Value);
                                }
                            }
                            else
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = string.Format(ActionApiMessages.AssetTypeNotFound, q.Value);
                            }
                        }
                    }
                });

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return await Task.FromResult(errorMessageResponse(code, code.ToString(), errorMessage)).ConfigureAwait(false);
                }

                if (pageSize > 0 || pageNum > 0)
                {
                    if (pageSize < 1) { pageSize = 1; }
                    if (pageNum < 1) { pageNum = 1; }

                    offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";

                }

                

                if(whereClauseItems.Count > 0)
                {
                    whereClause = $" where {string.Join(" and ", whereClauseItems.ToArray()) } ";
                }

                #endregion

                string sql = $@"
                select 
                    act.value as 'action', 
                    ua.Value as 'userAgent',
                    h.Value as 'host',
                    bl.Value as 'language', 
		            stat.Timestamp as 'eventDate', 
                    COALESCE(att.Name, att2.Name) as 'assetTypeName', 
                    COALESCE(att.uid,  att2.uid) as 'assetTypeUid', 
                    a.uid as 'assetUid', 
                    adv.DisplayValue as 'assetDisplayValue', 
                    COALESCE(att.class,  att2.class) as 'assetClass',
                    gr.uid as 'resourceUid'
                from analytics.Statistic stat
	                inner join reporting.global_resource gr on stat.resourceid = gr.resourceid
	                inner join analytics.BrowserLanguage bl on bl.ID = stat.BrowserLanguageID
	                inner join analytics.Host h on h.id = stat.HostID
	                inner join analytics.UserAgent ua on ua.id = stat.UserAgentID
	                inner join analytics.Object o on o.id = stat.Object
	                inner join analytics.action act on act.id = stat.ActionID
	                left join assettype att on att.ObjectID = stat.ObjectID and att.object = o.value
	                left join asset a on a.Object = o.value and a.ObjectID = stat.ObjectID
	                left join AssetDisplayValue adv on adv.AssetID = a.id
	                left join assettype att2 on att2.id = a.AssetTypeID
                    {whereClause}
                    {orderBySql}
                    {offsetSql}
                ";

                string countSql = $@"
                select count(*) from analytics.Statistic stat
	                inner join reporting.global_resource gr on stat.resourceid = gr.resourceid
	                inner join analytics.BrowserLanguage bl on bl.ID = stat.BrowserLanguageID
	                inner join analytics.Host h on h.id = stat.HostID
	                inner join analytics.UserAgent ua on ua.id = stat.UserAgentID
	                inner join analytics.Object o on o.id = stat.Object
	                inner join analytics.action act on act.id = stat.ActionID
	                left join assettype att on att.ObjectID = stat.ObjectID and att.object = o.value
	                left join asset a on a.Object = o.value and a.ObjectID = stat.ObjectID
	                left join AssetDisplayValue adv on adv.AssetID = a.id
	                left join assettype att2 on att2.id = a.AssetTypeID
                    {whereClause}
                ";

                var response = await Company.QueryAsync<dynamic>(sql, dbArgs);

                
                if (includeTotal)
                {
                    var count = await Company.QueryFirstOrDefaultAsync<int>(countSql, dbArgs);
                    var model = new { pageSize, pageNum, total = count, items = response };
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model))).ConfigureAwait(false);
                }
                else
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response))).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() { {"Endpoint Method", "Environment.GetUsageDetails => "} });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Retrieves environment licensing info. 
        /// Infogix users are excluded from user counts.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("licensing"),
            SwaggerConsumes("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "License info", typeof(LicenceDetailsModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this information is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),

        ]
        public async Task<IHttpActionResult> GetLicensingDetails()
        {
            try
            {
                var includedAssetClasses = new List<AssetTypeClass>() {
                    AssetTypeClass.BusinessAsset,
                    AssetTypeClass.Diagram,
                    AssetTypeClass.Group,
                    AssetTypeClass.Model,
                    AssetTypeClass.Organization,
                    AssetTypeClass.Policy,
                    AssetTypeClass.Reference,
                    AssetTypeClass.Rule,
                    AssetTypeClass.TechnicalAsset
                };
                var allAssets = await Company.QueryFirstOrDefaultAsync<int>(@"select count(1) from asset a 
                                                                                inner join assettype att on a.assetTypeId = att.id 
                                                                                where att.class in @includedClassTypes",
                                                                            new { includedClassTypes = includedAssetClasses }).ConfigureAwait(false);
              
                var allusers = await Company.QueryFirstOrDefaultAsync<int>(@"SELECT count(*) from reporting.global_resource GR 
                                                                            WHERE gr.Email not like '%@infogix.com'
                                                                            and gr.Email not like '%@data3sixty.com'
                                                                            and gr.State = 1").ConfigureAwait(false);
            

              
                var allAdminUsers = await Company.QueryFirstOrDefaultAsync<int>(@"SELECT count(*) from reporting.global_resource GR 
                                                                            WHERE gr.Email not like '%@infogix.com'
                                                                            and gr.Email not like '%@data3sixty.com'
                                                                            and gr.State = 1 
                                                                            and gr.IsAdministrator = 1").ConfigureAwait(false);
              
                
                var contributorSql = @"
                    DROP TABLE if exists #AssetTypesWithResponsibilities
                    CREATE TABLE #AssetTypesWithResponsibilities
                    (
	                    AssetTypeID int
                    )
                    insert into #AssetTypesWithResponsibilities 
                    SELECT DISTINCT AT.ID from AssetType AT 
                    left join [dbo].[ResponsibilityTypeRelation] RT on AT.Object = RT.ObjectType and RT.ObjectID = AT.ObjectID
                    left join [dbo].[ResponsibilityTypeRelationRule] RTR on AT.Object = RTR.Object and RTR.ObjectID = AT.ObjectID
                    left join [dbo].[ResponsibilityRuleResultAsset] RRA on RRA.AssetTypeID = AT.ID
                    inner join Asset A on A.AssetTypeID = AT.ID 
                    left join [dbo].[ResponsibilityTypeRelationOverrideItem] RTOR on a.Id = RTOR.AssetID
                    WHERE A.ID = RTOR.AssetID


                    SELECT count(1) from reporting.global_resource GR
	                    where exists (
		                    SELECT 
                            1
		                    from #AssetTypesWithResponsibilities AT
			                    outer apply (Select * from UserAssetPermissions(GR.ResourceID,AT.AssetTypeID)) permission 
			                    where 1 = Case 
		                                                       when permission.PermissionsBitMask is null then gr.IsAdministrator
		                                                       when permission.PermissionsBitMask is not null and permission.PermissionsBitMask & @pm > 0 then 1
		                                                       when permission.PermissionsBitMask is not null and permission.PermissionsBitMask & @pd = @pd then 1 END

                    )   
                    and gr.Email not like '%@infogix.com' 
                    and gr.Email not like '%@data3sixty.com'  
                    and gr.State = 1
                    and gr.IsAdministrator = 0
                ";

                //Using ModifyAsset permission which is AddAsset | EditAsset - if PermissionsBitMask and'ed with this is greater than 0, the user has one or both
                var contibutorCount = await Company.QueryFirstOrDefaultAsync<int>(contributorSql, new { pm = (int)Permission.ModifyAsset, pd = (int)Permission.DeleteAsset }).ConfigureAwait(false);
                var model = new { assets = new { count = allAssets }, users = new { total = allusers, contributors = (contibutorCount + allAdminUsers), administrators = allAdminUsers } };


                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() { { "Endpoint Method", "Environment.GetLicensingDetails => " } });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Retrieves environment licensing info. 
        /// Infogix users are excluded from user counts.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("featureflaginfo"),
            SwaggerConsumes("application/json"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetFeatureFlagInfo()
        {
            try
            {
                var user = GetClientFeatureFlagUser();
                var ClientId = Config.GetValue<string>("LaunchDarklyClientId");
                return await Task.FromResult(
                    ResponseMessage(
                        Request.CreateResponse(HttpStatusCode.OK, new {
                            clientId = ClientId,
                            user = user
                        }))
                    ).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() { { "Endpoint Method", "Environment.GetLicensingDetails => " } });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }
    }
}
