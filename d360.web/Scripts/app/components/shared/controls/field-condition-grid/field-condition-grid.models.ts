import { FieldTypeAPIModelField } from "../../../../models/fieldtype-api.model";
import { SelectItem } from "primeng/api";

export class FieldTypeAPIModelFieldCondition extends FieldTypeAPIModelField {
    Values: SelectItem[];
    Operators: SelectItem[];
}

