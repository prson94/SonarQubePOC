using System;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [Serializable, DataContract(Namespace = NAMESPACE)]
    public class AssetDetail : BaseLongObject
    {
        [DataMember]
        public string DisplayValue { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember]
        public Guid? AssetTypeUid { get; set; }

        [DataMember]
        public State State { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public AssetTypeClass AssetTypeClass { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int TypeID { get; set; }

        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }
    }
}
