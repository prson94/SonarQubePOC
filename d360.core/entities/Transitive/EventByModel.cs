using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class EventByModel: BaseObject
    {
        //public int ObjectID { get; set; }
        //public string ObjectType { get; set; }
        //public string ObjectName { get; set; }
        public int EventID { get; set; }
        public int RuleID { get; set; }
        public string Rule { get; set; }
        public string EventName { get; set; }
        public int EventGroupID { get; set; }
        public string SourceID { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
    }
}
