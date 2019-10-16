export class RuleType {
    ID: number;
    Name: string;
    Description: string;
    HasDashboards: boolean;
    AllowAttributes: boolean; 
    AssetTypeID: string;
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

export class RuleImplementationPagedResults {
    total: number;
    results: any[];
    implementations: any[];
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