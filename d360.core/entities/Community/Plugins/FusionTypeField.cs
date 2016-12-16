using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Plugins
{
    [Table("FusionTypeField", Schema = "plugin")]
    public class FusionTypeField : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int FusionTypeID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string Name { get; set; }

        [DataMember]
        public string FriendlyName { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public bool IsListable { get; set; }

        [ForeignKey("FusionTypeID")]
        public virtual FusionType FusionType { get; set; }
    }
}
