using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionQueryAttribute : BaseIntObject, IIntObject, IFieldsObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public int FusionQueryAttributeTypeID { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string DisplayValue { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [DataMember]
        public bool Deleted { get; set; }

        [IgnoreDataMember]
        public virtual FusionQueryAttributeType FusionQueryAttributeType { get; set; }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.FusionQueryAttributeType, Object = SystemObjects.FusionQueryAttribute, TypeID = FusionQueryAttributeTypeID };
        }
    }
}
