
export class FieldTypeAPIModel {
    Action: string;
    Field: FieldTypeAPIModelField;
    ActionTypeUid: string;
    AssetTypeUid: string;
    RelationshipTypeUid: string;

    hasAnyKeyFields() {
        return this.Field.Type.IsPartyOfKey();
    }
}

export class FieldTypeAPIModelField {
    Name: string;
    FriendlyName: string;
    Category: string;
    Type: FieldType;
}

export class FieldType {
    Boolean: Boolean;
    ComputedFusionLookup: ComputedFusionLookup;
    ComputedOwnershipLookup: ComputedOwnershipLookup;
    ComputedRelationshipField: ComputedRelationshipField;
    ComputedRelationshipLookup: ComputedRelationshipLookup;
    ComputedRelationshipReferenceList: ComputedRelationshipReferenceList;
    Date: DateClass;
    DateTime: DateClass;
    Decimal: Decimal;
    Html: HTML;
    Json: ComputedRelationshipReferenceList;
    JsonElement: ComputedRelationshipField;
    Link: Link;
    Lookup: Lookup;
    Number: Decimal;
    Relationship: Boolean;
    Text: Text;
    Tag: Tag;
}


export interface Boolean {
    DefaultValue?: boolean;
    Description: BooleanDescription;
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
    IntersectTypeUid?: string;
    Validation?: BooleanValidation;
}

export interface BooleanDescription {
    Form: string;
    Display: string;
}

export interface BooleanValidation {
    IsRequired: boolean;
}

export interface ComputedFusionLookup {
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    IsDisplayable: boolean;
}

export interface ComputedFusionLookupDescription {
    Display: string;
}

export interface ComputedOwnershipLookup {
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    Definition: ComputedOwnershipLookupDefinition;
    IsDisplayable: boolean;
    ShowIfEmpty: boolean;
    HideFilter: boolean;
    HideFooter: boolean;
    HideHeader: boolean;
}

export interface ComputedOwnershipLookupDefinition {
    DisplayAssignmentSource: boolean;
    ExpandGroupMembership: boolean;
}

export interface ComputedRelationshipField {
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    Description: ComputedFusionLookupDescription;
    IntersectTypeUid?: string;
    FieldTypeName?: string;
    IsDisplayable: boolean;
    IsListable: boolean;
    ShowIfEmpty: boolean;
    JsonAttribute?: JSONAttribute;
}

export interface JSONAttribute {
    FieldName: string;
    Path: string;
    DataType: string;
}

export interface ComputedRelationshipLookup {
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    Definition: ComputedRelationshipLookupDefinition;
    IsDisplayable: boolean;
    ShowIfEmpty: boolean;
    HideFilter: boolean;
    HideFooter: boolean;
    HideHeader: boolean;
}

export interface ComputedRelationshipLookupDefinition {
    Fields: DefinitionField[];
    Relations: Relation[];
}

export interface DefinitionField {
    AssetTypeUid: string;
    FieldTypeName: string;
    Filter: string;
    OverrideDisplayName: string;
    DisplayOrder: number;
    SortOrder: number;
    Show: boolean;
    Width: number;
}

export interface Relation {
    IntersectTypeUid: string;
    AssetTypeUid: string;
    RelationType: string;
    Direction: string;
}

export interface ComputedRelationshipReferenceList {
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    IntersectTypeUid?: string;
    IsDisplayable: boolean;
    ShowIfEmpty: boolean;
    Validation?: BooleanValidation;
}

export interface DateClass {
    DefaultValue: Date;
    Description: BooleanDescription;
    Validation: BooleanValidation;
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
}

export interface Decimal {
    DefaultValue: number;
    Description: BooleanDescription;
    Increment: number;
    Validation: DecimalValidation;
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
}

export interface DecimalValidation {
    Precision?: number;
    MinimumValue: number;
    MaximumValue: number;
    IsRequired: boolean;
}

export interface HTML {
    DefaultValue: string;
    Description: BooleanDescription;
    Validation: HTMLValidation;
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
}

export interface HTMLValidation {
    MinimumLength: number;
    MaximumLength: number;
    IsRequired: boolean;
}

export interface Link {
    DefaultValue: DefaultValue;
    Description: BooleanDescription;
    Validation: BooleanValidation;
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
}

export interface DefaultValue {
    Text: string;
    Url: string;
}

export interface Lookup {
    DefaultValue: string;
    Description: BooleanDescription;
    AllowAllValue: boolean;
    AllowAllLabel: string;
    Filter: Filter;
    Format: Format;
    List: List;
    Validation: BooleanValidation;
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
}

export interface Filter {
    FieldTypeName: string;
    PredicateUid: string;
    UseDirection: boolean;
}

export interface Format {
    Display: string;
    Edit: string;
}

export interface List {
    Uid: string;
    Class: string;
    AllowMultipleValues: boolean;
}

export interface Tag {
    ColumnOrder: number;
    ColumnWidth: number;
    Description: ComputedFusionLookupDescription;
    IsListable: boolean;
    IsPrimaryFilter: boolean;
}

export interface Text {
    DefaultValue: string;
    Description: BooleanDescription;
    Validation: TextValidation;
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
}

export interface TextValidation {
    Message: string;
    Pattern: string;
    MinimumLength: number;
    MaximumLength: number;
    IsRequired: boolean;
}

export class Convert {
    public static toFieldTypeAPiModel(json: string): FieldTypeAPIModel {
        return JSON.parse(json);
    }

    public static fieldTypeAPiModelToJson(value: FieldTypeAPIModel): string {
        return JSON.stringify(value);
    }
}
