using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Fusion", Schema = "queue")]
    public class QueueFusionItem : BaseObject
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }
                
        public int FusionID { get; set; }
        
        public string Data { get; set; }

        public string MachineAssigned { get; set; }

        public bool? HasError { get; set; }

        public string ErrorMessage { get; set; }

        public int NumberOfRetries { get; set; }
    }
}
