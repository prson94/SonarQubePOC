import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { SiteUrlHelpers } from '../static/site-url-helpers';


@Injectable()
export class GalleryGuard  {    
    constructor(protected router: Router) { }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
        const url = window.location.href;
        if (url.indexOf('.eng.') > 0) {            
            return true;
        }
        else {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
            return false;
        }
    }
}