///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { EditorField } from '../models/editor-field.model';

@Injectable()
export class EditorDefinitionService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getEditorDefinition(ID: number, objectID: number, objectType: string, parentID?: number, targetType?: string, targetTypeID?: number, createParams?: any[], editParams?: any[]): Promise<EditorField[]> {
        let uri = "";

        if (ID == undefined) {            
            if (parentID)
                uri = `form/dynamiceditor/new/${objectType}/${objectID}/${parentID}`;
            else if (targetType && targetTypeID)
                uri = `form/dynamiceditorrel/new/${objectType}/${objectID}/${targetType}/${targetTypeID}`;
            else
                uri = `form/dynamiceditor/new/${objectType}/${objectID}`
        }
        else {
            uri = `form/dynamiceditor/edit/${objectType}/${ID}`;
        }

        if (createParams && createParams.length > 0) {
            return this.http.post(`form/dynamiceditor/new/${objectType}`, createParams)
                .toPromise()
                .then(response => <EditorField[]>response.json())
                .catch(err => this.handleError(err));
        } else if (editParams && editParams.length > 0) {
            return this.http.post(`form/dynamiceditor/edit/${objectType}`, editParams)
                .toPromise()
                .then(response => <EditorField[]>response.json())
                .catch(err => this.handleError(err));
        } else {
            return this.http.get(uri)
                .toPromise()
                .then(response => <EditorField[]>response.json())
                .catch(err => this.handleError(err));
        }

        

    }    
}