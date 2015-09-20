using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Plugins
{
    [Table("Package", Schema = "plugin")]
    public class Package : BaseIntObject
    {
        [DataMember]
        public string Version { get; set; }
        [DataMember]
        public string Hash { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public DateTime DateUpdated { get; set; }
        [DataMember]
        public string Component { get; set; }

        [DataMember, ForeignKey("PackageID")]
        public ICollection<PackageContent> PackageContents { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual ICollection<Company> Companies { get; set; }
    }
}
