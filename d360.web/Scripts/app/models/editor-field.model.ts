export class FieldValidation {    
    message: string;
    regex: string;
    rule: string;
}

export class EditorDropDownItem {
    Selected: boolean;
    Text: any;
    Value: any;
}

export class EditorField {
    FieldName: string;
    FieldType: string;
    FieldDescription: string;
    Name: string;
    Value: any;    
    ReadOnly: boolean;
    Required: boolean;
    Items: EditorDropDownItem[];
    Row: number;
    Column: number;
    SimilarItemsUri: string;
    Validations: FieldValidation[];
    TypeaheadUri: string;
    Category: string;
    MultiSelect: boolean;
    MultipleValues: string[];
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
