import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { StateService } from '../../../services/state.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
	selector: 'd3s-admin-site-menu-delete-dialog',
	providers: [SiteMenuService],
	templateUrl: './admin-site-menu-delete-dialog.component.html'
})

export class AdminSiteMenuDeleteDialogComponent extends BaseComponent implements OnInit {
	@Output() onDelete = new EventEmitter();
	@Output() onCancel = new EventEmitter();
	@Input() siteNavigationItemTitle: string;
	@Input() siteNavigationItemType: string;


	constructor(
		protected settingsService: CompanySettingsService,
		private stateService: StateService,
		private messagesService: MessagesObservableService
	) {
		super(settingsService);
	}

	ngOnInit() {
	}

	closeDeleteDialog() {
		this.onCancel.emit();
	}

	confirmDeleteFolder() {
		this.onDelete.emit();
	}
}