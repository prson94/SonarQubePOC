import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthenticationService } from '../services/authentication.service';
import { Observable } from 'rxjs/Observable';
import { SiteUrlHelpers } from '../static/site-url-helpers';

@Injectable()
export class AdminUserGuard implements CanActivate {
    _isAdmin: boolean = false;
    constructor(protected authenticationService: AuthenticationService, protected router: Router) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {
                
        // wait for user to be authenticated then return if the user is an admin               
        this.authenticationService.admin().subscribe(
            res => {         
                // Navigate to the home page
                if (!res) {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
                }                                
            },
            err => console.log(err),
            () => {
              //  console.log('auth guard can activate complete')
                
            }
        );


        return this.authenticationService.admin();
    }

    
}