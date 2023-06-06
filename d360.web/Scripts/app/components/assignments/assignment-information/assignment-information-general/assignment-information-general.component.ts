import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { AssignmentItem } from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-assignment-information-general',
	templateUrl: './assignment-information-general.component.html',
	styleUrls: ['./assignment-information-general.component.less']
})
export class AssignmentInformationGeneralComponent implements OnInit {
	isLoading: boolean = false;

	@Input() set workflowItemUid(value: string) {
		this.load(value);
	}

	@Input() assignmentItem: AssignmentItem;

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
