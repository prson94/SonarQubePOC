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
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;
using d360.core.helpers;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
                #region Artifact

        #region Field Generation

        /// <param name="at">ArtifactTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("Artifact_AddFields"), NonNullableParameters]
        public JsonResult Artifact_AddFields(int at, int p)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, at, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
                        
            var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                i.Object == "ArtifactType" &&
                i.ObjectID == at &&
                i.PredicateType.Value == PredicateType.InterTypeHierarchy
            ).SingleOrDefault();

            
            if (intersectType != null)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                var parents = Company.Query<SelectListItem>($"select convert(nvarchar(36), A.uid) as Value, AD.DisplayValue as Text from Asset a inner join AssetDisplayValue AD on AD.AssetID = A.ID inner join AssetType AT on A.AssetTypeID = AT.ID where AT.[Object] = 'ArtifactType' and AT.[ObjectID] = {intersectType.SubjectID}").OrderBy(i => i.Text).ToList();
                list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentUid", Name = $"Parent {pluralize.Singularize(intersectType.SubjectName)}", FieldType = DataType.Lookup.ToString(), Value = ((p > 0) ? p.ToString() : null), Items = parents });
            }

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ArtifactType, at).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <param name="id">ArtifactID</param>
        [Route("Artifact_EditFields"), NonNullableParameters]
        public JsonResult Artifact_EditFields(int id)
        {
            if (!Company.HasAssetPermission(SystemObjects.Artifact, id, Permission.ModifyAsset))
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();                        
            var a = Company.Assets.Where(x => x.ObjectID == id && x.Object == SystemObjects.Artifact.ToString()).Include(x => x.AssetType).FirstOrDefault();

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { FieldName = "AssetTypeUid", FieldType = DataType.Hidden.ToString(), Value = a.AssetType.uid.ToString() });

            var parentType = Company.GetParentType(a.AssetType.ObjectID, SystemObjects.ArtifactType);
            

            if (PluralCultureHelper.IsNeutralCultureEnglish())
            {
                if (parentType != null)
                {
                    var parent = Company.GetParentObject(a.ObjectID, SystemObjects.Artifact);
                   
                    var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                    var parents = Company.Query<SelectListItem>($"select lower(convert(nvarchar(36), A.uid)) as Value, AD.DisplayValue as Text from Asset A inner join AssetType AT on A.AssetTypeID = AT.ID inner join AssetDisplayValue AD on A.ID = AD.AssetID   where AT.[Object] = 'ArtifactType' and AT.[ObjectID] = {parentType.ObjectID}").OrderBy(i => i.Text).ToList();
                    list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentUID", Name = $"Parent {pluralize.Singularize(parentType.Name)}", FieldType = DataType.Lookup.ToString(), Value = ((parent != null) ? (parent.uid.ToString()??"").ToLower() : ""), Items = parents });
                }
            }

            list = (
                loadDynamicFields(
                    SystemObjects.Artifact.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.ArtifactType, a.AssetType.ObjectID).ToList(), 
                    Company.GetFieldRelationsByObject(SystemObjects.Artifact, id).ToList(), 
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }
                        
        [HttpGet, Route("Artifact_SimilarItems"), NonNullableParameters]
        public JsonNetResult Artifact_SimilarItems(int typeID, string query)
        {
            //escape wildcards
            query = query.Replace("_", "[_]");
            query = query.Replace("%", "[%]");
            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(QueryConstants.SimilarItems, new { type = new DbString { Value = "Artifact", IsAnsi = true, IsFixedLength = true, Length = 50 }, typeID, query }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        #endregion

        #region Form Get/Post

        [AjaxValidateAntiForgeryToken, HttpPost, Route("RequestCertification")]
        public JsonResult RequestCertification(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("artifact");

                int id = parseIntField(form, "ID");
                var asset = Company.Assets.Where(x => x.ObjectID == id && x.Object == SystemObjects.Artifact.ToString()).Include(x => x.AssetType).FirstOrDefault();
                
                if (asset == null) throw new NotFoundException("artifact");
                
                Company.RequestObjectCertification(SystemObjects.Artifact, asset.ObjectID, SystemObjects.ArtifactType, asset.AssetType.ObjectID);

                return jsonSuccess("Request successfully created.", "", "add", HttpStatusCode.Created);
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

    
        [HttpDelete, ActionName("ArtifactType"), Route("ArtifactType"), NonNullableParameters]
        public JsonResult DeleteArtifactType(int id)
        {
            try
            {
                var assetType = Company.AssetTypes.FirstOrDefault(a => a.Object == "ArtifactType" && a.ObjectID == id);
                if (assetType == null) throw new NotFoundException("artifact type");

                if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var intersectType = Company.Filter<IntersectType>(i =>
                    i.Object == "ArtifactType" &&
                    i.ObjectID == assetType.ObjectID &&
                    i.Predicate.Type == PredicateType.InterTypeHierarchy
                ).SingleOrDefault();

                if (intersectType != null)
                {
                    Company.Delete(SystemObjects.IntersectType, intersectType.ID);
                }

                Company.Delete(SystemObjects.ArtifactType, id);

                dynamic custom = new
                {                    
                    Name = assetType.Name,
                    action = "delete"              
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

        #endregion

        #region AssetType

        #region Form Get/Post

        [HttpGet, ActionName("AssetType"), Route("AssetType")]
        public JsonNetResult GetAssetType(AssetTypeClass @class, int? id = null, int? parentID = null)
        {
            try
            {
                var model = new AssetTypeEditorModel();

                Guid? parentUid = null;
                if (parentID.HasValue && parentID > 0)
                {
                    var parentAssetType = Company.Query<AssetType>("select * from AssetType where class = @class and ObjectID = @parentID", new { @class, parentID }).FirstOrDefault();
                    if (parentAssetType != null)
                        parentUid = parentAssetType.uid;
                }
                  
                var loadPredicates = false;
                var parentPredicateType = PredicateType.InterTypeHierarchy;
                var loadParentReferenceItemOptions = false;

                var ot = SystemObjects.ArtifactType;
                var appendTitle = "";
                switch (@class)
                {
                    case AssetTypeClass.FusionAttribute:
                        ot = SystemObjects.FusionAttributeType;
                        appendTitle = FormInfo.FusionAttributeType;
                        break;
                    case AssetTypeClass.BusinessAsset:
                    case AssetTypeClass.TechnicalAsset:
                        ot = SystemObjects.ArtifactType;
                        appendTitle = FormInfo.ArtifactType;
                        break;
                    case AssetTypeClass.Model:
                        ot = SystemObjects.TaxonomyType;
                        appendTitle = FormInfo.TaxonomyType;
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                        break;
                    case AssetTypeClass.Organization:
                        ot = SystemObjects.OrganizationType;
                        appendTitle = FormInfo.OrganizationType;
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                        break;
                    case AssetTypeClass.Policy:
                        ot = SystemObjects.PolicyType;
                        appendTitle = FormInfo.PolicyType;
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                        break;
                    case AssetTypeClass.Reference:
                    case AssetTypeClass.ReferenceItemType:
                        ot = SystemObjects.ReferenceItemType;
                        appendTitle = "Reference List";                        
                        loadParentReferenceItemOptions = true;
                        break;
                }

                if (id.HasValue)
                {
                    if (!id.HasValue)
                        return jsonNetException($"No asset type ID provided (id parameter).", HttpStatusCode.BadRequest);

                    var assetType = Company.GetById<AssetType>(id.Value);

                    if (assetType == null)
                        return jsonNetException($"No asset type found for the ID {id.Value}", HttpStatusCode.NotFound);

                    var style = Company.Filter<ObjectStyle>(i => i.ObjectType == assetType.Object && i.ObjectID == assetType.ObjectID).FirstOrDefault();

                    model = new AssetTypeEditorModel()
                    {
                        AssetType = new AssetTypeInsert()
                        {
                            Uid = assetType.uid,
                            ParentUid = parentUid,
                            AutoDisplayDescription = assetType.AutoDisplayDescription,
                            Class = @class,
                            UseAsTransformation = assetType.UseAsTransformation,
                            Notes = assetType.Notes,
                            IconStyle = new IconStyleInsert()
                            {
                                ForeColor = ((style != null) ? style.IconForeColor : "#FFF"),
                                BackColor = ((style != null) ? style.IconBackColor : "#000")
                            },
                            Hierarchy = new HierarchyInsert()
                            {
                                MaximumDepth = 1,
                                PredicateUid = null
                            }

                        },
                        Tokens = Company.Filter<FieldType>(i => i.Object == assetType.Object && i.ObjectID == assetType.ObjectID && !this.limitedFieldTypes.Contains(i.Type)).OrderBy(i => i.FriendlyName).Select(i => new PrimeSelectItem { label = i.FriendlyName, value = "{" + i.Name + "}" }).ToList()
                    };
                    
                    switch (@class)
                    {
                        case AssetTypeClass.FusionAttribute:
                            var f = Company.GetById<FusionAttributeType>(model.AssetType.ObjectID);
                            model.AssetType.Name = f.Name;
                            break;
                        case AssetTypeClass.BusinessAsset:
                        case AssetTypeClass.TechnicalAsset:
                            model.AssetType.CanOwnFusion = (@class == AssetTypeClass.BusinessAsset) ? assetType.CanOwnFusion : false;
                            model.AssetType.AutoDisplayDescription = assetType.AutoDisplayDescription;
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            break;
                        case AssetTypeClass.Model:                            
                            model.AssetType.Hierarchy.MaximumDepth = assetType.HierarchyMaximumDepth;
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            break;
                        case AssetTypeClass.Organization:
                            var o = Company.GetById<OrganizationType>(model.AssetType.ObjectID);
                            model.AssetType.Hierarchy.MaximumDepth = 1;
                            model.AssetType.Name = o.Name;
                            model.AssetType.Description = o.Description;
                            model.AssetType.DisplayFormat = o.DisplayFormat;
                            break;
                        case AssetTypeClass.Policy:
                            model.AssetType.Hierarchy.MaximumDepth = assetType.HierarchyMaximumDepth;
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            break;
                        case AssetTypeClass.Reference:
                        case AssetTypeClass.ReferenceItemType:
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Notes = assetType.Notes;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            if (model.Tokens != null) model.Tokens.Add(new PrimeSelectItem { label = "Code", value = "{Code}" });
                            break;
                    }
                    model.AssetType.Object = ot.ToString();
                    model.FormName = string.Format(FormInfo.Add_Asset_Type_Title, appendTitle);
                    model.FormDescription = string.Format(FormInfo.Add_Asset_Type_Directions, appendTitle.ToLower());

                    if (@class == AssetTypeClass.FusionAttribute || @class == AssetTypeClass.BusinessAsset || @class == AssetTypeClass.TechnicalAsset || @class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.Reference)
                    {
                        var intersectType = Company.Filter<IntersectType>(i =>
                            i.Object == assetType.Object &&
                            i.ObjectID == assetType.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).FirstOrDefault();


                        if (@class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.Reference) //If model or policy you must always have a predicate to load.
                            loadPredicates = true;

                        if (intersectType != null)
                        {
                            loadPredicates = true;

                            if (intersectType.SubjectUid.HasValue)
                            {
                                model.AssetType.ParentUid = intersectType.SubjectUid;
                            }
                            else
                            {
                                var parentAssetType = Company.AssetTypes.FirstOrDefault(x => x.Object == intersectType.Subject && x.ObjectID == intersectType.SubjectID);
                                model.AssetType.ParentUid = parentAssetType.uid;
                            }


                            model.AssetType.Hierarchy.PredicateUid = intersectType.Predicate.UID;
                        }
                    }
                }
                else
                {
                    loadPredicates = true;

                    model = new AssetTypeEditorModel()
                    {

                        AssetType = new AssetTypeInsert()
                        {
                            DisplayFormat = "{Name}",
                            Class = @class,
                            Object = ot.ToString(),
                            ParentUid = parentUid,
                            IconStyle = new IconStyleInsert()
                            {
                                BackColor = "#000",
                                ForeColor = "#FFF"
                            },
                            Hierarchy = new HierarchyInsert()
                            {
                                PredicateUid = null,
                                MaximumDepth = 1
                            }

                        },
                        Tokens = new List<PrimeSelectItem>() { new PrimeSelectItem { label = "Name", value = "{Name}" } }
                    };



                    if (@class == AssetTypeClass.Reference)
                    {
                        model.AssetType.DisplayFormat = "{Code}";
                        model.Tokens.Clear(); // remove the name token for reference item type it isnt created by default.
                        model.Tokens.Add(new PrimeSelectItem { label = "Code", value = "{Code}" });
                    }
                    model.FormName = string.Format(FormInfo.Edit_Asset_Type_Title, appendTitle);
                    model.FormDescription = string.Format(FormInfo.Add_Asset_Type_Directions, appendTitle.ToLower());
                }

                if (loadPredicates)
                {
                    model.Predicates = Company.Filter<Predicate>(i => i.Type == parentPredicateType).Select(i => new PrimeSelectItem { label = i.Inverse, value = i.UID.ToString() }).ToList();
                }

                if (loadParentReferenceItemOptions)
                {
                    if (model.AssetType != null && model.AssetType.ObjectID > 0)
                    {
                        var parents = Company.Query<PrimeSelectItem>(@"select a.ObjectUid as value, a.Name as label from  assettype a where a.[object] = 'ReferenceItemType'  and a.objectid != @id
                                                                    and  not exists(
                                                                    select  1 from IntersectType i where i.object = 'ReferenceItemType' and i.SubjectId = @id and i.objectid = a.objectid)
                                                                    order by Name", new { id = model.AssetType.ObjectID }).ToList();
                        model.Parents = parents;
                    }
                    else
                    {
                        var parents = Company.Query<PrimeSelectItem>("select LOWER(CAST(uid AS char(36))) as value, Name as label from assettype where [object] = 'ReferenceItemType' order by Name").ToList();
                        model.Parents = parents;
                    }
                    model.Parents?.Insert(0, new PrimeSelectItem() { label = "", value = "" });
                }

                return new JsonNetResult
                {
                    Data = model,
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [HttpDelete, ActionName("AssetType"), Route("AssetType"), NonNullableParameters]
        public JsonResult DeleteAssetType(int id)
        {
            try
            {
                var at = Company.GetById<AssetType>(id);
                if (at == null) throw new NotFoundException("asset type");

                SystemObjects ot;

                if (!Enum.TryParse<SystemObjects>(at.Object, out ot))
                    throw new GenericException(HttpStatusCode.BadRequest, "Missing Object Type", "No valid type provided. Please check your request and try again.");

                if (!Company.CurrentResourceIsAdmin)
                    throw new UnauthorizedException(FormInfo.Permisions_Error_Delete, FormInfo.Permisions_Error_Delete);

                var parentPredicateType = PredicateType.InterTypeHierarchy;

                if (at.Class == AssetTypeClass.Model || at.Class == AssetTypeClass.Policy)
                {
                    parentPredicateType = PredicateType.IntraTypeHierarchy;
                }

                var intersectType = Company.Filter<IntersectType>(i =>
                    i.Object == at.Object &&
                    i.ObjectID == at.ObjectID &&
                    i.Predicate.Type == parentPredicateType
                ).SingleOrDefault();

                if (intersectType != null)
                {
                    Company.Delete(SystemObjects.IntersectType, intersectType.ID);
                }

                Company.Delete(ot, at.ObjectID);

                dynamic custom = new
                {
                    Name = at.Name,
                    action = "delete"
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

        #endregion

        #endregion
    }
}