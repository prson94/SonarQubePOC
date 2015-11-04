using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AttributeOwner", Schema="fusion")]
    public class FusionAttributeOwnerDetail: BaseObject
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public int? ParentObjectID { get; set; }

        [DataMember]
        public string ParentObjectType { get; set; }

        [DataMember]
        public string ParentName { get; set; }

        [DataMember]
        public int RelationshipOwnerObjectID { get; set; }

        [DataMember]
        public string RelationshipOwnerObjectType { get; set; }

        [DataMember]
        public string RelationshipOwnerName { get; set; }
    }
}
