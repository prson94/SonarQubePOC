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
        
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int PolicyTypeID { get; set; }

        #endregion
    }

    [DataContract(Namespace = NAMESPACE)]
    public class Policy : PolicyModel, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ISearchable, IUpdatedMetadata
    {
        public Policy()
        {
            Visible = true;
        }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string KeyHash { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string FieldHash { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Level_Name", Description = "Level_Description")]
        public int Level { get; set; }

        [DataMember, ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public bool Visible { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [IgnoreDataMember]
        public virtual Policy Parent { get; set; }

        [ForeignKey("PolicyTypeID"), IgnoreDataMember]
        public virtual PolicyType PolicyType { get; set; }

        [ForeignKey("ParentID"), IgnoreDataMember]
        public virtual ICollection<Policy> Children { get; set; }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.PolicyType, Object = SystemObjects.Policy, TypeID = PolicyTypeID };
        }
    }
}
