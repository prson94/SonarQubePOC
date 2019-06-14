import {SelectItem} from 'primeng/components/common/api';
import {Observable} from "rxjs";

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
    ColumnOrder: string;
    SortOrder: string;
    IsListable: boolean;
    IsRequired: boolean;
    IsPartOfKey: boolean;
    DisplayDescription: string;
    FormDescription: string;
    Name: string;
    Type: string;
    ExtOrder: number;
}

export class FieldTypeEditorModel {
    FieldIsUsed: boolean;
    FieldType: FieldType;
    FusionItems: FieldTypeFusionItemEditorModel[] = new Array<FieldTypeFusionItemEditorModel>();
    RelationItems: FieldTypeRelationItemEditorModel[] = [];
    RelationItem: FieldTypeRelationItemEditorModel;
    selectedLookup: string;
    cardinalRelationship: number;
    LookupTokens: SelectItem[] = new Array<SelectItem>();
    FilteredLookupItems: FilteredLookupItem[] = [];
    FilteredLookupItem: FilteredLookupItem;
    OwnershipLookupSettings: OwnershipLookupSettings;
    JsonElementSettings: JsonElementSettings;
    IsPrimaryFilter: boolean;
}

export class JsonElementSettings {
    FieldTypeID: number;
    Path: string;
    DataType: string;
}

export class OwnershipLookupSettings {
    HideFilter: boolean;
    HideFooter: boolean;
    HideHeader: boolean;
    ID: number;
    Object: string;
    ObjectID: number;
    DisplayAssignmentSource: boolean;
    ExpandGroupMembership: boolean;
}

export class FilteredLookupItem {
    HideFooter: boolean;
    HideHeader: boolean;
    ID: number;
    Object: string;
    ObjectID: number;
    DisplayFields: FilteredLookupDisplayField[] = [];
}

export class FilteredLookupDisplayField {
    Filter: boolean;
    Show: boolean;
    SortOrder: number;
    value: string;
    FieldTypeID: number;
    FieldTypeName: string;
}

export class FieldType {
    ID: number;
    Name: string;
    Category: string;
    DefaultValue: string;
    DisplayDescription: string;
    FormDescription: string;
    ValidationDescription: string;
    FriendlyName: string;
    Type: string;
    LookupObjectType: string;
    LookupObjectID: number;
    LookupDisplayFormat: string;
    LookupEditFormat: string;
    LookupObjectFieldTypeID: number;
    Length: number;
    MinimumLength: number;
    MaximumLength: number;
    Pattern: string;
    Object: string;
    ObjectID: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsRequired: boolean;
    IsPrimaryFilter: boolean;
    AllowAllValue: boolean;
    AllowAllLabel: string;
    AllowMultipleValues: boolean;
    SortOrder: number;
    Fields: Field[];
    FieldTypeFusionLookupDefinitions: FieldTypeFusionLookupDefinition[];
    FieldTypeRelationLookupDefinitions: FieldTypeRelationLookupDefinition[];
    ParentFieldTypeID: number;
    Increment: number;
    Precision: number;
    FilterPredicateID: number;
    FilterPredicateDirection: number;
    FilterFieldTypeID: number;
    ShowIfEmpty: boolean;
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
    SourceFusionAttributeType: string;
    ReferenceType: number;
    TargetFusionAttributeType: string;
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
    value: string;

    Object: string;
    ObjectID: number;
    Filter: string;
    OverrideDisplayName: string;
    DisplayOrder: number;
    Width: number;
}

export class FieldTypeRelationItemEditorModel {
    ID: number;
    IntersectType: number;
    ReferenceType: number;
    ChildIntersectType: number;
    DisplayFields: FieldTypeItemDisplayFieldEditorModel[] = [];
    HideHeader: boolean;
    HideFooter: boolean;
    HideFilter: boolean;

    SortOrderList: any[] = [];
    selectedRelationItemID: string;
    selectedChildIntersectType: string;
    relationItems: any[];
    relationsLoading = false;
    displayValue: string;

    //new complex relation
    IntersectTypeID: number;
    Object: string;
    ObjectID: number;
    RelationType: number;
    Direction: number;
    selectedIntersectName: string;
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
    FieldTypeID: number;
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
    Show: boolean;
    value: string;
}

export class Lookups {
    DataTypes: SelectItem[];
    Patterns: SelectItem[];
    IntersectTypes: LookupItem[];
    FusionAttributeTypes: SelectItem[];
    Lookups: SelectItem[];
    Field_JsonFields: SelectItem[];
    Field_JsonDataTypes: SelectItem[];
    Field_Relationships: SelectItem[];
    Field_CardinalRelationships: SelectItem[];
    Field_CardinalReferenceRelationships: SelectItem[];
    ComplexLookupRelations: any[] = [];
    FilteredLookups: any[] = [];
    Field_FieldFromRelRelationships: any[] = [];

    ReferenceTypes: SelectItem[] = new Array<SelectItem>();
}

export class LookupItem {
    value: string;
    id: string;
    label: string;
}

export enum ComplexLookupRelationType {
    StandardRelationhip = 1,
    ChildRelationship = 2,
    ChildItem = 3,
    ParentItem = 4
}
