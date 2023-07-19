import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import {
	NodeModel,
	WorkflowActivityType,
	WorkflowDiagramModel,
	WorkflowDiagramNode,
	WorkflowEventRegistration
} from '../../../../models/workflow.model';
import { WorkflowHelpers } from '../../../../static/workflow-helpers';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-step-information',
	templateUrl: './step-information.component.html',
	styleUrls: ['./step-information.component.less']
})
export class StepInformationComponent implements OnInit, OnChanges {
	@Input() workflowTypeVersion: number;
	@Input() workflowTypeUid: string;
	@Input() selectedNode: NodeModel;
	workflowEvent: WorkflowEventRegistration;
	nodeList: WorkflowDiagramNode[];
	isLoading: boolean = false;
	helpers = WorkflowHelpers;
	workflowDiagramModel: WorkflowDiagramModel;

	constructor(private workflowService: WorkflowService) {
	}

	get icon(): string {
		return this.helpers.getActivityTypeIcon(this.selectedNode.activityType, this.selectedNode.stepType);
	}

	protected readonly WorkflowActivityType = WorkflowActivityType;

	private loadWorkflowDiagram() {
		this.workflowService.getWorkflowDiagram(0, this.workflowTypeUid, this.workflowTypeVersion).subscribe((response) => {
			this.workflowDiagramModel = response;
			this.nodeList = this.workflowDiagramModel.Nodes;
			this.workflowEvent = this.workflowDiagramModel.Event;
		});
	}

	ngOnChanges(): void {
		this.loadWorkflowDiagram();
	}

	ngOnInit(): void {
		this.loadWorkflowDiagram();
	}
}
