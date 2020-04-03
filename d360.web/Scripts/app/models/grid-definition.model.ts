import { ObjectRelationship } from './relationship.model';
import { SelectItem } from 'primeng/components/common/api';
import { FieldType } from './fields.model';

export class GridField {
    name: string;
    type: string;
    apiName: string;
    fieldType: string;
}

export class GridColumn {
    text: string;
    datafield: string;
    cellsformat: string;
    type: string;
    description: string;
    columnWidth: number;
}

export class GridRelationshipFilterExpression {
    includeType: string = "Any";
    objectIds: string[];
    options: SelectItem[];
    relationshipType: ObjectRelationship;
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

    public getAsV2ApiFilter(fieldColumns: GridFilterColumn[]): string {
        console.log(this);
        var f = fieldColumns.find(x => x.datafield.toLowerCase() == this.field.toLowerCase());
        console.log(f);
        var cond = this.convertCondition(this.condition);
        var val = this.wrapValue(f.fieldType, this.value);

        if (f.fieldType == 'Relationship') {
            this.condition = 'eq';
        }

        return `${f.apiName} ${cond} ${val}`;
    }

    private wrapValue(fieldType, value): string {
        if (fieldType == 'Number' || fieldType == 'Decimal') {
            return value;
        }
        return `'${value}'`;
    }

    private convertCondition(cond: string): string {
        switch (cond.toLowerCase()) {
            case 'contains': return 'ct';
            case 'equal': return 'eq';
            default: return 'ct';
        }
    }
}

export class GridFilterColumn {
    text: string;
    datafield: string;
    columntype: string;
    filteritems: string[];
    hiddenfield: boolean;
    id: string;
    type: string;
    description: string;
    value: any;
    disabled: boolean;
    parentFieldTypeID: number;
    canHaveMultipleFilters: boolean;
    fieldType: string;
    apiName: string;
}

export class GridDefinition {
    Columns: GridColumn[];
    Fields: GridField[];
    FieldsCount: number;
    FilterColumns: GridFilterColumn[];
    ID: number;
    Title: string;
    Type: string;
    TopLevelFilterColumns: GridFilterColumn[];
    IsReadOnly: boolean;
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