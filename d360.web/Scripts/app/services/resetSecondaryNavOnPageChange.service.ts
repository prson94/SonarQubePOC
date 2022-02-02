import { Injectable } from "@angular/core";
import { distinctUntilChanged, filter, map } from "rxjs/operators";
import { Router, NavigationEnd } from '@angular/router';
import { HeaderBreadcrumbService } from "./header-breadcrumb.service";
import { SecondaryNavService } from "./right-sidebar.service";

@Injectable()
export class ResetSecondaryNavOnPageChangeService {
    constructor(
        private router: Router,
        private secondaryNav: SecondaryNavService,
        private headerBreadcrumbs: HeaderBreadcrumbService
    ) {
    }

    initialize() {
        this.router.events.pipe(
            filter(event => event instanceof NavigationEnd),
            map(x => (x as NavigationEnd).urlAfterRedirects),
            distinctUntilChanged()
        ).subscribe(() => this.onPageChanged());
    }

    onPageChanged() {
        this.secondaryNav.clearItems();
        this.secondaryNav.clearButtons();
        this.secondaryNav.clearCurrentObject();
        this.headerBreadcrumbs.clearBreadcrumbs();
        this.headerBreadcrumbs.clearCurrentObjectInfo();
    }
}

export function resetSecondaryNavOnPageChangeServiceRunner(provider: ResetSecondaryNavOnPageChangeService) {
    return () => {
        provider.initialize();
    }
}