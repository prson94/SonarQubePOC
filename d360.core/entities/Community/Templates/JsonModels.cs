using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Community.Templates
{
    public class Field
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class AssetTypeVersionLevel
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
