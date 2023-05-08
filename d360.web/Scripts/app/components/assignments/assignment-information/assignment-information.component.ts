import { Component, Input, OnInit } from '@angular/core'

@Component({
	selector: 'd3s-assignment-information',
	templateUrl: './assignment-information.component.html',
	styleUrls: ['./assignment-information.component.less']
})
export class AssignmentInformationComponent implements OnInit {

	@Input() workflowItemId: number

	constructor() {
	}

	ngOnInit(): void {
	}

}
