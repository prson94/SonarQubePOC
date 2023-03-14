import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { CompanySettingsService } from '../../services/settings.service';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { FeatureFlags } from "../../services/feature-flags.enum";
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Router } from '@angular/router';

export class SemanticBaseComponent extends AssetGridBaseComponent {
    semanticTypesEnabled: boolean;

    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected router: Router,
        featureFlagService?: LaunchDarklyService,
        secondaryNavService?: SecondaryNavService,
        webAnalyticsService?: WebAnalyticsService) {
        super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);
        this.semanticTypesEnabled = featureFlagService.variation<boolean>(FeatureFlags.SemanticTypesUiFlag);
        if (!this.semanticTypesEnabled) {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
        }        
    }
}