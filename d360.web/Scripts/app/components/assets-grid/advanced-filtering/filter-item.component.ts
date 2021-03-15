import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, ElementRef, OnInit, HostListener, OnChanges } from '@angular/core';
import { SelectItemGroup } from 'primeng/api';
import * as _ from 'lodash';
import { FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { OperatorModel } from '../../../models/operator.model';
import { AdvancedFilterFieldCondition, SystemFields } from './advanced-filtering.models';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';

@Component({
    selector: 'filter-item',
    templateUrl: 'filter-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService]
})
export class FilterItemComponent implements OnInit, OnChanges {
    @Input() condition: AdvancedFilterFieldCondition;
    @Input() fields: FieldTypeAPIModelFieldCondition[] = null;
    @Input() operators: OperatorModel[] = [];

    currentField: FieldTypeAPIModelFieldCondition;

    allFieldsDropdown: SelectItemGroup[] = [];

    isSelectingCurrentField: boolean = false;
    isSelectingValue: boolean = false;

    tableSelection: any;

    isLookupValuesLoading: boolean = false;

    uiCurrentOperatorsList: any[] = [];

    uiTooltipValue: string = "";
    uiFilterLabel: string = "";

    uiIsAllDisabled: boolean = true;
    uiIsAnyDisabled: boolean = true;

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService
    ) {
    }

    ngOnChanges() {
        if (this.condition) {
            this.uiFilterLabel = this.condition.getFilterLabel();
        }
    }
    ngOnInit() {
        this.allFieldsDropdown = [];
        let assetFieldGroup: SelectItemGroup = { value: "asset-field", label: "Asset Fields", items: [] };
        let systemFieldsGroup: SelectItemGroup = { value: "asset-field", label: "System Fields", items: [] };
        this.allFieldsDropdown.push(assetFieldGroup);
        this.allFieldsDropdown.push(systemFieldsGroup);

        this.fields.forEach((f) => {
            assetFieldGroup.items.push({ value: f.Name, label: f.FriendlyName });
        });

        SystemFields.GetSystemFieldDefinition().forEach((f) => {
            systemFieldsGroup.items.push({ value: f.Name, label: f.FriendlyName });
        });
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
        this.uiCurrentOperatorsList = this.getOperators(this.condition);
        this.uiFilterLabel = this.condition.getFilterLabel();
        this.isSelectingValue = true;
    }

    onOperatorSelected($event) {
        this.updateAllAnyData();
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
        this.isSelectingValue = false;

        this.uiTooltipValue = this.condition.getTooltipValue();
        this.uiFilterLabel = this.condition.getFilterLabel();
    }

    cancel() {
        this.isSelectingValue = false
    }

    remove() {
        this.condition.markForDeletion = true;
    }

    needsValue() {
        if (!this.condition.operator) {
            return false;
        }

        switch (this.getTypeForCondition(this.condition)) {
            case "Boolean": return false;
        }

        switch (this.condition.operator.toString()) {
            case "Populated":
            case "NotPopulated":
                this.condition.value = null;
                this.condition.value2 = null;
                return false;
            default: return true;
        }
    }

    onItemSelected($event) {
        this.updateAllAnyData();
    }

    private updateAllAnyData() {
        if (this.condition.fieldType !== "Lookup") {
            return;
        }
        if (this.currentField.Type.Lookup?.List?.AllowMultipleValues) {
            this.uiIsAllDisabled = false;
            this.uiIsAnyDisabled = false;
        }
        else {
            this.condition.connectingOperator = "or";
            if (this.condition.operator.toString() === "NotEquals") {
                this.condition.connectingOperator = "and";
            }

            this.uiIsAllDisabled = true;
            this.uiIsAnyDisabled = true;
        }
    }

    isSingleOrMultiSelect() {
        var type = this.getTypeForCondition(this.condition);
        if (type !== "Lookup") {
            return null;
        }
    }


    fieldInputType() {
        var type = this.getTypeForCondition(this.condition);

        if (type == "Number" || type == "Decimal") {
            //First handle special case eq. Between
            if (this.condition.operator.toString() === "Between") {
                return "multi-number";
            }

            return "number";
        }


        if (type == "Date") {
            //First handle special case eq. Between
            if (this.condition.operator.toString() === "Between") {
                return "multi-date";
            }

            return "date";
        }

        if (type == "DateTime") {
            //First handle special case eq. Between
            if (this.condition.operator.toString() === "Between") {
                return "multi-date-time";
            }

            return "date-time";
        }

        if (type == "Lookup") {
            return "lookup";
        }

        return "text";
    }
}
