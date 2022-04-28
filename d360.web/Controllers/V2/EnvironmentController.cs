using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.resources;
using d360.extensions;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Linq;


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
        readonly IThemeRepository ThemeRepository;
        readonly IStorageProvider _storage;

        public EnvironmentController(ICoreComponentSet set, IThemeRepository themeRepository, IStorageProvider storage) : base(set)
        {
            ThemeRepository = themeRepository;
            _storage = storage;
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

                var currentStatusList = await Company.GetRebuildJobStatuses(constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS);
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

                var readyToActivate = await Company.UpdateRebuildJobStatus(model.Job, CompanyRebuildJobStatusState.Active, constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS);
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

                    return Request.CreateResponse(HttpStatusCode.Created, new { type = ApiMessages.confirm, title = ApiMessages.Success, action = ApiMessages.add, message = ApiMessages.RebuildRequest, id = "" });
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
            {
                return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
            }

            //delete the old css file
            try
            {
                await _storage.DeleteFile(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css");
            }
            catch
            {
                //no handling of this case
            }

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
            catch
            {
                //no handling of this case
            }

            return Request.CreateResponse(HttpStatusCode.OK, ApiMessages.StyleUpdated);
        }


        /// <summary>
        /// Retrieves a list of epplication settings.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("appsettings"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetAppSettings()
        {
            try
            {
                var settings = new List<ApplicationSetting>();

                settings.Add(new ApplicationSetting { Name = "HelpBaseUri", Value = Config.GetValue<string>("HelpBaseUri") });
                settings.Add(new ApplicationSetting { Name = "AppInsightsInstrumentationKey", Value = Config.GetValue<string>("AppInsightsInstrumentationKey") });

                return Request.CreateResponse(HttpStatusCode.OK, settings);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.Message);
            }

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
            var queryParams = Request.GetQueryNameValuePairs();
            var _settingId = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_settingid").Value;
            int? settingId = null;
            if (!string.IsNullOrEmpty(_settingId))
            {
                if (!int.TryParse(_settingId, out int val) || val <= 0)
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.SettingIDNotValid);
                }
                else
                {
                    settingId = val;
                }
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

        private async Task<string> updateSingleSettingImageFile(string folder, string url, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                var filesToDelete = _storage.ListFilenamesByPrefix(folder, $"{Company.CurrentCompanyID}.");
                filesToDelete.ForEach(f =>
                {
                    _storage.DeleteFile(folder, f).Wait();
                });
            }
            else
            {
                var info = data.GetFileFromDataUrl();
                var imgFileName = string.Format("{0}{1}", Company.CurrentCompanyID, info.Item1);
                await _storage.CreateFile(folder, imgFileName, info.Item2).ConfigureAwait(false);
                data = $"{url}{imgFileName}";
            }

            return data;
        }

        private void updateSingleSetting(SettingInfo setting, CompanySettingApiUpdateModel model)
        {
            if (setting == null)
            {
                throw new GenericException(HttpStatusCode.NotFound, ApiMessages.SettingIDNotFound);
            }
            if (setting.Locked)
            {
                throw new GenericException(HttpStatusCode.Forbidden, ApiMessages.SettingLocked);
            }
            if (!model.HasExactlyOneValue)
            {
                throw new GenericException(HttpStatusCode.BadRequest, ApiMessages.SettingValueProvided);
            }

            bool clearSetting = false;
            string value = "";

            string valueErrorMessage = ApiMessages.DataTypeValueNotMatched;
            switch (setting.Type)
            {
                case SettingType.Number:
                    if (model.NumberSetting == null)
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, valueErrorMessage);
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
                            throw new GenericException(HttpStatusCode.BadRequest, ApiMessages.InvalidNumber);
                        }
                    }
                    break;
                case SettingType.Boolean:
                    if (model.BooleanSetting == null)
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, valueErrorMessage);
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
                            throw new GenericException(HttpStatusCode.BadRequest, ApiMessages.InvalidBoolean);
                        }
                    }

                    break;
                case SettingType.IPAddress:
                    if (model.IpAddressSetting == null)
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, valueErrorMessage);
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
                            {
                                throw new GenericException(HttpStatusCode.BadRequest, ApiMessages.MissingIPAddressValue);
                            }
                            if (!IPAddress.TryParse(ip.Start, out IPAddress _))
                            {
                                throw new GenericException(HttpStatusCode.BadRequest, string.Format(ApiMessages.StartIPAddressNotValid, ip.Start));
                            }
                            if (!IPAddress.TryParse(ip.End, out IPAddress _))
                            {
                                throw new GenericException(HttpStatusCode.BadRequest, string.Format(ApiMessages.EndIPAddressNotValid, ip.End));
                            }

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
                        throw new GenericException(HttpStatusCode.BadRequest, valueErrorMessage);
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
                        throw new GenericException(HttpStatusCode.BadRequest, valueErrorMessage);
                    }

                    if (model.StringSetting.Value == null)
                    {
                        clearSetting = true;
                    }

                    value = model.StringSetting.Value;

                    break;
            }

            if (setting.ID == Setting.CompanyLogo || setting.ID == Setting.CompanyIcon || setting.ID == Setting.HomePageBackgroundImage)
            {
                if (!value.IsValidImageData())
                {
                    throw new GenericException(HttpStatusCode.BadRequest, valueErrorMessage);
                }
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

            if (setting.ID == Setting.CompanyLogo)
            {
                value = updateSingleSettingImageFile(constants.COMPANY_LOGO_FOLDER, constants.COMPANY_LOGO_URL, value).Result;
            }

            if (setting.ID == Setting.CompanyIcon)
            {
                value = updateSingleSettingImageFile(constants.COMPANY_ICON_FOLDER, constants.COMPANY_ICON_URL, value).Result;
            }

            if (setting.ID == Setting.HomePageBackgroundImage)
            {
                value = updateSingleSettingImageFile(constants.COMPANY_RESOURCES_FOLDER, constants.COMPANY_RESOURCES_URL, value).Result;
            }

            if (clearSetting)
            {
                SettingsRepository.DeleteSetting(setting.ID);
            }
            else
            {
                SettingsRepository.UpsertSetting(setting.ID, value);
            }
        }

        /// <summary>
        /// Update a setting. If the setting value is null, it will be set to the default value.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("settings"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage UpdateSetting(CompanySettingApiUpdateModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
            }

            if (model == null)
            {
                return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);
            }

            try
            {
                var setting = Setting.ActionMessage.GetAsList().SingleOrDefault(s => (int)s.ID == model.SettingID);
                updateSingleSetting(setting, model);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (GenericException ex)
            {
                return ReturnApiError(ex.StatusCode, ex.StatusMessage);
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
            Route("settings/batch"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage UpdateSettings(List<CompanySettingApiUpdateModel> models)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage);
            }

            if (models == null || models.Count == 0)
            {
                return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);
            }

            try
            {
                var list = Setting.ActionMessage.GetAsList();
                models.ForEach(model =>
                {
                    var setting = list.SingleOrDefault(s => (int)s.ID == model.SettingID);
                    updateSingleSetting(setting, model);
                });
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (GenericException ex)
            {
                return ReturnApiError(ex.StatusCode, ex.StatusMessage);
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
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Gets a list of operators.", typeof(List<OperatorInfo>))
        ]
        public async Task<IHttpActionResult> GetOperators(bool isForAdvancedFilters = false)
        {
            var response = Operator.Equals.GetAsList().OrderBy(x => x.SortOrder);
            if (isForAdvancedFilters)
            {
                response = Operator.Equals.GetAsListForAdvancedFilters().OrderBy(x => x.SortOrder);
            }
            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

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
                        Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            clientId = ClientId,
                            user
                        }))
                    ).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> { { "Endpoint Method", "Environment.GetFeatureFlagInfo => " } });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
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
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
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
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, "Forbidden your not an admin.")).ConfigureAwait(false);
                }

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
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString(), ApiMessages.InvalidDirection)).ConfigureAwait(false);
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
                                errorMessage = ApiMessages.Invalid_Order;
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
                        else if (key == "_includetotal")
                        {
                            if (!bool.TryParse(q.Value, out includeTotal))
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
                        else if (key == "_resourceuid")
                        {
                            Guid ruid = Guid.Empty;
                            if (Guid.TryParse(q.Value, out ruid))
                            {
                                if (Company.GlobalReportingResources.Any(x => x.Uid == ruid) && ruid != Guid.Empty)
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
                                if (Company.Assets.Any(x => x.uid == auid) && auid != Guid.Empty)
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
                                if (Company.AssetTypes.Any(x => x.uid == atuid) && atuid != Guid.Empty)
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



                if (whereClauseItems.Count > 0)
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
                    var model = new { pageSize, pageNum, items = response };
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model))).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() { { "Endpoint Method", "Environment.GetUsageDetails => " } });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Retrieves environment licensing info. 
        /// Precisely users are excluded from user counts.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("licensing"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
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
                                                                            and gr.Email not like '%@precisely.com'
                                                                            and gr.State = 1").ConfigureAwait(false);



                var allAdminUsers = await Company.QueryFirstOrDefaultAsync<int>(@"SELECT count(*) from reporting.global_resource GR 
                                                                            WHERE gr.Email not like '%@infogix.com'
                                                                            and gr.Email not like '%@data3sixty.com'
                                                                            and gr.Email not like '%@precisely.com'
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
			                    where ((permission.PermissionsBitMask is null and gr.IsAdministrator = 1)
		                               or (permission.PermissionsBitMask is not null and permission.PermissionsBitMask & @pm > 0)
		                               or (permission.PermissionsBitMask is not null and permission.PermissionsBitMask & @pd = @pd))

                    )   
                    and gr.Email not like '%@infogix.com' 
                    and gr.Email not like '%@data3sixty.com'  
                    and gr.Email not like '%@precisely.com'
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

        #region Help Menu Endpoints

        /// <summary>
        /// Gets help menu items.
        /// </summary>
        /// <returns></returns>
        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route("help"),
           SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.OK, "Gets help menu items.", typeof(List<HelpMenuItem>)),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetHelpMenuItems()
        {
            const string supportUrl = "https://support.infogix.com/hc/en-us/community/topics/360000029388-Data3Sixty-Govern";
            const string aboutUrl = "about";
            var baseUrl = System.Configuration.ConfigurationManager.AppSettings["HelpBaseUri"].ToString();

            try
            {
                var items = Company.HelpResources.ToList();
                List<HelpMenuItem> helpItems = new List<HelpMenuItem>();

                foreach (var item in items)
                {
                    HelpMenuItem help = new HelpMenuItem();
                    help.ID = item.ID;
                    help.Description = item.Description;
                    help.Name = item.Name;
                    if (item.isSystem && (item.Url != aboutUrl && item.Url != supportUrl))
                    {
                        help.Url = baseUrl + item.Url;
                    }
                    else
                    {
                        help.Url = item.Url;
                    }
                    help.order = item.order;
                    help.visibility = item.visibility;
                    help.uid = (Guid)item.uid;
                    help.isEditable = item.isEditable;
                    help.isSystem = item.isSystem;

                    helpItems.Add(help);
                }

                var response = Request.CreateResponse(HttpStatusCode.OK, helpItems);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Add new help menu items.
        /// </summary>
        /// <returns></returns>
        [
           HttpPost,
           MapToApiVersion("2.0"),
           Route("help"),
           SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.OK, "Adds new help menu items.", typeof(List<HelpMenuItemMessage>)),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> AddHelpMenuItems(List<AddHelpMenuItem> items)
        {
            List<int> visibilties = new List<int> { 1, 2, 3 };
            List<Guid> uids = new List<Guid>();
            List<HelpMenuItemMessage> result = new List<HelpMenuItemMessage>();

            try
            {
                foreach (var item in items)
                {
                    if (item.Name == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpName)).ConfigureAwait(false);
                    }
                    if (item.Name.Trim() == "")
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpName)).ConfigureAwait(false);
                    }
                    if (item.Name.Length > 500)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpNameLength)).ConfigureAwait(false);
                    }
                    if (item.Url == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpUrl)).ConfigureAwait(false);
                    }
                    if (item.Url.Trim() == "")
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpUrl)).ConfigureAwait(false);
                    }
                    if (item.Url.Length > 2000)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpUrlLength)).ConfigureAwait(false);
                    }
                    if (!visibilties.Contains(item.visibility))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.HelpMenuVisibilityError)).ConfigureAwait(false);
                    }
                    if (item.order < 0)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.HelpMenuOrderError)).ConfigureAwait(false);
                    }

                    var uid = Guid.NewGuid();
                    uids.Add(uid);
                    Company.HelpResources.Add(new HelpResource
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Url = item.Url,
                        uid = uid,
                        isEditable = true,
                        visibility = item.visibility,
                        order = item.order,
                        isSystem = false
                    });
                }

                Company.SaveChanges();
                foreach (var i in uids)
                {
                    result.Add(new HelpMenuItemMessage { uid = i, title = ApiMessages.HelpMenuItemsCreated, message = ApiMessages.HelpItemsAdded });
                }
                return await Task.FromResult<IHttpActionResult>(
                            ResponseMessage(
                                Request.CreateResponse(
                                    HttpStatusCode.OK, result
                                )
                            )
                        ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates a list of help menu items.
        /// </summary>
        /// <returns></returns>
        [
           HttpPut,
           MapToApiVersion("2.0"),
           Route("help"),
           SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.OK, "Updates already exisiting help menu items.", typeof(List<HelpMenuItemMessage>)),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> UpdateHelpMenuItems(List<UpdateHelpMenuItem> items)
        {
            List<int> visibilties = new List<int> { 1, 2, 3 };
            List<Guid> uids = new List<Guid>();
            List<HelpMenuItemMessage> result = new List<HelpMenuItemMessage>();

            try
            {
                foreach (var item in items)
                {
                    HelpResource helpItem = Company.HelpResources.Where(x => x.uid == item.uid).FirstOrDefault();

                    if (item.Name == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpName)).ConfigureAwait(false);
                    }
                    if (item.Name.Trim() == "")
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpName)).ConfigureAwait(false);
                    }
                    if (item.Name.Length > 500)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpNameLength)).ConfigureAwait(false);
                    }
                    if (item.Url == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpUrl)).ConfigureAwait(false);
                    }
                    if (item.Url.Trim() == "")
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpUrl)).ConfigureAwait(false);
                    }
                    if (item.Url.Length > 2000)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidHelpUrlLength)).ConfigureAwait(false);
                    }
                    if (!visibilties.Contains(item.visibility))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.HelpMenuVisibilityError)).ConfigureAwait(false);
                    }
                    if (item.order < 0)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.HelpMenuOrderError)).ConfigureAwait(false);
                    }

                    if (helpItem != null)
                    {
                        uids.Add((Guid)helpItem.uid);
                        helpItem.Description = item.Description;
                        helpItem.Name = item.Name;
                        helpItem.order = item.order;
                        helpItem.visibility = item.visibility;
                        if (!helpItem.isSystem)
                        {
                            helpItem.Url = item.Url;
                        }
                    }
                }

                Company.SaveChanges();
                if (uids.Count > 0)
                {
                    foreach (var i in uids)
                    {
                        result.Add(new HelpMenuItemMessage { uid = i, title = ApiMessages.HelpMenuItemsUpdated, message = ApiMessages.HelpMenuSuccess });
                    }
                    return await Task.FromResult<IHttpActionResult>(
                                ResponseMessage(
                                    Request.CreateResponse(
                                        HttpStatusCode.OK, result
                                    )
                                )
                            ).ConfigureAwait(false);
                }
                else
                {
                    result.Add(new HelpMenuItemMessage { uid = Guid.Empty, title = ApiMessages.BadRequest, message = ApiMessages.InvalidHelpUpdateUid });
                    return await Task.FromResult<IHttpActionResult>(
                                ResponseMessage(
                                    Request.CreateResponse(
                                        HttpStatusCode.OK, result
                                    )
                                )
                            ).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes a list of help menu items.
        /// </summary>
        /// <returns></returns>
        [
           HttpDelete,
           MapToApiVersion("2.0"),
           Route("help"),
           SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.OK, "Deletes currently created help menu items.", typeof(List<HelpMenuItemMessage>)),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> DeleteHelpMenuItems(List<DeleteMenuItem> items)
        {
            List<Guid> uids = new List<Guid>();
            List<HelpMenuItemMessage> result = new List<HelpMenuItemMessage>();

            try
            {
                foreach (var item in items)
                {
                    var helpItem = Company.HelpResources.Where(x => x.uid == item.uid).FirstOrDefault();
                    if (helpItem != null && helpItem.isSystem)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ErrorDeletingDefaultHelpItem)).ConfigureAwait(false);
                    }
                    if (helpItem != null && !helpItem.isSystem)
                    {
                        uids.Add(item.uid);
                        Company.HelpResources.Remove(helpItem);
                    }
                }

                Company.SaveChanges();
                if (uids.Count > 0)
                {
                    foreach (var i in uids)
                    {
                        result.Add(new HelpMenuItemMessage { uid = i, title = ApiMessages.HelpMenuItemsDeleted, message = ApiMessages.HelpItemsDeleted });
                    }
                    return await Task.FromResult<IHttpActionResult>(
                                ResponseMessage(
                                    Request.CreateResponse(
                                        HttpStatusCode.OK, result
                                    )
                                )
                            ).ConfigureAwait(false);
                }
                else
                {
                    result.Add(new HelpMenuItemMessage { uid = Guid.Empty, title = ApiMessages.BadRequest, message = ApiMessages.InvalidHelpDeleteUid });
                    return await Task.FromResult<IHttpActionResult>(
                                ResponseMessage(
                                    Request.CreateResponse(
                                        HttpStatusCode.OK, result
                                    )
                                )
                            ).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, e.Message)).ConfigureAwait(false);
            }
        }

        #endregion


        #region Theme Endpoints

        const string THEME_UID_FILTER_PARAMETER = "An optional unique identifier of the theme, to limit this list to a specific theme.";
        const string THEME_NOT_FOUND = "The theme was not found based on the provided unique identifier.";

        /// <summary>
        /// Gets a list of themes in an environment.
        /// </summary>
        /// <returns>A list of themes defined in your environment.</returns>
        [
            HttpGet,
            Route("themes"),
            SwaggerProduces("application/json"),
            SwaggerParameter("uid", THEME_UID_FILTER_PARAMETER, DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of themes.", typeof(List<GetTheme>)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetThemes(CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var apiModels = await ThemeRepository.GetThemesAsync(queryParams, cancellationToken);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apiModels));
            }
            catch (GenericException)
            {
                throw;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnGetMany, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Retrieves the generated CSS stylesheet for the current theme that contains variables used by other stylesheets within Govern.
        /// </summary>
        [
            HttpGet,
            Route("themes/current.css"),
            SwaggerProduces("text/css"),
            SwaggerResponse(HttpStatusCode.OK, "Returns CSS for the current theme.", typeof(string)),
            SwaggerResponse(HttpStatusCode.NotFound, "No themes exist or none are set as the current theme.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetCurrentThemeCss()
        {
            try
            {
                var theme = await ThemeRepository.GetCurrentThemeByUserAsync();

                string textColorFromBackground(string backgroundColor)
                {
                    Color col = ColorTranslator.FromHtml(backgroundColor);
                    if (col.R * 0.2126 + col.G * 0.7152 + col.B * 0.0722 < 255 / 2)
                    {
                        return "white";
                    }
                    return "black";
                }

                if (theme == null)
                {
                    throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnGet, ThemeErrors.NoActiveThemeExists);
                }

                var css = new StringBuilder();
                css.AppendLine(":root {");
                css.AppendCssVariable("backColor", theme.BackColor);
                css.AppendCssVariable("breadcrumbLinkColor", theme.BreadcrumbLinkColor);

                css.AppendCssVariable("buttonBackColor", theme.ButtonBackColor);
                css.AppendCssVariable("calculatedButtonTextColor", textColorFromBackground(theme.ButtonBackColor));

                css.AppendCssVariable("headerBackColor", theme.HeaderBackColor);
                css.AppendCssVariable("calculatedHeaderTextColor", textColorFromBackground(theme.HeaderBackColor));

                css.AppendCssVariable("navbarBackColor", theme.NavBarBackColor);
                css.AppendCssVariable("calculatedNavbarTextColor", textColorFromBackground(theme.NavBarBackColor));

                css.AppendCssVariable("navbarBackColorSelected", theme.NavBarBackSelectedColor);
                css.AppendCssVariable("calculatedNavbarSelectedTextColor", textColorFromBackground(theme.NavBarBackSelectedColor));

                css.AppendCssVariable("primaryButtonBackColor", theme.PrimaryButtonBackColor);
                css.AppendCssVariable("calculatedPrimaryButtonTextColor", textColorFromBackground(theme.PrimaryButtonBackColor));

                css.AppendCssVariable("tableHeaderBackColor", theme.TableHeaderBackColor);
                css.AppendCssVariable("tableRowBackColor", theme.TableRowBackSelectedColor);
                css.AppendCssVariable("tabLinkColor", theme.TabLinkColor);


                css.AppendLine("}");

                var customCss = ThemeRepository.GetCurrentThemeCustomCssByUser();
                css.AppendLine("");
                css.Append(customCss);

                return ResponseMessage(
                    new HttpResponseMessage
                    {
                        Content = new StringContent(css.ToString(), Encoding.UTF8, "text/css"),
                        StatusCode = HttpStatusCode.OK
                    });
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnGetMany, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Retrieves the generated CSS stylesheet for the theme (based on the provided Uid) that contains variables used by other stylesheets within Govern.
        /// </summary>
        /// <param name="uid">The unique identifier of the theme.</param>
        [
            HttpGet,
            Route("themes/{uid:Guid}.css"),
            SwaggerProduces("text/css"),
            SwaggerResponse(HttpStatusCode.OK, "Returns CSS for the specified theme.", typeof(string)),
            SwaggerResponse(HttpStatusCode.NotFound, THEME_NOT_FOUND, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult GetThemeCssByUid(Guid uid)
        {
            try
            {
                var theme = ThemeRepository.GetThemeByUid(uid);

                if (theme == null)
                {
                    throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnGet, ThemeErrors.ThemeWithUidNotFound);
                }

                var css = new StringBuilder();
                css.AppendLine(":root {");
                css.AppendCssVariable("backColor", theme.BackColor);
                css.AppendCssVariable("breadcrumbLinkColor", theme.BreadcrumbLinkColor);
                css.AppendCssVariable("buttonBackColor", theme.ButtonBackColor);
                css.AppendCssVariable("headerBackColor", theme.HeaderBackColor);
                css.AppendCssVariable("navbarBackColor", theme.NavBarBackColor);
                css.AppendCssVariable("navbarBackColorSelected", theme.NavBarBackSelectedColor);
                css.AppendCssVariable("primaryButtonBackColor", theme.PrimaryButtonBackColor);
                css.AppendCssVariable("tableHeaderBackColor", theme.TableHeaderBackColor);
                css.AppendCssVariable("tableRowBackColor", theme.TableRowBackSelectedColor);
                css.AppendCssVariable("tabLinkColor", theme.TabLinkColor);
                css.AppendLine("}");

                css.AppendLine("");
                css.Append(theme.CustomCss + "");

                return ResponseMessage(
                    new HttpResponseMessage
                    {
                        Content = new StringContent(css.ToString(), Encoding.UTF8, "text/css"),
                        StatusCode = HttpStatusCode.OK
                    });
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnGet, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Retrieves the generated CSS stylesheet for the theme (based on the provided Uid) that contains variables used by other stylesheets within Govern.
        /// </summary>
        /// <param name="uid">The unique identifier of the theme.</param>
        [
            HttpGet,
            Route("themes/{uid:Guid}/custom.css"),
            SwaggerProduces("text/css"),
            SwaggerResponse(HttpStatusCode.OK, "Returns custom CSS for the specified theme.", typeof(string)),
            SwaggerResponse(HttpStatusCode.NotFound, "Theme does not exist, or does not contain custom Css.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "Feature is not enabled.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult GetThemeCustomCssByUid(Guid uid)
        {
            try
            {
                // FeatureFlag Check
                var isCustomCssEnabled = Ld.BoolVariation(FeatureFlags.PERM_BRANDING_CUSTOM_CSS, GetSdkFeatureFlagUser(), false);
                if (!isCustomCssEnabled)
                {
                    throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ErrorOnGet, ThemeErrors.CustomCssNotAllowed);
                }


                var theme = ThemeRepository.GetThemeByUid(uid);
                if (theme == null)
                {
                    throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnGet, ThemeErrors.ThemeWithUidNotFound);
                }

                if (string.IsNullOrEmpty(theme.CustomCss))
                {
                    throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnGet, ThemeErrors.CustomCssNotFound);
                }

                return ResponseMessage(
                    new HttpResponseMessage
                    {
                        Content = new StringContent(theme.CustomCss, Encoding.UTF8, "text/css"),
                        StatusCode = HttpStatusCode.OK
                    });
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnGet, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Retrieves the generated SVG thumbnail for the theme based on the provided Uid.
        /// </summary>
        /// <param name="uid">The unique identifier of the theme.</param>
        [
            HttpGet,
            Route("themes/{uid:Guid}.svg"),
            SwaggerProduces("application/svg+xml", "image/svg+xml"),
            SwaggerResponse(HttpStatusCode.OK, "Returns an SVG thumbnail for the specified theme.", typeof(string)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse)),
            SwaggerParameter("width", "Desired width of SVG file", DataType = "integer", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetThemeSvgByUid(Guid uid)
        {
            var queryParams = Request.GetQueryNameValuePairs();

            int width = queryParams.Any(q => q.Key == "width") ? Math.Abs(int.Parse(queryParams.ToList().FirstOrDefault(q => q.Key == "width").Value)) : 330;
            int height = (int)Math.Round(175.0 / 330.0 * width);
            bool returnAsXml = Request.Headers.Accept.Any(h => h.MediaType == "application/svg+xml");

            try
            {
                var theme = ThemeRepository.GetThemeByUid(uid);

                if (theme == null)
                {
                    throw new GenericException(HttpStatusCode.NotFound, ThemeErrors.ErrorOnGet, ThemeErrors.ThemeWithUidNotFound);
                }

                var svg = new StringBuilder();
                svg.Append($@"<svg version=""1.2"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 330 175"" width=""{width}"" height=""{height}"">
	<title>Govern Theme Thumbnail</title>
	<defs>
		<filter x=""-50%"" y=""-50%"" width=""200%"" height=""200%"" id=""f1"" ><feDropShadow dx=""-1.8369701987210297e-16"" dy=""3"" stdDeviation=""2.916666666666667"" flood-color=""#000000"" flood-opacity="".2""/></filter>
	</defs>
	<style>
");
                svg.AppendLine($".s-main-bck {{ filter: url(#f1);fill: {theme.BackColor} }} ");
                svg.AppendLine($".s-header-bck {{ fill: {theme.HeaderBackColor} }} ");
                svg.AppendLine($".s-breadcrumb-txt {{ fill: {BlackOrWhite(theme.HeaderBackColor)} }} ");
                svg.AppendLine($".s-breadcrumb-lnk {{ fill: {theme.BreadcrumbLinkColor} }} ");
                svg.AppendLine($".s-actionbtn-txt {{ fill: {BlackOrWhite(theme.ButtonBackColor)} }} ");
                svg.AppendLine($".s-actionbtn-bck {{ fill: {theme.ButtonBackColor} }} ");
                svg.AppendLine($".s-button-txt {{ fill: #222222 }} ");
                svg.AppendLine($".s-button-bck {{ fill: #f1f2f3 }}");
                svg.AppendLine($".s-button-active-txt {{ fill: {BlackOrWhite(theme.PrimaryButtonBackColor)} }} ");
                svg.AppendLine($".s-button-active-bck {{ fill: {theme.PrimaryButtonBackColor} }} ");
                svg.AppendLine($".s-nav-bck {{ filter: url(#f1);fill: {theme.NavBarBackColor} }} ");
                svg.AppendLine($".s-nav-item {{ fill: {BlackOrWhite(theme.NavBarBackColor)} }} ");
                svg.AppendLine($".s-nav-item-selected {{ fill: {BlackOrWhite(theme.NavBarBackSelectedColor)} }} ");
                svg.AppendLine($".s-nav-bck-selected {{ fill: {theme.NavBarBackSelectedColor} }} ");
                svg.AppendLine($".s-tab {{ fill: {theme.TabLinkColor} }} ");
                svg.AppendLine($".s-tab-active {{ fill: #1e2435 }} ");
                svg.AppendLine($".s-table-bck {{ fill: #ffffff }} ");
                svg.AppendLine($".s-table-hdr-txt {{ fill: {BlackOrWhite(theme.TableHeaderBackColor)} }} ");
                svg.AppendLine($".s-table-hdr-bck {{ fill: {theme.TableHeaderBackColor} }}");
                svg.AppendLine($".s-table-row-selected {{ fill: {theme.TableRowBackSelectedColor} }} ");
                svg.AppendLine($".s-pill-green {{ fill: #00853e }} ");
                svg.AppendLine($".s-pill-yellow {{ fill: #ffaa01 }} ");
                svg.AppendLine($".s-pill-red {{ fill: #d11947 }} ");

                svg.Append(@"	</style>
	<g id=""Thumbnail"">
		<path id=""Background"" class=""s-main-bck"" d=""m10 6h310v164h-310z"" />
		<path id=""Navbar"" class=""s-nav-bck"" d=""m11 19h65v151h-65z"" />
		<g id=""Headerbar"">
			<path id=""HeaderBackground"" class=""s-header-bck"" d=""m11 5h310v14h-310z"" />
			<path id=""ActionButton"" class=""s-actionbtn-bck"" d=""m247 7h26v10h-26z"" />
			<path id=""ActionButtonTxt"" class=""s-actionbtn-txt"" d=""m252 11h16v2h-16z"" />
			<path id=""Breadcrumb"" class=""s-breadcrumb-txt"" d=""m76 11h23v2h-23z"" />
			<path id=""BreadcrumbLink"" class=""s-breadcrumb-lnk"" d=""m106 11h24v2h-24z"" />
		</g>
		<g id=""NavBullets"">
			<path id=""NavBullet 8"" class=""s-nav-item"" d=""m17 117h3v3h-3z"" />
			<path id=""Navbullet 6"" class=""s-nav-item"" d=""m17 92h3v3h-3z"" />
			<path id=""NavBullet 5"" class=""s-nav-item"" d=""m17 80h3v3h-3z"" />
			<path id=""NavBullet 4"" class=""s-nav-item"" d=""m17 67h3v3h-3z"" />
			<path id=""NavBullet 3"" class=""s-nav-item"" d=""m17 54h3v3h-3z"" />
			<path id=""NavBullet 2"" class=""s-nav-item"" d=""m17 41h3v3h-3z"" />
			<path id=""NavBullet 1"" class=""s-nav-item"" d=""m17 28h3v3h-3z"" />
		</g>
		<g id=""NavItems"">
			<path id=""NavItem 8"" class=""s-nav-item"" d=""m29 117h39v3h-39z"" />
			<path id=""NavItem 6"" class=""s-nav-item"" d=""m29 92h39v3h-39z"" />
			<path id=""NavItem 5"" class=""s-nav-item"" d=""m29 80h39v3h-39z"" />
			<path id=""NavItem 4"" class=""s-nav-item"" d=""m29 67h39v3h-39z"" />
			<path id=""NavItem 3"" class=""s-nav-item"" d=""m29 54h39v3h-39z"" />
			<path id=""NavItem 2"" class=""s-nav-item"" d=""m29 41h39v3h-39z"" />
			<path id=""NavItem 1"" class=""s-nav-item"" d=""m29 28h39v3h-39z"" />
		</g>
		<g id=""SelectedNav"">
			<path id=""SelectedBackground"" class=""s-nav-bck-selected"" d=""m11 102h65v9h-65z"" />
			<path id=""NavBullet 7"" class=""-nav-item-selected"" d=""m17 105h3v3h-3z"" />
			<path id=""NavItem 7"" class=""-nav-item-selected"" d=""m29 105h39v3h-39z"" />
		</g>
		<path id=""Tab 2"" class=""s-tab"" d=""m112 45h24v2h-24z"" />
		<path id=""Tab 3"" class=""s-tab"" d=""m144 45h24v2h-24z"" />
		<path id=""Active Tab"" class=""s-tab-active"" d=""m80 45h24v2h-24z"" />
		<g id=""Table"">
			<path id=""TableBackground"" class=""s-table-bck"" d=""m78 52h241v107h-241z"" />
			<path id=""TableHeaderBackground"" class=""s-table-hdr-bck"" d=""m85 55h229v8h-229z"" />
			<path id=""Header 4"" class=""s-table-hdr-txt"" d=""m270 58h23v2h-23z"" />
			<path id=""Header 3"" class=""s-table-hdr-txt"" d=""m216 58h24v2h-24z"" />
			<path id=""Header 2"" class=""s-table-hdr-txt"" d=""m153 58h24v2h-24z"" />
			<path id=""Header 1"" class=""s-table-hdr-txt"" d=""m90 58h23v2h-23z"" />
			<path id=""SelectedRow"" class=""s-table-row-selected"" d=""m85 85h229v8h-229z"" />
			<path id=""Status Green"" class=""s-pill-green"" d=""m220 74c0-1.7 1.3-3 3-3h9c1.7 0 3 1.3 3 3c0 1.7-1.3 3-3 3h-9c-1.7 0-3-1.3-3-3z"" />
			<path id=""Status Yellow"" class=""s-pill-yellow"" d=""m220 89c0-1.7 1.3-3 3-3h9c1.7 0 3 1.3 3 3c0 1.7-1.3 3-3 3h-9c-1.7 0-3-1.3-3-3z"" />
			<path id=""Status Red"" class=""s-pill-red"" d=""m220 106c0-1.7 1.3-3 3-3h9c1.7 0 3 1.3 3 3c0 1.7-1.3 3-3 3h-9c-1.7 0-3-1.3-3-3z"" />
		</g>
		<g id=""Buttons"">
			<path id=""ActiveButtonBackground"" class=""s-button-active-bck"" d=""m289 145h26v10h-26z"" />
			<path id=""ButtonBackground"" class=""s-button-bck"" d=""m82 145h26v10h-26z"" />
			<path id=""ActiveButtonText"" class=""s-button-active-txt"" d=""m294 149h16v2h-16z"" />
			<path id=""ButtonText"" class=""s-button-txt"" d=""m87 149h16v2h-16z"" />
		</g>
	</g>
</svg>
");

                return ResponseMessage(
                    new HttpResponseMessage
                    {
                        Content = new StringContent(svg.ToString(), Encoding.UTF8, returnAsXml ? "application/svg+xml" : "image/svg+xml"),
                        StatusCode = HttpStatusCode.OK
                    });
            }
            catch (GenericException ex)
            {
                if (returnAsXml)
                {
                    throw;
                }
                else
                {
                    return ResponseMessage(
                        new HttpResponseMessage
                        {
                            Content = new StringContent(ErrorAsSvg(ex.StatusMessage, width, height), Encoding.UTF8, "image/svg+xml"),
                            StatusCode = HttpStatusCode.OK
                        }
                    );
                }
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnGet, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Creates a theme.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="requestModel">An object containing the properties of the theme you want to create. See the example model for a list of all available properties.</param>
        /// <returns>The created theme.</returns>
        [
            HttpPost,
            Route("themes"),
            SwaggerConsumes("application/json"),
            SwaggerProduces("application/json"),
            SwaggerResponseRemoveDefaults,
            SwaggerResponse(HttpStatusCode.Created, "Returns the created theme.", typeof(GetTheme)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to insert the theme is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostTheme(PostTheme requestModel, [FromUri] bool validationOnly = false)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ThemeErrors.ErrorOnCreate, ApiMessages.EndpointNotAuthorizedMessage);
                }

                if (!string.IsNullOrEmpty(requestModel.CustomCss))
                {
                    // FeatureFlag Check
                    var isCustomCssEnabled = Ld.BoolVariation(FeatureFlags.PERM_BRANDING_CUSTOM_CSS, GetSdkFeatureFlagUser(), false);
                    if (!isCustomCssEnabled)
                    {
                        throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ErrorOnCreate, ThemeErrors.CustomCssNotAllowed);
                    }
                }

                var responseModel = await ThemeRepository.PostThemeAsync(requestModel, validationOnly);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, responseModel));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnCreate, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Updates a theme based on the provided Uid.
        /// </summary>
        /// <remarks>
        /// If you leave any properties as null or not present, those properties will be cleared out from the theme you are updating.
        /// </remarks>
        /// <param name="uid">The unique identifier of the theme.</param>
        /// <param name="requestModel">An object containing the properties of the theme you want to update. See the example model for a list of all available properties.</param>
        /// <returns>The updated theme.</returns>
        [
            HttpPut,
            Route("themes/{uid:Guid}"),
            SwaggerConsumes("application/json"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the updated theme.", typeof(GetTheme)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, THEME_NOT_FOUND, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to update the theme is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutTheme(Guid uid, PutTheme requestModel)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ThemeErrors.ErrorOnUpdate, ApiMessages.EndpointNotAuthorizedMessage);
                }

                if (!string.IsNullOrEmpty(requestModel.CustomCss))
                {
                    // FeatureFlag Check
                    var isCustomCssEnabled = Ld.BoolVariation(FeatureFlags.PERM_BRANDING_CUSTOM_CSS, GetSdkFeatureFlagUser(), false);
                    if (!isCustomCssEnabled)
                    {
                        throw new GenericException(HttpStatusCode.Conflict, ThemeErrors.ErrorOnUpdate, ThemeErrors.CustomCssNotAllowed);
                    }
                }

                var reponseModel = await ThemeRepository.PutThemeAsync(uid, requestModel);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, reponseModel));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnUpdate, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Makes the selected theme the current one.
        /// </summary>
        /// <param name="uid">The unique identifier of the theme.</param>
        /// <returns>An Http Status code.</returns>
        [
            HttpPatch,
            Route("themes/{uid:Guid}/current"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, SUCCESS_MESSAGE, typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, THEME_NOT_FOUND, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to update the theme is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> MarkThemeAsCurrent(Guid uid)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ThemeErrors.ErrorOnUpdate, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var success = await ThemeRepository.MarkThemeAsCurrentAsync(uid);
                if (success)
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ConfirmResponse { message = "Theme marked as current." }));
                }
                else
                {
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, new ErrorResponse { message = "Unable to mark theme as current." }));
                }
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnUpdate, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Deletes a theme, provided it is not set as the current theme of the environment.
        /// </summary>
        /// <param name="uid">The unique identifier of the theme.</param>
        /// <returns>A confirmation response.</returns>
        [
            HttpDelete,
            Route("themes/{uid:Guid}"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, SUCCESS_MESSAGE, typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, THEME_NOT_FOUND, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "Request to remove this theme is invalid, possibly due to being set as the current theme.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteTheme(Guid uid)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ThemeErrors.ErrorOnDelete, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var status = ThemeRepository.Delete(uid);

                return ResponseMessage(Request.CreateResponse(status, new ConfirmResponse { message = "Theme removed." }));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ThemeErrors.ErrorOnDelete, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Helper endpoint to convert a CSS block to a UTF-8 Base64 string.
        /// </summary>
        [
            HttpPut,
            ApiExplorerSettings(IgnoreApi = true),
            Route("themes/conversion/base64"),
            SwaggerConsumes("text/css"), SwaggerProduces("text/plain"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding theme.", typeof(string)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to insert the theme is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> ConvertCssToBase64()
        {
            const string ERROR_HEADING = "Error converting css";

            try
            {
                string css = await Request.Content.ReadAsStringAsync();

                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var cssBytes = System.Text.Encoding.UTF8.GetBytes(css);
                var responseModel = Convert.ToBase64String(cssBytes);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseModel));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Helper endpoint to convert an image file to a DataUrl string block.
        /// </summary>
        [
            HttpPut,
            ApiExplorerSettings(IgnoreApi = true),
            Route("themes/conversion/dataurl"),
            SwaggerParameter("file", "File to be uploaded", DataType = "file", ParameterType = "formData", Required = true),
            SwaggerProduces("text/plain"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding theme.", typeof(string)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to insert the theme is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> ConvertImageToDataUrl()
        {
            const string ERROR_HEADING = "Error converting css";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var c = await Request.Content.ReadAsMultipartAsync();
                var file = c.Contents.Where(x => x.Headers?.ContentDisposition?.Parameters.Any(param => param?.Value.Contains("file") == true) == true).FirstOrDefault();

                if (file == null)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You must provide an image file to convert.");
                }

                byte[] bytes = await file.ReadAsByteArrayAsync();
                string extension = Path.GetExtension(file.Headers.ContentDisposition.FileName.ToString().Replace("\"", ""));

                var goodExtensions = new List<string> { ".gif", ".ico", ".jpg", ".png" };
                if (!goodExtensions.Contains(extension))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You must provide a valid image file.");
                }

                var dataUri = bytes.GetDataUrlFromStream(extension);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, dataUri));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Helper endpoint to convert an theme images to base64 string.
        /// </summary>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            Route("themes/{uid}/base64data"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding theme.", typeof(string)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to insert the theme is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> Base64Images(Guid uid)
        {
            const string ERROR_HEADING = "Error converting css";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var theme = Company.Themes.AsNoTracking().FirstOrDefault(x => x.Uid == uid);
                var response = new ThemeBase64Data();
                if (theme.HomePageBackgroundExtension != null)
                {
                    using (var stream = new MemoryStream())
                    {
                        var url = $"{Company.CurrentCompanyID}/{theme.Uid.ToString().ToLowerInvariant()}_background{theme.HomePageBackgroundExtension}";
                        await _storage.GetFileStream("themes", url, stream);
                        response.HomeBackground = stream.ToArray().GetDataUrlFromStream(theme.HomePageBackgroundExtension);
                    }
                }

                if (theme.BrowserIconExtension != null)
                {
                    using (var stream = new MemoryStream())
                    {

                        var url = $"{Company.CurrentCompanyID}/{theme.Uid.ToString().ToLowerInvariant()}_icon{theme.BrowserIconExtension}";
                        await _storage.GetFileStream("themes", url, stream);
                        response.Icon = stream.ToArray().GetDataUrlFromStream(theme.BrowserIconExtension);
                    }
                }

                if (theme.HeaderLogoExtension != null)
                {
                    using (var stream = new MemoryStream())
                    {
                        var url = $"{Company.CurrentCompanyID}/{theme.Uid.ToString().ToLowerInvariant()}_logo{theme.HeaderLogoExtension}";
                        await _storage.GetFileStream("themes", url, stream);
                        response.HeaderLogo = stream.ToArray().GetDataUrlFromStream(theme.HeaderLogoExtension);
                    }
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        #endregion

        private bool IsDark(string htmlColor)
        {
            Color color = ColorTranslator.FromHtml(htmlColor);
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
            return luminance < 128;
        }

        private string BlackOrWhite(string htmlColor)
        {
            return IsDark(htmlColor) ? "#ffffff" : "#222222";
        }

        private string ErrorAsSvg(string message, int width = 330, int height = 175)
        {
            int partitionSize = 4;
            List<string> words = message.Split(' ').ToList();
            var svg = new StringBuilder();
            svg.AppendLine($@"<svg version=""1.2"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 330 175"" width=""{width}"" height=""{height}"">");
            svg.AppendLine(@"<text lengthAdjust=""spacing"" x=""5"" y=""20"">");

            for (int i = 0; i < Math.Ceiling((double)words.Count / partitionSize); i++)
            {
                var offset = i * partitionSize;
                var line = string.Join(" ", words.GetRange(offset, Math.Min(partitionSize, words.Count - offset)));
                svg.AppendLine($@"<tspan textLength=""320"" x=""0"" dy=""{i}em"" font-size=""1.6em"">{HttpUtility.HtmlEncode(line)}</tspan>");
            }

            svg.AppendLine("</text>");
            svg.AppendLine("</svg>");
            return svg.ToString();
        }
    }
}