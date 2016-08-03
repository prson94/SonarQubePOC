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

export class Rule {
    Name: string;
    ID: number;
    Description: string;
    RuleDimensionID: number;
    RuleType: RuleClassification;
    SourceID: number;
    Dimension: RuleDimension;
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