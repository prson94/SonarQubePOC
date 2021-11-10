using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
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
        #region Hierarchy

        #region Field Generation

        /// <param name="hierarchyType">TaxonomyType or PolicyType</param>
        /// <param name="t">TaxonomyTypeID</param>
        /// <param name="p">ParentID</param>        
        public JsonResult Hierarchy_AddFields(SystemObjects hierarchyType, int t, int p)
        {
            if (hierarchyType != SystemObjects.PolicyType && hierarchyType != SystemObjects.TaxonomyType)
            {
                throw new ArgumentNullException(FormControllerApiMessage.UnsupportedHierarchyAssetTypeAddField);
            }

            if (!Company.HasAssetTypePermission(hierarchyType, t, Permission.AddAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var parentUid = p > 0 ? Company.GetAssetUid(p, hierarchyType == SystemObjects.TaxonomyType ? SystemObjects.Taxonomy : SystemObjects.Policy).ToString() : "";
            list.Add(new EditableField { FieldName = "ParentUid", FieldType = DataType.Hidden.ToString(), Value = parentUid.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(hierarchyType, t).ToList(), 1, loadLookupValues: false);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="hierarchy">Policy or Taxonomy type</param>
        /// <param name="id">TaxonomyID or PolicyID</param>        
        public JsonResult Hierarchy_EditFields(SystemObjects hierarchy, int id)
        {
            if (hierarchy != SystemObjects.Policy && hierarchy != SystemObjects.Taxonomy)
            {
                throw new ArgumentNullException(FormControllerApiMessage.UnsupportedHierarchyTypeEditField);
            }
                

            if (!Company.HasAssetPermission(hierarchy, id, Permission.EditAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var model = Company.Query<dynamic>($@"
                                                    select	A.ID as AssetID,
                                                            A.UID as Uid,
		                                                    A.ObjectID,
		                                                    T.ID as TypeID,
                                                            T.ObjectID as HierarchyTypeID,
                                                            T.HierarchyMaximumDepth as MaximumDepth,
		                                                    P.TextPath,
		                                                    L.Level
                                                    from	Asset A
		                                                    inner join AssetType T on T.ID = A.AssetTypeID
		                                                    cross apply dbo.GetAssetTextPathById(A.ID, '/') P
		                                                    cross apply dbo.GetAssetLevelById(A.ID) L
                                                    where	A.Object = '{hierarchy}' and A.ObjectID = @id
                                                    ", new { id }).SingleOrDefault();
            if (model != null)
            {

                var parent = Company.GetParentObject(model.ObjectID, hierarchy);

                var parents = Company.Query<dynamic>($@"
                                select	A.ObjectID as ID,
		                                P.TextPath as Name,
                                        A.uid as Uid,
		                                coalesce(LV.[Level], 1) as [Level]
                                from	Asset A
                                        inner join AssetType T on T.ID = A.AssetTypeID and T.Object = '{hierarchy}Type' and T.ObjectID = @t
		                                cross apply dbo.GetAssetTextPathById(A.ID, ' / ') P
                                        cross apply dbo.GetAssetLevelById(A.ID) LV
                                where coalesce(LV.[Level], 1) <= @currentLevel 
                                order by P.TextPath 
                                option (maxrecursion 100)",
    new { t = model.HierarchyTypeID, currentLevel = model.Level ?? 1 }).Select(i => new { i.Uid, i.Name }).ToList();

                var thisEntry = parents.FirstOrDefault(i => i.Uid == model.Uid);

                if (thisEntry != null)
                    parents.RemoveAll(i => i.Name.StartsWith(thisEntry.Name));

                var parentItems = parents.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = $"{i.Uid}",
                    Selected = (parent != null ? (i.Uid == parent.uid) : false)
                }).ToList();
                parentItems.Insert(0, new SelectListItem { Text = "- Root -", Value = Guid.Empty.ToString(), Selected = (parent == null) });

                list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = model.Uid.ToString() });
                list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ParentUid", Name = "Parent Model", FieldDescription = FormInfo.Taxonomy_ChangeParent_Warning, FieldType = DataType.Lookup.ToString(), Items = parentItems, VirtualScroll = parents.Count > 9, ItemSize = 20, Value = ((parent != null) ? parent.uid.ToString() : Guid.Empty.ToString()) });
                list = (
                     loadDynamicFields(
                         SystemObjects.Taxonomy.ToString(),
                         id,
                         list,
                         Company.GetFieldTypesByObject((hierarchy == SystemObjects.Taxonomy ? SystemObjects.TaxonomyType : SystemObjects.PolicyType), (int)model.HierarchyTypeID).ToList(),
                         Company.GetFieldRelationsByObject(hierarchy, id).ToList(),
                         3,
                         loadOnlySelectedLookupValue: true
                     )
                 );
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion

        #region TaxonomyTypeLevel

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddTaxonomyTypeLevel")]
        public JsonResult AddTaxonomyTypeLevel(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");
                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "TaxonomyType").SingleOrDefault();
                if (assetType == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.AssetType);
                }
                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.AddAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.TaxonomyTypeLevel);
                }
                var a = new AssetTypeLevel
                {
                    AssetTypeID = assetType.ID,
                    Level = level,
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<AssetTypeLevel>(a);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated,a.Name), a.AssetTypeID.ToString(), "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.TaxonomyType);
                }

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "TaxonomyType").SingleOrDefault();
                if (assetType == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.AssetType);
                }
                Company.Delete<AssetTypeLevel>(i => i.AssetTypeID == assetType.ID && i.Level == level);
                return jsonSuccess(FormControllerApiMessage.ItemRemoved, id.ToString(), "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.TaxonomyType);
                }
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");
                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "TaxonomyType").SingleOrDefault();
                var model = Company.Filter<AssetTypeLevel>(i => i.AssetTypeID == assetType.ID && i.Level == level).SingleOrDefault();
                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.TaxonomyTypeLevel);
                }

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.EditAsset))
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<AssetTypeLevel>(model);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated, model.Name), id.ToString(), "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddPolicyTypeLevel")]
        public JsonResult AddPolicyTypeLevel(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.AddAsset))
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.PolicyTypeLevel);
                }

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "PolicyType").SingleOrDefault();

                if (assetType == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.AssetType);
                }
                var a = new AssetTypeLevel
                {
                    AssetTypeID = assetType.ID,
                    Level = level,
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                };

                Company.Add<AssetTypeLevel>(a);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated, a.Name), a.AssetTypeID.ToString(), "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
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
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.PolicyType);
                }
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.AddAsset)
                    || !Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.EditAsset))
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "PolicyType").SingleOrDefault();

                if (assetType == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.AssetType);
                }
                Company.Delete<AssetTypeLevel>(i => i.AssetTypeID == assetType.ID && i.Level == level);

                return jsonSuccess(FormControllerApiMessage.ItemRemoved, id.ToString(), "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.PolicyType);
                }
                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "PolicyType").SingleOrDefault();

                if (assetType == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.AssetType);
                }
                var model = Company.Filter<AssetTypeLevel>(i => i.AssetTypeID == assetType.ID && i.Level == level).SingleOrDefault();
                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.PolicyTypeLevel);
                }

                if (!Company.HasAssetTypePermission(SystemObjects.PolicyType, id, Permission.EditAsset))
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                model.Name = parseTextField(form, "Name");
                model.Description = parseTextField(form, "Description");

                Company.Update<AssetTypeLevel>(model);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated, model.Name), id.ToString(), "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
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