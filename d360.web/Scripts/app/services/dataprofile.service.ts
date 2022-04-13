import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpClientJsonpModule, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from "rxjs";
import { catchError, map, debounceTime } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import { ApiResult, ErrorResponse } from '../models/apiresult.model';

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';

import { SemanticSource, SemanticType, SemanticTypeGetAssetsResponse, SemanticTypeGetResponse } from '../models/semantic-type.model';

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
                catchError((err) => {
                    if (err?.status === 409) {
                        return of(0);
                    } else {
                        this.handleError(err, true);
                    }                    
                })
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

        var filename = `${name} ${new Date().toDateString()}.xlsx`;
        super.downloadFile(data, filename);
    }

    getSemanticTypes(pageNum: number, pageSize: number, simpleFilter: string = '', advancedFilter: string = "", order: string = "", direction: number = SortOrder.Ascending, isExport: boolean = false, callback: Function = null): Observable<SemanticTypeGetResponse> {
        let url = `api/v2/dataprofiles/semantictypes?_pageSize=${pageSize}&_pageNum=${((pageNum > 0) ? pageNum : 1)}`;

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
        if (isExport) {
            this.
                http
                .get(url, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
                .subscribe((data) => {
                    let filename = `Filtered Semantic Type List`;
                    this.downloadFile(data, filename);
                    if (callback) {
                        callback();
                    }
                });

        } else {
            return this
                .http
                .get(url, { headers: new HttpHeaders({ 'Content-Type': 'application/json' })})
                .pipe(
                    map((response) => <SemanticTypeGetResponse>response),
                    catchError((err) => {
                        if (err?.status === 409) {
                            return of(new SemanticTypeGetResponse());
                        } else {
                            this.handleError(err, true);
                        }
                    })
                );
        }        
    }

    getSemanticTypeMatchingAssets(typeQualifier: string, pageNum: number, pageSize: number, minConfidence: number = 0.01, simpleFilter: string = '', advancedFilter: string = "", order: string = "", direction: number = SortOrder.Ascending, isExport: boolean = false, typeName: string = "",  callback: Function = null): Observable<SemanticTypeGetAssetsResponse> {

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
       
        if (isExport) {
            this.
                http
                .get(url, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
                .subscribe((data) => {
                    let filename = `Filtered Asset list for ${typeName}`;
                    this.downloadFile(data, filename);
                    if (callback) {
                        callback();
                    }
                });

        } else {
            return this
                .http
                .get(url, { headers: new HttpHeaders({ 'Content-Type': 'application/json' }) })
                .pipe(
                    map((response) => <SemanticTypeGetAssetsResponse>response),
                    catchError((err) => {
                        if (err?.status === 409) {
                            return of(new SemanticTypeGetAssetsResponse());
                        } else {
                            this.handleError(err, true);
                        }
                    })
                );           
        } 
    }

    getSemanticLookupList(lookup: string, isExport: boolean = false, callback: Function = null): Observable<any> {
        
        let url = `api/v2/dataprofiles/semantictypes/lookups/${lookup}/`;        

        if (isExport) {
            this.
                http
                .get(`${url}?`, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
                .subscribe((data) => {
                    let filename = `Semantic Type Status List`;
                    this.downloadFile(data, filename);
                    if (callback) {
                        callback();
                    }
                });

        } else {
            return this
                .http
                .get(url, { headers: new HttpHeaders({ 'Content-Type': 'application/json' }) })
                .pipe(
                    map((response) => <any>response),
                    catchError((err) => this.handleError(err, false))
                );
        }
    }

    public deleteSemanticType(
        qualifier: string
    ): Observable<JsonResult> {

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        return this
            .http
            .delete(`api/v2/dataprofiles/semantictypes/${qualifier}`, httpOptions)
            .pipe(
                map((res) => <JsonResult>res),
                catchError((err) => this.handleError(err))
            );
    }

    public postSemanticType(        
        semanticType: SemanticType
    ): Observable<any> {

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        let semanticArray: SemanticType[] = [];
        semanticArray.push(semanticType);

        return this
            .http
            .post(`api/v2/dataprofiles/semantictypes/`, semanticArray, httpOptions)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError((err) => {
                    if (err?.status === 409) {
                        return of(err);
                    } else {
                        this.handleError(err, true);
                    }
                })
            );        
    }

    public putSemanticType(
        semanticType: SemanticType
    ): Observable<any> {

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        let semanticArray: SemanticType[] = [];
        semanticArray.push(semanticType);

        return this
            .http
            .put(`api/v2/dataprofiles/semantictypes/`, semanticArray, httpOptions)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }

    public patchSemanticType(
        semanticType: SemanticType
    ): Observable<any> {

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        let semanticArray: any[] = [];
        
        if (semanticType.source.toString() === SemanticSource[SemanticSource.BuiltIn]) {
            semanticArray.push({ qualifier: semanticType.qualifier, description: semanticType.description, name: semanticType.name });
        } else {
            semanticArray.push(semanticType);
        }
        
        return this
            .http
            .patch(`api/v2/dataprofiles/semantictypes/`, semanticArray, httpOptions)
            .pipe(
                map((res: any) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }
}
