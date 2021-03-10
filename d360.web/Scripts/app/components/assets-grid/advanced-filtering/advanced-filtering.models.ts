import { SelectItem } from "primeng/api";
import { FieldTypeAPIModelField } from "../../../models/fieldtype-api.model";
import { Operator } from "../../../models/operator.model";

export class FieldTypeAPIModelFieldCondition extends FieldTypeAPIModelField {
    Values: SelectItem[];
    Operators: SelectItem[];
}

export class AdvancedFilterFieldCondition {
    field: string;
    operator: Operator;
    value: any;

    friendlyFieldName: string = '';
}
