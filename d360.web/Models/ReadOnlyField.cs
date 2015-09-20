using d360.core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class ReadOnlyField
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string Value { get; set; }
        
        [DataMember]
        public string FieldDescription { get; set; }

        [DataMember]
        public List<string> MultipleValues { get; set; }

        [DataMember]
        public int? Row { get; set; }

        [DataMember]
        public int? Column { get; set; }
        
        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string TooltipType { get; set; } 

        [DataMember]
        public string TooltipContext { get; set; }

        [DataMember]
        public int? TooltipID { get; set; }

        [DataMember]
        public string TooltipUrl { get; set; }
    }
}