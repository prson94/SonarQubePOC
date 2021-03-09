import { SelectItem } from "primeng/api";
import { FieldTypeAPIModelField } from "../../../models/fieldtype-api.model";

export class FieldTypeAPIModelFieldCondition extends FieldTypeAPIModelField {
    Values: SelectItem[];
    Operators: SelectItem[];
}

