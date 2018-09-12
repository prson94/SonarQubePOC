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
                    case "Name":
                        typeSql += $@" (case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)') 
                                    when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id) 
                                    else coalesce(utility.getassetdisplayvalue(ass.id), '(unknown)') end) Like '{ff.RawValue}%' and ";
                        break;
                    case "TypeName":
                        typeSql += $@" case when wi.[object] = 'Issue' then it.Name else assettype.name end  Like '{ff.RawValue}%' and ";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(typeSql))
            {
                typeSql = typeSql.Trim().TrimEnd('a', 'n', 'd');
                typeSql = "where " + typeSql;
            }

            sortDataField = string.IsNullOrEmpty(sortDataField) ? "WorkflowName" : sortDataField;
            var stFieldType = sortDataField == "StartedOn" || sortDataField == "CompletedOn" ? "Date" : "string";
            var sortsql = applySortSuffix(string.Empty, sortDataField, sortOrder, "Date", "desc", sortFieldType: stFieldType);
          //  var pagingSql = applyPagingSuffix(string.Empty, pagenum, pagesize);

            var sql = $@"
                    select  wt.name as 'WorkflowName' ,
                    case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)') 
                    when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id) 
                    else coalesce(utility.getassetdisplayvalue(ass.id),'(unknown)') end as 'Name',
                    wi.[object] as 'Object' ,wi.[objectid] as 'ObjectID' ,
                    wi.startedOn as 'StartedOn'	,
                    wi.CompletedOn as 'CompletedOn',
                    gr.firstName + ' ' + gr.lastName as 'StartedBy' ,
                    case when wi.[object] = 'Issue' then it.Name else assettype.name end as 'TypeName' ,
                    assettype.[Object] as 'Type' ,assettype.ObjectID as 'ObjectTypeID'	
                    from [workflow].[type] wt 
                    inner join [workflow].[version] wv on (wt.id = wv.typeid) 
                    inner join [workflow].[item] wi on (wv.id = wi.versionid)	
                    left join [dbo].asset ass on(ass.[object] = wi.[object] and ass.[objectid] = wi.[objectid]) 
                    left join [dbo].assettype assettype on(ass.assettypeid = assettype.id)	         
                    inner join [reporting].global_resource gr on (wi.startedBy = gr.resourceid) left 
                    outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue') 
                    left outer join [dbo].[asset] cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                    left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id) 
                    {typeSql} 
                    {sortsql} 
                    ";

               

            var list = Company.Query<dynamic>(sql);

            var document = new SLDocument();
            document.AddWorksheet("Items");
            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Workflow Name");
            document.SetCellValue(1, 2, "Name");
            document.SetCellValue(1, 3, "Type Name");
            document.SetCellValue(1, 4, "Type");
            document.SetCellValue(1, 5, "Started");
            document.SetCellValue(1, 6, "Completed");

            #endregion

            int rowIndex = 1;
            foreach (var row in list)
            {
                rowIndex++;

                document.SetCellValue(rowIndex, 1, row.WorkflowName);
                document.SetCellValue(rowIndex, 2, row.Name);
                document.SetCellValue(rowIndex, 3, row.TypeName ?? "");
                document.SetCellValue(rowIndex, 4, row.Type ?? "");
                document.SetCellValue(rowIndex, 5, row.StartedOn??"");
                SLStyle style = document.CreateStyle();
                style.FormatCode = "mm/dd/yyyy";
                document.SetCellStyle(rowIndex, 5, style);
                document.SetCellValue(rowIndex, 6, row.CompletedOn??"");
                style = document.CreateStyle();
                style.FormatCode = "mm/dd/yyyy";
                document.SetCellStyle(rowIndex, 6, style);

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
                foreach (var f in filters)
                {   var ff = f as UiRequestFieldFilterValue;
                    if (ff == null) continue;
                    switch (ff.FieldName)
                    {
                        case "WorkflowId":
                            var types = ff.RawValue.Trim().TrimEnd(',');
                            typeSql += $@" wt.id in ({types}) and ";
                            break;
                        case "Name":
                            typeSql += $@" (case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)') 
                                        when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id) 
                                        else coalesce(utility.getassetdisplayvalue(ass.id), '(unknown)') end) Like '{ff.RawValue}%' and ";
                            break;
                        case "TypeName":
                            typeSql += $@" case when wi.[object] = 'Issue' then it.Name else assettype.name end  Like '{ff.RawValue}%' and ";
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(typeSql))
                {
                    typeSql = typeSql.Trim().TrimEnd( 'a','n','d');
                    typeSql = "where " + typeSql;
                }

                sortDataField = string.IsNullOrEmpty(sortDataField) ? "WorkflowName" : sortDataField;
                var stFieldType = sortDataField == "StartedOn"  || sortDataField == "CompletedOn" ? "Date" : "string";
                var sortsql = applySortSuffix(string.Empty, sortDataField, sortOrder, "Date", "desc", sortFieldType: stFieldType);
                var pagingSql = applyPagingSuffix(string.Empty, pagenum, pagesize);

                var sql = $@"
                        select wi.id as Id, wt.name as 'WorkflowName' ,
                        case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)') 
                        when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id) 
                        else coalesce(utility.getassetdisplayvalue(ass.id),'(unknown)') end as 'Name',
                        wi.[object] as 'Object' ,wi.[objectid] as 'ObjectID' ,
                        wi.startedOn as 'StartedOn'	,
                        wi.CompletedOn as 'CompletedOn',
                        gr.firstName + ' ' + gr.lastName as 'StartedBy' ,
                        case when wi.[object] = 'Issue' then it.Name else assettype.name end as 'TypeName' ,
                        assettype.[Object] as 'Type' ,assettype.ObjectID as 'ObjectTypeID'	
                        from [workflow].[type] wt 
                        inner join [workflow].[version] wv on (wt.id = wv.typeid) 
                        inner join [workflow].[item] wi on (wv.id = wi.versionid)	
                        left join [dbo].asset ass on(ass.[object] = wi.[object] and ass.[objectid] = wi.[objectid]) 
                        left join [dbo].assettype assettype on(ass.assettypeid = assettype.id)	         
                        inner join [reporting].global_resource gr on (wi.startedBy = gr.resourceid) left 
                        outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue') 
                        left outer join [dbo].[asset] cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                        left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id) 
                        {typeSql} 
                        {sortsql} 
                        {pagingSql}";

                var countSql = $@" select count(1)	
                        from [workflow].[type] wt 
                        inner join [workflow].[version] wv on (wt.id = wv.typeid) 
                        inner join [workflow].[item] wi on (wv.id = wi.versionid)	
                        left join [dbo].asset ass on(ass.[object] = wi.[object] and ass.[objectid] = wi.[objectid]) 
                        left join [dbo].assettype assettype on(ass.assettypeid = assettype.id)	         
                        inner join [reporting].global_resource gr on (wi.startedBy = gr.resourceid) left 
                        outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue') 
                        left outer join [dbo].[asset] cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                        left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id)  {typeSql}";


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
