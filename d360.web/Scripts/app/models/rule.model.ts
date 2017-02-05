export class RuleType {
    ID: number;
    Name: string;
}

export class RuleDimension {
    Name: string;
    Description: string;
    ID: number;
}


export enum RuleClassification {
    Informational = 1,
    Quality = 2,
    Metric = 3,
    Profile = 4
}

export enum RuleStatus {    
    Draft = 1,
    Active = 2,
    Inactive = 3
}

export class Rule {    
    Name: string;
    ID: number;
    Description: string;
    Measurement: string;
    Purpose: string;
    Resolution: string;
    Status: RuleStatus;
    StatusName: string;
    RuleDimensionID: number;
    RuleType: RuleClassification;
    RuleTypeName: string;
    SourceID: number;
    Dimension: RuleDimension;    
}


export class RuleResult {
    ID: number;
    RuleID: number;
    EffectiveDate: Date;
    RowsPassed: number;
    RowsFailed: number;
    PassFraction: number;
    FailFraction: number;
    Passed: boolean;
}

export class RuleDetail {
    Name: string;
    ID: number;
    Description: string;            
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
    ParentID: number;
    ParentType: string;
    PluralizedName: string;
    TextPath: string;
    Type: string;
    TypeID: number;
    Url: string;
}

export class RuleResultPagedResults {
    total: number;
    results: any[];
    qualifiers: any[];//string[];
}

export class RuleResultFilter {
    dataField: string;
    value: string;
    condition: string = 'CONTAINS';
}