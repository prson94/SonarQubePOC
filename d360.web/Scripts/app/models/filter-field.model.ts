
export enum FilterFieldType {
    Field = 0,
    Relationship,
    Attribute,
    Owner,
}

export class FilterField {
    Name: string;
    Data: any;
    Type: FilterFieldType;
}

export class FilterExpression {
    Type: FilterFieldType;
    Field: any;
    Data: any;
}