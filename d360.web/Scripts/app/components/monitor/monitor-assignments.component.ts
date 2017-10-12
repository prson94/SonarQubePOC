import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';
import { Router } from '@angular/router';


@Component({
    selector: 'd3s-monitor-assignments',
    template: ` 
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <div class="tile tile-detail">
        <header>
            My Assignments
            <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
        <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="15" [rowsPerPageOptions]="[10,15,25]" [paginator]="true" [pageLinks]="3" [(selection)]="selection">
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer> 
            <p-column header="Workflow Name" field="WorkflowName" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>  
            <p-column header="Item" field="ObjectName" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
            <p-column header="Step" field="StepName" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
            <p-column header="Started On" field="StartedOn" [sortable]="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
                <ng-template let-item="rowData" type="body" pTemplate>
                    {{item.StartedOn | date: 'short'}}
                </ng-template>
            </p-column>
            <p-column header="">
                <ng-template let-item="rowData" type="body" pTemplate>
                    <a style="cursor:pointer;" (click)="openItem(item)" title="Complete Form"><i class="fa fa-check-square-o"></i></a>                                                        
                </ng-template>
            </p-column>
        </p-dataTable>
    </div>
</div>
              `,
    providers: [WorkflowService],
})

export class MonitorAssignmentsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() workflowTypes: any[];
    @Input() objectType: string;
    @Input() objectId: number;

    items: any[];
    selection: any;

    constructor(protected workflowService: WorkflowService, protected router: Router) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    private load() {
        if (this.workflowTypes == null) {
            this.items = [];
            return;
        }

        let typeString = "";
        typeString = this.workflowTypes.join(', ');
        this.workflowService.getWorkflowOpenActions(typeString)
            .then(r => {
                this.items = r;
            })
            .then(() => {
                //filter at object level if applicable
                if (this.objectType != null && !this.objectType.endsWith('Type')) {
                    this.items = this.items.filter(i => i.Object == this.objectType && i.ObjectID == this.objectId);
                }
            });
       
    }

    openItem(item: any) {
        if (item == null)
            return;
        this.router.navigateByUrl(`/workflow/form/${item.TypeID}/${item.ItemStepID}/${item.ItemID}`);
    }
}
