using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
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
        public EnvironmentController(ICommunityContext community, ICompanyContext company, IStorageProvider storage) : base(community, company)
        {
            _storage = storage;
        }

        [HttpGet, AjaxValidateAntiForgeryToken, Route("rebuilds"), ApiExplorerSettings(IgnoreApi = true)]
        public async Task<HttpResponseMessage> GetRebuilds()
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin) return ReturnApiError(HttpStatusCode.Forbidden, "User not authorized to perfom this action");
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
                if (!Company.CurrentResourceIsAdmin) return ReturnApiError(HttpStatusCode.Forbidden, "User not authorized to perfom this action");
                if (model == null) return ReturnApiError(HttpStatusCode.BadRequest, "No valid request present.");

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
                return ReturnApiError(HttpStatusCode.Forbidden, "User not authorized to perfom this action");
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
        public HttpResponseMessage UpdateStyleCustomizations(UpdateCss UpdateCss)
        {
            if (!Company.CurrentResourceIsAdmin)
                return ReturnApiError(HttpStatusCode.Forbidden, "You do not have permissions to update this.");

            //delete the old css file
            try
            {
                _storage.DeleteFile(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css");
            }
            catch { }

            try
            {
                var settings = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

                var stylesSetting = settings.SingleOrDefault(i => i.SettingID == 24);
                //if the css is not empty or null create a new css
                if (!string.IsNullOrWhiteSpace(UpdateCss.css))
                {
                    //update the company setting to say where the files is 

                    if (stylesSetting == null)
                    {
                        stylesSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 24, Value = $"{constants.COMPANY_STYLES_URL}{Company.CurrentCompanyID}.css" };
                        Community.Add(stylesSetting);
                    }
                    else
                    {
                        stylesSetting.Value = $"{constants.COMPANY_STYLES_URL}{Company.CurrentCompanyID}.css";
                        Community.SaveChanges();
                    }

                    _storage.CreateFile(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css", UpdateCss.css, "text/css", false);
                }
                else
                {
                    Community.Delete<CompanySetting>(stylesSetting);
                }
            }
            catch { }

            return Request.CreateResponse(HttpStatusCode.OK, "Syles successfully updated.");
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
                return ReturnApiError(HttpStatusCode.Forbidden, "User not authorized to perfom this action");
            }

            var queryParams = Request.GetQueryNameValuePairs();
            var _settingId = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_settingid").Value;
            int? settingId = null;
            if (!string.IsNullOrEmpty(_settingId))
            {
                if (!int.TryParse(_settingId, out int val) || val <= 0)
                    return ReturnApiError(HttpStatusCode.BadRequest, "Value passed for _settingId is not valid");
                else
                    settingId = val;
            }

            try
            {
                var companySettings = Community.Query<SettingModel>(
                    $@"select    S.ID as SettingID, 
                                S.Name, 
                                S.FieldName, 
                                S.Description, 
                                coalesce(C.Value, S.DefaultValue) as Value
                    from        Setting S 
                                left join CompanySetting C on C.SettingID = S.ID and C.CompanyID = @c
                    {(settingId.HasValue ? "where S.ID = @settingId" : "")}", new { c = Company.CurrentCompanyID, settingId })
                    .ToDictionary(k => k.FieldName, v => v.Value);



                var settings = Community
                    .Settings
                    .AsEnumerable();

                if (settingId.HasValue)
                    settings = settings.Where(s => s.ID == settingId);

                if (settingId.HasValue && settings.Count() == 0)
                {
                    return ReturnApiError(HttpStatusCode.NotFound, "Setting with this id not found");
                }

                var response = settings.Select(s => new CompanySettingApiModel(s, companySettings[s.FieldName]));
   

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
                return ReturnApiError(HttpStatusCode.Forbidden, "User not authorized to perfom this action");
            }

            if (model == null)
                return ReturnApiError(HttpStatusCode.BadRequest, "Invalid model");

            try
            {
                var setting = Community
                    .Settings
                    .FirstOrDefault(s => s.ID == model.SettingID);

                if (setting == null)
                    return ReturnApiError(HttpStatusCode.NotFound, "Setting with this id not found");

                if (setting.Locked)
                    return ReturnApiError(HttpStatusCode.Forbidden, "This setting is locked and cannot be updated");

                if (!model.HasExactlyOneValue)
                    return ReturnApiError(HttpStatusCode.BadRequest, "Exactly one value must be provided based on the setting's data type");


                var companySetting = Community
                    .CompanySettings
                    .FirstOrDefault(c => c.CompanyID == Company.CurrentCompanyID && c.SettingID == model.SettingID);

                bool clearSetting = false;
                string value = "";

                string valueErrorMessage = "Provided value does not match the expected data type for this setting";
                switch (setting.SettingType)
                {
                    case SettingType.Text:
                        if (model.StringSetting == null)
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        if (model.StringSetting.Value == null)
                            clearSetting = true;
                        value = model.StringSetting.Value;
                        break;
                    case SettingType.Number:
                        if (model.NumberSetting == null)
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
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
                                return ReturnApiError(HttpStatusCode.BadRequest, "Provided value is not a valid number");
                            }
                        }
                        break;
                    case SettingType.Boolean:
                        if (model.BooleanSetting == null)
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
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
                                return ReturnApiError(HttpStatusCode.BadRequest, "Provided value is not a valid boolean");
                            }
                        }

                        break;
                    case SettingType.IPAddress:
                        if (model.IpAddressSetting == null)
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        if (model.IpAddressSetting.Value == null || model.IpAddressSetting.Value.Count == 0)
                            clearSetting = true;
                        
                        if (model.IpAddressSetting.Value?.Any() ?? false)
                        {
                            value = "<ips />";
                            var xml = new XElement("ips");
                            foreach (var ip in model.IpAddressSetting.Value)
                            {
                                if (string.IsNullOrEmpty(ip.Name) || string.IsNullOrEmpty(ip.Start) || string.IsNullOrEmpty(ip.End))
                                    return ReturnApiError(HttpStatusCode.BadRequest, "One or more IP Addresses is missing a value");
                                if (!IPAddress.TryParse(ip.Start, out IPAddress _))
                                    return ReturnApiError(HttpStatusCode.BadRequest, $"Start value {ip.Start} is not a valid IP Address");
                                if (!IPAddress.TryParse(ip.End, out IPAddress _))
                                    return ReturnApiError(HttpStatusCode.BadRequest, $"End value {ip.End} is not a valid IP Address");

                                xml.Add(new XElement("ip",
                                    new XElement("name", ip.Name),
                                    new XElement("start", ip.Start),
                                    new XElement("end", ip.End)
                                ));

                            }

                            value = xml.ToString();
                        }
                        break;
                }



                //sanitize allowed CORS origins
                if (setting.ID == 76 && !string.IsNullOrEmpty(value))
                {
                    value = string.Join(",", value
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
                        .ToList());
                }

                if (clearSetting && companySetting != null)
                {
                    Community.CompanySettings.Remove(companySetting);
                }
                else if (!clearSetting)
                {
                    if (companySetting == null)
                    {
                        companySetting = new CompanySetting
                        {
                            CompanyID = Company.CurrentCompanyID,
                            SettingID = model.SettingID,
                            Value = value
                        };

                        Community.CompanySettings.Add(companySetting);
                    }
                    else
                    {
                        companySetting.Value = value;
                    }
                }

                Community.SaveChanges();
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
            SwaggerConsumes("application/json")
        ]
        public async Task<IHttpActionResult> GetOperators()
        {
            var response = Operator.Equals.GetAsList();
            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

        }
    }
}
