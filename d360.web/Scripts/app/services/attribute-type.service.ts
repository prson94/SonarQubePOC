import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { AttributeType, AttributeTypeAllocation } from '../models/attribute-type.model';
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

    getAttributeTypeAllocations(attributeTypeId: number): Promise<AttributeTypeAllocation[]> {
        return this.http.get(`/api/AttributeType/${attributeTypeId}/allocations`)
            .toPromise()
            .then(response => <AttributeTypeAllocation[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteAttributeTypeAllocations(attributeTypeId: number, objectTypeId: number, objectType:string): Promise<JsonResult> {                        
        return this.http
            .delete(`form/DeleteAttributeTypeRelationWithUri?AttributeTypeID=${attributeTypeId}&ObjectType=${encodeURIComponent(objectType)}&ObjectID=${objectTypeId}`)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    addAttributeTypeAllocations(objectTypeInfo: string, allowMultiple: boolean, attributeTypeId: number): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });

        this.addRequestVerificationHeaders(headers);

        return this.http
            .post('form/AddAttributeTypeRelation', `AllowMultipleEntries=${allowMultiple}&ObjectTypeInfo=${objectTypeInfo}&AttributeTypeID=${attributeTypeId}`, { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    editAttributeTypeAllocations(objectTypeInfo: string, allowMultiple: boolean, attributeTypeId: number): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });

        this.addRequestVerificationHeaders(headers);

        return this.http
            .put('form/EditAttributeTypeRelation', `AllowMultipleEntries=${allowMultiple}&ObjectTypeInfo=${objectTypeInfo}&AttributeTypeID=${attributeTypeId}`, { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
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