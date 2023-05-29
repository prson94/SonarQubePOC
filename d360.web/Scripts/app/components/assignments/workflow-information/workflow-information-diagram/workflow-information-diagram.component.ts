import { Component, Input, OnInit } from '@angular/core';
import { WorkflowDiagramModel } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-information-diagram',
	templateUrl: './workflow-information-diagram.component.html',
	styleUrls: ['./workflow-information-diagram.component.less']
})
export class WorkflowInformationDiagramComponent implements OnInit {
	@Input() workflowDiagramModel: WorkflowDiagramModel

	constructor() {
	}

	ngOnInit(): void {
	}

}
