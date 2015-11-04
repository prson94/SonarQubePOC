using System.Collections.Generic;

namespace d360.core.entities
{
    public class AttributeTreeItem
    {
        public string ID { get; set; }
        public string ParentID { get; set; }
        public string Name { get; set; }

        public List<AttributeTreeItem> Items { get; set; }
    }
}
