import { Component, NgZone, Output, EventEmitter, Input} from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { Column, Header, MenuItem } from 'primeng/primeng';

@Component({
    selector: 'd3s-workflow-condition-list',
    template: `
    <header>
        &nbsp;
        <d3s-tile-actions [hasAdd]="!readonly" (addClick)="addClick.emit()"></d3s-tile-actions>
    </header>
    <p-dataTable [value]="conditions" selectionMode="single" [selection]="selection" (selectionChange)="selection = $event; selectionChange.emit(selection)">
        <p-column field="@FieldName" header="Field Name"></p-column>
        <p-column field="@Operator" header="Operator"></p-column>
        <p-column field="@Value" header="Value"></p-column>
        <p-column *ngIf="!readonly">
            <template let-item="rowData" pTemplate type="body">
                <div class="RowTools">
                    <a style="cursor:pointer;" (click)="removeClick.emit(item)"><i class="fa fa-trash"></i></a>
                    <!--<a style="cursor:pointer;" (click)="editClick.emit(item)"><i class="fa fa-pencil"></i></a>-->
                </div>
            </template>
        </p-column>
    </p-dataTable>
`
})

export class WorkflowConditionListComponent extends BaseComponent {
    @Input() conditions: any[] = [];
    @Input() selection;
    @Input() readonly = false;
    @Output() selectionChange = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() removeClick = new EventEmitter();
    @Output() editClick = new EventEmitter();

    constructor() {
        super();
    }
}