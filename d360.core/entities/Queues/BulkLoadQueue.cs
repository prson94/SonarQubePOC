using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Queues
{
    [DataContract(Namespace = NAMESPACE), Table("BulkLoad", Schema = "queue")]
    public class BulkLoadQueue : BaseGuidObject
    {
        [DataMember]
        public int LoadID { get; set; }

        [DataMember]
        public string MachineAssigned { get; set; }

        [DataMember]
        public bool HasError { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int NumberOfRetries { get; set; }
    }
}
