using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypeRoleRelation : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int IntersectTypeID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int IntersectTypeRoleID { get; set; }

        [DataMember]
        public string Side1Label { get; set; }

        [DataMember]
        public string Side2Label { get; set; }

        [IgnoreDataMember]
        public virtual IntersectType IntersectType { get; set; }

        [IgnoreDataMember]
        public virtual IntersectTypeRole IntersectTypeRole { get; set; }
    }
}
