import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NodeModel } from '../../../../../models/workflow.model';
import { LinkClickInterceptor } from '../../../../../services/href-click-service';

@Component({
	selector: 'd3s-workflow-pending-assignments',
	templateUrl: './workflow-pending-assignments.component.html',
	styleUrls: ['./workflow-pending-assignments.component.less']
})
export class WorkflowPendingAssignmentsComponent {
	@Input() workflowTypeUid: string = '00000000-0000-0000-0000-000000000000';
	@Input() workflowTypeVersion: number;
	@Input() title: string = 'Pending Assignments';
	@Input() showCountPanel: boolean = true;
}
