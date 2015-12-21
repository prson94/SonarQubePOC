using d360.core.entities.Contracts;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class PredicatePhrase : BaseIntObject, IIntObject
    {
        [DataMember]
        public int PredicateID { get; set; }

        [DataMember]
        public string Phrase { get; set; }
    }
}
