using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.core;
using d360.core.entities;
using d360.web.Models;
using d360.core.enums;
using d360.model;
using d360.web.Models.Attributes;
using System.Net;

namespace d360.web.Controllers
{
    [RoutePrefix("attributes"), Authorize]
    public class AttributesController : BaseController
    {
        #region DI

        public AttributesController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Partials

        [Route("hierarchy/{type}/{id:int}")]
        public JsonResult AttributeHierarchyItemsByObject(SystemObjects type, int id)
        {
            var list = new List<AttributeHierarchyItem>();
            if (Company.HasAssetPermission(type, id, Permission.ReadAttributes))
            {
                var attributes = Company.GetAttributeAndIntersectHierarchyByObject(type, id).ToList();
                var categories = attributes.Where(i => !string.IsNullOrEmpty(i.AttributeTypeCategory)).Select(i => i.AttributeTypeCategory).Distinct().OrderBy(i => i).ToList();
                
                var rootNode = new AttributeHierarchyItem { ID = "EC", IsCategory = true, ObjectTypeName = "", ShowNameInTree = true, Name = "Enterprise-wide", ObjectType = type.ToString(), ObjectID = id, IsTechnical = false, ParentObjectType = type.ToString(), ParentObjectID = id };
                rootNode.Items.AddRange(nestHierarchyNode(attributes, null, null));
                list.Add(rootNode);

                foreach (var c in categories)
                {
                    var cNode = new AttributeHierarchyItem { ID = c, IsCategory = true, ObjectTypeName = "", ShowNameInTree = true, Name = c, ObjectType = type.ToString(), ObjectID = id, IsTechnical = false, ParentObjectType = type.ToString(), ParentObjectID = id };
                    cNode.Items.AddRange(nestHierarchyNode(attributes, null, c));
                    list.Add(cNode);
                }
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
            

        #endregion

        #region Json

        [Route("actions/{type}/{id:int}/{owner}/{ownerID:int}/{attributeID:int?}")]
        public JsonResult AttributeActions(SystemObjects type, int id, SystemObjects owner, int ownerID, int? attributeID = null)
        {            
            var objectDetail = Company.GetObjectDetail(owner.ToString(), ownerID);
                        
            var list = new List<ToolbarItemNg>();
            var hasModifyPermission = Company.HasAssetPermission(objectDetail.AssetID.GetValueOrDefault(), Permission.ModifyAttributes);

            if (attributeID.HasValue)
            {
                var p = new
                {
                    attributeID = attributeID.Value
                };
                
                if (hasModifyPermission)
                    list.Add(new ToolbarItemNg { Title = "edit attribute", Icon = "pencil",  Action = "edit", Params = p });
                if (Company.HasAssetPermission(objectDetail.AssetID.GetValueOrDefault(), Permission.DeleteAttributes))
                    list.Add(new ToolbarItemNg { Title = "delete attribute", Icon = "trash-o", Action = "delete", Params = p });
            }

            IQueryable<AttributeType> types = null;
            if (hasModifyPermission)
            {
                if (type == SystemObjects.Attribute)
                {
                    types = Company.GetById<core.entities.Attribute>(id, i => i.AttributeType).AttributeType.Children.OrderBy(i => i.Name).AsQueryable();
                }
                else
                {
                    var sType = type.ToString();
                    var detail = Company.GetObjectDetail(sType, id);
                    int _id = id;

                    if (detail != null)
                    {
                        _id = sType.EndsWith("Type") ? detail.ID : detail.TypeID;
                    }
                    
                    var usedIDs = Company.Filter<core.entities.Attribute>(i => i.ObjectType == sType && i.ObjectID == id).Select(i => i.AttributeTypeID).ToList();

                    if (!sType.EndsWith("Type")) sType += "Type";
                    types = Company.Filter<AttributeTypeRelation>(r => r.ObjectType == sType && r.ObjectID == _id && (r.AllowMultipleEntries || !usedIDs.Contains(r.AttributeTypeID))).Select(r => r.AttributeType).OrderBy(t => t.Name);
                }

                if (types.Count() > 0)
                {
                    var addItem = new ToolbarItemNg { Icon = "plus", Title = "" };
                    foreach (var t in types)
                    {
                        var p = new
                        {
                            typeID = t.ID,
                            objectType = owner,
                            objectID = ownerID,
                            parentID = attributeID ?? 0
                        };

                        var uri = string.Format("/form/AddAttribute?typeID={0}&objectType={1}&objectID={2}", t.ID, owner, ownerID);
                        if (attributeID.HasValue) uri += "&parentID=" + attributeID.Value;
                        var a = new ToolbarItemNg { Title = "Add " + t.Name, Icon = "plus", Action = "add", Params = p };
                        addItem.Items.Add(a);
                    }
                    if (addItem.Items.Count > 0)
                    {
                        list.Add(addItem);
                    }
                }
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("fulltypes")]
        public JsonNetResult GetFullTypes()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            return new JsonNetResult
            {
                Data = Company.Table<AttributeType>().OrderBy(i => i.Parent.Name).ThenBy(i => i.Name),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }


        [Route("categories")]
        public JsonNetResult GetCategories(int? parentID)
        {
            var res = Company.Table<AttributeTypeCategory>().OrderBy(i => i.Name).Select(i => new { title = i.Name, value = i.ID.ToString() }).ToList();

            if (!parentID.HasValue)
            {
                res.Insert(0, new { title = "Enterprise-wide", value = "0" });
            }

            return new JsonNetResult
            {
                Data = res,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("types")]
        public JsonNetResult GetTypes()
        {
            return new JsonNetResult
            {
                Data = Company.Table<AttributeType>().OrderBy(i => i.Parent.Name).ThenBy(i => i.Name).Select(i => new { i.ID, i.Name, i.ParentID, expanded = true }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Private Methods

        List<AttributeHierarchyItem> nestHierarchyNode(List<AttributeHierarchyItem> attributes, AttributeHierarchyItem node, string category)
        {
            var list = new List<AttributeHierarchyItem>();
            var subset = attributes.AsQueryable();
            subset = (node != null) ? subset.Where(i => i.ParentID == node.ID) : subset.Where(i => string.IsNullOrEmpty(i.ParentID));

            foreach (var attr in subset.Where(i => ((node == null) && i.AttributeTypeCategory == category) || ((node != null))).OrderBy(i => i.ObjectTypeName).ThenBy(i => i.Name))
            {
                attr.Items.AddRange(nestHierarchyNode(attributes, attr, category));

                list.Add(attr);
            }

            return list;
        }

        #endregion
    }
}
