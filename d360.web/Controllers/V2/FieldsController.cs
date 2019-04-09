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

        private async Task<IHttpActionResult> GetFieldTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            Guid? actionTypeUid = null;
            Guid? assetTypeUid = null;
            Guid? relationshipTypeUid = null;

            var fieldTypes = Company.FieldTypes.Include("FieldTypeLookup").AsQueryable();

            #region Parameter Checking

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
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Type not found", $"Action Type not found based on Uid provided [{actionTypeUid.ToString()}]."));
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "assettypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid."));
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
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Type not found", $"Asset Type not found based on Uid provided [{assetTypeUid.ToString()}]."));
                        }
                    }
                }
            }
            if (parameters.Any(q => q.Key.ToLower() == "relationshiptypeuid"))
            {
                if (actionTypeUid.HasValue)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid."));
                }
                else if (assetTypeUid.HasValue)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid."));
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
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Type not found", $"Relationship Type not found based on Uid provided [{relationshipTypeUid.ToString()}]."));
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(obj) && objID.HasValue)
            {
                fieldTypes = fieldTypes.Where(i => i.Object == obj && i.ObjectID == objID.Value);
            }

            if (parameters.Any(q => q.Key.ToLower() == "name"))
            {
                var fieldTypeName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.ToLower();
                fieldTypes = fieldTypes.Where(i => i.Name.ToLower() == fieldTypeName);
            }

            if (parameters.Any(q => q.Key.ToLower() == "friendlyname"))
            {
                var fieldTypeFriendlyName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "friendlyname").Value.ToLower();
                fieldTypes = fieldTypes.Where(i => i.FriendlyName.ToLower() == fieldTypeFriendlyName);
            }

            if (parameters.Any(q => q.Key.ToLower() == "type"))
            {
                var fieldTypeType = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "type").Value.ToLower();
                fieldTypes = fieldTypes.Where(i => i.Type.ToLower() == fieldTypeType);
            }

            #endregion

            var countSql = $@"";

            var sql = $@"
select	FT.*,
		FTL.HideHeader,
		FTL.HideFooter,
		FTL.HideFilter,
		FTL.LookupType,
		FTL.Definition
from	FieldType FT
		left join FieldTypeLookup FTL on FTL.FieldTypeID = FT.ID ";

            //var countResults = await Company.QueryAsync<int>(countSql, dbArgs);
            var count = 1;//countResults.First();

            var model = new FieldTypesApiViewModel { items = new List<FieldTypeApiViewModel>(), pageNum = 1, pageSize = 250, total = 1 };

            foreach (var ft in fieldTypes)
            {
                var ftModel = new FieldTypeApiViewModel { Category = ft.Category, FriendlyName = ft.FriendlyName, Name = ft.Name };

                switch (ft.Type)
                {
                    case "Boolean":
                        ftModel.Type.Boolean = new FieldTypeDataTypeBooleanApiViewModel {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder
                        };
                        bool bValue;
                        if (bool.TryParse(ft.DefaultValue, out bValue))
                        {
                            ftModel.Type.Boolean.DefaultValue = bValue;
                        }
                        break;
                    case "ComplexRelationLookup":
                        ftModel.Type.ComputedRelationshipLookup = new FieldTypeDataTypeComputedRelationshipLookupApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            Description = new FieldTypeDescriptionApiViewModel_Display { Display = ft.DisplayDescription },
                            Definition = ft.FieldTypeLookup.ParseComplexLookupDefinition(),
                            IsDisplayable = ft.IsDisplayable,
                            ShowIfEmpty = ft.ShowIfEmpty
                        };
                        break;
                    case "Date":
                        ftModel.Type.Date = new FieldTypeDataTypeDateApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder
                        };
                        DateTime dValue;
                        if (DateTime.TryParse(ft.DefaultValue, out dValue))
                        {
                            ftModel.Type.Date.DefaultValue = dValue;
                        }
                        break;
                    case "DateTime":
                        ftModel.Type.DateTime = new FieldTypeDataTypeDateTimeApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder
                        };
                        DateTime dtValue;
                        if (DateTime.TryParse(ft.DefaultValue, out dtValue))
                        {
                            ftModel.Type.DateTime.DefaultValue = dtValue;
                        }
                        break;
                    case "Decimal":
                        ftModel.Type.Decimal = new FieldTypeDataTypeDecimalApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder,
                            Validation = new FieldTypeDescriptionApiViewModel_ValidationDecimal
                            {
                                IsRequired = ft.IsRequired,
                                Length = ft.Length,
                                MaximumLength = ft.MaximumLength,
                                Message = ft.ValidationDescription,
                                MinimumLength = ft.MinimumLength
                            }
                        };
                        decimal dcValue;
                        if (decimal.TryParse(ft.DefaultValue, out dcValue))
                        {
                            ftModel.Type.Decimal.DefaultValue = dcValue;
                        }
                        break;
                    case "FieldFromRelationship":
                        ftModel.Type.ComputedRelationshipField = new FieldTypeDataTypeComputedRelationshipFieldApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_Display { Display = ft.DisplayDescription },
                            FieldTypeName = ft.LookupObjectFieldTypeID.ToString(),
                            //IntersectTypeUid = ft.LookupObjectID,
                            IsDisplayable = ft.IsDisplayable,
                            IsListable = ft.IsListable,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder
                        };

                        break;
                    case "Html":
                        ftModel.Type.Html = new FieldTypeDataTypeHtmlApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder,
                            DefaultValue = ft.DefaultValue,
                            Validation = new FieldTypeDescriptionApiViewModel_ValidationText
                            {
                                IsRequired = ft.IsRequired,
                                Length = ft.Length,
                                MaximumLength = ft.MaximumLength,
                                Message = ft.ValidationDescription,
                                MinimumLength = ft.MinimumLength,
                                Pattern = ft.Pattern
                            }
                        };
                        break;
                    case "JSON":
                        ftModel.Type.Json = new FieldTypeDataTypeJsonApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            Description = new FieldTypeDescriptionApiViewModel_Display { Display = ft.DisplayDescription },
                            IsDisplayable = ft.IsDisplayable,
                            ShowIfEmpty = ft.ShowIfEmpty
                        };
                        break;
                    case "Link":
                        ftModel.Type.Link = new FieldTypeDataTypeLinkApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder,
                            Validation = new FieldTypeDescriptionApiViewModel_ValidationDecimal
                            {
                                IsRequired = ft.IsRequired,
                                Length = ft.Length,
                                MaximumLength = ft.MaximumLength,
                                Message = ft.ValidationDescription,
                                MinimumLength = ft.MinimumLength
                            },
                            DefaultValue = new FieldTypeDataTypeLinkApiViewModel_DefaultValue {
                                Text = string.IsNullOrEmpty(ft.DefaultValue) ? "" : ft.DefaultValue.Split('|')[0],
                                Url = string.IsNullOrEmpty(ft.DefaultValue) ? "" : ft.DefaultValue.Split('|')[1]
                            }
                        };
                        break;
                    case "Lookup":
                        ftModel.Type.Lookup = new FieldTypeDataTypeLookupApiViewModel
                        {
                            AllowAllLabel = ft.AllowAllLabel,
                            AllowAllValue = ft.AllowAllValue,
                            Filter = new FieldTypeDataTypeLookupApiViewModel_Filter {
                                FieldTypeName = ft.FilterFieldTypeID.ToString(),
                                //PredicateUid = ft.FilterPredicateID,
                                UseDirection = ft.FilterPredicateDirection
                            },
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            Format = new FieldTypeDataTypeLookupApiViewModel_Format {
                                Display = ft.LookupDisplayFormat,
                                Edit = ft.LookupEditFormat
                            },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            List = new FieldTypeDataTypeLookupApiViewModel_List {
                                AllowMultipleValues = ft.AllowMultipleValues//,
                                //Uid = ft.
                            },
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder
                        };
                        int lValue;
                        if (int.TryParse(ft.DefaultValue, out lValue))
                        {
                            ftModel.Type.Lookup.DefaultValue = ft.DefaultValue;
                        }
                        break;
                    case "Number":
                        ftModel.Type.Number = new FieldTypeDataTypeNumberApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            Increment = ft.Increment,
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder, 
                            Validation = new FieldTypeDescriptionApiViewModel_ValidationDecimal
                            {
                                IsRequired = ft.IsRequired,
                                Length = ft.Length,
                                MaximumLength = ft.MaximumLength,
                                Message = ft.ValidationDescription,
                                MinimumLength = ft.MinimumLength
                            }
                        };
                        int nValue;
                        if (int.TryParse(ft.DefaultValue, out nValue))
                        {
                            ftModel.Type.Number.DefaultValue = nValue;
                        }
                        break;
                    case "OwnershipLookup":
                        ftModel.Type.ComputedOwnershipLookup = new FieldTypeDataTypeComputedOwnershipLookupApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            Description = new FieldTypeDescriptionApiViewModel_Display { Display = ft.DisplayDescription },
                            Definition = ft.FieldTypeLookup.ParseOwnershipLookupDefinition(),
                            IsDisplayable = ft.IsDisplayable,
                            ShowIfEmpty = ft.ShowIfEmpty
                        };
                        break;
                    case "Percentage":
                        //ftModel.Type.Decimal = new FieldTypeDataTypeDecimalApiViewModel
                        //{
                        //    ColumnOrder = ft.ColumnOrder,
                        //    ColumnWidth = ft.ColumnWidth,
                        //    Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                        //    IsDisplayable = ft.IsDisplayable,
                        //    IsEditable = ft.IsEditable,
                        //    IsListable = ft.IsListable,
                        //    IsPartOfKey = ft.IsPartOfKey,
                        //    IsPrimaryFilter = ft.IsPrimaryFilter,
                        //    ShowIfEmpty = ft.ShowIfEmpty,
                        //    SortOrder = ft.SortOrder,
                        //    Validation = new FieldTypeDescriptionApiViewModel_ValidationDecimal
                        //    {
                        //        IsRequired = ft.IsRequired,
                        //        Length = ft.Length,
                        //        MaximumLength = ft.MaximumLength,
                        //        Message = ft.ValidationDescription,
                        //        MinimumLength = ft.MinimumLength
                        //    }
                        //};
                        //decimal pcValue;
                        //if (decimal.TryParse(ft.DefaultValue, out pcValue))
                        //{
                        //    ftModel.Type.Decimal.DefaultValue = pcValue;
                        //}
                        break;
                    case "RefListRelationship":
                        ftModel.Type.ComputedRelationshipReferenceList = new FieldTypeDataTypeComputedRelationshipReferenceListApiViewModel 
                        {
                            ColumnOrder = ft.ColumnOrder,
                            Description = new FieldTypeDescriptionApiViewModel_Display { Display = ft.DisplayDescription },
                            IsDisplayable = ft.IsDisplayable,
                            ShowIfEmpty = ft.ShowIfEmpty
                            //IntersectTypeUid = ft.
                        };
                        break;
                    case "Relationship":
                        ftModel.Type.Relationship = new FieldTypeDataTypeRelationshipApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder,
                            //IntersectTypeUid = ft.
                            Validation = new FieldTypeDescriptionApiViewModel_ValidationText
                            {
                                IsRequired = ft.IsRequired,
                                Length = ft.Length,
                                MaximumLength = ft.MaximumLength,
                                Message = ft.ValidationDescription,
                                MinimumLength = ft.MinimumLength,
                                Pattern = ft.Pattern
                            }
                        };
                        break;
                    case "Text":
                        ftModel.Type.Text = new FieldTypeDataTypeTextApiViewModel
                        {
                            ColumnOrder = ft.ColumnOrder,
                            ColumnWidth = ft.ColumnWidth,
                            Description = new FieldTypeDescriptionApiViewModel_DisplayForm { Display = ft.DisplayDescription, Form = ft.FormDescription },
                            IsDisplayable = ft.IsDisplayable,
                            IsEditable = ft.IsEditable,
                            IsListable = ft.IsListable,
                            IsPartOfKey = ft.IsPartOfKey,
                            IsPrimaryFilter = ft.IsPrimaryFilter,
                            ShowIfEmpty = ft.ShowIfEmpty,
                            SortOrder = ft.SortOrder,
                            DefaultValue = ft.DefaultValue,
                            Validation = new FieldTypeDescriptionApiViewModel_ValidationText {
                                IsRequired = ft.IsRequired,
                                Length = ft.Length,
                                MaximumLength = ft.MaximumLength,
                                Message = ft.ValidationDescription,
                                MinimumLength = ft.MinimumLength,
                                Pattern = ft.Pattern
                            }
                        };
                        break;
                }

                model.items.Add(ftModel);
            }

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model)));
        }


        /// <summary>
        /// Retrieves field types contained within your environment.
        /// </summary>
        /// <remarks>
        /// In addition to the below query parameters a field name for the asset type can be specified to filter by exact match. For example MyCustomField=someExactValue.
        /// </remarks>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(FieldTypesApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by AssetId.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_predicateUid", "The Uid of a predicate type to return relationships for. If specified the results will include relationships of this predicate type. Assets without this type of relationship defined will be omitted.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_subjectUid", "The Uid of the subject side of a relationship to filter by in addition to filtering by predicate type. _predicateUid is required.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetFieldTypesAsync()
        {
            var prefix = "Assets.GetAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var results = await GetFieldTypes(queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }
    }
}
