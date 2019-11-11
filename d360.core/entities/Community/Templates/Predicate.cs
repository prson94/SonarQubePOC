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
    public enum PredicateState
    {
        Active = 1,
        Inactive = 2
    }

    [Table("Predicate", Schema = "community")]
    public class Predicate : BaseTemplateCreatedAndUpdatedGuidObject
    {
        [StringLength(250)]
        public string Name { get; set; }
        [StringLength(250)] 
        public string Inverse { get; set; }
        public PredicateType Type { get; set; }
        [StringLength(7)] 
        public string BackColor { get; set; }
        [StringLength(7)] 
        public string ForeColor { get; set; }
        [StringLength(50)] 
        public string Icon { get; set; }
        public PredicateState State { get; set; }
    }
}
