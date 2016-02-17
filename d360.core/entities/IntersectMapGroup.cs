using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectMapGroup : BaseObject
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int IntersectMapID { get; set; }
        [DataMember]
        public int GroupNumber { get; set; }
    }
}
