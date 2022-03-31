import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { DomSanitizer, Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import * as _ from 'lodash';
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

    ngOnInit() {
        this.isLoading = true;
        this.brandingService.getThemes().subscribe((res: Theme[]) => {
            this.themes = res;
            this.themes.forEach((t) => {
                t.svg = this.svg_markup(t);
                var menuItems = [];

                t.hasDownloadOption ? menuItems.push(this.menuItemsDefaultOptions[0]) : null;
                t.hasEditOption ? menuItems.push(this.menuItemsDefaultOptions[1]) : null;
                t.hasDuplicateOption ? menuItems.push(this.menuItemsDefaultOptions[2]) : null;
                t.hasSetAsCurrentThemeOption ? menuItems.push(this.menuItemsDefaultOptions[3]) : null;
                t.hasDeleteOption ? menuItems.push(this.menuItemsDefaultOptions[4]) : null;

                t.menuItems = menuItems;

            });
            this.isLoading = false;
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
        var value = $event.value ?? $event;
        switch (value) {
            case 'Edit':
                this.isEditorVisible = true;
                break;
            case 'Delete':
                this.showDelete = true;
                break;
            case 'Duplicate':
                this.duplicateSelectedTheme();
                break;
            case 'Set as Current Theme':
                this.isSetCurrentThemeVisible = true;
                break;
        }
    }

    linkClicked($event) {
        this.clickMenuItem($event, this.selectedRow);
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

    duplicateSelectedTheme() {
        var theme = _.cloneDeep(this.selectedRow._orig);
        theme.name = this.getUniqueName(this.selectedRow.name, 0);
        theme.isCurrent = false;
        theme.uid = "";

        this.isLoading = true;
        this.cdRef.markForCheck();

        this.brandingService.saveTheme(theme)
            .subscribe((res) => {
                this.ngOnInit();
                this.cdRef.markForCheck();

            });
    }

    getUniqueName(name: string, idx: number) {
        var checkName = name;
        if (idx > 0) {
            checkName += ` (${idx})`;
        }
        if (this.themes.some((x) => x.name === checkName)) {
            return this.getUniqueName(name, idx + 1);
        }
        return checkName;
    }
}