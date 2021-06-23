import { Injectable } from '@angular/core';
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { AssetTypeStyle } from '../models/asset-type-style.model';

@Injectable({
    providedIn: 'root'
})
export class AssetStyleService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getAssetTypeStyle(
        assetTypeId: number
    ): Observable<AssetTypeStyle> {
        return this.http.get(`api/${assetTypeId}/style`).pipe(
            map(response => <AssetTypeStyle>response),
            catchError(err => this.handleError(err))
        );
    }

    getAssetTypeObjectStyle(
        objectType: string,
        objectID: number

    ): Observable<any> {
        return this.http.get(`api/${objectType}/${objectID}/style`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }
}
