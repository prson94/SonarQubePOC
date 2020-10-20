import { SelectItem } from "primeng/api";
import { FieldTypeAPIModelField } from "./fieldtype-api.model";

export class FieldTypeAPIModelFieldCondition extends FieldTypeAPIModelField {
    Values: SelectItem[];
    Operators: SelectItem[];
}

export class FieldCondition {
    field: string;
    operator: string;
    value: any;
    value2: any;

    disabled: boolean = true;
    isValid: boolean = false;

    hash: string = '';
}