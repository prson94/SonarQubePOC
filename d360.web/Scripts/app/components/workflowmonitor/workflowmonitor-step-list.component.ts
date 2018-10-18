import { Component, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowItemStep, WorkflowActivityType, StepType } from '../../models/workflow.model';
import { StateService } from '../../services/state.service';

@Component({
    selector: 'd3s-workflow-monitor-step-list',
    template: ` 
    <header *ngIf="!isIssueType" style="min-height: 32px">
        Steps
        <d3s-tile-actions [hasExport]="true" (exportClick)="export()"></d3s-tile-actions>
    </header>
    <simple-accordion *ngIf="isIssueType" [active]="true" [header]="'Action'">
        <d3s-workflow-monitor-action-details [id]="objectId"></d3s-workflow-monitor-action-details>
    </simple-accordion>
    <simple-accordion *ngIf="isIssueType" [active]="true" [header]="'Steps'">
        <header>
            <d3s-tile-actions [hasExport]="true" (exportClick)="export()"></d3s-tile-actions>
        </header>
        <div style="padding: 5px">
            Select a step to view details:    
        </div>
        <d3s-workflow-monitor-step-grid [itemSteps]="itemSteps" (selectionChange)="select($event)"></d3s-workflow-monitor-step-grid>
    </simple-accordion>
    <ng-container *ngIf="!isIssueType">
            <div style="padding: 5px">
                Select a step to view details:    
            </div>
            <d3s-workflow-monitor-step-grid [itemSteps]="itemSteps" (selectionChange)="select($event)"></d3s-workflow-monitor-step-grid>
    </ng-container>
`,
    providers: [WorkflowService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepListComponent extends BaseComponent implements OnChanges {
    @Input() itemId: number;
    @Output() selectionChange = new EventEmitter();

    itemSteps: WorkflowItemStep[] = [];
    selection: WorkflowItemStep = null;

    isIssueType = false;
    object: string = null;
    objectId: number = 0;

    constructor(private workflowService: WorkflowService, private ref: ChangeDetectorRef,
        private stateService: StateService ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        debugger
        if ((changes['itemId'] != null && changes['itemId'].currentValue != 0) &&
            //(changes['itemId'].isFirstChange || (changes['itemId'].currentValue != changes['itemId'].previousValue))) {
             (changes['itemId'].currentValue != changes['itemId'].previousValue)) {
            this.load();
        }
    }

    load() {
        debugger
        //this.itemSteps = null;
        //this.object = null;
        //this.objectId = 0;
        //this.isIssueType = false;
       // this.selection = null
        debugger;
        console.log("this.itemId", this.itemId);
        if (this.itemId != null && this.itemId != 0) {
            this.workflowService.getWorkflowItemSteps(this.itemId)
                .then(r => {
                    this.itemSteps = r;
                    if (this.itemSteps != null) {
                        debugger;
                        let index: number;
                        if (this.stateService.workflowItemFilters.itemId == this.itemId
                            && this.stateService.workflowItemFilters.stepId != 0) {
                            index = this.itemSteps.findIndex(x => x.ID == this.stateService.workflowItemFilters.stepId)
                        }

                        if (index != -1) {
                            this.selection = this.itemSteps[index];
                            console.log("index", index);
                        }
                        else {
                            this.selection = this.itemSteps[0];
                        }

                        this.selectionChange.emit(this.selection);
                        this.isIssueType = this.itemSteps[0].IsIssueType;

                        this.object = this.itemSteps[0].Object;
                        this.objectId = this.itemSteps[0].ObjectID;
                    } else {
                        this.itemSteps = null;
                        this.object = null;
                        this.objectId = 0;
                        this.isIssueType = false;
                        this.selection = null
                    }
                    this.ref.markForCheck();
                    //console.log('loaded', this.itemSteps);
                });
        } 
    }

    private select(e: any) {
        debugger;
        this.selection = e;
        if(e)
        this.stateService.workflowItemFilters.stepId = e.ID;
        this.selectionChange.emit(this.selection);
    }

    private export() {
        if (this.itemId != null && this.itemId > 0)
            this.workflowService.exportItemSteps(this.itemId);
    }
}