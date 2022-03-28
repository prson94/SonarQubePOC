import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { BrandingService, Theme } from '../../../services/branding.service';
import { FeatureFlags, FeatureFlagsService } from '../../../services/featureflags.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { StringConstants } from '../../../static/string-constants';
import { AdminBaseComponent } from '../admin-base.component';

@Component({
    selector: "admin-branding",
    templateUrl: "admin-branding.component.html",
    encapsulation: ViewEncapsulation.None,
    providers: [BrandingService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./admin-branding.component.less"]
})

export class AdminBrandingComponent extends AdminBaseComponent implements OnInit {
    themes: Theme[] = [];

    sidePanelOpen: boolean = false;
    sidePanelLoading: boolean = false;
    sidePanelStorageKey: string;
    sidePanelTab: string = 'detail';
    selectedRow: any;

    isEditorVisible: boolean = false;

    menuItems = [
        { title: 'Edit' },
        { title: 'Delete' },
    ];


    constructor(
        private brandingService: BrandingService,
        protected router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        featureFlagService?: FeatureFlagsService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);

        if (!featureFlagService.flags[FeatureFlags.BrandingThemeUiTemp]) {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
        }
        this.areaName = StringConstants.Section_Branding;
        this.setCommonItems();
    }

    ngOnInit() {
        this.brandingService.getThemes().subscribe((res) => {
            this.themes = res;
            console.log(this.themes);
            this.cdRef.markForCheck();
        })
    }

    clickMenuItem($event, item) {

    }
}