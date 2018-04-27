using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("SynchedAssetType", Schema = "integration")]
    public class IntegrationAssetType : BaseIntObject
    {
        [DataMember]
        public int IntegrationSettingID { get; set; }

        [DataMember]
        public string SourceAssetTypeName { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Object { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int ObjectID { get; set; }

        [DataMember]
        public bool ToGovern { get; set; } = true;

        [DataMember]
        public bool Active { get; set; } = false;

        [DataMember]
        public string OptionalIDName { get; set; } = null;

        [DataMember]
        public int? OptionalID { get; set; } = null;

        [DataMember]
        public bool AllowChangeDetection { get; set; } = false;

        [DataMember]
        public int? LastSuccessfulCount { get; set; }

        [DataMember]
        public DateTime? LastSynchOn { get; set; } = null;


        [IgnoreDataMember, ForeignKey("IntegrationSettingID")]
        public virtual ICollection<IntegrationSetting> IntegrationSetting { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual ICollection<IntegrationAssetTypeFieldItem> Fields { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual ICollection<IntegrationAssetTypeRelationItem> Relations { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual ICollection<IntegrationAssetTypeRoleItem> Roles { get; set; }
    }
}
