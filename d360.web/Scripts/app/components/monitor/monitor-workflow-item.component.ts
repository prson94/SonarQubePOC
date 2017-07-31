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