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
import { Artifact } from '../../models/artifacts.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { debounce, debounceTime } from 'rxjs/operators';

@Component({
    selector: 'd3s-artifact-list',
    templateUrl: './artifact-list.component.html',
    providers: [ArtifactTypeService],
})

export class ArtifactListComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifactType: ArtifactType;
    private artifactTypeHierarchy: ArtifactType[];
    private sub: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;


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
            this.artifactTypeHierarchy = [];
            this.headerBreadcrumbService.setCurrentObjectInfo('ArtifactType', artifactTypeId);
            this.logAction('open', 'ArtifactType', artifactTypeId);
            this
                .artifactTypeService
                .getArtifactTypeDetails(artifactTypeId)
                .subscribe(artifactType => {
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.artifactType = artifactType;
                    this.setObjectInfo('ArtifactType', this.artifactType.ID);

                    this.artifactTypeHierarchy.push(this.artifactType);
                    this.createBreadcrumbHierarchy(artifactType);
                    
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

    createBreadcrumbHierarchy(artifact: ArtifactType) {
        if (artifact.ParentID) {
            this.artifactTypeService.getArtifactTypeDetails(artifact.ParentID).subscribe(parent => {
                this.artifactTypeHierarchy.unshift(parent);
                if (parent.ParentID)
                    this.createBreadcrumbHierarchy(parent);
                else
                    this.displayBreadcrumb();
            });
        } else
            this.displayBreadcrumb();
    }

    displayBreadcrumb() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.currentAreaNameSubscription =
            this.headerBreadcrumbService
                .getAreaName('ArtifactType', this.artifactTypeHierarchy[0].ID)
                .subscribe(result => {
                    this.currentAreaName = result
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : this.folderTitle, this.areaLink));
                    this.artifactTypeHierarchy.forEach(x => {
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(
                            x.Name,
                            SiteUrlHelpers.getObjectUrl("ArtifactType", x.ID),
                            false,
                            "ArtifactType",
                            x.ID,
                            null,
                            null,
                            true,
                            x.ParentID > 0));
                });
        });

    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }
};
