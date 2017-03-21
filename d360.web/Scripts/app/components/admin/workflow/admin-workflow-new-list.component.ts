import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { WorkflowTypeItem } from '../../../models/workflow.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

@Component({
    selector: 'd3s-admin-workflow-new-list',
    providers: [WorkflowService],
    template: `

<div>
    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
    <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selection" >                                                        
    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>        
    <p-column field="TypeName" header="Type Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>  
    <p-column field="CreatedOn" header="Created On" [sortable]="true" [filter]="!showSimpleFilter" >
        <template let-item="rowData" pTemplate type="body">
            <span>{{item.CreatedOn | date:'shortDate'}}</span>
        </template>
    </p-column> 
    <p-column field="UpdatedOn" header="Updated On" [sortable]="true" [filter]="!showSimpleFilter">
        <template let-item="rowData" pTemplate type="body">
            <span>{{item.UpdatedOn | date:'shortDate'}}</span>
        </template>
    </p-column> 
    <p-column [style]="{width:'40px'}">
        <template let-item="rowData" pTemplate type="body">
            <div class="RowTools">
                <a style="cursor:pointer;" (click)="onViewClick.emit(item.ID)"><i class="fa fa-eye"></i></a>                                        
            </div>
        </template>
    </p-column>                                                      
    </p-dataTable>      
</div>
`
})

export class AdminWorkflowNewListComponent extends BaseComponent implements OnInit {
    @Output() onViewClick = new EventEmitter();

    private items: WorkflowTypeItem[] = [];
    private selection: WorkflowTypeItem;

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.workflowService.getTypes().then(r => {
            this.items = r;
            console.log(r);
        });

    }
}