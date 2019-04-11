using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/fields"),
        Authorize
    ]
    public class FieldsController : BaseApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;

        public FieldsController(CommunityContext community, CompanyContext company, IStorageProvider storage, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
        }

        #endregion

        #region utils

        private async Task<T> readRequestJsonContent<T>(HttpRequestMessage request)
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

        private void getFieldSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> fieldJoins, List<string> fieldColumns)
        {
            fieldTypes.ForEach(f =>
            {
                var defaultVal = f.DefaultFormattedValue;
                var joinPrefix = "left";
                var tableAlias = $"F{f.ID}";
                var columnName = f.Name;
                var valueColumn = "FormattedValue";
                var fieldDataType = getFieldDataType(f);

                if (f.Type == "Link")
                    valueColumn = "Value";

                if (f.Type == "FieldFromRelationship")
                {
                    if (!f.LookupObjectFieldTypeID.HasValue || !f.LookupObjectID.HasValue)
                        return;

                    var relatedField = Company.GetById<FieldType>((int)f.LookupObjectFieldTypeID);
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
                        else
                            fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");

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
                else
                {
                    fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = A.[Object] and {tableAlias}.[ObjectID] = A.[ObjectID]");
                }
            });
        }

        private void getQueryParamsSql(AssetsApiViewModel model, AssetType assetType, List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> whereStatements, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (queryParams != null)
            {

                var orderBySql = "";
                var offsetSql = "";
                var pageNum = -1;
                var pageSize = -1;

                //add base sort if none is specified
                if (!queryParams.Any(p => p.Key == "_order"))
                {
                    orderBySql = "order by A.ID";
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
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.Name";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "sourceid")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.SourceID";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "textpath")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.TextPath";
                                }
                                else if (assetType.Object == "ReferenceItemType" && q.Value.ToLower() == "code")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "RI.Code";
                                }
                                else
                                {
                                    var field = fieldTypes.FirstOrDefault(f => f.Name.ToLower() == q.Value.ToLower());
                                    var valueColumn = "FormattedValue";
                                    var fieldDataType = getFieldDataType(field);
                                    if (field.Type == "Link") valueColumn = "Value";

                                    if (field == null)
                                    {
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "A.ID";
                                        return;
                                    }

                                    if (!string.IsNullOrEmpty(fieldDataType))
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"cast(F{field.ID}.{valueColumn} as {fieldDataType})";
                                    else
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"F{field.ID}.{valueColumn}";
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
                                    var tableAlias = $"F{field.ID}";
                                    whereStatements.Add($"{tableAlias}.FormattedValue = @field{field.ID}");
                                    dbArgs.Add($"@field{field.ID}", q.Value);
                                }
                            }
                        }
                    });

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

        #endregion

        private async Task<FieldTypesApiViewModel> GetFieldTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            Guid? actionTypeUid = null;
            Guid? assetTypeUid = null;
            Guid? relationshipTypeUid = null;
            int pageNumber = 1;
            int pageSize = 250;

            var whereClause = "";

            #region Parameter Checking

            var dbArgs = new DynamicParameters();

            var parameters = queryParams.ToList();

            string obj = null;
            int? objID = null;

            if (parameters.Any(q => q.Key.ToLower() == "actiontypeuid"))
            {
                var actionTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "actiontypeuid").Value;
                Guid ac;
                if (Guid.TryParse(actionTypeUidString, out ac))
                {
                    actionTypeUid = ac;
                    var actionType = Company.Filter<IssueType>(i => i.ID == 0).SingleOrDefault();
                    if (actionType != null)
                    {
                        obj = "IssueType";
                        objID = actionType.ID;
                    }
                    else
                    {
                        throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Action Type not found based on Uid provided [{actionTypeUid.ToString()}].");
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "assettypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid.");
                }
                else
                {
                    var assetTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
                    Guid at;
                    if (Guid.TryParse(assetTypeUidString, out at))
                    {
                        assetTypeUid = at;
                        var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();
                        if (assetType != null)
                        {
                            obj = assetType.Object;
                            objID = assetType.ObjectID;
                        }
                        else
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Asset Type not found based on Uid provided [{assetTypeUid.ToString()}].");
                        }
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "relationshiptypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid.");
                }
                else if (assetTypeUid.HasValue)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid.");
                }
                else
                {
                    var relationshipTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
                    Guid rt;
                    if (Guid.TryParse(relationshipTypeUidString, out rt))
                    {
                        relationshipTypeUid = rt;
                        var intersectType = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault();
                        if (intersectType != null)
                        {
                            obj = "IntersectType";
                            objID = intersectType.ID;
                        }
                        else
                        {
                            throw new RestApiException(HttpStatusCode.NotFound, "Type not found", $"Relationship Type not found based on Uid provided [{relationshipTypeUid.ToString()}].");
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(obj) && objID.HasValue)
            {
                dbArgs.Add("@obj", obj);
                dbArgs.Add("@objID", objID.Value);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " ") + $"FT.[Object] = @obj and FT.[ObjectID] = @objID";
            }

            if (parameters.Any(q => q.Key.ToLower() == "name"))
            {
                var fieldTypeName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.ToLower();
                dbArgs.Add("@name", fieldTypeName);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " ") + $"lower(FT.[Name]) = @name";
            }

            if (parameters.Any(q => q.Key.ToLower() == "friendlyname"))
            {
                var fieldTypeFriendlyName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "friendlyname").Value.ToLower();
                dbArgs.Add("@fname", fieldTypeFriendlyName);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " ") + $"lower(FT.[FriendlyName]) = @fname";
            }

            if (parameters.Any(q => q.Key.ToLower() == "type"))
            {
                var fieldTypeType = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "type").Value.ToLower();
                dbArgs.Add("@type", fieldTypeType);
                whereClause += (string.IsNullOrEmpty(whereClause) ? " where " : " ") + $"lower(FT.[Type]) = @type";
            }

            if (parameters.Any(q => q.Key.ToLower() == "_pagenum"))
            {
                var pageNumberString = parameters.FirstOrDefault(q => q.Key.ToLower() == "_pagenum").Value;
                if (!int.TryParse(pageNumberString, out pageNumber))
                {
                    pageNumber = 1;
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "_pagesize"))
            {
                var pageSizeString = parameters.FirstOrDefault(q => q.Key.ToLower() == "_pagesize").Value;
                if (!int.TryParse(pageSizeString, out pageSize))
                {
                    pageSize = 250;
                }
            }

            if (pageNumber < 0)
            {
                pageNumber = 1;
            }
            if (pageSize < 0 || pageSize > 250)
            {
                pageSize = 250;
            }

            dbArgs.Add("@pageNum", pageNumber);
            dbArgs.Add("@pageSize", pageSize);

            #endregion
           
            var sql = $@"
declare @total int
select	@total = count(1) from FieldType FT {whereClause}

select	@pageSize as 'pageSize',
		@pageNum as 'pageNum',
		@total as 'total',
		(
        select	
		        FT.Name,
		        FT.FriendlyName,
		        FT.Category,

		        case when FT.Type = 'Boolean' then FT.ColumnOrder else null end as 'Type.Boolean.ColumnOrder',
		        case when FT.Type = 'Boolean' then FT.ColumnWidth else null end as 'Type.Boolean.ColumnWidth',
		        case when FT.Type = 'Boolean' then FT.SortOrder else null end as 'Type.Boolean.SortOrder',
		        case when FT.Type = 'Boolean' then FT.DefaultValue else null end as 'Type.Boolean.DefaultValue',
		        case when FT.Type = 'Boolean' then FT.DisplayDescription else null end as 'Type.Boolean.Description.Display',
		        case when FT.Type = 'Boolean' then FT.FormDescription else null end as 'Type.Boolean.Description.Form',
		        case when FT.Type = 'Boolean' then FT.IsDisplayable else null end as 'Type.Boolean.IsDisplayable',
		        case when FT.Type = 'Boolean' then FT.IsEditable else null end as 'Type.Boolean.IsEditable',
		        case when FT.Type = 'Boolean' then FT.IsListable else null end as 'Type.Boolean.IsListable',
		        case when FT.Type = 'Boolean' then FT.IsPartOfKey else null end as 'Type.Boolean.IsPartOfKey',
		        case when FT.Type = 'Boolean' then FT.IsPrimaryFilter else null end as 'Type.Boolean.IsPrimaryFilter',

		        case when FT.Type = 'FusionLookup' then FT.ColumnOrder else null end as 'Type.ComputedFusionLookup.ColumnOrder',

		        case when FT.Type = 'OwnershipLookup' then FT.ColumnOrder else null end as 'Type.ComputedOwnershipLookup.ColumnOrder',
		        case when FT.Type = 'OwnershipLookup' then FT.DisplayDescription else null end as 'Type.ComputedOwnershipLookup.Description.Display',
		        case when FT.Type = 'OwnershipLookup' then JSON_VALUE(FTL.Definition, '$.DisplayAssignmentSource') else null end as 'Type.ComputedOwnershipLookup.DisplayAssignmentSource',
		        case when FT.Type = 'OwnershipLookup' then JSON_VALUE(FTL.Definition, '$.ExpandGroupMembership') else null end as 'Type.ComputedOwnershipLookup.ExpandGroupMembership',
		        case when FT.Type = 'OwnershipLookup' then FT.IsDisplayable else null end as 'Type.ComputedOwnershipLookup.IsDisplayable',
		        case when FT.Type = 'OwnershipLookup' then FT.ShowIfEmpty else null end as 'Type.ComputedOwnershipLookup.ShowIfEmpty',

		        case when FT.Type = 'FieldFromRelationship' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipField.ColumnOrder',
		        case when FT.Type = 'FieldFromRelationship' then FT.ColumnWidth else null end as 'Type.ComputedRelationshipField.ColumnWidth',
		        case when FT.Type = 'FieldFromRelationship' then FT.SortOrder else null end as 'Type.ComputedRelationshipField.SortOrder',
		        case when FT.Type = 'FieldFromRelationship' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipField.Description.Display',
		        case when FT.Type = 'FieldFromRelationship' then IT.Uid else null end as 'Type.ComputedRelationshipField.IntersectTypeUid',
		        case when FT.Type = 'FieldFromRelationship' then LFT.Name else null end as 'Type.ComputedRelationshipField.FieldTypeName',
		        case when FT.Type = 'FieldFromRelationship' then FT.IsDisplayable else null end as 'Type.ComputedRelationshipField.IsDisplayable',
		        case when FT.Type = 'FieldFromRelationship' then FT.IsListable else null end as 'Type.ComputedRelationshipField.IsListable',
		        case when FT.Type = 'FieldFromRelationship' then FT.ShowIfEmpty else null end as 'Type.ComputedRelationshipField.ShowIfEmpty',

		        case when FT.Type = 'ComplexRelationLookup' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipLookup.ColumnOrder',
		        case when FT.Type = 'ComplexRelationLookup' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipLookup.Description.Display',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideHeader else null end as 'Type.ComputedRelationshipLookup.HideHeader',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideFooter else null end as 'Type.ComputedRelationshipLookup.HideFooter',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.HideFilter else null end as 'Type.ComputedRelationshipLookup.HideFilter',
		        case when FT.Type = 'ComplexRelationLookup' then FTL.LookupType else null end as 'Type.ComputedRelationshipLookup.LookupType',

		        case when FT.Type = 'ComplexRelationLookup' then (
		        select	IST.Uid as IntersectTypeUid,
				        AST.Uid as AssetTypeUid,
				        DR.RelationType,
				        DR.Direction
		        from	OPENJSON(FTL.Definition) with (Relations nvarchar(max) as json) D
				        outer apply OPENJSON(D.Relations) with (IntersectTypeID int, Object varchar(50), ObjectID int, RelationType int, Direction int) DR
				        left join IntersectType IST on IST.ID = DR.IntersectTypeID
				        left join AssetType AST on AST.Object = DR.Object and AST.ObjectID = DR.ObjectID
		        for json path
		        ) else null end as 'Type.ComputedRelationshipLookup.Relations',
		        case when FT.Type = 'ComplexRelationLookup' then (
		        select	AST.Uid as AssetTypeUid,
				        coalesce(AFT.Name, DF.FieldTypeName) as FieldTypeName,
				        DF.[Filter],
				        DF.OverrideDisplayName,
				        DF.DisplayOrder,
				        DF.SortOrder,
				        DF.Show,
				        DF.Width
		        from	OPENJSON(FTL.Definition) with (Fields nvarchar(max) as json) D
				        outer apply OPENJSON(D.Fields) with (Object varchar(50), ObjectId int, FieldTypeID int, FieldTypeName nvarchar(250), [Filter] nvarchar(500), OverrideDisplayName nvarchar(250), DisplayOrder int, SortOrder int, Show bit, Width int) DF
				        left join AssetType AST on AST.Object = DF.Object and AST.ObjectID = DF.ObjectID
				        left join FieldType AFT on AFT.ID = DF.FieldTypeID
		        order by DF.DisplayOrder
		        for json path
		        ) else null end as 'Type.ComputedRelationshipLookup.Fields',
		        case when FT.Type = 'ComplexRelationLookup' then FT.IsDisplayable else null end as 'Type.ComputedRelationshipLookup.IsDisplayable',
		        case when FT.Type = 'ComplexRelationLookup' then FT.ShowIfEmpty else null end as 'Type.ComputedRelationshipLookup.ShowIfEmpty',

		        case when FT.Type = 'RefListRelationship' then FT.ColumnOrder else null end as 'Type.ComputedRelationshipReferenceList.ColumnOrder',
		        case when FT.Type = 'RefListRelationship' then FT.DisplayDescription else null end as 'Type.ComputedRelationshipReferenceList.Description.Display',
		        case when FT.Type = 'RefListRelationship' then IT.Uid else null end as 'Type.ComputedRelationshipReferenceList.IntersectTypeUid',
		        case when FT.Type = 'RefListRelationship' then FT.IsDisplayable else null end as 'Type.ComputedRelationshipReferenceList.IsDisplayable',
		        case when FT.Type = 'RefListRelationship' then FT.ShowIfEmpty else null end as 'Type.ComputedRelationshipReferenceList.ShowIfEmpty',

		        case when FT.Type = 'Date' then FT.ColumnOrder else null end as 'Type.Date.ColumnOrder',
		        case when FT.Type = 'Date' then FT.ColumnWidth else null end as 'Type.Date.ColumnWidth',
		        case when FT.Type = 'Date' then FT.SortOrder else null end as 'Type.Date.SortOrder',
		        case when FT.Type = 'Date' then FT.DefaultValue else null end as 'Type.Date.DefaultValue',
		        case when FT.Type = 'Date' then FT.DisplayDescription else null end as 'Type.Date.Description.Display',
		        case when FT.Type = 'Date' then FT.FormDescription else null end as 'Type.Date.Description.Form',
		        case when FT.Type = 'Date' then FT.IsRequired else null end as 'Type.Date.Validation.IsRequired',
		        case when FT.Type = 'Date' then FT.ValidationDescription else null end as 'Type.Date.Validation.Message',
		        case when FT.Type = 'Date' then FT.IsDisplayable else null end as 'Type.Date.IsDisplayable',
		        case when FT.Type = 'Date' then FT.IsEditable else null end as 'Type.Date.IsEditable',
		        case when FT.Type = 'Date' then FT.IsListable else null end as 'Type.Date.IsListable',
		        case when FT.Type = 'Date' then FT.IsPartOfKey else null end as 'Type.Date.IsPartOfKey',
		        case when FT.Type = 'Date' then FT.IsPrimaryFilter else null end as 'Type.Date.IsPrimaryFilter',
		        case when FT.Type = 'Date' then FT.ShowIfEmpty else null end as 'Type.Date.ShowIfEmpty',

		        case when FT.Type = 'DateTime' then FT.ColumnOrder else null end as 'Type.DateTime.ColumnOrder',
		        case when FT.Type = 'DateTime' then FT.ColumnWidth else null end as 'Type.DateTime.ColumnWidth',
		        case when FT.Type = 'DateTime' then FT.SortOrder else null end as 'Type.DateTime.SortOrder',
		        case when FT.Type = 'DateTime' then FT.DefaultValue else null end as 'Type.DateTime.DefaultValue',
		        case when FT.Type = 'DateTime' then FT.DisplayDescription else null end as 'Type.DateTime.Description.Display',
		        case when FT.Type = 'DateTime' then FT.FormDescription else null end as 'Type.DateTime.Description.Form',
		        case when FT.Type = 'DateTime' then FT.IsRequired else null end as 'Type.DateTime.Validation.IsRequired',
		        case when FT.Type = 'DateTime' then FT.ValidationDescription else null end as 'Type.DateTime.Validation.Message',
		        case when FT.Type = 'DateTime' then FT.IsDisplayable else null end as 'Type.DateTime.IsDisplayable',
		        case when FT.Type = 'DateTime' then FT.IsEditable else null end as 'Type.DateTime.IsEditable',
		        case when FT.Type = 'DateTime' then FT.IsListable else null end as 'Type.DateTime.IsListable',
		        case when FT.Type = 'DateTime' then FT.IsPartOfKey else null end as 'Type.DateTime.IsPartOfKey',
		        case when FT.Type = 'DateTime' then FT.IsPrimaryFilter else null end as 'Type.DateTime.IsPrimaryFilter',
		        case when FT.Type = 'DateTime' then FT.ShowIfEmpty else null end as 'Type.DateTime.ShowIfEmpty',

		        case when FT.Type = 'Decimal' then FT.ColumnOrder else null end as 'Type.Decimal.ColumnOrder',
		        case when FT.Type = 'Decimal' then FT.ColumnWidth else null end as 'Type.Decimal.ColumnWidth',
		        case when FT.Type = 'Decimal' then FT.SortOrder else null end as 'Type.Decimal.SortOrder',
		        case when FT.Type = 'Decimal' then FT.DefaultValue else null end as 'Type.Decimal.DefaultValue',
		        case when FT.Type = 'Decimal' then FT.DisplayDescription else null end as 'Type.Decimal.Description.Display',
		        case when FT.Type = 'Decimal' then FT.FormDescription else null end as 'Type.Decimal.Description.Form',
		        case when FT.Type = 'Decimal' then FT.Increment else null end as 'Type.Decimal.Increment',
		        case when FT.Type = 'Decimal' then FT.MinimumLength else null end as 'Type.Decimal.Validation.MinimumLength',
		        case when FT.Type = 'Decimal' then FT.MaximumLength else null end as 'Type.Decimal.Validation.MaximumLength',
		        case when FT.Type = 'Decimal' then FT.[Precision] else null end as 'Type.Decimal.Validation.Precision',
		        case when FT.Type = 'Decimal' then FT.[Length] else null end as 'Type.Decimal.Validation.Length',
		        case when FT.Type = 'Decimal' then FT.IsRequired else null end as 'Type.Decimal.Validation.IsRequired',
		        case when FT.Type = 'Decimal' then FT.ValidationDescription else null end as 'Type.Decimal.Validation.Message',
		        case when FT.Type = 'Decimal' then FT.IsDisplayable else null end as 'Type.Decimal.IsDisplayable',
		        case when FT.Type = 'Decimal' then FT.IsEditable else null end as 'Type.Decimal.IsEditable',
		        case when FT.Type = 'Decimal' then FT.IsListable else null end as 'Type.Decimal.IsListable',
		        case when FT.Type = 'Decimal' then FT.IsPartOfKey else null end as 'Type.Decimal.IsPartOfKey',
		        case when FT.Type = 'Decimal' then FT.ShowIfEmpty else null end as 'Type.Decimal.ShowIfEmpty',

		        case when FT.Type = 'Html' then FT.ColumnOrder else null end as 'Type.Html.ColumnOrder',
		        case when FT.Type = 'Html' then FT.ColumnWidth else null end as 'Type.Html.ColumnWidth',
		        case when FT.Type = 'Html' then FT.SortOrder else null end as 'Type.Html.SortOrder',
		        case when FT.Type = 'Html' then FT.DefaultValue else null end as 'Type.Html.DefaultValue',
		        case when FT.Type = 'Html' then FT.DisplayDescription else null end as 'Type.Html.Description.Display',
		        case when FT.Type = 'Html' then FT.FormDescription else null end as 'Type.Html.Description.Form',
		        case when FT.Type = 'Html' then FT.MinimumLength else null end as 'Type.Html.Validation.MinimumLength',
		        case when FT.Type = 'Html' then FT.MaximumLength else null end as 'Type.Html.Validation.MaximumLength',
		        case when FT.Type = 'Html' then FT.[Length] else null end as 'Type.Html.Validation.Length',
		        case when FT.Type = 'Html' then FT.IsRequired else null end as 'Type.Html.Validation.IsRequired',
		        case when FT.Type = 'Html' then FT.ValidationDescription else null end as 'Type.Html.Validation.Message',
		        case when FT.Type = 'Html' then FT.IsDisplayable else null end as 'Type.Html.IsDisplayable',
		        case when FT.Type = 'Html' then FT.IsEditable else null end as 'Type.Html.IsEditable',
		        case when FT.Type = 'Html' then FT.IsListable else null end as 'Type.Html.IsListable',
		        case when FT.Type = 'Html' then FT.IsPartOfKey else null end as 'Type.Html.IsPartOfKey',
		        case when FT.Type = 'Html' then FT.IsPrimaryFilter else null end as 'Type.Html.IsPrimaryFilter',
		        case when FT.Type = 'Html' then FT.ShowIfEmpty else null end as 'Type.Html.ShowIfEmpty',

		        case when FT.Type = 'Json' then FT.ColumnOrder else null end as 'Type.Json.ColumnOrder',
		        case when FT.Type = 'Json' then FT.DisplayDescription else null end as 'Type.Json.Description.Display',
		        case when FT.Type = 'Json' then FT.IsDisplayable else null end as 'Type.Json.IsDisplayable',
		        case when FT.Type = 'Json' then FT.IsEditable else null end as 'Type.Json.IsEditable',
		        case when FT.Type = 'Json' then FT.ShowIfEmpty else null end as 'Type.Json.ShowIfEmpty',

		        case when FT.Type = 'Link' then FT.ColumnOrder else null end as 'Type.Link.ColumnOrder',
		        case when FT.Type = 'Link' then FT.ColumnWidth else null end as 'Type.Link.ColumnWidth',
		        case when FT.Type = 'Link' then FT.SortOrder else null end as 'Type.Link.SortOrder',
		        case when FT.Type = 'Link' then case when CHARINDEX('|', FT.DefaultValue, 1) > 1 then SUBSTRING(FT.DefaultValue, 1, CHARINDEX('|', FT.DefaultValue, 1)-1) else FT.DEfaultValue end else null end as 'Type.Link.DefaultValue.Text',
		        case when FT.Type = 'Link' then case when CHARINDEX('|', FT.DefaultValue, 1) > 1 then SUBSTRING(FT.DefaultValue, CHARINDEX('|', FT.DefaultValue, 1)+1, LEN(FT.DefaultValue)-CHARINDEX('|', FT.DefaultValue, 1)) else FT.DEfaultValue end else null end as 'Type.Link.DefaultValue.Url',
		        case when FT.Type = 'Link' then FT.DisplayDescription else null end as 'Type.Link.Description.Display',
		        case when FT.Type = 'Link' then FT.FormDescription else null end as 'Type.Link.Description.Form',
		        case when FT.Type = 'Link' then FT.IsRequired else null end as 'Type.Link.Validation.IsRequired',
		        case when FT.Type = 'Link' then FT.ValidationDescription else null end as 'Type.Link.Validation.Message',
		        case when FT.Type = 'Link' then FT.IsDisplayable else null end as 'Type.Link.IsDisplayable',
		        case when FT.Type = 'Link' then FT.IsEditable else null end as 'Type.Link.IsEditable',
		        case when FT.Type = 'Link' then FT.IsListable else null end as 'Type.Link.IsListable',
		        case when FT.Type = 'Link' then FT.ShowIfEmpty else null end as 'Type.Link.ShowIfEmpty',

		        case when FT.Type = 'Lookup' then FT.ColumnOrder else null end as 'Type.Lookup.ColumnOrder',
		        case when FT.Type = 'Lookup' then FT.ColumnWidth else null end as 'Type.Lookup.ColumnWidth',
		        case when FT.Type = 'Lookup' then FT.SortOrder else null end as 'Type.Lookup.SortOrder',
		        case when FT.Type = 'Lookup' then FT.DefaultValue else null end as 'Type.Lookup.DefaultValue',
		        case when FT.Type = 'Lookup' then FT.DisplayDescription else null end as 'Type.Lookup.Description.Display',
		        case when FT.Type = 'Lookup' then FT.FormDescription else null end as 'Type.Lookup.Description.Form',
		        case when FT.Type = 'Lookup' then FT.AllowAllValue else null end as 'Type.Lookup.Validation.AllowAllValue',
		        case when FT.Type = 'Lookup' then FT.AllowAllLabel else null end as 'Type.Lookup.Validation.AllAllLabel',
		        case when FT.Type = 'Lookup' then FilterFT.[Name] else null end as 'Type.Lookup.Filter.FieldTypeName',
		        case when FT.Type = 'Lookup' then FilterPT.[Uid] else null end as 'Type.Lookup.Filter.PredicateUid',
		        case when FT.Type = 'Lookup' then FT.FilterPredicateDirection else null end as 'Type.Lookup.Filter.UseDirection',
		        case when FT.Type = 'Lookup' then FT.LookupDisplayFormat else null end as 'Type.Lookup.Format.Display',
		        case when FT.Type = 'Lookup' then FT.LookupEditFormat else null end as 'Type.Lookup.Format.Edit',
		        case when FT.Type = 'Lookup' then LookupOT.Uid else null end as 'Type.Lookup.List.Uid',
		        case when FT.Type = 'Lookup' then FT.AllowMultipleValues else null end as 'Type.Lookup.List.AllowMultipleValues',
		        case when FT.Type = 'Lookup' then FT.IsDisplayable else null end as 'Type.Lookup.IsDisplayable',
		        case when FT.Type = 'Lookup' then FT.IsEditable else null end as 'Type.Lookup.IsEditable',
		        case when FT.Type = 'Lookup' then FT.IsListable else null end as 'Type.Lookup.IsListable',
		        case when FT.Type = 'Lookup' then FT.IsPartOfKey else null end as 'Type.Lookup.IsPartOfKey',
		        case when FT.Type = 'Lookup' then FT.IsPrimaryFilter else null end as 'Type.Lookup.IsPrimaryFilter',
		        case when FT.Type = 'Lookup' then FT.ShowIfEmpty else null end as 'Type.Lookup.ShowIfEmpty',

		        case when FT.Type = 'Number' then FT.ColumnOrder else null end as 'Type.Number.ColumnOrder',
		        case when FT.Type = 'Number' then FT.ColumnWidth else null end as 'Type.Number.ColumnWidth',
		        case when FT.Type = 'Number' then FT.SortOrder else null end as 'Type.Number.SortOrder',
		        case when FT.Type = 'Number' then FT.DefaultValue else null end as 'Type.Number.DefaultValue',
		        case when FT.Type = 'Number' then FT.DisplayDescription else null end as 'Type.Number.Description.Display',
		        case when FT.Type = 'Number' then FT.FormDescription else null end as 'Type.Number.Description.Form',
		        case when FT.Type = 'Number' then FT.Increment else null end as 'Type.Number.Increment',
		        case when FT.Type = 'Number' then FT.MinimumLength else null end as 'Type.Number.Validation.MinimumLength',
		        case when FT.Type = 'Number' then FT.MaximumLength else null end as 'Type.Number.Validation.MaximumLength',
		        case when FT.Type = 'Number' then FT.[Length] else null end as 'Type.Number.Validation.Length',
		        case when FT.Type = 'Number' then FT.IsRequired else null end as 'Type.Number.Validation.IsRequired',
		        case when FT.Type = 'Number' then FT.ValidationDescription else null end as 'Type.Number.Validation.Message',
		        case when FT.Type = 'Number' then FT.IsDisplayable else null end as 'Type.Number.IsDisplayable',
		        case when FT.Type = 'Number' then FT.IsEditable else null end as 'Type.Number.IsEditable',
		        case when FT.Type = 'Number' then FT.IsListable else null end as 'Type.Number.IsListable',
		        case when FT.Type = 'Number' then FT.IsPartOfKey else null end as 'Type.Number.IsPartOfKey',
		        case when FT.Type = 'Number' then FT.IsPrimaryFilter else null end as 'Type.Number.IsPrimaryFilter',
		        case when FT.Type = 'Number' then FT.ShowIfEmpty else null end as 'Type.Number.ShowIfEmpty',

		        case when FT.Type = 'Relationship' then FT.ColumnOrder else null end as 'Type.Relationship.ColumnOrder',
		        case when FT.Type = 'Relationship' then FT.ColumnWidth else null end as 'Type.Relationship.ColumnWidth',
		        case when FT.Type = 'Relationship' then FT.SortOrder else null end as 'Type.Relationship.SortOrder',
		        case when FT.Type = 'Relationship' then FT.DisplayDescription else null end as 'Type.Relationship.Description.Display',
		        case when FT.Type = 'Relationship' then FT.FormDescription else null end as 'Type.Relationship.Description.Form',
		        case when FT.Type = 'Relationship' then IT.Uid else null end as 'Type.Relationship.IntersectTypeUid',
		        case when FT.Type = 'Relationship' then FT.IsRequired else null end as 'Type.Relationship.Validation.IsRequired',
		        case when FT.Type = 'Relationship' then FT.ValidationDescription else null end as 'Type.Relationship.Validation.Message',
		        case when FT.Type = 'Relationship' then FT.IsDisplayable else null end as 'Type.Relationship.IsDisplayable',
		        case when FT.Type = 'Relationship' then FT.IsEditable else null end as 'Type.Relationship.IsEditable',
		        case when FT.Type = 'Relationship' then FT.IsListable else null end as 'Type.Relationship.IsListable',
		        case when FT.Type = 'Relationship' then FT.IsPrimaryFilter else null end as 'Type.Relationship.IsPrimaryFilter',
		        case when FT.Type = 'Relationship' then FT.ShowIfEmpty else null end as 'Type.Relationship.ShowIfEmpty',

		        case when FT.Type = 'Text' then FT.ColumnOrder else null end as 'Type.Text.ColumnOrder',
		        case when FT.Type = 'Text' then FT.ColumnWidth else null end as 'Type.Text.ColumnWidth',
		        case when FT.Type = 'Text' then FT.SortOrder else null end as 'Type.Text.SortOrder',
		        case when FT.Type = 'Text' then FT.DefaultValue else null end as 'Type.Text.DefaultValue',
		        case when FT.Type = 'Text' then FT.DisplayDescription else null end as 'Type.Text.Description.Display',
		        case when FT.Type = 'Text' then FT.FormDescription else null end as 'Type.Text.Description.Form',
		        case when FT.Type = 'Text' then FT.MinimumLength else null end as 'Type.Text.Validation.MinimumLength',
		        case when FT.Type = 'Text' then FT.MaximumLength else null end as 'Type.Text.Validation.MaximumLength',
		        case when FT.Type = 'Text' then FT.[Length] else null end as 'Type.Text.Validation.Length',
		        case when FT.Type = 'Text' then FT.Pattern else null end as 'Type.Text.Validation.Pattern',
		        case when FT.Type = 'Text' then FT.IsRequired else null end as 'Type.Text.Validation.IsRequired',
		        case when FT.Type = 'Text' then FT.ValidationDescription else null end as 'Type.Text.Validation.Message',
		        case when FT.Type = 'Text' then FT.IsDisplayable else null end as 'Type.Text.IsDisplayable',
		        case when FT.Type = 'Text' then FT.IsEditable else null end as 'Type.Text.IsEditable',
		        case when FT.Type = 'Text' then FT.IsListable else null end as 'Type.Text.IsListable',
		        case when FT.Type = 'Text' then FT.IsPartOfKey else null end as 'Type.Text.IsPartOfKey',
		        case when FT.Type = 'Text' then FT.IsPrimaryFilter else null end as 'Type.Text.IsPrimaryFilter',
		        case when FT.Type = 'Text' then FT.ShowIfEmpty else null end as 'Type.Text.ShowIfEmpty'
        from	FieldType FT
		        left join FieldTypeLookup FTL on FTL.FieldTypeID = FT.ID
		        left join IntersectType IT on (FT.[Type] = 'FieldFromRelationship' or FT.[Type] = 'RefListRelationship' or FT.[Type] = 'Relationship') and FT.LookupObjectType = 'IntersectType' and IT.ID = FT.LookupObjectID
		        left join FieldType LFT on FT.[Type] = 'FieldFromRelationship' and LFT.ID = FT.LookupObjectFieldTypeID

		        left join FieldType FilterFT on FT.[Type] = 'Lookup' and FilterFT.ID = FT.FilterFieldTypeID
		        left join [Predicate] FilterPT on FT.[Type] = 'Lookup' and FilterPT.ID = FT.FilterPredicateID
		        left join [AssetType] LookupOT on FT.[Type] = 'Lookup' and LookupOT.[Object] = FT.LookupObjectType and LookupOT.ObjectID = FT.LookupObjectID
        {whereClause}
        order by FT.Object, FT.ObjectID, FT.Name
        offset ((@pageNum-1) * @pageSize) rows fetch next @pageSize rows only
        for json path
        ) as 'items'
for json path, WITHOUT_ARRAY_WRAPPER";

            var model = await Company.GetDatabaseJsonAsObjectAsync<FieldTypesApiViewModel>(sql, dbArgs);

            return model;
        }

        // <param name="ActionTypeUid">The action type Uid to retrieve field types for.</param>
        /// <summary>
        /// Retrieves field types contained within your environment.
        /// </summary>
        /// <param name="AssetTypeUid">The asset type Uid to retrieve field types for.</param>
        /// <param name="RelationshipTypeUid">The relationship type Uid to retrieve field types for.</param>
        /// <param name="Name">The API Name to search for.</param>
        /// <param name="FriendlyName">The Friendly Name to search for.</param>
        /// <param name="Type">The data type to search for.</param>
        /// <param name="_pageSize">The number of results to return per page. The default value is 200.</param>
        /// <param name="_pageNum">The page number to return results for.</param>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpGet,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(FieldTypesApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetFieldTypesAsync(Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, 
            //Guid? ActionTypeUid = null, 
            string Name = "", string FriendlyName = "", DataType? Type = null, int? _pageSize = null, int? _pageNum = null)
        {
            var prefix = "Fields.GetFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var results = await GetFieldTypes(queryParams);
                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return ReturnApiError(ex.Status, errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }

        }
    }
}
