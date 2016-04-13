using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeLookupValue : BaseObject
    {
        [Column(Order = 1, TypeName = "varchar"), DataMember, Key, StringLength(8)]
        public string LookupObjectType { get; set; }

        [Column(Order = 2), DataMember, Key]
        public int? LookupObjectID { get; set; }

        [Column(Order = 3), DataMember, Key]
        public string Name { get; set; }
    }
}
