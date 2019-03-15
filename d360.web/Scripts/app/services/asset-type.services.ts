import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {BaseObservableService} from "./baseObservable.service";
import {MessagesService} from './messages.service';
import {JsonResult} from '../models/jsonresult.model';
import {AssetTypeEditorModel, AssetTypeClass} from "../models/asset.model";

@Injectable()
export class AssetTypeService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesService
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

    putAssetType(
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

    postAssetType(
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

    public deleteAssetType(
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
}
