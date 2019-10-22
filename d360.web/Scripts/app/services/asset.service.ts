import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import { ApiResult } from '../models/apiresult.model';

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { AssetEditorModel } from '../models/asset.model';

@Injectable()
export class AssetService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
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
}
