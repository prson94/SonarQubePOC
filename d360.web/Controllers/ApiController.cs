using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.helpers;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Linq;
using d360.core.resources;
using d360.model.DataAccessLayer;
using d360.web.Extensions;
using Resources;
using d360.core.Models;
using SmartFormat;
using d360.model.helpers;

namespace d360.web.Controllers
{
    [RoutePrefix("api"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class D3SApiController : BaseApiController
    {
        #region DI

        ICommentRepository commentsRepository;
        ISecurityContextProvider SecProvider;
        ITagRepository tagRepository;
        IConnectorLabelRepository connectorLabelRepository;
        IFieldsRepository fieldsRepository;

        public D3SApiController(ICoreComponentSet set, ICommentRepository comments, ITagRepository tagRepository, IConnectorLabelRepository connectorLabelRepository, ISecurityContextProvider secProvider, IFieldsRepository fieldsRepository)
            : base(set)
        {
#if DEBUG
            Company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
#endif
            SecProvider = secProvider;
            commentsRepository = comments;
            this.tagRepository = tagRepository;
            this.fieldsRepository = fieldsRepository;
            this.connectorLabelRepository = connectorLabelRepository;
        }

        #endregion

        #region Field Data

        async Task<List<DetailReadOnlyRowModel>> loadDynamicDisplayField(FieldType ft, List<FieldWithRelation> fields, ObjectDetail details, SystemObjects type, int id, List<LookupDataReadOnlyModel> lookupFieldData, ComplexRelationFieldHasAnyModel complexRelationFieldHasAnyModel)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var formattedValue = string.Empty;
            var value = string.Empty;
            var allowAllSelected = false;

            var k = fields.SingleOrDefault(i => i.FieldTypeID == ft.ID);
            if (k != null)
            {
                value = k.Value;
                if (value == "0" && ft.AllowAllValue && ft.Type == DataType.Lookup.ToString())
                {
                    formattedValue = ft.AllowAllLabel;
                    allowAllSelected = true;
                }
                else
                {
                    formattedValue = k.FormattedValue;
                }

            }
            else if (ft.Type == DataType.Counter.ToString())
            {
                value = formattedValue = Company.GetCounterFieldValue(ft.ID, details.AssetID.Value);
            }
            else
            {
                if (!string.IsNullOrEmpty(ft.DefaultFormattedValue))
                {
                    value = ft.DefaultValue;
                    formattedValue = ft.DefaultFormattedValue;
                }
            }

            if (ft.Type == DataType.Link.ToString())
            {
                var ro = new ReadOnlyField
                {
                    Name = ft.FriendlyName,
                    Value = value,
                    FieldDescription = ft.DisplayDescription,
                    FieldName = ft.Name,
                    ShowIfEmpty = ft.ShowIfEmpty,
                    DataType = ft.Type,
                    IsPartOfKey = ft.IsPartOfKey
                };

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro },
                    Category = ft.Category
                });
            }
            else if (!string.IsNullOrEmpty(formattedValue))
            {
                var ro = new ReadOnlyField
                {
                    Name = ft.FriendlyName,
                    Value = (ft.LookupDisplayFormat == formattedValue) ? "" : formattedValue,
                    FieldDescription = ft.DisplayDescription,
                    FieldName = ft.Name,
                    DataType = !string.IsNullOrEmpty(ft.Type) ? ft.Type : "",
                    ShowIfEmpty = ft.ShowIfEmpty,
                    IsPartOfKey = ft.IsPartOfKey
                };

                if (ft.Type == DataType.Date.ToString()) ro.DataType = "date";
                else if (ft.Type == DataType.DateTime.ToString()) ro.DataType = "datetime";
                else if (ft.Type == DataType.Boolean.ToString()) ro.DataType = "bool";

                if (!string.IsNullOrEmpty(ft.LookupObjectType) && ft.LookupObjectID.HasValue && !allowAllSelected)
                {
                    ro.Values = new List<ReadOnlyFieldValue>();
                    ro.Value = "values";
                    var items = ((!string.IsNullOrEmpty(value)) ? value.Split(',') : new string[] { });
                    var itemIds = new List<long>();
                    var isReference = ft.LookupObjectType == "ReferenceItem" || ft.LookupObjectType == "ReferenceItemType";
                    var tooltipContext = isReference ? TemplateAction.LookupPreview.ToString() : TemplateAction.Preview.ToString();
                    var lookupUrl = k?.LookupUrl;

                    foreach (var item in items)
                    {
                        if (long.TryParse(item, out long listId)) itemIds.Add(listId);
                    }

                    if (itemIds.Count > 0)
                    {
                        if (lookupFieldData.Count > 0)
                        {
                            foreach (var item in lookupFieldData)
                            {
                                string fieldValue = item.DisplayText;
                                var url = isReference && !string.IsNullOrEmpty(lookupUrl) ? lookupUrl : item?.Url ?? "";

                                if (!string.IsNullOrEmpty(item.ColorJson))
                                {
                                    ro.DataType = "color";
                                    var obj = JObject.Parse(item.ColorJson ?? "{}");
                                    fieldValue = $"[{{\"name\":\"{item.DisplayText}\", \"color\":\"{(string)obj["Value"] ?? "transparent"}\"}}]";
                                }

                                if (url.ToLower().IndexOf("referencelistid") > -1)
                                {
                                    url += "," + (item.uid != null ? item.uid.Value.ToString() : Guid.Empty.ToString());
                                }

                                ro.Values.Add(new ReadOnlyFieldValue
                                {
                                    TooltipContext = tooltipContext,
                                    TooltipID = item.Value,
                                    Value = fieldValue,
                                    TooltipType = ft.LookupObjectType,
                                    TooltipUrl = url,
                                    uid = (item.uid != null ? item.uid.Value : Guid.Empty),
                                    assetTypeUid = (item.assetTypeUid != null ? item.assetTypeUid.Value : Guid.Empty)
                                });
                            }
                        }
                    }

                }

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro },
                    Category = ft.Category
                });
            }
            else if (ft.Type == DataType.Path.ToString())
            {
                var assetPath = (await Company.QueryAsync<string>("select graph.GetPathByAssetId(@id, ' <i class=\"fa fa-angle-right\"></i> ', ' / ')", new { id = details.AssetID }).ConfigureAwait(false)).SingleOrDefault() + "";
                var ro = new ReadOnlyField
                {
                    Name = ft.FriendlyName,
                    Value = assetPath,
                    FieldDescription = ft.DisplayDescription,
                    FieldName = ft.Name,
                    ShowIfEmpty = ft.ShowIfEmpty,
                    DataType = ft.Type,
                    IsPartOfKey = ft.IsPartOfKey
                };

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro },
                    Category = ft.Category
                });
            }
            else if (ft.Type == DataType.Score.ToString())
            {
                var assetScore = (await Company.QueryFirstOrDefaultAsync<dynamic>(@"
select	case when S.Value is null then 
			' ' 
		else 
			cast(cast((round(S.[Value] * 100, 1)) as float) as nvarchar) + '%'
		end as [Value],
		case when cast((round(S.[Value] * 100, 1)) as float) < L.LowerThreshold then
			'poor'
		when cast((round(S.[Value] * 100, 1)) as float) between L.LowerThreshold and L.UpperThreshold then
			'average'
		when cast((round(S.[Value] * 100, 1)) as float) > L.UpperThreshold then
			'good'
		else
			'none'
		end as Threshold		
from	metrics.Score S
		inner join Asset A on A.uid = S.AssetUid and A.id = @id and S.EndDate is null
		inner join metrics.Allocation L on L.uid = S.AllocationUid and L.ScoreType = @scoreType and L.OverrideName is null"
                    , new { id = details.AssetID, ft.ScoreType }).ConfigureAwait(false));

                var ro = new ReadOnlyField
                {
                    Name = ft.FriendlyName,
                    Value = ((assetScore != null) ? JsonConvert.SerializeObject(new { name = assetScore.Value, Threshold = assetScore.Threshold }) : null),
                    FieldDescription = ft.DisplayDescription,
                    FieldName = ft.Name,
                    ShowIfEmpty = ft.ShowIfEmpty,
                    DataType = ft.Type,
                    IsPartOfKey = ft.IsPartOfKey
                };

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro },
                    Category = ft.Category
                });
            }
            else if (ft.Type == DataType.Tag.ToString())
            {
                list.AddRange(RenderTagField(ft, type, id));
            }
            else if (ft.Type == DataType.ComplexRelationLookup.ToString() || ft.Type == DataType.OwnershipLookup.ToString() || ft.Type == DataType.RefListRelationship.ToString())
            {
                //look at ownershiplookup / relationship lookup / reference list lookup field and figure out what to show
                list.AddRange(await RenderComplexLookupField(type.ToString(), id, ft, complexRelationFieldHasAnyModel).ConfigureAwait(false));
            }
            else if (ft.Type == DataType.Relationship.ToString() && !string.IsNullOrEmpty(ft.LookupObjectType) && ft.LookupObjectID.HasValue)
            {
                list.AddRange(await RenderRelationshipField(type.ToString(), id, ft).ConfigureAwait(false));
            }
            else if (ft.Type == DataType.JsonElement.ToString())
            {
                var jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(ft.Definition);
                var jsonElementValue = "";
                var jsonElementDataType = "Text";

                var jsonField = fields.SingleOrDefault(i => i.FieldTypeID == jsonElementDefinition.FieldTypeID);
                if (jsonField != null)
                {
                    var jsonElementProperty = await Company.QueryFirstOrDefaultAsync<FieldJsonProperty>("select * from FieldJsonProperty where FieldID = @ID and [Path] = @Path", new { jsonField.ID, jsonElementDefinition.Path });
                    if (jsonElementProperty != null)
                    {
                        jsonElementValue = jsonElementProperty.Value;
                    }
                }
                switch (jsonElementDefinition.DataType)
                {
                    case "date":
                    case "datetime":
                        jsonElementDataType = jsonElementDefinition.DataType;
                        DateTime jsonDate;
                        if (DateTime.TryParse(jsonElementValue, out jsonDate))
                        {
                            jsonElementValue = jsonDate.ToString("yyyy-MM-ddTHH:mm:ss\"Z\"");
                        }
                        break;
                    case "int":
                    case "bigint":
                    case "decimal":
                        jsonElementDataType = "number";
                        break;
                    case "bit":
                        jsonElementDataType = "bool";
                        break;
                }

                var ro = new ReadOnlyField
                {
                    Name = ft.FriendlyName,
                    Value = jsonElementValue,
                    FieldDescription = ft.DisplayDescription,
                    FieldName = ft.Name,
                    DataType = jsonElementDataType,
                    ShowIfEmpty = ft.ShowIfEmpty,
                    IsPartOfKey = ft.IsPartOfKey
                };

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro },
                    Category = ft.Category
                });
            }
            else if (ft.Type == DataType.FieldFromRelationship.ToString() && !string.IsNullOrEmpty(ft.LookupObjectType) && ft.LookupObjectID.HasValue && ft.LookupObjectFieldTypeID.HasValue)
            {
                var intersectTypeID = ft.LookupObjectID.Value;
                var fieldTypeID = ft.LookupObjectFieldTypeID.Value;
                var sType = type.ToString();
                var intersect = Company.Filter<Intersect>(i => i.IntersectTypeID == intersectTypeID && ((i.Subject == sType && i.SubjectID == id) || (i.Object == sType && i.ObjectID == id))).FirstOrDefault();

                string fieldValue = null;

                if (intersect != null)
                {
                    var isSubject = (intersect.Subject == sType && intersect.SubjectID == id);
                    var obj = isSubject ? intersect.Object : intersect.Subject;
                    var objID = isSubject ? intersect.ObjectID : intersect.SubjectID;

                    var rfld = (await Company.QueryAsync<string>(@"
declare @fieldValue nvarchar(max) = null,
		@type varchar(50) = '',
		@definition nvarchar(2500) = '[]',
        @assetId bigint
select @type = [Type], @definition = [Definition] from FieldType where ID = @fieldTypeID

if @type = 'JsonElement'
begin
	select	@fieldValue = P.Value
	from	openjson(@definition) with (FieldTypeID int '$.FieldTypeID', DataType varchar(50) '$.DataType', [Path] varchar(250) '$.Path') D
			left join Field F on F.FieldTypeID = D.FieldTypeID and [ObjectType] = @obj and ObjectID = @objID
			left join FieldJsonProperty P on P.FieldID = F.ID and P.[Path] = D.[Path]
end
else if @type = 'Path'
begin
    select  @assetId = ID from Asset where Object = @obj and ObjectID = @objId
    select	@fieldValue = graph.GetPathByAssetId(@assetId, ' <i class=""fa fa-angle-right""></i> ', ' / ')
end
else
begin
	select	@fieldValue = FormattedValue
	from	FieldDetail
	where	FieldTypeID = @fieldTypeID and [Object] = @obj and ObjectID = @objID
end
select @fieldValue", new { fieldTypeID, obj = new DbString() { Value = obj, IsAnsi = true, Length = 50 }, objID }).ConfigureAwait(false)).SingleOrDefault();

                    if (rfld != null)
                    {
                        fieldValue = rfld;
                    }
                }

                var ro = new ReadOnlyField
                {
                    Name = ft.FriendlyName,
                    Value = fieldValue,
                    FieldDescription = ft.DisplayDescription,
                    FieldName = ft.Name,
                    DataType = "Html",
                    ShowIfEmpty = ft.ShowIfEmpty,
                    IsPartOfKey = ft.IsPartOfKey
                };

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro },
                    Category = ft.Category
                });

            }
            else if (ft.ShowIfEmpty)
            {
                var ro = new ReadOnlyField
                {
                    Name = ft.FriendlyName,
                    Value = null,
                    FieldDescription = ft.DisplayDescription,
                    FieldName = ft.Name,
                    DataType = !string.IsNullOrEmpty(ft.Type) ? ft.Type : "",
                    ShowIfEmpty = ft.ShowIfEmpty,
                    IsPartOfKey = ft.IsPartOfKey
                };

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro },
                    Category = ft.Category
                });
            }

            return list;
        }

        async Task<List<DetailReadOnlyRowModel>> loadDynamicDisplayFields(SystemObjects type, int id)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var details = Company.GetObjectDetail(type.ToString(), id);
            if (details != null)
            {
                var fields = Company.GetFieldRelationsByObject(type, id).ToList();
                var fieldTypes = Company.Filter<FieldType>(i => i.Object == details.Type && i.ObjectID == details.TypeID && i.IsDisplayable).OrderBy(i => i.ColumnOrder).ToList();

                var lookupDataSql = $@"  select ft.id as FieldTypeId, 
                        trim(Val.Value) as Value, 
                        od.AssetId, 
                        od.Url, 
                        Color.ColorJson, 
                        flv.DisplayText, 
                        refAsset.uid,
                        refType.uid as assetTypeUid
                    from asset a
                    inner join fieldtype ft on ft.assettypeid = a.AssetTypeID
                    left join Field f on f.AssetID = a.ID and f.FieldTypeID = ft.ID
                    cross apply (
                        select * from STRING_SPLIT(f.Value,',')
                        union 
                        select DefaultValue from FieldType where id = ft.ID and isnull(f.value,'') = ''
                        )Val
                    outer apply utility.ObjectDetail(ft.LookupObjectType, trim(Val.Value))OD
                    left join Asset refAsset on refAsset.ID = od.AssetID
                    left join AssetType refType on refType.ID = od.AssetTypeId
                    outer apply dbo.GetAssetColorJsonByColor(refAsset.Color)Color
                    left join fieldlookupvalue flv on flv.fieldtypeid = ft.id and flv.value = trim(Val.Value)
                    where a.uid = @uid 
                    and (ft.LookupObjectType <> '' or ft.LookupObjectType is not null)
                    and (ft.LookupObjectID <> '' or ft.LookupObjectID is not null)
                    and Val.Value is not null and Val.value <> '';";

                var relationLookupDataSql = $@"
                        select ftl.* from fieldtype ft
                        inner join FieldTypeLookup ftl on ftl.FieldTypeID = ft.id
                        where ft.assettypeid = @AssetTypeID
                        and ft.type ='ComplexRelationLookup';";

                var dataReader = await Company.QueryMultipleAsync(lookupDataSql + relationLookupDataSql, new { uid = details.UID, details.AssetTypeID });

                var lookupData = dataReader.Read<LookupDataReadOnlyModel>().ToList();
                var fieldTypeLookups = dataReader.Read<FieldTypeLookup>().ToList();

                List<ComplexRelationFieldHasAnyModel> complexRelationFieldHasAnyModels
                    = await RelationshipLookupsHasValueResolver(details, fieldTypes, fieldTypeLookups);

                foreach (var ft in fieldTypes)
                {
                    var listData = lookupData.Where(x => x.FieldTypeId == ft.ID).ToList();
                    var complexRelationModel = complexRelationFieldHasAnyModels.FirstOrDefault(x => x.FieldTypeId == ft.ID);
                    list.AddRange(await loadDynamicDisplayField(ft, fields, details, type, id, listData, complexRelationModel).ConfigureAwait(false));
                }
            }

            return list;
        }
        ///<summary>
        ///This method does bulk check of all complex relation lookup fields if there are any values to render
        ///</summary>
        private async Task<List<ComplexRelationFieldHasAnyModel>> RelationshipLookupsHasValueResolver(ObjectDetail details, List<FieldType> fieldTypes, List<FieldTypeLookup> fieldTypeLookups)
        {
            var complexRelationFieldHasAnyModels = new List<ComplexRelationFieldHasAnyModel>();
            try
            {
                complexRelationFieldHasAnyModels = ComplexFieldsHelper.GetComplexRelationFieldHasAnyModels(fieldTypeLookups, fieldTypes);
                string multiSql = "";
                var complexModels = complexRelationFieldHasAnyModels.Where(x => !string.IsNullOrEmpty(x.SQL)).ToList();
                complexModels.ForEach(x => multiSql += x.SQL);
                var relationLookupHasAnyReader = await Company.QueryMultipleAsync(multiSql, new { assetUid = details.UID });
                foreach (var item in complexModels)
                {
                    var data = relationLookupHasAnyReader.Read<int>().FirstOrDefault();
                    item.HasAny = data > 0;
                }
            }
            catch (Exception ex)
            {
                //Add exception to Azure instead of throwing an error and not showing detail page
                //We needs this to preserve old behavior where invalid configuration of relation lookup fields was reported to azure
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", "ApiController.RelationshipLookupsHasValueResolver" },
                    { "SQL Satetment", $"Asset UID: {details.UID}, {Company.CurrentResourceID}" }
                });

                complexRelationFieldHasAnyModels.ForEach(x => x.NeedsFullCheck = true);
            }

            return complexRelationFieldHasAnyModels;
        }

        private List<ReadOnlyFieldValue> GetTagsValues(SystemObjects type, int id)
        {
            List<ReadOnlyFieldValue> tagsFields = new List<ReadOnlyFieldValue>();
            var asset = Company.Assets.SingleOrDefault(x => x.Object == type.ToString() && x.ObjectID == id);
            var tags = tagRepository.GetTagsForAsset(asset.ID);
            tags.ToList().ForEach(x =>
            {
                var roField = new ReadOnlyFieldValue
                {
                    Value = x.Value,
                    TooltipType = "tag",
                    TooltipID = x.ID,
                    CreatedBy = x.CreatedBy,
                    TooltipContext = "Preview",
                    TooltipUrl = "",
                    uid = x.uid
                };
                tagsFields.Add(roField);
            });

            return tagsFields;
        }

        private async Task<IEnumerable<AssetWithoutReadPermission>> GetObjectsWithoutReadAccess(List<BasicAsset> objectsToCheckAccesFor)
        {
            if (objectsToCheckAccesFor == null || objectsToCheckAccesFor.Count == 0) return new List<AssetWithoutReadPermission>();

            Dapper.DynamicParameters dbParams = new DynamicParameters();

            var dynamicSql = "";
            var indx = 0;
            foreach (var item in objectsToCheckAccesFor)
            {
                if (indx != 0) dynamicSql += " or ";

                dynamicSql += $"[object] = @obj{indx} and [objectid] = @objId{indx}";

                dbParams.Add($"obj{indx}", item.ObjectName, System.Data.DbType.AnsiString, size: 50);
                dbParams.Add($"objId{indx}", item.ObjectID);

                indx++;
            }

            var sql = $@"select [Object], ObjectID from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @resId and ({dynamicSql})";

            dbParams.Add("resId", Company.CurrentResourceID);

            return (await Company.QueryAsync<AssetWithoutReadPermission>(sql, dbParams)).ToList();
        }

        #endregion

        #region Grid Definition Methods

        decimal calculateDynamicColumnWidth(int remainingWidth, int dynamicFieldCount)
        {
            if (dynamicFieldCount > 0)
                return Math.Round((decimal)(remainingWidth / dynamicFieldCount), 0);
            else
                return 0;
        }

        GridColumn getGridColumnForColumn(FieldType item, decimal dynamicFieldWidth, bool serverPaged, bool loadLookupList = true, bool useNameAsDataField = false)
        {
            string cellsFormat = "";
            string columnType = GridColumn.COLUMN_TYPE_STRING;
            string filterType = GridColumn.FILTER_TYPE_STRING;
            List<string> filterItems = new List<string>();

            bool canHaveMultipleFilterItems = false;
            var columnDataType = item.Type;

            if (columnDataType == DataType.JsonElement.ToString())
            {
                FieldTypeDefinition_JsonElement jsonElementDefinition = null;
                jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(item.Definition);
                columnDataType = jsonElementDefinition.DataType;
                switch (columnDataType)
                {
                    case "bit":
                        columnDataType = "Boolean";
                        break;
                    case "date":
                        columnDataType = "Date";
                        break;
                    case "datetime":
                        columnDataType = "DateTime";
                        break;
                    case "decimal":
                        columnDataType = "Decimal";
                        break;
                    case "int":
                    case "bigint":
                        columnDataType = "Number";
                        break;
                    default:
                        columnDataType = "Text";
                        break;
                }
            }

            switch (columnDataType)
            {
                case "":
                case "Lookup":
                    if (loadLookupList)
                    {
                        if (item.LookupObjectType == "Resource" && HideData3SixtyUsers())
                        {

                            filterItems = Company.Query<string>(@"
                                select V.Text
                                from FieldLookupValue V
                                inner join reporting.Global_resource R on R.ResourceID = V.Value and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com and R.Email not like '%@precisely.com'
                                where V.LookupObjectType = @lookupObjectType and V.LookupObjectID = @lookupObjectId
                                order by V.Text", new { lookupObjectId = item.LookupObjectID, lookupObjectType = item.LookupObjectType })
                                .ToList();

                        }
                        else if (item.AllowMultipleValues)
                        {
                            filterItems = Company
                                .Filter<FieldLookupValue>(o => o.FieldTypeID == item.ID &&
                                                                o.LookupObjectType == item.LookupObjectType &&
                                                                o.LookupObjectID == item.LookupObjectID)
                                .OrderBy(o => o.Text)
                                .Select(o => o.Text + "!~!" + o.Text)
                                .ToList();
                        }
                        else
                        {
                            filterItems = Company
                                .Filter<FieldLookupValue>(o => o.FieldTypeID == item.ID &&
                                                                o.LookupObjectType == item.LookupObjectType &&
                                                                o.LookupObjectID == item.LookupObjectID)
                                .OrderBy(o => o.Text)
                                .Select(o => o.Text)
                                .ToList();
                        }

                    }

                    columnType = GridColumn.COLUMN_TYPE_DROPDOWN;
                    filterType = serverPaged ? GridColumn.FILTER_TYPE_LIST : GridColumn.FILTER_TYPE_CHECKEDLIST;
                    break;
                case "Date":
                    cellsFormat = "MM/dd/yyyy";
                    columnType = GridColumn.COLUMN_TYPE_DATE;
                    filterType = serverPaged ? GridColumn.FILTER_TYPE_DATE : GridColumn.FILTER_TYPE_RANGE;
                    break;
                case "DateTime":
                    cellsFormat = "MM/dd/yyyy HH:mm:ss";
                    columnType = GridColumn.COLUMN_TYPE_DATE;
                    filterType = serverPaged ? GridColumn.FILTER_TYPE_DATE : GridColumn.FILTER_TYPE_RANGE;
                    break;
                case "Number":
                    cellsFormat = "n";
                    columnType = GridColumn.COLUMN_TYPE_NUMBER;
                    filterType = GridColumn.FILTER_TYPE_NUMBER;
                    break;
                case "Decimal":
                    cellsFormat = "d4";
                    columnType = GridColumn.COLUMN_TYPE_NUMBER;
                    filterType = GridColumn.FILTER_TYPE_NUMBER;
                    break;
                case "Boolean":
                    columnType = GridColumn.COLUMN_TYPE_CHECKBOX;
                    filterType = GridColumn.FILTER_TYPE_LIST;
                    filterItems = new List<string> { "True", "False" };
                    break;
                case "Tag":
                    canHaveMultipleFilterItems = true;
                    break;
            }

            var width = item.ColumnWidth;
            if (!width.HasValue)
            {
                width = (int)dynamicFieldWidth;
            }
            var gc = new GridColumn { text = item.FriendlyName, datafield = useNameAsDataField ? $"{item.Name}" : $"Field{item.ID}", columntype = columnType, filtertype = filterType, filteritems = filterItems, cellsformat = cellsFormat, columnWidth = width, parentFieldTypeID = item.ParentFieldTypeID, canHaveMultipleFilters = canHaveMultipleFilterItems, apiName = item.Name, fieldType = item.Type };
            if (!string.IsNullOrEmpty(item.Category))
            {
                gc.columngroup = item.Category.Replace(" ", "");
            }
            return gc;
        }

        string getGridFieldTypeForColumn(FieldType item)
        {
            string fieldType = "string";

            if (item.Type == DataType.JsonElement.ToString())
            {
                FieldTypeDefinition_JsonElement jsonElementDefinition = null;
                jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(item.Definition);
                var dt = jsonElementDefinition.DataType;
                switch (dt)
                {
                    case "bit":
                        fieldType = "bool";
                        break;
                    case "date":
                    case "datetime":
                        fieldType = "date";
                        break;
                    case "decimal":
                    case "int":
                    case "bigint":
                        fieldType = "number";
                        break;
                }
            }
            else
            {
                switch (item.Type)
                {
                    case "Date":
                        fieldType = "date";
                        break;
                    case "DateTime":
                        fieldType = "date";
                        break;
                    case "Number":
                        fieldType = "number";
                        break;
                    case "Decimal":
                        fieldType = "number";
                        break;
                    case "Boolean":
                        fieldType = "bool";
                        break;
                    case "Html":
                        fieldType = "html";
                        break;
                    case "Link":
                        fieldType = "html";
                        break;
                    case "Path":
                        fieldType = "path";
                        break;
                    case "Tag":
                        fieldType = "tag";
                        break;
                    case "Score":
                        fieldType = "score";
                        break;
                    case "Lookup":
                        var lookupType = item.LookupObjectType == "ReferenceItem" ? "ReferenceItemType" : item.LookupObjectType;
                        var foundColorOnList = Company.Assets.Any(x => x.Color != null && x.AssetType.Object == lookupType && item.LookupObjectID == x.AssetType.ObjectID);
                        if (foundColorOnList) fieldType = "ListColor";
                        break;
                    case "OwnershipLookup":
                        fieldType = "ownershiplookup";
                        break;
                }
            }

            return fieldType;
        }

        GridField getGridFieldForColumn(FieldType item, bool useNameAsDataField = false)
        {
            return new GridField { name = useNameAsDataField ? $"{item.Name}" : $"Field{item.ID}", type = getGridFieldTypeForColumn(item), apiName = item.Name };
        }

        void parseDynamicColumnsAndFields(List<FieldType> items, List<GridColumn> columns, List<GridField> fields, decimal dynamicFieldWidth, bool serverPaged = false)
        {
            items.ForEach(i =>
            {
                columns.Add(getGridColumnForColumn(i, dynamicFieldWidth, serverPaged, false));

                fields.Add(getGridFieldForColumn(i));
            });
        }

        void parseDynamicFilterFields(List<FieldType> items, List<GridFilterColumn> columns, decimal dynamicFieldWidth, bool hiddenField)
        {
            items.ForEach(i =>
            {
                GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, dynamicFieldWidth, true, false));

                col.id = i.ID.ToString();
                col.hiddenfield = hiddenField;
                col.parentFieldTypeID = i.ParentFieldTypeID;

                columns.Add(col);

            });
        }

        [HttpGet, Route("{type}/{uid}/grid/definition")]
        public HttpResponseMessage GetGridDefinitionByType(SystemObjects type, string uid)
        {
            if (!Guid.TryParse(uid, out var guid))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ApiMessages.CustomUidNotValid);
            }

            int objectId = Company.GetObjectId(guid, type);
            return GetGridDefinitionByType(type, objectId);
        }

        [HttpGet, Route("{type}/{id:int}/grid/definition")]
        public HttpResponseMessage GetGridDefinitionByType(SystemObjects type, int id)
        {
            var sType = type.ToString();
            var skippedFieldTypes = DataType.Text.GetNonlistableFields();

            var totalItems = Company
                .Filter<FieldType>(i => i.Object == sType && i.ObjectID == id && !skippedFieldTypes.Contains(i.Type))
                .ToList();

            var items = totalItems.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

            var columns = new List<GridColumn>();
            var fields = new List<GridField>();
            var filterColumns = new List<GridFilterColumn>();
            var topLevelFilterFields = new List<GridFilterColumn>();
            decimal dynamicFieldWidth = 0;
            int remainingWidth = 0;
            ObjectDetail detail = null;
            bool isReadOnly = false;


            var scoreAllocations = Company.Query<dynamic>(@"
                select FT.[Name], FT.ScoreType, A.LowerThreshold, A.UpperThreshold  from FieldType FT
                inner join AssetType T on T.Id = FT.AssetTypeID
                inner join metrics.Allocation A on A.AssetTypeUid = T.[uid] and A.[State] = 1 and A.ScoreType = FT.ScoreType
                where FT.[Object] = @type and FT.ObjectID = @id and FT.[Type] = 'Score'", new { type = new DbString { Value = type.ToString(), IsAnsi = true, Length = 50 }, id }).ToList();

            var hasProfiling = Company.Query<bool>("select case when exists (select 1 from AssetDataProfile P inner join AssetWithType A on A.ID = P.AssetID where A.Type = @type and A.TypeID = @id) then 1 else 0 end", new { type = new DbString { Value = type.ToString(), IsAnsi = true, Length = 50 }, id }).SingleOrDefault();

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    #region
                    bool showParent = true;
                    var assetType = Company.Filter<AssetType>(x => x.Object == type.ToString() && x.ObjectID == id).FirstOrDefault();
                    if (assetType != null)
                    {
                        showParent = assetType.AutoDisplayParent.HasValue ? (bool)assetType.AutoDisplayParent : true;
                    }

                    var hasParentType = Company.TypeHasParent(SystemObjects.ArtifactType, id);
                    parseDynamicColumnsAndFields(items, columns, fields, 0, true);

                    if (hasParentType && showParent)
                    {
                        columns.Insert(1, new GridColumn
                        {
                            text = d360.core.resources.Fields.Parent_Name,
                            datafield = "Parent",
                            columntype = GridColumn.COLUMN_TYPE_DROPDOWN,
                            filtertype = GridColumn.FILTER_TYPE_LIST,
                            filterable = true,
                            filteritems = new List<string>(),
                            columnWidth = 200
                        });
                    }


                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    if (hasParentType && showParent)
                    {
                        fields.Add(new GridField { name = "ParentID", type = "number" });
                        fields.Add(new GridField { name = "Parent", type = "string", apiName = "ParentDisplayName" });
                        fields.Add(new GridField { name = "ParentUrl", type = "string" });
                    }
                    fields.Add(new GridField { name = "Url", type = "string" });


                    filterColumns.AddRange(columns.Select(p => new GridFilterColumn(p)));

                    //clear the filtercolumns of the columns since they are not used and copied to the filtercolumns
                    foreach (var column in columns)
                    {
                        column.filteritems = new List<string>();
                    }

                    var hiddenItems = totalItems.Where(i => i.Type != "RelationLookup" && !i.IsListable).OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName).ToList();
                    parseDynamicFilterFields(hiddenItems, filterColumns, 0, true);

                    filterColumns = filterColumns.OrderBy(x => x.text).ToList();

                    //Load any field types that are top level filter fields
                    var topFiltersHidden = totalItems.Where(i => i.IsPrimaryFilter).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

                    topFiltersHidden.ForEach(i =>
                    {
                        GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, 0, true));

                        col.id = i.ID.ToString();
                        col.hiddenfield = !i.IsListable;

                        topLevelFilterFields.Add(col);

                    });

                    break;
                #endregion
                case SystemObjects.IntersectType:
                    #region

                    var intersectType = Company.GetById<IntersectType>(id);

                    if (intersectType != null && intersectType.Predicate != null)
                    {
                        isReadOnly = !intersectType.Predicate.Type.AsInfoModel().AllowEditFromRelationshipEditor;
                    }
                    var targetType = Request.GetQueryString("target");
                    var targetTypeID = Request.GetQueryString("targetID");

                    columns.Add(
                            new GridColumn { text = "Asset Path", datafield = "Name", columntype = GridColumn.COLUMN_TYPE_STRING, filtertype = GridColumn.FILTER_TYPE_STRING }
                    );


                    remainingWidth = 80;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "ObjectID", type = "number" });
                    fields.Add(new GridField { name = "Object", type = "string" });
                    fields.Add(new GridField { name = "TypeID", type = "number" });
                    fields.Add(new GridField { name = "Type", type = "string" });
                    fields.Add(new GridField { name = "TypeName", type = "string" });
                    fields.Add(new GridField { name = "Url", type = "string" });
                    break;
                #endregion                
                case SystemObjects.TaxonomyType:
                case SystemObjects.PolicyType:
                    #region
                    parseDynamicColumnsAndFields(items, columns, fields, 0, true);

                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "ParentID", type = "number" });
                    fields.Add(new GridField { name = $"{type}ID", type = "number" });
                    break;
                #endregion                                
                case SystemObjects.ReferenceItemType:
                    #region

                    remainingWidth = 85;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Code_Name, datafield = "Code" });
                    columns.Add(new GridColumn { text = "Color", datafield = "Color" });
                    var parentRefType = Company.GetParentType(id, SystemObjects.ReferenceItemType);
                    var loopCount = 0;
                    //add the parent columns
                    while (parentRefType != null && loopCount < 20)
                    {
                        columns.Insert(0, new GridColumn { text = parentRefType.Name, datafield = $"Rel{parentRefType.ObjectID}" });
                        fields.Add(new GridField { name = $"Rel{parentRefType.ObjectID}", apiName = "ParentDisplayName", type = "string" });
                        parentRefType = Company.GetParentType(parentRefType.ObjectID, SystemObjects.ReferenceItemType);
                        loopCount++;
                    }

                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Code", type = "string" });
                    fields.Add(new GridField { name = "Color", type = "Color" });
                    fields.Add(new GridField { name = "ReferenceItemType", type = "number" });
                    break;
                #endregion               
                case SystemObjects.RuleType:
                    #region

                    parseDynamicColumnsAndFields(items, columns, fields, 0, true);

                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "RuleTypeID", type = "number" });

                    filterColumns.AddRange(columns.Select(p => new GridFilterColumn(p)));

                    //clear the filtercolumns of the columns since they are not used and copied to the filtercolumns
                    foreach (var column in columns)
                    {
                        column.filteritems = new List<string>();
                    }

                    var hiddenItemsRuleType = totalItems.Where(i => i.Type != "RelationLookup" && !i.IsListable).OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName).ToList();
                    parseDynamicFilterFields(hiddenItemsRuleType, filterColumns, 0, true);

                    filterColumns = filterColumns.OrderBy(x => x.text).ToList();

                    //Load any field types that are top level filter fields
                    var topFiltersHiddenRuleType = totalItems.Where(i => i.IsPrimaryFilter).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

                    topFiltersHiddenRuleType.ForEach(i =>
                    {
                        GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, 0, true));

                        col.id = i.ID.ToString();
                        col.hiddenfield = !i.IsListable;

                        topLevelFilterFields.Add(col);

                    });

                    break;
                #endregion                  
                case SystemObjects.ResourceType:
                    #region

                    var queryParams = Request.GetQueryNameValuePairs();
                    bool iscommunityuserresposibility = false;

                    if (queryParams.Any(q => q.Key.ToLower() == "iscommunityuserresposibility"))
                    {
                        bool tempbool;
                        if (!bool.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "iscommunityuserresposibility").Value, out tempbool))
                        {
                            iscommunityuserresposibility = false;
                        }
                        else
                        {
                            iscommunityuserresposibility = tempbool;
                        }
                    }


                    if (!iscommunityuserresposibility)
                    {
                        remainingWidth = 27;
                        dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());
                        columns.Add(new GridColumn { text = Fields.FirstName_Name, datafield = "FirstName", fieldType = DataType.Text.ToString() });
                        columns.Add(new GridColumn { text = Fields.LastName_Name, datafield = "LastName", fieldType = DataType.Text.ToString() });
                        columns.Add(new GridColumn { text = Fields.Email_Name, datafield = "Email", fieldType = DataType.Text.ToString() });
                        parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth);
                        columns.Add(new GridColumn { text = Fields.LastLoggedInOn_Name, datafield = "LastLoggedInOn", filtertype = GridColumn.FILTER_TYPE_RANGE, cellsformat = "F", fieldType = DataType.DateTime.ToString() });
                        columns.Add(new GridColumn { text = "Administrator?", datafield = "IsAdministrator", columntype = GridColumn.COLUMN_TYPE_CHECKBOX, filtertype = GridColumn.FILTER_TYPE_CHECKBOX, fieldType = DataType.Boolean.ToString() });
                        columns.Add(new GridColumn
                        {
                            text = d360.core.resources.Fields.Status_Name,
                            datafield = "State",
                            filtertype = GridColumn.FILTER_TYPE_CHECKEDLIST,
                            fieldType = DataType.Text.ToString(),
                            filteritems = new List<string>() {
                            CompanyResourceState.Active.ToString(),
                            CompanyResourceState.Inactive.ToString(),
                        }
                        });
                        fields.Add(new GridField { name = "IsAdministrator", type = "bool", apiName = "IsAdministrator" });
                        fields.Add(new GridField { name = "ID", type = "number" });
                        fields.Add(new GridField { name = "Email", type = "string", apiName = "Email" });
                        fields.Add(new GridField { name = "FirstName", type = "string", apiName = "FirstName" });
                        fields.Add(new GridField { name = "LastName", type = "string", apiName = "LastName" });
                        fields.Add(new GridField { name = "LastLoggedInOn", type = "date", apiName = "LastLoggedInOn" });
                        fields.Add(new GridField { name = "State", type = "string", apiName = "State" });
                    }
                    else
                    {
                        remainingWidth = 27;
                        dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());
                        columns.Add(new GridColumn { text = "Name", datafield = "FirstName", fieldType = DataType.Text.ToString() });
                        columns.Add(new GridColumn { text = "Owned items", datafield = "OwnedItemCount", fieldType = DataType.Number.ToString() });
                        parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth);

                        fields.Add(new GridField { name = "FirstName", type = "string", apiName = "FirstName" });
                        fields.Add(new GridField { name = "OwnedItemCount", type = "string", apiName = "OwnedItemCount" });

                    }
                    break;
                    #endregion
            }

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Title = (detail != null) ? detail.PluralizedName : "Child Items",
                Type = type.ToString(),
                ID = id,
                FieldsCount = totalItems.Count,
                Fields = fields,
                Columns = columns,
                FilterColumns = filterColumns,
                TopLevelFilterColumns = topLevelFilterFields,
                IsReadOnly = isReadOnly,
                ScoreAllocations = scoreAllocations,
                HasProfiling = hasProfiling
            });
        }

        #endregion

        #region Navigation

        [Route("legacyuri/{obj}/{uid}")]
        public HttpResponseMessage GetLegacyUri(string obj, string uid)
        {
            var uri = "";

            Guid convertedUid;
            if (Guid.TryParse(uid, out convertedUid))
            {
                switch (obj)
                {
                    case "Asset":
                        uri = Company.Query<string>("declare @id bigint; select @id = ID from Asset where [Uid] = @convertedUid; select dbo.GenerateAssetUrl(@id);", new { convertedUid }).Single();
                        break;
                    case "AssetType":
                        uri = Company.Query<string>("declare @id int; select @id = ID from AssetType where [Uid] = @convertedUid; select dbo.GenerateAssetTypeUrl(@id);", new { convertedUid }).Single();
                        break;
                }
            }

            if (uri == null)
            {
                uri = "";
            }

            return Request.CreateResponse<string>(uri);
        }

        [Route("authenticationModel")]
        public HttpResponseMessage GetAuthenticationModel()
        {
            var c = Community.GetById<Company>(Company.CurrentCompanyID, i => i.CompanyDomainSettings);

            var authType = "sso";

            foreach (var settings in c.CompanyDomainSettings)
            {
                if (SecProvider.CompanyPrefix == settings.UrlPrefix)
                {
                    authType = settings.AuthenticationType == AuthenticationType.Forms ? "forms" : "sso";
                    break;
                }
            }

            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object>() {
                    { "model", authType },
                    { "prefix", SecProvider.CompanyPrefix }
                }
            );
        }

        #endregion

        #region Artifacts

        [Route("artifact/{id:int}")]
        public HttpResponseMessage GetArtifact(int id)
        {

            var json = Company.GetPageInformation(SystemObjects.Artifact, id);

            bool addModifySynonym = true;
            bool deleteSynonym = true;

            if (!Company.CurrentResourceIsAdmin)
            {
                string objectType = SystemObjects.Artifact.ToString();
                addModifySynonym = Company.HasAssetPermission(objectType, id, Permission.AddRelationships) || Company.HasAssetPermission(objectType, id, Permission.EditRelationships);
                deleteSynonym = Company.HasAssetPermission(objectType, id, Permission.DeleteRelationships);
            }

            var permission = new JObject();
            permission["addModifySynonym"] = addModifySynonym;
            permission["deleteSynonym"] = deleteSynonym;

            if (json == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, ApiMessages.ArtifactNotFound);
            }

            json.Add("SynonymPermission", permission);

            return Request.CreateResponse(HttpStatusCode.OK, json);
        }

        [Route("artifacts/{typeID:int}")]
        public Dictionary<string, object> GetArtifactType(int typeID)
        {

            var assetType = Company.Filter<AssetType>(i => i.Object == "ArtifactType" && i.ObjectID == typeID).SingleOrDefault();
            if (assetType == null) throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            var model = new Dictionary<string, object>();

            try
            {

                model.Add("ID", assetType.ObjectID);
                model.Add("Name", assetType.Name);
                model.Add("Description", assetType.Description);
                model.Add("ParentID", Company.GetParentType(assetType.ObjectID, SystemObjects.ArtifactType)?.ObjectID ?? null);
                model.Add("HasCustomExportTemplates", Company.AssetTypeExportTemplates.Where(x => x.AssetTypeID == assetType.ID).Any());
                model.Add("AutoDisplayDescription", assetType.AutoDisplayDescription);
                model.Add("Class", assetType.Class);
                model.Add("AutoDisplayParent", assetType.AutoDisplayParent);

                bool hasDashboards = Company.Filter<Report>(x => x.ObjectType == "ArtifactType" && x.ObjectID == typeID && x.ReportType != "legacy").Any();
                model.Add("HasDashboards", hasDashboards);

                var sql = $"select count(1) from [workflow].[EventRegistration] where [object] = 'ArtifactType' and [objectId] = {typeID}";

                var hasV2WorkflowsAssigned = (Company.Query<int>(sql).FirstOrDefault() > 0);
                model.Add("HasV2Workflows", hasV2WorkflowsAssigned);
                model.Add("AssetTypeUID", assetType.uid);
                model.Add("AssetTypeID", assetType.ID);

                var assetTypePath = Company.Query<string>(@"select p.path from assettype at cross apply dbo.GetAssetTypeTextPathById(at.id, ' > ') p where at.id = @atid", new { atid = assetType.ID }).FirstOrDefault();
                model.Add("AssetTypePath", assetTypePath);
            }
            catch (Exception ex)
            {
                SendException(ex, new Dictionary<string, string>());
                throw ex;
            }

            return model;

        }
        #endregion

        #region Followers

        [HttpGet, Route("followinfo/{type}/{uid}")]
        public dynamic GetFollowInfo(Guid uid, SystemObjects type)
        {
            int id = Company.GetObjectId(uid, type);
            return GetFollowInfo(id, type);
        }
        [HttpGet, Route("followinfo/{type}/{id:int}")]
        public dynamic GetFollowInfo(int id, SystemObjects type)
        {
            var following = Company.IsUserFollowing(type, id, null);
            var followParent = Company.GetFollowingParent(type, id, null);
            var followingParent = (followParent != null && followParent.FollowTypeID == FollowType.Parent);

            return new
            {
                isFollowing = following,
                isFollowingParent = followingParent,
                parent = followParent
            };

        }

        #endregion

        #region Groups

        [HttpGet, Route("groups")]
        public async Task<HttpResponseMessage> GetGroups()
        {
            string sql = $"select g.*, a.[uid] from [group] g inner join [asset] a on g.id = a.ObjectID and a.[object] = 'Group'";

            var results = await Company.QueryAsync<dynamic>(sql);
            var orderedResults = results.OrderBy(i => i.Name);
            return Request.CreateResponse(HttpStatusCode.OK, orderedResults);
        }

        [Route("{type}/{id:int}/groups")]
        public IQueryable<Group> GetGroupsByObject(SystemObjects type, int id)
        {
            return Company.Filter<ResourceGroup>(i => i.ResourceID == id, i => i.Group).Select(i => i.Group);
        }

        [Route("groups/{id:int}/resources")]
        public IQueryable<GroupResourceInfo> GetResourcesByGroup(int id)
        {
            return Company.Query<GroupResourceInfo>(
                QueryConstants.GroupResourceInfoList,
                new { id = id, userStatus = CompanyResourceState.Active }
                )
                .OrderBy(i => i.LastName)
                .ThenBy(i => i.FirstName)
                .AsQueryable();
        }

        #endregion

        #region Loads

        [HttpGet, Route("loads")]
        public IEnumerable<LoadDetail> GetLoads()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
            }

            return Company.GetLoadDetails();
        }

        [HttpGet, Route("loads/{id:int}/columns")]
        public IEnumerable<dynamic> GetLoadColumns(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
            }

            return Company.GetLoadColumnDetails(id);
        }

        [HttpGet, Route("loads/{id:int}/items")]
        public IEnumerable<dynamic> GetLoadItems(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
            }

            return Company.GetLoadItemDetails(id);
        }

        [HttpGet, Route("loads/{id:int}/uid")]
        public Guid GetLoadUid(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
            }

            var load = Company.GetById<Load>(id);

            return load.uid;
        }

        #endregion

        #region Lookup Methods

        [Route("lookups/{id:int}/allocations")]
        public IEnumerable<dynamic> GetAllocationsByLookupType(int id)
        {
            return Company.Query<dynamic>(QueryConstants.LookupAllocations, new { type = "Lookup", id });
        }

        [Route("lookup/list/{id:int}"), HttpGet]
        public HttpResponseMessage GetLookupList(int id)
        {
            var field = Company.FieldTypes.Find(id);

            var list = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == id && o.LookupObjectType == field.LookupObjectType && o.LookupObjectID == field.LookupObjectID.Value)
                             .OrderBy(o => o.Text)
                             .Select(i => new { value = i.Value.ToString(), label = i.Text })
                             .ToList();

            if (!field.IsRequired)
            {
                list.Insert(0, new { value = "", label = "" });
            }

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }

        #endregion

        #region Tag Fields

        private List<DetailReadOnlyRowModel> RenderTagField(FieldType ft, SystemObjects type, int id)
        {
            var list = new List<DetailReadOnlyRowModel>();

            list.Add(new DetailReadOnlyRowModel
            {
                columns = 1,
                FirstColumnFields = new List<ReadOnlyField> {
                    new ReadOnlyField {
                        Column = 1,
                        Name = ft.FriendlyName,
                        FieldDescription = ft.DisplayDescription,
                        FieldName = ft.Name,
                        ShowIfEmpty = true,
                        DataType = "tag",
                        Values = GetTagsValues(type, id),
                        IsPartOfKey = ft.IsPartOfKey
                    }
                },
                Category = ft.Category
            });

            return list;
        }

        #endregion

        #region Complex Lookup Fields

        private async Task<List<DetailReadOnlyRowModel>> RenderComplexLookupField(string type, int id, FieldType ft, ComplexRelationFieldHasAnyModel complexRelationFieldHasAnyModel)
        {
            var list = new List<DetailReadOnlyRowModel>();

            if (ft != null)
            {
                bool hasAnyGridValues;

                var lookup = complexRelationFieldHasAnyModel?.FieldTypeLookup;
                if (lookup == null)
                {
                    lookup = await Company.QueryFirstOrDefaultAsync<FieldTypeLookup>("select FieldTypeID, HideHeader, HideFooter, LookupType, Definition, HideFilter from FieldTypeLookup where FieldTypeID = @id", new { id = ft.ID });
                }

                if (complexRelationFieldHasAnyModel == null || complexRelationFieldHasAnyModel.NeedsFullCheck == true)
                {
                    hasAnyGridValues = await AnyComplexLookupGridValues(type, id, ft.ID);
                }
                else
                {
                    hasAnyGridValues = complexRelationFieldHasAnyModel.HasAny.Value;
                }

                if (hasAnyGridValues)
                {
                    bool isGrid = true;
                    if (ft.Type == DataType.OwnershipLookup.ToString() && !string.IsNullOrWhiteSpace(lookup.Definition))
                    {
                        FieldTypeOwnershipLookupDefinition lookupdefinition = JsonConvert.DeserializeObject<FieldTypeOwnershipLookupDefinition>(lookup.Definition);
                        if (lookupdefinition.DisplayAsList == true)
                        {
                            isGrid = false;
                        }
                    }
                    list.Add(new DetailReadOnlyRowModel
                    {
                        columns = 1,
                        FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField {
                                        Column = 1,
                                        Name = ft.FriendlyName,
                                        FieldDescription = ft.DisplayDescription,
                                        FieldName = ft.Name,
                                        HideHeader = (lookup != null) ? lookup.HideHeader : false,
                                        HideFooter = (lookup != null) ? lookup.HideFooter : false,
                                        HideFilter = (lookup != null) ? lookup.HideFilter : false,
                                        ComplexLookupType = isGrid ? ComplexLookupType.Grid : ComplexLookupType.List,
                                        LookupObjectID = id,
                                        LookupObjectType = type,
                                        LookupFieldTypeID = ft.ID,
                                        LookupType = (int)((DataType)Enum.Parse(typeof(DataType), ft.Type)),
                                        ShowIfEmpty = ft.ShowIfEmpty,
                                        DataType = ft.Type,
                                        IsPartOfKey = ft.IsPartOfKey
                                    }
                                },
                        Category = ft.Category
                    });
                }
                else if (ft.ShowIfEmpty)
                {

                    var ro = new ReadOnlyField
                    {
                        Name = ft.FriendlyName,
                        Value = "",
                        FieldDescription = ft.DisplayDescription,
                        FieldName = ft.Name,
                        Values = null,
                        DataType = !string.IsNullOrEmpty(ft.Type) ? ft.Type : "",
                        ShowIfEmpty = ft.ShowIfEmpty,
                        IsPartOfKey = ft.IsPartOfKey
                    };

                    list.Add(new DetailReadOnlyRowModel
                    {
                        columns = 1,
                        FirstColumnFields = new List<ReadOnlyField> { ro },
                        Category = ft.Category
                    });
                }
            }

            return list;
        }

        private async Task<bool> AnyComplexLookupGridValues(string type, int id, int fieldTypeId)
        {
            bool any = false;

            try
            {
                var qparams = Request.GetQueryNameValuePairs();
                var result = new Dictionary<string, object>();
                var asset = Company.Assets.FirstOrDefault(x => x.Object == type && x.ObjectID == id);

                var fieldType = Company.FieldTypes.FirstOrDefault(x => x.ID == fieldTypeId);

                List<FieldType> fields = fieldsRepository.GetFieldDefinitionForComplexLookupFieldType(fieldType, asset.uid);
                FieldTypeLookup ftl = Company.FieldTypeLookups.FirstOrDefault(x => x.FieldTypeID == fieldType.ID);

                List<dynamic> Values = new List<dynamic>();
                List<GridColumn> Columns = new List<GridColumn>();
                List<GridField> Fields = new List<GridField>();
                List<dynamic> scoringInfo = new List<dynamic>();

                int count = 0;
                var dbArgs = new DynamicParameters();

                dbArgs.Add("resourceId", Company.CurrentResourceID);
                dbArgs.Add("assetUid", asset.uid);
                dbArgs.Add("object", asset.Object);
                dbArgs.Add("objectId", asset.ObjectID);
                dbArgs.Add("fieldTypeId", fieldType.ID);

                if (fieldType.Type == "ComplexRelationLookup")
                {
                    (Columns, Fields, Values, count, scoringInfo) =
                       await fieldsRepository.GetComplexRelationLookupGrid(ftl, fields, dbArgs, "", "", "", "", countOnly: true);

                }

                if (fieldType.Type == "RefListRelationship")
                {
                    (Columns, Fields, Values, count) =
                       await fieldsRepository.GetRefListFromRelationshipGrid(fields, dbArgs, "", "", "", "", countOnly: true);
                }

                if (fieldType.Type == "OwnershipLookup")
                {
                    (Columns, Fields, Values, count) =
                       await fieldsRepository.GetOwnershipLookupGrid(ftl, fields, dbArgs, "", "", "", "", countOnly: true);
                }

                return count > 0;
            }
            catch (Exception ex)
            {
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", "ApiController.AnyComplexLookupGridValues" },
                    { "SQL Satetment", $"ComplexLookupByAsset '{type}', {id}, {fieldTypeId}, {Company.CurrentResourceID}, 1, 1" }
                });
            }

            return any;
        }

        /// <summary>
        /// Gets a list of available field-level filters for a grid in the UI. This includes standard/custom fields, relationships, attributes, owner.
        /// </summary>
        [Route("{type}/{id:int}/fieldfilters")]
        public async Task<IEnumerable<FieldFilterModel>> GetFieldFiltersByType(SystemObjects type, int id)
        {
            return await Company.GetFieldFiltersByType(type, id);
        }

        #endregion

        #region Relationships

        private async Task<List<DetailReadOnlyRowModel>> RenderRelationshipField(string type, int id, FieldType ft)
        {
            var list = new List<DetailReadOnlyRowModel>();
            var intersectTypeID = ft.LookupObjectID.Value;
            var sType = type.ToString();
            var values = new List<ReadOnlyFieldValue>();
            var intersects = Company.Filter<IntersectDetail>(i => i.IntersectTypeID == intersectTypeID && ((i.Subject == sType && i.SubjectID == id) || (i.Object == sType && i.ObjectID == id))).OrderBy(x => x.ObjectName);
            if (intersects == null)
            {
                return list;
            }

            //load the current users permissions to these objects if they dont have access we cant show the link to let them go nowhere
            var objectsToCheckAccesFor = new List<BasicAsset>();

            foreach (var intersect in intersects)
            {
                var isSubject = (intersect.Subject == sType && intersect.SubjectID == id);

                var obj = isSubject ? intersect.Object : intersect.Subject;
                var objID = isSubject ? intersect.ObjectID : intersect.SubjectID;

                objectsToCheckAccesFor.Add(new BasicAsset { ObjectID = objID, ObjectName = obj });
            }

            var objectsWithoutReadAccess = await GetObjectsWithoutReadAccess(objectsToCheckAccesFor);

            foreach (var intersect in intersects)
            {
                var isSubject = (intersect.Subject == sType && intersect.SubjectID == id);
                var intersectDisplayValue = isSubject ? intersect.ObjectName : intersect.SubjectName;
                var url = isSubject ? intersect.ObjectUrl : intersect.SubjectUrl;
                var obj = isSubject ? intersect.Object : intersect.Subject;
                var objID = isSubject ? intersect.ObjectID : intersect.SubjectID;

                if (obj == "Taxonomy")
                {
                    var det = await Company.QueryFirstOrDefaultAsync<string>("select tp.TextPath from  asset a cross apply GetAssetTextPathById(a.id, '/') tp where a.[Object] = 'Taxonomy' and a.ObjectID = @id", new { id = objID }).ConfigureAwait(false);
                    intersectDisplayValue = det;
                }

                if (objectsWithoutReadAccess != null && objectsWithoutReadAccess.Any(x => (x.Object == obj && x.ObjectID == objID)))
                {
                    url = null;
                }

                var relVal = new ReadOnlyFieldValue { Value = intersectDisplayValue, TooltipContext = "Preview", TooltipID = objID, TooltipType = obj, TooltipUrl = url };

                var assetUid = Company.Assets.Where(x => x.Object == obj && x.ObjectID == objID).Select(x => x.uid).FirstOrDefault();
                if (assetUid != null)
                {
                    relVal.uid = assetUid;
                    if (relVal.TooltipUrl.ToLower().IndexOf("referencelistid") > -1)
                    {
                        relVal.TooltipUrl += "," + assetUid.ToString();
                    }
                }
                values.Add(relVal);
            }

            values = values.Distinct(new ReadOnlyFieldValueComparer()).OrderBy(x => x.Value).ToList();

            var ro = new ReadOnlyField
            {
                Name = ft.FriendlyName,
                Value = values.Count > 0 ? "values" : "",
                FieldDescription = ft.DisplayDescription,
                FieldName = ft.Name,
                Values = values,
                ShowIfEmpty = ft.ShowIfEmpty,
                DataType = ft.Type,
                IsPartOfKey = ft.IsPartOfKey
            };

            list.Add(new DetailReadOnlyRowModel
            {
                columns = 1,
                FirstColumnFields = new List<ReadOnlyField> { ro },
                Category = ft.Category
            });

            return list;
        }

        [HttpGet, Route("RelationshipObjectsByType")]
        public async Task<IEnumerable<FilterObjectItem>> RelationshipObjectsByType(SystemObjects type, int id, int intersectTypeId)
        {
            var sql = "";

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    sql = @"select distinct disp.DisplayValue as Name, ASS.ObjectID as ID, 'Artifact' as [Type] , ASS.Uid
							from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'ArtifactType')                            
                            inner join [Intersect] I on ( (I.Subject = 'Artifact' and ASS.ObjectID = I.SubjectID and I.IntersectTypeID = @intersectTypeId)) 
							cross apply [dbo].GetAssetDisplayValueById(ASS.ID) disp
							union
							select distinct disp.DisplayValue as Name, ASS.ObjectID as ID, 'Artifact' as [Type] , ASS.Uid
                            from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'ArtifactType')     
                            inner join [Intersect] I on ( (I.Object = 'Artifact' and ASS.ObjectID = I.ObjectID and I.IntersectTypeID = @intersectTypeId) ) 
                            cross apply [dbo].GetAssetDisplayValueById(ASS.ID) disp
                            order by disp.DisplayValue";
                    break;
                case SystemObjects.IntersectType:
                    sql = @"select distinct iname.Name as Name, A.ID, 'Intersect' as [Type] , I.Uid
                            from [Intersect] A 
                            inner join [Intersect] I on A.IntersectTypeID = @id and ( (I.Subject = 'Intersect' and A.ID = I.SubjectID) OR (I.Object = 'Intersect' and A.ID = I.ObjectID) ) 
                            cross apply [dbo].getintersectNames(A.ID) iname
                            order by iname.Name";
                    break;
                case SystemObjects.PolicyType:
                case SystemObjects.Policy:
                case SystemObjects.TaxonomyType:
                    var ty = (type == SystemObjects.TaxonomyType ? "Taxonomy" : "Policy");
                    sql = $@"select distinct disp.TextPath as Name, ASS.ObjectID as ID, '{ty}' as [Type] , ASS.Uid
							from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = '{ty}Type')                            
                            inner join [Intersect] I on ( (I.Subject = '{ty}' and ASS.ObjectID = I.SubjectID)) and I.IntersectTypeID = @intersectTypeId
							cross apply [dbo].GetAssetTextPathById(ASS.ID,'/') disp
							union
							select distinct disp.TextPath as Name, ASS.ObjectID as ID, '{ty}' as [Type] , ASS.Uid
                            from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = '{ty}Type')     
                            inner join [Intersect] I on ( (I.Object = '{ty}' and ASS.ObjectID = I.ObjectID) ) and I.IntersectTypeID = @intersectTypeId
                            cross apply [dbo].GetAssetTextPathById(ASS.ID,'/') disp
                            order by disp.TextPath";
                    break;
                case SystemObjects.ReferenceItemType:
                    if (id != 0)
                    {
                        sql = @"select distinct AD.DisplayValue as Name, A.ID, 'ReferenceItem' as [Type] , A.uid
                            from Asset A 
                            inner join AssetType AST on AST.ID = A.AssetTypeID
                            inner join AssetDisplayValue AD on AD.AssetID =A.ID
                            inner join [Intersect] I on  ( (I.Subject = 'ReferenceItem' and A.ObjectID = I.SubjectID) OR (I.Object = 'ReferenceItem' and A.ObjectID = I.ObjectID) ) 
                            where AST.ObjectID= @id and AST.[Object]='ReferenceItemType' and I.IntersectTypeID = @intersectTypeId
                            order by AD.DisplayValue";
                    }
                    else
                    {
                        sql = @"select distinct Name, ID, 'ReferenceItemType' as [Type] , uid
                                from AssetType where Object ='ReferenceItemType'";
                    }
                    break;
                case SystemObjects.ResourceType:
                    sql = @"select distinct A.LastName + ', ' + A.FirstName as Name, A.ResourceID as ID, 'Resource' as [Type] , A.Uid
                            from reporting.Global_Resource A 
                            inner join [Intersect] I on ( (I.Subject = 'Resource' and A.ResourceID = I.SubjectID) OR (I.Object = 'Resource' and A.ResourceID = I.ObjectID) ) 
                            where I.IntersectTypeID = @intersectTypeId
                            order by 1";
                    break;
                case SystemObjects.RuleType:
                case SystemObjects.Rule:
                    sql = @"select distinct D.DisplayValue as Name, D.ObjectID as ID, D.Object as [Type], D.Uid
                            from AssetDetail D
                            inner join [Intersect] I on D.Object = 'Rule' and D.TypeID = @id and (I.Subject = 'Rule' and A.ID = I.SubjectID) and I.IntersectTypeID = @intersectTypeId
                            union
                            select distinct D.DisplayValue as Name, D.ObjectID as ID, D.Object as [Type], D.Uid
                            from AssetDetail D
                            inner join [Intersect] I on D.Object = 'Rule' and D.TypeID = @id and (I.Object = 'Rule' and A.ID = I.ObjectID) and I.IntersectTypeID = @intersectTypeId
                            order by D.DisplayValue";
                    break;
                case SystemObjects.TaskType:
                    sql = @"select distinct disp.DisplayValue as Name, ASS.ObjectID as ID, 'TaskType' as [Type] , ASS.Uid
							from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'TaskType')                            
                            inner join [Intersect] I on ( (I.Subject = 'Task' and ASS.ObjectID = I.SubjectID and I.IntersectTypeID = @intersectTypeId)) 
							cross apply [dbo].GetAssetDisplayValueById(ASS.ID) disp
							union
							select distinct disp.DisplayValue as Name, ASS.ObjectID as ID, 'TaskType' as [Type] , ASS.Uid
                            from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'TaskType')     
                            inner join [Intersect] I on ( (I.Object = 'Task' and ASS.ObjectID = I.ObjectID and I.IntersectTypeID = @intersectTypeId) ) 
                            cross apply [dbo].GetAssetDisplayValueById(ASS.ID) disp
                            order by disp.DisplayValue";
                    break;
            }

            if (string.IsNullOrEmpty(sql)) return null;

            return await Company.QueryAsync<FilterObjectItem>(sql, new { id, intersectTypeId });
        }

        /// <summary>
        /// Gets a list of available relationships types based on the source type specified in parameters. 
        /// Used in the Filter By Relationship tile on artifact list pages.
        /// </summary>
        [Route("{type}/{id:int}/relationshiptypes")]
        public async Task<IEnumerable<AllowedIntersectionType>> GetRelationshipTypes(SystemObjects type, int id)
        {
            return await Company.GetAllowedIntersectionTypes(type.ToString(), id);
        }

        [Route("relationships/{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteRelationship(int id)
        {
            var msg = new HttpResponseMessage();
            try
            {
                Company.DeleteRelationship(id);
                msg.StatusCode = HttpStatusCode.OK;
                msg.ReasonPhrase = RelationshipsApiMessages.RelationshipSucessfullyRemoved;
            }
            catch (SqlException ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.GetFullExceptionData());
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.GetFullExceptionData());
            }

            return msg;
        }

        [Route("relationships/field/{fieldTypeID:int}"), HttpGet]
        public HttpResponseMessage GetRelationshipFieldItems(int fieldTypeID, string @object = null, int? objectID = null, int offset = 0, int rows = 25, string query = null)
        {
            var selected = Company.GetRelationshipFieldItems(fieldTypeID, @object, objectID, offset, rows, query, true);

            if (selected.ContainsKey("RelationshipError"))
            {
                var errorMessage = Smart.Format(AssetTypeErrors.InvalidRelationshipFieldType, new
                {
                    FriendlyName = (string)selected["RelationshipError"]
                });

                return Request.CreateErrorResponse(HttpStatusCode.NotFound, errorMessage);
            }

            List<System.Web.Mvc.SelectListItem> selection = new List<System.Web.Mvc.SelectListItem>();

            if (selected.ContainsKey("Selection"))
            {
                List<dynamic> items = (List<dynamic>)selected["Selection"];
                int preselectedCount = items.Count;
                bool includeSelected = offset < preselectedCount;
                if (includeSelected)
                {
                    items.OrderBy(x => x.Text.ToString()).Skip(offset).Take(rows).ToList().ForEach(d =>
                     {
                         selection.Add(new System.Web.Mvc.SelectListItem { Text = d.Text, Value = d.Value.ToString(), Selected = true });
                     });
                }


                if (preselectedCount > 0)
                {
                    var targetOffset = offset - preselectedCount;

                    if (targetOffset < 0)
                    {
                        offset = 0;
                        rows = rows + targetOffset;
                    }
                    else
                    {
                        offset = targetOffset;
                    }
                }


            }
            Dictionary<string, object> result = null;
            if ((offset + rows) > 0)
            {
                List<string> excludeValues = selection.Select(s => s.Value).ToList(); //Exclude values already added to selection
                result = Company.GetRelationshipFieldItems(fieldTypeID, @object, objectID, offset, rows, query, false);
                if (result.ContainsKey("Items"))
                {
                    List<dynamic> items = (List<dynamic>)result["Items"];
                    items.ForEach(d =>
                    {
                        if (!excludeValues.Contains(d.Value.ToString()))
                            selection.Add(new System.Web.Mvc.SelectListItem { Text = d.Text, Value = d.Value.ToString(), Selected = false });
                    });
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                items = selection,
                count = (int)selected["Count"],
                hasCardinalityOne = (bool)selected["HasCardinalityOne"]
            });
        }

        #endregion

        #region Governance/Ownership/Responsibility

        [Route("resources/{resourceID:int}/ownership/{type}/{id:int}")]
        public IEnumerable<dynamic> GetResponsibilitiesByResourceByType(int resourceID, SystemObjects type, int id, int? responsibilityTypeId = null)
        {
            var sql = $@"
		select 
			RD.SecurityAsset,
		    RD.SecurityAssetID,
		    RD.SecurityAssetName,
		    RD.ResourceID,
		    RD.ResponsibilityTypeID,
		    T.Object as Type,
		    T.ObjectID as TypeID,
		    T.Name as TypeName,
		    A.Object,
		    A.ObjectID,
		    utility.GetAssetDisplayValueWrapper(A.ID) as ObjectName,
		    RD.ResponsibilityTypeName,
		    case RD.SecurityAsset
			    when 'G' then 'Via Group'
			    when 'O' then 'Via Organization'
			    else ''
		    end as Via,
            RD.Context 
		from 
		ResponsibilityDetail RD 
		inner join AssetType T on T.ObjectID = RD.TypeID and T.Object = RD.Type and T.Object = @type and T.ObjectID = @id
		inner join Asset A on A.AssetTypeID = T.ID
		where {(responsibilityTypeId.HasValue && responsibilityTypeId > 0 ? " ResponsibilityTypeID = @responsibilityTypeId and " : "")} 
            ResourceID = @resourceID and AssetID = 0 and ApplyToType = 1 and RD.IsVisible = 1
		
		union all

		select	RD.SecurityAsset,
		        RD.SecurityAssetID,
		        RD.SecurityAssetName,
		        RD.ResourceID,
		        RD.ResponsibilityTypeID,
		        RD.Type,
		        RD.TypeID,
		        T.Name as TypeName,
		        RD.Object,
		        RD.ObjectID,
		        utility.GetAssetDisplayValueWrapper(RD.AssetID) as ObjectName,
		        RD.ResponsibilityTypeName,
		        case RD.SecurityAsset
			        when 'G' then 'Via Group'
			        when 'O' then 'Via Organization'
			        else ''
		        end as Via,
                RD.Context
        from	ResponsibilityDetail RD
		        inner join AssetType T on T.Object = RD.Type and T.ObjectID = RD.TypeID and RD.ResourceID = @resourceID and T.Object = @type and T.ObjectID = @id
        where  {(responsibilityTypeId.HasValue && responsibilityTypeId > 0 ? " ResponsibilityTypeID = @responsibilityTypeId and " : "")} RD.AssetID != 0 
            and RD.ApplyToType = 0 and RD.IsVisible = 1
";


            return Company.Query<dynamic>(sql, new { type = new DbString { Value = type.ToString(), IsAnsi = true }, id, resourceID, responsibilityTypeId }).ToList();
        }


        [Route("groups/{groupID:int}/ownership/{type}/{id:int}")]
        public IEnumerable<dynamic> GetResponsibilitiesByGroupByType(int groupID, SystemObjects type, int id)
        {
            var sql = $@"
		select distinct
			RD.SecurityAsset,
		    RD.SecurityAssetID,
		    RD.SecurityAssetName,
		    RD.ResponsibilityTypeID,
		    T.Object as Type,
		    T.ObjectID as TypeID,
		    T.Name as TypeName,
		    A.Object,
		    A.ObjectID,
		    utility.GetAssetDisplayValueWrapper(A.ID) as ObjectName,
		    RD.ResponsibilityTypeName,
		    case RD.SecurityAsset
			    when 'G' then 'Via Group'
			    when 'O' then 'Via Organization'
			    else ''
		    end as Via,
            RD.Context 
		from 
		ResponsibilityDetail RD 
		inner join AssetType T on T.ObjectID = RD.TypeID and T.Object = RD.Type and T.Object = @type and T.ObjectID = @id
		inner join Asset A on A.AssetTypeID = T.ID
		where RD.SecurityAsset = 'G' and RD.SecurityAssetID = @groupID and AssetID = 0 and ApplyToType = 1 and RD.IsVisible = 1
		
		union all

		select	distinct
                RD.SecurityAsset,
		        RD.SecurityAssetID,
		        RD.SecurityAssetName,
		        RD.ResponsibilityTypeID,
		        RD.Type,
		        RD.TypeID,
		        T.Name as TypeName,
		        RD.Object,
		        RD.ObjectID,
		        utility.GetAssetDisplayValueWrapper(RD.AssetID) as ObjectName,
		        RD.ResponsibilityTypeName,
		        case RD.SecurityAsset
			        when 'G' then 'Via Group'
			        when 'O' then 'Via Organization'
			        else ''
		        end as Via,
                RD.Context
        from	ResponsibilityDetail RD
		        inner join AssetType T on T.Object = RD.Type and T.ObjectID = RD.TypeID and T.Object = @type and T.ObjectID = @id
        where  RD.AssetID != 0 
            and RD.ApplyToType = 0 and RD.IsVisible = 1
            and RD.SecurityAsset = 'G' and RD.SecurityAssetID = @groupID
";
            return Company.Query<dynamic>(sql, new { groupID, type = type.ToString(), id });
        }

        [Route("ownership/types")]
        public IQueryable<dynamic> GetResponsibilityTypes()
        {
            return Company.Table<ResponsibilityType>()
                .Select(i => new
                {
                    i.ID,
                    i.Name,
                    i.Description
                })
                .OrderBy(i => i.Name)
                .AsQueryable();
        }

        [Route("ownership/types/{id:int}/relations")]
        public List<ResponsibilityTypeRelationViewModel> GetResponsibilityTypeRelationsByResponsibilityType(int id)
        {
            var list = Company.Query<ResponsibilityTypeRelationViewModel>(@"
select  R.ResponsibilityTypeID,
        O.Name as ResponsibilityTypeName,
        D.[Class],
        P.[Path] as AssetTypeName, 
        D.ID as AssetTypeID,
        R.ObjectType,
        R.ObjectID,
        R.PermissionsBitMask
from    ResponsibilityTypeRelation R 
        inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID and O.ID = @id 
        left join AssetType D on D.Object = R.ObjectType and D.ObjectID = R.ObjectID
        cross apply dbo.GetAssetTypeTextPathById(D.ID, ' / ') P",
            new { id }).ToList().OrderBy(i => i.ClassName).ThenBy(i => i.AssetTypeName).ToList();

            list.ForEach(i =>
            {
                i.LoadPermissionsFromMask();
            });

            return list;
        }

        [Route("ownership/types/{id:int}/rules")]
        public IEnumerable<dynamic> GetRulesByResponsibilityType(int id)
        {
            return Company.Query<dynamic>(@"
select  R.ID,
        R.uid,
        R.ResponsibilityTypeID, 
        R.Name, 
        R.Context,
        D.Name as ObjectName, 
        O.Name as ResponsibilityType,
        O.uid as ResponsibilityTypeUid,
		R.LastRunOn 
from    ResponsibilityTypeRelationRule R 
        inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID and O.ID = @id 
        left join AssetType D on D.Object = R.Object and D.ObjectID = R.ObjectID",
            new { id });
        }

        [Route("ownership/{type}/{id:int}/responsibilitytypes")]
        public HttpResponseMessage GetResponsibilityTypesByObject(SystemObjects type, int id)
        {
            if (!Company.CurrentResourceIsAdmin) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));

            var sType = type.ToString();
            return Request.CreateResponse(HttpStatusCode.OK,
                Company.Filter<ResponsibilityTypeRelation>(i => i.ObjectID == id && i.ObjectType == sType, i => i.ResponsibilityType)
                .Select(i => new
                {
                    i.ResponsibilityTypeID,
                    i.ObjectID,
                    i.ObjectType,
                    i.ResponsibilityType.Name,
                    ResponsibilityTypeName = i.ResponsibilityType.Name,
                    Description = i.ResponsibilityType.Description ?? String.Empty
                })
                );
        }

        #endregion

        #region Policies

        [Route("policytypes/{id:int}")]
        public HttpResponseMessage GetPolicyType(int id)
        {
            var row = Company.Query<dynamic>(QueryConstants.PolicySettingsItem, new { id }).Single();
            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object>() {
                    { "ID", row.ObjectID },
                    { "Name", row.Name },
                    { "Description", row.Description },
                    { "NymTypes", Company.Query<dynamic>(QueryConstants.ObjectNymTypes, new { id = id, ot = new DbString {Value = "PolicyType", IsFixedLength = true, IsAnsi = true, Length = 50 } }) },
                    { "MaximumDepth", row.HierarchyMaximumDepth },
                    { "AssetTypeUID", row.Uid }
                }
            );
        }

        [Route("policytypes/{id:int}/policies")]
        public IEnumerable<dynamic> GetPoliciesByType(int id, bool stripHtml = false)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Policy", out joins, out columns, false, false, true, false, "A.ObjectID");

            var permissionSql = $@"case when exists (
                                        select 1 from UserAssetPermissions(@r, A.AssetTypeID) u where u.PermissionsBitMask & {(int)Permission.EditAsset} = {(int)Permission.EditAsset} and (u.AssetID = A.ID  or (u.AssetID = 0 and u.AssetTypeID = A.AssetTypeID))
						                ) 
						                    then 1
						                    else 0

                                        end as P_CanEdit,
		                                case when exists(
                                                             select 1 from UserAssetPermissions(@r, A.AssetTypeID) u where u.PermissionsBitMask & {(int)Permission.DeleteAsset} = {(int)Permission.DeleteAsset} and (u.AssetID = A.ID  or (u.AssetID = 0 and u.AssetTypeID = A.AssetTypeID))
						                                   ) 
						                                   then 1
						                                   else 0

                                        end as P_CanDelete";

            if (Company.CurrentResourceIsAdmin)
            {
                permissionSql = "1 as P_CanEdit, 1 as P_CanDelete";
            }

            var querySql = $@"
select	top 100 percent 
        A.ObjectID as ID, 
        A.[Uid],
        A.ID as AssetID, 
        P.SubjectID as ParentID,
        TD.DisplayValue,
        {columns}
		case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow,
        {permissionSql}
from	
        Asset A
        inner join AssetType ATT on ATT.ID = A.AssetTypeID and ATT.ObjectID = @id  and A.Object = 'Policy'
        {joins}         
        inner join dbo.AssetDisplayValue TD on TD.AssetID = A.ID
        outer apply (
					select	I.SubjectID
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = 'Policy' and I.ObjectID = A.ObjectID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
					) P
		cross apply (
					select	count(1) as [Count]
					from	workflow.EventRegistration WER
							inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 
					where	WER.Object = ATT.Object
							and WER.ObjectID = ATT.ObjectID
					) Work
where   A.ID not in ({Company.GetNoReadSqlStatement()})
        and A.AssetTypeID not in ({Company.GetAssetTypeNoReadSqlStatement()})";

            var sql = string.Format(@"select * from ({0}) A", querySql);

            sql = applyFilteringSuffix(sql, Request);

            sql += " order by A.DisplayValue";

            var policies = Company.Query<dynamic>(sql, new { id, r = Company.CurrentResourceID }).ToList();

            return policies;
        }

        [Route("PolicyType/{id:int}/levels")]
        public IQueryable<dynamic> GetPolicyTypeLevels(int id)
        {
            return Company.Query<dynamic>(@"Select AT.ObjectId as PolicyTypeID,ATL.Level,ATL.Name,ATL.Description
                                            From AssetTypeLevel ATL
                                            inner join AssetType AT on AT.Id = ATL.AssetTypeID
                                            WHERE  [object]='PolicyType' and ObjectId=@ObjectId
                                            order by Level", new { ObjectId = id }).AsQueryable();
        }


        #endregion

        #region Reports

        [Route("reports/targets")]
        public IEnumerable<dynamic> GetReportTargetAreas()
        {

            var items = Company.Query<dynamic>($@"
select      *
from        (                 
            select      'ArtifactType|' + cast(ObjectId as varchar(15)) as value,
                        'Business Asset : ' + Name as title
            from        AssetType where [object]='ArtifactType'  and [Class] = 1                       
            union
            select      'ArtifactType|' + cast(ObjectId as varchar(15)) as value,
                        'Technical Asset : ' + Name as title
            from        AssetType where [object]='ArtifactType'  and [Class] = 8                       
            union 
            select      'Artifact|' + cast(ObjectId as varchar(15)) as value,
                        'Business Asset Instance : ' + Name as title
            from       AssetType where [object]='ArtifactType' and [Class] = 1   
            union 
            select      'Artifact|' + cast(ObjectId as varchar(15)) as value,
                        'Technical Asset Instance : ' + Name as title
            from       AssetType where [object]='ArtifactType' and [Class] = 8  
            union
            select      'Resource|1' as value,
                        'Resource' as title
            union
            select      'Taxonomy|' + cast(ObjectId as varchar(15)) as value,
                        'Model Instance : ' + Name as title
            from         AssetType where [object]='TaxonomyType' 
            union
            select      'TaxonomyType|' + cast(ObjectId as varchar(15)) as value,
                        'Model Type : ' + Name as title
            from        AssetType where [object]='TaxonomyType' 
            union
            select      'Policy|' + cast(ObjectId as varchar(15)) as value,
                        'Policy Instance : ' + Name as title
            from         AssetType where [object]='PolicyType' 
            union
            select      'PolicyType|' + cast(ObjectId as varchar(15)) as value,
                        'Policy Type : ' + Name as title
            from        AssetType where [object]='PolicyType' 
            union
            select      'RuleType|' + cast(ObjectId as varchar(15)) as value,
                        'Rule Type : ' + Name as title
            from         AssetType where [object]='RuleType' 
            ) O
            order by    title
            ").ToList();

            return items;
        }

        #endregion

        #region Resources

        [HttpGet, Route("resources/{typeID:int}")]
        public HttpResponseMessage GetResourcesByType(int typeID, string filter = "", bool includeInactive = true)
        {
            var showUsers = SettingsRepository.GetSettingValue<bool>(Setting.ShowResources);
            //check that current user is an admin or the company settings allow users to be listed
            if (!Company.CurrentResourceIsAdmin && !showUsers)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

            var joins = "";
            var columns = "";
            var filterSql = "";
            getDynamicFieldJoinStatements(typeID, "Resource", out joins, out columns, false, false);

            var dbArgs = new DynamicParameters();
            dbArgs.Add("deleteStatus", CompanyResourceState.Deleted);
            dbArgs.Add("inactiveStatus", CompanyResourceState.Inactive);

            var statusCondition = $"State <> @deleteStatus";
            if (includeInactive == false)
            {
                statusCondition = $"State not in (@deleteStatus, @inactiveStatus)";
            }

            var querySql = $@"
select  A.FirstName,
		A.LastName,
        A.Email,
		A.LastLoggedInOn,
        case A.State when 1 then 'Active' when 2 then 'Inactive' else 'Deleted' end as [State],
        A.IsAdministrator,
        {columns}
		A.ID,
        A.ID as ResourceID,
        A.FirstName + ' ' + A.LastName as FullName 
from    (
        select	FirstName,
		        LastName,
                Email,
		        LastLoggedInOn,
                State,
                IsAdministrator,
                ResourceID as ID
        from	reporting.Global_Resource
                where {statusCondition} 
        ) A 
        {joins}";



            if (HideData3SixtyUsers())
            {
                querySql += " where (A.Email not like '%@data3sixty.com' and A.Email not like '%@infogix.com' and A.Email not like '%@precisely.com')";
            }

            if (!string.IsNullOrEmpty(filter))
            {
                filterSql = " where " + this.GetColumnsWithGlobalFilter(filter, SystemObjects.ResourceType, typeID, "B", dbArgs).Result;
            }

            var sql = string.Format(@"select * from ({0}) B {1} order by FirstName", querySql, filterSql);


            return Request.CreateResponse(HttpStatusCode.OK, Company.Query<dynamic>(sql, dbArgs));
        }


        private async Task<string> GetColumnsWithGlobalFilter(string filter, SystemObjects type, int id, string alias, DynamicParameters dbArgs)
        {
            string gridDefinitionString = await this.GetGridDefinitionByType(SystemObjects.ResourceType, id).Content.ReadAsStringAsync();
            dynamic gridDefinition = JsonConvert.DeserializeObject<dynamic>(gridDefinitionString);
            string wherecondition = string.Empty;


            for (int i = 0; i < gridDefinition.Fields.Count; i++)
            {
                var field = gridDefinition.Fields[i];
                var fieldName = field["name"].Value;
                switch (field["type"].Value)
                {
                    case "bool":
                        break;
                    case "number":
                        break;
                    case "string":
                        dbArgs.Add($"{fieldName}", $"%{filter}%");
                        wherecondition += $"or {alias}.{fieldName} Like @{fieldName} ";
                        break;

                }
            }

            return wherecondition.Remove(0, 2);
        }

        [Route("resources/{typeID:int}/{id:int}")]
        public Resource GetResource(int typeID, int id)
        {
            // See if user can see other users profiles by checking that current user is an admin or the company settings allow users to be listed.
            if (id != Company.CurrentResourceID)
            {
                if (!SettingsRepository.GetSettingValue<bool>(Setting.ShowResources))
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            //check that this user exists in this environment
            if (!Company.GlobalReportingResources.Where(x => x.ResourceID == id).Any())
            {
                // user is not a user of this environment get them outa here!
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            var model = Community.GetById<Resource>(id);

            if (model == null)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

            return model;
        }

        #endregion

        #region Rules

        [Route("ruletypes/{id:int}")]
        public HttpResponseMessage GetRuleType(int id)
        {
            var row = Company.Query<dynamic>(QueryConstants.RuleSettingsItem, new { id }).Single();

            int objectId = int.Parse(row.ID.ToString());
            var hasCustomExports = Company.AssetTypeExportTemplates.Any(x => x.AssetTypeID == objectId);

            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object>() {
                    { "ID", row.ObjectID },
                    { "Name", row.Name },
                    { "Description", row.Description },
                    { "HasCustomExportTemplates", hasCustomExports },
                    { "HasWorkflow", (bool)row.HasWorkflow },
                    { "NymTypes", Company.Query<dynamic>(QueryConstants.ObjectNymTypes, new { id = id, ot = new DbString {Value = "RuleType", IsFixedLength = true, IsAnsi = true, Length = 50 } }) },
                    { "HasDashboards",Company.Reports.Any(x=>x.ObjectID == id && x.ObjectType == SystemObjects.RuleType.ToString() && x.ReportType != "legacy") },
                    { "AssetTypeUID", row.uid }
                }
            );
        }
        #endregion

        #region Comment Tag Suggestions

        [HttpGet, Route("tagsuggestions")]
        public IEnumerable<TagSuggestionModel> TagSuggestions(string phrase, string excludeObjects = "")
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return new List<TagSuggestionModel>();

            Dapper.DynamicParameters dbParams = new DynamicParameters();

            var sql = @"select 
										c.[Object], 
										c.ObjectID, 
										AD.DisplayValue as TextPath, 
										cU.Url, 
										c.TypeName as ObjectTypeName, 
										c.ForeColor as IconForeColor, 
										c.BackColor as IconBackColor,
										case  when c.[Object] = 'Artifact' and c.AssetTypeClass = 1 then 'Business Asset'
										when c.[Object] = 'Artifact' and c.AssetTypeClass = 8 then 'Technical Asset'
										else	c.[Object] end [Displayobject],
                                        Uid as AssetUid
										from [dbo].AssetWithType c   
										inner join  AssetDisplayValue as AD   on
										AD.AssetID = C.ID
										cross apply [dbo].getAssetUrlById(c.ID) cU                              
										where (AD.DisplayValue like @beginsWith or (len(@val) > 2 and AD.DisplayValue like @contains))";

            dbParams.Add("beginsWith", $"{phrase}%");
            dbParams.Add("val", $"{phrase}%");
            dbParams.Add("contains", $"%{phrase}%");

            var tags = Company.Query<TagSuggestionModel>(sql, dbParams);

            return tags;
        }

        #endregion

        #region Type/ID Endpoints

        [Route("asset/{id:long}")]
        public AssetDetail GetAssetDetail(long id)
        {
            return Company.GetAssetDetail(id);
        }

        [Route("{type}/{uid}")]
        public ObjectDetail GetObjectDetail(SystemObjects type, Guid uid)
        {
            int id = Company.GetObjectId(uid, type);
            return GetObjectDetail(type, id);
        }

        [Route("{type}/{id:int}")]
        public ObjectDetail GetObjectDetail(SystemObjects type, int id)
        {
            return Company.GetObjectDetail(type.ToString(), id);
        }

        [Route("{type}/{id:int}/fieldName/{fieldName}/{useFriendlyName}")]
        public string GetObjectFieldColorAndValue(SystemObjects type, int id, string fieldName, bool useFriendlyName = true)
        {
            var objectDetail = Company.GetObjectDetail(type.ToString(), id);
            //check if there is a matching field for this type
            var fieldType = Company.FieldTypes.Where(x => x.Object == objectDetail.Type && x.ObjectID == objectDetail.TypeID && ((useFriendlyName && string.Compare(x.FriendlyName, fieldName, true) == 0) || (!useFriendlyName && string.Compare(x.Name, fieldName, false) == 0))).FirstOrDefault();

            if (fieldType == null || (!useFriendlyName && !fieldType.Name.Equals(fieldName)))
            {
                return null;
            }


            var sql = "select FormattedValue from field where objecttype = @obj and objectid = @id and fieldtypeid = @fieldId";
            if (fieldType?.LookupObjectType == SystemObjects.ReferenceItem.ToString() && !LookupFieldHasColorItem(fieldType))
            {
                var joincondition = " ";
                var joinconditionField = " ";
                var adddisitnct = " ";

                if ((bool)fieldType?.AllowMultipleValues)
                {
                    joincondition = $@" cross apply STRING_SPLIT(F.Value, ',') SPF ";
                    joinconditionField = "SPF.value";
                    adddisitnct = " distinct ";
                }
                else
                {
                    joinconditionField = "F.value";
                }

                sql = $@"select {adddisitnct} 
                            F.FormattedValue as name
                            , FD.FormattedValue as description
                            , FPL.FormattedValue as profilelevel
                        from 
                            field F
                            inner join FieldType ft on ft.ID = f.FieldTypeID
                            {joincondition}
							inner join Asset ACF on ACF.Object = ft.LookupObjectType and ACF.ObjectID = try_cast({joinconditionField} as int)   
                            outer apply (select FormattedValue from field FD1 inner join FieldType FT1 on FD1.FieldTypeID = FT1.ID where FT1.[Type]='{DataType.Text}' and FT1.FriendlyName='description' and FD1.ObjectID = try_cast({joinconditionField} as int) and FD1.AssetID=ACF.ID) FD
							outer apply (select FormattedValue from field FD2 inner join FieldType FT2 on FD2.FieldTypeID = FT2.ID where FD2.ObjectType='{SystemObjects.ReferenceItem}' and FT2.[Type]='{DataType.Text}' and FT2.FriendlyName='profile level' and FD2.ObjectID = try_cast({joinconditionField} as int) and FD2.AssetID=ACF.ID) FPL
                        where 
                            F.objecttype = @obj 
                            and F.objectid = @id 
                            and F.fieldtypeid = @fieldId FOR JSON PATH";
            }
            string value = Company.Query<string>(sql, new { obj = new DbString { Value = type.ToString(), IsFixedLength = true, Length = 20, IsAnsi = true }, id = id, fieldId = fieldType.ID }).FirstOrDefault();
            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fieldType.DefaultFormattedValue))
                value = $@"[{{""name"":""{fieldType.DefaultFormattedValue}""}}]";

            if (LookupFieldHasColorItem(fieldType))
            {
                string colorAndValueSql = $@"(SELECT TOP 1 F.FormattedValue as name,
								COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color
                                , FD.FormattedValue as description
                                , FPL.FormattedValue as profilelevel
                                from Field F 
								inner join FieldType ft on ft.ID = f.FieldTypeID
                                cross apply STRING_SPLIT(F.Value, ',') SPF
								inner join Asset ACF on ACF.Object = ft.LookupObjectType and ACF.ObjectID = SPF.value     
                                inner join Asset AI on AI.AssetTypeId = {objectDetail.AssetTypeID} and AI.ObjectID = f.ObjectID 
                                cross apply dbo.GetAssetColorJsonByColor(ACf.Color) ACJ
                                cross apply GetAssetDisplayValueByID(ACF.ID) ADV 
                                outer apply (select FormattedValue from field FD1 inner join FieldType FT1 on FD1.FieldTypeID = FT1.ID where FT1.[Type]='{DataType.Text}' and LOWER(FT1.FriendlyName)='description' and FD1.ObjectID = SPF.Value and FD1.AssetID=ACF.ID) FD
								outer apply (select FormattedValue from field FD2 inner join FieldType FT2 on FD2.FieldTypeID = FT2.ID where FD2.ObjectType='{SystemObjects.ReferenceItem}' and FT2.[Type]='{DataType.Text}' and LOWER(FT2.FriendlyName)='profile level' and FD2.ObjectID = SPF.Value and FD2.AssetID=ACF.ID) FPL
                                where f.FieldTypeID = {fieldType.ID} and f.[ObjectType] = '{type.ToString()}' and f.[ObjectID] = {id}) FOR JSON PATH";
                string colorAndValue = Company.Query<string>(colorAndValueSql).FirstOrDefault();
                if (!string.IsNullOrEmpty(colorAndValue))
                {
                    return colorAndValue;
                }
            }

            return value;
        }

        private bool LookupFieldHasColorItem(FieldType f)
        {
            if (f.LookupObjectType != null && f.LookupObjectID.HasValue)
            {
                var obj = f.LookupObjectType == "ReferenceItem" ? "ReferenceItemType" : f.LookupObjectType;
                if (obj != "ReferenceItemType")
                    return false;
                var assettype = Company.AssetTypes.FirstOrDefault(x => x.Object == obj && x.ObjectID == f.LookupObjectID);
                if (assettype != null)
                    return Company.Assets.Any(x => x.AssetTypeID == assettype.ID && x.Color != null);
            }
            return false;
        }
        [Route("{assetTypeId:int}/style")]
        public AssetTypeStyle GetAssetTypeStyle(int assetTypeId)
        {
            return Company.GetAssetTypeStyle(assetTypeId);
        }

        [Route("{type}/{objectId:int}/style")]
        public AssetTypeStyle GetAssetTypeStyle(SystemObjects type, int objectId)
        {
            return Company.GetAssetTypeStyle(type.ToString(), objectId);
        }

        [Route("{type}/{uid}/detail")]
        public async Task<DetailReadOnlyModel> GetObjectDetailFields(SystemObjects type, Guid uid, bool useSingleColumn = false, bool includeHeader = false, bool useAssetDetailColumnDefinition = false)
        {
            int objectId = -1;
            switch (type)
            {
                case SystemObjects.Tag:
                    objectId = Company.Tags.FirstOrDefault(x => x.uid == uid).ID;
                    return await GetObjectDetailFields(type, objectId, useSingleColumn, includeHeader);
                default:
                    var asset = Company.Assets.FirstOrDefault(a => a.uid == uid);

                    SystemObjects sysObject = (SystemObjects)Enum.Parse(typeof(SystemObjects), asset.Object, true);
                    return await GetObjectDetailFields(sysObject, asset?.ObjectID ?? -1, useSingleColumn, includeHeader, useAssetDetailColumnDefinition);
            }
        }


        [Route("{type}/{id:int}/detail")]
        public async Task<DetailReadOnlyModel> GetObjectDetailFields(SystemObjects type, int id, bool useSingleColumn = false, bool includeHeader = false, bool useAssetDetailColumnDefinition = false)
        {
            var model = new DetailReadOnlyModel() { columns = useSingleColumn ? 1 : 2 };
            model.Object = type.ToString();
            model.ObjectID = id;

            var metadata = Company.Query<dynamic>(@"
                    select  V.DisplayValue as AssetName, 
                            T.Name as AssetTypeName, 
                            T.Object as ObjectType, 
                            T.ObjectID as ObjectTypeID,
                            A.Uid as AssetUid,
                            A.ID as AssetID,
                            T.ID as AssetTypeID
                    from    Asset A 
                            inner join AssetDisplayValue V on V.AssetID = A.ID 
                            inner join AssetType T on T.ID = A.AssetTypeID 
                            outer apply dbo.UserAssetPermissions(@resourceId, T.ID) P 
                    where   A.ObjectID = @id and A.Object = @type", new { type = type.ToString(), id, resourceId = Company.CurrentResourceID }).FirstOrDefault();

            if (metadata != null)
            {
                var perms = Company.GetPermissions((long)metadata.AssetID, (int)metadata.AssetTypeID);

                if (perms.Any(x => x.ID == Permission.ReadResponsibilities) || perms.Count == 0 || Company.CurrentResourceIsAdmin)
                {
                    model.HasResponsibilityReadAccess = true;
                }
                else
                {
                    model.HasResponsibilityReadAccess = false;
                }

                model.CanEdit = true;
                if (!Company.CurrentResourceIsAdmin && !perms.Any(x => x.ID == Permission.EditAsset))
                {
                    model.CanEdit = false;
                }

                model.AssetUid = metadata.AssetUid;
                model.AssetID = metadata.AssetID;

                model.AssetName = metadata.AssetName;
                model.AssetTypeName = metadata.AssetTypeName;

                model.ObjectType = metadata.ObjectType;
                model.ObjectTypeID = metadata.ObjectTypeID;
            }

            if (includeHeader)
            {
                model.Scores = Company.Query<dynamic>(@"
                    select	*
                    from	(
		                    select  S.EffectiveDate,
				                    S.EndDate,
				                    S.RunDate,
				                    case 
					                    when AL.ScoreType = 1 then 'GV'
					                    when AL.ScoreType = 2 then 'DQ'
				                    end as ShortName,
				                    case 
					                    when AL.ScoreType = 1 then 'Governance'
					                    when AL.ScoreType = 2 then 'Data Quality'
				                    end as ScoreType,
				                    ROW_NUMBER() OVER(PARTITION BY AL.ScoreType ORDER BY S.EffectiveDate DESC) as RowNum,
				                    S.Value, 
				                    AL.LowerThreshold, 
				                    AL.UpperThreshold 
		                    from    metrics.Score S
				                    inner join Asset A on A.Uid = S.AssetUid and A.Object = @Object and A.ObjectID = @ObjectID and S.EffectiveDate <= @date 
				                    inner join metrics.Allocation AL on AL.Uid = S.AllocationUid
		                    ) O
                    where	O.RowNum = 1", new { model.Object, model.ObjectID, date = DateTime.UtcNow }).ToList();
            }

            FieldColumnMapper fcMapper = null;

            if (useAssetDetailColumnDefinition)
            {
                var fieldColumnMappings = (await Company.QueryAsync<FieldColumnMapping>(@"
                select ft.Name, DisplayInColumn, Category from asset a
                inner join fieldtype ft on ft.assettypeid = a.assettypeid
                where a.object = @type and a.objectid = @objectid
                and ft.isdisplayable = 1
                order by ColumnOrder", new { type = type.ToString(), objectid = id })).ToList();

                fcMapper = new FieldColumnMapper(fieldColumnMappings, model);
                fcMapper.TransformRowsAndCols();
            }

            switch (type)
            {
                case SystemObjects.Artifact:
                case SystemObjects.Rule:
                    #region Fields
                    {
                        var sType = type.ToString();
                        var asset = Company.Filter<Asset>(
                            x => x.ObjectID == id && x.Object == sType,
                            x => x.AssetType).FirstOrDefault();

                        if (asset != null)
                        {
                            if (type == SystemObjects.Rule)
                            {
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField> {
                                        new ReadOnlyField { Name = Resources.FieldInfo.RuleType_Name, FieldName = "AssetTypeName", FieldDescription = Resources.FieldInfo.RuleType_Description, Value = asset.AssetType.Name }
                                    },
                                    Category = Resources.FieldInfo.SystemFieldCategory
                                });
                            }

                            var dynamicRows = await loadDynamicDisplayFields(type, id).ConfigureAwait(false);

                            if (useAssetDetailColumnDefinition && fcMapper != null)
                            {
                                fcMapper.ArrangeRowsAndCols(dynamicRows);
                            }
                            else
                            {
                                model.rows.AddRange(dynamicRows);
                            }

                            if (type == SystemObjects.Artifact)
                            {
                                var parent = Company.GetParentObject(id, type);

                                if (parent != null)
                                {
                                    var parentAsset = Company.GetAssetDetail("Artifact", parent.ObjectID);
                                    var parentUrl = Company.Query<string>($"select dbo.GenerateAssetUrl({parentAsset.ID})").First();

                                    model.rows.Insert(1, new DetailReadOnlyRowModel
                                    {
                                        columns = 1,
                                        FirstColumnFields = new List<ReadOnlyField> {
                                        new ReadOnlyField {
                                            Name = Resources.FieldInfo.Parent_Name ,
                                            FieldName = "ArtifactParentName",
                                            FieldDescription = Resources.FieldInfo.Parent_Description,
                                            Value = parentAsset.DisplayValue,
                                            TooltipUrl = parentUrl,
                                            TooltipType="Artifact",
                                            TooltipContext="Preview",
                                            TooltipID = parent.ObjectID,
                                            Values = new List<ReadOnlyFieldValue>
                                            {
                                                new ReadOnlyFieldValue{
                                                    Value = parentAsset.DisplayValue,
                                                    uid = parentAsset.uid,
                                                    TooltipUrl = parentUrl,
                                                    TooltipType="Artifact",
                                                    TooltipContext="Preview",
                                                    TooltipID = parent.ObjectID,
                                                    assetTypeUid = parentAsset.AssetTypeUid.HasValue ? parentAsset.AssetTypeUid.Value : Guid.Empty
                                                }
                                            }
                                        }
                                    },
                                        Category = Resources.FieldInfo.SystemNoCategory
                                    });
                                }
                            }

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = FieldInfo.AssetId_Description, Value = asset.ID.ToString(), DataType = "string" }
                            },
                                Category = FieldInfo.SystemFieldCategory
                            });

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.Asset_UID_Name, FieldName = "AssetUid", FieldDescription = Resources.FieldInfo.Asset_UID_Description, Value = asset.uid.ToString(), DataType = "string" }
                            },
                                SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetType_UID_Name, FieldName = "AssetTypeUid", FieldDescription = Resources.FieldInfo.AssetType_UID_Description, Value = asset.AssetType.uid.ToString(), DataType = "string" }
                            },
                                Category = Resources.FieldInfo.SystemFieldCategory
                            });

                            if (asset.UpdatedOn.HasValue)
                            {
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 2,
                                    FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = FieldInfo.CreatedOn_Name, FieldName = "AssetCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = asset.CreatedOn.HasValue ? asset.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "", DataType = "date" }
                                },
                                    SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = FieldInfo.UpdatedOn_Name, FieldName = "AssetUpdatedOn", FieldDescription = Resources.FieldInfo.UpdatedOn_Description, Value = asset.UpdatedOn.GetValueOrDefault().ToString("yyyy-MM-ddTHH:mm:ssZ"), DataType = "date" }
                                },
                                    Category = Resources.FieldInfo.SystemFieldCategory
                                });
                            }
                            else
                            {
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "AssetCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = asset.CreatedOn.HasValue ? asset.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "", DataType = "date" }
                                },
                                    Category = Resources.FieldInfo.SystemFieldCategory
                                });
                            }

                            if (type == SystemObjects.Rule)
                            {
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 2,
                                    FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = FieldInfo.RuleID_Name, FieldName = "RuleID", Value = $"{asset.ObjectID}" }
                                },
                                    Category = Resources.FieldInfo.SystemFieldCategory
                                });
                            }
                        }
                    }
                    break;

                #endregion
                case SystemObjects.Task:
                    #region Fields
                    {
                        var asset = Company.Assets.FirstOrDefault(x => x.ObjectID == id && x.Object == SystemObjects.Task.ToString());

                        if (asset != null)
                        {
                            model.rows.AddRange(await loadDynamicDisplayFields(type, id).ConfigureAwait(false));

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = asset.ID.ToString(), DataType = "string" }
                            },
                                Category = Resources.FieldInfo.SystemFieldCategory
                            });

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.Asset_UID_Name, FieldName = "AssetUid", FieldDescription = Resources.FieldInfo.Asset_UID_Description, Value = asset.uid.ToString(), DataType = "string" }
                            },
                                SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetType_UID_Name, FieldName = "AssetTypeUid", FieldDescription = Resources.FieldInfo.AssetType_UID_Description, Value = asset.AssetType.uid.ToString(), DataType = "string" }
                            },
                                Category = Resources.FieldInfo.SystemFieldCategory
                            });

                            if (asset.UpdatedOn.HasValue)
                            {
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 2,
                                    FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "ArtifactCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = asset.CreatedOn.HasValue ? asset.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "", DataType = "date" }
                                },
                                    SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.UpdatedOn_Name, FieldName = "ArtifactUpdatedOn", FieldDescription = Resources.FieldInfo.UpdatedOn_Description, Value = asset.UpdatedOn.GetValueOrDefault().ToString("yyyy-MM-ddTHH:mm:ssZ"), DataType = "date" }
                                },
                                    Category = Resources.FieldInfo.SystemFieldCategory
                                });
                            }
                            else
                            {
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "ArtifactCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = asset.CreatedOn.HasValue ? asset.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "", DataType = "date" }
                                },
                                    Category = Resources.FieldInfo.SystemFieldCategory
                                });
                            }
                        }
                    }
                    break;

                #endregion
                case SystemObjects.ArtifactType:
                    #region Fields
                    var artifactType = Company.Filter<AssetType>(i => i.ObjectID == id && i.Object == "ArtifactType").SingleOrDefault();
                    if (artifactType != null)
                    {

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = Fields.Name_Name, FieldDescription = Fields.Name_Description, Value = artifactType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = Fields.ID_Name, FieldName = "ArtifactTypeID", FieldDescription = Fields.ID_Description, Value = artifactType.ID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name =Fields.Description_Name, FieldName = "ArtifactTypeDescription", FieldDescription =Fields.Description_Description, DataType = "Html", Value = string.IsNullOrEmpty(artifactType.Description) ? "None provided" : artifactType.Description }
                            }
                        });

                    }
                    break;
                #endregion
                case SystemObjects.Group:
                    #region Fields
                    var group = Company.GetById<Group>(id);
                    if (group != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = group.GetName(i => i.Name), FieldName = "GroupName", FieldDescription = group.GetDescription(i => i.Name), Value = group.Name }
                            }
                        });

                        if (group.PrimaryOwnerResourceID.HasValue || group.SecondaryOwnerResourceID.HasValue)
                        {
                            var groupOwnerIDs = new List<int>();
                            if (group.PrimaryOwnerResourceID.HasValue)
                            {
                                groupOwnerIDs.Add(group.PrimaryOwnerResourceID.Value);
                            }
                            if (group.SecondaryOwnerResourceID.HasValue)
                            {
                                groupOwnerIDs.Add(group.SecondaryOwnerResourceID.Value);
                            }

                            var groupOwners = GetCompanyResources().Where(i => groupOwnerIDs.Contains(i.ID)).ToList();
                            var row = new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = group.GetName(i => i.PrimaryOwnerResourceID), FieldName = "GroupOwner", FieldDescription = group.GetDescription(i => i.PrimaryOwnerResourceID), Value = groupOwners.Single(i => i.ID == group.PrimaryOwnerResourceID.Value).FormatDisplayName() }
                                }
                            };

                            if (group.SecondaryOwnerResourceID.HasValue)
                            {
                                row.SecondColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = group.GetName(i => i.SecondaryOwnerResourceID), FieldName = "GroupOwner", FieldDescription = group.GetDescription(i => i.SecondaryOwnerResourceID), Value = groupOwners.Single(i => i.ID == group.SecondaryOwnerResourceID.Value).FormatDisplayName() }
                                };
                            }


                            model.rows.Add(row);
                        }

                        if (useAssetDetailColumnDefinition || useSingleColumn)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Is Active Directory Group", FieldName = "IsActiveDirectoryGroup", FieldDescription = group.GetDescription(i => i.IsActiveDirectoryGroup), DataType = "Bool", Value = group.IsActiveDirectoryGroup.ToString() }
                                }
                            });
                        }

                        if (!string.IsNullOrEmpty(group.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = group.GetName(i => i.Description), FieldName = "GroupDescription", FieldDescription = group.GetDescription(i => i.Description), DataType = "Html", Value = group.Description }
                                }
                            });
                        }
                    }
                    group = null;
                    break;
                #endregion
                case SystemObjects.ExportTemplate:
                    #region Fields
                    var template = Company.AssetTypeExportTemplates.FirstOrDefault(x => x.ID == id);
                    if (template != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Name", FieldName = "Name", Value = template.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = "Include Asset Url", FieldName = "IncludeUrl", Value = template.IncludeUrl ? "Yes" : "No"}
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = "Asset Type", FieldName = "TypeName", Value = template.AssetType.Name}
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = "Include Parent Name", FieldName = "IncludeParent", Value = template.IncludeParent ? "Yes" : "No"}
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = "List Arrangement", FieldName = "Arrangement", Value = template.ExportViewType.ToString()}
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = "Has Template File", FieldName = "TemplateFile", Value = template.TemplateFile == null ? "No" : "Yes"}
                            }
                        });

                        if (!string.IsNullOrEmpty(template.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Descripition", FieldName = "Description", Value = template.Description }
                                }
                            });
                        }

                        if (!string.IsNullOrEmpty(template.UsageNotes))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Usage Notes", FieldName = "UsageNotes", Value = template.UsageNotes }
                                }
                            });
                        }
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "UID", Value = template.Uid.ToString() }
                                }
                        });
                    }
                    group = null;
                    break;
                #endregion
                case SystemObjects.FieldType:
                    #region Fields
                    var fieldType = Company.GetById<FieldType>(id);
                    if (fieldType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fieldType.GetName(i => i.Name), FieldName = "FieldTypeName", FieldDescription = fieldType.GetDescription(i => i.Name), Value = fieldType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fieldType.GetName(i => i.FriendlyName), FieldName = "FieldTypeFriendlyName", FieldDescription = fieldType.GetDescription(i => i.FriendlyName), Value = fieldType.FriendlyName }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fieldType.GetName(i => i.Type), FieldName = "FieldTypeType", FieldDescription = fieldType.GetDescription(i => i.Type), Value = fieldType.Type }
                            }
                        });

                        if (!string.IsNullOrEmpty(fieldType.Pattern))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fieldType.GetName(i => i.Pattern), FieldName = "FieldTypePattern", FieldDescription = fieldType.GetDescription(i => i.Pattern), Value = fieldType.Pattern }
                            }
                            });
                        }

                        var ftML = new DetailReadOnlyRowModel { columns = 2 };

                        if (fieldType.MinimumLength.HasValue)
                        {
                            ftML.FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fieldType.GetName(i => i.MinimumLength), FieldName = "FieldTypeMinimumLength", FieldDescription = fieldType.GetDescription(i => i.MinimumLength), Value = fieldType.MinimumLength.Value.ToString() }
                            };
                        }
                        if (fieldType.MaximumLength.HasValue)
                        {
                            ftML.SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fieldType.GetName(i => i.MaximumLength), FieldName = "FieldTypeMaximumLength", FieldDescription = fieldType.GetDescription(i => i.MaximumLength), Value = fieldType.MaximumLength.Value.ToString() }
                            };
                        }
                        model.rows.Add(ftML);

                        if (!string.IsNullOrEmpty(fieldType.LookupObjectType))
                        {
                            var ftLO = new DetailReadOnlyRowModel
                            {
                                columns = (fieldType.LookupObjectID.HasValue) ? 2 : 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Row = 5, Column = 1, Name = fieldType.GetName(i => i.LookupObjectType), FieldName = "FieldTypeLookupObjectType", FieldDescription = fieldType.GetDescription(i => i.LookupObjectType), Value = fieldType.LookupObjectType }
                                }
                            };
                            if (fieldType.LookupObjectID.HasValue)
                            {
                                ftLO.SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Row = 5, Column = 2, Name = fieldType.GetName(i => i.LookupObjectID), FieldName = "FieldTypeLookupObjectID", FieldDescription = fieldType.GetDescription(i => i.LookupObjectID), Value = fieldType.LookupObjectID.ToString() }
                                };
                            }
                            model.rows.Add(ftLO);


                            if (!string.IsNullOrEmpty(fieldType.LookupDisplayFormat))
                            {
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Row = 6, Column = 1, Name = fieldType.GetName(i => i.LookupDisplayFormat), FieldName = "FieldTypeLookupDisplayFormat", FieldDescription = fieldType.GetDescription(i => i.LookupDisplayFormat), Value = fieldType.LookupDisplayFormat }
                                }
                                });
                            }
                        }

                        if (!string.IsNullOrEmpty(fieldType.DisplayDescription))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Row = 7, Column = 1, Name = fieldType.GetName(i => i.DisplayDescription), FieldName = "FieldTypeDisplayDescription", FieldDescription = fieldType.GetDescription(i => i.DisplayDescription), DataType = "Html", Value = fieldType.DisplayDescription }
                            }
                            });
                        }

                        if (!string.IsNullOrEmpty(fieldType.FormDescription))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Row = 7, Column = 1, Name = fieldType.GetName(i => i.FormDescription), FieldName = "FieldTypeFormDescription", FieldDescription = fieldType.GetDescription(i => i.FormDescription), DataType = "Html", Value = fieldType.FormDescription }
                            }
                            });
                        }
                    }
                    fieldType = null;
                    break;
                #endregion                                
                case SystemObjects.Intersect:
                    #region Fields                    
                    var intersect = Company.GetById<Intersect>(id);
                    if (intersect != null)
                    {
                        model.columns = 1;
                        model.rows.AddRange(await loadDynamicDisplayFields(type, id).ConfigureAwait(false));
                    }
                    intersect = null;
                    break;
                #endregion
                case SystemObjects.IntersectType:
                    #region Fields
                    var intersectType = Company.GetById<IntersectType>(id);
                    if (intersectType != null)
                    {
                        model.columns = 1;
                        string intersectName = Company.GetIntersectTypeName(intersectType);

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = intersectName, FieldName = "IntersectTypeName", FieldDescription = "" , Value = intersectName }
                            }
                        });
                    }
                    intersectType = null;
                    break;
                #endregion
                case SystemObjects.Load:
                    #region Fields
                    var load = Company.GetLoadDetail(id);
                    if (load != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Action", FieldName = "LoadAction", FieldDescription = "", Value = load.Action }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Target", FieldName = "LoadObjectName", FieldDescription = "", Value = load.ObjectName }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Uploaded By", FieldName = "Requestor", FieldDescription = "", Value = load.Requestor }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Notes", FieldName = "LoadNotes", FieldDescription = "", Value = load.Notes + "" }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Error Messages", FieldName = "ErrorMessage", FieldDescription = "", Value = load.ErrorMessage + "" }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Total", FieldName = "LoadTotal", FieldDescription = "", Value = load.Total.ToString() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "# Incompletes", FieldName = "LoadIncomplete", FieldDescription = "", Value = load.Incomplete.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "# Successes", FieldName = "LoadSuccess", FieldDescription = "", Value = load.Success.ToString() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "# Errors", FieldName = "LoadError", FieldDescription = "", Value = load.Error.ToString() }
                            }
                        });

                        if (load.DateCompleted.HasValue && load.DateStarted.HasValue)
                        {
                            var minutes = Math.Round((load.DateCompleted.Value - load.DateStarted.Value).TotalMinutes);

                            var minutesMessage = (minutes == 0 ? "less than a minute" : minutes + " minute(s)");

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Took (minutes)", FieldName = "EllapsedTime", FieldDescription = "", Value = minutesMessage  }
                            }
                            });
                        }
                        else if (load.Action == "Promotion") // if bulk load promote display current status of the job
                        {
                            var currentStatus = GetPromotionStatusMessage(load);

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Status", FieldName = "Status", FieldDescription = "", Value = currentStatus  }
                            }
                            });
                        }
                    }
                    break;
                #endregion
                case SystemObjects.Policy:
                    #region Fields
                    var policy = Company.Query<dynamic>(@"
                                                            select	A.ID as AssetId,
                                                                    A.UID as UID,
		                                                            A.ObjectID ,
		                                                            T.ID as AssetTypeId,
		                                                            P.TextPath,
		                                                            L.Level
                                                            from	Asset A
		                                                            inner join AssetType T on T.ID = A.AssetTypeID
		                                                            cross apply dbo.GetAssetTextPathById(A.ID, '/') P
		                                                            cross apply dbo.GetAssetLevelById(A.ID) L
                                                            where	A.Object = 'Policy' and A.ObjectID = @id
                                                            ", new { id }).SingleOrDefault();
                    if (policy != null)
                    {
                        var policyAssetTypeID = (int)policy.AssetTypeId;
                        var policyLevel = (int)policy.Level;
                        var policyLevelInfo = Company.Filter<AssetTypeLevel>(i => i.AssetTypeID == policyAssetTypeID && i.Level == policyLevel).SingleOrDefault();

                        if (policyLevelInfo != null)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Level Name", Value = policyLevelInfo.Name }
                                },
                                SecondColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Level Number", Value = policyLevel.ToString() }
                                },
                                Category = Resources.FieldInfo.SystemNoCategory
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name =Fields.Path_Name, FieldName = "PolicyTextPath", FieldDescription =Fields.Path_Description, Value = policy.TextPath }
                            },
                            Category = Resources.FieldInfo.SystemNoCategory
                        });

                        var dynamicRows = await loadDynamicDisplayFields(type, id).ConfigureAwait(false);

                        if (useAssetDetailColumnDefinition && fcMapper != null)
                        {
                            fcMapper.ArrangeRowsAndCols(dynamicRows);
                        }
                        else
                        {
                            model.rows.AddRange(dynamicRows);
                        }
                        var asset = Company.Assets.Where(x => x.Object == "Policy" && x.ObjectID == id).FirstOrDefault();

                        if (asset != null)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = asset.ID.ToString(), DataType = "string" }
                            },
                                Category = Resources.FieldInfo.SystemFieldCategory
                            });

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.Asset_UID_Name, FieldName = "AssetUid", FieldDescription = Resources.FieldInfo.Asset_UID_Description, Value = asset.uid.ToString(), DataType = "string" }
                            },
                                SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetType_UID_Name, FieldName = "AssetTypeUid", FieldDescription = Resources.FieldInfo.AssetType_UID_Description, Value = asset.AssetType.uid.ToString(), DataType = "string" }
                            },
                                Category = Resources.FieldInfo.SystemFieldCategory
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = "ID", FieldName = "PolicyID", FieldDescription = Fields.Type_Description, Value = $"{policy.ObjectID}" }
                                },
                            Category = Resources.FieldInfo.SystemFieldCategory
                        });
                    }
                    policy = null;
                    break;
                #endregion

                case SystemObjects.RuleType:
                    #region Fields
                    var ruleType = Company.Filter<AssetType>(i => i.ObjectID == id && i.Object == "RuleType").SingleOrDefault();
                    if (ruleType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.Name_Name, FieldName = "RuleTypeName", Value = ruleType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.ID_Name, FieldName = "RuleTypeID", Value = ruleType.ObjectID.ToString() }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = ruleType.uid.ToString()  }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.Description_Name, FieldName = "RuleTypeDescription", DataType = "Html", Value = string.IsNullOrEmpty(ruleType.Description) ? "None provided" : ruleType.Description }
                            }
                        });
                    }
                    ruleType = null;
                    break;
                #endregion
                case SystemObjects.ResponsibilityType:
                    #region Fields
                    var responsibilityType = Company.GetById<ResponsibilityType>(id);
                    var responsibilityDetail = Company.GetObjectDetail("ResponsibilityType", id);

                    if (responsibilityType != null)
                    {
                        model.columns = 1;

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = responsibilityType.GetName(i => i.Name), FieldName = "Name", FieldDescription = responsibilityType.GetDescription(i => i.Name), Value = responsibilityType.Name }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = responsibilityType.UID.ToString()  }
                            }
                        });
                        if (!string.IsNullOrEmpty(responsibilityType.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = responsibilityType.GetName(i => i.Description), FieldName = "Description", DataType = "Html", FieldDescription = responsibilityType.GetDescription(i => i.Description), Value = responsibilityType.Description }
                                }
                            });
                        }
                    }
                    responsibilityType = null;
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region Fields
                    var policyType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "PolicyType").SingleOrDefault();
                    var objectDetail = Company.GetObjectDetail("PolicyType", id);
                    if (policyType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.Name_Name, FieldName = "PolicyTypeName", FieldDescription = Fields.Name_Description, Value = policyType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.ID_Name, FieldName = "PolicyTypeID", FieldDescription = Fields.ID_Description, Value = policyType.ObjectID.ToString() }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = objectDetail.UID.ToString()  }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.Description_Name, FieldName = "PolicyTypeDescription", FieldDescription = Fields.Description_Description, DataType = "Html", Value = string.IsNullOrEmpty(policyType.Description) ? "None provided" : policyType.Description }
                            }
                        });
                    }
                    policyType = null;
                    break;
                #endregion
                case SystemObjects.ReferenceItemType:
                    #region Fields                    
                    var refType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "ReferenceItemType").SingleOrDefault();
                    if (refType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.Name_Name, FieldName = "Name", FieldDescription = Fields.Name_Description, Value = refType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.DisplayFormat_Name, FieldName = "DisplayFormat", FieldDescription = Fields.DisplayFormat_Description, Value = refType.DisplayFormat }
                            }
                        });

                        if (!string.IsNullOrEmpty(refType.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Fields.Description_Name, FieldName = "Description", FieldDescription = Fields.Description_Description, DataType = "Html", Value = refType.Description }
                                }
                            });
                        }

                        if (!string.IsNullOrEmpty(refType.Notes))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Fields.SourceNotes_Name, FieldName = "SourceNotes", FieldDescription = Fields.SourceNotes_Description, DataType = "Html", Value = refType.Notes }
                                }
                            });
                        }


                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = refType.uid.ToString()  }
                                },
                            SecondColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Asset Type ID", FieldName = "AssetTypeId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = refType.ID.ToString(), DataType = "string" }
                                }
                        });

                        var parentRefType = Company.GetParentType(refType.ObjectID, SystemObjects.ReferenceItemType);

                        var heirarchyColumns = new DetailReadOnlyRowModel
                        {
                            columns = (parentRefType != null) ? 2 : 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Hierarchical", FieldName = "Hierarchical", FieldDescription = "Is this reference list a hierarchical reference list", Value = parentRefType != null ? "Yes":"No" }
                            }
                        };

                        if (parentRefType != null)
                        {
                            heirarchyColumns.SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Parent", FieldName = "Parent", FieldDescription = "Parent Reference List", Value = parentRefType.Name }
                            };
                        }

                        model.rows.Add(heirarchyColumns);
                    }
                    break;
                #endregion
                case SystemObjects.Report:
                    #region Fields
                    var report = Company.GetById<Report>(id, i => i.Responsibilities);
                    if (report == null)
                        report = Company.GetById<Report>(id);

                    if (report != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = report.GetName(i => i.Name), FieldName = "ReportName", FieldDescription = report.GetDescription(i => i.Description), Value = report.Name }
                            }
                        });

                        var reportType = "";

                        switch (report.ReportType ?? "")
                        {
                            case "powerbi":
                                reportType = "Power BI";
                                break;
                            case "sagacity":
                                reportType = "Data3Sixty Foundation";
                                break;
                            default:
                                reportType = "Default";
                                break;
                        }


                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Report Type", FieldName = "ReportType", Value = reportType}
                            }
                        });

                        if (!string.IsNullOrEmpty(report.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = report.GetName(i => i.Description), FieldName = "ReportDescription", FieldDescription = report.GetDescription(i => i.Description), DataType = "HTML", Value = report.Description }
                                }
                            });
                        }

                        if (!string.IsNullOrEmpty(report.FileName))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "File Name", FieldName = "FileName", Value = report.FileName }
                                }
                            });
                        }

                        if (report.Responsibilities != null && report.Responsibilities.Count > 0)
                        {
                            var responsibilityIds = report.Responsibilities.Select(x => x.ResponsibilityTypeID).ToList();
                            var responsibilities = Company.ResponsibilityTypes.Where(x => responsibilityIds.Contains(x.ID));

                            if (responsibilities != null)
                            {
                                var val = "";
                                foreach (var responsibility in responsibilities)
                                {
                                    if (val.Length > 0) val += ", ";
                                    val += responsibility.Name;
                                }

                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { Name = "Visible To", FieldName = "ReportResponsibilities", FieldDescription = "The report is only executable by users in the following roles.", Value = val }
                                    }
                                });
                            }
                        }

                        var sql = "";

                        switch (report.ObjectType)
                        {
                            case "Artifact":
                                sql = "select case when Class = 1 then 'Business Asset Instance : ' + Name when Class = 8 then 'Technical Asset Instance : ' + Name END, Class from AssetType where objectid = @id and [Object]='ArtifactType'";
                                break;
                            case "ArtifactType":
                                sql = "select case when Class = 1 then 'Business Asset : ' + Name when Class = 8 then 'Technical Asset : ' + Name END, Class from AssetType where objectid = @id and [Object]='ArtifactType'";
                                break;
                            case "Resource":
                                sql = "select 'Resource Instance'";
                                break;
                            case "Policy":
                                sql = "select 'Policy Instance : ' + Name from AssetType where objectid = @id and [Object]='PolicyType'";
                                break;
                            case "PolicyType":
                                sql = "select 'Policy Type : ' + Name from AssetType where objectid = @id and [Object]='PolicyType'";
                                break;
                            case "Rule":
                                sql = "select 'Rule Instance : ' + Name from AssetType where objectid = @id and [Object]='RuleType'";
                                break;
                            case "RuleType":
                                sql = "select 'Rule Type : ' + Name from AssetType where objectid = @id and [Object]='RuleType'";
                                break;
                            case "Taxonomy":
                                sql = "select 'Model Instance : ' + Name from AssetType where objectid = @id and [Object]='TaxonomyType'";
                                break;
                            case "TaxonomyType":
                                sql = "select 'Model Type : ' + Name from AssetType where objectid = @id and [Object]='TaxonomyType'";
                                break;
                        }

                        var objectName = (!string.IsNullOrEmpty(sql)) ?
                            Company.Query<string>(sql, new { id = report.ObjectID }).SingleOrDefault() :
                            "Not found.";

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Row = 3, Column = 2, Name = report.GetName(i => i.ObjectType), FieldName = "ReportObjectType", FieldDescription = report.GetDescription(i => i.ObjectType), Value = objectName }
                            }
                        });
                    }
                    report = null;
                    break;
                #endregion
                case SystemObjects.Resource:
                    #region Fields
                    var resource = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();
                    if (resource != null)
                    {
                        model.columns = 1;

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Name", Value = resource.FullName },
                            },
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Email", FieldName = "ResourceEmail", FieldDescription = resource.GetDescription(i => i.Email), Value = resource.Email }
                            },
                        });

                        var lastSeen = getUserLastSeenText(resource.LastLoggedInOn);

                        if (!string.IsNullOrEmpty(lastSeen))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Last Seen", FieldName = "LastSeen", Value = lastSeen }
                            }
                            });
                        }

                        model.rows.AddRange(await loadDynamicDisplayFields(type, id).ConfigureAwait(false));
                    }
                    resource = null;
                    break;
                #endregion
                case SystemObjects.ResourceType:
                    #region Fields
                    var resourceType = Community.Filter<AssetType>(i => i.ObjectID == id && i.Object == "ResourceType").SingleOrDefault();
                    if (resourceType != null)
                    {
                        model.columns = 1;

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name =Fields.Name_Name, FieldName = "ResourceTypeName", FieldDescription = Fields.Name_Description, Value = resourceType.Name }
                            }
                        });
                    }
                    resourceType = null;
                    break;
                #endregion
                case SystemObjects.SurveyType:
                    #region Fields
                    var surveyType = Company.GetById<SurveyType>(id);
                    if (surveyType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Survey Name", FieldName = "SurveyTypeName", FieldDescription = surveyType.GetDescription(i => i.Name), Value = surveyType.Name }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Uid", FieldName = "Uid", FieldDescription = surveyType.GetDescription(i => i.Uid), Value = surveyType.Uid.ToString() }
                            }
                        });

                        var dtlSurveyType = Company.GetObjectDetail(surveyType.Object, surveyType.ObjectID);
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Object Type", FieldName = "SurveyTypeObjectType", FieldDescription = surveyType.GetDescription(i => i.Object), Value = (dtlSurveyType != null) ? dtlSurveyType.Class.GetDisplayName() : "Invalid class" }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Object", FieldName = "SurveyTypeObjectID", FieldDescription = surveyType.GetDescription(i => i.ObjectID), Value = (dtlSurveyType != null) ? dtlSurveyType.Name : surveyType.ObjectID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Description", FieldName = "Description", FieldDescription = surveyType.GetDescription(i => i.Description), Value = surveyType.Description, DataType = "HTML" }
                            }
                        });

                    }
                    surveyType = null;
                    break;
                #endregion
                case SystemObjects.Tag:
                    #region Fields
                    var tag = Company.GetById<Tag>(id);
                    if (tag != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Tag name", FieldName = "TagName", FieldDescription = tag.GetDescription(i => i.Value), Value = tag.Value }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Uid", FieldName = "Uid", FieldDescription = tag.GetDescription(i => i.uid), Value = tag.uid.ToString() }
                            }
                        });

                    }
                    surveyType = null;
                    break;
                #endregion
                case SystemObjects.Taxonomy:
                    #region Fields
                    var taxonomy = Company.Query<dynamic>(@"
select	A.ID as AssetID,
        A.UID as UID,
		A.ObjectID,
		T.ID as TypeID,
        T.uid as AssetTypeUid,
		P.TextPath,
		L.Level
from	Asset A
		inner join AssetType T on T.ID = A.AssetTypeID
		cross apply dbo.GetAssetTextPathById(A.ID, '/') P
		cross apply dbo.GetAssetLevelById(A.ID) L
where	A.Object = 'Taxonomy' and A.ObjectID = @id
", new { id }).SingleOrDefault();

                    if (taxonomy != null)
                    {

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = d360.core.resources.Fields.Path_Name, FieldName = "TaxonomyTextPath", FieldDescription = d360.core.resources.Fields.Path_Description, Value = taxonomy.TextPath }
                            },
                            Category = Resources.FieldInfo.SystemNoCategory
                        });

                        var assetTypeID = (int)taxonomy.TypeID;
                        var taxonomyLevel = (int)taxonomy.Level;
                        var levelInfo = Company.Filter<AssetTypeLevel>(i => i.AssetTypeID == assetTypeID && i.Level == taxonomyLevel).SingleOrDefault();

                        if (levelInfo != null)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Level Name", Value = levelInfo.Name }
                                },
                                SecondColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Level Number", Value = taxonomy.Level.ToString() }
                                },
                                Category = Resources.FieldInfo.SystemNoCategory
                            });
                        }

                        var dynamicRows = await loadDynamicDisplayFields(type, id).ConfigureAwait(false);

                        if (useAssetDetailColumnDefinition && fcMapper != null)
                        {
                            fcMapper.ArrangeRowsAndCols(dynamicRows);
                        }
                        else
                        {
                            model.rows.AddRange(dynamicRows);
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = $"{taxonomy.AssetID}", DataType = "string" }
                            },
                            Category = Resources.FieldInfo.SystemFieldCategory
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.Asset_UID_Name, FieldName = "AssetUid", FieldDescription = Resources.FieldInfo.Asset_UID_Description, Value = taxonomy.UID.ToString(), DataType = "string" }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetType_UID_Name, FieldName = "AssetTypeUid", FieldDescription = Resources.FieldInfo.AssetType_UID_Description, Value = taxonomy.AssetTypeUid.ToString(), DataType = "string" }
                            },
                            Category = Resources.FieldInfo.SystemFieldCategory
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = "ID", FieldName = "TaxonomyID", Value = $"{taxonomy.ID}" }
                            },
                            Category = Resources.FieldInfo.SystemNoCategory
                        });
                    }
                    taxonomy = null;
                    break;
                #endregion
                case SystemObjects.TaxonomyType:
                    #region Fields
                    var taxonomyType = Company.Filter<AssetType>(i => i.ObjectID == id && i.Object == "TaxonomyType").SingleOrDefault();
                    var taxonomyObjectDetail = Company.GetObjectDetail("TaxonomyType", id);
                    if (taxonomyType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.Name_Name, FieldName = "TaxonomyTypeName", FieldDescription = Fields.Name_Description, Value = taxonomyType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name =Fields.ID_Name, FieldName = "TaxonomyTypeID", FieldDescription = Fields.ID_Description, Value = taxonomyType.ObjectID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.MaximumDepth_Name, FieldName = "TaxonomyTypeMaximumDepth", FieldDescription = Fields.MaximumDepth_Description, Value = taxonomyType.HierarchyMaximumDepth.ToString() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = taxonomyObjectDetail.UID.ToString()  }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Fields.Description_Name, FieldName = "TaxonomyTypeDescription", FieldDescription = Fields.Description_Description, DataType = "Html", Value = string.IsNullOrEmpty(taxonomyType.Description) ? "None provided" : taxonomyType.Description }
                            }
                        });
                    }
                    taxonomyType = null;
                    break;
                #endregion
                case SystemObjects.Monitor:
                    #region Fields
                    var WorkflowDtl = Company.Query<dynamic>(string.Format(@"select 
	v.updatedon,
	v.version,	
	r.firstname +' '+ r.lastname as updatedby,
	wt.name
from workflow.version v 
left join reporting.global_resource r on (v.updatedby = r.resourceid)
inner join workflow.type wt on (wt.id = v.typeid)
where v.id = {0}", id)).FirstOrDefault();
                    if (WorkflowDtl != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Name", FieldName = "WorkFlowTypeName", FieldDescription = "WorkFlowTypeName", Value = !string.IsNullOrEmpty(WorkflowDtl.name) ? WorkflowDtl.name :  "None Provided" }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Update On", FieldName = "UpdateOn", FieldDescription = "UpdateOn", Value = WorkflowDtl.updatedon != null ? WorkflowDtl.updatedon.ToString() : "None Provided" }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Updated By", FieldName = "UpdatedBy", FieldDescription = "UpdatedBy", Value = string.IsNullOrEmpty(WorkflowDtl.updatedby) ? "None Provided" : WorkflowDtl.updatedby }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Version", FieldName = "Version", FieldDescription = "Version", Value = WorkflowDtl.version > 0 ? WorkflowDtl.version.ToString() : "None Provided" }
                            }
                        });
                    }
                    WorkflowDtl = null;
                    break;
                    #endregion

            }

            if (useSingleColumn || useAssetDetailColumnDefinition)
            {
                model.rows.ForEach(r =>
                {
                    if (r.columns == 2)
                    {
                        r.FirstColumnFields.AddRange(r.SecondColumnFields);
                        r.columns = 1;
                        r.SecondColumnFields = new List<ReadOnlyField>();
                    }
                });
            }

            return model;
        }

        private string GetPromotionStatusMessage(LoadDetail load)
        {
            var status = "Queued...";
            if (Company.LoadItems.Any(x => x.LoadID == load.ID))
            {
                status = "Processing spreadsheet data...";

                //once there are load items and put / post execution are both null
                var loadInfo = Company.Loads.FirstOrDefault(x => x.ID == load.ID);
                //once a post / put uid is in place
                if (loadInfo != null && (loadInfo.PostExecutionID.HasValue || loadInfo.PutExecutionID.HasValue))
                {
                    status = "Submitted requests waiting processing...";
                    //check if post / put uid is started
                    if (loadInfo.PostExecutionID.HasValue)
                    {
                        var post = Company.ApiExecutions.FirstOrDefault(x => x.ExecutionID == loadInfo.PostExecutionID.Value);

                        if (post != null && post.ProcessingStartedOn.HasValue)
                        {
                            status = "Submitted requests processing data...";
                        }
                        else
                        {
                            var put = Company.ApiExecutions.FirstOrDefault(x => x.ExecutionID == loadInfo.PutExecutionID.Value);

                            if (put != null && post.ProcessingStartedOn.HasValue)
                            {
                                status = "Submitted requests processing data...";
                            }
                        }
                    }
                }
            }

            return status;
        }

        private string getUserLastSeenText(DateTime? dateLastLoggedIn)
        {
            if (dateLastLoggedIn.HasValue)
            {
                DateTime now = DateTime.UtcNow;
                if (dateLastLoggedIn.Value > now.AddHours(-24) && dateLastLoggedIn.Value <= now)
                    return "Today";
                else if (dateLastLoggedIn.Value > now.AddDays(-7) && dateLastLoggedIn.Value <= now)
                    return "This week";
                else
                    return dateLastLoggedIn.Value.ToShortDateString();
            }
            return "";
        }

        [Route("{type}/{id:int}/object/statistics")]
        public ObjectStatisticTileModel GetTileObjectStatistics(SystemObjects type, int id)
        {
            return Company.GetObjectStatistics(type, id);
        }

        [Route("{type}/{id:int}/fields")]
        public List<EditableFieldItem> GetFieldTypesByObject(SystemObjects type, int id)
        {
            List<EditableFieldItem> list = Company
                    .GetFieldTypesByObject(type, id)
                    .Select(i => new EditableFieldItem
                    {
                        Text = i.FriendlyName,
                        Value = "{" + i.Name + "}"
                    })
                    .ToList();

            switch (type)
            {
                case SystemObjects.ReferenceItem:
                case SystemObjects.ReferenceItemType:
                    list.Add(new EditableFieldItem { Text = "Code", Value = "{Code}" });
                    break;
                case SystemObjects.Resource:
                case SystemObjects.ResourceType:
                    list.Add(new EditableFieldItem { Text = "First Name", Value = "{FirstName}" });
                    list.Add(new EditableFieldItem { Text = "Last Name", Value = "{LastName}" });
                    list.Add(new EditableFieldItem { Text = "Email", Value = "{Email}" });
                    break;
            }

            return list.OrderBy(i => i.Text).ToList();
        }

        [Route("{type}/{id:int}/followers")]
        public IQueryable<FollowDetail> GetFollowers(SystemObjects type, int id)
        {
            return Company.GetFollowersByObject(type, id);
        }


        [Route("{id:int}/permissionsbyid")]
        public List<PermissionInfo> GetPermissionsObject(int id)
        {
            var isAdmin = Company.CurrentResourceIsAdmin;

            AssetDetail asset = null;
            if (!isAdmin)
            {
                asset = Company.GetAssetDetail(id);
                if (asset == null)
                {
                    throw new ArgumentNullException(ApiMessages.AssetNotfound);
                }
            }

            return GetPermissionsByObject(asset, isAdmin);
        }

        [Route("{type}/{uid}/permissions")]
        public List<PermissionInfo> GetPermissionsByObject(SystemObjects type, Guid uid)
        {

            if (type == SystemObjects.Tag)
            {
                List<PermissionInfo> ret = new List<PermissionInfo>();
                if (tagRepository.IsAuthorizedToEditTag(uid))
                {
                    ret.AddRange(Permission.DeleteAsset.GetList());
                }

                return ret;
            }

            if (type == SystemObjects.ConnectorLabel)
            {
                List<PermissionInfo> ret = new List<PermissionInfo>();
                if (connectorLabelRepository.IsAuthorizedToEditConnectorLabel(uid))
                {
                    ret.AddRange(Permission.DeleteAsset.GetList());
                }

                return ret;
            }
            int id = Company.GetObjectId(uid, type);
            return GetPermissionsByObject(type, id);
        }

        [Route("{type}/{id:int}/permissions")]
        public List<PermissionInfo> GetPermissionsByObject(SystemObjects type, int id)
        {
            var isAdmin = Company.CurrentResourceIsAdmin;
            AssetDetail asset = null;
            if (!isAdmin)
            {
                var sType = type.ToString();

                if (type.IsType())
                {
                    return Company.GetTypePermissions(sType, id);
                }
                else
                {
                    asset = Company.Filter<AssetDetail>(i => i.Object == sType && i.ObjectID == id).FirstOrDefault();
                    if (asset == null)
                    {
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = ApiMessages.AssetNotfound });
                    }
                    return GetPermissionsByObject(asset, isAdmin);
                }
            }
            else
            {
                return GetPermissionsByObject(asset, isAdmin);
            }
        }

        private List<PermissionInfo> GetPermissionsByObject(AssetDetail asset, bool isAdministrator)
        {
            List<PermissionInfo> permissions = null;

            if (isAdministrator)
            {
                permissions = Permission.DeleteAsset.GetList();
                permissions.ForEach(p => { p.Selected = true; });
            }
            else
            {
                permissions = Company.GetPermissions(asset.ID, asset.AssetTypeID);
            }

            return permissions;
        }

        #region NEW Relationship Tile

        [Route("{obj}/{objid:int}/relationships/counts")]
        public IEnumerable<dynamic> GetRelationshipCountsByObject(SystemObjects obj, int objid)
        {
            var predicateTypeInfo = new PredicateType().GetAsList();
            var disallowEditIds = predicateTypeInfo.Where(p => p.AllowEditFromRelationshipEditor == false).Select(p => (int)p.ID).ToList();
            if (disallowEditIds.Count == 0) disallowEditIds.Add(0); //catch-all, just in case list is empty.
            //Only allow editing when diagram
            if (obj != SystemObjects.Task) disallowEditIds.Add((int)PredicateType.DiagramUse);

            string disallowEditFilter = string.Join(", ", disallowEditIds);

            var excludedPredicateTypes = new[] { (int)PredicateType.Diagram, (int)PredicateType.DiagramReference };

            var sql = "";

            if (obj == SystemObjects.ReferenceItemType)
                sql = string.Format(QueryConstants.ReferenceListTypeRelationshipsAllCountsWithZero, disallowEditFilter);
            else
                sql = string.Format(QueryConstants.ObjectRelationshipAllCountsWithZero, disallowEditFilter, string.Join(",", excludedPredicateTypes));

            var data = Company.Query<dynamic>(sql, new { obj = new DbString { IsAnsi = true, Value = obj.ToString(), IsFixedLength = true, Length = 50 }, objid });

            return data;

        }

        [Route("{obj}/{objid:int}/relationships/{targettype}/{targettypeid:int}/fields")]
        public HttpResponseMessage GetRelationshipFieldsByObject(SystemObjects obj, int objid, SystemObjects targettype, int targettypeid)
        {
            var columns = new List<GridColumn>();
            var fields = new List<GridField>();

            var IDs = Company.Query<int>(
                QueryConstants.ObjectRelationshipTypeIDs,
                new
                {
                    obj = new Dapper.DbString { IsAnsi = true, Value = obj.ToString() },
                    objid,
                    objtype = new Dapper.DbString { IsAnsi = true, Value = targettype.ToString() },
                    objtypeid = targettypeid
                }
            ).ToList();

            var fieldTypes = Company.Filter<FieldType>(i =>
                i.Object == "IntersectType" &&
                IDs.Contains(i.ObjectID) &&
                i.IsListable
            ).OrderBy(i => i.SortOrder).ToList();

            columns.Add(new GridColumn { text = "Name", datafield = "Name", columntype = GridColumn.COLUMN_TYPE_STRING, filtertype = GridColumn.FILTER_TYPE_STRING });

            fieldTypes.ForEach(f =>
            {
                fields.Add(getGridFieldForColumn(f));
                columns.Add(getGridColumnForColumn(f, 100, false, false));
            });

            fields.Add(new GridField { name = "ID", type = "number" });
            fields.Add(new GridField { name = "Name", type = "string" });
            fields.Add(new GridField { name = "ObjectID", type = "number" });
            fields.Add(new GridField { name = "Object", type = "string" });

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Fields = fields,
                Columns = columns
            });
        }

        #endregion

        [Route("{type}/{id:int}/relations")]
        public IEnumerable<dynamic> GetRelationships(SystemObjects type, int id)
        {
            return Company.Query<dynamic>(QueryConstants.ObjectRelationships, new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString(), IsFixedLength = true, Length = 50 }, id });
        }

        [Route("{type}/{id:int}/relationships/{targetType}/{targetID:int}/{intersectTypeUID}"), HttpGet]
        public IEnumerable<dynamic> RelationshipsForObjectByTargetType(SystemObjects type, int id, SystemObjects targetType, int targetID, string intersectTypeUID, bool includeInverse = true, bool sourceIsObject = false)
        {
            Guid guid = Guid.Parse(intersectTypeUID);
            int ID = Company.GetObjectId(guid, SystemObjects.IntersectType);
            return RelationshipsForObjectByTargetType(type, id, targetType, targetID, ID, includeInverse, sourceIsObject);
        }

        [Route("{type}/{id:int}/relationships/{targetType}/{targetID:int}/{intersectTypeID:int}"), HttpGet]
        public IEnumerable<dynamic> RelationshipsForObjectByTargetType(SystemObjects type, int id, SystemObjects targetType, int targetID, int intersectTypeID, bool includeInverse = true, bool sourceIsObject = false)
        {
            var joins = "";
            var columns = "";
            var assetColumns = ", cast(1 as bit) as CanEdit, cast(1 as bit) as CanDelete ";

            getDynamicFieldJoinStatements(intersectTypeID, "Intersect", out joins, out columns, true, false);

            var sourceJoins = "";
            var sourceColumns = "";

            getDynamicFieldJoinStatements(targetID, targetType.ToString().Replace("Type", ""), out sourceJoins, out sourceColumns, false, false, false, true, "A.ObjectID", "A.Name", false);

            joins += sourceJoins;
            columns += sourceColumns;

            var intersectType = Company.GetById<IntersectType>(intersectTypeID);

            if (intersectType == null)
            {
                throw new ArgumentNullException(ApiMessages.InvalidIntersecttypeid);
            }

            var sTargetType = targetType.ToString();
            var targetAssetType = Company.Filter<AssetType>(i => i.Object == sTargetType && i.ObjectID == targetID).SingleOrDefault();
            if (targetAssetType == null)
            {
                throw new ArgumentNullException(ApiMessages.TargetAssetType);
            }


            var isTargetObject = intersectType.Object == sTargetType && intersectType.ObjectID == targetID;
            var isTargetSubjectSame = intersectType.Object == intersectType.Subject && intersectType.ObjectID == intersectType.SubjectID;
            var isTargetReferenceItemType = targetType.ToString() == "ReferenceItemType" && targetID == 0;

            var innerSql = "";
            var assetJoin = "";

            var permissionColumns = Company.CurrentResourceIsAdmin ? assetColumns : @"
, case when PE.AssetID is not null then cast(1 as bit) else cast(0 as bit) end as CanEdit
, case when PD.AssetID is not null then cast(1 as bit) else cast(0 as bit) end as CanDelete ";

            var permissionJoins = $@"
outer apply (select top 1 * from UserAssetPermissions(@userId,@targetAssetTypeId) where PermissionsBitMask & {(int)Permission.EditRelationships} = {(int)Permission.EditRelationships} and AssetTypeID = @targetAssetTypeId and (AssetID = IA.ID or AssetID = 0)) PE 
outer apply (select top 1 * from UserAssetPermissions(@userId,@targetAssetTypeId) where PermissionsBitMask & {(int)Permission.DeleteRelationships} = {(int)Permission.DeleteRelationships} and AssetTypeID = @targetAssetTypeId and (AssetID = IA.ID or AssetID = 0)) PD ";

            if (isTargetSubjectSame)
            {
                if (isTargetReferenceItemType)
                {
                    assetJoin = $@"
inner join (select 'Reference List' as Name) AST on 1 = 1 
inner join AssetType IA on	{(sourceIsObject ? " IA.Object = I.Subject and IA.ObjectID = I.SubjectID " : " IA.Object = I.Object and IA.ObjectID = I.ObjectID ")}
inner join (select ID, Name  as TextPath from AssetType) P on P.ID = IA.ID";
                }
                else
                {


                    assetColumns = permissionColumns;

                    if (includeInverse)
                    {
                        assetJoin = $@"
inner join AssetType AST on AST.Object = case when I.Subject = @type and I.SubjectID = @id then IT.Object else IT.Subject end
						    and AST.ObjectID = case when I.Subject = @type and I.SubjectID = @id then IT.ObjectID else IT.SubjectID end 
inner join Asset IA on	IA.Object = case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end
						and IA.ObjectID = case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end 
left join graph.AssetNodeDisplayPath P on P.ID = IA.ID
{permissionJoins}";
                    }
                    else
                    {
                        assetJoin = $@"
inner join AssetType AST on {(sourceIsObject ? "AST.Object = IT.Subject and AST.ObjectID = IT.SubjectID" : "AST.Object = IT.Object and AST.ObjectID = IT.ObjectID")}
inner join Asset IA on	{(sourceIsObject ? " IA.Object = I.Subject and IA.ObjectID = I.SubjectID " : " IA.Object = I.Object and IA.ObjectID = I.ObjectID ")}
left join graph.AssetNodeDisplayPath P on P.ID = IA.ID
{permissionJoins}";
                    }

                }

                var whereSql = "";

                if (includeInverse)
                {
                    whereSql = $@"((I.Subject = @type  and I.SubjectID = @id) or (I.Object = @type  and I.ObjectID = @id))";
                }
                else
                {
                    whereSql = $@"{(sourceIsObject ? "(I.Object = @type and I.ObjectID = @id)" : "(I.Subject = @type and I.SubjectID = @id)")}";
                }

                innerSql = $@"
select	I.[Uid],
        I.ID,
        IntersectTypeID,
        case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end as Object,
		case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end as ObjectID,
		{(isTargetReferenceItemType ? "P.TextPath as Name," : "P.DisplayPath as Name,")}        
		case when I.Subject = @type and I.SubjectID = @id then IT.Object else IT.Subject end as Type,
		case when I.Subject = @type and I.SubjectID = @id then IT.ObjectID else IT.SubjectID end as TypeID,
		AST.Name as TypeName,
        IA.uid as ObjectUid,
        case when I.Subject = @type and I.SubjectID = @id then cast(1 as bit) else cast(0 as bit) end as IsSubject
        {assetColumns}
from	[Intersect] I
        inner join IntersectType IT on IT.ID = I.IntersectTypeID
		{assetJoin}		
where	{whereSql}
        and I.IntersectTypeID = {intersectTypeID} ";
            }
            else if (isTargetObject)
            {
                if (isTargetReferenceItemType)
                {
                    assetJoin = $@"inner join (select 'Reference List' as Name) AST on 1 = 1
		                        inner join AssetType IA on	IA.Object = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end)" : "I.Object")}
								                        and IA.ObjectID = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end)" : "I.ObjectID")}
		                        inner join (select ID, Name  as TextPath from AssetType) P on P.ID = IA.ID";
                }
                else
                {
                    assetColumns = permissionColumns;
                    assetJoin = $@"
inner join AssetType AST on AST.Object = (case when I.Subject = @type and I.SubjectID = @id then IT.Object else IT.Subject end)
						    and AST.ObjectID = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then IT.ObjectID else IT.SubjectID end)" : "IT.ObjectID")} 
inner join Asset IA on	IA.Object = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end)" : "I.Object")}
						and IA.ObjectID = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end)" : "I.ObjectID")} 
left join graph.AssetNodeDisplayPath P on P.ID = IA.ID
{permissionJoins}";
                }

                innerSql = $@"
select	I.[Uid],
        I.ID,
        IntersectTypeID,
        case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end as Object,
		case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end as ObjectID,
        IA.uid as ObjectUid,
		{(isTargetReferenceItemType ? "P.TextPath as Name," : "P.DisplayPath as Name,")}        
		IT.Object Type,
		IT.ObjectID TypeID,
		AST.Name as TypeName,
        case when I.Subject = @type and I.SubjectID = @id then cast(1 as bit) else cast(0 as bit) end as IsSubject
        {assetColumns}
from	[Intersect] I
		inner join IntersectType IT on IT.ID = I.IntersectTypeID
		{assetJoin}		
where	((I.Subject = @type  and I.SubjectID = @id) {(includeInverse ? " or (I.Object = @type  and I.ObjectID = @id) " : "")})        
        and I.IntersectTypeID = {intersectTypeID} ";
            }
            else
            {
                if (isTargetReferenceItemType)
                {
                    assetJoin = $@"
inner join (select 'Reference List' as Name) AST on 1 = 1 
inner join AssetType IA on IA.Object = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.Subject else I.Object end)" : "I.Subject")}
                           and IA.ObjectID = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.SubjectID else I.ObjectID end)" : "I.SubjectID")} 
inner join (select ID, Name as TextPath from AssetType) P on P.ID = IA.ID";
                }
                else
                {
                    assetColumns = permissionColumns;
                    assetJoin = $@"
inner join AssetType AST on AST.Object = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then IT.Subject else IT.Object end)" : "IT.Subject")}
                            and AST.ObjectID = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then IT.SubjectID else IT.ObjectID end)" : "IT.SubjectID")}
inner join Asset IA on IA.Object = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.Subject else I.Object end)" : "I.Subject")}
                       and IA.ObjectID = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.SubjectID else I.ObjectID end)" : "I.SubjectID")}
left join graph.AssetNodeDisplayPath P on P.ID = IA.ID
{permissionJoins}";
                }

                innerSql = $@"
select	I.[Uid],
        I.ID,
        IntersectTypeID,
        case when I.Object = @type and I.ObjectID = @id then
            I.Subject
        else
            I.Object
        end as Object,
        case when I.Object = @type and I.ObjectID = @id then
            I.SubjectID
        else
            I.ObjectID
        end as ObjectID,
        IA.uid as ObjectUid,
		{(isTargetReferenceItemType ? "P.TextPath as Name," : "P.DisplayPath as Name,")}              
		IT.Subject as Type,
		IT.SubjectID as TypeID,
		AST.Name as TypeName,
        case when I.Subject = @type and I.SubjectID = @id then cast(1 as bit) else cast(0 as bit) end as IsSubject
        {assetColumns}
from    [Intersect] I 
        inner join IntersectType IT on IT.ID = I.IntersectTypeID 
        {assetJoin}         
where   ( 
        (I.Object = @type and I.ObjectID = @id) 
        {(includeInverse ? " or (I.Subject = @type  and I.SubjectID = @id) " : "")}
        ) 
        and IntersectTypeID = {intersectTypeID}";
            }

            var querySql = $"select {columns} A.* from ({innerSql}) A {joins}";

            var sql = string.Format(@"select * from ({0}) AA", querySql);
            sql = applyFilteringSuffix(sql, Request);

            sql += " order by AA.Name";

            return Company.Query<dynamic>(sql,
                new
                {
                    userId = Company.CurrentResourceID,
                    it = intersectTypeID,
                    type = new Dapper.DbString { IsAnsi = true, Value = type.ToString(), IsFixedLength = true, Length = 20 },
                    id,
                    targetAssetTypeId = targetAssetType.ID
                });
        }

        [Route("{type}/{id:int}/{predicateId:int}/synonyms")]
        public async Task<HttpResponseMessage> GetSynonymsByObject(SystemObjects type, int id, int predicateId)
        {
            var models = await Company.QueryAsync<dynamic>(
                QueryConstants.SynonymsByObjectList,
                new
                {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id,
                    predicateId
                }
            );

            return Request.CreateResponse(
                HttpStatusCode.OK,
                models
            );
        }

        [Route("{type}/{id:int}/nymAllocations")]
        public HttpResponseMessage GetNymAllocations(SystemObjects type, int id)
        {
            var model = new List<Dictionary<string, object>>();
            //get universe of available nyms / predicates of type 8.

            var availablePredicates = Company.Filter<Predicate>(x => x.Type == PredicateType.Grammar).OrderBy(x => x.Name);

            // get which ones are allocted for this object.

            var selectedPredicates = Company.Filter<NymRelation>(x => x.Object == type.ToString() && x.ObjectID == id);

            foreach (var predicate in availablePredicates)
            {
                model.Add(new Dictionary<string, object>
                {
                    { "Name",predicate.Name },
                    { "ID",predicate.ID },
                    { "Enabled",selectedPredicates.Where(x =>x.PredicateID == predicate.ID).Any() }
                });
            }

            return Request.CreateResponse(
                HttpStatusCode.OK,
                model
            );
        }

        #endregion

        #region Surveys

        [Route("surveys")]
        public IQueryable<SurveyType> GetSurveyTypes()
        {
            if (!Company.CurrentResourceIsAdmin) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));

            return Company.Table<SurveyType>();
        }

        [Route("surveys/{typeID:int}/questions")]
        public HttpResponseMessage GetQuestionTypesBySurveyType(int typeID)
        {
            var list = Company.Filter<QuestionType>(i => i.SurveyTypeID == typeID, i => i.QuestionTypeOptions)
                .ToList()
                .Select(i => new
                {
                    i.ID,
                    i.Name,
                    OptionCount = i.QuestionTypeOptions.Count,
                    DisplayStyle = i.DisplayStyle.GetDescription(),
                    Description = i.Description
                });
            return Request.CreateResponse(HttpStatusCode.OK, list);
        }

        [Route("surveys/{typeID:int}/{type}/{id}/report")]
        public JObject GetSurveyReport(int typeID, SystemObjects type, int id)
        {
            var sql = $@"
SELECT (
		SELECT	(
				SELECT
					(
					SELECT		QT.ID,
								QT.Name AS Title,
								S.Average/S.Total AS Score,
								COALESCE(S.Responses, 0) AS TotalResponses,
								(
								SELECT	(
											SELECT		IQTO.Name,
														COUNT(1) AS Value
											FROM		Question IQ
														INNER JOIN QuestionOption IQO ON IQ.ID = IQO.QuestionID
														INNER JOIN QuestionTypeOption IQTO on IQTO.ID = IQO.QuestionTypeOptionID and IQTO.QuestionTypeID = QT.ID
														inner join Survey S on S.ID = IQ.SurveyID and S.Object = @Object and S.ObjectID = @ObjectID
														inner join  SurveyType ST on ST.ID = S.SurveyTypeID and ST.ID = @SurveyTypeID
											WHERE		IQTO.QuestionTypeID = QT.ID
											GROUP BY	IQTO.QuestionTypeID, 
														IQTO.Name
											ORDER BY	IQTO.QuestionTypeID
											FOR XML PATH('Result'), Type
										) FOR XML PATH('Results'), Type
								)
					FROM		QuestionType QT
								LEFT JOIN	(
											SELECT		QT.ID AS QuestionTypeID,
														AVG(QTO.Value) AS Average,
														QTO.Value as Total,
														COUNT(1) AS Responses
											FROM		QuestionType QT
														INNER JOIN QuestionTypeOption QTO on QTO.QuestionTypeID = QT.ID and QT.ID = @SurveyTypeID
														INNER JOIN QuestionOption QO ON QO.QuestionTypeOptionID = QTO.ID
														LEFT JOIN Question Q ON Q.ID = QO.QuestionID
											GROUP BY	QT.ID, QTO.Value
											) AS S ON S.QuestionTypeID = QT.ID
					WHERE		QT.SurveyTypeID = ST.ID
					ORDER BY	QT.ID
					FOR XML PATH('Chart'), Type
					)
				FOR XML PATH('Charts'), Type--as Charts
				)
		FROM		SurveyType ST
					INNER JOIN Survey S ON ST.ID = S.SurveyTypeID AND S.Object = @Object AND S.ObjectID = @ObjectID and getutcdate() between S.CreatedOn and dateadd(dd, ST.[ValidForDays], S.CreatedOn)
		WHERE		ST.ID = @SurveyTypeID
		GROUP BY ST.Name, ST.ID
		FOR XML PATH(''), Type
		)
		FOR XML PATH('Report')";


            var models = Company.Query<string>(sql, new { SurveyTypeID = typeID, Object = new DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, ObjectID = id });
            var xmlString = string.Join("", models);
            var xml = XElement.Parse(xmlString);
            string json = JsonConvert.SerializeXNode(xml);
            return JObject.Parse(json);
        }

        [Route("surveys/{parentType}/{parentId}/{type}/{id}/survey")]
        public ObjectSurveyModel GetSurvey(SystemObjects parentType, int parentId, SystemObjects type, int id)
        {
            var sql = @"
                        select id, name from surveytype where object= @parObj and objectid= @parObjId and id not in(
			                    select 
				                    st.id
			                    from 
				                    surveytype st 
				                    inner join survey s on (s.surveytypeid = st.id and s.resourceid = @resource and s.createdon > DATEADD(day, (st.validfordays*-1), getdate()) and s.[object] = @obj and s.ObjectID = @objId)
                    )
            ";

            var surveys = Company.Query<ObjectSurveyModel>(sql, new { parObj = new DbString { Value = parentType.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, parObjId = parentId, resource = Company.CurrentResourceID, obj = new DbString { Value = type.ToString(), IsFixedLength = true, IsAnsi = true, Length = 50 }, objId = id }).ToList();

            if (surveys == null || surveys.Count == 0) return null;

            var rand = new Random();

            var randIndex = rand.Next(0, surveys.Count);

            if (randIndex > 0 && randIndex < surveys.Count)
                return surveys[randIndex];

            return surveys.First();
        }

        [Route("survey/{surveyId}/{objectId}/{type}")]
        [ValidateHttpAntiForgeryTokenAttribute]
        public CreateResponse PostSurveyResponse(int surveyId, int objectId, string type, SurveyResponseModel data)
        {
            foreach (var question in data.Questions)
            {
                if (!question.Values.Any(x => x.IsChecked == true))
                {
                    throw new Exception(ApiMessages.InvalidModel);
                }
            }

            var survey = new Survey
            {
                SurveyTypeID = surveyId,
                Object = type,
                ObjectID = objectId,
                ResourceID = Company.CurrentResourceID,
                CreatedOn = DateTime.UtcNow
            };

            Company.SaveOrUpdate<Survey>(survey);

            foreach (var question in data.Questions)
            {
                //insert the question
                var q = new Question
                {
                    SurveyID = survey.ID,
                    Comment = question.Comments
                };

                Company.SaveOrUpdate<Question>(q);

                // insert each selected survey value

                var selected = question.Values.Where(x => x.IsChecked);

                foreach (var value in selected)
                {
                    Company.Query<int>("insert into questionoption (QuestionID, QuestionTypeOptionID) values(@qId, @qTypeId)", new { qId = q.ID, qTypeId = value.ID }).FirstOrDefault();
                }
            }


            return new CreateResponse { Message = "Created" };
        }

        [Route("surveys/question/{questionId}/values")]
        public IEnumerable<ObjectSurveyQuestionValuesModel> GetSurveyQuestionValues(int questionId)
        {
            var sql = @"select 
	                        ID,
	                        Name,
	                        [Value]
                        from questiontypeoption where questiontypeid = @id order by id";

            return Company.Query<ObjectSurveyQuestionValuesModel>(sql, new { id = questionId });
        }

        #endregion

        #region Taxonomy

        [Route("TaxonomyType/{id:int}/levels")]
        public IQueryable<dynamic> GetTaxonomyTypeLevels(int id)
        {
            return Company.Query<dynamic>(@"Select AT.ObjectId as TaxonomyTypeID,ATL.Level,ATL.Name,ATL.Description
                                            From AssetTypeLevel ATL
                                            inner join AssetType AT on AT.Id = ATL.AssetTypeID
                                            WHERE  [object]='TaxonomyType' and ObjectId=@ObjectId
                                            order by Level", new { ObjectId = id }).AsQueryable();
        }

        [Route("catalogs/{typeID:int}")]
        public async Task<HttpResponseMessage> GetTaxonomyType(int typeID)
        {
            var row = await Company.QueryFirstOrDefaultAsync<dynamic>(QueryConstants.TaxonomySettingsItem, new { id = typeID });

            if (row == null)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object> {
                    { "ID", row.ID },
                    { "MaximumDepth", row.MaximumDepth },
                    { "Name", row.Name },
                    { "Description", row.Description },
                    { "NymTypes", Company.Query<dynamic>(QueryConstants.ObjectNymTypes, new { id = typeID, ot = new DbString {Value = "TaxonomyType", IsFixedLength = true, IsAnsi = true, Length = 50 } }) },
                    { "HasDashboards", row.HasDashboards },
                    { "AssetTypeUID", row.Uid }
                }
            );
        }

        [Route("getAssetTypeObjectAndObjectID/{uid}")]
        public HttpResponseMessage GetObjectandId(Guid uid)
        {
            var sql = $@"SELECT top 1 Object, ObjectID, Id from AssetType WHERE Uid = @uid";
            var details = Company.Query<dynamic>(sql, new { uid }).Single();
            return Request.CreateResponse<dynamic>(new { details.Object, details.ObjectID, details.Id });
        }

        #endregion

        #region Counts

        [Route("CountItems/Activity/{assetTypeId}/{days}")]
        public IEnumerable<AssetDetail> GetAreaActivityItems(int assetTypeId, int days)
        {
            var sql = @"select * from AssetDetail where AssetTypeID = @assetTypeId";

            if (days != 0)
            {
                days = days * -1;

                sql += " and (CreatedOn > dateadd(day, @d, CURRENT_TIMESTAMP) or UpdatedOn > dateadd(day, @d, CURRENT_TIMESTAMP))";

                return Company.Query<AssetDetail>(sql, new { assetTypeId, d = days });
            }

            return Company.Query<AssetDetail>(sql, new { assetTypeId });
        }

        [Route("Count/{area}/{days}")]
        public IEnumerable<CountModel> GetHomeCounts(string area, int days, int id = -1)
        {
            //Social count has been moved to V2 Social controller and should be got from there
            var areaName = (area ?? string.Empty).ToUpper();
            var resourceId = id > 0 ? id : Company.CurrentResourceID;

            switch (areaName)
            {
                case "SOCIAL":
                    return LoadSocialActivityCount(days, resourceId).Result;
                case "ACTIVITY":
                    return LoadArtifactActivityCount(days);
                case "ASSIGNMENTS":
                    return LoadWorkflowAssignmentsCount(resourceId);
            }
            return null;
        }

        [Route("Counts/{id}/{days}")]
        public async Task<IEnumerable<CountModel>> GetTheCounts(int days, int id = -1)
        {
            //Social count has been moved to V2 Social controller and should be got from there
            var resourceId = id > 0 ? id : Company.CurrentResourceID;
            return await LoadSocialActivityCount(days, resourceId);
        }

        private IEnumerable<CountModel> LoadArtifactActivityCount(int days)
        {
            var sql = string.Empty;
            if (days != 0)
            {
                days = days * -1;
                sql = QueryConstants.ArtifactActivitySpecificDateCountList;
            }
            else
            {
                sql = QueryConstants.ArtifactActivityAllDateCountList;
            }

            return Company.Query<CountModel>(sql, new { d = days });
        }

        private async Task<IEnumerable<CountModel>> LoadSocialActivityCount(int days, int resourceId)
        {
            days = days * -1;
            var rangeEnd = DateTime.UtcNow;
            var rangeStart = rangeEnd.AddDays(days);
            var countsRequest = await commentsRepository.GetCommentCountsByFollower(resourceId, null, rangeStart, rangeEnd);
            var counts = countsRequest.OrderBy(i => i.CommentTypeName);

            List<CountModel> items = new List<CountModel>();

            //need to add a record for social, Issue, Task, DataEvent, Question

            items.Add(new CountModel { Name = Resources.Core.CommentType_Social, Total = getCommentCategoryCount(counts, CommentType.Social) });

            items.Add(new CountModel { Name = Resources.Core.CommentType_Action, Total = getCommentCategoryCount(counts, CommentType.Issue) });

            return items.OrderBy(x => x.Name);
        }

        private int getCommentCategoryCount(IEnumerable<CommentCount> counts, CommentType commentType)
        {
            var commentsItem = (counts.FirstOrDefault(x => x.CommentType == commentType));
            return commentsItem == null ? 0 : commentsItem.Count;
        }

        private IEnumerable<CountModel> LoadWorkflowAssignmentsCount(int resourceId)
        {
            var sql = @"select
	                             wt.name as Name
                                ,wt.id as Id
								,wv.[version]
								, wvs.name as Step
                                ,wvs.Id as StepId
                                ,count(1) as Total 
                                from
	                                [workflow].[type] wt
	                                inner join [workflow].[version] wv on (wt.id = wv.typeid)
	                                inner join [workflow].[item] wi on (wv.id = wi.versionid)	
                                    inner join [workflow].[itemstep] wis on(wis.itemid = wi.id and wis.completedon is null)
	                                inner join [workflow].[itemassignment] wia on(wia.itemid = wi.id and wia.resourceobject = 'Resource' and wia.resourceobjectid = @r and (wia.itemstepid = wis.id or wia.itemstepid is null))
	                                inner join [workflow].[versionstep] wvs on(wvs.id = wis.stepid)
                                where
                                    wi.completedon is null and wvs.steptype = 2 and wvs.activitytype = 3
									group by wt.name, wt.id,wv.[version],wvs.name,wvs.Id order by wt.Name asc,[version] desc,Step asc";

            return Company.Query<CountModel>(sql, new { r = resourceId });
        }

        #endregion

        #region Angular Breadcrumb calls

        public class BreadcrumbTypeAheadModel
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public int ID { get; set; }
        }

        [Route("breadcrumb/typeahead")]
        public async Task<IEnumerable<BreadcrumbTypeAheadModel>> GetBreadcrumbTypeahead(string q, int num, SystemObjects objectType, int objectId)
        {
            var sql = $"select top {num} ad.DisplayValue as Name, u.Url  from asset ast inner join assettype astt on (ast.assetTypeID = astt.id)  inner join AssetDisplayValue AD on AD.assetid = ast.id cross apply [dbo].GetAssetUrlById(ast.ID) u where ast.[object] = @typeName and astt.objectId = @typeId and ad.DisplayValuePrefix like @search " +
                        $"Order By ad.DisplayValue";

            return await Company.QueryAsync<BreadcrumbTypeAheadModel>(sql, new { typeName = new DbString { Value = objectType.ToString(), IsFixedLength = true, Length = 20, IsAnsi = true }, typeId = objectId, search = $"{q}%" });
        }

        [Route("breadcrumb/typeaheadfortype")]
        public async Task<IEnumerable<BreadcrumbTypeAheadModel>> GetBreadcrumbTypeaheadForType(string q, int num, SystemObjects objectType, int objectId)
        {
            //var sql = $"select top {num} ad.DisplayValue as Name, u.Url  from asset ast inner join assettype astt on (ast.assetTypeID = astt.id)  inner join AssetDisplayValue AD on AD.assetid = ast.id cross apply [dbo].GetAssetUrlById(ast.ID) u where ast.[object] = @typeName and astt.objectId = @typeId and ad.DisplayValuePrefix like @search";
            var sql = $" select top {num} AT.ID, AT.ObjectID, AT.Name, u.Url,IT.SubjectID as ParentID from AssetType AT " +
                        $"cross apply [dbo].GetAssetTypeUrlById(AT.ID) u " +
                        $"outer apply (SELECT IT.SubjectID from IntersectType IT " +
                        $"              inner join [Predicate] P on IT.Object = @typeName " +
                        $"              and IT.ObjectID = AT.ObjectID and P.ID = IT.PredicateID and P.Type = 3) IT " +
                        $" WHERE IT.SubjectID = " +
                        $" (SELECT  IT.SubjectID as ParentID FROM AssetType AT " +
                        $"          outer apply (select	IT.SubjectID from	IntersectType IT " +
                        $"          inner join [Predicate] P on IT.Object = @typeName and IT.ObjectID = AT.ObjectID " +
                        $"          and P.ID = IT.PredicateID and P.Type = 3) IT " +
                        $"where AT.[Object] = @typeName and AT.[objectId] = @typeId) AND AT.[Object] = @typeName AND AT.Name like @search " +
                        $"Order By AT.Name";

            return await Company.QueryAsync<BreadcrumbTypeAheadModel>(sql, new { typeName = new DbString { Value = objectType.ToString(), IsFixedLength = true, Length = 30, IsAnsi = true }, typeId = objectId, search = $"%{q}%" });
        }

        [Route("breadcrumb/typeaheadfortypewithoutparent")]
        public async Task<IEnumerable<BreadcrumbTypeAheadModel>> GetBreadcrumbTypeaheadForTypewithoutparent(string q, int num, SystemObjects objectType)
        {
            //var sql = $"select top {num} ad.DisplayValue as Name, u.Url  from asset ast inner join assettype astt on (ast.assetTypeID = astt.id)  inner join AssetDisplayValue AD on AD.assetid = ast.id cross apply [dbo].GetAssetUrlById(ast.ID) u where ast.[object] = @typeName and astt.objectId = @typeId and ad.DisplayValuePrefix like @search";
            var sql = $" select top {num} AT.ID, AT.ObjectID, AT.Name, u.Url from AssetType AT " +
                        $"cross apply [dbo].GetAssetTypeUrlById(AT.ID) u " +
                        $" where AT.[Object] = @typeName AND AT.Name like @search " +
                        $"Order By AT.Name";

            return await Company.QueryAsync<BreadcrumbTypeAheadModel>(sql, new { typeName = new DbString { Value = objectType.ToString(), IsFixedLength = true, Length = 30, IsAnsi = true }, search = $"%{q}%" });
        }

        [Route("breadcrumb/getArea")]
        public async Task<string> GetBreadcrumbAreaByType(SystemObjects objectType, int objectId)
        {
            //var sql = $"select top {num} ad.DisplayValue as Name, u.Url  from asset ast inner join assettype astt on (ast.assetTypeID = astt.id)  inner join AssetDisplayValue AD on AD.assetid = ast.id cross apply [dbo].GetAssetUrlById(ast.ID) u where ast.[object] = @typeName and astt.objectId = @typeId and ad.DisplayValuePrefix like @search";
            var sql = $" select Title FROM [dbo].[SiteNav] WHERE ID = (Select top 1 ParentID FROM [dbo].[SiteNav] WHERE [Object] = @typeName and [objectId] = @typeId)";

            var res = await Company.QueryAsync<string>(sql, new { typeName = new DbString { Value = objectType.ToString(), IsFixedLength = true, Length = 30, IsAnsi = true }, typeId = objectId });
            return res.FirstOrDefault();
        }

        #endregion

        #region Reference

        [HttpGet, Route("referenceItems/{typeID:int}/items.json")]
        public async Task<HttpResponseMessage> GetReferenceItems(int typeID)
        {
            var models = await Company.QueryAsync<dynamic>($"exec [dbo].[GetReferenceItemValues] {typeID}, {Company.CurrentResourceID}");
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        [HttpGet, Route("referenceItems/field/{fieldId:int}/items.json")]
        public Task<HttpResponseMessage> GetReferenceItemsByFieldId(int fieldId)
        {
            var field = Company.GetById<FieldType>(fieldId);
            return GetReferenceItems((int)field.LookupObjectID);

        }

        [HttpGet, Route("canReadReferenceItemType/{id:int}")]
        public async Task<HttpResponseMessage> CanReadReferenceItemType(int id)
        {
            var records = await Company.QueryAsync<dynamic>(@"
select	1 
from	ResponsibilityDetail
where	Type = 'ReferenceItemType'
		and TypeID = @id 
		and PermissionsBitMask & @p = 0
		and ResourceID = @resource", new { id, resource = Company.CurrentResourceID, p = (int)Permission.ReadAsset });

            return Request.CreateResponse(HttpStatusCode.OK, !records.Any());
        }

        #endregion

        #region Cascading dropdown values

        [Route("FieldType_CascadingListValues/{fieldTypeID:int}")]
        public List<SelectListInfoItem> GetCascadingDropdownFieldValues(int fieldTypeID, string parentItemId, string parentValues)
        {
            string[] parents = null;

            List<SelectListInfoItem> items = new List<SelectListInfoItem>();

            var fieldType = Company.FieldTypes.Where(x => x.ID == fieldTypeID).FirstOrDefault();

            if (fieldType == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            if (!string.IsNullOrEmpty(parentItemId))
            {
                parents = parentItemId.Split(',');
            }
            else if (!string.IsNullOrEmpty(parentValues))
            {

                var sqlParent = "select value from fieldlookupvalue where fieldtypeid = @id and text in @vals";
                parents = Company.Query<string>(sqlParent, new { id = fieldType.ParentFieldTypeID, vals = parentValues.Split(',') }).ToArray();
            }

            if (fieldType.FilterFieldTypeID > 0)
            {
                var filterFieldType = Company.FieldTypes.Where(x => x.ID == fieldType.FilterFieldTypeID).FirstOrDefault();

                var sql = @"select V.Text, V.Value";
                var join = "";

                if (fieldType.FilterPredicateDirection == true)
                {
                    sql += @", I.PredicateInverse as Predicate, I.SubjectShortName as ShortName ";
                    join = " on I.ObjectID = V.Value and V.LookupObjectType = I.Object and V.lookupObjectID = I.ObjectTypeID";
                }
                else
                {
                    sql += @", I.PredicateName  as Predicate, I.ObjectShortName as ShortName ";
                    join = " on I.SubjectID = V.Value and V.LookupObjectType = I.Subject and V.lookupObjectID = i.SubjectTypeID";
                }
                sql += $@"from fieldlookupvalue V
                        inner join IntersectDetail I {join} 
                        where V.fieldTypeID = @id and I.PredicateId = @PredcateId and I.{(fieldType.FilterPredicateDirection == true ? "SubjectID" : "ObjectID")} in @Parents";
                var rawItems = Company.Query<dynamic>(sql, new
                {
                    id = fieldTypeID,
                    PredcateId = fieldType.FilterPredicateID,
                    Parents = parents
                }).OrderBy(i => i.Text).ToList();

                foreach (var rawItem in rawItems)
                {
                    SelectListInfoItem match = items.FirstOrDefault(i => i.Value == rawItem.Value.ToString());
                    if (match != null)
                    {
                        match.Info += ", " + rawItem.ShortName;
                    }
                    else
                    {
                        items.Add(new SelectListInfoItem
                        {
                            Text = rawItem.Text,
                            Value = rawItem.Value.ToString(),
                            Info = rawItem.Predicate + " " + rawItem.ShortName
                        });
                    }
                }

            }
            else
            {
                //Cascading list should be reference items
                var predicateTypeId = 3;

                if ((fieldType.LookupObjectType ?? "").ToUpper() != "REFERENCEITEM") throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

                var referenceListType = Company.Filter<AssetType>(i => i.Object == "ReferenceItemType" && i.ObjectID == fieldType.LookupObjectID).FirstOrDefault();


                if (referenceListType == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

                var parentReferenceListType = Company.GetParentType(referenceListType.ObjectID, SystemObjects.ReferenceItemType);

                if (parentReferenceListType == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
                string textValue, colorjoin;
                if (LookupFieldHasColorItem(fieldType))
                {
                    textValue = "colorJSON.FV as Text";
                    colorjoin = $@" outer apply (SELECT FV = (
							SELECT flv.Text as name,
							 COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color
							from Asset A 
                            outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
							where A.Object = flv.LookupObjectType and A.ObjectID = flv.Value 
							FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
						)colorJSON";
                }
                else
                {
                    textValue = "flv.Text";
                    colorjoin = "";
                }
                var sql = $@"select {textValue}, flv.Value from fieldlookupvalue flv 
                        inner join[intersectdetail] id on(id.subjecttype = 'ReferenceItemType' and id.objecttype = 'ReferenceItemType' and id.predicatetype = @predicate and id.objectid = flv.value and id.objecttypeid = flv.lookupobjectid and id.subjecttypeid = @parentReferenceListTypeId)
                        inner join AssetDetail ad on(ad.TypeId = id.subjecttypeid and ad.Type='ReferenceItemType' and ad.[ObjectId] = id.subjectid  and ad.[Object]='ReferenceItem' )
                        {colorjoin}
                        where flv.fieldTypeID = @id and ad.[ObjectId] in @parentReferenceItemId";

                items = Company.Query<SelectListInfoItem>(sql, new { id = fieldTypeID, predicate = predicateTypeId, parentReferenceItemId = parents, parentReferenceListTypeId = parentReferenceListType.ObjectID }).ToList();
            }

            return items;

        }

        #endregion

        #region Custom API

        [Route("custom/services")]
        public List<ApiService> GetCustomAPIServices()
        {
            return Company.ApiServices.ToList();
        }

        [Route("custom/service/{id:int}/namespaces")]
        public List<ApiNamespace> GetCustomAPINamespaces(int id)
        {
            return Company.ApiNamespaces.Where(i => i.ServiceID == id).ToList();
        }

        [Route("custom/service/{id:int}")]
        public ApiService GetCustomAPIService(int id)
        {
            return Company.ApiServices.FirstOrDefault(x => x.ID == id);
        }


        [Route("custom/service/{serviceId:int}/endpoints")]
        public List<ApiEndpoint> GetCustomAPIServiceEndpoints(int serviceId)
        {
            return Company.ApiEndpoints.Where(x => x.ServiceID == serviceId).ToList();
        }

        [Route("custom/endpoint/{endpointId:int}")]
        public ApiEndpoint GetCustomAPIServiceEndpoint(int endpointId)
        {
            return Company.ApiEndpoints.FirstOrDefault(x => x.ID == endpointId);
        }

        [Route("custom/endpoint/{endpointId:int}/versions")]
        public List<dynamic> GetCustomAPIEndpointVersions(int endpointId)
        {
            return Company.Query<dynamic>(@"select 
	                                    v.*,
                                        e.ID as EntityID
                                    from api.EndpointVersion v
                                    inner join api.Entity e on v.id = e.endpointversionid
                                    where v.endpointid = @id", new { id = endpointId }).ToList();
        }

        [Route("custom/version/{versionId:int}/fields")]
        public List<dynamic> GetCustomAPIVersionFields(int versionId)
        {
            var entity = Company.ApiEntities.FirstOrDefault(x => x.EndpointVersionID == versionId);

            if (entity == null) return null;

            var types = DataType.Text.GetDataTypeInfoList();
            var fields = Company.Query<dynamic>(@"select 
                                           eft.*,
                                           ft.Name,
                                           ft.Type
                                       from api.entityfieldtype eft
                                       inner join fieldtype ft on ft.id = eft.fieldtypeid                                    
                                       where eft.entityid = @id", new { id = entity.ID }).ToList();

            foreach (var field in fields)
            {
                var t = types.FirstOrDefault(x => x.Name == field.Type);

                if (t != null)
                    field.Type = t.Description;
            }

            return fields;

        }

        [Route("custom/version/{versionId:int}/uritypes")]
        public List<ApiEntityUri> GetCustomAPIVersionUriTypes(int versionId)
        {
            var entity = Company.ApiEntities.FirstOrDefault(x => x.EndpointVersionID == versionId);

            if (entity == null) return null;

            return Company.ApiEntityUris.Where(x => x.EntityID == entity.ID).ToList();
        }

        #endregion        
    }
}
