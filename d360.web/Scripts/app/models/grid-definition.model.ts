import { ObjectRelationship} from './relationship.model';

export class GridField {
    name: string;
    type: string;
}

export class GridColumn {
    text: string;
    datafield: string;
    width: string;
    cellsformat: string;
    type: string;
}

export class GridRelationshipFilterExpression {
    includeType: string = "Any";    
    objectIds: string[];
    relationshipType: ObjectRelationship;
}

export class GridAttributeFilterExpression {
    attributeType: number;
    attributeSearchValue: string;
}

export class GridOwnerFilter {
    ownerUsers: string[];
    ownerGroups: string[];
}

export enum GridFilterFieldType {
    Normal,
    Hidden,
    Relation
}

export class GridFilterExpression {
    field: string;
    condition: string;
    value: string;
    fieldtype: GridFilterFieldType;
}

export class GridFilterColumn {
    text: string;
    datafield: string;
    columntype: string;
    filteritems: string[];
    relatedfield: boolean;
    hiddenfield: boolean;
    id: string;

    type: string;
}

export class GridDefinition {
    Columns: GridColumn[];
    Fields: GridField[];
    FieldsCount: number;
    FilterColumns: GridFilterColumn[];
    ID: number;
    Title: string;
    Type: string;
}


export class DynamicGridDefinitionBase {
    Columns: GridFilterColumn[];
    Fields: GridField[];
}


export class LookupGrid extends DynamicGridDefinitionBase {    
    Values: any[];
}

export class DynamicGridResultsInData extends DynamicGridDefinitionBase {    
    Data: any[];    
}