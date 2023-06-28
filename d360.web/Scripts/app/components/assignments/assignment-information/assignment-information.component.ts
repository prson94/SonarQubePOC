import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { AssignmentItem, AssignmentItemStep, WorkflowStepDetail } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-information',
	templateUrl: './assignment-information.component.html',
	styleUrls: ['./assignment-information.component.less']
})
export class AssignmentInformationComponent implements OnInit {
	@Input() assignmentItem: AssignmentItem;
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

	isLoading: boolean = false;
	workflowStepDetail: WorkflowStepDetail;

	private assignmentItemStep: AssignmentItemStep;

	@Input() set workflowItemUid(value: string) {
		if (value) {
			this.loadAssignmentItem(value);
			this.loadAssignmentSteps(value);
		}
	};

	@Output() linkClick: EventEmitter<any> = new EventEmitter<any>();

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {

	}

	loadAssignmentItem(workflowItemUid: string): void {
		this.isLoading = true;
		this.workflowService.getAssignmentItem(workflowItemUid).subscribe(response => {
			this.isLoading = false;
			this.assignmentItem = response;
		});
	}

	private loadAssignmentSteps(workflowItemUid: string) {
		let assignmentItemSteps: AssignmentItemStep[];
		this.workflowService.getAssignmentItemSteps(workflowItemUid)
			.subscribe((response: AssignmentItemStep[]): void => {
				assignmentItemSteps = response;
				for (const assignmentItemStep of assignmentItemSteps) {
					if (!assignmentItemStep.CompletedOn) {
						this.assignmentItemStep = assignmentItemStep;
						this.workflowService.getAssignmentStepDetail(assignmentItemStep.Uid).subscribe((response) => {
							this.workflowStepDetail = response;
						});
						break;
					}
				}
			});
	}

	completeAssignmentClick(): void {
		this.completeAssignment.emit({
			workflowItemUid: this.workflowItemUid,
			stepUid: this.assignmentItemStep?.Uid,
			assetId: this.workflowStepDetail?.ObjectID
		});
	}
}
