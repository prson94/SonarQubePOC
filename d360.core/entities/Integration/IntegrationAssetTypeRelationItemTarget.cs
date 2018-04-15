using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("SynchedAssetTypeRelationItemTarget", Schema = "integration")]
    public class IntegrationAssetTypeRelationItemTarget : BaseIntObject
    {
        [DataMember]
        public int SynchedAssetTypeRelationItemID { get; set; }

        [DataMember]
        public bool IncludeInPropertyRequest { get; set; } = true;

        [DataMember]
        public string SourceAssetType { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeRelationItemID")]
        public virtual ICollection<IntegrationAssetTypeRelationItem> IntegrationAssetTypeRelationItem { get; set; }
    }
}
