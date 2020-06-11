using d360.core.enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("SynchedAssetTypeRelationItem", Schema = "integration")]
    public class IntegrationAssetTypeRelationItem : BaseIntObject
    {
        [DataMember]
        public int SynchedAssetTypeID { get; set; }

        [DataMember]
        public bool IncludeInPropertyRequest { get; set; } = true;

        [DataMember]
        public string SourceField { get; set; }

        [DataMember]
        public int PredicateType { get; set; }

        [DataMember]
        public bool IsSubject { get; set; } = false;

        [DataMember]
        public bool Active { get; set; } = true;

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual IntegrationAssetType IntegrationAssetType { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeRelationItemID")]
        public virtual ICollection<IntegrationAssetTypeRelationItemTarget> Targets { get; set; }
    }
}
