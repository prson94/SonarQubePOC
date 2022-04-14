import { DatePipe } from "@angular/common";
import * as _ from "lodash";
import { SelectItem } from "primeng/api";
import { Observable } from "rxjs/internal/Observable";
import { FieldTypeAPIModelFieldCondition } from "../../../models/field-condition-grid.models";
import { FieldType, FieldTypeAPIModelField } from "../../../models/fieldtype-api.model";
import { ScoreTypeAllocation } from "../../../models/metrics.model";
import { Operator } from "../../../models/operator.model";
import { RelationshipType } from "../../../models/relationship.model";

export interface LookupValuesAPIParameters {
    skip?: number;
    take?: number;
    filter?: string;
}

export class LookupValuesAPIModel {
    count: number;
    items: any[];
}

export class AdvancedFilterFieldType extends FieldTypeAPIModelField {
    RemovePopulatedOperator?: boolean = false;
    ValueLoader?(params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel>;
}

type WithOptional<T, K extends keyof T> = Omit<T, K> & Partial<T>

export class FieldTypeAPIModelFieldAdvancedCondition extends FieldTypeAPIModelFieldCondition {
    Values: SelectItem[];
    Operators: SelectItem[];
    ValueLoader?(params: any): Observable<LookupValuesAPIModel>;

    IsOwnerField?: boolean = false;

    constructor(data: WithOptional<FieldTypeAPIModelFieldAdvancedCondition, "AssetTypeUid" | "ValueLoader" | "IsOwnerField" | "IsSystemField" | "IsRelationship" | "RelationshipTypeUid">) {
        super();
        Object.assign(this, data);
    }
}

export class AdvancedFilterFieldCondition {
    field: string;
    operator: Operator;
    value: any;
    value2: any;
    exact: boolean = false;

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
            return this.friendlyFieldName + ": " + $localize`Any`;
        }
        return $localize`Filter: ${this.getDescriptionText()}<br/>Click to modify`;
    }


    public getDescriptionText() {
        if (!this.operator) {
            return "";
        }
        let str: string = this.friendlyFieldName;

        switch (this.operator.toString()) {
            case "IsTrue":
                return this.friendlyFieldName + $localize` is True`;
            case "IsFalse":
                return this.friendlyFieldName + $localize` is False`;
            case "Contains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    str += $localize` is `;
                }
                else {
                    str += $localize` contains `;
                }
                break;
            case "NotContains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    str += " is not ";
                }
                else {
                    str += $localize` does not contain `;
                }
                break;
            case "Equals":
                if ((this.isRelationship && this.relationshipCardinality !== "One") || this.field === SystemFields.OwnedByFieldCode) {
                    str += $localize` contains `;
                }
                else {
                    str += $localize` is `;
                }
                break;
            case "NotEquals":
                if ((this.isRelationship && this.relationshipCardinality !== "One") || this.field === SystemFields.OwnedByFieldCode) {
                    str += $localize` does not contain `;
                }
                else {
                    str += $localize` is not `;
                }
                break;
            case "StartsWith":
                str += $localize` starts with `;
                break;
            case "EndsWith":
                str += $localize` ends with `;
                break;
            case "Populated":
                if (this.isRelationship || this.fieldType === "Relationship") {
                    str += $localize` relationships exist `;
                }
                else {
                    str += $localize` is populated `;
                }
                break;
            case "NotPopulated":
                if (this.isRelationship || this.fieldType === "Relationship") {
                    str += $localize` relationships do not exist `;
                }
                else {
                    str += $localize` is not populated `;
                }
                break;
            case "Before":
                str += $localize` is before `;
                break;
            case "LessThan":
                str += $localize` is less than `;
                break;
            case "OnOrBefore":
                str += $localize` is on or before `;
                break;
            case "LessThanOrEquals":
                str += $localize` is less than or equal `;
                break;
            case "After":
                str += $localize` is after `;
                break;
            case "GreaterThan":
                str += $localize` is greater than `;
                break;
            case "OnOrAfter":
                str += $localize` is on or before `;
                break;
            case "GreaterThanOrEquals":
                str += $localize` is greater than or equal `;
                break;
            case "Between":
                str += $localize` is between `;
                break;
            case "IsInBand":
                str += $localize` is in band `;
                break;
            default:
                return $localize`Description Text not defined`;
        }

        if (this.value) {
            str += this.getTypedValue();
        }
        if (this.value2 && this.operator.toString() === "Between") {
            str += $localize` and ` + this.getTypedValue2();
        }

        if (this.exact) {
            str += $localize` (match exact phrase)`;
        }

        return str;
    }

    public getFilterLabel() {
        if (!this.field) {
            return $localize`Add filter`;
        }
        if (!this.operator) {
            return this.friendlyFieldName + ": " + $localize`Any`;
        }
        var fieldName = this.friendlyFieldName;

        switch (this.operator.toString()) {
            case "IsTrue":
                return fieldName + $localize` is True`;
            case "IsFalse":
                return fieldName + $localize` is False`;
            case "Contains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    return `${fieldName} ` + $localize`is` + `${this.getTypedValue(this.value, true)}`;
                }
                return `${fieldName} : *${this.getTypedValue(this.value, true)}*`;
            case "NotContains":
                if (this.field === "CreatedOn" || this.field === "UpdatedOn") {
                    return `${fieldName} ` + $localize`is not` + ` ${this.getTypedValue(this.value, true)}`;
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
                    return $localize`${fieldName} exists`;
                }
                return $localize`${fieldName} : populated`;
            case "NotPopulated":
                if (this.isRelationship || this.fieldType === "Relationship") {
                    return $localize`${fieldName} does not exist`;
                }
                return $localize`${fieldName} : not populated`;
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
                return $localize`${fieldName} is in band ${this.getTypedValue()}`;
            default:
                return $localize`Any`;
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
                    let chevronHtml = "<i class='slim-fa fa fa-chevron-right'></i>";
                    let placeholder = "#chevronPlaceholder";

                    //escape user input, but avoid escaping chevron icons using js replace all method .split(x).join(y)
                    let arr = value
                        .map((v: SelectItem) =>
                            _.escape(v.title.split(chevronHtml).join(placeholder)).split(placeholder).join(chevronHtml)
                        );

                    if (arr.length === 1) {
                        return arr[0];
                    }
                    if (isForLabel === true && arr.length > 2) {
                        if (this.field === SystemFields.OwnedByFieldCode) {
                            return arr.length + ` ` + $localize`Users`.toLowerCase();
                        }
                        else {
                            return arr.length + ` ` + $localize`Items`.toLowerCase();
                        }
                    }

                    var match = this.connectingOperator === "and" ? "(" + $localize`Match all` + ")" : "(" + $localize`Match any` + ")";

                    valueAsString = arr.slice(0, 5).join(", ");
                    var leftover = arr.length - 5;
                    if (leftover === 1) {
                        valueAsString += ", " + $localize`1 other item`;
                    }
                    else if (leftover > 1) {
                        valueAsString += `, ${leftover}` + $localize`other items`;
                    }
                    valueAsString += " " + match;

                    return valueAsString.trim();
                }
            }

            if (this.fieldType === "Path") {
                if (this.operator.toString() === "StartsWith" || this.operator.toString() === "EndsWith") {
                    return _.escape(value);
                }

                let valueAsString = "";
                var stringArr = this.value.map((v) => _.escape(v)) as string[];
                if (stringArr.length === 1) {
                    return stringArr[0];
                }
                if (isForLabel === true && stringArr.length > 1) {
                    return stringArr.length + " " + $localize`Items`.toLowerCase();
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
            return _.escape(value);
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
        if (this.fieldType === "Number" || this.fieldType === "Decimal" || this.fieldType === "Counter") {
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
        f.data = _.cloneDeep(this.filters);
        return f;
    }

    private getQueryStringValue(): string {
        if (this.filters.length === 0) {
            return "";
        }
        const lenghtOfTheGuid = 36;
        let queries: string[] = [];
        let valuesArr: any[];
        this.filters.filter((x) => x.field && x.operator && x.markForDeletion !== true).forEach((cond) => {
            let treatAsRelationship: boolean =
                (cond.operator.toString() !== "Populated" && cond.operator.toString() !== "NotPopulated" && cond.relationshipFieldName.indexOf("|") === 36)
                || (cond.fieldType == null && cond.field.indexOf("|") === 36);

            if ((cond.fieldType === "Lookup" || cond.fieldType === "Tag" || cond.field === SystemFields.OwnedByFieldCode) && cond.value) {
                let subConditions: AdvancedFilterFieldCondition[] = [];
                valuesArr = cond.value as SelectItem[];

                if (cond.field === "Color" && typeof cond.value === "string") {
                    valuesArr = [];
                    valuesArr.push({ value: cond.value });
                }

                valuesArr.filter((v) => v.value !== "").forEach((r) => {
                    if (cond.field === SystemFields.OwnedByFieldCode) {
                        if ((r.value as string).length === lenghtOfTheGuid) {
                            subConditions.push(cond.getCopyWithNewValue(r.value));
                        } else if ((r.value as string).length > lenghtOfTheGuid) {
                            let ownerAndResponsibilitySubCondition = cond.getCopyWithNewValue(r.value)
                            ownerAndResponsibilitySubCondition.field = "$OwnedByAndResponsibility";
                            subConditions.push(ownerAndResponsibilitySubCondition);
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

            var scoreType = cond.type.Type.Score.ScoreType;
            var scoreTypeString = cond.type.Type.Score.ScoreType.toString();
            if (scoreType.toString() === "Governance") {
                scoreType = 1;
            }

            if (scoreType.toString() === "DataQuality") {
                scoreType = 2;
            }

            let alloc = this.allocations.filter((x) => x.scoreType === scoreType || x.scoreType.toString() === scoreTypeString)[0];

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
    data: any;

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

    public static GetSystemFieldDefinition(gridType: string): FieldTypeAPIModelFieldAdvancedCondition[] {
        var fields: FieldTypeAPIModelFieldAdvancedCondition[] = [];

        fields.push(new FieldTypeAPIModelFieldAdvancedCondition({
            Category: "System Fields",
            FriendlyName: "Date Created",
            Name: "CreatedOn",
            Type: new FieldType("Date"),
            Operators: [],
            Values: [],
            IsSystemField: true
        }));


        fields.push(new FieldTypeAPIModelFieldAdvancedCondition({
            Category: "System Fields",
            FriendlyName: "Date Last Modified",
            Name: "UpdatedOn",
            Type: new FieldType("Date"),
            Operators: [],
            Values: [],
            IsSystemField: true
        }));

        fields.push(new FieldTypeAPIModelFieldAdvancedCondition({
            Category: "System Fields",
            FriendlyName: "Owned By",
            Name: this.OwnedByFieldCode,
            Type: null,
            Operators: [],
            Values: [],
            IsOwnerField: true,
            IsSystemField: true
        }));

        if (gridType === "Tree") {
            fields.push(new FieldTypeAPIModelFieldAdvancedCondition({
                Category: "System Fields",
                FriendlyName: "Level",
                Name: "[Level]",
                Type: new FieldType("Lookup"),
                Operators: [],
                Values: [],
                IsSystemField: true
            }));
        }

        return fields;
    }

    public static GetRelationshipDefinition(relTypes: RelationshipType[], assetType: string): FieldTypeAPIModelFieldAdvancedCondition[] {
        var fields: FieldTypeAPIModelFieldAdvancedCondition[] = [];

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

                var field = new FieldTypeAPIModelFieldAdvancedCondition({
                    Category: "Relationships",
                    FriendlyName: `${predicate} ${typeName}`,
                    Name: r.Uid + "|" + sideUid,
                    Type: null,
                    Operators: [],
                    Values: [],
                    IsRelationship: true
                });
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

export class ComplexFieldDefinition {
    AssetUid: string = '';
    FieldApiName: string = '';
    FieldType: string = '';
}
