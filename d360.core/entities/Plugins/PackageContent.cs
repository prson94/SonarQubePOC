using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace d360.core.entities.Plugins
{
    [Table("PackageContent", Schema = "plugin")]
    public class PackageContent: BaseObject
    {
        [Key, Column(Order = 1)]
        public int PackageID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string FileName { get; set; }

        [IgnoreDataMember, ForeignKey("PackageID")]
        public virtual Package Package { get; set; }
    }
}
