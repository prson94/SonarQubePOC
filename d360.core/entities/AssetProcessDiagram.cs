using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AssetProcessDiagram", Schema = "dbo")]
    public class AssetProcessDiagram : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public long AssetId { get; set; }
        
        [DataMember]
        public string Diagram { get; set; }
    }
}
