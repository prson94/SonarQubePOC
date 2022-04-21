export enum Operator {
    Equals = 1,
    NotEquals = 2,
    Contains = 3,
    NotContains = 4,
    StartsWith = 5,
    EndsWith = 6,
    Before = 7,
    After = 8,
    Between = 9,
    Populated = 10,
    NotPopulated = 11,
    GreaterThan = 12,
    LessThanOrEquals = 13,
    LessThan = 14,
    GreaterThanOrEquals = 15,
    In = 16,
    NotIn = 17,
    IsTrue = 18,
    IsFalse = 19,
    OnOrBefore = 20,
    OnOrAfter = 21,
    IsInBand = 22
}

export enum OperatorString {
    Equals = 'Equals',
    NotEquals = 'NotEquals',
    Contains = "Contains",
    NotContains = 'NotContains',
    StartsWith = 'StartsWith',
    EndsWith = 'EndsWith',
    Before = 'Before',
    After = 'After',
    Between = 'Between',
    Populated = 'Populated',
    NotPopulated = 'NotPopulated',
    GreaterThan = 'GreaterThan',
    LessThanOrEquals = 'LessThanOrEquals',
    LessThan = 'LessThan',
    GreaterThanOrEquals = 'GreaterThanOrEquals',
    In = 'In',
    NotIn = 'NotIn',
    IsTrue = 'IsTrue',
    IsFalse = 'IsFalse',
    OnOrBefore = 'OnOrBefore',
    OnOrAfter = 'OnOrAfter',
    IsInBand = 'IsInBand'
}

export class OperatorDataTypeModel {
    ID: number;
    Name: string;
}
export class OperatorMetricGovernanceCheckTypeInfo {
    ID: number;
    Name: string;
}
export class OperatorModel {
    ID: Operator;
    Name: string;
    Description: string;
    MinimumValueCount: number;
    MaximumValueCount: number;
    AllowedDataTypes: OperatorDataTypeModel[];
    AllowedMeasureChecks: OperatorMetricGovernanceCheckTypeInfo[];
    FieldRequiresMultipleValueSupport: boolean;
}
