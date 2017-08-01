import { Component, NgZone, OnInit, Output, EventEmitter, Input, OnChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    WorkflowEventRegistration,
    WorkflowObjectType,
    WorkflowChangeType,
    ChangeTypeInfo,
    EventCondition,
    WorkflowListItem,
    WorkflowDiagramModel,
    WorkflowDiagramNode,
    NodeModel,
    WorkflowActivityType,
    WorkflowTaskProcedure,
    EmailTaskRecipientType
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { Column, Header, Editor } from 'primeng/primeng';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
    selector: 'd3s-workflow-history',
    providers: [WorkflowService],
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<header>&nbsp;
    <d3s-tile-actions [hasExport]="true" (exportClick)="export()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
</header>
<input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
<p-dataTable *ngIf="!isLoading" #dt [globalFilter]="gb" [value]="history" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,15]" [paginator]="true" [pageLinks]="3" scrollable="true" scrollWidth="560px">
    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>    
    <p-column header="Object" field="Name" [style]="{'width' : '120px'}" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
        <ng-template pTemplate="body" let-item="rowData">
            <a>{{item.Name}}</a>
        </ng-template>
    </p-column>    
    <p-column header="Status" field="Status" [style]="{'width' : '80px'}" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">></p-column>
    <p-column header="Started On" field="StartedOn" [style]="{'width' : '80px'}" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
        <ng-template pTemplate="body" let-item="rowData">
            {{item.StartedOn | date:'shortDate'}}
        </ng-template>
    </p-column>
    <p-column header="Completed On" field="CompletedOn" [style]="{'width' : '80px'}" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">
        <ng-template pTemplate="body" let-item="rowData">
            {{item.CompletedOn | date:'shortDate'}}
        </ng-template>
    </p-column>
    <p-column header="Started By" field="StartedBy" [style]="{'width' : '120px'}" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains"></p-column>
    <p-column header="Comment" field="Comment" [style]="{'width' : '200px'}" sortable="true" [filter]="!showSimpleFilter" filterMatchMode="contains">></p-column>
</p-dataTable>

`
})

export class WorkflowHistoryComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() versionStepId: number;
    @Input() versionStepTransitionFromId: number;
    @Input() versionStepTransitionToId: number;

    history: any[];

    constructor( private workflowService: WorkflowService) {
        super();
        
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.history = [];
        if (this.versionStepId != null) {
            this.isLoading = true;
            this.workflowService.getWorkflowVersionStepHistory(this.versionStepId)
                .then(r => {
                    this.history = r;
                    this.isLoading = false;
                });
        }
    }

    export() {
        this.workflowService.exportVersionStepHistory(this.versionStepId);
    }
}