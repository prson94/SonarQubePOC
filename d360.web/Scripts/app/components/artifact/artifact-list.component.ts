import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactTypeService, HeaderBreadcrumbService, RightSidebarService, ObjectActionsService, WebAnalyticsService } from '../../services/index';
import { ArtifactType } from '../../models/artifact-type.model';
import { ArtifactBaseComponent} from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-artifact-list',
    template: ` 
                <d3s-dashboard-tab *ngIf="!isLoading && isDashboardVisible" [objectID]="artifactType?.ID" [objectName]="artifactType?.Name" [objectType]="'ArtifactType'"></d3s-dashboard-tab>
                <d3s-artifact-type-metrics *ngIf="!isLoading && isMetricsVisible" [artifactType]="artifactType"></d3s-artifact-type-metrics>
                <d3s-artifact-type-workflow-status [artifactType]="artifactType" *ngIf="!isLoading && isWorkflowStatusVisible"></d3s-artifact-type-workflow-status>
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" *ngIf="!isLoading && !isDashboardVisible && !isMetricsVisible && !isWorkflowStatusVisible">
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
    private isMetricsVisible: boolean = false;
    private isWorkflowStatusVisible: boolean = false;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactTypeService: ArtifactTypeService,                
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        private objectActionsService: ObjectActionsService,
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
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.artifactType.Name, null));
                    this.setBrowserTitle(this.titleService, this.artifactType.Name);
                                        
                    this.isLoading = false;
                });
            this.objectActionsService.getObjectActions(artifactTypeId, 'ArtifactType', 'list')
                .then(actions => {                     
                    this.clearSidebar();
                    this.setCommonRightSideBar(false, false, actions.HasDashboards);
                    this.rightSidebarService.showItem(new RightSidebarItem('Metrics', 'metrics'));
                    this.rightSidebarService.showItem(new RightSidebarItem('Workflows', 'workflowstatus'));
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'metrics') this.isMetricsVisible = !this.isMetricsVisible;
        else if (activatedItem.tag == 'workflowstatus') this.isWorkflowStatusVisible = !this.isWorkflowStatusVisible;
    }

};