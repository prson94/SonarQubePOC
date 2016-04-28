using d360.core.entities.Contracts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class DomainClassification: BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }
    }

    public class DomainItemXref : BaseIntObject, IIntObject
    {        
        public int HouseDomainItemID { get; set; }
        public int DomainItemID { get; set; }
    }

    public class DomainXrefGridItem: BaseObject
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int HouseDomainItemID { get; set; }
        [DataMember]
        public int DomainItemID { get; set; }
        [DataMember]
        public string HouseCode { get; set; }
        [DataMember]
        public string Code { get; set; }
        [DataMember]
        public int SourceArtifactID { get; set; }
        [DataMember]
        public string SourceArtifactName { get; set; }
        [DataMember]
        public string ListName { get; set; }
    }
}
