import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';

import { ArtifactTypeService } from '../../services/artifact-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { ArtifactType } from '../../models/artifact-type.model';
import { ArtifactBaseComponent } from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-artifact-list',
    templateUrl: './artifact-list.component.html',
    providers: [ArtifactTypeService],
})

export class ArtifactListComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifactType: ArtifactType;
    private sub: any;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactTypeService: ArtifactTypeService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        rightSidebarService: RightSidebarService) {
        super(headerBreadcrumbService, rightSidebarService, webAnalyticsService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            let artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            this.isLoading = true;
            this.headerBreadcrumbService.setCurrentObjectInfo('ArtifactType', artifactTypeId);
            this.logAction('open', 'ArtifactType', artifactTypeId);
            this
                .artifactTypeService
                .getArtifactTypeDetails(artifactTypeId)
                .subscribe(artifactType => {
                    this.artifactType = artifactType;
                    this.setObjectInfo('ArtifactType', this.artifactType.ID);
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area, this.areaLink));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.artifactType.Name, this.router.url));
                    this.clearSidebar();
                    this.setBrowserTitle(this.titleService, this.artifactType.Name);
                    this.setCommonRightSideBar(false, false, this.artifactType.HasDashboards);

                    if (this.artifactType.HasV2Workflows) {
                        this
                            .rightSidebarService
                            .showItem(
                                new RightSidebarItem(
                                    'Workflow',
                                    'workflowmonitor',
                                    ['fa-usb'],
                                    `/sidebar/workflowmonitor${this.objectContextUrl()}`
                                )
                            )
                            ;
                    }

                    this.isLoading = false;
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }
};
