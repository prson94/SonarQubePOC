using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
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

            if (!Company.HasAssetTypePermission(SystemObjects.FusionType, ft, Permission.AddAsset))
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
            list.Add(new EditableField { Row = 4, Column = 1, FieldName = "IntervalType", Required = true, Name = fusion.GetName(i => i.IntervalType), FieldDescription = fusion.GetDescription(i => i.IntervalType), FieldType = DataType.Lookup.ToString(), Items = intervalTypes });
            list.Add(new EditableField { Row = 4, Column = 2, Required = true, FieldName = "Interval", Name = fusion.GetName(i => i.Interval), FieldDescription = fusion.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString(), Validations = checkAndAddValidation("Number", "Interval", true, "([1-9]|[1-8][0-9]|9[0-9]|[1-8][0-9]{2}|9[0-8][0-9]|99[0-9]|[1-8][0-9]{3}|9[0-8][0-9]{2}|99[0-8][0-9]|999[0-9]|10000)", null, null, "Please enter value between 1,10000.") });

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

            if (!Company.HasAssetPermission(SystemObjects.Fusion, id, Permission.EditAsset))
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
            list.Add(new EditableField { Row = 4, Column = 2, Required = true, FieldName = "Interval", Name = a.GetName(i => i.Interval), FieldDescription = a.GetDescription(i => i.Interval), FieldType = DataType.Number.ToString(), Value = (a.Interval.HasValue ? a.Interval.Value.ToString() : ""), Validations = checkAndAddValidation("Number", "Interval", true, "([1-9]|[1-8][0-9]|9[0-9]|[1-8][0-9]{2}|9[0-8][0-9]|99[0-9]|[1-8][0-9]{3}|9[0-8][0-9]{2}|99[0-8][0-9]|999[0-9]|10000)", null, null, "Please enter value between 1,10000.") });

            list.Add(new EditableField { Row = 5, Column = 1, FieldName = "ForceRefresh", Name = "Force Refresh on Next Run?", FieldDescription = "Force the local agent to perform a full refresh of this configuration on the next run.", FieldType = DataType.Boolean.ToString(), Value = a.ForceRefresh.GetValueOrDefault().ToString().ToLower() });
            list.Add(new EditableField { Row = 5, Column = 2, FieldName = "LockPromotedItems", Name = a.GetName(i => i.LockPromotedItems), FieldDescription = a.GetDescription(i => i.LockPromotedItems), FieldType = DataType.Boolean.ToString(), Value = a.LockPromotedItems.ToString().ToLower() });

            var owners = Company.GetFusionOwnerOptions()
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = $"{i.ID}",
                    Selected = a.FusionOwners.Any(c => c.ObjectID == i.ID && c.Object == "Artifact")
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

                if (!Company.HasAssetTypePermission(SystemObjects.FusionType, typeID, Permission.AddAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var rawOwners = parseTextField(form, "Owners");
                if (string.IsNullOrEmpty(rawOwners))
                    return jsonException("No selected owners", HttpStatusCode.BadRequest);

                var items = rawOwners.Split(',').ToList().Select(i => int.Parse(i)).ToList();

                var ownerArtifacts = Company.Filter<Asset>(i => items.Contains(i.ObjectID) && i.Object == "Artifact").ToList();

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

                if (Company.FusionAttributes.Any(x => x.FusionID == model.ID))
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

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.ID, Permission.AddAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var rawOwners = parseTextField(form, "Owners");
                if (string.IsNullOrEmpty(rawOwners))
                    return jsonException("No selected owners", HttpStatusCode.BadRequest);

                var items = rawOwners.Split(',').ToList().Select(i => int.Parse(i)).ToList();

                var ownerArtifacts = Company.Filter<Asset>(i => items.Contains(i.ObjectID) && i.Object == "Artifact").ToList();

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
                foreach (var co in model.FusionOwners)
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

                Company.SaveOrUpdate<Fusion>(model, fields, -1, true);

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

        #region FusionType

        #region Form Get/Post


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

        #endregion

        #endregion

        #region FusionAttribute

        #region Field Generation

        /// <param name="fat">FusionAttributeTypeID</param>
        /// <param name="f">FusionID</param>
        [Route("FusionAttributeType_AddFields"), NonNullableParameters]
        public JsonResult FusionAttribute_AddFields(int fat, int f)
        {
            if (!Company.HasAssetPermission(SystemObjects.Fusion, f, Permission.AddAsset))
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

            if (!Company.HasAssetPermission(SystemObjects.Fusion, a.FusionID, Permission.EditAsset))
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

                if (!Company.HasAssetPermission(SystemObjects.Fusion, model.FusionID, Permission.EditAsset))
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

    }
}