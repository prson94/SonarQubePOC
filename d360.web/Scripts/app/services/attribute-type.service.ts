
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { AttributeType } from '../models/attribute-type.model';
import { JsonResult } from '../models/jsonresult.model';
import { DropdownOption } from '../models/dropdown.model';

@Injectable()
export class AttributeTypeService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getAttributes(): Promise<AttributeType[]> {
        return this.http.get('attributes/fulltypes')
            .toPromise()
            .then(response => <AttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAttributeCategoryTypes(parentID?: number): Promise<DropdownOption[]> {
        let url = `attributes/categories`;

        if (parentID != undefined) url = `attributes/categories?parentID={parentID}`;

        return this.http.get(url)
            .toPromise()
            .then(response => <DropdownOption[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAttributeTypesForObject(objectType: string, objectId: number): Promise<AttributeType[]> {        
        return this.http.get(`/api/${objectType}/${objectId}/attributetypefilters`)
            .toPromise()
            .then(response => <AttributeType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAttributeFilterValues(objectType: string, objectId: number, attributeId: number): Promise<string[]> {
        return this.http.get(`/api/${objectType}/${objectId}/${attributeId}/attributefiltervalues`)
            .toPromise()
            .then(response => response.json().map(function (item) {return item['Name'];}))            
            .catch(err => this.handleError(err));
    }

    deleteAttributeType(id: number) {
        return this.deleteDynamic(this.http, 'attributetype', id);
    }

    saveAttributeType(attributeType: AttributeType): Promise<JsonResult> {
        if (attributeType.ID == undefined || !attributeType.ID) {
            return this.postDynamic(this.http, 'attributetype', attributeType);
        }
        return this.putDynamic(this.http, 'attributetype', attributeType);
    }
}