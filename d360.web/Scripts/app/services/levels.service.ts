import {Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {JsonResult} from '../models/jsonresult.model';

import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable({
    providedIn: 'root'
})
export class LevelsService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getObjectLevels(
        objectID: number,
        objectType: string
    ): Observable<any[]> {
        return this.http.get(`api/${objectType}/${objectID}/levels`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    saveObjectLevel(
        level: any,
        objectType: string,
        objectId: number,
        action: string
    ) {
        level.ID = objectId;
        let methodName = 'putDynamic';

        if (action == 'new') {
            methodName = 'postDynamic';
        }

        return this[methodName](this.http, `${objectType}level`, level);
    }

    deleteObjectLevel(objectType: string, objectId: number, levelId: number): Observable<JsonResult> {
        const httpHeaders = new HttpHeaders(
            {
                'Content-Type': 'application/json'
            }
        );
        const url = `form/${objectType}/${objectId}/levels/${levelId}`;

        return this.http.delete(
            url,
            {
                headers: httpHeaders
            }
        ).pipe(
            map(res => <JsonResult>res),
            catchError(err => this.handleError(err))
        );
    }
}
