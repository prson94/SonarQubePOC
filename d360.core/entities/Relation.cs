using d360.core.entities.Contracts;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Relation : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public int SubjectID { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int RelationTypeID { get; set; }

        [DataMember]
        public bool Deleted { get; set; }

        [IgnoreDataMember]
        public virtual RelationType RelationType { get; set; }
    }
}
