import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, ElementRef, OnInit, HostListener } from '@angular/core';
import { SelectItemGroup } from 'primeng/api';
import * as _ from 'lodash';
import { FieldCondition, FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { Operator, OperatorModel } from '../../../models/operator.model';
import { AdvancedFilterFieldCondition } from './advanced-filtering.models';
import { DeclareFunctionStmt } from '@angular/compiler';
import { FieldType } from '../../../models/fieldtype-api.model';
import { valueOf } from 'core-js/fn/symbol/match';

@Component({
    selector: 'filter-item',
    templateUrl: 'filter-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FilterItemComponent implements OnInit {
    @Input() condition: AdvancedFilterFieldCondition;
    @Input() fields: FieldTypeAPIModelFieldCondition[] = null;
    @Input() operators: OperatorModel[] = [];

    allFieldsDropdown: SelectItemGroup[] = [];

    isSelectingCurrentField: boolean = false;
    isSelectingValue: boolean = false;

    currentOperator: any;
    currentValue: any;

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef) {
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
        this.isSelectingValue = true;
    }

    confirmValue() {
        this.condition.operator = this.currentOperator;
    }

    cancel() {
        this.isSelectingValue = false
        this.condition = new AdvancedFilterFieldCondition();
    }

    getFilterLabel() {
        if (!this.condition.field) {
            return "Add filter";
        }
        else {
            if (!this.condition.operator) {
                return this.condition.friendlyFieldName + ": Any"
            }
            else {
                switch (this.condition.operator) {
                    case Operator.IsTrue:
                        return this.condition.friendlyFieldName + ": True";
                    case Operator.IsFalse:
                        return this.condition.friendlyFieldName + ": False";
                    default:
                        return "Format not defined";
                }

            }
        }
    }

    needsValue() {
        switch (this.getTypeForCondition(this.condition)) {
            case "Boolean": return false;
        }

        switch (this.currentOperator as Operator) {
            case Operator.Populated:
            case Operator.NotPopulated:
                return false;
            default: return true;
        }
    }
}
