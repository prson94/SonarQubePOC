export class FusionType {
    ID: number;
    Name: string;
    Description: string;
    UpdatedOn: string;
    UpdatedBy: number;
}

export class FusionAttributeType {
    ID: number;
    ParentID: number;
    FusionTypeID: number;
    Assignable: boolean;
    Name: string;
    Path: string;
    TextPath: string;
    UpdatedOn: string;
    UpdatedBy: number;
}

export class FusionConfiguration {
    ID: number;
    Name: string;
    Description: string;
    FusionTypeID: number;
    FusionType: string;
    Enabled: boolean;
}

export class FusionFilter {
    FusionID: number;
    FusionAttributeTypeID: number;
    Filter: string;
    Name: string; 

}

export class ObjectStyle {
    ObjectType: string;
    ObjectID: number;
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
}

export class Fusion {
    Description: string;
    Enabled: boolean;
    FusionType: string;
    FusionTypeID: number;
    ID: number;
    Name: string;
}

export class FusionConfigurationDetails {
    Description: string;
    Enabled: boolean;    
    ID: number;
    Name: string;
    ForceRefresh: boolean;
    Interval: number;
    IntervalType: number;
    LockPromotedItems: boolean;
    Manual: boolean;
    FusionTypeID: number;
}

export class FusionAgentExecutionStats {
    DateCompleted: Date;
    DateStarted: Date;
    Fusion: string;
    FusionID: number;
    FusionType: string;
    FusionTypeID: number;
    MachineQueuedOn: string;
    Message: string;
    Success: boolean;
}

export class FusionWorkerExecution {
    Adds: number;
    DateCompleted: Date;
    DateStarted: Date;
    Deletes: number;
    ErrorCount: number;
    Fusion: string;
    FusionID: number;
    FusionType: string;
    FusionTypeID: number;
    ID: number;
    RawLogFileName: string;
    ResultCount: number;
    Updates: number;
}

export class FusionPromotionExecutionStats {
    AttributesConsidered: number;
    DateCompleted: Date;
    DateStarted: Date;
    ID: number;
    NumberOfRules: string;
    PromotedArtifacts: number;
    PromotedDomainItems: number;
    PromotedDomains: number;
    PromotedTaxonomies: number;
    RelationshipsAdded: number;
    TotalNewPromotions: number;
}

export class FusionSummaryStats {
    AgentErrors: number;
    AgentExecutions: number;
    FusionErrors: number;
    FusionExecutions: number;
    PromotionJobsExecuted: number;
}


export class MapRuleItemDetail {
    ID: number;
    Type: string;
    TextID: string;
    ParentTextID: string;
    Transformation: string;
    SourceFusion: string;
    SourceFusionAttributeID: number;
    SourceFusionAttributeTextPath: string;
    SourceObjectName: string;
    SourceObjectID: number;
    SourceObject: string;
    TargetFusion: string;
    TargetFusionAttributeID: number;        
    TargetFusionAttributeTextPath: string;
    TargetObjectName: string;
    TargetObjectID: number;
    TargetObject: string;

    children: MapRuleItemDetail[];
}

export class FusionRule {
    ID: number;
    Enabled: boolean;
    FusionID: number;
    ObjectType: string;
    ObjectID: number;
    ObjectName: string;
    Description: string;
}

export class FusionRuleStep {
    ID: number;
    RuleID: number;
    Step: number;
    Action: string;
    Description: string;
}


export class FusionRuleItem {
    ID: number;
    RuleID: number;
    FusionAttributeID: number;
    FusionAttributeName: string;
}

export class FusionRuleMapping {
    ID: number;
    SourceFieldTypeID: number;
    SourceFieldName: string;
    TargetFieldTypeID: number;
    TargetFieldName: string;
}


export class FusionProcessError {
    Date: Date;
    Error: string;
    ExecutionID: number;
    Fusion: string;
    FUsionID: number;
    FusionType: string;
    FusionTypeID: number;
}

export class FusionAgentError {
    Date: Date;
    Fusion: string;
    FusionID: number;
    MachineName: string;
    Message: string;
}


export class FusionExecutionError {
    Date: Date;
    Error: string;
    ExecutionID: number;
    Fusion: string;
    FusionID: number;
    FusionType: string;
    FusionTypeID: number;
}

export class FusionExecutionResult {
    Action: string;
    Body: string;
    ExecutionID: number;
    FieldName: string;
    FieldTypeID: number;
    Fusion: string;
    FusionAttribute: string;
    FusionAttributeType: string;
    FusionID: number;
    FusionType: string;
    FusionTypeID: number;
    ID: string;
    NewValue: string;
    OldValue: string;
}

export class FusionRuleEditorModel {
    FusionTypeID: number;
    FusionID: number;
    FormUri: string;
    FormMethod: string;
    FormName: string;
    Rule: FusionRule;
    AttributeTypes: FusionAttributeType[] = [];


}

export class FusionRuleStepEditorModel {
    FormUri: string;
    FormMethod: string;
    FormName: string;
    RuleStep: FusionRuleStep;
    FusionID: number;
    FusionTypeID: number;
}
