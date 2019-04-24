import { Injectable } from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {catchError, map} from 'rxjs/operators';

import { ResponsibilityTypeRelationPermission } from '../models/responsibility-type.model';
import { MessagesService } from './messages.service';
import {BaseObservableService} from './baseObservable.service';

@Injectable()
export class PermissionsService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
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
