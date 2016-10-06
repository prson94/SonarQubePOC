import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ObjectStyle } from '../models/object-detail.model';

@Injectable()
export class ObjectStyleService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectStyle(objectID: number, objectType: string): Promise<ObjectStyle> {
        return this.http.get(`api/${objectType}/${objectID}/style`)
            .toPromise()
            .then(response => <ObjectStyle>response.json())
            .catch(err => this.handleError(err));
    }
}