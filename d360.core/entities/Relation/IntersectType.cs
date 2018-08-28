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
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectType : BaseIntObject, IIntObject, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [ReadOnly(true)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Name { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember]
        public int SubjectID { get; set; }

        [DataMember]
        public Guid? SubjectUid { get; set; }

        [DataMember]
        public Cardinality SubjectCardinality { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public Guid? ObjectUid { get; set; }

        [DataMember]
        public Cardinality ObjectCardinality { get; set; }

        [DataMember]
        public bool? IsSystem { get; set; }

        [DataMember]
        public int? PredicateID { get; set; }

        [ForeignKey("PredicateID")]
        public virtual Predicate Predicate { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual ICollection<Intersect> Intersects { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.IntersectType,
                ObjectID = ID,
                ObjectType = SystemObjects.IntersectType,
                ObjectTypeID = 0
            };
        }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypeApiViewModel : BaseObject
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }

        [DataMember]
        public string PredicateName { get; set; }

        [DataMember]
        public string PredicateInverse { get; set; }

        PredicateType _PredicateTypeID;
        public PredicateType PredicateTypeID
        {
            get { return _PredicateTypeID; }
            set
            {
                _PredicateTypeID = value;
                this.PredicateType = _PredicateTypeID.AsInfoModel();
            }
        }

        [DataMember]
        public PredicateTypeInfo PredicateType { get; set; }

        [DataMember]
        public Guid SubjectUid { get; set; }

        AssetTypeClass _SubjectClassID;
        public AssetTypeClass SubjectClassID
        {
            get { return _SubjectClassID; }
            set
            {
                _SubjectClassID = value;
                this.SubjectClass = _SubjectClassID.GetInfo();
            }
        }

        [DataMember]
        public AssetTypeClassInfo SubjectClass { get; set; }

        [DataMember]
        public string SubjectTypeName { get; set; }

        [DataMember]
        public Guid ObjectUid { get; set; }

        AssetTypeClass _ObjectClassID;
        public AssetTypeClass ObjectClassID
        {
            get { return _ObjectClassID; }
            set
            {
                _ObjectClassID = value;
                this.ObjectClass = _ObjectClassID.GetInfo();
            }
        }

        [DataMember]
        public AssetTypeClassInfo ObjectClass { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }
    }
}