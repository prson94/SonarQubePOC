import { Component, OnInit, OnChanges, Input, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowItemStep, WorkflowActivityType, StepType, WorkflowDiagramNode, NodeModel, ActivityTypeInfo, DiagramObjectType, WorkflowStepDetail, WorkflowChangeType } from '../../models/workflow.model';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { WorkflowHelpers } from '../../static/workflow-helpers';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-monitor-step-details',
    templateUrl: 'workflowmonitor-step-details.component.html',
    providers: [WorkflowService, ResponsibilityTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepDetailsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() itemStepId: number;
    @Input() visible: boolean = true;
    @Output() visibleChange = new EventEmitter();
    @Output() onCloseClick = new EventEmitter();
    step: WorkflowStepDetail = null;
    StepType = StepType;
    WorkflowActivityType = WorkflowActivityType;
    WorkflowChangeType = WorkflowChangeType
    responsibilities: any[] = [];
    fields: any[] = [];
    helper = WorkflowHelpers;
    private states = [
        { value: '0', label: 'Pending Add' },
        { value: '1', label: 'Active' },
        { value: '2', label: 'Pending Delete' },
        { value: '3', label: 'Deleted' },
    ];

    constructor(private workflowService: WorkflowService, private ref: ChangeDetectorRef, private responsibilityService: ResponsibilityTypeService) {
        super();
    }

    ngOnInit() {
        this.load()
            .then(() => this.responsibilityService.getResponsibilityTypes())
            .then(r => this.responsibilities = r)
            .then(() => {
                if (this.step != null)
                    this.workflowService.getWorkflowFieldTypes(this.step.ObjectTypeID, this.step.ObjectType, true)
                        .then(r => {
                            this.fields = r;
                        });
            });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['itemStepId'] != null && (changes['itemStepId'].isFirstChange || (changes['itemStepId'].currentValue != changes['itemStepId'].previousValue))) {
            this.load();
        }
    }

    load() {
        this.step = null;

        if (this.itemStepId != null) {
            this.isLoading = true;
            return this.workflowService.getWorkflowStepDetail(this.itemStepId)
                .then(r => {
                    this.isLoading = false;
                    this.step = r;
                    this.ref.markForCheck();
                    console.log('load', this.step);
                });
        }
        else
            return Promise.resolve();
    }

    private close() {
        this.visible = false;
        this.visibleChange.emit(false);
        this.ref.markForCheck();
    }
}