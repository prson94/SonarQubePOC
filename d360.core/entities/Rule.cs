using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;

namespace d360.core.entities
{
    public class RuleModel : BaseIntObject
    {
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "RuleType_Name", Description = "RuleType_Description")]
        public RuleType RuleType { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "SourceID_Name", Description = "SourceID_Description")]
        public string SourceID { get; set; }
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
