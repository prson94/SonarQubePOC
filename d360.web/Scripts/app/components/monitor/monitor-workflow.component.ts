import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';
import { Router } from '@angular/router';


@Component({
    selector: 'd3s-monitor-workflow',
    template: ` 
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <div class="tile tile-detail">
        <header>
            Workflows
            <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
        <p-dataTable #dt [globalFilter]="gb" [value]="workflowItems" selectionMode="single" [rows]="15" [rowsPerPageOptions]="[10,15,25]" [paginator]="true" [pageLinks]="3" [selection]="selection" (selectionChange)="selection = $event; selectionChange.emit($event)">
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>            
            <p-column field="Name" header="Name" sortable="true" [filter]="!showSimpleFilter"></p-column>
            <p-column field="Version" header="Version" sortable="true" [filter]="!showSimpleFilter"></p-column>
            <p-column field="UpdatedOn" header="Updated On" sortable="true" [filter]="!showSimpleFilter">
                <ng-template pTemplate="body" let-item="rowData">
                    {{item.UpdatedOn | date:'shortDate'}}
                </ng-template>
            </p-column>
            <p-column field="UpdatedBy" header="Updated By" sortable="true" [filter]="!showSimpleFilter"></p-column>
            <p-column field="ObjectType" header="Type"sortable="true" [filter]="!showSimpleFilter">
                <ng-template pTemplate="body" let-item="rowData">
                    <a (click)="openItem(item.NgUrl)">{{item.ObjectType}}</a>
                </ng-template>
            </p-column>
        </p-dataTable>
    </div>
</div>

<!--
 <p-column field="Name" header="Name" sortable="true" [filter]="!showSimpleFilter"></p-column>
            <p-column field="Version" header="Version" sortable="true" [filter]="!showSimpleFilter"></p-column>
            <p-column field="ObjectTypeName" header="Object Type" sortable="true" [filter]="!showSimpleFilter"></p-column>
            <p-column field="ObjectName" header="Object Name"sortable="true" [filter]="!showSimpleFilter">
                <ng-template pTemplate="body" let-item="rowData">
                    <a (click)="openItem(item.Url)">{{item.ObjectName}}</a>
                </ng-template>
            </p-column>
            <p-column field="StartedOn" header="Started On" sortable="true" [filter]="!showSimpleFilter">
                <ng-template pTemplate="body" let-item="rowData">
                    {{item.StartedOn | date:'shortDate'}}
                </ng-template>
            </p-column>
            <p-column field="CompletedOn" header="Completed On" sortable="true" [filter]="!showSimpleFilter">
                <ng-template pTemplate="body" let-item="rowData">
                    {{item.CompletedOn | date:'shortDate'}}
                </ng-template>
            </p-column>
-->

              `,
    providers: [WorkflowService],
})

export class MonitorWorkflowComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() workflowTypes: any[];
    @Input() selection: any;
    @Output() selectionChange = new EventEmitter();

    workflowItems: any[];

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
        if (this.workflowTypes == null || this.workflowTypes.length < 1) {
            this.workflowItems = [];
            this.selection = null;
            this.selectionChange.emit(null);
            return;
        }

        this.isLoading = true;
        let typeList = "";
        this.workflowTypes.forEach(s => typeList += s.toString() + ',');
        this.workflowService.getWorkflowsByTypeList(typeList)
            .then(r => {
                this.workflowItems = r;
                this.isLoading = false;
            });
    }

    openItem(url: string) {
        this.router.navigateByUrl(url);
    }
}
