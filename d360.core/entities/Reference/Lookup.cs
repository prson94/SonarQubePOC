using System;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Lookup : BaseIntObject, IIntObject, IFieldsObject, ISearchable, IUpdatedMetadata
    {
        public int LookupTypeID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual LookupType LookupType { get; set; }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.LookupType, Object = SystemObjects.Lookup, TypeID = LookupTypeID };
        }
    }
}
