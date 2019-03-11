using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Column(TypeName = "varchar"), StringLength(500)]
        public string SourceAssetTypeName { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int? ObjectID { get; set; }

        [DataMember]
        public bool ToGovern { get; set; } = true;

        [DataMember]
        public bool Active { get; set; } = false;

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string OptionalIDName { get; set; } = null;

        [DataMember]
        public int? OptionalID { get; set; } = null;

        [DataMember]
        public bool AllowChangeDetection { get; set; } = false;

        [DataMember]
        public DateTime? LastSynchOn { get; set; } = null;

        [DataMember]
        public bool TriggerTopicMessage { get; set; }

        [DataMember]
        public int? PageSize { get; set; }

        [DataMember]
        public int? FieldPageSize { get; set; }

        [DataMember]
        public int? RelationshipPageSize { get; set; }

        [DataMember]
        public int? OwnershipPageSize { get; set; }

        [DataMember]
        public int? RefreshIntervalOverride { get; set; }

        [DataMember]
        public int? DeleteExecutionTimeoutHours { get; set; }

        [DataMember]
        public bool EnableAppInsightsVerboseLogging { get; set; }

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
