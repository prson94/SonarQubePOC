using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        [DataMember, StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember, StringLength(250)]
        public string AttributeTypeCategory { get; set; }

        [DataMember]
        public bool ShowNameInTree { get; set; }
    }
}
