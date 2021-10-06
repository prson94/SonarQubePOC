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
using d360.core.enums;
using System.Collections.Generic;
using d360.model.DataAccessLayer;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/monitor"), Authorize]
    public class MonitorController : BaseController
    {
        #region DI

        public MonitorController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
        }

        #endregion

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
            document.SetCellValue(1, 9, "Workflow Instance UID");
            document.SetCellValue(1, 10, "Url");
            

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
                document.SetCellValue(rowIndex, 9, row.UID.ToString() ?? "");
                document.SetCellValue(rowIndex, 10, "/workflow/details/" + (row.UID.ToString() ?? ""));

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
                case "Business Asset":
                case "Technical Asset":
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
                case "Rule":
                    objType = "RuleType";
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
            List<string> typeClause = new List<string>();
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
                        typeClause.Add($@"wt.id in @{ff.FieldName}{count}");
                        break;
                    case "Asset":
                        dbArgs.Add($"{ff.FieldName}{count}", $"%{ff.RawValue}%");
                        typeClause.Add($@"coalesce(wiis.AssetName,IntersectName.Name,wia.AssetName) Like @{ff.FieldName}{count}");
                        break;
                    case "TypeName":
                        dbArgs.Add($"{ff.FieldName}{count}", $"%{ff.RawValue}%");
                        typeClause.Add($@"coalesce(assettype.Name, it.Name,ITypeName.Name) LIKE @{ff.FieldName}{count}");
                        break;
                    case "Type":
                        switch (ff.RawValue)
                        {
                            case "Action":
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeClause.Add($@"( wi.[object] = 'Issue' or assettype.[Object]=@{ff.FieldName}{count} )");
                                break;
                            case "Business Asset":
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeClause.Add($@"assettype.[Object]= @{ff.FieldName}{count} and assettype.[Class] = {(int)AssetTypeClass.BusinessAsset}");
                                break;
                            case "Technical Asset":
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeClause.Add($@"assettype.[Object]= @{ff.FieldName}{count} and assettype.[Class] = {(int)AssetTypeClass.TechnicalAsset}");
                                break;
                            case "Relationship":
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeClause.Add($@"( assettype.[Object] = @{ff.FieldName}{count} or wi.[object]='Intersect' )");
                                break;
                            default:
                                dbArgs.Add($"{ff.FieldName}{count}", $"{getAssetType(ff.RawValue)}");
                                typeClause.Add($@"assettype.[Object]= @{ff.FieldName}{count}");
                                break;
                        }
                        break;
                    case "StartedOn":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeClause.Add($@"datediff(hour, wi.StartedOn, @{ff.FieldName}{count}) <= 0 and datediff(hour, wi.StartedOn, @{ff.FieldName}{count}) > -24");
                        break;
                    case "CompletedOn":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeClause.Add($@"datediff(hour, wi.CompletedOn, @{ff.FieldName}{count}) <= 0 and datediff(hour, wi.CompletedOn, @{ff.FieldName}{count}) > -24");
                        break;
                    case "Status":
                        typeClause.Add(ff.RawValue == "Pending" ? " wi.CompletedOn is null" : " wi.CompletedOn is not null");
                        break;
                    case "Initiator":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}%");
                        typeClause.Add($@"( gr.firstName Like @{ff.FieldName}{count} or gr.lastName Like @{ff.FieldName}{count} or gr.firstName + ' ' + gr.lastName LIKE @{ff.FieldName}{count} )");
                        break;
                    case "AssignedTo":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}%");
                        assignedSql = @"inner join workflow.ItemAssignment WIA on WIA.ItemID = wi.ID and WIA.ResourceObject = 'Resource'
                                            inner join reporting.Global_Resource GRA on WIA.ResourceObjectID = GRA.ResourceID ";
                        typeClause.Add($@"( gra.firstName Like @{ff.FieldName}{count} or gra.lastName Like @{ff.FieldName}{count} or gra.firstName + ' ' + gra.lastName LIKE @{ff.FieldName}{count} )");
                        break;
                    case "Object":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeClause.Add($@"coalesce(cod.Object, ass.Object) = @{ff.FieldName}{count}");
                        break;
                    case "ObjectID":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeClause.Add($@"coalesce(cod.ObjectID, ass.ObjectID) = cast(@{ff.FieldName}{count} as int)");
                        break;
                    case "ObjectType":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeClause.Add($@"assettype.Object = @{ff.FieldName}{count}");
                        break;
                    case "ObjectTypeID":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeClause.Add($@"assettype.ObjectID = cast(@{ff.FieldName}{count} as int)");
                        break;
                    case "ItemID":
                        dbArgs.Add($"{ff.FieldName}{count}", $"{ff.RawValue}");
                        typeClause.Add($@"wi.ID = cast(@{ff.FieldName}{count} as int)");
                        break;
                }
            }

            if (typeClause.Any())
            {
                whereSql = "where " + string.Join(" and ", typeClause.ToArray());
            }

            if (!string.IsNullOrEmpty(havingSql))
            {
                havingSql = $@"having ({havingSql})";
            }


            var groupby = @"group by wi.id,wt.name, wi.startedOn,wi.CompletedOn,wi.[object],wi.objectid,cod.id,ass.id, wi.startedOn, wi.CompletedOn,
		                        gr.firstName , gr.lastName,assettype.name, it.Name,assettype.[Object],assettype.[Class],ITypeName.Name, assettype.ObjectId, coalesce(cod.Object,ass.Object), coalesce(cod.ObjectId,ass.ObjectId), wi.Uid,IntersectName.Name,wia.AssetName, wiis.AssetName";

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
                                outer apply dbo.GetIntersectTypeNames(inter.IntersectTypeId) ITypeName
								left join [workflow].[ItemAsset] wia on wia.WorkFlowItemId = wi.id
								left join [workflow].[ItemIssue] wiis on wiis.WorkFlowItemId = wi.id
								outer apply (select utility.deriveintersectname(wi.objectid))IntersectName(Name)
";
            var sql = $@"
                            select wi.id as Id,                    
                            wt.name as 'WorkflowName' ,                    
                            case when assettype.[Object] = 'ArtifactType' and assettype.[Class] = {(int)AssetTypeClass.BusinessAsset} then
                            'Business Asset'
                            when assettype.[Object] = 'ArtifactType' and assettype.[Class] = {(int)AssetTypeClass.TechnicalAsset} then
                            'Technical Asset'
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
                         case when wi.[object] = 'Intersect' then coalesce(IntersectName.Name,'(unknown relationship)')   
                         else coalesce(wiis.AssetName,wia.AssetName,'(unknown)') end as 'Asset',                     
                         gr.firstName + ' ' + gr.lastName as 'Initiator' ,                    
                         wi.startedOn as 'StartedOn',                    wi.CompletedOn as 'CompletedOn',
                        case when   wi.CompletedOn is null then    'Pending'            
                        else        'Complete'    end as [Status],
                        assettype.Object as ObjectType,
                        assettype.ObjectID as ObjectTypeID,
                        coalesce(cod.Object, ass.Object) as Object,
                        coalesce(cod.objectID, ass.ObjectID) as ObjectID,
                        wi.Uid as UID
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
