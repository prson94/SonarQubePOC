import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { SemanticType } from '../../models/semantic-type.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { CompanySettingsService } from '../../services/settings.service';
import { DataProfileService } from '../../services/dataprofile.service';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Subscription } from 'rxjs';
import { SecondaryNavItem } from '../../models/secondaryNav.model';


@Component({
    selector: 'semantic-definition',
    templateUrl: './semantic-type-definition.component.html',
    providers: [DataProfileService]
})


export class SemanticDefinitionComponent extends AssetGridBaseComponent implements OnInit, OnDestroy {
   
    private semanticType: SemanticType
    private sub: any;

    semanticDetails: SemanticType;
    semanticAssets: any[];
    showAssetsTab: boolean = true;
    tab: string = 'detail';
    navigationItemsSubs: Subscription[] = [];


    constructor(
        private route: ActivatedRoute,
        private router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);
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
        this.isLoading = true;
        this.dataProfileService.getSemanticTypes(1, 1, "", `uid eq '${uid}'`).subscribe((s) => {
            this.semanticType = s.items[0];            
            this.displayBreadCrumbs();
            this.isLoading = false;
            this.cdRef.markForCheck();
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }        
    }

    displayBreadCrumbs() {
        this.headerBreadcrumbService.getFolderTitle('#SemanticTypes').then(res => {
            this.folderTitle = res;
            this.area = res;

            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res, SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT));
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.semanticType.name, `${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${this.semanticType.uid}`, null, null, null, null, null, null));

            this.setBrowserTitle(this.headerBreadcrumbService.getTitleService(), this.semanticType.name);

            var breadCrumbsSub = this.headerBreadcrumbService.getFolderIcon(res).subscribe((icon) => {
                this.secondaryNavService.clearItems();
                this.secondaryNavService.clearCurrentObject();
                this.secondaryNavService.setCurrentArea(this.semanticType.name, icon, 'Definition');
                let assetstab = new SecondaryNavItem("Assets", "abc", ["123"], `${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${this.semanticType.uid}/assets`, 10 ,2);

                this.secondaryNavService.showItem(assetstab);

                this.secondaryNavService.showHeader(true);
            });
        });
    }
}