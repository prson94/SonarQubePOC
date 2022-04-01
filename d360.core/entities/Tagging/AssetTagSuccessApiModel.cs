using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class AssetTagSuccessApiModel
    {
        [DataMember]
        public Guid? Uid { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public bool Success { get; set; }
    }
}
