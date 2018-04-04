import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { APIService } from '../models/custom-api.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class CustomAPIService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getServices(): Promise<APIService[]> {
        return this.http.get(`api/custom/services`)
            .toPromise()
            .then(response => <APIService[]>response.json())
            .catch(err => this.handleError(err));
    }
    
    saveService(service: APIService): Promise<JsonResult> {
        if (service.ID == undefined || !service.ID) {
            return this.postDynamic(this.http, 'service', service);
        }
        return this.putDynamic(this.http, 'service', service);
    }
}