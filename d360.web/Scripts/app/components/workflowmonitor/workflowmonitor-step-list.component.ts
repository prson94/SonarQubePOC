import { Component, OnInit, OnChanges, Input, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowItemStep, WorkflowActivityType, StepType } from '../../models/workflow.model';
import { WorkflowHelpers } from '../../static/workflow-helpers';

@Component({
    selector: 'd3s-workflow-monitor-step-list',
    template: ` 
    <header>
        Steps
        <d3s-tile-actions [hasExport]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (exportClick)="export()"></d3s-tile-actions>
    </header>
    <div style="padding: 5px">
        Select a step to view details:
    </div>
    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
    <p-dataTable #dt [globalFilter]="gb" [value]="itemSteps" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selection" (onRowClick)="selectionChange.emit($event.data)">                                                                        
        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
        <p-column field="Name" header="Step Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>    
        <p-column field="StepType" header="Step Type" [sortable]="true" [filter]="!showSimpleFilter" [style]="{'width': '95px'}">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                {{helper.stepTypeName(item.StepType)}}
            </ng-template>                                                        
        </p-column>  
        <p-column field="Complete" header="Complete" [sortable]="true" [filter]="!showSimpleFilter" [style]="{'width': '90px'}">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                <span>
                    <i *ngIf="item.Complete == true" class="fa fa-check enabled" title="True"></i>
                    <i *ngIf="item.Complete == false" class="fa fa-times disabled" title="False"></i>
                </span>
            </ng-template>                                                        
        </p-column> 
        <p-column field="ActivityType" header="Activity Type" [sortable]="true" [filter]="!showSimpleFilter">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                {{helper.activityTypeName(item.ActivityType)}}
            </ng-template>                                                        
        </p-column>  
        <p-column *ngIf="showAssigneeColumn" field="Assignee" header="Assignee" [sortable]="true" [filter]="!showSimpleFilter"></p-column>    
        <p-column field="StartedOn" header="Date Started" [sortable]="true" [filter]="!showSimpleFilter">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                {{item.StartedOn | date:'shortDate'}}
            </ng-template>                                                        
        </p-column>  
        <p-column field="CompletedOn" header="Date Completed" [sortable]="true" [filter]="!showSimpleFilter">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                {{item.CompletedOn | date:'shortDate'}}
            </ng-template>  
        </p-column>  
    </p-dataTable> 
`,
    providers: [WorkflowService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() itemId: number;
    @Output() selectionChange = new EventEmitter();

    helper = WorkflowHelpers;
    itemSteps: WorkflowItemStep[] = [];
    selection: WorkflowItemStep = null;

    showAssigneeColumn = false;

    constructor(private workflowService: WorkflowService, private ref: ChangeDetectorRef) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['itemId'] != null && (changes['itemId'].isFirstChange || (changes['itemId'].currentValue != changes['itemId'].previousValue))) {
            this.load();
        }
    }

    ngOnDestroy() {
    }

    load() {
        this.itemSteps = null;
        if (this.itemId != null)
            this.workflowService.getWorkflowItemSteps(this.itemId)
                .then(r => {
                    this.itemSteps = r;
                    if (this.itemSteps != null) {
                        this.showAssigneeColumn = (this.itemSteps.find(i => i.ActivityType == WorkflowActivityType.Form) != null)
                        this.selection = this.itemSteps[0];
                        this.selectionChange.emit(this.selection);
                    }
                    this.ref.markForCheck();
                    //console.log('loaded', this.itemSteps);
                });
    }

    private export() {
        if (this.itemId != null && this.itemId > 0)
            this.workflowService.exportItemSteps(this.itemId);
    }
}