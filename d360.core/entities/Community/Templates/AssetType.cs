using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Community.Templates
{
    [Table("AssetType", Schema = "community")]
    public class AssetType : BaseTemplateGuidObject
    {
        [StringLength(250)]
        public string Name { get; set; }
        public AssetTypeClass Class { get; set; }
        public bool Hierarchical { get; set; }
        public SystemObjects Object { get; set; }
        public int ObjectID { get; set; }
    }
}
