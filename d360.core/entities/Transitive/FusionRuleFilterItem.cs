using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class FusionRuleFilterItem
    {
        public int FusionRuleFilterID { get; set; }
        public string Type { get; set; }
        public int FieldTypeID { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
}
