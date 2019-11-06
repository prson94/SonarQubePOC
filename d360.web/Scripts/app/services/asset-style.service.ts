import { Injectable } from '@angular/core';
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";
import { AssetTypeStyle } from '../models/asset-type-style.model';

@Injectable()
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
}
