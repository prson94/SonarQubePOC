import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked, ViewChildren, ElementRef } from '@angular/core';
import * as _ from 'lodash';
import { FieldCondition, FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { Operator, OperatorModel } from '../../../models/operator.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { forkJoin } from 'rxjs';
import { AdvancedFilterFieldCondition } from './advanced-filtering.models';

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

    conditions: AdvancedFilterFieldCondition[] = [];

    visible: boolean = false;

    @ViewChild("dropdownRef", { static: false }) dropdownRef: ElementRef;

    constructor(public cdRef: ChangeDetectorRef,
        private elRef: ElementRef,
        private fieldsService: FieldsObservableService,
        private settingsService: CompanySettingsService) {
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
                if (FieldTypeHelper.isFieldForOperator(f.Type)) {
                    tempFields.push(f as FieldTypeAPIModelFieldCondition);
                }
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
                });


            });

            this.fields = tempFields;
            this.cdRef.markForCheck();
            this.visible = true;
        })
        this.cdRef.markForCheck();

        this.conditions.push(new AdvancedFilterFieldCondition());
    }


    ngOnChanges(changes: SimpleChanges) {

        if (changes && changes.assetTypeUid && changes.assetTypeUid.currentValue != changes.assetTypeUid.previousValue) {
            this.initializeData();
        }
        this.cdRef.detectChanges();
    }

    getQuery() {
        let queries: string[] = [];
        this.conditions.filter(x => x.field).forEach((cond) => {
            let fieldName: string = cond.field;
            let operation: string = this.getOperatorString(cond.operator);
            let value: string = this.getValue(cond.operator, cond.value);
            queries.push(`(${fieldName} ${operation} ${value})`);
        });
        console.log(queries.join(" and "));
    }

    getOperatorString(o: Operator): string {
        if (!o) {
            return "ne null";
        }

        switch (o) {
            default:
                return "";
        }
    }

    getValue(o: Operator, val: string): string {
        if (!val) {
            return "";
        }
    }
}
