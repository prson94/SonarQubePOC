using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionJobSchedule : BaseObject
    {
        [DataMember, Key]
        public int FusionID { get; set; }

        [DataMember]
        public string IncrementType { get; set; }

        [DataMember]
        public int Increment { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }
    }
}
