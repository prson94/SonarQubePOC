import { Component, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';

@Component({
    selector: 'field-condition-grid',
    templateUrl: 'field-condition-grid.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./field-condition-grid.component.less']
})
export class FieldConditionGrid implements OnInit {
    private fields = [
        { label: 'Text Field', value: 'api_text', type: 'Text' },
        { label: 'Boolean Field', value: 'api_boolean', type: 'Boolean' },
        { label: 'Number Field', value: 'api_number', type: 'Number' },
        { label: 'List Field', value: 'api_list', type: 'Lookup' },
        { label: 'Decimal Field', value: 'api_decimal', type: 'Decimal' },
        { label: 'Date Field', value: 'api_date', type: 'Date' },
        { label: 'Date Time Field', value: 'api_date_time', type: 'DateTime' }
    ];

    private operators = [
        { label: 'Is', value: 'Is' },
        { label: 'Is not', value: 'Is not' },
        { label: 'In', value: 'In' },
        { label: 'Not In', value: 'Not In' },
        { label: 'Contains', value: 'Does not contain' }
    ];


    private booleanValues = [
        { label: 'True', value: 'true' },
        { label: 'False', false: 'false' }
    ]


    private conditions: Condition[] = [];

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

    @ViewChild('conditionsForm', { static: true }) formGroup: NgForm;

    constructor(public cdRef: ChangeDetectorRef) {

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
        return this.fields.filter(x => x.value === item.fieldApiName)[0].type;
    }
}
export class Condition {
    fieldApiName: string;
    operator: string;
    value: any;
}