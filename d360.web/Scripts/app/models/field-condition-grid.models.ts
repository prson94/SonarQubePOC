import { SelectItem, SelectItemGroup } from "primeng/api";
import { FieldTypeAPIModelField } from "./fieldtype-api.model";
import { Operator } from "./operator.model";

export class FieldTypeAPIModelFieldCondition extends FieldTypeAPIModelField {
    AssetTypeUid: string;
    RelationshipTypeUid: string;
    Values: SelectItem[];
    Operators: SelectItem[];

    IsSystemField?: boolean = false;
    IsRelationship?: boolean = false;
}

export class FieldCondition {
    field: string;
    operator: Operator;
    value: any;
    value2: any;

    disabled: boolean = true;
    isValid: boolean = false;

    hash: string = '';
}