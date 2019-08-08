import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResourcesService } from '../../services/resources.service';
import { ObjectStatisticsService } from '../../services/object-statistics.service';
import { UriBasedService } from '../../services/uri-based.service';
import { SocialService } from '../../services/social.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Resource } from '../../models/resource.model';
import { ObjectStatistics } from '../../models/object-statistics.model';
import { WorkflowType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { MessagesObservableService } from '../../services/messages-observable.service';

declare var CompanySettings;
declare var CurrentResourceID;
declare var SingleSignOn;

enum PageMode {
    Default,
    Board,
    Followers,
    Governance,
    Assignment,
    EditingInfo,
    EditingPassword,
    ViewingAPICredentials
}

@Component({
    selector: 'd3s-resource-item',
    templateUrl: './resource-item.component.html',
    providers: [ResourcesService, ObjectStatisticsService, UriBasedService, SocialService]
})

export class ResourceItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private resourceId = -1;
    private resource: Resource;
    private isMe = false;
    private totNumber = 0;
    private days = 90;
    private resourceType = ' ';

    private statistics: ObjectStatistics;
    private selectedWorkflow: WorkflowType;
    private pageMode: PageMode = PageMode.Default;
    private showResourcesLink: boolean = ((CompanySettings.ShowResources) && (CompanySettings.ShowResources.toUpperCase() == 'TRUE'));
    PageMode = PageMode;
    private allowChangePassword = !SingleSignOn;
    itemsOwn: RightSidebarItem;
    itemsFollow: RightSidebarItem;
    memberGroups: RightSidebarItem;
    comments: RightSidebarItem;
    hasRelations: RightSidebarItem;

    constructor(
        protected router: Router,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private route: ActivatedRoute,
        private resourcesService: ResourcesService,
        private statisticsService: ObjectStatisticsService,
        private uriBasedService: UriBasedService,
        private socialService: SocialService,
        rightSideBarService: RightSidebarService,
        protected messagesService: MessagesObservableService) {
        super();
        this.rightSidebarService = rightSideBarService;
    }

    ngOnInit() {
        this.isLoading = true;

        this.sub = this.route.params.subscribe(params => {
            const resourceId = +params['resourceId'];

            this.resourceId = resourceId;

            this.headerBreadcrumbService.setCurrentObjectInfo('Resource', resourceId);
            this.resourcesService.getResource(this.resourceId)
                .subscribe(r => {
                    this.resource = r;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resource', SiteUrlHelpers.SITE_URL_RESOURCE_ROOT));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.resource.FirstName} ${this.resource.LastName}`));

                    this.setBrowserTitle(this.titleService, `${this.resource.FirstName} ${this.resource.LastName}`);

                    if (this.resourceId.toString() === CurrentResourceID.toString()) {
                        this.isMe = true;
                    } else {
                        this.isMe = false;
                    }

                    this.socialService.getTheCounts(this.resourceId, this.days).subscribe(
                        k => {
                            for (let i = 0; i < k.length; i++) {
                                this.totNumber += k[i].Total;
                            }
                        }
                    );

                    this.isLoading = false;

                    this.setCommonRightSideBar(
                        false, false, false,
                        false, false, false,
                        false, false
                    );
                    this.rightSidebarService.showHeader(true);
                    this.rightSidebarService.setCurrentArea(this.resource.FirstName + " " + this.resource.LastName, 'fa-cog', 'User');
                   
                    this.itemsOwn = new RightSidebarItem(
                        'Responsibilities', 'itemOwn', ['fa-tasks'],
                        `/sidebar/itemown/${resourceId}`, null, 15
                    );
                    this.rightSidebarService.showItem(this.itemsOwn);
                    this.memberGroups = new RightSidebarItem(
                        'Groups', 'memberGroup', ['fa-user-circle'],
                        `/sidebar/membergroup/${resourceId}`,null,5
                    );
                    this.rightSidebarService.showItem(this.memberGroups);
                    this.itemsFollow = new RightSidebarItem(
                        'Following', 'itemFollow', ['fa-user-plus'],
                        `/sidebar/itemfollow/${resourceId}`, null, 20
                    );
                    this.rightSidebarService.showItem(this.itemsFollow);
                    this.hasRelations = new RightSidebarItem(
                        'Related Assets', 'hasRelations', ['fa-retweet'],
                        `/sidebar/relationships/resource/${resourceId}`, null, 10
                    );
                    this.rightSidebarService.showItem(this.hasRelations);
                    this.comments = new RightSidebarItem(
                        'Comments', 'comments', ['fa-comments'],
                        `/sidebar/comments/Resource/${resourceId}/${this.resource.FirstName}`, null, 25
                    );
                    this.rightSidebarService.showItem(this.comments);
                });

            this.pageMode = PageMode.Default;
        });
    }

    updateStatistics() {
        this.statisticsService.getObjectStatistics(this.resourceId, 'Resource').subscribe(
            s => {
                this.statistics = s;
            }
        );
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    showAssignment(e: any) {
        if (e.resourceID > 0) {
            if (e.workflowId)
                this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2}/${e.workflowId}/${e.version}/${e.stepId};resourceID=${e.resourceID}`);
            else
                this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST}/${e.workflowType};resourceID=${e.resourceID}`);
        }
        else {
            if (e.workflowId)
                this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2}/${e.workflowId}/${e.version}/${e.stepId}`);
            else
                this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST}/${e.workflowType}`);
        }
    }

    action(e: string) {
        switch (e) {
            case 'edit':
                this.pageMode = PageMode.EditingInfo;
                break;
            case 'password':
                this.pageMode = PageMode.EditingPassword;
                break;
            case 'api':
                this.pageMode = PageMode.ViewingAPICredentials;
                break;
            default:
                this.pageMode = PageMode.Default;
                break;
        }
    }

    save(e: any) {
        const values = e.item;
        values.ID = -1;

        this.uriBasedService.saveItem(null, 'form/dynamicedit/edit/resourceself', values)
            .subscribe(
                result => {
                    this.pageMode = PageMode.Default;
                    this.showMessageForResult(this.messagesService, result);
                }
            );
    }
}
