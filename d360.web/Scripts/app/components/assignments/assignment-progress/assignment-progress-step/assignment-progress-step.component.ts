import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import {
	AssignmentItemStep,
	StepType,
	WorkflowActivityType,
	WorkflowStepDetail
} from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-assignment-progress-step',
	templateUrl: './assignment-progress-step.component.html',
	styleUrls: ['./assignment-progress-step.component.less']
})
export class AssignmentProgressStepComponent implements OnInit {

	@Input() assignmentItemStep: AssignmentItemStep;

	@Input() workflowItemUid: string;

	@Input() isLastStep: boolean = false;

	@Input() showCompleteAssignment: boolean = true;

	@Output() completeAssignment: EventEmitter<{
		workflowUid: string,
		stepUid: string,
		assetUid: string
	}> = new EventEmitter<{
		workflowUid: string,
		stepUid: string,
		assetUid: string
	}>();

	@Output() stepClickChange: EventEmitter<boolean> = new EventEmitter<boolean>();

	workflowStepDetail: WorkflowStepDetail;

	isLoading: boolean = false;
	selected: boolean = false;

	get header(): string {
		return this.assignmentItemStep.Name;
	}

	get status(): string {
		return this.assignmentItemStep.CompletedOn ? 'Done' : 'Current step';
	}

	get message(): string {
		return 'Assigned to ' + this.assignmentItemStep.StartedByUid + '\nOpen for ' + this.getTimeSpan(Date.parse(this.assignmentItemStep.StartedOn));
	}

	get icon(): string {
		if (this.assignmentItemStep.StepType === StepType.Start.toString()) {
			return 'fa-play-circle';
		} else if (this.assignmentItemStep.StepType === StepType.Finish.toString()) {
			return 'fa-stop-circle';
		} else if (this.assignmentItemStep.ActivityType === WorkflowActivityType.EmailNotification.toString()) {
			return 'fa-envelope';
		} else if (this.assignmentItemStep.ActivityType === WorkflowActivityType.Form.toString()) {
			return 'fa-sliders';
		} else if (this.assignmentItemStep.ActivityType === WorkflowActivityType.FieldChange.toString()) {
			return 'fa-sliders';
		} else if (this.assignmentItemStep.ActivityType === WorkflowActivityType.HTTPRequest.toString()) {
			return 'fa-globe';
		} else if (this.assignmentItemStep.ActivityType === WorkflowActivityType.HTTPResponse.toString()) {
			return 'fa-cogs';
		} else if (this.assignmentItemStep.ActivityType === WorkflowActivityType.RelationshipUpdate.toString()) {
			return 'fa-users';
		} else if (this.assignmentItemStep.ActivityType === WorkflowActivityType.Delete.toString()) {
			return 'fa-trash';
		}
	}

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
		// if (!this.assignmentItemStep.Complete) {
		// 	this.isLoading = true;
		// 	this.workflowService.getWorkflowStepDetail(this.assignmentItemStep.ID).subscribe((response) => {
		// 		this.workflowStepDetail = response;
		// 		this.isLoading = false;
		// 	});
		// }
	}

	toggleStepDetails(): void {
		this.selected = !this.selected;
		this.stepClickChange.emit(this.selected);
	}

	completeAssignmentClick() {
		this.completeAssignment.emit({
			workflowUid: this.workflowItemUid,
			stepUid: this.assignmentItemStep.Uid,
			assetUid: undefined
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
