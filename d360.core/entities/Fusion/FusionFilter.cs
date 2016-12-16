using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionFilter : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int FusionID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int FusionAttributeTypeID { get; set; }

        [DataMember]
        public string Filter { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttributeType FusionAttributeType { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }
    }
}
