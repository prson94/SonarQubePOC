import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";
import {HttpClient} from "@angular/common/http";
import {Injectable} from '@angular/core';

import {FollowDetail, FollowInfo} from '../models/follower.model';

import {MessagesService} from './messages.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class FollowerService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

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
        return this
            .http
            .get(`api/followinfo/${type}/${id}`)
            .pipe(
                map(response => <FollowInfo>response),
                catchError(err => this.handleError(err))
            );
    }

    updateFollowStatus(
        type: string,
        id: number,
        includeChildren: boolean = false
    ): Observable<any> {
        return this
            .http
            .post('resources/UpdateFollowStatus', {type: type, id: id, includeChildren: includeChildren})
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }
}
