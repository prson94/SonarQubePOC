import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked } from '@angular/core';
import { NgForm, FormGroup, FormBuilder, FormControl, Validators } from '@angular/forms';
import { SelectItem } from 'primeng/api';
import * as _ from 'lodash';
import { FieldCondition, FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { Operator, OperatorModel } from '../../../models/operator.model';
import { Condition } from '../../../models/metrics.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'advanced-filtering',
    templateUrl: 'advanced-filtering.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./advanced-filtering.component.less'],
    providers: [FieldsObservableService, CompanySettingsService]
})
export class AdvancedFilteringComponent implements OnChanges, OnDestroy {
    @Input() assetTypeUid: string = '';
    @Output() onChange = new EventEmitter();

    fields: FieldTypeAPIModelFieldCondition[] = null;
    operators: OperatorModel[] = [];

    visible: boolean = false;

    constructor(public cdRef: ChangeDetectorRef,
        private fieldsService: FieldsObservableService,
        private settingsService: CompanySettingsService,) {
    }

    ngOnDestroy() {
        //
    }

    private initializeData() {
        this.visible = false;

        forkJoin(
            this.settingsService.getOperators(),
            this.fieldsService.getFieldsV2(this.assetTypeUid, null, null)
        ).subscribe((response) => {
            this.operators = response[0];
            let res = response[1];
            var tempFields = [];
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
    }


    ngOnChanges(changes: SimpleChanges) {

        if (changes && changes.assetTypeUid && changes.assetTypeUid.currentValue != changes.assetTypeUid.previousValue) {
            this.initializeData();
        }
        this.cdRef.detectChanges();
    }


    getTypeForCondition(item: FieldCondition) {
        var ft = this.getFieldType(item);
        if (!ft) return '';
        return Object.keys(ft.Type)[0];
    }

    getOperators(item: FieldCondition) {
        var ft = this.getFieldType(item);
        if (ft && ft.Operators && !item.operator) {
            if (ft.Operators.length > 0) {
                item.operator = ft.Operators[0].value;
            }
        }
        return ft ? ft.Operators : [];
    }

    getValues(item: FieldCondition) {
        if (item.disabled || !this.getFieldType(item)) return [];
        return this.getFieldType(item).Values;
    }

    getFieldType(item: FieldCondition) {
        if (this.fields) {
            let fieldDataArray = item.field.split('.');
            return this.fields.filter(x => x.AssetTypeUid === fieldDataArray[0] && x.Name === fieldDataArray[1])[0];
        }

        return null;
    }
}
