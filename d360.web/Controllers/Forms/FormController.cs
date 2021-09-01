using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Newtonsoft.Json.Linq;
using Resources;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Text;
using d360.core.resources;
using Newtonsoft.Json;
using d360.model.DataAccessLayer;
using d360.core.helpers;

namespace d360.web.Controllers
{
    [RoutePrefix("form"), Authorize, AiHandleError, NonNullableParameters]
    public partial class FormController : BaseController
    {
        #region DI

        readonly IStorageProvider Storage;
        readonly IResponsibilityRepository ResponsibilityRepository;

        public FormController(ICommunityContext community, ICompanyContext company, ISecurityContextProvider secProvider, IStorageProvider storage, IResponsibilityRepository responsibilityRepository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            Storage = storage;
            ResponsibilityRepository = responsibilityRepository;
#if DEBUG
            company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
#endif

        }

        #endregion

        #region Field Loading For Type Forms Below

        void loadIconFields(List<EditableField> list, int row, AssetTypeStyle style = null)
        {
            var b = "#000000";
            var f = "#ffffff";

            if (style != null)
            {
                b = style.IconBackColor;
                f = style.IconForeColor;
            }

            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "IconBackColor", Name = "Background Color", FieldDescription = "The icon's background color", FieldType = DataType.Color.ToString(), Value = b });
            list.Add(new EditableField { Row = row, Column = 2, Required = true, FieldName = "IconForeColor", Name = "Text Color", FieldDescription = "The icon's text color", FieldType = DataType.Color.ToString(), Value = f });
        }

        void upsertAssetStyle(string type, int id, string foreColor, string backColor, string objectName = "Tx")
        {
            var assetType = Company.Filter<AssetType>(i => i.Object == type && i.ObjectID == id).FirstOrDefault();
            var style = Company.GetAssetTypeStyle(assetType.ID);
            bool add = (style == null);

            if (add)
            {
                style = new AssetTypeStyle
                {
                    ID = assetType.ID,
                    IconBackColor = backColor,
                    IconForeColor = foreColor,
                    IconText = IconHelper.GetIconText(objectName)
                };
                Company.Add(style);
            }
            else
            {
                style.IconBackColor = backColor;
                style.IconForeColor = foreColor;
                style.IconText = IconHelper.GetIconText(objectName);
                Company.Update(style);
            }
        }

        void upsertAssetStyle(SystemObjects type, int id, string foreColor, string backColor, string objectName = "Tx")
        {
            upsertAssetStyle(type.ToString(), id, foreColor, backColor, objectName);
        }

        void upsertAssetStyle(SystemObjects type, int id, FormCollection form, string objectName = "Tx")
        {
            var assetType = Company.Filter<AssetType>(i => i.Object == type.ToString() && i.ObjectID == id).FirstOrDefault();
            var style = Company.GetAssetTypeStyle(assetType.ID);
            bool add = (style == null);

            string iconText;

            var words = objectName.Split(' ');
            if (words.Length > 1)
            {
                iconText = words[0][0].ToString().ToUpper() + words[1][0].ToString().ToLower();
            }
            else
            {
                iconText = objectName[0].ToString().ToUpper() + objectName[1].ToString().ToLower();
            }

            if (add)
            {
                style = new AssetTypeStyle
                {
                    ID = assetType.ID,
                    IconBackColor = form["IconBackColor"],
                    IconForeColor = form["IconForeColor"],
                    IconText = iconText
                };
                Company.Add(style);
            }
            else
            {
                style.IconBackColor = form["IconBackColor"];
                style.IconForeColor = form["IconForeColor"];
                style.IconText = iconText;
                Company.Update(style);
            }
        }

        #endregion

        #region Parse Methods

        bool parseBooleanField(FormCollection form, string fieldName)
        {
            if (form.AllKeys.Any(i => i == fieldName))
            {
                bool value = false;

                var booleanRawValue = form[fieldName];

                switch (booleanRawValue)
                {
                    case "value":
                    case "on":
                    case "1":
                    case "true":
                    case "True":
                        value = true;
                        break;
                    default:
                        if (booleanRawValue.Contains(','))
                        {
                            booleanRawValue = form[fieldName].Split(',').ToList()[0];
                            value = bool.Parse(booleanRawValue);
                        }
                        break;
                }

                return value;
            }
            else
            {
                return false;
            }
        }

        T parseEnumField<T>(FormCollection form, string fieldName)
        {
            return form.AllKeys.Any(i => i == fieldName) ? (T)Enum.Parse(typeof(T), form[fieldName]) : default(T);
        }

        int? parseNullableIntField(FormCollection form, string fieldName, int? defaultValue = null)
        {
            if (form.AllKeys.Any(i => i == fieldName))
            {
                int value;
                if (int.TryParse(form[fieldName], out value))
                {
                    return value;
                }
                else
                {
                    return defaultValue;
                }
            }
            else
            {
                return defaultValue;
            }
        }

        int parseIntField(FormCollection form, string fieldName)
        {
            return form.AllKeys.Any(i => i == fieldName) ? int.Parse(form[fieldName]) : 0;
        }

        string parseTextField(FormCollection form, string fieldName, string defaultValue = null, bool htmlEncode = false)
        {
            return form.AllKeys.Any(i => i == fieldName) ? ((htmlEncode) ? Server.HtmlEncode(form[fieldName]) : form[fieldName]) : defaultValue;
        }

        #endregion

        #region Dynamic Editor Field Type Information For Angular2

        [
            HttpGet, 
            Route("dynamiceditor/assets/{assetTypeUid}"),
            Route("dynamiceditor/assets/{assetTypeUid}/{assetUid}")
        ]
        public JsonResult GetUidAssetEditor(Guid assetTypeUid, Guid? assetUid = null, Guid? parentUid = null)
        {
            Asset parentAsset = null;
            int? parentId = null;
            if (parentUid.HasValue)
            {
                parentAsset = Company.Filter<Asset>(a => a.uid == parentUid.Value).SingleOrDefault();
                if (parentAsset == null)
                {
                    return jsonException(string.Format(ActionApiMessages.AssetNotFound, parentUid.Value), HttpStatusCode.NotFound);
                }
                parentId = parentAsset.ObjectID;
            }

            if (assetUid.HasValue)
            {
                var asset = Company.Filter<Asset>(a => a.uid == assetUid.Value).SingleOrDefault();
                if (asset == null)
                {
                    return jsonException(string.Format(ActionApiMessages.AssetNotFound, assetUid.Value), HttpStatusCode.NotFound);
                }
                return DynamicEditorEditFields(asset.Object, assetUid.Value);
            }
            else
            {
                var assetType = Company.AssetTypes.SingleOrDefault(x => x.uid == assetTypeUid);
                if (assetType == null)
                {
                    return jsonException(string.Format(ActionApiMessages.AssetTypeNotFound, assetUid.Value), HttpStatusCode.NotFound);
                }
                return DynamicEditorAddFields(assetType.Object, null, parentId, assetType.ObjectID);
            }
            
        }

        [HttpGet, Route("dynamiceditor/byUid/{assetTypeUid}/{assetUid}")]
        public JsonResult DynamicEditorNewV2(Guid assetTypeUid, Guid assetUid)
        {
            var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == assetTypeUid);
            var o = assetType.Object;
            return this.DynamicEditorEditFields(o, assetUid);

            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditor/edit/{o}/{uid}")]
        public JsonResult DynamicEditorEditFields(string o, Guid? uid)
        {
            int objectId = -1;

            switch ((o ?? "").ToUpper())
            {
                case "TAG":
                    objectId = Company.Tags.FirstOrDefault(x => x.uid == uid).ID;
                    return DynamicEditorEditFields(o, objectId);
                case "INTERSECTTYPE":
                    objectId = Company.Intersects.FirstOrDefault(x => x.uid == uid).ID;
                    return DynamicEditorEditFields(o, objectId);
                case "PREDICATE":
                    objectId = Company.Predicates.FirstOrDefault(x => x.UID == uid).ID;
                    return DynamicEditorEditFields(o, objectId);
                case "EXPORTTEMPLATE":
                    objectId = Company.AssetTypeExportTemplates.FirstOrDefault(x => x.Uid == uid).ID;
                    return DynamicEditorEditFields(o, objectId);
                case "ISSUETYPE":
                    objectId = Company.IssueTypes.FirstOrDefault(x => x.uid == uid).ID;
                    return DynamicEditorEditFields(o, objectId);
                default:
                    foreach (SystemObjects sysobj in (SystemObjects[])Enum.GetValues(typeof(SystemObjects)))
                    {
                        if (sysobj.ToString().ToUpper() == o.ToUpper())
                        {
                            objectId = Company.GetObjectId(uid.Value, sysobj);
                        }
                    }
                    return DynamicEditorEditFields(o, objectId);
            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditor/edit/{o}/{oid:int}")]
        public JsonResult DynamicEditorEditFields(string o, int oid)
        {
            JsonResult res;
            switch ((o ?? "").ToUpper())
            {
                case "APIFIELD":
                    res = CustomAPIVersionField_EditFields(oid);
                    break;
                case "ARTIFACT":
                    res = Artifact_EditFields(oid);
                    break;
                case "CONTRACT":
                    res = Contract_EditFields(oid);
                    break;
                case "ENDPOINT":
                    res = CustomAPIServiceEndpoint_EditFields(oid);
                    break;
                case "EXPORTTEMPLATE":
                    res = ExportTemplate_EditFields(oid);
                    break;
                case "INTERSECTTYPE":
                    res = Relationship_EditFields(oid);
                    break;
                case "ISSUETYPE":
                    res = IssueType_EditFields(oid);
                    break;
                case "NAMESPACE":
                    res = CustomAPINamespace_EditFields(oid);
                    break;
                case "ORGANIZATION":
                    res = Organization_EditFields(oid);
                    break;
                case "ORGANIZATIONDOMAIN":
                    res = OrganizationDomain_EditFields(oid);
                    break;
                case "ORGANIZATIONINVITATION":
                    res = OrganizationInvitation_EditFields(oid);
                    break;
                case "POLICY":
                    res = Hierarchy_EditFields(SystemObjects.Policy, oid);
                    break;
                case "PREDICATE":
                    res = Predicate_EditFields(oid);
                    break;
                case "REFERENCEITEM":
                    res = ReferenceItem_EditFields(oid);
                    break;
                case "RESOURCESELF":
                    res = Resource_EditMyInfoFields();
                    break;
                case "RESOURCETYPE":
                    res = Resource_EditFields(oid);
                    break;
                case "RULE":
                    res = Rule_EditFields(oid);
                    break;
                case "RULETYPE":
                    res = RuleType_EditFields(oid);
                    break;
                case "SERVICE":
                    res = CustomAPIService_EditFields(oid);
                    break;
                case "SURVEYTYPE":
                    res = SurveyType_EditFields(oid);
                    break;
                case "TAG":
                    res = Tag_EditFields(oid);
                    break;
                case "TASKTYPE":
                    res = Diagram_EditFields(oid);
                    break;
                case "TAXONOMY":
                case "TAXONOMYTYPE":
                    res = Hierarchy_EditFields(SystemObjects.Taxonomy, oid);
                    break;
                case "VERSION":
                    res = CustomAPIServiceEndpointVersion_EditFields(oid);
                    break;
                case "URI":
                    res = CustomAPIVersionUri_EditFields(oid);
                    break;
                default:
                    throw new Exception("Invalid or non implemented editor type");
            }


            res.MaxJsonLength = int.MaxValue;
            return res;
        }

        [
            HttpGet,
            Route("dynamiceditor/new/uid/{uid}/type/{objectType?}"),
            Route("dynamiceditor/new/uid/{uid}/type/{objectType?}/target/{targetTypeUid?}")
        ]
        public JsonResult DynamicEditorAddFieldsByUid(string uid, string objectType, string targetTypeUid)
        {
            Guid guid = Guid.Empty;

            if (Guid.TryParse(uid, out guid))
            {

                if (objectType == SystemObjects.Issue.ToString())
                {
                    var issueType = Company.IssueTypes.FirstOrDefault(x => x.uid == guid);
                    if (issueType != null)
                    {
                        return DynamicEditorAddFields(SystemObjects.Issue.ToString(), issueType.ID, null, null);
                    }
                    else
                    {
                        throw new ArgumentException("No Issue Type found for given Guid");
                    }
                }
                else if (objectType == SystemObjects.IssueTypeRelation.ToString())
                {
                    var issueType = Company.IssueTypes.FirstOrDefault(x => x.uid == guid);
                    if (issueType != null)
                    {
                        return DynamicEditorAddFields(SystemObjects.IssueTypeRelation.ToString(), issueType.ID, null, null);
                    }
                    else
                    {
                        throw new ArgumentException("No Issue Type found for given Guid");
                    }
                }
                else if (objectType == SystemObjects.IntersectType.ToString())
                {
                    var intersectType = Company.IntersectTypes.FirstOrDefault(x => x.uid == guid);
                    Guid targetGuid = Guid.Empty;
                    Guid.TryParse(targetTypeUid, out targetGuid);
                    var targetAsset = Company.Assets.FirstOrDefault(x => x.uid == targetGuid);
                    AssetType assetType = null;
                    if (targetAsset == null)
                    {
                        assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == targetGuid);
                    }

                    if (intersectType != null && (targetAsset != null || assetType != null))
                    {
                        return Relationship_AddFields(intersectType, targetAsset, assetType);
                    }
                    else
                    {
                        throw new ArgumentException("Not valid Intersect Type Uid or Target Type Uid");
                    }

                }
                else
                {
                    var asset = Company.AssetTypes.FirstOrDefault(x => x.uid == guid);
                    if (asset != null)
                    {
                        return DynamicEditorAddFields(asset.Object.Replace("Type", ""), asset.ObjectID, null, null);
                    }
                    else
                    {
                        throw new ArgumentException("No Asset Type found for given Guid");
                    }
                }
            }
            throw new ArgumentException("Invalid Guid");

        }

        [HttpGet, Route("dynamiceditor/new/{objectType}/{objectID?}/{parentID?}/{typeID?}")]
        public JsonResult DynamicEditorAddFields(string objectType, int? objectID, int? parentID, int? typeID)
        {
            JsonResult res;
            switch ((objectType ?? "").ToUpper())
            {
                case "APIFIELD":
                    res = CustomAPIVersionField_AddFields(parentID.GetValueOrDefault());
                    break;
                case "ARTIFACT":
                    res = Artifact_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                    break;
                case "CONTRACT":
                    res = Contract_AddFields(objectID.HasValue ? objectID.Value : 0);
                    break;
                case "ENDPOINT":
                    res = CustomAPIServiceEndpoint_AddFields(parentID.GetValueOrDefault());
                    break;
                case "EXPORTTEMPLATE":
                    res = ExportTemplate_AddFields();
                    break;
                case "ISSUE":
                    res = Issue_AddFields(objectID.GetValueOrDefault());
                    break;
                case "ISSUETYPE":
                    res = IssueType_AddFields();
                    break;
                case "ISSUETYPERELATION":
                    res = IssueTypeRelation_AddFields(objectID.GetValueOrDefault());
                    break;
                case "NAMESPACE":
                    res = CustomAPINamespace_AddFields(parentID.GetValueOrDefault());
                    break;
                case "ORGANIZATION":
                    res = Organization_AddFields(objectID.GetValueOrDefault());
                    break;
                case "ORGANIZATIONDOMAIN":
                    res = OrganizationDomain_AddFields(objectID.Value);
                    break;
                case "ORGANIZATIONINVITATION":
                    res = OrganizationInvitation_AddFields(objectID.Value);
                    break;
                case "POLICY":
                    res = Hierarchy_AddFields(SystemObjects.PolicyType, objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                    break;
                case "POLICYTYPE":
                    res = Hierarchy_AddFields(SystemObjects.PolicyType, typeID.GetValueOrDefault(), parentID.GetValueOrDefault());
                    break;
                case "PREDICATE":
                    res = Predicate_AddFields();
                    break;
                case "REFERENCEITEM":
                    res = ReferenceItem_AddFields(objectID.GetValueOrDefault());
                    break;
                case "RESOURCETYPE":
                    res = Resource_AddFields(objectID.GetValueOrDefault());
                    break;
                case "RULE":
                    res = Rule_AddFields(objectID.GetValueOrDefault());
                    break;
                case "RULETYPE":
                    res = RuleType_AddFields();
                    break;
                case "SERVICE":
                    res = CustomAPIService_AddFields();
                    break;
                case "SURVEYTYPE":
                    res = SurveyType_AddFields();
                    break;
                case "TAG":
                    res = Tag_AddFields();
                    break;
                case "TASK":
                    res = Diagram_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                    break;
                case "TAXONOMY":
                    res = Hierarchy_AddFields(SystemObjects.TaxonomyType, objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                    break;
                case "TAXONOMYTYPE":
                    res = Hierarchy_AddFields(SystemObjects.TaxonomyType, typeID.GetValueOrDefault(), parentID.GetValueOrDefault());
                    break;
                case "VERSION":
                    res = CustomAPIServiceEndpointVersion_AddFields(parentID.GetValueOrDefault());
                    break;
                case "URI":
                    res = CustomAPIVersionUri_AddFields(parentID.GetValueOrDefault());
                    break;
                default:
                    throw new Exception("Invalid or non implemented editor type");

            }
            res.MaxJsonLength = int.MaxValue;
            return res;
        }

        [HttpPut, Route("dynamicedit/edit/{objectType}"), ValidateInput(false)]
        public async Task<JsonResult> DynamicEdit(string objectType, string json)
        {
            JObject jsonObject = JObject.Parse(json);
            FormCollection form = new FormCollection();

            foreach (var item in jsonObject)
            {
                form.Add(item.Key, item.Value.ToString());
            }

            switch ((objectType ?? "").ToUpper())
            {
                case "APIFIELD":
                    return EditApiField(form);
                case "ENDPOINT":
                    return EditServiceEndpoint(form);
                case "INTERSECT":
                    return EditRelationship(form);
                case "INTERSECTTYPE":
                    return EditIntersectType(form);
                case "NAMESPACE":
                    return EditNamespace(form);
                case "ORGANIZATION":
                    return PutOrganization(form);
                case "ORGANIZATIONDOMAIN":
                    return PutOrganizationDomain(form);
                case "ORGANIZATIONINVITATION":
                    return PutOrganizationInvitation(form);
                case "POLICYTYPELEVEL":
                    return EditPolicyTypeLevel(form);
                case "REPORT":
                    return await EditReport(form);
                case "RULETYPE":
                    return EditRuleType(form);
                case "SERVICE":
                    return EditService(form);
                case "SURVEYTYPE":
                    return EditSurveyType(form);
                case "TAXONOMYTYPELEVEL":
                    return EditTaxonomyTypeLevel(form);
                case "VERSION":
                    return EditServiceEndpointVersion(form);
                case "URI":
                    return EditServiceEndpointVersionUri(form);
                default:
                    throw new Exception("Invalid / unsupported edit type");
            }
        }

        [HttpDelete, Route("dynamicedit/delete/{objectType}/{objectID:int}"), ValidateInput(false)]
        public async Task<JsonResult> DynamicDelete(string objectType, int objectID)
        {
            FormCollection form = new FormCollection();
            form.Add("ID", objectID.ToString());

            switch ((objectType ?? "").ToUpper())
            {
                case "APIFIELD":
                    return DeleteApiField(form);
                case "CONTRACT":
                    return DeleteContract(objectID);
                case "CUSTOMSYNONYM":
                    return DeleteCustomSynonym(form);
                case "ENDPOINT":
                    return DeleteCustomAPIEndPoint(form);
                case "INTERSECTTYPE":
                    IntersectType intersectType = Company.GetById<IntersectType>(objectID);
                    form.Add("IntersectTypeUid", intersectType.uid.ToString());
                    return DeleteIntersectType(form);
                case "NAMESPACE":
                    return DeleteCustomAPINamespace(form);
                case "ORGANIZATION":
                    return DeleteOrganization(objectID);
                case "ORGANIZATIONDOMAIN":
                    return DeleteOrganizationDomain(objectID);
                case "ORGANIZATIONINVITATION":
                    return DeleteOrganizationInvitation(objectID);
                case "REPORT":
                    return await DeleteReport(form);
                case "RULETYPE":
                    return DeleteRuleType(form);
                case "POLICYTYPELEVEL":
                    return DeletePolicyTypeLevel(form);
                case "SERVICE":
                    return DeleteCustomAPIService(form);
                case "SURVEYTYPE":
                    return DeleteSurveyType(form);
                case "SURVEYQUESTIONTYPE":
                    return DeleteQuestionType(form);
                case "TAXONOMYTYPELEVEL":
                    return DeleteTaxonomyTypeLevel(form);
                case "URI":
                    return DeleteCustomAPIUri(form);
                case "VERSION":
                    return DeleteCustomAPIVersion(form);
                default:
                    throw new Exception("Invalid / unsupported delete type");
            }
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("dynamicedit/create/{objectType}"), ValidateInput(false)]
        public async Task<JsonResult> DynamicCreate(string objectType, string json)
        {
            JObject jsonObject = JObject.Parse(json);
            FormCollection form = new FormCollection();

            foreach (var item in jsonObject)
            {
                form.Add(item.Key, item.Value.ToString());
            }

            switch ((objectType ?? "").ToUpper())
            {
                case "APIFIELD":
                    return AddServiceEndpointVersionField(form);
                case "CUSTOMSYNONYM":
                    return AddCustomSynonym(form);
                case "ENDPOINT":
                    return AddServiceEndpoint(form);
                case "INTERSECT":
                    return AddRelationship(form);
                case "INTERSECTTYPE":
                    return AddIntersectType(form);
                case "NAMESPACE":
                    return AddNamespace(form);
                case "ORGANIZATION":
                    return PostOrganization(form);
                case "ORGANIZATIONDOMAIN":
                    return PostOrganizationDomain(form);
                case "ORGANIZATIONINVITATION":
                    return PostOrganizationInvitation(form);
                case "POLICYTYPELEVEL":
                    return AddPolicyTypeLevel(form);
                case "REPORT":
                    return await AddReport(form);
                case "RULETYPE":
                    return AddRuleType(form);
                case "SERVICE":
                    return AddService(form);
                case "SURVEYTYPE":
                    return AddSurveyType(form);
                case "TAXONOMYTYPELEVEL":
                    return AddTaxonomyTypeLevel(form);
                case "VERSION":
                    return AddServiceEndpointVersion(form);
                case "URI":
                    return AddServiceEndpointVersionUri(form);
                default:
                    throw new Exception("Invalid / unsupported create type");
            }
        }

        #endregion

        #region Company Settings

        [Route("CompanySettings/Groups")]
        public JsonNetResult GetGroups()
        {
            var list = Company.Query<dynamic>($@"
				select	cast(ID as varchar) as [value],
                Name as label 
                from	[Group]
                order by Name");
            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("CompanySettings")]
        public JsonNetResult CompanySettings()
        {
            var settings = SettingsRepository.GetSettings();
            var model = new CompanySettingsEditorModel
            {
                DisableCommunityPosting = settings.GetValue<bool>(Setting.DisableCommunityPosting),
                DisableIssueManagement = settings.GetValue<bool>(Setting.DisableIssueManagement),
                EnableShoppingCart = settings.GetValue<bool>(Setting.EnableShoppingCart),
                DefaultRoute = settings.GetValue(Setting.DefaultRoute),
                EnableSearchExactMatch = settings.GetValue<bool>(Setting.SearchExactMatch),
                HideData3SixtyUsers = settings.GetValue<bool>(Setting.HideData3SixtyUsers),
                ShowAllUsersAPIKey = settings.GetValue<bool>(Setting.ShowAllUsersAPIKey),
                WorkflowCatchAllGroup = settings.GetValue<int>(Setting.WorkflowCatchAllGroup),
                WorkflowDigestEmailDays = settings.GetValue<int>(Setting.WorkflowDigestEmailDays),
                MaxDropdownItems = settings.GetValue<int>(Setting.MaxDropdownItems),
                WriteActionDescription = settings.GetValue<bool>(Setting.WriteActionDescription),
                MaxExcelExportRows = settings.GetValue<int>(Setting.MaxExcelExportRows),
                CurrentCompanyIconPath = settings.GetValue(Setting.CompanyIcon),
                CurrentCompanyLogoPath = settings.GetValue(Setting.CompanyLogo),
                DefaultSearchTypes = settings.GetValue(Setting.DefaultSearchTypes),
                LineageVersion = settings.GetValue<int>(Setting.LineageVersion),
                HeaderBackgroundColor = settings.GetValue(Setting.HeaderBackgroundColor),
                ShowHomeAssignmentTile = settings.GetValue<bool>(Setting.ShowHomeAssignmentTile),
                ShowHomeBoardTile = settings.GetValue<bool>(Setting.ShowHomeBoardTile),
                ShowHomeActivityTile = settings.GetValue<bool>(Setting.ShowHomeActivityTile),
                ShowHomePageTitle = settings.GetValue<bool>(Setting.ShowHomePageTitle),
                HomePageTitleSize = settings.GetValue(Setting.HomePageTitleSize),
                HomePageTitleColor = settings.GetValue(Setting.HomePageTitleColor),
                HomePageBackgroundImage = settings.GetValue(Setting.HomePageBackgroundImage),
                BrowserTitlePrefix = settings.GetValue(Setting.BrowserTitlePrefix),
                AllowedOrigins = settings.GetValue(Setting.AllowedOrigins),
                FramingDomains = settings.GetValue(Setting.FramingDomains)
            };
            var ipRaw = settings.GetValue(Setting.IpRestriction);
            if (!string.IsNullOrEmpty(ipRaw))
            {
                var ipXml = XElement.Parse(ipRaw);
                var ips = ipXml.Elements("ip").Select(i => new CompanySettingsIpRestrictionEditorModel { Name = i.Element("name").Value, Start = i.Element("start").Value, End = i.Element("end").Value });
                model.IpRestrictions.AddRange(ips);
            }

            IQueryable<SiteNav> siteNavs = Company.SiteNav.Where(s => s.ParentID == null && s.Name != "#Home").OrderBy(s => s.SortOrder);
            model.SiteNav = siteNavs.ToList();

            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPut, ValidateInput(false), Route("UpdateCompanySettings")]
        public async Task<JsonResult> UpdateCompanySettings(CompanySettingsEditorModel formModel)
        {
            try
            {
                if (formModel == null) throw new NoFormDataException("company settings");

                // Permissions validation.
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var settings = SettingsRepository.GetSettings();
                SettingInfo currentSettingInfo = null;
                Setting currentSetting;

                Action<Setting, SettingInfo, bool> settingAction = (Setting s, SettingInfo i, bool delete) => {
                    if (delete)
                    {
                        SettingsRepository.DeleteSetting(s);
                        settings.Remove(i);
                        i = null;
                    }
                    else
                    {
                        SettingsRepository.UpsertSetting(s, i.Value);
                    }
                };

                Action<Setting, string> settingActionValue = (Setting s, string newValue) => {
                    currentSettingInfo = settings.Single(o => o.ID == s);
                    currentSettingInfo.Value = newValue;
                    settingAction(s, currentSettingInfo, !currentSettingInfo.IsOverridden);
                };

                #region Icon

                currentSetting = Setting.CompanyIcon;
                currentSettingInfo = settings.Single(s => s.ID == currentSetting);
                if (formModel.SetIconToDefault)
                {
                    settingAction(currentSetting, currentSettingInfo, true);
                }
                else
                {
                    if (!string.IsNullOrEmpty(formModel.CompanyIcon))
                    {
                        var iconMatch = MimeTypeExtensionsMap.RegEx.Match(formModel.CompanyIcon);

                        var iconMime = iconMatch.Groups["mime"].Value;
                        var iconData = iconMatch.Groups["data"].Value;
                        var iconExtension = MimeTypeExtensionsMap.GetExtension(iconMime);
                        var iconByteArray = Convert.FromBase64String(iconData);
                        using (var iconStream = new MemoryStream(iconByteArray))
                        {
                            var iconFileName = string.Format("{0}{1}", Company.CurrentCompanyID, iconExtension);
                            await Storage.CreateFile(constants.COMPANY_ICON_FOLDER, iconFileName, iconStream);
                            settingActionValue(currentSetting, $"{constants.COMPANY_ICON_URL}{iconFileName}");
                        }
                    }
                }

                #endregion

                #region Logo

                currentSetting = Setting.CompanyLogo;
                currentSettingInfo = settings.Single(s => s.ID == currentSetting);
                if (formModel.SetLogoToDefault)
                {
                    settingAction(currentSetting, currentSettingInfo, true);
                }
                else
                {
                    if (!string.IsNullOrEmpty(formModel.CompanyLogo))
                    {
                        var logoMatch = MimeTypeExtensionsMap.RegEx.Match(formModel.CompanyLogo);

                        var logoMime = logoMatch.Groups["mime"].Value;
                        var logoData = logoMatch.Groups["data"].Value;
                        var logoExtension = MimeTypeExtensionsMap.GetExtension(logoMime);
                        var logoByteArray = Convert.FromBase64String(logoData);
                        using (var logoStream = new MemoryStream(logoByteArray))
                        {
                            var filesToDelete = Storage.ListFilenamesByPrefix(constants.COMPANY_LOGO_FOLDER, $"{Company.CurrentCompanyID}.");
                            filesToDelete.ForEach(f =>
                            {
                                Storage.DeleteFile(constants.COMPANY_LOGO_FOLDER, f).Wait();
                            });

                            var logoFileName = string.Format("{0}{1}", Company.CurrentCompanyID, logoExtension);
                            await Storage.CreateFile(constants.COMPANY_LOGO_FOLDER, logoFileName, logoStream);
                            settingActionValue(currentSetting, $"{constants.COMPANY_LOGO_URL}{logoFileName}");
                        }
                    }
                }

                #endregion

                // Social
                settingActionValue(Setting.DisableCommunityPosting, formModel.DisableCommunityPosting.ToString().ToLower());

                #region Global Fields

                settingActionValue(Setting.DisableIssueManagement, formModel.DisableIssueManagement.ToString().ToLower());
                settingActionValue(Setting.EnableShoppingCart, formModel.EnableShoppingCart.ToString().ToLower());
                settingActionValue(Setting.DefaultRoute, (formModel.DefaultRoute ?? "").Trim());
                settingActionValue(Setting.SearchExactMatch, formModel.EnableSearchExactMatch.ToString().ToLower());
                settingActionValue(Setting.HideData3SixtyUsers, formModel.HideData3SixtyUsers.ToString().ToLower());
                settingActionValue(Setting.ShowAllUsersAPIKey, formModel.ShowAllUsersAPIKey.ToString().ToLower());
                settingActionValue(Setting.WorkflowCatchAllGroup, formModel.WorkflowCatchAllGroup.ToString());
                settingActionValue(Setting.WorkflowDigestEmailDays, formModel.WorkflowDigestEmailDays.ToString());
                settingActionValue(Setting.MaxDropdownItems, Math.Abs(formModel.MaxDropdownItems).ToString());
                settingActionValue(Setting.WriteActionDescription, formModel.WriteActionDescription.ToString().ToLower());
                settingActionValue(Setting.MaxExcelExportRows, Math.Abs(formModel.MaxExcelExportRows).ToString());

                #endregion

                #region IP

                currentSetting = Setting.IpRestriction;

                var ipValidationCheckPassed = true;
                if (formModel.IpRestrictions != null)
                {
                    var xml = new XElement("ips");
                    foreach (var ip in formModel.IpRestrictions)
                    {
                        if (string.IsNullOrEmpty(ip.Name) || string.IsNullOrEmpty(ip.Start) || string.IsNullOrEmpty(ip.End))
                        {
                            ipValidationCheckPassed = false;
                            break;
                        }
                        else
                        {
                            xml.Add(new XElement("ip",
                                new XElement("name", ip.Name),
                                new XElement("start", ip.Start),
                                new XElement("end", ip.End)
                            ));
                        }
                    }
                    if (ipValidationCheckPassed)
                    {
                        settingActionValue(currentSetting, xml.ToString());
                    }
                    else
                    {
                        throw new MissingPropertiesException("IP Restrictions");
                    }
                }
                else
                {
                    SettingsRepository.DeleteSetting(currentSetting);
                }

                #endregion

                // Search
                settingActionValue(Setting.DefaultSearchTypes, (formModel.DefaultSearchTypes ?? "").ToString());

                // Header Styles
                settingActionValue(Setting.HeaderBackgroundColor, formModel.HeaderBackgroundColor);

                #region Home Page Customization

                settingActionValue(Setting.ShowHomeAssignmentTile, formModel.ShowHomeAssignmentTile.ToString().ToLower());
                settingActionValue(Setting.ShowHomeBoardTile, formModel.ShowHomeBoardTile.ToString().ToLower());
                settingActionValue(Setting.ShowHomeActivityTile, formModel.ShowHomeActivityTile.ToString().ToLower());
                settingActionValue(Setting.ShowHomePageTitle, formModel.ShowHomePageTitle.ToString().ToLower());
                settingActionValue(Setting.BrowserTitlePrefix, formModel.BrowserTitlePrefix);

                //prevent the user from entering special characters
                var alphaNumericChars = "abcdefghijklmnopqrstuvwxyz0123456789";
                var sizeAllowedChars = alphaNumericChars + ".";
                var colorAllowedChars = alphaNumericChars + "#";

                var safeSize = System.Text.RegularExpressions.Regex.Replace(formModel.HomePageTitleSize?.Trim() ?? "", $"[^{sizeAllowedChars}]", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var safeColor = System.Text.RegularExpressions.Regex.Replace(formModel.HomePageTitleColor?.Trim() ?? "", $"[^{colorAllowedChars}]", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                settingActionValue(Setting.HomePageTitleSize, safeSize);
                settingActionValue(Setting.HomePageTitleColor, safeColor);

                #region Home Page Background Image

                currentSetting = Setting.HomePageBackgroundImage;
                currentSettingInfo = settings.Single(s => s.ID == currentSetting);
                if (formModel.ClearHomePageBackgroundImage)
                {
                    settingAction(currentSetting, currentSettingInfo, true);
                }
                else
                {
                    if (!string.IsNullOrEmpty(formModel.HomePageBackgroundImage))
                    {
                        var imageMatch = MimeTypeExtensionsMap.RegEx.Match(formModel.HomePageBackgroundImage);

                        var imageMime = imageMatch.Groups["mime"].Value;
                        var imageData = imageMatch.Groups["data"].Value;
                        var imageExtension = MimeTypeExtensionsMap.GetExtension(imageMime);
                        if (imageExtension == null)
                        {
                            return jsonException(string.Format("Invalid file type: {0} cannot be uploaded.", imageMime), HttpStatusCode.BadRequest);
                        }
                        var imageByteArray = Convert.FromBase64String(imageData);
                        var imageGuid = Guid.NewGuid();

                        using (var imageStream = new MemoryStream(imageByteArray))
                        {
                            var filesToDelete = Storage.ListFilenamesByPrefix(constants.COMPANY_RESOURCES_FOLDER, $"{Company.CurrentCompanyID}.home.");
                            filesToDelete.ForEach(f =>
                            {
                                Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, f).Wait();
                            });

                            var imageFileName = string.Format("{0}.home.{1}{2}", Company.CurrentCompanyID, imageGuid, imageExtension);
                            await Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);
                            
                            settingActionValue(currentSetting, $"{constants.COMPANY_RESOURCES_URL}{imageFileName}");
                        }
                    }
                }

                #endregion

                #endregion

                #region Security

                currentSetting = Setting.AllowedOrigins;
                currentSettingInfo = settings.Single(s => s.ID == currentSetting);
                if (string.IsNullOrWhiteSpace(formModel.AllowedOrigins))
                {
                    settingAction(currentSetting, currentSettingInfo, true);
                }
                else
                {
                    var origins = formModel.AllowedOrigins
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
                        .ToList();

                    currentSettingInfo.Value = string.Join(",", origins);
                    settingAction(currentSetting, currentSettingInfo, false);
                }

                currentSetting = Setting.FramingDomains;
                currentSettingInfo = settings.Single(s => s.ID == currentSetting);
                if (string.IsNullOrWhiteSpace(formModel.FramingDomains))
                {
                    settingAction(currentSetting, currentSettingInfo, true);
                }
                else
                {
                    var domains = formModel.FramingDomains
                        .Split(',')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
                        .ToList();

                    currentSettingInfo.Value = string.Join(",", domains);
                    settingAction(currentSetting, currentSettingInfo, false);
                }

                #endregion

                return jsonSuccess("Settings successfully updated.", "0", "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.GetFullExceptionData(), HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet, Route("GetSiteNavFolderItems"), NonNullableParameters]
        public JsonNetResult GetSiteNavFolderItems(int id)
        {
            var sql = @"SELECT v.ID
                          ,v.ParentID
                          ,COALESCE(a.Name,v.Name) as Name
                          ,v.Route
                          ,v.SortOrder
                          ,v.ObjectID
                          ,v.[Object]
                          ,v.Icon
                          ,v.Title
                      FROM [dbo].[SiteNav] v
		                    left join assettype a on a.objectid = v.objectID and v.Object = 'ArtifactType' and a.Object = 'ArtifactType'
                            WHERE   v.ParentID = @parentId
                            ORDER BY v.SortOrder";

            IList<SiteNav> items = Company.Query<SiteNav>(sql, new { parentId = id }).ToList();
            var maxSortOrderVal = items.Max(p => p.SortOrder);
            maxSortOrderVal = maxSortOrderVal == null ? 0 : maxSortOrderVal;
            foreach (SiteNav i in items)
            {
                if (i.SortOrder == null)
                {
                    var sortOrder = ++maxSortOrderVal;
                    SiteNav siteNav = Company.GetById<SiteNav>(i.ID);
                    siteNav.SortOrder = sortOrder;
                    i.SortOrder = sortOrder;
                    Company.Update(siteNav);
                }
            }
            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Load

        public class OptionModel
        {
            public string title { get; set; }
            public string value { get; set; }
        }

        public class LoadFilePostModel
        {
            public string LoadAction { get; set; }
            public string Type { get; set; }
            public string Notes { get; set; }
            public string File { get; set; }
        }

        [Route("Load_TypeOptions"), NonNullableParameters]
        public JsonNetResult Load_TypeOptions(string act)
        {
            IEnumerable<OptionModel> models = null;

            var sql = "";
            switch (act)
            {
                case "O":   // Responsibility/Ownership
                    #region
                    sql = $@"
select * from (
select 'ArtifactType|0' as value, '{CommonNames.AssetTypeClass_Business.CleanForSql()}' as title
union
select 'ArtifactType|0' as value, '{CommonNames.AssetTypeClass_Business.CleanForSql()}' as title
union
select 'TaxonomyType|0' as value, '{CommonNames.AssetTypeClass_Model.CleanForSql()}' as title
union
select 'PolicyType|0' as value, '{CommonNames.AssetTypeClass_Policy.CleanForSql()}' as title
union
select 'ReferenceItemType|0' as value, 'Reference' as title
) O order by title";
                    break;
                #endregion
                case "P":   // Promotion
                    #region
                    sql = $@"
select	value,
		title
from	(
		select		1 as Sort,
					'ArtifactType|' + cast(ObjectID as varchar(10)) as value, 
					'{CommonNames.AssetTypeClass_Business.CleanForSql()}: ' + P.[Path] as title 
		from		AssetType A
					cross apply dbo.GetAssetTypeTextPathById(A.ID, ' > ') P
		where		[Class] = 1 

		union

		select		2 as Sort,
					'TaxonomyType|' + cast(ObjectID  as varchar(10)) as value, 
					'{CommonNames.AssetTypeClass_Model.CleanForSql()}: ' + Name as title 
		from		AssetType
		where		[Class] = 2

		union

		select		3 as Sort,
					'ReferenceItemType|' + cast(ObjectID  as varchar(10)) as value, 
					'Reference Item: ' + P.[Path] as title 
		from		AssetType  A
					cross apply dbo.GetAssetTypeTextPathById(A.ID, ' > ') P
		where		[Class] = 9

		union

		select		8 as Sort,
					'ArtifactType|' + cast(ObjectID as varchar(10)) as value, 
					'{CommonNames.AssetTypeClass_Technical.CleanForSql()}: ' + P.[Path] as title 
		from		AssetType A
					cross apply dbo.GetAssetTypeTextPathById(A.ID, ' > ') P
		where		[Class] = 8 
        union
		select		9 as Sort,
					'RuleType|' + cast(ObjectID as varchar(10)) as value, 
					'{CommonNames.AssetTypeClass_Rule.CleanForSql()}: ' + P.[Path] as title 
		from		AssetType A
					cross apply dbo.GetAssetTypeTextPathById(A.ID, ' > ') P
		where		[Class] = 7
		) O
order by Sort, title";
                    break;
                #endregion
                case "R":   // Relation
                case "U":   // Unrelation
                    #region
                    sql = $@"select 'IntersectType|' + cast(itd.ID as varchar(10)) as value, IName.Name as title from intersecttypedetail itd cross apply dbo.GetIntersectTypeNames(itd.ID) IName where (itd.IsSystem = 0 or (itd.Subject = 'ReferenceItemType' and itd.Object = 'ReferenceItemType')) and itd.predicatetype not in (3,4,{(int)PredicateType.Diagram},{(int)PredicateType.DiagramUse},{(int)PredicateType.DiagramReference}) order by IName.Name";
                    break;
                #endregion
                case "M":   // Users/Groups
                    models = new List<OptionModel> {
                        new OptionModel { title = "Group Membership", value = "Membership|0" },
                        new OptionModel { title = "Users", value = "Membership|1" }
                    };
                    break;
                case "BL":   // Lineage
                    models = new List<OptionModel> { new OptionModel { title = "Default", value = "Lineage|-1" } };
                    break;
                default:
                    models = new List<OptionModel>();
                    break;
            }

            if (!string.IsNullOrEmpty(sql))
            {
                models = Company.Query<OptionModel>(sql);
            }

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("Load_ExpectedColumns"), NonNullableParameters]
        public JsonNetResult Load_ExpectedColumns(string action, string type, int id)
        {
            return new JsonNetResult
            {
                Data = Company.GetLoadColumns(action, type, id, false),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [FileDownload, Route("Load_ExpectedColumns_ToExcel"), NonNullableParameters]
        public FileResult Load_ExpectedColumns_ToExcel(string action, string type, int id)
        {
            var document = new SLDocument();
            var defaultSheet = "Items";
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, defaultSheet);
            document.AddWorksheet("Lookups");
            document.SelectWorksheet(defaultSheet);

            var models = Company.GetLoadColumns(action, type, id, true);
            var lookupColumns = 1;

            #region Header

            for (int i = 0; i < models.Count; i++)
            {
                var model = models[i];
                SLStyle style = document.CreateStyle();

                style.Font.Bold = model.Required || model.PartOfKey;

                document.SetCellStyle(1, i + 1, style);

                document.SetCellValue(1, i + 1, model.Name);

                if (model.IsLookup && !model.AllowMultipleValues && model.Lookups != null)
                {
                    IEnumerable<string> values;

                    values = model.Lookups.Select(m => (string.IsNullOrEmpty(m.Value) ? m.Label : $"{m.Label} [{m.Value}]"));

                    var dv = document.CreateDataValidation(2, i + 1, model.Lookups.Count + 1, i + 1);
                    CreateExcelList(lookupColumns++, document, "Lookups", dv, values);
                    document.AddDataValidation(dv);
                }

                document.AutoFitColumn(1, i + 1);
            }

            #endregion

            document.HideWorksheet("Lookups");

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", "Load.xlsx");
        }

        private void CreateExcelList(int numLookupColumns, SLDocument document, string lookupWorksheetName, SLDataValidation dataValidation, IEnumerable<string> values)
        {
            if (!values.Any())
            {
                return;
            }

            var currentSheet = document.GetCurrentWorksheetName();
            document.SelectWorksheet(lookupWorksheetName);
            int rowNum = 0;
            foreach (var item in values)
            {
                document.SetCellValue(++rowNum, numLookupColumns, WebUtility.HtmlDecode(item));
            }

            document.SelectWorksheet(currentSheet);

            //add a column to the given lookup worksheet with the specified values
            string range = SLConvert.ToCellRange(lookupWorksheetName, 1, numLookupColumns, rowNum, numLookupColumns, true);
            dataValidation.AllowList($"={range}", true, true);
        }

        #region Internal Models for Load File Validation

        internal class LevelField
        {
            public int Level { get; set; }
            public string Name { get; set; }
            public bool PartOfKey { get; set; }
            public bool Required { get; set; }
            public int ColumnIndex { get; set; }
            public bool DataLoaded { get; set; } = false;
        }

        internal class LoadLevelStatus
        {
            public int Level { get; set; }
            public bool Required { get; set; }
            public bool DataLoaded { get; set; } = false;
        }

        internal class LoadLevelStatusComparer : IEqualityComparer<LoadLevelStatus>
        {
            public bool Equals(LoadLevelStatus x, LoadLevelStatus y)
            {
                return (x.Level == y.Level);
            }

            public int GetHashCode(LoadLevelStatus obj)
            {
                return obj.Level.GetHashCode();
            }
        }

        #endregion

        [HttpPost, AjaxValidateAntiForgeryToken, Route("AddLoad")]
        public async Task<JsonResult> AddLoad()
        {
            try
            {
                Stream inputStream = Request.InputStream;
                string postJson = new StreamReader(inputStream).ReadToEnd();
                LoadFilePostModel model = JsonConvert.DeserializeObject<LoadFilePostModel>(postJson);


                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                // Perform checks to make sure fields are populated.
                if (string.IsNullOrEmpty(model.Type))
                {
                    throw new NoFormDataException("Type");
                }

                if (string.IsNullOrEmpty(model.LoadAction))
                {
                    throw new NoFormDataException("LoadAction");
                }

                var match = MimeTypeExtensionsMap.RegEx.Match(model.File);

                var mime = match.Groups["mime"].Value;
                var data = match.Groups["data"].Value;
                var extension = MimeTypeExtensionsMap.GetExtension(mime);
                var byteArray = Convert.FromBase64String(data);

                JsonResult json;
                Load load = null;
                var errorMessages = new List<string>();
                SLDocument xls;

                using (var stream = new MemoryStream(byteArray))
                {
                    if (extension == ".xlsx")
                    {
                        var typeInfo = model.Type.Split('|');
                        var typeParams = new { @object = typeInfo[0], objectID = int.Parse(typeInfo[1]) };

                        var assetTypeUid = Company.Query<Guid?>("select [uid] from AssetType where Object = @object and ObjectID = @objectID", typeParams).FirstOrDefault();
                        var intersectTypeUid = Company.Query<Guid?>("select [uid] from [IntersectType] where ID = @objectID and @object = 'IntersectType'", typeParams).FirstOrDefault();

                        load = new Load
                        {
                            File = stream.ToArray(),
                            Action = model.LoadAction,
                            Extension = extension,
                            Notes = model.Notes,
                            Object = typeParams.@object,
                            ObjectID = typeParams.objectID,
                            DateStarted = DateTime.UtcNow,
                            UpdatedBy = Company.CurrentResourceID,
                            AssetTypeUid = assetTypeUid,
                            IntersectTypeUid = intersectTypeUid
                        };

                        xls = new SLDocument(stream);

                        var fieldTypeNames = Company.GetLoadColumns(load.Action, load.Object, load.ObjectID, false);

                        var stats = xls.GetWorksheetStatistics();
                        int columnCount = 0;

                        for (int i = 1; i <= stats.NumberOfColumns; i++)
                        {
                            var testValue = xls.GetCellValueAsString(1, i);
                            if (string.IsNullOrEmpty(testValue))
                            {
                                errorMessages.Add($"Invalid column header in column {i}.");
                            }
                            else
                            {
                                columnCount++;
                            }
                        }

                        if (errorMessages.Count == 0)
                        {
                            // Spreadsheet should not have more columns than the type has, but it can have less.
                            // Spreadsheet should only contain columns that the type has.
                            if (columnCount <= fieldTypeNames.Count)
                            {
                                load.LoadColumns = new List<LoadColumn>();

                                #region Loop through spreadsheet columns and make sure type has that column defined.
                                for (var i = stats.StartColumnIndex; i <= stats.EndColumnIndex; i++)
                                {
                                    var columnName = (xls.GetCellValueAsString(1, i) ?? string.Empty).Trim();

                                    if (string.IsNullOrEmpty(columnName)) continue;

                                    if (!fieldTypeNames.Any(x => x.Name == columnName))
                                    {
                                        errorMessages.Add($"Unexpected column found [{columnName}]");
                                    }
                                    else
                                    {

                                        if (load.Action == "P" && load.LoadColumns.Any(l => l.Name == columnName))
                                        {
                                            errorMessages.Add($"Duplicate column found [{columnName}]");
                                        }
                                        else
                                        {
                                            load.LoadColumns.Add(new LoadColumn { ColumnIndex = i, Name = columnName });
                                        }
                                    }
                                }
                                #endregion

                                Func<int, bool> allColumnRowsHaveValue = delegate (int columnIndex)
                                {
                                    bool returnValue = true;

                                    if (columnIndex >= 0)
                                    {
                                        returnValue = stats.EndRowIndex > stats.StartRowIndex; // If there is only header row, this fails.

                                        for (var i = stats.StartRowIndex + 1; i <= stats.EndRowIndex; i++)
                                        {
                                            if (returnValue) // Continue checking ONLY if we are still set to TRUE.
                                            {
                                                var rowValue = (xls.GetCellValueAsString(i, columnIndex) ?? string.Empty).Trim();

                                                if (string.IsNullOrEmpty(rowValue))
                                                {
                                                    returnValue = false;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        returnValue = false;
                                    }

                                    return returnValue;
                                };

                                // This is where we do our key check, split by ordered Level.
                                var levelFields = (
                                                  from fl in fieldTypeNames
                                                  select new LevelField
                                                  {
                                                      Level = fl.Level,
                                                      Name = fl.Name,
                                                      PartOfKey = fl.PartOfKey,
                                                      Required = fl.Required,
                                                      ColumnIndex = load.LoadColumns.Any(lc => lc.Name == fl.Name) ? load.LoadColumns.First(lc => lc.Name == fl.Name).ColumnIndex : -1
                                                  }
                                                  ).OrderBy(i => i.Level).ToList();

                                // Determine which key/required columns are fully loaded.
                                foreach (var lf in levelFields.Where(f => f.PartOfKey || f.Required))
                                {
                                    lf.DataLoaded = allColumnRowsHaveValue(lf.ColumnIndex);
                                }

                                var requiredLevels = levelFields
                                    .Select(i => new LoadLevelStatus
                                    {
                                        Level = i.Level,
                                        Required = false
                                    })
                                    .Distinct(new LoadLevelStatusComparer())
                                    .OrderByDescending(l => l.Level)
                                    .ToList();

                                // Determine which levels are required.
                                requiredLevels.ForEach(l =>
                                {
                                    l.DataLoaded = !levelFields.Any(f => f.Level == l.Level && (f.PartOfKey || f.Required) && !f.DataLoaded);

                                    if (l.Level == 1)
                                    {
                                        // Level 1 is always required.
                                        l.Required = true;
                                    }
                                    else
                                    {
                                        if (requiredLevels.Any(p => p.Level == l.Level + 1 && p.DataLoaded))
                                        {
                                            l.Required = true; // Since level below CURRENT is data-populated, then CURRENT is required.
                                        }
                                        else
                                        {
                                            l.Required = l.DataLoaded;
                                        }
                                    }
                                });

                                List<string> invalidKeyFields = new List<string>();
                                List<string> invalidRequiredFields = new List<string>();

                                // Log missing required column messages.
                                requiredLevels.ForEach(l =>
                                {
                                    if (l.Required)
                                    {
                                        // Log any missing key field errors.
                                        errorMessages.AddRange(
                                            levelFields
                                            .Where(f => f.Level == l.Level && f.PartOfKey && f.Required && f.ColumnIndex == -1)
                                            .Select(f => $"Key column not provided [{f.Name}]")
                                        );

                                        // Log any missing required, non-key field errors.
                                        errorMessages.AddRange(
                                            levelFields
                                            .Where(f => f.Level == l.Level && f.Required && !f.PartOfKey && f.ColumnIndex == -1)
                                            .Select(f => $"Required column not provided [{f.Name}]")
                                        );

                                        // Get any key columns that do not have data populated for this level.
                                        invalidKeyFields.AddRange(
                                            levelFields.Where(lf => lf.Level == l.Level && lf.PartOfKey && lf.Required && lf.ColumnIndex > -1 && !lf.DataLoaded).Select(lf => lf.Name)
                                        );

                                        // Get any required, non-key columns that do not have data populated for this level.
                                        invalidRequiredFields.AddRange(
                                            levelFields.Where(lf => lf.Level == l.Level && lf.Required && !lf.PartOfKey && lf.ColumnIndex > -1 && !lf.DataLoaded).Select(lf => lf.Name)
                                        );
                                    }
                                });

                                if (invalidKeyFields.Count > 0)
                                {
                                    errorMessages.Add($"One or more values not populated for Key column{(invalidKeyFields.Count > 1 ? "s" : "")} [{string.Join(", ", invalidKeyFields)}]");
                                }
                                if (invalidRequiredFields.Count > 0)
                                {
                                    errorMessages.Add($"One or more values not populated for Required column{(invalidRequiredFields.Count > 1 ? "s" : "")} [{string.Join(", ", invalidRequiredFields)}]");
                                }
                            }
                            else
                            {
                                errorMessages.Add("The number of columns in the spreadsheet exceeds the number of defined fields for this load type.");
                            }
                        }
                    }
                    else
                    {
                        errorMessages.Add("Incorrect file type");
                    }
                }

                if (errorMessages.Count == 0)
                {
                    load.File = null;
                    Company.Add<Load>(load);
                    await Storage.CreateFolder($"{constants.COMPANY_BULK_LOAD_FOLDER}");
                    await Storage.CreateFile($"{constants.COMPANY_BULK_LOAD_FOLDER}", $"{Company.CurrentCompanyID}/load_{load.ID}.{load.Extension}", new MemoryStream(byteArray));
                    Company.Enqueue(Config.GetValue<string>("BulkLoadQueue"), new BulkLoadInfo { CompanyID = Company.CurrentCompanyID, LoadID = load.ID, To = QueueAction.BulkLoad });

                    json = jsonSuccess("File uploaded and queued for processing.", load.ID.ToString(), "A", HttpStatusCode.Created);
                }
                else
                {
                    json = jsonException(string.Join(";", errorMessages), HttpStatusCode.BadRequest);
                }

                return json;
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("loads/{id:int}/Errors.xlsx"), FileDownload, HttpGet]
        public FileResult ErrorLoadFile(int id)
        {
            //only admins can access this route
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            var load = Company.GetById<Load>(id);

            var itemSql = @"select RowIndex, StatusMessage from LoadItem where LoadID = @id and Status = 0 order by RowIndex asc";
            var itemColumnSql = @"select C.* from LoadItem I inner join LoadItemColumn C on C.LoadID = I.LoadID and I.RowIndex = C.RowIndex and I.LoadID = @id and I.Status = 0 order by I.RowIndex asc, C.ColumnIndex asc";

            if (load.PutExecutionID.HasValue || load.PostExecutionID.HasValue)
            {
                itemSql = @"select L.RowIndex, EA.[Message] as StatusMessage from LoadItem L
inner join (
		select ExecutionId, ItemNumber, ExecutionItemUid, ParentAssetID, Message, Success from api.ExecutionAsset where success = 0
		union all
		select ExecutionID, ItemNumber, ExecutionItemUid, null as ParentAssetID, Message, cast(0 as bit) as Success from api.ExecutionAssetError
	 )  EA on EA.ExecutionItemUid = L.ExecutionItemUid
where L.LoadID = @id order by RowIndex asc";

                itemColumnSql = @"
select C.LoadID, C.RowIndex, C.ColumnIndex, coalesce(EF.FieldValue, C.[Value]) as [Value] 
from LoadItem I 
inner join (
		select ExecutionId, ItemNumber, ExecutionItemUid, ParentAssetID, Message, Success from api.ExecutionAsset where success = 0
		union all
		select ExecutionID, ItemNumber, ExecutionItemUid, null as ParentAssetID, Message, cast(0 as bit) as Success from api.ExecutionAssetError
	 )  EA on EA.ExecutionItemUid = I.ExecutionItemUid
left join LoadItemColumn C on C.LoadID = I.LoadID and I.RowIndex = C.RowIndex and I.LoadID = @id 
left join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
left join api.ExecutionField EF on EF.ExecutionId = EA.ExecutionID and EF.ItemNumber = EA.ItemNumber and EF.FieldName = LC.[Name]
order by I.RowIndex asc, C.ColumnIndex asc";
            }


            var loadColumns = Company.Filter<LoadColumn>(i => i.LoadID == id).OrderBy(i => i.ColumnIndex).ToList();
            var loadItems = Company.Query<dynamic>(itemSql, new { id }).ToList();
            var loadItemColumns = Company.Query<dynamic>(itemColumnSql, new { id }).ToList();

            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            var r = 1;
            var columnCount = loadColumns.Count;

            #region Header

            foreach (var lc in loadColumns)
            {
                document.SetCellValue(r, lc.ColumnIndex, lc.Name);
            }
            document.SetCellValue(r, columnCount + 1, "Error");
            document.SetRowStyle(r, new SLStyle { Font = new SLFont { Bold = true } });
            document.FreezePanes(1, columnCount);

            #endregion


            foreach (var i in loadItems)
            {
                r++;
                foreach (var lic in loadItemColumns.Where(c => c.RowIndex == (int)i.RowIndex).OrderBy(c => c.ColumnIndex))
                {
                    document.SetCellValue(r, lic.ColumnIndex, (string)lic.Value);
                }
                document.SetCellValue(r, columnCount + 1, i.StatusMessage);
            }

            document.SetColumnStyle(columnCount + 1, new SLStyle { Font = new SLFont { FontColor = System.Drawing.Color.Red } });

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);

            return File(stream.ToArray(), "application/vnd.ms-excel", $"Errors-{id}.xlsx");
        }

        [Route("loads/{id:int}/all.xlsx"), FileDownload, HttpGet]
        public FileResult FullLoadFile(int id)
        {
            //only admins can access this route
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            var load = Company.GetById<Load>(id);
            var bytes = load.File;

            if (bytes == null)
            {
                var fileString = Storage.GetFileContentsAsString($"{constants.COMPANY_BULK_LOAD_FOLDER}", $"{Company.CurrentCompanyID}/load_{load.ID}.{load.Extension}");
                bytes = Encoding.Default.GetBytes(fileString);
            }
            return File(bytes, "application/vnd.ms-excel", $"{load.DateCompleted.ToString()}.xlsx");
        }

        #endregion

        #region Reference Item

        /// <param name="id">LookupTypeID</param>
        [Route("ReferenceItem_AddFields"), NonNullableParameters]
        public JsonResult ReferenceItem_AddFields(int id)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.ReferenceItemType, id, Permission.AddAsset))
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var row = 1;

            list.Add(new EditableField { FieldName = "ReferenceItemTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = row++, Column = 1, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Code", true, "", 1, 250, "Must be between 1 and 250 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = row++, Column = 1, FieldName = "Color", Name = "Color", FieldType = DataType.Color.ToString() });

            //if the reference type has a parent we need to add parent field with the values from the parent

            var parentType = Company.GetParentType(id, SystemObjects.ReferenceItemType);
            if (parentType != null)
            {
                var sql = "select DisplayValue, uid from assetdetail where [object] = 'Referenceitem' and TypeID = @id";
                list.Add(new EditableField { Row = row++, Column = 1, FieldName = "ParentUid", Name = parentType.Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = false, Items = Company.Query<dynamic>(sql, new { id = parentType.ObjectID }).Select(i => new SelectListItem { Text = i.DisplayValue, Value = string.Format("{0}", i.uid) }).ToList() });
            }


            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, id).ToList(), row, false);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
        [Route("ReferenceItem_EditFields"), NonNullableParameters]
        public JsonResult ReferenceItem_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.Assets.FirstOrDefault(x => x.ObjectID == id && x.Object == "ReferenceItem");

            if (!Company.HasAssetPermission(SystemObjects.ReferenceItem, a.ObjectID, Permission.EditAsset))
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var row = 1;
            //resolve the color correctly from the Id or hex value
            var color = Company.Query<string>($@"SELECT top 1 COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Name'), '') as Text FROM Asset A cross apply dbo.GetAssetColorJsonByColor(A.Color) ACJ  WHERE A.ID = {a.ID}").SingleOrDefault();
            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { Row = row++, Column = 1, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString(), Value = a.Code.ToString(), Validations = checkAndAddValidation("Text", "Code", true, "", 1, 250, "Must be between 1 and 250 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = row++, Column = 1, FieldName = "Color", Name = "Color", FieldType = DataType.Color.ToString(), Value = color });

            //if the reference type has a parent we need to add parent field with the values from the parent

            var parentType = Company.GetParentType(a.AssetType.ObjectID, SystemObjects.ReferenceItemType);

            if (parentType != null)
            {
                var parent = Company.GetParentObject(id, SystemObjects.ReferenceItem);
                var sql = "select DisplayValue, uid from assetdetail where [object] = 'Referenceitem' and TypeID = @id";
                list.Add(new EditableField
                {
                    Row = row++,
                    Column = 1,
                    FieldName = "ParentUid",
                    Name = parentType.Name,
                    FieldType = DataType.Lookup.ToString(),
                    Required = true,
                    MultiSelect = false,
                    Value = ((parent != null) ? (parent.uid.ToString() ?? "").ToLower() : ""),
                    Items = Company.Query<dynamic>(sql, new { id = parentType.ObjectID }).Select(i => new SelectListItem { Text = i.DisplayValue, Value = string.Format("{0}", i.uid), Selected = i.uid == (parent != null ? parent.uid : Guid.Empty) }).ToList()
                });
            }

            list = loadDynamicFields(SystemObjects.ReferenceItem.ToString(), id, list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, a.AssetType.ObjectID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.ReferenceItem, id).ToList(), row, false, false);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ShoppingCart

        [HttpPut, Route("shoppingcart/add")]
        public JsonResult AddShoppingCartItem(string type, int id, int cartTypeID)
        {
            var carts = Company.ShoppingCarts.Where(s => s.ResourceID == Company.CurrentResourceID && s.ShoppingCartTypeID == cartTypeID && s.RequestedOn == null).ToList();
            ShoppingCart myCart = new ShoppingCart();

            if (carts.Count == 0)
            {
                myCart = new ShoppingCart();
                myCart.ResourceID = Company.CurrentResourceID;
                myCart.ShoppingCartTypeID = cartTypeID;
                myCart.RequestedOn = null;
                myCart.CreatedOn = DateTime.UtcNow;

                try
                {
                    Company.ShoppingCarts.Add(myCart);
                    Company.SaveChanges();
                }
                catch (Exception ex)
                {
                    return jsonException(ex, HttpStatusCode.InternalServerError);
                }

            }
            else if (carts.Count == 1)
            {
                myCart = carts[0];
            }
            else if (carts.Count > 1)
            {
                return jsonException("An error occurred - there are more than 1 open carts for this user", HttpStatusCode.InternalServerError);
            }

            if (myCart == null)
            {
                return jsonException("The specified cart could not be found", HttpStatusCode.NotFound);
            }

            if (myCart.ResourceID != Company.CurrentResourceID)
            {
                return jsonException("You do not have permission to add items to this cart", HttpStatusCode.Forbidden);
            }

            var existingItem = Company.ShoppingCartItems.Where(i => i.ShoppingCartID == myCart.ID && i.Object == type && i.ObjectID == id).FirstOrDefault();

            if (existingItem == null)
            {
                ShoppingCartItem item = new ShoppingCartItem();
                item.ShoppingCartID = myCart.ID;
                item.Object = type;
                item.ObjectID = id;
                item.AddedOn = DateTime.UtcNow;

                try
                {
                    Company.ShoppingCartItems.Add(item);
                    Company.SaveChanges();
                }
                catch (Exception ex)
                {
                    return jsonException(ex, HttpStatusCode.InternalServerError);
                }

            }
            else
            {
                return jsonException("This item is already in your cart", HttpStatusCode.OK);
            }

            return jsonSuccess("The item has been added to your cart", id.ToString(), "add", HttpStatusCode.OK);

        }

        [HttpDelete, Route("shoppingcart/remove")]
        public JsonResult RemoveShoppingCartItem(string type, int id, int shoppingCartID)
        {

            var cart = Company.GetById<ShoppingCart>(shoppingCartID);

            if (cart == null)
            {
                return jsonException("Could not find the shopping cart specified.", HttpStatusCode.NotFound);
            }

            if (cart.ResourceID != Company.CurrentResourceID)
                return jsonException("You do not have permission to remove this item", HttpStatusCode.Forbidden);

            var item = Company.ShoppingCartItems.Where(i => i.ShoppingCartID == shoppingCartID && i.Object == type && i.ObjectID == id).FirstOrDefault();
            if (item == null)
            {
                return jsonException("Shopping cart item could not be found", HttpStatusCode.NotFound);
            }

            try
            {
                Company.ShoppingCartItems.Remove(item);
                Company.SaveChanges();
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
            return jsonSuccess("Shopping cart item removed successfully", id.ToString(), "delete", HttpStatusCode.OK);
        }

        [HttpGet, Route("shoppingcart/list/{typeID:int}")]
        public JsonNetResult GetMyShoppingCart(int typeID)
        {
            var cart = Company.ShoppingCarts.Where(s => s.ResourceID == Company.CurrentResourceID && s.ShoppingCartTypeID == typeID && s.RequestedOn == null).FirstOrDefault();
            if (cart == null)
            {
                return new JsonNetResult
                {
                    Data =
                    new
                    {
                        Cart = (ShoppingCart)null,
                        Items = (dynamic)null
                    },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }

            if (cart.ResourceID > 0)
            {
                cart.Requestor = Company.Query<string>("select FirstName + ' ' + LastName as Requestor from reporting.Global_Resource where ResourceID = @id", new { id = cart.ResourceID }).SingleOrDefault();
            }

            var items = Company.Query<dynamic>(QueryConstants.ShoppingCartItemList, new { id = cart.ID }).ToList();

            return new JsonNetResult
            {
                Data = new
                {
                    Cart = cart,
                    Items = items
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };


        }

        [HttpGet, Route("shoppingcart/list/{typeID:int}/{cartID:int}")]
        public JsonNetResult GetShoppingCart(int typeID, int cartID)
        {
            var cart = Company.GetById<ShoppingCart>(cartID);
            var items = Company.Query<dynamic>(QueryConstants.ShoppingCartItemList, new { id = cart.ID }).ToList();

            if (cart != null && cart.ResourceID > 0)
            {
                cart.Requestor = Company.Query<string>("select FirstName + ' ' + LastName as Requestor from reporting.Global_Resource where ResourceID = @id", new { id = cart.ResourceID }).SingleOrDefault();
            }


            return new JsonNetResult
            {
                Data = new
                {
                    Cart = cart,
                    Items = items
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }


        [HttpPost, AjaxValidateAntiForgeryToken, Route("shoppingcart/request")]
        public JsonResult RequestShoppingCart(ShoppingCart cart)
        {
            var myCart = Company.GetById<ShoppingCart>(cart.ID);
            if (myCart == null)
            {
                return jsonException("Could not find shopping cart", HttpStatusCode.NotFound);
            }

            if (myCart.ResourceID != Company.CurrentResourceID)
            {
                return jsonException("You do not have permission to request this shopping cart.", HttpStatusCode.Forbidden);
            }

            try
            {
                myCart.RequestedOn = DateTime.UtcNow;
                myCart.Request = cart.Request;

                Company.SaveChanges();
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

            return jsonSuccess("Your request has been submitted", cart.ID.ToString(), "update", HttpStatusCode.OK);

        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("shoppingcart/clear")]
        public JsonResult EmptyShoppingCart(int cartID)
        {
            try
            {
                var cart = Company.GetById<ShoppingCart>(cartID);
                if (cart == null)
                {
                    return jsonException("Could not find the specified cart.", HttpStatusCode.NotFound);
                }

                if (cart.ResourceID != Company.CurrentResourceID)
                {
                    return jsonException("You do not have permission to clear this cart.", HttpStatusCode.Forbidden);
                }

                var items = Company.ShoppingCartItems.Where(i => i.ShoppingCartID == cartID).ToList();
                Company.ShoppingCartItems.RemoveRange(items);
                Company.SaveChanges();
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

            return jsonSuccess("Shopping cart cleared successfully", cartID.ToString(), "update", HttpStatusCode.OK);

        }

        #endregion

        #region Shortcut

        [HttpPost, AjaxValidateAntiForgeryToken, Route("shortcut/add")]
        public async Task<JsonResult> AddShortcut(Shortcut shortcut)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException("You do not have permission to edit shortcuts.", HttpStatusCode.Forbidden);
            }

            if (string.IsNullOrEmpty(shortcut.Name))
            {
                return jsonException("This shortcut requires a name", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(shortcut.Icon) && string.IsNullOrEmpty(shortcut.IconPayload))
            {
                return jsonException("This shortcut is missing an icon", HttpStatusCode.BadRequest);
            }

            try
            {
                if (!string.IsNullOrEmpty(shortcut.IconPayload))
                {
                    var imageMatch = MimeTypeExtensionsMap.RegEx.Match(shortcut.IconPayload);

                    var imageMime = imageMatch.Groups["mime"].Value;
                    var imageData = imageMatch.Groups["data"].Value;
                    var imageExtension = MimeTypeExtensionsMap.GetExtension(imageMime);
                    var imageByteArray = Convert.FromBase64String(imageData);
                    var imageGuid = Guid.NewGuid();

                    using (var imageStream = new MemoryStream(imageByteArray))
                    {
                        var imageFileName = string.Format("{0}.shortcut.{1}{2}", Company.CurrentCompanyID, imageGuid, imageExtension);
                        await Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                        shortcut.IconUrl = $"{imageFileName}";

                    }
                }

                var MaxDisplayShortcut = Company.Shortcuts.OrderByDescending(o => o.DisplayOrder).FirstOrDefault();
                shortcut.DisplayOrder = (MaxDisplayShortcut != null) ? MaxDisplayShortcut.DisplayOrder + 1 : 0;

                shortcut.Url += "";
                Company.Add(shortcut);
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

            return jsonSuccess("Shortcut added successfully", shortcut.ID.ToString(), "add", HttpStatusCode.OK);

        }

        [HttpPut, Route("shortcut/edit")]
        public async Task<JsonResult> EditShortcut(Shortcut shortcut)
        {

            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException("You do not have permission to edit shortcuts.", HttpStatusCode.Forbidden);
            }

            var existing = Company.GetById<Shortcut>(shortcut.ID);

            if (existing == null)
            {
                return jsonException($"The shortcut with id {shortcut.ID} could not be found.", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(shortcut.Name))
            {
                return jsonException("This shortcut requires a name", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(shortcut.Icon) && string.IsNullOrEmpty(shortcut.IconUrl) && string.IsNullOrEmpty(shortcut.IconPayload))
            {
                return jsonException("This shortcut is missing an icon", HttpStatusCode.BadRequest);
            }

            try
            {
                if (!string.IsNullOrEmpty(shortcut.IconPayload))
                {
                    //remove old icon
                    if (!string.IsNullOrEmpty(existing.IconUrl))
                    {
                        try
                        {
                            await Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, new Uri(existing.IconUrl).Segments.Last());
                        }
                        catch { }
                    }



                    var imageMatch = MimeTypeExtensionsMap.RegEx.Match(shortcut.IconPayload);

                    var imageMime = imageMatch.Groups["mime"].Value;
                    var imageData = imageMatch.Groups["data"].Value;
                    var imageExtension = MimeTypeExtensionsMap.GetExtension(imageMime);
                    var imageByteArray = Convert.FromBase64String(imageData);
                    var imageGuid = Guid.NewGuid();

                    using (var imageStream = new MemoryStream(imageByteArray))
                    {
                        var imageFileName = string.Format("{0}.shortcut.{1}{2}", Company.CurrentCompanyID, imageGuid, imageExtension);
                        await Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                        shortcut.IconUrl = $"{imageFileName}";

                    }
                }
                else if (!string.IsNullOrEmpty(existing.IconUrl) && string.IsNullOrEmpty(shortcut.IconUrl))
                {
                    try
                    {
                        await Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, new Uri(existing.IconUrl).Segments.Last());
                    }
                    catch { }
                }

                existing.Name = shortcut.Name;
                existing.Icon = shortcut.Icon;
                existing.IconUrl = shortcut.IconUrl;
                existing.Url = shortcut.Url + "";
                existing.Description = shortcut.Description;
                existing.IconColor = shortcut.IconColor;
                existing.TitleColor = shortcut.TitleColor;
                existing.BackgroundColor = shortcut.BackgroundColor;
                existing.LinkTarget = shortcut.LinkTarget;

                Company.Update(existing);

            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

            return jsonSuccess("Shortcut modified successfully", shortcut.ID.ToString(), "edit", HttpStatusCode.OK);
        }

        [HttpDelete, Route("shortcut/delete/{id:int}")]
        public async Task<JsonResult> DeleteShortcut(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException("You do not have permission to edit shortcuts.", HttpStatusCode.Forbidden);
            }

            var existing = Company.GetById<Shortcut>(id);

            if (existing == null)
            {
                return jsonException($"The shortcut with the id {id} could not be found.", HttpStatusCode.BadRequest);
            }

            try
            {
                if (!string.IsNullOrEmpty(existing.IconUrl))
                {
                    try
                    {
                        await Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, new Uri(existing.FullURL).Segments.Last());
                    }
                    catch
                    {
                        //surpress the exception if we cant delete the custom file 
                        // it is most likely already deleted we should not prevent 
                        // removing of the shortcut from govern in this case see GOV-13572
                    }
                }

                Company.Delete(existing);
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

            return jsonSuccess("Shortcut deleted successfully.", id.ToString(), "delete", HttpStatusCode.OK);
        }

        [HttpPut, Route("shortcut/Move")]
        public JsonNetResult MoveShortCut(int id, bool moveUp)
        {
            var success = true;
            var message = "";
            var direction = "";
            try
            {
                var shortcut = Company.GetById<Shortcut>(id);
                if (shortcut == null)
                {
                    throw new Exception($"Shortcut Id ${id} not found");
                }

                direction = moveUp ? "up" : "down";
                Shortcut adjacentShortcut = null;
                if (moveUp)
                {
                    adjacentShortcut = Company.Shortcuts.OrderByDescending(s => s.DisplayOrder).FirstOrDefault(s => shortcut.DisplayOrder > s.DisplayOrder);
                }
                else
                {
                    adjacentShortcut = Company.Shortcuts.OrderBy(s => s.DisplayOrder).FirstOrDefault(s => shortcut.DisplayOrder < s.DisplayOrder);
                }

                if (adjacentShortcut == null)
                    throw new Exception($"Shortcut is already sorted to the " + (moveUp ? "top." : "bottom."));


                int newOrder = adjacentShortcut.DisplayOrder;
                adjacentShortcut.DisplayOrder = shortcut.DisplayOrder;
                shortcut.DisplayOrder = newOrder;


                Company.SaveChanges();
                message = $"Shortcut {shortcut.Name} moved {direction} successfully.";
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }
            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, ValidateContracts(Ignore = true), Route("shortcut/list")]

        public JsonNetResult ListShortcuts()
        {
            return new JsonNetResult
            {
                Data = Company.Shortcuts.OrderBy(s => s.DisplayOrder).ToList(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        #endregion

        #region Tag
        [Route("Tag_AddFields")]
        public JsonResult Tag_AddFields()
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Value", Name = "Tag name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Value", true, "", 1, 100) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        [Route("Tag_EditFields"), NonNullableParameters]
        public JsonResult Tag_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Tag>(id);

            list.Add(new EditableField { FieldName = "uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Value", Name = "Tag name", FieldType = DataType.Text.ToString(), Value = a.Value, Validations = checkAndAddValidation("Text", "Value", true, "", 1, 100) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Synonym

        #region Json

        [HttpGet, Route("SynonymTypes"), NonNullableParameters]
        public JsonNetResult SynonymTypes(string type, int id, int predicateId)
        {
            var items = Company.Query<dynamic>(QueryConstants.SynonymTypes, new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString() }, id, predicateId }).ToList();

            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("SynonymsOptions"), NonNullableParameters]
        public JsonNetResult SynonymsOptions(int predicateId, string type, int typeId, string obj, int objId, string query = "")
        {
            query = query.Replace("_", "[_]").Replace("%", "[%]");

            var items = Company.Query<dynamic>(QueryConstants.SynonymOptions, new { predicateId, type = new Dapper.DbString { IsAnsi = true, Value = type.ToString(), IsFixedLength = true, Length = 50 }, @object = new Dapper.DbString { IsAnsi = true, Value = obj.ToString(), IsFixedLength = true, Length = 50 }, objectId = objId, typeId, query }).ToList();

            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Form Get/Post


        [HttpPost, AjaxValidateAntiForgeryToken, Route("AddNymAllocation")]
        public JsonResult AddNymAllocation(NymAllocationModel model)
        {
            try
            {
                if (!Company.HasAssetPermission(model.Object, model.ObjectID, Permission.AddRelationships) || !Company.HasAssetPermission(model.Object, model.ObjectID, Permission.EditRelationships))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                // delete any existing allocations
                var rels = Company.Filter<NymRelation>(x => x.Object == model.Object.ToString() && x.ObjectID == model.ObjectID).ToList();

                foreach (var rel in rels)
                {
                    Company.Delete(rel);
                }

                if (model.PredicateIDs != null)
                {
                    foreach (var predicateId in model.PredicateIDs)
                    {
                        NymRelation rel = new NymRelation
                        {
                            PredicateID = predicateId,
                            Object = model.Object.ToString(),
                            ObjectID = model.ObjectID,
                            UpdatedBy = Company.CurrentResourceID,
                            UpdatedOn = DateTime.UtcNow
                        };

                        Company.Add<NymRelation>(rel);
                    }
                }

                return jsonSuccess("Grammar allocation successfully modified.", "", "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("AddCustomSynonym")]
        public JsonResult AddCustomSynonym(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("custom synonym");

                var name = parseTextField(form, "Name");
                var predicateId = parseIntField(form, "PredicateID");
                var objectType = parseTextField(form, "Object");
                var objectId = parseIntField(form, "ObjectID");

                Nym model = new Nym
                {
                    Name = name,
                    PredicateID = predicateId,
                    Object = objectType,
                    ObjectID = objectId,
                    CreatedBy = Company.CurrentResourceID,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                if (!Company.HasAssetPermission(model.Object, model.ObjectID, Permission.AddAsset) || !Company.HasAssetPermission(model.Object, model.ObjectID, Permission.EditAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Add(model);

                return jsonSuccess("Synonym " + model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete, Route("DeleteCustomSynonym")]
        public JsonResult DeleteCustomSynonym(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("synonym");
                var id = parseIntField(form, "ID");

                var detail = Company.GetById<Nym>(id);

                if (detail == null)
                    throw new NullReferenceException("Custom Synonym not found");

                if (!Company.HasAssetPermission(detail.Object, detail.ObjectID, Permission.DeleteRelationships))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (detail != null)
                {
                    Company.Delete(detail);
                }

                return jsonSuccess("Synonym successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        #endregion

        #endregion

        #region Export Templates

        private JsonResult ExportTemplate_EditFields(int id)
        {
            var template = Company.AssetTypeExportTemplates.Where(x => x.ID == id).FirstOrDefault();

            string templateFieldTypesSQL = $@"select FT.Name from AssetTypeExportTemplateField ATETF inner join FieldType FT on ATETF.FieldTypeId = FT.ID where ATETF.TemplateId = @templateId order by [Order] asc";

            template.IncludeFieldTypes = Company.Database.Connection.Query<string>(templateFieldTypesSQL, new { templateId = template.ID }).ToArray();

            var list = new List<EditableField>();
            var assetPaths = Company.GetAssetTypePathsByAssetClasses(
                new List<int>()
                {
                    { (int)AssetTypeClass.BusinessAsset },
                    { (int)AssetTypeClass.TechnicalAsset },
                    { (int) AssetTypeClass.Rule }
                });            
            list.Add(new EditableField { FieldName = "ID", Name = "ID", FieldType = DataType.Hidden.ToString(), Value = template.ID.ToString() });
            list.Add(new EditableField { FieldName = "Uid", Name = "Uid", FieldType = DataType.Hidden.ToString(), Value = template.Uid.ToString() });
            list.Add(new EditableField { FieldName = "IncludeFieldTypes", Name = "IncludeFieldTypes", FieldType = DataType.Hidden.ToString(), Value = template.IncludeFieldTypes == null ? template.IncludeFieldTypes.ToString() : null });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Namespace", true, "", 1, 250), Value = template.Name });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = template.Description });
            var names = Enum.GetNames(typeof(ExportView)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();            

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "ExportViewType", Name = "List Arrangement", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = names, Value = template.ExportViewType.ToString() });

            var types = Company.AssetTypes.Where(f => f.Class == AssetTypeClass.BusinessAsset || f.Class == AssetTypeClass.TechnicalAsset || f.Class == AssetTypeClass.Rule).ToArray()
                .Select(i => new SelectListItem
                {
                    Text = $"{i.Class.GetDisplayName()} : {assetPaths[i.uid]}",
                    Value = i.uid.ToString()
                }).OrderBy(x => x.Text).ToList();

            if (template.AssetTypeUID == Guid.Empty)
            {
                template.AssetTypeUID = Company.AssetTypes.Where(t => t.ID == template.AssetTypeID).Select(i => i.uid).FirstOrDefault();
            }

            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "AssetTypeUID", Name = "Asset Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = types, Value = template.AssetTypeUID.ToString() });

            list.Add(new EditableField { Row = 5, Column = 1, Required = true, FieldName = "IncludeUrl", Name = "Include Asset Url", FieldDescription = "", FieldType = DataType.Boolean.ToString(), Value = template.IncludeUrl.ToString() });

            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "IncludeParent", Name = "Include Parent Name", FieldDescription = "", FieldType = DataType.Boolean.ToString(), Value = template.IncludeParent.ToString() });

            list.Add(new EditableField { Row = 7, Column = 1, Required = false, FieldName = "UsageNotes", Name = "Usage Notes", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = template.UsageNotes });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        private JsonResult ExportTemplate_AddFields()
        {
            var list = new List<EditableField>();
            var assetPaths = Company.GetAssetTypePathsByAssetClasses(
                new List<int>()
                {
                    { (int)AssetTypeClass.BusinessAsset },
                    { (int)AssetTypeClass.TechnicalAsset },
                    { (int) AssetTypeClass.Rule }
                });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Namespace", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldDescription = "", FieldType = DataType.Text.ToString() });

            var names = Enum.GetNames(typeof(ExportView)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "ExportViewType", Name = "List Arrangement", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = names });

            var types = Company.AssetTypes.Where(f => f.Class == AssetTypeClass.BusinessAsset || f.Class == AssetTypeClass.TechnicalAsset || f.Class == AssetTypeClass.Rule).ToArray()
                .Select(i => new SelectListItem
                {
                    Text = $"{i.Class.GetDisplayName()} : {assetPaths[i.uid]}",
                    Value = i.uid.ToString()
                }).OrderBy(x => x.Text).ToList();
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "AssetTypeUID", Name = "Asset Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = types });

            list.Add(new EditableField { Row = 5, Column = 1, Required = true, FieldName = "IncludeUrl", Name = "Include Asset Url", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "IncludeParent", Name = "Include Parent Name", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 7, Column = 1, Required = false, FieldName = "UsageNotes", Name = "Usage Notes", FieldDescription = "", FieldType = DataType.Text.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}