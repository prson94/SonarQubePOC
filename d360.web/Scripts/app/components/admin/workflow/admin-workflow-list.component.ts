import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { TableColumn } from '../../../models/turbotable.model';
import { WorkflowListItem, ChangeTypeInfo } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { Router } from '@angular/router';

@Component({
    selector: 'd3s-admin-workflow-list',
    providers: [WorkflowService],
    template: `

<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <header>
        Workflow Types
        <d3s-tile-actions hasAdd="true" (addClick)="onAddClick.emit()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
    </header>

    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
    <p-table #dt [value]="items" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" [(selection)]="selection" 
        [globalFilterFields]="filterFields">
        <ng-template pTemplate="header">
            <tr>
                <th *ngFor="let col of columns" [pSortableColumn]="col.sortable ? col.field : null">
                    {{col.header}}
                    <d3s-sortIcon *ngIf="col.sortable" [field]="col.field"></d3s-sortIcon>
                </th>
                <th style="width: 215px"></th>
            </tr>
            <tr [hidden]="showSimpleFilter">
                <th *ngFor="let col of columns">
                    <d3s-column-filter *ngIf="col.filterable" [field]="col.field" [datatype]="col.datatype"></d3s-column-filter>
                </th>
                <th></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item >
            <tr (dblclick)="onEditClick.emit({ ID: item.ID, isClone: false })" [pSelectableRow]="item">
                <td *ngFor="let col of columns" [ngSwitch]="col.datatype">
                    <span *ngSwitchCase="'text'">{{item[col.field]}}</span>
                    <span *ngSwitchCase="'date'">{{item[col.field] | date:'shortDate'}}</span>
                </td>
                <td>
                    <div class="RowTools">
                        <a style="cursor:pointer;" (click)="onEditClick.emit({ID:item.ID,isClone:false})"><i class="fa fa-pencil"></i></a> 
                        <a style="cursor:pointer;" (click)="cloneWorkflow(item.ID)"><i class="fa fa-copy"></i></a> 
                        <a style="cursor:pointer;" (click)="onDeleteClick.emit(item.ID)"><i class="fa fa-trash-o"></i></a>    
                        <a style="cursor:pointer;" (click)="onViewClick.emit(item.ID)"><i class="fa fa-eye"></i></a>    
                        <a style="cursor:pointer;" (click)="navigate(item.ID)"><i class="fa fa-television"></i></a>                                      
                    </div>
                </td>
            </tr>
        </ng-template>
        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords" ></d3s-grid-paging-info>
        </ng-template>
    </p-table>
</div>
`
})

export class AdminWorkflowListComponent extends BaseComponent implements OnInit {
    @Output() onViewClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();
    @Output() onEditClick = new EventEmitter();
    @Output() onAddClick = new EventEmitter();

    private items: WorkflowListItem[] = [];
    private selection: WorkflowListItem;

    private changeTypes: ChangeTypeInfo[] = [];

    private columns: TableColumn[] = [
        new TableColumn({ field: 'Name', header: 'Name', sortable: true, filterable: true }),
        new TableColumn({ field: 'TypeName', header: 'Type Name', sortable: true, filterable: true }),
        new TableColumn({ field: 'Type', header: 'Type', sortable: true, filterable: true }),
        new TableColumn({ field: 'ChangeTypeName', header: 'Change Type', sortable: true, filterable: true }),
        new TableColumn({ field: 'UpdatedOn', header: 'Updated On', sortable: true, filterable: true, datatype: 'date' }),
        new TableColumn({ field: 'UpdatedBy', header: 'Updated By', sortable: true, filterable: true }),
        new TableColumn({ field: 'Published', header: 'Status', sortable: true, filterable: true }),
    ];

    get filterFields(): string[] {
        return this.columns.filter(c => c.filterable).map(c => c.field);
    }

    constructor(private workflowService: WorkflowService, protected router: Router) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    cloneWorkflow(id) {
      
        this.isLoading = true;
        this.workflowService.cloneWorkflowDiagramModel(id)
            .then(id => {
                this.isLoading = false;
                this.onEditClick.emit({ ID: id, isClone: true });
            })
    }
    load() {
        this.isLoading = true;

        this.workflowService.getChangeTypes()
            .then(r => this.changeTypes = r)
            .then(() => this.workflowService.getAdminTypes())
            .then(r => {
                this.items = r
                this.items.forEach(i => {
                    i.ChangeTypeName = this.changeTypes.find(c => c.ID == i.ChangeType).Description;
                });
            })
            .then(() => this.isLoading = false);
    }

    navigate(id: string) {
        this.router.navigateByUrl(`/monitor/type/${id}`);
    }
}