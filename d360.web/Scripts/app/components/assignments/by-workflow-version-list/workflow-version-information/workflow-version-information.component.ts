import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NodeModel } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-version-information',
	templateUrl: './workflow-version-information.component.html',
	styleUrls: ['./workflow-version-information.component.less']
})
export class WorkflowVersionInformationComponent {
	@Input() workflowTypeUid: string;
	@Input() workflowTypeVersion: number;

	@Output() linkClick = new EventEmitter();
	@Output() close: EventEmitter<void> = new EventEmitter<void>();
	@Output() nodeSelection: EventEmitter<{
		selectedNodeModel: NodeModel;
		workflowTypeUid: string;
		workflowTypeVersion: number
	}> = new EventEmitter<{ selectedNodeModel: NodeModel; workflowTypeUid: string; workflowTypeVersion: number }>();

	stepSelection(event: { selectedNodeModel: NodeModel; workflowTypeUid: string; workflowTypeVersion: number }): void {
		this.nodeSelection.emit(event);
	}
}
