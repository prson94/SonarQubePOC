import { Component, Input } from '@angular/core';
import {
	NodeModel,
	WorkflowActivityType,
	WorkflowDiagramNode,
	WorkflowEventRegistration
} from '../../../../models/workflow.model';
import { WorkflowHelpers } from '../../../../static/workflow-helpers';

@Component({
	selector: 'd3s-step-information',
	templateUrl: './step-information.component.html',
	styleUrls: ['./step-information.component.less']
})
export class StepInformationComponent {

	@Input() selectedNode: NodeModel;
	@Input() workflowEvent: WorkflowEventRegistration;
	@Input() nodeList: WorkflowDiagramNode[];
	isLoading: boolean = false;
	helpers = WorkflowHelpers;

	get icon(): string {
		return this.helpers.getActivityTypeIcon(this.selectedNode.activityType, this.selectedNode.stepType);
	}

	protected readonly WorkflowActivityType = WorkflowActivityType;
}
