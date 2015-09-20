using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("FusionSynchronizationInstance", Schema = "utility")]
    public class FusionSynchronizationInstance : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public int FusionID { get; set; }

        [Key, Column(Order = 2), DataMember]
        public DateTime DateStarted { get; set; }

        [DataMember]
        public DateTime? DateCompleted { get; set; }

        [DataMember]
        public bool Processed { get; set; }
    }
}
