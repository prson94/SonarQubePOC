
export class FieldTypeAPIModel {
    Action: string;
    Fields: FieldTypeAPIModelField[];
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
    constructor(type?: string) {
        switch (type) {
            case 'Boolean':
                this.Boolean = new Boolean();
                break;
            case 'OwnershipLookup':
                this.OwnershipLookup = new ComputedOwnershipLookup();
                break;
            case 'FieldFromRelationship':
                this.FieldFromRelationship = new ComputedRelationshipField();
                break;
            case 'ComplexRelationLookup':
                this.ComplexRelationLookup = new ComputedRelationshipLookup();
                break;
            case 'RefListRelationship':
                this.RefListRelationship = new ComputedRelationshipReferenceList();
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
            case 'JSON':
                this.JSON = new ComputedRelationshipReferenceList();
                this.JSON.ShowIfEmpty = true;
                break;
            case 'Json':
                this.Json = new ComputedRelationshipReferenceList();
                this.Json.ShowIfEmpty = true;
                break;
            case 'JsonElement':
                this.JsonElement = new ComputedRelationshipField();
                this.JsonElement.JsonAttribute = new JSONAttribute();
                break;
            case 'Link':
                this.Link = new Link();
                break;
            case 'Lookup':
                this.Lookup = new Lookup();
                break;
            case 'Number':
                this.Number = new Decimal();
                break;
            case 'Path':
                this.Path = new Path();
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
            case 'Score':
                this.Score = new Score();
                break;
            case 'Counter':
                this.Counter = new Counter();
                break;
            default:
                this.Empty = new Empty();
        }
    }

    Boolean: Boolean;

    //need a second ComputedOwnershipLookup so the API can serialize the object by the class name
    OwnershipLookup: ComputedOwnershipLookup;
    ComputedOwnershipLookup: ComputedOwnershipLookup;

    //need a second ComputedRelationshipField so the API can serialize the object by the class name
    FieldFromRelationship: ComputedRelationshipField;
    ComputedRelationshipField: ComputedRelationshipField;

    //need a second ComputedRelationshipLookup so the API can serialize the object by the class name
    ComplexRelationLookup: ComputedRelationshipLookup;
    ComputedRelationshipLookup: ComputedRelationshipLookup;

    //need a second ComputedRelationshipReferenceList so the API can serialize the object by the class name
    RefListRelationship: ComputedRelationshipReferenceList;
    ComputedRelationshipReferenceList: ComputedRelationshipReferenceList;

    //need a second JSON so the API can serialize the object by the class name
    JSON: ComputedRelationshipReferenceList;
    Json: ComputedRelationshipReferenceList;

    Date: DateClass;
    DateTime: DateClass;
    Decimal: Decimal;
    Html: HTML;
    JsonElement: ComputedRelationshipField;
    Link: Link;
    Lookup: Lookup;
    Number: Decimal;
    Path: Path;
    Relationship: Boolean;
    Text: Text;
    Tag: Tag;
    Score: Score;
    Counter: Counter;
    Empty: Empty;
}

export class FieldDisplayModel {
    Name: string;
    FriendlyName: string;
    Category: string;
    FieldType: string;
    DisplayInColumn: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsRequired: boolean;
    ShowIfEmpty: boolean;
    SortOrder: number;
    ColumnOrder: number;
}

export interface ICommonOptions {
    ColumnOrder: number;
    ColumnWidth: number;
    SortOrder: number;
    IsDisplayable: boolean;
    IsEditable: boolean;
    IsListable: boolean;
    IsPartOfKey: boolean;
    IsPrimaryFilter: boolean;
    ShowIfEmpty: boolean;
    Validation: BooleanValidation;
    Search: Search;
}

export class Empty implements ICommonOptions {
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    Validation: BooleanValidation = new BooleanValidation();
    Description: Description = new Description();
    Search: Search = new Search();
}

export class Boolean implements ICommonOptions {
    DefaultValue?: boolean;
    Description: Description = new Description();
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    IntersectTypeUid?: string;
    Validation: BooleanValidation = new BooleanValidation();
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class Description {
    Form: string;
    Display: string;
}

export class BooleanValidation {
    IsRequired: boolean = false;
}

export class DisplayOnlyDescription {
    Display: string;
}

export class Search {
    AddToResult: boolean = false;
    Prefix: string;
    Suffix: string;
    DisplayOrder: number;
}

export class ComputedOwnershipLookup implements ICommonOptions {
    Validation: BooleanValidation = new BooleanValidation();
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsEditable: boolean = false;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ColumnOrder: number;
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    Definition: ComputedOwnershipLookupDefinition = new ComputedOwnershipLookupDefinition();
    IsDisplayable: boolean = true;
    ShowIfEmpty: boolean = false;
    HideFilter: boolean = false;
    HideFooter: boolean = false;
    HideHeader: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class ComputedOwnershipLookupDefinition {
    DisplayAsList: boolean = false;
    DisplayAssignmentSource: boolean = false;
    ExpandGroupMembership: boolean = true;
    ResponsibilityTypeUid: string = null;
}

export class ComputedRelationshipField implements ICommonOptions {
    Validation: BooleanValidation = new BooleanValidation();
    IsEditable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    IntersectTypeUid?: string;
    FieldTypeName?: string;
    IsDisplayable: boolean = true;
    IsListable: boolean = false;
    ShowIfEmpty: boolean = false;
    JsonAttribute?: JSONAttribute;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class JSONAttribute {
    FieldName: string;
    Path: string;
    DataType: string;
}

export class ComputedRelationshipLookup implements ICommonOptions {
    Validation: BooleanValidation = new BooleanValidation();
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsEditable: boolean = false;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ColumnOrder: number;
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    Definition: ComputedRelationshipLookupDefinition = new ComputedRelationshipLookupDefinition();
    IsDisplayable: boolean = true;
    ShowIfEmpty: boolean = false;
    HideFilter: boolean;
    HideFooter: boolean;
    HideHeader: boolean;
    Search: Search = new Search();
}

export class Counter implements ICommonOptions {
    Validation: BooleanValidation = new BooleanValidation();
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsEditable: boolean = false;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ColumnOrder: number;
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    IsDisplayable: boolean = true;
    ShowIfEmpty: boolean = false;
    HideFilter: boolean;
    HideFooter: boolean;
    HideHeader: boolean;
    CounterPrefix: string;
    CounterInitialIndex: number;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class ComputedRelationshipLookupDefinition {
    Fields: DefinitionField[] = [];
    Relations: Relation[] = [];
}

export class DefinitionField {
    AssetTypeUid: string;
    FieldTypeName: string;
    Filter: string;
    OverrideDisplayName: string;
    DisplayOrder: number;
    SortOrder: number = 0;
    Show: boolean;
    Width: number;
    RelationIndex: number;
}

export class Relation {
    IntersectTypeUid: string;
    AssetTypeUid: string;
    RelationType: string;
    Direction: string;
}

export class ComputedRelationshipReferenceList implements ICommonOptions {
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsEditable: boolean = false;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ColumnOrder: number;
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    Definition: ComputedRelationshipReferenceListDefinition = new ComputedRelationshipReferenceListDefinition();
    IntersectTypeUid?: string;
    IsDisplayable: boolean = true;
    ShowIfEmpty: boolean = false;
    Validation: BooleanValidation = new BooleanValidation();
    Search: Search = new Search();
}

export class ComputedRelationshipReferenceListDefinition {
    DisplayRefListDescription: boolean = true;
}

export class DateClass implements ICommonOptions {
    DefaultValue?: Date = undefined;
    Description: Description = new Description();
    Validation: BooleanValidation = new BooleanValidation();
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class Decimal implements ICommonOptions {
    DefaultValue: number;
    Description: Description = new Description();
    Increment: number;
    Validation: DecimalValidation = new DecimalValidation();
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class DecimalValidation {
    Precision?: number;
    MinimumValue: number;
    MaximumValue: number;
    IsRequired: boolean = false;
}

export class HTML implements ICommonOptions {
    DefaultValue: string;
    Description: Description = new Description();
    Validation: HTMLValidation = new HTMLValidation();
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class HTMLValidation {
    MinimumLength: number;
    MaximumLength: number;
    IsRequired: boolean = false;
}

export class Link implements ICommonOptions {
    DefaultValue: DefaultValue = new DefaultValue();
    Description: Description = new Description();
    Validation: BooleanValidation = new BooleanValidation();
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class DefaultValue {
    Text: string;
    Url: string;
}

export class Lookup implements ICommonOptions {
    DefaultValue: string;
    Description: Description = new Description();
    AllowAllValue: boolean;
    AllowMultipleValues: boolean;
    AllowAllLabel: string;
    Filter: Filter = new Filter();
    Format: Format = new Format();
    List: List = new List();
    Validation: BooleanValidation = new BooleanValidation();
    ParentFieldTypeName: string;
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class Filter {
    FieldTypeName: string;
    PredicateUid: string;
    UseDirection: boolean;
}

export class Format {
    Display: string = undefined;
    Edit: string = undefined;
}

export class List {
    Uid: string = undefined;
    Class: string = undefined;
    AllowMultipleValues: boolean = undefined;
}

export class Path implements ICommonOptions {
    Validation: BooleanValidation = new BooleanValidation();
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = false;
    IsPartOfKey: boolean = false;
    ShowIfEmpty: boolean = true;
    ColumnOrder: number;
    ColumnWidth: number = null;
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    IsListable: boolean = true;
    IsPrimaryFilter: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
    Definition: PathDefinition = new PathDefinition();
}

export class PathDefinition {
    AssetTypeUid: string = null;
}

export class Tag implements ICommonOptions {
    Validation: BooleanValidation = new BooleanValidation();
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = false;
    IsPartOfKey: boolean = false;
    ShowIfEmpty: boolean = true;
    ColumnOrder: number;
    ColumnWidth: number = null;
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    IsListable: boolean = true;
    IsPrimaryFilter: boolean = false;
    Search: Search = new Search();
}

export class Text implements ICommonOptions {
    DefaultValue: string;
    Description: Description = new Description();
    Validation: TextValidation = new TextValidation();
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = true;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class TextValidation {
    Message: string;
    Pattern: string;
    MinimumLength: number;
    MaximumLength: number;
    IsRequired: boolean = false;
}


export class Score implements ICommonOptions {
    DefaultValue: number;
    Increment: number;
    Validation: BooleanValidation = new BooleanValidation();
    Description: DisplayOnlyDescription = new DisplayOnlyDescription();
    ColumnOrder: number;
    ColumnWidth: number = null;
    SortOrder: number = 0;
    IsDisplayable: boolean = true;
    IsEditable: boolean = false;
    IsListable: boolean = false;
    IsPartOfKey: boolean = false;
    IsPrimaryFilter: boolean = false;
    ShowIfEmpty: boolean = false;
    ScoreType: number = null;
    Search: Search = new Search();
    DisplayInColumn: boolean = false;
}

export class FieldTypeHelper {
    public static getFieldType(field: FieldType): string {
        return Object.keys(field)[0];
    }

    public static isFieldForOperator(field: FieldType): boolean {
        let allowedFieldTypes = ['boolean', 'date', 'datetime', 'decimal', 'html', 'lookup', 'number', 'text'];
        return allowedFieldTypes.some(x => x === this.getFieldType(field).toLowerCase());
    }
    public static isFieldForOperatorAdvancedFilters(field: FieldType): boolean {
        let allowedFieldTypes = ['boolean', 'date', 'datetime', 'decimal', 'html', 'lookup', 'number', 'text', 'link', 'tag', 'score', 'path', 'computedrelationshipfield', 'json', 'relationship', 'counter'];
        return allowedFieldTypes.some(x => x === this.getFieldType(field).toLowerCase());
    }
}