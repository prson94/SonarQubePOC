using d360.core.enums;
using System;
using System.Collections.Generic;
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
        public string Name { get; set; }
        public string Inverse { get; set; }
        public PredicateType Type { get; set; }
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
        public string Icon { get; set; }
        public PredicateState State { get; set; }
    }
}
