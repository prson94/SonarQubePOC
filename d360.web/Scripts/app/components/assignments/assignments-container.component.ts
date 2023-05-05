import { Component, OnInit } from '@angular/core'

@Component({
	selector: 'd3s-assignments-container',
	template: `
		<div id="main">
			<router-outlet></router-outlet>
		</div>
	`
})
export class AssignmentsContainerComponent implements OnInit {

	constructor() {
	}

	ngOnInit(): void {
	}

}
