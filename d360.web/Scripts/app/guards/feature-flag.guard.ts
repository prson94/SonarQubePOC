import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { FeatureFlags } from '../services/feature-flags.enum';
import { SiteUrlHelpers } from '../static/site-url-helpers';

@Injectable({ providedIn: 'root' })
export class FeatureFlagGuard  {
	constructor(
		protected router: Router,
		protected featureFlagService: LaunchDarklyService
	) { }

	canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {

		if (state.url.startsWith('/monitor') || state.url.endsWith('/workflowmonitor') || state.url.endsWith('/workflow')) {
			if (this.featureFlagService.variation<boolean>(FeatureFlags.AssignmentsFlag)) {
				this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
			}
		}

		if (state.url.startsWith('/assignments') || state.url.startsWith('/requests') || state.url.endsWith('/assignments')) {
			if (!this.featureFlagService.variation<boolean>(FeatureFlags.AssignmentsFlag)) {
				this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
			}
		}

		if (state.url.startsWith('/reference')) {
			if (!this.featureFlagService.variation<boolean>(FeatureFlags.ReferenceListV2Flag)) {
				this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
			}
		}

		if (state.url.endsWith('/dashboard')) {
			if (!this.featureFlagService.variation<boolean>(FeatureFlags.DashboardingEnabled)) {
				this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
			}
		}

		return true;
    }
}
