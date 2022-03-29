import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { DomSanitizer, Title } from '@angular/platform-browser';
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
    sidePanelStorageKey: string = "gov-branding-side-panel";
    sidePanelTab: string = 'detail';
    selectedRow: Theme;
    defaultThemeUid: string = 'AAAAAAAA-0000-0000-0000-000000000001';

    isEditorVisible: boolean = false;
    showDelete: boolean = false;
    deleteInProgress: boolean = false;

    isSetCurrentThemeVisible: boolean = false;
    settingCurrentThemeInProgress: boolean = false;

    menuItemsDefaultOptions = [
        { title: 'Download' },
        { title: 'Edit' },
        { title: 'Duplicate' },
        { title: 'Set as Current Theme' },
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
        public sanitizer: DomSanitizer,
        featureFlagService?: FeatureFlagsService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);

        if (!featureFlagService.flags[FeatureFlags.BrandingThemeUiTemp]) {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
        }
        this.areaName = StringConstants.Section_Branding;
        this.setCommonItems();
    }

    isDefaultTheme(theme: Theme): boolean {
        return theme.uid.toLowerCase() === this.defaultThemeUid.toLowerCase();
    }

    hasDownloadOption(theme: Theme): boolean {
        return this.isDefaultTheme(theme) ? false : true;
    }

    hasEditOption(theme: Theme): boolean {
        return this.isDefaultTheme(theme) ? false : true;
    }

    hasDuplicateOption(theme: Theme): boolean {
        return true;
    }

    hasSetAsCurrentThemeOption(theme: Theme): boolean {
        return theme.isCurrent ? false : true;
    }

    hasDeleteOption(theme: Theme): boolean {
        return theme.isCurrent || this.isDefaultTheme(theme) ? false : true;
    }

    ngOnInit() {
        this.brandingService.getThemes().subscribe((res) => {
            this.themes = res;
            this.themes.forEach((t) => {
                t.svg = this.svg_markup(t);
                var menuItems = [];

                this.hasDownloadOption(t) ? menuItems.push(this.menuItemsDefaultOptions[0]) : null;
                this.hasEditOption(t) ? menuItems.push(this.menuItemsDefaultOptions[1]) : null;
                this.hasDuplicateOption(t) ? menuItems.push(this.menuItemsDefaultOptions[2]) : null;
                this.hasSetAsCurrentThemeOption(t) ? menuItems.push(this.menuItemsDefaultOptions[3]) : null;
                this.hasDeleteOption(t) ? menuItems.push(this.menuItemsDefaultOptions[4]) : null;

                t.menuItems = menuItems;

            })
            this.cdRef.markForCheck();
        })
    }

    svg_markup(theme: Theme) {
        return `<svg width="158" height="80">
                   <image xlink:href="/api/v2/environment/themes/${theme.uid}.svg?width=158" width="158" height="80" />
                </svg>`;
    }

    onSave() {
        this.isEditorVisible = false;
        this.ngOnInit();
    }

    onCancel() {
        this.isEditorVisible = false;
    }

    clickMenuItem($event, item) {
        this.selectedRow = item;
        switch ($event.value) {
            case 'Edit':
                this.isEditorVisible = true;
                break;
            case 'Delete':
                this.showDelete = true;
                break;
            case 'Set as Current Theme':
                this.isSetCurrentThemeVisible = true;
                break;
        }
    }

    delete() {
        this.deleteInProgress = true;
        this.brandingService.deleteTheme(this.selectedRow.uid)
            .subscribe((res) => {
                if (res) {
                    this.showDelete = false;
                }
                this.deleteInProgress = false;
                this.ngOnInit();
                this.cdRef.markForCheck();
            });
    }

    setAsCurrentTheme() {
        this.settingCurrentThemeInProgress = true;
        this.brandingService.setAsCurrentTheme(this.selectedRow.uid)
            .subscribe((res) => {
                if (res) {
                    this.isSetCurrentThemeVisible = false;
                }
                this.settingCurrentThemeInProgress = false;
                this.ngOnInit();
                this.cdRef.markForCheck();
            });
    }
}