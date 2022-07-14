using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Intersect : BaseIntObject, IIntObject, IFieldsObject, ICreatedMetadata, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

		#region LEGACY COLUMNS

		[DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember]
        public int SubjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

		#endregion LEGACY COLUMNS

		#region NEW COLUMNS

		[DataMember]
		public long SubjectAssetID { get; set; }

		[DataMember]
		public int SubjectAssetTypeID { get; set; }

		[DataMember]
		public long ObjectAssetID { get; set; }

		[DataMember]
		public int ObjectAssetTypeID { get; set; }

		#endregion NEW COLUMNS

		[DataMember]
        public State State { get; set; } = State.Active;


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
