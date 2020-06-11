using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Plugins
{
    [Table("ClientFusionType", Schema = "plugin")]
    public class ClientFusionType : BaseObject
    {
        [DataMember]
        public int ClientID { get; set; }
        [DataMember]
        public int FusionTypeID { get; set; }
    }
}
