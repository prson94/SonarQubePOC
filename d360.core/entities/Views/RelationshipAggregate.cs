using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RelationshipAggregate : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public string Group { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int TypeID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string Type { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public bool Critical { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string IconBackColor { get; set; }

        [DataMember]
        public int Count { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }
    }
}
