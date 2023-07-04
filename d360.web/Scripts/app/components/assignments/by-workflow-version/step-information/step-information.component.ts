import { Component, Input } from '@angular/core';
import { NodeModel, StepType, WorkflowActivityType } from '../../../../models/workflow.model';
import { WorkflowHelpers } from '../../../../static/workflow-helpers';

@Component({
  selector: 'd3s-step-information',
  templateUrl: './step-information.component.html',
  styleUrls: ['./step-information.component.less']
})
export class StepInformationComponent{
	@Input() selectedNode: NodeModel
	isLoading: boolean = false
	helpers = WorkflowHelpers

	get icon(): string {
		return this.helpers.getActivityTypeIcon(this.selectedNode.activityType,this.selectedNode.stepType);
	}

}
