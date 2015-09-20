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

namespace d360.web.Controllers
{
    [RoutePrefix("form"), Authorize]
    public class FormController : BaseController
    {
        #region DI

        ISecurityContextProvider SecProvider;

        public FormController(CommunityContext community, CompanyContext company, ISecurityContextProvider secProvider)
            : base(community, company)
        {
            SecProvider = secProvider;
        }

        #endregion

        #region Field Loading For Type Forms Below

        List<FieldValidationModel> checkAndAddValidation(string fieldType, string friendlyName, bool required, string pattern, int? minLength, int? maxLength, string validationMessage = "")
        {
            var models = new List<FieldValidationModel>();

            #region Validation

            if (fieldType != "Lookup")
            {
                if (string.IsNullOrEmpty(validationMessage))
                {
                    switch (fieldType)
                    {
                        case "Number":
                            validationMessage = string.Format(Validation.Pattern_Tokenized, friendlyName, "must be a whole number");
                            break;
                        case "Decimal":
                            validationMessage = string.Format(Validation.Pattern_Tokenized, friendlyName, "must be a decimal number");
                            break;
                    }
                }

                // Required validation
                if (required)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.Required_Tokenized, friendlyName), rule = "required" });
                }

                // Pattern validation
                if (!string.IsNullOrEmpty(pattern))
                {
                    models.Add(new FieldValidationModel { action = "blur", message = validationMessage, regex = pattern });
                }

                // Min/Max next precedent
                if (maxLength.HasValue && minLength.HasValue)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.Length_Tokenized, friendlyName, minLength.Value, maxLength.Value), rule = string.Format("length={0},{1}", minLength.Value, maxLength.Value) });
                }
                // Min next precedent
                else if (minLength.HasValue)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.MaxLength_Tokenized, friendlyName, minLength.Value), rule = string.Format("minLength={0}", minLength.Value) });
                }
                // Max next precedent
                else if (maxLength.HasValue)
                {
                    models.Add(new FieldValidationModel { action = "blur", message = string.Format(Validation.MinLength_Tokenized, friendlyName, maxLength.Value), rule = string.Format("maxLength={0}", maxLength.Value) });
                }
            }

            #endregion

            return models.Count > 0 ? models : null;
        }
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

            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "IconBackColor", Name = "Icon Back Color", FieldDescription = "The icon's background color", FieldType = DataType.Color.ToString(), Value = b });
            list.Add(new EditableField { Row = row, Column = 2, Required = true, FieldName = "IconForeColor", Name = "Icon Fore Color", FieldDescription = "The icon's text color", FieldType = DataType.Color.ToString(), Value = f });
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

        List<EditableField> loadDynamicFields(List<EditableField> list, List<FieldTypeWithRelation> fields, int startRow = 10)
        {
            var row = startRow;

            fields.ForEach(f =>
            {
                var patternMessage = "";

                if (string.IsNullOrEmpty(f.ValidationDescription))
                {
                    switch (f.Type)
                    { 
                        case "Number":
                            patternMessage = "must be a whole number";
                            break;
                        case "Decimal":
                            patternMessage = "must be a decimal number";
                            break;
                    }
                }
                else 
                {
                    patternMessage = f.ValidationDescription;
                }


                var fld = new EditableField
                {
                    Row = row,
                    Column = 1,
                    FieldName = f.Name,
                    Name = f.FriendlyName,
                    FieldType = f.Type.ToString(),
                    FieldDescription = f.FormDescription,
                    Validations = checkAndAddValidation(f.Type.ToString(), f.FriendlyName, f.IsRequired, f.Pattern, f.MinimumLength, f.MaximumLength, patternMessage)
                };

                if (!string.IsNullOrEmpty(f.LookupObjectType))
                {
                    fld.FieldType = DataType.Lookup.ToString();
                    try
                    {
                        fld.Items = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == f.ID && o.LookupObjectType == f.LookupObjectType && o.LookupObjectID == f.LookupObjectID.Value)
                            .OrderBy(o => o.Text)
                            .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                            .ToList();
                        if (!f.IsRequired) fld.Items.Insert(0, new SelectListItem { Text = "Choose...", Value = "" });
                    }
                    catch
                    {
                        fld.Items.Add(new SelectListItem { Text = "No valid lookup found", Value = "" });
                    }
                }
                fld.Required = (f.MinimumLength > 0 || f.Length > 0);
                /* Boolean, Date, DateTime, Decimal, Integer, String */
                list.Add(fld);

                row++;
            });

            return list;
        }

        List<EditableField> loadDynamicFields(List<EditableField> list, List<FieldTypeWithRelation> fieldTypes, List<FieldWithRelation> fields, int startRow = 10)
        {
            var row = startRow;

            fieldTypes.ForEach(ft =>
            {
                var f = fields.SingleOrDefault(i => i.FieldTypeID == ft.ID);

                var patternMessage = "";

                if (string.IsNullOrEmpty(ft.ValidationDescription))
                {
                    switch (ft.Type)
                    {
                        case "Number":
                            patternMessage = "must be a whole number";
                            break;
                        case "Decimal":
                            patternMessage = "must be a decimal number";
                            break;
                    }
                }
                else
                {
                    patternMessage = ft.ValidationDescription;
                }

                var fld = new EditableField
                {
                    Row = row,
                    Column = 1,
                    FieldName = ft.Name,
                    Name = ft.FriendlyName,
                    FieldType = ft.Type.ToString(),
                    FieldDescription = ft.FormDescription,
                    Validations = checkAndAddValidation(ft.Type.ToString(), ft.FriendlyName, ft.IsRequired, ft.Pattern, ft.MinimumLength, ft.MaximumLength, patternMessage)
                };

                if (!string.IsNullOrEmpty(ft.LookupObjectType))
                {
                    fld.FieldType = DataType.Lookup.ToString();
                    try
                    {
                        fld.Items = Company.Filter<FieldLookupValue>(o => o.FieldTypeID == ft.ID && o.LookupObjectType == ft.LookupObjectType && o.LookupObjectID == ft.LookupObjectID.Value)
                            .OrderBy(o => o.Text)
                            .Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                            .ToList();
                        if (!ft.IsRequired) fld.Items.Insert(0, new SelectListItem { Text = "Choose...", Value = "" });
                    }
                    catch
                    {
                        fld.Items.Add(new SelectListItem { Text = "No valid lookup found", Value = "" });
                    }
                }
                fld.Required = (ft.MinimumLength > 0 || ft.Length > 0);
                /* Boolean, Date, DateTime, Decimal, Integer, String */
                if (f != null) fld.Value = f.Value;
                list.Add(fld);

                row++;
            });

            return list;
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

        JsonResult jsonException(string message, HttpStatusCode statusCode, string title = "Error Occurred!")
        {
            Response.StatusCode = (int)statusCode;
            Response.StatusDescription = message.Replace("\n", "  ").Replace("\r", " ");
            return Json(new { type = "error", title = title, message = Response.StatusDescription }, JsonRequestBehavior.AllowGet);
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

        string parseTextField(FormCollection form, string fieldName, string defaultValue = null)
        {
            return form.AllKeys.Any(i => i == fieldName) ? form[fieldName] : defaultValue;
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
            var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == type.ID && i.WorkflowType == WorkflowType.SuggestNewArtifact).Any();

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

            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = row, Column = 2, Required = true, FieldName = "TaxonomyTypeID", Name = Resources.FieldInfo.TaxonomyType_Name, FieldDescription = Resources.FieldInfo.TaxonomyType_Description, FieldType = DataType.Lookup.ToString(), Items = Company.Table<TaxonomyType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });
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
            var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == a.ArtifactTypeID && i.WorkflowType == WorkflowType.SuggestNewArtifact).Any();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            if (a.ParentID.HasValue)
            {
                var currentParent = Company.GetById<Artifact>(a.ParentID.Value);
                var parents = Company.Filter<Artifact>(i => i.ArtifactTypeID == currentParent.ArtifactTypeID).OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
                list.Add(new EditableField { Row = 1, Column = 1, FieldName = "ParentID", Name = string.Format("{0}", currentParent.ArtifactType.Name), FieldType = DataType.Lookup.ToString(), Value = a.ParentID.ToString(), Items = parents });
                currentParent = null;
            }

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "TaxonomyTypeID", Name = Resources.FieldInfo.TaxonomyType_Name, FieldDescription = Resources.FieldInfo.TaxonomyType_Description, FieldType = DataType.Lookup.ToString(), Value = a.TaxonomyTypeID.ToString(), Items = Company.Table<TaxonomyType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });

            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            if (!workflowEnabled)
                list = loadStatusField(list, SystemObjects.Artifact, a.Status, 4, 1);

            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, a.ArtifactTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Artifact, id).ToList(), 5);

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

            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = row, Column = 2, Required = true, FieldName = "TaxonomyTypeID", Name = Resources.FieldInfo.TaxonomyType_Name, FieldDescription = Resources.FieldInfo.TaxonomyType_Description, FieldType = DataType.Lookup.ToString(), Items = Company.Table<TaxonomyType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });
            row++;
            list.Add(new EditableField { Row = row, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });
            row++;
            list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, at).ToList(), row + 1);

            return Json(list, JsonRequestBehavior.AllowGet);
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

                var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == typeID && i.WorkflowType == WorkflowType.SuggestNewArtifact).Any();

                int taxonomyTypeID = parseIntField(form, "TaxonomyTypeID");

                var model = new Artifact();
                // Static fields
                model.ArtifactTypeID = typeID;
                model.TaxonomyTypeID = taxonomyTypeID;
                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                var workflowEnabled = Company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == model.ArtifactTypeID && i.WorkflowType == WorkflowType.SuggestNewArtifact).Any();


                // Static fields
                if (!isPromoted) model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.TaxonomyTypeID = parseIntField(form, "TaxonomyTypeID");
                if (!workflowEnabled) model.Status = form["Status"];

                //model.TaxonomyTypeID = string.IsNullOrEmpty(form["TaxonomyTypeID"]) ? new Nullable<int>() : parseIntField(form, "TaxonomyTypeID");
                model.ParentID = parseIntField(form, "ParentID");
                if (model.ParentID == 0) model.ParentID = null;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Artifact, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, model.ArtifactTypeID).ToList(), form, Server);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Attribute, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.AttributeType, model.AttributeTypeID).ToList(), form, Server);
                
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                AttributeType = new AttributeType { ParentID = parentID },
                AttributeTypeCategories = (parentID.HasValue) ? new List<SelectListItem>() : Company.Table<AttributeTypeCategory>().OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList()
            };
            if (!parentID.HasValue)
            {
                model.AttributeTypeCategories.Insert(0, new SelectListItem { Text = "Enterprise-wide", Value = "0" });
            }

            return PartialView("AttributeTypeEditForm", model);
        }

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                if (model.ParentID.HasValue) id = model.ParentID.Value;

                Company.Delete<AttributeType>(model);

                return jsonSuccess(Resources.FormInfo.Delete_AttributeType_Confirmation, id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region Domain

        #region Field Generation

        /// <param name="t">DomainTypeID</param>
        public JsonResult Domain_AddFields(int t, int g)
        {
            if (!Company.HasPermission(SystemObjects.DomainType, t, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "DomainGroupID", FieldType = DataType.Hidden.ToString(), Value = g.ToString() });
            list.Add(new EditableField { FieldName = "DomainTypeID", FieldType = DataType.Hidden.ToString(), Value = t.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

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

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "DomainGroupID", Name = "Grouping", FieldType = DataType.Lookup.ToString(), Items = groups, Value = a.DomainGroupID.Value.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
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
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    EnforceParentItemSelection = false,
                    DomainGroupID = parseIntField(form, "DomainGroupID")
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.DomainGroupID = parseIntField(form, "DomainGroupID");

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

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
                    Name = parseTextField(form, "Name"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region FieldType

        #region Field Generation

        /// <param name="id">ID of the object</param>
        public JsonResult FieldType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.FieldType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            if (Company.Table<FieldWithRelation>().Any(i => i.FieldTypeID == id))
                return jsonException(FormInfo.FieldType_Error_Used, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<FieldType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddFieldType(SystemObjects type, int id)
        {
            var model = new FieldTypeEditorModel
            {
                LookupLists = convertToEditableFieldItems(Company.GetFieldTypeLookupOptions().ToList()),
                FormUri = "/Form/AddFieldType",
                FormMethod = "POST",
                FormName = Resources.FormInfo.Add_FieldType_Title,
                FieldType = new FieldType { Object = type.ToString(), ObjectID = id, Pattern = "", Type = DataType.Text.ToString(), IsListable = true, IsRequired = true }
            };

            for (var i = 0; i < model.DataTypes.Count; i++)
            {
                model.DataTypes[i].Selected = (model.DataTypes[i].Value == model.FieldType.Type);
            }

            return PartialView("FieldTypeEditForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddFieldType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var model = new FieldType();

                var type = (SystemObjects)Enum.Parse(typeof(SystemObjects), form["Object"]);
                var id = parseIntField(form, "ObjectID");

                int maxSort = 0;
                try { maxSort = Company.GetFieldTypeRelationsByObject(type, id).Max(i => i.SortOrder); }
                catch { }

                // Static fields
                model.Object = type.ToString();
                model.ObjectID = id;
                model.Name = parseTextField(form, "Name");
                model.FriendlyName = string.IsNullOrEmpty(form["FriendlyName"]) ? form["Name"] : form["FriendlyName"];
                model.DisplayDescription = parseTextField(form, "DisplayDescription", "");
                model.FormDescription = parseTextField(form, "FormDescription", "");
                model.ValidationDescription = parseTextField(form, "ValidationDescription", "");
                model.Type = parseTextField(form, "Type");
                model.IsListable = parseBooleanField(form, "IsListable");
                model.IsRequired = parseBooleanField(form, "IsRequired");
                model.SortOrder = maxSort + 1;

                int value;
                //if (int.TryParse(form["Length"], out value)) model.Length = value;
                if (int.TryParse(form["MinimumLength"], out value)) model.MinimumLength = value;
                if (model.MinimumLength.HasValue)
                {
                    if (model.MinimumLength.Value == 0) model.MinimumLength = null;
                }
                if (int.TryParse(form["MaximumLength"], out value)) model.MaximumLength = value;
                if (model.MaximumLength.HasValue)
                {
                    if (model.MaximumLength.Value == 0) model.MaximumLength = null;
                }

                if (model.MinimumLength.HasValue && model.MaximumLength.HasValue)
                {
                    if (model.MinimumLength.Value > model.MaximumLength.Value)
                    {
                        throw new ConflictException("Error Occurred!", "You may not have a minimum length that is greater than the maximum length.");
                    }
                }

                model.Pattern = parseTextField(form, "Pattern");
                if (model.Type == DataType.Lookup.ToString())
                {
                    var lookupValues = parseTextField(form, "LookupObject");
                    if (!string.IsNullOrEmpty(lookupValues))
                    {
                        var split = lookupValues.Split('|');
                        model.LookupObjectType = split[0].Replace("Type", "");
                        model.LookupObjectID = int.Parse(split[1]);
                        model.LookupDisplayFormat = parseTextField(form, "LookupDisplayFormat");
                    }
                }

                Company.Add<FieldType>(model);

                return jsonSuccess(Resources.FormInfo.Add_FieldType_Confirmation, model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                Company.Delete<FieldType>(model);

                return jsonSuccess(Resources.FormInfo.Delete_FieldType_Confirmation, id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditFieldType(int id)
        {
            var a = Company.GetById<FieldType>(id);
            if (a == null) return HttpNotFound();
            var used = Company.Fields.Any(i => i.FieldTypeID == id);
            var qry = Company.FieldTypeLookupValues.OrderBy(i => i.LookupObjectType).ThenBy(i => i.Name).AsQueryable();
            var model = new FieldTypeEditorModel
            {
                LookupLists = convertToEditableFieldItems(Company.GetFieldTypeLookupOptions().ToList()),
                FormUri = "/Form/EditFieldType",
                FieldIsUsed = used,
                FormMethod = "PUT",
                FormName = Resources.FormInfo.Edit_FieldType_Title,
                FieldType = a
            };

            for (var i = 0; i < model.DataTypes.Count; i++)
            {
                model.DataTypes[i].Selected = (model.DataTypes[i].Value == model.FieldType.Type);
            }

            if (model.FieldType.Type == DataType.Lookup.ToString())
            {
                var selectedListValue = string.Format("{0}|{1}", model.FieldType.LookupObjectType, model.FieldType.LookupObjectID);
                model.LookupLists.ForEach(l => {
                    l.Selected = (l.Value == selectedListValue);
                });
            }

            return PartialView("FieldTypeEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFieldType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<FieldType>(id);
                var used = model.Fields.Any(i => i.FieldTypeID == id);

                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                // Static fields
                model.Name = parseTextField(form, "Name");
                model.FriendlyName = string.IsNullOrEmpty(form["FriendlyName"]) ? form["Name"] : form["FriendlyName"];
                model.DisplayDescription = parseTextField(form, "DisplayDescription", "");
                model.FormDescription = parseTextField(form, "FormDescription", "");
                model.ValidationDescription = parseTextField(form, "ValidationDescription", "");
                model.IsListable = parseBooleanField(form, "IsListable");
                model.IsRequired = parseBooleanField(form, "IsRequired");

                int value;
                //if (int.TryParse(form["Length"], out value)) model.Length = value; else model.Length = null;
                if (int.TryParse(form["MinimumLength"], out value)) model.MinimumLength = value; else model.MinimumLength = null;
                if (model.MinimumLength.HasValue)
                {
                    if (model.MinimumLength.Value == 0) model.MinimumLength = null;
                }
                if (int.TryParse(form["MaximumLength"], out value)) model.MaximumLength = value; else model.MaximumLength = null;
                if (model.MaximumLength.HasValue)
                {
                    if (model.MaximumLength.Value == 0) model.MaximumLength = null;
                }
                model.Pattern = parseTextField(form, "Pattern");

                if (!used)
                {
                    model.Type = parseTextField(form, "Type");
                    if (model.Type == DataType.Lookup.ToString())
                    {
                        var lookupValues = parseTextField(form, "LookupObject");
                        if (!string.IsNullOrEmpty(lookupValues))
                        {
                            var split = lookupValues.Split('|');
                            model.LookupObjectType = split[0].Replace("Type", "");
                            model.LookupObjectID = int.Parse(split[1]);
                        }
                    }
                }
                model.LookupDisplayFormat = parseTextField(form, "LookupDisplayFormat");

                Company.Update<FieldType>(model);

                return jsonSuccess(Resources.FormInfo.Edit_FieldType_Confirmation, id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name")//,
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                model.Name = parseTextField(form, "Name");
                model.IntervalType = (JobIntervalType)Enum.Parse(typeof(JobIntervalType), form["IntervalType"]);
                model.Interval = parseIntField(form, "Interval");
                model.ForceRefresh = parseBooleanField(form, "ForceRefresh");


                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Fusion, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.FusionType, model.FusionTypeID).ToList(), form, Server);
                
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionOwnerRule(FormCollection form)//(FusionOwnerEditListModel model)
        {
            try
            {
                var item = new FusionAttributeOwnerRule
                {
                    RelationshipOwnerObjectType = "Artifact",
                    RelationshipOwnerObjectID = parseIntField(form , "FusionOwnerOptionsDropdown"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

            return PartialView("FusionAttributePromotionRuleEditForm", model);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionAttributeOwnerRuleItem(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "FusionAttributeOwnerRuleID");
                var fusionAttributeIDs = form["FusionAttributeID"].Split(',').ToList();
                if (fusionAttributeIDs.Count == 0)
                {
                    Company.FusionAttributeOwnerRuleItems.Add(
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
                        Company.FusionAttributeOwnerRuleItems.Add(
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionAttributeOwnerRuleItem(int id)
        {
            var a = Company.GetById<FusionAttributePromotionRuleItem>(id);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region FusionPromotionRule

        #region Field Generation

        /// <param name="id">FusionAttributePromotionRuleID</param>
        public JsonResult FusionPromotionRule_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Fusion>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("fusion/{typeID:int}/configurations/{fusionID:int}/promotions/add")]
        public ActionResult AddFusionPromotionRule(int typeID, int fusionID)
        {
            //if (!Company.HasPermission(SystemObjects.AttributeType, 0, Claim.Create))
            //    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var model = new FusionPromotionRuleEditorModel
            {
                FusionID = fusionID,
                FusionTypeID = typeID,
                FormUri = "/Form/AddFusionPromotionRule",
                FormMethod = "POST",
                FormName = "Add Promotion Rule",
                AttributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == typeID).ToList(),
                Rule = new FusionAttributePromotionRule { FusionID = fusionID, Enabled = true }
            };
            return PartialView("FusionAttributePromotionRuleEditForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionPromotionRule(FormCollection form)//(FusionPromotionEditListModel model)
        {
            try
            {
                var promotionObject = form["PrOptionsDropdown"].Split('|');
                var promotionParent = form["PrOptionsParentDropdown"];

                var item = new FusionAttributePromotionRule
                {
                    Enabled = parseBooleanField(form, "Enabled"),
                    FusionID = parseIntField(form, "FusionID"),
                    ObjectType = "FusionAttributeType",
                    ObjectID = parseIntField(form, "FusionAttributeTypeID"),
                    PromotionObjectType = promotionObject[0],
                    PromotionObjectID = int.Parse(promotionObject[1])
                };

                if (!string.IsNullOrEmpty(promotionParent))
                {
                    item.PromotionParentObjectType = item.PromotionObjectType.Replace("Type", "");
                    item.PromotionParentObjectID = int.Parse(promotionParent);
                }

                Company.Add<FusionAttributePromotionRule>(item);

                return jsonSuccess("Items marked for auto-promotion", "0", ContextList.FusionPromotionRule, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionPromotionRule(int id)
        {
            var a = Company.GetById<FusionAttributePromotionRule>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionPromotionRule,
                FieldUri = string.Format("/form/FusionPromotionRule_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this promotion rule"),
                FormUri = "/form/DeleteFusionPromotionRule",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionPromotionRule(FormCollection form)//(int typeID, int id)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                Company.Delete<FusionAttributePromotionRule>(i => i.ID == id);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditFusionPromotionRule(int id)
        {
            var a = Company.GetById<FusionAttributePromotionRule>(id, i => i.Fusion);
            if (a == null) return HttpNotFound();

            var model = new FusionPromotionRuleEditorModel
            {
                FusionID = a.FusionID,
                FusionTypeID = 0,
                FormUri = "/Form/EditFusionPromotionRule",
                FormMethod = "PUT",
                FormName = "Update Promotion Rule",
                Rule = a,
                AttributeTypes = Company.Filter<FusionAttributeType>(i => i.FusionTypeID == a.Fusion.FusionTypeID).ToList()
            };

            return PartialView("FusionAttributePromotionRuleEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFusionPromotionRule(FormCollection form)
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region FusionPromotionRuleItem

        #region Field Generation

        public JsonResult FusionPromotionRuleItem_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddFusionAttributePromotionRuleItem(int id)
        {
            var rule = Company.GetById<FusionAttributePromotionRule>(id, i => i.Fusion, i => i.FusionAttributePromotionRuleMappings);

            if (rule == null)
                return jsonException("Rule not found", HttpStatusCode.NotFound);

            if (!Company.HasPermission(SystemObjects.Fusion, rule.FusionID, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionPromotionRuleItemEditorModel
            {
                FormUri = "/Form/AddFusionAttributePromotionRuleItem",
                FormMethod = "POST",
                FormName = "Add Promotion Target Item",
                FusionID = rule.FusionID,
                TargetFusionAttributeTypeID = rule.ObjectID,
                Item = new FusionAttributePromotionRuleItem { FusionAttributePromotionRuleID = id }
        };
            return PartialView("FusionAttributePromotionRuleItemEditForm", editorModel);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionAttributePromotionRuleItem(FormCollection form)
        {
            try
            {
                var ruleID = parseIntField(form, "FusionAttributePromotionRuleID");
                var fusionAttributeIDs = form["FusionAttributeID"].Split(',').ToList();
                if (fusionAttributeIDs.Count == 0)
                {
                    Company.FusionAttributePromotionRuleItems.Add(
                        new FusionAttributePromotionRuleItem { FusionAttributePromotionRuleID = ruleID, FusionAttributeID = null }
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
                        Company.FusionAttributePromotionRuleItems.Add(
                            new FusionAttributePromotionRuleItem { FusionAttributePromotionRuleID = ruleID, FusionAttributeID = fusionAttributeID }
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionAttributePromotionRuleItem(int id)
        {
            var a = Company.GetById<FusionAttributePromotionRuleItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionPromotionRuleItem,
                FieldUri = string.Format("/form/FusionPromotionRuleItem_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this target item"),
                FormUri = "/form/DeleteFusionAttributePromotionRuleItem",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionAttributePromotionRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                Company.Delete<FusionAttributePromotionRuleItem>(i => i.ID == id);
                return jsonSuccess("Target item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region FusionPromotionRuleMapping

        #region Field Generation

        public JsonResult FusionPromotionRuleMapping_DeleteFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        List<SelectListItem> loadSourceItemOptions(FusionAttributePromotionRule rule, FusionAttributePromotionRuleMapping existingItem = null)
        {
            #region Process Source Field Logic

            var sourceFieldIDs = rule.FusionAttributePromotionRuleMappings.Where(i => i.SourceFieldTypeID > 0).Select(i => i.SourceFieldTypeID).ToList();
            var sourceFieldNames = rule.FusionAttributePromotionRuleMappings.Where(i => i.SourceFieldTypeID == 0).Select(i => i.SourceFieldName).ToList();

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

            var sourceFields = Company.Filter<FieldType>(i => i.Object == rule.ObjectType && i.ObjectID == rule.ObjectID)
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
            sourceFields.Insert(0, new SelectListItem { Text = "Name", Value = "Name|0" });

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

            sourceFields.ForEach(i =>
            {
                i.Selected = i.Value.Contains(selectedID);
            });

            return sourceFields;
        }

        List<SelectListItem> loadTargetItemOptions(FusionAttributePromotionRule rule, FusionAttributePromotionRuleMapping existingItem = null)
        {
            var targetFields = new List<SelectListItem>();

            #region Process Target Field Logic

            var targetFieldIDs = rule.FusionAttributePromotionRuleMappings.Where(i => i.TargetFieldTypeID > 0).Select(i => i.TargetFieldTypeID).ToList();
            var targetFieldNames = rule.FusionAttributePromotionRuleMappings.Where(i => i.TargetFieldTypeID == 0).Select(i => i.TargetFieldName).ToList();

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

            var promotionType = rule.PromotionObjectType;
            switch (promotionType)
            {
                case "DomainType":
                    if (!targetFieldNames.Contains("Name"))
                        targetFields.Add(new SelectListItem { Text = "Name", Value = "Name|0" });
                    if (rule.PromotionParentObjectType == "Domain")
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
                    var targetDynamicFields = Company.Filter<FieldType>(i => i.Object == promotionType && i.ObjectID == rule.PromotionObjectID)
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

        public ActionResult AddFusionAttributePromotionRuleMapping(int id)
        {
            var rule = Company.GetById<FusionAttributePromotionRule>(id, i => i.Fusion, i => i.FusionAttributePromotionRuleMappings);

            if (rule == null)
                return jsonException("Rule not found", HttpStatusCode.NotFound);

            if (!Company.HasPermission(SystemObjects.Fusion, rule.FusionID, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var editorModel = new FusionPromotionRuleMappingEditorModel
            {
                FormUri = "/Form/AddFusionAttributePromotionRuleMapping",
                FormMethod = "POST",
                FormName = "Add Promotion Field Mapping",
                Item = new FusionAttributePromotionRuleMapping { FusionAttributePromotionRuleID = id },
                SourceFields = loadSourceItemOptions(rule),
                TargetFields = loadTargetItemOptions(rule)
            };
            return PartialView("FusionAttributePromotionRuleMappingEditForm", editorModel);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddFusionAttributePromotionRuleMapping(FormCollection form)
        {
            try
            {
                var model = new FusionAttributePromotionRuleMapping
                {
                    FusionAttributePromotionRuleID = parseIntField(form, "FusionAttributePromotionRuleID")
                };

                var source = form["Source"].Split('|');
                var target = form["Target"].Split('|');

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

                Company.Add<FusionAttributePromotionRuleMapping>(model);

                return jsonSuccess("Field mapping successfully created.", "0", ContextList.FusionPromotionRuleMapping, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteFusionAttributePromotionRuleMapping(int id)
        {
            var a = Company.GetById<FusionAttributePromotionRuleMapping>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                FormSize = "small",
                Context = ContextList.FusionPromotionRuleMapping,
                FieldUri = string.Format("/form/FusionPromotionRuleMapping_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this field mapping"),
                FormUri = "/form/DeleteFusionAttributePromotionRuleMapping",
                FormMethod = "DELETE"
            };

            return PartialView("OverlayDeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteFusionAttributePromotionRuleMapping(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("configuration");
                var id = parseIntField(form, "ID");
                Company.Delete<FusionAttributePromotionRuleMapping>(i => i.ID == id);
                return jsonSuccess("Mapping successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditFusionAttributePromotionRuleMapping(int id)
        {
            var a = Company.GetById<FusionAttributePromotionRuleMapping>(id, i => i.FusionAttributePromotionRule.FusionAttributePromotionRuleMappings);
            if (a == null) return HttpNotFound();

            var editorModel = new FusionPromotionRuleMappingEditorModel
            {
                FormUri = "/Form/EditFusionAttributePromotionRuleMapping",
                FormMethod = "PUT",
                FormName = "Update Promotion Field Mapping",
                Item = a,
                SourceFields = loadSourceItemOptions(a.FusionAttributePromotionRule, a),
                TargetFields = loadTargetItemOptions(a.FusionAttributePromotionRule, a)
            };

            return PartialView("FusionAttributePromotionRuleMappingEditForm", editorModel);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditFusionAttributePromotionRuleMapping(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("field mapping");

                var model = Company.GetById<FusionAttributePromotionRuleMapping>(parseIntField(form, "ID"));
                if (model == null) throw new NotFoundException("field mapping");

                var source = form["Source"].Split('|');
                var target = form["Target"].Split('|');

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

                Company.Update<FusionAttributePromotionRuleMapping>(model);

                return jsonSuccess("Field mapping successfully updated.", model.ID.ToString(), ContextList.FusionPromotionRuleMapping, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name")
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                model.Name = parseTextField(form, "Name");

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name")
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

                model.Name = parseTextField(form, "Name");

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region Intersect/Other Relationships

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
                return jsonException(ex.Message.Replace(Environment.NewLine, " "), HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message.Replace(Environment.NewLine, " "), HttpStatusCode.InternalServerError);
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

        #region Form Get/Post

        public JsonNetResult IntersectType_FormData(int id)
        {
            var type = Company.GetById<IntersectType>(id, i => i.Nodes);
            if (type == null) return new JsonNetResult { Data = null };

            var currentIntersects = Company.Filter<Intersect>(i => i.IntersectTypeID == id).Any();
            var first = type.Nodes.OrderBy(i => i.Order).First();
            var last = type.Nodes.OrderBy(i => i.Order).Last();

            var model = new IntersectTypeEditorModel
            {
                ID = id,
                LimitedChangesOnly = currentIntersects,
                Roles = Company.Filter<IntersectTypeRoleRelation>(i => i.IntersectTypeID == id, i => i.IntersectTypeRole)
                .Select(i => new IntersectTypeRoleEditorModel {
                    RoleID = i.IntersectTypeRoleID,
                    Side1Label = i.Side1Label,
                    Side2Label = i.Side2Label
                }).ToList(),
                Side1 = string.Format("{0}|{1}", first.ObjectType, first.ObjectID),
                Side1DisplayText = first.MenuDisplayText,
                Side2 = string.Format("{0}|{1}", last.ObjectType, last.ObjectID),
                Side2DisplayText = last.MenuDisplayText
            };

            return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult IntersectType_RoleOptions()
        {
            var models = Company.Table<IntersectTypeRole>()
                .OrderBy(i => i.Name)
                .Select(i => new { i.Name, ID = i.ID });

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
                .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        void saveIntersectTypeRoles(IntersectTypeEditorModel formModel, List<IntersectTypeRoleRelation> existingRoles = null)
        {
            var globalRoles = Company.Table<IntersectTypeRole>().ToList();

            if (existingRoles == null)  // If NULL, set as 0-sized list.
            {
                existingRoles = new List<IntersectTypeRoleRelation>();
            }

            if (formModel.Roles != null)
            {
                foreach (var roleModel in formModel.Roles)
                {
                    IntersectTypeRoleRelation roleRelation = null;
                    IntersectTypeRole newRole = null;

                    if (!string.IsNullOrEmpty(roleModel.NewRoleName))
                    {
                        if (globalRoles.Any(i => i.Name.ToLower() == roleModel.NewRoleName.Trim().ToLower()))
                        {
                            newRole = globalRoles.FirstOrDefault(i => i.Name.ToLower() == roleModel.NewRoleName.Trim().ToLower());
                        }
                        else
                        {
                            newRole = new IntersectTypeRole { Name = roleModel.NewRoleName };
                            Company.Add<IntersectTypeRole>(newRole);

                            roleModel.RoleID = newRole.ID;
                        }
                    }
                    else
                    {
                        if (roleModel.RoleID.HasValue)
                        {
                            roleRelation = new IntersectTypeRoleRelation { IntersectTypeRoleID = roleModel.RoleID.Value };

                            if (globalRoles.Any(i => i.ID == roleModel.RoleID.Value))
                            {
                                newRole = globalRoles.FirstOrDefault(i => i.ID == roleModel.RoleID.Value);
                            }
                        }
                    }

                    if (newRole != null)
                    {
                        if (existingRoles.Any(i => i.IntersectTypeRoleID == newRole.ID))
                        {
                            roleRelation = existingRoles.FirstOrDefault(i => i.IntersectTypeRoleID == newRole.ID);
                            roleRelation.Side1Label = roleModel.Side1Label;
                            roleRelation.Side2Label = roleModel.Side2Label;
                            Company.Update<IntersectTypeRoleRelation>(roleRelation);
                        }
                        else
                        {
                            roleRelation = new IntersectTypeRoleRelation { IntersectTypeRoleID = newRole.ID, IntersectTypeID = formModel.ID, Side1Label = roleModel.Side1Label, Side2Label = roleModel.Side2Label };
                            Company.Add<IntersectTypeRoleRelation>(roleRelation);
                            existingRoles.Add(roleRelation);
                        }
                    }
                }
            }

            var saveForRemovals = false;
            foreach (var existingRole in existingRoles)
            {
                if (!formModel.Roles.Any(i => i.RoleID == existingRole.IntersectTypeRoleID))
                {
                    Company.IntersectTypeRoleRelations.Remove(existingRole);
                    saveForRemovals = true;
                }
            }
            if (saveForRemovals)
            {
                Company.SaveChanges();
            }
        }

        public ActionResult AddIntersectType()
        {
            ViewBag.ID = 0;
            return PartialView("IntersectTypeEditForm");
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddIntersectType(IntersectTypeEditorModel formModel)
        {
            try
            {
                if (formModel == null) throw new NoFormDataException("relationship type");

                var nodes = new List<IntersectTypeNode>();

                var side1 = formModel.Side1;
                short side1Order = 1;
                var side1Info = side1.Split('|');
                var node1 = new IntersectTypeNode { ObjectID = int.Parse(side1Info[1]), ObjectType = side1Info[0], Order = side1Order };
                if (!string.IsNullOrEmpty(formModel.Side1DisplayText))
                    node1.MenuDisplayText = formModel.Side1DisplayText;

                nodes.Add(node1);

                var side2 = formModel.Side2;
                short side2Order = 2;
                var side2Info = side2.Split('|');
                var node2 = new IntersectTypeNode { ObjectID = int.Parse(side2Info[1]), ObjectType = side2Info[0], Order = side2Order };
                if (!string.IsNullOrEmpty(formModel.Side2DisplayText))
                    node2.MenuDisplayText = formModel.Side2DisplayText;
                nodes.Add(node2);

                Company.ValidateIntersectType(0, nodes);

                var model = new IntersectType { 
                    Nodes = nodes
                };
                Company.Add<IntersectType>(model);
                formModel.ID = model.ID;

                //saveIntersectTypeRoles(formModel);

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), ContextList.IntersectType, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditIntersectType(int id)
        {
            ViewBag.ID = id;
            return PartialView("IntersectTypeEditForm");
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditIntersectType(IntersectTypeEditorModel formModel)
        {
            try
            {
                if (formModel == null) throw new NoFormDataException("relationship type");

                // Permisisons validation.
                if (!Company.HasPermission(SystemObjects.IntersectType, formModel.ID, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                
                var model = Company.GetById<IntersectType>(formModel.ID, i => i.Nodes, i => i.RoleRelations);
                if (model == null) throw new NotFoundException("relationship type");

                var nodes = new List<IntersectTypeNode>();

                var side1 = formModel.Side1;
                short side1Order = 1;
                var side1Info = side1.Split('|');

                var side2 = formModel.Side2;
                short side2Order = 2;
                var side2Info = side2.Split('|');

                var side1Node = new IntersectTypeNode { ObjectID = int.Parse(side1Info[1]), ObjectType = side1Info[0], Order = side1Order };
                if (!string.IsNullOrEmpty(formModel.Side1DisplayText))
                    side1Node.MenuDisplayText = formModel.Side1DisplayText;
                nodes.Add(side1Node);

                var side2Node = new IntersectTypeNode { ObjectID = int.Parse(side2Info[1]), ObjectType = side2Info[0], Order = side2Order };
                if (!string.IsNullOrEmpty(formModel.Side2DisplayText))
                    side2Node.MenuDisplayText = formModel.Side2DisplayText;
                nodes.Add(side2Node);


                // Validation
                Company.ValidateIntersectType(formModel.ID, nodes);


                // Now set the properties we need to overwrite.

                var existingSide1Node = model.Nodes.Single(i => i.Order == 1);
                existingSide1Node.ObjectType = side1Node.ObjectType;
                existingSide1Node.ObjectID = side1Node.ObjectID;
                existingSide1Node.MenuDisplayText = side1Node.MenuDisplayText;

                var existingSide2Node = model.Nodes.Single(i => i.Order == 2);
                existingSide2Node.ObjectType = side2Node.ObjectType;
                existingSide2Node.ObjectID = side2Node.ObjectID;
                existingSide2Node.MenuDisplayText = side2Node.MenuDisplayText;

                Company.Update<IntersectType>(model);
                Company.Update<IntersectTypeNode>(existingSide1Node);
                Company.Update<IntersectTypeNode>(existingSide2Node);

                //saveIntersectTypeRoles(formModel, model.RoleRelations.ToList());

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), ContextList.IntersectType, "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                .Select(i => new { ID = i.ID, i.FirstName, i.LastName })
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
            if (!Company.HasPermission(SystemObjects.Group, id, Claim.Update))  return jsonException("You do not have permissions to add users.", HttpStatusCode.Forbidden);
            if (!Company.Groups.Any(i => i.ID == id))                           return jsonException("No group exists for the specified ID.", HttpStatusCode.NotFound);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            var currentGroupUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
            var resList = GetCompanyResources()
                .Where(i => !currentGroupUsers.Contains(i.ID))
                .Select(i => new { ID = i.ID, i.FirstName, i.LastName }).ToList().Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() }).ToList();
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
                .Select(i => new { ID = i.ID, i.FirstName, i.LastName, MembershipStatus = currentGroupUsers.Any(o => o == i.ID) ? "Current Member" : "Not Yet a Member" })
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

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #endregion

        #region Load

        class OptionModel
        {
            public string title { get; set; }
            public string value { get; set; }
        }

        public JsonNetResult Load_TypeOptions(string action)
        {
            IEnumerable<OptionModel> models;
            var sql = "";
            switch (action) {
                case "P": // Promotion
                    #region
                    sql = @"
select 'ArtifactType|' + cast(ID as varchar(10)) as value, Name as title from ArtifactType order by Name";
                    break;
                    #endregion
                case "R": // Relation
                case "U": // Unrelation
                    #region
                    sql = @"select 'IntersectType|' + cast(ID as varchar(10)) as value, Name as title from IntersectType order by Name";
                    break;
                    #endregion
            }
            models = Company.Query<OptionModel>(sql);

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }


        public ActionResult AddLoad(int id)
        {
            ViewData.Add("LoadTypeID", id);
            return PartialView();
        }

        public class LoadFilePostModel
        {
            public string Action { get; set; }
            public string Type { get; set; }
            public string Notes { get; set; }
            public string File { get; set; }
        }

        [HttpPost]
        public HttpStatusCodeResult AddLoadFile(int id)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Load, 0, Claim.Create))
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);

                var a = new Load
                {
                    Date = DateTime.UtcNow,
                    LoadTypeID = id
                };

                var file = Request.Files[0];
                var fileExt = Path.GetExtension(file.FileName);
                var target = new MemoryStream();
                file.InputStream.CopyTo(target);
                byte[] data = target.ToArray();

                a.File = data;

                SLDocument xls;
                var success = false;
                var errorMessage = "";

                if (fileExt.ToLower() == ".xlsx")
                {
                    xls = new SLDocument(target);

                    var loadType = Company.GetById<LoadType>(id, i => i.LoadTypeFields);

                    var stats = xls.GetWorksheetStatistics();
                    var columnCount = stats.NumberOfColumns;
                    if (columnCount == loadType.LoadTypeFields.Count)
                    {
                        success = true;
                    }
                    else
                    {
                        errorMessage = "The number of columns in the spreadsheet does not match the number of defined fields for this load type.";
                    }
                }
                else
                {
                    errorMessage = "Incorrect file type";
                }

                data = null;
                target = null;

                if (success)
                {
                    Company.Add<Load>(a);
                    return new HttpStatusCodeResult(HttpStatusCode.Created, "File uploaded and queued for processing.");
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, errorMessage);
                }
            }
            catch (BaseException ex)
            {
                return new HttpStatusCodeResult(ex.StatusCode, ex.StatusDescription);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        //[HttpPost]
        //public JsonResult AddLoadFile(LoadFilePostModel model)
        //{
        //    try
        //    {
        //        var base64Data = System.Text.RegularExpressions.Regex.Match(model.File, @"data:application/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
        //        var byteArray = Convert.FromBase64String(base64Data);
        //        using (var stream = new MemoryStream(byteArray))
        //        {
        //            var boo = stream.CanRead;
        //        }

        //        return jsonSuccess("File uploaded and queued for processing.", "0", ContextList.Load, "A", HttpStatusCode.Created);
        //    }
        //    catch (BaseException ex)
        //    {
        //        return jsonException(ex.StatusDescription, ex.StatusCode); //jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return jsonException(ex.Message, HttpStatusCode.InternalServerError);//jsonException(ex.Message, HttpStatusCode.InternalServerError);
        //    }
        //}

        #endregion

        #region LoadType

        #region Field Generation

        public JsonResult LoadType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.LoadType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LoadTypeID</param>
        public JsonResult LoadType_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LoadType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LoadTypeID</param>
        public JsonResult LoadType_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LoadType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddLoadType()
        {
            var model = new EditableForm
            {
                Context = ContextList.LoadType,
                FieldUri = "/form/LoadType_AddFields",
                FormTitle = "Add New Bulk Load Type",
                FormUri = "/form/AddLoadType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost]
        public JsonResult AddLoadType(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LoadType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("bulk load type");

                var a = new LoadType
                {
                    Name = parseTextField(form, "Name")
                };

                Company.SaveOrUpdate<LoadType>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteLoadType(int id)
        {
            var a = Company.GetById<LoadType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LoadType,
                FieldUri = string.Format("/form/LoadType_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteLoadType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteLoadType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadType>(id);
                if (model == null) throw new NotFoundException("bulk load type");

                if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<LoadType>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditLoadType(int id)
        {
            var a = Company.GetById<LoadType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LoadType,
                FieldUri = string.Format("/form/LoadType_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditLoadType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut]
        public JsonResult EditLoadType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadType>(id);
                if (model == null) throw new NotFoundException("bulk load type");

                if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");

                Company.SaveOrUpdate<LoadType>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusMessage, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region LoadTypeField

        #region Field Generation

        /// <param name="id">LoadTypeID</param>
        public JsonResult LoadTypeField_AddFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin) //if (!Company.HasPermission(SystemObjects.LoadTypeField, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var lookups = convertToEditableFieldItems(
                Company.GetLoadTypeFieldLookupOptions().ToList()
            );
            lookups.Insert(0, new SelectListItem { Text = "-None-", Value = "", Selected = true });

            list.Add(new EditableField { FieldName = "LoadTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "LookupObject", Name = "Lookup Type", FieldType = DataType.Lookup.ToString(), Items = lookups });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "LookupFieldName", Name = "Lookup Field", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "LookupFieldName", false, "", 0, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LoadTypeFieldID</param>
        public JsonResult LoadTypeField_DeleteFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin) return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LoadTypeField>(id);

            if (a == null) return jsonException("Load type field not found", HttpStatusCode.NotFound);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LoadTypeFieldID</param>
        public JsonResult LoadTypeField_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin) //if (!Company.HasPermission(SystemObjects.LoadTypeField, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LoadTypeField>(id);

            if (a == null) return jsonException("Load type field not found", HttpStatusCode.NotFound);

            var lookups = convertToEditableFieldItems(
                Company.GetLoadTypeFieldLookupOptions().ToList()
            );
            lookups.Insert(0, new SelectListItem { Text = "-None-", Value = "" });

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "LookupObject", Name = "Lookup Type", FieldType = DataType.Lookup.ToString(), Items = lookups, Value = (a.LookupObjectID.HasValue ? string.Format("{0}|{1}", a.LookupObjectType.ToString(), a.LookupObjectID) : "") });
            list.Add(new EditableField { Row = 2, Column = 2, Required = false, FieldName = "LookupFieldName", Name = "Lookup Field", FieldType = DataType.Text.ToString(), Value = a.LookupFieldName, Validations = checkAndAddValidation("Text", "LookupFieldName", false, "", 0, 250) });
            
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddLoadTypeField(int id)
        {
            var model = new EditableForm
            {
                Context = ContextList.LoadTypeField,
                FieldUri = "/form/LoadTypeField_AddFields?id=" + id,
                FormTitle = "Add Load Type Field",
                FormUri = "/form/AddLoadTypeField",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost]
        public JsonResult AddLoadTypeField(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin) //if (!Company.HasPermission(SystemObjects.LoadType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("bulk load type field");

                var loadTypeID = parseIntField(form, "LoadTypeID");
                var sortOrder = Company.Filter<LoadTypeField>(i => i.LoadTypeID == loadTypeID).Count() + 1;
                var lookupObjectValue = form["LookupObject"];

                if (lookupObjectValue.Contains("None")) lookupObjectValue = string.Empty;
                string[] split = string.IsNullOrEmpty(lookupObjectValue) ? null : lookupObjectValue.Split('|');

                var a = new LoadTypeField
                {
                    LoadTypeID = loadTypeID,
                    LookupObjectType = (split != null) ? split[0] : null,
                    LookupObjectID = (split != null) ? int.Parse(split[1]) : new Nullable<int>(),
                    LookupFieldName = form["LookupFieldName"],
                    Name = parseTextField(form, "Name"),
                    SortOrder = sortOrder
                };
                Company.SaveOrUpdate<LoadTypeField>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteLoadTypeField(int id)
        {
            var a = Company.GetById<LoadTypeField>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LoadTypeField,
                FieldUri = string.Format("/form/LoadTypeField_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteLoadTypeField",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteLoadTypeField(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type field");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadTypeField>(id);
                if (model == null) throw new NotFoundException("bulk load type field");

                if (!Company.CurrentResourceIsAdmin) //if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<LoadTypeField>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditLoadTypeField(int id)
        {
            var a = Company.GetById<LoadTypeField>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LoadTypeField,
                FieldUri = string.Format("/form/LoadTypeField_EditFields?id={0}", id),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditLoadTypeField",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut]
        public JsonResult EditLoadTypeField(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type field");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadTypeField>(id);
                if (model == null) throw new NotFoundException("bulk load type field");

                if (!Company.CurrentResourceIsAdmin) //if (!Company.HasPermission(SystemObjects.LoadTypeField, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var lookupObjectValue = form["LookupObject"];
                if (lookupObjectValue.Contains("None")) lookupObjectValue = string.Empty;
                string[] split = string.IsNullOrEmpty(lookupObjectValue) ? null : lookupObjectValue.Split('|');
                model.LookupObjectType = (split != null) ? split[0] : null;
                model.LookupObjectID = (split != null) ? int.Parse(split[1]) : new Nullable<int>();
                model.LookupFieldName = form["LookupFieldName"];
                model.Name = parseTextField(form, "Name");

                Company.SaveOrUpdate<LoadTypeField>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusMessage, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region LoadTypeRule

        #region Field Generation

        /// <param name="id">LoadTypeRuleID</param>
        public JsonResult LoadTypeRule_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LoadTypeRule, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LoadTypeRule>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddLoadTypeRule(int id)
        {
            if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var model = new LoadTypeRuleEditorModel
            {
                LoadTypeID = id,
                Fields = Company.Filter<LoadTypeField>(i => i.LoadTypeID == id)
                            .OrderBy(i => i.SortOrder)
                            .ThenBy(i => i.Name)
                            .Select(i => new SelectListItem { Value = i.ID.ToString(), Text = i.Name })
                            .ToList(),
                LookupTypeRuleGroups = new List<SelectListItem>() {
                                            new SelectListItem { Text = LoadTypeRuleGroup.Promotion.ToString(), Value = ((int)LoadTypeRuleGroup.Promotion).ToString() },
                                            new SelectListItem { Text = LoadTypeRuleGroup.Relation.ToString(), Value = ((int)LoadTypeRuleGroup.Relation).ToString() }
                                        },
                LookupTypeRuleGroupsEnabled = true,
                Objects = convertToEditableFieldItems(
                            Company.GetLoadTypeRulePromotionOptions().ToList()
                          )
            };

            return PartialView(model);
        }

        [HttpPost]
        public JsonResult AddLoadTypeRule(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LoadType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("bulk load type rule");

                var loadTypeID = parseIntField(form, "LoadTypeID");
                var sortOrder = Company.Filter<LoadTypeRule>(i => i.LoadTypeID == loadTypeID).Count() + 1;
                var loadTypeRuleGroup = (LoadTypeRuleGroup)Enum.Parse(typeof(LoadTypeRuleGroup), form["LookupTypeRuleGroup"]);

                var a = new LoadTypeRule
                {
                    LoadTypeID = loadTypeID,
                    LoadTypeRuleGroup = loadTypeRuleGroup,
                    SortOrder = sortOrder
                };

                if (loadTypeRuleGroup == LoadTypeRuleGroup.Promotion)
                {
                    var split = form["Object"].Split('|');

                    a.ObjectType = split[0];
                    a.ObjectID = int.Parse(split[1]);
                    a.UniqueLoadTypeFieldID = parseIntField(form, "UniqueLoadTypeFieldID");
                }

                Company.SaveOrUpdate<LoadTypeRule>(a);

                if (loadTypeRuleGroup == LoadTypeRuleGroup.Promotion)
                {
                    var ruleItemsToAdd = new List<LoadTypeRuleItem>();

                    var ot = (SystemObjects)Enum.Parse(typeof(SystemObjects), a.ObjectType);
                    var loadType = Company.GetById<LoadType>(loadTypeID, i => i.LoadTypeFields);
                    var targetFields = Company.GetFieldTypeRelationsByObject(ot, a.ObjectID).ToList();

                    foreach (var s in loadType.LoadTypeFields)
                    {
                        switch (ot)
                        { 
                            case SystemObjects.ArtifactType:
                                switch (s.Name)
                                { 
                                    case "Name":
                                    case "Description":
                                        ruleItemsToAdd.Add(new LoadTypeRuleItem { IsCustomField = false, LoadTypeRuleID = a.ID, SourceLoadTypeFieldID = s.ID, TargetFieldName = s.Name });
                                        break;
                                }
                                break;
                            case SystemObjects.AttributeType:
                                switch (s.Name)
                                {
                                    case "Owner":
                                        if (s.LookupObjectType == "ArtifactType" || s.LookupObjectType == "DomainType" || s.LookupObjectType == "TaxonomyType")
                                            ruleItemsToAdd.Add(new LoadTypeRuleItem { IsCustomField = false, LoadTypeRuleID = a.ID, SourceLoadTypeFieldID = s.ID, TargetFieldName = "ObjectID" });
                                        break;
                                }
                                break;
                            case SystemObjects.TaxonomyType:
                                switch (s.Name)
                                {
                                    case "Name":
                                    case "Description":
                                        ruleItemsToAdd.Add(new LoadTypeRuleItem { IsCustomField = false, LoadTypeRuleID = a.ID, SourceLoadTypeFieldID = s.ID, TargetFieldName = s.Name });
                                        break;
                                }
                                break;
                        }

                        foreach (var t in targetFields)
                        {
                            if (t.Type == "Lookup" && t.LookupObjectType == s.LookupObjectType && t.LookupObjectID == s.LookupObjectID)
                            {
                                ruleItemsToAdd.Add(new LoadTypeRuleItem { IsCustomField = true, LoadTypeRuleID = a.ID, SourceLoadTypeFieldID = s.ID, TargetFieldName = t.Name });
                            }
                            else
                            {
                                if (s.Name.ToLower() == t.Name.ToLower())
                                {
                                    ruleItemsToAdd.Add(new LoadTypeRuleItem { IsCustomField = true, LoadTypeRuleID = a.ID, SourceLoadTypeFieldID = s.ID, TargetFieldName = t.Name });
                                }
                            }
                        }
                    }
                    ruleItemsToAdd.ForEach(ri => {
                        Company.Add<LoadTypeRuleItem>(ri);
                    });
                    
                }

                return jsonSuccess("Rule successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteLoadTypeRule(int id)
        {
            var a = Company.GetById<LoadTypeRule>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LoadTypeRule,
                FieldUri = string.Format("/form/LoadTypeRule_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "Rule"),
                FormUri = "/form/DeleteLoadTypeRule",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteLoadTypeRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type rule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadTypeRule>(id);
                if (model == null) throw new NotFoundException("bulk load type rule");

                if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<LoadTypeRuleItem>(i => i.LoadTypeRuleID == id);
                Company.Delete<LoadTypeRule>(model);
                
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditLoadTypeRule(int id)
        {
            var a = Company.GetById<LoadTypeRule>(id);
            if (a == null) return HttpNotFound();

            var model = new LoadTypeRuleEditorModel
            {
                ID = a.ID,
                LoadTypeID = a.LoadTypeID,
                Fields = Company.Filter<LoadTypeField>(i => i.LoadTypeID == a.LoadTypeID)
                            .OrderBy(i => i.SortOrder)
                            .ThenBy(i => i.Name)
                            .Select(i => new SelectListItem { Value = i.ID.ToString(), Text = i.Name })
                            .ToList(),
                LookupTypeRuleGroups = new List<SelectListItem>() {
                                            new SelectListItem { Text = LoadTypeRuleGroup.Promotion.ToString(), Value = ((int)LoadTypeRuleGroup.Promotion).ToString() },
                                            new SelectListItem { Text = LoadTypeRuleGroup.Relation.ToString(), Value = ((int)LoadTypeRuleGroup.Relation).ToString() }
                                        },
                LookupTypeRuleGroupsEnabled = false,
                Objects = convertToEditableFieldItems(
                            Company.GetLoadTypeRulePromotionOptions().ToList()
                          )
            };

            foreach (var o in model.Fields)
            {
                o.Selected = (o.Value == a.UniqueLoadTypeFieldID.ToString());
            }
            foreach (var o in model.LookupTypeRuleGroups)
            {
                o.Selected = (o.Value == ((int)a.LoadTypeRuleGroup).ToString());
            }
            foreach (var o in model.Objects)
            {
                o.Selected = (o.Value == string.Format("{0}|{1}", a.ObjectType, a.ObjectID));
            }


            return PartialView(model);
        }

        [HttpPut]
        public JsonResult EditLoadTypeRule(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type rule");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadTypeRule>(id);
                if (model == null) throw new NotFoundException("bulk load type rule");

                if (!Company.HasPermission(SystemObjects.LoadTypeRule, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.LoadTypeRuleGroup = (LoadTypeRuleGroup)Enum.Parse(typeof(LoadTypeRuleGroup), form["LookupTypeRuleGroup"]);

                var loadTypeRuleGroup = (LoadTypeRuleGroup)Enum.Parse(typeof(LoadTypeRuleGroup), form["LookupTypeRuleGroup"]);
                if (loadTypeRuleGroup == LoadTypeRuleGroup.Promotion)
                {
                    var split = form["Object"].Split('|');
                    model.ObjectType = split[0];
                    model.ObjectID = int.Parse(split[1]);
                    model.UniqueLoadTypeFieldID = parseIntField(form, "UniqueLoadTypeFieldID");                
                }

                Company.SaveOrUpdate<LoadTypeRule>(model);

                return jsonSuccess("Rule successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusMessage, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region LoadTypeRuleItem

        #region Field Generation

        /// <param name="id">LoadTypeRuleID</param>
        public JsonResult LoadTypeRuleItem_AddFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LoadType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            
            var list = new List<EditableField>();

            var loadTypeRule = Company.GetById<LoadTypeRule>(id);

            list.Add(new EditableField { FieldName = "LoadTypeRuleID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            
            var sourceFields = Company.Filter<LoadTypeField>(i => i.LoadTypeID == loadTypeRule.LoadTypeID)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.ID.ToString(), Text = i.Name })
                .ToList();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "SourceLoadTypeFieldID", Name = "Source Field", FieldType = DataType.Lookup.ToString(), Items = sourceFields });

            if (loadTypeRule.ObjectType != null)
            {
                var targetFields = convertToEditableFieldItems(Company.GetFieldNamesByObjectType(loadTypeRule.ObjectType, loadTypeRule.ObjectID).ToList());
                list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "TargetFieldName", Name = "Target Field", FieldType = DataType.Lookup.ToString(), Items = targetFields });            
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LoadTypeRuleItemID</param>
        public JsonResult LoadTypeRuleItem_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LoadTypeRule, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LoadTypeRuleItem>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">LoadTypeRuleItemID</param>
        public JsonResult LoadTypeRuleItem_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.LoadTypeRule, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<LoadTypeRuleItem>(id, i => i.LoadTypeRule);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            var sourceFields = Company.Filter<LoadTypeField>(i => i.LoadTypeID == a.LoadTypeRule.LoadTypeID)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.ID.ToString(), Text = i.Name })
                .ToList();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "SourceLoadTypeFieldID", Name = "Source Field", FieldType = DataType.Lookup.ToString(), Items = sourceFields, Value = a.SourceLoadTypeFieldID.ToString() });

            if (a.LoadTypeRule.ObjectType != null)
            {
                var targetFields = convertToEditableFieldItems(Company.GetFieldNamesByObjectType(a.LoadTypeRule.ObjectType, a.LoadTypeRule.ObjectID).ToList());
                list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "TargetFieldName", Name = "Target Field", FieldType = DataType.Lookup.ToString(), Items = targetFields, Value = string.Format("{0}|{1}", a.TargetFieldName, a.IsCustomField) });
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddLoadTypeRuleItem(int id)
        {
            var model = new EditableForm
            {
                Context = ContextList.LoadTypeRuleItem,
                FieldUri = "/form/LoadTypeRuleItem_AddFields?id=" + id,
                FormTitle = "Add Load Type Rule Item",
                FormUri = "/form/AddLoadTypeRuleItem",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost]
        public JsonResult AddLoadTypeRuleItem(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LoadType, 0, Claim.Create))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("bulk load type rule item");

                var loadTypeRuleID = parseIntField(form, "LoadTypeRuleID");
                var sourceLoadTypeFieldID = parseIntField(form, "SourceLoadTypeFieldID");

                var rule = Company.GetById<LoadTypeRule>(loadTypeRuleID);

                var a = new LoadTypeRuleItem
                {
                    LoadTypeRuleID = loadTypeRuleID,
                    SourceLoadTypeFieldID = sourceLoadTypeFieldID,
                    TargetFieldName = "Name"
                };

                if (rule.ObjectType != null)
                {
                    var split = form["TargetFieldName"].Split('|');
                    a.TargetFieldName = split[0];
                    a.IsCustomField = bool.Parse(split[1]);                
                }

                Company.SaveOrUpdate<LoadTypeRuleItem>(a);

                return jsonSuccess("Rule Item successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteLoadTypeRuleItem(int id)
        {
            var a = Company.GetById<LoadTypeRuleItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LoadTypeRuleItem,
                FieldUri = string.Format("/form/LoadTypeRuleItem_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "Rule Item"),
                FormUri = "/form/DeleteLoadTypeRuleItem",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteLoadTypeRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type rule item");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadTypeRuleItem>(id);
                if (model == null) throw new NotFoundException("bulk load type rule item");

                if (!Company.HasPermission(SystemObjects.LoadType, id, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete<LoadTypeRuleItem>(model);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditLoadTypeRuleItem(int id)
        {
            var a = Company.GetById<LoadTypeRuleItem>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.LoadTypeRuleItem,
                FieldUri = string.Format("/form/LoadTypeRuleItem_EditFields?id={0}", id),
                FormTitle = "Edit Rule Item",
                FormUri = "/form/EditLoadTypeRuleItem",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut]
        public JsonResult EditLoadTypeRuleItem(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("bulk load type rule item");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<LoadTypeRuleItem>(id, i => i.LoadTypeRule);
                if (model == null) throw new NotFoundException("bulk load type rule item");

                if (!Company.HasPermission(SystemObjects.LoadTypeRule, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var sourceLoadTypeFieldID = parseIntField(form, "SourceLoadTypeFieldID");
                model.SourceLoadTypeFieldID = sourceLoadTypeFieldID;

                if (model.LoadTypeRule.ObjectType != null)
                {
                    var split = form["TargetFieldName"].Split('|');
                    model.TargetFieldName = split[0];
                    model.IsCustomField = bool.Parse(split[1]);                
                }

                Company.SaveOrUpdate<LoadTypeRuleItem>(model);

                return jsonSuccess("Rule Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusMessage, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Lookup, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, model.LookupTypeID).ToList(), form, Server);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region Policy

        #region Field Generation

        public JsonResult Policy_AddFields(int? parentID)
        {
            var model = new Policy();
            if (!Company.HasPermission(SystemObjects.Policy, 0, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            if (parentID.HasValue) list.Add(new EditableField { FieldName = "ParentID", FieldType = DataType.Hidden.ToString(), Value = parentID.Value.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PolicyID</param>
        public JsonResult Policy_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Policy, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            if (Company.Table<Rule>().Any(i => i.PolicyID == id))
                return jsonException(FormInfo.Policy_Error_Delete_ExistingRulesPresent, HttpStatusCode.Forbidden);

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

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddPolicy(int? parentID)
        {
            var model = new EditableForm
            {
                Context = ContextList.Policy,
                FieldUri = "/form/Policy_AddFields" + ((parentID.HasValue) ? "?parentID=" + parentID.Value : ""),
                FormTitle = Resources.FormInfo.Add_Policy_Title,
                FormDescription = Resources.FormInfo.Add_Policy_Directions,
                FormUri = "/form/AddPolicy",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

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
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description")
                };

                if (!string.IsNullOrEmpty(form["ParentID"]))
                {
                    model.ParentID = parseIntField(form, "ParentID");
                    if (model.ParentID == 0) model.ParentID = null;
                }

                Company.Add<Policy>(model);

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                
                Company.Update<Policy>(model);

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
            ) O
order by    Text

").ToList();
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
                Report = new Report { }
            };
            loadReportEditorModel(o);
            return PartialView("ReportEditForm", o);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddReport(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var objectType = form["ObjectType"].Split('|').ToArray();

                if (objectType.Length == 2)
                {
                    var model = new Report
                    {
                        Name = parseTextField(form, "Name"),
                        Description = parseTextField(form, "Description"),
                        ObjectType = objectType[0],
                        ObjectID = int.Parse(objectType[1]),
                        ReportLayoutID = parseIntField(form, "ReportLayoutID"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                Report = o
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
        public JsonResult EditReport(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Report>(id);

                if (model == null) throw new NotFoundException(Resources.FormInfo.NoFormData_FieldType);

                // Static fields
                var objectType = form["ObjectType"].Split('|').ToArray();

                if (objectType.Length == 2)
                {
                    model.Name = parseTextField(form, "Name");
                    model.Description = parseTextField(form, "Description");
                    model.ObjectType = objectType[0];
                    model.ObjectID = int.Parse(objectType[1]);
                    model.ReportLayoutID = parseIntField(form, "ReportLayoutID");

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                ReportBaseUri = SecProvider.RawCompanyID,
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

        [HttpPost, ValidateInput(false)]
        public JsonResult AddReportTile(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(Resources.FormInfo.NoFormData_FieldType);

                var model = new ReportTile 
                {
                    Name = parseTextField(form, "Name"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                ReportBaseUri = SecProvider.RawCompanyID,
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
                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region Responsibility

        #region Field Generation

        //public JsonResult PeopleResponsibility_AddFields(SystemObjects type, int id)//, int responsibilityTypeID)
        //{
        //    //if (type == SystemObjects.Artifact)
        //    //{
        //    //    if (!Company.GetAllowedResponsibilityTypesByObject(type, id).Any(i => i.ID == responsibilityTypeID))
        //    //        return jsonException("You may not assign any responsibilities to this item.  All responsibilities must be defined elsewhere.", HttpStatusCode.Forbidden);                
        //    //}

        //    var list = new List<EditableField>();

        //    list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
        //    list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

        //    var responsibilityTypes = Company.GetAllowedResponsibilityTypesByObject(type, id)
        //        .Where(i => i.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People)
        //        .OrderBy(i => i.Name)
        //        .Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() })
        //        .ToList();
        //    list.Add(new EditableField { Row = 1, Column = 1, FieldName = "ResponsibilityTypeID", Name = "Responsibility Type ", FieldType = DataType.Lookup.ToString(), Items = responsibilityTypes });
        //    //list.Add(new EditableField { FieldName = "ResponsibilityTypeID", FieldType = DataType.Hidden.ToString(), Value = responsibilityTypeID.ToString() });

        //    var resList = GetCompanyResources()
        //        .Select(i => new { ID = i.ID, i.FirstName, i.LastName })
        //        .ToList()
        //        .Select(i => new SelectListItem
        //        {
        //            Text = string.Format("{0}, {1}", i.LastName, i.FirstName),
        //            Value = string.Format("Resource|{0}", i.ID)
        //        })
        //        .ToList();
        //    resList.AddRange(
        //        Company.Table<Group>()
        //        .Select(i => new { i.ID, i.Name })
        //        .ToList()
        //        .Select(i => new SelectListItem
        //        {
        //            Text = i.Name,
        //            Value = string.Format("Group|{0}", i.ID)
        //        }).ToList()
        //    );
        //    resList.Insert(0, new SelectListItem { Text = "Please select", Value = "" });
        //    list.Add(new EditableField { Row = 2, Column = 1, FieldName = "ResponsibleObject", Name = "Responsible Party", FieldType = DataType.Lookup.ToString(), Items = resList });

        //    var contexts = (
        //        from l in Company.GetMasterLists().Where(i => i.Items.Count > 0)
        //        from i in l.Items
        //        orderby l.DomainType.Name
        //        orderby l.Name
        //        select new { DomainType = l.DomainType.Name, List = l.Name, i.Code, i.Name, i.ID })
        //        .ToList()
        //        .Select(i => new SelectListItem
        //        {
        //            Group = string.Format("{0} : {1}", i.DomainType, i.List),
        //            Text = string.Format("{0} : {1}", i.Code, i.Name),
        //            Value = i.ID.ToString()
        //        }).OrderBy(i => i.Group).ThenBy(i => i.Text).ToList();

        //    list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Context", Name = "Context", MultiSelect = true, FieldType = DataType.Lookup.ToString(), Items = contexts });

        //    if (type.ToString().EndsWith("Type"))
        //        list.Add(new EditableField { Row = 4, Column = 1, FieldName = "Visible", Name = "Is Visible?", FieldDescription = "This responsibility is displayed to the user.", FieldType = DataType.Boolean.ToString(), Value = "false" });
        //    else
        //        list.Add(new EditableField { FieldName = "Visible", FieldType = DataType.Hidden.ToString(), Value = "true" });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        //public JsonResult SourcingResponsibility_AddFields(SystemObjects type, int id)//, int responsibilityTypeID)
        //{
        //    var list = new List<EditableField>();

        //    list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
        //    list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
        //    //list.Add(new EditableField { FieldName = "ResponsibilityTypeID", FieldType = DataType.Hidden.ToString(), Value = responsibilityTypeID.ToString() });

        //    var responsibilityTypes = Company.GetAllowedResponsibilityTypesByObject(type, id)
        //        .Where(i => i.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People)
        //        .OrderBy(i => i.Name)
        //        .Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() })
        //        .ToList();
        //    list.Add(new EditableField { Row = 1, Column = 1, FieldName = "ResponsibilityTypeID", Name = "Responsibility Type ", FieldType = DataType.Lookup.ToString(), Items = responsibilityTypes });

        //    var artifacts = (
        //                    from a in Company.Table<Artifact>()
        //                    join rt in Company.Filter<ResponsibilityTypeSourceType>(i => i.ResponsibilityTypeID == responsibilityTypeID) on a.ArtifactTypeID equals rt.ObjectID
        //                    join t in Company.Table<ArtifactType>() on rt.ObjectID equals t.ID
        //                    orderby t.Name
        //                    orderby a.Name
        //                    select new SelectListItem
        //                    {
        //                        Group = t.Name,
        //                        Text = a.Name,
        //                        Value = a.ID.ToString()
        //                    }
        //                    ).OrderBy(i => i.Group).ThenBy(i => i.Text).ToList();
        //    var contexts = (
        //                   from l in Company.GetMasterLists().Where(i => i.Items.Count > 0)
        //                   from i in l.Items
        //                   orderby l.DomainType.Name
        //                   orderby l.Name
        //                   select new { DomainType = l.DomainType.Name, List = l.Name, i.Code, i.Name, i.ID })
        //                   .ToList()
        //                   .Select(i => new SelectListItem
        //                   {
        //                       Group = string.Format("{0} : {1}", i.DomainType, i.List),
        //                       Text = string.Format("{0} : {1}", i.Code, i.Name),
        //                       Value = i.ID.ToString()
        //                   }).OrderBy(i => i.Group).ThenBy(i => i.Text).ToList();

        //    list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Artifact", Name = "Artifact to Source From", Required = true, FieldType = DataType.Lookup.ToString(), Items = artifacts });
        //    list.Add(new EditableField { Row = 1, Column = 2, FieldName = "Context", Name = "Contexts", Required = true, MultiSelect = true, FieldType = DataType.Lookup.ToString(), Items = contexts });

        //    if (type != SystemObjects.Intersect)
        //    {
        //        list.Add(new EditableField { Row = 2, Column = 1, FieldName = "BusinessTransformation", Name = "Business Transformation", Required = false, FieldType = DataType.Html.ToString() });
        //        list.Add(new EditableField { Row = 2, Column = 2, FieldName = "TechnicalTransformation", Name = "Technical Transformation", Required = false, FieldType = DataType.Html.ToString() });
        //    }

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        /// <param name="id">ResponsibilityID</param>
        public JsonResult Responsibility_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Responsibility>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        ///// <param name="id">ResponsibilityID</param>
        //public JsonResult PeopleResponsibility_EditFields(int id)
        //{
        //    var list = new List<EditableField>();
        //    var a = Company.GetById<Responsibility>(id);

        //    list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
        //    list.Add(
        //        new EditableField
        //        {
        //            Row = 1,
        //            Column = 1,
        //            FieldName = "ResponsibilityTypeID",
        //            Name = "Responsibility",
        //            FieldType = DataType.Lookup.ToString(),
        //            Items = Company.Filter<ResponsibilityType>(i => i.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People).Select(i => new { i.ID, i.Name }).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList(),
        //            Value = a.ResponsibilityTypeID.ToString()
        //        }
        //    );

        //    var selectedContexts = Company.Filter<ResponsibilityContextItem>(i => i.ResponsibilityID == id).Select(i => i.ObjectID).ToList();
        //    var contexts = (
        //                   from l in Company.GetMasterLists().Where(i => i.Items.Count > 0)
        //                   from i in l.Items
        //                   orderby l.DomainType.Name
        //                   orderby l.Name
        //                   select new { DomainType = l.DomainType.Name, List = l.Name, i.Code, i.Name, i.ID })
        //                   .ToList()
        //                   .Select(i => new SelectListItem
        //                   {
        //                       Group = string.Format("{0} : {1}", i.DomainType, i.List),
        //                       Text = string.Format("{0} : {1}", i.Code, i.Name),
        //                       Value = i.ID.ToString(),
        //                       Selected = selectedContexts.Contains(i.ID)
        //                   }).OrderBy(i => i.Group).ThenBy(i => i.Text).ToList();
        //    selectedContexts = null;

        //    list.Add(new EditableField { Row = 1, Column = 2, FieldName = "Context", Name = "Context", MultiSelect = true, FieldType = DataType.Lookup.ToString(), Items = contexts });

        //    if (a.ObjectType.ToString().EndsWith("Type"))
        //        list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Visible", Name = "Is Visible?", FieldDescription = "This responsibility is displayed to the user.", FieldType = DataType.Boolean.ToString(), Value = a.Visible.ToString() });
        //    else
        //        list.Add(new EditableField { FieldName = "Visible", FieldType = DataType.Hidden.ToString(), Value = "true" });

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        ///// <param name="id">ResponsibilityID</param>
        //public JsonResult SourcingResponsibility_EditFields(int id)
        //{
        //    var list = new List<EditableField>();
        //    var model = Company.GetById<Responsibility>(id);

        //    list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });

        //    var artifacts = (
        //        from a in Company.Table<Artifact>()
        //        join rt in Company.Filter<ResponsibilityTypeSourceType>(i => i.ResponsibilityTypeID == model.ResponsibilityTypeID) on a.ArtifactTypeID equals rt.ObjectID
        //        join t in Company.Table<ArtifactType>() on rt.ObjectID equals t.ID
        //        orderby t.Name
        //        orderby a.Name
        //        select new SelectListItem
        //        {
        //            Group = t.Name,
        //            Text = a.Name,
        //            Value = a.ID.ToString(),
        //            Selected = (a.ID == model.ResponsibleObjectID)
        //        }
        //    ).OrderBy(i => i.Group).ThenBy(i => i.Text).ToList();
        //    var selectedContexts = Company.Filter<ResponsibilityContextItem>(i => i.ResponsibilityID == model.ID).Select(i => i.ObjectID).ToList();
        //    var contexts = (
        //                   from l in Company.GetMasterLists().Where(i => i.Items.Count > 0)
        //                   from i in l.Items
        //                   orderby l.DomainType.Name
        //                   orderby l.Name
        //                   select new { DomainType = l.DomainType.Name, List = l.Name, i.Code, i.Name, i.ID })
        //                   .ToList()
        //                   .Select(i => new SelectListItem
        //                   {
        //                       Group = string.Format("{0} : {1}", i.DomainType, i.List),
        //                       Text = string.Format("{0} : {1}", i.Code, i.Name),
        //                       Value = i.ID.ToString(),
        //                       Selected = selectedContexts.Contains(i.ID)
        //                   }).OrderBy(i => i.Group).ThenBy(i => i.Text).ToList();
        //    selectedContexts = null;

        //    list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Artifact", Name = "Artifact to Source From", Required = true, FieldType = DataType.Lookup.ToString(), Items = artifacts, Value = model.ResponsibleObjectID.ToString() });
        //    list.Add(new EditableField { Row = 1, Column = 2, FieldName = "Context", Name = "Contexts", Required = true, MultiSelect = true, FieldType = DataType.Lookup.ToString(), Items = contexts });

        //    if (model.ObjectType != "Intersect")
        //    {
        //        var transformations = Company.Filter<ResponsibilityTransformation>(i => i.ResponsibilityID == id).ToList();

        //        list.Add(new EditableField { Row = 2, Column = 1, FieldName = "BusinessTransformation", Name = "Business Transformation", Required = false, FieldType = DataType.Html.ToString(), Value = (transformations.Any(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Business) ? transformations.First(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Business).Description : "") });
        //        list.Add(new EditableField { Row = 2, Column = 2, FieldName = "TechnicalTransformation", Name = "Technical Transformation", Required = false, FieldType = DataType.Html.ToString(), Value = (transformations.Any(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Technical) ? transformations.First(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Technical).Description : "") });
        //    }

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}

        #endregion

        #region Form Get/Post

        void processContextFormFieldForResponsibility(int responsibilityID, FormCollection form, bool isAdding = true)
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

                if (!isAdding)
                    Company.Delete<ResponsibilityContextItem>(i => i.ResponsibilityID == responsibilityID);

                if (contexts.Count > 0)
                {
                    foreach (var o in contexts)
                    {
                        Company.ResponsibilityContextItems.Add(o);
                    }
                    Company.SaveChanges();
                }
            }
        }

        //public ActionResult AddSourcingResponsibility(SystemObjects type, int id)
        //{
        //    var artifacts = (
        //                    from a in Company.Table<Artifact>()
        //                    join rt in Company.Filter<ResponsibilityTypeSourceType>(i => i.ResponsibilityTypeID == 0) on a.ArtifactTypeID equals rt.ObjectID
        //                    join t in Company.Table<ArtifactType>() on rt.ObjectID equals t.ID
        //                    orderby t.Name
        //                    orderby a.Name
        //                    select new SelectListItem
        //                    {
        //                        Group = t.Name,
        //                        Group2 = rt.ResponsibilityTypeID.ToString(),
        //                        Text = a.Name,
        //                        Value = a.ID.ToString()
        //                    }
        //                    ).ToList();
        //    var contexts = (
        //                    from l in Company.GetMasterLists().Where(i => i.Items.Count > 0)
        //                    from i in l.Items
        //                    orderby l.DomainType.Name
        //                    orderby l.Name
        //                    select new { DomainType = l.DomainType.Name, List = l.Name, i.Code, i.Name, i.ID })
        //                    .ToList()
        //                    .Select(i => new SelectListItem
        //                    {
        //                        Group = string.Format("{0} : {1}", i.DomainType, i.List),
        //                        Text = string.Format("{0} : {1}", i.Code, i.Name),
        //                        Value = i.ID.ToString()
        //                    }).ToList();

        //    var oModel = new SourcingResponsibilityEditorModel
        //    {
        //        ObjectID = id,
        //        ObjectType = type,
        //        Artifacts = artifacts,
        //        Contexts = contexts,
        //        ID = 0
        //    };
        //    return PartialView(oModel);
        //}

        //public ActionResult AddResponsibility(int responsibilityTypeID, SystemObjects type, int id, string context = ContextList.Responsibility)
        //{
        //    var responsibilityType = Company.GetById<ResponsibilityType>(responsibilityTypeID);

        //    if (responsibilityType.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People)
        //    {
        //        var pModel = new EditableForm
        //        {
        //            Context = context,
        //            FieldUri = string.Format("/form/PeopleResponsibility_AddFields?type={0}&id={1}&responsibilityTypeID={2}", type.ToString(), id, responsibilityTypeID),
        //            FormTitle = string.Format("Add {0}", responsibilityType.Name),
        //            FormUri = "/form/AddPeopleResponsibility",
        //            FormMethod = "POST"
        //        };

        //        return PartialView("EditableForm", pModel);
        //    }
        //    else
        //    {
        //        var pModel = new EditableForm
        //        {
        //            Context = context,
        //            FieldUri = string.Format("/form/SourcingResponsibility_AddFields?type={0}&id={1}&responsibilityTypeID={2}", type.ToString(), id, responsibilityTypeID),
        //            FormTitle = string.Format("Add {0}", responsibilityType.Name),
        //            FormUri = "/form/AddSourcingResponsibility",
        //            FormMethod = "POST"
        //        };

        //        return PartialView("EditableForm", pModel);
        //    }
        //}

        List<SelectListItem> getArtifactsForSourcing(int responsibilityTypeID, int selectedID = 0)
        {
            return
                (
                Company.Query<dynamic>(@"select	A.ID, A.Name, AT.Name as ArtifactType from Artifact A
inner join ResponsibilityTypeSourceType R on R.ResponsibilityTypeID = @t and R.ObjectID = A.ArtifactTypeID
inner join ArtifactType AT on R.ObjectID = AT.ID
order by AT.Name, A.Name", new { t = responsibilityTypeID }).Select(t => new SelectListItem {
                    Group = new SelectListGroup { Name = t.ArtifactType },
                    Text = t.Name,
                    Value = t.ID.ToString(),
                    Selected = (t.ID == selectedID)
                }).ToList()
                );
        }
        List<SelectListItem> getContextSelectList(List<int> contextIDs = null)
        {
            if (contextIDs == null) contextIDs = new List<int>();

//            var sql = @"select	T.Name + ' : ' + D.Name as [Group], I.Code + ' : ' + I.Name as [Text], I.ID as Value, I.ID  
//from	DomainItem I
//		inner join Domain D on D.ID = I.DomainID
//		inner join DomainType T on T.ID = D.DomainTypeID
//		inner join DomainGroup G on G.MasterListID = D.ID
//order by	T.Name, D.Name, I.Code, I.Name";

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

        List<SelectListItem> getSourceResponsibilitiesSelectList(string type, int id, int? selectedID = null)
        {
            var models = Company.Filter<SourcingResponsibilityDetail>(i => i.ObjectType == type && i.ObjectID == id).OrderBy(i => i.Role).ThenBy(i => i.ResponsibleObjectName).ToList()
                .Select(i => new SelectListItem
                {
                    Text = string.Format("{0} : {1}", i.Role, i.ResponsibleObjectName),
                    Value = i.ResponsibilityID.ToString(),
                    Selected = (i.ResponsibilityID == selectedID)
                })
                .ToList();

            models.Insert(0, new SelectListItem { Text = "None", Value = "" });

            return models;
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
                .Where(i => i.ID > 0)
                .Select(i => new { ID = i.ID, i.FirstName, i.LastName })
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

        public ActionResult AddPeopleResponsibility(SystemObjects type, int id)
        {
            var model = new PeopleResponsibilityEditorModel
            {
                FormName = string.Format("Add Responsibility"),
                FormUri = "/form/AddPeopleResponsibility",
                FormMethod = "POST",
                Contexts = getContextSelectList(),
                FormDescription = "",
                Resources = getResponsibilityResources(),
                ResponsibilityTypes = getResponsibilityTypeSelectList(type, id, ResponsibilityTypeGroup.People),
                Responsibility = new Responsibility { ObjectType = type.ToString(), ObjectID = id, Visible = true }
            };

            return PartialView("PeopleResponsibilityEditForm", model);
        }
        
        [HttpPost]
        public JsonResult AddPeopleResponsibility(FormCollection form)
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult AddSourcingResponsibility(SystemObjects type, int id)
        {
            var model = new SourcingResponsibilityEditorModel
            {
                FormName = string.Format("Add Source"),
                FormUri = "/form/AddSourcingResponsibility",
                FormMethod = "POST",
                Contexts = getContextSelectList(),
                FormDescription = "", 
                Responsibility = new Responsibility { ObjectType = type.ToString(), ObjectID = id, Visible = true }
            };

            if (type == SystemObjects.Intersect)
            {
                model.ResponsibilityTypes = new List<SelectListItem>();
                model.Artifacts = getArtifactsForSourcing(0);
            }
            else 
            {
                model.ResponsibilityTypes = getResponsibilityTypeSelectList(type, id, ResponsibilityTypeGroup.Sourcing);
                model.SourceResponsibilities = getSourceResponsibilitiesSelectList(type.ToString(), id);
            }

            return PartialView("SourcingResponsibilityEditForm", model);
        }
        
        [HttpPost, ValidateInput(false)]
        public JsonResult AddSourcingResponsibility(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("responsibility");

                var objectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), form["ObjectType"]);
                var responsiblePartyID = parseIntField(form, "ResponsibleObject");
                var o = new Responsibility
                {
                    ResponsibilityTypeID = parseIntField(form, "ResponsibilityType"),
                    ObjectType = objectType.ToString(),
                    ObjectID = parseIntField(form, "ObjectID"),
                    ResponsibleObjectType = "Artifact",
                    ResponsibleObjectID = responsiblePartyID,
                    Visible = true,
                    TargetResponsibilityID =  parseNullableIntField(form, "TargetResponsibility")
                };

                if (!Company.Table<Responsibility>().Any(i => 
                    i.ObjectType == o.ObjectType && 
                    i.ObjectID == o.ObjectID && 
                    i.ResponsibilityTypeID == o.ResponsibilityTypeID && 
                    i.ResponsibleObjectType == o.ResponsibleObjectType && 
                    i.ResponsibleObjectID == o.ResponsibleObjectID)
                    )
                {
                    Company.Add<Responsibility>(o);

                    processContextFormFieldForResponsibility(o.ID, form);

                    Company.Update<Responsibility>(o);  //Call this again so we can re-cache via trigger.

                    try
                    {
                        if (objectType != SystemObjects.Intersect)
                        {
                            var bt = parseTextField(form, "BusinessTransformation");
                            if (!string.IsNullOrEmpty(bt) && bt != "<p></p>")
                            {
                                var brt = new ResponsibilityTransformation { Description = bt, ResponsibilityID = o.ID, ResponsibilityTransformationType = ResponsibilityTransformationType.Business };
                                Company.Add<ResponsibilityTransformation>(brt);
                            }

                            var tt = parseTextField(form, "TechnicalTransformation");
                            if (!string.IsNullOrEmpty(tt) && tt != "<p></p>")
                            {
                                var trt = new ResponsibilityTransformation { Description = tt, ResponsibilityID = o.ID, ResponsibilityTransformationType = ResponsibilityTransformationType.Technical };
                                Company.Add<ResponsibilityTransformation>(trt);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SendException(ex);
                    }

                    #region Now figure out the object to create an intersect for (if required)

                    if (objectType == SystemObjects.Intersect)
                    { 
                        // Figure out which side to relate to the responsible object.
                        var intersect = Company.GetById<Intersect>(o.ObjectID, i => i.Nodes, i => i.IntersectType.Nodes);
                        if (intersect != null)
                        {
                            var sourcingTypeSide = intersect.IntersectType.Nodes.FirstOrDefault(i => i.Order == 2);
                            if (sourcingTypeSide != null)
                            {
                                var sourcingSide = intersect.Nodes.FirstOrDefault(i => i.IntersectTypeNodeID == sourcingTypeSide.ID);
                                if (sourcingSide != null)
                                {
                                    var objs = new List<ObjectModel>();
                                    objs.Add(new ObjectModel { ObjectType = sourcingSide.ObjectType, ObjectID = sourcingSide.ObjectID });
                                    var classification = intersect.Classification.HasValue ? intersect.Classification.Value : IntersectClassification.Normal;
                                    var description = intersect.Description + "";
                                    Company.AddRelationship(
                                        SystemObjects.Artifact, responsiblePartyID, 
                                        (SystemObjects)Enum.Parse(typeof(SystemObjects), sourcingSide.ObjectType), sourcingSide.ObjectID, 
                                        classification, null, description);

                                    sourcingSide = null;
                                }
                                sourcingTypeSide = null;
                            }
                        
                            intersect = null;
                        }
                    }

                    #endregion
                }

                return jsonSuccess("Item successfully created.", o.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, new { ObjectType = o.ObjectType.ToString(), ObjectID = o.ObjectID.ToString() });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteResponsibility(int id)
        {
            var responsibility = Company.GetById<Responsibility>(id, i => i.ResponsibilityType);

            var context = (responsibility.ResponsibilityType.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People) ? ContextList.PeopleResponsibility : ContextList.SourcingResponsibility;
            if (responsibility.ObjectType == "Intersect") context = ContextList.IntersectSourcingResponsibility;

            var model = new EditableForm
            {
                Context = context,
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult EditPeopleResponsibility(int id)
        {
            var r = Company.GetById<Responsibility>(id, i => i.ResponsibilityType, i => i.ResponsibilityContextItems);
            if (r == null) return HttpNotFound();

            var model = new PeopleResponsibilityEditorModel
            {
                FormName = "Edit Responsibility",
                FormUri = "/form/EditPeopleResponsibility",
                FormMethod = "PUT", 
                Contexts = getContextSelectList(r.ResponsibilityContextItems.Select(i => i.ObjectID).ToList()),
                FormDescription = "",
                Resources = getResponsibilityResources(string.Format("{0}|{1}", r.ResponsibleObjectType, r.ResponsibleObjectID)),
                Responsibility = r,
                ResponsibilityTypes = getResponsibilityTypeSelectList((SystemObjects)Enum.Parse(typeof(SystemObjects), r.ObjectType), r.ObjectID, ResponsibilityTypeGroup.People, r.ResponsibilityTypeID)
            };

            return PartialView("PeopleResponsibilityEditForm", model);
        }

        [HttpPut]
        public JsonResult EditPeopleResponsibility(FormCollection form)
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public JsonNetResult SourcesByResponsibilityType(int id)
        {
            return new JsonNetResult { Data = getArtifactsForSourcing(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        public ActionResult EditSourcingResponsibility(int id)
        {
            var r = Company.GetById<Responsibility>(id, i => i.ResponsibilityType, i => i.ResponsibilityContextItems, i => i.ResponsibilityTransformations);
            if (r == null) return HttpNotFound();

            var model = new SourcingResponsibilityEditorModel
            {
                FormName = "Edit Source",
                FormUri = "/form/EditSourcingResponsibility",
                FormMethod = "PUT",
                Artifacts = getArtifactsForSourcing(r.ResponsibilityTypeID, r.ResponsibleObjectID),
                Contexts = getContextSelectList(r.ResponsibilityContextItems.Select(i => i.ObjectID).ToList()),
                FormDescription = "",
                Responsibility = r,
                ResponsibilityTypes = getResponsibilityTypeSelectList((SystemObjects)Enum.Parse(typeof(SystemObjects), r.ObjectType), r.ObjectID, ResponsibilityTypeGroup.Sourcing, r.ResponsibilityTypeID),
                SourceResponsibilities = getSourceResponsibilitiesSelectList(r.ObjectType, r.ObjectID, r.TargetResponsibilityID)
        };

            if (r.ResponsibilityTransformations.Count > 0)
            {
                var bt = r.ResponsibilityTransformations.FirstOrDefault(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Business);
                if (bt != null)
                {
                    model.BusinessTransformation = bt.Description;
                }

                var tt = r.ResponsibilityTransformations.FirstOrDefault(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Technical);
                if (tt != null)
                {
                    model.TechnicalTransformation = tt.Description;
                }
            }

            return PartialView("SourcingResponsibilityEditForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditSourcingResponsibility(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("responsibility");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Responsibility>(id);
                if (model == null) throw new NotFoundException("responsibility");

                model.ResponsibleObjectID = parseIntField(form, "ResponsibleObject");
                model.TargetResponsibilityID = parseNullableIntField(form, "TargetResponsibility");

                processContextFormFieldForResponsibility(id, form, false);

                Company.Update<Responsibility>(model);  //Do this after context so the trigger will properly re-cache with the contextxs.

                try
                {
                    if (model.ObjectType != SystemObjects.Intersect.ToString())
                    {
                        var transformations = Company.Filter<ResponsibilityTransformation>(i => i.ResponsibilityID == id).ToList();
                        var bt = parseTextField(form, "BusinessTransformation");
                        ResponsibilityTransformation brt = transformations.FirstOrDefault(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Business);
                        if (bt != "<p></p>" && !string.IsNullOrEmpty(bt))
                        {
                            if (brt == null)
                            {
                                brt = new ResponsibilityTransformation { Description = bt, ResponsibilityID = id, ResponsibilityTransformationType = ResponsibilityTransformationType.Business };
                                Company.Add<ResponsibilityTransformation>(brt);
                            }
                            else 
                            {
                                brt.Description = bt;
                                Company.Update<ResponsibilityTransformation>(brt);
                            }
                        }
                        else
                        {
                            if (brt != null) Company.Delete<ResponsibilityTransformation>(brt);
                        }

                        var tt = parseTextField(form, "TechnicalTransformation");
                        ResponsibilityTransformation trt = transformations.FirstOrDefault(i => i.ResponsibilityTransformationType == ResponsibilityTransformationType.Technical);
                        if (tt != "<p></p>" && !string.IsNullOrEmpty(tt))
                        {
                            if (trt == null)
                            {
                                trt = new ResponsibilityTransformation { Description = tt, ResponsibilityID = id, ResponsibilityTransformationType = ResponsibilityTransformationType.Technical };
                                Company.Add<ResponsibilityTransformation>(trt);
                            }
                            else
                            {
                                trt.Description = tt;
                                Company.Update<ResponsibilityTransformation>(trt);
                            }
                        }
                        else 
                        {
                            if (trt != null) Company.Delete<ResponsibilityTransformation>(trt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SendException(ex);
                }

                return jsonSuccess("Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, new { ObjectType = model.ObjectType.ToString(), ObjectID = model.ObjectID.ToString() });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region ResponsibilityTransformation

        #region Field Generation

        public JsonResult ResponsibilityTransformation_AddFields(int responsibilityID)
        {
            var model = new ResponsibilityTransformation();
            //if (!Company.HasPermission(SystemObjects.Rule, 0, Claim.Create, ClaimObject.Root))
            //    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "ResponsibilityID", FieldType = DataType.Hidden.ToString(), Value = responsibilityID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ResponsibilityTransformationType", Name = model.GetName(i => i.ResponsibilityTransformationType), FieldDescription = model.GetDescription(i => i.ResponsibilityTransformationType), Items = ResponsibilityTransformationType.Business.GetEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList(), FieldType = DataType.Lookup.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResponsibilityTransformationID</param>
        public JsonResult ResponsibilityTransformation_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResponsibilityTransformationID</param>
        public JsonResult ResponsibilityTransformation_EditFields(int id)
        {
            //if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update))
            //    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var model = Company.GetById<ResponsibilityTransformation>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ResponsibilityTransformationType", Name = model.GetName(i => i.ResponsibilityTransformationType), FieldDescription = model.GetDescription(i => i.ResponsibilityTransformationType), Items = ResponsibilityTransformationType.Business.GetEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList(), FieldType = DataType.Lookup.ToString(), Value = ((int)model.ResponsibilityTransformationType).ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = model.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddResponsibilityTransformation(int responsibilityID)
        {
            var model = new EditableForm
            {
                Context = ContextList.ResponsibilityTransformation,
                FieldUri = "/form/ResponsibilityTransformation_AddFields?responsibilityID=" + responsibilityID,
                FormTitle = Resources.FormInfo.Add_ResponsibilityTransformation_Title,
                FormDescription = Resources.FormInfo.Add_ResponsibilityTransformation_Directions,
                FormUri = "/form/AddResponsibilityTransformation",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddResponsibilityTransformation(FormCollection form)
        {
            try
            {
                //if (!Company.HasPermission(SystemObjects.Rule, 0, Claim.Create, ClaimObject.Root))
                //    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("Responsibility Transformation");

                var model = new ResponsibilityTransformation
                {
                    ResponsibilityTransformationType = (ResponsibilityTransformationType)Enum.Parse(typeof(ResponsibilityTransformationType), form["ResponsibilityTransformationType"]),
                    Description = parseTextField(form, "Description"),
                    ResponsibilityID = parseIntField(form, "ResponsibilityID")
                };

                Company.Add<ResponsibilityTransformation>(model);

                dynamic custom = new
                {
                    action = "add",
                    Context = form["_context"]
                };

                return jsonSuccess("Transformation successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        public ActionResult DeleteResponsibilityTransformation(int id)
        {
            var a = Company.GetById<ResponsibilityTransformation>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResponsibilityTransformation,
                FieldUri = string.Format("/form/ResponsibilityTransformation_DeleteFields?id={0}", id),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "Transformation"),
                FormUri = "/form/DeleteResponsibilityTransformation",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteResponsibilityTransformation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Responsibility Transformation");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ResponsibilityTransformation>(id);
                if (model == null) throw new NotFoundException("Responsibility Transformation");

                //if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Delete))
                //    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

                dynamic custom = new
                {
                    action = "delete",
                    Context = form["_context"]
                };

                return jsonSuccess("Transformation successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditResponsibilityTransformation(int id)
        {
            if (!Company.Exists<ResponsibilityTransformation>(id)) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResponsibilityTransformation,
                FieldUri = string.Format("/form/ResponsibilityTransformation_EditFields?id={0}", id),
                FormTitle = Resources.FormInfo.Edit_ResponsibilityTransformation_Title,
                FormDescription = Resources.FormInfo.Edit_ResponsibilityTransformation_Directions,
                FormUri = "/form/EditResponsibilityTransformation",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResponsibilityTransformation(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("Responsibility Transformation");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ResponsibilityTransformation>(id);
                if (model == null) throw new NotFoundException("Responsibility Transformation");

                //if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update))
                //    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Description = parseTextField(form, "Description");
                model.ResponsibilityTransformationType = (ResponsibilityTransformationType)Enum.Parse(typeof(ResponsibilityTransformationType), form["ResponsibilityTransformationType"]);

                Company.Update<ResponsibilityTransformation>(model);

                dynamic custom = new
                {
                    action = "edit",
                    Context = form["_context"]
                };

                return jsonSuccess("Transformation successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK, custom);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region ResponsibilityType

        #region Field Generation

        public JsonResult ResponsibilityType_AddFields(ResponsibilityTypeGroup Group)
        {
            var list = new List<EditableField>();
            var o = new ResponsibilityType();
            var row = 1;

            list.Add(new EditableField { FieldName = "ResponsibilityTypeGroup", FieldType = DataType.Hidden.ToString(), Value = ((int)Group).ToString() });
            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "Name", Name = Resources.FieldInfo.Name_Name, FieldType = DataType.Text.ToString() });
            row++;

            list.Add(new EditableField { Row = row, Column = 1, FieldName = "AllocationType", Name = Resources.FieldInfo.ResponsibilityAllocatedTo_Name, FieldType = DataType.Lookup.ToString(), MultiSelect = true, Items = Company.GetAvailableAllocationPossibilities().Select(i => new SelectListItem { Text = i.Name, Value = string.Format("{0}|{1}", i.ObjectType, i.ObjectTypeID) }).ToList() });
            row++;

            if (Group == ResponsibilityTypeGroup.Sourcing)
            {
                list.Add(new EditableField { Row = row, Column = 1, FieldName = "SourceType", Name = Resources.FieldInfo.ResponsibilitySourcedFrom_Name, FieldType = DataType.Lookup.ToString(), MultiSelect = true, Items = Company.Table<ArtifactType>().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });
                row++;
            }
            list.Add(new EditableField { Row = row, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

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
            var row = 1;

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = row, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name });
            row++;

            var selectedAllocations = Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id).ToList();
            var allocations = Company
                .GetAvailableAllocationPossibilities()
                .Select(i => new SelectListItem { 
                    Text = i.Name, 
                    Value = string.Format("{0}|{1}", i.ObjectType, i.ObjectTypeID),
                    Selected = selectedAllocations.Any(c => c.ObjectType == i.ObjectType && c.ObjectID == i.ObjectTypeID)
                }).ToList();
            list.Add(new EditableField { Row = row, Column = 1, FieldName = "AllocationType", Name = Resources.FieldInfo.ResponsibilityAllocatedTo_Name, FieldType = DataType.Lookup.ToString(), MultiSelect = true, Items = allocations });
            row++;

            if (a.ResponsibilityTypeGroup == ResponsibilityTypeGroup.Sourcing)
            {
                var selectedArtifactTypes = Company.Filter<ResponsibilityTypeSourceType>(i => i.ResponsibilityTypeID == a.ID).Select(i => i.ObjectID).ToList();
                var artifactTypes = (
                                                    from i in Company.Table<ArtifactType>()
                                                    select new SelectListItem
                                                    {
                                                        Text = i.Name,
                                                        Value = i.ID.ToString(),
                                                        Selected = selectedArtifactTypes.Contains(i.ID)
                                                    }
                                                    ).ToList();
                selectedArtifactTypes = null;
                list.Add(new EditableField
                {
                    Row = row,
                    Column = 1,
                    FieldName = "SourceType",
                    Name = Resources.FieldInfo.ResponsibilitySourcedFrom_Name,
                    FieldType = DataType.Lookup.ToString(),
                    MultiSelect = true,
                    Items = artifactTypes
                });
                row++;
            }
            
            list.Add(new EditableField { Row = row, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

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

        [HttpPost, ValidateInput(false)]
        public JsonResult AddResponsibilityType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ownership type");

                var a = new ResponsibilityType
                {
                    Name = parseTextField(form, "Name"),
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
                    Company.ResponsibilityTypeRelations.Add(r);
                }
                Company.SaveChanges();
                
                if (a.ResponsibilityTypeGroup == ResponsibilityTypeGroup.Sourcing)
                {
                    Company.AddSourceTypesToResponsibilityType(a.ID, form["SourceType"].Split(',').Select(i => new ObjectModel { ObjectID = int.Parse(i), ObjectType = SystemObjects.ArtifactType.ToString() }).ToList());
                }

                return jsonSuccess("Item successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResponsibilityType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("ownership type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ResponsibilityType>(id);
                if (model == null) throw new NotFoundException("ownership type");

                model.Name = parseTextField(form, "Name");
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
                    Company.ResponsibilityTypeRelations.Add(r);
                }
                Company.SaveChanges();

                if (model.ResponsibilityTypeGroup == ResponsibilityTypeGroup.Sourcing)
                {
                    Company.EditSourceTypesForResponsibilityType(id, form["SourceType"].Split(',').Select(i => new ObjectModel { ObjectID = int.Parse(i), ObjectType = SystemObjects.ArtifactType.ToString() }).ToList());
                }

                return jsonSuccess("Item successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
        //        return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
        //        return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
        //        return jsonException(ex.Message, HttpStatusCode.InternalServerError);
        //    }
        //}

        #endregion

        #endregion

        #region ResponsibilityTypeHierarchy

        #region Field Generation

        public JsonResult ResponsibilityTypeHierarchy_AddFields()
        {
            var o = new AttributeTypeCategory();
            if (!Company.HasPermission(SystemObjects.ResponsibilityType, 0, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var models = Company.Filter<ResponsibilityType>(i => i.ResponsibilityTypeGroup == ResponsibilityTypeGroup.Sourcing).OrderBy(i => i.Name).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
            models.Insert(0, new SelectListItem { Text = "None", Value = "" });

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Start", Name = "Start", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = models });
            list.Add(new EditableField { Row = 1, Column = 2, FieldName = "End", Name = "End", FieldDescription = "", FieldType = DataType.Lookup.ToString(), Items = models });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResponsibilityTypeHierarchy ID</param>
        public JsonResult ResponsibilityTypeHierarchy_DeleteFields(int s, int? e)
        {
            if (!Company.HasPermission(SystemObjects.ResponsibilityType, 0, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "Start", FieldType = DataType.Hidden.ToString(), Value = s.ToString() });
            list.Add(new EditableField { FieldName = "End", FieldType = DataType.Hidden.ToString(), Value = ((e.HasValue) ? e.Value.ToString() : "") });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddResponsibilityTypeHierarchy()
        {
            var model = new EditableForm
            {
                Context = ContextList.ResponsibilityTypeHierarchy,
                FieldUri = "/form/ResponsibilityTypeHierarchy_AddFields",
                FormTitle = "Add a Responsibility Type Order",
                FormDescription = "Add an order between two sourcing responsibility types to indicate the direction or flow of data.",
                FormUri = "/form/AddResponsibilityTypeHierarchy",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddResponsibilityTypeHierarchy(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.AttributeTypeCategory, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("responsibility type hierarchy");

                var start = parseIntField(form, "Start");
                var end = parseNullableIntField(form, "End");

                var parameters = new List<System.Data.SqlClient.SqlParameter>();
                parameters.Add(new System.Data.SqlClient.SqlParameter("s", System.Data.SqlDbType.Int, start));
                if (end.HasValue)
                {
                    Company.ExecuteNonQueryCommand(string.Format(@"insert into ResponsibilityTypeHierarchy values ({0}, {1})", start, end.Value), new List<System.Data.SqlClient.SqlParameter>());
                }
                else
                {
                    Company.ExecuteNonQueryCommand(string.Format(@"insert into ResponsibilityTypeHierarchy values ({0}, null)", start), new List<System.Data.SqlClient.SqlParameter>());
                }

                return jsonSuccess("Order successfully created.", "0", form["_context"], "add", HttpStatusCode.Created, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteResponsibilityTypeHierarchy(int s, int? e)
        {
            var model = new EditableForm
            {
                Context = ContextList.ResponsibilityTypeHierarchy,
                FieldUri = string.Format("/form/ResponsibilityTypeHierarchy_DeleteFields?s={0}&e={1}", s, e),
                FormTitle = "Remove this Responsibility Type Order",
                FormUri = "/form/DeleteResponsibilityTypeHierarchy",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteResponsibilityTypeHierarchy(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ResponsibilityType, 0, Claim.Delete))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var start = parseIntField(form, "Start");
                var end = parseNullableIntField(form, "End");

                if (end.HasValue)
                {
                    Company.ExecuteNonQueryCommand(string.Format(@"delete ResponsibilityTypeHierarchy where ID = {0} and ParentID = {1}", start, end.Value), new List<System.Data.SqlClient.SqlParameter>());
                }
                else
                {
                    Company.ExecuteNonQueryCommand(string.Format(@"delete ResponsibilityTypeHierarchy where ID = {0} and ParentID is null", start), new List<System.Data.SqlClient.SqlParameter>());
                }

                return jsonSuccess("Item successfully removed.", "0", form["_context"], "delete", HttpStatusCode.OK, new { });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditResponsibilityTypeClaims(SystemObjects type, int id, int responsibilityTypeID)
        {
            var sType = type.ToString();
            var model = new ClaimsMatrixEditorModel 
            { 
                Items = Company.Filter<ResponsibilityTypeObjectClaim>(i => i.ObjectID == id && i.ObjectType == sType && i.ResponsibilityTypeID == responsibilityTypeID)
                .Select(i => new ClaimsMatrixEditorItemModel { Claim = i.Claim, ClaimObject = i.ClaimObject, ID = i.ID})
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
                        Company.ResponsibilityTypeObjectClaims.Add(nc);
                    }
                }
                // Remove old that are no longer present.
                foreach (var ec in existingClaims)
                {
                    if (!list.Any(i => i.ClaimObject == ec.ClaimObject && i.Claim == ec.Claim))
                    {
                        Company.ResponsibilityTypeObjectClaims.Remove(ec);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region QuestionType

        #region Field Generation

        /// <param name="id">SurveyTypeID</param>
        public JsonResult QuestionType_AddFields(int id)
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "SurveyTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ResponseTypeID", Name = "Response Type", FieldType = DataType.Lookup.ToString(), Items = Company.Table<ResponseType>().OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">QuestionTypeID</param>
        public JsonResult QuestionType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<QuestionType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">QuestionTypeID</param>
        public JsonResult QuestionType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<QuestionType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ResponseTypeID", Name = "Response Type", FieldType = DataType.Lookup.ToString(), Items = Company.Table<ResponseType>().OrderBy(i => i.Name).ToList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList(), Value = a.ResponseTypeID.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("surveys/{surveyTypeID:int}/questions/add")]
        public ActionResult AddQuestionType(int surveyTypeID)
        {
            var a = Company.GetById<SurveyType>(surveyTypeID);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.QuestionType,
                FieldUri = string.Format("/form/QuestionType_AddFields?id={0}", a.ID),
                FormTitle = "Add question to " + a.Name,
                FormUri = "/form/AddQuestionType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddQuestionType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("question");

                int typeID = parseIntField(form, "SurveyTypeID");
                var type = Company.GetById<SurveyType>(typeID);
                if (type == null) throw new NotFoundException("survey type");

                var model = new QuestionType
                {
                    SurveyTypeID = typeID,
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    ResponseTypeID = parseIntField(form, "ResponseTypeID")
                };
                Company.Add<QuestionType>(model);

                return jsonSuccess("Question successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("surveys/{surveyTypeID:int}/questions/{questionTypeID:int}/delete")]
        public ActionResult DeleteQuestionType(int surveyTypeID, int questionTypeID)
        {
            var a = Company.GetById<QuestionType>(questionTypeID);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.QuestionType,
                FieldUri = string.Format("/form/QuestionType_DeleteFields?id={0}", a.ID),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, "this question"),
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
                if (!form.HasKeys()) throw new NoFormDataException("question");

                var id = parseIntField(form, "ID");
                Company.Delete<QuestionType>(i => i.ID == id);

                return jsonSuccess("Question successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("surveys/{surveyTypeID:int}/questions/{questionTypeID:int}/edit")]
        public ActionResult EditQuestionType(int surveyTypeID, int questionTypeID)
        {
            var a = Company.GetById<QuestionType>(questionTypeID);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.QuestionType,
                FieldUri = string.Format("/form/QuestionType_EditFields?id={0}", a.ID),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditQuestionType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditQuestionType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("question");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<QuestionType>(id);
                if (model == null) throw new NotFoundException("question");

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.ResponseTypeID = parseIntField(form, "ResponseTypeID");

                Company.Update<QuestionType>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region RedFlag

        [Route("{type}/{id:int}/redflag")]
        public ActionResult UpdateRedFlag(SystemObjects type, int id)
        {
            var flag = Company.GetActiveAlertFlagByObject(type, id);
            ViewData.Add("Type", type);
            ViewData.Add("ID", id);
            return PartialView(flag);
        }

        [HttpPost, ValidateInput(false), Route("{type}/{id:int}/redflag")]
        public JsonResult UpdateRedFlag(SystemObjects type, int id, FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("red flag");

                var flag = Company.GetActiveAlertFlagByObject(type, id);
                
                if (flag != null)
                    Company.CloseActiveAlertFlag(type, id, form["Comment"]);
                else
                    Company.AddActiveAlertFlag(type, id, form["Comment"]);

                return jsonSuccess("Red flag successfully set.", "0", form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

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
                    a.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) } );//@"^([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})$", null, null, "be an email address") });
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
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Value = a.Email, Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) } );//@"^([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})$", null, null, "be an email address") });
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
                    Community.ChangePassword(a.ID, "", form["Password"]);
                }

                if (!GetCompanyResources().Any(i => i.ID == a.ID))
                {
                    var isAdmin = parseBooleanField(form, "IsAdministrator");
                    Community.Add<CompanyResource>(new CompanyResource
                    {
                        CompanyID = Company.CurrentCompanyID,
                        IsAdministrator = isAdmin,
                        ResourceID = a.ID
                    });
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                var model = Community.GetById<Resource>(id);
                if (model == null) throw new NotFoundException("resource");

                Community.Delete<Resource>(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

        [HttpPost, ValidateInput(false)]
        public JsonResult AddResourceType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource type");

                var a = new ResourceType
                {
                    Name = parseTextField(form, "Name")
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                model.Name = parseTextField(form, "Name");

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region ResponseType

        #region Field Generation

        public JsonResult ResponseType_AddFields()
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        public JsonResult ResponseType_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        public JsonResult ResponseType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ResponseType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("responsetypes/add")]
        public ActionResult AddResponseType()
        {
            var model = new EditableForm
            {
                Context = ContextList.ResponseType,
                FieldUri = "/form/ResponseType_AddFields",
                FormTitle = "Add Type",
                FormUri = "/form/AddResponseType",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddResponseType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("response type");

                var model = new ResponseType
                {
                    Name = parseTextField(form, "Name"),
                    AllowOptions = true,
                    AllowValueOverride = false
                };
                Company.Add<ResponseType>(model);

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("responsetypes/{id:int}/delete")]
        public ActionResult DeleteResponseType(int id)
        {
            var a = Company.GetById<ResponseType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResponseType,
                FieldUri = string.Format("/form/ResponseType_DeleteFields?id={0}", a.ID),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteResponseType",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteResponseType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("response type");

                var id = parseIntField(form, "ID");
                Company.Delete<ResponseType>(i => i.ID == id);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("responsetypes/{id:int}/edit")]
        public ActionResult EditResponseType(int id)
        {
            var a = Company.GetById<ResponseType>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResponseType,
                FieldUri = string.Format("/form/ResponseType_EditFields?id={0}", a.ID),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditResponseType",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResponseType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("response type");

                var id = parseIntField(form, "ID");
                var a = Company.GetById<ResponseType>(id);
                if (a == null) throw new NotFoundException("response type");

                a.Name = parseTextField(form, "Name");

                Company.Update<ResponseType>(a);

                return jsonSuccess(a.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region ResponseTypeOption

        #region Field Generation

        public JsonResult ResponseTypeOption_AddFields(int typeID)
        {
            var a = new ResponseTypeOption();

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ResponseTypeID", FieldType = DataType.Hidden.ToString(), Value = typeID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Value", Name = a.GetName(i => i.Value), FieldType = DataType.Number.ToString(), Validations = checkAndAddValidation("Text", "Value", true, "", 1, 250) });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        public JsonResult ResponseTypeOption_DeleteFields(int id)
        {
            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        public JsonResult ResponseTypeOption_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<ResponseTypeOption>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Value", Name = a.GetName(i => i.Value), FieldType = DataType.Number.ToString(), Value = a.Value.ToString(), Validations = checkAndAddValidation("Text", "Value", true, "", 1, 250) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("responsetypes/{typeID:int}/add")]
        public ActionResult AddResponseTypeOption(int typeID)
        {
            var model = new EditableForm
            {
                Context = ContextList.ResponseTypeOption,
                FieldUri = "/form/ResponseTypeOption_AddFields?typeID=" + typeID,
                FormTitle = "Add Option",
                FormUri = "/form/AddResponseTypeOption",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddResponseTypeOption(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("response type option");

                int typeID;
                if (!int.TryParse(form["ResponseTypeID"], out typeID))
                {
                    throw new MissingPropertiesException("ResponseTypeID");
                }
                var type = Company.GetById<ResponseType>(typeID);
                if (type == null) throw new NotFoundException("response type");

                var model = new ResponseTypeOption
                {
                    Name = parseTextField(form, "Name"),
                    Value = parseIntField(form, "Value"),
                    ResponseTypeID = typeID
                };
                Company.Add<ResponseTypeOption>(model);

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("responsetypes/{typeID:int}/{id:int}/delete")]
        public ActionResult DeleteResponseTypeOption(int typeID, int id)
        {
            var a = Company.GetById<ResponseTypeOption>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResponseTypeOption,
                FieldUri = string.Format("/form/ResponseTypeOption_DeleteFields?id={0}", a.ID),
                FormTitle = string.Format(Resources.FormInfo.Delete_Generic_Title, a.Name),
                FormUri = "/form/DeleteResponseTypeOption",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteResponseTypeOption(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("response type option");

                var id = parseIntField(form, "ID");
                Company.Delete<ResponseTypeOption>(i => i.ID == id);

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("responsetypes/{typeID:int}/{id:int}/edit")]
        public ActionResult EditResponseTypeOption(int typeID, int id)
        {
            var a = Company.GetById<ResponseTypeOption>(id);
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.ResponseTypeOption,
                FieldUri = string.Format("/form/ResponseTypeOption_EditFields?id={0}", a.ID),
                FormTitle = "Edit " + a.Name,
                FormUri = "/form/EditResponseTypeOption",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut, ValidateInput(false)]
        public JsonResult EditResponseTypeOption(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("response type option");

                var id = parseIntField(form, "ID");
                var a = Company.GetById<ResponseTypeOption>(id);
                if (a == null) throw new NotFoundException("response type option");

                a.Name = parseTextField(form, "Name");
                a.Value = parseIntField(form, "Value");

                Company.Update<ResponseTypeOption>(a);

                return jsonSuccess(a.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region Rule

        #region Field Generation

        public JsonResult Rule_AddFields(int policyID)
        {
            var model = new Rule();
            if (!Company.HasPermission(SystemObjects.Rule, 0, Claim.Create, ClaimObject.Root))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { FieldName = "PolicyID", FieldType = DataType.Hidden.ToString(), Value = policyID.ToString() });
            
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "RuleType", Name = model.GetName(i => i.RuleType), FieldDescription = model.GetDescription(i => i.RuleType), Items = RuleType.Informational.GetRuleTypeEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString()}).ToList(), FieldType = DataType.Lookup.ToString() });

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Description), FieldDescription = model.GetDescription(i => i.Description), FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        public JsonResult Rule_DeleteFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Delete))
                return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        public JsonResult Rule_EditFields(int id)
        {
            if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var model = Company.GetById<Rule>(id);
            var anyEvents = Company.Events.Any(i => i.EventGroup.RuleID == id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = model.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = model.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, ReadOnly = anyEvents, FieldName = "RuleType", Name = model.GetName(i => i.RuleType), FieldDescription = model.GetDescription(i => i.RuleType), Items = RuleType.Informational.GetRuleTypeEnumList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList(), FieldType = DataType.Lookup.ToString(), Value = ((int)model.RuleType).ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Description", Name = model.GetName(i => i.Name), FieldDescription = model.GetDescription(i => i.Name), FieldType = DataType.Html.ToString(), Value = model.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddRule(int policyID)
        {
            var model = new EditableForm
            {
                Context = ContextList.Rule,
                FieldUri = "/form/Rule_AddFields?policyID=" + policyID,
                FormTitle = Resources.FormInfo.Add_Rule_Title,
                FormDescription = Resources.FormInfo.Add_Rule_Directions,
                FormUri = "/form/AddRule",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddRule(FormCollection form)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Rule, 0, Claim.Create, ClaimObject.Root))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("Rule");

                var model = new Rule
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    PolicyID = parseIntField(form, "PolicyID"),
                    RuleType = (RuleType)Enum.Parse(typeof(RuleType), form["RuleType"])
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Delete))
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                if (!Company.HasPermission(SystemObjects.Rule, id, Claim.Update))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.RuleType = (RuleType)Enum.Parse(typeof(RuleType), form["RuleType"]);

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region SourceToTarget

        public JsonNetResult SourceToTarget_Step1()
        {
            //var models = (
            //            from a in Company.Table<Artifact>()
            //            join rt in Company.Filter<ResponsibilityTypeSourceType>(i => i.ResponsibilityTypeID == 0) on a.ArtifactTypeID equals rt.ObjectID
            //            join t in Company.Table<ArtifactType>() on rt.ObjectID equals t.ID
            //            orderby t.Name
            //            orderby a.Name
            //            select new { @group = t.Name, title = a.Name, value = a.ID.ToString() }//{ group = t.Name, title = a.Name }//{ group = t.Name, text = a.Name, value = a.ID.ToString()}
            //            );
            var models = Company.Query<dynamic>(
@"select    AT.Name as [group],
			A.Name as title,
			A.ID as value
from		Artifact A
			inner join ResponsibilityTypeSourceType RT on RT.ResponsibilityTypeID = 0 and RT.ObjectID = A.ArtifactTypeID
			inner join ArtifactType AT on AT.ID = A.ArtifactTypeID
order by	AT.Name,
			A.Name");
            return new JsonNetResult
            {
                Formatting = Newtonsoft.Json.Formatting.None,
                Data = models
            };
        }

        public JsonNetResult SourceToTarget_SourcingObjectOptions(SystemObjects type, int id)
        {
            var models = Company.Query<dynamic>(
@"select		cast(TTN.IntersectTypeID as varchar(15)) + '|' + D.[Object] + '|' + cast(D.ObjectID as varchar(15)) as value,
			D.Name as title,
			D.ObjectTypeName as [group],
			case 
				when CR.value is null then 0
				else 1
			end as related
from		cache.ObjectDetails SD 
			inner join IntersectTypeNode STN on SD.[Object] = @type and SD.ObjectID = @id and STN.ObjectType = SD.ObjectType and STN.ObjectID = SD.ObjectTypeID
			inner join IntersectTypeNode TTN on TTN.IntersectTypeID = STN.IntersectTypeID and TTN.ID <> STN.ID and TTN.[Order] = 2 and TTN.[Order] = 1
			inner join cache.ObjectDetails D on D.ObjectType = TTN.ObjectType and D.ObjectTypeID = TTN.ObjectID
			left join	(
						select	cast(R.IntersectTypeID as varchar(15)) + '|' + R.TargetObject + '|' + cast(R.TargetObjectID as varchar(15)) as value
						from	[cache].[Relationships] R
								inner join IntersectTypeNode TN on TN.ID = R.TargetIntersectTypeNodeID and TN.[Order] = 2 and TN.[Order] = 1 and R.SourceObject = @type and R.SourceObjectID = @id
						) CR on CR.value = cast(TTN.IntersectTypeID as varchar(15)) + '|' + D.[Object] + '|' + cast(D.ObjectID as varchar(15))
order by	D.ObjectTypeName,
			D.Name", new { type = type.ToString(), id });
            return new JsonNetResult
            {
                Formatting = Newtonsoft.Json.Formatting.None,
                Data = models
            };
        }

        public JsonNetResult SourceToTarget_SourcingAttributeOptions(SystemObjects type, int id)
        {
            var models = Company.Query<dynamic>(
@"with fa as	(
			select	A.ID,
					A.ParentID,
					A.FusionAttributeTypeID
			from	FusionAttributeOwnerRule R
					inner join FusionAttributeOwnerRuleItem RI on RI.FusionAttributeOwnerRuleID = R.ID and R.RelationshipOwnerObjectType = @type and R.RelationshipOwnerObjectID = @id
					inner join FusionAttribute A on (
													(RI.FusionAttributeID is not null and A.ID = RI.FusionAttributeID) OR 
													(RI.FusionAttributeID is null and A.FusionAttributeTypeID = R.ObjectID)
													)
			union all
			select	C.ID,
					C.ParentID,
					C.FusionAttributeTypeID
			from	FusionAttribute C
					inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
			)

SELECT	B.ID as value,
		B.TextPath as title,
		C.TextPath as [group]
FROM	fa 
		inner join FusionAttribute B on B.ID = fa.ID
		INNER JOIN FusionAttributeType C ON	C.ID = B.FusionAttributeTypeID
where	fa.FusionAttributeTypeID in (
									select		TI.ObjectID
									from		cache.ObjectDetails D 
												inner join IntersectTypeNode S on D.[Object] = @type and D.ObjectID = @id and S.ObjectType = D.ObjectType and S.ObjectID = D.ObjectTypeID
												inner join IntersectTypeNode T on T.IntersectTypeID = S.IntersectTypeID and T.ID <> S.ID and T.[Order] = 2 and S.[Order] = 1
												inner join IntersectTypeNode SI on SI.ObjectType = 'IntersectType' and SI.ObjectID = T.IntersectTypeID  
												inner join IntersectTypeNode TI on TI.IntersectTypeID = SI.IntersectTypeID and TI.ID <> SI.ID and T.[Order] = 2 and TI.ObjectType = 'FusionAttributeType'
									)
order by	C.TextPath,
			B.TextPath", new { type = type.ToString(), id });
            return new JsonNetResult
            {
                Formatting = Newtonsoft.Json.Formatting.None,
                Data = models
            };
        }

        public ActionResult AddSourceToTarget(SystemObjects type, int id)
        {
            var detail = Company.GetObjectDetail(type.ToString(), id);

            var o = new SourceToTargetEditForm
            {
                FormUri = "/Form/AddSourceToTarget",
                FormMethod = "POST",
                FormTitle = Resources.FormInfo.Add_SourceTargetMapping_Title,
                Context = ContextList.SourceToTarget,
                FormDescription = Resources.FormInfo.Add_SourceTargetMapping_Directions,
                Object = type.ToString(),
                ObjectID = id,
                ObjectName = detail.Name
            };

            return PartialView("SourceToTargetEditForm", o);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddSourceToTarget(SourceToTargetEditModel model)
        {
            try
            {
                model.Groups.ForEach(g =>
                {
                    var mapping = new IntersectFlowMapping { Definition = g.Definition, Formula = g.Formula };
                    Company.Add<IntersectFlowMapping>(mapping);

                    g.Items.ForEach(i => 
                    {
                        var sourceSystem = "Artifact";
                        var sourceSystemID = int.Parse(i.SourceSystem);
                        var sourceObjectRaw = i.SourceObject.Split('|');
                        var sourceObjectIntersectTypeID = int.Parse(sourceObjectRaw[0]);
                        var sourceObject = sourceObjectRaw[1];
                        var sourceObjectID = int.Parse(sourceObjectRaw[2]);
                        var sourceFusionAttributeID = i.SourceFusionAttribute;

                        var targetSystem = "Artifact";
                        var targetSystemID = int.Parse(i.TargetSystem);
                        var targetObjectRaw = i.TargetObject.Split('|');
                        var targetObjectIntersectTypeID = int.Parse(targetObjectRaw[0]);
                        var targetObject = targetObjectRaw[1];
                        var targetObjectID = int.Parse(targetObjectRaw[2]);
                        var targetFusionAttributeID = i.TargetFusionAttribute;

                        Company.AddMappingDependency(mapping.ID,
                            sourceSystem, sourceSystemID, sourceObject, sourceObjectID, sourceFusionAttributeID,
                            targetSystem, targetSystemID, targetObject, targetObjectID, targetFusionAttributeID
                        );
                    });

                });
                return jsonSuccess("", "0", ContextList.SourceToTarget, "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

        public ActionResult AddStatisticType()
        {
            var model = new StatisticTypeEditorModel
            {   
                FormDescription = Resources.FormInfo.Add_AnalyticType_Directions,
                FormMethod = "POST",
                FormName = Resources.FormInfo.Add_AnalyticType_Title,
                FormUri = "/Form/AddStatisticType",
                StatisticType = new StatisticType { CheckType = StatisticCheckType.Existence }
            };

            #region Lookup Lists

            model.ExistenceCheckItems = Company.GetStatisticTypeExistenceCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            model.CountCheckItems = Company.GetStatisticTypeCountCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            model.PropertyExistenceCheckItems.Add(new SelectListItem { Text = "Description", Value = "Description" });

            model.PropertyValueCheckItems.Add(new SelectListItem { Text = "Status", Value = "Status" });

            model.RelationshipCheckItems = Company.GetStatisticTypeRelationshipCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            model.RollupCheckItems = Company.GetStatisticTypeRollupCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            #endregion

            return PartialView("StatisticTypeEditForm", model);
        }

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
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    CheckType = (StatisticCheckType)Enum.Parse(typeof(StatisticCheckType), form["CheckType"]),
                    PartOfScore = parseBooleanField(form, "PartOfScore")
                };

                var fields = new XElement("fields");
                
                switch (a.CheckType)
                {
                    case StatisticCheckType.Count:
                    case StatisticCheckType.Existence:
                    case StatisticCheckType.Relationship:
                    case StatisticCheckType.ScoreRollupViaRelationship:
                    case StatisticCheckType.ScoreRollupViaOwnership:
                        string[] value = form["ObjectTypeInfo"].Split('|');
                        fields.Add(new XElement("ObjectType", value[0]));
                        fields.Add(new XElement("ObjectID", value[1]));
                        break;
                    case StatisticCheckType.PropertyPopulated:
                        fields.Add(new XElement("PropertyName", form["ObjectTypeInfo"]));
                        break;
                    case StatisticCheckType.PropertyValueCheck:
                        fields.Add(new XElement("PropertyName", form["ObjectTypeInfo"]));
                        fields.Add(new XElement("Value", form["Value"]));
                        break;
                    case StatisticCheckType.EventMetric:
                        fields.Add(new XElement("ValidField", form["ValidFieldName"]));
                        fields.Add(new XElement("InvalidField", form["InvalidFieldName"]));
                        fields.Add(new XElement("Threshold", decimal.Parse(form["Threshold"])));
                        break;
                }

                a.Configuration = fields.ToString();
                Company.Add<StatisticType>(a);

                return jsonSuccess(a.Name + " successfully created.", a.ID.ToString(), form["_context"], "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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

                return jsonSuccess("Item successfully removed.", id.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditStatisticType(int id)
        {
            var a = Company.GetById<StatisticType>(id);
            if (a == null) return HttpNotFound();

            var model = new StatisticTypeEditorModel {
                FormDescription = Resources.FormInfo.Add_AnalyticType_Directions,
                FormMethod = "PUT",
                FormName = string.Format(Resources.FormInfo.Edit_Generic_Title, a.Name),
                FormUri = "/Form/EditStatisticType",
                StatisticType = a
            };

            #region Lookup Lists

            model.ExistenceCheckItems = Company.GetStatisticTypeExistenceCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            model.CountCheckItems = Company.GetStatisticTypeCountCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            model.PropertyExistenceCheckItems.Add(new SelectListItem { Text = "Description", Value = "Description" });
            
            model.PropertyValueCheckItems.Add(new SelectListItem { Text = "Status", Value = "Status" });

            model.RelationshipCheckItems = Company.GetStatisticTypeRelationshipCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            model.RollupCheckItems = Company.GetStatisticTypeRollupCheckOptions()
                .Select(i => new SelectListItem { Text = i.Name, Value = i.ID })
                .ToList();

            #endregion

            var fields = XElement.Parse(a.Configuration);

            var objectTypeInfo = "";
            if (fields.Element("ObjectType") != null && fields.Element("ObjectID") != null)
            {
                objectTypeInfo = string.Format("{0}|{1}", fields.Element("ObjectType").Value, fields.Element("ObjectID").Value);
            }
            model.ObjectTypeInfo = objectTypeInfo;

            switch (a.CheckType)
            {
                case StatisticCheckType.Existence:
                    break;
                case StatisticCheckType.Count:
                    break;
                case StatisticCheckType.PropertyValueCheck:
                    model.ObjectTypeInfo = fields.Element("PropertyName").Value;
                    model.Value = fields.Element("Value").Value;
                    break;
                case StatisticCheckType.PropertyPopulated:
                    model.ObjectTypeInfo = fields.Element("PropertyName").Value;
                    break;
                case StatisticCheckType.Relationship:
                    break;
                case StatisticCheckType.FusionOwnership:
                    break;
                case StatisticCheckType.EventMetric:
                    model.ValidFieldName = fields.Element("ValidField").Value;
                    model.InvalidFieldName = fields.Element("InvalidField").Value;
                    model.Threshold = fields.Element("Threshold").Value;
                    break;
            }

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

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.PartOfScore = parseBooleanField(form, "PartOfScore");
                model.CheckType = (StatisticCheckType)Enum.Parse(typeof(StatisticCheckType), form["CheckType"]);

                var fields = new XElement("fields");

                switch (model.CheckType)
                {
                    case StatisticCheckType.Count:
                    case StatisticCheckType.Existence:
                    case StatisticCheckType.Relationship:
                    case StatisticCheckType.ScoreRollupViaRelationship:
                    case StatisticCheckType.ScoreRollupViaOwnership:
                        string[] value = form["ObjectTypeInfo"].Split('|');
                        fields.Add(new XElement("ObjectType", value[0]));
                        fields.Add(new XElement("ObjectID", value[1]));
                        break;
                    case StatisticCheckType.PropertyPopulated:
                        fields.Add(new XElement("PropertyName", form["ObjectTypeInfo"]));
                        break;
                    case StatisticCheckType.PropertyValueCheck:
                        fields.Add(new XElement("PropertyName", form["ObjectTypeInfo"]));
                        fields.Add(new XElement("Value", form["Value"]));
                        break;
                    case StatisticCheckType.EventMetric:
                        fields.Add(new XElement("ValidField", form["ValidFieldName"]));
                        fields.Add(new XElement("InvalidField", form["InvalidFieldName"]));
                        fields.Add(new XElement("Threshold", decimal.Parse(form["Threshold"])));
                        break;
                }

                model.Configuration = fields.ToString();

                Company.Update<StatisticType>(model);

                return jsonSuccess(model.Name + " successfully updated.", id.ToString(), form["_context"], "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region StatisticTypeRelation

        #region Field Generation

        /// <param name="st">StatisticTypeID</param>
        public JsonResult StatisticTypeRelation_AddFields(int st)
        {
            var list = new List<EditableField>();
            var type = Company.GetById<StatisticType>(st);

            var relation = new StatisticTypeRelation();

            var comparer = new AllocationPossibilityComparer();
            var objectTypeInfos = 
                Company.Database
                .SqlQuery<AllocationPossibility>("EXEC GetAllocationOptions")
                .ToList()
                .Except(Company.Filter<StatisticTypeRelationDetail>(i => i.StatisticTypeID == st).Select(i => new AllocationPossibility { ObjectType = i.ObjectType, ObjectTypeID = i.ObjectID }).ToList(), comparer)
                .Select(i => new SelectListItem
                    {
                        Value = i.ObjectType + "|" + i.ObjectTypeID,
                        Text = i.Name
                    })
                .ToList();
            objectTypeInfos.Add(new SelectListItem { Text = "Groups", Value = "Group|0" });
            objectTypeInfos.Add(new SelectListItem { Text = "Resources", Value = "ResourceType|1" });

            list.Add(new EditableField { FieldName = "StatisticTypeID", FieldType = DataType.Hidden.ToString(), Value = st.ToString() });
            list.Add(new EditableField
            {
                Row = 1,
                Column = 1,
                FieldName = "ObjectTypeInfo",
                Name = "Type",
                FieldType = DataType.Lookup.ToString(),
                Items = objectTypeInfos
            });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Score", Name = relation.GetName(i => i.Score), FieldDescription = relation.GetDescription(i => i.Score), FieldType = DataType.Number.ToString(), Validations = checkAndAddValidation("Text", "Score", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="at">StatisticTypeID</param>
        /// <param name="ot">ObjectType</param>
        /// <param name="oid">ObjectID</param>
        public JsonResult StatisticTypeRelation_DeleteFields(int st, string ot, int oid)
        {
            var list = new List<EditableField>();
            var sType = ot.ToString();
            var a = Company.Filter<StatisticTypeRelationDetail>(i => i.StatisticTypeID == st && i.ObjectType == sType && i.ObjectID == oid).SingleOrDefault();

            list.Add(new EditableField { FieldName = "StatisticTypeID", FieldType = DataType.Hidden.ToString(), Value = a.StatisticTypeID.ToString() });
            list.Add(new EditableField { FieldName = "ObjectType", FieldType = DataType.Hidden.ToString(), Value = a.ObjectType });
            list.Add(new EditableField { FieldName = "ObjectID", FieldType = DataType.Hidden.ToString(), Value = a.ObjectID.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult StatisticTypeRelation_EditFields(int st, SystemObjects ot, int oid)
        {
            var list = new List<EditableField>();
            var sType = ot.ToString();
            var a = Company.Filter<StatisticTypeRelationDetail>(i => i.StatisticTypeID == st && i.ObjectType == sType && i.ObjectID == oid).SingleOrDefault();

            var relation = new StatisticTypeRelation();

            list.Add(new EditableField { FieldName = "StatisticTypeID", FieldType = DataType.Hidden.ToString(), Value = a.StatisticTypeID.ToString() });
            list.Add(new EditableField { FieldName = "ObjectTypeInfo", FieldType = DataType.Hidden.ToString(), Value = string.Format("{0}|{1}", a.ObjectType, a.ObjectID) });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Score", Name = relation.GetName(i => i.Score), FieldDescription = relation.GetDescription(i => i.Score), FieldType = DataType.Number.ToString(), Value = a.Score.ToString(), Validations = checkAndAddValidation("Text", "Score", true, "", null, null) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        public ActionResult AddStatisticTypeRelation(int id)
        {
            var type = Company.GetById<StatisticType>(id);
            if (type == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.StatisticTypeRelation,
                FieldUri = string.Format("/form/StatisticTypeRelation_AddFields?st={0}", id),
                FormTitle = "Allocating " + type.Name,
                FormUri = "/form/AddStatisticTypeRelation",
                FormMethod = "POST"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult AddStatisticTypeRelation(FormCollection form)
        {
            try
            {
                if (form.HasKeys())
                {
                    var a = new StatisticTypeRelation();

                    int typeID = parseIntField(form, "StatisticTypeID");
                    var type = Company.GetById<StatisticType>(typeID);
                    if (type == null)
                    {
                        return jsonException("Invalid statistic type.", HttpStatusCode.BadRequest);
                    }

                    var value = form["ObjectTypeInfo"].Split('|');

                    // Static fields
                    a.StatisticType = type;
                    a.Score = parseIntField(form, "Score");
                    a.ObjectType = value[0];
                    a.ObjectID = int.Parse(value[1]);

                    // Save the allocation
                    Company.Add<StatisticTypeRelation>(a);

                    return jsonSuccess(type.Name + " successfully allocated.", a.StatisticTypeID.ToString(), form["_context"], "add", HttpStatusCode.Created);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteStatisticTypeRelation(int id, SystemObjects objectType, int objectTypeID)
        {
            var sType = objectType.ToString();
            var a = Company.Filter<StatisticTypeRelationDetail>(i => i.StatisticTypeID == id && i.ObjectType == sType && i.ObjectID == objectTypeID).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.StatisticTypeRelation,
                FieldUri = string.Format("/form/StatisticTypeRelation_DeleteFields?st={0}&ot={1}&oid={2}", a.StatisticTypeID, a.ObjectType, a.ObjectID),
                FormTitle = "Are you sure you want to de-allocate " + a.ObjectName + "?",
                FormUri = "/form/DeleteStatisticTypeRelation",
                FormMethod = "DELETE"
            };

            return PartialView("DeleteForm", model);
        }

        [HttpDelete]
        public JsonResult DeleteStatisticTypeRelation(FormCollection form)
        {
            try
            {
                var st = parseIntField(form, "StatisticTypeID");
                var ot = form["ObjectType"];
                var oid = parseIntField(form, "ObjectID");

                Company.Delete<StatisticTypeRelation>(i => i.ObjectID == oid && i.ObjectType == ot && i.StatisticTypeID == st);

                return jsonSuccess("Allocation successfully removed.", ot.ToString(), form["_context"], "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditStatisticTypeRelation(int id, SystemObjects objectType, int objectTypeID)
        {
            var sType = objectType.ToString();
            var a = Company.Filter<StatisticTypeRelationDetail>(i => i.StatisticTypeID == id && i.ObjectType == sType && i.ObjectID == objectTypeID).SingleOrDefault();
            if (a == null) return HttpNotFound();
            var model = new EditableForm
            {
                Context = ContextList.StatisticTypeRelation,
                FieldUri = string.Format("/form/StatisticTypeRelation_EditFields?st={0}&ot={1}&oid={2}", a.StatisticTypeID, a.ObjectType, a.ObjectID),
                FormTitle = "Edit Allocation",
                FormUri = "/form/EditStatisticTypeRelation",
                FormMethod = "PUT"
            };

            return PartialView("EditableForm", model);
        }

        [HttpPut]
        public JsonResult EditStatisticTypeRelation(FormCollection form)
        {
            try
            {
                var at = parseIntField(form, "StatisticTypeID");
                var value = form["ObjectTypeInfo"].Split('|');
                var ot = value[0];
                var oid = int.Parse(value[1]);
                var model = Company.Filter<StatisticTypeRelation>(i => i.StatisticTypeID == at && i.ObjectType == ot && i.ObjectID == oid).SingleOrDefault();
                if (model == null)
                {
                    return jsonException("Allocation does not exist.", HttpStatusCode.NotFound);
                }

                model.Score = parseIntField(form, "Score");

                Company.Update<StatisticTypeRelation>(model);

                return jsonSuccess("Allocation successfully updated.", value[0], form["_context"], "update", HttpStatusCode.OK, new { StatisticTypeID = at });
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
            items.AddRange(Company.Table<ArtifactType>().OrderBy(i => i.Name).Select(i => new { i.ID, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Artifact Type :: {0}", i.Name), Value = string.Format("{0}|{1}", SystemObjects.ArtifactType.ToString(), i.ID) }));
            items.AddRange(Community.Table<ResourceType>().OrderBy(i => i.Name).Select(i => new { i.ID, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Resource Type :: {0}", i.Name), Value = string.Format("{0}|{1}", SystemObjects.ResourceType.ToString(), i.ID) }));

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Object", Name = "Assign Survey To", FieldType = DataType.Lookup.ToString(), Items = items });

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

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [Route("surveys/add")]
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
                    Name = parseTextField(form, "Name"),
                    ObjectType = ot.ToString(),
                    ObjectID = oid
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("surveys/{id:int}/delete")]
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }


        [Route("surveys/{id:int}/edit")]
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

                model.Name = parseTextField(form, "Name");

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
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
                a.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");
                model.ParentID = parseIntField(form, "ParentID");
                if (model.ParentID == 0) model.ParentID = null;

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Taxonomy, model.ID, Company.GetFieldTypeRelationsByObject(SystemObjects.TaxonomyType, model.TaxonomyTypeID).ToList(), form, Server);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region TaxonomyType

        #region Field Generation

        public JsonResult TaxonomyType_AddFields()
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, 0, Claim.Create))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = new TaxonomyType();
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Class", Name = a.GetName(i => i.Class), FieldDescription = a.GetDescription(i => i.Class), FieldType = DataType.Lookup.ToString(), Items = Enums.GetEnumValuesAsDictionary<TaxonomyTypeClass>().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList() });
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

            var maxLevel = Company.Query<int>("select max([Level]) from Taxonomy where TaxonomyTypeID = @t", new { t = id }).SingleOrDefault();

            var maxDepthNotification = (maxLevel > 1) ? string.Format("  The current depth of this model type's hierarchy is {0} levels, so you may not set a Maxiumum Depth less than that.", maxLevel) : "";

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = a.GetName(i => i.Name), FieldDescription = a.GetDescription(i => i.Name), FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Class", Name = a.GetName(i => i.Class), FieldDescription = a.GetDescription(i => i.Class), FieldType = DataType.Lookup.ToString(), Value = ((int)a.Class).ToString(), Items = Enums.GetEnumValuesAsDictionary<TaxonomyTypeClass>().Select(i => new SelectListItem { Text = i.Value, Value = i.Key.ToString() }).ToList() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MaximumDepth", Name = a.GetName(i => i.MaximumDepth), RangeMin = maxLevel, RangeMax = 25, FieldDescription = a.GetDescription(i => i.MaximumDepth) + maxDepthNotification, FieldType = DataType.Number.ToString(), Value = a.MaximumDepth.HasValue ? a.MaximumDepth.Value.ToString() : "5" });
            list.Add(new EditableField { Row = 3, Column = 1, FieldName = "Description", Name = a.GetName(i => i.Description), FieldDescription = a.GetDescription(i => i.Description), FieldType = DataType.Html.ToString(), Value = a.Description });
            loadIconFields(list, 4, style);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

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
                    Class = (TaxonomyTypeClass)Enum.Parse(typeof(TaxonomyTypeClass), form["Class"]),
                    MaximumDepth = parseIntField(form, "MaximumDepth")
                };

                Company.SaveOrUpdate<TaxonomyType>(a);

                for (int i = 1; i <= a.MaximumDepth; i++)
                {
                    Company.TaxonomyTypeLevels.Add(new TaxonomyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), TaxonomyTypeID = a.ID });
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                model.Class = (TaxonomyTypeClass)Enum.Parse(typeof(TaxonomyTypeClass), form["Class"]);
                model.MaximumDepth = parseIntField(form, "MaximumDepth");

                var currentMaxLevel = Company.Query<int>("select max([Level]) from Taxonomy where TaxonomyTypeID = @t", new { t = id }).SingleOrDefault();

                if (currentMaxLevel > model.MaximumDepth)
                    throw new InvalidFieldException(d360.core.resources.Fields.MaximumDepth_Name, "less than the current maximum depth of " + currentMaxLevel);

                Company.SaveOrUpdate<TaxonomyType>(model);

                bool addedLevel = false;
                for (int i = 1; i <= model.MaximumDepth; i++)
                {
                    var level = model.TaxonomyTypeLevels.SingleOrDefault(l => l.Level == i);
                    if (level == null)
                    {
                        Company.TaxonomyTypeLevels.Add(new TaxonomyTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), TaxonomyTypeID = model.ID });
                        addedLevel = true;
                    }
                }
                Company.TaxonomyTypeLevels.RemoveRange(model.TaxonomyTypeLevels.Where(l => l.Level > model.MaximumDepth));
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
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

                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    Name = parseTextField(form, "Name"),
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                model.Name = parseTextField(form, "Name");
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                    //checkValueAndAddNode("CertificationStartDate", form, xml);
                    //checkValueAndAddNode("CertificationEndDate", form, xml);
                    break;
                case WorkflowType.SuggestNewArtifact:
                    break;
            }

            return xml;
        }

        public ActionResult AddWorkflowAllocation(WorkflowType workflowType)
        {
            var model = new WorkflowTypeRelationEditorModel
            {
                FormDescription = Resources.FormInfo.Allocate_Workflow_Description,
                FormMethod = "POST",
                FormName = Resources.FormInfo.Allocate_Workflow_Title,
                FormUri = "/form/AddWorkflowAllocation",
                ObjectTypes = Company.GetWorkflowObjectTypeOptions().Select(i => new SelectListItem { Text = i.Name, Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) }).ToList(),
                WorkflowType = workflowType,
                WorkflowTypeRelation = new WorkflowTypeRelation { Enabled = true }
            };

            return PartialView("WorkflowTypeRelationEditForm", model);
        }

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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditWorkflowAllocation(int id)
        {
            var relation = Company.GetById<WorkflowTypeRelation>(id);
            
            var parentTypes = Company.GetWorkflowParentTypeOptions((int)relation.WorkflowType, relation.Object, relation.ObjectID, true);
            var responsibilityTypes = Company.GetWorkflowResponsibilityTypeOptions(relation.Object, relation.ObjectID);
            
            var model = new WorkflowTypeRelationEditorModel
            {
                FormDescription = Resources.FormInfo.Allocate_Workflow_Description,
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
                return jsonException(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion
    }
}
