import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
//import { CompanySettingEnum } from '../models/settings.model';
//import { CompanySettingsService } from '../services/settings.service';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { FeatureFlags } from '../services/feature-flags.enum';
import { SiteUrlHelpers } from '../static/site-url-helpers';

@Injectable({ providedIn: 'root' })
export class FeatureFlagGuard implements CanActivate {
	constructor(
		protected router: Router,
		protected featureFlagService: LaunchDarklyService
	) { }

	canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {

		if (state.url.startsWith('/monitor') || state.url.endsWith('/workflowmonitor')) {
			if (this.featureFlagService.variation<boolean>(FeatureFlags.AssignmentsFlag)) {
				this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
			}
		}

		if (state.url.startsWith('/assignments') || state.url.startsWith('/requests') || state.url.endsWith('/assignments')) {
			if (!this.featureFlagService.variation<boolean>(FeatureFlags.AssignmentsFlag)) {
				this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
			}
		}

		return true;
    }
}
