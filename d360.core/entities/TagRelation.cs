using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class TagRelation : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int TagID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public virtual Tag Tag { get; set; }
    }
}
