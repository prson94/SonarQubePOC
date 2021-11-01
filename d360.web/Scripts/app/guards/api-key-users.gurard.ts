import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { CompanySettingEnum } from '../models/settings.model';
import { AuthenticationService } from '../services/authentication.service';
import { CompanySettingsService } from '../services/settings.service';
import { SiteUrlHelpers } from '../static/site-url-helpers';

@Injectable()
export class ApiKeyUsersGuard implements CanActivate {
    _isAdmin: boolean = false;
    constructor(
        protected authenticationService: AuthenticationService,
        protected settingsService: CompanySettingsService,
        protected router: Router) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
        let showApiKey = this.settingsService.getSettingById(CompanySettingEnum.ShowAllUsersAPIKey).BooleanSetting.Value;
        if (this.authenticationService.isAdmin || showApiKey)
        {
             return true;
        }
        else {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
            return false;
        }
    }
}