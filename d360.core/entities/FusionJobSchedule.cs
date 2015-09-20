using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

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
