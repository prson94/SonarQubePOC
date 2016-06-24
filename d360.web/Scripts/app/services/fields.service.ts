///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { FieldDefinition, IFieldsService, FieldTypeEditorModel } from '../models/fields.model';
import { MessagesService } from './index';
import { BaseService } from './base.service';

@Injectable()
export class FieldsService extends BaseService implements IFieldsService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFields(objectID: number, objectType: string): Promise<FieldDefinition[]> {
        return this.http.get(`/fields/${objectType}/${objectID}.json`)
            .toPromise()
            .then(response => <FieldDefinition[]>response.json())
            .catch(err=>this.handleError(err));
    }

    getFieldTypeEditor(id: number): Promise<FieldTypeEditorModel> {
        return this.http.get(`form/FieldType?id=${id}`)
            .toPromise()
            .then(response => <FieldTypeEditorModel>response.json())
            .catch(err=>this.handleError(err));
    }

    getFusionLookupDisplayFields(id: number): Promise<any> {
        return this.http.get(`form/FieldType_FusionLookup_DisplayFields?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err=>this.handleError(err));
    }

    getFusionLookupTargetAttributeTypes(id: number): Promise<any> {
        return this.http.get(`form/FieldType_FusionLookup_TargetAttributeTypes?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err =>this.handleError(err));
    }    
}


