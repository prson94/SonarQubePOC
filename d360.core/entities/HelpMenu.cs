using System;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class HelpMenu : BaseIntObject, IIntObject, ICreatedObject, IUpdatedObject
    {
        [DataMember]
        public Guid Uid { get; set; }
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public bool isEditable { get; set; }
        [DataMember]
        public int visibilty { get; set; }
        [DataMember]
        public int order { get; set; }
    }
}
