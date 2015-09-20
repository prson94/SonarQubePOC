using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Queue", Schema = "utility")]
    public class QueueItem : BaseObject
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ID { get; set; }
        
        public string ObjectType { get; set; }
        
        public int ObjectID { get; set; }
        
        public string Action { get; set; }
        
        public DateTime Date { get; set; }

        public string Data { get; set; }

        public string MachineAssigned { get; set; }

    }
}
