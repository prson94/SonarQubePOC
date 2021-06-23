import { Injectable } from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {catchError, map} from 'rxjs/operators';

import { ResponsibilityTypeRelationPermission } from '../models/responsibility-type.model';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class PermissionsService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getPermissions(
        objectID: number,
        objectType: string
    ): Observable<ResponsibilityTypeRelationPermission[]> {
        return this.http.get(`api/${objectType}/${objectID}/permissions`).pipe(
            map(response => <ResponsibilityTypeRelationPermission[]>response),
            catchError(err => this.handleError(err))
        );
    }

    getPermissionsById(assetID: number): Observable<ResponsibilityTypeRelationPermission[]> {
        return this.http.get(`api/${assetID}/permissionsbyid`).pipe(
            map(response => <ResponsibilityTypeRelationPermission[]>response),
            catchError(err => this.handleError(err))
        );
    }
}
