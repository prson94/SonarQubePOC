using d360.core.entities;
using d360.model;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace d360.web.Controllers.V2
{
    [ValidateModel]
    [ValidateCompanyState]
    public class BaseV2ApiController : BaseApiController
    {
        ICompanyContext _company;
        public BaseV2ApiController(ICommunityContext community, ICompanyContext company)
            : base(community,company)

        {
            _company = company;
        }

        public void getFieldSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> fieldJoins, List<string> fieldColumns)
        {
            fieldTypes.ForEach(f =>
            {
                var defaultVal = f.DefaultFormattedValue;
                var joinPrefix = "left";
                var tableAlias = $"F{f.ID}";
                var columnName = f.Name;
                var valueColumn = "FormattedValue";
                var fieldDataType = getFieldDataType(f);

                FieldTypeDefinition_JsonElement jsonElementDefinition = null;

                if (f.Type == "JsonElement")
                {
                    jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(f.Definition);
                }

                if (f.Type == "Link")
                    valueColumn = "Value";

                if (f.Type == "FieldFromRelationship")
                {
                    if (!f.LookupObjectFieldTypeID.HasValue || !f.LookupObjectID.HasValue)
                        return;

                    var relatedField = _company.GetById<FieldType>((int)f.LookupObjectFieldTypeID);
                    if (relatedField == null)
                        return;

                }

                if (f.IsRequired && string.IsNullOrEmpty(f.DefaultValue))
                {
                    joinPrefix = "left";
                    if (!string.IsNullOrEmpty(fieldDataType))
                    {
                        if (fieldDataType == "bit")
                            fieldColumns.Add($"cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                        else
                            fieldColumns.Add($"cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                    }
                    else
                        fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
                }
                else
                {
                    if (!string.IsNullOrEmpty(f.DefaultValue))
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                        {
                            if (fieldDataType == "bit")
                                fieldColumns.Add($"coalesce(cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                            else
                                fieldColumns.Add($"coalesce(cast({tableAlias}.{valueColumn} as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                        }
                        else
                            fieldColumns.Add($"coalesce({tableAlias}.{valueColumn}, @defaultValue{tableAlias}) as [{columnName}]");

                        dbArgs.Add($"@defaultValue{tableAlias}", defaultVal);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                        {
                            if (fieldDataType == "bit")
                                fieldColumns.Add($"cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                            else
                                fieldColumns.Add($"cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                        }
                        else if (f.Type == "JsonElement")
                        {
                            fieldColumns.Add($"try_cast(FJP{f.ID}.[Value] as {jsonElementDefinition.DataType}) as [{columnName}]");
                        }
                        else
                        {
                            fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
                        }
                    }
                }

                if (f.Type == "FieldFromRelationship")
                {
                    fieldJoins.Add($@"outer apply (
                        select top 1 
                            F.[Value], 
                            F.FormattedValue 
                        from [Intersect] I
                        inner join Asset R on R.[Object] = I.[Object] and R.ObjectID = I.ObjectID
                        inner join Field F on F.FieldTypeID = {f.LookupObjectFieldTypeID} and F.AssetID = R.ID
                        where I.[Subject] = A.Object and I.SubjectID = A.ObjectID and I.IntersectTypeID = {f.LookupObjectID}
                    ) {tableAlias} ");
                }
                else if (f.Type == "JsonElement")
                {
                    fieldJoins.Add($@"
                        {joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {jsonElementDefinition.FieldTypeID} and {tableAlias}.[ObjectType] = A.[Object] and {tableAlias}.[ObjectID] = A.[ObjectID]
                        {joinPrefix} join FieldJsonProperty FJP{f.ID} on FJP{f.ID}.FieldID = {tableAlias}.ID and FJP{f.ID}.[Path] = @jsonPath{f.ID}
                    ");
                    dbArgs.Add($"@jsonPath{f.ID}", jsonElementDefinition.Path);
                }
                else if (f.Type == "Tag")
                {
                    fieldJoins.Add($@"outer apply(
                        select FormattedValue = STUFF((
                            select '|' + T.Value from AssetTag AT
                                inner join Tag T on AT.TagID = T.ID
                                where AT.AssetID = A.ID
                            for xml path ('')), 1, 1, '')
                         ){tableAlias}(FormattedValue) ");
                }
                else if (f.Type == "Relationship")
                {
                    if (!f.LookupObjectID.HasValue)
                    {
                        throw new Exception("Invalid Relationship field encountered no relationship type to lookup found in definition.");
                    }
                    var intersectType = Company.GetById<IntersectType>(f.LookupObjectID.Value);

                    if (intersectType == null)
                    {
                        throw new Exception("Invalid Relationship field encountered invalid or deleted relationship type encountered.");
                    }

                    fieldJoins.Add($@"
                    outer apply (
		                    SELECT hello = Stuff((
		                    SELECT  distinct ' | ' + p.TextPath
			                    from [Intersect] I 
			                    inner Join Asset RA on 
			                    I.[IntersectTypeID] = {intersectType.ID} AND 
			                    (((A.[Object] = I.[Subject] and A.[ObjectID] = I.[SubjectID]) AND (RA.[Object] = I.[Object] and RA.[ObjectID] = I.[ObjectID])) 
			                    OR (A.[Object] = I.[Object] and A.[ObjectID] = I.[ObjectID]) AND (I.[Subject] = RA.[Object] and I.[SubjectID] = RA.ObjectID))
			                    cross apply GetAssetTextPathById(RA.ID, '/') P
			                    Where 
			                    I.[IntersectTypeID] = {intersectType.ID} AND
			                    ((I.[Object] = A.[Object] and I.ObjectID = A.ObjectID) 
			                    or 
			                    (I.[Subject] = A.[Object] and I.[SubjectID] = A.ObjectID))
		                    for xml path ('')
		                    ), 2, 1, '')
		                    ){tableAlias}(FormattedValue) ");
                }
                else
                {
                    fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = A.[Object] and {tableAlias}.[ObjectID] = A.[ObjectID]");
                }
            });
        }

        private string getFieldDataType(FieldType field)
        {
            switch (field.Type)
            {
                case "Date":
                case "DateTime":
                    return "datetime";
                case "Number":
                    return "bigint";
                case "Decimal":
                    return "float";
                case "Boolean":
                    return "bit";
                default:
                    return "";
            }
        }

        public bool isPageSizeAndNumValid(int _pageSize, int _pageNum)
        {
            if (_pageSize > 200000) return false;

            if (_pageNum > 10000) return false;

            return true;
        }

        protected async Task<T> readRequestJsonContent<T>(HttpRequestMessage request, bool deserializeAsIs = false)
        {
            string json = "";

            if (request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await request.Content.ReadAsMultipartAsync(streamProvider);

                json = await streamProvider.Contents.Single().ReadAsStringAsync();
            }
            else
            {
                json = await request.Content.ReadAsStringAsync();
            }

            if (deserializeAsIs) return JsonConvert.DeserializeObject<T>(json);

            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                return default(T);
            else
            {
                if ((json.StartsWith("{") && json.EndsWith("}")) || //For object
                        (json.StartsWith("[") && json.EndsWith("]"))) //For array
                {
                    bool isValid = false;
                    try
                    {
                        var obj = JToken.Parse(json);
                        isValid = true;
                        obj = null;
                    }
                    catch
                    {
                        isValid = false;
                    }

                    if (isValid)
                        return JsonConvert.DeserializeObject<T>(json);
                    else
                        return default(T);
                }
                else
                {
                    return default(T);
                }
            }
        }

        #region excel export functions

        internal SLDocument createExcelBaseDocument(AssetTypeExportTemplate template, string worksheetName)
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
                document.RenameWorksheet(SLDocument.DefaultFirstSheetName, worksheetName);

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
        internal SLDocument GenerateDefaultSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template = null, string worksheetName = "Items")
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
                    var val = getRowFieldValue(row, field.ID, field.Name);
                    SetSpreadsheetValueFromField(document, rowNumber, index, field, val);
                    SetColumnStylesFromField(styles, document, rowNumber, index, field, row);
                    index++;
                }
            }

            SetExcelColumnWidths(document, fields);

            return document;
        }

        internal SLDocument GenerateGroupedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template, string worksheetName = "Items")
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
                    var val = getRowFieldValue(row, field.ID, field.Name);
                    var previousVal = previousRow != null ? getRowFieldValue(previousRow, field.ID, field.Name) : "";

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

        internal SLDocument GeneratePivotedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template, string worksheetName = "Items")
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
                    concatenatedValue += getRowFieldValue(row, field.ID, field.Name);
                }
                if (!uniques.Contains(concatenatedValue))
                {
                    index = 1;
                    columnNumber++;

                    uniques.Add(concatenatedValue);
                    foreach (var field in fields)
                    {
                        var val = getRowFieldValue(row, field.ID, field.Name);
                        SetSpreadsheetValueFromField(document, index, columnNumber, field, val);
                        SetRowStylesFromField(styles, document, index, columnNumber, field, row);

                        index++;
                    }
                }
            }

            for (int i = 1; i < index; i++)
            {
                SetRowStyles(document, i, styles);
            }

            for (int i = 1; i < columnNumber; i++)
            {
                SetColumnCellStyle(document, i, index - 1, styles);
            }

            document.AutoFitColumn(1, columnNumber);

            return document;
        }


        internal void SetSpreadsheetValueFromField(SLDocument document, int rowIndex, int columnIndex, FieldType field, string value)
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
                    doc.LoadHtml(value + "");
                    var txt = HtmlAgilityPack.HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
                    if (txt.StartsWith("="))
                        txt = "'" + txt;
                    document.SetCellValue(rowIndex, columnIndex, txt);
                    break;
            }
        }

        internal void SetRowStylesFromField(ICollection<AssetTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
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

        internal void SetColumnStylesFromField(ICollection<AssetTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
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

        private void SetColumnCellStyle(SLDocument document, int column, int totalRows, ICollection<AssetTypeExportTemplateStyle> styles)
        {
            if (styles == null) return;
            //style for the whole column
            var columnStyle = styles.Where(x => x.Row == -1 && x.Column == column).FirstOrDefault();

            if (columnStyle != null)
            {
                document.SetCellStyle(1, column, totalRows, column, CreateStyle(columnStyle));
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
                document.SetCellStyle(1, column, CreateStyle(columnheaderStyle));
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

            if (columnStyle.ColorValueFieldTypeID > 0 && row != null)
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
        private string getRowFieldValue(dynamic row, int fieldId, string hardCodedName = null)
        {
            if (fieldId > 0 && string.IsNullOrEmpty(hardCodedName))
                return (string)((row as IDictionary<string, object>)[$"Field{fieldId}"]);
            else
                return (((row as IDictionary<string, object>)[$"{hardCodedName}"]) ?? "").ToString();
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
        #endregion

    }
}
