using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class RawAttributeTreeItem
    {
        public string ID { get; set; }
        public string ParentID { get; set; }
        public string Name { get; set; }
    }

    public class AttributeTreeItem
    {
        public string ID { get; set; }
        public string ParentID { get; set; }
        public string Name { get; set; }

        public List<AttributeTreeItem> Items { get; set; }
    }
}
