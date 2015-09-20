using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    /// <summary>
    /// Corresponds to the data row retrieved from GetFlattenedFusionAttributeDefinitionByType procedure.
    /// </summary>
    public partial class FlattenedFusionAttributeDefinitionByType
    {
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
    }
}
