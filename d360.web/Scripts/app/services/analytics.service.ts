///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Analytic } from '../models/analytic.model';

@Injectable()
export class AnalyticsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getAnalytics(): Promise<Analytic[]> {
        return this.http.get(`api/statistics`)
            .toPromise()
            .then(response => <Analytic[]>response.json())
            .catch(err => this.handleError(err));
    }
}