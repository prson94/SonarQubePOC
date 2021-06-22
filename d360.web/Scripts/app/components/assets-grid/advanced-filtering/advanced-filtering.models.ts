import { DatePipe } from "@angular/common";
import * as _ from "lodash";
import { SelectItem } from "primeng/api";
import { FieldType, FieldTypeAPIModelField } from "../../../models/fieldtype-api.model";
import { ScoreTypeAllocation } from "../../../models/metrics.model";
import { Operator } from "../../../models/operator.model";
import { RelationshipType } from "../../../models/relationship.model";

export class FieldTypeAPIModelFieldCondition extends FieldTypeAPIModelField {
    Values: SelectItem[];
    Operators: SelectItem[];

    IsOwnerField?: boolean = false;
    IsSystemField?: boolean = false;
    IsRelationship?: boolean = false;
}

export class AdvancedFilterFieldCondition {
    field: string;
    operator: Operator;
    value: any;
    value2: any;

    friendlyFieldName: string = "";
    markForDeletion: boolean = false;
    isNew: boolean = false;
    fieldType: string = "";

    type?: FieldTypeAPIModelField;

    connectingOperator: string = "or";
    isRelationship?: boolean = false;
    relationshipCardinality?: string = "";

    isDefaultFilter?: boolean = false;

    isPreloaded?: boolean = false;
    isConfirmed?: boolean = false;

    relationshipFieldName?: string = "";

    constructor(private datePipe: DatePipe) {

    }

    public getTooltipValue() {
        if (!this.operator) {
            return this.friendlyFieldName + ": Any";
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
                return this.friendlyFieldName + " is True";
            case "IsFalse":
                return this.friendlyFieldName + " is False";
            case "Contains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    str += " is ";
                }
                else {
                    str += " contains ";
                }
                break;
            case "NotContains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    str += " is not ";
                }
                else {
                    str += " does not contain ";
                }
                break;
            case "Equals":
                if ((this.isRelationship && this.relationshipCardinality !== "One") || this.field === SystemFields.OwnedByFieldCode) {
                    str += " contains ";
                }
                else {
                    str += " is ";
                }
                break;
            case "NotEquals":
                if ((this.isRelationship && this.relationshipCardinality !== "One") || this.field === SystemFields.OwnedByFieldCode) {
                    str += " does not contain ";
                }
                else {
                    str += " is not ";
                }
                break;
            case "StartsWith":
                str += " starts with ";
                break;
            case "EndsWith":
                str += " ends with ";
                break;
            case "Populated":
                if (this.isRelationship || this.fieldType === "Relationship") {
                    str += " relationships exist ";
                }
                else {
                    str += " is populated ";
                }
                break;
            case "NotPopulated":
                if (this.isRelationship || this.fieldType === "Relationship") {
                    str += " relationships do not exist ";
                }
                else {
                    str += " is not populated ";
                }
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
            case "IsInBand":
                str += " is in band ";
                break;
            default:
                return "Description Text not defined";
        }

        if (this.value) {
            str += this.getTypedValue();
        }
        if (this.value2 && this.operator.toString() === "Between") {
            str += " and " + this.getTypedValue2();
        }
        return str;
    }

    public getFilterLabel() {
        if (!this.field) {
            return "Add filter";
        }
        if (!this.operator) {
            return this.friendlyFieldName + ": Any";
        }
        var fieldName = this.friendlyFieldName;

        switch (this.operator.toString()) {
            case "IsTrue":
                return fieldName + " is True";
            case "IsFalse":
                return fieldName + " is False";
            case "Contains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    return `${fieldName} is ${this.getTypedValue(this.value, true)}`;
                }
                return `${fieldName} : *${this.getTypedValue(this.value, true)}*`;
            case "NotContains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    return `${fieldName} is not ${this.getTypedValue(this.value, true)}`;
                }
                return `${fieldName} &#8800; *${this.getTypedValue(this.value, true)}*`;
            case "Equals":
                return `${fieldName} : ${this.getTypedValue(this.value, true)}`;
            case "NotEquals":
                return `${fieldName} &#8800; ${this.getTypedValue(this.value, true)}`;
            case "StartsWith":
                return `${fieldName} : ${this.getTypedValue()}*`;
            case "EndsWith":
                return `${fieldName} : *${this.getTypedValue()}`;
            case "Populated":
                if (this.isRelationship || this.fieldType === "Relationship") {
                    return `${fieldName} exists`;
                }
                return `${fieldName} : populated`;
            case "NotPopulated":
                if (this.isRelationship || this.fieldType === "Relationship") {
                    return `${fieldName} does not exist`;
                }
                return `${fieldName} : not populated`;
            case "Before":
            case "LessThan":
                return `${fieldName} < ${this.getTypedValue()}`;
            case "OnOrBefore":
            case "LessThanOrEquals":
                return `${fieldName} &#8804; ${this.getTypedValue()}`;
            case "After":
            case "GreaterThan":
                return `${fieldName} > ${this.getTypedValue()}`;
            case "OnOrAfter":
            case "GreaterThanOrEquals":
                return `${fieldName} &#8805; ${this.getTypedValue()}`;
            case "Between":
                return `${fieldName} : ${this.getTypedValue()} - ${this.getTypedValue2()}`;
            case "IsInBand":
                return `${fieldName} is in band ${this.getTypedValue()}`;
            default:
                return "Any";
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
                return "ct";
            case "NotContains":
                return "nct";
            case "Equals":
                return "eq";
            case "NotEquals":
                return "ne";
            case "StartsWith":
                return "ct";
            case "EndsWith":
                return "ct";
            case "Populated":
                return "ne null";
            case "NotPopulated":
                return "eq null";
            case "LessThan":
            case "Before":
                return "lt";
            case "OnOrBefore":
            case "LessThanOrEquals":
                return "le";
            case "After":
            case "GreaterThan":
                return "gt";
            case "OnOrAfter":
            case "GreaterThanOrEquals":
                return "ge";
            default:
                return "---";
        }
    }

    getTypedValue(value: any = null, isForLabel: boolean = false): any {
        try {
            if (value == null) {
                value = this.value;
            }

            if (this.fieldType === "Counter") {
                return this.type.Type.Counter.CounterPrefix + value;
            }

            if (this.fieldType === "Number" || this.fieldType === "Decimal") {
                return +value;
            }
            if (this.fieldType === "Date") {
                return `${this.parseDateToString(value)}`;
            }
            if (this.fieldType === "DateTime") {
                return `${this.parseDateTimeToString(value)}`;
            }

            if (this.fieldType === "Score") {
                if (this.operator.toString() === "IsInBand") {
                    var stringValue = this.value as string;
                    return "'" + stringValue.slice(0, 1).toUpperCase() + stringValue.slice(1, stringValue.length) + "'";
                }
                return +value;
            }

            if (this.fieldType === "Lookup" || this.fieldType === "Tag" || this.fieldType === "Relationship" || this.field === SystemFields.OwnedByFieldCode || this.isRelationship) {
                let valueAsString = "";
                if (Array.isArray(value)) {
                    var arr = value as SelectItem[];
                    if (arr.length === 1) {
                        return arr[0].title;
                    }
                    if (isForLabel === true && arr.length > 2) {
                        if (this.field === SystemFields.OwnedByFieldCode) {
                            return arr.length + " users";
                        }
                        else {
                            return arr.length + " items";
                        }
                    }

                    var match = this.connectingOperator === "and" ? "(match all)" : "(match any)";

                    if (arr.length <= 5) {
                        valueAsString = arr.map((v) => v.title).join(", ");
                        valueAsString += " " + match;
                    }
                    else {
                        valueAsString = arr.slice(0, 5).map((v) => v.title).join(", ");
                        var leftover = arr.length - 5;
                        if (leftover === 1) {
                            valueAsString += ", 1 other item";
                        }
                        else {
                            valueAsString += `, ${leftover} other items`;
                        }
                        valueAsString += " " + match;
                    }

                    return valueAsString.trim();
                }
            }

            if (this.fieldType === "Path") {
                if (this.operator.toString() === "StartsWith" || this.operator.toString() === "EndsWith") {
                    return value;
                }

                let valueAsString = "";
                var stringArr = this.value as string[];
                if (stringArr.length === 1) {
                    return stringArr[0];
                }
                if (isForLabel === true && stringArr.length > 1) {
                    return stringArr.length + " items";
                }

                for (let i = 0; i < stringArr.length - 1; i++) {
                    if (i !== stringArr.length - 2) {
                        valueAsString += stringArr[i] + ", ";
                    }
                    else {
                        if (this.operator.toString() === "Contains") {
                            valueAsString += stringArr[i] + " " + this.connectingOperator + " " + stringArr[i + 1];
                        }
                        else if (this.operator.toString() === "NotContains") {
                            valueAsString += stringArr[i] + " nor " + stringArr[i + 1];
                        }
                    }
                }

                if (this.operator.toString().indexOf("Equals") !== -1) {
                    return stringArr.join("<i class='slim-fa fa fa-chevron-right'></i>");
                }

                return valueAsString.trim();

            }

            return value;
        }
        catch (ex) {
            console.warn(ex);
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
        if (this.fieldType === "Number" || this.fieldType === "Decimal") {
            return value;
        }

        if (this.fieldType === "Date") {
            return `'${this.parseDateToString(value, true)}'`;
        }

        if (this.fieldType === "DateTime") {
            return `'${this.parseDateTimeToString(value, true)}'`;
        }

        if (this.fieldType === "Lookup" && this.field === "[Level]") {
            if (value) {
                return `${value}`;
            }
        }

        if (this.fieldType === "Lookup") {
            if (value.value) {
                return `'${value.value}'`;
            }
        }
        value = (value as string).replace(/'/g, "&apos;");
        return `'${encodeURIComponent(value)}'`;
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

    public getCopyWithNewValue(newValue: string): AdvancedFilterFieldCondition {
        var newObj = _.cloneDeep(this);
        newObj.value = newValue;
        return newObj;
    }

    public getQueryString() {
        if (this.operator.toString() === "Between") {
            return `(${this.field} ge ${this.getValue()} and ${this.field} le ${this.getValue2()})`;
        }
        else {
            let operation: string = this.getOperatorString();
            let value: string = this.getValue();
            return `(${this.field} ${operation} ${value})`;
        }
    }
}

export class AdvancedFilterFieldConditionCollection {
    connector: string = " and ";
    filters: AdvancedFilterFieldCondition[] = [];
    allocations: ScoreTypeAllocation[] = [];

    public getFilters(allocations: ScoreTypeAllocation[]): Filters {
        this.allocations = allocations;
        var f = new Filters();
        f.filter = this.getQueryStringValue();
        return f;
    }

    private getQueryStringValue(): string {
        if (this.filters.length === 0) {
            return "";
        }
        let queries: string[] = [];
        let valuesArr: any[];
        this.filters.filter((x) => x.field && x.operator && x.markForDeletion !== true).forEach((cond) => {
            let treatAsRelationship: boolean =
                (cond.operator.toString() !== "Populated" && cond.operator.toString() !== "NotPopulated" && cond.relationshipFieldName.indexOf("|") === 36)
                || (cond.fieldType == null && cond.field.indexOf("|") === 36);

            if ((cond.fieldType === "Lookup" || cond.fieldType === "Tag" || cond.field === SystemFields.OwnedByFieldCode) && cond.value) {
                let subConditions: AdvancedFilterFieldCondition[] = [];
                valuesArr = cond.value as SelectItem[];
                valuesArr.filter((v) => v.value !== "").forEach((r) => {
                    if (cond.field === SystemFields.OwnedByFieldCode) {
                        if ((r.value as string).length === 36) {
                            subConditions.push(cond.getCopyWithNewValue(r.value));
                        }
                    }
                    else {
                        subConditions.push(cond.getCopyWithNewValue(r.value));
                    }
                });

                let subQueries: string[] = [];
                subConditions.forEach((sc) => {
                    subQueries.push(sc.getQueryString());
                });
                queries.push("(" + subQueries.join(" " + cond.connectingOperator + " ") + ")");
            }
            else if (cond.fieldType === "Score" && cond.operator.toString() === "IsInBand") {
                queries.push(this.getInBandQuery(cond));
            }
            else if (cond.fieldType === "Path" && cond.value) {
                if (cond.operator.toString() === "StartsWith" || cond.operator.toString() === "EndsWith") {
                    queries.push(cond.getQueryString());
                }
                else {
                    var stringArr = cond.value as string[];

                    if (cond.operator.toString().indexOf("Equals") === -1) {
                        let subConditions: AdvancedFilterFieldCondition[] = [];
                        stringArr.forEach((r) => {
                            subConditions.push(cond.getCopyWithNewValue(r));
                        });

                        let subQueries: string[] = [];
                        subConditions.forEach((sc) => {
                            subQueries.push(sc.getQueryString());
                        });
                        queries.push("(" + subQueries.join(" " + cond.connectingOperator + " ") + ")");
                    }
                    else {
                        if (cond.operator.toString() === "Equals") {
                            queries.push(`(${cond.field} eq '${(stringArr.join(' > '))}')`);
                        }
                        if (cond.operator.toString() === "NotEquals") {
                            queries.push(`(${cond.field} ne '${(stringArr.join(' > '))}')`);
                        }
                    }
                }
            }
            else if (treatAsRelationship) {
                let subConditions: AdvancedFilterFieldCondition[] = [];
                if (cond.value) {
                    valuesArr = cond.value as SelectItem[];
                    valuesArr.forEach((r) => {
                        var copyCond = cond.getCopyWithNewValue(r.value);

                        //in case of relationship field, but still treat as realtionship
                        if (cond.relationshipFieldName.indexOf("|") === 36) {
                            copyCond.field = cond.relationshipFieldName;
                        }

                        copyCond.field = "$Related:" + copyCond.field.split("|")[0];
                        subConditions.push(copyCond);
                    });

                    let subQueries: string[] = [];
                    subConditions.forEach((sc) => {
                        subQueries.push(sc.getQueryString());
                    });
                    queries.push("(" + subQueries.join(" " + cond.connectingOperator + " ") + ")");
                }
                else {
                    var copyCond = cond.getCopyWithNewValue(null);
                    copyCond.field = "$Related:" + copyCond.field.split("|")[0];
                    queries.push(copyCond.getQueryString());
                }
            }
            else {
                queries.push(cond.getQueryString());
            }
        });
        return queries.join(this.connector);
    }

    private getInBandQuery(cond: AdvancedFilterFieldCondition): string {
        if (cond.value) {
            let minValue: number = null;
            let maxValue: number = 100;
            let alloc = this.allocations.filter((x) => x.scoreType === cond.type.Type.Score.ScoreType)[0];
            switch (cond.value) {
                case "poor":
                    minValue = null;
                    maxValue = alloc.lowerThreshold;
                    break;
                case "average":
                    minValue = alloc.lowerThreshold;
                    maxValue = alloc.upperThreshold;
                    break;
                case "good":
                    minValue = alloc.upperThreshold;
                    maxValue = null;
                    break;
            }

            if (minValue === null) {
                return `(${cond.field} le '${maxValue}')`;
            }

            if (maxValue === null) {
                return `(${cond.field} gt '${minValue}')`;
            }

            return `(${cond.field} gt '${minValue}' and ${cond.field} le '${maxValue}')`;
        }
        return "";
    }
}

export class Filters {
    filter: string = "";

    public applyFilters(params: any) {
        delete params["_filter"];
        if (this.filter) {
            params._filter = this.filter;
        }

    }
}

export class SystemFields {
    public static OwnedByFieldCode: string = "$OwnedBy";
    public static RelationshipFieldCode: string = "$Related";

    public static GetSystemFieldDefinition(gridType: string): FieldTypeAPIModelFieldCondition[] {
        var fields: FieldTypeAPIModelFieldCondition[] = [];

        fields.push({
            Category: "System Fields",
            FriendlyName: "Date Created",
            Name: "CreatedOn",
            Type: new FieldType("Date"),
            Operators: [],
            Values: [],
            IsSystemField: true
        });


        fields.push({
            Category: "System Fields",
            FriendlyName: "Date Last Modified",
            Name: "UpdatedOn",
            Type: new FieldType("Date"),
            Operators: [],
            Values: [],
            IsSystemField: true
        });

        var owner: FieldTypeAPIModelFieldCondition = {
            Category: "System Fields",
            FriendlyName: "Owned By",
            Name: this.OwnedByFieldCode,
            Type: null,
            Operators: [],
            Values: [],
            IsOwnerField: true,
            IsSystemField: true
        };
        fields.push(owner);

        if (gridType === "Tree") {
            var level: FieldTypeAPIModelFieldCondition = {
                Category: "System Fields",
                FriendlyName: "Level",
                Name: "[Level]",
                Type: new FieldType("Lookup"),
                Operators: [],
                Values: [],
                IsSystemField: true
            };
            fields.push(level);
        }

        return fields;
    }

    public static GetRelationshipDefinition(relTypes: RelationshipType[], assetType: string): FieldTypeAPIModelFieldCondition[] {
        var fields: FieldTypeAPIModelFieldCondition[] = [];

        relTypes.forEach((r) => {
            try {
                let predicate: string = "";
                let typeName: string = "";
                let sideUid: string = "";
                if (r.Object.Uid === assetType) {
                    predicate = r.Predicate.Inverse;
                    typeName = r.Subject.Name;
                    sideUid = r.Subject.Uid;
                }
                else {
                    predicate = r.Predicate.Name;
                    typeName = r.Object.Name;
                    sideUid = r.Object.Uid;
                }

                typeName = typeName.split("/").join("<i class='slim-fa fa fa-chevron-right'></i>");

                var field = {
                    Category: "Relationships",
                    FriendlyName: `${predicate} ${typeName}`,
                    Name: r.Uid + "|" + sideUid,
                    Type: null,
                    Operators: [],
                    Values: [],
                    IsRelationship: true
                };
                field["predicate"] = predicate;

                fields.push(field);
            }
            catch (ex) {
                //GOV-14432 - catch error if there is something wrong with relationship type (missing prop)
                //to avoid breaking all advanced filtering 
                console.warn(ex, r);
            }
        });
        return fields.sort((a, b) => { return a.FriendlyName > b.FriendlyName ? 1 : -1; });
    }
}
