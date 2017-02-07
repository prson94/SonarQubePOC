import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthenticationService } from '../services/authentication.service';
import { Observable } from 'rxjs/Observable';
import { SiteUrlHelpers } from '../static/site-url-helpers';
import 'rxjs/add/operator/first';

@Injectable()
export class AdminUserGuard implements CanActivate {
    _isAdmin: boolean = false;
    constructor(protected authenticationService: AuthenticationService, protected router: Router) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {                
        if (this.authenticationService.isAdmin) {
            return true;
        }
        else {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
            return false;
        }        
    }    
}