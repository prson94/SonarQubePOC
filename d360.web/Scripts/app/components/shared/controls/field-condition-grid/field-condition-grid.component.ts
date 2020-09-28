import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { FieldTypeAPIModelField } from '../../../../models/fieldtype-api.model';
import { SelectItem } from 'primeng/api';
import { FieldTypeAPIModelFieldCondition } from './field-condition-grid.models';

@Component({
    selector: 'field-condition-grid',
    templateUrl: 'field-condition-grid.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./field-condition-grid.component.less']
})
export class FieldConditionGrid implements OnInit, OnChanges {
    @Input() fields: FieldTypeAPIModelFieldCondition[] = [];


    fieldsSelect: SelectItem[] = [];

    private booleanValues = [
        { label: 'True', value: 'true' },
        { label: 'False', false: 'false' }
    ]


    private conditions: Condition[] = [];
    @ViewChild('conditionsForm', { static: true }) formGroup: NgForm;
    constructor(public cdRef: ChangeDetectorRef) {

    }

    ngOnInit() {
        this.addNewCondition();

        this.formGroup.valueChanges.subscribe(obs => {
            setInterval(() => {
                var lastCondition = this.conditions[this.conditions.length - 1];
                if (lastCondition.value != null) {
                    this.addNewCondition();
                }
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
    }


    deleteCondition(item: Condition) {
        this.conditions = this.conditions.filter(x => x != item);
        if (this.conditions.length == 0) {
            this.addNewCondition();
        }
    }


    addNewCondition() {
        this.conditions.push({ fieldApiName: '', operator: '', value: null });
    }

    getTypeForCondition(item: Condition) {
        var ft = this.getFieldType(item);
        if (!ft) return '';
        return Object.keys(ft.Type)[0];
    }

    getOperators(item: Condition) {
        return this.getFieldType(item).Operators;
    }

    getValues(item: Condition) {
        return this.getFieldType(item).Values;
    }

    getFieldType(item: Condition) {
        if (this.fields)
            return this.fields.filter(x => x.Name === item.fieldApiName)[0];

        return null;
    }
}
export class Condition {
    fieldApiName: string;
    operator: string;
    value: any;
}
