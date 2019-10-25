using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
        #region Rule

        #region Field Generation

        [Route("Rule_AddFields")]
        public JsonResult Rule_AddFields(int typeID)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.RuleType, typeID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Threshold", Name = FieldInfo.RuleThreshold_Name, FieldDescription = FieldInfo.RuleThreshold_Description, FieldType = DataType.Percentage.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.RuleType, typeID).ToList(), 3);


            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("Rule_EditFields"), NonNullableParameters]
        public JsonResult Rule_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var model = Company.GetById<Rule>(id);

            var list = new List<EditableField>();
            var uid = Company.Assets.FirstOrDefault(x => x.Object == SystemObjects.Rule.ToString() && x.ObjectID == id).uid;

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = uid.ToString() });

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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRuleImplementation")]
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
                    foreach (var ruleResult in res)
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
            }
            catch (Exception ex)
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
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "DisplayFormat", Name = FieldInfo.DisplayFormat_Name, FieldDescription = FieldInfo.DisplayFormat_Description, FieldType = DataType.Text.ToString(), Value = "{Name}", Validations = checkAndAddValidation("DisplayFormat", FieldInfo.DisplayFormat_Name, true, "", 2, 250) });
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRuleType")]
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

                Company.CreateOrUpdateTypeDisplayValuesAsync(id, "RuleType");

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
    }
}