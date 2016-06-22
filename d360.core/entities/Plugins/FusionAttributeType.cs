using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Plugins
{
    [Table("FusionAttributeType", Schema = "plugin")]
    public class FusionAttributeType: BaseIntObject
    {
        [DataMember]
        public int? ParentID { get; set; }
        [DataMember]
        public int FusionTypeID { get; set; }
        [DataMember]
        public string Name { get; set; }

        [ForeignKey("FusionAttributeTypeID")]
        public ICollection<FusionAttributeTypeField> FusionAttributeTypeFields { get; set; }
    }
}
