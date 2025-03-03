import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CoreModule } from '../../../components/shared/core.module';

@Component({
	selector: 'site-menu-delete-dialog',
	templateUrl: './site-menu-delete-dialog.html',
	standalone: true,
	imports: [CoreModule]
})
export class SiteMenuDeleteDialog {
	@Output() onDelete = new EventEmitter();
	@Output() onCancel = new EventEmitter();
	@Input() siteNavigationItemTitle: string;
	@Input() siteNavigationItemType: string;

	closeDeleteDialog() {
		this.onCancel.emit();
	}

	confirmDeleteFolder() {
		this.onDelete.emit();
	}
}