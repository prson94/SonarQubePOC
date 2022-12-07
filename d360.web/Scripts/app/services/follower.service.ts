import { Observable } from "rxjs";
import { catchError, map, shareReplay } from "rxjs/operators";
import { HttpClient, HttpContext } from "@angular/common/http";
import { Injectable } from '@angular/core';

import { FollowDetail, FollowInfo } from '../models/follower.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { ROUTE_INDEPENDENT_QUERY } from "../http-interceptors";

@Injectable({
    providedIn: 'root'
})
export class FollowerService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    //Follow Info cache
    private _followInfoCache: Map<string, Observable<FollowInfo>> = new Map<string, Observable<FollowInfo>>();
    private _cacheTime = () => 5 * 1000; //5 seconds

    getFollowers(
		assetUid: string,
		assetTypeUid: string
    ): Observable<FollowDetail[]> {
        return this
            .http
			.get(`api/${assetTypeUid}/${assetUid}/followers`)
            .pipe(
                map((response) => <FollowDetail[]>response),
                catchError((err) => this.handleError(err))
            );

	}

	getFollowersByAssetUid(assetUid: string): Observable<FollowDetail[]> {
		return this
			.http
			.get(`api/${assetUid}/followers`)
			.pipe(
				map((response) => <FollowDetail[]>response),
				catchError((err) => this.handleError(err))
			);

	}

    getFollowInfo(
		assetUid: string,
		assetTypeUid: string
    ): Observable<FollowInfo> {
		const cacheKey = assetUid + '||' + assetTypeUid;

        if (!this._followInfoCache.has(cacheKey)) {
            this._followInfoCache.set(cacheKey, this
                .http
                .get(
					`api/followinfo/${assetTypeUid}/${assetUid}`,
                    { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
                )
                .pipe(
                    map((response) => <FollowInfo>response),
                    catchError((err) => this.handleError(err)),
                    shareReplay(1, this._cacheTime())
                ));
            setTimeout(() => {
                this._followInfoCache.delete(cacheKey);
            }, this._cacheTime());
        }
        return this._followInfoCache.get(cacheKey);
    }

    updateFollowStatus(
		assetUid: string,
		assetTypeUid: string,
        includeChildren: boolean = false
    ): Observable<any> {
        this._followInfoCache.clear();
        return this
            .http
			.post('resources/UpdateFollowStatus', { assetTypeUid, assetUid, includeChildren })
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }
}
