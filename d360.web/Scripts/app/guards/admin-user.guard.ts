import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthenticationService } from '../services/authentication.service';
import { Observable } from 'rxjs';
import { SiteUrlHelpers } from '../static/site-url-helpers';


@Injectable()
export class AdminUserGuard implements CanActivate {
    constructor(protected authenticationService: AuthenticationService, protected router: Router) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {
        if (this.authenticationService.isAdmin == null) {
            return this.authenticationService.checkCurrentUserAdmin();
        }

        if (this.authenticationService.isAdmin) {
            //
            console.log('auth');
            return true;
        }
        else {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
            return false;
        }
    }
}