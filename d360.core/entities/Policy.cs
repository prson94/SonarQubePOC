using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    public class PolicyModel : BaseIntObject
    {
        #region Properties

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Status_Name", Description = "Status_Description")]
        public PolicyStatus Status { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), StringLength(250)]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int PolicyTypeID { get; set; }

        #endregion
    }

    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Policy, "Policy")]
    public class Policy : PolicyModel, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ISearchable, IUpdatedMetadata
    {
        #region Properties

        [DataMember, ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Level_Name", Description = "Level_Description")]
        public int Level { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        #endregion

        #region Navigation Properties

        [IgnoreDataMember]
        public virtual Policy Parent { get; set; }

        [ForeignKey("PolicyTypeID"), IgnoreDataMember]
        public virtual PolicyType PolicyType { get; set; }

        [ForeignKey("ParentID"), IgnoreDataMember]
        public virtual ICollection<Policy> Children { get; set; }

        #endregion
    }
}
