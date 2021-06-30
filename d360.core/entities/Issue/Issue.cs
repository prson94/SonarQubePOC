using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using d360.core.entities.Contracts;
using d360.core.enums;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Issue : BaseIntObject, IIntObject, IFieldsObject, IUpdatedMetadata, IEventTrackedEntity, IUIDMetadata
    {
        public int IssueTypeID { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectTypeID { get; set; }

        public virtual IssueType IssueType { get; set; }

        [DataMember]
        public int? CommentID { get; set; }

        [DataMember]
        public Guid? UID { get; set; }

        public DateTime? CompletedOn { get; set; }

        public int? CompletedBy { get; set; }
        public int? InitiatorID { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Issue,
                ObjectID = ID,
                ObjectType = SystemObjects.IssueType,
                ObjectTypeID = IssueTypeID
            };
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.IssueType, Object = SystemObjects.Issue, TypeID = IssueTypeID };
        }
    }
}