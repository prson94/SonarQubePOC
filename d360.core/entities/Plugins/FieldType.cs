using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Plugins
{
    [Table("FieldType", Schema = "plugin")]
    public class FieldType: BaseIntObject
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string FriendlyName { get; set; }
        [DataMember]
        public string Type { get; set; }

        [ForeignKey("FieldTypeID")]
        public ICollection<EventType> EventTypes { get; set; }

        [ForeignKey("FieldTypeID")]
        public ICollection<FusionAttributeType> FusionAttributeTypes { get; set; }
        
        [ForeignKey("FieldTypeID")]
        public ICollection<FusionType> FusionTypes { get; set; }
    }
}
