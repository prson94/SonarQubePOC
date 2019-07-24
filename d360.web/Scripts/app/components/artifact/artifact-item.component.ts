import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { Title } from '@angular/platform-browser';

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
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from "../../static/site-url-helpers";
import { Permission } from '../../models/responsibility-type.model';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';

declare var CompanySettings;

@Component({
    selector: 'd3s-artifact-item',
    templateUrl:'./artifact-item.component.html',
    providers: [ArtifactService, SurveysService, PermissionsService]
})

export class ArtifactItemComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifact: Artifact
    private sub: any; 
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    private artifactTypeId: number;
    private messages: MessageBarItem[]=[];
    private surveyType: SurveyType;
    private showSurvey: boolean = false;
    private showSocialScoreBar: boolean = true;

    constructor(
        private route: ActivatedRoute,
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
            this.currentAreaNameSubscription =
                this.headerBreadcrumbService
                    .getAreaName('ArtifactType', this.artifactTypeId)
                    .subscribe(result => { this.currentAreaName = result; if (this.artifact) this.buildBreadcrumb(); });

            this
                .loadPermissions(this.permissionsService, StringConstants.ObjectArtifact, artifactId)
                .then(p => {
                    this.load(artifactId, this.artifactTypeId)
                    }
                )
            ;
            
            this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.currentAreaNameSubscription.unsubscribe();
        this.clearSidebar();
    }

    private load(id: number, typeID: number) {
        this.messages = []; /* clear any messages for this artifact */
        this
            .artifactService
            .getArtifact(id)
            .pipe(
                finalize(() => {
                    this.isLoading = false;
                })
            )
            .subscribe(
                artifact => {
                    this.artifact = artifact;
                    this.buildBreadcrumb();
                    this.setBrowserTitle(this.titleService, this.artifact.DisplayValue);
                    this
                        .setObjectInfo(
                            'Artifact',
                            this.artifact.ID,
                            this.artifact.DisplayValue,
                            this.artifact.AssetID,
                            this.artifact.AssetTypeID,
                            this.artifact.Uid
                        )
                    ;
                    
                    this
                        .setCommonRightSideBar(
                            true,
                            this.hasPermission(Permission.ReadResponsibilities),
                            this.artifact.HasDashboards,
                            true,
                            true,
                            this.hasPermission(Permission.ReadRelationships),
                            true,
                            true
                        )
                        ;
                    this.rightSidebarService.setCurrentObject("ArtifactType", typeID, "Artifact", id, false, artifact.HasWorkflow);
                    if (this.artifact.HasChildArtifacts) {
                        this
                            .rightSidebarService
                            .showItem(
                                new RightSidebarItem(
                                    'Children',
                                    'children',
                                    ['fa-sitemap'],
                                    `/sidebar/children${this.objectContextUrl()}`
                                )
                            )
                        ;
                    }
                    this.rightSidebarService.showItem(
                        new RightSidebarItem(
                            'Scoring',
                            'Scoring',
                            ['fa-sitemap'],
                            `/sidebar/score/Artifact/${this.artifact.Uid}`

                        )
                    );
                    this.rightSidebarService.showItem(
                        new RightSidebarItem(
                            'Comments', 'Comments', ['fa-comments'],
                            `/sidebar/comments/Artifact/${this.artifact.ID}/${this.artifact.DisplayValue.replace("/", "%2F")}`
                        )
                    );
                    this.rightSidebarService.showItem(
                        new RightSidebarItem(
                            'Actions', 'Actions', null,
                            `/sidebar/actions/Artifact/${this.artifact.ID}/${this.artifact.DisplayValue.replace("/", "%2F")}`
                        )
                    );
                    this.loadItemSurvey(id);
                },
                err => {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
                }
            )
        ;
    }

    private loadItemSurvey(artifactId: number) {
        this
            .surveysService
            .getObjectSurvey(
                this.artifactTypeId,
                'ArtifactType',
                artifactId, 'Artifact'
        )
            .subscribe(result => {
                this.surveyType = undefined;
                
                if (result) {
                    this.surveyType = result;
                    this.messages.push({
                        content: `<u>Click here</u> to take the survey: <em>${result.Name}</em>.`, showClose: true, data: 'Survey'
                    });
                }

            }); 
    }

    private buildBreadcrumb() {
        let index = 0;
        let currentFolderName = this.currentAreaName ? this.currentAreaName : this.folderTitle;
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.getFolderIcon(currentFolderName).then(res => {
            this.rightSidebarService.setCurrentArea(this.artifact.DisplayValue, res, 'Definition');
        });
        let areaBreadcrumb = new Breadcrumb(
            this.currentAreaName ? this.currentAreaName : this.folderTitle,
            this.areaLink,
            false
        );
        this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);

        for (let breadcrumb of this.artifact.Breadcrumbs) {
            index++;

            if (index == this.artifact.Breadcrumbs.length) {
                //last item in the breadcrumb
                this
                    .headerBreadcrumbService
                    .showBreadcrumb(
                        new Breadcrumb(
                            breadcrumb.Name,
                            breadcrumb.Url,
                            false,
                            'Artifact',
                            this.artifactTypeId,
                            null,
                            null,
                            false,
                            breadcrumb.TypeName !== undefined,
                            breadcrumb.TypeName,
                            'ArtifactType',
                            this.GetIDFromUrl(breadcrumb.TypeUrl),
                            breadcrumb.TypeUrl
                        )
                    )
                    ;
            } else {
                this
                    .headerBreadcrumbService
                    .showBreadcrumb(
                        new Breadcrumb(
                            breadcrumb.Name,
                            breadcrumb.Url,
                            false,
                            'Artifact',
                            this.GetIDFromUrl(breadcrumb.Url),
                            null,
                            null,
                            false,
                            breadcrumb.TypeName !== undefined,
                            breadcrumb.TypeName,
                            'ArtifactType',
                            this.GetIDFromUrl(breadcrumb.TypeUrl),
                            breadcrumb.TypeUrl
                        )
                    )
                    ;
            }
        }
    }

    private GetIDFromUrl(url: string) {
        return +url.split("/")[url.split.length - 1];
    }
    private completeSurvey() {
        this.showSurvey = false;
        var index = this.messages.findIndex(x => x.data == 'Survey');

        if (index >= 0 && index < this.messages.length) {
            this.messages.splice(index, 1);
        }
    }
    
    private editArtifact(e: any) {
        this.isLoading = true;
        this.load(e.ID, this.artifactTypeId);
    }
};
