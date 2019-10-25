using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions;
using d360.extensions.powerbi;
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
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Configuration;
using d360.core.helpers;
using System.Text;
using d360.core.resources;

namespace d360.web.Controllers
{    
    [RoutePrefix("form"), Authorize, AiHandleError, NonNullableParameters]
    public partial class FormController : BaseController
    {
        #region DI

        IStorageProvider Storage;

        public FormController(ICommunityContext community, ICompanyContext company, ISecurityContextProvider secProvider, IStorageProvider storage)
            : base(community, company)
        {            
            Storage = storage;
#if DEBUG
            company.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
#endif

        }

        #endregion

        #region Field Loading For Type Forms Below

        void loadIconFields(List<EditableField> list, int row, ObjectStyle style = null)
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

        void upsertObjectStyle(string type, int id, string foreColor, string backColor, string objectName = "Tx")
        {
            var style = Company.GetObjectStyle(type, id);
            bool add = (style == null);

            if (add)
            {
                style = new ObjectStyle
                {
                    ObjectType = type,
                    ObjectID = id,
                    IconBackColor = backColor,
                    IconForeColor = foreColor,
                    IconText = getIconText(objectName)
                };
                Company.Add<ObjectStyle>(style);
            }
            else
            {
                style.IconBackColor = backColor;
                style.IconForeColor = foreColor;
                style.IconText = getIconText(objectName);
                Company.Update<ObjectStyle>(style);
            }
        }

        /// <summary>
        /// Generates the icon text shown on icons that represent the Asset 
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private string getIconText(string assetName)
        {
            string iconText = "Tx";
            if (string.IsNullOrEmpty(assetName))
            {
                return iconText;
            }

            var name = assetName.Trim();
            
            var words = name.Split(' ');
            if (words.Length > 1 && words[1].Length > 0)
            {
                if (!string.IsNullOrEmpty(words[0]))
                {
                    iconText = words[0][0].ToString().ToUpper();
                }
                else
                {
                    iconText = "_"; // first character is space.
                }
                
                if (!string.IsNullOrEmpty(words[1]))
                {

                    iconText += words[1][0].ToString().ToLower();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    iconText = name[0].ToString().ToUpper();
                    if (name.Length > 1)
                    {
                        iconText += name[1].ToString().ToLower();
                    }
                }
            }

            return iconText;

        }

        void upsertObjectStyle(SystemObjects type, int id, string foreColor, string backColor, string objectName = "Tx")
        {
            upsertObjectStyle(type.ToString(), id, foreColor, backColor, objectName);
        }

        void upsertObjectStyle(SystemObjects type, int id, FormCollection form, string objectName = "Tx")
        {
            var style = Company.GetObjectStyle(type, id);
            bool add = (style == null);

            string iconText = "Tx";

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
                style = new ObjectStyle
                {
                    ObjectType = type.ToString(),
                    ObjectID = id,
                    IconBackColor = form["IconBackColor"],
                    IconForeColor = form["IconForeColor"],
                    IconText = iconText
                };
                Company.Add<ObjectStyle>(style);
            }
            else
            {
                style.IconBackColor = form["IconBackColor"];
                style.IconForeColor = form["IconForeColor"];
                style.IconText = iconText;
                Company.Update<ObjectStyle>(style);
            }
        }

        private string ConverDate(string date)
        {
            var stringDate = date;
            DateTime dateVal = DateTime.MinValue;
            if (DateTime.TryParse(stringDate, out dateVal))
            {
                return  dateVal.ToShortDateString();
            }
            return null;
        }
        #endregion

        #region Parse Methods

        bool parseBooleanField(FormCollection form, string fieldName, bool defaultValue = false)
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
                return false;// defaultValue;
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
                return defaultValue;
        }

        int parseIntField(FormCollection form, string fieldName)
        {
            return form.AllKeys.Any(i => i == fieldName) ? int.Parse(form[fieldName]) : 0;
        }

        string parseTextField(FormCollection form, string fieldName, string defaultValue = null, bool htmlEncode = false)
        {
            return form.AllKeys.Any(i => i == fieldName) ? ((htmlEncode) ? Server.HtmlEncode(form[fieldName]) : form[fieldName]) : defaultValue;
        }

        string parseNameField(FormCollection form, string fieldName, string defaultValue = null)
        {
            var value = form.AllKeys.Any(i => i == fieldName) ? (form[fieldName]) : defaultValue;

            // only allow alpha numeric, whitespace, and apostrophes in firstname / last name.
            if (!isValidUserProfileName(value))
                throw new Exception("Error invalid characters contained in the provided name field.");

            return value;
        }

        #endregion

        #region Dynamic Editor Field Type Information For Angular2

        [HttpPost, AjaxValidateAntiForgeryToken, Route("dynamiceditor/new/{objectType}")]
        public JsonResult DynamicEditorAddFields(string objectType, object[] param)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "ATTRIBUTE":
                    return Attribute_AddFields((int)param[0], param[1].ToString(), (int)param[2], (int)param[3]);

            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("dynamiceditor/edit/{objectType}")]
        public JsonResult DynamicEditorEditFields(string objectType, object[] param)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "ATTRIBUTEALLOCATION":
                    return AttributeTypeRelation_EditFields((int)param[0], param[1].ToString(), (int)param[2]);                
                default: break;
            }
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
                default:
                    foreach (SystemObjects sysobj in (SystemObjects[])Enum.GetValues(typeof(SystemObjects)))
                    {
                        if (sysobj.ToString().ToUpper() == o.ToUpper())
                            objectId = Company.GetObjectId(uid.Value, sysobj);
                    }
                    return DynamicEditorEditFields(o, objectId);
            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditor/edit/{o}/{oid:int}")]
        public JsonResult DynamicEditorEditFields(string o, int oid)
        {
            switch ((o ?? "").ToUpper())
            {
                case "APIFIELD":
                    return CustomAPIVersionField_EditFields(oid);
                case "ARTIFACT":
                    return Artifact_EditFields(oid);
                case "ATTRIBUTE":
                    return Attribute_EditFields(oid);
                case "CONTRACT":
                    return Contract_EditFields(oid);
                case "ENDPOINT":
                    return CustomAPIServiceEndpoint_EditFields(oid);
                case "EXPORTTEMPLATE":
                    return ExportTemplate_EditFields(oid);
                case "FUSION":
                    return Fusion_EditFields(oid);
                case "FUSIONATTRIBUTE":
                    return FusionAttribute_EditFields(oid);
                case "INTERSECTTYPE":
                    return Relationship_EditFields(oid);
                case "ISSUETYPE":
                    return IssueType_EditFields(oid);
                case "LOOKUPTYPE":
                    return Lookup_EditFields(oid);
                case "MAP":
                    return Map_EditFields(oid);                
                case "NAMESPACE":
                    return CustomAPINamespace_EditFields(oid);
                case "ORGANIZATION":
                    return Organization_EditFields(oid);
                case "ORGANIZATIONDOMAIN":
                    return OrganizationDomain_EditFields(oid);
                case "ORGANIZATIONINVITATION":
                    return OrganizationInvitation_EditFields(oid);
                case "POLICY":
                    return Policy_EditFields(oid);
                case "PREDICATE":
                    return Predicate_EditFields(oid);
                case "REFERENCEITEMTYPE":
                    return ReferenceItem_EditFields(oid);
                case "RESOURCESELF":
                    return Resource_EditMyInfoFields();
                case "RESOURCESELFPASSWORD":
                    return Resource_ChangeMyPasswordFields();
                case "RESOURCETYPE":
                    return Resource_EditFields(oid);
                case "RULE":
                    return Rule_EditFields(oid);
                case "RULEIMPLEMENTATION":
                    return RuleImplementation_EditFields(oid);
                case "RULETYPE":
                    return RuleType_EditFields(oid);
                case "SERVICE":
                    return CustomAPIService_EditFields(oid);
                case "SURVEYTYPE":
                    return SurveyType_EditFields(oid);
                case "TAG":
                    return Tag_EditFields(oid);
                case "TAXONOMY":
                    return Taxonomy_EditFields(oid);
                case "VERSION":
                    return CustomAPIServiceEndpointVersion_EditFields(oid);
                case "URI":
                    return CustomAPIVersionUri_EditFields(oid);                
            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditor/new/{objectType}/{objectID?}/{parentID?}/{typeID?}")]
        public JsonResult DynamicEditorAddFields(string objectType, int? objectID, int? parentID, int? typeID)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "APIFIELD":
                    return CustomAPIVersionField_AddFields(parentID.GetValueOrDefault());
                case "ARTIFACT":
                    return Artifact_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "ATTRIBUTEALLOCATION":
                    return AttributeTypeRelation_AddFields(parentID.GetValueOrDefault());
                case "CONTRACT":
                    return Contract_AddFields(objectID.HasValue ? objectID.Value : 0);
                case "ENDPOINT":
                    return CustomAPIServiceEndpoint_AddFields(parentID.GetValueOrDefault());
                case "EXPORTTEMPLATE":
                    return ExportTemplate_AddFields();
                case "FUSION":
                    return Fusion_AddFields(objectID.GetValueOrDefault());
                case "FUSIONATTRIBUTE":
                    return FusionAttribute_AddFields(objectID.GetValueOrDefault(), typeID.GetValueOrDefault());
                case "ISSUE":
                    return Issue_AddFields(objectID.GetValueOrDefault());
                case "ISSUETYPE":
                    return IssueType_AddFields();
                case "ISSUETYPERELATION":
                    return IssueTypeRelation_AddFields(objectID.GetValueOrDefault());
                case "LOOKUPTYPE":
                    return Lookup_AddFields(objectID.GetValueOrDefault());
                case "MAP":
                    return Map_AddFields();                
                case "NAMESPACE":
                    return CustomAPINamespace_AddFields(parentID.GetValueOrDefault());
                case "ORGANIZATION":
                    return Organization_AddFields(objectID.GetValueOrDefault());
                case "ORGANIZATIONDOMAIN":
                    return OrganizationDomain_AddFields(objectID.Value);
                case "ORGANIZATIONINVITATION":
                    return OrganizationInvitation_AddFields(objectID.Value);
                case "POLICY":
                    return Policy_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "PREDICATE":
                    return Predicate_AddFields();
                case "REFERENCEITEMTYPE":
                    return ReferenceItem_AddFields(objectID.GetValueOrDefault());
                case "RESOURCETYPE":
                    return Resource_AddFields(objectID.GetValueOrDefault());
                case "RULE":
                    return Rule_AddFields(objectID.GetValueOrDefault());
                case "RULEIMPLEMENTATION":
                    return RuleImplementation_AddFields(objectID.GetValueOrDefault());
                case "RULETYPE":
                    return RuleType_AddFields();
                case "SERVICE":
                    return CustomAPIService_AddFields();
                case "SURVEYTYPE":
                    return SurveyType_AddFields();
                case "TAG":
                    return Tag_AddFields();
                case "TAXONOMY":
                    return Taxonomy_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "VERSION":
                    return CustomAPIServiceEndpointVersion_AddFields(parentID.GetValueOrDefault());
                case "URI":
                    return CustomAPIVersionUri_AddFields(parentID.GetValueOrDefault());

            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditorrel/new/{objectType}/{objectUID}/{targetType}/{targetID:int}")]
        public JsonResult DynamicEditorAddRelationFields(string objectType, string objectUID, SystemObjects targetType, int targetID)
        {
            Guid guid = Guid.Parse(objectUID);
            int objectId = Company.GetObjectId(guid, SystemObjects.IntersectType);
            return DynamicEditorAddRelationFields(objectType, objectId, targetType, targetID);
        }

        [HttpGet, Route("dynamiceditorrel/new/{objectType}/{objectID:int}/{targetType}/{targetID:int}")]
        public JsonResult DynamicEditorAddRelationFields(string objectType, int objectID, SystemObjects targetType, int targetID)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "INTERSECTTYPE":
                    return Relationship_AddFields(objectID, targetType, targetID);                
            }
            throw new Exception("Invalid or non implemented editor type");
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

            switch ((objectType ?? "" ).ToUpper())
            {
                case "APIFIELD":
                    return EditApiField(form);
                case "ATTRIBUTE":
                    return EditAttribute(form);
                case "ATTRIBUTETYPE":
                    return EditAttributeType(form);
                case "ENDPOINT":
                    return EditServiceEndpoint(form);
                case "FUSION":
                    return EditFusion(form);
                case "FUSIONATTRIBUTE":
                    return EditFusionAttribute(form);
                case "FUSIONATTRIBUTETYPECUSTOMQUERY":
                    return EditFusionAttributeTypeCustomQuery(form);
                case "FUSIONQUERYATTRIBUTE":
                    return EditFusionQueryAttribute(form);
                case "FUSIONSCHEDULE":
                    return EditFusionSchedule(form);
                case "INTERSECT":
                    return EditRelationship(form);
                case "INTERSECTTYPE":
                    return EditIntersectType(form);
                case "ISSUETYPE":
                    return EditIssueType(form);
                case "LOOKUP":
                    return EditLookup(form);
                case "MAP":
                    return EditMap(form);                
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
                case "REPORTTILE":
                    return EditReportTile(form, true);
                case "RESOURCE":
                    return EditResource(form);
                case "RESOURCESELF":
                    return EditMyInfo(form);
                case "RESOURCESELFPASSWORD":
                    return ChangeMyPassword(form);
                case "RULEIMPLEMENTATION":
                    return EditRuleImplementation(form);
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
            }

            throw new Exception("Invalid / unsupported edit type");
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
                case "ARTIFACTTYPE":
                    return DeleteArtifactType(objectID);
                case "ATTRIBUTETYPE":
                    return DeleteAttributeType(form);
                case "CONTRACT":
                    return DeleteContract(objectID);
                case "CUSTOMSYNONYM":
                    return DeleteCustomSynonym(form);
                case "ENDPOINT":
                    return DeleteCustomAPIEndPoint(form);
                case "FUSIONCONFIGURATION":
                    return DeleteFusion(form);
                case "FUSIONQUERYATTRIBUTE":
                    return DeleteFusionQueryAttribute(form);
                case "FUSIONATTRIBUTETYPECUSTOMQUERY":
                    return DeleteFusionAttributeTypeCustomQuery(form);
                case "FUSIONSCHEDULE":
                    return DeleteFusionSchedule(form);
                case "INTERSECTTYPE":
                    return DeleteIntersectType(form);
                case "ISSUETYPE":
                    return DeleteIssueType(form);
                case "LINEAGEMAPPING":
                    return DeleteLineageMapping(form);
                case "LOOKUP":
                    return DeleteLookup(form);
                case "LOOKUPTYPE":
                    return DeleteLookupType(form);
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
                case "REPORTTILE":
                    return DeleteReportTile(form);                
                case "RULETYPE":
                    return DeleteRuleType(form);                
                case "POLICYTYPELEVEL":
                    return DeletePolicyTypeLevel(form);
                case "RULEIMPLEMENTATION":
                    return DeleteRuleImplementation(form);
                case "SERVICE":
                    return DeleteCustomAPIService(form);
                case "SURVEYTYPE":
                    return DeleteSurveyType(form);
                case "SURVEYQUESTIONTYPE":
                    return DeleteQuestionType(form);
                case "SYNONYM":
                    return DeleteSynonym(form);                
                case "TAXONOMYTYPE":
                    return DeleteTaxonomyType(form);
                case "TAXONOMYTYPELEVEL":
                    return DeleteTaxonomyTypeLevel(form);
                case "URI":
                    return DeleteCustomAPIUri(form);
                case "VERSION":
                    return DeleteCustomAPIVersion(form);
            }

            throw new Exception("Invalid / unsupported edit type");
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
                case "ATTRIBUTE":
                    return AddAttribute(form);
                case "ATTRIBUTETYPE":
                    return AddAttributeType(form);
                case "CUSTOMSYNONYM":
                    return AddCustomSynonym(form);
                case "ENDPOINT":
                    return AddServiceEndpoint(form);
                case "FUSION":
                    return AddFusion(form);
                case "FUSIONQUERYATTRIBUTE":
                    return AddFusionQueryAttribute(form);
                case "FUSIONATTRIBUTETYPECUSTOMQUERY":
                    return AddFusionAttributeTypeCustomQuery(form);
                case "FUSIONSCHEDULE":
                    return AddFusionSchedule(form);
                case "INTERSECT":
                    return AddRelationship(form);
                case "INTERSECTTYPE":
                    return AddIntersectType(form);
                case "ISSUE":
                    return AddIssue(form);
                case "ISSUETYPE":
                    return AddIssueType(form);
                case "ISSUETYPERELATION":
                    return AddIssueTypeRelation(form);
                case "LOOKUP":
                    return AddLookup(form);
                case "MAP":
                    return AddMap(form);                
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
                case "REPORTTILE":
                    return AddReportTile(form, true);
                case "RESOURCE":
                    return AddResource(form);
                case "RULEIMPLEMENTATION":
                    return AddRuleImplementation(form);
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
            }

            throw new Exception("Invalid / unsupported create type");
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("dynamicedit/copy/{objectType}"), ValidateInput(false)]
        public JsonResult DynamicCopy(string objectType, string json)
        {
            JObject jsonObject = JObject.Parse(json);
             FormCollection form = new FormCollection();

            foreach (var item in jsonObject)
            {
                form.Add(item.Key, item.Value.ToString());
            }

            switch ((objectType ?? "").ToUpper())
            {
                case "RULEIMPLEMENTATION":
                    return CopyRuleImplementation(form);
            }
            throw new Exception("Invalid / unsupported copy type");
        }


        [HttpGet, Route("dynamiceditor/copy/{o}/{oid:int}")]
        public JsonResult DynamicEditorCopyFields(string o, int oid)
        {
            switch ((o ?? "").ToUpper())
            {
                case "RULEIMPLEMENTATION":
                    return RuleImplementation_CopyFields(oid);
            }
            throw new Exception("Invalid or non implemented editor type");
        }

        #endregion

        #region Artifact

        #region Field Generation

        /// <param name="at">ArtifactTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("Artifact_AddFields"), NonNullableParameters]
        public JsonResult Artifact_AddFields(int at, int p)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, at, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
                        
            var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                i.Object == "ArtifactType" &&
                i.ObjectID == at &&
                i.PredicateType.Value == PredicateType.InterTypeHierarchy
            ).SingleOrDefault();

            
            if (intersectType != null)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                var parents = Company.Query<SelectListItem>($"select convert(nvarchar(36), A.uid) as Value, AD.DisplayValue as Text from Asset a inner join AssetDisplayValue AD on AD.AssetID = A.ID inner join AssetType AT on A.AssetTypeID = AT.ID where AT.[Object] = 'ArtifactType' and AT.[ObjectID] = {intersectType.SubjectID}").OrderBy(i => i.Text).ToList();
                list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentUid", Name = $"Parent {pluralize.Singularize(intersectType.SubjectName)}", FieldType = DataType.Lookup.ToString(), Value = ((p > 0) ? p.ToString() : null), Items = parents });
            }

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ArtifactType, at).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <param name="id">ArtifactID</param>
        [Route("Artifact_EditFields"), NonNullableParameters]
        public JsonResult Artifact_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Artifact, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();                        
            var a = Company.Assets.Where(x => x.ObjectID == id && x.Object == SystemObjects.Artifact.ToString()).Include(x => x.AssetType).FirstOrDefault();

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { FieldName = "AssetTypeUid", FieldType = DataType.Hidden.ToString(), Value = a.AssetType.uid.ToString() });

            var parentType = Company.GetParentType(a.AssetType.ObjectID, SystemObjects.ArtifactType);
            

            if (PluralCultureHelper.IsNeutralCultureEnglish())
            {
                if (parentType != null)
                {
                    var parent = Company.GetParentObject(a.ObjectID, SystemObjects.Artifact);
                   
                    var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                    var parents = Company.Query<SelectListItem>($"select lower(convert(nvarchar(36), A.uid)) as Value, AD.DisplayValue as Text from Asset A inner join AssetType AT on A.AssetTypeID = AT.ID inner join AssetDisplayValue AD on A.ID = AD.AssetID   where AT.[Object] = 'ArtifactType' and AT.[ObjectID] = {parentType.ObjectID}").OrderBy(i => i.Text).ToList();
                    list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentUID", Name = $"Parent {pluralize.Singularize(parentType.Name)}", FieldType = DataType.Lookup.ToString(), Value = ((parent != null) ? (parent.uid.ToString()??"").ToLower() : ""), Items = parents });
                }
            }

            list = (
                loadDynamicFields(
                    SystemObjects.Artifact.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.ArtifactType, a.AssetType.ObjectID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Artifact, id).ToList(), 
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }
                        
        [HttpGet, Route("Artifact_SimilarItems"), NonNullableParameters]
        public JsonNetResult Artifact_SimilarItems(int typeID, string query)
        {
            //escape wildcards
            query = query.Replace("_", "[_]");
            query = query.Replace("%", "[%]");
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.SimilarItems, new { type = new DbString { Value = "Artifact", IsAnsi = true, IsFixedLength = true, Length = 50 }, typeID, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        #endregion

        #region Form Get/Post

        [AjaxValidateAntiForgeryToken, HttpPost, Route("RequestCertification")]
        public JsonResult RequestCertification(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int id = parseIntField(form, "ID");
                var asset = Company.Assets.Where(x => x.ObjectID == id && x.Object == SystemObjects.Artifact.ToString()).Include(x => x.AssetType).FirstOrDefault();
                
                if (asset == null) throw new NotFoundException("artifact");
                
                Company.RequestObjectCertification(SystemObjects.Artifact, asset.ObjectID, SystemObjects.ArtifactType, asset.AssetType.ObjectID);

                return jsonSuccess("Request successfully created.", "", "add", HttpStatusCode.Created);
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

        #region ArtifactType

    
        [HttpDelete, ActionName("ArtifactType"), Route("ArtifactType"), NonNullableParameters]
        public JsonResult DeleteArtifactType(int id)
        {
            try
            {
                var assetType = Company.AssetTypes.FirstOrDefault(a => a.Object == "ArtifactType" && a.ObjectID == id);
                if (assetType == null) throw new NotFoundException("artifact type");

                if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var intersectType = Company.Filter<IntersectType>(i =>
                    i.Object == "ArtifactType" &&
                    i.ObjectID == assetType.ObjectID &&
                    i.Predicate.Type == PredicateType.InterTypeHierarchy
                ).SingleOrDefault();

                if (intersectType != null)
                {
                    Company.Delete(SystemObjects.IntersectType, intersectType.ID);
                }

                Company.Delete(SystemObjects.ArtifactType, id);

                dynamic custom = new
                {                    
                    Name = assetType.Name,
                    action = "delete"              
                };

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region AssetType

        #region Form Get/Post

        [HttpGet, ActionName("AssetType"), Route("AssetType")]
        public JsonNetResult GetAssetType(AssetTypeClass @class, int? id = null, int? parentID = null)
        {
            try
            {
                var model = new AssetTypeEditorModel();

                Guid? parentUid = null;
                if (parentID.HasValue && parentID > 0)
                {
                    var parentAssetType = Company.Query<AssetType>("select * from AssetType where class = @class and ObjectID = @parentID", new { @class, parentID }).FirstOrDefault();
                    if (parentAssetType != null)
                        parentUid = parentAssetType.uid;
                }
                  
                var loadPredicates = false;
                var parentPredicateType = PredicateType.InterTypeHierarchy;
                var loadParentReferenceItemOptions = false;

                var ot = SystemObjects.ArtifactType;
                var appendTitle = "";
                switch (@class)
                {
                    case AssetTypeClass.FusionAttribute:
                        ot = SystemObjects.FusionAttributeType;
                        appendTitle = FormInfo.FusionAttributeType;
                        break;
                    case AssetTypeClass.BusinessAsset:
                    case AssetTypeClass.TechnicalAsset:
                        ot = SystemObjects.ArtifactType;
                        appendTitle = FormInfo.ArtifactType;
                        break;
                    case AssetTypeClass.Model:
                        ot = SystemObjects.TaxonomyType;
                        appendTitle = FormInfo.TaxonomyType;
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                        break;
                    case AssetTypeClass.Organization:
                        ot = SystemObjects.OrganizationType;
                        appendTitle = FormInfo.OrganizationType;
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                        break;
                    case AssetTypeClass.Policy:
                        ot = SystemObjects.PolicyType;
                        appendTitle = FormInfo.PolicyType;
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                        break;
                    case AssetTypeClass.Reference:
                    case AssetTypeClass.ReferenceItemType:
                        ot = SystemObjects.ReferenceItemType;
                        appendTitle = "Reference List";                        
                        loadParentReferenceItemOptions = true;
                        break;
                }

                if (id.HasValue)
                {
                    if (!id.HasValue)
                        return jsonNetException($"No asset type ID provided (id parameter).", HttpStatusCode.BadRequest);

                    var assetType = Company.GetById<AssetType>(id.Value);

                    if (assetType == null)
                        return jsonNetException($"No asset type found for the ID {id.Value}", HttpStatusCode.NotFound);

                    var style = Company.Filter<ObjectStyle>(i => i.ObjectType == assetType.Object && i.ObjectID == assetType.ObjectID).FirstOrDefault();

                    model = new AssetTypeEditorModel()
                    {
                        AssetType = new AssetTypeInsert()
                        {
                            Uid = assetType.uid,
                            ParentUid = parentUid,
                            AutoDisplayDescription = assetType.AutoDisplayDescription,
                            Class = @class,
                            UseAsTransformation = assetType.UseAsTransformation,
                            Notes = assetType.Notes,
                            IconStyle = new IconStyleInsert()
                            {
                                ForeColor = ((style != null) ? style.IconForeColor : "#FFF"),
                                BackColor = ((style != null) ? style.IconBackColor : "#000")
                            },
                            Hierarchy = new HierarchyInsert()
                            {
                                MaximumDepth = 1,
                                PredicateUid = null
                            }

                        },
                        Tokens = Company.Filter<FieldType>(i => i.Object == assetType.Object && i.ObjectID == assetType.ObjectID && !this.limitedFieldTypes.Contains(i.Type)).OrderBy(i => i.FriendlyName).Select(i => new PrimeSelectItem { label = i.FriendlyName, value = "{" + i.Name + "}" }).ToList()
                    };
                    
                    switch (@class)
                    {
                        case AssetTypeClass.FusionAttribute:
                            var f = Company.GetById<FusionAttributeType>(model.AssetType.ObjectID);
                            model.AssetType.Name = f.Name;
                            break;
                        case AssetTypeClass.BusinessAsset:
                        case AssetTypeClass.TechnicalAsset:
                            model.AssetType.CanOwnFusion = (@class == AssetTypeClass.BusinessAsset) ? assetType.CanOwnFusion : false;
                            model.AssetType.AutoDisplayDescription = assetType.AutoDisplayDescription;
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            break;
                        case AssetTypeClass.Model:                            
                            model.AssetType.Hierarchy.MaximumDepth = assetType.HierarchyMaximumDepth;
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            break;
                        case AssetTypeClass.Organization:
                            var o = Company.GetById<OrganizationType>(model.AssetType.ObjectID);
                            model.AssetType.Hierarchy.MaximumDepth = 1;
                            model.AssetType.Name = o.Name;
                            model.AssetType.Description = o.Description;
                            model.AssetType.DisplayFormat = o.DisplayFormat;
                            break;
                        case AssetTypeClass.Policy:
                            model.AssetType.Hierarchy.MaximumDepth = assetType.HierarchyMaximumDepth;
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            break;
                        case AssetTypeClass.Reference:
                        case AssetTypeClass.ReferenceItemType:
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Notes = assetType.Notes;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            if (model.Tokens != null) model.Tokens.Add(new PrimeSelectItem { label = "Code", value = "{Code}" });
                            break;
                    }
                    model.AssetType.Object = ot.ToString();
                    model.FormName = string.Format(FormInfo.Add_Asset_Type_Title, appendTitle);
                    model.FormDescription = string.Format(FormInfo.Add_Asset_Type_Directions, appendTitle.ToLower());

                    if (@class == AssetTypeClass.FusionAttribute || @class == AssetTypeClass.BusinessAsset || @class == AssetTypeClass.TechnicalAsset || @class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.Reference)
                    {
                        var intersectType = Company.Filter<IntersectType>(i =>
                            i.Object == assetType.Object &&
                            i.ObjectID == assetType.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).FirstOrDefault();


                        if (@class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.Reference) //If model or policy you must always have a predicate to load.
                            loadPredicates = true;

                        if (intersectType != null)
                        {
                            loadPredicates = true;

                            if (intersectType.SubjectUid.HasValue)
                            {
                                model.AssetType.ParentUid = intersectType.SubjectUid;
                            }
                            else
                            {
                                var parentAssetType = Company.AssetTypes.FirstOrDefault(x => x.Object == intersectType.Subject && x.ObjectID == intersectType.SubjectID);
                                model.AssetType.ParentUid = parentAssetType.uid;
                            }


                            model.AssetType.Hierarchy.PredicateUid = intersectType.Predicate.UID;
                        }
                    }
                }
                else
                {
                    loadPredicates = true;

                    model = new AssetTypeEditorModel()
                    {

                        AssetType = new AssetTypeInsert()
                        {
                            DisplayFormat = "{Name}",
                            Class = @class,
                            Object = ot.ToString(),
                            ParentUid = parentUid,
                            IconStyle = new IconStyleInsert()
                            {
                                BackColor = "#000",
                                ForeColor = "#FFF"
                            },
                            Hierarchy = new HierarchyInsert()
                            {
                                PredicateUid = null,
                                MaximumDepth = 1
                            }

                        },
                        Tokens = new List<PrimeSelectItem>() { new PrimeSelectItem { label = "Name", value = "{Name}" } }
                    };



                    if (@class == AssetTypeClass.Reference)
                    {
                        model.AssetType.DisplayFormat = "{Code}";
                        model.Tokens.Clear(); // remove the name token for reference item type it isnt created by default.
                        model.Tokens.Add(new PrimeSelectItem { label = "Code", value = "{Code}" });
                    }
                    model.FormName = string.Format(FormInfo.Edit_Asset_Type_Title, appendTitle);
                    model.FormDescription = string.Format(FormInfo.Add_Asset_Type_Directions, appendTitle.ToLower());
                }

                if (loadPredicates)
                {
                    model.Predicates = Company.Filter<Predicate>(i => i.Type == parentPredicateType).Select(i => new PrimeSelectItem { label = i.Inverse, value = i.UID.ToString() }).ToList();
                }

                if (loadParentReferenceItemOptions)
                {
                    if (model.AssetType != null && model.AssetType.ObjectID > 0)
                    {
                        var parents = Company.Query<PrimeSelectItem>(@"select a.ObjectUid as value, a.Name as label from  assettype a where a.[object] = 'ReferenceItemType'  and a.objectid != @id
                                                                    and  not exists(
                                                                    select  1 from IntersectType i where i.object = 'ReferenceItemType' and i.SubjectId = @id and i.objectid = a.objectid)
                                                                    order by Name", new { id = model.AssetType.ObjectID }).ToList();
                        model.Parents = parents;
                    }
                    else
                    {
                        var parents = Company.Query<PrimeSelectItem>("select LOWER(CAST(uid AS char(36))) as value, Name as label from assettype where [object] = 'ReferenceItemType' order by Name").ToList();
                        model.Parents = parents;
                    }
                    model.Parents?.Insert(0, new PrimeSelectItem() { label = "", value = "" });
                }

                return new JsonNetResult
                {
                    Data = model,
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [HttpDelete, ActionName("AssetType"), Route("AssetType"), NonNullableParameters]
        public JsonResult DeleteAssetType(int id)
        {
            try
            {
                var at = Company.GetById<AssetType>(id);
                if (at == null) throw new NotFoundException("asset type");

                SystemObjects ot;

                if (!Enum.TryParse<SystemObjects>(at.Object, out ot))
                    throw new GenericException(HttpStatusCode.BadRequest, "Missing Object Type", "No valid type provided. Please check your request and try again.");

                if (!Company.CurrentResourceIsAdmin)
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Delete, FormInfo.Permisions_Error_Delete);

                var parentPredicateType = PredicateType.InterTypeHierarchy;

                if (at.Class == AssetTypeClass.Model || at.Class == AssetTypeClass.Policy)
                {
                    parentPredicateType = PredicateType.IntraTypeHierarchy;
                }

                var intersectType = Company.Filter<IntersectType>(i =>
                    i.Object == at.Object &&
                    i.ObjectID == at.ObjectID &&
                    i.Predicate.Type == parentPredicateType
                ).SingleOrDefault();

                if (intersectType != null)
                {
                    Company.Delete(SystemObjects.IntersectType, intersectType.ID);
                }

                Company.Delete(ot, at.ObjectID);

                dynamic custom = new
                {
                    Name = at.Name,
                    action = "delete"
                };

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Style Customizations


        [Route("StyleCustomizations")]
        public JsonNetResult StyleCustomizations()
        {
            var css = "";

            //only admins can access this route
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }


            //go to azure storage for this company try to get the custom css
            try
            {
                css = Storage.GetFileContentsAsString(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css");
            }
            catch(Exception ex) {
                SendException(ex);
                return jsonNetException(ex, HttpStatusCode.InternalServerError, string.Empty);
            }

            return new JsonNetResult { Data = (css ?? ""), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPut, ValidateInput(false), Route("UpdateStyleCustomizations")]
        public JsonResult UpdateStyleCustomizations(string css)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            //delete the old css file
            try
            {
                Storage.DeleteFile(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css");
            }
            catch { }

            try
            {
                var settings = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

                var stylesSetting = settings.SingleOrDefault(i => i.SettingID == 24);
                //if the css is not empty or null create a new css
                if (!string.IsNullOrWhiteSpace(css))
                {
                    //update the company setting to sya where the files is 
                    

                    if (stylesSetting == null)
                    {
                        stylesSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 24, Value = $"{constants.COMPANY_STYLES_URL}{Company.CurrentCompanyID}.css" };
                        Community.Add(stylesSetting);
                    }
                    else
                    {
                        stylesSetting.Value = $"{constants.COMPANY_STYLES_URL}{Company.CurrentCompanyID}.css";
                        Community.SaveChanges();
                    }

                    Storage.CreateFile(constants.COMPANY_STYLES_FOLDER, $"{Company.CurrentCompanyID}.css", css, "text/css", false);
                }
                else
                {
                    Community.Delete<CompanySetting>(stylesSetting);
                }
            }
            catch { }

            return jsonSuccess("Syles successfully updated.", "0", "edit", HttpStatusCode.OK);
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
        private bool IsWriteActionDescriptionEnabled()
        {
            var setting = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID && i.SettingID == 61).SingleOrDefault();
            if (setting == null)
                return true;
            else
                return bool.Parse(setting.Value);
             
        }

        [Route("CompanySettings")]
        public JsonNetResult CompanySettings()
        {
            var settings = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID).ToList();
            var model = new CompanySettingsEditorModel();
            model.DisableCommunityPosting = (settings.Any(i => i.SettingID == 1) ? bool.Parse(settings.Single(i => i.SettingID == 1).Value) : false);
            model.DisableIssuePosting = (settings.Any(i => i.SettingID == 5) ? bool.Parse(settings.Single(i => i.SettingID == 5).Value) : false);
            model.DisableIssueManagement = (settings.Any(i => i.SettingID == 17) ? bool.Parse(settings.Single(i => i.SettingID == 17).Value) : false);
            model.UseNewWorkflow = (settings.Any(i => i.SettingID == 18) ? bool.Parse(settings.Single(i => i.SettingID == 18).Value) : false);
            model.EnableShoppingCart = (settings.Any(i => i.SettingID == 20) ? bool.Parse(settings.Single(i => i.SettingID == 20).Value) : false);
            model.DefaultRoute = (settings.Any(i => i.SettingID == 22) ? settings.Single(i => i.SettingID == 22).Value : "");
            model.EnableSearchExactMatch = (settings.Any(i => i.SettingID == 23) ? bool.Parse(settings.Single(i => i.SettingID == 23).Value) : false);
            model.ShowDefaultHelpVideos = (settings.Any(i => i.SettingID == 35) ? bool.Parse(settings.Single(i => i.SettingID == 35).Value) : true);
            model.HideData3SixtyUsers = (settings.Any(i => i.SettingID == 9) ? bool.Parse(settings.Single(i => i.SettingID == 9).Value) : true);
            model.ShowAllUsersAPIKey = (settings.Any(i => i.SettingID == 57) ? bool.Parse(settings.Single(i => i.SettingID == 57).Value) : true);
            model.WorkflowCatchAllGroup = (settings.Any(i => i.SettingID == 58) ? Int32.Parse(settings.Single(i => i.SettingID == 58).Value) : 0);
            model.WorkflowDigestEmailEnabled = (settings.Any(i => i.SettingID == 59) ? bool.Parse(settings.Single(i => i.SettingID == 59).Value) : false);
            model.MaxDropdownItems = (settings.Any(i => i.SettingID == 60) ? Int32.Parse(settings.Single(i => i.SettingID == 60).Value) : 10000);
            model.WriteActionDescription = (settings.Any(i => i.SettingID == 61) ? bool.Parse(settings.Single(i => i.SettingID == 61).Value) : true);

            model.CurrentCompanyIconPath = (settings.Any(i => i.SettingID == 3) ? settings.Single(i => i.SettingID == 3).Value : "");
            model.CurrentCompanyLogoPath = (settings.Any(i => i.SettingID == 2) ? settings.Single(i => i.SettingID == 2).Value : "");
            if (settings.Any(i => i.SettingID == 4))
            {
                var ipRaw = settings.Single(i => i.SettingID == 4).Value;
                if (!string.IsNullOrEmpty(ipRaw))
                {
                    var ipXml = XElement.Parse(ipRaw);
                    var ips = ipXml.Elements("ip").Select(i => new CompanySettingsIpRestrictionEditorModel { Name = i.Element("name").Value, Start = i.Element("start").Value, End = i.Element("end").Value });
                    model.IpRestrictions.AddRange(ips);
                }
            }
            model.ArtifactType_TaxonomyTypeID = (settings.Any(i => i.SettingID == 7) ? settings.Single(i => i.SettingID == 7).Value : "");
            model.ArtifactType_TaxonomyTypeIDNodes = (settings.Any(i => i.SettingID == 8) ? settings.Single(i => i.SettingID == 8).Value : "");

            model.DefaultSearchTypes = (settings.Any(i => i.SettingID == 13) ? settings.Single(i => i.SettingID == 13).Value : "");

            model.FusionEnabled = (settings.Any(i => i.SettingID == 70) ? bool.Parse(settings.Single(i => i.SettingID == 70).Value) : true);
            model.LineageVersion = (settings.Any(i => i.SettingID == 68) ? int.Parse(settings.Single(i => i.SettingID == 68).Value) : 1);

            IQueryable<SiteNav> siteNavs = Company.SiteNav.Where(s => s.ParentID == null && s.Name != "#Home").OrderBy(s => s.SortOrder);
            if (!model.FusionEnabled)
            {
                siteNavs = siteNavs.Where(x => x.Name != "#Fusion");
            }
            else
            {
                siteNavs = siteNavs.Where(x => x.Name != "#Technical");
            }

            model.SiteNav = siteNavs.ToList();

            model.HeaderBackgroundColor = (settings.Any(i => i.SettingID == 10) ? settings.Single(i => i.SettingID == 10).Value : "");

            model.ShowHomeAssignmentTile = (settings.Any(i => i.SettingID == 39) ? bool.Parse(settings.Single(i => i.SettingID == 39).Value) : true);
            model.ShowHomeBoardTile = (settings.Any(i => i.SettingID == 40) ? bool.Parse(settings.Single(i => i.SettingID == 40).Value) : true);
            model.ShowHomeActivityTile = (settings.Any(i => i.SettingID == 41) ? bool.Parse(settings.Single(i => i.SettingID == 41).Value) : true);
            model.ShowHomePageTitle = (settings.Any(i => i.SettingID == 42) ? bool.Parse(settings.Single(i => i.SettingID == 42).Value) : false);
            model.HomePageTitleSize = (settings.Any(i => i.SettingID == 43) ? settings.Single(i => i.SettingID == 43).Value : "38pt");
            model.HomePageTitleColor = (settings.Any(i => i.SettingID == 44) ? settings.Single(i => i.SettingID == 44).Value : "#fff");
            model.HomePageBackgroundImage = (settings.Any(i => i.SettingID == 45) ? settings.Single(i => i.SettingID == 45).Value : "");
            model.BrowserTitlePrefix = (settings.Any(i => i.SettingID == 33) ? settings.Single(i => i.SettingID == 33).Value : "D3S");


            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPut, ValidateInput(false), Route("UpdateCompanySettings")]
        public JsonResult UpdateCompanySettings(CompanySettingsEditorModel formModel)
        {
            try
            {
                if (formModel == null) throw new NoFormDataException("company settings");

                // Permissions validation.
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var settings = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

                #region Icon

                var iconSetting = settings.SingleOrDefault(i => i.SettingID == 3);
                if (formModel.SetIconToDefault)
                {
                    if (iconSetting != null)
                    {
                        Community.Delete<CompanySetting>(iconSetting);
                    }
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
                            Storage.CreateFile(constants.COMPANY_ICON_FOLDER, iconFileName, iconStream);
                            if (iconSetting == null)
                            {
                                iconSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 3, Value = string.Format("{0}{1}", constants.COMPANY_ICON_URL, iconFileName) };
                                Community.Add<CompanySetting>(iconSetting);
                            }
                            else
                            {
                                iconSetting.Value = string.Format("{0}{1}", constants.COMPANY_ICON_URL, iconFileName);
                                Community.SaveChanges();
                            }
                        }
                    }
                }



                #endregion

                #region Logo

                var logoSetting = settings.SingleOrDefault(i => i.SettingID == 2);
                if (formModel.SetLogoToDefault)
                {
                    if (logoSetting != null)
                    {
                        Community.Delete<CompanySetting>(logoSetting);
                    }
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
                                Storage.DeleteFile(constants.COMPANY_LOGO_FOLDER, f);
                            });

                            var logoFileName = string.Format("{0}{1}", Company.CurrentCompanyID, logoExtension);
                            Storage.CreateFile(constants.COMPANY_LOGO_FOLDER, logoFileName, logoStream);

                            if (logoSetting == null)
                            {
                                logoSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 2, Value = string.Format("{0}{1}", constants.COMPANY_LOGO_URL, logoFileName) };
                                Community.Add<CompanySetting>(logoSetting);
                            }
                            else
                            {
                                logoSetting.Value = string.Format("{0}{1}", constants.COMPANY_LOGO_URL, logoFileName);
                                Community.SaveChanges();
                            }
                        }
                    }
                }

                #endregion

                #region Social

                updateCompanySetting(settings, 1, formModel.DisableCommunityPosting.ToString().ToLower());
                updateCompanySetting(settings, 5, formModel.DisableIssuePosting.ToString().ToLower());

                #endregion

                #region Global Fields
                
                updateCompanySetting(settings, 7, formModel.ArtifactType_TaxonomyTypeID);
                updateCompanySetting(settings, 8, formModel.ArtifactType_TaxonomyTypeIDNodes);
                updateCompanySetting(settings, 17, formModel.DisableIssueManagement.ToString().ToLower());
                updateCompanySetting(settings, 20, formModel.EnableShoppingCart.ToString().ToLower());
                updateCompanySetting(settings, 22, (formModel.DefaultRoute ?? "").Trim());
                updateCompanySetting(settings, 23, formModel.EnableSearchExactMatch.ToString().ToLower());
                updateCompanySetting(settings, 35, formModel.ShowDefaultHelpVideos.ToString().ToLower());
                updateCompanySetting(settings, 9, formModel.HideData3SixtyUsers.ToString().ToLower());
                updateCompanySetting(settings, 57, formModel.ShowAllUsersAPIKey.ToString().ToLower());
                updateCompanySetting(settings, 58, formModel.WorkflowCatchAllGroup.ToString());
                updateCompanySetting(settings, 59, formModel.WorkflowDigestEmailEnabled.ToString().ToLower());
                updateCompanySetting(settings, 60, Math.Abs(formModel.MaxDropdownItems).ToString());
                updateCompanySetting(settings, 61, formModel.WriteActionDescription.ToString().ToLower());

                #endregion

                #region IP

                var ipValidationCheckPassed = true;
                var ipSetting = settings.SingleOrDefault(i => i.SettingID == 4);
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
                        if (ipSetting == null)
                        {
                            ipSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 4, Value = xml.ToString() };
                            Community.Add<CompanySetting>(ipSetting);
                        }
                        else
                        {
                            ipSetting.Value = xml.ToString();
                            Community.SaveChanges();
                        }
                    }
                    else
                    {
                        throw new MissingPropertiesException("IP Restrictions");
                    }
                }
                else
                {
                    if (ipSetting != null)
                    {
                        Community.Delete<CompanySetting>(ipSetting);
                    }
                }

                #endregion

                #region Search

                updateCompanySetting(settings, 13, (formModel.DefaultSearchTypes ?? "").ToString());

                #endregion

                #region Header Styles

                updateCompanySetting(settings, 10, formModel.HeaderBackgroundColor);

                #endregion

                #region Home Page Customization

                updateCompanySetting(settings, 39, formModel.ShowHomeAssignmentTile.ToString().ToLower());
                updateCompanySetting(settings, 40, formModel.ShowHomeBoardTile.ToString().ToLower());
                updateCompanySetting(settings, 41, formModel.ShowHomeActivityTile.ToString().ToLower());
                updateCompanySetting(settings, 42, formModel.ShowHomePageTitle.ToString().ToLower());
                updateCompanySetting(settings, 33, formModel.BrowserTitlePrefix);

                //prevent the user from entering special characters
                var alphaNumericChars = "abcdefghijklmnopqrstuvwxyz0123456789";
                var sizeAllowedChars = alphaNumericChars + ".";
                var colorAllowedChars = alphaNumericChars + "#";

                var safeSize = System.Text.RegularExpressions.Regex.Replace(formModel.HomePageTitleSize?.Trim() ?? "", $"[^{sizeAllowedChars}]", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var safeColor = System.Text.RegularExpressions.Regex.Replace(formModel.HomePageTitleColor?.Trim() ?? "", $"[^{colorAllowedChars}]", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                updateCompanySetting(settings, 43, safeSize);
                updateCompanySetting(settings, 44, safeColor);

                #region Home Page Background Image

                var homePageBackgroundSetting = settings.SingleOrDefault(i => i.SettingID == 45);
                if (formModel.ClearHomePageBackgroundImage)
                {
                    if (homePageBackgroundSetting != null)
                    {
                        Community.Delete(homePageBackgroundSetting);
                    }
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
                                Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, f);
                            });

                            var imageFileName = string.Format("{0}.home.{1}{2}", Company.CurrentCompanyID, imageGuid, imageExtension);
                            Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                            //always delete and add for guid
                            if (homePageBackgroundSetting != null)
                                Community.Delete(homePageBackgroundSetting);

                            var newHomePageBackgroundSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 45, Value = string.Format("{0}{1}", constants.COMPANY_RESOURCES_URL, imageFileName) };
                            Community.Add(newHomePageBackgroundSetting);

                        }
                    }
                }

                #endregion

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

        private void updateCompanySetting(List<CompanySetting> settings, int settingID, string value)
        {
            var setting = settings.FirstOrDefault(i => i.SettingID == settingID);
            if (setting == null)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    setting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = settingID, Value = value };
                    Community.Add(setting);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(value))
                {
                    Community.Delete(setting);
                }
                else
                {
                    setting.Value = value;
                    Community.SaveChanges();
                }
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
      


        #region Issue Types

        [Route("IssueTypeRelation_AddFields"), NonNullableParameters]
        public JsonResult IssueTypeRelation_AddFields(int issueTypeId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "IssueTypeID", FieldType = DataType.Hidden.ToString(), Value = issueTypeId.ToString() });

            List<string> ignoreObjects = new List<string>();
            string ignoreObjectTypeSQL = string.Empty;
            if (!Community.IsFusionEnabled())
            {
                ignoreObjects.Add(SystemObjects.FusionType.ToString());
                ignoreObjects.Add(SystemObjects.FusionAttributeType.ToString());
                ignoreObjects.Add(SystemObjects.FusionQueryAttributeType.ToString());
            }

            if (ignoreObjects.Count > 0)
                ignoreObjectTypeSQL = $" AND T.Object not in ({string.Join(",", ignoreObjects.Select(o => "'" + o + "'"))})";

            var availableTypes = Company.Query<SelectListItem>($@"select T.ID as [Value], {QueryConstants.HighLevelTypeCaseStatement} + coalesce(FAT.TextPath, T.[Name]) as [Text]
                from AssetType T
                left join FusionAttributeType FAT on T.[Object] = 'FusionAttributeType' and FAT.ID = T.ObjectID
                where not exists (select 1 from IssueTypeRelation where AssetTypeID = T.ID and IssueTypeID = @issueTypeId)
                {ignoreObjectTypeSQL}
                order by 2", new { issueTypeId }).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "AssetTypeID", Name = "Asset Type", FieldType = DataType.Lookup.ToString(), Items = availableTypes, Required = true });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("IssueType_EditFields"), NonNullableParameters]
        public JsonResult IssueType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<IssueType>(id);

            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        [Route("IssueType_AddFields"), NonNullableParameters]
        public JsonResult IssueType_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Issue_AddFields")]
        public JsonResult Issue_AddFields(int issueTypeId)
        {            
            var list = new List<EditableField>();
            var type = Company.GetById<IssueType>(issueTypeId);

            if (type == null) throw new NotFoundException("issue type");

            list.Add(new EditableField { FieldName = "IssueTypeID", FieldType = DataType.Hidden.ToString(), Value = issueTypeId.ToString() });
         
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIssue")]
        public JsonResult AddIssue(FormCollection form)
        {
            try
            {
                var issueTypeId = parseIntField(form, "IssueTypeID");
                var objectId = parseIntField(form, "ObjectID");
                var objectType = parseTextField(form, "ObjectType");
                var desc = parseTextField(form, "ProblemDesc");
                int commentDetailID=0;
               

                var issueType = Company.GetById<IssueType>(issueTypeId);

                if (issueType == null) throw new NoFormDataException("issue type");

                //get the object name
                var obj = Company.GetObjectDetail(objectType, objectId);

                if (obj == null) throw new NoFormDataException("GetObject");

                if (this.IsWriteActionDescriptionEnabled()) {
                    var relations = new List<CommentRelation>();                
                    var comment = new Comment();

                    relations.Add(new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow });

                comment.OwnerObjectType = SystemObjects.Resource.ToString();
                comment.OwnerObjectID = Company.CurrentResourceID;
                comment.CommentTypeID = CommentType.Issue;
                comment.Body = desc ?? $"New {issueType.Name} Raised.";
                

                    //add relation to current artifact
                    relations.Add(new CommentRelation { ObjectType = objectType, ObjectID = objectId, Date = DateTime.UtcNow });

                    var dtl = Company.AddComment(comment, relations).FirstOrDefault(i => i.ID == comment.ID);
                        commentDetailID = dtl.ID;
                }
               

                //insert issue into issue table
                var model = new Issue
                {
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                    IssueTypeID = issueTypeId,
                    Object = objectType,
                    ObjectID = objectId,
                    ObjectType = obj.Type,
                    ObjectTypeID = obj.TypeID,
                    CommentID = commentDetailID
                };


                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Issue, model.ID, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList(), form, Server);
                Company.SaveOrUpdate(model, fields);

                return jsonSuccess("Successfully created issue.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIssueTypeRelation")]
        public JsonResult AddIssueTypeRelation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("IssueType");

                var model = new IssueTypeRelation
                {
                    IssueTypeID = parseIntField(form, "IssueTypeID"),
                    AssetTypeID = parseIntField(form, "AssetTypeID")
                };

                Company.Add(model);

                return jsonSuccess("Issue Type allocation successfully added.", model.AssetTypeID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, ValidateInput(false), Route("DeleteIssueTypeRelation"), NonNullableParameters]
        public JsonResult DeleteIssueTypeRelation(int issueTypeID, int assetTypeID)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (issueTypeID < 1 || assetTypeID < 1) throw new InvalidDataException("IssueTypeRelation");

                var relation = Company.IssueTypeRelations.Where(i => i.IssueTypeID == issueTypeID && i.AssetTypeID == assetTypeID).FirstOrDefault();
                Company.Delete(relation);

                return jsonSuccess("Issue Type allocation successfully deleted.", assetTypeID.ToString(), "delete", HttpStatusCode.OK);

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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIssueType")]
        public JsonResult AddIssueType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("IssueType");

                var model = new IssueType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),  
                    IsSystem = false,                  
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow                    
                };

                Company.Add(model);

                if (model.ID > 0)
                {
                    Company.Add(new FieldType
                    {
                        ObjectID = model.ID,
                        Object = SystemObjects.IssueType.ToString(),
                        IsListable = true,
                        IsRequired = true,
                        IsEditable = true,
                        FriendlyName = "Description",
                        Name = "ProblemDesc",                        
                        SortOrder = 1,
                        Type = DataType.Html.ToString()
                    });
                }

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), Route("EditIssueType")]
        public JsonResult EditIssueType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("issuetype");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<IssueType>(id);

                if (model == null) throw new NotFoundException("issuetype");

                model.Name = form["Name"];
                model.Description = form["Description"];
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.SaveOrUpdate(model);

                return jsonSuccess("Item successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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


        [HttpDelete, Route("DeleteIssueType")]
        public JsonResult DeleteIssueType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("issue type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<IssueType>(id);
                if (model == null) throw new NotFoundException("issue type");
                
                var typeRelations = Company.IssueTypeRelations.Where(i => i.IssueTypeID == id).ToList();
                Company.IssueTypeRelations.RemoveRange(typeRelations);

                var customFields = Company.FieldTypes.Where(x => x.Object == SystemObjects.IssueType.ToString() && x.ObjectID == id);
                Company.FieldTypes.RemoveRange(customFields);
                Company.Delete<IssueType>(i => i.ID == id);
                
                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        #region Group
                
        #region Form Get/Post


        #region Group : Add User
                

        [HttpPost, AjaxValidateAntiForgeryToken,  ValidateInput(false), ActionName("ResourceGroup"), Route("ResourceGroup")]
        public JsonResult PostResourceGroup(ResourceGroup[] model)
        {
            try
            {
                if (!Company.HasAssetPermission(SystemObjects.Group, model[0].GroupID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                foreach (var m in model)
                    Company.Add(m);

                return jsonSuccess("User successfully assigned.", model[0].ResourceID.ToString(), "add", HttpStatusCode.Created);
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

      
        [HttpGet, Route("GetGroupUserList"), NonNullableParameters]
        public JsonNetResult GetGroupUserList(int id, int pagenum, int pagesize, string sortDataField, string sortOrder,string gbfilter)
        {

            string querySql;
            var dbArgs = new Dapper.DynamicParameters();

            var hideUsersSql = "";

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com')";
            }

            querySql = @"
			select  r.LastName + ', ' + r.FirstName as Text, 'Resource|' + cast(r.ResourceID as varchar) + '|' + r.LastName + ', ' + r.FirstName  as [Value],'User' as [Type] from reporting.Global_Resource r                                    
			where r.[State] = @userStatus 
			and  not exists   (select 1 from ResourceGroup where Groupid =@id   and ResourceID= r.ResourceID) "
            + hideUsersSql;
            dbArgs.Add("id", id);
            dbArgs.Add("userStatus", CompanyResourceState.Active);

            if (!string.IsNullOrEmpty(gbfilter))
            {
                querySql = string.Format(@"select * from ({0}) gb where  [Text] like '%' +   @gbfilter + '%'", querySql);
                dbArgs.Add("gbfilter", gbfilter);
            }
            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);
            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            int totalCount = Company.Query<int>(countSql, dbArgs).First();
            
            sql = applySortSuffix(sql, sortDataField, sortOrder, "Text", "asc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);
            
            return new JsonNetResult
            {
                Data = new { total = totalCount, results = query },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #region Group : Delete User
        
        [HttpDelete,  ActionName("ResourceGroup"), Route("ResourceGroup"), NonNullableParameters]
        public JsonResult DeleteResourceGroup(int groupID, int resourceID)
        {
            try
            {
                if (!Company.HasAssetPermission(SystemObjects.Group, groupID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                Company.Delete<ResourceGroup>(i => i.GroupID == groupID && i.ResourceID == resourceID);

                return jsonSuccess("User successfully removed from group.", resourceID.ToString(), "delete", HttpStatusCode.OK);
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

        #region Group : Delete

        [HttpDelete, Route("DeleteGroup")]
        public JsonResult DeleteGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Group>(id);
                if (model == null) throw new NotFoundException("group");

                if (!Company.HasAssetPermission(SystemObjects.Group, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteGroupByID"), NonNullableParameters]
        public JsonResult DeleteGroupByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteGroup(form);
        }

        #endregion

        #region Group : Edit

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false),  ActionName("Group"), Route("Group")]
        public JsonResult PostGroup(Group model)
        {
            try
            {
                if (!Company.HasAssetTypePermission(SystemObjects.Group, 0, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Add(model);

                Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = (int)model.PrimaryOwnerResourceID });
                try
                {
                    if (model.SecondaryOwnerResourceID.HasValue)
                    {
                        if (!model.PrimaryOwnerResourceID.Equals(model.SecondaryOwnerResourceID))
                            Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.SecondaryOwnerResourceID.Value});
                    }
                }
                catch
                {
                }

                long assetId = Company.Assets.FirstOrDefault(x => x.Object == "Group" && x.ObjectID == model.ID).ID;

                Company.CreateOrUpdateDisplayValue(assetId);

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false),  ActionName("Group"), Route("Group")]
        public JsonResult PutGroup(Group model)
        {
            try
            {
                var existing = Company.GetById<Group>(model.ID);
                if (existing == null) throw new NotFoundException("group");

                if (!Company.HasAssetPermission(SystemObjects.Group, existing.ID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                existing.Name = model.Name;
                existing.Description = model.Description;
                existing.PrimaryOwnerResourceID = model.PrimaryOwnerResourceID; 
                existing.SecondaryOwnerResourceID = model.SecondaryOwnerResourceID;

                Company.Update(existing);

                var currentGroupUsers = Company.Filter<ResourceGroup>(i => i.GroupID == model.ID).Select(i => i.ResourceID).ToList();

                if (!currentGroupUsers.Any(o => o == model.PrimaryOwnerResourceID))
                {
                    Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.PrimaryOwnerResourceID.Value });
                }
                if (model.SecondaryOwnerResourceID.HasValue)
                {
                    if (!currentGroupUsers.Any(o => o == model.SecondaryOwnerResourceID))
                    {
                        Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.SecondaryOwnerResourceID.Value });
                    }
                }

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpGet, ActionName("Group"), Route("Group"), NonNullableParameters]
        public JsonNetResult GetGroup(int id)
        {
            var group = new Group();
            var resourceList = new List<SelectListItem>();

            if (id == 0)
            {
                resourceList = GetCompanyResources()
                    .OrderBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName })
                    .ToList()
                    .Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                    .ToList();
            }
            else
            {
                group = Company.GetById<Group>(id);

                var primaryOwner = GetCompanyResources().Where(x => x.ResourceID == group.PrimaryOwnerResourceID).FirstOrDefault();
                var secondaryOwner = GetCompanyResources().Where(x => x.ResourceID== group.SecondaryOwnerResourceID).FirstOrDefault();
                group.PrimaryOwnerName = primaryOwner!=null ? primaryOwner.LastName + ", " + primaryOwner.FirstName:"";
                group.SecondaryOwnerName = secondaryOwner!=null ? secondaryOwner.LastName + ", " + secondaryOwner.FirstName : "";

                var currentUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
                resourceList = GetCompanyResources()
                    .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName, MembershipStatus = currentUsers.Any(o => o == i.ResourceID) ? "Current Member" : "Not Yet a Member" })
                    .OrderBy(i => i.MembershipStatus)
                    .ThenBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .ToList()
                    .Select(i => new SelectListItem { Group = new SelectListGroup { Name = i.MembershipStatus }, Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                    .ToList();
            }

            resourceList.Insert(0, new SelectListItem { Text = "None", Value = "", Group = new SelectListGroup { Name = "" } });

            return new JsonNetResult
            {
                Data = new
                {
                    group,
                    resourceList,
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #endregion

        #endregion

        #region LEGACY Lineage (TO BE REMOVED WHEN NEW LINEAGE IS COMPLETE)
        
        [HttpPost, AjaxValidateAntiForgeryToken, Route("UpdateLineage")]
        public JsonNetResult UpdateLineage(LineageEditorModel model)
        {
            if (!Company.HasAssetPermission(model.Focal, model.FocalID, Permission.ModifyAsset))
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            model.Deletes?.ForEach(d =>
            {
                var mapItem = Company.GetById<MapItem>(d.ID);
                
                try
                {
                    mapItem.Maps.ToList().ForEach(m =>
                    {
                        m.MapItems.Remove(mapItem);
                    });
                    Company.MapSequences.RemoveRange(mapItem.MapSequences);
                    Company.MapItems.Remove(mapItem);

                    Company.SaveChanges();

                }
                catch (Exception ex)
                {
                    //reset state on fail to avoid future errors in SaveChanges()
                    if (mapItem != null)
                        Company.Entry(mapItem).State = System.Data.Entity.EntityState.Unchanged;

                    d.HasError = true;
                    d.ErrorMessage = ex.GetFullExceptionData();
                }
            });

            model.Adds?.ForEach(a =>
            {
                try
                {
                    var sourceIntersect = Company.IntersectDetails.Where(i =>
                    i.IntersectTypeID == a.SourceIntersectTypeID &&
                    i.SubjectID == a.SourceSubjectID &&
                    i.ObjectID == a.SourceObjectID).SingleOrDefault();

                    if (sourceIntersect == null)  //add source intersect if it doesn't exist
                        sourceIntersect = Company.AddIntersect(a.SourceIntersectTypeID, a.SourceSubject, a.SourceSubjectID, a.SourceObject, a.SourceObjectID);


                    var targetIntersect = Company.IntersectDetails.Where(i =>
                    i.IntersectTypeID == a.TargetIntersectTypeID &&
                    i.SubjectID == a.TargetSubjectID &&
                    i.ObjectID == a.TargetObjectID).SingleOrDefault();

                    if (targetIntersect == null)//add target intersect
                        targetIntersect = Company.AddIntersect(a.TargetIntersectTypeID, a.TargetSubject, a.TargetSubjectID, a.TargetObject, a.TargetObjectID);

                    //add map item
                    var mapItem = Company.MapItems.Where(i => i.SourceIntersectID == sourceIntersect.ID && i.TargetIntersectID == targetIntersect.ID).SingleOrDefault();

                    if (mapItem == null)
                        mapItem = Company.MapItems.Add(new MapItem()
                        {
                            SourceIntersectID = sourceIntersect.ID,
                            TargetIntersectID = targetIntersect.ID,
                            CreatedBy = Company.CurrentResourceID,
                            UpdatedBy = Company.CurrentResourceID
                        });

                    Company.SaveChanges();
                    a.SourceIntersectID = sourceIntersect.ID;
                    a.TargetIntersectID = targetIntersect.ID;
                    a.ID = mapItem.ID;
                }
                catch (Exception ex)
                {
                    a.HasError = true;
                    a.ErrorMessage = ex.GetFullExceptionData();
                }
            });

            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("UpdateTechnicalLineage")]
        public JsonNetResult UpdateTechnicalLineage(LineageEditorTechnicalModel model)
        {
            if (!Company.HasAssetPermission(model.Focal, model.FocalID, Permission.ModifyAsset))
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            model.Deletes?.ForEach(d =>
            {
                var mapRuleItem = Company.GetById<MapRuleItem>(d.ID);

                var mapRuleItemMapItem = Company.MapRuleItemMapItems.Where(m => m.MapRuleItemID == mapRuleItem.ID).FirstOrDefault();

                try
                {
                    if (mapRuleItemMapItem != null)
                    {
                        Company.MapRuleItemMapItems.Remove(mapRuleItemMapItem);
                    }

                    mapRuleItem.MapRules.ToList().ForEach(m =>
                    {
                        m.MapRuleItems.Remove(mapRuleItem);
                    });

                    Company.MapRuleItems.Remove(mapRuleItem);

                    Company.SaveChanges();
                }
                catch (Exception ex)
                {
                    d.HasError = true;
                    d.ErrorMessage = ex.GetFullExceptionData();
                }
            });

            model.Adds?.ForEach(a =>
            {
                try
                {
                    var mapRuleItem = new MapRuleItem();
                    mapRuleItem.SourceFusionAttributeID = a.SourceFusionAttributeID;
                    mapRuleItem.TargetFusionAttributeID = a.TargetFusionAttributeID;

                    //add map rule item
                    Company.MapRuleItems.Add(mapRuleItem);
                    Company.SaveChanges();

                    //add map rule item map item
                    if (a.MapItemID > 0)
                    {
                        Company.MapRuleItemMapItems.Add(new MapRuleItemMapItem()
                        {
                            MapItemID = a.MapItemID,
                            MapRuleItemID = mapRuleItem.ID

                        });
                        Company.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    a.HasError = true;
                    a.ErrorMessage = ex.GetFullExceptionData();
                }
            });

            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #region MapSequence

        public class MapItemsSequenceEditModel
        {
            public List<MapItemSequenceEditModel> Items { get; set; }
        }

        public class BaseObjectModel
        {
            public string Object { get; set; }
            public int ObjectID { get; set; }
        }


        public class MapItemSequenceEditModel
        {
            public MapItemSequenceEditModel()
            {
                Contexts = new List<BaseObjectModel>();
            }
            public int ID { get; set; }
            public int MapItemID { get; set; }
            public string Description { get; set; }
            public int Sequence { get; set; }
            public List<BaseObjectModel> Contexts { get; set; }
            public bool IsDeleting { get; set; } = false;
        }

        [HttpGet, Route("mapsequence/{type}/{id:int}/mapitems")]
        public JsonNetResult GetMapItemsForMapSequenceManagement(string type, int id)
        {
            var availableItems = Company.Query<dynamic>(
                QueryConstants.MapItemsForMapSequenceManagement,
                new
                {
                    type = new Dapper.DbString { IsAnsi = true, Value = type },
                    id
                }
            );

            var availableIDs = availableItems.Select(i => (int)i.ID).ToList();

            var referencedItems = Company.Filter<MapSequence>(i =>
               availableIDs.Contains(i.MapItemID),
                i => i.MapSequenceContexts,
                i => i.MapItem
            )
            .OrderBy(i => i.Sequence)
            .Select(i => new
            {
                i.ID,
                i.MapItemID,
                i.MapItem.TargetIntersectID,
                i.Sequence,
                i.Description,
                Contexts = i.MapSequenceContexts.Select(c => new { c.Object, c.ObjectID }).ToList()
            });


            var contexts = Company.Query<dynamic>($@"		
                select 
			        D.DisplayValue as [Name],
			        A.[Object] + '|' + cast(A.ObjectID as varchar) as ID,
			        case 
                        when A.[Object] = 'Artifact' and A.AssetTypeClass = 1 then '{CommonNames.AssetTypeClass_Business.CleanForSql()}'
                        when A.[Object] = 'Artifact' and A.AssetTypeClass = 8 then '{CommonNames.AssetTypeClass_Technical.CleanForSql()}'
			            when A.[Object] = 'Taxonomy' then 'Model'
			            else ''
			        end as Category,
			        A.TypeName as [Type],
			        cast(0 as bit) as Checked
		        from 
			        AssetWithType A
			        cross apply dbo.GetAssetDisplayValueById(A.ID) D
		        where 
			        A.[Object] = 'Artifact' or A.[Object] = 'Taxonomy'
		        order by D.DisplayValue").ToList();

            return new JsonNetResult
            {
                Data = new
                {
                    Available = availableItems,
                    Referenced = referencedItems,
                    Contexts = contexts
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("mapsequence/{type}/{id:int}/mapitems")]
        public JsonResult SetMapItemsForMapSequenceManagement(SystemObjects type, int id, MapItemsSequenceEditModel model)
        {
            try
            {
                if (!Company.HasAssetPermission(type, id, Permission.ModifyRelationships))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                model.Items.ForEach(m =>
                {
                    MapSequence mapSequence = null;
                    if (m.ID > 0)
                    {
                        mapSequence = Company.GetById<MapSequence>(m.ID, i => i.MapSequenceContexts);
                    }

                    if (m.IsDeleting)
                    {
                        if (mapSequence == null)
                        {                            
                            return;
                        }

                        Company.MapSequences.Remove(mapSequence);
                        Company.SaveChanges();
                    }
                    else
                    {
                        if (mapSequence == null)
                        {
                            mapSequence = new MapSequence { };
                        }



                        mapSequence.Description = m.Description;
                        mapSequence.MapItemID = m.MapItemID;
                        mapSequence.Sequence = m.Sequence;

                        if (m.Contexts.Count > 0)
                        {
                            m.Contexts.ForEach(c => {
                                mapSequence.MapSequenceContexts.Add(
                                    new MapSequenceContext
                                    {
                                        Object = c.Object,
                                        ObjectID = c.ObjectID
                                    }
                                );
                            });
                        }

                        Company.SaveOrUpdate<MapSequence>(mapSequence);
                    }

                });

                return jsonSuccess("Source Conditions successfully created.", "0", "add", HttpStatusCode.Created, null);
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
            switch (act) {
                case "O":   // Responsibility/Ownership
                    #region
                    sql = $@"
select * from (
select 'FusionType|0' as value, 'Fusion' as title
union
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

                    var fusionEnabled = Community.GetCompanySettingByKey<bool>("FusionEnabled");
                    string technicalAssetSql = "";
                    if (!fusionEnabled) { 
                        technicalAssetSql = $@"union
select		4 as Sort,
			'ArtifactType|' + cast(ObjectID as varchar(10)) as value, 
			'{CommonNames.AssetTypeClass_Technical.CleanForSql()}: ' + P.[Path] as title 
from		AssetType A
			cross apply dbo.GetAssetTypeTextPathById(A.ID, ' > ') P
where		[Class] = 8";
                    }

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

		{technicalAssetSql}

		union

		select		5 as Sort,
					'AttributeType|' + cast(ID as varchar(10)) as value, 'Attribute: ' + Name as title 
		from		AttributeType 
		where		ParentID is null
		) O
order by Sort, title";
                    break;
                #endregion
                case "R":   // Relation
                case "U":   // Unrelation
                    #region
                    sql = @"select 'IntersectType|' + cast(itd.ID as varchar(10)) as value, IName.Name as title from intersecttypedetail itd cross apply dbo.GetIntersectTypeNames(itd.ID) IName	 where itd.IsSystem = 0 or (itd.Subject = 'ReferenceItemType' and itd.Object = 'ReferenceItemType') order by IName.Name";
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
                case "TL":   // Technical Lineage
                    models = new List<OptionModel> { new OptionModel { title = "Default", value = "TechnicalLineage|-1" } };
                    break;
            }

            if (!string.IsNullOrEmpty(sql))
                models = Company.Query<OptionModel>(sql);

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

                style.Font.Bold = model.Required;

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
            if (!values.Any()) return;

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

        private void CreateExcelList(int numLookupColumns, SLDocument document, string lookupWorksheetName, SLDataValidation dataValidation, Dictionary<string, string> values)
        {
            if (!values.Any()) return;

            var currentSheet = document.GetCurrentWorksheetName();
            document.SelectWorksheet(lookupWorksheetName);
            int rowNum = 0;
            foreach (var key in values.Keys)
            {
                document.SetCellValue(++rowNum, numLookupColumns, WebUtility.HtmlDecode(key));
                document.SetCellValue(rowNum, numLookupColumns + 1, WebUtility.HtmlDecode(values[key]));
            }

            document.SelectWorksheet(currentSheet);

            //add a column to the given lookup worksheet with the specified values
            string range = SLConvert.ToCellRange(lookupWorksheetName, 1, numLookupColumns, rowNum, numLookupColumns + 1, true);
            dataValidation.AllowList($"={range}", true, true);
        }

        [ HttpPost, AjaxValidateAntiForgeryToken, Route("AddLoad")]
        public JsonResult AddLoad(LoadFilePostModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                // Perform checks to make sure fields are populated.
                if (string.IsNullOrEmpty(model.Type)) throw new NoFormDataException("Type");
                if (string.IsNullOrEmpty(model.LoadAction)) throw new NoFormDataException("LoadAction");

                var match = MimeTypeExtensionsMap.RegEx.Match(model.File);

                var mime = match.Groups["mime"].Value;                
                var data = match.Groups["data"].Value;
                var extension = MimeTypeExtensionsMap.GetExtension(mime);
                var byteArray = Convert.FromBase64String(data);

                JsonResult json;
                Load load = null;
                var success = false;
                var errorMessage = "";
                SLDocument xls;

                using (var stream = new MemoryStream(byteArray))
                {
                    if (extension == ".xlsx")
                    {
                        var typeInfo = model.Type.Split('|');
                        var assetType = Company.Query<AssetType>("select * from AssetType where Object = @object and ObjectID = @objectID", new { @object = typeInfo[0], objectID = typeInfo[1] }).FirstOrDefault();

                        load = new Load
                        {
                            File = stream.ToArray(),
                            Action = model.LoadAction,
                            Extension = extension,
                            Notes = model.Notes,
                            Object = typeInfo[0],
                            ObjectID = int.Parse(typeInfo[1]),
                            DateStarted = DateTime.UtcNow,
                            UpdatedBy = Company.CurrentResourceID,
                            AssetTypeUid = assetType?.uid
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
                                break;
                            }
                            else
                            {
                                columnCount++;
                            }
                        }

                        // spreadsheet should not have more columns than the type has
                        // it can have less
                        // spreadsheet should only contain columns that the type has
                        if (columnCount <= fieldTypeNames.Count)
                        {
                            var hasError = false;
                            load.LoadColumns = new List<LoadColumn>();
                            //loop through spreadsheet columns and make sure type has that column
                            for (var i = stats.StartColumnIndex; i <= stats.EndColumnIndex; i++)
                            {
                                var columnName = (xls.GetCellValueAsString(1, i) ?? string.Empty).Trim();

                                if (string.IsNullOrEmpty(columnName)) continue;

                                if (!fieldTypeNames.Any(x => x.Name == columnName))
                                {
                                    hasError = true;
                                    errorMessage += string.Format("Unexpected column found [{0}]", columnName);
                                }
                                else
                                {
                                    load.LoadColumns.Add(new LoadColumn { ColumnIndex = i, Name = columnName });
                                }
                            }

                            success = !hasError;
                        }
                        else
                        {
                            errorMessage = "The number of columns in the spreadsheet exceeds the number of defined fields for this load type.";
                        }
                    }
                    else
                    {
                        errorMessage = "Incorrect file type";
                    }
                }

                if (success)
                {
                    //TODO: cleanup
                    load.File = null;
                    Company.Add<Load>(load);
                    Storage.CreateFolder($"{constants.COMPANY_BULK_LOAD_FOLDER}");
                    Storage.CreateFile($"{constants.COMPANY_BULK_LOAD_FOLDER}", $"{Company.CurrentCompanyID}/load_{load.ID}.{load.Extension}", new MemoryStream(byteArray));
                    Company.Enqueue(Config.GetValue<string>("BulkLoadQueue"), new BulkLoadInfo { CompanyID = Company.CurrentCompanyID, LoadID = load.ID, To = QueueAction.BulkLoad });

                    json = jsonSuccess("File uploaded and queued for processing.", load.ID.ToString(), "A", HttpStatusCode.Created);
                }
                else
                {
                    json = jsonException(errorMessage, HttpStatusCode.BadRequest);
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
            var loadItems = Company.Query<dynamic>(itemSql, new { id}).ToList();
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
                var fileString = Storage.GetFileContentsAsString($"{constants.COMPANY_BULK_LOAD_FOLDER}/{Company.CurrentCompanyID}", $"load_{load.ID}.{load.Extension}");
                bytes = Encoding.Default.GetBytes(fileString);
            }
            return File(bytes, "application/vnd.ms-excel", $"{load.DateCompleted.ToString()}.xlsx");
        }

        #endregion

        #region Lookup

        #region Field Generation

        /// <param name="id">LookupTypeID</param>
        [Route("Lookup_AddFields"), NonNullableParameters]
        public JsonResult Lookup_AddFields(int id)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.LookupType, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            
            list.Add(new EditableField { FieldName = "LookupTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.LookupType, id).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
        [Route("Lookup_EditFields"), NonNullableParameters]
        public JsonResult Lookup_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Lookup>(id);

            if (!Company.HasAssetTypePermission(SystemObjects.LookupType, a.LookupTypeID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list =(
                loadDynamicFields(
                    SystemObjects.Lookup.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.LookupType, a.LookupTypeID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Lookup, id).ToList(), 
                    1
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post
        
        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddLookup")]
        public JsonResult AddLookup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup");

                int typeID = parseIntField(form, "LookupTypeID");
                var type = Company.GetById<LookupType>(typeID);

                if (type == null) throw new NotFoundException("lookup type");

                if (!Company.HasAssetTypePermission(SystemObjects.LookupType, typeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new Lookup
                {
                    LookupTypeID = typeID
                };

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Lookup, a.ID, Company.GetFieldTypesByObject(SystemObjects.LookupType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate(a, fields);

                return jsonSuccess(type.Name + " successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteLookupByIdRaw")]
        public ActionResult DeleteLookupByIdRaw(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteLookup(form);
        }

        [HttpDelete, Route("DeleteLookup")]
        public JsonResult DeleteLookup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Lookup");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Lookup>(id);
                if (model == null) throw new NotFoundException("Lookup");

                if (!Company.HasAssetTypePermission(SystemObjects.LookupType, model.LookupTypeID, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditLookup")]
        public JsonResult EditLookup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Lookup>(id);

                if (model == null) throw new NotFoundException("lookup");

                if (!Company.HasAssetTypePermission(SystemObjects.LookupType, model.LookupTypeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Lookup, model.ID, Company.GetFieldTypesByObject(SystemObjects.LookupType, model.LookupTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate(model, fields);

                return jsonSuccess("Item successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        #region LookupType
        
        #region Form Get/Post

        public class LookupTypeModel
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddLookupTypeRaw")]
        public JsonResult AddLookupTypeRaw(LookupTypeModel lookup)
        {
            var form = new FormCollection();
            form.Add("Name", lookup.Name);            

            return AddLookupType(form);
        }

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddLookupType")]
        public JsonResult AddLookupType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("lookup type");

                var a = new LookupType
                {
                    Name = parseTextField(form, "Name")
                };

                Company.Add(a);

                if (a.ID > 0)
                {
                    Company.Add(new FieldType
                    {
                        ObjectID = a.ID,
                        Object = SystemObjects.LookupType.ToString(),
                        IsListable = true,
                        IsRequired = true,
                        FriendlyName = "Name",
                        Name = "Name",
                        MaximumLength = 250,
                        MinimumLength = 1,
                        SortOrder = 1,
                        Type = DataType.Text.ToString(),
                        IsEditable = true,
                        IsPartOfKey=true
                    });
                }

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteLookupType")]
        public JsonResult DeleteLookupType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup type");

                var id = parseIntField(form, "ID");

                if (!Company.HasAssetTypePermission(SystemObjects.LookupType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.LookupType, id);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditLookupTypeRaw")]
        public JsonResult EditLookupTypeRaw(LookupTypeModel lookup)
        {
            var form = new FormCollection();
            form.Add("Name", lookup.Name);
            form.Add("ID", lookup.ID.ToString());

            return EditLookupType(form);
        }

        [HttpPut, ValidateInput(false), Route("EditLookupType")]
        public JsonResult EditLookupType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LookupType>(id);
                if (model == null) throw new NotFoundException("lookup type");

                if (!Company.HasAssetTypePermission(SystemObjects.LookupType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");

                Company.Update(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusMessage, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region Map

        [Route("Map_AddFields"), NonNullableParameters]
        public JsonResult Map_AddFields()
        {
            var list = new List<EditableField>();
            
            if (!Company.HasAssetPermission(SystemObjects.Map, 0, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var mapTypes = new List<SelectListItem>();            
            
            foreach (var item in Company.MapTypes)
            {
                mapTypes.Add(new SelectListItem { Text = item.Name, Value = item.ID.ToString() });
            }
                        
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = "" });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Transformation", Name = "Transformation", FieldType = DataType.Text.ToString(), Value = "" });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "MapType", Name = "Type", FieldType = DataType.Lookup.ToString(), Items = mapTypes });            
            
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Map_EditFields"), NonNullableParameters]
        public JsonResult Map_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<core.entities.Map>(id);

            if (!Company.HasAssetPermission(SystemObjects.Map, a.ID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var mapTypes = new List<SelectListItem>();

            foreach (var item in Company.MapTypes)
            {
                mapTypes.Add(new SelectListItem { Text = item.Name, Value = item.ID.ToString() });
            }

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "MapType", Name = "Type", FieldType = DataType.Lookup.ToString(), Items = mapTypes, Value = a.MapTypeID.ToString() });            

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddMap")]
        public JsonResult AddMap(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Map");

                var map = new Map
                {                    
                    MapTypeID = parseIntField(form, "MapType"),
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                if (!Company.HasAssetTypePermission(SystemObjects.MapType, map.MapTypeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Add(map);

                return jsonSuccess("Map successfully allocated.", map.ID.ToString(), "add", HttpStatusCode.Created);
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

        [ HttpPut, ValidateInput(false), Route("EditMap")]
        public JsonResult EditMap(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Map");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Map>(id);
                
                if (!Company.HasAssetPermission(SystemObjects.Map, model.ID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                
                model.MapTypeID = parseIntField(form, "MapType");                
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;
                
                Company.Update(model);

                return jsonSuccess("Map successfully updated.", model.ID.ToString(), "update", HttpStatusCode.OK);
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


        [HttpDelete, Route("DeleteLineageMapping")]
        public JsonResult DeleteLineageMapping(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lineage mapping");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Map>(id);
                if (model == null) throw new NotFoundException("mapping");

                if (!Company.HasAssetPermission(SystemObjects.Map, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        #region Policy

        #region Field Generation

        [Route("Policy_AddFields"), NonNullableParameters]
        public JsonResult Policy_AddFields(int typeID, int? parentID)
        {            
            if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, typeID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            if (parentID.HasValue && parentID.Value > 0) {
                var parentUid = Company.GetAssetUid(parentID.Value, SystemObjects.Policy).ToString();
                list.Add(new EditableField { FieldName = "ParentUid", FieldType = DataType.Hidden.ToString(), Value = parentUid });
            }
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.PolicyType, typeID).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyID</param>
        [Route("Policy_EditFields"), NonNullableParameters]
        public JsonResult Policy_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Policy, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                        
            var model = Company.Assets.Where(x => x.ObjectID == id && x.Object == SystemObjects.Policy.ToString()).Include(x => x.AssetType).FirstOrDefault();
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = model.uid.ToString() });

            list = (
                loadDynamicFields(
                    SystemObjects.Policy.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.AssetType.ObjectID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Policy, id).ToList(), 
                    1, 
                    true
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        #endregion

        #endregion

        #region PolicyTypeLevel
                
        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddPolicyTypeLevel")]
        public JsonResult AddPolicyTypeLevel(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("policy type level");

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "PolicyType").SingleOrDefault();

                if (assetType == null) throw new NotFoundException("asset type");

                var a = new AssetTypeLevel
                {
                    AssetTypeID = assetType.ID,
                    Level = level,
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<AssetTypeLevel>(a);

                return jsonSuccess(a.Name + " successfully created.", a.AssetTypeID.ToString(), "add", HttpStatusCode.Created);
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

        [Route("PolicyType/{policyTypeId:int}/levels/{policyTypeLevelId:int}")]
        public ActionResult DeleteTaxonomyTypeLevelById(int policyTypeId, int policyTypeLevelId)
        {
            var form = new FormCollection();
            form.Add("Level", policyTypeLevelId.ToString());
            form.Add("ID", policyTypeId.ToString());
            return DeletePolicyTypeLevel(form);
        }

        [HttpDelete, Route("DeletePolicyTypeLevel")]
        public JsonResult DeletePolicyTypeLevel(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("policy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "PolicyType").SingleOrDefault();

                if (assetType == null) throw new NotFoundException("asset type");

                Company.Delete<AssetTypeLevel>(i => i.AssetTypeID == assetType.ID && i.Level == level);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditPolicyTypeLevel")]
        public JsonResult EditPolicyTypeLevel(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("policy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "PolicyType").SingleOrDefault();

                if (assetType == null) throw new NotFoundException("asset type");

                var model = Company.Filter<AssetTypeLevel>(i => i.AssetTypeID == assetType.ID && i.Level == level).SingleOrDefault();
                if (model == null) throw new NotFoundException("policy type level");


                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<AssetTypeLevel>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Reference Item

        /// <param name="id">LookupTypeID</param>
        [Route("ReferenceItem_AddFields"), NonNullableParameters]
        public JsonResult ReferenceItem_AddFields(int id)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.ReferenceItemType, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var row = 1;

            list.Add(new EditableField { FieldName = "ReferenceItemTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = row++, Column = 1, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString() });

            //if the reference type has a parent we need to add parent field with the values from the parent

            var parentType = Company.GetParentType(id, SystemObjects.ReferenceItemType);

            if(parentType != null)
            {
                var sql = "select DisplayValue, uid from assetdetail where [object] = 'Referenceitem' and TypeID = @id";
                list.Add(new EditableField { Row = row++, Column = 1, FieldName = "ParentUid", Name = parentType.Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = false, Items = Company.Query<dynamic>(sql, new { id = parentType.ObjectID }).Select(i => new SelectListItem { Text = i.DisplayValue, Value = string.Format("{0}", i.uid) }).ToList() });
            }
                        
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, id).ToList(), row);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
        [Route("ReferenceItem_EditFields"), NonNullableParameters]
        public JsonResult ReferenceItem_EditFields(int id)
        {
            var list = new List<EditableField>();            
            var a = Company.Assets.FirstOrDefault(x => x.ObjectID == id && x.Object == "ReferenceItem");

            if (!Company.HasAssetPermission(SystemObjects.ReferenceItem, a.ObjectID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var row = 1;

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { Row = row++, Column = 1, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString(), Value = a.Code.ToString() });

            //if the reference type has a parent we need to add parent field with the values from the parent
            
            var parentType = Company.GetParentType(a.AssetType.ObjectID, SystemObjects.ReferenceItemType);

            if (parentType != null)
            {
                var parent = Company.GetParentObject(id, SystemObjects.ReferenceItem);
                var sql = "select DisplayValue, uid from assetdetail where [object] = 'Referenceitem' and TypeID = @id";
                list.Add(new EditableField { Row = row++, Column = 1, FieldName = "ParentUid", Name = parentType.Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = false, Items = Company.Query<dynamic>(sql, new { id = parentType.ObjectID }).Select(i => new SelectListItem { Text = i.DisplayValue, Value = string.Format("{0}", i.uid), Selected = i.uid == (parent != null ? parent.uid : Guid.Empty)  }).ToList() });
            }

            list = loadDynamicFields(SystemObjects.ReferenceItem.ToString(), id, list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, a.AssetType.ObjectID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.ReferenceItem, id).ToList(), row);

            return Json(list, JsonRequestBehavior.AllowGet);
        }
         
        #endregion

        #region Report

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddReport")]
        public async Task<JsonResult> AddReport(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var objectType = form["ObjectType"].Split('|').ToArray();

                if (objectType.Length == 2)
                {
                    var fileCount = HttpContext.Request.Files.Count;
                    var reportType = parseTextField(form, "ReportType");
                    var name = parseTextField(form, "Name");
                    var showOnHomePage = reportType == "legacy" ? false : parseBooleanField(form, "ShowOnHomePage");
                    string powerBIID = string.Empty;
                    string datasetID = string.Empty;
                    string filename = string.Empty;

                    if (fileCount > 0 && reportType == "powerbi")
                    {
                        var file = HttpContext.Request.Files[0];
                        

                        if (file.ContentLength > 0)
                        {
                            var importResult = await uploadPowerBIReport(file, name);
                            
                            if (importResult.ImportState == "Failed")
                                throw new Exception("FAILED TO LOAD POWER BI WORKSHEET INTO WORKSPACE!");

                            datasetID = importResult.Datasets.FirstOrDefault().Id;
                            powerBIID = importResult.Reports.FirstOrDefault().Id;
                            filename = file.FileName;
                        }
                    }
                    else if (reportType == "powerbi" && fileCount==0) {
                        throw new ConflictException("Error", "File is required");
                    }

                    var model = new Report
                    {
                        Name = parseTextField(form, "Name"),
                        Description = parseTextField(form, "Description"),
                        ObjectType = objectType[0],
                        ObjectID = int.Parse(objectType[1]),
                        ReportLayoutID = parseNullableIntField(form, "ReportLayoutID", -1).GetValueOrDefault(-1),
                        ReportType = parseTextField(form, "ReportType"),
                        PowerBIReportID = string.IsNullOrEmpty(powerBIID) ? null : powerBIID,
                        PowerBIDatasetID = string.IsNullOrEmpty(datasetID) ? null : datasetID,
                        Url = parseTextField(form, "Url"),
                        ShowOnHomePage = showOnHomePage,
                        FileName = filename
                    };

                    var visibleTo = form["VisibleTo"];

                    if (!string.IsNullOrEmpty(visibleTo))
                    {
                        model.Responsibilities = new List<ReportResponsibility>();

                        var visibleToResponsibilityTypes = visibleTo.Split(',').Select(x => int.Parse(x));
                                                
                        //add any new responsibilities
                        foreach (var newResponsibilityType in visibleToResponsibilityTypes)
                        {
                            model.Responsibilities.Add(new ReportResponsibility
                            {
                                    ReportID = model.ID,
                                    ResponsibilityTypeID = newResponsibilityType
                            });                            
                        }
                    }

                    if (showOnHomePage)
                    {
                        var existing = Company.Filter<Report>(r => r.ShowOnHomePage).FirstOrDefault();
                        if (existing != null)
                        {
                            existing.ShowOnHomePage = false;
                            Company.Update(existing);
                        }
                    }

                    Company.Add(model);

                    return jsonSuccess("Dashboard successfully created", model.ID.ToString(), "add", HttpStatusCode.Created);
                }
                else
                {
                    throw new MissingPropertiesException("Report");
                }
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

        private static readonly string pbiUsername = ConfigurationManager.AppSettings["pbiUsername"];
        private static readonly string pbiPassword = ConfigurationManager.AppSettings["pbiPassword"];
      
        [HttpDelete, Route("DeleteReport")]
        public async Task<JsonResult> DeleteReport(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Report>(id);
                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                //delete any power bi reports
                if(model.ReportType == "powerbi" && !string.IsNullOrEmpty(model.PowerBIDatasetID))
                {
                    var companySettings = Community.GetCompanySettings();

                    var groupId = string.Empty;
                    var clientId = string.Empty;

                    companySettings.TryGetValue("PowerBIClientId", out clientId);
                    companySettings.TryGetValue("PowerBIGroupId", out groupId);
                    
                    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(groupId))
                        throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");
                    try
                    {
                        await PowerBI.DeleteDataset(pbiUsername, pbiPassword, clientId, groupId, model.PowerBIDatasetID);
                    }
                    catch { } // ok we cant delete the report delete the reference to it at least
                }

                Company.Delete(model);

                return jsonSuccess("Dashboard successfully deleted", id.ToString(), "delete", HttpStatusCode.OK);
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

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddPowerBICredentials")]
        public async Task<JsonResult> AddPowerBICredentials(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);
                
                //get username / password
                var user = parseTextField(form, "Username");
                var pwd = parseTextField(form, "Password");

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pwd))
                    throw new Exception("Please specify a valid username and password.");

                var companySettings = Community.GetCompanySettings();
                var groupId = string.Empty;
                var clientId = string.Empty;

                companySettings.TryGetValue("PowerBIClientId", out clientId);
                companySettings.TryGetValue("PowerBIGroupId", out groupId);

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(groupId))
                    throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");

                // if the workspace id is null create a new one and update the companysettings
                groupId = await checkPowerBIValidWorkspace(groupId, clientId);
                
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(groupId))
                    throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");

                //save password in this workspace for all ds's
                await PowerBI.UpdateConnectionCredentials(pbiUsername, pbiPassword, clientId, groupId, user, pwd);

                return jsonSuccess("Power BI Credentials successfully updated", "", "add", HttpStatusCode.Created);
                
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

        [HttpPut, ValidateInput(false), Route("EditReport")]
        public async Task<JsonResult> EditReport(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Report>(id);

                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                var fileCount = HttpContext.Request.Files.Count;
                var reportType = parseTextField(form, "ReportType");
                var name = parseTextField(form, "Name");
                var showOnHomePage = reportType == "legacy" ? false : parseBooleanField(form, "ShowOnHomePage");
                string powerBIID = string.Empty;
                string datasetID = string.Empty;
                string filename = string.Empty;
                string url = parseTextField(form, "Url");

                if (fileCount > 0 && reportType == "powerbi")
                {
                    var file = HttpContext.Request.Files[0];

                    if (file.ContentLength > 0)
                    {
                        var importResult = await uploadPowerBIReport(file, name, model.PowerBIDatasetID);

                        if (importResult.ImportState == "Failed")
                            throw new Exception("FAILED TO LOAD POWER BI WORKSHEET INTO WORKSPACE!");

                        datasetID = importResult.Datasets.FirstOrDefault().Id;

                        var rpt = importResult.Reports.FirstOrDefault();
                        
                        if (rpt != null)
                            powerBIID = rpt.Id;

                        filename = file.FileName;
                    }           
                }else if (reportType == "powerbi" && string.IsNullOrEmpty(model.FileName))
                {
                    throw new ConflictException("Error", "File is required");
                }

                var visibleTo = form["VisibleTo"];

                if (!string.IsNullOrEmpty(visibleTo))
                {
                    var visibleToResponsibilityTypes = visibleTo.Split(',').Select(x => int.Parse(x));

                    //delete any removed responsibilities
                    foreach (var responsibility in model.Responsibilities.ToList())
                    {
                        if (!visibleToResponsibilityTypes.Contains(responsibility.ResponsibilityTypeID))
                            Company.ReportResponsibilities.Remove(responsibility);
                                      
                    }

                    //add any new responsibilities
                    foreach (var newResponsibilityType in visibleToResponsibilityTypes)
                    {
                        if(!model.Responsibilities.Any(x=>x.ResponsibilityTypeID == newResponsibilityType))
                        {
                            model.Responsibilities.Add(new ReportResponsibility
                            {
                                ReportID = model.ID,
                                ResponsibilityTypeID = newResponsibilityType
                            });
                        }
                    }
                }
                else
                {
                    foreach (var responsibility in model.Responsibilities.ToList())
                    {
                        Company.ReportResponsibilities.Remove(responsibility);
                    }
                }

                // Static fields
                var objectType = form["ObjectType"].Split('|').ToArray();

                if (objectType.Length == 2)
                {
                    model.Name = name;
                    model.Description = parseTextField(form, "Description");
                    model.ObjectType = objectType[0];
                    model.ObjectID = int.Parse(objectType[1]);
                    model.ReportLayoutID = parseNullableIntField(form, "ReportLayoutID", -1).GetValueOrDefault(-1);
                    model.ReportType = reportType;
                    model.Url = url;
                    model.ShowOnHomePage = showOnHomePage;

                    if (!string.IsNullOrEmpty(datasetID))
                        model.PowerBIDatasetID = datasetID;

                    if (!string.IsNullOrEmpty(powerBIID))
                        model.PowerBIReportID = powerBIID;

                    if (!string.IsNullOrEmpty(filename))
                        model.FileName = filename;

                    if (showOnHomePage)
                    {
                        var existing = Company.Filter<Report>(r => r.ShowOnHomePage).FirstOrDefault();
                        if (existing != null)
                        {
                            existing.ShowOnHomePage = false;
                            Company.Update(existing);
                        }
                    }

                    Company.Update<Report>(model);

                    return jsonSuccess("Dashboard successfully edited", id.ToString(), "edit", HttpStatusCode.OK);
                }
                else
                {
                    throw new MissingPropertiesException("Report");
                }                
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

        private async Task<string> checkPowerBIValidWorkspace(string groupId, string clientId)
        {
            groupId = (groupId ?? "").Trim();

             if (string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(clientId))
             {                
                var groupName = $"D3S{Company.CurrentCompanyID}";
                var res = await PowerBI.CreateWorkspace(pbiUsername, pbiPassword, clientId, groupName);

                var workspaceSetting = Community.Filter<CompanySetting>(i => i.SettingID == 56 && i.CompanyID == Company.CurrentCompanyID).FirstOrDefault();

                if (workspaceSetting == null)
                {
                    Community.Add<CompanySetting>(new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 56, Value = res.Id });
                }
                else
                 {
                     workspaceSetting.Value = res.Id;

                     Community.Update<CompanySetting>(workspaceSetting);
                 }

                 return res.Id;
             }

            return groupId;            
        }

        private async Task<Microsoft.PowerBI.Api.V2.Models.Import> uploadPowerBIReport(HttpPostedFileBase file, string name, string datasetId = "")
        {
            var companySettings = Community.GetCompanySettings();
            var groupId = string.Empty;
            var clientId = string.Empty;

            companySettings.TryGetValue("PowerBIClientId", out clientId);
            companySettings.TryGetValue("PowerBIGroupId", out groupId);

            if (string.IsNullOrEmpty(clientId))
                throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");

            // if the workspace id is null create a new one and update the companysettings
            groupId = await checkPowerBIValidWorkspace(groupId, clientId);
            
            // if an existing one exists delete it
            if (!string.IsNullOrEmpty(datasetId))
                await PowerBI.DeleteDataset(pbiUsername, pbiPassword, clientId, groupId, datasetId);


            return await PowerBI.ImportPbix(pbiUsername, pbiPassword, clientId, groupId, name, file.InputStream);
        }

        #endregion

        #endregion

        #region ReportTile
        
        #region Form Get/Post
        
        [HttpPost, AjaxValidateAntiForgeryToken,  ValidateInput(false), Route("AddReportTile")]
        public JsonResult AddReportTile(FormCollection form, bool isNg = false)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var model = new ReportTile
                {
                    Name = parseTextField(form, "Name"),
                    CommandText = parseTextField(form, isNg ? "CommandText" : "SqlStatement"),
                    ReportID = parseIntField(form, "ReportID"),
                    ContentAreaNumber = parseIntField(form, "ContentAreaNumber"),
                    ReportTileType = (ReportTileType)Enum.Parse(typeof(ReportTileType), form["ReportTileType"])
                };

                var sXml = XElement.Parse("<settings/>");
                switch (model.ReportTileType)
                {
                    case ReportTileType.Area:
                    case ReportTileType.Bar:
                    case ReportTileType.Line:
                        sXml.Add(new XElement("data", form["data"]));
                        sXml.Add(new XElement("display", form["display"]));
                        sXml.Add(new XElement("xaxis", form["xaxis"]));
                        break;
                    case ReportTileType.Pie:
                        sXml.Add(new XElement("data", form["data"]));
                        sXml.Add(new XElement("display", form["display"]));
                        break;
                }
                model.Settings = sXml.ToString();

                var valid = Company.IsValidReportingQuery(model.CommandText);
                if (valid)
                {
                    Company.Add(model);
                }
                else
                {
                    throw new InvalidFieldException("Command Text", "not a SELECT statement or recognized query.");
                }

                return jsonSuccess(FormInfo.Add_ReportTile_Confirmation, model.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteReportTile")]
        public JsonResult DeleteReportTile(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReportTile>(id);
                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);
                Company.Delete(model);

                return jsonSuccess(FormInfo.Delete_ReportTile_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditReportTile")]
        public JsonResult EditReportTile(FormCollection form, bool isNg = false)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, isNg ? "ID" : "TileID");
                var model = Company.GetById<ReportTile>(id);

                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                // Static fields
                model.Name = parseTextField(form, "Name");
                model.CommandText = parseTextField(form, isNg ? "CommandText":"SqlStatement");
                model.ContentAreaNumber = parseIntField(form, "ContentAreaNumber");
                model.ReportTileType = (ReportTileType)Enum.Parse(typeof(ReportTileType), form["ReportTileType"]);

                var sXml = XElement.Parse("<settings/>");
                switch (model.ReportTileType)
                {
                    case ReportTileType.Area:
                    case ReportTileType.Bar:
                    case ReportTileType.Line:
                        sXml.Add(new XElement("data", form["data"]));
                        sXml.Add(new XElement("display", form["display"]));
                        sXml.Add(new XElement("xaxis", form["xaxis"]));
                        break;
                    case ReportTileType.Pie:
                        sXml.Add(new XElement("data", form["data"]));
                        sXml.Add(new XElement("display", form["display"]));
                        break;
                }
                model.Settings = sXml.ToString();

                var valid = Company.IsValidReportingQuery(model.CommandText);
                if (valid)
                {
                    Company.Update(model);
                }
                else
                {
                    throw new InvalidFieldException("Command Text", "not a SELECT statement or recognized query.");
                }

                return jsonSuccess(FormInfo.Edit_ReportTile_Confirmation, id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Resource

        string passwordRegex = Validation.Password_Regex;
        string passwordRegexMessage = Validation.Password_Requirements;

        #region Field Generation

        /// <param name="id">ResourceTypeID</param>
        [Route("Resource_AddFields"), NonNullableParameters]
        public JsonResult Resource_AddFields(int id)
        {
            var list = new List<EditableField>();
            
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var stateList = CompanyResourceState.Active.GetList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();

            list.Add(new EditableField { FieldName = "ResourceTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });//@"^([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})$", null, null, "be an email address") });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "Password", Name = "Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, Required = true, FieldName = "State", Name = "Active?", FieldType = DataType.Lookup.ToString(), Items = stateList, Value = ((int)CompanyResourceState.Active).ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ResourceType, id).ToList(), 5);

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <param name="id">ResourceID</param>
        [Route("Resource_EditFields"), NonNullableParameters]
        public JsonResult Resource_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Community.GetById<Resource>(id, i => i.CompanyResources);

            var stateList = CompanyResourceState.Active.GetList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            var cr = a.CompanyResources.Single(i => i.CompanyID == Company.CurrentCompanyID);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Value = a.Email, Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString(), Value = cr.IsAdministrator.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, Required = true, FieldName = "State", Name = "Active?", FieldType = DataType.Lookup.ToString(), Items = stateList, Value = ((int)cr.State).ToString() });

            list =(
                loadDynamicFields(
                    SystemObjects.Resource.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(), 
                    4
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Resource_EditMyInfoFields")]
        public JsonResult Resource_EditMyInfoFields()
        {
            var list = new List<EditableField>();
            var id = Company.CurrentResourceID;
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });

            list= (
                loadDynamicFields(
                    SystemObjects.Resource.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(), 
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Resource_ChangeMyPasswordFields")]
        public JsonResult Resource_ChangeMyPasswordFields()
        {
            var list = new List<EditableField>();
            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "CurrentPassword", Name = "Current Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Current Password", true, "", 7, 25) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "NewPassword", Name = "New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "New Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "ConfirmNewPassword", Name = "Confirm New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Confirm New Password", true, passwordRegex, null, null, passwordRegexMessage) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddResource")]
        public JsonResult AddResource(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("resource");

                int typeID = 1;
                var email = form["Email"].Trim();

                var a = Community.Filter<Resource>(i => i.Email == email).FirstOrDefault();

                var id = 0;

                // Only add resource account if it does not already exist.
                if (a == null)
                {
                    a = new Resource
                    {
                        ResourceTypeID = typeID,
                        FirstName = parseNameField(form, "FirstName"),
                        LastName = parseNameField(form, "LastName"),
                        Email = parseTextField(form, "Email"),
                        Username = parseTextField(form, "Email"),
                        Password = "temp"
                    };

                    Community.Add(a);

                    id = a.ID;
                    Community.ChangePassword(a.ID, "", form["Password"]);
                }
                else
                {
                    id = a.ID;
                    var globalResource = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();
                    if (globalResource != null && globalResource.State != CompanyResourceState.Deleted)
                    {
                        throw new ConflictException("Error", "The specified email address / username is already in use.");
                    }
                }

                var firstName = parseNameField(form, "FirstName");
                var lastName = parseNameField(form, "LastName");
                var isAdmin = parseBooleanField(form, "IsAdministrator");
                var state = parseEnumField<CompanyResourceState>(form, "State");
                var companyResource = Community.Filter<CompanyResource>(i => i.CompanyID == Community.CurrentCompanyID && i.ResourceID == id).FirstOrDefault();

                if (companyResource == null)
                {
                    companyResource = new CompanyResource
                    {
                        CompanyID = Company.CurrentCompanyID,
                        IsAdministrator = isAdmin,
                        ResourceID = id,
                        State = state                        
                    };
                    Community.Add(companyResource);
                }
                else
                {
                    companyResource.IsAdministrator = isAdmin;
                    companyResource.State = state;
                    Community.Update(companyResource);
                }

                if (!GetCompanyResources().Any(i => i.ResourceID == a.ID))
                {
                    GlobalReportingResource gr = new GlobalReportingResource
                    {
                        IsAdministrator = isAdmin,
                        ResourceID = id,
                        Email = a.Email,
                        LastName = lastName,
                        FirstName = firstName,
                        State = state,
                        UpdatedOn = DateTime.UtcNow,
                        Uid = a.Uid
                    };

                    Company.Add(gr);
                }
                else
                {
                    GlobalReportingResource gr = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();

                    gr.FirstName = firstName;
                    gr.LastName = lastName;
                    gr.Email = a.Email;
                    gr.IsAdministrator = isAdmin;
                    gr.State = state;
                    gr.UpdatedOn = DateTime.UtcNow;

                    Company.Update(gr);
                }
                
                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, a.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, typeID).ToList(), form, Server);
                Company.AddOrUpdateFields(fields);

                return jsonSuccess("User successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created);
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

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("ResetResourcePassword")]
        public JsonResult ResetResourcePassword(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);


                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null) throw new NotFoundException("resource");

                //valid user at this point generate a password
                ResetResourcePassword(model.ID, model.FirstName, model.Email, model.FormatDisplayName());
                
                return jsonSuccess("Users password has been successfully updated!", id.ToString(), "reset", HttpStatusCode.OK);

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

        [HttpDelete, Route("DeleteResource")]
        public JsonResult DeleteResource(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");

                if (id <= 0) throw new NotFoundException("Resource with ID less than or equal to 0 cannot be removed.");

                var model = Community.Filter<CompanyResource>(i => i.ResourceID == id && i.CompanyID == Company.CurrentCompanyID).SingleOrDefault();
                var globalResource = Company.Filter<GlobalReportingResource>(x => x.ResourceID == id).SingleOrDefault();
                if (model == null) throw new NotFoundException("resource");
                if (globalResource == null) throw new NotFoundException("resource");
                model.State = CompanyResourceState.Deleted;
                globalResource.State = CompanyResourceState.Deleted;

                Community.Update<CompanyResource>(model);
                Company.Update<GlobalReportingResource>(globalResource);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteResourceByID"), NonNullableParameters]
        public JsonResult DeleteResourceByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteResource(form);
        }

        [HttpPut, ValidateInput(false), Route("EditResource")]
        public JsonResult EditResource(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null) throw new NotFoundException("resource");

                if (id <= 0) throw new NotFoundException("Resource with ID less than or equal to 0 cannot be removed.");

                var newEmail = parseTextField(form, "Email");
                                
                if (string.IsNullOrEmpty(newEmail)) throw new NoFormDataException("Resource doesnt have a valid email / username specified.");

                //we need to compare the new email to the old email.  If they are different, we need to check if the new email already exists for another user
                // if the username is already in use we should throw an error to prevent this from happening as the other account should be updated
                if (string.Compare(newEmail, model.Username, true) != 0)
                {
                    //check if the resource already exists in community
                    var a = Community.Filter<Resource>(i => i.Email == newEmail).FirstOrDefault();

                    if (a != null) throw new Exception("Cannot update the user.  The specified email address / username is already in use.");
                }

                // Static fields
                model.FirstName = parseNameField(form, "FirstName");
                model.LastName = parseNameField(form, "LastName");
                model.Email = newEmail;
                model.Username = newEmail;
                model.UpdatedOn = DateTime.UtcNow;

                Community.Update(model);    //Must be first before saving fields.

                var cr = Community.Filter<CompanyResource>(i => i.ResourceID == id && i.CompanyID == Company.CurrentCompanyID).SingleOrDefault();
                if (cr != null)
                {
                    cr.State = parseEnumField<CompanyResourceState>(form, "State");
                    cr.IsAdministrator = parseBooleanField(form, "IsAdministrator");
                    Community.Update(cr);
                }

                GlobalReportingResource gr = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();

                gr.FirstName = model.FirstName;
                gr.LastName = model.LastName;
                gr.Email = model.Email;
                gr.IsAdministrator = cr.IsAdministrator;
                gr.State = cr.State;
                gr.UpdatedOn = DateTime.UtcNow;

                Company.Update(gr);

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);
                                

                return jsonSuccess("Resource successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditMyInfo")]
        public JsonResult EditMyInfo(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var model = Community.GetById<Resource>(Company.CurrentResourceID);

                if (model == null) throw new NotFoundException("resource");

                // Static fields
                model.FirstName = parseNameField(form, "FirstName");
                model.LastName = parseNameField(form, "LastName");
                model.UpdatedOn = DateTime.UtcNow;

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                Community.Update<Resource>(model);

                return jsonSuccess("Info successfully updated.", Company.CurrentResourceID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("ChangeMyPassword")]
        public JsonResult ChangeMyPassword(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var model = Community.GetById<Resource>(Company.CurrentResourceID);

                if (model == null) throw new NotFoundException("resource");

                var currentpassword = parseTextField(form, "CurrentPassword");
                var password1 = parseTextField(form, "NewPassword");
                var password2 = parseTextField(form, "ConfirmNewPassword");
                                
                if (!password1.Equals(password2))
                {
                    throw new ConflictException("Password values do not match", "Password values do not match.  Please try again.");
                }

                Community.ChangePassword(Company.CurrentResourceID, currentpassword, password1);

                return jsonSuccess("Password successfully updated.", Company.CurrentResourceID.ToString(), "edit", HttpStatusCode.OK);
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
                return jsonException("The specified cart could not be found", HttpStatusCode.NotFound);

            if (myCart.ResourceID != Company.CurrentResourceID)
                return jsonException("You do not have permission to add items to this cart", HttpStatusCode.Forbidden);

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
                return jsonException("Could not find the shopping cart specified.", HttpStatusCode.NotFound);

            if (cart.ResourceID != Company.CurrentResourceID)
                return jsonException("You do not have permission to remove this item", HttpStatusCode.Forbidden);

            var item = Company.ShoppingCartItems.Where(i => i.ShoppingCartID == shoppingCartID && i.Object == type && i.ObjectID == id).FirstOrDefault();
            if (item == null)
                return jsonException("Shopping cart item could not be found", HttpStatusCode.NotFound);

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

            if (cart.ResourceID > 0)
                cart.Requestor = Company.Query<string>("select FirstName + ' ' + LastName as Requestor from reporting.Global_Resource where ResourceID = @id", new { id = cart.ResourceID }).SingleOrDefault();

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
                cart.Requestor = Company.Query<string>("select FirstName + ' ' + LastName as Requestor from reporting.Global_Resource where ResourceID = @id", new { id = cart.ResourceID }).SingleOrDefault();


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
                return jsonException("Could not find shopping cart", HttpStatusCode.NotFound);

            if (myCart.ResourceID != Company.CurrentResourceID)
                return jsonException("You do not have permission to request this shopping cart.", HttpStatusCode.Forbidden);

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
                    return jsonException("Could not find the specified cart.", HttpStatusCode.NotFound);

                if (cart.ResourceID != Company.CurrentResourceID)
                    return jsonException("You do not have permission to clear this cart.", HttpStatusCode.Forbidden);

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
        public JsonResult AddShortcut(Shortcut shortcut)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException("You do not have permission to edit shortcuts.", HttpStatusCode.Forbidden);
            if (string.IsNullOrEmpty(shortcut.Name))
                return jsonException("This shortcut requires a name", HttpStatusCode.BadRequest);            
            if (string.IsNullOrEmpty(shortcut.Icon) && string.IsNullOrEmpty(shortcut.IconPayload))
                return jsonException("This shortcut is missing an icon", HttpStatusCode.BadRequest);

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
                        Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                        shortcut.IconUrl = $"{imageFileName}";

                    }
                }

                var MaxDisplayShortcut = Company.Shortcuts.OrderByDescending(o => o.DisplayOrder).FirstOrDefault();
                shortcut.DisplayOrder = (MaxDisplayShortcut != null) ? MaxDisplayShortcut.DisplayOrder + 1 : 0;
                


                shortcut.Url += "";
                Company.Add(shortcut);
            }
            catch(Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }          

            return jsonSuccess("Shortcut added successfully", shortcut.ID.ToString(), "add", HttpStatusCode.OK);

        }

        [HttpPut, Route("shortcut/edit")]
        public JsonResult EditShortcut(Shortcut shortcut)
        {

            if (!Company.CurrentResourceIsAdmin)
                return jsonException("You do not have permission to edit shortcuts.", HttpStatusCode.Forbidden);

            var existing = Company.GetById<Shortcut>(shortcut.ID);

            if (existing == null)
                return jsonException($"The shortcut with id {shortcut.ID} could not be found.", HttpStatusCode.BadRequest);
            if (string.IsNullOrEmpty(shortcut.Name))
                return jsonException("This shortcut requires a name", HttpStatusCode.BadRequest);            
            if (string.IsNullOrEmpty(shortcut.Icon) && string.IsNullOrEmpty(shortcut.IconUrl) && string.IsNullOrEmpty(shortcut.IconPayload))
                return jsonException("This shortcut is missing an icon", HttpStatusCode.BadRequest);

            try
            {
                if (!string.IsNullOrEmpty(shortcut.IconPayload))
                {
                    //remove old icon
                    if (!string.IsNullOrEmpty(existing.IconUrl))
                    {
                        try
                        {
                            Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, new Uri(existing.IconUrl).Segments.Last());
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
                        Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                        shortcut.IconUrl = $"{imageFileName}";

                    }
                }
                else if (!string.IsNullOrEmpty(existing.IconUrl) && string.IsNullOrEmpty(shortcut.IconUrl))
                {
                    try
                    {
                        Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, new Uri(existing.IconUrl).Segments.Last());
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
        public JsonResult DeleteShortcut(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException("You do not have permission to edit shortcuts.", HttpStatusCode.Forbidden);

            var existing = Company.GetById<Shortcut>(id);

            if (existing == null)
                return jsonException($"The shortcut with the id {id} could not be found.", HttpStatusCode.BadRequest);

            try
            {
                if (!string.IsNullOrEmpty(existing.IconUrl))
                {
                    //delete the file
                    Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, new Uri(existing.FullURL).Segments.Last());
                }

                Company.Delete(existing);
            }
            catch(Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

            return jsonSuccess("Shortcut deleted successfully.", id.ToString(), "delete", HttpStatusCode.OK);
        }

        [HttpPut, Route("shortcut/Move")]
        public JsonNetResult MoveShortCut(int id,bool moveUp)
        {
            var success = true;
            var message = "";
            var direction = "";
            try
            {
                var shortcut = Company.GetById<Shortcut>(id);
                if (shortcut == null)
                    throw new Exception($"Shortcut Id ${id} not found");
                direction = moveUp ? "up" : "down";
                Shortcut adjacentShortcut = null;
                if (moveUp)
                {
                    adjacentShortcut = Company.Shortcuts.OrderByDescending(s => s.DisplayOrder).FirstOrDefault(s=> shortcut.DisplayOrder > s.DisplayOrder);
                }
                else
                {
                    adjacentShortcut = Company.Shortcuts.OrderBy(s => s.DisplayOrder).FirstOrDefault(s => shortcut.DisplayOrder < s.DisplayOrder);
                }
                
                if (adjacentShortcut == null)
                    throw new Exception($"Shortcut is already sorted to the "+(moveUp ? "top." : "bottom."));


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

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Value", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Value", true, "", 1, 100) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        [Route("Tag_EditFields"), NonNullableParameters]
        public JsonResult Tag_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Tag>(id);

            list.Add(new EditableField { FieldName = "uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Value", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Value, Validations = checkAndAddValidation("Text", "Value", true, "", 1, 100) });

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
        public JsonResult SynonymsOptions(int predicateId, string type, int typeId, string obj, int objId, string query = "")
        {
            query = query.Replace("_", "[_]").Replace("%", "[%]");
                        
            var items = Company.Query<dynamic>(QueryConstants.SynonymOptions, new { predicateId,  type = new Dapper.DbString { IsAnsi = true, Value = type.ToString(), IsFixedLength = true, Length = 50 }, @object = new Dapper.DbString { IsAnsi = true, Value = obj.ToString(), IsFixedLength = true, Length = 50 }, objectId = objId, typeId, query }).ToList();
            var typeIsSubject = true;
            if (items.Count > 0)
            {
                typeIsSubject = (bool)items[0].TargetingSubject;
            }

            var model = new
            {
                items,
                typeIsSubject
            };

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post


        [ HttpPost, AjaxValidateAntiForgeryToken, Route("AddNymAllocation")]
        public JsonResult AddNymAllocation(NymAllocationModel model)
        {
            try
            {
                if (!Company.HasAssetPermission(model.Object, model.ObjectID, Permission.ModifyRelationships))
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
        
        [ HttpPost, AjaxValidateAntiForgeryToken, Route("AddCustomSynonym")]
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

                if (!Company.HasAssetPermission(model.Object, model.ObjectID, Permission.ModifyAsset))
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

        [ HttpPost, AjaxValidateAntiForgeryToken, Route("AddSynonym")]
        public JsonResult AddSynonym(SynonymEditModel model)
        {
            try
            {
                if (!Company.HasAssetPermission(model.Type, model.ID, Permission.ModifyRelationships))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var synonymSegments = model.Synonym.Split('|');
                var subject = model.TypeIsSubject ? model.Type : (SystemObjects)Enum.Parse(typeof(SystemObjects), synonymSegments[0]);
                var subjectID = model.TypeIsSubject ? model.ID : int.Parse(synonymSegments[1]);
                var @object = !model.TypeIsSubject ? model.Type : (SystemObjects)Enum.Parse(typeof(SystemObjects), synonymSegments[0]);
                var objectID = !model.TypeIsSubject ? model.ID : int.Parse(synonymSegments[1]);

                if (subjectID == objectID) return jsonException("Cannot add a synonym that specifies the same object as the current object.", HttpStatusCode.Forbidden);

                var sSubject = subject.ToString();
                var sObject = @object.ToString();
                
                var subjectDetail = Company.GetObjectDetail(sSubject, subjectID);
                var objectDetail = Company.GetObjectDetail(sObject, objectID);

                if (subjectDetail != null && objectDetail != null)
                {
                    var intersectType = Company.Filter<IntersectType>(i =>
                        (
                        (i.Subject == subjectDetail.Type && i.SubjectID == subjectDetail.TypeID && i.Object == objectDetail.Type && i.ObjectID == objectDetail.TypeID) ||
                        (i.Subject == objectDetail.Type && i.SubjectID == objectDetail.TypeID && i.Object == subjectDetail.Type && i.ObjectID == subjectDetail.TypeID)
                        )
                        && i.PredicateID == model.PredicateID
                    ).SingleOrDefault();
                    var intersect = Company.AddIntersect(intersectType.ID, subject, subjectID, @object, objectID);

                    if (intersect == null)
                        throw new ApplicationException("Failed to create synonym relationship.");

                    return jsonSuccess("Synonym assigned.", intersect.ID.ToString(), "add", HttpStatusCode.Created, new { });
                }
                else
                {
                    return jsonException("Item not found.", HttpStatusCode.NotFound, "Item not found.");
                }
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

        [HttpDelete, Route("DeleteSynonym")]
        public JsonResult DeleteSynonym(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("synonym");
                var id = parseIntField(form, "ID");

                var detail = Company.GetById<Intersect>(id);

                if (detail == null)
                    throw new NullReferenceException("Intersect not found");

                if (!Company.HasAssetPermission(detail.Subject, detail.SubjectID, Permission.DeleteRelationships))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (detail != null)
                {
                    Company.Delete(detail);
                }

                dynamic custom = new
                {
                    Name = "Synonym",
                    Context = form["_context"]
                };

                return jsonSuccess("Synonym successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region UpdateDisplayValues

        [HttpPost, AjaxValidateAntiForgeryToken, Route("rebuildDisplayValues")]
        public JsonResult RebuildDisplayValues(string objectType, object[] param)
        {
            if(!Company.CurrentResourceIsAdmin) return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            Company.RebuildDisplayValuesRequest();

            return jsonSuccess("Rebuild request received and accepted.", "", "add", HttpStatusCode.Created);
        }

        #endregion

        #region UpdateAssetGraph

        [HttpPost, AjaxValidateAntiForgeryToken, Route("rebuildAssetGraph")]
        public JsonResult RebuildAssetGraph()
        {
            if (!Company.CurrentResourceIsAdmin) return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            Company.RebuildAssetGraphRequest();

            return jsonSuccess("Rebuild request received and accepted.", "", "add", HttpStatusCode.Created);
        }

        #endregion

        #region Export Templates

        private JsonResult ExportTemplate_EditFields(int id)
        {
            var template = Company.AssetTypeExportTemplates.Where(x => x.ID == id).FirstOrDefault();

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", Name = "ID", FieldType = DataType.Hidden.ToString(), Value = template.ID.ToString() });
            list.Add(new EditableField { FieldName = "IncludeFields", Name = "IncludeFields", FieldType = DataType.Hidden.ToString(), Value = (string.IsNullOrEmpty(template.IncludeFields) ? "" : template.IncludeFields.ToString()) });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Namespace", true, "", 1, 250), Value = template.Name });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = template.Description });
            var names = Enum.GetNames(typeof(ExportView)).Select(i => new SelectListItem { Text = i, Value = i, Selected = template.ExportViewType.ToString() == i }).ToList();

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "ExportViewType", Name = "List Arrangement", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = names });

            var types = Company.AssetTypes.Where(f => f.Class == AssetTypeClass.BusinessAsset || f.Class == AssetTypeClass.TechnicalAsset).Select(i => new SelectListItem { Text = i.Name, Value = i.uid.ToString(), Selected = template.AssetTypeID == i.ID }).OrderBy(x=>x.Text).ToList();
            
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "AssetTypeUID", Name = "Asset Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = types });

            list.Add(new EditableField { Row = 5, Column = 1, Required = true, FieldName = "IncludeUrl", Name = "Include Asset Url", FieldDescription = "", FieldType = DataType.Boolean.ToString() , Value = template.IncludeUrl.ToString()});
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "IncludeParent", Name = "Include Parent Name", FieldDescription = "", FieldType = DataType.Boolean.ToString(), Value = template.IncludeParent.ToString() });
            list.Add(new EditableField { Row = 7, Column = 1, Required = false, FieldName = "UsageNotes", Name = "Usage Notes", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = template.UsageNotes });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        private JsonResult ExportTemplate_AddFields()
        {
            var list = new List<EditableField>();
                        
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Namespace", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldDescription = "", FieldType = DataType.Text.ToString() });

            var names = Enum.GetNames(typeof(ExportView)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();
            
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "ExportViewType", Name = "List Arrangement", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = names});

            var types = Company.AssetTypes.Where(f => f.Class == AssetTypeClass.BusinessAsset || f.Class == AssetTypeClass.TechnicalAsset).Select(i => new SelectListItem { Text = i.Name, Value = i.uid.ToString() }).OrderBy(x=>x.Text).ToList();
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "AssetTypeUID", Name = "Asset Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = types });

            list.Add(new EditableField { Row = 5, Column = 1, Required = true, FieldName = "IncludeUrl", Name = "Include Asset Url", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "IncludeParent", Name = "Include Parent Name", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 7, Column = 1, Required = false, FieldName = "UsageNotes", Name = "Usage Notes", FieldDescription = "", FieldType = DataType.Text.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}