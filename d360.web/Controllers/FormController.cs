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
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace d360.web.Controllers
{
    [ValidateHttpAntiForgeryToken]
    [RoutePrefix("form"), Authorize, AiHandleError, NonNullableParameters]
    public class FormController : BaseController
    {
        #region DI

        ISecurityContextProvider SecProvider;
        IStorageProvider Storage;

        public FormController(CommunityContext community, CompanyContext company, ISecurityContextProvider secProvider, IStorageProvider storage)
            : base(community, company)
        {
            SecProvider = secProvider;
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
            var t = "";

            if (style != null)
            {
                b = style.IconBackColor;
                f = style.IconForeColor;
                t = style.IconText;
            }

            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "IconBackColor", Name = "Background Color", FieldDescription = "The icon's background color", FieldType = DataType.Color.ToString(), Value = b });
            list.Add(new EditableField { Row = row, Column = 2, Required = true, FieldName = "IconForeColor", Name = "Text Color", FieldDescription = "The icon's text color", FieldType = DataType.Color.ToString(), Value = f });
        }

        void upsertObjectStyle(SystemObjects type, int id, string foreColor, string backColor, string objectName = "Tx")
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
                    ObjectType = type.ToString(),
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

        void deleteObjectStyle(SystemObjects type, int id)
        {
            var sType = type.ToString();
            Company.Delete<ObjectStyle>(i => i.ObjectType == sType && i.ObjectID == id);

        }

        List<EditableField> loadStatusField(List<EditableField> list, SystemObjects type, string value, int row, int column)
        {
            var f = new EditableField
            {
                Row = row,
                Column = column,
                FieldName = "Status",
                Name = "Status",
                FieldType = DataType.Lookup.ToString(),
                Value = value,
                Required = true
            };

            var statusList = new List<SelectListItem>();
            switch (type)
            {
                case SystemObjects.Artifact:
                    statusList.Add(new SelectListItem { Text = "Draft", Value = "Draft" });
                    statusList.Add(new SelectListItem { Text = "Under Review", Value = "Under Review" });
                    statusList.Add(new SelectListItem { Text = "Certified", Value = "Certified" });
                    statusList.Add(new SelectListItem { Text = "Archived", Value = "Archived" });
                    break;
                case SystemObjects.Policy:
                    statusList.Add(new SelectListItem { Text = "Draft", Value = "Draft" });
                    statusList.Add(new SelectListItem { Text = "Active", Value = "Active" });
                    statusList.Add(new SelectListItem { Text = "Retired", Value = "Retired" });
                    break;
            }
            f.Items.AddRange(statusList);

            list.Add(f);


            return list;
        }

        #endregion

        #region Json Message Handling

        JsonNetResult jsonNetException(Exception ex, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return new JsonNetResult { Data = new { type = "error", title = title, message = ex.GetFullExceptionData() }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        JsonResult jsonException(Exception ex, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            return Json(new { type = "error", title = title, message = ex.GetFullExceptionData() }, JsonRequestBehavior.AllowGet);
        }

        JsonResult jsonException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {            
            return Json(new { type = "error", title = title, message = message }, JsonRequestBehavior.AllowGet);
        }

        JsonNetResult jsonNetException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {         
            return new JsonNetResult
            {
                Data = new { type = "error", title = title, message = message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        JsonResult jsonSuccess(string message, string id, string action, HttpStatusCode statusCode, dynamic customdata = null)
        {
            Response.StatusCode = (int)statusCode;
            Response.StatusDescription = message.Replace("\n", "  ");
            return Json(new { type = "confirm", title = "Success!", action = action, message = message.Replace("\n", "  "), id = id, custom = customdata }, JsonRequestBehavior.AllowGet);
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

        [HttpPost, Route("dynamiceditor/new/{objectType}")]
        public JsonResult DynamicEditorAddFields(string objectType, object[] param)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "ATTRIBUTE":
                    return Attribute_AddFields((int)param[0], param[1].ToString(), (int)param[2], (int)param[3]);

            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpPost, Route("dynamiceditor/edit/{objectType}")]
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
                case "ARTIFACT":
                    return Artifact_EditFields(oid);
                case "ATTRIBUTE":
                    return Attribute_EditFields(oid);
                case "CONTRACT":
                    return Contract_EditFields(oid);
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
                case "MAPRULE":
                    return MapRule_EditFields(oid);
                case "MAPRULEITEM":
                    return MapRuleItem_EditFields(oid);
                case "ORGANIZATION":
                    return Organization_EditFields(oid);
                case "ORGANIZATIONDOMAIN":
                    return OrganizationDomain_EditFields(oid);
                case "ORGANIZATIONINVITATION":
                    return OrganizationInvitation_EditFields(oid);
                case "POLICY":
                    return Policy_EditFields(oid);
                case "POLICYTYPE":
                    return PolicyType_EditFields(oid);
                case "POLICYTYPECLASS":
                    return PolicyTypeClass_EditFields(oid);
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
                case "SURVEYTYPE":
                    return SurveyType_EditFields(oid);
                case "TAXONOMY":
                    return Taxonomy_EditFields(oid);
                case "TAXONOMYTYPECLASS":
                    return TaxonomyTypeClass_EditFields(oid);
            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditor/new/{objectType}/{objectID?}/{parentID?}/{typeID?}")]
        public JsonResult DynamicEditorAddFields(string objectType, int? objectID, int? parentID, int? typeID)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "ARTIFACT":
                    return Artifact_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "ATTRIBUTEALLOCATION":
                    return AttributeTypeRelation_AddFields(parentID.GetValueOrDefault());
                case "CONTRACT":
                    return Contract_AddFields(objectID.HasValue ? objectID.Value : 0);
                case "FUSION":
                    return Fusion_AddFields(objectID.GetValueOrDefault());
                case "FUSIONATTRIBUTE":
                    return FusionAttribute_AddFields(objectID.GetValueOrDefault(), typeID.GetValueOrDefault());
                case "ISSUE":
                    return Issue_AddFields(objectID.GetValueOrDefault());
                case "ISSUETYPE":
                    return IssueType_AddFields();
                case "LOOKUPTYPE":
                    return Lookup_AddFields(objectID.GetValueOrDefault());
                case "MAP":
                    return Map_AddFields();
                case "MAPRULE":
                    return MapRule_AddFields();
                case "MAPRULEITEM":
                    return MapRuleItem_AddFields(objectID.GetValueOrDefault());
                case "ORGANIZATION":
                    return Organization_AddFields();
                case "ORGANIZATIONDOMAIN":
                    return OrganizationDomain_AddFields(objectID.Value);
                case "ORGANIZATIONINVITATION":
                    return OrganizationInvitation_AddFields(objectID.Value);
                case "POLICY":
                    return Policy_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "POLICYTYPE":
                    return PolicyType_AddFields();
                case "POLICYTYPECLASS":
                    return PolicyTypeClass_AddFields();
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
                case "SURVEYTYPE":
                    return SurveyType_AddFields();
                case "TAXONOMY":
                    return Taxonomy_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "TAXONOMYTYPECLASS":
                    return TaxonomyTypeClass_AddFields();

            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditorrel/new/{objectType}/{objectID}/{targetType}/{targetID}")]
        public JsonResult DynamicEditorAddRelationFields(string objectType, int objectID, SystemObjects targetType, int targetID)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "INTERSECTTYPE":
                    return Relationship_AddFields(objectID, targetType, targetID,true);                
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
                case "ARTIFACT":
                    return EditArtifact(form);
                case "ATTRIBUTE":
                    return EditAttribute(form);
                case "ATTRIBUTETYPE":
                    return EditAttributeType(form);
                case "CONTRACT":
                    return PutContract(form);
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
                case "MAPRULE":
                    return EditMapRule(form);
                case "MAPRULEITEM":
                    return EditMapRuleItem(form);
                case "ORGANIZATION":
                    return PutOrganization(form);
                case "ORGANIZATIONDOMAIN":
                    return PutOrganizationDomain(form);
                case "ORGANIZATIONINVITATION":
                    return PutOrganizationInvitation(form);
                case "POLICY":
                    return EditPolicy(form);
                case "POLICYTYPE":
                    return EditPolicyType(form);
                case "POLICYTYPECLASS":
                    return EditPolicyTypeClass(form);
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
                case "SCORETYPE":
                    return EditScoreType(form);
                case "SCORETYPEMETRIC":
                    return EditScoreTypeMetric(form);
                case "SURVEYTYPE":
                    return EditSurveyType(form);
                case "TAXONOMY":
                    return EditTaxonomy(form);
                case "TAXONOMYTYPECLASS":
                    return EditTaxonomyTypeClass(form);
                case "TAXONOMYTYPELEVEL":
                    return EditTaxonomyTypeLevel(form);
            }

            throw new Exception("Invalid / unsupported edit type");
        }

        [HttpDelete, Route("dynamicedit/delete/{objectType}/{objectID:int}"), ValidateInput(false)]
        public JsonResult DynamicDelete(string objectType, int objectID)
        {            
            FormCollection form = new FormCollection();
            form.Add("ID", objectID.ToString());

            switch ((objectType ?? "").ToUpper())
            {
                case "ARTIFACT":
                    return DeleteArtifact(form);
                case "ATTRIBUTETYPE":
                    return DeleteAttributeType(form);
                case "CONTRACT":
                    return DeleteContract(objectID);
                case "CUSTOMSYNONYM":
                    return DeleteCustomSynonym(form);
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
                case "MAPRULE":
                    return DeleteMapRule(form);
                case "MAPRULEITEM":
                    return DeleteMapRuleItem(form);
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
                    return DeleteReport(form);
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
                case "POLICYTYPE":
                    return DeletePolicyType(form);
                case "POLICYTYPECLASS":
                    return DeletePolicyTypeClass(form);                
                case "RULEIMPLEMENTATION":
                    return DeleteRuleImplementation(form);
                case "SCORETYPE":
                    return DeleteScoreType(form);
                case "SCORETYPEMETRIC":
                    return DeleteScoreTypeMetric(form);
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
                case "TAXONOMYTYPECLASS":
                    return DeleteTaxonomyTypeClass(form);                
                case "TEMPLATE":
                    return DeleteTooltipTemplate(form);
            }

            throw new Exception("Invalid / unsupported edit type");
        }

        [HttpPost, Route("dynamicedit/create/{objectType}"), ValidateInput(false)]
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
                case "ARTIFACT":
                    return AddArtifact(form);
                case "ATTRIBUTE":
                    return AddAttribute(form);
                case "ATTRIBUTETYPE":
                    return AddAttributeType(form);
                case "CONTRACT":
                    return PostContract(form);
                case "CUSTOMSYNONYM":
                    return AddCustomSynonym(form);
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
                case "LOOKUP":
                    return AddLookup(form);
                case "MAP":
                    return AddMap(form);
                case "MAPRULE":
                    return AddMapRule(form);
                case "MAPRULEITEM":
                    return AddMapRuleItem(form);
                case "ORGANIZATION":
                    return PostOrganization(form);
                case "ORGANIZATIONDOMAIN":
                    return PostOrganizationDomain(form);
                case "ORGANIZATIONINVITATION":
                    return PostOrganizationInvitation(form);
                case "POLICY":
                    return AddPolicy(form);
                case "POLICYTYPE":
                    return AddPolicyType(form);
                case "POLICYTYPECLASS":
                    return AddPolicyTypeClass(form);
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
                case "SCORETYPE":
                    return AddScoreType(form);
                case "SCORETYPEMETRIC":
                    return AddScoreTypeMetric(form);
                case "RULE":
                    return AddRule(form);                
                case "SURVEYTYPE":
                    return AddSurveyType(form);
                case "TAXONOMY":
                    return AddTaxonomy(form);
                case "TAXONOMYTYPECLASS":
                    return AddTaxonomyTypeClass(form);
                case "TAXONOMYTYPELEVEL":
                    return AddTaxonomyTypeLevel(form);
            }

            throw new Exception("Invalid / unsupported create type");
        }

        #endregion

        #region Artifact

        #region Field Generation

        /// <param name="at">ArtifactTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("Artifact_AddFields"), NonNullableParameters]
        public JsonResult Artifact_AddFields(int at, int p)
        {
            if (!Company.HasPermission(SystemObjects.ArtifactType, at, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var type = Company.GetById<ArtifactType>(at, i => i.Parent);
          
            list.Add(new EditableField { FieldName = "ArtifactTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });

            var row = 1;

            if (p == 0 && type.ParentID.HasValue)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                var parents = Company.Filter<Artifact>(i => i.ArtifactTypeID == type.ParentID).OrderBy(i => i.TextPath).Select(i => new SelectListItem { Text = i.TextPath, Value = i.ID.ToString(), Selected = false }).ToList();
                list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "ParentID", FieldType = DataType.Lookup.ToString(), Items = parents, Name = $"Parent {pluralize.Singularize(type.Parent.Name)}" });
                pluralize = null;
                row++;
            }
            else
            {
                list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });
            }

            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = $"/form/Aritfact_SimilarItems?typeID={at}&query=" });

            var parentTaxonomy = Company.GetById<Artifact>(p);
            int parentTaxonomyId = 0;
            if (parentTaxonomy != null)
                parentTaxonomyId = parentTaxonomy.TaxonomyTypeID;

            list.Add(new EditableField { Row = row, Column = 2,
                Required = true,
                FieldName = "TaxonomyTypeID",
                Name = Resources.FieldInfo.TaxonomyType_Name,
                ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID",
                FieldDescription = Resources.FieldInfo.TaxonomyType_Description,
                FieldType = DataType.Lookup.ToString(),
                Items = Company.Table<TaxonomyType>().OrderBy(x=>x.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList(),
                Value = parentTaxonomyId == 0 ? string.Empty : parentTaxonomyId.ToString()
            });
            row++;

            list.Add(new EditableField { Row = row, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            
            row++;
            list = loadStatusField(list, SystemObjects.Artifact, null, row, 1);
            
            row++;
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ArtifactType, at).ToList(), row + 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ArtifactID</param>
        [Route("Artifact_DeleteFields"), NonNullableParameters]
        public JsonResult Artifact_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Delete, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ArtifactID</param>
        [Route("Artifact_EditFields"), NonNullableParameters]
        public JsonResult Artifact_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Update, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Artifact>(id);
            
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            var type = Company.GetById<ArtifactType>(a.ArtifactTypeID, i => i.Parent);

            if (type.ParentID.HasValue)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);                
                var parents = Company.Filter<Artifact>(i => i.ArtifactTypeID == type.ParentID).OrderBy(i => i.TextPath).ToList().Select(i => new SelectListItem { Text = i.TextPath, Value = i.ID.ToString() }).ToList();
                list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentID", Name = $"Parent {pluralize.Singularize(type.Parent.Name)}", FieldType = DataType.Lookup.ToString(), Value = (a.ParentID.HasValue ? a.ParentID.ToString() : ""), Items = parents });                
            }

            bool isPromoted = false;

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, ReadOnly = isPromoted, FieldName = "Name", Name = "Name", FieldDescription = ((isPromoted) ? "Artifact promoted via Fusion.  No changes allowed to the Name." : ""), FieldType = DataType.Text.ToString(), Value = Server.HtmlDecode(a.Name), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "TaxonomyTypeID", Name = Resources.FieldInfo.TaxonomyType_Name, ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID", FieldDescription = Resources.FieldInfo.TaxonomyType_Description, FieldType = DataType.Lookup.ToString(), Value = a.TaxonomyTypeID.ToString(), Items = Company.Table<TaxonomyType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });

            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            list = loadStatusField(list, SystemObjects.Artifact, a.Status, 4, 1);

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ArtifactType, a.ArtifactTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Artifact, id).ToList(), 5, true);

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <param name="id">ID</param>
        [Route("Artifact_RaiseIssue"), NonNullableParameters]
        public JsonResult Artifact_RaiseIssue(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Issue", Name = "Issue", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }
                
        [HttpGet, Route("Aritfact_SimilarItems"), NonNullableParameters]
        public JsonNetResult Aritfact_SimilarItems(int typeID, string query)
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

        [Route("AddArtifact"), ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false)]
        public JsonResult AddArtifact(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int typeID = parseIntField(form, "ArtifactTypeID");
                var type = Company.GetById<ArtifactType>(typeID);

                if (!Company.HasPermission(SystemObjects.ArtifactType, typeID, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (type == null) throw new NotFoundException("artifact type");
                                
                int taxonomyTypeID = parseIntField(form, "TaxonomyTypeID");

                var model = new Artifact();
                // Static fields
                model.ArtifactTypeID = typeID;
                model.TaxonomyTypeID = taxonomyTypeID;
                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.Status = form["Status"];

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    model.ParentID = parseIntField(form, "ParentID");
                    if (model.ParentID == 0) model.ParentID = null;
                }

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, model.ID, Company.GetFieldTypesByObject(SystemObjects.ArtifactType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate<Artifact>(model, fields);

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
                var model = Company.GetById<Artifact>(id);
                if (model == null) throw new NotFoundException("artifact");

                if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Delete, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

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

                if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var model = Company.GetById<Artifact>(id);

                if (model == null) throw new NotFoundException("artifact");

                var sType = SystemObjects.Artifact.ToString();
                bool isPromoted = false;// Company.Filter<FusionAttributePromotion>(i => i.ObjectType == sType && i.ObjectID == id).Any();
                                
                // Static fields
                if (!isPromoted) model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.TaxonomyTypeID = parseIntField(form, "TaxonomyTypeID");
                model.Status = form["Status"];

                //model.TaxonomyTypeID = string.IsNullOrEmpty(form["TaxonomyTypeID"]) ? new Nullable<int>() : parseIntField(form, "TaxonomyTypeID");
                model.ParentID = parseIntField(form, "ParentID");
                if (model.ParentID == 0) model.ParentID = null;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, model.ID, Company.GetFieldTypesByObject(SystemObjects.ArtifactType, model.ArtifactTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<Artifact>(model, fields);


                #region Add Comment

                try
                {
                    var comment = new Comment
                    {
                        Body = $"I updated the definition.",
                        CreatingResourceID = model.UpdatedBy.HasValue ? model.UpdatedBy.Value : Company.CurrentResourceID,
                        OwnerObjectType = "Artifact",
                        OwnerObjectID = model.ID,
                        CommentTypeID = CommentType.Governance,
                        DateCreated = DateTime.UtcNow,
                        Relations = new List<CommentRelation>()
                    };
                    comment.Relations.Add(new CommentRelation { ObjectType = "Artifact", ObjectID = model.ID, Date = DateTime.UtcNow });
                    comment.Relations.Add(new CommentRelation { ObjectType = "Resource", ObjectID = Company.CurrentResourceID, Date = DateTime.UtcNow });
                    Company.Add<Comment>(comment);
                }
                catch (Exception ex)
                {
                }

                #endregion

                #region Create Certify Workflow

                try
                {
                    var certificationWorkflowEnabled = Company.WorkflowEventRegistrations.Where(x => x.Object == "ArtifactType" && x.ObjectID == model.ArtifactTypeID && x.Type.PublishedVersionID != null && x.Type.State == State.Active).Any();

                    if (model.Status == "Certified" && certificationWorkflowEnabled)
                    {
                        //check for any outstanding certification workflows for this item
                        var sql = @"select
                                count(1)
                            from
                                workflow.eventregistration we
                                inner join workflow.type wt on we.typeid = wt.id
                                inner join workflow.version wv on wt.id = wv.typeid
                                inner join workflow.item wi on wi.versionid = wv.id and(wi.[object] = 'Artifact' and wi.objectid = @id)
                            where
                                we.changetype = 8 and wi.completedOn is null";

                        var count = Company.Query<int>(sql, new { id = id }).FirstOrDefault();

                        if (count == 0)
                        {
                            Company.RequestObjectCertification(SystemObjects.Artifact, model.ID, SystemObjects.ArtifactType, model.ArtifactTypeID);
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                #endregion

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

        [ValidateHttpAntiForgeryToken, HttpPost, Route("RequestCertification")]
        public JsonResult RequestCertification(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int id = parseIntField(form, "ID");
                var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);

                if (artifact == null) throw new NotFoundException("artifact");
                if (artifact.Status != "Draft") throw new ConflictException("Certification Not Allowed", "You may not request a certification on this item as it is not in Draft status.");

                //check for any outstanding certification workflows for this item
                var sql = @"select
                                count(1)
                            from
                                workflow.eventregistration we
                                inner join workflow.type wt on we.typeid = wt.id
                                inner join workflow.version wv on wt.id = wv.typeid
                                inner join workflow.item wi on wi.versionid = wv.id and(wi.[object] = 'Artifact' and wi.objectid = @id)
                            where
                                we.changetype = 8 and wi.completedOn is null";

                var count = Company.Query<int>(sql, new { id = id }).FirstOrDefault();

                if(count > 0)
                {
                    throw new ConflictException("Certification Not Allowed", "There is already a certification request in process for this item.");
                }

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

        #region Field Generation

        /// <param name="id">ArtifactID</param>
        [Route("ArtifactType_DeleteFields"), NonNullableParameters]
        public JsonResult ArtifactType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpGet, ActionName("ArtifactType"), Route("ArtifactType")]
        public JsonNetResult GetArtifactType(int? id = null, int? parentID = null)
        {
            try
            {
                var model = new ArtifactTypeEditorModel();

                if (parentID == null)
                {
                    var at = Company.GetById<ArtifactType>((int)id);
                    //if (at == null) return HttpNotFound();
                    var style = Company.GetObjectStyle(SystemObjects.ArtifactType, (int)id);

                    model = new ArtifactTypeEditorModel
                    {
                        FormName = Resources.FormInfo.Edit_ArtifactType_Title,
                        FormDescription = Resources.FormInfo.Edit_ArtifactType_Directions,
                        FormUri = "/form/EditArtifactType",
                        FormMethod = "PUT",
                        ArtifactType = at,
                        IconBackColor = ((style != null) ? style.IconBackColor : "#000"),
                        IconForeColor = ((style != null) ? style.IconForeColor : "#FFF")
                    };
                } 
                else
                {
                    model = new ArtifactTypeEditorModel
                    {
                        FormName = Resources.FormInfo.Add_ArtifactType_Title,
                        FormDescription = Resources.FormInfo.Add_ArtifactType_Directions,
                        FormUri = "/form/AddArtifactType",
                        FormMethod = "POST",
                        ArtifactType = new ArtifactType { ParentID = parentID, CanOwnFusion = false },
                        IconBackColor = "#000",
                        IconForeColor = "#FFF"
                    };
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), ActionName("ArtifactType"), Route("ArtifactType")]
        public JsonResult PostArtifactType(ArtifactTypeEditorModel model)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ArtifactType, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new ArtifactType
                {
                    Name = model.ArtifactType.Name,
                    Description = model.ArtifactType.Description,
                    CanOwnFusion = model.ArtifactType.CanOwnFusion
                };

                if (model.ArtifactType.ParentID != null)
                {
                    a.ParentID = model.ArtifactType.ParentID;
                    if (a.ParentID == 0) a.ParentID = null;
                }

                Company.Add(a);

                upsertObjectStyle(SystemObjects.ArtifactType, a.ID, model.IconForeColor, model.IconBackColor, a.Name);

                dynamic custom = new
                {
                    ParentID = a.ParentID,
                    Name = a.Name,
                    action = "add"
                };

                

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [ValidateHttpAntiForgeryToken, HttpPut, ActionName("ArtifactType"), Route("ArtifactType")]
        public JsonResult PutArtifactType(ArtifactTypeEditorModel model)
        {
            try
            {
                var id = model.ArtifactType.ID;
                var existing = Company.GetById<ArtifactType>(id);
                if (existing == null) throw new NotFoundException("artifact type");

                if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                existing.Name = model.ArtifactType.Name;
                existing.Description = model.ArtifactType.Description;
                existing.CanOwnFusion = model.ArtifactType.CanOwnFusion;

                Company.Update(existing);

                upsertObjectStyle(SystemObjects.ArtifactType, existing.ID, model.IconForeColor, model.IconBackColor, existing.Name);

                dynamic custom = new
                {
                    ParentID = existing.ParentID,
                    Name = existing.Name,
                    action = "edit"                    
                };

                return jsonSuccess(existing.Name + " successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [ValidateHttpAntiForgeryToken, HttpDelete, ActionName("ArtifactType"), Route("ArtifactType"), NonNullableParameters]
        public JsonResult DeleteArtifactType(int id)
        {
            try
            {               
                var model = Company.GetById<ArtifactType>(id);
                if (model == null) throw new NotFoundException("artifact type");

                if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (model.ParentID.HasValue) id = model.ParentID.Value;

                Company.Delete(model);
                deleteObjectStyle(SystemObjects.ArtifactType, id);

                dynamic custom = new
                {
                    ParentID = model.ParentID,
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
            var type = Company.GetById<AttributeType>(at);

            list.Add(new EditableField { FieldName = "AttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });
            list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = ot });
            list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = oid.ToString() });
            list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.AttributeType, at).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeID</param>
        [Route("Attribute_DeleteFields"), NonNullableParameters]
        public JsonResult Attribute_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeID</param>
        [Route("Attribute_EditFields"), NonNullableParameters]
        public JsonResult Attribute_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<d360.core.entities.Attribute>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list = loadDynamicFields(list,
                Company.GetFieldTypesByObject(SystemObjects.AttributeType, a.AttributeTypeID).ToList(),
                Company.GetFieldRelationsByObject(SystemObjects.Attribute, id).ToList(),
                1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddAttribute")]
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

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    a.ParentID = parseIntField(form, "ParentID");
                    if (a.ParentID == 0) a.ParentID = null;
                }

                // Dynamic fields
                var loader = new FieldLoader();
                var fields = loader.GetFormDynamicFieldValues(SystemObjects.Attribute, a.ID, Company.GetFieldTypesByObject(SystemObjects.AttributeType, typeID).ToList(), form, Server);

                Company.SaveOrUpdate<d360.core.entities.Attribute>(a, fields);

                dynamic custom = new
                {
                    AttributeTypeID = typeID,
                    ObjectID = a.ObjectID,
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
                Company.Delete<core.entities.Attribute>(i => i.ID == id);

                return jsonSuccess(Resources.FormInfo.Delete_Attribute_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Attribute, model.ID, Company.GetFieldTypesByObject(SystemObjects.AttributeType, model.AttributeTypeID).ToList(), form, Server, false);

                Company.SaveOrUpdate<core.entities.Attribute>(model, fields);

                dynamic custom = new
                {
                    AttributeTypeID = model.AttributeTypeID,
                    ObjectID = model.ObjectID,
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

        #region Field Generation

        /// <param name="id">AttributeTypeID</param>
        [Route("AttributeType_DeleteFields"), NonNullableParameters]
        public JsonResult AttributeType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.AttributeType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<AttributeType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddAttributeType")]
        public JsonResult AddAttributeType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.AttributeType, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_AttributeType);

                var a = new AttributeType
                {
                    Name = parseTextField(form, "Name"),
                    ShowNameInTree = parseBooleanField(form, "ShowNameInTree"),
                    Description = parseTextField(form, "Description"),
                    TextFormatString = parseTextField(form, "TextFormatString")
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

                Company.SaveOrUpdate<AttributeType>(a);

                return jsonSuccess(Resources.FormInfo.Add_AttributeType_Confirmation, a.ID.ToString(), "add", HttpStatusCode.Created, new { ParentID = a.ParentID, Name = a.Name });
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
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_AttributeType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<AttributeType>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_AttributeType);

                if (!Company.HasPermission(SystemObjects.AttributeType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                                
                Company.Delete("AttributeType", id);//Company.Delete<AttributeType>(model);

                return jsonSuccess(Resources.FormInfo.Delete_AttributeType_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_AttributeType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<AttributeType>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_AttributeType);

                if (!Company.HasPermission(SystemObjects.AttributeType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.ShowNameInTree = parseBooleanField(form, "ShowNameInTree");
                model.Description = parseTextField(form, "Description");
                model.TextFormatString = parseTextField(form, "TextFormatString");

                if (!model.ParentID.HasValue)
                {
                    if (!string.IsNullOrEmpty(form["AttributeTypeCategoryID"]))
                    {
                        model.AttributeTypeCategoryID = parseIntField(form, "AttributeTypeCategoryID");
                        if (model.AttributeTypeCategoryID == 0) model.AttributeTypeCategoryID = null;
                    }
                }

                Company.SaveOrUpdate<AttributeType>(model);

                return jsonSuccess(Resources.FormInfo.Edit_AttributeType_Confirmation, id.ToString(), "edit", HttpStatusCode.OK, new { ParentID = model.ParentID, Name = model.Name });
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

        #region AttributeTypeCategory

        #region Field Generation

        /// <param name="p">ParentID</param>
        [Route("AttributeTypeCategory_AddFields")]
        public JsonResult AttributeTypeCategory_AddFields()
        {
            var o = new AttributeTypeCategory();
            if (!Company.HasPermission(SystemObjects.ArtifactType, 0, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = o.GetName(i => i.Name), FieldDescription = o.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = o.GetName(i => i.Description), FieldDescription = o.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeTypeCategoryID</param>
        [Route("AttributeTypeCategory_DeleteFields"), NonNullableParameters]
        public JsonResult AttributeTypeCategory_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeTypeCategoryID</param>
        [Route("AttributeTypeCategory_EditFields"), NonNullableParameters]
        public JsonResult AttributeTypeCategory_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<AttributeTypeCategory>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddAttributeTypeCategory")]
        public JsonResult AddAttributeTypeCategory(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.AttributeTypeCategory, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("attribute type category");

                var a = new AttributeTypeCategory
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description")
                };

                Company.Add<AttributeTypeCategory>(a);

                dynamic custom = new
                {
                    Name = a.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpDelete, Route("DeleteAttributeTypeCategory")]
        public JsonResult DeleteAttributeTypeCategory(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("attribute type category");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<AttributeTypeCategory>(id);
                if (model == null) throw new NotFoundException("attribute type category");

                if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    Name = model.Name,
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

        [HttpPut, ValidateInput(false), Route("EditAttributeTypeCategory")]
        public JsonResult EditAttributeTypeCategory(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("attribute type category");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<AttributeTypeCategory>(id);
                if (model == null) throw new NotFoundException("attribute type category");

                if (!Company.HasPermission(SystemObjects.AttributeTypeCategory, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<AttributeTypeCategory>(model);

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

        #endregion

        #region AttributeTypeRelation

        #region Field Generation

        /// <param name="at">AttributeTypeID</param>
        [Route("AttributeTypeRelation_AddFields"), NonNullableParameters]
        public JsonResult AttributeTypeRelation_AddFields(int at)
        {
            var list = new List<EditableField>();
            var type = Company.GetById<AttributeType>(at);

            var relation = new AttributeTypeRelation();


            list.Add(new EditableField { FieldName = "AttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });
            list.Add(new EditableField
            {
                Row = 1,
                Column = 1,
                FieldName = "ObjectTypeInfo",
                Name = "Type",
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

        /// <param name="at">AttributeTypeID</param>
        /// <param name="ot">ObjectType</param>
        /// <param name="oid">ObjectID</param>
        [Route("AttributeTypeRelation_DeleteFields"), NonNullableParameters]
        public JsonResult AttributeTypeRelation_DeleteFields(int at, string ot, int oid)
        {
            var list = new List<EditableField>();
            var sType = ot.ToString();
            var a = Company.Filter<AttributeTypeRelationDetail>(i => i.AttributeTypeID == at && i.ObjectID == oid && i.ObjectType == sType).SingleOrDefault();

            list.Add(new EditableField { FieldName = "AttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = a.AttributeTypeID.ToString() });
            list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = a.ObjectType });
            list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = a.ObjectID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="at">AttributeTypeID</param>
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddAttributeTypeRelation")]
        public JsonResult AddAttributeTypeRelation(FormCollection form)
        {
            try
            {
                if (form.HasKeys())
                {
                    int typeID = parseIntField(form, "AttributeTypeID");
                    var type = Company.GetById<AttributeType>(typeID);
                    if (type == null)
                    {
                        return jsonException("Invalid attribute type.", HttpStatusCode.BadRequest);
                    }

                    var value = form["ObjectTypeInfo"].Split('|');


                    Company.Add<AttributeTypeRelation>(new AttributeTypeRelation {
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

        #region Company Settings

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
            
            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPut, ValidateInput(false), Route("UpdateCompanySettings")]
        public JsonResult UpdateCompanySettings(CompanySettingsEditorModel formModel)
        {
            try
            {
                if (formModel == null) throw new NoFormDataException("company settings");

                // Permisisons validation.
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var settings = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

                #region icon

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
                        var iconEncoding = iconMatch.Groups["encoding"].Value;
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

                #region logo

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
                        var logoEncoding = logoMatch.Groups["encoding"].Value;
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

                #region social

                var socialSetting = settings.FirstOrDefault(i => i.SettingID == 1);
                if (socialSetting == null)
                {
                    socialSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 1, Value = formModel.DisableCommunityPosting.ToString().ToLower() };
                    Community.Add<CompanySetting>(socialSetting);
                }
                else
                {
                    socialSetting.Value = formModel.DisableCommunityPosting.ToString().ToLower();
                    Community.SaveChanges();
                }

                socialSetting = settings.FirstOrDefault(i => i.SettingID == 5);
                if (socialSetting == null)
                {
                    socialSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 5, Value = formModel.DisableIssuePosting.ToString().ToLower() };
                    Community.Add<CompanySetting>(socialSetting);
                }
                else
                {
                    socialSetting.Value = formModel.DisableIssuePosting.ToString().ToLower();
                    Community.SaveChanges();
                }

                #endregion

                #region global fields

                var subjectAreaSetting = settings.FirstOrDefault(i => i.SettingID == 7);
                if (subjectAreaSetting == null)
                {
                    if (!string.IsNullOrEmpty(formModel.ArtifactType_TaxonomyTypeID))
                    {
                        subjectAreaSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 7, Value = formModel.ArtifactType_TaxonomyTypeID };
                        Community.Add<CompanySetting>(subjectAreaSetting);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(formModel.ArtifactType_TaxonomyTypeID))
                    {
                        Community.Delete<CompanySetting>(subjectAreaSetting);
                    }
                    else
                    {
                        subjectAreaSetting.Value = formModel.ArtifactType_TaxonomyTypeID;
                        Community.SaveChanges();
                    }
                }

                var subjectAreaNodesSetting = settings.FirstOrDefault(i => i.SettingID == 8);
                if (subjectAreaNodesSetting == null)
                {
                    if (!string.IsNullOrEmpty(formModel.ArtifactType_TaxonomyTypeIDNodes))
                    {
                        subjectAreaNodesSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 8, Value = formModel.ArtifactType_TaxonomyTypeIDNodes };
                        Community.Add<CompanySetting>(subjectAreaNodesSetting);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(formModel.ArtifactType_TaxonomyTypeIDNodes))
                    {
                        Community.Delete<CompanySetting>(subjectAreaNodesSetting);
                    }
                    else
                    {
                        subjectAreaNodesSetting.Value = formModel.ArtifactType_TaxonomyTypeIDNodes;
                        Community.SaveChanges();
                    }
                }

                var issueManagamentSetting = settings.FirstOrDefault(i => i.SettingID == 17);
                if (issueManagamentSetting == null)
                {
                    issueManagamentSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 17, Value = formModel.DisableIssueManagement.ToString().ToLower() };
                    Community.Add<CompanySetting>(issueManagamentSetting);
                }
                else
                {
                    issueManagamentSetting.Value = formModel.DisableIssueManagement.ToString().ToLower();
                    Community.SaveChanges();
                }

                var shoppingCartSetting = settings.FirstOrDefault(i => i.SettingID == 20);
                if (shoppingCartSetting == null)
                {
                    shoppingCartSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 20, Value = formModel.EnableShoppingCart.ToString().ToLower() };
                    Community.Add<CompanySetting>(shoppingCartSetting);
                }
                else
                {
                    shoppingCartSetting.Value = formModel.EnableShoppingCart.ToString().ToLower();
                    Community.SaveChanges();
                }

                var defaultRouteSetting = settings.FirstOrDefault(i => i.SettingID == 22);
                if (defaultRouteSetting == null)
                {
                    defaultRouteSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 22, Value = (formModel.DefaultRoute??"").Trim() };
                    Community.Add<CompanySetting>(defaultRouteSetting);
                }
                else
                {
                    defaultRouteSetting.Value = formModel.DefaultRoute ?? "".Trim();
                    Community.SaveChanges();
                }

                var enableExactMatchSetting = settings.FirstOrDefault(i => i.SettingID == 23);
                if (enableExactMatchSetting == null)
                {
                    enableExactMatchSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 23, Value = formModel.EnableSearchExactMatch.ToString().ToLower() };
                    Community.Add<CompanySetting>(enableExactMatchSetting);
                }
                else
                {
                    enableExactMatchSetting.Value = formModel.EnableSearchExactMatch.ToString().ToLower();
                    Community.SaveChanges();
                }

                #endregion

                #region ip

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

                var searchSetting = settings.FirstOrDefault(i => i.SettingID == 13);
                if (searchSetting == null)
                {
                    searchSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 13, Value = formModel.DefaultSearchTypes.ToString() };
                    Community.Add<CompanySetting>(searchSetting);
                }
                else
                {
                    searchSetting.Value = (formModel.DefaultSearchTypes ?? "").ToString();
                    Community.SaveChanges();
                }

                #endregion

                #region Header Styles

                var headerBackgroundColorSetting = settings.SingleOrDefault(i => i.SettingID == 10);
                if (string.IsNullOrEmpty(formModel.HeaderBackgroundColor))
                {
                    if(headerBackgroundColorSetting != null)
                    {
                        Community.Delete<CompanySetting>(headerBackgroundColorSetting);
                    }                        
                }
                else
                {
                    if (headerBackgroundColorSetting == null)
                    {
                        searchSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 10, Value = formModel.HeaderBackgroundColor };
                        Community.Add<CompanySetting>(searchSetting);
                    }
                    else
                    {
                        headerBackgroundColorSetting.Value = formModel.HeaderBackgroundColor;
                        Community.SaveChanges();
                    }
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
                          ,COALESCE(a.Name,pc.Name,tc.Name,v.Name) as Name
                          ,v.Route
                          ,v.SortOrder
                          ,v.ObjectID
                          ,v.[Object]
                          ,v.Icon
                          ,v.Title
                      FROM [dbo].[SiteNav] v
		                    left join artifacttype a on a.id = v.objectID and v.Object = 'ArtifactType'
		                    left join policytypeclass pc on pc.id = v.objectID and v.Object = 'PolicyTypeClass'
		                    left join taxonomytypeclass tc on tc.id = v.objectid and v.object = 'TaxonomyTypeClass'
                            WHERE   v.ParentID = @parentId";

            var items = Company.Query<SiteNav>(sql, new { parentId = id });

            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("GetSiteNavFolderAvailableItems")]
        public JsonNetResult GetSiteNavFolderAvailableItems()
        {
            return null;
        }

        #endregion

        #region EmailTemplate

        #region Field Generation

        [Route("EmailTemplate_AddFields")]
        public JsonResult EmailTemplate_AddFields()
        {
            var list = new List<EditableField>();

            var names = Enum.GetNames(typeof(SystemObjects)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();
            names.Add(new SelectListItem { Text = "Global.Footer", Value = "Global.Footer" });
            names.Add(new SelectListItem { Text = "Global.Header", Value = "Global.Header" });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Lookup.ToString(), Items = names });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Action", Name = "Action", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Action", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TemplateSubject", Name = "Subject", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Subject", true, "", 1, 250) });
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "TemplateBody", Name = "Body", FieldType = DataType.Html.ToString(), Validations = checkAndAddValidation("Text", "Body", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">EmailTemplateID</param>
        [Route("EmailTemplate_DeleteFields"), NonNullableParameters]
        public JsonResult EmailTemplate_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<EmailTemplate>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">EmailTemplateID</param>
        [Route("EmailTemplate_EditFields"), NonNullableParameters]
        public JsonResult EmailTemplate_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<EmailTemplate>(id);

            var names = Enum.GetNames(typeof(SystemObjects)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();
            names.Add(new SelectListItem { Text = "Global.Footer", Value = "Global.Footer" });
            names.Add(new SelectListItem { Text = "Global.Header", Value = "Global.Header" });

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Lookup.ToString(), Items = names, Value = a.Name });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Action", Name = "Action", FieldType = DataType.Text.ToString(), Value = a.Action, Validations = checkAndAddValidation("Text", "Action", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TemplateSubject", Name = "Subject", FieldType = DataType.Text.ToString(), Value = a.TemplateSubject, Validations = checkAndAddValidation("Text", "Subject", true, "", 1, 250) });
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "TemplateBody", Name = "Body", FieldType = DataType.Html.ToString(), Value = a.TemplateBody, Validations = checkAndAddValidation("Text", "Body", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddEmailTemplate")]
        public JsonResult AddEmailTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("email template");

                var a = new EmailTemplate
                {
                    Action = parseTextField(form, "Action"),
                    Description = parseTextField(form, "Description"),
                    Name = parseTextField(form, "Name"),
                    TemplateBody = parseTextField(form, "TemplateBody"),
                    TemplateSubject = parseTextField(form, "TemplateSubject")
                };

                Company.Add<EmailTemplate>(a);

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

        [HttpDelete, Route("DeleteEmailTemplate")]
        public JsonResult DeleteEmailTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("email template");

                var id = parseIntField(form, "ID");
                Company.Delete<EmailTemplate>(i => i.ID == id);

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

        [HttpPut, ValidateInput(false), Route("EditEmailTemplate")]
        public JsonResult EditEmailTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("email template");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<EmailTemplate>(id);
                if (model == null) throw new NotFoundException("email template");

                model.Action = parseTextField(form, "Action");
                model.Description = parseTextField(form, "Description");
                model.Name = parseTextField(form, "Name");
                model.TemplateBody = parseTextField(form, "TemplateBody");
                model.TemplateSubject = parseTextField(form, "TemplateSubject");

                Company.Update<EmailTemplate>(model);

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
                    list = Company.Filter<ArtifactType>(i => i.ParentID == id)
                        .ToList()
                        .Select(i => new { value = $"0|ArtifactType|{i.ID}", title = i.Name })
                        .ToList();
                    break;
                case SystemObjects.FusionAttributeType:
                    list = Company.Filter<FusionAttributeType>(i => i.ParentID == id)
                        .ToList()
                        .Select(i => new { value = $"0|FusionAttributeType|{i.ID}", title = i.Name })
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

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list = Company.Filter<ArtifactType>(i => i.ID == id, i => i.Parent)
                        .ToList()
                        .Select(i => new { value = $"0|ArtifactType|{i.ParentID}", title = i.Parent?.Name })
                        .Where(i => i.title != null)
                        .ToList();
                    break;
                case SystemObjects.FusionAttributeType:
                    list = Company.Filter<FusionAttributeType>(i => i.ID == id, i => i.Parent)
                        .ToList()
                        .Select(i => new { value = $"0|FusionAttributeType|{i.ParentID}", title = i.Parent?.Name })
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
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
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
                list.Add("DateLastLoggedIn", 0);
            }
            else if (type == SystemObjects.FusionAttributeType)
            {
                list.Add("Name", 0);
                list.Add("TextPath", 0);
            }
            else if (type == SystemObjects.FusionQueryAttributeType)
            {
                list.Add("DisplayValue", 0);
            }
            else if (type == SystemObjects.RuleType)
            {
                list.Add("Name", 0);
                if (!list.ContainsKey("Description"))
                    list.Add("Description", 0);
                list.Add("Dimension", 0);
                list.Add("Threshold", 0);
            }
            else
            {
                list.Add("Name", 0);
                list.Add("TextPath", 0);
                if (!list.ContainsKey("Description"))
                    list.Add("Description", 0);

                if (type == SystemObjects.ArtifactType)
                    list.Add("SubjectArea", 0);
            }

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
                    ID = i.ID,//(i.Subject == sType && i.SubjectID == id) ? i.ObjectID : i.SubjectID,
                    Name = (i.Subject == sType && i.SubjectID == id) ? $"{i.ObjectName} ({i.PredicateName})" : $"{i.SubjectName} ({i.PredicateName})"
                }).Distinct().ToList();
            relatedTypeList.ForEach(r =>
            {
                list.Add($"Related Item.{r.Name}", r.ID);
            });


            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}|{i.Key}" }),
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
                    list.Add("Name", "Name");
                    list.Add("Status", "Status");
                    list.Add("Description", "Description");
                    list.Add("TextPath", "TextPath");
                    break;
                case SystemObjects.ReferenceItem:
                case SystemObjects.ReferenceItemType:
                    list.Add("Code", "Code");
                    break;
                case SystemObjects.PolicyType:
                    list.Add("Name", "Name");
                    list.Add("Description", "Description");
                    list.Add("TextPath", "TextPath");
                    break;
                //case SystemObjects.Predicate:
                //    list.Add("Name", "Name");
                //    list.Add("TextPath", "TextPath");
                //    break;
                case SystemObjects.Resource:
                case SystemObjects.ResourceType:
                    list.Add("First Name", "FirstName");
                    list.Add("Last Name", "LastName");
                    list.Add("Email", "Email");
                    break;
                case SystemObjects.RuleType:
                    list.Add("Name", "Name");
                    list.Add("Description", "Description");
                    break;
                case SystemObjects.TaxonomyType:
                    list.Add("Name", "Name");
                    list.Add("Description", "Description");
                    list.Add("TextPath", "TextPath");
                    break;                
            }

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = "{" + i.Value + "}" }),
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
                    list.AddRange(
                        Company.Filter<Artifact>(i => i.ArtifactTypeID == id)
                        .OrderBy(i => i.TextPath)
                        .Select(i => new ListIntItem { title = i.TextPath, value = i.ID })
                    );
                    break;
                case SystemObjects.ReferenceItem:
                case SystemObjects.ReferenceItemType:
                    list.AddRange(
                        Company.Filter<ReferenceItem>(i => i.ReferenceItemTypeID == id)
                        .OrderBy(i => i.DisplayValue)
                        .Select(i => new ListIntItem  { title = i.DisplayValue, value = i.ID })
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
                    list.AddRange(
                        Company.Table<GlobalReportingResource>().ToList()
                        .OrderBy(i => i.FullName)
                        .Select(i => new ListIntItem  { title = i.FullName, value = i.ResourceID })
                    );
                    break;
                case SystemObjects.RuleType:
                    list.AddRange(
                        Company.Filter<Rule>(i => i.RuleTypeID == id)
                        .OrderBy(i => i.Name)
                        .Select(i => new ListIntItem { title = i.Name, value = i.ID })
                    );
                    break;
                case SystemObjects.TaxonomyType:
                    list.AddRange(
                        Company.Filter<Taxonomy>(i => i.TaxonomyTypeID == id)
                        .OrderBy(i => i.TextPath)
                        .Select(i => new ListIntItem { title = i.TextPath, value = i.ID })
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

            var patterns = new Dictionary<string, string>() {
                { "Choose sample...", "" },
                { "Email", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b" },
                { "IP Address", @"^$|^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$" },
                { "North American Phone", @"^$|\b\d{3}[-.]?\d{3}[-.]?\d{4}\b" },                
                { "Internal Url", @"^$|\b(http(s)?:\/\/){1}([\da-z\.-]+)([\/\w \.-]*)*\/?\b" },
                { "Public Url", @"^$|\b(http(s)?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?\b" },
                { "US Zip Code", @"^$|\b[0-9]{5}(?:-[0-9]{4})?\b" }
            };
            var dataTypeOptions = DataType.Boolean.GetDataTypeInfoList()
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
                                ID = i.ID,
                                Object = i.Object,
                                ObjectID = i.ObjectID,
                                DisplayFields = (i.FieldTypeFilteredLookupDisplayFields != null) ? i.FieldTypeFilteredLookupDisplayFields.Select(df => new { value = $"{df.FieldTypeID}|{df.FieldTypeName}", Filter = df.Filter, Show = df.Show, SortOrder = df.SortOrder }).ToList() : null,
                                HideHeader = i.HideHeader,
                                HideFooter = i.HideFooter
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
                                ID = r.ID,
                                IntersectType = r.IntersectTypeID,
                                ReferenceType = r.RelationType,
                                ChildIntersectType = 0,
                                DisplayFields = new List<dynamic>(),
                                HideHeader = lookup.HideHeader,
                                HideFooter = lookup.HideFooter,
                                HideFilter = lookup.HideFilter,
                                Object = r.Object,
                                ObjectID = r.ObjectID
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

        #endregion

        #region Field Generation

        /// <param name="id">ID of the object</param>
        [Route("FieldType_DeleteFields"), NonNullableParameters]
        public JsonResult FieldType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.FieldType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            //if (Company.Table<FieldWithRelation>().Any(i => i.FieldTypeID == id))
            //    return jsonException(FormInfo.FieldType_Error_Used, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<FieldType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        private void CheckIsFieldTypeNameReserved(string name)
        {
            var nameUpper = name.ToUpper();

            if (nameUpper == "PARENTID") throw new Exception("Use of a field type with the name " + name + " is prohibited.");
            //if (nameUpper == "STATUS" || nameUpper == "NAME" || nameUpper == "DESCRIPTION" || nameUpper == "PARENTID" || nameUpper == "DATELASTCERTIFIED" || nameUpper == "TAXONOMYTYPEID") throw new Exception("Use of a field type with the name " + name + " is prohibited.");
        }

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFieldType")]
        public JsonResult AddFieldType(FieldTypeEditorModel model)
        {
            try
            {
                int maxSort = 0;
                try { maxSort = Company.GetFieldTypesByObject((SystemObjects)Enum.Parse(typeof(SystemObjects), model.FieldType.Object), model.FieldType.ObjectID).Max(i => i.SortOrder); }
                catch { }

                //dont let fields with reserved names in
                CheckIsFieldTypeNameReserved(model.FieldType.Name);

                model.FieldType.SortOrder = maxSort + 1;

                var nameRegex = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z0-9_-]+$");
                if (!nameRegex.IsMatch(model.FieldType.Name))
                {
                    throw new ConflictException("Error Occurred!", $"{Resources.FieldInfo.ApiName_Name} can only have uppercase letters, lowercase letters, numbers, dash, or underscore. It must also begin with a letter.");
                }

                if (model.FieldType.MinimumLength.HasValue && model.FieldType.MaximumLength.HasValue)
                {
                    if (model.FieldType.MinimumLength.Value > model.FieldType.MaximumLength.Value)
                    {
                        throw new ConflictException("Error Occurred!", "You may not have a minimum length that is greater than the maximum length.");
                    }
                }

                if (!model.FieldType.IsRequired) model.FieldType.MinimumLength = 0;

                var val = model.Validation();
                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                switch (model.FieldType.Type)
                {
                    case "Html":
                        model.FieldType.MinimumLength = (!model.FieldType.IsRequired) ? (int?)null : 1;
                        model.FieldType.MaximumLength = null;
                        Company.Add<FieldType>(model.FieldType);
                        break;
                    case "Lookup":
                        #region
                        if (string.IsNullOrEmpty(model.FieldType.LookupDisplayFormat))
                        {
                            throw new ConflictException("Error Occurred!", $"{Resources.FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
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
                                //FieldTypeID = model.FieldType.ID,
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
                            //Company.Add<FieldTypeRelationLookupDefinition>(def);

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
                                //FieldTypeID = model.FieldType.ID,
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
                                RelationType = r.ReferenceType

                            });
                            if (r.DisplayFields == null)
                                r.DisplayFields = new List<FieldTypeItemDisplayFieldEditorModel>();
                            foreach (var f in r.DisplayFields.Where(i => i.Show || !string.IsNullOrEmpty(i.FilterValue)))
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
                                    Show = f.Show
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
                        catch (Exception ex)
                        {
                            throw ex;
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
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        break;
                    #endregion
                    default:
                        Company.Add<FieldType>(model.FieldType);
                        break;
                }

                return jsonSuccess(Resources.FormInfo.Add_FieldType_Confirmation, model.FieldType.ID.ToString(), "add", HttpStatusCode.Created);
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
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FieldType>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);
                Company.Delete("FieldType", id);//Company.Delete<FieldType>(model);

                return jsonSuccess(Resources.FormInfo.Delete_FieldType_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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
            var qry = Company.Table<FieldTypeLookupValue>().OrderBy(i => i.LookupObjectType).ThenBy(i => i.Name).AsQueryable();

            var fusDef = a.FieldTypeFusionLookupDefinitions.FirstOrDefault();

            if (!a.IsRequired) a.MinimumLength = 0;

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

                if (ft == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                var val = model.Validation();
                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                var nameRegex = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z0-9_-]+$");
                if (!nameRegex.IsMatch(model.FieldType.Name))
                {
                    throw new ConflictException("Error Occurred!", $"{Resources.FieldInfo.ApiName_Name} can only have uppercase letters, lowercase letters, numbers, dash, or underscore. It must also begin with a letter.");
                }

                // Static fields
                var oldType = ft.Type;

                ft.Name = model.FieldType.Name;
                ft.Category = model.FieldType.Category;
                ft.FriendlyName = model.FieldType.FriendlyName;
                ft.DefaultValue = (string.IsNullOrEmpty(model.FieldType.DefaultValue)) ? null : model.FieldType.DefaultValue.Trim();
                ft.DisplayDescription = model.FieldType.DisplayDescription;
                ft.FormDescription = model.FieldType.FormDescription;
                ft.ValidationDescription = model.FieldType.ValidationDescription;
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

                ft.MinimumLength = model.FieldType.MinimumLength;
                ft.MaximumLength = model.FieldType.MaximumLength;
                ft.Pattern = model.FieldType.Pattern;

                if (!ft.IsRequired) ft.MinimumLength = 0;

                bool isNew;

                var defs = Company.Filter<FieldTypeFusionLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFusionLookupDisplayFields).ToList();
                var efli = Company.Filter<FieldTypeFilteredLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFilteredLookupDisplayFields).FirstOrDefault();
                var fl = Company.Filter<FieldTypeLookup>(i => i.FieldTypeID == ft.ID).FirstOrDefault();

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
                            throw new ConflictException("Error Occurred!", $"{Resources.FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
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
                                RelationType = r.ReferenceType

                            });
                            if (r.DisplayFields == null)
                                r.DisplayFields = new List<FieldTypeItemDisplayFieldEditorModel>();
                            foreach(var f in r.DisplayFields.Where(i => i.Show || !string.IsNullOrEmpty(i.FilterValue)))
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
                                    Show = f.Show
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
                        } catch (Exception ex)
                        {
                            throw ex;
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
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        break;
                        #endregion

                }

                Company.Update<FieldType>(ft);

                return jsonSuccess(Resources.FormInfo.Edit_FieldType_Confirmation, ft.ID.ToString(), "edit", HttpStatusCode.OK);
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

        /// <param name="fat">FusionTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("Fusion_AddFields"), NonNullableParameters]
        public JsonResult Fusion_AddFields(int ft)
        {
            var list = new List<EditableField>();
            var type = Company.GetById<FusionType>(ft);
            var fusion = new Fusion();

            list.Add(new EditableField { FieldName = "FusionTypeID", FieldType = DataType.Hidden.ToString(), Value = ft.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = fusion.GetName(i => i.Name), FieldDescription = fusion.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = fusion.GetName(i => i.Description), FieldDescription = fusion.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Manual", Name = fusion.GetName(i => i.Manual), FieldDescription = fusion.GetDescription(i => i.Manual), FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, FieldName = "Enabled", Name = fusion.GetName(i => i.Enabled), FieldDescription = fusion.GetDescription(i => i.Enabled), FieldType = DataType.Boolean.ToString() });

            var intervalTypes = new List<SelectListItem>();
            intervalTypes.Add(new SelectListItem { Text = "Minute(s)", Value = "3" });
            intervalTypes.Add(new SelectListItem { Text = "Hour(s)", Value = "2" });
            //intervalTypes.Add(new SelectListItem { Text = "Day(s)", Value = "1" });
            list.Add(new EditableField { Row = 4, Column = 1, FieldName = "IntervalType", Required= true, Name = fusion.GetName(i => i.IntervalType), FieldDescription = fusion.GetDescription(i => i.IntervalType), FieldType = DataType.Lookup.ToString(), Items = intervalTypes });
            list.Add(new EditableField { Row = 4, Column = 2, Required=true, FieldName = "Interval", Name = fusion.GetName(i => i.Interval), FieldDescription = fusion.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString() });

            list.Add(new EditableField { Row = 5, Column = 3, FieldName = "LockPromotedItems", Name = fusion.GetName(i => i.LockPromotedItems), FieldDescription = fusion.GetDescription(i => i.LockPromotedItems), FieldType = DataType.Boolean.ToString() });

            var owners = Company.GetFusionOwnerOptions().Select(i => new SelectListItem { Text = i.Name, Value = $"{i.ID}", Selected = false }).ToList();
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "Owners", Name = "Owners", FieldDescription = "You must assign one or more owners for this configuration.", FieldType = DataType.Lookup.ToString(), MultiSelect = true, Items = owners });
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.FusionType, ft).ToList(), 7);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
        [Route("Fusion_DeleteFields"), NonNullableParameters]
        public JsonResult Fusion_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Fusion>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
        [Route("Fusion_EditFields"), NonNullableParameters]
        public JsonResult Fusion_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Fusion>(id, i => i.FusionOwners);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });

            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Manual", Name = a.GetName(i => i.Manual), FieldDescription = a.GetDescription(i => i.Manual), FieldType = DataType.Boolean.ToString(), Value = a.Manual.ToString().ToLower() });
            list.Add(new EditableField { Row = 3, Column = 2, FieldName = "Enabled", Name = a.GetName(i => i.Enabled), FieldDescription = a.GetDescription(i => i.Enabled), FieldType = DataType.Boolean.ToString(), Value = a.Enabled.ToString().ToLower() });

            var intervalTypes = new List<SelectListItem>();
            intervalTypes.Add(new SelectListItem { Text = "Minute(s)", Value = "3" });
            intervalTypes.Add(new SelectListItem { Text = "Hour(s)", Value = "2" });
            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "IntervalType", Name = a.GetName(i => i.IntervalType), FieldDescription = a.GetDescription(i => i.IntervalType), FieldType = DataType.Lookup.ToString(), Items = intervalTypes, Value = a.IntervalType.HasValue ? ((int)a.IntervalType.Value).ToString() : "" });
            list.Add(new EditableField { Row = 4, Column = 2, Required = true,  FieldName = "Interval", Name = a.GetName(i => i.Interval), FieldDescription = a.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString(), Value = (a.Interval.HasValue ? a.Interval.Value.ToString() : "") });

            list.Add(new EditableField { Row = 5, Column = 1, FieldName = "ForceRefresh", Name = "Force Refresh on Next Run?", FieldDescription = "Force the local agent to perform a full refresh of this configuration on the next run.", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 5, Column = 2, FieldName = "LockPromotedItems", Name = a.GetName(i => i.LockPromotedItems), FieldDescription = a.GetDescription(i => i.LockPromotedItems), FieldType = DataType.Boolean.ToString(), Value = a.LockPromotedItems.ToString().ToLower() });

            var owners = Company.GetFusionOwnerOptions()
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = $"{i.ID}",
                    Selected = a.FusionOwners.Any(c => c.ID == i.ID)
                }).ToList();
            list.Add(new EditableField { Row = 6, Column = 1, Required = true, FieldName = "Owners", Name = "Owners", FieldDescription = "You must assign one or more owners for this configuration.", FieldType = DataType.Lookup.ToString(), MultiSelect = true, Items = owners});


            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.FusionType, a.FusionTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Fusion, id).ToList(), 7);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFusion")]
        public JsonResult AddFusion(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                int typeID = parseIntField(form, "FusionTypeID");
                var type = Company.GetById<FusionType>(typeID);
                if (type == null) throw new NotFoundException("fusion type");

                var rawOwners = parseTextField(form, "Owners");
                if (string.IsNullOrEmpty(rawOwners))
                    return jsonException("No selected owners", HttpStatusCode.BadRequest);

                var items = rawOwners.Split(',').ToList().Select(i => int.Parse(i)).ToList();

                var ownerArtifacts = Company.Filter<Artifact>(i => items.Contains(i.ID)).ToList();

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
        public JsonResult DeleteFusion(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var model = Company.GetById<Fusion>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("configuration");

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

        [HttpDelete, Route("DeleteFusionByID"), NonNullableParameters]
        public JsonResult DeleteFusionByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusion(form);
        }

        [HttpPut, ValidateInput(false), Route("EditFusion")]
        public JsonResult EditFusion(FormCollection form)//(int typeID, int id, FusionAttributeType model)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var model = Company.GetById<Fusion>(parseIntField(form, "ID"), i => i.FusionOwners);
                if (model == null) throw new NotFoundException("configuration");

                var rawOwners = parseTextField(form, "Owners");
                if (string.IsNullOrEmpty(rawOwners))
                    return jsonException("No selected owners", HttpStatusCode.BadRequest);

                var items = rawOwners.Split(',').ToList().Select(i => int.Parse(i)).ToList();

                var ownerArtifacts = Company.Filter<Artifact>(i => items.Contains(i.ID)).ToList();

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
                var ownersToRemove = new List<Artifact>();
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

                Company.SaveOrUpdate<Fusion>(model, fields);

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

        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("PostAddFusionRule")]
        public JsonResult PostAddFusionRule(FusionRule r)
        {
            try
            {
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

                Company.Add<FusionRule>(rule);
                Company.SaveChanges();

                //automatically add all items for query attribute types
                var exists = Company.FusionRuleItem.Any(i => i.RuleID == rule.ID && i.ObjectType == "FusionQueryAttributeType");
                if (r.ObjectType == "FusionQueryAttributeType" && !exists)
                {
                    var item = new FusionRuleItem();
                    item.ObjectType = "FusionQueryAttribute";
                    item.ObjectID = null;
                    item.RuleID = rule.ID;

                    Company.Add<FusionRuleItem>(item);
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFusionRule")]
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

                Company.Add<FusionRule>(item);

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
                Company.Delete<FusionRule>(i => i.ID == id);
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

            var attributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == a.Fusion.FusionTypeID).Select(i => new { i.ID, i.Name, @Type = "FusionAttributeType" }).ToList();
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

        [HttpPost, Route("PostEditFusionRule")]
        public JsonResult PostEditFusionRule(FusionRule r)
        {
            try
            {
                //if (!form.HasKeys()) throw new NoFormDataException("fusion rule");

                var model = Company.GetById<FusionRule>(r.ID);
                if (model == null) throw new NotFoundException("promotion rule");

                var type = model.ObjectType;

                model.Enabled = r.Enabled;
                model.Description = r.Description;
                model.FusionID = r.FusionID;
                model.ObjectID = r.ObjectID;
                model.ObjectType = r.ObjectType;

                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update<FusionRule>(model);

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

                model.Enabled = parseBooleanField(form, "Enabled");
                model.Description = parseTextField(form, "Description");
                model.FusionID = parseIntField(form, "FusionID");
                model.ObjectID = parseIntField(form, "FusionAttributeTypeID");
                model.ObjectType = "FusionAttributeType";

                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update<FusionRule>(model);

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

        #region Field Generation

        /// <param name="id">RuleID</param>
        [Route("FusionRule_DeleteFields"), NonNullableParameters]
        public ActionResult FusionRule_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionRule>(id);
            if (a == null) return new HttpNotFoundResult();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion

        #region FusionRuleFilter

        #region Field Generation

        [Route("FusionRuleFilter_DeleteFields"), NonNullableParameters]
        public JsonResult FusionRuleFilter_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

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
                                sql += $" inner join Field F{f.FieldTypeID} on F{f.FieldTypeID}.FieldTypeID = {f.FieldTypeID} and F{f.FieldTypeID}.ObjectType = '{rule.ObjectType.Replace("Type", "")}' and F{f.FieldTypeID}.ObjectID = A.ID and {string.Format(queryFormat, $"F{f.FieldTypeID}.FormattedValue", f.Value.Replace("'", "''"))}";
                        }
                    }
                }

                sql += " " + whereSql;

                return sql;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("TestFusionRuleFilter")]
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

            if (!Company.HasPermission(SystemObjects.Fusion, rule.FusionID, Claim.Create))
                return null;

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

        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("AddFusionRuleFilter")]
        public JsonResult AddFusionRuleFilter(FusionRuleFilterEditorModel form)
        {
            try
            {
                int ruleID = form.FusionRuleID;
                var rule = Company.GetById<FusionRule>(ruleID);

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

            if (!Company.HasPermission(SystemObjects.Fusion, filter.FusionRule.FusionID, Claim.Create))
                return null;

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

        [HttpPut, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("EditFusionRuleFilter")]
        public JsonResult EditFusionRuleFilter(FusionRuleFilterEditorModel form)
        {
            try
            {
                var filter = Company.GetById<FusionRuleFilter>(form.ID.Value);

                if (filter != null)
                {
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

        //[ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFusionRuleItem")]
        //public JsonResult AddFusionRuleItem(FormCollection form)
        //{
        //    try
        //    {
        //        var ruleID = parseIntField(form, "RuleID");
        //        var rule = Company.GetById<FusionRule>(ruleID);
        //        if (rule != null)
        //        {
        //            rule.UpdatedBy = Company.CurrentResourceID;
        //            rule.UpdatedOn = DateTime.UtcNow;
        //        }

        //        var fusionAttributeIDs = form["FusionAttributeID"].Split(',').ToList();
        //        if (fusionAttributeIDs.Count == 0)
        //        {
        //            Company.Set<FusionRuleItem>().Add(
        //                new FusionRuleItem { RuleID = ruleID, ObjectID = null }
        //                );
        //        }
        //        else
        //        {
        //            fusionAttributeIDs.ForEach(fa =>
        //            {
        //                int? fusionAttributeID = null;
        //                if (!string.IsNullOrEmpty(fa))
        //                {
        //                    fusionAttributeID = int.Parse(fa);
        //                }
        //                Company.Set<FusionRuleItem>().Add(
        //                    new FusionRuleItem { RuleID = ruleID, ObjectID = fusionAttributeID }
        //                    );
        //            });
        //        }
        //        Company.SaveChanges();

        //        return jsonSuccess("Target item(s) successfully created.", "0", "add", HttpStatusCode.Created);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        [HttpDelete, Route("DeleteFusionRuleFilter")]
        public JsonResult DeleteFusionRuleFilter(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                var filter = Company.GetById<FusionRuleFilter>(id);
                if (filter != null)
                {
                    var rule = Company.GetById<FusionRule>(filter.RuleID);
                    if (rule != null)
                    {
                        rule.UpdatedBy = Company.CurrentResourceID;
                        rule.UpdatedOn = DateTime.UtcNow;
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

        #region Field Generation

        [Route("FusionRuleItem_DeleteFields"), NonNullableParameters]
        public JsonResult FusionRuleItem_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpGet, Route("GetAddFusionRuleItem"), NonNullableParameters]
        public JsonNetResult GetAddFusionRuleItem(int id)
        {
            var rule = Company.GetById<FusionRule>(id);

            if (rule == null)
                return null;

            if (!Company.HasPermission(SystemObjects.Fusion, rule.FusionID, Claim.Create))
                return null;

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
        
        [HttpPost, ValidateInput(false),ValidateHttpAntiForgeryToken, Route("PostAddFusionRuleItem")]
        public JsonResult PostAddFusionRuleItem(FusionAddItemModel form)
        {
            try
            {
                int ruleID = form.RuleID;
                var rule = Company.GetById<FusionRule>(ruleID);
                bool allSelected = form.AllSelected;
                List<string> attributes = new List<string>();

                if (!string.IsNullOrEmpty(form.attributeIDs))
                    attributes = form.attributeIDs.Split(',').ToList();

                if(attributes.Count == 0 && allSelected)
                {
                    {
                        Company.Set<FusionRuleItem>().Add(
                            new FusionRuleItem { RuleID = ruleID, ObjectID = null, ObjectType = form.ObjectType }
                            );
                    }
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFusionRuleItem")]
        public JsonResult AddFusionRuleItem(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "RuleID");
                var rule = Company.GetById<FusionRule>(ruleID);
                if (rule != null)
                {
                    rule.UpdatedBy = Company.CurrentResourceID;
                    rule.UpdatedOn = DateTime.UtcNow;
                }

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
                var item = Company.GetById<FusionRuleItem>(id);
                if (item != null)
                {
                    var rule = Company.GetById<FusionRule>(item.RuleID);
                    if (rule != null)
                    {
                        rule.UpdatedBy = Company.CurrentResourceID;
                        rule.UpdatedOn = DateTime.UtcNow;
                    }
                    Company.FusionRuleItem.Remove(item);
                    Company.SaveChanges();
                }
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

        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("PostAddFusionRuleStep")]
        public JsonResult PostAddFusionRuleStep(FusionRuleStep s)
        {
            try
            {
                var ruleID = s.RuleID;

                if (ruleID <= 0) return jsonException("", HttpStatusCode.NotFound, "");

                var rule = Company.GetById<FusionRule>(ruleID);

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
                        Company.Add<FusionRuleStepSetting>(new FusionRuleStepSetting
                        {
                            RuleStepID = item.ID,
                            Name = setting.Key,
                            Value = setting.Value
                        });
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

        private void AddPromotionStepSettings(FusionRuleStep item, FormCollection form)
        {
            var action = (parseTextField(form, "Action") ?? "").ToUpper();

            if (action == "PROMOTE")
            {
                #region PROMOTE

                var promoteTo = parseTextField(form, "PrOptionsDropdown"); // Pipe delimited Object | ObjectID

                var promoteToInfo = promoteTo.Split('|');
                var objectType = "";
                var objectID = "";
                var parentObjectType = "";

                if (promoteToInfo.Length >= 2)
                {
                    objectID = promoteToInfo[0];
                    objectType = promoteToInfo[1];
                }

                if (promoteToInfo.Length >= 3)
                {
                    parentObjectType = promoteToInfo[2];
                }

                var parentSearchType = parseTextField(form, "PrOptionsParentSearchDropdown"); //ParentObjectSearch

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Object", Value = objectID });

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectID", Value = objectType });

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectSearch", Value = parentSearchType });

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectTypeID", Value = parentObjectType });

                if ((parentSearchType ?? "").ToUpper().Trim() == "DIRECT")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectID", Value = parseTextField(form, "PrOptionsParentDropdown") });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObject", Value = objectType });
                }
                else if ((parentSearchType ?? "").ToUpper().Trim() == "RESULTFROMSTEP")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObject", Value = "Step" });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectID", Value = parseTextField(form, "PromotionParentStep") });
                }
                else if ((parentSearchType ?? "").ToUpper().Trim() == "FUSIONOWNER")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObject", Value = "Owner" });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectID", Value = parseTextField(form, "PromotionParentOwnerRule") });
                }

                #endregion PROMOTE
            }
            else if (action == "FIND")
            {
                #region FIND

                var findSearchType = parseTextField(form, "FindSearchType"); //ObjectSearch

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectSearch", Value = findSearchType });

                //if the search type is result from step the object is step and the object id is the step id
                var findType = (findSearchType ?? "").ToUpper();

                if (findType == "GLOSSARY")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Object", Value = parseTextField(form, "FindTypeName") });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectID", Value = parseTextField(form, "FindTypeID") });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "FilterField", Value = parseTextField(form, "FindSearchField") });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "TargetField", Value = parseTextField(form, "TargetSearchField") });
                }
                else if (findType == "FUSION")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "FilterField", Value = parseTextField(form, "FindSearchField") });

                    handleSearchParameters("Find", "Object", item.FusionRuleStepSettings, findType, item.ID, form);
                }
                else
                {
                    handleSearchParameters("Find", "Object", item.FusionRuleStepSettings, findType, item.ID, form);
                }

                #endregion FIND
            }
            else if (action == "FINDRELATION")
            {
                #region FINDRELATION

                var intersectType = parseTextField(form, "FindIntersectType");
                var searchType = parseTextField(form, "FindSearchType");

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "IntersectType", Value = intersectType });
                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Search", Value = searchType });

                handleSearchParameters("Find", "", item.FusionRuleStepSettings, searchType, item.ID, form);

                #endregion FINDRELATION
            }
            else if (action == "RELATE")
            {
                #region RELATE

                var intersectType = parseTextField(form, "RelateIntersectType");

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "IntersectType", Value = intersectType });

                //subject settings
                var subjectSearch = parseTextField(form, "RelateSubjectSearchType");

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "SubjectSearch", Value = subjectSearch });

                handleSearchParameters("Relate", "Subject", item.FusionRuleStepSettings, subjectSearch, item.ID, form);

                // object settings
                var objectSearch = parseTextField(form, "RelateObjectSearchType");

                handleSearchParameters("Relate", "Object", item.FusionRuleStepSettings, objectSearch, item.ID, form);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectSearch", Value = objectSearch });

                #endregion RELATE
            }
            else if (action == "LINEAGE")
            {
                #region LINEAGE

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "SubjectSearch", Value = "ResultFromStep" });

                handleSearchParameters("Lineage", "Subject", item.FusionRuleStepSettings, "ResultFromStep", item.ID, form);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectSearch", Value = "ResultFromStep" });

                handleSearchParameters("Lineage", "Object", item.FusionRuleStepSettings, "ResultFromStep", item.ID, form);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "TechnicalSubjectSearch", Value = "ResultFromStep" });

                handleSearchParameters("Lineage", "TechnicalSubject", item.FusionRuleStepSettings, "ResultFromStep", item.ID, form);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "TechnicalObjectSearch", Value = "ResultFromStep" });

                handleSearchParameters("Lineage", "TechnicalObject", item.FusionRuleStepSettings, "ResultFromStep", item.ID, form);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Role", Value = parseTextField(form, "LineageRole") });

                #endregion LINEAGE
            }
        }

        private void AddPromotionStepSettings(FusionRuleStep item)
        {

            var action = item.Action.ToUpper();
            var settings = item.Settings;

            if (action == "PROMOTE")
            {
                #region PROMOTE

                var objectType = settings["Object"];
                var objectID = settings["ObjectID"];
                var parentObjectType = settings["ParentObjectTypeID"] ?? "";
                var parentObjectSearch = settings.ContainsKey("ParentObjectSearch") ? settings["ParentObjectSearch"] ?? "" : "";


                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Object", Value = objectType });

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectID", Value = objectID });

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectSearch", Value = parentObjectSearch });

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectTypeID", Value = parentObjectType });

                var parentObjectID = settings.ContainsKey("ParentObjectID") ? settings["ParentObjectID"] ?? "" : "";

                if ((parentObjectSearch ?? "").ToUpper().Trim() == "DIRECT")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectID", Value = parentObjectID });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObject", Value = objectType });
                }
                else if ((parentObjectSearch ?? "").ToUpper().Trim() == "RESULTFROMSTEP")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObject", Value = "Step" });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectID", Value = parentObjectID });
                }
                else if ((parentObjectSearch ?? "").ToUpper().Trim() == "FUSIONOWNER")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObject", Value = "Owner" });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ParentObjectID", Value = parentObjectID });
                }

                #endregion PROMOTE
            }
            else if (action == "FIND")
            {
                #region FIND

                var findSearchType = settings["FindSearchType"]; //ObjectSearch

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectSearch", Value = settings["FindSearchType"] });

                //if the search type is result from step the object is step and the object id is the step id
                var findType = (findSearchType ?? "").ToUpper();

                if (findType == "GLOSSARY")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Object", Value = settings["Object"] });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectID", Value = settings["ObjectID"] });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "FilterField", Value = settings["FilterField"] });

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "TargetField", Value = settings["TargetField"] });
                }
                else if (findType == "FUSION")
                {
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "FilterField", Value = settings["FilterField"] });

                    handleSearchParameters("Find", "Object", item.FusionRuleStepSettings, findType, item.ID, settings);
                }
                else
                {
                    handleSearchParameters("Find", "Object", item.FusionRuleStepSettings, findType, item.ID, settings);
                }

                #endregion FIND
            }
            else if (action == "FINDRELATION")
            {
                #region FINDRELATION

                var intersectType = settings["FindIntersectType"];
                var searchType = settings["FindSearchType"];

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "IntersectType", Value = intersectType });
                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Search", Value = searchType });

                handleSearchParameters("Find", "Object", item.FusionRuleStepSettings, searchType, item.ID, settings);

                #endregion FINDRELATION
            }
            else if (action == "RELATE")
            {
                #region RELATE

                var intersectType = settings["IntersectType"];

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "IntersectType", Value = intersectType });

                //subject settings
                var subjectSearch = settings["RelateSubjectSearchType"];

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "SubjectSearch", Value = subjectSearch });

                handleSearchParameters("Relate", "Subject", item.FusionRuleStepSettings, subjectSearch, item.ID, settings);

                // object settings
                var objectSearch = settings["RelateObjectSearchType"];

                handleSearchParameters("Relate", "Object", item.FusionRuleStepSettings, objectSearch, item.ID, settings);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectSearch", Value = objectSearch });

                #endregion RELATE
            }
            else if (action == "LINEAGE")
            {
                #region LINEAGE

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "SubjectSearch", Value = "ResultFromStep" });

                handleSearchParameters("Lineage", "Subject", item.FusionRuleStepSettings, "ResultFromStep", item.ID, settings);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectSearch", Value = "ResultFromStep" });

                handleSearchParameters("Lineage", "Object", item.FusionRuleStepSettings, "ResultFromStep", item.ID, settings);

                try
                {
                    //The user can skip adding these items, so do not error out of the whole process.  Should have better way to do this.
                    handleSearchParameters("Lineage", "TechnicalSubject", item.FusionRuleStepSettings, "ResultFromStep", item.ID, settings);
                    handleSearchParameters("Lineage", "TechnicalObject", item.FusionRuleStepSettings, "ResultFromStep", item.ID, settings);

                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "TechnicalSubjectSearch", Value = "ResultFromStep" });
                    item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "TechnicalObjectSearch", Value = "ResultFromStep" });
                }
                catch
                {
                }

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Role", Value = settings["Role"] });

                #endregion LINEAGE
            }
          
        }

        private void handleSearchParameters(string area, string target, ICollection<FusionRuleStepSetting> fusionRuleStepSettings, string searchType, int id, FormCollection form)
        {
            var searchUpper = (searchType ?? "").ToUpper();
            if (searchUpper == "RESULTFROMSTEP")
            {
                fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = target,
                                Value = "Step"
                            });

                fusionRuleStepSettings.Add(
                    new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = $"{target}ID",
                        Value = parseTextField(form, $"{area}{target}Step")
                    });

                //special find parent option
                if (string.Compare(area, "FIND", true) == 0)
                {
                    var findParent = parseBooleanField(form, "FindParent");

                    if (findParent)
                    {
                        fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = "FindParent",
                                Value = "1"
                            });
                    }
                }
            }
            else if (searchUpper == "SELF")
            {
                fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = target,
                                Value = "Self"
                            });

                fusionRuleStepSettings.Add(
                    new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = $"{target}ID",
                        Value = "0"
                    });
            }
            else if (searchUpper == "DIRECT")
            {
                var subjectObject = parseTextField(form, $"{area}{target}Item", "").Split('|');

                if (subjectObject.Length >= 2)
                {
                    fusionRuleStepSettings.Add(
                                new FusionRuleStepSetting
                                {
                                    RuleStepID = id,
                                    Name = target,
                                    Value = subjectObject[0]
                                });

                    fusionRuleStepSettings.Add(
                        new FusionRuleStepSetting
                        {
                            RuleStepID = id,
                            Name = $"{target}ID",
                            Value = subjectObject[1]
                        });
                }
            }
            else if (searchUpper == "FUSIONOWNER")
            {
                fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = target,
                                Value = "Owner"
                            });

                fusionRuleStepSettings.Add(
                    new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = $"{target}ID",
                        Value = parseTextField(form, $"{area}{target}OwnerRule")
                    });

            }
            else if (searchUpper == "FUSION")
            {
                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = $"{target}",
                    Value = "FusionAttributeType"
                });

                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = $"{target}ID",
                    Value = parseTextField(form, $"{area}{target}FusionAttribute")
                });
            }
            else if (searchUpper == "PROMOTION")
            {
                var filterField = parseTextField(form, "FindSearchField");

                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = "FilterField",
                    Value = filterField
                });

                if (filterField != "-2")
                {
                    fusionRuleStepSettings.Add(new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = "TargetField",
                        Value = parseTextField(form, "TargetSearchField")
                    });
                }



                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = "PromotionStepID",
                    Value = parseTextField(form, "PromotionStepName")
                });
                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = "PromotionFusionAttributeTypeID",
                    Value = parseTextField(form, "FusionAttributeTypeName")
                });
            }
        }

        private void handleSearchParameters(string area, string target, ICollection<FusionRuleStepSetting> fusionRuleStepSettings, string searchType, int id, Dictionary<string, string> settings)
        {
               var searchUpper = (searchType ?? "").ToUpper();
            if (searchUpper == "RESULTFROMSTEP")
            {
                fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = target,
                                Value = "Step"
                            });

                fusionRuleStepSettings.Add(
                    new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = $"{target}ID",
                        Value = settings[$"{area}{target}Step"]
                    });

                //special find parent option
                if (string.Compare(area, "FIND", true) == 0)
                {
                    
                    var findParent = (settings.ContainsKey("FindParent") && settings["FindParent"] == "true");

                    if (findParent)
                    {
                        fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = "FindParent",
                                Value = "1"
                            });
                    }
                }
            }
            else if (searchUpper == "SELF")
            {
                fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = target,
                                Value = "Self"
                            });

                fusionRuleStepSettings.Add(
                    new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = $"{target}ID",
                        Value = "0"
                    });
            }
            else if (searchUpper == "DIRECT")
            {
                var subjectObject = settings[$"{area}{target}Item"].Split('|');

                if (subjectObject.Length >= 2)
                {
                    fusionRuleStepSettings.Add(
                                new FusionRuleStepSetting
                                {
                                    RuleStepID = id,
                                    Name = target,
                                    Value = subjectObject[0]
                                });

                    fusionRuleStepSettings.Add(
                        new FusionRuleStepSetting
                        {
                            RuleStepID = id,
                            Name = $"{target}ID",
                            Value = subjectObject[1]
                        });
                }
            }
            else if (searchUpper == "FUSIONOWNER")
            {
                fusionRuleStepSettings.Add(
                            new FusionRuleStepSetting
                            {
                                RuleStepID = id,
                                Name = target,
                                Value = "Owner"
                            });

                fusionRuleStepSettings.Add(
                    new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = $"{target}ID",
                        Value = settings[$"{target}ID"]//settings[$"{area}{target}OwnerRule"]
                    });

            }
            else if (searchUpper == "FUSION")
            {
                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = $"{target}",
                    Value = "FusionAttributeType"
                });

                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = $"{target}ID",
                    Value = settings[$"{area}{target}FusionAttribute"]
                });
            }
            else if (searchUpper == "PROMOTION")
            {
                var filterField = settings["FindSearchField"];

                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = "FilterField",
                    Value = filterField
                });

                if (filterField != "-2")
                {
                    fusionRuleStepSettings.Add(new FusionRuleStepSetting
                    {
                        RuleStepID = id,
                        Name = "TargetField",
                        Value = settings["TargetSearchField"]
                    });
                }



                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = "PromotionStepID",
                    Value = settings["PromotionStepName"]
                });
                fusionRuleStepSettings.Add(new FusionRuleStepSetting
                {
                    RuleStepID = id,
                    Name = "PromotionFusionAttributeTypeID",
                    Value = settings["FusionAttributeTypeName"]
                });
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

        [HttpPut, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("PutEditFusionRuleStep")]
        public JsonResult PutEditFusionRuleStep(FusionRuleStep s)
        {
            try
            {
                var ruleID = s.RuleID;
                var ruleStepID = s.ID;

                if (ruleID <= 0 || ruleStepID <= 0) return null;

                var rule = Company.GetById<FusionRule>(ruleID);

                if (rule == null) return null;

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
                        Company.Add<FusionRuleStepSetting>(new FusionRuleStepSetting
                        {
                            RuleStepID = step.ID,
                            Name = setting.Key,
                            Value = setting.Value
                        });
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

        [ValidateHttpAntiForgeryToken, HttpPut, ValidateInput(false), Route("MoveFusionRuleStep")]
        public ActionResult MoveFusionRuleStep(int ruleID, int ruleStepID, bool moveUp)
        {
            var direction = moveUp ? "UP" : "DOWN";
            var currentRule = Company.GetById<FusionRule>(ruleID);
            var itemToMove = currentRule.FusionRuleSteps.SingleOrDefault(x => x.ID == ruleStepID);
            int currentStepNumber = itemToMove.Step;

            if (currentRule != null)
            {
                currentRule.UpdatedBy = Company.CurrentResourceID;
                currentRule.UpdatedOn = DateTime.UtcNow;
            }

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

        #region Field Generation

        /// <param name="id">RuleID</param>
        [Route("FusionRuleStep_AddFields"), NonNullableParameters]
        public ActionResult FusionRuleStep_AddFields(int id)
        {
            var list = new List<EditableField>();
            var currentRule = Company.GetById<FusionRule>(id);
            if (currentRule == null) return new HttpNotFoundResult();

            list.Add(new EditableField { FieldName = "ruleID", FieldType = DataType.Hidden.ToString(), Value = currentRule.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Description", false, "", 1, 4000) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Action", Name = "Action", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Action", true, "", 1, 25) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Step", Name = "Step", FieldType = DataType.Text.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("FusionRuleStep_EditFields"), NonNullableParameters]
        public ActionResult FusionRuleStep_EditFields(int id, int ruleStepID)
        {
            var list = new List<EditableField>();
            var currentRule = Company.GetById<FusionRule>(id);
            if (currentRule == null) return new HttpNotFoundResult();

            var step = currentRule.FusionRuleSteps.FirstOrDefault(x => x.ID == ruleStepID);

            if (step == null) return new HttpNotFoundResult();

            list.Add(new EditableField { FieldName = "ruleStepID", FieldType = DataType.Hidden.ToString(), Value = step.ID.ToString() });
            list.Add(new EditableField { FieldName = "ruleID", FieldType = DataType.Hidden.ToString(), Value = currentRule.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Description", false, "", 1, 4000), Value = step.Description });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Action", Name = "Action", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Action", true, "", 1, 25), Value = step.Action });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Step", Name = "Step", FieldType = DataType.Text.ToString(), Value = step.Step.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("FusionRuleStep_DeleteFields"), NonNullableParameters]
        public ActionResult FusionRuleStep_DeleteFields(int id, int ruleStepID)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionRule>(id);
            if (a == null) return new HttpNotFoundResult();

            var step = a.FusionRuleSteps.First(x => x.ID == ruleStepID);
            if (step == null) return new HttpNotFoundResult();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { FieldName = "RuleStepID", FieldType = DataType.Hidden.ToString(), Value = step.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("FusionRuleStep_MoveFields"), NonNullableParameters]
        public ActionResult FusionRuleStep_MoveFields(int id, int ruleStepID, string direction)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionRule>(id);
            if (a == null) return new HttpNotFoundResult();

            var step = a.FusionRuleSteps.First(x => x.ID == ruleStepID);
            if (step == null) return new HttpNotFoundResult();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { FieldName = "RuleStepID", FieldType = DataType.Hidden.ToString(), Value = step.ID.ToString() });
            list.Add(new EditableField { FieldName = "Direction", FieldType = DataType.Hidden.ToString(), Value = direction });

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #endregion

        #region FusionRuleStepMapping

        #region Field Generation

        [Route("FusionRuleStepMapping_DeleteFields"), NonNullableParameters]
        public JsonResult FusionRuleStepMapping_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

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

            // These settings must be before the relate check below.
            //var promotionType = ruleStep.PromotionObjectType;
            var promotionType = ruleStep.GetSettingValueByName("Object");
            var promotionObjectType = ruleStep.GetSettingValueByName("PromotionParentObjectType");
            int promotionObjectID = 0;
            int.TryParse(ruleStep.GetSettingValueByName("ObjectID"), out promotionObjectID);

            if (ruleStep.Action.ToLower() == "relate")
            {
                //find the referenced step
                int intersectTypeID = 0;
                int.TryParse(ruleStep.GetSettingValueByName("IntersectType"), out intersectTypeID);
                if (intersectTypeID > 0)
                {
                    promotionType = "IntersectType";
                    promotionObjectID = intersectTypeID;
                }
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
                    if (!targetFieldNames.Contains("Name"))
                        targetFields.Add(new SelectListItem { Text = "Name", Value = "Name|0" });
                    if (!targetFieldNames.Contains("Description"))
                        targetFields.Add(new SelectListItem { Text = "Description", Value = "Description|0" });

                    targetFields.AddRange(targetDynamicFields);

                    if (promotionType == "ArtifactType")
                    {
                        if (!targetFieldNames.Contains("Subject Area"))
                            targetFields.Add(new SelectListItem { Text = "Subject Area", Value = "TaxonomyTypeID|0" });
                    }
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
            var ruleStep = Company.GetById<FusionRuleStep>(id);

            if (ruleStep == null)
                return null;

            if (!Company.HasPermission(SystemObjects.Fusion, ruleStep.FusionRule.FusionID, Claim.Create))
                return null;

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

        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("PostAddFusionRuleStepMapping")]
        public JsonResult PostAddFusionRuleStepMapping(FusionRuleStepMapping map)
        {
            try
            {
                var model = new FusionRuleStepMapping
                {
                    RuleStepID = map.RuleStepID
                };

                model.SourceFieldName = map.SourceFieldName;
                model.SourceFieldTypeID = map.SourceFieldTypeID;
                model.TargetFieldName = map.TargetFieldName;
                model.TargetFieldTypeID = map.TargetFieldTypeID;


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

                Company.Add<FusionRuleStepMapping>(model);

                var ruleStep = Company.GetById<FusionRuleStep>(model.RuleStepID, i => i.FusionRule);
                if (ruleStep != null)
                {
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFusionRuleStepMapping")]
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

                Company.Add<FusionRuleStepMapping>(model);

                var ruleStep = Company.GetById<FusionRuleStep>(model.RuleStepID, i => i.FusionRule);
                if (ruleStep != null)
                {
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
                var currentRule = Company.GetById<FusionRule>(id);//, i => i.FusionRuleSteps);
                var itemToRemove = currentRule.FusionRuleSteps.SingleOrDefault(x => x.ID == ruleStepID);

                if (itemToRemove == null) throw new Exception("Fusion Rule Step not found.");                                

                Company.ObjectContext.DeleteObject(itemToRemove);

                if (currentRule != null)
                {
                    currentRule.UpdatedBy = Company.CurrentResourceID;
                    currentRule.UpdatedOn = DateTime.UtcNow;
                }

                Company.SaveChanges();

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
                Company.Delete<FusionRuleStepMapping>(i => i.ID == id);
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

        [HttpPut, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("PutEditFusionRuleStepMapping")]
        public JsonResult PutEditFusionRuleStepMapping(FusionRuleStepMapping map)
        {
            try
            {

                var model = Company.GetById<FusionRuleStepMapping>(map.ID, i => i.FusionRuleStep.FusionRule);
                if (model == null) throw new NotFoundException("field mapping");

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

                Company.Update<FusionRuleStepMapping>(model);

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
                if (model == null) throw new NotFoundException("field mapping");

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

                Company.Update<FusionRuleStepMapping>(model);

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

        #region Field Generation

        [Route("FusionType_AddFields")]
        public JsonResult FusionType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.FusionType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var fusionType = new FusionType();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = fusionType.GetName(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = fusionType.GetName(i => i.Description), FieldType = DataType.Html.ToString() });
            loadIconFields(list, 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionTypeID</param>
        [Route("FusionType_DeleteFields"), NonNullableParameters]
        public JsonResult FusionType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionType>(id);

            if (!Company.HasPermission(SystemObjects.FusionType, a.ID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionTypeID</param>
        [Route("FusionType_EditFields"), NonNullableParameters]
        public JsonResult FusionType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionType>(id);

            if (!Company.HasPermission(SystemObjects.FusionType, a.ID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var style = Company.GetObjectStyle(SystemObjects.FusionType, id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = a.GetName(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });
            loadIconFields(list, 3, style);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ActionName("FusionType"), HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("FusionType")]
        public JsonResult PostFusionType(FusionType fusion, ObjectStyle style = null)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.FusionType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                //   if (!form.HasKeys()) throw new NoFormDataException("fusion type");

                var model = new FusionType
                {
                    Description = fusion.Description, //parseTextField(form, "Description"),
                    Name = fusion.Name //parseTextField(form, "Name", null, true)
                };

                Company.Add<FusionType>(model);

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
        public JsonResult DeleteFusionType(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion type");

                var model = Company.GetById<FusionType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion type");

                if (!Company.HasPermission(SystemObjects.FusionType, model.ID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (Company.Filter<FusionAttributeType>(i => i.FusionTypeID == model.ID).Count() > 0)
                    return jsonException(FormInfo.FusionType_Remove, HttpStatusCode.Conflict);

                Company.Delete<FusionType>(model);
                deleteObjectStyle(SystemObjects.FusionType, model.ID);

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
               // if (!form.HasKeys()) throw new NoFormDataException("fusion type");

                var model = Company.GetById<FusionType>(fusion.ID);
                if (model == null) throw new NotFoundException("fusion type");

                if (!Company.HasPermission(SystemObjects.FusionType, model.ID, Claim.Update, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Description = fusion.Description; // parseTextField(form, "Description");
                model.Name = fusion.Name;  //parseTextField(form, "Name", null, true);

                Company.Update<FusionType>(model);
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
            if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Create))
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

            if (!Company.HasPermission(SystemObjects.Fusion, a.FusionID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            //list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.FusionAttributeType, a.FusionAttributeTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.FusionAttribute, a.ID).ToList(), 2, true);

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

                if (!Company.HasPermission(SystemObjects.Fusion, model.FusionID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);


                var sType = SystemObjects.FusionAttribute.ToString();

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

        #region FusionAttributeType

        [Route("getfusionattributetypes"), NonNullableParameters]
        public JsonNetResult GetFusionAttributeTypes(int fusionID)
        {
            var model = Company.GetById<Fusion>(fusionID, i => i.FusionType.FusionAttributeTypes);
            return new JsonNetResult {
                Data = model.FusionType.FusionAttributeTypes.OrderBy(i => i.TextPath),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #region Field Generation

        /// <param name="fat">FusionTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("FusionAttributeType_AddFields"), NonNullableParameters]
        public JsonResult FusionAttributeType_AddFields(int ft, int p)
        {
            if (!Company.HasPermission(SystemObjects.FusionType, ft, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var type = Company.GetById<FusionType>(ft);

            list.Add(new EditableField { FieldName = "FusionTypeID", FieldType = DataType.Hidden.ToString(), Value = ft.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            if (p > 0)
                list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "ScanEnabled", Name = "Agent Scanning Enabled?", FieldDescription = "Allow the fusion agent to scan for this metadata. If disabled on a parent, scanning will also be disabled on all child attribute types.", FieldType = DataType.Boolean.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
        [Route("FusionAttributeType_DeleteFields"), NonNullableParameters]
        public JsonResult FusionAttributeType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionAttributeType>(id);

            if (!Company.HasPermission(SystemObjects.FusionType, a.FusionTypeID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
        [Route("FusionAttributeType_EditFields"), NonNullableParameters]
        public JsonResult FusionAttributeType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionAttributeType>(id, i => i.Parent);

            if (!Company.HasPermission(SystemObjects.FusionType, a.FusionTypeID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            bool scanEnabledIsReadOnly = false;
            bool scanEnabledValue = a.ScanEnabled;
            if (a.Parent != null)
            {
                scanEnabledIsReadOnly = !a.Parent.ScanEnabled;
                if (!a.Parent.ScanEnabled)
                {
                    scanEnabledValue = false;
                }
            }
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, ReadOnly = scanEnabledIsReadOnly, FieldName = "ScanEnabled", Name = "Agent Scanning Enabled?", FieldDescription = "Allow the fusion agent to scan for this metadata. If disabled on a parent, scanning will also be disabled on all child attribute types.", FieldType = DataType.Boolean.ToString(), Value = scanEnabledValue.FormatBooleanReadOnlyValue() });



            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ActionName("FusionAttributeType"), HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, Route("FusionAttributeType")]
        public JsonResult PostFusionAttributeType(FusionAttributeType fusion, ObjectStyle style = null)
        {
            try
            {
                int typeID = fusion.FusionTypeID; 
                int? parentID = fusion.ParentID;

                var type = Company.GetById<FusionType>(typeID);
                if (type == null) throw new NotFoundException("fusion type");

                if (!Company.HasPermission(SystemObjects.FusionType, typeID, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new FusionAttributeType
                {
                    FusionTypeID = typeID,
                    ParentID = parentID,
                    Name = fusion.Name,
                    ScanEnabled = fusion.ScanEnabled//,
                    //Query = fusion.Query
                };

                Company.Add<FusionAttributeType>(model);

                if (style != null)
                    upsertObjectStyle(SystemObjects.FusionAttributeType, model.ID, style.IconForeColor, style.IconBackColor, model.Name);

                return jsonSuccess(type.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created, new { ParentID = model.ParentID, Type = "FusionAttributeType", Context = "FusionAttributeType", Name = model.Name });
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

        [HttpDelete, Route("DeleteFusionAttributeType"), NonNullableParameters]
        public JsonResult DeleteFusionAttributeType(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attribute type");

                var model = Company.GetById<FusionAttributeType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion attribute type");

                if (!Company.HasPermission(SystemObjects.FusionType, model.FusionTypeID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (Company.Filter<FusionAttribute>(i => i.FusionAttributeTypeID == model.ID).Count() > 0)
                    return jsonException(FormInfo.FusionAttributeType_Remove, HttpStatusCode.Conflict);

                Company.Delete<FusionAttributeType>(model);
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

        [HttpDelete, Route("DeleteFusionAttributeTypeByID"), NonNullableParameters]
        public JsonResult DeleteFusionAttributeTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionAttributeType(form);
        }

        [ActionName("FusionAttributeType"), HttpPut, ValidateInput(false), Route("FusionAttributeType")]
        public JsonResult PutFusionAttributeType(FusionAttributeType fusion, ObjectStyle style = null)
        {
            try
            {
                var model = Company.GetById<FusionAttributeType>(fusion.ID, p => p.Parent);
                if (model == null) throw new NotFoundException("fusion attibute type");

                if (!Company.HasPermission(SystemObjects.FusionType, model.FusionTypeID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = fusion.Name;
                if (model.Parent != null)
                {
                    model.ScanEnabled = (model.Parent.ScanEnabled) ? fusion.ScanEnabled : false;
                }
                else
                {
                    model.ScanEnabled = fusion.ScanEnabled;
                }

                //model.Query = fusion.Query;

                Company.Update<FusionAttributeType>(model);

                if (style != null)
                    upsertObjectStyle(SystemObjects.FusionAttributeType, model.ID, style.IconForeColor, style.IconBackColor, model.Name);

                Company.PerformObjectActionAfterSaveChanges(model);

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK, new { ParentID = model.ParentID, Type = "FusionAttributeType", Context = "FusionAttributeType", Name = model.Name });
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

        #region FusionQueryAttributeType
                
        protected JsonResult EditFusionQueryAttribute(FormCollection form)//(int typeID, int id, FusionAttributeType model)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attibute type");

                var model = Company.GetById<FusionQueryAttributeType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion attibute type");

                if (!Company.HasPermission(SystemObjects.Fusion, model.FusionID, Claim.Update))
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
                Company.Update<FusionQueryAttributeType>(model);

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
                                
                if (!Company.HasPermission(SystemObjects.Fusion, type.ID, Claim.Create, ClaimObject.Root))
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

                Company.Add<FusionQueryAttributeType>(model);

                foreach (var column in columns)
                {
                    Company.Add<FieldType>(new FieldType
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

        protected JsonResult DeleteFusionQueryAttribute(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attribute type");

                var model = Company.GetById<FusionQueryAttributeType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion query attribute type");

                if (!Company.HasPermission(SystemObjects.FusionQueryAttributeType, model.ID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (Company.Filter<FusionQueryAttribute>(i => i.FusionQueryAttributeTypeID == model.ID).Count() > 0)
                    return jsonException(FormInfo.FusionAttributeType_Remove, HttpStatusCode.Conflict);

                Company.Delete<FusionQueryAttributeType>(model);
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

        #region Intersect/Other Relationships

        [HttpDelete, Route("DeleteIntersect"), NonNullableParameters]
        public JsonResult DeleteIntersect(int id)
        {
            try
            {
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

        #region Field Generation

        /// <param name="id">IntersectTypeID</param>
        [Route("IntersectType_DeleteFields"), NonNullableParameters]
        public JsonResult IntersectType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.IntersectType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            if (Company.Filter<Intersect>(i => i.IntersectTypeID == id).Count() > 0)
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Conflict);

            var list = new List<EditableField>();
            var a = Company.GetById<IntersectType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

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
                    { "Side1", $"{type.Subject}|{type.SubjectID}" },
                    { "Side2", $"{type.Object}|{type.ObjectID}" },
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
                    .Where(i => i.Type.AsInfoModel().AllowIntersectTypeAssignment && !usedPredicateIDs.Contains(i.ID))
                    .Select(i => new {
                        title = $"{i.Name} ({i.Type.AsInfoModel().Name})",
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

        [Route("IntersectType_Side1Options")]
        public JsonNetResult IntersectType_Side1Options()
        {
            var models = Company.GetIntersectTypeOptions()
                .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("IntersectType_Side2Options"), NonNullableParameters]
        public JsonNetResult IntersectType_Side2Options(SystemObjects type, int id, SystemObjects? side2Type = null, int? side2ID = null, int? predicateID = null)
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddIntersectType")]
        public JsonResult AddIntersectType(FormCollection form)
        {
            try
            {
                if (form == null) throw new NoFormDataException("relationship type");

                var side1 = form["Side1"];
                var side1Info = side1.Split('|');
                var side2 = form["Side2"];
                var side2Info = side2.Split('|');

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
                if ((side1 != side2) && !predicateModel.Type.AsInfoModel().AllowDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object must be the same when using this Predicate.");
                }
                if ((side1 == side2) && predicateModel.Type.AsInfoModel().ForceDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object may not be the same when using this Predicate.");
                }

                var model = new IntersectType {
                    Subject = side1Info[0],
                    SubjectID = int.Parse(side1Info[1]),
                    Object = side2Info[0],
                    ObjectID = int.Parse(side2Info[1]),
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

                if (!Company.HasPermission(SystemObjects.IntersectType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (Company.Filter<Intersect>(i => i.IntersectTypeID == id).Count() > 0)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Conflict);

                var model = Company.GetById<IntersectType>(id);
                if (model == null) throw new NotFoundException("relationship type");

                Company.Delete<IntersectType>(model);

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
                if (!Company.HasPermission(SystemObjects.IntersectType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var model = Company.GetById<IntersectType>(id);
                if (model == null) throw new NotFoundException("relationship type");


                var side1 = form["Side1"];
                var side1Info = side1.Split('|');

                var side2 = form["Side2"];
                var side2Info = side2.Split('|');

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
                if ((side1 != side2) && !predicateModel.Type.AsInfoModel().AllowDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object must be the same when using this Predicate.");
                }
                if ((side1 == side2) && predicateModel.Type.AsInfoModel().ForceDifferentSubjectObject)
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "The subject and object may not be the same when using this Predicate.");
                }

                model.Subject = side1Info[0];
                model.SubjectID = int.Parse(side1Info[1]);
                model.Object = side2Info[0];
                model.ObjectID = int.Parse(side2Info[1]);
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

        #region Group

        #region Field Generation

        [Route("Group_AddFields")]
        public JsonResult Group_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.Group, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            var resList = GetCompanyResources()
                .OrderBy(i => i.LastName)
                .ThenBy(i => i.FirstName)
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName })
                .ToList()
                .Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                .ToList();
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, Name = d360.core.resources.Fields.GroupPrimaryOwner_Name, FieldName = "PrimaryOwnerResourceID", FieldDescription = d360.core.resources.Fields.GroupPrimaryOwner_Description, FieldType = DataType.Lookup.ToString(), Items = resList });
            resList.Insert(0, new SelectListItem { Text = "None", Value = "", Group = new SelectListGroup { Name = "" } });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, Name = d360.core.resources.Fields.GroupSecondaryOwner_Name, FieldName = "SecondaryOwnerResourceID", FieldDescription = d360.core.resources.Fields.GroupSecondaryOwner_Description, FieldType = DataType.Lookup.ToString(), Items = resList });

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Group_AddGroupUserFields"), NonNullableParameters]
        public JsonResult Group_AddGroupUserFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Group, id, Claim.Update)) return jsonException("You do not have permissions to add users.", HttpStatusCode.Forbidden);
            if (!Company.Any<Group>(i => i.ID == id)) return jsonException("No group exists for the specified ID.", HttpStatusCode.NotFound);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            var currentGroupUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
            var resList = GetCompanyResources()
                .Where(i => !currentGroupUsers.Contains(i.ResourceID))
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName }).ToList().Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() }).ToList();
            resList.Insert(0, new SelectListItem { Text = "Please select", Value = "" });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "ResourceID", Name = "Resource", FieldType = DataType.Lookup.ToString(), Items = resList });

            //list.Add(new EditableField { Row = 1, Column = 2, FieldName = "IsOwner", Name = "Group Owner?", FieldType = DataType.Boolean.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">GroupID</param>
        [Route("Group_DeleteFields"), NonNullableParameters]
        public JsonResult Group_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Group, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Group>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Group_DeleteGroupUserFields"), NonNullableParameters]
        public JsonResult Group_DeleteGroupUserFields(int groupID, int resourceID)
        {
            if (!Company.HasPermission(SystemObjects.Group, groupID, Claim.Update)) return jsonException("You do not have permissions to remove users.", HttpStatusCode.Forbidden);
            var group = Company.GetById<Group>(groupID);
            if (group == null) return jsonException("No group exists for the specified ID.", HttpStatusCode.NotFound);
            if (!Community.Resources.Any(i => i.ID == resourceID)) return jsonException("No user exists for the specified ID.", HttpStatusCode.NotFound);
            if (resourceID == group.PrimaryOwnerResourceID) return jsonException("You may not remove this user as they are the group's primary owner.", HttpStatusCode.NotFound);
            if (resourceID == group.SecondaryOwnerResourceID) return jsonException("You may not remove this user as they are the group's secondary owner.", HttpStatusCode.NotFound);

            var list = new List<EditableField>();

            list.Add(new EditableField { Required = true, FieldName = "GroupID", FieldType = DataType.Hidden.ToString(), Value = groupID.ToString() });
            list.Add(new EditableField { FieldName = "ResourceID", FieldType = DataType.Hidden.ToString(), Value = resourceID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">GroupID</param>
        [Route("Group_EditFields"), NonNullableParameters]
        public JsonResult Group_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Group, id, Claim.Update))
                return jsonException("You do not have permissions to edit this.", HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Group>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            var currentGroupUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
            var resList = GetCompanyResources()
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName, MembershipStatus = currentGroupUsers.Any(o => o == i.ResourceID) ? "Current Member" : "Not Yet a Member" })
                .OrderBy(i => i.MembershipStatus)
                .ThenBy(i => i.LastName)
                .ThenBy(i => i.FirstName)
                .ToList()
                .Select(i => new SelectListItem { Group = new SelectListGroup { Name = i.MembershipStatus }, Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                .ToList();
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, Name = d360.core.resources.Fields.GroupPrimaryOwner_Name, FieldName = "PrimaryOwnerResourceID", FieldDescription = d360.core.resources.Fields.GroupPrimaryOwner_Description, FieldType = DataType.Lookup.ToString(), Items = resList, Value = (a.PrimaryOwnerResourceID.HasValue ? a.PrimaryOwnerResourceID.Value.ToString() : "") });
            resList.Insert(0, new SelectListItem { Text = "None", Value = "", Group = new SelectListGroup { Name = "" } });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, Name = d360.core.resources.Fields.GroupSecondaryOwner_Name, FieldName = "SecondaryOwnerResourceID", FieldDescription = d360.core.resources.Fields.GroupSecondaryOwner_Description, FieldType = DataType.Lookup.ToString(), Items = resList, Value = (a.SecondaryOwnerResourceID.HasValue ? a.SecondaryOwnerResourceID.Value.ToString() : "") });

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        #region Group : Add

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddGroup")]
        public JsonResult AddGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group");

                var primaryOwnerResourceID = parseIntField(form, "PrimaryOwnerResourceID");
                var secondaryOwnerResourceID = parseNullableIntField(form, "SecondaryOwnerResourceID");

                var a = new Group
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    PrimaryOwnerResourceID = primaryOwnerResourceID,
                    SecondaryOwnerResourceID = secondaryOwnerResourceID
                };

                Company.Add<Group>(a);

                Company.Add<ResourceGroup>(new ResourceGroup { GroupID = a.ID, ResourceID = primaryOwnerResourceID, IsOwner = true });
                try
                {
                    if (secondaryOwnerResourceID.HasValue)
                    {
                        if (!primaryOwnerResourceID.Equals(secondaryOwnerResourceID))
                            Company.Add<ResourceGroup>(new ResourceGroup { GroupID = a.ID, ResourceID = secondaryOwnerResourceID.Value, IsOwner = true });
                    }
                }
                catch
                {
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

        #endregion

        #region Group : Add User

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddGroupUser")]
        public JsonResult AddGroupUser(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group user");

                var id = parseIntField(form, "ID");
                var resourceID = parseIntField(form, "ResourceID");
                var owner = false;//bool.Parse(form["IsOwner"]);

                Company.Add<ResourceGroup>(new ResourceGroup { GroupID = id, ResourceID = resourceID, IsOwner = owner });

                return jsonSuccess("User successfully assigned.", resourceID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPost, ValidateHttpAntiForgeryToken, ValidateInput(false), ActionName("ResourceGroup"), Route("ResourceGroup")]
        public JsonResult PostResourceGroup(ResourceGroup model)
        {
            try
            {
                Company.Add(model);
                return jsonSuccess("User successfully assigned.", model.ResourceID.ToString(), "add", HttpStatusCode.Created);
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
        public JsonResult GetGroupUserList(int id)
        {
            if (!Company.HasPermission(SystemObjects.Group, id, Claim.Update)) return jsonException("You do not have permissions to add users.", HttpStatusCode.Forbidden);
            if (!Company.Any<Group>(i => i.ID == id)) return jsonException("No group exists for the specified ID.", HttpStatusCode.NotFound);

            var currentGroupUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
            var resList = GetCompanyResources()
                .Where(i => !currentGroupUsers.Contains(i.ResourceID))
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName }).ToList().Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() }).ToList();
            //resList.Insert(0, new SelectListItem { Text = "Please select", Value = "" });

            return Json(new { resourceList = resList }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Group : Delete User

        [HttpPut, Route("DeleteGroupUser")]
        public JsonResult DeleteGroupUser(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group user");

                var groupID = parseIntField(form, "GroupID");
                var resourceID = parseIntField(form, "ResourceID");

                if (!Company.HasPermission(SystemObjects.Group, groupID, Claim.Delete, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var rg = Company.Delete<ResourceGroup>(i => i.GroupID == groupID && i.ResourceID == resourceID);

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

        [HttpDelete, ValidateHttpAntiForgeryToken, ActionName("ResourceGroup"), Route("ResourceGroup"), NonNullableParameters]
        public JsonResult DeleteResourceGroup(int groupID, int resourceID)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Group, groupID, Claim.Delete, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var rg = Company.Delete<ResourceGroup>(i => i.GroupID == groupID && i.ResourceID == resourceID);

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

                Company.Delete<Group>(model);

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

        [HttpPut, ValidateInput(false), Route("EditGroup")]
        public JsonResult EditGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Group>(id);
                if (model == null) throw new NotFoundException("group");

                var primaryOwnerResourceID = parseIntField(form, "PrimaryOwnerResourceID");
                var secondaryOwnerResourceID = parseNullableIntField(form, "SecondaryOwnerResourceID");

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.PrimaryOwnerResourceID = primaryOwnerResourceID;
                model.SecondaryOwnerResourceID = secondaryOwnerResourceID;

                Company.Update<Group>(model);

                var currentGroupUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();

                if (!currentGroupUsers.Any(o => o == model.PrimaryOwnerResourceID))
                {
                    Company.Add<ResourceGroup>(new ResourceGroup { GroupID = model.ID, ResourceID = model.PrimaryOwnerResourceID.Value, IsOwner = true });
                }
                if (model.SecondaryOwnerResourceID.HasValue)
                {
                    if (!currentGroupUsers.Any(o => o == model.SecondaryOwnerResourceID))
                    {
                        Company.Add<ResourceGroup>(new ResourceGroup { GroupID = model.ID, ResourceID = model.SecondaryOwnerResourceID.Value, IsOwner = true });
                    }
                }

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
        
        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("Group"), Route("Group")]
        public JsonResult PostGroup(Group model)
        {
            try
            {
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

        [HttpPut, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("Group"), Route("Group")]
        public JsonResult PutGroup(Group model)
        {
            try
            {
               // if (!form.HasKeys()) throw new NoFormDataException("group");

                //var id = parseIntField(form, "ID");
                var existing = Company.GetById<Group>(model.ID);
                if (existing == null) throw new NotFoundException("group");

                //var primaryOwnerResourceID = parseIntField(form, "PrimaryOwnerResourceID");
                //var secondaryOwnerResourceID = parseNullableIntField(form, "SecondaryOwnerResourceID");

                existing.Name = model.Name;  //parseTextField(form, "Name", null, true);
                existing.Description = model.Description; // parseTextField(form, "Description");
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

        #region Lineage

        #region Supporting Json Feeds

        /// <summary>
        /// Gets a list of intersect types that support lineage.
        /// </summary>
        /// <returns>A list of relevant fusion attribute types.</returns>
        [Route("Lineage_IntersectTypes")]
        public JsonNetResult Lineage_IntersectTypes()
        {
            var lineageIntersectTypeIDs = Company.Filter<IntersectType>(i => i.Predicate.Type == PredicateType.Lineage).Select(i => i.ID).Distinct().ToList();
            return new JsonNetResult
            {
                Data = Company
                    .Filter<IntersectTypeDetail>(i => lineageIntersectTypeIDs.Contains(i.ID) && 
                        i.Subject != "IntersectType" && i.Object != "IntersectType" && 
                        i.Subject != "FusionAttributeType" && i.Object != "FusionAttributeType"
                    )
                    .ToList()
                    .Select(i => new { title = $"{i.SubjectName} {i.PredicateName ?? "to"} {i.ObjectName}", value = $"{i.ID}" })
                    .OrderBy(i => i.title),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("Lineage_IntersectTypeSources")]
        public JsonNetResult Lineage_IntersectTypeSources()
        {
            var lineageIntersectTypeIDs = Company.Filter<IntersectType>(i => i.Predicate.Type == PredicateType.Lineage).Select(i => i.ID).Distinct().ToList();

            var detail = Company
                    .Filter<IntersectTypeDetail>(i => lineageIntersectTypeIDs.Contains(i.ID) &&
                        i.Subject != "IntersectType" && i.Object != "IntersectType" &&
                        i.Subject != "FusionAttributeType" && i.Object != "FusionAttributeType"
                    ).ToList();

            var sources = detail.Select(i => new { i.Subject, i.SubjectID, i.SubjectName }).Distinct().ToList();
            var sourcesList = sources.Select(i => new { value = i.Subject + '|' + i.SubjectID, label = i.SubjectName, intersectTypeID = detail.First(d => d.Subject == i.Subject && d.SubjectID == i.SubjectID)?.ID ?? -1 });


            return new JsonNetResult
            {
                Data = sourcesList.ToList().OrderBy(i => i.label),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("Lineage_IntersectTypeTargets"), NonNullableParameters]
        public JsonNetResult Lineage_IntersectTypeTargets(string type, int id)
        {
            var lineageIntersectTypeIDs = Company.Filter<IntersectType>(i => i.Predicate.Type == PredicateType.Lineage).Select(i => i.ID).Distinct().ToList();

            var targets = Company
                    .Filter<IntersectTypeDetail>(i => lineageIntersectTypeIDs.Contains(i.ID) &&
                        i.Subject != "IntersectType" && i.Object != "IntersectType" &&
                        i.Subject != "FusionAttributeType" && i.Object != "FusionAttributeType"
                    )
                    .Where(i => i.Subject == type && i.SubjectID == id)
                    .ToList().Select(i => new { value = i.Object + '|' + i.ObjectID, label = i.ObjectName, intersectTypeID = i.ID }).Distinct();



            return new JsonNetResult
            {
                Data = targets.ToList().OrderBy(i => i.label),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of subjects based on the given intersect type.
        /// </summary>
        /// <param name="id">The Intersect Type's ID</param>
        /// <returns>A list of name/value pairs.</returns>
        [Route("Lineage_MapSubjects"), NonNullableParameters]
        public JsonNetResult Lineage_MapSubjects(int id)//, SystemObjects o, int oid)
        {
            var intersectType = Company.Filter<IntersectTypeDetail>(i => i.ID == id).FirstOrDefault();
            if (intersectType == null)
                return new JsonNetResult { Data = new { message = "Intersect Type not found." } };

            var list = Company.Query<dynamic>(@"
select  TextPath as title, 
        Object+'|'+cast(ObjectID as varchar) as value 
from    cache.ObjectDetails 
where   ObjectType = @type 
        and ObjectTypeID = @id
order by TextPath", new { type = new Dapper.DbString { IsAnsi = true, Value = intersectType.Subject }, id = id = intersectType.SubjectID });

            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Gets a list of objects based on the given intersect type.
        /// </summary>
        /// <param name="id">The Intersect Type's ID</param>
        /// <returns>A list of name/value pairs.</returns>
        [Route("Lineage_MapObjects"), NonNullableParameters]
        public JsonNetResult Lineage_MapObjects(int id)//, SystemObjects o, int oid)
        {
            var intersectType = Company.Filter<IntersectTypeDetail>(i => i.ID == id).FirstOrDefault();
            if (intersectType == null)
                return new JsonNetResult { Data = new { message = "Intersect Type not found." } };

            var list = Company.Query<dynamic>(@"
select  TextPath as title, 
        Object+'|'+cast(ObjectID as varchar) as value 
from    cache.ObjectDetails 
where   ObjectType = @type 
        and ObjectTypeID = @id
order by TextPath", new { type = new Dapper.DbString { IsAnsi = true, Value = intersectType.Object }, id = id = intersectType.ObjectID });

            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

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
order by A.TextPath", new { phrase, SubjectID = intersect.SubjectID });

            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Gets a list of map rules based on the currently selected map.
        /// </summary>
        /// <param name="model">The intersectID we are searching under.</param>
        /// <returns>A deep hierarchy of map rules.</returns>
        [HttpPost, Route("MapRulesByMap")]
        public JsonNetResult MapRulesByMap(SourceTargetIntersectModel model)
        {
            var list = Company.Query<string>(@"
select	MR.ID,
		(
			select	I.ID,
					I.FusionAttributeID,
					A.TextPath as FusionAttributeTextPath
			from	MapRuleItem I
					inner join FusionAttribute A on A.ID = I.FusionAttributeID and I.MapRuleID = MR.ID and I.IsSource = 1
			for json path
		) as Sources,
		(
			select	I.ID,
					I.FusionAttributeID,
					A.TextPAth as FusionAttributeTextPath
			from	MapRuleItem I
					inner join FusionAttribute A on A.ID = I.FusionAttributeID and I.MapRuleID = MR.ID and I.IsSource = 0
			for json path
		) as Targets,
		MR.Transformation
from	MapRule MR
		inner join MapRuleMap MRM on MRM.MapRuleID = MR.ID
		inner join Map M on M.ID = MRM.MapID
		inner join MapItem SMI on SMI.MapID = M.ID and SMI.IntersectID = @s and SMI.DiagramKey = @sd
		inner join MapItem TMI on TMI.MapID = M.ID and TMI.IntersectID = @t and TMI.DiagramKey = @td
for json path", new { s = model.SourceIntersectID, sd = model.SourceDiagramKey, t = model.TargetIntersectID, td = model.TargetDiagramKey });

            var json = string.Join("", list);
            var arr = (string.IsNullOrEmpty(json)) ? new JArray() : JArray.Parse(json);

            return new JsonNetResult
            {
                Data = arr,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of map rules based on the currently selected object.
        /// </summary>
        /// <param name="models">A collection of source/target intersect IDs.</param>
        /// <returns>A deep hierarchy of map rules.</returns>
        [HttpPost, Route("MapRulesByObject")]
        public JsonNetResult MapRulesByObject(SourceTargetIntersectModels models)
        {
            if (models == null)
                jsonNetException("No valid models present.", HttpStatusCode.BadRequest);

            if (models.Items.Count <= 0)
                jsonNetException("No valid models present.", HttpStatusCode.BadRequest);

            var modelsSql = "";

            models.Items.ForEach(m =>
            {
                modelsSql += (string.IsNullOrEmpty(modelsSql)) ? "" : " union ";
                modelsSql += $"select {m.SourceIntersectID} as SourceIntersectID, {m.TargetIntersectID} as TargetIntersectID";
            });
            //need work on this query.
            var list = Company.Query<string>($@"
select	MR.ID,
        O.SourceIntersectID,
        O.TargetIntersectID,
        (
			select	I.ID,
					I.SourceFusionAttributeID as FusionAttributeID,
					A.TextPath as FusionAttributeTextPath
			from	MapRuleItem I
					inner join FusionAttribute A on A.ID = I.SourceFusionAttributeID and I.MapRuleID = MR.ID
			for json path
		) as Sources,
		(
			select	I.ID,
					I.TargetFusionAttributeID as FusionAttributeID,
					A.TextPath as FusionAttributeTextPath
			from	MapRuleItem I
					inner join FusionAttribute A on A.ID = I.TargetFusionAttributeID and I.MapRuleID = MR.ID
			for json path
		) as Targets,
		MR.Transformation
from	MapRule MR
		inner join MapItemMap MIM on
        inner join MapItem MI on MI.SourceIntersectID
		inner join ({modelsSql}) O on O.SourceIntersectID = SMI.IntersectID and O.SourceDiagramKey = SMI.DiagramKey and O.TargetIntersectID = TMI.IntersectID and O.TargetDiagramKey = TMI.DiagramKey
for json path");

            var json = string.Join("", list);
            var arr = (string.IsNullOrEmpty(json)) ? new JArray() : JArray.Parse(json);

            return new JsonNetResult
            {
                Data = arr,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [ValidateHttpAntiForgeryToken, HttpPost, Route("MapRules_Save")]
        public JsonNetResult MapRules_Save(MapRulesModel model)
        {
            if (model.Rules == null)
                return jsonNetException("No rules specified", HttpStatusCode.BadRequest);

            var message = "";

            bool canCreate = true;// Company.HasPermission(obj, model.FocalID, Claim.Create);
            bool canUpdate = true;//Company.HasPermission(obj, model.FocalID, Claim.Update);
            bool canDelete = true;//Company.HasPermission(obj, model.FocalID, Claim.Delete);

            model.Rules.ForEach(viewRule =>
            {
                var map = Company.Filter<Map>(i =>
                    i.MapItems.Any(mi => mi.SourceIntersectID == viewRule.SourceIntersectID && mi.TargetIntersectID == viewRule.TargetIntersectID),
                    i => i.MapItems
                    ).FirstOrDefault();

                if (map == null)
                {
                    message += "No valid map found for the provided source and target.";
                }
                else
                {
                    if (viewRule.ID == 0 && !canCreate)
                    {
                        message += $"[{DateTime.Now}] You do not have permission to create mapping rules on this item.\n";
                    }
                    else if (viewRule.ID != 0 && !canUpdate)
                    {
                        message += $"[{DateTime.Now}] You do not have permission to update mapping rules on this item.\n";
                    }
                    else
                    {
                        MapRule mapRule = null;

                        if (viewRule.ID > 0)
                        {
                            mapRule = Company.GetById<MapRule>(viewRule.ID, i => i.MapRuleItems);
                        }
                        else
                        {
                            mapRule = new MapRule
                            {
                                MapRuleItems = new List<MapRuleItem>()
                            };
                        }

                        if (mapRule != null)
                        {
                            mapRule.Transformation = viewRule.Transformation;

                            #region Process Sources

                            viewRule.Sources.ForEach(s =>
                            {
                                if (s.FusionAttributeID > 0)
                                {
                                    #region Process Targets

                                    viewRule.Targets.ForEach(t =>
                                    {
                                        if (t.FusionAttributeID > 0)
                                        {
                                            var existingMapRuleItem = mapRule.MapRuleItems.SingleOrDefault(i => i.SourceFusionAttributeID == s.FusionAttributeID && i.TargetFusionAttributeID == t.FusionAttributeID);
                                            if (existingMapRuleItem == null)
                                            {
                                                mapRule.MapRuleItems.Add(new MapRuleItem { SourceFusionAttributeID = s.FusionAttributeID, TargetFusionAttributeID = t.FusionAttributeID });
                                            }
                                        }
                                    });

                                    #endregion

                                }
                            });

                            #endregion

                            #region Now check for any sources that have been deleted.

                            var mapItemsToDelete = new List<int>();
                            foreach (var existingMapRuleItem in mapRule.MapRuleItems)
                            {
                                if (
                                    !viewRule.Sources.Any(i => i.ID == existingMapRuleItem.ID) &&
                                    !viewRule.Targets.Any(i => i.ID == existingMapRuleItem.ID) &&
                                    existingMapRuleItem.ID > 0
                                )
                                {
                                    mapItemsToDelete.Add(existingMapRuleItem.ID);
                                }
                            }

                            #endregion

                            try
                            {
                                Company.SaveOrUpdate<MapRule>(mapRule);

                                if (canDelete)
                                {
                                    mapItemsToDelete.ForEach(id =>
                                    {
                                        var existingMapRuleItem = mapRule.MapRuleItems.Single(i => i.ID == id);
                                        Company.Delete<MapRuleItem>(existingMapRuleItem);
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                message += $"[{DateTime.Now}] An error occurred while saving rule changes: {ex.Message}\n{ex.StackTrace}\n\n";
                            }

                        }
                        else
                        {
                            message += $" The map rule with ID {viewRule.ID} could not be found.";
                        }
                    }
                }
            });

            return new JsonNetResult
            {
                Data = new { message, error = !string.IsNullOrEmpty(message) },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        /// <summary>
        /// Creates relationships for the various objects the user is adding to the diagram.
        /// </summary>
        /// <param name="model">An array of items to add relationships for.</param>
        /// <returns>A list of name/value pairs.</returns>
        [HttpPost, Route("Lineage_AddItemsToDiagram")]
        public JsonNetResult Lineage_AddItemsToDiagram(AddItemsToDiagramModel model)
        {
            model.Items.ForEach(i =>
            {
                try
                {
                    var intersect = Company.AddIntersect(i.IntersectTypeID, i.Subject, i.SubjectID, i.Object, i.ObjectID);
                    if (intersect != null)
                    {
                        i.Intersect = intersect;
                        i.IntersectID = intersect.ID;
                    }
                    else
                    {
                        i.ErrorMessage = "Relationship not successfully created";
                    }
                }
                catch (Exception ex)
                {
                    i.ErrorMessage = ex.GetFullExceptionData();
                }
            });

            return new JsonNetResult { Data = model.Items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Creates relationships for the various objects the user is adding to the diagram.
        /// </summary>
        /// <param name="models">An array of items to add relationships for.</param>
        /// <returns>A list of name/value pairs.</returns>
        [HttpPost, Route("Lineage_Update")]
        public JsonNetResult Lineage_Update(SourcePostModel models)
        {
            var message = "";
            var success = false;

            models.Adds.ForEach(model =>
            {
                #region 
                if (model.SourceIntersectID <= 0)
                {
                    message += $"The source you provided is invalid.";
                }
                else
                {
                    if (model.TargetIntersectID <= 0)
                    {
                        message += $"The target you provided is invalid.";
                    }
                    else
                    {
                        var newMap = new Map { Transformation = model.Transformation };
                        newMap.MapItems = new List<MapItem>();
                        newMap.MapItems.Add(new MapItem { SourceIntersectID = model.SourceIntersectID, TargetIntersectID = model.TargetIntersectID });
                        Company.Add<Map>(newMap);
                    }
                }
                
                #endregion
            });

            models.Deletes.ForEach(model =>
            {
                #region 
                if (model.MapID <= 0)
                {
                    message = $"The ID ({model.MapID}) is invalid.";
                }
                else
                {
                    var o = Company.GetById<Map>(model.MapID);
                    if (o == null)
                    {
                        message += $"The ID ({model.MapID}) could not be found.";
                    }
                    else
                    {
                        //if (!Company.HasPermission(model.Focal, model.FocalID, Claim.Delete, ClaimObject.Relationship))
                        //{
                        //    message = FormInfo.Permisions_Error_Delete;
                        //}
                        //else
                        //{
                            Company.Delete<Map>(o);
                        //}
                    }
                }
                #endregion
            });

            models.Edits.ForEach(model =>
            {
                #region 
                if (model.MapID <= 0)
                {
                    message += $"The map ID ({model.MapID}) is invalid.";
                }
                else
                {
                    var o = Company.GetById<Map>(model.MapID);
                    if (o == null)
                    {
                        message += $"The map with ID ({model.MapID}) cound not be found.";
                    }
                    else
                    {
                        o.Transformation = model.Transformation;
                        Company.Update(o);
                    }
                }
                #endregion
            });

            success = string.IsNullOrEmpty(message);

            if (string.IsNullOrEmpty(message))
            {
                message = "Successfully updated lineage.";
            }

            return new JsonNetResult
            {
                Data = new
                {
                    message = message,
                    success = success
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, Route("UpdateLineage")]
        public JsonNetResult UpdateLineage(LineageEditorModel model)
        {
            model.Deletes?.ForEach(d =>
            {
                var mapItem = Company.GetById<MapItem>(d.ID);
                
                var leftMaps = Company.MapItems.Where(m => m.TargetIntersectID == d.SourceIntersectID);
                var rightMaps = Company.MapItems.Where(m => m.SourceIntersectID == d.TargetIntersectID);
                
                try
                {
                    //leftMaps.ToList().ForEach(l =>
                    //{
                    //    //remove map items from map
                    //    l.Maps.ToList().ForEach(m =>
                    //    {
                    //        m.MapItems.Remove(l);
                    //    });

                    //    //remove map sequences and contexts
                    //    Company.MapSequences.RemoveRange(l.MapSequences);
                    //    //remove the map item
                    //    Company.MapItems.Remove(l);

                    //});

                    //rightMaps.ToList().ForEach(r =>
                    //{
                    //    r.Maps.ToList().ForEach(m =>
                    //    {
                    //        m.MapItems.Remove(r);
                    //    });
                    //    Company.MapSequences.RemoveRange(r.MapSequences);
                    //    Company.MapItems.Remove(r);

                    //});

                    mapItem.Maps.ToList().ForEach(m =>
                    {
                        m.MapItems.Remove(mapItem);
                    });
                    Company.MapSequences.RemoveRange(mapItem.MapSequences);
                    Company.MapItems.Remove(mapItem);

                    Company.SaveChanges();
                    
                } catch (Exception ex)
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

        [HttpPost, Route("UpdateTechnicalLineage")]
        public JsonNetResult UpdateTechnicalLineage(LineageEditorTechnicalModel model)
        {
            model.Deletes?.ForEach(d =>
            {
                var mapRuleItem = Company.GetById<MapRuleItem>(d.ID);

                var mapRuleItemMapItem = Company.MapRuleItemMapItems.Where(m => m.MapRuleItemID == mapRuleItem.ID).FirstOrDefault();

                var leftMaps = Company.MapRuleItems.Where(m => m.TargetFusionAttributeID == d.SourceFusionAttributeID);
                var rightMaps = Company.MapRuleItems.Where(m => m.SourceFusionAttributeID == d.TargetFusionAttributeID);

                try
                {
                    if (mapRuleItemMapItem != null)
                    {
                        Company.MapRuleItemMapItems.Remove(mapRuleItemMapItem);
                    }   

                    //leftMaps.ToList().ForEach(l =>
                    //{
                    //    //remove map items from map
                    //    l.MapRules.ToList().ForEach(m =>
                    //        {
                    //            m.MapRuleItems.Remove(l);
                    //        });

                    //    //remove the map item
                    //    Company.MapRuleItems.Remove(l);

                    //});

                    //rightMaps.ToList().ForEach(r =>
                    //{
                    //    r.MapRules.ToList().ForEach(m =>
                    //    {
                    //        m.MapRuleItems.Remove(r);
                    //    });

                    //    //remove the map item
                    //    Company.MapRuleItems.Remove(r);

                    //});

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

        List<string> getFieldNamesByType(string type, int id)
        {
            var fieldTypeNames = new List<string>();

            switch (type)
            {
                case "Lineage":
                    #region
                    fieldTypeNames.Add("Action");

                    fieldTypeNames.Add("Source Relation");
                    fieldTypeNames.Add("Source subject subject area");
                    fieldTypeNames.Add("Source subject");
                    fieldTypeNames.Add("Source object subject area");
                    fieldTypeNames.Add("Source object");

                    fieldTypeNames.Add("Source Fusion Configuration");
                    fieldTypeNames.Add("Source Fusion Path");

                    fieldTypeNames.Add("Target Relation");
                    fieldTypeNames.Add("Target subject subject area");
                    fieldTypeNames.Add("Target subject");
                    fieldTypeNames.Add("Target object subject area");
                    fieldTypeNames.Add("Target object");

                    fieldTypeNames.Add("Target Fusion Configuration");
                    fieldTypeNames.Add("Target Fusion Path");

                    fieldTypeNames.Add("Transformation");

                    break;
                #endregion
                case "Synonym":
                    #region
                    fieldTypeNames.Add("Source object type");
                    fieldTypeNames.Add("Source object type name");
                    fieldTypeNames.Add("Source object subject area");
                    fieldTypeNames.Add("Source object");

                    fieldTypeNames.Add("Target object type");
                    fieldTypeNames.Add("Target object type name");
                    fieldTypeNames.Add("Target object subject area");
                    fieldTypeNames.Add("Target object");
                    break;
                    #endregion
                case "TechnicalLineage":
                    #region
                    fieldTypeNames.Add("Source Fusion Configuration");
                    fieldTypeNames.Add("Source Fusion Path");
                    fieldTypeNames.Add("Target Fusion Configuration");
                    fieldTypeNames.Add("Target Fusion Path");
                    fieldTypeNames.Add("Group");
                    break;
                #endregion
                default:
                    #region
                    if (id > 0)
                    {
                        switch (type)
                        {
                            case "ArtifactType":
                            case "AttributeType":
                            case "ReferenceItemType":
                            case "IntersectType":
                            case "TaxonomyType":
                                fieldTypeNames.AddRange(
                                    Company
                                    .Filter<FieldType>(i => 
                                        i.Object == type && 
                                        i.ObjectID == id &&
                                        i.Type != "Attribute" &&
                                        i.Type != "FilteredLookup" &&
                                        i.Type != "FusionLookup" &&
                                        i.Type != "ComplexRelationLookup" &&
                                        i.Type != "OwnershipLookup"
                                    )
                                    .OrderBy(i => i.SortOrder)
                                    .Select(i => i.Name)
                                );
                                break;
                        }

                        switch (type)
                        {
                            case "ArtifactType":
                                #region
                                fieldTypeNames.Insert(0, "Name");
                                fieldTypeNames.Insert(1, "Description");
                                fieldTypeNames.Insert(2, "Subject Area");
                                var artifactType = Company.GetById<ArtifactType>(id, i => i.Parent);
                                if (artifactType.ParentID.HasValue)
                                    fieldTypeNames.Insert(3, string.Format("Parent {0}", artifactType.Parent.Name));
                                break;
                            #endregion
                            case "AttributeType":
                                #region
                                fieldTypeNames.Insert(0, "Owner Type");
                                fieldTypeNames.Insert(1, "Owner Type Name");
                                fieldTypeNames.Insert(2, "Owner Name");
                                break;
                            #endregion
                            case "ReferenceItemType":
                                #region
                                fieldTypeNames.Insert(0, "Code");
                                break;
                            #endregion
                            case "IntersectType":
                                #region
                                var intersectType = Company.Filter<IntersectTypeDetail>(i => i.ID == id).FirstOrDefault();
                                if (intersectType != null)
                                {
                                    fieldTypeNames.Insert(0, intersectType.ObjectName);

                                    if (intersectType.Object == "ArtifactType")
                                        fieldTypeNames.Insert(0, $"{intersectType.ObjectName} Subject Area");


                                    fieldTypeNames.Insert(0, intersectType.SubjectName);

                                    if (intersectType.Subject == "ArtifactType")
                                        fieldTypeNames.Insert(0, $"{intersectType.SubjectName} Subject Area");
                                }
                                break;
                            #endregion
                            case "TaxonomyType":
                                #region
                                var levels = Company.Query<int>("select MaximumDepth from TaxonomyType where ID = @id", new { id }).SingleOrDefault();
                                for (int i = 0; i < levels; i++)
                                {
                                    fieldTypeNames.Insert(i, "Level" + (i + 1));
                                }

                                // fieldTypeNames.Add("Name");
                                fieldTypeNames.Add("Description");
                                // fieldTypeNames.Add("Parent");
                                break;
                            #endregion
                        }
                    }
                    else
                    {
                        fieldTypeNames = new List<string>() {
                            "Item Type",
                            "Item Path",
                            "Responsibility",
                            "Resource"
                        };

                        switch (type)
                        {
                            case "ArtifactType":
                                fieldTypeNames.Insert(1, "Subject Area");
                                break;
                        }
                    }

                    break;
                    #endregion
            }

            return fieldTypeNames;
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
select 'ArtifactType|' + cast(ID as varchar(10)) as value, 'Glossary: ' + Name as title from ArtifactType
union
select 'TaxonomyType|' + cast(ID as varchar(10)) as value, 'Model: ' + Name as title from TaxonomyType
union
select 'ReferenceItemType|' + cast(ID as varchar(10)) as value, 'Reference Item: ' + Name as title from ReferenceItemType
) O order by title";
                    break;
                #endregion
                case "W":   // Propose Promotion
                    #region
                    sql = @"
select 'ArtifactType|' + cast(A.ID as varchar(10)) as value, 'Glossary: ' + A.Name as title 
from ArtifactType A
	inner join WorkflowTypeRelation WTR on WTR.Object = 'ArtifactType' and WTR.ObjectID = A.ID and WTR.Enabled = 1 and WTR.WorkflowType = 1";
                    break;
                #endregion
                case "R":   // Relation
                case "U":   // Unrelation
                    #region
                    sql = @"select 'IntersectType|' + cast(ID as varchar(10)) as value, Name as title from IntersectType where IsSystem = 0 order by Name";
                    break;
                    #endregion
                case "BL":   // Lineage
                    models = new List<OptionModel> { new OptionModel { title = "Default", value = "Lineage|-1" } };
                    break;
                case "TL":   // Technical Lineage
                    models = new List<OptionModel> { new OptionModel { title = "Default", value = "TechnicalLineage|-1" } };
                    break;
                case "S":   // Synonym
                    models = new List<OptionModel> { new OptionModel { title = "Default", value = "Synonym|-1" } };
                    break;
            }

            if (!string.IsNullOrEmpty(sql))
                models = Company.Query<OptionModel>(sql).OrderBy(i => i.title);

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("Load_ExpectedColumns"), NonNullableParameters]
        public JsonNetResult Load_ExpectedColumns(string type, int id)
        {
            return new JsonNetResult { Data = getFieldNamesByType(type, id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [FileDownload, Route("Load_ExpectedColumns_ToExcel"), NonNullableParameters]
        public FileResult Load_ExpectedColumns_ToExcel(string type, int id)
        {
            var document = new SLDocument();
            var defaultSheet = "Items";
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, defaultSheet);
            document.AddWorksheet("Lookups");
            document.SelectWorksheet(defaultSheet);
            var columns = getFieldNamesByType(type, id);
            var lookupColumns = 1;
            var parentColumnName = string.Empty;
            var artifactParentID = -1;

            if (type == "ArtifactType" && id > 0)
            {
                var artifactType = Company.GetById<ArtifactType>(id, i => i.Parent);
                if (artifactType.ParentID.HasValue)
                {
                    artifactParentID = artifactType.ParentID.Value;
                    parentColumnName = string.Format("Parent {0}", artifactType.Parent.Name).ToLower();
                }
            }

            #region Header

            for (int i = 0; i < columns.Count; i++)
            {
                SLStyle style = document.CreateStyle();

                style.Font.Bold = isRequiredColumn(type, id, columns[i], parentColumnName);

                document.SetCellStyle(1, i + 1, style);

                document.SetCellValue(1, i + 1, columns[i]);

                var lowerColName = columns[i].ToLower();

                if ((type == "ArtifactType" && lowerColName == "subject area") || (type == "IntersectType" && lowerColName.Contains("subject area")))
                {
                    var items = Company.Table<TaxonomyType>().OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (lowerColName == "action") //Lineage, Relationship
                {
                    var items = new List<string>() { "Add", "Remove" };

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (lowerColName == "item type" && id == 0) //Responsibility bulk load
                {
                    switch (type)
                    {
                        case "ArtifactType":
                            #region
                            var artifactTypeItems = Company.Table<ArtifactType>().OrderBy(x => x.Name).Select(x => x.Name);

                            if (artifactTypeItems.Any())
                            {
                                var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                                CreateExcelList(lookupColumns++, document, "Lookups", dv, artifactTypeItems);

                                document.AddDataValidation(dv);
                            }
                            break;
                            #endregion
                        case "ReferenceItemType":
                            #region
                            var referenceItemTypeItems = Company.Table<ReferenceItemType>().OrderBy(x => x.Name).Select(x => x.Name);

                            if (referenceItemTypeItems.Any())
                            {
                                var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                                CreateExcelList(lookupColumns++, document, "Lookups", dv, referenceItemTypeItems);

                                document.AddDataValidation(dv);
                            }
                            break;
                            #endregion
                        case "FusionType":
                            #region
                            var fusionTypeItems = Company.Table<FusionType>().OrderBy(x => x.Name).Select(x => x.Name);

                            if (fusionTypeItems.Any())
                            {
                                var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                                CreateExcelList(lookupColumns++, document, "Lookups", dv, fusionTypeItems);

                                document.AddDataValidation(dv);
                            }
                            break;
                            #endregion
                        case "PolicyType":
                            #region
                            var policyTypeItems = Company.Table<PolicyType>().OrderBy(x => x.Name).Select(x => x.Name);

                            if (policyTypeItems.Any())
                            {
                                var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                                CreateExcelList(lookupColumns++, document, "Lookups", dv, policyTypeItems);

                                document.AddDataValidation(dv);
                            }
                            break;
                            #endregion
                        case "TaxonomyType":
                            #region
                            var taxonomyTypeItems = Company.Table<TaxonomyType>().OrderBy(x => x.Name).Select(x => x.Name);

                            if (taxonomyTypeItems.Any())
                            {
                                var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                                CreateExcelList(lookupColumns++, document, "Lookups", dv, taxonomyTypeItems);

                                document.AddDataValidation(dv);
                            }
                            break;
                            #endregion
                    }
                }
                else if (lowerColName == "responsibility" && id == 0) //Responsibility bulk load
                {
                    var items = Company.Table<ResponsibilityType>().OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (lowerColName == "resource" && id == 0) //Responsibility bulk load
                {
                    var items = Company.Table<Group>().OrderBy(x => x.Name).Select(x => "Group:"+ x.Name).ToList();
                    items.AddRange(
                        Company.Table<GlobalReportingResource>().ToList().OrderBy(r => r.LastName).ThenBy(r => r.FirstName).Select(x => "User:" + x.FullName)
                     );

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (type == "ArtifactType" && lowerColName == parentColumnName)
                {
                    if (artifactParentID < 0) continue;

                    var items = Company.Filter<Artifact>(x => x.ArtifactTypeID == artifactParentID).OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (type == "Lineage" && lowerColName.In("source relation", "target relation"))
                {
                    var items = Company.Table<IntersectType>().Where(o => !o.IsSystem.Value || !o.IsSystem.HasValue).OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (type == "Synonym" && lowerColName.In("source object type", "target object type"))
                {
                    var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);
                    var typesList = new List<string> { "Artifact", "Policy", "Rule", "Taxonomy" };

                    CreateExcelList(lookupColumns++, document, "Lookups", dv, typesList.OrderBy(x => x));

                    document.AddDataValidation(dv);
                }
                else if (
                    ((type == "Lineage") && lowerColName.In("source subject subject area", "source object subject area", "target subject subject area", "target object subject area")) ||
                    (type == "Synonym" && lowerColName.In("source object subject area", "target object subject area"))
                    )
                {
                    var items = Company.Table<TaxonomyType>().OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if ( (type == "Lineage" || type == "TechnicalLineage") && (lowerColName == "source fusion configuration" || lowerColName == "target fusion configuration") )
                {
                    var items = Company.Table<Fusion>().OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }

                document.AutoFitColumn(1, i + 1);
            }

            #endregion

            document.HideWorksheet("Lookups");

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", "Load.xlsx");
        }

        private bool isRequiredColumn(string type, int id, string columnName, string parentColumnName)
        {
            columnName = columnName.ToLower();

            var required = true;

            if (type == "ArtifactType" && (columnName != "name" && columnName != "subject area" && columnName != "description" && columnName != parentColumnName))
                required = false;
            else if (type == "ReferenceItemType" && (columnName != "code"))
                required = false;
            else if (type == "Lineage" && (columnName == "source fusion configuration" || columnName == "target fusion configuration" || columnName == "source fusion path" || columnName == "target fusion path"))
                required = false;

            if (type == "IntersectType")
            {
                required = true; //All fields are required.
            }
            if (type == "TechnicalLineage")
            {
                required = (columnName != "group"); //All fields except Group are required.
            }
            else if (type == "Synonym")
            {
                required = true; //All fields are required.
            }


            if (id == 0)
            {
                if (
                    columnName == "item type" ||
                    columnName == "subject area" ||
                    columnName == "item path" ||
                    columnName == "responsibility" ||
                    columnName == "resource"
                    )
                {
                    required = true;
                }
            }

            return required;
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

        [ValidateHttpAntiForgeryToken, HttpPost, Route("AddLoad")]
        public JsonResult AddLoad(LoadFilePostModel model)
        {
            try
            {
                // Perform checks to make sure fields are populated.
                if (string.IsNullOrEmpty(model.Type)) throw new NoFormDataException("Type");
                if (string.IsNullOrEmpty(model.LoadAction)) throw new NoFormDataException("LoadAction");

                var match = MimeTypeExtensionsMap.RegEx.Match(model.File);

                var mime = match.Groups["mime"].Value;
                var encoding = match.Groups["encoding"].Value;
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

                        var fieldTypeNames = getFieldNamesByType(load.Object, load.ObjectID);

                        fieldTypeNames = fieldTypeNames.Select(i => i.Trim()).ToList();

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

                        //spreadsheet should not have more columns than the type has
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

                                if (!fieldTypeNames.Any(x => x == columnName))
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
#if DEBUG
                    // use bulkloaddev queue to debug bulk load web job
                    Company.Enqueue(QueueType.BulkLoadDev, new BulkLoadInfo { CompanyID = Company.CurrentCompanyID, LoadID = load.ID, To = QueueAction.BulkLoad });
#else
                    // regular production queue
                    Company.Enqueue(QueueType.BulkLoad, new BulkLoadInfo { CompanyID = Company.CurrentCompanyID, LoadID = load.ID, To = QueueAction.BulkLoad });
#endif

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
            if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var type = Company.GetById<LookupType>(id);

            list.Add(new EditableField { FieldName = "LookupTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.LookupType, id).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
        [Route("Lookup_DeleteFields"), NonNullableParameters]
        public JsonResult Lookup_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Lookup>(id);

            if (!Company.HasPermission(SystemObjects.LookupType, a.LookupTypeID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
        [Route("Lookup_EditFields"), NonNullableParameters]
        public JsonResult Lookup_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Lookup>(id);

            if (!Company.HasPermission(SystemObjects.LookupType, a.LookupTypeID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.LookupType, a.LookupTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Lookup, id).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post
        
        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddLookup")]
        public JsonResult AddLookup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup");

                int typeID = parseIntField(form, "LookupTypeID");
                var type = Company.GetById<LookupType>(typeID);

                if (type == null) throw new NotFoundException("lookup type");

                if (!Company.HasPermission(SystemObjects.LookupType, typeID, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new Lookup
                {
                    LookupTypeID = typeID
                };

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Lookup, a.ID, Company.GetFieldTypesByObject(SystemObjects.LookupType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate<Lookup>(a, fields);

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

        [HttpDelete, Route("DeleteLookup")]
        public JsonResult DeleteLookup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Lookup");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Lookup>(id);
                if (model == null) throw new NotFoundException("Lookup");

                if (!Company.HasPermission(SystemObjects.LookupType, model.LookupTypeID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<Lookup>(model);

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

                if (!Company.HasPermission(SystemObjects.LookupType, model.LookupTypeID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Lookup, model.ID, Company.GetFieldTypesByObject(SystemObjects.LookupType, model.LookupTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<Lookup>(model, fields);

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

        #region Field Generation

        [Route("LookupType_AddFields")]
        public JsonResult LookupType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.LookupType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupTypeID</param>
        [Route("LookupType_DeleteFields"), NonNullableParameters]
        public JsonResult LookupType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupTypeID</param>
        [Route("LookupType_EditFields"), NonNullableParameters]
        public JsonResult LookupType_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LookupType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public class LookupTypeModel
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        [HttpPost, ValidateInput(false), Route("AddLookupTypeRaw")]
        public JsonResult AddLookupTypeRaw(LookupTypeModel lookup)
        {
            var form = new FormCollection();
            form.Add("Name", lookup.Name);            

            return AddLookupType(form);
        }

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddLookupType")]
        public JsonResult AddLookupType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LookupType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("lookup type");

                var a = new LookupType
                {
                    Name = parseTextField(form, "Name")
                };

                Company.Add<LookupType>(a);

                if (a.ID > 0)
                {
                    Company.Add<FieldType>(new FieldType
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
                        IsEditable = true
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

                if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<LookupType>(i => i.ID == id);

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

                if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");

                Company.Update<LookupType>(model);

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

        #region MapRule

        #region Field Generation

        [Route("MapRule_DeleteFields"), NonNullableParameters]
        public JsonResult MapRule_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            // if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Delete))
            //   return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("MapRule_EditFields"), NonNullableParameters]
        public JsonResult MapRule_EditFields(int id)
        {
            var a = Company.GetById<MapRule>(id);
            if (a == null) throw new Exception("Error cannot find rule.");

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Transformation", Name = "Transformation", FieldType = DataType.Html.ToString(), Value = a.Transformation });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("MapRule_AddFields")]
        public JsonResult MapRule_AddFields()
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Transformation", Name = "Transformation", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddMapRule")]
        public JsonResult AddMapRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var transformation = parseTextField(form, "Transformation");

                var model = new MapRule
                {
                    Transformation = transformation
                };

                Company.SaveOrUpdate<MapRule>(model);

                return jsonSuccess("successfully created rule.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteMapRule")]
        public JsonResult DeleteMapRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var mapRuleID = parseIntField(form, "ID");

                var model = Company.GetById<MapRule>(mapRuleID);
                if (model == null) throw new NotFoundException("configuration");

                //delete the map rule item map rule record
                Company.Query<int>(@"delete MapRuleItemMapRule where MapRuleID = @id", new { id = model.ID });
                Company.Query<int>(@"delete MapRuleMap where MapRuleID = @id", new { id = model.ID });

                //delete the map rule item
                Company.Delete<MapRule>(model);

                return jsonSuccess("Rule successfully removed.", model.ID.ToString(), "delete", HttpStatusCode.OK);
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

        [ValidateHttpAntiForgeryToken, HttpPut, ValidateInput(false), Route("EditMapRule")]
        public JsonResult EditMapRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("rule");

                var model = Company.GetById<MapRule>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("rule");

                var transformation = parseTextField(form, "Transformation");
                model.Transformation = transformation;

                Company.SaveOrUpdate<MapRule>(model);

                return jsonSuccess("Successfully updated rule.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        #region MapRuleItem

        #region Field Generation

        [Route("MapRuleItem_DeleteFields"), NonNullableParameters]
        public JsonResult MapRuleItem_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            // if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Delete))
            //   return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("MapRuleItem_EditFields"), NonNullableParameters]
        public JsonResult MapRuleItem_EditFields(int id)
        {
            var a = Company.GetById<MapRuleItem>(id);
            if (a == null) throw new Exception("Error cannot find technical mapping.");

            var list = new List<EditableField>();

            var types = Company.Filter<Artifact>(i => i.ArtifactType.CanOwnFusion == true).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
            types.Insert(0, new SelectListItem { Text = "", Value = "" });

            var rules = Company.MapRules.OrderBy(x => x.Transformation).AsEnumerable().Select(i => new SelectListItem { Text = string.Format("ID:{0} - Transformation Name:{1}", i.ID, i.Transformation ?? "N/A"), Value = i.ID.ToString(), Selected = a.MapRules.Any(c => c.ID == i.ID) }).ToList();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "SourceArtifact", Name = "Source Artifact", FieldType = DataType.Lookup.ToString(), Items = types, Value = (a.SourceOwner == "Artifact" ? a.SourceOwnerID.ToString() : "") });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "SourceFusionAttribute", Name = "Source Fusion Attribute", FieldType = DataType.Text.ToString(), Value = a.SourceFusionAttribute.TextPath, TypeaheadUri = "/api/fusion/textpathautocomplete" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "TargetArtifact", Name = "Target Artifact", FieldType = DataType.Lookup.ToString(), Items = types, Value = (a.TargetOwner == "Artifact" ? a.TargetOwnerID.ToString() : "") });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "TargetFusionAttribute", Name = "Target Fusion Attribute", FieldType = DataType.Text.ToString(), Value = a.TargetFusionAttribute.TextPath, TypeaheadUri = "/api/fusion/textpathautocomplete" });

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TargetRule", Name = "Map Rule", FieldType = DataType.Lookup.ToString(), Items = rules, MultiSelect = true });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("MapRuleItem_AddFields"), NonNullableParameters]
        public JsonResult MapRuleItem_AddFields(int id)
        {
            var list = new List<EditableField>();

            var types = Company.Filter<Artifact>(i => i.ArtifactType.CanOwnFusion == true).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            var rules = Company.MapRules.OrderBy(x => x.Transformation).AsEnumerable().Select(i => new SelectListItem { Text = string.Format("ID:{0} - Transformation Name:{1}", i.ID, i.Transformation ?? "N/A"), Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "SourceArtifact", Name = "Source Artifact", FieldType = DataType.Lookup.ToString(), Items = types });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "SourceFusionAttribute", Name = "Source Fusion Attribute", FieldType = DataType.Text.ToString(), TypeaheadUri = "/api/fusion/textpathautocomplete" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "TargetArtifact", Name = "Target Artifact", FieldType = DataType.Lookup.ToString(), Items = types });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "TargetFusionAttribute", Name = "Target Fusion Attribute", FieldType = DataType.Text.ToString(), TypeaheadUri = "/api/fusion/textpathautocomplete" });

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TargetRule", Name = "Map Rule", FieldType = DataType.Lookup.ToString(), Items = rules, MultiSelect = true, Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddMapRuleItem")]
        public JsonResult AddMapRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                                
                var existingRuleArray = parseTextField(form, "TargetRule").Split(',').Select(Int32.Parse).ToList();

                if (!existingRuleArray.Any()) throw new Exception("Invalid Rule");

                var sourceFusionTextPath = parseTextField(form, "SourceFusionAttribute");
                var targetFusionTextPath = parseTextField(form, "TargetFusionAttribute");

                var sourceFusionAttribute = Company.Filter<FusionAttribute>(i => i.TextPath == sourceFusionTextPath).FirstOrDefault();
                var targetFusionAttribute = Company.Filter<FusionAttribute>(i => i.TextPath == targetFusionTextPath).FirstOrDefault();

                if (sourceFusionAttribute == null || targetFusionAttribute == null) throw new Exception("Invalid fusion textpath specified");

                var model = new MapRuleItem
                {
                    SourceOwner = "Artifact",
                    SourceOwnerID = parseIntField(form, "SourceArtifact"),
                    SourceFusionAttributeID = sourceFusionAttribute.ID,
                    TargetOwner = "Artifact",
                    TargetOwnerID = parseIntField(form, "TargetArtifact"),
                    TargetFusionAttributeID = targetFusionAttribute.ID,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                Company.SaveOrUpdate<MapRuleItem>(model);

                foreach (var rule in existingRuleArray)
                {
                    // add mapping
                    Company.Query<int>(@"insert [dbo].[mapruleitemmaprule] (mapruleid,mapruleitemid) values(@ruleId, @itemId)", new { itemId = model.ID, ruleId = rule });
                }                

                return jsonSuccess("successfully created mapping.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteMapRuleItem")]
        public JsonResult DeleteMapRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var mapRuleItemId = parseIntField(form, "ID");

                var model = Company.GetById<MapRuleItem>(mapRuleItemId);
                if (model == null) throw new NotFoundException("configuration");

                //delete the map rule item map rule record
                Company.Query<int>(@"delete MapRuleItemMapRule where MapRuleItemID = @id", new { id = model.ID });
                Company.Query<int>(@"delete MapRuleItemMapItem where MapRuleItemID = @id", new { id = model.ID });

                //delete the map rule item
                Company.Delete<MapRuleItem>(model);
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

        [ValidateHttpAntiForgeryToken, HttpPut, ValidateInput(false), Route("EditMapRuleItem")]
        public JsonResult EditMapRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var model = Company.GetById<MapRuleItem>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("configuration");

                var existingRuleArray = parseTextField(form, "TargetRule").Split(',').Select(Int32.Parse).ToList();

                if (!existingRuleArray.Any()) throw new Exception("Invalid Rule");

                var sourceFusionTextPath = parseTextField(form, "SourceFusionAttribute");
                var targetFusionTextPath = parseTextField(form, "TargetFusionAttribute");

                var sourceFusionAttribute = Company.Filter<FusionAttribute>(i => i.TextPath == sourceFusionTextPath).FirstOrDefault();
                var targetFusionAttribute = Company.Filter<FusionAttribute>(i => i.TextPath == targetFusionTextPath).FirstOrDefault();

                if (sourceFusionAttribute == null || targetFusionAttribute == null) throw new Exception("Invalid fusion textpath specified");

                var sourceArtifactID = parseNullableIntField(form, "SourceArtifact");

                if (sourceArtifactID.HasValue)
                {
                    model.SourceOwner = "Artifact";
                    model.SourceOwnerID = sourceArtifactID.GetValueOrDefault();
                }

                model.SourceFusionAttributeID = sourceFusionAttribute.ID;

                var targetArtifactID = parseNullableIntField(form, "TargetArtifact");
                
                if (targetArtifactID.HasValue)
                {                    
                    model.TargetOwner = "Artifact";
                    model.TargetOwnerID = targetArtifactID.GetValueOrDefault();
                }
                                
                model.TargetFusionAttributeID = targetFusionAttribute.ID;
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;
                
                Company.SaveOrUpdate<MapRuleItem>(model);

                //delete old mapruleitemmaprule records
                //Company.Query<int>(@"delete [dbo].[mapruleitemmaprule] where [mapruleitemid] = @id", new { id = model.ID });

                //add new ones
                //foreach (var rule in existingRuleArray)
                //{
                //    // add mapping
                //    Company.Query<int>(@"insert [dbo].[mapruleitemmaprule] (mapruleid,mapruleitemid) values(@ruleId, @itemId)", new { itemId = model.ID, ruleId = rule });
                //}

                return jsonSuccess("Successfully updated rule item.", model.ID.ToString(), "add", HttpStatusCode.Created);
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
        public JsonResult Organization_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "AdministratorEmail", Name = "Administrator Email", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true,"", 1, 250) });

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

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = Server.HtmlDecode(a.Name), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "AdministratorEmail", Name = "Administrator Email", FieldType = DataType.Text.ToString(), Value = a.AdministratorEmail, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ArtifactID</param>
        [Route("Organization_DeleteFields"), NonNullableParameters]
        public JsonResult Organization_DeleteFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), ActionName("Organization"), Route("Organization")]
        public JsonResult PostOrganization(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new Organization
                {
                    Name = parseTextField(form, "Name"),
                    AdministratorEmail = parseTextField(form, "AdministratorEmail")
                };

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);


                if (!regex.IsMatch(a.AdministratorEmail))
                    return jsonException("The email you entered is not valid", HttpStatusCode.Forbidden);

                Company.Add(a);

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

        [ValidateHttpAntiForgeryToken, HttpPut, ActionName("Organization"), Route("Organization")]
        public JsonResult PutOrganization(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var existing = Company.GetById<Organization>(id);
                if (existing == null) throw new NotFoundException("organization");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                existing.Name = parseTextField(form, "Name");
                existing.AdministratorEmail = parseTextField(form, "AdministratorEmail");

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);


                if (!regex.IsMatch(existing.AdministratorEmail))
                    return jsonException("The email you entered is not valid", HttpStatusCode.Forbidden);


                Company.Update(existing);

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

        [ValidateHttpAntiForgeryToken, HttpDelete, ActionName("Organization"), Route("Organization"), NonNullableParameters]
        public JsonResult DeleteOrganization(int id)
        {
            try
            {
                var model = Company.GetById<Organization>(id);
                if (model == null) throw new NotFoundException("organization");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);


                //get child records
                var domains = Company.Filter<OrganizationDomain>(i => i.OrganizationID == model.ID);
                var invitations = Company.Filter<OrganizationInvitation>(i => i.OrganizationID == model.ID);
                var resources = Company.Filter<OrganizationResource>(i => i.OrganizationID == model.ID);
                var registrations = Company.Filter<OrganizationRegistration>(i => i.OrganizationID == model.ID);


                Company.OrganizationDomains.RemoveRange(domains);
                Company.OrganizationInvitations.RemoveRange(invitations);
                Company.OrganizationResources.RemoveRange(resources);
                Company.OrganizationRegistrations.RemoveRange(registrations);

                Company.Organizations.Remove(model);

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


        /// <param name="id">ID</param>
        [Route("Contract_DeleteFields"), NonNullableParameters]
        public JsonResult Contract_DeleteFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), ActionName("Contract"), Route("Contract")]
        public JsonResult PostContract(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var o = new Contract
                {
                    OrganizationID = parseIntField(form, "OrganizationID"),
                    Body = parseTextField(form, "Body"),
                    ContractType = parseEnumField<ContractType>(form, "ContractType"),
                    Title = parseTextField(form, "Title")
                };

                if (o.OrganizationID == 0) o.OrganizationID = null;

                Company.Add(o);

                dynamic custom = new
                {
                    title = o.Title,
                    action = "add"
                };

                return jsonSuccess($"{o.ContractType.GetDisplayName()} contract successfully created.", o.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [ValidateHttpAntiForgeryToken, HttpPut, ActionName("Contract"), Route("Contract")]
        public JsonResult PutContract(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var o = Company.GetById<Contract>(id);
                if (o == null) throw new NotFoundException("contract");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                o.Body = parseTextField(form, "Body");
                o.ContractType = parseEnumField<ContractType>(form, "ContractType");
                o.Title = parseTextField(form, "Title");

                Company.Update(o);

                dynamic custom = new
                {
                    title = o.Title,
                    action = "edit"
                };

                return jsonSuccess($"{o.ContractType.GetDisplayName()} contract successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [ValidateHttpAntiForgeryToken, HttpDelete, ActionName("Contract"), Route("Contract"), NonNullableParameters]
        public JsonResult DeleteContract(int id)
        {
            try
            {
                var o = Company.GetById<Contract>(id);
                if (o == null) throw new NotFoundException("contract");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(o);

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

        /// <param name="id">ID</param>
        [Route("OrganizationDomain_DeleteFields"), NonNullableParameters]
        public JsonResult OrganizationDomain_DeleteFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), ActionName("OrganizationDomain"), Route("OrganizationDomain")]
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

        [ValidateHttpAntiForgeryToken, HttpPut, ActionName("OrganizationDomain"), Route("OrganizationDomain")]
        public JsonResult PutOrganizationDomain(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var existing = Company.GetById<OrganizationDomain>(id);
                if (existing == null) throw new NotFoundException("organization domain");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

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

        [ValidateHttpAntiForgeryToken, HttpDelete, ActionName("OrganizationDomain"), Route("OrganizationDomain"), NonNullableParameters]
        public JsonResult DeleteOrganizationDomain(int id)
        {
            try
            {
                var model = Company.GetById<OrganizationDomain>(id);
                if (model == null) throw new NotFoundException("organization domain");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

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

        /// <param name="id">Organization Invitation ID</param>
        [Route("OrganizationInvitation_DeleteFields"), NonNullableParameters]
        public JsonResult OrganizationInvitation_DeleteFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), ActionName("OrganizationInvitation"), Route("OrganizationInvitation")]
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

        [ValidateHttpAntiForgeryToken, HttpPut, ActionName("OrganizationInvitation"), Route("OrganizationInvitation")]
        public JsonResult PutOrganizationInvitation(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var existing = Company.GetById<OrganizationInvitation>(id);
                if (existing == null) throw new NotFoundException("organization invitation");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

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

        [ValidateHttpAntiForgeryToken, HttpDelete, ActionName("OrganizationInvitation"), Route("OrganizationInvitation"), NonNullableParameters]
        public JsonResult DeleteOrganizationInvitation(int id)
        {
            try
            {
                var model = Company.GetById<OrganizationInvitation>(id);
                if (model == null) throw new NotFoundException("organization invitation");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

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

        #region Policy

        #region Field Generation

        [Route("Policy_AddFields"), NonNullableParameters]
        public JsonResult Policy_AddFields(int typeID, int? parentID)
        {
            var model = new Policy();
            if (!Company.HasPermission(SystemObjects.Policy, 0, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var statuses = PolicyStatus.Active.GetStatusEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "PolicyTypeID", FieldType = DataType.Hidden.ToString(), Value = typeID.ToString() });
            if (parentID.HasValue) list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = parentID.Value.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = $"form/Policy_SimilarItems?typeID={typeID}&query=" });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Status", Name = FieldInfo.RuleStatus_Name, FieldDescription = FieldInfo.RuleStatus_Description, Items = statuses, FieldType = DataType.Lookup.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.PolicyType, typeID).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyID</param>
        [Route("Policy_DeleteFields"), NonNullableParameters]
        public JsonResult Policy_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyID</param>
        [Route("Policy_EditFields"), NonNullableParameters]
        public JsonResult Policy_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var model = Company.GetById<Policy>(id);

            var statuses = PolicyStatus.Active.GetStatusEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Status", Name = FieldInfo.RuleStatus_Name, FieldDescription = FieldInfo.RuleStatus_Description, Items = statuses, FieldType = DataType.Lookup.ToString(), Value = ((int)model.Status).ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = model.Description });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Policy, id).ToList(), 5, true);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Policy_SimilarItems"), NonNullableParameters]
        public JsonNetResult Policy_SimilarItems(int typeID, string query)
        {
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.SimilarItems, new { type = "Policy", typeID, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddPolicy")]
        public JsonResult AddPolicy(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Policy, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("Policy");

                var model = new Policy
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    Status = (PolicyStatus)Enum.Parse(typeof(PolicyStatus), form["Status"]),
                    PolicyTypeID = parseIntField(form, "PolicyTypeID")
                };

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    model.ParentID = parseIntField(form, "ParentID");
                    if (model.ParentID == 0) model.ParentID = null;
                }
                Company.Add<Policy>(model);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Policy, model.ID, Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), form, Server);
                Company.AddOrUpdateFields(fields);

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

        [HttpDelete, Route("DeletePolicy")]
        public JsonResult DeletePolicy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Policy");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Policy>(id);
                if (model == null) throw new NotFoundException("Policy");

                if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    Name = model.Name,
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

                if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.Status = (PolicyStatus)Enum.Parse(typeof(PolicyStatus), form["Status"]);

                Company.Update<Policy>(model);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Policy, model.ID, Company.GetFieldTypesByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

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

        #endregion

        #region PolicyType

        #region Field Generation

        [Route("PolicyType_AddFields")]
        public JsonResult PolicyType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.PolicyType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = new PolicyType();
            var classes = Company.Table<PolicyTypeClass>().OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Class", Name = a.GetName(i => i.PolicyTypeClassID), FieldDescription = a.GetDescription(i => i.PolicyTypeClassID), FieldType = DataType.Lookup.ToString(), Items = classes });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MaximumDepth", Name = a.GetName(i => i.MaximumDepth), RangeMin = 1, RangeMax = 25, FieldDescription = a.GetDescription(i => i.MaximumDepth), FieldType = DataType.Number.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });
            loadIconFields(list, 4);

            a = null;
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeID</param>
        [Route("PolicyType_DeleteFields"), NonNullableParameters]
        public JsonResult PolicyType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<PolicyType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeID</param>
        [Route("PolicyType_EditFields"), NonNullableParameters]
        public JsonResult PolicyType_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<PolicyType>(id);
            var style = Company.GetObjectStyle(SystemObjects.PolicyType, id);
            var classes = Company.Table<PolicyTypeClass>().OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Class", Name = a.GetName(i => i.PolicyTypeClassID), FieldDescription = a.GetDescription(i => i.PolicyTypeClassID), FieldType = DataType.Lookup.ToString(), Value = a.PolicyTypeClassID.ToString(), Items = classes });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MaximumDepth", Name = a.GetName(i => i.MaximumDepth), RangeMin = 1, RangeMax = 25, FieldDescription = a.GetDescription(i => i.MaximumDepth), FieldType = DataType.Number.ToString(), Value = ((a.MaximumDepth.HasValue) ? a.MaximumDepth.Value.ToString() : "1") });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });
            loadIconFields(list, 4, style);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddPolicyType")]
        public JsonResult AddPolicyType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.PolicyType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("policy type");

                var a = new PolicyType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    MaximumDepth = parseIntField(form, "MaximumDepth"),
                    PolicyTypeClassID = parseIntField(form, "Class")
                };

                if (a.MaximumDepth <= 0 || a.MaximumDepth > 10) return jsonException("Invalid Maximum Policy level specified must be a value between 1 and 10", HttpStatusCode.InternalServerError);

                Company.Add<PolicyType>(a);

                for (int i = 1; i <= a.MaximumDepth; i++)
                {
                    Company.Set<PolicyTypeLevel>().Add(new PolicyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), PolicyTypeID = a.ID });
                }
                Company.SaveChanges();

                upsertObjectStyle(SystemObjects.PolicyType, a.ID, form, a.Name);

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

        [HttpDelete, Route("DeletePolicyType")]
        public JsonResult DeletePolicyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("policy type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<PolicyType>(id);
                if (model == null) throw new NotFoundException("policy type");

                if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<PolicyType>(i => i.ID == id);
                deleteObjectStyle(SystemObjects.PolicyType, id);

                

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

        [HttpPut, ValidateInput(false), Route("EditPolicyType")]
        public JsonResult EditPolicyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("policy type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<PolicyType>(id);
                if (model == null) throw new NotFoundException("policy type");

                var style = Company.GetObjectStyle(SystemObjects.PolicyType, id);

                if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.MaximumDepth = parseIntField(form, "MaximumDepth");
                model.PolicyTypeClassID = parseIntField(form, "Class");

                if (model.MaximumDepth <= 0 || model.MaximumDepth > 10) return jsonException("Invalid Maximum Policy level specified must be a value between 1 and 10", HttpStatusCode.InternalServerError);

                var currentMaxLevel = Company.Query<int>("select coalesce(max([Level]), 0) from Policy where PolicyTypeID = @t", new { t = id }).SingleOrDefault();

                if (currentMaxLevel > model.MaximumDepth)
                    throw new InvalidFieldException(d360.core.resources.Fields.MaximumDepth_Name, "less than the current maximum depth of " + currentMaxLevel);

                Company.Update<PolicyType>(model);

                for (int i = 1; i <= model.MaximumDepth; i++)
                {
                    var level = model.PolicyTypeLevels.SingleOrDefault(l => l.Level == i);
                    if (level == null)
                    {
                        Company.Set<PolicyTypeLevel>().Add(new PolicyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), PolicyTypeID = model.ID });
                    }
                }
                Company.SaveChanges();

                Company.Delete<PolicyTypeLevel>(l => l.Level > model.MaximumDepth);

                upsertObjectStyle(SystemObjects.PolicyType, model.ID, form, model.Name);

                

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

        #region PolicyTypeClass

        #region Field Generation

        [Route("PolicyTypeClass_AddFields")]
        public JsonResult PolicyTypeClass_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.PolicyTypeClass, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = new PolicyTypeClass();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            a = null;
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeClassID</param>
        [Route("PolicyTypeClass_DeleteFields"), NonNullableParameters]
        public JsonResult PolicyTypeClass_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.PolicyTypeClass, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<PolicyTypeClass>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeClassID</param>
        [Route("PolicyTypeClass_EditFields"), NonNullableParameters]
        public JsonResult PolicyTypeClass_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.PolicyTypeClass, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<PolicyTypeClass>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddPolicyTypeClass")]
        public JsonResult AddPolicyTypeClass(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.PolicyTypeClass, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("policy class");

                var a = new PolicyTypeClass
                {
                    Name = parseTextField(form, "Name")
                };

                Company.Add<PolicyTypeClass>(a);

                

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

        [HttpDelete, Route("DeletePolicyTypeClass")]
        public JsonResult DeletePolicyTypeClass(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("policy class");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<PolicyTypeClass>(id);
                if (model == null) throw new NotFoundException("policy class");

                if (!Company.HasPermission(SystemObjects.PolicyTypeClass, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<PolicyTypeClass>(i => i.ID == id);
                
                

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

        [HttpPut, ValidateInput(false), Route("EditPolicyTypeClass")]
        public JsonResult EditPolicyTypeClass(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("policy class");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<PolicyTypeClass>(id);
                if (model == null) throw new NotFoundException("policy class");

                if (!Company.HasPermission(SystemObjects.PolicyTypeClass, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");

                Company.Update<PolicyTypeClass>(model);

                

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

        #region PolicyTypeLevel

        #region Field Generation

        [Route("PolicyTypeLevel_AddFields"), NonNullableParameters]
        public JsonResult PolicyTypeLevel_AddFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var type = Company.GetById<PolicyType>(id);
            if (type == null) return jsonException("Type not found.", HttpStatusCode.NotFound);
            var existingLevels = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == id).Select(i => i.Level).ToList();

            var levels = new List<SelectListItem>();
            for (int i = 1; i <= type.MaximumDepth; i++)
            {
                if (!existingLevels.Contains(i)) levels.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });
            }

            var list = new List<EditableField>();
            var a = new PolicyTypeLevel();

            list.Add(new EditableField { Required = true, FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField
            {
                Group = "",
                Row = 1,
                Column = 2,
                Required = true,
                FieldName = "Level",
                Name = a.GetName(i => i.Level),
                Items = levels,
                FieldDescription = a.GetDescription(i => i.Level),
                FieldType = DataType.Lookup.ToString(),
                Validations = checkAndAddValidation("Text", "Level", true, "", 1, 250)
            });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            a = null;
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeID</param>
        [Route("PolicyTypeLevel_DeleteFields"), NonNullableParameters]
        public JsonResult PolicyTypeLevel_DeleteFields(int id, int level)
        {
            if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { FieldName = "Level", FieldType = DataType.Hidden.ToString(), Value = level.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeID</param>
        [Route("PolicyTypeLevel_EditFields"), NonNullableParameters]
        public JsonResult PolicyTypeLevel_EditFields(int id, int level)
        {
            if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == id && i.Level == level).SingleOrDefault();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.PolicyTypeID.ToString() });
            list.Add(new EditableField { ReadOnly = true, FieldName = "Level", FieldType = DataType.Hidden.ToString(), Value = a.Level.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddPolicyTypeLevel")]
        public JsonResult AddPolicyTypeLevel(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Create, ClaimObject.Root))
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

        [HttpDelete, Route("PolicyType/{policyTypeId:int}/levels/{policyTypeLevelId:int}")]
        public JsonResult DeletePolicyTypeLevel(int policyTypeId, int policyTypeLevelId)
        {
            try
            {                
                var id = policyTypeId;
                var level = policyTypeLevelId;

                if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Delete))
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

                if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Update))
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
            if (!Company.HasPermission(SystemObjects.Predicate, 0, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Functional Type", FieldType = DataType.Lookup.ToString(), Items = PredicateType.Lineage.GetAsList().Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name }).ToList() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PredicateID</param>
        [Route("Predicate_DeleteFields"), NonNullableParameters]
        public JsonResult Predicate_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Predicate>(id);
            if (!Company.HasPermission(SystemObjects.Predicate, id, Claim.Delete))
                return jsonException("You do not have permissions to delete this.", HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PredicateID</param>
        [Route("Predicate_EditFields"), NonNullableParameters]
        public JsonResult Predicate_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Predicate>(id);
            var any = Company.Any<IntersectType>(i => i.PredicateID == id);
            if (!Company.HasPermission(SystemObjects.Predicate, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Value = a.Inverse, Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            list.Add(new EditableField { ReadOnly=any, Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Functional Type", FieldType = DataType.Lookup.ToString(), Value = ((int)a.Type).ToString(), Items = PredicateType.Lineage.GetAsList().Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name }).ToList() });
            
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddPredicate")]
        public JsonResult AddPredicate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("predicate");

                if (!Company.HasPermission(SystemObjects.Predicate, 0, Claim.Update))
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

                if (!Company.HasPermission(SystemObjects.Predicate, model.ID, Claim.Delete))
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

                if (!Company.HasPermission(SystemObjects.Predicate, model.ID, Claim.Update))
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

        #region Relationship

        #region Field Generation

        /// <param name="it">IntersectTypeID</param>
        /// <param name="type">Object</param>
        /// <param name="id">ObjectID</param>
        [Route("Relationship_AddFields"), NonNullableParameters]
        public JsonResult Relationship_AddFields(int it, SystemObjects type, int id, bool isNg = false)
        {
            if (!Company.HasPermission(type, id, Claim.Create, ClaimObject.Relationship))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var relationshipType = Company.GetById<IntersectType>(it, i => i.Predicate);
            var obj = Company.GetObjectDetail(type, id);

            if (obj == null || relationshipType == null)
            {
                return jsonException("Invalid relationship type or source item.", HttpStatusCode.NotFound);
            }

            var targetType = "";
            var targetTypeID = 0;
            if (relationshipType.Subject == obj.Type && relationshipType.SubjectID == obj.TypeID)
            {
                targetType = relationshipType.Object;
                targetTypeID = relationshipType.ObjectID;
            }
            else
            {
                targetType = relationshipType.Subject;
                targetTypeID = relationshipType.SubjectID;
            }

            list.Add(new EditableField { FieldName = "IntersectTypeID", FieldType = DataType.Hidden.ToString(), Value = it.ToString() });
            list.Add(new EditableField { FieldName = "Source", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
            list.Add(new EditableField { FieldName = "SourceID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            #region

            var sql = "";

            switch (targetType)
            {
                case "FusionAttributeType":
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
		select	SubjectID
		from	[IntersectDetail] N
				inner join Artifact A with(nolock) on N.[Subject] = 'Artifact' and A.ID = N.SubjectID and N.ID = @id
				inner join ArtifactType [AT] with(nolock) on [AT].ID = A.ArtifactTypeID and [AT].CanOwnFusion = 1
	insert into @owners
		select	ObjectID
		from	[IntersectDetail] N
				inner join Artifact A with(nolock) on N.[Object] = 'Artifact' and A.ID = N.ObjectID and N.ID = @id
				inner join ArtifactType [AT] with(nolock) on [AT].ID = A.ArtifactTypeID and [AT].CanOwnFusion = 1
END
ELSE
BEGIN
	set @OwnerSourceType = @source
	insert into @owners values (@id)
END

declare @h table (ID int);

if @OwnerSourceType = 'Artifact'
	begin
		with h as	(
					select	A.ID,
							A.ParentID
					from	Artifact A with(nolock)
							inner join @owners O on O.ID = A.ID
					union all
					select	P.ID,
							P.ParentID
					from	Artifact P with(nolock)
							inner join h as C on C.ParentID = P.ID
					)
		insert into @h
			select ID from h;
	end
else
	begin
		insert into @h values (@id)
	end;

select	'FusionAttribute' as [Object], 
        FA.ID as ObjectID, 
        F.Name + '.' + FA.TextPath as Name
from	FusionAttribute FA with(nolock)
		inner join Fusion F with(nolock) on F.ID = FA.FusionID and FA.FusionAttributeTypeID = @targetTypeID and FA.Deleted = 0
        inner join FusionOwner FO on FO.FusionID = FA.FusionID
        inner join @h H on H.ID = FO.ArtifactID
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	IntersectTypeID = {it} and ( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) )
					)
order by F.Name, FA.TextPath";
                    }
                    break;
                case "Group":
                case "GroupType":
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
					where	IntersectTypeID = {it} and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'Group' and ObjectTypeID = 1) ) OR
							 ( (SubjectType = 'Group' and SubjectTypeID = 1) AND (Object = @source and ObjectID = @id) )
							)
					)
order by D.Name";
                    break;
                case "Resource":
                case "ResourceType":
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
					where	IntersectTypeID = {it} and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'ResourceType' and ObjectTypeID = 1) ) OR
							 ( (SubjectType = 'ResourceType' and SubjectTypeID = 1) AND (Object = @source and ObjectID = @id) )
							)
					)
order by D.LastName, D.FirstName";
                    break;
                default:
                    sql = $@"
select	D.[Object], 
        D.ObjectID, 
        D.TextPath as Name
from	cache.ObjectDetails D with(nolock)
		left join [IntersectDetail] I on	I.IntersectTypeID = {it} and (
											 ( (I.Subject = @source and I.SubjectID = @id) AND (I.Object = D.[Object] and I.ObjectID = D.ObjectID) ) OR
											 ( (I.Subject = D.[Object] and I.SubjectID = D.ObjectID) AND (I.Object = @source and I.ObjectID = @id) )
											)
where	D.[ObjectType] = @targetType and D.ObjectTypeID = @targetTypeID 
        and D.ObjectTypeID <> D.ObjectID 
        and D.ObjectTypeID <> 0
        and (D.[Object] + cast(D.ObjectID as varchar) <> @source + cast(@id as varchar))
        and I.ID is null
order by D.TextPath";
                    break;
            }

            #endregion

            if (isNg)
            {
                list.Add(new EditableField
                {
                    Row = 1,
                    Column = 1,
                    Required = true,
                    FieldName = "Items",
                    Name = "What Items Are You Relating?",                    
                    MultiSelect = true,                    
                    FieldType = DataType.DataTableSelect.ToString(),
                    TypeaheadUri = $"/form/Relationship_DataTable?intersectTypeId={it}&type={type}&objectId={id}"
                });
            }
            else
            {
                list.Add(new EditableField
                {
                    Row = 1,
                    Column = 1,
                    Required = true,
                    FieldName = "Items",
                    Name = "What Items Are You Relating?",
                    //DataUri = $"",
                    MultiSelect = true,
                    //FieldDescription = Resources.FieldInfo.,
                    FieldType = DataType.Lookup.ToString(),
                    Items = Company.Query<dynamic>(sql, new { targetType, targetTypeID, source = type.ToString(), id }).Select(i => new SelectListItem { Text = i.Name, Value = $"{i.Object}|{i.ObjectID}" }).ToList()
                });
            }

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IntersectType, it).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RelationshipID</param>
        [Route("Relationship_DeleteFields"), NonNullableParameters]
        public JsonResult Relationship_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Intersect, id, Claim.Delete, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RelationshipID</param>
        [Route("Relationship_EditFields"), NonNullableParameters]
        public JsonResult Relationship_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Intersect, id, Claim.Create, ClaimObject.Relationship))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var relationship = Company.GetById<Intersect>(id, i => i.IntersectType);

            if (relationship == null) return jsonException("Relationship not found.", HttpStatusCode.NotFound);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IntersectType, relationship.IntersectTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Intersect, relationship.ID).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddRelationship")]
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

                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), sourceObject.Type), sourceObject.TypeID, Claim.Create, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (relationshipType == null) throw new NotFoundException("relationship");

                var rawItems = parseTextField(form, "Items");
                if (string.IsNullOrEmpty(rawItems))
                    return jsonException("No selected items", HttpStatusCode.BadRequest);

                var items = rawItems.Split(',').ToList();

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

                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), intersect.IntersectType.Subject), intersect.IntersectType.SubjectID, Claim.Update, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), intersect.IntersectType.Object), intersect.IntersectType.ObjectID, Claim.Update, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Update<Intersect>(intersect);
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
                var obj = Company.GetObjectDetail(type, objectId);
                objectTypeID = obj.TypeID;
                parentType = obj.Type;
            }

            if (objectTypeID <= 0 || string.IsNullOrEmpty(parentType) || relationshipType == null)
            {
                return jsonException("Invalid relationship type or source item.", HttpStatusCode.NotFound);
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
		select	SubjectID
		from	[IntersectDetail] N
				inner join Artifact A with(nolock) on N.[Subject] = 'Artifact' and A.ID = N.SubjectID and N.ID = @id
				inner join ArtifactType [AT] with(nolock) on [AT].ID = A.ArtifactTypeID and [AT].CanOwnFusion = 1
	insert into @owners
		select	ObjectID
		from	[IntersectDetail] N
				inner join Artifact A with(nolock) on N.[Object] = 'Artifact' and A.ID = N.ObjectID and N.ID = @id
				inner join ArtifactType [AT] with(nolock) on [AT].ID = A.ArtifactTypeID and [AT].CanOwnFusion = 1
END
ELSE
BEGIN
	set @OwnerSourceType = @source
	insert into @owners values (@id)
END

declare @h table (ID int);

if @OwnerSourceType = 'Artifact'
	begin
		with h as	(
					select	A.ID,
							A.ParentID
					from	Artifact A with(nolock)
							inner join @owners O on O.ID = A.ID
					union all
					select	P.ID,
							P.ParentID
					from	Artifact P with(nolock)
							inner join h as C on C.ParentID = P.ID
					)
		insert into @h
			select ID from h;
	end
else
	begin
		insert into @h values (@id)
	end;

select	'FusionAttribute' as [Object], 
        FA.ID as ObjectID, 
        F.Name + '.' + FA.TextPath as Name
from	FusionAttribute FA with(nolock)
		inner join Fusion F with(nolock) on F.ID = FA.FusionID and FA.FusionAttributeTypeID = @targetTypeID and FA.Deleted = 0
        inner join FusionOwner FO on FO.FusionID = FA.FusionID
        inner join @h H on H.ID = FO.ArtifactID
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
from		cache.Object D
			left join [Intersect] I on	I.IntersectTypeID = @it and (
											( (I.Subject = @source and I.SubjectID = @id) AND (I.Object = D.[Object] and I.ObjectID = D.ObjectID) ) OR
											( (I.Subject = D.[Object] and I.SubjectID = D.ObjectID) AND (I.Object = @source and I.ObjectID = @id) )
										)
where		D.ObjectType = @targetType and D.ObjectTypeID = @targetTypeID 
			and D.ObjectType <> D.Object
			and I.ID is null
) C on C.ObjectID = O.ID";

                    switch (targetType)
                    {
                        case "ArtifactType":
                            sql = $@"select C.Object, C.ObjectID, O.TextPath + ' ( ' + T.Name + ' )' as Name from Artifact O inner join TaxonomyType T on T.ID = O.TaxonomyTypeID inner join {sql} order by O.TextPath + ' ( ' + T.Name + ' )'";
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
                        case "PolicyType":
                            sql = $@"select C.Object, C.ObjectID, O.TextPath as Name from [Policy] O inner join {sql} order by O.TextPath";
                            break;
                        case "ReferenceItemType":
                            sql = $@"select C.Object, C.ObjectID, O.Name from [ReferenceItemType] O inner join {sql} order by O.Name";
                            break;
                        case "ResourceType":
                            sql = $@"select C.Object, C.ObjectID, O.LastName + ', ' + O.FirstName as Name from reporting.[Global_Resource] O inner join {sql} order by O.LastName + ', ' + O.FirstName";
                            break;
                        case "RuleType":
                            sql = $@"select C.Object, C.ObjectID, O.Name from [Rule] O inner join {sql} order by O.Name";
                            break;
                        case "TaxonomyType":
                            sql = $@"select C.Object, C.ObjectID, O.TextPath as Name from Taxonomy O inner join {sql} order by O.TextPath";
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

        #region Field Generation

        /// <param name="id">ID of the object</param>
        [Route("Report_DeleteFields"), NonNullableParameters]
        public JsonResult Report_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Report, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Report>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("PowerBICredentials_AddFields")]
        public JsonResult PowerBICredentials_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.Report, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            
            list.Add(new EditableField { Row = 1, Column = 1, Name= "Username", FieldName = "Username", FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Name = "Password", FieldName = "Password", FieldType = DataType.Password.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddReport")]
        public async Task<JsonResult> AddReport(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var objectType = form["ObjectType"].Split('|').ToArray();

                if (objectType.Length == 2)
                {
                    var fileCount = HttpContext.Request.Files.Count;
                    var reportType = parseTextField(form, "ReportType");
                    var name = parseTextField(form, "Name");
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

                    Company.Add<Report>(model);

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

        [HttpDelete, Route("DeleteReport")]
        public JsonResult DeleteReport(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Report>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                //delete any power bi reports
                if(model.ReportType == "powerbi" && !string.IsNullOrEmpty(model.PowerBIDatasetID))
                {
                    var companySettings = Community.GetCompanySettings();
                    var workspaceCollectionName = string.Empty;
                    var workspaceId = string.Empty;
                    var accessKey = string.Empty;

                    companySettings.TryGetValue("PowerBIWorkspaceCollectionName", out workspaceCollectionName);
                    companySettings.TryGetValue("PowerBIWorkspaceId", out workspaceId);
                    companySettings.TryGetValue("PowerBIAccessKey", out accessKey);

                    if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(workspaceCollectionName))
                        throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");

                    PowerBI.DeleteDataset(accessKey, workspaceCollectionName, workspaceId, model.PowerBIDatasetID);
                }

                Company.Delete<Report>(model);

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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddPowerBICredentials")]
        public async Task<JsonResult> AddPowerBICredentials(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);
                
                //get username / password
                var user = parseTextField(form, "Username");
                var pwd = parseTextField(form, "Password");

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pwd))
                    throw new Exception("Please specify a valid username and password.");

                var companySettings = Community.GetCompanySettings();
                var workspaceCollectionName = string.Empty;
                var workspaceId = string.Empty;
                var accessKey = string.Empty;

                companySettings.TryGetValue("PowerBIWorkspaceCollectionName", out workspaceCollectionName);
                companySettings.TryGetValue("PowerBIWorkspaceId", out workspaceId);
                companySettings.TryGetValue("PowerBIAccessKey", out accessKey);

                // if the workspace id is null create a new one and update the companysettings
                workspaceId = await checkPowerBIValidWorkspace(workspaceId, accessKey, workspaceCollectionName);


                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(workspaceCollectionName))
                    throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");

                //save password in this workspace for all ds's
                await PowerBI.UpdateConnectionCredentials(accessKey, workspaceCollectionName, workspaceId, user, pwd);

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
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Report>(id);

                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                var fileCount = HttpContext.Request.Files.Count;
                var reportType = parseTextField(form, "ReportType");
                var name = parseTextField(form, "Name");
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

                    if (!string.IsNullOrEmpty(datasetID))
                        model.PowerBIDatasetID = datasetID;

                    if (!string.IsNullOrEmpty(powerBIID))
                        model.PowerBIReportID = powerBIID;

                    if (!string.IsNullOrEmpty(filename))
                        model.FileName = filename;

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

        private async Task<string> checkPowerBIValidWorkspace(string workspaceId, string accessKey, string workspaceCollectionName)
        {
            workspaceId = (workspaceId ?? "").Trim();

            if (string.IsNullOrEmpty(workspaceId) && !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(workspaceCollectionName))
            {
                var res = await PowerBI.CreateWorkspace(accessKey, workspaceCollectionName);

                var workspaceSetting = Community.Filter<CompanySetting>(i => i.SettingID == 15 && i.CompanyID == Company.CurrentCompanyID).FirstOrDefault();

                if (workspaceSetting == null)
                {
                    Community.Add<CompanySetting>(new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 15, Value = res.WorkspaceId });
                }
                else
                {
                    workspaceSetting.Value = res.WorkspaceId;

                    Community.Update<CompanySetting>(workspaceSetting);
                }

                return res.WorkspaceId;
            }

            return workspaceId;
        }

        private async Task<Microsoft.PowerBI.Api.V1.Models.Import> uploadPowerBIReport(HttpPostedFileBase file, string name, string datasetId = "")
        {
            var companySettings = Community.GetCompanySettings();
            var workspaceCollectionName = string.Empty;
            var workspaceId = string.Empty;
            var accessKey = string.Empty;

            companySettings.TryGetValue("PowerBIWorkspaceCollectionName", out workspaceCollectionName);
            companySettings.TryGetValue("PowerBIWorkspaceId", out workspaceId);
            companySettings.TryGetValue("PowerBIAccessKey", out accessKey);

            // if the workspace id is null create a new one and update the companysettings
            workspaceId = await checkPowerBIValidWorkspace(workspaceId, accessKey, workspaceCollectionName);
            

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(workspaceCollectionName))
                throw new Exception("ERROR : UNABLE TO FIND ALL POWER BI COMMUNITY SETTINGS.");

            // if an existing one exists delete it
            if (!string.IsNullOrEmpty(datasetId))
                await PowerBI.DeleteDataset(accessKey, workspaceCollectionName, workspaceId, datasetId);


            return await PowerBI.ImportPbix(accessKey, workspaceCollectionName, workspaceId, name, file.InputStream);
        }


        #endregion

        #endregion

        #region ReportTile

        #region Field Generation

        /// <param name="id">ID of the object</param>
        [Route("ReportTile_DeleteFields"), NonNullableParameters]
        public JsonResult ReportTile_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ReportTile>(id);

            if (!Company.HasPermission(SystemObjects.Report, a.ReportID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post
        
        [HttpPost, ValidateHttpAntiForgeryToken, ValidateInput(false), Route("AddReportTile")]
        public JsonResult AddReportTile(FormCollection form, bool isNg = false)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

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
                    Company.Add<ReportTile>(model);
                }
                else
                {
                    throw new InvalidFieldException("Command Text", "not a SELECT statement or recognized query.");
                }

                return jsonSuccess(Resources.FormInfo.Add_ReportTile_Confirmation, model.ID.ToString(), "add", HttpStatusCode.Created);
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
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReportTile>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);
                Company.Delete<ReportTile>(model);

                return jsonSuccess(Resources.FormInfo.Delete_ReportTile_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, isNg ? "ID" : "TileID");
                var model = Company.GetById<ReportTile>(id);

                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

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
                    Company.Update<ReportTile>(model);
                }
                else
                {
                    throw new InvalidFieldException("Command Text", "not a SELECT statement or recognized query.");
                }

                return jsonSuccess(Resources.FormInfo.Edit_ReportTile_Confirmation, id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Field Generation

        /// <param name="id">ResponsibilityID</param>
        [Route("Responsibility_DeleteFields"), NonNullableParameters]
        public JsonResult Responsibility_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Responsibility>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        List<ResponsibilityContextItem> getContextFormFieldForResponsibility(int responsibilityID, FormCollection form)
        {
            var contexts = new List<ResponsibilityContextItem>();

            if (form.AllKeys.Contains("Context"))
            {
                if (!string.IsNullOrEmpty(form["Context"]))
                {
                    var IDs = form["Context"].Split(',').Select(i => int.Parse(i)).ToList();
                    IDs.ForEach(id =>
                    {
                        contexts.Add(new ResponsibilityContextItem { ObjectID = id, ObjectType = "ReferenceItem", ResponsibilityID = responsibilityID });
                    });
                }
            }

            return contexts;
        }

        List<ResponsibilityContextItem> getContextFieldForResponsibility(int responsibilityID, List<ResponsibilityContextItem> contexts)
        {
            var ctx = new List<ResponsibilityContextItem>();

            if (contexts == null) return ctx;

            var IDs = contexts.Select(c => c.ObjectID).ToList();

            IDs.ForEach(id =>
            {
                ctx.Add(new ResponsibilityContextItem { ObjectID = id, ObjectType = "ReferenceItem", ResponsibilityID = responsibilityID });
            });

            return ctx;
        }

        void processContextFormFieldForResponsibility(int responsibilityID, FormCollection form, bool isAdding = true)
        {
            var contexts = getContextFormFieldForResponsibility(responsibilityID, form);

            if (!isAdding)
                Company.Delete<ResponsibilityContextItem>(i => i.ResponsibilityID == responsibilityID);

            if (contexts.Count > 0)
            {
                foreach (var o in contexts)
                {
                    Company.Set<ResponsibilityContextItem>().Add(o);
                }
                Company.SaveChanges();
            }
        }

        void processContextFieldForResponsibility(int responsibilityID, List<ResponsibilityContextItem> contexts, bool isAdding = true)
        {
            var ctx = getContextFieldForResponsibility(responsibilityID, contexts);
            if (!isAdding)
                Company.Delete<ResponsibilityContextItem>(i => i.ResponsibilityID == responsibilityID);

            if (ctx?.Count > 0)
            {
                foreach (var o in ctx)
                {
                    o.ResponsibilityID = responsibilityID;
                    Company.Set<ResponsibilityContextItem>().Add(o);
                }
                Company.SaveChanges();
            }
        }

        List<SelectListItem> getContextSelectList(List<int> contextIDs = null)
        {
            if (contextIDs == null) contextIDs = new List<int>();

            var sql = @"select	T.Name + ' : ' + I.DisplayValue as [Text], I.ID as Value, I.ID  
from	ReferenceItem I
		inner join ReferenceItemType T on T.ID = I.ReferenceItemTypeID
order by	T.Name, I.DisplayValue";

            return Company.Query<dynamic>(sql)
                .ToList()
                .Select(i => new SelectListItem
                {
                    //Group = new SelectListGroup { Name = i.Group },
                    Text = i.Text,
                    Value = i.Value.ToString(),
                    Selected = contextIDs.Contains(i.ID)
                })
                .ToList();
        }

        List<SelectListItem> getResponsibilityTypeSelectList(SystemObjects type, int id, ResponsibilityTypeGroup group, int selectedID = 0)
        {
            return Company.GetAllowedResponsibilityTypesByObject(type, id)
                .Where(i => i.ResponsibilityTypeGroup == group)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = (i.ID == selectedID) })
                .ToList();
        }

        List<SelectListItem> getResponsibilityResources(string selectedID = "")
        {
            var list = GetCompanyResources()
                .Where(i => i.ResourceID > 0)
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = string.Format("{0}, {1}", i.LastName, i.FirstName),
                    Value = string.Format("Resource|{0}", i.ID),
                    Selected = (string.Format("Resource|{0}", i.ID) == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList();

            list.AddRange(
                Company.Table<Group>()
                .Select(i => new { i.ID, i.Name })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = string.Format("Group|{0}", i.ID),
                    Selected = (string.Format("Group|{0}", i.ID) == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList()
            );

            return list;
        }

        //[ValidateHttpAntiForgeryToken, HttpPost, Route("AddResponsibility")]
        //public JsonResult AddResponsibility(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("responsibility");

        //        var objectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), form["ObjectType"]);
        //        var responsibleParty = form["ResponsibleObject"].Split('|');
        //        var o = new Responsibility
        //        {
        //            ResponsibilityTypeID = parseIntField(form, "ResponsibilityType"),
        //            ObjectType = objectType.ToString(),
        //            ObjectID = parseIntField(form, "ObjectID"),
        //            ResponsibleObjectType = responsibleParty[0],
        //            ResponsibleObjectID = int.Parse(responsibleParty[1]),
        //            Visible = parseBooleanField(form, "IsVisible", true)
        //        };

        //        #region Existence check

        //        var existing = Company.Filter<Responsibility>(i => i.ResponsibilityTypeID == o.ResponsibilityTypeID && i.ObjectType == o.ObjectType && i.ObjectID == o.ObjectID, i => i.ResponsibilityContextItems).FirstOrDefault();
        //        if (existing != null)
        //        {
        //            var newContexts = getContextFormFieldForResponsibility(0, form);
        //            var existingContexts = existing.ResponsibilityContextItems.ToList();
        //            var matchingCount = 0;
        //            existingContexts.ForEach(ec =>
        //            {
        //                if (newContexts.Any(nc => nc.ObjectType == ec.ObjectType && nc.ObjectID == ec.ObjectID))
        //                {
        //                    matchingCount++;
        //                }
        //            });
        //            if (matchingCount == existingContexts.Count && matchingCount > 0 && existingContexts.Count > 0)
        //            {
        //                throw new ArgumentException("A responsibility with these settings already exists for the item.");
        //            }
        //        }

        //        #endregion

        //        Company.Add<Responsibility>(o);

        //        processContextFormFieldForResponsibility(o.ID, form);

        //        Company.Update<Responsibility>(o);  //Call this again so we can re-cache via trigger.

        //        return jsonSuccess("Item successfully created.", o.ID.ToString(), "add", HttpStatusCode.Created, new { ObjectType = o.ObjectType.ToString(), ObjectID = o.ObjectID.ToString() });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        //[HttpDelete, Route("DeleteResponsibility")]
        //public JsonResult DeleteResponsibility(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("responsibility");

        //        var id = parseIntField(form, "ID");
        //        var model = Company.GetById<Responsibility>(id);
        //        if (model == null) throw new NotFoundException("responsibility");

        //        Company.Delete<Responsibility>(model);
        //        return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        [HttpDelete, Route("DeleteResponsibilityByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityByID(int id)
        {
            try
            {
                var model = Company.GetById<Responsibility>(id);
                if (model == null) throw new NotFoundException("responsibility");

                Company.Delete<Responsibility>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
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

        [HttpGet, Route("Responsibility"), NonNullableParameters]
        public JsonNetResult Responsibility(int? id, SystemObjects? type, int? responsibilityID)
        {
            List<SelectListItem> contexts;
            List<SelectListItem> resources;
            List<SelectListItem> responsibilityTypes;
            Responsibility responsibility;
            if (responsibilityID != null)
            {
                responsibility = Company.GetById<Responsibility>((int)responsibilityID, i => i.ResponsibilityType, i => i.ResponsibilityContextItems);
                contexts = getContextSelectList(responsibility.ResponsibilityContextItems.Select(i => i.ObjectID).ToList());
                resources = getResponsibilityResources(string.Format("{0}|{1}", responsibility.ResponsibleObjectType, responsibility.ResponsibleObjectID));
                responsibilityTypes = getResponsibilityTypeSelectList((SystemObjects)Enum.Parse(typeof(SystemObjects), responsibility.ObjectType), responsibility.ObjectID, ResponsibilityTypeGroup.People, responsibility.ResponsibilityTypeID);
            }
            else
            {
                contexts = getContextSelectList();
                resources = getResponsibilityResources();
                responsibilityTypes = getResponsibilityTypeSelectList((SystemObjects)type, (int)id, ResponsibilityTypeGroup.People);
                responsibility = new Responsibility { ObjectType = type.ToString(), ObjectID = (int)id, Visible = true };
            }

            return new JsonNetResult
            {
                Data =
                new {
                    resources,
                    contexts,
                    responsibilityTypes,
                    responsibility
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        //[HttpPut, Route("EditResponsibility")]
        //public JsonResult EditResponsibility(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("responsibility");

        //        var id = parseIntField(form, "ID");
        //        var model = Company.GetById<Responsibility>(id);
        //        if (model == null) throw new NotFoundException("responsibility");
        //        var responsibleParty = form["ResponsibleObject"].Split('|');

        //        model.ResponsibleObjectType = responsibleParty[0];
        //        model.ResponsibleObjectID = int.Parse(responsibleParty[1]);
        //        model.ResponsibilityTypeID = parseIntField(form, "ResponsibilityType");
        //        model.Visible = parseBooleanField(form, "IsVisible", true);

        //        #region Existence check

        //        var existing = Company.Filter<Responsibility>(i => i.ResponsibilityTypeID == model.ResponsibilityTypeID && i.ObjectType == model.ObjectType && i.ObjectID == model.ObjectID && i.ID != model.ID, i => i.ResponsibilityContextItems).FirstOrDefault();
        //        if (existing != null)
        //        {
        //            var newContexts = getContextFormFieldForResponsibility(0, form);
        //            var existingContexts = existing.ResponsibilityContextItems.ToList();
        //            var matchingCount = 0;
        //            existingContexts.ForEach(ec =>
        //            {
        //                if (newContexts.Any(nc => nc.ObjectType == ec.ObjectType && nc.ObjectID == ec.ObjectID))
        //                {
        //                    matchingCount++;
        //                }
        //            });
        //            if (matchingCount == existingContexts.Count && matchingCount > 0 && existingContexts.Count > 0)
        //            {
        //                throw new ArgumentException("A responsibility with these settings already exists for the item.");
        //            }
        //        }

        //        #endregion

        //        processContextFormFieldForResponsibility(id, form, false);
        //        Company.Update<Responsibility>(model);  //Do this after context so the trigger will properly re-cache with the contextxs.

        //        return jsonSuccess("Item successfully updated.", id.ToString(), "edit", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        [HttpPost, Route("Responsibility")]
        public JsonResult Responsibility(Responsibility r)
        {
            Responsibility model; // = new Responsibility();

            if (r.ID == 0)
            {
                try
                {
                    //if (!form.HasKeys()) throw new NoFormDataException("responsibility");

                    var objectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), r.ObjectType);
                    //var responsibleParty = form["ResponsibleObject"].Split('|');
                    model = new Responsibility
                    {
                        ResponsibilityTypeID = r.ResponsibilityTypeID, //parseIntField(form, "ResponsibilityType"),
                        ObjectType = objectType.ToString(),
                        ObjectID = r.ObjectID, //parseIntField(form, "ObjectID"),
                        ResponsibleObjectType = r.ResponsibleObjectType, //responsibleParty[0],
                        ResponsibleObjectID = r.ResponsibleObjectID, //int.Parse(responsibleParty[1]),
                        Visible = r.Visible, //parseBooleanField(form, "IsVisible", true)
                    };

                    #region Existence check
                    var existing = Company.Filter<Responsibility>(i => i.ResponsibilityTypeID == model.ResponsibilityTypeID && i.ObjectType == model.ObjectType && i.ObjectID == model.ObjectID, i => i.ResponsibilityContextItems).FirstOrDefault();
                    if (existing != null)
                    {
                        if (r.ResponsibilityContextItems != null)
                        {
                            var newContexts = r.ResponsibilityContextItems.ToList();

                            //var newContexts = getContextFormFieldForResponsibility(0, form);
                            var existingContexts = existing.ResponsibilityContextItems.ToList();
                            var matchingCount = 0;
                            existingContexts.ForEach(ec =>
                            {
                                if (newContexts.Any(nc => nc.ObjectType == ec.ObjectType && nc.ObjectID == ec.ObjectID))
                                {
                                    matchingCount++;
                                }
                            });
                            if (matchingCount == existingContexts.Count && matchingCount > 0 && existingContexts.Count > 0)
                            {
                                throw new ArgumentException("A responsibility with these settings already exists for the item.");
                            }
                        }
                    }

                    #endregion

                    Company.Add(model);
                    processContextFieldForResponsibility(model.ID, (r.ResponsibilityContextItems == null ? null : r.ResponsibilityContextItems.ToList()));
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
            }
            else
            {
                try
                {
                    // if (!form.HasKeys()) throw new NoFormDataException("responsibility");

                    // var id = parseIntField(form, "ID");
                    model = Company.GetById<Responsibility>(r.ID);
                    if (model == null) throw new NotFoundException("responsibility");
                    //var responsibleParty = form["ResponsibleObject"].Split('|');

                    model.ResponsibleObjectType = r.ResponsibleObjectType; // responsibleParty[0];
                    model.ResponsibleObjectID = r.ResponsibleObjectID;// int.Parse(responsibleParty[1]);
                    model.ResponsibilityTypeID = r.ResponsibilityTypeID; // parseIntField(form, "ResponsibilityType");
                    model.Visible = r.Visible; // parseBooleanField(form, "IsVisible", true);

                    #region Existence check

                    var existing = Company.Filter<Responsibility>(i => i.ResponsibilityTypeID == model.ResponsibilityTypeID && i.ObjectType == model.ObjectType && i.ObjectID == model.ObjectID && i.ID != model.ID, i => i.ResponsibilityContextItems).FirstOrDefault();
                    if (existing != null)
                    {
                        var newContexts = r.ResponsibilityContextItems;
                        var existingContexts = existing.ResponsibilityContextItems.ToList();
                        var matchingCount = 0;
                        existingContexts.ForEach(ec =>
                        {
                            if (newContexts.Any(nc => nc.ObjectType == ec.ObjectType && nc.ObjectID == ec.ObjectID))
                            {
                                matchingCount++;
                            }
                        });
                        if (matchingCount == existingContexts.Count && matchingCount > 0 && existingContexts.Count > 0)
                        {
                            throw new ArgumentException("A responsibility with these settings already exists for the item.");
                        }
                    }
                    #endregion

                    processContextFieldForResponsibility(model.ID, r.ResponsibilityContextItems?.ToList(), false);
                    Company.Update(model);  //Do this after context so the trigger will properly re-cache with the contextxs.

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
            //processContextFormFieldForResponsibility(id, form, false);

            return jsonSuccess("Item successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
        }

        #endregion

        #endregion

        #region ResponsibilityType

        #region Field Generation

        [Route("ResponsibilityType_AddFields")]
        public JsonResult ResponsibilityType_AddFields(ResponsibilityTypeGroup Group)
        {
            var list = new List<EditableField>();
            var o = new ResponsibilityType();

            list.Add(new EditableField { FieldName = "ResponsibilityTypeGroup", FieldType = DataType.Hidden.ToString(), Value = ((int)Group).ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = Resources.FieldInfo.Name_Name, FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "AllocationType", Name = Resources.FieldInfo.ResponsibilityAllocatedTo_Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = true, Items = Company.GetAllocationOptions().Select(i => new SelectListItem { Text = i.Name, Value = string.Format("{0}|{1}", i.ObjectType, i.ObjectTypeID) }).ToList() });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResponsibilityTypeID</param>
        [Route("ResponsibilityType_DeleteFields"), NonNullableParameters]
        public JsonResult ResponsibilityType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ResponsibilityType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResponsibilityTypeID</param>
        [Route("ResponsibilityType_EditFields"), NonNullableParameters]
        public JsonResult ResponsibilityType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ResponsibilityType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name });
            var selectedAllocations = Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id).ToList();
            var allocations = Company
                .GetAllocationOptions()
                .Select(i => new SelectListItem {
                    Text = i.Name,
                    Value = string.Format("{0}|{1}", i.ObjectType, i.ObjectTypeID),
                    Selected = selectedAllocations.Any(c => c.ObjectType == i.ObjectType && c.ObjectID == i.ObjectTypeID)
                }).ToList();
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "AllocationType", Name = Resources.FieldInfo.ResponsibilityAllocatedTo_Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = true, Items = allocations });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        //[ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddResponsibilityType")]
        //public JsonResult AddResponsibilityType(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("ownership type");

        //        if (string.IsNullOrEmpty(form["AllocationType"]))
        //        {
        //            throw new GenericException(HttpStatusCode.BadRequest, "Allocations missing", "You have not allocated this responsibility type.");
        //        }

        //        var a = new ResponsibilityType
        //        {
        //            Name = parseTextField(form, "Name"),
        //            ResponsibilityTypeGroup = (ResponsibilityTypeGroup)Enum.Parse(typeof(ResponsibilityTypeGroup), form["ResponsibilityTypeGroup"]),
        //            Description = parseTextField(form, "Description")
        //        };

        //        Company.Add<ResponsibilityType>(a);

        //        var items = form["AllocationType"].Split(',')
        //            .Select(i => i.Split('|'))
        //            .Select(i => new ObjectModel
        //            {
        //                ObjectType = i[0],
        //                ObjectID = int.Parse(i[1])
        //            }).ToList();

        //        foreach (var o in items)
        //        {
        //            var r = new ResponsibilityTypeRelation { ObjectID = o.ObjectID, ObjectType = o.ObjectType, ResponsibilityTypeID = a.ID };
        //            Company.Set<ResponsibilityTypeRelation>().Add(r);
        //        }
        //        Company.SaveChanges();

        //        return jsonSuccess("Item successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        //[HttpDelete, Route("DeleteResponsibilityType")]
        //public JsonResult DeleteResponsibilityType(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("ownership type");

        //        var id = parseIntField(form, "ID");
        //        var model = Company.GetById<ResponsibilityType>(id);
        //        if (model == null) throw new NotFoundException("ownership type");

        //        Company.Delete<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id);
        //        Company.Delete<ResponsibilityType>(model);

        //        return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        [HttpDelete, ValidateHttpAntiForgeryToken, Route("DeleteResponsibilityTypeByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityTypeByID(int id)
        {
            try
            {
                var model = Company.GetById<ResponsibilityType>(id);
                if (model == null) throw new NotFoundException("ownership type");

                Company.Delete<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id);
                Company.Delete<ResponsibilityType>(model);

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
        public JsonNetResult GetResponsibilityType(int id, ResponsibilityTypeGroup group = ResponsibilityTypeGroup.People)
        {

            ResponsibilityType model;

            var selectedAllocations = Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id)
            .ToList()
            .Select(i => new
            {
                ResponsibilityTypeID = i.ResponsibilityTypeID,
                ObjectID = i.ObjectID,
                ObjectType = i.ObjectType
            }).ToList();


            if (id < 1)
            {
                model = new ResponsibilityType();
                model.ResponsibilityTypeGroup = group;
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

        [HttpPut, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("ResponsibilityType"), Route("ResponsibilityType")]
        public JsonResult PutResponsibilityType(ResponsibilityType model)
        {
            try
            {
                var existing = Company.GetById<ResponsibilityType>(model.ID);
                if (existing == null) throw new NotFoundException("ownership type");

                existing.Name = model.Name;
                existing.Description = model.Description;
                
                Company.Update(existing);

                Company.Delete<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == model.ID);

                foreach(var r in model.ResponsibilityTypeRelations)
                {
                    Company.Set<ResponsibilityTypeRelation>().Add(r);
                }

                Company.SaveChanges();

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

        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("ResponsibilityType"), Route("ResponsibilityType")]
        public JsonResult PostResponsibilityType(ResponsibilityType model)
        {
            try
            {

                Company.Add(model);
                Company.SaveChanges();

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

        //[HttpPut, ValidateInput(false), Route("EditResponsibilityType")]
        //public JsonResult EditResponsibilityType(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("ownership type");

        //        var id = parseIntField(form, "ID");
        //        var model = Company.GetById<ResponsibilityType>(id);
        //        if (model == null) throw new NotFoundException("ownership type");

        //        if (string.IsNullOrEmpty(form["AllocationType"]))
        //        {
        //            throw new GenericException(HttpStatusCode.BadRequest, "Allocations missing", "You have not allocated this responsibility type.");
        //        }

        //        model.Name = parseTextField(form, "Name");
        //        model.Description = parseTextField(form, "Description");

        //        Company.Update<ResponsibilityType>(model);

        //        Company.Delete<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == model.ID);

        //        var items = form["AllocationType"].Split(',')
        //            .Select(i => i.Split('|'))
        //            .Select(i => new ObjectModel
        //            {
        //                ObjectType = i[0],
        //                ObjectID = int.Parse(i[1])
        //            }).ToList();

        //        foreach (var o in items)
        //        {
        //            var r = new ResponsibilityTypeRelation { ObjectID = o.ObjectID, ObjectType = o.ObjectType, ResponsibilityTypeID = id };
        //            Company.Set<ResponsibilityTypeRelation>().Add(r);
        //        }
        //        Company.SaveChanges();

        //        return jsonSuccess("Item successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        #endregion

        #endregion

        #region ResponsibilityTypeObjectClaim

        #region Form Get/Post

        void loadValueFromCheckbox(FormCollection form, List<ResponsibilityTypeObjectClaim> list, ClaimObject co, Claim c)
        {
            bool value = false;
            var boxName = string.Format("{0}_{1}", (int)co, (int)c);
            var stringValue = form[boxName];
            value = (stringValue == "on" || stringValue == "true");
            if (value)
                list.Add(new ResponsibilityTypeObjectClaim
                {
                    Claim = c,
                    ClaimObject = co,
                    ObjectType = form["ObjectType"],
                    ObjectID = parseIntField(form, "ObjectID"),
                    ResponsibilityTypeID = parseIntField(form, "ResponsibilityTypeID")
                });
        }

        [ValidateHttpAntiForgeryToken, HttpPost, Route("AddResponsibilityTypeClaims")]
        public JsonResult AddResponsibilityTypeClaims(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("responsibility type object claims");
                var list = new List<ResponsibilityTypeObjectClaim>();

                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Delete);

                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Delete);

                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Delete);

                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Delete);

                Company.ResponsibilityTypeObjectClaims.AddRange(list);
                Company.SaveChanges();

                return jsonSuccess("Item successfully created.", "0", "add", HttpStatusCode.Created);
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

        [HttpPut, Route("EditResponsibilityTypeClaims")]
        public JsonResult EditResponsibilityTypeClaims(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("responsibility type object claims");
                var list = new List<ResponsibilityTypeObjectClaim>();

                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Root, Claim.Delete);

                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Attribute, Claim.Delete);

                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Governance, Claim.Delete);

                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Read);
                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Create);
                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Update);
                loadValueFromCheckbox(form, list, ClaimObject.Relationship, Claim.Delete);

                var ObjectType = form["ObjectType"];
                var ObjectID = parseIntField(form, "ObjectID");
                var ResponsibilityTypeID = parseIntField(form, "ResponsibilityTypeID");

                var existingClaims = Company.Filter<ResponsibilityTypeObjectClaim>(i => i.ObjectID == ObjectID && i.ObjectType == ObjectType && i.ResponsibilityTypeID == ResponsibilityTypeID).ToList();

                // Add new that were not present before.
                foreach (var nc in list)
                {
                    if (!existingClaims.Any(i => i.ClaimObject == nc.ClaimObject && i.Claim == nc.Claim))
                    {
                        Company.Set<ResponsibilityTypeObjectClaim>().Add(nc);
                    }
                }
                // Remove old that are no longer present.
                foreach (var ec in existingClaims)
                {
                    if (!list.Any(i => i.ClaimObject == ec.ClaimObject && i.Claim == ec.Claim))
                    {
                        Company.Set<ResponsibilityTypeObjectClaim>().Remove(ec);
                    }
                }

                Company.SaveChanges();

                return jsonSuccess("Item successfully created.", "0", "add", HttpStatusCode.Created);
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

        [HttpPut, Route("EditClaimsMatrix"), NonNullableParameters]
        public JsonResult EditClaimsMatrix(List<ResponsibilityTypeObjectClaim> claims, int objectID, string objectType, int responsibilityTypeID)
        {
            try
            {
                claims.ForEach(c =>
                {
                    c.ObjectID = objectID;
                    c.ObjectType = objectType;
                    c.ResponsibilityTypeID = responsibilityTypeID;
                });

                var existingClaims = Company.Filter<ResponsibilityTypeObjectClaim>(i => i.ObjectID == objectID && i.ObjectType == objectType && i.ResponsibilityTypeID == responsibilityTypeID).ToList();

                // Add new that were not present before.
                foreach (var nc in claims)
                {
                    if (!existingClaims.Any(i => i.ClaimObject == nc.ClaimObject && i.Claim == nc.Claim))
                    {
                        Company.Set<ResponsibilityTypeObjectClaim>().Add(nc);
                    }
                }
                // Remove old that are no longer present.
                foreach (var ec in existingClaims)
                {
                    if (!claims.Any(i => i.ClaimObject == ec.ClaimObject && i.Claim == ec.Claim))
                    {
                        Company.Set<ResponsibilityTypeObjectClaim>().Remove(ec);
                    }
                }

                Company.SaveChanges();

                return jsonSuccess("Item successfully created.", "0", "add", HttpStatusCode.Created);
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

        string passwordRegex = Resources.Validation.Password_Regex;
        string passwordRegexMessage = Resources.Validation.Password_Requirements;

        #region Field Generation

        /// <param name="id">ResourceTypeID</param>
        [Route("Resource_AddFields"), NonNullableParameters]
        public JsonResult Resource_AddFields(int id)
        {
            var list = new List<EditableField>();
            var type = Community.GetById<ResourceType>(id);

            list.Add(new EditableField { FieldName = "ResourceTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });//@"^([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})$", null, null, "be an email address") });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "Password", Name = "Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, Required = true, FieldName = "Status", Name = "Active?", FieldType = DataType.Boolean.ToString(), Value = "true" });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ResourceType, id).ToList(), 5);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceID</param>
        [Route("Resource_DeleteFields"), NonNullableParameters]
        public JsonResult Resource_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceID</param>
        [Route("Resource_EditFields"), NonNullableParameters]
        public JsonResult Resource_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Community.GetById<Resource>(id, i => i.CompanyResources);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Value = a.Email, Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });//@"^([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})$", null, null, "be an email address") });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString(), Value = a.CompanyResources.Single(i => i.CompanyID == Company.CurrentCompanyID).IsAdministrator.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, Required = true, FieldName = "Status", Name = "Active?", FieldType = DataType.Boolean.ToString(), Value = (a.Status == "Active").ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(), 4);

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

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Resource_ChangeMyPasswordFields")]
        public JsonResult Resource_ChangeMyPasswordFields()
        {
            var list = new List<EditableField>();
            var id = Company.CurrentResourceID;
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "CurrentPassword", Name = "Current Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Current Password", true, "", 7, 25) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "NewPassword", Name = "New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "New Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "ConfirmNewPassword", Name = "Confirm New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Confirm New Password", true, passwordRegex, null, null, passwordRegexMessage) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Resource_ChangeUserPasswordFields"), NonNullableParameters]
        public JsonResult Resource_ChangeUserPasswordFields(int id)
        {
            var list = new List<EditableField>();
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "NewPassword", Name = "New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "New Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ConfirmNewPassword", Name = "Confirm New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Confirm New Password", true, passwordRegex, null, null, passwordRegexMessage) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddResource")]
        public JsonResult AddResource(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                int typeID = parseIntField(form, "ResourceTypeID");
                var type = Community.GetById<ResourceType>(typeID);

                if (type == null) throw new NotFoundException("resource type");

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
                        Status = parseBooleanField(form, "Status") ? "Active" : "Inactive",
                        Password = "temp"
                    };

                    Community.Add<Resource>(a);

                    id = a.ID;
                    Community.ChangePassword(a.ID, "", form["Password"]);
                }
                else
                {
                    id = a.ID;
                }

                var isAdmin = parseBooleanField(form, "IsAdministrator");
                var companyResource = Community.Filter<CompanyResource>(i => i.CompanyID == Community.CurrentCompanyID && i.ResourceID == id).FirstOrDefault();

                if (companyResource == null)
                {
                    Community.Add<CompanyResource>(new CompanyResource
                    {
                        CompanyID = Company.CurrentCompanyID,
                        IsAdministrator = isAdmin,
                        ResourceID = id
                    });
                }

                if (!GetCompanyResources().Any(i => i.ResourceID == a.ID))
                {
                    GlobalReportingResource gr = new GlobalReportingResource
                    {
                        IsAdministrator = isAdmin,
                        ResourceID = id,
                        Email = a.Email,
                        LastName = a.LastName,
                        FirstName = a.FirstName,
                        Status = a.Status
                    };

                    Company.Add<GlobalReportingResource>(gr);
                }

                if (Request.ContentLength > 0)
                {
                    //var length = Request.ContentLength;
                    //var bytes = new byte[length];
                    //Request.InputStream.Read(bytes, 0, length); 
                    //HttpPostedFileBase photo = Request.Files["Image"];
                    //SecurityService.EditResourceImage(a.ID, Request.InputStream);
                }

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, a.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, typeID).ToList(), form, Server);
                Company.AddOrUpdateFields(fields);

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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("ResetResourcePassword")]
        public JsonResult ResetResourcePassword(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin) throw new NotFoundException("resource"); // only admins can reset passwords

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
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.Filter<CompanyResource>(i => i.ResourceID == id && i.CompanyID == Company.CurrentCompanyID).SingleOrDefault();
                if (model == null) throw new NotFoundException("resource");

                Community.Delete<CompanyResource>(model);
                Company.Delete<GlobalReportingResource>(x => x.ResourceID == id);
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
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null) throw new NotFoundException("resource");

                // Static fields
                model.FirstName = parseNameField(form, "FirstName");
                model.LastName = parseNameField(form, "LastName");
                model.Email = parseTextField(form, "Email");
                model.Username = parseTextField(form, "Email");
                model.Status = parseBooleanField(form, "Status") ? "Active" : "Inactive";

                Community.Update<Resource>(model);    //Must be first before saving fields.

                var cr = Community.Filter<CompanyResource>(i => i.ResourceID == id && i.CompanyID == Company.CurrentCompanyID).SingleOrDefault();
                if (cr != null)
                {
                    cr.IsAdministrator = parseBooleanField(form, "IsAdministrator");
                    Community.Update<CompanyResource>(cr);
                }

                GlobalReportingResource gr = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();

                gr.FirstName = model.FirstName;
                gr.LastName = model.LastName;
                gr.Email = model.Email;
                gr.IsAdministrator = cr.IsAdministrator;
                gr.Status = model.Status;

                Company.Update<GlobalReportingResource>(gr);

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                if (Request.ContentLength > 0)
                {
                    //SecurityService.EditResourceImage(model.ID, Request.InputStream);
                }

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

                //AuthenticationSource.

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

        [HttpPut, ValidateInput(false), Route("ChangeUserPassword")]
        public JsonResult ChangeUserPassword(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null) throw new NotFoundException("resource");

                var password1 = parseTextField(form, "NewPassword");
                var password2 = parseTextField(form, "ConfirmNewPassword");

                //AuthenticationSource.

                if (!password1.Equals(password2))
                {
                    throw new ConflictException("Password values do not match", "Password values do not match.  Please try again.");
                }

                Community.ChangePassword(id, "", password1);

                return jsonSuccess("Password successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Field Generation

        /// <param name="id">ResponseTypeID</param>
        [Route("QuestionType_DeleteFields"), NonNullableParameters]
        public JsonResult QuestionType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddQuestionType")]
        public JsonResult AddQuestionType(QuestionTypeEditorModel model)
        {
            try
            {
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

        [ValidateHttpAntiForgeryToken, HttpPut, ValidateInput(false), Route("EditQuestionType")]
        public JsonResult EditQuestionType(QuestionTypeEditorModel model)
        {
            try
            {
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
            var statuses = RuleStatus.Active.GetStatusEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            var dimensions = Company.RuleDimensions.Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldType = DataType.Hidden.ToString(), FieldName = "RuleTypeID", Value = typeID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = FieldInfo.RuleName_Description, FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = "form/Rule_SimilarItems?query=" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Status", Name = FieldInfo.RuleStatus_Name, FieldDescription = FieldInfo.RuleStatus_Description, Items = statuses, FieldType = DataType.Lookup.ToString() });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "RuleDimensionID", Name = FieldInfo.RuleDimension_Name, FieldDescription = FieldInfo.RuleDimension_Description, Items = dimensions, FieldType = DataType.Lookup.ToString() });
                        
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Threshold", Name = FieldInfo.RuleThreshold_Name, FieldDescription = FieldInfo.RuleThreshold_Description, FieldType = DataType.Percentage.ToString()});
            list.Add(new EditableField { Row = 4, Column = 1, Required = false, FieldName = "Description", Name = FieldInfo.RuleDescription_Name, FieldDescription = FieldInfo.RuleDescription_Description, FieldType = DataType.Html.ToString() });
            list.Add(new EditableField { Row = 4, Column = 2, Required = false, FieldName = "Measurement", Name = FieldInfo.RuleMeasurement_Name, FieldDescription = FieldInfo.RuleMeasurement_Description, FieldType = DataType.Html.ToString() });
            list.Add(new EditableField { Row = 5, Column = 1, Required = false, FieldName = "Purpose", Name = FieldInfo.RulePurpose_Name, FieldDescription = FieldInfo.RulePurpose_Description, FieldType = DataType.Html.ToString() });
            list.Add(new EditableField { Row = 5, Column = 2, Required = false, FieldName = "Resolution", Name = FieldInfo.RuleResolution_Name, FieldDescription = FieldInfo.RuleResolution_Description, FieldType = DataType.Html.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.RuleType, typeID).ToList(), 6);


            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("Rule_DeleteFields"), NonNullableParameters]
        public JsonResult Rule_DeleteFields(int id)
        {
            var model = Company.GetById<Rule>(id);

            if (!Company.HasPermission(SystemObjects.RuleType, model.RuleTypeID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("Rule_EditFields"), NonNullableParameters]
        public JsonResult Rule_EditFields(int id)
        {
            var statuses = RuleStatus.Active.GetStatusEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            var dimensions = Company.RuleDimensions.Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            
            var model = Company.GetById<Rule>(id);

            if ((!Company.HasPermission(SystemObjects.Rule, id, Claim.Update)) && (!Company.HasPermission(SystemObjects.RuleType, model.RuleTypeID, Claim.Update)))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = FieldInfo.RuleName_Description, FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = "form/Rule_SimilarItems?query=" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Status", Name = FieldInfo.RuleStatus_Name, FieldDescription = FieldInfo.RuleStatus_Description, Items = statuses, FieldType = DataType.Lookup.ToString(), Value = ((int)model.Status).ToString() });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "RuleDimensionID", Name = FieldInfo.RuleDimension_Name, FieldDescription = FieldInfo.RuleDimension_Description, Items = dimensions, FieldType = DataType.Lookup.ToString(), Value = model.RuleDimensionID.GetValueOrDefault(-1).ToString() });

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Threshold", Name = FieldInfo.RuleThreshold_Name, FieldDescription = FieldInfo.RuleThreshold_Description, FieldType = DataType.Percentage.ToString(), Value = model.Threshold.ToString() });

            list.Add(new EditableField { Row = 4, Column = 1, Required = false, FieldName = "Description", Name = FieldInfo.RuleDescription_Name, FieldDescription = FieldInfo.RuleDescription_Description, FieldType = DataType.Html.ToString(), Value = model.Description });
            list.Add(new EditableField { Row = 4, Column = 2, Required = false, FieldName = "Measurement", Name = FieldInfo.RuleMeasurement_Name, FieldDescription = FieldInfo.RuleMeasurement_Description, FieldType = DataType.Html.ToString(), Value = model.Measurement });
            list.Add(new EditableField { Row = 5, Column = 1, Required = false, FieldName = "Purpose", Name = FieldInfo.RulePurpose_Name, FieldDescription = FieldInfo.RulePurpose_Description, FieldType = DataType.Html.ToString(), Value = model.Purpose });
            list.Add(new EditableField { Row = 5, Column = 2, Required = false, FieldName = "Resolution", Name = FieldInfo.RuleResolution_Name, FieldDescription = FieldInfo.RuleResolution_Description, FieldType = DataType.Html.ToString(), Value = model.Resolution });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Rule, id).ToList(), 6);

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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddRule")]
        public JsonResult AddRule(FormCollection form)
        {
            try
            {                
                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var model = new Rule
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    Measurement = parseTextField(form, "Measurement"),
                    Purpose = parseTextField(form, "Purpose"),
                    Resolution = parseTextField(form, "Resolution"),
                    RuleDimensionID = parseNullableIntField(form, "RuleDimensionID"),
                    RuleTypeID = parseIntField(form, "RuleTypeID"),
                    Status = (RuleStatus)Enum.Parse(typeof(RuleStatus), form["Status"]),
                    Threshold = decimal.Parse(form["Threshold"])
                };

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Rule, model.ID, Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList(), form, Server);
                Company.SaveOrUpdate<Rule>(model, fields);

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

        [HttpDelete, Route("DeleteRule")]
        public JsonResult DeleteRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Rule>(id);
                if (model == null) throw new NotFoundException("Rule");

                if (!Company.HasPermission(SystemObjects.RuleType, model.RuleTypeID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    Name = model.Name,
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

                if ((!Company.HasPermission(SystemObjects.Rule, id, Claim.Update)) && (!Company.HasPermission(SystemObjects.RuleType, model.RuleTypeID, Claim.Update)))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                var dimension = parseNullableIntField(form, "RuleDimensionID");

                if (dimension.HasValue && dimension.GetValueOrDefault() > 0)
                    model.RuleDimensionID = dimension;
                else
                    model.RuleDimensionID = null;
                
                model.Measurement = parseTextField(form, "Measurement");
                model.Purpose = parseTextField(form, "Purpose");
                model.Resolution = parseTextField(form, "Resolution");
                model.Status = (RuleStatus)Enum.Parse(typeof(RuleStatus), form["Status"]);
                model.Threshold = decimal.Parse(form["Threshold"]);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Rule, model.ID, Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList(), form, Server);
                Company.SaveOrUpdate<Rule>(model, fields);

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

        #endregion
        
        #region RuleDimension

        #region Field Generation

        [Route("RuleDimension_AddFields")]
        public JsonResult RuleDimension_AddFields()
        {
            var model = new Rule();
            if (!Company.HasPermission(SystemObjects.RuleType, 0, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("RuleDimension_DeleteFields"), NonNullableParameters]
        public JsonResult RuleDimension_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("RuleDimension_EditFields"), NonNullableParameters]
        public JsonResult RuleDimension_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var model = Company.GetById<RuleDimension>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Html.ToString(), Value = model.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddRuleDimension")]
        public JsonResult AddRuleDimension(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.RuleType, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("RuleDimension");

                var model = new RuleDimension
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                Company.Add<RuleDimension>(model);

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
                if (!form.HasKeys()) throw new NoFormDataException("RuleDimension");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleDimension>(id);
                if (model == null) throw new NotFoundException("RuleDimension");

                if (Company.Rules.Where(x => x.RuleDimensionID == id).Any())
                {
                    return jsonException(FormInfo.Delete_Error_Rule_Exist, HttpStatusCode.Forbidden);
                }

                if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    Name = model.Name,
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
                if (!form.HasKeys()) throw new NoFormDataException("RuleDimension");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleDimension>(id);
                if (model == null) throw new NotFoundException("RuleDimension");

                if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update<RuleDimension>(model);

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

        #region RuleImplementation

        #region Field Generation

        /// <param name="ruleID">RuleID</param>
        [Route("RuleImplementation_AddFields")]
        public JsonResult RuleImplementation_AddFields(int ruleID)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldType = DataType.Hidden.ToString(), FieldName = "RuleID", Value = ruleID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = FieldInfo.RuleImplementation_Name, FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = "form/Rule_SimilarItems?query=" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "SourceID", Name = FieldInfo.RuleImplementation_SourceID, FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "SourceUri", Name = FieldInfo.RuleImplementation_SourceUri, FieldType = DataType.Text.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleImplementationID</param>
        [Route("RuleImplementation_DeleteFields"), NonNullableParameters]
        public JsonResult RuleImplementation_DeleteFields(int id)
        {
            var model = Company.GetById<RuleImplementation>(id);

            if (!Company.HasPermission(SystemObjects.Rule, model.RuleID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleImplementationID</param>
        [Route("RuleImplementation_EditFields"), NonNullableParameters]
        public JsonResult RuleImplementation_EditFields(int id)
        {
            var model = Company.GetById<RuleImplementation>(id);

            if ((!Company.HasPermission(SystemObjects.Rule, id, Claim.Update)) && (!Company.HasPermission(SystemObjects.Rule, model.RuleID, Claim.Update)))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = FieldInfo.Name_Name, FieldDescription = FieldInfo.RuleName_Description, FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "SourceID", Name = FieldInfo.RuleImplementation_SourceID, FieldType = DataType.Text.ToString(), Value = model.SourceID });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "SourceUri", Name = FieldInfo.RuleImplementation_SourceUri, FieldType = DataType.Text.ToString(), Value = model.SourceUri });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddRuleImplementation")]
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

        [HttpDelete, Route("DeleteRuleImplementation")]
        public JsonResult DeleteRuleImplementation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule Implementation");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<RuleImplementation>(id);
                if (model == null) throw new NotFoundException("Rule Implementation");

                if (!Company.HasPermission(SystemObjects.Rule, model.RuleID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    Name = model.Name,
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

                if ((!Company.HasPermission(SystemObjects.Rule, model.RuleID, Claim.Update)) && (!Company.HasPermission(SystemObjects.Rule, model.RuleID, Claim.Update)))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");

                model.SourceID = parseTextField(form, "SourceID");
                model.SourceUri = parseTextField(form, "SourceUri");

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

        #endregion

        #region RuleQualifierType

        #region Form Get/Post

        [HttpPut, Route("MoveRuleQualifierType"), ValidateInput(false), ValidateHttpAntiForgeryToken]
        public JsonResult MoveRuleQualifierType(int id, bool moveUp = false)
        {
            try
            {
                var q = Company.GetById<RuleResultQualifierType>(id);
                if (q == null)
                    throw new Exception($"Could not find rule qualifier for id '{id}'");
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

        [HttpPost, Route("AddRuleQualifierType"), ValidateInput(false), ValidateHttpAntiForgeryToken]
        public JsonResult AddQualifierType(RuleResultQualifierType model)
        {
            try
            {
                if (model == null)
                    throw new Exception("Supplied model was null");
                model.Order = Company.Count<RuleResultQualifierType>(r => r.RuleImplementationID == model.RuleImplementationID) + 1;

                Company.RuleResultQualifierTypes.Add(model);
                Company.SaveChanges();
            } catch(Exception ex)
            {
                return jsonException(ex, HttpStatusCode.OK);
            }
            return jsonSuccess("Qualifier Type added successfully", model.ID.ToString(), "add", HttpStatusCode.OK);
        }

        [HttpPut, Route("EditRuleQualifierType"), ValidateInput(false), ValidateHttpAntiForgeryToken]
        public JsonResult EditQualifierType(RuleResultQualifierType model)
        {
            try
            {
                if (model == null)
                    throw new Exception("Supplied model was null");
                var qualifier = Company.GetById<RuleResultQualifierType>(model.ID);
                if (qualifier == null)
                    throw new Exception($"Cannot find qualifier id '{model?.ID}'");

                qualifier.Name = model.Name;
                qualifier.ResolutionObject = model.ResolutionObject;
                qualifier.ResolutionObjectID = model.ResolutionObjectID;
                qualifier.ResolutionFieldTypeID = model.ResolutionFieldTypeID;
                qualifier.ResolutionFieldTypeName = model.ResolutionFieldTypeName;

                Company.SaveChanges();
                
            } catch(Exception ex)
            {
                return jsonException(ex, HttpStatusCode.OK);
            }
            return jsonSuccess("Qualifier Type edited successfully", model.ID.ToString(), "edit", HttpStatusCode.OK);
        }

        [HttpDelete, Route("DeleteQualifierType")]
        public JsonResult DeleteQualifierType(int id)
        {
            try
            {
                var qualifier = Company.GetById<RuleResultQualifierType>(id);
                if (qualifier == null)
                    throw new Exception($"Could not find qualifier type id {id}");
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
            if (!Company.HasPermission(SystemObjects.RuleType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = new RuleType();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });
            loadIconFields(list, 3);

            a = null;
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeID</param>
        [Route("RuleType_DeleteFields"), NonNullableParameters]
        public JsonResult RuleType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<RuleType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyTypeID</param>
        [Route("RuleType_EditFields"), NonNullableParameters]
        public JsonResult RuleType_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<RuleType>(id);
            var style = Company.GetObjectStyle(SystemObjects.RuleType, id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });
            loadIconFields(list, 3, style);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddRuleType")]
        public JsonResult AddRuleType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.RuleType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("rule type");

                var a = new RuleType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description")
                };

                Company.Add<RuleType>(a);

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

                if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<RuleType>(i => i.ID == id);
                deleteObjectStyle(SystemObjects.RuleType, id);

                

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

                var style = Company.GetObjectStyle(SystemObjects.RuleType, id);

                if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<RuleType>(model);

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

        #region MapSequence

        public class MapItemsSequenceEditModel
        {
            public List<MapItemSequenceEditModel> Items { get; set; }
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

            var referencedItems =  Company.Filter<MapSequence>(i =>
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


            var contexts = Company.Query<dynamic>(@"select * from
                                                (
                                                select a.Name, 'Artifact|' + cast(a.id as varchar(10)) as ID, 'Glossary' as Category, t.name as Type, cast(0 as bit) as Checked from artifact a
                                                join artifacttype t on t.id = a.artifacttypeid
                                                union all
                                                select x.Name, 'Taxonomy|' + cast(x.id as varchar(10)) as ID, 'Model' as Category, t.name as Type, cast(0 as bit) as Checked from taxonomy x
                                                join taxonomytype t on t.id = x.taxonomytypeid
                                                ) z
                                                order by name").ToList();

            return new JsonNetResult
            {
                Data = new {
                    Available = availableItems,
                    Referenced = referencedItems,
                    Contexts = contexts
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, Route("mapsequence/{type}/{id:int}/mapitems")]
        public JsonResult SetMapItemsForMapSequenceManagement(SystemObjects type, int id, MapItemsSequenceEditModel model)
        {
            try
            {
                if (!Company.HasPermission(type, id, Claim.Create, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                model.Items.ForEach(m =>
                {
                    MapSequence mapSequence = null;
                    if (m.ID > 0) {
                        mapSequence = Company.GetById<MapSequence>(m.ID, i => i.MapSequenceContexts);
                    }

                    if (m.IsDeleting)
                    {
                        if (mapSequence == null)
                        {
                            //return jsonException($"Map Sequence ID {m.ID} not found", HttpStatusCode.Forbidden);
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

        #region ScoreType Form Get/Post

        #region Field Generation

        /// <param name="id">ScoreTypeID</param>
        [Route("ScoreType_DeleteFields"), NonNullableParameters]
        public JsonResult ScoreType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.ScoreType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<ScoreType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddScoreType")]
        public JsonResult AddScoreType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ScoreType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("score type");

                var a = new ScoreType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<ScoreType>(a);

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

        [HttpDelete, Route("DeleteScoreType")]
        public JsonResult DeleteScoreType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("score type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ScoreType>(id);
                if (model == null) throw new NotFoundException("score type");

                if (!Company.HasPermission(SystemObjects.ScoreType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<ScoreType>(model);

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

        [HttpPut, ValidateInput(false), Route("EditScoreType")]
        public JsonResult EditScoreType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("score type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ScoreType>(id);
                if (model == null) throw new NotFoundException("score type");

                if (!Company.HasPermission(SystemObjects.ScoreType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<ScoreType>(model);

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

        #region ScoreTypeMetric Form Get/Post

        #region Field Generation

        /// <param name="id">ScoreTypeMetricID</param>
        [Route("ScoreTypeMetric_DeleteFields"), NonNullableParameters]
        public JsonResult ScoreTypeMetric_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.ScoreTypeMetric, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<ScoreTypeMetric>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region JSON Feeds

        [Route("ScoreTypeMetric_FormData"), NonNullableParameters]
        public JsonNetResult ScoreTypeMetric_FormData(int id)
        {
            var type = Company.GetById<ScoreTypeMetric>(id);
            if (type == null) return new JsonNetResult { Data = null };

            var model = new Dictionary<string, object>();
            model.Add("ID", type.ID);
            model.Add("Name", type.Name);
            model.Add("CheckType", type.CheckType);
            model.Add("Description", type.Description);
            model.Add("Object", type.Object);
            model.Add("ObjectID", type.ObjectID);
            model.Add("ObjectCombined", $"{type.Object}|{type.ObjectID}");
            model.Add("MaximumScore", type.MaximumScore);

            var xml = XElement.Parse(type.Configuration);
            switch (type.CheckType)
            {
                //case StatisticCheckType.Count:
                case StatisticCheckType.Existence:
                case StatisticCheckType.ScoreRollupViaRelationship:
                case StatisticCheckType.ScoreRollupViaOwnership:
                    model.Add("CheckObject", xml.Element("ObjectType").Value);
                    model.Add("CheckObjectID", xml.Element("ObjectID").Value);
                    break;
                case StatisticCheckType.PropertyPopulated:
                    model.Add("PropertyName", xml.Element("PropertyName").Value);
                    break;
                case StatisticCheckType.PropertyValueCheck:
                    model.Add("PropertyName", xml.Element("PropertyName").Value);
                    model.Add("PropertyValue", (xml.Element("PropertyValue") != null) ? xml.Element("PropertyValue").Value : "");
                    break;
                //case StatisticCheckType.EventMetric:
                //    model.Add("ValidField", xml.Element("ValidField").Value);
                //    model.Add("InvalidField", xml.Element("InvalidField").Value);
                //    model.Add("Threshold", xml.Element("Threshold").Value);
                //    break;
                case StatisticCheckType.PredicateMetric:
                    model.Add("Predicate", xml.Element("Predicate").Value);
                    break;
                case StatisticCheckType.Relationship:
                    try
                    {
                        if (xml.Element("CheckObjects") != null && xml.Element("CheckObjects").Elements("Object").ToList().Count > 0)
                        {
                            model.Add("CheckObjects",
                                xml.Element("CheckObjects")
                                    .Elements("Object")
                                    .Select(co => $"{co.Element("Type").Value}|{co.Element("ID").Value}").ToList()
                                );
                        }
                        else if (xml.Element("CheckObjects") != null && xml.Element("CheckObjects").Elements("IntersectType").ToList().Count > 0)
                        {
                            model.Add("CheckObjects", xml.Element("CheckObjects").Elements("IntersectType").Select(e => $"{e.Value}").ToList());
                        }
                        else
                        {
                            model.Add("CheckObjects", new List<string> { $"{xml.Element("ObjectType").Value}|{xml.Element("ObjectID").Value}" });
                        }
                    }
                    catch (Exception)
                    {
                    }
                    break;
            }

            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ScoreTypeMetric_CheckTypeOptions")]
        public JsonNetResult ScoreTypeMetric_CheckTypeOptions()
        {
            var models = StatisticCheckType.Existence.GetEnumList().Select(i => new KnockoutListItem(i.Name, ((int)i.ID).ToString()));
            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ScoreTypeMetric_ObjectOptions")]
        public JsonNetResult ScoreTypeMetric_ObjectOptions()
        {
            var models = Company.GetTypes().Select(i => new KnockoutListItem(i.Name, $"{i.ObjectType}|{i.ObjectTypeID}"));
            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("ScoreTypeMetric_CheckObjectOptions"), NonNullableParameters]
        public JsonNetResult ScoreTypeMetric_CheckObjectOptions(SystemObjects type, int id, StatisticCheckType check)
        {
            var models = new List<KnockoutListItem>();

            switch (check)
            {
                case StatisticCheckType.Existence:
                    models.AddRange(Company.Query<KnockoutListItem>(@"
SELECT	'AttributeType|'+ cast(T.ID as varchar) as value, 
		'Attribute :' + T.Name as title
from	AttributeType T
		inner join AttributeTypeRelation R on R.AttributeTypeID = T.ID and T.ParentID is null and R.ObjectType = @type and R.ObjectID = @id
union 
SELECT	'ResponsibilityType|'+ cast(ID as varchar) as value, 
		'Responsibility :' + Name as title 
from	ResponsibilityType T
		inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeID = T.ID and R.ObjectType = @type and R.ObjectID = @id", new { type = type.ToString(), id }).OrderBy(i => i.title));
                    break;
                case StatisticCheckType.PropertyValueCheck:
                case StatisticCheckType.PropertyPopulated:
                    switch (type)
                    {
                        case SystemObjects.ArtifactType:
                            models.Add(new KnockoutListItem("Name", "Name"));
                            models.Add(new KnockoutListItem("Description", "Description"));
                            models.Add(new KnockoutListItem("Status", "Status"));
                            break;
                        case SystemObjects.ReferenceItemType:
                            models.Add(new KnockoutListItem("Code", "Code"));
                            break;
                        case SystemObjects.TaxonomyType:
                        case SystemObjects.PolicyType:
                        case SystemObjects.RuleType:
                            models.Add(new KnockoutListItem("Name", "Name"));
                            models.Add(new KnockoutListItem("Description", "Description"));
                            break;
                    }
                    models.AddRange(Company.GetFieldTypesByObject(type, id).Select(i => new KnockoutListItem { title = i.FriendlyName, value = i.Name }));
                    break;
                case StatisticCheckType.Relationship:
                    models.AddRange(Company.Query<KnockoutListItem>(@"
                    select  distinct 
                            RT.ID as value, 
                            RT.Name as title
                    from    [IntersectType] RT
                    where ((RT.Subject = @type and RT.SubjectID = @id) or (RT.Object = @type and RT.ObjectID = @id))", new { type = type.ToString(), id }).OrderBy(i => i.title));
                    break;
                case StatisticCheckType.FusionOwnership:
                    //models.AddRange(Company.GetStatisticTypeCountCheckOptions().Select(i => new { title = i.Name, value = i.ID.ToString() }));
                    break;
                case StatisticCheckType.ScoreRollupViaRelationship:
                    models.AddRange(Company.Query<KnockoutListItem>(@"
select  distinct 
        D.Object + '|' + cast(D.ObjectID as varchar) as value, 
        'Relationship :' + D.TextPath as title
from    [IntersectType] RT
        inner join cache.ObjectDetails D on D.[Object] = case when (RT.Subject = @type and RT.SubjectID = @id) then RT.Object else RT.Subject end 
                                            and D.ObjectID = case when (RT.Subject = @type and RT.SubjectID = @id) then RT.ObjectID else RT.SubjectID end
                                            and ( 
                                                (RT.Subject = @type and RT.SubjectID = @id) OR 
                                                (RT.Object = @type and RT.ObjectID = @id) 
                                                )", new { type = type.ToString(), id }).OrderBy(i => i.title));
                    break;
                case StatisticCheckType.ScoreRollupViaOwnership:
                    models.AddRange(Company.GetStatisticTypeRollupCheckOptions().Select(i => new KnockoutListItem { title = i.Name, value = i.ID.ToString() }));
                    break;
                //case StatisticCheckType.EventMetric:
                //    models.AddRange(Company.GetStatisticTypeCountCheckOptions().Select(i => new KnockoutListItem { title = i.Name, value = i.ID.ToString() }));
                //    break;
                case StatisticCheckType.PredicateMetric:
                    models.AddRange(Company.Table<Predicate>().Select(i => new KnockoutListItem { title = i.Name, value = i.ID.ToString() }));
                    break;
            }

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion

        string getXmlConfigurationFromFormFields(FormCollection form, StatisticCheckType checkType)
        {
            var fields = new XElement("fields");

            switch (checkType)
            {
                //case StatisticCheckType.Count:
                case StatisticCheckType.Existence:
                case StatisticCheckType.ScoreRollupViaRelationship:
                case StatisticCheckType.ScoreRollupViaOwnership:
                    fields.Add(new XElement("ObjectType", form["CheckObject"]));
                    fields.Add(new XElement("ObjectID", form["CheckObjectID"]));
                    break;
                case StatisticCheckType.PropertyPopulated:
                    fields.Add(new XElement("PropertyName", form["PropertyName"]));
                    break;
                case StatisticCheckType.PropertyValueCheck:
                    fields.Add(new XElement("PropertyName", form["PropertyName"]));
                    fields.Add(new XElement("PropertyValue", form["PropertyValue"]));
                    break;
                case StatisticCheckType.Relationship:
                    var rawCheckObjects = form["CheckObjects"];
                    if (!string.IsNullOrEmpty(rawCheckObjects))
                    {
                        //remove formatting
                        rawCheckObjects = rawCheckObjects.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim('[').Trim(']');

                        var checkObjectStrings = rawCheckObjects.Split(',').ToList();
                        var checksElement = new XElement("CheckObjects");
                        checkObjectStrings.ForEach(i =>
                        {
                            var checkElement = new XElement("IntersectType", i.Trim('"'));
                            checksElement.Add(checkElement);
                        });
                        fields.Add(checksElement);
                    }
                    break;
                //case StatisticCheckType.EventMetric:
                //    fields.Add(new XElement("ValidField", form["ValidField"]));
                //    fields.Add(new XElement("InvalidField", form["InvalidField"]));
                //    fields.Add(new XElement("Threshold", decimal.Parse(form["Threshold"])));
                //    break;
                case StatisticCheckType.PredicateMetric:
                    fields.Add(new XElement("Predicate", form["Predicate"]));
                    break;
            }

            return fields.ToString();
        }

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddScoreTypeMetric")]
        public JsonResult AddScoreTypeMetric(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ScoreTypeMetric, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("score type metric");

                var a = new ScoreTypeMetric
                {
                    Name = parseTextField(form, "Name"),
                    ScoreTypeID = parseIntField(form, "ScoreTypeID"),
                    Description = parseTextField(form, "Description"),
                    CheckType = (StatisticCheckType)Enum.Parse(typeof(StatisticCheckType), form["CheckType"]),
                    MaximumScore = parseIntField(form, "MaximumScore"),
                    Object = parseTextField(form, "Object"),
                    ObjectID = parseIntField(form, "ObjectID")
                };
                a.Configuration = getXmlConfigurationFromFormFields(form, a.CheckType);

                Company.Add(a);

                var version = new ScoreTypeMetricVersion { CheckType = a.CheckType, Configuration = a.Configuration, Description = a.Description, MaximumScore = a.MaximumScore, Name = a.Name, ScoreTypeMetricID = a.ID };
                Company.Add(version);

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

        [HttpDelete, Route("DeleteScoreTypeMetric")]
        public JsonResult DeleteScoreTypeMetric(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("score type metric");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ScoreTypeMetric>(id);
                if (model == null) throw new NotFoundException("score type");

                if (!Company.HasPermission(SystemObjects.ScoreTypeMetric, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                model.Deleted = true;
                Company.Update<ScoreTypeMetric>(model);

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

        [HttpPut, ValidateInput(false), Route("EditScoreTypeMetric")]
        public JsonResult EditScoreTypeMetric(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("score type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ScoreTypeMetric>(id);
                if (model == null) throw new NotFoundException("score type");

                if (!Company.HasPermission(SystemObjects.ScoreTypeMetric, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.MaximumScore = parseIntField(form, "MaximumScore");
                model.CheckType = (StatisticCheckType)Enum.Parse(typeof(StatisticCheckType), form["CheckType"]);
                model.Configuration = getXmlConfigurationFromFormFields(form, model.CheckType);

                var version = new ScoreTypeMetricVersion { CheckType = model.CheckType, Configuration = model.Configuration, Description = model.Description, MaximumScore = model.MaximumScore, Name = model.Name, ScoreTypeMetricID = model.ID };
                Company.Add(version);

                Company.Update<ScoreTypeMetric>(model);

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
                    new {
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

        
        [HttpPost, Route("shoppingcart/request")]
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
            catch(Exception ex)
            {
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

            return jsonSuccess("Your request has been submitted", cart.ID.ToString(), "update", HttpStatusCode.OK);

        }
        
        [HttpPost, Route("shoppingcart/clear")]
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


            //items.AddRange(Community.Table<ResourceType>().OrderBy(i => i.Name).Select(i => new { i.ID, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Resource Type :: {0}", i.Name), Value = string.Format("{0}|{1}", SystemObjects.ResourceType.ToString(), i.ID) }));

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Object", Name = "Assign Survey To", FieldType = DataType.Lookup.ToString(), Items = items });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "ValidForDays", Name = "# of Days before user can retake", FieldType = DataType.Number.ToString()});
            

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        [Route("SurveyType_DeleteFields"), NonNullableParameters]
        public JsonResult SurveyType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddSurveyType")]
        public JsonResult AddSurveyType(FormCollection form)
        {
            try
            {
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
                Company.Add<SurveyType>(model);

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
                if (!form.HasKeys()) throw new NoFormDataException("survey type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<SurveyType>(id);
                if (model == null) throw new NotFoundException("survey type");

                model.Name = parseTextField(form, "Name");
                model.ValidForDays = parseNullableIntField(form, "ValidForDays", 1).GetValueOrDefault(1);

                Company.Update<SurveyType>(model);

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

            string joinStatement = "";

            switch (type.ToLower())
            {
                case "artifacttype":
                    joinStatement = "inner join artifact a on a.id = d.ObjectID and d.Object = @object and d.ObjectTypeID = @typeId and d.ObjectType = @type";
                    break;
                case "taxonomytype":
                    joinStatement = "inner join taxonomy t on  t.id = d.ObjectID and d.Object = @object and d.ObjectTypeID = @typeId and d.ObjectType = @type";
                    break;
                case "policytype":
                    joinStatement = "inner join policy a on  a.id = d.ObjectID and d.Object = @object and d.ObjectTypeID = @typeId and d.ObjectType = @type";
                    break;
                case "attributetype":
                    joinStatement = "inner join attributetype a on  a.id = d.ObjectID and d.Object = @type and d.ObjectTypeID = @typeId and d.ObjectType = @type";
                    break;
                case "fusionattributetype":
                    joinStatement = "inner join fusionattribute a on  a.id = d.ObjectID and d.Object = @object and d.ObjectTypeID = @typeId and d.ObjectType = @type";
                    break;
                case "domaingroup":
                    joinStatement = "inner join domainitem a on  a.id = d.ObjectID and d.Object = @object and d.ObjectTypeID = @typeId and d.ObjectType = @type";
                    break;

            }


            var list = new List<EditableField>();
            var items = Company.Query<dynamic>(string.Format(QueryConstants.SynonymOptions, joinStatement), new { predicateId,  type = new Dapper.DbString { IsAnsi = true, Value = type.ToString(), IsFixedLength = true, Length = 50 }, @object = new Dapper.DbString { IsAnsi = true, Value = obj.ToString(), IsFixedLength = true, Length = 50 }, objectId = objId, typeId, query }).ToList();
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

        #region Field Generation

        [Route("Synonym_AddFields"), NonNullableParameters]
        public JsonResult Synonym_AddFields(string type, int id)
        {
            //if (!Company.HasPermission(SystemObjects.TaxonomyType, t, Claim.Create))
            //    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var items = Company.Query<dynamic>(QueryConstants.SynonymOptions, new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString(), IsFixedLength = true, Length = 50 }, id }).ToList();
            var typeIsSubject = true;
            if (items.Count > 0)
            {
                typeIsSubject = (bool)items[0].TargetingSubject;
            }
            list.Add(new EditableField { FieldName = "TypeIsSubject", FieldType = DataType.Hidden.ToString(), Value = typeIsSubject.ToString() });
            list.Add(new EditableField { FieldName = "Type", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Synonym", Name = "Synonym", FieldType = DataType.Lookup.ToString(), Items = items.Select(i => new SelectListItem { Text = i.Name, Value = i.ID }).ToList() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Object's ID</param>
        [Route("Synonym_DeleteFields"), NonNullableParameters]
        public JsonResult Synonym_DeleteFields(int id)
        {
            var detail = Company.GetById<Intersect>(id);

            if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), detail.Subject), detail.SubjectID, Claim.Delete, ClaimObject.Relationship))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "IntersectID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post


        [ValidateHttpAntiForgeryToken, HttpPost, Route("AddNymAllocation")]
        public JsonResult AddNymAllocation(NymAllocationModel model)
        {
            try
            {
                if (!Company.HasPermission(model.Object, model.ObjectID, Claim.Create, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                // delete any existing allocations
                var rels = Company.Filter<NymRelation>(x => x.Object == model.Object.ToString() && x.ObjectID == model.ObjectID);

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
        

        [ValidateHttpAntiForgeryToken, HttpPost, Route("AddCustomSynonym")]
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

                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), model.Object), model.ObjectID, Claim.Create, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Add<Nym>(model);

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

        [ValidateHttpAntiForgeryToken, HttpPost, Route("AddSynonym")]
        public JsonResult AddSynonym(SynonymEditModel model)
        {
            try
            {
                if (!Company.HasPermission(model.Type, model.ID, Claim.Create, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var synonymSegments = model.Synonym.Split('|');
                var subject = model.TypeIsSubject ? model.Type : (SystemObjects)Enum.Parse(typeof(SystemObjects), synonymSegments[0]);
                var subjectID = model.TypeIsSubject ? model.ID : int.Parse(synonymSegments[1]);
                var @object = !model.TypeIsSubject ? model.Type : (SystemObjects)Enum.Parse(typeof(SystemObjects), synonymSegments[0]);
                var objectID = !model.TypeIsSubject ? model.ID : int.Parse(synonymSegments[1]);

                if (subjectID == objectID) return jsonException("Cannot add a synonym that specifies the same object as the current object.", HttpStatusCode.Forbidden);

                var sSubject = subject.ToString();
                var sObject = @object.ToString();

                var subjectDetail = Company.CacheObjects.SingleOrDefault(i => i.Object == sSubject && i.ObjectID == subjectID);
                var objectDetail = Company.CacheObjects.SingleOrDefault(i => i.Object == sObject && i.ObjectID == objectID);

                if (subjectDetail != null && objectDetail != null)
                {
                    var intersectType = Company.Filter<IntersectType>(i =>
                        (
                        (i.Subject == subjectDetail.ObjectType && i.SubjectID == subjectDetail.ObjectTypeID && i.Object == objectDetail.ObjectType && i.ObjectID == objectDetail.ObjectTypeID) ||
                        (i.Subject == objectDetail.ObjectType && i.SubjectID == objectDetail.ObjectTypeID && i.Object == subjectDetail.ObjectType && i.ObjectID == subjectDetail.ObjectTypeID)
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

                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), detail.Subject), detail.SubjectID, Claim.Delete, ClaimObject.Relationship))
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

                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), detail.Object), detail.ObjectID, Claim.Delete, ClaimObject.Relationship))
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
            if (!Company.HasPermission(SystemObjects.TaxonomyType, t, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var type = Company.GetById<TaxonomyType>(t);

            list.Add(new EditableField { FieldName = "TaxonomyTypeID", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });
            list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = $"form/Taxonomy_SimilarItems?typeID={t}&id={p}&query=" });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, t).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyID</param>
        [Route("Taxonomy_DeleteFields"), NonNullableParameters]
        public JsonResult Taxonomy_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Taxonomy, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyID</param>
        [Route("Taxonomy_EditFields"), NonNullableParameters]
        public JsonResult Taxonomy_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Taxonomy, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Taxonomy>(id);

            var parents = Company.Query<dynamic>(@"
declare @typeName nvarchar(250)

select	@typeName = Name + '/'
from	TaxonomyType
where ID = @t;

with P as	(
			select	ID,
					ParentID
			from	Taxonomy
			where	TaxonomyTypeID = @t and ID = @i
			union all
			select	C.ID,
					C.ParentID
			from	Taxonomy C
					inner join P on P.ID = C.ParentID
			)

select	ID,
		REPLACE(TextPath, @typeName, '') as Name 
from	Taxonomy 
where	TaxonomyTypeID = @t
		and ID not in (select ID from P)
order by TextPath
", new { t = a.TaxonomyTypeID, i = a.ID }).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = (i.ID.ToString() == a.ParentID.ToString()) }).ToList();
            parents.Insert(0, new SelectListItem { Text = "- Root -", Value = "0", Selected = !(a.ParentID.HasValue) });

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ParentID", Name = "Parent Model", FieldDescription = Resources.FormInfo.Taxonomy_ChangeParent_Warning, FieldType = DataType.Lookup.ToString(), Items = parents, Value = ((a.ParentID.HasValue) ? a.ParentID.Value.ToString() : "0") });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, a.TaxonomyTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Taxonomy, id).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Taxonomy_SimilarItems"), NonNullableParameters]
        public JsonNetResult Taxonomy_SimilarItems(int typeID, int id, string query)
        {

            var sql = @"with p as
                    (
                    select t.id, t.parentid, t.name from taxonomy t
                    where t.id = @id
                    union all
                    select t.id, t.parentid, t.name from taxonomy t
                    join p on t.parentid = p.id and t.parentid is not null and t.id != p.id
                    )
                    select 
	                    d.Name,
	                    d.Url, 
	                    d.IconForeColor, 
	                    d.IconBackColor, 
	                    d.[Description],
	                    d.objecttypeid
                    from p
                    join cache.objectdetails d on d.objectid = p.id and d.[object] = @type
                    where d.name like @query + '%'
                    ";
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>((id > 0) ? sql : QueryConstants.SimilarItems, new { type = new DbString { Value = "Taxonomy", IsFixedLength = true, IsAnsi = true, Length = 50 }, typeID, id, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        
        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddTaxonomy")]
        public JsonResult AddTaxonomy(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy");

                int typeID = parseIntField(form, "TaxonomyTypeID");
                var type = Company.GetById<TaxonomyType>(typeID);
                if (type == null) throw new NotFoundException("taxonomy type");

                if (!Company.HasPermission(SystemObjects.TaxonomyType, typeID, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new Taxonomy();

                // Static fields
                a.TaxonomyTypeID = typeID;
                a.Name = parseTextField(form, "Name");
                a.Description = parseTextField(form, "Description");

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    a.ParentID = parseIntField(form, "ParentID");
                    if (a.ParentID == 0) a.ParentID = null;
                }

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Taxonomy, a.ID, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate<Taxonomy>(a, fields);

                dynamic custom = new
                {
                    TaxonomyTypeID = typeID,
                    ParentID = a.ParentID,
                    Name = a.Name,
                    Context = form["_context"]
                };

                

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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
                var model = Company.GetById<Taxonomy>(id);
                if (model == null) throw new NotFoundException("taxonomy");

                if (!Company.HasPermission(SystemObjects.Taxonomy, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                dynamic custom = new
                {
                    TaxonomyTypeID = model.TaxonomyTypeID,
                    ParentID = model.ParentID,
                    Name = model.Name,
                    Context = form["_context"]
                };

                Company.Delete(model);

                

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

                if (!Company.HasPermission(SystemObjects.Taxonomy, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                // Static fields
                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.ParentID = parseIntField(form, "ParentID");
                if (model.ParentID == 0) model.ParentID = null;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Taxonomy, model.ID, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, model.TaxonomyTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<Taxonomy>(model, fields);

                dynamic custom = new
                {
                    TaxonomyTypeID = model.TaxonomyTypeID,
                    ParentID = model.ParentID,
                    Name = model.Name,
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

        #endregion

        #region TaxonomyType

        public class TaxonomyTypeModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string MaximumDepth { get; set; }
            public string Class { get; set; }
            public string IconBackColor { get; set; }
            public string IconForeColor { get; set; }
            public string ID { get; set; }
        }

        #region Field Generation

        [Route("TaxonomyType_AddFields")]
        public JsonResult TaxonomyType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = new TaxonomyType();
            var classes = Company.Table<TaxonomyTypeClass>().OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Class", Name = a.GetName(i => i.TaxonomyTypeClassID), FieldDescription = a.GetDescription(i => i.TaxonomyTypeClassID), FieldType = DataType.Lookup.ToString(), Items = classes });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MaximumDepth", Name = a.GetName(i => i.MaximumDepth), RangeMin = 1, RangeMax = 25, FieldDescription = a.GetDescription(i => i.MaximumDepth), FieldType = DataType.Number.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });
            loadIconFields(list, 4);

            a = null;
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyTypeID</param>
        [Route("TaxonomyType_DeleteFields"), NonNullableParameters]
        public JsonResult TaxonomyType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<TaxonomyType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyTypeID</param>
        [Route("TaxonomyType_EditFields"), NonNullableParameters]
        public JsonResult TaxonomyType_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<TaxonomyType>(id);
            var style = Company.GetObjectStyle(SystemObjects.TaxonomyType, id);
            var classes = Company.Table<TaxonomyTypeClass>().OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
            var maxLevel = Company.Query<int>("select coalesce(max([Level]), 1) from Taxonomy where TaxonomyTypeID = @t", new { t = id }).SingleOrDefault();

            var maxDepthNotification = (maxLevel > 1) ? string.Format("  The current depth of this model type's hierarchy is {0} levels, so you may not set a Maxiumum Depth less than that.", maxLevel) : "";

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Class", Name = a.GetName(i => i.TaxonomyTypeClassID), FieldDescription = a.GetDescription(i => i.TaxonomyTypeClassID), FieldType = DataType.Lookup.ToString(), Value = a.TaxonomyTypeClassID.ToString(), Items = classes });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MaximumDepth", Name = a.GetName(i => i.MaximumDepth), RangeMin = maxLevel, RangeMax = 25, FieldDescription = a.GetDescription(i => i.MaximumDepth) + maxDepthNotification, FieldType = DataType.Number.ToString(), Value = a.MaximumDepth.HasValue ? a.MaximumDepth.Value.ToString() : "5" });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });
            loadIconFields(list, 4, style);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post
        
        [HttpPost, ValidateInput(false), Route("AddTaxonomyTypeRaw")]
        public JsonResult AddTaxonomyTypeRaw(TaxonomyTypeModel taxonomyType)
        {
            var form = new FormCollection();
            form.Add("Name", taxonomyType.Name);
            form.Add("Description", taxonomyType.Description);
            form.Add("Class", taxonomyType.Class);
            form.Add("MaximumDepth", taxonomyType.MaximumDepth);
            form.Add("IconBackColor", taxonomyType.IconBackColor);
            form.Add("IconForeColor", taxonomyType.IconForeColor);            

            return AddTaxonomyType(form);            
        }

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddTaxonomyType")]
        public JsonResult AddTaxonomyType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.TaxonomyType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var a = new TaxonomyType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    TaxonomyTypeClassID = parseIntField(form, "Class"),
                    MaximumDepth = parseIntField(form, "MaximumDepth")
                };

                if (a.MaximumDepth <= 0 || a.MaximumDepth > 10) return jsonException("Invalid Maximum Model level specified must be a value between 1 and 10", HttpStatusCode.InternalServerError);

                Company.SaveOrUpdate<TaxonomyType>(a);

                for (int i = 1; i <= a.MaximumDepth; i++)
                {
                    Company.Set<TaxonomyTypeLevel>().Add(new TaxonomyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), TaxonomyTypeID = a.ID });
                }
                Company.SaveChanges();

                upsertObjectStyle(SystemObjects.TaxonomyType, a.ID, form, a.Name);

                

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

        [HttpDelete, Route("DeleteTaxonomyType")]
        public JsonResult DeleteTaxonomyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<TaxonomyType>(id);
                if (model == null) throw new NotFoundException("taxonomy type");

                if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<TaxonomyType>(i => i.ID == id);
                deleteObjectStyle(SystemObjects.TaxonomyType, id);

                

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
        
        [HttpPut, ValidateInput(false), Route("EditTaxonomyTypeRaw")]
        public JsonResult EditTaxonomyTypeRaw(TaxonomyTypeModel taxonomyType)
        {
            var form = new FormCollection();
            form.Add("Name", taxonomyType.Name);
            form.Add("Description", taxonomyType.Description);
            form.Add("Class", taxonomyType.Class);
            form.Add("MaximumDepth", taxonomyType.MaximumDepth);
            form.Add("IconBackColor", taxonomyType.IconBackColor);
            form.Add("IconForeColor", taxonomyType.IconForeColor);
            form.Add("ID", taxonomyType.ID);

            return EditTaxonomyType(form);
        }

        [HttpPut, ValidateInput(false), Route("EditTaxonomyType")]
        public JsonResult EditTaxonomyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<TaxonomyType>(id, i => i.TaxonomyTypeLevels);
                if (model == null) throw new NotFoundException("taxonomy type");

                var style = Company.GetObjectStyle(SystemObjects.TaxonomyType, id);

                if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.MaximumDepth = parseIntField(form, "MaximumDepth");
                model.TaxonomyTypeClassID = parseIntField(form, "Class");

                if (model.MaximumDepth <= 0 || model.MaximumDepth > 10) return jsonException("Invalid Maximum Model level specified must be a value between 1 and 10", HttpStatusCode.InternalServerError);

                var currentMaxLevel = Company.Query<int>("select coalesce(max([Level]), 0) from Taxonomy where TaxonomyTypeID = @t", new { t = id }).SingleOrDefault();

                if (currentMaxLevel > model.MaximumDepth)
                    throw new InvalidFieldException(d360.core.resources.Fields.MaximumDepth_Name, "less than the current maximum depth of " + currentMaxLevel);

                Company.SaveOrUpdate<TaxonomyType>(model);

                for (int i = 1; i <= model.MaximumDepth; i++)
                {
                    var level = model.TaxonomyTypeLevels.SingleOrDefault(l => l.Level == i);
                    if (level == null)
                    {
                        Company.Set<TaxonomyTypeLevel>().Add(new TaxonomyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), TaxonomyTypeID = model.ID });
                    }
                }
                Company.Delete<TaxonomyTypeLevel>(l => l.Level > model.MaximumDepth);
                Company.SaveChanges();

                upsertObjectStyle(SystemObjects.TaxonomyType, model.ID, form, model.Name);

                

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

        #region TaxonomyTypeClass

        #region Field Generation

        [Route("TaxonomyTypeClass_AddFields")]
        public JsonResult TaxonomyTypeClass_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyTypeClass, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = new TaxonomyTypeClass();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            a = null;
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyTypeClassID</param>
        [Route("TaxonomyTypeClass_DeleteFields"), NonNullableParameters]
        public JsonResult TaxonomyTypeClass_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyTypeClass, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<TaxonomyTypeClass>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyTypeClassID</param>
        [Route("TaxonomyTypeClass_EditFields"), NonNullableParameters]
        public JsonResult TaxonomyTypeClass_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyTypeClass, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<TaxonomyTypeClass>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddTaxonomyTypeClass")]
        public JsonResult AddTaxonomyTypeClass(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.TaxonomyTypeClass, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type class");

                var a = new TaxonomyTypeClass
                {
                    Name = parseTextField(form, "Name")
                };

                Company.SaveOrUpdate<TaxonomyTypeClass>(a);

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

        [HttpDelete, Route("DeleteTaxonomyTypeClass")]
        public JsonResult DeleteTaxonomyTypeClass(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type class");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<TaxonomyTypeClass>(id);
                if (model == null) throw new NotFoundException("taxonomy type class");

                if (!Company.HasPermission(SystemObjects.TaxonomyTypeClass, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<TaxonomyTypeClass>(i => i.ID == id);

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

        [HttpPut, ValidateInput(false), Route("EditTaxonomyTypeClass")]
        public JsonResult EditTaxonomyTypeClass(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type class");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<TaxonomyTypeClass>(id);
                if (model == null) throw new NotFoundException("taxonomy type class");

                if (!Company.HasPermission(SystemObjects.TaxonomyTypeClass, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");

                Company.SaveOrUpdate<TaxonomyTypeClass>(model);

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

        #region TaxonomyTypeLevel

        #region Field Generation

        [Route("TaxonomyTypeLevel_AddFields"), NonNullableParameters]
        public JsonResult TaxonomyTypeLevel_AddFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var type = Company.GetById<TaxonomyType>(id);
            if (type == null) return jsonException("Type not found.", HttpStatusCode.NotFound);
            var existingLevels = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id).Select(i => i.Level).ToList();

            var levels = new List<SelectListItem>();
            for (int i = 1; i <= type.MaximumDepth; i++)
            {
                if (!existingLevels.Contains(i)) levels.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString() });
            }

            var list = new List<EditableField>();
            var a = new TaxonomyTypeLevel();

            list.Add(new EditableField { Required = true, FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField
            {
                Group = "",
                Row = 1,
                Column = 2,
                Required = true,
                FieldName = "Level",
                Name = a.GetName(i => i.Level),
                Items = levels,
                FieldDescription = a.GetDescription(i => i.Level),
                FieldType = DataType.Lookup.ToString(),
                Validations = checkAndAddValidation("Text", "Level", true, "", 1, 250)
            });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            a = null;
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyTypeID</param>
        [Route("TaxonomyTypeLevel_DeleteFields"), NonNullableParameters]
        public JsonResult TaxonomyTypeLevel_DeleteFields(int id, int level)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { FieldName = "Level", FieldType = DataType.Hidden.ToString(), Value = level.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyTypeID</param>
        [Route("TaxonomyTypeLevel_EditFields"), NonNullableParameters]
        public JsonResult TaxonomyTypeLevel_EditFields(int id, int level)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id && i.Level == level).SingleOrDefault();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.TaxonomyTypeID.ToString() });
            list.Add(new EditableField { ReadOnly = true, FieldName = "Level", FieldType = DataType.Hidden.ToString(), Value = a.Level.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public class TaxonomyTypeLevelModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Level { get; set; }
            public string TaxonomyTypeID { get; set; }
        }
                
        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddTaxonomyTypeLevel")]
        public JsonResult AddTaxonomyTypeLevel(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Create, ClaimObject.Root))
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

        [HttpDelete, Route("TaxonomyType/{taxonomyTypeId:int}/levels/{taxonomyTypeLevelId:int}")]        
        public JsonResult DeleteTaxonomyTypeLevel(int taxonomyTypeId, int taxonomyTypeLevelId)
        {
            try
            {                
                var id = taxonomyTypeId;
                var level = taxonomyTypeLevelId;

                if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Delete))
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

                if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Update))
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

        #region TooltipTemplate

        #region Field Generation

        [Route("TooltipTemplate_AddFields")]
        public JsonResult TooltipTemplate_AddFields()
        {
            var list = new List<EditableField>();

            var names = Enum.GetNames(typeof(SystemObjects)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();
            var actions = new List<SelectListItem>();
            actions.Add(new SelectListItem { Text = "Preview", Value = "Preview" });
            actions.Add(new SelectListItem { Text = "Assigning Item Preview", Value = "AssigningItemPreview" });
            actions.Add(new SelectListItem { Text = "Lookup Preview", Value = "LookupPreview" });
            actions.Add(new SelectListItem { Text = "View Statistics", Value = "Statistics" });

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Lookup.ToString(), Items = names });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Action", Name = "Action", FieldType = DataType.Lookup.ToString(), Items = actions });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TemplateBody", Name = "Body", FieldType = DataType.Html.ToString(), Validations = checkAndAddValidation("Text", "Body", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TooltipTemplateID</param>
        [Route("TooltipTemplate_DeleteFields"), NonNullableParameters]
        public JsonResult TooltipTemplate_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<TooltipTemplate>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TooltipTemplateID</param>
        [Route("TooltipTemplate_EditFields"), NonNullableParameters]
        public JsonResult TooltipTemplate_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<TooltipTemplate>(id);

            var names = Enum.GetNames(typeof(SystemObjects)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();

            var actions = new List<SelectListItem>();
            actions.Add(new SelectListItem { Text = "Preview", Value = "Preview" });
            actions.Add(new SelectListItem { Text = "Assigning Item Preview", Value = "AssigningItemPreview" });
            actions.Add(new SelectListItem { Text = "Lookup Preview", Value = "LookupPreview" });
            actions.Add(new SelectListItem { Text = "View Statistics", Value = "Statistics" });

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Lookup.ToString(), Items = names, Value = a.Name });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Action", Name = "Action", FieldType = DataType.Lookup.ToString(), Items = actions, Value = a.Action });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TemplateBody", Name = "Body", FieldType = DataType.Html.ToString(), Value = a.TemplateBody, Validations = checkAndAddValidation("Text", "Body", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, ValidateInput(false), Route("AddTooltipTemplateRaw")]
        public JsonResult AddTooltipTemplateRaw(TemplateModel template)
        {
            var form = new FormCollection();            
            form.Add("Name", template.Name);
            form.Add("Description", template.Description);
            form.Add("TemplateBody", template.TemplateBody);
            form.Add("Action", template.Action);

            return AddTooltipTemplate(form);
        }

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddTooltipTemplate")]
        public JsonResult AddTooltipTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("tooltip template");

                var a = new TooltipTemplate
                {
                    Action = parseTextField(form, "Action"),
                    Description = parseTextField(form, "Description"),
                    Name = parseTextField(form, "Name"),
                    TemplateBody = parseTextField(form, "TemplateBody")
                };

                Company.Add<TooltipTemplate>(a);

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
 
        [HttpDelete, Route("DeleteTooltipTemplate")]
        public JsonResult DeleteTooltipTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("tooltip template");

                var id = parseIntField(form, "ID");
                Company.Delete<TooltipTemplate>(i => i.ID == id);

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

        public class TemplateModel
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Action { get; set; }
            public string TemplateBody { get; set; }
        }

        [HttpPut, ValidateInput(false), Route("EditTooltipTemplateRaw")]
        public JsonResult EditTooltipTemplateRaw(TemplateModel template)        
        {            
                        
            var form = new FormCollection();
            form.Add("ID", template.ID);
            form.Add("Name", template.Name);
            form.Add("Description", template.Description);
            form.Add("TemplateBody", template.TemplateBody);
            form.Add("Action", template.Action);

            return EditTooltipTemplate(form);
        }

        [HttpPut, ValidateInput(false), Route("EditTooltipTemplate")]
        public JsonResult EditTooltipTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("tooltip template");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<TooltipTemplate>(id);
                if (model == null) throw new NotFoundException("tooltip template");

                model.Action = parseTextField(form, "Action");
                model.Description = parseTextField(form, "Description");
                model.Name = parseTextField(form, "Name");
                model.TemplateBody = parseTextField(form, "TemplateBody");

                Company.Update<TooltipTemplate>(model);

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
        
        
        #region Reference Item Types


        [HttpPut, ValidateInput(false), Route("EditReferenceItemType")]
        public JsonResult EditReferenceItemType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ReferenceItemType");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReferenceItemType>(id);
                if (model == null) throw new NotFoundException("ReferenceItemType");

                if ((!Company.HasPermission(SystemObjects.ReferenceItemType, id, Claim.Update)) && (!Company.HasPermission(SystemObjects.ReferenceItemType, 0, Claim.Update)))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.DisplayFormat = parseTextField(form, "DisplayFormat");
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update<ReferenceItemType>(model);

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


        [HttpDelete, Route("DeleteReferenceItemType")]
        public JsonResult DeleteReferenceItemType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ReferenceItemType");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReferenceItemType>(id);
                if (model == null) throw new NotFoundException("ReferenceItemType");

                if (!Company.HasPermission(SystemObjects.ReferenceItemType, 0, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    Name = model.Name,
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddReferenceItemType")]
        public JsonResult AddReferenceItemType(FormCollection form)
        {
            try
            {                
                if (!Company.HasPermission(SystemObjects.ReferenceItemType, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("ReferenceItemType");

                var model = new ReferenceItemType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    DisplayFormat = parseTextField(form, "DisplayFormat"),
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow
                };

                Company.Add<ReferenceItemType>(model);
                
                if (model.ID > 0)
                {
                    Company.Add<FieldType>(new FieldType
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


        /// <param name="id">LookupTypeID</param>
        [Route("ReferenceItem_AddFields"), NonNullableParameters]
        public JsonResult ReferenceItem_AddFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.ReferenceItemType, id, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var type = Company.GetById<ReferenceItemType>(id);

            list.Add(new EditableField { FieldName = "ReferenceItemTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, id).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("ReferenceItem_DeleteFields"), NonNullableParameters]
        public JsonResult ReferenceItem_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ReferenceItem>(id);

            if (!Company.HasPermission(SystemObjects.ReferenceItemType, a.ReferenceItemTypeID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
        [Route("ReferenceItem_EditFields"), NonNullableParameters]
        public JsonResult ReferenceItem_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ReferenceItem>(id);

            if (!Company.HasPermission(SystemObjects.ReferenceItemType, a.ReferenceItemTypeID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString(), Value = a.Code.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, a.ReferenceItemTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.ReferenceItem, id).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
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

                if (!Company.HasPermission(SystemObjects.ReferenceItemType, model.ReferenceItemTypeID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<ReferenceItem>(model);

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


        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddReferenceItem")]
        public JsonResult AddReferenceItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup");

                int typeID = parseIntField(form, "ReferenceItemTypeID");
                var type = Company.GetById<ReferenceItemType>(typeID);

                if (type == null) throw new NotFoundException("referenceitemtype");

                if (!Company.HasPermission(SystemObjects.ReferenceItemType, typeID, Claim.Create))
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
                    UpdatedOn = DateTime.UtcNow//,
                    //DisplayValue = 
                };

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.ReferenceItem, a.ID, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate<ReferenceItem>(a, fields);

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


        [HttpPut, ValidateInput(false), Route("EditReferenceItem")]
        public JsonResult EditReferenceItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("referenceitem");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReferenceItem>(id);

                if (model == null) throw new NotFoundException("referenceitem");

                if (!Company.HasPermission(SystemObjects.ReferenceItemType, model.ReferenceItemTypeID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var code = form["Code"].ToString();

                if (Company.Any<ReferenceItem>(r => r.ReferenceItemTypeID == model.ReferenceItemTypeID && r.Code == code && r.ID != model.ID))
                    return jsonException(new Exception($"A reference item with the code value {code} already exists."), HttpStatusCode.Forbidden);

                model.Code = code;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.ReferenceItem, model.ID, Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, model.ReferenceItemTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<ReferenceItem>(model, fields);

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

        #region Fusion Schedule

        [HttpDelete, Route("DeleteFusionSchedule")]
        public JsonResult DeleteFusionSchedule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("delete fusion schedule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<core.entities.FusionSchedule>(id);
                if (model == null) throw new NotFoundException("fusion schedule");

                if (!Company.HasPermission(SystemObjects.Fusion, model.FusionID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<core.entities.FusionSchedule>(i => i.ID == id);

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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFusionSchedule")]
        public JsonResult AddFusionSchedule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusionschedule");

                if (!Company.HasPermission(SystemObjects.Fusion, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

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
                
                Company.Add<FusionSchedule>(a);

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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("EditFusionSchedule")]
        public JsonResult EditFusionSchedule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusionschedule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FusionSchedule>(id);

                if (model == null) throw new NotFoundException("issuetype");

                if (!Company.HasPermission(SystemObjects.Fusion, model.FusionID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.FullRefresh = parseBooleanField(form, "FullRefresh");
                model.Day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), form["Day"]);
                model.Time = TimeSpan.Parse(parseTextField(form, "Time"));                
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.Update<core.entities.FusionSchedule>(model);

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

                if (!Company.HasPermission(SystemObjects.Fusion, model.FusionID, Claim.Delete))
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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddFusionAttributeTypeCustomQuery")]
        public JsonResult AddFusionAttributeTypeCustomQuery(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusionattributetypecustomquery");

                if (!Company.HasPermission(SystemObjects.Fusion, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new FusionAttributeTypeCustomQuery
                {
                    FusionID = parseIntField(form, "FusionID"),
                    FusionAttributeTypeID = parseIntField(form, "FusionAttributeTypeID"),
                    Query = parseTextField(form, "Query")
                };

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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("EditFusionAttributeTypeCustomQuery")]
        public JsonResult EditFusionAttributeTypeCustomQuery(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusionattributetypecustomquery");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FusionAttributeTypeCustomQuery>(id);

                if (model == null) throw new NotFoundException("fusionattributetypecustomquery");

                if (!Company.HasPermission(SystemObjects.Fusion, model.FusionID, Claim.Update))
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

        #region Issue Types

        [Route("IssueType_EditFields"), NonNullableParameters]
        public JsonResult IssueType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<core.entities.IssueType>(id);

            if (!Company.HasPermission(SystemObjects.IssueType, a.ID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        
        [Route("IssueType_AddFields"), NonNullableParameters]
        public JsonResult IssueType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.IssueType, 0, Claim.Create))
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
            var type = Company.GetById<core.entities.IssueType>(issueTypeId);

            if (type == null) throw new NotFoundException("issuetype");

            list.Add(new EditableField { FieldName = "IssueTypeID", FieldType = DataType.Hidden.ToString(), Value = issueTypeId.ToString() });

            var names = Enum.GetNames(typeof(IssueCriticality)).Select(i => new SelectListItem { Text = i, Value = i }).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Criticality", Name = "Criticality", Required = true, FieldType = DataType.Lookup.ToString(), Items = names });            
            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddIssue")]
        public JsonResult AddIssue(FormCollection form)
        {
            try
            {
                var issueTypeId = parseIntField(form, "IssueTypeID");
                var objectId = parseIntField(form, "ObjectID");
                var objectType = parseTextField(form, "ObjectType");
                var desc = parseTextField(form, "ProblemDesc");
                IssueCriticality criticality =  (IssueCriticality)Enum.Parse(typeof(IssueCriticality), parseTextField(form, "Criticality"));

                var issueType = Company.GetById<core.entities.IssueType>(issueTypeId);

                if (issueType == null) throw new NoFormDataException("IssueType");

                //get the object name
                var obj = Company.GetObjectDetail(objectType, objectId);

                if (obj == null) throw new NoFormDataException("GetObject");

                var relations = new List<CommentRelation>();
                var resourceRelation = new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow };
                var comment = new Comment();

                relations.Add(new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow });

                comment.OwnerObjectType = SystemObjects.Resource.ToString();
                comment.OwnerObjectID = Company.CurrentResourceID;
                comment.CommentTypeID = CommentType.Issue;
                comment.Body = desc ?? "";
                

                //add relation to current artifact
                relations.Add(new CommentRelation { ObjectType = objectType, ObjectID = objectId, Date = DateTime.UtcNow });

                var dtl = Company.AddComment(comment, relations).FirstOrDefault(i => i.ID == comment.ID);

                //insert issue into issue table
                var model = new Issue
                {
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                    IssueTypeID = issueTypeId,
                    Criticality = criticality,
                    Object = objectType,
                    ObjectID = objectId,
                    ObjectType = obj.Type,
                    ObjectTypeID = obj.TypeID,
                    CommentID = dtl.ID
                };


                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Issue, model.ID, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList(), form, Server);
                Company.SaveOrUpdate<Issue>(model, fields);

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

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddIssueType")]
        public JsonResult AddIssueType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.IssueType, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("IssueType");

                var model = new core.entities.IssueType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),  
                    IsSystem = false,                  
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow                    
                };

                Company.Add<core.entities.IssueType>(model);

                if (model.ID > 0)
                {
                    Company.Add<FieldType>(new FieldType
                    {
                        ObjectID = model.ID,
                        Object = SystemObjects.IssueType.ToString(),
                        IsListable = true,
                        IsRequired = true,
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
                if (!form.HasKeys()) throw new NoFormDataException("issuetype");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<core.entities.IssueType>(id);

                if (model == null) throw new NotFoundException("issuetype");

                if (!Company.HasPermission(SystemObjects.IssueType, model.ID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = form["Name"];
                model.Description = form["Description"];
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.SaveOrUpdate<core.entities.IssueType>(model);

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
                if (!form.HasKeys()) throw new NoFormDataException("issue type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<core.entities.IssueType>(id);
                if (model == null) throw new NotFoundException("issue type");

                if (!Company.HasPermission(SystemObjects.IssueType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<core.entities.IssueType>(i => i.ID == id);
                
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

        #region Lineage Mapping

        [Route("Map_AddFields"), NonNullableParameters]
        public JsonResult Map_AddFields()
        {
            var list = new List<EditableField>();
            
            if (!Company.HasPermission(SystemObjects.Map, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

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

            if (!Company.HasPermission(SystemObjects.Map, a.ID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var mapTypes = new List<SelectListItem>();

            foreach (var item in Company.MapTypes)
            {
                mapTypes.Add(new SelectListItem { Text = item.Name, Value = item.ID.ToString() });
            }

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Transformation", Name = "Transformation", FieldType = DataType.Text.ToString(), Value = a.Transformation });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "MapType", Name = "Type", FieldType = DataType.Lookup.ToString(), Items = mapTypes, Value = a.MapTypeID.ToString() });            

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false), Route("AddMap")]
        public JsonResult AddMap(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Map, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("Map");

                var map = new Map
                {
                    Name = parseTextField(form,"Name"),
                    Transformation= parseTextField(form, "Transform"),
                    MapTypeID = parseIntField(form, "MapType"),
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                Company.Add<Map>(map);

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

        [ValidateHttpAntiForgeryToken, HttpPut, ValidateInput(false), Route("EditMap")]
        public JsonResult EditMap(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Map");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<core.entities.Map>(id);
                
                if (!Company.HasPermission(SystemObjects.Map, 0, Claim.Update, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                
                model.Name = parseTextField(form, "Name");
                model.Transformation = parseTextField(form, "Transform");                
                model.MapTypeID = parseIntField(form, "MapType");                
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;
                
                Company.Update<Map>(model);

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
                var model = Company.GetById<core.entities.Map>(id);
                if (model == null) throw new NotFoundException("mapping");

                if (!Company.HasPermission(SystemObjects.Map, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<core.entities.Map>(i => i.ID == id);

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
    }
}
