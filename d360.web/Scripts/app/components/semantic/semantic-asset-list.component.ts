import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SecondaryNavItem } from '../../models/secondaryNav.model';

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
	semanticAssetsCount: number;

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
            const uid = params['semanticTypeUid'];

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
				this.dataProfileService.getSemanticTypeMatchingAssets(this.semanticType.qualifier, 1, 1, this.semanticType.threshold).subscribe((result) => {
					this.semanticAssetsCount = result.total;
					this.displayBreadCrumbs();
					this.isLoading = false;
					this.cdRef.markForCheck();
				});
            });
        }
    }

	selectAsset(asset: SemanticTypeAsset) {
		if (!asset) {
			return;
		}
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

	displayBreadCrumbs() {
		this.headerBreadcrumbService.getFolderTitle('#SemanticTypes').then((res) => {
			this.folderTitle = res;
			this.area = res;

			this.headerBreadcrumbService.clearBreadcrumbs();
			this.headerBreadcrumbService.clearCurrentObjectInfo();
			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res, SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT));

			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(
				this.semanticType.name,
				`${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${this.semanticType.uid}`,
				false,
				'Semantic',
				this.semanticType.id,
				null,
				null,
				null));

			this.setBrowserTitle(this.headerBreadcrumbService.getTitleService(), this.semanticType.name);

			var breadCrumbsSub = this.headerBreadcrumbService.getFolderIcon(res).subscribe((icon) => {
				this.secondaryNavService.clearItems();
				this.secondaryNavService.clearCurrentObject();
				const disabledBadge = this.isDisabled() ? "[{\"name\":\"Disabled\", \"color\":\"#D7D8DC\"}]" : "";
				this.secondaryNavService.setCurrentArea(this.semanticType.name, icon, $localize`Definition`, [disabledBadge]);
				this.secondaryNavService.setLocalHomeUrl(`${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${this.semanticType.uid}`);
				const assetstab = new SecondaryNavItem($localize`Assets`, null, null, `${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${this.semanticType.uid}/assets`, this.semanticAssetsCount, 2);

				this.secondaryNavService.showItem(assetstab);

				this.secondaryNavService.showHeader(true);
			});
		});
	}

	isDisabled() {
		return new Date(this.semanticType.effectiveDate) < new Date(this.semanticType.updatedOn);
	}
}