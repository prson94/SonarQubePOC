import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, ElementRef, OnInit, HostListener, OnChanges } from '@angular/core';
import { SelectItem, SelectItemGroup } from 'primeng/api';
import * as _ from 'lodash';
import { FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { OperatorModel } from '../../../models/operator.model';
import { AdvancedFilterFieldCondition, SystemFields } from './advanced-filtering.models';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { AssetService } from '../../../services/asset.service';
import { AssetTypeService } from '../../../services/asset-type.service';

@Component({
    selector: 'filter-item',
    templateUrl: 'filter-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService, AssetTypeService]
})
export class FilterItemComponent implements OnInit, OnChanges {
    @Input() assetTypeUid: string = "";
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
        private fieldsService: FieldsObservableService,
        private assetTypeService: AssetTypeService
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
        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            return "";
        }
        var ft = this.getFieldType(item);
        if (!ft) return '';
        return Object.keys(ft.Type)[0];
    }

    getOperators(item: AdvancedFilterFieldCondition) {
        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            var options: SelectItem[] = [];
            options.push({ label: "contains", value: "Contains" });
            options.push({ label: "not contains", value: "NotContains" });
            return options;
        }

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
        if (type.Type) {
            this.condition.friendlyFieldName = type.FriendlyName;
            this.condition.fieldType = this.getTypeForCondition(this.condition);
            if (this.condition.fieldType === "Lookup") {
                this.loadLookupValues();
            }
            this.uiCurrentOperatorsList = this.getOperators(this.condition);
            this.uiFilterLabel = this.condition.getFilterLabel();
        }
        else {
            if (this.condition.field === SystemFields.OwnedByFieldCode) {
                this.condition.friendlyFieldName = "Owned By";
                this.condition.fieldType = null;
                this.loadLookupValuesForOwners();
                this.uiCurrentOperatorsList = this.getOperators(this.condition);
                this.uiFilterLabel = this.condition.getFilterLabel();
            }
        }
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
                        this.currentField.Values.push({ title: str, value: str});
                    })
                })
        }
    }

    loadLookupValuesForOwners() {
        this.currentField = this.fields.filter(x => x.Name === this.condition.field)[0];
        if (!this.currentField.Values || this.currentField.Values.length == 0) {
            this.isLookupValuesLoading = true;
            this.assetTypeService.GetAssetTypePossibleOwners(this.assetTypeUid).subscribe((res) => {
                this.isLookupValuesLoading = false;
                this.currentField.Values = [];
                let mapped: any[] = [];
                res.forEach((item) => {
                    if (item.Name.indexOf("] - ")) {
                        var data = (item.Name as string).split("] - ");
                        mapped.push({ value: item.Uid, title: data[1], group: data[0] });
                    }
                    else {
                        mapped.push({ value: item.Uid, title: item.Name });
                    }
                });

                mapped.filter(x => !x.group).forEach(str => {
                    this.currentField.Values.push({ title: str.title, value: str.value });
                })

                var grouped = _.mapValues(_.groupBy(mapped, "group"),
                    clist => clist.map(item => _.omit(item, "group")));

                var keys = Object.keys(grouped);
                keys.forEach((key) => {
                    console.log(key);
                    console.log(grouped[key]);
                    this.currentField.Values.push({ title: key, value: key, });
                })
            });
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

    fieldInputType() {
        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            return "lookup";
        }

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
