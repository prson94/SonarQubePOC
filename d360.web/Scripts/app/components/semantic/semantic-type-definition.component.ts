import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SemanticType } from '../../models/semantic-type.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { CompanySettingsService } from '../../services/settings.service';
import { DataProfileService } from '../../services/dataprofile.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Subscription } from 'rxjs';
import { SecondaryNavItem } from '../../models/secondaryNav.model';
import { SemanticBaseComponent } from './semantics-base.component';
import { FeatureFlagsService } from '../../services/featureflags.service';
import { AuthenticationService } from '../../services/authentication.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { IOutputData } from 'angular-split';
import { SidePanelService } from '../../services/side-panel.service';


declare var CurrentResourceID;

@Component({
    selector: 'semantic-definition',
    templateUrl: './semantic-type-definition.component.html',
    providers: [DataProfileService]
})


export class SemanticDefinitionComponent extends SemanticBaseComponent implements OnInit, OnDestroy {
   
    private semanticType: SemanticType;
    private sub: any;

    semanticDetails: SemanticType;
    semanticAssets: any[];
    showAssetsTab: boolean = true;
    tab: string = 'detail';
    navigationItemsSubs: Subscription[] = [];
    semanticAssetsCount: number;
    resourceUid: string;
    sidePanelTab: string = 'detail';
    sidePanelOpen: boolean = true;
    sidePanelLoading: boolean = false;
    sidePanelStorageKey: string;
    isAdmin: boolean = false;
    showEditor: boolean = false;

    constructor(
        private route: ActivatedRoute,
        protected router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        private sidePanelService: SidePanelService,
        private featureFlagService: FeatureFlagsService,
        private authenticationService: AuthenticationService,
        private headerActionsService: HeaderActionsService,
    ) {
        super(headerBreadcrumbService, settingsService, router, featureFlagService, secondaryNavService, webAnalyticsService);
        this.isAdmin = this.authenticationService.isAdmin;
    }



    ngOnInit() {
        this.sub = this.route.params.subscribe((params) => {
            let uid = params['semanticTypeUid'];
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
                this.sidePanelStorageKey = 'Semantic_Definition' + this.semanticType + '_' + CurrentResourceID;
                this.dataProfileService.getSemanticTypeMatchingAssets(this.semanticType.qualifier, 1, 1, this.semanticType.threshold).subscribe((result) => {
                    this.semanticAssetsCount = result.total;
                    this.displayBreadCrumbs();
                    this.isLoading = false;
                });
                this.cdRef.markForCheck();
            });
        }        
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
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
				!this.semanticType.isDisabled,
				'Semantic',
				this.semanticType.id,
				null,
				null,
				null));

            this.setBrowserTitle(this.headerBreadcrumbService.getTitleService(), this.semanticType.name);

            var breadCrumbsSub = this.headerBreadcrumbService.getFolderIcon(res).subscribe((icon) => {
                this.secondaryNavService.clearItems();
                this.secondaryNavService.clearCurrentObject();
                let disabledBadge = this.isDisabled() ? "[{\"name\":\"Disabled\", \"color\":\"#D7D8DC\"}]" : "";
                this.secondaryNavService.setCurrentArea(this.semanticType.name, icon, $localize`Definition`, [disabledBadge]);
                let assetstab = new SecondaryNavItem($localize`Assets`, null, null, `${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${this.semanticType.uid}/assets`, this.semanticAssetsCount, 2);

                this.secondaryNavService.showItem(assetstab);

                this.secondaryNavService.showHeader(true);
            });
        });
    }

    handleLinkClick($event: any) {
        if ($event?.resourceUid) {
            this.resourceUid = $event.resourceUid;
            this.sidePanelTab = 'detail';
        } else {
            this.sidePanelTab = 'status';
        }

    }

    editSemantic($event) {
        this.headerActionsService.emitFavoritesChange();    
        this.getData(this.semanticType.uid);
        this.showEditor = false;
    }

    getSidePanelWidth(): number {
        return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
    }

    getSidePanelMaxWidth(): number {
        return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
    }

    getSidePanelMinWidth(): number {
        return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
    }

    onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
        this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
    }

    isDisabled() {
        return new Date(this.semanticType.effectiveDate ) < new Date(this.semanticType.updatedOn);
    }
}