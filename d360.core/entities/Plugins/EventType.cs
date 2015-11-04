using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Plugins
{
    [Table("EventType", Schema = "plugin")]
    public class EventType: BaseIntObject
    {
        [DataMember]
        public int? ParentID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public bool MarkAsResolvedOnSynch { get; set; }

        [ForeignKey("EventTypeID")]
        public ICollection<FieldType> FieldTypes { get; set; }
    }
}
