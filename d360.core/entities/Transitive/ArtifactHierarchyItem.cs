using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace d360.core.entities
{
    public class HierarchyItem
    {
        public int ID { get; set; }
        public int? ParentID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int Level { get; set; }
    }
}
