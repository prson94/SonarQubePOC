import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked } from '@angular/core';
import { NgForm, FormGroup, FormBuilder, FormControl, Validators } from '@angular/forms';
import { SelectItem } from 'primeng/api';
import { Operator } from '../../../../models/operator.model';
import { FieldTypeAPIModelFieldCondition, FieldCondition } from '../../../../models/field-condition-grid.models';
import { Condition } from '../../../../models/metrics.model';
import * as _ from 'lodash';

@Component({
    selector: 'field-condition-grid',
    templateUrl: 'field-condition-grid.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./field-condition-grid.component.less']
})
export class FieldConditionGrid implements OnChanges, OnDestroy {
    @Input() formGroup: FormGroup;
    @Input() fields: FieldTypeAPIModelFieldCondition[] = [];
    @Input() conditions: FieldCondition[] = [];
    @Input() singleSelectMode: boolean = false;
    @Input() required: boolean = false;
    @Input() conditionPrefix: string = '';

    @Output() onChange = new EventEmitter();

    fieldsSelect: SelectItem[] = [];
    visible: boolean = false;
    operatorRequiredValue: boolean = false;
    Operators = Operator;
    private disabledValuesOperators = [Operator.NotPopulated, Operator.Populated, Operator.IsFalse, Operator.IsTrue];

    constructor(public cdRef: ChangeDetectorRef, private fb: FormBuilder) {
        this.formGroup = fb.group({});
    }

    delayedClearUnusedConditions = _.debounce(() => {
        this.clearUnusedFormControls();
    }, 200);

    ngOnDestroy() {
        this.resetFormControls();
        this.delayedClearUnusedConditions();
        this.conditions = null;
    }

    private initializeData() {
        this.visible = false;
        if (!this.conditions) {
            this.conditions = [];
            this.visible = true;
        }
        else {
            this.conditions.forEach(cond => {
                if (!cond.hash) {
                    cond.hash = this.randstr('id');
                }
                this.createFormControl(cond);
            });
            this.cdRef.markForCheck();
            this.visible = true;
        }
        this.tryAddNewCondition();

        this.formGroup.valueChanges.subscribe(obs => {
            setTimeout(() => {
                if (!this.conditions) return;
                this.conditions.forEach(cond => {
                    if (this.disabledValuesOperators.some(x => (x === +cond.operator || Operator[x] == <any>cond.operator))) {
                        cond.disabled = true;
                    }
                    else
                        cond.disabled = false;

                    let formControl1 = this.formGroup.get(this.conditionPrefix + 'value_1_' + cond.hash);
                    if (formControl1) {
                        cond.disabled ? formControl1.disable({ emitEvent: false }) : formControl1.enable({ emitEvent: false });
                    }
                    let formControl2 = this.formGroup.get(this.conditionPrefix + 'value_2_' + cond.hash);
                    if (formControl2) {
                        cond.disabled ? formControl2.disable({ emitEvent: false }) : formControl2.enable({ emitEvent: false });
                    }

                });
                this.tryAddNewCondition();
                this.cdRef.markForCheck();
            });
        });
        this.cdRef.markForCheck();
    }


    ngOnChanges(changes: SimpleChanges) {
        if (changes && changes.fields && changes.fields.currentValue != changes.fields.previousValue) {
            this.fieldsSelect = [];

            this.fields.forEach(f => {
                this.fieldsSelect.push({
                    value: `${f.AssetTypeUid}.${f.Name}`,
                    label: f.FriendlyName
                });
            });
        }

        if (changes && changes.conditions && changes.conditions.currentValue != changes.conditions.previousValue) {
            this.initializeData();
        }
        this.cdRef.detectChanges();
    }


    deleteCondition(item: FieldCondition) {
        let tempArr: FieldCondition[] = [];
        while (this.conditions.length > 0)
            tempArr.push(this.conditions.pop());

        var arr = tempArr.filter(x => x.field != item.field);
        while (arr.length > 0) {
            this.conditions.push(arr.pop());
        }

        if (this.conditions.length == 0) {
            this.tryAddNewCondition();
        } else {
            this.onChange.emit({ event: 'Value changed', value: this.conditions });
        }

        window.setTimeout(() => {
            this.removeFormControl(null,item.hash);
        }, 100);
    }

    tryAddNewCondition() {
        var lastCondition = this.conditions[this.conditions.length - 1];
        var availableFields = this.getAvailableFields(null);
        if (this.singleSelectMode) {
            if (this.conditions.length == 0) {
                var hash = this.randstr('id');
                let cond = { assetTypeUid: '', field: '', operator: null, value: null, disabled: false, value2: null, isValid: true, hash: hash };
                this.createFormControl(cond);
                this.conditions.push(cond);
            }
        } else {
            if (!lastCondition || (lastCondition.operator != null && lastCondition.operator)) {
                if (availableFields.length > 0) {
                    var hash = this.randstr('id');
                    let cond = { assetTypeUid: '', field: '', operator: null, value: null, disabled: false, value2: null, isValid: true, hash: hash };
                    this.createFormControl(cond);
                    this.conditions.push(cond);
                }
            }
        }

        window.setTimeout(() => { this.delayedClearUnusedConditions(); }, 100);
        this.onChange.emit({ event: 'Value changed', value: this.conditions });

    }

    clearUnusedFormControls() {
        if (this.conditions) {
            Object.keys(this.formGroup.controls).forEach((control) => {
                if (control.startsWith(this.conditionPrefix)) {
                    if (control.indexOf(this.conditionPrefix + 'option_') !== -1
                        || control.indexOf(this.conditionPrefix + 'condition_') !== -1
                        || control.indexOf(this.conditionPrefix + 'value_1_') !== -1 ||
                        control.indexOf(this.conditionPrefix + 'value_2_') !== -1) {

                        let shouldDelete = true;
                        this.conditions.forEach(x => {
                            if (control.indexOf(x.hash) !== -1) {
                                shouldDelete = false;
                             }
                        });

                        if (shouldDelete) {
                            this.removeFormControl(control);
                        }
                    }
                }
            });
        }
        this.cdRef.markForCheck(); 
    }

    resetFormControls() {
        if (this.conditions) {
            Object.keys(this.formGroup.controls).forEach(control => {
                if (control.startsWith(this.conditionPrefix)) {
                    if (control.indexOf(this.conditionPrefix + 'option_') !== -1
                        || control.indexOf(this.conditionPrefix + 'condition_') !== -1
                        || control.indexOf(this.conditionPrefix + 'value_1_') !== -1 ||
                        control.indexOf(this.conditionPrefix + 'value_2_') !== -1) {
                        this.removeFormControl(control);
                    }
                }
            });
        }
        this.cdRef.markForCheck();
    }

    private createFormControl(hash: FieldCondition) {
        let type = this.getTypeForCondition(hash);
        this.formGroup.addControl(this.conditionPrefix + 'option_' + hash.hash, new FormControl(''));
        this.formGroup.addControl(this.conditionPrefix + 'condition_' + hash.hash, new FormControl(''));
        if (type == "date" || type == "date") {
            this.formGroup.addControl(this.conditionPrefix + 'value_1_' + hash.hash, new FormControl(new Date(hash.value), [Validators.maxLength(250)]));
            this.formGroup.addControl(this.conditionPrefix + 'value_2_' + hash.hash, new FormControl(new Date(hash.value2), [Validators.maxLength(250)]));
        } else {
            this.formGroup.addControl(this.conditionPrefix + 'value_1_' + hash.hash, new FormControl('', [Validators.maxLength(250)]));
            this.formGroup.addControl(this.conditionPrefix + 'value_2_' + hash.hash, new FormControl('', [Validators.maxLength(250)]));
        }
    }

    private removeFormControl(name: string, hash?: string) {
        if (name) {
            this.formGroup.removeControl(name);
            this.formGroup.removeControl(name);
            this.formGroup.removeControl(name);
            this.formGroup.removeControl(name);
        }
        if (hash) {
            this.formGroup.removeControl(this.conditionPrefix + 'option_' + hash);
            this.formGroup.removeControl(this.conditionPrefix + 'condition_' + hash);
            this.formGroup.removeControl(this.conditionPrefix + 'value_1_' + hash);
            this.formGroup.removeControl(this.conditionPrefix + 'value_2_' + hash);
        }
    }

    onFieldChange($event, condition: FieldCondition) {
        condition.operator = null;
        condition.value = '';
        this.tryAddNewCondition();
    }

    onConditionChange(event, condition: FieldCondition) {
        if (this.disabledValuesOperators.some(x => (this.Operators[x] === event.value || this.Operators[x] === event.value))) {
            condition.disabled = true;
        }
        else {
            condition.disabled = false;
        }
        condition.value = '';
        condition.value2 = '';
        let formControl1 = this.formGroup.get(this.conditionPrefix + 'value_1_' + condition.hash);
        if (formControl1) {
            condition.disabled ? formControl1.disable() : formControl1.enable();
            formControl1.reset();
        }
        let formControl2 = this.formGroup.get(this.conditionPrefix + 'value_2_' + condition.hash);
        if (formControl2) {
            condition.disabled ? formControl2.disable() : formControl2.enable();
            formControl2.reset();
        }
        setTimeout(() => {
            this.cdRef.markForCheck();
        })
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
            this.tryAddNewCondition();
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

    getAvailableFields(item: FieldCondition) {

        var allowedFields = this.fieldsSelect.filter(x => !this.conditions.some(c => c.field === x.value));
        if (item && item.field) {
            let fieldDataArray = item.field.split('.');
            var field = this.fields.filter(x => x.AssetTypeUid == fieldDataArray[0] && x.Name == fieldDataArray[1])[0];
            if (field) {
                allowedFields.push({ value: `${field.AssetTypeUid}.${field.Name}`, label: field.FriendlyName });
                allowedFields = allowedFields.sort((a, b) => a.label > b.label ? 1 : -1);
            }
        }
        return allowedFields;
    }

    public get condition_form() {
        return this.formGroup;
    }

    private randstr(prefix) {
        return Math.random().toString(36).replace('0.', prefix || '');
    }

    private getUniqueId(prefix: string, item: Condition): string {
        return prefix + item['hash'];
    }
}
