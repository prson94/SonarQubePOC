using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;


namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class DomainSourceType : BaseObject
    {
        [Key]
        public int ArtifactTypeID { get; set; }
    }
}
