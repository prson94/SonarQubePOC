import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { CompanySettingEnum } from '../models/settings.model';
import { CompanySettingsService } from '../services/settings.service';


declare var ResourceHomePage;

@Injectable()
export class RedirectGuard implements CanActivate {
    constructor(
        protected settingsService: CompanySettingsService,
        protected router: Router
    ) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {
        let defaultRoute = this.settingsService.getSettingById(CompanySettingEnum.DefaultRoute).StringSetting.Value;
        if (ResourceHomePage !== null && ResourceHomePage !== "" && ResourceHomePage !== '/') {
            this.router.navigate([ResourceHomePage]);
        }
        else if (defaultRoute !== null && defaultRoute !== '' && defaultRoute !== '/') {
            this.router.navigate([defaultRoute]);
        } else {
            this.router.navigate(['home']);
        }

        return true;
    }
}