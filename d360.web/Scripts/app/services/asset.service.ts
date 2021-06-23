import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map, debounceTime } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import { ApiResult, ErrorResponse } from '../models/apiresult.model';

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { AssetEditorModel, AssetTypeClass, AssetCount } from '../models/asset.model';
import { AssetSearchFilter, AssetSearchApiResponse } from '../models/asset-search.model';
import { SelectItem } from 'primeng/api';
import { LookupGrid } from '../models/grid-definition.model';
import * as _ from 'lodash';

@Injectable({
    providedIn: 'root'
})
export class AssetService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    public getAssetLegacyUri(uid: string)
        : Observable<string & ErrorResponse> {
        return this
            .http
            .get(`api/legacyuri/Asset/${uid}`)
            .pipe(
                map(response => <string & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );
    }

    public getAssetTypeLegacyUri(uid: string)
        : Observable<string & ErrorResponse> {
        return this
            .http
            .get(`api/legacyuri/AssetType/${uid}`)
            .pipe(
                map(response => <string & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );
    }

    public deleteAsset(
        assetTypeUid: string,
        uid: string
    ): Observable<ApiResult[]> {

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: [{ Uid: uid, Cascade: true }]
        };

        return this
            .http
            .delete(`api/v2/assets/${assetTypeUid}`, httpOptions)
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    public saveAsset(
        assetTypeUid: string,
        asset: AssetEditorModel
    ): Observable<ApiResult> {

        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        let assetArray: AssetEditorModel[] = [];
        assetArray.push(asset);

        if (asset.Uid) {

            return this
                .http
                .put(`api/v2/assets/${assetTypeUid}?triggersWorkflow=true&lookupFieldsPassedByValue=true`, assetArray, httpOptions)
                .pipe(
                    map((res: ApiResult[]) => {
                        return res[0];
                    }),
                    catchError(err => this.handleError(err))
                );
        }
        else {
            return this
                .http
                .post(`api/v2/assets/${assetTypeUid}?triggersWorkflow=true&lookupFieldsPassedByValue=true`, assetArray, httpOptions)
                .pipe(
                    map((res: ApiResult[]) => {
                        return res[0];
                    }),
                    catchError(err => this.handleError(err))
                );
        }
    }

    public getArtifactType(artifactTypeId: number): Observable<any> {
        return this.
            http
            .get(`/api/v2/assets/artfactType/${artifactTypeId}`)
            .pipe(map(res => { return <any>res }),
                catchError(err => this.handleError(err, true)));
    }

    public getAssetCountsByAssetType(cs: AssetTypeClass): Observable<AssetCount[]> {
        return this.http.get(`/api/v2/assets/counts/byAssetType?class=${cs.toString()}`)
            .pipe(map(res => { return <AssetCount[]>res }),
                catchError(err => this.handleError(err, true)));
    }

    public getAllColors(): Observable<SelectItem[]> {
        return this.http.get(`/api/v2/assets/colors`)
            .pipe(map(res => { return <SelectItem[]>res }),
                catchError(err => this.handleError(err, true)));
    }

    public getAssetTypeLegacyData(uid: string): Observable<any> {
        return this.http.get(`/api/v2/assets/assetTypeLegacyData/${uid}`)
            .pipe(map(res => { return <any>res[0] }),
                catchError(err => this.handleError(err, true)));
    }

    public getAssets(assetTypeUid: string, params: any, onlyListableFields: boolean = false): Observable<any> {
        var qString = '';
        if (onlyListableFields) {
            params._onlyListableFields = true;
            params._includeOwnershipLookup = true;
        }
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }
        return this.
            http
            .get(`/api/v2/assets/${assetTypeUid}${qString}`)
            .pipe(debounceTime(500),
                map(res => { return <any>res }),
                catchError(err => {
                    return this.handleError(err);
                }));
    }

    public getUIDetailsForAssetUID(uid: string): Observable<any> {
        return this.http.get('api/v2/assets/GetUIDetails/' + uid)
            .pipe(map(res => { return <any>res }),
                catchError(err => this.handleError(err, true)));
    }

    public GetObjectUIDetailsForAssetUID(uid: string): Observable<any> {
        return this.http.get('api/v2/assets/GetObjectDetailUIDetails/' + uid)
            .pipe(map(res => { return <any>res }),
                catchError(err => this.handleError(err, true)));
    }

    getAssetsComplexFieldValue(assetUid: string, fieldName: string, params: any = null, isExport: boolean = false, fileName: string = ''): Observable<LookupGrid> | null {
        var url = `/api/v2/assets/${assetUid}/fields/${fieldName}?forUi=true`;

        if (params) {
            var qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                url = url + '&' + qString;
        }

        //to be removed with new filtering UI component
        //current(sprint 5/2021) filtering uses contains on all fields, so we need to avoid value checks on numbers, decimals etc...
        url += "&handleFiltersAsString=true";

        if (!isExport) {
            return this.http.get(url)
                .pipe(
                    map(result => {
                        result['Values'] = result['items'];
                        delete result['items'];
                        return <LookupGrid>result;
                    }),
                    catchError(err => this.handleError(err))
                );
        }
        else {
            this.
                http
                .get(url, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
                .subscribe(data => this.downloadFile(data, fileName));
        }
    }

    public downloadAssetsExcel(assetTypeUid: string, params: any, fileName, callback: Function = null) {
        var copyParams = _.clone(params);

        //Setup paging for export
        copyParams['_pageNum'] = 1;
        copyParams['_pageSize'] = 200000;
        copyParams['_includeTotal'] = false;

        var qString = '';
        if (copyParams) {
            qString = Object.keys(copyParams).map(key => key + '=' + copyParams[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }
        this.
            http
            .get(`/api/v2/assets/${assetTypeUid}${qString}`, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' })
            .subscribe(data => {
                this.downloadFile(data, fileName);
                if (callback) {
                    callback();
                }
            });
    }

    public searchAssetPath(filterValue: AssetSearchFilter): Observable<AssetSearchApiResponse> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };

        return this
            .http
            .post(`/api/v2/assets/paths`, filterValue, httpOptions)
            .pipe(
                debounceTime(500),
                map(response => {
                    return <AssetSearchApiResponse>(response);
                }),
                catchError(err => this.handleError(err))
            );
    }

    public getProcessDiagramUrl(uid: string): Observable<any> {
        return this
            .http
            .get(`/api/v2/assets/${uid}/diagramUrl`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err, true))
            );
    }

    public getAsset(uid: string): Observable<any> {
        return this
            .http
            .get(`/api/v2/assets/asset/${uid}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err, true))
            );
    }

    getAssetsLookupValues(assetTypeUID: string, params: any): Observable<any> {
        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }
        return this.http.get(`api/v2/assets/lookupvalues/${assetTypeUID}` + qString)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }
}
