using System;
using System.Linq;
using System.Web.Mvc;
using d360.model;
using d360.core.entities;
using d360.web.Models.Attributes;
using SpreadsheetLight;
using System.IO;
using Newtonsoft.Json;
using d360.web.Models;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/monitor"), Authorize]
    public class MonitorController : BaseController
    {
        #region DI

        public MonitorController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        #region Json

        [Route("rules/{id:int}/results")]
        public JsonNetResult GetRuleResults(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter)
        {
            var dbArgs = new Dapper.DynamicParameters();

            dbArgs.Add("id", id);

            var querySql = @"select	A.*, coalesce(FA.[Name], RF.FusionAttribute) as FusionAttribute 
{1}
from	RuleResult A 
left join RuleResultFusionAttribute RF on RF.RuleResultID = A.ID
left join FusionAttribute FA on FA.ID = RF.FusionAttributeID
        {0}
where   A.RuleImplementationID = @id";

            var ruleQualifiers = Company.Query<RuleQualifierTypeField>(@"select Name as Header, replace(Name, ' ', '') as Field from RuleResultQualifierType where RuleImplementationID = @id order by [Order]", new { id }).ToList();
            var qualifierFieldsSql = "";

            if (ruleQualifiers.Count > 0)
            {
                qualifierFieldsSql = @"
                        left join
		                        (select * from
			                        (select q.RuleResultID as ResID, replace(QT.[Name], ' ', '') as N, Q.[Value] as Val from RuleResultQualifierType QT
			                        join RuleResultQualifier Q on Q.RuleResultQualifierTypeID = QT.ID
			                        where QT.RuleImplementationID = @id) as vt
			                        pivot
			                        (
			                        max(Val) for N in (
			                        {0}
			                        )
			                        ) as qr) as RQ on RQ.ResID = A.ID
                                    ";
                qualifierFieldsSql = string.Format(qualifierFieldsSql, string.Join(",", ruleQualifiers.Select(q => $"[{q.Field}]")));
            }

            querySql = string.Format(querySql, qualifierFieldsSql, (ruleQualifiers.Count > 0) ? ",RQ.*" : "");

            //if simple filter specified add that citeria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                querySql = $@"{querySql} and {addDynamicFieldSimpleFilter(new string[] { 
                    "A.EffectiveDate", 
                    "A.RowsPassed",
                    "A.RowsFailed",
                    "A.PassFraction",
                    "A.FailFraction",
                    "A.Passed",
                    "F.TextPath"
                }, "RuleResult", id, filter, dbArgs)}";
            }

            querySql = applyRelationFilteringExists(querySql, Request, dbArgs);

           

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);


            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs, true);
            sql = applyFilteringSuffixBind(sql, Request, dbArgs, true);


            sql = applySortSuffix(sql, sortDataField, sortOrder, "EffectiveDate", "desc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            int total = Company.Query<int>(countSql, dbArgs).First();
            var query = Company.Query<dynamic>(sql, dbArgs);

            return new JsonNetResult { Data = new { total, results = query, qualifiers = ruleQualifiers }, Formatting = Formatting.None };
        }

        #endregion

        [Route("ExportResultsByRule"), FileDownload, NonNullableParameters]
        public FileResult ExportQueryItemsByAttributeType(int id)
        {
            var detail = Company.GetObjectDetail("Rule", id);
            var results = Company.Filter<RuleResult>(i => i.RuleImplementation.RuleID == id).OrderByDescending(i => i.EffectiveDate);

            #region Create the list sheet

            var document = new SLDocument();
            var defaultSheet = detail.PluralizedName;
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, defaultSheet);
            document.SelectWorksheet(defaultSheet);

            var row = 1;

            #region Header

            document.SetCellValue(row, 1, "Effective Date");
            document.SetCellValue(row, 2, "Rows Passed");
            document.SetCellValue(row, 3, "Rows Failed");
            document.SetCellValue(row, 4, "Passed");
            document.SetCellValue(row, 5, "Created On");

            #endregion

            foreach (var item in results)
            {
                row++;
                document.SetCellValue(row, 1, item.EffectiveDate.ToShortDateString());
                document.SetCellValue(row, 2, item.RowsPassed);
                document.SetCellValue(row, 3, item.RowsFailed);
                document.SetCellValue(row, 4, (item.Passed) ? "Y" : "N");
                document.SetCellValue(row, 5, item.CreatedOn.ToShortDateString());
            }

            document.AutoFitColumn(1, 5);

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"{detail.PluralizedName}.xlsx");
        }

        [Route("workflowmonitor/items/download/excel.xls"), HttpGet, FileDownload]
        public FileResult GetWorkFlowMonitorToExcel(int pagenum, int pagesize, string sortDataField, string sortOrder)
        {

            var filters = GetFilterValuesFromRequest(Request, true);
            string typeSql = "";
            string whereSql = "";
            string assignedSql = "";
            string havingSql = "";
            foreach (var f in filters)
            {
                var ff = f as UiRequestFieldFilterValue;
                if (ff == null) continue;
                switch (ff.FieldName)
                {
                    case "WorkflowId":
                        var types = ff.RawValue.Trim().TrimEnd(',');
                        typeSql += $@" wt.id in ({types}) and ";
                        break;
                    case "Asset":
                        typeSql += $@" (case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)') 
                                        when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id) 
                                        else coalesce(utility.getassetdisplayvalue(ass.id), '(unknown)') end) Like '%{ff.RawValue}%' and ";
                        break;
                    case "TypeName":
                        typeSql += $@" case when wi.[object] = 'Issue' then it.Name else assettype.name end  Like '%{ff.RawValue}%' and ";
                        break;
                    case "Type":
                        typeSql += $@"assettype.[Object]='{ff.RawValue}' and ";
                        break;
                    case "StartedOn":
                        typeSql += $@"datediff(day, wi.startedOn, '{ff.RawValue}') = 0 and ";
                        break;
                    case "CompletedOn":
                        typeSql += $@"datediff(day, wi.CompletedOn, '{ff.RawValue}') = 0 and ";
                        break;
                    case "Status":
                        havingSql = $@"case when count(s.StepID) > 0 then    
                                            case when max(vs.ActivityType) = 3 then    'Waiting on user action'                   
                                            else     'Incomplete'    end         
                                            else        'Complete'    end ='{ff.RawValue}'";
                        break;
                    case "Initiator":
                        typeSql += $@"( gr.firstName Like '{ff.RawValue}%' or gr.lastName Like '{ff.RawValue}%' or gr.firstName + ' ' + gr.lastName LIKE '{ff.RawValue}%' ) and ";
                        break;
                    case "AssignedTo":
                        assignedSql = @"inner join workflow.ItemAssignment WIA on WIA.ItemID = WI.ID and WIA.ResourceObject = 'Resource'
                                            inner join reporting.Global_Resource GRA on WIA.ResourceObjectID = GRA.ResourceID ";
                        typeSql += $@"( gra.firstName Like '{ff.RawValue}%' or gra.lastName Like '{ff.RawValue}%' or gra.firstName + ' ' + gra.lastName LIKE '{ff.RawValue}%' ) and ";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(typeSql))
            {
                typeSql = typeSql.Trim().TrimEnd('a', 'n', 'd');
                whereSql = "where " + typeSql;
            }

            if (!string.IsNullOrEmpty(havingSql))
            {
                havingSql = $@"having ({havingSql})";
            }

            sortDataField = string.IsNullOrEmpty(sortDataField) ? "StartedOn" : sortDataField;
            var stFieldType = sortDataField == "StartedOn" || sortDataField == "CompletedOn" ? "Date" : "string";
            var sortsql = applySortSuffix(string.Empty, sortDataField, sortOrder, "Date", "desc", sortFieldType: stFieldType);
            var pagingSql = applyPagingSuffix(string.Empty, pagenum, pagesize);
            var groupby = @"group by wi.id,wt.name, wi.startedOn,wi.CompletedOn,wi.[object],wi.objectid,cod.id,ass.id, wi.startedOn, wi.CompletedOn,
		                        gr.firstName , gr.lastName,assettype.name, it.Name,assettype.[Object]";

            var fromSql = @"		from [workflow].[type] wt 
                                inner join [workflow].[version] wv on (wt.id = wv.typeid) 
                                inner join [workflow].[item] wi on (wv.id = wi.versionid)	
                                left join workflow.versionstep vs on vs.versionid = wi.id
                                left join workflow.itemstep s on s.stepid = vs.id and s.CompletedOn is null
                                left join [dbo].asset ass on(ass.[object] = wi.[object] and ass.[objectid] = wi.[objectid]) 
                                left join [dbo].assettype assettype on(ass.assettypeid = assettype.id)	         
                                inner join [reporting].global_resource gr on (wi.startedBy = gr.resourceid) left 
                                outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue') 
                                left outer join [dbo].[asset] cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                                left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id) ";
            var sql = $@"
                         select wi.id as Id,                    
                         wt.name as 'WorkflowName' ,                    
                         assettype.[Object] as 'Type',                    
                         case when wi.[object] = 'Issue' then it.Name else assettype.name end as 'TypeName' ,                    
                         case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), 
                         '(unknown relationship)')   when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id)  
                         else coalesce(utility.getassetdisplayvalue(ass.id),'(unknown)') end as 'Asset',                    
                         gr.firstName + ' ' + gr.lastName as 'Initiator' ,                    
                         wi.startedOn as 'StartedOn',                    wi.CompletedOn as 'CompletedOn',                    
                         case when count(s.StepID) > 0 then    case when max(vs.ActivityType) = 3 then    'Waiting on user action'                   
                         else     'Incomplete'    end         else        'Complete'    end as [Status]   
                        {fromSql}
                        {assignedSql}
                        {whereSql} 
                        {groupby}
                        {havingSql}
                        ";

            sql = $@"Select * from ({sql}) as A {sortsql} ";
            var list = Company.Query<dynamic>(sql);

            var document = new SLDocument();
            document.AddWorksheet("Items");
            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Workflow Name");
            document.SetCellValue(1, 2, "Type");
            document.SetCellValue(1, 3, "Type Name");
            document.SetCellValue(1, 4, "Asset");
            document.SetCellValue(1, 5, "Initiator");
            document.SetCellValue(1, 6, "Started");
            document.SetCellValue(1, 7, "Completed");
            document.SetCellValue(1, 8, "Status");

            #endregion

            int rowIndex = 1;
            foreach (var row in list)
            {
                rowIndex++;

                document.SetCellValue(rowIndex, 1, row.WorkflowName);
                document.SetCellValue(rowIndex, 2, row.Type ?? "");
                document.SetCellValue(rowIndex, 3, row.TypeName ?? "");
                document.SetCellValue(rowIndex, 4, row.Asset ?? "");
                document.SetCellValue(rowIndex, 5, row.Initiator ?? "");
                document.SetCellValue(rowIndex, 6, row.StartedOn??"");
                SLStyle style = document.CreateStyle();
                style.FormatCode = "mm/dd/yyyy";
                document.SetCellStyle(rowIndex, 6, style);

                document.SetCellValue(rowIndex, 7, row.CompletedOn??"");
                SLStyle style1 = document.CreateStyle();
                style1.FormatCode = "mm/dd/yyyy";
                document.SetCellStyle(rowIndex, 7, style1);

                document.SetCellValue(rowIndex, 8, row.Status ?? "");

            }

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"WorkflowItems {System.DateTime.Now.ToShortDateString()}.xlsx");


        }
        [Route("workflowmonitor/items"), HttpGet, NonNullableParameters]
        public JsonNetResult GetWorkflowMonitor( int pagenum, int pagesize, string sortDataField, string sortOrder)
        {

            try
            {
                var filters = GetFilterValuesFromRequest(Request, true);
                string typeSql="";
                string whereSql = "";
                string assignedSql = "";
                string havingSql = "";
                foreach (var f in filters)
                {   var ff = f as UiRequestFieldFilterValue;
                    if (ff == null) continue;
                    switch (ff.FieldName)
                    {
                        case "WorkflowId":
                            var types = ff.RawValue.Trim().TrimEnd(',');
                            typeSql += $@" wt.id in ({types}) and ";
                            break;
                        case "Asset":
                            typeSql += $@" (case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)') 
                                        when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id) 
                                        else coalesce(utility.getassetdisplayvalue(ass.id), '(unknown)') end) Like '%{ff.RawValue}%' and ";
                            break;
                        case "TypeName":
                            typeSql += $@" case when wi.[object] = 'Issue' then it.Name else assettype.name end  Like '%{ff.RawValue}%' and ";
                            break;
                        case "Type":
                            typeSql += $@"assettype.[Object]='{ff.RawValue}' and ";
                            break;
                        case "StartedOn":
                            typeSql += $@"datediff(day, wi.startedOn, '{ff.RawValue}') = 0 and ";
                            break;
                        case "CompletedOn":
                            typeSql += $@"datediff(day, wi.CompletedOn, '{ff.RawValue}') = 0 and ";
                            break;
                        case "Status":
                            havingSql = $@"case when count(s.StepID) > 0 then    
                                            case when max(vs.ActivityType) = 3 then    'Waiting on user action'                   
                                            else     'Incomplete'    end         
                                            else        'Complete'    end ='{ff.RawValue}'";
                            break;
                        case "Initiator":
                            typeSql += $@"( gr.firstName Like '{ff.RawValue}%' or gr.lastName Like '{ff.RawValue}%' or gr.firstName + ' ' + gr.lastName LIKE '{ff.RawValue}%' ) and ";
                            break;
                        case "AssignedTo":
                            assignedSql = @"inner join workflow.ItemAssignment WIA on WIA.ItemID = WI.ID and WIA.ResourceObject = 'Resource'
                                            inner join reporting.Global_Resource GRA on WIA.ResourceObjectID = GRA.ResourceID ";
                            typeSql += $@"( gra.firstName Like '{ff.RawValue}%' or gra.lastName Like '{ff.RawValue}%' or gra.firstName + ' ' + gra.lastName LIKE '{ff.RawValue}%' ) and ";
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(typeSql))
                {
                    typeSql = typeSql.Trim().TrimEnd( 'a','n','d');
                    whereSql = "where " + typeSql;
                }

                if (!string.IsNullOrEmpty(havingSql))
                {
                    havingSql = $@"having ({havingSql})"  ;
                }

                    sortDataField = string.IsNullOrEmpty(sortDataField) ? "StartedOn" : sortDataField;
                var stFieldType = sortDataField == "StartedOn"  || sortDataField == "CompletedOn" ? "Date" : "string";
                var sortsql = applySortSuffix(string.Empty, sortDataField, sortOrder, "Date", "desc", sortFieldType: stFieldType);
                var pagingSql = applyPagingSuffix(string.Empty, pagenum, pagesize);
                var groupby = @"group by wi.id,wt.name, wi.startedOn,wi.CompletedOn,wi.[object],wi.objectid,cod.id,ass.id, wi.startedOn, wi.CompletedOn,
		                        gr.firstName , gr.lastName,assettype.name, it.Name,assettype.[Object]";

               var fromSql = @"		from [workflow].[type] wt 
                                inner join [workflow].[version] wv on (wt.id = wv.typeid) 
                                inner join [workflow].[item] wi on (wv.id = wi.versionid)	
                                left join workflow.versionstep vs on vs.versionid = wi.id
                                left join workflow.itemstep s on s.stepid = vs.id and s.CompletedOn is null
                                left join [dbo].asset ass on(ass.[object] = wi.[object] and ass.[objectid] = wi.[objectid]) 
                                left join [dbo].assettype assettype on(ass.assettypeid = assettype.id)	         
                                inner join [reporting].global_resource gr on (wi.startedBy = gr.resourceid) left 
                                outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue') 
                                left outer join [dbo].[asset] cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                                left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id) ";
                var sql = $@"
                         select wi.id as Id,                    
                         wt.name as 'WorkflowName' ,                    
                         assettype.[Object] as 'Type',                    
                         case when wi.[object] = 'Issue' then it.Name else assettype.name end as 'TypeName' ,                    
                         case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), 
                         '(unknown relationship)')   when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id)  
                         else coalesce(utility.getassetdisplayvalue(ass.id),'(unknown)') end as 'Asset',                    
                         gr.firstName + ' ' + gr.lastName as 'Initiator' ,                    
                         wi.startedOn as 'StartedOn',                    wi.CompletedOn as 'CompletedOn',                    
                         case when count(s.StepID) > 0 then    case when max(vs.ActivityType) = 3 then    'Waiting on user action'                   
                         else     'Incomplete'    end         else        'Complete'    end as [Status]   
                        {fromSql}
                        {assignedSql}
                        {whereSql} 
                        {groupby}
                        {havingSql}
                        ";

                sql = $@"Select * from ({sql}) as A {sortsql} 
                        {pagingSql}";

                var countSql = $@" select count(1)	
                          {fromSql}
                          {assignedSql}
                          {whereSql} ";


                var list = Company.Query<dynamic>(sql);
                var totalCount = Company.Query<int>(countSql);

                return new JsonNetResult
                {
                    Data = new { Items = list, Total = totalCount },
                    Formatting = Newtonsoft.Json.Formatting.None
                };

            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }


    }
}
