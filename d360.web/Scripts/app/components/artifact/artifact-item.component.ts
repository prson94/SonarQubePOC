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

declare var CompanySettings;

@Component({
    selector: 'd3s-artifact-item',
    template: ` <d3s-loading [isLoading]="isLoading"></d3s-loading>                                                
                <div *ngIf="!isLoading">                                    
                    <d3s-messages-bar [messages]="messages" (messageClick)="showSurvey=true" (messageClose)="showSurvey=false"></d3s-messages-bar>
                    <div class="row" *ngIf="showSurvey && surveyType">
                        <div class="col s12">
                            <div class="tile tile-detail">
                                <d3s-take-survey [surveyType]="surveyType" [objectID]="artifact?.ID" [objectType]="'Artifact'" (surveyCancel)="showSurvey=false" (surveyComplete)="completeSurvey()"></d3s-take-survey>
                            </div>
                        </div>
                    </div>
                    <div class="row" *ngIf="showSocialScoreBar">
                        <div class="col s12">
                             <div class="tile tile-detail" style="padding-left:0;padding-right:0;">
                                <d3s-object-governance [objectType]="'Artifact'" [objectID]="artifact?.ID" [objectName]="artifact?.Name" [status]="artifact?.Status" [isWorkflowEnabled]="artifact?.HasWorkflow"></d3s-object-governance>
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail">                               
                                <d3s-object-definition-tile [objectPermissions]="permissions" [objectID]="artifact?.ID" [objectType]="'Artifact'" [hasAttributes]="artifact?.AllowAttributes" [nymTypes]="artifact?.NymTypes" (onEditComplete)="editArtifact($event)"></d3s-object-definition-tile>
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

    private showSocialScoreBar: boolean = true;

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
            
            this.loadPermissions(this.permissionsService, StringConstants.ObjectArtifact, artifactId);

            this.load(artifactId).then(() => this.isLoading = false);

            this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
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
                this.setBrowserTitle(this.titleService, this.artifact.DisplayValue);
                                
                this.setObjectInfo('Artifact', this.artifact.ID, this.artifact.DisplayValue);
                this.setCommonRightSideBar(true, true, this.artifact.HasDashboards, true, true, true, true, true);
                if (this.artifact.HasChildArtifacts) this.rightSidebarService.showItem(new RightSidebarItem('Children', 'children', ['fa-sitemap'], `/sidebar/children${this.objectContextUrl()}`));
                                
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
    

    private completeSurvey() {
        this.showSurvey = false;
        var index = this.messages.findIndex(x => x.data == 'Survey');
        if (index >= 0 && index < this.messages.length)
            this.messages.splice(index, 1);
    }
    
    private editArtifact(e: any) {
        this.isLoading = true;
        this.load(e.ID).then(() => this.isLoading = false);
    }
};