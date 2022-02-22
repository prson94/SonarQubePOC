import { Injectable } from '@angular/core';
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { HttpClient } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { IconProperties } from '../models/icon-properties.model';
import { AssetTypeClass } from '../models/asset.model';

@Injectable({
    providedIn: 'root'
})
export class IconService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    private readonly prefix: string = 'fa-';

    private data;
    private observable;

    public getIconProperties(): Observable<IconProperties[]> {
        if (this.data) {
            return of(this.data);
        } else if (this.observable) {
            return this.observable;
        } else {
            this.observable = this.http.get('/content/json/fontawesome4x.json', {
                observe: 'response'
            }).pipe(
                map((res: any) => {
                    this.observable = null;
                    this.data = res.body;
                    return this.data;
                }),
                catchError(err => this.handleError(err))
            );

            return this.observable;
        }
    }

    public getIconImages(): Observable<IconProperties[]> {
        this.observable = this.http.get('/content/json/governanceIcons.json', {
            observe: 'response'
        }).pipe(
            map((res: any) => {
                this.observable = null;
                return res.body;
            }),
            catchError(err => this.handleError(err))
        );

        return this.observable;
    }

    public removeIconPrefix(s: string): string {
        if (s == null || s.length == 0)
            return s;
        return s.replace(this.prefix, '');
    }

    public getIconIdByClass(c: AssetTypeClass): string {
        switch (c) {
            case AssetTypeClass.BusinessAsset:
                return 'book';
            case AssetTypeClass.TechnicalAsset:
                return 'database';
            case AssetTypeClass.Model:
                return 'sitemap';
            case AssetTypeClass.Policy:
                return 'university';
            case AssetTypeClass.Reference:
            case AssetTypeClass.ReferenceItemType:
                return 'list-alt';
            case AssetTypeClass.Rule:
                return 'check-square';
            case AssetTypeClass.User:
                return 'user';
            case AssetTypeClass.SemanticType:
                return 'tags';
            default:
                console.warn('No default icon defined for ' + AssetTypeClass[c]);
                return '';
        }
    }
}
