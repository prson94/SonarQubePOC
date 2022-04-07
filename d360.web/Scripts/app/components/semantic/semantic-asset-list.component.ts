import { ChangeDetectorRef, Component, OnInit} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { DataProfileService } from '../../services/dataprofile.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CompanySettingsService } from '../../services/settings.service';
import { SemanticType, SemanticTypeAsset } from '../../models/semantic-type.model';
import { LazyLoadEvent } from 'primeng/api';
import { forkJoin } from 'rxjs';
import { SemanticBaseComponent } from './semantics-base.component';
import { FeatureFlagsService } from '../../services/featureflags.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-semantic-asset-list',
    templateUrl: './semantic-asset-list.component.html',
    styleUrls: ["semanticTypes.less"],
    providers: [DataProfileService],
})

export class SemanticTypeAssetListComponent extends SemanticBaseComponent implements OnInit {

    semanticType: SemanticType;
    private sub: any;
    selectedAsset: SemanticTypeAsset;
    showSidePanel: boolean = true;
    private sidePanelOpen: boolean = false;
    sidePanelTab: string = 'detail';
    rowsPerPage: number = this.defaultInitialItemsPerPage;
    currentPageNumber: number = 1;    
    dataProfile: any;
    sidePanelLoading: boolean = false;
    sidePanelStorageKey: string;
    secondarySidePanelOpen: boolean = false;
    secondarySidePanel: string = 'detail';
    resourceUid: any;

    constructor(private route: ActivatedRoute,
        protected router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        private featureFlagService: FeatureFlagsService,) {
        super(headerBreadcrumbService, settingsService, router, featureFlagService, secondaryNavService, webAnalyticsService);
    }    

    ngOnInit() {        
        this.sub = this.route.params.subscribe((params) => {
            let uid = params['semanticTypeUid'];

            this.sidePanelStorageKey = 'SemanticTypes_' + uid + '_' + CurrentResourceID;
            this.headerBreadcrumbService.setCurrentObjectInfo('SemanticType', uid);
            this.logAction('open', 'SemanticType', uid);            
            this.getData(uid);
        });
    }

    getData(uid: string) {
        if (this.semanticTypesEnabled) {
            this.isLoading = true;
            this.dataProfileService.getSemanticTypes(1, 1, "", `uid eq '${uid}'`).subscribe((s) => {
                this.semanticType = s.items[0];
                this.isLoading = false;
                this.cdRef.markForCheck();
            });
        }
    }

    selectAsset(asset: SemanticTypeAsset) {
        this.selectedAsset = asset;
        this.sidePanelLoading = true;
        this.dataProfileService.getDataProfiles(this.selectedAsset.uid).subscribe(
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

                    this.sidePanelLoading = false;
                }
            });

    }

    lazyLoad(event: LazyLoadEvent) {
        this.rowsPerPage = event.rows;
        this.currentPageNumber = (event.first / event.rows) + 1;
        this.getData(this.semanticType.uid);
    }    

    handleSemanticLinkClick(event: any) {
        this.secondarySidePanelOpen = true;
        if (event && event.resourceUid) {            
            if (event.resourceUid) {
                this.secondarySidePanel = "user";
                this.resourceUid = event.resourceUid;
            }            
        } else {
            this.secondarySidePanel = 'status';
        }
    }
}