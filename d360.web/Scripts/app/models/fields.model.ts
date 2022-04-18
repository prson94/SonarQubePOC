import { SelectItem } from 'primeng/api';
import { Observable } from "rxjs";
import { FieldTypeAPIModelField } from './fieldtype-api.model';

export interface IFieldsService {    
    getFieldTypeEditor(name: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<FieldTypeAPIModelField>;
        
    getRelationLookupDisplayFields(assetTypeUid: string, intersectTypeUid: string): Observable<SelectItem[]>;

    getLookupTokens(uid: string): Observable<Array<SelectItem>>;

    getLookups(assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string, fieldTypeName: string): Observable<Lookups>;

    getFormData(name: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<FieldTypeEditorModel>;

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
    FieldType: FieldTypeAPIModelField;    
    RelationItems: FieldTypeRelationItemEditorModel[] = [];
    RelationItem: FieldTypeRelationItemEditorModel;
    selectedLookup: string;
    cardinalRelationship: string;
    LookupTokens: SelectItem[] = [];
    OwnershipLookupSettings: OwnershipLookupSettings;
    JsonElementSettings: JsonElementSettings;
    RefListFromRelSettings: RefListFromRelSettings;
    IsPrimaryFilter: boolean;
}

export class RefListFromRelSettings {
    DisplayRefListDescription: boolean;
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
    DisplayAsList: boolean;
    DisplayAssignmentSource: boolean;
    ExpandGroupMembership: boolean;
    ResponsibilityType: number;
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

export class FieldTypeItemDisplayFieldEditorModel {
    FieldTypeID: number;
    FilterValue: string;
    value: string;

    AssetTypeUid: string;
    FieldTypeName: string;
    Filter: string;
    OverrideDisplayName: string;
    DisplayOrder: number;
    SortOrder: number;
    Show: boolean;
    Width: number;
}

export class FieldTypeRelationItemEditorModel {
    ID: number;
    DisplayFields: FieldTypeItemDisplayFieldEditorModel[] = [];

    SortOrderList: any[] = [];
    selectedRelationItemID: string;
    relationItems: any[];
    relationsLoading = false;
    displayValue: string;

    //new complex relation
    IntersectTypeUid: string;
    AssetTypeUid: string;
    RelationType: number;
    Direction: number;
    selectedIntersectName: string;
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

export class Lookups {
    DataTypes: SelectItem[];
    Patterns: SelectItem[];
    IntersectTypes: LookupItem[];
    Lookups: SelectItem[];
    Field_JsonFields: SelectItem[];
    Field_JsonDataTypes: SelectItem[];
    Field_Relationships: SelectItem[];
    Field_CardinalRelationships: SelectItem[];
    Field_CardinalReferenceRelationships: SelectItem[];
    FieldResponsibilityTypes: SelectItem[];
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

export enum Direction {
    Back = 1,
    Forward = 2,
    Both = 3
}

export interface AssetTypeAncestry {
    Uid: string;
    Name: string;
}
