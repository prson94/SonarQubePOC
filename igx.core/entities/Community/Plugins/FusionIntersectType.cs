using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

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
        [DataMember]
        public int? PredicateType { get; set; }
    }
}
