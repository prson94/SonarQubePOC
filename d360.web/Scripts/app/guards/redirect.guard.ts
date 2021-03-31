import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';


declare var CompanySettings;
declare var ResourceHomePage;


@Injectable()
export class RedirectGuard implements CanActivate {
    constructor(protected router: Router) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | boolean {

        if (ResourceHomePage != null && ResourceHomePage != "" && ResourceHomePage != '/') {
            this.router.navigate([ResourceHomePage]);
        }
        else if (CompanySettings != null && CompanySettings.DefaultRoute != null && CompanySettings.DefaultRoute != '' && CompanySettings.DefaultRoute != '/') {
            this.router.navigate([CompanySettings.DefaultRoute]);
        } else {
            this.router.navigate(['home']);
        }

        return true;
    }
}