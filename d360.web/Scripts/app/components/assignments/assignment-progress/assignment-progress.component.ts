import { Component, EventEmitter, Input, Output, ViewChildren } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { AssignmentItemStep } from '../../../models/workflow.model';
import { AssignmentProgressStepComponent } from './assignment-progress-step/assignment-progress-step.component';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { FeatureFlagService } from '../../../guards/feature-flag.service';

@Component({
	selector: 'd3s-assignment-progress',
	templateUrl: './assignment-progress.component.html',
	styleUrls: ['./assignment-progress.component.less']
})
export class AssignmentProgressComponent {

	@ViewChildren(AssignmentProgressStepComponent) assignmentProgressStepComponents: AssignmentProgressStepComponent[];

	@Input({ required: true }) workflowUid: string;
	@Input({ required: true }) workflowTypeVersion: number;
	@Input() shouldBePadded: boolean = false;
	@Input() isCurrentUserAssigned: boolean = false;
	@Input() showCompleteAssignment: boolean = true;
	@Input() isRequestsFlow: boolean = false;
	@Input() hideLinks: boolean = false;
	@Input({ required: true }) set workflowItemUid(value: string) {
		this._workflowItemUid = value;
		if (this._workflowItemUid) {
			this.loadAssignmentSteps();
		}
	}

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

	isLoading: boolean = false;
	assignmentItemSteps: AssignmentItemStep[];

	protected canActivateAssignmentDetails: boolean = false;

	private _workflowItemUid: string;

	constructor(private workflowService: WorkflowService,
				public linkClickInterceptor: LinkClickInterceptor,
				featureFlagService: FeatureFlagService) {
		this.canActivateAssignmentDetails = featureFlagService.canActivateAssignmentDetails();
	}

	private loadAssignmentSteps() {
		this.assignmentItemSteps = [];
		this.isLoading = true;
		this.workflowService.getAssignmentItemSteps(this._workflowItemUid)
			.subscribe((response: AssignmentItemStep[]): void => {
				this.isLoading = false;
				this.assignmentItemSteps = response.sort(function (a: AssignmentItemStep, b: AssignmentItemStep) {
					return (a.StartedOn < b.StartedOn) ? -1 : ((a.StartedOn > b.StartedOn) ? 1 : 0);
				});
			});
	}

	stepSelectionChanged(assignmentItemStep: AssignmentItemStep): void {
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
}
