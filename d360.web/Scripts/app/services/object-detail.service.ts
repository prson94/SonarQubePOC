///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { DetailField, DetailRow, DetailModel, IObjectDetailService } from '../models/object-detail.model';
import { MessagesService } from './index';
import { BaseService } from './base.service';

@Injectable()
export class ObjectDetailService extends BaseService implements IObjectDetailService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectDetail(objectID: number, objectType: string): Promise<DetailModel> {
        return this.http.get(`api/${objectType}/${objectID}/detail`)
            .toPromise()
            .then(response => <DetailModel>response.json())
            .catch(err=>this.handleError(err));
    }
}