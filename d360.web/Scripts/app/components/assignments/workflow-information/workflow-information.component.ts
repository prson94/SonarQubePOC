import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
	selector: 'd3s-workflow-information',
	templateUrl: './workflow-information.component.html',
	styleUrls: ['./workflow-information.component.less']
})
export class WorkflowInformationComponent implements OnInit {

	@Input() shouldBePadded: boolean = true;
	@Input() showHeaderLine: boolean = true;
	@Input() isSidePanel: boolean = false;
	@Input() interceptLinkClick: boolean = false;
	@Output() linkClick = new EventEmitter();
	@Output() close: EventEmitter<void> = new EventEmitter<void>();
	@Input() workflowTypeId: number = 129;

	constructor() {
	}

	ngOnInit(): void {
	}

}
