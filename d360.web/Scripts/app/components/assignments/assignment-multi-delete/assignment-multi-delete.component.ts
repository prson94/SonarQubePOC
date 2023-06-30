import { Component, EventEmitter, Output } from '@angular/core';

@Component({
	selector: 'd3s-assignment-multi-delete',
	templateUrl: './assignment-multi-delete.component.html',
	styleUrls: ['./assignment-multi-delete.component.less']
})
export class AssignmentMultiDeleteComponent {

	constructor() {
	}

	@Output() confirmDelete: EventEmitter<boolean> = new EventEmitter();

	deleteAssignments(): void {
		this.confirmDelete.emit(true);
	}
}
