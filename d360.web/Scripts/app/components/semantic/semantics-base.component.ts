import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { CompanySettingsService } from '../../services/settings.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Router } from '@angular/router';
import { FeatureFlags } from '../../_shared/models/feature-flags';
import { FeatureFlagsInitService } from '../../services/feature-flags-init.service';

export class SemanticBaseComponent extends AssetGridBaseComponent {
    semanticTypesEnabled: boolean;

    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected router: Router,
        featureFlagService?: FeatureFlagsInitService,
        secondaryNavService?: SecondaryNavService,
        webAnalyticsService?: WebAnalyticsService) {
		super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);

		featureFlagService.getFlagValue(FeatureFlags.SemanticTypesUiFlag).then((flag) => {
			this.semanticTypesEnabled = flag;
		});

        if (!this.semanticTypesEnabled) {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
        }        
    }
}