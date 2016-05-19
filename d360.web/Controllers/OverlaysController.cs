using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;
using System.Diagnostics;

namespace d360.web.Controllers
{
    [RoutePrefix("overlays"), Authorize]
    public class OverlaysController : BaseController
    {
        #region DI

        public OverlaysController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        [Route("MyApiCredentials")]
        public ActionResult MyApiCredentials()
        {
            var resource = Community.GetById<Resource>(Community.CurrentResourceID);

            ViewBag.ApiKey = resource.APIPublicKey;
            ViewBag.ApiSecret = resource.APIPrivateKey;
            ViewBag.ApiToken = resource.ApiReadOnlyAccessToken;

            return PartialView();
        }

        public ActionResult ArtifactListMetricsDashboard(int id)
        {
            var model = Company.GetById<ArtifactType>(id);
            if (model == null) return HttpNotFound();
            ViewBag.TypeID = model.ID;
            ViewBag.TypeName = model.Name;
            model = null;
            return PartialView();
        }

        public ActionResult AttributeTypeCategories()
        {
            return PartialView();
        }

        public ActionResult FusionConfigurationFilters(int fusionTypeID, int fusionID)
        {
            ViewBag.FusionTypeID = fusionTypeID;
            ViewBag.FusionID = fusionID;
            return PartialView();
        }

        public ActionResult FusionConfigurationHistory(int fusionTypeID, int fusionID)
        {
            ViewBag.FusionTypeID = fusionTypeID;
            ViewBag.FusionID = fusionID;
            return PartialView();
        }

        public ActionResult FusionConfigurationOwnershipRules(int fusionTypeID, int fusionID)
        {
            ViewBag.FusionTypeID = fusionTypeID;
            ViewBag.FusionID = fusionID;
            return PartialView();
        }

        public ActionResult FusionConfigurationPromotionRules(int fusionTypeID, int fusionID)
        {
            ViewBag.FusionTypeID = fusionTypeID;
            ViewBag.FusionID = fusionID;
            return PartialView();
        }

        public ActionResult FusionRules(int fusionTypeID, int fusionID)
        {
            ViewBag.FusionTypeID = fusionTypeID;
            ViewBag.FusionID = fusionID;
            return PartialView();
        }

        [Route("{type}/{id:int}/audit")]
        public ActionResult Audit(SystemObjects type, int id)
        {
            var detail = Company.GetObjectDetail(type.ToString(), id);
            if (detail != null)
            {
                ViewBag.ObjectName = detail.Name;
                detail = null;
                return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
            }
            else
            {
                return HttpNotFound();
            }
        }

        [Route("{type}/{id:int}/audit.json")]
        public JsonNetResult Audit(SystemObjects type, int id, string sortDataField, string sortOrder, int pagenum, int pagesize)
        {
            Trace.TraceInformation("Calling OverlaysController.Audit : {0}", id);

            var querySql = string.Format(@"select A.*, R.FirstName + ' ' + R.LastName as ResourceName
from	[reporting].[Global_Audit] A 
inner join [reporting].[Global_Resource] R on R.ResourceID = A.ResourceID and A.[Object] = @objType and A.ObjectID = {0}", id);

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("objType", type.ToString());

            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            int total = Company.Query<int>(countSql, dbArgs).First();

            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder, "Date", "desc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("{type}/{id:int}/audit/{auditID:long}/fields.json")]
        public JsonNetResult AuditFields(SystemObjects type, int id, long auditID)
        {
            Trace.TraceInformation("Calling OverlaysController.AuditFields : {0}, {1}", id, auditID);

            var querySql = string.Format(@"select A.*
from	[reporting].[Global_FieldAudit] A 
where A.AuditID = {0}", auditID);

            var query = Company.Query<dynamic>(querySql);

            return new JsonNetResult { Data = query, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("TaxonomyType/{id:int}/diagrams/catalog")]
        public ActionResult TaxonomyTypeDiagram_Catalog(int id)
        {
            ViewData.Add("ID", id);
            return PartialView();
        }

        //[Route("TaxonomyType/{id:int}/diagrams/catalog")]
        public ActionResult TaxonomyTypeDiagram_CatalogIFrame(int id)
        {
            ViewData.Add("ID", id);
            return View();
        }

        [Route("{id:int}/{childArtifactTypeID:int}/ChildArtifacts")]
        public ActionResult ChildArtifacts(int id, int childArtifactTypeID)
        {
            ViewData.Add("ChildArtifactTypeID", childArtifactTypeID);
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = SystemObjects.Artifact.ToString() });
        }

        [Route("{type}/{id:int}/comments")]
        public ActionResult Comments(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("raiseissue")]
        public ActionResult RaiseIssue()
        {
            return PartialView();
        }

        [Route("{type}/{id:int}/detail")]
        public ActionResult Detail(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("{type}/{id:int}/events")]
        public ActionResult Events(SystemObjects type, int id)
        {
            ViewData.Add("ShowHeader", true);
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }
        [Route("{type}/{id:int}/events/noheader")]
        public ActionResult EventsNoHeader(SystemObjects type, int id)
        {
            ViewData.Add("ShowHeader", false);
            return PartialView("Events", new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("{type}/{id:int}/followers")]
        public ActionResult Followers(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("{type}/{id:int}/issues")]
        public ActionResult Issues(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        public ActionResult LookupTypeUsage(int id)
        {
            var detail = Company.GetById<LookupType>(id);
            if (detail != null)
            {
                ViewBag.Name = detail.Name;
                ViewBag.ID = id;
                detail = null;
                return PartialView();
            }
            else
            {
                return HttpNotFound();
            }
        }

        public ActionResult PolicyTypeClasses()
        {
            return PartialView();
        }

        //public ActionResult ResponsibilityTypeHierarchies()
        //{
        //    return PartialView();
        //}

        [Route("{type}/{id:int}/score")]
        public ActionResult Score(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        public ActionResult TaxonomyTypeClasses()
        {
            return PartialView();
        }
    }
}
