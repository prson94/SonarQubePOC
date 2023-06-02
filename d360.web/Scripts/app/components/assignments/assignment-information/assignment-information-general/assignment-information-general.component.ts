import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { WorkflowMonitorItem } from '../../../../models/workflowmonitor.model';
import { WorkflowAssignmentItem } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-information-general',
	templateUrl: './assignment-information-general.component.html',
	styleUrls: ['./assignment-information-general.component.less']
})
export class AssignmentInformationGeneralComponent implements OnInit {

	@Input() workflowAssignmentItem: WorkflowAssignmentItem;
	@Output() linkClick: EventEmitter<any> = new EventEmitter<any>();

	constructor() {
	}

	ngOnInit(): void {
	}

}
