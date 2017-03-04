using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class FieldFilterModel
    {
        public string Group { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
    }
}
