using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{    
    [DataContract(Namespace = NAMESPACE)]
    public class RuleResult : BaseCreatedIntObject, IIntObject, ICreatedObject
    {        
        [DataMember]
        public int RuleID { get; set; }

        [DataMember]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public int RowsPassed { get; set; }

        [DataMember]
        public int RowsFailed { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal PassFraction { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal FailFraction { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public bool Passed { get; set; }

        [DataMember]
        public int? FusionAttributeID { get; set; }

        [IgnoreDataMember]
        public virtual Rule Rule { get; set; }
    }
}
