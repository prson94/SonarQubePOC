using d360.core.entities;
using d360.core.entities.Views;
using d360.core;
using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using d360.core.exceptions;
using System.Data.SqlClient;
using d360.core.enums;
using d360.model;
using d360.core.entities.Transitive;
using System.Data.Entity.Design.PluralizationServices;
using System.Web.Http.Description;
using d360.workflow.entities;
using d360.workflow;
using System.Runtime.Serialization;
using System.Dynamic;
using System.Web;

namespace d360.web.Controllers
{
    [RoutePrefix("api"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class D3SApiController : BaseApiController
    {
        #region DI


        public D3SApiController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
#if DEBUG
            company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
#endif
        }

        #endregion

        #region Field Data

        void loadDisplayFields(List<DisplayField> list, SystemObjects type, int id)
        {
            var fields = Company.GetFieldRelationsByObject(type, id);
            foreach (var k in fields)
            {
                list.Add(new DisplayField {
                    FriendlyName = k.FriendlyName,
                    Value = k.FormattedValue,
                    Name = k.Name
                });
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
            

            var typeCheckSql = "";
            switch (type)
            {
                case SystemObjects.Attribute:
                    typeCheckSql = $"select 'AttributeType' as [Type], AttributeTypeID as ID from [Attribute] where ID = {id}";
                    break;
                case SystemObjects.FusionAttribute:
                    typeCheckSql = $"select 'FusionAttributeType' as [Type], FusionAttributeTypeID as ID from [FusionAttribute] where ID = {id}";
                    break;
                default:
                    typeCheckSql = $"select ObjectType as [Type], ObjectTypeID as ID from cache.Object where Object = '{type.ToString()}' and ObjectID = {id}";
                    break;
            }

            var typeCheck = Company.Query<TypeCheckModel>(typeCheckSql).FirstOrDefault();
            if (typeCheck != null)
            {
                var fields = Company.GetFieldRelationsByObject(type, id).ToList();
                var fieldTypes = Company.Filter<FieldType>(i => i.Object == typeCheck.Type && i.ObjectID == typeCheck.ID).OrderBy(i => i.SortOrder).ToList();

                fieldTypes.ForEach(ft => {
                    var k = fields.SingleOrDefault(i => i.FieldTypeID == ft.ID);
                    if (k != null)
                    {
                        if (k.Type == DataType.FusionLookup.ToString())
                        {
                            //look at fusionlookup field and figure out what to show
                            list.AddRange(RenderFusionLookupField(k));
                        }
                        else
                        {
                            var ro = new ReadOnlyField
                            {
                                Name = k.FriendlyName,
                                Value = k.FormattedValue,
                                FieldDescription = k.DisplayDescription,
                                FieldName = k.Name
                            };
                            if (!string.IsNullOrEmpty(k.LookupObjectType) && k.LookupObjectID.HasValue)
                            {
                                ro.TooltipContext = TemplateAction.LookupPreview.ToString();
                                ro.TooltipID = k.LookupObjectType == "Lookup" ? k.LookupObjectID : (string.IsNullOrEmpty(k.Value)) ? 0 : int.Parse(k.Value);
                                ro.TooltipType = k.LookupObjectType == "Lookup" ? SystemObjects.LookupType.ToString() : k.LookupObjectType;
                                ro.TooltipUrl = k.LookupUrl;
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
                        //Computed field, maybe.
                        if (ft.Type == DataType.RelationLookup.ToString())
                        {
                            //look at fusionlookup field and figure out what to show
                            list.AddRange(RenderRelationLookupField(type.ToString(), id, ft.ID));
                        }
                    }
                });
            }


            return list;
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
                    FieldName = string.Format("", k.TargetObject, k.TargetObjectID)
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
        string calculateStaticColumnWidth(int thisColumnWidth, decimal dynamicFieldWidth, int remainingWidth, int staticFieldCount)
        {
            return string.Format("{0}%", thisColumnWidth + ((dynamicFieldWidth == 0) ? remainingWidth / staticFieldCount : 0));
        }

        GridColumn getGridColumnForColumn(FieldTypeWithRelation item, decimal dynamicFieldWidth, bool serverPaged, bool loadLookupList = true)
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
                        filterItems = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == item.ID && o.LookupObjectType == item.LookupObjectType && o.LookupObjectID == item.LookupObjectID).OrderBy(o => o.Text).Select(o => o.Text).ToList();                    
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
                    filterType = GridColumn.FILTER_TYPE_CHECKBOX;
                    break;
            }

            var gc = new GridColumn { text = item.FriendlyName, datafield = $"Field{item.ID}", width = string.Format("{0}%", dynamicFieldWidth), columntype = columnType, filtertype = filterType, filteritems = filterItems, cellsformat = cellsFormat };
            if (!string.IsNullOrEmpty(item.Category))
            {
                gc.columngroup = item.Category.Replace(" ", "");
            }
            return gc;
        }

        GridField getGridFieldForColumn(FieldTypeWithRelation item)
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
            }

            return new GridField { name = $"Field{item.ID}", type = fieldType };
        }

        void parseDynamicColumnsAndFields(List<FieldTypeWithRelation> items, List<GridColumn> columns, List<GridField> fields, List<GridColumnGroup> groups, decimal dynamicFieldWidth, bool serverPaged = false)
        {
            items.ForEach(i =>
            {
                if (!string.IsNullOrEmpty(i.Category))
                {
                    groups.Add(new GridColumnGroup { align = "center", name = i.Category.Replace(" ", ""), text = i.Category });
                }
                columns.Add(getGridColumnForColumn(i, dynamicFieldWidth, serverPaged));

                fields.Add(getGridFieldForColumn(i));
            });        
        }

        void parseDynamicFilterFields(List<FieldTypeWithRelation> items, List<GridFilterColumn> columns, decimal dynamicFieldWidth, bool relatedField, bool hiddenField)
        {
            items.ForEach(i =>
            {
                GridFilterColumn col = new GridFilterColumn(getGridColumnForColumn(i, dynamicFieldWidth, true));

                col.id = i.ID.ToString();
                col.relatedfield = relatedField;
                col.hiddenfield = hiddenField;

                columns.Add(col);
                
            });
        }

        [HttpGet, Route("{type}/{id:int}/grid/definition")]
        public HttpResponseMessage GetGridDefinitionByType(SystemObjects type, int id)
        {
            #region Resolve to underlying types
            switch (type)
            {
                case SystemObjects.EventGroup:
                    var eventGroup = Company.GetById<EventGroup>(id);
                    if (eventGroup != null)
                    {
                        type = SystemObjects.Rule;
                        id = eventGroup.RuleID ?? 0;
                        eventGroup = null;
                    }
                    break;
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
            var totalItems = Company.Filter<FieldTypeWithRelation>(i => i.Object == sType && i.ObjectID == id).ToList();
            var items = totalItems.Where(i => i.IsListable).OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName).ToList();
            
            var columns = new List<GridColumn>();
            var fields = new List<GridField>();
            var filterColumns = new List<GridFilterColumn>();
            var groups = new List<GridColumnGroup>();
            decimal dynamicFieldWidth = 0;
            int remainingWidth = 0;
            //int columnWidth = 0;
            int staticFieldCount = 0;
            ObjectDetail detail = null;

            Dictionary<string, string> settings = null;

            switch (type)
            { 
                case SystemObjects.ArtifactType:
                    #region

                    settings = Community.GetCompanySettings();

                    var taxonomyTypes = Company.Table<TaxonomyType>().Select(i => i.Name).ToList();
                    var artifactType = Company.GetById<ArtifactType>(id);
                    var hasParentType = false;

                    if (artifactType != null)
                        hasParentType = artifactType.ParentID.HasValue;

                    staticFieldCount = hasParentType ? 4 : 3;
                    remainingWidth = hasParentType ? 35 : 45;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Name_Name, datafield = "Name", width = calculateStaticColumnWidth(20, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    
                    if (hasParentType)
                    {
                        columns.Add(new GridColumn { text = d360.core.resources.Fields.Parent_Name, datafield = "Parent", columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_LIST, width = calculateStaticColumnWidth(10, dynamicFieldWidth, remainingWidth, staticFieldCount), filterable = true, filteritems = Company.Filter<Artifact>(i => i.ArtifactTypeID == artifactType.ParentID).OrderBy(i => i.TextPath).Select(i => i.TextPath).ToList() });                        
                    }

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    columns.Add(new GridColumn { text = settings["ArtifactType_TaxonomyTypeID"], datafield = "TaxonomyType", width = calculateStaticColumnWidth(14, dynamicFieldWidth, remainingWidth, staticFieldCount), filterable = true, columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_LIST, filteritems = taxonomyTypes });                    
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Status_Name, datafield = "Status", width = calculateStaticColumnWidth(9, dynamicFieldWidth, remainingWidth, staticFieldCount), filterable = true, columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_LIST, filteritems = new List<string>() { "Draft", "Under Review", "Certified" } });
                    
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    if (hasParentType)
                    {
                        fields.Add(new GridField { name = "ParentID", type = "number" });
                        fields.Add(new GridField { name = "Parent", type = "string" });
                        fields.Add(new GridField { name = "ParentUrl", type = "string" });
                    }
                    fields.Add(new GridField { name = "TaxonomyTypeID", type = "number" });
                    fields.Add(new GridField { name = "TaxonomyType", type = "string" });
                    fields.Add(new GridField { name = "Status", type = "string" });
                    fields.Add(new GridField { name = "DateLastCertified", type = "date" });
                    fields.Add(new GridField { name = "Url", type = "string" });


                    filterColumns.AddRange(columns.Select(p => new GridFilterColumn(p)));

                    filterColumns.Add(new GridFilterColumn { text = d360.core.resources.Fields.Description_Name, datafield = "Description", width = "0"});

                    var hiddenItems = totalItems.Where(i => i.Type != "FusionLookup" && i.Type != "RelationLookup" && !i.IsListable).OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName).ToList();
                    parseDynamicFilterFields(hiddenItems, filterColumns, dynamicFieldWidth, false, true);

                    //Load any fields that are displayed on relationships so we can show them as 
                    // filters in the grid
                    IEnumerable<int> intersectTypeIDs = Company.Query<int>("select  intersecttypeid from utility.relationshiptypes where sourceobjecttype = 'ArtifactType' and sourceobjectid = @objectid", new { objectid = id });

                    if (intersectTypeIDs.Any())
                    {
                        var totalRelItems = Company.Filter<FieldTypeWithRelation>(i => i.Object == "IntersectType" && intersectTypeIDs.Contains(i.ObjectID)).ToList();
                        var relItems = totalRelItems.Where(i => i.IsListable).OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName).ToList();

                        if (relItems.Any())
                        {                            
                            parseDynamicFilterFields(relItems, filterColumns, dynamicFieldWidth, true, false);                         
                        }
                    }

                    filterColumns = filterColumns.OrderByDescending(x => x.datafield == "Name").ThenByDescending(x => x.datafield == "Description").ThenByDescending(x => x.datafield == "Status").ThenBy(x => x.text).ToList();

                    break;
                #endregion
                case SystemObjects.IntersectType:
                    #region

                    var intersectType = Company.GetById<IntersectType>(id);

                    staticFieldCount = 4;
                    remainingWidth = 50;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "IntersectID", type = "number" });
                    fields.Add(new GridField { name = "Description", type = "string" });
                    fields.Add(new GridField { name = "Role", type = "string" });
                    fields.Add(new GridField { name = "TargetName", type = "string" });
                    fields.Add(new GridField { name = "TargetObjectID", type = "string" });
                    fields.Add(new GridField { name = "TargetObjectType", type = "date" });
                    fields.Add(new GridField { name = "TargetTypeID", type = "string" });
                    fields.Add(new GridField { name = "TargetType", type = "string" });
                    fields.Add(new GridField { name = "TargetTypeName", type = "string" });
                    fields.Add(new GridField { name = "Classification", type = "string" });
                    fields.Add(new GridField { name = "TargetUrl", type = "string" });
                    fields.Add(new GridField { name = "HasTechnicalRelationships", type = "string" });
                    break;
                #endregion
                case SystemObjects.LookupType:
                    #region
                    staticFieldCount = 1;
                    remainingWidth = 90;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "LookupTypeID", type = "number" });
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region

                    var policyType = Company.GetById<PolicyType>(id);

                    staticFieldCount = 1;
                    remainingWidth = 45;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Name_Name, datafield = "Name", width = calculateStaticColumnWidth(55, dynamicFieldWidth, remainingWidth, staticFieldCount) });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "ParentID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    //fields.Add(new GridField { name = "Description", type = "string" });
                    fields.Add(new GridField { name = "PolicyTypeID", type = "number" });
                    break;
                #endregion
                case SystemObjects.Rule:
                    #region
                    staticFieldCount = 4;
                    remainingWidth = 55;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = "Date", datafield = "Date", columntype = GridColumn.COLUMN_TYPE_DATE, filtertype = GridColumn.FILTER_TYPE_RANGE, width = calculateStaticColumnWidth(15, dynamicFieldWidth, remainingWidth, staticFieldCount), cellsformat = "MM/dd/yyyy HH:mm:ss" });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth, true);

                    columns.Add(new GridColumn { text = "Criticality", datafield = "Criticality", columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_CHECKEDLIST, width = calculateStaticColumnWidth(10, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.SourceID_Name, datafield = "SourceID", width = calculateStaticColumnWidth(10, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Status_Name, datafield = "Status", columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_LIST, filteritems = new List<string>() { "Assigned", "Open", "Closed" }, width = calculateStaticColumnWidth(10, dynamicFieldWidth, remainingWidth, staticFieldCount) });

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Date", type = "date" });
                    fields.Add(new GridField { name = "Criticality", type = "string" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "SourceID", type = "string" });
                    fields.Add(new GridField { name = "Rule", type = "string" });
                    fields.Add(new GridField { name = "Status", type = "string" });
                    break;
                    #endregion
                case SystemObjects.FusionAttributeType:
                    #region
                    staticFieldCount = 1;
                    remainingWidth = 75;

                    detail = Company.GetObjectDetail(type, id);


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
                    if (!string.IsNullOrEmpty(Request.GetQueryString("fusionID")))
                    {
                        fusionIDPresent = int.TryParse(Request.GetQueryString("fusionID"), out fusionID);
                    }

                    //Parent columns have be listed in DESC order by Level.
                    parents.ForEach(i =>
                    {
                        if (fusionIDPresent)
                        {
                            var parentFilterValues = Company.Query<string>(@"select Name from FusionAttribute where FusionID = @f and FusionAttributeTypeID = @t group by Name order by Name", new { f = fusionID, t = i.ID }).ToList();
                            filterColumns.Add(new GridFilterColumn { text = i.Name, datafield = $"Parent{i.ID}", width = "", filtertype = GridColumn.COLUMN_TYPE_DROPDOWN, columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filteritems = parentFilterValues });
                            columns.Add(new GridColumn { text = i.Name, datafield = $"Parent{i.ID}", width = "100px", filteritems = new List<string>() });
                        }
                        else
                        {
                            columns.Add(new GridColumn { text = i.Name, datafield = $"Parent{i.ID}", width = "100px", filteritems = new List<string>() });
                        }
                        fields.Add(new GridField { name = $"Parent{i.ID}", type = "string" });
                    });

                    #endregion

                    var relations = Company.Query<dynamic>(@"SELECT distinct 'IntersectType' + cast(S.IntersectTypeID as varchar(10)) as Name, TD.Name as FriendlyName
				FROM		IntersectTypeNode S
							inner join IntersectTypeNode T ON T.IntersectTypeID = S.IntersectTypeID and T.ID <> S.ID and S.ObjectType = 'FusionAttributeType' and S.ObjectID = @id
							inner join cache.ObjectDetails TD on TD.[Object] = T.ObjectType and TD.ObjectID = T.ObjectID", new { id = id }).ToList();

                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count() + relations.Count);

                    filterColumns.Add(new GridFilterColumn { text = "ID", datafield = "ID", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
                    filterColumns.Add(new GridFilterColumn { text = detail.Name, datafield = "Name", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
                    columns.Add(new GridColumn { text = detail.Name, datafield = "Name", width = calculateStaticColumnWidth(25, dynamicFieldWidth, remainingWidth, staticFieldCount), filteritems = new List<string>() });
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

                    relations.ForEach(i =>
                    {
                        columns.Add(new GridColumn { text = i.FriendlyName, datafield = i.Name, width = string.Format("{0}%", dynamicFieldWidth), filtertype = GridColumn.FILTER_TYPE_NUMBER, cellsformat = "n" });
                        fields.Add(new GridField { name = i.Name, type = "number" });
                        filterColumns.Add(new GridFilterColumn { text = i.FriendlyName, datafield = i.Name, width = "", filtertype = GridColumn.FILTER_TYPE_NUMBER, columntype = GridColumn.COLUMN_TYPE_NUMBER });
                    });

                    break;
                    #endregion
                case SystemObjects.FusionType:
                    #region
                    staticFieldCount = 2;
                    remainingWidth = 61;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Name_Name, datafield = "Name", width = calculateStaticColumnWidth(23, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Enabled_Name, columntype = GridColumn.COLUMN_TYPE_CHECKBOX, filtertype = GridColumn.FILTER_TYPE_CHECKBOX, datafield = "Enabled", width = calculateStaticColumnWidth(8, dynamicFieldWidth, remainingWidth, staticFieldCount) });

                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "Enabled", type = "boolean" });
                    break;
                    #endregion
                case SystemObjects.ResourceType:
                    #region
                    staticFieldCount = 5;
                    remainingWidth = 34;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.LastName_Name, datafield = "LastName", width = calculateStaticColumnWidth(13, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.FirstName_Name, datafield = "FirstName", width = calculateStaticColumnWidth(13, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Email_Name, datafield = "Email", width = calculateStaticColumnWidth(15, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    parseDynamicColumnsAndFields(items, columns, fields, groups, dynamicFieldWidth);
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.DateLastLoggedIn_Name, datafield = "DateLastLoggedIn", filtertype = GridColumn.FILTER_TYPE_RANGE, cellsformat = "F", width = calculateStaticColumnWidth(15, dynamicFieldWidth, remainingWidth, staticFieldCount) });
                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Status_Name, datafield = "Status", filtertype = GridColumn.FILTER_TYPE_CHECKEDLIST, filteritems = new List<string>() { "Active", "Disabled" }, width = calculateStaticColumnWidth(4, dynamicFieldWidth, remainingWidth, staticFieldCount) });

                    fields.Add(new GridField { name = "ResourceID", type = "number" });
                    fields.Add(new GridField { name = "Email", type = "string" });
                    fields.Add(new GridField { name = "FirstName", type = "string" });
                    fields.Add(new GridField { name = "LastName", type = "string" });
                    fields.Add(new GridField { name = "DateLastLoggedIn", type = "date" });
                    fields.Add(new GridField { name = "Status", type = "string" });
                    break;
                    #endregion
            }

            settings = null;

            return Request.CreateResponse(HttpStatusCode.OK, new {
                Title = (detail != null) ? detail.PluralizedName : "Child Items",
                Type = type.ToString(),
                ID = id,
                FieldsCount = totalItems.Count,
                Fields = fields,
                Columns = columns,
                FilterColumns = filterColumns,
                ColumnGroups = groups
            });
        }

        #endregion

        #region Navigation

        PageActionItem appendReportMenu(SystemObjects type, int id, SystemObjects objectType, int objectTypeID, bool includeRootTypeReports = false)
        {
            var sType = type.ToString();
            //var surveys = Company.Filter<SurveyObjectCache>(i => i.ObjectType == sType && i.ObjectID == id, i => i.SurveyType).OrderBy(i => i.SurveyType.Name).ToList();

            var reports = Company.Filter<core.entities.Report>(i => i.ObjectType == sType && i.ObjectID == objectTypeID).OrderBy(i => i.Name).ToList();

            if (includeRootTypeReports)
            {
                var sOt = objectType.ToString();
                reports.AddRange(Company.Filter<core.entities.Report>(i => i.ObjectType == sOt && i.ObjectID == objectTypeID).OrderBy(i => i.Name));
            }
            
            bool addReportMenu = false;

            var reportActionMenu = new PageActionItem { Title = "Dashboards", Icon = Resources.Actions.Report_Icon };
            
            //surveys.Count > 0 || 
            if (reports.Count > 0) //|| definitions.Count > 0
            {
                addReportMenu = true;
                
                #region Reports

                foreach (var r in reports)
                {
                    reportActionMenu.Items.Add(
                        new PageActionItem
                        {
                            Context = ContextList.ActionGenericReport,
                            Icon = Resources.Actions.Report_Icon,
                            Title = r.Name,
                            Uri = string.Format("/reports/Overlay?reportID={0}&type={1}&id={2}", r.ID, type.ToString(), id)
                        });
                }

                #endregion

                #region Survey Reports

                //foreach (var r in surveys)
                //{
                //    reportActionMenu.Items.Add(
                //        new PageActionItem
                //        {
                //            Context = ContextList.ActionGenericReport,
                //            Icon = Resources.Actions.Report_Icon,
                //            Title = r.SurveyType.Name,
                //            CustomData = {
                //                    new PageActionItemData { Name = "surveyTypeID", Value = r.SurveyTypeID.ToString() }, 
                //                    new PageActionItemData { Name = "objectType", Value = type.ToString() }, 
                //                    new PageActionItemData { Name = "objectID", Value = id.ToString() } 
                //                },
                //            Uri = string.Format("/parts/{0}/{1}/reports/survey/{2}", type.ToString(), id, r.SurveyTypeID)
                //        });
                //}

                #endregion
            }

            return (addReportMenu) ? reportActionMenu : null;
        }

        bool hasPermission(List<SecurityDetail> permissions, Claim claim, ClaimObject claimObject = ClaimObject.Root)
        {
            var hasPermissions = Company.CurrentResourceIsAdmin;
            if (!hasPermissions) hasPermissions = permissions.Any(i => i.ClaimObject == claimObject && i.Claim == claim);
            return hasPermissions;
        }

        void loadResponsiblityTypeAddMenu(SystemObjects type, int id, PageActionItem addItem, bool peopleOnly = false)
        {
            var RTs = Company.GetAllowedResponsibilityTypesByObject(type, id);
            if (peopleOnly)
            {
                RTs = RTs.Where(i => i.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People);
            }

            var addPeopleItem = new PageActionItem { Title = ResponsibilityTypeGroup.People.ToString() };
            foreach (var r in RTs)
            {
                var rItem = new PageActionItem { Context = ContextList.Responsibility, Title = string.Format("{0}", r.Name), Uri = string.Format("/form/AddResponsibility?responsibilityTypeID={0}&type={1}&id={2}", r.ID, type.ToString(), id) };
                addPeopleItem.Items.Add(rItem);
            }

            if (addPeopleItem.Items.Count > 0)
                addItem.Items.Add(addPeopleItem);
            else
                addPeopleItem = null;
        }

        // '/form/EditWorkflowAllocation?workflowType={0}&type=' + type + '&id=' + id
        void loadWorkflowAllocationAddMenu(SystemObjects type, int id, PageActionItem addItem)
        {
            var workflows = type.GetAllowedWorkflows().Where(i => i.ID != WorkflowType.WorkIssue).ToList();

            var sType = type.ToString();
            var currentItems = Company.Filter<WorkflowTypeRelation>(i => i.Object == sType && i.ObjectID == id).ToList();

            var addWorkflowAllocationItem = new PageActionItem { Title = "Workflows" };

            foreach (var r in workflows)
            {
                bool allowAdd = !currentItems.Any(i => i.WorkflowType == r.ID);

                if (allowAdd)
                {
                    var rItem = new PageActionItem { Context = "WorkflowTypeRelation", Icon = Resources.Actions.Governance_Icon, Title = string.Format("{0}", r.Name), Uri = string.Format("/form/AddWorkflowAllocation?workflowType={0}&type={1}&id={2}", r.ID, type.ToString(), id) };
                    addWorkflowAllocationItem.Items.Add(rItem);
                }
            }

            if (addWorkflowAllocationItem.Items.Count > 0)
                addItem.Items.Add(addWorkflowAllocationItem);
        }

        void loadFusionAttributeTypeExportsForFusion(PageActionItem p, List<FusionAttributeType> types, int? parentID, string baseUri, PluralizationService pluralize)
        {
            var funcList = new List<PageActionItem>();

            foreach (var a in types.Where(i => i.ParentID == parentID).OrderBy(i => i.Name))
            {
                var c = new PageActionItem { Context = ContextList.ActionExport, Icon = Resources.Actions.TemplateDownload_Icon, Uri = string.Format("{0}{1}", baseUri, a.ID), Title = pluralize.Pluralize(a.Name) };
                loadFusionAttributeTypeExportsForFusion(c, types, a.ID, baseUri, pluralize);
                p.Items.Add(c);
            }
        }

        void loadFusionAttributeTypeUploadsForFusion(PageActionItem p, List<FusionAttributeType> types, int? parentID, string baseUri, PluralizationService pluralize)
        {
            var funcList = new List<PageActionItem>();

            foreach (var a in types.Where(i => i.ParentID == parentID).OrderBy(i => i.Name))
            {
                var c = new PageActionItem { Context = ContextList.Load, Title = pluralize.Pluralize(a.Name), Uri = string.Format("{0}{1}", baseUri, a.ID) };
                loadFusionAttributeTypeUploadsForFusion(c, types, a.ID, baseUri, pluralize);
                p.Items.Add(c);
            }
        }

        [Route("{type}/{id:int}/actions/{context=default}")]
        public List<PageActionItem> GetObjectActions(SystemObjects type, int id, string context)
        {
            var list = new List<PageActionItem>();
            bool following = false;
            bool followingParent = false;
            Follow followParent = null;
            string followText = "";


            #region Determine permissions

            List<SecurityDetail> permissions = null;
            if (type != SystemObjects.FusionAttributeType)
            {
                if (context == "root")
                {
                    switch (type)
                    { 
                        case SystemObjects.Artifact:
                            permissions = Company.GetPermissions(SystemObjects.ArtifactType, id).ToList();
                            break;
                        case SystemObjects.Domain:
                        case SystemObjects.DomainGroup:
                            permissions = Company.GetPermissions(SystemObjects.DomainType, id).ToList();
                            break;
                        case SystemObjects.Taxonomy:
                            permissions = Company.GetPermissions(SystemObjects.TaxonomyType, id).ToList();
                            break;
                        default:
                            permissions = Company.GetPermissions(type, id).ToList();
                            break;
                    }
                }
                else
                {
                    permissions = Company.GetPermissions(type, id).ToList();
                }
            }

            #endregion

            PageActionItem addItem = null;
            PageActionItem reportNode = null;
            //PageActionItem otherActionsItem = new PageActionItem { Context = "nullform", Uri = "#", Title = "Other Actions" };

            switch (type)
            {
                case SystemObjects.Artifact:
                    #region Actions

                    var artifact = Company.GetById<Artifact>(id);
                    following = Company.IsUserFollowing(type, id, null);
                    followParent = Company.GetFollowingParent(type, id, null);
                    followingParent = (followParent != null && followParent.FollowTypeID == FollowType.Parent);
                    followText = "";
                    if (!followingParent)
                        if (!following)
                            followText = Resources.Actions.Follow;
                        else
                            followText = Resources.Actions.Unfollow;
                    else
                    {
                        followText = "Following ";
                        var obj = Company.GetObjectDetail(followParent.ObjectType, followParent.ObjectID);
                        followText += obj.Name;
                    }

                    if (artifact != null)
                    {
                        #region Add Items menu logic

                        addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };

                        var childTypes = Company.Filter<ArtifactType>(i => i.ParentID == artifact.ArtifactTypeID).ToList();
                        foreach (var c in childTypes)
                        {
                            if (Company.Filter<WorkflowTypeRelation>(i => i.WorkflowType == WorkflowType.SuggestNewArtifact && i.Object == "ArtifactType" && i.ObjectID == c.ID && i.Enabled).Any())
                            {
                                addItem.Items.Add(new PageActionItem { Context = ContextList.Artifact, Icon = Resources.Actions.Add_Icon, Title = c.Name, Uri = string.Format("/form/SuggestNewArtifact?typeID={0}&parentID={1}", c.ID, id) });
                            }
                            else
                            {
                                if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                                {
                                    addItem.Items.Add(new PageActionItem { Context = ContextList.Artifact, Icon = Resources.Actions.Add_Icon, Title = c.Name, Uri = string.Format("/form/artifacts/{0}/add/{1}", c.ID, id) });
                                }
                                
                            }
                        }
                        

                        if (addItem.Items.Count > 0)
                            list.Add(addItem);

                        #endregion

                        if (hasPermission(permissions, Claim.Create, ClaimObject.Root) || hasPermission(permissions, Claim.Create, ClaimObject.Governance))
                        {
                            var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == artifact.ArtifactTypeID && i.WorkflowType == WorkflowType.CertifyArtifact).Any();
                            if (workflowEnabled && artifact.Status == "Draft")
                            {
                                list.Add(new PageActionItem { Context = "RequestCertification", Icon = "send-o", Title = "Request Certification", Uri = string.Format("/form/RequestCertification?id={0}", id) });                        
                            }
                        }

                        
                        if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                        {
                            list.Add(new PageActionItem { Context = ContextList.Artifact, Icon = Resources.Actions.Edit_Icon, Title = Resources.Actions.Edit, Uri = string.Format("/form/artifacts/{0}/{1}/edit", artifact.ArtifactTypeID, id) });
                        }
                        reportNode = appendReportMenu(type, id, SystemObjects.ArtifactType, artifact.ArtifactTypeID);
                        if (reportNode != null) list.Add(reportNode);


                        list.Add(new PageActionItem { Context = followingParent ? "nullform" : ContextList.ActionCommand, CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = followText, Uri = followingParent ? "#" : string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id), Enabled = !followingParent ? true : false });
                        list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                        
                        var challengeExistsSql = @"select count(W.ID)
                                                        from Workflow W
                                                        where
                                                            W.WorkflowType = 4
                                                            and W.Data.exist('/fields/ArtifactID[text() = sql:variable(""@id"")]') = 1
                                                            and W.DateCompleted is null";

                        if (Company.Query<int>(challengeExistsSql, new { id = artifact.ID }).FirstOrDefault() <= 0)
                        {
                            list.Add(new PageActionItem { Context = "Challenge", Icon = Resources.Actions.Challenge_Icon, Title = Resources.Actions.Challenge, Uri = $"/form/Challenge?id={id}" });
                        }
                        
                        var companySettings = Community.GetCompanySettings();
                        var disableIssues = "";

                        companySettings.TryGetValue("DisableIssuePosting", out disableIssues);

                        if(string.Compare(disableIssues, bool.TrueString, true) != 0 )
                            list.Add(new PageActionItem { Context = "Issue", Icon = Resources.Actions.Issue_Icon, Title = Resources.Actions.Issue, Uri = $"/form/RaiseIssue?id={id}" });
                    }
                    break;
                    #endregion
                case SystemObjects.ArtifactType:
                    following = Company.IsUserFollowing(type, id, null);
                    #region Actions
                    if (id > 0)
                    {
                        if (context != "default")
                        {
                            #region Non-admin sidebar

                            var wtr = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == id && i.Enabled).ToList();
                            if (wtr.Count(i => i.WorkflowType == WorkflowType.SuggestNewArtifact) > 0)
                            {
                                var responsibilityIDs = wtr.Select(i => i.ResponsibilityTypeID).ToList();
                                var any = Company.Filter<ResponsibilityDetail>(i => i.ObjectType == "ArtifactType" && i.ObjectID == id && responsibilityIDs.Contains(i.ResponsibilityTypeID)).Any();
                                var suggestAction = new PageActionItem { Context = ContextList.Artifact, Icon = Resources.Actions.Add_Icon, Title = "Suggest new item", Uri = string.Format("/form/SuggestNewArtifact?typeID={0}&parentID=0", id) };
                                if (!any)
                                {
                                    suggestAction.Context = "nullform";
                                    suggestAction.Uri = "#";
                                    suggestAction.Enabled = false;
                                    suggestAction.Title = "Suggest";
                                    suggestAction.Warning = "Suggestion disabled since there is no responsibility assigned to this type.";
                                }
                                list.Add(suggestAction);
                            }
                            else
                            {
                                if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                                {
                                    list.Add(new PageActionItem { Context = ContextList.Artifact, Icon = Resources.Actions.Add_Icon, Title = "Add", Uri = string.Format("/form/artifacts/{0}/add", id) });
                                }
                            }

                            if (hasPermission(permissions, Claim.Update, ClaimObject.Root) && wtr.Count > 0)
                                list.Add(new PageActionItem { Context = ContextList.Workflow, Icon = "code-fork", Title = "Workflow Status", Uri = string.Format("/workflow/ArtifactTypeWorkflowStatusOverlay?id={0}", id) });

                            var exportActionMenu = new PageActionItem { Context = ContextList.ActionExport, Icon = Resources.Actions.ExportToExcel_Icon, Title = Resources.Actions.ExportToExcel_Text, CustomData = { new PageActionItemData { Name = "ExportType", Value = "xls" } } };
                            list.Add(exportActionMenu);

                            reportNode = appendReportMenu(type, id, SystemObjects.ArtifactType, id);
                            if (reportNode != null) list.Add(reportNode);

                            list.Add(new PageActionItem { Context = ContextList.ActionCommand, CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id) });
                            list.Add(
                                new PageActionItem
                                {
                                    Context = ContextList.ActionGenericReport,
                                    Icon = "line-chart",
                                    Title = "Metrics",
                                    Uri = string.Format("/overlays/ArtifactListMetricsDashboard?id={0}", id)
                                }
                            );

                            #endregion Non-admin sidebar
                        }
                        else
                        {
                            #region Admin sidebar
                            list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                            #endregion
                        }
                    }
                    else
                    {
                        reportNode = appendReportMenu(type, 0, type, 0);
                        if (reportNode != null) list.Add(reportNode);
                    }
                    break;
                    #endregion
                case SystemObjects.AttributeType:
                    #region Actions
                    list.Add(new PageActionItem { Context = "AttributeTypeCategories", Icon = "tags", Title = "Categories", Uri = "/overlays/AttributeTypeCategories" });
                    if (id > 0) {
                        list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    }
                    break;
                    #endregion
                case SystemObjects.Domain:
                    #region Actions
                    var Domain = Company.GetById<Domain>(id, i => i.DomainType, i => i.DomainGroup);
                    following = Company.IsUserFollowing(type, id, null);
                    list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id) });
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.DomainGroup:
                    #region Actions
                    following = Company.IsUserFollowing(type, id, null);
                    list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = $"/resources/UpdateFollowStatus?type={type}&id={id}" });
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.DomainType:
                    #region Actions
                    if (context != "root")
                    {
                        if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                        {
                            //addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                            list.Add(new PageActionItem { Context = ContextList.DomainType, Icon = Resources.Actions.Add_Icon, Uri = "/form/domains/add" });

                            //if (id > 0)
                            //{
                            //    if (hasPermission(permissions, Claim.Create, ClaimObject.Governance))
                            //        loadResponsiblityTypeAddMenu(type, id, addItem, true);
                            //}
                            
                            //list.Add(addItem);
                        }
                    }
                    following = Company.IsUserFollowing(type, id, null);
                    list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = $"/resources/UpdateFollowStatus?type={type}&id={id}" });

                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.EmailTemplate:
                case SystemObjects.TooltipTemplate:
                    #region Actions
                    if (id <= 0)
                    {
                        if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                        {
                            addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                            addItem.Items.Add(new PageActionItem { Context = "emailtemplateform", Icon = "envelope", Title = "Email Template", Uri = "/form/templates/email/add" });
                            addItem.Items.Add(new PageActionItem { Context = "tooltiptemplateform", Icon = "file-text-o", Title = "Tooltip Template", Uri = "/form/templates/tooltip/add" });
                            list.Add(addItem);
                        }
                    }
                    break;
                #endregion
                case SystemObjects.Group:
                    #region Actions
                    if (context == "default")
                    {
                        //if (hasPermission(permissions, Claim.Create, ClaimObject.Root) || hasPermission(permissions, Claim.Update, ClaimObject.Root))
                        //{
                        //    list.Add(new PageActionItem { Context = ContextList.Group, Icon = Resources.Actions.Add_Icon, Uri = "/form/AddGroup" });
                        //}
                        following = Company.IsUserFollowing(type, id, null);
                        followParent = Company.GetFollowingParent(type, id, null);
                        followingParent = (followParent != null);

                        if (!followingParent)
                            if (!following)
                                followText = Resources.Actions.Follow;
                            else
                                followText = Resources.Actions.Unfollow;
                        else
                        {
                            followText = "Following Groups";
                        }

                        following = Company.IsUserFollowing(type, 0, null);
                        list.Add(new PageActionItem { Context = followingParent ? "nullform" : "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = followText, Uri = followingParent ? "#" : string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id), Enabled = !followingParent ? true : false });
                    }
                    else 
                    {
                        following = Company.IsUserFollowing(SystemObjects.ResourceType, 1, null);
                        list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow + " People" : "Follow People", Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", SystemObjects.ResourceType, 1) });
                        following = Company.IsUserFollowing(type, 0, null);
                        list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow + " Groups" : "Follow Groups", Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, 0) });
                    }
                    //list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.Fusion:
                    #region Actions
                    if (id > 0)
                    {
                        if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                        {
                            var fusion = Company.GetById<Fusion>(id, i => i.FusionType.FusionAttributeTypes);
                            //list.Add(new PageActionItem { Context = "FusionConfigurationFilters", Icon = "filter", Title = "Filters", Uri = string.Format("/overlays/FusionConfigurationFilters?fusionTypeID={0}&fusionID={1}", fusion.FusionTypeID, fusion.ID) });
                            list.Add(new PageActionItem { Context = "FusionConfigurationHistory", Icon = "history", Title = "History", Uri = string.Format("/overlays/FusionConfigurationHistory?fusionTypeID={0}&fusionID={1}", fusion.FusionTypeID, fusion.ID) });
                            list.Add(new PageActionItem { Context = "FusionConfigurationHistory", Icon = "bolt", Title = "Ownership Rules", Uri = string.Format("/overlays/FusionConfigurationOwnershipRules?fusionTypeID={0}&fusionID={1}", fusion.FusionTypeID, fusion.ID) });
                            list.Add(new PageActionItem { Context = "FusionConfigurationHistory", Icon = "arrow-up", Title = "Promotion Rules", Uri = string.Format("/overlays/FusionConfigurationPromotionRules?fusionTypeID={0}&fusionID={1}", fusion.FusionTypeID, fusion.ID) });

                            if (fusion.Manual)
                            {
                                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                                var export = new PageActionItem { Context = "nullform", Icon = Resources.Actions.TemplateDownload_Icon, Title = "Download", Uri = "#" };
                                loadFusionAttributeTypeExportsForFusion(export, fusion.FusionType.FusionAttributeTypes.ToList(), null, string.Format("/fusion/{0}/configurations/{1}/template/", fusion.FusionTypeID, fusion.ID), pluralize);
                                pluralize = null;
                                list.Add(export);

                                list.Add(new PageActionItem { Context = ContextList.Load, Icon = Resources.Actions.Upload_Icon, Title = "Upload", Uri = string.Format("/form/AddFusionSpreadsheetImport?typeID={0}&id={1}", fusion.FusionTypeID, fusion.ID) });
                            }
                        }
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.FusionAttribute:
                    #region Actions
                    if (id > 0)
                    {
                        //var fusionAttribute = FusionService.GetAttribute(id);
                        //var addMenu = new PageActionItem { Context = "#", Icon = Resources.Actions.Add_Icon, TabIndex = -1, Title = "Add", Uri = "#" };

                        //list.Add(new PageActionItem { Context = ContextList.Intersect, Icon = Resources.Actions.ViewRelationships_Icon, Title = Resources.Actions.ViewRelationships, Uri = string.Format("/relations/RelationOverlay?type={0}&id={1}", type.ToString(), id) });
                        //list.Add(new PageActionItem { Context = ContextList.Intersect, Icon = "retweet", Title = "Add relationships", Uri = string.Format("/relations/Add?type={0}&id={1}", type.ToString(), id) });
                        //list.Add(new PageActionItem { Context = ContextList.ActionDiagram, Icon = "exchange", Title = "Lineage Diagram", Uri = string.Format("/parts/FusionAttribute/{0}/lineage", id) });
                        //list.Add(addMenu);
                    }
                    break;
                    #endregion
                case SystemObjects.FusionAttributeType:
                    #region Actions
                    if (id > 0)
                    {
                        var fusionAttributeType = Company.GetById<FusionAttributeType>(id);
                        permissions = Company.GetPermissions(SystemObjects.FusionType, fusionAttributeType.FusionTypeID).ToList();
                        if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                        {
                            addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                            addItem.Items.Add(new PageActionItem { Context = ContextList.FieldType, Icon = Resources.Actions.Fields_Icon, Title = Resources.Actions.AddField_Text, Uri = "/form/AddFieldType?type=FusionAttributeType&id=" + id });
                            addItem.Items.Add(new PageActionItem { Context = ContextList.FusionAttributeType, Icon = "plus", Title = "Child Type", Uri = string.Format("/form/fusion/{1}/attributes/{0}/add", id, fusionAttributeType.FusionTypeID) });
                            list.Add(addItem);
                        }
                        if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                            list.Add(new PageActionItem { Context = ContextList.FusionAttributeType, Icon = Resources.Actions.Edit_Icon, Title = Resources.Actions.Edit, Uri = string.Format("/form/fusion/{1}/attributes/{0}/edit", id, fusionAttributeType.FusionTypeID) });
                        if (hasPermission(permissions, Claim.Delete, ClaimObject.Root))
                            list.Add(new PageActionItem { Context = ContextList.FusionAttributeType, Icon = Resources.Actions.Delete_Icon, Title = Resources.Actions.Delete, Uri = string.Format("/form/fusion/{1}/attributes/{0}/delete", id, fusionAttributeType.FusionTypeID) });
                        fusionAttributeType = null;
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.FusionType:
                    #region Actions
                    if (id > 0)
                    {
                        //if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                        //    list.Add(new PageActionItem { Context = ContextList.FusionType, Icon = Resources.Actions.Edit_Icon, Title = Resources.Actions.Edit, Uri = string.Format("/form/EditFusionType?id={0}", id) });
                        //if (hasPermission(permissions, Claim.Delete, ClaimObject.Root))
                        //    list.Add(new PageActionItem { Context = ContextList.FusionType, Icon = Resources.Actions.Delete_Icon, Title = Resources.Actions.Delete, Uri = string.Format("/form/DeleteFusionType?id={0}", id) });

                        list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    }
                    break;
                    #endregion
                case SystemObjects.IntersectType:
                    #region Actions
                    if (id > 0)
                    {
                        //list.Add(new PageActionItem { Context = "Allocation", Icon = Resources.Actions.Allocation_Icon, Title = "Allocate Predicates", Uri = string.Format("/form/IntersectTypePredicateEditForm?id={0}", id) });
                        list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    }
                    break;
                    #endregion
                case SystemObjects.LookupType:
                    #region Actions
                    //if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    //{
                    //    list.Add(new PageActionItem { Context = ContextList.LookupType, Icon = Resources.Actions.Add_Icon, Uri = "/form/AddLookupType" });
                    //}
                    list.Add(new PageActionItem { Context = "Usage", Icon = Resources.Actions.Usage_Icon, Title = Resources.Actions.Usage, Uri = string.Format("/overlays/LookupTypeUsage?id={0}", id) });
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.Event:
                    #region Actions
                    if (id > 0)
                    {
                        list.Add(new PageActionItem { Context = ContextList.ActionBoard, Icon = Resources.Actions.Board_Icon, Title = Resources.Actions.Board });
                        //list.Add(new PageActionItem { Context = "assignmentform", Icon = "users", Title = "Assign", Uri = string.Format("/form/monitor/{0}/{1}/assignments/add", type, id) });
                        //list.Add(new PageActionItem { Context = ContextList.Event, Icon = "close", Title = "Resolve", Uri = string.Format("/monitor/Event/{0}/resolve", id) });
                        //list.Add(new PageActionItem { Context = "eventform", Icon = Resources.Actions.Delete_Icon, Title = Resources.Actions.Delete, TabIndex = 1, Uri = "/monitor/Delete?id=" + id });
                    }
                    break;
                    #endregion
                case SystemObjects.EventGroup:
                    #region Actions
                    if (id > 0)
                    {
                        list.Add(new PageActionItem { Context = ContextList.ActionBoard, Icon = Resources.Actions.Board_Icon, Title = Resources.Actions.Board });
                        //list.Add(new PageActionItem { Context = "assignmentform", Icon = "plus", Title = "Events", Uri = string.Format("/monitor/{0}/{1}/assignments/add", type, id) });
                        list.Add(new PageActionItem { Context = "eventgroupform", Icon = "close", Title = "Resolve", Uri = string.Format("/monitor/EventGroup/{0}/resolve", id) });
                        //list.Add(new PageActionItem { Context = "eventform", Icon = Resources.Actions.Delete_Icon, Title = Resources.Actions.Delete, TabIndex = 1, Uri = "/monitor/Delete?id=" + id });
                    }
                    break;
                    #endregion
                case SystemObjects.Policy:
                    #region Actions
                    Policy policy = null;

                    string nextPolicyLevelName = "policy";
                    PolicyTypeLevel policyLevel = null;
                    if (context == "root")
                    {
                        var policyType = Company.GetById<PolicyType>(id);
                        if (policyType != null)
                        {
                            policyLevel = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == id && i.Level == 1).SingleOrDefault();
                            nextPolicyLevelName = (policyLevel != null) ? policyLevel.Name : string.Format("{0} {1}", policyType.Name.ToLower(), nextPolicyLevelName);
                            if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                            {
                                addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                                addItem.Items.Add(new PageActionItem { Context = ContextList.Policy, Icon = Resources.Actions.Add_Icon, Title = string.Format("Add {0}", nextPolicyLevelName), Uri = string.Format("/form/AddPolicy?typeID={0}", id) });
                                list.Add(addItem);
                            }

                            reportNode = appendReportMenu(SystemObjects.PolicyType, id, SystemObjects.PolicyType, id);
                            if (reportNode != null) list.Add(reportNode);
                        }
                    }
                    else
                    {
                        if (id > 0)
                        {
                            policy = Company.GetById<Policy>(id, i => i.PolicyType.PolicyTypeLevels);
                            var levels = policy.PolicyType.PolicyTypeLevels.ToList(); //Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == policy.PolicyTypeID).ToList();
                            nextPolicyLevelName = (levels.Any(i => i.Level == policy.Level + 1)) ? levels.Single(i => i.Level == policy.Level + 1).Name : string.Format("{0} {1}", policy.PolicyType.Name.ToLower(), "model");
                            var rootLevelName = (levels.Any(i => i.Level == 1)) ? levels.Single(i => i.Level == 1).Name : string.Format("{0} {1}", policy.PolicyType.Name.ToLower(), "root model");
                            //list.Add(new PageActionItem { Context = ContextList.Intersect, Icon = Resources.Actions.ViewRelationships_Icon, Title = Resources.Actions.ViewRelationships, Uri = string.Format("/relations/RelationOverlay?type={0}&id={1}", type.ToString(), id) });
                            if (hasPermission(permissions, Claim.Create, ClaimObject.Root) || hasPermission(permissions, Claim.Create, ClaimObject.Governance))
                            {
                                addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                                if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                                {
                                    if (levels.Count > policy.Level)
                                        addItem.Items.Add(new PageActionItem { Context = ContextList.Policy, Icon = "cube", Title = string.Format("{0}", nextPolicyLevelName), Uri = string.Format("/form/AddPolicy?typeID={0}&parentID={1}", policy.PolicyTypeID, id) });

                                    addItem.Items.Add(new PageActionItem { Context = ContextList.Policy, Icon = "cube", Title = string.Format("{0}", rootLevelName), Uri = string.Format("/form/AddPolicy?typeID={0}", policy.PolicyTypeID) });
                                }
                                list.Add(addItem);
                            }
                        }
                    }

                    if (policy != null && context != "root")
                    {
                        following = Company.IsUserFollowing(type, id, null);

                        if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                        {
                            list.Add(new PageActionItem { Context = ContextList.Policy, Icon = Resources.Actions.Edit_Icon, Title = Resources.Actions.Edit, Uri = string.Format("/form/EditPolicy?id={0}", id) });
                        }
                        if (hasPermission(permissions, Claim.Delete, ClaimObject.Root))
                        {
                            list.Add(new PageActionItem { Context = ContextList.Policy, Icon = Resources.Actions.Delete_Icon, Title = Resources.Actions.Delete, Uri = string.Format("/form/DeletePolicy?id={0}", id) });
                        }
                        reportNode = appendReportMenu(type, id, SystemObjects.PolicyType, policy.PolicyTypeID, true);
                        if (reportNode != null) list.Add(reportNode);
                        list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = $"/resources/UpdateFollowStatus?type={type}&id={id}&includeChildren=true" });
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region Actions
                    following = Company.IsUserFollowing(type, id, null);
                    if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    {
                        if (context == "default")
                        {
                            list.Add(new PageActionItem { Context = "PolicyTypeClasses", Icon = "tags", Title = "Classifications", Uri = "/overlays/PolicyTypeClasses" });
                        }
                        else
                        {
                            addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                            if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                            {
                                addItem.Items.Add(new PageActionItem { Context = ContextList.Policy, Icon = "cube", Title = "Top-level policy", Uri = string.Format("/form/AddPolicy?typeID={0}", id) });
                                list.Add(addItem);
                            }
                        }
                    }
                    reportNode = appendReportMenu(type, id, SystemObjects.PolicyType, id);
                    if (reportNode != null) list.Add(reportNode);
                    list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = $"/resources/UpdateFollowStatus?type={type}&id={id}" });
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                #endregion
                case SystemObjects.Report:
                    #region Actions
                    //if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    //{
                    //    //addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                    //    if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    //    {
                    //        list.Add(new PageActionItem { Context = ContextList.Report, Icon = Resources.Actions.Add_Icon, Uri = "/form/AddReport" });
                    //        //if (id > 0)
                    //        //{
                    //        //    var report = Company.GetById<Report>(id);

                    //        //    if (report != null)
                    //        //    {
                    //        //        //addItem.Items.Add(new PageActionItem { Context = ContextList.ReportTile, Icon = "key", Title = "Tile", Uri = string.Format("/form/AddReportTile?reportID={0}", id) });
                    //        //        //if (loadType.LoadTypeFields.Count > 0)
                    //        //        //    addItem.Items.Add(new PageActionItem { Context = ContextList.Load, Icon = "key", Title = "Rule", Uri = string.Format("/form/AddLoadTypeRule?id={0}", id) });
                    //        //        //if (loadType.LoadTypeRules.Count > 0)
                    //        //        //    addItem.Items.Add(new PageActionItem { Context = ContextList.Load, Icon = "key", Title = "Upload spreadsheet", Uri = string.Format("/form/AddLoad?id={0}", id) });
                    //        //    }

                    //        //    report = null;
                    //        //}
                    //        list.Add(addItem);
                    //    }
                    //}
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.Responsibility:
                    break;
                case SystemObjects.ResponsibilityType:
                    #region Actions
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.ResponsibilityTypeClaim:
                    break;
                case SystemObjects.Resource:
                    #region Actions
                    following = Company.IsUserFollowing(type, id, null);
                    reportNode = appendReportMenu(type, id, SystemObjects.ResourceType, 1);
                    if (reportNode != null) list.Add(reportNode);
                    if (id == Company.CurrentResourceID)
                    {
                        list.Add(new PageActionItem { Context = ContextList.Resource, Icon = Resources.Actions.Edit_Icon, Title = "Edit My Info", Uri = "/form/resources/me/edit" });
                        list.Add(new PageActionItem { Context = ContextList.Resource, Icon = "key", Title = "View My API Credentials", Uri = "/overlays/MyApiCredentials" });
                        list.Add(new PageActionItem { Context = ContextList.Resource, Icon = "asterisk", Title = "Change password", Uri = "/form/resources/me/changepassword" });
                    }
                    else {
                        following = Company.IsUserFollowing(type, id, null);
                        followParent = Company.GetFollowingParent(type, id, null);
                        followingParent = (followParent != null);

                        if (!followingParent)
                            if (!following)
                                followText = Resources.Actions.Follow;
                            else
                                followText = Resources.Actions.Unfollow;
                        else
                        {
                            followText = "Following People";
                        }
                        list.Add(new PageActionItem { Context = followingParent ? "nullform" : "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = followText, Uri = followingParent ? "#" : string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id), Enabled = !followingParent ? true : false });                    
                    }
                    break;
                    #endregion
                case SystemObjects.ResourceType:
                    #region Actions
                    //if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    //{
                    //    if (id > 0)
                    //    {
                    //        list.Add(new PageActionItem { Context = ContextList.Resource, Icon = Resources.Actions.Add_Icon, Uri = string.Format("/form/resources/{0}/add", id) });
                    //    }
                    //}
                    list.Add(new PageActionItem { Context = "command", Icon = Resources.Actions.Follow_Icon, Uri = "#" });
                    break;
                    #endregion
                case SystemObjects.ResponseType:
                    #region Actions
                    if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    {
                        addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                        addItem.Items.Add(new PageActionItem { Context = ContextList.ResponseType, Icon = "list-alt", Title = "Type", Uri = "/form/responsetypes/add" });
                        if (id > 0)
                        {
                            addItem.Items.Add(new PageActionItem { Context = ContextList.ResponseTypeOption, Icon = "dot-circle-o", Title = "Option", Uri = string.Format("/form/responsetypes/{0}/add", id) });
                        }
                        list.Add(addItem);
                    }
                    break;
                    #endregion
                case SystemObjects.Rule:
                    #region Actions
                    if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                        list.Add(new PageActionItem { Context = ContextList.Rule, Icon = Resources.Actions.Add_Icon, Title = "Rule", Uri = "/form/AddRule" });

                    if (id > 0)
                    {
                        var rule = Company.GetById<Rule>(id);

                        if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                            list.Add(new PageActionItem { Context = ContextList.Rule, Icon = Resources.Actions.Edit_Icon, Title = Resources.Actions.Edit, Uri = string.Format("/form/EditRule?id={0}", id) });

                        if (hasPermission(permissions, Claim.Delete, ClaimObject.Root))
                            list.Add(new PageActionItem { Context = ContextList.Rule, Icon = Resources.Actions.Delete_Icon, Title = Resources.Actions.Delete, Uri = string.Format("/form/DeleteRule?id={0}", id) });

                        reportNode = appendReportMenu(type, id, SystemObjects.RuleType, (int)rule.RuleType, true);
                        if (reportNode != null) list.Add(reportNode);

                        following = Company.IsUserFollowing(type, id, null);
                        list.Add(new PageActionItem { Context = ContextList.ActionCommand, CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id) });
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.StatisticType:
                    #region Actions
                    //if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    //{
                    //    list.Add(new PageActionItem { Context = ContextList.StatisticType, Icon = Resources.Actions.Add_Icon, Uri = "/form/AddStatisticType" });
                    //}
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.SurveyType:
                    #region Actions
                    if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    {
                        addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                        addItem.Items.Add(new PageActionItem { Context = ContextList.SurveyType, Icon = "bar-chart-o", Title = "Survey", Uri = "/form/surveys/add" });
                        if (id > 0)
                        {
                            addItem.Items.Add(new PageActionItem { Context = ContextList.QuestionType, Icon = "question", Title = "Question", Uri = string.Format("/form/surveys/{0}/questions/add", id) });
                        }
                        list.Add(addItem);
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.Taxonomy:
                    #region Actions
                    Taxonomy taxonomy = null;

                    string nextLevelName = "model";
                    TaxonomyTypeLevel level = null;
                    if (context == "root")
                    {
                        var taxonomyType = Company.GetById<TaxonomyType>(id);
                        if (taxonomyType != null) {
                            level = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id && i.Level == 1).SingleOrDefault();
                            nextLevelName = (level != null) ? level.Name : string.Format("{0} {1}", taxonomyType.Name.ToLower(), "model");
                            if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                            {
                                addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                                addItem.Items.Add(new PageActionItem { Context = ContextList.Taxonomy, Icon = Resources.Actions.Add_Icon, Title = string.Format("Add {0}", nextLevelName), Uri = string.Format("/form/taxonomy/{0}/0/add", id) });
                                list.Add(addItem);
                            }

                            reportNode = appendReportMenu(SystemObjects.TaxonomyType, id, SystemObjects.TaxonomyType, id);
                            if (reportNode != null) list.Add(reportNode);
                        }
                    }
                    else
                    {
                        if (id > 0)
                        {
                            taxonomy = Company.GetById<Taxonomy>(id);
                            var levels = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == taxonomy.TaxonomyTypeID).ToList();
                            nextLevelName = (levels.Any(i => i.Level == taxonomy.Level + 1)) ? levels.Single(i => i.Level == taxonomy.Level + 1).Name : string.Format("{0} {1}", taxonomy.TaxonomyType.Name.ToLower(), "model");
                            var rootLevelName = (levels.Any(i => i.Level == 1)) ? levels.Single(i => i.Level == 1).Name : string.Format("{0} {1}", taxonomy.TaxonomyType.Name.ToLower(), "root model");
                            //list.Add(new PageActionItem { Context = ContextList.Intersect, Icon = Resources.Actions.ViewRelationships_Icon, Title = Resources.Actions.ViewRelationships, Uri = string.Format("/relations/RelationOverlay?type={0}&id={1}", type.ToString(), id) });
                            if (hasPermission(permissions, Claim.Create, ClaimObject.Root) || hasPermission(permissions, Claim.Create, ClaimObject.Governance))
                            {
                                addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                                if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                                { 
                                    if (levels.Count > taxonomy.Level)
                                        addItem.Items.Add(new PageActionItem { Context = ContextList.Taxonomy, Icon = "sitemap", Title = string.Format("{0}", nextLevelName), Uri = string.Format("/form/taxonomy/{0}/{1}/add", taxonomy.TaxonomyTypeID, id) });

                                    addItem.Items.Add(new PageActionItem { Context = ContextList.Taxonomy, Icon = "sitemap", Title = string.Format("{0}", rootLevelName), Uri = string.Format("/form/taxonomy/{0}/0/add", taxonomy.TaxonomyTypeID) });
                                }                                  
                                list.Add(addItem);
                            }

                            list.Add(
                                new PageActionItem
                                {
                                    Context = ContextList.ActionGenericReport,
                                    Icon = Resources.Actions.Diagram_Icon,
                                    Title = "Diagram",
                                    Uri = string.Format("/overlays/TaxonomyType/{0}/diagrams/catalog", taxonomy.TaxonomyTypeID)
                                }
                            );
                        }
                    }

                    if (taxonomy != null && context != "root")
                    {
                        following = Company.IsUserFollowing(type, id, null);
                        followParent = Company.GetFollowingParent(type, id, null);
                        followingParent = (followParent != null);
                        
                        if (!followingParent)
                            if (!following)
                                followText = Resources.Actions.Follow;
                            else
                                followText = Resources.Actions.Unfollow;
                        else
                        {
                            followText = "Following ";
                            var obj = Company.GetObjectDetail(followParent.ObjectType, followParent.ObjectID);
                            followText += obj.Name;
                        }

                        if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                            list.Add(new PageActionItem { Context = ContextList.Taxonomy, Icon = Resources.Actions.Edit_Icon, Title = Resources.Actions.Edit, Uri = string.Format("/form/taxonomy/{0}/{1}/edit", taxonomy.TaxonomyTypeID, id) });
                        if (hasPermission(permissions, Claim.Delete, ClaimObject.Root))
                            list.Add(new PageActionItem { Context = ContextList.Taxonomy, Icon = Resources.Actions.Delete_Icon, Title = Resources.Actions.Delete, Uri = string.Format("/form/taxonomy/{0}/{1}/delete", taxonomy.TaxonomyTypeID, id) });
                        //list.Add(new PageActionItem { Context = ContextList.ActionDiagram, Icon = "exchange", Title = "Lineage Diagram", Uri = string.Format("/parts/Taxonomy/{0}/lineage", id) });
                        reportNode = appendReportMenu(type, id, SystemObjects.TaxonomyType, taxonomy.TaxonomyTypeID, true);
                        if (reportNode != null) list.Add(reportNode);  

                        var followNode = new PageActionItem { Context = followingParent ?  "nullform" : "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon  : Resources.Actions.Follow_Icon, Title = followText, Uri = followingParent ? "#" : string.Format("/resources/UpdateFollowStatus?type={0}&id={1}&includeChildren=true", type, id), Enabled = !followingParent };
                        list.Add(followNode);


                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.TaxonomyType:
                    #region Actions
                    if (hasPermission(permissions, Claim.Create, ClaimObject.Root) || hasPermission(permissions, Claim.Create, ClaimObject.Governance))
                    {
                        //addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                       // if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                       //     list.Add(new PageActionItem { Context = ContextList.TaxonomyType, Icon = Resources.Actions.Add_Icon, Uri = "/form/catalogs/add" });
                        
                        if (id > 0)
                        {
                            //if (hasPermission(permissions, Claim.Create, ClaimObject.Governance))
                            //{
                            //    //addItem.Items.Add(new PageActionItem { Context = ContextList.ResponsibilityTypeClaim, Icon = "key", Title = Resources.Actions.AddClaim_Text, Uri = string.Format("/form/AddResponsibilityTypeObjectClaim?type={0}&id={1}", type.ToString(), id) });
                            //    loadResponsiblityTypeAddMenu(type, id, addItem, true);
                            //}
                            
                            //list.Add(addItem);

                            reportNode = appendReportMenu(type, id, SystemObjects.TaxonomyType, id);
                            if (reportNode != null) list.Add(reportNode);
                        }
                        else
                        {
                            //list.Add(addItem);
                            reportNode = appendReportMenu(type, 0, type, 0);
                            if (reportNode != null) list.Add(reportNode);
                        }

                        if (context == "default")
                        {
                            list.Add(new PageActionItem { Context = "TaxonomyTypeClasses", Icon = "tags", Title = "Classifications", Uri = "/overlays/TaxonomyTypeClasses" });
                        }
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                    #endregion
                case SystemObjects.WorkflowTypeRelation:
                    #region Actions
                    if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                    {
                        addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };

                        var workflows = WorkflowType.CertifyArtifact.GetWorkflowTypeEnumList();

                        foreach (var r in workflows)
                        {
                            var rItem = new PageActionItem { Context = "WorkflowTypeRelation", Icon = Resources.Actions.Governance_Icon, Title = string.Format("{0}", r.Name), Uri = string.Format("/form/AddWorkflowAllocation?workflowType={0}", r.ID) };
                            addItem.Items.Add(rItem);
                        }

                        list.Add(addItem);
                    }
                    if (id > 0)
                    {
                        list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    }
                    break;
                    #endregion
            }

            return list;
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
        public ArtifactModelRequest GetArtifact(int id)
        {
            var a = Company.GetById<Artifact>(id, i => i.ArtifactType);

            if (a == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            var model = new ArtifactModelRequest();

            //Static fields
            model.Add("ID", a.ID);
            model.Add("Name", a.Name);
            model.Add("Description", a.Description);
            model.Add("TypeName", a.ArtifactType.Name);
            model.Add("AllowRelatedArtifacts", a.ArtifactType.AllowRelatedArtifacts);
            model.Add("Status", a.Status);

            try
            {
                var row = Company.Query<dynamic>(QueryConstants.ArtifactSettingsItem, new { id = a.ArtifactTypeID }).Single();

                model.Add("AllowAttributes", (bool)row.AllowAttributes);
                model.Add("AllowSynonyms", (bool)row.AllowSynonyms);
                model.Add("AllowPredicateHierarchies", (bool)row.AllowPredicateHierarchies);
            }
            catch(Exception ex)
            { }

            var breadcrumbItems = Company.Query<dynamic>(QueryConstants.ArtifactBreadcrumbItem, new { id = id }).ToList();
            var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

            var breadcrumbs = new List<BreadcrumbItem>() {
                new BreadcrumbItem { Name = "Glossary" }
            };
            breadcrumbItems.ForEach(b =>
            {
                breadcrumbs.Add(new BreadcrumbItem { Name = pluralize.Pluralize((string)b.TypeName), Url = (string)b.TypeUrl });
                breadcrumbs.Add(new BreadcrumbItem { Name = HttpUtility.HtmlDecode((string)b.Name), Url = (string)b.Url });
            });
            pluralize = null;

            breadcrumbs.Last().Active = true;

            model.Add("Breadcrumbs", breadcrumbs);

            return model;
        }

        [Route("artifacts/{typeID:int}")]
        public ArtifactType GetArtifactType(int typeID)
        {
            var artifactType = Company.GetById<ArtifactType>(typeID);
            if (artifactType == null) throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            return artifactType;
        }

        [Route("artifacts/{id}/{take?}")]
        public ArtifactModelRequestList GetArtifacts(int id, int take = 10)
        {
            var list = new ArtifactModelRequestList();

            string prefix = "";
            var qs = Request.GetQueryNameValuePairs();
            if (qs.Any(i => i.Key == "prefix"))
            {
                prefix = qs.Single(i => i.Key == "prefix").Value;
            }

            var items = Company.Filter<Artifact>(i => i.ArtifactTypeID == id).OrderBy(i => i.Name).AsQueryable();
            if (!string.IsNullOrEmpty(prefix)) items = items.Where(i => i.Name.StartsWith(prefix));
            var lItems = items.Take(take).ToList();

            var IDs = lItems.Select(i => i.ID).ToList();

            var sType = SystemObjects.Artifact.ToString();
            var values = Company.Filter<FieldWithRelation>(i => i.ObjectType == sType && IDs.Contains(i.ObjectID)).ToList();

            foreach (var item in lItems)
            {
                var listItem = new ArtifactModelRequest();

                //Static fields
                listItem.Add("ID", item.ID);
                listItem.Add("Name", item.Name);
                listItem.Add("Description", item.Description);
                listItem.Add("Status", item.Status);

                // Dynamic fields
                foreach (var f in values.Where(i => i.ObjectID == item.ID).OrderBy(i => i.Name))
                { 
                    listItem.Add(f.Name, f.FormattedValue);
                };

                // Add to list
                list.Add(listItem);
            }

            return list;//.AsQueryable();
        }

        #endregion

        #region Domains

        [Route("domains")]
        public IQueryable<DomainType> GetDomainsListTypes()
        {
            return Company.Table<DomainType>().AsQueryable();
        }

        [Route("domains/{id:int}")]
        public HttpResponseMessage GetDomainType(int id)
        {
            var a = Company.GetById<DomainType>(id);

            if (a == null)
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            var row = Company.Query<dynamic>(QueryConstants.DomainSettingsItem, new { id = id }).Single();

            return Request.CreateResponse<dynamic>(
                new Dictionary<string, object> {
                    { "ID", a.ID },
                    { "Name", a.Name },
                    { "Description", a.Description },
                    { "AllowAttributes", (bool)row.AllowAttributes }
                }
            );
        }

        [Route("domains/{typeID:int}/all")]
        public IQueryable<Domain> GetDomainsByType(int typeID)
        {
            return Company.Filter<Domain>(i => i.DomainTypeID == typeID, i => i.DomainGroup);
        }

        [Route("domains/{typeID:int}/{id:int}")]
        public Domain GetDomain(int typeID, int id)
        {
            var model = Company.GetById<Domain>(id, i => i.DomainGroup, i => i.DomainType);

            if (model == null)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }
            else 
            {
                if (model.DomainTypeID != typeID)
                    throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            return model;
        }

        [Route("domains/{typeID:int}/{id:int}/allocations")]
        public IQueryable<DomainAllocationDetail> GetDomainAllocations(int typeID, int id)
        {
            var sId = id.ToString();
            return Company.Filter<DomainAllocationDetail>(i => i.DomainID == sId);
        }

        [Route("domains/{typeID:int}/{id:int}/all")]
        public IQueryable<DomainItem> GetDomainItemsByType(int typeID, int id)
        {
            return Company.Filter<DomainItem>(i => i.DomainID == id);
        }

        [Route("domains/{typeID:int}/{listID:int}/{id:int}")]
        public DomainItem GetDomainItem(int typeID, int listID, int id)
        {
            var model = Company.GetById<DomainItem>(id, i=> i.Domain.DomainType);

            if (model == null)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }
            else
            {
                if (model.DomainID != listID)
                    throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            return model;
        }

        #endregion

        #region Events

        [Route("{type}/{id:int}/events/headers")]
        public IQueryable<OverlayEventHeader> GetEventTileHeaders(SystemObjects type, int id)
        {
            return Company.GetEventHeadersByObject(type, id);
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
            
            while(itemID > 0)
            {
                var currentItem = Company.Query<dynamic>(
                    QueryConstants.FusionBreadcrumbItem, 
                    new { item = itemID }
                ).FirstOrDefault();

                if (currentItem == null) throw new Exception("invalid item id specified to generate breadcrumb from");
                
                itemID = currentItem.parentID ?? default(int);
                itemPathData.Insert(0,currentItem);
            }

            return Request.CreateResponse(HttpStatusCode.OK, itemPathData);
        }
        
        [Route("fusion/ownership/ChildAttributeNodes"), HttpGet]
        public HttpResponseMessage GetOwnershipChildAttributeNodes(int fusionID, int targetFusionAttributeTypeID, int ruleID, int currentFusionAttributeTypeID = 0, int fusionAttributeID = 0)
        {
            var models = Company.Query<dynamic>(
                QueryConstants.FusionOwnershipChildAttributeNodeList, 
                new {
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
                new {
                    fusionID,
                    targetFusionAttributeTypeID,
                    ruleID,
                    currentFusionAttributeTypeID,
                    fusionAttributeID
                }
            );

            return Request.CreateResponse(HttpStatusCode.OK, models);
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

         [Route("fusion/{typeID:int}/ownership/relationshiptypes")]
        public List<IntersectType> GetAllowedIntersectTypesForFusionOwnership(int typeID)
        {
            return Company.Filter<IntersectType>(i => !i.Nodes.Any(n => n.ObjectType == "IntersectType")).OrderBy(i => i.Name).ToList();
        }

        #region Owner

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/ownership/options")]
         public List<FusionOwnerOption> GetFusionOwnerOptions(int typeID, int fusionID) //intersectTypeID
        {
            return Company.GetFusionOwnerOptions();// (intersectTypeID);
        }

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/ownership")]
        public IQueryable<FusionAttributeOwnerDetail> GetFusionAttributeOwnerDetails(int typeID, int fusionID)
        {
            return Company.Filter<FusionAttributeOwnerDetail>(i => i.FusionID == fusionID);
        }

        #endregion

        #region Promotion

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/promotion/options")]
        public List<FusionPromotionOption> GetFusionPromotionOptions(int typeID, int fusionID)
        {
            return Company.GetFusionPromotionOptions();
        }

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/promotion")]
        public IQueryable<FusionAttributePromotionDetail> GetFusionAttributePromotionDetails(int typeID, int fusionID)
        {
            return Company.Filter<FusionAttributePromotionDetail>(i => i.FusionID == fusionID);
        }

        [Route("fusion/{id:int}/OwnershipRuleItems")]
        public HttpResponseMessage GetFusionAttributeOwnershipRuleItems(int id)
        {
            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company.Query<dynamic>(QueryConstants.FusionOwnershipRuleList, new { id })
            );
        }
        
        [Route("fusion/{id:int}/PromotionRuleItems")]
        public HttpResponseMessage GetFusionAttributePromotionRuleItems(int id)
        {
            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company.Query<dynamic>(QueryConstants.FusionPromotionRuleList, new { id })
            );
        }

        [Route("fusion/{id:int}/PromotionRuleMappings")]
        public HttpResponseMessage GetFusionAttributePromotionRuleMappings(int id)
        {
            return Request.CreateResponse(
                HttpStatusCode.OK,
                Company.Query<dynamic>(QueryConstants.FusionPromotionRuleMappingList, new { id })
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
                new { id }
                )
                .OrderBy(i => i.LastName)
                .ThenBy(i => i.FirstName)
                .AsQueryable();
        }

        #endregion

        #region IntersectType

        [HttpGet, Route("IntersectTypePredicates/{id:int}")]
        public IQueryable<Predicate> GetAllocatedPredicates(int id)
        {
            var allocations = Company.Filter<IntersectTypePredicate>(p => p.IntersectTypeID == id);
            return Company.Filter<Predicate>(p => allocations.Select(a => a.PredicateType).Distinct().ToList().Contains(p.Type));
        }

        [HttpGet, Route("IntersectTypePredicates/{id:int}/available")]
        public IQueryable<Predicate> GetAvailablePredicates(int id)
        {
            var allocated = GetAllocatedPredicates(id).ToList();

            var availableTypes = new List<int>();

            availableTypes.Add((int)MapType.Lineage);
            availableTypes.Add((int)MapType.ParentChildHierarchy);

            var intersectType = Company.GetById<IntersectType>(id);
            if (intersectType == null)
            {
                return null;
            }
            var nodes = intersectType.Nodes.ToList();

            if (nodes.Count >= 2)
            {
                if (nodes[0].ObjectType == nodes[1].ObjectType && nodes[0].ObjectID == nodes[1].ObjectID)
                {
                    availableTypes.Add((int)MapType.TypeHierarchy);
                    availableTypes.Add((int)MapType.GroupHierarchy);
                }
            }

            var predicates = Company.Filter<Predicate>(p => availableTypes.Contains((int)p.Type));
            var allocatedIDs = allocated.Select(a => a.ID).Distinct().ToList();

            var availablePredicates = predicates.Where(p => !allocatedIDs.Contains(p.ID));

            return availablePredicates;
        }
        #endregion

        #region Loads

        [HttpGet, Route("loads")]
        public IEnumerable<LoadDetail> GetLoads()
        {
            return Company.GetLoadDetails();
        }

        [HttpGet, Route("loads/{id:int}")]
        public LoadDetail GetLoad(int id)
        {
            return Company.GetLoadDetail(id);
        }

        [HttpGet, Route("loads/{id:int}/columns")]
        public IEnumerable<dynamic> GetLoadColumns(int id)
        {
            return Company.GetLoadColumnDetails(id);
        }

        [HttpGet, Route("loads/{id:int}/items")]
        public IEnumerable<dynamic> GetLoadItems(int id)
        {
            return Company.GetLoadItemDetails(id);
        }

        #endregion

        #region Lookup Methods

//        [Route("allitems")]
//        public IEnumerable<dynamic> GetAllItems()
//        {
//            return Company.Query<dynamic>(@"select		[Object], 
//			ObjectID, 
//			ObjectTypeName + ': ' + TextPath as Name
//from		cache.ObjectDetails 
//where		[Object] in ('Artifact', 'Rule', 'Policy', 'Domain') 
//			and ObjectID <> 0
//order by	Name");
//        }

        [Route("AttributeTypeCategories")]
        public IQueryable<AttributeTypeCategory> GetAttributeTypeCategories()
        {
            return Company.Table<AttributeTypeCategory>();
        }

        //        [Route("ResponsibilityTypeHierarchies")]
        //        public HttpResponseMessage GetResponsibilityTypeHierarchies()
        //        {
        //            var models = Company.Query<ResponsibilityTypeHierarchy>(
        //@"select	H.ID as StartID,
        //		    S.Name as StartName,
        //		    H.ParentID as EndID,
        //		    T.Name as EndName
        //from	    ResponsibilityTypeHierarchy H
        //		    inner join ResponsibilityType S on S.ID = H.ID
        //		    left join ResponsibilityType T on T.ID = H.ParentID");
        //            return Request.CreateResponse(HttpStatusCode.OK, models);
        //        }

        [Route("lookups/{id:int}/allocations")]
        public IQueryable<LookupAllocation> GetAllocationsByLookupType(int id)
        {
            return Company.Filter<LookupAllocation>(i => i.LookupObjectType == "Lookup" && i.LookupTypeID == id);
        }

        [Route("PolicyTypeClasses")]
        public IQueryable<PolicyTypeClass> GetPolicyTypeClasses()
        {
            return Company.Table<PolicyTypeClass>();
        }

        [Route("TaxonomyTypeClasses")]
        public IQueryable<TaxonomyTypeClass> GetTaxonomyTypeClasses()
        {
            return Company.Table<TaxonomyTypeClass>();
        }

        #endregion

        #region Fusion Lookup Fields

        private List<DetailReadOnlyRowModel> RenderFusionLookupField(FieldWithRelation k)
        {
            var list = new List<DetailReadOnlyRowModel>();

            //load the definition of the field from the [FieldTypeFusionLookupDefinition] table
            int fusionAttributeID = int.Parse(k.Value);
            var def = Company.Filter<FieldTypeFusionLookupDefinition>(x => x.FieldTypeID == k.FieldTypeID).FirstOrDefault();

            var sql =string.Empty;

            switch (def.ReferenceType)
            {
                case 1: //Self Reference
                case 2: //Parent Reference
                case 3: //Child Reference
                case 4: //Relationship Reference
                    list.Add(new DetailReadOnlyRowModel {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField {
                                    Column = 1,
                                    Name = k.FriendlyName,
                                    FieldDescription = k.DisplayDescription,
                                    FieldName = k.Name,
                                    HideHeader = def.HideHeader,
                                    HideFooter = def.HideFooter,
                                    LookupGridUrl = $"/api/FusionLookupField/{fusionAttributeID}/{def.ID}/values"
                                }
                            },
                            Category = k.Category
                    });
                    break;
            }

            return list;
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

            if (displayFields.Any(i => i.FieldTypeName == "Name"))
            {
                gridFields.Add(new GridField { name = "Name", type = "string" });
                columns.Add(new GridColumn { text = "Name", datafield = "Name", width = "auto" });
            }
            if (displayFields.Any(i => i.FieldTypeName == "TextPath"))
            {
                gridFields.Add(new GridField { name = "TextPath", type = "string" });
                columns.Add(new GridColumn { text = "Path", datafield = "TextPath", width = "auto" });
            }
            if (fieldTypeIDs != null)
            {
                var fieldTypeInfo = Company.Filter<FieldType>(i => fieldTypeIDs.Contains(i.ID)).ToList();
                foreach (var fieldType in fieldTypeInfo)
                {
                    gridFields.Add(new GridField { name = fieldType.Name, type = "string" });
                    columns.Add(new GridColumn { text = fieldType.FriendlyName, datafield = fieldType.Name, width = "auto" });
                    sqlColumns.Add($"F{fieldType.ID}.FormattedValue as {fieldType.Name}");
                    sqlJoins.Add($"left join Field F{fieldType.ID} on F{fieldType.ID}.FieldTypeID = {fieldType.ID} and F{fieldType.ID}.ObjectType = 'FusionAttribute' and F{fieldType.ID}.ObjectID = A.ID");
                }
            }
            gridFields.Add(new GridField { name = "Object", type = "string" });
            gridFields.Add(new GridField { name = "Url", type = "string" });
            gridFields.Add(new GridField { name = "ID", type = "number" });

            #endregion

            #region Calculate SQL statement

            string sql = string.Empty;
            string sqlColumnString = string.Join(",", sqlColumns);
            if (!string.IsNullOrEmpty(sqlColumnString)) sqlColumnString = "," + sqlColumnString;
            string sqlJoinString = string.Join(" ", sqlJoins);

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
        [dbo].GenerateObjectUrl('FusionAttribute', A.FusionAttributeTypeID, A.ID) as Url
        {sqlColumnString}
from    FusionAttribute A
        {sqlJoinString}
where   A.ID = {sourceFusionAttributeID}";
                    break;
                case 2: //Parent Reference
                    sql = $@"
select  A.ID,
	    A.ParentID,
	    A.Name,
	    A.TextPath,
	    A.SourceID,
        'FusionAttribute' as Object,
        [dbo].GenerateObjectUrl('FusionAttribute', A.FusionAttributeTypeID, A.ID) as Url
        {sqlColumnString}
from    FusionAttribute c
        inner join FusionAttribute A on c.ID = {sourceFusionAttributeID} and A.ID = c.ParentID and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}
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
        [dbo].GenerateObjectUrl('FusionAttribute', A.FusionAttributeTypeID, A.ID) as Url
        {sqlColumnString}
from    FusionAttribute A
        {sqlJoinString}
where   A.ParentID = {sourceFusionAttributeID}
        and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}";

                    break;
                default: //Relationship Reference
                    sql = $@"
select  A.ID,
        A.ParentID,
        A.name,
        A.TextPath,
        A.SourceID,
        'FusionAttribute' as Object,
        [dbo].GenerateObjectUrl('FusionAttribute', A.FusionAttributeTypeID, A.ID) as Url
        {sqlColumnString}
from    IntersectNode S
        inner join IntersectNode T on S.IntersectID = T.IntersectID and T.ID <> S.ID and S.ObjectType = 'FusionAttribute' and S.ObjectID = {sourceFusionAttributeID} and T.ObjectType = 'FusionAttribute'
        inner join [fusionattribute] A on A.ID = T.ObjectID and A.FusionAttributeTypeID = {def.TargetFusionAttributeTypeID}
        {sqlJoinString}";

                    break;
            }

            #endregion

            results = Company.Query<dynamic>(sql);

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = results,
                Columns = columns,
                Fields = gridFields
            });
        }

        #endregion

        #region Relation Lookup Fields

        private List<DetailReadOnlyRowModel> RenderRelationLookupField(string type, int id, int fieldTypeID)
        {
            var list = new List<DetailReadOnlyRowModel>();

            var ft = Company.GetById<FieldType>(fieldTypeID, i => i.FieldTypeRelationLookupDefinitions);

            if (ft.FieldTypeRelationLookupDefinitions != null)
            {
                if (ft.FieldTypeRelationLookupDefinitions.Count > 0)
                {
                    var def = ft.FieldTypeRelationLookupDefinitions.First();
                    switch (def.ReferenceType)
                    {
                        case 1: //Self Reference
                        case 2: //Child Reference
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
                                        LookupGridUrl = $"/api/RelationLookupField/{type}/{id}/{def.ID}/values"
                                    }
                                },
                                Category = ft.Category
                            });
                            break;
                    }
                }
            }

            return list;
        }

        [Route("RelationLookupField/{type}/{id:int}/{definitionID:int}/values")]
        public HttpResponseMessage GetRelationLookupGridField(string type, int id, int definitionID)
        {
            var gridFields = new List<GridField>();
            var columns = new List<GridColumn>();
            IEnumerable<dynamic> results = null;

            try
            {
                var def = Company.GetById<FieldTypeRelationLookupDefinition>(definitionID, i => i.FieldTypeRelationLookupDisplayFields);
                if (def == null) throw new Exception("Invalid fusion lookup field is specified");

                var displayFields = def.FieldTypeRelationLookupDisplayFields.ToList();
                var fieldTypeIDs = displayFields.Where(i => i.FieldTypeID != 0).Select(x => x.FieldTypeID).ToList();
                var fieldTypes = Company.Filter<FieldType>(i => fieldTypeIDs.Contains(i.ID)).ToList();

                var sqlColumns = new List<string>();
                var sqlJoins = new List<string>();

                #region Load Columns/Fields

                if (displayFields.Any(i => i.FieldTypeName == "Name"))
                {
                    gridFields.Add(new GridField { name = "Name", type = "string" });
                    columns.Add(new GridColumn { text = "Name", datafield = "Name", width = "auto" });
                }
                if (displayFields.Any(i => i.FieldTypeName == "TextPath"))
                {
                    gridFields.Add(new GridField { name = "TextPath", type = "string" });
                    columns.Add(new GridColumn { text = "Path", datafield = "TextPath", width = "auto" });
                }
                if (fieldTypeIDs != null)
                {
                    var fieldTypeInfo = Company.Filter<FieldType>(i => fieldTypeIDs.Contains(i.ID)).ToList();
                    foreach (var fieldType in fieldTypeInfo)
                    {
                        gridFields.Add(new GridField { name = fieldType.Name, type = "string" });
                        columns.Add(new GridColumn { text = fieldType.FriendlyName, datafield = fieldType.Name, width = "auto" });
                        sqlColumns.Add($"F{fieldType.ID}.FormattedValue as {fieldType.Name}");

                        if (displayFields.First(i => i.FieldTypeID == fieldType.ID).FieldTypeName.Contains("Relation."))
                        {
                            sqlJoins.Add($"left join Field F{fieldType.ID} on F{fieldType.ID}.FieldTypeID = {fieldType.ID} and F{fieldType.ID}.ObjectType = 'Intersect' and F{fieldType.ID}.ObjectID = R.IntersectID");
                        }
                        else
                        {
                            sqlJoins.Add($"left join Field F{fieldType.ID} on F{fieldType.ID}.FieldTypeID = {fieldType.ID} and F{fieldType.ID}.ObjectType = R.TargetObject and F{fieldType.ID}.ObjectID = R.TargetObjectID");
                        }
                    }
                }
                gridFields.Add(new GridField { name = "Object", type = "string" });
                gridFields.Add(new GridField { name = "Url", type = "string" });
                gridFields.Add(new GridField { name = "ID", type = "number" });

                #endregion

                #region Calculate SQL statement

                string sql = string.Empty;
                string sqlColumnString = string.Join(",", sqlColumns);
                if (!string.IsNullOrEmpty(sqlColumnString)) sqlColumnString = "," + sqlColumnString;
                string sqlJoinString = string.Join(" ", sqlJoins);

                switch (def.ReferenceType)
                {
                    case 1: //Self Reference
                        sql = $@"
    select  R.IntersectID,
            D.[Object],
		    D.ObjectID,
            D.ObjectID as ID,
		    D.Name,
		    D.[TextPath],
		    D.Url 
            {sqlColumnString}
    from    cache.Relationship R
		    inner join [Intersect] I on I.ID = R.IntersectID AND I.IntersectTypeID = {def.IntersectTypeID} and R.SourceObject = '{type}' and R.SourceObjectID = {id}
		    inner join [cache].[ObjectDetails] D on D.[Object] = R.TargetObject and D.ObjectID = R.TargetObjectID
            {sqlJoinString}";
                        break;
                    default: //Child Reference
                        sql = $@"
    select  R.IntersectID,
            D2.[Object],
		    D2.ObjectID,
            D2.ObjectID as ID,
		    D2.Name,
		    D2.[TextPath],
		    D2.Url
            {sqlColumnString}
    from    cache.Relationship R1
		    inner join [Intersect] I1 on I1.ID = R1.IntersectID AND I1.IntersectTypeID = {def.IntersectTypeID} and R1.SourceObject = '{type}' and R1.SourceObjectID = {id}
		    inner join [cache].[ObjectDetails] D1 on D1.[Object] = R1.TargetObject and D1.ObjectID = R1.TargetObjectID
		    inner join cache.Relationship R ON R.SourceObject = 'Intersect' and R.SourceObjectID = I1.ID
		    inner join [Intersect] I2 on I2.ID = R.IntersectID and I2.IntersectTypeID = {def.ChildIntersectTypeID}
		    inner join [cache].[ObjectDetails] D2 on D2.[Object] = R.TargetObject and D2.ObjectID = R.TargetObjectID
            {sqlJoinString}";
                        break;
                }

                #endregion

                results = Company.Query<dynamic>(sql);
            }
            catch (Exception ex)
            {

            }

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = results,//values,
                Columns = columns,
                Fields = gridFields
            });
        }

        #endregion

        #region Relationships

        [HttpGet, Route("RelationshipObjectsByType")]
        public List<FilterObjectItem> RelationshipObjectsByType(SystemObjects type, int id)//, SystemObjects targetObject)
        {
            var sql = "";

            switch (type)
            { 
                case SystemObjects.ArtifactType:
                    sql = @"select TextPath as Name, ID, 'Artifact' as [Type] from Artifact where ArtifactTypeID = @id and ID in (select ObjectID from IntersectNode where ObjectType = 'Artifact') order by TextPath";
                    break;
                case SystemObjects.DomainType:
                    sql = @"select Name, ID, 'Domain' as [Type] from Domain where DomainTypeID = @id and ID in (select ObjectID from IntersectNode where ObjectType = 'Domain') order by Name";
                    break;
                case SystemObjects.FusionAttributeType:
                    sql = @"select TextPath as Name, ID, 'FusionAttribute' as [Type] from FusionAttribute where FusionAttributeTypeID = @id and ID in (select ObjectID from IntersectNode where ObjectType = 'FusionAttribute') order by TextPath";
                    break;
                case SystemObjects.IntersectType:
                    sql = @"select Name, ID, 'Intersect' as [Type] from [Intersect] where IntersectTypeID = @id and ID in (select ObjectID from IntersectNode where ObjectType = 'Intersect') order by Name";
                    break;
                case SystemObjects.Policy:
                    sql = @"select TextPath as Name, ID, 'Policy' as [Type] from [Policy] where ID in (select ObjectID from IntersectNode where ObjectType = 'Policy') order by TextPath";
                    break;
                case SystemObjects.ResourceType:
                    sql = @"select LastName + ', ' + FirstName as Name, ResourceID as ID, 'Resource' as [Type] from reporting.Global_Resource where ResourceID in (select ObjectID from IntersectNode where ObjectType = 'Resource') order by LastName, FirstName";
                    break;
                case SystemObjects.Rule:
                    sql = @"select TextPath as Name, ID, 'Rule' as [Type] from [Rule] where ID in (select ObjectID from IntersectNode where ObjectType = 'Rule') order by TextPath";
                    break;
                case SystemObjects.TaxonomyType:
                    sql = @"select TextPath as Name, ID, 'Taxonomy' as [Type] from Taxonomy where TaxonomyTypeID = @id and ID in (select ObjectID from IntersectNode where ObjectType = 'Taxonomy') order by TextPath";
                    break;
                default:
                    sql = "";
                    break;
            }

            return Company.Query<FilterObjectItem>(sql, new { id = id }).ToList();
        }

        /// <summary>
        /// Gets a list of available relationships types based on the source type specified in parameters. 
        /// Used in the Filter By Relationship tile on artifact list pages.
        /// </summary>
        [Route("{type}/{id:int}/relationshiptypes")]
        public List<AllowedIntersectionType> GetRelationshipTypes(SystemObjects type, int id)//List<GetRelationshipModel>
        {
            return Company.GetAllowedIntersectionTypes(type.ToString(), id);
        }

        [Route("{type}/{id:int}/relations")]
        public IQueryable<Relationship> GetRelationships(SystemObjects type, int id)//List<GetRelationshipModel>
        {
            var sType = type.ToString();
            return Company.Filter<Relationship>(i => i.SourceObjectType == sType && i.SourceObjectID == id).OrderBy(i => i.TargetTypeName).ThenBy(i => i.TargetName).AsQueryable();
            //return Company.GetRelationships(type, id);
        }

        //[Route("{type}/{id:int}/relations/{intersectID:int}/items")]
        //public List<GetRelationshipModel> GetChildRelationships(SystemObjects type, int id, int intersectID)
        //{
        //    var list = Company.GetRelationships(SystemObjects.Intersect, intersectID);
        //    return list;
        //}

        [Route("{type}/{id:int}/relations/critical")]
        public IQueryable<CriticalRelationshipsByObject> GetCriticalRelations(SystemObjects type, int id)
        {
            return Company.GetCriticalRelationshipsByObject(type, id);
        }

        [Route("{type}/{id:int}/relationships/{targetType}/{targetID:int}/{intersectTypeID:int}/{criticalOnly:bool=false?}"), HttpGet]
        public IEnumerable<dynamic> RelationshipsForObjectByTargetType(SystemObjects type, int id, SystemObjects targetType, int targetID, int intersectTypeID, bool criticalOnly)
        { 
            var sType = type.ToString();
            var tType = targetType.ToString();

            var joins = "";
            var columns = "";
            //var whereClause = "";
            getDynamicFieldJoinStatements(intersectTypeID, "Intersect", out joins, out columns);

            var querySql = $@"
select  {columns} 
        A.*
from	(
        select  IntersectID as ID, 
                * 
        from    Relationship 
        where   SourceObjectType = '{type.ToString()}' 
                and SourceObjectID = {id}
                and TargetType = '{targetType.ToString()}' 
                and TargetTypeID = {targetID}
        ) A {joins}";

            if (criticalOnly)
                querySql += $" where A.Classification = {(int)IntersectClassification.Critical}";

            querySql += " order by A.TargetName";

            return Company.Query<dynamic>(querySql);
        }

        public class RawSourceRuleItem
        {
            public int IntersectMapID { get; set; }
            public int SourceRuleID { get; set; }
            public string Name { get; set; }
            public string SourceObject { get; set; }
            public int SourceObjectID { get; set; }
            public string SourceObjectName { get; set; }
            public string SourceTypeName { get; set; }
            public string Description { get; set; }
            public string RuleContexts { get; set; }
            public string ItemContexts { get; set; }
            public int SortOrder { get; set; }
        }

        public class SourceRulesViewModel
        {
            public List<SourceRuleViewModel> Rules { get; set; }
        }

        public class SourceRuleViewModel
        {
            public int SourceRuleID { get; set; }
            public string Name { get; set; }
            public string RuleContexts { get; set; }
            public List<SourceRuleItemViewModel> Items { get; set; }
        }

        public class SourceRuleItemViewModel
        {
            public int IntersectMapID { get; set; }
            public string SourceObject { get; set; }
            public int SourceObjectID { get; set; }
            public string SourceObjectName { get; set; }
            public string SourceTypeName { get; set; }
            public string Description { get; set; }
            public string ItemContexts { get; set; }
            public int SortOrder { get; set; }
        }

        [Route("{focal}/{focalID:int}/sources/{obj}/{objID:int}/rules")]
        public SourceRulesViewModel GetSourceRules(string focal, int focalID, string obj, int objID)
        {
            var rules = new SourceRulesViewModel { Rules = new List<SourceRuleViewModel>() };
            var rawItems = Company.Query<RawSourceRuleItem>(QueryConstants.SourceRuleList, new { focal = new Dapper.DbString { Value = focal, IsAnsi = true }, focalID, obj = new Dapper.DbString { Value = obj, IsAnsi = true }, objID }).OrderBy(i => i.Name).ThenBy(i => i.SortOrder).ToList();

            rawItems.Select(r => new { r.Name, r.SourceRuleID, r.RuleContexts }).Distinct().ToList().ForEach(r => {
                var ruleModel = new SourceRuleViewModel { Name = r.Name, SourceRuleID = r.SourceRuleID, RuleContexts = r.RuleContexts, Items = new List<SourceRuleItemViewModel>() };
                rawItems.Where(i => i.SourceRuleID == r.SourceRuleID).OrderBy(i => i.SortOrder).ToList().ForEach(i =>
                {
                    ruleModel.Items.Add(new SourceRuleItemViewModel {
                        Description = i.Description,
                        IntersectMapID = i.IntersectMapID,
                        ItemContexts = i.ItemContexts,
                        SortOrder = i.SortOrder,
                        SourceObject = i.SourceObject,
                        SourceObjectID = i.SourceObjectID,
                        SourceObjectName = i.SourceObjectName,
                        SourceTypeName = i.SourceTypeName
                    });
                });
                rules.Rules.Add(ruleModel);
            });

            return rules;
        }

        [HttpGet, Route("{focal}/{focalID:int}/{source}/{sourceID:int}/{target}/{targetID:int}/rules")]
        public SourceRulesViewModel GetSourceRulesForRelationship(string focal, int focalID, string source, int sourceID, string target, int targetID)
        {
            var model = GetSourceRules(focal, focalID, target, targetID);
            model.Rules = model.Rules.Where(r => r.Items.Count(i => i.SourceObject == source && i.SourceObjectID == sourceID) > 0).ToList();
            return model;
        }

        private List<int> LoadAttributes(int intersectTypeID)
        {
            return Company.Filter<AttributeTypeRelation>(i => i.ObjectType == "IntersectType" && i.ObjectID == intersectTypeID).Select(i => i.AttributeTypeID).ToList();
        }

        [Route("{type}/{id:int}/relationshipsAndAttributes/{targetType}/{targetID:int}/{criticalOnly:bool=false?}"), HttpGet]
        public List<RelationAttributeValue> GetRelationshipsAndAttributesForObjectByTargetType(SystemObjects type, int id, SystemObjects targetType, int targetID, bool criticalOnly, int intersectTypeID)
        {
            //get list of relationships
            var sType = type.ToString();
            var tType = targetType.ToString();
            var rels = Company.Filter<Relationship>(i => i.SourceObjectType == sType && i.SourceObjectID == id && i.TargetType == tType && i.TargetTypeID == targetID && ((i.Classification == IntersectClassification.Critical && criticalOnly) || !criticalOnly));

            //get list of attributes
            List<int> attributesList = LoadAttributes(intersectTypeID);

            //build a list of object ids so we dont make tons of queries
            var targetIDList = rels.Select(i => i.IntersectID).ToList();
            
            List<RelationAttributeValue> results = Company.Filter<AttributeDetail>(i => targetIDList.Contains(i.ObjectID) && attributesList.Contains(i.AttributeTypeID))
                    .Select(t => new RelationAttributeValue { AttributeTypeID = t.AttributeTypeID, Name = t.Name, Value = t.FormattedValue, TargetID = t.ObjectID }).OrderBy(t=>t.TargetID).ToList();

            return results;
        }

        [Route("{type}/{id:int}/relationships"), HttpPost]
        public HttpResponseMessage AddRelationships(SystemObjects type, int id, AddRelationshipsModel model)
        {
            HttpResponseMessage msg = null;

            try
            {
                Company.AddRelationships(type, id, model.Classification, model.Role, model.Description, model.Targets);
                msg = Request.CreateResponse<string>(HttpStatusCode.Created, "Relationships added successfully.");
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Cannot insert the value NULL into column 'IntersectID'"))
                {
                    msg = Request.CreateErrorResponse(HttpStatusCode.Conflict, string.Format("You do not yet have a relationship type defined for between {0} and {1}.", type.ToString(), model.Targets.First().ObjectType.ToString()), ex);
                }
                else
                {
                    msg = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message.Replace(System.Environment.NewLine, " "), ex);
                }
            }
            catch (Exception ex)
            {
                msg = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message.Replace(System.Environment.NewLine, " "), ex);
            }

            return msg;
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

        [Route("relationships/{id:int}"), HttpPut]
        public HttpResponseMessage EditRelationship(int id, EditRelationshipModel model)
        {
            HttpResponseMessage msg = null;

            try
            {
                Company.EditRelationship(id, model.Role, model.Classification, model.Description);
                msg = Request.CreateResponse<string>(HttpStatusCode.Created, "Relationships updated successfully.");
            }
            catch (SqlException ex)
            {
                msg = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message.Replace(System.Environment.NewLine, " "), ex);
            }
            catch (Exception ex)
            {
                msg = Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message.Replace(System.Environment.NewLine, " "), ex);
            }

            return msg;
        }
       
        #endregion

        #region Template Logic

        [Route("templates/email")]
        public List<EmailTemplate> GetEmailTemplates()
        {
            return Company.Table<EmailTemplate>().OrderBy(i => i.Name).ToList();
        }

        [Route("templates/tooltip")]
        public List<TooltipTemplate> GetTooltipTemplates()
        {
            return Company.Table<TooltipTemplate>().OrderBy(i => i.Name).ToList();
        }

        #endregion

        #region Governance/Ownership/Responsibility

        [Route("groups/{id:int}/ownership")]
        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByGroup(int id)
        {
            return Company.GetResponsibilitiesByResource(SystemObjects.Group, id);
        }

        [Route("resources/{id:int}/ownership")]
        public IQueryable<ResponsibilityDetailForResource> GetResponsibilitiesByResource(int id)
        {
            return Company.Filter<ResponsibilityDetailForResource>(i => i.ResponsibleObjectType == "Resource" && i.ResponsibleObjectID == id);
        }

        [Route("resources/{resourceID:int}/ownership/{type}/{id:int}")]
        public IQueryable<ResponsibilityDetailForResource> GetResponsibilitiesByResourceByType(int resourceID, string type, int id)
        {
            if (type == "Policy" || type == "Rule")
            {
                return Company.Filter<ResponsibilityDetailForResource>(i => i.ResponsibleObjectType == "Resource" && i.ResponsibleObjectID == resourceID && i.ObjectType == type);
            }
            else
            {
                return Company.Filter<ResponsibilityDetailForResource>(i => i.ResponsibleObjectType == "Resource" && i.ResponsibleObjectID == resourceID && i.ObjectType == type && i.ObjectTypeID == id);
            }
        }

        [Route("ownership/types")]
        public IQueryable<dynamic> GetResponsibilityTypes()
        {
            return Company.Table<ResponsibilityType>()
                .Select(i => new { 
                    i.ID, 
                    i.Name, 
                    i.Description, 
                    ResponsibilityTypeGroup = i.ResponsibilityTypeGroup.ToString() 
                })
                .OrderBy(i => i.ResponsibilityTypeGroup)
                .ThenBy(i => i.Name)
                .AsQueryable();
        }

        [Route("ownership/types/{id:int}/claims")]
        public IQueryable<ResponsibilityTypeClaimDetail> GetClaimsByResponsibilityType(int id)
        {
            return Company
                .Filter<ResponsibilityTypeClaim>(
                    i => i.ResponsibilityTypeID == id,
                    i => i.Claim,
                    i => i.ResponsibilityType
                )
                .Select(i => new ResponsibilityTypeClaimDetail
                {
                    Claim = i.Claim,
                    ClaimObject = i.ClaimObject,
                    ID = i.ID,
                    ResponsibilityType = i.ResponsibilityType.Name,
                    ResponsibilityTypeID = i.ResponsibilityTypeID
                });
        }

        [Route("ownership/types/{id:int}/usage")]
        public IQueryable<ResponsibilitySummaryDetail> GetUsageByResponsibilityType(int id)
        {
            return Company.GetResponsibilitiesByType(id);
        }

        [Route("ownership/{type}/{id:int}/claims")]
        public IQueryable<ResponsibilityTypeObjectClaimDetail> GetClaimsByObject(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Company
                .Filter<ResponsibilityTypeObjectClaimDetail>(
                    i => i.ObjectType == sType && i.ObjectID == id);
        }

        [Route("ownership/{type}/{id:int}/responsibilitytypes")]
        public HttpResponseMessage GetResponsibilityTypesByObject(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Request.CreateResponse(HttpStatusCode.OK, 
                Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityType.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People && i.ObjectID == id && i.ObjectType == sType, i => i.ResponsibilityType)
                .Select(i => new 
                {
                    ResponsibilityTypeGroup = i.ResponsibilityType.ResponsibilityTypeGroup,
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
        public IQueryable<PolicyType> GetPolicyTypes()
        {
            return Company.Table<PolicyType>();
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
                    { "AllowAttributes", (bool)row.AllowAttributes }
                }
            );
        }

        [Route("policytypes/{id:int}/policies")]
        public IEnumerable<dynamic> GetPoliciesByType(int id)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Policy", out joins, out columns);

            var querySql = string.Format(@"select	A.ID,
        A.ParentID,
        {0}
		A.Name,
		A.Description
from	[Policy] A  {1} 
where    A.PolicyTypeID = @id", columns, joins);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            sql = applyFilteringSuffix(sql, Request);

            return Company.Query<dynamic>(sql, new { id = id });
        }


        [Route("PolicyType/{id:int}/levels")]
        public IQueryable<PolicyTypeLevel> GetPolicyTypeLevels(int id)
        {
            return Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == id).OrderBy(i => i.Level);
        }


        #endregion

        #region Reports

        [Route("reports/mostactiveusers")]
        public IQueryable<MostActiveUserReportModel> GetMostActiveUsersReport()
        {
            return Company.GetMostActiveUsersReport();
        }

        #endregion

        #region Resources

        [HttpGet, Route("resources")]
        public IQueryable<ResourceType> GetResourceTypes()
        {
            return Community.ResourceTypes.OrderBy(i => i.Name).AsQueryable();
        }

        [Route("resources/{typeID:int}")]
        public IQueryable<GlobalReportingResource> GetResourcesByType(int typeID)
        {
            var query = Company.Table<GlobalReportingResource>();
            if (HideData3SixtyUsers())
            {
                return query.Where(i => !i.Email.Contains("data3sixty.com"));
            }
            else
            {
                return query;
            }
        }

        [Route("resources/{typeID:int}/{id:int}")]
        public Resource GetResource(int typeID, int id)
        {
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

        [Route("rules")]
        public IQueryable<Rule> GetRules()
        {
            return Company.Table<Rule>();
        }

        #endregion

        //#region Comment Tag Suggestions

        //[DataContract]
        //public class TagSuggestionModel
        //{
        //    [DataMember]
        //    public string Object { get; set; }

        //    [DataMember]
        //    public int ObjectID { get; set; }

        //    [DataMember]
        //    public string TextPath { get; set; }

        //    [DataMember]
        //    public string Url { get; set; }

        //    [DataMember]
        //    public string ObjectTypeName { get; set; }

        //    [DataMember]
        //    public string IconForeColor { get; set; }

        //    [DataMember]
        //    public string IconBackColor { get; set; }
        //}

        //[HttpGet, Route("tagsuggestions")]
        //public List<TagSuggestionModel> TagSuggestions(string phrase)
        //{
        //    if (string.IsNullOrWhiteSpace(phrase))
        //        return new List<TagSuggestionModel>();

        //    var sql = string.Format(@"select [Object], ObjectID, TextPath, Url, ObjectTypeName, IconForeColor, IconBackColor from cache.ObjectDetails where [Object] not in ('FusionAttribute', 'Intersect') and (lower(Name) like lower('{0}%') or (len('{0}') > 2 and lower(Name) like lower('%{0}%')))", phrase.Replace("'", "''").Replace("--", ""));

        //    var list = Company.Query<TagSuggestionModel>(sql).ToList();

        //    return list;
        //}

        //#endregion

        #region Type/ID Endpoints

        [Route("{type}/{id:int}")]
        public ObjectDetail GetObjectDetail(SystemObjects type, int id)
        {
            return Company.GetObjectDetail(type, id);
        }

        //[Route("Artifact/{id:int}/artifacts/statistics")]
        //public List<ChildArtifactStatisticsByObject> GetChildArtifactTileStatistics(int id)
        //{
        //    return Company.GetChildArtifactStatisticsByObject(id);
        //}

        //[Route("{type}/{id:int}/flags")]
        //public HttpResponseMessage GetFlags(SystemObjects type, int id)
        //{
        //    var flag = Company.GetActiveAlertFlagByObject(type, id);
        //    return Request.CreateResponse(HttpStatusCode.OK, 
        //        new {
        //            RedFlagged = (flag != null) ? flag.Active : false, 
        //            RedFlaggedOn = (flag != null) ? flag.Date : DateTime.MinValue
        //        });
        //}

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
                    var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType, i => i.TaxonomyType);
                    if (artifact != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = artifact.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = artifact.GetName(i => i.Name), Name = "Name", Value = artifact.Name });
                        list.Add(new DisplayField { FriendlyName = artifact.GetName(i => i.Description), Name = "Description", Value = artifact.Description });
                        list.Add(new DisplayField { FriendlyName = artifact.GetName(i => i.Status), Name = "Status", Value = artifact.Status });
                        list.Add(new DisplayField { FriendlyName = artifact.GetName(i => i.ArtifactTypeID), Name = "Type", Value = artifact.ArtifactType.Name });
                        list.Add(new DisplayField { FriendlyName = artifact.GetName(i => i.TaxonomyTypeID), Name = "OwningModel", Value = artifact.TaxonomyType.Name });
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
                case SystemObjects.Domain:
                    #region Fields
                    var domain = Company.GetById<Domain>(id, i => i.DomainType, i => i.DomainGroup);
                    if (domain != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = domain.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = domain.GetName(i => i.Name), Name = "Name", Value = domain.Name });
                        list.Add(new DisplayField { FriendlyName = domain.GetName(i => i.Description), Name = "Description", Value = domain.Description });
                        list.Add(new DisplayField { FriendlyName = "Domain Group", Name = "Group", Value = domain.DomainGroup.Name });
                        list.Add(new DisplayField { FriendlyName = domain.GetName(i => i.DomainGroupID), Name = "Type", Value = domain.DomainType.Name });
                    }
                    domain = null;
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
                        list.Add(new DisplayField { FriendlyName = policy.GetName(i => i.Name), Name = "Name", Value = policy.Name });
                        list.Add(new DisplayField { FriendlyName = policy.GetName(i => i.Description), Name = "Description", Value = policy.Description });
                        list.Add(new DisplayField { FriendlyName = policy.GetName(i => i.TextPath), Name = "TextPath", Value = policy.TextPath });
                        loadDisplayFields(list, type, id);
                    }
                    policy = null;
                    break;
                    #endregion
                case SystemObjects.Rule:
                    #region Fields
                    var rule = Company.GetById<Rule>(id);
                    if (rule != null)
                    {
                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = rule.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = rule.GetName(i => i.Name), Name = "Name", Value = rule.Name });
                        list.Add(new DisplayField { FriendlyName = rule.GetName(i => i.Description), Name = "Description", Value = rule.Description });
                        list.Add(new DisplayField { FriendlyName = rule.GetName(i => i.RuleType), Name = "RuleType", Value = rule.RuleType.ToString() });
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
                    var taxonomy = Company.GetById<Taxonomy>(id);
                    if (taxonomy != null)
                    {
                        var levelInfo = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == taxonomy.TaxonomyTypeID && i.Level == taxonomy.Level).SingleOrDefault();

                        list.Add(new DisplayField { FriendlyName = "ID", Name = "ID", Value = taxonomy.ID.ToString() });
                        list.Add(new DisplayField { FriendlyName = taxonomy.GetName(i => i.Name), Name = "Name", Value = taxonomy.Name });
                        list.Add(new DisplayField { FriendlyName = taxonomy.GetName(i => i.Description), Name = "Description", Value = taxonomy.Description });
                        list.Add(new DisplayField { FriendlyName = taxonomy.GetName(i => i.TaxonomyTypeID), Name = "TaxonomyType", Value = taxonomy.TaxonomyType.Name });
                        list.Add(new DisplayField { FriendlyName = taxonomy.GetName(i => i.TextPath), Name = "TextPath", Value = taxonomy.TextPath });
                        list.Add(new DisplayField { FriendlyName = "Level", Name = "Level", Value = levelInfo.Name });
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

        [Route("{type}/{id:int}/detail")]
        public DetailReadOnlyModel GetObjectDetailFields(SystemObjects type, int id)
        {
            var model = new DetailReadOnlyModel() { columns = 2 } ;

            var sections = new List<ReadOnlySection>();

            var list = new List<ReadOnlyField>();
            int row = 0;
            switch (type)
            {
                case SystemObjects.Artifact:
                    #region Fields

                    var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType, i => i.TaxonomyType);
                    if (artifact != null)
                    {
                        if (!string.IsNullOrEmpty(artifact.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = artifact.GetName(i => i.Description), FieldName = "ArtifactDescription", FieldDescription = artifact.GetDescription(i => i.Description), Value = artifact.Description }
                                }
                            });
                        }

                        var nodes = "None assigned";
                        var owningModels = Company.Filter<Relationship>(i => i.SourceObjectType == "Artifact" && i.SourceObjectID == id && i.TargetType == "TaxonomyType" && i.TargetTypeID == artifact.TaxonomyTypeID).Select(i => new { i.TargetUrl, i.TargetName, i.TargetObjectID }).OrderBy(i => i.TargetName).ToList();
                        if (owningModels.Count > 0)
                        {
                            nodes = "";
                            owningModels.ForEach(i =>
                            {
                                var displayName = (i.TargetName ?? string.Empty).ReplaceFirst($"{artifact.TaxonomyType.Name}/","");
                                
                                nodes += string.Format("<div><a data-context='Preview' data-type='Taxonomy' data-id='{2}' href='{0}'>{1}</a></div>", i.TargetUrl, displayName, i.TargetObjectID);
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.TaxonomyType_Name, ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID", FieldName = "ArtifactTaxonomyType", FieldDescription = artifact.GetDescription(i => i.TaxonomyTypeID), Value = artifact.TaxonomyType.Name }
                                },
                            SecondColumnFields = new List<ReadOnlyField> {
                                    new ReadOnlyField { Name = Resources.FieldInfo.TaxonomyType_Name + " Nodes", ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeIDNodes", FieldName = "ArtifactTaxonomyTypeNodes", Value = nodes }
                                }
                        });

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                    }
                    artifact = null;
                    break;
                    #endregion
                case SystemObjects.ArtifactType:
                    #region Fields
                    var artifactType = Company.GetById<ArtifactType>(id);
                    if (artifactType != null)
                    {

                        model.rows.Add(new DetailReadOnlyRowModel {
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

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField> {
                                new ReadOnlyField { Name = artifactType.GetName(i => i.AllowRelatedArtifacts), FieldName = "ArtifactTypeAllowRelatedArtifacts", FieldDescription = artifactType.GetDescription(i => i.AllowRelatedArtifacts), Value = artifactType.AllowRelatedArtifacts.FormatBooleanReadOnlyValue() }
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
                                new ReadOnlyField { Name = attributeType.GetName(i => i.TextFormatString), FieldName = "AttributeTypeTextFormatString", FieldDescription = attributeType.GetDescription(i => i.TextFormatString), Value = attributeType.TextFormatString }
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
                case SystemObjects.Domain:
                    #region Fields
                    var domain = Company.GetById<Domain>(id, i => i.DomainType, i => i.DomainGroup);
                    if (domain != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domain.GetName(i => i.Name), FieldName = "DomainGroupName", FieldDescription = domain.GetDescription(i => i.Name), Value = domain.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domain.GetName(i => i.ID), FieldName = "DomainGroupID", FieldDescription = domain.GetDescription(i => i.ID), Value = domain.ID.ToString() }
                            }
                        });

                        if (!string.IsNullOrEmpty(domain.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = domain.GetName(i => i.Description), FieldName = "DomainGroupDescription", FieldDescription = domain.GetDescription(i => i.Description), Value = domain.Description }
                                }
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domain.GetName(i => i.DomainType), FieldName = "DomainGroupDomainType", FieldDescription = domain.GetDescription(i => i.DomainType), Value = domain.DomainType.Name }
                            }
                        });
                    }
                    domain = null;
                    break;
                    #endregion
                case SystemObjects.DomainGroup:
                    #region Fields
                    var domainGroup = Company.GetById<DomainGroup>(id, d => d.DomainType);
                    if (domainGroup != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domainGroup.GetName(i => i.Name), FieldName = "DomainGroupName", FieldDescription = domainGroup.GetDescription(i => i.Name), Value = domainGroup.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domainGroup.GetName(i => i.ID), FieldName = "DomainGroupID", FieldDescription = domainGroup.GetDescription(i => i.ID), Value = domainGroup.ID.ToString() }
                            }
                        });

                        if (!string.IsNullOrEmpty(domainGroup.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = domainGroup.GetName(i => i.Description), FieldName = "DomainGroupDescription", FieldDescription = domainGroup.GetDescription(i => i.Description), Value = domainGroup.Description }
                                }
                            });
                        }

                        if (domainGroup.MasterListID.HasValue)
                        {
                            var groupMasterList = Company.GetById<Domain>(domainGroup.MasterListID.Value);
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = domainGroup.GetName(i => i.MasterListID), FieldName = "DomainGroupMasterListID", FieldDescription = domainGroup.GetDescription(i => i.MasterListID), Value = groupMasterList.Name }
                                }
                            });
                        }
                    }
                    domainGroup = null;
                    break;
                    #endregion
                case SystemObjects.DomainType:
                    #region Fields
                    var domainType = Company.GetById<DomainType>(id);
                    if (domainType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domainType.GetName(i => i.Name), FieldName = "DomainTypeName", FieldDescription = domainType.GetDescription(i => i.Name), Value = domainType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domainType.GetName(i => i.ID), FieldName = "DomainTypeID", FieldDescription = domainType.GetDescription(i => i.ID), Value = domainType.ID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = domainType.GetName(i => i.Description), FieldName = "DomainTypeDescription", FieldDescription = domainType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(domainType.Description) ? "None provided" : domainType.Description }
                            }
                        });
                    }
                    domainType = null;
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
                                new ReadOnlyField { Name = fusion.GetName(i => i.Name), FieldName = "FusionName", FieldDescription = fusion.GetDescription(i => i.Name), Value = fusion.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusion.GetName(i => i.ID), FieldName = "FusionID", FieldDescription = fusion.GetDescription(i => i.ID), Value = fusion.ID.ToString() }
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
                                new ReadOnlyField { Name = fusionAttribute.GetName(i => i.TextPath), FieldName = "FATextPath", FieldDescription = fusionAttribute.GetDescription(i => i.TextPath), Value = fusionAttribute.TextPath }
                            }
                        });

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                        model.rows.AddRange(loadDisplayableRelationshipsAsFields(type, id));
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

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = fusionAttributeType.GetName(i => i.Assignable), FieldName = "FATAssignable", FieldDescription = fusionAttributeType.GetDescription(i => i.Assignable), Value = fusionAttributeType.Assignable.FormatBooleanReadOnlyValue() }
                            }
                        });
                    }
                    fusionAttributeType = null;
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

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = intersect.GetName(i => i.Classification), FieldName = "IntersectClassification", FieldDescription = intersect.GetDescription(i => i.Classification), Value = (Enum.IsDefined(typeof(IntersectClassification), intersect.Classification.GetValueOrDefault(IntersectClassification.Normal)) ? intersect.Classification.GetValueOrDefault(IntersectClassification.Normal).ToString() : IntersectClassification.Normal.ToString()) }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = intersect.GetName(i => i.Description), FieldName = "IntersectDescription", FieldDescription = intersect.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(intersect.Description) ? "None provided" : intersect.Description }
                            }
                        });

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
                    var policy = Company.GetById<Policy>(id, i => i.Children);
                    if (policy != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = policy.GetName(i => i.Name), FieldName = "PolicyName", FieldDescription = policy.GetDescription(i => i.Description), Value = policy.Name }
                            }
                        });

                        var policyLevelInfo = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == policy.PolicyTypeID && i.Level == policy.Level).SingleOrDefault();

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
                                    new ReadOnlyField { Name = "Level Number", Value = policy.Level.ToString() }
                                }
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = policy.GetName(i => i.TextPath), FieldName = "PolicyTextPath", FieldDescription = policy.GetDescription(i => i.TextPath), Value = policy.TextPath }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = policy.GetName(i => i.Description), FieldName = "PolicyDescription", FieldDescription = policy.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(policy.Description) ? "None provided" : policy.Description }
                            }
                        });

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                    }
                    policy = null;
                    break;
                    #endregion
                case SystemObjects.Rule:
                    #region Fields
                    var rule = Company.GetById<Rule>(id);
                    if (rule != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = rule.GetName(i => i.Name), FieldName = "RuleName", FieldDescription = rule.GetDescription(i => i.Description), Value = rule.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = rule.GetName(i => i.RuleType), FieldName = "RuleRuleType", FieldDescription = rule.GetDescription(i => i.RuleType), Value = rule.RuleType.GetRuleTypeDisplayName() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = rule.GetName(i => i.Description), FieldName = "RuleDescription", FieldDescription = rule.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(rule.Description) ? "None provided" : rule.Description }
                            }
                        });
                    }
                    policy = null;
                    break;
                    #endregion
                case SystemObjects.ResponsibilityType:
                    #region Fields
                    var responsibilityType = Company.GetById<ResponsibilityType>(id);
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

                        #region Allocation

                        var allocations = string.Empty;

                        var comparer = new AllocationPossibilityComparer();
                        var allocationPossibilities = 
                            Company.GetAllocationOptions()
                            .Intersect(Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == responsibilityType.ID)
                            .Select(i => new AllocationPossibility { ObjectType = i.ObjectType, ObjectTypeID = i.ObjectID })
                            .ToList(), comparer)
                            .ToList();

                        foreach (var a in allocationPossibilities.Select(i => i.Name))
                        {
                            allocations += string.Format("<li>{0}</li>", a);
                        }
                        if (string.IsNullOrEmpty(allocations))
                        {
                            allocations = "None specified";
                        }
                        else
                        {
                            allocations = string.Format("<ul>{0}</ul>", allocations);
                        }
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Allocations", FieldName = "Allocations", FieldDescription = "", Value = allocations }
                            }
                        });

                        #endregion
                    }
                    responsibilityType = null;
                    break;
                    #endregion
                case SystemObjects.Event:
                    #region Fields
                    var evt = Company.GetById<Event>(id);
                    if (evt != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = evt.GetName(i => i.Status), FieldName = "EventStatus", FieldDescription = evt.GetDescription(i => i.Status), Value = evt.Status }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = evt.GetName(i => i.SourceID), FieldName = "EventSourceID", FieldDescription = evt.GetDescription(i => i.SourceID), Value = evt.SourceID }
                            }
                        });

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                    }
                    evt = null;
                    break;
                    #endregion
                case SystemObjects.EventGroup:
                    #region Fields
                    var evtgrp = Company.Filter<EventGroup>(i => i.ID == id).Select(i => new { 
                        i.ID, 
                        i.Name, 
                        i.PublicID, 
                        RuleName = i.Rule.Name, 
                        i.RuleID, 
                        EventCount = i.Events.Count 
                    }).SingleOrDefault();
                    if (evtgrp != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = core.resources.Fields.PublicID_Name, FieldName = "EventGroupPublicID", FieldDescription = core.resources.Fields.PublicID_Description, Value = evtgrp.PublicID }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = core.resources.Fields.ID_Name, FieldName = "EventGroupID", FieldDescription = core.resources.Fields.ID_Description, Value = evtgrp.ID.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = core.resources.Fields.Name_Name, FieldName = "EventGroupName", FieldDescription = core.resources.Fields.Name_Description, Value = evtgrp.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "# Event Details", FieldName = "EventGroupEventCount", Value = evtgrp.EventCount.ToString() }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = core.resources.Fields.Rule_Name, FieldName = "EventGroupRuleName", FieldDescription = core.resources.Fields.Rule_Description, Value = evtgrp.RuleName }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Rule ID", FieldName = "EventGroupRuleID", Value = evtgrp.RuleID.ToString() }
                            }
                        });
                    }
                    evtgrp = null;
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region Fields
                    var policyType = Company.GetById<PolicyType>(id);
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
                                new ReadOnlyField { Name = policyType.GetName(i => i.Description), FieldName = "PolicyTypeDescription", FieldDescription = policyType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(policyType.Description) ? "None provided" : policyType.Description }
                            }
                        });
                    }
                    policyType = null;
                    break;
                #endregion
                case SystemObjects.Report:
                    #region Fields
                    var report = Company.GetById<Report>(id, i => i.ReportLayout);
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

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = report.GetName(i => i.ReportLayout), FieldName = "ReportReportLayout", FieldDescription = report.GetDescription(i => i.ReportLayout), Value = report.ReportLayout.Name }
                            }
                        });

                        var sql = "";
                        //var targetObject = 
                        switch (report.ObjectType)
                        { 
                            case "Artifact":
                                sql = "select 'Artifact Instance : ' + Name from ArtifactType where ID = @id";
                                break;
                            case "ArtifactType":
                                sql = "select 'Artifact Type : ' + Name from ArtifactType where ID = @id";
                                break;
                            case "Domain":
                                sql = "select 'Domain Instance : ' + Name from DomainType where ID = @id";
                                break;
                            case "DomainType":
                                sql = "select 'Domain Type : ' + Name from DomainType where ID = @id";
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
                                var ruleEnum = (RuleType)report.ObjectID;
                                sql = string.Format("select 'Rule Instance : {0}'", ruleEnum.GetRuleTypeDisplayName());
                                break;
                            case "Taxonomy":
                                sql = "select 'Model Instance : ' + Name from TaxonomyType where ID = @id";
                                break;
                            case "TaxonomyType":
                                sql = "select 'Model Type : ' + Name from TaxonomyType where ID = @id";
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
                case SystemObjects.Resolution:
                    #region Fields
                    var resolution = Company.GetById<Resolution>(id);
                    if (resolution != null)
                    {
                        model.columns = 1;

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = resolution.GetName(i => i.Name), FieldName = "ResolutionName", FieldDescription = resolution.GetDescription(i => i.Name), Value = resolution.Name }
                            }
                        });
                        if (!string.IsNullOrEmpty(resolution.Body))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = resolution.GetName(i => i.Body), FieldName = "ResolutionBody", FieldDescription = resolution.GetDescription(i => i.Body), Value = resolution.Body }
                                }
                            });
                        }
                    }
                    resolution = null;
                    break;
                    #endregion
                case SystemObjects.Resource:
                    #region Fields
                    var resource = Community.GetById<Resource>(id);
                    if (resource != null)
                    {
                        model.columns = 1;                        

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Name", Value = resource.FormatDisplayName() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {                                
                                new ReadOnlyField { Name = resource.GetName(i => i.Email), FieldName = "ResourceEmail", FieldDescription = resource.GetDescription(i => i.Email), Value = resource.Email }
                            }
                        });
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
                case SystemObjects.ResponseType:
                    #region Fields
                    var responseType = Company.GetById<ResponseType>(id);
                    if (responseType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = responseType.GetName(i => i.Name), FieldName = "ResponseTypeName", FieldDescription = responseType.GetDescription(i => i.Name), Value = responseType.Name }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = responseType.GetName(i => i.AllowOptions), FieldName = "ResponseTypeAllowOptions", FieldDescription = responseType.GetDescription(i => i.AllowOptions), Value = responseType.AllowOptions.FormatBooleanReadOnlyValue() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = responseType.GetName(i => i.AllowValueOverride), FieldName = "ResponseTypeAllowValueOverride", FieldDescription = responseType.GetDescription(i => i.AllowValueOverride), Value = responseType.AllowValueOverride.FormatBooleanReadOnlyValue() }
                            }
                        });
                    }
                    responseType = null;
                    break;
                    #endregion
                case SystemObjects.StatisticType:
                    #region Fields
                    var statisticType = Company.GetById<StatisticType>(id);
                    if (statisticType != null)
                    {
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = statisticType.GetName(i => i.Name), FieldName = "StatisticTypeName", FieldDescription = statisticType.GetDescription(i => i.Name), Value = statisticType.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = statisticType.GetName(i => i.PartOfScore), FieldName = "StatisticTypePartOfScore", FieldDescription = statisticType.GetDescription(i => i.PartOfScore), Value = statisticType.PartOfScore.FormatBooleanReadOnlyValue() }
                            }
                        });

                        if (!string.IsNullOrEmpty(statisticType.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = statisticType.GetName(i => i.Description), FieldName = "StatisticTypeDescription", FieldDescription = statisticType.GetDescription(i => i.Description), Value = statisticType.Description }
                            }
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                        {
                            new ReadOnlyField { Name = statisticType.GetName(i => i.CheckType), FieldName = "StatisticTypeCheckType", FieldDescription = statisticType.GetDescription(i => i.CheckType), Value = statisticType.CheckType.GetDisplayName() }
                        }
                        });

                        var fields = XElement.Parse(statisticType.Configuration);
                        int oID = 0;
                        ObjectDetail dtl = null;

                        switch (statisticType.CheckType)
                        {
                            case StatisticCheckType.Existence:              //1
                                #region
                                oID = int.Parse(fields.Element("ObjectID").Value);
                                dtl = Company.GetObjectDetail(fields.Element("ObjectType").Value, oID);
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_Target", Name = "Target", Value = (dtl != null) ? dtl.Name : "Not found" }
                                    }
                                });
                                dtl = null;
                                break;
                                #endregion
                            case StatisticCheckType.Count:                  //2
                                #region
                                oID = int.Parse(fields.Element("ObjectID").Value);
                                dtl = Company.GetObjectDetail(fields.Element("ObjectType").Value, oID);
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_Target", Name = "Target", Value = (dtl != null) ? dtl.Name : "Not found" }
                                    }
                                });
                                dtl = null;
                                break;
                                #endregion
                            case StatisticCheckType.PropertyValueCheck:     //3
                                #region
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 2,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_PropertyName", Name = "Property Name", Value = fields.Element("PropertyName").Value }
                                    },
                                    SecondColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_PropertyValue", Name = "Property Value", Value = (fields.Element("PropertyValue") != null) ? fields.Element("PropertyValue").Value : "Not set" }
                                    }
                                });
                                break;
                                #endregion
                            case StatisticCheckType.PropertyPopulated:  //4
                                #region
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_PropertyName", Name = "Property Name", Value = fields.Element("PropertyName").Value }
                                    }
                                });
                                break;
                                #endregion
                            case StatisticCheckType.Relationship:       //5
                                #region
                                var items = new List<string>();
                                var html = string.Empty;

                                try
                                {
                                    if (fields.Element("CheckObjects") != null)
                                    {
                                        var checkObjects = fields.Element("CheckObjects").Elements("Object").Select(co => new { Type = (SystemObjects)Enum.Parse(typeof(SystemObjects), co.Element("Type").Value), ID = int.Parse(co.Element("ID").Value) }).ToList();
                                        checkObjects.ForEach(co =>
                                        {
                                            var cod = Company.GetObjectDetail(co.Type, co.ID);
                                            if (cod != null)
                                            {
                                                items.Add(cod.TextPath);
                                            }
                                        });
                                    }
                                    else
                                    {
                                        var cod = Company.GetObjectDetail((SystemObjects)Enum.Parse(typeof(SystemObjects), fields.Element("ObjectType").Value), int.Parse(fields.Element("ObjectID").Value));
                                        if (cod != null)
                                        {
                                            items.Add(cod.TextPath);
                                        }
                                    }

                                    foreach (var t in items.OrderBy(i => i))
                                    {
                                        html += (string.IsNullOrEmpty(html) ? t : " or " + t);
                                    }
                                }
                                catch (Exception ex)
                                {

                                }

                                if (string.IsNullOrEmpty(html)) html = "Not found";
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_Targets", Name = "Target(s)", Value = html }
                                    }
                                });
                                dtl = null;
                                break;
                                #endregion
                            case StatisticCheckType.FusionOwnership:    //6
                                break;
                            case StatisticCheckType.ScoreRollupViaRelationship:    //7
                                #region
                                oID = int.Parse(fields.Element("ObjectID").Value);
                                dtl = Company.GetObjectDetail(fields.Element("ObjectType").Value, oID);
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_Target", Name = "Target", Value = (dtl != null) ? dtl.Name : "Not found" }
                                    }
                                });
                                dtl = null;
                                break;
                            case StatisticCheckType.ScoreRollupViaOwnership:    //8
                                oID = int.Parse(fields.Element("ObjectID").Value);
                                dtl = Company.GetObjectDetail(fields.Element("ObjectType").Value, oID);
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_Target", Name = "Target", Value = (dtl != null) ? dtl.Name : "Not found" }
                                    }
                                });
                                dtl = null;
                                break;
                                #endregion
                            case StatisticCheckType.EventMetric:        //9
                                #region
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 2,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_ValidField", Name = "Valid Field", Value = fields.Element("ValidField").Value }
                                    },
                                    SecondColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_InvalidField", Name = "Invalid Field", Value = fields.Element("InvalidField").Value }
                                    }
                                });
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_Threshold", Name = "Threshold", Value = fields.Element("Threshold").Value }
                                    }
                                });
                                break;
                            #endregion
                            case StatisticCheckType.PredicateMetric:    //10
                                #region
                                oID = int.Parse(fields.Element("Predicate").Value);
                                var p = Company.GetById<Predicate>(oID);
                                model.rows.Add(new DetailReadOnlyRowModel
                                {
                                    columns = 1,
                                    FirstColumnFields = new List<ReadOnlyField>
                                    {
                                        new ReadOnlyField { FieldName = "Display_Predicate", Name = "Predicate", Value = (p != null) ? p.Name : "Not found" }
                                    }
                                });
                                p = null;
                                break;
                                #endregion
                        }
                    }
                    statisticType = null;
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
                                new ReadOnlyField { Name = surveyType.GetName(i => i.Name), FieldName = "SurveyTypeName", FieldDescription = surveyType.GetDescription(i => i.Name), Value = surveyType.Name }
                            }
                        });

                        var dtlSurveyType = Company.GetObjectDetail(surveyType.ObjectType, surveyType.ObjectID);
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = surveyType.GetName(i => i.ObjectType), FieldName = "SurveyTypeObjectType", FieldDescription = surveyType.GetDescription(i => i.ObjectType), Value = surveyType.ObjectType.ToString() }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = surveyType.GetName(i => i.ObjectID), FieldName = "SurveyTypeObjectID", FieldDescription = surveyType.GetDescription(i => i.ObjectID), Value = (dtlSurveyType != null) ? dtlSurveyType.Name : surveyType.ObjectID.ToString() }
                            }
                        });

                    }
                    surveyType = null;
                    break;
                    #endregion
                case SystemObjects.Taxonomy:
                    #region Fields
                    var taxonomy = Company.GetById<Taxonomy>(id, i => i.TaxonomyType.TaxonomyTypeClass);
                    if (taxonomy != null)
                    {
                        var levelInfo = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == taxonomy.TaxonomyTypeID && i.Level == taxonomy.Level).SingleOrDefault();

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = taxonomy.GetName(i => i.Name), FieldName = "TaxonomyName", FieldDescription = taxonomy.GetDescription(i => i.Name), Value = taxonomy.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = taxonomy.GetName(i => i.TaxonomyType.TaxonomyTypeClassID), FieldName = "TaxonomyTypeClass", FieldDescription = taxonomy.GetDescription(i => i.TaxonomyType.TaxonomyTypeClassID), Value = taxonomy.TaxonomyType.TaxonomyTypeClass.Name }
                            }
                        });

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

                        if (!string.IsNullOrEmpty(taxonomy.Description))
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = taxonomy.GetName(i => i.Description), FieldName = "TaxonomyDescription", FieldDescription = taxonomy.GetDescription(i => i.Description), Value = taxonomy.Description }
                                }
                            });
                        }

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = taxonomy.GetName(i => i.TextPath), FieldName = "TaxonomyTextPath", FieldDescription = taxonomy.GetDescription(i => i.TextPath), Value = taxonomy.TextPath }
                            }
                        });

                        model.rows.AddRange(loadDynamicDisplayFields(type, id));
                    }
                    taxonomy = null;
                    break;
                    #endregion
                case SystemObjects.TaxonomyType:
                    #region Fields
                    var taxonomyType = Company.GetById<TaxonomyType>(id, i => i.TaxonomyTypeClass);
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
                                new ReadOnlyField { Name = taxonomyType.GetName(i => i.TaxonomyTypeClassID), FieldName = "TaxonomyTypeClass", FieldDescription = taxonomyType.GetDescription(i => i.TaxonomyTypeClassID), Value = taxonomyType.TaxonomyTypeClass.Name }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = taxonomyType.GetName(i => i.MaximumDepth), FieldName = "TaxonomyTypeMaximumDepth", FieldDescription = taxonomyType.GetDescription(i => i.MaximumDepth), Value = taxonomyType.MaximumDepth.ToString() }
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
                case SystemObjects.WorkflowTypeRelation:
                    #region Fields
                    var wtr = Company.GetWorkflowRelations().SingleOrDefault(i => i.ID == id);
                    if (wtr != null)
                    {
                        var rowNumber = 1;
                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 2,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Type", FieldName = "WtrType", FieldDescription = "", Value = wtr.ObjectName }
                            },
                            SecondColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Row = rowNumber, Column = 2, Name = Resources.FieldInfo.TaxonomyType_Name, ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID", FieldName = "WtrOwner", FieldDescription = "", Value = wtr.ParentName ?? "None" }
                            }
                        });

                        model.rows.Add(new DetailReadOnlyRowModel
                        {
                            columns = 1,
                            FirstColumnFields = new List<ReadOnlyField>
                            {
                                new ReadOnlyField { Name = "Responsibility", FieldName = "WtrResponsibility", FieldDescription = "", Value = wtr.ResponsibilityType }
                            }
                        });

                        foreach (var p in wtr.Properties)
                        {
                            model.rows.Add(new DetailReadOnlyRowModel
                            {
                                columns = 1,
                                FirstColumnFields = new List<ReadOnlyField>
                                {
                                    new ReadOnlyField { Name = p.Key, FieldName = string.Format("Wtr{0}", p.Key), FieldDescription = "", Value = p.Value }
                                }
                            });
                        }
                    }
                    wtr = null;
                    break;
                    #endregion
            }

            sections.Add(new ReadOnlySection { Name = "Governance", Fields = list, ID = 0 });

            return model;

            //return Request.CreateResponse(HttpStatusCode.OK, sections);//new { Fields = list });
        }

        [Route("{type}/{id:int}/object/statistics")]
        public ObjectStatisticTileModel GetTileObjectStatistics(SystemObjects type, int id)
        {
            return Company.GetObjectStatistics(type, id);
        }

        [Route("fusion/statistics")]
        public FusionStatisticTileModel GetFusionStatistics()
        {
            return Company.Query<FusionStatisticTileModel>(QueryConstants.FusionStatisticsItem).FirstOrDefault();
        }

        [Route("{type}/{id:int}/fieldlookup")]
        public EditableFieldLookupList GetEditableFieldLookupData(SystemObjects type, int id, int take = 10000)
        {
            var list = new EditableFieldLookupList();
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
                        var ei = new EditableFieldLookupItem();
                        ei.Add("ID", item.ID);
                        ei.Add("Name", item.Name);
                        list.Add(ei);
                    }
                    break;
                case SystemObjects.Artifact:
                    var aItems = Company.Filter<Artifact>(i => i.ArtifactTypeID == id);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        aItems = aItems.Where(i => i.Name.StartsWith(prefix));
                    }
                    else
                    {
                        aItems = aItems.Take(take);
                    }
                    foreach (var item in aItems)
                    {
                        var ei = new EditableFieldLookupItem();
                        ei.Add("ID", item.ID);
                        ei.Add("Name", item.Name);
                        list.Add(ei);
                    }
                    break;
                case SystemObjects.DomainType:
                case SystemObjects.Domain:
                    var dItems = Company.Filter<Domain>(i => i.DomainTypeID == id);
                    if (!string.IsNullOrEmpty(prefix)) dItems = dItems.Where(i => i.Name.StartsWith(prefix));
                    dItems = dItems.Take(take);
                    foreach (var item in dItems)
                    {
                        var ei = new EditableFieldLookupItem();
                        ei.Add("ID", item.ID);
                        ei.Add("Name", item.Name);
                        list.Add(ei);
                    }
                    break;
                case SystemObjects.Taxonomy:
                    var imItems = Company.Table<Taxonomy>();
                    if (!string.IsNullOrEmpty(prefix)) imItems = imItems.Where(i => i.TextPath.Contains(prefix));
                    imItems = imItems.OrderBy(i => i.TextPath).Take(take);
                    foreach (var item in imItems)
                    {
                        var ei = new EditableFieldLookupItem();
                        ei.Add("ID", item.ID);
                        ei.Add("Name", item.TextPath);
                        list.Add(ei);
                    }
                    break;
                case SystemObjects.TaxonomyType:
                    var imtItems = Company.Filter<Taxonomy>(i => i.TaxonomyTypeID == id);
                    if (!string.IsNullOrEmpty(prefix)) imtItems = imtItems.Where(i => i.TextPath.Contains(prefix));
                    imtItems = imtItems.OrderBy(i => i.TextPath).Take(take);
                    foreach (var item in imtItems)
                    {
                        var ei = new EditableFieldLookupItem();
                        ei.Add("ID", item.ID);
                        ei.Add("Name", item.TextPath);
                        list.Add(ei);
                    }
                    break;
            }
            return list;
        }

        [Route("{type}/{id:int}/fields")]
        public List<EditableFieldItem> GetFieldTypesByObject(SystemObjects type, int id)
        {
            List<EditableFieldItem> list;

            if (type != SystemObjects.DomainItem)
            {
                list = Company
                    .GetFieldTypeRelationsByObject(type, id)
                    .Select(i => new EditableFieldItem
                    {
                        Text = i.FriendlyName,
                        Value = "{" + i.Name + "}"
                    })
                    .ToList();
            }
            else
            {
                list = new List<EditableFieldItem>();
            }

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list.Add(new EditableFieldItem { Text = "Name", Value = "{Name}" });
                    list.Add(new EditableFieldItem { Text = "Status", Value = "{Status}" });
                    list.Add(new EditableFieldItem { Text = "Description", Value = "{Description}" });
                    break;
                case SystemObjects.DomainItem:
                case SystemObjects.DomainType:
                    list.Add(new EditableFieldItem { Text = "Name", Value = "{Name}" });
                    list.Add(new EditableFieldItem { Text = "Code", Value = "{Code}" });
                    list.Add(new EditableFieldItem { Text = "Description", Value = "{Description}" });
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

        [Route("{type}/{id:int}/ownership")]
        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByObject(SystemObjects type, int id, bool showHidden = false)
        {
            return Company.GetResponsibilitiesByObject(type, id, showHidden);
        }

        [Route("{type}/{id:int}/permissions")]
        public List<PermissionModel> GetPermissionsObObject(SystemObjects type, int id)
        {
            List<PermissionModel> permissions = null;

            if (Company.CurrentResourceIsAdmin)
            {
                permissions = new List<PermissionModel>() {
                    new PermissionModel{ ClaimObject = ClaimObject.Attribute.ToString(), Claim = Claim.Create.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Attribute.ToString(), Claim = Claim.Delete.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Attribute.ToString(), Claim = Claim.Read.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Attribute.ToString(), Claim = Claim.Update.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Governance.ToString(), Claim = Claim.Create.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Governance.ToString(), Claim = Claim.Delete.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Governance.ToString(), Claim = Claim.Read.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Governance.ToString(), Claim = Claim.Update.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Relationship.ToString(), Claim = Claim.Create.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Relationship.ToString(), Claim = Claim.Delete.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Relationship.ToString(), Claim = Claim.Read.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Relationship.ToString(), Claim = Claim.Update.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Root.ToString(), Claim = Claim.Create.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Root.ToString(), Claim = Claim.Delete.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Root.ToString(), Claim = Claim.Read.ToString() },
                    new PermissionModel{ ClaimObject = ClaimObject.Root.ToString(), Claim = Claim.Update.ToString() }
                };
            }
            else 
            {
                permissions = Company.GetPermissions(type, id).ToList().Select(i => new PermissionModel { ClaimObject = i.ClaimObject.ToString(), Claim = i.Claim.ToString() }).ToList();
            }

            return permissions;
        }

        //[Route("{type}/{id:int}/social/statistics")]
        //public SocialStatisticsByObject GetSocialTileStatistics(SystemObjects type, int id)
        //{
        //    return Company.GetSocialStatisticsByObject(type, id);
        //}

        [Route("{type}/{id:int}/statistics")]
        public IQueryable<StatisticDetail> GetStatisticDetails(SystemObjects type, int id)
        {
            return Company.GetStatisticDetailsByType(type, id).AsQueryable();
        }

        [Route("{type}/{id:int}/synonyms")]
        public HttpResponseMessage GetSynonymsByObject(SystemObjects type, int id)
        {
            var models = Company.Query<dynamic>(
                QueryConstants.SynonymsByObjectList, 
                new {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id
                }
            );

            return Request.CreateResponse(
                HttpStatusCode.OK,
                models
            );
        }

        [Route("workflows/relations")]
        public HttpResponseMessage GetWorkflowRelations()
        {
            var models = Company.GetWorkflowRelations();
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        #endregion

        #region Surveys

        [Route("responsetypes")]
        public IQueryable<ResponseType> GetResponseTypes()
        {
            return Company.Table<ResponseType>();
        }

        [Route("responsetypes/{typeID:int}/options")]
        public IQueryable<ResponseTypeOption> GetOptionsByResponseType(int typeID)
        {
            return Company.Filter<ResponseTypeOption>(i => i.ResponseTypeID == typeID);
        }

        [Route("surveys")]
        public IQueryable<SurveyType> GetSurveyTypes()
        {
            return Company.Table<SurveyType>();
        }

        [Route("surveys/{typeID:int}/entries")]
        public List<SurveyModel> GetEntriesBySurveyType(int typeID)
        {
            var type = Company.GetById<SurveyType>(typeID);
            if (type != null)
            {
                return  (
                        from s in Company.Filter<Survey>(i => i.SurveyTypeID == typeID).ToList()
                        join r in Community.Table<Resource>() on s.ResourceID equals r.ID
                        select new SurveyModel 
                        {
                            ID = s.ID, 
                            ResourceID = s.ResourceID, 
                            ResourceName = r.FormatDisplayName(), 
                            PercentComplete = (int)(Math.Round((decimal)s.Questions.Count / (decimal)type.QuestionTypes.Count, 2) * 100)
                        }
                        ).ToList();
            }
            else
            {
                return null;
            }
        }

        [Route("surveys/{typeID:int}/questions")]
        public IQueryable<QuestionType> GetQuestionTypesBySurveyType(int typeID)
        {
            return Company.Filter<QuestionType>(i => i.SurveyTypeID == typeID);
        }

        [Route("surveys/{typeID:int}/{type}/{id}/report")]
        public JObject GetSurveyReport(int typeID, SystemObjects type, int id)
        {
            var sType = type.ToString();
            var model = Company.Filter<SurveyObjectCache>(i => i.SurveyTypeID == typeID && i.ObjectType == sType && i.ObjectID == id).SingleOrDefault();
            if (model == null)
            {
                return null;
            }
            else
            {
                var xml = XElement.Parse(model.ReportCache);
                string json = JsonConvert.SerializeXNode(xml);
                return JObject.Parse(json);
            }
        }

        [Route("surveys/{type}/{id}/randomquestion")]
        public JObject GetRandomSurveyQuestion(SystemObjects type, int id)
        {
            var xml = Company.GetRandomSurveyQuestionForUser(type, id);
            string json = JsonConvert.SerializeXNode(xml);
            return JObject.Parse(json);
        }

        [Route("surveys/randomquestion")]
        public CreateResponse Post(QuestionResponseModel model)
        {
            var option = Company.Filter<ResponseTypeOption>(i => i.ResponseType.QuestionTypes.Any(q => q.ID == model.QuestionTypeID) && i.Value == model.Value).SingleOrDefault();
            if (option == null) throw new NotFoundException("Response Option");

            var survey = Company.Filter<Survey>(i => i.SurveyTypeID == model.SurveyTypeID && i.ResourceID == Company.CurrentResourceID).SingleOrDefault();
            if (survey == null)
            {
                survey = new Survey { ObjectID = model.ObjectID, ObjectType = model.ObjectType.ToString(), ResourceID = Company.CurrentResourceID, SurveyTypeID = model.SurveyTypeID };
                Company.Add<Survey>(survey);
            }
            Company.Add<Question>(new Question { Comment = model.Comment, ResponseTypeOptionID = option.ID, QuestionTypeID = model.QuestionTypeID, SurveyID = survey.ID });

            return new CreateResponse { Message = "Created" };
        }

        #endregion

        #region Statistics

        [Route("statistics")]
        public IEnumerable<dynamic> GetStatisticTypes()
        {
            return Company.Query<dynamic>(QueryConstants.StatisticTypeDetailList);
        }

        #endregion

        #region Taxonomy

        [Route("catalogs")]
        public HttpResponseMessage GetTaxonomyTypes()
        {
            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, 
                Company.Table<TaxonomyType>().OrderBy(i => i.Name).Select(i => new { i.Description, i.ID, i.MaximumDepth, i.Name, TaxonomyTypeClass = i.TaxonomyTypeClass.Name })
            );
        }

        [Route("TaxonomyType/{id:int}/levels")]
        public IQueryable<TaxonomyTypeLevel> GetTaxonomyTypeLevels(int id)
        {
            return Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id).OrderBy(i => i.Level);
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
                    { "AllowSynonyms", (bool)row.AllowSynonyms }
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

        public class CountModel
        {
            public string Name { get; set; }
            public int? New { get; set; }
            public int? Total { get; set; }
            public string NewUri { get; set; }
            public string TotalUri { get; set; }
        }

        public class CountTempModel
        {
            public int TypeID { get; set; }
            public int Count { get; set; }
        }



        [Route("CountItems/Activity/{artifactTypeId}/{days}")]
        public IQueryable GetAreaActivityItems(int artifactTypeId, int days)
        {
            if(days != 0)
            {
                DateTime startDate = DateTime.Now.AddDays(days * -1);

                return Company.Filter<Artifact>(i => i.CreatedOn > startDate && i.ArtifactTypeID == artifactTypeId).AsQueryable();
            }                
            
            return Company.Filter<Artifact>(i => i.ArtifactTypeID == artifactTypeId).AsQueryable();
        }

        [Route("Count/{area}/{days}")]
        public IEnumerable<CountModel> GetHomeCounts(string area, int days, int id = -1)
        {
            var areaName = (area ?? string.Empty).ToUpper();
            var resourceId = id > 0 ? id : Company.CurrentResourceID;

            switch (areaName)
            {
                case "SOCIAL":
                    return LoadSocialActivityCount(days,resourceId);
                case "ACTIVITY":
                    return LoadArtifactActivityCount(days);
                case "ASSIGNMENTS":
                    return LoadWorkflowAssignmentsCount(resourceId);
            }

            return null;
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
            var socialUri = "/Home/SocialActivityOverlay";

            var counts = Company.GetCommentCountByFollower(resourceId, days).ToList().OrderBy(i => i.CommentTypeName);
            
            List<CountModel> items = new List<CountModel>();

            //need to add a record for social, Issue, Task, DataEvent, Question

            items.Add(new CountModel { Name = Resources.Core.CommentType_Social, Total = getCommentCategoryCount(counts, CommentType.Social), TotalUri = $"{socialUri}?type={(int)CommentType.Social}" });

            items.Add(new CountModel { Name = Resources.Core.CommentType_Issue, Total = getCommentCategoryCount(counts, CommentType.Issue), TotalUri = $"{socialUri}?type={(int)CommentType.Issue}" });

            items.Add(new CountModel { Name = Resources.Core.CommentType_Task, Total = getCommentCategoryCount(counts, CommentType.Task), TotalUri = $"{socialUri}?type={(int)CommentType.Task}" });

            items.Add(new CountModel { Name = Resources.Core.CommentType_DataEvent, Total = getCommentCategoryCount(counts, CommentType.DataEvent), TotalUri = $"{socialUri}?type={(int)CommentType.DataEvent}" });

            items.Add(new CountModel { Name = Resources.Core.CommentType_Challenge, Total = getCommentCategoryCount(counts, CommentType.Challenge), TotalUri = $"{socialUri}?type={(int)CommentType.Challenge}" });

            return items.OrderBy(x => x.Name);
        }

        private int getCommentCategoryCount(IEnumerable<CommentCount> counts, CommentType commentType)
        {
            var commentsItem = (counts.FirstOrDefault(x => x.CommentType == commentType));
            return commentsItem == null ? 0 : commentsItem.Count;
        }

        private IEnumerable<CountModel> LoadWorkflowAssignmentsCount(int resourceId)
        {
            var sql = @"(select '/Home/AssignmentActivityOverlay?mode=total&type=1&resourceID=" + resourceId + "' as TotalUri, '" + Resources.Core.WorkflowType_SuggestNewArtifact + @"' as Name, COUNT(*) AS Total FROM WorkflowResource WR inner join Workflow W on (W.ID = WR.WorkflowID) where W.DateCompleted is null and WR.ResourceID = @r and WR.IsComplete = 0 and W.WorkflowType = 1
                        union
                        select '/Home/AssignmentActivityOverlay?mode=total&type=2&resourceID=" + resourceId + "' as TotalUri, '" + Resources.Core.WorkflowType_CertifyArtifact + @"' as Name, COUNT(*) AS Total FROM WorkflowResource WR inner join Workflow W on (W.ID = WR.WorkflowID) where W.DateCompleted is null and WR.ResourceID = @r and WR.IsComplete = 0 and W.WorkflowType = 2
                        union
                        select '/Home/AssignmentActivityOverlay?mode=total&type=3&resourceID=" + resourceId + "' as TotalUri, '" + Resources.Core.WorkflowType_WorkIssue + @"' as Name, COUNT(*) AS Total FROM WorkflowResource WR inner join Workflow W on (W.ID = WR.WorkflowID) where W.DateCompleted is null and WR.ResourceID = @r and WR.IsComplete = 0 and W.WorkflowType = 3
                        union
                        select '/Home/AssignmentActivityOverlay?mode=total&type=4&resourceID=" + resourceId + "' as TotalUri, '" + Resources.Core.WorkflowType_ChallengeArtifact + @"' as Name, COUNT(*) AS Total FROM WorkflowResource WR inner join Workflow W on (W.ID = WR.WorkflowID) where W.DateCompleted is null and WR.ResourceID = @r and WR.IsComplete = 0 and W.WorkflowType = 4)
                        order by Name";

            return Company.Query<CountModel>(sql, new { r = resourceId });
        }

        #endregion
    }
}
