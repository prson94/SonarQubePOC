import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';

import { ArtifactService } from '../../services/artifacts.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { SurveysService } from '../../services/surveys.service';
import { PermissionsService } from '../../services/permissions.service';
import { Artifact } from '../../models/artifacts.model';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactBaseComponent } from './artifact-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SecondaryNavItem, SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { SurveyType } from '../../models/survey.model';
import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from "../../static/site-url-helpers";
import { Permission } from '../../models/responsibility-type.model';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { AssetTypeClass } from '../../models/asset.model';
import { SiteMenuService } from '../../services/site-menu.service';

declare var CompanySettings;

@Component({
    selector: 'd3s-artifact-item',
    templateUrl: './artifact-item.component.html',
    providers: [ArtifactService, SurveysService, PermissionsService, SiteMenuService]
})

export class ArtifactItemComponent extends ArtifactBaseComponent implements OnInit, OnDestroy {
    private artifact: Artifact
    private sub: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    private artifactTypeId: number;
    private messages: MessageBarItem[] = [];
    private surveyType: SurveyType;
    private showSurvey: boolean = false;
    private showSocialScoreBar: boolean = true;

    constructor(
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        private router: Router,
        private artifactService: ArtifactService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private surveysService: SurveysService,
        protected permissionsService: PermissionsService
    ) {
        super(headerBreadcrumbService, secondaryNavService, webAnalyticsService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            let artifactId = +params['artifactId']; // (+) converts string 'id' to a number
            this.artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            this.headerBreadcrumbService.setCurrentObjectInfo('Artifact', artifactId); 
            this.logAction('open', 'Artifact', artifactId);
            this.isLoading = true;
            this.messages = [];
            this
                .loadPermissions(this.permissionsService, StringConstants.ObjectArtifact, artifactId)
                .then(p => {
                    this.load(artifactId, this.artifactTypeId);
                }
                )
                ;

            this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
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

                    this.buildSecondaryNavigation(this.artifact.Uid);

                    this.setBrowserTitle(this.titleService, this.artifact.DisplayValue);

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
