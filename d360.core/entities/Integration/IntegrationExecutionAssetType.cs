using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("ExecutionAssetType", Schema = "integration")]
    public class IntegrationExecutionAssetType : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long ExecutionID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int SynchedAssetTypeID { get; set; }

        [DataMember]
        public int CurrentSourceAssetCount { get; set; }

        [DataMember]
        public int CurrentTargetAssetCount { get; set; }

        [DataMember]
        public DateTime StartedOn { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }


        [IgnoreDataMember, ForeignKey("ExecutionID")]
        public virtual IntegrationExecution Execution { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual IntegrationAssetType SynchedAssetType { get; set; }
    }
}
