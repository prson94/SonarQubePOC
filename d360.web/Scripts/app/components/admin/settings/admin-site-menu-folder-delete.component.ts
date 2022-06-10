import * as _ from 'lodash';
import {
	Component,
	EventEmitter,
	Input,
	OnInit,
	Output,
	ElementRef,
	QueryList
} from '@angular/core';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { StateService } from '../../../services/state.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
	selector: 'd3s-admin-site-menu-delete-folder',
	providers: [SiteMenuService],
	templateUrl: './admin-site-menu-folder-delete.component.html'
})

export class AdminSiteMenuFolderDeleteComponent extends BaseComponent implements OnInit {
	@Output() onDelete = new EventEmitter();
	@Output() onCancel = new EventEmitter();
	@Input() navigationFolderToDelete: string;

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