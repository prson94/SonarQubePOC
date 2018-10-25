using System.Runtime.Serialization;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Environment : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int DatabaseServerID { get; set; }

        [IgnoreDataMember]
        public virtual DatabaseServer DatabaseServer { get; set; }
    }
}
