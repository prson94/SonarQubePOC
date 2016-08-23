///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactService, HeaderBreadcrumbService, PageHeader, RightSidebarService, WebAnalyticsService } from '../../services/index';
import { Artifact } from '../../models/artifacts.model';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactBaseComponent } from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-artifact-item',
    template: `  <div class="row" *ngIf="isLoading">
                    <div class="col s12">
                        <div>
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                    </div>
                </div>
                <d3s-ownership-tab *ngIf="!isLoading && isOwnershipVisible" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-ownership-tab>
                <d3s-lineage *ngIf="!isLoading && isLineageVisible" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-lineage>
                <d3s-dashboard-tab *ngIf="!isLoading && isDashboardVisible" [objectID]="artifactTypeId" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-dashboard-tab>
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-audit>
                <div *ngIf="!isLoading && !isTabVisible()">
                    <div class="row">
                        <div class="col s12">
                             <div class="tile tile-detail">
                                <d3s-object-governance-tile [objectType]="'Artifact'" [objectID]="artifact?.ID"></d3s-object-governance-tile>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail">
                               <!-- <object-detail [objectType]="'Artifact'" [objectID]="artifact?.ID"></object-detail> -->
                                <d3s-object-definition-tile [objectID]="artifact?.ID" [objectType]="'Artifact'"></d3s-object-definition-tile>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail">
                                <d3s-object-relationships-tile [objectType]="'Artifact'" [objectID]="artifact?.ID"></d3s-object-relationships-tile>
                            </div>
                        </div>
                    </div>
                </div>                
                `,
    providers: [ArtifactService]
})

export class ArtifactItemComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifact: Artifact
    private sub: any;    
    private isLineageVisible: boolean = false;    
    private artifactTypeId: number;
    
    constructor(private route: ActivatedRoute,
        rightSidebarService: RightSidebarService,
        private router: Router,
        private artifactService: ArtifactService,
        pageHeader: PageHeader,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        headerBreadcrumbService: HeaderBreadcrumbService) {
        super(headerBreadcrumbService, pageHeader, rightSidebarService, webAnalyticsService);

    }

    ngOnInit() {

        this.sub = this.route.params.subscribe(params => {            
            let artifactId = +params['artifactId']; // (+) converts string 'id' to a number
            this.artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            this.logAction('open', 'Artifact', artifactId);
            this.isLoading = true;
            this.artifactService.getArtifact(artifactId)
                .then(artifact => {
                    this.artifact = artifact;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    let index = 0;
                    for (let breadcrumb of this.artifact.Breadcrumbs) {
                        index++;
                        if (index == this.artifact.Breadcrumbs.length)
                            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(breadcrumb.Name, breadcrumb.Url, breadcrumb.Active, 'Artifact', this.artifactTypeId));
                        else
                            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(breadcrumb.Name, breadcrumb.Url, breadcrumb.Active));                                
                    }             
                    this.setBrowserTitle(this.titleService, this.artifact.Name);       

                    this.clearSidebar();
                    this.setCommonRightSideBar(true, true, this.artifact.HasDashboards);
                                        
                    this.rightSidebarService.showItem(new RightSidebarItem('Lineage', 'lineage'));           
                    this.isLoading = false;
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }

    protected isTabVisible() {
        return this.isAuditVisible || this.isDashboardVisible || this.isLineageVisible || this.isOwnershipVisible;
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {        
        // put logic to show hide lineage / dashboard / ownership here
        if (activatedItem.tag == 'dashboards') this.isDashboardVisible = !this.isDashboardVisible;
        else if (activatedItem.tag == 'lineage') this.isLineageVisible = !this.isLineageVisible;        
    }


};