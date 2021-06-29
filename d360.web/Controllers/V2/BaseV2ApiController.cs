using d360.core.entities;
using d360.core.enums;
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
            : base(community, company)

        {
            _company = company;
        }

        public int ApiTimeout
        {
            get
            {
                return Company.ApiTimeout;
            }
        }
        protected void getFieldSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> fieldJoins, List<string> fieldColumns, string joinObjectField = "A.[Object]", string joinObjectIdField = "A.[ObjectID]", string assetIdColumn = "A.ID")
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
                {
                    valueColumn = "Value";
                }

                if (f.Type == "FieldFromRelationship")
                {
                    if (!f.LookupObjectFieldTypeID.HasValue || !f.LookupObjectID.HasValue)
                    {
                        return;
                    }

                    var relatedField = _company.GetById<FieldType>((int)f.LookupObjectFieldTypeID);
                    if (relatedField == null)
                    {
                        return;
                    }

                }

                if (f.IsRequired && string.IsNullOrEmpty(f.DefaultValue))
                {
                    joinPrefix = "left";
                    if (!string.IsNullOrEmpty(fieldDataType))
                    {
                        if (fieldDataType == "bit")
                        {
                            fieldColumns.Add($"cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                        }
                        else
                        {
                            fieldColumns.Add($"cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                        }
                    }
                    else
                    {
                        fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
                    }
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
                        select
                            STRING_AGG(ISNULL(FFR_F1.FormattedValue,FFR_F2.FormattedValue),',') as FormattedValue
                        from [Intersect] FFR_I
                        left join Asset FFR_R1 on FFR_R1.[Object] = FFR_I.[Subject] and FFR_R1.ObjectID = FFR_I.SubjectId and FFR_I.[Object] = {joinObjectField} and FFR_I.ObjectID = {joinObjectIdField}
                        left join Field FFR_F1 on FFR_F1.FieldTypeID = {f.LookupObjectFieldTypeID} and FFR_F1.AssetID = FFR_R1.ID
						left join Asset FFR_R2 on FFR_R2.[Object] = FFR_I.[Object] and FFR_R2.ObjectID = FFR_I.ObjectId and FFR_I.[Subject] = {joinObjectField} and FFR_I.SubjectID = {joinObjectIdField}
                        left join Field FFR_F2 on FFR_F2.FieldTypeID = {f.LookupObjectFieldTypeID} and FFR_F2.AssetID = FFR_R2.ID
                        where FFR_I.IntersectTypeID = {f.LookupObjectID} and ISNULL(FFR_F1.FormattedValue, FFR_F2.FormattedValue) is not null
                    ) {tableAlias} ");
                }
                else if (f.Type == "JsonElement")
                {
                    fieldJoins.Add($@"
                        {joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {jsonElementDefinition.FieldTypeID} and {tableAlias}.[ObjectType] = {joinObjectField} and {tableAlias}.[ObjectID] = {joinObjectIdField}
                        {joinPrefix} join FieldJsonProperty FJP{f.ID} on FJP{f.ID}.FieldID = {tableAlias}.ID and FJP{f.ID}.[Path] = @jsonPath{f.ID}
                    ");
                    dbArgs.Add($"@jsonPath{f.ID}", jsonElementDefinition.Path);
                }
                else if (f.Type == "Tag")
                {
                    fieldJoins.Add($@"outer apply(
                        select FormattedValue = STUFF((
                            select '|' + FTag_T.Value from AssetTag FTag_AT
                                inner join Tag FTag_T on FTag_AT.TagID = FTag_T.ID
                                where FTag_AT.AssetID = {assetIdColumn}
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
		                    SELECT  distinct ' | ' + FRelation_P.TextPath
			                    from [Intersect] FRelation_I 
			                    inner Join Asset FRelation_RA on 
			                    FRelation_I.[IntersectTypeID] = {intersectType.ID} AND 
			                    ((({joinObjectField} = FRelation_I.[Subject] and {joinObjectIdField} = FRelation_I.[SubjectID]) AND (FRelation_RA.[Object] = FRelation_I.[Object] and FRelation_RA.[ObjectID] = FRelation_I.[ObjectID])) 
			                    OR ({joinObjectField} = FRelation_I.[Object] and {joinObjectIdField} = FRelation_I.[ObjectID]) AND (FRelation_I.[Subject] = FRelation_RA.[Object] and FRelation_I.[SubjectID] = FRelation_RA.ObjectID))
			                    cross apply GetAssetTextPathById(FRelation_RA.ID, '/') FRelation_P
			                    Where 
			                    FRelation_I.[IntersectTypeID] = {intersectType.ID} AND
			                    ((FRelation_I.[Object] = {joinObjectField} and FRelation_I.ObjectID = {joinObjectIdField}) 
			                    or 
			                    (FRelation_I.[Subject] = {joinObjectField} and FRelation_I.[SubjectID] = {joinObjectIdField}))
		                    for xml path ('')
		                    ), 2, 1, '')
		                    ){tableAlias}(FormattedValue) ");
                }
                else if (f.Type == "Lookup" && Company.LookupFieldHasColorItem(f))
                {
                    string lookupValueJoinCriteria;
                    string displayName;

                    if (f.AllowMultipleValues)
                    {
                        displayName = $@"ADV{tableAlias}.DisplayValue";
                        lookupValueJoinCriteria = $" cross apply STRING_SPLIT({tableAlias}.Value, ',') SPF{tableAlias} inner join Asset AC{tableAlias} on AC{tableAlias}.Object = FT{tableAlias}.LookupObjectType and AC{tableAlias}.ObjectID = SPF{tableAlias}.value ";
                    }
                    else
                    {
                        displayName = $@"{tableAlias}.formattedValue";
                        lookupValueJoinCriteria = $" inner join Asset AC{tableAlias} on AC{tableAlias}.Object = FT{tableAlias}.LookupObjectType and AC{tableAlias}.ObjectID = {tableAlias}.Value ";
                    }

                    string sql = $@"outer apply(
                                select FormattedValue = 
                                (SELECT COALESCE({displayName}, AC{tableAlias}.Code) as name,
                                COALESCE(JSON_VALUE(ACJ{tableAlias}.ColorJSON,'$.Value'), 'transparent') as color
                                from Field {tableAlias}
								inner join FieldType FT{tableAlias} on FT{tableAlias}.ID = {tableAlias}.FieldTypeID
                                {lookupValueJoinCriteria}								
                                cross apply dbo.GetAssetColorJsonByColor(AC{tableAlias}.Color) ACJ{tableAlias}
                                cross apply GetAssetDisplayValueByID(AC{tableAlias}.ID) ADV{tableAlias}
                                where {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = {joinObjectField} and {tableAlias}.[ObjectID] = {joinObjectIdField} FOR JSON PATH),
                                [Value] = 
									(SELECT [Value] from Field {tableAlias}
									 where {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = {joinObjectField} and {tableAlias}.[ObjectID] = {joinObjectIdField})                                
                            ){tableAlias}(FormattedValue, [Value]) ";
                    fieldJoins.Add(sql);
                }
                else
                {
                    fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = {joinObjectField} and {tableAlias}.[ObjectID] = {joinObjectIdField}");
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

        public string isPageSizeAndNumValid(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var parameters = queryParams.ToList();
            long pageSize = 0;
            long pageNum = 0;

            if (parameters.Any(q => q.Key == "_pageSize"))
            {
                var _pageSize = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageSize").Value;
                if (_pageSize.Length > 10)
                {
                    return "Invalid pageSize value provided.";
                }
                if (long.TryParse(_pageSize, out pageSize))
                {
                    if (pageSize > 200000)
                    {
                        return "Invalid pageSize value provided. Number is too large";
                    }
                    if (pageSize <= 0)
                    {
                        return "Invalid pageSize value provided. Value must be greater than 0";
                    }
                }
                else
                {
                    return "Invalid pageSize value provided. Must be a numeric value";
                }
            }

            if (parameters.Any(q => q.Key == "_pageNum"))
            {
                var _pageNum = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageNum").Value;
                if (_pageNum.Length > 10)
                {
                    return "Invalid pageNum value provided.";
                }
                if (long.TryParse(_pageNum, out pageNum))
                {
                    if (pageNum > 100000)
                    {
                        return "Invalid pageNum value provided. Number is too large";
                    }
                    if (pageNum <= 0)
                    {
                        return "Invalid pageNum value provided. Value must be greater than 0";
                    }
                }
                else
                {
                    return "Invalid pageNum value provided. Must be a numeric value.";
                }
            }

            return "";
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

            if (deserializeAsIs)
            {
                return JsonConvert.DeserializeObject<T>(json);
            }

            if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
            {
                return default(T);
            }
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
                    {
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                    else
                    {
                        return default(T);
                    }
                }
                else
                {
                    return default(T);
                }
            }
        }

        #region excel export functions

        protected internal SLDocument createExcelBaseDocument(AssetTypeExportTemplate template, string worksheetName)
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
        protected internal SLDocument GenerateDefaultSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template = null, string worksheetName = "Items")
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

        protected internal SLDocument GenerateGroupedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template, string worksheetName = "Items")
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

                if (previousRow == null)
                {
                    rowSameAsPrevious = false;
                }

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

        protected internal SLDocument GeneratePivotedSpreadsheet(List<FieldType> fields, IEnumerable<dynamic> results, AssetTypeExportTemplate template, string worksheetName = "Items")
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


        protected internal void SetSpreadsheetValueFromField(SLDocument document, int rowIndex, int columnIndex, FieldType field, string value)
        {
            switch ((field.Type ?? "").ToUpper())
            {
                case "DECIMAL":
                    double dVal = 0;
                    if (double.TryParse(value, out dVal))
                    {
                        document.SetCellValue(rowIndex, columnIndex, dVal);
                    }
                    else
                    {
                        document.SetCellValue(rowIndex, columnIndex, value);
                    }
                    break;
                case "NUMBER":
                    int intVal = 0;
                    if (int.TryParse(value, out intVal))
                    {
                        document.SetCellValue(rowIndex, columnIndex, intVal);
                    }
                    else
                    {
                        document.SetCellValue(rowIndex, columnIndex, value);
                    }
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
                    {
                        txt = "'" + txt;
                    }
                    document.SetCellValue(rowIndex, columnIndex, txt);
                    break;
            }
        }

        protected internal void SetRowStylesFromField(ICollection<AssetTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
        {
            if (styles == null || !styles.Any())
            {
                return;
            }

            //check if the styles collection has an entry for this row
            var style = styles.Where(x => x.Row == rowIndex && x.Column == -1 && (x.BackgroundColorValueFieldTypeID > 0 || x.ColorValueFieldTypeID > 0)).FirstOrDefault();

            if (style != null)
            {
                //we have a style based on the value in another column(s)
                document.SetCellStyle(rowIndex, columnIndex, CreateStyle(style, row));
            }
        }

        protected internal void SetColumnStylesFromField(ICollection<AssetTypeExportTemplateStyle> styles, SLDocument document, int rowIndex, int columnIndex, FieldType field, dynamic row)
        {
            if (styles != null && styles.Any())
            {

                //check if the styles collection has an entry for this row
                var style = styles.Where(x => x.Row == -1 && x.Column == columnIndex && (x.BackgroundColorValueFieldTypeID > 0 || x.ColorValueFieldTypeID > 0)).FirstOrDefault();

                if (style != null)
                {
                    var st = CreateStyle(style, row);
                    if (field.Type == "Date")
                    {
                        st.FormatCode = "m/d/yyyy";
                    }

                    //we have a style based on the value in another column(s)
                    document.SetCellStyle(rowIndex, columnIndex, st);
                }
            }
        }

        private void SetColumnCellStyle(SLDocument document, int column, int totalRows, ICollection<AssetTypeExportTemplateStyle> styles)
        {
            if (styles == null)
            {
                return;
            }
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
            if (styles == null)
            {
                return;
            }

            var columnStyle = styles.Where(x => x.Row == row && x.Column == -1).FirstOrDefault();

            if (columnStyle == null)
            {
                return;
            }

            document.SetRowStyle(row, CreateStyle(columnStyle));
        }

        protected void SetCellValue(SLDocument document, int rowIndex, int colIndex, string dataType, object value)
        {
            var valueString = value?.ToString() ?? "";
            switch (dataType.ToUpper())
            {
                case "DECIMAL":
                    double dVal = 0;
                    if (double.TryParse(valueString, out dVal))
                    {
                        document.SetCellValue(rowIndex, colIndex, dVal);
                    }
                    else
                    {
                        document.SetCellValue(rowIndex, colIndex, valueString);
                    }
                    break;
                case "NUMBER":
                    int intVal = 0;
                    if (int.TryParse(valueString, out intVal))
                    {
                        document.SetCellValue(rowIndex, colIndex, intVal);
                    }
                    else
                    {
                        document.SetCellValue(rowIndex, colIndex, valueString);
                    }
                    break;
                case "DATE":
                    if (DateTime.TryParse((value ?? "").ToString(), out DateTime dateVal))
                    {
                        document.SetCellValue(rowIndex, colIndex, dateVal);

                        SLStyle style = document.CreateStyle();
                        style.FormatCode = "m/d/yyyy";
                        document.SetCellStyle(rowIndex, colIndex, style);
                    }
                    break;
                case "COLOR":
                    var data = JsonConvert.DeserializeObject<dynamic>(valueString);
                    if (data != null && data.Name != null)
                    {
                        document.SetCellValue(rowIndex, colIndex, data.Name.ToString());
                    }
                    else
                    {
                        document.SetCellValue(rowIndex, colIndex, "");
                    }
                    break;
                default:
                    var doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(value + "");
                    var txt = HtmlAgilityPack.HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
                    if (txt.StartsWith("="))
                        txt = "'" + txt;
                    document.SetCellValue(rowIndex, colIndex, txt);
                    break;
            }
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
            {
                style.SetFontColor(System.Drawing.Color.FromArgb(columnStyle.Color.Value));
            }

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
            {
                return (string)((row as IDictionary<string, object>)[$"Field{fieldId}"]);
            }
            else
            {
                return (((row as IDictionary<string, object>)[$"{hardCodedName}"]) ?? "").ToString();
            }
        }

        private void SetExcelColumnWidths(SLDocument document, List<FieldType> fields)
        {
            int index = 1;
            foreach (var field in fields)
            {
                try
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
                catch
                {
                    document.SetColumnWidth(index, 10);
                    index++;
                }
            }
        }

        protected internal static void UseTempleteFields(AssetTypeExportTemplate template, List<FieldType> fieldTypes)
        {
            var oldFields = new List<FieldType>(fieldTypes);
            //if include fields is specified only include field ids from list
            if (template.IncludeFieldTypes != null && template.IncludeFieldTypes.Length > 0)
            {
                var fieldNameList = template.IncludeFieldTypes;

                fieldTypes.Clear();

                //done this way to set order of fields in spreadsheet to the order specified in include fields.
                foreach (var fieldName in fieldNameList)
                {
                    var field = oldFields.Find(x => x.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                    if (field != null)
                    {
                        fieldTypes.Add(field);
                    }
                }
            }
        }

        #endregion

        protected internal WorkHttpStatus validateScoreAllocation(string allocationUid, out Guid uid)
        {
            var status = new WorkHttpStatus(System.Net.HttpStatusCode.OK, "", "");

            if (!Guid.TryParse(allocationUid, out uid))
            {
                status.StatusCode = System.Net.HttpStatusCode.BadRequest;
                status.Message = $"allocationUid {allocationUid} is not a correctly formatted identifier.";
            }
            else
            {
                var auid = uid;
                if (!Company.Any<core.entities.Metric.MetricAllocation>(i => i.Uid == auid))
                {
                    status.StatusCode = System.Net.HttpStatusCode.NotFound;
                    status.Message = $"Allocation identifier with value {uid} does not correspond to a valid allocation.";
                }
            }

            return status;
        }

        protected internal WorkHttpStatus validateAsset(string assetUid, Permission permission, out Guid uid)
        {
            var status = new WorkHttpStatus(System.Net.HttpStatusCode.OK, "", "");

            if (!Guid.TryParse(assetUid, out uid))
            {
                status.StatusCode = System.Net.HttpStatusCode.BadRequest;
                status.Message = $"assetUid {assetUid} is not a correctly formatted identifier.";
            }
            else
            {
                var auid = uid;
                var asset = Company.Filter<Asset>(i => i.uid == auid).SingleOrDefault();

                if (asset == null)
                {
                    status.StatusCode = System.Net.HttpStatusCode.BadRequest;
                    status.Message = $"Asset identifier with value {uid} does not correspond to a valid asset.";
                }
                else
                {
                    var canRead = Company.HasAssetPermission(asset.ID, permission);
                    if (!canRead)
                    {
                        status.StatusCode = System.Net.HttpStatusCode.Forbidden;
                        status.Message = $"You do not have permissions to view score history on this asset.";
                    }
                }
            }

            return status;
        }
    }
}
