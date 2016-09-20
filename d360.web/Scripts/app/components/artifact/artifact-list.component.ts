///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactTypeService, HeaderBreadcrumbService, PageHeader, RightSidebarService, ObjectActionsService, WebAnalyticsService } from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';


@Component({
    selector: 'd3s-artifact-list',
    template: ` 
                <d3s-dashboard-tab *ngIf="!isLoading && isDashboardVisible" [objectID]="artifactType?.ID" [objectName]="artifactType?.Name" [objectType]="'ArtifactType'"></d3s-dashboard-tab>
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" *ngIf="!isLoading && !isDashboardVisible">
                            <d3s-artifact-grid [artifactType]="artifactType"></d3s-artifact-grid>                                                                       
                        </div>
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService, ObjectActionsService],
})

export class ArtifactListComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifactType: ArtifactType;
    private sub: any;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactTypeService: ArtifactTypeService,        
        pageHeader: PageHeader,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        private objectActionsService: ObjectActionsService,
        webAnalyticsService: WebAnalyticsService,
        rightSidebarService: RightSidebarService) {
        super(headerBreadcrumbService, pageHeader, rightSidebarService, webAnalyticsService );      

        
    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {
            let artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            this.isLoading = true;
            this.headerBreadcrumbService.setCurrentObjectInfo('ArtifactType', artifactTypeId);
            this.logAction('open', 'ArtifactType', artifactTypeId);
            this.artifactTypeService.getArtifactTypeDetails(artifactTypeId)
                .then(artifactType => {
                    this.artifactType = artifactType;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area, this.areaLink));   
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.artifactType.Name, null));
                    this.setBrowserTitle(this.titleService, this.artifactType.Name);
                                        
                    this.isLoading = false;
                });
            this.objectActionsService.getObjectActions(artifactTypeId, 'ArtifactType', 'list')
                .then(actions => {                     
                    this.clearSidebar();
                    this.setCommonRightSideBar(false, false, actions.HasDashboards);
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }

};