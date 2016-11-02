using d360.core.entities.Contracts;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SiteNav : BaseIntObject, IIntObject
    {
        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        public string Route { get; set; }

        [DataMember]
        public int? SortOrder { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }

        [DataMember]
        public string Icon{ get; set; }

        [DataMember]
        public string Title{ get; set; }
    }
}
