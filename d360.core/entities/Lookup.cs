using System.Xml.Linq;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.entities.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Lookup, "Lookup")]
    public class Lookup : BaseIntObject, IIntObject, IFieldsObject, ISearchable, IUpdatedMetadata
    {
        public int LookupTypeID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual LookupType LookupType { get; set; }
    }
}
