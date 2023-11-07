import { Component, Input } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowAssignmentItem, WorkflowAssignments } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-details',
	templateUrl: './assignment-details.component.html',
	styleUrls: ['./assignment-details.component.less']
})
export class AssignmentDetailsComponent {
	@Input({ required: true }) set assignmentUid(value: string) {
		if (value) {
			this.loadWorkflowAssignment(value);
		}
	}

	workflowAssignmentItem: WorkflowAssignmentItem;

	constructor(private workflowService: WorkflowService) {
	}

	private loadWorkflowAssignment(assignmentUid: string): void {
		this.workflowService.getWorkflowAssignments(1, 1, null, '(workflowItemUid eq \'' + assignmentUid + '\')').subscribe((workflowAssignments: WorkflowAssignments): void => {
			this.workflowAssignmentItem = workflowAssignments.items[0];
		});
	}
}
