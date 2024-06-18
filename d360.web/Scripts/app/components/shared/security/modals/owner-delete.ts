import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AssetOwnerModel } from '../../../../models/security.model';
import { SecurityService } from '../../../../services/security.service';

@Component({
	selector: 'owner-delete',
	templateUrl: './owner-delete.html'
})
export class OwnerDelete {
	@Input() item: AssetOwnerModel;
	@Input() isModalVisible: boolean;

	@Output() onCancel = new EventEmitter();
	@Output() onSave = new EventEmitter();

	deleteInProgress: boolean = false;

	constructor(
		private securityService: SecurityService
	) { }

	cancel() {
		this.onCancel.emit();
	}

	delete() {
		this.deleteInProgress = true;
		this.securityService.deletePolicyOverride(this.item.ruleUid)
			.subscribe((result) => {
				result = result[0];
				this.deleteInProgress = false;
				this.onSave.emit(result);
				this.onCancel.emit();
			});
	}

}
