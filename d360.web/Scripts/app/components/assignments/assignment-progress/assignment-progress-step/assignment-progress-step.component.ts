import { Component, Input, OnInit } from '@angular/core';

@Component({
	selector: 'd3s-assignment-progress-step',
	templateUrl: './assignment-progress-step.component.html',
	styleUrls: ['./assignment-progress-step.component.less']
})
export class AssignmentProgressStepComponent implements OnInit {

	/**
	 *
	 */
	@Input() header: string;

	/**
	 *
	 */
	@Input() status: string;

	/**
	 *
	 */
	@Input() message: string;

	/**
	 *
	 */
	@Input() icon: string = 'information';

	@Input() isLastStep: boolean = false

	constructor() {
	}

	ngOnInit(): void {
	}

}
