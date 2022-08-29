import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ActivatedRoute, Router } from '@angular/router';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { ModelsService } from '../../services/models.service';
import { PoliciesService } from '../../services/policies.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AssetTypeClass } from '../../models/asset.model';
import { StringConstants } from '../../static/string-constants';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { TreeNode } from 'primeng/api';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SynonymPermission } from '../../models/artifacts.model';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';
import { AssetDetailClickType, LinkClickInterceptor } from '../../services/href-click-service';
import { Subscription } from 'rxjs';
import { AssetService } from '../../services/asset.service';
import { ArtifactService } from '../../services/artifacts.service';

@Component({
	selector: 'd3s-hierarchy-item',
	providers: [
		ModelsService,
		PoliciesService,
		PermissionsService,
		WebAnalyticsService,
	],
	templateUrl: 'hierarchy-item.component.html'
})

export class HierarchyItemComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() assetTypeClass: AssetTypeClass;
	@Input() assetUid: string;

	treeSub: any;
	routeSub: any;
	currentAreaNameSub: any;
	currentAreaName: string;
	showSocialScoreBar: boolean;


	selected: any;
	assetType: any;
	treeNodeArray: TreeNode[] = [];
	crumbs: Breadcrumb[] = [];
	messages: MessageBarItem[] = [];

	hrefSub: Subscription;
	selectedAsset: any;
	selectedTag: any;
	selectedReferenceItem: any;

	sidePanelOpen: boolean = false;
	sidePanelStorageKey;

	synonymPermission: SynonymPermission;

	constructor(
		private route: ActivatedRoute,
		private router: Router,
		secondaryNavService: SecondaryNavService,
		protected modelsService: ModelsService,
		protected policiesService: PoliciesService,
		private assetService: AssetService,
		private artifactService: ArtifactService,
		protected titleService: Title,
		protected headerBreadcrumbService: HeaderBreadcrumbService,
		protected permissionsService: PermissionsService,
		protected settingsService: CompanySettingsService,
		webAnalyticsService: WebAnalyticsService,
		private linkClickInterceptor: LinkClickInterceptor,
	) {
		super(settingsService);

		this.webAnalyticsService = webAnalyticsService;
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerBreadcrumbService;
	}

	ngOnInit() {
		this.uid = this.baseAssetUid = this.assetUid;
		switch (this.assetTypeClass) {
			case AssetTypeClass.Model:
				this.assetTypeClass = AssetTypeClass.Model;
				this.objectName = 'Model';
				break;
			case AssetTypeClass.Policy:
				this.assetTypeClass = AssetTypeClass.Policy;
				this.objectName = 'Policy';
				break;
		}

		this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
			id => {
				//this.selectHierarchy(id);
				//this.showHierarchy(id);
			});

		this.currentAreaNameSub =
			this.headerBreadcrumbService
				.getAreaNameByUid(this.assetUid)
				.subscribe(result => {
					this.currentAreaName = result;
					if (this.assetType) {
						this.buildBreadcrumb();
					}
				});

		this.logAction("open", this.assetTypeClass.toString(), this.assetUid);

		this.artifactService.getArtifactByUid(this.assetUid)
			.subscribe((res) => {
				this.selected = res;
				this.baseAssetTypeUid = res["AssetTypeUid"];
				this.load();
			})

		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this, ev);
		});

		this.showSocialScoreBar = this.settingsService.getSettingById(CompanySettingEnum.ShowSocialScoreBar).BooleanSetting.Value;
	}

	ngOnDestroy() {
		this.clearSidebar();
	}

	private load() {
		switch (this.assetTypeClass) {
			case AssetTypeClass.Model:
				this.modelsService.getModel(this.baseAssetTypeUid)
					.subscribe(result => {
						this.assetType = result;
						this.buildBreadcrumb();
					});
				break;
			case AssetTypeClass.Policy:
				this.policiesService.getPolicyType(this.baseAssetTypeUid)
					.subscribe(result => {
						this.assetType = result;
						this.buildBreadcrumb();
					});
				break;
		}
	}

	//private editComplete(e: any) {
	//	this.load(e.ID);
	//}

	//private showHierarchy(id: number) {
	//	this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(this.object, id, this.objectTypeId));
	//	this.buildBreadcrumb();
	//}

	private buildBreadcrumb() {
		this.buildSecondaryNavigation(this.baseAssetUid);
	}


		//private selectHierarchy(selectedHierarchyId: number): Promise<void> {
		//	if (selectedHierarchyId > 0) {
		//		let selArray = this.preloadedTreeData.filter(x => x.ID == selectedHierarchyId);
		//		if (selArray.length > 0) this.selected = selArray[0];
		//		else {
		//			this.selected = (this.preloadedTreeData.length && this.preloadedTreeData.length > 0) ? this.preloadedTreeData[0] : null;
		//		}
		//	} else {
		//		this.selected = (this.preloadedTreeData.length && this.preloadedTreeData.length > 0) ? this.preloadedTreeData[0] : null;
		//	}

		//	this.assetID = this.selected.AssetID;

		//	this.baseAssetUid = this.selected.Uid;

		//	this.loadPermissions(this.permissionsService, this.object, this.selected.ID);

		//	let TempsynonymPermission = new SynonymPermission;

		//	this.loadPermissions(this.permissionsService, this.object, this.selected.ID).then((perms) => {
		//		if (this.hasAddRelationshipsPermissions() || this.hasModifyRelationshipsPermissions()) {
		//			TempsynonymPermission.addModifySynonym = true;
		//		}

		//		if (this.hasDeleteRelationshipsPermissions()) {
		//			TempsynonymPermission.deleteSynonym = true;
		//		}
		//		this.synonymPermission = TempsynonymPermission;
		//	});

		//	this.buildBreadcrumb();

		//	return Promise.resolve(null);
		//}
}