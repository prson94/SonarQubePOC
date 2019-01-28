using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;
using d360.core.queue;

namespace d360.core.entities
{
    public class PolicyModel : BaseIntObject
    {
        #region Properties
        
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int PolicyTypeID { get; set; }

        #endregion
    }

    [DataContract(Namespace = NAMESPACE)]
    public class Policy : PolicyModel, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ISearchable, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string KeyHash { get; set; }

        [Column(TypeName = "varchar"), StringLength(250)]
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string FieldHash { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Level_Name", Description = "Level_Description")]
        public int Level { get; set; }

        [DataMember, ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [DataMember]
        public string SourceID { get; set; }
        
        [ForeignKey("PolicyTypeID"), IgnoreDataMember]
        public virtual PolicyType PolicyType { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Policy,
                ObjectID = ID,
                ObjectType = SystemObjects.PolicyType,
                ObjectTypeID = PolicyTypeID
            };
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.PolicyType, Object = SystemObjects.Policy, TypeID = PolicyTypeID };
        }
    }
}
