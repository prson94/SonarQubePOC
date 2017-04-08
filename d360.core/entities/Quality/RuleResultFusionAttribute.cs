using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RuleResultFusionAttribute : BaseObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public int RuleResultID { get; set; }

        [DataMember]
        public string FusionAttribute { get; set; }

        [DataMember]
        public int? FusionAttributeID { get; set; }

        [IgnoreDataMember]
        public virtual RuleResult RuleResult { get; set; }
    }
}
