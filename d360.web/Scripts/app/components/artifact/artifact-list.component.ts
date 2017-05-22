import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactTypeService } from '../../services/artifact-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { ArtifactType } from '../../models/artifact-type.model';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-artifact-list',
    template: `                 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" *ngIf="!isLoading">
                            <d3s-artifact-grid [artifactType]="artifactType"></d3s-artifact-grid>                                                                       
                        </div>
                    </div>
                </div>
                `,
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
        super(headerBreadcrumbService, rightSidebarService, webAnalyticsService );      
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
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.artifactType.Name, this.router.url));
                    this.clearSidebar();
                    this.setBrowserTitle(this.titleService, this.artifactType.Name);
                    this.setCommonRightSideBar(false, false, this.artifactType.HasDashboards);
                    this.setObjectInfo('ArtifactType', this.artifactType.ID);
                    this.rightSidebarService.showItem(new RightSidebarItem('Metrics', 'metrics', ['fa-bar-chart-o'], `/artifact/type/metrics/${this.artifactType.ID}`));

                    if (this.artifactType.HasV2Workflows) this.rightSidebarService.showItem(new RightSidebarItem('Workflow Monitor', 'workflowmonitor', ['fa-television'], `/sidebar/workflowmonitor${this.objectContextUrl()}`));

                    this.isLoading = false;
                });            
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }    
};