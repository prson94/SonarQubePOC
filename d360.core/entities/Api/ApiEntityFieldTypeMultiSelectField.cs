using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("EntityFieldTypeMultiSelectField", Schema = "api")]
    public class ApiEntityFieldTypeMultiSelectField : BaseIntObject, IIntObject
    {
        [DataMember]
        public int EntityFieldTypeID { get; set; }

        [DataMember]
        public int FieldTypeID { get; set; }

    }
}
