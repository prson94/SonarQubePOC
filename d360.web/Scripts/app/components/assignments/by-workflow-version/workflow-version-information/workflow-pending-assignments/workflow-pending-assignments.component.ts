import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NodeModel, WorkflowDiagramModel } from '../../../../../models/workflow.model';

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

	@Output() nodeSelection: EventEmitter<{
		NodeModel: NodeModel,
		WorkflowDiagramModel: WorkflowDiagramModel
	}> = new EventEmitter<{ NodeModel: NodeModel, WorkflowDiagramModel: WorkflowDiagramModel }>();

	onNodeClick(event: { NodeModel: NodeModel, WorkflowDiagramModel: WorkflowDiagramModel }) {
		this.nodeSelection.emit(event);
	}

}
