import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { ArtifactService } from '../../services/artifacts.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { SurveysService } from '../../services/surveys.service';
import { PermissionsService } from '../../services/permissions.service';
import { Artifact } from '../../models/artifacts.model';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactBaseComponent } from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-artifact-item',
    template: ` <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="artifact?.ID" [objectType]="'Artifact'" [title]="'Ownership of ' + artifact?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>    
                <div class="row"  *ngIf="!isLoading && isRelationshipsVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-relationships [objectType]="'Artifact'" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectPermissions]="permissions"></d3s-object-relationships>
                        </div>
                    </div>
                </div>      
                <div class="row" *ngIf="!isLoading && isFollowersVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">       
                            <d3s-follower-grid [objectType]="'Artifact'" [objectID]="artifact?.ID" [objectName]="artifact?.Name"></d3s-follower-grid> 
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading && isChildrenVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">       
                            <d3s-artifact-item-children [objectType]="'Artifact'" [objectID]="artifact?.ID" [objectName]="artifact?.Name"></d3s-artifact-item-children> 
                        </div>
                    </div>
                </div>                
                <d3s-lineage *ngIf="!isLoading && isLineageVisible" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-lineage>
                <d3s-impact *ngIf="!isLoading && isImpactVisible" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-impact>
                <d3s-dashboard-tab *ngIf="!isLoading && isDashboardVisible" [objectID]="artifactTypeId" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-dashboard-tab>
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [objectType]="'Artifact'"></d3s-audit>
                <div *ngIf="!isLoading && !isTabVisible()">                                    
                    <d3s-messages-bar [messages]="messages" (messageClick)="showSurvey=true" (messageClose)="showSurvey=false"></d3s-messages-bar>
                    <div class="row" *ngIf="showSurvey && surveyType">
                        <div class="col s12">
                            <div class="tile tile-detail">
                                <d3s-take-survey [surveyType]="surveyType" [objectID]="artifact?.ID" [objectType]="'Artifact'" (surveyCancel)="showSurvey=false" (surveyComplete)="completeSurvey()"></d3s-take-survey>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                             <div class="tile tile-detail" style="padding-left:0;padding-right:0;">
                                <d3s-object-governance [objectType]="'Artifact'" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [status]="artifact?.Status" [isWorkflowEnabled]="artifact?.HasWorkflow"></d3s-object-governance>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail">                               
                                <d3s-object-definition-tile [objectPermissions]="permissions" [objectID]="artifact?.ID" [objectType]="'Artifact'" [hasAttributes]="artifact?.AllowAttributes" [hasSynonyms]="artifact?.AllowSynonyms" (onEditComplete)="editArtifact($event)"></d3s-object-definition-tile>
                            </div>
                        </div>
                    </div>                    
                </div>                
                `,
    providers: [ArtifactService, SurveysService, PermissionsService]
})

export class ArtifactItemComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifact: Artifact
    private sub: any;        
    private artifactTypeId: number;
    private messages: MessageBarItem[]=[];
    private surveyType: SurveyType;
    private showSurvey: boolean = false;
    private isChildrenVisible: boolean = false;

    constructor(private route: ActivatedRoute,
        rightSidebarService: RightSidebarService,
        private router: Router,
        private artifactService: ArtifactService,        
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private surveysService: SurveysService,
        protected permissionsService: PermissionsService            
    ) {
        super(headerBreadcrumbService, rightSidebarService, webAnalyticsService);

    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {            
            let artifactId = +params['artifactId']; // (+) converts string 'id' to a number
            this.artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            this.headerBreadcrumbService.setCurrentObjectInfo('Artifact', artifactId);
            this.logAction('open', 'Artifact', artifactId);
            this.isLoading = true;
            this.messages = [];

            this.hideSidebarItems(); // this is needed if we changed artifacts.
            this.isChildrenVisible = false;

            this.loadPermissions(this.permissionsService, StringConstants.ObjectArtifact, artifactId);

            this.load(artifactId).then(() => this.isLoading = false);

           
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }

    private load(id: number): Promise<any> {
        this.messages = []; //clear any messages for this artifact
        return this.artifactService.getArtifact(id)
            .then(artifact => {
                this.artifact = artifact;
                this.headerBreadcrumbService.clearBreadcrumbs();
                let index = 0;
                for (let breadcrumb of this.artifact.Breadcrumbs) {
                    index++;
                    if (index == this.artifact.Breadcrumbs.length)
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(breadcrumb.Name, breadcrumb.Url, breadcrumb.Active, 'Artifact', this.artifactTypeId));
                    else if (index == 1) //top level link
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area, this.areaLink, breadcrumb.Active));
                    else
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(breadcrumb.Name, breadcrumb.Url, breadcrumb.Active));
                }
                this.setBrowserTitle(this.titleService, this.artifact.Name);

                this.clearSidebar();
                this.setCommonRightSideBar(true, true, this.artifact.HasDashboards, true, true, true, true);
                if (this.artifact.HasChildArtifacts) this.rightSidebarService.showItem(new RightSidebarItem('Children', 'children', ['fa-sitemap']));
                                
                this.loadItemSurvey(id);
            });
    }

    private loadItemSurvey(artifactId: number) {
        this.surveysService.getObjectSurvey(this.artifactTypeId, 'ArtifactType', artifactId, 'Artifact')        
            .then(result => {
                this.surveyType = undefined;
                if (result) {
                    this.surveyType = result;
                    this.messages.push({
                        content: `<u>Click here</u> to take the survey: <em>${result.Name}</em>.`, showClose: true, data: 'Survey'
                    });
                }

            });
    }

    protected isTabVisible() {
        return this.isAuditVisible || this.isDashboardVisible || this.isLineageVisible || this.isOwnershipVisible || this.isRelationshipsVisible || this.isFollowersVisible || this.isChildrenVisible || this.isImpactVisible;
    }

    private completeSurvey() {
        this.showSurvey = false;
        var index = this.messages.findIndex(x => x.data == 'Survey');
        if (index >= 0 && index < this.messages.length)
            this.messages.splice(index, 1);
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'children') this.isChildrenVisible = !this.isChildrenVisible;        
    }

    private editArtifact(e: any) {
        this.isLoading = true;
        this.load(e.ID).then(() => this.isLoading = false);
    }
};