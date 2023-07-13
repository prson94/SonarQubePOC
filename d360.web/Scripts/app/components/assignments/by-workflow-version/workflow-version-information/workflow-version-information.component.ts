import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AssignmentVersionItem, NodeModel, WorkflowDiagramModel } from '../../../../models/workflow.model';

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
		NodeModel: NodeModel,
		WorkflowDiagramModel: WorkflowDiagramModel
	}> = new EventEmitter<{ NodeModel: NodeModel, WorkflowDiagramModel: WorkflowDiagramModel }>();

	stepSelection(event: { NodeModel: NodeModel, WorkflowDiagramModel: WorkflowDiagramModel }): void {
		this.nodeSelection.emit(event);
	}
}
