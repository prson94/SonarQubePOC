import { Component, Input, OnInit } from '@angular/core';

@Component({
	selector: 'd3s-workflow-information-diagram',
	templateUrl: './workflow-information-diagram.component.html',
	styleUrls: ['./workflow-information-diagram.component.less']
})
export class WorkflowInformationDiagramComponent implements OnInit {
	@Input() workflowTypeId: number = 0;
	@Input() workflowTypeUid: string = '00000000-0000-0000-0000-000000000000';

	constructor() {
	}

	ngOnInit(): void {
	}

}
