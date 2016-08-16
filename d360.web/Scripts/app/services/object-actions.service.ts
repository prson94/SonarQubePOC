///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';

@Injectable()
export class ObjectActionsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectActions(objectID: number, objectType: string, context?: string): Promise<any> {
        var currentContext = context == undefined ? "default" : context;
        return this.http.get(`api/${objectType}/${objectID}/angularactions/${currentContext}`)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }
}