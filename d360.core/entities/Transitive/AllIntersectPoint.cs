using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public partial class AllIntersectPoint
    {
        public int IntersectID { get; set; }

        public int IntersectTypeID { get; set; }

        public int ID { get; set; }

        public string Name { get; set; }

        public int TypeID { get; set; }

        public string Type { get; set; }

        public string TypeName { get; set; }

        public int? ParentID { get; set; }

        public string ParentName { get; set; }

        public int? ParentTypeID { get; set; }

        public string ParentType { get; set; }

        public string ParentTypeName { get; set; }

        public string Role { get; set; }
    }
}
