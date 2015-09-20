using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace d360.core.entities
{
    public class RelationNode
    {
        public int IntersectTypeID { get; set; }

        public int? IntersectID { get; set; }

        public string HierarchyID { get; set; }

        public string ParentHierarchyID { get; set; }

        public int? TargetID { get; set; }

        public string TargetType { get; set; }

        public string TargetName { get; set; }

        public string TargetUrl { get; set; }

        public string TypeName { get; set; }

        public bool ReadOnly { get; set; }

        public bool HasChildren { get; set; }

        public List<RelationNode> Items { get; set; }
    }
}