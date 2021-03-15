import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked, ViewChildren, ElementRef } from '@angular/core';
import * as _ from 'lodash';
import { FieldCondition, FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { Operator, OperatorModel } from '../../../models/operator.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { forkJoin } from 'rxjs';
import { AdvancedFilterFieldCondition, AdvancedFilterFieldConditionCollection, SystemFields } from './advanced-filtering.models';
import { GallerySwitchComponent } from '../../gallery/gallery.switch.component';
import { DatePipe } from '@angular/common';

@Component({
    selector: 'advanced-filtering',
    templateUrl: 'advanced-filtering.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./advanced-filtering.component.less'],
    providers: [FieldsObservableService, CompanySettingsService]
})
export class AdvancedFilteringComponent implements OnChanges {
    @Input() assetTypeUid: string = '';
    @Output() onChange = new EventEmitter();

    fields: FieldTypeAPIModelFieldCondition[] = null;
    operators: OperatorModel[] = [];

    conditions: AdvancedFilterFieldConditionCollection;

    visible: boolean = false;
    queryString: string = "";

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService,
        private settingsService: CompanySettingsService,
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


        this.onChange.emit(this.queryString);
    }

    private initializeData() {
        this.visible = false;
        forkJoin(
            this.settingsService.getOperators(),
            this.fieldsService.getFieldsV2(this.assetTypeUid, null, null)
        ).subscribe((response) => {
            this.operators = response[0];
            let res = response[1];
            var tempFields: FieldTypeAPIModelFieldCondition[] = [];
            res.forEach(f => {
                if (FieldTypeHelper.isFieldForOperatorAdvancedFilters(f.Type)) {
                    tempFields.push(f as FieldTypeAPIModelFieldCondition);
                }
            });

            SystemFields.GetSystemFieldDefinition().forEach((f) => {
                tempFields.push(f as FieldTypeAPIModelFieldCondition);
            });

            tempFields.forEach(f => {
                f.Operators = [];
                this.operators.forEach(op => {
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
                        f.Operators = f.Operators.;
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
        this.queryString = this.conditions.getQueryStringValue();
        this.cdRef.markForCheck();
    }


}
