using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("ExecutionAsset", Schema = "integration")]
    public class IntegrationExecutionAsset : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid Uid { get; set; } = Guid.NewGuid();

        [DataMember]
        public long ExecutionID { get; set; }

        [DataMember]
        public int SynchedAssetTypeID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(100)]
        public string SourceID { get; set; }

        public string RawObject { get; set; }

        public string RawRelationships { get; set; }

        public string RawResponsibilitites { get; set; }

        [DataMember]
        public string ErrorMessages { get; set; }

        [IgnoreDataMember, ForeignKey("ExecutionID")]
        public virtual IntegrationExecution Execution { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual IntegrationAssetType SynchedAssetType { get; set; }
    }
}
