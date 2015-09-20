using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    /// <summary>
    /// Loaded by calling the stored procedure GetFieldNamesByObjectType
    /// </summary>
    public class FieldNameByObjectType
    {
        public string Name { get; set; }
        public bool IsCustomField { get; set; }
    }
}
