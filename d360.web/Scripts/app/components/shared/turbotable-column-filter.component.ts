import { Component, NgModule, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Table } from 'primeng/table';

@Component({
    selector: 'd3s-column-filter',
    template: `
        <ng-container [ngSwitch]="datatype">
            <input *ngSwitchCase="'text'" type="text" pInputText (input)="dt.filter($event.target.value, field, filterMatchMode)" class="ui-column-filter ui-inputtext">
            <input *ngSwitchCase="'date'" type="text" pInputText (input)="dt.filter($event.target.value, field, filterMatchMode)" class="ui-column-filter ui-inputtext">
        </ng-container>
    `
})
export class D3SColumnFilter {
    @Input() datatype: string = 'text';
    @Input() field: string;
    @Input() filterMatchMode = 'contains';

    constructor(public dt: Table) {
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
        CommonModule,
    ]
})
export class D3SColumnFilterModule { }
