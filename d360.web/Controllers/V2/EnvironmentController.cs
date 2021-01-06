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
        public EnvironmentController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IAssetRepository assetRepository) : base(community, company)
        {
            _storage = storage;
            _assetRepository = assetRepository;
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

                        if (model.SettingID == 73)
                        {
                            if (Guid.TryParse(model.StringSetting.Value, out Guid val))
                            {
                                value = val.ToString();
                            }
                            else
                            {
                                return ReturnApiError(HttpStatusCode.BadRequest, "Provided value is not a valid Guid");
                            }
                        }
                        else
                        {
                            value = model.StringSetting.Value;
                        }
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

                    case SettingType.Guid:
                        if (model.GuidSetting == null)
                            return ReturnApiError(HttpStatusCode.BadRequest, valueErrorMessage);
                        if (model.GuidSetting.Value == null)
                            clearSetting = true;

                        value = model.GuidSetting.Value.ToString();

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

        /// <summary>
        /// Retrieves usage information for assets and asset types a user or users has viewed.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("usage"),
            SwaggerConsumes("application/json"), 
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid)).ConfigureAwait(false);
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
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString(), $"Invalid _direction provided!")).ConfigureAwait(false);
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
                                errorMessage = $"Invalid _order provided!";
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
                                errorMessage = $"Invalid _startDate provided!";
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
                                errorMessage = $"Invalid _endDate provided!";
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
                                whereClauseItems.Add("gr.uid = @resourceUid");
                                dbArgs.Add("resourceUid", ruid);
                            }
                            else
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = $"Invalid _resourceuid provided!";
                            }
                        }
                        else if (key == "_assetuid")
                        {
                            Guid auid = Guid.Empty;
                            if (Guid.TryParse(q.Value, out auid))
                            {
                                whereClauseItems.Add("a.uid = @assetuid");
                                dbArgs.Add("assetuid", auid);
                            }
                            else
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = $"Invalid _assetuid provided!";
                            }
                        }
                        else if (key == "_assettypeuid")
                        {
                            Guid atuid = Guid.Empty;
                            if (Guid.TryParse(q.Value, out atuid))
                            {
                                whereClauseItems.Add("(att.uid = @assettypeuid or att2.uid = @assettypeuid )");
                                dbArgs.Add("assettypeuid", atuid);
                            }
                            else
                            {
                                code = HttpStatusCode.BadRequest;
                                errorMessage = $"Invalid _assettypeuid provided!";
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
                    att.Name as 'assetTypeName', 
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage)).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Retrieves environment licensing info. 
        /// Only non infogix user are included in these counts.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("licensing"),
            SwaggerConsumes("application/json"),
        ]
        public async Task<IHttpActionResult> GetLicensingDetails()
        {
            try
            {
                
                AssetsCountModel assetCount = await _assetRepository.GetAssetsCounts().ConfigureAwait(false);
                var allusers = Company.GlobalReportingResources.Where(x => x.State == CompanyResourceState.Active && (!x.Email.Contains("@data3sixty.com") && !x.Email.Contains("@infogix.com"))).Count();
                var allAdminUsers = Company.GlobalReportingResources.Where(x => x.IsAdministrator && x.State == CompanyResourceState.Active && (!x.Email.Contains("@data3sixty.com") && !x.Email.Contains("@infogix.com"))).Count();
                var contributorSql = @"
                SELECT count(distinct GR.resourceid)
                from Asset A
	                left join reporting.global_resource GR on 1=1
	                outer apply (
                            select top 1 * from
				                 (select PermissionsBitMask from UserAssetPermissions(GR.ResourceID,A.assetTypeID) 
					                where AssetID = A.ID 
			                union all 	
					                select PermissionsBitMask from UserAssetPermissions(GR.ResourceID,A.assetTypeID)
					                where AssetID = 0 and AssetTypeID = A.AssetTypeID)t
				                   )Permission(mask)
                WHERE gr.Email not like '%@infogix.com' 
	                and gr.Email not like '%@data3sixty.com'  
	                and gr.State = 1
	                and	1 = Case 
		                   when Permission.mask is null then gr.IsAdministrator
		                   when Permission.mask is not null and Permission.mask & 2 = 2 then 1
		                   when Permission.mask is null then gr.IsAdministrator
		                   when Permission.mask is not null and Permission.mask & 4 = 4 then 1
		                 else 0
		        end ";
                var contibutorCount = await Company.QueryFirstOrDefaultAsync<int>(contributorSql).ConfigureAwait(false);

                var model = new { assets = new { count = assetCount.totalNumberOfAssets }, users = new { total = allusers, contributors = contibutorCount, administrators = allAdminUsers } };


                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() { { "Endpoint Method", "Environment.GetLicensingDetails => " } });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage)).ConfigureAwait(false);
            }
        }
    }
}
