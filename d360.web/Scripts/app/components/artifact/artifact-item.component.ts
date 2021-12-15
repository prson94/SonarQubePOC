import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';

import { ArtifactService } from '../../services/artifacts.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { PermissionsService } from '../../services/permissions.service';
import { Artifact, SynonymPermission } from '../../models/artifacts.model';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from "../../static/site-url-helpers";
import { finalize } from 'rxjs/operators';
import { SiteMenuService } from '../../services/site-menu.service';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { DataProfileService } from '../../services/dataprofile.service';
import { forkJoin, Subscription } from 'rxjs';
import { AssetTypeClass } from '../../models/asset.model';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';
import { AssetDetailClickType, HrefClickService } from '../../services/href-click-service';
import { AssetService } from '../../services/asset.service';

declare var CurrentResourceID;

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
    private dataProfileList: any[];
    private sidePanelOpen: boolean = false;
    private sidePanelStorageKey;
    private synonymPermission: SynonymPermission;
    hrefSub: Subscription;
    selectedAsset: any;
    selectedReferenceItem: any;

    constructor(
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        private router: Router,
        private artifactService: ArtifactService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        private dataProfileService: DataProfileService,
        protected settingsService: CompanySettingsService,
        private hrefClickService: HrefClickService,
        private assetService: AssetService
    ) {
        super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            let artifactId = +params['artifactId']; // (+) converts string 'id' to a number
            this.artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number
            this.headerBreadcrumbService.setCurrentObjectInfo('Artifact', artifactId);
            this.logAction('open', 'Artifact', artifactId);
            this.isLoading = true;
            this.messages = [];
            this.loadPermissions(this.permissionsService, StringConstants.ObjectArtifact, artifactId)
                .then(p => {
                    this.load(artifactId, this.artifactTypeId);
                });

            this.hrefSub = this.hrefClickService.getEvents().subscribe(ev => {
                this.selectedAsset = null;
                this.selectedReferenceItem = null;
                if (ev.type === AssetDetailClickType.Asset) {
                    this.selectedAsset = { uid: ev.uid, type: ev.objectType };
                }

                if (ev.type === AssetDetailClickType.ReferenceItem) {
                    this.selectedReferenceItem = { uid: ev.assetTypeUid, type: ev.objectType };
                }
                console.log(ev);
            });

            this.showSocialScoreBar = this.settingsService.getSettingById(CompanySettingEnum.ShowSocialScoreBar).BooleanSetting.Value;
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
        if (this.hrefSub) {
            this.hrefSub.unsubscribe();
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
                    this.synonymPermission = artifact.SynonymPermission;

                    this.buildSecondaryNavigation(this.artifact.Uid, null, null, null, null, null, null, this.artifact.DisplayValue);

                    this.sidePanelStorageKey = 'detail_' + AssetTypeClass[artifact.Class] + '_' + CurrentResourceID;

                    this.setBrowserTitle(this.titleService, this.artifact.DisplayValue);
                    let startDate = new Date();
                    startDate.setFullYear(startDate.getUTCFullYear() - 100);
                    this.dataProfileService.getDataProfiles(this.artifact.Uid, startDate).subscribe(
                        (r) => {
                            if (r && r.items && r.items.length > 0) {
                                this.dataProfileList = r.items;
                                this.dataProfile = r.items[0];

                                forkJoin(
                                    this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Structure'),
                                    this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Data')
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


    sidePanelTab: string = '';
    get showSidePanel() {
        return this.showDataProfile || true;
    }
}
