import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ActivatedRoute, Router } from '@angular/router';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { ModelsService } from '../../services/models.service';
import { PoliciesService } from '../../services/policies.service';
import { AssetTypeClass } from '../../models/asset.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { TreeNode } from 'primeng/api';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SynonymPermission } from '../../models/artifacts.model';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';
import { LinkClickInterceptor } from '../../services/href-click-service';
import { forkJoin, Subscription } from 'rxjs';
import { SidePanelService } from '../../services/side-panel.service';
import { IOutputData } from 'angular-split';
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
	sidePanelStorageKey = 'side_panel_width_detail_';

	synonymPermission: SynonymPermission;

	constructor(
		private sidePanelService: SidePanelService,
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
				.subscribe((result) => {
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

			const TempsynonymPermission = new SynonymPermission;
			if (this.hasAddRelationshipsPermissions() || this.hasModifyRelationshipsPermissions()) {
				TempsynonymPermission.addModifySynonym = true;
			}

			if (this.hasDeleteRelationshipsPermissions()) {
				TempsynonymPermission.deleteSynonym = true;
			}
			this.synonymPermission = TempsynonymPermission;

			this.load();
		});

		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this, ev);
		});

		this.showSocialScoreBar = this.settingsService.getSettingById(CompanySettingEnum.ShowSocialScoreBar).BooleanSetting.Value;
	}

	ngOnDestroy() {
		this.clearSidebar();
	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}

	private load() {
		switch (this.assetTypeClass) {
			case AssetTypeClass.Model:
				this.modelsService.getModel(this.baseAssetTypeUid)
					.subscribe((result) => {
						this.assetType = result;
						this.loadHierarchy();
					});
				break;
			case AssetTypeClass.Policy:
				this.policiesService.getPolicyType(this.baseAssetTypeUid)
					.subscribe((result) => {
						this.assetType = result;
						this.loadHierarchy();
					});
				break;
		}
	}

	private editComplete(e: any) {
		this.load();
	}

	private buildBreadcrumb() {
		this.buildSecondaryNavigationByAssetUid(this.baseAssetUid);
	}

	private loadHierarchy(): void {
		this.assetService.getAssetHierarchy(this.baseAssetUid)
			.subscribe((result) => {
				this.preloadedTreeData = result;
				this.baseTreeNodeArray = this.buildTreeNodeArrayBase(this.preloadedTreeData, null, true);
				this.buildBreadcrumb();
			});
	}
}