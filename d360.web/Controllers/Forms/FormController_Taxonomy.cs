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
        #region Taxonomy

        #region Field Generation

        /// <param name="t">TaxonomyTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("Taxonomy_AddFields"), NonNullableParameters]
        public JsonResult Taxonomy_AddFields(int t, int p)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, t, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var parentUid = p > 0 ? Company.GetAssetUid(p, SystemObjects.Taxonomy).ToString() : "";
            list.Add(new EditableField { FieldName = "ParentUid", FieldType = DataType.Hidden.ToString(), Value = parentUid.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, t).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">TaxonomyID</param>
        [Route("Taxonomy_EditFields"), NonNullableParameters]
        public JsonResult Taxonomy_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Taxonomy, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var taxonomy = Company.Query<dynamic>(@"
                                                    select	A.ID as AssetID,
                                                            A.UID as Uid,
		                                                    A.ObjectID,
		                                                    T.ID as TypeID,
                                                            T.ObjectID as TaxonomyTypeID,
                                                            T.HierarchyMaximumDepth as MaximumDepth,
		                                                    P.TextPath,
		                                                    L.Level
                                                    from	Asset A
		                                                    inner join AssetType T on T.ID = A.AssetTypeID
		                                                    cross apply dbo.GetAssetTextPathById(A.ID, '/') P
		                                                    cross apply dbo.GetAssetLevelById(A.ID) L
                                                    where	A.Object = 'Taxonomy' and A.ObjectID = @id
                                                    ", new { id }).SingleOrDefault();
            if (taxonomy != null)
            {

                var parent = Company.GetParentObject(taxonomy.ObjectID, SystemObjects.Taxonomy);

                var parents = Company.Query<dynamic>(@"
                                select	A.ObjectID as ID,
		                                P.TextPath as Name,
                                        A.uid as Uid,
		                                coalesce(LV.[Level], 1) as [Level]
                                from	Asset A
                                        inner join AssetType T on T.ID = A.AssetTypeID and T.Object = 'TaxonomyType' and T.ObjectID = @t
		                                cross apply dbo.GetAssetTextPathById(A.ID, '/') P
                                        cross apply dbo.GetAssetLevelById(A.ID) LV
                                where coalesce(LV.[Level], 1) <= @currentLevel 
                                option (maxrecursion 100)",
    new { t = taxonomy.TaxonomyTypeID, currentLevel = taxonomy.Level ?? 1 }).Select(i => new { i.Uid, i.Name }).ToList();

                var thisEntry = parents.FirstOrDefault(i => i.Uid == taxonomy.Uid);

                if (thisEntry != null)
                    parents.RemoveAll(i => i.Name.StartsWith(thisEntry.Name));

                var parentItems = parents.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = $"{i.Uid}",
                    Selected = (parent != null ? (i.Uid == parent.uid) : false)
                }).ToList();
                parentItems.Insert(0, new SelectListItem { Text = "- Root -", Value = Guid.Empty.ToString(), Selected = (parent == null) });

                list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = taxonomy.Uid.ToString() });
                list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ParentUid", Name = "Parent Model", FieldDescription = FormInfo.Taxonomy_ChangeParent_Warning, FieldType = DataType.Lookup.ToString(), Items = parentItems, Value = ((parent != null) ? parent.uid.ToString() : Guid.Empty.ToString()) });
                list = (
                     loadDynamicFields(
                         SystemObjects.Taxonomy.ToString(),
                         id,
                         list,
                         Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, (int)taxonomy.TaxonomyTypeID).ToList(),
                         Company.GetFieldRelationsByObject(SystemObjects.Taxonomy, id).ToList(),
                         3
                     )
                 );
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion

        #region TaxonomyType

        [HttpDelete, Route("DeleteTaxonomyType")]
        public JsonResult DeleteTaxonomyType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var model = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "TaxonomyType").SingleOrDefault();
                if (model == null) throw new NotFoundException("taxonomy type");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(SystemObjects.TaxonomyType, id);

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
                if (assetType == null) throw new NotFoundException("asset type");
                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type level");

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
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "TaxonomyType").SingleOrDefault();
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

        [HttpPut, ValidateInput(false), Route("EditTaxonomyTypeLevel")]
        public JsonResult EditTaxonomyTypeLevel(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("taxonomy type");

                var id = parseIntField(form, "ID");
                var level = parseIntField(form, "Level");
                var assetType = Company.Filter<AssetType>(x => x.ObjectID == id && x.Object == "TaxonomyType").SingleOrDefault();
                var model = Company.Filter<AssetTypeLevel>(i => i.AssetTypeID == assetType.ID && i.Level == level).SingleOrDefault();
                if (model == null) throw new NotFoundException("taxonomy type level");

                if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.ModifyAsset))
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