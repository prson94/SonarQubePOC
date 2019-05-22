using d360.core;
using d360.core.entities;
using d360.core.enums;
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
using System.Threading.Tasks;
using System.Web.Mvc;
using Dapper;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/artifacts"), Authorize, AiHandleError]
    public class ArtifactsController : BaseController
    {
        #region DI

        public ArtifactsController(ICommunityContext community, ICompanyContext company)
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
                DataType.OwnershipLookup.ToString(),
                DataType.FieldFromRelationship.ToString()
            };
            var fields = getFieldTypesByObjectType("ArtifactType", id, listableOnly).Where(i => !typesToAvoid.Contains(i.Type)).ToList();

            getDynamicFieldJoinStatements(id, "Artifact", out joins, out columns, true, false, listableOnly, fields, "A.ObjectID");

            var dbArgs = new Dapper.DynamicParameters();

            var assetType = Company.AssetTypes.FirstOrDefault(a => a.Object == "ArtifactType" && a.ObjectID == id);


            dbArgs.Add("typeId", assetType.ID);

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
                            dbo.GenerateAssetUrl(IA.ID) as ParentUrl
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = A.Object and I.ObjectID = A.ObjectID and IA.Object = 'Artifact' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            left join dbo.GetAssetDisplayValue() ID on ID.ID = IA.ID
				    ) P";
            }


            #region Sql

            var sql = $@"
select	distinct 
        A.ObjectID as ID,
        {parentSqlColumn}
        {columns}
        A.ID as AssetID,dbo.GenerateAssetUrl(A.ID) as Url
from	AssetDetail A 
        {parentSqlJoin} 
        {joins} 
where   A.Type = 'ArtifactType' 
        and A.AssetTypeID = @typeId 
        and A.[State] = 1 ";

            #endregion

            //if simple filter specified add that citeria to the sql
            if (!string.IsNullOrEmpty(filter))
            {
                var fixedColumns = new List<string>();

                if (parentIntersectType != null)
                    fixedColumns.Add("P.DisplayValue"); //Owner/Parent

                sql = $"{sql} and {addDynamicFieldSimpleFilter(fixedColumns.ToArray(), "Artifact", id, filter, dbArgs)}";
            }
            
            sql = $"select * from ({sql}) A ";
            
            var filterSql = applyFilteringSuffixBindRaw(Request, dbArgs, fields:fields, applyHiddenFilters:true,fromArtifact:true);

            sql = sql + filterSql;

            if (string.IsNullOrEmpty(filterSql))
            {
                sql += $" where not exists(select 1 from AssetTypesUserCantRead({Company.CurrentResourceID})u where u.AssetTypeID = @typeId) and not exists(select 1 from AssetsByTypeUserCantRead({Company.CurrentResourceID}, @typeId) u where u.AssetID = A.AssetID) ";                
            }
            else
            {
                sql += $" and not exists(select 1 from AssetTypesUserCantRead({Company.CurrentResourceID})u where u.AssetTypeID = @typeId) and not exists(select 1 from AssetsByTypeUserCantRead({Company.CurrentResourceID}, @typeId) u where u.AssetID = A.AssetID) ";
            }

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

            if (Company.TypeHasParent(SystemObjects.ArtifactType, assetType.ObjectID))
                fields.Insert(0, new FieldType { Type = "string", Name = "Parent", FriendlyName = "Parent" });

            fields.Add(new FieldType { Type = "Number", Name = "AssetID", FriendlyName = "Asset ID" });
            fields.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "Url" });

            
            var results = Company.Query<dynamic>(sql + " OPTION (RECOMPILE)", dbArgs);            
            var document = GenerateDefaultSpreadsheet(fields, results);
            
            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Filtered {assetType.Name} List for {DateTime.Now.ToShortDateString()}.xlsx");
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
                        

            var template = Company.AssetTypeExportTemplates.Where(x => x.ID == templateId).FirstOrDefault();

            if(template == null)
            {
                throw new Exception("INVALID TEMPLATE ID SPECIFIED.");
            }
                        
            var fields = getFieldTypesByObjectType("ArtifactType", artifactTypeId, listableOnly).Where(i => !typesToAvoid.Contains(i.Type)).ToList();

            getDynamicFieldJoinStatements(artifactTypeId, "Artifact", out joins, out columns, true, false, listableOnly, fields, "A.ObjectID");

            joins = addOwnershipJoinCriteria(joins, ownerUsers, ownerGroups);

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
                            dbo.GenerateAssetUrl(IA.ID) as ParentUrl
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = 'Artifact' and I.ObjectID = A.ObjectID and IA.Object = 'Artifact' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            left join dbo.GetAssetDisplayValue() ID on ID.ID = IA.ID
				    ) P";
            }

            #region Sql

            var sql = $@"
select	A.ObjectID as ID,
        {parentSqlColumn}
        {columns}
		dbo.GenerateAssetUrl(A.ID) as Url
from	AssetDetail A 
        {parentSqlJoin}
        {joins} 
where   A.Type = 'ArtifactType' 
        and A.TypeID = @id 
        and A.[State] = 1 
        and not exists(select 1 from AssetTypesUserCantRead({ Company.CurrentResourceID})u where u.AssetTypeID = A.AssetTypeID) and not exists(select 1 from AssetsByTypeUserCantRead({ Company.CurrentResourceID}, A.AssetTypeID) u where u.AssetID = A.ID) ";        


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
            
            sql = string.Format(@"select * from ({0}) A", sql);

            sql = applyFilteringSuffixBind(sql, Request, dbArgs, fields: oldFields,fromArtifact:true);
                        
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

                if (!string.IsNullOrEmpty(sortSql))
                    sql += " ORDER BY " + sortSql;
            }
            else
            {
                //The user sorted by something else, other than the default SortOrder settings on the FieldTypes.                
                sql = applySortSuffix(sql, sortDataField, sortOrder, sortFieldType: sortColumnType(sortDataField, oldFields));
            }


            var results = Company.Query<dynamic>(sql, dbArgs);
                                    
            SLDocument document = null;
            if (template.IncludeParent)
            {
                var assetType = Company.AssetTypes.FirstOrDefault(a => a.Object == "ArtifactType" && a.ObjectID == artifactTypeId);
                if (Company.TypeHasParent(SystemObjects.ArtifactType, assetType.ObjectID)) fields.Insert(0, new FieldType { Type = "string", Name = "Parent", FriendlyName = "Parent" });
            }

            if (template.IncludeUrl) fields.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "Url" });
                        
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

        private SLDocument GenerateDefaultSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template = null, string worksheetName = "Items")
        {
            ICollection<AssetTypeExportTemplateStyle> styles = null;
            if (template != null)
            {
                styles = template.AssetTypeExportTemplateStyles;
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

        private SLDocument GenerateGroupedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template, string worksheetName = "Items")
        {
            var styles = template.AssetTypeExportTemplateStyles;

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

        private SLDocument GeneratePivotedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template, string worksheetName = "Items")
        {
            var styles = template.AssetTypeExportTemplateStyles;

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

            for (int i = 1; i < columnNumber; i++)
            {
                SetColumnCellStyle(document, i,index-1, styles);
            }

            document.AutoFitColumn(1, columnNumber);

            return document;
        }

        private void SetRowStylesFromField(ICollection<AssetTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
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

        private void SetColumnStylesFromField(ICollection<AssetTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
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

        private SLDocument createExcelBaseDocument(AssetTypeExportTemplate template, string worksheetName)
        {
            SLDocument document = null;

            if (template == null)
            {
                template = new AssetTypeExportTemplate();
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

        private void SetColumnCellStyle(SLDocument document, int column,int totalRows, ICollection<AssetTypeExportTemplateStyle> styles)
        {
            if (styles == null) return;
            //style for the whole column
            var columnStyle = styles.Where(x => x.Row == -1 && x.Column == column).FirstOrDefault();

            if (columnStyle != null)
            {
                document.SetCellStyle(1,column,totalRows,column, CreateStyle(columnStyle));
            }
        }
        private void SetColumnStyles(SLDocument document, int column, ICollection<AssetTypeExportTemplateStyle> styles)
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

        private void SetRowStyles(SLDocument document, int row, ICollection<AssetTypeExportTemplateStyle> styles)
        {
            if (styles == null) return;

            var columnStyle = styles.Where(x => x.Row == row && x.Column == -1).FirstOrDefault();

            if (columnStyle == null) return;

            document.SetRowStyle(row, CreateStyle(columnStyle));
        }

        private SLStyle CreateStyle(AssetTypeExportTemplateStyle columnStyle, dynamic row = null)
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
                    var txt = HtmlAgilityPack.HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
                    if (txt.StartsWith("="))
                        txt = "'" + txt;
                    document.SetCellValue(rowIndex, columnIndex, txt);
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
        dbo.GenerateAssetUrl(P.ID) as ParentUrl,
        {0}
        dbo.GenerateAssetUrl(O.ID) as Url
from	AssetDetail O
        {1} 
        inner join [PredicateIntersect] PI on PI.Subject = 'Artifact' and PI.Object = O.Object and PI.SubjectID = @p and PI.ObjectID = O.ObjectID and PI.PredicateType = 3 
        inner join AssetDetail P on P.Object = PI.Subject and P.ObjectID = PI.SubjectID 
where   O.Type = 'ArtifactType' and O.TypeID = @id and O.[State] = 1 
        and O.ID not in (" + GetNoReadSqlStatement() + ")";
            var model = processDynamicResults(
                sql, Request,
                "ArtifactType", childArtifactTypeID,
                true,
                sortDataField, sortOrder, pagenum, pagesize,
                new string[] { "P.DisplayValue" },
                filter, extraParams: d, includeIdColumn: false, idColumn: "O.ID", innerIdColumn:"O.ObjectID");
            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("artifactsbytype"), NonNullableParameters]
        public async Task<JsonNetResult> ArtifactsByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter)
        {
            return await ByType(id, sortDataField, sortOrder, pagenum, pagesize, filter);
        }

        [HttpPost, Route("bytype"), NonNullableParameters]
        public async Task<JsonNetResult> ByType(int id, string sortDataField, string sortOrder, int pagenum, int pagesize, string filter)
        {
            try
            {
                var assetType = Company.Filter<AssetType>(i => i.Object == "ArtifactType" && i.ObjectID == id).SingleOrDefault();
                
                if (assetType == null)
                {
                    return new JsonNetResult
                    {
                        Data = new { message = "Asset Type not found" },
                        Formatting = Newtonsoft.Json.Formatting.None
                    };
                }
                var filters = GetFilterValuesFromRequest(Request,true);
                var results = await Company.GetPivotVersionDynamicAssets(assetType, filters, pagenum, pagesize, false, sortDataField, sortOrder, filter);

                return new JsonNetResult
                {
                    Data = new { results = results.Results, total = results.Count },
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
select	    AT.ObjectID as ID,
		    IT.SubjectID as ParentID,
		    AT.Name,
            AT.Description,
			AT.AutoDisplayDescription,
			AT.CanOwnFusion,
			AT.DisplayFormat,
		    AT.CreatedBy,
			AT.CreatedOn,
			AT.UpdatedBy,
		    AT.UpdatedOn,
            AT.ID as AssetTypeID,
			K.kount
from	   
			 AssetType AT
			left join (SELECT count(a.AssetTypeID) kount,a.AssetTypeID
							FROM [dbo].[Asset] a
							inner join AssetType b on a.AssetTypeID = b.ID
							group by AssetTypeID
						) K on K.AssetTypeID = AT.ID 
		    outer apply (
					    select	IT.SubjectID
					    from	IntersectType IT 
							    inner join [Predicate] P on IT.Object = 'ArtifactType' and IT.ObjectID = AT.ObjectID and P.ID = IT.PredicateID and P.Type = 3
					    ) IT
			where  AT.Object = 'ArtifactType'
order by    AT.Name").AsQueryable();

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