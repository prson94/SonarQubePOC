using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LookupAllocation : BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int FieldTypeID { get; set; }

        [Column(Order = 2), DataMember, Key, StringLength(250)]
        public string FieldTypeName { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int LookupTypeID { get; set; }

        [Column(Order = 4), DataMember, Key, StringLength(250)]
        public string LookupTypeName { get; set; }

        [Column(Order = 5, TypeName = "varchar"), DataMember, Key, StringLength(25)]
        public string LookupObjectType { get; set; }

        [Column(Order = 6), DataMember, Key]
        public int ObjectID { get; set; }

        [Column(Order = 7), DataMember, Key, StringLength(250)]
        public string ObjectName { get; set; }

        [Column(Order = 8), DataMember, Key, StringLength(250)]
        public string ObjectTypeName { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string ObjectType { get; set; }

        [DataMember, StringLength(2500)]
        public string ObjectUrl { get; set; }
    }
}
