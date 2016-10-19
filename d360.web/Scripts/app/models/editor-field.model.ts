export class EditorField {
    FieldName: string;
    FieldType: string;
    FieldDescription: string;
    Name: string;
    Value: any;
    ReadOnly: boolean;
    Required: boolean;
    Items: string[];
    Row: number;
    Column: number;
    SimilarItemsUri: string;
}

export class EditorRow {
    Row: number = 0;
    Fields: EditorField[] = [];

    getColClass() {
        return 's' + Math.round(12 / (this.Fields.length || 1));
    }
}
