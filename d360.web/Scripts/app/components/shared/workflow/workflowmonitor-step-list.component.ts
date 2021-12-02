import { Component, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowItemStep } from '../../../models/workflow.model';
import { CompanySettingsService } from '../../../services/settings.service';

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

    constructor(
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges) {
        if ((changes['itemId'] != null && changes['itemId'].currentValue != 0) &&
            (changes['itemId'].isFirstChange || (changes['itemId'].currentValue != changes['itemId'].previousValue))) {
            this.load();
        }
    }

    load() {
        this.itemSteps = null;
        this.object = null;
        this.objectId = 0;
        this.isIssueType = false;
        this.selection = null;
        if (this.itemId != null && this.itemId != 0) {
            this.workflowService.getWorkflowItemSteps(this.itemId)
                .subscribe(r => {
                    this.itemSteps = r;
                    if (this.itemSteps != null) {
                        this.selection = this.itemSteps[0];
                        this.selectionChange.emit(this.selection);
                        this.isIssueType = this.itemSteps[0].IsIssueType;

                        this.object = this.itemSteps[0].Object;
                        this.objectId = this.itemSteps[0].ObjectID;
                    }
                    this.ref.markForCheck();
                    //console.log('loaded', this.itemSteps);
                });
        }
    }

    private select(e: any) {
        this.selection = e;
        this.selectionChange.emit(this.selection);
    }

    private export() {
        if (this.itemId != null && this.itemId > 0)
            this.workflowService.exportItemSteps(this.itemId);
    }
}