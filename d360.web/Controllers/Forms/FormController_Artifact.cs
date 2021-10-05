using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
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
        [Route("Diagram_AddFields"), NonNullableParameters]
        public JsonResult Diagram_AddFields(int at, int p)
        {
            var list = new List<EditableField>();

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.TaskType, at).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ArtifactID</param>
        [Route("Diagram_EditFields"), NonNullableParameters]
        public JsonResult Diagram_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.Assets.Where(x => x.ObjectID == id && x.Object == "Task").Include(x => x.AssetType).FirstOrDefault();

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { FieldName = "AssetTypeUid", FieldType = DataType.Hidden.ToString(), Value = a.AssetType.uid.ToString() });

            list = (
                loadDynamicFields(
                    SystemObjects.Task.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.TaskType, a.AssetType.ObjectID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Task, id).ToList(),
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="at">ArtifactTypeID</param>
        /// <param name="p">ParentID</param>
        [Route("Asset_AddFields"), NonNullableParameters]
        public JsonResult Asset_AddFields(int at, int p)
        {
            if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, at, Permission.AddAsset))
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();

            var intersectType = Company.Filter<IntersectTypeDetail>(i =>
                i.Object == "ArtifactType" &&
                i.ObjectID == at &&
                i.PredicateType.Value == PredicateType.InterTypeHierarchy
            ).SingleOrDefault();

            var parentType = Company.GetParentType(at, SystemObjects.ArtifactType);
            if (intersectType != null)
            {
                var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                var parents = Company.Query<SelectListItem>(
           $@"select 
                                    lower(convert(nvarchar(36), A.uid)) as Value, 
                                    AN.DisplayPath as Text 	
                                        from Asset A 
                                        inner join graph.AssetNodeDisplayPath AN on AN.ID = A.ID 
                                        where A.AssetTypeID = {parentType.ID}").OrderBy(i => i.Text).ToList();
                list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "ParentUid", Name = $"Parent {pluralize.Singularize(intersectType.SubjectName)}", FieldType = DataType.Lookup.ToString(), Value = ((p > 0) ? p.ToString() : null), Items = parents, VirtualScroll = parents.Count > 9, ItemSize = 20 });
            }

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ArtifactType, at).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ArtifactID</param>
        [Route("Asset_EditFields"), NonNullableParameters]
        public JsonResult Asset_EditFields(SystemObjects type, SystemObjects obj, int id)
        {
            if (!Company.HasAssetPermission(obj, id, Permission.EditAsset))
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.Assets.Where(x => x.ObjectID == id && x.Object == obj.ToString()).Include(x => x.AssetType).FirstOrDefault();

            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { FieldName = "AssetTypeUid", FieldType = DataType.Hidden.ToString(), Value = a.AssetType.uid.ToString() });

            var parentType = Company.GetParentType(a.AssetType.ObjectID, type);


            if (PluralCultureHelper.IsNeutralCultureEnglish())
            {
                if (parentType != null)
                {
                    var parent = Company.GetParentObject(a.ObjectID, obj);

                    var pluralize = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                    var parents = Company.Query<SelectListItem>(
           $@"select 
                                    lower(convert(nvarchar(36), A.uid)) as Value, 
                                    AN.DisplayPath as Text 	
                                        from Asset A 
                                        inner join graph.AssetNodeDisplayPath AN on AN.ID = A.ID 
                                        where A.AssetTypeID = {parentType.ID}").OrderBy(i => i.Text).ToList();


                    list.Add(new EditableField
                    {
                        Row = 1,
                        Column = 1,
                        Required = true,
                        FieldName = "ParentUID",
                        Name = $"Parent {pluralize.Singularize(parentType.Name)}",
                        FieldType = DataType.Lookup.ToString(),
                        Value = ((parent != null) ? (parent.uid.ToString() ?? "").ToLower() : ""),
                        Items = parents,
                        VirtualScroll = parents.Count > 9,
                        ItemSize = 20,
                        ReadOnly = a.AssetType?.CanEditParent == false ? true : false,
                        TooltipText = a.AssetType?.CanEditParent == false ? "The parent for this type of asset cannot be changed once set" : null
                    });
                }
            }

            list = (
                loadDynamicFields(
                    obj.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(type, a.AssetType.ObjectID).ToList(),
                    Company.GetFieldRelationsByObject(obj, id).ToList(),
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion

        #region AssetType

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
                    {
                        parentUid = parentAssetType.uid;
                    }
                }

                var loadPredicates = false;
                var parentPredicateType = PredicateType.InterTypeHierarchy;
                var loadParentReferenceItemOptions = false;

                var ot = SystemObjects.ArtifactType;
                var appendTitle = "";
                switch (@class)
                {
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
                    {
                        return jsonNetException($"No asset type ID provided (id parameter).", HttpStatusCode.BadRequest);
                    }

                    var assetType = Company.GetById<AssetType>(id.Value);

                    if (assetType == null)
                    {
                        return jsonNetException($"No asset type found for the ID {id.Value}", HttpStatusCode.NotFound);
                    }

                    var style = assetType.AssetTypeStyle;

                    model = new AssetTypeEditorModel()
                    {
                        AssetType = new AssetTypeUpsert()
                        {
                            Uid = assetType.uid,
                            ParentUid = parentUid,
                            AutoDisplayDescription = assetType.AutoDisplayDescription,
                            Description = assetType.Description,
                            DisplayFormat = assetType.DisplayFormat,
                            Class = @class,
                            UseAsTransformation = assetType.UseAsTransformation,
                            Notes = assetType.Notes,
                            IconStyle = new IconStyleInsert()
                            {
                                ForeColor = ((style != null) ? style.IconForeColor : "#FFF"),
                                BackColor = ((style != null) ? style.IconBackColor : "#000"),
                                Icon = ((style != null) ? style.Icon : null)
                            },
                            Hierarchy = new HierarchyInsert()
                            {
                                MaximumDepth = 1,
                                PredicateUid = null
                            },
                            AutoDisplayParent = assetType.AutoDisplayParent,
                            FlowObjectType = assetType.FlowObjectType,
                            CanEditParent = assetType.CanEditParent
                        },
                        Tokens = Company.Filter<FieldType>(i => i.Object == assetType.Object && i.ObjectID == assetType.ObjectID && !this.limitedFieldTypes.Contains(i.Type)).OrderBy(i => i.FriendlyName).Select(i => new PrimeSelectItem { label = i.FriendlyName, value = "{" + i.Name + "}" }).ToList()
                    };

                    switch (@class)
                    {
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
                            var o = Company.GetById<OrganizationType>(assetType.ObjectID);
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
                            if (model.Tokens != null)
                            {
                                model.Tokens.Add(new PrimeSelectItem { label = "Code", value = "{Code}" });
                            }
                            break;
                        case AssetTypeClass.Rule:
                        case AssetTypeClass.Diagram:
                            model.AssetType.Name = assetType.Name;
                            model.AssetType.Description = assetType.Description;
                            model.AssetType.DisplayFormat = assetType.DisplayFormat;
                            break;
                    }
                    model.AssetType.Object = ot.ToString();
                    model.FormName = string.Format(FormInfo.Add_Asset_Type_Title, appendTitle);
                    model.FormDescription = string.Format(FormInfo.Add_Asset_Type_Directions, appendTitle.ToLower());

                    if (@class == AssetTypeClass.BusinessAsset || @class == AssetTypeClass.TechnicalAsset || @class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.Reference)
                    {
                        var intersectType = Company.Filter<IntersectType>(i =>
                            i.Object == assetType.Object &&
                            i.ObjectID == assetType.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).FirstOrDefault();


                        if (@class == AssetTypeClass.Model || @class == AssetTypeClass.Policy || @class == AssetTypeClass.Reference) //If model or policy you must always have a predicate to load.
                        {
                            loadPredicates = true;
                        }

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

                        AssetType = new AssetTypeUpsert()
                        {
                            DisplayFormat = "{Name}",
                            Class = @class,
                            Object = ot.ToString(),
                            ParentUid = parentUid,
                            IconStyle = new IconStyleInsert()
                            {
                                BackColor = "#000",
                                ForeColor = "#FFF",
                                Icon = null
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

        #endregion
    }
}