import { Component, NgZone, OnInit, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowListItem, ChangeTypeInfo } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';

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
        [globalFilterFields]="globalFilterFields">
        <ng-template pTemplate="header">
            <tr>
                <th *ngFor="let col of columns" [pSortableColumn]="col.datafield">
                    {{col.text}}
                    <d3s-sortIcon [field]="col.datafield"></d3s-sortIcon>
                </th>
                <th style="width: 215px"></th>
            </tr>
            <tr [hidden]="showSimpleFilter">
                <th *ngFor="let col of columns">
                    <d3s-column-filter  [field]="col.datafield"></d3s-column-filter>
                </th>
                <th></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item >
            <tr (dblclick)="onEditClick.emit({ ID: item.ID, isClone: false })" [pSelectableRow]="item">
                <td *ngFor="let col of columns" [ngSwitch]="col.type">
                    <span *ngSwitchCase="'text'">{{item[col.datafield]}}</span>
                    <span *ngSwitchCase="'date'">{{item[col.datafield] | date:'shortDate'}}</span>
                     <span *ngSwitchCase="'State'">
                        <i *ngIf="item[col.datafield] == 1" class="fa fa-check enabled" title="True"></i>
                        <i *ngIf="item[col.datafield] == 4" class="fa fa-times disabled" title="False"></i>
                    </span>
                </td>
                <td>
                    <div class="RowTools">
                        <a style="cursor:pointer;" (click)="onEditClick.emit({ID:item.ID,isClone:false})"><i class="fa fa-pencil"></i></a> 
                        <a style="cursor:pointer;" (click)="cloneWorkflow(item.ID)"><i class="fa fa-copy"></i></a> 
                        <a style="cursor:pointer;" (click)="onDeleteClick.emit(item.ID)"><i class="fa fa-trash-o"></i></a>    
                        <a style="cursor:pointer;" (click)="onViewClick.emit(item.ID)"><i class="fa fa-eye"></i></a>    
                        <a style="cursor:pointer;" (click)="navigate(item.ID)"><i class="fa fa-usb"></i></a>                                      
                    </div>
                </td>
            </tr>
        </ng-template>
        <ng-template pTemplate="summary">
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
    public selection: WorkflowListItem;

    private changeTypes: ChangeTypeInfo[] = [];

    private columns: any[] = [
        { datafield: 'Name', text: 'Name', type: 'text' },
        { datafield: 'TypeName', text: 'Type Name', type: 'text' },
        { datafield: 'Type', text: 'Type', type: 'text' },
        { datafield: 'ChangeTypeName', text: 'Change Type', type: 'text' },
        { datafield: 'State', text: 'Active', type: 'State' },
        { datafield: 'UpdatedOn', text: 'Updated On', type: 'date' },
        { datafield: 'UpdatedBy', text: 'Updated By', type: 'text' },
        { datafield: 'Published', text: 'Status', type: 'text' },
    ];

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(
        private workflowService: WorkflowService,
        protected router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    cloneWorkflow(id) {

        this.isLoading = true;
        this.workflowService.cloneWorkflowDiagramModel(id)
            .subscribe(id => {
                this.isLoading = false;
                this.onEditClick.emit({ ID: id, isClone: true });
            })
    }
    load() {
        this.isLoading = true;

        this.workflowService.getChangeTypes()
            .pipe(
                map(r => this.changeTypes = r),
                map(() =>
                    this.workflowService.getAdminTypes()
                        .subscribe(r => {
                            this.items = r;
                            if (this.items.length > 0) {
                                this.selection = this.items[0];
                            }
                            this.items.forEach(i => {
                                i.ChangeTypeName = this.changeTypes.find(c => c.ID == i.ChangeType).Description;
                            });
                        })),
                map(() => this.isLoading = false))
            .subscribe();
    }

    navigate(id: string) {
        this.router.navigateByUrl(`/monitor/type/${id}?tab=monitor`);
    }
}