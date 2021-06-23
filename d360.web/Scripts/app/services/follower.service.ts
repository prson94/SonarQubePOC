import {Observable} from "rxjs";
import { catchError, map, shareReplay } from "rxjs/operators";
import {HttpClient} from "@angular/common/http";
import {Injectable} from '@angular/core';

import {FollowDetail, FollowInfo} from '../models/follower.model';

import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

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
        type: string,
        id: number
    ): Observable<FollowDetail[]> {
        return this
            .http
            .get(`api/${type}/${id}/followers`)
            .pipe(
                map(response => <FollowDetail[]>response),
                catchError(err => this.handleError(err))
            );

    }

    getFollowInfo(
        type: string,
        id: number
    ): Observable<FollowInfo> {
        const cacheKey = type + '||' + id.toString();

        if (!this._followInfoCache.has(cacheKey)) {
            this._followInfoCache.set(cacheKey, this
                .http
                .get(`api/followinfo/${type}/${id}`)
                .pipe(
                    map(response => <FollowInfo>response),
                    catchError(err => this.handleError(err)),
                    shareReplay(1, this._cacheTime())
                ));
            setTimeout(() => {
                this._followInfoCache.delete(cacheKey);
            }, this._cacheTime())
        }
        return this._followInfoCache.get(cacheKey)
    }

    updateFollowStatus(
        type: string,
        id: number,
        includeChildren: boolean = false
    ): Observable<any> {
        this._followInfoCache.clear();
        return this
            .http
            .post('resources/UpdateFollowStatus', {type: type, id: id, includeChildren: includeChildren})
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }
}
