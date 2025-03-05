import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SiteNav } from '../../../models/site-menu.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeService } from '../../../services/asset-type.service';
import { HelpService } from '../../../services/help.service';
import { Table } from 'primeng/table';
import { Subscription } from 'rxjs';
import { AdminBaseComponent } from '../../../components/admin/admin-base.component';
import { TableModule } from 'primeng/table';
import { SearchFieldModule } from '../../../components/shared/controls/search-field/search-field.component';
import { SharedGridPagingInfoModule } from '../../../components/shared/grid-paging-info.component';
import { CoreModule } from '../../../components/shared/core.module';

@Component({
	selector: 'site-menu-asset-type',
	templateUrl: './site-menu-asset-type.html',
	standalone: true,
	imports: [CoreModule, FormsModule, SearchFieldModule, SharedGridPagingInfoModule, TableModule]
})
export class SiteMenuAssetType extends AdminBaseComponent implements OnDestroy, OnChanges {
	@Output() onSave = new EventEmitter();
	@Output() onCancel = new EventEmitter();
	@Input() showAddAssetType: boolean = false;

	addAssetTypeHelpLink: string = '';
	simpleTextFilter: string = '';

	areAssetTypesLoading: boolean = false;
	selectedAssetType: any;
	assetTypes: any[] = [];
	addAssetTypeFolderSaving: boolean = false;
	isTableLoading: boolean = false;

	@ViewChild('dt', { static: true }) table: Table;
	loadSub: Subscription;

	constructor(
		headerBreadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService,
		titleService: Title,
		private siteMenuService: SiteMenuService,
		private messagesService: MessagesObservableService,
		private assetTypeService: AssetTypeService,
		private helpService: HelpService
	) {
		super(headerBreadcrumbService, titleService, settingsService);
		this.addAssetTypeHelpLink = this.helpService.getHelpUrl("GOV-0004");
	}

	ngOnDestroy() {
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.showAddAssetType.currentValue !== changes.showAddAssetType.previousValue && this.showAddAssetType) {
			this.loadAvailableTypes();
		}
	}

	loadAvailableTypes() {
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
		this.areAssetTypesLoading = true;
		this.loadSub = this.assetTypeService.GetPossibleAssetTypeForSiteNav()
			.subscribe((res) => {
				this.assetTypes = res;
				this.areAssetTypesLoading = false;
			});
	}

	addAssetType() {
		this.showAddAssetType = true;
	}

	closeAddAssetType() {
		this.selectedAssetType = null;
		this.showAddAssetType = false;
		this.table.reset();
		this.addAssetTypeFolderSaving = false;
		this.simpleTextFilter = '';
		this.onCancel.emit();
	}

	addAssetTypeFolder() {
		if (!this.selectedAssetType) {
			return;
		}
		this.addAssetTypeFolderSaving = true;
		const nav = new SiteNav();
		nav.Name = "#ASSET_TYPE";
		nav.Object = this.selectedAssetType.Object;
		nav.ObjectID = this.selectedAssetType.ObjectID;
		const model = {
			folder: nav
		};

		this.siteMenuService.addFolder(model)
			.subscribe((r) => {
				this.showMessageForResult(this.messagesService, r);
				this.closeAddAssetType();
				this.onSave.emit(model);
				this.loadAvailableTypes();
			});
	}
}
