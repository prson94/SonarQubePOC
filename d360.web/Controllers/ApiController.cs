using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.helpers;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Xml.Linq;
using d360.core.resources;

namespace d360.web.Controllers
{
    [ApiVersion("1.0"), RoutePrefix("api"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class D3SApiController : BaseApiController
    {
        #region DI

        ISecurityContextProvider SecProvider;

        public D3SApiController(CommunityContext community, CompanyContext company, ISecurityContextProvider secProvider)
            : base(community, company)
        {
#if DEBUG
            company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
#endif
            SecProvider = secProvider;
        }

        #endregion

        #region Field Data

        void loadDisplayFields(List<DisplayField> list, SystemObjects type, int id)
        {
            var fields = Company.GetFieldRelationsByObject(type, id);
            foreach (var k in fields)
            {
                if (!string.IsNullOrEmpty(k.Value))
                {
                    list.Add(new DisplayField
                    {
                        FriendlyName = k.FriendlyName,
                        Value = k.FormattedValue,
                        Name = k.Name
                    });
                }
            }
        }

        public class TypeCheckModel
        {
            public string Type { get; set; }
            public int ID { get; set; }
        }
        
        List<DetailReadOnlyRowModel> loadDynamicDisplayFields(SystemObjects type, int id)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var details = Company.GetObjectDetail(type.ToString(), id);
            if (details != null)
            {
                var fields = Company.GetFieldRelationsByObject(type, id).ToList();
                var fieldTypes = Company.Filter<FieldType>(i => i.Object == details.Type && i.ObjectID == details.TypeID && i.IsDisplayable).OrderBy(i => i.ColumnOrder).ToList();

                fieldTypes.ForEach(ft =>
                {
                    var formattedValue = string.Empty;
                    var value = string.Empty;

                    var k = fields.SingleOrDefault(i => i.FieldTypeID == ft.ID);
                    if (k != null)
                    {
                        value = k.Value;
                        if (value == "0" && ft.AllowAllValue && ft.Type == DataType.Lookup.ToString())
                        {
                            formattedValue = ft.AllowAllLabel;
                        }
                        else
                        {
                            formattedValue = k.FormattedValue;
                        }
                        
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(ft.DefaultFormattedValue))
                        {
                            value = ft.DefaultValue;
                            formattedValue = ft.DefaultFormattedValue;
                        }
                    }

                    if (!string.IsNullOrEmpty(formattedValue))
                    {
                        if (ft.Type == DataType.FusionLookup.ToString())
                        {
                            if (k != null)
                            {
                                //look at fusionlookup field and figure out what to show
                                list.AddRange(RenderFusionLookupField(k));
                            }
                        }
                        else
                        {
                            var ro = new ReadOnlyField
                            {
                                Name = ft.FriendlyName,
                                Value = (ft.LookupDisplayFormat == formattedValue) ? "" : formattedValue,
                                FieldDescription = ft.DisplayDescription,
                                FieldName = ft.Name,
                                DataType = !string.IsNullOrEmpty(ft.Type) ? ft.Type : ""
                            };
                            
                            if (ft.Type == DataType.Date.ToString()) ro.DataType = "date";
                            else if (ft.Type == DataType.Boolean.ToString()) ro.DataType = "bool";
                            
                            if (!string.IsNullOrEmpty(ft.LookupObjectType) && ft.LookupObjectID.HasValue)
                            {
                                if(ft.AllowMultipleValues)
                                {
                                    ro.Values = new List<ReadOnlyFieldValue>();
                                    ro.Value = "values";
                                                                        
                                    var items = ((k != null) ? k.Value.Split(',') : new string[] { });
                                    var itemIds = new List<long>();

                                    foreach (var item in items)
                                    {
                                        if (long.TryParse(item, out long listId)) itemIds.Add(listId);
                                    }

                                    if (itemIds.Count > 0)
                                    {
                                        var lookupItems = Company.Query<FieldLookupValue>(@"select FieldTypeID, LookupObjectType, LookupObjectID, Value, Text from fieldlookupvalue where fieldtypeid = @fId and value in @vals order by Text", new { fId = ft.ID, vals = itemIds });

                                        if (lookupItems != null)
                                        {

                                            foreach (var item in lookupItems)
                                            {
                                                var detail = Company.GetObjectDetail(ft.LookupObjectType, item.Value);

                                                ro.Values.Add(new ReadOnlyFieldValue
                                                {
                                                    TooltipContext = "Preview",
                                                    TooltipID = item.Value,
                                                    Value = item.Text,
                                                    TooltipType = ft.LookupObjectType,
                                                    TooltipUrl = (detail == null ? "" : detail.NgUrl)
                                                });
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                   bool showPreviewLink = true;
                                    if (k != null)
                                    {
                                        if (k.Value == "0")
                                        {
                                            showPreviewLink = false;
                                        }
                                    }

                                    if (showPreviewLink)
                                    {                                        
                                        ro.TooltipContext = TemplateAction.LookupPreview.ToString();

                                        if (ft.LookupObjectType == "Lookup")
                                        {
                                            if (ft.LookupObjectID.HasValue)
                                            {
                                                ro.TooltipID = ft.LookupObjectID;
                                            }
                                            else
                                            {
                                                ro.TooltipID = 0;
                                            }
                                        }
                                        else
                                        {
                                            if(ft.LookupObjectType == "Artifact" || ft.LookupObjectType == "Taxonomy" || ft.LookupObjectType == "TaxonomyType")
                                                ro.TooltipContext = TemplateAction.Preview.ToString();

                                            if (string.IsNullOrEmpty(value))
                                            {
                                                ro.TooltipID = 0;
                                            }
                                            else
                                            {
                                                int textValue;
                                                if (int.TryParse(value, out textValue))
                                                {
                                                    ro.TooltipID = textValue;
                                                }
                                            }
                                        }

                                        ro.TooltipType = ft.LookupObjectType == "Lookup" ? SystemObjects.LookupType.ToString() : ft.LookupObjectType;
                                        if (k != null)
                                            ro.TooltipUrl = k.LookupUrl;
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
                    }
                    else
                    {
                        //Computed field.
                        if (ft.Type == DataType.FilteredLookup.ToString())
                        {
                            //look at fusionlookup field and figure out what to show
                            list.AddRange(RenderFilteredLookupField(type.ToString(), id, ft.ID));
                        }
                        if (ft.Type == DataType.Attribute.ToString())
                        {
                            //look at attribute field and figure out what to show
                            list.AddRange(RenderAttributeField(type.ToString(), id, ft.ID));
                        }
                        if (ft.Type == DataType.ComplexRelationLookup.ToString())
                        {
                            //look at fusionlookup field and figure out what to show
                            list.AddRange(RenderComplexLookupField(type.ToString(), id, ft.ID));
                        }
                        if (ft.Type == DataType.OwnershipLookup.ToString())
                        {
                            //look at fusionlookup field and figure out what to show
                            list.AddRange(RenderOwnershipLookupField(type.ToString(), id, ft.ID));
                        }

                        if (ft.Type == DataType.Relationship.ToString() && !string.IsNullOrEmpty(ft.LookupObjectType) && ft.LookupObjectID.HasValue)
                        {
                            var intersectTypeID = ft.LookupObjectID.Value;
                            var sType = type.ToString();
                            var values = new List<ReadOnlyFieldValue>();
                            var intersects = Company.Filter<IntersectDetail>(i => i.IntersectTypeID == intersectTypeID && ((i.Subject == sType && i.SubjectID == id) || (i.Object == sType && i.ObjectID == id))).OrderBy(x=>x.ObjectName);
                            if (intersects != null)
                            {
                                //load the current users permissions to these objects if they dont have access we cant show the link to let them go nowhere
                                var objectsToCheckAccesFor = new List<BasicAsset>();
                                
                                foreach(var intersect in intersects)
                                {
                                    var isSubject = (intersect.Subject == sType && intersect.SubjectID == id);
                                    
                                    var obj = isSubject ? intersect.Object : intersect.Subject;
                                    var objID = isSubject ? intersect.ObjectID : intersect.SubjectID;

                                    objectsToCheckAccesFor.Add(new BasicAsset { ObjectID = objID, ObjectName = obj });
                                }

                                var objectsWithoutReadAccess = GetObjectsWithoutReadAccess(objectsToCheckAccesFor);

                                foreach (var intersect in intersects)
                                {
                                    var isSubject = (intersect.Subject == sType && intersect.SubjectID == id);
                                    var intersectDisplayValue = isSubject ? intersect.ObjectName : intersect.SubjectName;
                                    var url = isSubject ? intersect.ObjectUrl : intersect.SubjectUrl;
                                    var obj = isSubject ? intersect.Object : intersect.Subject;
                                    var objID = isSubject ? intersect.ObjectID : intersect.SubjectID;

                                    if (obj == "Taxonomy")
                                    {
                                        var det = Company.Query<string>("select tp.TextPath from  asset a cross apply GetAssetTextPathById(a.id, '/') tp where a.[Object] = 'Taxonomy' and a.ObjectID = @id", new { id = objID }).FirstOrDefault();
                                        intersectDisplayValue = det;
                                    }


                                    if(objectsWithoutReadAccess != null && objectsWithoutReadAccess.Count > 0)
                                    {
                                        if(objectsWithoutReadAccess.Any(x=>(x.Object == obj && x.ObjectID == objID)))
                                        {
                                            url = null;
                                        }
                                    }
                                                                        
                                    values.Add(new ReadOnlyFieldValue { Value = intersectDisplayValue, TooltipContext = "Preview", TooltipID = objID, TooltipType = obj, TooltipUrl = url });                                    
                                }

                                values = values.OrderBy(x => x.Value).ToList();

                                var ro = new ReadOnlyField
                                {
                                    Name = ft.FriendlyName,
                                    Value = values.Count > 0 ?"values" : "",
                                    FieldDescription = ft.DisplayDescription,
                                    FieldName = ft.Name,                                    
                                    Values = values                                
                                };
                                
                                list.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField> { ro },
                                    Category = ft.Category
                                });
                            }
                        }

                        if (ft.Type == DataType.FieldFromRelationship.ToString() && !string.IsNullOrEmpty(ft.LookupObjectType) && ft.LookupObjectID.HasValue && ft.LookupObjectFieldTypeID.HasValue)
                        {
                            var intersectTypeID = ft.LookupObjectID.Value;
                            var fieldTypeID = ft.LookupObjectFieldTypeID.Value;
                            var sType = type.ToString();
                            var intersect = Company.Filter<Intersect>(i => i.IntersectTypeID == intersectTypeID && ((i.Subject == sType && i.SubjectID == id) || (i.Object == sType && i.ObjectID == id))).FirstOrDefault();
                            if (intersect != null)
                            {
                                var isSubject = (intersect.Subject == sType && intersect.SubjectID == id);
                                var obj = isSubject ? intersect.Object : intersect.Subject;
                                var objID = isSubject ? intersect.ObjectID : intersect.SubjectID;

                                var rfld = Company.Filter<Field>(i => i.FieldTypeID == fieldTypeID && i.ObjectType == obj && i.ObjectID == objID).SingleOrDefault();

                                if (rfld != null)
                                {
                                    var ro = new ReadOnlyField
                                    {
                                        Name = ft.FriendlyName,
                                        Value = rfld.FormattedValue,
                                        FieldDescription = ft.DisplayDescription,
                                        FieldName = ft.Name,
                                        DataType = "Html"
                                    };

                                    list.Add(new DetailReadOnlyRowModel
                                    {
                                        columns = 1,
                                        FirstColumnFields = new List<ReadOnlyField> { ro },
                                        Category = ft.Category
                                    });
                                }
                            }
                        }

                        if (ft.Type == DataType.RefListRelationship.ToString())
                        {
                            //look at fusionlookup field and figure out what to show
                            list.AddRange(RenderReferenceListItemsField(type.ToString(), id, ft.ID));
                        }
                    }
                });
            }


            return list;
        }

        private List<AssetWithoutReadPermission> GetObjectsWithoutReadAccess(List<BasicAsset> objectsToCheckAccesFor)
        {
            if (objectsToCheckAccesFor == null || objectsToCheckAccesFor.Count == 0) return new List<AssetWithoutReadPermission>();

            Dapper.DynamicParameters dbParams = new DynamicParameters();

            var dynamicSql = "";
            var indx = 0;
            foreach (var item in objectsToCheckAccesFor)
            {
                if (indx != 0) dynamicSql += " or ";

                dynamicSql += $"[object] = @obj{indx} and [objectid] = @objId{indx}";

                dbParams.Add($"obj{indx}", item.ObjectName);
                dbParams.Add($"objId{indx}", item.ObjectID);

                indx++;
            }
                        
            var sql = $@"select [Object], ObjectID from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @resId and ({dynamicSql})";

            dbParams.Add("resId", Company.CurrentResourceID);

            return Company.Query<AssetWithoutReadPermission>(sql, dbParams).ToList();
        }

        List<DetailReadOnlyRowModel> loadDisplayableRelationshipsAsFields(SystemObjects type, int id)
        {
            var list = new List<DetailReadOnlyRowModel>();
            var relationships = Company.GetDetailDisplayableRelationships(type, id);

            foreach (var k in relationships)
            {
                var ro = new ReadOnlyField
                {
                    Name = k.TargetTypeName,
                    Value = k.TargetObjectName,
                    FieldDescription = "",
                    FieldName = ""
                };
                if (k.Count > 0)
                {
                    ro.TooltipContext = TemplateAction.LookupPreview.ToString();
                    ro.TooltipID = k.TargetObjectID;
                    ro.TooltipType = k.TargetObject;
                    ro.TooltipUrl = k.TargetUrl;
                }

                list.Add(new DetailReadOnlyRowModel
                {
                    columns = 1,
                    FirstColumnFields = new List<ReadOnlyField> { ro }
                });
            }
            return list;
        }

        [Route("FieldTypes")]
        public IQueryable<FieldType> GetFieldTypes()
        {
            return Company.Table<FieldType>();
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

            switch (item.Type)
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
                                inner join reporting.Global_resource R on R.ResourceID = V.Value and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com'
                                where V.LookupObjectType = @lookupObjectType and V.LookupObjectID = @lookupObjectId
                                order by V.Text", new { lookupObjectId = item.LookupObjectID, lookupObjectType = item.LookupObjectType })
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
            }

            var width = item.ColumnWidth;
            if (!width.HasValue)
            {
                width = (int)dynamicFieldWidth;
            }
            var gc = new GridColumn { text = item.FriendlyName, datafield = useNameAsDataField ? $"{item.Name}" : $"Field{item.ID}", columntype = columnType, filtertype = filterType, filteritems = filterItems, cellsformat = cellsFormat, columnWidth = width, parentFieldTypeID = item.ParentFieldTypeID };
            if (!string.IsNullOrEmpty(item.Category))
            {
                gc.columngroup = item.Category.Replace(" ", "");
            }
            return gc;
        }

        string getGridFieldTypeForColumn(FieldType item)
        {
            string fieldType = "string";

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
            }

            return fieldType;
        }

        GridField getGridFieldForColumn(FieldType item, bool useNameAsDataField = false)
        {
            return new GridField { name = useNameAsDataField ? $"{item.Name}" : $"Field{item.ID}", type = getGridFieldTypeForColumn(item), apiName = item.Name };
        }

        void parseDynamicColumnsAndFields(List<FieldType> items, List<GridColumn> columns, List<GridField> fields, List<GridColumnGroup> groups, decimal dynamicFieldWidth, bool serverPaged = false)
        {
            items.ForEach(i =>
            {
                if (!string.IsNullOrEmpty(i.Category))
                {
                    groups.Add(new GridColumnGroup { align = "center", name = i.Category.Replace(" ", ""), text = i.Category });
                }
                columns.Add(getGridColumnForColumn(i, dynamicFieldWidth, serverPaged, false));

                fields.Add(getGridFieldForColumn(i));
            });
        }

        void parseDynamicFilterFields(List<FieldType> items, List<GridFilterColumn> columns, decimal dynamicFieldWidth, bool relatedField, bool hiddenField)
        {
            items.ForEach(i =>
            {
                GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, dynamicFieldWidth, true, false));

                col.id = i.ID.ToString();
                col.relatedfield = relatedField;
                col.hiddenfield = hiddenField;
                col.parentFieldTypeID = i.ParentFieldTypeID;

                columns.Add(col);

            });
        }

        [HttpGet, Route("{type}/{id:int}/grid/definition")]
        public HttpResponseMessage GetGridDefinitionByType(SystemObjects type, int id)
        {
            #region Resolve to underlying types
            switch (type)
            {
                case SystemObjects.Fusion:
                    var fusionType = Company.GetById<Fusion>(id);
                    if (fusionType != null)
                    {
                        type = SystemObjects.FusionType;
                        id = fusionType.FusionTypeID;
                        fusionType = null;
                    }
                    break;
            }
            #endregion

            var sType = type.ToString();
            var totalItems = Company
                .Filter<FieldType>(i => i.Object == sType && i.ObjectID == id && !CalculatedFieldTypes.Contains(i.Type))
                .ToList();
            var items = totalItems.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

            var columns = new List<GridColumn>();
            var fields = new List<GridField>();
            var filterColumns = new List<GridFilterColumn>();
            var groups = new List<GridColumnGroup>();
            var topLevelFilterFields = new List<GridFilterColumn>();
            decimal dynamicFieldWidth = 0;
            int remainingWidth = 0;            
            ObjectDetail detail = null;
            bool isReadOnly = false;

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    #region
                                        
                    var hasParentType = Company.TypeHasParent(SystemObjects.ArtifactType, id);

                    parseDynamicColumnsAndFields(items, columns, fields, groups, 0, true);

                    if (hasParentType)
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
                    if (hasParentType)
                    {
                        fields.Add(new GridField { name = "ParentID", type = "number" });
                        fields.Add(new GridField { name = "Parent", type = "string" });
                        fields.Add(new GridField { name = "ParentUrl", type = "string" });
                    }
                    fields.Add(new GridField { name = "Url", type = "string" });


                    filterColumns.AddRange(columns.Select(p => new GridFilterColumn(p)));

                    //clear the filtercolumns of the columns since they are not used and copied to the filtercolumns
                    foreach (var column in columns)
                    {
                        column.filteritems = new List<string>();
                    }

                    var hiddenItems = totalItems.Where(i => i.Type != "FusionLookup" && i.Type != "RelationLookup" && !i.IsListable).OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName).ToList();
                    parseDynamicFilterFields(hiddenItems, filterColumns, 0, false, true);

                    //Load any fields that are displayed on relationships so we can show them as 
                    // filters in the grid
                    IEnumerable<int> intersectTypeIDs = Company.Query<int>("select ID from [IntersectType] where (Subject = 'ArtifactType' and SubjectID = @objectid) OR (Object = 'ArtifactType' and ObjectID = @objectid)", new { objectid = id });

                    if (intersectTypeIDs.Any())
                    {
                        var totalRelItems = Company.Filter<FieldType>(i => i.Object == "IntersectType" && intersectTypeIDs.Contains(i.ObjectID)).ToList();
                        var relItems = totalRelItems.Where(i => i.IsListable).OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName).ToList();

                        if (relItems.Any())
                        {
                            parseDynamicFilterFields(relItems, filterColumns, 0, true, false);
                        }
                    }

                    filterColumns = filterColumns.OrderBy(x => x.text).ToList();

                    //Load any field types that are top level filter fields
                    var topFiltersHidden = totalItems.Where(i => i.IsPrimaryFilter).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

                    topFiltersHidden.ForEach(i =>
                    {
                        GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, 0, true));

                        col.id = i.ID.ToString();
                        col.relatedfield = false;
                        col.hiddenfield = !i.IsListable;

                        topLevelFilterFields.Add(col);

                    });

                    break;
                #endregion
                case SystemObjects.IntersectType:
                    #region

                    var intersectType = Company.GetById<IntersectType>(id);

                    if(intersectType != null && intersectType.Predicate != null)
                    {
                        isReadOnly = !intersectType.Predicate.Type.AsInfoModel().AllowEditFromRelationshipEditor;
                    }
                    var targetType = Request.GetQueryString("target");
                    var targetTypeID = Request.GetQueryString("targetID");

                    if (!string.IsNullOrEmpty(targetType) && !string.IsNullOrEmpty(targetTypeID))
                    {
                        var ttID = int.Parse(targetTypeID);
                        var targetKeyFields = Company.Filter<FieldType>(i => i.Object == targetType && i.ObjectID == ttID && i.IsPartOfKey).OrderBy(i => i.SortOrder).ToList();
                        items.InsertRange(0, targetKeyFields);
                    }

                    if (targetType == SystemObjects.ReferenceItemType.ToString() || targetType == SystemObjects.FusionAttributeType.ToString() || targetType == SystemObjects.ResourceType.ToString())
                    {
                        columns.Add(
                            new GridColumn { text = "Name", datafield = "Name", columntype = GridColumn.COLUMN_TYPE_STRING, filtertype = GridColumn.FILTER_TYPE_STRING }
                        );
                    }
                    
                    
                    remainingWidth = 80;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "ObjectID", type = "number" });
                    fields.Add(new GridField { name = "Object", type = "string" });
                    fields.Add(new GridField { name = "TypeID", type = "number" });
                    fields.Add(new GridField { name = "Type", type = "string" });
                    fields.Add(new GridField { name = "TypeName", type = "string" });
                    fields.Add(new GridField { name = "Url", type = "string" });
                    fields.Add(new GridField { name = "HasTechnicalRelationships", type = "bool" });
                    fields.Add(new GridField { name = "HasAttributes", type = "bool" });
                    break;
                #endregion
                case SystemObjects.LookupType:
                    #region
                    
                    remainingWidth = 90;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "LookupTypeID", type = "number" });
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region
                    
                    remainingWidth = 45;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "ParentID", type = "number" });
                    fields.Add(new GridField { name = "PolicyTypeID", type = "number" });
                    break;
                #endregion                                
                case SystemObjects.ReferenceItemType:
                    #region
                    
                    remainingWidth = 85;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Code_Name, datafield = "Code" });

                    var parentRefType = Company.GetParentType(id, SystemObjects.ReferenceItemType);
                    var loopCount = 0;
                    //add the parent columns
                    while (parentRefType != null && loopCount < 20)
                    {
                        columns.Insert(0,new GridColumn { text = parentRefType.Name, datafield = $"Rel{parentRefType.ObjectID}" });

                        parentRefType = Company.GetParentType(parentRefType.ObjectID, SystemObjects.ReferenceItemType);
                        loopCount++;
                    }

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Code", type = "string" });
                    fields.Add(new GridField { name = "ReferenceItemType", type = "number" });
                    break;
                #endregion
                case SystemObjects.Rule:
                    #region
                    
                    remainingWidth = 55;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = "Date", datafield = "Date", columntype = GridColumn.COLUMN_TYPE_DATE, filtertype = GridColumn.FILTER_TYPE_RANGE, cellsformat = "MM/dd/yyyy HH:mm:ss" });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    columns.Add(new GridColumn { text = "Criticality", datafield = "Criticality", columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_CHECKEDLIST });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.SourceID_Name, datafield = "SourceID" });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Status_Name, datafield = "Status", columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_LIST, filteritems = new List<string>() { "Assigned", "Open", "Closed" } });

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Date", type = "date" });
                    fields.Add(new GridField { name = "Criticality", type = "string" });
                    fields.Add(new GridField { name = "SourceID", type = "string" });
                    fields.Add(new GridField { name = "Rule", type = "string" });
                    fields.Add(new GridField { name = "Status", type = "string" });
                    break;
                #endregion
                case SystemObjects.RuleType:
                    #region

                    remainingWidth = 45;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Name_Name, datafield = "Name" });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "RuleTypeID", type = "number" });
                    break;
                #endregion  
                case SystemObjects.FusionAttributeType:
                    #region
                    
                    remainingWidth = 75;

                    detail = Company.GetObjectDetail(type.ToString(), id);

                    #region Parents

                    var parentSql = @"
with h as	(
			select	ID,
					ParentID,
                    Name,
					0 as [Level]
			from	FusionAttributeType
			where	ID = @t
			union all
			select	P.ID,
					P.ParentID,
                    P.Name,
					C.[Level] + 1 as [Level]
			from	FusionAttributeType P
					inner join h as C on C.ParentID = P.ID
			)

select  T.* 
from    h
        inner join FusionAttributeType T on T.ID = h.ID 
where   h.ID <> @t order by h.[Level] desc;
";
                    var parents = Company.Query<FusionAttributeType>(parentSql, new { t = id }).ToList();

                    int fusionID = 0;
                    bool fusionIDPresent = false;
                    if (!string.IsNullOrEmpty(Request.GetQueryString("targetID")))
                    {
                        fusionIDPresent = int.TryParse(Request.GetQueryString("targetID"), out fusionID);
                    }

                    //Parent columns have be listed in DESC order by Level.
                    parents.ForEach(i =>
                    {
                        if (fusionIDPresent)
                        {
                            var parentFilterValues = Company.Query<string>(@"select Name from FusionAttribute where FusionID = @f and FusionAttributeTypeID = @t and Deleted = 0 group by Name order by Name", new { f = fusionID, t = i.ID }).ToList();
                            filterColumns.Add(new GridFilterColumn { text = i.Name, datafield = $"Parent{i.ID}", filtertype = GridColumn.COLUMN_TYPE_DROPDOWN, columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filteritems = parentFilterValues });
                            columns.Add(new GridColumn { text = i.Name, datafield = $"Parent{i.ID}", filteritems = new List<string>() });
                        }
                        else
                        {
                            columns.Add(new GridColumn { text = i.Name, datafield = $"Parent{i.ID}", filteritems = new List<string>() });
                        }
                        fields.Add(new GridField { name = $"Parent{i.ID}", type = "string" });
                    });

                    #endregion

                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    filterColumns.Add(new GridFilterColumn { text = "ID", datafield = "ID", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
                    filterColumns.Add(new GridFilterColumn { text = detail.Name, datafield = "Name", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });                    
                    columns.Add(new GridColumn { text = detail.Name, datafield = "Name", filteritems = new List<string>() });
                    fields.Add(new GridField { name = "AssetID", type = "number" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth);

                    items.ForEach(i =>
                    {
                        GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, dynamicFieldWidth, true));

                        col.id = i.ID.ToString();
                        col.relatedfield = false;
                        col.hiddenfield = false;

                        filterColumns.Add(col);
                    });

                    break;
                #endregion
                case SystemObjects.FusionQueryAttributeType:
                    #region
                    
                    remainingWidth = 90;

                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    filterColumns.Add(new GridFilterColumn { text = "ID", datafield = "ID", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
                    fields.Add(new GridField { name = "ID", type = "number" });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth);

                    items.ForEach(i =>
                    {
                        GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, dynamicFieldWidth, true));

                        col.id = i.ID.ToString();
                        col.relatedfield = false;
                        col.hiddenfield = false;

                        filterColumns.Add(col);
                    });
                    break;
                #endregion
                case SystemObjects.FusionType:
                    #region
                    
                    remainingWidth = 61;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Name_Name, datafield = "Name" });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Enabled_Name, columntype = GridColumn.COLUMN_TYPE_CHECKBOX, filtertype = GridColumn.FILTER_TYPE_CHECKBOX, datafield = "Enabled" });
                    columns.Add(new GridColumn { text = "Owners", columntype = GridColumn.COLUMN_TYPE_STRING, filtertype = GridColumn.COLUMN_TYPE_STRING, datafield = "Owners" });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "Enabled", type = "boolean" });
                    fields.Add(new GridField { name = "Owners", type = "string" });
                    break;
                #endregion
                case SystemObjects.ResourceType:
                    #region
                    
                    remainingWidth = 27;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.FirstName_Name, datafield = "FirstName" });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.LastName_Name, datafield = "LastName" });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Email_Name, datafield = "Email" });
                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth);
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.LastLoggedInOn_Name, datafield = "LastLoggedInOn", filtertype = GridColumn.FILTER_TYPE_RANGE, cellsformat = "F" });
                    columns.Add(new GridColumn { text = "Administrator?", datafield = "IsAdministrator", columntype = GridColumn.COLUMN_TYPE_CHECKBOX, filtertype = GridColumn.FILTER_TYPE_CHECKBOX });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Status_Name, datafield = "State", filtertype = GridColumn.FILTER_TYPE_CHECKEDLIST, filteritems = new List<string>() {
                        CompanyResourceState.Active.ToString(),
                        CompanyResourceState.Inactive.ToString()
                    } });

                    fields.Add(new GridField { name = "IsAdministrator", type = "bool" });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Email", type = "string" });
                    fields.Add(new GridField { name = "FirstName", type = "string" });
                    fields.Add(new GridField { name = "LastName", type = "string" });
                    fields.Add(new GridField { name = "LastLoggedInOn", type = "date" });
                    fields.Add(new GridField { name = "State", type = "string" });
                    break;
                #endregion
                case SystemObjects.TaxonomyType:
                    #region TaxonomyType
                    {
                        var taxonomyFields = Company.Filter<FieldType>(i => i.Object == "TaxonomyType" && i.ObjectID == id && i.IsListable).OrderBy(i => i.SortOrder).ToList();

                        foreach (var field in taxonomyFields)
                        {
                            columns.Add(getGridColumnForColumn(field, 0, false, useNameAsDataField: false));
                            fields.Add(getGridFieldForColumn(field, useNameAsDataField: false));
                        }

                        fields.Add(new GridField { name = "AssetID", type = "number" });
                        fields.Add(new GridField { name = "ID", type = "number" });
                        fields.Add(new GridField { name = "ParentID", type = "number" });
                        fields.Add(new GridField { name = "TaxonomyTypeID", type = "number" });
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
                ColumnGroups = groups,
                TopLevelFilterColumns = topLevelFilterFields,
                IsReadOnly = isReadOnly
            });
        }

        
        [HttpGet, Route("{type}/{id:int}/grid/definition/parentValues")]
        public async Task<HttpResponseMessage> GetGridParentFilterItems(string type, int id)
        {
            if ((type ?? "").ToUpper() == "ARTIFACTTYPE")
            {
                var parentType = Company.GetParentType(id, SystemObjects.ArtifactType);

                if(parentType == null) return Request.CreateErrorResponse(HttpStatusCode.NotFound, new Exception("parent"));

                var res = await Company.QueryAsync<string>("select ADV.DisplayValue from AssetDisplayValue ADV inner join Asset A on (A.ID = ADV.AssetID) inner join AssetType ATT on (A.AssetTypeID = ATT.ID) where ATT.[Object] = 'ArtifactType' and ATT.[ObjectID] = @objID order by 1", new { objID = parentType.ObjectID });

                return Request.CreateResponse(HttpStatusCode.OK, res);
            }

            return Request.CreateErrorResponse(HttpStatusCode.NotFound, new Exception("parent"));
        }

        [HttpGet, Route("{type}/{id:int}/grid/definition/filterValues/{fieldTypeId:int}")]
        public HttpResponseMessage GetGridFilterItems(int fieldTypeId)
        {
            var ft = Company.GetById<FieldType>(fieldTypeId);
            if (ft == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, new Exception("field type"));

            var gridColumn = getGridColumnForColumn(ft, 0, false, true);
            return Request.CreateResponse(HttpStatusCode.OK, gridColumn.filteritems);
        }

        #endregion

        #region Navigation


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

        #region Attributes

        [HttpGet, Route("{type}/{id:int}/attributetypefilters")]
        public HttpResponseMessage GetFilterableAttributeTypesByType(SystemObjects type, int id)
        {
            var models = Company.Query<dynamic>(QueryConstants.FilterableAttributeTypesByTypeList, new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id });
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        [HttpGet, Route("{type}/{id:int}/{attributeTypeID:int}/attributefiltervalues")]
        public HttpResponseMessage GetFilterableAttributeValues(SystemObjects type, int id, int attributeTypeID)
        {
            var models = Company.Query<dynamic>(QueryConstants.FilterableAttributeValuesList, new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id, attributeTypeID });
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        #endregion

        #region Artifacts

        [Route("artifact/{id:int}")]
        public HttpResponseMessage GetArtifact(int id)
        {
            var json = Company.GetPageInformation(SystemObjects.Artifact, id);

            if (json == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, json);
            }

            var isPluralize = PluralCultureHelper.IsNeutralCultureEnglish();

            foreach (var br in json["Breadcrumbs"].Children())
            {
                if (br["IsType"].ToObject<bool>())
                {
                    if (isPluralize)
                    {
                        var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                        br["Name"] = pluralize.Pluralize(br["Name"].Value<string>());
                        pluralize = null;
                    } else
                    {
                        br["Name"] = "";
                    }
                    
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, json);
        }

        [Route("artifacts/{typeID:int}")]
        public Dictionary<string, object> GetArtifactType(int typeID)
        {
            var artifactType = Company.GetById<ArtifactType>(typeID);
            if (artifactType == null) throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

            var assetType = Company.Filter<AssetType>(i => i.Object == "ArtifactType" && i.ObjectID == artifactType.ID).SingleOrDefault();
            if (assetType == null) throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

            var model = new Dictionary<string, object>();

            model.Add("ID", artifactType.ID);
            model.Add("Name", artifactType.Name);
            model.Add("Description", artifactType.Description);
            model.Add("ParentID", Company.GetParentType(artifactType.ID, SystemObjects.ArtifactType)?.ObjectID ?? null);
            model.Add("CanOwnFusion", assetType.CanOwnFusion);
            model.Add("HasCustomExportTemplates", Company.AssetTypeExportTemplates.Where(x => x.AssetTypeID == assetType.ID).Any());
            model.Add("AutoDisplayDescription", assetType.AutoDisplayDescription);

            bool hasDashboards = Company.Filter<Report>(x => x.ObjectType == "ArtifactType" && x.ObjectID == typeID && x.ReportType != "legacy").Any();
            model.Add("HasDashboards", hasDashboards);

            var sql = $"select count(1) from [workflow].[EventRegistration] where [object] = 'ArtifactType' and [objectId] = {typeID}";

            var hasV2WorkflowsAssigned = (Company.Query<int>(sql).FirstOrDefault() > 0);
            model.Add("HasV2Workflows", hasV2WorkflowsAssigned);
            model.Add("AssetTypeUID", assetType.uid);

            return model;
        }

        [Route("artifacttypes")]
        public IEnumerable<ArtifactType> GetArtifactTypes()
        {
            var artifactTypes = Company.Table<ArtifactType>().ToList().OrderBy<ArtifactType,string>(x=>x.Name);

            //get intersecttypes for intra parent so we can determine parents

            var artifactParentRelations = Company.IntersectTypeDetails.Where(x => x.PredicateType == PredicateType.InterTypeHierarchy && x.Object == "ArtifactType" && x.Subject == "ArtifactType").ToList();
           
            foreach (var artifactType in artifactTypes)
            {              
                var parents = artifactParentRelations.Where(x => x.ObjectID == artifactType.ID).FirstOrDefault();

                if(parents != null)
                {
                    artifactType.ParentID = parents.SubjectID;
                }
            }

            return artifactTypes;
        }

        [Route("artifacts/{id:int}/{take:int?}")]
        public ArtifactModelRequestList GetArtifacts(int id, int take = 10)
        {
            var list = new ArtifactModelRequestList();
            var items = Company.Filter<Artifact>(i => i.ArtifactTypeID == id).AsQueryable();            
            var lItems = items.Take(take).ToList();

            var IDs = lItems.Select(i => i.ID).ToList();

            var sType = SystemObjects.Artifact.ToString();
            var values = Company.Filter<FieldWithRelation>(i => i.ObjectType == sType && IDs.Contains(i.ObjectID)).ToList();

            foreach (var item in lItems)
            {
                var listItem = new ArtifactModelRequest();

                //Static fields
                listItem.Add("ID", item.ID);

                // Dynamic fields
                foreach (var f in values.Where(i => i.ObjectID == item.ID).OrderBy(i => i.Name))
                {
                    listItem.Add(f.Name, f.FormattedValue);
                };

                // Add to list
                list.Add(listItem);
            }
           
            return list;
        }

        [Route("artifacttype/possibleowners/{artifactTypeId:int}")]
        public HttpResponseMessage GetArtifactTypePossibleOwners(int artifactTypeId)
        {
            var sql = @"
select  distinct 
	    cast(ResponsibilityTypeID as varchar) + '|' + cast(SecurityAssetID as varchar) as 'ID', 
	    '[' + ResponsibilityTypeName + '] - ' + SecurityAssetName  as 'Name', 
	    case 
            when SecurityAsset = 'R' or SecurityAsset = 'O' then 'Resource' 
			when SecurityAsset = 'G' then 'Group' 
            else [Type] 
        end as [Type]
from    ResponsibilityDetail
where   TypeID = @id 
        and [Type] = 'ArtifactType' 
        and IsVisible = 1 
order by 'Name'";

            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company.Query<dynamic>(
                    sql,
                    new { id = artifactTypeId }
                )
            );
        }

        #endregion

        #region Followers

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

        #region Fusion

        [Route("fusion")]
        public IQueryable<FusionType> GetFusionTypes()
        {
            return Company.Table<FusionType>();
        }

        [Route("fusion/{typeID:int}")]
        public FusionType GetFusionType(int typeID)
        {
            var model = Company.GetById<FusionType>(typeID);

            if (model == null)
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

            return model;
        }

        [Route("fusion/{typeID:int}/configurations")]
        public IQueryable<Fusion> GetFusionConfigurationsByType(int typeID)
        {
            return Company.Filter<Fusion>(i => i.FusionTypeID == typeID);
        }

        [Route("fusion/{typeID:int}/configurations/{id:int}")]
        public Fusion GetFusionConfiguration(int typeID, int id)
        {
            return Company.GetById<Fusion>(id, i => i.FusionType);
        }

        [Route("fusion/{fusionAttributeID:int}/configurations/fromFusionAttribute")]
        public HttpResponseMessage GetFusionConfigurationFromFusionAttribute(int fusionAttributeID)
        {
            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company.Query<dynamic>(
                    QueryConstants.FusionConfigurationFromFusionAttributeItem,
                    new { id = fusionAttributeID }
                )
            );
        }

        [Route("fusion/selectedbreadcrumb/{selectedItemID:int}")]
        public HttpResponseMessage GetSelectedFusionBreadcrumb(int selectedItemID)
        {
            var itemPathData = new List<dynamic>();

            int itemID = selectedItemID;

            while (itemID > 0)
            {
                var currentItem = Company.Query<dynamic>(
                    QueryConstants.FusionBreadcrumbItem,
                    new { item = itemID }
                ).FirstOrDefault();

                if (currentItem == null) throw new Exception("invalid item id specified to generate breadcrumb from");

                itemID = currentItem.parentID ?? default(int);
                itemPathData.Insert(0, currentItem);
            }

            return Request.CreateResponse(HttpStatusCode.OK, itemPathData);
        }

        [Route("fusion/ownership/ChildAttributeNodes"), HttpGet]
        public HttpResponseMessage GetOwnershipChildAttributeNodes(int fusionID, int targetFusionAttributeTypeID, int ruleID, int currentFusionAttributeTypeID = 0, int fusionAttributeID = 0)
        {
            var models = Company.Query<dynamic>(
                QueryConstants.FusionOwnershipChildAttributeNodeList,
                new
                {
                    fusionID,
                    targetFusionAttributeTypeID,
                    ruleID,
                    currentFusionAttributeTypeID,
                    fusionAttributeID
                }
            );

            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        [Route("fusion/promotion/ChildAttributeNodes"), HttpGet]
        public HttpResponseMessage GetPromotionChildAttributeNodes(int fusionID, int targetFusionAttributeTypeID, int ruleID, int currentFusionAttributeTypeID = 0, int fusionAttributeID = 0)
        {
            var models = Company.Query<dynamic>(
                QueryConstants.FusionPromotionChildAttributeNodeList,
                new
                {
                    fusionID,
                    targetFusionAttributeTypeID,
                    ruleID,
                    currentFusionAttributeTypeID,
                    fusionAttributeID
                }
            );

            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        [Route("fusion/promotion/QueryAttributes"), HttpGet]
        public HttpResponseMessage GetPromotionFusionQueryAttributes(int ruleID)
        {
            var results = Company.Query<dynamic>(@"select f.id, f.[name], f.friendlyName from fieldtype f
                join fusion.[rule] r on r.id = @ruleID and f.[object] = r.[objecttype] and f.objectid = r.objectid", new { ruleID });
            return Request.CreateResponse(HttpStatusCode.OK, results);

        }

        [Route("fusion/{typeID:int}/configurations/{id:int}/filters")]
        public HttpResponseMessage GetFilterByFusion(int typeID, int id)
        {
            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company
                .Filter<FusionFilter>(i => i.FusionID == id, i => i.FusionAttributeType)
                .Select(i => new
                {
                    i.Filter,
                    i.FusionAttributeTypeID,
                    i.FusionID,
                    Name = i.FusionAttributeType.Name
                })
            );
        }
        
        [Route("fusion/{fusionID:int}/rules")]
        public HttpResponseMessage GetFusionRules(int fusionID)
        {
            var sql = @"select 
                        r.id as ID,
	                    r.[description] as Description,
	                    r.[enabled] as Enabled,
	                    r.fusionid as FusionID,
	                    r.objecttype as ObjectType,
	                    r.objectid as ObjectID,
						case when r.objecttype = 'FusionQueryAttributeType' then
							'Query :: ' + fqat.Name
						else
							fat.Textpath
						end as ObjectName
                    from[fusion].[Rule]
                            r
                        left outer join [dbo].fusionattributetype fat on (r.objectid = fat.id and r.objecttype ='FusionAttributeType')
                        left outer join [dbo].fusionqueryattributetype fqat on (r.objectid = fqat.id and r.objecttype ='FusionQueryAttributeType')                       
                    where r.fusionid = @fid";


            var dbArgs = new Dapper.DynamicParameters();

            dbArgs.Add("fid", fusionID);

            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company.Query<dynamic>(sql, dbArgs)
            );
        }

        [Route("fusion/rules/{ruleID:int}/steps")]
        public IEnumerable<FusionRuleStep> GetFusionRuleSteps(int ruleID)
        {
            var rule = Company.GetById<FusionRule>(ruleID);

            if (rule == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            return rule.FusionRuleSteps.OrderBy(x => x.Step);
        }

        [Route("fusion/rule/{ruleID:int}/steps/{ruleStepID:int}")]
        public IEnumerable<dynamic> GetRuleSteps(int ruleID, int ruleStepID)
        {
            return Company.Filter<FusionRuleStep>(x => x.RuleID == ruleID && x.ID != ruleStepID)
                .Select(i => new { Step = i.Step, Description = i.Description, ID = i.ID })
                .AsEnumerable()
                .Select(y => new { Description = $"{y.Step} - {y.Description}", ID = y.ID });
        }

        [Route("fusion/rule/actions")]
        public IEnumerable<dynamic> GetActions()
        {
            var types = new List<dynamic>();

            types.Add(new { Name = FusionRuleType.Promote.ToString(), ID = FusionRuleType.Promote.ToString().ToLower() });
            types.Add(new { Name = FusionRuleType.Find.ToString(), ID = FusionRuleType.Find.ToString().ToLower() });
            types.Add(new { Name = FusionRuleType.Relate.ToString(), ID = FusionRuleType.Relate.ToString().ToLower() });

            return types;
        }

        [Route("fusion/rule/fusionattributetypes")]
        public IQueryable GetFusionAttributeTypes()
        {
            return Company.FusionAttributeTypes.OrderBy(x => x.TextPath).Select(x => new { Name = x.TextPath, ID = x.ID });
        }

        [Route("fusion/rule/fusionOwners/{fusionID:int}")]
        public IEnumerable<dynamic> GetRuleFusionOwners(int fusionID)
        {
            var sql = @"
                      with cte as (

					select	b.objectId AS ID,
			                I.SubjectID as ParentID,
			                b.TypeID,
			                d.DisplayValue
	                from	FusionOwner fo
						 inner join AssetDetail b on
							 b.ID = fo.AssetID
			                left join IntersectTypeDetail ITD on ITD.PredicateType = 3 and ITD.[Object] = 'ArtifactType' 
				                and ITD.ObjectID = b.TypeID and ITD.[Subject] = 'ArtifactType'
			                left join [Intersect] I on I.IntersectTypeID = ITD.ID and I.ObjectID = b.ObjectID
			                cross apply dbo.GetAssetDisplayValueById(b.ID) d
	                where	fo.fusionID = @fusionID
	                union all
	                select	c.ObjectID,
			                x.SubjectID as ParentID,
			                c.TypeID,
			                d.DisplayValue
	                from	AssetDetail c
			                outer apply (
				                select I.* from [Intersect] I
				                inner join IntersectTypeDetail ITD on ITD.PredicateType = 3 and ITD.[Object] = 'ArtifactType' 
					                and ITD.ObjectID = c.TypeID and ITD.[Subject] = 'ArtifactType'
					                and I.IntersectTypeID = ITD.ID
				                where I.ObjectID = c.ObjectID
			                ) x
			                cross apply dbo.GetAssetDisplayValueById(c.ID) d
			                inner join ArtifactType ct on ct.ID = c.TypeID and ct.CanOwnFusion = 1
			                inner join cte p on p.ID = c.ObjectID
                )

                select	a.ID,
		                t.Name + ': ' + a.DisplayValue as Name
                from	cte a
		                inner join ArtifactType t on t.ID = a.TypeID";

            return Company.Query<dynamic>(sql, new { fusionID });
        }

        [Route("fusion/rule/relate/intersectTypes")]
        public IQueryable GetIntersectTypes()
        {
            return Company.Filter<IntersectTypeDetail>(x => 
                x.SubjectID > 0 && 
                x.ObjectID > 0 && 
                !string.IsNullOrEmpty(x.Subject) && 
                !string.IsNullOrEmpty(x.Object) && 
                !x.IsSystem)
                .OrderBy(x => x.SubjectName)
                .ThenBy(x => x.ObjectName)
                .ToList()
                .Select(x => new {
                    Name = $"{x.SubjectName} {x.PredicateName ?? " / "} {x.ObjectName}",
                    x.ID,
                    x.Subject,
                    x.SubjectID,
                    x.Object,
                    x.ObjectID
                }).OrderBy(x => x.Name).AsQueryable();
        }

        [Route("fusion/rule/relate/objectTypes")]
        public IEnumerable<dynamic> GetDirectObjectRelateTypes()
        {
            return Company.GetIntersectTypeOptions()
                .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });
        }
        
        [Route("fusion/technicalmapping")]
        public IQueryable<MapRuleItemDetail> GetFusionTechnicalMappings() 
        {            
            return Company.Table<MapRuleItemDetail>();
        }

        [Route("fusion/fusionowningartifacts")]
        public IEnumerable<Artifact> GetArtifactsOwningFusion()
        {
            return Company.Filter<Artifact>(i => i.ArtifactType.CanOwnFusion == true);
        }

        [Route("fusion/textpathautocomplete")]
        public IEnumerable<string> GetFusionTextpathsAutocomplete(string startsWith, int maxRows)
        {
            return (from fusionAttribute in Company.FusionAttributes
                    where fusionAttribute.TextPath.StartsWith(startsWith)
                    select fusionAttribute.TextPath).Take(maxRows).AsEnumerable();
        }

        #region Owner

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/ownership/options")]
        public List<FusionOwnerOption> GetFusionOwnerOptions(int typeID, int fusionID)
        {
            return Company.GetFusionOwnerOptions();
        }

        #endregion

        #region Promotion

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/promotion/options")]
        public List<FusionPromotionOption> GetFusionPromotionOptions(int typeID, int fusionID)
        {
            return Company.GetFusionPromotionOptions();
        }

        [Route("fusion/{id:int}/FusionRuleFilters")]
        public HttpResponseMessage GetFusionRuleFilters(int id)
        {
            var list = Company.Filter<FusionRuleFilter>(i => i.RuleID == id).OrderBy(i => i.Name).ToList();

            if (list.Count >= 0)
            {
                var fieldTypeIDs = (
                    from f in list
                    from fld in f.FieldsDocument.Elements("field")
                    where fld.Element("FieldTypeID").Value != "0"
                    select int.Parse(fld.Element("FieldTypeID").Value)
                    ).ToList();

                var fieldTypes = Company.Filter<FieldType>(i => fieldTypeIDs.Contains(i.ID))
                    .Select(i => new { i.ID, i.FriendlyName, i.Name, i.Type})
                    .ToList()
                    .Select(i => new { i.ID, Name = $"{i.FriendlyName} ({i.Name})", i.Type })
                    .ToList();

                list.ForEach(f =>
                {
                    foreach (var e in f.FieldsDocument.Elements("field"))
                    {
                        var fld = new FusionRuleFilterItem
                        {
                            FieldTypeID = int.Parse(e.Element("FieldTypeID").Value),
                            FusionRuleFilterID = f.ID,
                            Operator = e.Element("Operator").Value,
                            Value = e.Element("Value").Value
                        };

                        if (fld.FieldTypeID > 0)
                        {
                            var ft = fieldTypes.FirstOrDefault(i => i.ID == fld.FieldTypeID);
                            fld.Type = (ft != null) ? ft.Type : "Text";
                        }

                        f.Items.Add(fld);
                    }

                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }

        [Route("fusion/{id:int}/FusionRuleItems")]
        public HttpResponseMessage GetFusionRuleItems(int id)
        {
            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company.Query<dynamic>(QueryConstants.FusionRuleItemList, new { id })
            );
        }

        [Route("fusion/{id:int}/FusionRuleStepMappings")]
        public HttpResponseMessage GetFusionRuleMappings(int id)
        {
            var rulestep = Company.GetById<FusionRuleStep>(id);
            IEnumerable<string> unMappedColumns = new List<string>();
            if (rulestep?.Action.ToUpper() == "PROMOTE")
            {
                 unMappedColumns = Company.Query<string>(QueryConstants.FusionRuleUnmappedKeyColumnList,
                    new
                    {
                        obj = rulestep.GetSettingValueByName("Object"),
                        objId = Convert.ToInt32(rulestep.GetSettingValueByName("ObjectID")),
                        ruleStepId = id
                    });
            }

           
            return Request.CreateResponse(
                HttpStatusCode.OK, new {Items = Company.Query<dynamic>(QueryConstants.FusionRuleMappingList, new { id }), UnMappedKeyColumns = unMappedColumns }
               
            );
        }

        #endregion

        #endregion

        #region Groups

        [HttpGet, Route("groups")]
        public IQueryable<GroupSearchResultModel> GetGroups()
        {
            return Company.Table<Group>()
                .OrderBy(i => i.Name)
                .Select(i => new GroupSearchResultModel
                {
                    ID = i.ID,
                    Name = i.Name,
                    NumberOfMembers = i.ResourceGroups.Count,
                    IsMember = i.ResourceGroups.Any(r => r.ResourceID == Company.CurrentResourceID)
                });
        }

        [Route("groups/{id:int}")]
        public Group GetGroup(int id)
        {
            return Company.GetById<Group>(id);
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
                new { id =id , userStatus  = CompanyResourceState.Active}
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

        #endregion

        #region Lookup Methods

        [Route("AttributeTypeCategories")]
        public IQueryable<AttributeTypeCategory> GetAttributeTypeCategories()
        {
            return Company.Table<AttributeTypeCategory>();
        }

        [Route("lookups/{id:int}/allocations")]
        public IEnumerable<dynamic> GetAllocationsByLookupType(int id)
        {
            return Company.Query<dynamic>(QueryConstants.LookupAllocations, new { type = "Lookup", id });
        }

        [Route("fusionlookup/list/{id:int}"), HttpGet]
        public HttpResponseMessage GetFusionLookupList(int id)
        {
            var field = Company.FieldTypes.Find(id);
            var ids = Company.Filter<FieldTypeFusionLookupDefinition>(x => x.FieldTypeID == id).Select(i => i.SourceFusionAttributeTypeID).Distinct().ToList();

            var list = Company.Filter<FusionAttribute>(x => ids.Contains(x.FusionAttributeTypeID), i => i.FusionAttributeType)
                   .Select(i => new { value = i.ID.ToString(), label = i.TextPath })
                   .ToList();

            if (!field.IsRequired)
            {
                list.Insert(0, new { value = "", label = "" });
            }

            return Request.CreateResponse(HttpStatusCode.OK, list);

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
                list.Insert(0, new { value = "" , label = "" });
            }

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }

        #endregion

        #region Attribute Lookup Fields

        private List<DetailReadOnlyRowModel> RenderAttributeField(string type, int id, int fieldTypeID)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var ft = Company.GetById<FieldType>(fieldTypeID);

            list.Add(new DetailReadOnlyRowModel
            {
                columns = 1,
                FirstColumnFields = new List<ReadOnlyField> {
                    new ReadOnlyField {
                        Column = 1,
                        Name = ft.FriendlyName,
                        FieldDescription = ft.DisplayDescription,
                        FieldName = ft.Name,

                    }
                },
                Category = ft.Category
            });

            return list;
        }

        #endregion

        #region Filtered Lookup Fields

        private List<DetailReadOnlyRowModel> RenderFilteredLookupField(string type, int id, int fieldTypeID)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var ft = Company.GetById<FieldType>(fieldTypeID, i => i.FieldTypeFilteredLookupDefinitions);

            if (ft.FieldTypeFilteredLookupDefinitions != null)
            {
                if (ft.FieldTypeFilteredLookupDefinitions.Count > 0)
                {
                    var def = ft.FieldTypeFilteredLookupDefinitions.First();
                    list.Add(new DetailReadOnlyRowModel
                    {
                        columns = 1,
                        FirstColumnFields = new List<ReadOnlyField> {
                            new ReadOnlyField {
                                Column = 1,
                                Name = ft.FriendlyName,
                                FieldDescription = ft.DisplayDescription,
                                FieldName = ft.Name,
                                HideHeader = def.HideHeader,
                                HideFooter = def.HideFooter,
                                HideFilter = true,
                                LookupGridUrl = $"/api/FilteredLookupField/{type}/{id}/{def.ID}/values",
                                LookupFieldTypeID = def.ID,
                                LookupObjectID = id,
                                LookupObjectType = type,
                                LookupType = (int)DataType.FilteredLookup
                            }
                        },
                        Category = ft.Category
                    });
                }
            }

            return list;
        }
        
        [Route("FilteredLookupField/{type}/{id:int}/{definitionID:int}/values")]
        public HttpResponseMessage GetFilteredLookupGridField(string type, int id, int definitionID)
        {
            var gridFields = new List<GridField>();
            var columns = new List<GridColumn>();
            IEnumerable<dynamic> results = null;

            try
            {
                var def = Company.GetById<FieldTypeFilteredLookupDefinition>(definitionID, i => i.FieldTypeFilteredLookupDisplayFields);
                if (def == null) throw new Exception("Invalid filtered lookup field is specified");

                var displayFields = def.FieldTypeFilteredLookupDisplayFields.ToList();
                var fieldTypeIDs = displayFields.Where(i => i.FieldTypeID != 0).Select(x => x.FieldTypeID).ToList();
                var fieldTypes = Company.Filter<FieldType>(i => fieldTypeIDs.Contains(i.ID)).ToList();

                var sqlColumns = new List<string>();
                var sqlJoins = new List<string>();
                var sqlWhere = "";
                var sqlOrderBy = "";

                #region Load Columns/Fields

                foreach (var fieldType in fieldTypes)
                {
                    var displayField = displayFields.Single(i => i.FieldTypeID == fieldType.ID);
                    if (displayField.Show)
                    {
                        var cellsformat = "";
                        var columntype = GridColumn.COLUMN_TYPE_STRING;
                        var gridfieldType = "string";
                        switch (fieldType.Type)
                        {
                            case "Boolean":
                                columntype = GridColumn.COLUMN_TYPE_CHECKBOX;
                                gridfieldType = "bool";
                                break;
                            case "Date":
                                cellsformat = "MM/dd/yyyy";
                                columntype = GridColumn.COLUMN_TYPE_DATE;
                                gridfieldType = "date";
                                break;
                            case "DateTime":
                                cellsformat = "MM/dd/yyyy hh:mm tt";
                                columntype = GridColumn.COLUMN_TYPE_DATE;
                                gridfieldType = "date";
                                break;
                            case "Decimal":
                                cellsformat = "d";
                                columntype = GridColumn.COLUMN_TYPE_NUMBER;
                                gridfieldType = "number";
                                break;
                            case "Number":
                                cellsformat = "n";
                                columntype = GridColumn.COLUMN_TYPE_NUMBER;
                                gridfieldType = "number";
                                break;
                        }

                        if (!gridFields.Any(i => i.name == fieldType.Name) && !columns.Any(i => i.datafield == fieldType.Name))
                        {
                            gridFields.Add(new GridField { name = fieldType.Name, type = gridfieldType });
                            var gc = new GridColumn { text = fieldType.FriendlyName, columntype = columntype, datafield = fieldType.Name };
                            if (!string.IsNullOrEmpty(cellsformat))
                            {
                                gc.cellsformat = cellsformat;
                            }
                            columns.Add(gc);
                        }

                        sqlColumns.Add($"F{fieldType.ID}.FormattedValue as [{fieldType.Name}]");
                    }

                    sqlJoins.Add($"left join Field F{fieldType.ID} on F{fieldType.ID}.FieldTypeID = {fieldType.ID} and F{fieldType.ID}.ObjectType = 'Lookup' and F{fieldType.ID}.ObjectID = I.ID ");
                }

                gridFields.Add(new GridField { name = "Object", type = "string" });
                gridFields.Add(new GridField { name = "Url", type = "string" });
                gridFields.Add(new GridField { name = "ID", type = "number" });

                #region Where

                foreach (var df in displayFields.Where(i => i.Filter))
                {
                    sqlWhere += (string.IsNullOrEmpty(sqlWhere) ? "" : "AND ");
                    sqlWhere += $" F{df.FieldTypeID}.Value = {id}";
                }

                #endregion

                #region OrderBy

                foreach (var df in displayFields.Where(i => i.SortOrder.HasValue).OrderBy(i => i.SortOrder).ThenBy(i => i.FieldTypeName))
                {
                    sqlOrderBy += (string.IsNullOrEmpty(sqlOrderBy) ? "" : ", ");
                    if (df.FieldTypeID > 0)
                    {
                        var fieldTypeInfo = fieldTypes.SingleOrDefault(i => i.ID == df.FieldTypeID);
                        if (fieldTypeInfo != null)
                        {
                            switch (fieldTypeInfo.Type)
                            {
                                case "Date":
                                case "DateTime":
                                    sqlOrderBy += $" cast(F{df.FieldTypeID}.FormattedValue as datetime) asc";
                                    break;
                                case "Decimal":
                                case "Number":
                                    sqlOrderBy += $" cast(F{df.FieldTypeID}.FormattedValue as decimal) asc";
                                    break;
                                default:
                                    sqlOrderBy += $" F{df.FieldTypeID}.FormattedValue asc";
                                    break;
                            }
                        }
                        else
                        {
                            sqlOrderBy += $" F{df.FieldTypeID}.FormattedValue asc";
                        }
                    }
                }

                #endregion

                #endregion

                #region Calculate SQL statement

                string sql = string.Empty;
                string sqlColumnString = string.Join(",", sqlColumns);
                if (!string.IsNullOrEmpty(sqlColumnString)) sqlColumnString = "," + sqlColumnString;
                string sqlJoinString = string.Join(" ", sqlJoins);

                sql = $@"
select  'Lookup' as Object,
        I.ID as ObjectID,
        I.ID,
        dbo.GenerateUrlByTypeName('Lookup', I.LookupTypeID, I.ID) as Url
        {sqlColumnString}
from    [Lookup] I
        {sqlJoinString}";

                #endregion

                if (!string.IsNullOrEmpty(sqlWhere))
                {
                    sqlWhere = " where " + sqlWhere;
                }
                sql += sqlWhere;

                if (!string.IsNullOrEmpty(sqlOrderBy))
                {
                    sqlOrderBy = " order by " + sqlOrderBy;
                }
                sql += sqlOrderBy;

                results = Company.Query<dynamic>(sql);
            }
            catch (Exception)
            {

            }

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = results,
                Columns = columns,
                Fields = gridFields
            });
        }

        #endregion

        #region Fusion Lookup Fields

        private List<DetailReadOnlyRowModel> RenderFusionLookupField(FieldWithRelation k)
        {
            var list = new List<DetailReadOnlyRowModel>();

            //load the definition of the field from the [FieldTypeFusionLookupDefinition] table
            int fusionAttributeID = int.Parse(k.Value);
            var fa = Company.GetById<FusionAttribute>(fusionAttributeID);

            FieldTypeFusionLookupDefinition def = null;

            if (fa != null)
            {
                def = Company.Filter<FieldTypeFusionLookupDefinition>(x =>
                    x.FieldTypeID == k.FieldTypeID &&
                    x.SourceFusionAttributeTypeID == fa.FusionAttributeTypeID
                ).FirstOrDefault();
            }

            if (def == null)
                def = Company.Filter<FieldTypeFusionLookupDefinition>(x => x.FieldTypeID == k.FieldTypeID).FirstOrDefault();

            switch (def.ReferenceType)
            {
                case 1: //Self Reference
                case 2: //Parent Reference
                case 3: //Child Reference
                case 4: //Relationship Reference
                    if (AnyFusionLookupGrid(fusionAttributeID, def))
                    {
                        list.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField {
                                    Column = 1,
                                    Name = k.FriendlyName,
                                    FieldDescription = k.DisplayDescription,
                                    FieldName = k.Name,
                                    HideHeader = def.HideHeader,
                                    HideFooter = def.HideFooter,
                                    HideFilter = true,
                                    LookupGridUrl = $"/api/FusionLookupField/{fusionAttributeID}/{def.ID}/values",
                                    LookupFieldTypeID = def.ID,
                                    LookupObjectType = "FusionAttribute",
                                    LookupObjectID = fusionAttributeID,
                                    LookupType = (int)DataType.FusionLookup
                                }
                            },
                            Category = k.Category
                        });
                    }
                    break;
            }

            return list;
        }

        private bool AnyFusionLookupGrid(int sourceFusionAttributeID, FieldTypeFusionLookupDefinition def)
        {
            string sql = string.Empty;

            switch (def.ReferenceType)
            {
                case 1: //Self Reference
                    sql = $@"
from    FusionAttribute A
where   A.ID = {sourceFusionAttributeID}";
                    break;
                case 2: //Parent Reference
                    sql = $@"
from    FusionAttribute c
        inner join FusionAttribute A on c.ID = {sourceFusionAttributeID} and A.ID = c.ParentID and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}";
                    break;
                case 3: //Child Reference
                    sql = $@"
from    FusionAttribute A
where   A.ParentID = {sourceFusionAttributeID}
        and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}";
                    break;
                default: //Relationship Reference
                    sql = $@"
from    [Intersect] I
        inner join FusionAttribute A on (I.Subject = 'FusionAttribute' and I.Object = 'FusionAttribute') and I.SubjectID = {sourceFusionAttributeID} 
                                        and A.ID = I.ObjectID and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}";
                    break;
            }

            sql = "select  case when count(1) > 0 then cast(1 as bit) else cast(0 as bit) end " + sql;

            return Company.Query<bool>(sql).First();
        }

        [Route("FusionLookupField/{sourceFusionAttributeID:int}/{fieldTypeFusionLookupDefinitionID:int}/values")]
        public HttpResponseMessage GetFusionLookupGridField(int sourceFusionAttributeID, int fieldTypeFusionLookupDefinitionID)
        {
            var gridFields = new List<GridField>();
            var columns = new List<GridColumn>();
            IEnumerable<dynamic> results = null;

            var sqlColumns = new List<string>();
            var sqlJoins = new List<string>();

            var def = Company.GetById<FieldTypeFusionLookupDefinition>(fieldTypeFusionLookupDefinitionID, i => i.FieldTypeFusionLookupDisplayFields);
            if (def == null) throw new Exception("Invalid fusion lookup field id specified");

            var displayFields = def.FieldTypeFusionLookupDisplayFields.ToList();
            var fieldTypeIDs = displayFields.Where(i => i.FieldTypeID != 0).Select(x => x.FieldTypeID).ToList();

            #region Load Columns/Fields

            if (displayFields.Any(i => i.FieldTypeName == "Name" && i.Show))
            {
                gridFields.Add(new GridField { name = "Name", type = "string" });
                columns.Add(new GridColumn { text = "Name", datafield = "Name" });
            }
            if (displayFields.Any(i => i.FieldTypeName == "TextPath" && i.Show))
            {
                gridFields.Add(new GridField { name = "TextPath", type = "string" });
                columns.Add(new GridColumn { text = "Path", datafield = "TextPath" });
            }
            if (fieldTypeIDs != null)
            {
                var fieldTypeInfo = Company.Filter<FieldType>(i => fieldTypeIDs.Contains(i.ID)).ToList();
                foreach (var fieldType in fieldTypeInfo)
                {
                    gridFields.Add(new GridField { name = fieldType.Name, type = "string" });
                    if (displayFields.Any(i => i.Show && i.FieldTypeID == fieldType.ID))
                    {
                        columns.Add(new GridColumn { text = fieldType.FriendlyName, datafield = fieldType.Name });
                        sqlColumns.Add($"F{fieldType.ID}.FormattedValue as [{fieldType.Name}]");
                    }
                    sqlJoins.Add($"left join Field F{fieldType.ID} on F{fieldType.ID}.FieldTypeID = {fieldType.ID} and F{fieldType.ID}.ObjectType = 'FusionAttribute' and F{fieldType.ID}.ObjectID = A.ID");
                }
            }
            gridFields.Add(new GridField { name = "Object", type = "string" });
            gridFields.Add(new GridField { name = "Url", type = "string" });
            gridFields.Add(new GridField { name = "ID", type = "number" });

            #endregion

            #region Where Clause

            var sqlWhere = "";

            foreach (var df in displayFields.Where(i => !string.IsNullOrEmpty(i.FilterValue)))
            {
                sqlWhere += (string.IsNullOrEmpty(sqlWhere) ? "" : "AND ");
                if (df.FieldTypeID > 0)
                {
                    sqlWhere += $" F{df.FieldTypeID}.FormattedValue like '{df.FilterValue.StripFormatting(null).CleanForSql()}%'";
                }
                else
                {
                    sqlWhere += $" A.{df.FieldTypeName} like '{df.FilterValue.StripFormatting(null).CleanForSql()}%'";
                }
            }

            #endregion

            #region OrderBy Clause

            var sqlOrderBy = "";

            foreach (var df in displayFields.Where(i => i.SortOrder.HasValue).OrderBy(i => i.SortOrder).ThenBy(i => i.FieldTypeName))
            {
                sqlOrderBy += (string.IsNullOrEmpty(sqlOrderBy) ? "" : ", ");
                if (df.FieldTypeID > 0)
                {
                    var fieldTypeInfo = Company.Filter<FieldType>(i => i.ID == df.FieldTypeID).SingleOrDefault();
                    if (fieldTypeInfo != null)
                    {
                        switch (fieldTypeInfo.Type)
                        {
                            case "Date":
                            case "DateTime":
                                sqlOrderBy += $" cast(F{df.FieldTypeID}.FormattedValue as datetime) asc";
                                break;
                            case "Decimal":
                            case "Number":
                                sqlOrderBy += $" cast(F{df.FieldTypeID}.FormattedValue as decimal) asc";
                                break;
                            default:
                                sqlOrderBy += $" F{df.FieldTypeID}.FormattedValue asc";
                                break;
                        }
                    }
                    else
                    {
                        sqlOrderBy += $" F{df.FieldTypeID}.FormattedValue asc";
                    }
                }
                else
                {
                    sqlOrderBy += $" A.[{df.FieldTypeName}] asc";
                }
            }

            #endregion

            #region Calculate SQL statement

            string sql = string.Empty;
            string sqlColumnString = string.Join(",", sqlColumns);
            if (!string.IsNullOrEmpty(sqlColumnString)) sqlColumnString = "," + sqlColumnString;
            string sqlJoinString = string.Join(" ", sqlJoins);

            bool whereClausePresent = false;

            switch (def.ReferenceType)
            {
                case 1: //Self Reference
                    sql = $@"
select  A.ID,
	    A.ParentID,
	    A.Name,
	    A.TextPath,
	    A.SourceID,
        'FusionAttribute' as Object,
        [dbo].GenerateAssetUrl(F_Asset.ID) as Url
        {sqlColumnString}
from    FusionAttribute A
inner join Asset F_Asset on F_Asset.Object = 'FusionAttribute' and F_Asset.ObjectID = A.ID 
        {sqlJoinString}
where   A.ID = {sourceFusionAttributeID}";

                    whereClausePresent = true;
                    break;
                case 2: //Parent Reference
                    sql = $@"
select  A.ID,
	    A.ParentID,
	    A.Name,
	    A.TextPath,
	    A.SourceID,
        'FusionAttribute' as Object,
        [dbo].GenerateAssetUrl(F_Asset.ID) as Url
        {sqlColumnString}
from    FusionAttribute c
        inner join FusionAttribute A on c.ID = {sourceFusionAttributeID} and A.ID = c.ParentID and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}
        inner join Asset F_Asset on F_Asset.Object = 'FusionAttribute' and F_Asset.ObjectID = A.ID 
        {sqlJoinString}";
                    break;
                case 3: //Child Reference
                    sql = $@"
select  A.ID,
        A.ParentID,
        A.Name,
        A.TextPath,
        A.SourceID,
        'FusionAttribute' as Object,
        [dbo].GenerateAssetUrl(F_Asset.ID) as Url
        {sqlColumnString}
from    FusionAttribute A
        inner join Asset F_Asset on F_Asset.Object = 'FusionAttribute' and F_Asset.ObjectID = A.ID 
        {sqlJoinString}
where   A.ParentID = {sourceFusionAttributeID}
        and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}";

                    whereClausePresent = true;
                    break;
                default: //Relationship Reference
                    sql = $@"
select  A.ID,
        A.ParentID,
        A.name,
        A.TextPath,
        A.SourceID,
        'FusionAttribute' as Object,
        [dbo].GenerateAssetUrl(F_Asset.ID) as Url
        {sqlColumnString}
from    [Intersect] I
        inner join FusionAttribute A on (I.Subject = 'FusionAttribute' and I.Object = 'FusionAttribute') and I.SubjectID = {sourceFusionAttributeID} 
                                        and A.ID = I.ObjectID and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}
        inner join Asset F_Asset on F_Asset.Object = 'FusionAttribute' and F_Asset.ObjectID = A.ID 
        {sqlJoinString}";

                    break;
            }

            #endregion

            if (!string.IsNullOrEmpty(sqlWhere))
            {
                sqlWhere = (whereClausePresent ? " and " : " where ") + sqlWhere;
            }
            sql += sqlWhere;

            if (!string.IsNullOrEmpty(sqlOrderBy))
            {
                sqlOrderBy = " order by " + sqlOrderBy;
            }
            sql += sqlOrderBy;

            results = Company.Query<dynamic>(sql);

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = results,
                Columns = columns,
                Fields = gridFields
            });
        }

        #endregion

        #region Lineage



        [HttpGet, Route("maps/{source}/{sourceID:int}/{target}/{targetID:int}/mapitems")]
        public HttpResponseMessage MapItems(string source, int sourceID, string target, int targetID)
        {
            var list = Company.Query<dynamic>(QueryConstants.MapItems, new { source, sourceID, target, targetID });

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }


        [Route("lineage/query/relationshiptypes"), HttpGet]
        public HttpResponseMessage QueryRelationshipTypes(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Request.CreateResponse(HttpStatusCode.OK, "");
            query = sanitizeQueryString(query);
            var types = Company.Query<IntersectType>(@"
select  ID, 
        SubjectName + ' ' + coalesce(PredicateName, '/') + ' ' +  ObjectName as Name, 
        Subject, 
        SubjectID, 
        Object, 
        ObjectID 
from    IntersectTypeDetail 
where   [Subject] <> 'FusionAttributeType' 
        and [Object] <> 'FusionAttributeType'
        and (
            [SubjectName] like '%' + @query + '%' OR
            [ObjectName] like '%' + @query + '%'
            )", new { query });

            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("lineage/query/objects/{type}/{id:int}"), HttpGet]
        public HttpResponseMessage QueryObjects(string type, int id, string query, int maxResults = 25)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Request.CreateResponse(HttpStatusCode.OK, "");
            query = sanitizeQueryString(query);

            var sql = $@"
select  top {maxResults}
        A.[Object] + '|' + cast(A.ObjectID as varchar) as ID,
        A.[Object],
	    A.ObjectID,
	    A.DisplayValue as [Name],
		T.TextPath,
	    A.TypeName as ObjectTypeName,
	    A.BackColor as IconBackColor,
	    A.ForeColor as IconForeColor,
        case when A.[DisplayValue] like @query + '%' then
            1
        else
            10
        end as rnk
from        AssetDetail A
			cross apply dbo.GetAssetTextPathById(A.ID, '/') T
where       Type = @type 
            and TypeID = @id
            and DisplayValue like '%' + @query + '%'
order by    rnk, [Name]";


            var objects = Company.Query<dynamic>(sql, new { type = new DbString { Value = type, IsAnsi = true, IsFixedLength = true, Length = 50 }, id, query });

            return Request.CreateResponse(HttpStatusCode.OK, objects);
        }

        [Route("lineage/query/fusionattributes"), HttpGet]
        public HttpResponseMessage QueryFusionAttributes(string query, int maxResults = 25)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Request.CreateResponse(HttpStatusCode.OK, "");
            query = sanitizeQueryString(query);

            var objects = Company.Query<dynamic>($@"select top {maxResults}
			    ID,
	            TextPath as Name,
                case when TextPath like @query + '%' then
                    1
                else
                    10
                end as rnk
                from FusionAttribute
                where Deleted = 0 AND TextPath like  '%' + @query + '%'
                order by rnk, TextPath", new { query = query });

            return Request.CreateResponse(HttpStatusCode.OK, objects);
        }
        string sanitizeQueryString(string query)
        {
            query = query ?? "";
            query = query.Replace("%", "[%]").Replace("_", "[_]").TrimStart(' ');
            return query;
        }


        [Route("lineage/mappings"), HttpGet]
        public HttpResponseMessage GetLineageMaps()
        {
            var sql = @"
                        select
                            M.ID
	                        ,M.Name
	                        ,M.Transformation
	                        ,M.MapTypeID as 'MapTypeID'
	                        ,MT.Name as 'MapType'
	                        ,MT.Description as 'MapTypeDescription'
	                        ,MT.MapClass as 'MapClass'
                        from
                            map M
                            inner join maptype MT on (M.MapTypeID = MT.ID);
                    ";

            var list = Company.Query<dynamic>(sql);


            return Request.CreateResponse(HttpStatusCode.OK, list);
        }

        [Route("lineage/objectTypes"), HttpGet]
        public HttpResponseMessage GetLineageObjectTypes()
        {
            var sql = @"
                select 
	                coalesce(FAT.TextPath, A.[Name]) as label,
	                A.ID as [value],
	                R.[object], 
	                R.objectId 
                from
                (
	                select 
		                [Subject] as [Object], 
		                SubjectID as ObjectID
	                from 
		                IntersectType T
		                inner join [Predicate] P on T.PredicateID = P.ID and P.[Type] = 1
	                where 
		                T.[State] = 1

	                union

	                select 
		                [Object] as [Object], 
		                ObjectID as ObjectID
	                from 
		                IntersectType T
		                inner join [Predicate] P on T.PredicateID = P.ID and P.[Type] = 1
	                where 
		                T.[State] = 1
                ) R
                inner join AssetType A on A.[Object] = R.[Object] and A.ObjectID = R.ObjectID
				left join FusionAttributeType FAT on A.[Object] = 'FusionAttributeType' and FAT.ID = A.ObjectID
				order by label asc";

            var list = Company.Query<dynamic>(sql);

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }

        [HttpGet, Route("lineage/intersectTypes")]
        public HttpResponseMessage GetLineageIntersectTypes()
        {
            var sql = @"
                   select
						T.ID as intersectTypeId,
						T.[subject],
						T.[subjectId],
						ATS.ID as subjectAssetTypeId,
						T.[object],
						T.[objectId],
						ATO.ID as objectAssetTypeId,
						P.ID as predicateId,
						P.[Name] as predicateName,
						P.Inverse as predicateInverse
					from 
						IntersectType T
					inner join [Predicate] P on P.ID = T.PredicateID
					inner join AssetType ATS on ATS.[Object] = T.[Subject] and ATS.ObjectID = T.SubjectID
					inner join AssetType ATO on ATO.[Object] = T.[Object] and ATO.ObjectID = T.ObjectID
					where 
						P.[Type] = 1 and T.[State] = 1";

            var results = Company.Query<dynamic>(sql).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [Route("lineage/objects/{assetTypeId:int}"), HttpGet]
        public HttpResponseMessage GetLineageObjects(int assetTypeId, int offset = 0 , int rows = 100000, string query = null)
        {
            query = sanitizeQueryString(query);
            if (string.IsNullOrWhiteSpace(query))
                query = null;
            int count = 0;

            var assetType = Company.GetById<AssetType>(assetTypeId);
            bool isFusionAttributeType = assetType?.Object == SystemObjects.FusionAttributeType.ToString();


            #region Sql

            string countSql = @"select 
                    count(*)
                from 
                    asset a
                inner join assettype t on t.id = a.assettypeid
				{0}
                where  
                    t.id = @id and (@query is null or {1} like '%' + @query + '%')";
            string sql = @"select 
                    a.ID as assetId,
                    {0} as [name],
                    t.[Name] as typeName,
					coalesce(s.IconBackColor, '#000') as backColor,
					coalesce(s.IconForeColor, '#fff') as foreColor,
                    a.[object],
                    a.objectId,
                    t.id as assetTypeId
                from 
                    asset a
                inner join assettype t on t.id = a.assettypeid
				left join objectstyle s on s.objecttype = t.[object] and s.objectid = t.objectid
                {1}
                where  
                    t.id = @id and (@query is null or {2} like '%' + @query + '%')
					order by {3}
					OFFSET  @offset ROWS FETCH NEXT @rows ROWS ONLY";

            #endregion
            string fieldName, join;

            if (isFusionAttributeType)
            {
                fieldName = "fa.TextPath";
                join = "inner join FusionAttribute fa on fa.ID = A.ObjectID";
            } else
            {
                fieldName = "d.TextPath";
                join = "cross apply dbo.GetAssetTextPathById(a.id, '/') d";
            }

            countSql = string.Format(countSql, join, fieldName);
            sql = string.Format(sql, fieldName, join, fieldName, fieldName);

            if (offset == 0 || query != null)
                count = Company.Query<int>(countSql, new { id = assetTypeId, query = new Dapper.DbString { Value = query, IsAnsi = true } }).FirstOrDefault();

            var results = Company.Query<dynamic>(sql, new { id = assetTypeId, offset = offset, rows = rows, query = new Dapper.DbString { Value = query, IsAnsi = true } }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                count = count,
                results = results
            });
        }
        #endregion

        #region Complex Lookup Fields

        private void SetCellValue(SLDocument document, int rowIndex, int colIndex, string dataType, object value)
        {
            var valueString = value?.ToString() ?? "";
            switch (dataType.ToUpper())
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
                    if (DateTime.TryParse((value ?? "").ToString(), out DateTime dateVal))
                    {
                        document.SetCellValue(rowIndex, colIndex, dateVal);

                        SLStyle style = document.CreateStyle();
                        style.FormatCode = "m/d/yyyy";
                        document.SetCellStyle(rowIndex, colIndex, style);
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

        [HttpGet, Route("dynamiclookup/export/{type}/{id:int}/{fieldTypeID:int}/{lookupType:int}/excel.xls")]
        public async Task<HttpResponseMessage> ExportDynamicLookup(string type, int id, int fieldTypeID, int lookupType)
        {
            string resultString = "";

            var ft = Company.GetById<FieldType>(fieldTypeID);

            switch((DataType)lookupType)
            {
                case DataType.RefListRelationship:
                    resultString = await GetReferenceListItemsField(id).Content.ReadAsStringAsync();
                    break;
                case DataType.ComplexRelationLookup:
                    resultString = await GetComplexLookupGridField(type, id, fieldTypeID).Content.ReadAsStringAsync();
                    break;
                case DataType.OwnershipLookup:
                    resultString = await GetOwnershipLookupGridField(type, id, fieldTypeID).Content.ReadAsStringAsync();
                    break;
                case DataType.FilteredLookup:
                    resultString = await GetFilteredLookupGridField(type, id, fieldTypeID).Content.ReadAsStringAsync();
                    break;
                case DataType.FusionLookup:
                    resultString = await GetFusionLookupGridField(id, fieldTypeID).Content.ReadAsStringAsync();
                    break;
                default:
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, new Exception("invalid lookup type"));

            }

            dynamic result = JsonConvert.DeserializeObject<dynamic>(resultString);

            var document = new SLDocument();
            document.AddWorksheet("Items");

            int colIndex = 1;
            for(int i = 0; i < result.Columns.Count; i++)
            {
                var colField = result.Columns[i].datafield.Value;
                var dataType = "string";

                for (int k = 0; k < result.Fields.Count; k++)
                {
                    var field = result.Fields[k];
                    if (field["name"].Value == colField)
                    {
                        dataType = field["type"].Value;
                        break;
                    }

                }

                document.SetCellValue(1, colIndex, result.Columns[i].text.Value);

                int rowIndex = 2;

                for (int j = 0; j < result.Values.Count; j++)
                {
                    var value = result.Values[j][colField].Value;

                    SetCellValue(document, rowIndex, colIndex, dataType, value);

                    rowIndex++;
                }
                colIndex++;
            }

            var stream = new MemoryStream();
            document.SaveAs(stream);
            var len = stream.Length;
            stream.Position = 0;
            HttpResponseMessage response = null;
            // serve the file to the client      
            response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            response.Content.Headers.ContentLength = stream.Length;
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Items {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return response;
        }

        private List<DetailReadOnlyRowModel> RenderComplexLookupField(string type, int id, int fieldTypeID)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var ft = Company.GetById<FieldType>(fieldTypeID, i => i.FieldTypeLookup);
            var lookup = ft.FieldTypeLookup;

            if (ft != null && lookup != null)
            {
                if (AnyComplexLookupGridValues(type, id, lookup))
                {
                    list.Add(new DetailReadOnlyRowModel
                    {
                        columns = 1,
                        FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField {
                                        Column = 1,
                                        Name = ft.FriendlyName,
                                        FieldDescription = ft.DisplayDescription,
                                        FieldName = ft.Name,
                                        HideHeader = lookup.HideHeader,
                                        HideFooter = lookup.HideFooter,
                                        HideFilter = lookup.HideFilter,
                                        LookupGridUrl = $"/api/ComplexLookupField/{type}/{id}/{ft.ID}/values",
                                        LookupObjectID = id,
                                        LookupObjectType = type,
                                        LookupFieldTypeID = ft.ID,
                                        LookupType = (int)DataType.ComplexRelationLookup
                                    }
                                },
                        Category = ft.Category
                    });
                }
            }

            return list;
        }

        internal class ComplexColumnModel
        {
            public ComplexColumnModel()
            {
                SortColumn = null;
                SortOrder = null;
                DisplayOrder = 1;
                OutputColumn = false;
            }

            public string text { get; set; }
            public string texttype { get; set; }
            public string datafield { get; set; }
            public string format { get; set; }
            public string description { get; set; }

            public string datafieldtype { get; set; }

            public string DisplayColumn { get; set; }
            public int DisplayOrder { get; set; }

            public string SortColumn { get; set; }
            public int? SortOrder { get; set; }
            public int? Width { get; set; }

            public string Filter { get; set; }

            public bool OutputColumn { get; set; }

            public string objectfield { get; set; }

            public string objectidfield { get; set; }

            public string urlfield { get; set; }

            public string contextfield { get; set; }
        }

        private string getFieldTypeColumnString(string type, string columnName)
        {
            switch (type.ToLower())
            {
                case "decimal":
                case "number":
                    return $"cast({columnName} as float)";
                case "date":
                case "datetime":
                    return $"cast({columnName} as datetime)";
                case "boolean":
                    return $"case when lower({columnName}) = 'true' then 1 else 0 end";
                case "link":
                    return $"substring({columnName}, 0, charindex('|', {columnName}))";
                default:
                    return $"{columnName}";
            }
        }

        private void loadComplexLookupColumns(List<FieldType> fieldTypes,
            List<FieldTypeComplexLookupDefinitionField> fields,
            List<ComplexColumnModel> columnModels,
            FieldTypeComplexLookupDefinitionRelation join,
            string intersectIDColumn,
            string objColumn,
            string objIDColumn,
            string joinType,
            int pos)
        {
            Func<FieldType, FieldTypeComplexLookupDefinitionField, ComplexColumnModel, string> setColumnTypeInfo = (ft, df, c) =>
            {

                c.format = "";
                c.texttype = GridColumn.COLUMN_TYPE_STRING;
                c.datafieldtype = "string";
                c.DisplayOrder = df.DisplayOrder;
                c.SortOrder = df.SortOrder;
                c.Filter = df.Filter;
                c.OutputColumn = df.Show;

                if (ft != null)
                {
                    switch (ft.Type)
                    {
                        case "Boolean":
                            c.texttype = GridColumn.COLUMN_TYPE_CHECKBOX;
                            c.datafieldtype = "bool";
                            break;
                        case "Date":
                            c.format = "MM/dd/yyyy";
                            c.texttype = GridColumn.COLUMN_TYPE_DATE;
                            c.datafieldtype = "date";
                            break;
                        case "DateTime":
                            c.format = "MM/dd/yyyy hh:mm tt";
                            c.texttype = GridColumn.COLUMN_TYPE_DATE;
                            c.datafieldtype = "datetime";
                            break;
                        case "Decimal":
                            c.format = "d";
                            c.texttype = GridColumn.COLUMN_TYPE_NUMBER;
                            c.datafieldtype = "number";
                            break;
                        case "Number":
                            c.format = "n";
                            c.texttype = GridColumn.COLUMN_TYPE_NUMBER;
                            c.datafieldtype = "number";
                            break;
                        case "Html":
                        case "Link":
                            c.datafieldtype = "html";
                            break;
                    }

                    c.description = ft.DisplayDescription;
                }

                return "";
            };


            var multiFieldReferencePosition = 1;
            fields.ForEach(i =>
            {

                FieldType ft = (i.FieldTypeID > 0) ? fieldTypes.SingleOrDefault(o => o.ID == i.FieldTypeID) : null;
                if (i.FieldTypeName.Contains("."))
                    i.FieldTypeName = i.FieldTypeName.Replace('.', '~');

                var dataField = $"H{pos}_{System.Text.RegularExpressions.Regex.Replace(i.FieldTypeName, "[^a-zA-z0-9]", "")}";

                // As long as field type is NOT null, you can go ahead and add the field.
                if (i.FieldTypeID > 0 && ((i.Object == "IntersectType" && i.ObjectID == join.IntersectTypeID) || (join.Object == i.Object && join.ObjectID == i.ObjectID)))
                {
                    #region IF FieldTypeID has value
                    if (i.FieldTypeName.StartsWith("Related Item~")) //special case, these records use FieldTypeID to store an IntersectTypeID and there can be overlap
                        ft = null;
                    else
                        ft = fieldTypes.SingleOrDefault(o => o.ID == i.FieldTypeID);

                    if (ft != null)
                    {
                        #region

                        var tbPrefix = $"F{pos}_{multiFieldReferencePosition}";
                        var tbtPrefix = $"FT{pos}_{multiFieldReferencePosition}";

                        // Determine the join syntax for the eventual query.
                        join.JoinStatement += $" inner join FieldType {tbtPrefix} on {tbtPrefix}.ID = {i.FieldTypeID} ";
                        if ((i.Object == "IntersectType" && i.ObjectID == join.IntersectTypeID) || i.FieldTypeName.StartsWith("Relation~"))
                            join.JoinStatement += $" {joinType} join Field {tbPrefix} on {tbPrefix}.FieldTypeID = {i.FieldTypeID} and {tbPrefix}.ObjectType = 'Intersect' and {tbPrefix}.ObjectID = {intersectIDColumn}";
                        else if (join.Object == i.Object && join.ObjectID == i.ObjectID)
                            join.JoinStatement += $" {joinType} join Field {tbPrefix} on {tbPrefix}.FieldTypeID = {i.FieldTypeID} and {tbPrefix}.ObjectType = {objColumn} and {tbPrefix}.ObjectID = {objIDColumn}";

                        //Create the column/field to display the visible column cell.
                        var fc = new ComplexColumnModel
                        {
                            DisplayColumn = (ft.Type == "Boolean") ?
 $@"case 
    when {tbtPrefix}.AllowAllValue = 1 and {tbPrefix}.Value = '0' then lower({tbtPrefix}.AllowAllLabel) 
    when {tbPrefix}.Value is not null then lower({tbPrefix}.FormattedValue)
    when {tbtPrefix}.DefaultValue is not null then lower({tbtPrefix}.DefaultFormattedValue) 
    else '' 
end" :
 $@"case 
    when {tbtPrefix}.AllowAllValue = 1 and {tbPrefix}.Value = '0' then {tbtPrefix}.AllowAllLabel 
    when {tbPrefix}.Value is not null then {tbPrefix}.FormattedValue 
    when {tbtPrefix}.DefaultValue is not null then {tbtPrefix}.DefaultFormattedValue 
    else '' 
end",
                            text = i.OverrideDisplayName ?? ft.FriendlyName,
                            datafield = $"{dataField}",
                            OutputColumn = true,
                            Width = i.Width
                        };

                        if (i.SortOrder > 0)
                        {
                            var columnValue = getFieldTypeColumnString(ft.Type, ft.Type == "Link" ? $"{tbPrefix}.Value" : fc.DisplayColumn);
                            fc.SortColumn = columnValue == fc.DisplayColumn ? "" : columnValue;
                        }

                        setColumnTypeInfo(ft, i, fc);


                        if (ft.LookupObjectType == "Lookup" || ft.LookupObjectType == "ReferenceItem")
                        {
                            var context = "Preview";
                            if (ft.Type == "Lookup")
                                context = "LookupPreview";

                            // Add the fields that you need to create link in Angular component.
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'{context}'", datafield = $"{dataField}_Context" });
                            columnModels.Add(new ComplexColumnModel {
                                DisplayColumn = $@"
case 
    when {tbPrefix}.Value is not null then {tbtPrefix}.[LookupObjectType]
    when {tbtPrefix}.DefaultValue is not null then replace({tbtPrefix}.[LookupObjectType], 'Type', '')
    else '' 
end
",
                                datafield = $"{dataField}_Object" });
                            columnModels.Add(new ComplexColumnModel {
                                DisplayColumn = $@"
case 
    when {tbPrefix}.Value is not null then cast({tbPrefix}.Value as varchar)
    when {tbtPrefix}.DefaultValue is not null then cast({tbtPrefix}.DefaultValue as varchar)
    else '' 
end
",
                                datafield = $"{dataField}_ObjectID" });
                            columnModels.Add(new ComplexColumnModel {
                                DisplayColumn = $@"
case 
    when {tbPrefix}.Value is not null then [dbo].GenerateUrlByTypeName({tbtPrefix}.[LookupObjectType], {tbtPrefix}.LookupObjectID, {tbtPrefix}.LookupObjectID)
    when {tbtPrefix}.DefaultValue is not null then [dbo].GenerateUrlByTypeName({tbtPrefix}.[LookupObjectType], {tbtPrefix}.LookupObjectID, {tbtPrefix}.LookupObjectID) 
    else '' 
end",
                                datafield = $"{dataField}_Url" });

                            // Now set the fields to reference to create the preview link in Angular component.
                            fc.datafieldtype = "lookup";
                            fc.contextfield = $"{dataField}_Context";
                            fc.objectfield = $"{dataField}_Object";
                            fc.objectidfield = $"{dataField}_ObjectID";
                            fc.urlfield = $"{dataField}_Url";
                        }
                        else if (i.FieldTypeName.ToLower() == "name") //special case for name
                        {
                            fc.datafieldtype = "lookup";
                            fc.datafieldtype = "lookup";
                            fc.contextfield = $"{dataField}_Context";
                            fc.objectfield = $"{dataField}_Object";
                            fc.objectidfield = $"{dataField}_ObjectID";
                            fc.urlfield = $"{dataField}_Url";
                            // Add the fields that you need to create link in Angular component.

                            var obj = i.Object.Replace("Type", "");

                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'{obj}'", datafield = $"{dataField}_Object" });
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ID as varchar)", datafield = $"{dataField}_ObjectID" });
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetUrl((select ID from Asset where Object = '{obj}' and ObjectID = A{pos}.ID))", datafield = $"{dataField}_Url" });
                        }

                        //Add here, only after you determine if this should be a link ABOVE.
                        columnModels.Add(fc);

                        multiFieldReferencePosition++;  //Increment in case you reference multiple fields from the same objects, in a SINGLE hop.

                        #endregion
                    } //check if field type is NOT null
                    else
                    {
                        #region

                        if (i.FieldTypeName.StartsWith("Related Item~"))
                        {
                            var tbPrefix = $"F{pos}_{multiFieldReferencePosition}";
                            var tbTypePrefix = $"FT{pos}_{multiFieldReferencePosition}";
                            var tbDetailPrefix = $"FD{pos}_{multiFieldReferencePosition}";                            
                            
                            // Determine the join syntax for the eventual query.
                            join.JoinStatement += $@" {joinType} join [IntersectType] {tbTypePrefix} on {tbTypePrefix}.ID = {i.FieldTypeID} 
{joinType} join [Intersect] {tbPrefix} on {tbPrefix}.IntersectTypeID = {tbTypePrefix}.ID and 
( 
({tbTypePrefix}.Object = '{i.Object}' and {tbTypePrefix}.ObjectID = {i.ObjectID} and {tbPrefix}.Object = {objColumn} and {tbPrefix}.ObjectID = {objIDColumn}) OR
({tbTypePrefix}.Subject = '{i.Object}' and {tbTypePrefix}.SubjectID = {i.ObjectID} and {tbPrefix}.Subject = {objColumn} and {tbPrefix}.SubjectID = {objIDColumn})
)
outer apply (
	select {(i.Object == "FusionAttributeType" ? $"{tbDetailPrefix}TP.TextPath" : "DisplayValue")} as [Name], {tbDetailPrefix}DU.[Url] as [Url] from AssetDetail {tbDetailPrefix}D
	cross apply [dbo].[GetAssetUrlById]({tbDetailPrefix}D.ID) {tbDetailPrefix}DU
    {(i.Object == "FusionAttributeType" ? $"cross apply [dbo].GetAssetTextPathById({tbDetailPrefix}D.ID, '.') {tbDetailPrefix}TP " : " ")}
	where {tbDetailPrefix}D.Object = case when ({tbPrefix}.Subject = {objColumn} and {tbPrefix}.SubjectID = {objIDColumn}) then {tbPrefix}.Object else {tbPrefix}.Subject end
		and {tbDetailPrefix}D.ObjectID = case when ({tbPrefix}.Subject = {objColumn} and {tbPrefix}.SubjectID = {objIDColumn}) then {tbPrefix}.ObjectID else {tbPrefix}.SubjectID end
	union all
	select [Name], {tbDetailPrefix}TU.[Url] as [Url] from AssetType {tbDetailPrefix}T
	cross apply [dbo].[GetAssetTypeUrlById]({tbDetailPrefix}T.ID) {tbDetailPrefix}TU
	where {tbDetailPrefix}T.Object = case when ({tbPrefix}.Subject = {objColumn} and {tbPrefix}.SubjectID = {objIDColumn}) then {tbPrefix}.Object else {tbPrefix}.Subject end
		and {tbDetailPrefix}T.ObjectID = case when ({tbPrefix}.Subject = {objColumn} and {tbPrefix}.SubjectID = {objIDColumn}) then {tbPrefix}.ObjectID else {tbPrefix}.SubjectID end
  ) {tbDetailPrefix}
";

                            //Create the column/field to display the visible column cell.
                            var fc = new ComplexColumnModel
                            {
                                DisplayColumn = $"{tbDetailPrefix}.Name",
                                text = i.OverrideDisplayName ?? i.FieldTypeName.Replace("Related Item~", ""),
                                datafield = $"{dataField}",
                                SortColumn = i.SortOrder > 0 ? $"{tbDetailPrefix}.Name" : string.Empty,
                                OutputColumn = true,
                                Width = i.Width
                            };
                            setColumnTypeInfo(ft, i, fc);

                            var context = "Preview";

                            // Add the fields that you need to create link in Angular component.
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'{context}'", datafield = $"{dataField}_Context" });
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"case when {tbTypePrefix}.Object = '{i.Object}' and {tbTypePrefix}.ObjectID = {i.ObjectID} then {tbPrefix}.Subject else {tbPrefix}.Object end", datafield = $"{dataField}_Object" });
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"case when {tbTypePrefix}.Object = '{i.Object}' and {tbTypePrefix}.ObjectID = {i.ObjectID} then {tbPrefix}.SubjectID else {tbPrefix}.ObjectID end", datafield = $"{dataField}_ObjectID" });
                            columnModels.Add(new ComplexColumnModel { DisplayColumn = $"{tbDetailPrefix}.Url", datafield = $"{dataField}_Url" });

                            // Now set the fields to reference to create the preview link in Angular component.
                            fc.datafieldtype = "lookup";
                            fc.contextfield = $"{dataField}_Context";
                            fc.objectfield = $"{dataField}_Object";
                            fc.objectidfield = $"{dataField}_ObjectID";
                            fc.urlfield = $"{dataField}_Url";

                            //Add here, only after you determine if this should be a link ABOVE.
                            columnModels.Add(fc);

                            multiFieldReferencePosition++;  //Increment in case you reference multiple fields from the same objects, in a SINGLE hop.
                        }

                        #endregion
                    }

                    #endregion
                }
                else
                {
                    #region DEFAULT

                    string overrideDisplayColumn = null;
                    bool isReferenceType = (join.Object == "ReferenceItemType" && join.ObjectID == 0);
                    string @object = isReferenceType ? join.Object : join.Object.Replace("Type", "");
                    string idColumn = @object == "Resource" ? "ResourceID" : "ID";
                    string delim = @object.StartsWith("Fusion") ? "." : "/";

                    if (i.FieldTypeID == 0)
                    {
                        if (isReferenceType)
                        {
                            overrideDisplayColumn = $@"(select Name from AssetType where Object = '{@object}' and ObjectID = A{pos}.{idColumn})";
                        }
                        else if (i.FieldTypeName.ToLower() == "textpath")
                        {
                            
                            overrideDisplayColumn = $@"(select T.TextPath from Asset a
                                cross apply dbo.GetAssetTextPathById(a.ID, '{delim}') T
                                where a.[Object] = '{@object}' and a.[ObjectID] = A{pos}.{idColumn})";
                        }
                        else if (i.FieldTypeName.ToLower() == "displayvalue" || i.FieldTypeName.ToLower() == "name")
                        {
                            overrideDisplayColumn = $@"(select D.DisplayValue from Asset a
                                cross apply dbo.GetAssetDisplayValueById(a.ID) D
                                where a.[Object] = '{@object}' and a.[ObjectID] = A{pos}.{idColumn})";
                        }

                    }

                    if (i.Object == "IntersectType" && i.ObjectID == join.IntersectTypeID)
                    {
                        #region IntersectType field

                        var oc = new ComplexColumnModel
                        {
                            DisplayColumn = $"A{pos}.{i.FieldTypeName}",
                            SortColumn = i.SortOrder > 0 ? getFieldTypeColumnString(ft?.Type ?? "", $"A{pos}.{i.FieldTypeName}") : string.Empty,
                            datafield = $"{dataField}",
                            text = i.OverrideDisplayName ?? i.FieldTypeName,
                            OutputColumn = true,
                            Width = i.Width
                        };
                        setColumnTypeInfo(ft, i, oc);
                        columnModels.Add(oc);

                        #endregion
                    }
                    else if (join.Object == i.Object && join.ObjectID == i.ObjectID)
                    {
                        #region ObjectType field

                        string objectDisplayColumn = $"A{pos}.{i.FieldTypeName}";

                        if (ft != null)
                        {
                            if (ft.Type == "Boolean")
                                objectDisplayColumn = $"lower(A{pos}.{i.FieldTypeName}) as {i.FieldTypeName}";
                        }
                        else if (overrideDisplayColumn != null)
                            objectDisplayColumn = overrideDisplayColumn;

                        var objectFriendlyName = i.OverrideDisplayName ?? i.FieldTypeName;
                        var objectSortColumn = (i.SortOrder > 0) ? getFieldTypeColumnString(ft?.Type ?? "", objectDisplayColumn) : string.Empty;

                        switch (i.Object)
                        {
                            case "ArtifactType":
                                #region ArtifactType

                                var ac = new ComplexColumnModel
                                {
                                    DisplayColumn = objectDisplayColumn,
                                    text = objectFriendlyName,
                                    datafield = $"{dataField}",
                                    OutputColumn = true,
                                    contextfield = $"{dataField}_Context",
                                    objectfield = $"{dataField}_Object",
                                    objectidfield = $"{dataField}_ObjectID",
                                    SortColumn = objectSortColumn,
                                    urlfield = $"{dataField}_Url",
                                    Width = i.Width
                                };

                                setColumnTypeInfo(ft, i, ac);
                                ac.datafieldtype = "lookup"; //must be done after function call above.
                                columnModels.Add(ac);
                                // Add the fields that you need to create link in Angular component.
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Artifact'", datafield = $"{dataField}_Object" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ID as varchar)", datafield = $"{dataField}_ObjectID" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetUrl((select ID from Asset where Object = 'Artifact' and ObjectID =A{pos}.ID))", datafield = $"{dataField}_Url" });

                                break;
                                #endregion
                            case "FusionAttributeType":
                                #region FusionAttributeType

                                var oc = new ComplexColumnModel
                                {
                                    DisplayColumn = objectDisplayColumn,
                                    text = objectFriendlyName,
                                    datafield = $"{dataField}",
                                    OutputColumn = true,
                                    SortColumn = objectSortColumn,
                                    Width = i.Width,
                                    contextfield = $"{dataField}_Context",
                                    objectfield = $"{dataField}_Object",
                                    objectidfield = $"{dataField}_ObjectID",
                                    urlfield = $"{dataField}_Url"
                                };
                                setColumnTypeInfo(ft, i, oc);
                                oc.datafieldtype = "lookup"; //must be done after function call above.
                                columnModels.Add(oc);

                                // Add the fields that you need to create link in Angular component.
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'FusionAttribute'", datafield = $"{dataField}_Object" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ID as varchar)", datafield = $"{dataField}_ObjectID" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetUrl((select ID from Asset where Object = 'FusionAttribute' and ObjectID = A{pos}.ID))", datafield = $"{dataField}_Url" });
                                break;
                                
                                #endregion
                            case "PolicyType":
                                #region PolicyType

                                //Create the column/field to display the visible column cell.
                                var pc = new ComplexColumnModel
                                {
                                    DisplayColumn = objectDisplayColumn,
                                    text = objectFriendlyName,
                                    datafield = $"{dataField}",
                                    OutputColumn = true,
                                    contextfield = $"{dataField}_Context",
                                    objectfield = $"{dataField}_Object",
                                    objectidfield = $"{dataField}_ObjectID",
                                    SortColumn = objectSortColumn,
                                    Width = i.Width,
                                    urlfield = $"{dataField}_Url"
                                };
                                setColumnTypeInfo(ft, i, pc);
                                pc.datafieldtype = "lookup"; //must be done after function call above.
                                columnModels.Add(pc);

                                // Add the fields that you need to create link in Angular component.
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Policy'", datafield = $"{dataField}_Object" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ID as varchar)", datafield = $"{dataField}_ObjectID" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetUrl((select ID from Asset where Object = 'Policy' and ObjectID = A{pos}.ID))", datafield = $"{dataField}_Url" });
                                break;
                                
                                #endregion
                            case "ResourceType":
                                #region ResourceType

                                if (i.FieldTypeName.In("FirstName", "LastName", "Email", "TextPath", "DisplayValue"))
                                {
                                    var rec = new ComplexColumnModel
                                    {
                                        DisplayColumn = objectDisplayColumn,
                                        text = objectFriendlyName,
                                        datafield = $"{dataField}",
                                        OutputColumn = true,
                                        contextfield = $"{dataField}_Context",
                                        objectfield = $"{dataField}_Object",
                                        objectidfield = $"{dataField}_ObjectID",
                                        SortColumn = objectSortColumn,
                                        Width = i.Width,
                                        urlfield = $"{dataField}_Url"
                                    };

                                    setColumnTypeInfo(ft, i, rec);
                                    rec.datafieldtype = "lookup"; //must be done after function call above.
                                    columnModels.Add(rec);
                                    // Add the fields that you need to create link in Angular component.
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Resource'", datafield = $"{dataField}_Object" });
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ResourceID as varchar)", datafield = $"{dataField}_ObjectID" });
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetUrl((select ID from Asset where Object = 'Resource' and ObjectID = A{pos}.ResourceID))", datafield = $"{dataField}_Url" });
                                }
                                else
                                {
                                    var rec2 = new ComplexColumnModel
                                    {
                                        DisplayColumn = objectDisplayColumn,
                                        text = objectFriendlyName,
                                        datafield = $"{dataField}",
                                        SortColumn = objectSortColumn,
                                        OutputColumn = true,
                                        Width = i.Width
                                    };
                                    setColumnTypeInfo(ft, i, rec2);
                                    columnModels.Add(rec2);
                                }

                                break;
                                
                                #endregion
                            case "RuleType":
                                #region RuleType
                                //Create the column/field to display the visible column cell.
                                var rc = new ComplexColumnModel
                                {
                                    DisplayColumn = objectDisplayColumn,
                                    text = objectFriendlyName,
                                    datafield = $"{dataField}",
                                    OutputColumn = true,
                                    Width = i.Width,
                                    contextfield = $"{dataField}_Context",
                                    objectfield = $"{dataField}_Object",
                                    objectidfield = $"{dataField}_ObjectID",
                                    SortColumn = objectSortColumn,
                                    urlfield = $"{dataField}_Url"
                                };
                                setColumnTypeInfo(ft, i, rc);
                                rc.datafieldtype = "lookup"; //must be done after function call above.
                                columnModels.Add(rc);

                                // Add the fields that you need to create link in Angular component.
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Rule'", datafield = $"{dataField}_Object" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ID as varchar)", datafield = $"{dataField}_ObjectID" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetUrl((select ID from Asset where Object = 'Rule' and ObjectID = A{pos}.ID))", datafield = $"{dataField}_Url" });
                                break;                                
                                
                                #endregion
                            case "TaxonomyType":
                                #region TaxonomyType

                                var tc = new ComplexColumnModel
                                {
                                    DisplayColumn = objectDisplayColumn,
                                    text = objectFriendlyName,
                                    datafield = $"{dataField}",
                                    OutputColumn = true,
                                    Width = i.Width,
                                    contextfield = $"{dataField}_Context",
                                    objectfield = $"{dataField}_Object",
                                    objectidfield = $"{dataField}_ObjectID",
                                    SortColumn = objectSortColumn,
                                    urlfield = $"{dataField}_Url"
                                };
                                setColumnTypeInfo(ft, i, tc);
                                tc.datafieldtype = "lookup"; //must be done after function call above.
                                columnModels.Add(tc);

                                // Add the fields that you need to create link in Angular component.
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Taxonomy'", datafield = $"{dataField}_Object" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ID as varchar)", datafield = $"{dataField}_ObjectID" });
                                columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetUrl((select ID from Asset where Object = 'Taxonomy' and ObjectID = A{pos}.ID))", datafield = $"{dataField}_Url" });
                                break;

                                #endregion
                            default:
                                if (i.Object == "ReferenceItemType" && i.ObjectID == 0)
                                {
                                    #region ReferenceItemType
                                    //Create the column/field to display the visible column cell.
                                    var ric = new ComplexColumnModel
                                    {
                                        DisplayColumn = objectDisplayColumn,
                                        text = objectFriendlyName,
                                        datafield = $"{dataField}",
                                        OutputColumn = true,
                                        Width = i.Width,
                                        contextfield = $"{dataField}_Context",
                                        objectfield = $"{dataField}_Object",
                                        objectidfield = $"{dataField}_ObjectID",
                                        SortColumn = objectSortColumn,
                                        urlfield = $"{dataField}_Url"
                                    };
                                    setColumnTypeInfo(ft, i, ric);
                                    ric.datafieldtype = "lookup"; //must be done after function call above.
                                    columnModels.Add(ric);

                                    // Add the fields that you need to create link in Angular component.
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'Preview'", datafield = $"{dataField}_Context" });
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"'ReferenceItemType'", datafield = $"{dataField}_Object" });
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"cast(A{pos}.ID as varchar)", datafield = $"{dataField}_ObjectID" });
                                    columnModels.Add(new ComplexColumnModel { DisplayColumn = $"dbo.GenerateAssetTypeUrl((select ID from AssetType where Object = 'ReferenceItemType' and ObjectID = A{pos}.ID))", datafield = $"{dataField}_Url" });
                                    #endregion
                                }
                                else
                                {
                                    #region Default
                                    var dc = new ComplexColumnModel
                                    {
                                        DisplayColumn = objectDisplayColumn,
                                        text = objectFriendlyName,
                                        datafield = $"{dataField}",
                                        SortColumn = objectSortColumn,
                                        OutputColumn = true,
                                        Width = i.Width
                                    };
                                    setColumnTypeInfo(ft, i, dc);
                                    columnModels.Add(dc);
                                    #endregion
                                }
                                break;
                        }

                        #endregion
                    }

                    #endregion
                }
            });
        }

        private void loadComplexLookupJoin(string type, int id, List<FieldTypeComplexLookupDefinitionRelation> joins, int i, out string objectCol, out string idCol)
        {
            var join = joins[i];
            var currentObj = join.Object;

            if (join.ObjectID > 0)
                currentObj = currentObj.Replace("Type", "");

            var currentObjTable = currentObj;
            var currentObjIdColumn = "ID";

            if (currentObj == "Resource")
            {
                currentObjTable = "reporting.Global_Resource";
                currentObjIdColumn = "ResourceID";
            }
            else
            {
                currentObjTable = "[" + currentObj + "]";
            }

            var addDeletedCheck = currentObj.Equals("FusionAttribute", StringComparison.CurrentCultureIgnoreCase) ||
                                                      currentObj.Equals("FusionQueryAttribute", StringComparison.CurrentCultureIgnoreCase);
            var previousObj = (i > 0) ? joins[i - 1].Object.Replace("Type", "") : "";
            var previousObjIdColumn = previousObj == "Resource" ? "ResourceID" : "ID";
            var objColumn = "";
            var objIDColumn = "";
            var joinType = "inner"; //the SQL join.

            var permissionJoin = $@"
                inner join Asset O{i} on O{i}.Object = '{currentObj}' and O{i}.ObjectID = A{i}.ID ";
            var permissionsWhere = $" O{i}.ID not in ({GetNoReadSqlStatement()}) ";
            switch (currentObj.ToLower())
            {
                case "artifact":
                case "policy":
                case "taxonomy":
                    break;
                default:
                    permissionJoin = "";
                    permissionsWhere = "";
                    break;
            }

            switch (join.RelationType)
            {
                case ComplexLookupRelationType.StandardRelationhip:
                    #region
                    switch (join.Direction)
                    {
                        case FieldTypeComplexLookupRelationDirection.Back:
                            join.JoinStatement = (i == 0) ? 
                                $"from [Intersect] I{i}" : 
                                $"{joinType} join [Intersect] I{i} on I{i}.IntersectTypeID = {join.IntersectTypeID} and (I{i}.Deleted = 0 or I{i}.Deleted is null) and I{i}.Object = '{previousObj}' and I{i}.ObjectID = A{i - 1}.{previousObjIdColumn}";
                            break;
                        case FieldTypeComplexLookupRelationDirection.Forward:
                            join.JoinStatement = (i == 0) ? 
                                $"from [Intersect] I{i}" : 
                                $"{joinType} join [Intersect] I{i} on I{i}.IntersectTypeID = {join.IntersectTypeID} and (I{i}.Deleted = 0 or I{i}.Deleted is null) and I{i}.Subject = '{previousObj}' and I{i}.SubjectID = A{i - 1}.{previousObjIdColumn}";
                            break;
                        default:
                            join.JoinStatement = (i == 0) ? 
                                $"from [Intersect] I{i}" : 
                                $"{joinType} join [Intersect] I{i} on I{i}.IntersectTypeID = {join.IntersectTypeID} and (I{i}.Deleted = 0 or I{i}.Deleted is null) and ( (I{i}.Subject = '{previousObj}' and I{i}.SubjectID = A{i - 1}.{previousObjIdColumn}) OR (I{i}.Object = '{previousObj}' and I{i}.ObjectID = A{i - 1}.{previousObjIdColumn} ) )";
                            break;
                    }
                    
                    if (i == 0)
                    {
                        switch (join.Direction)
                        {
                            case FieldTypeComplexLookupRelationDirection.Back:
                                join.JoinStatement += $" inner join {currentObjTable} A{i} on A{i}.{currentObjIdColumn} = I{i}.SubjectID";
                                join.WhereStatement = $"I{i}.IntersectTypeID = {join.IntersectTypeID} and (I{i}.Deleted = 0 or I{i}.Deleted is null) and I{i}.Object = '{type}' and I{i}.ObjectID = {id}";
                                objColumn = $"I{i}.Subject";
                                objIDColumn = $"I{i}.SubjectID";
                                break;
                            case FieldTypeComplexLookupRelationDirection.Forward:
                                join.JoinStatement += $" inner join {currentObjTable} A{i} on A{i}.{currentObjIdColumn} = I{i}.ObjectID";
                                join.WhereStatement = $"I{i}.IntersectTypeID = {join.IntersectTypeID} and (I{i}.Deleted = 0 or I{i}.Deleted is null) and I{i}.Subject = '{type}' and I{i}.SubjectID = {id}";
                                objColumn = $"I{i}.Object";
                                objIDColumn = $"I{i}.ObjectID";
                                break;
                            default:
                                join.JoinStatement += $" inner join {currentObjTable} A{i} on A{i}.{currentObjIdColumn} = case when (I{i}.Subject = '{type}' and I{i}.SubjectID = {id}) then I{i}.ObjectID else I{i}.SubjectID end";
                                join.WhereStatement = $"I{i}.IntersectTypeID = {join.IntersectTypeID} and (I{i}.Deleted = 0 or I{i}.Deleted is null) and ( (I{i}.Subject = '{type}' and I{i}.SubjectID = {id}) OR (I{i}.Object = '{type}' and I{i}.ObjectID = {id} ) )";
                                objColumn = $"case when (I{i}.Subject = '{type}' and I{i}.SubjectID = {id}) then I{i}.Object else I{i}.Subject end";
                                objIDColumn = $"case when (I{i}.Subject = '{type}' and I{i}.SubjectID = {id}) then I{i}.ObjectID else I{i}.SubjectID end";
                                break;
                        }

                        join.JoinStatement += permissionJoin;
                        join.WhereStatement += string.IsNullOrEmpty(join.WhereStatement) ? permissionsWhere : (string.IsNullOrEmpty(permissionsWhere) ? "" : $" and {permissionsWhere}");
                    }
                    else
                    {
                        switch (join.Direction)
                        {
                            case FieldTypeComplexLookupRelationDirection.Back:
                                join.JoinStatement += $" {joinType} join {currentObjTable} A{i} on A{i}.{currentObjIdColumn} = I{i}.SubjectID";
                                join.WhereStatement += string.IsNullOrEmpty(join.WhereStatement) ? permissionsWhere : (string.IsNullOrEmpty(permissionsWhere) ? "" : $" and {permissionsWhere}");
                                objColumn = $"I{i}.Subject";
                                objIDColumn = $"I{i}.SubjectID";
                                break;
                            case FieldTypeComplexLookupRelationDirection.Forward:
                                join.JoinStatement += $" {joinType} join {currentObjTable} A{i} on A{i}.{currentObjIdColumn} = I{i}.ObjectID";
                                join.WhereStatement += string.IsNullOrEmpty(join.WhereStatement) ? permissionsWhere : (string.IsNullOrEmpty(permissionsWhere) ? "" : $" and {permissionsWhere}");
                                objColumn = $"I{i}.Object";
                                objIDColumn = $"I{i}.ObjectID";
                                break;
                            default:
                                join.JoinStatement += $" {joinType} join {currentObjTable} A{i} on A{i}.{currentObjIdColumn} = case when (I{i}.Subject = '{previousObj}' and I{i}.SubjectID = A{i - 1}.{previousObjIdColumn}) then I{i}.ObjectID else I{i}.SubjectID end";
                                join.WhereStatement += string.IsNullOrEmpty(join.WhereStatement) ? permissionsWhere : " and " + permissionsWhere;
                                objColumn = $"case when (I{i}.Subject = '{currentObj}' and I{i}.SubjectID = A{i - 1}.{currentObjIdColumn}) then I{i}.Object else I{i}.Subject end";
                                objIDColumn = $"case when (I{i}.Subject = '{currentObj}' and I{i}.SubjectID = A{i - 1}.{currentObjIdColumn}) then I{i}.ObjectID else I{i}.SubjectID end";
                                break;
                        }
                        join.JoinStatement += permissionJoin;
                    }
                    if (addDeletedCheck)
                    {
                        join.JoinStatement += $" and (A{i}.Deleted = 0 OR A{i}.Deleted is null)";
                    }
                    break;
                #endregion
                case ComplexLookupRelationType.ChildRelationship:
                    #region
                    join.JoinStatement = (i == 0) ? $"from [Intersect] I{i}" : $"{joinType} join [Intersect] I{i} on I{i}.Subject = 'Intersect' and I{i}.SubjectID = I{i - 1}.ID and I{i}.IntersectTypeID = {join.IntersectTypeID} and (I{i}.Deleted = 0 or I{i}.Deleted is null)";
                    join.JoinStatement += $" {joinType} join {currentObjTable} A{i} on I{i}.Object = '{join.Object.Replace("Type", "")}' and A{i}.ID = I{i}.ObjectID";
                    join.JoinStatement += permissionJoin;
                    objColumn = $"'{join.Object.Replace("Type", "")}'";
                    objIDColumn = $"I{i}.ObjectID";
                    if (i == 0)
                    {
                        join.WhereStatement = $"( (I{i}.Subject = '{type}' and I{i}.SubjectID = {id}) OR (I{i}.Object = '{type}' and I{i}.ObjectID = {id} ) ) and (I{i}.Deleted = 0 or I{i}.Deleted is null)";
                    }
                    if (addDeletedCheck)
                    {
                        join.JoinStatement += $" and A{i}.Deleted = 0";
                    }
                    join.WhereStatement += string.IsNullOrEmpty(join.WhereStatement) ? permissionsWhere : (string.IsNullOrEmpty(permissionsWhere) ? "" : $" and {permissionsWhere}");
                    break;
                #endregion
                case ComplexLookupRelationType.ChildItem:
                    #region
                    switch (join.Object)
                    {
                        case "ArtifactType":
                            join.JoinStatement = (i == 0) ? $"from Artifact A{i}" : $"{joinType} join Artifact A{i} on A{i}.ID in (select ChildID from dbo.GetChildObjectIds('Artifact',A{i - 1}.ID))";
                            if (i == 0)
                            {
                                join.WhereStatement = $"A{i}.ArtifactTypeID = {join.ObjectID} and A{i}.ID in (select ChildID from dbo.GetChildObjectIds('Artifact',{id}))";
                            }
                            break;
                        case "FusionAttributeType":
                            join.JoinStatement = (i == 0) ? $"from FusionAttribute A{i}" : $"{joinType} join FusionAttribute A{i} on A{i}.ParentID = A{i - 1}.ID and A{i}.FusionAttributeTypeID = {join.ObjectID} and A{i}.Deleted = 0";
                            if (i == 0)
                            {
                                join.WhereStatement = $"A{i}.FusionAttributeTypeID = {join.ObjectID} and A{i}.ParentID = {id} and A{i}.Deleted = 0";
                            }
                            break;
                    }

                    join.JoinStatement += permissionJoin;
                    join.WhereStatement += string.IsNullOrEmpty(join.WhereStatement) ? permissionsWhere : (string.IsNullOrEmpty(permissionsWhere) ? "" : $" and {permissionsWhere}");

                    objColumn = $"'{currentObj}'";
                    objIDColumn = $"A{i}.ID";
                    break;
                #endregion
                case ComplexLookupRelationType.ParentItem:
                    #region
                    switch (join.Object)
                    {
                        case "ArtifactType":
                            join.JoinStatement = (i == 0) ? $"from Artifact A{i}" : $"{joinType} join Artifact A{i} on A{i}.ID = dbo.GetParentObjectId('Artifact', A{i - 1}.ID) and A{i}.ArtifactTypeID = {join.ObjectID}";
                            if (i == 0)
                            {
                                join.WhereStatement = $"A{i}.ArtifactTypeID = {join.ObjectID} and A{i}.ID = dbo.GetParentObjectId('Artifact', {id})";
                            }
                            break;
                        case "FusionAttributeType":
                            join.JoinStatement = (i == 0) ? $"from FusionAttribute A{i}" : $"{joinType} join FusionAttribute A{i} on A{i}.ID = A{i - 1}.ParentID and A{i}.FusionAttributeTypeID = {join.ObjectID} and A{i}.Deleted = 0";
                            if (i == 0)
                            {
                                join.WhereStatement = $"A{i}.FusionAttributeTypeID = {join.ObjectID} and A{i}.ID in (select ParentID from FusionAttribute where ID = {id}) and A{i}.Deleted = 0";
                            }
                            break;
                    }

                    join.JoinStatement += permissionJoin;
                    join.WhereStatement += string.IsNullOrEmpty(join.WhereStatement) ? permissionsWhere : (string.IsNullOrEmpty(permissionsWhere) ? "" : $" and {permissionsWhere}");

                    objColumn = $"'{currentObj}'";
                    objIDColumn = $"A{i}.ID";
                    break;
                #endregion
                default:
                    break;
            }
            objectCol = objColumn;
            idCol = objIDColumn;
        }

        private bool AnyComplexLookupGridValues(string type, int id, FieldTypeLookup lookup)
        {
            var def = lookup.ParseComplexLookupDefinition();
            type = type.CleanForSql();

            #region Process Relations

            for (var i = 0; i < def.Relations.Count; i++)
            {
                string objColumn, objIDColumn;
                loadComplexLookupJoin(type, id, def.Relations, i, out objColumn, out objIDColumn);
            }

            #endregion

            var sqlQuery = @"select  case 
            when count(1) > 0 then cast(1 as bit)
			else cast(0 as bit)

        end ";
            sqlQuery += string.Join(" ", def.Relations.Select(i => i.JoinStatement));

            var whereQuery = string.Join(" AND ", def.Relations.Where(i => !string.IsNullOrEmpty(i.WhereStatement)).Select(i => i.WhereStatement));
            if (!string.IsNullOrEmpty(whereQuery)) whereQuery = " where " + whereQuery;
            sqlQuery += whereQuery + " ";

            return Company.Query<bool>(sqlQuery).First();
        }

        [Route("ComplexLookupField/{type}/{id:int}/{fieldTypeID:int}/values")]
        public HttpResponseMessage GetComplexLookupGridField(string type, int id, int fieldTypeID)
        {
            var columnModels = new List<ComplexColumnModel>();
            var gridFields = new List<GridField>();
            var columns = new List<GridColumn>();
            IEnumerable<dynamic> results = null;

            try
            {
                var lookup = Company.Filter<FieldTypeLookup>(i => i.FieldTypeID == fieldTypeID).SingleOrDefault();
                if (lookup == null) throw new Exception("Invalid complex lookup field is specified.");

                var def = lookup.ParseComplexLookupDefinition();

                if (def.Fields == null || def.Fields.Count == 0) throw new Exception("There is an invalid Relation Look up field. There is no field specified in the Relation Lookup definition. Please specify one or more fields in the Relation Lookup definition.");
                
                var fields = def.Fields.ToList();

                var fieldTypeIDs = fields.Where(i => i.FieldTypeID != 0).Select(x => x.FieldTypeID).ToList();
                var fieldTypes = Company.Filter<FieldType>(i => fieldTypeIDs.Contains(i.ID)).ToList();

                if (
                    (def.Fields.Count > 0) &&
                        (
                            (
                                (fieldTypes == null || fieldTypes.Count == 0) &&
                                !def.Fields.Any(x => (x.FieldTypeName == "TextPath") || (x.FieldTypeName == "Name" && x.FieldTypeID == 0))
                            )
                        )
                    )
                {
                    throw new Exception("The relationship lookup field has 0 valid fields to display. Please verify the definition is correct.");
                }

                type = type.CleanForSql();

                for (var i = 0; i < def.Relations.Count; i++)
                {
                    var join = def.Relations[i];
                    string objColumn, objIDColumn;

                    loadComplexLookupJoin(type, id, def.Relations, i, out objColumn, out objIDColumn);
                    loadComplexLookupColumns(fieldTypes, fields, columnModels, join, $"I{i}.ID", objColumn, objIDColumn, "left", i);
                }

                var sqlQuery = "select distinct " + string.Join(", ", columnModels.Where(i => i.DisplayOrder > 0).OrderBy(i => i.DisplayOrder).Select(i => $"{i.DisplayColumn} as [{i.datafield}]")) + " ";
                if (columnModels.Any(i => i.SortOrder.HasValue && i.SortOrder > 0 && !string.IsNullOrEmpty(i.SortColumn)))
                    sqlQuery += ", " + string.Join(", ", columnModels.Where(i => i.SortOrder.HasValue && i.SortOrder > 0 && !string.IsNullOrEmpty(i.SortColumn)).Select(i => $"{i.SortColumn} as [Sort_{i.datafield}]")) + " ";
                sqlQuery += string.Join(" ", def.Relations.Select(i => i.JoinStatement)) + " ";

                var whereQuery = string.Join(" AND ", def.Relations.Where(i => !string.IsNullOrEmpty(i.WhereStatement)).Select(i => i.WhereStatement));
                var filterWhereQuery = string.Join(" AND ", columnModels.Where(i => !string.IsNullOrEmpty(i.Filter)).Select(i => $"{i.DisplayColumn} like '{i.Filter.CleanForSql()}%'"));
                if (!string.IsNullOrEmpty(filterWhereQuery))
                    whereQuery += " AND " + filterWhereQuery;

                if (!string.IsNullOrEmpty(whereQuery)) whereQuery = " where " + whereQuery;
                sqlQuery += whereQuery + " ";

                var orderQuery = string.Join(", ", columnModels.Where(i => i.SortOrder.HasValue && i.SortOrder > 0).OrderBy(i => i.SortOrder).Select(i => string.IsNullOrEmpty(i.SortColumn) ? $"[{i.datafield}]" : $"[Sort_{i.datafield}]"));
                if (!string.IsNullOrEmpty(orderQuery)) orderQuery = " order by " + orderQuery;
                sqlQuery += orderQuery;

                columnModels = columnModels.OrderBy(c => c.DisplayOrder).ToList();

                columnModels.ForEach(c =>
                {
                    if (!gridFields.Any(gf => gf.name == c.datafield))
                    {
                        gridFields.Add(new GridField { name = c.datafield, type = c.datafieldtype ?? "string" });
                    }

                    if (c.OutputColumn)
                    {
                        if (!columns.Any(gc => gc.datafield == c.datafield && gc.text == c.text))
                        {
                            var gc = new GridColumn
                            {
                                text = c.text,
                                datafield = c.datafield,                                
                                columntype = c.texttype,
                                contextfield = c.contextfield,
                                objectfield = c.objectfield,
                                objectidfield = c.objectidfield,
                                urlfield = c.urlfield,
                                description = c.description,
                                columnWidth = c.Width
                                
                            };
                            if (!string.IsNullOrEmpty(c.format)) gc.cellsformat = c.format;
                            columns.Add(gc);
                        }
                    }
                });


                results = Company.Query<dynamic>(sqlQuery);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new ErrorResponse { title = "Error", message = ex.GetFullExceptionData() }
                );
            }

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = results,
                Columns = columns,
                Fields = gridFields
            });
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

        #region Reference List Items Field

        private List<DetailReadOnlyRowModel> RenderReferenceListItemsField(string type, int id, int fieldTypeID)
        {
            var list = new List<DetailReadOnlyRowModel>();
            var ft = Company.GetById<FieldType>(fieldTypeID);

            if (ft != null)
            {
                if (ft.LookupObjectType == SystemObjects.IntersectType.ToString() && ft.LookupObjectID.HasValue)
                {
                    var intersect = Company.Filter<IntersectDetail>(i => i.IntersectTypeID == ft.LookupObjectID.Value && ( (i.Subject == type && i.SubjectID == id) || (i.Object == type && i.ObjectID == id) ) ).FirstOrDefault();
                    if (intersect != null)
                    {
                        var referenceItemTypeID = (intersect.Subject == type && intersect.SubjectID == id) ? intersect.ObjectID : intersect.SubjectID;
                        if (AnyReferenceListItemsFieldValues(referenceItemTypeID))
                        {
                            var referenceItemType = Company.GetById<ReferenceItemType>(referenceItemTypeID);

                            list.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField {
                                        Column = 1,
                                        Name = $"{ft.FriendlyName} Name",
                                        FieldDescription = ft.DisplayDescription,
                                        FieldName = ft.Name,
                                        Value = referenceItemType.Name
                                    }
                                },
                                SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField {
                                        Column = 2,
                                        Name = $"{ft.FriendlyName} Description",
                                        FieldDescription = ft.DisplayDescription,
                                        FieldName = ft.Name,
                                        Value = referenceItemType.Description
                                    }
                                },
                                Category = ft.Category
                            });

                            list.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField {
                                        Column = 1,
                                        Name = $"{ft.FriendlyName} Items",
                                        FieldDescription = ft.DisplayDescription,
                                        FieldName = ft.Name,
                                        HideHeader = false,
                                        HideFooter = false,
                                        HideFilter = false,
                                        LookupGridUrl = $"/api/ReferenceListItemsField/{referenceItemTypeID}/values",
                                        LookupObjectType = "ReferenceListItem",
                                        LookupObjectID = referenceItemTypeID,
                                        LookupFieldTypeID = 0,
                                        LookupType = (int)DataType.RefListRelationship
                                    }
                                },
                                Category = ft.Category
                            });
                        }
                    }
                }
            }

            return list;
        }

        private bool AnyReferenceListItemsFieldValues(int id)
        {
            var sqlQuery = $"select case when count(1) > 0 then cast(1 as bit) else cast(0 as bit) end from ReferenceItem where ReferenceItemTypeID = {id}";
            return Company.Query<bool>(sqlQuery).First();
        }

        [Route("ReferenceListItemsField/{id:int}/values")]
        public HttpResponseMessage GetReferenceListItemsField(int id)
        {
            var gridFields = new List<GridField>();
            var columns = new List<GridColumn>();
            IEnumerable<dynamic> results = null;

            try
            {
                var fieldTypes = Company.Filter<FieldType>(i => i.Object == "ReferenceItemType" && i.ObjectID == id && i.IsListable).OrderBy(i => i.ColumnOrder).ToList();

                columns.Add(new GridColumn { datafield = "Code", text = "Code" });
                gridFields.Add(new GridField { name = "Code", type = "string" });

                var sqlJoins = "";
                var sqlColumns = "";
                var sqlOrderBy = "";
                foreach (var f in fieldTypes)
                {
                    sqlColumns += $", F{f.ID}.FormattedValue as [{f.Name}]";
                    sqlJoins += $" left join Field F{f.ID} on F{f.ID}.FieldTypeID = {f.ID} and F{f.ID}.ObjectType = 'ReferenceItem' and F{f.ID}.ObjectID = R.ID";

                    var gc = new GridColumn { datafield = f.Name, text = f.FriendlyName };
                    if (f.ColumnWidth.HasValue)
                    {
                        gc.columnWidth = f.ColumnWidth.Value;
                    }

                    columns.Add(gc);
                    gridFields.Add(new GridField { name = $"{f.Name}", type = getGridFieldTypeForColumn(f) });
                }
                foreach (var f in fieldTypes.Where(i => i.SortOrder > 0).OrderBy(i => i.SortOrder))
                {
                    sqlOrderBy += (string.IsNullOrEmpty(sqlOrderBy) ? "" : ", ") + $"F{f.ID}.FormattedValue";
                }
                if (!string.IsNullOrEmpty(sqlOrderBy))
                {
                    sqlOrderBy = $" order by {sqlOrderBy}";
                }

                var sqlQuery = $@"
select  R.Code
        {sqlColumns} 
from    ReferenceItem R 
        inner join Asset O on O.Object = 'ReferenceItem' and O.ObjectID = R.ID 
        {sqlJoins} 
where   R.ReferenceItemTypeID = {id} 
        and O.ID not in ({GetNoReadSqlStatement()})
{sqlOrderBy}";

                results = Company.Query<dynamic>(sqlQuery).Distinct();
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    Error = ex.GetFullExceptionData(),
                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = results,
                Columns = columns,
                Fields = gridFields
            });
        }

        #endregion

        #region Ownership Lookup Fields

        private List<DetailReadOnlyRowModel> RenderOwnershipLookupField(string type, int id, int fieldTypeID)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var ft = Company.GetById<FieldType>(fieldTypeID, i => i.FieldTypeLookup);
            var lookup = ft.FieldTypeLookup;

            if (ft != null && lookup != null)
            {
                if (AnyOwnershipLookupGridValues(type, id))
                {
                    list.Add(new DetailReadOnlyRowModel
                    {
                        columns = 1,
                        FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField {
                                        Column = 1,
                                        Name = ft.FriendlyName,
                                        FieldDescription = ft.DisplayDescription,
                                        FieldName = ft.Name,
                                        HideHeader = lookup.HideHeader,
                                        HideFooter = lookup.HideFooter,
                                        HideFilter = lookup.HideFilter,
                                        LookupGridUrl = $"/api/OwnershipLookupField/{type}/{id}/{ft.ID}/values",
                                        LookupObjectType = type,
                                        LookupObjectID = id,
                                        LookupFieldTypeID = ft.ID,
                                        LookupType = (int)DataType.OwnershipLookup
                                    }
                                },
                        Category = ft.Category
                    });
                }
            }

            return list;
        }

        private bool AnyOwnershipLookupGridValues(string type, int id)
        {            
            type = type.CleanForSql();

            var sql = @"
select  case 
            when count(1) > 0 then cast(1 as bit)
			else cast(0 as bit)
        end 
from    ResponsibilityDetail R
inner join Asset A on A.[Object] = @type and A.ObjectID = @id
inner join AssetType T on T.ID = A.AssetTypeID and T.ID = R.AssetTypeID
where R.IsVisible = 1 and ((R.Object = @type 
		and R.ObjectID = @id) or (R.ApplyToType = 1 and R.AssetTypeID = T.ID))";

            return Company.Query<bool>(sql, new { type, id }).First();
        }

        [Route("OwnershipLookupField/{type}/{id:int}/{fieldTypeID:int}/values")]
        public HttpResponseMessage GetOwnershipLookupGridField(string type, int id, int fieldTypeID)
        {            
            var gridFields = new List<GridField>();
            var columns = new List<GridColumn>();
            IEnumerable<dynamic> results = null;

            try
            {
                var lookup = Company.Filter<FieldTypeLookup>(i => i.FieldTypeID == fieldTypeID).SingleOrDefault();
                if (lookup == null) throw new Exception("Invalid ownership lookup field is specified.");
                var assetId = Company.Assets.FirstOrDefault(a => a.Object == type && a.ObjectID == id)?.ID ?? 0;

                var def = lookup.ParseOwnershipLookupDefinition();
                type = type.CleanForSql();

                var sql = @"
SELECT  R.ResponsibilityTypeName, 
        case SecurityAsset when 'G' then 'Group' when 'O' then 'Organization' else 'Resource' end as SecurityAsset, 
        SecurityAssetID, 
        case SecurityAsset when 'R' then '' else SecurityAssetName end as SecurityAssetName, 
        'Preview' as SecurityAssetContext, 
        U.FirstName + ' ' + U.LastName as ResourceName, 
        R.ResourceID, 
        'Resource' as ResourceObject, 
        'Preview' as ResourceItemContext, 
        '/resource/' + cast(R.ResourceID as varchar) as ResourceItemUrl,
        R.Context
from    [dbo].[ResponsibilityAllAsset] R
        inner join Asset A on A.ID = @assetId
        inner join AssetType T on T.ID = A.AssetTypeID and T.ID = R.AssetTypeID
        inner join reporting.Global_Resource U on U.ResourceID = R.ResourceID and U.State = 1 
where   R.IsVisible = 1 and ((R.AssetID = @assetId) or (R.ApplyToType = 1 and R.AssetTypeID = T.ID))";

                gridFields.Add(new GridField { name = "ResponsibilityTypeName", type = "string" });
                gridFields.Add(new GridField { name = "ResourceName", type = "lookup" });
                gridFields.Add(new GridField { name = "ResourceID", type = "number" });
                gridFields.Add(new GridField { name = "ResourceObject", type = "string" });
                gridFields.Add(new GridField { name = "ResourceItemContext", type = "string" });
                gridFields.Add(new GridField { name = "ResourceItemUrl", type = "string" });
                gridFields.Add(new GridField { name = "SecurityAsset", type = "string" });
                gridFields.Add(new GridField { name = "SecurityAssetID", type = "number" });
                gridFields.Add(new GridField { name = "SecurityAssetName", type = "lookup" });
                gridFields.Add(new GridField { name = "SecurityAssetContext", type = "string" });
                gridFields.Add(new GridField { name = "Context", type = "html" });

                columns.Add(new GridColumn
                {
                    text = "Responsibility",
                    datafield = "ResponsibilityTypeName",
                    columntype = "textbox",
                    filtertype = "textbox"
                });

                columns.Add(new GridColumn
                {
                    text = "Assigned User",
                    datafield = "ResourceName",
                    contextfield = "ResourceItemContext",
                    objectfield = "ResourceObject",
                    objectidfield = "ResourceID",
                    urlfield = "ResourceItemUrl",
                    columntype = "textbox",
                    filtertype = "textbox"
                });

                if (def.DisplayAssignmentSource)
                {
                    columns.Add(new GridColumn
                    {
                        text = "Via",
                        datafield = "SecurityAssetName",
                        contextfield = "SecurityAssetContext",
                        objectfield = "SecurityAsset",
                        objectidfield = "SecurityAssetID",
                        columntype = "textbox",
                        filtertype = "textbox"
                    });
                }

                columns.Add(new GridColumn
                {
                    text = "Context",
                    datafield = "Context",
                    columntype = "textbox",
                    filtertype = "textbox"
                });

                results = Company.Query<dynamic>(sql, new { assetId }).Distinct();
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    Error = ex.GetFullExceptionData(),
                });
            }

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = results,
                Columns = columns,
                Fields = gridFields
            });
        }

        #endregion

        #region Relationships

        [HttpGet, Route("RelationshipObjectsByType")]
        public async Task<IEnumerable<FilterObjectItem>> RelationshipObjectsByType(SystemObjects type, int id, int intersectTypeId)
        {
            var sql = "";

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    sql = @"select distinct disp.DisplayValue as Name, ASS.ObjectID as ID, 'Artifact' as [Type] 
							from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'ArtifactType')                            
                            inner join [Intersect] I on ( (I.Subject = 'Artifact' and ASS.ObjectID = I.SubjectID)) 
							cross apply [dbo].GetAssetDisplayValueById(ASS.ID) disp
							union
							select distinct disp.DisplayValue as Name, ASS.ObjectID as ID, 'Artifact' as [Type] 
                            from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'ArtifactType')     
                            inner join [Intersect] I on ( (I.Object = 'Artifact' and ASS.ObjectID = I.ObjectID) ) 
                            cross apply [dbo].GetAssetDisplayValueById(ASS.ID) disp
                            order by disp.DisplayValue";
                    break;
                case SystemObjects.FusionAttributeType:                    
                    sql = @"select distinct A.TextPath as Name, A.ID, 'FusionAttribute' as [Type] 
                            from FusionAttribute A 
                            inner join [Intersect] I on A.FusionAttributeTypeID = @id and ( (I.Subject = 'FusionAttribute' and A.ID = I.SubjectID) ) 
                            union 
                            select distinct A.TextPath as Name, A.ID, 'FusionAttribute' as [Type] 
                            from FusionAttribute A 
                            inner join [Intersect] I on A.FusionAttributeTypeID = @id and ( (I.Object = 'FusionAttribute' and A.ID = I.ObjectID) ) 
                            order by A.TextPath
                            ";
                    break;
                case SystemObjects.IntersectType:
                    sql = @"select distinct iname.Name as Name, A.ID, 'Intersect' as [Type] 
                            from [Intersect] A 
                            inner join [Intersect] I on A.IntersectTypeID = @id and ( (I.Subject = 'Intersect' and A.ID = I.SubjectID) OR (I.Object = 'Intersect' and A.ID = I.ObjectID) ) 
                            cross apply [dbo].getintersectNames(A.ID) iname
                            order by iname.Name";
                    break;
                case SystemObjects.PolicyType:
                case SystemObjects.Policy:                    
                    sql = @"select distinct disp.TextPath as Name, ASS.ObjectID as ID, 'Policy' as [Type] 
							from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'PolicyType')                            
                            inner join [Intersect] I on ( (I.Subject = 'Policy' and ASS.ObjectID = I.SubjectID)) 
							cross apply [dbo].GetAssetTextPathById(ASS.ID,'/') disp
							union
							select distinct disp.TextPath as Name, ASS.ObjectID as ID, 'Policy' as [Type] 
                            from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'PolicyType')     
                            inner join [Intersect] I on ( (I.Object = 'Policy' and ASS.ObjectID = I.ObjectID) ) 
                            cross apply [dbo].GetAssetTextPathById(ASS.ID,'/') disp
                            order by disp.TextPath";
                    break;
                case SystemObjects.ReferenceItemType:
                    sql = @"select distinct A.DisplayValue as Name, A.ID, 'ReferenceItem' as [Type] 
                            from ReferenceItem A 
                            inner join [Intersect] I on A.ReferenceItemTypeID = @id and ( (I.Subject = 'ReferenceItem' and A.ID = I.SubjectID) OR (I.Object = 'ReferenceItem' and A.ID = I.ObjectID) ) 
                            order by A.DisplayValue";
                    break;
                case SystemObjects.ResourceType:
                    sql = @"select distinct A.LastName + ', ' + A.FirstName as Name, A.ResourceID as ID, 'Resource' as [Type] 
                            from reporting.Global_Resource A 
                            inner join [Intersect] I on ( (I.Subject = 'Resource' and A.ResourceID = I.SubjectID) OR (I.Object = 'Resource' and A.ResourceID = I.ObjectID) ) 
                            order by 1";
                    break;
                case SystemObjects.RuleType:
                case SystemObjects.Rule:
                    sql = @"select distinct A.DisplayValue as Name, A.ID, 'Rule' as [Type] 
                            from [Rule] A 
                            inner join [Intersect] I on A.RuleTypeID = @id and (I.Subject = 'Rule' and A.ID = I.SubjectID)
                            union
                            select distinct A.DisplayValue as Name, A.ID, 'Rule' as [Type] 
                            from [Rule] A 
                            inner join [Intersect] I on A.RuleTypeID = @id and (I.Object = 'Rule' and A.ID = I.ObjectID)
                            order by A.DisplayValue";
                    break;
                case SystemObjects.TaxonomyType:
                    sql = @"select distinct disp.TextPath as Name, ASS.ObjectID as ID, 'Taxonomy' as [Type] 
							from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'TaxonomyType')                            
                            inner join [Intersect] I on ( (I.Subject = 'Taxonomy' and ASS.ObjectID = I.SubjectID)) 
							cross apply [dbo].GetAssetTextPathById(ASS.ID,'/') disp
							union
							select distinct disp.TextPath as Name, ASS.ObjectID as ID, 'Taxonomy' as [Type] 
                            from AssetType ATT
							inner join Asset ASS on (ATT.ID = ASS.AssetTypeID and ATT.ObjectID  = @id and ATT.[Object] = 'TaxonomyType')     
                            inner join [Intersect] I on ( (I.Object = 'Taxonomy' and ASS.ObjectID = I.ObjectID) ) 
                            cross apply [dbo].GetAssetTextPathById(ASS.ID,'/') disp
                            order by disp.TextPath";
                    break;
                case SystemObjects.MapType:
                    sql = @"
select	C.DisplayValue as Name, 
		ObjectID as ID, 
		[Object] as [Type] 
from	AssetDetail C 
		inner join (
			select	distinct 
					case 
						when Subject = 'Map' then Object
						else Subject
					end as O,
					case 
						when Subject = 'Map' then ObjectID
						else SubjectID
					end as OID
			from	[Intersect]
			where	IntersectTypeID = @intersectTypeId
		) I on I.O = C.Object and I.OID = C.ObjectID
order by C.DisplayValue";
                    break;
                default:
                    sql = "";
                    break;
            }

            if (string.IsNullOrEmpty(sql)) return null;

            return await Company.QueryAsync<FilterObjectItem>(sql, new { id = id, intersectTypeId = intersectTypeId });
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

        [Route("{focal}/{focalID:int}/sources/{obj}/{objID:int}/rules")]
        public HttpResponseMessage GetSourceRules(string focal, int focalID, string obj, int objID)
        {
            return Request.CreateResponse(HttpStatusCode.OK,
                Company.Query<dynamic>(QueryConstants.SourceRuleList,
                new
                {
                    focal = new Dapper.DbString { Value = focal, IsAnsi = true },
                    focalID,
                    obj = new Dapper.DbString { Value = obj, IsAnsi = true },
                    objID
                })
            );
        }

        [Route("relationships/{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteRelationship(int id)
        {
            var msg = new HttpResponseMessage();
            try
            {
                Company.DeleteRelationship(id);
                msg.StatusCode = HttpStatusCode.OK;
                msg.ReasonPhrase = "Relationship successfully removed.";
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusMessage;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 16)    //this is an app-specific error
                {
                    msg.StatusCode = HttpStatusCode.Conflict;
                    msg.ReasonPhrase = ex.Message;
                }
                else
                {
                    msg.StatusCode = HttpStatusCode.InternalServerError;
                    msg.ReasonPhrase = ex.Message;
                }
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = ex.Message;
            }

            return msg;
        }

        [Route("relationships/field/{fieldTypeID:int}"), HttpGet]
        public HttpResponseMessage GetRelationshipFieldItems(int fieldTypeID, string @object = null, int? objectID = null, int offset = 0, int rows = 25, string query = null)
        {
            var result = Company.GetRelationshipFieldItems(fieldTypeID, @object, objectID, offset, rows, query, false);
            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                items = ((List<dynamic>)result["Items"]).Select(s => new System.Web.Mvc.SelectListItem { Text = s.Text, Value = s.Value.ToString(), Selected = s.Selected == 1 ? true : false }).ToList(),
                count = (int)result["Count"]
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
            return Company.Query<dynamic>(@"
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
		inner join AssetType T on T.Object = RD.Type and T.ObjectID = RD.TypeID and RD.SecurityAsset = 'G' and RD.SecurityAssetID = @groupID 
            and T.Object = @o and T.ObjectID = @id and RD.IsVisible = 1", new { groupID, o = type.ToString(), id });
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

        [Route("ownership/admintypes")]
        public IQueryable<dynamic> GetAdminResponsibilityTypes()
        {
            if (!Company.CurrentResourceIsAdmin) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
            
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
        case D.Object
		 	when 'ArtifactType' then 'Artifacts :: '
			when 'TaxonomyType' then 'Models :: '
			when 'PolicyType' then 'Policies :: '
			when 'RuleType' then 'Rules :: '
			when 'FusionAttributeType' then 'Fusion Attributes :: '
			when 'FusionType' then 'Fusion Types :: '
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end  + P.[Path]  as AssetTypeName, 
        D.ID as AssetTypeID,
        R.ObjectType,
        R.ObjectID,
        R.PermissionsBitMask
from    ResponsibilityTypeRelation R 
        inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID and O.ID = @id 
        left join AssetType D on D.Object = R.ObjectType and D.ObjectID = R.ObjectID
        cross apply dbo.GetAssetTypeTextPathById(D.ID, ' / ') P",
            new { id }).ToList();

            list.ForEach(i =>
            {
                i.LoadPermissionsFromMask();
            });

            return list;
        }

        [Route("ownership/types/asset/{id:int}/relations")]
        public List<ResponsibilityTypeRelationViewModel> GetResponsibilityTypeRelationsByAssetType(int id)
        {
            var list = Company.Query<ResponsibilityTypeRelationViewModel>(@"
select  R.ResponsibilityTypeID,
        O.Name as ResponsibilityTypeName,
        D.Name as AssetTypeName, 
        D.ID as AssetTypeID,
        R.ObjectType,
        R.ObjectID,
        R.PermissionsBitMask
from    ResponsibilityTypeRelation R 
        inner join ResponsibilityType O on O.ID = R.ResponsibilityTypeID 
        inner join AssetType D on D.Object = R.ObjectType and D.ObjectID = R.ObjectID and D.ID = @id",
            new { id }).ToList();

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
        R.ResponsibilityTypeID, 
        R.Name, 
        R.Context,
        D.Name as ObjectName, 
        O.Name as ResponsibilityType 
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

        [Route("ownership/fusion/{id:int}/fusionresponsibilitytypes")]
        public HttpResponseMessage GetFusionTypeResponsibilityByFusion(int id)
        {
            var fusion = Company.GetById<Fusion>(id);
            if (fusion == null)
                id = -1;
            else
                id = fusion.FusionTypeID;

            return Request.CreateResponse(HttpStatusCode.OK,
                Company.Filter<ResponsibilityTypeRelation>(i => i.ObjectID == id && i.ObjectType == "FusionType", i => i.ResponsibilityType)
                .Select(i => new
                {
                    i.ResponsibilityTypeID,
                    i.ObjectID,
                    i.ObjectType,
                    Name = i.ResponsibilityType.Name,
                    Description = i.ResponsibilityType.Description
                })
                );
        }

        #endregion

        #region Policies

        [Route("policytypes")]
        public HttpResponseMessage GetPolicyTypes()
        {
            return Request.CreateResponse<dynamic>(
                HttpStatusCode.OK,
                Company.Query<PolicyType>(@"
select	    FAT.ID,
		    FAT.Name,
            FAT.Description,
		    FAT.MaximumDepth,
			FAT.DisplayFormat,
			T.CreatedBy,
			T.CreatedOn,
			T.UpdatedBy,
		    T.UpdatedOn,
            T.ID as AssetTypeID
from	    PolicyType FAT
		    inner join AssetType T on T.Object = 'PolicyType' and T.ObjectID = FAT.ID")
            .Select(i => new { i.Description, i.ID, i.MaximumDepth, i.Name, i.AssetTypeID })
            );
        }

        [Route("policytypes/{id:int}")]
        public HttpResponseMessage GetPolicyType(int id)
        {
            var row = Company.Query<dynamic>(QueryConstants.PolicySettingsItem, new { id }).Single();
            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object>() {
                    { "ID", row.ID },
                    { "Name", row.Name },
                    { "Description", row.Description },
                    { "AllowAttributes", (bool)row.AllowAttributes },
                    { "NymTypes", Company.Query<dynamic>(QueryConstants.ObjectNymTypes, new { id = id, ot = new DbString {Value = "PolicyType", IsFixedLength = true, IsAnsi = true, Length = 50 } }) },
                    { "MaximumDepth", row.MaximumDepth }
                }
            );
        }

        [Route("policytypes/{id:int}/policies")]
        public IEnumerable<dynamic> GetPoliciesByType(int id, bool stripHtml = false)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Policy", out joins, out columns, false, false);

            var permissionSql = @"case when exists (
                                        select 1 from UserAssetPermissions(@r, OA.AssetTypeID) u where u.PermissionsBitMask & 2 = 2 and u.AssetID = OA.ID
						                ) 
						                    then 1
						                    else 0

                                        end as P_CanEdit,
		                                case when exists(
                                                             select 1 from UserAssetPermissions(@r, OA.AssetTypeID) u where u.PermissionsBitMask & 4 = 4 and u.AssetID = OA.ID
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
        A.ID, 
        OA.[Uid],
        OA.ID as AssetID, 
        P.SubjectID as ParentID,
        TD.DisplayValue,
        {columns}
        A.[Level],
        {permissionSql}
from	[Policy] A 
        inner join Asset OA on OA.Object = 'Policy' and OA.ObjectID = A.ID and A.PolicyTypeID = @id 
        {joins} 
        left join dbo.GetAssetDisplayValue() TD on TD.ID = OA.ID
        outer apply (
					select	I.SubjectID
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = 'Policy' and I.ObjectID = A.ID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
					) P
where   OA.ID not in ({GetNoReadSqlStatement()})
        and OA.AssetTypeID not in ({GetAssetTypeNoReadSqlStatement()})";

            var sql = string.Format(@"select * from ({0}) A", querySql);

            sql = applyFilteringSuffix(sql, Request);

            sql += " order by A.DisplayValue";

            var policies = Company.Query<dynamic>(sql, new { id = id, r = Company.CurrentResourceID }).ToList();

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

        #region Qualifiers
        
        [Route("qualifier/resolutiontypes")]
        public IQueryable<dynamic> GetQualifierResolutionObjects()
        {
            return Company.Query<dynamic>(@"select ID, 'ArtifactType' as [Type],  'ArtifactType|' + cast(ID as varchar(50)) as [value],  'Artifact :: ' + [Name] as [label] from ArtifactType
                union all
                select ID, 'TaxonomyType' as [Type], 'TaxonomyType|' + cast(ID as varchar(50)) as [value],  'Model :: ' + [Name] as [label] from TaxonomyType
                union all
                select ID, 'ReferenceItemType' as [Type], 'ReferenceItemType|' + cast(ID as varchar(50)) as [value], 'Reference :: ' + [Name] as [label] from ReferenceItemType").AsQueryable();
        }

        #endregion
        
        #region Reports


        [Route("reports/layouts")]
        public IEnumerable<dynamic> GetReportLayouts()
        {
            return Company.Query<dynamic>(@"
                select      cast(ID as varchar(15)) as value,
                            Name as title
                from        ReportLayout
                order by    title
                ");
        }

        [Route("reports/targets")]
        public IEnumerable<dynamic> GetReportTargetAreas()
        {
            var items = Company.Query<dynamic>(@"
select      *
from        (                 
            select      'ArtifactType|' + cast(ID as varchar(15)) as value,
                        'Artifact Type : ' + Name as title
            from        ArtifactType                        
            union       
            select      'Artifact|' + cast(ID as varchar(15)) as value,
                        'Artifact Instance : ' + Name as title
            from        ArtifactType            
            union
            select      'Resource|1' as value,
                        'Resource' as title
            union
            select      'Taxonomy|' + cast(ID as varchar(15)) as value,
                        'Model Instance : ' + Name as title
            from        TaxonomyType
            union
            select      'TaxonomyType|' + cast(ID as varchar(15)) as value,
                        'Model Type : ' + Name as title
            from        TaxonomyType
            union
            select      'Policy|' + cast(ID as varchar(15)) as value,
                        'Policy Instance : ' + Name as title
            from        PolicyType
            union
            select      'PolicyType|' + cast(ID as varchar(15)) as value,
                        'Policy Type : ' + Name as title
            from        PolicyType
            union
            select      'RuleType|' + cast(ID as varchar(15)) as value,
                        'Rule Type : ' + Name as title
            from        RuleType
            union
            select      'FusionType|' + cast(ID as varchar(15)) as value,
                        'Fusion Type : ' + Name as title
            from        FusionType
) O
order by    title

").ToList();

            return items;
        }

        #endregion

        #region Resources

        [HttpGet, Route("resources")]
        public IQueryable<ResourceType> GetResourceTypes()
        {
            return Community.ResourceTypes.OrderBy(i => i.Name).AsQueryable();
        }

         [HttpGet,Route("resources/{typeID:int}")]
        public HttpResponseMessage GetResourcesByType(int typeID,string filter="")
        {
            var settings = Community.GetCompanySettings();
            //check that current user is an admin or the company settings allow users to be listed
            if (!Company.CurrentResourceIsAdmin && (settings["ShowResources"] ?? "").ToUpper() != "TRUE")
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            var joins = "";
            var columns = "";
            var filterSql = "";
            getDynamicFieldJoinStatements(typeID, "Resource", out joins, out columns, false, false);

             var querySql = $@"
select  A.FirstName,
		A.LastName,
        A.Email,
		A.LastLoggedInOn,
        case A.State when 1 then 'Active' else 'Inactive' end as [State],
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
                where State <> @excludeStatus
        ) A 
        {joins}";

            var dbArgs = new DynamicParameters();
            dbArgs.Add("excludeStatus", CompanyResourceState.Deleted);

            if (HideData3SixtyUsers())
            {
                querySql += " where (A.Email not like '%@data3sixty.com' and A.Email not like '%@infogix.com')";
            }

            if (!string.IsNullOrEmpty(filter))
            {
                 filterSql = " where " + this.GetColumnsWithGlobalFilter(filter, SystemObjects.ResourceType, typeID, "B", dbArgs).Result;
            }

            var sql = string.Format(@"select * from ({0}) B {1} order by FirstName", querySql, filterSql);


            return Request.CreateResponse(HttpStatusCode.OK, Company.Query<dynamic>(sql, dbArgs));
        }


        private async Task<string> GetColumnsWithGlobalFilter(string filter, SystemObjects type, int id,string alias,  DynamicParameters dbArgs )
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

            return wherecondition.Remove(0,2);
        }

        [HttpGet,Route("resources/{typeID:int}/excel/excel.xls")]
        public async Task<HttpResponseMessage> GetResourcesExcel(int typeID,string filter)
        {
            string headerString = await this.GetGridDefinitionByType(SystemObjects.ResourceType, typeID).Content.ReadAsStringAsync();
            dynamic header = JsonConvert.DeserializeObject<dynamic>(headerString);

            string resultString = await this.GetResourcesByType(typeID,filter).Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject<dynamic>(resultString);

            var document = new SLDocument();
            document.AddWorksheet("Users");

            int colIndex = 1;
            int rowIndex = 1;
            for (int i = 0; i < header.Columns.Count; i++)
            {
                document.SetCellValue(rowIndex, colIndex, header.Columns[i].text.Value);
                colIndex++;
            }

            for(int k = 0; k < result.Count; k++)
            {
                rowIndex++;
                colIndex = 1;
                for (int l = 0; l < header.Columns.Count; l++)
                {
                    var colField = header.Columns[l].datafield.Value;
                   
                    var value = result[k][colField].Value;

                    var dataType = "string";

                    for (int m = 0; m < header.Fields.Count; m++)
                    {
                        var field = header.Fields[m];
                        if (field["name"].Value == colField)
                        {
                            dataType = field["type"].Value;
                            break;
                        }

                    }

                    SetCellValue(document, rowIndex, colIndex, dataType, value);
                    colIndex++;
                }

            }

            var stream = new MemoryStream();
            document.SaveAs(stream);
            var len = stream.Length;
            stream.Position = 0;
            HttpResponseMessage response = null;
            // serve the file to the client      
            response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            response.Content.Headers.ContentLength = stream.Length;
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Users {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return response;

        }

        [Route("resources/{typeID:int}/{id:int}")]
        public Resource GetResource(int typeID, int id)
        {
            //check that the user can see other users profiles
            var settings = Community.GetCompanySettings();
            //check that current user is an admin or the company settings allow users to be listed
            if (id != Company.CurrentResourceID)
            {
                if (!Company.CurrentResourceIsAdmin && (settings["ShowResources"] ?? "").ToUpper() != "TRUE")
                    throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            //check that this user exists in this environment
            if (!Company.GlobalReportingResources.Where(x => x.ResourceID == id).Any())
            {
                // user is not a user of this environment get them outa here!
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            var model = Community.GetById<Resource>(id, i => i.ResourceType);

            if (model == null)
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            return model;
        }

        [HttpGet, Route("resources/find")]
        public IQueryable<PersonSearchResultModel> GetResourceSearchResults(string search)
        {
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                return GetCompanyResources()
                        .Where(i => i.Email.Trim().ToLower().StartsWith(search) || i.FirstName.Trim().ToLower().StartsWith(search) || i.LastName.Trim().ToLower().StartsWith(search))
                        .OrderBy(i => i.LastName).ThenBy(i => i.FirstName)
                        .Select(i => new PersonSearchResultModel
                        {
                            ID = i.ID,
                            FirstName = i.FirstName,
                            LastName = i.LastName
                        });
            }
            else
            {
                return GetCompanyResources()
                        .OrderBy(i => i.LastName).ThenBy(i => i.FirstName)
                        .Select(i => new PersonSearchResultModel
                        {
                            ID = i.ID,
                            FirstName = i.FirstName,
                            LastName = i.LastName
                        });
            }
        }

        #endregion

        #region Rules

        [Route("ruletypes")]
        public IEnumerable<dynamic> GetRuleTypes()
        {
            if (!Company.CurrentResourceIsAdmin) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));

            return Company.Query<dynamic>(@"
select      ID as AssetTypeID, 
            ObjectID as ID, 
            Name, 
            Description, 
            CreatedOn, 
            CreatedBy, 
            UpdatedOn, 
            UpdatedBy, 
            DisplayFormat 
from        AssetType 
where       Object = 'RuleType'
order by    Name
");
        }

        [Route("ruletypes/{id:int}")]
        public HttpResponseMessage GetRuleType(int id)
        {
            var row = Company.Query<dynamic>(QueryConstants.RuleSettingsItem, new { id }).Single();
            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object>() {
                    { "ID", row.ID },
                    { "Name", row.Name },
                    { "Description", row.Description },
                    { "AllowAttributes", (bool)row.AllowAttributes }, 
                    { "NymTypes", Company.Query<dynamic>(QueryConstants.ObjectNymTypes, new { id = id, ot = new DbString {Value = "RuleType", IsFixedLength = true, IsAnsi = true, Length = 50 } }) },
                    { "HasDashboards",Company.Reports.Any(x=>x.ObjectID == id && x.ObjectType == SystemObjects.RuleType.ToString() && x.ReportType != "legacy") }
                } 
            );
        }

        [Route("rules/{id:int}")]
        public HttpResponseMessage GetRules(int id)
        {            
            try
            {
                var dbArgs = new Dapper.DynamicParameters();

                dbArgs.Add("id", id);
                dbArgs.Add("r", Company.CurrentResourceID);

                var joins = "";
                var columns = "";
                getDynamicFieldJoinStatements(id, "Rule", out joins, out columns, false, false);

                var permissionSql = @"case when exists (
                                        select 1 from UserAssetPermissions(@r, O.AssetTypeID) u where u.PermissionsBitMask & 2 = 2 and u.AssetID = O.ID
						                ) 
						                    then 1
						                    else 0

                                        end as P_CanEdit,
		                                case when exists(
                                                             select 1 from UserAssetPermissions(@r, O.AssetTypeID) u where u.PermissionsBitMask & 4 = 4 and u.AssetID = O.ID
						                                   ) 
						                                   then 1
						                                   else 0

                                        end as P_CanDelete";

                if (Company.CurrentResourceIsAdmin)
                {
                    permissionSql = "1 as P_CanEdit, 1 as P_CanDelete";
                }

                var querySql = string.Format(@"
select	O.ID as AssetID,
        O.[Uid],
        A.ID,
        A.Threshold,
        A.RuleDimensionID,
        D.Name as Dimension,
        dbo.GenerateAssetUrl(O.ID) as Url,
        {0}
        A.RuleTypeID,
        {2}
from	[Rule] A
        inner join Asset O on O.Object = 'Rule' and O.ObjectID = A.ID 
        {1} 
        left join RuleDimension D on D.ID = A.RuleDimensionID 
where   A.RuleTypeID = @id 
        and O.ID not in (" + GetNoReadSqlStatement("@r") + ")" +
        "and O.AssetTypeID not in (" + GetAssetTypeNoReadSqlStatement("@r") + ")", columns, joins, permissionSql);

                var query = Company.Query<dynamic>(querySql, dbArgs);

                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.GetFullExceptionData());
            }
        }

        [Route("ruledimensions")]
        public IQueryable<RuleDimension> GetRuleDimensions()
        {
            return Company.Table<RuleDimension>();
        }

        [Route("ruleimplementations/{id:int}/")]
        public HttpResponseMessage GetRuleImplementation(int id)
        {
            var row = Company.GetById<RuleImplementation>(id, i => i.Rule.RuleType);
            var rule = Company.AssetDetails.FirstOrDefault(i => i.Object == SystemObjects.Rule.ToString() && i.ObjectID == row.RuleID);

            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object>() {
                    { "ID", row.ID },
                    { "Name", row.Name ?? $"Implementation {row.ID}" },
                    { "SourceID", row.SourceID },
                    { "SourceUri", row.SourceUri },
                    { "RuleID", row.RuleID },
                    { "RuleName", rule?.DisplayValue ?? "" },
                    { "RuleTypeID", row.Rule.RuleTypeID },
                    { "RuleTypeName", row.Rule.RuleType.Name },
                    { "CreatedOn", row.CreatedOn.GetValueOrDefault() },
                    { "UpdatedOn", row.UpdatedOn.GetValueOrDefault() }
                }
            );
        }

        [Route("rules/{id:int}/implementations")]
        public HttpResponseMessage GetRuleImplementations(int id)
        {            
            try
            {
                var query = Company.Query<dynamic>(@"
select	A.ID,
        A.RuleID,
        R.RuleTypeID,
        A.SourceID,
        A.SourceUri,
		coalesce(A.Name, A.SourceID, 'Implementation ' + cast(A.ID as varchar)) as Name,
        A.CreatedOn,
        A.UpdatedOn
from	RuleImplementation A inner join [Rule] R on R.ID = A.RuleID
where    A.RuleID = @id", new { id });

                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.GetFullExceptionData());
            }
        }

        [Route("ruleimplementations/{implementationID:int}/qualifiers")]
        public IQueryable<dynamic> GetRuleimplementationQualifierTypes(int implementationID)
        {
            return Company.Query<dynamic>(@"select R.*, D.Name as ResolutionObjectName from RuleResultQualifierType R
                left join AssetType D on D.[Object] = R.ResolutionObject and D.ObjectID = R.ResolutionObjectID
                where R.RuleImplementationID = @implementationID
                order by R.[Order]", new { implementationID }).AsQueryable();
        }

        #endregion

        #region Comment Tag Suggestions

        [HttpGet, Route("tagsuggestions")]
        public IEnumerable<TagSuggestionModel> TagSuggestions(string phrase, string excludeObjects = "")
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return new List<TagSuggestionModel>();

            Dapper.DynamicParameters dbParams = new DynamicParameters();

            List<string> objectsToExclude = new List<string> { "FusionAttribute" };

            if (!string.IsNullOrEmpty(excludeObjects)) objectsToExclude.AddRange(excludeObjects.Split(','));
        
            var sql = @"select 
										c.[Object], 
										c.ObjectID, 
										AD.DisplayValue as TextPath, 
										cU.Url, 
										c.TypeName as ObjectTypeName, 
										c.ForeColor as IconForeColor, 
										c.BackColor as IconBackColor
										from [dbo].AssetWithType c   
										inner join  AssetDisplayValue as AD   on
										AD.AssetID = C.ID
										cross apply [dbo].getAssetUrlById(c.ID) cU                              
										where c.[Object] not in @exclude and (AD.DisplayValue like @beginsWith or (len(@val) > 2 and AD.DisplayValue like @contains))";

            dbParams.Add("beginsWith", $"{phrase}%");
            dbParams.Add("val", $"{phrase}%");
            dbParams.Add("contains", $"%{phrase}%");
            dbParams.Add("exclude", objectsToExclude);

            return Company.Query<TagSuggestionModel>(sql,dbParams);
        }

        #endregion

        #region Type/ID Endpoints

        [Route("asset/{id:long}")]
        public AssetDetail GetAssetDetail(long id)
        {
            return Company.GetAssetDetail(id);
        }

        [Route("{type}/{id:int}")]
        public ObjectDetail GetObjectDetail(SystemObjects type, int id)
        {
            return Company.GetObjectDetail(type.ToString(), id);
        }

        [Route("{type}/{id:int}/status")]
        public string GetObjectStatus(SystemObjects type, int id)
        {
            var objectDetail = Company.GetObjectDetail(type.ToString(), id);
            //check if there is a status field for this type
            var fieldType = Company.FieldTypes.Where(x => x.Object == objectDetail.Type && x.ObjectID == objectDetail.TypeID && string.Compare(x.FriendlyName, "Status", true) == 0).FirstOrDefault();

            if (fieldType == null) return null;

            var sql = "select FormattedValue from field where objecttype = @obj and objectid = @id and fieldtypeid = @fieldId";

            return Company.Query<string>(sql, new { obj = new DbString { Value = type.ToString(), IsFixedLength = true, Length = 20, IsAnsi = true }, id = id, fieldId = fieldType.ID }).FirstOrDefault();
        }

        /// <summary>
        /// Used mainly by the client-side search tool.
        /// </summary>
        /// <param name="type">The type of object</param>
        /// <param name="id">The ID of the object</param>
        /// <returns>A list of fields with the name, friendly name, and value.</returns>
        [Route("{type}/{id:int}/info")]
        public IQueryable<DisplayField> GetFieldForObject(SystemObjects type, int id)
        {
            var list = new List<DisplayField>();
            switch (type)
            {
                case SystemObjects.Artifact:
                    #region Fields
                    var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);
                    if (artifact != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = artifact.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = artifact.GetName(i => i.ArtifactTypeID), Name = "Type", Value = artifact.ArtifactType.Name });
                        loadDisplayFields(list, type, id);
                    }
                    artifact = null;
                    break;
                #endregion
                case SystemObjects.Attribute:
                    #region Fields
                    var attr = Company.GetById<core.entities.Attribute>(id);
                    if (attr != null)
                    {
                        loadDisplayFields(list, type, id);
                    }
                    attr = null;
                    break;
                #endregion
                case SystemObjects.FusionAttribute:
                    #region Fields
                    var fusionAttribute = Company.GetById<FusionAttribute>(id);
                    if (fusionAttribute != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = fusionAttribute.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = fusionAttribute.GetName(i => i.Name), Name = "Name", Value = fusionAttribute.Name });
                        list.Add(new DisplayField { FriendlyName = fusionAttribute.GetName(i => i.TextPath), Name = "TextPath", Value = fusionAttribute.TextPath });
                        loadDisplayFields(list, type, id);
                    }
                    fusionAttribute = null;
                    break;
                #endregion
                case SystemObjects.Policy:
                    #region Fields
                    var policy = Company.GetById<Policy>(id);
                    if (policy != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = policy.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = policy.GetName(i => i.TextPath), Name = "TextPath", Value = policy.TextPath });
                        loadDisplayFields(list, type, id);
                    }
                    policy = null;
                    break;
                #endregion
                case SystemObjects.Rule:
                    #region Fields
                    var rule = Company.GetById<Rule>(id, i => i.RuleType);
                    if (rule != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = rule.ID.ToString() });                        
                        list.Add(new DisplayField { FriendlyName = Resources.FieldInfo.RuleType_Name, Name = "RuleTypeID", Value = rule.RuleType.Name });
                        loadDisplayFields(list, type, id);
                    }
                    rule = null;
                    break;
                #endregion
                case SystemObjects.Resource:
                    #region Fields
                    var resource = Community.GetById<Resource>(id);
                    if (resource != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = resource.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = "Display Name", Name = "DisplayName", Value = resource.FormatDisplayName() });
                        list.Add(new DisplayField { FriendlyName = resource.GetName(i => i.Email), Name = "Email", Value = resource.Email });
                        loadDisplayFields(list, type, id);
                    }
                    resource = null;
                    break;
                #endregion
                case SystemObjects.Taxonomy:
                    #region Fields
                    var taxonomy = Company.Query<dynamic>(@"Select  A.ObjectId,AT.Name as TypeName,AP.TextPath ,ATL.Name as LevelName
		                                                    from Asset A
		                                                    inner join AssetType AT on
		                                                    AT.ID  =A.AssetTypeID
		                                                    inner join AssetTypeLevel ATL on
		                                                    ATL.AssetTypeID =AT.ID 
		                                                    cross apply [dbo].[GetAssetTextPathById](A.ID,'/') AP
		                                                    where A.object='Taxonomy' and A.objectId=@objectId", new { objectId = id }).SingleOrDefault();
                    if (taxonomy != null)
                    {

                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = taxonomy.ObjectId.ToString() });
                        list.Add(new DisplayField { FriendlyName = Fields.Type_Name, Name = "TaxonomyType", Value = taxonomy.TypeName });
                        list.Add(new DisplayField { FriendlyName = Fields.Path_Name, Name = "TextPath", Value = taxonomy.TextPath });
                        list.Add(new DisplayField { FriendlyName = "Level", Name = "Level", Value = taxonomy.LevelName });
                        loadDisplayFields(list, type, id);
                    }
                    taxonomy = null;
                    break;
                #endregion
                case SystemObjects.FusionAttributeType:
                    #region Fields

                    var fields = Company.Filter<FieldType>(x => x.Object == "FusionAttributeType" && x.ObjectID == id);

                    foreach (var field in fields)
                    {
                        list.Add(new DisplayField
                        {
                            FriendlyName = field.FriendlyName,
                            Name = field.Name,
                            Value = field.ID.ToString()
                        });
                    }

                    break;

                    #endregion
            }

            return list.AsQueryable();
        }

        [Route("{type}/{id:int}/style")]
        public ObjectStyle GetObjectStyle(SystemObjects type, int id)
        {
            return Company.GetObjectStyle(type, id);
        }

        [Route("{type}/{id:int}/detail")]
        public DetailReadOnlyModel GetObjectDetailFields(SystemObjects type, int id)
        {
            var model = new DetailReadOnlyModel() { columns = 2 };
                        
            int row = 0;
            switch (type)
            {
                case SystemObjects.Artifact:
                    #region Fields

                    var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);
                    var parent = Company.GetParentObject(id, SystemObjects.Artifact);                    
                    if (artifact != null)
                    {
                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                        if (parent != null)
                        {
                            var parentAsset = Company.GetAssetDetail("Artifact", parent.ObjectID);
                            var parentUrl = Company.Query<string>($"select dbo.GenerateAssetUrl({parentAsset.ID})").First();

                            model.rows.Insert(1,new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.Parent_Name , FieldName = "ArtifactParentName", FieldDescription = Resources.FieldInfo.Parent_Description, Value = parentAsset.DisplayValue, TooltipUrl = parentUrl, TooltipType="Artifact", TooltipContext="Preview", TooltipID = parent.ObjectID}
                                }
                            });
                        }

                       

                        var asset = Company.Assets.Where(x => x.Object == "Artifact" && x.ObjectID == id).FirstOrDefault();

                        if (asset != null) {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = asset.ID.ToString(), DataType = "string" }
                            },
                                SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = asset.uid.ToString(), DataType = "string" }
                            },
                            });

                        }

                        if (artifact.UpdatedOn.HasValue)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "ArtifactCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = artifact.CreatedOn.HasValue ? artifact.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "", DataType = "date" }
                                },
                                SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.UpdatedOn_Name, FieldName = "ArtifactUpdatedOn", FieldDescription = Resources.FieldInfo.UpdatedOn_Description, Value = artifact.UpdatedOn.GetValueOrDefault().ToString("yyyy-MM-ddTHH:mm:ssZ"), DataType = "date" }
                                }
                            });
                        }
                        else
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "ArtifactCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = artifact.CreatedOn.HasValue ? artifact.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "", DataType = "date" }
                                }
                            });
                        }
                    }
                    artifact = null;
                    break;
                #endregion
                case SystemObjects.ArtifactType:
                    #region Fields
                    var artifactType = Company.GetById<ArtifactType>(id);
                    if (artifactType != null)
                    {

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = artifactType.GetName(i => i.Name), FieldName = "ArtifactTypeName", FieldDescription = artifactType.GetDescription(i => i.Name), Value = artifactType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = artifactType.GetName(i => i.ID), FieldName = "ArtifactTypeID", FieldDescription = artifactType.GetDescription(i => i.ID), Value = artifactType.ID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = artifactType.GetName(i => i.Description), FieldName = "ArtifactTypeDescription", FieldDescription = artifactType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(artifactType.Description) ? "None provided" : artifactType.Description }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = artifactType.GetName(i => i.CanOwnFusion), FieldName = "ArtifactTypeCanOwnFusion", FieldDescription = artifactType.GetDescription(i => i.CanOwnFusion), Value = artifactType.CanOwnFusion.FormatBooleanReadOnlyValue() }
                            }
                        });

                    }
                    artifactType = null;
                    break;
                #endregion                
                case SystemObjects.Attribute:
                    #region Fields
                    var attr = Company.GetById<core.entities.Attribute>(id);
                    if (attr != null)
                    {
                        model.columns = 1;

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                    }
                    attr = null;
                    break;
                #endregion
                case SystemObjects.AttributeType:
                    #region Fields
                    var attributeType = Company.GetById<AttributeType>(id);
                    if (attributeType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = attributeType.GetName(i => i.ID), FieldName = "AttributeTypeID", FieldDescription = attributeType.GetDescription(i => i.ID), Value = attributeType.ID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = attributeType.GetName(i => i.Name), FieldName = "AttributeTypeName", FieldDescription = attributeType.GetDescription(i => i.Name), Value = attributeType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = attributeType.GetName(i => i.DisplayFormat), FieldName = "AttributeTypeDisplayFormat", FieldDescription = attributeType.GetDescription(i => i.DisplayFormat), Value = attributeType.DisplayFormat }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = attributeType.GetName(i => i.Description), FieldName = "AttributeTypeDescription", FieldDescription = attributeType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(attributeType.Description) ? "None provided" : attributeType.Description }
                            }
                        });
                    }
                    attributeType = null;
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

                        if (group.PrimaryOwnerResourceID.HasValue && group.SecondaryOwnerResourceID.HasValue)
                        {
                            var groupOwnerIDs = new List<int>();
                            if (group.PrimaryOwnerResourceID.HasValue) groupOwnerIDs.Add(group.PrimaryOwnerResourceID.Value);
                            if (group.SecondaryOwnerResourceID.HasValue) groupOwnerIDs.Add(group.SecondaryOwnerResourceID.Value);

                            var groupOwners = GetCompanyResources().Where(i => groupOwnerIDs.Contains(i.ID)).ToList();

                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = group.GetName(i => i.PrimaryOwnerResourceID), FieldName = "GroupOwner", FieldDescription = group.GetDescription(i => i.PrimaryOwnerResourceID), Value = groupOwners.Single(i => i.ID == group.PrimaryOwnerResourceID.Value).FormatDisplayName() }
                                },
                                SecondColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = group.GetName(i => i.SecondaryOwnerResourceID), FieldName = "GroupOwner", FieldDescription = group.GetDescription(i => i.SecondaryOwnerResourceID), Value = groupOwners.Single(i => i.ID == group.SecondaryOwnerResourceID.Value).FormatDisplayName() }
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
                                    new ReadOnlyField { Name = group.GetName(i => i.Description), FieldName = "GroupDescription", FieldDescription = group.GetDescription(i => i.Description), Value = group.Description }
                                }
                            });
                        }
                    }
                    group = null;
                    break;
                #endregion
                case SystemObjects.ExportTemplate:
                    #region Fields
                    var template = Company.GetById<AssetTypeExportTemplate>(id);
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
                                new ReadOnlyField{ Name = "Include Glossary Url", FieldName = "IncludeUrl", Value = template.IncludeUrl ? "Yes" : "No"}                                
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
                                new ReadOnlyField { Row = 7, Column = 1, Name = fieldType.GetName(i => i.DisplayDescription), FieldName = "FieldTypeDisplayDescription", FieldDescription = fieldType.GetDescription(i => i.DisplayDescription), Value = fieldType.DisplayDescription }
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
                                new ReadOnlyField { Row = 7, Column = 1, Name = fieldType.GetName(i => i.FormDescription), FieldName = "FieldTypeFormDescription", FieldDescription = fieldType.GetDescription(i => i.FormDescription), Value = fieldType.FormDescription }
                            }
                            });
                        }
                    }
                    fieldType = null;
                    break;
                #endregion
                case SystemObjects.Fusion:
                    #region Fields
                    var fusion = Company.GetById<Fusion>(id);

                    if (fusion != null)
                    {
                        var fusionFields = Company.GetFieldRelationsByObject(SystemObjects.Fusion, id);
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusion.GetName(i => i.ID), FieldName = "FusionID", FieldDescription = fusion.GetDescription(i => i.ID), Value = fusion.ID.ToString() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusion.GetName(i => i.Name), FieldName = "FusionName", FieldDescription = fusion.GetDescription(i => i.Name), Value = fusion.Name }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusion.GetName(i => i.Description), FieldName = "FusionDescription", FieldDescription = fusion.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(fusion.Description) ? "None provided" : fusion.Description }
                            }
                        });

                        row = 3;
                        foreach (var k in fusionFields)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = k.FriendlyName, FieldName = "Fusion" + k.Name, FieldDescription = k.DisplayDescription, Value = k.FormattedValue }
                                }
                            });
                            row++;
                        }
                    }

                    fusion = null;
                    break;
                #endregion
                case SystemObjects.FusionAttribute:
                    #region Fields
                    var fusionAttribute = Company.GetById<FusionAttribute>(id);
                    var fusionAttDetail = Company.GetObjectDetail("FusionAttribute", id);
                    if (fusionAttribute != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionAttribute.GetName(i => i.Name), FieldName = "FAName", FieldDescription = fusionAttribute.GetDescription(i => i.Name), Value = fusionAttribute.Name }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = fusionAttDetail.UID.ToString()  }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionAttribute.GetName(i => i.TextPath), FieldName = "FATextPath", FieldDescription = fusionAttribute.GetDescription(i => i.TextPath), Value = fusionAttribute.TextPath }
                            }
                        });

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                        model.rows.AddRange(loadDisplayableRelationshipsAsFields(type, id));

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = fusionAttribute.GetName(i => i.ID), FieldName = "FAID", FieldDescription = fusionAttribute.GetDescription(i => i.ID), Value = $"{fusionAttribute.ID}" }
                                }
                        });
                    }
                    fusionAttribute = null;
                    break;
                #endregion
                case SystemObjects.FusionAttributeType:
                    #region Fields
                    var fusionAttributeType = Company.GetById<FusionAttributeType>(id, i => i.FusionType);
                    if (fusionAttributeType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionAttributeType.GetName(i => i.Name), FieldName = "FATName", FieldDescription = fusionAttributeType.GetDescription(i => i.Name), Value = fusionAttributeType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionAttributeType.GetName(i => i.ID), FieldName = "FATID", FieldDescription = fusionAttributeType.GetDescription(i => i.ID), Value = fusionAttributeType.ID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionAttributeType.GetName(i => i.FusionType), FieldName = "FATFusionType", FieldDescription = fusionAttributeType.GetDescription(i => i.FusionType), Value = fusionAttributeType.FusionType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionAttributeType.GetName(i => i.TextPath), FieldName = "FATTextPath", FieldDescription = fusionAttributeType.GetDescription(i => i.TextPath), Value = fusionAttributeType.TextPath }
                            }
                        });
                    }
                    fusionAttributeType = null;
                    break;
                #endregion
                case SystemObjects.FusionQueryAttribute:
                    #region Fields
                    var fusionQueryAttribute = Company.GetById<FusionQueryAttribute>(id);
                    if (fusionQueryAttribute != null)
                    {
                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                        model.rows.AddRange(loadDisplayableRelationshipsAsFields(type, id));
                    }
                    fusionQueryAttribute = null;
                    break;
                #endregion
                case SystemObjects.FusionExecution:
                    #region Fields
                    var fusionExecution = Company.GetById<FusionExecution>(id);
                    if (fusionExecution != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Date Started", FieldName = "DateStarted", Value = fusionExecution.DateStarted.HasValue ? JsonConvert.SerializeObject(fusionExecution.DateStarted.Value) : "Not started" }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Date Completed", FieldName = "DateCompleted", Value = fusionExecution.DateCompleted.HasValue ? JsonConvert.SerializeObject(fusionExecution.DateCompleted.Value) : "Not completed" }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "# Added", FieldName = "Adds", Value = fusionExecution.Adds.HasValue ? fusionExecution.Adds.Value.ToString() : ""}
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "# Updated", FieldName = "Updates", Value = fusionExecution.Updates.HasValue ? fusionExecution.Updates.Value.ToString() : "" }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "# Deleted", FieldName = "Deletes", Value = fusionExecution.Deletes.HasValue ? fusionExecution.Deletes.Value.ToString() : "" }
                            }
                        });
                    }
                    fusionExecution = null;
                    break;
                #endregion
                case SystemObjects.FusionType:
                    #region Fields
                    var fusionType = Company.GetById<FusionType>(id);
                    var fusionDetail = Company.GetObjectDetail("FusionType", id);
                    if (fusionType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionType.GetName(i => i.Name), FieldName = "FusionTypeName", FieldDescription = fusionType.GetDescription(i => i.Name), Value = fusionType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionType.GetName(i => i.ID), FieldName = "FusionTypeID", FieldDescription = fusionType.GetDescription(i => i.ID), Value = fusionType.ID.ToString() }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = fusionDetail.UID.ToString()  }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionType.GetName(i => i.Description), FieldName = "FusionTypeDescription", FieldDescription = fusionType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(fusionType.Description) ? "None provided" : fusionType.Description }
                            }
                        });
                    }
                    fusionType = null;
                    break;
                #endregion
                case SystemObjects.Intersect:
                    #region Fields                    
                    var intersect = Company.GetById<Intersect>(id);
                    if (intersect != null)
                    {
                        model.columns = 1;
                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
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

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = intersectType.GetName(i => i.Name), FieldName = "IntersectTypeName", FieldDescription = intersectType.GetDescription(i => i.Name), Value = intersectType.Name }
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
                    }
                    load = null;
                    break;
                #endregion
                case SystemObjects.LookupType:
                    #region Fields
                    var lookupType = Company.GetById<LookupType>(id);
                    if (lookupType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = lookupType.GetName(i => i.Name), FieldName = "LookupTypeName", FieldDescription = lookupType.GetDescription(i => i.Name), Value = lookupType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = lookupType.GetName(i => i.ID), FieldName = "LookupTypeID", FieldDescription = lookupType.GetDescription(i => i.ID), Value = lookupType.ID.ToString() }
                            }
                        });
                    }
                    lookupType = null;
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
                                }
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name =Fields.Path_Name, FieldName = "PolicyTextPath", FieldDescription =Fields.Path_Description, Value = policy.TextPath }
                            }
                        });


                        model.rows.AddRange(loadDynamicDisplayFields(type, id));

                        var asset = Company.Assets.Where(x => x.Object == "Policy" && x.ObjectID == id).FirstOrDefault();

                        if (asset != null)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = asset.ID.ToString(), DataType = "string" }
                            },
                                SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = asset.uid.ToString(), DataType = "string" }
                            }
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = "ID", FieldName = "PolicyID", FieldDescription = Fields.Type_Description, Value = $"{policy.ObjectID}" }
                                }
                        });
                    }
                    policy = null;
                    break;
                #endregion
                case SystemObjects.Rule:
                    #region Fields

                    var rule = Company.GetById<Rule>(id, i => i.Dimension, i => i.RuleType);
                    if (rule != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.RuleType_Name, FieldName = "RuleRuleType", FieldDescription = Resources.FieldInfo.RuleType_Description, Value = rule.RuleType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.RuleDimension_Name, FieldName = "RuleDimension", FieldDescription = Resources.FieldInfo.RuleDimension_Description, Value = (rule.RuleDimensionID.HasValue ? rule.Dimension.Name:""), TooltipContext = "Preview", TooltipID = rule.RuleDimensionID.GetValueOrDefault(), TooltipType = "RuleDimension" }
                            }    
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.RuleThreshold_Name, FieldName = "RuleThreshold", FieldDescription = Resources.FieldInfo.RuleThreshold_Description, Value = rule.Threshold.ToString() }
                            }
                        });

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));

                        var asset = Company.Assets.Where(x => x.Object == "Rule" && x.ObjectID == id).FirstOrDefault();

                        if (asset != null)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = asset.ID.ToString(), DataType = "string" }
                            },
                                SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = asset.uid.ToString(), DataType = "string" }
                            }
                            });
                        }

                        if (rule.UpdatedOn.HasValue)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "RuleCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = rule.CreatedOn.Value.ToString("o"), DataType = "date" }
                                },
                                SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.UpdatedOn_Name, FieldName = "RuleUpdatedOn", FieldDescription = Resources.FieldInfo.UpdatedOn_Description, Value = rule.UpdatedOn.GetValueOrDefault().ToString("o"), DataType = "date" }
                                }
                            });
                        }
                        else
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "RuleCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = rule.CreatedOn.Value.ToString("o"), DataType = "date" }
                                }
                            });
                        }
                        
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = rule.GetName(i => i.ID), FieldName = "RuleID", FieldDescription = rule.GetDescription(i => i.ID), Value = $"{rule.ID}" }
                                },
                            SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.RuleStatus_Name, FieldName = "Status", FieldDescription = Resources.FieldInfo.RuleStatus_Description, Value = rule.Status.GetDisplayName() }
                                }
                        });
                    }
                    rule = null;
                    break;
                #endregion
                case SystemObjects.RuleImplementation:
                    #region Fields

                    var impl = Company.GetById<RuleImplementation>(id, i => i.Rule.RuleType, i => i.RuleResultQualifierTypes, i => i.Rule.Dimension);
                    if (impl != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.RuleImplementation_Name, FieldName = "RuleImplementation_Name", Value = $"<b>{impl.Name ?? "Implementation " + impl.ID}</b>" }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.RuleImplementation_SourceID, FieldName = "RuleImplementation_SourceID", Value = impl.SourceID }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.RuleImplementation_ResultsEndpoint, FieldName = "RuleImplementation_ResultsEndpoint", Value = $"POST to <a href='/swagger/ui/index#!/Events/Events_AddRuleImplementationResults' target='api'>/services/events/rules/{impl.RuleID}/{impl.ID}/results</a>" }
                            }
                        });

                        if (impl.UpdatedOn.HasValue)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "RuleCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = impl.CreatedOn.Value.ToShortDateString() }
                                },
                                SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.UpdatedOn_Name, FieldName = "RuleUpdatedOn", FieldDescription = Resources.FieldInfo.UpdatedOn_Description, Value = impl.UpdatedOn.GetValueOrDefault().ToShortDateString() }
                                }
                            });
                        }
                        else
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.CreatedOn_Name, FieldName = "RuleCreatedOn", FieldDescription = Resources.FieldInfo.CreatedOn_Description, Value = impl.CreatedOn.Value.ToShortDateString() }
                                }
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = impl.GetName(i => i.ID), FieldName = "RuleImplementationID", FieldDescription = impl.GetDescription(i => i.ID), Value = $"{impl.ID}" }
                                }
                        });
                    }
                    rule = null;
                    break;
                #endregion
                case SystemObjects.RuleType:
                    #region Fields
                    var ruleType = Company.GetById<RuleType>(id);
                    var ruleDetail = Company.GetObjectDetail("RuleType", id);
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
                                new ReadOnlyField { Name = Resources.FieldInfo.ID_Name, FieldName = "RuleTypeID", Value = ruleType.ID.ToString() }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = ruleDetail.UID.ToString()  }
                            }
                        });
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.Description_Name, FieldName = "RuleTypeDescription", Value = string.IsNullOrEmpty(ruleType.Description) ? "None provided" : ruleType.Description }
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
                                new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = responsibilityDetail.UID.ToString()  }
                            }
                        });
                        if (!string.IsNullOrEmpty(responsibilityType.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = responsibilityType.GetName(i => i.Description), FieldName = "Description", FieldDescription = responsibilityType.GetDescription(i => i.Description), Value = responsibilityType.Description }
                                }
                            });
                        }
                    }
                    responsibilityType = null;
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region Fields
                    var policyType = Company.GetById<PolicyType>(id);
                    var objectDetail = Company.GetObjectDetail("PolicyType", id);
                    if (policyType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = policyType.GetName(i => i.Name), FieldName = "PolicyTypeName", FieldDescription = policyType.GetDescription(i => i.Name), Value = policyType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = policyType.GetName(i => i.ID), FieldName = "PolicyTypeID", FieldDescription = policyType.GetDescription(i => i.ID), Value = policyType.ID.ToString() }
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
                                new ReadOnlyField { Name = policyType.GetName(i => i.Description), FieldName = "PolicyTypeDescription", FieldDescription = policyType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(policyType.Description) ? "None provided" : policyType.Description }
                            }
                        });
                    }
                    policyType = null;
                    break;
                #endregion
                case SystemObjects.ReferenceItemType:
                    #region Fields
                    var refType = Company.GetById<ReferenceItemType>(id);
                    if (refType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = refType.GetName(i => i.Name), FieldName = "Name", FieldDescription = refType.GetDescription(i => i.Name), Value = refType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = refType.GetName(i => i.DisplayFormat), FieldName = "DisplayFormat", FieldDescription = refType.GetDescription(i => i.DisplayFormat), Value = refType.DisplayFormat }
                            }
                        });

                        if (!string.IsNullOrEmpty(refType.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = refType.GetName(i => i.Description), FieldName = "Description", FieldDescription = refType.GetDescription(i => i.Description), Value = refType.Description }
                                }
                            });
                        }

                        if (!string.IsNullOrEmpty(refType.SourceNotes))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = refType.GetName(i => i.SourceNotes), FieldName = "SourceNotes", FieldDescription = refType.GetDescription(i => i.SourceNotes), Value = refType.SourceNotes }
                                }
                            });
                        }

                        var assetType = Company.AssetTypes.Where(x => x.Object == "ReferenceItemType" && x.ObjectID == id).FirstOrDefault();

                        if (assetType != null)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 2,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField{ Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = assetType.uid.ToString()  }
                                },
                                SecondColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = "Asset Type ID", FieldName = "AssetTypeId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = assetType.ID.ToString(), DataType = "string" }
                                }
                            });
                        }

                        var parentRefType = Company.GetParentType(refType.ID, SystemObjects.ReferenceItemType);

                        var heirarchyColumns = new DetailReadOnlyRowModel
                        {
                            columns = (parentRefType != null) ? 2:1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Hierarchical", FieldName = "Hierarchical", FieldDescription = "Is this reference list a hierarchical reference list", Value = parentRefType != null ? "Yes":"No" }
                            }                            
                        };

                        if(parentRefType != null)
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
                    var report = Company.GetById<Report>(id, i => i.ReportLayout, i => i.Responsibilities );
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
                                    new ReadOnlyField { Name = report.GetName(i => i.Description), FieldName = "ReportDescription", FieldDescription = report.GetDescription(i => i.Description), Value = report.Description }
                                }
                            });
                        }

                        if(!string.IsNullOrEmpty(report.FileName))
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

                        if(report.Responsibilities != null && report.Responsibilities.Count > 0)
                        {
                            var responsibilityIds = report.Responsibilities.Select(x => x.ResponsibilityTypeID).ToList();
                            var responsibilities = Company.ResponsibilityTypes.Where(x => responsibilityIds.Contains(x.ID));

                            if (responsibilities != null)
                            {
                                var val = "";
                                foreach(var responsibility in responsibilities)
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
                        

                        if (report.ReportLayout != null)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = report.GetName(i => i.ReportLayout), FieldName = "ReportReportLayout", FieldDescription = report.GetDescription(i => i.ReportLayout), Value = report.ReportLayout.Name }
                            }
                            });
                        }

                        var sql = "";
                        
                        switch (report.ObjectType)
                        {
                            case "Artifact":
                                sql = "select 'Artifact Instance : ' + Name from ArtifactType where ID = @id";
                                break;
                            case "ArtifactType":
                                sql = "select 'Artifact Type : ' + Name from ArtifactType where ID = @id";
                                break;
                            case "Resource":
                                sql = "select 'Resource Instance'";
                                break;
                            case "Policy":
                                sql = "select 'Policy Instance : ' + Name from PolicyType where ID = @id";
                                break;
                            case "PolicyType":
                                sql = "select 'Policy Type : ' + Name from PolicyType where ID = @id";
                                break;
                            case "Rule":
                                sql = "select 'Rule Instance : ' + Name from RuleType where ID = @id";
                                break;
                            case "RuleType":
                                sql = "select 'Rule Type : ' + Name from RuleType where ID = @id";
                                break;
                            case "Taxonomy":
                                sql = "select 'Model Instance : ' + Name from TaxonomyType where ID = @id";
                                break;
                            case "TaxonomyType":
                                sql = "select 'Model Type : ' + Name from TaxonomyType where ID = @id";
                                break;
                            case "FusionType":
                                sql = "select 'Fusion Type : ' + Name from FusionType where ID = @id";
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
                                new ReadOnlyField { Name = resource.GetName(i => i.Email), FieldName = "ResourceEmail", FieldDescription = resource.GetDescription(i => i.Email), Value = resource.Email }
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

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                    }
                    resource = null;
                    break;
                #endregion
                case SystemObjects.ResourceType:
                    #region Fields
                    var resourceType = Community.GetById<ResourceType>(id);
                    if (resourceType != null)
                    {
                        model.columns = 1;

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = resourceType.GetName(i => i.Name), FieldName = "ResourceTypeName", FieldDescription = resourceType.GetDescription(i => i.Name), Value = resourceType.Name }
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

                        var dtlSurveyType = Company.GetObjectDetail(surveyType.Object, surveyType.ObjectID);
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Object Type", FieldName = "SurveyTypeObjectType", FieldDescription = surveyType.GetDescription(i => i.Object), Value = surveyType.Object.ToString() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Object", FieldName = "SurveyTypeObjectID", FieldDescription = surveyType.GetDescription(i => i.ObjectID), Value = (dtlSurveyType != null) ? dtlSurveyType.Name : surveyType.ObjectID.ToString() }
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
                            }
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
                                }
                            });
                        }

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 3,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = Resources.FieldInfo.AssetId_Name, FieldName = "AssetId", FieldDescription = Resources.FieldInfo.AssetId_Description, Value = $"{taxonomy.AssetID}", DataType = "string" }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = Resources.FieldInfo.UID_Name, FieldName = "uid", FieldDescription = Resources.FieldInfo.UID_Description, Value = taxonomy.UID.ToString(), DataType = "string" }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = "ID", FieldName = "TaxonomyID", Value = $"{taxonomy.ID}" }
                            },
                        });
                    }
                    taxonomy = null;
                    break;
                #endregion
                case SystemObjects.TaxonomyType:
                    #region Fields
                    var taxonomyType = Company.GetById<TaxonomyType>(id);
                    var taxonomyObjectDetail = Company.GetObjectDetail("TaxonomyType", id);
                    if (taxonomyType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = taxonomyType.GetName(i => i.Name), FieldName = "TaxonomyTypeName", FieldDescription = taxonomyType.GetDescription(i => i.Name), Value = taxonomyType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = taxonomyType.GetName(i => i.ID), FieldName = "TaxonomyTypeID", FieldDescription = taxonomyType.GetDescription(i => i.ID), Value = taxonomyType.ID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = taxonomyType.GetName(i => i.MaximumDepth), FieldName = "TaxonomyTypeMaximumDepth", FieldDescription = taxonomyType.GetDescription(i => i.MaximumDepth), Value = taxonomyType.MaximumDepth.ToString() }
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
                                new ReadOnlyField { Name = taxonomyType.GetName(i => i.Description), FieldName = "TaxonomyTypeDescription", FieldDescription = taxonomyType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(taxonomyType.Description) ? "None provided" : taxonomyType.Description }
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
            
            return model;
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

        [Route("fusion/statistics")]
        public FusionStatisticTileModel GetFusionStatistics(int daysToLookBack)
        {
            if (daysToLookBack <= 0) daysToLookBack = 5000;
            return Company.Query<FusionStatisticTileModel>(QueryConstants.FusionStatisticsItem, new { days = (daysToLookBack * -1) }).FirstOrDefault();
        }

        [Route("{type}/{id:int}/fieldlookup")]
        public List<dynamic> GetEditableFieldLookupData(SystemObjects type, int id, int take = 10000)
        {
            var list = new List<dynamic>();
            string prefix = "";
            var qs = Request.GetQueryNameValuePairs();
            if (qs.Any(i => i.Key == "prefix"))
            {
                prefix = qs.Single(i => i.Key == "prefix").Value;
            }

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    var atItems = Company.Table<ArtifactType>();
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        atItems = atItems.Where(i => i.Name.StartsWith(prefix));
                    }
                    else
                    {
                        atItems = atItems.Take(take);
                    }
                    foreach (var item in atItems)
                    {
                        var ei = new
                        {
                            ID = item.ID,
                            Name = item.Name
                        };

                        list.Add(ei);
                    }
                    break;
                case SystemObjects.Artifact:                    
                    var sql = "select a.ID,d.DisplayValue  from artifact a inner join asset ast on (a.id = ast.objectid and ast.[object] = 'Artifact') cross apply [dbo].GetAssetDisplayValueById(ast.id) d where a.ArtifactTypeID = @id order by d.DisplayValue";
                    var aItems = Company.Query<dynamic>(sql, new { id = id });

                    if (!string.IsNullOrEmpty(prefix))
                    {
                        aItems = aItems.Where(i => i.DisplayValue.StartsWith(prefix));
                    }
                    else
                    {
                        aItems = aItems.Take(take);
                    }
                    foreach (var item in aItems)
                    {
                        var ei = new
                        {
                            ID = item.ID,
                            Name = item.DisplayValue
                        };

                        list.Add(ei);
                    }
                    break;
                case SystemObjects.ReferenceItemType:
                case SystemObjects.ReferenceItem:
                    var dItems = Company.Table<ReferenceItemType>();
                    if (!string.IsNullOrEmpty(prefix)) dItems = dItems.Where(i => i.Name.StartsWith(prefix));
                    dItems = dItems.Take(take);
                    foreach (var item in dItems)
                    {
                        var ei = new
                        {
                            ID = item.ID,
                            Name = item.Name
                        };
                        list.Add(ei);
                    }
                    break;
                case SystemObjects.Taxonomy:
                    var imItems = Company.Table<Taxonomy>();
                    if (!string.IsNullOrEmpty(prefix)) imItems = imItems.Where(i => i.TextPath.Contains(prefix));
                    imItems = imItems.OrderBy(i => i.TextPath).Take(take);
                    foreach (var item in imItems)
                    {
                        var ei = new
                        {
                            ID = item.ID,
                            Name = item.TextPath
                        };
                        list.Add(ei);
                    }
                    break;
                case SystemObjects.TaxonomyType:
                    var imtItems = Company.Filter<Taxonomy>(i => i.TaxonomyTypeID == id);
                    if (!string.IsNullOrEmpty(prefix)) imtItems = imtItems.Where(i => i.TextPath.Contains(prefix));
                    imtItems = imtItems.OrderBy(i => i.TextPath).Take(take);
                    foreach (var item in imtItems)
                    {
                        var ei = new
                        {
                            ID = item.ID,
                            Name = item.TextPath
                        };
                        list.Add(ei);
                    }
                    break;
            }
            return list;            
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

        [Route("{assetID:int}/ownership")]
        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByObject(long assetID)
        {
            var asset = Company.GetAssetDetail(assetID);
            if (asset == null)
                return null;
            return Company.Filter<ResponsibilityDetail>(i => ( (i.AssetID == assetID) || (i.AssetID == 0 && i.AssetTypeID == asset.AssetTypeID) ) && i.IsVisible);
        }

        [Route("{id:int}/permissionsbyid")]
        public List<PermissionInfo> GetPermissionsObject(int id)
        {
            var isAdmin = Company.CurrentResourceIsAdmin;

            AssetDetail asset = null;
            if (!isAdmin)
            {
                asset = Company.GetAssetDetail(id);
                if (asset == null) throw new NotFoundException("Asset Not found");
            }

            return GetPermissionsByObject(asset, isAdmin);
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
                    if (asset == null) throw new NotFoundException("Asset Not found");
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
            if(obj == SystemObjects.FusionAttribute)
                return Company.Query<dynamic>(QueryConstants.FusionAttributeRelationshipAllCountsWithZero, new { objid });
            else if(obj == SystemObjects.FusionQueryAttribute)
                return Company.Query<dynamic>(QueryConstants.FusionQueryAttributeRelationshipAllCountsWithZero, new { objid });
            else if(obj == SystemObjects.ReferenceItemType)
                return Company.Query<dynamic>(QueryConstants.ReferenceListTypeRelationshipsAllCountsWithZero, new { obj = new Dapper.DbString { IsAnsi = true, Value = obj.ToString(), IsFixedLength = true, Length = 50 }, objid});
            return Company.Query<dynamic>(QueryConstants.ObjectRelationshipAllCountsWithZero, new { obj = new Dapper.DbString { IsAnsi = true, Value = obj.ToString(), IsFixedLength = true, Length = 50 }, objid });
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

        [Route("{type}/{id:int}/relationships/{targetType}/{targetID:int}/{intersectTypeID:int}"), HttpGet]
        public IEnumerable<dynamic> RelationshipsForObjectByTargetType(SystemObjects type, int id, SystemObjects targetType, int targetID, int intersectTypeID, bool includeInverse = true)
        {            
            var joins = "";
            var columns = "";
            
            getDynamicFieldJoinStatements(intersectTypeID, "Intersect", out joins, out columns, true, false);

            var sourceJoins = "";
            var sourceColumns = "";

            getDynamicFieldJoinStatements(targetID, targetType.ToString().Replace("Type", ""), out sourceJoins, out sourceColumns, false, false, false, true, "ObjectID", "A.Name");

            joins = joins + sourceJoins;
            columns = columns + sourceColumns;

            var attributesTypes = Company.Filter<AttributeTypeRelation>(i => i.ObjectType == "IntersectType" && i.ObjectID == intersectTypeID && !i.AllowMultipleEntries).ToList();
            foreach (var f in attributesTypes)
            {
                var name = $"AttributeType{f.AttributeType.ID}";
                columns += $"{name}_T.FormattedValue as [{name}], ";
                joins += $" left join AttributeDetail {name}_T on {name}_T.ObjectType = 'Intersect' and {name}_T.ObjectID = A.ID and {name}_T.AttributeTypeID = {f.AttributeTypeID}";
            }

            var intersectType = Company.IntersectTypes.Where(x => x.ID == intersectTypeID).FirstOrDefault();

            if (intersectType == null) throw new NotFoundException("intersect type id");

            var isTargetObject = intersectType.Object == targetType.ToString() && intersectType.ObjectID == targetID;
            var isTargetSubjectSame = intersectType.Object == intersectType.Subject && intersectType.ObjectID == intersectType.SubjectID;
            var isTargetReferenceItemType = targetType.ToString() == "ReferenceItemType" && targetID == 0;
            var isTargetFusion = targetType.ToString().StartsWith("Fusion");

            var innerSql = "";
            var assetJoin = "";


            if (isTargetSubjectSame)
            {
                if (isTargetReferenceItemType)
                {
                    assetJoin = $@"inner join (select 'Reference List' as Name) AST on 1 = 1
		                        inner join AssetType IA on	IA.Object = case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end
								    and IA.ObjectID = case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end
                                inner join (select ID, Name  as TextPath from AssetType) P on P.ID = IA.ID";
                }
                else
                {
                    assetJoin = $@"inner join AssetType AST on AST.Object = case when I.Subject = @type and I.SubjectID = @id then IT.Object else IT.Subject end
							        and AST.ObjectID = case when I.Subject = @type and I.SubjectID = @id then IT.ObjectID else IT.SubjectID end
		                        inner join Asset IA on	IA.Object = case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end
								    and IA.ObjectID = case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end
                                cross apply dbo.GetAssetTextPathById(IA.ID, '{(isTargetFusion ? '.' : '/')}') P";
                }

                innerSql = $@"select	I.ID,
                                IntersectTypeID,
                                case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end as Object,
		                        case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end as ObjectID,
		                        P.TextPath as Name,
                                --case when I.Subject = @type and I.SubjectID = @id then ObjectUrl else SubjectUrl end as Url,
		                        case when I.Subject = @type and I.SubjectID = @id then IT.Object else IT.Subject end as Type,
		                        case when I.Subject = @type and I.SubjectID = @id then IT.ObjectID else IT.SubjectID end as TypeID,
		                        AST.Name as TypeName,
		                        T.HasTechnicalRelationships
                        from	[Intersect] I
                                inner join IntersectType IT on IT.ID = I.IntersectTypeID
		                        {assetJoin}
		                        cross apply (
					                        select	case 
								                        when count(1) > 0 then cast(1 as bit)
								                        else cast(0 as bit)
							                        end as HasTechnicalRelationships
					                        from	[Intersect]
					                        where	Subject = 'Intersect' and SubjectID = I.ID) T
                        where	(
                                (I.Subject = @type  and I.SubjectID = @id) or
                                (I.Object = @type and I.ObjectID = @id)
                                )
                                and IntersectTypeID = {intersectTypeID} ";
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
                    assetJoin = $@"inner join AssetType AST on AST.Object = (case when I.Subject = @type and I.SubjectID = @id then IT.Object else IT.Subject end)
									                        and AST.ObjectID = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then IT.ObjectID else IT.SubjectID end)" : "IT.ObjectID")}
		                        inner join Asset IA on	IA.Object = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then I.Object else I.Subject end)" : "I.Object")}
								                        and IA.ObjectID = {(includeInverse ? "(case when I.Subject = @type and I.SubjectID = @id then I.ObjectID else I.SubjectID end)" : "I.ObjectID")}
		                        cross apply dbo.GetAssetTextPathById(IA.ID, '{(isTargetFusion ? '.' : '/')}') P";
                }

                innerSql = $@"select	I.ID,
                        IntersectTypeID,
                        case when I.Subject = @type and I.SubjectID = @id then
                            I.Object
                        else
                            I.Subject
                        end as Object,
		                case when I.Subject = @type and I.SubjectID = @id then
                            I.ObjectID
                        else
                            I.SubjectID
                        end as ObjectID,
		                P.TextPath as Name,        
		                IT.Object Type,
		                IT.ObjectID TypeID,
		                AST.Name as TypeName,
		                T.HasTechnicalRelationships
                from	[Intersect] I
		                inner join IntersectType IT on IT.ID = I.IntersectTypeID
		                {assetJoin}
		                cross apply (
					                select	case 
								                when count(1) > 0 then cast(1 as bit)
								                else cast(0 as bit)
							                end as HasTechnicalRelationships
					                from	[Intersect]
					                where	Subject = 'Intersect' and SubjectID = I.ID
					                ) T
                where	((I.Subject = @type  and I.SubjectID = @id) {(includeInverse ? " or (I.Object = @type  and I.ObjectID = @id) " : "")})        
                        and IntersectTypeID = {intersectTypeID} ";
            }
            else
            {
                if (isTargetReferenceItemType)
                {
                    assetJoin = $@"inner join (select 'Reference List' as Name) AST on 1 = 1
                                inner join AssetType IA on IA.Object = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.Subject else I.Object end)" : "I.Subject")}
                                                        and IA.ObjectID = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.SubjectID else I.ObjectID end)" : "I.SubjectID")}
                                inner join (select ID, Name as TextPath from AssetType) P on P.ID = IA.ID";
                }
                else
                {
                    assetJoin = $@"inner join AssetType AST on AST.Object = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then IT.Subject else IT.Object end)" : "IT.Subject")}
                                                            and AST.ObjectID = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then IT.SubjectID else IT.ObjectID end)" : "IT.SubjectID")}
                                inner join Asset IA on IA.Object = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.Subject else I.Object end)" : "I.Subject")}
                                                        and IA.ObjectID = {(includeInverse ? "(case when I.Object = @type and I.ObjectID = @id then I.SubjectID else I.ObjectID end)" : "I.SubjectID")}
                                cross apply dbo.GetAssetTextPathById(IA.ID, '{(isTargetFusion ? '.' : '/')}') P";
                }

                innerSql = $@"select	I.ID,
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
		            P.TextPath as Name,        
		            IT.Subject as Type,
		            IT.SubjectID as TypeID,
		            AST.Name as TypeName,
		            T.HasTechnicalRelationships
            from [Intersect] I
                inner join IntersectType IT on IT.ID = I.IntersectTypeID
                    {assetJoin}
                    cross apply(
                                select	case 
                                            when count(1) > 0 then cast(1 as bit)
								            else cast(0 as bit)
                                        end as HasTechnicalRelationships
                                from[Intersect]
                                where   Subject = 'Intersect' and SubjectID = I.ID
                                ) T
            where( (I.Object = @type and I.ObjectID = @id) {(includeInverse ? " or (I.Subject = @type  and I.SubjectID = @id) " : "")})
                    and IntersectTypeID = {intersectTypeID}";
            }

            

            var querySql = $@"
select  {columns} 
        A.*
from	(
            {innerSql}
        ) A 
{joins} 
";


            var sql = string.Format(@"select * from ({0}) AA", querySql);
            sql = applyFilteringSuffix(sql, Request);
           
            sql += " order by AA.Name";

            return Company.Query<dynamic>(sql, new { it = intersectTypeID, type = new Dapper.DbString { IsAnsi = true, Value = type.ToString(), IsFixedLength = true, Length = 20 }, id });
        }

        [Route("export/{type}/{id:int}/relationships/{targetType}/{targetID:int}/{intersectTypeID:int}/excel.xls"), HttpGet]
        public HttpResponseMessage RelationshipsForObjectByTargetTypeExportExcel(SystemObjects type, int id, SystemObjects targetType, int targetID, int intersectTypeID)
        {


            var results = this.RelationshipsForObjectByTargetType(type, id, targetType, targetID, intersectTypeID);

            //get the fields for the spreadsheet
            var fields = Company.Filter<FieldType>(i => i.Object == "IntersectType" && i.ObjectID == intersectTypeID && i.IsListable).ToList().OrderBy(x => x.SortOrder);

            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            var colIndex = 0;

            document.SetCellValue(1, ++colIndex, "Name");
            document.SetCellValue(1, ++colIndex, "Critical");


            //add fields for this relation
            foreach (var field in fields)
            {
                document.SetCellValue(1, ++colIndex, field.FriendlyName ?? "");
            }


            #endregion

            int rowIndex = 1;
            foreach (var row in results)
            {
                var dataColIndex = 0;
                rowIndex++;

                document.SetCellValue(rowIndex, ++dataColIndex, row.Name ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Critical == 1 ? "Critical" : "Normal");

                var rowDict = ((IDictionary<string, object>)row);
                foreach (var field in fields)
                {
                    var fieldKey = $"Field{field.ID}";

                    if (rowDict.ContainsKey(fieldKey))
                    {
                        if (rowDict[fieldKey] != null)
                            document.SetCellValue(rowIndex, ++dataColIndex, rowDict[fieldKey].ToString());
                    }
                }

            }

            #endregion

            var detail = Company.GetObjectDetail(type.ToString(), id);

            var stream = new MemoryStream();
            document.SaveAs(stream);            
            stream.Position = 0;
            HttpResponseMessage result = null;
            // serve the file to the client      
            result = Request.CreateResponse(HttpStatusCode.OK);            
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"{detail.Name} relations as of {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }

        [Route("export/maps/{source}/{sourceID:int}/{target}/{targetID:int}/mapitems/excel.xls"), HttpGet]
        public HttpResponseMessage MapItemsExcelExport(SystemObjects source, int sourceID, SystemObjects target, int targetID)
        {
            var list = Company.Query<dynamic>(QueryConstants.MapItems, new { source = source.ToString(), sourceID, target = target.ToString(), targetID });

            var document = new SLDocument();
            document.AddWorksheet("MapItems");

            

            #region Create the list sheet

            #region Header

            var colIndex = 0;

            document.SetCellValue(1, ++colIndex, "Source Type");
            document.SetCellValue(1, ++colIndex, "Source Name");
            document.SetCellValue(1, ++colIndex, "Source Fusion");
            document.SetCellValue(1, ++colIndex, "Source Fusion Attribute Type");
            document.SetCellValue(1, ++colIndex, "Source Fusion Attribute");


            document.SetCellValue(1, ++colIndex, "Target Type");
            document.SetCellValue(1, ++colIndex, "Target Name");
            document.SetCellValue(1, ++colIndex, "Target Fusion");
            document.SetCellValue(1, ++colIndex, "Target Fusion Attribute Type");
            document.SetCellValue(1, ++colIndex, "Target Fusion Attribute");

            #endregion

            int rowIndex = 1;
            foreach (var row in list)
            {
                var dataColIndex = 0;
                rowIndex++;

                document.SetCellValue(rowIndex, ++dataColIndex, row.SourceType ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.SourceName ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.SourceFusion ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.SourceFusionAttributeType ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.SourceFusionAttribute ?? "");

                document.SetCellValue(rowIndex, ++dataColIndex, row.TargetType ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.TargetName ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.TargetFusion ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.TargetFusionAttributeType ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.TargetFusionAttribute ?? "");

            }

            #endregion

            var sourceObj = GetObjectDetail(source, sourceID);
            var targetObj = GetObjectDetail(target, targetID);

            var stream = new MemoryStream();
            document.SaveAs(stream);
            
            stream.Position = 0;
            HttpResponseMessage result = null;
            // serve the file to the client      
            result = Request.CreateResponse(HttpStatusCode.OK);            
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"{sourceObj.Name} to {targetObj.Name} mappings {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }

        [Route("{type}/{id:int}/{predicateId:int}/synonyms")]
        public HttpResponseMessage GetSynonymsByObject(SystemObjects type, int id, int predicateId)
        {
            var models = Company.Query<dynamic>(
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

            var availablePredicates = Company.Filter<Predicate>(x => x.Type == PredicateType.Grammar).OrderBy(x=>x.Name);

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

            
            var models = Company.Query<string>(sql, new { SurveyTypeID = typeID, Object = new DbString {Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, ObjectID = id });
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
                    Company.Query<int>("insert into questionoption (QuestionID, QuestionTypeOptionID) values(@qId, @qTypeId)", new { qId = q.ID, qTypeId = value.ID });
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

        [Route("catalogs")]
        public HttpResponseMessage GetTaxonomyTypes()
        {
            var query = Company.Query<dynamic>(@"
select	    FAT.ID,
		    FAT.Name,
            ISNULL(FAT.Description,'') as Description,
		    FAT.MaximumDepth,
            T.ID as AssetTypeID
from	    TaxonomyType FAT
		    inner join AssetType T on T.Object = 'TaxonomyType' and T.ObjectID = FAT.ID");

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, query);
        }

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
        public HttpResponseMessage GetTaxonomyType(int typeID)
        {            
            var row = Company.Query<dynamic>(QueryConstants.TaxonomySettingsItem, new { id = typeID }).Single();
            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object> {
                    { "ID", row.ID },
                    { "MaximumDepth", row.MaximumDepth },
                    { "Name", row.Name },
                    { "Description", row.Description },
                    { "AllowAttributes", (bool)row.AllowAttributes },
                    { "NymTypes", Company.Query<dynamic>(QueryConstants.ObjectNymTypes, new { id = typeID, ot = new DbString {Value = "TaxonomyType", IsFixedLength = true, IsAnsi = true, Length = 50 } }) },
                    { "ClassificationName", row.ClassificationName },
                    { "HasDashboards", row.HasDashboards }
                }
            );
        }

        [Route("catalogs/{typeID:int}/all")]
        public IQueryable<Taxonomy> GetTaxonomiesByType(int typeID)
        {
            return Company.Filter<Taxonomy>(i => i.TaxonomyTypeID == typeID);
        }

        [Route("catalogs/{typeID:int}/{id:int}")]
        public Taxonomy GetTaxonomy(int typeID, int id)
        {
            var model = Company.GetById<Taxonomy>(id, i => i.TaxonomyType);

            if (model == null)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }
            else
            {
                if (model.TaxonomyTypeID != typeID)
                    throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            return model;
        }

        #endregion

        #region Allocations

        [Route("AttributeType/{id}/allocations")]
        public IQueryable<AttributeTypeRelationDetail> GetAllocationsByAttributeType(int id)
        {
            return Company.Filter<AttributeTypeRelationDetail>(i => i.AttributeTypeID == id);
        }

        #endregion

        #region Counts

        [Route("CountItems/Activity/{artifactTypeId}/{days}")]
        public IEnumerable<AssetDetail> GetAreaActivityItems(int artifactTypeId, int days)
        {
            var sql = @"select 
                            *
                        from
                            assetdetail a                            
                        where
                            a.[object] = 'Artifact' and a.[typeid] = @typeId";

            if (days != 0)
            {
                days = days * -1;

                sql += " and (a.createdon > dateadd(day, @d, CURRENT_TIMESTAMP) or a.updatedon > dateadd(day, @d, CURRENT_TIMESTAMP))";

                return Company.Query<AssetDetail>(sql, new { typeId = artifactTypeId, d = days });            
            }

            return Company.Query<AssetDetail>(sql, new { typeId = artifactTypeId});
        }

        [Route("Count/{area}/{days}")]
        public IEnumerable<CountModel> GetHomeCounts(string area, int days, int id = -1)
        {
            var areaName = (area ?? string.Empty).ToUpper();
            var resourceId = id > 0 ? id : Company.CurrentResourceID;

            switch (areaName)
            {
                case "SOCIAL":
                    return LoadSocialActivityCount(days, resourceId);
                case "ACTIVITY":
                    return LoadArtifactActivityCount(days);
                case "ASSIGNMENTS":
                    return LoadWorkflowAssignmentsCount(resourceId);
            }
            return null;
        }

        [Route("Counts/{id}/{days}")]
        public IEnumerable<CountModel> GetTheCounts(int days, int id = -1)
        {
            var resourceId = id > 0 ? id : Company.CurrentResourceID;
            return LoadSocialActivityCount(days, resourceId);
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

        private IEnumerable<CountModel> LoadSocialActivityCount(int days, int resourceId)
        {
            days = days * -1;

            var counts = Company.GetCommentCountByFollower(resourceId, days).ToList().OrderBy(i => i.CommentTypeName);

            List<CountModel> items = new List<CountModel>();

            //need to add a record for social, Issue, Task, DataEvent, Question

            items.Add(new CountModel { Name = Resources.Core.CommentType_Social, Total = getCommentCategoryCount(counts, CommentType.Social)  });

            items.Add(new CountModel { Name = Resources.Core.CommentType_Action, Total = getCommentCategoryCount(counts, CommentType.Issue) });

            items.Add(new CountModel { Name = Resources.Core.CommentType_Task, Total = getCommentCategoryCount(counts, CommentType.Task) });

            items.Add(new CountModel { Name = Resources.Core.CommentType_DataEvent, Total = getCommentCategoryCount(counts, CommentType.DataEvent) });
            
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
        }

        [Route("breadcrumb/typeahead")]
        public async Task<IEnumerable<BreadcrumbTypeAheadModel>> GetBreadcrumbTypeahead(string q, int num, SystemObjects objectType, int objectId)
        {
            var sql = $"select top {num} ad.DisplayValue as Name, u.Url  from asset ast inner join assettype astt on (ast.assetTypeID = astt.id)  inner join AssetDisplayValue AD on AD.assetid = ast.id cross apply [dbo].GetAssetUrlById(ast.ID) u where ast.[object] = @typeName and astt.objectId = @typeId and ad.DisplayValuePrefix like @search";

            return await Company.QueryAsync<BreadcrumbTypeAheadModel>(sql, new { typeName = new DbString { Value = objectType.ToString(), IsFixedLength = true, Length = 20, IsAnsi = true }, typeId = objectId, search = $"{q}%" });            
        }

        #endregion

        #region Reference - new replaces domain

        [HttpGet, Route("referenceItemTypes")]
        public IEnumerable<ReferenceItemType> GetReferenceItemTypes()
        {
            var sql = "select rit.*, ast.id as AssetTypeID from referenceitemtype rit inner join assettype ast on rit.id = ast.objectid and ast.[object] = 'ReferenceItemType'";

            return Company.Query<ReferenceItemType>(sql);            
        }

        [HttpGet, Route("canReadReferenceItemType/{id:int}")]
        public async Task<HttpResponseMessage> CanReadReferenceItemType(int id)
        {
            var records = await Company.QueryAsync<dynamic>(@"
select	1 
from	ResponsibilityDetail
where	Type = 'ReferenceItemType'
		and TypeID = @id 
		and PermissionsBitMask & 1 = 0
		and ResourceID = @resource", new { id, resource = Company.CurrentResourceID });

            return Request.CreateResponse(HttpStatusCode.OK, !records.Any());
        }

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

        [HttpGet, Route("referenceItems/{typeID:int}/items.xls")]
        public async Task<HttpResponseMessage> GetReferenceItemsExcel(int typeID)
        {
            var models = await Company.QueryAsync<dynamic>($"exec [dbo].[GetReferenceItemValues] {typeID}, {Company.CurrentResourceID}");


            var fields = Company.Filter<FieldType>(i => i.Object == "ReferenceItemType" && i.ObjectID == typeID).ToList().OrderBy(x => x.ColumnOrder);
            var relations = new List<AssetType>();

            var parent = Company.GetParentType(typeID, SystemObjects.ReferenceItemType);
            var maxLoops = 20;

            while(parent != null && maxLoops > 0)
            {
                relations.Insert(0,parent);

                parent = Company.GetParentType(parent.ID, SystemObjects.ReferenceItemType);
                                
                maxLoops--;
            }


            var document = new SLDocument();
            document.AddWorksheet("Items");
            document.DeleteWorksheet("Sheet1");

            #region Create the list sheet

            #region Header

            var colIndex = 0;

            document.SetCellValue(1, ++colIndex, "Asset ID");
            document.SetCellValue(1, ++colIndex, "Code");

            //add parents for this ref list
            foreach (var refList in relations)
            {
                document.SetCellValue(1, ++colIndex, refList.Name ?? "");
            }

            //add fields for this 
            foreach (var field in fields)
            {
                document.SetCellValue(1, ++colIndex, field.FriendlyName ?? "");
            }


            #endregion

            int rowIndex = 1;
            foreach (var row in models)
            {
                var dataColIndex = 0;
                rowIndex++;

                document.SetCellValue(rowIndex, ++dataColIndex, row.AssetID ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Code ?? "");
                
                var rowDict = ((IDictionary<string, object>)row);

                foreach(var parentRefList in relations)
                {
                    var key = $"Rel{parentRefList.ID}";

                    if (rowDict.ContainsKey(key))
                    {
                        document.SetCellValue(rowIndex, ++dataColIndex, (rowDict[key] ?? "").ToString());                        
                    }
                }

                foreach (var field in fields)
                {
                    var fieldKey = $"Field{field.ID}";

                    if (rowDict.ContainsKey(fieldKey))
                    {
                        var value = (rowDict[fieldKey] ?? "");

                        SetCellValue(document, rowIndex, ++dataColIndex, field.Type, value);
                    }
                }
            }

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            var len = stream.Length;
            stream.Position = 0;
            HttpResponseMessage result = null;
            // serve the file to the client      
            result = Request.CreateResponse(HttpStatusCode.OK);
            
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Reference Items.xlsx"
            };
            return result;
        }

        #endregion

        #region Issue Types

        [Route("adminissuetypes")]
        public IQueryable<core.entities.IssueType> GetAdminIssueTypes()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));                
            }

            return Company.Table<core.entities.IssueType>();
        }

        [Route("issuetypes")]
        public IQueryable<core.entities.IssueType> GetIssueTypes(string @object = null, int? objectID = null)
        {
            var sql = @"select I.ID, I.Name, I.Description, I.IsSystem, I.UpdatedBy, I.UpdatedOn from IssueType I
                cross apply (select count(*) as Allocations from IssueTypeRelation R where R.IssueTypeID = I.ID) C
                where C.Allocations = 0
                {0}";
            if (@object != null && objectID != null)
                sql = string.Format(sql, @"union all
                    select I.ID, I.Name, I.Description, I.IsSystem, I.UpdatedBy, I.UpdatedOn from IssueType I
                    inner join IssueTypeRelation R on R.IssueTypeID = I.ID
                    inner join AssetType T on T.ID = R.AssetTypeID
                    inner join Asset A on A.AssetTypeID = T.ID
                    where A.Object = @object and A.ObjectID = @objectID
                    order by name asc");
            else
                sql = string.Format(sql, "order by name asc");

            return Company.Query<IssueType>(sql, new { @object, objectID }).AsQueryable();
        }

        [Route("issue/{issueID:int}")]
        public HttpResponseMessage GetIssue(int issueID)
        {
            var issue = Company.GetById<Issue>(issueID);

            if (issue == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            var fields = Company.GetFieldRelationsByObject(SystemObjects.Issue, issueID).OrderBy(x=>x.SortOrder).ToList();
            List<dynamic> values = new List<dynamic>();

            foreach (var field in fields)
            {
                values.Add(new { FieldName = field.FriendlyName, Value = field.FormattedValue, Type = field.Type });
            }
            
            return Request.CreateResponse(HttpStatusCode.OK, new
            {        
                Issue = issue,    
                Fields = values,
            });

        }

        [Route("issuetype/{issueTypeID:int}/allocations")]
        public HttpResponseMessage GetIssueTypeRelations(int issueTypeID)
        {
            var relations = Company.Query<IssueTypeRelation>(@"select R.IssueTypeID, R.AssetTypeID, T.[Object] as ObjectType, coalesce(FAT.TextPath, T.[Name]) as TypeName 
                from IssueTypeRelation R
                inner join AssetType T on T.ID = R.AssetTypeID
                left join FusionAttributeType FAT on T.[Object] = 'FusionAttributeType' and FAT.ID = T.ObjectID
                inner join IssueType I on I.ID = R.IssueTypeID
                where R.IssueTypeID = @issueTypeID", new { issueTypeID }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, new { Allocations = relations });
        }
        #endregion

        #region Metrics

        [Route("metrics/assettypes")]
        public HttpResponseMessage GetMetricAssetTypes()
        {
            List<AssetTypeClass> classes = new List<AssetTypeClass>() {
                AssetTypeClass.Glossary,
                AssetTypeClass.Model,
                AssetTypeClass.Policy
            };
            var models = Company.Filter<AssetType>(i => classes.Contains(i.Class)).Select(i => new {
                Uid = i.uid,
                i.Name,
                i.Class
            }).ToList().Select(i => new {
                i.Uid,
                i.Name,
                Class = i.Class.GetDisplayName()
            }).OrderBy(i => i.Class).ThenBy(i => i.Name);

            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        #endregion

        #region Cascading dropdown values

        [Route("FieldType_CascadingListValues/{fieldTypeID:int}")]
        public List<System.Web.Mvc.SelectListItem> GetCascadingDropdownFieldValues(int fieldTypeID, string parentItemId, string parentValues)
        {
            var predicateTypeId = 3;
            string[] parents = null;

            List<System.Web.Mvc.SelectListItem> items = new List<System.Web.Mvc.SelectListItem>();

            var fieldType = Company.FieldTypes.Where(x => x.ID == fieldTypeID).FirstOrDefault();

            if (fieldType == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            if (!string.IsNullOrEmpty(parentItemId))
            {
                parents = parentItemId.Split(',');
            }
            else if(!string.IsNullOrEmpty(parentValues))
            {
                
                var sqlParent = "select value from fieldlookupvalue where fieldtypeid = @id and text in @vals";
                parents = Company.Query<string>(sqlParent, new { id = fieldType.ParentFieldTypeID, vals = parentValues.Split(',')}).ToArray();
            }
            
            if((fieldType.LookupObjectType ?? "").ToUpper() != "REFERENCEITEM") throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            var referenceListType = Company.ReferenceItemTypes.Where(x => x.ID == fieldType.LookupObjectID).FirstOrDefault();

            if(referenceListType == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
                        
            var parentReferenceListType = Company.GetParentType(referenceListType.ID, SystemObjects.ReferenceItemType);

            if(parentReferenceListType == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            var sql = @"select flv.Text, flv.Value from fieldlookupvalue flv 
                        inner join[intersectdetail] id on(id.subjecttype = 'ReferenceItemType' and id.objecttype = 'ReferenceItemType' and id.predicatetype = @predicate and id.objectid = flv.value and id.objecttypeid = flv.lookupobjectid and id.subjecttypeid = @parentReferenceListTypeId)
                        inner join[referenceitem] ri on(ri.referenceitemtypeid = id.subjecttypeid and ri.id = id.subjectid and ri.id in @parentReferenceItemId)
                        where flv.fieldTypeID = @id";

            items = Company.Query<System.Web.Mvc.SelectListItem>(sql, new { id = fieldTypeID, predicate = predicateTypeId, parentReferenceItemId = parents, parentReferenceListTypeId = parentReferenceListType.ObjectID }).ToList();

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
            return Company.ApiServices.FirstOrDefault(x=>x.ID==id);
        }


        [Route("custom/service/{serviceId:int}/endpoints")]
        public List<ApiEndpoint> GetCustomAPIServiceEndpoints(int serviceId)
        {
            return Company.ApiEndpoints.Where(x=>x.ServiceID == serviceId).ToList();
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
