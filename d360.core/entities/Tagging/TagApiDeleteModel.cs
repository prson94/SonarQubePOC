using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class TagApiDeleteModel
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public bool cascade { get; set; }
    }
}
