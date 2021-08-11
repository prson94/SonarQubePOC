import { ObjectRelationship } from './relationship.model';
import { SelectItem } from 'primeng/api';

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

    public getAsV2ApiFilter() {
        let filters: string[] = [];
        let condition: string = this.includeType == 'Any' ? ' or ' : ' and ';
        let relUid: string = this.relationshipType.Uid;
        if (this.objectIds) {
            this.objectIds.forEach(opt => {

                filters.push(`${relUid} eq ${opt}`);
            });
        }
        if (filters.length > 0)
            return `(${filters.join(condition)})`;

        return '';
    }
}

export class GridOwnerFilter {
    ownerUsers: string[];
    ownerGroups: string[];

    public getAsV2ApiFilter() {
        let filters: string[] = this.ownerGroups.concat(this.ownerUsers);
        return filters.join(',');
    }
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
        var f = fieldColumns.find(x => x.datafield.toLowerCase() == this.field.toLowerCase());
        var cond = this.convertCondition(this.condition);
        let multiValueDelimiter = '!~!';

        if (this.fieldtype == 0 && this.field == 'Parent') {
            f = new GridFilterColumn();
            f.fieldType = 'Lookup';
            f.apiName = 'ParentDisplayName';
        }
        let forceEqualFields: string[] = ['Boolean', 'Lookup', 'Decimal', 'Number'];

        if (forceEqualFields.some(x => x == f.fieldType)) {
            cond = 'eq';
        }

        if (this.value) {
            var values = this.value.split(multiValueDelimiter);
            let expressions: string[] = [];

            values.forEach(value => {
                var val = this.wrapValue(f.fieldType, value);
                expressions.push(`${f.apiName} ${cond} ${val}`);
            })

            if (expressions.length > 1) {
                return `(${expressions.join(' or ')})`;
            }

            return expressions.join(' or ');
        }

        return '';
    }

    private wrapValue(fieldType, value): string {
        if (fieldType == 'Number' || fieldType == 'Decimal' || fieldType == 'Boolean') {
            return value;
        }

        value = (value as string).replace(/'/g, "&apos;");
        return `'${encodeURIComponent(value)}'`;
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
    uidfield: string;
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
    ScoreAllocations: GridScoreAllocation[];
    HasProfiling: boolean;
}

export class GridScoreAllocation {
    Name: string;
    LowerThreshold: number;
    UpperThreshold: number;
    ScoreType: number;
}


export class DynamicGridDefinitionBase {
    Columns: GridFilterColumn[];
    Fields: GridField[];
}


export class LookupGrid extends DynamicGridDefinitionBase {
    Values: any[];
    ScoringInfo: any[];
    total: number;
}

export class LookupGridField {
    objectId: number;
    name: string;
    description: string;
    url: string;
    fieldTypeId: number;
    showDescription: boolean;
}

export class DynamicGridResultsInData extends DynamicGridDefinitionBase {
    Data: any[];
}