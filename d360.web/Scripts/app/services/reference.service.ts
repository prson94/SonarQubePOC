import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { ReferenceItemType, ReferenceItem } from '../models/reference.model';

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
}