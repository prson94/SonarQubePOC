using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldJsonProperty : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public long FieldID { get; set; }

        [Key, Column(Order = 2), DataMember]
        public int Position { get; set; }

        [Key, Column(Order = 3), DataMember]
        public string Parent { get; set; }

        [Key, Column(Order = 4), DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public bool IsArray { get; set; }

        [DataMember]
        public string Value { get; set; }
                
        [IgnoreDataMember]
        public Field Field { get; set; }
    }
}