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
    NotIn = 17
}
export class OperatorDataTypeModel {
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
    FieldRequiresMultipleValueSupport: boolean;
}

export class OperatorHelper {
    static getStringValueForOperator(operator: number): string {
        for (var enumMember in Operator) {
            console.log("enum member: ", enumMember);
        }
        return 'test';
    }
}