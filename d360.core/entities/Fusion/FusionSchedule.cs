using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionSchedule : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public DayOfWeek Day { get; set; }

        [DataMember]
        public TimeSpan Time { get; set; }

        [DataMember]
        public bool FullRefresh { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }
    }
}
