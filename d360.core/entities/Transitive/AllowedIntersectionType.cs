using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public partial class AllowedIntersectionType
    {
        public int IntersectTypeID { get; set; }

        public string TargetType { get; set; }

        public int? TargetTypeID { get; set; }

        public string TargetName { get; set; }

        public int? ParentIntersectID { get; set; }
    }
}
