export class RuleType {
    ID: number;
    Name: string;
    Description: string;
    HasDashboards: boolean;
    AllowAttributes: boolean; 
    AssetTypeUID: string;
    HasWorkflow: boolean;
}

export class Rule {    
    Name: string;
    ID: number;
    Description: string;
    Measurement: string;
    Purpose: string;
    Resolution: string;                 
    SourceID: number;
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
    UID: string;
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
    qualifiers: any[];
}

export class RuleResultFilter {
    dataField: string;
    value: string;
    condition: string = 'CONTAINS';
}