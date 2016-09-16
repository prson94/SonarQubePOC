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