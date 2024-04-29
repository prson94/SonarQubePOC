import { Component, EventEmitter, Input, Output, ViewChildren, ChangeDetectorRef } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { AssignmentItemStep, StepState } from '../../../models/workflow.model';
import { AssignmentProgressStepComponent } from './assignment-progress-step/assignment-progress-step.component';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { WorkflowHelpers } from '../../../static/workflow-helpers';

@Component({
	selector: 'd3s-assignment-progress',
	templateUrl: './assignment-progress.component.html',
	styleUrls: ['./assignment-progress.component.less']
})
export class AssignmentProgressComponent {

	@ViewChildren(AssignmentProgressStepComponent) private assignmentProgressStepComponents: AssignmentProgressStepComponent[];

	@Input({ required: true }) workflowUid: string;
	@Input({ required: true }) workflowTypeVersion: number;
	@Input() shouldBePadded: boolean = false;
	@Input() isCurrentUserAssigned: boolean = false;
	@Input() showCompleteAssignment: boolean = true;
	@Input() showLinks: boolean = false;
	@Input({ required: true }) set workflowItemUid(value: string) {
		this._workflowItemUid = value;
		if (this._workflowItemUid) {
			this.loadAssignmentSteps();
		}
	}
	@Input() assignmentStatus: string;

	get workflowItemUid(): string {
		return this._workflowItemUid;
	}

	@Output() completeAssignment: EventEmitter<{
		workflowItemUid: string,
		stepUid: string
	}> = new EventEmitter<{
		workflowItemUid: string,
		stepUid: string
	}>();

	@Output() stepClickChange: EventEmitter<AssignmentItemStep> = new EventEmitter<AssignmentItemStep>();

	protected isLoading: boolean = false;
	protected assignmentItemSteps: AssignmentItemStep[];

	private _workflowItemUid: string;

	public isFailedAssignment: boolean = false;

	helper = WorkflowHelpers;

	public itemState: { title: string, body: string } = {title:"", body:""};

	constructor(private workflowService: WorkflowService,
				public linkClickInterceptor: LinkClickInterceptor,
				private changeDetectorRef: ChangeDetectorRef) {
	}

	private loadAssignmentSteps() {
		this.assignmentItemSteps = [];
		this.isFailedAssignment = false;
		this.isLoading = true;
		this.workflowService.getAssignmentItemSteps(this._workflowItemUid)
			.subscribe((response: AssignmentItemStep[]): void => {
				this.isLoading = false;
				this.assignmentItemSteps = response.sort(function (a: AssignmentItemStep, b: AssignmentItemStep) {
					return (a.StartedOn < b.StartedOn) ? -1 : ((a.StartedOn > b.StartedOn) ? 1 : 0);
				});
				if (this.assignmentItemSteps.some(x => (x.Status !== StepState.Complete && x.Status !== StepState.Pending))) {
					let failedStep = this.assignmentItemSteps.find(x => (x.Status !== StepState.Complete && x.Status !== StepState.Pending));
					this.itemState = this.helper.workflowStateDetail(StepState[failedStep.StatusCode].toString());
					this.isFailedAssignment = true;
				} else if (this.assignmentStatus === StepState[StepState.Failed] || this.assignmentStatus === StepState[StepState.Error]) {
					this.itemState = this.helper.workflowStateDetail(this.assignmentStatus);
					this.isFailedAssignment = true;
				}
				this.changeDetectorRef.detectChanges();
			});
	}

	protected stepSelectionChanged(assignmentItemStep: AssignmentItemStep): void {
		this.deselectWorkflowSteps(assignmentItemStep);
		this.stepClickChange.emit(assignmentItemStep);
	}

	deselectWorkflowSteps(workflowItemStepToSkip?: AssignmentItemStep) {
		for (const assignmentProgressStepComponent of this.assignmentProgressStepComponents) {
			if (workflowItemStepToSkip !== assignmentProgressStepComponent.assignmentItemStep) {
				assignmentProgressStepComponent.selected = false;
			}
		}
	}

	forceRefresh(): void {
		this.workflowItemUid = this._workflowItemUid;
	}

	clearStepSelection() {
		this.stepSelectionChanged(null);
	}
}
