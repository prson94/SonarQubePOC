
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ObjectStatistics } from '../models/object-statistics.model';

@Injectable()
export class ObjectStatisticsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectStatistics(objectID: number, objectType: string): Promise<ObjectStatistics> {
        return this.http.get(`api/${objectType}/${objectID}/object/statistics`)
            .toPromise()
            .then(response => <ObjectStatistics>response.json())
            .catch(err => this.handleError(err));
    }
}