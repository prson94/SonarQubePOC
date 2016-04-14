using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.core;
using d360.core.entities;
using d360.web.Models;
using d360.core.enums;
using d360.model;

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
            var attributes = Company.GetAttributeAndIntersectHierarchyByObject(type, id).ToList();
            var categories = attributes.Where(i => !string.IsNullOrEmpty(i.AttributeTypeCategory)).Select(i => i.AttributeTypeCategory).Distinct().OrderBy(i => i).ToList();
            //categories.Insert(0, "Enterprise-wide Attributes");

            var list = new List<AttributeHierarchyItem>();

            var rootNode = new AttributeHierarchyItem { ID = "EC", IsCategory = true, ObjectTypeName = "", ShowNameInTree = true, Name = "Enterprise-wide", ObjectType = type.ToString(), ObjectID = id, IsTechnical = false, ParentObjectType = type.ToString(), ParentObjectID = id };
            rootNode.Items.AddRange(nestHierarchyNode(attributes, null, null));
            list.Add(rootNode);

            foreach (var c in categories)
            {
                var cNode = new AttributeHierarchyItem { ID = c, IsCategory = true, ObjectTypeName = "", ShowNameInTree = true, Name = c, ObjectType = type.ToString(), ObjectID = id, IsTechnical = false, ParentObjectType = type.ToString(), ParentObjectID = id };
                cNode.Items.AddRange(nestHierarchyNode(attributes, null, c));
                list.Add(cNode);
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AttributesForIntersect(int id, int? parentID = null)
        {
            var sType = SystemObjects.Intersect.ToString();
            var attributes = Company.Filter<AttributeDetail>(i => i.ObjectType == sType && i.ObjectID == id).ToList();

            var list = expandNode(null, parentID, attributes, 1, 3);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Json

        /// <summary>
        /// Gets the list of columns for the relationship overlay grid based on the input owner
        /// </summary>        
        /// <param name="intersectTypeID">The object ID to get list of attributes for.</param>        
        /// <returns></returns>
        public JsonResult RelationshipAttributesFieldList(int intersectTypeID)
        {         
            var permissions = Company.GetPermissions(SystemObjects.IntersectType, intersectTypeID).ToList();
            
            var list = new List<GridDynamicAttributeField>();
                        
            if (Company.HasClaimInCurrentPermissionList(permissions, Claim.Read, ClaimObject.Attribute))
            {                
                {
                    list = Company.Filter<AttributeTypeRelation>(i => i.ObjectType == "IntersectType" && i.ObjectID == intersectTypeID)
                        .Select(i => new GridDynamicAttributeField {
                            attributeID = i.AttributeTypeID,
                            label = i.AttributeType.Name,
                            allowMultiple = i.AllowMultipleEntries,
                            description = i.AttributeType.Description,
                            fieldCount = Company.Count<FieldType>(p => p.ObjectID == i.AttributeTypeID && p.Object == "AttributeType")
                        }).OrderByDescending(i => i.label).ToList();

                    //determine if any of these attributes are complex
                    var attributeIDList = list.Select(i => i.attributeID).ToList();

                    var complexAttributes = Company.Filter<AttributeType>(t => attributeIDList.Contains(t.ParentID.Value)).Select(t => t.ParentID).ToList();

                    foreach (var attr in list)
                    {
                        if (complexAttributes.Contains(attr.attributeID)) attr.isComplex = true;
                    }
                }                
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets list of allowed actions that you can take on the selected attribute or node.
        /// </summary>
        /// <param name="type">The object type to get actions for.</param>
        /// <param name="id">The object ID to get actions for.</param>
        /// <param name="owner">The type of the object that owns this attribute.</param>
        /// <param name="ownerID">The ID of the object that owns this attribute.</param>
        /// <param name="attributeID">The current or new parent attribute ID.</param>
        /// <returns>A list of available actions as JSON.</returns>
        public JsonResult AttributeActions(SystemObjects type, int id, SystemObjects owner, int ownerID, int? attributeID)
        {
            Company.Database.Log = message => System.Diagnostics.Trace.Write(message);

            var permissions = Company.GetPermissions(owner, ownerID).ToList();

            var list = new List<ToolbarItem>();

            if (attributeID.HasValue)
            {
                if (Company.HasClaimInCurrentPermissionList(permissions, Claim.Update, ClaimObject.Attribute))
                    list.Add(new ToolbarItem { Title = "", Icon = "pencil", Uri = string.Format("/form/EditAttribute?id={0}", attributeID.Value) });
                if (Company.HasClaimInCurrentPermissionList(permissions, Claim.Delete, ClaimObject.Attribute))
                    list.Add(new ToolbarItem { Title = "", Icon = "trash-o", Uri = string.Format("/form/DeleteAttribute?id={0}", attributeID.Value) });
            }

            IQueryable<AttributeType> types = null;
            if (Company.HasClaimInCurrentPermissionList(permissions, Claim.Create, ClaimObject.Attribute))
            {
                if (type == SystemObjects.Attribute)
                {
                    types = Company.GetById<core.entities.Attribute>(id, i => i.AttributeType).AttributeType.Children.OrderBy(i => i.Name).AsQueryable();
                    //types= (
                    //       from t in Company.AttributeTypes
                    //       join a in Company.Attributes on t.ParentID equals a.AttributeTypeID
                    //       where a.ID == id
                    //       select t
                    //       ).OrderBy(i => i.Name).AsQueryable();
                }
                else
                {
                    var detail = Company.GetObjectDetail(type, id);
                    var sType = type.ToString();
                    var _id = sType.EndsWith("Type") ? detail.ID : detail.TypeID;

                    var usedIDs = Company.Filter<core.entities.Attribute>(i => i.ObjectType == sType && i.ObjectID == id).Select(i => i.AttributeTypeID).ToList();

                    if (!sType.EndsWith("Type")) sType += "Type";
                    types = Company.Filter<AttributeTypeRelation>(r => r.ObjectType == sType && r.ObjectID == _id && (r.AllowMultipleEntries || !usedIDs.Contains(r.AttributeTypeID))).Select(r => r.AttributeType).OrderBy(t => t.Name);
                }

                if (types.Count() > 0)
                {
                    var addItem = new ToolbarItem { Context = "nullform", Icon = "plus", Title = "" };
                    foreach (var t in types)
                    {
                        var uri = string.Format("/form/AddAttribute?typeID={0}&objectType={1}&objectID={2}", t.ID, owner, ownerID);
                        if (attributeID.HasValue) uri += "&parentID=" + attributeID.Value;
                        var a = new ToolbarItem { Title = "Add " + t.Name, Icon = "plus", Uri = uri };
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

        List<AttributeNode> expandNode(AttributeNode node, int? parentID, List<AttributeDetail> attributes, int level, int maxLevels = 10)
        {
            var list = new List<AttributeNode>();
            foreach (var attr in attributes.Where(i => i.ParentID == parentID).OrderBy(i => i.Name))
            {
                bool isFolderNode = attributes.Any(i => i.ParentID == attr.ID);
                var objectType = (SystemObjects)System.Enum.Parse(typeof(SystemObjects), attr.ObjectType);

                AttributeNode d = loadNode(attr.FormattedValue,
                                            attr.ID,
                                            isFolderNode,
                                            objectType,
                                            "D",
                                            attr.ObjectID,//factObjectID,
                                            attr.Name,
                                            attr.AttributeTypeID
                                            );

                if (level <= maxLevels)
                {
                    d.Children.AddRange(expandNode(d, attr.ID, attributes, level + 1));
                }

                list.Add(d);
            }

            return list;
        }
        
        AttributeNode loadNode(string text, int id, bool isFolderNode, SystemObjects objectType, string nodeType, int objectID, string attributeTypeName, int? attributeTypeID)
        {
            return new AttributeNode { 
                Text = text,
                ID = id,
                AttributeType = attributeTypeName,
                IsFolderAttribute = isFolderNode,
                AttributeTypeID = (attributeTypeID.HasValue) ? attributeTypeID.Value : 0,
                ObjectType = objectType.ToString(),
                ObjectID = objectID
            };
        }

        #endregion
    }
}
