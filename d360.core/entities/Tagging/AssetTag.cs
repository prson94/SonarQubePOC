using System;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;


namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetTag : BaseLongObject, IUIDMetadata, ICreatedMetadata
    {
        [DataMember]
        public Guid? UID { get; set; }

        [DataMember]
        public long AssetID { get; set; }

        [DataMember]
        public int TagID { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? CreatedBy { get; set; }
    }
}
