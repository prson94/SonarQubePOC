import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpClientJsonpModule, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from "rxjs";
import { catchError, map, debounceTime } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import { ApiResult, ErrorResponse } from '../models/apiresult.model';

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';

import { SemanticTypeGetAssetsResponse, SemanticTypeGetResponse } from '../models/semantic-type.model';

import * as _ from 'lodash';
import { SortOrder } from '../models/enums.model';

@Injectable()
export class DataProfileService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    public getDataProfiles(assetUid: string, startDate: Date = null, endDate: Date = null, includeChildAssets: boolean = false, includeTotal: boolean = false, includeSamples: boolean = true): Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),            
        };

        let url = `/api/v2/dataprofiles/${assetUid}?_pageSize=10000&_pageNum=1`;

        if (startDate) {
            url += `&_startDate=${startDate?.toISOString()}`;
        }

        if (endDate) {
            url += `&_endDate=${endDate?.toISOString()}`;
        }

        if (includeChildAssets) {
            url += `&_includeChildAssets=${includeChildAssets}`;
        }

        if (includeTotal) {
            url += `&_includeTotal=${includeTotal}`;
        }

        if (!includeSamples) {
            url += `&_includeSamples=${includeSamples}`;
        }

        return this
            .http
            .get(url, httpOptions)
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

    public exportMatches(assetUid: string, matchType: string, simpleFilter: string = '', advancedFilter: string = "", assetName: string, sortField: string = "", sortOrder: number = SortOrder.Ascending, callback: Function = null) {

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

        if (sortField) {
            url += `&_order=${sortField}`;
            if (sortOrder && sortOrder !== SortOrder.None) {
                url += `&_direction=${sortOrder === SortOrder.Ascending ? "asc" : "desc"}`;
            }
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

    public getMatchesByMatchType(assetUid: string, matchType: string, pageNum: number, pageSize: number, simpleFilter: string = '', advancedFilter: string = "", sortField: string = "", sortOrder: number = SortOrder.Ascending): Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        
        let url: string = `/api/v2/dataprofiles/${assetUid}/similar/${matchType}?_pageSize=${pageSize}&_pageNum=${((pageNum > 0) ? pageNum : 1)}`;
        if (simpleFilter) {
            url += `&_simpleFilter=${simpleFilter}`;
        }

        if (advancedFilter) {
            url += `&_filter=${advancedFilter}`;
        }

        if (sortField) {
            url += `&_order=${sortField}`;
            if (sortOrder && sortOrder !== SortOrder.None) {
                url += `&_direction=${sortOrder === SortOrder.Ascending ? "asc" : "desc"}`;
            }
        }
        return this
            .http
            .get(url, httpOptions)
            .pipe(
                map((response) => <any>response),
                catchError((err) => err instanceof HttpErrorResponse && err.status === 404 ? null : this.handleError(err))
            );
    }

    downloadFile(data: Blob, name: string) {

        var filename = `${name} _${new Date().toDateString()}_.xlsx`;
        super.downloadFile(data, filename);
    }

    getSemanticTypes(pageNum: number, pageSize: number, simpleFilter: string = '', advancedFilter: string = ""): Observable<SemanticTypeGetResponse> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };

        let url = `api/v2/dataprofiles/semantictypes?_pageSize=${pageSize}&_pageNum=${((pageNum > 0) ? pageNum : 1)}`;

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
                map(response => <SemanticTypeGetResponse>response),
                catchError(err => this.handleError(err, false))
            )
            ;
    }

    getSemanticTypeMatchingAssets(typeQualifier: string, pageNum: number, pageSize: number, minConfidence: number = 0.01, simpleFilter: string = '', advancedFilter: string = "", order: string = "", direction: number = SortOrder.Ascending): Observable<SemanticTypeGetAssetsResponse> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        if (minConfidence <= 0) {
            minConfidence = 1;
        }

        let url = `api/v2/dataprofiles/type/${typeQualifier}/${minConfidence/100}?_pageSize=${pageSize}&_pageNum=${((pageNum > 0) ? pageNum : 1)}`;

        if (simpleFilter) {
            url += `&_simpleFilter=${simpleFilter}`;
        }

        if (advancedFilter) {
            url += `&_filter=${advancedFilter}`;
        }

        if (order) {
            url += `&_order=${order}`;
            if (direction && direction !== SortOrder.None) {
                url += `&_direction=${direction === SortOrder.Ascending ? "asc" : "desc"}`;
            }
        }

        return this
            .http
            .get(url, httpOptions)
            .pipe(
                map(response => <SemanticTypeGetAssetsResponse>response),
                catchError(err => this.handleError(err, false))
            )
            ;
    }
}
