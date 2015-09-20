using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Plugins
{
    [Table("FusionType", Schema = "plugin")]
    public class FusionType: BaseIntObject
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }

        [ForeignKey("FusionTypeID")]
        public ICollection<FieldType> FieldTypes { get; set; }
    }
}
