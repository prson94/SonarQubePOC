import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { AssignmentItem, AssignmentItemStep, WorkflowStepDetail } from '../../../models/workflow.model';
import { FeatureFlagService } from '../../../guards/feature-flag.service';

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

	protected canActivateAssignmentDetails: boolean = false;

	private assignmentItemStep: AssignmentItemStep;
	private _workflowItemUid: string;

	constructor(private workflowService: WorkflowService,
		private cdRef: ChangeDetectorRef, featureFlagService: FeatureFlagService) {
		this.canActivateAssignmentDetails = featureFlagService.canActivateAssignmentDetails();
	}

	loadAssignmentItem(workflowItemUid: string): void {
		this.isAssignmentItemLoading = true;
		this.assignmentItem = null;
		this.workflowService.getAssignmentItem(workflowItemUid).subscribe((response: AssignmentItem): void => {
			this.isAssignmentItemLoading = false;
			this.assignmentItem = response;
			this.cdRef.markForCheck();
		});
	}

	private loadAssignmentSteps(workflowItemUid: string) {
		let assignmentItemSteps: AssignmentItemStep[];
		this.workflowStepDetail = null;
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
