import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked, ViewChildren, ElementRef } from '@angular/core';
import * as _ from 'lodash';
import { OperatorModel } from '../../../models/operator.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { forkJoin } from 'rxjs';
import { AdvancedFilterFieldCondition, AdvancedFilterFieldConditionCollection, FieldTypeAPIModelFieldCondition, Filters, SystemFields } from './advanced-filtering.models';
import { DatePipe } from '@angular/common';
import { AllocationService } from '../../../services/allocations.service';
import { ScoreTypeAllocation } from '../../../models/metrics.model';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipType } from '../../../models/relationship.model';
import { AssetTypeService } from '../../../services/asset-type.service';

@Component({
    selector: 'advanced-filtering',
    templateUrl: 'advanced-filtering.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./advanced-filtering.component.less'],
    providers: [FieldsObservableService, CompanySettingsService, AllocationService, RelationshipsService]
})
export class AdvancedFilteringComponent implements OnChanges {
    @Input() assetTypeUid: string = '';
    @Output() onChange = new EventEmitter();

    allocations: ScoreTypeAllocation[] = [];
    relationshipTypes: RelationshipType[] = [];
    filters: Filters;

    fields: FieldTypeAPIModelFieldCondition[] = null;
    operators: OperatorModel[] = [];

    conditions: AdvancedFilterFieldConditionCollection;

    visible: boolean = false;

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService,
        private settingsService: CompanySettingsService,
        private allocationService: AllocationService,
        private relationshipService: RelationshipsService,
        private datePipe: DatePipe) {
        this.conditions = new AdvancedFilterFieldConditionCollection();
        this.conditions.filters = [];
        setInterval(() => {
            this.getQuery();
            this.customDoCheck();
        }, 200);
    }

    customDoCheck() {
        var allHaveField = this.conditions.filters.filter(x => x.field).length === this.conditions.filters.length;
        if (allHaveField) {
            this.conditions.filters.push(new AdvancedFilterFieldCondition(this.datePipe));
        }

        this.conditions.filters = this.conditions.filters.filter(x => x.markForDeletion != true);


        this.onChange.emit(this.filters);
    }

    private initializeData() {
        this.visible = false;
        forkJoin(
            this.settingsService.getOperators(),
            this.fieldsService.getFieldsV2(this.assetTypeUid, null, null),
            this.allocationService.getAllocationsByAssetTypeUid(this.assetTypeUid),
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid)
        ).subscribe((response) => {
            this.operators = response[0];
            let res = response[1];
            this.allocations = response[2];
            this.relationshipTypes = response[3];

            var tempFields: FieldTypeAPIModelFieldCondition[] = [];
            res.forEach(f => {
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

            tempFields.forEach(f => {
                f.Operators = [];

                this.operators.forEach(op => {
                    if (f.Type) {
                        if (op.AllowedDataTypes.some(x => x.Name === FieldTypeHelper.getFieldType(f.Type))) {
                            f.Operators.push({ label: op.Name, value: op.ID });
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Boolean') {
                            f.Values = [];
                            f.Values.push({ value: 'true', label: 'True' });
                            f.Values.push({ value: 'false', label: 'False' });
                        }
                        if (FieldTypeHelper.getFieldType(f.Type) === 'Date'
                            && f.Category === "System Fields") {
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
                    }
                });
            });

            this.fields = tempFields;
            this.cdRef.markForCheck();
            this.visible = true;
        })
        this.cdRef.markForCheck();

        this.conditions.filters.push(new AdvancedFilterFieldCondition(this.datePipe));
    }


    ngOnChanges(changes: SimpleChanges) {
        if (changes && changes.assetTypeUid && changes.assetTypeUid.currentValue != changes.assetTypeUid.previousValue) {
            this.initializeData();
        }
        this.cdRef.detectChanges();
    }

    getQuery() {
        this.filters = this.conditions.getFilters(this.allocations);

        this.cdRef.markForCheck();
    }


}
