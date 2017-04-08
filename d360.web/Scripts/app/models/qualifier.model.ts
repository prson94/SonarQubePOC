export class QualifierType {
    ID: number;
    RuleImplementationID: number;
    Name: string;
    Order: number;
    ResolutionObject: string;
    ResolutionObjectID: number;
    ResolutionObjectName: string;
    ResolutionFieldTypeID: number;
    ResolutionFieldTypeName: string;
}

export class ResolutionObjectType {
    ID: number;
    Type: string;
    value: string;
    label: string;
}

export class ResolutionFieldType {
    ID: number;
    FriendlyName: string;
    Category: string;
    DisplayDescription: string;
    FormDescription: string;
    IsListable: boolean;
    IsRequired: boolean;
    SortOrder: number;
    ObjectType: string;
    ObjectID: number;
    Name: string;
    Type: string;
}