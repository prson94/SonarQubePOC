using d360.core.entities.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeRelationRule : BaseIntObject, IIntObject, IUpdatedMetadata, ICreatedMetadata, IUIDMetadata
    {
        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Context { get; set; }

        [DataMember]
        public bool IsVisible { get; set; }

        [DataMember]
        public bool ApplyToType { get; set; }

        [DataMember]
        public DateTime? LastRunOn { get; set; }

        private string _rawDefinition = "";
        [IgnoreDataMember]
        public string Definition
        {
            get {
                return _rawDefinition;
            }
            set {
                _rawDefinition = value;
            }
        }

        [NotMapped, DataMember]
        public ResponsibilityRuleDefinition StructuredDefinition { get; set; }

        public void SetDefinitionFromRaw()
        {
            StructuredDefinition = JsonConvert.DeserializeObject<ResponsibilityRuleDefinition>(Definition);
        }

        public void SetRawFromDefinition()
        {
            Definition = JsonConvert.SerializeObject(StructuredDefinition);
        }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }


        public int? UpdatedBy { get; set; }

        [DataMember]
        public Guid? UID { get; set; }

        [ForeignKey("ResponsibilityTypeID")]
        public virtual ResponsibilityType ResponsibilityType { get; set; }

    }

    public class ResponsibilityRuleDefinition
    {
        public List<ResponsibilityRuleDefinitionWhen> When { get; set; }
        public ResponsibilityRuleDefinitionThen Then { get; set; }
    }

    public class ResponsibilityRuleDefinitionWhen
    {
        public string CheckType { get; set; }

        public int FieldTypeID { get; set; }
        public string FieldTypeName { get; set; }
        public string Value { get; set; }

        public int IntersectTypeID { get; set; }
        public string TargetObject { get; set; }
        public int TargetObjectID { get; set; }
        public Guid? IntersectTypeUID { get; set; }
        public Guid? AssetUID { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum ResponsibilityMatchType
    {
        [Name("and"), EnumMember(Value = "and"), ReadOnly(false), Description("")]
        And = 1,
        [Name("or"), EnumMember(Value = "or"), ReadOnly(false), Description("")]
        Or = 2
    }

    public class ResponsibilityRuleDefinitionThen
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public List<ResponsibilityRuleDefinitionWhen> Conditions { get; set; }
        public ResponsibilityMatchType MatchType { get; set; } = ResponsibilityMatchType.And;
    }
}
