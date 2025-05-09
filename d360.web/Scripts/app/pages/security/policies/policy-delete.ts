import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ReadSecurityPolicy } from '../../../models/security.model';
import { SecurityService } from '../../../services/security.service';
import { ButtonModule } from '../../../directives/ig-button-directive';
import { SiteModalModule } from '../../../components/shared/modal/gov-modal.module';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
	selector: 'policy-delete',
	templateUrl: './policy-delete.html',
	standalone: true,
	imports: [
		ButtonModule,
		SiteModalModule
	]
})
export class PolicyDelete {
	@Input() item: ReadSecurityPolicy;
	@Input() isModalVisible: boolean;

	@Output() onCancel = new EventEmitter();
	@Output() onDelete = new EventEmitter();

	deleteInProgress: boolean = false;

	constructor(
		protected messageService: MessagesObservableService,
		private securityService: SecurityService
	) { }

	cancel() {
		this.onCancel.emit();
	}

	delete() {
		this.deleteInProgress = true;
		this.securityService.deletePolicy(this.item.uid)
			.subscribe((result) => {
				this.deleteInProgress = false;
				this.onDelete.emit();
			}, (err) => {
				this.deleteInProgress = false;
				this.messageService.showError("Unable to remove policy", err);
			});
	}

}
