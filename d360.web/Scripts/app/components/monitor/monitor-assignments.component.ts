import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';
import { Router } from '@angular/router';


@Component({
    selector: 'd3s-monitor-assignments',
    template: ` 
<div class="tile tile-detail">
    <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>
            My Assignments
            <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
        <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['WorkflowName','ObjectName','StepName','StartedOn']" [pageLinks]="3" [paginator]="true" [rows]="15" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selection">
            <ng-template pTemplate="header">
                <tr>
                    <th [pSortableColumn]="'WorkflowName'">
                        Workflow Name
                        <d3s-sortIcon [field]="'WorkflowName'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'ObjectName'">
                        Item
                        <d3s-sortIcon [field]="'ObjectName'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'StepName'">
                        Step
                        <d3s-sortIcon [field]="'StepName'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'StartedOn'">
                        Started On
                        <d3s-sortIcon [field]="'StartedOn'"></d3s-sortIcon>
                    </th>
                    <th></th>
                </tr>
                <tr [hidden]="showSimpleFilter">
                    <th><d3s-column-filter [field]="'WorkflowName'" [datatype]="'text'"></d3s-column-filter></th>
                    <th><d3s-column-filter [field]="'ObjectName'" [datatype]="'text'"></d3s-column-filter></th>
                    <th><d3s-column-filter [field]="'StepName'" [datatype]="'text'"></d3s-column-filter></th>
                    <th><d3s-column-filter [field]="'StartedOn'" [datatype]="'text'"></d3s-column-filter></th>
                    <th></th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-item>
                <tr [pSelectableRow]="item">
                    <td>
                            <a *ngIf="item.Deleted == false" style="cursor:pointer;" (click)="openItem(item)" title="Complete Form">{{item.WorkflowName}}</a>
                            <span *ngIf="item.Deleted == true">{{item.WorkflowName}}</span>
                    </td>
                    <td>{{item.ObjectName}}</td>
                    <td>{{item.StepName}}</td>
                    <td>
                            {{item.StartedOn | date: 'short'}}
                    </td>
                    <td>
                            <a *ngIf="item.Deleted == false" style="cursor:pointer;" (click)="openItem(item)" title="Complete Form"><i class="fa fa-check-square-o"></i></a>
                    </td>
                </tr>
            </ng-template>
            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
            </ng-template>
        </p-table>

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
