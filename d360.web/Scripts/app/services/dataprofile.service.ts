import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpClientJsonpModule } from '@angular/common/http';
import { Observable, of, throwError } from "rxjs";
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
                map((response) => <any>response),
                catchError((err) => this.handleError(err, true))
            );
    }

    public getMatchCounts(assetUid: string, matchType: string): Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
        };

        return this
            .http
            .get(`/api/v2/dataprofiles/${assetUid}/similar/${matchType}/count`, httpOptions)
            .pipe(
                map((response) => <any>response),
                catchError((err) => {
                    if ((err?.error?.message as string).indexOf('signature not found') !== -1) {
                        return of(0);
                    }
                    return this.handleError(err, true);
                })
            );
    }

    public exportMatches(assetUid: string, matchType: string, simpleFilter: string = '', advancedFilter: string = "", assetName: string, callback: Function = null) {

        let pageNum: number = 1;
        let pageSize: number = 200000;

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': "application/octet-stream" }), responseType: 'blob'
        };

        let url: string = `/api/v2/dataprofiles/${assetUid}/similar/${matchType}?_pageSize=${pageSize}&_pageNum=${pageNum}&_includeTotal=false`;

        if (simpleFilter) {
            url += `&_simpleFilter=${simpleFilter}`;
        }

        if (advancedFilter) {
            url += `&_filter=${advancedFilter}`;
        }

        this.
            http
            .get(url, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
            .subscribe((data) => {
                let filename = `Filtered ${assetName} ${matchType.toLowerCase() === 'data' ? "Duplicate" : "Similiar"} Fields List`;
                this.downloadFile(data, filename);
                if (callback) {
                    callback();
                }
            });
    }

    public getMatchesByMatchType(assetUid: string, matchType: string, pageNum: number, pageSize: number, simpleFilter: string = '', advancedFilter: string = ""): Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };

        let url: string = `/api/v2/dataprofiles/${assetUid}/similar/${matchType}?_pageSize=${pageSize}&_pageNum=${pageNum}`;
        if (simpleFilter) {
            url += `&_simpleFilter=${simpleFilter}`;
        }

        if (advancedFilter) {
            url += `&_filter=${advancedFilter}`;
        }
        return this
            .http
            .get(url, httpOptions)
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err, true))
            );
    }

    downloadFile(data: Blob, name: string) {

        var filename = `${name} _${new Date().toDateString()}_.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }
}
