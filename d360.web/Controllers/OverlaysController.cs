using d360.core;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using Dapper;
using Resources;
using SpreadsheetLight;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("overlays"), Authorize, AiHandleError]
    public class OverlaysController : BaseController
    {
        #region DI

        public OverlaysController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        [Route("MyApiCredentialsNg")]
        public JsonNetResult MyApiCredentialsNg()
        {
            if (!Company.CurrentResourceIsAdmin && !this.ShowAllUsersAPIKey())
                return jsonNetException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);



            var resource = Community.GetById<Resource>(Community.CurrentResourceID);

            return new JsonNetResult
            {
                Data = new
                {
                    PublicKey = resource.APIPublicKey,
                    PrivateKey = resource.APIPrivateKey
                },
                Formatting = Newtonsoft.Json.Formatting.None

            };
        }

        [Route("{type}/{id:int}/auditcombined.json")]
        public JsonNetResult AuditCombined(SystemObjects type, int id, string sortDataField, string sortOrder, int pagenum, int pagesize)
        {
            try
            {
                Trace.TraceInformation("Calling OverlaysController.AuditCombined : {0}", id);
                var dbArgs = new Dapper.DynamicParameters();                

                var querySql = @"select 	                            
                                   ga.*,
                                    case when R.State = 1 then
                                        R.FirstName + ' ' + R.LastName
                                    else
                                        R.FirstName + ' ' + R.LastName + ' (deleted)'
                                    end as ResourceName,
                                     fa.FieldName as Field, 
                                     fa.Value as NewValue, 
                                     fa.[Version] as 'Version',	                            
	                                 ( select			
				                            top 1 fa_sub.value as 'value'			                            
			                            from reporting.global_fieldaudit fa_sub
				                            inner join reporting.global_audit ga_sub on ( fa_sub.auditid = ga_sub.id)	
			                            where ga_sub.[object] = ga.[object] and ga_sub.[objectid] = ga.[objectid] and fa_sub.version = (fa.Version -1) and fa_sub.fieldname = fa.FieldName and fa_sub.fieldtypeid = fa.FieldTypeId and ga_sub.actionObjectId=ga.actionObjectId) as 'PreviousValue'
			
                            from reporting.global_audit ga 
								left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
                                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = @objType and ga.ObjectID = @objId";

                if (type.ToString() == "FusionType")
                {//Gets the Fusion audit for the fusion type
                    querySql += @" UNION 
                                    select 	                            
                                    ga.*,
                                    case when R.State = 1 then
                                        R.FirstName + ' ' + R.LastName
                                    else
                                        R.FirstName + ' ' + R.LastName + ' (deleted)'
                                    end as ResourceName, 
                                     fa.FieldName as Field, 
                                     fa.Value as NewValue, 
                                     fa.[Version] as 'Version',	                            
	                                 ( select			
				                            top 1 fa_sub.value as 'value'			                            
			                            from reporting.global_fieldaudit fa_sub
				                            inner join reporting.global_audit ga_sub on ( fa_sub.auditid = ga_sub.id)	
			                            where ga_sub.[object] = ga.[object] and ga_sub.[objectid] = ga.[objectid] and fa_sub.version = (fa.Version -1) and fa_sub.fieldname = fa.FieldName and fa_sub.fieldtypeid = fa.FieldTypeId and ga_sub.actionObjectId=ga.actionObjectId) as 'PreviousValue'
			
                            from reporting.global_audit ga 
								left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
                                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = 'Fusion' 
                                and ga.ObjectID in ( Select  Id from Fusion where fusiontypeid= @objId)";
                }

                if (type == SystemObjects.ReferenceItemType)
                {
                    var referenceItemType = Company.GetById<ReferenceItemType>(id);
                    var referenceItemTypeIDs = referenceItemType.ReferenceItems.Select(x => x.ID);
                    querySql += @" UNION
                                    select 	                            
                                   ga.*,
                                    case when R.State = 1 then
                                        R.FirstName + ' ' + R.LastName
                                    else
                                        R.FirstName + ' ' + R.LastName + ' (deleted)'
                                    end as ResourceName,
                                     fa.FieldName as Field, 
                                     fa.Value as NewValue, 
                                     fa.[Version] as 'Version',	                            
	                                 ( select			
				                            top 1 fa_sub.value as 'value'			                            
			                            from reporting.global_fieldaudit fa_sub
				                            inner join reporting.global_audit ga_sub on ( fa_sub.auditid = ga_sub.id)	
			                            where ga_sub.[object] = ga.[object] and ga_sub.[objectid] = ga.[objectid] and fa_sub.version = (fa.Version -1) and fa_sub.fieldname = fa.FieldName and fa_sub.fieldtypeid = fa.FieldTypeId and ga_sub.actionObjectId=ga.actionObjectId) as 'PreviousValue'
			
                            from reporting.global_audit ga 
								left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
                                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = 'ReferenceItem' and ga.ObjectID IN @ReferenceIDs";
                    dbArgs.Add("ReferenceIDs", referenceItemTypeIDs);

                }

                var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
                var sql = string.Format(@"select * from ({0}) A", querySql);

                dbArgs.Add("objType", new DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 });
                dbArgs.Add("objId", id);

                countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
                int total = Company.Query<int>(countSql, dbArgs).First();

                sql = applyFilteringSuffixBind(sql, Request, dbArgs);
                var stFieldType = sortDataField == null || sortDataField == "Date" ? "DateTime" : "string";
                sql = applySortSuffix(sql, sortDataField, sortOrder, "Date", "desc",sortFieldType: stFieldType);
                sql = applyPagingSuffix(sql, pagenum, pagesize);

                var query = Company.Query<dynamic>(sql, dbArgs);

                return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (System.Exception ex)
            {
                return jsonNetException(ex);
            }
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
                                    case when R.State = 1 then
                                        R.FirstName + ' ' + R.LastName
                                    else
                                         R.FirstName + ' ' + R.LastName + ' (deleted)'
                                    end as ResourceName, 
                                     fa.FieldName as Field, 
                                     fa.Value as NewValue, 
                                     fa.[Version] as 'Version',	                            
	                                 ( select			
				                            top 1 fa_sub.value as 'value'			                            
			                            from reporting.global_fieldaudit fa_sub
				                            inner join reporting.global_audit ga_sub on ( fa_sub.auditid = ga_sub.id)	
			                            where ga_sub.[object] = ga.[object] and ga_sub.[objectid] = ga.[objectid] and fa_sub.version = (fa.Version -1) and fa_sub.fieldname = fa.FieldName and fa_sub.fieldtypeid = fa.FieldTypeId ) as 'PreviousValue'
			
                            from reporting.global_audit ga 
								left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
                                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = @objType and ga.ObjectID = @objId                        
            ";

            var sql = string.Format(@"select * from ({0}) A", querySql);
            sql = applySortSuffix(sql, "Date", "desc", sortFieldType: "DateTime");

            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("objType", new DbString {Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 });
            dbArgs.Add("objId", id);

            sql = applyFilteringSuffixBind(sql, Request, dbArgs);

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
                document.SetCellValue(rowIndex, 2, (((DateTime)row.Date)));

                SLStyle style = document.CreateStyle();
                style.FormatCode = "mmm dd yyyy hh:mm:ss";
                document.SetCellStyle(rowIndex, 2, style);

                document.SetCellValue(rowIndex, 3, row.Action);
                document.SetCellValue(rowIndex, 4, row.Field ?? "");
                document.SetCellValue(rowIndex, 5, row.NewValue ?? "");
                document.SetCellValue(rowIndex, 6, row.PreviousValue ?? "");
                document.SetCellValue(rowIndex, 7, row.ActionObject);
                document.SetCellValue(rowIndex, 8, row.ActionObjectTypeName);
                document.SetCellValue(rowIndex, 9, row.ActionObjectName);
                document.SetCellValue(rowIndex, 10, row.ActionDescription);
                document.SetCellValue(rowIndex, 11, row.Version ?? "");
            }

            #endregion

            var detail = Company.GetObjectDetail(type.ToString(), id);

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"Audit details for {detail.Name} as of {System.DateTime.Now.ToShortDateString()}.xlsx");
        }
    }        
}
