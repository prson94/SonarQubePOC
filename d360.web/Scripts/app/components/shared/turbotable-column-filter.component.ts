import { Component, NgModule, Input, Output } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Table } from 'primeng/table';
import { EventEmitter } from '@angular/core';

@Component({
    selector: 'd3s-column-filter',
    template: `
        <ng-container [ngSwitch]="datatype">
            <input *ngSwitchCase="'text'" type="text" pInputText (input)="dt.filter($event.target.value, field, filterMatchMode);onChange($event.target.value);" class="ui-column-filter ui-inputtext">
            <input *ngSwitchCase="'number'" type="number" pInputText (input)="dt.filter($event.target.value, field, 'equals');onChange($event.target.value);" class="ui-column-filter ui-inputtext">
            <input *ngSwitchCase="'date'" type="text" pInputText (input)="dt.filter($event.target.value, field, filterMatchMode)" class="ui-column-filter ui-inputtext">
        </ng-container>
    `
})
export class D3SColumnFilter {
    @Input() datatype: string = 'text';
    @Input() field: string;
    @Input() filterMatchMode = 'contains';

    @Output() onChangeCallback = new EventEmitter();

    constructor(public dt: Table) {
    }

    onChange(event) {
        if (this.onChangeCallback) {
            this.onChangeCallback.emit({ value: event, prop: this.field });
        }
    }

}
@NgModule({
    declarations: [
        D3SColumnFilter,
    ],
    exports: [
        D3SColumnFilter,
    ]
    , imports: [
        CommonModule
    ]
})
export class D3SColumnFilterModule { }
