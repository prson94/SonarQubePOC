using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeLookup : BaseObject
    {
        [DataMember, Key]//, ForeignKey("FieldTypeID")]
        public int FieldTypeID { get; set; }

        [DataMember]
        public bool HideHeader { get; set; }

        [DataMember]
        public bool HideFooter { get; set; }

        [DataMember]
        public int LookupType { get; set; }

        [DataMember]
        public string Definition { get; set; }

        //[IgnoreDataMember, ForeignKey("FieldTypeID")]
        //public virtual FieldType FieldType { get; set; }
    }
}
