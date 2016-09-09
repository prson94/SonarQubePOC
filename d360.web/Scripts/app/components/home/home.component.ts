///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, ObjectActionsService, WebAnalyticsService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SocialCommentType } from '../../models/social.model';
import { WorkflowType } from '../../models/workflow.model';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'home',
    templateUrl: 'scripts/app/components/home/home.component.html'
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
        //private objectActionsService: ObjectActionsService,
        webAnalyticsService: WebAnalyticsService,
        rightSidebarService: RightSidebarService) {
        super(rightSidebarService, webAnalyticsService);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Home');

        this.headerBreadcrumbService.clearBreadcrumbs();
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
        this.showActivityDetails = false;
        this.showBoardDetails = false;
        this.showAssignmentDetails = true;
        this.selectedWorkflowType = event.workflowType;
    }

    private onShowBoardDetails(event) {
        if (!event.selected) {
            console.log("ERROR NO SELECTION PASSED ON BOARD DETAILS CLICK.");
            return;
        }
        switch (event.selected.Name.toUpperCase()) {
            case "CHALLENGE":
                this.selectedSocialType = SocialCommentType.Challenge;
                break;
            case "COMMENT":
                this.selectedSocialType = SocialCommentType.Social;
                break;
            case "ISSUE":
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