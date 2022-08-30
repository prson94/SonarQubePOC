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
import { forkJoin, Subscription } from 'rxjs';
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
				this.objectType = 'Taxonomy';
				this.objectName = 'Model';
				break;
			case AssetTypeClass.Policy:
				this.assetTypeClass = AssetTypeClass.Policy;
				this.objectName = 'Policy';
				this.objectType = 'Policy';
				break;
		}

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
		this.baseAssetUid = this.assetUid;
		forkJoin(this.artifactService.getArtifactByUid(this.assetUid)
			, this.permissionsService.getAssetPermissions(this.assetUid)
		).subscribe((res) => {
			this.objectPermission = res[1];
			this.selected = res[0];
			this.baseAssetTypeUid = this.selected["AssetTypeUid"];

			let TempsynonymPermission = new SynonymPermission;
			if (this.hasAddRelationshipsPermissions() || this.hasModifyRelationshipsPermissions()) {
				TempsynonymPermission.addModifySynonym = true;
			}

			if (this.hasDeleteRelationshipsPermissions()) {
				TempsynonymPermission.deleteSynonym = true;
			}
			this.synonymPermission = TempsynonymPermission;

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

	private editComplete(e: any) {
		this.load();
	}

	private showHierarchy(id: number) {
		window.alert("showHierarchy");
		this.router.navigateByUrl("asset/");
		this.buildBreadcrumb();
	}

	private buildBreadcrumb() {
		this.buildSecondaryNavigationByAssetUid(this.baseAssetUid);
	}
}