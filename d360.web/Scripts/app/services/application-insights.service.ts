import { Injectable } from '@angular/core'
import { ActivatedRouteSnapshot, Router, NavigationStart, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';

declare var aisdk;

@Injectable()
export class ApplicationInsightsService {    
    private routerSubscriptionStart: Subscription;
    private routerSubscriptionEnd: Subscription;

    constructor(private router: Router) {        
        this.routerSubscriptionStart = this.router.events.pipe(filter(event => event instanceof NavigationStart)).subscribe((event: NavigationStart) => {
            this.startNavigationEvent(event.url);            
        });
        this.routerSubscriptionEnd = this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe((event: NavigationEnd) => {
            this.endNavigationEvent(event.url);                      
        });
    }

    setUserId(userId: string) {
        try {
            if (aisdk) aisdk.setAuthenticatedUserContext(userId);
        }
        catch (e) {
            console.warn("Exception setting authenticated user with Application Insights.",e);
        }
    }

    clearUserId() {
        try {
            if (aisdk) aisdk.clearAuthenticatedUserContext();
        }
        catch (e) {
            console.warn("Exception clearing authenticated user with Application Insights.", e);
        }
    }

    logPageView(name?: string, uri?: string) {
        if (aisdk) aisdk.trackPageView({ name: name, uri: uri });
    }

    private getActivatedComponent(snapshot: ActivatedRouteSnapshot): any {
        if (snapshot.firstChild) {
            return this.getActivatedComponent(snapshot.firstChild);
        }

        return snapshot.component;
    }

    private getRouteTemplate(snapshot: ActivatedRouteSnapshot): string {
        let path = '';
        if (snapshot.routeConfig) {
            path += snapshot.routeConfig.path;
        }

        if (snapshot.firstChild) {
            return path + this.getRouteTemplate(snapshot.firstChild);
        }

        return path;
    }

    startNavigationEvent(url: string) {
        try {
            if (aisdk) {
                aisdk.startTrackPage();
            }
        }
        catch (e) {
            console.warn("Exception starting page tracking with Application Insights.",e);
        }
    }

    endNavigationEvent(url: string) {    
        try {
            if (aisdk) {
                aisdk.stopTrackPage({ url: url });
            }
        }
        catch (e) {
            console.warn("Exception ending page tracking with Application Insights.",e);
        }
    }
}