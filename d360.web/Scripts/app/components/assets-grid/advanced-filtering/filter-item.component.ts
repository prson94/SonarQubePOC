import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, ElementRef, OnInit, OnDestroy, OnChanges, Output, EventEmitter, HostListener, AfterViewChecked } from "@angular/core";
import { LazyLoadEvent, SelectItem, SelectItemGroup } from "primeng/api";
import * as _ from "lodash";
import { FieldTypeAPIModelFieldCondition } from "../../../models/field-condition-grid.models";
import { OperatorModel } from "../../../models/operator.model";
import { AdvancedFilterFieldCondition, ComplexFieldDefinition, SystemFields } from "./advanced-filtering.models";
import { FieldsObservableService } from "../../../services/fieldsObservable.service";
import { AssetTypeService } from "../../../services/asset-type.service";
import { TagService } from "../../../services/tag.service";
import { RelationshipType } from "../../../models/relationship.model";
import { Subscription } from "rxjs";
import { MultiInputField } from "../../shared/controls/multi-input-field/multi-input-field.component";
import { Table } from "primeng/table";
import { AssetService } from "../../../services/asset.service";

@Component({
    selector: "filter-item",
    templateUrl: "filter-item.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService, AssetTypeService, TagService, AssetService]
})
export class FilterItemComponent implements OnInit, OnChanges, OnDestroy {
    @Input() assetTypeUid: string = "";
    @Input() loadIdentifier: string = "";
    @Input() gridType: string = "List";
    @Input() condition: AdvancedFilterFieldCondition;
    @Input() fields: FieldTypeAPIModelFieldCondition[] = null;
    @Input() operators: OperatorModel[] = [];
    @Input() relationshipTypes: RelationshipType[] = [];

    @Output() onChange = new EventEmitter();

    nonValueOperators: string[] = ["Populated", "NotPopulated", "IsTrue", "IsFalse"];

    lazyLoadSubscription: Subscription;

    currentField: FieldTypeAPIModelFieldCondition;

    allFieldsDropdown: SelectItemGroup[] = [];

    isSelectingCurrentField: boolean = false;
    isSelectingValue: boolean = false;
    hasSelectAllCheckbox: boolean = false;

    tableSelection: any;

    isLookupValuesLoading: boolean = false;
    filterTableValue: string = "";

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

    maxNumberOfFilterCharacters: number = 2000;
    selectionScrollHeight: string = "34px";

    relationshipFieldIntersectTypeUid: string = "";
    relationshipFieldIntersectCardinality: string = "";
    relationshipFieldName: string = "";

    minSQLDate = new Date(1753, 0, 1);
    numberMax: number = null;
    numberMin: number = null;

    defaultColorOptions: any[] = [];

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;
    @ViewChild("multiInput", { static: false }) multiInputRef: MultiInputField;
    @ViewChild("dataTable", { static: false }) dataTable: Table;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService,
        private assetTypeService: AssetTypeService,
        private tagService: TagService,
        private assetService: AssetService
    ) {
        setInterval(() => {
            this.updateTopPosition();
            this.setSelectionVirtualScrollHeight();
        }, 25);

        this.assetService.getAllColors().subscribe((x) => {
            this.defaultColorOptions = x;
            this.cdRef.markForCheck();
        });
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

        if (this.fields && this.fields.length > 0) {
            let assetFieldGroup: SelectItemGroup = { value: "asset-field", label: "Asset Fields", items: [] };
            this.allFieldsDropdown.push(assetFieldGroup);

            this.fields.filter((x) => x.IsSystemField !== true).forEach((f) => {
                assetFieldGroup.items.push({ value: f.Name, label: f.FriendlyName });
            });
        }

        if (this.isAssetType) {
            var systemFields = SystemFields.GetSystemFieldDefinition(this.gridType);
            if (systemFields.length > 0) {
                let systemFieldsGroup: SelectItemGroup = { value: "system-field", label: "System Fields", items: [] };
                this.allFieldsDropdown.push(systemFieldsGroup);

                systemFields.forEach((f) => {
                    systemFieldsGroup.items.push({ value: f.Name, label: f.FriendlyName });
                });
            }

            if (SystemFields.GetRelationshipDefinition(this.relationshipTypes, this.assetTypeUid).length > 0) {
                let relationshipGroup: SelectItemGroup = { value: "rel-field", label: "Relationships", items: [] };
                this.allFieldsDropdown.push(relationshipGroup);

                SystemFields.GetRelationshipDefinition(this.relationshipTypes, this.assetTypeUid).forEach((f) => {
                    relationshipGroup.items.push({ value: f.Name, label: f.FriendlyName });
                });
            }
        }
    }

    ngOnDestroy() {
        this.stopUpdateDynamicWidths();
    }

    filterTable($event: any) {
        this.dataTable.filterGlobal($event, "contains");
    }

    setSelectionVirtualScrollHeight() {
        try {
            let count: number = 0;
            let res = [];

            if (!this.dataTable || !this.dataTable.value) {
                return;
            }

            var filter = this.dataTable?.filters?.global ? (this.dataTable?.filters?.global["value"] as string) : "";
            if (!filter || !this.dataTable.filteredValue) {
                res = new Array(this.dataTable.value.length);
            }
            else {
                res = new Array(this.dataTable.filteredValue.length);
            }
            if (res.length) {
                count = res.length;
            }

            if (this.condition && this.condition.field && this.condition.field === SystemFields.OwnedByFieldCode) {
                //add one row count for group name
                count++;
            }

            let calculatedHeight: number = 0;
            let maxHeight: number = 320;
            let minHeight: number = 50;
            let margins: number = 180;
            let bottomPos: number = (this.elRef.nativeElement as HTMLElement).getBoundingClientRect().bottom;

            if (count < 10) {
                calculatedHeight = count * 32;
                if (calculatedHeight < 32) {
                    calculatedHeight = 32;
                }

            }
            else {
                calculatedHeight = maxHeight;
            }

            var diff = window.innerHeight - calculatedHeight - margins - bottomPos;
            if (diff < 0) {
                calculatedHeight += diff;
            }

            if (calculatedHeight > maxHeight) {
                calculatedHeight = maxHeight;
            }
            if (calculatedHeight < minHeight) {
                calculatedHeight = minHeight;
            }
            this.selectionScrollHeight = calculatedHeight + "px";
        }
        catch (ex) {
            console.warn(ex);
            this.selectionScrollHeight = "320px";
        }
        this.cdRef.markForCheck();
    }

    removePositionStyling() {
        var html = this.elRef.nativeElement as HTMLElement;
        var selectionElement = html.getElementsByClassName("value-selection")[0] as HTMLElement;
        selectionElement.style.removeProperty("top");
        selectionElement.style.removeProperty("left");
        let fieldSelectionElement = html.getElementsByClassName("field-selection")[0] as HTMLElement;
        fieldSelectionElement.style.removeProperty("left");
    }

    updateDynamicWidths() {
        try {
            var html = this.elRef.nativeElement as HTMLElement;
            var scrollWrapper = html.getElementsByClassName("p-datatable-scrollable-wrapper")[0];
            if (scrollWrapper) {
                let width = 500 + 60;

                var tableWrapper = html.getElementsByClassName("p-datatable-scrollable-wrapper")[0] as HTMLElement;

                if (tableWrapper) {
                    var selectionElement = html.getElementsByClassName("value-selection")[0] as HTMLElement;

                    let distanceFromRight = window.outerWidth - html.getBoundingClientRect().left;
                    if (distanceFromRight < width) {
                        let diff = Math.abs(width - distanceFromRight);
                        selectionElement.style.left = (html.getBoundingClientRect().left - diff) + "px";
                    } else {
                        selectionElement.style.removeProperty("left");
                    }
                }
            }
        }
        catch (ex) {
            console.warn(ex);
        }
    }

    updateTopPosition() {
        try {
            var html = this.elRef.nativeElement as HTMLElement;
            var topPosition = html.getBoundingClientRect().bottom;
            var selectionElement = html.getElementsByClassName("value-selection")[0] as HTMLElement;

            if (selectionElement) {
                selectionElement.style.top = topPosition + "px";
            }

            const fieldSelectionLeftOffset = window.innerWidth - html.getBoundingClientRect().left - 350;
            let fieldSelectionElement = html.getElementsByClassName("field-selection")[0] as HTMLElement;
            if (fieldSelectionElement) {
                if (fieldSelectionLeftOffset < 0) {
                    fieldSelectionElement.style.left = fieldSelectionLeftOffset + "px";
                } else {
                    fieldSelectionElement.style.removeProperty("left");
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
            options.push({ label: "does not contain", value: "NotEquals" });
            return options;
        }

        if (this.currentField.IsRelationship) {
            var intersectTypeUid = this.currentField.Name.split("|")[0];
            var intersectType = this.relationshipTypes.filter((r) => r.Uid === intersectTypeUid)[0];

            this.relationshipFieldIntersectCardinality =
                intersectType.Object.Uid === this.assetTypeUid
                    ? intersectType.Subject.Cardinality : intersectType.Object.Cardinality;

            this.condition.relationshipCardinality = this.relationshipFieldIntersectCardinality;

            options = [];
            options.push({ value: "Equals", label: " is " });
            options.push({ value: "NotEquals", label: " is not " });
            options.push({ value: "Populated", label: " exists " });
            options.push({ value: "NotPopulated", label: " does not exist " });

            if (this.relationshipFieldIntersectCardinality === "Many") {
                options[0].label = "contains";
                options[1].label = "does not contain";
            }
            return options;
        }

        var ft = this.getFieldType(item);

        if (!ft) {
            return [];
        }

        if (ft.Type.Relationship) {
            options = [];
            options.push({ value: "Equals", label: " is " });
            options.push({ value: "NotEquals", label: " is not " });
            options.push({ value: "Populated", label: " exists " });
            options.push({ value: "NotPopulated", label: " does not exist " });

            if (this.relationshipFieldIntersectCardinality === "Many") {
                options[0].label = "contains";
                options[1].label = "does not contain";
            }
            return options;
        }

        if (ft.Type.Lookup && ft.Type.Lookup.List.AllowMultipleValues) {
            ft.Operators[0].label = "contains";
            ft.Operators[1].label = "does not contain";
        }


        if (this.isComplexField && this.complexFieldDefinition.FieldType === 'OwnershipLookup') {
            ft.Operators = ft.Operators.filter((x) => x.value !== "Populated" && x.value !== "NotPopulated");
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
        if (this.condition.value) {
            this.rollbackValue1 = JSON.parse(JSON.stringify(this.condition.value));
        }

        this.rollbackValue2 = this.condition.value2;
        this.rollbackOperator = this.condition.operator;

        if (!this.condition.field) {
            this.isSelectingCurrentField = true;
            this.isSelectingValue = false;
            const fieldDropdown = this.elRef.nativeElement.querySelector(".auto-open-dropdown");
            if (fieldDropdown) {
                setTimeout(() => {
                    fieldDropdown.click();
                }, 50);
            }
        }
        else {
            this.isSelectingValue = true;
            this.startUpdateDynamicWidths();
        }
        this.updateFocus();
    }

    updateFocus() {
        setTimeout(() => {
            var home = this.elRef.nativeElement as HTMLElement;
            if (this.condition.operator && this.currentInputType !== "date") {
                var valueRef = home.querySelector(".value-selector input") as HTMLElement;
                if (valueRef) {
                    valueRef.focus();
                }
            }
            else {
                var operatorRef = home.querySelector(".operator-selector input") as HTMLElement;
                if (operatorRef) {
                    operatorRef.focus();
                }
            }
        }, 25);
    }

    interval;

    stopUpdateDynamicWidths() {
        if (this.interval) {
            clearInterval(this.interval);
            this.removePositionStyling();
            this.interval = null;
        }
    }
    startUpdateDynamicWidths() {
        if (!this.interval) {
            this.interval = setInterval(() => this.updateDynamicWidths(), 20);
        }
    }

    onFieldSelected($event) {
        this.isSelectingCurrentField = false;
        this.relationshipFieldIntersectTypeUid = "";
        this.hasSelectAllCheckbox = false;


        var type = this.getFieldType(this.condition);
        if (this.fields.filter((x) => x.Name === this.condition.field).length !== 0) {
            this.currentField = this.fields.filter((x) => x.Name === this.condition.field)[0];
        }
        if (type.Type) {
            if (type.Type.Relationship) {
                this.relationshipFieldIntersectTypeUid = type.Type.Relationship.IntersectTypeUid;
                var relationship = this.relationshipTypes.filter((r) => r.Uid.toLowerCase() === this.relationshipFieldIntersectTypeUid.toLowerCase())[0];

                let typeUidSide = this.assetTypeUid;

                if (!typeUidSide && relationship["SideOfRelationship"]) {
                    if (relationship["SideOfRelationship"] === "Object") {
                        typeUidSide = relationship.Object.Uid;
                    }
                    else {
                        typeUidSide = relationship.Subject.Uid;
                    }
                }

                this.relationshipFieldIntersectCardinality =
                    relationship.Object.Uid === this.assetTypeUid
                        ? relationship.Subject.Cardinality : relationship.Object.Cardinality;

                this.relationshipFieldName = this.relationshipFieldIntersectTypeUid + "|" + (relationship.Object.Uid === typeUidSide ? relationship.Subject.Uid : relationship.Object.Uid);
                this.condition.relationshipFieldName = this.relationshipFieldName;
            }

            this.condition.friendlyFieldName = type.FriendlyName;
            this.condition.fieldType = this.getTypeForCondition(this.condition);
            this.condition.type = this.currentField;
            this.uiCurrentOperatorsList = this.getOperators(this.condition);
            this.uiFilterLabel = this.condition.getFilterLabel();

            if (type.Type.Score && !this.condition.value) {
                this.condition.value = "poor";
            }
        }
        else {
            if (this.condition.field === SystemFields.OwnedByFieldCode) {
                this.condition.friendlyFieldName = "Owned By";
                this.condition.fieldType = null;
                this.uiCurrentOperatorsList = this.getOperators(this.condition);
                this.uiFilterLabel = this.condition.getFilterLabel();
                this.hasSelectAllCheckbox = true;
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
        if ((event && event.type !== "load") && !this.condition.isNew) {
            this.isSelectingValue = true;
            this.startUpdateDynamicWidths();
        }
        this.condition.isNew = false;

        if (this.uiCurrentOperatorsList) {
            if (this.condition.operator) {
                this.currentOperator = this.condition.operator;
            }
            else {
                this.currentOperator = (this.uiCurrentOperatorsList[0] as SelectItem).value;
            }
            this.updateOperatorData();
        }

        this.uiTooltipValue = this.condition.getTooltipValue();
        this.updateFocus();
    }

    loadListLazy(event: LazyLoadEvent) {

        var params = { skip: event.first, take: event.rows, filter: event.globalFilter ?? "" };
        var type = this.getFieldType(this.condition);
        if (type.Type) {
            if (this.condition.fieldType === "Lookup") {
                if (this.condition.field === "[Level]") {
                    this.loadLookupValuesForLevelNames();
                }
                else if (this.isComplexField && this.complexFieldDefinition.FieldType === 'OwnershipLookup') {
                    this.loadLookupValuesForComplexFields(params);
                }
                else {
                    this.loadLookupValues(params);
                }
            }
            if (this.condition.fieldType === "Tag") {
                this.loadTagValues();
            }
            if (this.condition.fieldType === "Relationship") {
                this.loadRelationshipValues(params);
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
        if (this.condition.fieldType === "Path") {
            var dataType = typeof this.condition.value;

            //Convert string to chips
            if (dataType === "string" && (this.currentOperator !== "StartsWith" && this.currentOperator !== "EndsWith")) {
                var values = (this.condition.value as string).split(",");
                this.condition.value = values;
            }
        }

        this.condition.operator = this.currentOperator;
        this.updateOperatorData();

        this.updateAllAnyData();
        this.removePositionStyling();
    }

    private updateOperatorData() {
        this.currentInputType = this.fieldInputType();
        this.doesNeedValue = this.needsValue();
    }

    loadLookupValues(params: any) {
        if (this.currentField.Values && this.currentField.Values.length > 0 && !params.filter) {
            var subData = this.currentField.Values.slice(+params.skip, +params.skip + +params.take);
            if (!subData.some((x) => !x)) {
                return;
            }
        }

        if (this.lazyLoadSubscription) {
            this.lazyLoadSubscription.unsubscribe();
        }
        this.isLookupValuesLoading = true;
        var fieldTypeUid = this.currentField.AssetTypeUid;

        if (!fieldTypeUid) {
            fieldTypeUid = "00000000-0000-0000-0000-000000000000";
        }

        this.lazyLoadSubscription = this.fieldsService.getLookupValues(fieldTypeUid, this.currentField.Name.trim(), params)
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

                this.isLookupValuesLoading = false;

                this.cdRef.markForCheck();
            });
    }

    loadLookupValuesForComplexFields(params: any) {
        if (this.currentField.Values && this.currentField.Values.length > 0 && !params.filter) {
            var subData = this.currentField.Values.slice(+params.skip, +params.skip + +params.take);
            if (!subData.some((x) => !x)) {
                return;
            }
        }

        if (this.lazyLoadSubscription) {
            this.lazyLoadSubscription.unsubscribe();
        }
        this.isLookupValuesLoading = true;

        var definition = this.complexFieldDefinition;

        this.lazyLoadSubscription = this.fieldsService.getLookupValuesForComplexField(definition.AssetUid, definition.FieldApiName, this.currentField.Name.trim(), params)
            .subscribe((res) => {
                if (!this.currentField.Values || this.currentField.Values.length === 0) {
                    this.currentField.Values = Array.from({ length: res.count });
                }

                let loadedData = [];

                res.items.forEach((str) => {
                    loadedData.push({ title: str.title, value: str.value });
                });

                Array.prototype.splice.apply(this.currentField.Values, [...[params.skip, params.take], ...loadedData]);

                this.currentField.Values = [...this.currentField.Values];

                this.isLookupValuesLoading = false;

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

                var grouped = _.mapValues(_.groupBy(mapped, "value"),
                    (clist) => clist.map((item) => _.omit(item, "value")));

                var keys = Object.keys(grouped);
                keys.forEach((key) => {
                    var value = key;
                    var data = [];
                    if (grouped.hasOwnProperty(key)) {
                        data = grouped[key] as any[];
                    }
                    var name = data[0].title;
                    var groups = data.map((m: any) => m.group).join(", ");
                    var title = name + " (" + groups + ")";

                    this.currentField.Values.push({ title, value });
                });

                this.currentField.Values = this.currentField.Values.sort((a, b) => { return a.title > b.title ? 1 : 0; });

                this.isLookupValuesLoading = false;

                this.cdRef.markForCheck();
            });
        }
    }

    loadRelationshipValues(params: any) {
        if (this.currentField.Values && this.currentField.Values.length > 0 && !params.filter) {
            var subData = this.currentField.Values.slice(+params.skip, +params.skip + +params.take);
            if (!subData.some((x) => !x)) {
                return;
            }
        }

        if (this.lazyLoadSubscription) {
            this.lazyLoadSubscription.unsubscribe();
        }
        this.isLookupValuesLoading = true;

        let nameAsParam: string = this.currentField.Name;

        if (this.condition.fieldType === "Relationship") {
            nameAsParam = this.relationshipFieldName;
        }

        this.lazyLoadSubscription = this.assetService
            .getAssetsLookupValues(nameAsParam.split("|")[1], params)
            .subscribe((res) => {
                if (!this.currentField.Values || params.filter) {
                    this.currentField.Values = Array.from({ length: 0 });
                }
                let loadedData = [];

                res.forEach((str) => {
                    let label: string = (str.label as string).split(">").join(" <i class='slim-fa fa fa-chevron-right'></i> ");
                    loadedData.push({ title: label, value: str.value });
                });


                Array.prototype.splice.apply(this.currentField.Values, [...[params.skip, params.take], ...loadedData]);

                this.currentField.Values = [...this.currentField.Values];

                if (+params.take === res.length) {
                    this.currentField.Values.push(null);
                }

                this.isLookupValuesLoading = false;
                this.cdRef.markForCheck();
            });
    }

    loadLookupValuesForLevelNames() {
        if (this.lazyLoadSubscription) {
            this.lazyLoadSubscription.unsubscribe();
        }
        this.isLookupValuesLoading = true;

        this.lazyLoadSubscription = this.assetTypeService.getAssetTypeLevels(this.assetTypeUid)
            .subscribe((res) => {
                if (!this.currentField.Values || this.currentField.Values.length === 0) {
                    this.currentField.Values = Array.from({ length: res.length });
                }
                let loadedData = [];

                res.forEach((item) => {
                    loadedData.push({ title: item.Name, value: item.Level });
                });

                this.currentField.Values = [...loadedData];
                this.isLookupValuesLoading = false;

                this.cdRef.markForCheck();
            });
    }

    confirmValue() {
        this.isSelectingValue = false;
        this.condition.isConfirmed = true;
        this.condition.operator = this.currentOperator;
        this.updateOperatorData();

        if (this.condition.operator.toString() === "Between") {
            var type = this.fieldInputType();
            var temp;
            switch (type) {
                case "multi-number":
                case "multi-counter":
                    if (parseFloat(this.condition.value) > parseFloat(this.condition.value2)) {
                        temp = this.condition.value;
                        this.condition.value = this.condition.value2;
                        this.condition.value2 = temp;
                    }
                    break;
                case "multi-date":
                case "multi-date-time":
                    if (new Date(this.condition.value) > new Date(this.condition.value2)) {
                        temp = this.condition.value;
                        this.condition.value = this.condition.value2;
                        this.condition.value2 = temp;
                    }
                    break;
                default:
                    if (this.condition.value > this.condition.value2) {
                        temp = this.condition.value;
                        this.condition.value = this.condition.value2;
                        this.condition.value2 = temp;
                    }
                    break;
            }

        }

        this.uiTooltipValue = this.condition.getTooltipValue();
        this.uiFilterLabel = this.condition.getFilterLabel();

        if (this.multiInputRef) {
            this.multiInputRef.clearTextValue();
        }

        this.resetLookupValues();
        this.stopUpdateDynamicWidths();

        this.onChange.emit();
    }

    cancel() {
        this.resetLookupValues();

        if (!this.rollbackValue1 && !this.rollbackOperator) {
            if (this.condition.isDefaultFilter) {
                this.resetPersistedFilter();
                return;
            }
            this.condition.markForDeletion = true;
        }
        this.condition.operator = this.rollbackOperator;
        this.currentOperator = this.rollbackOperator;

        this.currentInputType = this.fieldInputType();

        if (this.currentInputType.indexOf("date") !== -1) {
            this.resetDateFields();
        }
        else {
            this.condition.value = this.rollbackValue1;
            this.condition.value2 = this.rollbackValue2;
        }
        this.stopUpdateDynamicWidths();

        this.isSelectingValue = false;


        if (this.multiInputRef) {
            this.multiInputRef.clearTextValue();
        }
    }

    private resetLookupValues() {
        if (this.filterTableValue.length > 0) {
            this.filterTableValue = "";
            this.currentField.Values = [];
        }
    }

    private resetDateFields() {
        if (this.rollbackValue1) {
            this.condition.value = new Date(this.rollbackValue1);
        }
        if (this.rollbackValue2) {
            this.condition.value2 = new Date(this.rollbackValue2);
        }
        else {
            this.condition.value2 = this.rollbackValue2;
        }
    }

    hasRemoveButton() {
        if (this.condition.isDefaultFilter
            || (!this.isEmpty(this.condition.value) && this.condition.operator)
            || (this.condition.isConfirmed && this.isNonValueOperator())
        ) {
            return true;
        }

        return false;
    }

    hasRemoveIcon() {
        if (!this.condition.operator || !this.condition.isConfirmed) {
            return false;
        }

        if (this.isNonValueOperator()) {
            return true;
        }

        if (!this.isEmpty(this.condition.value) && this.condition.operator) {
            return true;
        }

        return false;
    }

    remove() {
        if (this.condition.isDefaultFilter) {
            this.resetPersistedFilter();
            return;
        }
        this.condition.markForDeletion = true;
        this.onChange.emit();
    }

    private resetPersistedFilter() {
        this.currentOperator = this.uiCurrentOperatorsList[0].value;
        this.condition.operator = undefined;
        this.condition.value = undefined;
        this.condition.value2 = undefined;
        this.currentInputType = this.fieldInputType();

        this.uiTooltipValue = this.condition.getTooltipValue();
        this.uiFilterLabel = this.condition.getFilterLabel();

        this.isSelectingValue = false;
        this.onChange.emit();
        this.cdRef.markForCheck();
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
        if (this.currentOperator === "Populated" || this.currentOperator === "NotPopulated") {
            return;
        }

        if (this.condition.fieldType === "Lookup") {
            if (this.currentField.Type.Lookup.List.AllowMultipleValues !== true) {
                if (this.currentOperator.toString() === "NotEquals") {
                    this.condition.connectingOperator = "and";
                } else {
                    this.condition.connectingOperator = "or";
                }
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
            else {
                if (this.currentOperator.toString() === "NotEquals") {
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
                    else {
                        this.condition.connectingOperator = "or";
                        this.uiIsAllDisabled = true;
                        this.uiIsAnyDisabled = true;
                    }
                }
            }
        }

        if (this.condition.fieldType === "Tag" && this.condition.value) {
            if (this.currentOperator.toString() === "NotContains") {
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
                else {
                    this.condition.connectingOperator = "or";
                    this.uiIsAllDisabled = true;
                    this.uiIsAnyDisabled = true;
                }
            }
        }

        if (this.condition.fieldType === "Relationship") {
            if (this.relationshipFieldIntersectCardinality === "Many" && this.condition.value && (this.condition.value as any[]).length > 1) {
                this.uiIsAllDisabled = false;
                this.uiIsAnyDisabled = false;
            }
            else {
                if (this.relationshipFieldIntersectCardinality === "One") {
                    this.condition.connectingOperator = "or";
                }
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
        }

        if (this.condition.fieldType === "Path") {
            if (this.currentOperator.toString() === "Contains" && (this.condition.value && (this.condition.value as any[]).length > 1)) {
                this.uiIsAllDisabled = false;
                this.uiIsAnyDisabled = false;
            }
            else {
                this.condition.connectingOperator = "and";
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
        }
        if (this.condition.field === SystemFields.OwnedByFieldCode && (this.condition.value && (this.condition.value as any[]).length > 1)) {
            if (this.currentOperator.toString() === "Equals") {
                this.uiIsAllDisabled = false;
                this.uiIsAnyDisabled = false;
            }
            else {
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
        }

        if (this.condition.isRelationship) {
            if (this.relationshipFieldIntersectCardinality === "Many" && this.condition.value && (this.condition.value as any[]).length > 1) {
                this.uiIsAllDisabled = false;
                this.uiIsAnyDisabled = false;
            }
            else {
                if (this.relationshipFieldIntersectCardinality === "One") {
                    this.condition.connectingOperator = "or";
                }
                this.uiIsAllDisabled = true;
                this.uiIsAnyDisabled = true;
            }
        }
    }

    fieldInputType() {
        if (!this.currentOperator) {
            return "";
        }

        if (this.condition.field === SystemFields.OwnedByFieldCode) {
            return "lookup";
        }

        var type = this.getTypeForCondition(this.condition);
        if (type === "Counter") {
            if (this.currentOperator.toString() === "Between") {
                return "multi-counter";
            }
            return "counter";
        }

        if (type === "Number") {
            this.numberMax = this.currentField.Type.Number?.Validation?.MaximumValue ?? 2147483647;
            this.numberMin = this.currentField.Type.Number?.Validation?.MinimumValue ?? -2147483648;
        }
        if (type === "Decimal") {
            this.numberMax = this.currentField.Type.Decimal?.Validation?.MaximumValue ?? 9223372036854775807;
            this.numberMin = this.currentField.Type.Decimal?.Validation?.MinimumValue ?? -9223372036854775808;
        }
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

        if (type === "Lookup" || type === "Tag" || type === "Relationship" || this.currentField.IsRelationship) {
            if (!this.currentField.Type?.Lookup?.List?.Uid && this.currentField.Name === "Color") {
                return "color-picker";
            }

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
        if ((this.currentField.Name === SystemFields.OwnedByFieldCode && this.currentField.Values && this.currentField.Values.length > 0)
            || (this.currentField.Type && this.currentField.Type.Tag && this.currentField.Values)) {
            return false;
        }
        return true;
    }

    isSaveDisabled() {
        if (!this.doesNeedValue) {
            return false;
        }
        if (this.currentInputType) {
            const checkValue2: boolean = (this.currentInputType.indexOf("multi") !== -1 && this.currentInputType !== "multi-input");
            const checkMinMax: boolean = this.currentInputType.indexOf("number") !== -1;
            if (checkValue2 && this.isEmpty(this.condition.value2)) {
                return true;
            }
            if (this.isEmpty(this.condition.value)) {
                return true;
            }
            if (checkValue2 && checkMinMax && this.isOutsideMinMax(this.condition.value2)) {
                return true;
            }
            if (checkMinMax && this.isOutsideMinMax(this.condition.value)) {
                return true;
            }
        }
        return this.isEmpty(this.condition.value);
    }

    isEmpty(value: any): boolean {
        if (Array.isArray(value) && (value as []).length > 0) {
            return false;
        }
        if (value === null || (typeof value === "undefined") || (value as string).length === 0) {
            return true;
        }

        return false;
    }

    isOutsideMinMax(value: any): boolean {
        if (value && !Array.isArray(value)) {
            return this.isUnderMin(+value) || this.isOverMax(+value);
        }
        return null;
    }

    private isOverMax(value: number): boolean {
        return typeof this.numberMax !== "undefined" && value > +this.numberMax;
    }

    private isUnderMin(value: number): boolean {
        return typeof this.numberMin !== "undefined" && value < +this.numberMin;
    }

    @HostListener("document:click", ["$event"])
    clickOutside(event: any) {
        var target = event.target as HTMLElement;
        if (!this.elRef.nativeElement.contains(event.target)
            && !this.isInBodyElement(target)
            && this.condition.field
            && this.isSelectingValue
        ) {
            this.isSelectingValue = false;
            this.cancel();
        }
    }

    private isInBodyElement(el: HTMLElement) {
        if (el.tagName === "P-DROPDOWNITEM"
            || el.classList.contains("remove-chip")
            || el.classList.contains("p-datepicker-group-container")
        ) {
            return true;
        }
        else {
            if (!el.parentElement) {
                return false;
            }
            const datepickerEl = document.querySelector("div.p-datepicker.p-component");
            if (datepickerEl && datepickerEl.contains(el)) {
                return true;
            }

            return this.isInBodyElement(el.parentElement);
        }
    }

    @HostListener("keydown", ["$event"]) onKeydownHandler(event: KeyboardEvent) {
        let allowedTypes = ["text", "number", "lookup", "date", "date-time", "score-band", "multi-date", "multi-number"];
        if (allowedTypes.some((x) => x === this.currentInputType)) {
            if (event.keyCode === 13 && !this.isSaveDisabled()) {
                this.confirmValue();
            }

            if (event.keyCode === 27) {
                this.cancel();
            }
        }
    }


    //table extensions
    selectSingleItem(event: MouseEvent, item: SelectItem) {
        if (!this.condition.value) {
            this.condition.value = [];
        }
        let valueRef = this.condition.value as SelectItem[];
        let elIdx = valueRef.findIndex((x) => x.value === item.value);

        if (elIdx > -1) {
            valueRef.splice(elIdx, 1);
        }
        else {
            valueRef.push(item);
        }
        //update reference
        this.condition.value = [...valueRef];
        this.updateAllAnyData();
    }

    isNonValueOperator(): boolean {
        if (!this.condition.operator) {
            return false;
        }
        return this.nonValueOperators.indexOf(this.condition.operator.toString()) !== -1;
    }

    get isAssetType() {
        return this.loadIdentifier.length === 36 && !this.loadIdentifier.startsWith("RuleResults");
    }

    get isRuleResults() {
        return this.loadIdentifier.startsWith("RuleResults");
    }

    get isComplexField() {
        return this.loadIdentifier.startsWith("ComplexField");
    }

    get complexFieldDefinition(): ComplexFieldDefinition {
        let res = new ComplexFieldDefinition();
        if (this.isComplexField) {
            var data = this.loadIdentifier.replace("ComplexField", "").replace("ComplexField", "").split("|");
            res.AssetUid = data[0];
            res.FieldApiName = data[1];
            res.FieldType = data[2];
        }
        return res;
    }

    get counterPrefix() {
        return this.currentField?.Type?.Counter?.CounterPrefix;
    }

    getFieldsDropdownClass(): string {
        if (this.allFieldsDropdown.length <= 1) {
            return "ig-dropdown-hide-groups";
        }
        else {
            return "ig-dropdown-grouped";
        }
    }
}
