export class FieldValidation {
    message: string;
    regex: string;
    rule: string;
}

export class EditorDropDownItem {
    Selected: boolean;
    Text: any;
    Value: any;
    Disabled: boolean;
    Color: string;
}

export class EditorField {
    FieldName: string;
    FieldType: string;
    FieldDescription: string;
    Name: string;
    Value: any;
    ReadOnly: boolean;
    TooltipText: boolean;
    Required: boolean;
    Items: any[];
    Row: number;
    Column: number;
    SimilarItemsUri: string;
    Validations: FieldValidation[];
    TypeaheadUri: string;
    Category: string;
    MultiSelect: boolean;
    ParentFieldTypeID: number;
    ParentFieldTypeName: string;
    FieldTypeID: number;
    RecordCount: number;
    UseTypeahead: boolean;
    DelayedLoadType: string;
    IsSemantic: boolean;
    VirtualScroll: boolean;
    ItemSize: number;
    UseNativeLookupControl: boolean;
    UseColorControl: boolean;
    IsPartOfKey: boolean;

    IsAssetLazyLoad: boolean;
    AssetUid: string;
    TargetAssetTypeUid: string;
    IntersectTypeUid: string;
    ObjectCardinality: string;
}

export class EditorCategory {
    name: string;
    rows: EditorRow[];
}

export class EditorRow {
    Row: number = 0;
    Fields: EditorField[] = [];

    getColClass() {
        return 's' + Math.round(12 / (this.Fields.length || 1));
    }
}
