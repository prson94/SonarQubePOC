import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { WorkflowListItem, ChangeTypeInfo } from '../../../models/workflow.model';
import { Column, Header } from 'primeng/primeng';
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
    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
    <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" [(selection)]="selection" (onRowDblclick)="onEditClick.emit($event.data.ID)">                                                        
    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>        
    <p-column field="TypeName" header="Type Name" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>  
    <p-column field="Type" header="Type" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column> 
    <p-column field="ChangeTypeName" header="Change Type" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>  
    <p-column field="UpdatedOn" header="Updated On" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
        <ng-template let-item="rowData" pTemplate type="body">
            <span>{{item.UpdatedOn | date:'shortDate'}}</span>
        </ng-template>
    </p-column> 
    <p-column field="UpdatedBy" header="Updated By" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
  <p-column field="Published" header="Status" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column> 
  <p-column [style]="{width:'200px'}">
        <ng-template let-item="rowData" pTemplate type="body">
            <div class="RowTools">
                <a style="cursor:pointer;" (click)="onEditClick.emit(item.ID)"><i class="fa fa-pencil"></i></a>    
                <a style="cursor:pointer;" (click)="onDeleteClick.emit(item.ID)"><i class="fa fa-trash-o"></i></a>    
                <a style="cursor:pointer;" (click)="onViewClick.emit(item.ID)"><i class="fa fa-eye"></i></a>    
                <a style="cursor:pointer;" (click)="navigate(item.ID)"><i class="fa fa-television"></i></a>                                      
            </div>
        </ng-template>
    </p-column>                                                      
    </p-dataTable>      
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

    constructor(private workflowService: WorkflowService, protected router: Router) {
        super();
    }

    ngOnInit() {
        this.load();
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