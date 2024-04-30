import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { AssignmentItem, AssignmentItemStep, StepState, WorkflowStepDetail } from '../../../models/workflow.model';
import { WorkflowHelpers } from '../../../static/workflow-helpers';

@Component({
	selector: 'd3s-assignment-information',
	templateUrl: './assignment-information.component.html',
	styleUrls: ['./assignment-information.component.less']
})
export class AssignmentInformationComponent {
	@Input() showCompleteAssignment: boolean = false;
	@Input() workflowTypeVersion: number;
	@Input() isCurrentUserAssigned: boolean = false;
	@Input() showLinks: boolean = false;
	@Input({ required: true }) set workflowItemUid(value: string) {
		this.assignmentItem = null;
		this.workflowStepDetail = null;
		if (value) {
			this._workflowItemUid = value;
			this.loadAssignmentItem(value);
			this.loadAssignmentSteps(value);
		}
	}

	get workflowItemUid() {
		return this._workflowItemUid;
	}

	@Output() completeAssignment: EventEmitter<{
		workflowItemUid: string,
		stepUid: string
	}> = new EventEmitter<{
		workflowItemUid: string,
		stepUid: string
	}>();
	assignmentItem: AssignmentItem;
	isAssignmentItemLoading: boolean = false;
	isWorkflowStepDetailLoading: boolean = false;
	workflowStepDetail: WorkflowStepDetail;

	private assignmentItemStep: AssignmentItemStep;
	private _workflowItemUid: string;

	public isFailedAssignment: boolean = false;

	helper = WorkflowHelpers;

	public itemState: { title: string, body: string } = { title: "", body: "" };

	constructor(private workflowService: WorkflowService,
		private cdRef: ChangeDetectorRef) {
	}

	loadAssignmentItem(workflowItemUid: string): void {
		this.isAssignmentItemLoading = true;
		this.isFailedAssignment = false;
		this.workflowService.getAssignmentItem(workflowItemUid).subscribe((response: AssignmentItem): void => {
			if (response.Status === StepState[StepState.Failed] || response.Status === StepState[StepState.Error]) {
				this.itemState = this.helper.workflowStateDetail(response.StatusCode);
				this.isFailedAssignment = true;
			}
			this.isAssignmentItemLoading = false;
			this.assignmentItem = response;
			this.cdRef.markForCheck();
		});
	}

	private loadAssignmentSteps(workflowItemUid: string) {
		let assignmentItemSteps: AssignmentItemStep[];
		this.isWorkflowStepDetailLoading = true;
		this.workflowService.getAssignmentItemSteps(workflowItemUid)
			.subscribe((response: AssignmentItemStep[]): void => {
				assignmentItemSteps = response;
				this.isWorkflowStepDetailLoading = false;
				for (const assignmentItemStep of assignmentItemSteps) {
					if (!assignmentItemStep.CompletedOn) {
						this.assignmentItemStep = assignmentItemStep;
						this.isWorkflowStepDetailLoading = true;
						this.workflowService.getAssignmentStepDetail(assignmentItemStep.Uid).subscribe((response: WorkflowStepDetail) => {
							this.workflowStepDetail = response;
							this.isWorkflowStepDetailLoading = false;
						});
						this.cdRef.markForCheck();
						break;
					}
				}
			});
	}

	completeAssignmentClick(): void {
		this.completeAssignment.emit({
			workflowItemUid: this.workflowItemUid,
			stepUid: this.assignmentItemStep?.Uid
		});
	}
}
