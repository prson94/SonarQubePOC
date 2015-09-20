using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    /// <summary>
    /// Corresponds to the data row retrieved from GetFlattenedFusionAttributesByType procedure.
    /// </summary>
    public partial class FlattenedFusionAttributeByType
    {
        public int ID { get; set; }

        //public int FusionAttributeTypeID { get; set; }

        //public int FusionID { get; set; }

        //public int FusionTypeID { get; set; }

        public string Name { get; set; }

        public string FormattedValue { get; set; }

        public int FusedItems { get; set; }
    }
}
