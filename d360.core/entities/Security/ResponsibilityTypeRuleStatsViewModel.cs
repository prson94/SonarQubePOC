using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeRuleStatsViewModel : BaseObject
    {
        [DataMember]
        public int AssignedUsers { get; set; }

        [DataMember]
        public int AssignedAssets { get; set; }
    }
}
