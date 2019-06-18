export class RuleType {
    ID: number;
    Name: string;
    Description: string;
    HasDashboards: boolean;
    AllowAttributes: boolean; 
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
}

export class RuleImplementation {
    ID: number;
    RuleID: number;
    RuleTypeID: number;
    SourceID: string;
    SourceUri: string;
    Name: string;
    CreatedOn: Date;
    UpdatedOn: Date
}

export class RuleImplementationDetail {
    ID: number;
    RuleID: number;
    RuleName: string;
    RuleTypeID: number;
    RuleTypeName: string;
    SourceID: string;
    SourceUri: string;
    Name: string;
    CreatedOn: Date;
    UpdatedOn: Date
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
    Uid: string;
    AssetID: number;
    Description: string;            
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
    ParentID: number;
    ParentType: string;
    PluralizedName: string;
    TextPath: string;
    TypeName: string;
    Type: string;
    TypeID: number;
    Url: string;
}

export class RuleResultPagedResults {
    total: number;
    results: any[];
    qualifiers: any[];//string[];
}

export class RuleImplementationPagedResults {
    total: number;
    results: any[];
    implementations: any[];//string[];
}

export class RuleImplementationFilter {
    dataField: string;
    value: string;
    condition: string = 'CONTAINS';
}

export class RuleResultFilter {
    dataField: string;
    value: string;
    condition: string = 'CONTAINS';
}