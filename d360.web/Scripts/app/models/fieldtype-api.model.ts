
export class FieldTypeAPIModel {
    Action: string;
    Fields: FieldTypeAPIModelField;
    ActionTypeUid: string;
    AssetTypeUid: string;
    RelationshipTypeUid: string;
}

export class FieldTypeAPIModelField {
    Name: string;
    FriendlyName: string;
    Category: string;
    Type: FieldType = new FieldType();
}

export class FieldType {

    constructor(type?: string){
        switch (type) {
            case 'Boolean':
                this.Boolean = new Boolean();
                break;
            case 'ComputedFusionLookup':
                this.ComputedFusionLookup = new ComputedFusionLookup();
                break;
            case 'ComputedOwnershipLookup':
                this.ComputedOwnershipLookup = new ComputedOwnershipLookup();
                break;
            case 'ComputedRelationshipField':
                this.ComputedRelationshipField = new ComputedRelationshipField();
                break;
            case 'ComputedRelationshipLookup':
                this.ComputedRelationshipLookup = new ComputedRelationshipLookup();
                break;
            case 'ComputedRelationshipReferenceList':
                this.ComputedRelationshipReferenceList = new ComputedRelationshipReferenceList();
                break;
            case 'Date':
                this.Date = new DateClass();
                break;
            case 'DateTime':
                this.DateTime = new DateClass();
                break;
            case 'Decimal':
                this.Decimal = new Decimal();
                break;
            case 'Html':
                this.Html = new HTML();
                break;
            case 'Json':
                this.Json = new ComputedRelationshipReferenceList();
                break;
            case 'JsonElement':
                this.JsonElement = new ComputedRelationshipField();
                break;
            case 'Link':
                this.Link = new Link();
                break;
            case 'Lookup':
                this.Lookup = new Lookup();
                break;
            case 'Number':
                this.Json = new Decimal();
                break;
            case 'Relationship':
                this.Relationship = new Boolean();
                break;
            case 'Text':
                this.Text = new Text();
                break;
            case 'Tag':
                this.Tag = new Tag();
                break;
        }
    }

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

export interface Editable {
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

export class Boolean implements Editable {
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

export class BooleanDescription {
    Form: string;
    Display: string;
}

export class BooleanValidation {
    IsRequired: boolean;
}

export class ComputedFusionLookup implements Editable  {
    ColumnWidth: number;
    SortOrder: number;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    IsDisplayable: boolean;
}

export class ComputedFusionLookupDescription {
    Display: string;
}

export class ComputedOwnershipLookup implements Editable  {
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    Definition: ComputedOwnershipLookupDefinition;
    IsDisplayable: boolean;
    ShowIfEmpty: boolean;
    HideFilter: boolean;
    HideFooter: boolean;
    HideHeader: boolean;
}

export class ComputedOwnershipLookupDefinition {
    DisplayAssignmentSource: boolean;
    ExpandGroupMembership: boolean;
}

export class ComputedRelationshipField {
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

export class JSONAttribute {
    FieldName: string;
    Path: string;
    DataType: string;
}

export class ComputedRelationshipLookup {
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    Definition: ComputedRelationshipLookupDefinition;
    IsDisplayable: boolean;
    ShowIfEmpty: boolean;
    HideFilter: boolean;
    HideFooter: boolean;
    HideHeader: boolean;
}

export class ComputedRelationshipLookupDefinition {
    Fields: DefinitionField[];
    Relations: Relation[];
}

export class DefinitionField {
    AssetTypeUid: string;
    FieldTypeName: string;
    Filter: string;
    OverrideDisplayName: string;
    DisplayOrder: number;
    SortOrder: number;
    Show: boolean;
    Width: number;
}

export class Relation {
    IntersectTypeUid: string;
    AssetTypeUid: string;
    RelationType: string;
    Direction: string;
}

export class ComputedRelationshipReferenceList {
    ColumnOrder: number;
    Description: ComputedFusionLookupDescription;
    IntersectTypeUid?: string;
    IsDisplayable: boolean;
    ShowIfEmpty: boolean;
    Validation?: BooleanValidation;
}

export class DateClass {
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

export class Decimal {
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

export class DecimalValidation {
    Precision?: number;
    MinimumValue: number;
    MaximumValue: number;
    IsRequired: boolean;
}

export class HTML {
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

export class HTMLValidation {
    MinimumLength: number;
    MaximumLength: number;
    IsRequired: boolean;
}

export class Link {
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

export class DefaultValue {
    Text: string;
    Url: string;
}

export class Lookup {
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

export class Filter {
    FieldTypeName: string;
    PredicateUid: string;
    UseDirection: boolean;
}

export class Format {
    Display: string;
    Edit: string;
}

export class List {
    Uid: string;
    Class: string;
    AllowMultipleValues: boolean;
}

export class Tag {
    ColumnOrder: number;
    ColumnWidth: number;
    Description: ComputedFusionLookupDescription;
    IsListable: boolean;
    IsPrimaryFilter: boolean;
}

export class Text {
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

export class TextValidation {
    Message: string;
    Pattern: string;
    MinimumLength: number;
    MaximumLength: number;
    IsRequired: boolean;
}

