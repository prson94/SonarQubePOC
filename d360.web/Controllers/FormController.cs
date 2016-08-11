using d360.core;
using d360.core.entities;
using d360.core.exceptions;
using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Xml.Linq;
using d360.extensions;
using Resources;
using d360.core.enums;
using d360.model;
using System.IO;
using SpreadsheetLight;
using d360.core.entities.Views;
using d360.workflow.models;
using d360.workflow;
using d360.workflow.entities;
using d360.web.Filters;
using d360.web.Models.Attributes;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using d360.extensions.powerbi;
using System.Web;
using d360.core.queue;

namespace d360.web.Controllers
{
    [ValidateHttpAntiForgeryToken]
    [RoutePrefix("form"), Authorize]
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
                Value = value
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
                case SystemObjects.Event:
                    statusList.Add(new SelectListItem { Text = "Open", Value = "Open" });
                    statusList.Add(new SelectListItem { Text = "Assigned", Value = "Assigned" });
                    statusList.Add(new SelectListItem { Text = "Closed", Value = "Closed" });
                    break;
            }
            f.Items.AddRange(statusList);

            list.Add(f);


            return list;
        }

        List<SelectListItem> convertToEditableFieldItems(List<FieldTypeLookupValue> items, string selectedValue = "", bool appendType = true)
        {
            return items
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID),
                    Selected = string.IsNullOrEmpty(selectedValue) ? false : selectedValue.Equals(string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID))
                })
                .ToList();
        }

        List<SelectListItem> convertToEditableFieldItems(List<FieldNameByObjectType> items, string selectedValue = "")
        {
            return items
                .Select(i => new SelectListItem
                {
                    Text = string.Format("{0}", i.Name),
                    Value = string.Format("{0}|{1}", i.Name, i.IsCustomField),
                    Selected = string.IsNullOrEmpty(selectedValue) ? false : selectedValue.Equals(string.Format("{0}|{1}", i.Name, i.IsCustomField))
                })
                .ToList();
        }
        
        #endregion

        #region Json Message Handling

        JsonResult jsonException(Exception ex, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            //Response.StatusCode = (int)statusCode;
            //Response.StatusDescription = ex.GetFullExceptionData();//.Replace("\n", "  ").Replace("\r", " ");
            return Json(new { type = "error", title = title, message = ex.GetFullExceptionData() }, JsonRequestBehavior.AllowGet);
        }

        JsonResult jsonException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            //Response.StatusCode = (int)statusCode;
            //Response.StatusDescription = message.Replace("\n", "  ").Replace("\r", " ");
            return Json(new { type = "error", title = title, message = message }, JsonRequestBehavior.AllowGet);
        }

        JsonNetResult jsonNetException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            //Response.StatusCode = (int)statusCode;
            //Response.StatusDescription = message.Replace("\n", "  ").Replace("\r", " ");
            return new JsonNetResult
            {
                Data = new { type = "error", title = title, message = message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        JsonResult jsonSuccess(string message, string id, string context, string action, HttpStatusCode statusCode, dynamic customdata)
        {
            Response.StatusCode = (int)statusCode;
            Response.StatusDescription = message.Replace("\n", "  ");
            return Json(new { type = "confirm", title = "Success!", action = action, message = message.Replace("\n", "  "), id = id, context = context, custom = customdata }, JsonRequestBehavior.AllowGet);
        }

        JsonResult jsonSuccess(string message, string id, string context, string action, HttpStatusCode statusCode)
        {
            return jsonSuccess(message, id, context, action, statusCode, null);
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

        #endregion

        #region Dynamic Editor Field Type Information For Angular2

        [HttpGet, Route("dynamiceditor/edit/{objectType}/{ID:int}")]
        public JsonResult DynamicEditorEditFields(string objectType, int ID)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "LOOKUPTYPE":
                    return Lookup_EditFields(ID);
                case "RULEDIMENSION":
                    return RuleDimension_EditFields(ID);
                case "POLICYTYPE":
                    return PolicyType_EditFields(ID);
                case "PREDICATE":
                    return Predicate_EditFields(ID);
                case "RESOURCETYPE":
                    return Resource_EditFields(ID);
                case "DOMAINTYPE":
                    return DomainType_EditFields(ID);
                case "FUSION":
                    return Fusion_EditFields(ID);
                case "ARTIFACT":
                    return Artifact_EditFields(ID);
                case "RULE":
                    return Rule_EditFields(ID);
                case "SURVEYTYPE":
                    return SurveyType_EditFields(ID);
                case "INTERSECTTYPE":
                    return Relationship_EditFields(ID);
            }
            throw new Exception("Invalid or non implemented editor type");
        }

        [HttpGet, Route("dynamiceditor/new/{objectType}/{objectID?}/{parentID?}/{typeID?}")]
        public JsonResult DynamicEditorAddFields(string objectType, int? objectID, int? parentID, int? typeID)
        {
            switch ((objectType ?? "").ToUpper())
            {
                case "LOOKUPTYPE":
                    return Lookup_AddFields(objectID.GetValueOrDefault());
                case "RULEDIMENSION":
                    return RuleDimension_AddFields();
                case "RULETYPE":
                    return Rule_AddFields();
                case "POLICYTYPE":
                    return PolicyType_AddFields();
                case "PREDICATE":
                    return Predicate_AddFields();
                case "RESOURCETYPE":
                    return Resource_AddFields(objectID.GetValueOrDefault());
                case "DOMAINTYPE":
                    return DomainType_AddFields();
                case "FUSION":
                    return Fusion_AddFields(objectID.GetValueOrDefault());
                case "ARTIFACT":
                    return Artifact_AddFields(objectID.GetValueOrDefault(), parentID.GetValueOrDefault());
                case "ATTRIBUTE":
                    return Attribute_AddFields(typeID.GetValueOrDefault(),objectType,objectID.GetValueOrDefault(),parentID.GetValueOrDefault());
                case "RULE":
                    return Rule_AddFields();
                case "SURVEYTYPE":
                    return SurveyType_AddFields();
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
                case "LOOKUP":
                    return EditLookup(form);                
                case "RULEDIMENSION":
                    return EditRuleDimension(form);
                case "POLICYTYPE":
                    return EditPolicyType(form);    
                case "PREDICATE":
                    return EditPredicate(form);
                case "RESOURCE":
                    return EditResource(form);
                case "STATISTICTYPE":
                    return EditStatisticType(form);
                case "DOMAINTYPE":
                    return EditDomainType(form);
                case "FUSION":
                    return EditFusion(form);
                case "INTERSECTTYPE":
                    return EditIntersectType(form);
                case "REPORT":
                    return await EditReport(form);
                case "REPORTTILE":
                    return EditReportTile(form);
                case "ATTRIBUTETYPE":
                    return EditAttributeType(form);
                case "ARTIFACT":
                    return EditArtifact(form);
                case "RULE":
                    return EditRule(form);
                case "SURVEYTYPE":
                    return EditSurveyType(form);
                case "INTERSECT":
                    return EditRelationship(form);
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
                case "RULEDIMENSION":
                    return DeleteRuleDimension(form);
                case "POLICYTYPE":
                    return DeletePolicyType(form);
                case "PREDICATE":
                    return DeletePredicate(form);
                case "STATISTICTYPE":
                    return DeleteStatisticType(form);
                case "INTERSECTTYPE":
                    return DeleteIntersectType(form);
                case "REPORT":
                    return DeleteReport(form);
                case "REPORTTILE":
                    return DeleteReportTile(form);
                case "ATTRIBUTETYPE":
                    return DeleteAttributeType(form);
                case "ARTIFACT":
                    return DeleteArtifact(form);
                case "RULE":
                    return DeleteRule(form);
                case "SURVEYTYPE":
                    return DeleteSurveyType(form);
                case "SURVEYQUESTIONTYPE":
                    return DeleteQuestionType(form);                
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
                case "LOOKUP":
                    return AddLookup(form);
                case "RULEDIMENSION":
                    return AddRuleDimension(form);
                case "POLICYTYPE":
                    return AddPolicyType(form);
                case "PREDICATE":
                    return AddPredicate(form);
                case "RESOURCE":
                    return AddResource(form);                             
                case "STATISTICTYPE":
                    return AddStatisticType(form);
                case "DOMAINTYPE":
                    return AddDomainType(form);
                case "FUSION":
                    return AddFusion(form);
                case "INTERSECTTYPE":
                    return AddIntersectType(form);
                case "REPORT":
                    return await AddReport(form);
                case "REPORTTILE":
                    return AddReportTile(form);
                case "ATTRIBUTETYPE":
                    return AddAttributeType(form);
                case "ARTIFACT":
                    return AddArtifact(form);
                case "ATTRIBUTE":
                    return AddAttribute(form);
                case "RULE":
                    return AddRule(form);
                case "SURVEYTYPE":
                    return AddSurveyType(form);
                case "INTERSECT":
                    return AddRelationship(form);              
            }

            throw new Exception("Invalid / unsupported create type");
        }

        #endregion

        #region Artifact

        #region Field Generation

        /// <param name="at">ArtifactTypeID</param>
        /// <param name="p">ParentID</param>
        public JsonResult Artifact_AddFields(int at, int p)
        {
            if (!Company.HasPermission(SystemObjects.ArtifactType, at, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var type = Company.GetById<ArtifactType>(at, i => i.Parent);
            var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == type.ID && (i.WorkflowType == WorkflowType.SuggestNewArtifact || i.WorkflowType == WorkflowType.SuggestNewArtifactMulti)).Any();

            list.Add(new EditableField { FieldName = "ArtifactTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });

            var row = 1;

            if (p == 0 && type.ParentID.HasValue)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                var parents = Company.Filter<Artifact>(i => i.ArtifactTypeID == type.ParentID).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = false }).ToList();
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
                Items = Company.Table<TaxonomyType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList(),
                Value = parentTaxonomyId.ToString()
            });
            row++;

            list.Add(new EditableField { Row = row, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            if (!workflowEnabled)
            {
                row++;
                list = loadStatusField(list, SystemObjects.Artifact, null, row, 1);
            }

            row++;
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, at).ToList(), row + 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ArtifactID</param>
        public JsonResult Artifact_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Delete, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ArtifactID</param>
        public JsonResult Artifact_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Update, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Artifact>(id);
            var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == a.ArtifactTypeID && (i.WorkflowType == WorkflowType.SuggestNewArtifact || i.WorkflowType == WorkflowType.SuggestNewArtifactMulti)).Any();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            if (a.ParentID.HasValue)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                var currentParent = Company.GetById<Artifact>(a.ParentID.Value);
                var parents = Company.Filter<Artifact>(i => i.ArtifactTypeID == currentParent.ArtifactTypeID).OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
                list.Add(new EditableField { Row = 1, Column = 1, FieldName = "ParentID", Name = $"Parent {pluralize.Singularize(currentParent.ArtifactType.Name)}", FieldType = DataType.Lookup.ToString(), Value = a.ParentID.ToString(), Items = parents });
                currentParent = null;
                pluralize = null;
            }

            bool isPromoted = Company.Filter<FusionAttributePromotion>(i => i.ObjectType == "Artifact" && i.ObjectID == id).Any();

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, ReadOnly = isPromoted, FieldName = "Name", Name = "Name", FieldDescription = ((isPromoted) ? "Artifact promoted via Fusion.  No changes allowed to the Name." : ""), FieldType = DataType.Text.ToString(), Value = Server.HtmlDecode(a.Name), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "TaxonomyTypeID", Name = Resources.FieldInfo.TaxonomyType_Name, ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID", FieldDescription = Resources.FieldInfo.TaxonomyType_Description, FieldType = DataType.Lookup.ToString(), Value = a.TaxonomyTypeID.ToString(), Items = Company.Table<TaxonomyType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });

            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            if (!workflowEnabled)
                list = loadStatusField(list, SystemObjects.Artifact, a.Status, 4, 1);

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, a.ArtifactTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Artifact, id).ToList(), 5, true);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ID</param>
        public JsonResult Artifact_RequestCertification(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            if (!Company.GetResponsibleResourcesByArtifactAndWorkflowType(WorkflowType.CertifyArtifact, id).Any())
            {
                return jsonException(FormInfo.Workflow_Certification_Request_Error, HttpStatusCode.Forbidden);
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ID</param>
        public JsonResult Artifact_Challenge(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Reason", Name = "Reason", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ID</param>
        public JsonResult Artifact_RaiseIssue(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Issue", Name = "Issue", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="at">ArtifactTypeID</param>
        /// <param name="p">ParentID</param>
        public JsonResult Artifact_SuggestFields(int at, int p)
        {
            var list = new List<EditableField>();

            var type = Company.GetById<ArtifactType>(at, i => i.Parent);

            list.Add(new EditableField { FieldName = "ArtifactTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });

            var row = 1;

            if (p == 0 && type.ParentID.HasValue)
            {
                var parents = Company.Filter<Artifact>(i => i.ArtifactTypeID == type.ParentID).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = false }).ToList();
                list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "ParentID", FieldType = DataType.Lookup.ToString(), Items = parents, Name = string.Format("Parent {0}", type.Parent.Name) });
                row++;
            }
            else
            {
                list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });
            }

            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = $"/form/Aritfact_SimilarItems?typeID={at}&query=" });
            list.Add(new EditableField { Row = row, Column = 2, Required = true, FieldName = "TaxonomyTypeID", ScriptProperty = "CompanySettings.ArtifactType_TaxonomyTypeID", Name = Resources.FieldInfo.TaxonomyType_Name, FieldDescription = Resources.FieldInfo.TaxonomyType_Description, FieldType = DataType.Lookup.ToString(), Items = Company.Table<TaxonomyType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });
            row++;
            list.Add(new EditableField { Row = row, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            row++;
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, at).ToList(), row + 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonNetResult Aritfact_SimilarItems(int typeID, string query)
        {
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.SimilarItems, new { type = "Artifact", typeID, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        #endregion

        #region Form Get/Post

        [Route("artifacts/{typeID:int}/add/{parentID:int=0}")]
        public ActionResult AddArtifact(int typeID, int parentID)
        {
            var type = Company.GetById<ArtifactType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Artifact,
                FieldUri = string.Format("/form/Artifact_AddFields?at={0}&p={1}", typeID, parentID),
                FormTitle = string.Format(Resources.FormInfo.Add_Generic_Title, type.Name),
                FormUri = "/form/AddArtifact",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == typeID && (i.WorkflowType == WorkflowType.SuggestNewArtifact || i.WorkflowType == WorkflowType.SuggestNewArtifactMulti)).Any();

                int taxonomyTypeID = parseIntField(form, "TaxonomyTypeID");

                var model = new Artifact();
                // Static fields
                model.ArtifactTypeID = typeID;
                model.TaxonomyTypeID = taxonomyTypeID;
                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                model.Status = (workflowEnabled) ? "Draft" : form["Status"];

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    model.ParentID = parseIntField(form, "ParentID");
                    if (model.ParentID == 0) model.ParentID = null;
                }

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate<Artifact>(model, fields);

                return jsonSuccess(type.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = model.ID });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("artifacts/{typeID:int}/{id:int}/delete")]
        public ActionResult DeleteArtifact(int typeID, int id, string context = "")
        {
            var a = Company.GetById<Artifact>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Artifact,
                FieldUri = string.Format("/form/Artifact_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteArtifact",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, new { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = id });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("artifacts/{typeID:int}/{id:int}/edit")]
        public ActionResult EditArtifact(int typeID, int id)
        {
            var a = Company.GetById<Artifact>(id, i => i.ArtifactType);
            if (a == null) return HttpNotFound();

            var model = new EditableForm
            {
                Context = ContextList.Artifact,
                FieldUri = string.Format("/form/Artifact_EditFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Edit_Generic_Title, a.ArtifactType.Name),
                FormUri = "/form/EditArtifact",
                FormMethod = "PUT"
            };

            //if (a.Locked) model.FormDescription = string.Format("NOTE: {0} was promoted via Fusion. Certain fields may not be editable.", a.Name);

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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
                bool isPromoted = Company.Filter<FusionAttributePromotion>(i => i.ObjectType == sType && i.ObjectID == id).Any();

                var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == model.ArtifactTypeID && (i.WorkflowType == WorkflowType.SuggestNewArtifact || i.WorkflowType == WorkflowType.SuggestNewArtifactMulti)).Any();


                // Static fields
                if (!isPromoted) model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.TaxonomyTypeID = parseIntField(form, "TaxonomyTypeID");
                if (!workflowEnabled) model.Status = form["Status"];

                //model.TaxonomyTypeID = string.IsNullOrEmpty(form["TaxonomyTypeID"]) ? new Nullable<int>() : parseIntField(form, "TaxonomyTypeID");
                model.ParentID = parseIntField(form, "ParentID");
                if (model.ParentID == 0) model.ParentID = null;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, model.ArtifactTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<Artifact>(model, fields);

                return jsonSuccess(model.ArtifactType.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = id });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult Challenge(int id)
        {
            var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);
            if (artifact == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "Challenge",
                FieldUri = string.Format("/form/Artifact_Challenge?id={0}", id),
                FormTitle = string.Format("Challenge {0}", artifact.Name),
                FormDescription = string.Format("This {0} challenge will be sent to the owner for further review.", artifact.ArtifactType.Name.ToLower()),
                FormUri = "/form/Challenge",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        public ActionResult RaiseIssue(int id)
        {
            var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);
            if (artifact == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "Issue",
                FieldUri = string.Format("/form/Artifact_RaiseIssue?id={0}", id),
                FormTitle = string.Format("Raise issue for {0}", artifact.Name),
                FormDescription = string.Format("This {0} issue will be sent to the owner for further review.", artifact.ArtifactType.Name.ToLower()),
                FormUri = "/form/RaiseIssue",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }


        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult Challenge(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int id = parseIntField(form, "ID");
                string challengeReason = parseTextField(form, "Reason");

                var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);

                if (artifact == null) throw new NotFoundException("artifact");

                var relations = new List<CommentRelation>();
                var resourceRelation = new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow };
                var comment = new Comment();

                relations.Add(new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow });

                comment.OwnerObjectType = SystemObjects.Resource.ToString();
                comment.OwnerObjectID = Company.CurrentResourceID;
                comment.CommentTypeID = CommentType.Challenge;
                comment.Body = challengeReason;

                //add relation to current artifact
                relations.Add(new CommentRelation { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = id, Date = DateTime.UtcNow });

                var dtl = Company.AddComment(comment, relations).FirstOrDefault(i => i.ID == comment.ID);

                if (dtl != null)
                {
                    var processor = new Processor();
                    var dictionary = new Dictionary<string, object>();
                    dictionary.Add("CompanyID", Company.CurrentCompanyID);
                    dictionary.Add("requestInfo",
                        new ChallengeRequest
                        {
                            ArtifactID = id,
                            ArtifactTypeID = artifact.ArtifactTypeID,
                            RequestingResourceID = Company.CurrentResourceID,
                            ArtifactTypeName = artifact.ArtifactType.Name,
                            Name = artifact.Name,
                            Reason = challengeReason,
                            CommentID = dtl.ID
                        }
                   );

                    processor.CreateNewWorkflowInstance(WorkflowVersionMap.ChallengeArtifact_vCurrent, dictionary);
                }

                return jsonSuccess("Request successfully created.", "", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult RaiseIssue(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int id = parseIntField(form, "ID");
                string challengeReason = parseTextField(form, "Issue");

                var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);

                if (artifact == null) throw new NotFoundException("artifact");

                var relations = new List<CommentRelation>();
                var resourceRelation = new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow };
                var comment = new Comment();

                relations.Add(new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow });

                comment.OwnerObjectType = SystemObjects.Resource.ToString();
                comment.OwnerObjectID = Company.CurrentResourceID;
                comment.CommentTypeID = CommentType.Issue;
                comment.Body = challengeReason;

                //add relation to current artifact
                relations.Add(new CommentRelation { ObjectType = SystemObjects.Artifact.ToString(), ObjectID = id, Date = DateTime.UtcNow });

                var dtl = Company.AddComment(comment, relations).FirstOrDefault(i => i.ID == comment.ID);

                if (dtl != null)
                {
                    var processor = new Processor();
                    var dictionary = new Dictionary<string, object>();
                    dictionary.Add("CompanyID", Company.CurrentCompanyID);
                    dictionary.Add("CommentID", dtl.ID);

                    processor.CreateNewWorkflowInstance(WorkflowVersionMap.WorkIssue_vCurrent, dictionary);
                }

                return jsonSuccess("Request successfully created.", "", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult RequestCertification(int id)
        {
            var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);
            if (artifact == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "RequestCertification",
                FieldUri = string.Format("/form/Artifact_RequestCertification?id={0}", id),
                FormTitle = string.Format("Request Certification for {0}", artifact.Name),
                FormDescription = string.Format("This {0} will be sent to the appropriate people for certification.", artifact.ArtifactType.Name.ToLower()),
                FormUri = "/form/RequestCertification",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost]
        public JsonResult RequestCertification(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int id = parseIntField(form, "ID");
                var artifact = Company.GetById<Artifact>(id, i => i.ArtifactType);

                if (artifact == null) throw new NotFoundException("artifact");
                if (artifact.Status != "Draft") throw new ConflictException("Certification Not Allowed", "You may not request a certification on this item as it is not in Draft status.");

                var workflow = Company.GetMostRecentCertificationWorkflowByArtifact(id);

                if (workflow != null) {
                    if (!workflow.DateCompleted.HasValue)
                        throw new ConflictException("Certification Not Allowed", "There is already a certification request in process for this item.");
                }

                var workflowSettings = Company.Filter<WorkflowTypeRelation>(i => i.Enabled
                    && i.WorkflowType == WorkflowType.CertifyArtifact
                    && i.Object == "ArtifactType"
                    && i.ObjectID == artifact.ArtifactTypeID).SingleOrDefault();

                if (workflowSettings == null)
                    throw new ConflictException("Certification Not Allowed", string.Format("There is no enabled workflow allocated to {0}.  Please check with an administrator.", artifact.ArtifactType.Name));

                int daysGivenToComplete = (workflowSettings.Fields.ContainsKey("DaysGivenToCompleteCertification")) ? int.Parse(workflowSettings.Fields["DaysGivenToCompleteCertification"]) : 7;

                var processor = new Processor();
                var dictionary = new Dictionary<string, object>();
                dictionary.Add("CompanyID", Company.CurrentCompanyID);
                dictionary.Add("requestInfo", new CertifyArtifactRequest
                {
                    ArtifactID = artifact.ID,
                    DueDate = DateTime.UtcNow.AddDays(daysGivenToComplete),
                    StartDate = DateTime.UtcNow,
                    SendMailFromWorkflow = true
                });
                processor.CreateNewWorkflowInstance(WorkflowVersionMap.CertifyArtifactIdentity_vCurrent, dictionary);

                return jsonSuccess("Request successfully created.", "", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult SuggestNewArtifact(int typeID, int parentID)
        {
            var type = Company.GetById<ArtifactType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "Suggest",
                FieldUri = string.Format("/form/Artifact_SuggestFields?at={0}&p={1}", typeID, parentID),
                FormTitle = string.Format("Suggest a new {0}", type.Name),
                FormDescription = "Your request will be sent to the appropriate people for approval.",
                FormUri = "/form/SuggestNewArtifact",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult SuggestNewArtifact(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int typeID = parseIntField(form, "ArtifactTypeID");
                var type = Company.GetById<ArtifactType>(typeID);

                if (type == null) throw new NotFoundException("artifact type");

                var model = new NewArtifactRequest();
                // Static fields
                model.ArtifactTypeID = typeID;
                model.TaxonomyTypeID = parseIntField(form, "TaxonomyTypeID");
                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    model.ParentID = parseIntField(form, "ParentID");
                    if (model.ParentID == 0) model.ParentID = null;
                }
                model.RequestingResourceID = Company.CurrentResourceID;


                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, 0, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, typeID).ToList(), form, Server);
                if (fields.Count > 0)
                {
                    model.Fields = new Dictionary<string, object>();
                    foreach (var field in fields)
                    {
                        model.Fields.Add(string.Format("FieldType_{0}", field.FieldTypeID), field.Value);
                    }
                }

                var processor = new Processor();
                var dictionary = new Dictionary<string, object>();
                dictionary.Add("CompanyID", Company.CurrentCompanyID);
                dictionary.Add("requestInfo", model);

                var wtr = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == typeID && i.Enabled).ToList();

                if (wtr.Count(i => i.WorkflowType == WorkflowType.SuggestNewArtifactMulti) > 0)
                    processor.CreateNewWorkflowInstance(WorkflowVersionMap.SuggestNewArtifactMultiStepIdentity_vCurrent, dictionary);
                else
                    processor.CreateNewWorkflowInstance(WorkflowVersionMap.SuggestNewArtifactIdentity_vCurrent, dictionary);

                return jsonSuccess("Request successfully created.", "", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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

        public ActionResult AddArtifactType(int parentID)
        {
            var model = new ArtifactTypeEditorModel
            {
                FormName = Resources.FormInfo.Add_ArtifactType_Title,
                FormDescription = Resources.FormInfo.Add_ArtifactType_Directions,
                FormUri = "/form/AddArtifactType",
                FormMethod = "POST",
                ArtifactType = new ArtifactType { ParentID = parentID, AllowHierarchy = false, AllowRelatedArtifacts = false, CanOwnFusion = false },
                IconBackColor = "#000",
                IconForeColor = "#FFF"
            };

            return PartialView("ArtifactTypeEditForm", model);
        }

        [HttpGet]
        [ActionName("ArtifactType")]
        public JsonNetResult GetArtifactType(int? id, int? parentID)
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
                    ArtifactType = new ArtifactType { ParentID = parentID, AllowHierarchy = false, AllowRelatedArtifacts = false, CanOwnFusion = false },
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

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        [ActionName("ArtifactType")]
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
                    CanOwnFusion = model.ArtifactType.CanOwnFusion, //parseBooleanField(form, "CanOwnFusion"),
                    AllowRelatedArtifacts = model.ArtifactType.AllowRelatedArtifacts, //parseBooleanField(form, "AllowRelatedArtifacts")
                };

                if (model.ArtifactType.ParentID != null)
                {
                    a.ParentID = model.ArtifactType.ParentID; // parseIntField(form, "ParentID");
                    if (a.ParentID == 0) a.ParentID = null;
                }

                Company.Add(a);

                upsertObjectStyle(SystemObjects.ArtifactType, a.ID, model.IconForeColor, model.IconBackColor, a.Name);

                dynamic custom = new
                {
                    ParentID = a.ParentID,
                    Name = a.Name,
                    action = "add",
                    //Context = "", // form["_context"]
                };

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), null, "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPut]
        [ActionName("ArtifactType")]
        public JsonResult PutArtifactType(ArtifactTypeEditorModel model)
        {
            try
            {
                var id = model.ArtifactType.ID; // parseIntField(form, "ID");
                var existing = Company.GetById<ArtifactType>(id);
                if (existing == null) throw new NotFoundException("artifact type");

                if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                existing.Name = model.ArtifactType.Name;  //parseTextField(form, "Name");
                existing.Description = model.ArtifactType.Description; // parseTextField(form, "Description");
                existing.AllowRelatedArtifacts = model.ArtifactType.AllowRelatedArtifacts; // parseBooleanField(form, "AllowRelatedArtifacts");
                existing.CanOwnFusion = model.ArtifactType.CanOwnFusion; // parseBooleanField(form, "CanOwnFusion");

                Company.Update(existing);

                upsertObjectStyle(SystemObjects.ArtifactType, existing.ID, model.IconForeColor, model.IconBackColor, existing.Name);

                dynamic custom = new
                {
                    ParentID = existing.ParentID,
                    Name = existing.Name,
                    action = "edit",
                    //Context = form["_context"]
                };

                return jsonSuccess(existing.Name + " successfully updated.", id.ToString(), null, "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ValidateHttpAntiForgeryToken]
        [HttpDelete]
        [ActionName("ArtifactType")]
        public JsonResult DeleteArtifactType2(int id)
        {
            try
            {
                //if (!form.HasKeys()) throw new NoFormDataException("artifact type");

               // var id = parseIntField(form, "ID");
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
                    action = "delete",
                   // Context = form["_context"]
                };

                return jsonSuccess("Item successfully removed.", id.ToString(), null, "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddArtifactType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ArtifactType, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("artifact type");

                var a = new ArtifactType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    CanOwnFusion = parseBooleanField(form, "CanOwnFusion"),
                    AllowRelatedArtifacts = parseBooleanField(form, "AllowRelatedArtifacts")
                };

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    a.ParentID = parseIntField(form, "ParentID");
                    if (a.ParentID == 0) a.ParentID = null;
                }

                Company.Add<ArtifactType>(a);

                upsertObjectStyle(SystemObjects.ArtifactType, a.ID, form, a.Name);

                dynamic custom = new
                {
                    ParentID = a.ParentID,
                    Name = a.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteArtifactType(int id)
        {
            var a = Company.GetById<ArtifactType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ArtifactType,
                FieldUri = string.Format("/form/ArtifactType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteArtifactType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteArtifactType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact type");

                var id = parseIntField(form, "ID");
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
                    action = "delete",
                    Context = form["_context"]
                };

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditArtifactType(int id)
        {
            var at = Company.GetById<ArtifactType>(id);
            if (at == null) return HttpNotFound();
            var style = Company.GetObjectStyle(SystemObjects.ArtifactType, id);

            var model = new ArtifactTypeEditorModel
            {
                FormName = Resources.FormInfo.Edit_ArtifactType_Title,
                FormDescription = Resources.FormInfo.Edit_ArtifactType_Directions,
                FormUri = "/form/EditArtifactType",
                FormMethod = "PUT",
                ArtifactType = at,
                IconBackColor = ((style != null) ? style.IconBackColor : "#000"),
                IconForeColor = ((style != null) ? style.IconForeColor : "#FFF")
            };

            return PartialView("ArtifactTypeEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditArtifactType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ArtifactType>(id);
                if (model == null) throw new NotFoundException("artifact type");

                if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                //model.AllowHierarchy = parseBooleanField(form, "AllowHierarchy");
                model.AllowRelatedArtifacts = parseBooleanField(form, "AllowRelatedArtifacts");
                model.CanOwnFusion = parseBooleanField(form, "CanOwnFusion");

                Company.Update<ArtifactType>(model);

                upsertObjectStyle(SystemObjects.ArtifactType, model.ID, form, model.Name);

                dynamic custom = new
                {
                    ParentID = model.ParentID,
                    Name = model.Name,
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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
        public JsonResult Attribute_AddFields(int at, string ot, int oid, int p)
        {
            var list = new List<EditableField>();
            var type = Company.GetById<AttributeType>(at);

            list.Add(new EditableField { FieldName = "AttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = at.ToString() });
            list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = ot });
            list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = oid.ToString() });
            list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = p.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.AttributeType, at).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeID</param>
        public JsonResult Attribute_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeID</param>
        public JsonResult Attribute_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<d360.core.entities.Attribute>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list = loadDynamicFields(list,
                Company.GetFieldTypeRelationsByObject(SystemObjects.AttributeType, a.AttributeTypeID).ToList(),
                Company.GetFieldRelationsByObject(SystemObjects.Attribute, id).ToList(),
                1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddAttribute(int typeID, string objectType, int objectID, int? parentID)
        {
            var type = Company.GetById<AttributeType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Attribute,
                FieldUri = string.Format("/form/Attribute_AddFields?at={0}&ot={1}&oid={2}&p={3}", typeID, objectType, objectID, parentID.HasValue ? parentID.Value : 0),
                FormTitle = string.Format(Resources.FormInfo.Add_Generic_Title, type.Name),
                FormUri = "/form/AddAttribute",
                FormMethod = "POST"
            };

            return PartialView("AttributeEditForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                var fields = loader.GetFormDynamicFieldValues(SystemObjects.Attribute, a.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.AttributeType, typeID).ToList(), form, Server);

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

                return jsonSuccess(type.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteAttribute(int id)
        {
            var a = Company.GetById<d360.core.entities.Attribute>(id, i => i.AttributeType);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Attribute,
                FieldUri = string.Format("/form/Attribute_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.AttributeType.Name),
                FormUri = "/form/DeleteAttribute",
                FormDescription = Resources.FormInfo.Delete_Attribute_Description,
                FormMethod = "DELETE"
            };

            return PartialView("AttributeDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("attribute");

                var id = parseIntField(form, "ID");
                Company.Delete<core.entities.Attribute>(i => i.ID == id);

                return jsonSuccess(Resources.FormInfo.Delete_Attribute_Confirmation, id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditAttribute(int id)
        {
            var a = Company.GetById<d360.core.entities.Attribute>(id, i => i.AttributeType);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Attribute,
                FieldUri = string.Format("/form/Attribute_EditFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Edit_Generic_Title, a.AttributeType.Name),
                FormDescription = Resources.FormInfo.Edit_Attribute_Description,
                FormUri = "/form/EditAttribute",
                FormMethod = "PUT"
            };

            return PartialView("AttributeEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditAttribute(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("attribute");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<d360.core.entities.Attribute>(id);
                if (model == null) throw new NotFoundException("attribute");

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Attribute, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.AttributeType, model.AttributeTypeID).ToList(), form, Server, false);

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

                return jsonSuccess("Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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

        public ActionResult AddAttributeType(int? parentID)
        {
            //if (!Company.HasPermission(SystemObjects.AttributeType, 0, Claim.Create))
            //    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var model = new AttributeTypeEditorModel()
            {
                FormUri = "/Form/AddAttributeType",
                FormMethod = "POST",
                Tokens = new List<SelectListItem>(),
                FormName = Resources.FormInfo.Add_AttributeType_Title,
                AttributeType = new AttributeType { ParentID = parentID, ShowNameInTree = true },
                AttributeTypeCategories = (parentID.HasValue) ? new List<SelectListItem>() : Company.Table<AttributeTypeCategory>().OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList()
            };
            if (!parentID.HasValue)
            {
                model.AttributeTypeCategories.Insert(0, new SelectListItem { Text = "Enterprise-wide", Value = "0" });
            }

            return PartialView("AttributeTypeEditForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                return jsonSuccess(Resources.FormInfo.Add_AttributeType_Confirmation, a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, new { ParentID = a.ParentID, Context = form["_context"], Name = a.Name });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteAttributeType(int id)
        {
            var a = Company.GetById<AttributeType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "attributetypeform",
                FieldUri = string.Format("/form/AttributeType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteAttributeType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess(Resources.FormInfo.Delete_AttributeType_Confirmation, id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditAttributeType(int id)
        {
            //if (!Company.HasPermission(SystemObjects.AttributeType, id, ObjectPermission.Update))
            //    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var a = Company.GetById<AttributeType>(id);
            if (a == null) return HttpNotFound();
            //var used = FieldService.GetFieldRelations().Any(i => i.FieldTypeID == id);
            var model = new AttributeTypeEditorModel
            {
                FormUri = "/Form/EditAttributeType",
                Tokens = Company.GetFieldTypeRelationsByObject(SystemObjects.AttributeType, id)
                .Select(i => new SelectListItem
                {
                    Text = i.FriendlyName,
                    Value = "{" + i.Name + "}"
                }).ToList(),
                FormMethod = "PUT",
                FormName = Resources.FormInfo.Edit_AttributeType_Title,
                FormDescription = Resources.FormInfo.Edit_AttributeType_Directions,
                AttributeType = a,
                AttributeTypeCategories = (a.ParentID.HasValue) ? new List<SelectListItem>() : Company.Table<AttributeTypeCategory>().OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = (a.AttributeTypeCategoryID == i.ID) }).ToList()
            };
            if (!a.ParentID.HasValue)
            {
                model.AttributeTypeCategories.Insert(0, new SelectListItem { Text = "Enterprise-wide", Value = "0", Selected = !a.AttributeTypeCategoryID.HasValue });
            }

            return PartialView("AttributeTypeEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess(Resources.FormInfo.Edit_AttributeType_Confirmation, id.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { ParentID = model.ParentID, Context = form["_context"], Name = model.Name });
            }
            catch (BaseException ex)
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
        public JsonResult AttributeTypeCategory_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">AttributeTypeCategoryID</param>
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

        public ActionResult AddAttributeTypeCategory()
        {
            var model = new EditableForm
            {
                Context = ContextList.AttributeTypeCategory,
                FieldUri = "/form/AttributeTypeCategory_AddFields",
                FormTitle = Resources.FormInfo.Add_AttributeTypeCategory_Title,
                FormDescription = Resources.FormInfo.AttributeTypeCategory_Directions,
                FormUri = "/form/AddAttributeTypeCategory",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddAttributeTypeCategory(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.AttributeTypeCategory, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("attribute type category");

                var a = new AttributeTypeCategory
                {
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description")
                };

                Company.Add<AttributeTypeCategory>(a);

                dynamic custom = new
                {
                    Name = a.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteAttributeTypeCategory(int id)
        {
            var a = Company.GetById<AttributeTypeCategory>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.AttributeTypeCategory,
                FieldUri = string.Format("/form/AttributeTypeCategory_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteAttributeTypeCategory",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditAttributeTypeCategory(int id)
        {
            if (!Company.Exists<ArtifactType>(id)) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.AttributeTypeCategory,
                FieldUri = string.Format("/form/AttributeTypeCategory_EditFields?id={0}", id),
                FormTitle = Resources.FormInfo.Edit_AttributeTypeCategory_Title,
                FormDescription = Resources.FormInfo.AttributeTypeCategory_Directions,
                FormUri = "/form/EditAttributeTypeCategory",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");

                Company.Update<AttributeTypeCategory>(model);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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

        public ActionResult AddAttributeTypeRelation(int id)
        {
            var type = Company.GetById<AttributeType>(id);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.AttributeTypeRelation,
                FieldUri = string.Format("/form/AttributeTypeRelation_AddFields?at={0}", id),
                FormTitle = "Allocating " + type.Name,
                FormUri = "/form/AddAttributeTypeRelation",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                    return jsonSuccess(type.Name + " successfully allocated.", typeID.ToString(), form["_context"], "add", HttpStatusCode.Created);
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

        public ActionResult DeleteAttributeTypeRelation(int id, string objectType, int objectTypeID)
        {
            var sType = objectType.ToString();
            var a = Company.Filter<AttributeTypeRelationDetail>(i => i.AttributeTypeID == id && i.ObjectID == objectTypeID && i.ObjectType == sType).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.AttributeTypeRelation,
                FieldUri = string.Format("/form/AttributeTypeRelation_DeleteFields?at={0}&ot={1}&oid={2}", a.AttributeTypeID, a.ObjectType, a.ObjectID),
                FormTitle = "Are you sure you want to de-allocate " + a.ObjectName + "?",
                FormUri = "/form/DeleteAttributeTypeRelation",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteAttributeTypeRelation(FormCollection form)
        {
            try
            {
                var at = parseIntField(form, "AttributeTypeID");
                var ot = form["ObjectType"];
                var oid = parseIntField(form, "ObjectID");
                if (Company.Delete<AttributeTypeRelation>(i => i.AttributeTypeID == at && i.ObjectType == ot && i.ObjectID == oid))
                    return jsonSuccess("Allocation successfully removed.", ot.ToString(), form["_context"], "delete", HttpStatusCode.OK);
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

        public ActionResult EditAttributeTypeRelation(int id, string objectType, int objectTypeID)
        {
            var sType = objectType.ToString();
            var a = Company.Filter<AttributeTypeRelationDetail>(i => i.AttributeTypeID == id && i.ObjectID == objectTypeID && i.ObjectType == sType).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.AttributeTypeRelation,
                FieldUri = string.Format("/form/AttributeTypeRelation_EditFields?at={0}&ot={1}&oid={2}", a.AttributeTypeID, a.ObjectType, a.ObjectID),
                FormTitle = string.Format(Resources.FormInfo.Edit_Generic_Title, "Allocation"),
                FormUri = "/form/EditAttributeTypeRelation",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut]
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
                    return jsonSuccess("Allocation successfully updated.", ot.ToString(), form["_context"], "update", HttpStatusCode.OK);
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

        public JsonNetResult CompanySettings()
        {
            var settings = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID).ToList();
            var model = new CompanySettingsEditorModel();
            model.DisableCommunityPosting = (settings.Any(i => i.SettingID == 1) ? bool.Parse(settings.Single(i => i.SettingID == 1).Value) : false);
            model.DisableIssuePosting = (settings.Any(i => i.SettingID == 5) ? bool.Parse(settings.Single(i => i.SettingID == 5).Value) : false);
            //model.DisableQuestionPosting = (settings.Any(i => i.SettingID == 6) ? bool.Parse(settings.Single(i => i.SettingID == 6).Value) : false);
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

            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPut, ValidateInput(false)]
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
                            var filesToDelete = Storage.ListFilenamesByPrefix(constants.COMPANY_LOGO_FOLDER, Company.CurrentCompanyID.ToString());
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

                //socialSetting = settings.FirstOrDefault(i => i.SettingID == 6);
                //if (socialSetting == null)
                //{
                //    socialSetting = new CompanySetting { CompanyID = Company.CurrentCompanyID, SettingID = 6, Value = formModel.DisableQuestionPosting.ToString().ToLower() };
                //    Community.Add<CompanySetting>(socialSetting);
                //}
                //else
                //{
                //    socialSetting.Value = formModel.DisableQuestionPosting.ToString().ToLower();
                //    Community.SaveChanges();
                //}

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

                return jsonSuccess("Settings successfully updated.", "0", "CompanySettings", "edit", HttpStatusCode.OK);
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

        #endregion

        #region Domain

        #region Field Generation

        #region Old Add/Edit

        /// <param name="t">DomainTypeID</param>
        public JsonResult Domain_AddFields(int t, int g)
        {
            if (!Company.HasPermission(SystemObjects.DomainType, t, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            //var sources = Company.Query<dynamic>(@"select objectid as id, objecttypename + ' :: ' + name as name from cache.objectdetails d
            //                                        join domainsourcetype t on t.artifacttypeid = d.objecttypeid and d.objecttype = 'ArtifactType' where objecttypename is not null").ToList();
            //var sourcesList = new List<SelectListItem>();
            //sourcesList.Add(new SelectListItem { Text = "(None)", Value = "-1" });
            //sourcesList.AddRange(sources.Select(i => new SelectListItem { Text = i.name, Value = i.id.ToString() }).ToList());

            var classificationList = Company.DomainClassifications.Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField { FieldName = "DomainGroupID", FieldType = DataType.Hidden.ToString(), Value = g.ToString() });
            list.Add(new EditableField { FieldName = "DomainTypeID", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = $"form/Domain_SimilarItems?typeID={t}&query=" });
            //list.Add(new EditableField { Row = 1, Column = 2, Required = false, FieldName = "Source", Name = "Source", FieldType = DataType.Lookup.ToString(), Items = sourcesList, Value = "-1" });
            list.Add(new EditableField { Row = 1, Column = 2, Required = false, FieldName = "Classification", Name = "Classification", FieldType = DataType.Lookup.ToString(), Items = classificationList, Value = "1" });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">DomainID</param>
        public JsonResult Domain_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Domain, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Domain>(id);

            var groups = Company.Filter<DomainGroup>(i => i.DomainTypeID == a.DomainTypeID)
                .ToList()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = (i.ID == a.DomainGroupID.Value) })
                .OrderBy(i => i.Text)
                .ToList();

            //var sources = Company.Query<dynamic>(@"select objectid as id, objecttypename + ' :: ' + name as name from cache.objectdetails d
            //                                        join domainsourcetype t on t.artifacttypeid = d.objecttypeid and d.objecttype = 'ArtifactType' where objecttypename is not null").ToList();
            //var sourcesList = new List<SelectListItem>();
            //sourcesList.Add(new SelectListItem { Text = "(None)", Value = "-1" });
            //sourcesList.AddRange(sources.Select(i => new SelectListItem { Text = i.name, Value = i.id.ToString() }).ToList());

            var classificationList = Company.DomainClassifications.Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "DomainGroupID", Name = "Grouping", FieldType = DataType.Lookup.ToString(), Items = groups, Value = a.DomainGroupID.Value.ToString() });
            //list.Add(new EditableField { Row = 1, Column = 3, Required = false, FieldName = "Source", Name = "Source", FieldType = DataType.Lookup.ToString(), Items = sourcesList, Value = a.SourceArtifactID.ToString() ?? "-1" });
            list.Add(new EditableField { Row = 1, Column = 3, Required = true, FieldName = "Classification", Name = "Classification", FieldType = DataType.Lookup.ToString(), Items = classificationList, Value = a.DomainClassificationID.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        /// <param name="id">DomainID</param>
        public JsonResult Domain_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Domain, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Domain>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonNetResult Domain_SimilarItems(int typeID, string query)
        {
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.SimilarItems, new { type = "Domain", typeID, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddDomain(int typeID, int groupID)
        {
            var type = Company.GetById<DomainType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Domain,
                FieldUri = string.Format("/form/Domain_AddFields?t={0}&g={1}", typeID, groupID),
                FormTitle = "Add domain for " + type.Name,
                FormUri = "/form/AddDomain",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddDomain(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list");

                int typeID = parseIntField(form, "DomainTypeID");
                var type = Company.GetById<DomainType>(typeID);
                if (type == null) throw new NotFoundException("domain list type");

                if (!Company.HasPermission(SystemObjects.DomainType, typeID, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new Domain
                {
                    DomainTypeID = typeID,
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description"),
                    EnforceParentItemSelection = false,
                    DomainGroupID = parseIntField(form, "DomainGroupID"),
                    DomainClassificationID = parseIntField(form, "Classification")
                };

                Company.Add<Domain>(a);

                return jsonSuccess(a.Name + " successfully created.", string.Format("Domain|{0}", a.ID), form["_context"], "add", HttpStatusCode.Created, new { });

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ValidateHttpAntiForgeryToken, HttpPost, Route("domain/add"), ValidateInput(false)]
        public JsonResult AddDomain(DomainEditorModel model)
        {
            try
            {

                var a = new Domain
                {
                    DomainTypeID = model.DomainTypeID,
                    Name = model.Name,
                    Description = model.Description,
                    EnforceParentItemSelection = false,
                    DomainGroupID = model.DomainGroupID,
                    SourceArtifactID = null,
                    DomainClassificationID = model.DomainClassificationID,
                };

                Company.Add(a);
                return jsonSuccess(a.Name + " successfully created.", string.Format("Domain|{0}", a.ID), ContextList.Domain, "add", HttpStatusCode.Created, new { });

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

        }

        public ActionResult DeleteDomain(int id)
        {
            var a = Company.GetById<Domain>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Domain,
                FieldUri = string.Format("/form/Domain_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteDomain",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteDomain(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Domain>(id);
                if (model == null) throw new NotFoundException("domain list");

                if (!Company.HasPermission(SystemObjects.Domain, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var returnID = string.Format("DomainGroup|{0}", model.DomainGroupID);

                Company.Delete<Domain>(model);

                return jsonSuccess("Item successfully removed.", returnID, form["_context"], "delete", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
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
        public JsonResult DeleteDomainByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteDomain(form);
        }
        public ActionResult EditDomain(int id)
        {
            var a = Company.GetById<Domain>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Domain,
                FieldUri = string.Format("/form/Domain_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditDomain",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditDomain(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Domain>(id);
                if (model == null) throw new NotFoundException("domain list");

                if (!Company.HasPermission(SystemObjects.Domain, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);


                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                model.DomainGroupID = parseIntField(form, "DomainGroupID");
                model.DomainClassificationID = parseIntField(form, "Classification");
                Company.Update<Domain>(model);

                return jsonSuccess(model.Name + " successfully updated.", string.Format("Domain|{0}", id), form["_context"], "edit", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ValidateHttpAntiForgeryToken, HttpPut, Route("domain/edit"), ValidateInput(false)]
        public JsonResult EditDomain(DomainEditorModel model)
        {
            try
            {
                var domain = Company.GetById<Domain>(model.DomainID);
                if (model == null) throw new NotFoundException("domain list");

                if (!Company.HasPermission(SystemObjects.Domain, model.DomainID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                domain.Name = model.Name;
                domain.Description = model.Description;
                domain.DomainGroupID = model.DomainGroupID;
                domain.SourceArtifactID = model.SourceArtifactID;
                domain.DomainClassificationID = model.DomainClassificationID;
                Company.Update<Domain>(domain);

                return jsonSuccess(model.Name + " successfully updated.", string.Format("Domain|{0}", model.DomainID), ContextList.Domain, "edit", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
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

        #region DomainGroup

        #region Field Generation

        /// <param name="t">DomainTypeID</param>
        public JsonResult DomainGroup_AddFields(int t)
        {
            if (!Company.HasPermission(SystemObjects.DomainType, t, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var type = Company.GetById<DomainType>(t);

            list.Add(new EditableField { FieldName = "DomainTypeID", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">DomainGroupID</param>
        public JsonResult DomainGroup_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<DomainGroup>(id);
            if (!Company.HasPermission(SystemObjects.DomainType, a.DomainTypeID, Claim.Delete))
                return jsonException("You do not have permissions to delete this.", HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">DomainGroupID</param>
        public JsonResult DomainGroup_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<DomainGroup>(id);

            if (!Company.HasPermission(SystemObjects.DomainType, a.DomainTypeID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, FieldName = "MasterListID", Name = "Master List", FieldType = DataType.Lookup.ToString(), Value = a.MasterListID.HasValue ? a.MasterListID.Value.ToString() : "", Items = a.Items.Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddDomainGroup(int typeID)
        {
            var type = Company.GetById<DomainType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainGroup,
                FieldUri = string.Format("/form/DomainGroup_AddFields?t={0}", typeID),
                FormTitle = "Add group for " + type.Name,
                FormUri = "/form/AddDomainGroup",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddDomainGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list group");

                int typeID = parseIntField(form, "DomainTypeID");
                if (!Company.Exists<DomainType>(typeID)) throw new NotFoundException("domain list type");

                if (!Company.HasPermission(SystemObjects.DomainType, typeID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new DomainGroup
                {
                    DomainTypeID = typeID,
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description")
                };

                Company.Add<DomainGroup>(a);

                return jsonSuccess(a.Name + " successfully created.", string.Format("DomainGroup|{0}", a.ID), form["_context"], "add", HttpStatusCode.Created, new { });

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteDomainGroup(int id)
        {
            var a = Company.GetById<DomainGroup>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainGroup,
                FieldUri = string.Format("/form/DomainGroup_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteDomainGroup",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteDomainGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list group");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<DomainGroup>(id);
                if (model == null) throw new NotFoundException("domain list group");

                if (!Company.HasPermission(SystemObjects.DomainType, model.DomainTypeID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<DomainGroup>(model);
                return jsonSuccess("Item successfully removed.", null, form["_context"], "delete", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditDomainGroup(int id)
        {
            var a = Company.GetById<DomainGroup>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainGroup,
                FieldUri = string.Format("/form/DomainGroup_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditDomainGroup",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditDomainGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list group");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<DomainGroup>(id);
                if (model == null) throw new NotFoundException("domain list group");

                if (!Company.HasPermission(SystemObjects.DomainType, model.DomainTypeID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                if (form["MasterListID"] != "")
                {
                    model.MasterListID = parseIntField(form, "MasterListID");
                }
                Company.Update<DomainGroup>(model);

                return jsonSuccess(model.Name + " successfully updated.", string.Format("DomainGroup|{0}", id), form["_context"], "edit", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
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

        #region DomainItem

        #region Field Generation

        /// <param name="t">DomainID</param>
        public JsonResult DomainItem_AddFields(int t)
        {
            if (!Company.HasPermission(SystemObjects.Domain, t, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "DomainID", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Code", true, "", 1, 25) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">DomainItemID</param>
        public JsonResult DomainItem_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<DomainItem>(id);

            if (!Company.HasPermission(SystemObjects.Domain, a.DomainID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">DomainItemID</param>
        public JsonResult DomainItem_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<DomainItem>(id);

            if (!Company.HasPermission(SystemObjects.Domain, a.DomainID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Code", Name = "Code", FieldType = DataType.Text.ToString(), Value = a.Code, Validations = checkAndAddValidation("Text", "Code", true, "", 1, 50) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddDomainItem(int typeID, int listID)
        {
            var list = Company.GetById<Domain>(listID);
            if (list == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainItem,
                FieldUri = string.Format("/form/DomainItem_AddFields?t={0}", listID),
                FormTitle = "Add item to " + list.Name,
                FormUri = "/form/AddDomainItem",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddDomainItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list item");

                int listID = parseIntField(form, "DomainID");
                var list = Company.GetById<Domain>(listID);
                if (list == null) throw new NotFoundException("domain list");

                if (!Company.HasPermission(SystemObjects.Domain, listID, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new DomainItem
                {
                    DomainID = listID,
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description"),
                    Code = parseTextField(form, "Code")
                };

                Company.Add<DomainItem>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteDomainItem(int id)
        {
            var a = Company.GetById<DomainItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainItem,
                FieldUri = string.Format("/form/DomainItem_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteDomainItem",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteDomainItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list item");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<DomainItem>(id);
                if (model == null) throw new NotFoundException("domain list item");

                if (!Company.HasPermission(SystemObjects.Domain, model.DomainID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<DomainItem>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditDomainItem(int id)
        {
            var a = Company.GetById<DomainItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainItem,
                FieldUri = string.Format("/form/DomainItem_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditDomainItem",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditDomainItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list item");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<DomainItem>(id);
                if (model == null) throw new NotFoundException("domain list item");

                if (!Company.HasPermission(SystemObjects.Domain, model.DomainID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                model.Code = parseTextField(form, "Code");

                Company.Update<DomainItem>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region DomainXrefItem

        //public JsonResult DomainXrefItem_AddFields(int t)
        //{
        //    if (!Company.HasPermission(SystemObjects.Domain, t, Claim.Create))
        //        return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

        //    var domainItem = Company.GetById<DomainItem>(t);

        //    var list = new List<EditableField>();

        //    list.Add(new EditableField { FieldName = "DomainItemID", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });

        //    list.Add(new EditableField { Row = 1, Column = 1,  FieldName = "HouseCode", Name = "House Code", Value = domainItem.Code, ReadOnly = true });

        //    var sources = Company.Query<dynamic>(@"select objectid as id, objecttypename + ' :: ' + name as name from cache.objectdetails d
        //                                            join domainsourcetype t on t.artifacttypeid = d.objecttypeid and d.objecttype = 'ArtifactType'
        //                                            ").ToList();
        //    var sourceItems = sources.Select(i => new SelectListItem { Text = i.name, Value = i.id.ToString() }).ToList();

        //    list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Source", Name = "Source", FieldType = DataType.Lookup.ToString(), Items = sourceItems });

        //    var lists = Company.Domains.ToList();
        //    var listItems = lists.Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

        //    list.Add(new EditableField { Row = 1, Column = 3, Required = true, FieldName = "Domain", Name = "Domain", FieldType = DataType.Lookup.ToString(), Items = listItems });

        //    list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Code", Name = "Code", FieldType = DataType.Lookup.ToString(), Items = null });
        //    //list.Add(new EditableFieldLookupList { FieldName})

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult AddDomainXrefItem(int domainItemID)
        {
            var item = Company.GetById<DomainItem>(domainItemID);
            var domain = Company.GetById<Domain>(item.DomainID);
            if (item == null) return HttpNotFound();
            var model = new DomainItemXrefEditorModel
            {
                HouseDomainItemID = item.ID,
                HouseCode = item.Code,
                SourceArtifactID = domain.SourceArtifactID
            };

            return PartialView("DomainXrefItemForm", model);
        }

        public ActionResult EditDomainXrefItem(int id)
        {
            var xref = Company.GetById<DomainItemXref>(id);
            var item = Company.GetById<DomainItem>(xref.HouseDomainItemID);
            var domain = Company.GetById<Domain>(item.DomainID);
            if (item == null) return HttpNotFound();
            var model = new DomainItemXrefEditorModel
            {
                ID = xref.ID,
                HouseDomainItemID = item.ID,
                HouseCode = item.Code,
                SourceArtifactID = domain.SourceArtifactID,
                LanguageID = xref.LanguageID
            };

            return PartialView("DomainXrefItemForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, Route("xref/add")]
        public JsonNetResult AddDomainXrefItem(int houseDomainItem, int domainItem, int languageID)
        {
            var error = false;
            var message = "";
            if (houseDomainItem < 1)
            {
                error = true;
                message += "An error occurred: house domain item ID missing\n";
            }
            if (domainItem < 1)
            {
                error = true;
                message += "An error occurred: domain item ID missing\n";
            }

            DomainItemXref i = new DomainItemXref();
            i.DomainItemID = domainItem;
            i.HouseDomainItemID = houseDomainItem;
            i.LanguageID = languageID;

            var existing = Company.DomainItemXrefs.Where(e => e.HouseDomainItemID == i.HouseDomainItemID && e.DomainItemID == i.DomainItemID && e.LanguageID == i.LanguageID).Count();

            if (existing == 0)
            {
                try
                {
                    Company.DomainItemXrefs.Add(i);
                    Company.SaveChanges();
                } catch (Exception ex)
                {
                    error = true;
                    message += $"An error occurred: {ex.Message}\n{ex.StackTrace}";
                }
            }

            return new JsonNetResult
            {
                Data = new { error = error, message = message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        public ActionResult DeleteDomainItemXref(int id)
        {
            var a = Company.GetById<DomainItemXref>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainXrefItem,
                FieldUri = string.Format("/form/DomainItemXref_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this cross reference"),
                FormUri = "/form/DeleteDomainItemXref",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteDomainItemXref(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list item");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<DomainItemXref>(id);
                if (model == null) throw new NotFoundException("domain list item");
                var domainItem = Company.GetById<DomainItem>(model.HouseDomainItemID);

                if (!Company.HasPermission(SystemObjects.Domain, domainItem.DomainID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<DomainItemXref>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        /// <param name="id">DomainItemXrefID</param>
        public JsonResult DomainItemXref_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<DomainItemXref>(id);
            var d = Company.GetById<DomainItem>(a.HouseDomainItemID);
            if (!Company.HasPermission(SystemObjects.Domain, d.DomainID, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region DomainType

        #region Field Generation

        public JsonResult DomainType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.DomainType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            loadIconFields(list, 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">DomainTypeID</param>
        public JsonResult DomainType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.DomainType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<DomainType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">DomainTypeID</param>
        public JsonResult DomainType_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.DomainType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<DomainType>(id);
            var style = Company.GetObjectStyle(SystemObjects.DomainType, id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });
            loadIconFields(list, 3, style);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("domains/add")]
        public ActionResult AddDomainType()
        {
            var model = new EditableForm
            {
                Context = ContextList.DomainType,
                FieldUri = "/form/DomainType_AddFields",
                FormTitle = "Add domain list type",
                FormUri = "/form/AddDomainType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddDomainType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.DomainType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("domain list type");

                var a = new DomainType
                {
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description")
                };

                Company.Add<DomainType>(a);

                upsertObjectStyle(SystemObjects.DomainType, a.ID, form, a.Name);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("domains/{id:int}/delete")]
        public ActionResult DeleteDomainType(int id)
        {
            var a = Company.GetById<DomainType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainType,
                FieldUri = string.Format("/form/DomainType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteDomainType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteDomainType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<DomainType>(id);
                if (model == null) throw new NotFoundException("domain list type");

                if (!Company.HasPermission(SystemObjects.DomainType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<DomainType>(model);

                deleteObjectStyle(SystemObjects.DomainType, id);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteDomainTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteDomainType(form);
        }

        [Route("domains/{id:int}/edit")]
        public ActionResult EditDomainType(int id)
        {
            var a = Company.GetById<DomainType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.DomainType,
                FieldUri = string.Format("/form/DomainType_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditDomainType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditDomainType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("domain list type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<DomainType>(id);
                if (model == null) throw new NotFoundException("domain list type");

                if (!Company.HasPermission(SystemObjects.DomainType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");

                Company.Update<DomainType>(model);

                upsertObjectStyle(SystemObjects.DomainType, model.ID, form, model.Name);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region EmailTemplate

        #region Field Generation

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
        public JsonResult EmailTemplate_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<EmailTemplate>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">EmailTemplateID</param>
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

        [Route("templates/email/add")]
        public ActionResult AddEmailTemplate()
        {
            var model = new EditableForm
            {
                Context = ContextList.EmailTemplate,
                FieldUri = "/form/EmailTemplate_AddFields",
                FormTitle = "Add Email Template",
                FormUri = "/form/AddEmailTemplate",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddEmailTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("email template");

                var a = new EmailTemplate
                {
                    Action = parseTextField(form, "Action"),
                    Description = parseTextField(form, "Description"),
                    Name = parseTextField(form, "Name", null, true),
                    TemplateBody = parseTextField(form, "TemplateBody"),
                    TemplateSubject = parseTextField(form, "TemplateSubject")
                };

                Company.Add<EmailTemplate>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("templates/email/{id:int}/delete")]
        public ActionResult DeleteEmailTemplate(int id)
        {
            var a = Company.GetById<EmailTemplate>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.EmailTemplate,
                FieldUri = string.Format("/form/EmailTemplate_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteEmailTemplate",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteEmailTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("email template");

                var id = parseIntField(form, "ID");
                Company.Delete<EmailTemplate>(i => i.ID == id);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("templates/email/{id:int}/edit")]
        public ActionResult EditEmailTemplate(int id)
        {
            var a = Company.GetById<EmailTemplate>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.EmailTemplate,
                FieldUri = string.Format("/form/EmailTemplate_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditEmailTemplate",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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
                model.Name = parseTextField(form, "Name", null, true);
                model.TemplateBody = parseTextField(form, "TemplateBody");
                model.TemplateSubject = parseTextField(form, "TemplateSubject");

                Company.Update<EmailTemplate>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region Event

        #region Field Generation

        /// <param name="id">EventID</param>
        public JsonResult Event_EditFields(int id)
        {
            var a = Company.GetById<Event>(id, i => i.EventGroup);

            if (a == null)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.NotFound);

            if (!Company.HasPermission(SystemObjects.Rule, a.EventGroup.RuleID.Value, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            var criticalities = EventCriticality.Critical.GetAsList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Criticality", Name = "Name", FieldType = DataType.Lookup.ToString(), Value = ((int)a.Criticality).ToString(), Items = criticalities });
            list = loadStatusField(list, SystemObjects.Event, a.Status, 1, 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult EditEvent(int id)
        {
            var a = Company.GetById<Event>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Event,
                FieldUri = string.Format("/form/Event_EditFields?id={0}", id),
                FormTitle = "Edit Event",
                FormUri = "/form/EditEvent",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditEvent(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("event");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Event>(id);
                if (model == null) throw new NotFoundException("event");

                if (!Company.HasPermission(SystemObjects.Domain, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Criticality = (EventCriticality)Enum.Parse(typeof(EventCriticality), form["Criticality"]);
                model.Status = parseTextField(form, "Status");

                Company.Update<Event>(model);

                return jsonSuccess("Event successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
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

        #region EventGroup

        #region Field Generation

        /// <param name="id">EventGroupID</param>
        public JsonResult EventGroup_EditFields(int id)
        {
            var a = Company.GetById<EventGroup>(id);

            if (a == null)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.NotFound);

            if (!Company.HasPermission(SystemObjects.Rule, a.RuleID.Value, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            var criticalities = EventCriticality.Critical.GetAsList().Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name }).ToList();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Criticality", Name = "Name", FieldType = DataType.Lookup.ToString(), Items = criticalities });
            list = loadStatusField(list, SystemObjects.Event, "", 1, 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult EditEventGroup(int id)
        {
            var a = Company.GetById<Event>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Event,
                FieldUri = string.Format("/form/EventGroup_EditFields?id={0}", id),
                FormTitle = "Edit Event Group",
                FormDescription = "You can set properties for all events under this group, including updating the status.",
                FormUri = "/form/EditEventGroup",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditEventGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("event group");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<EventGroup>(id, i => i.Events);
                if (model == null) throw new NotFoundException("event group");

                if (!Company.HasPermission(SystemObjects.Rule, model.RuleID.Value, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var criticality = (EventCriticality)Enum.Parse(typeof(EventCriticality), form["Criticality"]);
                var status = parseTextField(form, "Status");
                foreach (var e in model.Events)
                {
                    e.Criticality = criticality;
                    e.Status = status;
                }
                Company.SaveChanges();

                return jsonSuccess("Event group successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
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
        /// Gets a list of display fields that match a lookup.
        /// </summary>
        /// <param name="type">The type of object we are adding field type to.</param>
        /// <param name="id">The type Id of object we are adding field type to.</param>
        /// <param name="listType">The type of list to pull fields for.</param>
        /// <param name="listID">The type Id of the list to pull fields for.</param>
        /// <returns>A list of relevant fusion attribute types.</returns>
        public JsonNetResult FieldType_FilteredLookup_DisplayFields(string type, int id, string listType, int listID)
        {
            var list = Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, listID)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.RelationLookup.ToString())
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
        public JsonNetResult FieldType_FusionLookup_DisplayFields(int id)
        {
            var list = Company.GetFieldTypeRelationsByObject(SystemObjects.FusionAttributeType, id)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.RelationLookup.ToString())
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
                    var relations = Company.Query<int>(@"select TargetObjectID from utility.RelationshipTypes where SourceObjectType  = 'FusionAttributeType' and SourceObjectID = @id and TargetObjectType = 'FusionAttributeType'", new { id = s }).ToList();
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

//        public JsonNetResult FieldType_RelationLookup_IntersectTypes(SystemObjects type, int id)
//        {
//            #region
//            var sql = @"
//declare @tbl table(ID int, ParentID int, ObjectType varchar(50), ObjectTypeID int, Name nvarchar(250), Inferred bit)

//insert into @tbl
//	select	T.ID,
//			NULL,
//			case 
//				when (T.Subject = @type and T.SubjectID = @id) then T.Object
//				else T.Subject 
//			end,
//			case 
//				when (T.Subject = @type and T.SubjectID = @id) then T.ObjectID
//				else T.SubjectID
//			end,
//			D.TextPath,
//			0
//	from	IntersectType T
//			inner join cache.ObjectDetails D on 
//												D.Object = case 
//																when (T.Subject = @type and T.SubjectID = @id) then T.Object
//																else T.Subject 
//															end 
//											and D.ObjectID = case 
//																when (T.Subject = @type and T.SubjectID = @id) then T.ObjectID
//																else T.SubjectID
//															end
//	where	(T.Subject = @type and T.SubjectID = @id) OR (T.Object = @type and T.ObjectID = @id)

//-- Get inferred relationship types
//insert into @tbl
//	select	T.ID,
//			P.ID,
//			case 
//				when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.Object
//				else T.Subject 
//			end,
//			case 
//				when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.ObjectID
//				else T.SubjectID
//			end,
//			'Inferred :: ' + D.TextPath,
//			1
//	from	@tbl P
//			inner join IntersectType T on (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) OR (T.Object = P.ObjectType and T.ObjectID = P.ObjectTypeID)
//			inner join cache.ObjectDetails D on 
//												D.Object = case 
//																when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.Object
//																else T.Subject 
//															end 
//											and D.ObjectID = case 
//																when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.ObjectID
//																else T.SubjectID
//															end

//-- Get child relationship types
//insert into @tbl
//	select	T.ID,
//			P.ID,
//			case 
//				when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.Object
//				else T.Subject 
//			end,
//			case 
//				when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.ObjectID
//				else T.SubjectID
//			end,
//			'Child :: ' + D.TextPath,
//			0
//	from	@tbl P
//			inner join IntersectType T on (T.Subject = 'IntersectType' and T.SubjectID = P.ID) OR (T.Object = 'IntersectType' and T.ObjectID = P.ID) and P.Inferred = 0
//			inner join cache.ObjectDetails D on 
//												D.Object = case 
//																when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.Object
//																else T.Subject 
//															end 
//											and D.ObjectID = case 
//																when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.ObjectID
//																else T.SubjectID
//															end



//select * from @tbl";
//            #endregion

//            var intersectTypes = Company.Query<dynamic>(sql, new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString() }, id });

//            return new JsonNetResult
//            {
//                Data = intersectTypes,
//                Formatting = Newtonsoft.Json.Formatting.None
//            };
//        }

        public JsonNetResult FieldType_RelationLookup_ChildIntersectTypes(int id)
        {
            var intersectTypes = Company.Query<dynamic>(@"
select  distinct 
        cast(RT.IntersectTypeID as varchar) + '|' + RT.TargetObjectType + '|' + cast(RT.TargetObjectID as varchar) as value, 
        D.TextPath as title
from    utility.RelationshipTypes RT
        inner join cache.ObjectDetails D on D.[Object] = RT.TargetObjectType and D.ObjectID = RT.TargetObjectID
        and RT.SourceObjectType = 'IntersectType' and RT.SourceObjectID = @id
order by  D.TextPath
", new { id });

            return new JsonNetResult
            {
                Data = intersectTypes,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        public JsonNetResult FieldType_RelationLookup_DisplayFields(int intersectTypeID, SystemObjects type, int id)
        {
            var list = Company.GetFieldTypeRelationsByObject(type, id)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.RelationLookup.ToString())
                .Select(i => new { i.ID, i.Name })
                .ToDictionary(i => i.Name, i => i.ID);
            list.Add("Name", 0);
            list.Add("TextPath", 0);
            if (!list.ContainsKey("Description"))
                list.Add("Description", 0);

            var relList = Company.GetFieldTypeRelationsByObject(SystemObjects.IntersectType, intersectTypeID)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.RelationLookup.ToString())
                .Select(i => new { i.ID, i.Name }).ToList();
            relList.ForEach(r =>
            {
                list.Add($"Relation.{r.Name}", r.ID);
            });


            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}|{i.Key}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        public JsonNetResult FieldType_Lookup_Tokens(SystemObjects type, int id)
        {
            Dictionary<string, string> list;

            if (type != SystemObjects.DomainItem)
            {
                list = Company.GetFieldTypeRelationsByObject(type, id)
                    .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.RelationLookup.ToString())
                    .Select(i => new { i.ID, i.Name })
                    .ToDictionary(i => i.Name, i => i.Name);
            }
            else
            {
                list = new Dictionary<string, string>();
            }

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list.Add("Name", "Name");
                    list.Add("Status", "Status");
                    list.Add("Description", "Description");
                    list.Add("TextPath", "TextPath");
                    break;
                case SystemObjects.DomainItem:
                case SystemObjects.DomainType:
                    list.Add("Name", "Name");
                    list.Add("Code", "Code");
                    list.Add("Description", "Description");
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
                    //default:
                    //    list.Add("Name", "Name");
                    //    list.Add("TextPath", "TextPath");
                    //    break;
            }

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = "{" + i.Value + "}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        public JsonNetResult FieldType_Lookups(string type, int id)
        {
            #region Load static lists

            var intersectTypes = Company.Query<dynamic>(@"
            select  distinct 
                    cast(RT.IntersectTypeID as varchar) + '|' + RT.TargetObjectType + '|' + cast(RT.TargetObjectID as varchar) as value, 
                    D.TextPath as title
            from    utility.RelationshipTypes RT
                    inner join cache.ObjectDetails D on D.[Object] = RT.TargetObjectType and D.ObjectID = RT.TargetObjectID
                    and RT.SourceObjectType = @type and RT.SourceObjectID = @id
            order by  D.TextPath
            ", new { type, id });

            //#region
            //var sql = @"
            //declare @tbl table(ID int, ParentID int, ObjectType varchar(50), ObjectTypeID int, Name nvarchar(250), Inferred bit)

            //insert into @tbl
            //	select	T.ID,
            //			NULL,
            //			case 
            //				when (T.Subject = @type and T.SubjectID = @id) then T.Object
            //				else T.Subject 
            //			end,
            //			case 
            //				when (T.Subject = @type and T.SubjectID = @id) then T.ObjectID
            //				else T.SubjectID
            //			end,
            //			D.TextPath,
            //			0
            //	from	IntersectType T
            //			inner join cache.ObjectDetails D on 
            //												D.Object = case 
            //																when (T.Subject = @type and T.SubjectID = @id) then T.Object
            //																else T.Subject 
            //															end 
            //											and D.ObjectID = case 
            //																when (T.Subject = @type and T.SubjectID = @id) then T.ObjectID
            //																else T.SubjectID
            //															end
            //	where	(T.Subject = @type and T.SubjectID = @id) OR (T.Object = @type and T.ObjectID = @id)

            //-- Get inferred relationship types
            //insert into @tbl
            //	select	T.ID,
            //			P.ID,
            //			case 
            //				when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.Object
            //				else T.Subject 
            //			end,
            //			case 
            //				when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.ObjectID
            //				else T.SubjectID
            //			end,
            //			'Inferred :: ' + D.TextPath,
            //			1
            //	from	@tbl P
            //			inner join IntersectType T on (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) OR (T.Object = P.ObjectType and T.ObjectID = P.ObjectTypeID)
            //			inner join cache.ObjectDetails D on 
            //												D.Object = case 
            //																when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.Object
            //																else T.Subject 
            //															end 
            //											and D.ObjectID = case 
            //																when (T.Subject = P.ObjectType and T.SubjectID = P.ObjectTypeID) then T.ObjectID
            //																else T.SubjectID
            //															end

            //-- Get child relationship types
            //insert into @tbl
            //	select	T.ID,
            //			P.ID,
            //			case 
            //				when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.Object
            //				else T.Subject 
            //			end,
            //			case 
            //				when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.ObjectID
            //				else T.SubjectID
            //			end,
            //			'Child :: ' + D.TextPath,
            //			0
            //	from	@tbl P
            //			inner join IntersectType T on (T.Subject = 'IntersectType' and T.SubjectID = P.ID) OR (T.Object = 'IntersectType' and T.ObjectID = P.ID) and P.Inferred = 0
            //			inner join cache.ObjectDetails D on 
            //												D.Object = case 
            //																when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.Object
            //																else T.Subject 
            //															end 
            //											and D.ObjectID = case 
            //																when (T.Subject = 'IntersectType' and T.SubjectID = P.ID) then T.ObjectID
            //																else T.SubjectID
            //															end



            //select * from @tbl order by ParentID, Name";
            //#endregion

            //var intersectTypes = Company.Query<dynamic>(sql, new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString() }, id });
            var attributes = Company.Filter<AttributeType>(x => !x.ParentID.HasValue).OrderBy(x => x.Name).ToList().Select(i => new { title = i.Name, value = $"AttributeType|{i.ID}" });
            var fusionAttributeTypes = Company.Table<FusionAttributeType>().OrderBy(x => x.TextPath).Select(i => new { title = i.TextPath, value = i.ID });
            var lookups = Company.GetFieldTypeLookupOptions().Select(i => new KnockoutListItem { title = i.Name, value = $"{i.LookupObjectType}|{i.LookupObjectID}" }).ToList();
            var filteredLookups = Company.Query<KnockoutListItem>($@"
select	L.Name as title,
		'Lookup|' + cast(L.ID as varchar) as value
from	LookupType L
		cross apply (
					select	count(1) as [Count]
					from	FieldType
					where	Object = 'LookupType' 
							and ObjectID = L.ID
							and [Type] = 'Lookup'
							and LookupObjectType = @type 
							and LookupObjectID = @id
					) F
where	F.[Count] > 0
order by L.Name", new { type = type.Replace("Type", ""), id });

            var patterns = new Dictionary<string, string>() {
                { "Choose sample...", "" },
                { "Email", @"^$|\b([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b" },
                { "IP Address", @"^$|^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$" },
                { "North American Phone", @"^$|\b\d{3}[-.]?\d{3}[-.]?\d{4}\b" },
                //{ "International Phone", @"^$|\b\\+(9[976]\d|8[987530]\d|6[987]\d|5[90]\d|42\d|3[875]\d|2[98654321]\d|9[8543210]|8[6421]|6[6543210]|5[87654321]|4[987654310]|3[9643210]|2[70]|7|1)\W*\d\W*\d\W*\d\W*\d\W*\d\W*\d\W*\d\W*\d\W*(\d{1,2})\b" },
                //{ "Unc/Network Path", @"^$|^([A-Za-z]:){1}\\.+$|^\\\\.+$|^\/.+$" },
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
                    Lookups = lookups
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        public JsonNetResult FieldType_FormData(int id)
        {
            FieldType ft = null;
            List<dynamic> filteredLookupItems = null;
            List<dynamic> fusionItems = null;
            List<dynamic> relationItems = null;
            if (id > 0)
            {
                ft = Company.GetById<FieldType>(id, i => i.FieldTypeFusionLookupDefinitions, i => i.FieldTypeRelationLookupDefinitions);

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

                if (ft.FieldTypeRelationLookupDefinitions != null)
                {
                    if (ft.FieldTypeRelationLookupDefinitions.Count > 0)
                    {
                        relationItems = new List<dynamic>();
                        foreach (var i in ft.FieldTypeRelationLookupDefinitions)
                        {
                            relationItems.Add(new {
                                ID = i.ID,
                                IntersectType = i.IntersectTypeID,
                                ReferenceType = i.ReferenceType,
                                ChildIntersectType = i.ChildIntersectTypeID,
                                DisplayFields = (i.FieldTypeRelationLookupDisplayFields != null) ? i.FieldTypeRelationLookupDisplayFields.Select(df => new { value = $"{df.FieldTypeID}|{df.FieldTypeName}", FilterValue = df.FilterValue, Show = df.Show, SortOrder = df.SortOrder }).ToList() : null,
                                HideHeader = i.HideHeader,
                                HideFooter = i.HideFooter
                            });
                        }
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
                    RelationItems = relationItems
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Field Generation

        /// <param name="id">ID of the object</param>
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

            if (nameUpper == "STATUS" || nameUpper == "NAME" || nameUpper == "DESCRIPTION" || nameUpper == "PARENTID" || nameUpper == "DATELASTCERTIFIED" || nameUpper == "TAXONOMYTYPEID") throw new Exception("Use of a field type with the name " + name + " is prohibited.");
        }

        public ActionResult AddFieldType(SystemObjects type, int id)
        {
            var model = new FieldTypeEditorModel
            {
                FieldType = new FieldType { Object = type.ToString(), ObjectID = id, Pattern = "", Type = DataType.Text.ToString(), MinimumLength = 0, MaximumLength = 1000, IsListable = true, IsRequired = true },
            };
            return PartialView("FieldTypeEditForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFieldType(FieldTypeEditorModel model)
        {
            try
            {
                int maxSort = 0;
                try { maxSort = Company.GetFieldTypeRelationsByObject((SystemObjects)Enum.Parse(typeof(SystemObjects), model.FieldType.Object), model.FieldType.ObjectID).Max(i => i.SortOrder); }
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
                    case "Lookup":
                        #region
                        if (string.IsNullOrEmpty(model.FieldType.LookupDisplayFormat))
                        {
                            throw new ConflictException("Error Occurred!", $"{Resources.FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
                        }
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

                            model.FieldType.FieldTypeFilteredLookupDefinitions = new List<FieldTypeFilteredLookupDefinition>() { def };
                            //Company.Add<FieldTypeRelationLookupDefinition>(def);
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
                        break;
                    #endregion
                    case "RelationLookup":
                        #region
                        if (model.RelationItem != null)
                        {
                            val = model.RelationItem.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var def = new FieldTypeRelationLookupDefinition
                            {
                                //FieldTypeID = model.FieldType.ID,
                                ChildIntersectTypeID = model.RelationItem.ChildIntersectType,
                                IntersectTypeID = model.RelationItem.IntersectType,
                                ReferenceType = model.RelationItem.ReferenceType,
                                HideHeader = model.RelationItem.HideHeader,
                                HideFooter = model.RelationItem.HideFooter
                            };

                            if (model.RelationItem.DisplayFields != null)
                            {
                                if (model.RelationItem.DisplayFields.Count > 0)
                                {
                                    def.FieldTypeRelationLookupDisplayFields = new List<FieldTypeRelationLookupDisplayField>();

                                    foreach (var df in model.RelationItem.DisplayFields)
                                    {
                                        var ndf = new FieldTypeRelationLookupDisplayField
                                        {
                                            FieldTypeRelationLookupDefinitionID = def.ID,
                                            FieldTypeName = df.FieldTypeName,
                                            FieldTypeID = df.FieldTypeID,
                                            FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || !string.IsNullOrEmpty(ndf.FilterValue) || ndf.SortOrder.HasValue)
                                            def.FieldTypeRelationLookupDisplayFields.Add(ndf);
                                    }
                                }
                            }

                            model.FieldType.FieldTypeRelationLookupDefinitions = new List<FieldTypeRelationLookupDefinition>() { def };
                            //Company.Add<FieldTypeRelationLookupDefinition>(def);
                        }
                        break;
                        #endregion
                }

                Company.Add<FieldType>(model.FieldType);

                return jsonSuccess(Resources.FormInfo.Add_FieldType_Confirmation, model.FieldType.ID.ToString(), ContextList.FieldType, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteFieldType(int id)
        {
            var a = Company.GetById<FieldType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FieldType,
                FieldUri = string.Format("/form/FieldType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteFieldType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFieldType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FieldType>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);
                Company.Delete("FieldType", id);//Company.Delete<FieldType>(model);

                return jsonSuccess(Resources.FormInfo.Delete_FieldType_Confirmation, id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteFieldTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFieldType(form);
        }

        [HttpGet, ActionName("FieldType")]
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

        public ActionResult EditFieldType(int id)
        {
            var a = Company.GetById<FieldType>(id);
            if (a == null) return HttpNotFound();
            var used = Company.Any<Field>(i => i.FieldTypeID == id);
            var qry = Company.Table<FieldTypeLookupValue>().OrderBy(i => i.LookupObjectType).ThenBy(i => i.Name).AsQueryable();

            var fusDef = a.FieldTypeFusionLookupDefinitions.FirstOrDefault();

            if (!a.IsRequired) a.MinimumLength = 0;

            var model = new FieldTypeEditorModel
            {
                FieldIsUsed = used,
                FieldType = a
            };

            return PartialView("FieldTypeEditForm", model);
        }


        [HttpPut, ValidateInput(false)]
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
                ft.DisplayDescription = model.FieldType.DisplayDescription;
                ft.FormDescription = model.FieldType.FormDescription;
                ft.ValidationDescription = model.FieldType.ValidationDescription;

                ft.IsListable = (model.FieldType.Type != DataType.FusionLookup.ToString()) ? model.FieldType.IsListable : false;
                ft.IsRequired = model.FieldType.IsRequired;

                ft.MinimumLength = model.FieldType.MinimumLength;
                ft.MaximumLength = model.FieldType.MaximumLength;
                ft.Pattern = model.FieldType.Pattern;

                if (!ft.IsRequired) ft.MinimumLength = 0;

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
                }

                bool isNew;

                var defs = Company.Filter<FieldTypeFusionLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFusionLookupDisplayFields).ToList();
                var efli = Company.Filter<FieldTypeFilteredLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFilteredLookupDisplayFields).FirstOrDefault();
                var eri = Company.Filter<FieldTypeRelationLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeRelationLookupDisplayFields).FirstOrDefault();

                switch (ft.Type)
                {
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
                        if (eri != null)
                            Company.Set<FieldTypeRelationLookupDefinition>().Remove(eri);
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
                        if (eri != null)
                            Company.Set<FieldTypeRelationLookupDefinition>().Remove(eri);
                        break;
                    #endregion
                    case "Lookup":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupDisplayFormat = model.FieldType.LookupDisplayFormat;
                        if (string.IsNullOrEmpty(model.FieldType.LookupDisplayFormat))
                        {
                            throw new ConflictException("Error Occurred!", $"{Resources.FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
                        }

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        if (eri != null)
                            Company.Set<FieldTypeRelationLookupDefinition>().Remove(eri);
                        break;
                    #endregion
                    case "RelationLookup":
                        #region
                        isNew = false;
                        if (model.RelationItem != null)
                        {
                            val = model.RelationItem.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var listToRemove = new List<FieldTypeRelationLookupDisplayField>();

                            if (eri == null)
                            {
                                isNew = true;
                                eri = new FieldTypeRelationLookupDefinition
                                {
                                    FieldTypeID = model.FieldType.ID,
                                    ChildIntersectTypeID = model.RelationItem.ChildIntersectType,
                                    IntersectTypeID = model.RelationItem.IntersectType,
                                    ReferenceType = model.RelationItem.ReferenceType,
                                    HideHeader = model.RelationItem.HideHeader,
                                    HideFooter = model.RelationItem.HideFooter,
                                    FieldTypeRelationLookupDisplayFields = new List<FieldTypeRelationLookupDisplayField>()
                                };
                            }
                            else
                            {
                                eri.IntersectTypeID = model.RelationItem.IntersectType;
                                eri.ReferenceType = model.RelationItem.ReferenceType;
                                eri.ChildIntersectTypeID = model.RelationItem.ChildIntersectType;
                                eri.HideHeader = model.RelationItem.HideHeader;
                                eri.HideFooter = model.RelationItem.HideFooter;
                            }

                            if (model.RelationItem.DisplayFields != null)
                            {
                                // Add those that do not yet exist.
                                foreach (var df in model.RelationItem.DisplayFields)
                                {
                                    if (!eri.FieldTypeRelationLookupDisplayFields.Any(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName))
                                    {
                                        var ndf = new FieldTypeRelationLookupDisplayField {
                                            FieldTypeRelationLookupDefinitionID = eri.ID,
                                            FieldTypeID = df.FieldTypeID,
                                            FieldTypeName = df.FieldTypeName,
                                            FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || !string.IsNullOrEmpty(ndf.FilterValue) || ndf.SortOrder.HasValue)
                                            eri.FieldTypeRelationLookupDisplayFields.Add(ndf);
                                    }
                                    else
                                    {
                                        var edf = eri.FieldTypeRelationLookupDisplayFields.Single(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName);

                                        edf.FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue;
                                        edf.SortOrder = df.SortOrder;
                                        edf.Show = df.Show;

                                        if (!edf.Show && string.IsNullOrEmpty(edf.FilterValue) && !edf.SortOrder.HasValue)
                                            eri.FieldTypeRelationLookupDisplayFields.Remove(edf);
                                    }
                                }

                                // Remove those that no longer exist.
                                foreach (var edf in eri.FieldTypeRelationLookupDisplayFields)
                                {
                                    if (!model.RelationItem.DisplayFields.Any(i => i.FieldTypeID == edf.FieldTypeID && i.FieldTypeName == edf.FieldTypeName))
                                    {
                                        listToRemove.Add(edf);
                                    }
                                }
                            }
                            else
                            {
                                if (eri.FieldTypeRelationLookupDisplayFields != null)
                                {
                                    listToRemove.AddRange(eri.FieldTypeRelationLookupDisplayFields);
                                }
                            }

                            if (listToRemove.Count > 0)
                            {
                                Company.FieldTypeRelationLookupDisplayFields.RemoveRange(listToRemove);
                            }

                            listToRemove = null;

                            if (isNew)
                                Company.Add<FieldTypeRelationLookupDefinition>(eri);
                            else
                                Company.Update<FieldTypeRelationLookupDefinition>(eri);
                        }
                        else
                        {
                            if (eri != null)
                            {
                                ft.FieldTypeRelationLookupDefinitions.Remove(eri);
                            }
                        }

                        //Clean up previous stuff
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        break;
                        #endregion
                }

                Company.Update<FieldType>(ft);

                return jsonSuccess(Resources.FormInfo.Edit_FieldType_Confirmation, ft.ID.ToString(), ContextList.FieldType, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult Fusion_AddFields(int ft)
        {
            var list = new List<EditableField>();
            var type = Company.GetById<FusionType>(ft);
            var fusion = new Fusion();

            list.Add(new EditableField { FieldName = "FusionTypeID", FieldType = DataType.Hidden.ToString(), Value = ft.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = fusion.GetName(i => i.Name), FieldDescription = fusion.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Manual", Name = fusion.GetName(i => i.Manual), FieldDescription = fusion.GetDescription(i => i.Manual), FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 2, Column = 2, FieldName = "Enabled", Name = fusion.GetName(i => i.Enabled), FieldDescription = fusion.GetDescription(i => i.Enabled), FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 2, Column = 3, FieldName = "LockPromotedItems", Name = fusion.GetName(i => i.LockPromotedItems), FieldDescription = fusion.GetDescription(i => i.LockPromotedItems), FieldType = DataType.Boolean.ToString() });

            var intervalTypes = new List<SelectListItem>();
            intervalTypes.Add(new SelectListItem { Text = "Minute(s)", Value = "3" });
            intervalTypes.Add(new SelectListItem { Text = "Hour(s)", Value = "2" });
            //intervalTypes.Add(new SelectListItem { Text = "Day(s)", Value = "1" });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "IntervalType", Name = fusion.GetName(i => i.IntervalType), FieldDescription = fusion.GetDescription(i => i.IntervalType), FieldType = DataType.Lookup.ToString(), Items = intervalTypes });
            list.Add(new EditableField { Row = 3, Column = 2, FieldName = "Interval", Name = fusion.GetName(i => i.Interval), FieldDescription = fusion.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString() });

            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "Description", Name = fusion.GetName(i => i.Description), FieldDescription = fusion.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.FusionType, ft).ToList(), 5);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
        public JsonResult Fusion_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Fusion>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
        public JsonResult Fusion_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Fusion>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Manual", Name = a.GetName(i => i.Manual), FieldDescription = a.GetDescription(i => i.Manual), FieldType = DataType.Boolean.ToString(), Value = a.Manual.ToString().ToLower() });
            list.Add(new EditableField { Row = 2, Column = 2, FieldName = "Enabled", Name = a.GetName(i => i.Enabled), FieldDescription = a.GetDescription(i => i.Enabled), FieldType = DataType.Boolean.ToString(), Value = a.Enabled.ToString().ToLower() });
            list.Add(new EditableField { Row = 2, Column = 3, FieldName = "LockPromotedItems", Name = a.GetName(i => i.LockPromotedItems), FieldDescription = a.GetDescription(i => i.LockPromotedItems), FieldType = DataType.Boolean.ToString(), Value = a.LockPromotedItems.ToString().ToLower() });

            var intervalTypes = new List<SelectListItem>();
            intervalTypes.Add(new SelectListItem { Text = "Minute(s)", Value = "3" });
            intervalTypes.Add(new SelectListItem { Text = "Hour(s)", Value = "2" });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "IntervalType", Name = a.GetName(i => i.IntervalType), FieldDescription = a.GetDescription(i => i.IntervalType), FieldType = DataType.Lookup.ToString(), Items = intervalTypes, Value = a.IntervalType.HasValue ? ((int)a.IntervalType.Value).ToString() : "" });
            list.Add(new EditableField { Row = 3, Column = 2, FieldName = "Interval", Name = a.GetName(i => i.Interval), FieldDescription = a.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString(), Value = (a.Interval.HasValue ? a.Interval.Value.ToString() : "") });
            list.Add(new EditableField { Row = 3, Column = 3, FieldName = "ForceRefresh", Name = "Force Refresh on Next Run?", FieldDescription = "Force the local agent to perform a full refresh of this configuration on the next run.", FieldType = DataType.Boolean.ToString() });

            list.Add(new EditableField { Row = 4, Column = 1, Required = true, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.FusionType, a.FusionTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Fusion, id).ToList(), 5);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        //[Route("fusion/{typeID:int}/configurations/add")]
        public ActionResult AddFusion(int typeID)
        {
            var type = Company.GetById<FusionType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Fusion,
                FieldUri = string.Format("/form/Fusion_AddFields?ft={0}", typeID),
                FormTitle = string.Format(Resources.FormInfo.Add_Generic_Title, type.Name),
                FormUri = "/form/AddFusion",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusion(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                int typeID = parseIntField(form, "FusionTypeID");
                var type = Company.GetById<FusionType>(typeID);
                if (type == null) throw new NotFoundException("fusion type");

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
                    Name = parseTextField(form, "Name", null, true)//,
                };

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Fusion, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.FusionType, typeID).ToList(), form, Server);

                Company.SaveOrUpdate<Fusion>(model, fields);

                return jsonSuccess(type.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult AddFusionSpreadsheetImport(int typeID, int id)//, int attributeTypeID)
        {
            var fusion = Company.GetById<Fusion>(id, i => i.FusionType.FusionAttributeTypes);
            //ViewData.Add("FusionAttributeTypeID", attributeTypeID);

            ViewBag.FusionTypeID = typeID;
            ViewBag.FusionID = id;

            return PartialView(fusion.FusionType.FusionAttributeTypes.ToList());
        }


        public ActionResult DeleteFusion(int id)
        {
            var a = Company.GetById<Fusion>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Fusion,
                FieldUri = string.Format("/form/Fusion_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteFusion",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusion(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var model = Company.GetById<Fusion>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("configuration");

                Company.Delete<Fusion>(model);
                return jsonSuccess("Item successfully removed.", model.ID.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteFusionByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusion(form);
        }

        public ActionResult EditFusion(int id)
        {
            var a = Company.GetById<Fusion>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Fusion,
                FieldUri = string.Format("/form/Fusion_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditFusion",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFusion(FormCollection form)//(int typeID, int id, FusionAttributeType model)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var model = Company.GetById<Fusion>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("configuration");

                model.Description = parseTextField(form, "Description");
                model.Enabled = parseBooleanField(form, "Enabled");
                model.LockPromotedItems = parseBooleanField(form, "LockPromotedItems");
                model.Manual = parseBooleanField(form, "Manual");
                model.Name = parseTextField(form, "Name", null, true);
                model.IntervalType = (JobIntervalType)Enum.Parse(typeof(JobIntervalType), form["IntervalType"]);
                model.Interval = parseIntField(form, "Interval");
                model.ForceRefresh = parseBooleanField(form, "ForceRefresh");


                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Fusion, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.FusionType, model.FusionTypeID).ToList(), form, Server, false);

                Company.SaveOrUpdate<Fusion>(model, fields);

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditTechnicalMapping(int id)
        {
            var a = Company.GetById<MapRuleItem>(id);
            if (a == null) return HttpNotFound();

            var model = new EditableForm
            {
                Context = ContextList.FusionTechnicalMapping,
                FieldUri = string.Format("/form/FusionTechnicalMapping_EditMapping?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Edit_Generic_Title, "Technical Mapping"),
                FormUri = "/form/EditTechnicalMapping",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPut, ValidateInput(false)]
        public JsonResult EditTechnicalMapping(FormCollection form)
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
                Company.Query<int>(@"delete [dbo].[mapruleitemmaprule] where [mapruleitemid] = @id", new { id = model.ID });

                //add new ones
                foreach (var rule in existingRuleArray)
                {
                    // add mapping
                    Company.Query<int>(@"insert [dbo].[mapruleitemmaprule] (mapruleid,mapruleitemid) values(@ruleId, @itemId)", new { itemId = model.ID, ruleId = rule });
                }

                return jsonSuccess("successfully created mapping.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult AddTechnicalMapping()
        {            
            var model = new EditableForm
            {
                Context = ContextList.FusionTechnicalMapping,
                FieldUri = string.Format("/form/FusionTechnicalMapping_AddMapping"),
                FormTitle = string.Format(Resources.FormInfo.Add_Generic_Title, "Technical Mapping"),
                FormUri = "/form/AddTechnicalMapping",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }
        
        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddTechnicalMapping(FormCollection form)
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

                return jsonSuccess("successfully created mapping.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteTechnicalMapping(int id)
        {
            var a = Company.GetById<MapRuleItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "FusionTechnicalMapping",
                FieldUri = string.Format("/form/FusionTechincalMapping_DeleteFields?ID={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "the selected mapping"),
                FormUri = "/form/DeleteTechnicalMapping",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteTechnicalMapping(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");

                var mapRuleItemId = parseIntField(form, "ID");

                var model = Company.GetById<MapRuleItem>(mapRuleItemId);
                if (model == null) throw new NotFoundException("configuration");

                //delete the map rule item map rule record
                Company.Query<int>(@"delete [dbo].[mapruleitemmaprule] where [mapruleitemid] = @id", new { id = model.ID });
                                
                //delete the map rule item
                Company.Delete<MapRuleItem>(model);
                return jsonSuccess("Item successfully removed.", model.ID.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region FusionFilter

        #region Field Generation

        public JsonResult FusionFilter_AddFields(int f)
        {
            if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var fusion = Company.GetById<Fusion>(f);
            var types = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == fusion.FusionTypeID && !i.ParentID.HasValue).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "FusionID", FieldType = DataType.Hidden.ToString(), Value = f.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FusionAttributeTypeID", Name = "Fusion Attribute Type", FieldType = DataType.Lookup.ToString(), Items = types });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "FilterValue", Name = "Filter", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "FilterValue", true, @"^([A-Za-z0-9]{2,})(\,[A-Za-z0-9]{2,})*$", 2, 500, "may only contain letters and numbers,  with each segment separated by a comma (i.e.  xxx,yyy)") }); //, "may only contain letters and numbers,  with each segment separated by a comma (i.e.  xxx,yyy)"

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonNetResult GetFusionAttributeTypes(int fusionID)
        {
            var fusion = Company.GetById<Fusion>(fusionID);
            var types = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == fusion.FusionTypeID && !i.ParentID.HasValue).OrderBy(i => i.Name).ToList();
            return new JsonNetResult
            {
                Data = types,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
                
        public JsonResult FusionTechincalMapping_DeleteFields(int ID)
        {
            var list = new List<EditableField>();

           // if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Delete))
             //   return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = ID.ToString() });
            
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult FusionTechnicalMapping_EditMapping(int id)
        {
            var a = Company.GetById<MapRuleItem>(id);
            if (a == null) throw new Exception("Error cannot find technical mapping.");

            var list = new List<EditableField>();

            var types = Company.Filter<Artifact>(i => i.ArtifactType.CanOwnFusion == true).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
            types.Insert(0, new SelectListItem { Text = "", Value = "" });
                        
            var rules = Company.MapRules.OrderBy(x=>x.Transformation).AsEnumerable().Select(i => new SelectListItem { Text = string.Format("ID:{0} - Transformation Name:{1}", i.ID, i.Transformation??"N/A"), Value = i.ID.ToString(), Selected = a.MapRules.Any(c=>c.ID == i.ID) }).ToList();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "SourceArtifact", Name = "Source Artifact", FieldType = DataType.Lookup.ToString(), Items = types, Value = (a.SourceOwner == "Artifact" ? a.SourceOwnerID.ToString() : "") });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "SourceFusionAttribute", Name = "Source Fusion Attribute", FieldType = DataType.Text.ToString(), Value = a.SourceFusionAttribute.TextPath, TypeaheadUri = "/api/fusion/textpathautocomplete" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "TargetArtifact", Name = "Target Artifact", FieldType = DataType.Lookup.ToString(), Items = types, Value = (a.TargetOwner == "Artifact" ? a.TargetOwnerID.ToString() : "") });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "TargetFusionAttribute", Name = "Target Fusion Attribute", FieldType = DataType.Text.ToString(), Value = a.TargetFusionAttribute.TextPath, TypeaheadUri = "/api/fusion/textpathautocomplete" });
            
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TargetRule", Name = "Map Rule", FieldType = DataType.Lookup.ToString(), Items = rules, MultiSelect = true });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult FusionTechnicalMapping_AddMapping()
        {
            var list = new List<EditableField>();

            var types = Company.Filter<Artifact>(i => i.ArtifactType.CanOwnFusion == true).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            var rules = Company.MapRules.OrderBy(x=>x.Transformation).AsEnumerable().Select(i => new SelectListItem { Text = string.Format("ID:{0} - Transformation Name:{1}",i.ID, i.Transformation ?? "N/A"), Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "SourceArtifact", Name = "Source Artifact", FieldType = DataType.Lookup.ToString(), Items = types });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "SourceFusionAttribute", Name = "Source Fusion Attribute", FieldType = DataType.Text.ToString(), TypeaheadUri= "/api/fusion/textpathautocomplete" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "TargetArtifact", Name = "Target Artifact", FieldType = DataType.Lookup.ToString(), Items = types });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "TargetFusionAttribute", Name = "Target Fusion Attribute", FieldType = DataType.Text.ToString(), TypeaheadUri = "/api/fusion/textpathautocomplete" });
                        
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "TargetRule", Name = "Map Rule", FieldType = DataType.Lookup.ToString(), Items = rules, MultiSelect = true });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult FusionFilter_DeleteFields(int f, int a)
        {
            var list = new List<EditableField>();

            if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "FusionID", FieldType = DataType.Hidden.ToString(), Value = f.ToString() });
            list.Add(new EditableField { FieldName = "FusionAttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = a.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult FusionFilter_EditFields(int f, int a)
        {
            var list = new List<EditableField>();
            var o = Company.Filter<FusionFilter>(i => i.FusionID == f && i.FusionAttributeTypeID == a).SingleOrDefault();

            if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "FusionID", FieldType = DataType.Hidden.ToString(), Value = f.ToString() });
            list.Add(new EditableField { FieldName = "FusionAttributeTypeID", FieldType = DataType.Hidden.ToString(), Value = a.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FilterValue", Name = "Filter", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "FilterValue", true, @"^([A-Za-z0-9]{2,})(\,[A-Za-z0-9]{2,})*$", 2, 500, "may only contain letters and numbers,  with each segment separated by a comma (i.e.  xxx,yyy)"), Value = o.Filter });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddFusionFilter(int f)
        {
            if (!Company.Exists<Fusion>(f)) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FusionFilter,
                FieldUri = string.Format("/form/FusionFilter_AddFields?f={0}", f),
                FormTitle = "Add Filter",
                FormUri = "/form/AddFusionFilter",
                FormMethod = "POST"
            };

            return PartialView("OverlayEditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionFilter(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion filter");

                int f = parseIntField(form, "FusionID");
                int a = parseIntField(form, "FusionAttributeTypeID");

                if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new FusionFilter
                {
                    FusionID = f,
                    FusionAttributeTypeID = a,
                    Filter = parseTextField(form, "FilterValue")
                };

                Company.Add<FusionFilter>(model);
                return jsonSuccess("Filter successfully created.", a.ToString(), form["_context"], "add", HttpStatusCode.Created, new { Type = "FusionFilter", Context = form["_context"] });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ActionName("FusionFilter"), HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken]
        public JsonResult PostFusionFilter(FusionFilter f)
        {
            try
            {
             //   if (!form.HasKeys()) throw new NoFormDataException("fusion filter");

               // int f = parseIntField(form, "FusionID");
                //int a = parseIntField(form, "FusionAttributeTypeID");

                if (!Company.HasPermission(SystemObjects.Fusion, f.FusionID, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new FusionFilter
                {
                    FusionID = f.FusionID,
                    FusionAttributeTypeID = f.FusionAttributeTypeID,
                    Filter = f.Filter  //parseTextField(form, "FilterValue")
                };

                Company.Add<FusionFilter>(model);
                return jsonSuccess("Filter successfully created.", f.FusionAttributeTypeID.ToString(), null, "add", HttpStatusCode.Created, new { Type = "FusionFilter", Context = "FusionFilter" });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteFusionFilter(int f, int a)
        {
            var o = Company.Filter<FusionFilter>(i => i.FusionID == f && i.FusionAttributeTypeID == a).SingleOrDefault();
            if (o == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FusionFilter,
                FieldUri = string.Format("/form/FusionFilter_DeleteFields?f={0}&a={1}", f, a),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "Filter"),
                FormUri = "/form/DeleteFusionFilter",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionFilter(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion filter");

                int f = parseIntField(form, "FusionID");
                int a = parseIntField(form, "FusionAttributeTypeID");

                if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<FusionFilter>(i => i.FusionAttributeTypeID == a && i.FusionID == f);

                return jsonSuccess("Filter successfully removed.", a.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteFusionFilterByID(int fusionID, int fusionAttributeTypeID)
        {
            var form = new FormCollection();
            form.Add("FusionID", fusionID.ToString());
            form.Add("FusionAttributeTypeID", fusionAttributeTypeID.ToString());

            return DeleteFusionFilter(form);
        }

        public ActionResult EditFusionFilter(int f, int a)
        {
            var o = Company.Filter<FusionFilter>(i => i.FusionID == f && i.FusionAttributeTypeID == a).SingleOrDefault();
            if (o == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FusionFilter,
                FieldUri = string.Format("/form/FusionFilter_EditFields?f={0}&a={1}", f, a),
                FormTitle = "Edit Filter",
                FormUri = "/form/EditFusionFilter",
                FormMethod = "PUT"
            };

            return PartialView("OverlayEditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFusionFilter(FormCollection form)//(int typeID, int id, FusionAttributeType model)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion filter");

                int f = parseIntField(form, "FusionID");
                int a = parseIntField(form, "FusionAttributeTypeID");

                var o = Company.Filter<FusionFilter>(i => i.FusionID == f && i.FusionAttributeTypeID == a).SingleOrDefault();
                if (o == null) throw new NotFoundException("fusion filter");

                if (!Company.HasPermission(SystemObjects.Fusion, f, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                o.Filter = parseTextField(form, "FilterValue");

                Company.Update<FusionFilter>(o);

                return jsonSuccess("Filter successfully updated.", a.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { Type = "FusionFilter", Context = form["_context"] });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ActionName("FusionFilter"), HttpPut, ValidateInput(false)]
        public JsonResult PutFusionFilter(FusionFilter f)
        {
            try
            {
             //   if (!form.HasKeys()) throw new NoFormDataException("fusion filter");

               // int f = parseIntField(form, "FusionID");
                //int a = parseIntField(form, "FusionAttributeTypeID");

                var o = Company.Filter<FusionFilter>(i => i.FusionID == f.FusionID && i.FusionAttributeTypeID == f.FusionAttributeTypeID).SingleOrDefault();
                if (o == null) throw new NotFoundException("fusion filter");

                if (!Company.HasPermission(SystemObjects.Fusion, f.FusionID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                o.Filter = f.Filter; //parseTextField(form, "FilterValue");

                Company.Update<FusionFilter>(o);

                return jsonSuccess("Filter successfully updated.", f.FusionAttributeTypeID.ToString(), null, "edit", HttpStatusCode.OK, new { Type = "FusionFilter", Context = "FusionFilter" });
            }
            catch (BaseException ex)
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

        #region FusionOwnerRule

        #region Field Generation

        /// <param name="id">FusionAttributeOwnerRuleID</param>
        public JsonResult FusionOwnerRule_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/owners/add")]
        public ActionResult AddFusionOwnerRule(int typeID, int fusionID)
        {
            //if (!Company.HasPermission(SystemObjects.AttributeType, 0, Claim.Create))
            //    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var model = new FusionOwnerRuleEditorModel()
            {
                FusionID = fusionID,
                FusionTypeID = typeID,
                FormUri = "/Form/AddFusionOwnerRule",
                FormMethod = "POST",
                FormName = "Add Owner Rule",
                Rule = new FusionAttributeOwnerRule { FusionID = fusionID },
                AttributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == typeID).ToList(),
            };
            return PartialView("FusionAttributeOwnerRuleEditForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionOwnerRule(FormCollection form)//(FusionOwnerEditListModel model)
        {
            try
            {
                var item = new FusionAttributeOwnerRule
                {
                    RelationshipOwnerObjectType = "Artifact",
                    RelationshipOwnerObjectID = parseIntField(form, "FusionOwnerOptionsDropdown"),
                    FusionID = parseIntField(form, "FusionID"),
                    ObjectType = "FusionAttributeType",
                    ObjectID = parseIntField(form, "FusionAttributeTypeID"),
                };
                Company.Add<FusionAttributeOwnerRule>(item);

                return jsonSuccess("Items assigned to owner", "0", ContextList.FusionOwnerRule, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteFusionOwnerRule(int id)
        {
            var model = new EditableForm
            {
                Context = ContextList.FusionOwnerRule,
                FieldUri = string.Format("/form/FusionOwnerRule_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this ownership rule"),
                FormUri = "/form/DeleteFusionOwnerRule",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionOwnerRule(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("owner");

                var model = Company.GetById<FusionAttributeOwnerRule>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("owner");

                Company.Delete<FusionAttributeOwnerRule>(model);
                return jsonSuccess("Item successfully removed.", model.ID.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditFusionOwnerRule(int id)
        {
            var a = Company.GetById<FusionAttributeOwnerRule>(id, i => i.Fusion);
            if (a == null) return HttpNotFound();

            var model = new FusionOwnerRuleEditorModel
            {
                FusionID = a.FusionID,
                FusionTypeID = 0,
                FormUri = "/Form/EditFusionOwnerRule",
                FormMethod = "PUT",
                FormName = "Update Ownership Rule",
                Rule = a,
                AttributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == a.Fusion.FusionTypeID).ToList()
            };

            return PartialView("FusionAttributeOwnerRuleEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFusionOwnerRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("promotion rule");

                var model = Company.GetById<FusionAttributePromotionRule>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("promotion rule");

                var promotionObject = form["PrOptionsDropdown"].Split('|');
                var promotionParent = form["PrOptionsParentDropdown"];

                model.Enabled = parseBooleanField(form, "Enabled");
                model.ObjectID = parseIntField(form, "FusionAttributeTypeID");
                model.PromotionObjectType = promotionObject[0];
                model.PromotionObjectID = int.Parse(promotionObject[1]);

                if (!string.IsNullOrEmpty(promotionParent))
                {
                    model.PromotionParentObjectType = model.PromotionObjectType.Replace("Type", "");
                    model.PromotionParentObjectID = int.Parse(promotionParent);
                }

                Company.Update<FusionAttributePromotionRule>(model);

                return jsonSuccess("Promotion rule successfully updated.", model.ID.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region FusionOwnerRuleItem

        #region Field Generation

        public JsonResult FusionAttributeOwnerRuleItem_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddFusionAttributeOwnerRuleItem(int id)
        {
            var rule = Company.GetById<FusionAttributeOwnerRule>(id, i => i.Fusion);

            if (rule == null)
                return jsonException("Rule not found", HttpStatusCode.NotFound);

            if (!Company.HasPermission(SystemObjects.Fusion, rule.FusionID, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionOwnerRuleItemEditorModel
            {
                FormUri = "/Form/AddFusionAttributeOwnerRuleItem",
                FormMethod = "POST",
                FormName = "Add Ownership Target Item",
                FusionID = rule.FusionID,
                TargetFusionAttributeTypeID = rule.ObjectID,
                Item = new FusionAttributeOwnerRuleItem { FusionAttributeOwnerRuleID = id }
            };
            return PartialView("FusionAttributeOwnershipRuleItemEditForm", editorModel);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionAttributeOwnerRuleItem(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "FusionAttributeOwnerRuleID");
                var fusionAttributeIDs = form["FusionAttributeID"].Split(',').ToList();
                if (fusionAttributeIDs.Count == 0)
                {
                    Company.Set<FusionAttributeOwnerRuleItem>().Add(
                        new FusionAttributeOwnerRuleItem { FusionAttributeOwnerRuleID = ruleID, FusionAttributeID = null }
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
                        Company.Set<FusionAttributeOwnerRuleItem>().Add(
                            new FusionAttributeOwnerRuleItem { FusionAttributeOwnerRuleID = ruleID, FusionAttributeID = fusionAttributeID }
                            );
                    });
                }
                Company.SaveChanges();

                return jsonSuccess("Target item(s) successfully created.", "0", ContextList.FusionOwnerRuleItem, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionAttributeOwnerRuleItem(int id)
        {
            var a = Company.GetById<FusionAttributeOwnerRuleItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionOwnerRuleItem,
                FieldUri = string.Format("/form/FusionAttributeOwnerRuleItem_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this target item"),
                FormUri = "/form/DeleteFusionAttributeOwnerRuleItem",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionAttributeOwnerRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                Company.Delete<FusionAttributeOwnerRuleItem>(i => i.ID == id);
                return jsonSuccess("Target item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/rule/add")]
        public ActionResult AddFusionRule(int typeID, int fusionID)
        {
            var model = new FusionRuleEditorModel
            {
                FusionID = fusionID,
                FusionTypeID = typeID,
                FormUri = "/Form/AddFusionRule",
                FormMethod = "POST",
                FormName = "Add Fusion Rule",
                AttributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == typeID).ToList(),
                Rule = new FusionRule { FusionID = fusionID, Enabled = true }
            };
            return PartialView("FusionRuleEditForm", model);

        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                return jsonSuccess("Items marked for auto-promotion", "0", ContextList.FusionRule, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionRule(int id)
        {
            var a = Company.GetById<FusionRule>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionRule,
                FieldUri = string.Format("/form/FusionRule_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this promotion rule"),
                FormUri = "/form/DeleteFusionRule",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                Company.Delete<FusionRule>(i => i.ID == id);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditFusionRule(int id)
        {
            var a = Company.GetById<FusionRule>(id);
            if (a == null) return HttpNotFound();

            var model = new FusionRuleEditorModel
            {
                FusionID = a.Fusion.ID,
                FusionTypeID = a.Fusion.FusionTypeID,
                FormUri = "/Form/EditFusionRule",
                FormMethod = "PUT",
                FormName = "Edit Fusion Rule",
                AttributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == a.Fusion.FusionTypeID).ToList(),
                Rule = a
            };
            return PartialView("FusionRuleEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess("Fusion rule successfully updated.", model.ID.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        /// <param name="id">FusionAttributePromotionRuleID</param>
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

        #region FusionRuleItem

        #region Field Generation

        public JsonResult FusionRuleItem_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddFusionRuleItem(int id)
        {
            var rule = Company.GetById<FusionRule>(id);

            if (rule == null)
                return jsonException("Rule not found", HttpStatusCode.NotFound);

            if (!Company.HasPermission(SystemObjects.Fusion, rule.FusionID, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionRuleItemEditorModel
            {
                FormUri = "/Form/AddFusionRuleItem",
                FormMethod = "POST",
                FormName = "Add Promotion Target Item",
                FusionID = rule.FusionID,
                TargetFusionAttributeTypeID = rule.ObjectID,
                Item = new FusionRuleItem { RuleID = id }
            };
            return PartialView("FusionRuleItemEditForm", editorModel);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionRuleItem(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "FusionAttributePromotionRuleID");
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
                        new FusionRuleItem { RuleID = ruleID, FusionAttributeID = null }
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
                            new FusionRuleItem { RuleID = ruleID, FusionAttributeID = fusionAttributeID }
                            );
                    });
                }
                Company.SaveChanges();

                return jsonSuccess("Target item(s) successfully created.", "0", ContextList.FusionPromotionRuleItem, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionRuleItem(int id)
        {
            var a = Company.GetById<FusionRuleItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionPromotionRuleItem,
                FieldUri = string.Format("/form/FusionRuleItem_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this target item"),
                FormUri = "/form/DeleteFusionRuleItem",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
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
                return jsonSuccess("Target item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region FusionRuleStep

        #region Form Get/Post
        [Route("fusion/rule/{ruleID:int}/step/add")]
        public ActionResult AddFusionRuleStep(int ruleID)
        {
            if (ruleID <= 0) return new HttpNotFoundResult();

            var rule = Company.GetById<FusionRule>(ruleID);

            if (rule == null) return new HttpNotFoundResult();

            return PartialView("FusionRuleStepEditForm",
                new FusionRuleStepEditorModel
                {
                    FormUri = "/form/AddFusionRuleStep",
                    FormMethod = "POST",
                    RuleStep = new FusionRuleStep { Action = "promote", Step = rule.FusionRuleSteps.Count + 1, RuleID = ruleID, FusionRule = rule },
                    FormName = "Add Fusion Rule Step",
                    FusionID = rule.FusionID,
                    FusionTypeID = rule.Fusion.FusionTypeID
                });
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public ActionResult AddFusionRuleStep(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "RuleID");

                if (ruleID <= 0) return new HttpNotFoundResult();

                var rule = Company.GetById<FusionRule>(ruleID);

                var item = new FusionRuleStep
                {
                    Action = parseTextField(form, "Action"),
                    Description = parseTextField(form, "Description"),
                    Step = parseIntField(form, "Step"),
                    RuleID = rule.ID
                };

                rule.FusionRuleSteps.Add(item);
                if (rule != null)
                {
                    rule.UpdatedBy = Company.CurrentResourceID;
                    rule.UpdatedOn = DateTime.UtcNow;
                }

                AddPromotionStepSettings(item, form);

                Company.SaveChanges();

                return jsonSuccess("New Fusion Rule Step Added", "0", ContextList.FusionRuleStep, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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
            }
            else if (action == "FIND")
            {
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
                else
                {
                    handleSearchParameters("Find", "Object", item.FusionRuleStepSettings, findType, item.ID, form);
                }
            }
            else if (action == "RELATE")
            {
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
            }
            else if (action == "LINEAGE")
            {
                var intersectType = parseTextField(form, "LineageIntersectType");

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "IntersectType", Value = intersectType });

                var subjectSearch = parseTextField(form, "LineageSubjectSearchType");

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "SubjectSearch", Value = subjectSearch });

                handleSearchParameters("Lineage", "Subject", item.FusionRuleStepSettings, subjectSearch, item.ID, form);

                var objectSearch = parseTextField(form, "LineageObjectSearchType");

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "ObjectSearch", Value = objectSearch });

                handleSearchParameters("Lineage", "Object", item.FusionRuleStepSettings, objectSearch, item.ID, form);

                var focalSearch = parseTextField(form, "LineageFocalSearchType");

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "FocalSearch", Value = focalSearch });

                handleSearchParameters("Lineage", "Focal", item.FusionRuleStepSettings, focalSearch, item.ID, form);

                item.FusionRuleStepSettings.Add(new FusionRuleStepSetting { RuleStepID = item.ID, Name = "Predicate", Value = parseTextField(form, "LineagePredicate") });
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
        }

        [Route("fusion/rule/{ruleID:int}/step/edit/{ruleStepID:int}")]
        public ActionResult EditFusionRuleStep(int ruleID, int ruleStepID)
        {
            var rule = Company.GetById<FusionRule>(ruleID);
            if (rule == null) return new HttpNotFoundResult();

            var step = rule.FusionRuleSteps.SingleOrDefault(x => x.ID == ruleStepID);
            if (step == null) return new HttpNotFoundResult();

            return PartialView("FusionRuleStepEditForm",
                new FusionRuleStepEditorModel
                {
                    FormUri = "/form/EditFusionRuleStep",
                    FormMethod = "PUT",
                    RuleStep = step,
                    FormName = "Edit Fusion Rule Step",
                    FusionID = rule.FusionID,
                    FusionTypeID = rule.Fusion.FusionTypeID
                });
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPut, ValidateInput(false)]
        public ActionResult EditFusionRuleStep(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "RuleID");
                var ruleStepID = parseIntField(form, "RuleStepID");

                if (ruleID <= 0 || ruleStepID <= 0) return new HttpNotFoundResult();

                var rule = Company.GetById<FusionRule>(ruleID);

                if (rule == null) return new HttpNotFoundResult();

                var step = rule.FusionRuleSteps.First(x => x.ID == ruleStepID);

                if (step == null) return new HttpNotFoundResult();

                step.Description = parseTextField(form, "Description");
                step.Step = parseIntField(form, "Step");
                step.Action = parseTextField(form, "Action");
                
                rule.UpdatedBy = Company.CurrentResourceID;
                rule.UpdatedOn = DateTime.UtcNow;

                //remove old step settings                
                for (int i = step.FusionRuleStepSettings.Count - 1; i >= 0; i--)
                {
                    Company.ObjectContext.DeleteObject(step.FusionRuleStepSettings.ElementAt(i));
                }

                AddPromotionStepSettings(step, form);

                Company.SaveChanges();

                return jsonSuccess("Step updated", "0", ContextList.FusionRuleStep, "add", HttpStatusCode.Accepted);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("fusion/rule/{ruleID:int}/step/delete/{ruleStepID:int}")]
        public ActionResult DeleteFusionRuleStep(int ruleID, int ruleStepID)
        {
            var rule = Company.GetById<FusionRule>(ruleID);
            if (rule == null) return new HttpNotFoundResult();

            var step = rule.FusionRuleSteps.FirstOrDefault(x => x.ID == ruleStepID);
            if (step == null) return new HttpNotFoundResult();

            return PartialView("OverlayDeleteForm", new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionRule,
                FieldUri = $"/form/FusionRuleStep_DeleteFields?id={ruleID}&ruleStepID={ruleStepID}",
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this promotion rule step"),
                FormUri = "/form/DeleteFusionRuleStep",
                FormMethod = "DELETE"
            });
        }

        [HttpDelete]
        public ActionResult DeleteFusionRuleStep(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                var ruleStepID = parseIntField(form, "RuleStepID");
                var currentRule = Company.GetById<FusionRule>(id);
                var itemToRemove = currentRule.FusionRuleSteps.SingleOrDefault(x => x.ID == ruleStepID);

                if (itemToRemove == null) return new HttpNotFoundResult();

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

                return jsonSuccess("Step successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("fusion/rule/{ruleID:int}/step/move/{direction}/{ruleStepID:int}")]
        public ActionResult MoveFusionRuleStep(int ruleID, string direction, int ruleStepID)
        {
            var rule = Company.GetById<FusionRule>(ruleID);
            if (rule == null) return new HttpNotFoundResult();

            var step = rule.FusionRuleSteps.FirstOrDefault(x => x.ID == ruleStepID);
            if (step == null) return new HttpNotFoundResult();

            return PartialView("OverlayEditableForm", new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionRule,
                FieldUri = $"/form/FusionRuleStep_MoveFields?id={ruleID}&ruleStepID={ruleStepID}&direction={direction}",
                FormTitle = string.Format("Move this promotion rule step " + direction),
                FormUri = "/form/MoveFusionRuleStep",
                FormMethod = "POST"
            });
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public ActionResult MoveFusionRuleStep(FormCollection form)
        {
            var ruleID = parseIntField(form, "ID");
            var ruleStepID = parseIntField(form, "RuleStepID");
            var direction = parseTextField(form, "Direction");
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

            return jsonSuccess("Step successfully moved", ruleID.ToString(), ContextList.FusionRuleStep, "move", HttpStatusCode.OK);
        }

        #endregion

        #region Field Generation

        /// <param name="id">FusionAttributePromotionRuleID</param>
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
                //.Where(i => !sourceFieldIDs.Contains(i.ID))
                .Select(i => new SelectListItem
                {
                    Text = string.Format("{0} ({1})", i.FriendlyName, i.Name),
                    Value = string.Format("{0}|{1}", i.Name, i.ID)
                })
                .ToList();
            //if (!sourceFieldNames.Contains("Name"))
            sourceFields.Insert(0, new SelectListItem { Text = "ID", Value = "ID|0" });
            sourceFields.Insert(1, new SelectListItem { Text = "Name", Value = "Name|0" });
            sourceFields.Insert(2, new SelectListItem { Text = "TextPath", Value = "TextPath|0" });

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

            //var promotionType = ruleStep.PromotionObjectType;
            var promotionType = ruleStep.GetSettingValueByName("Object");
            var promotionObjectType = ruleStep.GetSettingValueByName("PromotionParentObjectType");
            var promotionObjectID = int.Parse(ruleStep.GetSettingValueByName("ObjectID"));

            switch (promotionType)
            {
                case "DomainType":
                    if (!targetFieldNames.Contains("Name"))
                        targetFields.Add(new SelectListItem { Text = "Name", Value = "Name|0" });
                    if (promotionObjectType == "Domain")
                    {
                        if (!targetFieldNames.Contains("Code"))
                            targetFields.Add(new SelectListItem { Text = "Code", Value = "Code|0" });
                    }
                    if (!targetFieldNames.Contains("Description"))
                        targetFields.Add(new SelectListItem { Text = "Description", Value = "Description|0" });
                    break;
                case "ArtifactType":
                case "TaxonomyType":
                    if (!targetFieldNames.Contains("Name"))
                        targetFields.Add(new SelectListItem { Text = "Name", Value = "Name|0" });
                    if (!targetFieldNames.Contains("Description"))
                        targetFields.Add(new SelectListItem { Text = "Description", Value = "Description|0" });
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
                    targetFields.AddRange(targetDynamicFields);

                    if (promotionType == "ArtifactType")
                    {
                        if (!targetFieldNames.Contains("Subject Area"))
                            targetFields.Add(new SelectListItem { Text = "Subject Area", Value = "TaxonomyTypeID|0" });
                    }
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

        public ActionResult AddFusionRuleStepMapping(int id)
        {
            var ruleStep = Company.GetById<FusionRuleStep>(id);

            if (ruleStep == null)
                return jsonException("Rule step not found", HttpStatusCode.NotFound);

            if (!Company.HasPermission(SystemObjects.Fusion, ruleStep.FusionRule.FusionID, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionRuleStepMappingEditorModel
            {
                FormUri = "/Form/AddFusionRuleStepMapping",
                FormMethod = "POST",
                FormName = "Add Promotion Field Mapping",
                Item = new FusionRuleStepMapping { RuleStepID = id, FusionRuleStep = ruleStep },
                SourceFields = loadSourceItemOptions(ruleStep),
                TargetFields = loadTargetItemOptions(ruleStep)
            };
            return PartialView("FusionRuleStepMappingEditForm", editorModel);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionRuleStepMapping(FormCollection form)
        {
            try
            {
                var model = new FusionRuleStepMapping
                {
                    RuleStepID = parseIntField(form, "FusionAttributePromotionRuleID")
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

                return jsonSuccess("Field mapping successfully created.", "0", ContextList.FusionPromotionRuleMapping, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionRuleStepMapping(int id)
        {
            var a = Company.GetById<FusionRuleStepMapping>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionPromotionRuleMapping,
                FieldUri = string.Format("/form/FusionRuleStepMapping_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this field mapping"),
                FormUri = "/form/DeleteFusionRuleStepMapping",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionRuleStepMapping(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                Company.Delete<FusionRuleStepMapping>(i => i.ID == id);
                return jsonSuccess("Mapping successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditFusionRuleStepMapping(int id)
        {
            var a = Company.GetById<FusionRuleStepMapping>(id);
            if (a == null) return HttpNotFound();

            var editorModel = new FusionRuleStepMappingEditorModel
            {
                FormUri = "/Form/EditFusionAttributePromotionRuleMapping",
                FormMethod = "PUT",
                FormName = "Update Promotion Field Mapping",
                Item = a,
                SourceFields = loadSourceItemOptions(a.FusionRuleStep, a),
                TargetFields = loadTargetItemOptions(a.FusionRuleStep, a)
            };

            return PartialView("FusionRuleStepMappingEditForm", editorModel);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess("Field mapping successfully updated.", model.ID.ToString(), ContextList.FusionPromotionRuleMapping, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public ActionResult AddFusionType()
        {
            var model = new EditableForm
            {
                Context = ContextList.FusionType,
                FieldUri = "/form/FusionType_AddFields",
                FormTitle = "Add Type",
                FormUri = "/form/AddFusionType",
                FormMethod = "POST",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.FusionType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("fusion type");

                var model = new FusionType
                {
                    Description = parseTextField(form, "Description"),
                    Name = parseTextField(form, "Name", null, true)
                };

                Company.Add<FusionType>(model);

                upsertObjectStyle(SystemObjects.FusionType, model.ID, form, model.Name);

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, new { ParentID = 0, Type = "FusionType", Context = form["_context"], Name = model.Name });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [ActionName("FusionType"), HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken]
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

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), null, "add", HttpStatusCode.Created, new { ParentID = 0, Type = "FusionType", Context = "FusionType", Name = model.Name });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteFusionType(int id)
        {
            var a = Company.GetById<FusionType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FusionType,
                FieldUri = string.Format("/form/FusionType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteFusionType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", model.ID.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteFusionTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionType(form);
        }

        public ActionResult EditFusionType(int id)
        {
            var a = Company.GetById<FusionType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FusionType,
                FieldUri = string.Format("/form/FusionType_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditFusionType",
                FormMethod = "PUT",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFusionType(FormCollection form)//(int typeID, int id, FusionAttributeType model)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion type");

                var model = Company.GetById<FusionType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion type");

                if (!Company.HasPermission(SystemObjects.FusionType, model.ID, Claim.Update, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Description = parseTextField(form, "Description");
                model.Name = parseTextField(form, "Name", null, true);

                Company.Update<FusionType>(model);

                upsertObjectStyle(SystemObjects.FusionType, model.ID, form, model.Name);

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { ParentID = 0, Type = "FusionType", Context = form["_context"], Name = model.Name });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ActionName("FusionType"), HttpPut, ValidateInput(false)]
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

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), null, "edit", HttpStatusCode.OK, new { ParentID = 0, Type = "FusionType", Context = "FusionType", Name = model.Name });
            }
            catch (BaseException ex)
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

        #region Field Generation

        /// <param name="fat">FusionTypeID</param>
        /// <param name="p">ParentID</param>
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
            //else
            //    list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Tab", Name = "Tab Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Tab", true, "^[a-zA-Z0-9]+$", 1, 250, "cannot contain any spaces") });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">FusionAttributeTypeID</param>
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
        public JsonResult FusionAttributeType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<FusionAttributeType>(id);

            if (!Company.HasPermission(SystemObjects.FusionType, a.FusionTypeID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddFusionAttributeType(int typeID, int parentID = 0)
        {
            var type = Company.GetById<FusionType>(typeID);
            if (type == null) return HttpNotFound();
            FusionAttributeType at = null;
            if (parentID > 0)
            {
                at = Company.GetById<FusionAttributeType>(parentID);
                if (at == null) return HttpNotFound();
            }
            var model = new EditableForm
            {
                Context = ContextList.FusionAttributeType,
                FieldUri = string.Format("/form/FusionAttributeType_AddFields?ft={0}&p={1}", typeID, parentID),
                FormTitle = string.Format("Add attribute type to {0}{1}", type.Name, ((at != null) ? " : " + at.Name : "")),
                FormUri = "/form/AddFusionAttributeType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionAttributeType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attribute type");

                int typeID = parseIntField(form, "FusionTypeID");
                int? parentID = null;
                if (form.AllKeys.Contains("ParentID"))
                {
                    parentID = parseIntField(form, "ParentID");
                }
                var type = Company.GetById<FusionType>(typeID);
                if (type == null) throw new NotFoundException("fusion type");

                if (!Company.HasPermission(SystemObjects.FusionType, typeID, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new FusionAttributeType
                {
                    FusionTypeID = typeID,
                    ParentID = parentID,
                    Assignable = true,//bool.Parse(form["Assignable"]),
                    Name = parseTextField(form, "Name", null, true)
                };

                Company.Add<FusionAttributeType>(model);
                return jsonSuccess(type.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, new { ParentID = model.ParentID, Type = "FusionAttributeType", Context = form["_context"], Name = model.Name });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ActionName("FusionAttributeType"), HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken]
        public JsonResult PostFusionAttributeType(FusionAttributeType fusion)
        {
            try
            {
                //if (!form.HasKeys()) throw new NoFormDataException("fusion attribute type");

                int typeID = fusion.FusionTypeID; // parseIntField(form, "FusionTypeID");
                int? parentID = fusion.ParentID;
                //if (form.AllKeys.Contains("ParentID"))
                //{
                //    parentID = parseIntField(form, "ParentID");
                //}
                var type = Company.GetById<FusionType>(typeID);
                if (type == null) throw new NotFoundException("fusion type");

                if (!Company.HasPermission(SystemObjects.FusionType, typeID, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var model = new FusionAttributeType
                {
                    FusionTypeID = typeID,
                    ParentID = parentID,
                    Assignable = true,//bool.Parse(form["Assignable"]),
                    Name = fusion.Name //parseTextField(form, "Name", null, true)
                };

                Company.Add<FusionAttributeType>(model);
                return jsonSuccess(type.Name + " successfully created.", model.ID.ToString(), null, "add", HttpStatusCode.Created, new { ParentID = model.ParentID, Type = "FusionAttributeType", Context = "FusionAttributeType", Name = model.Name });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteFusionAttributeType(int id)
        {
            var a = Company.GetById<FusionAttributeType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FusionAttributeType,
                FieldUri = string.Format("/form/FusionAttributeType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteFusionAttributeType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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
                return jsonSuccess("Item successfully removed.", model.ID.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteFusionAttributeTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFusionAttributeType(form);
        }

        public ActionResult EditFusionAttributeType(int id)
        {
            var a = Company.GetById<FusionAttributeType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.FusionAttributeType,
                FieldUri = string.Format("/form/FusionAttributeType_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditFusionAttributeType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFusionAttributeType(FormCollection form)//(int typeID, int id, FusionAttributeType model)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("fusion attibute type");

                var model = Company.GetById<FusionAttributeType>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("fusion attibute type");

                if (!Company.HasPermission(SystemObjects.FusionType, model.FusionTypeID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name", null, true);

                Company.Update<FusionAttributeType>(model);

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { ParentID = model.ParentID, Type = "FusionAttributeType", Context = form["_context"], Name = model.Name });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [ActionName("FusionAttributeType"), HttpPut, ValidateInput(false)]
        public JsonResult PutFusionAttributeType(FusionAttributeType fusion)
        {
            try
            {
                //if (!form.HasKeys()) throw new NoFormDataException("fusion attibute type");

                var model = Company.GetById<FusionAttributeType>(fusion.ID);
                if (model == null) throw new NotFoundException("fusion attibute type");

                if (!Company.HasPermission(SystemObjects.FusionType, model.FusionTypeID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = fusion.Name;  //parseTextField(form, "Name", null, true);

                Company.Update<FusionAttributeType>(model);

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), null, "edit", HttpStatusCode.OK, new { ParentID = model.ParentID, Type = "FusionAttributeType", Context = "FusionAttributeType", Name = model.Name });
            }
            catch (BaseException ex)
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

        #region Intersect/Other Relationships

        [ValidateHttpAntiForgeryToken]
        [HttpPost, Route("RelatedArtifact/{s:int}/{t:int}")]
        public JsonResult AddRelatedArtifact(int s, int t)
        {
            try
            {
                if (
                    !Company.HasPermission(SystemObjects.Artifact, s, Claim.Update, ClaimObject.Relationship) ||
                    !Company.HasPermission(SystemObjects.Artifact, t, Claim.Update, ClaimObject.Relationship)
                    )
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Add, "You do not have permission to relate this artifact.");

                Company.AddRelatedArtifact(s, t);
                return jsonSuccess("Relationship successfully created.", "0", "action", "add", HttpStatusCode.Created, new { commandname = "RelatedArtifactAdded" });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusMessage, ex.StatusCode);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message.Replace(System.Environment.NewLine, " "), HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete, Route("RelatedArtifact/{s:int}/{t:int}")]
        public JsonResult DeleteRelatedArtifact(int s, int t)
        {
            try
            {
                if (
                    !Company.HasPermission(SystemObjects.Artifact, s, Claim.Update, ClaimObject.Relationship) ||
                    !Company.HasPermission(SystemObjects.Artifact, t, Claim.Update, ClaimObject.Relationship)
                    )
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Delete, "You do not have permission to remove this related artifact.");

                Company.DeleteRelatedArtifact(s, t);
                return jsonSuccess("Relationship successfully removed.", "0", "action", "delete", HttpStatusCode.OK, new { commandname = "RelatedArtifactDeleted" });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusMessage, ex.StatusCode);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message.Replace(System.Environment.NewLine, " "), HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete]
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

        #region IntersectRole

        #region Field Generation

        public JsonResult IntersectRole_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.IntersectRole, 0, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">IntersectRoleID</param>
        public JsonResult IntersectRole_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<IntersectRole>(id);
            if (!Company.HasPermission(SystemObjects.IntersectRole, id, Claim.Delete))
                return jsonException("You do not have permissions to delete this.", HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">IntersectRoleID</param>
        public JsonResult IntersectRole_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<IntersectRole>(id);

            if (!Company.HasPermission(SystemObjects.IntersectRole, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddIntersectRole()
        {
            var model = new EditableForm
            {
                Context = ContextList.IntersectRole,
                FieldUri = "/form/IntersectRole_AddFields",
                FormTitle = "Add role",
                FormUri = "/form/AddIntersectRole",
                FormMethod = "POST"
            };

            return PartialView("OverlayEditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddIntersectRole(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("role");

                if (!Company.HasPermission(SystemObjects.IntersectRole, 0, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new IntersectRole
                {
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description", null, false)
                };

                Company.Add<IntersectRole>(a);

                return jsonSuccess(a.Name + " successfully created.", string.Format("IntersectRole|{0}", a.ID), form["_context"], "add", HttpStatusCode.Created, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteIntersectRole(int id)
        {
            var a = Company.GetById<IntersectRole>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.IntersectRole,
                FieldUri = string.Format("/form/IntersectRole_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteIntersectRole",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteIntersectRole(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("role");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<IntersectRole>(id);
                if (model == null) throw new NotFoundException("role");

                if (!Company.HasPermission(SystemObjects.IntersectRole, model.ID, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<IntersectRole>(model);
                return jsonSuccess("Item successfully removed.", null, form["_context"], "delete", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditIntersectRole(int id)
        {
            var a = Company.GetById<IntersectRole>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.IntersectRole,
                FieldUri = string.Format("/form/IntersectRole_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditIntersectRole",
                FormMethod = "PUT"
            };

            return PartialView("OverlayEditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditIntersectRole(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("role");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<IntersectRole>(id);
                if (model == null) throw new NotFoundException("role");

                if (!Company.HasPermission(SystemObjects.IntersectRole, model.ID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description", null, false);

                Company.Update<IntersectRole>(model);

                return jsonSuccess(model.Name + " successfully updated.", string.Format("IntersectRole|{0}", id), form["_context"], "edit", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
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

        #region IntersectType

        #region Field Generation

        /// <param name="id">IntersectTypeID</param>
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

        public JsonNetResult IntersectType_FormData(int id)
        {
            var type = Company.GetById<IntersectType>(id, i => i.Nodes, i => i.IntersectTypePredicates);
            if (type == null) return new JsonNetResult { Data = null };

            var currentIntersects = Company.Filter<Intersect>(i => i.IntersectTypeID == id).Any();
            var first = type.Nodes.OrderBy(i => i.Order).First();
            var last = type.Nodes.OrderBy(i => i.Order).Last();

            var model = new Dictionary<string, object> {
                { "ID", id },
                { "LimitedChangesOnly", currentIntersects },
                { "Side1", $"{first.ObjectType}|{first.ObjectID}" },
                //{ "Side1DisplayText", first.MenuDisplayText },
                { "Side2", $"{last.ObjectType}|{last.ObjectID}" },
                { "Predicate", type.PredicateID }//{ "Side2DisplayText", last.MenuDisplayText }
            };

            //model.Add("Predicates", type.IntersectTypePredicates.Select(i => (int)i.PredicateType).ToArray());

            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult IntersectType_PredicateOptions()
        {
            //var models = PredicateType.Lineage.GetAsList().Select(i => new { title = i.Name, value = (int)i.ID }).OrderBy(i => i.title); //Company.Table<Predicate>().ToList().Select(i => new { title = $"{i.Type.ToString()}: {i.Name}", value = i.ID }).OrderBy(i => i.title);
            var models = Company.Table<Predicate>()
                .ToList()
                .Where(i => !i.Type.AsInfoModel().ReadOnly)
                .Select(i => new {
                    title = $"{i.Name} <span style='color: #999; font-size: 85%'>({i.Type.AsInfoModel().Name})</span>",
                    value = i.ID
                })
                .OrderBy(i => i.title);
            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult IntersectType_Side1Options()
        {
            var models = Company.GetIntersectTypeOptions()
                .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult IntersectType_Side2Options(SystemObjects type, int id, SystemObjects? side2Type = null, int? side2ID = null)
        {
            var models = Company.GetIntersectTypeOptions(type, id, side2Type, side2ID)
                .Where(i => i.Type != "IntersectType")
                .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddIntersectType()
        {
            ViewBag.ID = 0;
            return PartialView("IntersectTypeEditForm");
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddIntersectType(FormCollection form)
        {
            try
            {
                if (form == null) throw new NoFormDataException("relationship type");

                var nodes = new List<IntersectTypeNode>();

                var side1 = form["Side1"];
                short side1Order = 1;
                var side1Info = side1.Split('|');
                var node1 = new IntersectTypeNode { ObjectID = int.Parse(side1Info[1]), ObjectType = side1Info[0], Order = side1Order };
                //if (!string.IsNullOrEmpty(form["Side1DisplayText"]))
                //    node1.MenuDisplayText = form["Side1DisplayText"];

                nodes.Add(node1);

                var side2 = form["Side2"];
                short side2Order = 2;
                var side2Info = side2.Split('|');
                var node2 = new IntersectTypeNode { ObjectID = int.Parse(side2Info[1]), ObjectType = side2Info[0], Order = side2Order };
                //if (!string.IsNullOrEmpty(form["Side2DisplayText"]))
                //    node2.MenuDisplayText = form["Side2DisplayText"];
                nodes.Add(node2);

                Company.ValidateIntersectType(0, nodes);

                var predicate = form["Predicate"];

                var model = new IntersectType {
                    Nodes = nodes,
                    Subject = side1Info[0],
                    SubjectID = int.Parse(side1Info[1]),
                    Object = side2Info[0],
                    ObjectID = int.Parse(side2Info[1]),
                    IsSystem = false,
                    PredicateID = int.Parse(predicate)
                };
                Company.Add<IntersectType>(model);
                var id = model.ID;

                //if (!string.IsNullOrEmpty(form["Predicates[]"]))
                //{
                //    var predicates = form["Predicates[]"].Split(',').Select(i => (PredicateType)Enum.Parse(typeof(PredicateType), i)).ToList();

                //    predicates.ForEach(p => {
                //        Company.Set<IntersectTypePredicate>().Add(new IntersectTypePredicate() { IntersectTypeID = id, PredicateType = p });
                //    });
                //    Company.SaveChanges();
                //}

                //if (!string.IsNullOrEmpty(form["Predicates"]))
                //{
                //    var vals = form["Predicates"].TrimStart('[').TrimEnd(']');
                //    if (!string.IsNullOrEmpty(vals))
                //    {
                //        var predicates = vals.Split(',').Select(i => (PredicateType)Enum.Parse(typeof(PredicateType), i)).ToList();
                //        predicates.ForEach(p => {
                //            Company.Set<IntersectTypePredicate>().Add(new IntersectTypePredicate() { IntersectTypeID = id, PredicateType = p });
                //        });
                //        Company.SaveChanges();
                //    }                    
                //}

                return jsonSuccess(model.Name + " successfully created.", id.ToString(), ContextList.IntersectType, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteIntersectType(int id)
        {
            var type = Company.GetById<IntersectType>(id);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.IntersectType,
                FieldUri = string.Format("/form/IntersectType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, type.Name),
                FormUri = "/form/DeleteIntersectType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                var model = Company.GetById<IntersectType>(id, i => i.Nodes);
                if (model == null) throw new NotFoundException("relationship type");

                Company.Delete<IntersectType>(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditIntersectType(int id)
        {
            ViewBag.ID = id;
            return PartialView("IntersectTypeEditForm");
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditIntersectType(FormCollection form)
        {
            try
            {
                if (form == null) throw new NoFormDataException("relationship type");

                var id = int.Parse(form["ID"]);

                // Permisisons validation.
                if (!Company.HasPermission(SystemObjects.IntersectType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var model = Company.GetById<IntersectType>(id, i => i.Nodes, i => i.IntersectTypePredicates);
                if (model == null) throw new NotFoundException("relationship type");

                var nodes = new List<IntersectTypeNode>();

                var side1 = form["Side1"];
                short side1Order = 1;
                var side1Info = side1.Split('|');

                var side2 = form["Side2"];
                short side2Order = 2;
                var side2Info = side2.Split('|');

                var side1Node = new IntersectTypeNode { ObjectID = int.Parse(side1Info[1]), ObjectType = side1Info[0], Order = side1Order };
                if (!string.IsNullOrEmpty(form["Side1DisplayText"]))
                    side1Node.MenuDisplayText = form["Side1DisplayText"];
                nodes.Add(side1Node);

                var side2Node = new IntersectTypeNode { ObjectID = int.Parse(side2Info[1]), ObjectType = side2Info[0], Order = side2Order };
                if (!string.IsNullOrEmpty(form["Side2DisplayText"]))
                    side2Node.MenuDisplayText = form["Side2DisplayText"];
                nodes.Add(side2Node);

                var predicate = form["Predicate"];

                // Validation
                Company.ValidateIntersectType(id, nodes);


                // Now set the properties we need to overwrite.

                var existingSide1Node = model.Nodes.Single(i => i.Order == 1);
                existingSide1Node.ObjectType = side1Node.ObjectType;
                existingSide1Node.ObjectID = side1Node.ObjectID;
                //existingSide1Node.MenuDisplayText = side1Node.MenuDisplayText;

                var existingSide2Node = model.Nodes.Single(i => i.Order == 2);
                existingSide2Node.ObjectType = side2Node.ObjectType;
                existingSide2Node.ObjectID = side2Node.ObjectID;
                //existingSide2Node.MenuDisplayText = side2Node.MenuDisplayText;

                model.Subject = side1Info[0];
                model.SubjectID = int.Parse(side1Info[1]);
                model.Object = side2Info[0];
                model.ObjectID = int.Parse(side2Info[1]);
                model.PredicateID = int.Parse(predicate);

                Company.Update<IntersectType>(model);
                Company.Update<IntersectTypeNode>(existingSide1Node);
                Company.Update<IntersectTypeNode>(existingSide2Node);

                //List<PredicateType> predicates = null;
                //if (!string.IsNullOrEmpty(form["Predicates[]"]))
                //{
                //    predicates = form["Predicates[]"].Split(',').Select(i => (PredicateType)Enum.Parse(typeof(PredicateType), i)).ToList();
                //}

                //if (!string.IsNullOrEmpty(form["Predicates"]))
                //{
                //    var vals = form["Predicates"].TrimStart('[').TrimEnd(']');
                //    if (!string.IsNullOrEmpty(vals))
                //        predicates = vals.Split(',').Select(i => (PredicateType)Enum.Parse(typeof(PredicateType), i)).ToList();
                //    else
                //        predicates = new List<PredicateType>();
                //}

                //var invalidPredicates = model.IntersectTypePredicates.Select(i => i.PredicateType).Except(predicates).ToList();
                //invalidPredicates.ForEach(p => {
                //    var ip = model.IntersectTypePredicates.FirstOrDefault(i => i.PredicateType == p);
                //    if (ip != null)
                //    {
                //        Company.Set<IntersectTypePredicate>().Remove(ip);
                //    }
                //});
                //if (invalidPredicates.Count > 0)
                //{
                //    Company.SaveChanges();
                //}

                //predicates.ForEach(p => {
                //    if (!model.IntersectTypePredicates.Any(i => i.PredicateType == p))
                //        Company.Set<IntersectTypePredicate>().Add(new IntersectTypePredicate() { IntersectTypeID = id, PredicateType = p });
                //});
                //Company.SaveChanges();

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), ContextList.IntersectType, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult Group_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Group, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Group>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

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

        public ActionResult AddGroup()
        {
            var model = new EditableForm
            {
                Context = "groupform",
                FormSize = "small",
                FieldUri = "/form/Group_AddFields",
                FormTitle = "Add Group",
                FormUri = "/form/AddGroup",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group");

                var primaryOwnerResourceID = parseIntField(form, "PrimaryOwnerResourceID");
                var secondaryOwnerResourceID = parseNullableIntField(form, "SecondaryOwnerResourceID");

                var a = new Group
                {
                    Name = parseTextField(form, "Name", null, true),
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

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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

        public ActionResult AddGroupUser(int id)
        {
            var g = Company.GetById<Group>(id);
            if (g == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResourceGroup,
                FieldUri = string.Format("/form/Group_AddGroupUserFields?id={0}", id),
                FormTitle = "Add User to " + ((g != null) ? g.Name : "group"),
                FormUri = "/form/AddGroupUser",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddGroupUser(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group user");

                var id = parseIntField(form, "ID");
                var resourceID = parseIntField(form, "ResourceID");
                var owner = false;//bool.Parse(form["IsOwner"]);

                Company.Add<ResourceGroup>(new ResourceGroup { GroupID = id, ResourceID = resourceID, IsOwner = owner });

                return jsonSuccess("User successfully assigned.", resourceID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost, ValidateHttpAntiForgeryToken, ValidateInput(false), ActionName("ResourceGroup")]
        public JsonResult PostResourceGroup(ResourceGroup model)
        {
            try
            {
                Company.Add(model);
                return jsonSuccess("User successfully assigned.", model.ResourceID.ToString(), null, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
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

        public ActionResult DeleteGroupUser(int groupID, int resourceID)
        {
            //var g = Company.GetById<Group>(groupID);            
            //if (g == null || !Company.Filter<ResourceGroup>(i => i.GroupID == groupID && i.ResourceID == resourceID).Any()) return HttpNotFound();

            var g = Company.GetById<Group>(groupID);
            var r = Community.GetById<Resource>(resourceID);

            var model = new EditableForm
            {
                Context = ContextList.ResourceGroup,
                FieldUri = string.Format("/form/Group_DeleteGroupUserFields?groupID={0}&resourceID={1}", groupID, resourceID),
                FormTitle = string.Format("Are you sure you want to remove {0} from {1}?", ((r != null) ? r.FormatDisplayName() : "this user"), ((g != null) ? g.Name : "group")),
                FormUri = "/form/DeleteGroupUser",
                FormMethod = "PUT"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpPut]
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

                return jsonSuccess("User successfully removed from group.", resourceID.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete, ValidateHttpAntiForgeryToken, ActionName("ResourceGroup")]
        public JsonResult DeleteResourceGroup(int groupID, int resourceID)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Group, groupID, Claim.Delete, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var rg = Company.Delete<ResourceGroup>(i => i.GroupID == groupID && i.ResourceID == resourceID);

                return jsonSuccess("User successfully removed from group.", resourceID.ToString(), null, "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public ActionResult DeleteGroup(int id)
        {
            var a = Company.GetById<Group>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "groupform",
                FormSize = "small",
                FieldUri = string.Format("/form/Group_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteGroup",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Group>(id);
                if (model == null) throw new NotFoundException("group");

                Company.Delete<Group>(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteGroupByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteGroup(form);
        }
        #endregion

        #region Group : Edit

        public ActionResult EditGroup(int id)
        {
            var a = Company.GetById<Group>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "groupform",
                FormSize = "small",
                FieldUri = string.Format("/form/Group_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditGroup",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);
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

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("Group")]
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

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), null, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("Group")]
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

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), null, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet, ActionName("Group")]
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
        /// Gets a list of fusion attribute types that meet the criteria based on the reference type and source fusion attribute type ID.
        /// </summary>
        /// <returns>A list of relevant fusion attribute types.</returns>
        public JsonNetResult Lineage_IntersectRoles()
        {
            return new JsonNetResult
            {
                Data = Company
                    .Table<IntersectRole>()
                    .ToList()
                    .Select(i => new { title = $"{i.Name}", value = $"{i.ID}" })
                    .OrderBy(i => i.title),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of intersect types that support lineage.
        /// </summary>
        /// <returns>A list of relevant fusion attribute types.</returns>
        public JsonNetResult Lineage_IntersectTypes()
        {
            var lineageIntersectTypeIDs = Company.Filter<IntersectTypePredicate>(i => i.PredicateType == PredicateType.Lineage).Select(i => i.IntersectTypeID).Distinct().ToList();
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

        public JsonNetResult Lineage_IntersectTypeSources()
        {
            var lineageIntersectTypeIDs = Company.Filter<IntersectTypePredicate>(i => i.PredicateType == PredicateType.Lineage).Select(i => i.IntersectTypeID).Distinct().ToList();

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

        public JsonNetResult Lineage_IntersectTypeTargets(string type, int id)
        {
            var lineageIntersectTypeIDs = Company.Filter<IntersectTypePredicate>(i => i.PredicateType == PredicateType.Lineage).Select(i => i.IntersectTypeID).Distinct().ToList();

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

        public JsonNetResult Lineage_IntersectSharedObjects(string sourceType, int sourceTypeID, string targetType, int targetTypeID, string source, string target, int sourceID, int targetID)
        {
            var sql = @"select
	i.ID as SourceIntersectID,
	i.Object as Source,
	i.ObjectID as SourceID,
	d.Name as SourceName,
	i2.ID as ObjectIntersectID,
	i2.Object as Object,
	i2.ObjectID as ObjectID,
	d2.Name as ObjectName
 from [intersect] i
 join [intersect] i2 on 
	i2.subject = @target 
	and i2.subjectid = @targetID
	and i2.object = i.object 
	and i2.objectid = i.objectid 
	and i.id != i2.id 
	and i2.intersecttypeid in (
	 select t.id from intersecttype t
 join intersecttypepredicate tp on tp.intersecttypeid = t.id and tp.predicatetype=1
 where t.subject = @targetType and t.subjectid = @targetTypeID

	)
 join cache.objectdetails d on
	d.object = i.Object and d.objectid = i.objectid
 join cache.objectdetails d2 on
	d2.object = i2.object and d2.objectid = i2.objectid
 where 
	i.subject = @source
	and i.subjectid = @sourceID 
	and i.intersecttypeid in ( select t.id from intersecttype t
 join intersecttypepredicate tp on tp.intersecttypeid = t.id and tp.predicatetype=1
 where t.subject = @sourceType and t.subjectid = @sourceTypeID
)";

            var results = Company.Query<dynamic>(sql, new { sourceType, sourceTypeID, targetType, targetTypeID, source, target, sourceID, targetID }).ToList();

            return new JsonNetResult
            {
                Data = results,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }


        public JsonNetResult Lineage_IntersectSubjects(string sourceType, int sourceTypeID, string targetType, int targetTypeID, string source, string target, int sourceID, int targetID)
        {
            var sql = @"select i.ID as SourceIntersectID,
 i.IntersectTypeID as SourceIntersectTypeID,
 i.Object as Source,
i.ObjectID as SourceID,
d.Name as SourceName
from [intersect] i
join intersecttype t on i.intersecttypeid = t.id and t.subject = @sourceType and t.subjectid = @sourceTypeID
join intersecttypepredicate p on p.intersecttypeid = t.id and p.predicatetype = 1
join cache.objectdetails d on d.object = i.object and d.objectid = i.objectid
where  i.subject = @source and i.subjectid = @sourceID
and i.id not in (
 select
	i.ID
 from [intersect] i
 join [intersect] i2 on 
	i2.subject = @target 
	and i2.subjectid = @targetID
	--and (i2.object + '|' + cast(i2.objectid as varchar(50))) != (i.object + '|' + cast(i.objectid as varchar(50)))
	and i2.object = i.object 
	and i2.objectid = i.objectid 
	and i.id != i2.id 
	and i2.intersecttypeid in (
	 select t.id from intersecttype t
 join intersecttypepredicate tp on tp.intersecttypeid = t.id and tp.predicatetype=1
 where t.subject = @targetType and t.subjectid = @targetTypeID

	)
 join cache.objectdetails d on
	d.object = i.Object and d.objectid = i.objectid
 join cache.objectdetails d2 on
	d2.object = i2.object and d2.objectid = i2.objectid
 where 
	i.subject = @source
	and i.subjectid = @sourceID 
	and i.intersecttypeid in ( select t.id from intersecttype t
 join intersecttypepredicate tp on tp.intersecttypeid = t.id and tp.predicatetype=1
 where t.subject = @sourceType and t.subjectid = @sourceTypeID
)
)";

            var results = Company.Query<dynamic>(sql, new { sourceType, sourceTypeID, targetType, targetTypeID, source, target, sourceID, targetID }).ToList();

            return new JsonNetResult
            {
                Data = results,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        public JsonNetResult Lineage_IntersectObjects(string sourceType, int sourceTypeID, string targetType, int targetTypeID, string source, string target, int sourceID, int targetID)
        {
            var sql = @"select i.ID as SourceIntersectID,
 i.IntersectTypeID as SourceIntersectTypeID,
 i.Object as Source,
i.ObjectID as SourceID,
d.Name as SourceName
from [intersect] i
join intersecttype t on i.intersecttypeid = t.id and t.subject = @targetType and t.subjectid = @targetTypeID
join intersecttypepredicate p on p.intersecttypeid = t.id and p.predicatetype = 1
join cache.objectdetails d on d.object = i.object and d.objectid = i.objectid
where  i.subject = @target and i.subjectid = @targetID
and i.id not in (
 select
	i2.ID
 from [intersect] i
 join [intersect] i2 on 
	i2.subject = @target 
	and i2.subjectid = @targetID
	--and (i2.object + '|' + cast(i2.objectid as varchar(50))) != (i.object + '|' + cast(i.objectid as varchar(50)))
	and i2.object = i.object 
	and i2.objectid = i.objectid 
	and i.id != i2.id 
	and i2.intersecttypeid in (
	 select t.id from intersecttype t
 join intersecttypepredicate tp on tp.intersecttypeid = t.id and tp.predicatetype=1
 where t.subject = @targetType and t.subjectid = @targetTypeID

	)
 join cache.objectdetails d on
	d.object = i.Object and d.objectid = i.objectid
 join cache.objectdetails d2 on
	d2.object = i2.object and d2.objectid = i2.objectid
 where 
	i.subject = @source
	and i.subjectid = @sourceID 
	and i.intersecttypeid in ( select t.id from intersecttype t
 join intersecttypepredicate tp on tp.intersecttypeid = t.id and tp.predicatetype=1
 where t.subject = @sourceType and t.subjectid = @sourceTypeID
)
)";

            var results = Company.Query<dynamic>(sql, new { sourceType, sourceTypeID, targetType, targetTypeID, source, target, sourceID, targetID }).ToList();

            return new JsonNetResult
            {
                Data = results,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        /// <summary>
        /// Gets a list of subjects based on the given intersect type.
        /// </summary>
        /// <param name="id">The Intersect Type's ID</param>
        /// <returns>A list of name/value pairs.</returns>
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
        [HttpPost]
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
        [HttpPost]
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

        [ValidateHttpAntiForgeryToken, HttpPost]
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
        [HttpPost]
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
        [HttpPost]
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
                        var role = Company.GetById<IntersectRole>(model.IntersectRoleID);
                        if (role == null)
                        {
                            message += $"The role you provided is invalid.";
                        }
                        else
                        {
                            var newMap = new Map { IntersectRoleID = model.IntersectRoleID, Transformation = model.Transformation };
                            newMap.MapItems = new List<MapItem>();
                            newMap.MapItems.Add(new MapItem { SourceIntersectID = model.SourceIntersectID, TargetIntersectID = model.TargetIntersectID });
                            Company.Add<Map>(newMap);
                        }
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
                        o.IntersectRoleID = model.IntersectRoleID;
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
                    fieldTypeNames.Add("Focal point object type");
                    fieldTypeNames.Add("Focal point object type name");
                    fieldTypeNames.Add("Focal point subject area");
                    fieldTypeNames.Add("Focal point");

                    fieldTypeNames.Add("Source object type");
                    fieldTypeNames.Add("Source object type name");
                    fieldTypeNames.Add("Source object subject area");
                    fieldTypeNames.Add("Source object");

                    fieldTypeNames.Add("Target object type");
                    fieldTypeNames.Add("Target object type name");
                    fieldTypeNames.Add("Target object subject area");
                    fieldTypeNames.Add("Target object");

                    fieldTypeNames.Add("Predicate");
                    break;
                #endregion
                case "NewLineage":
                    #region
                    fieldTypeNames.Add("Source subject type");
                    fieldTypeNames.Add("Source subject type name");
                    fieldTypeNames.Add("Source subject subject area");
                    fieldTypeNames.Add("Source subject");
                    fieldTypeNames.Add("Source object type");
                    fieldTypeNames.Add("Source object type name");
                    fieldTypeNames.Add("Source object subject area");
                    fieldTypeNames.Add("Source object");

                    fieldTypeNames.Add("Source Fusion Configuration");
                    fieldTypeNames.Add("Source Fusion Path");

                    fieldTypeNames.Add("Target subject type");
                    fieldTypeNames.Add("Target subject type name");
                    fieldTypeNames.Add("Target subject subject area");
                    fieldTypeNames.Add("Target subject");
                    fieldTypeNames.Add("Target object type");
                    fieldTypeNames.Add("Target object type name");
                    fieldTypeNames.Add("Target object subject area");
                    fieldTypeNames.Add("Target object");

                    fieldTypeNames.Add("Target Fusion Configuration");
                    fieldTypeNames.Add("Target Fusion Path");

                    fieldTypeNames.Add("Transformation");
                    fieldTypeNames.Add("Role");
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
                            case "DomainType":
                            case "IntersectType":
                            case "TaxonomyType":
                                fieldTypeNames.AddRange(
                                    Company
                                    .Filter<FieldType>(i => 
                                        i.Object == type && 
                                        i.ObjectID == id &&
                                        i.Type != "FilteredLookup" &&
                                        i.Type != "FusionLookup" &&
                                        i.Type != "RelationLookup"
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
                            case "Domain":
                                #region
                                fieldTypeNames.Insert(0, "Code");
                                fieldTypeNames.Insert(1, "Name");
                                fieldTypeNames.Insert(2, "Description");
                                break;
                            #endregion
                            case "DomainType":
                                #region
                                fieldTypeNames.Insert(0, "Name");
                                fieldTypeNames.Insert(1, "Description");
                                fieldTypeNames.Insert(2, "Domain Group");
                                break;
                            #endregion
                            case "IntersectType":
                                #region
                                var intersectType = Company.Query<dynamic>(@"select	O.Subject, SD.Name as SubjectName, O.Object, TD.Name as ObjectName
                                from	IntersectType O
                                        inner join cache.ObjectDetails SD on SD.[Object] = O.Subject and SD.ObjectID = O.SubjectID
                                        inner join cache.ObjectDetails TD on TD.[Object] = O.Object and TD.ObjectID = O.ObjectID
                                where   O.ID = @id", new { id }).SingleOrDefault();
                                if (intersectType != null)
                                {
                                    //Do fields backwards, because of insert at 0.

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
                                //case "DomainType":
                                //    break;
                                //case "FusionType":
                                //    break;
                                //case "PolicyType":
                                //    break;
                                //case "TaxonomyType":
                                //    break;
                        }
                    }

                    break;
                    #endregion
            }

            return fieldTypeNames;
        }

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
select 'DomainType|0' as value, 'Reference' as title
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
select 'DomainType|' + cast(ID as varchar(10)) as value, 'Reference List: ' + Name as title from DomainType
union
select 'Domain|' + cast(D.ID as varchar(10)) as value, 'Reference List Item: ' + T.Name  + ' - ' + D.Name as title from Domain D inner join DomainType T on T.ID = D.DomainTypeID
) O order by title";
                    break;
                    #endregion
                case "R":   // Relation
                case "U":   // Unrelation
                    #region
                    sql = @"select 'IntersectType|' + cast(ID as varchar(10)) as value, Name as title from IntersectType where IsSystem = 0 order by Name";
                    break;
                    #endregion
                case "L":   // Lineage
                    models = new List<OptionModel> { new OptionModel { title = "Default", value = "Lineage|-1" } };
                    break;
                case "N":   // Lineage
                    models = new List<OptionModel> { new OptionModel { title = "Default", value = "NewLineage|-1" } };
                    break;
                case "T":   // Technical Lineage
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

        public JsonNetResult Load_ExpectedColumns(string type, int id)
        {
            return new JsonNetResult { Data = getFieldNamesByType(type, id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [FileDownload]
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

            /*
                    "Resource Type",
                    "Resource"             
             */

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
                        case "DomainType":
                            #region
                            var domainTypeItems = Company.Table<DomainType>().OrderBy(x => x.Name).Select(x => x.Name);

                            if (domainTypeItems.Any())
                            {
                                var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                                CreateExcelList(lookupColumns++, document, "Lookups", dv, domainTypeItems);

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
                        Company.Table<GlobalReportingResource>().ToList().Select(x => "User:" + x.FullName)
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
                else if (type == "NewLineage" && lowerColName == "role")
                {
                    var items = Company.Table<IntersectRole>().OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (type == "Lineage" && lowerColName == "predicate")
                {
                    var items = Company.Filter<Predicate>(x => x.Type == PredicateType.Lineage).OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }                
                else if (
                    (type == "NewLineage" && (lowerColName == "source subject type" || lowerColName == "source object type" || lowerColName == "target subject type" || lowerColName == "target object type")) ||
                    (type == "Lineage" && (lowerColName == "focal point object type" || lowerColName == "source object type" || lowerColName == "target object type")) ||
                    (type == "Synonym" && (lowerColName == "source object type" || lowerColName == "target object type"))
                    )
                {
                    var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);
                    var typesList = new List<string> { "Artifact", "Domain", "Policy", "Rule", "Taxonomy" };

                    CreateExcelList(lookupColumns++, document, "Lookups", dv, typesList.OrderBy(x => x));

                    document.AddDataValidation(dv);
                }
                else if (
                    (type == "NewLineage" && (lowerColName == "source subject subject area" || lowerColName == "source object subject area" || lowerColName == "target subject subject area" || lowerColName == "target object subject area")) ||
                    (type == "Lineage" && (lowerColName == "focal point subject area" || lowerColName == "source object subject area" || lowerColName == "target object subject area") ) ||
                    (type == "Synonym" && (lowerColName == "source object subject area" || lowerColName == "target object subject area"))
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
                else if ( (type == "NewLineage" || type == "TechnicalLineage") && (lowerColName == "source fusion configuration" || lowerColName == "target fusion configuration") )
                {
                    var items = Company.Table<Fusion>().OrderBy(x => x.Name).Select(x => x.Name);

                    if (items.Any())
                    {
                        var dv = document.CreateDataValidation(2, i + 1, 1000, i + 1);

                        CreateExcelList(lookupColumns++, document, "Lookups", dv, items);

                        document.AddDataValidation(dv);
                    }
                }
                else if (type == "DomainType" && lowerColName == "domain group")
                {
                    var items = Company.Filter<DomainGroup>(x => x.DomainTypeID == id).OrderBy(x => x.Name).Select(x => x.Name);

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

            if (type == "ArtifactType" && (columnName != "name" && columnName != "subject area" && columnName != parentColumnName))
                required = false;
            else if (type == "Domain" && (columnName != "name" && columnName != "code"))
                required = false;
            else if (type == "DomainType" && (columnName != "name" && columnName != "domain group"))
                required = false;
            else if (type == "NewLineage" && (columnName == "source fusion configuration" || columnName == "target fusion configuration" || columnName == "source fusion path" || columnName == "target fusion path"))
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

        public ActionResult AddLoad()
        {

            return PartialView();
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost]
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
                    // use bulkloaddev queue to debug bulk load web job
                    //Company.Enqueue(QueueType.BulkLoadDev, new BulkLoadInfo { CompanyID = Company.CurrentCompanyID, LoadID = load.ID, To = QueueAction.BulkLoad });

                    // regular production queue
                    Company.Enqueue(QueueType.BulkLoad, new BulkLoadInfo { CompanyID = Company.CurrentCompanyID, LoadID = load.ID, To = QueueAction.BulkLoad });
                    json = jsonSuccess("File uploaded and queued for processing.", load.ID.ToString(), ContextList.Load, "A", HttpStatusCode.Created);
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

        #endregion

        #region Lookup

        #region Field Generation

        /// <param name="id">LookupTypeID</param>
        public JsonResult Lookup_AddFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var type = Company.GetById<LookupType>(id);

            list.Add(new EditableField { FieldName = "LookupTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, id).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupID</param>
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
        public JsonResult Lookup_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Lookup>(id);

            if (!Company.HasPermission(SystemObjects.LookupType, a.LookupTypeID, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, a.LookupTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Lookup, id).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post
        

        public ActionResult AddLookup(int id)
        {
            var a = Company.GetById<LookupType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Lookup,
                FieldUri = string.Format("/form/Lookup_AddFields?id={0}", id),
                FormTitle = "Add item to " + a.Name,
                FormUri = "/form/AddLookup",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Lookup, a.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate<Lookup>(a, fields);

                return jsonSuccess(type.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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
        public ActionResult DeleteLookupByIdRaw(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteLookup(form);
        }

        public ActionResult DeleteLookup(int id)
        {
            var a = Company.GetById<Lookup>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Lookup,
                FieldUri = string.Format("/form/Lookup_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this item"),
                FormUri = "/form/DeleteLookup",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

   

        public ActionResult EditLookup(int id)
        {
            var a = Company.GetById<Lookup>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Lookup,
                FieldUri = string.Format("/form/Lookup_EditFields?id={0}", id),
                FormTitle = "Edit item",
                FormUri = "/form/EditLookup",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Lookup, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, model.LookupTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<Lookup>(model, fields);

                return jsonSuccess("Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public JsonResult LookupType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.LookupType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupTypeID</param>
        public JsonResult LookupType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LookupTypeID</param>
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

        [HttpPost, ValidateInput(false)]
        public JsonResult AddLookupTypeRaw(LookupTypeModel lookup)
        {
            var form = new FormCollection();
            form.Add("Name", lookup.Name);            

            return AddLookupType(form);
        }

        public ActionResult AddLookupType()
        {
            var model = new EditableForm
            {
                Context = ContextList.LookupType,
                FieldUri = "/form/LookupType_AddFields",
                FormTitle = "Add New Lookup",
                FormUri = "/form/AddLookupType",
                FormMethod = "POST",
                FormSize = "small"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                        Type = DataType.Text.ToString()
                    });
                }

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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
        [Route("lookuptype/{lookupTypeId:int}")]
        public ActionResult DeleteLookupTypeById(int lookupTypeId)
        {
            var form = new FormCollection();
            form.Add("ID", lookupTypeId.ToString());
            return DeleteLookupType(form);
        }

        [HttpGet]
        public ActionResult DeleteLookupType(int id)
        {
            var a = Company.GetById<LookupType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LookupType,
                FieldUri = string.Format("/form/LookupType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteLookupType",
                FormMethod = "DELETE",
                FormSize = "small"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteLookupType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("lookup type");

                var id = parseIntField(form, "ID");

                if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<LookupType>(i => i.ID == id);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditLookupTypeRaw(LookupTypeModel lookup)
        {
            var form = new FormCollection();
            form.Add("Name", lookup.Name);
            form.Add("ID", lookup.ID.ToString());

            return EditLookupType(form);
        }

        public ActionResult EditLookupType(int id)
        {
            var a = Company.GetById<LookupType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LookupType,
                FieldUri = string.Format("/form/LookupType_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditLookupType",
                FormMethod = "PUT",
                FormSize = "small"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
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

        #region Policy

        #region Field Generation

        public JsonResult Policy_AddFields(int typeID, int? parentID)
        {
            var model = new Policy();
            if (!Company.HasPermission(SystemObjects.Policy, 0, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "PolicyTypeID", FieldType = DataType.Hidden.ToString(), Value = typeID.ToString() });
            if (parentID.HasValue) list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = parentID.Value.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = $"form/Policy_SimilarItems?typeID={typeID}&query=" });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.PolicyType, typeID).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyID</param>
        public JsonResult Policy_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyID</param>
        public JsonResult Policy_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var model = Company.GetById<Policy>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = model.Description });

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Policy, id).ToList(), 5, true);

            return Json(list, JsonRequestBehavior.AllowGet);
        }


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

        public ActionResult AddPolicy(int typeID, int? parentID)
        {
            var type = Company.GetById<PolicyType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Policy,
                FieldUri = "/form/Policy_AddFields?typeID=" + typeID + ((parentID.HasValue) ? "&parentID=" + parentID.Value : ""),
                FormTitle = string.Format(Resources.FormInfo.Add_Policy_Title, type.Name),
                FormDescription = string.Format(Resources.FormInfo.Add_Policy_Directions, type.Name),
                FormUri = "/form/AddPolicy",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddPolicy(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Policy, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("Policy");

                var model = new Policy
                {
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description"),
                    PolicyTypeID = parseIntField(form, "PolicyTypeID")
                };

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    model.ParentID = parseIntField(form, "ParentID");
                    if (model.ParentID == 0) model.ParentID = null;
                }
                Company.Add<Policy>(model);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Policy, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), form, Server);
                Company.AddOrUpdateFields(fields);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeletePolicy(int id)
        {
            var a = Company.GetById<Policy>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Policy,
                FieldUri = string.Format("/form/Policy_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeletePolicy",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditPolicy(int id)
        {
            if (!Company.Exists<Policy>(id)) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Policy,
                FieldUri = string.Format("/form/Policy_EditFields?id={0}", id),
                FormTitle = Resources.FormInfo.Edit_Policy_Title,
                FormDescription = Resources.FormInfo.Edit_Policy_Directions,
                FormUri = "/form/EditPolicy",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");

                Company.Update<Policy>(model);

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Policy, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.PolicyType, model.PolicyTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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

        public ActionResult AddPolicyType()
        {
            var model = new EditableForm
            {
                Context = ContextList.PolicyType,
                FieldUri = "/form/PolicyType_AddFields",
                FormTitle = "Add Policy Type",
                FormUri = "/form/AddPolicyType",
                FormMethod = "POST",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                Company.Add<PolicyType>(a);

                for (int i = 1; i <= a.MaximumDepth; i++)
                {
                    Company.Set<PolicyTypeLevel>().Add(new PolicyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), PolicyTypeID = a.ID });
                }
                Company.SaveChanges();

                upsertObjectStyle(SystemObjects.PolicyType, a.ID, form, a.Name);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeletePolicyType(int id)
        {
            var a = Company.GetById<PolicyType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.PolicyType,
                FieldUri = "/form/PolicyType_DeleteFields?id=" + id,
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormDescription = Resources.FormInfo.PolicyType_Remove,
                FormUri = "/form/DeletePolicyType",
                FormMethod = "DELETE",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditPolicyType(int id)
        {
            var a = Company.GetById<PolicyType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.PolicyType,
                FieldUri = "/form/PolicyType_EditFields?id=" + id,
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditPolicyType",
                FormMethod = "PUT",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public ActionResult AddPolicyTypeClass()
        {
            var model = new EditableForm
            {
                Context = ContextList.PolicyTypeClass,
                FieldUri = "/form/PolicyTypeClass_AddFields",
                FormTitle = "Add Policy Class",
                FormUri = "/form/AddPolicyTypeClass",
                FormMethod = "POST",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("OverlayEditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddPolicyTypeClass(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.PolicyTypeClass, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("policy class");

                var a = new PolicyTypeClass
                {
                    Name = parseTextField(form, "Name", null, true)
                };

                Company.Add<PolicyTypeClass>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeletePolicyTypeClass(int id)
        {
            var a = Company.GetById<PolicyTypeClass>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.PolicyTypeClass,
                FieldUri = "/form/PolicyTypeClass_DeleteFields?id=" + id,
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormDescription = Resources.FormInfo.PolicyType_Remove,
                FormUri = "/form/DeletePolicyTypeClass",
                FormMethod = "DELETE",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditPolicyTypeClass(int id)
        {
            var a = Company.GetById<PolicyTypeClass>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.PolicyTypeClass,
                FieldUri = "/form/PolicyTypeClass_EditFields?id=" + id,
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditPolicyTypeClass",
                FormMethod = "PUT",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("OverlayEditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public ActionResult AddPolicyTypeLevel(int id)
        {
            var type = Company.GetById<PolicyType>(id);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.PolicyTypeLevel,
                FieldUri = string.Format("/form/PolicyTypeLevel_AddFields?id={0}", id),
                FormTitle = string.Format("Add {0} Level", type.Name),
                FormUri = "/form/AddPolicyTypeLevel",
                FormMethod = "POST"
            };
            type = null;

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<PolicyTypeLevel>(a);

                return jsonSuccess(a.Name + " successfully created.", a.PolicyTypeID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeletePolicyTypeLevel(int id, int level)
        {
            var a = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == id && i.Level == level).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.PolicyTypeLevel,
                FieldUri = string.Format("/form/PolicyTypeLevel_DeleteFields?id={0}&level={1}", id, level),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormDescription = Resources.FormInfo.PolicyTypeLevel_Remove,
                FormUri = "/form/DeletePolicyTypeLevel",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeletePolicyTypeLevel(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("policy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasPermission(SystemObjects.PolicyType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<PolicyTypeLevel>(i => i.PolicyTypeID == id && i.Level == level);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditPolicyTypeLevel(int id, int level)
        {
            var a = Company.Filter<PolicyTypeLevel>(i => i.PolicyTypeID == id && i.Level == level).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.PolicyTypeLevel,
                FieldUri = string.Format("/form/PolicyTypeLevel_EditFields?id={0}&level={1}", id, level),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditPolicyTypeLevel",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");

                Company.Update<PolicyTypeLevel>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public JsonResult Predicate_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.Predicate, 0, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Predicate Type", FieldType = DataType.Lookup.ToString(), Items = PredicateType.Lineage.GetAsList().Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name }).ToList() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PredicateID</param>
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
        public JsonResult Predicate_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Predicate>(id);
            var any = Company.Any<IntersectMap>(i => i.PredicateID == id);
            if (!Company.HasPermission(SystemObjects.Predicate, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Value = a.Inverse, Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            if (!any)
            {
                list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Predicate Type", FieldType = DataType.Lookup.ToString(), Value = ((int)a.Type).ToString(), Items = PredicateType.Lineage.GetAsList().Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name }).ToList() });
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddPredicate()
        {
            var model = new EditableForm
            {
                Context = ContextList.Predicate,
                FieldUri = "/form/Predicate_AddFields",
                FormTitle = "Add predicate",
                FormUri = "/form/AddPredicate",
                FormMethod = "POST"
            };

            return PartialView("OVerlayEditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddPredicate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("predicate");

                if (!Company.HasPermission(SystemObjects.Predicate, 0, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var a = new Predicate
                {
                    Name = parseTextField(form, "Name", null, true),
                    Inverse = parseTextField(form, "Inverse", null, true),
                    Type = (PredicateType)Enum.Parse(typeof(PredicateType), form["Type"]),
                    IsSystem = false
                };

                Company.Add<Predicate>(a);

                return jsonSuccess(a.Name + " successfully created.", string.Format("Predicate|{0}", a.ID), form["_context"], "add", HttpStatusCode.Created, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeletePredicate(int id)
        {
            var a = Company.GetById<Predicate>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Predicate,
                FieldUri = string.Format("/form/Predicate_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeletePredicate",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
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
                return jsonSuccess("Item successfully removed.", null, form["_context"], "delete", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditPredicate(int id)
        {
            var a = Company.GetById<Predicate>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Predicate,
                FieldUri = string.Format("/form/Predicate_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditPredicate",
                FormMethod = "PUT"
            };

            return PartialView("OverlayEditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);
                model.Inverse = parseTextField(form, "Inverse", null, true);

                Company.Update<Predicate>(model);

                return jsonSuccess(model.Name + " successfully updated.", string.Format("IntersectRole|{0}", id), form["_context"], "edit", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
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
        public JsonResult Relationship_AddFields(int it, SystemObjects type, int id)
        {
            if (!Company.HasPermission(type, id, Claim.Create, ClaimObject.Relationship))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var relationshipType = Company.GetById<IntersectType>(it, i => i.Nodes);
            var obj = Company.GetObjectDetail(type, id);

            if (obj == null || relationshipType == null)
            {
                return jsonException("Invalid relationship type or source item.", HttpStatusCode.NotFound);
            }

            var targetType = "";
            var targetTypeID = 0;
            var firstNode = relationshipType.Nodes.First();
            var lastNode = relationshipType.Nodes.Last();
            if (firstNode.ObjectType == obj.Type && firstNode.ObjectID == obj.TypeID)
            {
                targetType = lastNode.ObjectType;
                targetTypeID = lastNode.ObjectID;
            }
            else
            {
                targetType = firstNode.ObjectType;
                targetTypeID = firstNode.ObjectID;
            }

            list.Add(new EditableField { FieldName = "IntersectTypeID", FieldType = DataType.Hidden.ToString(), Value = it.ToString() });
            list.Add(new EditableField { FieldName = "Source", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
            list.Add(new EditableField { FieldName = "SourceID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            #region

            var sql = "";

            switch (targetType)
            {
                case "FusionAttributeType":
                    sql = @"
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

with attr as	(
			select	A.ID,
					A.ParentID,
					A.FusionAttributeTypeID
			from	FusionAttributeOwnerRule R with(nolock)
					inner join FusionAttributeOwnerRuleItem RI with(nolock) on RI.FusionAttributeOwnerRuleID = R.ID and R.RelationshipOwnerObjectType = 'Artifact'
					inner join @h H on H.ID = R.RelationshipOwnerObjectID
					inner join FusionAttribute A with(nolock) on (
													(RI.FusionAttributeID is not null and A.ID = RI.FusionAttributeID) OR 
													(RI.FusionAttributeID is null and A.FusionAttributeTypeID = R.ObjectID)
													)
													AND A.FusionID = R.FusionID
                                                    AND A.Deleted = 0
			union all
			select	C.ID,
					C.ParentID,
					C.FusionAttributeTypeID
			from	FusionAttribute C with(nolock)
					inner join attr P on C.ParentID = P.ID and C.Deleted = 0
			)

select	'FusionAttribute' as [Object], 
        FA.ID as ObjectID, 
        F.Name + '.' + FA.TextPath as Name
from	FusionAttribute FA with(nolock)
		inner join Fusion F with(nolock) on F.ID = FA.FusionID and FA.FusionAttributeTypeID = @targetTypeID and FA.Deleted = 0
		inner join attr on attr.ID = FA.ID
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	(
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) ) --OR
							 --( (SubjectType = @targetType and SubjectTypeID = @targetTypeID) AND (Object = @source and ObjectID = @id) )
							)
					)
order by F.Name, FA.TextPath";
                    break;
                case "Group":
                case "GroupType":
                    sql = @"
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
					where	(
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'Group' and ObjectTypeID = 1) ) OR
							 ( (SubjectType = 'Group' and SubjectTypeID = 1) AND (Object = @source and ObjectID = @id) )
							)
					)
order by D.Name";
                    break;
                case "Resource":
                case "ResourceType":
                    sql = @"
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
					where	(
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'ResourceType' and ObjectTypeID = 1) ) OR
							 ( (SubjectType = 'ResourceType' and SubjectTypeID = 1) AND (Object = @source and ObjectID = @id) )
							)
					)
order by D.LastName, D.FirstName";
                    break;
                default:
                    sql = @"
select	D.[Object], 
        D.ObjectID, 
        D.TextPath as Name
from	cache.ObjectDetails D with(nolock)
		left join [IntersectDetail] I on	(
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

            list.Add(new EditableField { Row = 1, Column = 2, FieldName = "Classification", Name = "Critical?", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Is There Anything Else We Should Know?", FieldType = DataType.Html.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.IntersectType, it).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RelationshipID</param>
        public JsonResult Relationship_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Intersect, id, Claim.Delete, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RelationshipID</param>
        public JsonResult Relationship_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Intersect, id, Claim.Create, ClaimObject.Relationship))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var relationship = Company.GetById<Intersect>(id, i => i.IntersectType);
            var critical = (relationship.Classification.HasValue) ? (relationship.Classification.Value == IntersectClassification.Critical) : false;

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Classification", Name = "Critical?", FieldType = DataType.Boolean.ToString(), Value = critical.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Is There Anything Else We Should Know?", FieldType = DataType.Html.ToString(), Value = relationship.Description });
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.IntersectType, relationship.IntersectTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Intersect, relationship.ID).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        //[Route("relationships/{intersectTypeID:int}/{type}/{id:int}/add")]
        public ActionResult AddRelationship(int intersectTypeID, string type, int id)
        {
            var intersectType = Company.GetById<IntersectType>(intersectTypeID);
            if (intersectType == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Intersect,
                FieldUri = $"/form/Relationship_AddFields?it={intersectTypeID}&type={type}&id={id}",
                FormTitle = string.Format(Resources.FormInfo.Add_Generic_Title, "Relationships"),
                FormUri = "/form/AddRelationship",
                FormMethod = "POST"
            };

            return PartialView("AddRelationship", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddRelationship(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("relationship");

                var source = parseTextField(form, "Source");
                var sourceID = parseIntField(form, "SourceID");
                int typeID = parseIntField(form, "IntersectTypeID");
                var relationshipType = Company.GetById<IntersectType>(typeID, i => i.Nodes);
                var sourceObject = Company.GetObjectDetail(source, sourceID);

                if (!Company.HasPermission(SystemObjects.IntersectType, typeID, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (relationshipType == null) throw new NotFoundException("relationship");

                var rawItems = parseTextField(form, "Items");
                if (string.IsNullOrEmpty(rawItems))
                    return jsonException("No selected items", HttpStatusCode.BadRequest);

                var items = rawItems.Split(',').ToList();

                var sourceIntersectTypeNodeID = 0;
                var targetIntersectTypeNodeID = 0;
                var firstNode = relationshipType.Nodes.First();
                var lastNode = relationshipType.Nodes.Last();
                if (firstNode.ObjectType == sourceObject.Type && firstNode.ObjectID == sourceObject.TypeID)
                {
                    sourceIntersectTypeNodeID = firstNode.ID;
                    targetIntersectTypeNodeID = lastNode.ID;
                }
                else
                {
                    sourceIntersectTypeNodeID = lastNode.ID;
                    targetIntersectTypeNodeID = firstNode.ID;
                }

                items.ForEach(item =>
                {
                    var itemInfo = item.Split('|');
                    if (itemInfo.Length == 2)
                    {
                        var classification = parseBooleanField(form, "Classification");
                        var description = parseTextField(form, "Description");

                        var intersect = Company.Query<Intersect>(
                            @"AddSingleIntersect @ResourceID, @IntersectTypeID, @Subject, @SubjectID, @Object, @ObjectID, @Classification, @Description",
                            new {
                                ResourceID = Company.CurrentResourceID,
                                IntersectTypeID = typeID,
                                Subject = source,
                                SubjectID = sourceID,
                                Object = itemInfo[0],
                                ObjectID = int.Parse(itemInfo[1]),
                                Classification = (classification) ? IntersectClassification.Critical : IntersectClassification.Normal,
                                Description = description
                            }
                        ).SingleOrDefault();
                        var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Intersect, intersect.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.IntersectType, typeID).ToList(), form, Server);
                        Company.AddOrUpdateFields(fields);
                    }
                });

                return jsonSuccess(relationshipType.Name + " successfully created.", "0", form["_context"], "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Intersect.ToString(), ObjectID = 0 });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        ////[Route("relationships/{id:int}/delete")]
        //public ActionResult DeleteRelationship(int id)
        //{
        //    var a = Company.GetById<Intersect>(id);
        //    if (a == null) return HttpNotFound();
        //    var model = new EditableForm
        //    {
        //        Context = ContextList.Intersect,
        //        FieldUri = string.Format("/form/Intersect_DeleteFields?id={0}", id),
        //        FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "Relationship"),
        //        FormUri = "/form/DeleteRelationship",
        //        FormMethod = "DELETE"
        //    };

        //    return PartialView("DeleteForm", model);
        //}

        //[HttpDelete]
        //public JsonResult DeleteRelationship(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("relationship");

        //        var id = parseIntField(form, "ID");
        //        var model = Company.GetById<Intersect>(id);
        //        if (model == null) throw new NotFoundException("relationship");

        //        //if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Delete, ClaimObject.Root))
        //        //    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
        //        Company.DeleteRelationship(id);

        //        return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, new { ObjectType = SystemObjects.Intersect.ToString(), ObjectID = id });
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


        //[Route("relationships/{id:int}/edit")]
        public ActionResult EditRelationship(int id)
        {
            var a = Company.GetById<Intersect>(id, i => i.IntersectType);
            if (a == null) return HttpNotFound();

            var model = new EditableForm
            {
                Context = ContextList.Intersect,
                FormDescription = "Please provide as much detail as possible in the form below.  You may select one or more relationships by clicking/highlighting the items in the list below.",
                FieldUri = string.Format("/form/Relationship_EditFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Edit_Generic_Title, "Relationship"),
                FormUri = "/form/EditRelationship",
                FormMethod = "PUT"
            };

            return PartialView("EditRelationship", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditRelationship(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("relationship");

                int id = parseIntField(form, "ID");
                var intersect = Company.GetById<Intersect>(id, i => i.Nodes);

                if (intersect == null) throw new NotFoundException("relationship");

                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), intersect.Nodes.First().ObjectType), intersect.Nodes.First().ObjectID, Claim.Update, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                if (!Company.HasPermission((SystemObjects)Enum.Parse(typeof(SystemObjects), intersect.Nodes.Last().ObjectType), intersect.Nodes.Last().ObjectID, Claim.Update, ClaimObject.Relationship))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var classification = parseBooleanField(form, "Classification");
                var description = parseTextField(form, "Description");

                intersect.Classification = classification ? IntersectClassification.Critical : IntersectClassification.Normal;
                intersect.Description = description;

                Company.Update<Intersect>(intersect);
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Intersect, intersect.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.IntersectType, intersect.IntersectTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                return jsonSuccess("Relationship successfully updated.", intersect.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Intersect.ToString(), ObjectID = intersect.ID });
            }
            catch (BaseException ex)
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

        #region Report

        #region Field Generation

        /// <param name="id">ID of the object</param>
        public JsonResult Report_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Report, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<Report>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

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

        void loadReportEditorModel(ReportEditorModel model)
        {
            model.ObjectTypes = Company.Query<SelectListItem>(@"
select      *
from        (
            select      'Artifact|' + cast(ID as varchar(15)) as Value,
                        'Artifact Instance : ' + Name as Text
            from        ArtifactType
            union
            select      'ArtifactType|' + cast(ID as varchar(15)) as Value,
                        'Artifact Type : ' + Name as Text
            from        ArtifactType
            union
            select      'Domain|' + cast(ID as varchar(15)) as Value,
                        'Domain Instance : ' + Name as Text
            from        DomainType
            union
            select      'DomainType|' + cast(ID as varchar(15)) as Value,
                        'Domain Type : ' + Name as Text
            from        DomainType
            union
            select      'Resource|1' as Value,
                        'Resource' as Text
            union
            select      'Taxonomy|' + cast(ID as varchar(15)) as Value,
                        'Model Instance : ' + Name as Text
            from        TaxonomyType
            union
            select      'TaxonomyType|' + cast(ID as varchar(15)) as Value,
                        'Model Type : ' + Name as Text
            from        TaxonomyType
            union
            select      'Policy|' + cast(ID as varchar(15)) as Value,
                        'Policy Instance : ' + Name as Text
            from        PolicyType
            union
            select      'PolicyType|' + cast(ID as varchar(15)) as Value,
                        'Policy Type : ' + Name as Text
            from        PolicyType
) O
order by    Text

").ToList();

            model.ObjectTypes.AddRange(RuleType.Informational.GetRuleTypeEnumList().Select(i => new SelectListItem { Text = string.Format("Rule Instance : {0}", i.Name), Value = string.Format("Rule|{0}", (int)i.ID) }));
            //model.ObjectTypes.AddRange(RuleType.Informational.GetRuleTypeEnumList().Select(i => new SelectListItem { Text = string.Format("Rule Type : {0}", i.Name), Value = string.Format("RuleType|{0}", (int)i.ID) }));

            model.ReportLayouts = Company.Query<SelectListItem>(@"
select      cast(ID as varchar(15)) as Value,
            Name as Text
from        ReportLayout
order by    Name
").ToList();
        }

        public ActionResult AddReport()
        {

            var o = new ReportEditorModel
            {
                FormUri = "/Form/AddReport",
                FormMethod = "POST",
                FormName = Resources.FormInfo.Add_Report_Title,
                FormDirections = Resources.FormInfo.Add_Report_Directions,
                Report = new Report { },
                ReportTypes = new List<SelectListItem> { new SelectListItem { Text = "Default", Value = "legacy", Selected = true }, new SelectListItem { Text = "PowerBI", Value = "powerbi" } }
            };
            loadReportEditorModel(o);
            return PartialView("ReportEditForm", o);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                    var name = parseTextField(form, "Name", null, true);
                    string powerBIID = string.Empty;
                    string datasetID = string.Empty;

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
                        }
                    }

                    var model = new Report
                    {
                        Name = parseTextField(form, "Name", null, true),
                        Description = parseTextField(form, "Description"),
                        ObjectType = objectType[0],
                        ObjectID = int.Parse(objectType[1]),
                        ReportLayoutID = parseNullableIntField(form, "ReportLayoutID", -1).GetValueOrDefault(-1),
                        ReportType = parseTextField(form, "ReportType"),
                        PowerBIReportID = string.IsNullOrEmpty(powerBIID) ? null : powerBIID,
                        PowerBIDatasetID = string.IsNullOrEmpty(datasetID) ? null : datasetID
                    };

                    Company.Add<Report>(model);

                    return jsonSuccess(Resources.FormInfo.Add_FieldType_Confirmation, model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
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
                
        public ActionResult DeleteReport(int id)
        {
            var a = Company.GetById<Report>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Report,
                FieldUri = string.Format("/form/Report_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteReport",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess(Resources.FormInfo.Delete_FieldType_Confirmation, id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult AddPowerBICredentials()
        {
            var model = new EditableForm
            {
                Context = ContextList.PowerBICredentialsSet,
                FieldUri = "/form/PowerBICredentials_AddFields",
                FormTitle = Resources.FormInfo.Add_PowerBI_Credentials_Title,
                FormDescription = Resources.FormInfo.Add_PowerBI_Credentials_Directions,
                FormUri = "/form/AddPowerBICredentials",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);

        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                return jsonSuccess(Resources.FormInfo.Add_FieldType_Confirmation, "", form["_context"], "add", HttpStatusCode.Created);
                
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditReport(int id)
        {
            var o = Company.GetById<Report>(id);
            if (o == null) return HttpNotFound();
            var model = new ReportEditorModel
            {
                FormUri = "/Form/EditReport",
                FormMethod = "PUT",
                FormName = Resources.FormInfo.Edit_Report_Title,
                FormDirections = Resources.FormInfo.Edit_Report_Directions,
                Report = o,
                ReportTypes = new List<SelectListItem> { new SelectListItem { Text = "Default", Value = "legacy", Selected = (o.ReportType != "powerbi") }, new SelectListItem { Text = "PowerBI", Value = "powerbi", Selected = ( o.ReportType == "powerbi") } }
            };
            loadReportEditorModel(model);

            var selectedObjectType = model.ObjectTypes.SingleOrDefault(i => i.Value == string.Format("{0}|{1}", model.Report.ObjectType, model.Report.ObjectID));
            if (selectedObjectType != null)
                selectedObjectType.Selected = true;

            var selectedReportLayout = model.ReportLayouts.SingleOrDefault(i => i.Value == model.Report.ReportLayoutID.ToString());
            if (selectedReportLayout != null)
                selectedReportLayout.Selected = true;

            return PartialView("ReportEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
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
                var name = parseTextField(form, "Name", null, true);
                string powerBIID = string.Empty;
                string datasetID = string.Empty;

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

                    if (!string.IsNullOrEmpty(datasetID))
                        model.PowerBIDatasetID = datasetID;

                    if (!string.IsNullOrEmpty(powerBIID))
                        model.PowerBIReportID = powerBIID;

                    Company.Update<Report>(model);

                    return jsonSuccess(Resources.FormInfo.Edit_FieldType_Confirmation, id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
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

        private List<SelectListItem> getReportTilePreviewObjects(string objectType, int objectID)
        {
            var list = new List<SelectListItem>();

            switch (objectType)
            {
                case "Artifact":
                    list = Company.Filter<Artifact>(i => i.ArtifactTypeID == objectID)
                        .OrderBy(i => i.Name)
                        .ToList()
                        .Select(i => new SelectListItem { Text = i.Name, Value = string.Format("Artifact|{0}", i.ID) })
                        .ToList();
                    break;
                case "Resource":
                    list = Company.Table<GlobalReportingResource>()
                        .OrderBy(i => i.LastName).ThenBy(i => i.FirstName)
                        .ToList()
                        .Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = string.Format("Resource|{0}", i.ResourceID) })
                        .ToList();
                    break;
                case "Taxonomy":
                    list = Company.Filter<Taxonomy>(i => i.TaxonomyTypeID == objectID)
                        .OrderBy(i => i.TextPath)
                        .ToList()
                        .Select(i => new SelectListItem { Text = i.TextPath, Value = string.Format("Taxonomy|{0}", i.ID) })
                        .ToList();
                    break;
            }

            return list;
        }

        public ActionResult AddReportTile(int reportID)
        {
            var report = Company.GetById<Report>(reportID, i => i.ReportLayout, i => i.ReportTiles);
            if (report == null) return HttpNotFound();

            var o = new ReportTileEditorModel
            {
                FormUri = "/Form/AddReportTile",
                FormMethod = "POST",
                FormName = "Add Tile to Report",//Resources.FormInfo.Add_Report_Title,
                FormDirections = Resources.FormInfo.Add_Report_Directions,
                ReportBaseUri = SecProvider.CompanyPrefix,
                ReportTile = new ReportTile { Report = report, ReportID = reportID },
                ReportTileTypes = ReportTileType.Area.GetReportTileTypeEnumList().OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList(),
                ContentAreaNumbers = new List<SelectListItem>(),
                //SchemaItems = Company.GetReportingSchema(),
                ObjectTypes = getReportTilePreviewObjects(report.ObjectType, report.ObjectID)
            };

            var existingTiles = report.ReportTiles.ToList();
            for (var i = 1; i <= report.ReportLayout.NumberOfContentAreas; i++)
            {
                o.ContentAreaNumbers.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString(), Disabled = existingTiles.Any(t => t.ContentAreaNumber == i) });
            }
            existingTiles = null;

            return PartialView("ReportTileEditForm", o);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddReportTile(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var model = new ReportTile
                {
                    Name = parseTextField(form, "Name", null, true),
                    CommandText = parseTextField(form, "SqlStatement"),
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

                return jsonSuccess(Resources.FormInfo.Add_FieldType_Confirmation, model.ID.ToString(), ContextList.ReportTile, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteReportTile(int id)
        {
            var a = Company.GetById<ReportTile>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ReportTile,
                FieldUri = string.Format("/form/ReportTile_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteReportTile",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteReportTile(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ReportTile>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);
                Company.Delete<ReportTile>(model);

                return jsonSuccess(Resources.FormInfo.Delete_FieldType_Confirmation, id.ToString(), ContextList.ReportTile, "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditReportTile(int id)
        {
            var o = Company.GetById<ReportTile>(id, i => i.Report, i => i.Report.ReportLayout, i => i.Report.ReportTiles);
            if (o == null) return HttpNotFound();
            var model = new ReportTileEditorModel
            {
                FormUri = "/Form/EditReportTile",
                FormMethod = "PUT",
                FormName = string.Format("Edit {0}", o.Name),
                FormDirections = Resources.FormInfo.Edit_Report_Directions,
                ReportBaseUri = SecProvider.CompanyPrefix,
                ReportTile = o,
                ReportTileTypes = ReportTileType.Area.GetReportTileTypeEnumList().OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList(),
                ContentAreaNumbers = new List<SelectListItem>(),
                SchemaItems = Company.GetReportingSchema(),
                ObjectTypes = getReportTilePreviewObjects(o.Report.ObjectType, o.Report.ObjectID)
            };

            var selectedTileType = model.ReportTileTypes.SingleOrDefault(i => i.Value == model.ReportTile.ReportTileType.ToString());
            if (selectedTileType != null)
                selectedTileType.Selected = true;

            var existingTiles = o.Report.ReportTiles.ToList();
            for (var i = 1; i <= o.Report.ReportLayout.NumberOfContentAreas; i++)
            {
                model.ContentAreaNumbers.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString(), Selected = (o.ContentAreaNumber == i), Disabled = (existingTiles.Any(t => t.ContentAreaNumber == i) && o.ContentAreaNumber != i) });
            }
            existingTiles = null;

            return PartialView("ReportTileEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditReportTile(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "TileID");
                var model = Company.GetById<ReportTile>(id);

                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                // Static fields
                model.Name = parseTextField(form, "Name", null, true);
                model.CommandText = parseTextField(form, "SqlStatement");
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

                return jsonSuccess(Resources.FormInfo.Edit_FieldType_Confirmation, id.ToString(), ContextList.ReportTile, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
                        contexts.Add(new ResponsibilityContextItem { ObjectID = id, ObjectType = "DomainItem", ResponsibilityID = responsibilityID });
                    });
                }
            }

            return contexts;
        }

        List<ResponsibilityContextItem> getContextFieldForResponsibility(int responsibilityID, List<ResponsibilityContextItem> contexts)
        {
            var ctx = new List<ResponsibilityContextItem>();
            var IDs = contexts.Select(c => c.ObjectID).ToList();

            IDs.ForEach(id =>
            {
                ctx.Add(new ResponsibilityContextItem { ObjectID = id, ObjectType = "DomainItem", ResponsibilityID = responsibilityID });
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

            var sql = @"select	D.Name + ' : ' + I.Name as [Text], I.ID as Value, I.ID  
from	DomainItem I
		inner join Domain D on D.ID = I.DomainID
		inner join DomainType T on T.ID = D.DomainTypeID
order by	D.Name, I.Name";

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

        public ActionResult AddResponsibility(SystemObjects type, int id)
        {
            var model = new PeopleResponsibilityEditorModel
            {
                FormName = string.Format("Add Responsibility"),
                FormUri = "/form/AddResponsibility",
                FormMethod = "POST",
                Contexts = getContextSelectList(),
                FormDescription = "",
                Resources = getResponsibilityResources(),
                ResponsibilityTypes = getResponsibilityTypeSelectList(type, id, ResponsibilityTypeGroup.People),
                Responsibility = new Responsibility { ObjectType = type.ToString(), ObjectID = id, Visible = true }
            };

            return PartialView("ResponsibilityEditForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost]
        public JsonResult AddResponsibility(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("responsibility");

                var objectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), form["ObjectType"]);
                var responsibleParty = form["ResponsibleObject"].Split('|');
                var o = new Responsibility
                {
                    ResponsibilityTypeID = parseIntField(form, "ResponsibilityType"),
                    ObjectType = objectType.ToString(),
                    ObjectID = parseIntField(form, "ObjectID"),
                    ResponsibleObjectType = responsibleParty[0],
                    ResponsibleObjectID = int.Parse(responsibleParty[1]),
                    Visible = parseBooleanField(form, "IsVisible", true)
                };

                #region Existence check

                var existing = Company.Filter<Responsibility>(i => i.ResponsibilityTypeID == o.ResponsibilityTypeID && i.ObjectType == o.ObjectType && i.ObjectID == o.ObjectID, i => i.ResponsibilityContextItems).FirstOrDefault();
                if (existing != null)
                {
                    var newContexts = getContextFormFieldForResponsibility(0, form);
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

                Company.Add<Responsibility>(o);

                processContextFormFieldForResponsibility(o.ID, form);

                Company.Update<Responsibility>(o);  //Call this again so we can re-cache via trigger.

                return jsonSuccess("Item successfully created.", o.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, new { ObjectType = o.ObjectType.ToString(), ObjectID = o.ObjectID.ToString() });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteResponsibility(int id)
        {
            var responsibility = Company.GetById<Responsibility>(id, i => i.ResponsibilityType);

            var model = new EditableForm
            {
                Context = ContextList.PeopleResponsibility,
                FieldUri = string.Format("/form/Responsibility_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this owner"),
                FormUri = "/form/DeleteResponsibility",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteResponsibility(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("responsibility");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Responsibility>(id);
                if (model == null) throw new NotFoundException("responsibility");

                Company.Delete<Responsibility>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
            }
            catch (BaseException ex)
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
        public JsonResult DeleteResponsibilityByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteResponsibility(form);
        }

        public ActionResult EditResponsibility(int id)
        {
            var r = Company.GetById<Responsibility>(id, i => i.ResponsibilityType, i => i.ResponsibilityContextItems);
            if (r == null) return HttpNotFound();

            var model = new PeopleResponsibilityEditorModel
            {
                FormName = "Edit Responsibility",
                FormUri = "/form/EditResponsibility",
                FormMethod = "PUT",
                Contexts = getContextSelectList(r.ResponsibilityContextItems.Select(i => i.ObjectID).ToList()),
                FormDescription = "",
                Resources = getResponsibilityResources(string.Format("{0}|{1}", r.ResponsibleObjectType, r.ResponsibleObjectID)),
                Responsibility = r,
                ResponsibilityTypes = getResponsibilityTypeSelectList((SystemObjects)Enum.Parse(typeof(SystemObjects), r.ObjectType), r.ObjectID, ResponsibilityTypeGroup.People, r.ResponsibilityTypeID)
            };

            return PartialView("ResponsibilityEditForm", model);
        }

        [HttpGet]
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

        [HttpPut]
        public JsonResult EditResponsibility(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("responsibility");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Responsibility>(id);
                if (model == null) throw new NotFoundException("responsibility");
                var responsibleParty = form["ResponsibleObject"].Split('|');

                model.ResponsibleObjectType = responsibleParty[0];
                model.ResponsibleObjectID = int.Parse(responsibleParty[1]);
                model.ResponsibilityTypeID = parseIntField(form, "ResponsibilityType");
                model.Visible = parseBooleanField(form, "IsVisible", true);

                #region Existence check

                var existing = Company.Filter<Responsibility>(i => i.ResponsibilityTypeID == model.ResponsibilityTypeID && i.ObjectType == model.ObjectType && i.ObjectID == model.ObjectID && i.ID != model.ID, i => i.ResponsibilityContextItems).FirstOrDefault();
                if (existing != null)
                {
                    var newContexts = getContextFormFieldForResponsibility(0, form);
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

                processContextFormFieldForResponsibility(id, form, false);
                Company.Update<Responsibility>(model);  //Do this after context so the trigger will properly re-cache with the contextxs.

                return jsonSuccess("Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
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

                    #endregion

                    Company.Add(model);
                    processContextFieldForResponsibility(model.ID, r.ResponsibilityContextItems.ToList());
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

            return jsonSuccess("Item successfully updated.", model.ID.ToString(), null, "edit", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
        }


        #endregion

        #endregion

        #region ResponsibilityType

        #region Field Generation

        public JsonResult ResponsibilityType_AddFields(ResponsibilityTypeGroup Group)
        {
            var list = new List<EditableField>();
            var o = new ResponsibilityType();

            list.Add(new EditableField { FieldName = "ResponsibilityTypeGroup", FieldType = DataType.Hidden.ToString(), Value = ((int)Group).ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = Resources.FieldInfo.Name_Name, FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "AllocationType", Name = Resources.FieldInfo.ResponsibilityAllocatedTo_Name, FieldType = DataType.Lookup.ToString(), Required = true, MultiSelect = true, Items = Company.GetAvailableAllocationPossibilities().Select(i => new SelectListItem { Text = i.Name, Value = string.Format("{0}|{1}", i.ObjectType, i.ObjectTypeID) }).ToList() });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResponsibilityTypeID</param>
        public JsonResult ResponsibilityType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ResponsibilityType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResponsibilityTypeID</param>
        public JsonResult ResponsibilityType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ResponsibilityType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name });
            var selectedAllocations = Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id).ToList();
            var allocations = Company
                .GetAvailableAllocationPossibilities()
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

        public ActionResult AddResponsibilityType(ResponsibilityTypeGroup Group)
        {
            var pModel = new EditableForm
            {
                Context = ContextList.ResponsibilityType,
                FieldUri = string.Format("/form/ResponsibilityType_AddFields?Group={0}", ((int)Group).ToString()),
                FormSize = "small",
                FormTitle = string.Format("Add {0} Type", Group.ToString()),
                FormUri = "/form/AddResponsibilityType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", pModel);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddResponsibilityType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ownership type");

                if (string.IsNullOrEmpty(form["AllocationType"]))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "Allocations missing", "You have not allocated this responsibility type.");
                }

                var a = new ResponsibilityType
                {
                    Name = parseTextField(form, "Name", null, true),
                    ResponsibilityTypeGroup = (ResponsibilityTypeGroup)Enum.Parse(typeof(ResponsibilityTypeGroup), form["ResponsibilityTypeGroup"]),
                    Description = parseTextField(form, "Description")
                };

                Company.Add<ResponsibilityType>(a);

                var items = form["AllocationType"].Split(',')
                    .Select(i => i.Split('|'))
                    .Select(i => new ObjectModel
                    {
                        ObjectType = i[0],
                        ObjectID = int.Parse(i[1])
                    }).ToList();

                foreach (var o in items)
                {
                    var r = new ResponsibilityTypeRelation { ObjectID = o.ObjectID, ObjectType = o.ObjectType, ResponsibilityTypeID = a.ID };
                    Company.Set<ResponsibilityTypeRelation>().Add(r);
                }
                Company.SaveChanges();

                return jsonSuccess("Item successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteResponsibilityType(int id)
        {
            var a = Company.GetById<ResponsibilityType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResponsibilityType,
                FieldUri = string.Format("/form/ResponsibilityType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this type"),
                FormUri = "/form/DeleteResponsibilityType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteResponsibilityType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ownership type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ResponsibilityType>(id);
                if (model == null) throw new NotFoundException("ownership type");

                Company.Delete<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id);
                Company.Delete<ResponsibilityType>(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete, ValidateHttpAntiForgeryToken]
        public JsonResult DeleteResponsibilityTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteResponsibilityType(form);
        }

        public ActionResult EditResponsibilityType(int id)
        {
            var model = Company.GetById<ResponsibilityType>(id);
            if (model == null) return HttpNotFound();

            var pModel = new EditableForm
            {
                Context = ContextList.ResponsibilityType,
                FieldUri = string.Format("/form/ResponsibilityType_EditFields?id={0}", id),
                FormSize = "small",
                FormTitle = "Edit Type",
                FormUri = "/form/EditResponsibilityType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", pModel);
        }

        [HttpGet, ActionName("ResponsibilityType")]
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
                .GetAvailableAllocationPossibilities()
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

        [HttpPut, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("ResponsibilityType")]
        public JsonResult PutResponsibilityType(ResponsibilityType model)
        {
            try
            {
                //if (!form.HasKeys()) throw new NoFormDataException("ownership type");

                // var id = parseIntField(form, "ID");
                //var model = Company.GetById<ResponsibilityType>(id);
                var existing = Company.GetById<ResponsibilityType>(model.ID);
                if (existing == null) throw new NotFoundException("ownership type");

                existing.Name = model.Name;
                existing.Description = model.Description;
                

                

                //if (string.IsNullOrEmpty(form["AllocationType"]))
                //{
                //    throw new GenericException(HttpStatusCode.BadRequest, "Allocations missing", "You have not allocated this responsibility type.");
                //}

                //model.Name = parseTextField(form, "Name", null, true);
                //model.Description = parseTextField(form, "Description");

                Company.Update(existing);

                Company.Delete<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == model.ID);

                //var items = form["AllocationType"].Split(',')
                //    .Select(i => i.Split('|'))
                //    .Select(i => new ObjectModel
                //    {
                //        ObjectType = i[0],
                //        ObjectID = int.Parse(i[1])
                //    }).ToList();

                foreach(var r in model.ResponsibilityTypeRelations)
                {
                    Company.Set<ResponsibilityTypeRelation>().Add(r);
                }

                //foreach (var o in items)
                //{
                //    var r = new ResponsibilityTypeRelation { ObjectID = o.ObjectID, ObjectType = o.ObjectType, ResponsibilityTypeID = id };
                //    Company.Set<ResponsibilityTypeRelation>().Add(r);
                //}
                Company.SaveChanges();

                return jsonSuccess("Item successfully updated.", model.ID.ToString(), null, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost, ValidateInput(false), ValidateHttpAntiForgeryToken, ActionName("ResponsibilityType")]
        public JsonResult PostResponsibilityType(ResponsibilityType model)
        {
            try
            {
                // if (!form.HasKeys()) throw new NoFormDataException("ownership type");

                //if (string.IsNullOrEmpty(form["AllocationType"]))
                //{
                //    throw new GenericException(HttpStatusCode.BadRequest, "Allocations missing", "You have not allocated this responsibility type.");
                //}
                var a = model;
                //var a = new ResponsibilityType
                //{
                //    Name = parseTextField(form, "Name", null, true),
                //    ResponsibilityTypeGroup = (ResponsibilityTypeGroup)Enum.Parse(typeof(ResponsibilityTypeGroup), form["ResponsibilityTypeGroup"]),
                //    Description = parseTextField(form, "Description")
                //};

                Company.Add(a);

                //var items = form["AllocationType"].Split(',')
                //    .Select(i => i.Split('|'))
                //    .Select(i => new ObjectModel
                //    {
                //        ObjectType = i[0],
                //        ObjectID = int.Parse(i[1])
                //    }).ToList();

                foreach(var r in model.ResponsibilityTypeRelations)
                {
                    Company.Set<ResponsibilityTypeRelation>().Add(r);
                }
                //foreach (var o in items)
                //{
                //    var r = new ResponsibilityTypeRelation { ObjectID = o.ObjectID, ObjectType = o.ObjectType, ResponsibilityTypeID = a.ID };
                  //  Company.Set<ResponsibilityTypeRelation>().Add(r);
                //}
                Company.SaveChanges();

                return jsonSuccess("Item successfully created.", a.ID.ToString(), null, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResponsibilityType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ownership type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ResponsibilityType>(id);
                if (model == null) throw new NotFoundException("ownership type");

                if (string.IsNullOrEmpty(form["AllocationType"]))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "Allocations missing", "You have not allocated this responsibility type.");
                }

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");

                Company.Update<ResponsibilityType>(model);

                Company.Delete<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == model.ID);

                var items = form["AllocationType"].Split(',')
                    .Select(i => i.Split('|'))
                    .Select(i => new ObjectModel
                    {
                        ObjectType = i[0],
                        ObjectID = int.Parse(i[1])
                    }).ToList();

                foreach (var o in items)
                {
                    var r = new ResponsibilityTypeRelation { ObjectID = o.ObjectID, ObjectType = o.ObjectType, ResponsibilityTypeID = id };
                    Company.Set<ResponsibilityTypeRelation>().Add(r);
                }
                Company.SaveChanges();

                return jsonSuccess("Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region ResponsibilityTypeClaim

        #region Field Generation

        ///// <param name="id">ResponsibilityTypeID</param>
        //public JsonResult ResponsibilityTypeClaim_AddFields(int id)
        //{
        //    var list = new List<EditableField>();
        //    var claims = Company.GetClaims().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();
        //    var claimObjects = Company.GetClaimObjects().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();
        //    list.Add(new EditableField { FieldName = "ResponsibilityTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
        //    list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Claim", Name = "Claim", FieldType = DataType.Lookup.ToString(), Items = claims });
        //    list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ClaimObject", Name = "Dependent Object", FieldType = DataType.Lookup.ToString(), Items = claimObjects });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        ///// <param name="id">ResponsibilityTypeClaimID</param>
        //public JsonResult ResponsibilityTypeClaim_DeleteFields(int id)
        //{
        //    var list = new List<EditableField>();
        //    var a = GovernanceService.GetResponsibilityTypeClaim(id);

        //    list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        ///// <param name="id">ResponsibilityTypeClaimID</param>
        //public JsonResult ResponsibilityTypeClaim_EditFields(int id)
        //{
        //    var list = new List<EditableField>();
        //    var a = GovernanceService.GetResponsibilityTypeClaim(id);
        //    var claims = Company.GetClaims().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();
        //    var claimObjects = Company.GetClaimObjects().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();
        //    list.Add(new EditableField { FieldName = "ResponsibilityTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

        //    list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
        //    list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Claim", Name = "Claim", FieldType = DataType.Lookup.ToString(), Items = claims, Value = a.Claim.ToString() });
        //    list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ClaimObject", Name = "Dependent Object", FieldType = DataType.Lookup.ToString(), Items = claimObjects, Value = a.ClaimObject.ToString() });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        #endregion

        #region Form Get/Post

        //public ActionResult AddResponsibilityTypeClaim(int id)
        //{
        //    var model = new EditableForm
        //    {
        //        Context = ContextList.ResponsibilityTypeClaim,
        //        FieldUri = "/form/ResponsibilityTypeClaim_AddFields?id=" + id,
        //        FormTitle = "Add Claim",
        //        FormUri = "/form/AddResponsibilityTypeClaim",
        //        FormMethod = "POST"
        //    };

        //    return PartialView("EditableForm", model);
        //}

        //[HttpPost]
        //public JsonResult AddResponsibilityTypeClaim(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("responsibility type claim");

        //        var a = new ResponsibilityTypeClaim
        //        {
        //            Claim = (Claim)Enum.Parse(typeof(Claim), form["Claim"]),
        //            ResponsibilityTypeID = parseIntField(form, "ResponsibilityTypeID")
        //        };

        //        if (!string.IsNullOrEmpty(form["ClaimObject"])) a.ClaimObject = (ClaimObject)Enum.Parse(typeof(ClaimObject), form["ClaimObject"]);

        //        GovernanceService.AddResponsibilityTypeClaim(a);

        //        return jsonSuccess("Item successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        //public ActionResult DeleteResponsibilityTypeClaim(int id)
        //{
        //    var a = GovernanceService.GetResponsibilityTypeClaim(id);
        //    if (a == null) return HttpNotFound();
        //    var model = new EditableForm
        //    {
        //        Context = ContextList.ResponsibilityTypeClaim,
        //        FieldUri = string.Format("/form/ResponsibilityTypeClaim_DeleteFields?id={0}", id),
        //        FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this claim"),
        //        FormUri = "/form/DeleteResponsibilityTypeClaim",
        //        FormMethod = "DELETE"
        //    };

        //    return PartialView("DeleteForm", model);
        //}

        //[HttpDelete]
        //public JsonResult DeleteResponsibilityTypeClaim(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("responsibility type claim");

        //        var id = parseIntField(form, "ID");
        //        var model = GovernanceService.GetResponsibilityTypeClaim(id);
        //        if (model == null) throw new NotFoundException("responsibility type claim");

        //        GovernanceService.DeleteResponsibilityTypeClaim(model);
        //        return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        //public ActionResult EditResponsibilityTypeClaim(int id)
        //{
        //    var a = GovernanceService.GetResponsibilityTypeClaim(id);
        //    if (a == null) return HttpNotFound();
        //    var model = new EditableForm
        //    {
        //        Context = ContextList.ResponsibilityTypeClaim,
        //        FieldUri = string.Format("/form/ResponsibilityTypeClaim_EditFields?id={0}", id),
        //        FormTitle = "Edit Type",
        //        FormUri = "/form/EditResponsibilityTypeClaim",
        //        FormMethod = "PUT"
        //    };

        //    return PartialView("EditableForm", model);
        //}

        //[HttpPut]
        //public JsonResult EditResponsibilityTypeClaim(FormCollection form)
        //{
        //    try
        //    {
        //        if (!form.HasKeys()) throw new NoFormDataException("responsibility type claim");

        //        var id = parseIntField(form, "ID");
        //        var model = GovernanceService.GetResponsibilityTypeClaim(id);
        //        if (model == null) throw new NotFoundException("responsibility type claim");

        //        model.Claim = (Claim)Enum.Parse(typeof(Claim), form["Claim"]);
        //        model.ClaimObject = (ClaimObject)Enum.Parse(typeof(ClaimObject), form["ClaimObject"]);

        //        GovernanceService.EditResponsibilityTypeClaim(model);

        //        return jsonSuccess("Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        #endregion

        #endregion

        #region ResponsibilityTypeObjectClaim

        #region Field Generation

        ///// <param name="r">ResponsibilityTypeID</param>
        ///// <param name="type">Object Type</param>
        ///// <param name="id">Object ID</param>
        //public JsonResult ResponsibilityTypeObjectClaim_AddFields(SystemObjects type, int id)
        //{
        //    var list = new List<EditableField>();

        //    var claims = Company.GetClaims().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();
        //    var claimobjects = Company.GetClaimObjects().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();
        //    var types = Company.Filter<ResponsibilityType>(i => i.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

        //    list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
        //    list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
        //    list.Add(new EditableField { Row = 1, Column = 1, FieldName = "ResponsibilityTypeID", Name = "Role", FieldType = DataType.Lookup.ToString(), Items = types });
        //    list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Claim", Name = "Claim", FieldType = DataType.Lookup.ToString(), Items = claims });
        //    list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "ClaimObject", Name = "Claim Object", FieldType = DataType.Lookup.ToString(), Items = claimobjects });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        ///// <param name="id">ResponsibilityTypeObjectClaimID</param>
        //public JsonResult ResponsibilityTypeObjectClaim_DeleteFields(int id)
        //{
        //    var list = new List<EditableField>();
        //    var a = GovernanceService.GetResponsibilityTypeObjectClaim(id);

        //    list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        ///// <param name="id">ResponsibilityTypeObjectClaimID</param>
        //public JsonResult ResponsibilityTypeObjectClaim_EditFields(int id)
        //{
        //    var list = new List<EditableField>();
        //    var a = GovernanceService.GetResponsibilityTypeObjectClaim(id);

        //    var claims = Company.GetClaims().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();
        //    var claimobjects = Company.GetClaimObjects().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList();

        //    list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
        //    list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Claim", Name = "Claim", FieldType = DataType.Lookup.ToString(), Items = claims, Value = a.Claim.ToString() });
        //    list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ClaimObject", Name = "Claim Object", FieldType = DataType.Lookup.ToString(), Items = claimobjects, Value = a.ClaimObject.ToString() });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        #endregion

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

        public ActionResult AddResponsibilityTypeClaims(SystemObjects type, int id)
        {
            var model = new ClaimsMatrixEditorModel
            {
                Items = new List<ClaimsMatrixEditorItemModel>(),
                ObjectID = id,
                ObjectType = type.ToString()
            };
            return PartialView(model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost]
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

                return jsonSuccess("Item successfully created.", "0", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditResponsibilityTypeClaims(SystemObjects type, int id, int responsibilityTypeID)
        {
            var sType = type.ToString();
            var model = new ClaimsMatrixEditorModel
            {
                Items = Company.Filter<ResponsibilityTypeObjectClaim>(i => i.ObjectID == id && i.ObjectType == sType && i.ResponsibilityTypeID == responsibilityTypeID)
                .Select(i => new ClaimsMatrixEditorItemModel { Claim = i.Claim, ClaimObject = i.ClaimObject, ID = i.ID })
                .ToList(),
                ObjectID = id,
                ObjectType = type.ToString(),
                ResponsibilityTypeID = responsibilityTypeID
            };
            return PartialView(model);
        }

        [HttpPut]
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

                return jsonSuccess("Item successfully created.", "0", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut]
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

                return jsonSuccess("Item successfully created.", "0", null, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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

        #region Resolution

        #region Field Generation

        /// <param name="t">ObjectType</param>
        /// <param name="id">ObjectID</param>
        public JsonResult Resolution_AddFields(SystemObjects t, int id)
        {
            var list = new List<EditableField>();
            IQueryable<Resolution> model = null;
            switch (t)
            {
                case SystemObjects.Rule:
                    model = Company.Filter<Resolution>(i => i.RuleID == id);
                    break;
            }

            list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });
            list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            var items = model.ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
            if (items.Count > 0)
            {
                items.Insert(0, new SelectListItem { Text = "", Value = "" });
                list.Add(new EditableField
                {
                    Row = 1,
                    Column = 1,
                    FieldName = "ExistingResolution",
                    Name = "Resolve With Existing Resolution",
                    FieldType = DataType.Lookup.ToString(),
                    Items = items
                });
            }
            else
            {
                list.Add(new EditableField
                {
                    Row = 1,
                    Column = 1,
                    FieldName = "ExistingResolution",
                    FieldType = DataType.Hidden.ToString()
                });
            }
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Body", Name = "Body", FieldType = DataType.Html.ToString(), Validations = checkAndAddValidation("Text", "Body", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResolutionID</param>
        public JsonResult Resolution_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Resolution>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResolutionID</param>
        public JsonResult Resolution_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Resolution>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Body", Name = "Body", FieldType = DataType.Html.ToString(), Value = a.Body, Validations = checkAndAddValidation("Text", "Body", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddResolution(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resolution");

                Resolution a = null;
                SystemObjects type;
                int id;

                try
                {
                    type = (SystemObjects)Enum.Parse(typeof(SystemObjects), form["ObjectType"]);
                    id = parseIntField(form, "ObjectID");
                }
                catch
                {
                    throw new NoFormDataException("target object");
                }

                if (form["ExistingResolution"] != "")
                {
                    a = Company.GetById<Resolution>(parseIntField(form, "ExistingResolution"));
                }
                else
                {
                    a = new Resolution();

                    // Static fields
                    a.Name = parseTextField(form, "Name", null, true);
                    a.Body = parseTextField(form, "Body");
                    a.RuleID = 0;

                    Company.Add<Resolution>(a);
                }

                var relation = new ResolutionRelation { ResolutionID = a.ID, ObjectType = type.ToString(), ObjectID = id };
                Company.Add<ResolutionRelation>(relation);

                return jsonSuccess("Resolution successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteResolution(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resolution");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Resolution>(id);
                if (model == null) throw new NotFoundException("resolution");

                Company.Delete<Resolution>(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResolution(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resolution");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Resolution>(id);

                if (model == null) throw new NotFoundException("resolution");

                // Static fields
                model.Name = parseTextField(form, "Name", null, true);
                model.Body = parseTextField(form, "Body");

                Company.Update<Resolution>(model);

                return jsonSuccess("Resolution successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        string passwordRegex = @"(?=^.{7,25}$)((?=.*\d)(?=.*[A-Z])(?=.*[a-z])|(?=.*\d)(?=.*[^A-Za-z0-9])(?=.*[a-z])|(?=.*[^A-Za-z0-9])(?=.*[A-Z])(?=.*[a-z])|(?=.*\d)(?=.*[A-Z])(?=.*[^A-Za-z0-9]))^.*";
        string passwordRegexMessage = "be between 7 and 25 characters in length; at least 1 uppercase character; at least 1 lowercase chacter; at least 1 number; at least 1 special character";

        #region Field Generation

        /// <param name="id">ResourceTypeID</param>
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

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, id).ToList(), 5);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceID</param>
        public JsonResult Resource_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceID</param>
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

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(), 4);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Resource_EditMyInfoFields()
        {
            var list = new List<EditableField>();
            var id = Company.CurrentResourceID;
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

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

        [Route("resources/{typeID:int}/add")]
        public ActionResult AddResource(int typeID)
        {
            var type = Community.GetById<ResourceType>(typeID);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourceform",
                FormSize = "small",
                FieldUri = string.Format("/form/Resource_AddFields?id={0}", typeID),
                FormTitle = string.Format(Resources.FormInfo.Add_Generic_Title, "Resource"),
                FormUri = "/form/AddResource",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                        FirstName = parseTextField(form, "FirstName"),
                        LastName = parseTextField(form, "LastName"),
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
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, a.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, typeID).ToList(), form, Server);
                Company.AddOrUpdateFields(fields);

                return jsonSuccess(type.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("resources/{typeID:int}/{id:int}/delete")]
        public ActionResult DeleteResource(int typeID, int id)
        {
            var a = Community.GetById<Resource>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourceform",
                FormSize = "small",
                FieldUri = string.Format("/form/Resource_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.FormatDisplayName()),
                FormUri = "/form/DeleteResource",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteResourceByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteResource(form);
        }

        [Route("resources/{typeID:int}/{id:int}/edit")]
        public ActionResult EditResource(int typeID, int id)
        {
            var a = Community.GetById<Resource>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourceform",
                FormSize = "small",
                FieldUri = string.Format("/form/Resource_EditFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Edit_Generic_Title, "Resource"),
                FormUri = "/form/EditResource",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResource(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null) throw new NotFoundException("resource");

                // Static fields
                model.FirstName = parseTextField(form, "FirstName");
                model.LastName = parseTextField(form, "LastName");
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
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                if (Request.ContentLength > 0)
                {
                    //SecurityService.EditResourceImage(model.ID, Request.InputStream);
                }

                return jsonSuccess("Resource successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("resources/me/edit")]
        public ActionResult EditMyInfo()
        {
            var a = Community.GetById<Resource>(Company.CurrentResourceID);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourceform",
                FormSize = "small",
                FieldUri = "/form/Resource_EditMyInfoFields",
                FormTitle = "Edit Your Bio",
                FormUri = "/form/EditMyInfo",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditMyInfo(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var model = Community.GetById<Resource>(Company.CurrentResourceID);

                if (model == null) throw new NotFoundException("resource");

                // Static fields
                model.FirstName = parseTextField(form, "FirstName");
                model.LastName = parseTextField(form, "LastName");

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                Community.Update<Resource>(model);

                return jsonSuccess("Info successfully updated.", Company.CurrentResourceID.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("resources/me/changepassword")]
        public ActionResult ChangeMyPassword()
        {
            var a = Community.GetById<Resource>(Company.CurrentResourceID);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourceform",
                FormSize = "small",
                FieldUri = "/form/Resource_ChangeMyPasswordFields",
                FormTitle = "Change Your Password",
                FormUri = "/form/ChangeMyPassword",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess("Password successfully updated.", Company.CurrentResourceID.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("resources/{typeID:int}/{id:int}/password")]
        public ActionResult ChangeUserPassword(int typeID, int id)
        {
            var a = Community.GetById<Resource>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourceform",
                FormSize = "small",
                FieldUri = string.Format("/form/Resource_ChangeUserPasswordFields?id={0}", id),
                FormTitle = string.Format("Change Password for {0}", a.FormatDisplayName()),
                FormUri = "/form/ChangeUserPassword",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess("Password successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region ResourceType

        #region Field Generation

        public JsonResult ResourceType_AddFields()
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceTypeID</param>
        public JsonResult ResourceType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Community.GetById<ResourceType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceTypeID</param>
        public JsonResult ResourceType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Community.GetById<ResourceType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("resources/add")]
        public ActionResult AddResourceType()
        {
            var model = new EditableForm
            {
                Context = "resourcetypeform",
                FieldUri = "/form/ResourceType_AddFields",
                FormTitle = "Add Type",
                FormUri = "/form/AddResourceType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddResourceType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource type");

                var a = new ResourceType
                {
                    Name = parseTextField(form, "Name", null, true)
                };

                Community.Add<ResourceType>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("resources/{typeID:int}/delete")]
        public ActionResult DeleteResourceType(int typeID)
        {
            var a = Community.GetById<ResourceType>(typeID);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourcetypeform",
                FieldUri = string.Format("/form/ResourceType_DeleteFields?id={0}", typeID),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteResourceType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteResourceType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource type");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<ResourceType>(id);
                if (model == null) throw new NotFoundException("resource type");

                Community.Delete<ResourceType>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("resources/{typeID:int}/edit")]
        public ActionResult EditResourceType(int typeID)
        {
            var a = Community.GetById<ResourceType>(typeID);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = "resourcetypeform",
                FieldUri = string.Format("/form/ResourceType_EditFields?id={0}", typeID),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditResourceType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResourceType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource type");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<ResourceType>(id);
                if (model == null) throw new NotFoundException("resource type");

                model.Name = parseTextField(form, "Name", null, true);

                Community.Update<ResourceType>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult QuestionType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddQuestionType(int surveyTypeID)
        {
            ViewBag.ID = 0;
            ViewBag.SurveyTypeID = surveyTypeID;
            return PartialView("QuestionTypeEditForm");
        }

        [ValidateHttpAntiForgeryToken, HttpPost, ValidateInput(false)]
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

                return jsonSuccess("Survey question successfully created.", qt.ID.ToString(), ContextList.QuestionType.ToString(), "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [HttpGet]
        public ActionResult DeleteQuestionType(int id)
        {
            var a = Company.GetById<QuestionType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.QuestionType,
                FieldUri = string.Format("/form/QuestionType_DeleteFields?id={0}", a.ID),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteQuestionType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteQuestionType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("response type");

                var id = parseIntField(form, "ID");
                Company.Delete<QuestionType>(i => i.ID == id);

                return jsonSuccess("Survey question successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [HttpGet]
        public ActionResult EditQuestionType(int id)
        {
            var a = Company.GetById<QuestionType>(id);
            if (a == null) return HttpNotFound();
            ViewBag.ID = id;
            ViewBag.SurveyTypeID = a.SurveyTypeID;
            return PartialView("QuestionTypeEditForm");
        }

        [ValidateHttpAntiForgeryToken, HttpPut, ValidateInput(false)]
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

                return jsonSuccess("Survey question successfully updated.", qt.ID.ToString(), ContextList.QuestionType.ToString(), "update", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public JsonResult Rule_AddFields()
        {
            var model = new Rule();

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250), SimilarItemsUri = "form/Rule_SimilarItems?query=" });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "RuleType", Name = model.GetName(i => i.RuleType), FieldDescription = model.GetDescription(i => i.RuleType), Items = RuleType.Informational.GetRuleTypeEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList(), FieldType = DataType.Lookup.ToString() });

            var dimensions = Company.RuleDimensions.Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            dimensions.Insert(0, new SelectListItem { Text = "Choose...", Value = "" });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "RuleDimensionID", Name = model.GetName(i => i.RuleDimensionID), FieldDescription = model.GetDescription(i => i.RuleDimensionID), Items = dimensions, FieldType = DataType.Lookup.ToString() });

            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        public JsonResult Rule_DeleteFields(int id)
        {
            var model = Company.GetById<Rule>(id);

            if (!Company.HasPermission(SystemObjects.RuleType, (int)model.RuleType, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        public JsonResult Rule_EditFields(int id)
        {
            var list = new List<EditableField>();
            var model = Company.GetById<Rule>(id);

            if ((!Company.HasPermission(SystemObjects.Rule, id, Claim.Update)) && (!Company.HasPermission(SystemObjects.RuleType, (int)model.RuleType, Claim.Update)))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var anyEvents = Company.Any<Event>(i => i.EventGroup.RuleID == id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, ReadOnly = anyEvents, FieldName = "RuleType", Name = model.GetName(i => i.RuleType), FieldDescription = model.GetDescription(i => i.RuleType), Items = RuleType.Informational.GetRuleTypeEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList(), FieldType = DataType.Lookup.ToString(), Value = ((int)model.RuleType).ToString() });

            var dimensions = Company.RuleDimensions.Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            dimensions.Insert(0, new SelectListItem { Text = "Choose...", Value = "" });

            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "RuleDimensionID", Name = model.GetName(i => i.RuleDimensionID), FieldDescription = model.GetDescription(i => i.RuleDimensionID), Items = dimensions, FieldType = DataType.Lookup.ToString(), Value = model.RuleDimensionID.GetValueOrDefault(-1).ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = model.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

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

        public ActionResult AddRule()
        {
            var model = new EditableForm
            {
                Context = ContextList.Rule,
                FieldUri = "/form/Rule_AddFields",
                FormTitle = Resources.FormInfo.Add_Rule_Title,
                FormDescription = Resources.FormInfo.Add_Rule_Directions,
                FormUri = "/form/AddRule",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddRule(FormCollection form)
        {
            try
            {
                var ruleType = (RuleType)Enum.Parse(typeof(RuleType), form["RuleType"]);

                if (!Company.HasPermission(SystemObjects.RuleType, (int)ruleType, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var model = new Rule
                {
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description"),
                    RuleType = ruleType,
                    RuleDimensionID = parseNullableIntField(form, "RuleDimensionID")
                };

                Company.Add<Rule>(model);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteRule(int id)
        {
            var a = Company.GetById<Rule>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Rule,
                FieldUri = string.Format("/form/Rule_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteRule",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Rule>(id);
                if (model == null) throw new NotFoundException("Rule");

                if (!Company.HasPermission(SystemObjects.RuleType, (int)model.RuleType, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "delete",
                    Context = form["_context"]
                };

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditRule(int id)
        {
            if (!Company.Exists<Rule>(id)) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Rule,
                FieldUri = string.Format("/form/Rule_EditFields?id={0}", id),
                FormTitle = Resources.FormInfo.Edit_Rule_Title,
                FormDescription = Resources.FormInfo.Edit_Rule_Directions,
                FormUri = "/form/EditRule",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Rule>(id);
                if (model == null) throw new NotFoundException("Rule");

                if ((!Company.HasPermission(SystemObjects.Rule, id, Claim.Update)) && (!Company.HasPermission(SystemObjects.RuleType, (int)model.RuleType, Claim.Update)))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                model.RuleType = (RuleType)Enum.Parse(typeof(RuleType), form["RuleType"]);
                model.RuleDimensionID = parseNullableIntField(form, "RuleDimensionID");

                Company.Update<Rule>(model);

                dynamic custom = new
                {
                    Name = model.Name,
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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
        public JsonResult RuleDimension_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.RuleType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
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

        public ActionResult AddRuleDimension()
        {
            var model = new EditableForm
            {
                Context = ContextList.RuleDimension,
                FieldUri = "/form/RuleDimension_AddFields",
                FormTitle = Resources.FormInfo.Add_Rule_Dimension_Title,
                FormDescription = Resources.FormInfo.Add_Rule_Dimension_Directions,
                FormUri = "/form/AddRuleDimension",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddRuleDimension(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.RuleType, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("RuleDimension");

                var model = new RuleDimension
                {
                    Name = parseTextField(form, "Name", null, true),
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

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteRuleDimension(int id)
        {
            var a = Company.GetById<RuleDimension>(id);
            if (a == null) return HttpNotFound();

            var model = new EditableForm
            {
                Context = ContextList.RuleDimension,
                FieldUri = string.Format("/form/RuleDimension_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteRuleDimension",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditRuleDimension(int id)
        {
            if (!Company.Exists<RuleDimension>(id)) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.RuleDimension,
                FieldUri = string.Format("/form/RuleDimension_EditFields?id={0}", id),
                FormTitle = Resources.FormInfo.Edit_Rule_Dimension_Title,
                FormDescription = Resources.FormInfo.Edit_Rule_Dimension_Directions,
                FormUri = "/form/EditRuleDimension",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);
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

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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

        #region SourceRule

        [HttpGet, Route("SourceRules/{id:int}")]
        public JsonNetResult GetSourceRule(int id)
        {
            var sr = Company.GetById<SourceRule>(id, i => i.Contexts, i => i.Items);
            //var srItems = Company.Filter<IntersectMapSourceRule>(i => i.SourceRuleID == id).ToList();


            var srItems = Company.Query<dynamic>(@"select r.ID, d.Name, r.Description, r.SortOrder, d.IconForeColor, d.IconBackColor from intersectmapsourcerule r
                                    join intersectmap m on m.id = r.intersectmapid
                                    join intersectnode n1 on n1.id = m.subjectintersectnodeid
                                    join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
                                    where r.SourceRuleID = @id", new { id = id }).ToList();
            //srItems.ForEach(i})

            srItems.ForEach(i =>
            {
                int myID = i.ID;
                i.Contexts = Company.Filter<IntersectMapSourceRuleContext>(j => j.IntersectMapSourceRuleID == myID).ToList();
            });

            return new JsonNetResult
            {
                Data = new
                {
                    sr.Name,
                    sr.Object,
                    sr.ObjectID,
                    Contexts = sr.Contexts.Select(i => new { i.Object, i.ObjectID }),
                    Items = srItems.Select(i => new { i.Name, i.SortOrder, i.Description, i.Contexts })
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("SourceRules/{target}/{targetId:int}/{type}/{id:int}")]
        public JsonNetResult GetSourceRules(string target, int targetId, string type, int id)
        {
            var items = Company.Filter<SourceRule>(s => s.Object == type && s.ObjectID == id && s.AppliesToObject.ToString() == target && s.AppliesToObjectID == targetId).ToList();

            items.ForEach(i =>
            {
                int myID = i.ID;
                var newItems = Company.Query<IntersectMapSourceRule>(@"select r.*,n.objecttype as Object, n.ObjectID, d.Name, d.IconForeColor, d.IconBackColor
                                                                from intersectmapsourcerule r
                                                                join intersectmap m on m.id = r.intersectmapid
                                                                join intersectnode n on n.id = m.subjectintersectnodeid 
                                                                join cache.objectdetails d on d.object = n.objecttype and d.objectid = n.objectid
                                                                where r.sourceruleid = @id"
                                                                , new { id = myID }).ToList();
                i.Contexts = new List<SourceRuleContext>();

                foreach (IntersectMapSourceRule r in i.Items)
                {
                    var newItem = newItems.Where(j => j.ID == r.ID).FirstOrDefault();
                    //var contexts = Company.IntersectMapSourceRuleContexts.Where(c => c.IntersectMapSourceRuleID == r.ID).ToList();
                    var contexts = Company.Query<IntersectMapSourceRuleContext>(@"select intersectmapsourceruleid, d.[object] + cast(d.objectid as varchar(10)) as ID, d.[Object], d.ObjectID, cast(1 as bit) as Checked, case when objecttype = 'ArtifactType' then 'Glossary' else 'Model' end as Category, ObjectTypeName as Type, Name from cache.ObjectDetails d
                                            join intersectmapsourcerulecontext r on r.object = d.object and r.objectid = d.objectid
                                            where r.intersectmapsourceruleid = @id", new { id = r.ID }).ToList();
                    r.Object = newItem.Object;
                    r.ObjectID = newItem.ObjectID;
                    r.Name = newItem.Name;
                    r.IconBackColor = newItem.IconBackColor;
                    r.IconForeColor = newItem.IconForeColor;
                    r.Contexts.Clear();
                    r.Contexts = contexts;
                    if (r.Contexts == null)
                        r.Contexts = new List<IntersectMapSourceRuleContext>();
                    // if (r.Contexts.Count > 0)
                    //     r.Contexts.ToList();
                }
            });

            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("SourceRules/sources/{target}/{targetId:int}/{type}/{id:int}")]
        public JsonNetResult GetAvailableSources(string target, int targetId, string type, int id)
        {
            #region Old Sql
            //string sql = @"	
            //                select *, row_number() over (order by [Object]) as SortOrder
            //                from
            //                (
            //                select	distinct
            //       M.ID as IntersectMapID,
            //       R.SourceTypeName as TypeName,
            //       R.SourceObjectName as Name,
            //                null as Description,
            //       R.SourceObject as [Object],
            //       R.SourceObjectID as ObjectID,
            //       SD.[IconBackColor],
            //       SD.[IconForeColor]
            //     from	IntersectMap M
            //       inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
            //       inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
            //       inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
            //       inner join Predicate P on P.ID = M.PredicateID
            //       inner join [cache].[Relationship] SR on SR.SourceObject = @target and SR.SourceObjectID = @targetId and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
            //       inner join [cache].[Relationship] TR on TR.SourceObject = @target and TR.SourceObjectID = @targetId and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
            //     where r.targetObject = @type and r.targetObjectID = @id) z
            //        where z.intersectmapid not in (select intersectmapid from intersectmapsourcerule r join sourcerule s on s.id = r.sourceruleid where s.object= @target and s.objectid = @targetId)";

            #endregion

            #region Sql
            string sql = @"select 
	row_number() over (order by SourceObject) as SortOrder,
	ID as IntersectMapID,
	SourceTypeName as TypeName,
	SourceObjectName as Name,
	null as [Description],
	SourceObject as [Object],
	SourceObjectID as ObjectID,
	SourceIconBackColor as IconBackColor,
	SourceIconForeColor as IconForeColor
 from (
	select	distinct
			R.IntersectID,
			M.ID,
			M.SubjectIntersectNodeID,
			R.SourceTypeName,
			R.SourceType,
			R.SourceTypeID,
			R.SourceObjectName,
			R.SourceObject,
			R.SourceObjectID,
			SD.[IconBackColor] as SourceIconBackColor,
			SD.[IconForeColor] as SourceIconForeColor,
			M.ObjectIntersectNodeID,
			R.TargetTypeName,
			R.TargetType,
			R.TargetTypeID,
			R.TargetObjectName,
			R.TargetObject,
			R.TargetObjectID,
			TD.[IconBackColor] as TargetIconBackColor,
			TD.[IconForeColor] as TargetIconForeColor,
			M.PredicateID,
			P.Name as Predicate
	from	IntersectMap M
			inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
			inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			inner join Predicate P on P.ID = M.PredicateID
			inner join [cache].[Relationship] SR on SR.SourceObject = @type and SR.SourceObjectID = @id and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
			inner join [cache].[Relationship] TR on TR.SourceObject = @type and TR.SourceObjectID = @id and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
	union
	select	distinct
			R.IntersectID,
			M.ID,
			M.SubjectIntersectNodeID,
			R.SourceTypeName,
			R.SourceType,
			R.SourceTypeID,
			R.SourceObjectName,
			R.SourceObject,
			R.SourceObjectID,
			SD.[IconBackColor] as SourceIconBackColor,
			SD.[IconForeColor] as SourceIconForeColor,
			M.ObjectIntersectNodeID,
			R.TargetTypeName,
			R.TargetType,
			R.TargetTypeID,
			R.TargetObjectName,
			R.TargetObject,
			R.TargetObjectID,
			TD.[IconBackColor] as TargetIconBackColor,
			TD.[IconForeColor] as TargetIconForeColor,
			M.PredicateID,
			P.Name as Predicate
	from	IntersectMap M
			inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @type and R.SourceObjectID = @id and M.[Type] = 1
			inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			inner join Predicate P on P.ID = M.PredicateID
	union
	select	distinct
			R.IntersectID,
			M.ID,
			M.SubjectIntersectNodeID,
			R.SourceTypeName,
			R.SourceType,
			R.SourceTypeID,
			R.SourceObjectName,
			R.SourceObject,
			R.SourceObjectID,
			SD.[IconBackColor] as SourceIconBackColor,
			SD.[IconForeColor] as SourceIconForeColor,
			M.ObjectIntersectNodeID,
			R.TargetTypeName,
			R.TargetType,
			R.TargetTypeID,
			R.TargetObjectName,
			R.TargetObject,
			R.TargetObjectID,
			TD.[IconBackColor] as TargetIconBackColor,
			TD.[IconForeColor] as TargetIconForeColor,
			M.PredicateID,
			P.Name as Predicate
	from	IntersectMap M
			inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @type and R.TargetObjectID = @id and M.[Type] = 1
			inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			inner join Predicate P on P.ID = M.PredicateID
			) z
			where z.targetobject = @target and z.targetobjectid = @targetId";
            #endregion

            var results = Company.Query<dynamic>(sql, new { target = target, targetId = targetId, type = type, id = id }).ToList();

            return new JsonNetResult
            {
                Data = results,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("SourceRules/contexts")]
        public JsonNetResult GetContexts()
        {
            var countAll = Company.Query<int>(@"select (select count(*) from artifact) + (select count(*) from taxonomy)").SingleOrDefault();

            var items = Company.Query<dynamic>(@"select * from
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
                Data = new { count = countAll, items = items },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("SourceRules/contexts/{phrase}")]
        public JsonNetResult GetContexts(string phrase)
        {
            //var countAll = Company.Query<int>(@"select count(*) from cache.ObjectDetails where objecttype in ('ArtifactType','TaxonomyType')").SingleOrDefault();
            phrase = '%' + phrase.Trim('%') + '%';
            var items = Company.Query<dynamic>(@"select * from
                                                (
                                                select a.Name, 'Artifact|' + cast(a.id as varchar(10)) as ID, 'Glossary' as Category, t.name as Type, cast(0 as bit) as Checked from artifact a
                                                join artifacttype t on t.id = a.artifacttypeid
                                                union all
                                                select x.Name, 'Taxonomy|' + cast(x.id as varchar(10)) as ID, 'Model' as Category, t.name as Type, cast(0 as bit) as Checked from taxonomy x
                                                join taxonomytype t on t.id = x.taxonomytypeid
                                                ) z
                                                where name like @phrase
                                                order by name", new { phrase = phrase }).ToList();
            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, Route("SourceRules/save"), ValidateInput(false)]
        public JsonNetResult SaveRule(SourceRule rule)
        {

            var message = "";
            var error = false;

            var items = rule.Items.ToList();
            rule.Items = null;
            if (rule.ID <= 0)
                rule.ID = 0;
            else
                rule = Company.GetById<SourceRule>(rule.ID);

            SystemObjects obj = SystemObjects.Artifact;

            try
            {
                obj = (SystemObjects)Enum.Parse(typeof(SystemObjects), rule.AppliesToObject);
            }
            catch
            {
                error = true;
                message += $"[{DateTime.Now}] An error occurred while trying to determine the focal object type.\n";
                return new JsonNetResult
                {
                    Data = new { error, message },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }

            bool canCreate = Company.HasPermission(obj, rule.AppliesToObjectID, Claim.Create);
            bool canUpdate = Company.HasPermission(obj, rule.AppliesToObjectID, Claim.Update);

            if (rule.ID == 0 && !canCreate)
            {
                error = true;
                message += $"[{DateTime.Now}] You do not have permission to create a source rule on this item.\n";
            }

            if (!error && (canUpdate || canCreate))
                foreach (var i in items)
                {
                    if (i.ID < 0)
                        i.ID = 0;
                    if (i.Contexts == null)
                        i.Contexts = new List<IntersectMapSourceRuleContext>();
                    if (i.Description == null)
                        i.Description = "";
                    var ctx = i.Contexts.ToList();
                    i.Contexts = null;
                    i.SourceRuleID = rule.ID;
                    i.SourceRule = rule;

                    try
                    {
                        Company.SaveOrUpdate(i);
                    }
                    catch (Exception ex)
                    {
                        error = true;
                        message += $"[{DateTime.Now}] An error occurred while attempting to save the intersect map source rule: {ex.Message}\n{ex.StackTrace}\n\n";
                        continue;
                    }

                    var contexts = Company.Filter<IntersectMapSourceRuleContext>(c => c.IntersectMapSourceRuleID == i.ID).ToList();
                    foreach (var c in ctx)
                    {
                        c.IntersectMapSourceRuleID = i.ID;
                        c.IntersectMapSourceRule = i;
                        if (contexts.Count(r => r.Object == c.Object && r.ObjectID == c.ObjectID) < 1)
                            Company.Set<IntersectMapSourceRuleContext>().Add(c);
                    }

                    foreach (var c in contexts)
                    {
                        if (ctx.Count(r => r.Object == c.Object && r.ObjectID == c.ObjectID) < 1)
                            Company.Set<IntersectMapSourceRuleContext>().Remove(c);
                    }
                    try
                    {
                        Company.SaveChanges();
                        message = rule.ID.ToString();
                    }
                    catch (Exception ex)
                    {
                        error = true;
                        message += $"[{DateTime.Now}] An error occurred while attempting to add or remove source rule contexts: {ex.Message}\n{ex.StackTrace}\n\n";
                    }

                }

            if (!error)
            {
                try
                {
                    Company.SaveOrUpdate(rule);
                }
                catch (Exception ex)
                {
                    error = true;
                    message += $"[{DateTime.Now}] An error occurred while attempting to save changes to the source rule: {ex.Message}\n{ex.StackTrace}\n\n";
                }
                //delete rule items which no longer exist
                var ids = Company.Query<int>("select id from intersectmapsourcerule where sourceruleid = @id", new { id = rule.ID });
                foreach (var i in ids.Where(j => !rule.Items.Select(k => k.ID).Contains(j)))
                {
                    //delete contexts first

                    Company.Filter<IntersectMapSourceRuleContext>(r => r.IntersectMapSourceRuleID == i).ToList().ForEach(j =>
                    {
                        Company.Set<IntersectMapSourceRuleContext>().Remove(j);
                    });
                    Company.Delete(Company.GetById<IntersectMapSourceRule>(i));
                }
            }

            return new JsonNetResult
            {
                Data = new { error, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }


        [ValidateHttpAntiForgeryToken]
        [HttpDelete, Route("SourceRules/delete"), ValidateInput(false)]
        public JsonNetResult DeleteSourceRule(int id)
        {
            var rule = Company.GetById<SourceRule>(id);
            var message = "";
            var error = false;

            if (rule == null)
            {
                message += "An error occurred, source rule id not found.";
                error = true;
                return new JsonNetResult
                {
                    Data = new { error, message },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            else
            {
                try
                {
                    var contexts = Company.Filter<SourceRuleContext>(c => c.SourceRuleID == rule.ID);
                    var intersectSourceRules = Company.Filter<IntersectMapSourceRule>(i => i.SourceRuleID == rule.ID).ToList();




                    var imContexts = new List<IntersectMapSourceRuleContext>();


                    foreach(var i in intersectSourceRules)
                    {
                        imContexts.AddRange(Company.Filter<IntersectMapSourceRuleContext>(c => c.IntersectMapSourceRuleID == i.ID));
                    }
                   

                    Company.SourceRuleContexts.RemoveRange(contexts);
                    Company.IntersectMapSourceRuleContexts.RemoveRange(imContexts);
                    Company.IntersectMapSourceRules.RemoveRange(intersectSourceRules);
                    Company.SaveChanges();
                    
                    Company.Delete(rule);
                    
                }
                catch (Exception ex)
                {
                    error = true;
                    message += $"An error occurred: {ex.Message}\n\n{ex.StackTrace}";
                }
                return new JsonNetResult
                {
                    Data = new { error, message },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
        }

        #endregion

        #region SourceToTarget

        [HttpGet, Route("sourcetarget/load/{focal}/{focalid}/{source}/{sourceid}/{target}/{targetid}")]
        public JsonNetResult LoadSourceTargetRules(string focal, int focalid, string source, int sourceid, string target, int targetid)
        {

            var items = new List<SourceTargetRule>();

            var sourceObj = Company.GetObjectDetail(source, sourceid);
            var targetObj = Company.GetObjectDetail(target, targetid);

            items = Company.Filter<SourceTargetRule>(r => 
                    r.FocalObject == focal && r.FocalObjectID == focalid && 
                    r.SourceObject == source && r.SourceObjectID == sourceid && 
                    r.TargetObject == target && r.TargetObjectID == targetid)
                .OrderBy(i => i.Sequence).ToList();

            var sql = @"select distinct r.id as RuleID, n.objectid as FusionID, a.textpath, 'source' as [type] from sourcetargetrule r
                        join intersectmapsourcetargetrule st on st.ruleid = r.id
                        join intersectmap m on m.type = 2 and m.id = st.intersectmapid
                        join intersectnode n on n.id = m.subjectintersectnodeid
                        join fusionattribute a on a.id = n.objectid
                        where r.focalobject = @focal and r.focalobjectid = @focalid and r.sourceobject = @source and r.sourceobjectid = @sourceid and r.targetobject = @target and r.targetobjectid = @targetid
                        union all
                        select distinct r.id as RuleID, n.objectid as FusionID, a.textpath, 'target' as [type] from sourcetargetrule r
                        join intersectmapsourcetargetrule st on st.ruleid = r.id
                        join intersectmap m on m.type = 2 and m.id = st.intersectmapid
                        join intersectnode n on n.id = m.objectintersectnodeid
                        join fusionattribute a on a.id = n.objectid
                        where r.focalobject = @focal and r.focalobjectid = @focalid and r.sourceobject = @source and r.sourceobjectid = @sourceid and r.targetobject = @target and r.targetobjectid = @targetid";


            var ruleItems = Company.Query<dynamic>(sql, new { focal = focal, focalid = focalid, source = source, sourceid = sourceid, target = target, targetid = targetid }).ToList();

            foreach (SourceTargetRule rule in items)
            {
                var sources = ruleItems.Where(r => r.RuleID == rule.ID && r.type == "source");
                var targets = ruleItems.Where(r => r.RuleID == rule.ID && r.type == "target");

                rule.Sources = new List<SourceTargetItem>();
                rule.Targets = new List<SourceTargetItem>();

                foreach (dynamic s in sources)
                {
                    var sourceItem = new SourceTargetItem();
                    sourceItem.FusionID = s.FusionID;
                    sourceItem.Name = s.textpath;
                    rule.Sources.Add(sourceItem);
                }
                foreach (dynamic t in targets)
                {
                    var targetItem = new SourceTargetItem();
                    targetItem.FusionID = t.FusionID;
                    targetItem.Name = t.textpath;
                    rule.Targets.Add(targetItem);
                }
            }

            int sourceCount = 0;
            int targetCount = 0;

            if (items.Count == 0)
            {
                var sql2 = @"select count(*) from 
                        fusion.attributeowner f
                        join fusionattributetype t on t.id = f.objectid
                        join fusionattribute a on a.fusionattributetypeid = f.objectid
                        where 
                        f.relationshipownerobjectid = @id and f.relationshipownerobjecttype = @type";

                sourceCount = Company.Query<int>(sql2, new { id = sourceid, type = source }).SingleOrDefault();
                if (sourceCount > 0)
                    targetCount = Company.Query<int>(sql2, new { id = targetid, type = target }).SingleOrDefault();
            }

            return new JsonNetResult
            {
                Data = new { items = items.ToList(), sourceCount, targetCount, sourceObj, targetObj },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("sourcetarget/fusion/")]
        public JsonNetResult GetRelatedFusionItems(string type, int id, string phrase, bool getDefault = false)
        {
            string sql = "";
            if (phrase == null)
                phrase = "";

            if (getDefault)
            {
                sql = @"select top 100 r.targetobjectid as id, r.targetname as name 
                        from	Relationship R
		                inner join Relationship S on R.SourceObjectType = 'Intersect' 
										                and S.IntersectID = R.SourceObjectID 
										                and S.SourceObjectType = @type 
										                and S.SourceObjectID = @id
						                where r.targetobjecttype = 'FusionAttribute'";
            } else
            {
                phrase = '%' + phrase.Trim() + '%';
                sql = @"select top 100 a.textpath as name, a.id from 
                        fusion.attributeowner f
                        join fusionattributetype t on t.id = f.objectid
                        join fusionattribute a on a.fusionattributetypeid = f.objectid
                        where 
                        f.relationshipownerobjectid = @id and f.relationshipownerobjecttype = @type
                        and a.textpath like @phrase";
            }




            var items = Company.Query<dynamic>(sql, new { type = type, id = id, phrase = phrase });

            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region StatisticType

        #region Field Generation

        /// <param name="id">StatisticTypeID</param>
        public JsonResult StatisticType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.StatisticType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<StatisticType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public JsonNetResult StatisticType_FormData(int id)
        {
            var type = Company.GetById<StatisticType>(id);
            if (type == null) return new JsonNetResult { Data = null };

            var model = new Dictionary<string, object>();
            model.Add("ID", type.ID);
            model.Add("Name", type.Name);
            model.Add("CheckType", type.CheckType);
            model.Add("Description", type.Description);
            model.Add("Object", type.Object);
            model.Add("ObjectID", type.ObjectID);
            model.Add("PartOfScore", type.PartOfScore);
            model.Add("Score", type.Score);

            var xml = XElement.Parse(type.Configuration);
            switch (type.CheckType)
            {
                case StatisticCheckType.Count:
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
                case StatisticCheckType.EventMetric:
                    model.Add("ValidField", xml.Element("ValidField").Value);
                    model.Add("InvalidField", xml.Element("InvalidField").Value);
                    model.Add("Threshold", xml.Element("Threshold").Value);
                    break;
                case StatisticCheckType.PredicateMetric:
                    model.Add("Predicate", xml.Element("Predicate").Value);
                    break;
                case StatisticCheckType.Relationship:
                    try
                    {
                        if (xml.Element("CheckObjects") != null)
                        {
                            model.Add("CheckObjects",
                                xml.Element("CheckObjects")
                                    .Elements("Object")
                                    .Select(co => $"{co.Element("Type").Value}|{co.Element("ID").Value}").ToList()
                                );
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

        public JsonNetResult StatisticType_CheckTypeOptions()
        {
            var models = StatisticCheckType.Count.GetEnumList().Select(i => new KnockoutListItem(i.Name, ((int)i.ID).ToString()));
            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult StatisticType_ObjectOptions()
        {
            var models = Company.GetTypes().Select(i => new KnockoutListItem(i.Name, $"{i.ObjectType}|{i.ObjectTypeID}"));
            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult StatisticType_CheckObjectOptions(SystemObjects type, int id, StatisticCheckType check)
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
                case StatisticCheckType.Count:
                    models.AddRange(Company.Query<KnockoutListItem>(@"
SELECT	'AttributeType|'+ cast(T.ID as varchar) as value, 
		'Attribute :' + T.Name as title
from	AttributeType T
		inner join AttributeTypeRelation R on R.AttributeTypeID = T.ID and T.ParentID is null and R.ObjectType = @type and R.ObjectID = @id
union 
SELECT	'ResponsibilityType|'+ cast(ID as varchar) as value, 
		'Responsibility :' + Name as title 
from	ResponsibilityType T
		inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeID = T.ID and R.ObjectType = @type and R.ObjectID = @id
union
select	distinct
		D.Object + '|' + cast(D.ObjectID as varchar) as value,
		'Relationship :' + D.TextPath as title
from	utility.RelationshipTypes RT
		inner join cache.ObjectDetails D on D.Object = RT.TargetObjectType and D.ObjectID = RT.TargetObjectID
where	RT.SourceObjectType = @type
		and RT.SourceObjectID = @id", new { type = type.ToString(), id }).OrderBy(i => i.title));
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
                        case SystemObjects.DomainType:
                            models.Add(new KnockoutListItem("Name", "Name"));
                            models.Add(new KnockoutListItem("Description", "Description"));
                            models.Add(new KnockoutListItem("Code", "Code"));
                            break;
                        case SystemObjects.TaxonomyType:
                        case SystemObjects.PolicyType:
                        case SystemObjects.RuleType:
                            models.Add(new KnockoutListItem("Name", "Name"));
                            models.Add(new KnockoutListItem("Description", "Description"));
                            break;
                    }
                    models.AddRange(Company.GetFieldTypeRelationsByObject(type, id).Select(i => new KnockoutListItem { title = i.FriendlyName, value = i.Name }));
                    break;
                case StatisticCheckType.Relationship:
                    models.AddRange(Company.Query<KnockoutListItem>(@"
select	distinct
		D.Object + '|' + cast(D.ObjectID as varchar) as value,
		D.TextPath as title
from	utility.RelationshipTypes RT
		inner join cache.ObjectDetails D on D.Object = RT.TargetObjectType and D.ObjectID = RT.TargetObjectID
where	RT.SourceObjectType = @type
		and RT.SourceObjectID = @id", new { type = type.ToString(), id }).OrderBy(i => i.title));
                    break;
                case StatisticCheckType.FusionOwnership:
                    //models.AddRange(Company.GetStatisticTypeCountCheckOptions().Select(i => new { title = i.Name, value = i.ID.ToString() }));
                    break;
                case StatisticCheckType.ScoreRollupViaRelationship:
                    models.AddRange(Company.Query<KnockoutListItem>(@"
select	distinct
		D.Object + '|' + cast(D.ObjectID as varchar) as value,
		D.TextPath as title
from	utility.RelationshipTypes RT
		inner join cache.ObjectDetails D on D.Object = RT.TargetObjectType and D.ObjectID = RT.TargetObjectID
where	RT.SourceObjectType = @type
		and RT.SourceObjectID = @id", new { type = type.ToString(), id }).OrderBy(i => i.title));
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

        public ActionResult AddStatisticType()
        {
            var model = new EditableForm
            {
                FormDescription = Resources.FormInfo.Add_AnalyticType_Directions,
                FormMethod = "POST",
                FormTitle = Resources.FormInfo.Add_AnalyticType_Title,
                FormUri = "/Form/AddStatisticType"
            };

            ViewBag.ID = 0;

            return PartialView("StatisticTypeEditForm", model);
        }

        string getXmlConfigurationFromFormFields(FormCollection form, StatisticCheckType checkType)
        {
            var fields = new XElement("fields");

            switch (checkType)
            {
                case StatisticCheckType.Count:
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
                    var rawCheckObjects = form["CheckObjects[]"];
                    if (!string.IsNullOrEmpty(rawCheckObjects))
                    {
                        var checkObjectStrings = rawCheckObjects.Split(',').ToList();
                        var checksElement = new XElement("CheckObjects");
                        checkObjectStrings.ForEach(i =>
                        {
                            var values = i.Split('|');
                            var checkElement = new XElement("Object");
                            checkElement.Add(
                                new XElement("Type", values[0]),
                                new XElement("ID", values[1])
                            );
                            checksElement.Add(checkElement);
                        });
                        fields.Add(checksElement);
                    }
                    break;
                case StatisticCheckType.EventMetric:
                    fields.Add(new XElement("ValidField", form["ValidField"]));
                    fields.Add(new XElement("InvalidField", form["InvalidField"]));
                    fields.Add(new XElement("Threshold", decimal.Parse(form["Threshold"])));
                    break;
                case StatisticCheckType.PredicateMetric:
                    fields.Add(new XElement("Predicate", form["Predicate"]));
                    break;
            }

            return fields.ToString();
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddStatisticType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.StatisticType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("statistic type");

                var a = new StatisticType
                {
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description"),
                    CheckType = (StatisticCheckType)Enum.Parse(typeof(StatisticCheckType), form["CheckType"]),
                    PartOfScore = parseBooleanField(form, "PartOfScore"),
                    Score = parseIntField(form, "Score"),
                    Object = parseTextField(form, "Object"),
                    ObjectID = parseIntField(form, "ObjectID")
                };
                a.Configuration = getXmlConfigurationFromFormFields(form, a.CheckType);

                //while (a.Score.ToString().StartsWith("0"))
                //{
                //    return jsonException(FormInfo., HttpStatusCode.Conflict);
                //}

                Company.Add<StatisticType>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), ContextList.StatisticType, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteStatisticType(int id)
        {
            var a = Company.GetById<StatisticType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.StatisticType,
                FieldUri = string.Format("/form/StatisticType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteStatisticType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteStatisticType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("statistic type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<StatisticType>(id);
                if (model == null) throw new NotFoundException("statistic type");

                if (!Company.HasPermission(SystemObjects.StatisticType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<StatisticType>(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), ContextList.StatisticType, "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditStatisticType(int id)
        {
            var a = Company.GetById<StatisticType>(id);
            if (a == null) return HttpNotFound();

            var model = new EditableForm {
                FormDescription = Resources.FormInfo.Add_AnalyticType_Directions,
                FormMethod = "PUT",
                FormTitle = string.Format(Resources.FormInfo.Edit_Generic_Title, a.Name),
                FormUri = "/Form/EditStatisticType"
            };

            ViewBag.ID = id;

            return PartialView("StatisticTypeEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditStatisticType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("statistic type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<StatisticType>(id);
                if (model == null) throw new NotFoundException("statistic type");

                if (!Company.HasPermission(SystemObjects.StatisticType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                model.PartOfScore = parseBooleanField(form, "PartOfScore");
                model.Score = parseIntField(form, "Score");
                model.CheckType = (StatisticCheckType)Enum.Parse(typeof(StatisticCheckType), form["CheckType"]);

                model.Configuration = getXmlConfigurationFromFormFields(form, model.CheckType);

                Company.Update<StatisticType>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), ContextList.StatisticType, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region SurveyType

        #region Field Generation

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
        public JsonResult SurveyType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
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

        //[Route("surveys/add")]
        public ActionResult AddSurveyType()
        {
            var model = new EditableForm
            {
                Context = ContextList.SurveyType,
                FieldUri = "/form/SurveyType_AddFields",
                FormTitle = "Add Type",
                FormUri = "/form/AddSurveyType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                    Name = parseTextField(form, "Name", null, true),
                    Object = ot.ToString(),
                    ObjectID = oid,
                    ValidForDays = parseNullableIntField(form, "ValidForDays", 1).GetValueOrDefault(1)
                };
                Company.Add<SurveyType>(model);

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        //[Route("surveys/{id:int}/delete")]
        public ActionResult DeleteSurveyType(int id)
        {
            var a = Company.GetById<SurveyType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.SurveyType,
                FieldUri = string.Format("/form/SurveyType_DeleteFields?id={0}", a.ID),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteSurveyType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteSurveyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("survey type");

                var id = parseIntField(form, "ID");
                Company.Delete<SurveyType>(i => i.ID == id);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        //[Route("surveys/{id:int}/edit")]
        public ActionResult EditSurveyType(int id)
        {
            var a = Company.GetById<SurveyType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.SurveyType,
                FieldUri = string.Format("/form/SurveyType_EditFields?id={0}", a.ID),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditSurveyType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditSurveyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("survey type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<SurveyType>(id);
                if (model == null) throw new NotFoundException("survey type");

                model.Name = parseTextField(form, "Name", null, true);
                model.ValidForDays = parseNullableIntField(form, "ValidForDays", 1).GetValueOrDefault(1);

                Company.Update<SurveyType>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        [HttpGet]
        public JsonResult SynonymsOptions(string type, int id)
        {
            var list = new List<EditableField>();
            var items = Company.Query<dynamic>(QueryConstants.SynonymOptions, new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString() }, id }).ToList();
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

        public JsonResult Synonym_AddFields(string type, int id)
        {
            //if (!Company.HasPermission(SystemObjects.TaxonomyType, t, Claim.Create))
            //    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var items = Company.Query<dynamic>(QueryConstants.SynonymOptions, new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString() }, id }).ToList();
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

        public ActionResult AddSynonym(SystemObjects type, int id)
        {
            var model = new EditableForm
            {
                Context = ContextList.Synonym,
                FieldUri = $"/form/Synonym_AddFields?type={type.ToString()}&id={id}",
                FormTitle = "Add Synonym",
                FormUri = "/form/AddSynonym",
                FormMethod = "POST",
                FormSize = "small"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken, HttpPost]
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
                        && i.Predicate.Type == PredicateType.Synonym
                    ).SingleOrDefault();
                    var intersect = Company.AddIntersect(intersectType.ID, subject, subjectID, @object, objectID);

                    if (intersect == null)
                        throw new ApplicationException("Failed to create synonym relationship.");

                    return jsonSuccess("Synonym assigned.", intersect.ID.ToString(), ContextList.Synonym, "add", HttpStatusCode.Created, new { });
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

        public ActionResult DeleteSynonym(int id)
        {
            var model = new EditableForm
            {
                Context = ContextList.Synonym,
                FieldUri = $"/form/Synonym_DeleteFields?id={id}",
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "Synonym"),
                FormUri = "/form/DeleteSynonym",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteSynonym(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("synonym");
                var id = parseIntField(form, "IntersectID");

                var detail = Company.GetById<Intersect>(id);

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

                return jsonSuccess("Synonym successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.TaxonomyType, t).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyID</param>
        public JsonResult Taxonomy_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Taxonomy, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyID</param>
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
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ParentID", Name = "Parent Model", FieldDescription = Resources.FormInfo.Taxonomy_ChangeParent_Warning, FieldType = DataType.Lookup.ToString(), Items = parents, Value = ((a.ParentID.HasValue) ? a.ParentID.Value.ToString() : "") });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.TaxonomyType, a.TaxonomyTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Taxonomy, id).ToList(), 3);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

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
                Data = Company.Query<dynamic>((id > 0) ? sql : QueryConstants.SimilarItems, new { type = "Taxonomy", typeID, id, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        #endregion

        #region Form Get/Post

        [Route("taxonomy/{typeID:int}/{parentID:int=0}/add")]
        public ActionResult AddTaxonomy(int typeID, int parentID)
        {
            var type = Company.GetById<TaxonomyType>(typeID);
            if (type == null) return HttpNotFound();

            var levelName = "";
            var levels = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == typeID).ToList();
            if (parentID > 0)
            {
                var parent = Company.GetById<Taxonomy>(parentID);
                if (parent == null) return HttpNotFound();
                levelName = (levels.Any(i => i.Level == parent.Level + 1)) ? levels.Single(i => i.Level == parent.Level + 1).Name : string.Format("{0} {1}", type.Name, "Model");
            }
            else
            {
                levelName = (levels.Any(i => i.Level == 1)) ? levels.Single(i => i.Level == 1).Name : string.Format("{0} {1}", type.Name, "Model");
            }
            levels = null;

            var model = new EditableForm
            {
                Context = ContextList.Taxonomy,
                FieldUri = string.Format("/form/Taxonomy_AddFields?t={0}&p={1}", typeID, parentID),
                FormTitle = string.Format("Add {0}", levelName),
                FormUri = "/form/AddTaxonomy",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                a.Name = parseTextField(form, "Name", null, true);
                a.Description = parseTextField(form, "Description");

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    a.ParentID = parseIntField(form, "ParentID");
                    if (a.ParentID == 0) a.ParentID = null;
                }

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Taxonomy, a.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.TaxonomyType, typeID).ToList(), form, Server);
                Company.SaveOrUpdate<Taxonomy>(a, fields);

                dynamic custom = new
                {
                    TaxonomyTypeID = typeID,
                    ParentID = a.ParentID,
                    Name = a.Name,
                    Context = form["_context"]
                };

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("taxonomy/{typeID:int}/{id:int}/delete")]
        public ActionResult DeleteTaxonomy(int typeID, int id)
        {
            var a = Company.GetById<Taxonomy>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Taxonomy,
                FieldUri = "/form/Taxonomy_DeleteFields?id=" + id,
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteTaxonomy",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("taxonomy/{typeID:int}/{id:int}/edit")]
        public ActionResult EditTaxonomy(int typeID, int id)
        {
            var a = Company.GetById<Taxonomy>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.Taxonomy,
                FieldUri = "/form/Taxonomy_EditFields?id=" + id,
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditTaxonomy",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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
                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");
                model.ParentID = parseIntField(form, "ParentID");
                if (model.ParentID == 0) model.ParentID = null;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Taxonomy, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.TaxonomyType, model.TaxonomyTypeID).ToList(), form, Server, false);
                Company.SaveOrUpdate<Taxonomy>(model, fields);

                dynamic custom = new
                {
                    TaxonomyTypeID = model.TaxonomyTypeID,
                    ParentID = model.ParentID,
                    Name = model.Name,
                    Context = form["_context"]
                };

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
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


        [HttpPost, ValidateInput(false)]
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

        [Route("catalogs/add")]
        public ActionResult AddTaxonomyType()
        {
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyType,
                FieldUri = "/form/TaxonomyType_AddFields",
                FormTitle = "Add Model",
                FormUri = "/form/AddTaxonomyType",
                FormMethod = "POST",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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

                Company.SaveOrUpdate<TaxonomyType>(a);

                for (int i = 1; i <= a.MaximumDepth; i++)
                {
                    Company.Set<TaxonomyTypeLevel>().Add(new TaxonomyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), TaxonomyTypeID = a.ID });
                }
                Company.SaveChanges();

                upsertObjectStyle(SystemObjects.TaxonomyType, a.ID, form, a.Name);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [Route("catalogs/{taxonomyTypeId:int}")]
        public ActionResult DeleteTaxonomyById(int taxonomyTypeId)
        {
            var form = new FormCollection();
            form.Add("ID", taxonomyTypeId.ToString());
            return DeleteTaxonomyType(form);
        }

        [Route("catalogs/{id:int}/delete")]
        public ActionResult DeleteTaxonomyType(int id)
        {
            var a = Company.GetById<TaxonomyType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyType,
                FieldUri = "/form/TaxonomyType_DeleteFields?id=" + id,
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormDescription = Resources.FormInfo.TaxonomyType_Remove,
                FormUri = "/form/DeleteTaxonomyType",
                FormMethod = "DELETE",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [HttpPut, ValidateInput(false)]
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

        [Route("catalogs/{id:int}/edit")]
        public ActionResult EditTaxonomyType(int id)
        {
            var a = Company.GetById<TaxonomyType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyType,
                FieldUri = "/form/TaxonomyType_EditFields?id=" + id,
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditTaxonomyType",
                FormMethod = "PUT",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        public ActionResult AddTaxonomyTypeClass()
        {
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyTypeClass,
                FieldUri = "/form/TaxonomyTypeClass_AddFields",
                FormTitle = "Add Model Class",
                FormUri = "/form/AddTaxonomyTypeClass",
                FormMethod = "POST",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("OverlayEditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddTaxonomyTypeClass(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.TaxonomyTypeClass, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type class");

                var a = new TaxonomyTypeClass
                {
                    Name = parseTextField(form, "Name", null, true)
                };

                Company.SaveOrUpdate<TaxonomyTypeClass>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteTaxonomyTypeClass(int id)
        {
            var a = Company.GetById<TaxonomyTypeClass>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyTypeClass,
                FieldUri = "/form/TaxonomyTypeClass_DeleteFields?id=" + id,
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormDescription = Resources.FormInfo.TaxonomyType_Remove,
                FormUri = "/form/DeleteTaxonomyTypeClass",
                FormMethod = "DELETE",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditTaxonomyTypeClass(int id)
        {
            var a = Company.GetById<TaxonomyTypeClass>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyTypeClass,
                FieldUri = "/form/TaxonomyTypeClass_EditFields?id=" + id,
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditTaxonomyTypeClass",
                FormMethod = "PUT",
                FormSize = EditableForm.FormSize_Small
            };

            return PartialView("OverlayEditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);

                Company.SaveOrUpdate<TaxonomyTypeClass>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        [HttpPost, ValidateInput(false)]
        public JsonResult AddTaxonomyTypeLevelRaw(TaxonomyTypeLevelModel template)
        {
            var form = new FormCollection();
            form.Add("Name", template.Name);
            form.Add("Description", template.Description);
            form.Add("Level", template.Level);
            form.Add("ID", template.TaxonomyTypeID);

            return AddTaxonomyTypeLevel(form);
        }

        public ActionResult AddTaxonomyTypeLevel(int id)
        {
            var type = Company.GetById<TaxonomyType>(id);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyTypeLevel,
                FieldUri = string.Format("/form/TaxonomyTypeLevel_AddFields?id={0}", id),
                FormTitle = string.Format("Add {0} Level", type.Name),
                FormUri = "/form/AddTaxonomyTypeLevel",
                FormMethod = "POST"
            };
            type = null;

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
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
                    Name = parseTextField(form, "Name", null, true),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<TaxonomyTypeLevel>(a);

                return jsonSuccess(a.Name + " successfully created.", a.TaxonomyTypeID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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
        public ActionResult DeleteTaxonomyTypeLevelById(int taxonomyTypeId, int taxonomyTypeLevelId)
        {
            var form = new FormCollection();
            form.Add("Level", taxonomyTypeLevelId.ToString());
            form.Add("ID", taxonomyTypeId.ToString());
            return DeleteTaxonomyTypeLevel(form);
        }

        public ActionResult DeleteTaxonomyTypeLevel(int id, int level)
        {
            var a = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id && i.Level == level).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyTypeLevel,
                FieldUri = string.Format("/form/TaxonomyTypeLevel_DeleteFields?id={0}&level={1}", id, level),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormDescription = Resources.FormInfo.TaxonomyType_Remove,
                FormUri = "/form/DeleteTaxonomyTypeLevel",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteTaxonomyTypeLevel(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id && i.Level == level);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditTaxonomyTypeLevelRaw(TaxonomyTypeLevelModel template)
        {
            var form = new FormCollection();
            form.Add("Name", template.Name);
            form.Add("Description", template.Description);
            form.Add("Level", template.Level);
            form.Add("ID", template.TaxonomyTypeID);

            return EditTaxonomyTypeLevel(form);
        }

        public ActionResult EditTaxonomyTypeLevel(int id, int level)
        {
            var a = Company.Filter<TaxonomyTypeLevel>(i => i.TaxonomyTypeID == id && i.Level == level).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TaxonomyTypeLevel,
                FieldUri = string.Format("/form/TaxonomyTypeLevel_EditFields?id={0}&level={1}", id, level),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditTaxonomyTypeLevel",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
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

                model.Name = parseTextField(form, "Name", null, true);
                model.Description = parseTextField(form, "Description");

                Company.Update<TaxonomyTypeLevel>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult TooltipTemplate_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<TooltipTemplate>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TooltipTemplateID</param>
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


        [HttpPost, ValidateInput(false)]
        public JsonResult AddTooltipTemplateRaw(TemplateModel template)
        {
            var form = new FormCollection();            
            form.Add("Name", template.Name);
            form.Add("Description", template.Description);
            form.Add("TemplateBody", template.TemplateBody);
            form.Add("Action", template.Action);

            return AddTooltipTemplate(form);
        }

        [Route("templates/tooltip/add")]
        public ActionResult AddTooltipTemplate()
        {
            var model = new EditableForm
            {
                Context = ContextList.TooltipTemplate,
                FieldUri = "/form/TooltipTemplate_AddFields",
                FormTitle = "Add Tooltip Template",
                FormUri = "/form/AddTooltipTemplate",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddTooltipTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("tooltip template");

                var a = new TooltipTemplate
                {
                    Action = parseTextField(form, "Action"),
                    Description = parseTextField(form, "Description"),
                    Name = parseTextField(form, "Name", null, true),
                    TemplateBody = parseTextField(form, "TemplateBody")
                };

                Company.Add<TooltipTemplate>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("templates/tooltip/{id:int}/delete")]
        public ActionResult DeleteTooltipTemplate(int id)
        {
            var a = Company.GetById<TooltipTemplate>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TooltipTemplate,
                FieldUri = string.Format("/form/TooltipTemplate_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteTooltipTemplate",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }
                
        [Route("templates/tooltip/{templateId:int}")]
        public ActionResult DeleteTooltipTemplateById(int templateId)
        {
            var form = new FormCollection();
            form.Add("ID", templateId.ToString());
            return DeleteTooltipTemplate(form);
        }


        [HttpDelete]
        public JsonResult DeleteTooltipTemplate(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("tooltip template");

                var id = parseIntField(form, "ID");
                Company.Delete<TooltipTemplate>(i => i.ID == id);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [Route("templates/tooltip/{id:int}/edit")]
        public ActionResult EditTooltipTemplate(int id)
        {
            var a = Company.GetById<TooltipTemplate>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.TooltipTemplate,
                FieldUri = string.Format("/form/TooltipTemplate_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditTooltipTemplate",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        public class TemplateModel
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Action { get; set; }
            public string TemplateBody { get; set; }
        }

        [HttpPut, ValidateInput(false)]        
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


        [HttpPut, ValidateInput(false)]
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
                model.Name = parseTextField(form, "Name", null, true);
                model.TemplateBody = parseTextField(form, "TemplateBody");

                Company.Update<TooltipTemplate>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        #region WorkflowTypeRelation

        #region Field Generation

        public JsonResult WorkflowAllocation_DeleteFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var relation = Company.GetById<WorkflowTypeRelation>(id);

            if (relation == null)
                return jsonException("Workflow allocation not found.", HttpStatusCode.NotFound);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = relation.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        void checkValueAndAddNode(string key, FormCollection form, XElement xml)
        {
            try
            {
                if (form.AllKeys.Any(i => i == key))
                {
                    if (xml.Element(key) != null)
                    {
                        xml.Element(key).SetValue(form[key]);
                    }
                    else
                    {
                        xml.Add(new XElement(key, form[key]));
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        XElement getWorkflowTypRelationFields(WorkflowType type, FormCollection form, XElement xml = null)
        {
            if (xml == null) xml = XElement.Parse("<fields/>");

            switch (type)
            {
                case WorkflowType.CertifyArtifact:
                    checkValueAndAddNode("DateForScheduleCalculation", form, xml);
                    checkValueAndAddNode("MonthsUntilCertification", form, xml);
                    checkValueAndAddNode("DaysGivenToCompleteCertification", form, xml);                    
                    break;                
                case WorkflowType.SuggestNewArtifactMulti:
                    checkValueAndAddNode("ResponsibilityFinalApproval", form, xml);
                    break;
            }

            return xml;
        }

        public ActionResult AddWorkflowAllocation(WorkflowType workflowType)
        {
            var desc = Resources.FormInfo.Allocate_Workflow_Description;
            if (workflowType == WorkflowType.ChallengeArtifact)
                desc = Resources.FormInfo.Allocate_Workflow_Challenge_Description;

            var model = new WorkflowTypeRelationEditorModel
            {
                FormDescription = desc,
                FormMethod = "POST",
                FormName = Resources.FormInfo.Allocate_Workflow_Title,
                FormUri = "/form/AddWorkflowAllocation",
                ObjectTypes = Company.GetWorkflowObjectTypeOptions().Select(i => new SelectListItem { Text = i.Name, Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) }).ToList(),
                WorkflowType = workflowType,
                WorkflowTypeRelation = new WorkflowTypeRelation { Enabled = true }
            };

            return PartialView("WorkflowTypeRelationEditForm", model);
        }

        [ValidateHttpAntiForgeryToken]
        [HttpPost, ValidateInput(false)]
        public JsonResult AddWorkflowAllocation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var workflowType = (WorkflowType)Enum.Parse(typeof(WorkflowType), form["WorkflowType"]);
                var ObjectValue = form["ObjectType"];
                var ObjectArray = (string.IsNullOrEmpty(ObjectValue)) ? new string[2] : ObjectValue.Split('|');
                var Object = ObjectArray[0];
                int ObjectID = int.Parse(ObjectArray[1]);

                var ParentValue = form["ParentType"];
                var ParentArray = (string.IsNullOrEmpty(ParentValue)) ? new string[2] { null, "0" } : ParentValue.Split('|');
                var Parent = ParentArray[0];
                int? ParentID = null;
                if (ParentArray[1] != "0") ParentID = int.Parse(ParentArray[1]);

                var responsibilityTypeID = parseIntField(form, "ResponsibilityType");

                if (Company.Filter<WorkflowTypeRelation>(i =>
                    i.WorkflowType == workflowType &&
                    i.Object == Object && i.ObjectID == ObjectID &&
                    i.Parent == Parent && i.ParentID == ParentID
                   ).Any())
                {
                    throw new DuplicateObjectException("Workflow Allocation");
                }

                var model = new WorkflowTypeRelation { FieldsXml = "<fields/>" };

                // Static fields
                model.WorkflowType = workflowType;

                model.Object = Object;
                model.ObjectID = ObjectID;

                if (!string.IsNullOrEmpty(Parent))
                {
                    model.Parent = Parent;
                    model.ParentID = ParentID;
                }

                model.Enabled = parseBooleanField(form, "Enabled");
                model.ResponsibilityTypeID = responsibilityTypeID;
                model.FieldsXml = getWorkflowTypRelationFields(workflowType, form).ToString();
                Company.Add<WorkflowTypeRelation>(model);

                return jsonSuccess(string.Format(Resources.FormInfo.Allocate_Workflow_Confirmation, workflowType.GetWorkflowTypeDisplayName()), "0", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteWorkflowAllocation(int id)
        {
            var model = new EditableForm
            {
                Context = ContextList.WorkflowTypeRelation,
                FieldUri = string.Format("/form/WorkflowAllocation_DeleteFields?id={0}", id),
                FormTitle = Resources.FormInfo.DeAllocate_Workflow_Title,
                FormUri = "/form/DeleteWorkflowAllocation",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteWorkflowAllocation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                if (!Company.CurrentResourceIsAdmin)
                    throw new UnauthorizedException("Workflow Allocation Error", "You do not have permission to remove this workflow allocation.");

                var id = parseIntField(form, "ID");

                var model = Company.GetById<WorkflowTypeRelation>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                Company.Delete<WorkflowTypeRelation>(model);

                return jsonSuccess(Resources.FormInfo.DeAllocate_Workflow_Confirmation, "0", form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
        public JsonResult DeleteWorkflowAllocationByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteWorkflowAllocation(form);
        }

        public ActionResult EditWorkflowAllocation(int id)
        {
            var relation = Company.GetById<WorkflowTypeRelation>(id);
            
            var parentTypes = Company.GetWorkflowParentTypeOptions((int)relation.WorkflowType, relation.Object, relation.ObjectID, true);
            var responsibilityTypes = Company.GetWorkflowResponsibilityTypeOptions(relation.Object, relation.ObjectID);

            var desc = Resources.FormInfo.Allocate_Workflow_Description;
            if (relation.WorkflowType == WorkflowType.ChallengeArtifact)
                desc = Resources.FormInfo.Allocate_Workflow_Challenge_Description;

            var model = new WorkflowTypeRelationEditorModel
            {
                FormDescription = desc,
                FormMethod = "PUT",
                FormName = Resources.FormInfo.Allocate_Workflow_Title,
                FormUri = "/form/EditWorkflowAllocation",
                ObjectTypes = Company.GetWorkflowObjectTypeOptions().Select(i => new SelectListItem { 
                    Text = i.Name, 
                    Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID), 
                    Selected = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) == string.Format("{0}|{1}", relation.Object, relation.ObjectID) 
                }).ToList(),
                ParentTypes = parentTypes.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID),
                    Selected = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) == string.Format("{0}|{1}", relation.Parent, relation.ParentID)
                }).ToList(),
                ResponsibilityTypes = responsibilityTypes.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.ID.ToString(),
                    Selected = (i.ID == relation.ResponsibilityTypeID)
                }).ToList(),
                Enabled = relation.Enabled,
                WorkflowType = relation.WorkflowType,
                WorkflowTypeRelation = relation
            };

            return PartialView("WorkflowTypeRelationEditForm", model);
        }

        [HttpGet]
        public JsonNetResult WorkflowAllocation(int? id, WorkflowType? workflowType)
        {
            var model = new WorkflowTypeRelationEditorModel();

            if (id != null && id > 0)
            {
                var relation = Company.GetById<WorkflowTypeRelation>((int)id);

                var parentTypes = Company.GetWorkflowParentTypeOptions((int)relation.WorkflowType, relation.Object, relation.ObjectID, true);
                var responsibilityTypes = Company.GetWorkflowResponsibilityTypeOptions(relation.Object, relation.ObjectID);

                var desc = Resources.FormInfo.Allocate_Workflow_Description;
                if (relation.WorkflowType == WorkflowType.ChallengeArtifact)
                    desc = Resources.FormInfo.Allocate_Workflow_Challenge_Description;

                model = new WorkflowTypeRelationEditorModel
                {
                    FormDescription = desc,
                    FormMethod = "PUT",
                    FormName = Resources.FormInfo.Allocate_Workflow_Title,
                    FormUri = "/form/EditWorkflowAllocation",
                    ObjectTypes = Company.GetWorkflowObjectTypeOptions().Select(i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID),
                        Selected = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) == string.Format("{0}|{1}", relation.Object, relation.ObjectID)
                    }).ToList(),
                    ParentTypes = parentTypes.Select(i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID),
                        Selected = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) == string.Format("{0}|{1}", relation.Parent, relation.ParentID)
                    }).ToList(),
                    ResponsibilityTypes = responsibilityTypes.Select(i => new SelectListItem
                    {
                        Text = i.Name,
                        Value = i.ID.ToString(),
                        Selected = (i.ID == relation.ResponsibilityTypeID)
                    }).ToList(),
                    Enabled = relation.Enabled,
                    WorkflowType = relation.WorkflowType,
                    WorkflowTypeRelation = relation
                };
            }
            else
            {
                var desc = Resources.FormInfo.Allocate_Workflow_Description;
                if (workflowType == WorkflowType.ChallengeArtifact)
                    desc = Resources.FormInfo.Allocate_Workflow_Challenge_Description;

                model = new WorkflowTypeRelationEditorModel
                {
                    FormDescription = desc,
                    FormMethod = "POST",
                    FormName = Resources.FormInfo.Allocate_Workflow_Title,
                    FormUri = "/form/AddWorkflowAllocation",
                    ObjectTypes = Company.GetWorkflowObjectTypeOptions().Select(i => new SelectListItem { Text = i.Name, Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) }).ToList(),
                    WorkflowType = (WorkflowType)workflowType,
                    WorkflowTypeRelation = new WorkflowTypeRelation { Enabled = true }
                };
            }
          

            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost]
        public JsonResult WorkflowAllocation(WorkflowTypeRelation r)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    throw new UnauthorizedException("Workflow Allocation Error", "You do not have permission to update this workflow allocation.");

                if (Company.Filter<WorkflowTypeRelation>(i =>
                    i.WorkflowType == r.WorkflowType &&
                    i.Object == r.Object && i.ObjectID == r.ObjectID &&
                    i.Parent == r.Parent && i.ParentID == r.ParentID &&
                    i.ID != r.ID
                   ).Any())
                {
                    throw new DuplicateObjectException("Workflow Allocation");
                }

                var model = Company.GetById<WorkflowTypeRelation>(r.ID);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                // Static fields
                model.Object = r.Object;
                model.ObjectID = r.ObjectID;

                if (!string.IsNullOrEmpty(r.Parent))
                {
                    model.Parent = r.Parent;
                    model.ParentID = r.ParentID;
                }

                model.Enabled = r.Enabled; // parseBooleanField(form, "Enabled");
                model.ResponsibilityTypeID = r.ResponsibilityTypeID; // responsibilityTypeID;


                var xml = XElement.Parse("<fields/>");
               
                if (r.WorkflowType == WorkflowType.CertifyArtifact)
                {
                    foreach(var key in r.Fields.Keys)
                    {
                        try
                        {
                            if (xml.Element(key) != null)
                                xml.Element(key).SetValue(r.Fields[key]);
                            else
                                xml.Add(new XElement(key, r.Fields[key]));
                        }
                        catch { }

                    }
                }

                model.FieldsXml = xml.ToString();

                Company.Update<WorkflowTypeRelation>(model);

                return jsonSuccess(Resources.FormInfo.Edit_Workflow_Allocation_Confirmation, "0", null, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditWorkflowAllocation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                if (!Company.CurrentResourceIsAdmin)
                    throw new UnauthorizedException("Workflow Allocation Error", "You do not have permission to update this workflow allocation.");

                var workflowType = (WorkflowType)Enum.Parse(typeof(WorkflowType), form["WorkflowType"]);
                var ObjectValue = form["ObjectType"];
                var ObjectArray = (string.IsNullOrEmpty(ObjectValue)) ? new string[2] : ObjectValue.Split('|');
                var Object = ObjectArray[0];
                int ObjectID = int.Parse(ObjectArray[1]);

                var ParentValue = form["ParentType"];
                var ParentArray = (string.IsNullOrEmpty(ParentValue)) ? new string[2] { null, "0" } : ParentValue.Split('|');
                var Parent = ParentArray[0];
                int? ParentID = null;
                if (ParentArray[1] != "0") ParentID = int.Parse(ParentArray[1]);

                var id = parseIntField(form, "ID");
                var responsibilityTypeID = parseIntField(form, "ResponsibilityType");

                if (Company.Filter<WorkflowTypeRelation>(i => 
                    i.WorkflowType == workflowType && 
                    i.Object == Object && i.ObjectID == ObjectID &&
                    i.Parent == Parent && i.ParentID == ParentID &&
                    i.ID != id
                   ).Any())
                {
                    throw new DuplicateObjectException("Workflow Allocation");
                }

                var model = Company.GetById<WorkflowTypeRelation>(id);
                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                // Static fields
                model.Object = Object;
                model.ObjectID = ObjectID;

                if (!string.IsNullOrEmpty(Parent))
                {
                    model.Parent = Parent;
                    model.ParentID = ParentID;
                }

                model.Enabled = parseBooleanField(form, "Enabled");
                model.ResponsibilityTypeID = responsibilityTypeID;
                model.FieldsXml = getWorkflowTypRelationFields(workflowType, form).ToString();

                Company.Update<WorkflowTypeRelation>(model);

                return jsonSuccess(Resources.FormInfo.Edit_Workflow_Allocation_Confirmation, "0", form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
    }
}
