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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using static d360.model.CommunityContext;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling environment and settings in Govern.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/environment"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class EnvironmentController : BaseV2ApiController
    {
        IStorageProvider _storage;
        public EnvironmentController(ICommunityContext community, ICompanyContext company, IStorageProvider storage) : base(community, company)
        {
            _storage = storage;
        }

        [HttpGet, AjaxValidateAntiForgeryToken, Route("rebuilds")]
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

        [HttpPost, AjaxValidateAntiForgeryToken, Route("rebuilds")]
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

        [HttpGet, Route("styles")]
        public async Task<HttpResponseMessage> StyleCustomizations()
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
        public async Task<HttpResponseMessage> UpdateStyleCustomizations(UpdateCss UpdateCss)
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
                    //update the company setting to sya where the files is 


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
    }
}
