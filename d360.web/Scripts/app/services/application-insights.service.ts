import { Injectable } from '@angular/core'
import { ActivatedRouteSnapshot, ResolveEnd, Router, NavigationStart, NavigationEnd } from '@angular/router';
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
        if (aisdk) aisdk.setAuthenticatedUserContext(userId);
    }

    clearUserId() {
        if (aisdk) aisdk.clearAuthenticatedUserContext();
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
        if (aisdk) {
            aisdk.startTrackPage();
        }
    }

    endNavigationEvent(url: string) {        
        if (aisdk) {
            aisdk.stopTrackPage({ url: url });
        }
    }
}