import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { AssignmentItem } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-information',
	templateUrl: './assignment-information.component.html',
	styleUrls: ['./assignment-information.component.less']
})
export class AssignmentInformationComponent implements OnInit {
	@Input() assignmentItem: AssignmentItem;
	isLoading: boolean = false;

	@Input() set workflowItemUid(value: string) {
		this.load(value);
	};

	@Output() linkClick: EventEmitter<any> = new EventEmitter<any>();

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {

	}

	load(workflowItemUid: string): void {
		this.isLoading = true;
		this.workflowService.getAssignmentItem(workflowItemUid).subscribe(response => {
			this.isLoading = false;
			this.assignmentItem = response;
		});
	}
}
