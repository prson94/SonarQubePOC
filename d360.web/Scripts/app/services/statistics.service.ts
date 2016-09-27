
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { StatisticType, StatisticCheckObjectOptions, StatisticCheckType, StatisticObjectOptions } from '../models/statistic.model';
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

    getStatistic(id: number): Promise<StatisticType> {
        return this.http.get(`form/statistictype_formdata?id=${id}`)
            .toPromise()
            .then(response => <StatisticType>response.json())
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


    getStatisticCheckTypes(): Promise<StatisticCheckType[]> {
        return this.http.get('form/statistictype_checktypeoptions')
            .toPromise()
            .then(response => <StatisticCheckType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getStatisticObjects(): Promise<StatisticObjectOptions[]> {
        return this.http.get('form/statistictype_objectoptions')
            .toPromise()
            .then(response => <StatisticObjectOptions[]>response.json())
            .catch(err => this.handleError(err));
    }

    getStatisticCheckObjects(type: string, id:number, check:number): Promise<StatisticCheckObjectOptions[]> {
        return this.http.get(`form/statistictype_checkobjectoptions?type=${type}&id=${id}&check=${check}`)
            .toPromise()
            .then(response => <StatisticCheckObjectOptions[]>response.json())
            .catch(err => this.handleError(err));
    }
}