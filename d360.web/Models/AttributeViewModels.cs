using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core;
using d360.core.entities;

namespace d360.web.Models
{
    public class AttributeNode
    {
        public AttributeNode()
        {
            Children = new List<AttributeNode>();
        }

        public string AttributeType { get; set; }
        public int AttributeTypeID { get; set; }
        public int ID { get; set; }
        public int FusionIntersectID { get; set; }
        public string Text { get; set; }
        public int ObjectID { get; set; }
        public string ObjectType { get; set; }
        public bool IsFolderAttribute { get; set; }
        public List<AttributeNode> Children { get; set; }
    }
}