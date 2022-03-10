using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldJsonProperty : BaseCreatedAndUpdatedObject
    {
        [Key, Column(Order = 1), DataMember]
        public long FieldID { get; set; }

        [Key, Column(Order = 2), DataMember]
        public int Position { get; set; }

        [Key, Column(Order = 3), DataMember, MaxLength(250)]
        public string Parent { get; set; }

        [Key, Column(Order = 4), DataMember, MaxLength(250)]
        public string Name { get; set; }

        [DataMember, MaxLength(500)]
        public string Path { get; set; }

        [DataMember]
        public bool IsArray { get; set; }

        [DataMember, MaxLength(2500)]
        public string Value { get; set; }

        [IgnoreDataMember]
        public Field Field { get; set; }
    }
}
