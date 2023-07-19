import { Component, Input } from '@angular/core';

@Component({
	selector: 'd3s-workflow-pending-assignments',
	templateUrl: './workflow-pending-assignments.component.html'
})
export class WorkflowPendingAssignmentsComponent {
	@Input() workflowTypeUid: string = '00000000-0000-0000-0000-000000000000';
	@Input() workflowTypeVersion: number;
	@Input() title: string = 'Pending Assignments';
	@Input() showCountPanel: boolean = true;
}
