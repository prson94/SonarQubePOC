using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class RawIntersectContextMap
    {
        public int IntersectClassificationID { get; set; }
        public string IntersectClassification { get; set; }
        public int IntersectRoleID { get; set; }
        public string IntersectRole { get; set; }
        public int IntersectID { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Url { get; set; }

        public string Contexts { get; set; }
    }
}
