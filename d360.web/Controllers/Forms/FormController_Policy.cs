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
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
        #region Policy

        #region Field Generation

        [Route("Policy_AddFields"), NonNullableParameters]
        public JsonResult Policy_AddFields(int typeID, int? parentID)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, typeID, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            if (parentID.HasValue && parentID.Value > 0)
            {
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddPolicyTypeLevel")]
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
    }
}