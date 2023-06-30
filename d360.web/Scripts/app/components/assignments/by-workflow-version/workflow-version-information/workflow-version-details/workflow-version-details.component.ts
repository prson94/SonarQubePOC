import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AssignmentByVersion } from '../../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-version-details',
	templateUrl: './workflow-version-details.component.html',
	styleUrls: ['./workflow-version-details.component.less']
})
export class WorkflowVersionDetailsComponent {

	@Input() assignmentByVersion: AssignmentByVersion;
	@Output() linkClick: EventEmitter<{ objectType: string, objectUid: string }> = new EventEmitter<{
		objectType: string,
		objectUid: string
	}>();
}
