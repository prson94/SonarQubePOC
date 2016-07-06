export enum StatisticCheckTypes {
    Existence = 1,
    Count = 2,
    PropertyValueCheck = 3,    
    PropertyPopulated = 4,    
    Relationship = 5,    
    FusionOwnership = 6,    
    ScoreRollupViaRelationship = 7,    
    ScoreRollupViaOwnership = 8,    
    EventMetric = 9,    
    PredicateMetric = 10
}

export class StatisticType {
    Object: string;
    ObjectID: number;
    ObjectName: string;
    ObjectCombined :string;    
    PartOfScore: boolean;
    Name: string;
    Description: string;    
    Target: string;
    ID: number;
    CheckType: StatisticCheckTypes;
    Configuration: string;
    Score: number;
    CheckObject: string;
    CheckObjectID: number;
    CheckObjectCombined: string;
    PropertyName: string;
    PropertyValue: string;
    Threshold: string;
    ValidField: string;
    InvalidField: string;
    CheckObjects: string[];
}

export class StatisticCheckType {
    title: string;
    value: string;
}

export class StatisticCheckObjectOptions {
    title: string;
    value: string;
}

export class StatisticObjectOptions {
    title: string;
    value: string;
}
