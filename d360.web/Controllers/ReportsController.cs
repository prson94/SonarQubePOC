using d360.core;
using d360.core.entities;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.PowerBI.Api.V2;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using System.Net;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;
using d360.core.entities.Views;
using d360.model.DataAccessLayer;
using Resources;

namespace d360.web.Controllers
{
    [RoutePrefix("reports"), Authorize]
    public class ReportsController : BaseController
    {
        #region DI

        public ReportsController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        { }

        #endregion

        private static readonly string pbiUsername = ConfigurationManager.AppSettings["pbiUsername"];
        private static readonly string pbiPassword = ConfigurationManager.AppSettings["pbiPassword"];
        private static readonly string pbiAuthorityUrl = "https://login.microsoftonline.com/02292cae-2fe6-4371-8da1-b03d14808575";
        private static readonly string pbiResourceUrl = "https://analysis.windows.net/powerbi/api";
        private static readonly string pbiUrl = "https://api.powerbi.com";


        [Route("powerbi/tokens/{reportId}")]
        public async Task<JsonNetResult> GetPowerBITokens(string reportId)
        {
            var companySettings = SettingsRepository.GetSettings();
            var groupId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIGroupId).Value;
            var clientId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIClientId).Value;

            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentNullException(FormControllerApiMessage.PowerBINotSetupOnGovernEnvironment);
            }

            // Create a user password cradentials.
            var credential = new UserPasswordCredential(pbiUsername, pbiPassword);

            // Authenticate using created credentials
            var authenticationContext = new AuthenticationContext(pbiAuthorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(pbiResourceUrl, clientId, credential);

            if (authenticationResult == null)
            {
                throw new ArgumentNullException(FormControllerApiMessage.AuthenticationFailed);
            }

            var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");

            using (var client = new PowerBIClient(new Uri(pbiUrl), tokenCredentials))
            {
                var reportsResponse = await client.Reports.GetReportsAsync(groupId);
                var report = reportsResponse.Value.FirstOrDefault(r => string.Compare(r.Id, reportId, true) == 0);

                if (report == null)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.NoSuchReport);
                }

                Microsoft.PowerBI.Api.V2.Models.GenerateTokenRequest generateTokenRequestParameters = new Microsoft.PowerBI.Api.V2.Models.GenerateTokenRequest(accessLevel: "view");

                var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(groupId, report.Id, generateTokenRequestParameters);

                if (tokenResponse == null)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.FailedGenerateToken);
                }

                var viewModel = new PowerBIReportViewModel
                {
                    Report = report,
                    AccessToken = tokenResponse.Token
                };

                return new JsonNetResult { Data = viewModel, Formatting = Newtonsoft.Json.Formatting.None };
            }
        }

        [Route("reports")]
        public JsonNetResult GetReports()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            var reports = Company.Reports.Include(rpt => rpt.Responsibilities).OrderBy(x => x.Name).ToList();

            foreach (var report in reports)
            {
                if (report.Responsibilities == null) continue;
                var visibleTo = "";
                foreach (var responsibility in report.Responsibilities)
                {
                    if (!string.IsNullOrEmpty(visibleTo))
                        visibleTo += ",";
                    visibleTo += responsibility.ResponsibilityTypeID.ToString();
                }
                report.VisibleTo = string.IsNullOrEmpty(visibleTo) ? null : visibleTo;
            }

            return new JsonNetResult { Data = reports, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ByContext/{type}/{id:int}")]
        public JsonNetResult GetReportsByObject(string type, int id)
        {
            if (id > 0)
            {
                bool isType = type.Contains("Type");

                SystemObjects objectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), type);
                var objectId = id;
                if (objectType == SystemObjects.Artifact || objectType == SystemObjects.Taxonomy)
                {
                    var asset = Company.AssetDetails.Where(x => x.Object == objectType.ToString() && x.ObjectID == id).First();

                    objectId = asset.TypeID;
                }

                var reports = Company.Filter<Report>(x => x.ObjectType == type && x.ObjectID == objectId && x.ReportType != "legacy").Include(rpt => rpt.Responsibilities).OrderBy(i => i.Name).ToList();

                List<ResponsibilityDetail> currentUserResponsibilityTypeList = new List<ResponsibilityDetail>();
                if (!string.IsNullOrEmpty(type) && !isType)
                {
                    currentUserResponsibilityTypeList = Company.ResponsibilityDetails.Where(x => x.ObjectID == id && x.Object == type && x.ResourceID == Company.CurrentResourceID).ToList();
                    var asset = Company.GetAssetDetail(type, id);

                    if (asset != null)
                        currentUserResponsibilityTypeList.AddRange(Company.ResponsibilityDetails.Where(x => x.AssetTypeID == asset.AssetTypeID && x.AssetID == 0 && x.ResourceID == Company.CurrentResourceID).ToList());

                }
                else if (isType)
                    currentUserResponsibilityTypeList = Company.ResponsibilityDetails.Where(x => x.TypeID == id && x.Type == type && x.ResourceID == Company.CurrentResourceID).ToList();
                else
                    currentUserResponsibilityTypeList = Company.ResponsibilityDetails.Where(x => x.ObjectID == id && x.Object == type && x.ResourceID == Company.CurrentResourceID).ToList();

                var currentUserResponsibilityTypeIDList = new List<int>();

                if (currentUserResponsibilityTypeList != null && currentUserResponsibilityTypeList.Count() > 0)
                {
                    currentUserResponsibilityTypeIDList = currentUserResponsibilityTypeList.Select(i => i.ResponsibilityTypeID).ToList();
                }

                //check that the current user has access to the current report
                for (int i = reports.Count - 1; i >= 0; i--)
                {
                    var report = reports[i];

                    if (report.Responsibilities != null && report.Responsibilities.Count > 0)
                    {
                        bool userHasAccess = false;

                        foreach (var responsibility in report.Responsibilities)
                        {
                            if (currentUserResponsibilityTypeIDList.Contains(responsibility.ResponsibilityTypeID))
                            {
                                userHasAccess = true;
                                break;
                            }
                        }
                        if (!userHasAccess)
                            reports.RemoveAt(i);
                    }
                }

                return new JsonNetResult { Data = reports, Formatting = Newtonsoft.Json.Formatting.None };
            }
            else
            {
                var reports = Company.Filter<Report>(x => x.ReportType != "legacy").OrderBy(i => i.Name).ToList();

                for (int i = reports.Count - 1; i >= 0; i--)
                {
                    var report = reports[i];

                    if (report.Responsibilities != null && report.Responsibilities.Count > 0)
                    {
                        List<core.entities.Views.ResponsibilityDetail> currentUserResponsibilityType = new List<core.entities.Views.ResponsibilityDetail>();
                        if (!string.IsNullOrEmpty(report.ObjectType) && !report.ObjectType.Contains("Type"))
                        {
                            currentUserResponsibilityType = Company.ResponsibilityDetails.Where(x => x.TypeID == report.ObjectID && x.Object == report.ObjectType && x.ResourceID == Company.CurrentResourceID).ToList();
                            currentUserResponsibilityType.AddRange(Company.ResponsibilityDetails.Where(x => x.TypeID == report.ObjectID && x.Type == report.ObjectType + "Type" && x.AssetID == 0 && x.ResourceID == Company.CurrentResourceID).ToList());
                        }
                        else if (report.ObjectType.Contains("Type"))
                            currentUserResponsibilityType = Company.ResponsibilityDetails.Where(x => x.TypeID == report.ObjectID && x.Type == report.ObjectType && x.ResourceID == Company.CurrentResourceID).ToList();
                        else
                            currentUserResponsibilityType = Company.ResponsibilityDetails.Where(x => x.ObjectID == report.ObjectID && x.Object == report.ObjectType && x.ResourceID == Company.CurrentResourceID).ToList();

                        var currentUserResponsibilityTypeIDList = new List<int>();

                        if (currentUserResponsibilityType != null && currentUserResponsibilityType.Count() > 0)
                        {
                            currentUserResponsibilityTypeIDList = currentUserResponsibilityType.Select(x => x.ResponsibilityTypeID).ToList();
                        }

                        bool userHasAccess = false;

                        foreach (var responsibility in report.Responsibilities)
                        {
                            if (currentUserResponsibilityTypeIDList.Contains(responsibility.ResponsibilityTypeID))
                            {
                                userHasAccess = true;
                                break;
                            }
                        }
                        if (!userHasAccess)
                            reports.RemoveAt(i);
                    }
                }

                return new JsonNetResult { Data = reports, Formatting = Newtonsoft.Json.Formatting.None };
            }
        }

        [Route("byid/{id:int}")]
        public ActionResult GetReportsByID(int id)
        {
            var report = Company.Reports.Include(rpt => rpt.Responsibilities).FirstOrDefault(x => x.ID == id && x.ReportType != "legacy");

            if(report == null)
            {
                return new HttpNotFoundResult();
            }

            var type = report.ObjectType;

            bool isType = type.Contains("Type");

            List<ResponsibilityDetail> currentUserResponsibilityTypeList = new List<ResponsibilityDetail>();
            if (!string.IsNullOrEmpty(type) && !isType)
            {
                currentUserResponsibilityTypeList = Company.ResponsibilityDetails.Where(x => x.ObjectID == report.ObjectID && x.Object == type && x.ResourceID == Company.CurrentResourceID).ToList();
                var asset = Company.AssetTypes.FirstOrDefault(x => x.Object == (type + "Type") && x.ObjectID == report.ObjectID);

                if (asset != null)
                    currentUserResponsibilityTypeList.AddRange(Company.ResponsibilityDetails.Where(x => x.AssetTypeID == asset.ID && x.AssetID == 0 && x.ResourceID == Company.CurrentResourceID).ToList());

            }
            else if (isType)
                currentUserResponsibilityTypeList = Company.ResponsibilityDetails.Where(x => x.TypeID == report.ObjectID && x.Type == type && x.ResourceID == Company.CurrentResourceID).ToList();
            else
                currentUserResponsibilityTypeList = Company.ResponsibilityDetails.Where(x => x.ObjectID == report.ObjectID && x.Object == type && x.ResourceID == Company.CurrentResourceID).ToList();

            var currentUserResponsibilityTypeIDList = new List<int>();

            if (currentUserResponsibilityTypeList != null && currentUserResponsibilityTypeList.Count() > 0)
            {
                currentUserResponsibilityTypeIDList = currentUserResponsibilityTypeList.Select(i => i.ResponsibilityTypeID).ToList();
            }

            //check that the current user has access to the current report
            if (report.Responsibilities != null && report.Responsibilities.Count > 0)
            {
                bool userHasAccess = false;

                foreach (var responsibility in report.Responsibilities)
                {
                    if (currentUserResponsibilityTypeIDList.Contains(responsibility.ResponsibilityTypeID))
                    {
                        userHasAccess = true;
                        break;
                    }
                }
                if (!userHasAccess)
                    report = null;
            }
            return new JsonNetResult { Data = report, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("home"), ValidateContracts(Ignore = true), HttpGet]
        public JsonNetResult GetHomePageReports()
        {
            var reports = Company.Filter<Report>(r => r.ShowOnHomePage && r.ReportType.ToLower() != "legacy").ToList();
            return new JsonNetResult
            {
                Data = reports,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
    }
}
