using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;
using System;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Intersect : BaseIntObject, IIntObject, IFieldsObject, ICreatedMetadata, IUpdatedMetadata, IEventTrackedEntity
    {
        public Intersect()
        {
            Visible = true;
        }
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Name { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        public bool Visible { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember]
        public int SubjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        public bool? Deleted { get; set; }

        [IgnoreDataMember]
        public virtual IntersectType IntersectType { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Intersect,
                ObjectID = ID,
                ObjectType = SystemObjects.IntersectType,
                ObjectTypeID = IntersectTypeID
            };
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.IntersectType, Object = SystemObjects.Intersect, TypeID = IntersectTypeID };
        }
    }
}
