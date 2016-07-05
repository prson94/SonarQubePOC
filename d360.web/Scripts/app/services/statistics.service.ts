///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { StatisticType } from '../models/statistic.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class StatisticService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getStatistics(): Promise<StatisticType[]> {
        return this.http.get(`api/statistics`)
            .toPromise()
            .then(response => <StatisticType[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteStatistic(id: number) {
        return this.deleteDynamic(this.http, 'statistictype', id);
    }

    saveStatistic(statisticType: StatisticType): Promise<JsonResult> {
        if (statisticType.ID == undefined || !statisticType.ID) {
            return this.postDynamic(this.http, 'statistictype', statisticType);
        }
        return this.putDynamic(this.http, 'statistictype', statisticType);
    }
}