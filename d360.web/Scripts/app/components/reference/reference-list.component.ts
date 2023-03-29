import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Location } from '@angular/common';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ReferenceItemType } from '../../models/reference.model';
import { SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { ReferenceService } from '../../services/reference.service';
import { AuthenticationService } from '../../services/authentication.service';
import { FormMode } from '../../models/form.model';
import { AssetTypeService } from '../../services/asset-type.service';
import { Subscription } from 'rxjs';
import { CompanySettingsService } from '../../services/settings.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { HeaderActions } from '../../models/header.model';

@Component({
	selector: 'd3s-reference-list',
	templateUrl: './reference-list.component.html',
	providers: [PermissionsService, ReferenceService, AssetTypeService],
})

export class ReferenceListComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() assetTypeUid: string = "";

	private selectedReferenceItemType: ReferenceItemType;
	private selectedReferenceListUid: string = '';

	private showDefault: boolean = true;

	private canAddReferenceItem: boolean = false;

	private loadPermissionSub: Subscription;
	private replaceUrl: boolean = true;
	highlightUid: string = '';
	constructor(
		private assetTypeService: AssetTypeService,
		protected authenticationService: AuthenticationService,
		protected headerBreadcrumbService: HeaderBreadcrumbService,
		public headerActionsService: HeaderActionsService,
		private permissionsService: PermissionsService,
		protected referenceService: ReferenceService,
		secondaryNavService: SecondaryNavService,
		protected settingsService: CompanySettingsService,
		protected titleService: Title,
		private location: Location
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerBreadcrumbService;
	}

	ngOnInit() {
		this.setBrowserTitle(this.titleService, 'Reference');
		this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);

		const headerActions: HeaderActions = new HeaderActions();
		headerActions.showRaiseIssue = false;
		this.headerActionsService.setCurrentHeaderActions(headerActions);

		this.loadSelectedItem();

	}
	
	// eslint-disable-next-line
	private loadSelectedItem() {
		var refListIdString = "";
		//load default perms
		refListIdString = this.assetTypeUid;
		if (this.assetTypeUid && (this.assetTypeUid as string).indexOf(',') !== -1) {
			var items = refListIdString.split(',');
			refListIdString = items[0];
			this.highlightUid = items[1];
		}
		else {
			this.selectedReferenceListUid = this.assetTypeUid;
		}

		if (refListIdString) {
			if (refListIdString.toString().length === 36) {
				this.baseAssetTypeUid = this.selectedReferenceListUid = refListIdString;
				this.load();
				if (this.selectedReferenceItemType && this.selectedReferenceItemType.uid !== this.selectedReferenceListUid) {
					var referenceItemType: ReferenceItemType = new ReferenceItemType();
					referenceItemType.uid = this.selectedReferenceListUid;
					this.changeType(referenceItemType, true);
				}
				this.replaceUrl = false;
			}
			else if (this.selectedReferenceListUid != null) {
				this.load();
				this.replaceUrl = true;
			}
		}
	}

	private load() {
		//check if the user has permission to read the selected type
		if (this.loadPermissionSub) { this.loadPermissionSub.unsubscribe(); }

		if (this.selectedReferenceListUid) {
			this.permissionsService.getAssetTypePermissions(this.selectedReferenceListUid)
				.subscribe((res) => {
					this.objectPermission = res;
					this.canAddReferenceItem = this.hasAddAssetPermissions();
				});

			this.buildSecondaryNavigationForAssetTypeUid(this.selectedReferenceListUid, () => {
				this.headerBreadcrumbService.getFolderTitle('#Reference').then((res) => {
					this.headerBreadcrumbService.clearBreadcrumbs();
					this.headerBreadcrumbService.clearCurrentObjectInfo();
					this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res));
					if (this.selectedReferenceItemType) { this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.selectedReferenceItemType.Name)); }
					if (this.auditSidebar) {
						this.auditSidebar.url = `/assets/${this.selectedReferenceListUid}/log`;
					}
				});
			});
		}
	}

	ngOnDestroy() {
		this.clearSidebar();
		if (this.loadPermissionSub) { this.loadPermissionSub.unsubscribe(); }
	}

	private changeFormMode(formMode: FormMode) {
		if (formMode === FormMode.Default) { this.showDefault = true; }
		else { this.showDefault = false; }
	}

	changeType(e: any, replaceUrl: boolean) {
		const requiresRedirect = this.selectedReferenceListUid !== e.uid;
		this.selectedReferenceItemType = e;
		this.baseAssetTypeUid = this.selectedReferenceListUid = this.selectedReferenceItemType.uid;
		this.setSecondaryNavItems();
		if (requiresRedirect) {
			this.location.go(`/assets/${e.uid}`);
			this.assetTypeUid = e.uid;
			this.loadSelectedItem();
		}
	}

	setSecondaryNavItems() {
		this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(null, null, null, null, true, null, null));
		if (this.auditSidebar) {
			this.auditSidebar.url = `/assets/${this.selectedReferenceListUid}/log`;
		}

		if (this.impactSidebar) {
			this.impactSidebar.orderPriority = 2;
			this.impactSidebar.url = `/sidebar/visualization/impact/ReferenceItemType/${this.selectedReferenceListUid}`;
		}

		if (this.relationsSidebar) {
			this.relationsSidebar.orderPriority = 3;
			this.relationsSidebar.url = `/assets/${this.selectedReferenceListUid}/relationships`;
		}

		if (this.monitorSidebar) {
			this.monitorSidebar.url = `/assets/${this.selectedReferenceListUid}/workflowmonitor`;
		}

		if (this.authenticationService.isAdmin && this.fieldNav) {

			this.fieldNav.icons = ['fa-drivers-license-o'];
			this.fieldNav.tag = 'fields';
			this.fieldNav.title = $localize`Field Definitions`;
			this.fieldNav.orderPriority = 1;
			this.fieldNav.url = `/assets/${this.selectedReferenceListUid}/fields`;

		}

		if (this.authenticationService.isAdmin && this.ownershipSidebar) {

			this.ownershipSidebar.icons = ['fa-bars'];
			this.ownershipSidebar.tag = 'responsibilities';
			this.ownershipSidebar.title = $localize`Responsibilities`;
			this.ownershipSidebar.orderPriority = 4;
			this.ownershipSidebar.url = `/assets/${this.selectedReferenceListUid}/owners`;
		}
	}
}
