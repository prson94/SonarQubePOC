import { inject, Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { FeatureFlags } from '../services/feature-flags.enum';
import { SiteUrlHelpers } from '../static/site-url-helpers';

@Injectable({
	providedIn: 'root'
})
export class FeatureFlagService {

	constructor(private launchDarklyService: LaunchDarklyService) {
	}

	canActivateAssignmentDetails(): boolean {
		return this.launchDarklyService.variation<boolean>(FeatureFlags.AssignmentDetailsFlag);
	}
}

export const AssignmentDetailsGuard: CanActivateFn = (next: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean => {
	if (!inject(FeatureFlagService).canActivateAssignmentDetails()) {
		inject(Router).navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
	}
	return true;
};
