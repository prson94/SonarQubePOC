using d360.core;
using d360.core.entities;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/artifacts"), Authorize, AiHandleError]
    public class ArtifactsController : BaseController
    {
        #region DI

        public ArtifactsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Exports

        [Route("download/excel/{id:int}.xls"), FileDownload, HttpGet]
        public FileResult ToExcel(int id, string sortDataField, string sortOrder, string filter, string ownerUsers = "", string ownerGroups = "", bool listableOnly = true)
        { 
            var joins = "";
            var columns = "";

            var typesToAvoid = new List<string>() {
                DataType.Attribute.ToString(),
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.FilteredLookup.ToString(),
                DataType.FusionLookup.ToString(),
                DataType.OwnershipLookup.ToString()
            };
            var fields = getFieldTypesByObjectType("ArtifactType", id, listableOnly).Where(i => !typesToAvoid.Contains(i.Type)).ToList();

            getDynamicFieldJoinStatements(id, "Artifact", out joins, out columns, true, false, listableOnly, fields, "A.ObjectID");

            var dbArgs = new Dapper.DynamicParameters();

            dbArgs.Add("id", id);

            joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups);

            var parentIntersectType = Company.Filter<IntersectType>(i => i.Object == "ArtifactType" && i.ObjectID == id && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy).FirstOrDefault();

            var parentSqlColumn = @"null as ParentID, null as Parent, null as ParentUrl,";
            var parentSqlJoin = @"";

            if (parentIntersectType != null)
            {
                parentSqlColumn = @"P.ParentID, P.DisplayValue as Parent, P.ParentUrl, ";
                parentSqlJoin = @" outer apply (
				    select	I.SubjectID as ParentID,
                            ID.DisplayValue,
                            dbo.GenerateObjectUrl('Artifact', IAT.ObjectID, I.SubjectID) as ParentUrl
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = A.Object and I.ObjectID = A.ObjectID and IA.Object = 'Artifact' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            left join dbo.GetAssetDisplayValue() ID on ID.ID = IA.ID
				    ) P";
            }


            #region Sql

            //A.ID as AssetID, 
        
            var sql = $@"
select	A.ObjectID as ID,
        {parentSqlColumn}
        {columns}
        A.ID as AssetID,dbo.GenerateNgObjectUrl('Artifact', A.TypeID, A.ObjectID) as Url
from	AssetDetail A 
        {parentSqlJoin} 
        {joins} 
where   A.Type = 'ArtifactType' and A.TypeID = @id and A.[State] = 1 and not exists (select 1 from AssetWithoutReadPermission RP where RP.ResourceID = {Company.CurrentResourceID} and RP.AssetID = A.ID)";

            #endregion

            //if simple filter specified add that citeria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                var fixedColumns = new List<string>();

                if (parentIntersectType != null)
                    fixedColumns.Add("P.DisplayValue"); //Owner/Parent

                sql = $"{sql} and {addDynamicFieldSimpleFilter(fixedColumns.ToArray(), "Artifact", id, filter, dbArgs)}";
            }

            var type = Company.GetById<ArtifactType>(id);
            
            sql = string.Format(@"select * from ({0}) A", sql);

            sql = applyFilteringSuffixBind(sql, Request, dbArgs, fields: fields);
            
            if (string.IsNullOrEmpty(sortDataField))
            {
                var sortSql = "";

                foreach (var field in fields.Where(i => i.SortOrder > 0).OrderBy(i => i.SortOrder))
                {
                    var columnName = $"Field{field.ID}";
                    if (field.Type == "Number")
                        sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST([{columnName}] AS int)";
                    else
                        sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"[{columnName}]";
                }

                if(!string.IsNullOrEmpty(sortSql))
                    sql += " ORDER BY " + sortSql;
            }
            else
            {
                //The user sorted by something else, other than the default SortOrder settings on the FieldTypes.                
                sql = applySortSuffix(sql, sortDataField, sortOrder, sortFieldType: sortColumnType(sortDataField, fields));
            }

            if (Company.TypeHasParent(SystemObjects.ArtifactType, type.ID))
                fields.Insert(0, new FieldType { Type = "string", Name = "Parent", FriendlyName = "Parent" });

            fields.Add(new FieldType { Type = "Number", Name = "AssetID", FriendlyName = "Asset ID" });
            fields.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "Url" });

            var results = Company.Query<dynamic>(sql, dbArgs);            
            var document = GenerateDefaultSpreadsheet(fields, results);
            
            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Filtered {type.Name} List for {DateTime.Now.ToShortDateString()}.xlsx");
        }

        [Route("download/customexcel/{templateId:int}/{artifactTypeId:int}.xls"), FileDownload, HttpGet]
        public FileResult ToCustomExcel(int templateId, int artifactTypeId, string sortDataField, string sortOrder, string filter, string ownerUsers = "", string ownerGroups = "", bool listableOnly = false)
        {
            var joins = "";
            var columns = "";

            var typesToAvoid = new List<string>() {
                DataType.Attribute.ToString(),
                DataType.ComplexRelationLookup.ToString(),
                DataType.DataTableSelect.ToString(),
                DataType.FilteredLookup.ToString(),
                DataType.FusionLookup.ToString(),
                DataType.OwnershipLookup.ToString()
            };
            
            var dbArgs = new Dapper.DynamicParameters();

            dbArgs.Add("id", artifactTypeId);

            joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups);

            var template = Company.ArtifactTypeExportTemplates.Where(x => x.ID == templateId).FirstOrDefault();

            if(template == null)
            {
                throw new Exception("INVALID TEMPLATE ID SPECIFIED.");
            }
                        
            var fields = getFieldTypesByObjectType("ArtifactType", artifactTypeId, listableOnly).Where(i => !typesToAvoid.Contains(i.Type)).ToList();

            getDynamicFieldJoinStatements(artifactTypeId, "Artifact", out joins, out columns, true, false, listableOnly, fields, "A.ObjectID");
            
            var oldFields = new List<FieldType>(fields);
            //if include fields is specified only include field ids from list
            if (!string.IsNullOrEmpty(template.IncludeFields))
            {
                var fieldIdList = template.IncludeFields.Split(',').Select(int.Parse);

                fields.Clear();

                //done this way to set order of fields in spreadsheet to the order specified in include fields.
                foreach (var fieldId in fieldIdList)
                {
                    var field = oldFields.Find(x => x.ID == fieldId);
                    if (field != null) fields.Add(field);
                }                
            }

            var parentIntersectType = Company.Filter<IntersectType>(i => i.Object == "ArtifactType" && i.ObjectID == artifactTypeId && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy).FirstOrDefault();

            var parentSqlColumn = @"null as ParentID, null as Parent, null as ParentUrl,";
            var parentSqlJoin = @"";

            if (parentIntersectType != null)
            {
                parentSqlColumn = @"P.ParentID, P.DisplayValue as Parent, P.ParentUrl, ";
                parentSqlJoin = @" outer apply (
				    select	I.SubjectID as ParentID,
                            ID.DisplayValue,
                            dbo.GenerateObjectUrl('Artifact', IAT.ObjectID, I.SubjectID) as ParentUrl
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = 'Artifact' and I.ObjectID = A.ID and IA.Object = 'Artifact' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            left join dbo.GetAssetDisplayValue() ID on ID.ID = IA.ID
				    ) P";
            }

            #region Sql

            var sql = $@"
select	A.ObjectID as ID,
        {parentSqlColumn}
        {columns}
		dbo.GenerateNgObjectUrl('Artifact', A.TypeID, A.ObjectID) as Url
from	AssetDetail A 
        {parentSqlJoin}
        {joins} 
where   A.Type = 'ArtifactType' and A.TypeID = @id and A.[State] = 1 and not exists (select 1 from AssetWithoutReadPermission RP where RP.ResourceID = {Company.CurrentResourceID} and RP.AssetID = A.ID)";

            #endregion

            //if simple filter specified add that citeria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                var fixedColumns = new List<string>();
                fixedColumns.Add("A.DisplayValue");

                if (parentIntersectType != null)
                    fixedColumns.Add("P.DisplayValue"); //Owner/Parent

                sql = $"{sql} and {addDynamicFieldSimpleFilter(fixedColumns.ToArray(), "Artifact", artifactTypeId, filter, dbArgs)}";
            }

            var type = Company.GetById<ArtifactType>(artifactTypeId);

            sql = string.Format(@"select * from ({0}) A", sql);

            sql = applyFilteringSuffixBind(sql, Request, dbArgs, fields: oldFields);
                        
            if (string.IsNullOrEmpty(sortDataField))
            {
                var sortSql = "";

                foreach (var field in fields.Where(i => i.SortOrder > 0).OrderBy(i => i.SortOrder))
                {
                    var columnName = $"Field{field.ID}";
                    if(field.Type == "Number")                        
                        sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST([{columnName}] AS int)";
                    else
                        sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"[{columnName}]";
                }

                
                sql += " ORDER BY " + sortSql;
            }
            else
            {
                //The user sorted by something else, other than the default SortOrder settings on the FieldTypes.                
                sql = applySortSuffix(sql, sortDataField, sortOrder, sortFieldType: sortColumnType(sortDataField, oldFields));
            }


            var results = Company.Query<dynamic>(sql, dbArgs);
                                    
            SLDocument document = null;
            if (template.IncludeParent && Company.TypeHasParent(SystemObjects.ArtifactType, type.ID)) fields.Insert(0, new FieldType { Type = "string", Name = "Parent", FriendlyName = "Parent" });
            if (template.IncludeUrl) fields.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "Url" });

            var styles = template.ArtifactTypeExportTemplateStyles;

            switch (template.ExportViewType)
            {
                case core.enums.ExportView.None:
                    document = GenerateDefaultSpreadsheet(fields,results, template, "Items");
                    break;
                case core.enums.ExportView.Pivot:
                    document = GeneratePivotedSpreadsheet(fields, results, template, "Items");
                    break;
                case core.enums.ExportView.Grouped:
                    document = GenerateGroupedSpreadsheet(fields, results, template, "Items");
                    break;
                default:
                    throw new Exception("INVALID EXPORT VIEW TYPE SPECIFIED");
            }

            // Select the first worksheet as the active one.
            var firstSheet = document.GetWorksheetNames()[0];
            document.SelectWorksheet(firstSheet);

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{template.Name} for {DateTime.Now.ToShortDateString()}.xlsx");
        }

        private SLDocument GenerateDefaultSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, ArtifactTypeExportTemplate template = null, string worksheetName = "Items")
        {
            ICollection<ArtifactTypeExportTemplateStyle> styles = null;
            if (template != null)
            {
                styles = template.ArtifactTypeExportTemplateStyles;
            }

            int index = 1;
            var document = createExcelBaseDocument(template, worksheetName);

            #region Header

            SetRowStyles(document, 1, styles);

            foreach (var field in fields)
            {
                SetColumnStyles(document, index, styles);

                document.SetCellValue(1, index++, (string)field.FriendlyName);                
            }

            #endregion

            int rowNumber = 1;
            foreach (var row in results)
            {
                index = 1;
                rowNumber++;
                
                foreach (var field in fields)
                {
                    var val = getRowFieldValue(row, field);
                    SetSpreadsheetValueFromField(document, rowNumber, index, field, val);
                    SetColumnStylesFromField(styles, document, rowNumber, index, field, row);
                    index++;
                }                
            }

            SetExcelColumnWidths(document, fields);

            return document;
        }

        private SLDocument GenerateGroupedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, ArtifactTypeExportTemplate template, string worksheetName = "Items")
        {
            var styles = template.ArtifactTypeExportTemplateStyles;

            int index = 1;
            var document = createExcelBaseDocument(template, worksheetName);

            #region Header

            SetRowStyles(document, 1, styles);

            foreach (var field in fields)
            {
                SetColumnStyles(document, index, styles);
                document.SetCellValue(1, index++, (string)field.FriendlyName);
            }

            #endregion
            
            int rowNumber = 1;
            dynamic previousRow = null;

            foreach (var row in results)
            {
                bool rowSameAsPrevious = true;

                if (previousRow == null) rowSameAsPrevious = false;
                
                index = 1;
                rowNumber++;
                
                foreach (var field in fields)
                {
                    var val = getRowFieldValue(row, field);
                    var previousVal = previousRow != null ? getRowFieldValue(previousRow, field) :"";

                    rowSameAsPrevious = rowSameAsPrevious && (val == previousVal);

                    if (!rowSameAsPrevious)
                    {
                        SetSpreadsheetValueFromField(document, rowNumber, index, field, val);
                        SetColumnStylesFromField(styles, document, rowNumber, index, field, row);
                        index++;                        
                    }
                    else
                    {
                        document.SetCellValue(rowNumber, index++, "");
                        SetColumnStylesFromField(styles, document, rowNumber, index, field, row);
                    }
                }
                
                previousRow = row;
            }

            SetExcelColumnWidths(document, fields);

            return document;
        }

        private SLDocument GeneratePivotedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, ArtifactTypeExportTemplate template, string worksheetName = "Items")
        {
            var styles = template.ArtifactTypeExportTemplateStyles;

            int index = 1;

            var document = createExcelBaseDocument(template, worksheetName);

            var uniques = new List<string>();

            int columnNumber = 0;
            foreach (var row in results)
            {                
                var concatenatedValue = "";
                foreach (var field in fields)
                {
                    concatenatedValue += getRowFieldValue(row, field);
                }
                if (!uniques.Contains(concatenatedValue))
                {
                    index = 1;
                    columnNumber++;

                    uniques.Add(concatenatedValue);
                    foreach (var field in fields)
                    {
                        var val = getRowFieldValue(row, field);
                        SetSpreadsheetValueFromField(document, index, columnNumber, field, val);
                        SetRowStylesFromField(styles, document, index, columnNumber, field, row);

                        index++;                        
                    }
                }
            }

            for(int i = 1; i < index; i++)
            {
                SetRowStyles(document, i, styles);
            }

            document.AutoFitColumn(1, columnNumber);

            return document;
        }

        private void SetRowStylesFromField(ICollection<ArtifactTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
        {
            if (!styles.Any()) return;

            //check if the styles collection has an entry for this row
            var style = styles.Where(x => x.Row == rowIndex && x.Column == -1 && (x.BackgroundColorValueFieldTypeID > 0 || x.ColorValueFieldTypeID > 0)).FirstOrDefault();

            if (style != null)
            {
                //we have a style based on the value in another column(s)
                document.SetCellStyle(rowIndex, columnIndex, CreateStyle(style, row));
            }
        }

        private void SetColumnStylesFromField(ICollection<ArtifactTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
        {            
            if (styles != null && styles.Any())
            {

                //check if the styles collection has an entry for this row
                var style = styles.Where(x => x.Row == -1 && x.Column == columnIndex && (x.BackgroundColorValueFieldTypeID > 0 || x.ColorValueFieldTypeID > 0)).FirstOrDefault();

                if (style != null)
                {
                    var st = CreateStyle(style, row);
                    if (field.Type == "Date")
                        st.FormatCode = "m/d/yyyy";
                    
                    //we have a style based on the value in another column(s)
                    document.SetCellStyle(rowIndex, columnIndex, st);
                }
            }            
        }

        private SLDocument createExcelBaseDocument(ArtifactTypeExportTemplate template, string worksheetName)
        {
            SLDocument document = null;

            if (template == null)
            {
                template = new ArtifactTypeExportTemplate();
            }

            if (template.TemplateFile != null)
            {
                document = new SLDocument(new MemoryStream(template.TemplateFile));
                document.AddWorksheet(worksheetName);
            }
            else
            {
                document = new SLDocument();
                document.RenameWorksheet("Sheet1", worksheetName);

                if (!string.IsNullOrEmpty(template.UsageNotes))
                {
                    var wk = "Usage Notes";
                    document.AddWorksheet(wk);
                    document.MoveWorksheet(wk, 0);
                    document.SelectWorksheet(wk);

                    document.SetCellValue("A1", "Usage Notes");
                    document.SetCellValue("A2", template.UsageNotes);
                    document.SetColumnWidth(0, 600);
                }

                document.SelectWorksheet(worksheetName);
            }

            return document;
        }

        private void SetColumnStyles(SLDocument document, int column, ICollection<ArtifactTypeExportTemplateStyle> styles)
        {
            if (styles == null) return;

            //style for the whole column
            var columnStyle = styles.Where(x => x.Row == -1 && x.Column == column).FirstOrDefault();

            if (columnStyle != null)
            {
                document.SetColumnStyle(column, CreateStyle(columnStyle));
            }

            //style for the header
            var columnheaderStyle = styles.Where(x => x.Row == 1 && x.Column == column).FirstOrDefault();

            if (columnheaderStyle != null)
            {
                document.SetCellStyle(1,column, CreateStyle(columnheaderStyle));
            }            
        }

        private void SetRowStyles(SLDocument document, int row, ICollection<ArtifactTypeExportTemplateStyle> styles)
        {
            if (styles == null) return;

            var columnStyle = styles.Where(x => x.Row == row && x.Column == -1).FirstOrDefault();

            if (columnStyle == null) return;

            document.SetRowStyle(row, CreateStyle(columnStyle));
        }

        private SLStyle CreateStyle(ArtifactTypeExportTemplateStyle columnStyle, dynamic row = null)
        {
            SLStyle style = new SLStyle();

            if (columnStyle.BackgroundColor.HasValue)
            {
                style.Fill.SetPatternType(DocumentFormat.OpenXml.Spreadsheet.PatternValues.Solid);
                style.Fill.SetPatternForegroundColor(System.Drawing.Color.FromArgb(columnStyle.BackgroundColor.Value));
            }

            if (columnStyle.Color.HasValue)
                style.SetFontColor(System.Drawing.Color.FromArgb(columnStyle.Color.Value));

            if (columnStyle.BackgroundColorValueFieldTypeID > 0 && row != null)
            {
                var color = getRowFieldValue(row, columnStyle.BackgroundColorValueFieldTypeID);
                if (!string.IsNullOrWhiteSpace(color))
                {
                    style.Fill.SetPatternType(DocumentFormat.OpenXml.Spreadsheet.PatternValues.Solid);
                    style.Fill.SetPatternForegroundColor(System.Drawing.ColorTranslator.FromHtml(color));
                }
            }

            if(columnStyle.ColorValueFieldTypeID > 0 && row != null)
            {
                var color = getRowFieldValue(row, columnStyle.ColorValueFieldTypeID);
                if (!string.IsNullOrWhiteSpace(color))
                {
                    style.SetFontColor(System.Drawing.ColorTranslator.FromHtml(color));
                }
            }

            style.SetFontBold(columnStyle.IsBold);

            return style;
        }
        
        private void SetExcelColumnWidths(SLDocument document, List<FieldType> fields)
        {
            int index = 1;
            foreach (var field in fields)
            {
                if (field.ColumnWidth.HasValue)
                {
                    int width = field.ColumnWidth.Value > 0 ? field.ColumnWidth.Value / 10 : 0;
                    document.SetColumnWidth(index, width);                    
                }
                else
                {
                    document.AutoFitColumn(index);
                }
                index++;
            }
        }
        
        private void SetSpreadsheetValueFromField(SLDocument document, int rowIndex, int columnIndex, FieldType field, string value)
        {
            switch ((field.Type ?? "").ToUpper())
            {
                case "DECIMAL":
                    double dVal = 0;
                    if (double.TryParse(value, out dVal))
                        document.SetCellValue(rowIndex, columnIndex, dVal);
                    else
                        document.SetCellValue(rowIndex, columnIndex, value);
                    break;
                case "NUMBER":
                    int intVal = 0;
                    if (int.TryParse(value, out intVal))
                        document.SetCellValue(rowIndex, columnIndex, intVal);
                    else
                        document.SetCellValue(rowIndex, columnIndex, value);
                    break;
                case "DATE":
                    if (DateTime.TryParse((value ?? "").ToString(), out DateTime dateVal))
                    {
                        document.SetCellValue(rowIndex, columnIndex, dateVal);

                        SLStyle style = document.CreateStyle();
                        style.FormatCode = "m/d/yyyy";
                        document.SetCellStyle(rowIndex, columnIndex, style);
                    }
                    break;
                default:
                    var doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(value+"");
                    document.SetCellValue(rowIndex, columnIndex, doc.DocumentNode.InnerText);
                    break;
            }
        }

        private string getRowFieldValue(dynamic row, FieldType field)
        {
            if(field != null && field.ID > 0)
                return (((row as IDictionary<string, object>)[$"Field{field.ID}"]) ?? "").ToString();
            else if(field != null && field.Name == "Parent")
                return (string)((row as IDictionary<string, object>)["Parent"]);
            else if (field != null && field.Name == "Url")
                return (string)((row as IDictionary<string, object>)["Url"]);
            else if (field != null && field.Name == "AssetID")
                return (string)((row as IDictionary<string, object>)["AssetID"].ToString());
            return "";
        }

        private string getRowFieldValue(dynamic row, int fieldId)
        {
            if(fieldId>0)
                return (string)((row as IDictionary<string, object>)[$"Field{fieldId}"]);            
            return "";
        }

        #endregion

        #region Json

        [HttpGet, Route("artifactsbyparent"), NonNullableParameters]
        public JsonNetResult ArtifactsByParent(int parentID, int childArtifactTypeID, string sortDataField, string sortOrder, string filter, int pagenum = 0 , int pagesize = 20)
        {            
            return ByParent(parentID, sortDataField, sortOrder, filter, pagenum, pagesize, childArtifactTypeID);
        }

        [HttpPost, Route("byparent"), NonNullableParameters]
        public JsonNetResult ByParent(int parentID, string sortDataField, string sortOrder, string filter, int pagenum = 0, int pagesize = 20, int childArtifactTypeID = 0)
        {
            var d = new Dictionary<string, object>();
            d.Add("p", parentID);

            var parent = Company.Filter<Asset>(a => a.Object == "Artifact" && a.ObjectID == parentID);
            if (parent == null) throw new Exception("Parent Not found");

            var sql = @"
select	O.ID as AssetID,
        O.ObjectID as ID,
        P.ID as ParentID,
        P.DisplayValue as Parent,
        dbo.GenerateObjectUrl('Artifact', P.TypeID, P.ObjectID) as ParentUrl,
        {0}
        dbo.GenerateObjectUrl('Artifact', O.TypeID, O.ObjectID) as Url
from	AssetDetail O
        {1} 
        inner join [PredicateIntersect] PI on PI.Subject = 'Artifact' and PI.Object = O.Object and PI.SubjectID = @p and PI.ObjectID = O.ObjectID and PI.PredicateType = 3 
        inner join AssetDetail P on P.Object = PI.Subject and P.ObjectID = PI.SubjectID 
where   O.Type = 'ArtifactType' and O.TypeID = @id and O.[State] = 1 
        and not exists (select 1 from AssetWithoutReadPermission RP where RP.ResourceID = " + Company.CurrentResourceID + @" and RP.AssetID = O.ID)";
            var model = processDynamicResults(
                sql, Request,
                "ArtifactType", childArtifactTypeID,
                true,
                sortDataField, sortOrder, pagenum, pagesize,
                new string[] { "P.DisplayValue" },
                filter, extraParams: d, applyHiddenFilters: true, includeIdColumn: false, idColumn: "O.ID", innerIdColumn:"O.ObjectID");
            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("artifactsbytype"), NonNullableParameters]
        public JsonNetResult ArtifactsByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter, string ownerUsers = "", string ownerGroups = "")
        {
            return ByType(id, sortDataField, sortOrder, pagenum, pagesize, filter, ownerUsers, ownerGroups);
        }

        //        [HttpPost, Route("bytype"), NonNullableParameters]
        //        public JsonNetResult ByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter, string ownerUsers = "", string ownerGroups = "")
        //        {
        //            var objType = "ArtifactType";
        //            var obj = "Artifact";


        //            try
        //            {
        //                var type = Company.Filter<AssetType>(i => i.Object == objType && i.ObjectID == id).SingleOrDefault();
        //                if (type == null) throw new Exception("Asset Type Not found");

        //                var fields = Company.Filter<FieldType>(i => i.AssetTypeID == type.ID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList();
        //                var parentIntersectType = Company.Filter<IntersectType>(i => i.Object == objType && i.ObjectID == id && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy).FirstOrDefault();

        //                var requestParams = Request.Params;
        //                var dbArgs = new Dapper.DynamicParameters();
        //                dbArgs.Add("id", type.ID);


        //                var parentSqlColumn = @"null as ParentID, null as Parent, null as ParentUrl,";
        //                var parentSqlJoin = @"";

        //                if (parentIntersectType != null)
        //                {
        //                    parentSqlColumn = @"PID.ParentID, PID.ParentDisplayValue as Parent, PID.ParentUrl, ";
        //                    parentSqlJoin = @" cross apply [dbo].[GetArtifactParentByAssetID](A.ID) PID";
        //                }

        //                var columnsNamesStatement = string.Join(", ", fields.Select(o => $"[{o.ID}] as Field{o.ID}"));
        //                var fieldIDs = string.Join(", ", fields.Select(o => $"{o.ID}"));
        //                var blockFieldIDs = string.Join(", ", fields.Select(o => $"[{o.ID}]"));
        //                var sortFields = string.Join(", ", fields.Select(o => $"[Field{o.ID}]"));

        //                var simpleWhereClause = "";
        //                if (!string.IsNullOrEmpty(filter))
        //                {
        //                    simpleWhereClause = string.Join(" or ", fields.Select(o => $"pvt.[{o.ID}] like @sfilter + '%'"));
        //                    simpleWhereClause = "where " + simpleWhereClause;
        //                    dbArgs.Add("sfilter", filter);
        //                }

        //                var countColumnPlaceholder = "{0}";
        //                var orderPagingPlaceholder = "{1}";

        //                var sql = $@"
        //select {countColumnPlaceholder}
        //from	(
        //		select	AssetID, Object, ObjectID, Type, TypeID,
        //				{columnsNamesStatement}
        //		from	(
        //				select	A.ID as AssetID,
        //						A.Object,
        //						A.ObjectID,        
        //						AST.Object as Type,
        //						AST.ObjectID as TypeID,
        //						{parentSqlColumn} 
        //						Field_TT.ID as FieldTypeID,
        //						case 
        //							when Field_TT.AllowAllValue = 1 and Field_T.Value = '0' then Field_TT.AllowAllLabel 
        //							when Field_T.Value is not null then Field_T.FormattedValue 
        //							when Field_TT.DefaultValue is not null then Field_TT.DefaultFormattedValue 
        //							else '' 
        //						end as [Field],
        //						1 P_CanEdit, 
        //						1 P_CanDelete,
        //						dbo.GenerateObjectUrl('Artifact', AST.ObjectID, A.ObjectID) as Url
        //				from	Asset A 
        //						inner join AssetType AST on AST.ID = A.AssetTypeID and AST.Object = 'ArtifactType' and AST.ObjectID = 2 and A.State = 1
        //		                {parentSqlJoin} 
        //						inner join FieldType Field_TT on Field_TT.ID in ({fieldIDs}) and Field_TT.AssetTypeID = A.AssetTypeID
        //						left join Field Field_T on Field_T.AssetID = A.ID and Field_T.FieldTypeID = Field_TT.ID  

        //				where   not exists (select 1 from AssetWithoutReadPermission RP where RP.ResourceID = 1 and RP.AssetID = A.ID)
        //				) A
        //		pivot	(
        //				MIN([Field]) for FieldTypeID in ({blockFieldIDs})
        //				) pvt
        //				{simpleWhereClause}
        //		{orderPagingPlaceholder}
        //		) A";

        //                #region Field and Other Logic

        //        //        var columns = "";
        //        //        var joins = "";
        //        //        var wheres = new List<string>();

        //        //        var relationFieldInfos = getRelationFieldData(objType, id, fields);

        //        //        foreach (var f in fields)
        //        //        {
        //        //            var name = $"Field{f.ID}";
        //        //            var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");

        //        //            if (f.Type == DataType.Relationship.ToString() || f.Type == DataType.FieldFromRelationship.ToString())
        //        //            {
        //        //                var relationFieldInfo = relationFieldInfos.SingleOrDefault(i => i.FieldTypeID == f.ID);

        //        //                //if (includeIdColumn) columns += $"{name}_T.ID as [{name}ID], ";

        //        //                if (relationFieldInfo != null)
        //        //                {
        //        //                    joins += $" left join [Intersect] {name}_T on {name}_T.IntersectTypeID = {f.LookupObjectID} and";
        //        //                    joins += relationFieldInfo.IsSubject ?
        //        //                                $" {name}_T.Subject = '{obj}' and {name}_T.SubjectID = A.ObjectID" :
        //        //                                $" {name}_T.Object = '{obj}' and {name}_T.ObjectID = A.ObjectID";

        //        //                    if (f.Type == DataType.Relationship.ToString())
        //        //                    {
        //        //                        var tableName = relationFieldInfo.Object.Replace("Type", "");
        //        //                        var typeIDColumnName = relationFieldInfo.Object + "ID";

        //        //                        columns += $"{name}_OTD.DisplayValue as [{name}], ";
        //        //                        joins += $" left join [{tableName}] {name}_OT on {name}_OT.{typeIDColumnName} = {relationFieldInfo.ObjectID} AND ";
        //        //                        joins += $"{name}_OT.ID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID");
        //        //                        joins += $" left join Asset {name}_AS on {name}_AS.Object = '{tableName}' and  {name}_AS.ObjectId = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID");
        //        //                        joins += $" cross apply [dbo].GetAssetDisplayValueById({name}_AS.ID) {name}_OTD";

        //        //                        // If simple filter specified add that criteria to the sql
        //        //                        if (!string.IsNullOrEmpty(filter))
        //        //                        {
        //        //                            wheres.Add($"{name}_OTD.DisplayValue like @simpleFilter + '%'");
        //        //                        }
        //        //                    }
        //        //                    else if (f.Type == DataType.FieldFromRelationship.ToString())
        //        //                    {
        //        //                        columns += $"{name}_OT.FormattedValue as [{name}], ";
        //        //                        joins += $" left join [Field] {name}_OT on {name}_OT.FieldTypeID = {f.LookupObjectFieldTypeID}";
        //        //                        joins += $" and {name}_OT.ObjectType = {name}_T." + (relationFieldInfo.IsSubject ? "Object" : "Subject");
        //        //                        joins += $" and {name}_OT.ObjectID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID");

        //        //                        // If simple filter specified add that criteria to the sql
        //        //                        if (!string.IsNullOrEmpty(filter))
        //        //                        {
        //        //                            wheres.Add($"{name}_OT.FormattedValue like @simpleFilter + '%'");
        //        //                        }
        //        //                    }
        //        //                }
        //        //            }
        //        //            else
        //        //            {
        //        //                joins += $@"
        //        //inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.AssetTypeID = AST.ID 
        //        //left join Field {name}_T on {name}_T.AssetID = A.ID and {name}_T.FieldTypeID = {name}_TT.ID ";

        //        //                //if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";

        //        //                switch (f.Type)
        //        //                {
        //        //                    case "Decimal":

        //        //                        columns += $@"
        //        //case     
        //        //    when {name}_T.Value is not null then cast({name}_T.FormattedValue as decimal(38,6))
        //        //    when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as decimal(38,6))
        //        //    else null 
        //        //end as [{name}], ";
        //        //                        break;
        //        //                    case "Number":
        //        //                        columns += $@"
        //        //case     
        //        //    when {name}_T.Value is not null then cast({name}_T.FormattedValue as bigint)
        //        //    when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as bigint)
        //        //    else null 
        //        //end as [{name}], ";
        //        //                        break;
        //        //                    case "DateTime":
        //        //                        columns += $@"
        //        //case     
        //        //    when {name}_T.Value is not null then cast({name}_T.FormattedValue as datetime)
        //        //    when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as datetime)
        //        //    else null 
        //        //end as [{name}], ";
        //        //                        break;
        //        //                    default:
        //        //                        columns += $@"
        //        //case 
        //        //    when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
        //        //    when {name}_T.Value is not null then {name}_T.FormattedValue 
        //        //    when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
        //        //    else '' 
        //        //end as [{name}], ";
        //        //                        break;
        //        //                }

        //        //                // If simple filter specified add that criteria to the sql
        //        //                if (!string.IsNullOrEmpty(filter))
        //        //                {
        //        //                    wheres.Add($@"case when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel when {name}_T.Value is not null then {name}_T.FormattedValue when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue else '' end like @simpleFilter + '%'");
        //        //                }
        //        //            }
        //        //        }



        //        //        // Ownership Joins
        //        //        joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups, "A.ObjectID");

        //        //        if (Company.CurrentResourceIsAdmin)
        //        //        {
        //        //            columns += "1 P_CanEdit, 1 P_CanDelete,";
        //        //        }
        //        //        else
        //        //        {
        //        //            columns += "S_E.P_CanEdit, S_D.P_CanDelete, ";
        //        //            joins += $@"
        //        //    cross apply (
        //        //				select	case 
        //        //							when count(1) > 0 then 1
        //        //							else 0
        //        //						end as P_CanEdit
        //        //				from	SecurityDetail 
        //        //				where	(
        //        //						(IsType = 0 and Object = A.Object and ObjectID = A.ObjectID) OR 
        //        //						(IsType = 1 and Object = A.Type and ObjectID = A.TypeID)
        //        //						) and Claim = 3 and ClaimObject = 1 and ResponsibleObjectID = {Company.CurrentResourceID}
        //        //				) S_E
        //        //    cross apply (
        //        //				select	case 
        //        //							when count(1) > 0 then 1
        //        //							else 0
        //        //						end as P_CanDelete
        //        //				from	SecurityDetail 
        //        //				where	(
        //        //						(IsType = 0 and Object = A.Object and ObjectID = A.ObjectID) OR 
        //        //						(IsType = 1 and Object = A.Type and ObjectID = A.TypeID)
        //        //						) and Claim = 3 and ClaimObject = 1 and ResponsibleObjectID = {Company.CurrentResourceID}
        //        //				) S_D ";
        //        //            //columns += $" (select count(1) from securitydetail p_sd_edit where ({innerIdColumn} = p_sd_edit.ObjectID and p_sd_edit.Object = 'Artifact' and p_sd_edit.Claim = 3 and p_sd_edit.ClaimObject = 1 and p_sd_edit.ResponsibleObjectType = 'Resource' and p_sd_edit.ResponsibleObjectID = {Company.CurrentResourceID})) as P_CanEdit, ";
        //        //            //columns += $" (select count(1) from securitydetail p_sd_delete where ({innerIdColumn} = p_sd_delete.ObjectID and p_sd_delete.Object = 'Artifact' and p_sd_delete.Claim = 4 and p_sd_delete.ClaimObject = 1 and p_sd_delete.ResponsibleObjectType = 'Resource' and p_sd_delete.ResponsibleObjectID = {Company.CurrentResourceID})) as P_CanDelete, ";
        //        //        }

        //        //        var querySql = $@"select * from ({string.Format(sql, columns, joins)}) A";
        //        //        var countSql = $@"select count(1) from ({string.Format(sql, "", joins)}) A";

        //        //        #region Filtering

        //        //        var filters = applyRelationFilteringExistsRawSuffix(Request, dbArgs, fields, "A.ObjectID");

        //        //        countSql += filters;
        //        //        querySql += filters;

        //        //        filters += applyFilteringSuffixBindRaw(Request, dbArgs, true, fields, "A.ObjectID");  // Filtering

        //        //        if (wheres.Count > 0)
        //        //        {
        //        //            filters += (string.IsNullOrEmpty(filters)) ? "where " : " ";
        //        //            filters += string.Join(" or ", wheres);
        //        //            dbArgs.Add("simpleFilter", filter);
        //        //        }

        //        //        countSql += filters;
        //        //        querySql += filters;

        //        //        #endregion

        //        //        #region Sorting

        //        //        if (string.IsNullOrEmpty(sortDataField))
        //        //        {
        //        //            var sortSql = "";

        //        //            foreach (var field in fields.Where(i => i.SortOrder > 0).OrderBy(i => i.SortOrder))
        //        //            {
        //        //                var columnName = $"Field{field.ID}";
        //        //                switch (field.Type)
        //        //                {
        //        //                    case "Number":
        //        //                        sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST(+ [{columnName}] AS bigint)";
        //        //                        break;
        //        //                    case "Date":
        //        //                        sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST(+ [{columnName}] AS date)";
        //        //                        break;
        //        //                    default:
        //        //                        sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"[{columnName}]";
        //        //                        break;
        //        //                }
        //        //            }

        //        //            if (string.IsNullOrEmpty(sortSql))
        //        //            {
        //        //                sortSql = "DisplayValue";
        //        //            }

        //        //            querySql += " ORDER BY " + sortSql;
        //        //        }
        //        //        else
        //        //        {
        //        //            //The user sorted by something else, other than the default SortOrder settings on the FieldTypes.
        //        //            querySql = applySortSuffix(querySql, sortDataField, sortOrder, "DisplayValue", "asc", sortFieldType: sortColumnType(sortDataField, fields));         // Sorting
        //        //        }

        //        //        #endregion

        //        //        #region Paging

        //        //        querySql = applyPagingSuffix(querySql, pagenum, pagesize);

        //        //        #endregion


        //                #endregion

        //                int total = Company.Query<int>(string.Format(sql, "count(1)", ""), dbArgs).First();
        //                var results = Company.Query<dynamic>(string.Format(sql, "*", $"ORDER BY {sortFields} OFFSET({pagenum}) ROWS FETCH NEXT ({pagesize}) ROWS ONLY"), dbArgs);

        //                return new JsonNetResult
        //                {
        //                    Data = new { results, total },
        //                    Formatting = Newtonsoft.Json.Formatting.None
        //                };
        //            }
        //            catch (Exception ex)
        //            {
        //                return jsonNetException(ex);
        //            }
        //        }

        [HttpPost, Route("bytype"), NonNullableParameters]
        public JsonNetResult ByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter, string ownerUsers = "", string ownerGroups = "")
        {
            var objType = "ArtifactType";
            var obj = "Artifact";


            try
            {
                var type = Company.Filter<AssetType>(i => i.Object == objType && i.ObjectID == id).SingleOrDefault();
                if (type == null) throw new Exception("Asset Type Not found");

                var parentIntersectType = Company.Filter<IntersectType>(i => i.Object == objType && i.ObjectID == id && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy).FirstOrDefault();

                var parentSqlColumn = @"null as ParentID, null as Parent, null as ParentUrl,";
                var parentSqlJoin = @"";

                if (parentIntersectType != null)
                {
                    parentSqlColumn = @"PID.ParentID, PID.ParentDisplayValue as Parent, PID.ParentUrl, ";
                    parentSqlJoin = @" cross apply [dbo].[GetArtifactParentByAssetID](A.ID) PID";
                }

                var dcToken = "{0}";
                var djToken = "{1}";

                var sql = $@"
        select	A.ID as AssetID, 
                A.ObjectID as ID,        
                {parentSqlColumn} 
                {dcToken} 
                dbo.GenerateObjectUrl('{obj}', AST.ObjectID, A.ObjectID) as Url 
        from	Asset A 
                inner join AssetType AST on AST.ID = A.AssetTypeID and AST.ID = @id and A.State = 1
                {djToken} 
                {parentSqlJoin} 
        where   not exists (select 1 from AssetWithoutReadPermission RP where RP.ResourceID = " + Company.CurrentResourceID + @" and RP.AssetID = A.ID)";

                #region Field and Other Logic

                var requestParams = Request.Params;
                var dbArgs = new Dapper.DynamicParameters();


                var columns = "";
                var joins = "";
                var wheres = new List<string>();

                var fields = Company.Filter<FieldType>(i => i.AssetTypeID == type.ID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList();

                var relationFieldInfos = getRelationFieldData(objType, id, fields);

                foreach (var f in fields)
                {
                    var name = $"Field{f.ID}";
                    var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");

                    if (f.Type == DataType.Relationship.ToString() || f.Type == DataType.FieldFromRelationship.ToString())
                    {
                        var relationFieldInfo = relationFieldInfos.SingleOrDefault(i => i.FieldTypeID == f.ID);

                        //if (includeIdColumn) columns += $"{name}_T.ID as [{name}ID], ";

                        if (relationFieldInfo != null)
                        {
                            joins += $" left join [Intersect] {name}_T on {name}_T.IntersectTypeID = {f.LookupObjectID} and";
                            joins += relationFieldInfo.IsSubject ?
                                        $" {name}_T.Subject = '{obj}' and {name}_T.SubjectID = A.ObjectID" :
                                        $" {name}_T.Object = '{obj}' and {name}_T.ObjectID = A.ObjectID";

                            if (f.Type == DataType.Relationship.ToString())
                            {
                                var tableName = relationFieldInfo.Object.Replace("Type", "");
                                var typeIDColumnName = relationFieldInfo.Object + "ID";

                                columns += $"{name}_OTD.DisplayValue as [{name}], ";
                                joins += $" left join [{tableName}] {name}_OT on {name}_OT.{typeIDColumnName} = {relationFieldInfo.ObjectID} AND ";
                                joins += $"{name}_OT.ID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID");
                                joins += $" left join Asset {name}_AS on {name}_AS.Object = '{tableName}' and  {name}_AS.ObjectId = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID");
                                joins += $" cross apply [dbo].GetAssetDisplayValueById({name}_AS.ID) {name}_OTD";

                                // If simple filter specified add that criteria to the sql
                                if (!string.IsNullOrEmpty(filter))
                                {
                                    wheres.Add($"{name}_OTD.DisplayValue like @simpleFilter + '%'");
                                }
                            }
                            else if (f.Type == DataType.FieldFromRelationship.ToString())
                            {
                                columns += $"{name}_OT.FormattedValue as [{name}], ";
                                joins += $" left join [Field] {name}_OT on {name}_OT.FieldTypeID = {f.LookupObjectFieldTypeID}";
                                joins += $" and {name}_OT.ObjectType = {name}_T." + (relationFieldInfo.IsSubject ? "Object" : "Subject");
                                joins += $" and {name}_OT.ObjectID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID");

                                // If simple filter specified add that criteria to the sql
                                if (!string.IsNullOrEmpty(filter))
                                {
                                    wheres.Add($"A.[{name}] like @simpleFilter + '%'");
                                }
                            }
                        }
                    }
                    else
                    {
                        joins += $@"
        inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.AssetTypeID = AST.ID 
        left join Field {name}_T on {name}_T.AssetID = A.ID and {name}_T.FieldTypeID = {name}_TT.ID ";

                        //if (includeIdColumn) columns += $"{name}_T.Value as [{name}ID], ";

                        switch (f.Type)
                        {
                            case "Decimal":

                                columns += $@"
        case     
            when {name}_T.Value is not null then cast({name}_T.FormattedValue as decimal(38,6))
            when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as decimal(38,6))
            else null 
        end as [{name}], ";
                                break;
                            case "Number":
                                columns += $@"
        case     
            when {name}_T.Value is not null then cast({name}_T.FormattedValue as bigint)
            when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as bigint)
            else null 
        end as [{name}], ";
                                break;
                            case "DateTime":
                                columns += $@"
        case     
            when {name}_T.Value is not null then cast({name}_T.FormattedValue as datetime)
            when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as datetime)
            else null 
        end as [{name}], ";
                                break;
                            default:
                                columns += $@"
        case 
            when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
            when {name}_T.Value is not null then {name}_T.FormattedValue 
            when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
            else '' 
        end as [{name}], ";
                                break;
                        }

                        // If simple filter specified add that criteria to the sql
                        if (!string.IsNullOrEmpty(filter))
                        {
                            wheres.Add($"A.[{name}] like @simpleFilter + '%'");
                        }
                    }
                }

                dbArgs.Add("id", type.ID);
                //if (requestParams != null)
                //{
                //    foreach (string k in requestParams.Keys)
                //    {
                //        dbArgs.Add(k, (string)requestParams[k]);
                //    }
                //}

                // Ownership Joins
                joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups, "A.ObjectID");

                if (Company.CurrentResourceIsAdmin)
                {
                    columns += "1 P_CanEdit, 1 P_CanDelete,";
                }
                else
                {
                    columns += "S_E.P_CanEdit, S_D.P_CanDelete, ";
                    joins += $@"
            cross apply (
        				select	case 
        							when count(1) > 0 then 1
        							else 0
        						end as P_CanEdit
        				from	SecurityDetail 
        				where	(
        						(IsType = 0 and Object = A.Object and ObjectID = A.ObjectID) OR 
        						(IsType = 1 and Object = A.Type and ObjectID = A.TypeID)
        						) and Claim = 3 and ClaimObject = 1 and ResponsibleObjectID = {Company.CurrentResourceID}
        				) S_E
            cross apply (
        				select	case 
        							when count(1) > 0 then 1
        							else 0
        						end as P_CanDelete
        				from	SecurityDetail 
        				where	(
        						(IsType = 0 and Object = A.Object and ObjectID = A.ObjectID) OR 
        						(IsType = 1 and Object = A.Type and ObjectID = A.TypeID)
        						) and Claim = 3 and ClaimObject = 1 and ResponsibleObjectID = {Company.CurrentResourceID}
        				) S_D ";
                    //columns += $" (select count(1) from securitydetail p_sd_edit where ({innerIdColumn} = p_sd_edit.ObjectID and p_sd_edit.Object = 'Artifact' and p_sd_edit.Claim = 3 and p_sd_edit.ClaimObject = 1 and p_sd_edit.ResponsibleObjectType = 'Resource' and p_sd_edit.ResponsibleObjectID = {Company.CurrentResourceID})) as P_CanEdit, ";
                    //columns += $" (select count(1) from securitydetail p_sd_delete where ({innerIdColumn} = p_sd_delete.ObjectID and p_sd_delete.Object = 'Artifact' and p_sd_delete.Claim = 4 and p_sd_delete.ClaimObject = 1 and p_sd_delete.ResponsibleObjectType = 'Resource' and p_sd_delete.ResponsibleObjectID = {Company.CurrentResourceID})) as P_CanDelete, ";
                }

                var querySql = $@"select * from ({string.Format(sql, columns, joins)}) A";
                var countSql = $@"select count(1) from ({string.Format(sql, columns, joins)}) A";

                #region Filtering

                var filters = applyRelationFilteringExistsRawSuffix(Request, dbArgs, fields, "A.ObjectID");

                countSql += filters;
                querySql += filters;

                filters += applyFilteringSuffixBindRaw(Request, dbArgs, true, fields, "A.ObjectID");  // Filtering

                if (wheres.Count > 0)
                {
                    filters += (string.IsNullOrEmpty(filters)) ? " where " : " ";
                    filters += string.Join(" or ", wheres);
                    dbArgs.Add("simpleFilter", filter);
                }

                countSql += filters;
                querySql += filters;

                #endregion

                #region Sorting

                if (string.IsNullOrEmpty(sortDataField))
                {
                    var sortSql = "";

                    foreach (var field in fields.Where(i => i.SortOrder > 0).OrderBy(i => i.SortOrder))
                    {
                        var columnName = $"Field{field.ID}";
                        switch (field.Type)
                        {
                            case "Number":
                                sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST(+ [{columnName}] AS bigint)";
                                break;
                            case "Date":
                                sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"CAST(+ [{columnName}] AS date)";
                                break;
                            default:
                                sortSql += ((string.IsNullOrEmpty(sortSql)) ? "" : ", ") + $"[{columnName}]";
                                break;
                        }
                    }

                    if (string.IsNullOrEmpty(sortSql))
                    {
                        sortSql = "DisplayValue";
                    }

                    querySql += " ORDER BY " + sortSql;
                }
                else
                {
                    //The user sorted by something else, other than the default SortOrder settings on the FieldTypes.
                    querySql = applySortSuffix(querySql, sortDataField, sortOrder, "DisplayValue", "asc", sortFieldType: sortColumnType(sortDataField, fields));         // Sorting
                }

                #endregion

                #region Paging

                querySql = applyPagingSuffix(querySql, pagenum, pagesize);

                #endregion


                #endregion

                int total = Company.Query<int>(countSql, dbArgs).First();
                var results = Company.Query<dynamic>(querySql, dbArgs);

                return new JsonNetResult
                {
                    Data = new { results, total },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [Route("types")]
        public JsonNetResult GetTypes()
        {
            
            var models = Company.Query<dynamic>(@"
select	    T.ID,
		    IT.SubjectID as ParentID,
		    T.Name,
            AT.Description,
			T.AutoDisplayDescription,
			T.CanOwnFusion,
			T.DisplayFormat,
		    AT.CreatedBy,
			AT.CreatedOn,
			T.UpdatedBy,
		    T.UpdatedOn,
            AT.ID as AssetTypeID,
			K.kount
from	    ArtifactType T
			left join AssetType AT on AT.Object = 'ArtifactType' and AT.ObjectID = T.ID
			left join (SELECT count(a.ArtifactTypeID) kount,a.ArtifactTypeID
							FROM [dbo].[Artifact] a
							inner join ArtifactType b on a.ArtifactTypeID = b.ID
							group by ArtifactTypeID
						) K on K.ArtifactTypeID = t.ID
		    outer apply (
					    select	IT.SubjectID
					    from	IntersectType IT 
							    inner join [Predicate] P on IT.Object = 'ArtifactType' and IT.ObjectID = T.ID and P.ID = IT.PredicateID and P.Type = 3
					    ) IT
order by    T.Name").AsQueryable();

            return new JsonNetResult
            {
                Data = models.ToList().Select(i => new { i.ID, i.Name, i.Description, i.kount, i.ParentID, i.AssetTypeID, expanded = true }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("typeswithstatistics")]
        public JsonNetResult GetTypesWithStatistics()
        {
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.ArtifactTypeStatisticsList),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion
    }
}