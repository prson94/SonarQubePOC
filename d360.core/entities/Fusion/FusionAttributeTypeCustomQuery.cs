using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributeTypeCustomQuery : BaseIntObject
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public int FusionAttributeTypeID { get; set; }

        [DataMember]
        public string Query { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttributeType FusionAttributeType { get; set; }
    }
}
