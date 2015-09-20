using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public partial class IntersectionPoint
    {
        public int? IntersectID { get; set; }

        public int? IntersectTypeID { get; set; }

        public string TargetUrl { get; set; }

        public int? TargetID { get; set; }

        public string TargetName { get; set; }

        public string TargetType { get; set; }

        public int? SourceID { get; set; }

        public string SourceName { get; set; }

        public string TargetDescription { get; set; }

        public bool ReadOnly { get; set; }

        public string Role { get; set; }
    }
}
