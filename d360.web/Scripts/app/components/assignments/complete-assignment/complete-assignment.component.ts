import { Component, OnInit } from '@angular/core';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less']
})
export class CompleteAssignmentComponent implements OnInit {

	isModalVisible: boolean = false;

	constructor() {
	}

	ngOnInit(): void {
	}
	
	openModal(): void {
		this.isModalVisible = true
	}

	submit(): void {
		console.log("submit")
	}

}
