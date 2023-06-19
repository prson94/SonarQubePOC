import { Component } from '@angular/core';
import { WorkflowStepDetail } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-form-response',
	templateUrl: './assignment-form-response.component.html',
	styleUrls: ['./assignment-form-response.component.less']
})
export class AssignmentFormResponseComponent {
	isModalVisible: boolean = false;
	step: WorkflowStepDetail = null

	openModal(step: WorkflowStepDetail): void {
		this.step = step
		this.isModalVisible = true
	}
}
