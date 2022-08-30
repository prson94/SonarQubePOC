import { Component, OnInit, OnDestroy, ViewChild, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RulesService } from '../../services/rules.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleType } from '../../models/rule.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { AssetGridObject } from '../assets-grid/asset-grid.model';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { DataProfileService } from '../../services/dataprofile.service';
import { forkJoin, Subscription } from 'rxjs';
import { AssetTypeClass } from '../../models/asset.model';
import { CompanySettingsService } from '../../services/settings.service';
import { AssetGridComponent } from '../assets-grid/asset-grid.component';
import { LinkClickInterceptor } from '../../services/href-click-service';
import { SemanticType } from '../../models/semantic-type.model';
import { AssetDetailComponent } from "../shared/asset-detail/asset-detail.component";
import { SidePanelService } from '../../services/side-panel.service';
import { IOutputData } from 'angular-split';

declare var CurrentResourceID;

@Component({
	selector: 'd3s-rule-list',
	providers: [GridDefinitionService, RulesService, PermissionsService, WebAnalyticsService, DataProfileService],
	templateUrl: './rule-list.component.html'
})

export class RuleListComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() assetTypeUid: string;

	routeParamsSubscription: any;
	private currentAreaNameSubscription: any;
	private currentAreaName: string;
	gridObject: AssetGridObject;
	ruleType: RuleType;

	selection: any = null;
	showEditor: boolean = false;
	private sidePanelOpen: boolean = false;
	private sidePanelLoading: boolean = false;
	private sidePanelTab: string;
	private sidePanelStorageKey: string;
	private hasProfiling: boolean = false;
	gridLoading: boolean = true;
	definitionLoaded: boolean = false;
	dataProfile: any;

	@ViewChild('grid', { static: false }) assetGrid: AssetGridComponent;
	@ViewChild('assetDetail') assetDetail: AssetDetailComponent;

	hrefSub: Subscription;
	selectedAsset: any;
	selectedReferenceItem: any;
	selectedTag: any;
	semanticType: SemanticType;
	secondarySidePanelOpen: boolean;
	secondarySidePanel: string = "detail";
	resourceUid: string;

	constructor(private route: ActivatedRoute,
		private router: Router,
		protected rulesService: RulesService,
		protected titleService: Title,
		private sidePanelService: SidePanelService,
		protected messagesService: MessagesObservableService,
		private gridDefinitionService: GridDefinitionService,
		private headerActionsService: HeaderActionsService,
		private dataProfileService: DataProfileService,
		protected headerBreadcrumbService: HeaderBreadcrumbService,
		protected permissionsService: PermissionsService,
		secondaryNavService: SecondaryNavService,
		protected settingsService: CompanySettingsService,
		webAnalyticsService: WebAnalyticsService,
		private linkClickInterceptor: LinkClickInterceptor,
	) {
		super(settingsService);
		this.webAnalyticsService = webAnalyticsService;
		this.secondaryNavService = secondaryNavService;

		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this, ev);
		});
	}

	ngOnInit() {

		this.logAction("open", "RuleType", this.assetTypeUid);


		this.isLoading = true;
		this.rulesService.getRuleType(this.assetTypeUid)
			.subscribe(result => {
				this.isLoading = false;
				this.ruleType = result;
				this.gridObject = RuleType.AsGridObject(this.ruleType);
				this.baseAssetTypeUid = this.gridObject.AssetTypeUID;


				this.currentAreaNameSubscription =
					this.headerBreadcrumbService
						.getAreaName('RuleType', this.ruleType.ID)
						.subscribe((result) => { this.currentAreaName = result; });
				this.headerBreadcrumbService.setCurrentObjectInfo('RuleType', this.ruleType.ID, this.baseAssetTypeUid);
				this.setObjectInfo('RuleType', this.ruleType.ID);

				this.sidePanelStorageKey = 'list_' + AssetTypeClass[AssetTypeClass.Rule] + '_' + CurrentResourceID;

				this.headerBreadcrumbService.getFolderTitle('#Data Quality').then((res) => {
					this.headerBreadcrumbService.clearBreadcrumbs();
					this.headerBreadcrumbService.showBreadcrumb(
						new Breadcrumb(
							this.currentAreaName ? this.currentAreaName : res,
							`${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${SiteUrlHelpers.SITE_URL_ASSET_RULE}`
						)
					);
					this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.ruleType.Name, `assets/${this.ruleType.AssetTypeUID}`,
						undefined,
						'RuleType',
						this.ruleType.ID,
						undefined,
						undefined,
						true));

					this.headerBreadcrumbService.getAssetFolderIcon('RuleType', this.ruleType.ID, this.currentAreaName ? this.currentAreaName : res).subscribe((icon) => {
						this.secondaryNavService.setCurrentArea(this.ruleType.Name, icon, 'Rules');
						this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject('RuleType', this.ruleType.ID, this.ruleType.Name, null, true, null, this.ruleType.AssetTypeUID));
						this.setCommonSecondaryNavTabs({ hasAudit: false, hasOwnership: false, hasDashboard: this.ruleType.HasDashboards });
					});
					this.secondaryNavService.showHeader(true);
				});
				this.loadPermissions(this.permissionsService, StringConstants.ObjectRuleType, this.ruleType.ID);
				this.setBrowserTitle(this.titleService, this.ruleType.Name);
			});
	}

	selectAsset(event: any) {
		this.selection = event.row;
		this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;

		if (event.forceRefresh) {
			this.assetDetail.load();
		}

		if (this.selection && this.selection.HasProfiling) {
			this.sidePanelLoading = true;
			this.dataProfileService.getDataProfiles(this.selection.AssetUid).subscribe(
				(r) => {
					if (r && r.items && r.items.length > 0) {
						this.dataProfile = r.items[0];

						forkJoin(
							this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Structure'),
							this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Data')
						).subscribe((res) => {
							this.dataProfile['matches'] = {
								structure: res[0],
								data: res[1]
							};
						});
					}
					this.sidePanelLoading = false;
				});
		}
	}

    get panelApplies(): boolean {
        if (this.selection == null || this.sidePanelTab === 'detail') {
            return true;
        }
        if (this.selection != null && this.sidePanelTab === 'dataprofile') {
            return this.selection.HasProfiling;
        }
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

	ngOnDestroy() {
		this.clearSidebar();
		if (this.currentAreaNameSubscription) {
			this.currentAreaNameSubscription.unsubscribe();
		}
		if (this.routeParamsSubscription) {
			this.routeParamsSubscription.unsubscribe();
		}
	}

	secondaryPanelOpen(event: any) {
		this.secondarySidePanelOpen = true;
		if (event) {
			if (event.resourceUid) {
				this.secondarySidePanel = "user";
				this.resourceUid = event.resourceUid;
			}
			if (event.semanticType) {
				this.secondarySidePanel = "detail";
				this.semanticType = event.semanticType;
			}
		} else {
			this.secondarySidePanel = "status";
		}
	}
}
