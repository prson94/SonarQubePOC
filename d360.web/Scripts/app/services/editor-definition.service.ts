import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { EditorField } from '../models/editor-field.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";

@Injectable({
    providedIn: 'root'
})
export class EditorDefinitionService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getEditorDefinition(
        ID: number,
        objectID: number,
        objectType: string,
        parentID?: number,
        targetType?: string,
        targetTypeID?: number,
        createParams?: any[],
        editParams?: any[],
        action?: string
    ): Observable<EditorField[]> {
        let uri = "";
        if (ID == undefined) {
            if (parentID) {
                uri = `form/dynamiceditor/new/${objectType}/${objectID}/${parentID}`;
            }
            else {
                uri = `form/dynamiceditor/new/${objectType}/${objectID}`;
            }
        } else {
            if (action && action == "Copy") {
                uri = `form/dynamiceditor/copy/${objectType}/${ID}`;
            } else {
                uri = `form/dynamiceditor/edit/${objectType}/${ID}`;
            }
        }

        return this.http.get(uri).pipe(
            map(response => <EditorField[]>response),
            catchError(err => this.handleError(err))
        );
    }

    public getEditorDefinitionUid(uid: string, objectType?: string, targetAssetUid?: string): Observable<EditorField[]> {
        var url = `form/dynamiceditor/new/uid/${uid}/type/${objectType}`;

        if (targetAssetUid) {
            url += `/target/${targetAssetUid}`;
        }

        return this.http
            .get(url)
            .pipe(
                map(res => <EditorField[]>res),
                catchError(err => this.handleError(err))
            );
    }

    public getEditorDefinitionNonLegacy(assetTypeUid: string, assetUid: string, objectType: string = null): Observable<EditorField[]> {
        if (!assetUid) {
            return this.getEditorDefinitionUid(assetTypeUid, objectType);
        }
        return this.http
            .get(`form/dynamiceditor/byUid/${assetTypeUid}/${assetUid}`)
            .pipe(
                map(res => <EditorField[]>res),
                catchError(err => this.handleError(err))
            );
    }

    public getAssetEditorDefinition(assetTypeUid: string, assetUid: string, parentAssetUid: string): Observable<EditorField[]> {
        let uri: string = `form/dynamiceditor/assets/${assetTypeUid}`;
        if (assetUid) {
            uri += `/${assetUid}`;
        }
        if (parentAssetUid) {
            uri += `?parentUid=${parentAssetUid}`;
        }
        return this.http
            .get(uri)
            .pipe(
                map(res => <EditorField[]>res),
                catchError(err => this.handleError(err))
            );
    }

}
