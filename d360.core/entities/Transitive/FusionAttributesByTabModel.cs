using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class FusionAttributesByTabModel
    {
        public List<Dictionary<string, object>> Models { get; set; }
        public List<UIColumnDefinition> Columns { get; set; }
    }
}
