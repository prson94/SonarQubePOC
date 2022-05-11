import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { head } from 'lodash';
import { Category } from '../../../models/object-detail.model';

import { BrandingService, Theme } from '../../../services/branding.service';
import { FeatureFlags, FeatureFlagsService } from '../../../services/featureflags.service';

@Component({
    selector: "theme-detail",
    templateUrl: "theme-details.component.html",
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./theme-details.component.less"]
})

export class ThemeDetailComponent implements OnChanges {
    @Input() theme: Theme;

    @Output() linkClicked = new EventEmitter();

    categories: Category[] = new Array<Category>();
    hasCustomCss: boolean = false;

    constructor(private brandingService: BrandingService,
        featureFlagService?: FeatureFlagsService) {
        if (featureFlagService.flags[FeatureFlags.BrandingThemeCustomCss]) {
            this.hasCustomCss = true;
        }
    }

    ngOnChanges(simpleChange: SimpleChanges) {
        this.loadData();
    }

    loadData() {
        this.categories = [];
        this.theme.fillDefaultValues();
        var header = new Category('Header Bar');
        header.active = true;
        header.rows = [];
        header.rows.push(
            { title: $localize`Header logo image`, value: this.theme.headerLogoUri ?? this.brandingService.headerLogoDefault, type: "img", style: "logo" });
        header.rows.push(
            { title: $localize`Browser logo icon`, value: this.theme.iconUri ?? this.brandingService.iconDefault, type: "img", style: "icon" });
        header.rows.push(
            { title: $localize`Background color`, value: this.theme.navbarBackColor, type: "color" });
        header.rows.push(
            { title: $localize`Background link color`, value: this.theme.breadcrumbLinkColor, type: "color" });
        header.rows.push(
            { title: $localize`Button color`, value: this.theme.buttonBackColor, type: "color" }
        );

        var navSidebar = new Category('Navigation Sidebar');
        navSidebar.rows.push(
            { title: $localize`Side menu color`, value: this.theme.navbarBackColor, type: "color" }
        );
        navSidebar.rows.push(
            { title: $localize`Side menu selection color`, value: this.theme.navbarBackColorSelected, type: "color" }
        );

        var home = new Category($localize`Home Page`);
        home.rows.push(
            { title: $localize`Background Image`, value: this.theme.homeBackgroundUri ?? this.brandingService.homeBackgroundDefault, type: "img" });

        var general = new Category($localize`General`);
        general.rows.push(
            { title: $localize`Primary Button Color`, value: this.theme.primaryButtonBackColor, type: "color" }
        );
        general.rows.push(
            { title: $localize`Background color`, value: this.theme.backColor, type: "color" }
        );
        general.rows.push(
            { title: $localize`Tab/link color`, value: this.theme.tabLinkColor, type: "color" }
        );
        general.rows.push(
            { title: $localize`Table Header Color`, value: this.theme.tableHeaderBackColor, type: "color" }
        );
        general.rows.push(
            { title: $localize`Table Row Background Color`, value: this.theme.tableRowBackColor, type: "color" }
        );
        this.categories.push(header);
        this.categories.push(navSidebar);
        this.categories.push(home);
        this.categories.push(general);

        let cssCat: Category;
        if (this.hasCustomCss) {
            cssCat = new Category($localize`CSS Customization`);
            cssCat.rows.push(
                { title: '', value: this.theme.customCss, type: "code" });

            this.categories.push(cssCat);
        }

        this.brandingService.getThemeCustomCSS(this.theme)
            .subscribe((res) => {
                if (res) {
                    this.theme.customCss = res;
                    cssCat.rows[0].value = this.theme.customCss;
                }
            });

        this.categories.forEach((cat) => {
            cat.loaded = true;
            cat.hasData = true;
        });
    }
}