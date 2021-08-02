using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldLookupValue: BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int FieldTypeID { get; set; }

        [Column(Order = 2, TypeName = "varchar"), DataMember, Key, StringLength(25)]
        public string LookupObjectType { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int? LookupObjectID { get; set; }

        [Column(Order = 4), DataMember, Key]
        public Guid AssetUid { get; set; }

        [Column(Order = 5), DataMember, Key]
        public int Value { get; set; }

        [Column(Order = 6), DataMember, Key]
        public string Text { get; set; }

        [Column(Order = 7), DataMember]
        public string DisplayText { get; set; }
    }
}
