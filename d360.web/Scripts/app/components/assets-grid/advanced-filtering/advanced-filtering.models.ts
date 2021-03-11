import { DatePipe } from "@angular/common";
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
    value2: any;

    friendlyFieldName: string = '';
    markForDeletion: boolean = false;
    fieldType: string = "";

    constructor(private datePipe: DatePipe) {

    }

    public getTooltipValue() {
        if (!this.operator) {
            return "";
        }
        return `Filter: ${this.getDescriptionText()}<br/>Click to modify`;
    }


    public getDescriptionText() {
        if (!this.operator) {
            return "";
        }
        let str: string = this.friendlyFieldName;

        switch (this.operator.toString()) {
            case "IsTrue":
                return this.friendlyFieldName + ": True";
            case "IsFalse":
                return this.friendlyFieldName + ": False";
            case "Contains":
                str += " contains ";
                break;
            case "NotContains":
                str += " does not contain ";
                break;
            case "Equals":
                str += " is ";
                break;
            case "NotEquals":
                str += " is not ";
                break;
            case "StartsWith":
                str += " starts with ";
                break;
            case "EndsWith":
                str += " ends with ";
                break;
            case "Populated":
                str += " is populated ";
                break;
            case "NotPopulated":
                str += " is not populated ";
                break;
            case "Before":
                str += " is before ";
                break;
            case "LessThan":
                str += " is less than ";
                break;
            case "OnOrBefore":
                str += " is on or before ";
                break;
            case "LessThanOrEquals":
                str += " is less than or equal ";
                break;
            case "After":
                str += " is after ";
                break;
            case "GreaterThan":
                str += " is greater than ";
                break;
            case "OnOrAfter":
                str += " is on or before ";
                break;
            case "GreaterThanOrEquals":
                str += " is greater than or equal ";
                break;
            case "Between":
                str += " is between ";
                break;
            default:
                return "Description Text not defined";
        }

        if (this.value) {
            str += this.getTypedValue();
        }
        if (this.value2) {
            str += " and " + this.getTypedValue2();
        }
        return str;
    }

    public getFilterLabel() {
        if (!this.field) {
            return "Add filter";
        }
        if (!this.operator) {
            return this.friendlyFieldName + ": Any"
        }
        switch (this.operator.toString()) {
            case "IsTrue":
                return this.friendlyFieldName + ": True";
            case "IsFalse":
                return this.friendlyFieldName + ": False";
            case "Contains":
                return `${this.friendlyFieldName} : *${this.getTypedValue()}*`;
            case "NotContains":
                return `${this.friendlyFieldName} &#8800; *${this.getTypedValue()}*`;
            case "Equals":
                return `${this.friendlyFieldName} : ${this.getTypedValue()}`;
            case "NotEquals":
                return `${this.friendlyFieldName} &#8800; ${this.getTypedValue()}`;
            case "StartsWith":
                return `${this.friendlyFieldName} : ${this.getTypedValue()}*`;
            case "EndsWith":
                return `${this.friendlyFieldName} : *${this.getTypedValue()}`;
            case "Populated":
                return `${this.friendlyFieldName} : populated`;
            case "NotPopulated":
                return `${this.friendlyFieldName} : not populated`;
            case "Before":
            case "LessThan":
                return `${this.friendlyFieldName} < ${this.getTypedValue()}`;
            case "OnOrBefore":
            case "LessThanOrEquals":
                return `${this.friendlyFieldName} &#8804; ${this.getTypedValue()}`;
            case "After":
            case "GreaterThan":
                return `${this.friendlyFieldName} > ${this.getTypedValue()}`;
            case "OnOrAfter":
            case "GreaterThanOrEquals":
                return `${this.friendlyFieldName} &#8805; ${this.getTypedValue()}`;
            case "Between":
                return `${this.friendlyFieldName} : ${this.getTypedValue()} - ${this.getTypedValue2()}`;
            default:
                return "Format not defined 11";
        }

    }

    getOperatorString(): string {
        if (!this.operator) {
            return "ne null";
        }

        switch (this.operator.toString()) {
            case "IsTrue":
                return "eq true";
            case "IsFalse":
                return "eq false";
            case "Contains":
                return `ct`;
            case "NotContains":
                return `nct`;
            case "Equals":
                return `eq`;
            case "NotEquals":
                return `ne`;
            case "StartsWith":
                return `ct`;
            case "EndsWith":
                return `ct`;
            case "Populated":
                return `ne null`;
            case "NotPopulated":
                return `eq null`;
            case "LessThan":
            case "Before":
                return `lt`;
            case "OnOrBefore":
            case "LessThanOrEquals":
                return `le`;
            case "After":
            case "GreaterThan":
                return `gt`;
            case "OnOrAfter":
            case "GreaterThanOrEquals":
                return `ge`;
            default:
                return "---";
        }
    }

    getTypedValue(value: any = null): any {
        try {
            if (value == null) {
                value = this.value;
            }

            if (this.fieldType == "Number" || this.fieldType == "Decimal") {
                return +value;
            }

            if (this.fieldType == "Date") {
                return `${this.parseDateToString(value)}`
            }
            if (this.fieldType == "DateTime") {
                return `${this.parseDateTimeToString(value)}`
            }
            if (this.fieldType == "Lookup") {
                if (value.value) {
                    return `'${value.value}'`
                }
            }
            return value;
        }
        catch (ex) {
            console.log(value);
        }
    }

    getTypedValue2(): any {
        return this.getTypedValue(this.value2);
    }

    getValue(value: any = null): string {
        if (value == null) {
            value = this.value;
        }

        if (value == null) {
            return "";
        }
        switch (this.operator.toString()) {
            case "StartsWith":
                value = value + "*";
                break;
            case "EndsWith":
                value = "*" + value;
                break;
        }
        if (this.fieldType == "Number" || this.fieldType == "Decimal") {
            return value;
        }

        if (this.fieldType == "Date") {
            return `'${this.parseDateToString(value, true)}'`
        }

        if (this.fieldType == "DateTime") {
            return `'${this.parseDateTimeToString(value, true)}'`
        }

        if (this.fieldType == "Lookup") {
            if (value.value) {
                return `'${value.value}'`
            }
        }

        return `'${value}'`;
    }
    getValue2(): string {
        return this.getValue(this.value2);
    }

    private parseDateToString(value: any, forApi = false) {
        if (forApi) {
            return this.datePipe.transform(value, "yyyy-MM-dd");
        }
        return this.datePipe.transform(value, "shortDate");
    }

    private parseDateTimeToString(value: any, forApi = false) {
        var date = new Date(value);
        date.setMinutes(date.getMinutes() + date.getTimezoneOffset());
        if (forApi) {
            return this.datePipe.transform(date, "yyyy-MM-ddTHH:mm");
        }
        return this.datePipe.transform(value, "shortDate") + " " + this.datePipe.transform(value, "HH:mm");
    }
}

export class AdvancedFilterFieldConditionCollection {
    connector: string = " and ";
    filters: AdvancedFilterFieldCondition[] = [];

    public getQueryStringValue(): string {
        if (this.filters.length === 0) {
            return "";
        }

        let queries: string[] = [];
        this.filters.filter(x => x.field && x.operator).forEach((cond) => {
            let fieldName: string = cond.field;
            if (cond.operator.toString() === "Between") {
                queries.push(`(${fieldName} gt ${cond.getValue()} and ${fieldName} lt ${cond.getValue2()})`);
            }
            else {
                let operation: string = cond.getOperatorString();
                let value: string = cond.getValue();
                queries.push(`(${fieldName} ${operation} ${value})`);
            }
        });
        return queries.join(" and ");
    }
}
