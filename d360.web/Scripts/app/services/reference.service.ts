import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ReferenceItemType, ReferenceItem } from '../models/reference.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class ReferenceService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getReferenceItemTypes(): Promise<ReferenceItemType[]> {
        return this.http.get(`api/referenceItemTypes`)
            .toPromise()
            .then(response => <ReferenceItemType[]>response.json())
            .catch(err => this.handleError(err));
    }


    getReferenceItems(referenceItemTypeId: number): Promise<ReferenceItem[]> {
        return this.http.get(`api/referenceItems/${referenceItemTypeId}`)
            .toPromise()
            .then(response => <ReferenceItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    saveReferenceItemType(item: ReferenceItemType) {
        if (item.ID == undefined || !item.ID) {
            return this.postDynamic(this.http, 'referenceItemType', item);
        }
        return this.putDynamic(this.http, 'referenceItemType', item);
    }

    deleteReferenceItemType(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'referenceItemType', id);
    }
}