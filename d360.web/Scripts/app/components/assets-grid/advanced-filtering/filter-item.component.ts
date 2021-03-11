import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, ElementRef, OnInit, HostListener } from '@angular/core';
import { SelectItemGroup } from 'primeng/api';
import * as _ from 'lodash';
import { FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { OperatorModel } from '../../../models/operator.model';
import { AdvancedFilterFieldCondition } from './advanced-filtering.models';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';

@Component({
    selector: 'filter-item',
    templateUrl: 'filter-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService]
})
export class FilterItemComponent implements OnInit {
    @Input() condition: AdvancedFilterFieldCondition;
    @Input() fields: FieldTypeAPIModelFieldCondition[] = null;
    @Input() operators: OperatorModel[] = [];

    currentField: FieldTypeAPIModelFieldCondition;

    allFieldsDropdown: SelectItemGroup[] = [];

    isSelectingCurrentField: boolean = false;
    isSelectingValue: boolean = false;

    currentOperator: any;
    currentValue: any;
    currentValue2: any;

    tableSelection: any;

    isLookupValuesLoading: boolean = false;

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService
    ) {
    }

    ngOnInit() {
        this.allFieldsDropdown = [];
        let assetFieldGroup: SelectItemGroup = { value: "asset-field", label: "Asset Fields", items: [] };
        this.allFieldsDropdown.push(assetFieldGroup);

        this.fields.forEach((f) => {
            assetFieldGroup.items.push({ value: f.Name, label: f.FriendlyName });
        })
    }

    getTypeForCondition(item: AdvancedFilterFieldCondition) {
        var ft = this.getFieldType(item);
        if (!ft) return '';
        return Object.keys(ft.Type)[0];
    }

    getOperators(item: AdvancedFilterFieldCondition) {
        var ft = this.getFieldType(item);
        return ft ? ft.Operators : [];
    }

    getValues(item: AdvancedFilterFieldCondition) {
        if (!this.getFieldType(item)) return [];
        return this.getFieldType(item).Values;
    }

    getFieldType(item: AdvancedFilterFieldCondition) {
        if (this.fields) {
            return this.fields.filter(x => x.Name === item.field)[0];
        }

        return null;
    }

    updateFilter() {
        if (!this.condition.field) {
            this.isSelectingCurrentField = true;
            this.isSelectingValue = false;
            const fieldDropdown = this.elRef.nativeElement.querySelector(".auto-open-dropdown");
            if (fieldDropdown) {
                setTimeout(() => {
                    fieldDropdown.click();
                });
            }
        }
        else {
            this.isSelectingCurrentField = true;
            this.isSelectingValue = true;
        }
    }

    onFieldSelected($event) {
        this.isSelectingCurrentField = false;
        var type = this.getFieldType(this.condition);
        this.condition.friendlyFieldName = type.FriendlyName;
        this.condition.fieldType = this.getTypeForCondition(this.condition);
        if (this.condition.fieldType === "Lookup") {
            this.loadLookupValues();
        }

        this.isSelectingValue = true;
    }

    loadLookupValues() {
        this.currentField = this.fields.filter(x => x.Name === this.condition.field)[0];
        if (!this.currentField.Values) {
            this.isLookupValuesLoading = true;

            this.fieldsService.getLookupValues(this.currentField.AssetTypeUid, this.currentField.Name.trim())
                .subscribe(res => {
                    this.isLookupValuesLoading = false;
                    this.currentField.Values = [];
                    res.forEach(str => {
                        this.currentField.Values.push({ title: str, value: str });
                    })
                })
        }
    }

    confirmValue() {

        if (this.needsValue() && !this.currentValue) {
            return;
        }
        this.condition.operator = this.currentOperator;
        if (this.needsValue()) {
            this.condition.value = this.currentValue;
            this.condition.value2 = this.currentValue2;
        }
        this.isSelectingValue = false;
    }

    cancel() {
        this.isSelectingValue = false
    }

    remove() {
        this.condition.markForDeletion = true;
    }

    needsValue() {
        if (!this.currentOperator) {
            return false;
        }

        switch (this.getTypeForCondition(this.condition)) {
            case "Boolean": return false;
        }

        switch (this.currentOperator.toString()) {
            case "Populated":
            case "NotPopulated":
                this.currentValue = null;
                this.currentValue2 = null;
                this.condition.value = null;
                return false;
            default: return true;
        }
    }

    fieldInputType() {
        var type = this.getTypeForCondition(this.condition);

        if (type == "Number" || type == "Decimal") {
            //First handle special case eq. Between
            if (this.currentOperator.toString() === "Between") {
                return "multi-number";
            }

            return "number";
        }


        if (type == "Date") {
            //First handle special case eq. Between
            if (this.currentOperator.toString() === "Between") {
                return "multi-date";
            }

            return "date";
        }

        if (type == "DateTime") {
            //First handle special case eq. Between
            if (this.currentOperator.toString() === "Between") {
                return "multi-date-time";
            }

            return "date-time";
        }

        if (type == "Lookup") {
            if (this.currentField.Type.Lookup?.List?.AllowMultipleValues) {
                return "lookup-multi";
            }
            return "lookup";
        }

        return "text";
    }
}
