using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionSchedule : BaseCreatedAndUpdatedObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int FusionID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public DayOfWeek Day { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public TimeSpan Time { get; set; }

        [DataMember]
        public bool FullRefresh { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }
    }
}
