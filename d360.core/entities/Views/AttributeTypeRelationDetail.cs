using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class AttributeTypeRelationDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int AttributeTypeID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string ObjectType { get; set; }

        [DataMember]
        public bool Required { get; set; }

        [DataMember]
        public bool AllowMultipleEntries { get; set; }
    }
}
