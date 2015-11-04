using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class DomainAllocationDetail : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public string DomainID { get; set; }

        [Key, Column(Order = 2), DataMember]
        public int AttributeTypeID { get; set; }

        [Key, Column(Order = 3), DataMember]
        public string LocationType { get; set; }

        [Key, Column(Order = 4), DataMember]
        public string Location { get; set; }

        [Key, Column(Order = 5), DataMember]
        public string Type { get; set; }

        [Key, Column(Order = 6), DataMember]
        public string Name { get; set; }
    }
}
