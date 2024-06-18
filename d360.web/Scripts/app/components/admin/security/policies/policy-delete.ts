import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ReadSecurityPolicy } from '../../../../models/security.model';
import { SecurityService } from '../../../../services/security.service';

@Component({
	selector: 'policy-delete',
	templateUrl: './policy-delete.html'
})
export class PolicyDelete {
	@Input() item: ReadSecurityPolicy;
	@Input() isModalVisible: boolean;

	@Output() onCancel = new EventEmitter();
	@Output() onDelete = new EventEmitter();

	deleteInProgress: boolean = false;

	constructor(
		private securityService: SecurityService
	) { }

	cancel() {
		this.onCancel.emit();
	}

	delete() {
		this.deleteInProgress = true;
		this.securityService.deletePolicy(this.item.uid)
			.subscribe((result) => {
				result = result[0];
				this.deleteInProgress = false;
				this.onDelete.emit(result);
				this.onCancel.emit();
			});
	}

}
