using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Field : BaseObject
    {
        [DataMember]
        public long? AssetID { get; set; }

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

        [DataMember]
        public int UpdatedBy { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }

        [IgnoreDataMember]
        public FieldType FieldType { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class FieldApiModel : BaseObject
    {
        [Key, Column(Order = 1)]
        public long AssetID { get; set; }

        [Key, Column(Order = 2)]
        public int FieldTypeID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Value { get; set; }
    }
}
