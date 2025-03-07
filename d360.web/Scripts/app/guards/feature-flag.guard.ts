import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { SiteUrlHelpers } from '../static/site-url-helpers';
import { FeatureFlagsInitService } from '../services/feature-flags-init.service';
import { FeatureFlags } from '../_shared/models/feature-flags';

@Injectable({ providedIn: 'root' })
export class FeatureFlagGuard  {
	constructor(
		protected router: Router,
		protected featureFlagService: FeatureFlagsInitService
	) { }

	canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {

		if (state.url.startsWith('/monitor') || state.url.endsWith('/workflowmonitor') || state.url.endsWith('/workflow')) {
			this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);		
		}

		if (state.url.endsWith('/dashboard')) {
			this.featureFlagService.getFlagValue(FeatureFlags.DashboardingEnabled).then((flag) => {
				if (!flag) {
					this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
				}
			});
        }

		if (state.url.endsWith('/security/roles') || state.url.endsWith('/security/policies')) {
			this.featureFlagService.getFlagValue(FeatureFlags.NewSecurityModel).then((flag) => {
				if (!flag) {
					this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
				}
			});
		}

		return true;
    }
}
