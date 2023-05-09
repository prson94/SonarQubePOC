import { Component, Input, OnInit } from '@angular/core'
import { WorkflowMonitorItem } from '../../../models/workflowmonitor.model'

@Component({
	selector: 'd3s-assignment-information',
	templateUrl: './assignment-information.component.html',
	styleUrls: ['./assignment-information.component.less']
})
export class AssignmentInformationComponent implements OnInit {

	@Input() workflowAssignmentItem: WorkflowMonitorItem

	constructor() {
	}

	ngOnInit(): void {
	}

}
