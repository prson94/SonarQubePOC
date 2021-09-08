using System;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]    
    public class HelpResource : BaseIntObject, IIntObject
    {        
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public int SortIndex { get; set; }
        [DataMember]
        public bool isEditable { get; set; }
        [DataMember]
        public int visibilty { get; set; }
        [DataMember]
        public int order { get; set; }
        [DataMember]
        public bool isSystem { get; set; }
    }
}
