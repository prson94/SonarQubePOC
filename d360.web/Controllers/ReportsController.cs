using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.V1;
using Microsoft.PowerBI.Security;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Data.Entity;

namespace d360.web.Controllers
{
    [RoutePrefix("reports"), Authorize]
    public class ReportsController : BaseController
    {
        #region DI

        public ReportsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion


        [Route("Overlay"), NonNullableParameters]
        public ActionResult Overlay(int reportID, string type, int id)
        {
            var report = Company.GetById<Report>(reportID, i => i.ReportLayout);
            if (report == null) return HttpNotFound();

            var model = new ReportOverlayModel { ReportID = reportID, ReportName = report.Name, ObjectType = type, ObjectID = id };
            var objectName = "";

            switch (report.ObjectType)
            {
                case "Artifact":
                    var a = Company.GetById<Artifact>(id);
                    if (a!= null) objectName = a.Name;
                    a = null;
                    break;
                case "ArtifactType":
                    var at = Company.GetById<ArtifactType>(id);
                    if (at != null) objectName = at.Name;
                    at = null;
                    break;
                case "Resource":
                    var r = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).SingleOrDefault();
                    if (r != null) objectName = string.Format("{0} {1}", r.FirstName, r.LastName);
                    r = null;
                    break;
                case "TaxonomyType":
                    var t = Company.GetById<Taxonomy>(id);
                    if (t != null) objectName = t.Name;
                    t = null;
                    break;
                case "Taxonomy":
                    var tt = Company.GetById<TaxonomyType>(id);
                    if (tt != null) objectName = tt.Name;
                    tt = null;
                    break;
            }

            if (!string.IsNullOrEmpty(objectName))
                model.ReportName = string.Format("{0} : {1}", model.ReportName, objectName);


            return PartialView(model);
        }

        [Route("PreviewOverlay"), NonNullableParameters]
        public ActionResult PreviewOverlay(int id)
        {
            var report = Company.GetById<Report>(id, i => i.ReportLayout);
            if (report == null) return HttpNotFound();

            var model = new ReportOverlayModel { ReportID = id, ReportName = report.Name, ObjectTypes = new List<SelectListItem>() };

            switch (report.ObjectType)
            { 
                case "Artifact":
                    model.ObjectTypes = Company.Filter<Artifact>(i => i.ArtifactTypeID == report.ObjectID)
                        .OrderBy(i => i.Name)
                        .ToList()
                        .Select(i => new SelectListItem { Text = i.Name, Value = string.Format("Artifact|{0}", i.ID) })
                        .ToList();
                    break;
                case "ArtifactType":
                case "Resource":
                    model.ObjectTypes = Company.Table<GlobalReportingResource>()
                        .OrderBy(i => i.LastName).ThenBy(i => i.FirstName)
                        .ToList()
                        .Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = string.Format("Resource|{0}", i.ResourceID) })
                        .ToList();
                    break;
                case "TaxonomyType":
                    model.ObjectType = report.ObjectType;
                    model.ObjectID = report.ObjectID;
                    break;
                case "Taxonomy":
                    model.ObjectTypes = Company.Filter<Taxonomy>(i => i.TaxonomyTypeID == report.ObjectID)
                        .OrderBy(i => i.TextPath)
                        .ToList()
                        .Select(i => new SelectListItem { Text = i.TextPath, Value = string.Format("Taxonomy|{0}", i.ID) })
                        .ToList();
                    break;
            }

            
            return PartialView(model);
        }

        private IPowerBIClient CreatePowerBIClient(string accessKey)
        {
            var credentials = new TokenCredentials(accessKey, "AppKey");
            var client = new PowerBIClient(credentials)
            {
                BaseUri = new Uri("https://api.powerbi.com")
            };

            return client;
        }
        
        [Route("powerbi/tokens/{reportId}")]
        public async Task<JsonNetResult> GetPowerBITokens(string reportId)
        {
            var companySettings = Community.GetCompanySettings();
            var workspaceCollectionName = string.Empty;
            var workspaceId = string.Empty;
            var accessKey = string.Empty;

            companySettings.TryGetValue("PowerBIWorkspaceCollectionName", out workspaceCollectionName);
            companySettings.TryGetValue("PowerBIWorkspaceId", out workspaceId);
            companySettings.TryGetValue("PowerBIAccessKey", out accessKey);

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(workspaceCollectionName))
                throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");

            using (var client = CreatePowerBIClient(accessKey))
            {
                var reportsResponse = await client.Reports.GetReportsAsync(workspaceCollectionName, workspaceId);
                var report = reportsResponse.Value.FirstOrDefault(r => r.Id == reportId);
                var embedToken = PowerBIToken.CreateReportEmbedToken(workspaceCollectionName, workspaceId, report.Id);

                var viewModel = new PowerBIReportViewModel
                {
                    Report = report,
                    AccessToken = embedToken.Generate(accessKey)
                };

                return new JsonNetResult { Data = viewModel, Formatting = Newtonsoft.Json.Formatting.None };
            }            
        }

        [Route("reports")]
        public JsonNetResult GetReports()
        {
            var reports = Company.Reports.Include(rpt=>rpt.Responsibilities).OrderBy(x => x.Name).ToList();

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
                report.VisibleTo = string.IsNullOrEmpty(visibleTo)? null:visibleTo;
            }

            return new JsonNetResult { Data = reports, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ByContext/{type}/{id:int}/{reportType?}")]
        public JsonNetResult GetReportsByObject(string type, int id, string reportType)
        {
            if (!string.IsNullOrEmpty(reportType))
            {
                if (id > 0)
                {
                    var reports = Company.Filter<Report>(x => x.ObjectType == type && x.ObjectID == id && x.ReportType == reportType).Include(rpt => rpt.Responsibilities).OrderBy(i => i.Name).ToList();

                    var currentUserResponsibilityType = Company.Responsibilities.Where(x => x.ObjectID == id && x.ObjectType == type && x.ResponsibleObjectType == "Resource" && x.ResponsibleObjectID == Company.CurrentResourceID).FirstOrDefault();

                    var currentUserResponsibilityTypeID = 0;

                    if (currentUserResponsibilityType != null)
                        currentUserResponsibilityTypeID = currentUserResponsibilityType.ResponsibilityTypeID;

                    //check that the current user has access to the current report
                    for (int i = reports.Count - 1; i >= 0; i--)
                    {
                        var report = reports[i];

                        if (report.Responsibilities != null && report.Responsibilities.Count > 0)
                        {
                            bool userHasAccess = false;

                            foreach (var responsibility in report.Responsibilities)
                            {
                                if (responsibility.ResponsibilityTypeID == currentUserResponsibilityTypeID)
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
                    var reports = Company.Filter<Report>(x => x.ReportType == reportType).OrderBy(i => i.Name).ToList();

                    for (int i = reports.Count - 1; i >= 0; i--)
                    {
                        var report = reports[i];

                        if (report.Responsibilities != null && report.Responsibilities.Count > 0)
                        {
                            var currentUserResponsibilityType = Company.Responsibilities.Where(x => x.ObjectID == report.ObjectID && x.ObjectType == report.ObjectType && x.ResponsibleObjectType == "Resource" && x.ResponsibleObjectID == Company.CurrentResourceID).FirstOrDefault();

                            var currentUserResponsibilityTypeID = 0;

                            if (currentUserResponsibilityType != null)
                                currentUserResponsibilityTypeID = currentUserResponsibilityType.ResponsibilityTypeID;

                            bool userHasAccess = false;

                            foreach (var responsibility in report.Responsibilities)
                            {
                                if (responsibility.ResponsibilityTypeID == currentUserResponsibilityTypeID)
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

            if (id > 0)
                return new JsonNetResult { Data = Company.Filter<Report>(x => x.ObjectType == type && x.ObjectID == id).OrderBy(i => i.Name), Formatting = Newtonsoft.Json.Formatting.None };            
            else
                return new JsonNetResult { Data = Company.Table<Report>().OrderBy(i => i.Name), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("tiles")]
        public JsonNetResult GetReportTiles()
        {
            return new JsonNetResult
            {
                Data = Company.Filter<ReportTile>(i => 1 == 1, i => i.Report).OrderBy(i => i.Name).Select(i => new { i.ID, i.Name, i.ReportID, Report = i.Report.Name, i.Report.ObjectType, i.Report.ObjectID }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("instances/{type}/{id:int}")]
        public JsonNetResult GetInstanceOptionsForType(string type, int id)
        {
            switch (type)
            {
                case "ArtifactType":
                    return new JsonNetResult
                    {
                        Data = Company.Filter<Artifact>(i => i.ArtifactTypeID == id).OrderBy(i => i.Name).ToList().Select(i => new { i.Name, i.ID }),
                        Formatting = Newtonsoft.Json.Formatting.None
                    };
                case "TaxonomyType":
                    return new JsonNetResult
                    {
                        Data = Company.Filter<Taxonomy>(i => i.TaxonomyTypeID == id).OrderBy(i => i.TextPath).ToList().Select(i => new { Name = i.TextPath, i.ID }),
                        Formatting = Newtonsoft.Json.Formatting.None
                    };
                case "ReferenceItemType":
                    return new JsonNetResult
                    {
                        Data = Company.Table<ReferenceItemType>().OrderBy(i => i.Name).ToList().Select(i => new { i.Name, i.ID }),
                        Formatting = Newtonsoft.Json.Formatting.None
                    };
                case "ResourceType":
                    return new JsonNetResult
                    {
                        Data = Company.Table<GlobalReportingResource>().OrderBy(i => i.LastName).ThenBy(i => i.FirstName).ToList().Select(i => new { Name = i.LastName + ", " + i.FirstName, ID = i.ResourceID }),
                        Formatting = Newtonsoft.Json.Formatting.None
                    };
            }

            return new JsonNetResult
            {
                Data = null,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("schema")]
        public JsonNetResult GetReportingSchema()
        {
            return new JsonNetResult 
            {
                Data = Company.GetReportingSchema(), 
                Formatting = Newtonsoft.Json.Formatting.None 
            };
        }

        [Route("{reportID:int}/layout")]
        public JsonNetResult GetReportLayout(int reportID)
        {
            var model = Company.GetById<Report>(reportID, r => r.ReportLayout);
            var tiles = Company.Filter<ReportTile>(i => i.ReportID == reportID).ToList();

            if (model != null)
            {
                var json = JArray.Parse(model.ReportLayout.Template);
                foreach (dynamic r in json)
                {
                    foreach (dynamic c in r.cells)
                    {
                        foreach (dynamic a in c.areas)
                        {
                            var tileList = tiles.Where(i => i.ContentAreaNumber == (int)a.id).ToList();
                            //var tileArray = JArray.FromObject(tileList.Select(i => new { i.ID, i.Name, i.ReportTileTypeID }));
                            var tileArray = new JArray();
                            foreach (var t in tileList)
                            {
                                var tileObject = new JObject();
                                tileObject.Add("ID", t.ID);
                                tileObject.Add("Name", t.Name);
                                tileObject.Add("ReportTileType", (int)t.ReportTileType);
                                tileObject.Add("Icon", t.ReportTileType.GetReportTileTypeIcon());

                                var settingsObject = new JObject();
                                var settingsXml = XElement.Parse(string.IsNullOrEmpty(t.Settings) ? "<settings/>" : t.Settings);

                                foreach (var s in settingsXml.Elements())
                                {
                                    settingsObject.Add(s.Name.LocalName, s.Value);
                                }
                                tileObject.Add("Settings", settingsObject);

                                tileArray.Add(tileObject);
                            }

                            (a as JObject).Add("tiles", tileArray);
                        }
                    }
                }
                return new JsonNetResult { Data = json, Formatting = Newtonsoft.Json.Formatting.None };
            }
            else 
            {
                return new JsonNetResult { Data = null, Formatting = Newtonsoft.Json.Formatting.None };
            }
        }

        [Route("layouts/{id:int}/layout")]
        public JsonNetResult GetReportLayoutSample(int id)
        {
            var model = Company.GetById<ReportLayout>(id);

            if (model != null)
            {
                return new JsonNetResult { Data = JArray.Parse(model.Template), Formatting = Newtonsoft.Json.Formatting.None };
            }
            else
            {
                return new JsonNetResult { Data = null, Formatting = Newtonsoft.Json.Formatting.None };
            }
        }

        [Route("{reportID:int}/tiles")]
        public JsonNetResult GetReportTiles(int reportID)
        {
            var models = Company.Filter<ReportTile>(i => i.ReportID == reportID);
            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("data"), HttpPost, NonNullableParameters]
        public JsonNetResult GetReportTilePreviewData(string sql)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return new JsonNetResult { Data = new { error = "You must be an administrator." }, Formatting = Newtonsoft.Json.Formatting.None };
                }

                if (Company.IsValidReportingQuery(sql))
                {
                    var models = Company.Query<dynamic>(sql, null, 180);
                    return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
                }
                else
                {
                    return new JsonNetResult { Data = new { error = "Your command is not a valid reporting query." }, Formatting = Newtonsoft.Json.Formatting.None };
                }
            }
            catch (SqlException ex)
            {
                return new JsonNetResult { Data = new { error = ex.GetFullExceptionData() }, Formatting = Newtonsoft.Json.Formatting.None };
                //throw;
            }
        }
    }
}