import { Component, OnDestroy, OnInit } from "@angular/core";
import { NavigationEnd, Router } from "@angular/router";
import { Title } from "@angular/platform-browser";

import { Breadcrumb } from "../../models/breadcrumb.model";
import { DashboardModel } from "../../models/dashboard.model";

import { HeaderBreadcrumbService } from "../../services/header-breadcrumb.service";
import { SecondaryNavService } from "../../services/right-sidebar.service";
import { WebAnalyticsService } from "../../services/web-analytics.service";
import { DashboardService } from "../../services/dashboard.service";

import { SiteUrlHelpers } from "../../static/site-url-helpers";

import { CommentType } from "../../models/social.model";
import { CompanySettingsService } from "../../services/settings.service";
import { CompanySettingEnum } from "../../models/settings.model";
import { FeatureFlags } from "../../services/feature-flags.enum";
import { LaunchDarklyService } from "@precisely/prism-ng/launch-darkly";
import { BaseComponent } from "../../components/shared/base.component";

import { ActivityDetailsTile } from "./components/activity-details-tile";
import { ActivityTile } from "./components/activity-tile";
import { BoardTile } from "./components/board-tile";
import { DashboardModule } from "../../components/sidebar/dashboard/dashboard.module";
import { ShortcutDisplay } from "./components/shortcut-display";
import { SocialModule } from "../../components/shared/social/social.module";
import { UserAssignmentsModule } from "../../components/assignments/user-assignments/user-assignments.module";
import { SearchModule } from "../../components/search/search.module";

@Component({
	selector: "home",
	standalone: true,
	imports: [
		ActivityDetailsTile,
		ActivityTile,
		BoardTile,
		DashboardModule,
		SearchModule,
		ShortcutDisplay,
		SocialModule,
		UserAssignmentsModule
	],
	templateUrl: "./index.html"//,
	//providers: [DashboardService]
})
export class HomeIndex extends BaseComponent implements OnInit, OnDestroy {
	public showActivityDetails: boolean = false;
	public showBoardDetails: boolean = false;
	public showAssignmentDetails: boolean = false;

	public showActivityTile: boolean = true;
	public showBoardTile: boolean = true;
	public showAssignmentTile: boolean = true;
	public showTitle: boolean = false;
	private titleSize: string = "38pt";
	private titleColor: string = "#fff";
	private title: string = "D3S";
	public backgroundImage: string = "";

	private activityDaysToLookBack: number = 7;
	private boardDaysToLookBack: number = 7;

	private selectedArtifactTypeId: number;
	private selectedArtifactTypeName: string;

	private selectedCommentType: CommentType;

	public numTiles: number = 3;
	private colSize = 4;
	public hasResults = false;
	public dashboard: DashboardModel = null;
	private sub;

	dashboardingEnabledFeatureFlag: boolean = true;

	constructor(
		protected titleService: Title,
		protected headerBreadcrumbService: HeaderBreadcrumbService,
		webAnalyticsService: WebAnalyticsService,
		protected router: Router,
		secondaryNavService: SecondaryNavService,
		private dashboardService: DashboardService,
		protected settingsService: CompanySettingsService,
		private featureFlagService: LaunchDarklyService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.webAnalyticsService = webAnalyticsService;

		this.sub = this.router.events.subscribe((val) => {
			if (val instanceof NavigationEnd) {
				this.hasResults = false;
			}
		});

		this.dashboardingEnabledFeatureFlag = this.featureFlagService.variation<boolean>(FeatureFlags.DashboardingEnabled);
	}

	ngOnInit() {
		this.setBrowserTitle(this.titleService, "Home");

		this.headerBreadcrumbService.clearBreadcrumbs();
		this.headerBreadcrumbService.clearCurrentObjectInfo();
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Home`));

		this.clearSidebar();

		this.secondaryNavService.showHeader(false);
		this.showActivityTile = this.settingsService.getSettingById(CompanySettingEnum.ShowHomeActivityTile).BooleanSetting.Value;
		this.showAssignmentTile = this.settingsService.getSettingById(CompanySettingEnum.ShowHomeAssignmentTile).BooleanSetting.Value;
		this.showBoardTile = this.settingsService.getSettingById(CompanySettingEnum.ShowHomeBoardTile).BooleanSetting.Value;

		this.showTitle = this.settingsService.getSettingById(CompanySettingEnum.ShowHomePageTitle).BooleanSetting.Value;
		this.title = this.settingsService.getSettingById(CompanySettingEnum.BrowserTitlePrefix).StringSetting.Value;
		this.titleSize = this.settingsService.getSettingById(CompanySettingEnum.HomePageTitleSize).StringSetting.Value;
		this.titleColor = this.settingsService.getSettingById(CompanySettingEnum.HomePageTitleColor).StringSetting.Value;

		const bgImage = this.settingsService.getSettingById(CompanySettingEnum.HomePageBackgroundImage).StringSetting.Value;
		if (bgImage !== null && bgImage !== "") {
			this.backgroundImage = bgImage;
		}

		this.numTiles = (this.showBoardTile ? 1 : 0)
			+ (this.showActivityTile ? 1 : 0);

		this.colSize = 12.0 / (this.numTiles === 0 ? 1 : this.numTiles);

		if (this.dashboardingEnabledFeatureFlag) {
			this.dashboardService.getHomePageDashboards().subscribe(
				(r) => {
					if (r && r.length > 0) {
						this.dashboard = r[0];
					}
				}
			);
		}
	}

	ngOnDestroy() {
		this.clearSidebar();
		if (this.sub) {
			this.sub.unsubscribe();
		}
	}

	public onShowActivityDetails(event) {
		this.showActivityDetails = true;
		this.showBoardDetails = false;
		this.showAssignmentDetails = false;

		this.selectedArtifactTypeId = event.Id;
		this.selectedArtifactTypeName = event.name;
	}

	public onShowAssignmentDetails(event) {
		if (event.workflowId) {
			this.router.navigateByUrl(this.federateUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2}/${event.workflowId}/${event.version}/${event.stepId}`));
		}
		else {
			this.router.navigateByUrl(this.federateUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST}/${event.workflowType}`));
		}
	}

	public onShowBoardDetails(event) {
		if (!event.selected) {
			console.log("ERROR NO SELECTION PASSED ON BOARD DETAILS CLICK.");
			return;
		}
		switch (event.selected.Name.toUpperCase()) {
			case "COMMENT":
				this.selectedCommentType = CommentType.Social;
				break;
			case "OPEN ACTIONS":
				this.selectedCommentType = CommentType.Issue;
				break;
			default:
				this.selectedCommentType = null;
				break;
		}

		this.showBoardDetails = true;
		this.showAssignmentDetails = false;
		this.showActivityDetails = false;
	}

	private checkHasResults(e: any) {
		this.hasResults = (e != null);
	}
}
