using d360.core.enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("SynchedAssetTypeRoleItem", Schema = "integration")]
    public class IntegrationAssetTypeRoleItem : BaseIntObject
    {
        [DataMember]
        public int SynchedAssetTypeID { get; set; }

        [DataMember]
        public bool IncludeInPropertyRequest { get; set; }

        [DataMember]
        public string SourceIdField { get; set; } = string.Empty;

        [DataMember]
        public string SourceNameField { get; set; } = string.Empty;

        [DataMember]
        public string RoleName { get; set; }

        [DataMember]
        public bool Active { get; set; } = true;

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual ICollection<IntegrationAssetType> IntegrationAssetType { get; set; }
    }
}
