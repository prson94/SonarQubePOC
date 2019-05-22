using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Setting", Schema = "integration")]
    public class IntegrationSetting : BaseIntObject
    {
        [DataMember]
        public IntegrationSystem IntegrationSystem { get; set; }

        [DataMember]
        public string SourceUri { get; set; }

        [DataMember]
        public string SourceUser { get; set; }

        [DataMember]
        public string SourcePassword { get; set; }

        [DataMember]
        public int TargetResourceID { get; set; }

        [DataMember]
        public DateTime? LastRefreshOn { get; set; }

        [DataMember]
        public int RefreshInterval { get; set; }

        [DataMember]
        public int PageSize { get; set; }

        [DataMember]
        public DateTime? DelayStartUntil { get; set; }

        [DataMember]
        public int DeleteExecutionTimeoutHours { get; set; }

        [DataMember]
        public bool AutoTrustServerCertificate { get; set; }

        [DataMember]
        public bool RemoveUriPortOnConnect { get; set; }

        [IgnoreDataMember, ForeignKey("IntegrationSettingID")]
        public virtual ICollection<IntegrationAssetType> IntegrationAssetTypes { get; set; }
    }
}
