using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Newtonsoft.Json;
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
            if (!Company.HasAssetTypePermission(SystemObjects.RuleType, typeID, Permission.AddAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.RuleType, typeID).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">RuleID</param>
        [Route("Rule_EditFields"), NonNullableParameters]
        public JsonResult Rule_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Rule, id, Permission.EditAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var model = Company.GetById<Rule>(id);

            var list = new List<EditableField>();
            var uid = Company.Assets.FirstOrDefault(x => x.Object == SystemObjects.Rule.ToString() && x.ObjectID == id).uid;

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = uid.ToString() });

            list = (
                loadDynamicFields(
                    SystemObjects.Rule.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.RuleType, model.RuleTypeID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Rule, id).ToList(),
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
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

        /// <param name="id">RuleTypeID</param>
        [Route("RuleType_EditFields"), NonNullableParameters]
        public JsonResult RuleType_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.RuleType, id, Permission.EditAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Company.GetById<RuleType>(id);
            var style = Company.GetAssetTypeStyle(SystemObjects.RuleType.ToString(), id);

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

                upsertAssetStyle(SystemObjects.RuleType, a.ID, form, a.Name);

                var assetType = Company.Filter<AssetType>(i => i.Object == "RuleType" && i.ObjectID == a.ID).FirstOrDefault();
                if (assetType != null)
                {
                    Company.CreateRollupPathChangedExecution(null, assetType.ID);
                    assetType = null;
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

                var assetType = Company.Filter<AssetType>(i => i.Object == "RuleType" && i.ObjectID == id).FirstOrDefault();
                if (assetType != null)
                {
                    Company.CreateRollupPathChangedExecution(assetTypeId: assetType.ID);
                    assetType = null;
                }

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

                if (!Company.HasAssetTypePermission(SystemObjects.RuleType, id, Permission.EditAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.Name = parseTextField(form, "Name");
                model.DisplayFormat = parseTextField(form, "DisplayFormat");
                model.Description = parseTextField(form, "Description");

                Company.Update(model);

                upsertAssetStyle(SystemObjects.RuleType, model.ID, form, model.Name);

                var assetType = Company.Filter<AssetType>(i => i.Object == "RuleType" && i.ObjectID == model.ID).FirstOrDefault();
                if (assetType != null)
                {
                    Company.CreateRollupPathChangedExecution(assetTypeId: assetType.ID);
                    assetType = null;
                }

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