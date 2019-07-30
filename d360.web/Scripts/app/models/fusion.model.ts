export class FusionType {
    ID: number;
    Name: string;
    Description: string;
    UpdatedOn: string;
    UpdatedBy: number;
}

export class FusionAttributeType {
    AssetTypeID: number;
    ID: number;
    ParentID: number;
    FusionTypeID: number;
    Assignable: boolean;
    ScanEnabled: boolean;
    Name: string;
    Path: string;
    TextPath: string;
    UpdatedOn: string;
    UpdatedBy: number;
}

export class FusionAttributeItem {
    ID: number;
    Name: string;
    Type: string;
}

export class FusionConfiguration {
    ID: number;
    Name: string;
    Description: string;
    FusionTypeID: number;
    FusionType: string;
    Enabled: boolean;
}

export class FusionQueryAttributeType {
    ID: number;
    FusionID: number;
    Name: string;
    DisplayFormat: string;
    Query: string;
    CreatedOn: string;
    CreatedBy: number;
    UpdatedOn: string;
    UpdatedBy: number;
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
    HasDashboards: boolean;
    AssetID: number;
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

export class FusionAttributeTypeCustomQuery {
    ID: number;
    FusionID: number;
    FusionAttributeType: string;
    FusionAttributeTypeID: number;
    Query: string;
}

export enum FusionScheduleDay {
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

export class FusionSchedule {
    ID: number;
    Day: FusionScheduleDay;
    DayText: string;
    Time: string;
    FullRefresh: boolean;
    FusionID: number;
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

export class FusionExecutionResultPaged {
    total: number;
    results: FusionExecutionResult[];
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
    NewValue: string;
    OldValue: string;
}



export class RelationIntersectType {
    ID: number;
    Name: string;
    Subject: string;
    Object: string;
    SubjectID: number;
    ObjectID: number;
}


export class AttributeNode {
    ID: number;
    ParentID: number;
    FusionAttributeTypeID: number;
    Name: string;

    selected: boolean = false;
    parentType: number = 0;
    isLoadingChildren: boolean = false;
}

    


export class AssetDataProfile {
    DataProfileID: number;
    AssetID: number;
    RowCount: number;	
    Uniqueness: number;	
    UniqueCount: number;
    Completeness: number;
    NullCount: number;
    BlankCount: number;
    DataType: string;
    MinimumValue: string;
    MaximumValue: string;
    Precision: number;
    Scale: number;
    Average: number;
    Median: number;
    StandardDeviation: string;
    Top10Values: string;
    ProcessIdentifier: string;
}