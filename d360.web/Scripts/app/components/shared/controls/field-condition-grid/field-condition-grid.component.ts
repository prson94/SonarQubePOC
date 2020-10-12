import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit, OnDestroy, Output, EventEmitter } from '@angular/core';
import { NgForm } from '@angular/forms';
import { SelectItem } from 'primeng/api';
import { FieldTypeAPIModelFieldCondition } from './field-condition-grid.models';
import { Operator } from '../../../../models/operator.model';

@Component({
    selector: 'field-condition-grid',
    templateUrl: 'field-condition-grid.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./field-condition-grid.component.less']
})
export class FieldConditionGrid implements OnInit, OnChanges, OnDestroy {
    @Input() fields: FieldTypeAPIModelFieldCondition[] = [];
    @Input() conditions: Condition[] = [];

    @Output() onChange = new EventEmitter();

    fieldsSelect: SelectItem[] = [];


    private disabledValuesOperators = [Operator.NotPopulated, Operator.Populated];
    private dataCheck: any;

    @ViewChild('conditionsForm', { static: true }) formGroup: NgForm;
    constructor(public cdRef: ChangeDetectorRef) {

    }

    ngOnDestroy() {
        if (this.dataCheck) {
            clearInterval(this.dataCheck);
        }
    }

    ngOnInit() {
        if (!this.conditions)
            this.conditions = [];

        this.tryAddNewCondition();

        this.dataCheck = setInterval(() => {

            this.conditions.forEach(cond => {
                if (this.disabledValuesOperators.some(x => x === +cond.operator))
                    cond.disabled = true;
                else cond.disabled = false;
            });
            this.cdRef.markForCheck();

        }, 100);

        this.formGroup.valueChanges.subscribe(obs => {
            setInterval(() => {
                this.tryAddNewCondition();
            });
        });
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
        this.cdRef.detectChanges();
    }


    deleteCondition(item: Condition) {
        this.conditions = this.conditions.filter(x => x != item);
        if (this.conditions.length == 0) {
            this.tryAddNewCondition();
        }
    }


    tryAddNewCondition() {
        var lastCondition = this.conditions[this.conditions.length - 1];
        var availableFields = this.getAvailableFields(null);
        if (!lastCondition || (lastCondition.operator != null && lastCondition.operator != '')) {
            if (availableFields.length > 0)
                this.conditions.push({ field: '', operator: '', value: null, disabled: false, value2: null });
        }

        this.onChange.emit({ event: 'Value changed', value: this.conditions });

    }
    onFieldChange($event, condition: Condition) {
        condition.operator = '';
        condition.value = '';
        this.tryAddNewCondition();
    }

    onConditionChange(event, condition: Condition) {
        if (this.disabledValuesOperators.some(x => x === +event.value)) {
            condition.disabled = true;
        }
        else {
            condition.disabled = false;
        }
        condition.value = '';
        this.cdRef.markForCheck();
    }

    getTypeForCondition(item: Condition) {
        var ft = this.getFieldType(item);
        if (!ft) return '';
        return Object.keys(ft.Type)[0];
    }

    getOperators(item: Condition) {
        var ft = this.getFieldType(item);
        if (ft && ft.Operators && !item.operator) {
            item.operator = ft.Operators[0].value;
            this.tryAddNewCondition();
        }
        return ft ? ft.Operators : [];
    }

    getValues(item: Condition) {
        if (item.disabled || !this.getFieldType(item)) return [];
        return this.getFieldType(item).Values;
    }

    getFieldType(item: Condition) {
        if (this.fields)
            return this.fields.filter(x => x.Name === item.field)[0];

        return null;
    }

    getAvailableFields(item: Condition) {
        var allowedFields = this.fieldsSelect.filter(x => !this.conditions.some(c => c.field === x.value));

        if (item && item.field) {
            var field = this.fields.filter(x => x.Name == item.field)[0];
            allowedFields.push({ value: field.Name, label: field.FriendlyName });
            allowedFields = allowedFields.sort((a, b) => a.label > b.label ? 1 : -1);
        }
        return allowedFields;
    }
}
export class Condition {
    field: string;
    operator: string;
    value: any;
    value2: any;

    disabled: boolean = true;
}
