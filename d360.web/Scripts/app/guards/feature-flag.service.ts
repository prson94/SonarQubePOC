import { inject, Injectable } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
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

export const AssignmentDetailsGuard: CanActivateFn = (): boolean => {
	if (!inject(FeatureFlagService).canActivateAssignmentDetails()) {
		inject(Router).navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
		return false;
	}
	return true;
};
