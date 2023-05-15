import { Component, Input, OnInit } from '@angular/core';
import { StepType, WorkflowItemStep } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-progress-step',
	templateUrl: './assignment-progress-step.component.html',
	styleUrls: ['./assignment-progress-step.component.less']
})
export class AssignmentProgressStepComponent implements OnInit {

	@Input() workflowItemStep: WorkflowItemStep;

	get header(): string {
		return StepType[this.workflowItemStep.StepType];
	}

	get status(): string {
		return this.workflowItemStep.Complete ? 'Done' : (this.isLastStep ? 'In Progress' : 'Not started');
	}

	get message(): string {
		return '';
	}

	get icon(): string {
		if (this.workflowItemStep.StepType === 1) {
			return 'fa-play-circle';
		} else if (this.workflowItemStep.StepType === 4) {
			return 'fa-stop-circle';
		}
	}

	@Input() isLastStep: boolean = false;

	constructor() {
	}

	ngOnInit(): void {
	}

	showStepDetails(workflowItemStep: WorkflowItemStep) {

	}
}
