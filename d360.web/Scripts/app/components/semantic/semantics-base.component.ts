import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { CompanySettingsService } from '../../services/settings.service';
import { FeatureFlags, FeatureFlagsService } from '../../services/featureflags.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Router } from '@angular/router';

export class SemanticBaseComponent extends AssetGridBaseComponent {
    private _featureFlagSubscription: any;
    semanticTypesEnabled: boolean;

    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected router: Router,
        featureFlagService?: FeatureFlagsService,
        secondaryNavService?: SecondaryNavService,
        webAnalyticsService?: WebAnalyticsService) {
        super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);

        if (!featureFlagService.flags[FeatureFlags.SemanticTypesUiFlag]) {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
        }        
    }
}