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
import { ObjectStatistics } from '../../models/object-statistics.model';
import { WorkflowType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SecondaryNavItem } from '../../models/secondaryNav.model';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { ResourceApiModel } from '../../models/resource.model';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';

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
    NotFound
}

@Component({
    selector: 'd3s-resource-item',
    templateUrl: './resource-item.component.html',
    providers: [ResourcesService, ObjectStatisticsService, UriBasedService, SocialService]
})

export class ResourceItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private resourceId = -1;
    private resourceUid = "";
    private items: any[] = [];
    private resource: any;
    isSavingProcess: boolean = false;
    private isMe = false;
    showAllUsersAPIKey = false;
    private totNumber = 0;
    private days = 90;
    private resourceType = ' ';

    isApiKeysPopupVisible = false;

    private statistics: ObjectStatistics;
    private selectedWorkflow: WorkflowType;
    private pageMode: PageMode = PageMode.Default;
    private showResourcesLink: boolean = false;
    PageMode = PageMode;
    private allowChangePassword = !SingleSignOn;
    itemsOwn: SecondaryNavItem;
    itemsFollow: SecondaryNavItem;
    memberGroups: SecondaryNavItem;
    comments: SecondaryNavItem;
    hasRelations: SecondaryNavItem;

    constructor(
        protected router: Router,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private route: ActivatedRoute,
        private resourcesService: ResourcesService,
        private statisticsService: ObjectStatisticsService,
        private uriBasedService: UriBasedService,
        protected settingsService: CompanySettingsService,
        private socialService: SocialService,
        secondaryNavService: SecondaryNavService,
        protected messagesService: MessagesObservableService) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    ngOnInit() {
        this.showResourcesLink = this.settingsService.getSettingById(CompanySettingEnum.ShowResources).BooleanSetting.Value;
        this.isLoading = true;

        this.sub = this.route.params.subscribe(params => {
            const resourceId = +params['resourceId'];

            this.resourceId = resourceId;

            this.resourcesService.getResource(this.resourceId)
                .subscribe(r => {
                    this.items = r.items;
                    if (this.items.length > 0) {
                        this.resource = this.items[0];
                    }

                    if (!this.resource || this.resource.State != 'Active') {
                        this.isLoading = false;
                        this.pageMode = PageMode.NotFound;
                        return;
                    }

                    this.resourceUid = this.resource.uid;
                    let showApi = this.settingsService.getSettingById(CompanySettingEnum.ShowAllUsersAPIKey).BooleanSetting.Value;
                    this.showAllUsersAPIKey = (this.resource.IsAdministrator || showApi);

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

                    this.buildSecondaryNavigation(this.resource.uid);

                    window.setTimeout(() => {
                        this.isLoading = false;
                        this.pageMode = PageMode.Default;
                    }, 100);
                });

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
        if (this.sub) {
            this.sub.unsubscribe();
        }
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
                this.isApiKeysPopupVisible = true;
                break;
            default:
                this.pageMode = PageMode.Default;
                break;
        }
    }

    save(e: any) {
        const user = new ResourceApiModel;
        user.FirstName = e.item.FirstName;
        user.LastName = e.item.LastName;
        user.uid = this.resource.uid;
        user.State = this.resource.State;
        user.Username = this.resource.Email;
        user.IsAdministrator = this.resource.IsAdministrator;

        user.Fields = new Object();

        // handle dynamic fields
        for (let key in e.item) {
            if (key != 'Email' && key != 'FirstName' && key != 'LastName' && key != 'IsAdministrator' && key != 'State' && key != 'ID' && key != 'Password' && key != 'uid' && key != 'ResourceID' && key != 'LastLoggedInOn') {
                user.Fields[key] = e.item[key];
            }
        }
        this.isLoading = true;
        this.isSavingProcess = true;
        this.resourcesService.saveResource(user, true, false)
            .subscribe(
                result => {
                    this.isLoading = false;
                    this.isSavingProcess = false;
                    if (result.Message == "" && result.Success) {
                        result.Message = $localize`Info successfully updated.`;
                    }
                    this.showMessageForApiResult(this.messagesService, result, $localize`Info successfully updated.`);
                    this.pageMode = PageMode.Default;
                }
            )
    }
}
