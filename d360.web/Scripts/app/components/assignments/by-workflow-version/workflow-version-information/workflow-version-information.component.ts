import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AssignmentByVersion, NodeModel } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-version-information',
	templateUrl: './workflow-version-information.component.html',
	styleUrls: ['./workflow-version-information.component.less']
})
export class WorkflowVersionInformationComponent {
	@Output() linkClick = new EventEmitter();
	@Output() close: EventEmitter<void> = new EventEmitter<void>();

	@Input() assignmentByVersion: AssignmentByVersion;
	@Output() nodeSelection: EventEmitter<NodeModel> = new EventEmitter<NodeModel>();

	stepSelection(event: NodeModel): void {
		this.nodeSelection.emit(event);
	}
}
