import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import {
	StepType,
	WorkflowActivityType,
	WorkflowItemStep,
	WorkflowStepDetail
} from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-assignment-progress-step',
	templateUrl: './assignment-progress-step.component.html',
	styleUrls: ['./assignment-progress-step.component.less']
})
export class AssignmentProgressStepComponent implements OnInit {

	@Input() workflowItemStep: WorkflowItemStep;

	@Input() isLastStep: boolean = false;

	@Output() completeAssignment: EventEmitter<{ workflowId, stepId, assetId }> = new EventEmitter<{
		workflowId;
		stepId;
		assetId
	}>();

	workflowStepDetail: WorkflowStepDetail;

	isLoading: boolean = false;

	get header(): string {
		return this.workflowItemStep.Name;
	}

	get status(): string {
		return this.workflowItemStep.Complete ? 'Done' : 'In Progress';
	}

	get message(): string {
		return 'Assigned to ' + this.workflowItemStep.Assignee + '\nOpen for ' + this.getTimeSpan(Date.parse(this.workflowItemStep.StartedOn));
	}

	get icon(): string {
		if (this.workflowItemStep.StepType === StepType.Start) {
			return 'fa-play-circle';
		} else if (this.workflowItemStep.StepType === StepType.Finish) {
			return 'fa-stop-circle';
		} else if (this.workflowItemStep.ActivityType === WorkflowActivityType.EmailNotification) {
			return 'fa-envelope';
		} else if (this.workflowItemStep.ActivityType === WorkflowActivityType.Form) {
			return 'fa-sliders';
		} else if (this.workflowItemStep.ActivityType === WorkflowActivityType.FieldChange) {
			return 'fa-sliders';
		}
	}

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
		if (!this.workflowItemStep.Complete) {
			this.isLoading = true;
			this.workflowService.getWorkflowStepDetail(this.workflowItemStep.ID).subscribe((response) => {
				this.workflowStepDetail = response;
				this.isLoading = false;
			});
		}
	}

	showStepDetails() {

	}

	completeAssignmentClick() {
		this.completeAssignment.emit({
			workflowId: this.workflowItemStep.ID,
			stepId: this.workflowItemStep.ItemID,
			assetId: this.workflowItemStep.ObjectID
		});
	}

	private getTimeSpan(startDateMilliseconds: number, endDateMilliseconds: number = Date.now()): string {
		const totalMilliseconds: number = endDateMilliseconds - startDateMilliseconds;
		const minutes: number = Math.floor(Math.abs(totalMilliseconds) / (60 * 1000));
		const hours: number = Math.floor(minutes / 60);
		const days: number = Math.floor(hours / 24);

		return `${days}d ${hours % 24}h ${minutes % 60}m`;
	}
}
