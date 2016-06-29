export class GridDefinition {
    Columns: GridColumn[];
    Fields: GridField[];
    FieldsCount: number;
    ID: number;
    Title: string;
    Type: string;
}

export class GridField {
    name: string;
    type: string;
}

export class GridColumn {
    text: string;
    datafield: string;
    width: string;
}