using d360.core.entities.Contracts;
using d360.core.queue;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ReferenceItem : BaseIntObject, IIntObject, IFieldsObject, ISearchable, ICreatedMetadata, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public int ReferenceItemTypeID { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual ReferenceItemType ReferenceItemType { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.ReferenceItem,
                ObjectID = ID,
                ObjectType = SystemObjects.ReferenceItemType,
                ObjectTypeID = ReferenceItemTypeID
            };
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.ReferenceItemType, Object = SystemObjects.ReferenceItem, TypeID = ReferenceItemTypeID };
        }
    }
}
