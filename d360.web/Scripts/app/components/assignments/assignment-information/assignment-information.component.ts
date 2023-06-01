import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { WorkflowMonitorItem } from '../../../models/workflowmonitor.model';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowAssignments } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-information',
	templateUrl: './assignment-information.component.html',
	styleUrls: ['./assignment-information.component.less']
})
export class AssignmentInformationComponent implements OnInit {

	@Input() workflowAssignmentItem: WorkflowMonitorItem;
	@Input() isIssueType: boolean = false;
	workflowAssignments: WorkflowAssignments;

	@Input() set workflowItemId(value: number) {
		this.loadWorkflowDetails(value);
	}

	@Output() linkClick: EventEmitter<any> = new EventEmitter<any>();

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
	}

	private loadWorkflowDetails(workflowItemId: number) {
		this.workflowService.getWorkflowAssignments().subscribe(response => {
			this.workflowAssignments = response;
		});
	}
}
