using d360.core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class ReadOnlySection
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<ReadOnlyField> Fields { get; set; }
    }
}