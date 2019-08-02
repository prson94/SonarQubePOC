import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {EditorField} from '../models/editor-field.model';

import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
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
            else if (targetType && targetTypeID) {
                uri = `form/dynamiceditorrel/new/${objectType}/${objectID}/${targetType}/${targetTypeID}`;
            }
            else {
                uri = `form/dynamiceditor/new/${objectType}/${objectID}`;

                if (objectID && objectID.toString().length == 36) {
                    uri = `form/dynamiceditor/edit/${objectType}/${objectID}`;
                }
            }
        } else {
            if (action && action == "Copy") {
                uri = `form/dynamiceditor/copy/${objectType}/${ID}`;
            } else {
                uri = `form/dynamiceditor/edit/${objectType}/${ID}`;
            }
        }

        if (createParams && createParams.length > 0) {
            return this.http.post(`form/dynamiceditor/new/${objectType}`, createParams).pipe(
                map(response => <EditorField[]>response),
                catchError(err => this.handleError(err))
            );
        } else if (editParams && editParams.length > 0) {
            return this.http.post(`form/dynamiceditor/edit/${objectType}`, editParams).pipe(
                map(response => <EditorField[]>response),
                catchError(err => this.handleError(err))
            );
        } else {
            return this.http.get(uri).pipe(
                map(response => <EditorField[]>response),
                catchError(err => this.handleError(err))
            );
        }
    }
}
