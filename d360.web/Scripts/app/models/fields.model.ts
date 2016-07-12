import { SelectItem } from 'primeng/primeng';

export interface IFieldsService {
    getFields(objectID: number, objectType: string): Promise<FieldDefinition[]>;
    getFieldTypeEditor(id: number): Promise<FieldTypeEditorModel>;
    getFusionLookupDisplayFields(id: number): Promise<SelectItem[]>;
    getFusionLookupTargetAttributeTypes(sourceID: number, referenceTypeID: number): Promise<SelectItem[]>;
    getRelationLookupChildIntersectTypes(id: number): Promise<SelectItem[]>;
    getRelationLookupDisplayFields(id: number, type: string, intersectTypeID: number): Promise<SelectItem[]>;
    getLookupTokens(id: number, type: string): Promise<SelectItem[]>;
    getLookups(id: number, type: string): Promise<Lookups>;
    getFormData(id: number): Promise<FieldTypeEditorModel>;
    putFieldType(model: FieldTypeEditorModel): Promise<any>;
    postFieldType(model: FieldTypeEditorModel): Promise<any>;
}

export class FieldDefinition {

    ObjectType: string;
    ObjectID: string;
    ID: number;
    Category: string;
    FriendlyName: string;
    SortOrder: string;
    IsRequired: boolean;
    IsListable: boolean;
    DisplayDescription: string;
    FormDescription: string;
    Name: string;
}


export class FieldTypeEditorModel {
    FieldIsUsed: boolean;
    FieldType: FieldType;
    FusionItems: FieldTypeFusionItemEditorModel[] = new Array<FieldTypeFusionItemEditorModel>();
    RelationItem: FieldTypeRelationItemEditorModel;

    selectedLookup: string;
    LookupTokens: SelectItem[] = new Array<SelectItem>();

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
    DisplayFields: string[] | FieldTypeFusionLookupDisplayField[] = new Array<string>();

    TargetFusionAttributeTypes: SelectItem[] = new Array<SelectItem>();
    FusionDisplayFields: SelectItem[] = new Array<SelectItem>();
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

export class Lookups {
    
    DataTypes: SelectItem[];
    Patterns: SelectItem[];
    IntersectTypes: SelectItem[];
    FusionAttributeTypes: SelectItem[];
    Lookups: SelectItem[];

    ReferenceTypes: SelectItem[] = new Array<SelectItem>();
}
