export class GridDefinition {
    Columns: GridColumn[];
    Fields: GridField[];
    FieldsCount: number;
    FilterColumns: GridFilterColumn[];
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

export class GridRelationshipFilterExpression {
    includeType: string = "Any";
    objectType: string;
    objectIds: string;
}

export class GridAttributeFilterExpression {
    attributeType: number;
    attributeSearchValue: string;
}

export class GridFilterExpression {
    field: string;
    condition: string;
    value: string;
}

export class GridFilterColumn {
    text: string;
    datafield: string;
    columntype: string;
    filteritems: string[];
}