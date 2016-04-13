using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Field : BaseObject
    {
        [Column(Order = 1, TypeName = "varchar"), DataMember, Key, StringLength(25)]
        public string ObjectType { get; set; }

        [Column(Order = 2), DataMember, Key]
        public int ObjectID { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int FieldTypeID { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string FormattedValue { get; set; }

        [IgnoreDataMember]
        public FieldType FieldType { get; set; }
    }
}
