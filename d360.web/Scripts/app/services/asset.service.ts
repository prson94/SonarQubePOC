import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import { ApiResult, ErrorResponse } from '../models/apiresult.model';

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { AssetEditorModel } from '../models/asset.model';
import { CommonComponentAssetResult, AssetSearchFilter, AssetSearchApiResponse } from '../models/asset-search.model';
import { URLSearchParams } from 'url';

@Injectable()
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
            .delete(`api/v2/assets/${assetTypeUid}`,httpOptions)
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

    public getAssets(assetTypeUid: string, params: any): Observable<any> {
        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }
        return this.
            http
            .get(`/api/v2/assets/${assetTypeUid}${qString}`)
            .pipe(map(res => { return <any>res }),
                catchError(err => this.handleError(err, true)));
    }


    public searchAssetPath(filter: AssetSearchFilter): Observable<AssetSearchApiResponse> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };

        return this
            .http
            .post(`/api/v2/assets/paths`, filter, httpOptions)
            .pipe(map(response => {
                return <AssetSearchApiResponse>(response);
            }),
                catchError(err => this.handleError(err))
            );
    }
}
