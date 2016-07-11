///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { AttributeType } from '../models/attribute-type.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class AttributeTypeService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getAttributes(): Promise<AttributeType[]> {
        return this.http.get('attributes/types')
            .toPromise()
            .then(response => <AttributeType[]>response.json())
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