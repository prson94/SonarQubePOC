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
    [DataContract(Namespace = NAMESPACE), Table("JobActivity", Schema = "utility")]
    public class JobActivity: BaseObject
    {
        [Key]
        public Guid ID { get; set; }
        public string Name { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime DateStopped { get; set; }
        public string Status { get; set; }
    }
}
