import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { PointBreakdown, ScorePoint, AverageScore } from '../models/score.model';

@Injectable()
export class ScoreService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getPointBreakdown(objectID: number, objectType: string, date: string = null): Promise<PointBreakdown[]> {
        let uri = `queries/${objectType}/${objectID}/PointBreakdownByObject` + (date == null ? '' : `?date=${date}`);

        return this.http.get(uri)
            .toPromise()
            .then(response => <PointBreakdown[]>response.json())
            .catch(err => this.handleError(err));
    }

    getScoreHistory(objectID: number, objectType: string): Promise<ScorePoint[]> {
        return this.http.get(`queries/${objectType}/${objectID}/ScoreHistoryByObject`)
            .toPromise()
            .then(response => <ScorePoint[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAverageScore(objectID: number, objectType: string): Promise<AverageScore> {
        return this.http.get(`queries/${objectType}/${objectID}/AverageScoreByObjectType`)
            .toPromise()
            .then(response => <AverageScore>response.json())
            .catch(err => this.handleError(err));
    }
}