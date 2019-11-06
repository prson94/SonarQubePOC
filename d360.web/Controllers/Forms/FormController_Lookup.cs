using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
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
            list = (
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddLookup")]
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddLookupType")]
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
                        IsPartOfKey = true
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
    }
}