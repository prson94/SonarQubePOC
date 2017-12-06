import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SocialCommentType } from '../../models/social.model';
import { WorkflowType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { DashboardService } from '../../services/dashboard.service';
import { Dashboard } from '../../models/dashboard.model';

declare var CompanySettings;

@Component({
    selector: 'home',
    templateUrl: './home.component.html',
    providers: [ DashboardService ]
})

export class HomeComponent extends BaseComponent implements OnInit, OnDestroy {
    private showActivityDetails: boolean = false;
    private showBoardDetails: boolean = false;
    private showAssignmentDetails: boolean = false;

    private showActivityTile: boolean = true;
    private showBoardTile: boolean = true;
    private showAssignmentTile: boolean = true;
    private showTitle: boolean = false;
    private titleSize: string = '38pt';
    private titleColor: string = '#fff';
    private title: string = 'D3S';
    private backgroundImage: string = '';

    private activityDaysToLookBack: number = 7;
    private boardDaysToLookBack: number = 7;

    private selectedArtifactTypeId: number;
    private selectedArtifactTypeName: string;

    private selectedSocialType: SocialCommentType;

    private selectedWorkflowType: WorkflowType;

    private numTiles: number = 3;
    private colSize = 4;
    private hasResults = false;
    private dashboard: Dashboard = null;

    constructor(protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,        
        webAnalyticsService: WebAnalyticsService,
        protected router: Router,
        rightSidebarService: RightSidebarService,
        private dashboardService: DashboardService) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.webAnalyticsService = webAnalyticsService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Home');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Home'));
        this.clearSidebar();
        this.setCommonRightSideBar(false, false, true);

        this.showActivityTile = CompanySettings.ShowHomeActivityTile == 'true' ? true: false;
        this.showAssignmentTile = CompanySettings.ShowHomeAssignmentTile == 'true' ? true : false;;
        this.showBoardTile = CompanySettings.ShowHomeBoardTile == 'true' ? true : false;

        this.showTitle = CompanySettings.ShowHomePageTitle == 'true' ? true : false;
        this.title = CompanySettings.BrowserTitlePrefix;
        this.titleSize = CompanySettings.HomePageTitleSize;
        this.titleColor = CompanySettings.HomePageTitleColor;

        this.backgroundImage = CompanySettings.HomePageBackgroundImage;

        this.numTiles = (this.showAssignmentTile ? 1 : 0)
            + (this.showBoardTile ? 1 : 0)
            + (this.showActivityTile ? 1 : 0);

        this.colSize = 12.0 / (this.numTiles == 0 ? 1 : this.numTiles);

       // this.dashboardService.getHomePageDashboards().then(r => {
        //    if (r && r.length > 0)
         //       this.dashboard = r[0];
       // });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private onShowActivityDetails(event) {
        this.showActivityDetails = true;        
        this.showBoardDetails = false;
        this.showAssignmentDetails = false;
        
        this.selectedArtifactTypeId = event.Id;
        this.selectedArtifactTypeName = event.name;
    }

    private onShowAssignmentDetails(event) {
        if (event.workflowId)
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2}/${event.workflowId}`);
        else 
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST}/${event.workflowType}`);
    }

    private onShowBoardDetails(event) {
        if (!event.selected) {
            console.log("ERROR NO SELECTION PASSED ON BOARD DETAILS CLICK.");
            return;
        }
        switch (event.selected.Name.toUpperCase()) {            
            case "COMMENT":
                this.selectedSocialType = SocialCommentType.Social;
                break;
            case "ACTIONS":
                this.selectedSocialType = SocialCommentType.Issue;
                break;
            case "TASK":
                this.selectedSocialType = SocialCommentType.Task;
                break;
            default:
                this.selectedSocialType = undefined;
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