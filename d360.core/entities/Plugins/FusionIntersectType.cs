using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Plugins
{
    [Table("plugin.FusionIntersectType")]
    public class FusionIntersectType: BaseObject
    {
        [DataMember, Key, Column(Order=1)]
        public int StartFusionAttributeTypeID { get; set; }
        [DataMember, Key, Column(Order = 2)]
        public int EndFusionAttributeTypeID { get; set; }
        [DataMember]
        public int FusionTypeID { get; set; }
        [DataMember]
        public bool ReadOnly { get; set; }
    }
}
