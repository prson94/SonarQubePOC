using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Rule", Schema = "fusion")]
    public class FusionRule : BaseIntObject, IUpdatedMetadata
    {
        [DataMember]
        public bool Enabled { get; set; }

        [DataMember]
        [StringLength(500)]
        public string Description { get; set; }

        [DataMember, StringLength(25), Column(TypeName ="varchar")]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int FusionID { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
               
        private ICollection<FusionRuleStep> _steps;

        [IgnoreDataMember, ForeignKey("RuleID")]
        public virtual ICollection<FusionRuleStep> FusionRuleSteps
        {
            get { return _steps ?? (_steps = new Collection<FusionRuleStep>()); }
            set { _steps = value; }
        }

        [ForeignKey("FusionID")]
        public virtual Fusion Fusion { get; set; }
    }
}
