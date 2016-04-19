using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    public class RuleModel : BaseIntObject
    {
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), StringLength(250)]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "RuleType_Name", Description = "RuleType_Description")]
        public RuleType RuleType { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "SourceID_Name", Description = "SourceID_Description"), StringLength(250)]
        public string SourceID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "RuleDimensionID_Name", Description = "RuleDimensionID_Description")]
        public int? RuleDimensionID { get; set; }

        [DataMember, ForeignKey("RuleDimensionID")]
        public RuleDimension Dimension { get; set; }
    }


    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Rule, "Rule")]
    public class Rule : RuleModel, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ISearchable, IUpdatedMetadata
    {
        #region Properties

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion
    }
}
