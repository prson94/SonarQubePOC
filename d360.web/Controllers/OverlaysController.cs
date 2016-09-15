using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;
using System.Diagnostics;
using SpreadsheetLight;
using System.IO;
using d360.web.Models.Attributes;

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

        public JsonNetResult MyApiCredentialsNg()
        {
            var resource = Community.GetById<Resource>(Community.CurrentResourceID);

            return new JsonNetResult
            {
                Data = new
                {
                    PublicKey = resource.APIPublicKey,
                    PrivateKey = resource.APIPrivateKey,
                    Token = resource.ApiReadOnlyAccessToken
                },
                Formatting = Newtonsoft.Json.Formatting.None

            };
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

        public ActionResult FusionRules(int fusionTypeID, int fusionID)
        {
            ViewBag.FusionTypeID = fusionTypeID;
            ViewBag.FusionID = fusionID;
            return PartialView();
        }

        public ActionResult FusionTechMapping()
        {            
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

        [Route("{type}/{id:int}/auditcombined.json")]
        public JsonNetResult AuditCombined(SystemObjects type, int id, string sortDataField, string sortOrder, int pagenum, int pagesize)
        {
            Trace.TraceInformation("Calling OverlaysController.AuditCombined : {0}", id);

            var querySql = @"select 	                            
	                                 ga.*,
                                     R.FirstName + ' ' + R.LastName as ResourceName, 
                                     fa.FieldName as Field, 
                                     fa.Value as NewValue, 
                                     fa.[Version] as 'Version',	                            
	                                 ( select			
				                            top 1 fa_sub.value as 'value'			                            
			                            from reporting.global_fieldaudit fa_sub
				                            inner join reporting.global_audit ga_sub on ( fa_sub.auditid = ga_sub.id)	
			                            where ga_sub.[object] = ga.[object] and ga_sub.[objectid] = ga.[objectid] and fa_sub.version = (fa.Version -1) and fa_sub.fieldname = fa.FieldName and fa_sub.fieldtypeid = fa.FieldTypeId ) as 'PreviousValue'
			
                            from reporting.global_fieldaudit fa
	                            inner join reporting.global_audit ga on ( fa.auditid = ga.id) 
                                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = @objType and ga.ObjectID = @objId";	                            

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("objType", type.ToString());
            dbArgs.Add("objId", id);

            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            int total = Company.Query<int>(countSql, dbArgs).First();

            sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            sql = applySortSuffix(sql, sortDataField, sortOrder, "Date", "desc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);

            return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        
        [Route("{type}/{id:int}/download/excel/audit.xls"), FileDownload, HttpGet]
        public FileResult GetAuditToExcel(SystemObjects type, int id)
        {
            var querySql = @"select
									ga.[Date],   
									ga.[Action],
									ga.ActionObject,
									ga.ActionObjectTypeName,
									ga.ActionObjectName,     
	                                 ga.ActionDescription,
                                     R.FirstName + ' ' + R.LastName as ResourceName, 
                                     fa.FieldName as Field, 
                                     fa.Value as NewValue, 
                                     fa.[Version] as 'Version',	                            
	                                 ( select			
				                            top 1 fa_sub.value as 'value'			                            
			                            from reporting.global_fieldaudit fa_sub
				                            inner join reporting.global_audit ga_sub on ( fa_sub.auditid = ga_sub.id)	
			                            where ga_sub.[object] = ga.[object] and ga_sub.[objectid] = ga.[objectid] and fa_sub.version = (fa.Version -1) and fa_sub.fieldname = fa.FieldName and fa_sub.fieldtypeid = fa.FieldTypeId ) as 'PreviousValue'
			
                            from reporting.global_fieldaudit fa
	                            inner join reporting.global_audit ga on ( fa.auditid = ga.id) 
                                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = @objType and ga.ObjectID = @objId";
                        
            var sql = string.Format(@"select * from ({0}) A", querySql);

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("objType", type.ToString());
            dbArgs.Add("objId", id);
                                    
            var query = Company.Query<dynamic>(sql, dbArgs);
            
            var document = new SLDocument();
            document.AddWorksheet("Items");
                        
            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "User");
            document.SetCellValue(1, 2, "Date");
            document.SetCellValue(1, 3, "Action");
            document.SetCellValue(1, 4, "Field");
            document.SetCellValue(1, 5, "New Value");
            document.SetCellValue(1, 6, "Previous Value");
            document.SetCellValue(1, 7, "Object");
            document.SetCellValue(1, 8, "Type");
            document.SetCellValue(1, 9, "Item");
            document.SetCellValue(1, 10, "Audit Description");
            document.SetCellValue(1, 11, "Revision");

            #endregion

            int rowIndex = 1;
            foreach (var row in query)
            {
                rowIndex++;

                document.SetCellValue(rowIndex, 1, row.ResourceName);
                document.SetCellValue(rowIndex, 2, row.Date.ToString());
                document.SetCellValue(rowIndex, 3, row.Action);
                document.SetCellValue(rowIndex, 4, row.Field);
                document.SetCellValue(rowIndex, 5, row.NewValue ?? "");
                document.SetCellValue(rowIndex, 6, row.PreviousValue ?? "");
                document.SetCellValue(rowIndex, 7, row.ActionObject);
                document.SetCellValue(rowIndex, 8, row.ActionObjectTypeName);
                document.SetCellValue(rowIndex, 9, row.ActionObjectName);
                document.SetCellValue(rowIndex, 10, row.ActionDescription);
                document.SetCellValue(rowIndex, 11, row.Version);
            }

            #endregion

            var detail = Company.GetObjectDetail(type.ToString(), id);

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"Audit details for {detail.Name} as of {System.DateTime.Now.ToShortDateString()}.xlsx");
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

        public ActionResult IntersectRoles()
        {
            return PartialView();
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

        public ActionResult Predicates()
        {
            return PartialView();
        }

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
