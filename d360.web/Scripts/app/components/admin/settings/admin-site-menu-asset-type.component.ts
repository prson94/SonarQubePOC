import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges, ViewChild, ViewEncapsulation } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, CompanyImage, AppSettingsEnum, } from '../../../models/settings.model';
import { SiteNav } from '../../../models/site-menu.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FeatureFlagsService } from '../../../services/featureflags.service';
import { AssetTypeService } from '../../../services/asset-type.service';
import { Table } from 'primeng/table';

@Component({
	selector: 'd3s-admin-site-menu-asset-type-editor',
	providers: [SiteMenuService],
	templateUrl: './admin-site-menu-asset-type.component.html'
})

export class AdminSiteMenuAssetTypeEditorComponent extends AdminBaseComponent implements OnInit {
	@Output() onSave = new EventEmitter();
	@Output() onCancel = new EventEmitter();

	addAssetTypeHelpLink: string = '';
	simpleTextFilter: string = '';

	showAddAssetType: boolean = false;
	areAssetTypesLoading: boolean = false;
	selectedAssetType: any;
	assetTypes: any[] = [];
	addAssetTypeFolderSaving: boolean = false;

	@ViewChild('dt', { static: true }) table: Table;

	constructor(
		headerBreadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService,
		titleService: Title,
		private siteMenuService: SiteMenuService,
		private stateService: StateService,
		private messagesService: MessagesObservableService,
		private featureFlagService: FeatureFlagsService,
		private assetTypeService: AssetTypeService
	) {
		super(headerBreadcrumbService, titleService, settingsService);
		let helpBaseUri: string = this.settingsService.getAppSetting(AppSettingsEnum.HelpBaseUri);
		this.addAssetTypeHelpLink = helpBaseUri + "Default.htm#d-admin/establishing-responsibilities.htm?TocPath=Administration%257CManaging%2520users%2520and%2520groups%257C_____3";
	}

	ngOnInit() {
		this.assetTypeService.GetPossibleAssetTypeForSiteNav()
			.subscribe((res) => {
				this.assetTypes = res;
				this.areAssetTypesLoading = false;
			});
	}

	addAssetType() {
		this.showAddAssetType = true;
		this.areAssetTypesLoading = true;
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
		let nav = new SiteNav();
		nav.Name = "#ASSET_TYPE";
		nav.Object = this.selectedAssetType.Object;
		nav.ObjectID = this.selectedAssetType.ObjectID;
		var model = {
			folder: nav
		};

		this.siteMenuService.addFolder(model)
			.subscribe((r) => {
				this.showMessageForResult(this.messagesService, r);
				this.closeAddAssetType();
				this.onSave.emit(model);
			});
	}
}
