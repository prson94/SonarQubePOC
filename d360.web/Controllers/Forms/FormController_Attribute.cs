using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
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
            list = (
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
                Required = true,
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


                    Company.Add(new AttributeTypeRelation
                    {
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
    }
}