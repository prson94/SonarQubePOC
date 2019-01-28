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
using Dapper;

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

            var querySql = @"select	A.*, coalesce(FA.[TextPath], RF.FusionAttribute) as FusionAttribute 
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


            var dbArgs = new DynamicParameters();
            var sql = GetWorkflowMonitorSql(dbArgs);

            sortDataField = string.IsNullOrEmpty(sortDataField) ? "StartedOn" : sortDataField;
            var stFieldType = sortDataField == "StartedOn" || sortDataField == "CompletedOn" ? "DateTime" : "string";
            var sortsql = applySortSuffix(string.Empty, sortDataField, sortOrder, "DateTime", "desc", sortFieldType: stFieldType);
  

            sql = $@"Select * from ({sql}) as A {sortsql} ";
            var list = Company.Query<dynamic>(sql,dbArgs);

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

        private string getAssetType(string obj)
        {
            string objType = "";
            switch (obj)
            {
                case "Artifact":
                    objType = "ArtifactType";
                    break;
                case "Policy":
                    objType = "PolicyType";
                        break;
                case "Model":
                    objType = "TaxonomyType";
                        break;
                case "Action":
                    objType = "IssueType";
                      break;
                case "Relationship":
                    objType = "IntersectType";
                     break;                
                case "Reference List":
                    objType = "ReferenceItemType";
                     break;
                case "Fusion":
                    objType = "FusionType";
                    break;
            }
            return objType;
        }

        private string GetWorkflowMonitorSql(DynamicParameters dbArgs)
        {
            var filters = GetFilterValuesFromRequest(Request, true);
            string typeSql = "";
            string whereSql = "";
            string assignedSql = "";
            string havingSql = "";
            int count = 0; //same filtername multiple times
            foreach (var f in filters)
            {
                var ff = f as UiRequestFieldFilterValue;
                if (ff == null) continue;
                count++;
                switch (ff.FieldName)
                {
                    case "WorkflowId":
                        var types=  Array.ConvertAll(ff.RawValue.Trim().TrimEnd(',').Split(','), s => int.Parse(s));
                        dbArgs.Add($"{ff.FieldName}{count}", types);
                        typeSql += $@" wt.id in @{ff.FieldName}{count} and ";
                        break;
                    case "Asset":
                        dbArgs.Add($"{ff.FieldName}{count}", $"%{ff.RawValue}%");
                        typeSql += $@" (case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)') 
                                        when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id) 
                                        else coalesce(utility.getassetdisplayvalue(ass.id), '(unknown)') end) Like @{ff.FieldName}{count} and ";
                        break;
                    case "TypeName":
                        dbArgs.Add($"{ff.FieldName}{count}", $"%{ff.RawValue}%");
                        typeSql += $@"  coalesce(assettype.Name, it.Name,ITypeName.Name)  LIKE @{ff.FieldName}{count} and ";
                        break;
                    case "Type":
                            switch(ff.RawValue)
                                {
                            case "Action":
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeSql += $@"( wi.[object] = 'Issue' or assettype.[Object]=@{ff.FieldName}{count} ) and ";
                                break;
                            case "Relationship":
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeSql += $@"( assettype.[Object] = @{ff.FieldName}{count} or wi.[object]='Intersect' ) and";
                                break;
                            default:
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeSql +=  $@"assettype.[Object]= @{ff.FieldName}{count} and ";
                                break;
                        }
                        
                        break;
                    case "StartedOn":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeSql += $@"datediff(day, wi.startedOn, @{ff.FieldName}{count}) = 0 and ";
                        break;
                    case "CompletedOn":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeSql += $@"datediff(day, wi.CompletedOn, @{ff.FieldName}{count}) = 0 and ";
                        break;
                    case "Status":
                        typeSql +=  ff.RawValue == "Pending" ? " wi.CompletedOn is null and " : " wi.CompletedOn is not null and ";
                        break;
                    case "Initiator":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}%");
                        typeSql += $@"( gr.firstName Like @{ff.FieldName}{count} or gr.lastName Like @{ff.FieldName}{count} or gr.firstName + ' ' + gr.lastName LIKE @{ff.FieldName}{count} ) and ";
                        break;
                    case "AssignedTo":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}%");
                        assignedSql = @"inner join workflow.ItemAssignment WIA on WIA.ItemID = wi.ID and WIA.ResourceObject = 'Resource'
                                            inner join reporting.Global_Resource GRA on WIA.ResourceObjectID = GRA.ResourceID ";
                        typeSql += $@"( gra.firstName Like @{ff.FieldName}{count} or gra.lastName Like @{ff.FieldName}{count} or gra.firstName + ' ' + gra.lastName LIKE @{ff.FieldName}{count} ) and ";
                        break;
                    case "Object":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeSql += $@"coalesce(cod.Object, ass.Object) = @{ff.FieldName}{count} and ";
                        break;
                    case "ObjectID":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeSql += $@"coalesce(cod.ObjectID, ass.ObjectID) = cast(@{ff.FieldName}{count} as int) and ";
                        break;
                    case "ObjectType":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeSql += $@"assettype.Object = @{ff.FieldName}{count} and ";
                        break;
                    case "ObjectTypeID":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeSql += $@"assettype.ObjectID = cast(@{ff.FieldName}{count} as int) and ";
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


            var groupby = @"group by wi.id,wt.name, wi.startedOn,wi.CompletedOn,wi.[object],wi.objectid,cod.id,ass.id, wi.startedOn, wi.CompletedOn,
		                        gr.firstName , gr.lastName,assettype.name, it.Name,assettype.[Object],ITypeName.Name, assettype.ObjectId, coalesce(cod.Object,ass.Object), coalesce(cod.ObjectId,ass.ObjectId)";

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
                                left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id) 
                                left  join [dbo].[intersect] inter on (wi.[object]='Intersect' and inter.id=wi.[objectId])
                                outer apply dbo.GetIntersectTypeNames(inter.IntersectTypeId) ITypeName";
            var sql = $@"
                            select wi.id as Id,                    
                            wt.name as 'WorkflowName' ,                    
                            case when assettype.[Object] = 'ArtifactType' then
                            'Artifact'
                            when assettype.[Object] = 'RuleType' then
                            'Rule'
                            when assettype.[Object] = 'PolicyType' then
                            'Policy'
                            when assettype.[Object] = 'TaxonomyType' then
                            'Model'
                            when assettype.[Object] = 'IssueType' or  wi.[object] = 'Issue' then
                            'Action'
                            when assettype.[Object] = 'IntersectType' or wi.[object]='Intersect' then 
                            'Relationship'                    
                            when assettype.[Object] = 'ReferenceItemType' then 
                            'Reference List'  
                            when assettype.[Object] = 'FusionType'  then
                            'Fusion'
                            else
                            ''
                            end as 'Type',                    
                         coalesce(assettype.Name, it.Name,ITypeName.Name) as TypeName,                    
                         case when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), 
                         '(unknown relationship)')   when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id)  
                         else coalesce(utility.getassetdisplayvalue(ass.id),'(unknown)') end as 'Asset',                    
                         gr.firstName + ' ' + gr.lastName as 'Initiator' ,                    
                         wi.startedOn as 'StartedOn',                    wi.CompletedOn as 'CompletedOn',
                        case when   wi.CompletedOn is null then    'Pending'            
                        else        'Complete'    end as [Status],
                        assettype.Object as ObjectType,
                        assettype.ObjectID as ObjectTypeID,
                        coalesce(cod.Object, ass.Object) as Object,
                        coalesce(cod.objectID, ass.ObjectID) as ObjectID  
                        {fromSql}
                        {assignedSql}
                        {whereSql} 
                        {groupby}
                        {havingSql}
                        ";
            return sql;
        }
        [Route("workflowmonitor/items"), HttpGet, NonNullableParameters]
        public JsonNetResult GetWorkflowMonitor( int pagenum, int pagesize, string sortDataField, string sortOrder)
        {

            try
            {
                var dbArgs = new DynamicParameters();
                var sql = GetWorkflowMonitorSql(dbArgs);

                sortDataField = string.IsNullOrEmpty(sortDataField) ? "StartedOn" : sortDataField;
                var stFieldType = sortDataField == "StartedOn" || sortDataField == "CompletedOn" ? "DateTime" : "string";
                var sortsql = applySortSuffix(string.Empty, sortDataField, sortOrder, "DateTime", "desc", sortFieldType: stFieldType);
                var pagingSql = applyPagingSuffix(string.Empty, pagenum, pagesize);

                

                var countSql = $@"Select count(1) from ({sql}) as A ";

                sql = $@"Select * from ({sql}) as A {sortsql} 
                        {pagingSql}";
                               
                var list = Company.Query<dynamic>(sql,dbArgs);
                var totalCount = Company.Query<int>(countSql,dbArgs);

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
