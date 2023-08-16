import { Component } from '@angular/core';
import { WorkflowStepDetail } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-form-response',
	templateUrl: './assignment-form-response.component.html',
	styleUrls: ['./assignment-form-response.component.less']
})
export class AssignmentFormResponseComponent {
	isModalVisible: boolean = false;
	step: WorkflowStepDetail = null;

	openModal(step: WorkflowStepDetail): void {
		this.step = step;
		this.isModalVisible = true;
	}

	getDate(value: string): string {
		if (!isNaN(Date.parse(value))) {
			return new Date(value).toLocaleDateString();
		} else {
			return '';
		}
	}

	getUrl(value: string): string {
		return value.split('|')[1];
	}

	getName(value: string): string {
		return value.split('|')[0];
	}
}
