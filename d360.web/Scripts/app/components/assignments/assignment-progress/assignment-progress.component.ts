import { Component, Input, OnInit } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowItemStep } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-progress',
	templateUrl: './assignment-progress.component.html',
	styleUrls: ['./assignment-progress.component.less']
})
export class AssignmentProgressComponent implements OnInit {
	private _workflowItemId: number;
	workflowItemSteps: WorkflowItemStep[];

	@Input() set workflowItemId(value: number) {
		this._workflowItemId = value;
		this.loadData();
	}

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
	}

	private loadData(): void {
		this.workflowItemSteps = [];
		if (this._workflowItemId) {
			this.workflowService.getWorkflowItemSteps(this._workflowItemId)
				.subscribe((response: WorkflowItemStep[]) => {
					this.workflowItemSteps = response;
				});
		}
	}
}
