///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { PointBreakdown } from '../models/score.model';

@Injectable()
export class ScoreService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getPointBreakdown(objectID: number, objectType: string): Promise<PointBreakdown[]> {
        return this.http.get(`queries/${objectType}/${objectID}/PointBreakdownByObject`)
            .toPromise()
            .then(response => <PointBreakdown[]>response.json())
            .catch(err => this.handleError(err));
    }
}