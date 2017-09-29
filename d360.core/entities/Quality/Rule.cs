using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class RuleModel : Dictionary<string, string>
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public enums.RuleStatus Status { get; set; }

        [DataMember]
        public decimal? Threshold { get; set; }

        [DataMember]
        public int? RuleDimensionID { get; set; }

        [DataMember]
        public string SourceID { get; set; }
        
    }


    [DataContract(Namespace = NAMESPACE)]
    public class Rule : BaseCreatedAndUpdatedIntObject, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ICreatedMetadata, IUpdatedMetadata, IDisplayValueObject
    {
        public Rule()
        {
            Visible = true;
        }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string DisplayValue { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string KeyHash { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string FieldHash { get; set; }

        [DataMember]
        public enums.RuleStatus Status { get; set; }

        [DataMember]
        public decimal Threshold { get; set; }

        [DataMember]
        public int? RuleDimensionID { get; set; }

        [DataMember]
        public int RuleTypeID { get; set; }

        [DataMember, ForeignKey("RuleTypeID")]
        public RuleType RuleType { get; set; }

        [DataMember, ForeignKey("RuleDimensionID")]
        public RuleDimension Dimension { get; set; }

        public bool Visible { get; set; }

        [ForeignKey("RuleID")]
        public virtual ICollection<RuleImplementation> RuleImplementations { get; set; }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.RuleType, Object = SystemObjects.Rule, TypeID = RuleTypeID };
        }
    }
}
