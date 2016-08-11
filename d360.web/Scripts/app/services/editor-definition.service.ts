///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { EditorField } from '../models/editor-field.model';
import { FormControl, FormGroup, Validators } from '@angular/forms';

@Injectable()
export class EditorDefinitionService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getEditorDefinition(ID: number, objectID: number, objectType: string, parentID?: number, targetType?: string, targetTypeID?: number): Promise<EditorField[]> {
        let uri = "";

        if (ID == undefined) {
            console.log(parentID);
            console.log(targetType);
            console.log(targetTypeID);
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
        
        return this.http.get(uri)
            .toPromise()
            .then(response => <EditorField[]>response.json())
            .catch(err => this.handleError(err));
    }

    toFormGroup(editorField: EditorField[]) {
        let group: any = {};

        editorField.forEach(field => {
            group[field.FieldName] = field.Required ? new FormControl(field.Value || '', Validators.required)
                : new FormControl(field.Value || '');
        });
        return new FormGroup(group);
    }
}