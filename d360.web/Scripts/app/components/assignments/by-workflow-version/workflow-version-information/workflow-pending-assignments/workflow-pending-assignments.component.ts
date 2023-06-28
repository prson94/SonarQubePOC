import { Component, EventEmitter, Input, Output } from '@angular/core';
import { LinkModel, NodeModel, WorkflowDiagramModel } from '../../../../../models/workflow.model';
import { WorkflowService } from '../../../../../services/workflow.service';

@Component({
	selector: 'd3s-workflow-pending-assignments',
	templateUrl: './workflow-pending-assignments.component.html',
	styleUrls: ['./workflow-pending-assignments.component.less']
})
export class WorkflowPendingAssignmentsComponent {
	isLoading: boolean = false;
	version: number;
	private id: number = 0;
	private uid: string = '00000000-0000-0000-0000-000000000000';
	@Output() nodeSelection: EventEmitter<NodeModel> = new EventEmitter<NodeModel>()

	@Input() set workflowTypeId(value: number) {
		if (value) {
			this.id = value;
			this.getWorkflowTypeDetails();
		}
	}

	@Input() set workflowTypeUid(value: string) {
		if (value) {
			this.uid = value;
			this.getWorkflowTypeDetails();
		}
	}

	@Input() set workflowTypeVersion(value: number) {
		if (value) {
			this.version = value;
			this.getWorkflowTypeDetails();
		}
	}

	workflowDiagramModel: WorkflowDiagramModel;

	constructor(private workflowService: WorkflowService) {
	}

	private getWorkflowTypeDetails() {
		this.isLoading = true;
		this.workflowService.getWorkflowDiagram(this.id, this.uid, this.version).subscribe(response => {
			this.isLoading = false;
			this.workflowDiagramModel = response;
		});
	}

	onNodeClick(event: NodeModel) {
		this.nodeSelection.emit(event)
	}

}
