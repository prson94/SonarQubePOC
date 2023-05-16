import { Component, OnInit } from '@angular/core';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less']
})
export class CompleteAssignmentComponent implements OnInit {

	isModalVisible: boolean = false;
	isAssignmentProgressSelected: boolean = false;
	modalTitle: string = 'Assignment';

	constructor() {
	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	openModal(): void {
		this.isModalVisible = true;
	}

	submit(): void {
		console.log('submit');
	}

	showAssignment(): void {
		this.isAssignmentProgressSelected = false
		this.modalTitle = 'Assignment'
	}

	showAssignmentProgress(): void {
		this.isAssignmentProgressSelected = true
		this.modalTitle = 'Assignment Progress and Information'
	}

}
