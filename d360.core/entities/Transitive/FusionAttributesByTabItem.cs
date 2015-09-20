using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class FusionAttributesByTabItem
    {
        public int ID { get; set; }
        public int? ParentID { get; set; }
        public string SourceID { get; set; }
        public string Path { get; set; }
        public string Fields { get; set; }
        public string Relationships { get; set; }
    }
}
