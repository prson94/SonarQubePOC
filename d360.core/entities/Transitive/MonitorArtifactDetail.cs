using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MonitorArtifactDetail: BaseObject
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public int ArtifactTypeID { get; set; }
        public string ArtifactType { get; set; }
        public string Criticality { get; set; }
        public int All { get; set; }
        public int Open { get; set; }
        public int Closed { get; set; }
        public int Assigned { get; set; }
    }
}
