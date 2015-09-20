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
        [Key, Column(Order=1)]
        public int CompanyID { get; set; }
        [Key, Column(Order = 2)]
        public string ObjectType { get; set; }
        [Key, Column(Order = 3)]
        public int ObjectID { get; set; }
        [Key, Column(Order = 4)]
        public string Action { get; set; }
        [Key, Column(Order = 5)]
        public DateTime Date { get; set; }

        public string Data { get; set; }

        public string MachineAssigned { get; set; }

    }
}
