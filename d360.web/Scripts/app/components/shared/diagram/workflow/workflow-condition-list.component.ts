import { Component, NgZone, Output, EventEmitter, Input, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { Column, Header, MenuItem } from 'primeng/primeng';

@Component({
    selector: 'd3s-workflow-condition-list',
    template: `
    <header>
                <div class="row" *ngIf="!isLoading && conditions.length > 1">
                        <input type="radio" name="isAll"
                               [(ngModel)]="satisfyAll"
                               (ngModelChange)="connectorChange.emit($event)"
                               [value]="true"
                               style="width: 15px;" />
                        <div class="FieldName">
                            Satisfy all&nbsp;&nbsp;&nbsp;&nbsp;
                        </div>
                        <input type="radio" name="isAll"
                               [(ngModel)]="satisfyAll"
                               (ngModelChange)="connectorChange.emit($event)"
                               [value]="false"
                               style="width: 15px;" />
                        <div class="FieldName">
                            Satisfy any
                        </div>
                    </div>
        <d3s-tile-actions hideTooltip="true" [hasAdd]="!readonly" (addClick)="addClick.emit()"></d3s-tile-actions>
<div *ngIf="conditions.length <= 1">&nbsp;</div>
</header>
    <p-table #dt [value]="filteredConditions" selectionMode="single" [metaKeySelection]="true" [pageLinks]="3" [paginator]="true" [rows]="5" [rowsPerPageOptions]="defaultPagingOptions">
        <ng-template pTemplate="header">
            <tr>
                <th>Field Name</th>
                <th>Operator</th>
                <th>Value</th>
                <th *ngIf="!readonly"></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item>
            <tr [pSelectableRow]="item">
                <td>{{item['@FieldName']}}</td>
                <td>
                    {{(item['@Operator'] == 'C') ? 'value changed' : item['@Operator']}}
                </td>
                <td>
                    {{(item['@Operator'] == 'C') ? '[any value change]' : (item['@ValueLabel'] == null ? item['@Value'] : item['@ValueLabel']) }}
                </td>
                <td *ngIf="!readonly">
                    <div class="RowTools">
                        <a style="cursor:pointer;" (click)="removeClick.emit(item)"><i class="fa fa-trash"></i></a>
                    </div>
                </td>
            </tr>
        </ng-template>
        <ng-template pTemplate="summary">
            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
        </ng-template>
    </p-table>
`
})

export class WorkflowConditionListComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() conditions: any[] = [];
    @Input() selection;
    @Input() readonly = false;
    @Input() satisfyAll: boolean = true;
    @Output() selectionChange = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() removeClick = new EventEmitter();
    @Output() editClick = new EventEmitter();
    @Output() connectorChange = new EventEmitter();

    private filteredConditions: any[] = [];
    

    ngOnInit() {
        this.satisfyAll = this.conditions.every(c => c["@Connector"] == "AND");
    }

    ngOnChanges(changes: SimpleChanges) {
        this.filteredConditions = this.conditions.filter(c => c['@ContextualFieldID'] == null || (c['@ContextualFieldID'] != 'IssueObject' && c['@ContextualFieldID'] != 'IssueObjectID'));
    }

    constructor() {
        super();
    }
}