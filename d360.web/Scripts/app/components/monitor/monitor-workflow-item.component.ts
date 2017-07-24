import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';
import { Router } from '@angular/router';


@Component({
    selector: 'd3s-monitor-workflow-item',
    template: ` 
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <div class="tile tile-detail">
        <header>
            Workflow Items
            <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
        <p-dataTable [value]="workflowItems" [rows]="10" paginator="true" selectionMode="single" [selection]="selection" (selectionChange)="selection = $event; selectionChange.emit($event)">
            <p-column header="Step Name" field="Name"></p-column>
            <p-column header="Number of Events" field="NumberOfEvents"></p-column>
        </p-dataTable>
    </div>
</div>

<!--
            <p-column field="Name" header="Item" [sortable]="true">
                <ng-template let-item="rowData" pTemplate type="body">
                    <a (click)="openItem(item.Url)">{{item.Name}}</a>
                </ng-template>
            </p-column>  
            <p-column field="NumberOfEvents" header="Total Events" [sortable]="true"></p-column>                                      
            <p-column field="UpdatedOn" header="Updated" [sortable]="true">
                <ng-template let-col let-data="rowData" pTemplate type="body">
                    <span>{{data.UpdatedOn | date: 'shortDate'}}</span>
                </ng-template>
            </p-column>
            <p-column field="CompletedOn" header="Completed" [sortable]="true">
                <ng-template let-col let-data="rowData" pTemplate type="body">
                    <span>{{data.CompletedOn | date: 'shortDate'}}</span>
                </ng-template>
            </p-column>
-->

              `,
    providers: [WorkflowService],
})

export class MonitorWorkflowItemComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() workflowVersionID: number = 0;
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
        if (this.workflowVersionID == null || this.workflowVersionID< 1) {
            this.workflowItems = [];
            this.selection = null;
            this.selectionChange.emit(null);
            return;
        }

        this.isLoading = true;
        this.workflowService.getWorkflowVersionStepEvents(this.workflowVersionID)
            .then(r => {
                this.workflowItems = r;
                this.isLoading = false;
            });
    }

    openItem(url: string) {
        this.router.navigateByUrl(url);
    }
}