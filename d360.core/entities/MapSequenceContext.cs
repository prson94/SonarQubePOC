using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapSequenceContext : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int MapSequenceID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string Object { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [IgnoreDataMember]
        public virtual MapSequence MapSequence { get; set; }
    }
}
