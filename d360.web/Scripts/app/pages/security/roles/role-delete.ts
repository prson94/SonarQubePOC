import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ReadRole } from '../../../models/security.model';
import { SecurityService } from '../../../services/security.service';
import { DirectivesModule } from '../../../directives/directives.module';
import { ButtonModule } from '../../../directives/ig-button-directive';
import { SiteModalModule } from '../../../components/shared/modal/gov-modal.module';

@Component({
	selector: 'role-delete',
	templateUrl: './role-delete.html',
	standalone: true,
	imports: [
		ButtonModule,
		DirectivesModule,
		SiteModalModule
	]
})
export class RoleDelete {
	@Input() item: ReadRole;
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
		this.securityService.deleteRole(this.item.uid)
			.subscribe((result) => {
				this.deleteInProgress = false;
				if (result != undefined) {
					result = result[0];
					this.onDelete.emit(result);
				}
				else {
					this.deleteInProgress = false;
				}
				this.onCancel.emit();
			});
	}

}
