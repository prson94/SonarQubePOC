import { Component, EventEmitter, Input, Output, ViewEncapsulation } from "@angular/core";
/*global $localize*/

@Component({
	selector: "action-modal-form",
	templateUrl: './action-modal-form.component.html',
	encapsulation: ViewEncapsulation.None
})
export class ActionModalFormComponent {
	@Input() isModalVisible: boolean = false;
	@Input() uid: string;

	@Output() onClose = new EventEmitter();
	@Output() onUpdated = new EventEmitter();

	constructor(
	) {
	}


	close() {
		this.onClose.emit();
	}
	save() {

	}

}
