import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, Subject } from "rxjs";
import { distinctUntilChanged, filter, map, takeUntil, tap } from "rxjs/operators";
import { Router, NavigationEnd } from '@angular/router';
import { isQueryRequest } from './isQuery';
import { ROUTE_INDEPENDENT_QUERY } from "./routeIndependentQuery";
import { takeUntilAndThrow } from "../static/rxjs";

@Injectable({ providedIn: 'root' })
export class CancelOnPageChangeInterceptor implements HttpInterceptor {
    constructor(private router: Router) {
        this.router.events.pipe(
            filter((event) => event instanceof NavigationEnd),
            map((x) => (x as NavigationEnd).urlAfterRedirects),
            distinctUntilChanged(),
            tap(() => { this.hasAtLeastOneNavigationEnd = true; })
        ).subscribe((url) => this.navigationEnd$.next(url));
    }

    hasAtLeastOneNavigationEnd = false;
    navigationEnd$ = new Subject();

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (!isQueryRequest(req)) {
            return next.handle(req);
        }

        if (req.context.get(ROUTE_INDEPENDENT_QUERY) === true) {
            return next.handle(req);
        }

        if (!this.hasAtLeastOneNavigationEnd) {
            return next.handle(req).pipe(
                takeUntilAndThrow(this.navigationEnd$, () => {
                    return new Error(
                        `Request ${req.method} ${req.url} was cancelled, but it's called before first page load. 
                        Thus it's system event that shouldn't be cancelled.
                        In order to fix it, change code for making API call to next: 

                            this.http.${req.method.toLowerCase()}(
                                '${req.url}', 
                                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
                            )
                        `);
                }));
        }

        return next.handle(req).pipe(takeUntil(this.navigationEnd$));
    }
}
