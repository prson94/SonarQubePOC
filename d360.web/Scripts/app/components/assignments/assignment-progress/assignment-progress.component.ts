import { Component, EventEmitter, Input, OnInit, Output, ViewChildren } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { AssignmentItemStep } from '../../../models/workflow.model';
import { AssignmentProgressStepComponent } from './assignment-progress-step/assignment-progress-step.component';

@Component({
	selector: 'd3s-assignment-progress',
	templateUrl: './assignment-progress.component.html',
	styleUrls: ['./assignment-progress.component.less']
})
export class AssignmentProgressComponent implements OnInit {

	@ViewChildren(AssignmentProgressStepComponent) assignmentProgressStepComponents: AssignmentProgressStepComponent[];

	@Input() workflowUid: string;
	@Input() set workflowItemUid(value: string) {
		this._workflowItemUid = value;
		this.loadData();
	}

	get workflowItemUid(): string {
		return this._workflowItemUid;
	}

	@Input() showCompleteAssignment: boolean = true;

	@Output() completeAssignment: EventEmitter<{
		workflowItemUid: string,
		stepUid: string,
		assetId: number
	}> = new EventEmitter<{
		workflowItemUid: string,
		stepUid: string,
		assetId: number
	}>();

	@Output() stepClickChange: EventEmitter<{
		assignmentItemStep: AssignmentItemStep,
		open: boolean
	}> = new EventEmitter<{
		assignmentItemStep: AssignmentItemStep,
		open: boolean
	}>();

	@Output() linkClick: EventEmitter<{ objectType: string, objectUid: string }> = new EventEmitter<{
		objectType: string,
		objectUid: string
	}>();

	isLoading: boolean = false;
	assignmentItemSteps: AssignmentItemStep[];

	private _workflowItemUid: string;

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
	}

	private loadData(): void {
		this.assignmentItemSteps = [];
		if (this._workflowItemUid) {
			this.loadAssignmentSteps();
		}
	}

	private loadAssignmentSteps() {
		this.isLoading = true;
		this.workflowService.getAssignmentItemSteps(this._workflowItemUid)
			.subscribe((response: AssignmentItemStep[]): void => {
				this.isLoading = false;
				this.assignmentItemSteps = response;
			});
	}

	stepSelectionChanged(assignmentItemStep: AssignmentItemStep, open: boolean): void {
		if (open) {
			this.deselectWorkflowSteps(assignmentItemStep);
		}
		this.stepClickChange.emit({ assignmentItemStep: assignmentItemStep, open: open });
	}

	deselectWorkflowSteps(workflowItemStepToSkip?: AssignmentItemStep) {
		for (const assignmentProgressStepComponent of this.assignmentProgressStepComponents) {
			if (workflowItemStepToSkip !== assignmentProgressStepComponent.assignmentItemStep) {
				assignmentProgressStepComponent.selected = false;
			}
		}
	}
}
