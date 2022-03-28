using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class AssetTagApiModel
    {
        [DataMember]
        public Guid AssetUID { get; set; }

        [DataMember]
        public Guid TagUID { get; set; }

        [DataMember]
        public string TagName { get; set; }
    }
}
