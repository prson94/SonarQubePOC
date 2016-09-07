///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ResourcesService, ObjectStatisticsService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Router, ActivatedRoute } from '@angular/router';
import { Resource } from '../../models/resource.model';
import { ObjectStatistics } from '../../models/object-statistics.model';
import { WorkflowType } from '../../models/workflow.model';


//TODO: find out where this comes from
declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-item',
    templateUrl: 'scripts/app/components/resource/resource-item.component.html',
    providers: [ ResourcesService, ObjectStatisticsService ]
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

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private route: ActivatedRoute,
        private resourcesService: ResourcesService,
        private statisticsService: ObjectStatisticsService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.sub = this.route.params.subscribe(params => {
            let resourceId = +params['resourceId'];
            this.resourceId = resourceId;

            this.resourcesService.getResource(this.resourceId)
                .then(r => {
                    this.resource = r;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resource'));
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
};

enum PageMode {
    Default,
    Board,
    Followers,
    Governance,
    Assignment
}