import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { StatisticType, StatisticCheckObjectOptions, StatisticCheckType, StatisticObjectOptions, ScoreType, ScoreTypeMetric } from '../models/statistic.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class StatisticService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getScoreTypeMetrics(scoreTypeId: number): Promise<ScoreTypeMetric[]> {
        return this.http.get(`api/scoring/types/${scoreTypeId}/metrics`)
            .toPromise()
            .then(response => <ScoreTypeMetric[]>response.json())
            .catch(err => this.handleError(err));
    }

    getScoreTypes(): Promise<ScoreType[]> {
        return this.http.get(`api/scoring/types?$orderby=Name`)
            .toPromise()
            .then(response => <ScoreType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getScoreType(id: number): Promise<ScoreType> {
        return this.http.get(`form/scoretype_formdata?id=${id}`)
            .toPromise()
            .then(response => <ScoreType>response.json())
            .catch(err => this.handleError(err));
    }

    getScoreTypeMetric(id: number): Promise<ScoreTypeMetric> {
        return this.http.get(`form/scoretypemetric_formdata?id=${id}`)
            .toPromise()
            .then(response => <ScoreTypeMetric>response.json())
            .catch(err => this.handleError(err));
    }

    deleteScoreType(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'scoretype', id);
    }

    deleteScoreTypeMetric(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'scoretypemetric', id);
    }

    saveScoreType(scoreType: ScoreType): Promise<JsonResult> {
        scoreType.Description = String(scoreType.Description).replace(/<(?:.|\n)*?>/gm, '');
       if (scoreType.ID == undefined || !scoreType.ID) {
            return this.postDynamic(this.http, 'scoretype', scoreType);
        }
        return this.putDynamic(this.http, 'scoretype', scoreType);
    }

    saveScoreTypeMetric(scoreTypeMetric: ScoreTypeMetric): Promise<JsonResult> {
        if (scoreTypeMetric.ID == undefined || !scoreTypeMetric.ID) {
            return this.postDynamic(this.http, 'scoretypemetric', scoreTypeMetric);
        }
        return this.putDynamic(this.http, 'scoretypemetric', scoreTypeMetric);
    }


    getMetricCheckTypes(): Promise<StatisticCheckType[]> {
        return this.http.get('form/scoretypemetric_checktypeoptions')
            .toPromise()
            .then(response => <StatisticCheckType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getMetricObjects(): Promise<StatisticObjectOptions[]> {
        return this.http.get('form/scoretypemetric_objectoptions')
            .toPromise()
            .then(response => <StatisticObjectOptions[]>response.json())
            .catch(err => this.handleError(err));
    }

    getMetricCheckObjects(type: string, id:number, check:number): Promise<StatisticCheckObjectOptions[]> {
        return this.http.get(`form/scoretypemetric_checkobjectoptions?type=${type}&id=${id}&check=${check}`)
            .toPromise()
            .then(response => <StatisticCheckObjectOptions[]>response.json())
            .catch(err => this.handleError(err));
    }
}