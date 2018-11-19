using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class RetryLogModel
    {
        public int RetryCount { get; set; }
        public int LastStepCompleted { get; set; } = 0;
        public bool LastRetryInError { get; set; }
        public RetryLogBeginsModel Begins { get; set; }
    }

    public class RetryLogBeginsModel
    {
        public int Fields { get; set; }
        public int Relations { get; set; }
        public int Responsibilities { get; set; }
    }

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
        public bool IsFullRefresh { get; set; }

        [DataMember]
        public DateTime StartedOn { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public bool ProcessedDelete { get; set; }

        [DataMember]
        public string RawDefinition { get; set; }

        [DataMember]
        public string EnumFieldValues { get; set; }

        [DataMember]
        public string RetryLog { get; set; } = "{RetryCount:0, LastRetryInError: false, LastStepCompleted: 0, Begins:{Fields:0,Relations:0,Responsibilities:0}}";

        [IgnoreDataMember, ForeignKey("ExecutionID")]
        public virtual IntegrationExecution Execution { get; set; }

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual IntegrationAssetType SynchedAssetType { get; set; }
    }
}
