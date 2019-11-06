using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Community.Templates
{
    [Table("PackageDeploymentHistory", Schema = "community")]
    public class PackageDeploymentHistory : BaseTemplateGuidObject
    {
        [DataMember]
        public DateTime ChangedOn { get; set; }
    }
}
