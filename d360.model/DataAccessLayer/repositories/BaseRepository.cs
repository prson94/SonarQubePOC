using d360.core.entities;
using Dapper;
using Newtonsoft.Json;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
{
    public abstract class BaseRepository
    {
        ICompanyContext CompanyContext;
        public BaseRepository(ICompanyContext ctx)
        {
            this.CompanyContext = ctx;
        }
        protected void getFieldSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> fieldJoins, List<string> fieldColumns, string objectSql = "A.[Object]", string objectIdSql= "A.[ObjectId]")
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

                    var relatedField = CompanyContext.GetById<FieldType>((int)f.LookupObjectFieldTypeID);
                    if (relatedField == null)
                        return;

                }

                if (f.IsRequired && string.IsNullOrEmpty(f.DefaultValue))
                {
                    joinPrefix = "left";
                    if (!string.IsNullOrEmpty(fieldDataType))
                    {
                        if (fieldDataType == "bit")
                            fieldColumns.Add($"try_cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                        else
                            fieldColumns.Add($"try_cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
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
                                fieldColumns.Add($"coalesce(try_cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                            else
                                fieldColumns.Add($"coalesce(try_cast({tableAlias}.{valueColumn} as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
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
                                fieldColumns.Add($"try_cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                            else
                                fieldColumns.Add($"try_cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                        }
                        else if (f.Type == "JsonElement")
                        {
                            if (jsonElementDefinition.DataType == "decimal")
                            {
                                jsonElementDefinition.DataType = "float";
                            }
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
                            STRING_AGG(ISNULL(F1.FormattedValue,F2.FormattedValue),',') as FormattedValue
                        from [Intersect] I
                        left join Asset R1 on R1.[Object] = I.[Subject] and R1.ObjectID = I.SubjectId and I.[Object] = A.Object and I.ObjectID = A.ObjectID
                        left join Field F1 on F1.FieldTypeID = {f.LookupObjectFieldTypeID} and F1.AssetID = R1.ID
						left join Asset R2 on R2.[Object] = I.[Object] and R2.ObjectID = I.ObjectId and I.[Subject] = A.Object and I.SubjectID = A.ObjectID
                        left join Field F2 on F2.FieldTypeID = {f.LookupObjectFieldTypeID} and F2.AssetID = R2.ID
                        where I.IntersectTypeID = {f.LookupObjectID} and ISNULL(F1.FormattedValue,F2.FormattedValue) is not null
                    ) {tableAlias}");
                }
                else if (f.Type == "Relationship")
                {
                    fieldJoins.Add($@"outer apply (
                        select 
                            STRING_AGG(ISNULL(AD1.DisplayValue,AD2.DisplayValue),',') as FormattedValue
                        from [Intersect] I
                        left join Asset R1 on R1.[Object] = I.[Subject] and R1.ObjectID = I.SubjectId and I.[Object] = A.Object and I.ObjectID = A.ObjectID
                        left join AssetDetail AD1 on AD1.Object = R1.Object and AD1.ObjectID = R1.ObjectId
						left join Asset R2 on R2.[Object] = I.[Object] and R2.ObjectID = I.ObjectId and I.[Subject] = A.Object and I.SubjectID = A.ObjectID
                        left join AssetDetail AD2 on AD2.Object = R2.Object and AD2.ObjectID = R2.ObjectId
                        where I.IntersectTypeID = {f.LookupObjectID} and ISNULL(AD1.DisplayValue,AD2.DisplayValue) is not null
                    ) {tableAlias}");
                }
                else if (f.Type == "RefListRelationship")
                {
                    fieldJoins.Add($@"outer apply (
                        select
                           STRING_AGG(ISNULL(R1.SubjectName,R2.ObjectName),',') as FormattedValue
                        from [Intersect] I
                        left join [IntersectDetail] R1 on R1.[Object] = I.[Subject] and R1.ObjectID = I.SubjectId and I.[Object] = A.Object and I.ObjectID = A.ObjectID
						left join [IntersectDetail] R2 on R2.[Object] = I.[Object] and R2.ObjectID = I.ObjectId and I.[Subject] = A.Object and I.SubjectID = A.ObjectID
                        where I.IntersectTypeID = {f.LookupObjectID} and ISNULL(R1.SubjectName,R2.ObjectName) is not null
                    ) {tableAlias}");
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
                else
                {
                    fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = {objectSql} and {tableAlias}.[ObjectID] = {objectIdSql}");
                }
            });
        }
        protected void getQueryParamsSql(AssetsApiViewModel model, AssetType assetType, List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> whereStatements, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (queryParams != null)
            {

                var orderBySql = "";
                var orderDirection = "";
                var offsetSql = "";
                var pageNum = -1;
                var pageSize = 200;

                if (queryParams.Any(x => x.Key == "_direction"))
                {
                    string[] allowedDirections = new string[] { "asc", "desc" };
                    var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value;

                    orderDirection = allowedDirections.Contains(order.Trim().ToLower()) ? order : "";
                }

                //add base sort if none is specified
                if (!queryParams.Any(p => p.Key == "_order"))
                {
                    orderBySql = $"order by A.ID {orderDirection}";
                }

                queryParams
                    .ToList()
                    .ForEach(q =>
                    {
                        var key = q.Key.ToLower();

                        if (key.StartsWith("_"))
                        {
                            if (key == "_order")
                            {
                                if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "name")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"FA.Name {orderDirection} ";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "sourceid")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"FA.SourceID {orderDirection} ";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "textpath")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"FA.TextPath {orderDirection} ";
                                }
                                else if (assetType.Object == "ReferenceItemType" && q.Value.ToLower() == "code")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"A.Code {orderDirection} ";
                                }
                                else
                                {
                                    var field = fieldTypes.FirstOrDefault(f => f.Name.ToLower() == q.Value.ToLower());
                                    var valueColumn = "FormattedValue";
                                    var fieldDataType = getFieldDataType(field);

                                    if (field == null)
                                    {
                                        var orderBy = $"A.ID {orderDirection}";
                                        switch (q.Value.Trim().ToLower())
                                        {
                                            case "createdon":
                                                orderBy = $"A.CreatedOn {(string.IsNullOrEmpty(orderDirection) ? "DESC" : orderDirection)}";
                                                break;
                                            case "updatedon":
                                                orderBy = $"A.UpdatedOn {(string.IsNullOrEmpty(orderDirection) ? "DESC" : orderDirection)}";
                                                break;
                                            default:
                                                orderBy = $"A.ID {orderDirection}";
                                                break;
                                        }

                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + orderBy;
                                        return;
                                    }

                                    if (field.Type == "Link") valueColumn = "Value";

                                    if (!string.IsNullOrEmpty(fieldDataType))
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"cast(F{field.ID}.{valueColumn} as {fieldDataType}) {orderDirection}";
                                    else
                                    {
                                        if (field.Type == "JsonElement")
                                        {
                                            FieldTypeDefinition_JsonElement jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(field.Definition);

                                            if (jsonElementDefinition.DataType == "decimal")
                                            {
                                                jsonElementDefinition.DataType = "float";
                                            }

                                            fieldDataType = jsonElementDefinition.DataType;

                                            orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"try_cast(FJP{field.ID}.Value as {fieldDataType}) {orderDirection}";
                                        }
                                        else
                                        {
                                            orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"F{field.ID}.{valueColumn} {orderDirection}";
                                        }
                                    }
                                }
                            }
                            else if (key == "_pagenum")
                            {
                                if (int.TryParse(q.Value, out pageNum))
                                {
                                    if (pageNum < 1) pageNum = 1;
                                }
                            }
                            else if (key == "_pagesize")
                            {
                                if (int.TryParse(q.Value, out pageSize))
                                {
                                    if (pageSize < 1) pageSize = 1;
                                }
                            }
                        }
                        else
                        {
                            if (assetType.Object == "FusionAttributeType" && key == "name")
                            {
                                whereStatements.Add($"FA.[Name] = @faName");
                                dbArgs.Add($"@faName", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "sourceid")
                            {
                                whereStatements.Add($"FA.[SourceID] = @sourceID");
                                dbArgs.Add($"@sourceID", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "textpath")
                            {
                                whereStatements.Add($"FA.[TextPath] = @textpath");
                                dbArgs.Add($"@textpath", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "parentuid")
                            {
                                if ((CompanyContext.Database.Connection.QueryFirstOrDefault<int>("select ISNULL(parentId,0) from fusionattributetype where id = @id", new { id = assetType.ObjectID })) > 0)
                                {
                                    whereStatements.Add($"ATP.[uid] = @parentuid");
                                    dbArgs.Add($"@parentuid", q.Value);
                                }

                            }
                            else if (assetType.Object == "ReferenceItemType" && key == "code")
                            {
                                whereStatements.Add($"RI.[Code] = @code");
                                dbArgs.Add($"@code", q.Value);
                            }
                            else
                            {
                                var field = fieldTypes.Find(f => f.Name.ToLower() == key);

                                if (field != null)
                                {
                                    if (field.Type == "JsonElement")
                                    {
                                        whereStatements.Add($"FJP{field.ID}.Value = @field{field.ID}");
                                        dbArgs.Add($"@field{field.ID}", q.Value);
                                    }
                                    else
                                    {
                                        whereStatements.Add($"F{field.ID}.FormattedValue = @field{field.ID}");
                                        dbArgs.Add($"@field{field.ID}", q.Value);
                                    }
                                }
                            }
                        }
                    });


                bool useTypeLevelDefaultSorts = false;
                var defSorts = queryParams.FirstOrDefault(x => x.Key.ToLower() == "usetypeleveldefaultsorts");
                if (!string.IsNullOrEmpty(defSorts.Key))
                    bool.TryParse(defSorts.Value, out useTypeLevelDefaultSorts);

                if (useTypeLevelDefaultSorts)
                {
                    var orderFields = fieldTypes.Where(x => x.SortOrder > 0 && x.IsListable == true)
                        .OrderBy(x => x.SortOrder)
                        .GroupBy(x => x.SortOrder)
                        .ToList();

                    if (orderFields.Count == 0)
                        orderBySql = "order by A.ID ";
                    else
                    {
                        List<string> sortStatements = new List<string>();
                        orderFields.ForEach(ft =>
                        {
                            if (ft.Count() == 1)
                            {
                                sortStatements.Add(getFieldDataTypeWrapper(ft.FirstOrDefault()));
                            }
                            else
                            {
                                //If same sort number order by field type Name
                                var fts = ft.ToList().OrderBy(x => x.Name).ToList();
                                fts.ForEach(_ft =>
                                {
                                    sortStatements.Add(getFieldDataTypeWrapper(_ft));
                                });
                            }
                        });

                        orderBySql = "order by " + string.Join(", ", sortStatements);
                    }
                }

                pagingSql.Add(orderBySql);

                if (pageSize > 0 || pageNum > 0)
                {
                    if (pageSize < 1) pageSize = 1;
                    if (pageNum < 1) pageNum = 1;

                    model.pageSize = pageSize;
                    model.pageNum = pageNum;

                    offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                    pagingSql.Add(offsetSql);
                }

            }
        }

        protected string getFieldDataTypeWrapper(FieldType ft)
        {
            var fieldType = getFieldDataType(ft);

            if (!string.IsNullOrEmpty(fieldType))
            {
                string val = $"F{ft.ID}.FormattedValue";

                if (!string.IsNullOrEmpty(ft.DefaultFormattedValue))
                {
                    val = $"coalesce({val}, {ft.DefaultFormattedValue})";
                }

                if (fieldType == "bit")
                    return $"try_cast(case when {val} = 'true' then 1 else 0 end as {fieldType})";
                else
                    return $"try_cast({val} as {fieldType})";
            }

            return $"F{ft.ID}.FormattedValue";
        }
        protected string getFieldDataType(FieldType field)
        {
            switch (field?.Type)
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
        protected void setCellValueFromField(SLDocument document, int rowIndex, int colIndex, FieldType field, object value)
        {
            var valueString = value?.ToString() ?? "";
            switch ((field.Type ?? "").ToUpper())
            {
                case "DECIMAL":
                    double dVal = 0;
                    if (double.TryParse(valueString, out dVal))
                        document.SetCellValue(rowIndex, colIndex, dVal);
                    else
                        document.SetCellValue(rowIndex, colIndex, valueString);
                    break;
                case "NUMBER":
                    int intVal = 0;
                    if (int.TryParse(valueString, out intVal))
                        document.SetCellValue(rowIndex, colIndex, intVal);
                    else
                        document.SetCellValue(rowIndex, colIndex, valueString);
                    break;
                case "DATE":
                    if (DateTime.TryParse(valueString, out DateTime dateVal))
                    {
                        document.SetCellValue(rowIndex, colIndex, dateVal);

                        SLStyle style = document.CreateStyle();
                        style.FormatCode = "m/d/yyyy";
                        document.SetCellStyle(rowIndex, colIndex, style);
                    }
                    break;
                default:
                    if (valueString.StartsWith("="))
                        valueString = "'" + valueString;
                    document.SetCellValue(rowIndex, colIndex, valueString);
                    break;
            }
        }
    }
}
