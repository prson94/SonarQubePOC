import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
	selector: 'd3s-workflow-version-information',
	templateUrl: './workflow-version-information.component.html',
	styleUrls: ['./workflow-version-information.component.less']
})
export class WorkflowVersionInformationComponent {
	@Output() linkClick = new EventEmitter();
	@Output() close: EventEmitter<void> = new EventEmitter<void>();

	@Input() workflowTypeVersionId: number;

	@Input() workflowTypeId: number;

	@Input() workflowTypeUid: string;

	@Input() workflowTypeVersion: number;

}
