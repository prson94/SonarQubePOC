import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked, ViewChildren, ElementRef, HostBinding } from "@angular/core";
import * as _ from "lodash";
import { OperatorModel } from "../../../models/operator.model";
import { FieldsObservableService } from "../../../services/fieldsObservable.service";
import { CompanySettingsService } from "../../../services/settings.service";
import { FieldTypeAPIModelField, FieldTypeHelper } from "../../../models/fieldtype-api.model";
import { forkJoin, Observable } from "rxjs";
import { AdvancedFilterFieldCondition, AdvancedFilterFieldConditionCollection, FieldTypeAPIModelFieldCondition, Filters, SystemFields } from "./advanced-filtering.models";
import { DatePipe } from "@angular/common";
import { AllocationService } from "../../../services/allocations.service";
import { ScoreTypeAllocation } from "../../../models/metrics.model";
import { RelationshipsService } from "../../../services/relationships.service";
import { RelationshipType } from "../../../models/relationship.model";
import { Router } from "@angular/router";

@Component({
    selector: "advanced-filtering",
    templateUrl: "advanced-filtering.component.html",
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./advanced-filtering.component.less"],
    providers: [FieldsObservableService, CompanySettingsService, AllocationService, RelationshipsService]
})
export class AdvancedFilteringComponent implements OnChanges {
    @Input() assetTypeUid: string = "";
    @Output() onChange = new EventEmitter();
    @Output() onLoad = new EventEmitter();

    allocations: ScoreTypeAllocation[] = [];
    relationshipTypes: RelationshipType[] = [];

    filters: Filters;
    emittedFilters: string;

    fields: FieldTypeAPIModelFieldCondition[] = null;
    operators: OperatorModel[] = [];

    conditions: AdvancedFilterFieldConditionCollection;


    visible: boolean = false;

    filterMenu = [
        {
            title: "Clear Filters",
            callback: () => {
                this.conditions.filters = [];
                this.clearFiltersStorage();
                this.fields.forEach((field) => {
                    if (field.Type) {
                        var key = Object.keys(field.Type)[0];
                        var isDefaultFilter = false;
                        if (Object.keys(field.Type).some((x) => x === key)) {
                            isDefaultFilter = field.Type[key]["IsPrimaryFilter"];
                        }

                        if (isDefaultFilter === true) {
                            var defaultFilter = new AdvancedFilterFieldCondition(this.datePipe);
                            defaultFilter.field = field.Name;
                            defaultFilter.isDefaultFilter = true;
                            defaultFilter.isNew = true;
                            this.conditions.filters.push(defaultFilter);
                        }
                    }
                });
                this.onItemChange();
                this.cdRef.markForCheck();
            }
        },
        {
            isSeparator: true
        },
        {
            title: "Match All",
            hasCheckbox: true,
            isChecked: true,
            callback: () => {
                this.conditions.connector = " and ";
                this.filterMenu[3].isChecked = false;
                this.onItemChange();
                this.cdRef.markForCheck();
            }
        },
        {
            title: "Match Any",
            hasCheckbox: true,
            callback: () => {
                this.filterMenu[2].isChecked = false;

                this.conditions.connector = " or ";
                this.onItemChange();
                this.cdRef.markForCheck();
            }
        }
    ]
    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;
    @HostBinding("class") class = "advanced-filtering-component";

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService,
        private settingsService: CompanySettingsService,
        private allocationService: AllocationService,
        private relationshipService: RelationshipsService,
        private datePipe: DatePipe,
        private router: Router
    ) {
        this.conditions = new AdvancedFilterFieldConditionCollection();
        this.conditions.filters = [];

        this.router.routeReuseStrategy.shouldReuseRoute = function () {
            return false;
        };
    }

    customDoCheck() {
        var allHaveField = this.conditions.filters.filter((x) => x.field).length === this.conditions.filters.length;
        if (allHaveField) {
            this.conditions.filters.push(new AdvancedFilterFieldCondition(this.datePipe));
        }

        this.conditions.filters = this.conditions.filters.filter((x) => x.markForDeletion !== true);
        this.cdRef.markForCheck();
    }

    onItemChange() {
        this.getQuery();

        var currentFilters = JSON.stringify(this.filters);

        if (currentFilters !== this.emittedFilters) {
            this.onChange.emit(this.filters);
            this.saveFilters();
            this.emittedFilters = JSON.stringify(this.filters);
        }
    }

    private initializeData() {
        this.visible = false;
        forkJoin(
            this.settingsService.getOperators(true),
            this.fieldsService.getFieldsV2(this.assetTypeUid, null, null),
            this.allocationService.getAllocationsByAssetTypeUid(this.assetTypeUid),
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid)
        ).subscribe((response) => {
            this.operators = response[0];
            let res = response[1] as FieldTypeAPIModelField[];
            this.allocations = response[2];
            this.relationshipTypes = response[3];

            if (res.some((f) => f.Type.ComputedRelationshipField)) {
                try {
                    //load field types for Field from relationship
                    this.loadFieldFromRelationshipData(res);
                }
                catch (ex) {
                    this.processLoadedData(res);
                }
            }
            else {
                this.processLoadedData(res);
            }


            this.onLoad.emit();
        }
        );


    }


    private loadFieldFromRelationshipData(res: FieldTypeAPIModelField[]) {
        try {
            let toLoad: any[] = [];
            let obsArr: Observable<FieldTypeAPIModelField[]>[] = [];

            res.filter((f) => f.Type.ComputedRelationshipField).forEach((f) => {
                var intersectTypeUid = f.Type.ComputedRelationshipField.IntersectTypeUid;
                var intersect = this.relationshipTypes.filter((f) => f.Uid === intersectTypeUid);
                if (intersect) {
                    var intersectType = intersect[0];
                    var assetTypeUid = intersectType.Object.Uid === this.assetTypeUid ? intersectType.Subject.Uid : intersectType.Object.Uid;

                    var fieldName = f.Type.ComputedRelationshipField.FieldTypeName;
                    toLoad.push({ origField: f.Name, uid: assetTypeUid, field: fieldName, persistInFilters: f.Type.ComputedRelationshipField.IsPrimaryFilter });
                    obsArr.push(this.fieldsService.getFieldsV2(assetTypeUid, "", "", fieldName));
                }
            });

            forkJoin(obsArr).subscribe((results) => {
                results.forEach((f) => {
                    var refField = f[0];
                    var idx = toLoad.findIndex((tl) => tl.uid === refField["AssetTypeUid"] && tl.field === refField.Name);
                    if (idx !== -1) {
                        var origField = res.findIndex((rf) => rf.Name === toLoad[parseInt(idx.toString())].origField);
                        var prop = Object.keys(refField.Type)[0];
                        refField.Type[prop]["IsPrimaryFilter"] = toLoad[idx].persistInFilters;

                        res[parseInt(origField.toString())].Type = refField.Type;
                    }
                });
                this.processLoadedData(res);
            });
        }
        catch (ex) {
            console.warn(ex);
            this.processLoadedData(res);

        }
    }

    private processLoadedData(res: FieldTypeAPIModelField[]) {
        var tempFields: FieldTypeAPIModelFieldCondition[] = [];
        res.forEach((f) => {
            if (FieldTypeHelper.isFieldForOperatorAdvancedFilters(f.Type)) {
                var fModel = f as FieldTypeAPIModelFieldCondition;
                tempFields.push(fModel);
            }
        });

        SystemFields.GetSystemFieldDefinition().forEach((f) => {
            var fModel = f as FieldTypeAPIModelFieldCondition;
            fModel.IsSystemField = true;
            tempFields.push(fModel);
        });

        SystemFields.GetRelationshipDefinition(this.relationshipTypes, this.assetTypeUid).forEach((f) => {
            var fModel = f as FieldTypeAPIModelFieldCondition;
            fModel.IsSystemField = true;
            tempFields.push(fModel);
        });

        tempFields.forEach((f) => {
            f.Operators = [];

            this.operators.forEach((op) => {
                if (f.Type) {
                    var fieldType = FieldTypeHelper.getFieldType(f.Type).toLowerCase();

                    if (fieldType === "computedrelationshipfield") {
                        fieldType = "fieldfromrelationship";
                    }

                    if (op.AllowedDataTypes.some((x) => x.Name.toLowerCase() === fieldType)) {
                        f.Operators.push({ label: op.Name, value: op.ID });
                    }

                    if (FieldTypeHelper.getFieldType(f.Type) === "Boolean") {
                        f.Values = [];
                        f.Values.push({ value: "true", label: "True" });
                        f.Values.push({ value: "false", label: "False" });
                    }
                    if (FieldTypeHelper.getFieldType(f.Type) === "Date"
                        && f.Category === "System Fields") {
                        this.updateOperatorsForDateTimeSystemField(f);
                    }
                }
            });
        });

        this.fields = tempFields;

        this.cdRef.markForCheck();
        var loadedFilters = this.loadFilters();

        this.fields.forEach((field) => {
            if (field.Type) {
                var key = Object.keys(field.Type)[0];
                var isDefaultFilter = false;
                if (Object.keys(field.Type).some((x) => x === key)) {
                    isDefaultFilter = field.Type[key]["IsPrimaryFilter"];
                }
                if (isDefaultFilter === true) {
                    var existingFilter = loadedFilters.filter((f) => f !== null).find((df) => df.isDefaultFilter === true && df.field == field.Name);
                    if (existingFilter) {
                        this.conditions.filters.push(existingFilter);
                        var idx = loadedFilters.indexOf(existingFilter);
                        loadedFilters[idx] = null;
                    }
                    else {
                        var defaultFilter = new AdvancedFilterFieldCondition(this.datePipe);
                        defaultFilter.field = field.Name;
                        defaultFilter.isDefaultFilter = true;
                        this.conditions.filters.push(defaultFilter);
                    }
                }
            }
        });

        loadedFilters.filter((f) => f !== null).forEach((f) => {
            this.conditions.filters.push(f);
        });

        this.conditions.filters.push(new AdvancedFilterFieldCondition(this.datePipe));
        this.visible = true;

        this.onItemChange();

        this.cdRef.markForCheck();
        setInterval(() => {
            this.customDoCheck();
        }, 50);
    }

    private updateOperatorsForDateTimeSystemField(f: FieldTypeAPIModelFieldCondition) {
        f.Operators = f.Operators.filter((x) => x.value !== "Populated" && x.value !== "NotPopulated");
        f.Operators.forEach((item) => {
            if (item.value === "Equals") {
                item.value = "Contains";
            }
            if (item.value === "NotEquals") {
                item.value = "NotContains";
            }
        });
    }

    getLocalStorageKey() {
        return this.assetTypeUid + "_advancedFilters";
    }

    private saveFilters() {
        localStorage.setItem(this.getLocalStorageKey(), JSON.stringify(this.conditions.filters));
    }

    private clearFiltersStorage() {
        localStorage.removeItem(this.getLocalStorageKey());
    }

    private loadFilters(): AdvancedFilterFieldCondition[] {
        try {
            var prefilters: any[] = [];
            let loadedFilters: AdvancedFilterFieldCondition[] = [];

            var storageFilters = localStorage.getItem(this.getLocalStorageKey());
            prefilters = JSON.parse(storageFilters);
            (prefilters as any[]).forEach((f) => {
                var filter = f as AdvancedFilterFieldCondition;

                //do not load from storage if field got removed in meantime
                if (this.fields && filter.field) {
                    if (!this.fields.some((f) => f.Name === filter.field)) {
                        return false;
                    }
                }

                if (!filter.operator) {
                    return false;
                }

                var newfilter = new AdvancedFilterFieldCondition(this.datePipe);
                newfilter.connectingOperator = filter.connectingOperator;
                newfilter.field = filter.field;
                newfilter.fieldType = filter.fieldType;
                newfilter.friendlyFieldName = filter.friendlyFieldName;
                newfilter.isRelationship = filter.isRelationship;
                newfilter.markForDeletion = filter.markForDeletion;
                newfilter.relationshipFieldName = filter.relationshipFieldName;
                newfilter.operator = filter.operator;
                newfilter.type = filter.type;
                newfilter.isDefaultFilter = filter.isDefaultFilter;
                newfilter.value = filter.value;
                newfilter.isPreloaded = true;
                newfilter.isConfirmed = true;
                if (newfilter.type && (newfilter.type.Type.Date || filter.type.Type.DateTime)) {
                    newfilter.value = new Date(filter.value);
                }
                newfilter.value2 = filter.value2;
                loadedFilters.push(newfilter);
            });
            return loadedFilters;
        }
        catch (ex) {
            console.warn("Error parsing saved filter");
            return [];
        }
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && changes.assetTypeUid && changes.assetTypeUid.currentValue !== changes.assetTypeUid.previousValue) {
            this.initializeData();
        }
        this.cdRef.detectChanges();
    }

    getQuery() {
        this.filters = this.conditions.getFilters(this.allocations);

        this.cdRef.markForCheck();
    }
}
