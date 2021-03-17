import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, ElementRef, OnInit, HostListener, OnChanges } from '@angular/core';
import { SelectItem, SelectItemGroup } from 'primeng/api';
import * as _ from 'lodash';
import { FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { OperatorModel } from '../../../models/operator.model';
import { AdvancedFilterFieldCondition, SystemFields } from './advanced-filtering.models';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { AssetService } from '../../../services/asset.service';
import { AssetTypeService } from '../../../services/asset-type.service';
import { TagService } from '../../../services/tag.service';

@Component({
    selector: 'filter-item',
    templateUrl: 'filter-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService, AssetTypeService, TagService]
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
        private assetTypeService: AssetTypeService,
        private tagService: TagService
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

        this.fields.filter((x) => x.IsSystemField !== true).forEach((f) => {
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
            options.push({ label: "contains", value: "Equals" });
            options.push({ label: "not contains", value: "NotEquals" });
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
            this.currentField = this.fields.filter(x => x.Name === this.condition.field)[0];
            this.condition.type = this.currentField;
            if (this.condition.fieldType === "Lookup") {
                this.loadLookupValues();
            }
            if (this.condition.fieldType === "Tag") {
                this.loadTagValues();
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
        if (!this.currentField.Values) {
            this.isLookupValuesLoading = true;

            this.fieldsService.getLookupValues(this.currentField.AssetTypeUid, this.currentField.Name.trim())
                .subscribe(res => {
                    this.currentField.Values = [];
                    res.forEach(str => {
                        this.currentField.Values.push({ title: str, value: str });
                    })

                    this.isLookupValuesLoading = false;
                    this.cdRef.markForCheck();
                })
        }
    }


    loadTagValues() {
        if (!this.currentField.Values) {
            this.isLookupValuesLoading = true;

            this.tagService.getTagsList(true).subscribe((res) => {
                this.currentField.Values = [];
                res.forEach(str => {
                    this.currentField.Values.push({ title: str.Value, value: str.Value });
                })

                this.isLookupValuesLoading = false;
                this.cdRef.markForCheck();
            });


        }
    }

    loadLookupValuesForOwners() {
        if (!this.currentField.Values || this.currentField.Values.length == 0) {
            this.isLookupValuesLoading = true;
            this.assetTypeService.GetAssetTypePossibleOwners(this.assetTypeUid).subscribe((res) => {
                this.currentField.Values = [];
                let mapped: any[] = [];
                res.forEach((item) => {
                    if (item.Name.indexOf("] - ")) {
                        var data = (item.Name as string).split("] - ");
                        mapped.push({ value: item.Uid, title: data[1], group: data[0].replace("[", "") });
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
                    this.currentField.Values.push({ title: key, value: key, disabled: true, styleClass: "group-name" });
                    grouped[key].forEach((d: SelectItem) => {
                        this.currentField.Values.push({ title: d.title, value: d.value });
                    });

                })

                this.isLookupValuesLoading = false;
                this.cdRef.markForCheck();
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
        if (this.condition.fieldType === "Lookup") {
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

        if (this.condition.fieldType === "Tag") {
            if (this.condition.operator.toString() === "NotContains") {
                this.condition.connectingOperator = "and";
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }

            if (this.condition) {
                var count = (this.condition.value as any[]).length;
                if (count > 1) {
                    this.uiIsAllDisabled = false;
                    this.uiIsAnyDisabled = false;
                }
            }
        }
    }

    fieldInputType() {
        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            return "lookup";
        }

        var type = this.getTypeForCondition(this.condition);

        if (type == "Number" || type == "Decimal" || type == "Score") {

            if (this.condition.operator.toString() === "IsInBand") {
                this.currentField.Values = [];
                this.currentField.Values.push({ value: "poor", title: "Poor" });
                this.currentField.Values.push({ value: "average", title: "Average" });
                this.currentField.Values.push({ value: "good", title: "Good" });
                return "score-band";
            }

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

        if (type == "Lookup" || type === "Tag") {
            return "lookup";
        }

        return "text";
    }
}
