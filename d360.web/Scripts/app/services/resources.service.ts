///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Resource, CountObject } from '../models/resource.model';

@Injectable()
export class ResourcesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getResources(): Promise<Resource[]> {
        return this.http.get('/api/resources/1')
            .toPromise()
            .then(response => <Resource[]>response.json())
            .catch(err => this.handleError(err));
    }

    getResource(id: number): Promise<Resource> {
        return this.http.get(`/api/resources/1/${id}`)
            .toPromise()
            .then(response => <Resource>response.json())
            .catch(err => this.handleError(err));
    }

    getResponsibilityBreakdownByResource(id: number): Promise<CountObject[]> {
        console.log(id);
        return this.http.get(`tiles/ResponsibilityBreakdownByResource?id=${id}`)
            .toPromise()
            .then(response => <CountObject[]>response.json())
            .catch(err => this.handleError(err));
    }

    getFollowingBreakdownByResource(id: number): Promise<CountObject[]> {
        return this.http.get(`tiles/FollowingBreakdownByResource?id=${id}`)
            .toPromise()
            .then(response => <CountObject[]>response.json())
            .catch(err => this.handleError(err));
    }
}