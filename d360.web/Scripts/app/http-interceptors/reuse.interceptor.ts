import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { cloneDeep } from "lodash";
import { Observable } from "rxjs";
import { map, tap } from "rxjs/operators";
import { cancellableShareReplayLast } from "../static/rxjs";
import { isQueryRequest } from './isQuery';

const CacheEntryLifeTimeInMinutes = 10;

@Injectable({ providedIn: 'root' })
export class ReuseInterceptor implements HttpInterceptor {
    cache = new Map<string, CacheEntry>();

    public forceRefresh() {
        this.cache.clear();
    }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (isQueryRequest(req)) {
            return this.reuseRequest(req, next);
        }

        if (this.needResetCache(req)) {
            this.cache.clear();
        }

        return next.handle(req);
    }

    reuseRequest(req: HttpRequest<any>, next: HttpHandler) {
        const cacheKey = this.getCacheKey(req);
        const cachedValue = this.cache.get(cacheKey);
        if (cachedValue != null) {
            const cacheEntry = this.cache.get(cacheKey)!;
            const isExpired = new Date().getTime() > cacheEntry.validTo.getTime();
            if (!isExpired) {
                return cacheEntry.httpEvent$;
            }
        }

        const valueToCache = next.handle(req).pipe(
            tap({
                error: () => {
                    this.cache.delete(cacheKey);
                }
            }),
            cancellableShareReplayLast(),
            map(x => cloneDeep(x))
        );

        this.cache.set(cacheKey, {
            validTo: addMinutes(new Date(), CacheEntryLifeTimeInMinutes),
            httpEvent$: valueToCache
        });

        return valueToCache;
    }

    needResetCache(req: HttpRequest<any>) {
        const safeUrls = new Set([
            'api/v2/errors/log/clienterror',
            'webanalytics/logactivity'
        ].map(k => k.toLowerCase()));

        if (safeUrls.has(req.url.toLowerCase())) {
            return false;
        }

        if (isQueryRequest(req)) {
            return false;
        }

        return true;
    }

    getCacheKey(req: HttpRequest<any>) {
        return req.urlWithParams + "-" + JSON.stringify(req.body);
    }
}

type CacheEntry = {
    validTo: Date,
    httpEvent$: Observable<HttpEvent<any>>
}

function addMinutes(date, minutes) {
    return new Date(date.getTime() + minutes * 60000);
}
