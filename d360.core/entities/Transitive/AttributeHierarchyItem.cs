using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class AttributeHierarchyItem
    {
        public AttributeHierarchyItem()
        {
            expanded = true;
            IsCategory = false;
            Items = new List<AttributeHierarchyItem>();
        }

        public string ID { get; set; }
        public string ParentID { get; set; }
        public int TypeID { get; set; }
        public string ObjectTypeName { get; set; }
        public string ObjectType { get; set; }
        public int ObjectID { get; set; }
        public string ParentObjectType { get; set; }
        public int ParentObjectID { get; set; }
        public string TargetObjectType { get; set; }
        public int TargetObjectID { get; set; }
        public string Name { get; set; }
        public string AttributeTypeCategory { get; set; }

        public bool IsCategory { get; set; }
        public bool IsTechnical { get; set; }
        public bool expanded { get; set; }

        public List<AttributeHierarchyItem> Items { get; set; }
    }
}
