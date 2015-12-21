using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Predicate : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [ForeignKey("PredicateID"), IgnoreDataMember]
        public virtual ICollection<PredicatePhrase> PredicatePhrases { get; set; }
    }
}
