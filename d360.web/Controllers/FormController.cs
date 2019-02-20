using d360.core;
using d360.core.resources;
using d360.core.entities;
using d360.core.entities.Metric;
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
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Configuration;
using d360.core.helpers;

namespace d360.web.Controllers
{    
    [RoutePrefix("form"), Authorize, AiHandleError, NonNullableParameters]
    public class FormController : BaseController
    {
        #region DI

        IStorageProvider Storage;

        public FormController(CommunityContext community, CompanyContext company, ISecurityContextProvider secProvider, IStorageProvider storage)
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

            string iconText = "Tx";

            var words = objectName.Split(' ');
            if (words.Length > 1 && words[1].Length > 0)
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
                    ObjectType = type,
                    ObjectID = id,
                    IconBackColor = backColor,
                    IconForeColor = foreColor,
                    IconText = iconText
                };
                Company.Add<ObjectStyle>(style);
            }
            else
            {
                style.IconBackColor = backColor;
                style.IconForeColor = foreColor;
                style.IconText = iconText;
                Company.Update<ObjectStyle>(style);
            }
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
                case "RULEDIMENSION":
                    return RuleDimension_EditFields(oid);
                case "RULETYPE":
                    return RuleType_EditFields(oid);
                case "SERVICE":
                    return CustomAPIService_EditFields(oid);
                case "SURVEYTYPE":
                    return SurveyType_EditFields(oid);
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
                case "RULEDIMENSION":
                    return RuleDimension_AddFields();
                case "RULEIMPLEMENTATION":
                    return RuleImplementation_AddFields(objectID.GetValueOrDefault());
                case "RULETYPE":
                    return RuleType_AddFields();
                case "SERVICE":
                    return CustomAPIService_AddFields();
                case "SURVEYTYPE":
                    return SurveyType_AddFields();
                case "TAXONOMY":
                    return Taxonomy_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "VERSION":
                    return CustomAPIServiceEndpointVersion_AddFields(parentID.GetValueOrDefault());
                case "URI":
                    return CustomAPIVersionUri_AddFields(parentID.GetValueOrDefault());

            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditorrel/new/{objectType}/{objectID}/{targetType}/{targetID}")]
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
                case "ARTIFACT":
                    return EditArtifact(form);
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
                case "POLICY":
                    return EditPolicy(form);
                case "POLICYTYPELEVEL":
                    return EditPolicyTypeLevel(form);
                case "PREDICATE":
                    return EditPredicate(form);
                case "REFERENCEITEM":
                    return EditReferenceItem(form);
                case "REFERENCEITEMTYPE":
                    return EditReferenceItemType(form);
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
                case "RULE":
                    return EditRule(form);
                case "RULEDIMENSION":
                    return EditRuleDimension(form);
                case "RULEIMPLEMENTATION":
                    return EditRuleImplementation(form);
                case "RULETYPE":
                    return EditRuleType(form);
                case "SERVICE":
                    return EditService(form);
                case "SURVEYTYPE":
                    return EditSurveyType(form);
                case "TAXONOMY":
                    return EditTaxonomy(form);
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
                case "ARTIFACT":
                    return DeleteArtifact(form);
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
                case "PREDICATE":
                    return DeletePredicate(form);
                case "REFERENCEITEM":
                    return DeleteReferenceItem(form);
                case "REFERENCEITEMTYPE":
                    return DeleteReferenceItemType(form);
                case "REPORT":
                    return await DeleteReport(form);
                case "REPORTTILE":
                    return DeleteReportTile(form);
                case "RULE":
                    return DeleteRule(form);
                case "RULEDIMENSION":
                    return DeleteRuleDimension(form);
                case "RULETYPE":
                    return DeleteRuleType(form);
                case "POLICY":
                    return DeletePolicy(form);                
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
                case "TAXONOMY":
                    return DeleteTaxonomy(form);
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
                case "ARTIFACT":
                    return AddArtifact(form);
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
                case "POLICY":
                    return AddPolicy(form);
                case "POLICYTYPELEVEL":
                    return AddPolicyTypeLevel(form);
                case "PREDICATE":
                    return AddPredicate(form);
                case "REFERENCEITEM":
                    return AddReferenceItem(form);
                case "REFERENCEITEMTYPE":
                    return AddReferenceItemType(form);
                case "REPORT":
                    return await AddReport(form);
                case "REPORTTILE":
                    return AddReportTile(form, true);
                case "RESOURCE":
                    return AddResource(form);
                case "RULEDIMENSION":
                    return AddRuleDimension(form);
                case "RULEIMPLEMENTATION":
                    return AddRuleImplementation(form);
                case "RULETYPE":
                    return AddRuleType(form);
                case "SERVICE":
                    return AddService(form);
                case "RULE":
                    return AddRule(form);                
                case "SURVEYTYPE":
                    return AddSurveyType(form);
                case "TAXONOMY":
                    return AddTaxonomy(form);
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

            var type = Company.GetById<ArtifactType>(at);

            var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                i.Object == "ArtifactType" &&
                i.ObjectID == type.ID &&
                i.PredicateType.Value == PredicateType.InterTypeHierarchy
            ).SingleOrDefault();

            list.Add(new EditableField { FieldName = "ArtifactTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });

            if (intersectType != null)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                var parents = Company.Query<SelectListItem>($"select ObjectID as Value, DisplayValue as Text from AssetDetail where Type = 'ArtifactType' and TypeID = {intersectType.SubjectID}").OrderBy(i => i.Text).ToList();
                list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentID", Name = $"Parent {pluralize.Singularize(intersectType.SubjectName)}", FieldType = DataType.Lookup.ToString(), Value = ((p > 0) ? p.ToString() : null), Items = parents });
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
            var a = Company.GetById<Artifact>(id);
            
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            var parentType = Company.GetParentType(a.ArtifactTypeID, SystemObjects.ArtifactType);
            

            if (PluralCultureHelper.IsNeutralCultureEnglish())
            {
                if (parentType != null)
                {
                    var parent = Company.GetParentObject(a.ID, SystemObjects.Artifact);
                   

                    var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                    var parents = Company.Query<SelectListItem>($"select ObjectID as Value, DisplayValue as Text from AssetDetail where Type = 'ArtifactType' and TypeID = {parentType.ObjectID}").OrderBy(i => i.Text).ToList();
                    list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentID", Name = $"Parent {pluralize.Singularize(parentType.Name)}", FieldType = DataType.Lookup.ToString(), Value = ((parent != null) ? parent.ObjectID.ToString() : ""), Items = parents });
                }
            }

            list = (
                loadDynamicFields(
                    SystemObjects.Artifact.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.ArtifactType, a.ArtifactTypeID).ToList(), 
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

        [Route("AddArtifact"), HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false)]
        public JsonResult AddArtifact(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int typeID = parseIntField(form, "ArtifactTypeID");
                var type = Company.GetById<ArtifactType>(typeID);

                if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, typeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (type == null) throw new NotFoundException("artifact type");
                                
                var model = new Artifact {
                    ArtifactTypeID = typeID
                };

                int? parentId = parseNullableIntField(form, "ParentID");

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.ArtifactType, typeID).ToList();
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, model.ID, fieldTypes, form, Server);
                Company.SaveOrUpdate<Artifact>(model, fields, (parentId.HasValue ? parentId.Value : -1));
                processFormDynamicRelationshipFields(SystemObjects.ArtifactType, typeID, SystemObjects.Artifact, model.ID, fieldTypes, form);

                if (parentId.HasValue)
                {
                    if(!Company.AddObjectParentRelationship(SystemObjects.ArtifactType, type.ID, SystemObjects.Artifact, parentId.Value, model.ID))
                    {
                        return jsonException($"Parent intersect with could not be found.", HttpStatusCode.NotFound);
                    }                    
                }

                return jsonSuccess(type.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = model.ID });
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

        [Route("DeleteArtifact"), HttpDelete]
        public JsonResult DeleteArtifact(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                var id = parseIntField(form, "ID");

                if (!Company.HasAssetPermission(SystemObjects.Artifact, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.Artifact, id);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, new { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = id });
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

        [Route("EditArtifact"), HttpPut, ValidateInput(false)]
        public JsonResult EditArtifact(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                var id = parseIntField(form, "ID");

                if (!Company.HasAssetPermission(SystemObjects.Artifact, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var model = Company.GetById<Artifact>(id, i => i.ArtifactType);

                if (model == null) throw new NotFoundException("artifact");

                var sType = SystemObjects.Artifact.ToString();

                var parentID = parseIntField(form, "ParentID");

                if (parentID > 0)
                {
                    var intersect = Company.Filter<Intersect>(i => 
                        i.Subject == sType &&
                        i.Object == sType &&
                        i.ObjectID == model.ID &&
                        i.IntersectType.Predicate.Type == PredicateType.InterTypeHierarchy
                    ).SingleOrDefault();

                    if (intersect != null)
                    {
                        if (intersect.SubjectID != parentID)
                        {
                            intersect.SubjectID = parentID;
                            Company.Update(intersect);
                        }
                    }
                    else
                    {
                        var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                        i.Object == "ArtifactType" &&
                        i.ObjectID == model.ArtifactType.ID &&
                        i.PredicateType.Value == PredicateType.InterTypeHierarchy
                    ).SingleOrDefault();

                        if (intersectType != null)
                        {
                            var newIntersect = new Intersect
                            {
                                Subject = SystemObjects.Artifact.ToString(),
                                SubjectID = parentID,
                                Object = SystemObjects.Artifact.ToString(),
                                ObjectID = model.ID,
                                IntersectTypeID = intersectType.ID
                            };

                            var parentExists = Company.Any<Asset>(i =>
                                i.ObjectID == newIntersect.SubjectID &&
                                i.AssetType.Object == "ArtifactType" &&
                                i.AssetType.ObjectID == intersectType.SubjectID
                                );

                            if (!parentExists)
                            {
                                return jsonException($"Parent {intersectType.SubjectName} with ID {newIntersect.SubjectID} could not be found.", HttpStatusCode.NotFound);
                            }

                            Company.Add(newIntersect);
                        }
                    }
                }
                
                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.ArtifactType, model.ArtifactTypeID).ToList();
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, model.ID, fieldTypes, form, Server, false);
                Company.SaveOrUpdate<Artifact>(model, fields, (parentID > 0 ? parentID : -1));
                processFormDynamicRelationshipFields(SystemObjects.ArtifactType, model.ArtifactTypeID, SystemObjects.Artifact, model.ID, fieldTypes, form);
                
                return jsonSuccess(model.ArtifactType.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, new { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = id });
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

        [AjaxValidateAntiForgeryToken, HttpPost, Route("RequestCertification")]
        public JsonResult RequestCertification(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int id = parseIntField(form, "ID");
                var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);

                if (artifact == null) throw new NotFoundException("artifact");
                
                Company.RequestObjectCertification(SystemObjects.Artifact, artifact.ID, SystemObjects.ArtifactType, artifact.ArtifactTypeID);

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
                var model = Company.GetById<ArtifactType>(id);
                if (model == null) throw new NotFoundException("artifact type");

                if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var intersectType = Company.Filter<IntersectType>(i =>
                    i.Object == "ArtifactType" &&
                    i.ObjectID == model.ID &&
                    i.Predicate.Type == PredicateType.InterTypeHierarchy
                ).SingleOrDefault();

                if (intersectType != null)
                {
                    Company.Delete(SystemObjects.IntersectType, intersectType.ID);
                }

                Company.Delete(SystemObjects.ArtifactType, id);

                dynamic custom = new
                {                    
                    Name = model.Name,
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
                    case AssetTypeClass.Glossary:
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

                    model = new AssetTypeEditorModel
                    {
                        AssetType = assetType,
                        IconBackColor = ((style != null) ? style.IconBackColor : "#000"),
                        IconForeColor = ((style != null) ? style.IconForeColor : "#FFF"),
                        Tokens = Company.Filter<FieldType>(i => i.Object == assetType.Object && i.ObjectID == assetType.ObjectID && !this.limitedFieldTypes.Contains(i.Type)).OrderBy(i => i.FriendlyName).Select(i => new PrimeSelectItem { label = i.FriendlyName, value = "{" + i.Name + "}" }).ToList()
                    };
                    
                    switch (@class)
                    {
                        case AssetTypeClass.FusionAttribute:
                            var f = Company.GetById<FusionAttributeType>(model.AssetType.ObjectID);
                            model.AssetType.Name = f.Name;
                            model.ScanEnabled = f.ScanEnabled;
                            break;
                        case AssetTypeClass.Glossary:
                            var a = Company.GetById<ArtifactType>(model.AssetType.ObjectID);
                            model.CanOwnFusion = a.CanOwnFusion;
                            model.AutoDisplayDescription = assetType.AutoDisplayDescription;
                            model.AssetType.Name = a.Name;
                            model.AssetType.Description = a.Description;
                            model.AssetType.DisplayFormat = a.DisplayFormat;
                            break;
                        case AssetTypeClass.Model:
                            var t = Company.GetById<TaxonomyType>(model.AssetType.ObjectID);
                            model.AssetType.HierarchyMaximumDepth = t.MaximumDepth ?? 1;
                            model.AssetType.Name = t.Name;
                            model.AssetType.Description = t.Description;
                            model.AssetType.DisplayFormat = t.DisplayFormat;
                            break;
                        case AssetTypeClass.Organization:
                            var o = Company.GetById<OrganizationType>(model.AssetType.ObjectID);
                            model.AssetType.HierarchyMaximumDepth = 1;
                            model.AssetType.Name = o.Name;
                            model.AssetType.Description = o.Description;
                            model.AssetType.DisplayFormat = o.DisplayFormat;
                            break;
                        case AssetTypeClass.Policy:
                            var p = Company.GetById<PolicyType>(model.AssetType.ObjectID);
                            model.AssetType.HierarchyMaximumDepth = p.MaximumDepth ?? 1;
                            model.AssetType.Name = p.Name;
                            model.AssetType.Description = p.Description;
                            model.AssetType.DisplayFormat = p.DisplayFormat;
                            break;
                        case AssetTypeClass.ReferenceItemType:
                            var r = Company.GetById<ReferenceItemType>(model.AssetType.ObjectID);
                            model.AssetType.Name = r!= null ? r.Name : "";
                            model.AssetType.Notes = r.SourceNotes;
                            if (model.Tokens != null) model.Tokens.Add(new PrimeSelectItem { label = "Code", value = "{Code}" });
                            break;
                    }
                    model.AssetType.Object = ot.ToString();
                    model.FormName = string.Format(FormInfo.Add_Asset_Type_Title, appendTitle);
                    model.FormDescription = string.Format(FormInfo.Add_Asset_Type_Directions, appendTitle.ToLower());

                    if (@class == AssetTypeClass.FusionAttribute || @class == AssetTypeClass.Glossary || @class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.ReferenceItemType)
                    {
                        var intersectType = Company.Filter<IntersectType>(i =>
                            i.Object == assetType.Object &&
                            i.ObjectID == assetType.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).FirstOrDefault();


                        if (@class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.ReferenceItemType) //If model or policy you must always have a predicate to load.
                            loadPredicates = true;

                        if (intersectType != null)
                        {
                            loadPredicates = true;
                            model.ParentID = intersectType.SubjectID;
                            model.SelectedPredicateID = intersectType.PredicateID;
                        }
                    }
                }
                else
                {
                    loadPredicates = true;
                    model = new AssetTypeEditorModel
                    {
                        AssetType = new AssetType { DisplayFormat = "{Name}", Class = @class, Object = ot.ToString() },
                        IconBackColor = "#000",
                        IconForeColor = "#FFF",
                        SelectedPredicateID = null,
                        ParentID = parentID,
                        Tokens = new List<PrimeSelectItem>() { new PrimeSelectItem { label = "Name", value = "{Name}" } }
                    };

                    if(@class == AssetTypeClass.ReferenceItemType) model.Tokens.Add(new PrimeSelectItem { label = "Code", value = "{Code}" });
                    model.FormName = string.Format(FormInfo.Edit_Asset_Type_Title, appendTitle);
                    model.FormDescription = string.Format(FormInfo.Add_Asset_Type_Directions, appendTitle.ToLower());
                }

                if (loadPredicates)
                {
                    model.Predicates = Company.Filter<Predicate>(i => i.Type == parentPredicateType).Select(i => new PrimeSelectItem { label = i.Inverse, value = i.ID.ToString() }).ToList();
                }

                if (loadParentReferenceItemOptions)
                {
                    if (model.AssetType != null && model.AssetType.ObjectID > 0)
                    {
                        var parents = Company.Query<PrimeSelectItem>(@"select a.ObjectID as value, a.Name as label from  assettype a where a.[object] = 'ReferenceItemType'  and a.objectid != @id
                                                                    and  not exists(
                                                                    select  1 from IntersectType i where i.object = 'ReferenceItemType' and i.SubjectId = @id and i.objectid = a.objectid)
                                                                    order by Name", new { id = model.AssetType.ObjectID }).ToList();
                        model.Parents = parents;
                    }
                    else
                    {
                        var parents = Company.Query<PrimeSelectItem>("select ObjectID as value, Name as label from assettype where [object] = 'ReferenceItemType' order by Name").ToList();
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("AssetType"), Route("AssetType")]
        public JsonResult PostAssetType(AssetTypeEditorModel model)
        {
            try
            {
                var isNamePartOfKey = true;
                var nameFriendlyName = "Name";
                SystemObjects ot;
                var parentType = SystemObjects.ArtifactType;

                if (!Enum.TryParse<SystemObjects>(model.AssetType.Object, out ot))
                    throw new GenericException(HttpStatusCode.BadRequest, "Missing Object Type", "No valid type provided. Please check your request and try again.");

                if (!Company.CurrentResourceIsAdmin)
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Add, FormInfo.Permisions_Error_Add);

                switch (ot)
                {
                    case SystemObjects.ArtifactType:
                        #region
                        var a = new ArtifactType
                        {
                            Name = model.AssetType.Name,
                            DisplayFormat = model.AssetType.DisplayFormat,
                            Description = model.AssetType.Description,
                            CanOwnFusion = model.CanOwnFusion ?? false,
                        };
                        Company.Add(a);
                        parentType = SystemObjects.ArtifactType;
                        model.AssetType.ObjectID = a.ID;
                        #endregion
                        break;
                    case SystemObjects.FusionAttributeType:
                        #region
                        if (!model.TopLevelTypeID.HasValue)
                        {
                            throw new GenericException(HttpStatusCode.BadRequest, "Missing Fusion Type", "No valid fusion type provided. Please check your request and try again.");
                        }
                        var f = new FusionAttributeType
                        {
                            Name = model.AssetType.Name,
                            ScanEnabled = model.ScanEnabled ?? true,
                            ParentID = model.ParentID,
                            FusionTypeID = model.TopLevelTypeID.Value
                        };
                        Company.Add(f);
                        parentType = SystemObjects.FusionAttributeType;
                        model.AssetType.ObjectID = f.ID;
                        #endregion
                        break;
                    case SystemObjects.OrganizationType:
                        #region
                        var org = new OrganizationType
                        {
                            Name = model.AssetType.Name,
                            Description = model.AssetType.Description,
                            DisplayFormat = model.AssetType.DisplayFormat
                        };
                        var existing = Company.Filter<OrganizationType>(o => o.Name == org.Name && o.State == State.Active).FirstOrDefault();
                        if (existing != null)
                            return jsonException("There is already an organization type with that name.", HttpStatusCode.BadRequest);
                        Company.Add(org);
                        parentType = SystemObjects.OrganizationType;
                        model.AssetType.ObjectID = org.ID;
                        #endregion
                        break;
                    case SystemObjects.PolicyType:
                        #region
                        var p = new PolicyType
                        {
                            Name = model.AssetType.Name,
                            DisplayFormat = model.AssetType.DisplayFormat,
                            Description = model.AssetType.Description,
                            MaximumDepth = model.AssetType.HierarchyMaximumDepth,
                        };
                        Company.Add(p);
                        parentType = SystemObjects.PolicyType;
                        model.AssetType.ObjectID = p.ID;
                        #endregion
                        break;
                    case SystemObjects.TaxonomyType:
                        #region
                        var t = new TaxonomyType
                        {
                            Name = model.AssetType.Name,
                            DisplayFormat = model.AssetType.DisplayFormat,
                            Description = model.AssetType.Description,
                            MaximumDepth = model.AssetType.HierarchyMaximumDepth,
                        };

                        if (t.MaximumDepth <= 0 || t.MaximumDepth > 10)
                            throw new GenericException(HttpStatusCode.BadRequest, "Invalid Maximum Level", "Invalid Maximum Model level specified must be a value between 1 and 10");

                        Company.Add(t);

                        for (int i = 1; i <= t.MaximumDepth; i++)
                        {
                            Company.Set<TaxonomyTypeLevel>().Add(new TaxonomyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), TaxonomyTypeID = t.ID });
                        }
                        Company.SaveChanges();
                        
                        parentType = SystemObjects.TaxonomyType;
                        model.AssetType.ObjectID = t.ID;
                        #endregion
                        break;
                    case SystemObjects.ReferenceItemType:
                        #region
                        var rt = new ReferenceItemType
                        {
                            Name = model.AssetType.Name,
                            DisplayFormat = model.AssetType.DisplayFormat,
                            Description = model.AssetType.Description,     
                            SourceNotes = model.AssetType.Notes
                        };
                        isNamePartOfKey = false;
                        nameFriendlyName = "Long Description";
                        Company.Add(rt);
                        parentType = SystemObjects.ReferenceItemType;
                        model.AssetType.ObjectID = rt.ID;
                        #endregion
                        break;                        
                }

                if (model.SelectedPredicateID.HasValue)
                {
                    var intersectType = new IntersectType
                    {
                        Subject = parentType.ToString(),
                        SubjectID = (model.ParentID.HasValue && model.ParentID.Value > 0) ? model.ParentID.Value : model.AssetType.ObjectID,
                        SubjectCardinality = Cardinality.One,
                        Object = model.AssetType.Object,
                        ObjectID = model.AssetType.ObjectID,
                        ObjectCardinality = Cardinality.Many,
                        PredicateID = model.SelectedPredicateID
                    };
                    Company.Add(intersectType);
                }

                upsertObjectStyle(model.AssetType.Object, model.AssetType.ObjectID, model.IconForeColor, model.IconBackColor, model.AssetType.Name);

                dynamic custom = new
                {
                    ParentID = model.ParentID,
                    Name = model.AssetType.Name,
                    action = "add"
                };

                if (model.AssetType.ObjectID > 0)
                {
                    if (model.AssetType.Class != AssetTypeClass.FusionAttribute && model.AssetType.Class != AssetTypeClass.Organization)
                    {
                        Company.Add(new FieldType
                        {
                            ObjectID = model.AssetType.ObjectID,
                            Object = model.AssetType.Object,
                            IsListable = true,
                            IsRequired = true,
                            IsEditable = true,
                            FriendlyName = nameFriendlyName,
                            Name = "Name",
                            MaximumLength = 500,
                            MinimumLength = 1,
                            SortOrder = 1,
                            Type = DataType.Text.ToString(),
                            IsDisplayable = true,
                            IsPartOfKey = isNamePartOfKey
                        });
                    }
                }

                return jsonSuccess(model.AssetType.Name + " successfully created.", model.AssetType.ObjectID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpPut, ActionName("AssetType"), Route("AssetType")]
        public JsonResult PutAssetType(AssetTypeEditorModel model)
        {
            try
            {
                SystemObjects ot;
                var parentType = SystemObjects.ArtifactType;
                bool shouldRemoveOldRelationshipType = false;
                bool shouldRemoveExistingParentChildRelationshipType = false;

                if (!Enum.TryParse<SystemObjects>(model.AssetType.Object, out ot))
                    throw new GenericException(HttpStatusCode.BadRequest, "Missing Object Type", "No valid type provided. Please check your request and try again.");

                if (!Company.HasAssetTypePermission(ot, model.AssetType.ObjectID, Permission.ModifyAsset))
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Edit, FormInfo.Permisions_Error_Edit);

                switch (ot)
                {
                    case SystemObjects.ArtifactType:
                        var a = Company.GetById<ArtifactType>(model.AssetType.ObjectID);
                        if (a == null) throw new NotFoundException("artifact type");

                        a.Name = model.AssetType.Name;
                        a.DisplayFormat = model.AssetType.DisplayFormat;
                        a.Description = model.AssetType.Description;
                        a.CanOwnFusion = model.CanOwnFusion ?? false;
                        a.AutoDisplayDescription = model.AutoDisplayDescription ?? false;

                        Company.Update(a);

                        parentType = SystemObjects.ArtifactType;
                        break;
                    case SystemObjects.FusionAttributeType:
                        var f = Company.GetById<FusionAttributeType>(model.AssetType.ObjectID);
                        if (f == null) throw new NotFoundException("fusion attribute type");

                        f.Name = model.AssetType.Name;
                        f.ScanEnabled = !(model.ScanEnabled == null);

                        Company.Update(f);

                        parentType = SystemObjects.FusionAttributeType;
                        break;
                    case SystemObjects.OrganizationType:
                        var org = Company.GetById<OrganizationType>(model.AssetType.ObjectID);
                        if (org == null) throw new NotFoundException("organization type");

                        org.Name = model.AssetType.Name;
                        org.Description = model.AssetType.Description;
                        org.DisplayFormat = model.AssetType.DisplayFormat;
                        Company.Update(org);

                        parentType = SystemObjects.OrganizationType;
                        break;
                    case SystemObjects.PolicyType:
                        var p = Company.GetById<PolicyType>(model.AssetType.ObjectID);
                        if (p == null) throw new NotFoundException("policy type");

                        p.Name = model.AssetType.Name;
                        p.DisplayFormat = model.AssetType.DisplayFormat;
                        p.Description = model.AssetType.Description;
                        p.MaximumDepth = model.AssetType.HierarchyMaximumDepth;

                        Company.Update(p);

                        parentType = SystemObjects.PolicyType;
                        break;
                    case SystemObjects.ReferenceItemType:
                        var rt = Company.GetById<ReferenceItemType>(model.AssetType.ObjectID);
                        if (rt == null) throw new NotFoundException("reference item type");

                        rt.Name = model.AssetType.Name;
                        rt.DisplayFormat = model.AssetType.DisplayFormat;
                        rt.Description = model.AssetType.Description;
                        rt.SourceNotes = model.AssetType.Notes;

                        Company.Update(rt);

                        shouldRemoveOldRelationshipType = true;
                        shouldRemoveExistingParentChildRelationshipType = true;
                        parentType = SystemObjects.ReferenceItemType;
                        break;
                    case SystemObjects.TaxonomyType:
                        var t = Company.GetById<TaxonomyType>(model.AssetType.ObjectID, i => i.TaxonomyTypeLevels);
                        if (t == null) throw new NotFoundException("model type");

                        t.Name = model.AssetType.Name;
                        t.DisplayFormat = model.AssetType.DisplayFormat;
                        t.Description = model.AssetType.Description;
                        t.MaximumDepth = model.AssetType.HierarchyMaximumDepth;

                        if (t.MaximumDepth <= 0 || t.MaximumDepth > 10)
                            throw new GenericException(HttpStatusCode.BadRequest, "Invalid Maximum Level", "Invalid Maximum Model level specified must be a value between 1 and 10");
                        
                        Company.Update(t);

                        for (int i = 1; i <= t.MaximumDepth; i++)
                        {
                            var level = t.TaxonomyTypeLevels.SingleOrDefault(l => l.Level == i);
                            if (level == null)
                            {
                                Company.Set<TaxonomyTypeLevel>().Add(new TaxonomyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), TaxonomyTypeID = t.ID });
                            }
                        }
                        Company.Delete<TaxonomyTypeLevel>(l => l.Level > t.MaximumDepth);
                        Company.SaveChanges();

                        parentType = SystemObjects.TaxonomyType;
                        break;
                }

                upsertObjectStyle(model.AssetType.Object, model.AssetType.ObjectID, model.IconForeColor, model.IconBackColor, model.AssetType.Name);

                if (model.ParentID.HasValue || model.SelectedPredicateID.HasValue)
                {
                    var parentPredicateType = PredicateType.InterTypeHierarchy;

                    if (model.AssetType.Class == AssetTypeClass.Model || model.AssetType.Class == AssetTypeClass.Policy)
                    {
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                    }

                    IntersectType intersectType = null;

                    if (shouldRemoveExistingParentChildRelationshipType)
                    {
                        intersectType = Company.Filter<IntersectType>(i =>
                            i.Subject == parentType.ToString() &&                            
                            i.Object == model.AssetType.Object &&
                            i.ObjectID == model.AssetType.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).SingleOrDefault();
                    }
                    else
                    {
                        intersectType = Company.Filter<IntersectType>(i =>
                            i.Subject == parentType.ToString() &&
                            i.SubjectID == (model.ParentID.HasValue ? model.ParentID : model.AssetType.ObjectID) &&
                            i.Object == model.AssetType.Object &&
                            i.ObjectID == model.AssetType.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).SingleOrDefault();
                    }

                    if (model.SelectedPredicateID.HasValue)
                    {
                        if (intersectType != null)
                        {
                            if (intersectType.PredicateID != model.SelectedPredicateID)
                            {
                                intersectType.PredicateID = model.SelectedPredicateID.Value;                                
                                Company.Update(intersectType);
                            }

                            var parentID = (model.ParentID.HasValue ? model.ParentID.Value : model.AssetType.ObjectID);

                            if (intersectType.SubjectID != parentID)
                            {
                                intersectType.SubjectID = parentID;
                                Company.Update(intersectType);
                            }
                        }
                        else
                        {
                            intersectType = new IntersectType {
                                IsSystem = true,
                                Subject = parentType.ToString(),
                                SubjectID = model.ParentID.HasValue ? model.ParentID.Value : model.AssetType.ObjectID,
                                Object = model.AssetType.Object,
                                ObjectID = model.AssetType.ObjectID,
                                PredicateID = model.SelectedPredicateID.Value
                            };
                            Company.Add(intersectType);
                        }
                    }
                }
                else if(shouldRemoveOldRelationshipType)
                {
                    var parentPredicateType = PredicateType.InterTypeHierarchy;

                    var intersectType = Company.Filter<IntersectType>(i =>                        
                        i.Object == model.AssetType.Object &&
                        i.ObjectID == model.AssetType.ObjectID &&
                        i.Predicate.Type == parentPredicateType
                    ).FirstOrDefault();

                    if(intersectType != null)
                    {
                        Company.Delete(SystemObjects.IntersectType, intersectType.ID);
                    }
                }

                dynamic custom = new
                {
                    ParentID = model.ParentID,
                    Name = model.AssetType.Name,
                    action = "edit"
                };

                //update affected display values
                Company.CreateOrUpdateTypeDisplayValuesAsync(model.AssetType.ObjectID, model.AssetType.Object.ToString());

                return jsonSuccess(model.AssetType.Name + " successfully updated.", model.AssetType.ObjectID.ToString(), "edit", HttpStatusCode.OK, custom);
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

        #region Attribute

        #region Field Generation

        /// <param name="at">AttributeTypeID</param>
        /// <param name="ot">ObjectType</param>
        /// <param name="oid">ObjectID</param>
        /// <param name="p">ParentID</param>
        [Route("Attribute_AddFields"), NonNullableParameters]
        public JsonResult Attribute_AddFields(int at, string ot, int oid, int p)
        {
            var list = new List<EditableField>();            

            list.Add(new EditableField { FieldName = "AttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });
            list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = ot });
            list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = oid.ToString() });
            list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.AttributeType, at).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeID</param>
        [Route("Attribute_EditFields"), NonNullableParameters]
        public JsonResult Attribute_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<d360.core.entities.Attribute>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list =(
                loadDynamicFields(
                    SystemObjects.Attribute.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.AttributeType, a.AttributeTypeID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Attribute, id).ToList(),
                    1)
                );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddAttribute")]
        public JsonResult AddAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("attribute");

                int typeID = parseIntField(form, "AttributeTypeID");
                var type = Company.GetById<AttributeType>(typeID);
                if (type == null) throw new NotFoundException("attribute type");

                var a = new d360.core.entities.Attribute
                {
                    AttributeTypeID = typeID,
                    ObjectType = form["ObjectType"],
                    ObjectID = parseIntField(form, "ObjectID")
                };

                if (!Company.HasAssetPermission(a.ObjectType, a.ObjectID, Permission.ModifyAttributes))
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Add, FormInfo.Permisions_Error_Add);

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    a.ParentID = parseIntField(form, "ParentID");
                    if (a.ParentID == 0) a.ParentID = null;
                }

                // Dynamic fields
                var loader = new FieldLoader();
                var fields = loader.GetFormDynamicFieldValues(SystemObjects.Attribute, a.ID, Company.GetFieldTypesByObject(SystemObjects.AttributeType, typeID).ToList(), form, Server);

                Company.SaveOrUpdate(a, fields);

                dynamic custom = new
                {
                    AttributeTypeID = typeID,
                    a.ObjectID,
                    Object = a.ObjectType,
                    ObjectType = "AttributeType",
                    ObjectTypeID = typeID,
                    ObjectTypeName = type.Name,
                    Name = Company.GetById<AttributeDetail>(a.ID).FormattedValue
                };

                return jsonSuccess(type.Name + " successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeleteAttributeById"), NonNullableParameters]
        public JsonResult DeleteAttributeById(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteAttribute(form);
        }

        [HttpDelete, Route("DeleteAttribute")]
        public JsonResult DeleteAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("attribute");

                var id = parseIntField(form, "ID");
                var attr = Company.GetById<d360.core.entities.Attribute>(id);
                if (attr == null)
                    throw new NotFoundException("attribute");

                if (!Company.HasAssetPermission(attr.ObjectType, attr.ObjectID, Permission.DeleteAttributes))
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Delete, FormInfo.Permisions_Error_Delete);

                Company.Delete(attr);

                return jsonSuccess(FormInfo.Delete_Attribute_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditAttribute")]
        public JsonResult EditAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("attribute");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<d360.core.entities.Attribute>(id);
                if (model == null) throw new NotFoundException("attribute");

                if (!Company.HasAssetPermission(model.ObjectType, model.ObjectID, Permission.ModifyAttributes))
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Edit, FormInfo.Permisions_Error_Edit);

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Attribute, model.ID, Company.GetFieldTypesByObject(SystemObjects.AttributeType, model.AttributeTypeID).ToList(), form, Server, false);

                Company.SaveOrUpdate(model, fields);

                dynamic custom = new
                {
                    model.AttributeTypeID,
                    model.ObjectID,
                    Object = model.ObjectType,
                    ObjectType = "AttributeType",
                    ObjectTypeID = model.AttributeTypeID,
                    ObjectTypeName = model.AttributeType.Name,
                    Name = Company.GetById<AttributeDetail>(id).FormattedValue
                };

                return jsonSuccess("Item successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        #region AttributeType

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddAttributeType")]
        public JsonResult AddAttributeType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_AttributeType);

                var a = new AttributeType
                {
                    Name = parseTextField(form, "Name"),
                    ShowNameInTree = parseBooleanField(form, "ShowNameInTree"),
                    Description = parseTextField(form, "Description"),
                    DisplayFormat = parseTextField(form, "DisplayFormat")
                };

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    a.ParentID = parseIntField(form, "ParentID");
                    if (a.ParentID == 0) a.ParentID = null;
                }

                if (!a.ParentID.HasValue)
                {
                    if (!string.IsNullOrEmpty(form["AttributeTypeCategoryID"]))
                    {
                        a.AttributeTypeCategoryID = parseIntField(form, "AttributeTypeCategoryID");
                        if (a.AttributeTypeCategoryID == 0) a.AttributeTypeCategoryID = null;
                    }
                }

                Company.SaveOrUpdate(a);

                return jsonSuccess(FormInfo.Add_AttributeType_Confirmation, a.ID.ToString(), "add", HttpStatusCode.Created, new { ParentID = a.ParentID, Name = a.Name });
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

        [HttpDelete, Route("DeleteAttributeType")]
        public JsonResult DeleteAttributeType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_AttributeType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<AttributeType>(id);
                if (model == null) throw new NotFoundException(FormInfo.NoFormData_AttributeType);

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                                
                Company.Delete(SystemObjects.AttributeType, id);

                return jsonSuccess(FormInfo.Delete_AttributeType_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditAttributeType")]
        public JsonResult EditAttributeType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_AttributeType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<AttributeType>(id);
                if (model == null) throw new NotFoundException(FormInfo.NoFormData_AttributeType);

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.ShowNameInTree = parseBooleanField(form, "ShowNameInTree");
                model.Description = parseTextField(form, "Description");
                model.DisplayFormat = parseTextField(form, "DisplayFormat");

                if (!model.ParentID.HasValue)
                {
                    if (!string.IsNullOrEmpty(form["AttributeTypeCategoryID"]))
                    {
                        model.AttributeTypeCategoryID = parseIntField(form, "AttributeTypeCategoryID");
                        if (model.AttributeTypeCategoryID == 0) model.AttributeTypeCategoryID = null;
                    }
                }

                Company.SaveOrUpdate(model);

                return jsonSuccess(FormInfo.Edit_AttributeType_Confirmation, id.ToString(), "edit", HttpStatusCode.OK, new { model.ParentID, model.Name });
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

        #region AttributeTypeRelation

        #region Field Generation

        /// <param name="at">AttributeTypeID</param>
        [Route("AttributeTypeRelation_AddFields"), NonNullableParameters]
        public JsonResult AttributeTypeRelation_AddFields(int at)
        {
            var list = new List<EditableField>();            

            var relation = new AttributeTypeRelation();
            
            list.Add(new EditableField { FieldName = "AttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });
            list.Add(new EditableField
            {
                Row = 1,
                Column = 1,
                FieldName = "ObjectTypeInfo",
                Name = "Type",
                Required=true,
                FieldType = DataType.Lookup.ToString(),
                Items = Company.GetAvailableAllocationOptions(at)
                    .Select(i => new SelectListItem
                    {
                        Value = i.ObjectType + "|" + i.ObjectTypeID,
                        Text = i.Name
                    })
                .ToList()
            });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "AllowMultipleEntries", Name = relation.GetName(i => i.AllowMultipleEntries), FieldDescription = relation.GetDescription(i => i.AllowMultipleEntries), FieldType = DataType.Boolean.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="at">AttributeTypeID</param>
        /// <param name="ot"></param>
        /// <param name="oid"></param>
        /// <returns></returns>
        [Route("AttributeTypeRelation_EditFields"), NonNullableParameters]
        public JsonResult AttributeTypeRelation_EditFields(int at, string ot, int oid)
        {
            var list = new List<EditableField>();
            var sType = ot.ToString();
            var a = Company.Filter<AttributeTypeRelationDetail>(i => i.AttributeTypeID == at && i.ObjectID == oid && i.ObjectType == sType).SingleOrDefault();

            var relation = new AttributeTypeRelation();

            list.Add(new EditableField { FieldName = "AttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = a.AttributeTypeID.ToString() });
            list.Add(new EditableField { FieldName = "ObjectTypeInfo", FieldType = DataType.Hidden.ToString(), Value = string.Format("{0}|{1}", a.ObjectType, a.ObjectID) });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "AllowMultipleEntries", Name = relation.GetName(i => i.AllowMultipleEntries), FieldDescription = relation.GetDescription(i => i.AllowMultipleEntries), FieldType = DataType.Boolean.ToString(), Value = a.AllowMultipleEntries.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddAttributeTypeRelation")]
        public JsonResult AddAttributeTypeRelation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (form.HasKeys())
                {
                    int typeID = parseIntField(form, "AttributeTypeID");
                    var type = Company.GetById<AttributeType>(typeID);
                    if (type == null)
                    {
                        return jsonException("Invalid attribute type.", HttpStatusCode.BadRequest);
                    }

                    var value = form["ObjectTypeInfo"].Split('|');


                    Company.Add(new AttributeTypeRelation {
                        AttributeType = type,
                        AllowMultipleEntries = parseBooleanField(form, "AllowMultipleEntries"),
                        ObjectType = value[0],
                        ObjectID = int.Parse(value[1])
                    });

                    return jsonSuccess(type.Name + " successfully allocated.", typeID.ToString(), "add", HttpStatusCode.Created);
                }
                else
                {
                    throw new NoFormDataException("allocation");
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

        /// <summary>
        /// Wraps deleteattributetyperelations as it is using a delete operation with a form body which is not supported 
        /// by delete according to the spec for DELETE and it is not supported in angular http object.
        /// </summary>
        /// <param name="AttributeTypeID"></param>
        /// <param name="ObjectType"></param>
        /// <param name="ObjectID"></param>
        /// <returns></returns>
        [HttpDelete, Route("DeleteAttributeTypeRelationWithUri"), NonNullableParameters]
        public JsonResult DeleteAttributeTypeRelationWithUri(int AttributeTypeID, string ObjectType, int ObjectID)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var form = new FormCollection();
            form.Add("AttributeTypeID", AttributeTypeID.ToString());
            form.Add("ObjectType", ObjectType);
            form.Add("ObjectID", ObjectID.ToString());

            return DeleteAttributeTypeRelation(form);
        }

        [HttpDelete, Route("DeleteAttributeTypeRelation")]
        public JsonResult DeleteAttributeTypeRelation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var at = parseIntField(form, "AttributeTypeID");
                var ot = form["ObjectType"];
                var oid = parseIntField(form, "ObjectID");
                if (Company.Delete<AttributeTypeRelation>(i => i.AttributeTypeID == at && i.ObjectType == ot && i.ObjectID == oid))
                    return jsonSuccess("Allocation successfully removed.", ot.ToString(), "delete", HttpStatusCode.OK);
                else
                    return jsonException("Allocation does not exist.", HttpStatusCode.NotFound);
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

        [HttpPut, Route("EditAttributeTypeRelation")]
        public JsonResult EditAttributeTypeRelation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var at = parseIntField(form, "AttributeTypeID");
                var value = form["ObjectTypeInfo"].Split('|');
                var ot = value[0];
                var oid = int.Parse(value[1]);
                var model = Company.Filter<AttributeTypeRelation>(i => i.AttributeTypeID == at && i.ObjectID == oid && i.ObjectType == ot).SingleOrDefault();
                if (model == null)
                {
                    return jsonException("Allocation does not exist.", HttpStatusCode.NotFound);
                }
                model.AllowMultipleEntries = parseBooleanField(form, "AllowMultipleEntries");
                if (Company.Update<AttributeTypeRelation>(model))
                    return jsonSuccess("Allocation successfully updated.", ot.ToString(), "update", HttpStatusCode.OK);
                else
                    return jsonException("Allocation does not exist.", HttpStatusCode.NotFound);
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

            return new JsonNetResult { Data = css, Formatting = Newtonsoft.Json.Formatting.None };
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
            model.SiteNav = Company.SiteNav.Where(s => s.ParentID == null && s.Name != "#Home").OrderBy(s => s.SortOrder).ToList();

            model.HeaderBackgroundColor = (settings.Any(i => i.SettingID == 10) ? settings.Single(i => i.SettingID == 10).Value : "");

            model.ShowHomeAssignmentTile = (settings.Any(i => i.SettingID == 39) ? bool.Parse(settings.Single(i => i.SettingID == 39).Value) : true);
            model.ShowHomeBoardTile = (settings.Any(i => i.SettingID == 40) ? bool.Parse(settings.Single(i => i.SettingID == 40).Value) : true);
            model.ShowHomeActivityTile = (settings.Any(i => i.SettingID == 41) ? bool.Parse(settings.Single(i => i.SettingID == 41).Value) : true);
            model.ShowHomePageTitle = (settings.Any(i => i.SettingID == 42) ? bool.Parse(settings.Single(i => i.SettingID == 42).Value) : false);
            model.HomePageTitleSize = (settings.Any(i => i.SettingID == 43) ? settings.Single(i => i.SettingID == 43).Value : "38pt");
            model.HomePageTitleColor = (settings.Any(i => i.SettingID == 44) ? settings.Single(i => i.SettingID == 44).Value : "#fff");
            model.HomePageBackgroundImage = (settings.Any(i => i.SettingID == 45) ? settings.Single(i => i.SettingID == 45).Value : "");
            model.BrowserTitlePrefix = (settings.Any(i => i.SettingID == 33) ? settings.Single(i => i.SettingID == 33).Value : "D3S");

            model.UseLegacyLineage = (settings.Any(i => i.SettingID == 46) ? bool.Parse(settings.Single(i => i.SettingID == 46).Value) : true);

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
		                    left join artifacttype a on a.id = v.objectID and v.Object = 'ArtifactType'
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

        #region FieldType

        #region Supporting Json Feeds

        /// <summary>
        /// Used to get the child types of a specific parent type.
        /// </summary>
        /// <param name="type">The Type></param>
        /// <param name="id">The Type ID></param>
        /// <returns>A list of child realtionship types</returns>
        [Route("FieldType_ComplexLookup_ChildItems"), NonNullableParameters]
        public JsonNetResult FieldType_ComplexLookup_ChildItems(SystemObjects type, int id)
        {
            dynamic list = null;

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list = Company.GetChildTypes(id, SystemObjects.ArtifactType)
                        .ToList()
                        .Select(i => new { value = $"0|ArtifactType|{i.ObjectID}|0", title = i.Name })
                        .ToList();
                    break;
                case SystemObjects.FusionAttributeType:
                    list = Company.GetChildTypes(id, SystemObjects.FusionAttributeType)
                        .ToList()
                        .Select(i => new { value = $"0|FusionAttributeType|{i.ObjectID}|0", title = i.Name })
                        .ToList();
                    break;
            }

            return new JsonNetResult
            {
                Data = list ?? new List<dynamic>(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Used to get the parent types of a specific child type.
        /// </summary>
        /// <param name="type">The Type></param>
        /// <param name="id">The Type ID></param>
        /// <returns>A list of child realtionship types</returns>
        [Route("FieldType_ComplexLookup_ParentItems"), NonNullableParameters]
        public JsonNetResult FieldType_ComplexLookup_ParentItems(SystemObjects type, int id)
        {
            dynamic list = null;
            BaseIntObject parent;

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list = new List<AssetType>();
                    parent = Company.GetParentType(id, SystemObjects.ArtifactType);
                    if (parent != null)
                        list.Add((AssetType)parent);

                        list = ((List<AssetType>)list).Select(i => new { value = $"0|ArtifactType|{i.ObjectID}", title = i.Name })
                        .Where(i => i.title != null)
                        .ToList();
                    break;
                case SystemObjects.FusionAttributeType:
                    list = new List<AssetType>();
                    parent = Company.GetParentType(id, SystemObjects.FusionAttributeType);
                    if (parent != null)
                        list.Add((AssetType)parent);

                    list = ((List<AssetType>)list).Select(i => new { value = $"0|FusionAttributeType|{i.ObjectID}", title = i.Name })
                        .Where(i => i.title != null)
                        .ToList();
                    break;
            }

            return new JsonNetResult
            {
                Data = list ?? new List<dynamic>(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Used for complex lookup
        /// </summary>
        /// <param name="type">The Type></param>
        /// <param name="id">The Type ID></param>
        /// <returns>A list of relationship types</returns>
        [Route("FieldType_ComplexLookup_IntersectTypes"), NonNullableParameters]
        public JsonNetResult FieldType_ComplexLookup_IntersectTypes(SystemObjects type, int id)
        {
            var intersectTypes = Company.Query<dynamic>($@"select value, title from utility.GetIntersectTypesByType('{type.ToString()}', {id}) order by title");

            return new JsonNetResult
            {
                Data = intersectTypes,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of display fields that match a lookup.
        /// </summary>
        /// <param name="type">The type of object we are adding field type to.</param>
        /// <param name="id">The type Id of object we are adding field type to.</param>
        /// <param name="listType">The type of list to pull fields for.</param>
        /// <param name="listID">The type Id of the list to pull fields for.</param>
        /// <returns>A list of relevant fusion attribute types.</returns>
        [Route("FieldType_FilteredLookup_DisplayFields"), NonNullableParameters]
        public JsonNetResult FieldType_FilteredLookup_DisplayFields(string type, int id, string listType, int listID)
        {
            var list = Company.GetFieldTypesByObject(SystemObjects.LookupType, listID)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                .OrderBy(i => i.Name)
                .Select(i => new { i.ID, i.Name, i.FriendlyName, i.LookupObjectType, i.LookupObjectID })
                .ToList()
                .Select(i => new {
                    title = i.FriendlyName,
                    value = $"{i.ID}|{i.Name}",
                    AllowFilter = ($"{i.LookupObjectType}|{i.LookupObjectID}" == $"{type.Replace("Type", "")}|{id}")
                });

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of fusion attribute types that meet the criteria based on the reference type and source fusion attribute type ID.
        /// </summary>
        /// <param name="id">The Source FusionAttributeType ID</param>
        /// <returns>A list of relevant fusion attribute types.</returns>
        [Route("FieldType_FusionLookup_DisplayFields"), NonNullableParameters]
        public JsonNetResult FieldType_FusionLookup_DisplayFields(int id)
        {
            var list = Company.GetFieldTypesByObject(SystemObjects.FusionAttributeType, id)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                .Select(i => new { i.ID, i.Name })
                .ToDictionary(i => i.Name, i => i.ID);
            list.Add("Name", 0);
            list.Add("TextPath", 0);

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}|{i.Key}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of fusion attribute types that meet the criteria based on the reference type and source fusion attribute type ID.
        /// </summary>
        /// <param name="s">The Source FusionAttributeType ID</param>
        /// <param name="r">The Reference Type we are checking</param>
        /// <returns>A list of relevant fusion attribute types.</returns>
        [Route("FieldType_FusionLookup_TargetAttributeTypes"), NonNullableParameters]
        public JsonNetResult FieldType_FusionLookup_TargetAttributeTypes(int s, int r)
        {
            IQueryable<FusionAttributeType> qry = null;
            switch (r)
            {
                case 2: //Parent Reference
                    var self = Company.GetById<FusionAttributeType>(s);
                    if (self != null)
                    {
                        qry = Company.Filter<FusionAttributeType>(i => i.ID == self.ParentID);
                    }
                    break;
                case 3: //Child Reference
                    qry = Company.Filter<FusionAttributeType>(i => i.ParentID == s);
                    break;
                case 4: //Relationship Reference
                    var relations = Company.Query<int>(@"select case when (SubjectID = @id) then ObjectID else SubjectID end as ID from [IntersectType] where (Subject = 'FusionAttributeType' and Object = 'FusionAttributeType') AND (SubjectID = @id or ObjectID = @id)", new { id = s }).ToList();
                    qry = Company.Filter<FusionAttributeType>(i => relations.Contains(i.ID));
                    break;
            }

            if (qry != null)
            {
                return new JsonNetResult
                {
                    Data = qry.OrderBy(x => x.TextPath).Select(i => new { title = i.TextPath, value = i.ID }),
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            else
            {
                return new JsonNetResult
                {
                    Data = JArray.Parse("[]"),
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
        }

        /// <summary>
        /// Used for both relation lookup and complex lookup
        /// </summary>
        /// <param name="id">IntersectTypeID></param>
        /// <returns>A list of child relationship types</returns>
        [Route("FieldType_RelationLookup_ChildIntersectTypes"), NonNullableParameters]
        public JsonNetResult FieldType_RelationLookup_ChildIntersectTypes(int id)
        {
            var intersectTypes = Company.Query<dynamic>($@"select value, title from utility.GetIntersectTypesByType('IntersectType', {id}) order by title");

            return new JsonNetResult
            {
                Data = intersectTypes,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_RelationLookup_DisplayFields"), NonNullableParameters]
        public JsonNetResult FieldType_RelationLookup_DisplayFields(int intersectTypeID, SystemObjects type, int id)
        {
            var list = Company.GetFieldTypesByObject(type, id)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.Relationship.ToString()  && i.Type != DataType.OwnershipLookup.ToString() && i.Type != DataType.RefListRelationship.ToString()
              && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.FilteredLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                .Select(i => new { i.ID, i.Name })
                .ToDictionary(i => i.Name, i => i.ID);

            if (type == SystemObjects.ReferenceItemType)
            {
                if (id == 0)
                {
                    list.Add("Name", 0);
                    if (!list.ContainsKey("Description"))
                        list.Add("Description", 0);
                }
                else
                {
                    list.Add("Code", 0);
                }
            }
            else if (type == SystemObjects.ResourceType)
            {
                list.Add("FirstName", 0);
                list.Add("LastName", 0);
                list.Add("Email", 0);
                list.Add("LastLoggedInOn", 0);
                list.Add("DisplayValue", 0);
            }
            else if (type == SystemObjects.FusionAttributeType)
            {
                list.Add("Name", 0);
            }
            else if (type == SystemObjects.FusionQueryAttributeType)
            {
                list.Add("Name", 0);
                list.Add("DisplayValue", 0);
            }
            else
            {
                list.Add("DisplayValue", 0);
            }

            list.Add("TextPath", 0);

            var relList = Company.GetFieldTypesByObject(SystemObjects.IntersectType, intersectTypeID)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString())
                .Select(i => new { i.ID, i.Name }).ToList();
            relList.ForEach(r =>
            {
                list.Add($"Relation.{r.Name}", r.ID);
            });

            var sType = type.ToString();
            var relatedTypeList = Company.Filter<IntersectTypeDetail>(i => 
                (i.Subject == sType && i.SubjectID == id) || 
                (i.Object == sType && i.ObjectID == id)
                ).ToList().Select(i => new {
                    ID = i.ID,
                    Name = (i.Subject == sType && i.SubjectID == id) ? $"{i.ObjectName} ({i.PredicateName})" : $"{i.SubjectName} ({i.PredicateName})"
                }).Distinct().ToList();
            relatedTypeList.ForEach(r =>
            {
                if (list.ContainsKey($"Related Item.{r.Name}"))
                {
                    list.Add($"Related Item.{r.Name} ({r.ID})", r.ID);
                }
                else
                {
                    list.Add($"Related Item.{r.Name}", r.ID);
                }
            });


            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}|{i.Key}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_FieldFromRelationship_Fields"), NonNullableParameters]
        public JsonNetResult FieldType_FieldFromRelationship_Fields(SystemObjects type, int id, int intersectTypeID)
        {
            var intersectType = Company.GetById<IntersectType>(intersectTypeID);

            if (intersectType == null)
                return new JsonNetResult { Data = new Dictionary<string, int>() };

            var isSubject = (intersectType.Subject == type.ToString() && intersectType.SubjectID == id);

            var targetObjectType = isSubject ? intersectType.Object : intersectType.Subject;
            var targetObjectTypeID = isSubject ? intersectType.ObjectID : intersectType.SubjectID;

            var list = Company.Filter<FieldType>(f => f.Object == targetObjectType && f.ObjectID == targetObjectTypeID)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString() && i.Type != DataType.Relationship.ToString())
                .Select(i => new { i.ID, i.Name })
                .Distinct()
                .ToDictionary(i => i.Name, i => i.ID);

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_Lookup_Tokens"), NonNullableParameters]
        public JsonNetResult FieldType_Lookup_Tokens(SystemObjects type, int id)
        {
            var list = Company.GetFieldTypesByObject(type, id)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                .Select(i => new { i.ID, i.Name })
                .ToDictionary(i => i.Name, i => i.Name);

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list.Add("ID", "ID");
                    break;
                case SystemObjects.ReferenceItem:
                case SystemObjects.ReferenceItemType:
                    list.Add("Code", "Code");
                    break;
                case SystemObjects.PolicyType:
                    list.Add("TextPath", "TextPath");
                    break;
                case SystemObjects.Resource:
                case SystemObjects.ResourceType:
                    list.Add("First Name", "FirstName");
                    list.Add("Last Name", "LastName");
                    list.Add("Email", "Email");
                    break;
                case SystemObjects.TaxonomyType:
                    if (id == 0)
                    {
                        list.Add("Name", "Name");
                    }
                    else
                    {
                        list.Add("TextPath", "TextPath");
                    }
                    break;                
            }

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = "{" + i.Value + "}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("Reference_Hierarchy"), NonNullableParameters]
        public JsonNetResult Reference_Hierarchy(int id, SystemObjects objectType, int objectId)
        {
            //return possible hierarchy parents for this object type
            var parent = Company.GetParentType(id, SystemObjects.ReferenceItemType);
            var list = new List<PrimeSelectItem>();

            if(parent != null)
            {                
                //get possible parent reference list types defined for this object / object id they cant already be parents
                list = Company.FieldTypes.Where(x => x.Object == objectType.ToString() && x.ObjectID == objectId && x.LookupObjectType == "ReferenceItem" && x.LookupObjectID == parent.ObjectID).Select(i =>  new PrimeSelectItem { label = i.FriendlyName, value = i.ID.ToString() }).ToList();
                if(list.Count > 0) list.Insert(0, new PrimeSelectItem { label = "", value = "" });
            }

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };            
        }

        [Route("FieldType_Relationship_IsListable"), NonNullableParameters]
        public JsonNetResult FieldType_Relationship_IsListable(SystemObjects type, int id, int intersectTypeId)
        {
            bool isListable = false;
            var sType = type.ToString();
            
            var intersectType = Company.Filter<IntersectTypeDetail>(i => i.ID == intersectTypeId).FirstOrDefault();

            if(intersectType != null)
            {
                if (intersectType.Subject == sType && intersectType.SubjectID == id && intersectType.ObjectCardinality == Cardinality.One) isListable = true;
                else if (intersectType.Object == sType && intersectType.ObjectID == id && intersectType.SubjectCardinality == Cardinality.One) isListable = true;
            }

            return new JsonNetResult
            {
                Data = isListable,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_Lookup_DefaultValueOptions"), NonNullableParameters]
        public JsonNetResult FieldType_Lookup_DefaultValueOptions(SystemObjects type, int id)
        {
            var list = new List<ListIntItem>();
            list.Add(new ListIntItem { title = "- No default -", value = null });

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    var sql = "select ast.ObjectID as value,d.DisplayValue as title  from asset ast inner join assettype astt on (ast.assettypeid = astt.id and ast.[object] = 'Artifact') cross apply [dbo].GetAssetDisplayValueById(ast.id) d where astt.ObjectID = @id order by d.DisplayValue";

                    list.AddRange(
                        Company.Query<ListIntItem>(sql, new { id = id })
                    );
                    break;
                case SystemObjects.ReferenceItem:
                case SystemObjects.ReferenceItemType:
                    list.AddRange(
                        Company.Filter<ReferenceItem>(i => i.ReferenceItemTypeID == id)
                        .OrderBy(i => i.DisplayValue)
                        .Select(i => new ListIntItem { title = i.DisplayValue, value = i.ID })
                    );
                    break;
                case SystemObjects.PolicyType:
                    list.AddRange(
                        Company.Filter<Policy>(i => i.PolicyTypeID == id)
                        .OrderBy(i => i.TextPath)
                        .Select(i => new ListIntItem { title = i.TextPath, value = i.ID })
                    );
                    break;
                case SystemObjects.Resource:
                case SystemObjects.ResourceType:
                    if (HideData3SixtyUsers())
                    {
                        list.AddRange(
                            Company.Table<GlobalReportingResource>().ToList()
                            .Where(i => !i.Email.EndsWith("@data3sixty.com") && !i.Email.EndsWith("@infogix.com"))
                            .OrderBy(i => i.FullName)
                            .Select(i => new ListIntItem { title = i.FullName, value = i.ResourceID }));
                    }
                    else
                    {
                        list.AddRange(
                            Company.Table<GlobalReportingResource>().ToList()
                            .OrderBy(i => i.FullName)
                            .Select(i => new ListIntItem { title = i.FullName, value = i.ResourceID }));
                    }
                    break;
                case SystemObjects.RuleType:
                    list.AddRange(
                        Company.Filter<AssetDetail>(i => i.Type == type.ToString() && i.TypeID == id)
                        .OrderBy(i => i.DisplayValue)
                        .Select(i => new ListIntItem { title = i.DisplayValue, value = i.ObjectID })
                    );
                    break;
                case SystemObjects.TaxonomyType:
                    var sqlForTaxonomy = "select a.ObjectID as value, textpath as Title from asset a inner join assettype att on a.assettypeid = att.id cross apply[dbo].[GetAssetTextPathById](a.id, '/') atp where atp.id = a.id and a.object = 'Taxonomy' and att.ObjectID = @id";
                    list.AddRange(
                        Company.Query<ListIntItem>(sqlForTaxonomy, new { id = id })
                    );
                    break;                
            }

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }



        [Route("FieldType_Lookups"), NonNullableParameters]
        public JsonNetResult FieldType_Lookups(SystemObjects type, int id, bool isNg = false)
        {
            #region Load static lists

            var lists = Company.Query<dynamic>("exec utility.GetFieldTypeLookupList @type, @id", new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString() }, id }).ToList();
            var intersectTypes = lists.Where(i => i.type == "I").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var attributes = lists.Where(i => i.type == "A").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var fusionAttributeTypes = lists.Where(i => i.type == "F").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var lookups = lists.Where(i => i.type == "L").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var filteredLookups = lists.Where(i => i.type == "FL").Select(i => new { i.value, i.title }).OrderBy(i => i.title);

            var complexLookupRelations = ComplexLookupRelationType.ChildItem.GetComplexLookupRelationTypeInfoList().ToList();

            var sType = type.ToString();

            var allRelationships = Company.Filter<IntersectTypeDetail>(i =>
                (i.Subject == sType && i.SubjectID == id) ||
                (i.Object == sType && i.ObjectID == id)
            ).ToList();

            var cardinalRelationships = allRelationships.Where(i =>
                (i.Subject == sType && i.SubjectID == id && i.SubjectCardinality == Cardinality.One) ||
                (i.Object == sType && i.ObjectID == id && i.ObjectCardinality == Cardinality.One)
            ).ToList();

            var fieldFromRelRelationships = allRelationships.Where(i =>
                (i.Subject == sType && i.SubjectID == id && i.ObjectCardinality == Cardinality.One) ||
                (i.Object == sType && i.ObjectID == id && i.SubjectCardinality == Cardinality.One)
            ).ToList();

            var Field_Relationships = allRelationships
                .Where(x=>x.PredicateType != PredicateType.InterTypeHierarchy)
                .Select(i => new {
                    title = ((i.Subject == sType && i.SubjectID == id) ? 
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" : 
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                    value = $"{i.ID}"
                });

            var Field_CardinalRelationships = cardinalRelationships
                .Select(i => new {
                    title = ((i.Subject == sType && i.SubjectID == id) ?
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                    value = $"{i.ID}"
                });

            var Field_CardinalReferenceRelationships = cardinalRelationships
                .Where(i =>
                    (i.Subject == sType && i.SubjectID == id) ?
                        (i.Object == SystemObjects.ReferenceItemType.ToString() && i.ObjectID == 0) :
                        (i.Subject == SystemObjects.ReferenceItemType.ToString() && i.SubjectID == 0)
                )
                .Select(i => new {
                    title = ((i.Subject == sType && i.SubjectID == id) ?
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                    value = $"{i.ID}"
                });

            var Field_FieldFromRelRelationships = fieldFromRelRelationships.Select(i => new {
                title = ((i.Subject == sType && i.SubjectID == id) ?
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                value = $"{i.ID}"
            });

            var patterns = new Dictionary<string, string>() {
                { "Choose sample...", "" },
                { "Email", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b" },
                { "IP Address", @"^$|^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$" },
                { "North American Phone", @"^$|\b\d{3}[-.]?\d{3}[-.]?\d{4}\b" },                
                { "Internal Url", @"^$|\b(http(s)?:\/\/){1}([\da-z\.-]+)([\/\w \.-]*)*\/?\b" },
                { "Public Url", @"^$|\b(http(s)?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?\b" },
                { "US Zip Code", @"^(\d{5}(?:\-\d{4})?)$" }
            };
            var dataTypeOptions = DataType.Boolean.GetDataTypeInfoList(type)
                    .Where(i => !i.ReadOnly)
                    .Select(i => new
                    {
                        title = i.Description,
                        value = i.Name
                    })
                    .OrderBy(i => i.title)
                    .ToList();

            #endregion

            return new JsonNetResult
            {
                Data = new
                {
                    Attributes = attributes,
                    Field_Relationships,
                    Field_CardinalRelationships,
                    Field_FieldFromRelRelationships,
                    Field_CardinalReferenceRelationships,
                    DataTypes = dataTypeOptions,
                    FilteredLookups = filteredLookups,
                    Patterns = patterns.Select(i => new { title = i.Key, value = i.Value }),
                    IntersectTypes = intersectTypes,
                    FusionAttributeTypes = fusionAttributeTypes,
                    Lookups = lookups,
                    ComplexLookupRelations = complexLookupRelations
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_FormData"), NonNullableParameters]
        public JsonNetResult FieldType_FormData(int id)
        {
            FieldType ft = null;
            List<dynamic> filteredLookupItems = null;
            List<dynamic> fusionItems = null;
            List<dynamic> relationItems = null;
            dynamic ownershipLookupSettings = null;

            if (id > 0)
            {
                ft = Company.GetById<FieldType>(id, i => i.FieldTypeFusionLookupDefinitions);

                if (ft.FieldTypeFilteredLookupDefinitions != null)
                {
                    if (ft.FieldTypeFilteredLookupDefinitions.Count > 0)
                    {
                        filteredLookupItems = new List<dynamic>();
                        foreach (var i in ft.FieldTypeFilteredLookupDefinitions)
                        {
                            filteredLookupItems.Add(new
                            {
                                i.ID,
                                i.Object,
                                i.ObjectID,
                                DisplayFields = (i.FieldTypeFilteredLookupDisplayFields != null) ? i.FieldTypeFilteredLookupDisplayFields.Select(df => new { value = $"{df.FieldTypeID}|{df.FieldTypeName}", Filter = df.Filter, Show = df.Show, SortOrder = df.SortOrder }).ToList() : null,
                                i.HideHeader,
                                i.HideFooter
                            });
                        }
                    }
                }

                if (ft.FieldTypeFusionLookupDefinitions != null)
                {
                    if (ft.FieldTypeFusionLookupDefinitions.Count > 0)
                    {
                        fusionItems = new List<dynamic>();
                        foreach (var i in ft.FieldTypeFusionLookupDefinitions)
                        {
                            fusionItems.Add(new {
                                ID = i.ID,
                                SourceFusionAttributeType = i.SourceFusionAttributeTypeID,
                                ReferenceType = i.ReferenceType,
                                TargetFusionAttributeType = i.TargetFusionAttributeTypeID,
                                DisplayFields = (i.FieldTypeFusionLookupDisplayFields != null) ? i.FieldTypeFusionLookupDisplayFields.Select(df => new { value = $"{df.FieldTypeID}|{df.FieldTypeName}", FilterValue = df.FilterValue, Show = df.Show, SortOrder = df.SortOrder }).ToList() : null,
                                HideHeader = i.HideHeader,
                                HideFooter = i.HideFooter
                            });
                        }
                    }
                }

                var lookup = Company.FieldTypeLookups.Where(i => i.FieldTypeID == id).FirstOrDefault();
                if (lookup != null)
                {
                    var definition = (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject(lookup.Definition);

                    if (ft.Type == DataType.ComplexRelationLookup.ToString())
                    {
                        relationItems = new List<dynamic>();
                        foreach (var r in definition.Relations)
                        {
                            relationItems.Add(new
                            {
                                r.ID,
                                IntersectType = r.IntersectTypeID,
                                ReferenceType = r.RelationType,
                                ChildIntersectType = 0,
                                DisplayFields = new List<dynamic>(),
                                lookup.HideHeader,
                                lookup.HideFooter,
                                lookup.HideFilter,
                                Direction = r.Direction ?? 0,
                                r.Object,
                                r.ObjectID
                            });
                        }
                        if (definition.Fields != null)
                        {
                            foreach (var f in definition.Fields)
                            {
                                var r = relationItems.Where(i => i.Object == f.Object && i.ObjectID == f.ObjectID).FirstOrDefault();

                                if (r != null)
                                {
                                    r.DisplayFields.Add(f);
                                }
                            }
                        }
                    }
                    else if (ft.Type == DataType.OwnershipLookup.ToString())
                    {
                        ownershipLookupSettings = new {
                            definition.DisplayAssignmentSource,
                            definition.ExpandGroupMembership,
                            lookup.HideFilter,
                            lookup.HideFooter,
                            lookup.HideHeader
                        };
                    }
                }
            }

            return new JsonNetResult
            {
                Data = new
                {
                    FieldType = ft,
                    FilteredLookupItems = filteredLookupItems,
                    FusionItems = fusionItems,
                    OwnershipLookupSettings = ownershipLookupSettings,
                    RelationItems = relationItems
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_TypeAheadLookup"), NonNullableParameters]
        public JsonNetResult FieldType_TypeAheadLookup(int fieldTypeId, string value = "", string query = "")
        {
            var selectList = new List<SelectListItem>();
            var ft = Company.GetById<FieldType>(fieldTypeId);
            string selectedValue = string.IsNullOrWhiteSpace(value) ? (string.IsNullOrWhiteSpace(ft.DefaultValue) ? "" : ft.DefaultValue) : value;

            if (ft.AllowAllValue)
                selectList.Add(new SelectListItem { Text = ft.AllowAllLabel, Value = "0" });

            int maxItems = 20;
            var columns = $@"
                V.FieldTypeID,
                V.LookupObjectType,
                V.LookupObjectID,
                V.Value,
                V.Text";

            var selectedSql = $@"select {columns} 
                from FieldLookupValue V 
                where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId and V.Value = @selectedValue 
                union
                ";

            var resourceJoin = $@"
                inner join reporting.Global_resource R on R.ResourceID = V.Value and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com'
                ";
            
            var itemsSql = $@"
                {(string.IsNullOrWhiteSpace(selectedValue) ? "" : selectedSql)}
                select top {maxItems} {columns}
                from FieldLookupValue V
                {(HideData3SixtyUsers() && ft.LookupObjectType == "Resource" ? resourceJoin : "")}
                where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId {(string.IsNullOrWhiteSpace(query) ? "" : " and V.Text like '%' + @query + '%' ")}
                ";

            var items = Company.Query<FieldLookupValue>(itemsSql, new { fieldTypeId = ft.ID, lookupObjectType = ft.LookupObjectType, lookupObjectId = ft.LookupObjectID, selectedValue, query }).ToList();

            selectList.AddRange(items.Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = string.IsNullOrEmpty(selectedValue) ? false : i.Value.ToString() == selectedValue }));

            selectList = selectList.OrderBy(i => i.Selected ? 0 : 1).ToList();

            return new JsonNetResult
            {
                Data = selectList,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }
        #endregion

        #region Form Get/Post

        private void CheckIsFieldTypeNameReserved(string name)
        {
            var nameUpper = name.ToUpper();

            if (nameUpper == "PARENTID" || nameUpper == "DATABASE")  throw new Exception("Use of a field type with the name " + name + " is prohibited.");
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFieldType")]
        public JsonResult AddFieldType(FieldTypeEditorModel model)
        {
            try
            {
                if (!Company.HasAssetTypePermission(model.FieldType.Object, model.FieldType.ObjectID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                int maxColumnOrder = 0;
                try { maxColumnOrder = Company.GetFieldTypesByObject((SystemObjects)Enum.Parse(typeof(SystemObjects), model.FieldType.Object), model.FieldType.ObjectID).Max(i => i.ColumnOrder); }
                catch { }

                //dont let fields with reserved names in
                CheckIsFieldTypeNameReserved(model.FieldType.Name);

                model.FieldType.ColumnOrder = maxColumnOrder + 1;
                model.FieldType.UpdatedBy = Company.CurrentResourceID;

                //set the default formatted value to the same as the default value, for lists the trigger will update this to the display value for the list
                // however for strings, bools etc it will stay since the lookupobjecttype is null since the trigger only looks at where it is not null.
                if(!string.IsNullOrEmpty(model.FieldType.DefaultValue))
                    model.FieldType.DefaultFormattedValue = model.FieldType.DefaultValue;

                var nameRegex = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z0-9_-]+$");
                if (!nameRegex.IsMatch(model.FieldType.Name))
                {
                    throw new ConflictException("Error Occurred!", $"{FieldInfo.ApiName_Name} can only have uppercase letters, lowercase letters, numbers, dash, or underscore. It must also begin with a letter.");
                }

                if (!string.IsNullOrEmpty(model.FieldType.Name) && (model.FieldType.Name).ToUpper().Equals("ID"))
                {
                    throw new ConflictException("Error Occurred!", "You can not add field with API Name [ID].");
                }

                if (model.FieldType.MinimumLength.HasValue && model.FieldType.MaximumLength.HasValue)
                {
                    if (model.FieldType.MinimumLength.Value > model.FieldType.MaximumLength.Value)
                    {
                        throw new ConflictException("Error Occurred!", "You may not have a minimum length that is greater than the maximum length.");
                    }
                }
                if (!new[] { "Number", "Decimal" }.Contains(model.FieldType.Type))
                {
                    if (!model.FieldType.IsRequired) model.FieldType.MinimumLength = 0;
                }

                var val = model.Validation();
                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                if (model.FieldType.Type == DataType.RefListRelationship.ToString() && (model.FieldType.LookupObjectType != "IntersectType" || model.FieldType.LookupObjectID ==null))
                {
                    throw new ConflictException("Error Occurred!", FieldInfo.FieldReferenceItemListFromRelationship_NeededRelationship);
                }
                    if (model.FieldType.Type != DataType.Lookup.ToString())                
                    model.FieldType.ParentFieldTypeID = 0;

                switch (model.FieldType.Type)
                {
                    case "Date":

                        var date = ConverDate(model.FieldType.DefaultValue);
                        if (date != null)
                        {
                            model.FieldType.DefaultValue = date;
                        }
                        Company.Add<FieldType>(model.FieldType);
                        break;
                    case "Html":
                        model.FieldType.MinimumLength = (!model.FieldType.IsRequired) ? (int?)null : 1;
                        model.FieldType.MaximumLength = null;
                        Company.Add<FieldType>(model.FieldType);
                        break;
                    case "Lookup":
                        #region
                        if (string.IsNullOrEmpty(model.FieldType.LookupDisplayFormat))
                        {
                            throw new ConflictException("Error Occurred!", $"{FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
                        }
                        Company.Add<FieldType>(model.FieldType);
                        break;
                    #endregion
                    case "FilteredLookup":
                        #region
                        if (model.FilteredLookupItem != null)
                        {
                            val = model.FilteredLookupItem.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var def = new FieldTypeFilteredLookupDefinition
                            {                                
                                Object = model.FilteredLookupItem.Object,
                                ObjectID = model.FilteredLookupItem.ObjectID,
                                HideHeader = model.FilteredLookupItem.HideHeader,
                                HideFooter = model.FilteredLookupItem.HideFooter
                            };

                            if (model.FilteredLookupItem.DisplayFields != null)
                            {
                                if (model.FilteredLookupItem.DisplayFields.Count > 0)
                                {
                                    def.FieldTypeFilteredLookupDisplayFields = new List<FieldTypeFilteredLookupDisplayField>();

                                    foreach (var df in model.FilteredLookupItem.DisplayFields)
                                    {
                                        var ndf = new FieldTypeFilteredLookupDisplayField
                                        {
                                            FieldTypeFilteredLookupDefinitionID = def.ID,
                                            FieldTypeName = df.FieldTypeName,
                                            FieldTypeID = df.FieldTypeID,
                                            Filter = df.Filter,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || ndf.Filter || ndf.SortOrder.HasValue)
                                            def.FieldTypeFilteredLookupDisplayFields.Add(ndf);
                                    }
                                }
                            }

                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsRequired = false;
                            model.FieldType.FieldTypeFilteredLookupDefinitions = new List<FieldTypeFilteredLookupDefinition>() { def };
                            
                            Company.Add<FieldType>(model.FieldType);
                        }
                        break;
                    #endregion
                    case "FusionLookup":
                        #region
                        foreach (var fi in model.FusionItems)
                        {
                            val = fi.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var def = new FieldTypeFusionLookupDefinition
                            {                                
                                ReferenceType = fi.ReferenceType,
                                SourceFusionAttributeTypeID = fi.SourceFusionAttributeType,
                                TargetFusionAttributeTypeID = fi.TargetFusionAttributeType,
                                HideHeader = fi.HideHeader,
                                HideFooter = fi.HideFooter
                            };

                            if (fi.DisplayFields != null)
                            {
                                if (fi.DisplayFields.Count > 0)
                                {
                                    def.FieldTypeFusionLookupDisplayFields = new List<FieldTypeFusionLookupDisplayField>();

                                    foreach (var df in fi.DisplayFields)
                                    {
                                        var ndf = new FieldTypeFusionLookupDisplayField
                                        {
                                            FieldTypeFusionLookupDefinitionID = def.ID,
                                            FieldTypeName = df.FieldTypeName,
                                            FieldTypeID = df.FieldTypeID,
                                            FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || !string.IsNullOrEmpty(ndf.FilterValue) || ndf.SortOrder.HasValue)
                                            def.FieldTypeFusionLookupDisplayFields.Add(ndf);
                                    }
                                }
                            }
                            model.FieldType.FieldTypeFusionLookupDefinitions = new List<FieldTypeFusionLookupDefinition>() { def };
                        }

                        model.FieldType.IsDisplayable = true;

                        Company.Add<FieldType>(model.FieldType);
                        break;
                    #endregion
                    case "ComplexRelationLookup":
                        #region
                        var relations = new List<FieldLookupRelationItem>();
                        var fields = new List<FieldLookupFieldItem>();
                        foreach (var r in model.RelationItems)
                        {
                            relations.Add(new FieldLookupRelationItem
                            {
                                IntersectTypeID = r.IntersectType,
                                Object = r.Object,
                                ObjectID = r.ObjectID,
                                RelationType = r.ReferenceType,
                                Direction = r.Direction

                            });
                            if (r.DisplayFields == null)
                                r.DisplayFields = new List<FieldTypeItemDisplayFieldEditorModel>();
                            foreach (var f in r.DisplayFields.Where(i => i.Show || !string.IsNullOrEmpty(i.FilterValue) || (i.SortOrder.HasValue && i.SortOrder != 0)))
                            {
                                fields.Add(new FieldLookupFieldItem
                                {
                                    DisplayOrder = f.DisplayOrder,
                                    Object = r.Object,
                                    ObjectID = r.ObjectID,
                                    FieldTypeID = f.FieldTypeID,
                                    FieldTypeName = f.FieldTypeName,
                                    SortOrder = f.SortOrder ?? 0,
                                    OverrideDisplayName = f.OverrideDisplayName,
                                    Filter = f.FilterValue,
                                    Show = f.Show,
                                    Width = f.Width
                                });
                            }
                        }

                        var lookup = new
                        {
                            Relations = relations,
                            Fields = fields
                        };
                        var lookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.RelationItems[0].HideFooter,
                            HideHeader = model.RelationItems[0].HideHeader,
                            HideFilter = model.RelationItems[0].HideFilter,
                            LookupType = model.RelationItems[0].RelationType,
                            Definition = Newtonsoft.Json.JsonConvert.SerializeObject(lookup)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsRequired = false;

                            Company.Add<FieldType>(model.FieldType);
                            lookupRow.FieldTypeID = model.FieldType.ID;
                            Company.FieldTypeLookups.Add(lookupRow);
                            Company.SaveChanges();
                        }
                        catch 
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "OwnershipLookup":
                        #region
                         var ownershipSettings = new
                        {
                            DisplayAssignmentSource = model.OwnershipLookupSettings.DisplayAssignmentSource,
                            ExpandGroupMembership = model.OwnershipLookupSettings.ExpandGroupMembership
                        };
                        var ownershipLookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.OwnershipLookupSettings.HideFooter,
                            HideHeader = model.OwnershipLookupSettings.HideHeader,
                            HideFilter = model.OwnershipLookupSettings.HideFilter,
                            LookupType = 1,
                            Definition = Newtonsoft.Json.JsonConvert.SerializeObject(ownershipSettings)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsRequired = false;

                            Company.Add<FieldType>(model.FieldType);
                            ownershipLookupRow.FieldTypeID = model.FieldType.ID;
                            Company.FieldTypeLookups.Add(ownershipLookupRow);
                            Company.SaveChanges();
                        }
                        catch 
                        {
                            throw;
                        }

                        break;
                    #endregion
                    default:
                        Company.Add<FieldType>(model.FieldType);
                        break;
                }

                return jsonSuccess(FormInfo.Add_FieldType_Confirmation, model.FieldType.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteFieldType")]
        public JsonResult DeleteFieldType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");

                var model = Company.GetById<FieldType>(id);
                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                if (!Company.HasAssetTypePermission(model.Object, model.ObjectID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                return jsonSuccess(FormInfo.Delete_FieldType_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteFieldTypeByID"), NonNullableParameters]
        public JsonResult DeleteFieldTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFieldType(form);
        }

        [HttpGet, ActionName("FieldType"), Route("FieldType"), NonNullableParameters]
        public JsonNetResult GetFieldType(int id)
        {
            var a = Company.GetById<FieldType>(id);
            if (a == null) return null;
            var used = Company.Any<Field>(i => i.FieldTypeID == id);

            if (!new[] { "Number", "Decimal" }.Contains(a.Type))
            {
                if (!a.IsRequired) a.MinimumLength = 0;
            }

            var model = new FieldTypeEditorModel
            {
                FieldIsUsed = used,
                FieldType = a
            };
            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [HttpPut, ValidateInput(false), Route("EditFieldType")]
        public JsonResult EditFieldType(FieldTypeEditorModel model)
        {
            try
            {
                //dont let fields with reserved names in
                CheckIsFieldTypeNameReserved(model.FieldType.Name);

                var ft = Company.GetById<FieldType>(model.FieldType.ID);
                var used = Company.Any<Field>(i => i.FieldTypeID == ft.ID);

                if (ft == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                if (!Company.HasAssetTypePermission(ft.Object, ft.ObjectID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var val = model.Validation();
                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                var nameRegex = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z0-9_-]+$");
                if (!nameRegex.IsMatch(model.FieldType.Name))
                {
                    throw new ConflictException("Error Occurred!", $"{FieldInfo.ApiName_Name} can only have uppercase letters, lowercase letters, numbers, dash, or underscore. It must also begin with a letter.");
                }

           
                if (ft.Type == "Lookup" && ft.AllowMultipleValues && !model.FieldType.AllowMultipleValues &&
                            Company.Fields.Where(x => x.FieldTypeID == ft.ID).Where(x => x.Value.Contains(",")).ToList().Count() > 0)
                {
                    throw new ConflictException("Error Occurred!", FormInfo.FieldType_List_Error_Multiple_Items_Used);
                }

                if (model.FieldType.Type == DataType.RefListRelationship.ToString() && (model.FieldType.LookupObjectType != "IntersectType" || model.FieldType.LookupObjectID == null))
                {
                    throw new ConflictException("Error Occurred!", FieldInfo.FieldReferenceItemListFromRelationship_NeededRelationship);
                }
                //shallow copy of fieldType
                var ftCopy = (FieldType)Company.Entry(ft)
                                              .CurrentValues.ToObject();
                // Static fields

                ft.Name = model.FieldType.Name;
                ft.SortOrder = model.FieldType.SortOrder;
                ft.Category = model.FieldType.Category;
                ft.FriendlyName = model.FieldType.FriendlyName;                
                ft.DefaultValue = (string.IsNullOrEmpty(model.FieldType.DefaultValue)) ? null : model.FieldType.DefaultValue.Trim();
                //set the default formatted value to the same as the default value, for lists the trigger will update this to the display value for the list
                // however for strings, bools etc it will stay as there is no lookupfield column.
                if (!string.IsNullOrEmpty(ft.DefaultValue))
                    ft.DefaultFormattedValue = ft.DefaultValue;
                ft.DisplayDescription = model.FieldType.DisplayDescription;
                ft.FormDescription = model.FieldType.FormDescription;
                ft.ValidationDescription = model.FieldType.ValidationDescription;
                ft.ColumnWidth = model.FieldType.ColumnWidth;
                ft.AllowMultipleValues = model.FieldType.AllowMultipleValues;
                ft.Increment = model.FieldType.Increment;
                if (model.FieldType.Type == DataType.Lookup.ToString())
                    ft.ParentFieldTypeID = model.FieldType.ParentFieldTypeID;
                else
                    ft.ParentFieldTypeID = 0;

                if (
                    (model.FieldType.Type == DataType.ComplexRelationLookup.ToString()) ||
                    (model.FieldType.Type == DataType.FilteredLookup.ToString()) ||
                    (model.FieldType.Type == DataType.FusionLookup.ToString()) ||
                    (model.FieldType.Type == DataType.OwnershipLookup.ToString())
                    )
                {
                    ft.IsDisplayable = true;
                    ft.IsEditable = false;
                    ft.IsListable = false;
                    ft.IsPartOfKey = false;
                }
                else
                {
                    ft.IsDisplayable = model.FieldType.IsDisplayable;
                    ft.IsEditable = model.FieldType.IsEditable;
                    ft.IsListable = model.FieldType.IsListable;
                    ft.IsPartOfKey = model.FieldType.IsPartOfKey;
                    ft.IsPrimaryFilter = model.FieldType.IsPrimaryFilter;
                }

                if (model.FieldType.Type == DataType.Lookup.ToString())
                {
                    ft.AllowAllLabel = model.FieldType.AllowAllLabel;
                    ft.AllowAllValue = model.FieldType.AllowAllValue;
                }
                else
                {
                    ft.AllowAllLabel = null;
                    ft.AllowAllValue = false;
                }

                ft.IsRequired = model.FieldType.IsRequired;

                ft.MaximumLength = model.FieldType.MaximumLength;
                ft.Pattern = model.FieldType.Pattern;

                if (new[] { "Number", "Decimal" }.Contains(ft.Type))
                {
                    ft.MinimumLength = model.FieldType.MinimumLength;
                }
                else
                {
                    if (!ft.IsRequired) ft.MinimumLength = 0;
                }

                bool isNew;

                var defs = Company.Filter<FieldTypeFusionLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFusionLookupDisplayFields).ToList();
                var efli = Company.Filter<FieldTypeFilteredLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFilteredLookupDisplayFields).FirstOrDefault();
                var fl = Company.Filter<FieldTypeLookup>(i => i.FieldTypeID == ft.ID).FirstOrDefault();

                if (ft.Type == "Date")
                {
                    var date = ConverDate(ft.DefaultValue);
                    if (date != null)
                        ft.DefaultValue = date;
                }

                if (used)
                {
                    var allowTypeChange = false;
                    switch (ft.Type)
                    {
                        case "Text":
                            allowTypeChange = (model.FieldType.Type == DataType.Text.ToString()) || (model.FieldType.Type == DataType.Html.ToString()) || (model.FieldType.Type == DataType.Password.ToString());
                            break;
                        case "Number":
                            allowTypeChange = (model.FieldType.Type == DataType.Number.ToString()) || (model.FieldType.Type == DataType.Decimal.ToString());
                            break;
                        case "Password":
                            allowTypeChange = (model.FieldType.Type == DataType.Password.ToString()) || (model.FieldType.Type == DataType.Html.ToString()) || (model.FieldType.Type == DataType.Text.ToString());
                            break;
                    }
                    if (allowTypeChange)
                    {
                        ft.Type = model.FieldType.Type;
                    }
                    else
                    {
                        if (ft.Type != model.FieldType.Type)
                        {
                            throw new ConflictException("Error Occurred!", $"You may not change the input type for {ft.FriendlyName} as it is already used.");
                        }
                    }
                }
                else
                {
                    ft.Type = model.FieldType.Type;

                    //reset type specific properties
                    ft.LookupObjectType = null;
                    ft.LookupObjectID = null;
                    ft.LookupDisplayFormat = null;

                    if (defs != null && ft.Type != DataType.FusionLookup.ToString())
                    {
                        foreach(var i in defs)
                        {
                            var d = Company.FieldTypeFusionLookupDisplayFields.Where(j => j.FieldTypeFusionLookupDefinitionID == i.ID).ToList();
                            if (d != null && d.Count > 0)
                                Company.FieldTypeFusionLookupDisplayFields.RemoveRange(d);
                        }
                        Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                            
                    }

                    if (efli != null && ft.Type != DataType.FilteredLookup.ToString())
                    {

                        var d = Company.FieldTypeFilteredLookupDisplayFields.Where(j => j.FieldTypeFilteredLookupDefinitionID == efli.ID).ToList();
                        if (d != null && d.Count > 0)
                            Company.FieldTypeFilteredLookupDisplayFields.RemoveRange(d);
                        Company.FieldTypeFilteredLookupDefinitions.Remove(efli);
                    }

                    if (fl != null && ft.Type != DataType.ComplexRelationLookup.ToString())
                    {
                        Company.FieldTypeLookups.Remove(fl);
                    }

                }

                switch (ft.Type)
                {
                    case "Html":
                        ft.MinimumLength = (!ft.IsRequired) ? (int?)null : 1;
                        ft.MaximumLength = null;
                    break;
                    case "FilteredLookup":
                        #region
                        isNew = false;
                        if (model.FilteredLookupItem != null)
                        {
                            val = model.FilteredLookupItem.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var listToRemove = new List<FieldTypeFilteredLookupDisplayField>();

                            if (efli == null)
                            {
                                isNew = true;
                                efli = new FieldTypeFilteredLookupDefinition
                                {
                                    FieldTypeID = model.FieldType.ID,
                                    Object = model.FilteredLookupItem.Object,
                                    ObjectID = model.FilteredLookupItem.ObjectID,
                                    HideHeader = model.FilteredLookupItem.HideHeader,
                                    HideFooter = model.FilteredLookupItem.HideFooter,
                                    FieldTypeFilteredLookupDisplayFields = new List<FieldTypeFilteredLookupDisplayField>()
                                };
                            }
                            else
                            {
                                efli.Object = model.FilteredLookupItem.Object;
                                efli.ObjectID = model.FilteredLookupItem.ObjectID;
                                efli.HideHeader = model.FilteredLookupItem.HideHeader;
                                efli.HideFooter = model.FilteredLookupItem.HideFooter;
                            }

                            if (model.FilteredLookupItem.DisplayFields != null)
                            {
                                // Add those that do not yet exist.
                                foreach (var df in model.FilteredLookupItem.DisplayFields)
                                {
                                    if (!efli.FieldTypeFilteredLookupDisplayFields.Any(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName))
                                    {
                                        var ndf = new FieldTypeFilteredLookupDisplayField
                                        {
                                            FieldTypeFilteredLookupDefinitionID = efli.ID,
                                            FieldTypeID = df.FieldTypeID,
                                            FieldTypeName = df.FieldTypeName,
                                            Filter = df.Filter,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || ndf.Filter || ndf.SortOrder.HasValue)
                                            efli.FieldTypeFilteredLookupDisplayFields.Add(ndf);
                                    }
                                    else
                                    {
                                        var edf = efli.FieldTypeFilteredLookupDisplayFields.Single(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName);

                                        edf.Filter = df.Filter;
                                        edf.SortOrder = df.SortOrder;
                                        edf.Show = df.Show;

                                        if (!edf.Show && !edf.Filter && !edf.SortOrder.HasValue)
                                            efli.FieldTypeFilteredLookupDisplayFields.Remove(edf);
                                    }
                                }

                                // Remove those that no longer exist.
                                foreach (var edf in efli.FieldTypeFilteredLookupDisplayFields)
                                {
                                    if (!model.FilteredLookupItem.DisplayFields.Any(i => i.FieldTypeID == edf.FieldTypeID && i.FieldTypeName == edf.FieldTypeName))
                                    {
                                        listToRemove.Add(edf);
                                    }
                                }
                            }
                            else
                            {
                                if (efli.FieldTypeFilteredLookupDisplayFields != null)
                                {
                                    listToRemove.AddRange(efli.FieldTypeFilteredLookupDisplayFields);
                                }
                            }

                            if (listToRemove.Count > 0)
                            {
                                Company.FieldTypeFilteredLookupDisplayFields.RemoveRange(listToRemove);
                            }

                            listToRemove = null;

                            if (isNew)
                                Company.Add<FieldTypeFilteredLookupDefinition>(efli);
                            else
                                Company.Update<FieldTypeFilteredLookupDefinition>(efli);
                        }
                        else
                        {
                            if (efli != null)
                            {
                                ft.FieldTypeFilteredLookupDefinitions.Remove(efli);
                            }
                        }

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        break;
                    #endregion
                    case "FusionLookup":
                        #region
                        foreach (var fi in model.FusionItems)
                        {
                            val = fi.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            isNew = false;
                            FieldTypeFusionLookupDefinition efi = null;

                            if (fi.ID > 0)
                            {
                                efi = defs.SingleOrDefault(i => i.ID == fi.ID);
                                if (efi == null)
                                {
                                    isNew = true;
                                }
                            }
                            else
                            {
                                isNew = true;
                            }


                            if (isNew)
                            {
                                efi = new FieldTypeFusionLookupDefinition
                                {
                                    FieldTypeID = ft.ID,
                                    ReferenceType = fi.ReferenceType,
                                    SourceFusionAttributeTypeID = fi.SourceFusionAttributeType,
                                    TargetFusionAttributeTypeID = fi.TargetFusionAttributeType,
                                    FieldTypeFusionLookupDisplayFields = new List<FieldTypeFusionLookupDisplayField>(),
                                    HideHeader = fi.HideHeader,
                                    HideFooter = fi.HideFooter
                                };
                            }
                            else
                            {
                                efi.ReferenceType = fi.ReferenceType;
                                efi.SourceFusionAttributeTypeID = fi.SourceFusionAttributeType;
                                efi.TargetFusionAttributeTypeID = fi.TargetFusionAttributeType;
                                efi.HideHeader = fi.HideHeader;
                                efi.HideFooter = fi.HideFooter;
                            }


                            var listToRemove = new List<FieldTypeFusionLookupDisplayField>();

                            if (fi.DisplayFields != null)
                            {
                                // Add those that do not yet exist.
                                foreach (var df in fi.DisplayFields)
                                {
                                    if (!efi.FieldTypeFusionLookupDisplayFields.Any(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName))
                                    {
                                        var ndf = new FieldTypeFusionLookupDisplayField
                                        {
                                            FieldTypeFusionLookupDefinitionID = efi.ID,
                                            FieldTypeID = df.FieldTypeID,
                                            FieldTypeName = df.FieldTypeName,
                                            FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || !string.IsNullOrEmpty(ndf.FilterValue) || ndf.SortOrder.HasValue)
                                            efi.FieldTypeFusionLookupDisplayFields.Add(ndf);
                                    }
                                    else
                                    {
                                        var edf = efi.FieldTypeFusionLookupDisplayFields.Single(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName);

                                        edf.FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue;
                                        edf.SortOrder = df.SortOrder;
                                        edf.Show = df.Show;

                                        if (!edf.Show && string.IsNullOrEmpty(edf.FilterValue) && !edf.SortOrder.HasValue)
                                            efi.FieldTypeFusionLookupDisplayFields.Remove(edf);
                                    }
                                }

                                // Remove those that no longer exist.
                                foreach (var edf in efi.FieldTypeFusionLookupDisplayFields)
                                {
                                    if (!fi.DisplayFields.Any(i => i.FieldTypeID == edf.FieldTypeID && i.FieldTypeName == edf.FieldTypeName))
                                    {
                                        listToRemove.Add(edf);
                                    }
                                }
                            }
                            else
                            {
                                if (efi.FieldTypeFusionLookupDisplayFields != null)
                                {
                                    listToRemove.AddRange(efi.FieldTypeFusionLookupDisplayFields);
                                }
                            }

                            if (listToRemove.Count > 0)
                            {
                                Company.FieldTypeFusionLookupDisplayFields.RemoveRange(listToRemove);
                            }

                            listToRemove = null;

                            if (isNew)
                                Company.Add<FieldTypeFusionLookupDefinition>(efi);
                            else
                                Company.Update<FieldTypeFusionLookupDefinition>(efi);
                        }

                        //Clean up previous stuff
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                    #endregion
                    case "Lookup":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupDisplayFormat = model.FieldType.LookupDisplayFormat;
                        ft.LookupEditFormat = model.FieldType.LookupEditFormat;
                        if (string.IsNullOrEmpty(model.FieldType.LookupDisplayFormat))
                        {
                            throw new ConflictException("Error Occurred!", $"{FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
                        }

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                    #endregion
                    case "ComplexRelationLookup":
                        #region
                        var relations = new List<FieldLookupRelationItem>();
                        var fields = new List<FieldLookupFieldItem>();
                        foreach(var r in model.RelationItems)
                        {
                            relations.Add(new FieldLookupRelationItem
                            {
                                IntersectTypeID = r.IntersectType,
                                Object = r.Object,
                                ObjectID = r.ObjectID,
                                RelationType = r.ReferenceType,
                                Direction = r.Direction

                            });
                            if (r.DisplayFields == null)
                                r.DisplayFields = new List<FieldTypeItemDisplayFieldEditorModel>();
                            foreach(var f in r.DisplayFields.Where(i => i.Show || !string.IsNullOrEmpty(i.FilterValue) || (i.SortOrder.HasValue && i.SortOrder != 0)))
                            {
                                fields.Add(new FieldLookupFieldItem
                                {
                                    DisplayOrder = f.DisplayOrder,
                                    Object = r.Object,
                                    ObjectID = r.ObjectID,
                                    FieldTypeID = f.FieldTypeID,
                                    FieldTypeName = f.FieldTypeName,
                                    SortOrder = f.SortOrder ?? 0,
                                    OverrideDisplayName = f.OverrideDisplayName,
                                    Filter = f.FilterValue,
                                    Show = f.Show,
                                    Width = f.Width
                                });
                            }
                        }

                        var lookup = new
                        {
                            Relations = relations,
                            Fields = fields
                        };
                        var lookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.RelationItems[0].HideFooter,
                            HideHeader = model.RelationItems[0].HideHeader,
                            HideFilter = model.RelationItems[0].HideFilter,
                            LookupType = model.RelationItems[0].RelationType,
                            Definition  = Newtonsoft.Json.JsonConvert.SerializeObject(lookup)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            Company.FieldTypeLookups.Add(lookupRow);
                            Company.SaveChanges();
                        } catch 
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "OwnershipLookup":
                        #region
                        var ownershipSettings = new
                        {
                            DisplayAssignmentSource = model.OwnershipLookupSettings.DisplayAssignmentSource,
                            ExpandGroupMembership = model.OwnershipLookupSettings.ExpandGroupMembership
                        };
                        var ownershipLookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.OwnershipLookupSettings.HideFooter,
                            HideHeader = model.OwnershipLookupSettings.HideHeader,
                            HideFilter = model.OwnershipLookupSettings.HideFilter,
                            LookupType = 1,
                            Definition = Newtonsoft.Json.JsonConvert.SerializeObject(ownershipSettings)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsRequired = false;

                            ownershipLookupRow.FieldTypeID = model.FieldType.ID;
                            Company.FieldTypeLookups.Add(ownershipLookupRow);
                            Company.SaveChanges();
                        }
                        catch 
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "Relationship":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupDisplayFormat = null;
                        ft.LookupEditFormat = null;

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                    #endregion
                    case "FieldFromRelationship":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupObjectFieldTypeID = model.FieldType.LookupObjectFieldTypeID;
                        ft.LookupDisplayFormat = null;
                        ft.LookupEditFormat = null;

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                    #endregion
                    case "RefListRelationship":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupDisplayFormat = null;
                        ft.LookupEditFormat = null;

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                        #endregion
                }

                ft.UpdatedBy = Company.CurrentResourceID;

                bool columnModified = false;
                foreach (System.Reflection.PropertyInfo property in ft.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) 
                {
                    if (property.Name == "Fields" || property.Name == "FieldTypeLookup" || property.Name == "FieldTypeFilteredLookupDefinitions"
                          || property.Name == "UpdatedBy" || property.Name ==  "FieldTypeFusionLookupDefinitions")
                        continue;

                    object value1 = property.GetValue(ft, null);
                    object value2 = property.GetValue(ftCopy, null);
                    if (!object.Equals(value1,value2)){
                        Company.Entry(ft).Property(property.Name).IsModified = true;
                        columnModified = true;
                    }
                    else
                        Company.Entry(ft).Property(property.Name).IsModified = false;

                }

                if(columnModified)
                    Company.Entry(ft).Property(x=>x.UpdatedBy).IsModified = true;

                Company.SaveChanges();

               

                return jsonSuccess(FormInfo.Edit_FieldType_Confirmation, ft.ID.ToString(), "edit", HttpStatusCode.OK);
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

        #region Fusion

        #region Field Generation

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ft">FusionTypeID</param>
        /// <returns></returns>
        [Route("Fusion_AddFields"), NonNullableParameters]
        public JsonResult Fusion_AddFields(int ft)
        {
            var list = new List<EditableField>();
            
            if (!Company.HasAssetTypePermission(SystemObjects.FusionType, ft, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var fusion = new Fusion();

            list.Add(new EditableField { FieldName = "FusionTypeID", FieldType = DataType.Hidden.ToString(), Value = ft.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = fusion.GetName(i => i.Name), FieldDescription = fusion.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = fusion.GetName(i => i.Description), FieldDescription = fusion.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Manual", Name = fusion.GetName(i => i.Manual), FieldDescription = fusion.GetDescription(i => i.Manual), FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, FieldName = "Enabled", Name = fusion.GetName(i => i.Enabled), FieldDescription = fusion.GetDescription(i => i.Enabled), FieldType = DataType.Boolean.ToString() });

            var intervalTypes = new List<SelectListItem>();
            intervalTypes.Add(new SelectListItem { Text = "Minute(s)", Value = "3" });
            intervalTypes.Add(new SelectListItem { Text = "Hour(s)", Value = "2" });            
            list.Add(new EditableField { Row = 4, Column = 1, FieldName = "IntervalType", Required= true, Name = fusion.GetName(i => i.IntervalType), FieldDescription = fusion.GetDescription(i => i.IntervalType), FieldType = DataType.Lookup.ToString(), Items = intervalTypes });
            list.Add(new EditableField { Row = 4, Column = 2, Required=true, FieldName = "Interval", Name = fusion.GetName(i => i.Interval), FieldDescription = fusion.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString(), Validations = checkAndAddValidation("Number", "Interval", true, "([1-9]|[1-8][0-9]|9[0-9]|[1-8][0-9]{2}|9[0-8][0-9]|99[0-9]|[1-8][0-9]{3}|9[0-8][0-9]{2}|99[0-8][0-9]|999[0-9]|10000)", null, null, "Please enter value between 1,10000.") });

            list.Add(new EditableField { Row = 5, Column = 3, FieldName = "LockPromotedItems", Name = fusion.GetName(i => i.LockPromotedItems), FieldDescription = fusion.GetDescription(i => i.LockPromotedItems), FieldType = DataType.Boolean.ToString() });
            
            var owners = Company.GetFusionOwnerOptions().Select(i => new SelectListItem { Text = i.Name, Value = $"{i.ID}", Selected = false }).ToList();
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "Owners", Name = "Owners", FieldDescription = "You must assign one or more owners for this configuration.", FieldType = DataType.Lookup.ToString(), MultiSelect = true, Items = owners });
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.FusionType, ft).ToList(), 7);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
        [Route("Fusion_EditFields"), NonNullableParameters]
        public JsonResult Fusion_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Fusion>(id, i => i.FusionOwners);

            if (!Company.HasAssetPermission(SystemObjects.Fusion, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });

            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Manual", Name = a.GetName(i => i.Manual), FieldDescription = a.GetDescription(i => i.Manual), FieldType = DataType.Boolean.ToString(), Value = a.Manual.ToString().ToLower() });
            list.Add(new EditableField { Row = 3, Column = 2, FieldName = "Enabled", Name = a.GetName(i => i.Enabled), FieldDescription = a.GetDescription(i => i.Enabled), FieldType = DataType.Boolean.ToString(), Value = a.Enabled.ToString().ToLower() });

            var intervalTypes = new List<SelectListItem>();
            intervalTypes.Add(new SelectListItem { Text = "Minute(s)", Value = "3" });
            intervalTypes.Add(new SelectListItem { Text = "Hour(s)", Value = "2" });
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "IntervalType", Name = a.GetName(i => i.IntervalType), FieldDescription = a.GetDescription(i => i.IntervalType), FieldType = DataType.Lookup.ToString(), Items = intervalTypes, Value = a.IntervalType.HasValue ? ((int)a.IntervalType.Value).ToString() : "" });
            list.Add(new EditableField { Row = 4, Column = 2, Required = true,  FieldName = "Interval", Name = a.GetName(i => i.Interval), FieldDescription = a.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString(), Value = (a.Interval.HasValue ? a.Interval.Value.ToString() : "") ,Validations= checkAndAddValidation("Number", "Interval", true, "([1-9]|[1-8][0-9]|9[0-9]|[1-8][0-9]{2}|9[0-8][0-9]|99[0-9]|[1-8][0-9]{3}|9[0-8][0-9]{2}|99[0-8][0-9]|999[0-9]|10000)", null, null, "Please enter value between 1,10000.") });

            list.Add(new EditableField { Row = 5, Column = 1, FieldName = "ForceRefresh", Name = "Force Refresh on Next Run?", FieldDescription = "Force the local agent to perform a full refresh of this configuration on the next run.", FieldType = DataType.Boolean.ToString(), Value = a.ForceRefresh.GetValueOrDefault().ToString().ToLower() });
            list.Add(new EditableField { Row = 5, Column = 2, FieldName = "LockPromotedItems", Name = a.GetName(i => i.LockPromotedItems), FieldDescription = a.GetDescription(i => i.LockPromotedItems), FieldType = DataType.Boolean.ToString(), Value = a.LockPromotedItems.ToString().ToLower() });

            var owners = Company.GetFusionOwnerOptions()
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = $"{i.ID}",
                    Selected = a.FusionOwners.Any(c => c.ObjectID == i.ID && c.Object=="Artifact")
                }).ToList();
                        
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "Owners", Name = "Owners", FieldDescription = "You must assign one or more owners for this configuration.", FieldType = DataType.Lookup.ToString(), MultiSelect = true, Items = owners });

            list =
                loadDynamicFields(
                    SystemObjects.Fusion.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.FusionType, a.FusionTypeID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Fusion, id).ToList(),
                    7
               );           

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFusion")]
        public JsonResult AddFusion(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                int typeID = parseIntField(form, "FusionTypeID");
                var type = Company.GetById<FusionType>(typeID);
                if (type == null) throw new NotFoundException("fusion type");

                if (!Company.HasAssetTypePermission(SystemObjects.FusionType, typeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var rawOwners = parseTextField(form, "Owners");
                if (string.IsNullOrEmpty(rawOwners))
                    return jsonException("No selected owners", HttpStatusCode.BadRequest);

                var items = rawOwners.Split(',').ToList().Select(i => int.Parse(i)).ToList();

                var ownerArtifacts = Company.Filter<Asset>(i => items.Contains(i.ObjectID) && i.Object=="Artifact").ToList();

                var model = new Fusion
                {
                    FusionType = type,
                    FusionTypeID = typeID,
                    Description = parseTextField(form, "Description"),
                    LockPromotedItems = parseBooleanField(form, "LockPromotedItems"),
                    Enabled = parseBooleanField(form, "Enabled"),
                    IntervalType = (JobIntervalType)Enum.Parse(typeof(JobIntervalType), form["IntervalType"]),
                    Interval = parseIntField(form, "Interval"),
                    Manual = parseBooleanField(form, "Manual"),
                    Name = parseTextField(form, "Name"),
                    FusionOwners = ownerArtifacts
                };

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Fusion, model.ID, Company.GetFieldTypesByObject(SystemObjects.FusionType, typeID).ToList(), form, Server);

                Company.SaveOrUpdate<Fusion>(model, fields);

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

        [HttpDelete, Route("DeleteFusion")]
        public JsonResult DeleteFusion(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var model = Company.GetById<Fusion>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("configuration");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.ID, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                //check if the fusion config has data, if so dont allow the delete and popup a friendly message

                if(Company.FusionAttributes.Any(x=>x.FusionID == model.ID))
                    return jsonException("The selected fusion configuration contains data, and therefore cannot be deleted.", HttpStatusCode.Forbidden);

                Company.Delete<Fusion>(model);
                return jsonSuccess("Item successfully removed.", model.ID.ToString(), "delete", HttpStatusCode.OK);
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
        
        [HttpPut, ValidateInput(false), Route("EditFusion")]
        public JsonResult EditFusion(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var model = Company.GetById<Fusion>(parseIntField(form, "ID"), i => i.FusionOwners);
                if (model == null) throw new NotFoundException("configuration");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.ID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var rawOwners = parseTextField(form, "Owners");
                if (string.IsNullOrEmpty(rawOwners))
                    return jsonException("No selected owners", HttpStatusCode.BadRequest);

                var items = rawOwners.Split(',').ToList().Select(i => int.Parse(i)).ToList();

                var ownerArtifacts = Company.Filter<Asset>(i => items.Contains(i.ObjectID) && i.Object=="Artifact").ToList();

                model.Description = parseTextField(form, "Description");
                model.Enabled = parseBooleanField(form, "Enabled");
                model.LockPromotedItems = parseBooleanField(form, "LockPromotedItems");
                model.Manual = parseBooleanField(form, "Manual");
                model.Name = parseTextField(form, "Name");
                model.IntervalType = (JobIntervalType)Enum.Parse(typeof(JobIntervalType), form["IntervalType"]);
                model.Interval = parseIntField(form, "Interval");
                model.ForceRefresh = parseBooleanField(form, "ForceRefresh");

                #region  See which ones to add.
                ownerArtifacts.ForEach(no =>
                {
                    if (!model.FusionOwners.Any(co => co.ID == no.ID))
                    {
                        model.FusionOwners.Add(no);
                    }
                });
                #endregion

                #region See which ones to delete.
                var ownersToRemove = new List<Asset>();
                foreach(var co in model.FusionOwners)
                {
                    if (!ownerArtifacts.Any(no => no.ID == co.ID))
                    {
                        ownersToRemove.Add(co);
                    }
                }
                ownersToRemove.ForEach(o =>
                {
                    model.FusionOwners.Remove(o);
                });
                #endregion

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Fusion, model.ID, Company.GetFieldTypesByObject(SystemObjects.FusionType, model.FusionTypeID).ToList(), form, Server, false);

                Company.SaveOrUpdate<Fusion>(model, fields, -1,true);

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

        #endregion

        #endregion

        #region FusionRule

        #region Form Get/Post

        [HttpGet, Route("GetAddFusionRule"), NonNullableParameters]
        public JsonNetResult GetAddFusionRule(int typeID, int fusionID)
        {
            var attributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == typeID).Select(i => new { i.ID, Name = i.TextPath, @Type = "FusionAttributeType" }).ToList();
            attributeTypes.AddRange(Company.Filter<FusionQueryAttributeType>(i => i.FusionID == fusionID).Select(i => new { i.ID, Name = "Query :: " + i.Name, @Type = "FusionQueryAttributeType" }).ToList());
            attributeTypes = attributeTypes.AsEnumerable().OrderBy(a => a.Name).ToList();

            return new JsonNetResult
            {
                Data = attributeTypes,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("PostAddFusionRule")]
        public JsonResult PostAddFusionRule(FusionRule r)
        {
            try
            {
                if (!Company.HasAssetPermission(SystemObjects.Fusion, r.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var rule = new FusionRule
                {
                    Enabled = r.Enabled,
                    Description = r.Description,
                    FusionID = r.FusionID,
                    ObjectID = r.ObjectID,
                    ObjectType = r.ObjectType,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                Company.Add(rule);
                Company.SaveChanges();

                //automatically add all items for query attribute types
                var exists = Company.FusionRuleItem.Any(i => i.RuleID == rule.ID && i.ObjectType == "FusionQueryAttributeType");
                if (r.ObjectType == "FusionQueryAttributeType" && !exists)
                {
                    var item = new FusionRuleItem();
                    item.ObjectType = "FusionQueryAttribute";
                    item.ObjectID = null;
                    item.RuleID = rule.ID;

                    Company.Add(item);
                }

                return jsonSuccess("Items marked for auto-promotion", "0", "add", HttpStatusCode.Created);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFusionRule")]
        public JsonResult AddFusionRule(FormCollection form)
        {
            try
            {
                var item = new FusionRule
                {
                    Enabled = parseBooleanField(form, "Enabled"),
                    Description = parseTextField(form, "Description"),
                    FusionID = parseIntField(form, "FusionID"),
                    ObjectID = parseIntField(form, "FusionAttributeTypeID"),
                    ObjectType = "FusionAttributeType",
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                if (!Company.HasAssetPermission(SystemObjects.Fusion, item.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Add(item);

                return jsonSuccess("Items marked for auto-promotion", "0", "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteFusionRule")]
        public JsonResult DeleteFusionRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                var rule = Company.GetById<FusionRule>(id);

                if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<FusionRuleItem>(i => i.RuleID == id);
                Company.Delete(rule);

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

        [HttpDelete, Route("DeleteFusionRuleById"), NonNullableParameters]
        public JsonResult DeleteFusionRuleById(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionRule(form);
        }

        [HttpGet, Route("GetEditFusionRule"), NonNullableParameters]
        public JsonNetResult GetEditFusionRule(int id)
        {
            var a = Company.GetById<FusionRule>(id);
            if (a == null) return null;

            var attributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == a.Fusion.FusionTypeID).OrderBy(x=>x.TextPath).Select(i => new { i.ID, Name = i.TextPath, @Type = "FusionAttributeType" }).ToList();
            attributeTypes.AddRange(Company.Filter<FusionQueryAttributeType>(i => i.FusionID == a.FusionID).Select(i => new { i.ID, Name = "Query :: " + i.Name, @Type = "FusionQueryAttributeType" }).ToList());


            var model = new FusionRuleEditorModel
            {
                FusionID = a.Fusion.ID,
                FusionTypeID = a.Fusion.FusionTypeID,
                FormUri = "/Form/EditFusionRule",
                FormMethod = "PUT",
                FormName = "Edit Fusion Rule",
                Rule = a
            };

            return new JsonNetResult
            {
                Data = new { model, attributeTypes },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("PostEditFusionRule")]
        public JsonResult PostEditFusionRule(FusionRule r)
        {
            try
            {
                var model = Company.GetById<FusionRule>(r.ID);
                if (model == null) throw new NotFoundException("promotion rule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var type = model.ObjectType;

                model.Enabled = r.Enabled;
                model.Description = r.Description;
                model.FusionID = r.FusionID;
                model.ObjectID = r.ObjectID;
                model.ObjectType = r.ObjectType;

                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update(model);

                if (model.ObjectType != type)
                {
                    //if the type has changed, delete the rule items
                    Company.Delete<FusionRuleItem>(i => i.RuleID == model.ID);

                    //if the type was changed to query attribute, add the item record
                    if (model.ObjectType == "FusionQueryAttributeType")
                    {
                        var item = new FusionRuleItem();
                        item.RuleID = model.ID;
                        item.ObjectType = "FusionQueryAttribute";
                        item.ObjectID = null;

                        Company.Add(item);
                    }
                }

                return jsonSuccess("Fusion rule successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditFusionRule")]
        public JsonResult EditFusionRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion rule");

                var model = Company.GetById<FusionRule>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("promotion rule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Enabled = parseBooleanField(form, "Enabled");
                model.Description = parseTextField(form, "Description");
                model.FusionID = parseIntField(form, "FusionID");
                model.ObjectID = parseIntField(form, "FusionAttributeTypeID");
                model.ObjectType = "FusionAttributeType";

                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update(model);

                return jsonSuccess("Fusion rule successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        #region FusionRuleFilter

        string getFusionRuleFilterSql(FusionRuleFilterEditorModel form, bool getNameColumn = true)
        {
            try
            {
                var rule = Company.GetById<FusionRule>(form.FusionRuleID);

                if (rule == null)
                    throw new NotFoundException("Fusion Rule");

                var sql = "";
                var columnSql = "A.ID";
                var whereSql = "";
                var isQuery = (rule.ObjectType == "FusionQueryAttributeType");

                if (isQuery)
                {
                    if (getNameColumn) columnSql = "A.DisplayValue as Name";
                    sql = $"select {columnSql} from FusionQueryAttribute A inner join FusionQueryAttributeType T on T.ID = A.FusionQueryAttributeTypeID ";
                    whereSql = $"where T.FusionID = {rule.FusionID} and A.FusionQueryAttributeTypeID = {rule.ObjectID} and A.Deleted = 0 ";
                }
                else
                {
                    if (getNameColumn) columnSql = "coalesce(A.TextPath, A.Name) as Name";
                    sql = $"select {columnSql} from FusionAttribute A ";
                    whereSql = $"where A.FusionID = {rule.FusionID} and A.FusionAttributeTypeID = {rule.ObjectID} and A.Deleted = 0 ";
                }

                if (!form.All)
                {
                    if (form.Items.Count == 0)
                        throw new NotFoundException("Fusion Rule Filter Fields");

                    List<int> usedFieldTypeIDs = new List<int>();

                    foreach (var f in form.Items)
                    {
                        var queryFormat = "";
                        switch (f.Operator)
                        {
                            case "Contains":
                                queryFormat = "{0} like '%{1}%'";
                                break;
                            case "Does Not Contain":
                                queryFormat = "{0} not like '%{1}%'";
                                break;
                            case "Ends With":
                                queryFormat = "{0} like '%{1}'";
                                break;
                            case "Does Not End With":
                                queryFormat = "{0} not like '%{1}'";
                                break;
                            case "Does Not Equal":
                                queryFormat = "{0} <> '{1}'";
                                break;
                            case "Starts With":
                                queryFormat = "{0} like '{1}%'";
                                break;
                            case "Does Not Start With":
                                queryFormat = "{0} not like '{1}%'";
                                break;
                            default: //Equals
                                queryFormat = "{0} = '{1}'";
                                break;
                        }

                        if (f.FieldTypeID == 0)
                        {
                            whereSql += " and " + string.Format(queryFormat, (isQuery) ? "A.DisplayValue" : "A.Name", f.Value.Replace("'", "''"));
                        }
                        else if (f.FieldTypeID == -1)
                        {
                            whereSql += " and " + string.Format(queryFormat, (isQuery) ? "A.DisplayValue" : "A.TextPath", f.Value.Replace("'", "''"));
                        }
                        else
                        {
                            if (f.Type == "Boolean" && string.IsNullOrEmpty(f.Value)) f.Value = "false";

                            if (!string.IsNullOrEmpty(f.Value))
                            {
                                var fieldTypeCount = usedFieldTypeIDs.Count(i => i == f.FieldTypeID);
                                var alias = f.FieldTypeID.ToString() + (fieldTypeCount > 0 ? $"_{fieldTypeCount}" : "");
                                usedFieldTypeIDs.Add(f.FieldTypeID);

                                sql += $" inner join Field F{alias} on F{alias}.FieldTypeID = {f.FieldTypeID} and F{alias}.ObjectType = '{rule.ObjectType.Replace("Type", "")}' and F{alias}.ObjectID = A.ID and {string.Format(queryFormat, $"F{alias}.FormattedValue", f.Value.Replace("'", "''"))}";

                            }
                        }
                    }
                }

                sql += " " + whereSql;

                return sql;
            }
            catch 
            {
                throw;
            }
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("TestFusionRuleFilter")]
        public JsonNetResult TestFusionRuleFilter(FusionRuleFilterEditorModel form)
        {
            try
            {
                var sql = getFusionRuleFilterSql(form);
                return new JsonNetResult { Data = Company.Query<dynamic>(sql), Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (BaseException ex)
            {
                return jsonNetException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonNetException(ex, HttpStatusCode.InternalServerError);
            }
        }

        #region Form Get/Post

        [HttpGet, Route("GetAddFusionRuleFilter"), NonNullableParameters]
        public JsonNetResult GetAddFusionRuleFilter(int id)
        {
            var rule = Company.GetById<FusionRule>(id);

            if (rule == null)
                return null;

            if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                return jsonNetException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionRuleFilterEditorModel { FusionRuleID = id, All = false };

            editorModel.FieldTypes.AddRange(
                Company
                    .Filter<FieldType>(i => i.Object == rule.ObjectType && i.ObjectID == rule.ObjectID && (i.Type == "Text" || i.Type == "Boolean"))
                    .OrderBy(i => i.FriendlyName)
                    .Select(i => new FusionRuleFilterFieldEditorModel { ID = i.ID, Name = i.FriendlyName, Type = i.Type })
            );

            if (rule.ObjectType == "FusionAttributeType")
            {
                editorModel.FieldTypes.Insert(0, new FusionRuleFilterFieldEditorModel { ID = 0, Name = "Name", Type = core.DataType.Text.ToString() });
                editorModel.FieldTypes.Insert(1, new FusionRuleFilterFieldEditorModel { ID = -1, Name = "Text Path", Type = core.DataType.Text.ToString() });
            }

            return new JsonNetResult
            {
                Data = editorModel,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFusionRuleFilter")]
        public JsonResult AddFusionRuleFilter(FusionRuleFilterEditorModel form)
        {
            try
            {
                int ruleID = form.FusionRuleID;
                var rule = Company.GetById<FusionRule>(ruleID);

                if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var filter = new FusionRuleFilter { RuleID = ruleID, Name = form.Name, Items = form.Items, All = form.All };
                var fieldsXml = new XElement("fields");
                filter.Items.ForEach(f =>
                {
                    fieldsXml.Add(new XElement("field",
                        new XElement("FieldTypeID", f.FieldTypeID),
                        new XElement("Operator", f.Operator),
                        new XElement("Value", f.Value)
                    ));
                });
                filter.FieldsDocument = fieldsXml;
                filter.Sql = getFusionRuleFilterSql(form, false);
                
                Company.Add(filter);

                if (rule != null)
                {
                    rule.UpdatedBy = Company.CurrentResourceID;
                    rule.UpdatedOn = DateTime.UtcNow;
                }

                Company.SaveChanges();

                return jsonSuccess("Filter successfully created.", filter.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpGet, Route("GetEditFusionRuleFilter"), NonNullableParameters]
        public JsonNetResult GetEditFusionRuleFilter(int id)
        {
            var filter = Company.GetById<FusionRuleFilter>(id, i => i.FusionRule);

            if (filter == null)
                return null;

            if (!Company.HasAssetPermission(SystemObjects.Fusion, filter.FusionRule.FusionID, Permission.ModifyAsset))
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var editorModel = new FusionRuleFilterEditorModel { FusionRuleID = filter.RuleID, ID = filter.ID, Name = filter.Name, All = filter.All };

            editorModel.FieldTypes.AddRange(
                Company
                    .Filter<FieldType>(i => i.Object == filter.FusionRule.ObjectType && i.ObjectID == filter.FusionRule.ObjectID && (i.Type == "Text" || i.Type == "Boolean"))
                    .OrderBy(i => i.FriendlyName)
                    .Select(i => new FusionRuleFilterFieldEditorModel { ID = i.ID, Name = i.FriendlyName, Type = i.Type })
            );

            if (filter.FusionRule.ObjectType == "FusionAttributeType")
            {
                editorModel.FieldTypes.Insert(0, new FusionRuleFilterFieldEditorModel { ID = 0, Name = "Name", Type = core.DataType.Text.ToString() });
                editorModel.FieldTypes.Insert(1, new FusionRuleFilterFieldEditorModel { ID = -1, Name = "Text Path", Type = core.DataType.Text.ToString() });
            }

            foreach (var f in filter.FieldsDocument.Elements("field"))
            {
                var ft = editorModel.FieldTypes.FirstOrDefault(o => o.ID == int.Parse(f.Element("FieldTypeID").Value));
                if (ft != null)
                {
                    editorModel.Items.Add(new FusionRuleFilterItem { Type = ft.Type, FieldTypeID = int.Parse(f.Element("FieldTypeID").Value), Operator = f.Element("Operator").Value, Value = f.Element("Value").Value });
                }
            }

            return new JsonNetResult
            {
                Data = editorModel,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPut, ValidateInput(false), Route("EditFusionRuleFilter")]
        public JsonResult EditFusionRuleFilter(FusionRuleFilterEditorModel form)
        {
            try
            {
                var filter = Company.GetById<FusionRuleFilter>(form.ID.Value, i => i.FusionRule);

                if (filter != null)
                {
                    if (!Company.HasAssetPermission(SystemObjects.Fusion, filter.FusionRule.FusionID, Permission.ModifyAsset))
                        return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                    filter.Items = form.Items;

                    var fieldsXml = new XElement("fields");
                    filter.Items.ForEach(f =>
                    {
                        fieldsXml.Add(new XElement("field",
                            new XElement("FieldTypeID", f.FieldTypeID),
                            new XElement("Operator", f.Operator),
                            new XElement("Value", f.Value)
                        ));
                    });
                    filter.FieldsDocument = fieldsXml;
                    filter.Sql = getFusionRuleFilterSql(form, false);

                    filter.Name = form.Name;
                    filter.All = form.All;
                    filter.FusionRule.UpdatedBy = Company.CurrentResourceID;
                    filter.FusionRule.UpdatedOn = DateTime.UtcNow;

                    Company.SaveChanges();
                }

                return jsonSuccess("Filter successfully updated.", filter.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteFusionRuleFilter")]
        public JsonResult DeleteFusionRuleFilter(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                var filter = Company.GetById<FusionRuleFilter>(id, i => i.FusionRule);
                if (filter != null)
                {
                    if (!Company.HasAssetPermission(SystemObjects.Fusion, filter.FusionRule.FusionID, Permission.ModifyAsset))
                        return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                    if (filter.FusionRule != null)
                    {
                        filter.FusionRule.UpdatedBy = Company.CurrentResourceID;
                        filter.FusionRule.UpdatedOn = DateTime.UtcNow;
                    }
                    Company.Delete(filter);
                }
                return jsonSuccess("Filter successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteFusionRuleFilterByID"), NonNullableParameters]
        public JsonResult DeleteFusionRuleFilterByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionRuleFilter(form);
        }

        #endregion

        #endregion

        #region FusionRuleItem

        
        #region Form Get/Post

        [HttpGet, Route("GetAddFusionRuleItem"), NonNullableParameters]
        public JsonNetResult GetAddFusionRuleItem(int id)
        {
            var rule = Company.GetById<FusionRule>(id);

            if (rule == null)
                return null;

            if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                return jsonNetException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionRuleItemEditorModel
            {
                FormUri = "/Form/AddFusionRuleItem",
                FormMethod = "POST",
                FormName = "Add Promotion Target Item",
                FusionID = rule.FusionID,
                TargetFusionAttributeTypeID = rule.ObjectID,
                Items = Company.FusionRuleItem.Where(i => i.RuleID == id).ToList()
            };

            return new JsonNetResult
            {
                Data = editorModel,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        
        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false),Route("PostAddFusionRuleItem")]
        public JsonResult PostAddFusionRuleItem(FusionAddItemModel form)
        {
            try
            {
                int ruleID = form.RuleID;
                var rule = Company.GetById<FusionRule>(ruleID);

                if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                bool allSelected = form.AllSelected;
                List<string> attributes = new List<string>();

                if (!string.IsNullOrEmpty(form.attributeIDs))
                    attributes = form.attributeIDs.Split(',').ToList();

                if(attributes.Count == 0 && allSelected)
                {
                    Company.Set<FusionRuleItem>().Add(
                        new FusionRuleItem { RuleID = ruleID, ObjectID = null, ObjectType = form.ObjectType }
                    );
                }
                else
                {
                    attributes.ForEach(fa =>
                    {
                        int? attributeID = null;
                        if (!string.IsNullOrEmpty(fa))
                        {
                            attributeID = int.Parse(fa);
                        }

                        var existing = Company.FusionRuleItem.Any(i => i.RuleID == ruleID && i.ObjectID == attributeID && i.ObjectType == form.ObjectType);
                        if (!existing)
                            Company.Set<FusionRuleItem>().Add(
                                new FusionRuleItem { RuleID = ruleID, ObjectID = attributeID, ObjectType = form.ObjectType }
                            );
                    });
                }

                if (rule != null)
                {
                    rule.UpdatedBy = Company.CurrentResourceID;
                    rule.UpdatedOn = DateTime.UtcNow;
                }

                Company.SaveChanges();

                return jsonSuccess("Target item(s) successfully created.", "0", "add", HttpStatusCode.Created);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFusionRuleItem")]
        public JsonResult AddFusionRuleItem(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "RuleID");
                var rule = Company.GetById<FusionRule>(ruleID);

                if (rule == null)
                    throw new NotFoundException("fusion rule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                rule.UpdatedBy = Company.CurrentResourceID;
                rule.UpdatedOn = DateTime.UtcNow;

                var fusionAttributeIDs = form["FusionAttributeID"].Split(',').ToList();
                if (fusionAttributeIDs.Count == 0)
                {
                    Company.Set<FusionRuleItem>().Add(
                        new FusionRuleItem { RuleID = ruleID, ObjectID = null }
                        );
                }
                else
                {
                    fusionAttributeIDs.ForEach(fa =>
                    {
                        int? fusionAttributeID = null;
                        if (!string.IsNullOrEmpty(fa))
                        {
                            fusionAttributeID = int.Parse(fa);
                        }
                        Company.Set<FusionRuleItem>().Add(
                            new FusionRuleItem { RuleID = ruleID, ObjectID = fusionAttributeID }
                            );
                    });
                }
                Company.SaveChanges();

                return jsonSuccess("Target item(s) successfully created.", "0", "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteFusionRuleItem")]
        public JsonResult DeleteFusionRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                var item = Company.GetById<FusionRuleItem>(id, i => i.FusionRule);

                if (item == null)
                    throw new NotFoundException("fusion rule item");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, item.FusionRule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                item.FusionRule.UpdatedBy = Company.CurrentResourceID;
                item.FusionRule.UpdatedOn = DateTime.UtcNow;

                Company.FusionRuleItem.Remove(item);
                Company.SaveChanges();

                return jsonSuccess("Target item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteFusionRuleItemByID"), NonNullableParameters]
        public JsonResult DeleteFusionRuleItemByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionRuleItem(form);
        }

        #endregion

        #endregion

        #region FusionRuleStep

        #region Form Get/Post

        [HttpGet, Route("GetAddFusionRuleStep"), NonNullableParameters]
        public JsonNetResult GetAddFusionRuleStep(int ruleID)
        {
            if (ruleID <= 0) return null;

            var rule = Company.GetById<FusionRule>(ruleID);

            if (rule == null) return null;

            return new JsonNetResult
            {
                Data = new FusionRuleStepEditorModel
                {
                    FormUri = "/form/AddFusionRuleStep",
                    FormMethod = "POST",
                    RuleStep = new FusionRuleStep { Action = "promote", Step = rule.FusionRuleSteps.Count + 1, RuleID = ruleID, FusionRule = rule },
                    FormName = "Add Fusion Rule Step",
                    FusionID = rule.FusionID,
                    FusionTypeID = rule.Fusion.FusionTypeID
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false),  Route("PostAddFusionRuleStep")]
        public JsonResult PostAddFusionRuleStep(FusionRuleStep s)
        {
            try
            {
                var ruleID = s.RuleID;

                if (ruleID <= 0) return jsonException("", HttpStatusCode.NotFound, "");

                var rule = Company.GetById<FusionRule>(ruleID);

                if (rule == null)
                    throw new NotFoundException("fusion rule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var item = new FusionRuleStep
                {
                    Action = s.Action,
                    Description = s.Description,
                    Step = s.Step,
                    RuleID = rule.ID
                };

                rule.FusionRuleSteps.Add(item);
                if (rule != null)
                {
                    rule.UpdatedBy = Company.CurrentResourceID;
                    rule.UpdatedOn = DateTime.UtcNow;
                }

                Company.SaveChanges();

                foreach (var setting in s.Settings)
                {
                    if (!string.IsNullOrEmpty(setting.Value))
                    {
                        Company.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = setting.Key, Value = setting.Value });
                    }
                }

                Company.SaveChanges();

                return jsonSuccess("New Fusion Rule Step Added", "0", "add", HttpStatusCode.Created);
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
        
        [HttpGet, Route("GetEditFusionRuleStep"), NonNullableParameters]
        public JsonNetResult GetEditFusionRuleStep(int ruleID, int ruleStepID)
        {
            var rule = Company.GetById<FusionRule>(ruleID);
            if (rule == null) return null;

            var step = rule.FusionRuleSteps.SingleOrDefault(x => x.ID == ruleStepID);
            if (step == null) return null;

            step.Settings.Add("Search", step.GetSettingValueByName("Search"));
            step.Settings.Add("ID", step.GetSettingValueByName("ID"));

            step.Settings.Add("SubjectSearch", step.GetSettingValueByName("SubjectSearch"));
            step.Settings.Add("Subject", step.GetSettingValueByName("Subject"));
            step.Settings.Add("SubjectID", step.GetSettingValueByName("SubjectID"));
            step.Settings.Add("ObjectSearch", step.GetSettingValueByName("ObjectSearch"));
            step.Settings.Add("Object", step.GetSettingValueByName("Object"));
            step.Settings.Add("ObjectID", step.GetSettingValueByName("ObjectID"));

            step.Settings.Add("TechnicalSubjectSearch", step.GetSettingValueByName("TechnicalSubjectSearch"));
            step.Settings.Add("TechnicalSubject", step.GetSettingValueByName("TechnicalSubject"));
            step.Settings.Add("TechnicalSubjectID", step.GetSettingValueByName("TechnicalSubjectID"));

            step.Settings.Add("TechnicalObjectSearch", step.GetSettingValueByName("TechnicalObjectSearch"));
            step.Settings.Add("TechnicalObject", step.GetSettingValueByName("TechnicalObject"));
            step.Settings.Add("TechnicalObjectID", step.GetSettingValueByName("TechnicalObjectID"));

            step.Settings.Add("ParentObjectTypeID", step.GetSettingValueByName("ParentObjectTypeID"));
            step.Settings.Add("ParentObjectSearch", step.GetSettingValueByName("ParentObjectSearch"));
            step.Settings.Add("ParentObjectID", step.GetSettingValueByName("ParentObjectID"));

            step.Settings.Add("FilterField", step.GetSettingValueByName("FilterField"));
            step.Settings.Add("TargetField", step.GetSettingValueByName("TargetField"));
            step.Settings.Add("IntersectType", step.GetSettingValueByName("IntersectType"));

            step.Settings.Add("Role", step.GetSettingValueByName("Role"));
            step.Settings.Add("FindParent", step.GetSettingValueByName("FindParent"));

            step.Settings.Add("PromotionFusionAttributeTypeID", step.GetSettingValueByName("PromotionFusionAttributeTypeID"));
            step.Settings.Add("PromotionStepID", step.GetSettingValueByName("PromotionStepID"));

            return new JsonNetResult
            {

                Data = new FusionRuleStepEditorModel
                {
                    FormUri = "/form/EditFusionRuleStep",
                    FormMethod = "PUT",
                    RuleStep = step,
                    FormName = "Edit Fusion Rule Step",
                    FusionID = rule.FusionID,
                    FusionTypeID = rule.Fusion.FusionTypeID
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPut, ValidateInput(false), Route("PutEditFusionRuleStep")]
        public JsonResult PutEditFusionRuleStep(FusionRuleStep s)
        {
            try
            {
                var ruleID = s.RuleID;
                var ruleStepID = s.ID;

                if (ruleID <= 0 || ruleStepID <= 0) return null;

                var rule = Company.GetById<FusionRule>(ruleID);

                if (rule == null)
                    throw new NotFoundException("fusion rule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, rule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var step = rule.FusionRuleSteps.First(x => x.ID == ruleStepID);

                if (step == null) return null;

                step.Description = s.Description;
                step.Step = s.Step;
                step.Action = s.Action;
                step.Settings = s.Settings;

                rule.UpdatedBy = Company.CurrentResourceID;
                rule.UpdatedOn = DateTime.UtcNow;

                //remove old step settings                
                for (int i = step.FusionRuleStepSettings.Count - 1; i >= 0; i--)
                {
                    Company.ObjectContext.DeleteObject(step.FusionRuleStepSettings.ElementAt(i));
                }
                Company.SaveChanges();

                foreach (var setting in s.Settings)
                {
                    if (!string.IsNullOrEmpty(setting.Value))
                    {
                        Company.Add(new FusionRuleStepSetting { RuleStepID = step.ID, Name = setting.Key, Value = setting.Value });
                    }
                }
                Company.SaveChanges();

                return jsonSuccess("Step updated", "0", "add", HttpStatusCode.Accepted);
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

        [HttpPut, ValidateInput(false), Route("MoveFusionRuleStep")]
        public ActionResult MoveFusionRuleStep(int ruleID, int ruleStepID, bool moveUp)
        {
            var direction = moveUp ? "UP" : "DOWN";
            var currentRule = Company.GetById<FusionRule>(ruleID);

            if (currentRule == null)
                throw new NotFoundException("fusion rule");

            if (!Company.HasAssetPermission(SystemObjects.Fusion, currentRule.FusionID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var itemToMove = currentRule.FusionRuleSteps.SingleOrDefault(x => x.ID == ruleStepID);
            int currentStepNumber = itemToMove.Step;

            currentRule.UpdatedBy = Company.CurrentResourceID;
            currentRule.UpdatedOn = DateTime.UtcNow;

            if (string.Compare(direction, "UP", true) == 0)
            {
                //swap item to move and the item above it                
                var itemBeforeSelected = currentRule.FusionRuleSteps.OrderBy(x => x.Step).TakeWhile(x => x.ID != ruleStepID).LastOrDefault();

                if (itemBeforeSelected != null)
                {
                    itemToMove.Step = itemBeforeSelected.Step;
                    itemBeforeSelected.Step = currentStepNumber;
                    Company.Entry(itemToMove).Property(u => u.Step).IsModified = true;
                    Company.Entry(itemBeforeSelected).Property(u => u.Step).IsModified = true;
                    Company.SaveChanges();
                }
            }
            else if (string.Compare(direction, "DOWN", true) == 0)
            {
                var itemAfterSelected = currentRule.FusionRuleSteps.OrderBy(x => x.Step).SkipWhile(p => p.ID != ruleStepID)
                                  .ElementAt(1); //Zero-indexed, means second

                if (itemAfterSelected != null)
                {
                    itemToMove.Step = itemAfterSelected.Step;
                    itemAfterSelected.Step = currentStepNumber;
                    Company.Entry(itemToMove).Property(u => u.Step).IsModified = true;
                    Company.Entry(itemAfterSelected).Property(u => u.Step).IsModified = true;
                    Company.SaveChanges();
                }
            }

            return jsonSuccess("Step successfully moved", ruleID.ToString(), "move", HttpStatusCode.OK);
        }

        #endregion

        #endregion

        #region FusionRuleStepMapping
        
        #region Form Get/Post

        List<SelectListItem> loadSourceItemOptions(FusionRuleStep ruleStep, FusionRuleStepMapping existingItem = null)
        {
            #region Process Source Field Logic

            var sourceFieldIDs = ruleStep.FusionRuleStepMappings.Where(i => i.SourceFieldTypeID > 0).Select(i => i.SourceFieldTypeID).ToList();
            var sourceFieldNames = ruleStep.FusionRuleStepMappings.Where(i => i.SourceFieldTypeID == 0).Select(i => i.SourceFieldName).ToList();

            if (existingItem != null)
            {
                if (existingItem.SourceFieldTypeID > 0)
                {
                    sourceFieldIDs.Remove(existingItem.SourceFieldTypeID);
                }
                else
                {
                    sourceFieldNames.Remove(existingItem.SourceFieldName);
                }
            }

            var sourceFields = Company.Filter<FieldType>(i => i.Object == ruleStep.FusionRule.ObjectType && i.ObjectID == ruleStep.FusionRule.ObjectID)
                .OrderBy(i => i.FriendlyName)
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = string.Format("{0} ({1})", i.FriendlyName, i.Name),
                    Value = string.Format("{0}|{1}", i.Name, i.ID)
                })
                .ToList();

            if (ruleStep.FusionRule.ObjectType == "FusionAttributeType")
            {
                var thisFusionAttributeType = Company.GetById<FusionAttributeType>(ruleStep.FusionRule.ObjectID);
                if (thisFusionAttributeType != null)
                {
                    sourceFields.AddRange(
                        Company
                        .Filter<FieldType>(i => i.Object == ruleStep.FusionRule.ObjectType && i.ObjectID == thisFusionAttributeType.ParentID)
                        .OrderBy(i => i.FriendlyName)
                        .ToList()
                        .Select(i => new SelectListItem {
                            Text = string.Format("Parent.{0} ({1})", i.FriendlyName, i.Name),
                            Value = string.Format("{0}|{1}", i.Name, i.ID)
                        })
                    );
                }
            }

            //These fields do not exists, by default, for fusion query attributes.
            if (ruleStep.FusionRule.ObjectType != SystemObjects.FusionQueryAttributeType.ToString())
            {
                sourceFields.Insert(0, new SelectListItem { Text = "ID", Value = "ID|0" });
                sourceFields.Insert(1, new SelectListItem { Text = "Name", Value = "Name|0" });
                sourceFields.Insert(2, new SelectListItem { Text = "TextPath", Value = "TextPath|0" });
            }

            #endregion

            var selectedID = "";
            if (existingItem != null)
            {
                if (existingItem.SourceFieldTypeID > 0)
                {
                    selectedID = existingItem.SourceFieldTypeID.ToString();
                }
                else
                {
                    selectedID = existingItem.SourceFieldName;
                }
            }

            if (selectedID != null)
            {
                sourceFields.ForEach(i =>
                {
                    if (!string.IsNullOrEmpty(i.Value))
                    {
                        string[] parts = i.Value.Split('|');
                        i.Selected = parts.Length > 1 && parts[0] == selectedID || parts[1] == selectedID;
                    }
                });
            }

            return sourceFields;
        }

        List<SelectListItem> loadTargetItemOptions(FusionRuleStep ruleStep, FusionRuleStepMapping existingItem = null)
        {
            var targetFields = new List<SelectListItem>();

            if (ruleStep.Action.ToLower() == "update")
            {
                //find the referenced step
                int id = 0;
                int.TryParse(ruleStep.GetSettingValueByName("SubjectID"), out id);
                if (id > 0)
                    ruleStep = Company.GetById<FusionRuleStep>(id);
            }

            #region Process Target Field Logic

            var targetFieldIDs = ruleStep.FusionRuleStepMappings.Where(i => i.TargetFieldTypeID > 0).Select(i => i.TargetFieldTypeID).ToList();
            var targetFieldNames = ruleStep.FusionRuleStepMappings.Where(i => i.TargetFieldTypeID == 0).Select(i => i.TargetFieldName).ToList();

            if (existingItem != null)
            {
                if (existingItem.TargetFieldTypeID > 0)
                {
                    targetFieldIDs.Remove(existingItem.TargetFieldTypeID);
                }
                else
                {
                    targetFieldNames.Remove(existingItem.TargetFieldName);
                }
            }
                        
            var promotionType = ruleStep.GetSettingValueByName("Object");            
            int promotionObjectID = 0;
            int.TryParse(ruleStep.GetSettingValueByName("ObjectID"), out promotionObjectID);

            var targetDynamicFields = Company.Filter<FieldType>(i => i.Object == promotionType && i.ObjectID == promotionObjectID)
                .OrderBy(i => i.FriendlyName)
                .ToList()
                .Where(i => !targetFieldIDs.Contains(i.ID))
                .Select(i => new SelectListItem
                {
                    Text = string.Format("{0} ({1})", i.FriendlyName, i.Name),
                    Value = string.Format("{0}|{1}", i.Name, i.ID)
                })
                .ToList();

            switch (promotionType)
            {
                case "ReferenceItemType":
                    if (!targetFieldNames.Contains("Code"))
                        targetFields.Add(new SelectListItem { Text = "Code", Value = "Code|0" });
                    targetFields.AddRange(targetDynamicFields);
                    break;
                case "ArtifactType":
                case "TaxonomyType":                    
                    targetFields.AddRange(targetDynamicFields);
                    break;
                case "IntersectType":
                    targetFields.AddRange(targetDynamicFields);
                    break;
            }

            #endregion

            var selectedID = "";
            if (existingItem != null)
            {
                if (existingItem.TargetFieldTypeID > 0)
                {
                    selectedID = existingItem.TargetFieldTypeID.ToString();
                }
                else
                {
                    selectedID = existingItem.TargetFieldName;
                }
            }

            targetFields.ForEach(i =>
            {
                i.Selected = i.Value.Contains(selectedID);
            });

            return targetFields;
        }

        [HttpGet, Route("GetAddFusionRuleStepMapping"), NonNullableParameters]
        public JsonNetResult GetAddFusionRuleStepMapping(int id)
        {
            var ruleStep = Company.GetById<FusionRuleStep>(id, i => i.FusionRule);

            if (ruleStep == null)
                throw new NotFoundException("fusion rule step");

            if (!Company.HasAssetPermission(SystemObjects.Fusion, ruleStep.FusionRule.FusionID, Permission.ModifyAsset))
                return jsonNetException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionRuleStepMappingEditorModel
            {
                FormUri = "/Form/AddFusionRuleStepMapping",
                FormMethod = "POST",
                FormName = "Add Promotion Field Mapping",
                Item = new FusionRuleStepMapping { RuleStepID = id, FusionRuleStep = ruleStep },
                SourceFields = loadSourceItemOptions(ruleStep),
                TargetFields = loadTargetItemOptions(ruleStep)
            };

            return new JsonNetResult
            {
                Data = editorModel,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("PostAddFusionRuleStepMapping")]
        public JsonResult PostAddFusionRuleStepMapping(FusionRuleStepMapping map)
        {
            try
            {
                var model = new FusionRuleStepMapping
                {
                    RuleStepID = map.RuleStepID,
                    SourceFieldName = map.SourceFieldName,
                    SourceFieldTypeID = map.SourceFieldTypeID,
                    TargetFieldName = map.TargetFieldName,
                    TargetFieldTypeID = map.TargetFieldTypeID
                };

                if (map.IsConstantValue)
                {
                    model.IsConstantValue = true;
                    model.SourceFieldTypeID = 0;
                    model.ConstantValue = map.ConstantValue;
                    model.SourceFieldName = null;
                }
                else
                {
                    model.IsConstantValue = false;
                    model.ConstantValue = string.Empty;
                }

                Company.Add(model);

                var ruleStep = Company.GetById<FusionRuleStep>(model.RuleStepID, i => i.FusionRule);
                if (ruleStep != null)
                {
                    if (!Company.HasAssetPermission(SystemObjects.Fusion, ruleStep.FusionRule.FusionID, Permission.ModifyAsset))
                        return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                    ruleStep.FusionRule.UpdatedBy = Company.CurrentResourceID;
                    ruleStep.FusionRule.UpdatedOn = DateTime.UtcNow;
                    Company.SaveChanges();
                }

                return jsonSuccess("Field mapping successfully created.", "0", "add", HttpStatusCode.Created);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFusionRuleStepMapping")]
        public JsonResult AddFusionRuleStepMapping(FormCollection form)
        {
            try
            {
                var model = new FusionRuleStepMapping
                {
                    RuleStepID = parseIntField(form, "RuleID")
                };

                var source = form["Source"].Split('|');
                var target = form["Target"].Split('|');
                var constantValue = form["ConstantValue"];
                var isConstantValue = form["isConstantValue"];

                if (source[1] == "0")
                {
                    model.SourceFieldName = source[0];
                    model.SourceFieldTypeID = 0;
                }
                else
                    model.SourceFieldTypeID = int.Parse(source[1]);

                if (target[1] == "0")
                {
                    model.TargetFieldName = target[0];
                    model.TargetFieldTypeID = 0;
                }
                else
                    model.TargetFieldTypeID = int.Parse(target[1]);


                if (!string.IsNullOrEmpty(isConstantValue) && isConstantValue.Contains("true"))
                {
                    model.IsConstantValue = true;
                    model.SourceFieldTypeID = 0;
                    model.ConstantValue = constantValue;
                    model.SourceFieldName = null;
                }
                else
                {
                    model.IsConstantValue = false;
                    model.ConstantValue = string.Empty;
                }

                Company.Add(model);

                var ruleStep = Company.GetById<FusionRuleStep>(model.RuleStepID, i => i.FusionRule);
                if (ruleStep != null)
                {
                    if (!Company.HasAssetPermission(SystemObjects.Fusion, ruleStep.FusionRule.FusionID, Permission.ModifyAsset))
                        return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                    ruleStep.FusionRule.UpdatedBy = Company.CurrentResourceID;
                    ruleStep.FusionRule.UpdatedOn = DateTime.UtcNow;
                    Company.SaveChanges();
                }

                return jsonSuccess("Field mapping successfully created.", "0", "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteFusionRuleStep")]
        public JsonResult DeleteFusionRuleStep(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                var ruleStepID = parseIntField(form, "RuleStepID");
                var currentRule = Company.GetById<FusionRule>(id);

                if (currentRule == null)
                    throw new NotFoundException("fusion rule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, currentRule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var itemToRemove = currentRule.FusionRuleSteps.SingleOrDefault(x => x.ID == ruleStepID);

                if (itemToRemove == null) throw new Exception("Fusion Rule Step not found.");                     

                Company.Delete(itemToRemove);

                currentRule.UpdatedBy = Company.CurrentResourceID;
                currentRule.UpdatedOn = DateTime.UtcNow;
                    
                //update the step numbers 
                var steps = currentRule.FusionRuleSteps.OrderBy(x => x.Step);

                for (int i = 0; i < steps.Count(); i++)
                {
                    steps.ElementAt(i).Step = (i + 1);
                }

                Company.SaveChanges();

                return jsonSuccess("Step successfully removed.", id.ToString(), form["_context"], HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteFusionRuleStepByID")]
        public JsonResult DeleteFusionRuleStepByID(int ruleID, int ruleStepID)
        {
            var form = new FormCollection();
            form.Add("ID", ruleID.ToString());
            form.Add("RuleStepID", ruleStepID.ToString());
            
            return DeleteFusionRuleStep(form);
        }

        [HttpDelete, Route("DeleteFusionRuleStepMapping")]
        public JsonResult DeleteFusionRuleStepMapping(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");

                var mapping = Company.GetById<FusionRuleStepMapping>(id, i => i.FusionRuleStep.FusionRule);

                if (mapping == null)
                    throw new NotFoundException("field mapping");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, mapping.FusionRuleStep.FusionRule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(mapping);
                return jsonSuccess("Mapping successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteFusionRuleStepMappingByID"), NonNullableParameters]
        public JsonResult DeleteFusionRuleStepMappingByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionRuleStepMapping(form);
        }

        [Route("GetEditFusionRuleStepMapping"), NonNullableParameters]
        public JsonNetResult GetEditFusionRuleStepMapping(int id)
        {
            var a = Company.GetById<FusionRuleStepMapping>(id);
            if (a == null) return null;

            var editorModel = new FusionRuleStepMappingEditorModel
            {
                FormUri = "/Form/EditFusionAttributePromotionRuleMapping",
                FormMethod = "PUT",
                FormName = "Update Promotion Field Mapping",
                Item = a,
                SourceFields = loadSourceItemOptions(a.FusionRuleStep, a),
                TargetFields = loadTargetItemOptions(a.FusionRuleStep, a)
            };

            return new JsonNetResult
            {
                Data = editorModel,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPut, ValidateInput(false), Route("PutEditFusionRuleStepMapping")]
        public JsonResult PutEditFusionRuleStepMapping(FusionRuleStepMapping map)
        {
            try
            {
                var model = Company.GetById<FusionRuleStepMapping>(map.ID, i => i.FusionRuleStep.FusionRule);

                if (model == null)
                    throw new NotFoundException("field mapping");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionRuleStep.FusionRule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.SourceFieldName = map.SourceFieldName;
                model.SourceFieldTypeID = map.SourceFieldTypeID;
                model.TargetFieldName = map.TargetFieldName;
                model.TargetFieldTypeID = map.TargetFieldTypeID;

                if (map.IsConstantValue)
                {
                    model.IsConstantValue = true;
                    model.SourceFieldTypeID = 0;
                    model.ConstantValue = map.ConstantValue;
                }
                else
                {
                    model.IsConstantValue = false;
                    model.ConstantValue = null;
                }
                model.FusionRuleStep.FusionRule.UpdatedBy = Company.CurrentResourceID;
                model.FusionRuleStep.FusionRule.UpdatedOn = DateTime.UtcNow;

                Company.Update(model);

                return jsonSuccess("Field mapping successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditFusionRuleStepMapping")]
        public JsonResult EditFusionRuleStepMapping(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("field mapping");

                var model = Company.GetById<FusionRuleStepMapping>(parseIntField(form, "ID"), i => i.FusionRuleStep.FusionRule);
                
                if (model == null)
                    throw new NotFoundException("field mapping");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionRuleStep.FusionRule.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var source = form["Source"].Split('|');
                var target = form["Target"].Split('|');
                var constantValue = form["ConstantValue"];
                var isConstantValue = form["isConstantValue"];

                if (source[1] == "0")
                {
                    model.SourceFieldName = source[0];
                    model.SourceFieldTypeID = 0;
                }
                else
                    model.SourceFieldTypeID = int.Parse(source[1]);

                if (target[1] == "0")
                {
                    model.TargetFieldName = target[0];
                    model.TargetFieldTypeID = 0;
                }
                else
                    model.TargetFieldTypeID = int.Parse(target[1]);

                if (!string.IsNullOrEmpty(isConstantValue) && isConstantValue.Contains("true"))
                {
                    model.IsConstantValue = true;
                    model.SourceFieldTypeID = 0;
                    model.ConstantValue = constantValue;
                }
                else
                {
                    model.IsConstantValue = false;
                    model.ConstantValue = null;
                }
                model.FusionRuleStep.FusionRule.UpdatedBy = Company.CurrentResourceID;
                model.FusionRuleStep.FusionRule.UpdatedOn = DateTime.UtcNow;

                Company.Update(model);

                return jsonSuccess("Field mapping successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        #region FusionType

        #region Form Get/Post

        [ActionName("FusionType"), HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("FusionType")]
        public JsonResult PostFusionType(FusionType fusion, ObjectStyle style = null)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new FusionType
                {
                    Description = fusion.Description,
                    Name = fusion.Name
                };

                Company.Add(model);

                if (style != null)
                    upsertObjectStyle(SystemObjects.FusionType, model.ID, style.IconForeColor, style.IconBackColor, model.Name);

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, new { ParentID = 0, Type = "FusionType", Context = "FusionType", Name = model.Name });
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

        [HttpDelete, Route("DeleteFusionType")]
        public JsonResult DeleteFusionType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion type");

                var model = Company.GetById<FusionType>(parseIntField(form, "ID"));

                if (model == null)
                    throw new NotFoundException("fusion type");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.FusionType, model.ID);

                return jsonSuccess("Item successfully removed.", model.ID.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete]
        [Route("DeleteFusionTypeByID"), NonNullableParameters]
        public JsonResult DeleteFusionTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionType(form);
        }

        [ActionName("FusionType"), HttpPut, ValidateInput(false), Route("FusionType")]
        public JsonResult PutFusionType(FusionType fusion, ObjectStyle style = null)
        {
            try
            {
                var model = Company.GetById<FusionType>(fusion.ID);
                if (model == null) throw new NotFoundException("fusion type");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Description = fusion.Description;
                model.Name = fusion.Name;

                Company.Update(model);
                if (style != null)
                    upsertObjectStyle(SystemObjects.FusionType, model.ID, style.IconForeColor, style.IconBackColor, model.Name);

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK, new { ParentID = 0, Type = "FusionType", Context = "FusionType", Name = model.Name });
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

        #region FusionAttribute

        #region Field Generation

        /// <param name="fat">FusionAttributeTypeID</param>
        /// <param name="f">FusionID</param>
        [Route("FusionAttributeType_AddFields"), NonNullableParameters]
        public JsonResult FusionAttribute_AddFields(int fat, int f)
        {
            if (!Company.HasAssetPermission(SystemObjects.Fusion, f, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "FusionAttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = fat.ToString() });
            list.Add(new EditableField { FieldName = "FusionID", FieldType = DataType.Hidden.ToString(), Value = f.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.FusionAttributeType, fat).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeID</param>
        [Route("FusionAttribute_EditFields"), NonNullableParameters]
        public JsonResult FusionAttribute_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionAttribute>(id, i => i.FusionAttributeType);

            if (a == null)
                return jsonException("Fusion attribute not found.", HttpStatusCode.BadRequest, "Not found");

            if (!Company.HasAssetPermission(SystemObjects.Fusion, a.FusionID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            
            list = (
                loadDynamicFields(
                    SystemObjects.FusionAttribute.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.FusionAttributeType, a.FusionAttributeTypeID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.FusionAttribute, a.ID).ToList(), 
                    2, 
                    true
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("EditFusionAttribute"), HttpPut, ValidateInput(false)]
        public JsonResult EditFusionAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Fusion Attribute");

                var id = parseIntField(form, "ID");

                var model = Company.GetById<FusionAttribute>(id);

                if (model == null) throw new NotFoundException("Fusion Attribute");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.FusionAttribute, model.ID, Company.GetFieldTypesByObject(SystemObjects.FusionAttributeType, model.FusionAttributeTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<FusionAttribute>(model, fields);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, new { ObjectType = SystemObjects.FusionAttribute.ToString(), ObjectID = id });
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

        #region Fusion Attribute Type Custom Query

        [HttpDelete, Route("DeleteFusionAttributeTypeCustomQuery")]
        public JsonResult DeleteFusionAttributeTypeCustomQuery(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusionattributetypecustomquery");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FusionAttributeTypeCustomQuery>(id);
                if (model == null) throw new NotFoundException("fusionattributetypecustomquery");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                return jsonSuccess("Override successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFusionAttributeTypeCustomQuery")]
        public JsonResult AddFusionAttributeTypeCustomQuery(FormCollection form)
        {
            try
            {                
                var a = new FusionAttributeTypeCustomQuery
                {
                    FusionID = parseIntField(form, "FusionID"),
                    FusionAttributeTypeID = parseIntField(form, "FusionAttributeTypeID"),
                    Query = parseTextField(form, "Query")
                };

                if (!Company.HasAssetPermission(SystemObjects.Fusion, a.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                // only allow select
                var valid = Company.IsValidReportingQuery(a.Query);
                if (!valid)
                {
                    throw new InvalidFieldException("Query", "not a SELECT statement or recognized query.");
                }

                Company.Add(a);

                return jsonSuccess("Override successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, new { });
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("EditFusionAttributeTypeCustomQuery")]
        public JsonResult EditFusionAttributeTypeCustomQuery(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusionattributetypecustomquery");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FusionAttributeTypeCustomQuery>(id);

                if (model == null) throw new NotFoundException("fusionattributetypecustomquery");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.FusionAttributeTypeID = parseIntField(form, "FusionAttributeTypeID");
                model.Query = parseTextField(form, "Query");

                // only allow select
                var valid = Company.IsValidReportingQuery(model.Query);
                if (!valid)
                {
                    throw new InvalidFieldException("Query", "not a SELECT statement or recognized query.");
                }

                Company.Update(model);

                return jsonSuccess("Override successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        #region FusionAttributeType

        // used by filter icon in fusion page.
        [Route("getfusionattributetypes"), NonNullableParameters]
        public JsonNetResult GetFusionAttributeTypes(int fusionID)
        {
            var model = Company.GetById<Fusion>(fusionID, i => i.FusionType.FusionAttributeTypes);
            return new JsonNetResult
            {
                Data = model.FusionType.FusionAttributeTypes.OrderBy(i => i.TextPath),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }


        #endregion

        #region FusionQueryAttributeType

        protected JsonResult EditFusionQueryAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attibute type");

                var model = Company.GetById<FusionQueryAttributeType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion attibute type");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var sql = parseTextField(form, "Query");

                if (string.IsNullOrEmpty(sql)) throw new NotFoundException("No SQL Specified for Fusion Query Attribute");

                // only allow select
                var valid = Company.IsValidReportingQuery(sql);
                if (!valid)
                {
                    throw new InvalidFieldException("Command Text", "not a SELECT statement or recognized query.");
                }
                
                model.Name = parseTextField(form, "Name");
                model.DisplayFormat = parseTextField(form, "DisplayFormat");
                model.Query = sql;
                Company.Update(model);

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
        
        public JsonResult AddFusionQueryAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attribute type");

                int fusionID = parseIntField(form, "FusionID");
                
                var type = Company.GetById<Fusion>(fusionID);
                if (type == null) throw new NotFoundException("fusion configuration");
                                
                if (!Company.HasAssetPermission(SystemObjects.Fusion, fusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var sql = parseTextField(form, "Query");

                if (string.IsNullOrEmpty(sql)) throw new NotFoundException("No SQL Specified for Fusion Query Attribute");

                //check if it is a select we only allow selects
                var valid = Company.IsValidReportingQuery(sql);
                if (!valid)
                {
                    throw new InvalidFieldException("Command Text", "not a SELECT statement or recognized query.");
                }
                
                var columns = Company.SelectQueryColumns(sql);
                                                
                var model = new FusionQueryAttributeType
                {
                    FusionID = fusionID,
                    Query = sql,                     
                    Name = parseTextField(form, "Name"),
                    DisplayFormat = parseTextField(form, "DisplayFormat")
                };

                Company.Add(model);

                foreach (var column in columns)
                {
                    Company.Add(new FieldType
                    {
                        ObjectID = model.ID,
                        Object = SystemObjects.FusionQueryAttributeType.ToString(),
                        IsListable = true,
                        IsRequired = true,
                        FriendlyName = column,
                        Name = column,
                        MaximumLength = 500,
                        MinimumLength = 1,
                        SortOrder = 1,
                        Type = DataType.Text.ToString()
                    });
                }

                return jsonSuccess(type.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        protected JsonResult DeleteFusionQueryAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attribute type");

                var model = Company.GetById<FusionQueryAttributeType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion query attribute type");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                
                Company.Delete(model);
                return jsonSuccess("Item successfully removed.", model.ID.ToString(), "delete", HttpStatusCode.OK);
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

        #region Fusion Schedule

        [HttpDelete, Route("DeleteFusionSchedule")]
        public JsonResult DeleteFusionSchedule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("delete fusion schedule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FusionSchedule>(id);
                if (model == null) throw new NotFoundException("fusion schedule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.DeleteAsset))
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFusionSchedule")]
        public JsonResult AddFusionSchedule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusionschedule");
                
                var a = new FusionSchedule
                {                    
                    FusionID = parseIntField(form,"FusionID"),
                    FullRefresh = parseBooleanField(form, "FullRefresh"),
                    Day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), form["Day"]),
                    Time = TimeSpan.Parse(parseTextField(form,"Time")),
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                if (!Company.HasAssetPermission(SystemObjects.Fusion, a.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                
                Company.Add(a);

                return jsonSuccess("Fusion schedule successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, new { });
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("EditFusionSchedule")]
        public JsonResult EditFusionSchedule(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var model = Company.GetById<FusionSchedule>(id);

                if (model == null) throw new NotFoundException("fusion schedule");

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.FullRefresh = parseBooleanField(form, "FullRefresh");
                model.Day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), form["Day"]);
                model.Time = TimeSpan.Parse(parseTextField(form, "Time"));                
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update(model);

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

        #region Intersect/Other Relationships

        [HttpDelete, Route("DeleteIntersect"), NonNullableParameters]
        public JsonResult DeleteIntersect(int id)
        {
            try
            {
                var intersect = Company.GetById<Intersect>(id);

                if (intersect == null)
                    throw new NotFoundException("relationship");

                if (
                    !Company.HasAssetPermission(intersect.Subject, intersect.SubjectID, Permission.DeleteRelationships) ||
                    !Company.HasAssetPermission(intersect.Object, intersect.ObjectID, Permission.DeleteRelationships)
                    )
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.DeleteRelationship(id);
                Response.StatusCode = (int)HttpStatusCode.OK;
                Response.StatusDescription = "Successfully unrelated item.";
                return Json(new { message = Response.StatusDescription });
            }
            catch (BaseException ex)
            {
                Response.StatusCode = (int)ex.StatusCode;
                Response.StatusDescription = ex.StatusDescription;
                return Json(new { message = Response.StatusDescription });
            }
            catch (Exception ex)
            {
                SendException(ex);
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Response.StatusDescription = ex.Message;
                return Json(new { message = Response.StatusDescription });
            }
        }

        #endregion

        #region IntersectType


        #region Json Feeds To Support Editing

        [Route("IntersectType_FormData"), NonNullableParameters]
        public JsonNetResult IntersectType_FormData(int id)
        {
            try
            {
                var type = Company.GetById<IntersectType>(id);
                if (type == null) return new JsonNetResult { Data = null };

                var currentIntersects = Company.Filter<Intersect>(i => i.IntersectTypeID == id).Any();

                var model = new Dictionary<string, object> {
                    { "ID", id },
                    { "LimitedChangesOnly", currentIntersects },
                    { "Subject", $"{type.Subject}|{type.SubjectID}" },
                    { "SubjectCardinality", $"{(int)type.SubjectCardinality}" },
                    { "Object", $"{type.Object}|{type.ObjectID}" },
                    { "ObjectCardinality", $"{(int)type.ObjectCardinality}" },
                    { "Predicate", type.PredicateID }
                };

                return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [Route("IntersectType_PredicateOptions"), NonNullableParameters]
        public JsonNetResult IntersectType_PredicateOptions(SystemObjects subject, int subjectID, SystemObjects? @object = null, int objectID = 0, int predicateID = 0)
        {
            try
            {
                var usedPredicateIDs = new List<int>();

                var sSubject = subject.ToString();
                if (@object.HasValue && objectID > 0)
                {
                    var sObject = @object.Value.ToString();
                    usedPredicateIDs.AddRange(Company.Filter<IntersectType>(i => i.Subject == sSubject && i.SubjectID == subjectID && i.Object == sObject && i.ObjectID == objectID && i.PredicateID.HasValue).Select(i => i.PredicateID.Value));
                }
                
                if (predicateID > 0)
                {
                    usedPredicateIDs.Remove(predicateID);
                }

                var models = Company.Table<Predicate>()
                    .ToList()
                    .Where(i => i.Type.AsInfoModel().AllowIntersectTypeAssignment && i.Type.AsInfoModel().AllowEditFromRelationshipEditor && !usedPredicateIDs.Contains(i.ID))
                    .Select(i => new {
                        title = $"{i.Name} / {i.Inverse} ({i.Type.AsInfoModel().Name})",
                        value = i.ID
                    })
                    .OrderBy(i => i.title);

                return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [Route("IntersectType_CardinalityOptions")]
        public JsonNetResult IntersectType_CardinalityOptions()
        {
            var models = Cardinality.One.GetList()
                .Select(i => new { title = i.Name, value = i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("IntersectType_SubjectOptions")]
        public JsonNetResult IntersectType_SubjectOptions()
        {
            var models = Company.GetIntersectTypeOptions()
                .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("IntersectType_ObjectOptions"), NonNullableParameters]
        public JsonNetResult IntersectType_ObjectOptions(SystemObjects type, int id, SystemObjects? side2Type = null, int? side2ID = null, int? predicateID = null)
        {
            try
            {
                var models = Company.GetIntersectTypeOptions(type, id, side2Type, side2ID, predicateID)
                    .Where(i => i.Type != "IntersectType")
                    .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

                return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIntersectType")]
        public JsonResult AddIntersectType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (form == null) throw new NoFormDataException("relationship type");

                var subject = form["Subject"];
                var subjectInfo = subject.Split('|');
                var @object = form["Object"];
                var objectInfo = @object.Split('|');

                var predicate = form["Predicate"];
                int? predicateID = null;

                if (string.IsNullOrEmpty(predicate))
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Please select a predicate for this relationship.");
                }

                predicateID = int.Parse(predicate);

                var predicateModel = Company.GetById<Predicate>(predicateID.Value);

                if (!predicateModel.Type.AsInfoModel().AllowIntersectTypeAssignment)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Not allowed to add a relationship type with this predicate.");
                }
                if ((subject != @object) && !predicateModel.Type.AsInfoModel().AllowDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object must be the same when using this Predicate.");
                }
                if ((subject == @object) && predicateModel.Type.AsInfoModel().ForceDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object may not be the same when using this Predicate.");
                }

                var model = new IntersectType {
                    Subject = subjectInfo[0],
                    SubjectCardinality = parseEnumField<Cardinality>(form, "SubjectCardinality"),
                    SubjectID = int.Parse(subjectInfo[1]),
                    Object = objectInfo[0],
                    ObjectCardinality = parseEnumField<Cardinality>(form, "ObjectCardinality"),
                    ObjectID = int.Parse(objectInfo[1]),
                    IsSystem = false,
                    PredicateID = predicateID
                };
                Company.Add<IntersectType>(model);
                var id = model.ID;

                return jsonSuccess(model.Name + " successfully created.", id.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteIntersectType")]
        public JsonResult DeleteIntersectType(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                if (!form.HasKeys()) throw new NoFormDataException("relationship type");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (Company.Filter<Intersect>(i => i.IntersectTypeID == id).Count() > 0)
                    return jsonException(FormInfo.InUse_Error_Delete, HttpStatusCode.Conflict);
                if (Company.Filter<FieldType>(i => i.LookupObjectID == id && i.Type == "Relationship" && i.LookupObjectType == "IntersectType").Count() > 0)
                    return jsonException(FormInfo.InUse_RelationShipType_Error_Delete, HttpStatusCode.Conflict);

                var model = Company.GetById<IntersectType>(id);
                if (model == null) throw new NotFoundException("relationship type");

                Company.Delete(SystemObjects.IntersectType, id);

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

        [HttpPut, ValidateInput(false), Route("EditIntersectType")]
        public JsonResult EditIntersectType(FormCollection form)
        {
            try
            {
                if (form == null) throw new NoFormDataException("relationship type");

                var id = int.Parse(form["ID"]);

                // Permisisons validation.
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var model = Company.GetById<IntersectType>(id);
                if (model == null) throw new NotFoundException("relationship type");


                var subject = form["Subject"];
                var subjectInfo = subject.Split('|');

                var @object = form["Object"];
                var objectInfo = @object.Split('|');

                var predicate = form["Predicate"];
                int? predicateID = null;

                if (string.IsNullOrEmpty(predicate))
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Please select a predicate for this relationship.");
                }

                predicateID = int.Parse(predicate);

                var predicateModel = Company.GetById<Predicate>(predicateID.Value);

                if (!predicateModel.Type.AsInfoModel().AllowIntersectTypeAssignment)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Not allowed to edit relationship type with this predicate.");
                }
                if ((subject != @object) && !predicateModel.Type.AsInfoModel().AllowDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object must be the same when using this Predicate.");
                }
                if ((subject == @object) && predicateModel.Type.AsInfoModel().ForceDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object may not be the same when using this Predicate.");
                }

                model.Subject = subjectInfo[0];
                model.SubjectCardinality = parseEnumField<Cardinality>(form, "SubjectCardinality");
                model.SubjectID = int.Parse(subjectInfo[1]);
                model.Object = objectInfo[0];
                model.ObjectCardinality = parseEnumField<Cardinality>(form, "ObjectCardinality");
                model.ObjectID = int.Parse(objectInfo[1]);
                model.PredicateID = int.Parse(predicate);

                Company.Update<IntersectType>(model);

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

        #endregion

        #endregion

        #region Issue Types

        [Route("IssueTypeRelation_AddFields"), NonNullableParameters]
        public JsonResult IssueTypeRelation_AddFields(int issueTypeId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "IssueTypeID", FieldType = DataType.Hidden.ToString(), Value = issueTypeId.ToString() });
            
            var availableTypes = Company.Query<SelectListItem>(string.Format(@"select T.ID as [Value], {0} + coalesce(FAT.TextPath, T.[Name]) as [Text]
                from AssetType T
                left join FusionAttributeType FAT on T.[Object] = 'FusionAttributeType' and FAT.ID = T.ObjectID
                where not exists (select 1 from IssueTypeRelation where AssetTypeID = T.ID and IssueTypeID = @issueTypeId)
                order by 2", QueryConstants.HighLevelTypeCaseStatement), new { issueTypeId }).ToList();

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

                Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = (int)model.PrimaryOwnerResourceID, IsOwner = true });
                try
                {
                    if (model.SecondaryOwnerResourceID.HasValue)
                    {
                        if (!model.PrimaryOwnerResourceID.Equals(model.SecondaryOwnerResourceID))
                            Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.SecondaryOwnerResourceID.Value, IsOwner = true });
                    }
                }
                catch
                {
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
                    Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.PrimaryOwnerResourceID.Value, IsOwner = true });
                }
                if (model.SecondaryOwnerResourceID.HasValue)
                {
                    if (!currentGroupUsers.Any(o => o == model.SecondaryOwnerResourceID))
                    {
                        Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.SecondaryOwnerResourceID.Value, IsOwner = true });
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

        #region Supporting Json Feeds


        /// <summary>
        /// Gets a list of fusion attributes based on a search string provided 
        /// that should match part of the TextPath of the fusoin attribute.
        /// </summary>
        /// <param name="intersectID">The intersectID we are searching under.</param>
        /// <param name="phrase">Part of the text path to search for.</param>
        /// <returns>A list of name/value pairs.</returns>
        [Route(""), NonNullableParameters]
        public JsonNetResult MapRule_FindFusion(int intersectID, string phrase)
        {
            var intersect = Company.Filter<IntersectDetail>(i => i.ID == intersectID).FirstOrDefault();
            if (intersect == null)
                return new JsonNetResult { Data = new { message = "Intersect not found." } };

            phrase = $"%{phrase}%";

            var list = Company.Query<dynamic>(@"
select  A.ID,
        A.TextPath,
        T.TextPath as FusionAttributeType,
        F.Name as Fusion
from    FusionAttribute A
        inner join GetFusionAttributesByOwningArtifact(@SubjectID) O on O.ID = A.FusionID 
        and A.TextPath like @phrase
        inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
        inner join Fusion F on F.ID = A.FusionID
order by A.TextPath", new { phrase, intersect.SubjectID });

            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion

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


            var contexts = Company.Query<dynamic>(@"		
                select 
			        D.DisplayValue as [Name],
			        A.[Object] + '|' + cast(A.ObjectID as varchar) as ID,
			        case when A.[Object] = 'Artifact' then
				        'Glossary'
			        when A.[Object] = 'Taxonomy' then
				        'Model'
			        else
				        ''
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
                    sql = @"
select * from (
select 'FusionType|0' as value, 'Fusion' as title
union
select 'ArtifactType|0' as value, 'Glossary' as title
union
select 'TaxonomyType|0' as value, 'Model' as title
union
select 'PolicyType|0' as value, 'Policy' as title
union
select 'ReferenceItemType|0' as value, 'Reference' as title
) O order by title";
                    break;
                #endregion
                case "P":   // Promotion
                    #region
                    sql = @"
select * from (
select 'AttributeType|' + cast(ID as varchar(10)) as value, 'Attribute: ' + Name as title from AttributeType where ParentID is null
union
select 'ArtifactType|' + cast(ID as varchar(10)) as value, 'Artifact: ' + Name as title from ArtifactType
union
select 'TaxonomyType|' + cast(ID as varchar(10)) as value, 'Model: ' + Name as title from TaxonomyType
union
select 'ReferenceItemType|' + cast(ID as varchar(10)) as value, 'Reference Item: ' + Name as title from ReferenceItemType
) O order by title";
                    break;
                #endregion
                case "R":   // Relation
                case "U":   // Unrelation
                    #region
                    sql = @"select 'IntersectType|' + cast(itd.ID as varchar(10)) as value, IName.Name as title from intersecttypedetail itd cross apply dbo.GetIntersectTypeNames(itd.ID) IName	 where itd.IsSystem = 0 or (itd.Subject = 'ReferenceItemType' and itd.Object = 'ReferenceItemType') order by IName.Name";
                    break;
                #endregion
                case "M":   // USers/Groups
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
                models = Company.Query<OptionModel>(sql).OrderBy(i => i.title);

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
                    var dv = document.CreateDataValidation(2, i + 1, model.Lookups.Count + 1, i + 1);
                    CreateExcelList(lookupColumns++, document, "Lookups", dv, model.Lookups.Select(m => m.Value));
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

                        load = new Load
                        {
                            File = stream.ToArray(),
                            Action = model.LoadAction,
                            Extension = extension,
                            Notes = model.Notes,
                            Object = typeInfo[0],
                            ObjectID = int.Parse(typeInfo[1]),
                            DateStarted = DateTime.UtcNow,
                            UpdatedBy = Company.CurrentResourceID
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
                    Company.Add<Load>(load);
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

            var loadColumns = Company.Filter<LoadColumn>(i => i.LoadID == id).OrderBy(i => i.ColumnIndex).ToList();
            var loadItems = Company.Query<dynamic>("select RowIndex, StatusMessage from LoadItem where LoadID = @id and Status = 0 order by RowIndex asc", new { id}).ToList();
            var loadItemColumnss = Company.Query<LoadItemColumn>("select C.* from LoadItem I inner join LoadItemColumn C on C.LoadID = I.LoadID and I.RowIndex = C.RowIndex and I.LoadID = @id and I.Status = 0 order by I.RowIndex asc, C.ColumnIndex asc", new { id }).ToList();

            var document = new SLDocument();
            document.RenameWorksheet("Sheet1", "Items");

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
                foreach (var lic in loadItemColumnss.Where(c => c.RowIndex == (int)i.RowIndex).OrderBy(c => c.ColumnIndex))
                {
                    document.SetCellValue(r, lic.ColumnIndex, lic.Value);
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
            return File(load.File, "application/vnd.ms-excel", $"{load.DateCompleted.ToString()}.xlsx");
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
                
        #region Organization

        #region Field Generation

        [Route("Organization_AddFields"), NonNullableParameters]
        public JsonResult Organization_AddFields(int ot)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "OrganizationTypeID", FieldType = DataType.Hidden.ToString(), Value = ot.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "AdministratorEmail", Name = "Administrator Email", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.OrganizationType, ot).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Organization ID</param>
        [Route("Organization_EditFields"), NonNullableParameters]
        public JsonResult Organization_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Organization>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = Server.HtmlDecode(a.Name), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "AdministratorEmail", Name = "Administrator Email", FieldType = DataType.Text.ToString(), Value = a.AdministratorEmail, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list = (
                loadDynamicFields(
                    SystemObjects.Organization.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.OrganizationType, a.OrganizationTypeID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Organization, id).ToList(),
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("Organization"), Route("Organization")]
        public JsonResult PostOrganization(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("organization");

                int typeID = parseIntField(form, "OrganizationTypeID");
                var type = Company.GetById<OrganizationType>(typeID);

                if (type == null) throw new NotFoundException("organization type");

                var a = new Organization
                {
                    Name = parseTextField(form, "Name"),
                    AdministratorEmail = parseTextField(form, "AdministratorEmail"),
                    OrganizationTypeID = typeID
                };

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);


                if (!regex.IsMatch(a.AdministratorEmail))
                    return jsonException("The email you entered is not valid", HttpStatusCode.Forbidden);

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.OrganizationType, typeID).ToList();
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Organization, a.ID, fieldTypes, form, Server);
                Company.SaveOrUpdate<Organization>(a, fields);

                dynamic custom = new
                {
                    Name = a.Name,
                    action = "add"
                };

                return jsonSuccess("Organization successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [ HttpPut, ActionName("Organization"), Route("Organization")]
        public JsonResult PutOrganization(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var id = parseIntField(form, "ID");
                var existing = Company.GetById<Organization>(id);
                if (existing == null) throw new NotFoundException("organization");

                existing.Name = parseTextField(form, "Name");
                existing.AdministratorEmail = parseTextField(form, "AdministratorEmail");

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);


                if (!regex.IsMatch(existing.AdministratorEmail))
                    return jsonException("The email you entered is not valid", HttpStatusCode.Forbidden);

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.OrganizationType, existing.OrganizationTypeID).ToList();
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Organization, existing.ID, fieldTypes, form, Server, false);
                Company.SaveOrUpdate<Organization>(existing, fields);

                dynamic custom = new
                {
                    Name = existing.Name,
                    action = "edit"
                };

                return jsonSuccess("Organization successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [ HttpDelete, ActionName("Organization"), Route("Organization"), NonNullableParameters]
        public JsonResult DeleteOrganization(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<Organization>(id);
                if (model == null) throw new NotFoundException("organization");

                //get child records
                var domains = Company.Filter<OrganizationDomain>(i => i.OrganizationID == model.ID);
                var invitations = Company.Filter<OrganizationInvitation>(i => i.OrganizationID == model.ID);
                var resources = Company.Filter<OrganizationResource>(i => i.OrganizationID == model.ID);
                var registrations = Company.Filter<OrganizationRegistration>(i => i.OrganizationID == model.ID);


                Company.OrganizationDomains.RemoveRange(domains);
                Company.OrganizationInvitations.RemoveRange(invitations);
                Company.OrganizationResources.RemoveRange(resources);
                Company.OrganizationRegistrations.RemoveRange(registrations);
                                
                model.State = State.Deleted;

                Company.SaveChanges();

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "delete"
                };

                return jsonSuccess("Organization successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Contract

        #region Field Generation

        [Route("Contract_AddFields"), NonNullableParameters]
        public JsonResult Contract_AddFields(int o = 0)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var contractTypes = ContractType.OrganizationTermsOfUse.GetEnumList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField { FieldName = "OrganizationID", FieldType = DataType.Hidden.ToString(), Value = o.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Title", Name = "Title", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Title", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ContractType", Name = "Contract Type", FieldType = DataType.Lookup.ToString(), Items = contractTypes });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Body", Name = "Body", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Contract ID</param>
        [Route("Contract_EditFields"), NonNullableParameters]
        public JsonResult Contract_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Contract>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            var contractTypes = ContractType.OrganizationTermsOfUse.GetEnumList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Title", Name = "Title", FieldType = DataType.Text.ToString(), Value = Server.HtmlDecode(a.Title), Validations = checkAndAddValidation("Text", "Title", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ContractType", Name = "Contract Type", FieldType = DataType.Lookup.ToString(), Value = a.ContractType.ToString(), Items = contractTypes });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Body", Name = "Body", FieldType = DataType.Html.ToString(), Value = a.Body });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [HttpGet, Route("Contract/{id:int}")]
        public JsonResult GetContract(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var contract = Company.GetById<Contract>(id);
            if (contract.PublishedOn.HasValue)
                contract.PublishedOn = new DateTime(contract.PublishedOn.Value.Ticks, DateTimeKind.Utc);
            if (contract.UpdatedOn.HasValue)
                contract.UpdatedOn = new DateTime(contract.UpdatedOn.Value.Ticks, DateTimeKind.Utc);

            return Json(new
            {
                contract.ID,
                contract.Title,
                contract.Body,
                contract.OrganizationID,
                contract.ContractType,
                contract.State,
                PublishedOn = (contract.PublishedOn.HasValue ? ((DateTime)contract.PublishedOn).ToString("o") : null),
                UpdatedOn = (contract.UpdatedOn.HasValue ? ((DateTime)contract.UpdatedOn).ToString("o") : null),
                contract.UpdatedBy,
                contract.CreatedOn,
                contract.CreatedBy
            }
                    , JsonRequestBehavior.AllowGet);
        }

        [HttpPut, Route("Contract")]
        public JsonResult PutContract(Contract model, bool publish = false)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                int id = model.ID;

                if (id < 1)
                    throw new NotFoundException("contract");

                var contract = Company.GetById<Contract>(id);

                if (contract == null)
                    throw new NotFoundException("contract");


                contract.Title = model.Title;
                contract.Body = model.Body;
                contract.ContractType = model.ContractType;
                if (publish)
                {
                    contract.PublishedOn = DateTime.UtcNow;
                    if (contract.ContractType == ContractType.OrganizationTermsOfUse && contract.OrganizationID.HasValue)
                    {
                        var org = Company.GetById<Organization>((int)contract.OrganizationID);
                        org.Accepted = false;
                        org.AcceptedBy = null;
                        org.DateAccepted = null;
                        Company.SaveOrUpdate(org);
                    }
                }

                Company.SaveOrUpdate(contract);

                dynamic custom = new
                {
                    title = model.Title,
                    action = "edit"
                };

                return jsonSuccess($"{model.ContractType.GetDisplayName()} contract successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);

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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("Contract")]
        public JsonResult PostContract(Contract model, bool publish = false)
        {

            try
            {

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var contract = new Contract();
                contract.OrganizationID = model.OrganizationID;
                contract.Title = model.Title;
                contract.Body = model.Body;
                contract.ContractType = model.ContractType;
                if (publish)
                {
                    contract.PublishedOn = DateTime.UtcNow;
                    if (contract.ContractType == ContractType.OrganizationTermsOfUse && contract.OrganizationID.HasValue)
                    {
                        var org = Company.GetById<Organization>((int)contract.OrganizationID);
                        org.Accepted = false;
                        org.AcceptedBy = null;
                        org.DateAccepted = null;
                        Company.SaveOrUpdate(org);
                    }
                }
                    

                Company.Add(contract);

                dynamic custom = new
                {
                    title = contract.Title,
                    action = "add"
                };

                return jsonSuccess($"{contract.ContractType.GetDisplayName()} contract successfully created.", contract.ID.ToString(), "add", HttpStatusCode.Created, custom);


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

        [HttpDelete, ActionName("Contract"), Route("Contract"), NonNullableParameters]
        public JsonResult DeleteContract(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var o = Company.GetById<Contract>(id);
                if (o == null) throw new NotFoundException("contract");

                o.State = State.Deleted;
                Company.SaveOrUpdate(o);

                dynamic custom = new
                {
                    title = o.Title,
                    action = "delete"
                };

                return jsonSuccess($"{o.ContractType.GetDisplayName()} contract successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Domain

        #region Field Generation

        /// <param name="o">Organization ID</param>
        [Route("OrganizationDomain_AddFields"), NonNullableParameters]
        public JsonResult OrganizationDomain_AddFields(int o)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "OrganizationID", FieldType = DataType.Hidden.ToString(), Value = o.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Domain", Name = "Domain", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Domain", true, "", 5, 500) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Organization Domain ID</param>
        [Route("OrganizationDomain_EditFields"), NonNullableParameters]
        public JsonResult OrganizationDomain_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<OrganizationDomain>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Domain", Name = "Domain", FieldType = DataType.Text.ToString(), Value = a.Domain, Validations = checkAndAddValidation("Text", "Domain", true, "", 5, 500) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("OrganizationDomain"), Route("OrganizationDomain")]
        public JsonResult PostOrganizationDomain(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var o = new OrganizationDomain
                {
                    OrganizationID = parseIntField(form, "OrganizationID"),
                    Domain = parseTextField(form, "Domain")
                };

                if (Company.Any<OrganizationDomain>(i => i.OrganizationID == o.OrganizationID && i.Domain == o.Domain))
                    return jsonException("This domain is already part of this organization", HttpStatusCode.Forbidden);

                Company.Add(o);

                dynamic custom = new
                {
                    action = "add"
                };

                return jsonSuccess("Organization domain successfully created.", o.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [ HttpPut, ActionName("OrganizationDomain"), Route("OrganizationDomain")]
        public JsonResult PutOrganizationDomain(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var id = parseIntField(form, "ID");
                var existing = Company.GetById<OrganizationDomain>(id);
                if (existing == null) throw new NotFoundException("organization domain");

                existing.Domain = parseTextField(form, "Domain");

                if (Company.Any<OrganizationDomain>(i => i.OrganizationID == existing.OrganizationID && i.Domain == existing.Domain && i.ID != existing.ID))
                    return jsonException("This domain is already part of this organization", HttpStatusCode.Forbidden);

                Company.Update(existing);

                dynamic custom = new
                {
                    action = "edit"
                };

                return jsonSuccess("Organization domain successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [ HttpDelete, ActionName("OrganizationDomain"), Route("OrganizationDomain"), NonNullableParameters]
        public JsonResult DeleteOrganizationDomain(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<OrganizationDomain>(id);
                if (model == null) throw new NotFoundException("organization domain");

                Company.Delete(model);

                dynamic custom = new
                {
                    action = "delete"
                };

                return jsonSuccess("Organization domain successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Invitation

        #region Field Generation

        /// <param name="o">Organization ID</param>
        [Route("OrganizationInvitation_AddFields"), NonNullableParameters]
        public JsonResult OrganizationInvitation_AddFields(int o)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "OrganizationID", FieldType = DataType.Hidden.ToString(), Value = o.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Email", Name = "Email", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Email", true, "", 5, 500) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Organization Invitation ID</param>
        [Route("OrganizationInvitation_EditFields"), NonNullableParameters]
        public JsonResult OrganizationInvitation_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<OrganizationInvitation>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Email", Name = "Email", FieldType = DataType.Text.ToString(), Value = a.Email, Validations = checkAndAddValidation("Text", "Email", true, "", 5, 500) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("OrganizationInvitation"), Route("OrganizationInvitation")]
        public JsonResult PostOrganizationInvitation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new OrganizationInvitation
                {
                    OrganizationID = parseIntField(form, "OrganizationID"),
                    Email = parseTextField(form, "Email")
                };

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!regex.IsMatch(a.Email))
                    return jsonException("The email you entered is not valid", HttpStatusCode.Forbidden);

                if (Company.Any<OrganizationInvitation>(i => i.OrganizationID == a.OrganizationID && i.Email == a.Email))
                    return jsonException("This email has already been invited to this organization", HttpStatusCode.Forbidden);

                var userIsAlreadyRegistered = Company.Query<dynamic>(@"select 1 from organizationresource g
                    inner join reporting.Global_Resource r on r.ResourceID = g.ResourceID
                    where r.Email = @Email and g.OrganizationID = @OrganizationID", new { a.Email, a.OrganizationID }).Count() > 0;
                if (userIsAlreadyRegistered)
                    return jsonException("A user with this email address is already registered to this organization", HttpStatusCode.Forbidden);

                Company.Add(a);

                dynamic custom = new
                {
                    action = "add"
                };

                return jsonSuccess("Organization invitation successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [ HttpPut, ActionName("OrganizationInvitation"), Route("OrganizationInvitation")]
        public JsonResult PutOrganizationInvitation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var id = parseIntField(form, "ID");
                var existing = Company.GetById<OrganizationInvitation>(id);
                if (existing == null) throw new NotFoundException("organization invitation");

                existing.Email = parseTextField(form, "Email");

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);


                if (!regex.IsMatch(existing.Email))
                    return jsonException("The email you entered is not valid", HttpStatusCode.Forbidden);

                if (Company.Any<OrganizationInvitation>(i => i.OrganizationID == existing.OrganizationID && i.Email == existing.Email && i.ID != existing.ID))
                    return jsonException("This email has already been invited to this organization", HttpStatusCode.Forbidden);

                var userIsAlreadyRegistered = Company.Query<dynamic>(@"select 1 from organizationresource g
                    inner join reporting.Global_Resource r on r.ResourceID = g.ResourceID
                    where r.Email = @Email and g.OrganizationID = @OrganizationID", new { existing.Email, existing.OrganizationID }).Any();
                if (userIsAlreadyRegistered)
                    return jsonException("A user with this email address is already registered to this organization", HttpStatusCode.Forbidden);


                Company.Update(existing);

                dynamic custom = new
                {
                    action = "edit"
                };

                return jsonSuccess("Organization invitation successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [ HttpDelete, ActionName("OrganizationInvitation"), Route("OrganizationInvitation"), NonNullableParameters]
        public JsonResult DeleteOrganizationInvitation(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<OrganizationInvitation>(id);
                if (model == null) throw new NotFoundException("organization invitation");

                Company.Delete(model);

                dynamic custom = new
                {
                    action = "delete"
                };

                return jsonSuccess("Organization invitation successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Type

        [HttpDelete, ActionName("OrganizationType"), Route("OrganizationType"), NonNullableParameters]
        public JsonResult DeleteOrganizationType(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                
                var model = Company.GetById<OrganizationType>(id);
                if (model == null) throw new NotFoundException("organizationType");

                model.State = State.Deleted;

                Company.SaveChanges();
                
                dynamic custom = new
                {
                    Name = model.Name,
                    action = "delete"
                };

                return jsonSuccess("Organization successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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
            list.Add(new EditableField { FieldName = "PolicyTypeID", FieldType = DataType.Hidden.ToString(), Value = typeID.ToString() });
            if (parentID.HasValue) list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = parentID.Value.ToString() });
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.PolicyType, typeID).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyID</param>
        [Route("Policy_EditFields"), NonNullableParameters]
        public JsonResult Policy_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Policy, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var model = Company.GetById<Policy>(id);
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });

            list = (
                loadDynamicFields(
                    SystemObjects.Policy.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Policy, id).ToList(), 
                    1, 
                    true
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddPolicy")]
        public JsonResult AddPolicy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Policy");
                
                int typeID = parseIntField(form, "PolicyTypeID");
                var type = Company.GetById<PolicyType>(typeID);
                int? parentId = parseNullableIntField(form, "ParentID");

                if (type == null) throw new NotFoundException("policy type");

                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, typeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new Policy { PolicyTypeID = typeID };
                                
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Policy, model.ID, Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), form, Server);
                Company.SaveOrUpdate(model,fields, parentId.GetValueOrDefault());
                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList();
                processFormDynamicRelationshipFields(SystemObjects.PolicyType, model.PolicyTypeID, SystemObjects.Policy, model.ID, fieldTypes, form);
                                
                if (!string.IsNullOrEmpty(form["ParentID"]) && form["ParentID"] != "0")
                {
                    var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                        i.Object == "PolicyType" &&
                        i.ObjectID == type.ID &&
                        i.PredicateType.Value == PredicateType.IntraTypeHierarchy
                    ).SingleOrDefault();

                    if (intersectType != null)
                    {
                        var intersect = new Intersect
                        {
                            Subject = SystemObjects.Policy.ToString(),
                            SubjectID = parseIntField(form, "ParentID"),
                            Object = SystemObjects.Policy.ToString(),
                            ObjectID = model.ID,
                            IntersectTypeID = intersectType.ID
                        };

                        var parentExists = Company.Any<Asset>(i =>
                            i.ObjectID == intersect.SubjectID &&
                            i.AssetType.Object == "PolicyType" &&
                            i.AssetType.ObjectID == intersectType.SubjectID
                            );

                        if (!parentExists)
                        {
                            return jsonException($"Parent {intersectType.SubjectName} with ID {intersect.SubjectID} could not be found.", HttpStatusCode.NotFound);
                        }

                        Company.Add(intersect);
                    }
                }

                dynamic custom = new
                {                    
                    action = "add",
                    Context = form["_context"]
                };                

                return jsonSuccess("Policy successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeletePolicy")]
        public JsonResult DeletePolicy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Policy");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Policy>(id);
                if (model == null) throw new NotFoundException("Policy");

                if (!Company.HasAssetPermission(SystemObjects.Policy, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.Policy, id);

                dynamic custom = new
                {                    
                    action = "delete",
                    Context = form["_context"]
                };                

                return jsonSuccess("Policy successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, custom);
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
        
        [HttpDelete, Route("DeletePolicyByID"), NonNullableParameters]
        public JsonResult DeletePolicyByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeletePolicy(form);
        }

        [HttpPut, ValidateInput(false), Route("EditPolicy"), NonNullableParameters]
        public JsonResult EditPolicy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Policy");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Policy>(id);
                if (model == null) throw new NotFoundException("Policy");

                if (!Company.HasAssetPermission(SystemObjects.Policy, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                                
                Company.SaveOrUpdate(model, new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Policy, model.ID, Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), form, Server, false));
                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList();
                processFormDynamicRelationshipFields(SystemObjects.PolicyType, model.PolicyTypeID, SystemObjects.Policy, model.ID, fieldTypes, form);
                var sType = SystemObjects.Policy.ToString();
                var parentID = parseIntField(form, "ParentID");

                if (parentID > 0)
                {
                    var intersect = Company.Filter<Intersect>(i =>
                        i.Subject == sType &&
                        i.Object == sType &&
                        i.ObjectID == model.ID &&
                        i.IntersectType.Predicate.Type == PredicateType.IntraTypeHierarchy
                    ).SingleOrDefault();

                    if (intersect != null)
                    {
                        if (intersect.SubjectID != parentID)
                        {
                            intersect.SubjectID = parentID;
                            Company.Update(intersect);
                        }
                    }
                }

                dynamic custom = new
                {                    
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess("Policy successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

                var a = new PolicyTypeLevel
                {
                    PolicyTypeID = id,
                    Level = level,
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<PolicyTypeLevel>(a);

                return jsonSuccess(a.Name + " successfully created.", a.PolicyTypeID.ToString(), "add", HttpStatusCode.Created);
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

                Company.Delete<PolicyTypeLevel>(i => i.PolicyTypeID == id && i.Level == level);

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
                var model = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == id && i.Level == level).SingleOrDefault();
                if (model == null) throw new NotFoundException("policy type level");

                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<PolicyTypeLevel>(model);

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

        #region Predicate

        #region Field Generation

        [Route("Predicate_AddFields")]
        public JsonResult Predicate_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var functionalTypes = PredicateType.DataLineage.GetAsList()
                .Where(f => f.AllowEditFromRelationshipEditor && f.AllowIntersectTypeAssignment)
                .Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name })
                .ToList();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Functional Type", FieldType = DataType.Lookup.ToString(), Items = functionalTypes });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PredicateID</param>
        [Route("Predicate_EditFields"), NonNullableParameters]
        public JsonResult Predicate_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Predicate>(id);
            var any = Company.Any<IntersectType>(i => i.PredicateID == id);
            var functionalTypes = PredicateType.DataLineage.GetAsList()
                .Where(f =>  f.AllowEditFromRelationshipEditor && f.AllowIntersectTypeAssignment)
                .Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name })
                .ToList();

            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Value = a.Inverse, Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            list.Add(new EditableField { ReadOnly=any, Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Functional Type", FieldType = DataType.Lookup.ToString(), Value = ((int)a.Type).ToString(), Items = functionalTypes });
            
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddPredicate")]
        public JsonResult AddPredicate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("predicate");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new Predicate
                {
                    Name = parseTextField(form, "Name"),
                    Inverse = parseTextField(form, "Inverse", null, true),
                    Type = (PredicateType)Enum.Parse(typeof(PredicateType), form["Type"]),
                    IsSystem = false
                };

                if (a.Type.AsInfoModel().ReadOnly)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Not allowed to add a predicate of this type.");
                }
                if (!a.Type.AsInfoModel().AllowMultiplePredicates)
                {
                    var any = Company.Predicates.Any(i => i.Type == a.Type);
                    if (any)
                        throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Not allowed to add another predicate of this type. Only one may exist.");
                }

                Company.Add<Predicate>(a);

                return jsonSuccess(a.Name + " successfully created.", string.Format("Predicate|{0}", a.ID), "add", HttpStatusCode.Created, new { });
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

        [HttpDelete, Route("DeletePredicate")]
        public JsonResult DeletePredicate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("predicate");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Predicate>(id);
                if (model == null) throw new NotFoundException("predicate");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<Predicate>(model);
                return jsonSuccess("Item successfully removed.", null, "delete", HttpStatusCode.OK, new { });
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

        [HttpPut, ValidateInput(false), Route("EditPredicate")]
        public JsonResult EditPredicate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("predicate");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Predicate>(id);
                if (model == null) throw new NotFoundException("predicate");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Inverse = parseTextField(form, "Inverse");

                var any = Company.Any<IntersectType>(i => i.PredicateID == id);

                //only allow edit of type for unused predicates
                if (!any)
                {                    
                    model.Type = (PredicateType)parseIntField(form, "Type");
                }

                Company.Update<Predicate>(model);

                return jsonSuccess(model.Name + " successfully updated.", string.Format("IntersectRole|{0}", id), "edit", HttpStatusCode.OK, new { });
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
                var sql = "select DisplayValue, ObjectID from assetdetail where [object] = 'Referenceitem' and TypeID = @id";
                list.Add(new EditableField { Row = row++, Column = 1, FieldName = "ParentID", Name = parentType.Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = false, Items = Company.Query<dynamic>(sql, new { id = parentType.ObjectID }).Select(i => new SelectListItem { Text = i.DisplayValue, Value = string.Format("{0}", i.ObjectID) }).ToList() });
            }
                        
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, id).ToList(), row);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
        [Route("ReferenceItem_EditFields"), NonNullableParameters]
        public JsonResult ReferenceItem_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ReferenceItem>(id);

            if (!Company.HasAssetPermission(SystemObjects.ReferenceItem, a.ID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var row = 1;

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = row++, Column = 1, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString(), Value = a.Code.ToString() });

            //if the reference type has a parent we need to add parent field with the values from the parent

            var parentType = Company.GetParentType(a.ReferenceItemTypeID, SystemObjects.ReferenceItemType);

            if (parentType != null)
            {
                var parent = Company.GetParentObject(id, SystemObjects.ReferenceItem);
                var sql = "select DisplayValue, ObjectID from assetdetail where [object] = 'Referenceitem' and TypeID = @id";
                list.Add(new EditableField { Row = row++, Column = 1, FieldName = "ParentID", Name = parentType.Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = false, Items = Company.Query<dynamic>(sql, new { id = parentType.ID }).Select(i => new SelectListItem { Text = i.DisplayValue, Value = string.Format("{0}", i.ObjectID), Selected = i.ObjectID == (parent != null ? parent.ID : 0)  }).ToList() });
            }

            list = loadDynamicFields(SystemObjects.ReferenceItem.ToString(), id, list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, a.ReferenceItemTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.ReferenceItem, id).ToList(), row);

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddReferenceItem")]
        public JsonResult AddReferenceItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup");

                int typeID = parseIntField(form, "ReferenceItemTypeID");
                var type = Company.GetById<ReferenceItemType>(typeID);

                if (type == null) throw new NotFoundException("referenceitemtype");

                if (!Company.HasAssetTypePermission(SystemObjects.ReferenceItemType, typeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var code = form["Code"].ToString();

                if (Company.Any<ReferenceItem>(r => r.ReferenceItemTypeID == typeID && r.Code == code))
                    return jsonException(new Exception($"A reference item with the code value {code} already exists."), HttpStatusCode.Forbidden);

                var a = new ReferenceItem
                {
                    Code = code,
                    ReferenceItemTypeID = typeID,
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow                    
                };

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.ReferenceItem, a.ID, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate(a, fields);
                
                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    if (!Company.AddObjectParentRelationship(SystemObjects.ReferenceItemType, typeID, SystemObjects.ReferenceItem, parseIntField(form, "ParentID"), a.ID))
                    {
                        return jsonException($"Parent intersect with could not be found.", HttpStatusCode.NotFound);
                    }
                }

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

        [HttpDelete, Route("DeleteReferenceItem")]
        public JsonResult DeleteReferenceItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ReferenceItem");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReferenceItem>(id);
                if (model == null) throw new NotFoundException("ReferenceItem");

                if (!Company.HasAssetPermission(SystemObjects.ReferenceItem, model.ID, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.ReferenceItem, id);
                
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

        [HttpPut, ValidateInput(false), Route("EditReferenceItem")]
        public JsonResult EditReferenceItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("referenceitem");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReferenceItem>(id);

                if (model == null) throw new NotFoundException("referenceitem");

                if (!Company.HasAssetPermission(SystemObjects.ReferenceItem, model.ID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var code = form["Code"].ToString();

                if (Company.Any<ReferenceItem>(r => r.ReferenceItemTypeID == model.ReferenceItemTypeID && r.Code == code && r.ID != model.ID))
                    return jsonException(new Exception($"A reference item with the code value {code} already exists."), HttpStatusCode.Forbidden);

                model.Code = code;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.ReferenceItem, model.ID, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, model.ReferenceItemTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<ReferenceItem>(model, fields);
                
                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    if (!Company.UpdateObjectParentRelationship(SystemObjects.ReferenceItemType, model.ReferenceItemTypeID, SystemObjects.ReferenceItem, parseIntField(form, "ParentID"), model.ID))
                    {
                        return jsonException($"Parent intersect with could not be found.", HttpStatusCode.NotFound);
                    }
                }

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

        #region Reference Item Types

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddReferenceItemType")]
        public JsonResult AddReferenceItemType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("ReferenceItemType");

                var model = new ReferenceItemType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    SourceNotes = parseTextField(form, "SourceNotes"),
                    DisplayFormat = parseTextField(form, "DisplayFormat"),
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow
                };

                Company.Add(model);

                if (model.ID > 0)
                {
                    Company.Add(new FieldType
                    {
                        ObjectID = model.ID,
                        Object = SystemObjects.ReferenceItemType.ToString(),
                        IsListable = true,
                        IsRequired = true,
                        IsEditable = true,
                        FriendlyName = "Long Description",
                        Name = "LongDesc",
                        MaximumLength = 500,
                        MinimumLength = 1,
                        SortOrder = 1,
                        Type = DataType.Text.ToString(),
                        IsDisplayable = true
                    });
                }

                dynamic custom = new
                {
                    model.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeleteReferenceItemType")]
        public JsonResult DeleteReferenceItemType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ReferenceItemType");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReferenceItemType>(id);
                if (model == null) throw new NotFoundException("ReferenceItemType");

                //check if the reference list is the parent of any other reference item types
                if (Company.TypeHasChildren(SystemObjects.ReferenceItemType, id)) throw new Exception("The selected Reference List Is the parent to one or more Reference List(s).  Please delete those first.");

                if (Company.Filter<FieldType>(x => x.LookupObjectType == "ReferenceItem" && x.LookupObjectID == id).Count() > 0)
                    throw new ConflictException("Error", "The reference list you are trying to delete is in use");

                if (!Company.HasAssetTypePermission(SystemObjects.ReferenceItemType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.ReferenceItemType, model.ID);

                dynamic custom = new
                {
                    model.Name,
                    action = "delete",
                    Context = form["_context"]
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

        [HttpPut, ValidateInput(false), Route("EditReferenceItemType")]
        public JsonResult EditReferenceItemType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ReferenceItemType");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReferenceItemType>(id);
                if (model == null) throw new NotFoundException("ReferenceItemType");

                if (!Company.HasAssetTypePermission(SystemObjects.ReferenceItemType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.SourceNotes = parseTextField(form, "SourceNotes");
                model.DisplayFormat = parseTextField(form, "DisplayFormat");
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update(model);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        #region Relationship

        #region Field Generation

        /// <param name="it">IntersectTypeID</param>
        /// <param name="type">Object</param>
        /// <param name="id">ObjectID</param>
        [Route("Relationship_AddFields"), NonNullableParameters]
        public JsonResult Relationship_AddFields(int it, SystemObjects type, int id)
        {
            if (!Company.HasAssetPermission(type, id, Permission.ModifyRelationships))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var relationshipType = Company.GetById<IntersectType>(it, i => i.Predicate);
            var obj = Company.GetObjectDetail(type.ToString(), id);

            if (obj == null || relationshipType == null)
            {
                return jsonException("Invalid relationship type or source item.", HttpStatusCode.NotFound);
            }

            
            var targetCardinality = Cardinality.Many;
            if (relationshipType.Subject == obj.Type && relationshipType.SubjectID == obj.TypeID)
            {                
                targetCardinality = relationshipType.ObjectCardinality;
            }
            else
            {             
                targetCardinality = relationshipType.SubjectCardinality;
            }

            list.Add(new EditableField { FieldName = "IntersectTypeID", FieldType = DataType.Hidden.ToString(), Value = it.ToString() });
            list.Add(new EditableField { FieldName = "Source", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
            list.Add(new EditableField { FieldName = "SourceID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
                                   
            list.Add(new EditableField
            {
                    Row = 1,
                    Column = 1,
                    Required = true,
                    FieldName = "Items",
                    Name = "What Items Are You Relating?",                    
                    MultiSelect = (targetCardinality == Cardinality.Many),                    
                    FieldType = DataType.DataTableSelect.ToString(),
                    TypeaheadUri = $"/form/Relationship_DataTable?intersectTypeId={it}&type={type}&objectId={id}"
            });
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IntersectType, it).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        /// <param name="id">RelationshipID</param>
        [Route("Relationship_EditFields"), NonNullableParameters]
        public JsonResult Relationship_EditFields(int id)
        {
            var relationship = Company.GetById<Intersect>(id, i => i.IntersectType);
            if (relationship == null) return jsonException("Relationship not found.", HttpStatusCode.NotFound);

            if (!Company.HasAssetPermission(relationship.Subject, relationship.SubjectID, Permission.ModifyRelationships) &&
                !Company.HasAssetPermission(relationship.Object, relationship.ObjectID, Permission.ModifyRelationships))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list = loadDynamicFields(SystemObjects.Intersect.ToString(), id, list, Company.GetFieldTypesByObject(SystemObjects.IntersectType, relationship.IntersectTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Intersect, relationship.ID).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRelationship")]
        public JsonResult AddRelationship(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("relationship");

                var source = parseTextField(form, "Source");
                var sourceID = parseIntField(form, "SourceID");
                int typeID = parseIntField(form, "IntersectTypeID");
                var relationshipType = Company.GetById<IntersectType>(typeID);
                var sourceObject = Company.GetObjectDetail(source, sourceID);

                if (!Company.HasAssetPermission(source, sourceID, Permission.ModifyRelationships))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (relationshipType == null) throw new NotFoundException("relationship");

                var targetCardinality = Cardinality.Many;
                if (relationshipType.Subject == sourceObject.Type && relationshipType.SubjectID == sourceObject.TypeID)
                {                    
                    targetCardinality = relationshipType.ObjectCardinality;
                }
                else
                {                    
                    targetCardinality = relationshipType.SubjectCardinality;
                }


                var rawItems = parseTextField(form, "Items");
                if (string.IsNullOrEmpty(rawItems))
                    return jsonException("No selected items", HttpStatusCode.BadRequest);

                var items = rawItems.Split(',').ToList();
                                
                if ((targetCardinality == Cardinality.One && items.Count > 1))
                    return jsonException("Invalid relationship cardinality for multiple items.", HttpStatusCode.BadRequest);

                items.ForEach(item =>
                {
                    var itemInfo = item.Split('|');
                    if (itemInfo.Length == 2)
                    {
                        var intersect = Company.AddIntersect(typeID,
                            source, sourceID,
                            itemInfo[0], int.Parse(itemInfo[1])
                        );

                        var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Intersect, intersect.ID, Company.GetFieldTypesByObject(SystemObjects.IntersectType, typeID).ToList(), form, Server);
                        Company.AddOrUpdateFields(fields);
                    }
                });

                return jsonSuccess(relationshipType.Name + " successfully created.", "0", "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Intersect.ToString(), ObjectID = 0 });
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

        [HttpPut, ValidateInput(false), Route("EditRelationship")]
        public JsonResult EditRelationship(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("relationship");

                int id = parseIntField(form, "ID");
                var intersect = Company.GetById<Intersect>(id, i => i.IntersectType);

                if (intersect == null) throw new NotFoundException("relationship");

                if (!Company.HasAssetPermission(intersect.Subject, intersect.SubjectID, Permission.ModifyRelationships) &&
                    !Company.HasAssetPermission(intersect.Object, intersect.ObjectID, Permission.ModifyRelationships))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Update(intersect);
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Intersect, intersect.ID, Company.GetFieldTypesByObject(SystemObjects.IntersectType, intersect.IntersectTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                return jsonSuccess("Relationship successfully updated.", intersect.ID.ToString(), "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Intersect.ToString(), ObjectID = intersect.ID });
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

        #region DataTable Select Source

        [HttpGet, Route("Relationship_DataTable"), NonNullableParameters]
        public JsonResult Relationship_DataTable(int intersectTypeId, SystemObjects type, int objectId)
        {
            var relationshipType = Company.GetById<IntersectType>(intersectTypeId, i => i.Predicate);
            
            int objectTypeID = -1;
            string parentType = string.Empty;

            #region

            if(type == SystemObjects.FusionAttribute)
            {
                objectTypeID = Company.FusionAttributes.Where(x => x.ID == objectId).Single().FusionAttributeTypeID;
                parentType = "FusionAttributeType";
            }
            else
            {
                var obj = Company.GetObjectDetail(type.ToString(), objectId);
                objectTypeID = obj.TypeID;
                parentType = obj.Type;
            }

            if (objectTypeID <= 0 || string.IsNullOrEmpty(parentType) || relationshipType == null)
            {
                return jsonException("Invalid relationship type or source item.", HttpStatusCode.NotFound);
            }

            if(type == SystemObjects.ReferenceItemType)
            {
                objectTypeID = 0;
            }

            var targetType = "";
            var targetTypeID = 0;
            if (relationshipType.Subject == parentType && relationshipType.SubjectID == objectTypeID)
            {
                targetType = relationshipType.Object;
                targetTypeID = relationshipType.ObjectID;
            }
            else
            {
                targetType = relationshipType.Subject;
                targetTypeID = relationshipType.SubjectID;
            }

            #endregion

            #region sql

            var sql = "";

            switch (targetType)
            {
                case "FusionAttributeType":
                    #region
                    if ((relationshipType.Predicate != null) && (relationshipType.Predicate.Type == PredicateType.FusionMapping))
                    {
                        sql = $@"
select	'FusionAttribute' as [Object], 
        FA.ID as ObjectID, 
        F.Name + '.' + FA.TextPath as Name
from	FusionAttribute FA with(nolock)
		inner join Fusion F with(nolock) on F.ID = FA.FusionID and FA.FusionAttributeTypeID = @targetTypeID and FA.Deleted = 0
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) )
					)
order by F.Name, FA.TextPath";
                    }
                    else
                    {
                        sql = $@"
declare @OwnerSourceType varchar(50)
declare @owners table (ID int)


IF @source = 'Intersect'
BEGIN
	set @OwnerSourceType = 'Artifact'

	insert into @owners
		select	A.ID
		from	[IntersectDetail] N
				inner join Asset A with(nolock) on N.[Subject] = 'Artifact' and A.objectID = N.SubjectID and N.ID = @id
				inner join AssetType [AT] with(nolock) on [AT].ID = A.AssetTypeID and [AT].CanOwnFusion = 1
	insert into @owners
		select	A.ID
		from	[IntersectDetail] N
				inner join Asset A with(nolock) on N.[Object] = 'Artifact' and A.ObjectID = N.ObjectID and N.ID = @id
				inner join AssetType [AT] with(nolock) on [AT].ID = A.AssetTypeID and [AT].CanOwnFusion = 1
END
ELSE
BEGIN
	set @OwnerSourceType = @source
	insert into @owners 
	Select ID from Asset where [object]=@OwnerSourceType and [objectId]=@id
END

declare @h table (ID int);

if @OwnerSourceType = 'Artifact'
	begin
		with h as	(
					select	A.ID,
							PA.ID as ParentID
					from	Asset A with(nolock)
							inner join @owners O on O.ID = A.ID
							cross apply [dbo].[GetParentByAssetID] (A.ID) as PA
					union all
					select	AA.ID,
							PA.ID as ParentID
					from	Asset AA with(nolock)
							cross apply [dbo].[GetParentByAssetID] (AA.ID) as PA
							inner join h as C on C.ParentID =AA.ID
					)
		insert into @h
			select ID from h;
	end

		insert into @h values (@id)
        insert into @h select id from @Owners


select	'FusionAttribute' as [Object], 
        FA.ID as ObjectID, 
        F.Name + '.' + FA.TextPath as Name
from	FusionAttribute FA with(nolock)
		inner join Fusion F with(nolock) on F.ID = FA.FusionID and FA.FusionAttributeTypeID = @targetTypeID and FA.Deleted = 0
        inner join FusionOwner FO on FO.FusionID = FA.FusionID
        inner join @h H on H.ID = FO.ASSETID
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	IntersectTypeID = @it and ( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) )
					)
order by F.Name, FA.TextPath";
                    }
                    break;
                #endregion
                case "FusionQueryAttributeType":
                    #region                    
                        sql = $@"
select	'FusionQueryAttribute' as [Object], 
        FA.ID as ObjectID, 
        F.Name + '.' + FA.DisplayValue as Name
from	FusionQueryAttribute FA with(nolock)
        inner join FusionQueryAttributeType FAT on (FA.FusionQueryAttributeTypeID = FAT.ID)
		inner join Fusion F with(nolock) on F.ID = FAT.FusionID and FA.FusionQueryAttributeTypeID = @targetTypeID and FA.Deleted = 0
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) )
					)
order by F.Name, FA.DisplayValue";                 
                    break;
                #endregion
                case "Group":
                case "GroupType":
                    #region
                    sql = $@"
select	'Group' as [Object], 
        D.ID as ObjectID, 
        D.Name
from	[Group] D with(nolock)
where	D.ID not in (
					select	case 
                                when SubjectType = 'Group' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'Group') ) OR
							 ( (SubjectType = 'Group') AND (Object = @source and ObjectID = @id) )
							)
					)
order by D.Name";
                    break;
                #endregion
                case "Resource":
                case "ResourceType":
                    #region
                    sql = $@"
select	'Resource' as [Object], 
        D.ResourceID as ObjectID, 
        D.LastName + ', ' + D.FirstName as Name
from	reporting.Global_Resource D with(nolock)
where   D.ResourceID not in (
					select	case 
                                when SubjectType = 'ResourceType' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'Resource') ) OR
							 ( (SubjectType = 'Resource') AND (Object = @source and ObjectID = @id) )
							)
					)
order by D.LastName, D.FirstName";
                    break;
                #endregion
                case "ReferenceItemType":
                    #region
                    if (targetTypeID == 0)
                    {
                        sql = $@"
select	'ReferenceItemType' as [Object], 
        r.ID as ObjectID, 
        r.Name as Name
from	[dbo].[referenceitemtype] r with(nolock)
where   r.ID not in (
					select	case 
                                when SubjectType = 'ReferenceItemType' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'ReferenceItemType') ) OR
							 ( (SubjectType = 'ReferenceItemType') AND (Object = @source and ObjectID = @id) )
							)
					)
order by r.Name";
                    }
                    else
                    {
                        sql = $@"
select	'ReferenceItem' as [Object], 
        r.ID as ObjectID, 
        r.DisplayValue as Name
from	ReferenceItem r with(nolock)
where   r.ID not in (
					select	case 
                                when SubjectType = 'ReferenceItemType' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'ReferenceItem' and ObjectTypeID = r.ReferenceItemTypeID) ) OR
							 ( (SubjectType = 'ReferenceItem' and SubjectTypeID = r.ReferenceItemTypeID) AND (Object = @source and ObjectID = @id) )
							)
					)
        and r.ReferenceItemTypeID = @targetTypeID 
order by r.DisplayValue";
                    }
                    break;
                #endregion
                case "RuleImplementationType":
                    #region
                    if (targetTypeID == 0)
                    {
                        sql = $@"
select	'RuleImplementation' as [Object], 
        r.ID as ObjectID, 
        coalesce(r.Name, 'Implementation ' + cast(r.ID as varchar)) as Name
from	[dbo].[ruleimplementation] r with(nolock)
where   r.ID not in (
					select	case 
                                when SubjectType = 'RuleImplementation' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'RuleImplementation') ) OR
							 ( (SubjectType = 'RuleImplementation') AND (Object = @source and ObjectID = @id) )
							)
					)
order by r.Name";
                    }
                    else
                    {
                        sql = $@"
select	'RuleImplementation' as [Object], 
        r.ID as ObjectID, 
        coalesce(r.Name, 'Implementation ' + cast(r.ID as varchar)) as Name
from	RuleImplementation r with(nolock)
where   r.ID not in (
					select	case 
                                when SubjectType = 'RuleImplementation' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'RuleImplementation' and ObjectTypeID = r.RuleID) ) OR
							 ( (SubjectType = 'RuleImplementation' and SubjectTypeID = r.RuleID) AND (Object = @source and ObjectID = @id) )
							)
					)
        and r.RuleID = @targetTypeID 
order by r.Name";
                    }
                    break;
                #endregion
                default:
                    #region
                    sql = $@"(
select		D.[Object], 
			D.ObjectID
from		Asset D
            inner join AssetType AST on D.AssetTypeID = AST.ID
			left join [Intersect] I on	I.IntersectTypeID = @it and (
											( (I.Subject = @source and I.SubjectID = @id) AND (I.Object = D.[Object] and I.ObjectID = D.ObjectID) ) OR
											( (I.Subject = D.[Object] and I.SubjectID = D.ObjectID) AND (I.Object = @source and I.ObjectID = @id) )
										)
where		I.ID is null and AST.ObjectID = @targetTypeID and AST.[Object] = @targetType
) C on C.ObjectID = O.ID";

                    switch (targetType)
                    {
                        case "ArtifactType":
                            sql = $@"select C.Object, C.ObjectID, ADisp.DisplayValue as Name from Artifact O inner join {sql} inner join Asset Ass on (Ass.ObjectID = O.ID and Ass.[Object] = 'Artifact') cross apply [dbo].[GetAssetDisplayValueById](Ass.ID) ADisp order by ADisp.DisplayValue";
                            break;
                        case "GroupType":
                            sql = $@"select C.Object, C.ObjectID, O.Name from [Group] O inner join {sql} order by O.Name";
                            break;
                        case "IntersectType":
                            sql = $@"select C.Object, C.ObjectID, O.Name from [Intersect] O inner join {sql} order by O.Name";
                            break;
                        case "LookupType":
                            sql = $@"select C.Object, C.ObjectID, O.Name from [LookupType] O inner join {sql} order by O.Name";
                            break;                            
                        case "ReferenceItemType":
                            sql = $@"select C.Object, C.ObjectID, O.Name from [ReferenceItemType] O inner join {sql} order by O.Name";
                            break;
                        case "ResourceType":
                            sql = $@"select C.Object, C.ObjectID, O.LastName + ', ' + O.FirstName as Name from reporting.[Global_Resource] O inner join {sql} order by O.LastName + ', ' + O.FirstName";
                            break;
                        case "RuleType":
                            sql = $@"select C.Object, C.ObjectID, O.DisplayValue AS Name from [Rule] O inner join {sql}  inner join Asset Ass on (Ass.ObjectID = O.ID and Ass.[Object] = 'Rule') cross apply [dbo].[GetAssetDisplayValueById](Ass.ID) ADisp order by ADisp.DisplayValue";
                            break;
                        case "PolicyType":
                        case "TaxonomyType":
                            sql = $@"
select	A.Object,
        A.ObjectID, 
        TP.TextPath as Name 
from	AssetDetail A 
        cross apply dbo.GetAssetTextPathById(A.ID, '/') TP 
where   A.Type = @targetType 
        and A.TypeID = @targetTypeID 
        and A.[State] = 1 
        and A.ID not in ({GetNoReadSqlStatement()}) 
order by TP.TextPath"; 
                            
                            break;
                    }
                    break;
                    #endregion
            }

            #endregion

            var items = Company.Query<dynamic>(sql, new { targetType, targetTypeID, source = type.ToString(), id = objectId, it = intersectTypeId }).Select(i => new { Text = i.Name, Value = $"{i.Object}|{i.ObjectID}" }).ToList();

            return Json(items, JsonRequestBehavior.AllowGet);
        }
        
        #endregion

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

        #region Responsibility

        #region Form Get/Post

        List<SelectListItem> getResponsibilityResources(string selectedID = "")
        {
            var list = GetCompanyResources()
                .Where(i => i.ResourceID > 0 && i.State == CompanyResourceState.Active)
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"User: {i.LastName}, {i.FirstName}",
                    Value = $"R|{i.ID}",
                    Selected = ($"R|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList();

            list.AddRange(
                Company.Table<Group>()
                .Select(i => new { i.ID, i.Name })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"Group: {i.Name}",
                    Value = $"G|{i.ID}",
                    Selected = ($"G|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList()
            );

            list.AddRange(
                Company.Table<Organization>()
                .Select(i => new { i.ID, i.Name })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"Organization: {i.Name}",
                    Value = $"O|{i.ID}",
                    Selected = ($"O|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList()
            );

            return list;
        }

        [HttpDelete, Route("DeleteResponsibilityByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityByID(long id)
        {
            try
            {
                var model = Company.GetById<ResponsibilityTypeRelationOverrideItem>(id);
                if (model == null) throw new NotFoundException("responsibility");

                if (!Company.HasAssetPermission(model.AssetID, Permission.DeleteResponsibilities))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, new { AssetID = model.AssetID });
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

        [HttpGet, Route("Responsibility/Resources"), NonNullableParameters]
        public JsonNetResult ResponsibilityResources(long assetID, int resTypeId,string secAssettype,int secAssetTypeid, int pagenum, int pagesize, string sortDataField, string sortOrder,string gbfilter)
        {
            string querySql;
            string hideUsersSql = "";
            var dbArgs = new Dapper.DynamicParameters();

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com')";
            }
            if (resTypeId == 0)
            {
                querySql = @"
                            select  g.Name as Text, 'Group|' + cast(g.ID as varchar) as [Value],'Group' as [Type] from [Group] g
							where   not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='G' and SecurityAssetID= g.Id) 
							union all
							select  r.LastName + ', ' + r.FirstName as label, 'Resource|' + cast(r.ResourceID as varchar) as [Value],'User' as 'Type' from reporting.Global_Resource r
							where   r.[State] = 1 
                                    and not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='R' and ResourceID= r.ResourceID)";
                querySql += hideUsersSql;
            }
            else
            {
                if (secAssettype=="R")
                {
                    dbArgs.Add("resourceId", secAssetTypeid);
                    dbArgs.Add("groupId", -1);
                }
                else
                {
                    dbArgs.Add("resourceId", -1);
                    dbArgs.Add("groupId", secAssetTypeid);
                }
                querySql = @"
                    		select  g.Name as Text, 'Group|' + cast(g.ID as varchar) as [Value],'Group' as [Type] from [Group] g
							where   not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='G' and SecurityAssetID= g.Id and ResponsibilityTypeID=@responsibilityTypeID
                                and SecurityAssetId <> @groupId) 
							union all
							select  r.LastName + ', ' + r.FirstName as label, 'Resource|' + cast(r.ResourceID as varchar) as [Value],'User' as 'Type' from reporting.Global_Resource r
							where r.[State] = 1 and  not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='R' and ResourceID= r.ResourceID and ResponsibilityTypeID=@responsibilityTypeID
                            and ResourceID <> @resourceId)";
                querySql += hideUsersSql;
                dbArgs.Add("responsibilityTypeID", resTypeId);
            }
            dbArgs.Add("assetID", assetID);

            querySql = string.Format(@"select  Text as [Text],  [Value] + '|' + [Type] + ' :: ' + Text as [Value],[Type] from ({0}) as  Sub", querySql);

            if (!string.IsNullOrEmpty(gbfilter))
            {
                querySql = string.Format(@"select * from ({0}) gb where  [Text] like '%' +   @gbfilter + '%'  or [Type] like   @gbfilter + '%'", querySql);
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

        [HttpGet, Route("Responsibility"), NonNullableParameters]
        public JsonNetResult Responsibility(long assetID, long? overrideID)
        {
            List<SelectListItem> resources;
            List<SelectListItem> responsibilityTypes;
            ResponsibilityTypeRelationOverrideItem responsibility;
            List<ResponsibilityDetail> responsibilityDetails;
            if (overrideID.HasValue)
            {
                responsibility = Company.GetById<ResponsibilityTypeRelationOverrideItem>(overrideID.Value, i => i.ResponsibilityType);
                resources = getResponsibilityResources($"{responsibility.SecurityAsset}|{responsibility.SecurityAssetID}");
                responsibilityTypes = Company.GetAllowedResponsibilityTypesByAsset(assetID).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = (i.ID == responsibility.ResponsibilityTypeID) }).ToList();
            }
            else
            {
                resources = getResponsibilityResources();
                responsibilityTypes = Company.GetAllowedResponsibilityTypesByAsset(assetID).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
                responsibility = new ResponsibilityTypeRelationOverrideItem { AssetID = assetID };
            }
            responsibilityTypes.Insert(0, new SelectListItem() { Text = "", Value = "" });
            responsibilityDetails = Company.Filter<ResponsibilityDetail>(i => i.AssetID == assetID).ToList<ResponsibilityDetail>();
            return new JsonNetResult
            {
                Data = new {
                    resources,
                    responsibilityTypes,
                    responsibility,
                    responsibilityDetails
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("Responsibility")]
        public JsonResult Responsibility(ResponsibilityTypeRelationOverrideItem r)
        {
            ResponsibilityTypeRelationOverrideItem model;

            if (r.ID == 0)
            {
                try
                {
                    if (!Company.HasAssetPermission(r.AssetID, Permission.ModifyResponsibilities))
                        return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                    Company.Add(r);
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

                return jsonSuccess("Item successfully created.", r.ID.ToString(), "edit", HttpStatusCode.OK, new { AssetID = r.AssetID });
            }
            else
            {
                try
                {
                    model = Company.GetById<ResponsibilityTypeRelationOverrideItem>(r.ID);
                    if (model == null) throw new NotFoundException("responsibility");

                    if (!Company.HasAssetPermission(model.AssetID, Permission.ModifyResponsibilities))
                        return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                    model.ResponsibilityTypeID = r.ResponsibilityTypeID;
                    model.SecurityAsset = r.SecurityAsset;
                    model.SecurityAssetID = r.SecurityAssetID;
                    model.Context = r.Context;

                    Company.Update(model);

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

                return jsonSuccess("Item successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK, new { AssetID = model.AssetID });
            }
        }

        [HttpPut, Route("Responsibility")]
        public JsonResult OverrideResponsibility(ResponsibilityTypeRelationOverrideItem r)
        {
            ResponsibilityTypeRelationOverrideItem model;

            try
            {
                model = Company.GetById<ResponsibilityTypeRelationOverrideItem>(r.ID);
                if (model == null) throw new NotFoundException("responsibility");

                if (!Company.HasAssetPermission(model.AssetID, Permission.ModifyResponsibilities))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.ResponsibilityTypeID = r.ResponsibilityTypeID;
                model.SecurityAsset = r.SecurityAsset;
                model.SecurityAssetID = r.SecurityAssetID;
                model.Context = r.Context;

                Company.Update(model);

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

            return jsonSuccess("Item successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK, new { AssetID = model.AssetID });
        }

        #endregion

        #endregion

        #region ResponsibilityType

        #region Form Get/Post

        [HttpDelete,  Route("DeleteResponsibilityTypeByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityTypeByID(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<ResponsibilityType>(id);
                if (model == null) throw new NotFoundException("ownership type");

                Company.Delete(SystemObjects.ResponsibilityType, id);

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

        [HttpGet, ActionName("ResponsibilityType"), Route("ResponsibilityType"), NonNullableParameters]
        public JsonNetResult GetResponsibilityType(int id)
        {
            ResponsibilityType model;

            var selectedAllocations = Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id)
            .ToList()
            .Select(i => new
            {
                i.ResponsibilityTypeID,
                i.ObjectID,
                i.ObjectType
            }).ToList();


            if (id < 1)
            {
                model = new ResponsibilityType();
                selectedAllocations = null;
            }
            else
            {
                model = Company.GetById<ResponsibilityType>(id);

            }

            var allocations = Company
                .GetAllocationOptions()
                .Select(i => new
                {
                    label = i.Name,
                    value = string.Format("{0}|{1}", i.ObjectType, i.ObjectTypeID),
                });

            //remove any selected items that no longer exist in available list
            if (selectedAllocations != null && selectedAllocations.Count > 0)
            {
                int indx = selectedAllocations.Count - 1;
                while (indx >= 0)
                {
                    var tag = $"{selectedAllocations[indx].ObjectType}|{selectedAllocations[indx].ObjectID}";
                    if (!allocations.Any(x => x.value == tag))
                        selectedAllocations.RemoveAt(indx);
                    indx--;
                }
            }

            return new JsonNetResult
            {
                Data = new
                {
                    model,
                    allocations,
                    selectedAllocations
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPut, ValidateInput(false),  ActionName("ResponsibilityType"), Route("ResponsibilityType")]
        public JsonResult PutResponsibilityType(ResponsibilityType model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var existing = Company.GetById<ResponsibilityType>(model.ID, i => i.ResponsibilityTypeRelations);
                if (existing == null) throw new NotFoundException("ownership type");

                existing.Name = model.Name;
                existing.Description = model.Description;
                
                // First, do the ADDs.
                foreach (var nr in model.ResponsibilityTypeRelations)
                {
                    if (!existing.ResponsibilityTypeRelations.Any(i => i.ObjectType == nr.ObjectType && i.ObjectID == nr.ObjectID))
                    {
                        existing.ResponsibilityTypeRelations.Add(new ResponsibilityTypeRelation { ObjectType = nr.ObjectType, ObjectID = nr.ObjectID, ResponsibilityTypeID = existing.ID, PermissionsBitMask = 0 });
                    }
                }

                // Last, do the DELETEs.
                var deletes = new List<ResponsibilityTypeRelation>();
                foreach (var dr in existing.ResponsibilityTypeRelations)
                {
                    if (!model.ResponsibilityTypeRelations.Any(i => i.ObjectType == dr.ObjectType && i.ObjectID == dr.ObjectID))
                    {
                        deletes.Add(dr);
                    }
                }
                foreach (var dr in deletes)
                {
                    existing.ResponsibilityTypeRelations.Remove(dr);
                }

                Company.Update(existing);

                return jsonSuccess("Item successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("ResponsibilityType"), Route("ResponsibilityType")]
        public JsonResult PostResponsibilityType(ResponsibilityType model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                //setting all permission as default
                int allPermissions = Permission.DeleteAsset.GetList().Sum(i => i.Value);
                model.ResponsibilityTypeRelations.ToList().
                    ForEach( x => { x.PermissionsBitMask = allPermissions;});

                Company.Add(model);

                return jsonSuccess("Item successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        #region ResponsibilityTypeRelation

        [HttpGet, ActionName("ResponsibilityTypeRelation_FormData"), Route("ResponsibilityTypeRelation_FormData"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelation_FormData()
        {
            var AllocationOptions = Company.Query<dynamic>(@"
select	cast(0 as bit) as IsUsed,
        A.ID, 
		case Object
			when 'ArtifactType' then 'Artifacts :: '
			when 'TaxonomyType' then 'Models :: '
			when 'PolicyType' then 'Policies :: '
			when 'RuleType' then 'Rules :: '
			when 'FusionAttributeType' then 'Fusion Attributes :: '
			when 'FusionType' then 'Fusion Types :: '
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end + coalesce(FT.Name+ ' / ','') + P.[Path] as [Path]
from	AssetType A
		cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
		left join FusionAttributeType FA on A.Object = 'FusionAttributeType' and FA.ID = A.ObjectID
		left join FusionType FT on FT.ID = FA.FusionTypeID
where	Class in (1,2,3,4,6,7,9)
order by case Object
			when 'ArtifactType' then 'Artifacts :: '
			when 'TaxonomyType' then 'Models :: '
			when 'PolicyType' then 'Policies :: '
			when 'RuleType' then 'Rules :: '
			when 'FusionAttributeType' then 'Fusion Attributes :: '
			when 'FusionType' then 'Fusion Types :: '
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end + coalesce(FT.Name+ ' / ','') + P.[Path]
").ToList();
            var PermissionOptions = Permission.DeleteAsset.GetList();

            return new JsonNetResult
            {
                Data = new
                {
                    PermissionOptions,
                    AllocationOptions
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpDelete, Route("ResponsibilityTypeRelation"), NonNullableParameters]
        public JsonResult DeleteResponsibilityTypeRelation(int responsibilityTypeId, string type, int typeId)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.Filter<ResponsibilityTypeRelation>(i => 
                    i.ResponsibilityTypeID == responsibilityTypeId && 
                    i.ObjectType == type && 
                    i.ObjectID == typeId).SingleOrDefault();

                if (model == null) throw new NotFoundException("responsibility type relation");

                Company.RemoveResponsibilityTypeRelation(model);

                return jsonSuccess("Item successfully removed.", "0", "delete", HttpStatusCode.OK);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("ResponsibilityTypeRelation"), Route("ResponsibilityTypeRelation")]
        public JsonResult PostResponsibilityTypeRelation(ResponsibilityTypeRelationViewModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var assetType = Company.GetById<AssetType>(model.AssetTypeID);

                if (assetType == null)
                    return jsonException("Asset Type not found", HttpStatusCode.BadRequest);

                var rtr = new ResponsibilityTypeRelation { ObjectID = assetType.ObjectID, ObjectType = assetType.Object, ResponsibilityTypeID = model.ResponsibilityTypeID, PermissionsBitMask = 0 };

                rtr.PermissionsBitMask = model.Permissions.Where(i => i.Selected).Sum(i => i.Value);

                Company.Add(rtr);

                return jsonSuccess("Item successfully created.", model.ResponsibilityTypeID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), ActionName("ResponsibilityTypeRelation"), Route("ResponsibilityTypeRelation")]
        public JsonResult PutResponsibilityTypeRelation(ResponsibilityTypeRelationViewModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var existing = Company.Filter<ResponsibilityTypeRelation>(r => r.ObjectType == model.ObjectType && r.ObjectID == model.ObjectID && r.ResponsibilityTypeID == model.ResponsibilityTypeID).SingleOrDefault();
                if (existing == null) throw new NotFoundException("responsibility type relation");
                                
                existing.PermissionsBitMask = model.Permissions.Where(i => i.Selected).Sum(i => i.Value);

                Company.Update(existing);
                
                return jsonSuccess("Item successfully updated.", model.ResponsibilityTypeID.ToString(), "edit", HttpStatusCode.OK);
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

        #region ResponsibilityTypeRelationRule

        #region JSON Feeds

        [HttpPost, AjaxValidateAntiForgeryToken, Route("ResponsibilityTypeRelationRule_WhenTest"), NonNullableParameters]
        public JsonNetResult ResponsibilityTypeRelationRule_WhenTest(ResponsibilityTypeRelationRule rule)
        {
            if (!Company.CurrentResourceIsAdmin)
                return new JsonNetResult { Data = new { Message = "Permission Denied" }, Formatting = Newtonsoft.Json.Formatting.None };

            var results = Company.Database.Connection.GetWhenResults(rule).OrderBy(i => i.Name);
            return new JsonNetResult { Data = results, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("ResponsibilityTypeRelationRule_ThenTest"), NonNullableParameters]
        public JsonNetResult ResponsibilityTypeRelationRule_ThenTest(ResponsibilityTypeRelationRule rule)
        {
            if (!Company.CurrentResourceIsAdmin)
                return new JsonNetResult { Data = new { Message = "Permission Denied" }, Formatting = Newtonsoft.Json.Formatting.None };

            var results = Company.Database.Connection.GetThenResults(rule, this.HideData3SixtyUsers()); 
            return new JsonNetResult { Data = results, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, ActionName("RelationsByResponsibilityType"), Route("RelationsByResponsibilityType"), NonNullableParameters]
        public JsonNetResult GetRelationsByResponsibilityType(int id)
        {
            var list = Company.Query<dynamic>($@"
select	{QueryConstants.HighLevelTypeCaseStatement} + T.Name as label,
		T.Object + '|' + cast(T.ObjectID as varchar) as value
from	ResponsibilityTypeRelation R
		inner join AssetType T on T.Object = R.ObjectType and T.ObjectID = R.ObjectID and R.ResponsibilityTypeID = {id}
        where R.ObjectType<>'FusionAttributeType'
order by {QueryConstants.HighLevelTypeCaseStatement} + T.Name");
            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, ActionName("ResponsibilityTypeRelationRule_FormData"), Route("ResponsibilityTypeRelationRule_FormData"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelationRule_FormData(SystemObjects type, int id)
        {
            var tempFieldDataTypes = new List<string>();
            limitedFieldTypes.ForEach(o => { tempFieldDataTypes.Add($"'{o}'"); });
            var ftTypeRemoveString = string.Join(",", tempFieldDataTypes);

            var fieldTypes = Company.Query<string>($@"
select	ID as value,
		FriendlyName as label,
		FT.Type as [type],
		case FT.Type
			when 'Lookup' then cast(1 as bit)
			else cast(0 as bit) 
		end as isLookup,
		(
		select	cast(value as varchar) as [value],
				Text as label 
		from	FieldLookupValue
		where	FieldTypeID = FT.ID
		for json auto
		) as [values]
from	FieldType FT
where	[Object] = @type
		and ObjectID = @id
		and Type not in ({ftTypeRemoveString})
for json auto, WITHOUT_ARRAY_WRAPPER", new { type = type.ToString(), id }).ToList();

            if (type == SystemObjects.OrganizationType)
            {
                fieldTypes = Company.Query<string>($@"
select	FT.ID as value,
		T.[Name] + ' :: ' + FriendlyName as label,
		FT.Type as [type],
		case FT.Type
			when 'Lookup' then cast(1 as bit)
			else cast(0 as bit) 
		end as isLookup,
		(
		select	cast(value as varchar) as [value],
				Text as label 
		from	FieldLookupValue
		where	FieldTypeID = FT.ID
		for json auto
		) as [values]
from	FieldType FT
inner join OrganizationType T on T.ID = FT.ObjectID and T.[State] = 1
where	[Object] = @type
		and Type not in ({ftTypeRemoveString})
order by T.[Name] + ' :: ' + FriendlyName
for json auto, WITHOUT_ARRAY_WRAPPER", new { type = type.ToString() }).ToList();
            }

            var groupFieldTypes = new List<string>();
            if (type == SystemObjects.GroupType)
            {
                groupFieldTypes = Company.Query<string>($@"
		select	0 as value,
				'Name' as label,
				'Lookup' as type,
				cast(1 as bit) as isLookup,
				(
				select	cast(ID as varchar) as [value],
						Name as label 
				from	[Group]
				order by Name
				for json auto
				) as [values]
for json path, WITHOUT_ARRAY_WRAPPER
").ToList();
            }

            var resourceFieldTypes = new List<string>();
            var hideUsersSql = "";
            
            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (Email not like '%@data3sixty.com' and Email not like '%@infogix.com')";
            }

            if (type == SystemObjects.ResourceType)
            {
                resourceFieldTypes = Company.Query<string>($@"
		select	0 as value,
				'Name' as label,
				'Lookup' as type,
				cast(1 as bit) as isLookup,
				(
				select	cast(ResourceID as varchar) as [value],
						LastName + ', ' + FirstName as label 
				from	reporting.Global_Resource 
				where	[State] = {(int) CompanyResourceState.Active} " + hideUsersSql +
				@"order by LastName + ', ' + FirstName
				for json auto
				) as [values]
for json path, WITHOUT_ARRAY_WRAPPER
").ToList();

            }

            var tempAggregatedFieldValue = "";
            var fieldTypeString = "[";

            tempAggregatedFieldValue = string.Join("", fieldTypes);
            fieldTypeString += $"{tempAggregatedFieldValue}";

            tempAggregatedFieldValue = string.Join("", groupFieldTypes);
            if (fieldTypeString.Length > 1 && !string.IsNullOrEmpty(tempAggregatedFieldValue))
                fieldTypeString +=  ", ";
            fieldTypeString +=  string.IsNullOrEmpty(tempAggregatedFieldValue) ? "" : $"{tempAggregatedFieldValue}";

            tempAggregatedFieldValue = string.Join("", resourceFieldTypes);
            if (fieldTypeString.Length > 1 && !string.IsNullOrEmpty(tempAggregatedFieldValue))
                fieldTypeString += ", ";
            fieldTypeString += string.IsNullOrEmpty(tempAggregatedFieldValue) ? "" : $"{tempAggregatedFieldValue}";

            fieldTypeString += "]";

            var fieldTypeArray = JArray.Parse(fieldTypeString);

            var intersectTypes = Company.Query<dynamic>($@"
select	ID as [value],
		case
			when (Subject = @type and SubjectID = @id) then ObjectName + ' (' + coalesce(PredicateName, '') + ')'
			else SubjectName + ' (' + coalesce(PredicateInverse, 'inverse') + ')'
		end as label
from	IntersectTypeDetail 
where	(Subject = @type and SubjectID = @id) 
		or (Object = @type and ObjectID = @id)
order by	case
				when (Subject = @type and SubjectID = {id}) then ObjectName + ' (' + coalesce(PredicateName, '') + ')'
				else SubjectName + ' (' + coalesce(PredicateInverse, 'inverse') + ')'
			end", new { type = type.ToString(), id });
            
            return new JsonNetResult
            {
                Data = new {
                    FieldTypes = fieldTypeArray,
                    IntersectTypes = intersectTypes
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, ActionName("ResponsibilityTypeRelationRuleRelationships_FormData"), Route("ResponsibilityTypeRelationRuleRelationships_FormData"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelationRuleRelationships_FormData(SystemObjects type, int id, int intersectTypeID)
        {
            string crossApplyValue;
            string labelValue;
            string objType;
            string joinColumn;
            int objId;

            var intersectType = Company.GetById<IntersectType>(intersectTypeID);

            if (intersectType.Object == type.ToString() && intersectType.ObjectID == id)
            {
                objType = intersectType.Subject;
                objId = intersectType.SubjectID;
                joinColumn = "Subject";
            }
            else
            {
                objType = intersectType.Object;
                objId = intersectType.ObjectID;
                joinColumn = "Object";
            }

            if (objType == SystemObjects.TaxonomyType.ToString() || objType == SystemObjects.PolicyType.ToString())
            {
                crossApplyValue = "getassettextpathbyid(D.id, '/') atp";
                labelValue = "atp.textpath";
            }
            else
            {
                crossApplyValue = "dbo.GetAssetDisplayValueById(D.ID) DN";
                labelValue = "DN.DisplayValue";
            }

            var items = Company.Query<dynamic>($@"
                select	D.Object + '|' + cast(D.ObjectID as varchar) as value,
		            {labelValue} as label 
                from	Asset D
                    inner join AssetType DT on DT.ID = D.AssetTypeID
                    inner join IntersectType I on I.{joinColumn} = DT.Object and I.{joinColumn}ID = DT.ObjectID and I.ID = {intersectTypeID}
                    cross apply {crossApplyValue}
                    order by {labelValue}");

                return new JsonNetResult
                {
                    Data = items,
                    Formatting = Newtonsoft.Json.Formatting.None
                };
        }
        
        #endregion

        #region Form Get/Post

        [HttpDelete,  Route("DeleteResponsibilityTypeRelationRuleByID"), NonNullableParameters]
        public async Task<JsonResult> DeleteResponsibilityTypeRelationRuleByID(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<ResponsibilityTypeRelationRule>(id);
                if (model == null) throw new NotFoundException("responsibility type rule");

                Company.Delete(model);
                await ((Company.Database.Connection as System.Data.SqlClient.SqlConnection).RemoveRelationRuleResultsByRule(id));

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

        [HttpGet, ActionName("ResponsibilityTypeRelationRule"), Route("ResponsibilityTypeRelationRule"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelationRule(int id)
        {

            ResponsibilityTypeRelationRule model;

            if (id < 1)
            {
                model = new ResponsibilityTypeRelationRule();
            }
            else
            {
                model = Company.GetById<ResponsibilityTypeRelationRule>(id);
                model.SetDefinitionFromRaw();
            }

            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPut, ValidateInput(false),  ActionName("ResponsibilityTypeRelationRule"), Route("ResponsibilityTypeRelationRule")]
        public async Task<JsonResult> PutResponsibilityTypeRelationRule(ResponsibilityTypeRelationRule model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var existing = Company.GetById<ResponsibilityTypeRelationRule>(model.ID);
                if (existing == null) throw new NotFoundException("ownership type");

                existing.Name = model.Name;
                existing.StructuredDefinition = model.StructuredDefinition;
                existing.Object = model.Object;
                existing.ObjectID = model.ObjectID;
                existing.ResponsibilityTypeID = model.ResponsibilityTypeID;
                existing.Context = model.Context;
                existing.ApplyToType = model.ApplyToType;
                existing.IsVisible = model.IsVisible;
                existing.UpdatedOn = DateTime.UtcNow;

                var previousDefinition = existing.Definition;
                existing.SetRawFromDefinition();
                if (existing.StructuredDefinition?.Then?.Conditions?.Where(x => x.Value == null).Count() > 0)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "ResponsibilityType", FormInfo.Responsibility_Then_Filter_Value_Required);
                }

                var definitionIsDifferent = (previousDefinition != existing.Definition);
                if (definitionIsDifferent)
                {
                    existing.LastRunOn = DateTime.Parse("1/1/2000");
                }

                Company.Update(existing);

                // Re-process this rule.
                if (definitionIsDifferent)
                {
                    await ((Company.Database.Connection as System.Data.SqlClient.SqlConnection).ProcessResponsibilityRelationRules(existing.ID));
                }
                

                return jsonSuccess("Item successfully updated and processed.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false),  ActionName("ResponsibilityTypeRelationRule"), Route("ResponsibilityTypeRelationRule")]
        public async Task<JsonResult> PostResponsibilityTypeRelationRule(ResponsibilityTypeRelationRule model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                model.SetRawFromDefinition();
                if (model.StructuredDefinition?.Then?.Conditions?.Where(x => x.Value == null).Count() > 0)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "ResponsibilityType", FormInfo.Responsibility_Then_Filter_Value_Required);
                }

                model.UpdatedOn = DateTime.UtcNow;
                Company.Add(model);

                // Process this rule.
                await ((Company.Database.Connection as System.Data.SqlClient.SqlConnection).ProcessResponsibilityRelationRules(model.ID));

                return jsonSuccess("Item successfully created and processed.", model.ID.ToString(), "add", HttpStatusCode.Created);
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
                        State = state
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

        #region QuestionType

        #region JSON Feeds

        [Route("QuestionType_FormData"), NonNullableParameters]
        public JsonNetResult QuestionType_FormData(int surveyTypeID, int id = 0)
        {
            QuestionType qt = null;
            List<QuestionTypeItemEditorModel> items = null;

            var options = QuestionDisplayStyle.Radio.GetResponseTypeDisplayStyleInfoList().Where(x=>x.ID != QuestionDisplayStyle.Rating).Select(i => new KnockoutDisplayItem { title = i.Description, value = ((int)i.ID).ToString() });

            if (id > 0)
            {
                qt = Company.GetById<QuestionType>(id, i => i.QuestionTypeOptions);

                if (qt.QuestionTypeOptions != null)
                {
                    if (qt.QuestionTypeOptions.Count > 0)
                    {
                        items = new List<QuestionTypeItemEditorModel>();
                        foreach (var i in qt.QuestionTypeOptions)
                        {
                            items.Add(new QuestionTypeItemEditorModel
                            {
                                ID = i.ID,
                                Name = i.Name,
                                Value = i.Value
                            });
                        }
                    }
                }
            }
            else
            {
                qt = new QuestionType { Name = "", DisplayStyle = QuestionDisplayStyle.Radio, SurveyTypeID = surveyTypeID, Description = ""  };
            }

            return new JsonNetResult
            {
                Data = new QuestionTypeEditorModel
                {
                    Name = qt.Name,
                    Description = qt.Description,
                    DisplayStyle = qt.DisplayStyle,
                    SurveyTypeID = surveyTypeID,
                    DisplayStyleOptions = options.ToList(),
                    ID = id,
                    Items = items,
                    LimitedChangesOnly = false
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion
              

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddQuestionType")]
        public JsonResult AddQuestionType(QuestionTypeEditorModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var val = model.Validation();

                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                var qt = new QuestionType
                {
                    Name = model.Name,
                    SurveyTypeID = model.SurveyTypeID,
                    DisplayStyle = model.DisplayStyle,
                    Description = model.Description,
                    QuestionTypeOptions = new List<QuestionTypeOption>()
                };

                foreach (var item in model.Items)
                {
                    var itemVal = item.Validation();
                    if (itemVal.Valid)
                    {
                        qt.QuestionTypeOptions.Add(new QuestionTypeOption { Name = item.Name, Value = item.Value });
                    }
                }

                Company.Add(qt);

                return jsonSuccess("Survey question successfully created.", qt.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteQuestionType")]
        public JsonResult DeleteQuestionType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("response type");

                var id = parseIntField(form, "ID");
                Company.Delete<QuestionType>(i => i.ID == id);

                return jsonSuccess("Survey question successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [ HttpPut, ValidateInput(false), Route("EditQuestionType")]
        public JsonResult EditQuestionType(QuestionTypeEditorModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var val = model.Validation();

                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                var qt = Company.GetById<QuestionType>(model.ID, i => i.QuestionTypeOptions);

                if (qt == null)
                    throw new NotFoundException("Question");

                qt.Name = model.Name;
                qt.DisplayStyle = model.DisplayStyle;
                qt.Description = model.Description;

                //Process new and updated options.
                foreach (var item in model.Items)
                {
                    var itemVal = item.Validation();
                    if (itemVal.Valid)
                    {
                        if (item.ID > 0)
                        {
                            if (qt.QuestionTypeOptions.Any(i => i.ID == item.ID))
                            {
                                qt.QuestionTypeOptions.Single(i => i.ID == item.ID).Name = item.Name;
                                qt.QuestionTypeOptions.Single(i => i.ID == item.ID).Value = item.Value;
                            }
                        }
                        else
                        {
                            qt.QuestionTypeOptions.Add(new QuestionTypeOption { Name = item.Name, Value = item.Value });
                        }
                    }
                }

                //Process deleted options.
                var IDs = new List<int>();
                foreach (var item in qt.QuestionTypeOptions)
                {
                    if (!model.Items.Any(i => i.ID == item.ID))
                    {
                        IDs.Add(item.ID);
                    }
                }

                foreach (var id in IDs)
                {
                    var qto = qt.QuestionTypeOptions.Single(i => i.ID == id);
                    Company.QuestionTypeOptions.Remove(qto);
                }

                Company.Update(qt);

                return jsonSuccess("Survey question successfully updated.", qt.ID.ToString(), "update", HttpStatusCode.OK);
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

        #region Rule

        #region Field Generation

        [Route("Rule_AddFields")]
        public JsonResult Rule_AddFields(int typeID)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.RuleType, typeID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var statuses = RuleStatus.Active.GetStatusEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            var dimensions = Company.RuleDimensions.Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldType = DataType.Hidden.ToString(), FieldName = "RuleTypeID", Value = typeID.ToString() });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Status", Name = FieldInfo.RuleStatus_Name, FieldDescription = FieldInfo.RuleStatus_Description, Items = statuses, FieldType = DataType.Lookup.ToString() });
            list.Add(new EditableField { Row = 1, Column = 2, Required = false, FieldName = "RuleDimensionID", Name = FieldInfo.RuleDimension_Name, FieldDescription = FieldInfo.RuleDimension_Description, Items = dimensions, FieldType = DataType.Lookup.ToString() });
                        
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Threshold", Name = FieldInfo.RuleThreshold_Name, FieldDescription = FieldInfo.RuleThreshold_Description, FieldType = DataType.Percentage.ToString()});
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.RuleType, typeID).ToList(), 3);


            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("Rule_EditFields"), NonNullableParameters]
        public JsonResult Rule_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var statuses = RuleStatus.Active.GetStatusEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            var dimensions = Company.RuleDimensions.Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            
            var model = Company.GetById<Rule>(id);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Status", Name = FieldInfo.RuleStatus_Name, FieldDescription = FieldInfo.RuleStatus_Description, Items = statuses, FieldType = DataType.Lookup.ToString(), Value = ((int)model.Status).ToString() });
            list.Add(new EditableField { Row = 1, Column = 2, Required = false, FieldName = "RuleDimensionID", Name = FieldInfo.RuleDimension_Name, FieldDescription = FieldInfo.RuleDimension_Description, Items = dimensions, FieldType = DataType.Lookup.ToString(), Value = model.RuleDimensionID.GetValueOrDefault(-1).ToString() });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Threshold", Name = FieldInfo.RuleThreshold_Name, FieldDescription = FieldInfo.RuleThreshold_Description, FieldType = DataType.Percentage.ToString(), Value = model.Threshold.ToString() });

            list = (
                loadDynamicFields(
                    SystemObjects.Rule.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Rule, id).ToList(), 
                    3
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Rule_SimilarItems"), NonNullableParameters]
        public JsonNetResult Rule_SimilarItems(string query)
        {
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.SimilarItems, new { type = "Rule", typeID = (int?)null, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRule")]
        public JsonResult AddRule(FormCollection form)
        {
            try
            {                
                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var threshold = decimal.Parse(form["Threshold"]);

                if (threshold < 0 || threshold > 1)
                    throw new InvalidDataException("Threshold value must be between 0 and 1");

                var model = new Rule
                {
                    RuleDimensionID = parseNullableIntField(form, "RuleDimensionID"),
                    RuleTypeID = parseIntField(form, "RuleTypeID"),
                    Status = (RuleStatus)Enum.Parse(typeof(RuleStatus), form["Status"]),
                    Threshold = threshold
                };

                if (!Company.HasAssetTypePermission(SystemObjects.RuleType, model.RuleTypeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Rule, model.ID, Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList(), form, Server);
                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList();
                Company.SaveOrUpdate<Rule>(model, fields);
                processFormDynamicRelationshipFields(SystemObjects.RuleType, model.RuleTypeID, SystemObjects.Rule, model.ID, fieldTypes, form);

                dynamic custom = new
                {
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess("Rule successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeleteRule")]
        public JsonResult DeleteRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Rule>(id);
                if (model == null) throw new NotFoundException("Rule");

                if (!Company.HasAssetPermission(SystemObjects.Rule, model.ID, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.Rule, model.ID);

                dynamic custom = new
                {
                    action = "delete",
                    Context = form["_context"]
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

        [HttpPut, ValidateInput(false), Route("EditRule")]
        public JsonResult EditRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Rule>(id);
                if (model == null) throw new NotFoundException("Rule");

                if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var dimension = parseNullableIntField(form, "RuleDimensionID");

                if (dimension.HasValue && dimension.GetValueOrDefault() > 0)
                    model.RuleDimensionID = dimension;
                else
                    model.RuleDimensionID = null;

                var threshold = decimal.Parse(form["Threshold"]);
                if (threshold < 0 || threshold > 1)
                    throw new InvalidDataException("Threshold value must be between 0 and 1");

                model.Status = (RuleStatus)Enum.Parse(typeof(RuleStatus), form["Status"]);
                model.Threshold = threshold;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Rule, model.ID, Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList(), form, Server, false);
                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList();
                Company.SaveOrUpdate<Rule>(model, fields);
                processFormDynamicRelationshipFields(SystemObjects.RuleType, model.RuleTypeID, SystemObjects.Rule, model.ID, fieldTypes, form);

                dynamic custom = new
                {
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess("Rule successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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
        
        #region RuleDimension

        #region Field Generation

        [Route("RuleDimension_AddFields")]
        public JsonResult RuleDimension_AddFields()
        {
            var model = new RuleDimension();
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("RuleDimension_EditFields"), NonNullableParameters]
        public JsonResult RuleDimension_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var model = Company.GetById<RuleDimension>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Html.ToString(), Value = model.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRuleDimension")]
        public JsonResult AddRuleDimension(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("RuleDimension");

                var model = new RuleDimension
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                Company.Add(model);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeleteRuleDimension")]
        public JsonResult DeleteRuleDimension(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("RuleDimension");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleDimension>(id);
                if (model == null) throw new NotFoundException("RuleDimension");

                if (Company.Rules.Where(x => x.RuleDimensionID == id).Any())
                {
                    return jsonException(FormInfo.Delete_Error_Rule_Exist, HttpStatusCode.Forbidden);
                }

                Company.Delete(model);

                dynamic custom = new
                {
                    model.Name,
                    action = "delete",
                    Context = form["_context"]
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

        [HttpPut, ValidateInput(false), Route("EditRuleDimension")]
        public JsonResult EditRuleDimension(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("RuleDimension");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleDimension>(id);
                if (model == null) throw new NotFoundException("RuleDimension");

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update(model);

                dynamic custom = new
                {
                    model.Name,
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        #region RuleImplementation

        #region Field Generation

        /// <param name="ruleID">RuleID</param>
        [Route("RuleImplementation_AddFields")]
        public JsonResult RuleImplementation_AddFields(int ruleID)
        {
            if (!Company.HasAssetPermission(SystemObjects.Rule, ruleID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldType = DataType.Hidden.ToString(), FieldName = "RuleID", Value = ruleID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = FieldInfo.RuleImplementation_Name, FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = "form/Rule_SimilarItems?query=" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "SourceID", Name = FieldInfo.RuleImplementation_SourceID, FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "SourceUri", Name = FieldInfo.RuleImplementation_SourceUri, FieldType = DataType.Text.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleImplementationID</param>
        [Route("RuleImplementation_EditFields"), NonNullableParameters]
        public JsonResult RuleImplementation_EditFields(int id)
        {
            var model = Company.GetById<RuleImplementation>(id);

            if ((!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset)))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = FieldInfo.RuleName_Description, FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "SourceID", Name = FieldInfo.RuleImplementation_SourceID, FieldType = DataType.Text.ToString(), Value = model.SourceID });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "SourceUri", Name = FieldInfo.RuleImplementation_SourceUri, FieldType = DataType.Text.ToString(), Value = model.SourceUri });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RuleImplementation_CopyFields(int implementationID)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldType = DataType.Hidden.ToString(), FieldName = "ID", Value = implementationID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = FieldInfo.RuleImplementation_Name, FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = "form/Rule_SimilarItems?query=" });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRuleImplementation")]
        public JsonResult AddRuleImplementation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule Implementation");

                var model = new RuleImplementation
                {
                    RuleID = parseIntField(form, "RuleID"),
                    Name = parseTextField(form, "Name"),
                    SourceID = parseTextField(form, "SourceID"),
                    SourceUri = parseTextField(form, "SourceUri")
                };

                if (!Company.HasAssetPermission(SystemObjects.Rule, model.RuleID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Add(model);

                dynamic custom = new
                {
                    model.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeleteRuleImplementation")]
        public JsonResult DeleteRuleImplementation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule Implementation");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleImplementation>(id);
                if (model == null) throw new NotFoundException("Rule Implementation");

                if (!Company.HasAssetPermission(SystemObjects.Rule, model.RuleID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                
                //delete rule implementation qualifiters

                var qualifiers = Company.RuleResultQualifierTypes.Where(x => x.RuleImplementationID == id);

                // delete rule qualifiers

                foreach (var qualifier in qualifiers)
                {
                    var items = Company.RuleResultQualifiers.Where(x => x.RuleResultQualifierTypeID == qualifier.ID);
                    if (items.Any())
                    {
                        Company.RuleResultQualifiers.RemoveRange(items);
                    }
                }

                Company.RuleResultQualifierTypes.RemoveRange(qualifiers);

                //delete rule results for this implementation
                var res = Company.RuleResults.Where(x => x.RuleImplementationID == id);
                if (res.Any())
                {
                    foreach(var ruleResult in res)
                    {
                        var ruleResultFusionAttributes = Company.RuleResultFusionAttributes.Where(x => x.RuleResultID == ruleResult.ID);
                        if (ruleResultFusionAttributes.Any())
                        {
                            Company.RuleResultFusionAttributes.RemoveRange(ruleResultFusionAttributes);                           
                        }
                    }
                    Company.RuleResults.RemoveRange(res);                    
                }
                Company.SaveChanges();
                Company.Delete(model);

                dynamic custom = new
                {
                    model.Name,
                    action = "delete",
                    Context = form["_context"]
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

        [HttpPut, ValidateInput(false), Route("EditRuleImplementation")]
        public JsonResult EditRuleImplementation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule Implementation");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleImplementation>(id);
                if (model == null) throw new NotFoundException("Rule Implementation");

                if ((!Company.HasAssetPermission(SystemObjects.Rule, model.RuleID, Permission.ModifyAsset)))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.SourceID = parseTextField(form, "SourceID");
                model.SourceUri = parseTextField(form, "SourceUri");

                Company.Update(model);

                dynamic custom = new
                {
                    model.Name,
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        public JsonResult CopyRuleImplementation(FormCollection form)
        {
            try
            {
                int implementationID = parseIntField(form, "ID");
                string implementationName = parseTextField(form, "Name");

                var ExistingImplementation = Company.GetById<RuleImplementation>(implementationID, i => i.Rule.RuleType);

                if (!Company.HasAssetPermission(SystemObjects.Rule, ExistingImplementation.RuleID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var Model = new RuleImplementation
                {
                    RuleID = ExistingImplementation.RuleID,
                    Name = !string.IsNullOrEmpty(implementationName) ? implementationName : ExistingImplementation.Name,
                    SourceID = ExistingImplementation.SourceID,
                    SourceUri = ExistingImplementation.SourceUri
                };
                Company.Add(Model);

                var QualifierTypeList = Company.Query<RuleResultQualifierType>(@"select R.*, D.Name as ResolutionObjectName from RuleResultQualifierType R
                left join AssetType D on D.[Object] = R.ResolutionObject and D.ObjectID = R.ResolutionObjectID
                where R.RuleImplementationID = @implementationID
                order by R.[Order]", new { implementationID });

                foreach (var qualifierType in QualifierTypeList)
                {
                    qualifierType.RuleImplementationID = Model.ID;
                    Company.RuleResultQualifierTypes.Add(qualifierType);
                }
                Company.SaveChanges();

                dynamic custom = new
                {
                    Model.Name,
                    action = "copy",
                    Context = form["_context"]
                };

                return jsonSuccess(ExistingImplementation.Name + " successfully copied.", Model.ID.ToString(), "copy", HttpStatusCode.Created, custom);
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

        #region RuleQualifierType

        #region Form Get/Post

        [HttpPut, Route("MoveRuleQualifierType"), ValidateInput(false)]
        public JsonResult MoveRuleQualifierType(int id, bool moveUp = false)
        {
            try
            {
                var q = Company.GetById<RuleResultQualifierType>(id, i => i.RuleImplementation);
                if (q == null)
                    throw new Exception($"Could not find rule qualifier for id '{id}'");

                if (!Company.HasAssetPermission(SystemObjects.Rule, q.RuleImplementation.RuleID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var otherRule = Company.RuleResultQualifierTypes.Where(r => r.RuleImplementationID == q.RuleImplementationID && r.Order == (moveUp ? q.Order - 1 : q.Order + 1)).SingleOrDefault();
                if (otherRule != null)
                {
                    q.Order += (moveUp ? -1 : 1);
                    otherRule.Order += (moveUp ? 1 : -1);
                    Company.SaveChanges();
                }
            } catch(Exception ex)
            {
                return jsonException(ex.Message, HttpStatusCode.OK);
            }
            return jsonSuccess("Rule Qualifier moved", id.ToString(), "move", HttpStatusCode.OK);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("AddRuleQualifierType"), ValidateInput(false)]
        public JsonResult AddQualifierType(RuleResultQualifierType model)
        {
            try
            {
                if (model == null)
                    throw new Exception("Supplied model was null");

                model.Order = Company.Count<RuleResultQualifierType>(r => r.RuleImplementationID == model.RuleImplementationID) + 1;

                if (Company.RuleResultQualifierTypes.Any(x => x.RuleImplementationID == model.RuleImplementationID && string.Compare(x.Name, model.Name, true) == 0))
                    return jsonException("A rule result qualifier type with the same name already exists.  Please make sure you use a unique name.", HttpStatusCode.Conflict);

                Company.RuleResultQualifierTypes.Add(model);
                Company.SaveChanges();
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.OK);
            }

            return jsonSuccess("Qualifier Type added successfully", model.ID.ToString(), "add", HttpStatusCode.OK);
        }

        [HttpPut, Route("EditRuleQualifierType"), ValidateInput(false)]
        public JsonResult EditQualifierType(RuleResultQualifierType model)
        {
            try
            {
                if (model == null)
                    throw new Exception("Supplied model was null");

                var qualifier = Company.GetById<RuleResultQualifierType>(model.ID, i => i.RuleImplementation);

                if (qualifier == null)
                    throw new Exception($"Cannot find qualifier id '{model?.ID}'");

                if (!Company.HasAssetPermission(SystemObjects.Rule, qualifier.RuleImplementation.RuleID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);


                if (Company.RuleResultQualifierTypes.Any(x => x.RuleImplementationID == model.RuleImplementationID && string.Compare(x.Name, model.Name, true) == 0 && x.ID != qualifier.ID))
                    return jsonException("A rule result qualifier type with the same name already exists.  Please make sure you use a unique name.", HttpStatusCode.Conflict);

                qualifier.Name = model.Name;
                qualifier.ResolutionObject = model.ResolutionObject;
                qualifier.ResolutionObjectID = model.ResolutionObjectID;
                qualifier.ResolutionFieldTypeID = model.ResolutionFieldTypeID;
                qualifier.ResolutionFieldTypeName = model.ResolutionFieldTypeName;

                Company.SaveChanges();

                return jsonSuccess("Qualifier Type edited successfully", model.ID.ToString(), "edit", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.OK);
            }
        }

        [HttpDelete, Route("DeleteQualifierType")]
        public JsonResult DeleteQualifierType(int id)
        {
            try
            {
                var qualifier = Company.GetById<RuleResultQualifierType>(id, i => i.RuleImplementation);
                if (qualifier == null)
                    throw new Exception($"Could not find qualifier type id {id}");

                if (!Company.HasAssetPermission(SystemObjects.Rule, qualifier.RuleImplementation.RuleID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.RuleResultQualifierTypes.Remove(qualifier);
                Company.SaveChanges();
            }
            catch (Exception ex)
            {
                return jsonException(ex, HttpStatusCode.OK);
            }
            return jsonSuccess("Qualifier Type deleted successfully", id.ToString(), "delete", HttpStatusCode.OK);
        }

        #endregion

        #endregion

        #region RuleType

        #region Field Generation

        [Route("RuleType_AddFields")]
        public JsonResult RuleType_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "DisplayFormat", Name = FieldInfo.DisplayFormat_Name, FieldDescription = FieldInfo.DisplayFormat_Description, FieldType = DataType.Text.ToString(), Value="{Name}", Validations = checkAndAddValidation("DisplayFormat", FieldInfo.DisplayFormat_Name, true, "", 2, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = FieldInfo.Description_Name, FieldDescription = "", FieldType = DataType.Html.ToString() });
            loadIconFields(list, 3);
                        
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <param name="id">PolicyTypeID</param>
        [Route("RuleType_EditFields"), NonNullableParameters]
        public JsonResult RuleType_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.RuleType, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<RuleType>(id);
            var style = Company.GetObjectStyle(SystemObjects.RuleType, id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "DisplayFormat", Name = FieldInfo.DisplayFormat_Name, FieldDescription = FieldInfo.DisplayFormat_Description, FieldType = DataType.Text.ToString(), Value = a.DisplayFormat, Validations = checkAndAddValidation("DisplayFormat", FieldInfo.DisplayFormat_Name, true, "", 2, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = FieldInfo.Description_Name, FieldDescription = "", FieldType = DataType.Html.ToString(), Value = a.Description });
            loadIconFields(list, 3, style);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRuleType")]
        public JsonResult AddRuleType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("rule type");

                var a = new RuleType
                {
                    Name = parseTextField(form, "Name"),
                    DisplayFormat = parseTextField(form, "DisplayFormat"),
                    Description = parseTextField(form, "Description")
                };

                Company.Add(a);

                Company.Add(new FieldType
                {
                    ObjectID = a.ID,
                    Object = "RuleType",
                    IsListable = true,
                    IsRequired = true,
                    IsEditable = true,
                    FriendlyName = "Name",
                    Name = "Name",
                    MaximumLength = 500,
                    MinimumLength = 1,
                    SortOrder = 1,
                    Type = DataType.Text.ToString(),
                    IsDisplayable = true,
                    IsPartOfKey = true
                });

                upsertObjectStyle(SystemObjects.RuleType, a.ID, form, a.Name);

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

        [HttpDelete, Route("DeleteRuleType")]
        public JsonResult DeleteRuleType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("rule type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleType>(id);
                if (model == null) throw new NotFoundException("rule type");

                if (!Company.HasAssetTypePermission(SystemObjects.RuleType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.RuleType, id);

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

        [HttpPut, ValidateInput(false), Route("EditRuleType")]
        public JsonResult EditRuleType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("rule type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleType>(id);
                if (model == null) throw new NotFoundException("rule type");

                if (!Company.HasAssetTypePermission(SystemObjects.RuleType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.DisplayFormat = parseTextField(form, "DisplayFormat");
                model.Description = parseTextField(form, "Description");

                Company.Update(model);

                upsertObjectStyle(SystemObjects.RuleType, model.ID, form, model.Name);

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

        #region SurveyType

        #region Field Generation

        [Route("SurveyType_AddFields")]
        public JsonResult SurveyType_AddFields()
        {
            var list = new List<EditableField>();

            var items = new List<SelectListItem>();
            //artifacts
            items.AddRange(Company.Table<ArtifactType>().OrderBy(i => i.Name).Select(i => new { i.ID, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Artifact Type :: {0}", i.Name), Value = string.Format("{0}|{1}", SystemObjects.ArtifactType.ToString(), i.ID) }));

            //models
            items.AddRange(Company.Table<TaxonomyType>().OrderBy(i => i.Name).Select(i => new { i.ID, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Model Type :: {0}", i.Name), Value = string.Format("{0}|{1}", SystemObjects.TaxonomyType.ToString(), i.ID) }));

            //rules
            items.Add(new SelectListItem { Text = "Rule Type :: Informational", Value = "RuleType|1" });
            items.Add(new SelectListItem { Text = "Rule Type :: Quality Check", Value = "RuleType|2" });
            items.Add(new SelectListItem { Text = "Rule Type :: Metric", Value = "RuleType|3" });
            items.Add(new SelectListItem { Text = "Rule Type :: Profile", Value = "RuleType|4" });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Object", Name = "Assign Survey To", FieldType = DataType.Lookup.ToString(), Items = items });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "ValidForDays", Name = "# of Days before user can retake", FieldType = DataType.Number.ToString()});
            

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        [Route("SurveyType_EditFields"), NonNullableParameters]
        public JsonResult SurveyType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<SurveyType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ValidForDays", Name = "# of Days before user can retake", FieldType = DataType.Number.ToString(), Value = a.ValidForDays.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddSurveyType")]
        public JsonResult AddSurveyType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("survey type");

                var otVal = form["Object"].Split('|').ToList();
                var ot = (SystemObjects)Enum.Parse(typeof(SystemObjects), otVal[0]);
                var oid = int.Parse(otVal[1]);

                var model = new SurveyType
                {
                    Name = parseTextField(form, "Name"),
                    Object = ot.ToString(),
                    ObjectID = oid,
                    ValidForDays = parseNullableIntField(form, "ValidForDays", 1).GetValueOrDefault(1)
                };
                Company.Add(model);

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

        [HttpDelete, Route("DeleteSurveyType")]
        public JsonResult DeleteSurveyType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("survey type");

                var id = parseIntField(form, "ID");
                // delete this surveys questions..
                
                Company.Delete<Question>(i => i.Survey.SurveyTypeID == id);
                Company.Delete<Survey>(i => i.SurveyTypeID == id);

                Company.Delete<QuestionType>(i => i.SurveyTypeID == id);
                Company.Delete<SurveyType>(i => i.ID == id);

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

        [HttpPut, ValidateInput(false), Route("EditSurveyType")]
        public JsonResult EditSurveyType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("survey type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<SurveyType>(id);
                if (model == null) throw new NotFoundException("survey type");

                model.Name = parseTextField(form, "Name");
                model.ValidForDays = parseNullableIntField(form, "ValidForDays", 1).GetValueOrDefault(1);

                Company.Update(model);

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

        #region Taxonomy

        #region Field Generation

        /// <param name="t">TaxonomyTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("Taxonomy_AddFields"), NonNullableParameters]
        public JsonResult Taxonomy_AddFields(int t, int p)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, t, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "TaxonomyTypeID", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });
            list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, t).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyID</param>
        [Route("Taxonomy_EditFields"), NonNullableParameters]
        public JsonResult Taxonomy_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Taxonomy, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Taxonomy>(id, i => i.TaxonomyType);

            var parent = Company.GetParentObject(a.ID, SystemObjects.Taxonomy);

            var parents = Company.Query<dynamic>(@"
select	A.ObjectID as ID,
		P.TextPath as Name,
		coalesce(X.[Level], 1) as [Level]
from	Asset A
        inner join Taxonomy X on X.ID = A.ObjectID
        inner join AssetType T on T.ID = A.AssetTypeID and T.Object = 'TaxonomyType' and T.ObjectID = @t
		cross apply dbo.GetAssetTextPathById(A.ID, '/') P
where (coalesce(x.[Level], 1) + @currentLevel) <= @maxLevel
option (maxrecursion 100)",
new { t = a.TaxonomyTypeID, currentLevel = a.Level ?? 1, maxLevel = a.TaxonomyType.MaximumDepth ?? 1 }).Select(i => new { i.ID, i.Name }).ToList();

            var thisEntry = parents.FirstOrDefault(i => i.ID == id);

            parents.RemoveAll(i => i.Name.StartsWith(thisEntry.Name));

            var parentItems = parents.Select(i => new SelectListItem {
                Text = i.Name,
                Value = $"{i.ID}",
                Selected = (parent != null ? ((int)i.ID == parent.ObjectID) : false)
            }).ToList();
            parentItems.Insert(0, new SelectListItem { Text = "- Root -", Value = "0", Selected = (parent == null) });

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ParentID", Name = "Parent Model", FieldDescription = FormInfo.Taxonomy_ChangeParent_Warning, FieldType = DataType.Lookup.ToString(), Items = parentItems, Value = ((parent != null) ? parent.ObjectID.ToString() : "0") });
            list =(
                loadDynamicFields(
                    SystemObjects.Taxonomy.ToString(),
                    id,
                    list, 
                    Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, a.TaxonomyTypeID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Taxonomy, id).ToList(), 
                    3
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        
        
        #endregion

        #region Form Get/Post

        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddTaxonomy")]
        public JsonResult AddTaxonomy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy");

                int typeID = parseIntField(form, "TaxonomyTypeID");
                var type = Company.GetById<TaxonomyType>(typeID);
                if (type == null) throw new NotFoundException("taxonomy type");

                int? parentId = parseNullableIntField(form, "ParentID");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, typeID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new Taxonomy { TaxonomyTypeID = typeID };

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Taxonomy, model.ID, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, typeID).ToList(), form, Server);
                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, model.TaxonomyTypeID).ToList();
                Company.SaveOrUpdate<Taxonomy>(model, fields, parentId.GetValueOrDefault());
                processFormDynamicRelationshipFields(SystemObjects.TaxonomyType, model.TaxonomyTypeID, SystemObjects.Taxonomy, model.ID, fieldTypes, form);

                if (!string.IsNullOrEmpty(form["ParentID"]) && form["ParentID"] != "0")
                {                    
                    var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                        i.Object == "TaxonomyType" &&
                        i.ObjectID == type.ID &&
                        i.PredicateType.Value == PredicateType.IntraTypeHierarchy
                    ).SingleOrDefault();

                    if (intersectType != null)
                    {
                        var intersect = new Intersect
                        {
                            Subject = SystemObjects.Taxonomy.ToString(),
                            SubjectID = parseIntField(form, "ParentID"),
                            Object = SystemObjects.Taxonomy.ToString(),
                            ObjectID = model.ID,
                            IntersectTypeID = intersectType.ID
                        };

                        var parentExists = Company.Any<Asset>(i =>
                            i.ObjectID == intersect.SubjectID &&
                            i.AssetType.Object == "TaxonomyType" &&
                            i.AssetType.ObjectID == intersectType.SubjectID
                            );

                        if (!parentExists)
                        {
                            return jsonException($"Parent {intersectType.SubjectName} with ID {intersect.SubjectID} could not be found.", HttpStatusCode.NotFound);
                        }

                        Company.Add(intersect);
                    }
                }

                dynamic custom = new
                {
                    TaxonomyTypeID = typeID,
                    ID = model.ID,
                    Context = form["_context"]
                };

                return jsonSuccess("Model successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeleteTaxonomy")]
        public JsonResult DeleteTaxonomy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy");

                var id = parseIntField(form, "ID");

                if (!Company.HasAssetPermission(SystemObjects.Taxonomy, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<Taxonomy>(id);
                if (model == null) throw new NotFoundException("taxonomy");
                
                dynamic custom = new
                {
                    model.TaxonomyTypeID,
                    Context = form["_context"]
                };

                Company.Delete(SystemObjects.Taxonomy, id);
                
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

        [HttpPut, ValidateInput(false), Route("EditTaxonomy")]
        public JsonResult EditTaxonomy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Taxonomy>(id);

                if (model == null) throw new NotFoundException("taxonomy");

                if (!Company.HasAssetPermission(SystemObjects.Taxonomy, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var parentID = parseIntField(form, "ParentID");

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Taxonomy, model.ID, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, model.TaxonomyTypeID).ToList(), form, Server, false);
                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, model.TaxonomyTypeID).ToList();
                Company.SaveOrUpdate<Taxonomy>(model, fields);
                processFormDynamicRelationshipFields(SystemObjects.TaxonomyType, model.TaxonomyTypeID, SystemObjects.Taxonomy, model.ID, fieldTypes, form);


                var sType = SystemObjects.Taxonomy.ToString();
                

                if (parentID > 0)
                {
                    var intersect = Company.Filter<Intersect>(i =>
                        i.Subject == sType &&
                        i.Object == sType &&
                        i.ObjectID == model.ID &&
                        i.IntersectType.Predicate.Type == PredicateType.IntraTypeHierarchy
                    ).SingleOrDefault();

                    if (intersect != null)
                    {
                        if (intersect.SubjectID != parentID)
                        {
                            intersect.SubjectID = parentID;
                            Company.Update(intersect);
                        }
                    }
                    else
                    {
                        var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                            i.Object == "TaxonomyType" &&
                            i.ObjectID == model.TaxonomyTypeID &&
                            i.PredicateType.Value == PredicateType.IntraTypeHierarchy
                        ).SingleOrDefault();

                        if (intersectType != null)
                        {
                            intersect = new Intersect
                            {
                                Subject = SystemObjects.Taxonomy.ToString(),
                                SubjectID = parseIntField(form, "ParentID"),
                                Object = SystemObjects.Taxonomy.ToString(),
                                ObjectID = model.ID,
                                IntersectTypeID = intersectType.ID
                            };

                            Company.Add(intersect);
                        }
                    }
                }

                dynamic custom = new
                {
                    model.TaxonomyTypeID,
                    ParentID = parentID,
                    Context = form["_context"]
                };

                return jsonSuccess("Model successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        #region TaxonomyType

        [HttpDelete, Route("DeleteTaxonomyType")]
        public JsonResult DeleteTaxonomyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<TaxonomyType>(id);
                if (model == null) throw new NotFoundException("taxonomy type");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.TaxonomyType, id);

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

        #region TaxonomyTypeLevel

        #region Form Get/Post
    
        [ HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddTaxonomyTypeLevel")]
        public JsonResult AddTaxonomyTypeLevel(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type level");

                var a = new TaxonomyTypeLevel
                {
                    TaxonomyTypeID = id,
                    Level = level,
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<TaxonomyTypeLevel>(a);

                return jsonSuccess(a.Name + " successfully created.", a.TaxonomyTypeID.ToString(), "add", HttpStatusCode.Created);
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

        [Route("TaxonomyType/{taxonomyTypeId:int}/levels/{taxonomyTypeLevelId:int}")]
        public ActionResult DeletePolicyTypeLevelById(int taxonomyTypeId, int taxonomyTypeLevelId)
        {
            var form = new FormCollection();
            form.Add("Level", taxonomyTypeLevelId.ToString());
            form.Add("ID", taxonomyTypeId.ToString());
            return DeleteTaxonomyTypeLevel(form);
        }

        [HttpDelete, Route("DeleteTaxonomyTypeLevel")]
        public JsonResult DeleteTaxonomyTypeLevel(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id && i.Level == level);
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
        
        [HttpPut, ValidateInput(false), Route("EditTaxonomyTypeLevel")]
        public JsonResult EditTaxonomyTypeLevel(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");
                var model = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id && i.Level == level).SingleOrDefault();
                if (model == null) throw new NotFoundException("taxonomy type level");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<TaxonomyTypeLevel>(model);

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

        #region Custom API Service
                
        public JsonResult CustomAPIService_AddFields()
        {
            if(!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "MaxAge", Name = "Cache Max-Age (seconds)", FieldDescription = "", FieldType = DataType.Number.ToString(), Validations = checkAndAddValidation("Number", "MaxAge", true, "(3[2-8][0-9]{2}|39[0-8][0-9]|399[0-9]|[4-9][0-9]{3}|[1-7][0-9]{4}|8[0-3][0-9]{3}|84000)",null,null, "Please enter a cache max-age value between 3,200-84,000 seconds.  ") });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = FieldInfo.Description_Name, FieldDescription = "", FieldType = DataType.Html.ToString() });            

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIService_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                        
            var list = new List<EditableField>();
            var a = Company.ApiServices.Where(x => x.ID == id).FirstOrDefault();

            if(a == null) return jsonException("Cannot find the specified service to edit", HttpStatusCode.NotFound);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), Value = a.Name });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.UriPrefix, Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "MaxAge", Name = "Cache Max-Age (seconds)", FieldDescription = "", FieldType = DataType.Number.ToString(), Value = a.MaximumCacheAge.ToString(), Validations = checkAndAddValidation("Number", "MaxAge", true, "(3[2-8][0-9]{2}|39[0-8][0-9]|399[0-9]|[4-9][0-9]{3}|[1-7][0-9]{4}|8[0-3][0-9]{3}|84000)", null,null, "Please enter a cache max-age value between 3,200-84,000 seconds.  ") });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = FieldInfo.Description_Name, FieldDescription = "", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddService")]
        public JsonResult AddService(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("service");

                var name = parseTextField(form, "Name");
                var prefix = parseTextField(form, "URIPrefix");

                if(string.IsNullOrEmpty(name))
                    return jsonException("API Service Name is null", HttpStatusCode.NotFound);

                if (string.IsNullOrEmpty(prefix))
                    return jsonException("API Service Prefix is null", HttpStatusCode.NotFound);

                var service = new ApiService
                {
                    Name = name,
                    Description = parseTextField(form, "Description"),
                    UriPrefix = prefix,
                    MaximumCacheAge = parseIntField(form, "MaxAge")
                };

                Company.Add(service);

                return jsonSuccess("Service successfully created.", service.ID.ToString(), "add", HttpStatusCode.Created);
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


        [HttpPut, ValidateInput(false), Route("EditService")]
        public JsonResult EditService(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("service");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiService>(id);
                if (model == null) throw new NotFoundException("api service");

                model.Name = parseTextField(form, "Name");
                model.UriPrefix = parseTextField(form, "URIPrefix");
                model.Description = parseTextField(form, "Description");
                model.MaximumCacheAge = parseIntField(form, "MaxAge");

                Company.Update(model);

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

        #region Custom API Service Namespaces
        public JsonResult CustomAPINamespace_AddFields(int serviceId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ServiceID", FieldType = DataType.Hidden.ToString(), Value = serviceId.ToString() });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Element Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Namespace", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Namespace", Name = "Namespace", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Namespace", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPINamespace_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.ApiNamespaces.Where(x => x.ID == id).FirstOrDefault();

            if (a == null) return jsonException("Cannot find the specified service to edit", HttpStatusCode.NotFound);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Element Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), Value = a.Node });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Namespace", Name = "Namespace", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Namespace", true, "", 1, 250), Value = a.Namespace });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddNamespace")]
        public JsonResult AddNamespace(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("service");
                
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var name = parseTextField(form, "Name");
                var ns = parseTextField(form, "Namespace");
                var serviceId = parseIntField(form, "ServiceID");

                if (string.IsNullOrEmpty(name))
                    return jsonException("API Namespace Name is null", HttpStatusCode.NotFound);

                if (string.IsNullOrEmpty(ns))
                    return jsonException("API Namespace is null", HttpStatusCode.NotFound);

                var apiNamespace = new ApiNamespace
                {
                    ServiceID = serviceId,
                    Node = name,
                    Namespace = ns
                };

                Company.Add(apiNamespace);


                return jsonSuccess("Namespace successfully created.", apiNamespace.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), Route("EditNamespace")]
        public JsonResult EditNamespace(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("service");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiNamespace>(id);
                if (model == null) throw new NotFoundException("api service");

                model.Node = parseTextField(form, "Name");
                model.Namespace = parseTextField(form, "Namespace");

                Company.Update(model);

                return jsonSuccess("Namespace successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        private JsonResult DeleteCustomAPINamespace(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var id = parseIntField(form, "ID");

                Company.Delete<ApiNamespace>(o => o.ID == id);

                return jsonSuccess("api namespace successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        #region Custom API Service Endpoint

        public JsonResult CustomAPIServiceEndpoint_AddFields(int serviceId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ServiceID", FieldType = DataType.Hidden.ToString(), Value = serviceId.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 1, Column = 3, Required = true, FieldName = "ItemNode", Name = "Item Element Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = "item", Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 50, "Must be between 1 and 50 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = FieldInfo.Description_Name, FieldDescription = "", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIServiceEndpoint_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.ApiEndpoints.Where(x => x.ID == id).FirstOrDefault();

            if (a == null) return jsonException("Cannot find the specified service endpoint to edit", HttpStatusCode.NotFound);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), Value = a.Name });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.UriPrefix, Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 1, Column = 3, Required = true, FieldName = "ItemNode", Name = "Item Element Name", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.ItemNode ?? "item", Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 50, "Must be between 1 and 50 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = FieldInfo.Description_Name, FieldDescription = "", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpoint")]
        public JsonResult AddServiceEndpoint(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("endpoint");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var serviceId = parseIntField(form, "ServiceID");
                var name = parseTextField(form, "Name");
                var prefix = parseTextField(form, "URIPrefix");
                var itemNode = parseTextField(form, "ItemNode");

                if (string.IsNullOrEmpty(name))
                    return jsonException("API Service Endpoint Name is null", HttpStatusCode.NotFound);

                if (string.IsNullOrEmpty(prefix))
                    return jsonException("API Service Endpoint Prefix is null", HttpStatusCode.NotFound);

                var endpoint = new ApiEndpoint
                {
                    Name = name,
                    Description = parseTextField(form, "Description"),
                    UriPrefix = prefix,
                    ServiceID = serviceId,
                    ItemNode = itemNode
                };

                Company.Add(endpoint);

                return jsonSuccess("Service endpoint successfully created.", endpoint.ID.ToString(), "add", HttpStatusCode.Created);
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


        [HttpPut, ValidateInput(false), Route("EditServiceEndpoint")]
        public JsonResult EditServiceEndpoint(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("endpoint");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEndpoint>(id);
                if (model == null) throw new NotFoundException("api service endpoint");

                model.Name = parseTextField(form, "Name");
                model.UriPrefix = parseTextField(form, "URIPrefix");
                model.Description = parseTextField(form, "Description");
                model.ItemNode = parseTextField(form, "ItemNode");

                Company.Update(model);

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

        #region Custom API Service Endpoint Version

        public JsonResult CustomAPIServiceEndpointVersion_AddFields(int endpointId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "EndpointID", FieldType = DataType.Hidden.ToString(), Value = endpointId.ToString() });            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MajorVersion", Name ="Major Version", FieldDescription = "", FieldType = DataType.Number.ToString() });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "MinorVersion", Name = "Minor Version", FieldDescription = "", FieldType = DataType.Number.ToString() });
            list.Add(new EditableField
            {
                Row = 3,
                Column = 1,
                Required = true,
                FieldName = "AssetType",
                Name = "Asset Type",
                FieldDescription = "",
                FieldType = DataType.Lookup.ToString(),
                Items = Company.AssetTypes.ToList()
                    .Select(i => new SelectListItem
                    {
                        Value = i.ID.ToString(),
                        Text = $"{i.Object} - {i.Name}"
                    }).OrderBy(x => x.Text).ToList()
                .ToList()
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIServiceEndpointVersion_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.ApiEndpointVersions.Where(x => x.ID == id).FirstOrDefault();

            if (a == null) return jsonException("Cannot find the specified service endpoint version to edit", HttpStatusCode.NotFound);

            var ent = Company.ApiEntities.FirstOrDefault(x => x.EndpointVersionID == a.ID);
            if (ent == null) return jsonException("Cannot find the specified service endpoint version entity to edit", HttpStatusCode.NotFound);


            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.UriPrefix, Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MajorVersion", Name = "Major Version", FieldDescription = "", FieldType = DataType.Number.ToString(), Value = a.MajorVersion.ToString() });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "MinorVersion", Name = "Minor Version", FieldDescription = "", FieldType = DataType.Number.ToString(), Value = a.MinorVersion.ToString() });

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "AssetType", Name = "Asset Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(),
                    Items = Company.AssetTypes.ToList()
                    .Select(i => new SelectListItem
                    {
                        Value = i.ID.ToString(),
                        Text = $"{i.Object} - {i.Name}",
                        Selected = i.ID == ent.AssetTypeID
                    }).OrderBy(x=>x.Text).ToList()
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpointVersion")]
        public JsonResult AddServiceEndpointVersion(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("endpoint");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var endpointId = parseIntField(form, "EndpointID");                
                var prefix = parseTextField(form, "URIPrefix");
                var majorVersion = parseIntField(form, "MajorVersion");
                var minorVersion = parseIntField(form, "MinorVersion");
                var assetType = parseIntField(form, "AssetType");


                if (string.IsNullOrEmpty(prefix))
                    return jsonException("API Service Endpoint Version Prefix is null", HttpStatusCode.NotFound);

                var version = new ApiEndpointVersion
                {
                    MajorVersion = majorVersion,
                    MinorVersion = minorVersion,
                    UriPrefix = prefix,                    
                    EndpointID = endpointId
                };

                Company.Add(version);

                var entity = new ApiEntity
                {
                    AssetTypeID = assetType,
                    EndpointVersionID = version.ID,
                };

                Company.Add<ApiEntity>(entity);


                return jsonSuccess("Version successfully created.", version.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), Route("EditServiceEndpointVersion")]
        public JsonResult EditServiceEndpointVersion(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("version");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEndpointVersion>(id);
                if (model == null) throw new NotFoundException("api service version");
                                
                model.UriPrefix = parseTextField(form, "URIPrefix");
                model.MajorVersion = parseIntField(form, "MajorVersion");
                model.MinorVersion = parseIntField(form, "MinorVersion");

                Company.Update(model);

                var assetTypeID = parseIntField(form, "AssetType");

                var entity = Company.ApiEntities.FirstOrDefault(x => x.EndpointVersionID == model.ID);
                if (entity == null) throw new NotFoundException("api service version entity");

                entity.AssetTypeID = assetTypeID;

                Company.Update(entity);

                return jsonSuccess("Version successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Custom API Service Endpoint Version Uri

        public JsonResult CustomAPIVersionUri_AddFields(int versionId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var entity = Company.ApiEntities.First(x=>x.EndpointVersionID == versionId);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "EntityID", FieldType = DataType.Hidden.ToString(), Value = entity.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "UriType", Name = "Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items=
            new List<SelectListItem>{
                new SelectListItem{Text = "Singleton", Value = "2"},
                new SelectListItem{Text = "Collection", Value = "1"},
            }
            });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Format", Name = "Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Format", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") }); 

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIVersionUri_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.ApiEntityUris.Where(x => x.ID == id).FirstOrDefault();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "UriType", Name = "Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = new List<SelectListItem>()
            {
                new SelectListItem{Text = "Singleton", Value = "2", Selected = (a.UriType == ApiUriType.Singleton)},
                new SelectListItem{Text = "Collection", Value = "1", Selected = (a.UriType == ApiUriType.Collection)},
            }
            });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Format", Name = "Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.Format, Validations = checkAndAddValidation("Text", "Format", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpointVersionUri")]
        public JsonResult AddServiceEndpointVersionUri(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("uri");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var entityId = parseIntField(form, "EntityID");
                var format = parseTextField(form, "Format");
                var uriType = (ApiUriType)parseIntField(form, "UriType");

                if (string.IsNullOrEmpty(format))
                    return jsonException("API Service Endpoint Version URI format is null", HttpStatusCode.NotFound);

                var uri = new ApiEntityUri
                {
                    Format = format,
                    EntityID = entityId,
                    UriType = uriType
                };

                Company.Add(uri);
                
                return jsonSuccess("Uri successfully created.", uri.ID.ToString(), "add", HttpStatusCode.Created);
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


        [HttpPut, ValidateInput(false), Route("EditServiceEndpointVersionUri")]
        public JsonResult EditServiceEndpointVersionUri(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("version");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEntityUri>(id);
                if (model == null) throw new NotFoundException("api service version uri");

                model.Format = parseTextField(form, "Format");
                model.UriType = (ApiUriType)parseIntField(form, "UriType");                

                Company.Update(model);
                
                return jsonSuccess("Version successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Custom API Service Endpoint Version Field

        public JsonResult CustomAPIVersionField_AddFields(int versionId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var entity = Company.ApiEntities.First(x => x.EndpointVersionID == versionId);

            //get field types for this entity
            
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "EntityID", FieldType = DataType.Hidden.ToString(), Value = entity.ID.ToString() });
            list.Add(new EditableField
            {
                Row = 1,
                Column = 1,
                Required = true,
                FieldName = "FieldTypeID",
                Name = "Field",
                FieldDescription = "",
                FieldType = DataType.Lookup.ToString(),
                Items = Company.FieldTypes.Where(x=>x.AssetTypeID == entity.AssetTypeID).ToList()
                    .Select(i => new SelectListItem
                    {
                        Value = i.ID.ToString(),
                        Text = i.FriendlyName
                    }).OrderBy(x => x.Text).ToList()
                .ToList()
            });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "AllowSort", Name = "Allow Sort", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "AllowSelect", Name = "Allow Select", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "AllowFilter", Name = "Allow Filter", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, Required = false, FieldName = "JsonFieldNameOverride", Name = "Json Field Name Override", FieldDescription = "", FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 4, Column = 1, Required = false, FieldName = "XmlFieldNameOverride", Name = "Xml Field Name Override", FieldDescription = "", FieldType = DataType.Text.ToString() });
            

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIVersionField_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                        
            var list = new List<EditableField>();
            var a = Company.ApiEntityFieldTypes.Where(x => x.ID == id).FirstOrDefault();

            var entity = Company.ApiEntities.First(x => x.ID == a.EntityID);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            
            list.Add(new EditableField
            {
                Row = 1,
                Column = 1,
                Required = true,
                FieldName = "FieldTypeID",
                Name = "Field",
                FieldDescription = "",
                FieldType = DataType.Lookup.ToString(),
                Items = Company.FieldTypes.Where(x => x.AssetTypeID == entity.AssetTypeID).ToList()
                    .Select(i => new SelectListItem
                    {
                        Value = i.ID.ToString(),
                        Text = i.FriendlyName,
                        Selected = a.FieldTypeID == i.ID
                    }).OrderBy(x => x.Text).ToList()
                .ToList()
            });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "AllowSort", Name = "Allow Sort", FieldDescription = "", FieldType = DataType.Boolean.ToString(), Value = a.AllowSort.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "AllowSelect", Name = "Allow Select", FieldDescription = "", FieldType = DataType.Boolean.ToString(), Value = a.AllowSelect.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "AllowFilter", Name = "Allow Filter", FieldDescription = "", FieldType = DataType.Boolean.ToString(), Value = a.AllowFilter.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, Required = false, FieldName = "JsonFieldNameOverride", Name = "Json Field Name Override", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.JsonFieldNameOverride });
            list.Add(new EditableField { Row = 4, Column = 1, Required = false, FieldName = "XmlFieldNameOverride", Name = "Xml Field Name Override", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.XmlFieldNameOverride });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpointVersionField")]
        public JsonResult AddServiceEndpointVersionField(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("uri");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var entityId = parseIntField(form, "EntityID");
                var fieldTypeId = parseIntField(form, "FieldTypeID");
                var allowSort = parseBooleanField(form, "AllowSort");
                var allowSelect = parseBooleanField(form, "AllowSelect");
                var allowFilter = parseBooleanField(form, "AllowFilter");
                var jsonFieldNameOverride = parseTextField(form, "JsonFieldNameOverride");
                var xmlFieldNameOverride = parseTextField(form, "XmlFieldNameOverride");


                var field = new ApiEntityFieldType
                {
                    FieldTypeID = fieldTypeId,
                    EntityID = entityId,
                    AllowFilter = allowFilter,
                    AllowSelect = allowSelect,
                    AllowSort = allowSort
                };

                if (!string.IsNullOrWhiteSpace(jsonFieldNameOverride))
                    field.JsonFieldNameOverride = jsonFieldNameOverride;

                if (!string.IsNullOrWhiteSpace(xmlFieldNameOverride))
                    field.XmlFieldNameOverride = xmlFieldNameOverride;

                Company.Add(field);

                return jsonSuccess("Field successfully created.", field.EntityID.ToString(), "add", HttpStatusCode.Created);
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

        private JsonResult DeleteCustomAPIEndPoint(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var o = Company.GetById<ApiEndpoint>(id);

                Company.Delete(o);

                return jsonSuccess("end ponint successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        private JsonResult DeleteCustomAPIService(FormCollection form)
        {
            try {
                     var id = parseIntField(form, "ID");

                    if (!Company.CurrentResourceIsAdmin)
                        return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                    var o = Company.GetById<ApiService>(id);
                    
                    Company.Delete(o);

                    return jsonSuccess("api service successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex) {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        private JsonResult DeleteCustomAPIVersion(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var o = Company.GetById<ApiEndpointVersion>(id);

                Company.Delete(o);

                return jsonSuccess("api endpoint version successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        private JsonResult DeleteCustomAPIUri(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var o = Company.GetById<ApiEntityUri>(id);

                Company.Delete(o);

                return jsonSuccess("api endpoint uri successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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
        public JsonResult DeleteApiField(FormCollection form)
        {            
            try
            {
                var id = parseIntField(form, "ID");
                var o = Company.GetById<ApiEntityFieldType>(id);
                if (o == null) throw new NotFoundException("api field");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var multiSelectRecords = Company.ApiEntityFieldTypeMultiSelectFields.Where(i => i.EntityFieldTypeID == id);

                if (multiSelectRecords.Any())
                    Company.ApiEntityFieldTypeMultiSelectFields.RemoveRange(multiSelectRecords);
                
                Company.Delete(o);
                
                return jsonSuccess("api field successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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
                
        public JsonResult EditApiField(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("field");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEntityFieldType>(id);
                if (model == null) throw new NotFoundException("api field");

                model.FieldTypeID = parseIntField(form, "FieldTypeID");
                model.AllowFilter = parseBooleanField(form, "AllowFilter");
                model.AllowSelect = parseBooleanField(form, "AllowSelect");
                model.AllowSort = parseBooleanField(form, "AllowSort");

                var jsonFieldNameOverride = parseTextField(form, "JsonFieldNameOverride");
                var xmlFieldNameOverride = parseTextField(form, "XmlFieldNameOverride");

                if (string.IsNullOrWhiteSpace(jsonFieldNameOverride))
                    model.JsonFieldNameOverride = null;
                else
                    model.JsonFieldNameOverride = jsonFieldNameOverride;

                if (string.IsNullOrWhiteSpace(xmlFieldNameOverride))
                    model.XmlFieldNameOverride = null;
                else
                    model.XmlFieldNameOverride = xmlFieldNameOverride;

                Company.Update(model);
                                
                return jsonSuccess("Api Field successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        #region UpdateDisplayValues

        [HttpPost, AjaxValidateAntiForgeryToken, Route("rebuildDisplayValues")]
        public JsonResult RebuildDisplayValues(string objectType, object[] param)
        {
            if(!Company.CurrentResourceIsAdmin) return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            Company.RebuildDisplayValuesRequest();

            return jsonSuccess("request submitted.", "", "add", HttpStatusCode.Created);
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

            var types = Company.AssetTypes.Where(f => f.Class == AssetTypeClass.Glossary).Select(i => new SelectListItem { Text = i.Name, Value = i.uid.ToString(), Selected = template.AssetTypeID == i.ID }).OrderBy(x=>x.Text).ToList();
            
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "AssetTypeUID", Name = "Asset Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = types });

            list.Add(new EditableField { Row = 5, Column = 1, Required = true, FieldName = "IncludeUrl", Name = "Include Glossary Url", FieldDescription = "", FieldType = DataType.Boolean.ToString() , Value = template.IncludeUrl.ToString()});
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

            var types = Company.AssetTypes.Where(f => f.Class == AssetTypeClass.Glossary).Select(i => new SelectListItem { Text = i.Name, Value = i.uid.ToString() }).OrderBy(x=>x.Text).ToList();
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "AssetTypeUID", Name = "Asset Type", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = types });

            list.Add(new EditableField { Row = 5, Column = 1, Required = true, FieldName = "IncludeUrl", Name = "Include Glossary Url", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "IncludeParent", Name = "Include Parent Name", FieldDescription = "", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 7, Column = 1, Required = false, FieldName = "UsageNotes", Name = "Usage Notes", FieldDescription = "", FieldType = DataType.Text.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}