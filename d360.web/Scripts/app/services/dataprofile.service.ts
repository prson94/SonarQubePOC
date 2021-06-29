import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpClientJsonpModule } from '@angular/common/http';
import { Observable, throwError } from "rxjs";
import { catchError, map, debounceTime } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import { ApiResult, ErrorResponse } from '../models/apiresult.model';

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';

import * as _ from 'lodash';

@Injectable()
export class DataProfileService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }    

    public getDataProfiles(assetUid: string, startDate: Date = null, endDate: Date = null, includeChildAssets: boolean = false, includeTotal: boolean = false): Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: [{ _startDate: startDate, _endDate: endDate, _includeChildAssets: includeChildAssets, _includeTotal: includeTotal }]
        };

        return this
            .http
            .get(`/api/v2/dataprofiles/${assetUid}`, httpOptions)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err, true))
            );
    }
}
