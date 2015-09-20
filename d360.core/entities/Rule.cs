using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    public class RuleModel : BaseIntObject
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "RuleType_Name", Description = "RuleType_Description")]
        public RuleType RuleType { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Policy_Name", Description = "Policy_Description")]
        public int PolicyID { get; set; }
    }


    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Rule, "Rule")]
    public class Rule : RuleModel, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ISearchable, IUpdatedMetadata
    {
        #region Properties

        [DataMember, ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        #region Navigation Properties

        [IgnoreDataMember]
        public virtual Policy Policy { get; set; }

        #endregion
    }
}
