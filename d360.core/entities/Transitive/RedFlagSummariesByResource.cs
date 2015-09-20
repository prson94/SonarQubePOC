using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Transitive
{
    public class RedFlagSummariesByResource
    {
        public int TypeID { get; set; }
        public string Type { get; set; }
        public string TypeName { get; set; }
        public int CriticalRelationshipCount { get; set; }
        public int RedFlagCount { get; set; }
    }
}
