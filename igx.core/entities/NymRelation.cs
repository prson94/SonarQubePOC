using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class NymRelation : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        [DataMember, StringLength(25), Column(TypeName = "varchar")]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        public int PredicateID { get; set; }

        [IgnoreDataMember]
        public virtual Predicate Predicate { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

    }
}
