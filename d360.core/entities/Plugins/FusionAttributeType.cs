using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
        public ICollection<FieldType> FieldTypes { get; set; }
    }
}
