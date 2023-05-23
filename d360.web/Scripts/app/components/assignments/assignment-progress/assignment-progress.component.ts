import { Component, EventEmitter, Input, OnInit, Output, ViewChildren } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowItemStep } from '../../../models/workflow.model';
import { AssignmentProgressStepComponent } from './assignment-progress-step/assignment-progress-step.component';

@Component({
	selector: 'd3s-assignment-progress',
	templateUrl: './assignment-progress.component.html',
	styleUrls: ['./assignment-progress.component.less']
})
export class AssignmentProgressComponent implements OnInit {

	@ViewChildren(AssignmentProgressStepComponent) assignmentProgressStepComponents: AssignmentProgressStepComponent[];

	@Input() set workflowItemId(value: number) {
		this._workflowItemId = value;
		this.loadData();
	}

	@Output() completeAssignment: EventEmitter<{ workflowId, stepId, assetId }> = new EventEmitter<{
		workflowId;
		stepId;
		assetId
	}>();

	@Output() stepClickChange: EventEmitter<{ workflowItemStep: WorkflowItemStep, open: boolean }> = new EventEmitter<{
		workflowItemStep: WorkflowItemStep,
		open: boolean
	}>();

	@Output() linkClick = new EventEmitter();

	workflowItemSteps: WorkflowItemStep[];

	private _workflowItemId: number;

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
	}

	private loadData(): void {
		this.workflowItemSteps = [];
		if (this._workflowItemId) {
			this.workflowService.getWorkflowItemSteps(this._workflowItemId)
				.subscribe((response: WorkflowItemStep[]): void => {
					this.workflowItemSteps = response;
				});
		}
	}

	stepSelectionChanged(workflowItemStep: WorkflowItemStep, open: boolean): void {
		if (open) {
			this.deselectWorkflowSteps(workflowItemStep);
		}
		this.stepClickChange.emit({ workflowItemStep: workflowItemStep, open: open });
	}

	deselectWorkflowSteps(workflowItemStepToSkip?: WorkflowItemStep) {
		for (const assignmentProgressStepComponent of this.assignmentProgressStepComponents) {
			if (workflowItemStepToSkip !== assignmentProgressStepComponent.workflowItemStep) {
				assignmentProgressStepComponent.selected = false;
			}
		}
	}
}
