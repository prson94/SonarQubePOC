import {Component, OnDestroy, OnInit} from "@angular/core";
import {NavigationEnd, Router} from "@angular/router";
import {Title} from "@angular/platform-browser";

import {Breadcrumb} from "../../models/breadcrumb.model";
import {WorkflowType} from "../../models/workflow.model";
import {Dashboard} from "../../models/dashboard.model";

import {HeaderBreadcrumbService} from "../../services/header-breadcrumb.service";
import {SecondaryNavService} from "../../services/right-sidebar.service";
import {WebAnalyticsService} from "../../services/web-analytics.service";
import {DashboardService} from "../../services/dashboard.service";

import {SiteUrlHelpers} from "../../static/site-url-helpers";

import {BaseComponent} from "../shared/base.component";
import { CommentType } from "../../models/social.model";
import { CompanySettingsService } from "../../services/settings.service";
import { CompanySettingEnum } from "../../models/settings.model";

@Component({
    selector: "home",
    templateUrl: "./home.component.html",
    providers: [DashboardService]
})

export class HomeComponent extends BaseComponent implements OnInit, OnDestroy {
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

    private selectedWorkflowType: WorkflowType;

    public numTiles: number = 3;
    private colSize = 4;
    public hasResults = false;
    public dashboard: Dashboard = null;    
    private sub;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        webAnalyticsService: WebAnalyticsService,
        protected router: Router,
        secondaryNavService: SecondaryNavService,
        private dashboardService: DashboardService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.webAnalyticsService = webAnalyticsService;

        this.sub = this.router.events.subscribe((val) => {
            if (val instanceof NavigationEnd) {
                this.hasResults = false;
            }
        });
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, "Home");

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Home"));

        this.clearSidebar();

        this.secondaryNavService.showHeader(false);
        this.showActivityTile = this.settingsService.getSettingById(CompanySettingEnum.ShowHomeActivityTile).BooleanSetting.Value;
        this.showAssignmentTile = this.settingsService.getSettingById(CompanySettingEnum.ShowHomeAssignmentTile).BooleanSetting.Value;
        this.showBoardTile = this.settingsService.getSettingById(CompanySettingEnum.ShowHomeBoardTile).BooleanSetting.Value;

        this.showTitle = this.settingsService.getSettingById(CompanySettingEnum.ShowHomePageTitle).BooleanSetting.Value;
        this.title = this.settingsService.getSettingById(CompanySettingEnum.BrowserTitlePrefix).StringSetting.Value;
        this.titleSize = this.settingsService.getSettingById(CompanySettingEnum.HomePageTitleSize).StringSetting.Value;
        this.titleColor = this.settingsService.getSettingById(CompanySettingEnum.HomePageTitleColor).StringSetting.Value;

        let bgImage = this.settingsService.getSettingById(CompanySettingEnum.HomePageBackgroundImage).StringSetting.Value;
        if (bgImage != null && bgImage != "") {
            this.backgroundImage = bgImage;
        }
        else {
            this.backgroundImage = "/content/images/home.background.new.jpg";
        }

        this.numTiles = (this.showAssignmentTile ? 1 : 0)
            + (this.showBoardTile ? 1 : 0)
            + (this.showActivityTile ? 1 : 0);

        this.colSize = 12.0 / (this.numTiles == 0 ? 1 : this.numTiles);

        this.dashboardService.getHomePageDashboards().subscribe(
            r => {
                if (r && r.length > 0) {
                    this.dashboard = r[0];
                }
            }
        );

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
        if (event.workflowId)
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2}/${event.workflowId}/${event.version}/${event.stepId}`);
        else
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST}/${event.workflowType}`);
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
