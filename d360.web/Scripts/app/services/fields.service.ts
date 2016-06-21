///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { FieldDefinition, IFieldsService, FieldTypeEditorModel } from '../models/fields.model';


@Injectable()
export class FieldsService implements IFieldsService {

    constructor(private http: Http) { }

    getFields(objectID: number, objectType: string): Promise<FieldDefinition[]> {
        return this.http.get(`/fields/${objectType}/${objectID}.json`)
            .toPromise()
            .then(response => <FieldDefinition[]>response.json())
            .catch(this.handleError);
    }

    getFieldTypeEditor(id: number): Promise<FieldTypeEditorModel> {
        return this.http.get(`form/FieldType?id=${id}`)
            .toPromise()
            .then(response => <FieldTypeEditorModel>response.json())
            .catch(this.handleError);
    }

    getFusionLookupDisplayFields(id: number): Promise<any> {
        return this.http.get(`form/FieldType_FusionLookup_DisplayFields?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(this.handleError);
    }

    getFusionLookupTargetAttributeTypes(id: number): Promise<any> {
        return this.http.get(`form/FieldType_FusionLookup_TargetAttributeTypes?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error); 
    }
}


