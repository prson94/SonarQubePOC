import { Component, OnInit, OnChanges, Input, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowItemStep, WorkflowActivityType, StepType, WorkflowDiagramNode, NodeModel, ActivityTypeInfo, DiagramObjectType, WorkflowStepDetail, WorkflowChangeType, WorkflowStepReassignment } from '../../models/workflow.model';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { WorkflowHelpers } from '../../static/workflow-helpers';
import { map } from 'rxjs/operators';
import * as _ from 'lodash';
import { Observable,of } from 'rxjs';

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
    reassignments: WorkflowStepReassignment[] = [];
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
    private showAllAnyCondition: boolean = false;
    private isSatisfyAll: boolean = true;

    constructor(private workflowService: WorkflowService, private ref: ChangeDetectorRef, private responsibilityService: ResponsibilityTypeService) {
        super();
    }

    ngOnInit() {
        this.load()
            .pipe(
                map(() => this.responsibilityService.getResponsibilityTypes()
                    .then(r => this.responsibilities = r)),
                map(() => {
                    if (this.step != null)
                        this.workflowService.getWorkflowFieldTypes(this.step.ObjectTypeID, this.step.ObjectType, true)
                            .subscribe(r => {
                                this.fields = r;
                            });
                })
            ).subscribe();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['itemStepId'] != null && (changes['itemStepId'].isFirstChange || (changes['itemStepId'].currentValue != changes['itemStepId'].previousValue))) {
            this.load().subscribe();;
        }
    }

    load(): Observable<any>{
        this.step = null;

        if (this.itemStepId != null) {
            this.isLoading = true;
            return this.workflowService.getWorkflowStepDetail(this.itemStepId)
                .pipe(
                     map(r => {
                        this.isLoading = false;
                        this.step = r;
                        this.reassignments = [];

                        if (this.step.ItemFields != null && this.step.ItemFields.Reassigned != null) {
                            for (let i = 0; i < this.step.ItemFields.Reassigned.length; i++) {
                                this.reassignments.push(new WorkflowStepReassignment(this.step.ItemFields.Reassigned[i]));
                            }
                        }

                        this.ref.markForCheck();
                    }),
                    map(() => {
                        if (typeof this.step.Condition != 'undefined' && typeof this.step.Condition.length != 'undefined') {
                            this.showAllAnyCondition = this.step.Condition.filter(x => x['@FieldTypeID']).length > 1;
                            this.isSatisfyAll = this.step.Condition.every(x => x['@Connector'] == 'AND');
                        }
                    })
                );
        }
        else
            return of();
    }


    private get bulkReassignments() {
        return this.reassignments.filter(r => r.IsBulkReassignment);
    }

    private get reassignment() {
        if (this.reassignments == null || this.reassignments.length < 1)
            return null;
        else if (this.reassignments.length == 1)
            return this.reassignments[0];
        else
            return this.reassignments.find(r => !r.IsBulkReassignment);
    }

    private close() {
        this.visible = false;
        this.visibleChange.emit(false);
        this.ref.markForCheck();
    }
}