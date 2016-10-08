import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ResourcesService, ObjectStatisticsService, UriBasedService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Router, ActivatedRoute } from '@angular/router';
import { Resource } from '../../models/resource.model';
import { ObjectStatistics } from '../../models/object-statistics.model';
import { WorkflowType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';


declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-item',
    templateUrl: './resource-item.component.html',
    providers: [ResourcesService, ObjectStatisticsService, UriBasedService ]
})

export class ResourceItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private resourceId = -1;
    private resource: Resource;
    private isMe = false;
    private statistics: ObjectStatistics;
    private selectedWorkflow: WorkflowType;
    private pageMode: PageMode = PageMode.Default;
    PageMode = PageMode;
    private actions: any[] = [];

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private route: ActivatedRoute,
        private resourcesService: ResourcesService,
        private statisticsService: ObjectStatisticsService,
        private uriBasedService: UriBasedService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;

        this.actions = [];

        this.actions.push({
            icon: 'pencil',
            title: 'edit info',
            key: 'edit'
        });

        this.actions.push({
            icon: 'key',
            title: 'view api credentials',
            key: 'api'
        });

        this.actions.push({
            icon: 'asterisk',
            title: 'change password',
            key: 'password'
        });



        this.sub = this.route.params.subscribe(params => {
            let resourceId = +params['resourceId'];
            this.resourceId = resourceId;

            this.headerBreadcrumbService.setCurrentObjectInfo('Resource', resourceId);
            this.resourcesService.getResource(this.resourceId)
                .then(r => {
                    this.resource = r;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resource', SiteUrlHelpers.SITE_URL_RESOURCE_ROOT));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.resource.FirstName} ${this.resource.LastName}`));

                    this.setBrowserTitle(this.titleService, `${this.resource.FirstName} ${this.resource.LastName}`);
                    if (this.resourceId.toString() === CurrentResourceID.toString())
                        this.isMe = true;
                    else
                        this.isMe = false;
                    this.isLoading = false;
                });
            this.pageMode = PageMode.Default;
            this.updateStatistics();




        });
    }

    updateStatistics() {
        this.statisticsService.getObjectStatistics(this.resourceId, 'Resource')
            .then(s => {
                this.statistics = s;
            });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    showAssignment(e: any) {
        this.selectedWorkflow = e.workflowType;
        this.pageMode = PageMode.Assignment;
    }

    action(e: any) {
        switch (e.key) {
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
        let values = e.item;
        values.ID = -1;

        this.uriBasedService.saveItem(null, "form/dynamicedit/edit/resourceself", values)
            .then(result => {
                this.pageMode = PageMode.Default;
            });
    }

    savePass(e: any) {
        let values = e.item;
        values.ID = -1;

        this.uriBasedService.saveItem(null, "form/dynamicedit/edit/resourceselfpassword", values)
            .then(result => {
                this.pageMode = PageMode.Default;
            });
    }
};

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