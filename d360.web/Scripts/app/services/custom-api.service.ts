import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ApiService, ApiEndpoint } from '../models/custom-api.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class CustomAPIService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getServices(): Promise<ApiService[]> {
        return this.http.get(`api/custom/services`)
            .toPromise()
            .then(response => <ApiService[]>response.json())
            .catch(err => this.handleError(err));
    }

    getService(id: number): Promise<ApiService> {
        return this.http.get(`api/custom/service/${id}`)
            .toPromise()
            .then(response => <ApiService>response.json())
            .catch(err => this.handleError(err));
    }

    getEndpoints(id: number): Promise<ApiEndpoint[]> {
        return this.http.get(`api/custom/service/${id}/endpoints`)
            .toPromise()
            .then(response => <ApiEndpoint[]>response.json())
            .catch(err => this.handleError(err));
    }
    
    saveService(service: ApiService): Promise<JsonResult> {
        if (service.ID == undefined || !service.ID) {
            return this.postDynamic(this.http, 'service', service);
        }
        return this.putDynamic(this.http, 'service', service);
    }

    saveEndpoint(endpoint: ApiEndpoint): Promise<JsonResult> {
        if (endpoint.ID == undefined || !endpoint.ID) {
            return this.postDynamic(this.http, 'endpoint', endpoint);
        }
        return this.putDynamic(this.http, 'endpoint', endpoint);
    }
}