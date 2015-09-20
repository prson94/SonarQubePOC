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
    public class FusionJobHistory : BaseIntObject, IIntObject
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public int PercentComplete { get; set; }

        [DataMember]
        public string CurrentStatusMessage { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }
    }
}
