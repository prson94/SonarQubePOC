import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import { AssetTypeEditorModel, AssetTypeClass, AssetType, AssetTypeApiModel, AssetTypeLevelApiModel } from "../models/asset.model";


import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { ApiResult, ErrorResponse } from '../models/apiresult.model';
import { Response } from 'powerbi-router';

@Injectable({
    providedIn: 'root'
})
export class AssetTypeService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getAssetTypeEditor(
        cls: AssetTypeClass,
        id: number,
        parentID: number
    ): Observable<AssetTypeEditorModel> {
        return this
            .http
            .get(`form/AssetType?class=${cls}&parentID=${parentID}&id=${id}`)
            .pipe(
                map(response => <AssetTypeEditorModel>response),
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

    //#region v2 endpoints


    public deleteSingleAssetType(uid: string): Observable<any> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
            body: { Uid: uid, Cascade: true }
        };

        return this
            .http
            .delete(`api/v2/assets/single`, httpOptions)
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    public getAssetTypes(params: any)
        : Observable<AssetTypeApiModel[] & ErrorResponse> {

        var qString = '';
        if (params) {
            qString = Object.keys(params).map((key) => key + '=' + params[key]).join('&');
            if (qString) {
                qString = '?' + qString;
            }
        }

        return this
            .http
            .get(`api/v2/assets/types${qString}`)
            .pipe(
                map(response => <AssetTypeApiModel[] & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );
    }

    public getAssetTypesByClass(cs: AssetTypeClass): Observable<AssetTypeApiModel[] & ErrorResponse> {
        return this
            .http
            .get('api/v2/assets/types?class=' + cs.toString())
            .pipe(
                map(response => <AssetTypeApiModel[] & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );
    }

    public getAssetTypeObjectAndID(uid: string) {
        return this.http.get(`api/getAssetTypeObjectAndObjectID/${uid}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    GetAssetTypeByUid(uid: string): Observable<AssetTypeApiModel & ErrorResponse> {
        return this
            .http
            .get(`api/v2/assets/types?assetTypeUid=${uid}`)
            .pipe(
                map((response) => { return <AssetTypeApiModel>response[0] }),
                catchError(err => this.handleError(err))
            );
    }

    GetAssetTypePossibleOwners(uid: string): Observable<any[]> {
        return this
            .http
            .get(`api/v2/assets/${uid}/possibleOwners`)
            .pipe(
                map((response) => { return <any[]>response }),
                catchError(err => this.handleError(err))
            );
    }

    GetAssetTypePossibleCreators(uid: string): Observable<any[]> {
        return this
            .http
            .get(`api/v2/assets/${uid}/possibleCreators`)
            .pipe(
                map((response) => { return <any[]>response }),
                catchError(err => this.handleError(err))
            );
    }

    GetAssetTypePossibleRedactors(uid: string): Observable<any[]> {
        return this
            .http
            .get(`api/v2/assets/${uid}/possibleRedactors`)
            .pipe(
                map((response) => { return <any[]>response }),
                catchError(err => this.handleError(err))
            );
    }

    public postAssetType(model: AssetType)
        : Observable<ApiResult & ErrorResponse> {
        return this
            .http
            .post('api/v2/assets', model)
            .pipe(
                map(response => <ApiResult & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );
    }

    public putAssetType(model: AssetType)
        : Observable<ApiResult & ErrorResponse> {
        return this
            .http
            .put('api/v2/assets', model)
            .pipe(
                map(response => <ApiResult & ErrorResponse>response),
                catchError(err => this.handleError(err))
            );
    }

    public getAssetTypesDetails(): Observable<AssetType[]> {
        return this.http
            .get('api/v2/assets/types')
            .pipe(
                map(res => <AssetType[] & ErrorResponse>res),
                catchError(err => this.handleError(err))
            );
    }

    public getAssetTypeLevels(assetTypeUid: string): Observable<AssetTypeLevelApiModel[]> {
        return this.http
            .get(`api/v2/assets/${assetTypeUid}/levels`)
            .pipe(
                map((res) => <AssetTypeLevelApiModel[] & ErrorResponse>res),
                catchError((err) => this.handleError(err))
            );
    }
    //#endregion
}
