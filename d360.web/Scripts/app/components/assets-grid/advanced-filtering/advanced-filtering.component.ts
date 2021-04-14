import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked, ViewChildren, ElementRef, HostBinding } from "@angular/core";
import * as _ from "lodash";
import { OperatorModel } from "../../../models/operator.model";
import { FieldsObservableService } from "../../../services/fieldsObservable.service";
import { CompanySettingsService } from "../../../services/settings.service";
import { FieldTypeHelper } from "../../../models/fieldtype-api.model";
import { forkJoin } from "rxjs";
import { AdvancedFilterFieldCondition, AdvancedFilterFieldConditionCollection, FieldTypeAPIModelFieldCondition, Filters, SystemFields } from "./advanced-filtering.models";
import { DatePipe } from "@angular/common";
import { AllocationService } from "../../../services/allocations.service";
import { ScoreTypeAllocation } from "../../../models/metrics.model";
import { RelationshipsService } from "../../../services/relationships.service";
import { RelationshipType } from "../../../models/relationship.model";

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
                this.conditions.filters = this.conditions.filters.filter((x) => x.isDefaultFilter === true);
                this.onItemChange();
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
        private datePipe: DatePipe) {
        this.conditions = new AdvancedFilterFieldConditionCollection();
        this.conditions.filters = [];
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
            let res = response[1];
            this.allocations = response[2];
            this.relationshipTypes = response[3];

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

                    if (isDefaultFilter === true && !loadedFilters.some((x) => x.field === field.Name)) {
                        var defaultFilter = new AdvancedFilterFieldCondition(this.datePipe);
                        defaultFilter.field = field.Name;
                        defaultFilter.isDefaultFilter = true;
                        this.conditions.filters.push(defaultFilter);
                    }
                }
            });

            loadedFilters.forEach((f) => {
                this.conditions.filters.push(f);
            });

            this.conditions.filters.push(new AdvancedFilterFieldCondition(this.datePipe));

            this.cdRef.markForCheck();
            this.visible = true;

            setInterval(() => {
                this.customDoCheck();
            }, 50);
        });


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

    private loadFilters(): AdvancedFilterFieldCondition[] {
        var prefilters: any[] = [];
        let loadedFilters: AdvancedFilterFieldCondition[] = [];

        (prefilters as any[]).forEach((f) => {
            var filter = f as AdvancedFilterFieldCondition;
            var newfilter = new AdvancedFilterFieldCondition(this.datePipe);
            newfilter.connectingOperator = filter.connectingOperator;
            newfilter.field = filter.field;
            newfilter.fieldType = filter.fieldType;
            newfilter.friendlyFieldName = filter.friendlyFieldName;
            newfilter.isRelationship = filter.isRelationship;
            newfilter.markForDeletion = filter.markForDeletion;
            newfilter.operator = filter.operator;
            newfilter.type = filter.type;
            newfilter.value = filter.value;
            if (newfilter.type.Type.Date || filter.type.Type.DateTime) {
                newfilter.value = new Date(filter.value);
            }
            newfilter.value2 = filter.value2;
            loadedFilters.push(newfilter);
        });

        return loadedFilters;
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
