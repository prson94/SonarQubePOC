import { Component, Input, ViewChild } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowAssignmentItem, WorkflowAssignments } from '../../../../models/workflow.model';
import { AssignmentProgressComponent } from '../../assignment-progress/assignment-progress.component';
import {
	AssignmentInformationGeneralComponent
} from '../../assignment-information/assignment-information-general/assignment-information-general.component';

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

	@ViewChild('assignmentProgressComponent') assignmentProgressComponent: AssignmentProgressComponent;
	@ViewChild('assignmentInformationGeneralComponent') assignmentInformationGeneralComponent: AssignmentInformationGeneralComponent;

	workflowAssignmentItem: WorkflowAssignmentItem;

	constructor(private workflowService: WorkflowService) {
	}

	forceRefresh(): void {
		this.assignmentProgressComponent.forceRefresh();
		this.assignmentInformationGeneralComponent.forceRefresh();
	}

	private loadWorkflowAssignment(assignmentUid: string): void {
		this.workflowService.getWorkflowAssignments(1, 1, null, '(workflowItemUid eq \'' + assignmentUid + '\')').subscribe((workflowAssignments: WorkflowAssignments): void => {
			this.workflowAssignmentItem = workflowAssignments.items[0];
		});
	}
}
