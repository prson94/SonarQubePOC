import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {JsonResult} from '../models/jsonresult.model';
import {AssetTypeEditorModel, AssetTypeClass, AssetType} from "../models/asset.model";


import {BaseObservableService} from "./baseObservable.service";
import {MessagesObservableService} from './messages-observable.service';

@Injectable()
export class AssetTypeService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getAssetTypeEditorOld(
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

    putAssetTypeOld(
        model: AssetTypeEditorModel
    ): Observable<JsonResult> {
        return this
            .http
            .put('form/AssetType', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    postAssetTypeOld(
        model: AssetTypeEditorModel
    ): Observable<JsonResult> {
        return this
            .http
            .post('form/AssetType', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    public deleteAssetTypeOld(
        id: number
    ): Observable<JsonResult> {
        return this
            .http
            .delete(`form/AssetType?id=${id}`)
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    //#region v2 endpoints

    public postAssetType(model: AssetType)
        : Observable<JsonResult> {
        return this
            .http
            .post('api/v2/assets', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    public putAssetType(model: AssetType)
        : Observable<JsonResult> {
        return this
            .http
            .put('api/v2/assets', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }


    //#endregion
}
