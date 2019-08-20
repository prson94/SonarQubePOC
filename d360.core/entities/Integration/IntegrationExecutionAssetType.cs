using System;
using System.Collections.Generic;
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

        public int? ProcessedFieldsPage { get; set; }
        public int? ProcessedRelationsPage { get; set; }
        public int? ProcessedResponsibilitiesPage { get; set; }
    }

    public class StepExecutionTime
    {
        public int Step { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
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
        public DateTime? DelayUntil { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public bool ProcessedDelete { get; set; }

        [DataMember]
        public string RawDefinition { get; set; }

        [DataMember]
        public string EnumFieldValues { get; set; }

        [DataMember]
        public bool FieldHashesCleared { get; set; }

        [DataMember]
        public bool OwnershipHashesCleared { get; set; }

        [DataMember]
        public bool RelationshipHashesCleared { get; set; }

        [DataMember]
        public string RetryLog { get; set; } = "{ \"RetryCount\":0, \"LastRetryInError\": false, \"LastStepCompleted\": 0, \"Begins\":{\"Fields\":0,\"Relations\":0,\"Responsibilities\":0}}";

        [DataMember]
        public string StepExecutionTimes { get; set; } = "[]";

        [DataMember]
        public string IGCAssetRelationshipBreakdown { get; set; }

        [DataMember]
        public string IGCAssetResponsibilityBreakdown { get; set; }

        // IMPORTANT: Do not add any other property references that are purely used by the integration procedure. 
        // Doing so can alter the property results an set int properties to 0, nullifying metric results.

        [IgnoreDataMember, ForeignKey("SynchedAssetTypeID")]
        public virtual IntegrationAssetType SynchedAssetType { get; set; }
    }
}
