import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, ElementRef, OnInit, HostListener, OnChanges, AfterViewChecked } from "@angular/core";
import { LazyLoadEvent, SelectItem, SelectItemGroup } from "primeng/api";
import * as _ from "lodash";
import { FieldTypeAPIModelFieldCondition } from "../../../models/field-condition-grid.models";
import { OperatorModel } from "../../../models/operator.model";
import { AdvancedFilterFieldCondition, SystemFields } from "./advanced-filtering.models";
import { FieldsObservableService } from "../../../services/fieldsObservable.service";
import { AssetTypeService } from "../../../services/asset-type.service";
import { TagService } from "../../../services/tag.service";
import { RelationshipType } from "../../../models/relationship.model";
import { RelationshipsService } from "../../../services/relationships.service";
import { Subscription } from "rxjs";

@Component({
    selector: "filter-item",
    templateUrl: "filter-item.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService, AssetTypeService, TagService, RelationshipsService]
})
export class FilterItemComponent implements OnInit, OnChanges {
    @Input() assetTypeUid: string = "";
    @Input() condition: AdvancedFilterFieldCondition;
    @Input() fields: FieldTypeAPIModelFieldCondition[] = null;
    @Input() operators: OperatorModel[] = [];
    @Input() relationshipTypes: RelationshipType[] = [];

    lazyLoadSubscription: Subscription;

    currentField: FieldTypeAPIModelFieldCondition;

    allFieldsDropdown: SelectItemGroup[] = [];

    isSelectingCurrentField: boolean = false;
    isSelectingValue: boolean = false;


    tableSelection: any;

    isLookupValuesLoading: boolean = false;

    uiCurrentOperatorsList: any[] = [];
    currentOperator: any;
    currentInputType: string = "";
    doesNeedValue: boolean = false;

    uiTooltipValue: string = "";
    uiFilterLabel: string = "";

    uiIsAllDisabled: boolean = true;
    uiIsAnyDisabled: boolean = true;

    rollbackOperator: any;
    rollbackValue1: any;
    rollbackValue2: any;

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService,
        private assetTypeService: AssetTypeService,
        private tagService: TagService,
        private relationshipService: RelationshipsService
    ) {
    }

    ngOnChanges() {
        if (this.condition) {
            this.uiFilterLabel = this.condition.getFilterLabel();
        }

        if (this.condition.field && !this.currentField) {
            this.onFieldSelected(null);
        }
    }
    ngOnInit() {
        this.allFieldsDropdown = [];
        let assetFieldGroup: SelectItemGroup = { value: "asset-field", label: "Asset Fields", items: [] };
        let systemFieldsGroup: SelectItemGroup = { value: "system-field", label: "System Fields", items: [] };
        let relationshipGroup: SelectItemGroup = { value: "rel-field", label: "Relationships", items: [] };
        this.allFieldsDropdown.push(assetFieldGroup);
        this.allFieldsDropdown.push(systemFieldsGroup);
        this.allFieldsDropdown.push(relationshipGroup);

        this.fields.filter((x) => x.IsSystemField !== true).forEach((f) => {
            assetFieldGroup.items.push({ value: f.Name, label: f.FriendlyName });
        });

        SystemFields.GetSystemFieldDefinition().forEach((f) => {
            systemFieldsGroup.items.push({ value: f.Name, label: f.FriendlyName });
        });

        SystemFields.GetRelationshipDefinition(this.relationshipTypes, this.assetTypeUid).forEach((f) => {
            relationshipGroup.items.push({ value: f.Name, label: f.FriendlyName });
        });

    }

    interval: any = {};
    setTableWidth() {
        if (this.isSelectingValue) {
            if (this.interval) {
                clearInterval(this.interval);
            }

            var html = this.elRef.nativeElement as HTMLElement;
            var scrollWrapper = html.getElementsByClassName("p-datatable-scrollable-wrapper")[0];
            var selectionElement = html.getElementsByClassName("value-selection")[0];

            if (scrollWrapper) {
                (scrollWrapper as HTMLElement).style.width = 250 + "px";
            }

            if (selectionElement) {
                (selectionElement as HTMLElement).style.removeProperty("left");
            }

            this.interval = setInterval(() => this.updateDynamicWidths(), 20);
        }
    }

    updateDynamicWidths() {

        try {
            var html = this.elRef.nativeElement as HTMLElement;
            var topPosition = html.getBoundingClientRect().bottom;
            var tableElements = html.getElementsByClassName("item-value");
            var scrollWrapper = html.getElementsByClassName("p-datatable-scrollable-wrapper")[0];
            if (scrollWrapper) {
                let width = scrollWrapper.clientWidth;
                let oldWidth = width;
                if (tableElements.length > 0) {
                    for (let i = 0; i < tableElements.length; i++) {
                        var elementWidth = tableElements[i].clientWidth + 45;
                        if (elementWidth > width) {
                            width = elementWidth;
                        }
                    }
                }

                if (width > (window.outerWidth - 16)) {
                    width = window.outerWidth - 16;
                }

                var tableWrapper = html.getElementsByClassName("p-datatable-scrollable-wrapper")[0] as HTMLElement;
                if (tableWrapper && (Math.abs(oldWidth - width)) > 16) {
                    tableWrapper.style.width = (width + 5) + "px";

                    var difference = window.outerWidth - tableWrapper.getBoundingClientRect().right;
                    if (difference < 0) {
                        var selectionElement = html.getElementsByClassName("value-selection")[0] as HTMLElement;
                        var leftLocation = window.outerWidth - selectionElement.clientWidth;
                        if (leftLocation < 0) {
                            leftLocation = 0;
                        }
                        selectionElement.style.left = leftLocation + "px";
                        selectionElement.style.top = topPosition + "px";
                    }
                }
            }
        }
        catch (ex) {
            console.warn(ex);
        }
    }

    getTypeForCondition(item: AdvancedFilterFieldCondition) {
        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            return "";
        }

        if (this.currentField.IsRelationship) {
            return "";
        }
        var ft = this.getFieldType(item);
        if (!ft) {
            return "";
        }
        return Object.keys(ft.Type)[0];
    }

    getOperators(item: AdvancedFilterFieldCondition) {
        let options: SelectItem[] = [];
        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            options.push({ label: "contains", value: "Equals" });
            options.push({ label: "not contains", value: "NotEquals" });
            return options;
        }

        if (this.currentField.IsRelationship) {
            options = [];
            options.push({ value: "Equals", label: " contains " });
            options.push({ value: "NotEquals", label: " does not contains " });
            options.push({ value: "Populated", label: " exist " });
            options.push({ value: "NotPopulated", label: " do not exist " });
            return options;
        }

        var ft = this.getFieldType(item);

        if (ft.Type.Lookup && ft.Type.Lookup.List.AllowMultipleValues) {
            ft.Operators[0].label = "contains";
            ft.Operators[1].label = "does not contains";
        }

        return ft ? ft.Operators : [];
    }

    getValues(item: AdvancedFilterFieldCondition) {
        if (!this.getFieldType(item)) {
            return [];
        }
        return this.getFieldType(item).Values;
    }

    getFieldType(item: AdvancedFilterFieldCondition) {
        if (this.fields) {
            return this.fields.filter((x) => x.Name === item.field)[0];
        }

        return null;
    }

    updateFilter() {
        this.rollbackValue1 = this.condition.value;
        this.rollbackValue2 = this.condition.value2;
        this.rollbackOperator = this.condition.operator;

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
        if (this.fields.filter((x) => x.Name === this.condition.field).length !== 0) {
            this.currentField = this.fields.filter((x) => x.Name === this.condition.field)[0];
        }

        if (type.Type) {
            this.condition.friendlyFieldName = type.FriendlyName;
            this.condition.fieldType = this.getTypeForCondition(this.condition);
            this.condition.type = this.currentField;
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
            else if (this.currentField.IsRelationship) {
                this.condition.friendlyFieldName = this.currentField.FriendlyName;
                this.condition.fieldType = null;
                this.condition.isRelationship = true;

                this.uiCurrentOperatorsList = this.getOperators(this.condition);
                this.uiFilterLabel = this.condition.getFilterLabel();
            }
        }
        //if null, this method is not called from ui
        if (event && event.type !== "load") {
            this.isSelectingValue = true;
        }

        if (this.uiCurrentOperatorsList) {
            this.currentOperator = (this.uiCurrentOperatorsList[0] as SelectItem).value;
            this.updateOperatorData();
        }
    }

    getRelationshipCardinality(): string {
        if (!this.condition.isRelationship) {
            return "";
        }
        var data = this.condition.field.split("|");
        var obj = this.relationshipTypes.filter((x) => x.Uid === data[0])[0];

        return obj.Object.Uid === data[1] ? obj.Object.Cardinality : obj.Subject.Cardinality;
    }

    loadListLazy(event: LazyLoadEvent) {
        var params = { skip: event.first, take: event.rows, filter: event.globalFilter ?? "" };

        var type = this.getFieldType(this.condition);
        if (type.Type) {
            if (this.condition.fieldType === "Lookup") {
                this.loadLookupValues(params);
            }
            if (this.condition.fieldType === "Tag") {
                this.loadTagValues();
            }
        }
        else {
            if (this.condition.field === SystemFields.OwnedByFieldCode) {
                this.loadLookupValuesForOwners();
            }
            else if (this.currentField.IsRelationship) {
                this.loadRelationshipValues(params);
            }
        }
    }

    onOperatorSelected($event) {
        this.condition.operator = this.currentOperator;
        this.updateOperatorData();

        this.updateAllAnyData();
    }

    private updateOperatorData() {
        this.currentInputType = this.fieldInputType();
        this.doesNeedValue = this.needsValue();
    }

    loadLookupValues(params: any) {
        if (this.lazyLoadSubscription) {
            this.lazyLoadSubscription.unsubscribe();
        }

        this.lazyLoadSubscription = this.fieldsService.getLookupValues(this.currentField.AssetTypeUid, this.currentField.Name.trim(), params)
            .subscribe((res) => {
                if (!this.currentField.Values || this.currentField.Values.length === 0) {
                    this.currentField.Values = Array.from({ length: res.count });
                }

                let loadedData = [];

                res.items.forEach((str) => {
                    loadedData.push({ title: str, value: str });
                });

                Array.prototype.splice.apply(this.currentField.Values, [...[params.skip, params.take], ...loadedData]);

                this.currentField.Values = [...this.currentField.Values];
                this.setTableWidth();
                this.cdRef.markForCheck();
            });
    }


    loadTagValues() {
        if (!this.currentField.Values) {
            this.isLookupValuesLoading = true;

            this.tagService.getTagsList(true).subscribe((res) => {
                this.currentField.Values = [];
                res.forEach((str) => {
                    this.currentField.Values.push({ title: str.Value, value: str.Value });
                });

                this.isLookupValuesLoading = false;
                this.setTableWidth();
                this.cdRef.markForCheck();
            });


        }
    }

    loadLookupValuesForOwners() {
        if (!this.currentField.Values || this.currentField.Values.length === 0) {
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

                mapped.filter((x) => !x.group).forEach((str) => {
                    this.currentField.Values.push({ title: str.title, value: str.value });
                });

                var grouped = _.mapValues(_.groupBy(mapped, "group"),
                    (clist) => clist.map((item) => _.omit(item, "group")));

                var keys = Object.keys(grouped);
                keys.forEach((key) => {
                    this.currentField.Values.push({ title: key, value: key, disabled: true, styleClass: "group-name" });
                    grouped[key].forEach((d: SelectItem) => {
                        this.currentField.Values.push({ title: d.title, value: d.value });
                    });

                });

                this.isLookupValuesLoading = false;
                this.setTableWidth();

                this.cdRef.markForCheck();
            });
        }
    }

    loadRelationshipValues(params: any) {
        if (this.lazyLoadSubscription) {
            this.lazyLoadSubscription.unsubscribe();
        }


        this.lazyLoadSubscription = this.relationshipService
            .getRelationshipLookupValues(this.currentField.Name.split("|")[1], this.currentField.Name.split("|")[0], params)
            .subscribe((res) => {
                if (!this.currentField.Values || this.currentField.Values.length === 0) {
                    this.currentField.Values = Array.from({ length: res.count });
                }

                let loadedData = [];

                res.items.forEach((str) => {
                    let label: string = (str.label as string).split("].[").join(" <i class='slim-fa fa fa-chevron-right'></i> ").replace("[", "").replace("]", "");
                    loadedData.push({ title: label, value: str.value });
                });

                Array.prototype.splice.apply(this.currentField.Values, [...[params.skip, params.take], ...loadedData]);

                this.currentField.Values = [...this.currentField.Values];
                this.setTableWidth();

                this.cdRef.markForCheck();
            });
    }

    confirmValue() {
        this.isSelectingValue = false;
        this.condition.operator = this.currentOperator;
        this.updateOperatorData();

        this.uiTooltipValue = this.condition.getTooltipValue();
        this.uiFilterLabel = this.condition.getFilterLabel();
    }

    cancel() {
        this.condition.value = this.rollbackValue1;
        this.condition.value2 = this.rollbackValue2;
        this.condition.operator = this.rollbackOperator;
        this.currentOperator = this.rollbackOperator;

        this.isSelectingValue = false;
    }

    hasRemoveButton() {
        if (this.condition.isDefaultFilter) {
            return false;
        }

        return true;
    }

    remove() {
        if (this.condition.isDefaultFilter) {
            return;
        }
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
                this.condition.value = null;
                this.condition.value2 = null;
                return false;
            default: return true;
        }

    }

    onItemSelected($event) {
        this.updateAllAnyData();
    }

    private updateAllAnyData(event = null) {
        if (this.condition.fieldType === "Lookup") {
            if (this.currentField.Type.Lookup?.List?.AllowMultipleValues && (this.condition.value && (this.condition.value as any[]).length > 1)) {
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

        if (this.condition.fieldType === "Tag" && (this.condition.value && (this.condition.value as any[]).length > 1)) {
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

        if (this.condition.fieldType === "Path") {
            if (this.condition.operator.toString() === "Contains" && (this.condition.value && (this.condition.value as any[]).length > 1)) {
                this.uiIsAllDisabled = false;
                this.uiIsAnyDisabled = false;
            }
            else {
                this.condition.connectingOperator = "and";
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
        }
        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            if (this.condition.operator.toString() === "Equals") {
                this.uiIsAllDisabled = false;
                this.uiIsAnyDisabled = false;
            }
            else {
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
        }

        if (this.condition.isRelationship) {
            if (this.getRelationshipCardinality() === "Many" && this.condition.value && (this.condition.value as any[]).length > 1) {
                this.uiIsAllDisabled = false;
                this.uiIsAnyDisabled = false;
            }
            else {
                if (this.getRelationshipCardinality() === "One") {
                    this.condition.connectingOperator = "or";
                }
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
        }
    }

    fieldInputType() {
        if (!this.currentOperator) {
            return;
        }

        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            return "lookup";
        }

        var type = this.getTypeForCondition(this.condition);

        if (type === "Number" || type === "Decimal" || type === "Score") {

            if (this.currentOperator.toString() === "IsInBand") {
                this.currentField.Values = [];
                this.currentField.Values.push({ value: "poor", title: "Poor" });
                this.currentField.Values.push({ value: "average", title: "Average" });
                this.currentField.Values.push({ value: "good", title: "Good" });
                return "score-band";
            }

            //First handle special case eq. Between
            if (this.currentOperator.toString() === "Between") {
                return "multi-number";
            }

            return "number";
        }


        if (type === "Date") {
            //First handle special case eq. Between
            if (this.currentOperator.toString() === "Between") {
                return "multi-date";
            }

            return "date";
        }

        if (type === "DateTime") {
            //First handle special case eq. Between
            if (this.currentOperator.toString() === "Between") {
                return "multi-date-time";
            }

            return "date-time";
        }

        if (type === "Lookup" || type === "Tag" || this.currentField.IsRelationship) {
            return "lookup";
        }

        if (type === "Path") {
            if (this.currentOperator.toString() === "StartsWith" || this.currentOperator.toString() === "EndsWith") {
                return "text";
            }

            return "multi-input";
        }

        return "text";
    }

    isLazyLoad() {

        if (this.currentField.Name === SystemFields.OwnedByFieldCode
            || (this.currentField.Type && this.currentField.Type.Tag) !== null
        ) {
            return false;
        }

        return true;
    }

    isSaveDisabled() {
        if (!this.doesNeedValue) {
            return false;
        }

        if (this.currentInputType.indexOf("multi") !== -1) {
            return this.isEmpty(this.condition.value) || this.isEmpty(this.condition.value2);
        }
        else {
            return this.isEmpty(this.condition.value);
        }
    }

    isEmpty(value: any): boolean {
        if (value === null || (typeof value === "undefined") || (value as string).length === 0) {
            return true;
        }

        return false;
    }
}
