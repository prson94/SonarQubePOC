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

@Component({
    selector: 'home',
    templateUrl: './home.component.html'
})

export class HomeComponent extends BaseComponent implements OnInit, OnDestroy {
    private showActivityDetails: boolean = false;
    private showBoardDetails: boolean = false;
    private showAssignmentDetails: boolean = false;

    private activityDaysToLookBack: number = 7;
    private boardDaysToLookBack: number = 7;

    private selectedArtifactTypeId: number;
    private selectedArtifactTypeName: string;

    private selectedSocialType: SocialCommentType;

    private selectedWorkflowType: WorkflowType;

    constructor(protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,        
        webAnalyticsService: WebAnalyticsService,
        protected router: Router,
        rightSidebarService: RightSidebarService) {
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
}