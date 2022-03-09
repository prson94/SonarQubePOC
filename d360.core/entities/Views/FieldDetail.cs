using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldDetail : BaseObject
    {
        [DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string FriendlyName { get; set; }

        [DataMember]
        public long AssetID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string FormattedValue { get; set; }
    }
}
