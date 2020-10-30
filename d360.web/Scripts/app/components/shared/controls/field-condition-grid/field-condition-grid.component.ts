import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter, AfterViewChecked } from '@angular/core';
import { NgForm, FormGroup, FormBuilder, FormControl } from '@angular/forms';
import { SelectItem } from 'primeng/api';
import { Operator } from '../../../../models/operator.model';
import { FieldTypeAPIModelFieldCondition, FieldCondition } from '../../../../models/field-condition-grid.models';
import { settings } from 'cluster';

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

    @Output() onChange = new EventEmitter();

    fieldsSelect: SelectItem[] = [];
    visible: boolean = false;

    private disabledValuesOperators = [Operator.NotPopulated, Operator.Populated];

    constructor(public cdRef: ChangeDetectorRef, private fb: FormBuilder) {
        this.formGroup = fb.group({});
    }

    ngOnDestroy() {
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
                this.formGroup.addControl('option_' + cond.hash, new FormControl(''));
                this.formGroup.addControl('condition_' + cond.hash, new FormControl(''));
                this.formGroup.addControl('value_1_' + cond.hash, new FormControl(''));
                this.formGroup.addControl('value_2_' + cond.hash, new FormControl(''));
            });
            this.cdRef.markForCheck();
            this.visible = true;
        }
        this.tryAddNewCondition();

        this.formGroup.valueChanges.subscribe(obs => {
            setTimeout(() => {
                if (!this.conditions) return;

                this.conditions.forEach(cond => {
                    if (this.disabledValuesOperators.some(x => x === +cond.operator))
                        cond.disabled = true;
                    else
                        cond.disabled = false;

                    cond.isValid = false;

                    if (cond.disabled === true) {
                        if (cond.field && +cond.operator > 0) {
                            cond.isValid = true;
                        }
                    }
                    else {
                        if (cond.field && +cond.operator > 0 && cond.value) {
                            cond.isValid = true;
                        }
                    }
                    if (!cond.field)
                        cond.isValid = true;

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
                    value: f.Name,
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
        }
    }

    tryAddNewCondition() {
        var lastCondition = this.conditions[this.conditions.length - 1];
        var availableFields = this.getAvailableFields(null);
        if (this.singleSelectMode) {
            if (this.conditions.length == 0) {
                var hash = this.randstr('id');
                this.conditions.push({ field: '', operator: null, value: null, disabled: false, value2: null, isValid: true, hash: hash });
                this.formGroup.addControl('option_' + hash, new FormControl(''));
                this.formGroup.addControl('condition_' + hash, new FormControl(''));
                this.formGroup.addControl('value_1_' + hash, new FormControl(''));
                this.formGroup.addControl('value_2_' + hash, new FormControl(''));
            }
        } else {
            if (!lastCondition || (lastCondition.operator != null && lastCondition.operator)) {
                if (availableFields.length > 0) {
                    var hash = this.randstr('id');
                    this.conditions.push({ field: '', operator: null, value: null, disabled: false, value2: null, isValid: true, hash: hash });
                    this.formGroup.addControl('option_' + hash, new FormControl(''));
                    this.formGroup.addControl('condition_' + hash, new FormControl(''));
                    this.formGroup.addControl('value_1_' + hash, new FormControl(''));
                    this.formGroup.addControl('value_2_' + hash, new FormControl(''));
                }
            }
        }

        this.onChange.emit({ event: 'Value changed', value: this.conditions });

    }
    onFieldChange($event, condition: FieldCondition) {
        condition.operator = null;
        condition.value = '';
        this.tryAddNewCondition();
    }

    onConditionChange(event, condition: FieldCondition) {
        if (this.disabledValuesOperators.some(x => x === +event.value)) {
            condition.disabled = true;
        }
        else {
            condition.disabled = false;
        }
        condition.value = '';
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
        if (this.fields)
            return this.fields.filter(x => x.Name === item.field)[0];

        return null;
    }

    getAvailableFields(item: FieldCondition) {
        var allowedFields = this.fieldsSelect.filter(x => !this.conditions.some(c => c.field === x.value));
        if (item && item.field) {
            var field = this.fields.filter(x => x.Name == item.field)[0];
            allowedFields.push({ value: field.Name, label: field.FriendlyName });
            allowedFields = allowedFields.sort((a, b) => a.label > b.label ? 1 : -1);
        }
        return allowedFields;
    }

    public get condition_form() {
        return this.formGroup;
    }

    private randstr(prefix) {
        return Math.random().toString(36).replace('0.', prefix || '');
    }
}
