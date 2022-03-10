using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Global_FieldAudit", Schema = "reporting")]
    public class AuditField : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long AuditID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int FieldTypeID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string FieldName { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public int Version { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string PreviousValue { get; set; }
    }
}
