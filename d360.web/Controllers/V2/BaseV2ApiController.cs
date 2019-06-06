using d360.core.entities;
using d360.model;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace d360.web.Controllers.V2
{
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

            if(deserializeAsIs) return JsonConvert.DeserializeObject<T>(json);

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
    }
}
