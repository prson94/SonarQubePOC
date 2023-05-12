import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { WorkflowMonitorItem } from '../../../models/workflowmonitor.model';

@Component({
	selector: 'd3s-assignment-information',
	templateUrl: './assignment-information.component.html',
	styleUrls: ['./assignment-information.component.less']
})
export class AssignmentInformationComponent implements OnInit {

	@Input() workflowAssignmentItem: WorkflowMonitorItem;
	@Input() isIssueType: boolean = false;
	@Output() linkClick: EventEmitter<any> = new EventEmitter<any>();

	constructor() {
	}

	ngOnInit(): void {
	}

}
