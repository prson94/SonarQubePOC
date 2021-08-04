import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';

import { ArtifactService } from '../../services/artifacts.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { PermissionsService } from '../../services/permissions.service';
import { Artifact } from '../../models/artifacts.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from "../../static/site-url-helpers";
import { finalize } from 'rxjs/operators';
import { SiteMenuService } from '../../services/site-menu.service';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { DataProfileService } from '../../services/dataprofile.service';
import { forkJoin } from 'rxjs';

declare var CompanySettings;

@Component({
    selector: 'd3s-artifact-item',
    templateUrl: './artifact-item.component.html',
    providers: [ArtifactService, PermissionsService, SiteMenuService, DataProfileService]
})

export class ArtifactItemComponent extends AssetGridBaseComponent implements OnInit, OnDestroy {
    private artifact: Artifact
    private sub: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    private artifactTypeId: number;
    private messages: MessageBarItem[] = [];
    private showSurvey: boolean = false;
    private showSocialScoreBar: boolean = true;
    private showDataProfile: boolean = false;
    private dataProfile: any;

    constructor(
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        private router: Router,
        private artifactService: ArtifactService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        private dataProfileService: DataProfileService
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
        if (this.sub) {
            this.sub.unsubscribe();
        }
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
                    this.dataProfileService.getDataProfiles(this.artifact.Uid).subscribe(
                        (r) => {
                            if (r && r.items && r.items.length > 0 && r.items[0].totalCount != null && r.items[0].sampleCount != null) {
                                this.dataProfile = r.items[0];

                                forkJoin(
                                    this.dataProfileService.getMatchCounts(this.artifact.Uid, 'Structure'),
                                    this.dataProfileService.getMatchCounts(this.artifact.Uid, 'Data')
                                ).subscribe((res) => {
                                    this.dataProfile['matches'] = {
                                        structure: res[0],
                                        data: res[1]
                                    };
                                });

                                this.showDataProfile = true;
                            }
                        });
                },
                err => {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
                }
            )
            ;
    }

    private editArtifact(e: any) {
        this.isLoading = true;
        this.load(e.ID, this.artifactTypeId);
    }

    private showDataProfilePanel() {
        this.showDataProfile = !this.showDataProfile;
    }
}
