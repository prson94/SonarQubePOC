using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityTypeRelationOverrideItem : BaseLongObject
    {
        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public long AssetID { get; set; }

        [DataMember]
        public string SecurityAsset { get; set; }

        [DataMember]
        public int SecurityAssetID { get; set; }

        [DataMember]
        public string Context { get; set; }

        public int UpdatedBy { get; set; } = 0;

        [DataMember]
        public DateTime? UpdatedOn
        {
            get
            {
                return this.updatedon.HasValue
                   ? this.updatedon.Value
                   : DateTime.UtcNow;
            }

            set { this.updatedon = value; }
        }

        private DateTime? updatedon = null;

        [IgnoreDataMember]
        public virtual ResponsibilityType ResponsibilityType { get; set; }
    }
}
