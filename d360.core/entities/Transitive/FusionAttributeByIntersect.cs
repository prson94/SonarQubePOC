using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    /// <summary>
    /// Corresponds to the data row retrieved from GetFusionAttributesByIntersect procedure.
    /// </summary>
    public partial class FusionAttributeByIntersect
    {
        public int ID { get; set; }

        public int FusionIntersectID { get; set; }

        public int FusionTypeID { get; set; }

        public int FusionAttributeTypeID { get; set; }

        public string AttributePath { get; set; }

        public string TypePath { get; set; }

        public string FusionName { get; set; }
    }
}
