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
using d360.web.Models.Attributes;
using System.Dynamic;

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
                var ro = new DisplayField
                {
                    FriendlyName = k.FriendlyName,
                    Value = k.FormattedValue,
                    Name = k.Name
                };
                list.Add(ro);
            }
        }

        int loadDynamicDisplayFields(List<ReadOnlyField> list, SystemObjects type, int id, int startRow) 
        {
            var fields = Company.GetFieldRelationsByObject(type, id);
            var row = startRow-1;

            foreach (var k in fields)
            {
                row++;

                if (k.Type == DataType.FusionLookup.ToString())
                {
                    //look at fusionlookup field and figure out what to show
                    row += RenderFusionLookupField(k,list,row);
                }
                else
                {
                    var ro = new ReadOnlyField
                    {
                        Row = row,
                        Column = 1,
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

                    list.Add(ro);
                }                
            }

            return row+1;
        }
            

        int loadDisplayableRelationshipsAsFields(List<ReadOnlyField> list, SystemObjects type, int id, int row)
        {
            var relationships = Company.GetDetailDisplayableRelationships(type, id);

            foreach (var k in relationships)
            {
                row++;

                var ro = new ReadOnlyField
                {
                    Row = row,
                    Column = 1,
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

                list.Add(ro);
            }

            return row + 1;
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

        void parseDynamicColumnsAndFields(List<FieldTypeWithRelation> items, List<GridColumn> columns, List<GridField> fields, decimal dynamicFieldWidth, bool serverPaged = false)
        {
            items.ForEach(i =>
            {
                string cellsFormat = "";
                string fieldType = "string";
                string columnType = GridColumn.COLUMN_TYPE_STRING;
                string filterType = GridColumn.FILTER_TYPE_STRING;
                List<string> filterItems = new List<string>();

                switch (i.Type)
                {
                    case "":
                    case "Lookup":
                        switch (i.LookupObjectType)
                        {
                            case "Artifact":
                                filterItems = Company.Filter<Artifact>(o => o.ArtifactTypeID == i.LookupObjectID).OrderBy(o => o.Name).Select(o => o.Name).ToList();
                                break;
                            case "Domain":
                                filterItems = Company.Filter<DomainItem>(o => o.DomainID == i.LookupObjectID).OrderBy(o => o.Name).Select(o => o.Name).ToList();
                                break;
                            case "Lookup":
                                filterItems = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == i.ID && o.LookupObjectType == "Lookup" && o.LookupObjectID == i.LookupObjectID).OrderBy(o => o.Text).Select(o => o.Text).ToList();
                                break;
                        }
                        columnType = GridColumn.COLUMN_TYPE_DROPDOWN;
                        filterType = serverPaged ? GridColumn.FILTER_TYPE_LIST : GridColumn.FILTER_TYPE_CHECKEDLIST;
                        break;
                    case "Date":
                        cellsFormat = "MM/dd/yyyy";
                        fieldType = "date";
                        columnType = GridColumn.COLUMN_TYPE_DATE;
                        filterType = serverPaged ? GridColumn.FILTER_TYPE_DATE : GridColumn.FILTER_TYPE_RANGE;
                        break;
                    case "DateTime":
                        cellsFormat = "MM/dd/yyyy HH:mm:ss";
                        fieldType = "date";
                        columnType = GridColumn.COLUMN_TYPE_DATE;
                        filterType = serverPaged ? GridColumn.FILTER_TYPE_DATE : GridColumn.FILTER_TYPE_RANGE;
                        break;
                    case "Number":
                        cellsFormat = "n";
                        fieldType = "number";
                        columnType = GridColumn.COLUMN_TYPE_NUMBER;
                        filterType = GridColumn.FILTER_TYPE_NUMBER;
                        break;
                    case "Decimal":
                        cellsFormat = "d4";
                        fieldType = "number";
                        columnType = GridColumn.COLUMN_TYPE_NUMBER;
                        filterType = GridColumn.FILTER_TYPE_NUMBER;
                        break;
                    case "Boolean":
                        fieldType = "bool";
                        columnType = GridColumn.COLUMN_TYPE_CHECKBOX;
                        filterType = GridColumn.FILTER_TYPE_CHECKBOX;
                        break;
                }
                columns.Add(new GridColumn { text = i.FriendlyName, datafield = i.Name, width = string.Format("{0}%", dynamicFieldWidth), columntype = columnType, filtertype = filterType, filteritems = filterItems, cellsformat = cellsFormat });
                fields.Add(new GridField { name = i.Name, type = fieldType });
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
                        columns.Add(new GridColumn { text = d360.core.resources.Fields.Parent_Name, datafield = "Parent", columntype = GridColumn.COLUMN_TYPE_DROPDOWN, filtertype = GridColumn.FILTER_TYPE_LIST, width = calculateStaticColumnWidth(10, dynamicFieldWidth, remainingWidth, staticFieldCount), filterable = true, filteritems = Company.Filter<Artifact>(i => i.ArtifactTypeID == artifactType.ParentID).OrderBy(i => i.TextPath).Select(i => i.TextPath).ToList() });

                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth, true);

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
                    break;
                    #endregion
                case SystemObjects.LookupType:
                    #region
                    staticFieldCount = 1;
                    remainingWidth = 90;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth, true);

                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "LookupTypeID", type = "number" });
                    break;
                    #endregion
                case SystemObjects.Rule:
                    #region
                    staticFieldCount = 4;
                    remainingWidth = 55;
                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count());

                    columns.Add(new GridColumn { text = "Date", datafield = "Date", columntype = GridColumn.COLUMN_TYPE_DATE, filtertype = GridColumn.FILTER_TYPE_RANGE, width = calculateStaticColumnWidth(15, dynamicFieldWidth, remainingWidth, staticFieldCount), cellsformat = "MM/dd/yyyy HH:mm:ss" });

                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth, true);

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

                    var relations = Company.Query<dynamic>(@"SELECT distinct 'IntersectType' + cast(S.IntersectTypeID as varchar(10)) as Name, TD.Name as FriendlyName
				FROM		IntersectTypeNode S
							inner join IntersectTypeNode T ON T.IntersectTypeID = S.IntersectTypeID and T.ID <> S.ID and S.ObjectType = 'FusionAttributeType' and S.ObjectID = @id
							inner join cache.ObjectDetails TD on TD.[Object] = T.ObjectType and TD.ObjectID = T.ObjectID", new { id = id }).ToList();

                    dynamicFieldWidth = calculateDynamicColumnWidth(remainingWidth, items.Count() + relations.Count);

                    columns.Add(new GridColumn { text = d360.core.resources.Fields.Name_Name, datafield = "Name", width = calculateStaticColumnWidth(25, dynamicFieldWidth, remainingWidth, staticFieldCount), filteritems = new List<string>() });
                    fields.Add(new GridField { name = "ID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });

                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth);

                    relations.ForEach(i =>
                    {
                        columns.Add(new GridColumn { text = i.FriendlyName, datafield = i.Name, width = string.Format("{0}%", dynamicFieldWidth), filtertype = GridColumn.FILTER_TYPE_NUMBER, cellsformat = "n" });
                        fields.Add(new GridField { name = i.Name, type = "number" });
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

                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth);

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
                    parseDynamicColumnsAndFields(items, columns, fields, dynamicFieldWidth);
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
                Columns = columns
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
                    followingParent = (followParent != null);
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
                        //if (hasPermission(permissions, Claim.Create, ClaimObject.Root))
                        //{
                        //    var synonymTypes = Company.GetUnusedSynonymTypesByObject(type, id).ToList();
                        //    if (synonymTypes.Count > 0)
                        //    {
                        //        var addSynonymItem = new PageActionItem { Title = "Synonyms" };
                        //        foreach (var s in synonymTypes)
                        //        {
                        //            addSynonymItem.Items.Add(new PageActionItem { Context = ContextList.Synonym, Icon = Resources.Actions.SynonymType_Icon, Title = s.Name, Uri = string.Format("/form/AddSynonym?type={0}&id={1}&synonymTypeID={2}", type.ToString(), id, s.ID) });
                        //        }
                        //        addItem.Items.Add(addSynonymItem);
                        //    }
                        //}

                        //if (hasPermission(permissions, Claim.Create, ClaimObject.Governance))
                        //{
                        //    loadResponsiblityTypeAddMenu(type, id, addItem, false);
                        //}

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

                        //list.Add(new PageActionItem { Context = ContextList.Intersect, Icon = Resources.Actions.ViewRelationships_Icon, Title = Resources.Actions.ViewRelationships, Uri = string.Format("/relations/RelationOverlay?type={0}&id={1}", type.ToString(), id) });

                        if (hasPermission(permissions, Claim.Update, ClaimObject.Root))
                        {
                            list.Add(new PageActionItem { Context = ContextList.Artifact, Icon = Resources.Actions.Edit_Icon, Title = Resources.Actions.Edit, Uri = string.Format("/form/artifacts/{0}/{1}/edit", artifact.ArtifactTypeID, id) });
                        }
                        reportNode = appendReportMenu(type, id, SystemObjects.ArtifactType, artifact.ArtifactTypeID);
                        if (reportNode != null) list.Add(reportNode);


                        list.Add(new PageActionItem { Context = followingParent ? "nullform" : ContextList.ActionCommand, CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = followText, Uri = followingParent ? "#" : string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id), Enabled = !followingParent ? true : false });
                        list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                        //list.Add(otherActionsItem);
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
                        }
                        reportNode = appendReportMenu(type, id, SystemObjects.ArtifactType, id);
                        if (reportNode != null) list.Add(reportNode);
                    }
                    else
                    {
                        reportNode = appendReportMenu(type, 0, type, 0);
                        if (reportNode != null) list.Add(reportNode);
                    }
                    list.Add(
                        new PageActionItem
                        {
                            Context = ContextList.ActionGenericReport,
                            Icon = "line-chart",
                            Title = "Metrics",
                            Uri = string.Format("/overlays/ArtifactListMetricsDashboard?id={0}", id)
                        }
                    );
                    list.Add(new PageActionItem { Context = ContextList.ActionCommand, CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id) });
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    //list.Add(new PageActionItem { Context = ContextList.ActionCommand, CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? "Unfollow Type" : "Follow Type", Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id) });
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
                        list.Add(new PageActionItem { Context = "Allocation", Icon = Resources.Actions.Allocation_Icon, Title = "Allocate Predicates", Uri = string.Format("/form/IntersectTypePredicateEditForm?id={0}", id) });
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
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
                        list.Add(new PageActionItem { Context = "command", CommandName = "follow", Icon = following ? Resources.Actions.Unfollow_Icon : Resources.Actions.Follow_Icon, Title = following ? Resources.Actions.Unfollow : Resources.Actions.Follow, Uri = string.Format("/resources/UpdateFollowStatus?type={0}&id={1}", type, id) });
                    }
                    list.Add(new PageActionItem { Context = "Audit", Icon = Resources.Actions.Audit_Icon, Title = Resources.Actions.Audit, Uri = string.Format("/overlays/{0}/{1}/audit", type.ToString(), id) });
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region Actions
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
                    //addItem = new PageActionItem { Context = "nullform", Icon = Resources.Actions.Add_Icon, Uri = "#" };
                    //addItem.Items.Add(new PageActionItem { Context = ContextList.ResponsibilityType, Icon = "lock", Title = ResponsibilityTypeGroup.People.ToString() + " Type", Uri = string.Format("/form/AddResponsibilityType?Group=1") });
                    //addItem.Items.Add(new PageActionItem { Context = ContextList.ResponsibilityType, Icon = "database", Title = ResponsibilityTypeGroup.Sourcing.ToString() + " Type", Uri = string.Format("/form/AddResponsibilityType?Group=2") });
                    //if (id > 0)
                    //{
                        //addItem.Items.Add(new PageActionItem { Context = ContextList.ResponsibilityTypeClaim, Icon = "key", Title = Resources.Actions.AddClaim_Text, Uri = string.Format("/form/AddResponsibilityTypeClaim?id={0}", id) });
                        //list.Add(addItem);
                    //}
                    //list.Add(addItem);
                    //list.Add(new PageActionItem { Context = "ResponsibilityTypeHierarchies", Icon = "tags", Title = "Type Order", Uri = "/overlays/ResponsibilityTypeHierarchies" });
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

                        var workflows = WorkflowType.CertifyArtifact.GetWorkflowTypeEnumList().Where(i => i.ID != WorkflowType.WorkIssue).ToList();

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
            var models = Company.Query<dynamic>(@"
with relations as	(
					select	'IntersectType' as [Type],
							IntersectTypeID as ID
					from	[utility].[RelationshipTypes]
					where	SourceObjectType = @type and SourceObjectID = @id
					union
					select	@type as [Type],
							@id as ID
					)
select		T.ID,
			T.Name
from		AttributeTypeRelation ATR
			inner join relations R on R.[Type] = ATR.ObjectType and R.ID = ATR.ObjectID
			inner join AttributeType T on T.ID = ATR.AttributeTypeID
where       T.ID not in (select ObjectID from FieldType where [Object] = 'AttributeType' and ObjectID = T.ID and [Type] in ('Html', 'Link', 'UncLink') and CHARINDEX(Name, T.TextFormatString) > 0)
group by	T.ID,
			T.Name
order by	T.Name
", new { type = type.ToString(), id = id });
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        [HttpGet, Route("{type}/{id:int}/{attributeTypeID:int}/attributefiltervalues")]
        public HttpResponseMessage GetFilterableAttributeValues(SystemObjects type, int id, int attributeTypeID)
        {
            var models = Company.Query<dynamic>(@"
with types as	(
				select	'Intersect' as [Object],
						IntersectID as ID
				from	cache.Relationships
				where	SourceType = @type and SourceTypeID = @id
				union
				select	[Object] as [Object],
						ObjectID
				from	cache.ObjectDetails
				where	ObjectType = @type 
						and ObjectTypeID = @id
				)
select	A.FormattedValue as Name 
from	AttributeDetail A
		inner join types O on O.[Object] = A.ObjectType and O.ID = A.ObjectID and A.AttributeTypeID = @attributeTypeID
group by A.FormattedValue
order by A.FormattedValue
", new { type = type.ToString(), id, attributeTypeID });
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

            // Dynamic fields
            var values = Company.GetFieldRelationsByObject(SystemObjects.Artifact, a.ID).ToList();
            values.ForEach(f =>
            {
                model.Add(f.Name, f.FormattedValue);
            });

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
        public DomainType GetDomainType(int id)
        {
            var model = Company.GetById<DomainType>(id);

            if (model == null)
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));

            return model;
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

        //[Route("{type}/{id:int}/events/headers/{groupID:int}/items")]
        //public List<Dictionary<string, object>> GetEventHeaderItems(SystemObjects type, int id, int groupID)
        //{
        //    return Company.GetEventsByGroupAsDictionary(groupID);
        //}

        //[Route("{type}/{id:int}/events/headers/{groupID:int}/layout")]
        //public GridLayout GetEventHeaderLayout(SystemObjects type, int id, int groupID)
        //{
        //    var fields = (
        //                 from g in Company.Filter<EventGroup>(i => i.ID == groupID)
        //                 join f in Company.Filter<FieldType>(i => i.Object == "Rule") on g.RuleID equals f.ObjectID
        //                 orderby f.SortOrder
        //                 orderby f.FriendlyName
        //                 select f
        //                 ).ToList();

        //    fields.Insert(0, new FieldType { FriendlyName = "ID", Name = "ID", SortOrder = 0, Type = "Number" });
        //    fields.Insert(0, new FieldType { FriendlyName = "Source ID", Name = "SourceID", SortOrder = 0, Type = "Text" });
        //    fields.Insert(0, new FieldType { FriendlyName = "Status", Name = "Status", SortOrder = 0, Type = "Text" });
        //    fields.Insert(0, new FieldType { FriendlyName = "Date", Name = "Date", SortOrder = 0, Type = "Date" });

        //    var model = new GridLayout(fields);
        //    return model;
        //}

        //[Route("resources/{id:int}/assignments")]
        //public IQueryable<EventHeader> GetAssignmentsByResource(int id)
        //{
        //    return Company.GetEventsByAssignedResource(id);
        //}

        //[Route("Rule/{id:int}/resolutions")]
        //public IQueryable<Resolution> GetResolutionsByRule(int id)
        //{
        //    return Company.Filter<Resolution>(i => i.RuleID == id);
        //}

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
            var fusionItemInfo = Company.Query<dynamic>(@"
                            select
	                            f.name as 'ItemName',
	                            f.fusionID as 'ID',
	                            f.parentID as 'ParentID',
	                            f.fusionattributetypeid as 'FusionAttributeTypeID',
	                            fu.fusiontypeid as 'FusionTypeID',
	                            fu.name as 'Name',
	                            fu.[description] as 'Description',
                                f.id as 'SelectedID'
                            from
	                            fusionattribute f
	                            inner join fusion fu on (f.fusionID = fu.id)
	                            left outer join fusionattribute fp on (f.parentID = fp.id)
                            where 
	                            f.id = @fusAttrID
                        ", new { fusAttrID = fusionAttributeID });
                        
            return Request.CreateResponse(HttpStatusCode.OK, fusionItemInfo);
        }

        [Route("fusion/selectedbreadcrumb/{selectedItemID:int}")]
        public HttpResponseMessage GetSelectedFusionBreadcrumb(int selectedItemID)
        {
            var itemPathData = new List<dynamic>();
                        
            int itemID = selectedItemID;
            
            while(itemID > 0)
            {
                var currentItem = Company.Query<dynamic>(@"
                                select
                                    f.parentID as 'parentID', 
	                                f.name as 'name', 
	                                f.id as 'id', 
	                                f.fusionattributetypeid as 'typeid',
	                                ft.name as 'typename'                                    
                                from fusionattribute f
                                inner
                                join fusionattributetype ft on (f.fusionattributetypeid = ft.id)
                                where f.id = @item", new { item = itemID }).FirstOrDefault();

                if (currentItem == null) throw new Exception("invalid item id specified to generate breadcrumb from");

                
                itemID = currentItem.parentID ?? default(int);
                itemPathData.Insert(0,currentItem);
            }

            return Request.CreateResponse(HttpStatusCode.OK, itemPathData);
        }
        
        [Route("fusion/ownership/ChildAttributeNodes"), HttpGet]
        public HttpResponseMessage GetOwnershipChildAttributeNodes(int fusionID, int targetFusionAttributeTypeID, int ruleID, int currentFusionAttributeTypeID = 0, int fusionAttributeID = 0)
        {
            var models = Company.Query<dynamic>(@"
declare @tbl table (ID int, ParentID int);

with at as	(
			select	ID,
					ParentID
			from	FusionAttributeType
			where	ID = @targetFusionAttributeTypeID
			union all
			select	P.ID,
					P.ParentID
			from	FusionAttributeType P
					inner join at C on C.ParentID = P.ID and P.ID <> C.ID
			)
insert into @tbl 
	select * from at

if @currentFusionAttributeTypeID = 0 and @fusionAttributeID = 0
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID is null and A.FusionAttributeTypeiD = t.ID and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.FusionAttributeID
                                from    FusionAttributeOwnerRuleItem RI
                                        inner join FusionAttributeOwnerRule R on R.ID = RI.FusionAttributeOwnerRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
                                )
		order by	A.Name
	end
else
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID = @currentFusionAttributeTypeID 
								and A.FusionAttributeTypeiD = t.ID 
								and A.ParentID = @fusionAttributeID
								and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.FusionAttributeID
                                from    FusionAttributeOwnerRuleItem RI
                                        inner join FusionAttributeOwnerRule R on R.ID = RI.FusionAttributeOwnerRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
                                )
        order by	Name
	end
", new { fusionID, targetFusionAttributeTypeID, ruleID, currentFusionAttributeTypeID, fusionAttributeID });

            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        [Route("fusion/promotion/ChildAttributeNodes"), HttpGet]
        public HttpResponseMessage GetPromotionChildAttributeNodes(int fusionID, int targetFusionAttributeTypeID, int ruleID, int currentFusionAttributeTypeID = 0, int fusionAttributeID = 0)
        {
            var models = Company.Query<dynamic>(@"
declare @tbl table (ID int, ParentID int);

with at as	(
			select	ID,
					ParentID
			from	FusionAttributeType
			where	ID = @targetFusionAttributeTypeID
			union all
			select	P.ID,
					P.ParentID
			from	FusionAttributeType P
					inner join at C on C.ParentID = P.ID and P.ID <> C.ID
			)
insert into @tbl 
	select * from at

if @currentFusionAttributeTypeID = 0 and @fusionAttributeID = 0
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID is null and A.FusionAttributeTypeiD = t.ID and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.FusionAttributeID
                                from    FusionAttributePromotionRuleItem RI
                                        inner join FusionAttributePromotionRule R on R.ID = RI.FusionAttributePromotionRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
                                )
		order by	A.Name
	end
else
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID = @currentFusionAttributeTypeID 
								and A.FusionAttributeTypeiD = t.ID 
								and A.ParentID = @fusionAttributeID
								and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.FusionAttributeID
                                from    FusionAttributePromotionRuleItem RI
                                        inner join FusionAttributePromotionRule R on R.ID = RI.FusionAttributePromotionRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
                                )
        order by	Name
	end
", new { fusionID, targetFusionAttributeTypeID, ruleID, currentFusionAttributeTypeID, fusionAttributeID });

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
            var models = Company.Query<dynamic>(@"
select	I.ID,
        I.FusionAttributeOwnerRuleID,
        I.FusionAttributeID,
        case 
			when F.FusionAttributeTypeID = FT.ID then F.TextPath
			else coalesce(FT.Name + ' attributes under ' + F.TextPath, 'All ' + FT.Name + ' attributes') 
		end as FusionAttributeName
from	FusionAttributeOwnerRuleItem I
		inner join FusionAttributeOwnerRule R on R.ID = I.FusionAttributeOwnerRuleID
		inner join FusionAttributeType FT on FT.ID = R.ObjectID
		left join FusionAttribute F on F.ID = I.FusionAttributeID
where   I.FusionAttributeOwnerRuleID = @id
", new { id });
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }
        
        [Route("fusion/{id:int}/PromotionRuleItems")]
        public HttpResponseMessage GetFusionAttributePromotionRuleItems(int id)
        {
            var models = Company.Query<dynamic>(@"
select	I.ID,
        I.FusionAttributePromotionRuleID,
        I.FusionAttributeID,
        case 
			when F.FusionAttributeTypeID = FT.ID then F.TextPath
			else coalesce(FT.Name + ' attributes under ' + F.TextPath, 'All ' + FT.Name + ' attributes') 
		end as FusionAttributeName
from	FusionAttributePromotionRuleItem I
		inner join FusionAttributePromotionRule R on R.ID = I.FusionAttributePromotionRuleID
		inner join FusionAttributeType FT on FT.ID = R.ObjectID
        left join FusionAttribute F on F.ID = I.FusionAttributeID
where   I.FusionAttributePromotionRuleID = @id
", new { id });
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        [Route("fusion/{id:int}/PromotionRuleMappings")]
        public HttpResponseMessage GetFusionAttributePromotionRuleMappings(int id)
        {
            var models = Company.Query<dynamic>(@"
select	I.ID,
        I.FusionAttributePromotionRuleID,
        I.SourceFieldTypeID,
        coalesce(I.SourceFieldName, SF.FriendlyName + ' (' + SF.Name + ')') as SourceFieldName,
        I.TargetFieldTypeID,
        coalesce(I.TargetFieldName, TF.FriendlyName + ' (' + TF.Name + ')') as TargetFieldName
from	FusionAttributePromotionRuleMapping I
		left join FieldType SF on SF.ID = I.SourceFieldTypeID
		left join FieldType TF on TF.ID = I.TargetFieldTypeID
where   I.FusionAttributePromotionRuleID = @id
", new { id });
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        #endregion

        #endregion

        #region Groups

        //[HttpGet, Route("groups")]
        //public IQueryable<Group> GetGroups()
        //{
        //    return Company.Table<Group>().OrderBy(i => i.Name).AsQueryable();
        //}

        [HttpGet, Route("groups")]
        public IQueryable<GroupSearchResultModel> GetGroups()
        {
            //if (!string.IsNullOrEmpty(search))
            //{
            //    search = search.Trim().ToLower();
            //    return Company.Filter<Group>(i => i.Name.Trim().ToLower().StartsWith(search))
            //            .OrderBy(i => i.Name)
            //            .Select(i => new GroupSearchResultModel  { 
            //                ID = i.ID, 
            //                Name = i.Name, 
            //                NumberOfMembers = i.ResourceGroups.Count, 
            //                IsMember = i.ResourceGroups.Any(r => r.ResourceID == Company.CurrentResourceID) 
            //            });
            //}
            //else
            //{
                return Company.Table<Group>()
                        .OrderBy(i => i.Name)
                        .Select(i => new GroupSearchResultModel
                        {
                            ID = i.ID,
                            Name = i.Name,
                            NumberOfMembers = i.ResourceGroups.Count,
                            IsMember = i.ResourceGroups.Any(r => r.ResourceID == Company.CurrentResourceID)
                        });
            //}
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
            return Company.Query<GroupResourceInfo>(@"select  RG.GroupID,
R.Email,
R.FirstName,
R.LastName,
R.ResourceID,
case 
    when G.PrimaryOwnerResourceID = R.ResourceID then 'Primary'
    when G.SecondaryOwnerResourceID = R.ResourceID then 'Secondary'
	else ''
end as [Owner]
from [Group] G
inner join ResourceGroup RG on RG.GroupID = G.ID and G.ID = @id
inner join reporting.Global_Resource R on R.ResourceID = RG.ResourceID", new { id })
        .OrderBy(i => i.LastName).ThenBy(i => i.FirstName).AsQueryable();
        }

        #endregion

        #region IntersectType

        [HttpGet, Route("IntersectTypePredicates/{id:int}")]
        public IQueryable<Predicate> GetAllocatedPredicates(int id)
        {
            var allocations = Company.IntersectTypePredicates.Where(p => p.IntersectTypeID == id);
            var predicates = Company.Predicates.Where(p => allocations.Select(a => a.PredicateID).Distinct().ToList().Contains(p.ID));

            var xx = predicates.ToList();

            return predicates;
        }

        [HttpGet, Route("IntersectTypePredicates/{id:int}/available")]
        public IQueryable<Predicate> GetAvailablePredicates(int id)
        {
            var allocated = GetAllocatedPredicates(id).ToList();

            var availableTypes = new List<int>();

            availableTypes.Add((int)MapType.SourceToTarget);
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

            var predicates = Company.Predicates.Where(p => availableTypes.Contains(p.Type));
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

        [Route("allitems")]
        public IEnumerable<dynamic> GetAllItems()
        {
            return Company.Query<dynamic>(@"select		[Object], 
			ObjectID, 
			ObjectTypeName + ': ' + TextPath as Name
from		cache.ObjectDetails 
where		[Object] in ('Artifact', 'Rule', 'Policy', 'Domain') 
			and ObjectID <> 0
order by	Name");
        }

        [Route("AttributeTypeCategories")]
        public IQueryable<AttributeTypeCategory> GetAttributeTypeCategories()
        {
            return Company.Table<AttributeTypeCategory>();
        }

        [Route("ResponsibilityTypeHierarchies")]
        public HttpResponseMessage GetResponsibilityTypeHierarchies()
        {
            var models = Company.Query<ResponsibilityTypeHierarchy>(
@"select	H.ID as StartID,
		    S.Name as StartName,
		    H.ParentID as EndID,
		    T.Name as EndName
from	    ResponsibilityTypeHierarchy H
		    inner join ResponsibilityType S on S.ID = H.ID
		    left join ResponsibilityType T on T.ID = H.ParentID");
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

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

        private int RenderFusionLookupField(FieldWithRelation k, List<ReadOnlyField> list, int currentRowNumber)
        {
            //load the definition of the field from the [FieldTypeFusionLookupDefinition] table
            int fusionAttributeID = 0;
            var def = Company.FieldTypeFusionLookupDefinitions.Where(x => x.FieldTypeID == k.FieldTypeID).FirstOrDefault();

            //field value has the fusion attribute this guy is associated with
            if (!int.TryParse(k.Value, out fusionAttributeID))
                return 0; // the value isnt populated or correct render nothing

            string sql, urlSql = string.Empty;
            //take def and look for intersects on the original id to the type specified

            if (def.IsParentChild)
            {
                sql = @"select 
	                        fa.id as ID,
	                        fa.parentId as ParentID,
	                        fa.name as name,
	                        fa.textpath as textpath,
	                        fa.sourceid as SourceID
                        from 
	                        fusionattribute fa 
                        where fa.parentid = @currentObject and fa.fusionattributetypeid = @targetAttributeTypeID;";

                urlSql = @"select 
	                            od.objectid as id,
	                            od.url as url
                            from 
	                            fusionattribute fa 
	                            inner join cache.ObjectDetails od on (od.objectid = fa.id and od.objecttypeid = @targetAttributeTypeID and od.[object] = 'FusionAttribute')
                            where fa.parentid = @currentObject and fa.fusionattributetypeid = @targetAttributeTypeID";
            }
            else
            {
                sql = @"select 	
                            fa.id as ID,
                            fa.parentId as ParentID,
	                        fa.name as name,
	                        fa.textpath as textpath,
                            fa.sourceid as SourceID
                        from 
	                        [intersectnode] inode
	                        inner join [intersectnode] inode2 on (inode.intersectid = inode2.intersectid and inode.objectid != inode2.objectid and inode2.objecttype = 'FusionAttribute')
	                        inner join [fusionattribute] fa on (fa.id = inode2.objectid and fa.fusionattributetypeid = @targetAttributeTypeID)
                        where inode.objectid = @currentObject and inode.objecttype = 'FusionAttribute'";

                urlSql = @" select 	
                                od.objectid as id,
					            od.url as url
                            from 
	                            [intersectnode] inode
	                            inner join [intersectnode] inode2 on (inode.intersectid = inode2.intersectid and inode.objectid != inode2.objectid and inode2.objecttype = 'FusionAttribute')
	                            inner join cache.ObjectDetails od on (od.objectid = inode2.objectid and od.objecttypeid = @targetAttributeTypeID and od.[object] = 'FusionAttribute')
                            where inode.objectid = @currentObject and inode.objecttype = 'FusionAttribute'";
            }

            var results = Company.Query<FusionAttribute>(sql, new { currentObject = fusionAttributeID, targetAttributeTypeID = def.TargetFusionAttributeTypeID });

            //get all the items urls in one query otherwise its too slow

            var urlDict = Company.Query<dynamic>(urlSql, new { currentObject = fusionAttributeID, targetAttributeTypeID = def.TargetFusionAttributeTypeID }).ToDictionary(
                        row => (int)row.id,
                        row => (string)row.url
                    );
                        

            if (results.Count() > 1 || def.FieldTypeFusionLookupDisplayFields != null && def.FieldTypeFusionLookupDisplayFields.Count > 0)
            {
                var ro = new ReadOnlyField
                {
                    Row = currentRowNumber++,
                    Column = 1,
                    Name = k.FriendlyName,
                    FieldDescription = k.DisplayDescription,
                    FieldName = k.Name,
                    FusionLookupGridUrl = string.Format("api/FusionLookupField/{0}/{1}/values", fusionAttributeID, def.ID)
                };

                list.Add(ro);

                return currentRowNumber;
            }

            string url = null;
            string value = string.Empty;

            var firstItem = results.FirstOrDefault();

            if (firstItem != null)
            {
                var ro = new ReadOnlyField
                {
                    Row = currentRowNumber++,
                    Column = 1,
                    Name = k.FriendlyName,
                    FieldDescription = k.DisplayDescription,
                    FieldName = k.Name                  
                };

                urlDict.TryGetValue(firstItem.ID, out url);


                if (!string.IsNullOrEmpty(url))
                {
                    value = string.Format("<a href='{1}'>{0}</a>", (def.Display == "TextPath" ? firstItem.TextPath : firstItem.Name), url);
                }
                else
                {
                    value = (def.Display == "TextPath" ? firstItem.TextPath : firstItem.Name);
                }

                ro.Value = value;

                list.Add(ro);
            }

            return currentRowNumber;

        }

        [Route("FusionLookupField/{sourceFusionAttributeID:int}/{fieldTypeFusionLookupDefinitionID:int}/values")]
        public HttpResponseMessage GetFusionLookupField(int sourceFusionAttributeID, int fieldTypeFusionLookupDefinitionID)
        {
            var columns = new List<GridColumn>();
            var values = new List<dynamic>();

            var def = Company.GetById<FieldTypeFusionLookupDefinition>(fieldTypeFusionLookupDefinitionID);

            if (def == null) throw new Exception("Invalid fusion lookup field id specified");

            var fieldTypeIds = def.FieldTypeFusionLookupDisplayFields.Select(x => x.FieldTypeID);

            if (fieldTypeIds != null)
            {
                var fieldTypeInfo = (from f in Company.FieldTypes
                                     where fieldTypeIds.Contains(f.ID)
                                     select f);

                foreach (var fieldType in fieldTypeInfo)
                {
                    columns.Add(new GridColumn { text = fieldType.FriendlyName, datafield = fieldType.ID.ToString(), width = "auto" });
                }
            }
            
            //load the values
            string sql = string.Empty;

            if (def.IsParentChild)
            {
                sql = @"select 	                        
	                        fa.name as name,
	                        fa.textpath as textpath,
                            od.url as fusionUrl,
                            fa.id as id
                        from 
	                        fusionattribute fa 
                            inner join cache.ObjectDetails od on (od.objectid = fa.id and od.objecttypeid = fa.fusionattributetypeid and od.[object] = 'FusionAttribute')
                        where fa.parentid = @currentObject and fa.fusionattributetypeid = @targetAttributeTypeID;";
            }
            else
            {
                sql = @"select 	                            
	                        fa.name as name,
	                        fa.textpath as textpath,
                            od.url as fusionUrl,
                            fa.id as id
                        from 
	                        [intersectnode] inode
	                        inner join [intersectnode] inode2 on (inode.intersectid = inode2.intersectid and inode.objectid != inode2.objectid and inode2.objecttype = 'FusionAttribute')
	                        inner join [fusionattribute] fa on (fa.id = inode2.objectid and fa.fusionattributetypeid = @targetAttributeTypeID)
                            inner join cache.ObjectDetails od on (od.objectid = fa.id and od.objecttypeid = fa.fusionattributetypeid and od.[object] = 'FusionAttribute')
                        where inode.objectid = @currentObject and inode.objecttype = 'FusionAttribute'";
            }

            var results = Company.Query<dynamic>(sql, new { currentObject = sourceFusionAttributeID, targetAttributeTypeID = def.TargetFusionAttributeTypeID });

            IEnumerable<int> fusionAttributeIDs = results.Select(x => x.id).Cast<int>();


            var fieldValues = (from f in Company.Fields
                               where f.ObjectType == "FusionAttribute"
                                         &&
                                     fusionAttributeIDs.Contains(f.ObjectID)
                                         &&
                                     fieldTypeIds.Contains(f.FieldTypeID)
                               select f).ToDictionary(kvp => (kvp.ObjectID.ToString() + "_" + kvp.FieldTypeID.ToString()), kvp => kvp);


            foreach (var row in results)
            {
                dynamic r = new ExpandoObject();

                r.Name = (def.Display == "TextPath" ? row.textpath : row.name.ToString());
                r.Url = row.fusionUrl.ToString();

                if (fieldTypeIds != null && fieldValues != null)
                {
                    foreach (var item in fieldTypeIds)
                    {
                        int fusionAttributeID = row.id;

                        Field field = null;

                        if (fieldValues.TryGetValue(fusionAttributeID.ToString() + "_" + item, out field))
                        {
                            ((IDictionary<string, object>)r)[item.ToString()] = field.FormattedValue;
                        }                        
                    }
                }

                values.Add(r);
            }

            //create array of Name + any fields configured.
            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                Values = values,
                Columns = columns
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

        [Route("{type}/{id:int}/relationships/{targetType}/{targetID:int}/{criticalOnly:bool=false?}"), HttpGet]
        public IQueryable<Relationship> GetRelationshipsForObjectByTargetType(SystemObjects type, int id, SystemObjects targetType, int targetID, bool criticalOnly)
        { 
            var sType = type.ToString();
            var tType = targetType.ToString();
            return Company.Filter<Relationship>(i => i.SourceObjectType == sType && i.SourceObjectID == id && i.TargetType == tType && i.TargetTypeID == targetID && ((i.Classification == IntersectClassification.Critical && criticalOnly) || !criticalOnly));
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

            var sql = @"
select	J.IntersectMapID,
		J.SourceRuleID,
		S.Name,
		R.SourceObject,
		R.SourceObjectID,
		R.SourceObjectName,
		R.SourceTypeName,
		J.Description,
		(
		select substring(
						(
						SELECT  ', ' + D.TextPath AS 'data()' 
						from	SourceRuleContext JI
								inner join cache.ObjectDetails D on JI.Object = D.Object and JI.ObjectID = D.ObjectID and JI.SourceRuleID = J.ID
						FOR		XML PATH('')
						), 2, 2500)
		) as RuleContexts,
		(
		select substring(
						(
						SELECT  ', ' + D.TextPath AS 'data()' 
						from	IntersectMapSourceRuleContext JI
								inner join cache.ObjectDetails D on JI.Object = D.Object and JI.ObjectID = D.ObjectID and JI.IntersectMapSourceRuleID = J.ID
						FOR		XML PATH('')
						), 2, 2500)
		) as ItemContexts,
		J.SortOrder
from	IntersectMapSourceRule J
		inner join SourceRule S on S.ID = J.SourceRuleID and S.Object = @focal and S.ObjectID = @focalID 
		inner join IntersectMap M on J.IntersectMapID = M.ID
		inner join cache.Relationships R on R.SourceIntersectNodeID = M.SubjectIntersectNodeID and R.TargetObject = @obj and R.TargetObjectID = @objID";
            var rawItems = Company.Query<RawSourceRuleItem>(sql, new { focal, focalID, obj, objID }).OrderBy(i => i.Name).ThenBy(i => i.SortOrder).ToList();

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


        private List<int> LoadAttributes(int intersectTypeID)
        {                        
            var list = new List<int>();
            
            {
                list = (
                        from t in Company.AttributeTypes
                        join r in Company.AttributeTypeRelations on t.ID equals r.AttributeTypeID
                        where r.ObjectType == "IntersectType" && r.ObjectID == intersectTypeID
                        select r.AttributeTypeID).ToList();                
            }
            
            return list;
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
        public PolicyType GetPolicyType(int id)
        {
            return Company.GetById<PolicyType>(id);
        }

        [Route("policytypes/{id:int}/policies")]
        public List<Policy> GetPoliciesByType(int id)
        {
            var list = Company.Filter<Policy>(i => i.PolicyTypeID == id).OrderBy(i => i.TextPath).ToList();
           // var type = Company.GetById<PolicyType>(id);
            //foreach (var p in list.Where(i => !i.ParentID.HasValue))
            //{
            //    p.ParentID = 0;
            //}
            //list.Add(new Policy { PolicyTypeID = id, ID = 0, Name = ((type != null) ? type.Name : "Root") });

            return list;
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
            return Company.Table<GlobalReportingResource>();
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

        [Route("resources/me/redflagsummaries")]
        public IEnumerable<RedFlagSummariesByResource> GetResource()
        {
            return Company.GetRedFlagSummariesByCurrentResource();
        }

        #endregion

        #region Rules

        [Route("rules")]
        public IQueryable<Rule> GetRules()
        {
            return Company.Table<Rule>();
        }

        #endregion

        #region Tags

        [DataContract]
        public class TagSuggestionModel
        {
            [DataMember]
            public string Object { get; set; }

            [DataMember]
            public int ObjectID { get; set; }

            [DataMember]
            public string TextPath { get; set; }

            [DataMember]
            public string Url { get; set; }

            [DataMember]
            public string ObjectTypeName { get; set; }

            [DataMember]
            public string IconForeColor { get; set; }

            [DataMember]
            public string IconBackColor { get; set; }
        }

        [HttpGet, Route("tagsuggestions")]
        public List<TagSuggestionModel> TagSuggestions(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return new List<TagSuggestionModel>();

            var sql = string.Format(@"select [Object], ObjectID, TextPath, Url, ObjectTypeName, IconForeColor, IconBackColor from cache.ObjectDetails where [Object] not in ('FusionAttribute', 'Intersect') and (lower(Name) like lower('{0}%') or (len('{0}') > 2 and lower(Name) like lower('%{0}%')))", phrase.Replace("'", "''").Replace("--", ""));

            var list = Company.Query<TagSuggestionModel>(sql).ToList();

            return list;
        }

        [Route("tags")]
        public IQueryable<Tag> GetTags()
        {
            return Company.Table<Tag>();
        }

        [Route("tags/{type}/{id:int}")]
        public IQueryable<Tag> GetTagsByObject(SystemObjects type, int id)
        { 
            var sType = type.ToString();
            return Company.Filter<TagRelation>(i => i.Object == sType && i.ObjectID == id).Select(i => i.Tag);
        }

        [HttpPost, Route("tags/{type}/{id:int}")]
        public HttpResponseMessage RelateNewTagToObject(SystemObjects type, int id, Tag model)
        {
            var sType = type.ToString();
            model.Name = model.Name.Trim();
            var tag = Company.Filter<Tag>(i => i.Name == model.Name).SingleOrDefault();
            if (tag == null)
            {
                tag = new Tag { Name = model.Name };
                Company.Add<Tag>(tag);
            }

            if (tag.ID > 0)
            {
                var tagRelation = new TagRelation { Object = sType, ObjectID = id, TagID = tag.ID };
                Company.Add<TagRelation>(tagRelation);
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { context = "Tag" });
        }

        [HttpPut, Route("tags/{type}/{id:int}")]
        public HttpResponseMessage RelateExistingTagToObject(SystemObjects type, int id, Tag model)
        {
            var sType = type.ToString();
            if (!Company.Filter<TagRelation>(i => i.Object == sType && i.ObjectID == id && i.TagID == model.ID).Any())
            {
                var tagRelation = new TagRelation { Object = sType, ObjectID = id, TagID = model.ID };
                Company.Add<TagRelation>(tagRelation);
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { context = "Tag" });
        }

        #endregion

        #region Type/ID Endpoints

        [Route("{type}/{id:int}")]
        public ObjectDetail GetObjectDetail(SystemObjects type, int id)
        {
            return Company.GetObjectDetail(type, id);
        }

        [Route("Artifact/{id:int}/artifacts/statistics")]
        public List<ChildArtifactStatisticsByObject> GetChildArtifactTileStatistics(int id)
        {
            return Company.GetChildArtifactStatisticsByObject(id);
        }

        [Route("{type}/{id:int}/flags")]
        public HttpResponseMessage GetFlags(SystemObjects type, int id)
        {
            var flag = Company.GetActiveAlertFlagByObject(type, id);
            return Request.CreateResponse(HttpStatusCode.OK, 
                new {
                    RedFlagged = (flag != null) ? flag.Active : false, 
                    RedFlaggedOn = (flag != null) ? flag.Date : DateTime.MinValue
                });
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
        public HttpResponseMessage GetObjectDetailFields(SystemObjects type, int id)
        {
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
                        //list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = artifact.GetName(i => i.Name), FieldName = "ArtifactName", FieldDescription = artifact.GetDescription(i => i.Description), Value = artifact.Name });
                        //list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = artifact.GetName(i => i.Status), FieldName = "ArtifactStatus", FieldDescription = artifact.GetDescription(i => i.Status), Value = artifact.Status });

                        row = 1;
                        if (!string.IsNullOrEmpty(artifact.Description))
                        {

                            list.Add(new ReadOnlyField { Row = row, Column = 1, Name = artifact.GetName(i => i.Description), FieldName = "ArtifactDescription", FieldDescription = artifact.GetDescription(i => i.Description), Value = artifact.Description });
                            row++;
                        }

                        //list.Add(new ReadOnlyField { Row = row, Column = 1, Name = artifact.GetName(i => i.ArtifactTypeID), FieldName = "ArtifactArtifactType", FieldDescription = artifact.GetDescription(i => i.ArtifactTypeID), Value = artifact.ArtifactType.Name });
                        list.Add(new ReadOnlyField { Row = row, Column = 1, Name = Resources.FieldInfo.TaxonomyType_Name, ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID", FieldName = "ArtifactTaxonomyType", FieldDescription = artifact.GetDescription(i => i.TaxonomyTypeID), Value = artifact.TaxonomyType.Name });
                        var nodes = "None assigned";
                        var owningModels = Company.Filter<Relationship>(i => i.SourceObjectType == "Artifact" && i.SourceObjectID == id && i.TargetType == "TaxonomyType" && i.TargetTypeID == artifact.TaxonomyTypeID).Select(i => new { i.TargetUrl, i.TargetName, i.TargetObjectID }).OrderBy(i => i.TargetName).ToList();
                        if (owningModels.Count > 0)
                        {
                            nodes = "";
                            owningModels.ForEach(i =>
                            {
                                nodes += string.Format("<div><a data-context='Preview' data-type='Taxonomy' data-id='{2}' href='{0}'>{1}</a></div>", i.TargetUrl, i.TargetName, i.TargetObjectID);
                            });
                        }
                        list.Add(new ReadOnlyField { Row = row, Column = 2, Name = Resources.FieldInfo.TaxonomyType_Name + " Nodes", ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeIDNodes", FieldName = "ArtifactTaxonomyTypeNodes", Value = nodes });

                        row++;

                        row = loadDynamicDisplayFields(list, type, id, row);
                    }
                    artifact = null;
                    break;
                    #endregion
                case SystemObjects.ArtifactType:
                    #region Fields
                    var artifactType = Company.GetById<ArtifactType>(id);
                    if (artifactType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = artifactType.GetName(i => i.Name), FieldName = "ArtifactTypeName", FieldDescription = artifactType.GetDescription(i => i.Name), Value = artifactType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = artifactType.GetName(i => i.ID), FieldName = "ArtifactTypeID", FieldDescription = artifactType.GetDescription(i => i.ID), Value = artifactType.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = artifactType.GetName(i => i.Description), FieldName = "ArtifactTypeDescription", FieldDescription = artifactType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(artifactType.Description) ? "None provided" : artifactType.Description });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = artifactType.GetName(i => i.CanOwnFusion), FieldName = "ArtifactTypeCanOwnFusion", FieldDescription = artifactType.GetDescription(i => i.CanOwnFusion), Value = artifactType.CanOwnFusion.FormatBooleanReadOnlyValue() });

                        list.Add(new ReadOnlyField { Row = 4, Column = 1, Name = artifactType.GetName(i => i.AllowRelatedArtifacts), FieldName = "ArtifactTypeAllowRelatedArtifacts", FieldDescription = artifactType.GetDescription(i => i.AllowRelatedArtifacts), Value = artifactType.AllowRelatedArtifacts.FormatBooleanReadOnlyValue() });
                    }
                    artifactType = null;
                    break;
                    #endregion
                case SystemObjects.Attribute:
                    #region Fields
                    var attr = Company.GetById<core.entities.Attribute>(id);
                    if (attr != null)
                    {
                        row = loadDynamicDisplayFields(list, type, id, 1);
                    }
                    attr = null;
                    break;
                    #endregion
                case SystemObjects.AttributeType:
                    #region Fields
                    var attributeType = Company.GetById<AttributeType>(id);
                    if (attributeType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = attributeType.GetName(i => i.ID), FieldName = "AttributeTypeID", FieldDescription = attributeType.GetDescription(i => i.ID), Value = attributeType.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = attributeType.GetName(i => i.Name), FieldName = "AttributeTypeName", FieldDescription = attributeType.GetDescription(i => i.Name), Value = attributeType.Name });
                        list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = attributeType.GetName(i => i.TextFormatString), FieldName = "AttributeTypeTextFormatString", FieldDescription = attributeType.GetDescription(i => i.TextFormatString), Value = attributeType.TextFormatString });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = attributeType.GetName(i => i.Description), FieldName = "AttributeTypeDescription", FieldDescription = attributeType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(attributeType.Description) ? "None provided" : attributeType.Description });
                    }
                    attributeType = null;
                    break;
                    #endregion
                case SystemObjects.Domain:
                    #region Fields
                    var domain = Company.GetById<Domain>(id, i => i.DomainType, i => i.DomainGroup);
                    if (domain != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = domain.GetName(i => i.Name), FieldName = "DomainGroupName", FieldDescription = domain.GetDescription(i => i.Name), Value = domain.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = domain.GetName(i => i.ID), FieldName = "DomainGroupID", FieldDescription = domain.GetDescription(i => i.ID), Value = domain.ID.ToString() });

                        if (!string.IsNullOrEmpty(domain.Description))
                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = domain.GetName(i => i.Description), FieldName = "DomainGroupDescription", FieldDescription = domain.GetDescription(i => i.Description), Value = domain.Description });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = domain.GetName(i => i.DomainType), FieldName = "DomainGroupDomainType", FieldDescription = domain.GetDescription(i => i.DomainType), Value = domain.DomainType.Name });
                    }
                    domain = null;
                    break;
                    #endregion
                case SystemObjects.DomainGroup:
                    #region Fields
                    var domainGroup = Company.GetById<DomainGroup>(id, d => d.DomainType);
                    if (domainGroup != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = domainGroup.GetName(i => i.Name), FieldName = "DomainGroupName", FieldDescription = domainGroup.GetDescription(i => i.Name), Value = domainGroup.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = domainGroup.GetName(i => i.ID), FieldName = "DomainGroupID", FieldDescription = domainGroup.GetDescription(i => i.ID), Value = domainGroup.ID.ToString() });

                        if (!string.IsNullOrEmpty(domainGroup.Description))
                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = domainGroup.GetName(i => i.Description), FieldName = "DomainGroupDescription", FieldDescription = domainGroup.GetDescription(i => i.Description), Value = domainGroup.Description });

                        if (domainGroup.MasterListID.HasValue)
                        {
                            var groupMasterList = Company.GetById<Domain>(domainGroup.MasterListID.Value);
                            list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = domainGroup.GetName(i => i.MasterListID), FieldName = "DomainGroupMasterListID", FieldDescription = domainGroup.GetDescription(i => i.MasterListID), Value = groupMasterList.Name });
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
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = domainType.GetName(i => i.Name), FieldName = "DomainTypeName", FieldDescription = domainType.GetDescription(i => i.Name), Value = domainType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = domainType.GetName(i => i.ID), FieldName = "DomainTypeID", FieldDescription = domainType.GetDescription(i => i.ID), Value = domainType.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = domainType.GetName(i => i.Description), FieldName = "DomainTypeDescription", FieldDescription = domainType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(domainType.Description) ? "None provided" : domainType.Description });
                    }
                    domainType = null;
                    break;
                    #endregion
                case SystemObjects.Group:
                    #region Fields
                    var group = Company.GetById<Group>(id);
                    if (group != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = group.GetName(i => i.Name), FieldName = "GroupName", FieldDescription = group.GetDescription(i => i.Name), Value = group.Name });

                        if (group.PrimaryOwnerResourceID.HasValue && group.SecondaryOwnerResourceID.HasValue)
                        {
                            var groupOwnerIDs = new List<int>();
                            if (group.PrimaryOwnerResourceID.HasValue) groupOwnerIDs.Add(group.PrimaryOwnerResourceID.Value);
                            if (group.SecondaryOwnerResourceID.HasValue) groupOwnerIDs.Add(group.SecondaryOwnerResourceID.Value);

                            var groupOwners = GetCompanyResources().Where(i => groupOwnerIDs.Contains(i.ID)).ToList();

                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = group.GetName(i => i.PrimaryOwnerResourceID), FieldName = "GroupOwner", FieldDescription = group.GetDescription(i => i.PrimaryOwnerResourceID), Value = groupOwners.Single(i => i.ID == group.PrimaryOwnerResourceID.Value).FormatDisplayName() });
                            list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = group.GetName(i => i.SecondaryOwnerResourceID), FieldName = "GroupOwner", FieldDescription = group.GetDescription(i => i.SecondaryOwnerResourceID), Value = groupOwners.Single(i => i.ID == group.SecondaryOwnerResourceID.Value).FormatDisplayName() });                        
                        }

                        if (!string.IsNullOrEmpty(group.Description))
                            list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = group.GetName(i => i.Description), FieldName = "GroupDescription", FieldDescription = group.GetDescription(i => i.Description), Value = group.Description });
                    }
                    group = null;
                    break;
                    #endregion
                case SystemObjects.FieldType:
                    #region Fields
                    var fieldType = Company.GetById<FieldType>(id);
                    if (fieldType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = fieldType.GetName(i => i.Name), FieldName = "FieldTypeName", FieldDescription = fieldType.GetDescription(i => i.Name), Value = fieldType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = fieldType.GetName(i => i.FriendlyName), FieldName = "FieldTypeFriendlyName", FieldDescription = fieldType.GetDescription(i => i.FriendlyName), Value = fieldType.FriendlyName });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = fieldType.GetName(i => i.Type), FieldName = "FieldTypeType", FieldDescription = fieldType.GetDescription(i => i.Type), Value = fieldType.Type });

                        if (!string.IsNullOrEmpty(fieldType.Pattern)) list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = fieldType.GetName(i => i.Pattern), FieldName = "FieldTypePattern", FieldDescription = fieldType.GetDescription(i => i.Pattern), Value = fieldType.Pattern });
                        if (fieldType.MinimumLength.HasValue) list.Add(new ReadOnlyField { Row = 4, Column = 1, Name = fieldType.GetName(i => i.MinimumLength), FieldName = "FieldTypeMinimumLength", FieldDescription = fieldType.GetDescription(i => i.MinimumLength), Value = fieldType.MinimumLength.Value.ToString() });
                        if (fieldType.MaximumLength.HasValue) list.Add(new ReadOnlyField { Row = 4, Column = 2, Name = fieldType.GetName(i => i.MaximumLength), FieldName = "FieldTypeMaximumLength", FieldDescription = fieldType.GetDescription(i => i.MaximumLength), Value = fieldType.MaximumLength.Value.ToString() });

                        if (!string.IsNullOrEmpty(fieldType.LookupObjectType))
                        {
                            list.Add(new ReadOnlyField { Row = 5, Column = 1, Name = fieldType.GetName(i => i.LookupObjectType), FieldName = "FieldTypeLookupObjectType", FieldDescription = fieldType.GetDescription(i => i.LookupObjectType), Value = fieldType.LookupObjectType });
                            if (fieldType.LookupObjectID.HasValue)
                                list.Add(new ReadOnlyField { Row = 5, Column = 2, Name = fieldType.GetName(i => i.LookupObjectID), FieldName = "FieldTypeLookupObjectID", FieldDescription = fieldType.GetDescription(i => i.LookupObjectID), Value = fieldType.LookupObjectID.ToString() });
                            if (!string.IsNullOrEmpty(fieldType.LookupDisplayFormat))
                                list.Add(new ReadOnlyField { Row = 6, Column = 1, Name = fieldType.GetName(i => i.LookupDisplayFormat), FieldName = "FieldTypeLookupDisplayFormat", FieldDescription = fieldType.GetDescription(i => i.LookupDisplayFormat), Value = fieldType.LookupDisplayFormat });
                        }

                        if (!string.IsNullOrEmpty(fieldType.DisplayDescription))
                            list.Add(new ReadOnlyField { Row = 7, Column = 1, Name = fieldType.GetName(i => i.DisplayDescription), FieldName = "FieldTypeDisplayDescription", FieldDescription = fieldType.GetDescription(i => i.DisplayDescription), Value = fieldType.DisplayDescription });

                        if (!string.IsNullOrEmpty(fieldType.FormDescription))
                            list.Add(new ReadOnlyField { Row = 7, Column = 1, Name = fieldType.GetName(i => i.FormDescription), FieldName = "FieldTypeFormDescription", FieldDescription = fieldType.GetDescription(i => i.FormDescription), Value = fieldType.FormDescription });
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
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = fusion.GetName(i => i.Name), FieldName = "FusionName", FieldDescription = fusion.GetDescription(i => i.Name), Value = fusion.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = fusion.GetName(i => i.ID), FieldName = "FusionID", FieldDescription = fusion.GetDescription(i => i.ID), Value = fusion.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = fusion.GetName(i => i.Description), FieldName = "FusionDescription", FieldDescription = fusion.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(fusion.Description) ? "None provided" : fusion.Description });

                        row = 3;
                        foreach (var k in fusionFields)
                        {
                            list.Add(new ReadOnlyField { Row = row, Column = 1, Name = k.FriendlyName, FieldName = "Fusion" + k.Name, FieldDescription = k.DisplayDescription, Value = k.FormattedValue });
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
                        list.Add(new ReadOnlyField { Row = row, Column = 1, Name = fusionAttribute.GetName(i => i.Name), FieldName = "FAName", FieldDescription = fusionAttribute.GetDescription(i => i.Name), Value = fusionAttribute.Name });
                        row++;
                        list.Add(new ReadOnlyField { Row = row, Column = 1, Name = fusionAttribute.GetName(i => i.TextPath), FieldName = "FATextPath", FieldDescription = fusionAttribute.GetDescription(i => i.TextPath), Value = fusionAttribute.TextPath });
                        row++;
                        row = loadDynamicDisplayFields(list, type, id, row);
                        row = loadDisplayableRelationshipsAsFields(list, type, id, row);
                    }
                    fusionAttribute = null;
                    break;
                    #endregion
                case SystemObjects.FusionAttributeType:
                    #region Fields
                    var fusionAttributeType = Company.GetById<FusionAttributeType>(id, i => i.FusionType);
                    if (fusionAttributeType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = fusionAttributeType.GetName(i => i.Name), FieldName = "FATName", FieldDescription = fusionAttributeType.GetDescription(i => i.Name), Value = fusionAttributeType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = fusionAttributeType.GetName(i => i.ID), FieldName = "FATID", FieldDescription = fusionAttributeType.GetDescription(i => i.ID), Value = fusionAttributeType.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = fusionAttributeType.GetName(i => i.FusionType), FieldName = "FATFusionType", FieldDescription = fusionAttributeType.GetDescription(i => i.FusionType), Value = fusionAttributeType.FusionType.Name });
                        list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = fusionAttributeType.GetName(i => i.TextPath), FieldName = "FATTextPath", FieldDescription = fusionAttributeType.GetDescription(i => i.TextPath), Value = fusionAttributeType.TextPath });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = fusionAttributeType.GetName(i => i.Assignable), FieldName = "FATAssignable", FieldDescription = fusionAttributeType.GetDescription(i => i.Assignable), Value = fusionAttributeType.Assignable.FormatBooleanReadOnlyValue() });
                        //list.Add(new ReadOnlyField { Row = 4, Column = 2, Name = fusionAttributeType.GetName(i => i.Tab), FieldName = "FATTab", FieldDescription = fusionAttributeType.GetDescription(i => i.Tab), Value = fusionAttributeType.Tab });
                    }
                    fusionAttributeType = null;
                    break;
                    #endregion
                case SystemObjects.FusionExecution:
                    #region Fields
                    var fusionExecution = Company.GetById<FusionExecution>(id);
                    if (fusionExecution != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = "Date Started", FieldName = "DateStarted", Value = fusionExecution.DateStarted.HasValue ? JsonConvert.SerializeObject(fusionExecution.DateStarted.Value) : "Not started" });                        
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = "Date Completed", FieldName = "DateCompleted", Value = fusionExecution.DateCompleted.HasValue ? JsonConvert.SerializeObject(fusionExecution.DateCompleted.Value) : "Not completed" });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = "# Added", FieldName = "Adds", Value = fusionExecution.Adds.HasValue ? fusionExecution.Adds.Value.ToString() : ""});
                        list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = "# Updated", FieldName = "Updates", Value = fusionExecution.Updates.HasValue ? fusionExecution.Updates.Value.ToString() : "" });
                        list.Add(new ReadOnlyField { Row = 2, Column = 3, Name = "# Deleted", FieldName = "Deletes", Value = fusionExecution.Deletes.HasValue ? fusionExecution.Deletes.Value.ToString() : "" });
                    }
                    fusionExecution = null;
                    break;
                    #endregion
                case SystemObjects.FusionType:
                    #region Fields
                    var fusionType = Company.GetById<FusionType>(id);
                    if (fusionType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = fusionType.GetName(i => i.Name), FieldName = "FusionTypeName", FieldDescription = fusionType.GetDescription(i => i.Name), Value = fusionType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = fusionType.GetName(i => i.ID), FieldName = "FusionTypeID", FieldDescription = fusionType.GetDescription(i => i.ID), Value = fusionType.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = fusionType.GetName(i => i.Description), FieldName = "FusionTypeDescription", FieldDescription = fusionType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(fusionType.Description) ? "None provided" : fusionType.Description });
                    }
                    fusionType = null;
                    break;
                    #endregion
                case SystemObjects.Intersect:
                    #region Fields                    
                    var intersect = Company.GetById<Intersect>(id);
                    if (intersect != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = intersect.GetName(i => i.Classification), FieldName = "IntersectClassification", FieldDescription = intersect.GetDescription(i => i.Classification), Value = (Enum.IsDefined(typeof(IntersectClassification), intersect.Classification.GetValueOrDefault(IntersectClassification.Normal)) ? intersect.Classification.GetValueOrDefault(IntersectClassification.Normal).ToString() : IntersectClassification.Normal.ToString()) });
                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = intersect.GetName(i => i.Description), FieldName = "IntersectDescription", FieldDescription = intersect.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(intersect.Description) ? "None provided" : intersect.Description });
                    }
                    intersect = null;
                    break;
                    #endregion
                case SystemObjects.IntersectType:
                    #region Fields
                    var intersectType = Company.GetById<IntersectType>(id);
                    if (intersectType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = intersectType.GetName(i => i.Name), FieldName = "IntersectTypeName", FieldDescription = intersectType.GetDescription(i => i.Name), Value = intersectType.Name });

                        //list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = intersectType.GetName(i => i.ReadOnly), FieldName = "IntersectTypeReadOnly", FieldDescription = intersectType.GetDescription(i => i.ReadOnly), Value = intersectType.ReadOnly.FormatBooleanReadOnlyValue() });
                        //list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = intersectType.GetName(i => i.AllowGrouping), FieldName = "IntersectTypeAllowGrouping", FieldDescription = intersectType.GetDescription(i => i.AllowGrouping), Value = intersectType.AllowGrouping.FormatBooleanReadOnlyValue() });

                        //list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = intersectType.GetName(i => i.IsTechnical), FieldName = "IntersectTypeIsTechnical", FieldDescription = intersectType.GetDescription(i => i.IsTechnical), Value = intersectType.IsTechnical.FormatBooleanReadOnlyValue() });
                        //list.Add(new ReadOnlyField { Row = 3, Column = 2, Name = intersectType.GetName(i => i.AllowSourcing), FieldName = "IntersectTypeAllowSourcing", FieldDescription = intersectType.GetDescription(i => i.AllowSourcing), Value = intersectType.AllowSourcing.FormatBooleanReadOnlyValue() });

                    }
                    intersectType = null;
                    break;
                #endregion
                case SystemObjects.Load:
                    #region Fields
                    var load = Company.GetLoadDetail(id);
                    if (load != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = "Action", FieldName = "LoadAction", FieldDescription = "", Value = load.Action });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = "Target", FieldName = "LoadObjectName", FieldDescription = "", Value = load.ObjectName });
                        //list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = "Date Started", FieldName = "LoadDateStarted", FieldDescription = "", Value = load.DateStarted.FormatNullableDateTime() });
                        //list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = "Date Completed", FieldName = "LoadDateCompleted", FieldDescription = "", Value = load.DateCompleted.FormatNullableDateTime() });
                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = "Notes", FieldName = "LoadNotes", FieldDescription = "", Value = load.Notes + "" });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = "Total", FieldName = "LoadTotal", FieldDescription = "", Value = load.Total.ToString() });
                        list.Add(new ReadOnlyField { Row = 3, Column = 2, Name = "# Incompletes", FieldName = "LoadIncomplete", FieldDescription = "", Value = load.Incomplete.ToString() });
                        list.Add(new ReadOnlyField { Row = 4, Column = 1, Name = "# Successes", FieldName = "LoadSuccess", FieldDescription = "", Value = load.Success.ToString() });
                        list.Add(new ReadOnlyField { Row = 4, Column = 2, Name = "# Errors", FieldName = "LoadError", FieldDescription = "", Value = load.Error.ToString() });
                    }
                    load = null;
                    break;
                #endregion
                case SystemObjects.LookupType:
                    #region Fields
                    var lookupType = Company.GetById<LookupType>(id);
                    if (lookupType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = lookupType.GetName(i => i.Name), FieldName = "LookupTypeName", FieldDescription = lookupType.GetDescription(i => i.Name), Value = lookupType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = lookupType.GetName(i => i.ID), FieldName = "LookupTypeID", FieldDescription = lookupType.GetDescription(i => i.ID), Value = lookupType.ID.ToString() });
                    }
                    lookupType = null;
                    break;
                    #endregion
                case SystemObjects.Policy:
                    #region Fields
                    var policy = Company.GetById<Policy>(id, i => i.Children);
                    if (policy != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = policy.GetName(i => i.Name), FieldName = "PolicyName", FieldDescription = policy.GetDescription(i => i.Description), Value = policy.Name });

                        var policyLevelInfo = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == policy.PolicyTypeID && i.Level == policy.Level).SingleOrDefault();

                        if (policyLevelInfo != null)
                        {
                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = "Level Name", Value = policyLevelInfo.Name });
                            list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = "Level Number", Value = policy.Level.ToString() });
                        }

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = policy.GetName(i => i.TextPath), FieldName = "PolicyTextPath", FieldDescription = policy.GetDescription(i => i.TextPath), Value = policy.TextPath });

                        list.Add(new ReadOnlyField { Row = 4, Column = 1, Name = policy.GetName(i => i.Description), FieldName = "PolicyDescription", FieldDescription = policy.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(policy.Description) ? "None provided" : policy.Description });
                    }
                    policy = null;
                    break;
                    #endregion
                case SystemObjects.Rule:
                    #region Fields
                    var rule = Company.GetById<Rule>(id);
                    if (rule != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = rule.GetName(i => i.Name), FieldName = "RuleName", FieldDescription = rule.GetDescription(i => i.Description), Value = rule.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = rule.GetName(i => i.RuleType), FieldName = "RuleRuleType", FieldDescription = rule.GetDescription(i => i.RuleType), Value = rule.RuleType.GetRuleTypeDisplayName() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = rule.GetName(i => i.Description), FieldName = "RuleDescription", FieldDescription = rule.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(rule.Description) ? "None provided" : rule.Description });
                    }
                    policy = null;
                    break;
                    #endregion
                case SystemObjects.ResponsibilityType:
                    #region Fields
                    var responsibilityType = Company.GetById<ResponsibilityType>(id);
                    if (responsibilityType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = responsibilityType.GetName(i => i.Name), FieldName = "Name", FieldDescription = responsibilityType.GetDescription(i => i.Name), Value = responsibilityType.Name });
                        //list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = responsibilityType.GetName(i => i.ResponsibilityTypeGroup), FieldName = "ResponsibilityTypeGroup", FieldDescription = responsibilityType.GetDescription(i => i.ResponsibilityTypeGroup), Value = responsibilityType.ResponsibilityTypeGroup.ToString() });
                        int nextRow = 2;
                        if (!string.IsNullOrEmpty(responsibilityType.Description))
                        {
                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = responsibilityType.GetName(i => i.Description), FieldName = "Description", FieldDescription = responsibilityType.GetDescription(i => i.Description), Value = responsibilityType.Description });
                            nextRow = 3;
                        }

                        #region Allocation

                        var allocations = string.Empty;

                        var comparer = new AllocationPossibilityComparer();
                        var allocationPossibilities = 
                            Company.GetAllocationOptions()
                            .Intersect(Company.ResponsibilityTypeRelations
                            .Where(i => i.ResponsibilityTypeID == responsibilityType.ID)
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
                        list.Add(new ReadOnlyField { Row = nextRow, Column = 1, Name = "Allocations", FieldName = "Allocations", FieldDescription = "", Value = allocations });

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
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = evt.GetName(i => i.Status), FieldName = "EventStatus", FieldDescription = evt.GetDescription(i => i.Status), Value = evt.Status });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = evt.GetName(i => i.SourceID), FieldName = "EventSourceID", FieldDescription = evt.GetDescription(i => i.SourceID), Value = evt.SourceID });
                        row = loadDynamicDisplayFields(list, type, id, 2);
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
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = core.resources.Fields.PublicID_Name, FieldName = "EventGroupPublicID", FieldDescription = core.resources.Fields.PublicID_Description, Value = evtgrp.PublicID });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = core.resources.Fields.ID_Name, FieldName = "EventGroupID", FieldDescription = core.resources.Fields.ID_Description, Value = evtgrp.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = core.resources.Fields.Name_Name, FieldName = "EventGroupName", FieldDescription = core.resources.Fields.Name_Description, Value = evtgrp.Name });
                        list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = "# Event Details", FieldName = "EventGroupEventCount", Value = evtgrp.EventCount.ToString() });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = core.resources.Fields.Rule_Name, FieldName = "EventGroupRuleName", FieldDescription = core.resources.Fields.Rule_Description, Value = evtgrp.RuleName });
                        list.Add(new ReadOnlyField { Row = 3, Column = 2, Name = "Rule ID", FieldName = "EventGroupRuleID", Value = evtgrp.RuleID.ToString() });
                    }
                    evtgrp = null;
                    break;
                #endregion
                case SystemObjects.PolicyType:
                    #region Fields
                    var policyType = Company.GetById<PolicyType>(id);
                    if (policyType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = policyType.GetName(i => i.Name), FieldName = "PolicyTypeName", FieldDescription = policyType.GetDescription(i => i.Name), Value = policyType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = policyType.GetName(i => i.ID), FieldName = "PolicyTypeID", FieldDescription = policyType.GetDescription(i => i.ID), Value = policyType.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = policyType.GetName(i => i.Description), FieldName = "PolicyTypeDescription", FieldDescription = policyType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(policyType.Description) ? "None provided" : policyType.Description });
                    }
                    policyType = null;
                    break;
                #endregion
                case SystemObjects.Report:
                    #region Fields
                    var report = Company.GetById<Report>(id, i => i.ReportLayout);
                    if (report != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = report.GetName(i => i.Name), FieldName = "ReportName", FieldDescription = report.GetDescription(i => i.Description), Value = report.Name });

                        if (!string.IsNullOrEmpty(report.Description))
                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = report.GetName(i => i.Description), FieldName = "ReportDescription", FieldDescription = report.GetDescription(i => i.Description), Value = report.Description });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = report.GetName(i => i.ReportLayout), FieldName = "ReportReportLayout", FieldDescription = report.GetDescription(i => i.ReportLayout), Value = report.ReportLayout.Name });

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

                        var objectName = "";
                        if (!string.IsNullOrEmpty(sql))
                        {
                            objectName = Company.Query<string>(sql, new { id = report.ObjectID }).SingleOrDefault();
                        }
                        else
                        {
                            objectName = "Not found.";
                        }
                        list.Add(new ReadOnlyField { Row = 3, Column = 2, Name = report.GetName(i => i.ObjectType), FieldName = "ReportObjectType", FieldDescription = report.GetDescription(i => i.ObjectType), Value = objectName });
                    }
                    report = null;
                    break;
                    #endregion
                case SystemObjects.Resolution:
                    #region Fields
                    var resolution = Company.GetById<Resolution>(id);
                    if (resolution != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = resolution.GetName(i => i.Name), FieldName = "ResolutionName", FieldDescription = resolution.GetDescription(i => i.Name), Value = resolution.Name });
                        if (!string.IsNullOrEmpty(resolution.Body))
                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = resolution.GetName(i => i.Body), FieldName = "ResolutionBody", FieldDescription = resolution.GetDescription(i => i.Body), Value = resolution.Body });
                    }
                    resolution = null;
                    break;
                    #endregion
                case SystemObjects.Resource:
                    #region Fields
                    var resource = Community.GetById<Resource>(id);
                    if (resource != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = "Name", Value = resource.FormatDisplayName() });
                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = resource.GetName(i => i.Email), FieldName = "ResourceEmail", FieldDescription = resource.GetDescription(i => i.Email), Value = resource.Email });
                        //list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = "Administrator?", FieldName = "ResourceAdministrator", FieldDescription = "Resource is a system administrator and can perform any task in the system.", Value = resource. });
                        loadDynamicDisplayFields(list, type, id, 3);
                    }
                    resource = null;
                    break;
                    #endregion
                case SystemObjects.ResourceType:
                    #region Fields
                    var resourceType = Community.GetById<ResourceType>(id);
                    if (resourceType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = resourceType.GetName(i => i.Name), FieldName = "ResourceTypeName", FieldDescription = resourceType.GetDescription(i => i.Name), Value = resourceType.Name });
                    }
                    resourceType = null;
                    break;
                    #endregion
                case SystemObjects.ResponseType:
                    #region Fields
                    var responseType = Company.GetById<ResponseType>(id);
                    if (responseType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = responseType.GetName(i => i.Name), FieldName = "ResponseTypeName", FieldDescription = responseType.GetDescription(i => i.Name), Value = responseType.Name });
                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = responseType.GetName(i => i.AllowOptions), FieldName = "ResponseTypeAllowOptions", FieldDescription = responseType.GetDescription(i => i.AllowOptions), Value = responseType.AllowOptions.FormatBooleanReadOnlyValue() });
                        list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = responseType.GetName(i => i.AllowValueOverride), FieldName = "ResponseTypeAllowValueOverride", FieldDescription = responseType.GetDescription(i => i.AllowValueOverride), Value = responseType.AllowValueOverride.FormatBooleanReadOnlyValue() });
                    }
                    responseType = null;
                    break;
                    #endregion
                case SystemObjects.StatisticType:
                    #region Fields
                    var statisticType = Company.GetById<StatisticType>(id);
                    if (statisticType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = statisticType.GetName(i => i.Name), FieldName = "StatisticTypeName", FieldDescription = statisticType.GetDescription(i => i.Name), Value = statisticType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = statisticType.GetName(i => i.PartOfScore), FieldName = "StatisticTypePartOfScore", FieldDescription = statisticType.GetDescription(i => i.PartOfScore), Value = statisticType.PartOfScore.FormatBooleanReadOnlyValue() });
                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = statisticType.GetName(i => i.Description), FieldName = "StatisticTypeDescription", FieldDescription = statisticType.GetDescription(i => i.Description), Value = statisticType.Description });
                        var fields = XElement.Parse(statisticType.Configuration);
                        var oType = SystemObjects.StatisticType;
                        int oID = 0;

                        switch (statisticType.CheckType)
                        {
                            case StatisticCheckType.Count:
                                oID = int.Parse(fields.Element("ObjectID").Value);
                                oType = (SystemObjects)Enum.Parse(typeof(SystemObjects), fields.Element("ObjectType").Value);
                                list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = statisticType.GetName(i => i.CheckType), Value = "Count" });
                                break;
                            case StatisticCheckType.Existence:
                                oID = int.Parse(fields.Element("ObjectID").Value);
                                oType = (SystemObjects)Enum.Parse(typeof(SystemObjects), fields.Element("ObjectType").Value);
                                list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = statisticType.GetName(i => i.CheckType), Value = "Existence" });
                                break;
                            case StatisticCheckType.PropertyValueCheck:
                                list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = statisticType.GetName(i => i.CheckType), Value = "Property" });
                                list.Add(new ReadOnlyField { Row = 3, Column = 2, Name = "Property Name", Value = fields.Element("PropertyName").Value });
                                list.Add(new ReadOnlyField { Row = 3, Column = 3, Name = "Value", Value = fields.Element("Value").Value });
                                break;
                            case StatisticCheckType.PropertyPopulated:
                                list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = statisticType.GetName(i => i.CheckType), Value = "Property" });
                                list.Add(new ReadOnlyField { Row = 3, Column = 2, Name = "Property Name", Value = fields.Element("PropertyName").Value });
                                break;
                            case StatisticCheckType.Relationship:
                                oID = int.Parse(fields.Element("ObjectID").Value);
                                oType = (SystemObjects)Enum.Parse(typeof(SystemObjects), fields.Element("ObjectType").Value);
                                list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = statisticType.GetName(i => i.CheckType), Value = "Relationship" });
                                break;
                            case StatisticCheckType.FusionOwnership:
                                list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = statisticType.GetName(i => i.CheckType), Value = "Value" });
                                break;
                        }

                        if (oID > 0)
                        {
                            var dtlStatisticType = Company.GetObjectDetail(oType, oID);
                            if (dtlStatisticType != null)
                            {
                                list.Add(new ReadOnlyField { Row = 4, Column = 1, Name = "Type To Check", Value = dtlStatisticType.TypeName });
                                list.Add(new ReadOnlyField { Row = 4, Column = 2, Name = "Item To Check", Value = dtlStatisticType.Name });
                            }
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
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = surveyType.GetName(i => i.Name), FieldName = "SurveyTypeName", FieldDescription = surveyType.GetDescription(i => i.Name), Value = surveyType.Name });
                        var dtlSurveyType = Company.GetObjectDetail(surveyType.ObjectType, surveyType.ObjectID);
                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = surveyType.GetName(i => i.ObjectType), FieldName = "SurveyTypeObjectType", FieldDescription = surveyType.GetDescription(i => i.ObjectType), Value = surveyType.ObjectType.ToString() });
                        list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = surveyType.GetName(i => i.ObjectID), FieldName = "SurveyTypeObjectID", FieldDescription = surveyType.GetDescription(i => i.ObjectID), Value = (dtlSurveyType != null) ? dtlSurveyType.Name : surveyType.ObjectID.ToString() });
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

                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = taxonomy.GetName(i => i.Name), FieldName = "TaxonomyName", FieldDescription = taxonomy.GetDescription(i => i.Name), Value = taxonomy.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = taxonomy.GetName(i => i.TaxonomyType.TaxonomyTypeClassID), FieldName = "TaxonomyTypeClass", FieldDescription = taxonomy.GetDescription(i => i.TaxonomyType.TaxonomyTypeClassID), Value = taxonomy.TaxonomyType.TaxonomyTypeClass.Name });
                        //list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = taxonomy.GetName(i => i.TaxonomyType), FieldName = "TaxonomyTaxonomyType", FieldDescription = taxonomy.GetDescription(i => i.TaxonomyType), Value = taxonomy.TaxonomyType.Name });

                        if (levelInfo != null)
                        {
                            list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = "Level Name", Value = levelInfo.Name });
                            list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = "Level Number", Value = taxonomy.Level.ToString() });
                        }

                        if (!string.IsNullOrEmpty(taxonomy.Description))
                            list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = taxonomy.GetName(i => i.Description), FieldName = "TaxonomyDescription", FieldDescription = taxonomy.GetDescription(i => i.Description), Value = taxonomy.Description });

                        list.Add(new ReadOnlyField { Row = 4, Column = 1, Name = taxonomy.GetName(i => i.TextPath), FieldName = "TaxonomyTextPath", FieldDescription = taxonomy.GetDescription(i => i.TextPath), Value = taxonomy.TextPath });

                        row = loadDynamicDisplayFields(list, type, id, 5);
                    }
                    taxonomy = null;
                    break;
                    #endregion
                case SystemObjects.TaxonomyType:
                    #region Fields
                    var taxonomyType = Company.GetById<TaxonomyType>(id, i => i.TaxonomyTypeClass);
                    if (taxonomyType != null)
                    {
                        list.Add(new ReadOnlyField { Row = 1, Column = 1, Name = taxonomyType.GetName(i => i.Name), FieldName = "TaxonomyTypeName", FieldDescription = taxonomyType.GetDescription(i => i.Name), Value = taxonomyType.Name });
                        list.Add(new ReadOnlyField { Row = 1, Column = 2, Name = taxonomyType.GetName(i => i.ID), FieldName = "TaxonomyTypeID", FieldDescription = taxonomyType.GetDescription(i => i.ID), Value = taxonomyType.ID.ToString() });

                        list.Add(new ReadOnlyField { Row = 2, Column = 1, Name = taxonomyType.GetName(i => i.TaxonomyTypeClassID), FieldName = "TaxonomyTypeClass", FieldDescription = taxonomyType.GetDescription(i => i.TaxonomyTypeClassID), Value = taxonomyType.TaxonomyTypeClass.Name });
                        list.Add(new ReadOnlyField { Row = 2, Column = 2, Name = taxonomyType.GetName(i => i.MaximumDepth), FieldName = "TaxonomyTypeMaximumDepth", FieldDescription = taxonomyType.GetDescription(i => i.MaximumDepth), Value = taxonomyType.MaximumDepth.ToString() });

                        list.Add(new ReadOnlyField { Row = 3, Column = 1, Name = taxonomyType.GetName(i => i.Description), FieldName = "TaxonomyTypeDescription", FieldDescription = taxonomyType.GetDescription(i => i.Description), Value = string.IsNullOrEmpty(taxonomyType.Description) ? "None provided" : taxonomyType.Description });
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
                        list.Add(new ReadOnlyField { Row = rowNumber, Column = 1, Name = "Type", FieldName = "WtrType", FieldDescription = "", Value = wtr.ObjectName });
                        list.Add(new ReadOnlyField { Row = rowNumber, Column = 2, Name = Resources.FieldInfo.TaxonomyType_Name, ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID", FieldName = "WtrOwner", FieldDescription = "", Value = wtr.ParentName ?? "None" });
                        rowNumber++;
                        list.Add(new ReadOnlyField { Row = rowNumber, Column = 1, Name = "Responsibility", FieldName = "WtrResponsibility", FieldDescription = "", Value = wtr.ResponsibilityType });
                        rowNumber++;
                        foreach (var p in wtr.Properties)
                        {
                            list.Add(new ReadOnlyField { Row = rowNumber, Column = 1, Name = p.Key, FieldName = string.Format("Wtr{0}", p.Key), FieldDescription = "", Value = p.Value });
                            rowNumber++;
                        }
                    }
                    wtr = null;
                    break;
                    #endregion
            }

            sections.Add(new ReadOnlySection { Name = "Governance", Fields = list, ID = 0 });

            return Request.CreateResponse(HttpStatusCode.OK, sections);//new { Fields = list });
        }

        [Route("{type}/{id:int}/object/statistics")]
        public ObjectStatisticTileModel GetTileObjectStatistics(SystemObjects type, int id)
        {
            return Company.GetObjectStatistics(type, id);
        }

        [Route("fusion/statistics")]
        public FusionStatisticTileModel GetFusionStatistics()
        {
            FusionStatisticTileModel res =  Company.Query<FusionStatisticTileModel>(
                            @"  select
	                                (select count(1) from fusion.agenterror where [date] > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as AgentErrors,
	                                (select count(1) from fusion.execution where datestarted > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as AgentExecutions,
	                                (select count(1) from fusionstatuslog where success = 1 and datestarted > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as FusionExecutions,
	                                (select count(1) from fusion.error where [date] > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as FusionErrors,
	                                (select count(1) from fusionattributepromotionlogsummary where datestarted > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as NumberOfPromotions"
                ).FirstOrDefault();

            return res;
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
                    var imItems = Company.Taxonomies.AsQueryable();
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

        [Route("{type}/{id:int}/redflags")]
        public IQueryable<dynamic> GetRedFlagsByTypeAndResource(SystemObjects type, int id)
        {
            return Company.GetRedFlagsByTypeAndCurrentResource(type, id).AsQueryable();
        }

        [Route("{type}/{id:int}/social/statistics")]
        public SocialStatisticsByObject GetSocialTileStatistics(SystemObjects type, int id)
        {
            return Company.GetSocialStatisticsByObject(type, id);
        }

        [Route("{type}/{id:int}/statistics")]
        public IQueryable<StatisticDetail> GetStatisticDetails(SystemObjects type, int id)
        {
            return Company.GetStatisticDetailsByType(type, id).AsQueryable();
        }

        [Route("{type}/{id:int}/synonyms")]
        public HttpResponseMessage GetSynonymsByObject(SystemObjects type, int id)
        {
            //var sType = type.ToString();

            var models = Company.Query<dynamic>(
@"with A as	(
			select	D.Name as [Source],
					A.ID
			from	Attribute A
					inner join cache.ObjectDetails D on D.[Object] = A.ObjectType and D.ObjectID = A.ObjectID
			where	A.AttributeTypeID = 1
					and A.ObjectType = @type
					and A.ObjectID = @id
			union
			select	R.TargetObjectName as [Source],
					A.ID
			from	Attribute A
					inner join [cache].[Relationships] R on A.ObjectType = 'Intersect' and R.IntersectID = A.ObjectID and R.SourceObject = @type and R.SourceObjectID = @id
                    inner join IntersectTypeNode N on N.ID = R.[SourceIntersectTypeNodeID] --and N.[Order] = 2
			where	A.AttributeTypeID = 1
			)

select	A.[Source],
		A.ID,
		FN.Name,
		FD.Description
from	A
		cross apply (
					select	F.Value as Name
					from	Field F
							inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = 'Attribute' and F.ObjectID = A.ID and FT.Name = 'Name'
					) FN
		outer apply (
					select	F.Value as Description 
					from	Field F
							inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = 'Attribute' and F.ObjectID = A.ID and FT.Name = 'Description'					
					) FD", new { type = type.ToString(), id });
            return Request.CreateResponse(
                HttpStatusCode.OK,
                models
            );

            //Company.Filter<Synonym>(i => i.ObjectType == sType && i.ObjectID == id, i => i.SynonymType).Select(i => new { i.ID, i.Name, i.Description, SynonymType = i.SynonymType.Name })
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
        public IQueryable<StatisticType> GetStatisticTypes()
        {
            return Company.Table<StatisticType>();
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
            var model = Company.GetById<TaxonomyType>(typeID);
            if (model == null) return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Information model not found.");
            return Request.CreateResponse<TaxonomyType>(model);
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

        [Route("StatisticType/{id}/allocations")]
        public IQueryable<StatisticTypeRelationDetail> GetAllocationsByStatisticType(int id)
        { 
            return Company.Filter<StatisticTypeRelationDetail>(i => i.StatisticTypeID == id);
        }

        #endregion
    }
}
