using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Community.Templates
{
    public enum PackageVersionState
    { 
        Draft = 1,
        Published = 2,
        Archived = 3
    }

    [Table("PackageVersion", Schema = "community")]
    public class PackageVersion : BaseTemplateCreatedAndUpdatedGuidObject
    {
        public Guid PackageUid { get; set; }
        public PackageVersionState State { get; set; }
    }
}
