export interface IFieldsService {
    getFields(objectID: number, objectType: string): Promise<FieldDefinition[]>;
}

export class FieldDefinition {

    ObjectType: string;
    ObjectID: string;
    ID: string;
    Category: string;
    FriendlyName: string;
    SortOrder: string;
    IsRequired: boolean;
    IsListable: boolean;
    DisplayDescription: string;
    FormDescription: string;
}


export class FieldTypeEditorModel {
    FieldIsUsed: boolean;
    FieldType: FieldType;
    FusionItems: FieldTypeFusionItemEditorModel[];
    RelationItem: FieldTypeRelationItemEditorModel;
}

export class FieldType {
    ID: number;
    Name: string;
    Category: string;
    DisplayDescription: string;
    FormDescription: string;
    ValidationDescription: string;
    FriendlyName: string;
    Type: string;
    LookupObjectType: string;
    LookupObjectID: number;
    LookupDisplayFormat: string;
    Length: number;
    MinimumLength: number;
    MaximumLength: number;
    Pattern: string;
    Object: string;
    ObjectID: number;
    IsListable: boolean;
    IsRequired: boolean;
    SortOrder: number;
    Fields: Field[];
    FieldTypeFusionLookupDefinitions: FieldTypeFusionLookupDefinition[];
    FieldTypeRelationLookupDefinitions: FieldTypeRelationLookupDefinition[];
}

export class Field {
    ObjectType: string;
    ObjectID: number;
    FieldTypeID: number;
    Value: string;
    FormattedValue: string;
    FieldType: FieldType;
}

export class FieldTypeFusionItemEditorModel {
    ID: number;
    SourceFusionAttributeType: number;
    ReferenceType: number;
    TargetFusionAttributeType: number;
    HideHeader: boolean;
    HideFooter: boolean;
    DisplayFields: FieldTypeItemDisplayFieldEditorModel[];
}

export class FieldTypeItemDisplayFieldEditorModel {
    FieldTypeID: number;
    FieldTypeName: string;
    Show: boolean;
    SortOrder: number;
    FilterValue: string;
}

export class FieldTypeRelationItemEditorModel {
    ID: number;
    IntersectType: number;
    ReferenceType: number;
    ChildIntersectType: number;
    DisplayFields: FieldTypeItemDisplayFieldEditorModel[];
    HideHeader: boolean;
    HideFooter: boolean;
}

export class FieldTypeFusionLookupDefinition {
    ID: number;
    SourceFusionAttributeTypeID: number;
    TargetFusionAttributeTypeID: number;
    FieldTypeID: number;
    ReferenceType: number;
    HideHeader: boolean;
    HideFooter: boolean;
    FieldTypeFusionLookupDisplayFields: FieldTypeFusionLookupDisplayField[];
}


export class FieldTypeRelationLookupDefinition {
    ID: number;
    IntersectTypeID: number;
    ChildIntersectTypeID: number;
    FieldTypeID: number;
    ReferenceType: number;
    HideHeader: boolean;
    HideFooter: boolean;
    FieldType: FieldType;
    FieldTypeRelationLookupDisplayFields: FieldTypeRelationLookupDisplayField[];
}


export class FieldTypeRelationLookupDisplayField {
    ID: number;
    FieldTypeRelationLookupDefinitionID: number;
    FieldTypeID: number
    FieldTypeName: string;
    Show: boolean;
    SortOrder: number;
    FilterValue: string;
    FieldTypeRelationLookupDefinition: FieldTypeRelationLookupDefinition;
}

export class FieldTypeFusionLookupDisplayField {
    ID: number;
    FieldTypeFusionLookupDefinitionID: number;
    FieldTypeID: number;
    FieldTypeName: string;
    FieldTypeFusionLookupDefinition: FieldTypeFusionLookupDefinition;
}
