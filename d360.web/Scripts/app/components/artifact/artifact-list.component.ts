import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';

import { ArtifactTypeService } from '../../services/artifact-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { ArtifactType } from '../../models/artifact-type.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SecondaryNavItem, SecondaryNavCurrentObject } from '../../models/secondaryNav.model';
import { Artifact } from '../../models/artifacts.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { debounce, debounceTime } from 'rxjs/operators';
import { AssetTypeClass } from '../../models/asset.model';
import { forkJoin, Subscription } from 'rxjs';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { AssetGridObject } from '../assets-grid/asset-grid.model';
import { DataProfileService } from '../../services/dataprofile.service';
import { CompanySettingsService } from '../../services/settings.service';
import { LinkClickInterceptor } from '../../services/href-click-service';
import { SemanticType } from '../../models/semantic-type.model';
import { TitleAndTabsService } from '../../services/title-and-tabs.service';
import { FeatureFlags, FeatureFlagsService } from '../../services/featureflags.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-artifact-list',
    templateUrl: './artifact-list.component.html',
    providers: [ArtifactTypeService, DataProfileService],
})

export class ArtifactListComponent extends AssetGridBaseComponent implements OnInit, OnDestroy {

    gridObject: AssetGridObject;
    artifactType: ArtifactType;
    artifactTypeHierarchy: ArtifactType[];
    sub: any;
    currentAreaNameSubscription: any;
    navigationItemsSubs: Subscription[] = [];
    currentAreaName: string;

    selection: any = null;
    showEditor: boolean = false;
    sidePanelOpen: boolean = false;
    sidePanelLoading: boolean = false;
    sidePanelTab: string;
    sidePanelStorageKey: string;
    hasProfiling: boolean = false;
    gridLoading: boolean = true;
    definitionLoaded: boolean = false;
    dataProfile: any;

    hrefSub: Subscription;
    selectedAsset: any;
    selectedReferenceItem: any;
    selectedTag: any;
    semanticType: SemanticType;
    secondarySidePanelOpen: boolean;
    secondarySidePanel: string = "detail";
    resourceUid: any;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private artifactTypeService: ArtifactTypeService,
        private titleAndTabsService: TitleAndTabsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        private linkClickInterceptor: LinkClickInterceptor,
        protected settingsService: CompanySettingsService,
        private featureFlagService: FeatureFlagsService) {
        super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.linkClickInterceptor.handleEvent(this, ev);
        });
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            let artifactTypeId = +params['artifactTypeId']; // (+) converts string 'id' to a number


            this.isLoading = true;
            this.artifactTypeHierarchy = [];
            this.headerBreadcrumbService.setCurrentObjectInfo('ArtifactType', artifactTypeId);
            this.logAction('open', 'ArtifactType', artifactTypeId);
            this
                .artifactTypeService
                .getArtifactTypeDetails(artifactTypeId, true)
                .subscribe((artifactType) => {
                    let folderName: string = '#Business';
                    this.areaLink = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`;

                    if (artifactType.Class == AssetTypeClass.TechnicalAsset) {
                        folderName = '#Technical';
                        this.areaLink = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`;
                    }

                    this.sidePanelStorageKey = 'list_' + AssetTypeClass[artifactType.Class] + '_' + CurrentResourceID;

                    this.headerBreadcrumbService.getFolderTitle(folderName).then((res) => {
                        this.headerBreadcrumbService.clearBreadcrumbs();

                        this.folderTitle = res;
                        this.area = res;

                        this.artifactType = artifactType;
                        this.gridObject = ArtifactType.AsGridObject(this.artifactType);
                        this.setObjectInfo('ArtifactType', this.artifactType.ID);

                        this.artifactTypeHierarchy.push(this.artifactType);
                        this.createBreadcrumbHierarchy(artifactType);

                        this.setBrowserTitle(this.titleService, this.artifactType.Name);
                        this.isLoading = false;
                        this.titleAndTabsService.isInitialize = true;
                    });
                });
        });
    }

    createBreadcrumbHierarchy(artifact: ArtifactType) {
        if (artifact.ParentID) {
            var detailsSub = this.artifactTypeService.getArtifactTypeDetails(artifact.ParentID).subscribe(parent => {
                this.artifactTypeHierarchy.unshift(parent);
                if (parent.ParentID)
                    this.createBreadcrumbHierarchy(parent);
                else
                    this.displayBreadcrumb();
            });

            this.navigationItemsSubs.push(detailsSub);
        } else
            this.displayBreadcrumb();
    }

    displayBreadcrumb() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.currentAreaNameSubscription =
            this.headerBreadcrumbService
                .getAreaName('ArtifactType', this.artifactTypeHierarchy[0].ID)
                .subscribe(result => {
                    this.currentAreaName = result
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : this.folderTitle, this.areaLink));
                    this.artifactTypeHierarchy.forEach(x => {
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(
                            x.Name,
                            SiteUrlHelpers.getObjectUrl("ArtifactType", x.ID),
                            false,
                            "ArtifactType",
                            x.ID,
                            null,
                            null,
                            true,
                            x.ParentID > 0));

                    });

                    var breadCrumbsSub = this.headerBreadcrumbService.getAssetFolderIcon('ArtifactType', this.artifactType.ID, this.currentAreaName ? this.currentAreaName : this.folderTitle).subscribe(res => {
                        this.setCommonSecondaryNavTabs({ hasAudit: false, hasOwnership: false, hasDashboard: this.artifactType.HasDashboards });
                        this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject('ArtifactType', this.artifactType.ID, this.artifactType.Name, null, true, null, this.artifactType.AssetTypeUID));
                        this.secondaryNavService.setCurrentArea(this.artifactType.Name, res, 'Assets');
                        if (this.artifactType.HasV2Workflows) {
                            this.secondaryNavService.showItem(
                                new SecondaryNavItem('Workflow',
                                                     'workflowmonitor',
                                                     ['fa-usb'],
                                                     `/sidebar/workflowmonitor${this.objectContextUrl()};isAdminPage=false`)
                            );
                        }
                    });
                    this.navigationItemsSubs.push(breadCrumbsSub);
                });

    }

    selectAsset(event: any) {
        this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;
        this.selection = event;

        if (this.selection && this.selection.HasProfiling && this.featureFlagService.flags[FeatureFlags.DataProfilingUiFlag]) {
            this.sidePanelLoading = true;                                  
            this.dataProfileService.getDataProfiles(this.selection.AssetUid).subscribe(
                (r) => {
                    if (r && r.items && r.items.length > 0) {
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
                    }
                    this.sidePanelLoading = false;
                });
        }
    }

    get panelApplies(): boolean {
        if (this.selection == null || this.sidePanelTab === 'detail') {
            return true;
        }
        if (this.selection != null && this.sidePanelTab === 'dataprofile' && this.featureFlagService.flags[FeatureFlags.DataProfilingUiFlag]) {
            return this.selection.HasProfiling;
        }
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }

        if (this.currentAreaNameSubscription) {
            this.currentAreaNameSubscription.unsubscribe();
        }

        if (this.navigationItemsSubs) {
            this.navigationItemsSubs.forEach((s) => {
                s.unsubscribe();
            });
        }

        if (this.hrefSub) {
            this.hrefSub.unsubscribe();
        }

        this.clearSidebar();
    }

    secondaryPanelOpen(event: any) {
        this.secondarySidePanelOpen = true;        
        if (event) {
            if (event.resourceUid) {
                this.secondarySidePanel = "user";
                this.resourceUid = event.resourceUid;
            }
            if (event.semanticType) {
                this.secondarySidePanel = "detail";
                this.semanticType = event.semanticType;
            }            
        } else {
            this.secondarySidePanel = "status";
        }
    }    
}
