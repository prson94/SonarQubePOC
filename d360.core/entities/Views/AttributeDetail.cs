using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AttributeDetail: BaseIntObject
    {
        [DataMember]
        public int AttributeTypeID { get; set; }

        [DataMember]
        public string FormattedValue{ get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public string AttributeTypeCategory { get; set; }
    }
}
